module TI59_Cpu {
  import opened TI59_Nibbles
  import opened TI59_AluOps

  // ============================================================
  // CPU [alu.c:44-76] + Execute() [alu.c:187-643] + run_early()
  // [alu.c:689-726] + alu_gen_digit() [alu.c:728-777].
  //
  // cpu.digit/EXT/key z alu_process jsou jen cache hodnot sbenrnice -
  // v modelu se predavaji jako parametry (digit, ext, keyLine).
  // cpu.cycle [alu.c:70] je mrtvy stav (pouze pro KEEP_RUN timing) - vypusten.
  // ============================================================

  datatype Cpu = Cpu(
    A: Reg, B: Reg, C: Reg, D: Reg, E: Reg,
    KR: u16, SR: u16, fA: u16, fB: u16,
    R5: u8, Sout: Reg, Sin: Reg,
    opcode: u16, flags: u16,
    addr: int, reset: nat, zeroSuppr: bool)

  // inicializace dle alu_init() [alu.c:892-932]
  function CpuInit(): Cpu
  {
    Cpu(ZeroReg(), ZeroReg(), ZeroReg(), ZeroReg(), ZeroReg(),
        (0) as u16, (0) as u16, (0) as u16, (0) as u16,
        (0) as u8, ZeroReg(), ZeroReg(),
        (0) as u16, FLG_COND,   // [alu.c:897] "set cond for easy compare"
        0, 5, false)            // [alu.c:927] cpu.reset = 5
  }

  function Flg(f: u16, bit: u16): bool { (f & bit) != (0 as u16) }

  // ============================================================
  // Tabulky ALU [alu.c:529-580]
  // ============================================================
  datatype RegSel = RS_NULL | RS_A | RS_B | RS_C | RS_D

  datatype AluInp = AluInp(hasX: bool, x: RegSel, hasY: bool, y: RegSel, op: AluOp)

  // ALU_OP[32] [alu.c:532-567]; io-read varianty (24-31) ctou data pres Sin.
  const ALU_OPS: seq<AluInp> := [
    AluInp(true, RS_A, false, RS_NULL, OP_ADD),
    AluInp(true, RS_A, false, RS_NULL, OP_SUB),
    AluInp(false, RS_NULL, true, RS_B, OP_ADD),
    AluInp(false, RS_NULL, true, RS_B, OP_SUB),
    AluInp(true, RS_C, false, RS_NULL, OP_ADD),
    AluInp(true, RS_C, false, RS_NULL, OP_SUB),
    AluInp(false, RS_NULL, true, RS_D, OP_ADD),
    AluInp(false, RS_NULL, true, RS_D, OP_SUB),
    AluInp(true, RS_A, false, RS_NULL, OP_SHL),
    AluInp(true, RS_A, false, RS_NULL, OP_SHR),
    AluInp(false, RS_NULL, true, RS_B, OP_SHL),
    AluInp(false, RS_NULL, true, RS_B, OP_SHR),
    AluInp(true, RS_C, false, RS_NULL, OP_SHL),
    AluInp(true, RS_C, false, RS_NULL, OP_SHR),
    AluInp(false, RS_NULL, true, RS_D, OP_SHL),
    AluInp(false, RS_NULL, true, RS_D, OP_SHR),
    AluInp(true, RS_A, true, RS_B, OP_ADD),
    AluInp(true, RS_A, true, RS_B, OP_SUB),
    AluInp(true, RS_C, true, RS_B, OP_ADD),
    AluInp(true, RS_C, true, RS_B, OP_SUB),
    AluInp(true, RS_C, true, RS_D, OP_ADD),
    AluInp(true, RS_C, true, RS_D, OP_SUB),
    AluInp(true, RS_A, true, RS_D, OP_ADD),
    AluInp(true, RS_A, true, RS_D, OP_SUB),
    AluInp(true, RS_A, false, RS_NULL, OP_ADD),  // 24: io read (const)
    AluInp(true, RS_A, false, RS_NULL, OP_SUB),  // 25: io read
    AluInp(false, RS_NULL, false, RS_NULL, OP_ADD),  // 26: io read (SCOM/RAM)
    AluInp(false, RS_NULL, false, RS_NULL, OP_SUB),
    AluInp(true, RS_C, false, RS_NULL, OP_ADD),  // 28: io read
    AluInp(true, RS_C, false, RS_NULL, OP_SUB),
    AluInp(false, RS_NULL, false, RS_NULL, OP_ADD),  // 30: io read ??
    AluInp(false, RS_NULL, false, RS_NULL, OP_SUB)
  ]

