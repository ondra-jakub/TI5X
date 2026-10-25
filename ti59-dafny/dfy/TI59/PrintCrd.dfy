module TI59_PrintCrd {
  import opened TI59_Nibbles
  import opened TI59_Bus
  import opened TI59_Chips

  // ============================================================
  // print.c - tiskarna TMC0251 (TI-59) a crd.c - ctecka karet.
  // Verny port; LOG() volani C simulatoru (viz -v 3 = LOG_SHORT +
  // LOG_HRAST v log.txt) se emituji jako Ti59Event - runner je tiskne
  // ve stejnem formatu, compare-trace.py porovnavaje podsekvence.
  // ============================================================

  // ------------------------------------------------------------
  // Udalosti; poradi odpovida poradi chipu ve foldu = poradi C logu.
  // ------------------------------------------------------------
  datatype Ti59Event =
    // [print.c:175] LOG("PRINT_CHAR[%d]='%c' ", head, buffer[head])
    EvPrintChar(head: nat, ch: char)
    // [print.c:191,201] LOG("PRINT_FUNC[%d]='%.3s' ", head, str)
    | EvPrintFunc(head: nat, s: string)
    // [print.c:196] LOG("PRINT[%d]='%d not fund' ", head, code)
    | EvPrintNotFound(head: nat, code: nat)
    // [print.c:209] LOG("PRINT_CLEAR[%d]='%.20s' ", head, buffer)
    | EvPrintClear(head: nat, buf: string)
    // [print.c:232] LOG("PRINT[%d]='%.20s' ", head, buffer)
    | EvPrint(head: nat, buf: string)
    // [crd.c:41] LOG ("CRD.rd[%04d]=%02x ", pc, data)
    | EvCrdRd(pc: nat, data: u8)
    // [crd.c:63] LOG("CRD.wr[%04d]=%02x ", pc, data)
    | EvCrdWr(pc: nat, data: u8)
    // [crd.c:71] printf("crd read/write %d\n") - jde na stdout, ne do logu
    | EvCrdOff(pc: nat)

  // ------------------------------------------------------------
  // print.c - stav [print.c:24-32].
  // ------------------------------------------------------------
  // busy je v C nula a nikdo ho nenastavuje (mrtva vetev 0x0A98) -
  // model vynechava, 0x0A98 = vzdy print_step. Masky: TMC0251 0x0A08
  // (TI-59); TMC0253 0x0A06/sr60 font se maskou odmitne - neportujeme.
  datatype PrintState = PrintState(buffer: seq<char>,   // 20 znaku
                                   head: int,           // init 5
                                   tape: seq<seq<char>>) // 1 radek = PRINT/advance

  predicate PrintWF(p: PrintState) {
    |p.buffer| == 20 && 0 <= p.head <= 19
  }

  function PrintInit(): PrintState
  {
    // [print.c:251-254] memset 'X' (20 znaku), buffer[20] = 0, head = 5
    PrintState(seq(20, _ => 'X'), 5, [])
  }

  // [print.c:155-160] print_step
  function PrintHeadStep(head: int): int
    requires 0 <= head <= 19
  {
    if head == 0 then 19 else head - 1
  }

  // [print.c:36-45] print_font - TMC0251 (TI-59), 64 znaku (8 radku x 8,
  // index = 6bitovy kod z ext; font[0]=' ', vsechny znaky unikatni).
  function PrintFont(): seq<char>
  {
    " 0123456" +
    "789ABCDE" +
    "-FGHIJKL" +
    "MNOPQRST" +
    ".UVWXYZ+" +
    "x*sp,e()," +
    "^%|/='#~" +
    "z?:!]\"][$"   // z=^2, ]=2nd, "=delta, [=product, $=sum
  }

  // [print.c:74-83] print_font_sr60 - TMC0254 (SR-60); pro ti59 neaktivni,
  // uklada se pro uplnost (jedna by maska odmitla ops TMC0251).
  function PrintFontSr60(): seq<char>
  {
    " 0123456" +
    "789.z#!\"" +
    "=-+:x'~)" +
    "s^$[/|e]" +
    "QRSTUVWX" +
    "YZ*p?,%(" +
    "ABCDEFGH" +
    "IJKLMNOP"
  }

  // [print.c:86-130] print_func tabulka; 39 polozek, terminator
  // {0, {0}} v C = konec skenu (vsechny dalsi maji str[0] == NUL).
  function PrintFuncTable(): seq<(u8, string)>
  {
    [ (0x00, "   ")   , (0x11, " = ") , (0x12, " - ") , (0x13, " + ") ,
      (0x16, " / ")   , (0x17, " x ") , (0x1A, "xsY") , (0x1B, "Y^x") ,
      (0x21, "CLR")   , (0x22, "INV") , (0x23, "DPT") , (0x26, "CE ") ,
      (0x27, "+/-")   , (0x2D, "EE ") , (0x31, "e^x") , (0x33, "x^2") ,
      (0x36, "1/x")   , (0x3C, "sX ") , (0x3D, "X|Y") , (0x51, "LNX") ,
      (0x53, "PRM")   , (0x54, " % ") , (0x56, "COS") , (0x57, "SIN") ,
      (0x5D, "TAN")   , (0x61, "SUM") , (0x66, "STO") , (0x67, "pi ") ,
      (0x68, "RCL")   , (0x69, "$+ ") , (0x70, "ERR") , (0x71, " ( ") ,
      (0x72, " ) ")   , (0x73, "LRN") , (0x74, "RUN") , (0x76, "HLT") ,
      (0x78, "STP")   , (0x7A, "GTO") , (0x7C, "IF ") ]
  }

  // [print.c:185-194] first-match sken tabulky (C smycka i = 0..terminator)
  datatype FuncIdx = FNone | FFound(i: nat)

  function FuncLookup(t: seq<(u8, string)>, code: u8, k: nat): FuncIdx
    requires |t| > 0
    ensures match FuncLookup(t, code, k)
            case FNone => true
            case FFound(i) => i < |t|
    decreases |t| - k
  {
    if k >= |t| then FNone
    else if t[k].0 == code then FFound(k)
    else FuncLookup(t, code, k + 1)
  }

  function FuncMatch(code: u8): FuncIdx
  {
    FuncLookup(PrintFuncTable(), code, 0)
  }

  // %.20s na buffer (char[21], buffer[20] vzdy NUL) = vzdy 20 znaku.
  function BufStr20(buffer: seq<char>): string
    requires |buffer| >= 20
  {
    buffer[0..20]
  }

  // [print.c:162-242] print_process - jen S15R; maska = (irg & 0xFF0F) ==
  // 0x0A08. 0x0AA8 -> display_print(buffer) = radku pasky; 0x0AB8 -> display_
  // print("") = prazdny radek (v C koment XXX: "we advance one line instead
  // of half"; znakove %.20s "" -> nic). 0x0AA6/0x0A66/... (TMC0253) maskou
  // odmitnuty. 0x0A98 (step): busy vetev mrtva -> vzdy print_step.
  function PrintStep(p: PrintState, bus: Bus, sstate: nat, write: bool)
      : (PrintState, seq<Ti59Event>)
    requires PrintWF(p) && sstate <= 15
    ensures |PrintStep(p, bus, sstate, write).0.buffer| == 20
    ensures 0 <= PrintStep(p, bus, sstate, write).0.head <= 19
  {
    if sstate != 15 || write ||
        (bus.irg & (0xFF0F as u16)) != (0x0A08 as u16) then
      (p, [])
    else if bus.irg == (0x0A68 as u16) then
      // load char [print.c:169-178]; LOG pred print_step (stary head)
      var code := ((bus.ext >> 3) & (0x3F as u16)) as int;
      var ch := PrintFont()[code];
      (p.(buffer := p.buffer[p.head := ch], head := PrintHeadStep(p.head)),
       [EvPrintChar(p.head, ch)])
    else if bus.irg == (0x0A78 as u16) then
      // load func [print.c:179-205]; LOG po zapisu (posunuty head),
      // zapis buffer[head] = str[j] pro j = 2,1,0 (zprava doleva)
      var code := ((bus.ext >> 3) & (0x7F as u16)) as int;
      match FuncMatch(code as u8)
      case FNone =>
        // [print.c:195-202]: LOG "not fund", 3x '?', LOG "???"
        var h0 := p.head;
        var h1 := PrintHeadStep(h0);
        var h2 := PrintHeadStep(h1);
        var h3 := PrintHeadStep(h2);
        var b3 := p.buffer[h0 := '?'][h1 := '?'][h2 := '?'];
        (p.(buffer := b3, head := h3),
         [EvPrintNotFound(h0, code), EvPrintFunc(h3, "???")])
      case FFound(idx) =>
        var s := PrintFuncTable()[idx].1;
        var h0 := p.head;
        var h1 := PrintHeadStep(h0);
        var h2 := PrintHeadStep(h1);
        var h3 := PrintHeadStep(h2);
        var b3 := p.buffer[h0 := s[2]][h1 := s[1]][h2 := s[0]];
        (p.(buffer := b3, head := h3), [EvPrintFunc(h3, s)])
    else if bus.irg == (0x0A88 as u16) then
      // clear [print.c:206-212]: LOG pred memset; buffer = mezery, head = 19
      (p.(buffer := seq(20, _ => ' '), head := 19),
       [EvPrintClear(p.head, BufStr20(p.buffer))])
    else if bus.irg == (0x0A98 as u16) then
      // step [print.c:213-224]: busy vetev mrtva, bez udalosti
      (p.(head := PrintHeadStep(p.head)), [])
    else if bus.irg == (0x0AA8 as u16) then
      // print [print.c:225-233]: radek pasky = buffer; bez zmeny stavu
      (p.(tape := p.tape + [BufStr20(p.buffer)]),
       [EvPrint(p.head, BufStr20(p.buffer))])
    else if bus.irg == (0x0AB8 as u16) then
      // advance [print.c:234-238]: display_print("") = prazdny radek
      (p.(tape := p.tape + [seq(0, _ => ' ')]), [])
    else
      // nezname op kody (0x0A08/18/28/38/48/58/18/20/...): C switch nic
      // neudela - return 0 bez zmeny
      (p, [])
  }

  // ------------------------------------------------------------
  // crd.c - magneticka ctecka/zapisovacka karet.
  // ------------------------------------------------------------
  datatype OptCrd = CardNone | Card(img: seq<u8>)

  datatype CrdState = CrdState(pc: int,          // 0..299, mod 300
                               data: seq<u8>,    // 300 bytu (init 0x92 + obraz)
                               waitIn: bool,     // [crd.c:7,11] WAIT_IN_DATA
                               waitInDelay: bool, // [crd.c:12] flags_delay
                               cardOut: OptCrd)  // obraz pri 0x0A48 (file + pc)

  predicate CrdWF(c: CrdState) {
    |c.data| == 300 && 0 <= c.pc <= 299 &&
    (match c.cardOut case Card(img) => |img| == 300 case CardNone => true)
  }

  const CARD_FILL: u8 := 0x92  // [crd.c:100]

  function CrdInit(card: seq<u8>): CrdState
    requires |card| == 300
  {
    // [crd.c:98-100] pc = 0, flags = flags_delay = 0, memset 0x92;
    // fread zprava obrazem karty dela runner (data = card).
    CrdState(0, card, false, false, CardNone)
  }

  // [crd.c:34-86] crd_process.
  // S15W: flags_delay (WAIT_IN_DATA z minuleho S15R) -> ext |= data[pc]<<3,
  //       LOG CRD.rd, pc++ mod 300.
  // S15R: flags_delay := flags; flags := 0; decode:
  //   0x0A28 read flag, 0x0A38 write data ((ext>>3)&0xFF), 0x0A48 off,
  //   0x0A58/0x0AC8 no-op.
  // 0x0A48 vola crd_clear_switch() [key.c:735-741] = ZAPIS do klavesnice:
  //   key[10] |= 1 << KR_BIT (bit 4, [Chips.dfy]); bez toho by ROM
  //   neukoncila cteni karty (senzor KR). fwrite/printf stdout resi runner.
  function CrdStep(c: CrdState, k: KeyState, bus: Bus, sstate: nat, write: bool)
      : (CrdState, KeyState, Bus, seq<Ti59Event>)
    requires CrdWF(c) && |k.persKeys| == 16 && sstate <= 15
    ensures |CrdStep(c, k, bus, sstate, write).0.data| == 300
    ensures 0 <= CrdStep(c, k, bus, sstate, write).0.pc <= 299
    ensures |CrdStep(c, k, bus, sstate, write).1.persKeys| == 16
    ensures match CrdStep(c, k, bus, sstate, write).0.cardOut
            case Card(img) => |img| == 300
            case CardNone => true
  {
    if sstate != 15 then (c, k, bus, [])
    else if write then
      if c.waitInDelay then
        // [crd.c:37-45] cteni s zpozdenim pres flags_delay
        var d := c.data[c.pc];
        var bus1 := bus.(ext := bus.ext | ((d as u16) << 3));
        var c1 := c.(pc := if c.pc + 1 >= 300 then 0 else c.pc + 1);
        (c1, k, bus1, [EvCrdRd(c.pc, d)])
      else (c, k, bus, [])
    else
      // [crd.c:47-84] delay update + decode
      var c0 := c.(waitInDelay := c.waitIn, waitIn := false);
      if bus.irg == (0x0A28 as u16) then
        (c0.(waitIn := true), k, bus, [])
      else if bus.irg == (0x0A38 as u16) then
        var d := ((bus.ext >> 3) & (0xFF as u16)) as u8;
        var c1 := c0.(data := c0.data[c0.pc := d],
                     pc := if c0.pc + 1 >= 300 then 0 else c0.pc + 1);
        (c1, k, bus, [EvCrdWr(c0.pc, d)])
      else if bus.irg == (0x0A48 as u16) then
        var pers1 := k.persKeys[10] | Bit8(KR_BIT);
        var k1 := k.(persKeys := k.persKeys[10 := pers1]);
        var c2 := if c.pc != 0
                  then c0.(pc := 0, cardOut := Card(c0.data))
                  else c0.(pc := 0);
        (c2, k1, bus,
         if c.pc != 0 then [EvCrdOff(c.pc)] else [])
      else
        // 0x0A58 (crd read flag) / 0x0AC8 (crd write flag) [crd.c:80-82]
        // a vsechny ostatni op kody no-op
        (c0, k, bus, [])
  }
}