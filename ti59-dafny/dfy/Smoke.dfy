include "TI59/Nibbles.dfy"
include "TI59/AluOps.dfy"
include "TI59/Cpu.dfy"
include "TI59/Bus.dfy"
include "TI59/Chips.dfy"
include "TI59/Machine.dfy"

module Smoke {
  import opened TI59_Nibbles
  import opened TI59_AluOps
  import opened TI59_Cpu
  import opened TI59_Bus
  import opened TI59_Chips
  import opened TI59_Machine

  method Main() {
    // Mini-ROM: 0: 0A01 (Zero Idle), 1: 0A44 (INCKR), 2: 0001 (SET fA[0]),
    // 3: 0A01 (Zero Idle)
    var rom := [0x0A01 as u16, 0x0A44 as u16, 0x0001 as u16, 0x0A01 as u16];
    var m := Machine(
      CpuInit(),
      [BromInit(rom, 0, 4)],
      [ScomInit(seq(32, _ => ZeroReg()), 0)],
      [RamInit(seq(30, _ => ZeroReg()), 0)],
      LibInit(seq(5000, _ => (0x92) as u8)),
      KeyInit([]),
      DispInit(),
      BusInit(),
      false);
    var m1 := m.(broms := [m.broms[0], m.broms[0], m.broms[0]]);
    var m2 := m1.(scoms := [m1.scoms[0], m1.scoms[0]]);
    var m3 := m2.(rams := [m2.rams[0], m2.rams[0], m2.rams[0], m2.rams[0]]);
    assert |m3.broms| == 3;
    assert |m3.scoms| == 2;
    assert |m3.rams| == 4;
    assert MachineWF(m3);
    // beh 100 instrukci
    var mF := m3;
    var i := 0;
    while i < 100 && !mF.halted
      invariant MachineWF(mF)
      invariant 0 <= i <= 100
    {
      var r := StepInstruction(mF);
      match r.1 {
        case YesRec(rec) =>
          print "instr ", rec.addr, " ", Hex4(rec.opcode), " KR=", rec.KR as int,
                " fA=", rec.fA as int, " cond=", rec.cond, "\n";
        case NoRec =>
      }
      mF := r.0;
      i := i + 1;
    }
    print "halted=", mF.halted, " KR=", mF.cpu.KR as int,
          " fA=", mF.cpu.fA as int, " A=", RegInt(mF.cpu.A), "\n";
  }

  function RegInt(r: Reg): int {
    RegVal(r, 0)
  }
  function RegVal(r: Reg, i: nat): int
    requires i <= 16
    decreases 16 - i
  {
    if i == 16 then 0 else (r[i] as int) * Pow10(i) + RegVal(r, i + 1)
  }
  function Pow10(i: nat): int
    requires i <= 16
    decreases i
  { if i == 0 then 1 else 10 * Pow10(i - 1) }
  function Hex4(v: u16): string { Hex4From(v, 4) }
  function Hex4From(v: u16, n: nat): string
    requires n <= 4
    decreases n
  {
    if n == 0 then ""
    else HexDigit((((v >> (if n == 4 then 12 else if n == 3 then 8 else if n == 2 then 4 else 0)) & (0xF as u16)) as int)) +
      Hex4From(v, n - 1)
  }
  function HexDigit(d: int): string
    requires 0 <= d < 16
  {
    if d < 10 then ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"][d]
    else ["A", "B", "C", "D", "E", "F"][d - 10]
  }
}