  datatype DstSel = DS_A | DS_IO | DS_XCH_AB | DS_B | DS_C | DS_XCH_CD | DS_D | DS_XCH_AE

  // ALU_DST[8] [alu.c:571-580]
  const ALU_DSTS: seq<DstSel> := [
    DS_A, DS_IO, DS_XCH_AB, DS_B, DS_C, DS_XCH_CD, DS_D, DS_XCH_AE
  ]

  function SelReg(c: Cpu, s: RegSel): Reg
  {
    match s
    case RS_NULL => ZeroReg()
    case RS_A => c.A
    case RS_B => c.B
    case RS_C => c.C
    case RS_D => c.D
  }

  function DstHasReg(d: DstSel): bool
  {
    d == DS_A || d == DS_B || d == DS_C || d == DS_D
  }

  function DstReg(c: Cpu, d: DstSel): Reg
    requires DstHasReg(d)
  {
    match d
    case DS_A => c.A
    case DS_B => c.B
    case DS_C => c.C
    case DS_D => c.D
  }

  function SetDstReg(c: Cpu, d: DstSel, v: Reg): Cpu
    requires DstHasReg(d)
  {
    match d
    case DS_A => c.(A := v)
    case DS_B => c.(B := v)
    case DS_C => c.(C := v)
    case DS_D => c.(D := v)
  }

  // ============================================================
  // run_early() [alu.c:689-726]. Vraci (vykonej_brzo, nastav_IO_VALID).
  // IO.ALL (dst=IO, maska=ALL) zapisuje na io sbernici brzo (S0W) -
  // SR51 unit-conversion workaround, viz komentar [alu.c:709-719].
  // ============================================================
  function RunEarly(opcode: u16): (bool, bool)
  {
    var g := (opcode & (0x1F00 as u16)) as int;
    if g == 0x0000 then (true, false)        // flags
    else if g == 0x0800 then (true, false)   // keyboard (HOLD/COND)
    else if g == 0x0A00 then                 // wait
      var low := (opcode & (0x000F as u16)) as int;
      if low == 0x0 then (true, false)       // wait digit (HOLD)
      else if low == 0x3 || low == 0xB then (true, false) // busy (COND)
      else if low == 0xC then (false, false) // EXTKR needs EXT
      else (true, false)
    else
      // alu: jen IO.ALL zapis (dst==IO(1) && maska==ALL(1)) [alu.c:720]
      if (opcode & (0x0007 as u16)) == (0x0001 as u16)
          && ((opcode >> 8) & (0x000F as u16)) == (0x0001 as u16)
      then (true, true)
      else (false, false)
  }

  // ============================================================
  // execute() [alu.c:187-643]
  // ============================================================

