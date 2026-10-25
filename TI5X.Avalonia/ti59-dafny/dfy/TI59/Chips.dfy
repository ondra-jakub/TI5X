module TI59_Chips {
  import opened TI59_Nibbles
  import opened TI59_AluOps
  import opened TI59_Bus

  // ============================================================
  // Chipy mimo CPU: brom, scom, ram, lib (CROM), klavesnice, display.
  // Kazdy chip je cista funkce (stav chipu, sbernice) -> (stav, sbernice).
  // ============================================================

  // ------------------------------------------------------------
  // brom.c - ROM 1024 slov, vlastni pc u kazdeho chipu (vsechny 3
  // bromy sleduji stejnou adresu; jen ten s rozsahem vystupuje).
  // ------------------------------------------------------------
  datatype Brom = Brom(pc: int, lastIrg: u16, lastExt: u16,
                       data: seq<u16>, start: int, end: int)

  function BromInit(data: seq<u16>, start: int, end: int): Brom
    requires |data| == end - start && 0 <= start
  {
    Brom(65536, 0, 0, data, start, end)  // [brom.c:173] pc = 1<<16
  }

  // handle_branch [brom.c:53-66]
  function BromBranchTarget(b: Brom, cond: bool): int
  {
    var bcond := ((b.lastIrg >> 11) & 1) != 0;
    var boffset := ((b.lastIrg >> 1) & (0x03FF as u16)) as int;
    var bneg := (b.lastIrg & 1) != 0;
    var off := if bneg then -boffset else boffset;
    if cond == bcond then b.pc + off else b.pc + 1
  }

  // brom_process_out [brom.c:68-126] - jen S4W.
  // pc je v C unsigned int s wraparound; zaporne hodnoty v int davaji
  // stejnou "mimo rozsah" detekci (rozsa hy ROM jsou < 3072 << 2^32).
  function BromStep(b: Brom, bus: Bus, sstate: nat, write: bool): (Brom, Bus)
    requires sstate <= 15
    requires |b.data| == b.end - b.start
  {
    if sstate != 4 || !write then (b, bus)
    else
      var pc1: int :=
        if (bus.ext & EXT_HOLD) != 0 then b.pc            // [brom.c:83-89]
        else if (b.lastExt & EXT_PREG) != 0 then ((b.lastExt >> 3) as int) // [brom.c:97-99]
        else if (b.lastIrg & (0x1000 as u16)) != 0 then
          BromBranchTarget(b, (bus.ext & EXT_COND) != 0)   // [brom.c:102-104]
        else b.pc + 1;
      var bus1 :=
        if b.start <= pc1 && pc1 < b.end then
          bus.(irg := b.data[pc1 - b.start], addr := pc1)  // [brom.c:116-124]
        else bus;
      (b.(pc := pc1), bus1)
  }

  // brom_process_in [brom.c:128-136] - jen S15R
  function BromStepRead(b: Brom, bus: Bus, sstate: nat, write: bool): (Brom, Bus)
    requires sstate <= 15
  {
    if sstate != 15 || write then (b, bus)
    else (b.(lastIrg := bus.irg, lastExt := bus.ext), bus)
  }

  // ------------------------------------------------------------
  // scom.c - konstanty + SCOM scratch registry (v2: 32 const, 8 reg).
  // TI-59: TMC0582 (regs 0-7), TMC0583 (regs 8-15) - prave R8/R9 jsou
  // "skryte" polar/rect registry z ML-01 (doc/memory-map.txt).
  // ------------------------------------------------------------
  type u32 = bv32

  datatype Scom = Scom(CONST: seq<Reg>, SCOM: seq<Reg>,
                       fifoConst: u32, fifoReg: u32,
                       startConst: int, endConst: int,
                       startReg: int, endReg: int)

  function ScomInit(consts: seq<Reg>, base: int): Scom
    requires |consts| == 32
    requires 0 <= base && base % 32 == 0
  {
    Scom(consts, seq(8, _ => SeqFill(12)), 0, 0,
         base, base + 32,           // [scom.c:245-246] const rozsah
         base / 32 * 8, base / 32 * 8 + 8)  // [scom.c:272-273] v2: 8 reg
  }

  function SeqFill(v: u8): Reg { seq(16, _ => v) }  // [scom.c:267] SCOM = 0xC

  // konstantni ROM cteni pres FIFO (1-cyklus zpozdeni) [scom.c:84-95]
  function ScomConstStepW(s: Scom, bus: Bus, sstate: nat, write: bool): (Scom, Bus)
    requires sstate <= 15
    requires |s.CONST| > 0
    requires 0 <= s.startConst && s.endConst <= s.startConst + 32
  {
    if sstate != 0 || !write then (s, bus)
    else
      var fc := s.fifoConst >> 8;
      var bus1 :=
        if (fc & (0x80 as u32)) != 0 then
          // index < |CONST| garantuje disciplinou loadu (range check v
          // ScomStepR); ochranny guard kvuli totalite
          var idx := (fc & (0x7F as u32)) as int;
          if idx < |s.CONST| then bus.(io := s.CONST[idx]) else bus
        else bus;
      (s.(fifoConst := fc), bus1)
  }

  // scom2_reg_process [scom.c:151-209] (TI-59 = v2: hex adresa z io[0])
  function ScomRegStepW(s: Scom, bus: Bus, sstate: nat, write: bool): (Scom, Bus)
    requires sstate <= 15
    requires |s.SCOM| == 8
  {
    if sstate != 0 || !write then (s, bus)
    else
      var fr := s.fifoReg >> 8;
      var bus1 :=
        if (fr & (0x10 as u32)) != 0 then
          bus.(io := s.SCOM[((fr >> 5) & 7) as int])  // 3-bit index < 8
        else bus;
      (s.(fifoReg := fr), bus1)
  }

  // S15R: STO zapis + match RCL F/STO F [scom.c:163-199]
  function ScomRegStepR(s: Scom, bus: Bus, sstate: nat, write: bool): (Scom, Bus)
    requires sstate <= 15
    requires |s.SCOM| == 8
    requires 0 <= s.startReg && s.endReg <= s.startReg + 8
  {
    if sstate != 15 || write then (s, bus)
    else
      var s1 :=
        if (s.fifoReg & 1) != 0 && (s.fifoReg & (0x10 as u32)) == 0 then
          // STO: SCOM[addr] = io [scom.c:165-169]
          var a := ((s.fifoReg >> 5) & 7) as int;
          s.(SCOM := s.SCOM[a := bus.io])
        else s;
      var s2 :=
        if (bus.irg & (0xFFEF as u16)) == (0x0A0F as u16) then
          var addr := (bus.io[0]) as int;
          // [scom.c:190-193] "force 0 STO" - SR51-II workaround
          var addr1 := if (s1.fifoReg & (0x10 as u32)) != 0 then 0 else addr;
          if s.startReg <= addr1 && addr1 < s.endReg then
            // fifoReg |= ((addr<<5 | irg&0x1F) << 16) - spocteno v int
            var a := (addr1 - s.startReg) * 32 + ((bus.irg & (0x1F as u16)) as int);
            s1.(fifoReg := s1.fifoReg | ((a as u32) << 16))
          else s1
        else s1;
      (s2, bus)
  }

  // S15R: match "constant io" instrukci [scom.c:96-107]
  function ScomConstStepR(s: Scom, bus: Bus, sstate: nat, write: bool): (Scom, Bus)
    requires sstate <= 15
    requires 0 <= s.startConst && s.endConst <= s.startConst + 32
  {
    if sstate != 15 || write then (s, bus)
    else
      var s1 :=
        if (bus.irg & (0x10D0 as u16)) == (0x00C0 as u16)
            && (bus.irg & (0x0F00 as u16)) != 0
            && (bus.irg & (0x0F00 as u16)) != (0x0800 as u16)
            && (bus.irg & (0x0F00 as u16)) != (0x0A00 as u16) then
          var addr := (((bus.ext >> 4) & (0x0078 as u16)) | ((bus.ext >> 3) & (0x0007 as u16))) as int;
          if s.startConst <= addr && addr < s.endConst then
            // fifoConst |= ((addr-start) | 0x80) << 8; addr-start < 32
            // nema bit 0x80 -> +0x80 == |0x80
            var idx := ((addr - s.startConst) + 0x80) as u32;
            s.(fifoConst := s.fifoConst | (idx << 8))
          else s
        else s;
      (s1, bus)
  }

  // ------------------------------------------------------------
  // ram.c - 30x16 cisel. na chip, cmd protokol (0=read,1=write,
  // 2=clear1, 4=clear10), adresa = io[3]*10+io[2] (BCD).
  // ------------------------------------------------------------
  // [ram.c:32-34] RAM_WAIT_CMD/WAIT2/EXEC - v modelu jako booly
  datatype Ram = Ram(start: int, end: int, data: seq<Reg>,
                     waitCmd: bool, wait2Cmd: bool, execCmd: bool,
                     cmd: int, addr: int)

  function RamInit(data: seq<Reg>, start: int): Ram
    requires |data| == 30
  {
    Ram(start, start + 30, data, false, false, false, 0, 0)  // [ram.c:123-125]
  }

  // S0W [ram.c:48-68]
  function RamStepW(r: Ram, bus: Bus, sstate: nat, write: bool): (Ram, Bus)
    requires sstate <= 15
    requires |r.data| == 30
  {
    if sstate != 0 || !write then (r, bus)
    else if !r.execCmd then (r, bus)
    else
      var inRange := 0 <= r.addr && r.addr < |r.data|;
      (if r.cmd == 2 then                              // clear 1 register
         (r.(data := if inRange then r.data[r.addr := ZeroReg()] else r.data,
           execCmd := false), bus)
       else if r.cmd == 4 then                         // clear 10 (CMs)
         // C dela memset 16*10 - pro addr > 20 by pretekla do dalsich
         // poli structu (UB); model cisti jen registry v rozsahu chipu.
         (r.(data := RamClear10(r.data, r.addr), execCmd := false), bus)
       else if r.cmd == 0 then                          // read
         (r.(execCmd := false),
          if inRange then bus.(io := r.data[r.addr]) else bus)
       else (r, bus))
  }

  function RamClear10(data: seq<Reg>, addr: int): seq<Reg>
    requires |data| == 30
  {
    RamClearFrom(data, addr, 0)
  }

  function RamClearFrom(data: seq<Reg>, addr: int, k: nat): seq<Reg>
    requires |data| == 30
    decreases 10 - k
  {
    if k >= 10 then data
    else if 0 <= addr + k && addr + k < 30 then
      RamClearFrom(data[addr + k := ZeroReg()], addr, k + 1)
    else
      RamClearFrom(data, addr, k + 1)
  }

  // S15R [ram.c:69-113]
  function RamStepR(r: Ram, bus: Bus, sstate: nat, write: bool): (Ram, Bus)
    requires sstate <= 15
    requires |r.data| == 30
  {
    if sstate != 15 || write then (r, bus)
    else
      // write [ram.c:71-75]
      var r1 :=
        if r.execCmd && r.cmd == 1
            && 0 <= r.addr && r.addr < |r.data| then
          r.(data := r.data[r.addr := bus.io], execCmd := false)
        else r;
      // WAIT2: cmd + addr z io [ram.c:76-104]; ignore_cs=1 pro 4xRAM
      var r2 :=
        if r1.wait2Cmd then
          var addr := (bus.io[3] as int) * 10 + (bus.io[2] as int);
          var cmd := (bus.io[0]) as int;
          (if r.start <= addr && addr < r.end && (cmd <= 2 || cmd == 4)
           then r1.(addr := addr - r.start, cmd := cmd,
                   wait2Cmd := false, execCmd := true)
           else r1.(wait2Cmd := false))
        else r1;
      // WAIT -> WAIT2 [ram.c:105-108]
      var r3 :=
        if r2.waitCmd then r2.(waitCmd := false, wait2Cmd := true) else r2;
      // match RAM_OP instrukce [ram.c:110-112]
      var r4 :=
        if bus.irg == (0x0AF8 as u16) then r3.(waitCmd := true) else r3;
      (r4, bus)
  }

  // ------------------------------------------------------------
  // lib.c - CROM modul (Master Library), pc je BCD-souradnice dat.
  // ------------------------------------------------------------
  // [lib.c:26-28] WAIT_IN_* - v modelu booly
  datatype LibFlags = LibFlags(inData: bool, inPc: bool, inDataHigh: bool)

  const LIB_NO_FLAGS: LibFlags := LibFlags(false, false, false)

  datatype Lib = Lib(pc: int, data: seq<u8>, flags: LibFlags, flagsDelay: LibFlags)

  function LibInit(data: seq<u8>): Lib
    requires |data| == 5000
  {
    Lib(222, data, LIB_NO_FLAGS, LIB_NO_FLAGS)  // [lib.c:157] pc = 0xDE
  }

  function LibData(l: Lib, i: int): u8
  {
    if 0 <= i && i < |l.data| then l.data[i] else 0  // totalitni guard
  }

  // S15W [lib.c:66-85]
  function LibStepW(l: Lib, bus: Bus, sstate: nat, write: bool): (Lib, Bus)
    requires sstate <= 15
  {
    if sstate != 15 || !write then (l, bus)
    else if l.flagsDelay.inData then
      var d := LibData(l, l.pc) as u16;
      var pc1 := l.pc + 1;
      var pc2 := if pc1 >= |l.data| then 0 else pc1;   // [lib.c:72-73]
      (l.(pc := pc2), bus.(ext := bus.ext | (d << 3)))
    else if l.flagsDelay.inPc then
      (l.(pc := l.pc / 10),                            // [lib.c:76-78]
       bus.(ext := bus.ext | (((l.pc % 10) as u16) << 3)))
    else if l.flagsDelay.inDataHigh then
      var d := (LibData(l, l.pc) >> 4) as u16;
      (l, bus.(ext := bus.ext | (d << 3)))
    else (l, bus)
  }

  // S15R [lib.c:86-114]
  function LibStepR(l: Lib, bus: Bus, sstate: nat, write: bool): (Lib, Bus)
    requires sstate <= 15
  {
    if sstate != 15 || write then (l, bus)
    else
      var l1 := l.(flagsDelay := l.flags, flags := LIB_NO_FLAGS);
      var l2 :=
        if bus.irg == (0x0A0E as u16) then
          l1.(flags := LibFlags(true, l1.flags.inPc, l1.flags.inDataHigh))
        else if bus.irg == (0x0A1E as u16) then
          // set pc [lib.c:97-103]
          var pcH := ((bus.ext >> 3) & (0x000F as u16)) as int;
          l1.(pc := l1.pc / 10 + pcH * 1000)
        else if bus.irg == (0x0A2E as u16) then
          l1.(flags := LibFlags(l1.flags.inData, true, l1.flags.inDataHigh))
        else if bus.irg == (0x0A3E as u16) then
          l1.(flags := LibFlags(l1.flags.inData, l1.flags.inPc, true))
        else l1;
      (l2, bus)
  }

  // ------------------------------------------------------------
  // key.c - klavesnice + deterministicky skript (-K patch).
  // ------------------------------------------------------------
  datatype KeyEvent = KeyEvent(code: u8, at: nat, hold: nat, toggle: bool)

  datatype KeyState = KeyState(persKeys: seq<u8>,  // 16x u8 bit mask
                               keyCode: u8, keyCount: int,
                               keyCodeHw: u8, keyCountHw: int,
                               pressCycle: nat, unpressCycle: nat,
                               pressMask: u8, unpressMask: u8,
                               script: seq<KeyEvent>, scriptPos: nat)

  // [bus.h:55] KP_BIT = 2 (detekce tiskarny pri bootu)
  const KP_BIT: nat := 2

  // [bus.h:55] KR_BIT = 4 (card reader, crd_clear_switch zapisuje bit 4)
  const KR_BIT: nat := 4

  function KeyInit(script: seq<KeyEvent>, hasPrinter: bool): KeyState
    requires |script| >= 0
  {
    // [key.c:576-584] ti59: card reader closed (key[10] |= KR_BIT=bit4),
    // key_count=1, key_code=1 (fantom - verno!), masky 0x20, cykly 2/3.
    // [key.c:582-583] ti59 + HW_PRINTER: key[0] |= (1 << KP_BIT=2) - meni
    // boot sekvenci (detekce tiskarny); default false = chovani beze zmeny.
    KeyState(seq(16, i => if i == 10 then (0x10 as u8)
                          else if i == 0 && hasPrinter then (0x04 as u8)
                          else 0 as u8),
             (1) as u8, 1, 0, 0, 2, 3, 0x20, 0x20, script, 0)
  }

  // key_read2 v script modu [key.c -K patch 0003]: toggles (du, vsichni
  // key_read2 call site) + pripadny press (jen scan-all read-new)
  function KeyReadNew(k: KeyState, cycle: nat): KeyState
    requires |k.persKeys| == 16
    ensures |KeyReadNew(k, cycle).persKeys| == 16
  {
    var k1 := KeyApplyToggles(k, cycle);
    if k1.scriptPos < |k1.script| then
      var ev := k1.script[k1.scriptPos];
      if cycle >= ev.at then
        k1.(keyCode := ev.code,
           keyCount := if ev.hold > 0 then ev.hold else k1.pressCycle,
           scriptPos := k1.scriptPos + 1)
      else k1
    else k1
  }

  function KeyApplyToggles(k: KeyState, cycle: nat): KeyState
    requires |k.persKeys| == 16
    ensures |KeyApplyToggles(k, cycle).persKeys| == 16
    decreases |k.script| - k.scriptPos
  {
    if k.scriptPos < |k.script| && k.script[k.scriptPos].toggle
        && cycle >= k.script[k.scriptPos].at then
      var ev := k.script[k.scriptPos];
      var idx := (ev.code & 0x0F) as int;
      var bit := ((ev.code >> 4) & 7) as int;
      var pers := if 0 <= idx && idx < |k.persKeys| then
          k.persKeys[idx] else 0 as u8;
      var pers1 := pers ^ Bit8(bit);
      KeyApplyToggles(k.(persKeys := k.persKeys[idx := pers1],
                         scriptPos := k.scriptPos + 1), cycle)
    else k
  }

  // key_process [key.c:421-507] - jen S15R
  function KeyStep(k: KeyState, bus: Bus, sstate: nat, write: bool): (KeyState, Bus)
    requires sstate <= 15
    requires |k.persKeys| == 16
    requires bus.dstate <= 15
  {
    if sstate != 15 || write then (k, bus)
    else
      // [key.c:427-478] keyboard instrukce
      var k1: KeyState :=
        if (bus.ext & EXT_HOLD) == 0
            && (bus.irg & (0xFF00 as u16)) == (0x0800 as u16) then
          var scan := (bus.irg & (0x0008 as u16)) == 0;
          if !scan then
            // scan-row [key.c:540-551]: script mod - key_read2(0,0) site
            // = konzumace due TOGGLE entry [patch 0003], hw countdown
            (if bus.dstate < 15 && bus.dstate > 0 then
               (if k.keyCountHw <= 0 then
                  KeyApplyToggles(k, bus.cycleCount)
                else k.(keyCountHw := k.keyCountHw - 1))
             else k)
          else
            var allPress := (bus.irg & (0x00FF as u16)) == (k.pressMask as u16);
            var allUnpress := (bus.irg & (0x00FF as u16)) == (k.unpressMask as u16);
            if allPress && k.keyCount > 1 then                    // repeat
              k.(keyCount := k.keyCount - 1)
            else if allUnpress && k.keyCount > 0 - k.unpressCycle then
              k.(keyCode := 0, keyCount := k.keyCount - 1,       // no key
                keyCodeHw := 0, keyCountHw := 0)
            else if allPress && k.keyCode == 0 then
              var kread := KeyReadNew(k, bus.cycleCount);         // read new
              kread.(keyCodeHw := kread.keyCode, keyCountHw := kread.keyCount)
            else k
        else k;
      // [key.c:473-476] key_line z keyCode (scan-all vetev)
      var keyLine1: u8 :=
        if (bus.ext & EXT_HOLD) == 0
            && (bus.irg & (0xFF00 as u16)) == (0x0800 as u16)
            && (bus.irg & (0x0008 as u16)) == 0
            && ((k1.keyCode & 0x0F) as int) == bus.dstate && k1.keyCount > 0 then
          bus.keyLine | Bit8(((k1.keyCode >> 4) & 7) as int)
        else bus.keyLine;
      // [key.c:480-483] hw key line
      var keyLine2: u8 :=
        if ((k1.keyCodeHw & 0x0F) as int) == bus.dstate && k1.keyCountHw > 0 then
          keyLine1 | Bit8(((k1.keyCodeHw >> 4) & 7) as int)
        else keyLine1;
      // [key.c:485] persistent keys (card reader closed / printer)
      var keyLine3: u8 := keyLine2 | k1.persKeys[bus.dstate];
      (k1, bus.(keyLine := keyLine3))
  }

  // ------------------------------------------------------------
  // display.c display_process2 (ti59) - 12 cislic na D13-D2.
  // ------------------------------------------------------------
  datatype Disp = Disp(out: seq<char>, out1: seq<char>, pos: nat)

  function DispInit(): Disp
  {
    Disp(seq(30, _ => Nul()), seq(30, _ => Nul()), 0)  // C globaly zerovane
  }

  function SetChar(s: seq<char>, i: nat, c: char): seq<char>
    requires |s| == 30
  {
    if i < 30 then s[i := c] else s  // totalitni guard (C: pole 30 znaku)
  }

  // display_process2 [display.c:86-130] - jen S0R
  function DispStep(d: Disp, bus: Bus, sstate: nat, write: bool): (Disp, Bus)
    requires sstate <= 15
    requires |d.out| == 30 && |d.out1| == 30
  {
    if sstate != 0 || write then (d, bus)
    else
      var out1: seq<char> :=
        if 2 <= bus.dstate && bus.dstate <= 13 then
          if bus.dstate != 13 then
            SetChar(d.out, d.pos, bus.displayDigit)
          else if bus.displayDigit == '-' then
            // D13: '-' nebo E/C podle segH [display.c:98-110]
            SetChar(d.out, d.pos, if bus.displaySegH then 'E' else '-')
          else
            SetChar(d.out, d.pos, if bus.displaySegH then 'C' else ' ')
        else d.out;
      var pos1: nat :=
        if 2 <= bus.dstate && bus.dstate <= 13 then d.pos + 1 else d.pos;
      var out2: seq<char> :=
        if 2 <= bus.dstate && bus.dstate <= 13 && bus.displayDpt then
          SetChar(out1, pos1, '.')       // [display.c:113-114]
        else out1;
      var pos2: nat :=
        if 2 <= bus.dstate && bus.dstate <= 13 && bus.displayDpt then pos1 + 1
        else pos1;
      if bus.dstate == 0 then
        var outF := SetChar(out2, pos2 + 2, if bus.idle then ' ' else 'B');  // [display.c:118]
        // commit: out1 := outF (tisk resi runner); reset bufferu [display.c:125-129]
        // (memset ' ' na 29 znacich, out[29] zustava)
        (Disp(seq(30, i => if i < 29 then ' ' else outF[29]), outF, 0), bus)
      else (Disp(out2, d.out1, pos2), bus)
  }
}