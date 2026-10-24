module TI59_Machine {
  import opened TI59_Nibbles
  import opened TI59_AluOps
  import opened TI59_Cpu
  import opened TI59_Bus
  import opened TI59_Chips

  // ============================================================
  // Celny stroj: CPU + 3x brom + 2x scom + 4x ram + lib + key +
  // display + sbernice. Fold pres 16 sstavu S0..S15 x faze W/R
  // [vbus.c run()] v presnem poradi chipu [vbus.c main()].
  // ============================================================

    // Option type for the trace record (Dafny does not have a built-in one)
  datatype OptRec = NoRec | YesRec(rec: TraceRec)

  datatype Machine = Machine(cpu: Cpu, broms: seq<Brom>, scoms: seq<Scom>,
                             rams: seq<Ram>, lib: Lib, keys: KeyState,
                             disp: Disp, bus: Bus, halted: bool)

  // Strukturalni invarianty (data chipu se meni jen uvnitr, rozsahy ne)
  predicate MachineWF(m: Machine) {
    |m.broms| == 3 && |m.scoms| == 2 && |m.rams| == 4 &&
    (forall i | 0 <= i < 3 ::
       var b := m.broms[i];
       0 <= b.start && |b.data| == b.end - b.start) &&
    (forall i | 0 <= i < 2 ::
       var s := m.scoms[i];
       |s.CONST| == 32 && |s.SCOM| == 8 &&
       0 <= s.startConst && s.endConst == s.startConst + 32 &&
       0 <= s.startReg && s.endReg == s.startReg + 8) &&
    (forall i | 0 <= i < 4 :: |m.rams[i].data| == 30) &&
    |m.lib.data| == 5000 &&
    |m.keys.persKeys| == 16 &&
    |m.disp.out| == 30 && |m.disp.out1| == 30 &&
    m.bus.dstate <= 15
  }

  // ------------------------------------------------------------
  // alu_process() [alu.c:779-890] - CPU chip (chip 0, prvni v poradi).
  // ------------------------------------------------------------
  function AluChip(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(AluChip(m, sstate, write))
  {
    var c := m.cpu;
    var bus := m.bus;
    // [alu.c:781-784] cache refresh + IO_VALID clear (kazde volani)
    var c1 := c.(flags := c.flags & (0xFBFF as u16));
    if c1.reset != 0 then
      // [alu.c:786-796] reset sekvence; post-dekrement jen na S0W
      var reset1: nat := if sstate == 0 && write then c1.reset - 1 else c1.reset;
      var bus1 := if sstate == 0 && write && c1.reset > 1
                  then bus.(ext := (1 as u16)) else bus;
      var captured := sstate == 15 && !write && reset1 == 1;
      var c2 := if captured
                then c1.(reset := reset1, opcode := bus1.irg, addr := bus1.addr)
                else c1.(reset := reset1);
      // [alu.c:793] addr == -1 -> simulator konci (return 1);
      // v resetu se bus->idle NEaktualizuje (early return)
      m.(cpu := c2, bus := bus1,
         halted := m.halted || (captured && bus1.addr == -1))
    else
      // ---------------- normal cesta [alu.c:798+] ----------------
      if sstate == 0 && write then
        var c2 := c1.(flags := c1.flags & (0xFFFD as u16));  // &= ~HOLD
        var c3 := c2.(Sin := ZeroReg(), Sout := ZeroReg());
        var c4 := if Flg(c3.flags, FLG_COND)
                  then c3.(flags := c3.flags | FLG_COND_LAST) else c3;
        // [alu.c:808] alu_gen_digit - display decode
        var gd := GenDigitIdle(c4, bus.dstate);
        var c5 := if Flg(c4.flags, FLG_IDLE) then c4.(zeroSuppr := gd.3) else c4;
        var bus1 := if Flg(c4.flags, FLG_IDLE)
                    then bus.(displayDigit := gd.0, displayDpt := gd.1,
                              displaySegH := gd.2)
                    else bus.(displaySegH := true);  // [alu.c:773-775]
        // [alu.c:820-825] run_early + execute + io write
        var re := RunEarly(c5.opcode);
        var c6 := if re.1 then c5.(flags := c5.flags | FLG_IO_VALID) else c5;
        var c7 := if re.0
                  then Execute(c6, c6.opcode, bus.dstate, bus1.ext, bus1.keyLine)
                  else c6;
        var bus2 := if re.0 && Flg(c7.flags, FLG_IO_VALID)
                    then bus1.(io := c7.Sout) else bus1;
        // [alu.c:836-837] KR -> ext (rotace o 1 vlevo, chybi COND/HOLD bity)
        var bus3 := if c7.opcode != (0x0A0C as u16)
                    then bus2.(ext := ((c7.KR >> 1) | (c7.KR << 15)) & (0xFFF9 as u16))
                    else bus2;
        m.(cpu := c7, bus := bus3.(idle := Flg(c7.flags, FLG_IDLE)))
      else if sstate == 1 && write then
        // [alu.c:845-848] COND_LAST -> EXT_COND
        var bus1 := if Flg(c1.flags, FLG_COND_LAST)
                    then bus.(ext := bus.ext | EXT_COND) else bus;
        var c2 := if Flg(c1.flags, FLG_COND_LAST)
                  then c1.(flags := c1.flags & (0xEFFF as u16)) else c1;
        m.(cpu := c2, bus := bus1.(idle := Flg(c2.flags, FLG_IDLE)))
      else if sstate == 2 && write then
        // [alu.c:850-854] HOLD -> EXT_HOLD
        var bus1 := if Flg(c1.flags, FLG_HOLD)
                    then bus.(ext := bus.ext | EXT_HOLD) else bus;
        m.(cpu := c1, bus := bus1.(idle := Flg(c1.flags, FLG_IDLE)))
      else if sstate == 2 && !write then
        // [alu.c:855-860] KR[1] se maze, kdyz bus nedrzí (nHOLD)
        var c2 := if (bus.ext & EXT_HOLD) == 0
                  then c1.(KR := c1.KR & (0xFFFD as u16)) else c1;
        m.(cpu := c2, bus := bus.(idle := Flg(c2.flags, FLG_IDLE)))
      else if sstate == 14 && write then
        // [alu.c:861-866] clear display pred prepnutim D linky
        m.(cpu := c1,
           bus := bus.(displayDpt := false, displaySegH := false,
                       displayDigit := ' ', idle := Flg(c1.flags, FLG_IDLE)))
      else if sstate == 15 && !write then
        // [alu.c:870-884] late execute + fetch dalsi instrukce
        var re := RunEarly(c1.opcode);
        var c2 := if !re.0
                  then Execute(c1.(Sin := bus.io), c1.opcode, bus.dstate,
                               bus.ext, bus.keyLine)
                  else c1;
        var c3 := c2.(opcode := bus.irg, addr := bus.addr);
        m.(cpu := c3, bus := bus.(idle := Flg(c3.flags, FLG_IDLE)),
           halted := m.halted || bus.addr == -1)
      else
        m.(cpu := c1, bus := bus.(idle := Flg(c1.flags, FLG_IDLE)))
  }

  // ------------------------------------------------------------
  // Fold pres chipy v presnem poradi [vbus.c main()]:
  // alu, brom x3, scom x2, ram x4, lib, aux (no-op, vynechan),
  // display, key.
  // ------------------------------------------------------------
  function BromsFold(m: Machine, i: nat, sstate: nat, write: bool): Machine
    requires MachineWF(m) && i <= 3 && sstate <= 15
    ensures MachineWF(BromsFold(m, i, sstate, write))
    decreases 3 - i
  {
    if i == 3 then m
    else
      var (b, bus1) :=
        if write then BromStep(m.broms[i], m.bus, sstate, true)
        else BromStepRead(m.broms[i], m.bus, sstate, false);
      BromsFold(m.(broms := m.broms[i := b], bus := bus1),
                i + 1, sstate, write)
  }

  function ScomsFold(m: Machine, i: nat, sstate: nat, write: bool): Machine
    requires MachineWF(m) && i <= 2 && sstate <= 15
    ensures MachineWF(ScomsFold(m, i, sstate, write))
    decreases 2 - i
  {
    if i == 2 then m
    else
      var s := m.scoms[i];
      var (s1, bus1) :=
        if write then
          var (sa, busA) := ScomConstStepW(s, m.bus, sstate, true);
          var (sb, busB) := ScomRegStepW(sa, busA, sstate, true);
          (sb, busB)
        else
          var (sa, busA) := ScomConstStepR(s, m.bus, sstate, false);
          var (sb, busB) := ScomRegStepR(sa, busA, sstate, false);
          (sb, busB);
      ScomsFold(m.(scoms := m.scoms[i := s1], bus := bus1),
                i + 1, sstate, write)
  }

  function RamsFold(m: Machine, i: nat, sstate: nat, write: bool): Machine
    requires MachineWF(m) && i <= 4 && sstate <= 15
    ensures MachineWF(RamsFold(m, i, sstate, write))
    decreases 4 - i
  {
    if i == 4 then m
    else
      var (r, bus1) :=
        if write then RamStepW(m.rams[i], m.bus, sstate, true)
        else RamStepR(m.rams[i], m.bus, sstate, false);
      RamsFold(m.(rams := m.rams[i := r], bus := bus1),
               i + 1, sstate, write)
  }

  function LibChip(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(LibChip(m, sstate, write))
  {
    var (l, bus1) :=
      if write then LibStepW(m.lib, m.bus, sstate, true)
      else LibStepR(m.lib, m.bus, sstate, false);
    m.(lib := l, bus := bus1)
  }

  function DispChip(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(DispChip(m, sstate, write))
  {
    var (d, bus1) := DispStep(m.disp, m.bus, sstate, write);
    m.(disp := d, bus := bus1)
  }

  function KeyChip(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(KeyChip(m, sstate, write))
  {
    var (k, bus1) := KeyStep(m.keys, m.bus, sstate, write);
    m.(keys := k, bus := bus1)
  }

  // Jedna faze (write nebo read) - vsechny chipy v poradi [vbus.c:53-67]
  function PhaseFold(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(PhaseFold(m, sstate, write))
  {
    var m1 := AluChip(m, sstate, write);
    var m2 := BromsFold(m1, 0, sstate, write);
    var m3 := ScomsFold(m2, 0, sstate, write);
    var m4 := RamsFold(m3, 0, sstate, write);
    var m5 := LibChip(m4, sstate, write);
    var m6 := DispChip(m5, sstate, write);
    KeyChip(m6, sstate, write)
  }

  // Jeden sstav = write faze + read faze; po S14 update dstate/key_line
  // [vbus.c:68-75]
  function SStateFold(m: Machine, s: nat): Machine
    requires MachineWF(m) && s <= 15
    ensures MachineWF(SStateFold(m, s))
  {
    var m1 := PhaseFold(m, s, true);
    var m2 := PhaseFold(m1, s, false);
    if s == 14 then m2.(bus := BusAfterS14(m2.bus)) else m2
  }

  // Jeden instrukcni cyklus = 16 sstavu [vbus.c:50]
  function CycleFold(m: Machine, s: nat): Machine
    requires MachineWF(m) && s <= 16
    ensures MachineWF(CycleFold(m, s))
    decreases 16 - s
  {
    if s == 16 then m
    else CycleFold(SStateFold(m, s), s + 1)
  }

  // ------------------------------------------------------------
  // Trasovaci zaznam - stejna pole jako LOG_HRAST [alu.c:663-681].
  // Zapis se dela na zacatku S0W (debug() pred vycistenim HOLD) -
  // = stav stroje na zacatku cyklu. EXT je v S0W vzdy 0.
  // ------------------------------------------------------------
  datatype TraceRec = TraceRec(addr: int, opcode: u16,
                               A: Reg, B: Reg, C: Reg, D: Reg, E: Reg,
                               fA: u16, fB: u16, KR: u16, SR: u16, R5: u8,
                               ext: u16, cond: bool, idle: bool, hold: bool,
                               Sin: Reg, Sout: Reg, dstate: nat)

  function MkTrace(m: Machine): TraceRec
    requires MachineWF(m)
  {
    TraceRec(m.cpu.addr, m.cpu.opcode,
             m.cpu.A, m.cpu.B, m.cpu.C, m.cpu.D, m.cpu.E,
             m.cpu.fA, m.cpu.fB, m.cpu.KR, m.cpu.SR, m.cpu.R5,
             (0) as u16,  // EXT=%02X tiskne (cpu.EXT>>4)&0xFF, v S0W je ext=0
             Flg(m.cpu.flags, FLG_COND), Flg(m.cpu.flags, FLG_IDLE),
             Flg(m.cpu.flags, FLG_HOLD),
             m.cpu.Sin, m.cpu.Sout, m.bus.dstate)
  }

  // Jedna instrukce = jeden cyklus S0..S15 + trace zaznam.
  // Za haltu (addr==-1) je stroj "zamceny" a vraci se beze zmeny.
  function StepInstruction(m: Machine): (Machine, OptRec)
    requires MachineWF(m)
    ensures MachineWF(StepInstruction(m).0)
  {
    if m.halted then (m, NoRec)
    else
      var rec := if m.cpu.reset == 0 then YesRec(MkTrace(m)) else NoRec;
      var m1 := m.(bus := BusCycleStart(m.bus));
      var m2 := CycleFold(m1, 0);
      (m2, rec)
  }

  // ------------------------------------------------------------
  // Beh s rozpoctem (deterministicke; -n limit resi runner).
  // ------------------------------------------------------------
  function Run(m: Machine, budget: nat): Machine
    requires MachineWF(m)
    ensures MachineWF(Run(m, budget))
    decreases budget
  {
    if budget == 0 || m.halted then m
    else Run(StepInstruction(m).0, budget - 1)
  }
}