  // flag operations [alu.c:211-328]
  function ExecFlagOps(c: Cpu, opcode: u16): Cpu
    requires (opcode & (0x0F00 as u16)) == (0 as u16)
  {
    var bit := ((opcode >> 4) & (0x000F as u16)) as nat;  // 0..15
    var msk := Bit16(bit);
    var f := (opcode & (0x000F as u16)) as int;
    var c0 := c;
    var c1 :=
      if f == 0x0 then (if Flg(c0.fA, msk) then c0.(flags := c0.flags & ((0xFFFF as u16) ^ FLG_COND)) else c0) // TST fA
      else if f == 0x1 then c0.(fA := c0.fA | msk)   // SET fA
      else if f == 0x2 then c0.(fA := c0.fA & ((0xFFFF as u16) ^ msk))  // ZERO fA
      else if f == 0x3 then c0.(fA := c0.fA ^ msk)   // INV fA
      else if f == 0x4 then                          // XCH fA,fB [alu.c:243-251]
        (if (((c0.fA ^ c0.fB) & msk)) != (0 as u16)
         then c0.(fA := c0.fA ^ msk, fB := c0.fB ^ msk) else c0)
      else if f == 0x5 then c0.(KR := c0.KR | msk)   // SET KR
      else if f == 0x6 then                          // MOV fA,fB (B->A)
        (if (((c0.fA ^ c0.fB) & msk)) != (0 as u16) then c0.(fA := c0.fA ^ msk) else c0)
      else if f == 0x7 then                          // R5->fA[1..4] [alu.c:265-270]
        c0.(fA := (c0.fA & (0xFFE1 as u16)) | (((c0.R5 & (0x0F as u8)) as u16) << 1))
      else if f == 0x8 then (if Flg(c0.fB, msk) then c0.(flags := c0.flags & ((0xFFFF as u16) ^ FLG_COND)) else c0) // TST fB
      else if f == 0x9 then c0.(fB := c0.fB | msk)
      else if f == 0xA then c0.(fB := c0.fB & ((0xFFFF as u16) ^ msk))
      else if f == 0xB then c0.(fB := c0.fB ^ msk)
      else if f == 0xC then                          // CMP fA,fB [alu.c:298-306]
        (if (((c0.fA ^ c0.fB) & msk)) == (0 as u16) then c0.(flags := c0.flags & ((0xFFFF as u16) ^ FLG_COND)) else c0)
      else if f == 0xD then c0.(KR := c0.KR & ((0xFFFF as u16) ^ msk))  // ZERO KR
      else if f == 0xE then                          // MOV fB,fA (A->B)
        (if (((c0.fA ^ c0.fB) & msk)) != (0 as u16) then c0.(fB := c0.fB ^ msk) else c0)
      else c0.(fB := (c0.fB & (0xFFE1 as u16)) | (((c0.R5 & (0x0F as u8)) as u16) << 1)); // R5->fB
    c1
  }

  // count-trailing-zeros [alu.c:354-357] - index nejnizsi nastaveneho bitu
  // (x != 0), 8-vetveny if-chain: zadna rekurze, trivialni mez < 8.
  function Ctz8(x: u8): nat
    requires x != (0 as u8)
    ensures Ctz8(x) < 8
  {
    if (x & Bit8(0)) != (0 as u8) then 0
    else if (x & Bit8(1)) != (0 as u8) then 1
    else if (x & Bit8(2)) != (0 as u8) then 2
    else if (x & Bit8(3)) != (0 as u8) then 3
    else if (x & Bit8(4)) != (0 as u8) then 4
    else if (x & Bit8(5)) != (0 as u8) then 5
    else if (x & Bit8(6)) != (0 as u8) then 6
    else 7
  }

  // keyboard operations [alu.c:332-381]
  function ExecKeyb(c: Cpu, opcode: u16, digit: nat, keyLine: u8): Cpu
    requires digit <= 15
    requires (opcode & (0x0F00 as u16)) == (0x0800 as u16)
  {
    // [alu.c:340] mask = (((opcode & 0x07) | ((opcode >> 1) & 0x78)) ^ 0x7F) & key
    var km0 := (((opcode & (0x0007 as u16)) | ((opcode >> 1) & (0x0078 as u16))) ^ (0x007F as u16)) as u8;
    var km1 := km0 & keyLine;
    // [alu.c:343-345] vice jak 1 klavesa -> 0
    var km := if (km1 & (km1 - (1 as u8))) != (0 as u8) then (0 as u8) else km1;
    if (opcode & (0x0008 as u16)) == (0 as u16) then
      // scan all [alu.c:346-370]
      if (keyLine & km) != (0 as u8) then
        var b := Ctz8(km);
        // [alu.c:361] KR = (digit << 4) | ((bit << 8) & 0x0700) - plny prepis
        c.(KR := (((digit as u16) << 4) | (((b as u16) << 8) & (0x0700 as u16))),
           flags := c.flags & ((0xFFFF as u16) ^ FLG_COND))
      else if digit != 15 then
        c.(flags := c.flags | FLG_HOLD)   // [alu.c:365-369] wait for digit 15
      else c
    else
      // scan row [alu.c:371-379]
      if (keyLine & km) != (0 as u8)
      then c.(flags := c.flags & ((0xFFFF as u16) ^ FLG_COND))
      else c
  }

  // wait operations [alu.c:385-521]
  function ExecWait(c: Cpu, opcode: u16, digit: nat, ext: u16, keyLine: u8): Cpu
    requires digit <= 15
    requires (opcode & (0x0F00 as u16)) == (0x0A00 as u16)
  {
    var f := (opcode & (0x000F as u16)) as int;
    var arg := ((opcode >> 4) & (0x000F as u16)) as nat;  // 0..15
    var c0 := c;
    var c1 :=
      if f == 0x0 then                               // wait for digit [alu.c:387-395]
        (if digit != arg then c0.(flags := c0.flags | FLG_HOLD) else c0)
      else if f == 0x1 then c0.(flags := c0.flags & (0xFFFE as u16))  // zero idle
      else if f == 0x2 then c0.(fA := 0)                        // CLFA
      else if f == 0x3 then c0  // Wait Busy: #warning "Unknown behaviour..." [alu.c:408-411] - no-op
      else if f == 0x4 then                          // INCKR [alu.c:412-418]
        var kr := c0.KR + (0x0010 as u16);
        if (kr & (0xFFF0 as u16)) == (0 as u16) then c0.(KR := kr ^ (0x0001 as u16)) else c0.(KR := kr)
      else if f == 0x5 then                          // TKR [alu.c:419-428]
        (if Flg(c0.KR, Bit16(arg)) then c0.(flags := c0.flags & ((0xFFFF as u16) ^ FLG_COND)) else c0)
      else if f == 0x6 then                          // FLGR5 [alu.c:429-447]
        var sel := ((opcode & (0x00F0 as u16)) as int);
        if sel == 0x10 then c0.(R5 := ((c0.fB >> 1) & (0x000F as u16)) as u8)
        else if sel == 0x00 then c0.(R5 := ((c0.fA >> 1) & (0x000F as u16)) as u8)
        else c0
      else if f == 0x7 then c0.(R5 := ((opcode >> 4) & (0x000F as u16)) as u8)  // Number
      else if f == 0x8 then                          // KRR5/R5KR [alu.c:454-469]
        var sel := ((opcode & (0x00F0 as u16)) as int);
        if sel == 0x00 then c0.(R5 := ((c0.KR >> 4) & (0x000F as u16)) as u8)
        else if sel == 0x10 then c0.(KR := (c0.KR & (0xFF0F as u16)) | (((c0.R5 as u16) << 4)))
        else c0
      else if f == 0x9 then c0.(flags := c0.flags | FLG_IDLE)   // set idle
      else if f == 0xA then c0.(fB := 0)                        // CLFB
      else if f == 0xB then                          // TST BUSY [alu.c:483-489]
        // KR_BIT = 4 [bus.h:57]
        (if ((keyLine & Bit8(4)) != (0 as u8)) || Flg(c0.flags, FLG_BUSY)
         then c0.(flags := c0.flags & ((0xFFFF as u16) ^ (FLG_COND | FLG_BUSY))) else c0)
      else if f == 0xC then                          // EXTKR [alu.c:490-497]
        // XXX KR[0] set ???? - aktivni varianta [alu.c:494]
        c0.(KR := ((ext << 1) & (0xFFF0 as u16)) | (ext >> 15))
      else if f == 0xD then                          // XKRSR [alu.c:498-508]
        c0.(KR := c0.SR, SR := c0.KR)
      else c0;  // 0xE/0xF: prazdny switch v C -> no-op (LIB/STO/RCL resi jine chipy)
    c1
  }

  // R5->Adder special [alu.c:589-603]: dst[start]=R5, dst[cpos]=cval,
  // dst[start+1..end]=0 (cislice mimo rozsah masky zustavaji!), pak BCD
  // korekce druhym Alu volanim.
  function RegSpecial(m: Mask, dst: Reg, r5: u8): Reg
    requires m.start >= 0 && m.start <= 15 && m.start <= m.end + 1 && m.end <= 15
  {
    RegSpecialFrom(m.start, dst, m, r5)
  }

  function RegSpecialFrom(i: nat, dst: Reg, m: Mask, r5: u8): Reg
    requires m.start >= 0 && m.start <= 15 && m.end <= 15 && i <= m.end + 1
    decreases m.end + 1 - i
  {
    if i > m.end then dst
    else
      var d := if i == m.start then r5
               else if i == m.cpos then m.cval
               else (0 as u8);
      RegSpecialFrom(i + 1, dst[i := d], m, r5)
  }

  // ALU operations [alu.c:524-639]
  function ExecAlu(c: Cpu, opcode: u16): Cpu
    requires (opcode & (0x0F00 as u16)) != (0 as u16)
    requires (opcode & (0x0F00 as u16)) != (0x0800 as u16)
    requires (opcode & (0x0F00 as u16)) != (0x0A00 as u16)
  {
    var mIdx := ((opcode >> 8) & (0x000F as u16)) as nat;
    var m := MaskOf(mIdx);
    var inp := ALU_OPS[((opcode >> 3) & (0x001F as u16)) as nat];
    var dsIdx := (opcode & (0x0007 as u16)) as nat;
    var dsel := ALU_DSTS[dsIdx];
    var ioValid := Flg(c.flags, FLG_IO_VALID);
    var lowF8 := (opcode & (0x00F8 as u16)) as int;
    // [alu.c:583-604] generic / R5->Adder special
    var res :=
      if lowF8 == 0xF0 || lowF8 == 0xF8 then
        if DstHasReg(dsel) then
          // special: napln dst z R5 + masky, pak BCD korekce [alu.c:591-602]
          var dst0 := RegSpecial(m, DstRegOrZero(c, dsel), c.R5);
          AluExec(true, dst0, false, ZeroReg(), true, dst0, m,
                  if (opcode & (0x0008 as u16)) == (0 as u16) then OP_ADD else OP_SUB,
                  c.Sin, ioValid, c.R5, c.Sout)
        else
          AluExec(false, ZeroReg(), inp.hasX, SelReg(c, inp.x),
                 inp.hasY, SelReg(c, inp.y), m, inp.op,
                 c.Sin, ioValid, c.R5, c.Sout)
      else
        AluExec(DstHasReg(dsel), DstRegOrZero(c, dsel), inp.hasX, SelReg(c, inp.x),
                inp.hasY, SelReg(c, inp.y), m, inp.op,
                c.Sin, ioValid, c.R5, c.Sout);
    var c1 := c.(Sout := res.Sout, R5 := res.R5);
    var c2 := if DstHasReg(dsel) then SetDstReg(c1, dsel, res.dst) else c1;
    var c3 := if res.condClear then c2.(flags := c2.flags & ((0xFFFF as u16) ^ FLG_COND)) else c2;
    // EXCHANGE [alu.c:606-631] - pro ALU skupinu vzdy m.start >= 0
    var c4 :=
      if dsIdx == 2 then (var p := XchExec(c3.A, c3.B, m); c3.(A := p.0, B := p.1))
      else if dsIdx == 5 then (var p := XchExec(c3.C, c3.D, m); c3.(C := p.0, D := p.1))
      else if dsIdx == 7 then (var p := XchExec(c3.A, c3.E, m); c3.(A := p.0, E := p.1))
      else c3;
    c4
  }

  function DstRegOrZero(c: Cpu, d: DstSel): Reg
  {
    if DstHasReg(d) then DstReg(c, d) else ZeroReg()
  }

  // ============================================================
  // execute() [alu.c:187-643] - vstupni bod.
  // digit/ext/keyLine = cache sbenrnice z alu_process [alu.c:781-783].
  // ============================================================
  function Execute(c: Cpu, opcode: u16, digit: nat, ext: u16, keyLine: u8): Cpu
    requires digit <= 15
  {
    // [alu.c:195-201] jump
    if (opcode & (0x1000 as u16)) != (0 as u16) then
      c.(flags := c.flags | FLG_JUMP)
    else
      // [alu.c:202-206] COND se obnovi po posledni serii skoku
      var c0 := if Flg(c.flags, FLG_JUMP)
                then c.(flags := (c.flags & ((0xFFFF as u16) ^ FLG_JUMP)) | FLG_COND)
                else c;
      var g := (opcode & (0x0F00 as u16)) as int;
      if g == 0x0000 then ExecFlagOps(c0, opcode)
      else if g == 0x0800 then ExecKeyb(c0, opcode, digit, keyLine)
      else if g == 0x0A00 then ExecWait(c0, opcode, digit, ext, keyLine)
      else ExecAlu(c0, opcode)
  }

  // ============================================================
  // alu_gen_digit() [alu.c:728-777] - dekod segmentu displeje v idle.
  // Vraci (znak, dpt, segH, zeroSuppr').
  // ============================================================
  function GenDigitIdle(c: Cpu, dstate: nat): (char, bool, bool, bool)
    requires dstate <= 15
  {
    var i := dstate;
    // [alu.c:733-740] zero suppression
    var zs1 := if i == 15 then true else c.zeroSuppr;
    var zs2 := if i == 3 || ((c.R5 == (i as u8)) && i != 15)
                    || ((c.B[i] as int) >= 8) then false else zs1;
    var zs3 := if i == 2 then true else zs2;
    // [alu.c:741-756] znak podle B[i]/A[i]
    var ch: char :=
      if c.B[i] == 7 || c.B[i] == 3
           || (((c.B[i] as int) <= 4) && zs3 && c.A[i] == (0 as u8)) then ' '
      else if c.B[i] == 6 || (c.B[i] == 5 && c.A[i] == (0 as u8)) then '-'
      else if c.B[i] == 5 then 'o'
      else if c.B[i] == 4 then '\''
      else if c.B[3] == 2 then '"'   // [alu.c:749-750] XXX: B[3] misto B[i] - verno
      else ('0' as int + (c.A[i] as int)) as char;   // '0' + A[i]
    // [alu.c:753-756] zero_suppr se cisti nenulovou cislici
    var zs4 :=
      if !(c.B[i] == 7 || c.B[i] == 3 || ((c.B[i] as int) <= 4 && zs3 && c.A[i] == (0 as u8))
           || c.B[i] == 6 || (c.B[i] == 5 && c.A[i] == (0 as u8)) || c.B[i] == 5
           || c.B[i] == 4 || c.B[3] == 2) && c.A[i] != (0 as u8)
      then false else zs3;
    var dpt := c.R5 == (i as u8);              // [alu.c:757-760]
    // [alu.c:761-764] fA bit i+1 -> segH; D15: 1<<16 & 16bit fA == 0
    var segH := if i < 15 then Flg(c.fA, Bit16(i + 1)) else false;
    (ch, dpt, segH, zs4)
  }
}