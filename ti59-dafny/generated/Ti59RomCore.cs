// Dafny program the_program compiled into C#
// To recompile, you will need the libraries
//     System.Runtime.Numerics.dll System.Collections.Immutable.dll
// but the 'dotnet' tool in .NET should pick those up automatically.
// Optionally, you may want to include compiler switches like
//     /debug /nowarn:162,164,168,183,219,436,1717,1718

using System;
using System.Numerics;
using System.Collections;
[assembly: DafnyAssembly.DafnySourceAttribute(@"// dafny 4.11.0.0
// Command-line arguments: translate cs dfy/TI59/Nibbles.dfy dfy/TI59/AluOps.dfy dfy/TI59/Cpu.dfy dfy/TI59/Bus.dfy dfy/TI59/Chips.dfy dfy/TI59/Machine.dfy --no-verify --include-runtime -o generated/Ti59RomCore.cs
// the_program


module TI59_Nibbles {
  function ZeroReg(): Reg
  {
    seq(16, (_ /* _v1 */: int) => 0 as u8)
  }

  function RegUpd(s: Reg, i: nat, v: u8): Reg
    requires i < 16
    decreases s, i, v
  {
    s[i := v]
  }

  predicate BcdValid(s: Reg)
    decreases s
  {
    forall i: int {:trigger s[i]} | 0 <= i < 16 :: 
      s[i] as int < 10
  }

  ghost function ValueOf(s: seq<u8>, n: nat): int
    requires n <= |s|
    decreases n
  {
    if n == 0 then
      0
    else
      ValueOf(s, n - 1) + 10 * s[n - 1] as int * Pow10(n - 1)
  }

  ghost function Pow10(n: nat): nat
    decreases n
  {
    if n == 0 then
      1
    else
      10 * Pow10(n - 1)
  }

  function MaskOf(idx: nat): AluMask
    requires idx < 16
    decreases idx
  {
    if idx == 1 then
      Mask(0, 15, 0, 0)
    else if idx == 2 then
      Mask(0, 0, 0, 0)
    else if idx == 3 then
      Mask(0, 0, 0, 1)
    else if idx == 4 then
      Mask(0, 0, 0, 12)
    else if idx == 5 then
      Mask(3, 3, 3, 1)
    else if idx == 6 then
      Mask(1, 2, 1, 0)
    else if idx == 7 then
      Mask(1, 2, 1, 1)
    else if idx == 9 then
      Mask(3, 15, 3, 0)
    else if idx == 11 then
      Mask(3, 15, 3, 5)
    else if idx == 12 then
      Mask(1, 15, 1, 0)
    else if idx == 13 then
      Mask(1, 15, 3, 1)
    else if idx == 14 then
      Mask(1, 15, 15, 1)
    else if idx == 15 then
      Mask(1, 15, 1, 1)
    else
      Mask(0, 15, 0, 0)
  }

  const FLG_IDLE: u16 := 1 as u16
  const FLG_HOLD: u16 := 2 as u16
  const FLG_JUMP: u16 := 4 as u16
  const FLG_IO_VALID: u16 := 1024 as u16
  const FLG_COND: u16 := 2048 as u16
  const FLG_COND_LAST: u16 := 4096 as u16
  const FLG_BUSY: u16 := 32768 as u16

  function Bit8(b: nat): u8
    requires b < 8
    decreases b
  {
    if b == 0 then
      1 as u8
    else
      Bit8(b - 1) << 1 as bv4
  }

  function Bit16(b: nat): u16
    requires b < 16
    decreases b
  {
    if b == 0 then
      1 as u16
    else
      Bit16(b - 1) << 1 as bv5
  }

  function Nul(): char
  {
    0 as char
  }

  const EXT_PREG: u16 := 1 as u16
  const EXT_COND: u16 := 2 as u16
  const EXT_HOLD: u16 := 4 as u16

  type u8 = bv8

  type u16 = bv16

  type Reg = s: seq<u8>
    | |s| == 16
    witness seq(16, (_ /* _v0 */: int) => 0 as u8)

  datatype Mask = Mask(start: int, end: int, cpos: int, cval: u8)

  type AluMask = m: Mask
    | m.start >= 0 && m.start <= 15 && m.start <= m.end + 1 && m.end <= 15
    witness Mask(0, 15, 0, 0)
}

module TI59_AluOps {
  const MASK_ALL: Mask := Mask(0, 15, 0, 0)

  function IsSubLike(op: AluOp): bool
    decreases op
  {
    op == OP_SUB || op == OP_SHR
  }

  function IsShift(op: AluOp): bool
    decreases op
  {
    op == OP_SHL || op == OP_SHR
  }

  function CarryCorrect(i: nat, sum5: u8, op: AluOp): (u8, u8)
    requires i <= 15
    decreases i, sum5, op
  {
    if i == 0 then
      (if sum5 >= 16 then 1 else 0, if sum5 >= 16 then sum5 & 15 else sum5)
    else
      (if sum5 >= 10 then 1 else 0, if sum5 >= 10 then if !IsSubLike(op) then sum5 - 10 else sum5 + 10 else sum5)
  }

  function DstWrite(i: nat, dst: Reg, shlIn: u8, shr2: u8, sum6: u8, hasDst: bool, op: AluOp, m: Mask): (Reg, u8)
    requires i <= 15
    decreases i, dst, shlIn, shr2, sum6, hasDst, op, m
  {
    if !(m.start <= i && i <= m.end) then
      (dst, shlIn)
    else if !hasDst then
      (dst, shlIn)
    else if op == OP_SHL then
      (RegUpd(dst, i, shlIn), sum6)
    else if op == OP_SHR then
      if i == m.end then
        (RegUpd(if i > m.start && i >= 1 then RegUpd(dst, i - 1, shr2) else dst, i, 0), sum6)
      else if i > m.start && i >= 1 then
        (RegUpd(dst, i - 1, shr2), sum6)
      else
        (dst, sum6)
    else
      (RegUpd(dst, i, sum6), sum6)
  }

  function AluStep(i: nat, carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8, condClear: bool, hasDst: bool, hasX: bool, x: Reg, hasY: bool, y: Reg, m: Mask, op: AluOp, sin: Reg, ioValid: bool): AluAcc
    requires i <= 15
    decreases i, carry, shl, dst, Sout, R5, condClear, hasDst, hasX, x, hasY, y, m, op, sin, ioValid
  {
    var carryIn: u8 := if i == m.start then 0 else carry;
    var shlIn: u8 := if i == m.start then 0 else shl;
    var sum0: u8 := if hasY then y[i] else 0;
    var sum1: u8 := if ioValid then sum0 else sum0 | sin[i];
    var sum2: u8 := if i == m.cpos then sum1 | m.cval else sum1;
    var shr: u8 := sum2;
    var sum3: u8 := sum2 + carryIn;
    var sum4: u8 := if IsSubLike(op) then 0 - sum3 else sum3;
    var sum5: u8 := if hasX then sum4 + x[i] else sum4;
    var shr2: u8 := if hasX then shr | x[i] else shr;
    var Sout1: Reg := RegUpd(Sout, i, sum5 & 15);
    var corr: (u8, u8) := CarryCorrect(i, sum5, op);
    var carryOut: u8 := corr.0;
    var sum6: u8 := corr.1;
    var dw: (Reg, u8) := DstWrite(i, dst, shlIn, shr2, sum6, hasDst, op, m);
    var dst1: Reg := dw.0;
    var shl1: u8 := dw.1;
    var inRange: bool := m.start <= i && i <= m.end;
    var R51: u8 := if inRange && i == m.start then sum6 else R5;
    var cc1: bool := condClear || (inRange && i == m.end && !IsShift(op) && carryOut == 1);
    AluAcc(carryOut, shl1, dst1, Sout1, R51, cc1)
  }

  function AluDigits(i: nat, carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8, condClear: bool, hasDst: bool, hasX: bool, x: Reg, hasY: bool, y: Reg, m: Mask, op: AluOp, sin: Reg, ioValid: bool): AluRes
    requires i <= 16
    decreases 16 - i
  {
    if i == 16 then
      AluRes(dst, Sout, R5, condClear, carry)
    else
      var s: AluAcc := AluStep(i, carry, shl, dst, Sout, R5, condClear, hasDst, hasX, x, hasY, y, m, op, sin, ioValid); AluDigits(i + 1, s.carry, s.shl, s.dst, s.Sout, s.R5, s.cc, hasDst, hasX, x, hasY, y, m, op, sin, ioValid)
  }

  function AluExec(hasDst: bool, dst: Reg, hasX: bool, x: Reg, hasY: bool, y: Reg, m: Mask, op: AluOp, sin: Reg, ioValid: bool, R5: u8, Sout: Reg): AluRes
    decreases hasDst, dst, hasX, x, hasY, y, m, op, sin, ioValid, R5, Sout
  {
    AluDigits(0, 0, 0, dst, Sout, R5, false, hasDst, hasX, x, hasY, y, m, op, sin, ioValid)
  }

  function XchRange(i: int, s1: Reg, s2: Reg, m: Mask): (Reg, Reg)
    requires 0 <= i <= 16
    requires m.start >= 0 && m.end <= 15
    decreases m.end + 1 - i
  {
    if i > m.end then
      (s1, s2)
    else
      XchRange(i + 1, s1[i := s2[i]], s2[i := s1[i]], m)
  }

  function XchExec(s1: Reg, s2: Reg, m: Mask): (Reg, Reg)
    requires m.start >= 0 && m.start <= 16 && m.end <= 15
    decreases s1, s2, m
  {
    XchRange(m.start, s1, s2, m)
  }

  function BcdAddSpec(i: nat, cin: u8, acc: Reg, x: Reg, y: Reg): (Reg, u8)
    requires i <= 16
    requires cin as int <= 1
    requires forall j: int {:trigger y[j]} {:trigger x[j]} | i <= j < 16 :: x[j] as int < 10 && y[j] as int < 10
    decreases 16 - i
  {
    if i == 16 then
      (acc, cin)
    else
      var s: int := x[i] as int + y[i] as int + cin as int; var digit: int := if i == 0 then if s >= 16 then s - 16 else s else if s >= 10 then s - 10 else s; var cout: int := if i == 0 then if s >= 16 then 1 else 0 else if s >= 10 then 1 else 0; BcdAddSpec(i + 1, cout as u8, acc[i := digit as u8], x, y)
  }

  function BcdSubSpec(i: nat, bin: u8, acc: Reg, x: Reg, y: Reg): (Reg, u8)
    requires i <= 16
    requires bin as int <= 1
    requires forall j: int {:trigger y[j]} {:trigger x[j]} | i <= j < 16 :: x[j] as int < 10 && y[j] as int < 10
    decreases 16 - i
  {
    if i == 16 then
      (acc, bin)
    else
      var d: int := x[i] as int - (y[i] as int + bin as int); var digit: int := if d < 0 then if i == 0 then d + 16 else d + 10 else d; var bout: int := if d < 0 then 1 else 0; BcdSubSpec(i + 1, bout as u8, acc[i := digit as u8], x, y)
  }

  lemma L_BvOrZeroB(a: u8, b: u8)
    requires b == 0 as u8
    ensures a | b == a
    decreases a, b
  {
  }

  lemma L_BvAddMod256(a: u8, b: u8)
    ensures (a + b) as int == (a as int + b as int) % 256
    decreases a, b
  {
  }

  lemma L_BvAdd2(a: u8, b: u8)
    requires a as int + b as int < 256
    ensures (a + b) as int == a as int + b as int
    decreases a, b
  {
    L_BvAddMod256(a, b);
  }

  lemma L_BvNegInt(v: u8)
    ensures (0 - v) as int == (256 - v as int) % 256
    decreases v
  {
  }

  lemma L_BvNegAdd(v: u8, x: u8)
    requires v as int <= 10 && x as int <= 9
    ensures (0 - v + x) as int == (x as int - v as int) % 256
    decreases v, x
  {
    L_BvNegInt(v);
    L_BvAddMod256(0 - v, x);
    if x as int < v as int {
      assert (x as int - v as int) % 256 == 256 + x as int - v as int;
    }
  }

  lemma L_AndF(x: u8)
    ensures (x & 15) as int == x as int % 16
    decreases x
  {
  }

  lemma L_CcAdd(i: nat, sum: u8, s: int)
    requires i <= 15
    requires sum as int == s
    requires 0 <= s <= 19
    ensures CarryCorrect(i, sum, OP_ADD).0 as int == if i == 0 then if s >= 16 then 1 else 0 else if s >= 10 then 1 else 0
    ensures CarryCorrect(i, sum, OP_ADD).1 as int == if i == 0 then if s >= 16 then s - 16 else s else if s >= 10 then s - 10 else s
    decreases i, sum, s
  {
    if i == 0 {
      if s >= 16 {
        L_AndF(sum);
        assert 16 <= s <= 19;
        assert s % 16 == s - 16;
      }
    } else {
      if s >= 10 {
        assert (sum - 10 as u8) as int == s - 10;
      }
    }
  }

  lemma L_CcSub(i: nat, sum: u8, d: int)
    requires i <= 15
    requires sum as int == if d < 0 then 256 + d else d
    requires -10 <= d <= 9
    ensures CarryCorrect(i, sum, OP_SUB).0 as int == if d < 0 then 1 else 0
    ensures CarryCorrect(i, sum, OP_SUB).1 as int == if d < 0 then if i == 0 then d + 16 else d + 10 else d
    decreases i, sum, d
  {
    if d < 0 {
      assert 246 <= 256 + d <= 255;
      if i == 0 {
        L_AndF(sum);
        assert (256 + d) % 16 == d + 16;
      } else {
        assert (sum + 10 as u8) as int == d + 10;
      }
    }
  }

  lemma /*{:_inductionTrigger AluDigits(i, carry, shl, dst, Sout, R5, cc, hasDst, true, x, true, y, MASK_ALL, AluOp.OP_ADD, sin, false)}*/ /*{:_induction i, carry, shl, dst, Sout, R5, cc, hasDst, x, y, sin}*/ L_AlusAddBcd(i: nat, carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8, cc: bool, hasDst: bool, x: Reg, y: Reg, sin: Reg)
    requires i <= 16
    requires hasDst
    requires carry as int <= 1
    requires i != 0 || carry == 0 as u8
    requires forall j: int {:trigger sin[j]} {:trigger y[j]} {:trigger x[j]} | i <= j < 16 :: x[j] as int < 10 && y[j] as int < 10 && sin[j] as int == 0
    ensures ghost var a: AluRes := AluDigits(i, carry, shl, dst, Sout, R5, cc, hasDst, true, x, true, y, MASK_ALL, OP_ADD, sin, false); ghost var b: (Reg, u8) := BcdAddSpec(i, carry, dst, x, y); a.dst == b.0 && a.carry == b.1
    decreases 16 - i
  {
    if i < 16 {
      L_BvOrZeroB(y[i], sin[i]);
      ghost var carryIn: u8 := if i == 0 then 0 else carry;
      ghost var sum1: u8 := y[i] | sin[i];
      L_BvOrZeroB(sum1, MASK_ALL.cval);
      ghost var sum3: u8 := sum1 + carryIn;
      ghost var sum5: u8 := sum3 + x[i];
      ghost var s := y[i] as int + carryIn as int + x[i] as int;
      L_BvAdd2(sum1, carryIn);
      L_BvAdd2(sum3, x[i]);
      assert s <= 19;
      L_CcAdd(i, sum5, s);
      ghost var corr := CarryCorrect(i, sum5, OP_ADD);
      ghost var sum6: u8 := corr.1;
      ghost var cout: u8 := corr.0;
      ghost var digitS := if i == 0 then if s >= 16 then s - 16 else s else if s >= 10 then s - 10 else s;
      ghost var coutS := if i == 0 then if s >= 16 then 1 else 0 else if s >= 10 then 1 else 0;
      assert 0 <= digitS <= 15 && 0 <= coutS <= 1;
      assert digitS as u8 == sum6;
      assert coutS as u8 == cout;
      ghost var dst1: Reg := if hasDst then RegUpd(dst, i, sum6) else dst;
      ghost var Sout1 := RegUpd(Sout, i, sum5 & 15);
      ghost var R51: u8 := if i == 0 then sum6 else R5;
      ghost var cc1 := cc || (i == MASK_ALL.end && cout == 1 as u8);
      ghost var bsum2: u8 := if i == MASK_ALL.cpos then y[i] | sin[i] | MASK_ALL.cval else y[i] | sin[i];
      ghost var bcarryIn: u8 := if i == MASK_ALL.start then 0 else carry;
      assert bsum2 == sum1;
      assert bcarryIn == carryIn;
      assert bsum2 + bcarryIn == sum3;
      assert bsum2 + bcarryIn + x[i] == sum5;
      assert MASK_ALL.start >= 0 && i <= 15;
      ghost var st := AluStep(i, carry, shl, dst, Sout, R5, cc, true, true, x, true, y, MASK_ALL, OP_ADD, sin, false);
      assert st.carry == cout;
      assert st.shl == sum6;
      assert st.dst == dst1;
      assert st.Sout == Sout1;
      assert st.R5 == R51;
      assert st.cc == cc1;
      L_AlusAddBcd(i + 1, cout, sum6, dst1, Sout1, R51, cc1, hasDst, x, y, sin);
    }
  }

  lemma /*{:_inductionTrigger AluDigits(i, carry, shl, dst, Sout, R5, cc, hasDst, true, x, true, y, MASK_ALL, AluOp.OP_SUB, sin, false)}*/ /*{:_induction i, carry, shl, dst, Sout, R5, cc, hasDst, x, y, sin}*/ L_AlusSubBcd(i: nat, carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8, cc: bool, hasDst: bool, x: Reg, y: Reg, sin: Reg)
    requires i <= 16
    requires hasDst
    requires carry as int <= 1
    requires i != 0 || carry == 0 as u8
    requires forall j: int {:trigger sin[j]} {:trigger y[j]} {:trigger x[j]} | i <= j < 16 :: x[j] as int < 10 && y[j] as int < 10 && sin[j] as int == 0
    ensures ghost var a: AluRes := AluDigits(i, carry, shl, dst, Sout, R5, cc, hasDst, true, x, true, y, MASK_ALL, OP_SUB, sin, false); ghost var b: (Reg, u8) := BcdSubSpec(i, carry, dst, x, y); a.dst == b.0 && a.carry == b.1
    decreases 16 - i
  {
    if i < 16 {
      L_BvOrZeroB(y[i], sin[i]);
      ghost var carryIn: u8 := if i == 0 then 0 else carry;
      ghost var sum1: u8 := y[i] | sin[i];
      L_BvOrZeroB(sum1, MASK_ALL.cval);
      ghost var sum3: u8 := sum1 + carryIn;
      ghost var sum4: u8 := 0 - sum3;
      ghost var sum5: u8 := sum4 + x[i];
      ghost var d := x[i] as int - (y[i] as int + carryIn as int);
      assert -10 <= d <= 9;
      assert sum3 as int == y[i] as int + carryIn as int;
      L_BvNegAdd(sum3, x[i]);
      assert sum5 as int == if d < 0 then 256 + d else d;
      L_CcSub(i, sum5, d);
      ghost var corr := CarryCorrect(i, sum5, OP_SUB);
      ghost var sum6: u8 := corr.1;
      ghost var cout: u8 := corr.0;
      ghost var digitS := if d < 0 then if i == 0 then d + 16 else d + 10 else d;
      ghost var coutS := if d < 0 then 1 else 0;
      assert 0 <= digitS <= 15 && 0 <= coutS <= 1;
      assert digitS as u8 == sum6;
      assert coutS as u8 == cout;
      ghost var dst1: Reg := if hasDst then RegUpd(dst, i, sum6) else dst;
      ghost var Sout1 := RegUpd(Sout, i, sum5 & 15);
      ghost var R51: u8 := if i == 0 then sum6 else R5;
      ghost var cc1 := cc || (i == MASK_ALL.end && cout == 1 as u8);
      ghost var bsum2: u8 := if i == MASK_ALL.cpos then y[i] | sin[i] | MASK_ALL.cval else y[i] | sin[i];
      ghost var bcarryIn: u8 := if i == MASK_ALL.start then 0 else carry;
      assert bsum2 == sum1;
      assert bcarryIn == carryIn;
      assert bsum2 + bcarryIn == sum3;
      assert 0 - (bsum2 + bcarryIn) + x[i] == sum5;
      assert MASK_ALL.start >= 0 && i <= 15;
      ghost var st := AluStep(i, carry, shl, dst, Sout, R5, cc, true, true, x, true, y, MASK_ALL, OP_SUB, sin, false);
      assert st.carry == cout;
      assert st.shl == sum6;
      assert st.dst == dst1;
      assert st.Sout == Sout1;
      assert st.R5 == R51;
      assert st.cc == cc1;
      L_AlusSubBcd(i + 1, cout, sum6, dst1, Sout1, R51, cc1, hasDst, x, y, sin);
    }
  }

  import opened TI59_Nibbles

  datatype AluOp = OP_ADD | OP_SHL | OP_SUB | OP_SHR

  datatype AluRes = AluRes(dst: Reg, Sout: Reg, R5: u8, condClear: bool, carry: u8)

  datatype AluAcc = AluAcc(carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8, cc: bool)
}

module TI59_Cpu {
  function CpuInit(): Cpu
  {
    Cpu(ZeroReg(), ZeroReg(), ZeroReg(), ZeroReg(), ZeroReg(), 0 as u16, 0 as u16, 0 as u16, 0 as u16, 0 as u8, ZeroReg(), ZeroReg(), 0 as u16, FLG_COND, 0, 5, false)
  }

  function Flg(f: u16, bit: u16): bool
    decreases f, bit
  {
    f & bit != 0 as u16
  }

  const ALU_OPS: seq<AluInp> := [AluInp(true, RS_A, false, RS_NULL, OP_ADD), AluInp(true, RS_A, false, RS_NULL, OP_SUB), AluInp(false, RS_NULL, true, RS_B, OP_ADD), AluInp(false, RS_NULL, true, RS_B, OP_SUB), AluInp(true, RS_C, false, RS_NULL, OP_ADD), AluInp(true, RS_C, false, RS_NULL, OP_SUB), AluInp(false, RS_NULL, true, RS_D, OP_ADD), AluInp(false, RS_NULL, true, RS_D, OP_SUB), AluInp(true, RS_A, false, RS_NULL, OP_SHL), AluInp(true, RS_A, false, RS_NULL, OP_SHR), AluInp(false, RS_NULL, true, RS_B, OP_SHL), AluInp(false, RS_NULL, true, RS_B, OP_SHR), AluInp(true, RS_C, false, RS_NULL, OP_SHL), AluInp(true, RS_C, false, RS_NULL, OP_SHR), AluInp(false, RS_NULL, true, RS_D, OP_SHL), AluInp(false, RS_NULL, true, RS_D, OP_SHR), AluInp(true, RS_A, true, RS_B, OP_ADD), AluInp(true, RS_A, true, RS_B, OP_SUB), AluInp(true, RS_C, true, RS_B, OP_ADD), AluInp(true, RS_C, true, RS_B, OP_SUB), AluInp(true, RS_C, true, RS_D, OP_ADD), AluInp(true, RS_C, true, RS_D, OP_SUB), AluInp(true, RS_A, true, RS_D, OP_ADD), AluInp(true, RS_A, true, RS_D, OP_SUB), AluInp(true, RS_A, false, RS_NULL, OP_ADD), AluInp(true, RS_A, false, RS_NULL, OP_SUB), AluInp(false, RS_NULL, false, RS_NULL, OP_ADD), AluInp(false, RS_NULL, false, RS_NULL, OP_SUB), AluInp(true, RS_C, false, RS_NULL, OP_ADD), AluInp(true, RS_C, false, RS_NULL, OP_SUB), AluInp(false, RS_NULL, false, RS_NULL, OP_ADD), AluInp(false, RS_NULL, false, RS_NULL, OP_SUB)]
  const ALU_DSTS: seq<DstSel> := [DS_A, DS_IO, DS_XCH_AB, DS_B, DS_C, DS_XCH_CD, DS_D, DS_XCH_AE]

  function SelReg(c: Cpu, s: RegSel): Reg
    decreases c, s
  {
    match s
    case RS_NULL() =>
      ZeroReg()
    case RS_A() =>
      c.A
    case RS_B() =>
      c.B
    case RS_C() =>
      c.C
    case RS_D() =>
      c.D
  }

  function DstHasReg(d: DstSel): bool
    decreases d
  {
    d == DS_A || d == DS_B || d == DS_C || d == DS_D
  }

  function DstReg(c: Cpu, d: DstSel): Reg
    requires DstHasReg(d)
    decreases c, d
  {
    match d
    case DS_A() =>
      c.A
    case DS_B() =>
      c.B
    case DS_C() =>
      c.C
    case DS_D() =>
      c.D
  }

  function SetDstReg(c: Cpu, d: DstSel, v: Reg): Cpu
    requires DstHasReg(d)
    decreases c, d, v
  {
    match d
    case DS_A() =>
      c.(A := v)
    case DS_B() =>
      c.(B := v)
    case DS_C() =>
      c.(C := v)
    case DS_D() =>
      c.(D := v)
  }

  function RunEarly(opcode: u16): (bool, bool)
    decreases opcode
  {
    var g: int := (opcode & 7936 as u16) as int;
    if g == 0 then
      (true, false)
    else if g == 2048 then
      (true, false)
    else if g == 2560 then
      var low: int := (opcode & 15 as u16) as int;
      if low == 0 then
        (true, false)
      else if low == 3 || low == 11 then
        (true, false)
      else if low == 12 then
        (false, false)
      else
        (true, false)
    else if opcode & 7 as u16 == 1 as u16 && (opcode >> 8 as bv5) & 15 as u16 == 1 as u16 then
      (true, true)
    else
      (false, false)
  }

  function ExecFlagOps(c: Cpu, opcode: u16): Cpu
    requires opcode & 3840 as u16 == 0 as u16
    decreases c, opcode
  {
    var bit: nat := ((opcode >> 4 as bv5) & 15 as u16) as nat;
    var msk: u16 := Bit16(bit);
    var f: int := (opcode & 15 as u16) as int;
    var c0: Cpu := c;
    var c1: Cpu := if f == 0 then if Flg(c0.fA, msk) then c0.(flags := c0.flags & (65535 as u16 ^ FLG_COND)) else c0 else if f == 1 then c0.(fA := c0.fA | msk) else if f == 2 then c0.(fA := c0.fA & (65535 as u16 ^ msk)) else if f == 3 then c0.(fA := c0.fA ^ msk) else if f == 4 then if (c0.fA ^ c0.fB) & msk != 0 as u16 then c0.(fA := c0.fA ^ msk, fB := c0.fB ^ msk) else c0 else if f == 5 then c0.(KR := c0.KR | msk) else if f == 6 then if (c0.fA ^ c0.fB) & msk != 0 as u16 then c0.(fA := c0.fA ^ msk) else c0 else if f == 7 then c0.(fA := (c0.fA & 65505 as u16) | ((c0.R5 & 15 as u8) as u16 << 1 as bv5)) else if f == 8 then if Flg(c0.fB, msk) then c0.(flags := c0.flags & (65535 as u16 ^ FLG_COND)) else c0 else if f == 9 then c0.(fB := c0.fB | msk) else if f == 10 then c0.(fB := c0.fB & (65535 as u16 ^ msk)) else if f == 11 then c0.(fB := c0.fB ^ msk) else if f == 12 then if (c0.fA ^ c0.fB) & msk == 0 as u16 then c0.(flags := c0.flags & (65535 as u16 ^ FLG_COND)) else c0 else if f == 13 then c0.(KR := c0.KR & (65535 as u16 ^ msk)) else if f == 14 then if (c0.fA ^ c0.fB) & msk != 0 as u16 then c0.(fB := c0.fB ^ msk) else c0 else c0.(fB := (c0.fB & 65505 as u16) | ((c0.R5 & 15 as u8) as u16 << 1 as bv5));
    c1
  }

  function Ctz8(x: u8): nat
    requires x != 0 as u8
    ensures Ctz8(x) < 8
    decreases x
  {
    if x & Bit8(0) != 0 as u8 then
      0
    else if x & Bit8(1) != 0 as u8 then
      1
    else if x & Bit8(2) != 0 as u8 then
      2
    else if x & Bit8(3) != 0 as u8 then
      3
    else if x & Bit8(4) != 0 as u8 then
      4
    else if x & Bit8(5) != 0 as u8 then
      5
    else if x & Bit8(6) != 0 as u8 then
      6
    else
      7
  }

  function ExecKeyb(c: Cpu, opcode: u16, digit: nat, keyLine: u8): Cpu
    requires digit <= 15
    requires opcode & 3840 as u16 == 2048 as u16
    decreases c, opcode, digit, keyLine
  {
    var km0: u8 := (((opcode & 7 as u16) | ((opcode >> 1 as bv5) & 120 as u16)) ^ 127 as u16) as u8;
    var km1: bv8 := km0 & keyLine;
    var km: bv8 := if km1 & (km1 - 1 as u8) != 0 as u8 then 0 as u8 else km1;
    if opcode & 8 as u16 == 0 as u16 then
      if keyLine & km != 0 as u8 then
        var b: nat := Ctz8(km);
        c.(KR := (digit as u16 << 4 as bv5) | ((b as u16 << 8 as bv5) & 1792 as u16), flags := c.flags & (65535 as u16 ^ FLG_COND))
      else if digit != 15 then
        c.(flags := c.flags | FLG_HOLD)
      else
        c
    else if keyLine & km != 0 as u8 then
      c.(flags := c.flags & (65535 as u16 ^ FLG_COND))
    else
      c
  }

  function ExecWait(c: Cpu, opcode: u16, digit: nat, ext: u16, keyLine: u8): Cpu
    requires digit <= 15
    requires opcode & 3840 as u16 == 2560 as u16
    decreases c, opcode, digit, ext, keyLine
  {
    var f: int := (opcode & 15 as u16) as int;
    var arg: nat := ((opcode >> 4 as bv5) & 15 as u16) as nat;
    var c0: Cpu := c;
    var c1: Cpu := if f == 0 then if digit != arg then c0.(flags := c0.flags | FLG_HOLD) else c0 else if f == 1 then c0.(flags := c0.flags & 65534 as u16) else if f == 2 then c0.(fA := 0) else if f == 3 then c0 else if f == 4 then var kr: bv16 := c0.KR + 16 as u16; if kr & 65520 as u16 == 0 as u16 then c0.(KR := kr ^ 1 as u16) else c0.(KR := kr) else if f == 5 then if Flg(c0.KR, Bit16(arg)) then c0.(flags := c0.flags & (65535 as u16 ^ FLG_COND)) else c0 else if f == 6 then var sel: int := (opcode & 240 as u16) as int; if sel == 16 then c0.(R5 := ((c0.fB >> 1 as bv5) & 15 as u16) as u8) else if sel == 0 then c0.(R5 := ((c0.fA >> 1 as bv5) & 15 as u16) as u8) else c0 else if f == 7 then c0.(R5 := ((opcode >> 4 as bv5) & 15 as u16) as u8) else if f == 8 then var sel: int := (opcode & 240 as u16) as int; if sel == 0 then c0.(R5 := ((c0.KR >> 4 as bv5) & 15 as u16) as u8) else if sel == 16 then c0.(KR := (c0.KR & 65295 as u16) | (c0.R5 as u16 << 4 as bv5)) else c0 else if f == 9 then c0.(flags := c0.flags | FLG_IDLE) else if f == 10 then c0.(fB := 0) else if f == 11 then if keyLine & Bit8(4) != 0 as u8 || Flg(c0.flags, FLG_BUSY) then c0.(flags := c0.flags & (65535 as u16 ^ (FLG_COND | FLG_BUSY))) else c0 else if f == 12 then c0.(KR := ((ext << 1 as bv5) & 65520 as u16) | (ext >> 15 as bv5)) else if f == 13 then c0.(KR := c0.SR, SR := c0.KR) else c0;
    c1
  }

  function RegSpecial(m: Mask, dst: Reg, r5: u8): Reg
    requires m.start >= 0 && m.start <= 15 && m.start <= m.end + 1 && m.end <= 15
    decreases m, dst, r5
  {
    RegSpecialFrom(m.start, dst, m, r5)
  }

  function RegSpecialFrom(i: nat, dst: Reg, m: Mask, r5: u8): Reg
    requires m.start >= 0 && m.start <= 15 && m.end <= 15 && i <= m.end + 1
    decreases m.end + 1 - i
  {
    if i > m.end then
      dst
    else
      var d: bv8 := if i == m.start then r5 else if i == m.cpos then m.cval else 0 as u8; RegSpecialFrom(i + 1, dst[i := d], m, r5)
  }

  function ExecAlu(c: Cpu, opcode: u16): Cpu
    requires opcode & 3840 as u16 != 0 as u16
    requires opcode & 3840 as u16 != 2048 as u16
    requires opcode & 3840 as u16 != 2560 as u16
    decreases c, opcode
  {
    var mIdx: nat := ((opcode >> 8 as bv5) & 15 as u16) as nat;
    var m: AluMask := MaskOf(mIdx);
    var inp: AluInp := ALU_OPS[((opcode >> 3 as bv5) & 31 as u16) as nat];
    var dsIdx: nat := (opcode & 7 as u16) as nat;
    var dsel: DstSel := ALU_DSTS[dsIdx];
    var ioValid: bool := Flg(c.flags, FLG_IO_VALID);
    var lowF8: int := (opcode & 248 as u16) as int;
    var res: AluRes := if lowF8 == 240 || lowF8 == 248 then if DstHasReg(dsel) then var dst0: Reg := RegSpecial(m, DstRegOrZero(c, dsel), c.R5); AluExec(true, dst0, false, ZeroReg(), true, dst0, m, if opcode & 8 as u16 == 0 as u16 then OP_ADD else OP_SUB, c.Sin, ioValid, c.R5, c.Sout) else AluExec(false, ZeroReg(), inp.hasX, SelReg(c, inp.x), inp.hasY, SelReg(c, inp.y), m, inp.op, c.Sin, ioValid, c.R5, c.Sout) else AluExec(DstHasReg(dsel), DstRegOrZero(c, dsel), inp.hasX, SelReg(c, inp.x), inp.hasY, SelReg(c, inp.y), m, inp.op, c.Sin, ioValid, c.R5, c.Sout);
    var c1: Cpu := c.(Sout := res.Sout, R5 := res.R5);
    var c2: Cpu := if DstHasReg(dsel) then SetDstReg(c1, dsel, res.dst) else c1;
    var c3: Cpu := if res.condClear then c2.(flags := c2.flags & (65535 as u16 ^ FLG_COND)) else c2;
    var c4: Cpu := if dsIdx == 2 then var p: (Reg, Reg) := XchExec(c3.A, c3.B, m); c3.(A := p.0, B := p.1) else if dsIdx == 5 then var p: (Reg, Reg) := XchExec(c3.C, c3.D, m); c3.(C := p.0, D := p.1) else if dsIdx == 7 then var p: (Reg, Reg) := XchExec(c3.A, c3.E, m); c3.(A := p.0, E := p.1) else c3;
    c4
  }

  function DstRegOrZero(c: Cpu, d: DstSel): Reg
    decreases c, d
  {
    if DstHasReg(d) then
      DstReg(c, d)
    else
      ZeroReg()
  }

  function Execute(c: Cpu, opcode: u16, digit: nat, ext: u16, keyLine: u8): Cpu
    requires digit <= 15
    decreases c, opcode, digit, ext, keyLine
  {
    if opcode & 4096 as u16 != 0 as u16 then
      c.(flags := c.flags | FLG_JUMP)
    else
      var c0: Cpu := if Flg(c.flags, FLG_JUMP) then c.(flags := (c.flags & (65535 as u16 ^ FLG_JUMP)) | FLG_COND) else c; var g: int := (opcode & 3840 as u16) as int; if g == 0 then ExecFlagOps(c0, opcode) else if g == 2048 then ExecKeyb(c0, opcode, digit, keyLine) else if g == 2560 then ExecWait(c0, opcode, digit, ext, keyLine) else ExecAlu(c0, opcode)
  }

  function GenDigitIdle(c: Cpu, dstate: nat): (char, bool, bool, bool)
    requires dstate <= 15
    decreases c, dstate
  {
    var i: nat := dstate;
    var zs1: bool := if i == 15 then true else c.zeroSuppr;
    var zs2: bool := if i == 3 || (c.R5 == i as u8 && i != 15) || c.B[i] as int >= 8 then false else zs1;
    var zs3: bool := if i == 2 then true else zs2;
    var ch: char := if c.B[i] == 7 || c.B[i] == 3 || (c.B[i] as int <= 4 && zs3 && c.A[i] == 0 as u8) then ' ' else if c.B[i] == 6 || (c.B[i] == 5 && c.A[i] == 0 as u8) then '-' else if c.B[i] == 5 then 'o' else if c.B[i] == 4 then '\'' else if c.B[3] == 2 then '""' else ('0' as int + c.A[i] as int) as char;
    var zs4: bool := if !(c.B[i] == 7 || c.B[i] == 3 || (c.B[i] as int <= 4 && zs3 && c.A[i] == 0 as u8) || c.B[i] == 6 || (c.B[i] == 5 && c.A[i] == 0 as u8) || c.B[i] == 5 || c.B[i] == 4 || c.B[3] == 2) && c.A[i] != 0 as u8 then false else zs3;
    var dpt: bool := c.R5 == i as u8;
    var segH: bool := if i < 15 then Flg(c.fA, Bit16(i + 1)) else false;
    (ch, dpt, segH, zs4)
  }

  import opened TI59_Nibbles

  import opened TI59_AluOps

  datatype Cpu = Cpu(A: Reg, B: Reg, C: Reg, D: Reg, E: Reg, KR: u16, SR: u16, fA: u16, fB: u16, R5: u8, Sout: Reg, Sin: Reg, opcode: u16, flags: u16, addr: int, reset: nat, zeroSuppr: bool)

  datatype RegSel = RS_NULL | RS_A | RS_B | RS_C | RS_D

  datatype AluInp = AluInp(hasX: bool, x: RegSel, hasY: bool, y: RegSel, op: AluOp)

  datatype DstSel = DS_A | DS_IO | DS_XCH_AB | DS_B | DS_C | DS_XCH_CD | DS_D | DS_XCH_AE
}

module TI59_Bus {
  function BusInit(): Bus
  {
    Bus(0 as u16, 0 as u16, ZeroReg(), ' ', false, false, 0 as u8, 15, false, 0, 0)
  }

  function BusCycleStart(b: Bus): Bus
    decreases b
  {
    b.(ext := 0, irg := 0, io := ZeroReg(), addr := -1, cycleCount := b.cycleCount + 1)
  }

  function BusAfterS14(b: Bus): Bus
    decreases b
  {
    b.(keyLine := 0, dstate := if b.dstate != 0 then b.dstate - 1 else 15)
  }

  import opened TI59_Nibbles

  datatype Bus = Bus(ext: u16, irg: u16, io: Reg, displayDigit: char, displayDpt: bool, displaySegH: bool, keyLine: u8, dstate: nat, idle: bool, addr: int, cycleCount: nat)
}

module TI59_Chips {
  function BromInit(data: seq<u16>, start: int, end: int): Brom
    requires |data| == end - start && 0 <= start
    decreases data, start, end
  {
    Brom(65536, 0, 0, data, start, end)
  }

  function BromBranchTarget(b: Brom, cond: bool): int
    decreases b, cond
  {
    var bcond: bool := (b.lastIrg >> 11 as bv5) & 1 != 0;
    var boffset: int := ((b.lastIrg >> 1 as bv5) & 1023 as u16) as int;
    var bneg: bool := b.lastIrg & 1 != 0;
    var off: int := if bneg then -boffset else boffset;
    if cond == bcond then
      b.pc + off
    else
      b.pc + 1
  }

  function BromStep(b: Brom, bus: Bus, sstate: nat, write: bool): (Brom, Bus)
    requires sstate <= 15
    requires |b.data| == b.end - b.start
    decreases b, bus, sstate, write
  {
    if sstate != 4 || !write then
      (b, bus)
    else
      var pc1: int := if bus.ext & EXT_HOLD != 0 then b.pc else if b.lastExt & EXT_PREG != 0 then (b.lastExt >> 3 as bv5) as int else if b.lastIrg & 4096 as u16 != 0 then BromBranchTarget(b, bus.ext & EXT_COND != 0) else b.pc + 1; var bus1: Bus := if b.start <= pc1 && pc1 < b.end then bus.(irg := b.data[pc1 - b.start], addr := pc1) else bus; (b.(pc := pc1), bus1)
  }

  function BromStepRead(b: Brom, bus: Bus, sstate: nat, write: bool): (Brom, Bus)
    requires sstate <= 15
    decreases b, bus, sstate, write
  {
    if sstate != 15 || write then
      (b, bus)
    else
      (b.(lastIrg := bus.irg, lastExt := bus.ext), bus)
  }

  function ScomInit(consts: seq<Reg>, base: int): Scom
    requires |consts| == 32
    requires 0 <= base && base % 32 == 0
    decreases consts, base
  {
    Scom(consts, seq(8, (_ /* _v0 */: int) => SeqFill(12)), 0, 0, base, base + 32, base / 32 * 8, base / 32 * 8 + 8)
  }

  function SeqFill(v: u8): Reg
    decreases v
  {
    seq(16, (_ /* _v1 */: int) => v)
  }

  function ScomConstStepW(s: Scom, bus: Bus, sstate: nat, write: bool): (Scom, Bus)
    requires sstate <= 15
    requires |s.CONST| > 0
    requires 0 <= s.startConst && s.endConst <= s.startConst + 32
    decreases s, bus, sstate, write
  {
    if sstate != 0 || !write then
      (s, bus)
    else
      var fc: bv32 := s.fifoConst >> 8 as bv6; var bus1: Bus := if fc & 128 as u32 != 0 then var idx: int := (fc & 127 as u32) as int; if idx < |s.CONST| then bus.(io := s.CONST[idx]) else bus else bus; (s.(fifoConst := fc), bus1)
  }

  function ScomRegStepW(s: Scom, bus: Bus, sstate: nat, write: bool): (Scom, Bus)
    requires sstate <= 15
    requires |s.SCOM| == 8
    decreases s, bus, sstate, write
  {
    if sstate != 0 || !write then
      (s, bus)
    else
      var fr: bv32 := s.fifoReg >> 8 as bv6; var bus1: Bus := if fr & 16 as u32 != 0 then bus.(io := s.SCOM[((fr >> 5 as bv6) & 7) as int]) else bus; (s.(fifoReg := fr), bus1)
  }

  function ScomRegStepR(s: Scom, bus: Bus, sstate: nat, write: bool): (Scom, Bus)
    requires sstate <= 15
    requires |s.SCOM| == 8
    requires 0 <= s.startReg && s.endReg <= s.startReg + 8
    decreases s, bus, sstate, write
  {
    if sstate != 15 || write then
      (s, bus)
    else
      var s1: Scom := if s.fifoReg & 1 != 0 && s.fifoReg & 16 as u32 == 0 then var a: int := ((s.fifoReg >> 5 as bv6) & 7) as int; s.(SCOM := s.SCOM[a := bus.io]) else s; var s2: Scom := if bus.irg & 65519 as u16 == 2575 as u16 then var addr: int := bus.io[0] as int; var addr1: int := if s1.fifoReg & 16 as u32 != 0 then 0 else addr; if s.startReg <= addr1 && addr1 < s.endReg then var a: int := (addr1 - s.startReg) * 32 + (bus.irg & 31 as u16) as int; s1.(fifoReg := s1.fifoReg | (a as u32 << 16 as bv6)) else s1 else s1; (s2, bus)
  }

  function ScomConstStepR(s: Scom, bus: Bus, sstate: nat, write: bool): (Scom, Bus)
    requires sstate <= 15
    requires 0 <= s.startConst && s.endConst <= s.startConst + 32
    decreases s, bus, sstate, write
  {
    if sstate != 15 || write then
      (s, bus)
    else
      var s1: Scom := if bus.irg & 4304 as u16 == 192 as u16 && bus.irg & 3840 as u16 != 0 && bus.irg & 3840 as u16 != 2048 as u16 && bus.irg & 3840 as u16 != 2560 as u16 then var addr: int := (((bus.ext >> 4 as bv5) & 120 as u16) | ((bus.ext >> 3 as bv5) & 7 as u16)) as int; if s.startConst <= addr && addr < s.endConst then var idx: u32 := (addr - s.startConst + 128) as u32; s.(fifoConst := s.fifoConst | (idx << 8 as bv6)) else s else s; (s1, bus)
  }

  function RamInit(data: seq<Reg>, start: int): Ram
    requires |data| == 30
    decreases data, start
  {
    Ram(start, start + 30, data, false, false, false, 0, 0)
  }

  function RamStepW(r: Ram, bus: Bus, sstate: nat, write: bool): (Ram, Bus)
    requires sstate <= 15
    requires |r.data| == 30
    decreases r, bus, sstate, write
  {
    if sstate != 0 || !write then
      (r, bus)
    else if !r.execCmd then
      (r, bus)
    else
      var inRange: bool := 0 <= r.addr && r.addr < |r.data|; if r.cmd == 2 then (r.(data := if inRange then r.data[r.addr := ZeroReg()] else r.data, execCmd := false), bus) else if r.cmd == 4 then (r.(data := RamClear10(r.data, r.addr), execCmd := false), bus) else if r.cmd == 0 then (r.(execCmd := false), if inRange then bus.(io := r.data[r.addr]) else bus) else (r, bus)
  }

  function RamClear10(data: seq<Reg>, addr: int): seq<Reg>
    requires |data| == 30
    decreases data, addr
  {
    RamClearFrom(data, addr, 0)
  }

  function RamClearFrom(data: seq<Reg>, addr: int, k: nat): seq<Reg>
    requires |data| == 30
    decreases 10 - k
  {
    if k >= 10 then
      data
    else if 0 <= addr + k && addr + k < 30 then
      RamClearFrom(data[addr + k := ZeroReg()], addr, k + 1)
    else
      RamClearFrom(data, addr, k + 1)
  }

  function RamStepR(r: Ram, bus: Bus, sstate: nat, write: bool): (Ram, Bus)
    requires sstate <= 15
    requires |r.data| == 30
    decreases r, bus, sstate, write
  {
    if sstate != 15 || write then
      (r, bus)
    else
      var r1: Ram := if r.execCmd && r.cmd == 1 && 0 <= r.addr && r.addr < |r.data| then r.(data := r.data[r.addr := bus.io], execCmd := false) else r; var r2: Ram := if r1.wait2Cmd then var addr: int := bus.io[3] as int * 10 + bus.io[2] as int; var cmd: int := bus.io[0] as int; if r.start <= addr && addr < r.end && (cmd <= 2 || cmd == 4) then r1.(addr := addr - r.start, cmd := cmd, wait2Cmd := false, execCmd := true) else r1.(wait2Cmd := false) else r1; var r3: Ram := if r2.waitCmd then r2.(waitCmd := false, wait2Cmd := true) else r2; var r4: Ram := if bus.irg == 2808 as u16 then r3.(waitCmd := true) else r3; (r4, bus)
  }

  const LIB_NO_FLAGS: LibFlags := LibFlags(false, false, false)

  function LibInit(data: seq<u8>): Lib
    requires |data| == 5000
    decreases data
  {
    Lib(222, data, LIB_NO_FLAGS, LIB_NO_FLAGS)
  }

  function LibData(l: Lib, i: int): u8
    decreases l, i
  {
    if 0 <= i && i < |l.data| then
      l.data[i]
    else
      0
  }

  function LibStepW(l: Lib, bus: Bus, sstate: nat, write: bool): (Lib, Bus)
    requires sstate <= 15
    decreases l, bus, sstate, write
  {
    if sstate != 15 || !write then
      (l, bus)
    else if l.flagsDelay.inData then
      var d: u16 := LibData(l, l.pc) as u16;
      var pc1: int := l.pc + 1;
      var pc2: int := if pc1 >= |l.data| then 0 else pc1;
      (l.(pc := pc2), bus.(ext := bus.ext | (d << 3 as bv5)))
    else if l.flagsDelay.inPc then
      (l.(pc := l.pc / 10), bus.(ext := bus.ext | ((l.pc % 10) as u16 << 3 as bv5)))
    else if l.flagsDelay.inDataHigh then
      var d: u16 := (LibData(l, l.pc) >> 4 as bv4) as u16;
      (l, bus.(ext := bus.ext | (d << 3 as bv5)))
    else
      (l, bus)
  }

  function LibStepR(l: Lib, bus: Bus, sstate: nat, write: bool): (Lib, Bus)
    requires sstate <= 15
    decreases l, bus, sstate, write
  {
    if sstate != 15 || write then
      (l, bus)
    else
      var l1: Lib := l.(flagsDelay := l.flags, flags := LIB_NO_FLAGS); var l2: Lib := if bus.irg == 2574 as u16 then l1.(flags := LibFlags(true, l1.flags.inPc, l1.flags.inDataHigh)) else if bus.irg == 2590 as u16 then var pcH: int := ((bus.ext >> 3 as bv5) & 15 as u16) as int; l1.(pc := l1.pc / 10 + pcH * 1000) else if bus.irg == 2606 as u16 then l1.(flags := LibFlags(l1.flags.inData, true, l1.flags.inDataHigh)) else if bus.irg == 2622 as u16 then l1.(flags := LibFlags(l1.flags.inData, l1.flags.inPc, true)) else l1; (l2, bus)
  }

  function KeyInit(script: seq<KeyEvent>): KeyState
    requires |script| >= 0
    decreases script
  {
    KeyState(seq(16, (i: int) => if i == 10 then 16 as u8 else 0 as u8), 1 as u8, 1, 0, 0, 2, 3, 32, 32, script, 0)
  }

  function KeyReadNew(k: KeyState, cycle: nat): KeyState
    requires |k.persKeys| == 16
    ensures |KeyReadNew(k, cycle).persKeys| == 16
    decreases k, cycle
  {
    var k1: KeyState := KeyApplyToggles(k, cycle);
    if k1.scriptPos < |k1.script| then
      var ev: KeyEvent := k1.script[k1.scriptPos];
      if cycle >= ev.at then
        k1.(keyCode := ev.code, keyCount := if ev.hold > 0 then ev.hold else k1.pressCycle, scriptPos := k1.scriptPos + 1)
      else
        k1
    else
      k1
  }

  function KeyApplyToggles(k: KeyState, cycle: nat): KeyState
    requires |k.persKeys| == 16
    ensures |KeyApplyToggles(k, cycle).persKeys| == 16
    decreases |k.script| - k.scriptPos
  {
    if k.scriptPos < |k.script| && k.script[k.scriptPos].toggle && cycle >= k.script[k.scriptPos].at then
      var ev: KeyEvent := k.script[k.scriptPos];
      var idx: int := (ev.code & 15) as int;
      var bit: int := ((ev.code >> 4 as bv4) & 7) as int;
      var pers: bv8 := if 0 <= idx && idx < |k.persKeys| then k.persKeys[idx] else 0 as u8;
      var pers1: bv8 := pers ^ Bit8(bit);
      KeyApplyToggles(k.(persKeys := k.persKeys[idx := pers1], scriptPos := k.scriptPos + 1), cycle)
    else
      k
  }

  function KeyStep(k: KeyState, bus: Bus, sstate: nat, write: bool): (KeyState, Bus)
    requires sstate <= 15
    requires |k.persKeys| == 16
    requires bus.dstate <= 15
    decreases k, bus, sstate, write
  {
    if sstate != 15 || write then
      (k, bus)
    else
      var k1: KeyState := if bus.ext & EXT_HOLD == 0 && bus.irg & 65280 as u16 == 2048 as u16 then var scan: bool := bus.irg & 8 as u16 == 0; if !scan then if bus.dstate < 15 && bus.dstate > 0 then if k.keyCountHw <= 0 then k else k.(keyCountHw := k.keyCountHw - 1) else k else var allPress: bool := bus.irg & 255 as u16 == k.pressMask as u16; var allUnpress: bool := bus.irg & 255 as u16 == k.unpressMask as u16; if allPress && k.keyCount > 1 then k.(keyCount := k.keyCount - 1) else if allUnpress && k.keyCount > 0 - k.unpressCycle then k.(keyCode := 0, keyCount := k.keyCount - 1, keyCodeHw := 0, keyCountHw := 0) else if allPress && k.keyCode == 0 then var kread: KeyState := KeyReadNew(k, bus.cycleCount); kread.(keyCodeHw := kread.keyCode, keyCountHw := kread.keyCount) else k else k; var keyLine1: u8 := if bus.ext & EXT_HOLD == 0 && bus.irg & 65280 as u16 == 2048 as u16 && bus.irg & 8 as u16 == 0 && (k1.keyCode & 15) as int == bus.dstate && k1.keyCount > 0 then bus.keyLine | Bit8(((k1.keyCode >> 4 as bv4) & 7) as int) else bus.keyLine; var keyLine2: u8 := if (k1.keyCodeHw & 15) as int == bus.dstate && k1.keyCountHw > 0 then keyLine1 | Bit8(((k1.keyCodeHw >> 4 as bv4) & 7) as int) else keyLine1; var keyLine3: u8 := keyLine2 | k1.persKeys[bus.dstate]; (k1, bus.(keyLine := keyLine3))
  }

  function DispInit(): Disp
  {
    Disp(seq(30, (_ /* _v2 */: int) => Nul()), seq(30, (_ /* _v3 */: int) => Nul()), 0)
  }

  function SetChar(s: seq<char>, i: nat, c: char): seq<char>
    requires |s| == 30
    decreases s, i, c
  {
    if i < 30 then
      s[i := c]
    else
      s
  }

  function DispStep(d: Disp, bus: Bus, sstate: nat, write: bool): (Disp, Bus)
    requires sstate <= 15
    requires |d.out| == 30 && |d.out1| == 30
    decreases d, bus, sstate, write
  {
    if sstate != 0 || write then
      (d, bus)
    else
      var out1: seq<char> := if 2 <= bus.dstate && bus.dstate <= 13 then if bus.dstate != 13 then SetChar(d.out, d.pos, bus.displayDigit) else if bus.displayDigit == '-' then SetChar(d.out, d.pos, if bus.displaySegH then 'E' else '-') else SetChar(d.out, d.pos, if bus.displaySegH then 'C' else ' ') else d.out; var pos1: nat := if 2 <= bus.dstate && bus.dstate <= 13 then d.pos + 1 else d.pos; var out2: seq<char> := if 2 <= bus.dstate && bus.dstate <= 13 && bus.displayDpt then SetChar(out1, pos1, '.') else out1; var pos2: nat := if 2 <= bus.dstate && bus.dstate <= 13 && bus.displayDpt then pos1 + 1 else pos1; if bus.dstate == 0 then var outF: seq<char> := SetChar(out2, pos2 + 2, if bus.idle then ' ' else 'B'); (Disp(seq(30, (i: int) => if i < 29 then ' ' else outF[29]), outF, 0), bus) else (Disp(out2, d.out1, pos2), bus)
  }

  import opened TI59_Nibbles

  import opened TI59_AluOps

  import opened TI59_Bus

  datatype Brom = Brom(pc: int, lastIrg: u16, lastExt: u16, data: seq<u16>, start: int, end: int)

  type u32 = bv32

  datatype Scom = Scom(CONST: seq<Reg>, SCOM: seq<Reg>, fifoConst: u32, fifoReg: u32, startConst: int, endConst: int, startReg: int, endReg: int)

  datatype Ram = Ram(start: int, end: int, data: seq<Reg>, waitCmd: bool, wait2Cmd: bool, execCmd: bool, cmd: int, addr: int)

  datatype LibFlags = LibFlags(inData: bool, inPc: bool, inDataHigh: bool)

  datatype Lib = Lib(pc: int, data: seq<u8>, flags: LibFlags, flagsDelay: LibFlags)

  datatype KeyEvent = KeyEvent(code: u8, at: nat, hold: nat, toggle: bool)

  datatype KeyState = KeyState(persKeys: seq<u8>, keyCode: u8, keyCount: int, keyCodeHw: u8, keyCountHw: int, pressCycle: nat, unpressCycle: nat, pressMask: u8, unpressMask: u8, script: seq<KeyEvent>, scriptPos: nat)

  datatype Disp = Disp(out: seq<char>, out1: seq<char>, pos: nat)
}

module TI59_Machine {
  predicate MachineWF(m: Machine)
    decreases m
  {
    |m.broms| == 3 &&
    |m.scoms| == 2 &&
    |m.rams| == 4 &&
    (forall i: int {:trigger m.broms[i]} | 0 <= i < 3 :: 
      var b: Brom := m.broms[i]; 0 <= b.start && |b.data| == b.end - b.start) &&
    (forall i: int {:trigger m.scoms[i]} | 0 <= i < 2 :: 
      var s: Scom := m.scoms[i]; |s.CONST| == 32 && |s.SCOM| == 8 && 0 <= s.startConst && s.endConst == s.startConst + 32 && 0 <= s.startReg && s.endReg == s.startReg + 8) &&
    (forall i: int {:trigger m.rams[i]} | 0 <= i < 4 :: 
      |m.rams[i].data| == 30) &&
    |m.lib.data| == 5000 &&
    |m.keys.persKeys| == 16 &&
    |m.disp.out| == 30 &&
    |m.disp.out1| == 30 &&
    m.bus.dstate <= 15
  }

  function AluChip(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(AluChip(m, sstate, write))
    decreases m, sstate, write
  {
    var c: Cpu := m.cpu;
    var bus: Bus := m.bus;
    var c1: Cpu := c.(flags := c.flags & 64511 as u16);
    if c1.reset != 0 then
      var reset1: nat := if sstate == 0 && write then c1.reset - 1 else c1.reset;
      var bus1: Bus := if sstate == 0 && write && c1.reset > 1 then bus.(ext := 1 as u16) else bus;
      var captured: bool := sstate == 15 && !write && reset1 == 1;
      var c2: Cpu := if captured then c1.(reset := reset1, opcode := bus1.irg, addr := bus1.addr) else c1.(reset := reset1);
      m.(cpu := c2, bus := bus1, halted := m.halted || (captured && bus1.addr == -1))
    else if sstate == 0 && write then
      var c2: Cpu := c1.(flags := c1.flags & 65533 as u16);
      var c3: Cpu := c2.(Sin := ZeroReg(), Sout := ZeroReg());
      var c4: Cpu := if Flg(c3.flags, FLG_COND) then c3.(flags := c3.flags | FLG_COND_LAST) else c3;
      var gd: (char, bool, bool, bool) := GenDigitIdle(c4, bus.dstate);
      var c5: Cpu := if Flg(c4.flags, FLG_IDLE) then c4.(zeroSuppr := gd.3) else c4;
      var bus1: Bus := if Flg(c4.flags, FLG_IDLE) then bus.(displayDigit := gd.0, displayDpt := gd.1, displaySegH := gd.2) else bus.(displaySegH := true);
      var re: (bool, bool) := RunEarly(c5.opcode);
      var c6: Cpu := if re.1 then c5.(flags := c5.flags | FLG_IO_VALID) else c5;
      var c7: Cpu := if re.0 then Execute(c6, c6.opcode, bus.dstate, bus1.ext, bus1.keyLine) else c6;
      var bus2: Bus := if re.0 && Flg(c7.flags, FLG_IO_VALID) then bus1.(io := c7.Sout) else bus1;
      var bus3: Bus := if c7.opcode != 2572 as u16 then bus2.(ext := ((c7.KR >> 1 as bv5) | (c7.KR << 15 as bv5)) & 65529 as u16) else bus2;
      m.(cpu := c7, bus := bus3.(idle := Flg(c7.flags, FLG_IDLE)))
    else if sstate == 1 && write then
      var bus1: Bus := if Flg(c1.flags, FLG_COND_LAST) then bus.(ext := bus.ext | EXT_COND) else bus;
      var c2: Cpu := if Flg(c1.flags, FLG_COND_LAST) then c1.(flags := c1.flags & 61439 as u16) else c1;
      m.(cpu := c2, bus := bus1.(idle := Flg(c2.flags, FLG_IDLE)))
    else if sstate == 2 && write then
      var bus1: Bus := if Flg(c1.flags, FLG_HOLD) then bus.(ext := bus.ext | EXT_HOLD) else bus;
      m.(cpu := c1, bus := bus1.(idle := Flg(c1.flags, FLG_IDLE)))
    else if sstate == 2 && !write then
      var c2: Cpu := if bus.ext & EXT_HOLD == 0 then c1.(KR := c1.KR & 65533 as u16) else c1;
      m.(cpu := c2, bus := bus.(idle := Flg(c2.flags, FLG_IDLE)))
    else if sstate == 14 && write then
      m.(cpu := c1, bus := bus.(displayDpt := false, displaySegH := false, displayDigit := ' ', idle := Flg(c1.flags, FLG_IDLE)))
    else if sstate == 15 && !write then
      var re: (bool, bool) := RunEarly(c1.opcode);
      var c2: Cpu := if !re.0 then Execute(c1.(Sin := bus.io), c1.opcode, bus.dstate, bus.ext, bus.keyLine) else c1;
      var c3: Cpu := c2.(opcode := bus.irg, addr := bus.addr);
      m.(cpu := c3, bus := bus.(idle := Flg(c3.flags, FLG_IDLE)), halted := m.halted || bus.addr == -1)
    else
      m.(cpu := c1, bus := bus.(idle := Flg(c1.flags, FLG_IDLE)))
  }

  function BromsFold(m: Machine, i: nat, sstate: nat, write: bool): Machine
    requires MachineWF(m) && i <= 3 && sstate <= 15
    ensures MachineWF(BromsFold(m, i, sstate, write))
    decreases 3 - i
  {
    if i == 3 then
      m
    else
      var (b: Brom, bus1: Bus) := if write then BromStep(m.broms[i], m.bus, sstate, true) else BromStepRead(m.broms[i], m.bus, sstate, false); BromsFold(m.(broms := m.broms[i := b], bus := bus1), i + 1, sstate, write)
  }

  function ScomsFold(m: Machine, i: nat, sstate: nat, write: bool): Machine
    requires MachineWF(m) && i <= 2 && sstate <= 15
    ensures MachineWF(ScomsFold(m, i, sstate, write))
    decreases 2 - i
  {
    if i == 2 then
      m
    else
      var s: Scom := m.scoms[i]; var (s1: Scom, bus1: Bus) := if write then var (sa: Scom, busA: Bus) := ScomConstStepW(s, m.bus, sstate, true); var (sb: Scom, busB: Bus) := ScomRegStepW(sa, busA, sstate, true); (sb, busB) else var (sa: Scom, busA: Bus) := ScomConstStepR(s, m.bus, sstate, false); var (sb: Scom, busB: Bus) := ScomRegStepR(sa, busA, sstate, false); (sb, busB); ScomsFold(m.(scoms := m.scoms[i := s1], bus := bus1), i + 1, sstate, write)
  }

  function RamsFold(m: Machine, i: nat, sstate: nat, write: bool): Machine
    requires MachineWF(m) && i <= 4 && sstate <= 15
    ensures MachineWF(RamsFold(m, i, sstate, write))
    decreases 4 - i
  {
    if i == 4 then
      m
    else
      var (r: Ram, bus1: Bus) := if write then RamStepW(m.rams[i], m.bus, sstate, true) else RamStepR(m.rams[i], m.bus, sstate, false); RamsFold(m.(rams := m.rams[i := r], bus := bus1), i + 1, sstate, write)
  }

  function LibChip(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(LibChip(m, sstate, write))
    decreases m, sstate, write
  {
    var (l: Lib, bus1: Bus) := if write then LibStepW(m.lib, m.bus, sstate, true) else LibStepR(m.lib, m.bus, sstate, false);
    m.(lib := l, bus := bus1)
  }

  function DispChip(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(DispChip(m, sstate, write))
    decreases m, sstate, write
  {
    var (d: Disp, bus1: Bus) := DispStep(m.disp, m.bus, sstate, write);
    m.(disp := d, bus := bus1)
  }

  function KeyChip(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(KeyChip(m, sstate, write))
    decreases m, sstate, write
  {
    var (k: KeyState, bus1: Bus) := KeyStep(m.keys, m.bus, sstate, write);
    m.(keys := k, bus := bus1)
  }

  function PhaseFold(m: Machine, sstate: nat, write: bool): Machine
    requires MachineWF(m) && sstate <= 15
    ensures MachineWF(PhaseFold(m, sstate, write))
    decreases m, sstate, write
  {
    var m1: Machine := AluChip(m, sstate, write);
    var m2: Machine := BromsFold(m1, 0, sstate, write);
    var m3: Machine := ScomsFold(m2, 0, sstate, write);
    var m4: Machine := RamsFold(m3, 0, sstate, write);
    var m5: Machine := LibChip(m4, sstate, write);
    var m6: Machine := DispChip(m5, sstate, write);
    KeyChip(m6, sstate, write)
  }

  function SStateFold(m: Machine, s: nat): Machine
    requires MachineWF(m) && s <= 15
    ensures MachineWF(SStateFold(m, s))
    decreases m, s
  {
    var m1: Machine := PhaseFold(m, s, true);
    var m2: Machine := PhaseFold(m1, s, false);
    if s == 14 then
      m2.(bus := BusAfterS14(m2.bus))
    else
      m2
  }

  function CycleFold(m: Machine, s: nat): Machine
    requires MachineWF(m) && s <= 16
    ensures MachineWF(CycleFold(m, s))
    decreases 16 - s
  {
    if s == 16 then
      m
    else
      CycleFold(SStateFold(m, s), s + 1)
  }

  function MkTrace(m: Machine): TraceRec
    requires MachineWF(m)
    decreases m
  {
    TraceRec(m.cpu.addr, m.cpu.opcode, m.cpu.A, m.cpu.B, m.cpu.C, m.cpu.D, m.cpu.E, m.cpu.fA, m.cpu.fB, m.cpu.KR, m.cpu.SR, m.cpu.R5, 0 as u16, Flg(m.cpu.flags, FLG_COND), Flg(m.cpu.flags, FLG_IDLE), Flg(m.cpu.flags, FLG_HOLD), m.cpu.Sin, m.cpu.Sout, m.bus.dstate)
  }

  function StepInstruction(m: Machine): (Machine, OptRec)
    requires MachineWF(m)
    ensures MachineWF(StepInstruction(m).0)
    decreases m
  {
    if m.halted then
      (m, NoRec)
    else
      var rec: OptRec := if m.cpu.reset == 0 then YesRec(MkTrace(m)) else NoRec; var m1: Machine := m.(bus := BusCycleStart(m.bus)); var m2: Machine := CycleFold(m1, 0); (m2, rec)
  }

  function Run(m: Machine, budget: nat): Machine
    requires MachineWF(m)
    ensures MachineWF(Run(m, budget))
    decreases budget
  {
    if budget == 0 || m.halted then
      m
    else
      Run(StepInstruction(m).0, budget - 1)
  }

  import opened TI59_Nibbles

  import opened TI59_AluOps

  import opened TI59_Cpu

  import opened TI59_Bus

  import opened TI59_Chips

  datatype OptRec = NoRec | YesRec(rec: TraceRec)

  datatype Machine = Machine(cpu: Cpu, broms: seq<Brom>, scoms: seq<Scom>, rams: seq<Ram>, lib: Lib, keys: KeyState, disp: Disp, bus: Bus, halted: bool)

  datatype TraceRec = TraceRec(addr: int, opcode: u16, A: Reg, B: Reg, C: Reg, D: Reg, E: Reg, fA: u16, fB: u16, KR: u16, SR: u16, R5: u8, ext: u16, cond: bool, idle: bool, hold: bool, Sin: Reg, Sout: Reg, dstate: nat)
}
")]

//-----------------------------------------------------------------------------
//
// Copyright by the contributors to the Dafny Project
// SPDX-License-Identifier: MIT
//
//-----------------------------------------------------------------------------

// When --include-runtime is true, this file is directly prepended
// to the output program. We have to avoid these using directives in that case
// since they can only appear before any other declarations.
// The DafnyRuntime.csproj file is the only place that ISDAFNYRUNTIMELIB is defined,
// so these are only active when building the C# DafnyRuntime.dll library.
#if ISDAFNYRUNTIMELIB
using System; // for Func
using System.Numerics;
using System.Collections;
#endif

namespace DafnyAssembly {
  [AttributeUsage(AttributeTargets.Assembly)]
  public class DafnySourceAttribute : Attribute {
    public readonly string dafnySourceText;
    public DafnySourceAttribute(string txt) { dafnySourceText = txt; }
  }
}

namespace Dafny {
  using System.Collections.Generic;
  using System.Collections.Immutable;
  using System.Linq;

  // Similar to System.Text.Rune, which would be perfect to use
  // except that it isn't available in the platforms we support
  // (.NET Standard 2.0 and .NET Framework 4.5.2)
  public readonly struct Rune : IComparable, IComparable<Rune>, IEquatable<Rune> {

    private readonly uint _value;

    public Rune(int value)
      : this((uint)value) {
    }

    public Rune(uint value) {
      if (!(value < 0xD800 || (0xE000 <= value && value < 0x11_0000))) {
        throw new ArgumentException();
      }

      _value = value;
    }

    public static bool IsRune(BigInteger i) {
      return (0 <= i && i < 0xD800) || (0xE000 <= i && i < 0x11_0000);
    }

    public int Value => (int)_value;

    public bool Equals(Rune other) => this == other;

    public override bool Equals(object obj) => (obj is Rune other) && Equals(other);

    public override int GetHashCode() => Value;

    // Values are always between 0 and 0x11_0000, so overflow isn't possible
    public int CompareTo(Rune other) => this.Value - other.Value;

    int IComparable.CompareTo(object obj) {
      switch (obj) {
        case null:
          return 1; // non-null ("this") always sorts after null
        case Rune other:
          return CompareTo(other);
        default:
          throw new ArgumentException();
      }
    }

    public static bool operator ==(Rune left, Rune right) => left._value == right._value;

    public static bool operator !=(Rune left, Rune right) => left._value != right._value;

    public static bool operator <(Rune left, Rune right) => left._value < right._value;

    public static bool operator <=(Rune left, Rune right) => left._value <= right._value;

    public static bool operator >(Rune left, Rune right) => left._value > right._value;

    public static bool operator >=(Rune left, Rune right) => left._value >= right._value;

    public static explicit operator Rune(int value) => new Rune(value);
    public static explicit operator Rune(BigInteger value) => new Rune((uint)value);

    // Defined this way to be consistent with System.Text.Rune,
    // but note that Dafny will use Helpers.ToString(rune),
    // which will print in the style of a character literal instead.
    public override string ToString() {
      return char.ConvertFromUtf32(Value);
    }

    // Replacement for String.EnumerateRunes() from newer platforms
    public static IEnumerable<Rune> Enumerate(string s) {
      var sLength = s.Length;
      for (var i = 0; i < sLength; i++) {
        if (char.IsHighSurrogate(s[i])) {
          if (char.IsLowSurrogate(s[i + 1])) {
            yield return (Rune)char.ConvertToUtf32(s[i], s[i + 1]);
            i++;
          } else {
            throw new ArgumentException();
          }
        } else if (char.IsLowSurrogate(s[i])) {
          throw new ArgumentException();
        } else {
          yield return (Rune)s[i];
        }
      }
    }
  }

  public interface ISet<out T> {
    int Count { get; }
    long LongCount { get; }
    IEnumerable<T> Elements { get; }
    IEnumerable<ISet<T>> AllSubsets { get; }
    bool Contains<G>(G t);
    bool EqualsAux(ISet<object> other);
    ISet<U> DowncastClone<U>(Func<T, U> converter);
  }

  public class Set<T> : ISet<T> {
    readonly ImmutableHashSet<T> setImpl;
    readonly bool containsNull;
    Set(ImmutableHashSet<T> d, bool containsNull) {
      this.setImpl = d;
      this.containsNull = containsNull;
    }

    public static readonly ISet<T> Empty = new Set<T>(ImmutableHashSet<T>.Empty, false);

    private static readonly TypeDescriptor<ISet<T>> _TYPE = new Dafny.TypeDescriptor<ISet<T>>(Empty);
    public static TypeDescriptor<ISet<T>> _TypeDescriptor() {
      return _TYPE;
    }

    public static ISet<T> FromElements(params T[] values) {
      return FromCollection(values);
    }

    public static Set<T> FromISet(ISet<T> s) {
      return s as Set<T> ?? FromCollection(s.Elements);
    }

    public static Set<T> FromCollection(IEnumerable<T> values) {
      var d = ImmutableHashSet<T>.Empty.ToBuilder();
      var containsNull = false;
      foreach (T t in values) {
        if (t == null) {
          containsNull = true;
        } else {
          d.Add(t);
        }
      }

      return new Set<T>(d.ToImmutable(), containsNull);
    }

    public static ISet<T> FromCollectionPlusOne(IEnumerable<T> values, T oneMoreValue) {
      var d = ImmutableHashSet<T>.Empty.ToBuilder();
      var containsNull = false;
      if (oneMoreValue == null) {
        containsNull = true;
      } else {
        d.Add(oneMoreValue);
      }

      foreach (T t in values) {
        if (t == null) {
          containsNull = true;
        } else {
          d.Add(t);
        }
      }

      return new Set<T>(d.ToImmutable(), containsNull);
    }

    public ISet<U> DowncastClone<U>(Func<T, U> converter) {
      if (this is ISet<U> th) {
        return th;
      } else {
        var d = ImmutableHashSet<U>.Empty.ToBuilder();
        foreach (var t in this.setImpl) {
          var u = converter(t);
          d.Add(u);
        }

        return new Set<U>(d.ToImmutable(), this.containsNull);
      }
    }

    public int Count {
      get { return this.setImpl.Count + (containsNull ? 1 : 0); }
    }

    public long LongCount {
      get { return this.setImpl.Count + (containsNull ? 1 : 0); }
    }

    public IEnumerable<T> Elements {
      get {
        if (containsNull) {
          yield return default(T);
        }

        foreach (var t in this.setImpl) {
          yield return t;
        }
      }
    }

    /// <summary>
    /// This is an inefficient iterator for producing all subsets of "this".
    /// </summary>
    public IEnumerable<ISet<T>> AllSubsets {
      get {
        // Start by putting all set elements into a list, but don't include null
        var elmts = new List<T>();
        elmts.AddRange(this.setImpl);
        var n = elmts.Count;
        var which = new bool[n];
        var s = ImmutableHashSet<T>.Empty.ToBuilder();
        while (true) {
          // yield both the subset without null and, if null is in the original set, the subset with null included
          var ihs = s.ToImmutable();
          yield return new Set<T>(ihs, false);
          if (containsNull) {
            yield return new Set<T>(ihs, true);
          }

          // "add 1" to "which", as if doing a carry chain.  For every digit changed, change the membership of the corresponding element in "s".
          int i = 0;
          for (; i < n && which[i]; i++) {
            which[i] = false;
            s.Remove(elmts[i]);
          }

          if (i == n) {
            // we have cycled through all the subsets
            break;
          }

          which[i] = true;
          s.Add(elmts[i]);
        }
      }
    }

    public bool Equals(ISet<T> other) {
      if (ReferenceEquals(this, other)) {
        return true;
      }

      if (other == null || Count != other.Count) {
        return false;
      }

      foreach (var elmt in Elements) {
        if (!other.Contains(elmt)) {
          return false;
        }
      }

      return true;
    }

    public override bool Equals(object other) {
      if (other is ISet<T>) {
        return Equals((ISet<T>)other);
      }

      var th = this as ISet<object>;
      var oth = other as ISet<object>;
      if (th != null && oth != null) {
        // We'd like to obtain the more specific type parameter U for oth's type ISet<U>.
        // We do that by making a dynamically dispatched call, like:
        //     oth.Equals(this)
        // The hope is then that its comparison "this is ISet<U>" (that is, the first "if" test
        // above, but in the call "oth.Equals(this)") will be true and the non-virtual Equals
        // can be called. However, such a recursive call to "oth.Equals(this)" could turn
        // into infinite recursion. Therefore, we instead call "oth.EqualsAux(this)", which
        // performs the desired type test, but doesn't recurse any further.
        return oth.EqualsAux(th);
      } else {
        return false;
      }
    }

    public bool EqualsAux(ISet<object> other) {
      var s = other as ISet<T>;
      if (s != null) {
        return Equals(s);
      } else {
        return false;
      }
    }

    public override int GetHashCode() {
      var hashCode = 1;
      if (containsNull) {
        hashCode = hashCode * (Dafny.Helpers.GetHashCode(default(T)) + 3);
      }

      foreach (var t in this.setImpl) {
        hashCode = hashCode * (Dafny.Helpers.GetHashCode(t) + 3);
      }

      return hashCode;
    }

    public override string ToString() {
      var s = "{";
      var sep = "";
      if (containsNull) {
        s += sep + Dafny.Helpers.ToString(default(T));
        sep = ", ";
      }

      foreach (var t in this.setImpl) {
        s += sep + Dafny.Helpers.ToString(t);
        sep = ", ";
      }

      return s + "}";
    }
    public static bool IsProperSubsetOf(ISet<T> th, ISet<T> other) {
      return th.Count < other.Count && IsSubsetOf(th, other);
    }
    public static bool IsSubsetOf(ISet<T> th, ISet<T> other) {
      if (other.Count < th.Count) {
        return false;
      }
      foreach (T t in th.Elements) {
        if (!other.Contains(t)) {
          return false;
        }
      }
      return true;
    }
    public static bool IsDisjointFrom(ISet<T> th, ISet<T> other) {
      ISet<T> a, b;
      if (th.Count < other.Count) {
        a = th; b = other;
      } else {
        a = other; b = th;
      }
      foreach (T t in a.Elements) {
        if (b.Contains(t)) {
          return false;
        }
      }
      return true;
    }
    public bool Contains<G>(G t) {
      return t == null ? containsNull : t is T && this.setImpl.Contains((T)(object)t);
    }
    public static ISet<T> Union(ISet<T> th, ISet<T> other) {
      var a = FromISet(th);
      var b = FromISet(other);
      return new Set<T>(a.setImpl.Union(b.setImpl), a.containsNull || b.containsNull);
    }
    public static ISet<T> Intersect(ISet<T> th, ISet<T> other) {
      var a = FromISet(th);
      var b = FromISet(other);
      return new Set<T>(a.setImpl.Intersect(b.setImpl), a.containsNull && b.containsNull);
    }
    public static ISet<T> Difference(ISet<T> th, ISet<T> other) {
      var a = FromISet(th);
      var b = FromISet(other);
      return new Set<T>(a.setImpl.Except(b.setImpl), a.containsNull && !b.containsNull);
    }
  }

  public interface IMultiSet<out T> {
    bool IsEmpty { get; }
    int Count { get; }
    long LongCount { get; }
    BigInteger ElementCount { get; }
    IEnumerable<T> Elements { get; }
    IEnumerable<T> UniqueElements { get; }
    bool Contains<G>(G t);
    BigInteger Select<G>(G t);
    IMultiSet<T> Update<G>(G t, BigInteger i);
    bool EqualsAux(IMultiSet<object> other);
    IMultiSet<U> DowncastClone<U>(Func<T, U> converter);
  }

  public class MultiSet<T> : IMultiSet<T> {
    readonly ImmutableDictionary<T, BigInteger> dict;
    readonly BigInteger occurrencesOfNull;  // stupidly, a Dictionary in .NET cannot use "null" as a key
    MultiSet(ImmutableDictionary<T, BigInteger>.Builder d, BigInteger occurrencesOfNull) {
      dict = d.ToImmutable();
      this.occurrencesOfNull = occurrencesOfNull;
    }
    public static readonly MultiSet<T> Empty = new MultiSet<T>(ImmutableDictionary<T, BigInteger>.Empty.ToBuilder(), BigInteger.Zero);

    private static readonly TypeDescriptor<IMultiSet<T>> _TYPE = new Dafny.TypeDescriptor<IMultiSet<T>>(Empty);
    public static TypeDescriptor<IMultiSet<T>> _TypeDescriptor() {
      return _TYPE;
    }

    public static MultiSet<T> FromIMultiSet(IMultiSet<T> s) {
      return s as MultiSet<T> ?? FromCollection(s.Elements);
    }
    public static MultiSet<T> FromElements(params T[] values) {
      var d = ImmutableDictionary<T, BigInteger>.Empty.ToBuilder();
      var occurrencesOfNull = BigInteger.Zero;
      foreach (T t in values) {
        if (t == null) {
          occurrencesOfNull++;
        } else {
          if (!d.TryGetValue(t, out var i)) {
            i = BigInteger.Zero;
          }
          d[t] = i + 1;
        }
      }
      return new MultiSet<T>(d, occurrencesOfNull);
    }

    public static MultiSet<T> FromCollection(IEnumerable<T> values) {
      var d = ImmutableDictionary<T, BigInteger>.Empty.ToBuilder();
      var occurrencesOfNull = BigInteger.Zero;
      foreach (T t in values) {
        if (t == null) {
          occurrencesOfNull++;
        } else {
          if (!d.TryGetValue(t,
                out var i)) {
            i = BigInteger.Zero;
          }

          d[t] = i + 1;
        }
      }

      return new MultiSet<T>(d,
        occurrencesOfNull);
    }

    public static MultiSet<T> FromSeq(ISequence<T> values) {
      var d = ImmutableDictionary<T, BigInteger>.Empty.ToBuilder();
      var occurrencesOfNull = BigInteger.Zero;
      foreach (var t in values) {
        if (t == null) {
          occurrencesOfNull++;
        } else {
          if (!d.TryGetValue(t,
                out var i)) {
            i = BigInteger.Zero;
          }

          d[t] = i + 1;
        }
      }

      return new MultiSet<T>(d,
        occurrencesOfNull);
    }
    public static MultiSet<T> FromSet(ISet<T> values) {
      var d = ImmutableDictionary<T, BigInteger>.Empty.ToBuilder();
      var containsNull = false;
      foreach (T t in values.Elements) {
        if (t == null) {
          containsNull = true;
        } else {
          d[t] = BigInteger.One;
        }
      }
      return new MultiSet<T>(d, containsNull ? BigInteger.One : BigInteger.Zero);
    }
    public IMultiSet<U> DowncastClone<U>(Func<T, U> converter) {
      if (this is IMultiSet<U> th) {
        return th;
      } else {
        var d = ImmutableDictionary<U, BigInteger>.Empty.ToBuilder();
        foreach (var item in this.dict) {
          var k = converter(item.Key);
          d.Add(k, item.Value);
        }
        return new MultiSet<U>(d, this.occurrencesOfNull);
      }
    }

    public bool Equals(IMultiSet<T> other) {
      return IsSubsetOf(this, other) && IsSubsetOf(other, this);
    }
    public override bool Equals(object other) {
      if (other is IMultiSet<T>) {
        return Equals((IMultiSet<T>)other);
      }
      var th = this as IMultiSet<object>;
      var oth = other as IMultiSet<object>;
      if (th != null && oth != null) {
        // See comment in Set.Equals
        return oth.EqualsAux(th);
      } else {
        return false;
      }
    }

    public bool EqualsAux(IMultiSet<object> other) {
      var s = other as IMultiSet<T>;
      if (s != null) {
        return Equals(s);
      } else {
        return false;
      }
    }

    public override int GetHashCode() {
      var hashCode = 1;
      if (occurrencesOfNull > 0) {
        var key = Dafny.Helpers.GetHashCode(default(T));
        key = (key << 3) | (key >> 29) ^ occurrencesOfNull.GetHashCode();
        hashCode = hashCode * (key + 3);
      }
      foreach (var kv in dict) {
        var key = Dafny.Helpers.GetHashCode(kv.Key);
        key = (key << 3) | (key >> 29) ^ kv.Value.GetHashCode();
        hashCode = hashCode * (key + 3);
      }
      return hashCode;
    }
    public override string ToString() {
      var s = "multiset{";
      var sep = "";
      for (var i = BigInteger.Zero; i < occurrencesOfNull; i++) {
        s += sep + Dafny.Helpers.ToString(default(T));
        sep = ", ";
      }
      foreach (var kv in dict) {
        var t = Dafny.Helpers.ToString(kv.Key);
        for (var i = BigInteger.Zero; i < kv.Value; i++) {
          s += sep + t;
          sep = ", ";
        }
      }
      return s + "}";
    }
    public static bool IsProperSubsetOf(IMultiSet<T> th, IMultiSet<T> other) {
      // Be sure to use ElementCount to avoid casting into 32 bits
      // integers that could lead to overflows (see https://github.com/dafny-lang/dafny/issues/5554)
      return th.ElementCount < other.ElementCount && IsSubsetOf(th, other);
    }
    public static bool IsSubsetOf(IMultiSet<T> th, IMultiSet<T> other) {
      var a = FromIMultiSet(th);
      var b = FromIMultiSet(other);
      if (b.occurrencesOfNull < a.occurrencesOfNull) {
        return false;
      }
      foreach (T t in a.dict.Keys) {
        if (b.dict.ContainsKey(t)) {
          if (b.dict[t] < a.dict[t]) {
            return false;
          }
        } else {
          if (a.dict[t] != BigInteger.Zero) {
            return false;
          }
        }
      }
      return true;
    }
    public static bool IsDisjointFrom(IMultiSet<T> th, IMultiSet<T> other) {
      foreach (T t in th.UniqueElements) {
        if (other.Contains(t)) {
          return false;
        }
      }
      return true;
    }

    public bool Contains<G>(G t) {
      return Select(t) != 0;
    }
    public BigInteger Select<G>(G t) {
      if (t == null) {
        return occurrencesOfNull;
      }

      if (t is T && dict.TryGetValue((T)(object)t, out var m)) {
        return m;
      } else {
        return BigInteger.Zero;
      }
    }
    public IMultiSet<T> Update<G>(G t, BigInteger i) {
      if (Select(t) == i) {
        return this;
      } else if (t == null) {
        var r = dict.ToBuilder();
        return new MultiSet<T>(r, i);
      } else {
        var r = dict.ToBuilder();
        r[(T)(object)t] = i;
        return new MultiSet<T>(r, occurrencesOfNull);
      }
    }
    public static IMultiSet<T> Union(IMultiSet<T> th, IMultiSet<T> other) {
      if (th.IsEmpty) {
        return other;
      } else if (other.IsEmpty) {
        return th;
      }
      var a = FromIMultiSet(th);
      var b = FromIMultiSet(other);
      var r = ImmutableDictionary<T, BigInteger>.Empty.ToBuilder();
      foreach (T t in a.dict.Keys) {
        if (!r.TryGetValue(t, out var i)) {
          i = BigInteger.Zero;
        }
        r[t] = i + a.dict[t];
      }
      foreach (T t in b.dict.Keys) {
        if (!r.TryGetValue(t, out var i)) {
          i = BigInteger.Zero;
        }
        r[t] = i + b.dict[t];
      }
      return new MultiSet<T>(r, a.occurrencesOfNull + b.occurrencesOfNull);
    }
    public static IMultiSet<T> Intersect(IMultiSet<T> th, IMultiSet<T> other) {
      if (th.IsEmpty) {
        return th;
      } else if (other.IsEmpty) {
        return other;
      }
      var a = FromIMultiSet(th);
      var b = FromIMultiSet(other);
      var r = ImmutableDictionary<T, BigInteger>.Empty.ToBuilder();
      foreach (T t in a.dict.Keys) {
        if (b.dict.ContainsKey(t)) {
          r.Add(t, a.dict[t] < b.dict[t] ? a.dict[t] : b.dict[t]);
        }
      }
      return new MultiSet<T>(r, a.occurrencesOfNull < b.occurrencesOfNull ? a.occurrencesOfNull : b.occurrencesOfNull);
    }
    public static IMultiSet<T> Difference(IMultiSet<T> th, IMultiSet<T> other) { // \result == this - other
      if (other.IsEmpty) {
        return th;
      }
      var a = FromIMultiSet(th);
      var b = FromIMultiSet(other);
      var r = ImmutableDictionary<T, BigInteger>.Empty.ToBuilder();
      foreach (T t in a.dict.Keys) {
        if (!b.dict.ContainsKey(t)) {
          r.Add(t, a.dict[t]);
        } else if (b.dict[t] < a.dict[t]) {
          r.Add(t, a.dict[t] - b.dict[t]);
        }
      }
      return new MultiSet<T>(r, b.occurrencesOfNull < a.occurrencesOfNull ? a.occurrencesOfNull - b.occurrencesOfNull : BigInteger.Zero);
    }

    public bool IsEmpty { get { return occurrencesOfNull == 0 && dict.IsEmpty; } }

    public int Count {
      get { return (int)ElementCount; }
    }
    public long LongCount {
      get { return (long)ElementCount; }
    }

    public BigInteger ElementCount {
      get {
        // This is inefficient
        var c = occurrencesOfNull;
        foreach (var item in dict) {
          c += item.Value;
        }
        return c;
      }
    }

    public IEnumerable<T> Elements {
      get {
        for (var i = BigInteger.Zero; i < occurrencesOfNull; i++) {
          yield return default(T);
        }
        foreach (var item in dict) {
          for (var i = BigInteger.Zero; i < item.Value; i++) {
            yield return item.Key;
          }
        }
      }
    }

    public IEnumerable<T> UniqueElements {
      get {
        if (!occurrencesOfNull.IsZero) {
          yield return default(T);
        }
        foreach (var key in dict.Keys) {
          if (dict[key] != 0) {
            yield return key;
          }
        }
      }
    }
  }

  public interface IMap<out U, out V> {
    int Count { get; }
    long LongCount { get; }
    ISet<U> Keys { get; }
    ISet<V> Values { get; }
    IEnumerable<IPair<U, V>> ItemEnumerable { get; }
    bool Contains<G>(G t);
    /// <summary>
    /// Returns "true" iff "this is IMap<object, object>" and "this" equals "other".
    /// </summary>
    bool EqualsObjObj(IMap<object, object> other);
    IMap<UU, VV> DowncastClone<UU, VV>(Func<U, UU> keyConverter, Func<V, VV> valueConverter);
  }

  public class Map<U, V> : IMap<U, V> {
    readonly ImmutableDictionary<U, V> dict;
    readonly bool hasNullKey;  // true when "null" is a key of the Map
    readonly V nullValue;  // if "hasNullKey", the value that "null" maps to

    private Map(ImmutableDictionary<U, V>.Builder d, bool hasNullKey, V nullValue) {
      dict = d.ToImmutable();
      this.hasNullKey = hasNullKey;
      this.nullValue = nullValue;
    }
    public static readonly Map<U, V> Empty = new Map<U, V>(ImmutableDictionary<U, V>.Empty.ToBuilder(), false, default(V));

    private Map(ImmutableDictionary<U, V> d, bool hasNullKey, V nullValue) {
      dict = d;
      this.hasNullKey = hasNullKey;
      this.nullValue = nullValue;
    }

    private static readonly TypeDescriptor<IMap<U, V>> _TYPE = new Dafny.TypeDescriptor<IMap<U, V>>(Empty);
    public static TypeDescriptor<IMap<U, V>> _TypeDescriptor() {
      return _TYPE;
    }

    public static Map<U, V> FromElements(params IPair<U, V>[] values) {
      var d = ImmutableDictionary<U, V>.Empty.ToBuilder();
      var hasNullKey = false;
      var nullValue = default(V);
      foreach (var p in values) {
        if (p.Car == null) {
          hasNullKey = true;
          nullValue = p.Cdr;
        } else {
          d[p.Car] = p.Cdr;
        }
      }
      return new Map<U, V>(d, hasNullKey, nullValue);
    }
    public static Map<U, V> FromCollection(IEnumerable<IPair<U, V>> values) {
      var d = ImmutableDictionary<U, V>.Empty.ToBuilder();
      var hasNullKey = false;
      var nullValue = default(V);
      foreach (var p in values) {
        if (p.Car == null) {
          hasNullKey = true;
          nullValue = p.Cdr;
        } else {
          d[p.Car] = p.Cdr;
        }
      }
      return new Map<U, V>(d, hasNullKey, nullValue);
    }
    public static Map<U, V> FromIMap(IMap<U, V> m) {
      return m as Map<U, V> ?? FromCollection(m.ItemEnumerable);
    }
    public IMap<UU, VV> DowncastClone<UU, VV>(Func<U, UU> keyConverter, Func<V, VV> valueConverter) {
      if (this is IMap<UU, VV> th) {
        return th;
      } else {
        var d = ImmutableDictionary<UU, VV>.Empty.ToBuilder();
        foreach (var item in this.dict) {
          var k = keyConverter(item.Key);
          var v = valueConverter(item.Value);
          d.Add(k, v);
        }
        return new Map<UU, VV>(d, this.hasNullKey, (VV)(object)this.nullValue);
      }
    }
    public int Count {
      get { return dict.Count + (hasNullKey ? 1 : 0); }
    }
    public long LongCount {
      get { return dict.Count + (hasNullKey ? 1 : 0); }
    }

    public bool Equals(IMap<U, V> other) {
      if (ReferenceEquals(this, other)) {
        return true;
      }

      if (other == null || LongCount != other.LongCount) {
        return false;
      }

      if (hasNullKey) {
        if (!other.Contains(default(U)) || !object.Equals(nullValue, Select(other, default(U)))) {
          return false;
        }
      }

      foreach (var item in dict) {
        if (!other.Contains(item.Key) || !object.Equals(item.Value, Select(other, item.Key))) {
          return false;
        }
      }
      return true;
    }
    public bool EqualsObjObj(IMap<object, object> other) {
      if (ReferenceEquals(this, other)) {
        return true;
      }
      if (!(this is IMap<object, object>) || other == null || LongCount != other.LongCount) {
        return false;
      }
      var oth = Map<object, object>.FromIMap(other);
      if (hasNullKey) {
        if (!oth.Contains(default(U)) || !object.Equals(nullValue, Map<object, object>.Select(oth, default(U)))) {
          return false;
        }
      }
      foreach (var item in dict) {
        if (!other.Contains(item.Key) || !object.Equals(item.Value, Map<object, object>.Select(oth, item.Key))) {
          return false;
        }
      }
      return true;
    }
    public override bool Equals(object other) {
      // See comment in Set.Equals
      var m = other as IMap<U, V>;
      if (m != null) {
        return Equals(m);
      }
      var imapoo = other as IMap<object, object>;
      if (imapoo != null) {
        return EqualsObjObj(imapoo);
      } else {
        return false;
      }
    }

    public override int GetHashCode() {
      var hashCode = 1;
      if (hasNullKey) {
        var key = Dafny.Helpers.GetHashCode(default(U));
        key = (key << 3) | (key >> 29) ^ Dafny.Helpers.GetHashCode(nullValue);
        hashCode = hashCode * (key + 3);
      }
      foreach (var kv in dict) {
        var key = Dafny.Helpers.GetHashCode(kv.Key);
        key = (key << 3) | (key >> 29) ^ Dafny.Helpers.GetHashCode(kv.Value);
        hashCode = hashCode * (key + 3);
      }
      return hashCode;
    }
    public override string ToString() {
      var s = "map[";
      var sep = "";
      if (hasNullKey) {
        s += sep + Dafny.Helpers.ToString(default(U)) + " := " + Dafny.Helpers.ToString(nullValue);
        sep = ", ";
      }
      foreach (var kv in dict) {
        s += sep + Dafny.Helpers.ToString(kv.Key) + " := " + Dafny.Helpers.ToString(kv.Value);
        sep = ", ";
      }
      return s + "]";
    }
    public bool Contains<G>(G u) {
      return u == null ? hasNullKey : u is U && dict.ContainsKey((U)(object)u);
    }
    public static V Select(IMap<U, V> th, U index) {
      // the following will throw an exception if "index" in not a key of the map
      var m = FromIMap(th);
      return index == null && m.hasNullKey ? m.nullValue : m.dict[index];
    }
    public static IMap<U, V> Update(IMap<U, V> th, U index, V val) {
      var m = FromIMap(th);
      var d = m.dict.ToBuilder();
      if (index == null) {
        return new Map<U, V>(d, true, val);
      } else {
        d[index] = val;
        return new Map<U, V>(d, m.hasNullKey, m.nullValue);
      }
    }

    public static IMap<U, V> Merge(IMap<U, V> th, IMap<U, V> other) {
      var a = FromIMap(th);
      var b = FromIMap(other);
      ImmutableDictionary<U, V> d = a.dict.SetItems(b.dict);
      return new Map<U, V>(d, a.hasNullKey || b.hasNullKey, b.hasNullKey ? b.nullValue : a.nullValue);
    }

    public static IMap<U, V> Subtract(IMap<U, V> th, ISet<U> keys) {
      var a = FromIMap(th);
      ImmutableDictionary<U, V> d = a.dict.RemoveRange(keys.Elements);
      return new Map<U, V>(d, a.hasNullKey && !keys.Contains<object>(null), a.nullValue);
    }

    public ISet<U> Keys {
      get {
        if (hasNullKey) {
          return Dafny.Set<U>.FromCollectionPlusOne(dict.Keys, default(U));
        } else {
          return Dafny.Set<U>.FromCollection(dict.Keys);
        }
      }
    }
    public ISet<V> Values {
      get {
        if (hasNullKey) {
          return Dafny.Set<V>.FromCollectionPlusOne(dict.Values, nullValue);
        } else {
          return Dafny.Set<V>.FromCollection(dict.Values);
        }
      }
    }

    public IEnumerable<IPair<U, V>> ItemEnumerable {
      get {
        if (hasNullKey) {
          yield return new Pair<U, V>(default(U), nullValue);
        }
        foreach (KeyValuePair<U, V> kvp in dict) {
          yield return new Pair<U, V>(kvp.Key, kvp.Value);
        }
      }
    }

    public static ISet<_System._ITuple2<U, V>> Items(IMap<U, V> m) {
      var result = new HashSet<_System._ITuple2<U, V>>();
      foreach (var item in m.ItemEnumerable) {
        result.Add(_System.Tuple2<U, V>.create(item.Car, item.Cdr));
      }
      return Dafny.Set<_System._ITuple2<U, V>>.FromCollection(result);
    }
  }

  public interface ISequence<out T> : IEnumerable<T> {
    long LongCount { get; }
    int Count { get; }
    [Obsolete("Use CloneAsArray() instead of Elements (both perform a copy).")]
    T[] Elements { get; }
    T[] CloneAsArray();
    IEnumerable<T> UniqueElements { get; }
    T Select(ulong index);
    T Select(long index);
    T Select(uint index);
    T Select(int index);
    T Select(BigInteger index);
    bool Contains<G>(G g);
    ISequence<T> Take(long m);
    ISequence<T> Take(ulong n);
    ISequence<T> Take(BigInteger n);
    ISequence<T> Drop(long m);
    ISequence<T> Drop(ulong n);
    ISequence<T> Drop(BigInteger n);
    ISequence<T> Subsequence(long lo, long hi);
    ISequence<T> Subsequence(long lo, ulong hi);
    ISequence<T> Subsequence(long lo, BigInteger hi);
    ISequence<T> Subsequence(ulong lo, long hi);
    ISequence<T> Subsequence(ulong lo, ulong hi);
    ISequence<T> Subsequence(ulong lo, BigInteger hi);
    ISequence<T> Subsequence(BigInteger lo, long hi);
    ISequence<T> Subsequence(BigInteger lo, ulong hi);
    ISequence<T> Subsequence(BigInteger lo, BigInteger hi);
    bool EqualsAux(ISequence<object> other);
    ISequence<U> DowncastClone<U>(Func<T, U> converter);
    string ToVerbatimString(bool asLiteral);
  }

  public abstract class Sequence<T> : ISequence<T> {
    public static readonly ISequence<T> Empty = new ArraySequence<T>(new T[0]);

    private static readonly TypeDescriptor<ISequence<T>> _TYPE = new Dafny.TypeDescriptor<ISequence<T>>(Empty);
    public static TypeDescriptor<ISequence<T>> _TypeDescriptor() {
      return _TYPE;
    }

    public static ISequence<T> Create(BigInteger length, System.Func<BigInteger, T> init) {
      var len = (int)length;
      var builder = ImmutableArray.CreateBuilder<T>(len);
      for (int i = 0; i < len; i++) {
        builder.Add(init(new BigInteger(i)));
      }
      return new ArraySequence<T>(builder.MoveToImmutable());
    }
    public static ISequence<T> FromArray(T[] values) {
      return new ArraySequence<T>(values);
    }
    public static ISequence<T> FromElements(params T[] values) {
      return new ArraySequence<T>(values);
    }
    public static ISequence<char> FromString(string s) {
      return new ArraySequence<char>(s.ToCharArray());
    }
    public static ISequence<Rune> UnicodeFromString(string s) {
      var runes = new List<Rune>();

      foreach (var rune in Rune.Enumerate(s)) {
        runes.Add(rune);
      }
      return new ArraySequence<Rune>(runes.ToArray());
    }

    public static ISequence<ISequence<char>> FromMainArguments(string[] args) {
      Dafny.ISequence<char>[] dafnyArgs = new Dafny.ISequence<char>[args.Length + 1];
      dafnyArgs[0] = Dafny.Sequence<char>.FromString("dotnet");
      for (var i = 0; i < args.Length; i++) {
        dafnyArgs[i + 1] = Dafny.Sequence<char>.FromString(args[i]);
      }

      return Sequence<ISequence<char>>.FromArray(dafnyArgs);
    }
    public static ISequence<ISequence<Rune>> UnicodeFromMainArguments(string[] args) {
      Dafny.ISequence<Rune>[] dafnyArgs = new Dafny.ISequence<Rune>[args.Length + 1];
      dafnyArgs[0] = Dafny.Sequence<Rune>.UnicodeFromString("dotnet");
      for (var i = 0; i < args.Length; i++) {
        dafnyArgs[i + 1] = Dafny.Sequence<Rune>.UnicodeFromString(args[i]);
      }

      return Sequence<ISequence<Rune>>.FromArray(dafnyArgs);
    }

    public ISequence<U> DowncastClone<U>(Func<T, U> converter) {
      if (this is ISequence<U> th) {
        return th;
      } else {
        var values = new U[this.LongCount];
        for (long i = 0; i < this.LongCount; i++) {
          var val = converter(this.Select(i));
          values[i] = val;
        }
        return new ArraySequence<U>(values);
      }
    }
    public static ISequence<T> Update(ISequence<T> sequence, long index, T t) {
      T[] tmp = sequence.CloneAsArray();
      tmp[index] = t;
      return new ArraySequence<T>(tmp);
    }
    public static ISequence<T> Update(ISequence<T> sequence, ulong index, T t) {
      return Update(sequence, (long)index, t);
    }
    public static ISequence<T> Update(ISequence<T> sequence, BigInteger index, T t) {
      return Update(sequence, (long)index, t);
    }
    public static bool EqualUntil(ISequence<T> left, ISequence<T> right, int n) {
      for (int i = 0; i < n; i++) {
        if (!Equals(left.Select(i), right.Select(i))) {
          return false;
        }
      }
      return true;
    }
    public static bool IsPrefixOf(ISequence<T> left, ISequence<T> right) {
      int n = left.Count;
      return n <= right.Count && EqualUntil(left, right, n);
    }
    public static bool IsProperPrefixOf(ISequence<T> left, ISequence<T> right) {
      int n = left.Count;
      return n < right.Count && EqualUntil(left, right, n);
    }
    public static ISequence<T> Concat(ISequence<T> left, ISequence<T> right) {
      if (left.Count == 0) {
        return right;
      }
      if (right.Count == 0) {
        return left;
      }
      return new ConcatSequence<T>(left, right);
    }
    // Make Count a public abstract instead of LongCount, since the "array size is limited to a total of 4 billion
    // elements, and to a maximum index of 0X7FEFFFFF". Therefore, as a protection, limit this to int32.
    // https://docs.microsoft.com/en-us/dotnet/api/system.array
    public abstract int Count { get; }
    public long LongCount {
      get { return Count; }
    }
    // ImmutableElements cannot be public in the interface since ImmutableArray<T> leads to a
    // "covariant type T occurs in invariant position" error. There do not appear to be interfaces for ImmutableArray<T>
    // that resolve this.
    internal abstract ImmutableArray<T> ImmutableElements { get; }

    public T[] Elements { get { return CloneAsArray(); } }

    public T[] CloneAsArray() {
      return ImmutableElements.ToArray();
    }

    public IEnumerable<T> UniqueElements {
      get {
        return Set<T>.FromCollection(ImmutableElements).Elements;
      }
    }

    public IEnumerator<T> GetEnumerator() {
      foreach (var el in ImmutableElements) {
        yield return el;
      }
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return GetEnumerator();
    }

    public T Select(ulong index) {
      return ImmutableElements[checked((int)index)];
    }
    public T Select(long index) {
      return ImmutableElements[checked((int)index)];
    }
    public T Select(uint index) {
      return ImmutableElements[checked((int)index)];
    }
    public T Select(int index) {
      return ImmutableElements[index];
    }
    public T Select(BigInteger index) {
      return ImmutableElements[(int)index];
    }
    public bool Equals(ISequence<T> other) {
      return ReferenceEquals(this, other) || (Count == other.Count && EqualUntil(this, other, Count));
    }
    public override bool Equals(object other) {
      if (other is ISequence<T>) {
        return Equals((ISequence<T>)other);
      }
      var th = this as ISequence<object>;
      var oth = other as ISequence<object>;
      if (th != null && oth != null) {
        // see explanation in Set.Equals
        return oth.EqualsAux(th);
      } else {
        return false;
      }
    }
    public bool EqualsAux(ISequence<object> other) {
      var s = other as ISequence<T>;
      if (s != null) {
        return Equals(s);
      } else {
        return false;
      }
    }
    public override int GetHashCode() {
      ImmutableArray<T> elmts = ImmutableElements;
      // https://devblogs.microsoft.com/dotnet/please-welcome-immutablearrayt/
      if (elmts.IsDefaultOrEmpty) {
        return 0;
      }

      var hashCode = 0;
      for (var i = 0; i < elmts.Length; i++) {
        hashCode = (hashCode << 3) | (hashCode >> 29) ^ Dafny.Helpers.GetHashCode(elmts[i]);
      }
      return hashCode;
    }
    public override string ToString() {
      if (typeof(T) == typeof(char)) {
        return string.Concat(this);
      } else {
        return "[" + string.Join(", ", ImmutableElements.Select(Dafny.Helpers.ToString)) + "]";
      }
    }

    public string ToVerbatimString(bool asLiteral) {
      var builder = new System.Text.StringBuilder();
      if (asLiteral) {
        builder.Append('"');
      }
      foreach (var c in this) {
        var rune = (Rune)(object)c;
        if (asLiteral) {
          builder.Append(Helpers.EscapeCharacter(rune));
        } else {
          builder.Append(char.ConvertFromUtf32(rune.Value));
        }
      }
      if (asLiteral) {
        builder.Append('"');
      }
      return builder.ToString();
    }

    public bool Contains<G>(G g) {
      if (g == null || g is T) {
        var t = (T)(object)g;
        return ImmutableElements.Contains(t);
      }
      return false;
    }
    public ISequence<T> Take(long m) {
      return Subsequence(0, m);
    }
    public ISequence<T> Take(ulong n) {
      return Take((long)n);
    }
    public ISequence<T> Take(BigInteger n) {
      return Take((long)n);
    }
    public ISequence<T> Drop(long m) {
      return Subsequence(m, Count);
    }
    public ISequence<T> Drop(ulong n) {
      return Drop((long)n);
    }
    public ISequence<T> Drop(BigInteger n) {
      return Drop((long)n);
    }
    public ISequence<T> Subsequence(long lo, long hi) {
      if (lo == 0 && hi == Count) {
        return this;
      }
      int startingIndex = checked((int)lo);
      var length = checked((int)hi) - startingIndex;
      return new ArraySequence<T>(ImmutableArray.Create<T>(ImmutableElements, startingIndex, length));
    }
    public ISequence<T> Subsequence(long lo, ulong hi) {
      return Subsequence(lo, (long)hi);
    }
    public ISequence<T> Subsequence(long lo, BigInteger hi) {
      return Subsequence(lo, (long)hi);
    }
    public ISequence<T> Subsequence(ulong lo, long hi) {
      return Subsequence((long)lo, hi);
    }
    public ISequence<T> Subsequence(ulong lo, ulong hi) {
      return Subsequence((long)lo, (long)hi);
    }
    public ISequence<T> Subsequence(ulong lo, BigInteger hi) {
      return Subsequence((long)lo, (long)hi);
    }
    public ISequence<T> Subsequence(BigInteger lo, long hi) {
      return Subsequence((long)lo, hi);
    }
    public ISequence<T> Subsequence(BigInteger lo, ulong hi) {
      return Subsequence((long)lo, (long)hi);
    }
    public ISequence<T> Subsequence(BigInteger lo, BigInteger hi) {
      return Subsequence((long)lo, (long)hi);
    }
  }

  internal class ArraySequence<T> : Sequence<T> {
    private readonly ImmutableArray<T> elmts;

    internal ArraySequence(ImmutableArray<T> ee) {
      elmts = ee;
    }
    internal ArraySequence(T[] ee) {
      elmts = ImmutableArray.Create<T>(ee);
    }

    internal override ImmutableArray<T> ImmutableElements {
      get {
        return elmts;
      }
    }

    public override int Count {
      get {
        return elmts.Length;
      }
    }
  }

  internal class ConcatSequence<T> : Sequence<T> {
    // INVARIANT: Either left != null, right != null, and elmts's underlying array == null or
    // left == null, right == null, and elmts's underlying array != null
    internal volatile ISequence<T> left, right;
    internal ImmutableArray<T> elmts;
    private readonly int count;

    internal ConcatSequence(ISequence<T> left, ISequence<T> right) {
      this.left = left;
      this.right = right;
      this.count = left.Count + right.Count;
    }

    internal override ImmutableArray<T> ImmutableElements {
      get {
        // IsDefault returns true if the underlying array is a null reference
        // https://devblogs.microsoft.com/dotnet/please-welcome-immutablearrayt/
        if (elmts.IsDefault) {
          elmts = ComputeElements();
          // We don't need the original sequences anymore; let them be
          // garbage-collected
          left = null;
          right = null;
        }
        return elmts;
      }
    }

    public override int Count {
      get {
        return count;
      }
    }

    internal ImmutableArray<T> ComputeElements() {
      // Traverse the tree formed by all descendants which are ConcatSequences
      var ansBuilder = ImmutableArray.CreateBuilder<T>(count);
      var toVisit = new Stack<ISequence<T>>();
      var leftBuffer = left;
      var rightBuffer = right;
      if (left == null || right == null) {
        // elmts can't be .IsDefault while either left, or right are null
        return elmts;
      }
      toVisit.Push(rightBuffer);
      toVisit.Push(leftBuffer);

      while (toVisit.Count != 0) {
        var seq = toVisit.Pop();
        if (seq is ConcatSequence<T> cs && cs.elmts.IsDefault) {
          leftBuffer = cs.left;
          rightBuffer = cs.right;
          if (cs.left == null || cs.right == null) {
            // !cs.elmts.IsDefault, due to concurrent enumeration
            toVisit.Push(cs);
          } else {
            toVisit.Push(rightBuffer);
            toVisit.Push(leftBuffer);
          }
        } else {
          if (seq is Sequence<T> sq) {
            ansBuilder.AddRange(sq.ImmutableElements); // Optimized path for ImmutableArray
          } else {
            ansBuilder.AddRange(seq); // Slower path using IEnumerable
          }
        }
      }
      return ansBuilder.MoveToImmutable();
    }
  }

  public interface IPair<out A, out B> {
    A Car { get; }
    B Cdr { get; }
  }

  public class Pair<A, B> : IPair<A, B> {
    private A car;
    private B cdr;
    public A Car { get { return car; } }
    public B Cdr { get { return cdr; } }
    public Pair(A a, B b) {
      this.car = a;
      this.cdr = b;
    }
  }

  public class TypeDescriptor<T> {
    private readonly T initValue;
    public TypeDescriptor(T initValue) {
      this.initValue = initValue;
    }
    public T Default() {
      return initValue;
    }
  }

  public partial class Helpers {
    public static int GetHashCode<G>(G g) {
      return g == null ? 1001 : g.GetHashCode();
    }

    public static int ToIntChecked(BigInteger i, string msg) {
      if (i > Int32.MaxValue || i < Int32.MinValue) {
        if (msg == null) {
          msg = "value out of range for a 32-bit int";
        }

        throw new HaltException(msg + ": " + i);
      }
      return (int)i;
    }
    public static int ToIntChecked(long i, string msg) {
      if (i > Int32.MaxValue || i < Int32.MinValue) {
        if (msg == null) {
          msg = "value out of range for a 32-bit int";
        }

        throw new HaltException(msg + ": " + i);
      }
      return (int)i;
    }
    public static int ToIntChecked(int i, string msg) {
      return i;
    }

    public static string ToString<G>(G g) {
      if (g == null) {
        return "null";
      } else if (g is bool) {
        return (bool)(object)g ? "true" : "false";  // capitalize boolean literals like in Dafny
      } else if (g is Rune) {
        return "'" + EscapeCharacter((Rune)(object)g) + "'";
      } else {
        return g.ToString();
      }
    }

    public static string EscapeCharacter(Rune r) {
      switch (r.Value) {
        case '\n': return "\\n";
        case '\r': return "\\r";
        case '\t': return "\\t";
        case '\0': return "\\0";
        case '\'': return "\\'";
        case '\"': return "\\\"";
        case '\\': return "\\\\";
        default: return r.ToString();
      };
    }

    public static void Print<G>(G g) {
      System.Console.Write(ToString(g));
    }

    public static readonly TypeDescriptor<bool> BOOL = new TypeDescriptor<bool>(false);
    public static readonly TypeDescriptor<char> CHAR = new TypeDescriptor<char>('D');  // See CharType.DefaultValue in Dafny source code
    public static readonly TypeDescriptor<Rune> RUNE = new TypeDescriptor<Rune>(new Rune('D'));  // See CharType.DefaultValue in Dafny source code
    public static readonly TypeDescriptor<BigInteger> INT = new TypeDescriptor<BigInteger>(BigInteger.Zero);
    public static readonly TypeDescriptor<BigRational> REAL = new TypeDescriptor<BigRational>(BigRational.ZERO);
    public static readonly TypeDescriptor<byte> UINT8 = new TypeDescriptor<byte>(0);
    public static readonly TypeDescriptor<ushort> UINT16 = new TypeDescriptor<ushort>(0);
    public static readonly TypeDescriptor<uint> UINT32 = new TypeDescriptor<uint>(0);
    public static readonly TypeDescriptor<ulong> UINT64 = new TypeDescriptor<ulong>(0);

    public static TypeDescriptor<T> NULL<T>() where T : class {
      return new TypeDescriptor<T>(null);
    }

    public static TypeDescriptor<A[]> ARRAY<A>() {
      return new TypeDescriptor<A[]>(new A[0]);
    }

    public static bool Quantifier<T>(IEnumerable<T> vals, bool frall, System.Predicate<T> pred) {
      foreach (var u in vals) {
        if (pred(u) != frall) { return !frall; }
      }
      return frall;
    }
    // Enumerating other collections
    public static IEnumerable<bool> AllBooleans() {
      yield return false;
      yield return true;
    }
    public static IEnumerable<char> AllChars() {
      for (int i = 0; i < 0x1_0000; i++) {
        yield return (char)i;
      }
    }
    public static IEnumerable<Rune> AllUnicodeChars() {
      for (int i = 0; i < 0xD800; i++) {
        yield return new Rune(i);
      }
      for (int i = 0xE000; i < 0x11_0000; i++) {
        yield return new Rune(i);
      }
    }
    public static IEnumerable<BigInteger> AllIntegers() {
      yield return new BigInteger(0);
      for (var j = new BigInteger(1); ; j++) {
        yield return j;
        yield return -j;
      }
    }
    public static IEnumerable<BigInteger> IntegerRange(Nullable<BigInteger> lo, Nullable<BigInteger> hi) {
      if (lo == null) {
        for (var j = (BigInteger)hi; true;) {
          j--;
          yield return j;
        }
      } else if (hi == null) {
        for (var j = (BigInteger)lo; true; j++) {
          yield return j;
        }
      } else {
        for (var j = (BigInteger)lo; j < hi; j++) {
          yield return j;
        }
      }
    }
    public static IEnumerable<T> SingleValue<T>(T e) {
      yield return e;
    }
    // pre: b != 0
    // post: result == a/b, as defined by Euclidean Division (http://en.wikipedia.org/wiki/Modulo_operation)
    public static sbyte EuclideanDivision_sbyte(sbyte a, sbyte b) {
      return (sbyte)EuclideanDivision_int(a, b);
    }
    public static short EuclideanDivision_short(short a, short b) {
      return (short)EuclideanDivision_int(a, b);
    }
    public static int EuclideanDivision_int(int a, int b) {
      if (0 <= a) {
        if (0 <= b) {
          // +a +b: a/b
          return (int)(((uint)(a)) / ((uint)(b)));
        } else {
          // +a -b: -(a/(-b))
          return -((int)(((uint)(a)) / ((uint)(unchecked(-b)))));
        }
      } else {
        if (0 <= b) {
          // -a +b: -((-a-1)/b) - 1
          return -((int)(((uint)(-(a + 1))) / ((uint)(b)))) - 1;
        } else {
          // -a -b: ((-a-1)/(-b)) + 1
          return ((int)(((uint)(-(a + 1))) / ((uint)(unchecked(-b))))) + 1;
        }
      }
    }
    public static long EuclideanDivision_long(long a, long b) {
      if (0 <= a) {
        if (0 <= b) {
          // +a +b: a/b
          return (long)(((ulong)(a)) / ((ulong)(b)));
        } else {
          // +a -b: -(a/(-b))
          return -((long)(((ulong)(a)) / ((ulong)(unchecked(-b)))));
        }
      } else {
        if (0 <= b) {
          // -a +b: -((-a-1)/b) - 1
          return -((long)(((ulong)(-(a + 1))) / ((ulong)(b)))) - 1;
        } else {
          // -a -b: ((-a-1)/(-b)) + 1
          return ((long)(((ulong)(-(a + 1))) / ((ulong)(unchecked(-b))))) + 1;
        }
      }
    }
    public static BigInteger EuclideanDivision(BigInteger a, BigInteger b) {
      if (0 <= a.Sign) {
        if (0 <= b.Sign) {
          // +a +b: a/b
          return BigInteger.Divide(a, b);
        } else {
          // +a -b: -(a/(-b))
          return BigInteger.Negate(BigInteger.Divide(a, BigInteger.Negate(b)));
        }
      } else {
        if (0 <= b.Sign) {
          // -a +b: -((-a-1)/b) - 1
          return BigInteger.Negate(BigInteger.Divide(BigInteger.Negate(a) - 1, b)) - 1;
        } else {
          // -a -b: ((-a-1)/(-b)) + 1
          return BigInteger.Divide(BigInteger.Negate(a) - 1, BigInteger.Negate(b)) + 1;
        }
      }
    }
    // pre: b != 0
    // post: result == a%b, as defined by Euclidean Division (http://en.wikipedia.org/wiki/Modulo_operation)
    public static sbyte EuclideanModulus_sbyte(sbyte a, sbyte b) {
      return (sbyte)EuclideanModulus_int(a, b);
    }
    public static short EuclideanModulus_short(short a, short b) {
      return (short)EuclideanModulus_int(a, b);
    }
    public static int EuclideanModulus_int(int a, int b) {
      uint bp = (0 <= b) ? (uint)b : (uint)(unchecked(-b));
      if (0 <= a) {
        // +a: a % b'
        return (int)(((uint)a) % bp);
      } else {
        // c = ((-a) % b')
        // -a: b' - c if c > 0
        // -a: 0 if c == 0
        uint c = ((uint)(unchecked(-a))) % bp;
        return (int)(c == 0 ? c : bp - c);
      }
    }
    public static long EuclideanModulus_long(long a, long b) {
      ulong bp = (0 <= b) ? (ulong)b : (ulong)(unchecked(-b));
      if (0 <= a) {
        // +a: a % b'
        return (long)(((ulong)a) % bp);
      } else {
        // c = ((-a) % b')
        // -a: b' - c if c > 0
        // -a: 0 if c == 0
        ulong c = ((ulong)(unchecked(-a))) % bp;
        return (long)(c == 0 ? c : bp - c);
      }
    }
    public static BigInteger EuclideanModulus(BigInteger a, BigInteger b) {
      var bp = BigInteger.Abs(b);
      if (0 <= a.Sign) {
        // +a: a % b'
        return BigInteger.Remainder(a, bp);
      } else {
        // c = ((-a) % b')
        // -a: b' - c if c > 0
        // -a: 0 if c == 0
        var c = BigInteger.Remainder(BigInteger.Negate(a), bp);
        return c.IsZero ? c : BigInteger.Subtract(bp, c);
      }
    }

    public static U CastConverter<T, U>(T t) {
      return (U)(object)t;
    }

    public static Sequence<T> SeqFromArray<T>(T[] array) {
      return new ArraySequence<T>(array);
    }
    // In .NET version 4.5, it is possible to mark a method with "AggressiveInlining", which says to inline the
    // method if possible.  Method "ExpressionSequence" would be a good candidate for it:
    // [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    public static U ExpressionSequence<T, U>(T t, U u) {
      return u;
    }

    public static U Let<T, U>(T t, Func<T, U> f) {
      return f(t);
    }

    public static A Id<A>(A a) {
      return a;
    }

    public static void WithHaltHandling(Action action) {
      try {
        action();
      } catch (HaltException e) {
        Console.WriteLine("[Program halted] " + e.Message);
        // This is unfriendly given that Dafny's C# compiler will
        // invoke the compiled main method directly,
        // so we might be exiting the whole Dafny process here.
        // That's the best we can do until Dafny main methods support
        // a return value though (https://github.com/dafny-lang/dafny/issues/2699).
        // If we just set Environment.ExitCode here, the Dafny CLI
        // will just override that with 0.
        Environment.Exit(1);
      }
    }

    public static Rune AddRunes(Rune left, Rune right) {
      return (Rune)(left.Value + right.Value);
    }

    public static Rune SubtractRunes(Rune left, Rune right) {
      return (Rune)(left.Value - right.Value);
    }

    public static uint Bv32ShiftLeft(uint a, int amount) {
      return 32 <= amount ? 0 : a << amount;
    }
    public static ulong Bv64ShiftLeft(ulong a, int amount) {
      return 64 <= amount ? 0 : a << amount;
    }

    public static uint Bv32ShiftRight(uint a, int amount) {
      return 32 <= amount ? 0 : a >> amount;
    }
    public static ulong Bv64ShiftRight(ulong a, int amount) {
      return 64 <= amount ? 0 : a >> amount;
    }
  }

  public class BigOrdinal {
    public static bool IsLimit(BigInteger ord) {
      return ord == 0;
    }
    public static bool IsSucc(BigInteger ord) {
      return 0 < ord;
    }
    public static BigInteger Offset(BigInteger ord) {
      return ord;
    }
    public static bool IsNat(BigInteger ord) {
      return true;  // at run time, every ORDINAL is a natural number
    }
  }

  public struct BigRational {
    public static readonly BigRational ZERO = new BigRational(0);

    // We need to deal with the special case "num == 0 && den == 0", because
    // that's what C#'s default struct constructor will produce for BigRational. :(
    // To deal with it, we ignore "den" when "num" is 0.
    public readonly BigInteger num, den;  // invariant 1 <= den || (num == 0 && den == 0)

    public override string ToString() {
      if (num.IsZero || den.IsOne) {
        return string.Format("{0}.0", num);
      } else if (DividesAPowerOf10(den, out var factor, out var log10)) {
        var n = num * factor;
        string sign;
        string digits;
        if (n.Sign < 0) {
          sign = "-"; digits = (-n).ToString();
        } else {
          sign = ""; digits = n.ToString();
        }
        if (log10 < digits.Length) {
          var digitCount = digits.Length - log10;
          return string.Format("{0}{1}.{2}", sign, digits.Substring(0, digitCount), digits.Substring(digitCount));
        } else {
          return string.Format("{0}0.{1}{2}", sign, new string('0', log10 - digits.Length), digits);
        }
      } else {
        return string.Format("({0}.0 / {1}.0)", num, den);
      }
    }
    public static bool IsPowerOf10(BigInteger x, out int log10) {
      log10 = 0;
      if (x.IsZero) {
        return false;
      }
      while (true) {  // invariant: x != 0 && x * 10^log10 == old(x)
        if (x.IsOne) {
          return true;
        } else if (x % 10 == 0) {
          log10++;
          x /= 10;
        } else {
          return false;
        }
      }
    }
    /// <summary>
    /// If this method return true, then
    ///     10^log10 == factor * i
    /// Otherwise, factor and log10 should not be used.
    /// </summary>
    public static bool DividesAPowerOf10(BigInteger i, out BigInteger factor, out int log10) {
      factor = BigInteger.One;
      log10 = 0;
      if (i <= 0) {
        return false;
      }

      BigInteger ten = 10;
      BigInteger five = 5;
      BigInteger two = 2;

      // invariant: 1 <= i && i * 10^log10 == factor * old(i)
      while (i % ten == 0) {
        i /= ten;
        log10++;
      }

      while (i % five == 0) {
        i /= five;
        factor *= two;
        log10++;
      }
      while (i % two == 0) {
        i /= two;
        factor *= five;
        log10++;
      }

      return i == BigInteger.One;
    }

    public BigRational(int n) {
      num = new BigInteger(n);
      den = BigInteger.One;
    }
    public BigRational(uint n) {
      num = new BigInteger(n);
      den = BigInteger.One;
    }
    public BigRational(long n) {
      num = new BigInteger(n);
      den = BigInteger.One;
    }
    public BigRational(ulong n) {
      num = new BigInteger(n);
      den = BigInteger.One;
    }
    public BigRational(BigInteger n, BigInteger d) {
      // requires 1 <= d
      num = n;
      den = d;
    }
    /// <summary>
    /// Construct an exact rational representation of a double value.
    /// Throw an exception on NaN or infinite values. Does not support
    /// subnormal values, though it would be possible to extend it to.
    /// </summary>
    public BigRational(double n) {
      if (Double.IsNaN(n)) {
        throw new ArgumentException("Can't convert NaN to a rational.");
      }
      if (Double.IsInfinity(n)) {
        throw new ArgumentException(
          "Can't convert +/- infinity to a rational.");
      }

      // Double-specific values
      const int exptBias = 1023;
      const ulong signMask = 0x8000000000000000;
      const ulong exptMask = 0x7FF0000000000000;
      const ulong mantMask = 0x000FFFFFFFFFFFFF;
      const int mantBits = 52;
      ulong bits = BitConverter.ToUInt64(BitConverter.GetBytes(n), 0);

      // Generic conversion
      bool isNeg = (bits & signMask) != 0;
      int expt = ((int)((bits & exptMask) >> mantBits)) - exptBias;
      var mant = (bits & mantMask);

      if (expt == -exptBias && mant != 0) {
        throw new ArgumentException(
          "Can't convert a subnormal value to a rational (yet).");
      }

      var one = BigInteger.One;
      var negFactor = isNeg ? BigInteger.Negate(one) : one;
      var two = new BigInteger(2);
      var exptBI = BigInteger.Pow(two, Math.Abs(expt));
      var twoToMantBits = BigInteger.Pow(two, mantBits);
      var mantNum = negFactor * (twoToMantBits + new BigInteger(mant));
      if (expt == -exptBias && mant == 0) {
        num = den = 0;
      } else if (expt < 0) {
        num = mantNum;
        den = twoToMantBits * exptBI;
      } else {
        num = exptBI * mantNum;
        den = twoToMantBits;
      }
    }
    public BigInteger ToBigInteger() {
      if (num.IsZero || den.IsOne) {
        return num;
      } else if (0 < num.Sign) {
        return num / den;
      } else {
        return (num - den + 1) / den;
      }
    }

    public bool IsInteger() {
      var floored = new BigRational(this.ToBigInteger(), BigInteger.One);
      return this == floored;
    }

    /// <summary>
    /// Returns values such that aa/dd == a and bb/dd == b.
    /// </summary>
    private static void Normalize(BigRational a, BigRational b, out BigInteger aa, out BigInteger bb, out BigInteger dd) {
      if (a.num.IsZero) {
        aa = a.num;
        bb = b.num;
        dd = b.den;
      } else if (b.num.IsZero) {
        aa = a.num;
        dd = a.den;
        bb = b.num;
      } else {
        var gcd = BigInteger.GreatestCommonDivisor(a.den, b.den);
        var xx = a.den / gcd;
        var yy = b.den / gcd;
        // We now have a == a.num / (xx * gcd) and b == b.num / (yy * gcd).
        aa = a.num * yy;
        bb = b.num * xx;
        dd = a.den * yy;
      }
    }
    public int CompareTo(BigRational that) {
      // simple things first
      int asign = this.num.Sign;
      int bsign = that.num.Sign;
      if (asign < 0 && 0 <= bsign) {
        return -1;
      } else if (asign <= 0 && 0 < bsign) {
        return -1;
      } else if (bsign < 0 && 0 <= asign) {
        return 1;
      } else if (bsign <= 0 && 0 < asign) {
        return 1;
      }

      Normalize(this, that, out var aa, out var bb, out var dd);
      return aa.CompareTo(bb);
    }
    public int Sign {
      get {
        return num.Sign;
      }
    }
    public override int GetHashCode() {
      return num.GetHashCode() + 29 * den.GetHashCode();
    }
    public override bool Equals(object obj) {
      if (obj is BigRational) {
        return this == (BigRational)obj;
      } else {
        return false;
      }
    }
    public static bool operator ==(BigRational a, BigRational b) {
      return a.CompareTo(b) == 0;
    }
    public static bool operator !=(BigRational a, BigRational b) {
      return a.CompareTo(b) != 0;
    }
    public static bool operator >(BigRational a, BigRational b) {
      return a.CompareTo(b) > 0;
    }
    public static bool operator >=(BigRational a, BigRational b) {
      return a.CompareTo(b) >= 0;
    }
    public static bool operator <(BigRational a, BigRational b) {
      return a.CompareTo(b) < 0;
    }
    public static bool operator <=(BigRational a, BigRational b) {
      return a.CompareTo(b) <= 0;
    }
    public static BigRational operator +(BigRational a, BigRational b) {
      Normalize(a, b, out var aa, out var bb, out var dd);
      return new BigRational(aa + bb, dd);
    }
    public static BigRational operator -(BigRational a, BigRational b) {
      Normalize(a, b, out var aa, out var bb, out var dd);
      return new BigRational(aa - bb, dd);
    }
    public static BigRational operator -(BigRational a) {
      return new BigRational(-a.num, a.den);
    }
    public static BigRational operator *(BigRational a, BigRational b) {
      return new BigRational(a.num * b.num, a.den * b.den);
    }
    public static BigRational operator /(BigRational a, BigRational b) {
      // Compute the reciprocal of b
      BigRational bReciprocal;
      if (0 < b.num.Sign) {
        bReciprocal = new BigRational(b.den, b.num);
      } else {
        // this is the case b.num < 0
        bReciprocal = new BigRational(-b.den, -b.num);
      }
      return a * bReciprocal;
    }
  }

  public class HaltException : Exception {
    public HaltException(object message) : base(message.ToString()) {
    }
  }
}
// Dafny program systemModulePopulator.dfy compiled into C#
// To recompile, you will need the libraries
//     System.Runtime.Numerics.dll System.Collections.Immutable.dll
// but the 'dotnet' tool in .NET should pick those up automatically.
// Optionally, you may want to include compiler switches like
//     /debug /nowarn:162,164,168,183,219,436,1717,1718

#if ISDAFNYRUNTIMELIB
using System;
using System.Numerics;
using System.Collections;
#endif
#if ISDAFNYRUNTIMELIB
namespace Dafny {
  internal class ArrayHelpers {
    public static T[] InitNewArray1<T>(T z, BigInteger size0) {
      int s0 = (int)size0;
      T[] a = new T[s0];
      for (int i0 = 0; i0 < s0; i0++) {
        a[i0] = z;
      }
      return a;
    }
    public static T[,] InitNewArray2<T>(T z, BigInteger size0, BigInteger size1) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      T[,] a = new T[s0,s1];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          a[i0,i1] = z;
        }
      }
      return a;
    }
    public static T[,,] InitNewArray3<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      T[,,] a = new T[s0,s1,s2];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            a[i0,i1,i2] = z;
          }
        }
      }
      return a;
    }
    public static T[,,,] InitNewArray4<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      T[,,,] a = new T[s0,s1,s2,s3];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              a[i0,i1,i2,i3] = z;
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,] InitNewArray5<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      T[,,,,] a = new T[s0,s1,s2,s3,s4];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                a[i0,i1,i2,i3,i4] = z;
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,] InitNewArray6<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      T[,,,,,] a = new T[s0,s1,s2,s3,s4,s5];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  a[i0,i1,i2,i3,i4,i5] = z;
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,] InitNewArray7<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      T[,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    a[i0,i1,i2,i3,i4,i5,i6] = z;
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,] InitNewArray8<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      T[,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      a[i0,i1,i2,i3,i4,i5,i6,i7] = z;
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,,] InitNewArray9<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7, BigInteger size8) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      int s8 = (int)size8;
      T[,,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7,s8];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      for (int i8 = 0; i8 < s8; i8++) {
                        a[i0,i1,i2,i3,i4,i5,i6,i7,i8] = z;
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,,,] InitNewArray10<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7, BigInteger size8, BigInteger size9) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      int s8 = (int)size8;
      int s9 = (int)size9;
      T[,,,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7,s8,s9];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      for (int i8 = 0; i8 < s8; i8++) {
                        for (int i9 = 0; i9 < s9; i9++) {
                          a[i0,i1,i2,i3,i4,i5,i6,i7,i8,i9] = z;
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,,,,] InitNewArray11<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7, BigInteger size8, BigInteger size9, BigInteger size10) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      int s8 = (int)size8;
      int s9 = (int)size9;
      int s10 = (int)size10;
      T[,,,,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7,s8,s9,s10];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      for (int i8 = 0; i8 < s8; i8++) {
                        for (int i9 = 0; i9 < s9; i9++) {
                          for (int i10 = 0; i10 < s10; i10++) {
                            a[i0,i1,i2,i3,i4,i5,i6,i7,i8,i9,i10] = z;
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,,,,,] InitNewArray12<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7, BigInteger size8, BigInteger size9, BigInteger size10, BigInteger size11) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      int s8 = (int)size8;
      int s9 = (int)size9;
      int s10 = (int)size10;
      int s11 = (int)size11;
      T[,,,,,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7,s8,s9,s10,s11];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      for (int i8 = 0; i8 < s8; i8++) {
                        for (int i9 = 0; i9 < s9; i9++) {
                          for (int i10 = 0; i10 < s10; i10++) {
                            for (int i11 = 0; i11 < s11; i11++) {
                              a[i0,i1,i2,i3,i4,i5,i6,i7,i8,i9,i10,i11] = z;
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,,,,,,] InitNewArray13<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7, BigInteger size8, BigInteger size9, BigInteger size10, BigInteger size11, BigInteger size12) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      int s8 = (int)size8;
      int s9 = (int)size9;
      int s10 = (int)size10;
      int s11 = (int)size11;
      int s12 = (int)size12;
      T[,,,,,,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7,s8,s9,s10,s11,s12];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      for (int i8 = 0; i8 < s8; i8++) {
                        for (int i9 = 0; i9 < s9; i9++) {
                          for (int i10 = 0; i10 < s10; i10++) {
                            for (int i11 = 0; i11 < s11; i11++) {
                              for (int i12 = 0; i12 < s12; i12++) {
                                a[i0,i1,i2,i3,i4,i5,i6,i7,i8,i9,i10,i11,i12] = z;
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,,,,,,,] InitNewArray14<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7, BigInteger size8, BigInteger size9, BigInteger size10, BigInteger size11, BigInteger size12, BigInteger size13) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      int s8 = (int)size8;
      int s9 = (int)size9;
      int s10 = (int)size10;
      int s11 = (int)size11;
      int s12 = (int)size12;
      int s13 = (int)size13;
      T[,,,,,,,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7,s8,s9,s10,s11,s12,s13];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      for (int i8 = 0; i8 < s8; i8++) {
                        for (int i9 = 0; i9 < s9; i9++) {
                          for (int i10 = 0; i10 < s10; i10++) {
                            for (int i11 = 0; i11 < s11; i11++) {
                              for (int i12 = 0; i12 < s12; i12++) {
                                for (int i13 = 0; i13 < s13; i13++) {
                                  a[i0,i1,i2,i3,i4,i5,i6,i7,i8,i9,i10,i11,i12,i13] = z;
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,,,,,,,,] InitNewArray15<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7, BigInteger size8, BigInteger size9, BigInteger size10, BigInteger size11, BigInteger size12, BigInteger size13, BigInteger size14) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      int s8 = (int)size8;
      int s9 = (int)size9;
      int s10 = (int)size10;
      int s11 = (int)size11;
      int s12 = (int)size12;
      int s13 = (int)size13;
      int s14 = (int)size14;
      T[,,,,,,,,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7,s8,s9,s10,s11,s12,s13,s14];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      for (int i8 = 0; i8 < s8; i8++) {
                        for (int i9 = 0; i9 < s9; i9++) {
                          for (int i10 = 0; i10 < s10; i10++) {
                            for (int i11 = 0; i11 < s11; i11++) {
                              for (int i12 = 0; i12 < s12; i12++) {
                                for (int i13 = 0; i13 < s13; i13++) {
                                  for (int i14 = 0; i14 < s14; i14++) {
                                    a[i0,i1,i2,i3,i4,i5,i6,i7,i8,i9,i10,i11,i12,i13,i14] = z;
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
    public static T[,,,,,,,,,,,,,,,] InitNewArray16<T>(T z, BigInteger size0, BigInteger size1, BigInteger size2, BigInteger size3, BigInteger size4, BigInteger size5, BigInteger size6, BigInteger size7, BigInteger size8, BigInteger size9, BigInteger size10, BigInteger size11, BigInteger size12, BigInteger size13, BigInteger size14, BigInteger size15) {
      int s0 = (int)size0;
      int s1 = (int)size1;
      int s2 = (int)size2;
      int s3 = (int)size3;
      int s4 = (int)size4;
      int s5 = (int)size5;
      int s6 = (int)size6;
      int s7 = (int)size7;
      int s8 = (int)size8;
      int s9 = (int)size9;
      int s10 = (int)size10;
      int s11 = (int)size11;
      int s12 = (int)size12;
      int s13 = (int)size13;
      int s14 = (int)size14;
      int s15 = (int)size15;
      T[,,,,,,,,,,,,,,,] a = new T[s0,s1,s2,s3,s4,s5,s6,s7,s8,s9,s10,s11,s12,s13,s14,s15];
      for (int i0 = 0; i0 < s0; i0++) {
        for (int i1 = 0; i1 < s1; i1++) {
          for (int i2 = 0; i2 < s2; i2++) {
            for (int i3 = 0; i3 < s3; i3++) {
              for (int i4 = 0; i4 < s4; i4++) {
                for (int i5 = 0; i5 < s5; i5++) {
                  for (int i6 = 0; i6 < s6; i6++) {
                    for (int i7 = 0; i7 < s7; i7++) {
                      for (int i8 = 0; i8 < s8; i8++) {
                        for (int i9 = 0; i9 < s9; i9++) {
                          for (int i10 = 0; i10 < s10; i10++) {
                            for (int i11 = 0; i11 < s11; i11++) {
                              for (int i12 = 0; i12 < s12; i12++) {
                                for (int i13 = 0; i13 < s13; i13++) {
                                  for (int i14 = 0; i14 < s14; i14++) {
                                    for (int i15 = 0; i15 < s15; i15++) {
                                      a[i0,i1,i2,i3,i4,i5,i6,i7,i8,i9,i10,i11,i12,i13,i14,i15] = z;
                                    }
                                  }
                                }
                              }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
          }
        }
      }
      return a;
    }
  }
} // end of namespace Dafny
internal static class FuncExtensions {
  public static Func<UResult> DowncastClone<TResult, UResult>(this Func<TResult> F, Func<TResult, UResult> ResConv) {
    return () => ResConv(F());
  }
  public static Func<U, UResult> DowncastClone<T, TResult, U, UResult>(this Func<T, TResult> F, Func<U, T> ArgConv, Func<TResult, UResult> ResConv) {
    return arg => ResConv(F(ArgConv(arg)));
  }
  public static Func<U1, U2, UResult> DowncastClone<T1, T2, TResult, U1, U2, UResult>(this Func<T1, T2, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<TResult, UResult> ResConv) {
    return (arg1, arg2) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2)));
  }
  public static Func<U1, U2, U3, UResult> DowncastClone<T1, T2, T3, TResult, U1, U2, U3, UResult>(this Func<T1, T2, T3, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3)));
  }
  public static Func<U1, U2, U3, U4, UResult> DowncastClone<T1, T2, T3, T4, TResult, U1, U2, U3, U4, UResult>(this Func<T1, T2, T3, T4, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4)));
  }
  public static Func<U1, U2, U3, U4, U5, UResult> DowncastClone<T1, T2, T3, T4, T5, TResult, U1, U2, U3, U4, U5, UResult>(this Func<T1, T2, T3, T4, T5, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, TResult, U1, U2, U3, U4, U5, U6, UResult>(this Func<T1, T2, T3, T4, T5, T6, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, TResult, U1, U2, U3, U4, U5, U6, U7, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, TResult, U1, U2, U3, U4, U5, U6, U7, U8, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<U11, T11> ArgConv11, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10), ArgConv11(arg11)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<U11, T11> ArgConv11, Func<U12, T12> ArgConv12, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10), ArgConv11(arg11), ArgConv12(arg12)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<U11, T11> ArgConv11, Func<U12, T12> ArgConv12, Func<U13, T13> ArgConv13, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10), ArgConv11(arg11), ArgConv12(arg12), ArgConv13(arg13)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, U14, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, U14, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<U11, T11> ArgConv11, Func<U12, T12> ArgConv12, Func<U13, T13> ArgConv13, Func<U14, T14> ArgConv14, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10), ArgConv11(arg11), ArgConv12(arg12), ArgConv13(arg13), ArgConv14(arg14)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, U14, U15, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, U14, U15, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<U11, T11> ArgConv11, Func<U12, T12> ArgConv12, Func<U13, T13> ArgConv13, Func<U14, T14> ArgConv14, Func<U15, T15> ArgConv15, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10), ArgConv11(arg11), ArgConv12(arg12), ArgConv13(arg13), ArgConv14(arg14), ArgConv15(arg15)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, U14, U15, U16, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, U14, U15, U16, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<U11, T11> ArgConv11, Func<U12, T12> ArgConv12, Func<U13, T13> ArgConv13, Func<U14, T14> ArgConv14, Func<U15, T15> ArgConv15, Func<U16, T16> ArgConv16, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10), ArgConv11(arg11), ArgConv12(arg12), ArgConv13(arg13), ArgConv14(arg14), ArgConv15(arg15), ArgConv16(arg16)));
  }
}
// end of class FuncExtensions
#endif
namespace _System {

  public partial class nat {
    private static readonly Dafny.TypeDescriptor<BigInteger> _TYPE = new Dafny.TypeDescriptor<BigInteger>(BigInteger.Zero);
    public static Dafny.TypeDescriptor<BigInteger> _TypeDescriptor() {
      return _TYPE;
    }
    public static bool _Is(BigInteger __source) {
      BigInteger _0_x = __source;
      return (_0_x).Sign != -1;
    }
  }

  public interface _ITuple2<out T0, out T1> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    _ITuple2<__T0, __T1> DowncastClone<__T0, __T1>(Func<T0, __T0> converter0, Func<T1, __T1> converter1);
  }
  public class Tuple2<T0, T1> : _ITuple2<T0, T1> {
    public readonly T0 __0;
    public readonly T1 __1;
    public Tuple2(T0 _0, T1 _1) {
      this.__0 = _0;
      this.__1 = _1;
    }
    public _ITuple2<__T0, __T1> DowncastClone<__T0, __T1>(Func<T0, __T0> converter0, Func<T1, __T1> converter1) {
      if (this is _ITuple2<__T0, __T1> dt) { return dt; }
      return new Tuple2<__T0, __T1>(converter0(__0), converter1(__1));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple2<T0, T1>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ")";
      return s;
    }
    public static _System._ITuple2<T0, T1> Default(T0 _default_T0, T1 _default_T1) {
      return create(_default_T0, _default_T1);
    }
    public static Dafny.TypeDescriptor<_System._ITuple2<T0, T1>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1) {
      return new Dafny.TypeDescriptor<_System._ITuple2<T0, T1>>(_System.Tuple2<T0, T1>.Default(_td_T0.Default(), _td_T1.Default()));
    }
    public static _ITuple2<T0, T1> create(T0 _0, T1 _1) {
      return new Tuple2<T0, T1>(_0, _1);
    }
    public static _ITuple2<T0, T1> create____hMake2(T0 _0, T1 _1) {
      return create(_0, _1);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
  }

  public interface _ITuple0 {
    _ITuple0 DowncastClone();
  }
  public class Tuple0 : _ITuple0 {
    public Tuple0() {
    }
    public _ITuple0 DowncastClone() {
      if (this is _ITuple0 dt) { return dt; }
      return new Tuple0();
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple0;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      return (int) hash;
    }
    public override string ToString() {
      return "()";
    }
    private static readonly _System._ITuple0 theDefault = create();
    public static _System._ITuple0 Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<_System._ITuple0> _TYPE = new Dafny.TypeDescriptor<_System._ITuple0>(_System.Tuple0.Default());
    public static Dafny.TypeDescriptor<_System._ITuple0> _TypeDescriptor() {
      return _TYPE;
    }
    public static _ITuple0 create() {
      return new Tuple0();
    }
    public static _ITuple0 create____hMake0() {
      return create();
    }
    public static System.Collections.Generic.IEnumerable<_ITuple0> AllSingletonConstructors {
      get {
        yield return Tuple0.create();
      }
    }
  }

  public interface _ITuple1<out T0> {
    T0 dtor__0 { get; }
    _ITuple1<__T0> DowncastClone<__T0>(Func<T0, __T0> converter0);
  }
  public class Tuple1<T0> : _ITuple1<T0> {
    public readonly T0 __0;
    public Tuple1(T0 _0) {
      this.__0 = _0;
    }
    public _ITuple1<__T0> DowncastClone<__T0>(Func<T0, __T0> converter0) {
      if (this is _ITuple1<__T0> dt) { return dt; }
      return new Tuple1<__T0>(converter0(__0));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple1<T0>;
      return oth != null && object.Equals(this.__0, oth.__0);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ")";
      return s;
    }
    public static _System._ITuple1<T0> Default(T0 _default_T0) {
      return create(_default_T0);
    }
    public static Dafny.TypeDescriptor<_System._ITuple1<T0>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0) {
      return new Dafny.TypeDescriptor<_System._ITuple1<T0>>(_System.Tuple1<T0>.Default(_td_T0.Default()));
    }
    public static _ITuple1<T0> create(T0 _0) {
      return new Tuple1<T0>(_0);
    }
    public static _ITuple1<T0> create____hMake1(T0 _0) {
      return create(_0);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
  }

  public interface _ITuple3<out T0, out T1, out T2> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    _ITuple3<__T0, __T1, __T2> DowncastClone<__T0, __T1, __T2>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2);
  }
  public class Tuple3<T0, T1, T2> : _ITuple3<T0, T1, T2> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public Tuple3(T0 _0, T1 _1, T2 _2) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
    }
    public _ITuple3<__T0, __T1, __T2> DowncastClone<__T0, __T1, __T2>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2) {
      if (this is _ITuple3<__T0, __T1, __T2> dt) { return dt; }
      return new Tuple3<__T0, __T1, __T2>(converter0(__0), converter1(__1), converter2(__2));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple3<T0, T1, T2>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ")";
      return s;
    }
    public static _System._ITuple3<T0, T1, T2> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2) {
      return create(_default_T0, _default_T1, _default_T2);
    }
    public static Dafny.TypeDescriptor<_System._ITuple3<T0, T1, T2>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2) {
      return new Dafny.TypeDescriptor<_System._ITuple3<T0, T1, T2>>(_System.Tuple3<T0, T1, T2>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default()));
    }
    public static _ITuple3<T0, T1, T2> create(T0 _0, T1 _1, T2 _2) {
      return new Tuple3<T0, T1, T2>(_0, _1, _2);
    }
    public static _ITuple3<T0, T1, T2> create____hMake3(T0 _0, T1 _1, T2 _2) {
      return create(_0, _1, _2);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
  }

  public interface _ITuple4<out T0, out T1, out T2, out T3> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    _ITuple4<__T0, __T1, __T2, __T3> DowncastClone<__T0, __T1, __T2, __T3>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3);
  }
  public class Tuple4<T0, T1, T2, T3> : _ITuple4<T0, T1, T2, T3> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public Tuple4(T0 _0, T1 _1, T2 _2, T3 _3) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
    }
    public _ITuple4<__T0, __T1, __T2, __T3> DowncastClone<__T0, __T1, __T2, __T3>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3) {
      if (this is _ITuple4<__T0, __T1, __T2, __T3> dt) { return dt; }
      return new Tuple4<__T0, __T1, __T2, __T3>(converter0(__0), converter1(__1), converter2(__2), converter3(__3));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple4<T0, T1, T2, T3>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ")";
      return s;
    }
    public static _System._ITuple4<T0, T1, T2, T3> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3);
    }
    public static Dafny.TypeDescriptor<_System._ITuple4<T0, T1, T2, T3>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3) {
      return new Dafny.TypeDescriptor<_System._ITuple4<T0, T1, T2, T3>>(_System.Tuple4<T0, T1, T2, T3>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default()));
    }
    public static _ITuple4<T0, T1, T2, T3> create(T0 _0, T1 _1, T2 _2, T3 _3) {
      return new Tuple4<T0, T1, T2, T3>(_0, _1, _2, _3);
    }
    public static _ITuple4<T0, T1, T2, T3> create____hMake4(T0 _0, T1 _1, T2 _2, T3 _3) {
      return create(_0, _1, _2, _3);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
  }

  public interface _ITuple5<out T0, out T1, out T2, out T3, out T4> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    _ITuple5<__T0, __T1, __T2, __T3, __T4> DowncastClone<__T0, __T1, __T2, __T3, __T4>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4);
  }
  public class Tuple5<T0, T1, T2, T3, T4> : _ITuple5<T0, T1, T2, T3, T4> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public Tuple5(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
    }
    public _ITuple5<__T0, __T1, __T2, __T3, __T4> DowncastClone<__T0, __T1, __T2, __T3, __T4>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4) {
      if (this is _ITuple5<__T0, __T1, __T2, __T3, __T4> dt) { return dt; }
      return new Tuple5<__T0, __T1, __T2, __T3, __T4>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple5<T0, T1, T2, T3, T4>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ")";
      return s;
    }
    public static _System._ITuple5<T0, T1, T2, T3, T4> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4);
    }
    public static Dafny.TypeDescriptor<_System._ITuple5<T0, T1, T2, T3, T4>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4) {
      return new Dafny.TypeDescriptor<_System._ITuple5<T0, T1, T2, T3, T4>>(_System.Tuple5<T0, T1, T2, T3, T4>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default()));
    }
    public static _ITuple5<T0, T1, T2, T3, T4> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4) {
      return new Tuple5<T0, T1, T2, T3, T4>(_0, _1, _2, _3, _4);
    }
    public static _ITuple5<T0, T1, T2, T3, T4> create____hMake5(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4) {
      return create(_0, _1, _2, _3, _4);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
  }

  public interface _ITuple6<out T0, out T1, out T2, out T3, out T4, out T5> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    _ITuple6<__T0, __T1, __T2, __T3, __T4, __T5> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5);
  }
  public class Tuple6<T0, T1, T2, T3, T4, T5> : _ITuple6<T0, T1, T2, T3, T4, T5> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public Tuple6(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
    }
    public _ITuple6<__T0, __T1, __T2, __T3, __T4, __T5> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5) {
      if (this is _ITuple6<__T0, __T1, __T2, __T3, __T4, __T5> dt) { return dt; }
      return new Tuple6<__T0, __T1, __T2, __T3, __T4, __T5>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple6<T0, T1, T2, T3, T4, T5>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ")";
      return s;
    }
    public static _System._ITuple6<T0, T1, T2, T3, T4, T5> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5);
    }
    public static Dafny.TypeDescriptor<_System._ITuple6<T0, T1, T2, T3, T4, T5>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5) {
      return new Dafny.TypeDescriptor<_System._ITuple6<T0, T1, T2, T3, T4, T5>>(_System.Tuple6<T0, T1, T2, T3, T4, T5>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default()));
    }
    public static _ITuple6<T0, T1, T2, T3, T4, T5> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5) {
      return new Tuple6<T0, T1, T2, T3, T4, T5>(_0, _1, _2, _3, _4, _5);
    }
    public static _ITuple6<T0, T1, T2, T3, T4, T5> create____hMake6(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5) {
      return create(_0, _1, _2, _3, _4, _5);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
  }

  public interface _ITuple7<out T0, out T1, out T2, out T3, out T4, out T5, out T6> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    _ITuple7<__T0, __T1, __T2, __T3, __T4, __T5, __T6> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6);
  }
  public class Tuple7<T0, T1, T2, T3, T4, T5, T6> : _ITuple7<T0, T1, T2, T3, T4, T5, T6> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public Tuple7(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
    }
    public _ITuple7<__T0, __T1, __T2, __T3, __T4, __T5, __T6> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6) {
      if (this is _ITuple7<__T0, __T1, __T2, __T3, __T4, __T5, __T6> dt) { return dt; }
      return new Tuple7<__T0, __T1, __T2, __T3, __T4, __T5, __T6>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple7<T0, T1, T2, T3, T4, T5, T6>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ")";
      return s;
    }
    public static _System._ITuple7<T0, T1, T2, T3, T4, T5, T6> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6);
    }
    public static Dafny.TypeDescriptor<_System._ITuple7<T0, T1, T2, T3, T4, T5, T6>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6) {
      return new Dafny.TypeDescriptor<_System._ITuple7<T0, T1, T2, T3, T4, T5, T6>>(_System.Tuple7<T0, T1, T2, T3, T4, T5, T6>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default()));
    }
    public static _ITuple7<T0, T1, T2, T3, T4, T5, T6> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6) {
      return new Tuple7<T0, T1, T2, T3, T4, T5, T6>(_0, _1, _2, _3, _4, _5, _6);
    }
    public static _ITuple7<T0, T1, T2, T3, T4, T5, T6> create____hMake7(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6) {
      return create(_0, _1, _2, _3, _4, _5, _6);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
  }

  public interface _ITuple8<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    _ITuple8<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7);
  }
  public class Tuple8<T0, T1, T2, T3, T4, T5, T6, T7> : _ITuple8<T0, T1, T2, T3, T4, T5, T6, T7> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public Tuple8(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
    }
    public _ITuple8<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7) {
      if (this is _ITuple8<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7> dt) { return dt; }
      return new Tuple8<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple8<T0, T1, T2, T3, T4, T5, T6, T7>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ")";
      return s;
    }
    public static _System._ITuple8<T0, T1, T2, T3, T4, T5, T6, T7> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7);
    }
    public static Dafny.TypeDescriptor<_System._ITuple8<T0, T1, T2, T3, T4, T5, T6, T7>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7) {
      return new Dafny.TypeDescriptor<_System._ITuple8<T0, T1, T2, T3, T4, T5, T6, T7>>(_System.Tuple8<T0, T1, T2, T3, T4, T5, T6, T7>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default()));
    }
    public static _ITuple8<T0, T1, T2, T3, T4, T5, T6, T7> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7) {
      return new Tuple8<T0, T1, T2, T3, T4, T5, T6, T7>(_0, _1, _2, _3, _4, _5, _6, _7);
    }
    public static _ITuple8<T0, T1, T2, T3, T4, T5, T6, T7> create____hMake8(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
  }

  public interface _ITuple9<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    _ITuple9<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8);
  }
  public class Tuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8> : _ITuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public Tuple9(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
    }
    public _ITuple9<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8) {
      if (this is _ITuple9<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8> dt) { return dt; }
      return new Tuple9<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ")";
      return s;
    }
    public static _System._ITuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8);
    }
    public static Dafny.TypeDescriptor<_System._ITuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8) {
      return new Dafny.TypeDescriptor<_System._ITuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8>>(_System.Tuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default()));
    }
    public static _ITuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8) {
      return new Tuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8>(_0, _1, _2, _3, _4, _5, _6, _7, _8);
    }
    public static _ITuple9<T0, T1, T2, T3, T4, T5, T6, T7, T8> create____hMake9(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
  }

  public interface _ITuple10<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    _ITuple10<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9);
  }
  public class Tuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> : _ITuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public Tuple10(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
    }
    public _ITuple10<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9) {
      if (this is _ITuple10<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9> dt) { return dt; }
      return new Tuple10<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ")";
      return s;
    }
    public static _System._ITuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9);
    }
    public static Dafny.TypeDescriptor<_System._ITuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9) {
      return new Dafny.TypeDescriptor<_System._ITuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>>(_System.Tuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default()));
    }
    public static _ITuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9) {
      return new Tuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9);
    }
    public static _ITuple10<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9> create____hMake10(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
  }

  public interface _ITuple11<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    _ITuple11<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10);
  }
  public class Tuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> : _ITuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public Tuple11(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
    }
    public _ITuple11<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10) {
      if (this is _ITuple11<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10> dt) { return dt; }
      return new Tuple11<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ")";
      return s;
    }
    public static _System._ITuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10);
    }
    public static Dafny.TypeDescriptor<_System._ITuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10) {
      return new Dafny.TypeDescriptor<_System._ITuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>>(_System.Tuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default()));
    }
    public static _ITuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10) {
      return new Tuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10);
    }
    public static _ITuple11<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> create____hMake11(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
  }

  public interface _ITuple12<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    _ITuple12<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11);
  }
  public class Tuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> : _ITuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public Tuple12(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
    }
    public _ITuple12<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11) {
      if (this is _ITuple12<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11> dt) { return dt; }
      return new Tuple12<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ")";
      return s;
    }
    public static _System._ITuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11);
    }
    public static Dafny.TypeDescriptor<_System._ITuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11) {
      return new Dafny.TypeDescriptor<_System._ITuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>>(_System.Tuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default()));
    }
    public static _ITuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11) {
      return new Tuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11);
    }
    public static _ITuple12<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> create____hMake12(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
  }

  public interface _ITuple13<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11, out T12> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    T12 dtor__12 { get; }
    _ITuple13<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12);
  }
  public class Tuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> : _ITuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public readonly T12 __12;
    public Tuple13(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
      this.__12 = _12;
    }
    public _ITuple13<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12) {
      if (this is _ITuple13<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12> dt) { return dt; }
      return new Tuple13<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11), converter12(__12));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11) && object.Equals(this.__12, oth.__12);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__12));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__12);
      s += ")";
      return s;
    }
    public static _System._ITuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11, T12 _default_T12) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11, _default_T12);
    }
    public static Dafny.TypeDescriptor<_System._ITuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11, Dafny.TypeDescriptor<T12> _td_T12) {
      return new Dafny.TypeDescriptor<_System._ITuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>>(_System.Tuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default(), _td_T12.Default()));
    }
    public static _ITuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12) {
      return new Tuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12);
    }
    public static _ITuple13<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> create____hMake13(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
    public T12 dtor__12 {
      get {
        return this.__12;
      }
    }
  }

  public interface _ITuple14<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11, out T12, out T13> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    T12 dtor__12 { get; }
    T13 dtor__13 { get; }
    _ITuple14<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13);
  }
  public class Tuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> : _ITuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public readonly T12 __12;
    public readonly T13 __13;
    public Tuple14(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
      this.__12 = _12;
      this.__13 = _13;
    }
    public _ITuple14<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13) {
      if (this is _ITuple14<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13> dt) { return dt; }
      return new Tuple14<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11), converter12(__12), converter13(__13));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11) && object.Equals(this.__12, oth.__12) && object.Equals(this.__13, oth.__13);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__12));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__13));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__12);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__13);
      s += ")";
      return s;
    }
    public static _System._ITuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11, T12 _default_T12, T13 _default_T13) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11, _default_T12, _default_T13);
    }
    public static Dafny.TypeDescriptor<_System._ITuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11, Dafny.TypeDescriptor<T12> _td_T12, Dafny.TypeDescriptor<T13> _td_T13) {
      return new Dafny.TypeDescriptor<_System._ITuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>>(_System.Tuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default(), _td_T12.Default(), _td_T13.Default()));
    }
    public static _ITuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13) {
      return new Tuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13);
    }
    public static _ITuple14<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> create____hMake14(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
    public T12 dtor__12 {
      get {
        return this.__12;
      }
    }
    public T13 dtor__13 {
      get {
        return this.__13;
      }
    }
  }

  public interface _ITuple15<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11, out T12, out T13, out T14> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    T12 dtor__12 { get; }
    T13 dtor__13 { get; }
    T14 dtor__14 { get; }
    _ITuple15<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14);
  }
  public class Tuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> : _ITuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public readonly T12 __12;
    public readonly T13 __13;
    public readonly T14 __14;
    public Tuple15(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
      this.__12 = _12;
      this.__13 = _13;
      this.__14 = _14;
    }
    public _ITuple15<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14) {
      if (this is _ITuple15<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14> dt) { return dt; }
      return new Tuple15<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11), converter12(__12), converter13(__13), converter14(__14));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11) && object.Equals(this.__12, oth.__12) && object.Equals(this.__13, oth.__13) && object.Equals(this.__14, oth.__14);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__12));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__13));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__14));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__12);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__13);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__14);
      s += ")";
      return s;
    }
    public static _System._ITuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11, T12 _default_T12, T13 _default_T13, T14 _default_T14) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11, _default_T12, _default_T13, _default_T14);
    }
    public static Dafny.TypeDescriptor<_System._ITuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11, Dafny.TypeDescriptor<T12> _td_T12, Dafny.TypeDescriptor<T13> _td_T13, Dafny.TypeDescriptor<T14> _td_T14) {
      return new Dafny.TypeDescriptor<_System._ITuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>>(_System.Tuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default(), _td_T12.Default(), _td_T13.Default(), _td_T14.Default()));
    }
    public static _ITuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14) {
      return new Tuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14);
    }
    public static _ITuple15<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> create____hMake15(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
    public T12 dtor__12 {
      get {
        return this.__12;
      }
    }
    public T13 dtor__13 {
      get {
        return this.__13;
      }
    }
    public T14 dtor__14 {
      get {
        return this.__14;
      }
    }
  }

  public interface _ITuple16<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11, out T12, out T13, out T14, out T15> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    T12 dtor__12 { get; }
    T13 dtor__13 { get; }
    T14 dtor__14 { get; }
    T15 dtor__15 { get; }
    _ITuple16<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15);
  }
  public class Tuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> : _ITuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public readonly T12 __12;
    public readonly T13 __13;
    public readonly T14 __14;
    public readonly T15 __15;
    public Tuple16(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
      this.__12 = _12;
      this.__13 = _13;
      this.__14 = _14;
      this.__15 = _15;
    }
    public _ITuple16<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15) {
      if (this is _ITuple16<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15> dt) { return dt; }
      return new Tuple16<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11), converter12(__12), converter13(__13), converter14(__14), converter15(__15));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11) && object.Equals(this.__12, oth.__12) && object.Equals(this.__13, oth.__13) && object.Equals(this.__14, oth.__14) && object.Equals(this.__15, oth.__15);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__12));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__13));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__14));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__15));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__12);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__13);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__14);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__15);
      s += ")";
      return s;
    }
    public static _System._ITuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11, T12 _default_T12, T13 _default_T13, T14 _default_T14, T15 _default_T15) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11, _default_T12, _default_T13, _default_T14, _default_T15);
    }
    public static Dafny.TypeDescriptor<_System._ITuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11, Dafny.TypeDescriptor<T12> _td_T12, Dafny.TypeDescriptor<T13> _td_T13, Dafny.TypeDescriptor<T14> _td_T14, Dafny.TypeDescriptor<T15> _td_T15) {
      return new Dafny.TypeDescriptor<_System._ITuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>>(_System.Tuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default(), _td_T12.Default(), _td_T13.Default(), _td_T14.Default(), _td_T15.Default()));
    }
    public static _ITuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15) {
      return new Tuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15);
    }
    public static _ITuple16<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> create____hMake16(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
    public T12 dtor__12 {
      get {
        return this.__12;
      }
    }
    public T13 dtor__13 {
      get {
        return this.__13;
      }
    }
    public T14 dtor__14 {
      get {
        return this.__14;
      }
    }
    public T15 dtor__15 {
      get {
        return this.__15;
      }
    }
  }

  public interface _ITuple17<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11, out T12, out T13, out T14, out T15, out T16> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    T12 dtor__12 { get; }
    T13 dtor__13 { get; }
    T14 dtor__14 { get; }
    T15 dtor__15 { get; }
    T16 dtor__16 { get; }
    _ITuple17<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15, Func<T16, __T16> converter16);
  }
  public class Tuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> : _ITuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public readonly T12 __12;
    public readonly T13 __13;
    public readonly T14 __14;
    public readonly T15 __15;
    public readonly T16 __16;
    public Tuple17(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
      this.__12 = _12;
      this.__13 = _13;
      this.__14 = _14;
      this.__15 = _15;
      this.__16 = _16;
    }
    public _ITuple17<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15, Func<T16, __T16> converter16) {
      if (this is _ITuple17<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16> dt) { return dt; }
      return new Tuple17<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11), converter12(__12), converter13(__13), converter14(__14), converter15(__15), converter16(__16));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11) && object.Equals(this.__12, oth.__12) && object.Equals(this.__13, oth.__13) && object.Equals(this.__14, oth.__14) && object.Equals(this.__15, oth.__15) && object.Equals(this.__16, oth.__16);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__12));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__13));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__14));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__15));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__16));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__12);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__13);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__14);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__15);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__16);
      s += ")";
      return s;
    }
    public static _System._ITuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11, T12 _default_T12, T13 _default_T13, T14 _default_T14, T15 _default_T15, T16 _default_T16) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11, _default_T12, _default_T13, _default_T14, _default_T15, _default_T16);
    }
    public static Dafny.TypeDescriptor<_System._ITuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11, Dafny.TypeDescriptor<T12> _td_T12, Dafny.TypeDescriptor<T13> _td_T13, Dafny.TypeDescriptor<T14> _td_T14, Dafny.TypeDescriptor<T15> _td_T15, Dafny.TypeDescriptor<T16> _td_T16) {
      return new Dafny.TypeDescriptor<_System._ITuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>>(_System.Tuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default(), _td_T12.Default(), _td_T13.Default(), _td_T14.Default(), _td_T15.Default(), _td_T16.Default()));
    }
    public static _ITuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16) {
      return new Tuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16);
    }
    public static _ITuple17<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> create____hMake17(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
    public T12 dtor__12 {
      get {
        return this.__12;
      }
    }
    public T13 dtor__13 {
      get {
        return this.__13;
      }
    }
    public T14 dtor__14 {
      get {
        return this.__14;
      }
    }
    public T15 dtor__15 {
      get {
        return this.__15;
      }
    }
    public T16 dtor__16 {
      get {
        return this.__16;
      }
    }
  }

  public interface _ITuple18<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11, out T12, out T13, out T14, out T15, out T16, out T17> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    T12 dtor__12 { get; }
    T13 dtor__13 { get; }
    T14 dtor__14 { get; }
    T15 dtor__15 { get; }
    T16 dtor__16 { get; }
    T17 dtor__17 { get; }
    _ITuple18<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15, Func<T16, __T16> converter16, Func<T17, __T17> converter17);
  }
  public class Tuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> : _ITuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public readonly T12 __12;
    public readonly T13 __13;
    public readonly T14 __14;
    public readonly T15 __15;
    public readonly T16 __16;
    public readonly T17 __17;
    public Tuple18(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
      this.__12 = _12;
      this.__13 = _13;
      this.__14 = _14;
      this.__15 = _15;
      this.__16 = _16;
      this.__17 = _17;
    }
    public _ITuple18<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15, Func<T16, __T16> converter16, Func<T17, __T17> converter17) {
      if (this is _ITuple18<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17> dt) { return dt; }
      return new Tuple18<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11), converter12(__12), converter13(__13), converter14(__14), converter15(__15), converter16(__16), converter17(__17));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11) && object.Equals(this.__12, oth.__12) && object.Equals(this.__13, oth.__13) && object.Equals(this.__14, oth.__14) && object.Equals(this.__15, oth.__15) && object.Equals(this.__16, oth.__16) && object.Equals(this.__17, oth.__17);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__12));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__13));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__14));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__15));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__16));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__17));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__12);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__13);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__14);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__15);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__16);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__17);
      s += ")";
      return s;
    }
    public static _System._ITuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11, T12 _default_T12, T13 _default_T13, T14 _default_T14, T15 _default_T15, T16 _default_T16, T17 _default_T17) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11, _default_T12, _default_T13, _default_T14, _default_T15, _default_T16, _default_T17);
    }
    public static Dafny.TypeDescriptor<_System._ITuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11, Dafny.TypeDescriptor<T12> _td_T12, Dafny.TypeDescriptor<T13> _td_T13, Dafny.TypeDescriptor<T14> _td_T14, Dafny.TypeDescriptor<T15> _td_T15, Dafny.TypeDescriptor<T16> _td_T16, Dafny.TypeDescriptor<T17> _td_T17) {
      return new Dafny.TypeDescriptor<_System._ITuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>>(_System.Tuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default(), _td_T12.Default(), _td_T13.Default(), _td_T14.Default(), _td_T15.Default(), _td_T16.Default(), _td_T17.Default()));
    }
    public static _ITuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17) {
      return new Tuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17);
    }
    public static _ITuple18<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17> create____hMake18(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
    public T12 dtor__12 {
      get {
        return this.__12;
      }
    }
    public T13 dtor__13 {
      get {
        return this.__13;
      }
    }
    public T14 dtor__14 {
      get {
        return this.__14;
      }
    }
    public T15 dtor__15 {
      get {
        return this.__15;
      }
    }
    public T16 dtor__16 {
      get {
        return this.__16;
      }
    }
    public T17 dtor__17 {
      get {
        return this.__17;
      }
    }
  }

  public interface _ITuple19<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11, out T12, out T13, out T14, out T15, out T16, out T17, out T18> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    T12 dtor__12 { get; }
    T13 dtor__13 { get; }
    T14 dtor__14 { get; }
    T15 dtor__15 { get; }
    T16 dtor__16 { get; }
    T17 dtor__17 { get; }
    T18 dtor__18 { get; }
    _ITuple19<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15, Func<T16, __T16> converter16, Func<T17, __T17> converter17, Func<T18, __T18> converter18);
  }
  public class Tuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> : _ITuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public readonly T12 __12;
    public readonly T13 __13;
    public readonly T14 __14;
    public readonly T15 __15;
    public readonly T16 __16;
    public readonly T17 __17;
    public readonly T18 __18;
    public Tuple19(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17, T18 _18) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
      this.__12 = _12;
      this.__13 = _13;
      this.__14 = _14;
      this.__15 = _15;
      this.__16 = _16;
      this.__17 = _17;
      this.__18 = _18;
    }
    public _ITuple19<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15, Func<T16, __T16> converter16, Func<T17, __T17> converter17, Func<T18, __T18> converter18) {
      if (this is _ITuple19<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18> dt) { return dt; }
      return new Tuple19<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11), converter12(__12), converter13(__13), converter14(__14), converter15(__15), converter16(__16), converter17(__17), converter18(__18));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11) && object.Equals(this.__12, oth.__12) && object.Equals(this.__13, oth.__13) && object.Equals(this.__14, oth.__14) && object.Equals(this.__15, oth.__15) && object.Equals(this.__16, oth.__16) && object.Equals(this.__17, oth.__17) && object.Equals(this.__18, oth.__18);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__12));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__13));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__14));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__15));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__16));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__17));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__18));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__12);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__13);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__14);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__15);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__16);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__17);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__18);
      s += ")";
      return s;
    }
    public static _System._ITuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11, T12 _default_T12, T13 _default_T13, T14 _default_T14, T15 _default_T15, T16 _default_T16, T17 _default_T17, T18 _default_T18) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11, _default_T12, _default_T13, _default_T14, _default_T15, _default_T16, _default_T17, _default_T18);
    }
    public static Dafny.TypeDescriptor<_System._ITuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11, Dafny.TypeDescriptor<T12> _td_T12, Dafny.TypeDescriptor<T13> _td_T13, Dafny.TypeDescriptor<T14> _td_T14, Dafny.TypeDescriptor<T15> _td_T15, Dafny.TypeDescriptor<T16> _td_T16, Dafny.TypeDescriptor<T17> _td_T17, Dafny.TypeDescriptor<T18> _td_T18) {
      return new Dafny.TypeDescriptor<_System._ITuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>>(_System.Tuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default(), _td_T12.Default(), _td_T13.Default(), _td_T14.Default(), _td_T15.Default(), _td_T16.Default(), _td_T17.Default(), _td_T18.Default()));
    }
    public static _ITuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17, T18 _18) {
      return new Tuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17, _18);
    }
    public static _ITuple19<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18> create____hMake19(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17, T18 _18) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17, _18);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
    public T12 dtor__12 {
      get {
        return this.__12;
      }
    }
    public T13 dtor__13 {
      get {
        return this.__13;
      }
    }
    public T14 dtor__14 {
      get {
        return this.__14;
      }
    }
    public T15 dtor__15 {
      get {
        return this.__15;
      }
    }
    public T16 dtor__16 {
      get {
        return this.__16;
      }
    }
    public T17 dtor__17 {
      get {
        return this.__17;
      }
    }
    public T18 dtor__18 {
      get {
        return this.__18;
      }
    }
  }

  public interface _ITuple20<out T0, out T1, out T2, out T3, out T4, out T5, out T6, out T7, out T8, out T9, out T10, out T11, out T12, out T13, out T14, out T15, out T16, out T17, out T18, out T19> {
    T0 dtor__0 { get; }
    T1 dtor__1 { get; }
    T2 dtor__2 { get; }
    T3 dtor__3 { get; }
    T4 dtor__4 { get; }
    T5 dtor__5 { get; }
    T6 dtor__6 { get; }
    T7 dtor__7 { get; }
    T8 dtor__8 { get; }
    T9 dtor__9 { get; }
    T10 dtor__10 { get; }
    T11 dtor__11 { get; }
    T12 dtor__12 { get; }
    T13 dtor__13 { get; }
    T14 dtor__14 { get; }
    T15 dtor__15 { get; }
    T16 dtor__16 { get; }
    T17 dtor__17 { get; }
    T18 dtor__18 { get; }
    T19 dtor__19 { get; }
    _ITuple20<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18, __T19> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18, __T19>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15, Func<T16, __T16> converter16, Func<T17, __T17> converter17, Func<T18, __T18> converter18, Func<T19, __T19> converter19);
  }
  public class Tuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> : _ITuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> {
    public readonly T0 __0;
    public readonly T1 __1;
    public readonly T2 __2;
    public readonly T3 __3;
    public readonly T4 __4;
    public readonly T5 __5;
    public readonly T6 __6;
    public readonly T7 __7;
    public readonly T8 __8;
    public readonly T9 __9;
    public readonly T10 __10;
    public readonly T11 __11;
    public readonly T12 __12;
    public readonly T13 __13;
    public readonly T14 __14;
    public readonly T15 __15;
    public readonly T16 __16;
    public readonly T17 __17;
    public readonly T18 __18;
    public readonly T19 __19;
    public Tuple20(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17, T18 _18, T19 _19) {
      this.__0 = _0;
      this.__1 = _1;
      this.__2 = _2;
      this.__3 = _3;
      this.__4 = _4;
      this.__5 = _5;
      this.__6 = _6;
      this.__7 = _7;
      this.__8 = _8;
      this.__9 = _9;
      this.__10 = _10;
      this.__11 = _11;
      this.__12 = _12;
      this.__13 = _13;
      this.__14 = _14;
      this.__15 = _15;
      this.__16 = _16;
      this.__17 = _17;
      this.__18 = _18;
      this.__19 = _19;
    }
    public _ITuple20<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18, __T19> DowncastClone<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18, __T19>(Func<T0, __T0> converter0, Func<T1, __T1> converter1, Func<T2, __T2> converter2, Func<T3, __T3> converter3, Func<T4, __T4> converter4, Func<T5, __T5> converter5, Func<T6, __T6> converter6, Func<T7, __T7> converter7, Func<T8, __T8> converter8, Func<T9, __T9> converter9, Func<T10, __T10> converter10, Func<T11, __T11> converter11, Func<T12, __T12> converter12, Func<T13, __T13> converter13, Func<T14, __T14> converter14, Func<T15, __T15> converter15, Func<T16, __T16> converter16, Func<T17, __T17> converter17, Func<T18, __T18> converter18, Func<T19, __T19> converter19) {
      if (this is _ITuple20<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18, __T19> dt) { return dt; }
      return new Tuple20<__T0, __T1, __T2, __T3, __T4, __T5, __T6, __T7, __T8, __T9, __T10, __T11, __T12, __T13, __T14, __T15, __T16, __T17, __T18, __T19>(converter0(__0), converter1(__1), converter2(__2), converter3(__3), converter4(__4), converter5(__5), converter6(__6), converter7(__7), converter8(__8), converter9(__9), converter10(__10), converter11(__11), converter12(__12), converter13(__13), converter14(__14), converter15(__15), converter16(__16), converter17(__17), converter18(__18), converter19(__19));
    }
    public override bool Equals(object other) {
      var oth = other as _System.Tuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>;
      return oth != null && object.Equals(this.__0, oth.__0) && object.Equals(this.__1, oth.__1) && object.Equals(this.__2, oth.__2) && object.Equals(this.__3, oth.__3) && object.Equals(this.__4, oth.__4) && object.Equals(this.__5, oth.__5) && object.Equals(this.__6, oth.__6) && object.Equals(this.__7, oth.__7) && object.Equals(this.__8, oth.__8) && object.Equals(this.__9, oth.__9) && object.Equals(this.__10, oth.__10) && object.Equals(this.__11, oth.__11) && object.Equals(this.__12, oth.__12) && object.Equals(this.__13, oth.__13) && object.Equals(this.__14, oth.__14) && object.Equals(this.__15, oth.__15) && object.Equals(this.__16, oth.__16) && object.Equals(this.__17, oth.__17) && object.Equals(this.__18, oth.__18) && object.Equals(this.__19, oth.__19);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__0));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__2));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__3));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__4));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__6));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__7));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__8));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__9));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__10));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__11));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__12));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__13));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__14));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__15));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__16));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__17));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__18));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this.__19));
      return (int) hash;
    }
    public override string ToString() {
      string s = "";
      s += "(";
      s += Dafny.Helpers.ToString(this.__0);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__1);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__2);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__3);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__4);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__5);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__6);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__7);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__8);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__9);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__10);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__11);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__12);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__13);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__14);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__15);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__16);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__17);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__18);
      s += ", ";
      s += Dafny.Helpers.ToString(this.__19);
      s += ")";
      return s;
    }
    public static _System._ITuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> Default(T0 _default_T0, T1 _default_T1, T2 _default_T2, T3 _default_T3, T4 _default_T4, T5 _default_T5, T6 _default_T6, T7 _default_T7, T8 _default_T8, T9 _default_T9, T10 _default_T10, T11 _default_T11, T12 _default_T12, T13 _default_T13, T14 _default_T14, T15 _default_T15, T16 _default_T16, T17 _default_T17, T18 _default_T18, T19 _default_T19) {
      return create(_default_T0, _default_T1, _default_T2, _default_T3, _default_T4, _default_T5, _default_T6, _default_T7, _default_T8, _default_T9, _default_T10, _default_T11, _default_T12, _default_T13, _default_T14, _default_T15, _default_T16, _default_T17, _default_T18, _default_T19);
    }
    public static Dafny.TypeDescriptor<_System._ITuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>> _TypeDescriptor(Dafny.TypeDescriptor<T0> _td_T0, Dafny.TypeDescriptor<T1> _td_T1, Dafny.TypeDescriptor<T2> _td_T2, Dafny.TypeDescriptor<T3> _td_T3, Dafny.TypeDescriptor<T4> _td_T4, Dafny.TypeDescriptor<T5> _td_T5, Dafny.TypeDescriptor<T6> _td_T6, Dafny.TypeDescriptor<T7> _td_T7, Dafny.TypeDescriptor<T8> _td_T8, Dafny.TypeDescriptor<T9> _td_T9, Dafny.TypeDescriptor<T10> _td_T10, Dafny.TypeDescriptor<T11> _td_T11, Dafny.TypeDescriptor<T12> _td_T12, Dafny.TypeDescriptor<T13> _td_T13, Dafny.TypeDescriptor<T14> _td_T14, Dafny.TypeDescriptor<T15> _td_T15, Dafny.TypeDescriptor<T16> _td_T16, Dafny.TypeDescriptor<T17> _td_T17, Dafny.TypeDescriptor<T18> _td_T18, Dafny.TypeDescriptor<T19> _td_T19) {
      return new Dafny.TypeDescriptor<_System._ITuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>>(_System.Tuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>.Default(_td_T0.Default(), _td_T1.Default(), _td_T2.Default(), _td_T3.Default(), _td_T4.Default(), _td_T5.Default(), _td_T6.Default(), _td_T7.Default(), _td_T8.Default(), _td_T9.Default(), _td_T10.Default(), _td_T11.Default(), _td_T12.Default(), _td_T13.Default(), _td_T14.Default(), _td_T15.Default(), _td_T16.Default(), _td_T17.Default(), _td_T18.Default(), _td_T19.Default()));
    }
    public static _ITuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> create(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17, T18 _18, T19 _19) {
      return new Tuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19>(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17, _18, _19);
    }
    public static _ITuple20<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, T17, T18, T19> create____hMake20(T0 _0, T1 _1, T2 _2, T3 _3, T4 _4, T5 _5, T6 _6, T7 _7, T8 _8, T9 _9, T10 _10, T11 _11, T12 _12, T13 _13, T14 _14, T15 _15, T16 _16, T17 _17, T18 _18, T19 _19) {
      return create(_0, _1, _2, _3, _4, _5, _6, _7, _8, _9, _10, _11, _12, _13, _14, _15, _16, _17, _18, _19);
    }
    public T0 dtor__0 {
      get {
        return this.__0;
      }
    }
    public T1 dtor__1 {
      get {
        return this.__1;
      }
    }
    public T2 dtor__2 {
      get {
        return this.__2;
      }
    }
    public T3 dtor__3 {
      get {
        return this.__3;
      }
    }
    public T4 dtor__4 {
      get {
        return this.__4;
      }
    }
    public T5 dtor__5 {
      get {
        return this.__5;
      }
    }
    public T6 dtor__6 {
      get {
        return this.__6;
      }
    }
    public T7 dtor__7 {
      get {
        return this.__7;
      }
    }
    public T8 dtor__8 {
      get {
        return this.__8;
      }
    }
    public T9 dtor__9 {
      get {
        return this.__9;
      }
    }
    public T10 dtor__10 {
      get {
        return this.__10;
      }
    }
    public T11 dtor__11 {
      get {
        return this.__11;
      }
    }
    public T12 dtor__12 {
      get {
        return this.__12;
      }
    }
    public T13 dtor__13 {
      get {
        return this.__13;
      }
    }
    public T14 dtor__14 {
      get {
        return this.__14;
      }
    }
    public T15 dtor__15 {
      get {
        return this.__15;
      }
    }
    public T16 dtor__16 {
      get {
        return this.__16;
      }
    }
    public T17 dtor__17 {
      get {
        return this.__17;
      }
    }
    public T18 dtor__18 {
      get {
        return this.__18;
      }
    }
    public T19 dtor__19 {
      get {
        return this.__19;
      }
    }
  }
} // end of namespace _System
namespace Dafny {
  internal class ArrayHelpers {
    public static T[] InitNewArray1<T>(T z, BigInteger size0) {
      int s0 = (int)size0;
      T[] a = new T[s0];
      for (int i0 = 0; i0 < s0; i0++) {
        a[i0] = z;
      }
      return a;
    }
  }
} // end of namespace Dafny
internal static class FuncExtensions {
  public static Func<UResult> DowncastClone<TResult, UResult>(this Func<TResult> F, Func<TResult, UResult> ResConv) {
    return () => ResConv(F());
  }
  public static Func<U, UResult> DowncastClone<T, TResult, U, UResult>(this Func<T, TResult> F, Func<U, T> ArgConv, Func<TResult, UResult> ResConv) {
    return arg => ResConv(F(ArgConv(arg)));
  }
  public static Func<U1, U2, UResult> DowncastClone<T1, T2, TResult, U1, U2, UResult>(this Func<T1, T2, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<TResult, UResult> ResConv) {
    return (arg1, arg2) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2)));
  }
  public static Func<U1, U2, U3, UResult> DowncastClone<T1, T2, T3, TResult, U1, U2, U3, UResult>(this Func<T1, T2, T3, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3)));
  }
  public static Func<U1, U2, U3, U4, UResult> DowncastClone<T1, T2, T3, T4, TResult, U1, U2, U3, U4, UResult>(this Func<T1, T2, T3, T4, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4)));
  }
  public static Func<U1, U2, U3, U4, U5, UResult> DowncastClone<T1, T2, T3, T4, T5, TResult, U1, U2, U3, U4, U5, UResult>(this Func<T1, T2, T3, T4, T5, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, TResult, U1, U2, U3, U4, U5, U6, U7, U8, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<U11, T11> ArgConv11, Func<U12, T12> ArgConv12, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10), ArgConv11(arg11), ArgConv12(arg12)));
  }
  public static Func<U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, U14, U15, U16, UResult> DowncastClone<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult, U1, U2, U3, U4, U5, U6, U7, U8, U9, U10, U11, U12, U13, U14, U15, U16, UResult>(this Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> F, Func<U1, T1> ArgConv1, Func<U2, T2> ArgConv2, Func<U3, T3> ArgConv3, Func<U4, T4> ArgConv4, Func<U5, T5> ArgConv5, Func<U6, T6> ArgConv6, Func<U7, T7> ArgConv7, Func<U8, T8> ArgConv8, Func<U9, T9> ArgConv9, Func<U10, T10> ArgConv10, Func<U11, T11> ArgConv11, Func<U12, T12> ArgConv12, Func<U13, T13> ArgConv13, Func<U14, T14> ArgConv14, Func<U15, T15> ArgConv15, Func<U16, T16> ArgConv16, Func<TResult, UResult> ResConv) {
    return (arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8, arg9, arg10, arg11, arg12, arg13, arg14, arg15, arg16) => ResConv(F(ArgConv1(arg1), ArgConv2(arg2), ArgConv3(arg3), ArgConv4(arg4), ArgConv5(arg5), ArgConv6(arg6), ArgConv7(arg7), ArgConv8(arg8), ArgConv9(arg9), ArgConv10(arg10), ArgConv11(arg11), ArgConv12(arg12), ArgConv13(arg13), ArgConv14(arg14), ArgConv15(arg15), ArgConv16(arg16)));
  }
}
// end of class FuncExtensions
namespace TI59__Nibbles {

  public partial class __default {
    public static Dafny.ISequence<byte> ZeroReg() {
      return ((System.Func<Dafny.ISequence<byte>>) (() => {
        BigInteger dim0 = new BigInteger(16);
        var arr0 = new byte[Dafny.Helpers.ToIntChecked(dim0, "array size exceeds memory limit")];
        for (int i0 = 0; i0 < dim0; i0++) {
          var _0___v1 = (BigInteger) i0;
          arr0[(int)(_0___v1)] = (byte)(0);
        }
        return Dafny.Sequence<byte>.FromArray(arr0);
      }))();
    }
    public static Dafny.ISequence<byte> RegUpd(Dafny.ISequence<byte> s, BigInteger i, byte v)
    {
      return Dafny.Sequence<byte>.Update(s, i, v);
    }
    public static bool BcdValid(Dafny.ISequence<byte> s) {
      return Dafny.Helpers.Id<Func<Dafny.ISequence<byte>, bool>>((_0_s) => Dafny.Helpers.Quantifier<BigInteger>(Dafny.Helpers.IntegerRange(BigInteger.Zero, new BigInteger(16)), true, (((_forall_var_0) => {
        BigInteger _1_i = (BigInteger)_forall_var_0;
        return !(((_1_i).Sign != -1) && ((_1_i) < (new BigInteger(16)))) || ((new BigInteger((_0_s).Select(_1_i))) < (new BigInteger(10)));
      }))))(s);
    }
    public static TI59__Nibbles._IMask MaskOf(BigInteger idx) {
      if ((idx) == (BigInteger.One)) {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, new BigInteger(15), BigInteger.Zero, (byte)(0));
      } else if ((idx) == (new BigInteger(2))) {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, (byte)(0));
      } else if ((idx) == (new BigInteger(3))) {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, (byte)(1));
      } else if ((idx) == (new BigInteger(4))) {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, (byte)(12));
      } else if ((idx) == (new BigInteger(5))) {
        return TI59__Nibbles.Mask.create(new BigInteger(3), new BigInteger(3), new BigInteger(3), (byte)(1));
      } else if ((idx) == (new BigInteger(6))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(2), BigInteger.One, (byte)(0));
      } else if ((idx) == (new BigInteger(7))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(2), BigInteger.One, (byte)(1));
      } else if ((idx) == (new BigInteger(9))) {
        return TI59__Nibbles.Mask.create(new BigInteger(3), new BigInteger(15), new BigInteger(3), (byte)(0));
      } else if ((idx) == (new BigInteger(11))) {
        return TI59__Nibbles.Mask.create(new BigInteger(3), new BigInteger(15), new BigInteger(3), (byte)(5));
      } else if ((idx) == (new BigInteger(12))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(15), BigInteger.One, (byte)(0));
      } else if ((idx) == (new BigInteger(13))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(15), new BigInteger(3), (byte)(1));
      } else if ((idx) == (new BigInteger(14))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(15), new BigInteger(15), (byte)(1));
      } else if ((idx) == (new BigInteger(15))) {
        return TI59__Nibbles.Mask.create(BigInteger.One, new BigInteger(15), BigInteger.One, (byte)(1));
      } else {
        return TI59__Nibbles.Mask.create(BigInteger.Zero, new BigInteger(15), BigInteger.Zero, (byte)(0));
      }
    }
    public static byte Bit8(BigInteger b) {
      if ((b).Sign == 0) {
        return (byte)(1);
      } else {
        return unchecked((byte)(((byte)((TI59__Nibbles.__default.Bit8((b) - (BigInteger.One))) << ((int)((byte)(1)))))));
      }
    }
    public static ushort Bit16(BigInteger b) {
      if ((b).Sign == 0) {
        return (ushort)(1);
      } else {
        return unchecked((ushort)(((ushort)((TI59__Nibbles.__default.Bit16((b) - (BigInteger.One))) << ((int)((byte)(1)))))));
      }
    }
    public static Dafny.Rune Nul() {
      return new Dafny.Rune((int)(BigInteger.Zero));
    }
    public static ushort FLG__IDLE { get {
      return (ushort)(1);
    } }
    public static ushort FLG__HOLD { get {
      return (ushort)(2);
    } }
    public static ushort FLG__JUMP { get {
      return (ushort)(4);
    } }
    public static ushort FLG__IO__VALID { get {
      return (ushort)(1024);
    } }
    public static ushort FLG__COND { get {
      return (ushort)(2048);
    } }
    public static ushort FLG__COND__LAST { get {
      return (ushort)(4096);
    } }
    public static ushort FLG__BUSY { get {
      return (ushort)(32768);
    } }
    public static ushort EXT__PREG { get {
      return (ushort)(1);
    } }
    public static ushort EXT__COND { get {
      return (ushort)(2);
    } }
    public static ushort EXT__HOLD { get {
      return (ushort)(4);
    } }
  }

  public partial class Reg {
    private static readonly Dafny.ISequence<byte> Witness = ((System.Func<Dafny.ISequence<byte>>) (() => {
    BigInteger dim1 = new BigInteger(16);
    var arr1 = new byte[Dafny.Helpers.ToIntChecked(dim1, "array size exceeds memory limit")];
    for (int i1 = 0; i1 < dim1; i1++) {
      var _0___v0 = (BigInteger) i1;
      arr1[(int)(_0___v0)] = (byte)(0);
    }
    return Dafny.Sequence<byte>.FromArray(arr1);
  }))();
    public static Dafny.ISequence<byte> Default() {
      return Witness;
    }
    private static readonly Dafny.TypeDescriptor<Dafny.ISequence<byte>> _TYPE = new Dafny.TypeDescriptor<Dafny.ISequence<byte>>(TI59__Nibbles.Reg.Default());
    public static Dafny.TypeDescriptor<Dafny.ISequence<byte>> _TypeDescriptor() {
      return _TYPE;
    }
    public static bool _Is(Dafny.ISequence<byte> __source) {
      Dafny.ISequence<byte> _1_s = __source;
      return (new BigInteger((_1_s).Count)) == (new BigInteger(16));
    }
  }

  public interface _IMask {
    bool is_Mask { get; }
    BigInteger dtor_start { get; }
    BigInteger dtor_end { get; }
    BigInteger dtor_cpos { get; }
    byte dtor_cval { get; }
    _IMask DowncastClone();
  }
  public class Mask : _IMask {
    public readonly BigInteger _start;
    public readonly BigInteger _end;
    public readonly BigInteger _cpos;
    public readonly byte _cval;
    public Mask(BigInteger start, BigInteger end, BigInteger cpos, byte cval) {
      this._start = start;
      this._end = end;
      this._cpos = cpos;
      this._cval = cval;
    }
    public _IMask DowncastClone() {
      if (this is _IMask dt) { return dt; }
      return new Mask(_start, _end, _cpos, _cval);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Nibbles.Mask;
      return oth != null && this._start == oth._start && this._end == oth._end && this._cpos == oth._cpos && this._cval == oth._cval;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._start));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._end));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cpos));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cval));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Nibbles.Mask.Mask";
      s += "(";
      s += Dafny.Helpers.ToString(this._start);
      s += ", ";
      s += Dafny.Helpers.ToString(this._end);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cpos);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cval);
      s += ")";
      return s;
    }
    private static readonly TI59__Nibbles._IMask theDefault = create(BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, 0);
    public static TI59__Nibbles._IMask Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Nibbles._IMask> _TYPE = new Dafny.TypeDescriptor<TI59__Nibbles._IMask>(TI59__Nibbles.Mask.Default());
    public static Dafny.TypeDescriptor<TI59__Nibbles._IMask> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IMask create(BigInteger start, BigInteger end, BigInteger cpos, byte cval) {
      return new Mask(start, end, cpos, cval);
    }
    public static _IMask create_Mask(BigInteger start, BigInteger end, BigInteger cpos, byte cval) {
      return create(start, end, cpos, cval);
    }
    public bool is_Mask { get { return true; } }
    public BigInteger dtor_start {
      get {
        return this._start;
      }
    }
    public BigInteger dtor_end {
      get {
        return this._end;
      }
    }
    public BigInteger dtor_cpos {
      get {
        return this._cpos;
      }
    }
    public byte dtor_cval {
      get {
        return this._cval;
      }
    }
  }

  public partial class AluMask {
    private static readonly TI59__Nibbles._IMask Witness = TI59__Nibbles.Mask.create(BigInteger.Zero, new BigInteger(15), BigInteger.Zero, (byte)(0));
    public static TI59__Nibbles._IMask Default() {
      return Witness;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Nibbles._IMask> _TYPE = new Dafny.TypeDescriptor<TI59__Nibbles._IMask>(TI59__Nibbles.AluMask.Default());
    public static Dafny.TypeDescriptor<TI59__Nibbles._IMask> _TypeDescriptor() {
      return _TYPE;
    }
    public static bool _Is(TI59__Nibbles._IMask __source) {
      TI59__Nibbles._IMask _2_m = __source;
      return (((((_2_m).dtor_start).Sign != -1) && (((_2_m).dtor_start) <= (new BigInteger(15)))) && (((_2_m).dtor_start) <= (((_2_m).dtor_end) + (BigInteger.One)))) && (((_2_m).dtor_end) <= (new BigInteger(15)));
    }
  }
} // end of namespace TI59__Nibbles
namespace TI59__AluOps {

  public partial class __default {
    public static bool IsSubLike(TI59__AluOps._IAluOp op) {
      return (object.Equals(op, TI59__AluOps.AluOp.create_OP__SUB())) || (object.Equals(op, TI59__AluOps.AluOp.create_OP__SHR()));
    }
    public static bool IsShift(TI59__AluOps._IAluOp op) {
      return (object.Equals(op, TI59__AluOps.AluOp.create_OP__SHL())) || (object.Equals(op, TI59__AluOps.AluOp.create_OP__SHR()));
    }
    public static _System._ITuple2<byte, byte> CarryCorrect(BigInteger i, byte sum5, TI59__AluOps._IAluOp op)
    {
      if ((i).Sign == 0) {
        return _System.Tuple2<byte, byte>.create((((sum5) >= ((byte)(16))) ? ((byte)(1)) : ((byte)(0))), (((sum5) >= ((byte)(16))) ? ((byte)((sum5) & ((byte)(15)))) : (sum5)));
      } else {
        return _System.Tuple2<byte, byte>.create((((sum5) >= ((byte)(10))) ? ((byte)(1)) : ((byte)(0))), (((sum5) >= ((byte)(10))) ? (((!(TI59__AluOps.__default.IsSubLike(op))) ? (unchecked((byte)(((byte)((sum5) - ((byte)(10))))))) : (unchecked((byte)(((byte)((sum5) + ((byte)(10))))))))) : (sum5)));
      }
    }
    public static _System._ITuple2<Dafny.ISequence<byte>, byte> DstWrite(BigInteger i, Dafny.ISequence<byte> dst, byte shlIn, byte shr2, byte sum6, bool hasDst, TI59__AluOps._IAluOp op, TI59__Nibbles._IMask m)
    {
      if (!((((m).dtor_start) <= (i)) && ((i) <= ((m).dtor_end)))) {
        return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(dst, shlIn);
      } else if (!(hasDst)) {
        return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(dst, shlIn);
      } else if (object.Equals(op, TI59__AluOps.AluOp.create_OP__SHL())) {
        return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(TI59__Nibbles.__default.RegUpd(dst, i, shlIn), sum6);
      } else if (object.Equals(op, TI59__AluOps.AluOp.create_OP__SHR())) {
        if ((i) == ((m).dtor_end)) {
          return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(TI59__Nibbles.__default.RegUpd(((((i) > ((m).dtor_start)) && ((i) >= (BigInteger.One))) ? (TI59__Nibbles.__default.RegUpd(dst, (i) - (BigInteger.One), shr2)) : (dst)), i, (byte)(0)), sum6);
        } else if (((i) > ((m).dtor_start)) && ((i) >= (BigInteger.One))) {
          return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(TI59__Nibbles.__default.RegUpd(dst, (i) - (BigInteger.One), shr2), sum6);
        } else {
          return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(dst, sum6);
        }
      } else {
        return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(TI59__Nibbles.__default.RegUpd(dst, i, sum6), sum6);
      }
    }
    public static TI59__AluOps._IAluAcc AluStep(BigInteger i, byte carry, byte shl, Dafny.ISequence<byte> dst, Dafny.ISequence<byte> Sout, byte R5, bool condClear, bool hasDst, bool hasX, Dafny.ISequence<byte> x, bool hasY, Dafny.ISequence<byte> y, TI59__Nibbles._IMask m, TI59__AluOps._IAluOp op, Dafny.ISequence<byte> sin, bool ioValid)
    {
      byte _0_carryIn = (((i) == ((m).dtor_start)) ? ((byte)(0)) : (carry));
      byte _1_shlIn = (((i) == ((m).dtor_start)) ? ((byte)(0)) : (shl));
      byte _2_sum0 = ((hasY) ? ((y).Select(i)) : ((byte)(0)));
      byte _3_sum1 = ((ioValid) ? (_2_sum0) : ((byte)((_2_sum0) | ((sin).Select(i)))));
      byte _4_sum2 = (((i) == ((m).dtor_cpos)) ? ((byte)((_3_sum1) | ((m).dtor_cval))) : (_3_sum1));
      byte _5_shr = _4_sum2;
      byte _6_sum3 = unchecked((byte)(((byte)((_4_sum2) + (_0_carryIn)))));
      byte _7_sum4 = ((TI59__AluOps.__default.IsSubLike(op)) ? (unchecked((byte)(((byte)(((byte)(0)) - (_6_sum3)))))) : (_6_sum3));
      byte _8_sum5 = ((hasX) ? (unchecked((byte)(((byte)((_7_sum4) + ((x).Select(i))))))) : (_7_sum4));
      byte _9_shr2 = ((hasX) ? ((byte)((_5_shr) | ((x).Select(i)))) : (_5_shr));
      Dafny.ISequence<byte> _10_Sout1 = TI59__Nibbles.__default.RegUpd(Sout, i, (byte)((_8_sum5) & ((byte)(15))));
      _System._ITuple2<byte, byte> _11_corr = TI59__AluOps.__default.CarryCorrect(i, _8_sum5, op);
      byte _12_carryOut = (_11_corr).dtor__0;
      byte _13_sum6 = (_11_corr).dtor__1;
      _System._ITuple2<Dafny.ISequence<byte>, byte> _14_dw = TI59__AluOps.__default.DstWrite(i, dst, _1_shlIn, _9_shr2, _13_sum6, hasDst, op, m);
      Dafny.ISequence<byte> _15_dst1 = (_14_dw).dtor__0;
      byte _16_shl1 = (_14_dw).dtor__1;
      bool _17_inRange = (((m).dtor_start) <= (i)) && ((i) <= ((m).dtor_end));
      byte _18_R51 = (((_17_inRange) && ((i) == ((m).dtor_start))) ? (_13_sum6) : (R5));
      bool _19_cc1 = (condClear) || ((((_17_inRange) && ((i) == ((m).dtor_end))) && (!(TI59__AluOps.__default.IsShift(op)))) && ((_12_carryOut) == ((byte)(1))));
      return TI59__AluOps.AluAcc.create(_12_carryOut, _16_shl1, _15_dst1, _10_Sout1, _18_R51, _19_cc1);
    }
    public static TI59__AluOps._IAluRes AluDigits(BigInteger i, byte carry, byte shl, Dafny.ISequence<byte> dst, Dafny.ISequence<byte> Sout, byte R5, bool condClear, bool hasDst, bool hasX, Dafny.ISequence<byte> x, bool hasY, Dafny.ISequence<byte> y, TI59__Nibbles._IMask m, TI59__AluOps._IAluOp op, Dafny.ISequence<byte> sin, bool ioValid)
    {
    TAIL_CALL_START: ;
      if ((i) == (new BigInteger(16))) {
        return TI59__AluOps.AluRes.create(dst, Sout, R5, condClear, carry);
      } else {
        TI59__AluOps._IAluAcc _0_s = TI59__AluOps.__default.AluStep(i, carry, shl, dst, Sout, R5, condClear, hasDst, hasX, x, hasY, y, m, op, sin, ioValid);
        BigInteger _in0 = (i) + (BigInteger.One);
        byte _in1 = (_0_s).dtor_carry;
        byte _in2 = (_0_s).dtor_shl;
        Dafny.ISequence<byte> _in3 = (_0_s).dtor_dst;
        Dafny.ISequence<byte> _in4 = (_0_s).dtor_Sout;
        byte _in5 = (_0_s).dtor_R5;
        bool _in6 = (_0_s).dtor_cc;
        bool _in7 = hasDst;
        bool _in8 = hasX;
        Dafny.ISequence<byte> _in9 = x;
        bool _in10 = hasY;
        Dafny.ISequence<byte> _in11 = y;
        TI59__Nibbles._IMask _in12 = m;
        TI59__AluOps._IAluOp _in13 = op;
        Dafny.ISequence<byte> _in14 = sin;
        bool _in15 = ioValid;
        i = _in0;
        carry = _in1;
        shl = _in2;
        dst = _in3;
        Sout = _in4;
        R5 = _in5;
        condClear = _in6;
        hasDst = _in7;
        hasX = _in8;
        x = _in9;
        hasY = _in10;
        y = _in11;
        m = _in12;
        op = _in13;
        sin = _in14;
        ioValid = _in15;
        goto TAIL_CALL_START;
      }
    }
    public static TI59__AluOps._IAluRes AluExec(bool hasDst, Dafny.ISequence<byte> dst, bool hasX, Dafny.ISequence<byte> x, bool hasY, Dafny.ISequence<byte> y, TI59__Nibbles._IMask m, TI59__AluOps._IAluOp op, Dafny.ISequence<byte> sin, bool ioValid, byte R5, Dafny.ISequence<byte> Sout)
    {
      return TI59__AluOps.__default.AluDigits(BigInteger.Zero, (byte)(0), (byte)(0), dst, Sout, R5, false, hasDst, hasX, x, hasY, y, m, op, sin, ioValid);
    }
    public static _System._ITuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>> XchRange(BigInteger i, Dafny.ISequence<byte> s1, Dafny.ISequence<byte> s2, TI59__Nibbles._IMask m)
    {
    TAIL_CALL_START: ;
      if ((i) > ((m).dtor_end)) {
        return _System.Tuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>>.create(s1, s2);
      } else {
        BigInteger _in0 = (i) + (BigInteger.One);
        Dafny.ISequence<byte> _in1 = Dafny.Sequence<byte>.Update(s1, i, (s2).Select(i));
        Dafny.ISequence<byte> _in2 = Dafny.Sequence<byte>.Update(s2, i, (s1).Select(i));
        TI59__Nibbles._IMask _in3 = m;
        i = _in0;
        s1 = _in1;
        s2 = _in2;
        m = _in3;
        goto TAIL_CALL_START;
      }
    }
    public static _System._ITuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>> XchExec(Dafny.ISequence<byte> s1, Dafny.ISequence<byte> s2, TI59__Nibbles._IMask m)
    {
      return TI59__AluOps.__default.XchRange((m).dtor_start, s1, s2, m);
    }
    public static _System._ITuple2<Dafny.ISequence<byte>, byte> BcdAddSpec(BigInteger i, byte cin, Dafny.ISequence<byte> acc, Dafny.ISequence<byte> x, Dafny.ISequence<byte> y)
    {
    TAIL_CALL_START: ;
      if ((i) == (new BigInteger(16))) {
        return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(acc, cin);
      } else {
        BigInteger _0_s = ((new BigInteger((x).Select(i))) + (new BigInteger((y).Select(i)))) + (new BigInteger(cin));
        BigInteger _1_digit = (((i).Sign == 0) ? ((((_0_s) >= (new BigInteger(16))) ? ((_0_s) - (new BigInteger(16))) : (_0_s))) : ((((_0_s) >= (new BigInteger(10))) ? ((_0_s) - (new BigInteger(10))) : (_0_s))));
        BigInteger _2_cout = (((i).Sign == 0) ? ((((_0_s) >= (new BigInteger(16))) ? (BigInteger.One) : (BigInteger.Zero))) : ((((_0_s) >= (new BigInteger(10))) ? (BigInteger.One) : (BigInteger.Zero))));
        BigInteger _in0 = (i) + (BigInteger.One);
        byte _in1 = (byte)(_2_cout);
        Dafny.ISequence<byte> _in2 = Dafny.Sequence<byte>.Update(acc, i, (byte)(_1_digit));
        Dafny.ISequence<byte> _in3 = x;
        Dafny.ISequence<byte> _in4 = y;
        i = _in0;
        cin = _in1;
        acc = _in2;
        x = _in3;
        y = _in4;
        goto TAIL_CALL_START;
      }
    }
    public static _System._ITuple2<Dafny.ISequence<byte>, byte> BcdSubSpec(BigInteger i, byte bin, Dafny.ISequence<byte> acc, Dafny.ISequence<byte> x, Dafny.ISequence<byte> y)
    {
    TAIL_CALL_START: ;
      if ((i) == (new BigInteger(16))) {
        return _System.Tuple2<Dafny.ISequence<byte>, byte>.create(acc, bin);
      } else {
        BigInteger _0_d = (new BigInteger((x).Select(i))) - ((new BigInteger((y).Select(i))) + (new BigInteger(bin)));
        BigInteger _1_digit = (((_0_d).Sign == -1) ? ((((i).Sign == 0) ? ((_0_d) + (new BigInteger(16))) : ((_0_d) + (new BigInteger(10))))) : (_0_d));
        BigInteger _2_bout = (((_0_d).Sign == -1) ? (BigInteger.One) : (BigInteger.Zero));
        BigInteger _in0 = (i) + (BigInteger.One);
        byte _in1 = (byte)(_2_bout);
        Dafny.ISequence<byte> _in2 = Dafny.Sequence<byte>.Update(acc, i, (byte)(_1_digit));
        Dafny.ISequence<byte> _in3 = x;
        Dafny.ISequence<byte> _in4 = y;
        i = _in0;
        bin = _in1;
        acc = _in2;
        x = _in3;
        y = _in4;
        goto TAIL_CALL_START;
      }
    }
    public static TI59__Nibbles._IMask MASK__ALL { get {
      return TI59__Nibbles.Mask.create(BigInteger.Zero, new BigInteger(15), BigInteger.Zero, (byte)(0));
    } }
  }

  public interface _IAluOp {
    bool is_OP__ADD { get; }
    bool is_OP__SHL { get; }
    bool is_OP__SUB { get; }
    bool is_OP__SHR { get; }
    _IAluOp DowncastClone();
  }
  public abstract class AluOp : _IAluOp {
    public AluOp() {
    }
    private static readonly TI59__AluOps._IAluOp theDefault = create_OP__ADD();
    public static TI59__AluOps._IAluOp Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__AluOps._IAluOp> _TYPE = new Dafny.TypeDescriptor<TI59__AluOps._IAluOp>(TI59__AluOps.AluOp.Default());
    public static Dafny.TypeDescriptor<TI59__AluOps._IAluOp> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IAluOp create_OP__ADD() {
      return new AluOp_OP__ADD();
    }
    public static _IAluOp create_OP__SHL() {
      return new AluOp_OP__SHL();
    }
    public static _IAluOp create_OP__SUB() {
      return new AluOp_OP__SUB();
    }
    public static _IAluOp create_OP__SHR() {
      return new AluOp_OP__SHR();
    }
    public bool is_OP__ADD { get { return this is AluOp_OP__ADD; } }
    public bool is_OP__SHL { get { return this is AluOp_OP__SHL; } }
    public bool is_OP__SUB { get { return this is AluOp_OP__SUB; } }
    public bool is_OP__SHR { get { return this is AluOp_OP__SHR; } }
    public static System.Collections.Generic.IEnumerable<_IAluOp> AllSingletonConstructors {
      get {
        yield return AluOp.create_OP__ADD();
        yield return AluOp.create_OP__SHL();
        yield return AluOp.create_OP__SUB();
        yield return AluOp.create_OP__SHR();
      }
    }
    public abstract _IAluOp DowncastClone();
  }
  public class AluOp_OP__ADD : AluOp {
    public AluOp_OP__ADD() : base() {
    }
    public override _IAluOp DowncastClone() {
      if (this is _IAluOp dt) { return dt; }
      return new AluOp_OP__ADD();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__AluOps.AluOp_OP__ADD;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_AluOps.AluOp.OP_ADD";
      return s;
    }
  }
  public class AluOp_OP__SHL : AluOp {
    public AluOp_OP__SHL() : base() {
    }
    public override _IAluOp DowncastClone() {
      if (this is _IAluOp dt) { return dt; }
      return new AluOp_OP__SHL();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__AluOps.AluOp_OP__SHL;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 1;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_AluOps.AluOp.OP_SHL";
      return s;
    }
  }
  public class AluOp_OP__SUB : AluOp {
    public AluOp_OP__SUB() : base() {
    }
    public override _IAluOp DowncastClone() {
      if (this is _IAluOp dt) { return dt; }
      return new AluOp_OP__SUB();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__AluOps.AluOp_OP__SUB;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 2;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_AluOps.AluOp.OP_SUB";
      return s;
    }
  }
  public class AluOp_OP__SHR : AluOp {
    public AluOp_OP__SHR() : base() {
    }
    public override _IAluOp DowncastClone() {
      if (this is _IAluOp dt) { return dt; }
      return new AluOp_OP__SHR();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__AluOps.AluOp_OP__SHR;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 3;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_AluOps.AluOp.OP_SHR";
      return s;
    }
  }

  public interface _IAluRes {
    bool is_AluRes { get; }
    Dafny.ISequence<byte> dtor_dst { get; }
    Dafny.ISequence<byte> dtor_Sout { get; }
    byte dtor_R5 { get; }
    bool dtor_condClear { get; }
    byte dtor_carry { get; }
    _IAluRes DowncastClone();
  }
  public class AluRes : _IAluRes {
    public readonly Dafny.ISequence<byte> _dst;
    public readonly Dafny.ISequence<byte> _Sout;
    public readonly byte _R5;
    public readonly bool _condClear;
    public readonly byte _carry;
    public AluRes(Dafny.ISequence<byte> dst, Dafny.ISequence<byte> Sout, byte R5, bool condClear, byte carry) {
      this._dst = dst;
      this._Sout = Sout;
      this._R5 = R5;
      this._condClear = condClear;
      this._carry = carry;
    }
    public _IAluRes DowncastClone() {
      if (this is _IAluRes dt) { return dt; }
      return new AluRes(_dst, _Sout, _R5, _condClear, _carry);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__AluOps.AluRes;
      return oth != null && object.Equals(this._dst, oth._dst) && object.Equals(this._Sout, oth._Sout) && this._R5 == oth._R5 && this._condClear == oth._condClear && this._carry == oth._carry;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._dst));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._Sout));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._R5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._condClear));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._carry));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_AluOps.AluRes.AluRes";
      s += "(";
      s += Dafny.Helpers.ToString(this._dst);
      s += ", ";
      s += Dafny.Helpers.ToString(this._Sout);
      s += ", ";
      s += Dafny.Helpers.ToString(this._R5);
      s += ", ";
      s += Dafny.Helpers.ToString(this._condClear);
      s += ", ";
      s += Dafny.Helpers.ToString(this._carry);
      s += ")";
      return s;
    }
    private static readonly TI59__AluOps._IAluRes theDefault = create(TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), 0, false, 0);
    public static TI59__AluOps._IAluRes Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__AluOps._IAluRes> _TYPE = new Dafny.TypeDescriptor<TI59__AluOps._IAluRes>(TI59__AluOps.AluRes.Default());
    public static Dafny.TypeDescriptor<TI59__AluOps._IAluRes> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IAluRes create(Dafny.ISequence<byte> dst, Dafny.ISequence<byte> Sout, byte R5, bool condClear, byte carry) {
      return new AluRes(dst, Sout, R5, condClear, carry);
    }
    public static _IAluRes create_AluRes(Dafny.ISequence<byte> dst, Dafny.ISequence<byte> Sout, byte R5, bool condClear, byte carry) {
      return create(dst, Sout, R5, condClear, carry);
    }
    public bool is_AluRes { get { return true; } }
    public Dafny.ISequence<byte> dtor_dst {
      get {
        return this._dst;
      }
    }
    public Dafny.ISequence<byte> dtor_Sout {
      get {
        return this._Sout;
      }
    }
    public byte dtor_R5 {
      get {
        return this._R5;
      }
    }
    public bool dtor_condClear {
      get {
        return this._condClear;
      }
    }
    public byte dtor_carry {
      get {
        return this._carry;
      }
    }
  }

  public interface _IAluAcc {
    bool is_AluAcc { get; }
    byte dtor_carry { get; }
    byte dtor_shl { get; }
    Dafny.ISequence<byte> dtor_dst { get; }
    Dafny.ISequence<byte> dtor_Sout { get; }
    byte dtor_R5 { get; }
    bool dtor_cc { get; }
    _IAluAcc DowncastClone();
  }
  public class AluAcc : _IAluAcc {
    public readonly byte _carry;
    public readonly byte _shl;
    public readonly Dafny.ISequence<byte> _dst;
    public readonly Dafny.ISequence<byte> _Sout;
    public readonly byte _R5;
    public readonly bool _cc;
    public AluAcc(byte carry, byte shl, Dafny.ISequence<byte> dst, Dafny.ISequence<byte> Sout, byte R5, bool cc) {
      this._carry = carry;
      this._shl = shl;
      this._dst = dst;
      this._Sout = Sout;
      this._R5 = R5;
      this._cc = cc;
    }
    public _IAluAcc DowncastClone() {
      if (this is _IAluAcc dt) { return dt; }
      return new AluAcc(_carry, _shl, _dst, _Sout, _R5, _cc);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__AluOps.AluAcc;
      return oth != null && this._carry == oth._carry && this._shl == oth._shl && object.Equals(this._dst, oth._dst) && object.Equals(this._Sout, oth._Sout) && this._R5 == oth._R5 && this._cc == oth._cc;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._carry));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._shl));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._dst));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._Sout));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._R5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cc));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_AluOps.AluAcc.AluAcc";
      s += "(";
      s += Dafny.Helpers.ToString(this._carry);
      s += ", ";
      s += Dafny.Helpers.ToString(this._shl);
      s += ", ";
      s += Dafny.Helpers.ToString(this._dst);
      s += ", ";
      s += Dafny.Helpers.ToString(this._Sout);
      s += ", ";
      s += Dafny.Helpers.ToString(this._R5);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cc);
      s += ")";
      return s;
    }
    private static readonly TI59__AluOps._IAluAcc theDefault = create(0, 0, TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), 0, false);
    public static TI59__AluOps._IAluAcc Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__AluOps._IAluAcc> _TYPE = new Dafny.TypeDescriptor<TI59__AluOps._IAluAcc>(TI59__AluOps.AluAcc.Default());
    public static Dafny.TypeDescriptor<TI59__AluOps._IAluAcc> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IAluAcc create(byte carry, byte shl, Dafny.ISequence<byte> dst, Dafny.ISequence<byte> Sout, byte R5, bool cc) {
      return new AluAcc(carry, shl, dst, Sout, R5, cc);
    }
    public static _IAluAcc create_AluAcc(byte carry, byte shl, Dafny.ISequence<byte> dst, Dafny.ISequence<byte> Sout, byte R5, bool cc) {
      return create(carry, shl, dst, Sout, R5, cc);
    }
    public bool is_AluAcc { get { return true; } }
    public byte dtor_carry {
      get {
        return this._carry;
      }
    }
    public byte dtor_shl {
      get {
        return this._shl;
      }
    }
    public Dafny.ISequence<byte> dtor_dst {
      get {
        return this._dst;
      }
    }
    public Dafny.ISequence<byte> dtor_Sout {
      get {
        return this._Sout;
      }
    }
    public byte dtor_R5 {
      get {
        return this._R5;
      }
    }
    public bool dtor_cc {
      get {
        return this._cc;
      }
    }
  }
} // end of namespace TI59__AluOps
namespace TI59__Cpu {

  public partial class __default {
    public static TI59__Cpu._ICpu CpuInit() {
      return TI59__Cpu.Cpu.create(TI59__Nibbles.__default.ZeroReg(), TI59__Nibbles.__default.ZeroReg(), TI59__Nibbles.__default.ZeroReg(), TI59__Nibbles.__default.ZeroReg(), TI59__Nibbles.__default.ZeroReg(), (ushort)(0), (ushort)(0), (ushort)(0), (ushort)(0), (byte)(0), TI59__Nibbles.__default.ZeroReg(), TI59__Nibbles.__default.ZeroReg(), (ushort)(0), TI59__Nibbles.__default.FLG__COND, BigInteger.Zero, new BigInteger(5), false);
    }
    public static bool Flg(ushort f, ushort bit)
    {
      return ((ushort)((f) & (bit))) != ((ushort)(0));
    }
    public static Dafny.ISequence<byte> SelReg(TI59__Cpu._ICpu c, TI59__Cpu._IRegSel s)
    {
      TI59__Cpu._IRegSel _source0 = s;
      {
        if (_source0.is_RS__NULL) {
          return TI59__Nibbles.__default.ZeroReg();
        }
      }
      {
        if (_source0.is_RS__A) {
          return (c).dtor_A;
        }
      }
      {
        if (_source0.is_RS__B) {
          return (c).dtor_B;
        }
      }
      {
        if (_source0.is_RS__C) {
          return (c).dtor_C;
        }
      }
      {
        return (c).dtor_D;
      }
    }
    public static bool DstHasReg(TI59__Cpu._IDstSel d) {
      return (((object.Equals(d, TI59__Cpu.DstSel.create_DS__A())) || (object.Equals(d, TI59__Cpu.DstSel.create_DS__B()))) || (object.Equals(d, TI59__Cpu.DstSel.create_DS__C()))) || (object.Equals(d, TI59__Cpu.DstSel.create_DS__D()));
    }
    public static Dafny.ISequence<byte> DstReg(TI59__Cpu._ICpu c, TI59__Cpu._IDstSel d)
    {
      TI59__Cpu._IDstSel _source0 = d;
      {
        if (_source0.is_DS__A) {
          return (c).dtor_A;
        }
      }
      {
        if (_source0.is_DS__B) {
          return (c).dtor_B;
        }
      }
      {
        if (_source0.is_DS__C) {
          return (c).dtor_C;
        }
      }
      {
        return (c).dtor_D;
      }
    }
    public static TI59__Cpu._ICpu SetDstReg(TI59__Cpu._ICpu c, TI59__Cpu._IDstSel d, Dafny.ISequence<byte> v)
    {
      TI59__Cpu._IDstSel _source0 = d;
      {
        if (_source0.is_DS__A) {
          TI59__Cpu._ICpu _0_dt__update__tmp_h0 = c;
          Dafny.ISequence<byte> _1_dt__update_hA_h0 = v;
          return TI59__Cpu.Cpu.create(_1_dt__update_hA_h0, (_0_dt__update__tmp_h0).dtor_B, (_0_dt__update__tmp_h0).dtor_C, (_0_dt__update__tmp_h0).dtor_D, (_0_dt__update__tmp_h0).dtor_E, (_0_dt__update__tmp_h0).dtor_KR, (_0_dt__update__tmp_h0).dtor_SR, (_0_dt__update__tmp_h0).dtor_fA, (_0_dt__update__tmp_h0).dtor_fB, (_0_dt__update__tmp_h0).dtor_R5, (_0_dt__update__tmp_h0).dtor_Sout, (_0_dt__update__tmp_h0).dtor_Sin, (_0_dt__update__tmp_h0).dtor_opcode, (_0_dt__update__tmp_h0).dtor_flags, (_0_dt__update__tmp_h0).dtor_addr, (_0_dt__update__tmp_h0).dtor_reset, (_0_dt__update__tmp_h0).dtor_zeroSuppr);
        }
      }
      {
        if (_source0.is_DS__B) {
          TI59__Cpu._ICpu _2_dt__update__tmp_h1 = c;
          Dafny.ISequence<byte> _3_dt__update_hB_h0 = v;
          return TI59__Cpu.Cpu.create((_2_dt__update__tmp_h1).dtor_A, _3_dt__update_hB_h0, (_2_dt__update__tmp_h1).dtor_C, (_2_dt__update__tmp_h1).dtor_D, (_2_dt__update__tmp_h1).dtor_E, (_2_dt__update__tmp_h1).dtor_KR, (_2_dt__update__tmp_h1).dtor_SR, (_2_dt__update__tmp_h1).dtor_fA, (_2_dt__update__tmp_h1).dtor_fB, (_2_dt__update__tmp_h1).dtor_R5, (_2_dt__update__tmp_h1).dtor_Sout, (_2_dt__update__tmp_h1).dtor_Sin, (_2_dt__update__tmp_h1).dtor_opcode, (_2_dt__update__tmp_h1).dtor_flags, (_2_dt__update__tmp_h1).dtor_addr, (_2_dt__update__tmp_h1).dtor_reset, (_2_dt__update__tmp_h1).dtor_zeroSuppr);
        }
      }
      {
        if (_source0.is_DS__C) {
          TI59__Cpu._ICpu _4_dt__update__tmp_h2 = c;
          Dafny.ISequence<byte> _5_dt__update_hC_h0 = v;
          return TI59__Cpu.Cpu.create((_4_dt__update__tmp_h2).dtor_A, (_4_dt__update__tmp_h2).dtor_B, _5_dt__update_hC_h0, (_4_dt__update__tmp_h2).dtor_D, (_4_dt__update__tmp_h2).dtor_E, (_4_dt__update__tmp_h2).dtor_KR, (_4_dt__update__tmp_h2).dtor_SR, (_4_dt__update__tmp_h2).dtor_fA, (_4_dt__update__tmp_h2).dtor_fB, (_4_dt__update__tmp_h2).dtor_R5, (_4_dt__update__tmp_h2).dtor_Sout, (_4_dt__update__tmp_h2).dtor_Sin, (_4_dt__update__tmp_h2).dtor_opcode, (_4_dt__update__tmp_h2).dtor_flags, (_4_dt__update__tmp_h2).dtor_addr, (_4_dt__update__tmp_h2).dtor_reset, (_4_dt__update__tmp_h2).dtor_zeroSuppr);
        }
      }
      {
        TI59__Cpu._ICpu _6_dt__update__tmp_h3 = c;
        Dafny.ISequence<byte> _7_dt__update_hD_h0 = v;
        return TI59__Cpu.Cpu.create((_6_dt__update__tmp_h3).dtor_A, (_6_dt__update__tmp_h3).dtor_B, (_6_dt__update__tmp_h3).dtor_C, _7_dt__update_hD_h0, (_6_dt__update__tmp_h3).dtor_E, (_6_dt__update__tmp_h3).dtor_KR, (_6_dt__update__tmp_h3).dtor_SR, (_6_dt__update__tmp_h3).dtor_fA, (_6_dt__update__tmp_h3).dtor_fB, (_6_dt__update__tmp_h3).dtor_R5, (_6_dt__update__tmp_h3).dtor_Sout, (_6_dt__update__tmp_h3).dtor_Sin, (_6_dt__update__tmp_h3).dtor_opcode, (_6_dt__update__tmp_h3).dtor_flags, (_6_dt__update__tmp_h3).dtor_addr, (_6_dt__update__tmp_h3).dtor_reset, (_6_dt__update__tmp_h3).dtor_zeroSuppr);
      }
    }
    public static _System._ITuple2<bool, bool> RunEarly(ushort opcode) {
      BigInteger _0_g = new BigInteger((ushort)((opcode) & ((ushort)(7936))));
      if ((_0_g).Sign == 0) {
        return _System.Tuple2<bool, bool>.create(true, false);
      } else if ((_0_g) == (new BigInteger(2048))) {
        return _System.Tuple2<bool, bool>.create(true, false);
      } else if ((_0_g) == (new BigInteger(2560))) {
        BigInteger _1_low = new BigInteger((ushort)((opcode) & ((ushort)(15))));
        if ((_1_low).Sign == 0) {
          return _System.Tuple2<bool, bool>.create(true, false);
        } else if (((_1_low) == (new BigInteger(3))) || ((_1_low) == (new BigInteger(11)))) {
          return _System.Tuple2<bool, bool>.create(true, false);
        } else if ((_1_low) == (new BigInteger(12))) {
          return _System.Tuple2<bool, bool>.create(false, false);
        } else {
          return _System.Tuple2<bool, bool>.create(true, false);
        }
      } else if ((((ushort)((opcode) & ((ushort)(7)))) == ((ushort)(1))) && (((ushort)(((ushort)((opcode) >> ((int)((byte)(8))))) & ((ushort)(15)))) == ((ushort)(1)))) {
        return _System.Tuple2<bool, bool>.create(true, true);
      } else {
        return _System.Tuple2<bool, bool>.create(false, false);
      }
    }
    public static TI59__Cpu._ICpu ExecFlagOps(TI59__Cpu._ICpu c, ushort opcode)
    {
      BigInteger _0_bit = new BigInteger((ushort)(((ushort)((opcode) >> ((int)((byte)(4))))) & ((ushort)(15))));
      ushort _1_msk = TI59__Nibbles.__default.Bit16(_0_bit);
      BigInteger _2_f = new BigInteger((ushort)((opcode) & ((ushort)(15))));
      TI59__Cpu._ICpu _3_c0 = c;
      TI59__Cpu._ICpu _4_c1 = (((_2_f).Sign == 0) ? (((TI59__Cpu.__default.Flg((_3_c0).dtor_fA, _1_msk)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let0_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let0_0, _5_dt__update__tmp_h0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_flags) & ((ushort)(((ushort)(65535)) ^ (TI59__Nibbles.__default.FLG__COND)))), _pat_let1_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let1_0, _6_dt__update_hflags_h0 => TI59__Cpu.Cpu.create((_5_dt__update__tmp_h0).dtor_A, (_5_dt__update__tmp_h0).dtor_B, (_5_dt__update__tmp_h0).dtor_C, (_5_dt__update__tmp_h0).dtor_D, (_5_dt__update__tmp_h0).dtor_E, (_5_dt__update__tmp_h0).dtor_KR, (_5_dt__update__tmp_h0).dtor_SR, (_5_dt__update__tmp_h0).dtor_fA, (_5_dt__update__tmp_h0).dtor_fB, (_5_dt__update__tmp_h0).dtor_R5, (_5_dt__update__tmp_h0).dtor_Sout, (_5_dt__update__tmp_h0).dtor_Sin, (_5_dt__update__tmp_h0).dtor_opcode, _6_dt__update_hflags_h0, (_5_dt__update__tmp_h0).dtor_addr, (_5_dt__update__tmp_h0).dtor_reset, (_5_dt__update__tmp_h0).dtor_zeroSuppr)))))) : (_3_c0))) : ((((_2_f) == (BigInteger.One)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let2_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let2_0, _7_dt__update__tmp_h1 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fA) | (_1_msk)), _pat_let3_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let3_0, _8_dt__update_hfA_h0 => TI59__Cpu.Cpu.create((_7_dt__update__tmp_h1).dtor_A, (_7_dt__update__tmp_h1).dtor_B, (_7_dt__update__tmp_h1).dtor_C, (_7_dt__update__tmp_h1).dtor_D, (_7_dt__update__tmp_h1).dtor_E, (_7_dt__update__tmp_h1).dtor_KR, (_7_dt__update__tmp_h1).dtor_SR, _8_dt__update_hfA_h0, (_7_dt__update__tmp_h1).dtor_fB, (_7_dt__update__tmp_h1).dtor_R5, (_7_dt__update__tmp_h1).dtor_Sout, (_7_dt__update__tmp_h1).dtor_Sin, (_7_dt__update__tmp_h1).dtor_opcode, (_7_dt__update__tmp_h1).dtor_flags, (_7_dt__update__tmp_h1).dtor_addr, (_7_dt__update__tmp_h1).dtor_reset, (_7_dt__update__tmp_h1).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(2))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let4_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let4_0, _9_dt__update__tmp_h2 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fA) & ((ushort)(((ushort)(65535)) ^ (_1_msk)))), _pat_let5_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let5_0, _10_dt__update_hfA_h1 => TI59__Cpu.Cpu.create((_9_dt__update__tmp_h2).dtor_A, (_9_dt__update__tmp_h2).dtor_B, (_9_dt__update__tmp_h2).dtor_C, (_9_dt__update__tmp_h2).dtor_D, (_9_dt__update__tmp_h2).dtor_E, (_9_dt__update__tmp_h2).dtor_KR, (_9_dt__update__tmp_h2).dtor_SR, _10_dt__update_hfA_h1, (_9_dt__update__tmp_h2).dtor_fB, (_9_dt__update__tmp_h2).dtor_R5, (_9_dt__update__tmp_h2).dtor_Sout, (_9_dt__update__tmp_h2).dtor_Sin, (_9_dt__update__tmp_h2).dtor_opcode, (_9_dt__update__tmp_h2).dtor_flags, (_9_dt__update__tmp_h2).dtor_addr, (_9_dt__update__tmp_h2).dtor_reset, (_9_dt__update__tmp_h2).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(3))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let6_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let6_0, _11_dt__update__tmp_h3 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fA) ^ (_1_msk)), _pat_let7_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let7_0, _12_dt__update_hfA_h2 => TI59__Cpu.Cpu.create((_11_dt__update__tmp_h3).dtor_A, (_11_dt__update__tmp_h3).dtor_B, (_11_dt__update__tmp_h3).dtor_C, (_11_dt__update__tmp_h3).dtor_D, (_11_dt__update__tmp_h3).dtor_E, (_11_dt__update__tmp_h3).dtor_KR, (_11_dt__update__tmp_h3).dtor_SR, _12_dt__update_hfA_h2, (_11_dt__update__tmp_h3).dtor_fB, (_11_dt__update__tmp_h3).dtor_R5, (_11_dt__update__tmp_h3).dtor_Sout, (_11_dt__update__tmp_h3).dtor_Sin, (_11_dt__update__tmp_h3).dtor_opcode, (_11_dt__update__tmp_h3).dtor_flags, (_11_dt__update__tmp_h3).dtor_addr, (_11_dt__update__tmp_h3).dtor_reset, (_11_dt__update__tmp_h3).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(4))) ? (((((ushort)(((ushort)(((_3_c0).dtor_fA) ^ ((_3_c0).dtor_fB))) & (_1_msk))) != ((ushort)(0))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let8_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let8_0, _13_dt__update__tmp_h4 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fB) ^ (_1_msk)), _pat_let9_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let9_0, _14_dt__update_hfB_h0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fA) ^ (_1_msk)), _pat_let10_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let10_0, _15_dt__update_hfA_h3 => TI59__Cpu.Cpu.create((_13_dt__update__tmp_h4).dtor_A, (_13_dt__update__tmp_h4).dtor_B, (_13_dt__update__tmp_h4).dtor_C, (_13_dt__update__tmp_h4).dtor_D, (_13_dt__update__tmp_h4).dtor_E, (_13_dt__update__tmp_h4).dtor_KR, (_13_dt__update__tmp_h4).dtor_SR, _15_dt__update_hfA_h3, _14_dt__update_hfB_h0, (_13_dt__update__tmp_h4).dtor_R5, (_13_dt__update__tmp_h4).dtor_Sout, (_13_dt__update__tmp_h4).dtor_Sin, (_13_dt__update__tmp_h4).dtor_opcode, (_13_dt__update__tmp_h4).dtor_flags, (_13_dt__update__tmp_h4).dtor_addr, (_13_dt__update__tmp_h4).dtor_reset, (_13_dt__update__tmp_h4).dtor_zeroSuppr)))))))) : (_3_c0))) : ((((_2_f) == (new BigInteger(5))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let11_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let11_0, _16_dt__update__tmp_h5 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_KR) | (_1_msk)), _pat_let12_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let12_0, _17_dt__update_hKR_h0 => TI59__Cpu.Cpu.create((_16_dt__update__tmp_h5).dtor_A, (_16_dt__update__tmp_h5).dtor_B, (_16_dt__update__tmp_h5).dtor_C, (_16_dt__update__tmp_h5).dtor_D, (_16_dt__update__tmp_h5).dtor_E, _17_dt__update_hKR_h0, (_16_dt__update__tmp_h5).dtor_SR, (_16_dt__update__tmp_h5).dtor_fA, (_16_dt__update__tmp_h5).dtor_fB, (_16_dt__update__tmp_h5).dtor_R5, (_16_dt__update__tmp_h5).dtor_Sout, (_16_dt__update__tmp_h5).dtor_Sin, (_16_dt__update__tmp_h5).dtor_opcode, (_16_dt__update__tmp_h5).dtor_flags, (_16_dt__update__tmp_h5).dtor_addr, (_16_dt__update__tmp_h5).dtor_reset, (_16_dt__update__tmp_h5).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(6))) ? (((((ushort)(((ushort)(((_3_c0).dtor_fA) ^ ((_3_c0).dtor_fB))) & (_1_msk))) != ((ushort)(0))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let13_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let13_0, _18_dt__update__tmp_h6 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fA) ^ (_1_msk)), _pat_let14_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let14_0, _19_dt__update_hfA_h4 => TI59__Cpu.Cpu.create((_18_dt__update__tmp_h6).dtor_A, (_18_dt__update__tmp_h6).dtor_B, (_18_dt__update__tmp_h6).dtor_C, (_18_dt__update__tmp_h6).dtor_D, (_18_dt__update__tmp_h6).dtor_E, (_18_dt__update__tmp_h6).dtor_KR, (_18_dt__update__tmp_h6).dtor_SR, _19_dt__update_hfA_h4, (_18_dt__update__tmp_h6).dtor_fB, (_18_dt__update__tmp_h6).dtor_R5, (_18_dt__update__tmp_h6).dtor_Sout, (_18_dt__update__tmp_h6).dtor_Sin, (_18_dt__update__tmp_h6).dtor_opcode, (_18_dt__update__tmp_h6).dtor_flags, (_18_dt__update__tmp_h6).dtor_addr, (_18_dt__update__tmp_h6).dtor_reset, (_18_dt__update__tmp_h6).dtor_zeroSuppr)))))) : (_3_c0))) : ((((_2_f) == (new BigInteger(7))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let15_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let15_0, _20_dt__update__tmp_h7 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((ushort)(((_3_c0).dtor_fA) & ((ushort)(65505)))) | (unchecked((ushort)(((ushort)(((ushort)((byte)(((_3_c0).dtor_R5) & ((byte)(15))))) << ((int)((byte)(1))))))))), _pat_let16_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let16_0, _21_dt__update_hfA_h5 => TI59__Cpu.Cpu.create((_20_dt__update__tmp_h7).dtor_A, (_20_dt__update__tmp_h7).dtor_B, (_20_dt__update__tmp_h7).dtor_C, (_20_dt__update__tmp_h7).dtor_D, (_20_dt__update__tmp_h7).dtor_E, (_20_dt__update__tmp_h7).dtor_KR, (_20_dt__update__tmp_h7).dtor_SR, _21_dt__update_hfA_h5, (_20_dt__update__tmp_h7).dtor_fB, (_20_dt__update__tmp_h7).dtor_R5, (_20_dt__update__tmp_h7).dtor_Sout, (_20_dt__update__tmp_h7).dtor_Sin, (_20_dt__update__tmp_h7).dtor_opcode, (_20_dt__update__tmp_h7).dtor_flags, (_20_dt__update__tmp_h7).dtor_addr, (_20_dt__update__tmp_h7).dtor_reset, (_20_dt__update__tmp_h7).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(8))) ? (((TI59__Cpu.__default.Flg((_3_c0).dtor_fB, _1_msk)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let17_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let17_0, _22_dt__update__tmp_h8 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_flags) & ((ushort)(((ushort)(65535)) ^ (TI59__Nibbles.__default.FLG__COND)))), _pat_let18_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let18_0, _23_dt__update_hflags_h1 => TI59__Cpu.Cpu.create((_22_dt__update__tmp_h8).dtor_A, (_22_dt__update__tmp_h8).dtor_B, (_22_dt__update__tmp_h8).dtor_C, (_22_dt__update__tmp_h8).dtor_D, (_22_dt__update__tmp_h8).dtor_E, (_22_dt__update__tmp_h8).dtor_KR, (_22_dt__update__tmp_h8).dtor_SR, (_22_dt__update__tmp_h8).dtor_fA, (_22_dt__update__tmp_h8).dtor_fB, (_22_dt__update__tmp_h8).dtor_R5, (_22_dt__update__tmp_h8).dtor_Sout, (_22_dt__update__tmp_h8).dtor_Sin, (_22_dt__update__tmp_h8).dtor_opcode, _23_dt__update_hflags_h1, (_22_dt__update__tmp_h8).dtor_addr, (_22_dt__update__tmp_h8).dtor_reset, (_22_dt__update__tmp_h8).dtor_zeroSuppr)))))) : (_3_c0))) : ((((_2_f) == (new BigInteger(9))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let19_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let19_0, _24_dt__update__tmp_h9 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fB) | (_1_msk)), _pat_let20_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let20_0, _25_dt__update_hfB_h1 => TI59__Cpu.Cpu.create((_24_dt__update__tmp_h9).dtor_A, (_24_dt__update__tmp_h9).dtor_B, (_24_dt__update__tmp_h9).dtor_C, (_24_dt__update__tmp_h9).dtor_D, (_24_dt__update__tmp_h9).dtor_E, (_24_dt__update__tmp_h9).dtor_KR, (_24_dt__update__tmp_h9).dtor_SR, (_24_dt__update__tmp_h9).dtor_fA, _25_dt__update_hfB_h1, (_24_dt__update__tmp_h9).dtor_R5, (_24_dt__update__tmp_h9).dtor_Sout, (_24_dt__update__tmp_h9).dtor_Sin, (_24_dt__update__tmp_h9).dtor_opcode, (_24_dt__update__tmp_h9).dtor_flags, (_24_dt__update__tmp_h9).dtor_addr, (_24_dt__update__tmp_h9).dtor_reset, (_24_dt__update__tmp_h9).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(10))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let21_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let21_0, _26_dt__update__tmp_h10 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fB) & ((ushort)(((ushort)(65535)) ^ (_1_msk)))), _pat_let22_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let22_0, _27_dt__update_hfB_h2 => TI59__Cpu.Cpu.create((_26_dt__update__tmp_h10).dtor_A, (_26_dt__update__tmp_h10).dtor_B, (_26_dt__update__tmp_h10).dtor_C, (_26_dt__update__tmp_h10).dtor_D, (_26_dt__update__tmp_h10).dtor_E, (_26_dt__update__tmp_h10).dtor_KR, (_26_dt__update__tmp_h10).dtor_SR, (_26_dt__update__tmp_h10).dtor_fA, _27_dt__update_hfB_h2, (_26_dt__update__tmp_h10).dtor_R5, (_26_dt__update__tmp_h10).dtor_Sout, (_26_dt__update__tmp_h10).dtor_Sin, (_26_dt__update__tmp_h10).dtor_opcode, (_26_dt__update__tmp_h10).dtor_flags, (_26_dt__update__tmp_h10).dtor_addr, (_26_dt__update__tmp_h10).dtor_reset, (_26_dt__update__tmp_h10).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(11))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let23_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let23_0, _28_dt__update__tmp_h11 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fB) ^ (_1_msk)), _pat_let24_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let24_0, _29_dt__update_hfB_h3 => TI59__Cpu.Cpu.create((_28_dt__update__tmp_h11).dtor_A, (_28_dt__update__tmp_h11).dtor_B, (_28_dt__update__tmp_h11).dtor_C, (_28_dt__update__tmp_h11).dtor_D, (_28_dt__update__tmp_h11).dtor_E, (_28_dt__update__tmp_h11).dtor_KR, (_28_dt__update__tmp_h11).dtor_SR, (_28_dt__update__tmp_h11).dtor_fA, _29_dt__update_hfB_h3, (_28_dt__update__tmp_h11).dtor_R5, (_28_dt__update__tmp_h11).dtor_Sout, (_28_dt__update__tmp_h11).dtor_Sin, (_28_dt__update__tmp_h11).dtor_opcode, (_28_dt__update__tmp_h11).dtor_flags, (_28_dt__update__tmp_h11).dtor_addr, (_28_dt__update__tmp_h11).dtor_reset, (_28_dt__update__tmp_h11).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(12))) ? (((((ushort)(((ushort)(((_3_c0).dtor_fA) ^ ((_3_c0).dtor_fB))) & (_1_msk))) == ((ushort)(0))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let25_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let25_0, _30_dt__update__tmp_h12 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_flags) & ((ushort)(((ushort)(65535)) ^ (TI59__Nibbles.__default.FLG__COND)))), _pat_let26_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let26_0, _31_dt__update_hflags_h2 => TI59__Cpu.Cpu.create((_30_dt__update__tmp_h12).dtor_A, (_30_dt__update__tmp_h12).dtor_B, (_30_dt__update__tmp_h12).dtor_C, (_30_dt__update__tmp_h12).dtor_D, (_30_dt__update__tmp_h12).dtor_E, (_30_dt__update__tmp_h12).dtor_KR, (_30_dt__update__tmp_h12).dtor_SR, (_30_dt__update__tmp_h12).dtor_fA, (_30_dt__update__tmp_h12).dtor_fB, (_30_dt__update__tmp_h12).dtor_R5, (_30_dt__update__tmp_h12).dtor_Sout, (_30_dt__update__tmp_h12).dtor_Sin, (_30_dt__update__tmp_h12).dtor_opcode, _31_dt__update_hflags_h2, (_30_dt__update__tmp_h12).dtor_addr, (_30_dt__update__tmp_h12).dtor_reset, (_30_dt__update__tmp_h12).dtor_zeroSuppr)))))) : (_3_c0))) : ((((_2_f) == (new BigInteger(13))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let27_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let27_0, _32_dt__update__tmp_h13 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_KR) & ((ushort)(((ushort)(65535)) ^ (_1_msk)))), _pat_let28_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let28_0, _33_dt__update_hKR_h1 => TI59__Cpu.Cpu.create((_32_dt__update__tmp_h13).dtor_A, (_32_dt__update__tmp_h13).dtor_B, (_32_dt__update__tmp_h13).dtor_C, (_32_dt__update__tmp_h13).dtor_D, (_32_dt__update__tmp_h13).dtor_E, _33_dt__update_hKR_h1, (_32_dt__update__tmp_h13).dtor_SR, (_32_dt__update__tmp_h13).dtor_fA, (_32_dt__update__tmp_h13).dtor_fB, (_32_dt__update__tmp_h13).dtor_R5, (_32_dt__update__tmp_h13).dtor_Sout, (_32_dt__update__tmp_h13).dtor_Sin, (_32_dt__update__tmp_h13).dtor_opcode, (_32_dt__update__tmp_h13).dtor_flags, (_32_dt__update__tmp_h13).dtor_addr, (_32_dt__update__tmp_h13).dtor_reset, (_32_dt__update__tmp_h13).dtor_zeroSuppr)))))) : ((((_2_f) == (new BigInteger(14))) ? (((((ushort)(((ushort)(((_3_c0).dtor_fA) ^ ((_3_c0).dtor_fB))) & (_1_msk))) != ((ushort)(0))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let29_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let29_0, _34_dt__update__tmp_h14 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_3_c0).dtor_fB) ^ (_1_msk)), _pat_let30_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let30_0, _35_dt__update_hfB_h4 => TI59__Cpu.Cpu.create((_34_dt__update__tmp_h14).dtor_A, (_34_dt__update__tmp_h14).dtor_B, (_34_dt__update__tmp_h14).dtor_C, (_34_dt__update__tmp_h14).dtor_D, (_34_dt__update__tmp_h14).dtor_E, (_34_dt__update__tmp_h14).dtor_KR, (_34_dt__update__tmp_h14).dtor_SR, (_34_dt__update__tmp_h14).dtor_fA, _35_dt__update_hfB_h4, (_34_dt__update__tmp_h14).dtor_R5, (_34_dt__update__tmp_h14).dtor_Sout, (_34_dt__update__tmp_h14).dtor_Sin, (_34_dt__update__tmp_h14).dtor_opcode, (_34_dt__update__tmp_h14).dtor_flags, (_34_dt__update__tmp_h14).dtor_addr, (_34_dt__update__tmp_h14).dtor_reset, (_34_dt__update__tmp_h14).dtor_zeroSuppr)))))) : (_3_c0))) : (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_3_c0, _pat_let31_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let31_0, _36_dt__update__tmp_h15 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((ushort)(((_3_c0).dtor_fB) & ((ushort)(65505)))) | (unchecked((ushort)(((ushort)(((ushort)((byte)(((_3_c0).dtor_R5) & ((byte)(15))))) << ((int)((byte)(1))))))))), _pat_let32_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let32_0, _37_dt__update_hfB_h5 => TI59__Cpu.Cpu.create((_36_dt__update__tmp_h15).dtor_A, (_36_dt__update__tmp_h15).dtor_B, (_36_dt__update__tmp_h15).dtor_C, (_36_dt__update__tmp_h15).dtor_D, (_36_dt__update__tmp_h15).dtor_E, (_36_dt__update__tmp_h15).dtor_KR, (_36_dt__update__tmp_h15).dtor_SR, (_36_dt__update__tmp_h15).dtor_fA, _37_dt__update_hfB_h5, (_36_dt__update__tmp_h15).dtor_R5, (_36_dt__update__tmp_h15).dtor_Sout, (_36_dt__update__tmp_h15).dtor_Sin, (_36_dt__update__tmp_h15).dtor_opcode, (_36_dt__update__tmp_h15).dtor_flags, (_36_dt__update__tmp_h15).dtor_addr, (_36_dt__update__tmp_h15).dtor_reset, (_36_dt__update__tmp_h15).dtor_zeroSuppr)))))))))))))))))))))))))))))))))));
      return _4_c1;
    }
    public static BigInteger Ctz8(byte x) {
      if (((byte)((x) & (TI59__Nibbles.__default.Bit8(BigInteger.Zero)))) != ((byte)(0))) {
        return BigInteger.Zero;
      } else if (((byte)((x) & (TI59__Nibbles.__default.Bit8(BigInteger.One)))) != ((byte)(0))) {
        return BigInteger.One;
      } else if (((byte)((x) & (TI59__Nibbles.__default.Bit8(new BigInteger(2))))) != ((byte)(0))) {
        return new BigInteger(2);
      } else if (((byte)((x) & (TI59__Nibbles.__default.Bit8(new BigInteger(3))))) != ((byte)(0))) {
        return new BigInteger(3);
      } else if (((byte)((x) & (TI59__Nibbles.__default.Bit8(new BigInteger(4))))) != ((byte)(0))) {
        return new BigInteger(4);
      } else if (((byte)((x) & (TI59__Nibbles.__default.Bit8(new BigInteger(5))))) != ((byte)(0))) {
        return new BigInteger(5);
      } else if (((byte)((x) & (TI59__Nibbles.__default.Bit8(new BigInteger(6))))) != ((byte)(0))) {
        return new BigInteger(6);
      } else {
        return new BigInteger(7);
      }
    }
    public static TI59__Cpu._ICpu ExecKeyb(TI59__Cpu._ICpu c, ushort opcode, BigInteger digit, byte keyLine)
    {
      byte _0_km0 = (byte)((ushort)(((ushort)(((ushort)((opcode) & ((ushort)(7)))) | ((ushort)(((ushort)((opcode) >> ((int)((byte)(1))))) & ((ushort)(120)))))) ^ ((ushort)(127))));
      byte _1_km1 = (byte)((_0_km0) & (keyLine));
      byte _2_km = ((((byte)((_1_km1) & (unchecked((byte)(((byte)((_1_km1) - ((byte)(1))))))))) != ((byte)(0))) ? ((byte)(0)) : (_1_km1));
      if (((ushort)((opcode) & ((ushort)(8)))) == ((ushort)(0))) {
        if (((byte)((keyLine) & (_2_km))) != ((byte)(0))) {
          BigInteger _3_b = TI59__Cpu.__default.Ctz8(_2_km);
          TI59__Cpu._ICpu _4_dt__update__tmp_h0 = c;
          ushort _5_dt__update_hflags_h0 = (ushort)(((c).dtor_flags) & ((ushort)(((ushort)(65535)) ^ (TI59__Nibbles.__default.FLG__COND))));
          ushort _6_dt__update_hKR_h0 = (ushort)((unchecked((ushort)(((ushort)(((ushort)(digit)) << ((int)((byte)(4)))))))) | ((ushort)((unchecked((ushort)(((ushort)(((ushort)(_3_b)) << ((int)((byte)(8)))))))) & ((ushort)(1792)))));
          return TI59__Cpu.Cpu.create((_4_dt__update__tmp_h0).dtor_A, (_4_dt__update__tmp_h0).dtor_B, (_4_dt__update__tmp_h0).dtor_C, (_4_dt__update__tmp_h0).dtor_D, (_4_dt__update__tmp_h0).dtor_E, _6_dt__update_hKR_h0, (_4_dt__update__tmp_h0).dtor_SR, (_4_dt__update__tmp_h0).dtor_fA, (_4_dt__update__tmp_h0).dtor_fB, (_4_dt__update__tmp_h0).dtor_R5, (_4_dt__update__tmp_h0).dtor_Sout, (_4_dt__update__tmp_h0).dtor_Sin, (_4_dt__update__tmp_h0).dtor_opcode, _5_dt__update_hflags_h0, (_4_dt__update__tmp_h0).dtor_addr, (_4_dt__update__tmp_h0).dtor_reset, (_4_dt__update__tmp_h0).dtor_zeroSuppr);
        } else if ((digit) != (new BigInteger(15))) {
          TI59__Cpu._ICpu _7_dt__update__tmp_h1 = c;
          ushort _8_dt__update_hflags_h1 = (ushort)(((c).dtor_flags) | (TI59__Nibbles.__default.FLG__HOLD));
          return TI59__Cpu.Cpu.create((_7_dt__update__tmp_h1).dtor_A, (_7_dt__update__tmp_h1).dtor_B, (_7_dt__update__tmp_h1).dtor_C, (_7_dt__update__tmp_h1).dtor_D, (_7_dt__update__tmp_h1).dtor_E, (_7_dt__update__tmp_h1).dtor_KR, (_7_dt__update__tmp_h1).dtor_SR, (_7_dt__update__tmp_h1).dtor_fA, (_7_dt__update__tmp_h1).dtor_fB, (_7_dt__update__tmp_h1).dtor_R5, (_7_dt__update__tmp_h1).dtor_Sout, (_7_dt__update__tmp_h1).dtor_Sin, (_7_dt__update__tmp_h1).dtor_opcode, _8_dt__update_hflags_h1, (_7_dt__update__tmp_h1).dtor_addr, (_7_dt__update__tmp_h1).dtor_reset, (_7_dt__update__tmp_h1).dtor_zeroSuppr);
        } else {
          return c;
        }
      } else if (((byte)((keyLine) & (_2_km))) != ((byte)(0))) {
        TI59__Cpu._ICpu _9_dt__update__tmp_h2 = c;
        ushort _10_dt__update_hflags_h2 = (ushort)(((c).dtor_flags) & ((ushort)(((ushort)(65535)) ^ (TI59__Nibbles.__default.FLG__COND))));
        return TI59__Cpu.Cpu.create((_9_dt__update__tmp_h2).dtor_A, (_9_dt__update__tmp_h2).dtor_B, (_9_dt__update__tmp_h2).dtor_C, (_9_dt__update__tmp_h2).dtor_D, (_9_dt__update__tmp_h2).dtor_E, (_9_dt__update__tmp_h2).dtor_KR, (_9_dt__update__tmp_h2).dtor_SR, (_9_dt__update__tmp_h2).dtor_fA, (_9_dt__update__tmp_h2).dtor_fB, (_9_dt__update__tmp_h2).dtor_R5, (_9_dt__update__tmp_h2).dtor_Sout, (_9_dt__update__tmp_h2).dtor_Sin, (_9_dt__update__tmp_h2).dtor_opcode, _10_dt__update_hflags_h2, (_9_dt__update__tmp_h2).dtor_addr, (_9_dt__update__tmp_h2).dtor_reset, (_9_dt__update__tmp_h2).dtor_zeroSuppr);
      } else {
        return c;
      }
    }
    public static TI59__Cpu._ICpu ExecWait(TI59__Cpu._ICpu c, ushort opcode, BigInteger digit, ushort ext, byte keyLine)
    {
      var _pat_let_tv0 = opcode;
      var _pat_let_tv1 = ext;
      var _pat_let_tv2 = ext;
      BigInteger _0_f = new BigInteger((ushort)((opcode) & ((ushort)(15))));
      BigInteger _1_arg = new BigInteger((ushort)(((ushort)((opcode) >> ((int)((byte)(4))))) & ((ushort)(15))));
      TI59__Cpu._ICpu _2_c0 = c;
      TI59__Cpu._ICpu _3_c1 = (((_0_f).Sign == 0) ? ((((digit) != (_1_arg)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let33_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let33_0, _4_dt__update__tmp_h0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_2_c0).dtor_flags) | (TI59__Nibbles.__default.FLG__HOLD)), _pat_let34_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let34_0, _5_dt__update_hflags_h0 => TI59__Cpu.Cpu.create((_4_dt__update__tmp_h0).dtor_A, (_4_dt__update__tmp_h0).dtor_B, (_4_dt__update__tmp_h0).dtor_C, (_4_dt__update__tmp_h0).dtor_D, (_4_dt__update__tmp_h0).dtor_E, (_4_dt__update__tmp_h0).dtor_KR, (_4_dt__update__tmp_h0).dtor_SR, (_4_dt__update__tmp_h0).dtor_fA, (_4_dt__update__tmp_h0).dtor_fB, (_4_dt__update__tmp_h0).dtor_R5, (_4_dt__update__tmp_h0).dtor_Sout, (_4_dt__update__tmp_h0).dtor_Sin, (_4_dt__update__tmp_h0).dtor_opcode, _5_dt__update_hflags_h0, (_4_dt__update__tmp_h0).dtor_addr, (_4_dt__update__tmp_h0).dtor_reset, (_4_dt__update__tmp_h0).dtor_zeroSuppr)))))) : (_2_c0))) : ((((_0_f) == (BigInteger.One)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let35_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let35_0, _6_dt__update__tmp_h1 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_2_c0).dtor_flags) & ((ushort)(65534))), _pat_let36_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let36_0, _7_dt__update_hflags_h1 => TI59__Cpu.Cpu.create((_6_dt__update__tmp_h1).dtor_A, (_6_dt__update__tmp_h1).dtor_B, (_6_dt__update__tmp_h1).dtor_C, (_6_dt__update__tmp_h1).dtor_D, (_6_dt__update__tmp_h1).dtor_E, (_6_dt__update__tmp_h1).dtor_KR, (_6_dt__update__tmp_h1).dtor_SR, (_6_dt__update__tmp_h1).dtor_fA, (_6_dt__update__tmp_h1).dtor_fB, (_6_dt__update__tmp_h1).dtor_R5, (_6_dt__update__tmp_h1).dtor_Sout, (_6_dt__update__tmp_h1).dtor_Sin, (_6_dt__update__tmp_h1).dtor_opcode, _7_dt__update_hflags_h1, (_6_dt__update__tmp_h1).dtor_addr, (_6_dt__update__tmp_h1).dtor_reset, (_6_dt__update__tmp_h1).dtor_zeroSuppr)))))) : ((((_0_f) == (new BigInteger(2))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let37_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let37_0, _8_dt__update__tmp_h2 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(0), _pat_let38_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let38_0, _9_dt__update_hfA_h0 => TI59__Cpu.Cpu.create((_8_dt__update__tmp_h2).dtor_A, (_8_dt__update__tmp_h2).dtor_B, (_8_dt__update__tmp_h2).dtor_C, (_8_dt__update__tmp_h2).dtor_D, (_8_dt__update__tmp_h2).dtor_E, (_8_dt__update__tmp_h2).dtor_KR, (_8_dt__update__tmp_h2).dtor_SR, _9_dt__update_hfA_h0, (_8_dt__update__tmp_h2).dtor_fB, (_8_dt__update__tmp_h2).dtor_R5, (_8_dt__update__tmp_h2).dtor_Sout, (_8_dt__update__tmp_h2).dtor_Sin, (_8_dt__update__tmp_h2).dtor_opcode, (_8_dt__update__tmp_h2).dtor_flags, (_8_dt__update__tmp_h2).dtor_addr, (_8_dt__update__tmp_h2).dtor_reset, (_8_dt__update__tmp_h2).dtor_zeroSuppr)))))) : ((((_0_f) == (new BigInteger(3))) ? (_2_c0) : ((((_0_f) == (new BigInteger(4))) ? (Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(unchecked((ushort)(((ushort)(((_2_c0).dtor_KR) + ((ushort)(16)))))), _pat_let39_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let39_0, _10_kr => ((((ushort)((_10_kr) & ((ushort)(65520)))) == ((ushort)(0))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let40_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let40_0, _11_dt__update__tmp_h3 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)((_10_kr) ^ ((ushort)(1))), _pat_let41_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let41_0, _12_dt__update_hKR_h0 => TI59__Cpu.Cpu.create((_11_dt__update__tmp_h3).dtor_A, (_11_dt__update__tmp_h3).dtor_B, (_11_dt__update__tmp_h3).dtor_C, (_11_dt__update__tmp_h3).dtor_D, (_11_dt__update__tmp_h3).dtor_E, _12_dt__update_hKR_h0, (_11_dt__update__tmp_h3).dtor_SR, (_11_dt__update__tmp_h3).dtor_fA, (_11_dt__update__tmp_h3).dtor_fB, (_11_dt__update__tmp_h3).dtor_R5, (_11_dt__update__tmp_h3).dtor_Sout, (_11_dt__update__tmp_h3).dtor_Sin, (_11_dt__update__tmp_h3).dtor_opcode, (_11_dt__update__tmp_h3).dtor_flags, (_11_dt__update__tmp_h3).dtor_addr, (_11_dt__update__tmp_h3).dtor_reset, (_11_dt__update__tmp_h3).dtor_zeroSuppr)))))) : (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let42_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let42_0, _13_dt__update__tmp_h4 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_10_kr, _pat_let43_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let43_0, _14_dt__update_hKR_h1 => TI59__Cpu.Cpu.create((_13_dt__update__tmp_h4).dtor_A, (_13_dt__update__tmp_h4).dtor_B, (_13_dt__update__tmp_h4).dtor_C, (_13_dt__update__tmp_h4).dtor_D, (_13_dt__update__tmp_h4).dtor_E, _14_dt__update_hKR_h1, (_13_dt__update__tmp_h4).dtor_SR, (_13_dt__update__tmp_h4).dtor_fA, (_13_dt__update__tmp_h4).dtor_fB, (_13_dt__update__tmp_h4).dtor_R5, (_13_dt__update__tmp_h4).dtor_Sout, (_13_dt__update__tmp_h4).dtor_Sin, (_13_dt__update__tmp_h4).dtor_opcode, (_13_dt__update__tmp_h4).dtor_flags, (_13_dt__update__tmp_h4).dtor_addr, (_13_dt__update__tmp_h4).dtor_reset, (_13_dt__update__tmp_h4).dtor_zeroSuppr)))))))))) : ((((_0_f) == (new BigInteger(5))) ? (((TI59__Cpu.__default.Flg((_2_c0).dtor_KR, TI59__Nibbles.__default.Bit16(_1_arg))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let44_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let44_0, _15_dt__update__tmp_h5 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_2_c0).dtor_flags) & ((ushort)(((ushort)(65535)) ^ (TI59__Nibbles.__default.FLG__COND)))), _pat_let45_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let45_0, _16_dt__update_hflags_h2 => TI59__Cpu.Cpu.create((_15_dt__update__tmp_h5).dtor_A, (_15_dt__update__tmp_h5).dtor_B, (_15_dt__update__tmp_h5).dtor_C, (_15_dt__update__tmp_h5).dtor_D, (_15_dt__update__tmp_h5).dtor_E, (_15_dt__update__tmp_h5).dtor_KR, (_15_dt__update__tmp_h5).dtor_SR, (_15_dt__update__tmp_h5).dtor_fA, (_15_dt__update__tmp_h5).dtor_fB, (_15_dt__update__tmp_h5).dtor_R5, (_15_dt__update__tmp_h5).dtor_Sout, (_15_dt__update__tmp_h5).dtor_Sin, (_15_dt__update__tmp_h5).dtor_opcode, _16_dt__update_hflags_h2, (_15_dt__update__tmp_h5).dtor_addr, (_15_dt__update__tmp_h5).dtor_reset, (_15_dt__update__tmp_h5).dtor_zeroSuppr)))))) : (_2_c0))) : ((((_0_f) == (new BigInteger(6))) ? (Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(new BigInteger((ushort)((opcode) & ((ushort)(240)))), _pat_let46_0 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(_pat_let46_0, _17_sel => (((_17_sel) == (new BigInteger(16))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let47_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let47_0, _18_dt__update__tmp_h6 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>((byte)((ushort)(((ushort)(((_2_c0).dtor_fB) >> ((int)((byte)(1))))) & ((ushort)(15)))), _pat_let48_0 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>(_pat_let48_0, _19_dt__update_hR5_h0 => TI59__Cpu.Cpu.create((_18_dt__update__tmp_h6).dtor_A, (_18_dt__update__tmp_h6).dtor_B, (_18_dt__update__tmp_h6).dtor_C, (_18_dt__update__tmp_h6).dtor_D, (_18_dt__update__tmp_h6).dtor_E, (_18_dt__update__tmp_h6).dtor_KR, (_18_dt__update__tmp_h6).dtor_SR, (_18_dt__update__tmp_h6).dtor_fA, (_18_dt__update__tmp_h6).dtor_fB, _19_dt__update_hR5_h0, (_18_dt__update__tmp_h6).dtor_Sout, (_18_dt__update__tmp_h6).dtor_Sin, (_18_dt__update__tmp_h6).dtor_opcode, (_18_dt__update__tmp_h6).dtor_flags, (_18_dt__update__tmp_h6).dtor_addr, (_18_dt__update__tmp_h6).dtor_reset, (_18_dt__update__tmp_h6).dtor_zeroSuppr)))))) : ((((_17_sel).Sign == 0) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let49_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let49_0, _20_dt__update__tmp_h7 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>((byte)((ushort)(((ushort)(((_2_c0).dtor_fA) >> ((int)((byte)(1))))) & ((ushort)(15)))), _pat_let50_0 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>(_pat_let50_0, _21_dt__update_hR5_h1 => TI59__Cpu.Cpu.create((_20_dt__update__tmp_h7).dtor_A, (_20_dt__update__tmp_h7).dtor_B, (_20_dt__update__tmp_h7).dtor_C, (_20_dt__update__tmp_h7).dtor_D, (_20_dt__update__tmp_h7).dtor_E, (_20_dt__update__tmp_h7).dtor_KR, (_20_dt__update__tmp_h7).dtor_SR, (_20_dt__update__tmp_h7).dtor_fA, (_20_dt__update__tmp_h7).dtor_fB, _21_dt__update_hR5_h1, (_20_dt__update__tmp_h7).dtor_Sout, (_20_dt__update__tmp_h7).dtor_Sin, (_20_dt__update__tmp_h7).dtor_opcode, (_20_dt__update__tmp_h7).dtor_flags, (_20_dt__update__tmp_h7).dtor_addr, (_20_dt__update__tmp_h7).dtor_reset, (_20_dt__update__tmp_h7).dtor_zeroSuppr)))))) : (_2_c0))))))) : ((((_0_f) == (new BigInteger(7))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let51_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let51_0, _22_dt__update__tmp_h8 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>((byte)((ushort)(((ushort)((_pat_let_tv0) >> ((int)((byte)(4))))) & ((ushort)(15)))), _pat_let52_0 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>(_pat_let52_0, _23_dt__update_hR5_h2 => TI59__Cpu.Cpu.create((_22_dt__update__tmp_h8).dtor_A, (_22_dt__update__tmp_h8).dtor_B, (_22_dt__update__tmp_h8).dtor_C, (_22_dt__update__tmp_h8).dtor_D, (_22_dt__update__tmp_h8).dtor_E, (_22_dt__update__tmp_h8).dtor_KR, (_22_dt__update__tmp_h8).dtor_SR, (_22_dt__update__tmp_h8).dtor_fA, (_22_dt__update__tmp_h8).dtor_fB, _23_dt__update_hR5_h2, (_22_dt__update__tmp_h8).dtor_Sout, (_22_dt__update__tmp_h8).dtor_Sin, (_22_dt__update__tmp_h8).dtor_opcode, (_22_dt__update__tmp_h8).dtor_flags, (_22_dt__update__tmp_h8).dtor_addr, (_22_dt__update__tmp_h8).dtor_reset, (_22_dt__update__tmp_h8).dtor_zeroSuppr)))))) : ((((_0_f) == (new BigInteger(8))) ? (Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(new BigInteger((ushort)((opcode) & ((ushort)(240)))), _pat_let53_0 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(_pat_let53_0, _24_sel => (((_24_sel).Sign == 0) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let54_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let54_0, _25_dt__update__tmp_h9 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>((byte)((ushort)(((ushort)(((_2_c0).dtor_KR) >> ((int)((byte)(4))))) & ((ushort)(15)))), _pat_let55_0 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>(_pat_let55_0, _26_dt__update_hR5_h3 => TI59__Cpu.Cpu.create((_25_dt__update__tmp_h9).dtor_A, (_25_dt__update__tmp_h9).dtor_B, (_25_dt__update__tmp_h9).dtor_C, (_25_dt__update__tmp_h9).dtor_D, (_25_dt__update__tmp_h9).dtor_E, (_25_dt__update__tmp_h9).dtor_KR, (_25_dt__update__tmp_h9).dtor_SR, (_25_dt__update__tmp_h9).dtor_fA, (_25_dt__update__tmp_h9).dtor_fB, _26_dt__update_hR5_h3, (_25_dt__update__tmp_h9).dtor_Sout, (_25_dt__update__tmp_h9).dtor_Sin, (_25_dt__update__tmp_h9).dtor_opcode, (_25_dt__update__tmp_h9).dtor_flags, (_25_dt__update__tmp_h9).dtor_addr, (_25_dt__update__tmp_h9).dtor_reset, (_25_dt__update__tmp_h9).dtor_zeroSuppr)))))) : ((((_24_sel) == (new BigInteger(16))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let56_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let56_0, _27_dt__update__tmp_h10 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((ushort)(((_2_c0).dtor_KR) & ((ushort)(65295)))) | (unchecked((ushort)(((ushort)(((ushort)((_2_c0).dtor_R5)) << ((int)((byte)(4))))))))), _pat_let57_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let57_0, _28_dt__update_hKR_h2 => TI59__Cpu.Cpu.create((_27_dt__update__tmp_h10).dtor_A, (_27_dt__update__tmp_h10).dtor_B, (_27_dt__update__tmp_h10).dtor_C, (_27_dt__update__tmp_h10).dtor_D, (_27_dt__update__tmp_h10).dtor_E, _28_dt__update_hKR_h2, (_27_dt__update__tmp_h10).dtor_SR, (_27_dt__update__tmp_h10).dtor_fA, (_27_dt__update__tmp_h10).dtor_fB, (_27_dt__update__tmp_h10).dtor_R5, (_27_dt__update__tmp_h10).dtor_Sout, (_27_dt__update__tmp_h10).dtor_Sin, (_27_dt__update__tmp_h10).dtor_opcode, (_27_dt__update__tmp_h10).dtor_flags, (_27_dt__update__tmp_h10).dtor_addr, (_27_dt__update__tmp_h10).dtor_reset, (_27_dt__update__tmp_h10).dtor_zeroSuppr)))))) : (_2_c0))))))) : ((((_0_f) == (new BigInteger(9))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let58_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let58_0, _29_dt__update__tmp_h11 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_2_c0).dtor_flags) | (TI59__Nibbles.__default.FLG__IDLE)), _pat_let59_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let59_0, _30_dt__update_hflags_h3 => TI59__Cpu.Cpu.create((_29_dt__update__tmp_h11).dtor_A, (_29_dt__update__tmp_h11).dtor_B, (_29_dt__update__tmp_h11).dtor_C, (_29_dt__update__tmp_h11).dtor_D, (_29_dt__update__tmp_h11).dtor_E, (_29_dt__update__tmp_h11).dtor_KR, (_29_dt__update__tmp_h11).dtor_SR, (_29_dt__update__tmp_h11).dtor_fA, (_29_dt__update__tmp_h11).dtor_fB, (_29_dt__update__tmp_h11).dtor_R5, (_29_dt__update__tmp_h11).dtor_Sout, (_29_dt__update__tmp_h11).dtor_Sin, (_29_dt__update__tmp_h11).dtor_opcode, _30_dt__update_hflags_h3, (_29_dt__update__tmp_h11).dtor_addr, (_29_dt__update__tmp_h11).dtor_reset, (_29_dt__update__tmp_h11).dtor_zeroSuppr)))))) : ((((_0_f) == (new BigInteger(10))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let60_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let60_0, _31_dt__update__tmp_h12 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(0), _pat_let61_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let61_0, _32_dt__update_hfB_h0 => TI59__Cpu.Cpu.create((_31_dt__update__tmp_h12).dtor_A, (_31_dt__update__tmp_h12).dtor_B, (_31_dt__update__tmp_h12).dtor_C, (_31_dt__update__tmp_h12).dtor_D, (_31_dt__update__tmp_h12).dtor_E, (_31_dt__update__tmp_h12).dtor_KR, (_31_dt__update__tmp_h12).dtor_SR, (_31_dt__update__tmp_h12).dtor_fA, _32_dt__update_hfB_h0, (_31_dt__update__tmp_h12).dtor_R5, (_31_dt__update__tmp_h12).dtor_Sout, (_31_dt__update__tmp_h12).dtor_Sin, (_31_dt__update__tmp_h12).dtor_opcode, (_31_dt__update__tmp_h12).dtor_flags, (_31_dt__update__tmp_h12).dtor_addr, (_31_dt__update__tmp_h12).dtor_reset, (_31_dt__update__tmp_h12).dtor_zeroSuppr)))))) : ((((_0_f) == (new BigInteger(11))) ? ((((((byte)((keyLine) & (TI59__Nibbles.__default.Bit8(new BigInteger(4))))) != ((byte)(0))) || (TI59__Cpu.__default.Flg((_2_c0).dtor_flags, TI59__Nibbles.__default.FLG__BUSY))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let62_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let62_0, _33_dt__update__tmp_h13 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_2_c0).dtor_flags) & ((ushort)(((ushort)(65535)) ^ ((ushort)((TI59__Nibbles.__default.FLG__COND) | (TI59__Nibbles.__default.FLG__BUSY)))))), _pat_let63_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let63_0, _34_dt__update_hflags_h4 => TI59__Cpu.Cpu.create((_33_dt__update__tmp_h13).dtor_A, (_33_dt__update__tmp_h13).dtor_B, (_33_dt__update__tmp_h13).dtor_C, (_33_dt__update__tmp_h13).dtor_D, (_33_dt__update__tmp_h13).dtor_E, (_33_dt__update__tmp_h13).dtor_KR, (_33_dt__update__tmp_h13).dtor_SR, (_33_dt__update__tmp_h13).dtor_fA, (_33_dt__update__tmp_h13).dtor_fB, (_33_dt__update__tmp_h13).dtor_R5, (_33_dt__update__tmp_h13).dtor_Sout, (_33_dt__update__tmp_h13).dtor_Sin, (_33_dt__update__tmp_h13).dtor_opcode, _34_dt__update_hflags_h4, (_33_dt__update__tmp_h13).dtor_addr, (_33_dt__update__tmp_h13).dtor_reset, (_33_dt__update__tmp_h13).dtor_zeroSuppr)))))) : (_2_c0))) : ((((_0_f) == (new BigInteger(12))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let64_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let64_0, _35_dt__update__tmp_h14 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((ushort)((unchecked((ushort)(((ushort)((_pat_let_tv1) << ((int)((byte)(1)))))))) & ((ushort)(65520)))) | ((ushort)((_pat_let_tv2) >> ((int)((byte)(15)))))), _pat_let65_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let65_0, _36_dt__update_hKR_h3 => TI59__Cpu.Cpu.create((_35_dt__update__tmp_h14).dtor_A, (_35_dt__update__tmp_h14).dtor_B, (_35_dt__update__tmp_h14).dtor_C, (_35_dt__update__tmp_h14).dtor_D, (_35_dt__update__tmp_h14).dtor_E, _36_dt__update_hKR_h3, (_35_dt__update__tmp_h14).dtor_SR, (_35_dt__update__tmp_h14).dtor_fA, (_35_dt__update__tmp_h14).dtor_fB, (_35_dt__update__tmp_h14).dtor_R5, (_35_dt__update__tmp_h14).dtor_Sout, (_35_dt__update__tmp_h14).dtor_Sin, (_35_dt__update__tmp_h14).dtor_opcode, (_35_dt__update__tmp_h14).dtor_flags, (_35_dt__update__tmp_h14).dtor_addr, (_35_dt__update__tmp_h14).dtor_reset, (_35_dt__update__tmp_h14).dtor_zeroSuppr)))))) : ((((_0_f) == (new BigInteger(13))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c0, _pat_let66_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let66_0, _37_dt__update__tmp_h15 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((_2_c0).dtor_KR, _pat_let67_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let67_0, _38_dt__update_hSR_h0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((_2_c0).dtor_SR, _pat_let68_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let68_0, _39_dt__update_hKR_h4 => TI59__Cpu.Cpu.create((_37_dt__update__tmp_h15).dtor_A, (_37_dt__update__tmp_h15).dtor_B, (_37_dt__update__tmp_h15).dtor_C, (_37_dt__update__tmp_h15).dtor_D, (_37_dt__update__tmp_h15).dtor_E, _39_dt__update_hKR_h4, _38_dt__update_hSR_h0, (_37_dt__update__tmp_h15).dtor_fA, (_37_dt__update__tmp_h15).dtor_fB, (_37_dt__update__tmp_h15).dtor_R5, (_37_dt__update__tmp_h15).dtor_Sout, (_37_dt__update__tmp_h15).dtor_Sin, (_37_dt__update__tmp_h15).dtor_opcode, (_37_dt__update__tmp_h15).dtor_flags, (_37_dt__update__tmp_h15).dtor_addr, (_37_dt__update__tmp_h15).dtor_reset, (_37_dt__update__tmp_h15).dtor_zeroSuppr)))))))) : (_2_c0))))))))))))))))))))))))))));
      return _3_c1;
    }
    public static Dafny.ISequence<byte> RegSpecial(TI59__Nibbles._IMask m, Dafny.ISequence<byte> dst, byte r5)
    {
      return TI59__Cpu.__default.RegSpecialFrom((m).dtor_start, dst, m, r5);
    }
    public static Dafny.ISequence<byte> RegSpecialFrom(BigInteger i, Dafny.ISequence<byte> dst, TI59__Nibbles._IMask m, byte r5)
    {
    TAIL_CALL_START: ;
      if ((i) > ((m).dtor_end)) {
        return dst;
      } else {
        byte _0_d = (((i) == ((m).dtor_start)) ? (r5) : ((((i) == ((m).dtor_cpos)) ? ((m).dtor_cval) : ((byte)(0)))));
        BigInteger _in0 = (i) + (BigInteger.One);
        Dafny.ISequence<byte> _in1 = Dafny.Sequence<byte>.Update(dst, i, _0_d);
        TI59__Nibbles._IMask _in2 = m;
        byte _in3 = r5;
        i = _in0;
        dst = _in1;
        m = _in2;
        r5 = _in3;
        goto TAIL_CALL_START;
      }
    }
    public static TI59__Cpu._ICpu ExecAlu(TI59__Cpu._ICpu c, ushort opcode)
    {
      var _pat_let_tv0 = opcode;
      var _pat_let_tv1 = c;
      var _pat_let_tv2 = c;
      var _pat_let_tv3 = c;
      BigInteger _0_mIdx = new BigInteger((ushort)(((ushort)((opcode) >> ((int)((byte)(8))))) & ((ushort)(15))));
      TI59__Nibbles._IMask _1_m = TI59__Nibbles.__default.MaskOf(_0_mIdx);
      TI59__Cpu._IAluInp _2_inp = (TI59__Cpu.__default.ALU__OPS).Select(new BigInteger((ushort)(((ushort)((opcode) >> ((int)((byte)(3))))) & ((ushort)(31)))));
      BigInteger _3_dsIdx = new BigInteger((ushort)((opcode) & ((ushort)(7))));
      TI59__Cpu._IDstSel _4_dsel = (TI59__Cpu.__default.ALU__DSTS).Select(_3_dsIdx);
      bool _5_ioValid = TI59__Cpu.__default.Flg((c).dtor_flags, TI59__Nibbles.__default.FLG__IO__VALID);
      BigInteger _6_lowF8 = new BigInteger((ushort)((opcode) & ((ushort)(248))));
      TI59__AluOps._IAluRes _7_res = ((((_6_lowF8) == (new BigInteger(240))) || ((_6_lowF8) == (new BigInteger(248)))) ? (((TI59__Cpu.__default.DstHasReg(_4_dsel)) ? (Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__AluOps._IAluRes>(TI59__Cpu.__default.RegSpecial(_1_m, TI59__Cpu.__default.DstRegOrZero(c, _4_dsel), (c).dtor_R5), _pat_let69_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__AluOps._IAluRes>(_pat_let69_0, _8_dst0 => TI59__AluOps.__default.AluExec(true, _8_dst0, false, TI59__Nibbles.__default.ZeroReg(), true, _8_dst0, _1_m, ((((ushort)((_pat_let_tv0) & ((ushort)(8)))) == ((ushort)(0))) ? (TI59__AluOps.AluOp.create_OP__ADD()) : (TI59__AluOps.AluOp.create_OP__SUB())), (_pat_let_tv1).dtor_Sin, _5_ioValid, (_pat_let_tv2).dtor_R5, (_pat_let_tv3).dtor_Sout)))) : (TI59__AluOps.__default.AluExec(false, TI59__Nibbles.__default.ZeroReg(), (_2_inp).dtor_hasX, TI59__Cpu.__default.SelReg(c, (_2_inp).dtor_x), (_2_inp).dtor_hasY, TI59__Cpu.__default.SelReg(c, (_2_inp).dtor_y), _1_m, (_2_inp).dtor_op, (c).dtor_Sin, _5_ioValid, (c).dtor_R5, (c).dtor_Sout)))) : (TI59__AluOps.__default.AluExec(TI59__Cpu.__default.DstHasReg(_4_dsel), TI59__Cpu.__default.DstRegOrZero(c, _4_dsel), (_2_inp).dtor_hasX, TI59__Cpu.__default.SelReg(c, (_2_inp).dtor_x), (_2_inp).dtor_hasY, TI59__Cpu.__default.SelReg(c, (_2_inp).dtor_y), _1_m, (_2_inp).dtor_op, (c).dtor_Sin, _5_ioValid, (c).dtor_R5, (c).dtor_Sout)));
      TI59__Cpu._ICpu _9_c1 = Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(c, _pat_let70_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let70_0, _10_dt__update__tmp_h0 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>((_7_res).dtor_R5, _pat_let71_0 => Dafny.Helpers.Let<byte, TI59__Cpu._ICpu>(_pat_let71_0, _11_dt__update_hR5_h0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>((_7_res).dtor_Sout, _pat_let72_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let72_0, _12_dt__update_hSout_h0 => TI59__Cpu.Cpu.create((_10_dt__update__tmp_h0).dtor_A, (_10_dt__update__tmp_h0).dtor_B, (_10_dt__update__tmp_h0).dtor_C, (_10_dt__update__tmp_h0).dtor_D, (_10_dt__update__tmp_h0).dtor_E, (_10_dt__update__tmp_h0).dtor_KR, (_10_dt__update__tmp_h0).dtor_SR, (_10_dt__update__tmp_h0).dtor_fA, (_10_dt__update__tmp_h0).dtor_fB, _11_dt__update_hR5_h0, _12_dt__update_hSout_h0, (_10_dt__update__tmp_h0).dtor_Sin, (_10_dt__update__tmp_h0).dtor_opcode, (_10_dt__update__tmp_h0).dtor_flags, (_10_dt__update__tmp_h0).dtor_addr, (_10_dt__update__tmp_h0).dtor_reset, (_10_dt__update__tmp_h0).dtor_zeroSuppr)))))));
      TI59__Cpu._ICpu _13_c2 = ((TI59__Cpu.__default.DstHasReg(_4_dsel)) ? (TI59__Cpu.__default.SetDstReg(_9_c1, _4_dsel, (_7_res).dtor_dst)) : (_9_c1));
      TI59__Cpu._ICpu _14_c3 = (((_7_res).dtor_condClear) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_13_c2, _pat_let73_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let73_0, _15_dt__update__tmp_h1 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_13_c2).dtor_flags) & ((ushort)(((ushort)(65535)) ^ (TI59__Nibbles.__default.FLG__COND)))), _pat_let74_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let74_0, _16_dt__update_hflags_h0 => TI59__Cpu.Cpu.create((_15_dt__update__tmp_h1).dtor_A, (_15_dt__update__tmp_h1).dtor_B, (_15_dt__update__tmp_h1).dtor_C, (_15_dt__update__tmp_h1).dtor_D, (_15_dt__update__tmp_h1).dtor_E, (_15_dt__update__tmp_h1).dtor_KR, (_15_dt__update__tmp_h1).dtor_SR, (_15_dt__update__tmp_h1).dtor_fA, (_15_dt__update__tmp_h1).dtor_fB, (_15_dt__update__tmp_h1).dtor_R5, (_15_dt__update__tmp_h1).dtor_Sout, (_15_dt__update__tmp_h1).dtor_Sin, (_15_dt__update__tmp_h1).dtor_opcode, _16_dt__update_hflags_h0, (_15_dt__update__tmp_h1).dtor_addr, (_15_dt__update__tmp_h1).dtor_reset, (_15_dt__update__tmp_h1).dtor_zeroSuppr)))))) : (_13_c2));
      TI59__Cpu._ICpu _17_c4 = (((_3_dsIdx) == (new BigInteger(2))) ? (Dafny.Helpers.Let<_System._ITuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>>, TI59__Cpu._ICpu>(TI59__AluOps.__default.XchExec((_14_c3).dtor_A, (_14_c3).dtor_B, _1_m), _pat_let75_0 => Dafny.Helpers.Let<_System._ITuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>>, TI59__Cpu._ICpu>(_pat_let75_0, _18_p => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_14_c3, _pat_let76_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let76_0, _19_dt__update__tmp_h2 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>((_18_p).dtor__1, _pat_let77_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let77_0, _20_dt__update_hB_h0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>((_18_p).dtor__0, _pat_let78_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let78_0, _21_dt__update_hA_h0 => TI59__Cpu.Cpu.create(_21_dt__update_hA_h0, _20_dt__update_hB_h0, (_19_dt__update__tmp_h2).dtor_C, (_19_dt__update__tmp_h2).dtor_D, (_19_dt__update__tmp_h2).dtor_E, (_19_dt__update__tmp_h2).dtor_KR, (_19_dt__update__tmp_h2).dtor_SR, (_19_dt__update__tmp_h2).dtor_fA, (_19_dt__update__tmp_h2).dtor_fB, (_19_dt__update__tmp_h2).dtor_R5, (_19_dt__update__tmp_h2).dtor_Sout, (_19_dt__update__tmp_h2).dtor_Sin, (_19_dt__update__tmp_h2).dtor_opcode, (_19_dt__update__tmp_h2).dtor_flags, (_19_dt__update__tmp_h2).dtor_addr, (_19_dt__update__tmp_h2).dtor_reset, (_19_dt__update__tmp_h2).dtor_zeroSuppr)))))))))) : ((((_3_dsIdx) == (new BigInteger(5))) ? (Dafny.Helpers.Let<_System._ITuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>>, TI59__Cpu._ICpu>(TI59__AluOps.__default.XchExec((_14_c3).dtor_C, (_14_c3).dtor_D, _1_m), _pat_let79_0 => Dafny.Helpers.Let<_System._ITuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>>, TI59__Cpu._ICpu>(_pat_let79_0, _22_p => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_14_c3, _pat_let80_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let80_0, _23_dt__update__tmp_h3 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>((_22_p).dtor__1, _pat_let81_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let81_0, _24_dt__update_hD_h0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>((_22_p).dtor__0, _pat_let82_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let82_0, _25_dt__update_hC_h0 => TI59__Cpu.Cpu.create((_23_dt__update__tmp_h3).dtor_A, (_23_dt__update__tmp_h3).dtor_B, _25_dt__update_hC_h0, _24_dt__update_hD_h0, (_23_dt__update__tmp_h3).dtor_E, (_23_dt__update__tmp_h3).dtor_KR, (_23_dt__update__tmp_h3).dtor_SR, (_23_dt__update__tmp_h3).dtor_fA, (_23_dt__update__tmp_h3).dtor_fB, (_23_dt__update__tmp_h3).dtor_R5, (_23_dt__update__tmp_h3).dtor_Sout, (_23_dt__update__tmp_h3).dtor_Sin, (_23_dt__update__tmp_h3).dtor_opcode, (_23_dt__update__tmp_h3).dtor_flags, (_23_dt__update__tmp_h3).dtor_addr, (_23_dt__update__tmp_h3).dtor_reset, (_23_dt__update__tmp_h3).dtor_zeroSuppr)))))))))) : ((((_3_dsIdx) == (new BigInteger(7))) ? (Dafny.Helpers.Let<_System._ITuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>>, TI59__Cpu._ICpu>(TI59__AluOps.__default.XchExec((_14_c3).dtor_A, (_14_c3).dtor_E, _1_m), _pat_let83_0 => Dafny.Helpers.Let<_System._ITuple2<Dafny.ISequence<byte>, Dafny.ISequence<byte>>, TI59__Cpu._ICpu>(_pat_let83_0, _26_p => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_14_c3, _pat_let84_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let84_0, _27_dt__update__tmp_h4 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>((_26_p).dtor__1, _pat_let85_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let85_0, _28_dt__update_hE_h0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>((_26_p).dtor__0, _pat_let86_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let86_0, _29_dt__update_hA_h1 => TI59__Cpu.Cpu.create(_29_dt__update_hA_h1, (_27_dt__update__tmp_h4).dtor_B, (_27_dt__update__tmp_h4).dtor_C, (_27_dt__update__tmp_h4).dtor_D, _28_dt__update_hE_h0, (_27_dt__update__tmp_h4).dtor_KR, (_27_dt__update__tmp_h4).dtor_SR, (_27_dt__update__tmp_h4).dtor_fA, (_27_dt__update__tmp_h4).dtor_fB, (_27_dt__update__tmp_h4).dtor_R5, (_27_dt__update__tmp_h4).dtor_Sout, (_27_dt__update__tmp_h4).dtor_Sin, (_27_dt__update__tmp_h4).dtor_opcode, (_27_dt__update__tmp_h4).dtor_flags, (_27_dt__update__tmp_h4).dtor_addr, (_27_dt__update__tmp_h4).dtor_reset, (_27_dt__update__tmp_h4).dtor_zeroSuppr)))))))))) : (_14_c3))))));
      return _17_c4;
    }
    public static Dafny.ISequence<byte> DstRegOrZero(TI59__Cpu._ICpu c, TI59__Cpu._IDstSel d)
    {
      if (TI59__Cpu.__default.DstHasReg(d)) {
        return TI59__Cpu.__default.DstReg(c, d);
      } else {
        return TI59__Nibbles.__default.ZeroReg();
      }
    }
    public static TI59__Cpu._ICpu Execute(TI59__Cpu._ICpu c, ushort opcode, BigInteger digit, ushort ext, byte keyLine)
    {
      var _pat_let_tv0 = c;
      if (((ushort)((opcode) & ((ushort)(4096)))) != ((ushort)(0))) {
        TI59__Cpu._ICpu _0_dt__update__tmp_h0 = c;
        ushort _1_dt__update_hflags_h0 = (ushort)(((c).dtor_flags) | (TI59__Nibbles.__default.FLG__JUMP));
        return TI59__Cpu.Cpu.create((_0_dt__update__tmp_h0).dtor_A, (_0_dt__update__tmp_h0).dtor_B, (_0_dt__update__tmp_h0).dtor_C, (_0_dt__update__tmp_h0).dtor_D, (_0_dt__update__tmp_h0).dtor_E, (_0_dt__update__tmp_h0).dtor_KR, (_0_dt__update__tmp_h0).dtor_SR, (_0_dt__update__tmp_h0).dtor_fA, (_0_dt__update__tmp_h0).dtor_fB, (_0_dt__update__tmp_h0).dtor_R5, (_0_dt__update__tmp_h0).dtor_Sout, (_0_dt__update__tmp_h0).dtor_Sin, (_0_dt__update__tmp_h0).dtor_opcode, _1_dt__update_hflags_h0, (_0_dt__update__tmp_h0).dtor_addr, (_0_dt__update__tmp_h0).dtor_reset, (_0_dt__update__tmp_h0).dtor_zeroSuppr);
      } else {
        TI59__Cpu._ICpu _2_c0 = ((TI59__Cpu.__default.Flg((c).dtor_flags, TI59__Nibbles.__default.FLG__JUMP)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(c, _pat_let87_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let87_0, _3_dt__update__tmp_h1 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((ushort)(((_pat_let_tv0).dtor_flags) & ((ushort)(((ushort)(65535)) ^ (TI59__Nibbles.__default.FLG__JUMP))))) | (TI59__Nibbles.__default.FLG__COND)), _pat_let88_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let88_0, _4_dt__update_hflags_h1 => TI59__Cpu.Cpu.create((_3_dt__update__tmp_h1).dtor_A, (_3_dt__update__tmp_h1).dtor_B, (_3_dt__update__tmp_h1).dtor_C, (_3_dt__update__tmp_h1).dtor_D, (_3_dt__update__tmp_h1).dtor_E, (_3_dt__update__tmp_h1).dtor_KR, (_3_dt__update__tmp_h1).dtor_SR, (_3_dt__update__tmp_h1).dtor_fA, (_3_dt__update__tmp_h1).dtor_fB, (_3_dt__update__tmp_h1).dtor_R5, (_3_dt__update__tmp_h1).dtor_Sout, (_3_dt__update__tmp_h1).dtor_Sin, (_3_dt__update__tmp_h1).dtor_opcode, _4_dt__update_hflags_h1, (_3_dt__update__tmp_h1).dtor_addr, (_3_dt__update__tmp_h1).dtor_reset, (_3_dt__update__tmp_h1).dtor_zeroSuppr)))))) : (c));
        BigInteger _5_g = new BigInteger((ushort)((opcode) & ((ushort)(3840))));
        if ((_5_g).Sign == 0) {
          return TI59__Cpu.__default.ExecFlagOps(_2_c0, opcode);
        } else if ((_5_g) == (new BigInteger(2048))) {
          return TI59__Cpu.__default.ExecKeyb(_2_c0, opcode, digit, keyLine);
        } else if ((_5_g) == (new BigInteger(2560))) {
          return TI59__Cpu.__default.ExecWait(_2_c0, opcode, digit, ext, keyLine);
        } else {
          return TI59__Cpu.__default.ExecAlu(_2_c0, opcode);
        }
      }
    }
    public static _System._ITuple4<Dafny.Rune, bool, bool, bool> GenDigitIdle(TI59__Cpu._ICpu c, BigInteger dstate)
    {
      BigInteger _0_i = dstate;
      bool _1_zs1 = (((_0_i) == (new BigInteger(15))) ? (true) : ((c).dtor_zeroSuppr));
      bool _2_zs2 = (((((_0_i) == (new BigInteger(3))) || ((((c).dtor_R5) == ((byte)(_0_i))) && ((_0_i) != (new BigInteger(15))))) || ((new BigInteger(((c).dtor_B).Select(_0_i))) >= (new BigInteger(8)))) ? (false) : (_1_zs1));
      bool _3_zs3 = (((_0_i) == (new BigInteger(2))) ? (true) : (_2_zs2));
      Dafny.Rune _4_ch = (((((((c).dtor_B).Select(_0_i)) == ((byte)(7))) || ((((c).dtor_B).Select(_0_i)) == ((byte)(3)))) || ((((new BigInteger(((c).dtor_B).Select(_0_i))) <= (new BigInteger(4))) && (_3_zs3)) && ((((c).dtor_A).Select(_0_i)) == ((byte)(0))))) ? (new Dafny.Rune(' ')) : (((((((c).dtor_B).Select(_0_i)) == ((byte)(6))) || (((((c).dtor_B).Select(_0_i)) == ((byte)(5))) && ((((c).dtor_A).Select(_0_i)) == ((byte)(0))))) ? (new Dafny.Rune('-')) : ((((((c).dtor_B).Select(_0_i)) == ((byte)(5))) ? (new Dafny.Rune('o')) : ((((((c).dtor_B).Select(_0_i)) == ((byte)(4))) ? (new Dafny.Rune('\'')) : ((((((c).dtor_B).Select(new BigInteger(3))) == ((byte)(2))) ? (new Dafny.Rune('"')) : (new Dafny.Rune((int)((new BigInteger((new Dafny.Rune('0')).Value)) + (new BigInteger(((c).dtor_A).Select(_0_i)))))))))))))));
      bool _5_zs4 = (((!(((((((((((c).dtor_B).Select(_0_i)) == ((byte)(7))) || ((((c).dtor_B).Select(_0_i)) == ((byte)(3)))) || ((((new BigInteger(((c).dtor_B).Select(_0_i))) <= (new BigInteger(4))) && (_3_zs3)) && ((((c).dtor_A).Select(_0_i)) == ((byte)(0))))) || ((((c).dtor_B).Select(_0_i)) == ((byte)(6)))) || (((((c).dtor_B).Select(_0_i)) == ((byte)(5))) && ((((c).dtor_A).Select(_0_i)) == ((byte)(0))))) || ((((c).dtor_B).Select(_0_i)) == ((byte)(5)))) || ((((c).dtor_B).Select(_0_i)) == ((byte)(4)))) || ((((c).dtor_B).Select(new BigInteger(3))) == ((byte)(2))))) && ((((c).dtor_A).Select(_0_i)) != ((byte)(0)))) ? (false) : (_3_zs3));
      bool _6_dpt = ((c).dtor_R5) == ((byte)(_0_i));
      bool _7_segH = (((_0_i) < (new BigInteger(15))) ? (TI59__Cpu.__default.Flg((c).dtor_fA, TI59__Nibbles.__default.Bit16((_0_i) + (BigInteger.One)))) : (false));
      return _System.Tuple4<Dafny.Rune, bool, bool, bool>.create(_4_ch, _6_dpt, _7_segH, _5_zs4);
    }
    public static Dafny.ISequence<TI59__Cpu._IAluInp> ALU__OPS { get {
      return Dafny.Sequence<TI59__Cpu._IAluInp>.FromElements(TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), true, TI59__Cpu.RegSel.create_RS__B(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), true, TI59__Cpu.RegSel.create_RS__B(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), true, TI59__Cpu.RegSel.create_RS__D(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), true, TI59__Cpu.RegSel.create_RS__D(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SHL()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SHR()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), true, TI59__Cpu.RegSel.create_RS__B(), TI59__AluOps.AluOp.create_OP__SHL()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), true, TI59__Cpu.RegSel.create_RS__B(), TI59__AluOps.AluOp.create_OP__SHR()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SHL()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SHR()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), true, TI59__Cpu.RegSel.create_RS__D(), TI59__AluOps.AluOp.create_OP__SHL()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), true, TI59__Cpu.RegSel.create_RS__D(), TI59__AluOps.AluOp.create_OP__SHR()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), true, TI59__Cpu.RegSel.create_RS__B(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), true, TI59__Cpu.RegSel.create_RS__B(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), true, TI59__Cpu.RegSel.create_RS__B(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), true, TI59__Cpu.RegSel.create_RS__B(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), true, TI59__Cpu.RegSel.create_RS__D(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), true, TI59__Cpu.RegSel.create_RS__D(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), true, TI59__Cpu.RegSel.create_RS__D(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), true, TI59__Cpu.RegSel.create_RS__D(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__A(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(true, TI59__Cpu.RegSel.create_RS__C(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SUB()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__ADD()), TI59__Cpu.AluInp.create(false, TI59__Cpu.RegSel.create_RS__NULL(), false, TI59__Cpu.RegSel.create_RS__NULL(), TI59__AluOps.AluOp.create_OP__SUB()));
    } }
    public static Dafny.ISequence<TI59__Cpu._IDstSel> ALU__DSTS { get {
      return Dafny.Sequence<TI59__Cpu._IDstSel>.FromElements(TI59__Cpu.DstSel.create_DS__A(), TI59__Cpu.DstSel.create_DS__IO(), TI59__Cpu.DstSel.create_DS__XCH__AB(), TI59__Cpu.DstSel.create_DS__B(), TI59__Cpu.DstSel.create_DS__C(), TI59__Cpu.DstSel.create_DS__XCH__CD(), TI59__Cpu.DstSel.create_DS__D(), TI59__Cpu.DstSel.create_DS__XCH__AE());
    } }
  }

  public interface _ICpu {
    bool is_Cpu { get; }
    Dafny.ISequence<byte> dtor_A { get; }
    Dafny.ISequence<byte> dtor_B { get; }
    Dafny.ISequence<byte> dtor_C { get; }
    Dafny.ISequence<byte> dtor_D { get; }
    Dafny.ISequence<byte> dtor_E { get; }
    ushort dtor_KR { get; }
    ushort dtor_SR { get; }
    ushort dtor_fA { get; }
    ushort dtor_fB { get; }
    byte dtor_R5 { get; }
    Dafny.ISequence<byte> dtor_Sout { get; }
    Dafny.ISequence<byte> dtor_Sin { get; }
    ushort dtor_opcode { get; }
    ushort dtor_flags { get; }
    BigInteger dtor_addr { get; }
    BigInteger dtor_reset { get; }
    bool dtor_zeroSuppr { get; }
    _ICpu DowncastClone();
  }
  public class Cpu : _ICpu {
    public readonly Dafny.ISequence<byte> _A;
    public readonly Dafny.ISequence<byte> _B;
    public readonly Dafny.ISequence<byte> _C;
    public readonly Dafny.ISequence<byte> _D;
    public readonly Dafny.ISequence<byte> _E;
    public readonly ushort _KR;
    public readonly ushort _SR;
    public readonly ushort _fA;
    public readonly ushort _fB;
    public readonly byte _R5;
    public readonly Dafny.ISequence<byte> _Sout;
    public readonly Dafny.ISequence<byte> _Sin;
    public readonly ushort _opcode;
    public readonly ushort _flags;
    public readonly BigInteger _addr;
    public readonly BigInteger _reset;
    public readonly bool _zeroSuppr;
    public Cpu(Dafny.ISequence<byte> A, Dafny.ISequence<byte> B, Dafny.ISequence<byte> C, Dafny.ISequence<byte> D, Dafny.ISequence<byte> E, ushort KR, ushort SR, ushort fA, ushort fB, byte R5, Dafny.ISequence<byte> Sout, Dafny.ISequence<byte> Sin, ushort opcode, ushort flags, BigInteger addr, BigInteger reset, bool zeroSuppr) {
      this._A = A;
      this._B = B;
      this._C = C;
      this._D = D;
      this._E = E;
      this._KR = KR;
      this._SR = SR;
      this._fA = fA;
      this._fB = fB;
      this._R5 = R5;
      this._Sout = Sout;
      this._Sin = Sin;
      this._opcode = opcode;
      this._flags = flags;
      this._addr = addr;
      this._reset = reset;
      this._zeroSuppr = zeroSuppr;
    }
    public _ICpu DowncastClone() {
      if (this is _ICpu dt) { return dt; }
      return new Cpu(_A, _B, _C, _D, _E, _KR, _SR, _fA, _fB, _R5, _Sout, _Sin, _opcode, _flags, _addr, _reset, _zeroSuppr);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.Cpu;
      return oth != null && object.Equals(this._A, oth._A) && object.Equals(this._B, oth._B) && object.Equals(this._C, oth._C) && object.Equals(this._D, oth._D) && object.Equals(this._E, oth._E) && this._KR == oth._KR && this._SR == oth._SR && this._fA == oth._fA && this._fB == oth._fB && this._R5 == oth._R5 && object.Equals(this._Sout, oth._Sout) && object.Equals(this._Sin, oth._Sin) && this._opcode == oth._opcode && this._flags == oth._flags && this._addr == oth._addr && this._reset == oth._reset && this._zeroSuppr == oth._zeroSuppr;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._A));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._B));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._C));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._D));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._E));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._KR));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._SR));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._fA));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._fB));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._R5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._Sout));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._Sin));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._opcode));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._flags));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._addr));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._reset));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._zeroSuppr));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.Cpu.Cpu";
      s += "(";
      s += Dafny.Helpers.ToString(this._A);
      s += ", ";
      s += Dafny.Helpers.ToString(this._B);
      s += ", ";
      s += Dafny.Helpers.ToString(this._C);
      s += ", ";
      s += Dafny.Helpers.ToString(this._D);
      s += ", ";
      s += Dafny.Helpers.ToString(this._E);
      s += ", ";
      s += Dafny.Helpers.ToString(this._KR);
      s += ", ";
      s += Dafny.Helpers.ToString(this._SR);
      s += ", ";
      s += Dafny.Helpers.ToString(this._fA);
      s += ", ";
      s += Dafny.Helpers.ToString(this._fB);
      s += ", ";
      s += Dafny.Helpers.ToString(this._R5);
      s += ", ";
      s += Dafny.Helpers.ToString(this._Sout);
      s += ", ";
      s += Dafny.Helpers.ToString(this._Sin);
      s += ", ";
      s += Dafny.Helpers.ToString(this._opcode);
      s += ", ";
      s += Dafny.Helpers.ToString(this._flags);
      s += ", ";
      s += Dafny.Helpers.ToString(this._addr);
      s += ", ";
      s += Dafny.Helpers.ToString(this._reset);
      s += ", ";
      s += Dafny.Helpers.ToString(this._zeroSuppr);
      s += ")";
      return s;
    }
    private static readonly TI59__Cpu._ICpu theDefault = create(TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), 0, 0, 0, 0, 0, TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), 0, 0, BigInteger.Zero, BigInteger.Zero, false);
    public static TI59__Cpu._ICpu Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Cpu._ICpu> _TYPE = new Dafny.TypeDescriptor<TI59__Cpu._ICpu>(TI59__Cpu.Cpu.Default());
    public static Dafny.TypeDescriptor<TI59__Cpu._ICpu> _TypeDescriptor() {
      return _TYPE;
    }
    public static _ICpu create(Dafny.ISequence<byte> A, Dafny.ISequence<byte> B, Dafny.ISequence<byte> C, Dafny.ISequence<byte> D, Dafny.ISequence<byte> E, ushort KR, ushort SR, ushort fA, ushort fB, byte R5, Dafny.ISequence<byte> Sout, Dafny.ISequence<byte> Sin, ushort opcode, ushort flags, BigInteger addr, BigInteger reset, bool zeroSuppr) {
      return new Cpu(A, B, C, D, E, KR, SR, fA, fB, R5, Sout, Sin, opcode, flags, addr, reset, zeroSuppr);
    }
    public static _ICpu create_Cpu(Dafny.ISequence<byte> A, Dafny.ISequence<byte> B, Dafny.ISequence<byte> C, Dafny.ISequence<byte> D, Dafny.ISequence<byte> E, ushort KR, ushort SR, ushort fA, ushort fB, byte R5, Dafny.ISequence<byte> Sout, Dafny.ISequence<byte> Sin, ushort opcode, ushort flags, BigInteger addr, BigInteger reset, bool zeroSuppr) {
      return create(A, B, C, D, E, KR, SR, fA, fB, R5, Sout, Sin, opcode, flags, addr, reset, zeroSuppr);
    }
    public bool is_Cpu { get { return true; } }
    public Dafny.ISequence<byte> dtor_A {
      get {
        return this._A;
      }
    }
    public Dafny.ISequence<byte> dtor_B {
      get {
        return this._B;
      }
    }
    public Dafny.ISequence<byte> dtor_C {
      get {
        return this._C;
      }
    }
    public Dafny.ISequence<byte> dtor_D {
      get {
        return this._D;
      }
    }
    public Dafny.ISequence<byte> dtor_E {
      get {
        return this._E;
      }
    }
    public ushort dtor_KR {
      get {
        return this._KR;
      }
    }
    public ushort dtor_SR {
      get {
        return this._SR;
      }
    }
    public ushort dtor_fA {
      get {
        return this._fA;
      }
    }
    public ushort dtor_fB {
      get {
        return this._fB;
      }
    }
    public byte dtor_R5 {
      get {
        return this._R5;
      }
    }
    public Dafny.ISequence<byte> dtor_Sout {
      get {
        return this._Sout;
      }
    }
    public Dafny.ISequence<byte> dtor_Sin {
      get {
        return this._Sin;
      }
    }
    public ushort dtor_opcode {
      get {
        return this._opcode;
      }
    }
    public ushort dtor_flags {
      get {
        return this._flags;
      }
    }
    public BigInteger dtor_addr {
      get {
        return this._addr;
      }
    }
    public BigInteger dtor_reset {
      get {
        return this._reset;
      }
    }
    public bool dtor_zeroSuppr {
      get {
        return this._zeroSuppr;
      }
    }
  }

  public interface _IRegSel {
    bool is_RS__NULL { get; }
    bool is_RS__A { get; }
    bool is_RS__B { get; }
    bool is_RS__C { get; }
    bool is_RS__D { get; }
    _IRegSel DowncastClone();
  }
  public abstract class RegSel : _IRegSel {
    public RegSel() {
    }
    private static readonly TI59__Cpu._IRegSel theDefault = create_RS__NULL();
    public static TI59__Cpu._IRegSel Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Cpu._IRegSel> _TYPE = new Dafny.TypeDescriptor<TI59__Cpu._IRegSel>(TI59__Cpu.RegSel.Default());
    public static Dafny.TypeDescriptor<TI59__Cpu._IRegSel> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IRegSel create_RS__NULL() {
      return new RegSel_RS__NULL();
    }
    public static _IRegSel create_RS__A() {
      return new RegSel_RS__A();
    }
    public static _IRegSel create_RS__B() {
      return new RegSel_RS__B();
    }
    public static _IRegSel create_RS__C() {
      return new RegSel_RS__C();
    }
    public static _IRegSel create_RS__D() {
      return new RegSel_RS__D();
    }
    public bool is_RS__NULL { get { return this is RegSel_RS__NULL; } }
    public bool is_RS__A { get { return this is RegSel_RS__A; } }
    public bool is_RS__B { get { return this is RegSel_RS__B; } }
    public bool is_RS__C { get { return this is RegSel_RS__C; } }
    public bool is_RS__D { get { return this is RegSel_RS__D; } }
    public static System.Collections.Generic.IEnumerable<_IRegSel> AllSingletonConstructors {
      get {
        yield return RegSel.create_RS__NULL();
        yield return RegSel.create_RS__A();
        yield return RegSel.create_RS__B();
        yield return RegSel.create_RS__C();
        yield return RegSel.create_RS__D();
      }
    }
    public abstract _IRegSel DowncastClone();
  }
  public class RegSel_RS__NULL : RegSel {
    public RegSel_RS__NULL() : base() {
    }
    public override _IRegSel DowncastClone() {
      if (this is _IRegSel dt) { return dt; }
      return new RegSel_RS__NULL();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.RegSel_RS__NULL;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.RegSel.RS_NULL";
      return s;
    }
  }
  public class RegSel_RS__A : RegSel {
    public RegSel_RS__A() : base() {
    }
    public override _IRegSel DowncastClone() {
      if (this is _IRegSel dt) { return dt; }
      return new RegSel_RS__A();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.RegSel_RS__A;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 1;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.RegSel.RS_A";
      return s;
    }
  }
  public class RegSel_RS__B : RegSel {
    public RegSel_RS__B() : base() {
    }
    public override _IRegSel DowncastClone() {
      if (this is _IRegSel dt) { return dt; }
      return new RegSel_RS__B();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.RegSel_RS__B;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 2;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.RegSel.RS_B";
      return s;
    }
  }
  public class RegSel_RS__C : RegSel {
    public RegSel_RS__C() : base() {
    }
    public override _IRegSel DowncastClone() {
      if (this is _IRegSel dt) { return dt; }
      return new RegSel_RS__C();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.RegSel_RS__C;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 3;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.RegSel.RS_C";
      return s;
    }
  }
  public class RegSel_RS__D : RegSel {
    public RegSel_RS__D() : base() {
    }
    public override _IRegSel DowncastClone() {
      if (this is _IRegSel dt) { return dt; }
      return new RegSel_RS__D();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.RegSel_RS__D;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 4;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.RegSel.RS_D";
      return s;
    }
  }

  public interface _IAluInp {
    bool is_AluInp { get; }
    bool dtor_hasX { get; }
    TI59__Cpu._IRegSel dtor_x { get; }
    bool dtor_hasY { get; }
    TI59__Cpu._IRegSel dtor_y { get; }
    TI59__AluOps._IAluOp dtor_op { get; }
    _IAluInp DowncastClone();
  }
  public class AluInp : _IAluInp {
    public readonly bool _hasX;
    public readonly TI59__Cpu._IRegSel _x;
    public readonly bool _hasY;
    public readonly TI59__Cpu._IRegSel _y;
    public readonly TI59__AluOps._IAluOp _op;
    public AluInp(bool hasX, TI59__Cpu._IRegSel x, bool hasY, TI59__Cpu._IRegSel y, TI59__AluOps._IAluOp op) {
      this._hasX = hasX;
      this._x = x;
      this._hasY = hasY;
      this._y = y;
      this._op = op;
    }
    public _IAluInp DowncastClone() {
      if (this is _IAluInp dt) { return dt; }
      return new AluInp(_hasX, _x, _hasY, _y, _op);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.AluInp;
      return oth != null && this._hasX == oth._hasX && object.Equals(this._x, oth._x) && this._hasY == oth._hasY && object.Equals(this._y, oth._y) && object.Equals(this._op, oth._op);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._hasX));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._x));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._hasY));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._y));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._op));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.AluInp.AluInp";
      s += "(";
      s += Dafny.Helpers.ToString(this._hasX);
      s += ", ";
      s += Dafny.Helpers.ToString(this._x);
      s += ", ";
      s += Dafny.Helpers.ToString(this._hasY);
      s += ", ";
      s += Dafny.Helpers.ToString(this._y);
      s += ", ";
      s += Dafny.Helpers.ToString(this._op);
      s += ")";
      return s;
    }
    private static readonly TI59__Cpu._IAluInp theDefault = create(false, TI59__Cpu.RegSel.Default(), false, TI59__Cpu.RegSel.Default(), TI59__AluOps.AluOp.Default());
    public static TI59__Cpu._IAluInp Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Cpu._IAluInp> _TYPE = new Dafny.TypeDescriptor<TI59__Cpu._IAluInp>(TI59__Cpu.AluInp.Default());
    public static Dafny.TypeDescriptor<TI59__Cpu._IAluInp> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IAluInp create(bool hasX, TI59__Cpu._IRegSel x, bool hasY, TI59__Cpu._IRegSel y, TI59__AluOps._IAluOp op) {
      return new AluInp(hasX, x, hasY, y, op);
    }
    public static _IAluInp create_AluInp(bool hasX, TI59__Cpu._IRegSel x, bool hasY, TI59__Cpu._IRegSel y, TI59__AluOps._IAluOp op) {
      return create(hasX, x, hasY, y, op);
    }
    public bool is_AluInp { get { return true; } }
    public bool dtor_hasX {
      get {
        return this._hasX;
      }
    }
    public TI59__Cpu._IRegSel dtor_x {
      get {
        return this._x;
      }
    }
    public bool dtor_hasY {
      get {
        return this._hasY;
      }
    }
    public TI59__Cpu._IRegSel dtor_y {
      get {
        return this._y;
      }
    }
    public TI59__AluOps._IAluOp dtor_op {
      get {
        return this._op;
      }
    }
  }

  public interface _IDstSel {
    bool is_DS__A { get; }
    bool is_DS__IO { get; }
    bool is_DS__XCH__AB { get; }
    bool is_DS__B { get; }
    bool is_DS__C { get; }
    bool is_DS__XCH__CD { get; }
    bool is_DS__D { get; }
    bool is_DS__XCH__AE { get; }
    _IDstSel DowncastClone();
  }
  public abstract class DstSel : _IDstSel {
    public DstSel() {
    }
    private static readonly TI59__Cpu._IDstSel theDefault = create_DS__A();
    public static TI59__Cpu._IDstSel Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Cpu._IDstSel> _TYPE = new Dafny.TypeDescriptor<TI59__Cpu._IDstSel>(TI59__Cpu.DstSel.Default());
    public static Dafny.TypeDescriptor<TI59__Cpu._IDstSel> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IDstSel create_DS__A() {
      return new DstSel_DS__A();
    }
    public static _IDstSel create_DS__IO() {
      return new DstSel_DS__IO();
    }
    public static _IDstSel create_DS__XCH__AB() {
      return new DstSel_DS__XCH__AB();
    }
    public static _IDstSel create_DS__B() {
      return new DstSel_DS__B();
    }
    public static _IDstSel create_DS__C() {
      return new DstSel_DS__C();
    }
    public static _IDstSel create_DS__XCH__CD() {
      return new DstSel_DS__XCH__CD();
    }
    public static _IDstSel create_DS__D() {
      return new DstSel_DS__D();
    }
    public static _IDstSel create_DS__XCH__AE() {
      return new DstSel_DS__XCH__AE();
    }
    public bool is_DS__A { get { return this is DstSel_DS__A; } }
    public bool is_DS__IO { get { return this is DstSel_DS__IO; } }
    public bool is_DS__XCH__AB { get { return this is DstSel_DS__XCH__AB; } }
    public bool is_DS__B { get { return this is DstSel_DS__B; } }
    public bool is_DS__C { get { return this is DstSel_DS__C; } }
    public bool is_DS__XCH__CD { get { return this is DstSel_DS__XCH__CD; } }
    public bool is_DS__D { get { return this is DstSel_DS__D; } }
    public bool is_DS__XCH__AE { get { return this is DstSel_DS__XCH__AE; } }
    public static System.Collections.Generic.IEnumerable<_IDstSel> AllSingletonConstructors {
      get {
        yield return DstSel.create_DS__A();
        yield return DstSel.create_DS__IO();
        yield return DstSel.create_DS__XCH__AB();
        yield return DstSel.create_DS__B();
        yield return DstSel.create_DS__C();
        yield return DstSel.create_DS__XCH__CD();
        yield return DstSel.create_DS__D();
        yield return DstSel.create_DS__XCH__AE();
      }
    }
    public abstract _IDstSel DowncastClone();
  }
  public class DstSel_DS__A : DstSel {
    public DstSel_DS__A() : base() {
    }
    public override _IDstSel DowncastClone() {
      if (this is _IDstSel dt) { return dt; }
      return new DstSel_DS__A();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.DstSel_DS__A;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.DstSel.DS_A";
      return s;
    }
  }
  public class DstSel_DS__IO : DstSel {
    public DstSel_DS__IO() : base() {
    }
    public override _IDstSel DowncastClone() {
      if (this is _IDstSel dt) { return dt; }
      return new DstSel_DS__IO();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.DstSel_DS__IO;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 1;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.DstSel.DS_IO";
      return s;
    }
  }
  public class DstSel_DS__XCH__AB : DstSel {
    public DstSel_DS__XCH__AB() : base() {
    }
    public override _IDstSel DowncastClone() {
      if (this is _IDstSel dt) { return dt; }
      return new DstSel_DS__XCH__AB();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.DstSel_DS__XCH__AB;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 2;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.DstSel.DS_XCH_AB";
      return s;
    }
  }
  public class DstSel_DS__B : DstSel {
    public DstSel_DS__B() : base() {
    }
    public override _IDstSel DowncastClone() {
      if (this is _IDstSel dt) { return dt; }
      return new DstSel_DS__B();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.DstSel_DS__B;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 3;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.DstSel.DS_B";
      return s;
    }
  }
  public class DstSel_DS__C : DstSel {
    public DstSel_DS__C() : base() {
    }
    public override _IDstSel DowncastClone() {
      if (this is _IDstSel dt) { return dt; }
      return new DstSel_DS__C();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.DstSel_DS__C;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 4;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.DstSel.DS_C";
      return s;
    }
  }
  public class DstSel_DS__XCH__CD : DstSel {
    public DstSel_DS__XCH__CD() : base() {
    }
    public override _IDstSel DowncastClone() {
      if (this is _IDstSel dt) { return dt; }
      return new DstSel_DS__XCH__CD();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.DstSel_DS__XCH__CD;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 5;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.DstSel.DS_XCH_CD";
      return s;
    }
  }
  public class DstSel_DS__D : DstSel {
    public DstSel_DS__D() : base() {
    }
    public override _IDstSel DowncastClone() {
      if (this is _IDstSel dt) { return dt; }
      return new DstSel_DS__D();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.DstSel_DS__D;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 6;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.DstSel.DS_D";
      return s;
    }
  }
  public class DstSel_DS__XCH__AE : DstSel {
    public DstSel_DS__XCH__AE() : base() {
    }
    public override _IDstSel DowncastClone() {
      if (this is _IDstSel dt) { return dt; }
      return new DstSel_DS__XCH__AE();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Cpu.DstSel_DS__XCH__AE;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 7;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Cpu.DstSel.DS_XCH_AE";
      return s;
    }
  }
} // end of namespace TI59__Cpu
namespace TI59__Bus {

  public partial class __default {
    public static TI59__Bus._IBus BusInit() {
      return TI59__Bus.Bus.create((ushort)(0), (ushort)(0), TI59__Nibbles.__default.ZeroReg(), new Dafny.Rune(' '), false, false, (byte)(0), new BigInteger(15), false, BigInteger.Zero, BigInteger.Zero);
    }
    public static TI59__Bus._IBus BusCycleStart(TI59__Bus._IBus b) {
      TI59__Bus._IBus _0_dt__update__tmp_h0 = b;
      BigInteger _1_dt__update_hcycleCount_h0 = ((b).dtor_cycleCount) + (BigInteger.One);
      BigInteger _2_dt__update_haddr_h0 = new BigInteger(-1);
      Dafny.ISequence<byte> _3_dt__update_hio_h0 = TI59__Nibbles.__default.ZeroReg();
      ushort _4_dt__update_hirg_h0 = (ushort)(0);
      ushort _5_dt__update_hext_h0 = (ushort)(0);
      return TI59__Bus.Bus.create(_5_dt__update_hext_h0, _4_dt__update_hirg_h0, _3_dt__update_hio_h0, (_0_dt__update__tmp_h0).dtor_displayDigit, (_0_dt__update__tmp_h0).dtor_displayDpt, (_0_dt__update__tmp_h0).dtor_displaySegH, (_0_dt__update__tmp_h0).dtor_keyLine, (_0_dt__update__tmp_h0).dtor_dstate, (_0_dt__update__tmp_h0).dtor_idle, _2_dt__update_haddr_h0, _1_dt__update_hcycleCount_h0);
    }
    public static TI59__Bus._IBus BusAfterS14(TI59__Bus._IBus b) {
      TI59__Bus._IBus _0_dt__update__tmp_h0 = b;
      BigInteger _1_dt__update_hdstate_h0 = ((((b).dtor_dstate).Sign != 0) ? (((b).dtor_dstate) - (BigInteger.One)) : (new BigInteger(15)));
      byte _2_dt__update_hkeyLine_h0 = (byte)(0);
      return TI59__Bus.Bus.create((_0_dt__update__tmp_h0).dtor_ext, (_0_dt__update__tmp_h0).dtor_irg, (_0_dt__update__tmp_h0).dtor_io, (_0_dt__update__tmp_h0).dtor_displayDigit, (_0_dt__update__tmp_h0).dtor_displayDpt, (_0_dt__update__tmp_h0).dtor_displaySegH, _2_dt__update_hkeyLine_h0, _1_dt__update_hdstate_h0, (_0_dt__update__tmp_h0).dtor_idle, (_0_dt__update__tmp_h0).dtor_addr, (_0_dt__update__tmp_h0).dtor_cycleCount);
    }
  }

  public interface _IBus {
    bool is_Bus { get; }
    ushort dtor_ext { get; }
    ushort dtor_irg { get; }
    Dafny.ISequence<byte> dtor_io { get; }
    Dafny.Rune dtor_displayDigit { get; }
    bool dtor_displayDpt { get; }
    bool dtor_displaySegH { get; }
    byte dtor_keyLine { get; }
    BigInteger dtor_dstate { get; }
    bool dtor_idle { get; }
    BigInteger dtor_addr { get; }
    BigInteger dtor_cycleCount { get; }
    _IBus DowncastClone();
  }
  public class Bus : _IBus {
    public readonly ushort _ext;
    public readonly ushort _irg;
    public readonly Dafny.ISequence<byte> _io;
    public readonly Dafny.Rune _displayDigit;
    public readonly bool _displayDpt;
    public readonly bool _displaySegH;
    public readonly byte _keyLine;
    public readonly BigInteger _dstate;
    public readonly bool _idle;
    public readonly BigInteger _addr;
    public readonly BigInteger _cycleCount;
    public Bus(ushort ext, ushort irg, Dafny.ISequence<byte> io, Dafny.Rune displayDigit, bool displayDpt, bool displaySegH, byte keyLine, BigInteger dstate, bool idle, BigInteger addr, BigInteger cycleCount) {
      this._ext = ext;
      this._irg = irg;
      this._io = io;
      this._displayDigit = displayDigit;
      this._displayDpt = displayDpt;
      this._displaySegH = displaySegH;
      this._keyLine = keyLine;
      this._dstate = dstate;
      this._idle = idle;
      this._addr = addr;
      this._cycleCount = cycleCount;
    }
    public _IBus DowncastClone() {
      if (this is _IBus dt) { return dt; }
      return new Bus(_ext, _irg, _io, _displayDigit, _displayDpt, _displaySegH, _keyLine, _dstate, _idle, _addr, _cycleCount);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Bus.Bus;
      return oth != null && this._ext == oth._ext && this._irg == oth._irg && object.Equals(this._io, oth._io) && this._displayDigit == oth._displayDigit && this._displayDpt == oth._displayDpt && this._displaySegH == oth._displaySegH && this._keyLine == oth._keyLine && this._dstate == oth._dstate && this._idle == oth._idle && this._addr == oth._addr && this._cycleCount == oth._cycleCount;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._ext));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._irg));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._io));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._displayDigit));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._displayDpt));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._displaySegH));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._keyLine));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._dstate));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._idle));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._addr));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cycleCount));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Bus.Bus.Bus";
      s += "(";
      s += Dafny.Helpers.ToString(this._ext);
      s += ", ";
      s += Dafny.Helpers.ToString(this._irg);
      s += ", ";
      s += Dafny.Helpers.ToString(this._io);
      s += ", ";
      s += Dafny.Helpers.ToString(this._displayDigit);
      s += ", ";
      s += Dafny.Helpers.ToString(this._displayDpt);
      s += ", ";
      s += Dafny.Helpers.ToString(this._displaySegH);
      s += ", ";
      s += Dafny.Helpers.ToString(this._keyLine);
      s += ", ";
      s += Dafny.Helpers.ToString(this._dstate);
      s += ", ";
      s += Dafny.Helpers.ToString(this._idle);
      s += ", ";
      s += Dafny.Helpers.ToString(this._addr);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cycleCount);
      s += ")";
      return s;
    }
    private static readonly TI59__Bus._IBus theDefault = create(0, 0, TI59__Nibbles.Reg.Default(), new Dafny.Rune('D'), false, false, 0, BigInteger.Zero, false, BigInteger.Zero, BigInteger.Zero);
    public static TI59__Bus._IBus Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Bus._IBus> _TYPE = new Dafny.TypeDescriptor<TI59__Bus._IBus>(TI59__Bus.Bus.Default());
    public static Dafny.TypeDescriptor<TI59__Bus._IBus> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IBus create(ushort ext, ushort irg, Dafny.ISequence<byte> io, Dafny.Rune displayDigit, bool displayDpt, bool displaySegH, byte keyLine, BigInteger dstate, bool idle, BigInteger addr, BigInteger cycleCount) {
      return new Bus(ext, irg, io, displayDigit, displayDpt, displaySegH, keyLine, dstate, idle, addr, cycleCount);
    }
    public static _IBus create_Bus(ushort ext, ushort irg, Dafny.ISequence<byte> io, Dafny.Rune displayDigit, bool displayDpt, bool displaySegH, byte keyLine, BigInteger dstate, bool idle, BigInteger addr, BigInteger cycleCount) {
      return create(ext, irg, io, displayDigit, displayDpt, displaySegH, keyLine, dstate, idle, addr, cycleCount);
    }
    public bool is_Bus { get { return true; } }
    public ushort dtor_ext {
      get {
        return this._ext;
      }
    }
    public ushort dtor_irg {
      get {
        return this._irg;
      }
    }
    public Dafny.ISequence<byte> dtor_io {
      get {
        return this._io;
      }
    }
    public Dafny.Rune dtor_displayDigit {
      get {
        return this._displayDigit;
      }
    }
    public bool dtor_displayDpt {
      get {
        return this._displayDpt;
      }
    }
    public bool dtor_displaySegH {
      get {
        return this._displaySegH;
      }
    }
    public byte dtor_keyLine {
      get {
        return this._keyLine;
      }
    }
    public BigInteger dtor_dstate {
      get {
        return this._dstate;
      }
    }
    public bool dtor_idle {
      get {
        return this._idle;
      }
    }
    public BigInteger dtor_addr {
      get {
        return this._addr;
      }
    }
    public BigInteger dtor_cycleCount {
      get {
        return this._cycleCount;
      }
    }
  }
} // end of namespace TI59__Bus
namespace TI59__Chips {

  public partial class __default {
    public static TI59__Chips._IBrom BromInit(Dafny.ISequence<ushort> data, BigInteger start, BigInteger end)
    {
      return TI59__Chips.Brom.create(new BigInteger(65536), (ushort)(0), (ushort)(0), data, start, end);
    }
    public static BigInteger BromBranchTarget(TI59__Chips._IBrom b, bool cond)
    {
      bool _0_bcond = ((ushort)(((ushort)(((b).dtor_lastIrg) >> ((int)((byte)(11))))) & ((ushort)(1)))) != ((ushort)(0));
      BigInteger _1_boffset = new BigInteger((ushort)(((ushort)(((b).dtor_lastIrg) >> ((int)((byte)(1))))) & ((ushort)(1023))));
      bool _2_bneg = ((ushort)(((b).dtor_lastIrg) & ((ushort)(1)))) != ((ushort)(0));
      BigInteger _3_off = ((_2_bneg) ? ((BigInteger.Zero) - (_1_boffset)) : (_1_boffset));
      if ((cond) == (_0_bcond)) {
        return ((b).dtor_pc) + (_3_off);
      } else {
        return ((b).dtor_pc) + (BigInteger.One);
      }
    }
    public static _System._ITuple2<TI59__Chips._IBrom, TI59__Bus._IBus> BromStep(TI59__Chips._IBrom b, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = b;
      var _pat_let_tv1 = b;
      if (((sstate) != (new BigInteger(4))) || (!(write))) {
        return _System.Tuple2<TI59__Chips._IBrom, TI59__Bus._IBus>.create(b, bus);
      } else {
        BigInteger _0_pc1 = ((((ushort)(((bus).dtor_ext) & (TI59__Nibbles.__default.EXT__HOLD))) != ((ushort)(0))) ? ((b).dtor_pc) : (((((ushort)(((b).dtor_lastExt) & (TI59__Nibbles.__default.EXT__PREG))) != ((ushort)(0))) ? (new BigInteger((ushort)(((b).dtor_lastExt) >> ((int)((byte)(3)))))) : (((((ushort)(((b).dtor_lastIrg) & ((ushort)(4096)))) != ((ushort)(0))) ? (TI59__Chips.__default.BromBranchTarget(b, ((ushort)(((bus).dtor_ext) & (TI59__Nibbles.__default.EXT__COND))) != ((ushort)(0)))) : (((b).dtor_pc) + (BigInteger.One)))))));
        TI59__Bus._IBus _1_bus1 = (((((b).dtor_start) <= (_0_pc1)) && ((_0_pc1) < ((b).dtor_end))) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(bus, _pat_let89_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let89_0, _2_dt__update__tmp_h0 => Dafny.Helpers.Let<BigInteger, TI59__Bus._IBus>(_0_pc1, _pat_let90_0 => Dafny.Helpers.Let<BigInteger, TI59__Bus._IBus>(_pat_let90_0, _3_dt__update_haddr_h0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(((_pat_let_tv0).dtor_data).Select((_0_pc1) - ((_pat_let_tv1).dtor_start)), _pat_let91_0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(_pat_let91_0, _4_dt__update_hirg_h0 => TI59__Bus.Bus.create((_2_dt__update__tmp_h0).dtor_ext, _4_dt__update_hirg_h0, (_2_dt__update__tmp_h0).dtor_io, (_2_dt__update__tmp_h0).dtor_displayDigit, (_2_dt__update__tmp_h0).dtor_displayDpt, (_2_dt__update__tmp_h0).dtor_displaySegH, (_2_dt__update__tmp_h0).dtor_keyLine, (_2_dt__update__tmp_h0).dtor_dstate, (_2_dt__update__tmp_h0).dtor_idle, _3_dt__update_haddr_h0, (_2_dt__update__tmp_h0).dtor_cycleCount)))))))) : (bus));
        return _System.Tuple2<TI59__Chips._IBrom, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._IBrom, TI59__Chips._IBrom>(b, _pat_let92_0 => Dafny.Helpers.Let<TI59__Chips._IBrom, TI59__Chips._IBrom>(_pat_let92_0, _5_dt__update__tmp_h1 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IBrom>(_0_pc1, _pat_let93_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IBrom>(_pat_let93_0, _6_dt__update_hpc_h0 => TI59__Chips.Brom.create(_6_dt__update_hpc_h0, (_5_dt__update__tmp_h1).dtor_lastIrg, (_5_dt__update__tmp_h1).dtor_lastExt, (_5_dt__update__tmp_h1).dtor_data, (_5_dt__update__tmp_h1).dtor_start, (_5_dt__update__tmp_h1).dtor_end))))), _1_bus1);
      }
    }
    public static _System._ITuple2<TI59__Chips._IBrom, TI59__Bus._IBus> BromStepRead(TI59__Chips._IBrom b, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = bus;
      var _pat_let_tv1 = bus;
      if (((sstate) != (new BigInteger(15))) || (write)) {
        return _System.Tuple2<TI59__Chips._IBrom, TI59__Bus._IBus>.create(b, bus);
      } else {
        return _System.Tuple2<TI59__Chips._IBrom, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._IBrom, TI59__Chips._IBrom>(b, _pat_let94_0 => Dafny.Helpers.Let<TI59__Chips._IBrom, TI59__Chips._IBrom>(_pat_let94_0, _0_dt__update__tmp_h0 => Dafny.Helpers.Let<ushort, TI59__Chips._IBrom>((_pat_let_tv0).dtor_ext, _pat_let95_0 => Dafny.Helpers.Let<ushort, TI59__Chips._IBrom>(_pat_let95_0, _1_dt__update_hlastExt_h0 => Dafny.Helpers.Let<ushort, TI59__Chips._IBrom>((_pat_let_tv1).dtor_irg, _pat_let96_0 => Dafny.Helpers.Let<ushort, TI59__Chips._IBrom>(_pat_let96_0, _2_dt__update_hlastIrg_h0 => TI59__Chips.Brom.create((_0_dt__update__tmp_h0).dtor_pc, _2_dt__update_hlastIrg_h0, _1_dt__update_hlastExt_h0, (_0_dt__update__tmp_h0).dtor_data, (_0_dt__update__tmp_h0).dtor_start, (_0_dt__update__tmp_h0).dtor_end))))))), bus);
      }
    }
    public static TI59__Chips._IScom ScomInit(Dafny.ISequence<Dafny.ISequence<byte>> consts, BigInteger @base)
    {
      return TI59__Chips.Scom.create(consts, ((System.Func<Dafny.ISequence<Dafny.ISequence<byte>>>) (() => {
  BigInteger dim2 = new BigInteger(8);
  var arr2 = new Dafny.ISequence<byte>[Dafny.Helpers.ToIntChecked(dim2, "array size exceeds memory limit")];
  for (int i2 = 0; i2 < dim2; i2++) {
    var _0___v0 = (BigInteger) i2;
    arr2[(int)(_0___v0)] = TI59__Chips.__default.SeqFill((byte)(12));
  }
  return Dafny.Sequence<Dafny.ISequence<byte>>.FromArray(arr2);
}))(), 0U, 0U, @base, (@base) + (new BigInteger(32)), (Dafny.Helpers.EuclideanDivision(@base, new BigInteger(32))) * (new BigInteger(8)), ((Dafny.Helpers.EuclideanDivision(@base, new BigInteger(32))) * (new BigInteger(8))) + (new BigInteger(8)));
    }
    public static Dafny.ISequence<byte> SeqFill(byte v) {
      return ((System.Func<Dafny.ISequence<byte>>) (() => {
        BigInteger dim3 = new BigInteger(16);
        var arr3 = new byte[Dafny.Helpers.ToIntChecked(dim3, "array size exceeds memory limit")];
        for (int i3 = 0; i3 < dim3; i3++) {
          var _0___v1 = (BigInteger) i3;
          arr3[(int)(_0___v1)] = v;
        }
        return Dafny.Sequence<byte>.FromArray(arr3);
      }))();
    }
    public static _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus> ScomConstStepW(TI59__Chips._IScom s, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = bus;
      var _pat_let_tv1 = s;
      var _pat_let_tv2 = bus;
      var _pat_let_tv3 = s;
      if (((sstate).Sign != 0) || (!(write))) {
        return _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(s, bus);
      } else {
        uint _0_fc = Dafny.Helpers.Bv32ShiftRight((s).dtor_fifoConst, (int)((byte)(8)));
        TI59__Bus._IBus _1_bus1 = ((((_0_fc) & ((uint)(128U))) != (0U)) ? (Dafny.Helpers.Let<BigInteger, TI59__Bus._IBus>(new BigInteger((_0_fc) & ((uint)(127U))), _pat_let97_0 => Dafny.Helpers.Let<BigInteger, TI59__Bus._IBus>(_pat_let97_0, _2_idx => (((_2_idx) < (new BigInteger(((_pat_let_tv3).dtor_CONST).Count))) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let_tv0, _pat_let98_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let98_0, _3_dt__update__tmp_h0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Bus._IBus>(((_pat_let_tv1).dtor_CONST).Select(_2_idx), _pat_let99_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Bus._IBus>(_pat_let99_0, _4_dt__update_hio_h0 => TI59__Bus.Bus.create((_3_dt__update__tmp_h0).dtor_ext, (_3_dt__update__tmp_h0).dtor_irg, _4_dt__update_hio_h0, (_3_dt__update__tmp_h0).dtor_displayDigit, (_3_dt__update__tmp_h0).dtor_displayDpt, (_3_dt__update__tmp_h0).dtor_displaySegH, (_3_dt__update__tmp_h0).dtor_keyLine, (_3_dt__update__tmp_h0).dtor_dstate, (_3_dt__update__tmp_h0).dtor_idle, (_3_dt__update__tmp_h0).dtor_addr, (_3_dt__update__tmp_h0).dtor_cycleCount)))))) : (_pat_let_tv2))))) : (bus));
        return _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(s, _pat_let100_0 => Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(_pat_let100_0, _5_dt__update__tmp_h1 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(_0_fc, _pat_let101_0 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(_pat_let101_0, _6_dt__update_hfifoConst_h0 => TI59__Chips.Scom.create((_5_dt__update__tmp_h1).dtor_CONST, (_5_dt__update__tmp_h1).dtor_SCOM, _6_dt__update_hfifoConst_h0, (_5_dt__update__tmp_h1).dtor_fifoReg, (_5_dt__update__tmp_h1).dtor_startConst, (_5_dt__update__tmp_h1).dtor_endConst, (_5_dt__update__tmp_h1).dtor_startReg, (_5_dt__update__tmp_h1).dtor_endReg))))), _1_bus1);
      }
    }
    public static _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus> ScomRegStepW(TI59__Chips._IScom s, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = s;
      if (((sstate).Sign != 0) || (!(write))) {
        return _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(s, bus);
      } else {
        uint _0_fr = Dafny.Helpers.Bv32ShiftRight((s).dtor_fifoReg, (int)((byte)(8)));
        TI59__Bus._IBus _1_bus1 = ((((_0_fr) & ((uint)(16U))) != (0U)) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(bus, _pat_let102_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let102_0, _2_dt__update__tmp_h0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Bus._IBus>(((_pat_let_tv0).dtor_SCOM).Select(new BigInteger((Dafny.Helpers.Bv32ShiftRight(_0_fr, (int)((byte)(5)))) & (7U))), _pat_let103_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Bus._IBus>(_pat_let103_0, _3_dt__update_hio_h0 => TI59__Bus.Bus.create((_2_dt__update__tmp_h0).dtor_ext, (_2_dt__update__tmp_h0).dtor_irg, _3_dt__update_hio_h0, (_2_dt__update__tmp_h0).dtor_displayDigit, (_2_dt__update__tmp_h0).dtor_displayDpt, (_2_dt__update__tmp_h0).dtor_displaySegH, (_2_dt__update__tmp_h0).dtor_keyLine, (_2_dt__update__tmp_h0).dtor_dstate, (_2_dt__update__tmp_h0).dtor_idle, (_2_dt__update__tmp_h0).dtor_addr, (_2_dt__update__tmp_h0).dtor_cycleCount)))))) : (bus));
        return _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(s, _pat_let104_0 => Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(_pat_let104_0, _4_dt__update__tmp_h1 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(_0_fr, _pat_let105_0 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(_pat_let105_0, _5_dt__update_hfifoReg_h0 => TI59__Chips.Scom.create((_4_dt__update__tmp_h1).dtor_CONST, (_4_dt__update__tmp_h1).dtor_SCOM, (_4_dt__update__tmp_h1).dtor_fifoConst, _5_dt__update_hfifoReg_h0, (_4_dt__update__tmp_h1).dtor_startConst, (_4_dt__update__tmp_h1).dtor_endConst, (_4_dt__update__tmp_h1).dtor_startReg, (_4_dt__update__tmp_h1).dtor_endReg))))), _1_bus1);
      }
    }
    public static _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus> ScomRegStepR(TI59__Chips._IScom s, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = s;
      var _pat_let_tv1 = s;
      var _pat_let_tv2 = bus;
      var _pat_let_tv3 = s;
      var _pat_let_tv4 = bus;
      var _pat_let_tv5 = s;
      var _pat_let_tv6 = s;
      if (((sstate) != (new BigInteger(15))) || (write)) {
        return _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(s, bus);
      } else {
        TI59__Chips._IScom _0_s1 = ((((((s).dtor_fifoReg) & (1U)) != (0U)) && ((((s).dtor_fifoReg) & ((uint)(16U))) == (0U))) ? (Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>(new BigInteger((Dafny.Helpers.Bv32ShiftRight((s).dtor_fifoReg, (int)((byte)(5)))) & (7U)), _pat_let106_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>(_pat_let106_0, _1_a => Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(_pat_let_tv0, _pat_let107_0 => Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(_pat_let107_0, _2_dt__update__tmp_h0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<byte>>, TI59__Chips._IScom>(Dafny.Sequence<Dafny.ISequence<byte>>.Update((_pat_let_tv1).dtor_SCOM, _1_a, (_pat_let_tv2).dtor_io), _pat_let108_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<byte>>, TI59__Chips._IScom>(_pat_let108_0, _3_dt__update_hSCOM_h0 => TI59__Chips.Scom.create((_2_dt__update__tmp_h0).dtor_CONST, _3_dt__update_hSCOM_h0, (_2_dt__update__tmp_h0).dtor_fifoConst, (_2_dt__update__tmp_h0).dtor_fifoReg, (_2_dt__update__tmp_h0).dtor_startConst, (_2_dt__update__tmp_h0).dtor_endConst, (_2_dt__update__tmp_h0).dtor_startReg, (_2_dt__update__tmp_h0).dtor_endReg)))))))) : (s));
        TI59__Chips._IScom _4_s2 = ((((ushort)(((bus).dtor_irg) & ((ushort)(65519)))) == ((ushort)(2575))) ? (Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>(new BigInteger(((bus).dtor_io).Select(BigInteger.Zero)), _pat_let109_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>(_pat_let109_0, _5_addr => Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>((((((_0_s1).dtor_fifoReg) & ((uint)(16U))) != (0U)) ? (BigInteger.Zero) : (_5_addr)), _pat_let110_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>(_pat_let110_0, _6_addr1 => (((((_pat_let_tv5).dtor_startReg) <= (_6_addr1)) && ((_6_addr1) < ((_pat_let_tv6).dtor_endReg))) ? (Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>((((_6_addr1) - ((_pat_let_tv3).dtor_startReg)) * (new BigInteger(32))) + (new BigInteger((ushort)(((_pat_let_tv4).dtor_irg) & ((ushort)(31))))), _pat_let111_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>(_pat_let111_0, _7_a => Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(_0_s1, _pat_let112_0 => Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(_pat_let112_0, _8_dt__update__tmp_h1 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(((_0_s1).dtor_fifoReg) | (unchecked((uint)((Dafny.Helpers.Bv32ShiftLeft((uint)(_7_a), (int)((byte)(16))))))), _pat_let113_0 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(_pat_let113_0, _9_dt__update_hfifoReg_h0 => TI59__Chips.Scom.create((_8_dt__update__tmp_h1).dtor_CONST, (_8_dt__update__tmp_h1).dtor_SCOM, (_8_dt__update__tmp_h1).dtor_fifoConst, _9_dt__update_hfifoReg_h0, (_8_dt__update__tmp_h1).dtor_startConst, (_8_dt__update__tmp_h1).dtor_endConst, (_8_dt__update__tmp_h1).dtor_startReg, (_8_dt__update__tmp_h1).dtor_endReg)))))))) : (_0_s1))))))) : (_0_s1));
        return _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(_4_s2, bus);
      }
    }
    public static _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus> ScomConstStepR(TI59__Chips._IScom s, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = s;
      var _pat_let_tv1 = s;
      var _pat_let_tv2 = s;
      var _pat_let_tv3 = s;
      var _pat_let_tv4 = s;
      var _pat_let_tv5 = s;
      if (((sstate) != (new BigInteger(15))) || (write)) {
        return _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(s, bus);
      } else {
        TI59__Chips._IScom _0_s1 = (((((((ushort)(((bus).dtor_irg) & ((ushort)(4304)))) == ((ushort)(192))) && (((ushort)(((bus).dtor_irg) & ((ushort)(3840)))) != ((ushort)(0)))) && (((ushort)(((bus).dtor_irg) & ((ushort)(3840)))) != ((ushort)(2048)))) && (((ushort)(((bus).dtor_irg) & ((ushort)(3840)))) != ((ushort)(2560)))) ? (Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>(new BigInteger((ushort)(((ushort)(((ushort)(((bus).dtor_ext) >> ((int)((byte)(4))))) & ((ushort)(120)))) | ((ushort)(((ushort)(((bus).dtor_ext) >> ((int)((byte)(3))))) & ((ushort)(7)))))), _pat_let114_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IScom>(_pat_let114_0, _1_addr => (((((_pat_let_tv4).dtor_startConst) <= (_1_addr)) && ((_1_addr) < ((_pat_let_tv5).dtor_endConst))) ? (Dafny.Helpers.Let<uint, TI59__Chips._IScom>((uint)(((_1_addr) - ((_pat_let_tv0).dtor_startConst)) + (new BigInteger(128))), _pat_let115_0 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(_pat_let115_0, _2_idx => Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(_pat_let_tv1, _pat_let116_0 => Dafny.Helpers.Let<TI59__Chips._IScom, TI59__Chips._IScom>(_pat_let116_0, _3_dt__update__tmp_h0 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(((_pat_let_tv2).dtor_fifoConst) | (unchecked((uint)((Dafny.Helpers.Bv32ShiftLeft(_2_idx, (int)((byte)(8))))))), _pat_let117_0 => Dafny.Helpers.Let<uint, TI59__Chips._IScom>(_pat_let117_0, _4_dt__update_hfifoConst_h0 => TI59__Chips.Scom.create((_3_dt__update__tmp_h0).dtor_CONST, (_3_dt__update__tmp_h0).dtor_SCOM, _4_dt__update_hfifoConst_h0, (_3_dt__update__tmp_h0).dtor_fifoReg, (_3_dt__update__tmp_h0).dtor_startConst, (_3_dt__update__tmp_h0).dtor_endConst, (_3_dt__update__tmp_h0).dtor_startReg, (_3_dt__update__tmp_h0).dtor_endReg)))))))) : (_pat_let_tv3))))) : (s));
        return _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(_0_s1, bus);
      }
    }
    public static TI59__Chips._IRam RamInit(Dafny.ISequence<Dafny.ISequence<byte>> data, BigInteger start)
    {
      return TI59__Chips.Ram.create(start, (start) + (new BigInteger(30)), data, false, false, false, BigInteger.Zero, BigInteger.Zero);
    }
    public static _System._ITuple2<TI59__Chips._IRam, TI59__Bus._IBus> RamStepW(TI59__Chips._IRam r, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = r;
      var _pat_let_tv1 = r;
      var _pat_let_tv2 = r;
      var _pat_let_tv3 = r;
      var _pat_let_tv4 = r;
      var _pat_let_tv5 = r;
      var _pat_let_tv6 = r;
      if (((sstate).Sign != 0) || (!(write))) {
        return _System.Tuple2<TI59__Chips._IRam, TI59__Bus._IBus>.create(r, bus);
      } else if (!((r).dtor_execCmd)) {
        return _System.Tuple2<TI59__Chips._IRam, TI59__Bus._IBus>.create(r, bus);
      } else {
        bool _0_inRange = (((r).dtor_addr).Sign != -1) && (((r).dtor_addr) < (new BigInteger(((r).dtor_data).Count)));
        if (((r).dtor_cmd) == (new BigInteger(2))) {
          return _System.Tuple2<TI59__Chips._IRam, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(r, _pat_let118_0 => Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_pat_let118_0, _1_dt__update__tmp_h0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(false, _pat_let119_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let119_0, _2_dt__update_hexecCmd_h0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<byte>>, TI59__Chips._IRam>(((_0_inRange) ? (Dafny.Sequence<Dafny.ISequence<byte>>.Update((_pat_let_tv0).dtor_data, (_pat_let_tv1).dtor_addr, TI59__Nibbles.__default.ZeroReg())) : ((_pat_let_tv2).dtor_data)), _pat_let120_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<byte>>, TI59__Chips._IRam>(_pat_let120_0, _3_dt__update_hdata_h0 => TI59__Chips.Ram.create((_1_dt__update__tmp_h0).dtor_start, (_1_dt__update__tmp_h0).dtor_end, _3_dt__update_hdata_h0, (_1_dt__update__tmp_h0).dtor_waitCmd, (_1_dt__update__tmp_h0).dtor_wait2Cmd, _2_dt__update_hexecCmd_h0, (_1_dt__update__tmp_h0).dtor_cmd, (_1_dt__update__tmp_h0).dtor_addr))))))), bus);
        } else if (((r).dtor_cmd) == (new BigInteger(4))) {
          return _System.Tuple2<TI59__Chips._IRam, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(r, _pat_let121_0 => Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_pat_let121_0, _4_dt__update__tmp_h1 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(false, _pat_let122_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let122_0, _5_dt__update_hexecCmd_h1 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<byte>>, TI59__Chips._IRam>(TI59__Chips.__default.RamClear10((_pat_let_tv3).dtor_data, (_pat_let_tv4).dtor_addr), _pat_let123_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<byte>>, TI59__Chips._IRam>(_pat_let123_0, _6_dt__update_hdata_h1 => TI59__Chips.Ram.create((_4_dt__update__tmp_h1).dtor_start, (_4_dt__update__tmp_h1).dtor_end, _6_dt__update_hdata_h1, (_4_dt__update__tmp_h1).dtor_waitCmd, (_4_dt__update__tmp_h1).dtor_wait2Cmd, _5_dt__update_hexecCmd_h1, (_4_dt__update__tmp_h1).dtor_cmd, (_4_dt__update__tmp_h1).dtor_addr))))))), bus);
        } else if (((r).dtor_cmd).Sign == 0) {
          return _System.Tuple2<TI59__Chips._IRam, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(r, _pat_let124_0 => Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_pat_let124_0, _7_dt__update__tmp_h2 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(false, _pat_let125_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let125_0, _8_dt__update_hexecCmd_h2 => TI59__Chips.Ram.create((_7_dt__update__tmp_h2).dtor_start, (_7_dt__update__tmp_h2).dtor_end, (_7_dt__update__tmp_h2).dtor_data, (_7_dt__update__tmp_h2).dtor_waitCmd, (_7_dt__update__tmp_h2).dtor_wait2Cmd, _8_dt__update_hexecCmd_h2, (_7_dt__update__tmp_h2).dtor_cmd, (_7_dt__update__tmp_h2).dtor_addr))))), ((_0_inRange) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(bus, _pat_let126_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let126_0, _9_dt__update__tmp_h3 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Bus._IBus>(((_pat_let_tv5).dtor_data).Select((_pat_let_tv6).dtor_addr), _pat_let127_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Bus._IBus>(_pat_let127_0, _10_dt__update_hio_h0 => TI59__Bus.Bus.create((_9_dt__update__tmp_h3).dtor_ext, (_9_dt__update__tmp_h3).dtor_irg, _10_dt__update_hio_h0, (_9_dt__update__tmp_h3).dtor_displayDigit, (_9_dt__update__tmp_h3).dtor_displayDpt, (_9_dt__update__tmp_h3).dtor_displaySegH, (_9_dt__update__tmp_h3).dtor_keyLine, (_9_dt__update__tmp_h3).dtor_dstate, (_9_dt__update__tmp_h3).dtor_idle, (_9_dt__update__tmp_h3).dtor_addr, (_9_dt__update__tmp_h3).dtor_cycleCount)))))) : (bus)));
        } else {
          return _System.Tuple2<TI59__Chips._IRam, TI59__Bus._IBus>.create(r, bus);
        }
      }
    }
    public static Dafny.ISequence<Dafny.ISequence<byte>> RamClear10(Dafny.ISequence<Dafny.ISequence<byte>> data, BigInteger addr)
    {
      return TI59__Chips.__default.RamClearFrom(data, addr, BigInteger.Zero);
    }
    public static Dafny.ISequence<Dafny.ISequence<byte>> RamClearFrom(Dafny.ISequence<Dafny.ISequence<byte>> data, BigInteger addr, BigInteger k)
    {
    TAIL_CALL_START: ;
      if ((k) >= (new BigInteger(10))) {
        return data;
      } else if ((((addr) + (k)).Sign != -1) && (((addr) + (k)) < (new BigInteger(30)))) {
        Dafny.ISequence<Dafny.ISequence<byte>> _in0 = Dafny.Sequence<Dafny.ISequence<byte>>.Update(data, (addr) + (k), TI59__Nibbles.__default.ZeroReg());
        BigInteger _in1 = addr;
        BigInteger _in2 = (k) + (BigInteger.One);
        data = _in0;
        addr = _in1;
        k = _in2;
        goto TAIL_CALL_START;
      } else {
        Dafny.ISequence<Dafny.ISequence<byte>> _in3 = data;
        BigInteger _in4 = addr;
        BigInteger _in5 = (k) + (BigInteger.One);
        data = _in3;
        addr = _in4;
        k = _in5;
        goto TAIL_CALL_START;
      }
    }
    public static _System._ITuple2<TI59__Chips._IRam, TI59__Bus._IBus> RamStepR(TI59__Chips._IRam r, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = r;
      var _pat_let_tv1 = r;
      var _pat_let_tv2 = bus;
      var _pat_let_tv3 = bus;
      var _pat_let_tv4 = r;
      var _pat_let_tv5 = r;
      var _pat_let_tv6 = r;
      if (((sstate) != (new BigInteger(15))) || (write)) {
        return _System.Tuple2<TI59__Chips._IRam, TI59__Bus._IBus>.create(r, bus);
      } else {
        TI59__Chips._IRam _0_r1 = ((((((r).dtor_execCmd) && (((r).dtor_cmd) == (BigInteger.One))) && (((r).dtor_addr).Sign != -1)) && (((r).dtor_addr) < (new BigInteger(((r).dtor_data).Count)))) ? (Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(r, _pat_let128_0 => Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_pat_let128_0, _1_dt__update__tmp_h0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(false, _pat_let129_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let129_0, _2_dt__update_hexecCmd_h0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<byte>>, TI59__Chips._IRam>(Dafny.Sequence<Dafny.ISequence<byte>>.Update((_pat_let_tv0).dtor_data, (_pat_let_tv1).dtor_addr, (_pat_let_tv2).dtor_io), _pat_let130_0 => Dafny.Helpers.Let<Dafny.ISequence<Dafny.ISequence<byte>>, TI59__Chips._IRam>(_pat_let130_0, _3_dt__update_hdata_h0 => TI59__Chips.Ram.create((_1_dt__update__tmp_h0).dtor_start, (_1_dt__update__tmp_h0).dtor_end, _3_dt__update_hdata_h0, (_1_dt__update__tmp_h0).dtor_waitCmd, (_1_dt__update__tmp_h0).dtor_wait2Cmd, _2_dt__update_hexecCmd_h0, (_1_dt__update__tmp_h0).dtor_cmd, (_1_dt__update__tmp_h0).dtor_addr)))))))) : (r));
        TI59__Chips._IRam _4_r2 = (((_0_r1).dtor_wait2Cmd) ? (Dafny.Helpers.Let<BigInteger, TI59__Chips._IRam>(((new BigInteger(((bus).dtor_io).Select(new BigInteger(3)))) * (new BigInteger(10))) + (new BigInteger(((bus).dtor_io).Select(new BigInteger(2)))), _pat_let131_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IRam>(_pat_let131_0, _5_addr => Dafny.Helpers.Let<BigInteger, TI59__Chips._IRam>(new BigInteger(((_pat_let_tv3).dtor_io).Select(BigInteger.Zero)), _pat_let132_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IRam>(_pat_let132_0, _6_cmd => ((((((_pat_let_tv5).dtor_start) <= (_5_addr)) && ((_5_addr) < ((_pat_let_tv6).dtor_end))) && (((_6_cmd) <= (new BigInteger(2))) || ((_6_cmd) == (new BigInteger(4))))) ? (Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_0_r1, _pat_let133_0 => Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_pat_let133_0, _7_dt__update__tmp_h1 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(true, _pat_let134_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let134_0, _8_dt__update_hexecCmd_h1 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(false, _pat_let135_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let135_0, _9_dt__update_hwait2Cmd_h0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IRam>(_6_cmd, _pat_let136_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IRam>(_pat_let136_0, _10_dt__update_hcmd_h0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IRam>((_5_addr) - ((_pat_let_tv4).dtor_start), _pat_let137_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IRam>(_pat_let137_0, _11_dt__update_haddr_h0 => TI59__Chips.Ram.create((_7_dt__update__tmp_h1).dtor_start, (_7_dt__update__tmp_h1).dtor_end, (_7_dt__update__tmp_h1).dtor_data, (_7_dt__update__tmp_h1).dtor_waitCmd, _9_dt__update_hwait2Cmd_h0, _8_dt__update_hexecCmd_h1, _10_dt__update_hcmd_h0, _11_dt__update_haddr_h0)))))))))))) : (Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_0_r1, _pat_let138_0 => Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_pat_let138_0, _12_dt__update__tmp_h2 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(false, _pat_let139_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let139_0, _13_dt__update_hwait2Cmd_h1 => TI59__Chips.Ram.create((_12_dt__update__tmp_h2).dtor_start, (_12_dt__update__tmp_h2).dtor_end, (_12_dt__update__tmp_h2).dtor_data, (_12_dt__update__tmp_h2).dtor_waitCmd, _13_dt__update_hwait2Cmd_h1, (_12_dt__update__tmp_h2).dtor_execCmd, (_12_dt__update__tmp_h2).dtor_cmd, (_12_dt__update__tmp_h2).dtor_addr)))))))))))) : (_0_r1));
        TI59__Chips._IRam _14_r3 = (((_4_r2).dtor_waitCmd) ? (Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_4_r2, _pat_let140_0 => Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_pat_let140_0, _15_dt__update__tmp_h3 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(true, _pat_let141_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let141_0, _16_dt__update_hwait2Cmd_h2 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(false, _pat_let142_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let142_0, _17_dt__update_hwaitCmd_h0 => TI59__Chips.Ram.create((_15_dt__update__tmp_h3).dtor_start, (_15_dt__update__tmp_h3).dtor_end, (_15_dt__update__tmp_h3).dtor_data, _17_dt__update_hwaitCmd_h0, _16_dt__update_hwait2Cmd_h2, (_15_dt__update__tmp_h3).dtor_execCmd, (_15_dt__update__tmp_h3).dtor_cmd, (_15_dt__update__tmp_h3).dtor_addr)))))))) : (_4_r2));
        TI59__Chips._IRam _18_r4 = ((((bus).dtor_irg) == ((ushort)(2808))) ? (Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_14_r3, _pat_let143_0 => Dafny.Helpers.Let<TI59__Chips._IRam, TI59__Chips._IRam>(_pat_let143_0, _19_dt__update__tmp_h4 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(true, _pat_let144_0 => Dafny.Helpers.Let<bool, TI59__Chips._IRam>(_pat_let144_0, _20_dt__update_hwaitCmd_h1 => TI59__Chips.Ram.create((_19_dt__update__tmp_h4).dtor_start, (_19_dt__update__tmp_h4).dtor_end, (_19_dt__update__tmp_h4).dtor_data, _20_dt__update_hwaitCmd_h1, (_19_dt__update__tmp_h4).dtor_wait2Cmd, (_19_dt__update__tmp_h4).dtor_execCmd, (_19_dt__update__tmp_h4).dtor_cmd, (_19_dt__update__tmp_h4).dtor_addr)))))) : (_14_r3));
        return _System.Tuple2<TI59__Chips._IRam, TI59__Bus._IBus>.create(_18_r4, bus);
      }
    }
    public static TI59__Chips._ILib LibInit(Dafny.ISequence<byte> data) {
      return TI59__Chips.Lib.create(new BigInteger(222), data, TI59__Chips.__default.LIB__NO__FLAGS, TI59__Chips.__default.LIB__NO__FLAGS);
    }
    public static byte LibData(TI59__Chips._ILib l, BigInteger i)
    {
      if (((i).Sign != -1) && ((i) < (new BigInteger(((l).dtor_data).Count)))) {
        return ((l).dtor_data).Select(i);
      } else {
        return (byte)(0);
      }
    }
    public static _System._ITuple2<TI59__Chips._ILib, TI59__Bus._IBus> LibStepW(TI59__Chips._ILib l, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = bus;
      var _pat_let_tv1 = l;
      var _pat_let_tv2 = bus;
      var _pat_let_tv3 = l;
      var _pat_let_tv4 = bus;
      if (((sstate) != (new BigInteger(15))) || (!(write))) {
        return _System.Tuple2<TI59__Chips._ILib, TI59__Bus._IBus>.create(l, bus);
      } else if (((l).dtor_flagsDelay).dtor_inData) {
        ushort _0_d = (ushort)(TI59__Chips.__default.LibData(l, (l).dtor_pc));
        BigInteger _1_pc1 = ((l).dtor_pc) + (BigInteger.One);
        BigInteger _2_pc2 = (((_1_pc1) >= (new BigInteger(((l).dtor_data).Count))) ? (BigInteger.Zero) : (_1_pc1));
        return _System.Tuple2<TI59__Chips._ILib, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(l, _pat_let145_0 => Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_pat_let145_0, _3_dt__update__tmp_h0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._ILib>(_2_pc2, _pat_let146_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._ILib>(_pat_let146_0, _4_dt__update_hpc_h0 => TI59__Chips.Lib.create(_4_dt__update_hpc_h0, (_3_dt__update__tmp_h0).dtor_data, (_3_dt__update__tmp_h0).dtor_flags, (_3_dt__update__tmp_h0).dtor_flagsDelay))))), Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(bus, _pat_let147_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let147_0, _5_dt__update__tmp_h1 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>((ushort)(((_pat_let_tv0).dtor_ext) | (unchecked((ushort)(((ushort)((_0_d) << ((int)((byte)(3))))))))), _pat_let148_0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(_pat_let148_0, _6_dt__update_hext_h0 => TI59__Bus.Bus.create(_6_dt__update_hext_h0, (_5_dt__update__tmp_h1).dtor_irg, (_5_dt__update__tmp_h1).dtor_io, (_5_dt__update__tmp_h1).dtor_displayDigit, (_5_dt__update__tmp_h1).dtor_displayDpt, (_5_dt__update__tmp_h1).dtor_displaySegH, (_5_dt__update__tmp_h1).dtor_keyLine, (_5_dt__update__tmp_h1).dtor_dstate, (_5_dt__update__tmp_h1).dtor_idle, (_5_dt__update__tmp_h1).dtor_addr, (_5_dt__update__tmp_h1).dtor_cycleCount))))));
      } else if (((l).dtor_flagsDelay).dtor_inPc) {
        return _System.Tuple2<TI59__Chips._ILib, TI59__Bus._IBus>.create(Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(l, _pat_let149_0 => Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_pat_let149_0, _7_dt__update__tmp_h2 => Dafny.Helpers.Let<BigInteger, TI59__Chips._ILib>(Dafny.Helpers.EuclideanDivision((_pat_let_tv1).dtor_pc, new BigInteger(10)), _pat_let150_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._ILib>(_pat_let150_0, _8_dt__update_hpc_h1 => TI59__Chips.Lib.create(_8_dt__update_hpc_h1, (_7_dt__update__tmp_h2).dtor_data, (_7_dt__update__tmp_h2).dtor_flags, (_7_dt__update__tmp_h2).dtor_flagsDelay))))), Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(bus, _pat_let151_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let151_0, _9_dt__update__tmp_h3 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>((ushort)(((_pat_let_tv2).dtor_ext) | (unchecked((ushort)(((ushort)(((ushort)(Dafny.Helpers.EuclideanModulus((_pat_let_tv3).dtor_pc, new BigInteger(10)))) << ((int)((byte)(3))))))))), _pat_let152_0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(_pat_let152_0, _10_dt__update_hext_h1 => TI59__Bus.Bus.create(_10_dt__update_hext_h1, (_9_dt__update__tmp_h3).dtor_irg, (_9_dt__update__tmp_h3).dtor_io, (_9_dt__update__tmp_h3).dtor_displayDigit, (_9_dt__update__tmp_h3).dtor_displayDpt, (_9_dt__update__tmp_h3).dtor_displaySegH, (_9_dt__update__tmp_h3).dtor_keyLine, (_9_dt__update__tmp_h3).dtor_dstate, (_9_dt__update__tmp_h3).dtor_idle, (_9_dt__update__tmp_h3).dtor_addr, (_9_dt__update__tmp_h3).dtor_cycleCount))))));
      } else if (((l).dtor_flagsDelay).dtor_inDataHigh) {
        ushort _11_d = (ushort)((byte)((TI59__Chips.__default.LibData(l, (l).dtor_pc)) >> ((int)((byte)(4)))));
        return _System.Tuple2<TI59__Chips._ILib, TI59__Bus._IBus>.create(l, Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(bus, _pat_let153_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let153_0, _12_dt__update__tmp_h4 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>((ushort)(((_pat_let_tv4).dtor_ext) | (unchecked((ushort)(((ushort)((_11_d) << ((int)((byte)(3))))))))), _pat_let154_0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(_pat_let154_0, _13_dt__update_hext_h2 => TI59__Bus.Bus.create(_13_dt__update_hext_h2, (_12_dt__update__tmp_h4).dtor_irg, (_12_dt__update__tmp_h4).dtor_io, (_12_dt__update__tmp_h4).dtor_displayDigit, (_12_dt__update__tmp_h4).dtor_displayDpt, (_12_dt__update__tmp_h4).dtor_displaySegH, (_12_dt__update__tmp_h4).dtor_keyLine, (_12_dt__update__tmp_h4).dtor_dstate, (_12_dt__update__tmp_h4).dtor_idle, (_12_dt__update__tmp_h4).dtor_addr, (_12_dt__update__tmp_h4).dtor_cycleCount))))));
      } else {
        return _System.Tuple2<TI59__Chips._ILib, TI59__Bus._IBus>.create(l, bus);
      }
    }
    public static _System._ITuple2<TI59__Chips._ILib, TI59__Bus._IBus> LibStepR(TI59__Chips._ILib l, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = l;
      if (((sstate) != (new BigInteger(15))) || (write)) {
        return _System.Tuple2<TI59__Chips._ILib, TI59__Bus._IBus>.create(l, bus);
      } else {
        TI59__Chips._ILib _0_l1 = Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(l, _pat_let155_0 => Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_pat_let155_0, _1_dt__update__tmp_h0 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(TI59__Chips.__default.LIB__NO__FLAGS, _pat_let156_0 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(_pat_let156_0, _2_dt__update_hflags_h0 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>((_pat_let_tv0).dtor_flags, _pat_let157_0 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(_pat_let157_0, _3_dt__update_hflagsDelay_h0 => TI59__Chips.Lib.create((_1_dt__update__tmp_h0).dtor_pc, (_1_dt__update__tmp_h0).dtor_data, _2_dt__update_hflags_h0, _3_dt__update_hflagsDelay_h0)))))));
        TI59__Chips._ILib _4_l2 = ((((bus).dtor_irg) == ((ushort)(2574))) ? (Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_0_l1, _pat_let158_0 => Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_pat_let158_0, _5_dt__update__tmp_h1 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(TI59__Chips.LibFlags.create(true, ((_0_l1).dtor_flags).dtor_inPc, ((_0_l1).dtor_flags).dtor_inDataHigh), _pat_let159_0 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(_pat_let159_0, _6_dt__update_hflags_h1 => TI59__Chips.Lib.create((_5_dt__update__tmp_h1).dtor_pc, (_5_dt__update__tmp_h1).dtor_data, _6_dt__update_hflags_h1, (_5_dt__update__tmp_h1).dtor_flagsDelay)))))) : (((((bus).dtor_irg) == ((ushort)(2590))) ? (Dafny.Helpers.Let<BigInteger, TI59__Chips._ILib>(new BigInteger((ushort)(((ushort)(((bus).dtor_ext) >> ((int)((byte)(3))))) & ((ushort)(15)))), _pat_let160_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._ILib>(_pat_let160_0, _7_pcH => Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_0_l1, _pat_let161_0 => Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_pat_let161_0, _8_dt__update__tmp_h2 => Dafny.Helpers.Let<BigInteger, TI59__Chips._ILib>((Dafny.Helpers.EuclideanDivision((_0_l1).dtor_pc, new BigInteger(10))) + ((_7_pcH) * (new BigInteger(1000))), _pat_let162_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._ILib>(_pat_let162_0, _9_dt__update_hpc_h0 => TI59__Chips.Lib.create(_9_dt__update_hpc_h0, (_8_dt__update__tmp_h2).dtor_data, (_8_dt__update__tmp_h2).dtor_flags, (_8_dt__update__tmp_h2).dtor_flagsDelay)))))))) : (((((bus).dtor_irg) == ((ushort)(2606))) ? (Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_0_l1, _pat_let163_0 => Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_pat_let163_0, _10_dt__update__tmp_h3 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(TI59__Chips.LibFlags.create(((_0_l1).dtor_flags).dtor_inData, true, ((_0_l1).dtor_flags).dtor_inDataHigh), _pat_let164_0 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(_pat_let164_0, _11_dt__update_hflags_h2 => TI59__Chips.Lib.create((_10_dt__update__tmp_h3).dtor_pc, (_10_dt__update__tmp_h3).dtor_data, _11_dt__update_hflags_h2, (_10_dt__update__tmp_h3).dtor_flagsDelay)))))) : (((((bus).dtor_irg) == ((ushort)(2622))) ? (Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_0_l1, _pat_let165_0 => Dafny.Helpers.Let<TI59__Chips._ILib, TI59__Chips._ILib>(_pat_let165_0, _12_dt__update__tmp_h4 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(TI59__Chips.LibFlags.create(((_0_l1).dtor_flags).dtor_inData, ((_0_l1).dtor_flags).dtor_inPc, true), _pat_let166_0 => Dafny.Helpers.Let<TI59__Chips._ILibFlags, TI59__Chips._ILib>(_pat_let166_0, _13_dt__update_hflags_h3 => TI59__Chips.Lib.create((_12_dt__update__tmp_h4).dtor_pc, (_12_dt__update__tmp_h4).dtor_data, _13_dt__update_hflags_h3, (_12_dt__update__tmp_h4).dtor_flagsDelay)))))) : (_0_l1))))))));
        return _System.Tuple2<TI59__Chips._ILib, TI59__Bus._IBus>.create(_4_l2, bus);
      }
    }
    public static TI59__Chips._IKeyState KeyInit(Dafny.ISequence<TI59__Chips._IKeyEvent> script) {
      return TI59__Chips.KeyState.create(((System.Func<Dafny.ISequence<byte>>) (() => {
  BigInteger dim4 = new BigInteger(16);
  var arr4 = new byte[Dafny.Helpers.ToIntChecked(dim4, "array size exceeds memory limit")];
  for (int i4 = 0; i4 < dim4; i4++) {
    var _0_i = (BigInteger) i4;
    arr4[(int)(_0_i)] = (((_0_i) == (new BigInteger(10))) ? ((byte)(16)) : ((byte)(0)));
  }
  return Dafny.Sequence<byte>.FromArray(arr4);
}))(), (byte)(1), BigInteger.One, (byte)(0), BigInteger.Zero, new BigInteger(2), new BigInteger(3), (byte)(32), (byte)(32), script, BigInteger.Zero);
    }
    public static TI59__Chips._IKeyState KeyReadNew(TI59__Chips._IKeyState k, BigInteger cycle)
    {
      TI59__Chips._IKeyState _0_k1 = TI59__Chips.__default.KeyApplyToggles(k, cycle);
      if (((_0_k1).dtor_scriptPos) < (new BigInteger(((_0_k1).dtor_script).Count))) {
        TI59__Chips._IKeyEvent _1_ev = ((_0_k1).dtor_script).Select((_0_k1).dtor_scriptPos);
        if ((cycle) >= ((_1_ev).dtor_at)) {
          TI59__Chips._IKeyState _2_dt__update__tmp_h0 = _0_k1;
          BigInteger _3_dt__update_hscriptPos_h0 = ((_0_k1).dtor_scriptPos) + (BigInteger.One);
          BigInteger _4_dt__update_hkeyCount_h0 = ((((_1_ev).dtor_hold).Sign == 1) ? ((_1_ev).dtor_hold) : ((_0_k1).dtor_pressCycle));
          byte _5_dt__update_hkeyCode_h0 = (_1_ev).dtor_code;
          return TI59__Chips.KeyState.create((_2_dt__update__tmp_h0).dtor_persKeys, _5_dt__update_hkeyCode_h0, _4_dt__update_hkeyCount_h0, (_2_dt__update__tmp_h0).dtor_keyCodeHw, (_2_dt__update__tmp_h0).dtor_keyCountHw, (_2_dt__update__tmp_h0).dtor_pressCycle, (_2_dt__update__tmp_h0).dtor_unpressCycle, (_2_dt__update__tmp_h0).dtor_pressMask, (_2_dt__update__tmp_h0).dtor_unpressMask, (_2_dt__update__tmp_h0).dtor_script, _3_dt__update_hscriptPos_h0);
        } else {
          return _0_k1;
        }
      } else {
        return _0_k1;
      }
    }
    public static TI59__Chips._IKeyState KeyApplyToggles(TI59__Chips._IKeyState k, BigInteger cycle)
    {
    TAIL_CALL_START: ;
      var _pat_let_tv0 = k;
      var _pat_let_tv1 = k;
      if (((((k).dtor_scriptPos) < (new BigInteger(((k).dtor_script).Count))) && ((((k).dtor_script).Select((k).dtor_scriptPos)).dtor_toggle)) && ((cycle) >= ((((k).dtor_script).Select((k).dtor_scriptPos)).dtor_at))) {
        TI59__Chips._IKeyEvent _0_ev = ((k).dtor_script).Select((k).dtor_scriptPos);
        BigInteger _1_idx = new BigInteger((byte)(((_0_ev).dtor_code) & ((byte)(15))));
        BigInteger _2_bit = new BigInteger((byte)(((byte)(((_0_ev).dtor_code) >> ((int)((byte)(4))))) & ((byte)(7))));
        byte _3_pers = ((((_1_idx).Sign != -1) && ((_1_idx) < (new BigInteger(((k).dtor_persKeys).Count)))) ? (((k).dtor_persKeys).Select(_1_idx)) : ((byte)(0)));
        byte _4_pers1 = (byte)((_3_pers) ^ (TI59__Nibbles.__default.Bit8(_2_bit)));
        TI59__Chips._IKeyState _in0 = Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(k, _pat_let167_0 => Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let167_0, _5_dt__update__tmp_h0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(((_pat_let_tv0).dtor_scriptPos) + (BigInteger.One), _pat_let168_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(_pat_let168_0, _6_dt__update_hscriptPos_h0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Chips._IKeyState>(Dafny.Sequence<byte>.Update((_pat_let_tv1).dtor_persKeys, _1_idx, _4_pers1), _pat_let169_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Chips._IKeyState>(_pat_let169_0, _7_dt__update_hpersKeys_h0 => TI59__Chips.KeyState.create(_7_dt__update_hpersKeys_h0, (_5_dt__update__tmp_h0).dtor_keyCode, (_5_dt__update__tmp_h0).dtor_keyCount, (_5_dt__update__tmp_h0).dtor_keyCodeHw, (_5_dt__update__tmp_h0).dtor_keyCountHw, (_5_dt__update__tmp_h0).dtor_pressCycle, (_5_dt__update__tmp_h0).dtor_unpressCycle, (_5_dt__update__tmp_h0).dtor_pressMask, (_5_dt__update__tmp_h0).dtor_unpressMask, (_5_dt__update__tmp_h0).dtor_script, _6_dt__update_hscriptPos_h0)))))));
        BigInteger _in1 = cycle;
        k = _in0;
        cycle = _in1;
        goto TAIL_CALL_START;
      } else {
        return k;
      }
    }
    public static _System._ITuple2<TI59__Chips._IKeyState, TI59__Bus._IBus> KeyStep(TI59__Chips._IKeyState k, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      var _pat_let_tv0 = k;
      var _pat_let_tv1 = k;
      var _pat_let_tv2 = k;
      var _pat_let_tv3 = k;
      var _pat_let_tv4 = k;
      var _pat_let_tv5 = bus;
      var _pat_let_tv6 = bus;
      var _pat_let_tv7 = bus;
      var _pat_let_tv8 = k;
      var _pat_let_tv9 = bus;
      var _pat_let_tv10 = k;
      var _pat_let_tv11 = k;
      var _pat_let_tv12 = k;
      var _pat_let_tv13 = k;
      var _pat_let_tv14 = k;
      var _pat_let_tv15 = k;
      var _pat_let_tv16 = bus;
      var _pat_let_tv17 = k;
      var _pat_let_tv18 = k;
      var _pat_let_tv19 = k;
      var _pat_let_tv20 = k;
      var _pat_let_tv21 = k;
      if (((sstate) != (new BigInteger(15))) || (write)) {
        return _System.Tuple2<TI59__Chips._IKeyState, TI59__Bus._IBus>.create(k, bus);
      } else {
        TI59__Chips._IKeyState _0_k1 = (((((ushort)(((bus).dtor_ext) & (TI59__Nibbles.__default.EXT__HOLD))) == ((ushort)(0))) && (((ushort)(((bus).dtor_irg) & ((ushort)(65280)))) == ((ushort)(2048)))) ? (Dafny.Helpers.Let<bool, TI59__Chips._IKeyState>(((ushort)(((bus).dtor_irg) & ((ushort)(8)))) == ((ushort)(0)), _pat_let170_0 => Dafny.Helpers.Let<bool, TI59__Chips._IKeyState>(_pat_let170_0, _1_scan => ((!(_1_scan)) ? ((((((_pat_let_tv5).dtor_dstate) < (new BigInteger(15))) && (((_pat_let_tv6).dtor_dstate).Sign == 1)) ? (((((_pat_let_tv3).dtor_keyCountHw).Sign != 1) ? (_pat_let_tv0) : (Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let_tv1, _pat_let171_0 => Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let171_0, _2_dt__update__tmp_h0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(((_pat_let_tv2).dtor_keyCountHw) - (BigInteger.One), _pat_let172_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(_pat_let172_0, _3_dt__update_hkeyCountHw_h0 => TI59__Chips.KeyState.create((_2_dt__update__tmp_h0).dtor_persKeys, (_2_dt__update__tmp_h0).dtor_keyCode, (_2_dt__update__tmp_h0).dtor_keyCount, (_2_dt__update__tmp_h0).dtor_keyCodeHw, _3_dt__update_hkeyCountHw_h0, (_2_dt__update__tmp_h0).dtor_pressCycle, (_2_dt__update__tmp_h0).dtor_unpressCycle, (_2_dt__update__tmp_h0).dtor_pressMask, (_2_dt__update__tmp_h0).dtor_unpressMask, (_2_dt__update__tmp_h0).dtor_script, (_2_dt__update__tmp_h0).dtor_scriptPos)))))))) : (_pat_let_tv4))) : (Dafny.Helpers.Let<bool, TI59__Chips._IKeyState>(((ushort)(((_pat_let_tv7).dtor_irg) & ((ushort)(255)))) == ((ushort)((_pat_let_tv8).dtor_pressMask)), _pat_let173_0 => Dafny.Helpers.Let<bool, TI59__Chips._IKeyState>(_pat_let173_0, _4_allPress => Dafny.Helpers.Let<bool, TI59__Chips._IKeyState>(((ushort)(((_pat_let_tv9).dtor_irg) & ((ushort)(255)))) == ((ushort)((_pat_let_tv10).dtor_unpressMask)), _pat_let174_0 => Dafny.Helpers.Let<bool, TI59__Chips._IKeyState>(_pat_let174_0, _5_allUnpress => (((_4_allPress) && (((_pat_let_tv21).dtor_keyCount) > (BigInteger.One))) ? (Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let_tv11, _pat_let175_0 => Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let175_0, _6_dt__update__tmp_h1 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(((_pat_let_tv12).dtor_keyCount) - (BigInteger.One), _pat_let176_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(_pat_let176_0, _7_dt__update_hkeyCount_h0 => TI59__Chips.KeyState.create((_6_dt__update__tmp_h1).dtor_persKeys, (_6_dt__update__tmp_h1).dtor_keyCode, _7_dt__update_hkeyCount_h0, (_6_dt__update__tmp_h1).dtor_keyCodeHw, (_6_dt__update__tmp_h1).dtor_keyCountHw, (_6_dt__update__tmp_h1).dtor_pressCycle, (_6_dt__update__tmp_h1).dtor_unpressCycle, (_6_dt__update__tmp_h1).dtor_pressMask, (_6_dt__update__tmp_h1).dtor_unpressMask, (_6_dt__update__tmp_h1).dtor_script, (_6_dt__update__tmp_h1).dtor_scriptPos)))))) : ((((_5_allUnpress) && (((_pat_let_tv19).dtor_keyCount) > ((BigInteger.Zero) - ((_pat_let_tv20).dtor_unpressCycle)))) ? (Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let_tv13, _pat_let177_0 => Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let177_0, _8_dt__update__tmp_h2 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(BigInteger.Zero, _pat_let178_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(_pat_let178_0, _9_dt__update_hkeyCountHw_h1 => Dafny.Helpers.Let<byte, TI59__Chips._IKeyState>((byte)(0), _pat_let179_0 => Dafny.Helpers.Let<byte, TI59__Chips._IKeyState>(_pat_let179_0, _10_dt__update_hkeyCodeHw_h0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(((_pat_let_tv14).dtor_keyCount) - (BigInteger.One), _pat_let180_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(_pat_let180_0, _11_dt__update_hkeyCount_h1 => Dafny.Helpers.Let<byte, TI59__Chips._IKeyState>((byte)(0), _pat_let181_0 => Dafny.Helpers.Let<byte, TI59__Chips._IKeyState>(_pat_let181_0, _12_dt__update_hkeyCode_h0 => TI59__Chips.KeyState.create((_8_dt__update__tmp_h2).dtor_persKeys, _12_dt__update_hkeyCode_h0, _11_dt__update_hkeyCount_h1, _10_dt__update_hkeyCodeHw_h0, _9_dt__update_hkeyCountHw_h1, (_8_dt__update__tmp_h2).dtor_pressCycle, (_8_dt__update__tmp_h2).dtor_unpressCycle, (_8_dt__update__tmp_h2).dtor_pressMask, (_8_dt__update__tmp_h2).dtor_unpressMask, (_8_dt__update__tmp_h2).dtor_script, (_8_dt__update__tmp_h2).dtor_scriptPos)))))))))))) : ((((_4_allPress) && (((_pat_let_tv18).dtor_keyCode) == ((byte)(0)))) ? (Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(TI59__Chips.__default.KeyReadNew(_pat_let_tv15, (_pat_let_tv16).dtor_cycleCount), _pat_let182_0 => Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let182_0, _13_kread => Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_13_kread, _pat_let183_0 => Dafny.Helpers.Let<TI59__Chips._IKeyState, TI59__Chips._IKeyState>(_pat_let183_0, _14_dt__update__tmp_h3 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>((_13_kread).dtor_keyCount, _pat_let184_0 => Dafny.Helpers.Let<BigInteger, TI59__Chips._IKeyState>(_pat_let184_0, _15_dt__update_hkeyCountHw_h2 => Dafny.Helpers.Let<byte, TI59__Chips._IKeyState>((_13_kread).dtor_keyCode, _pat_let185_0 => Dafny.Helpers.Let<byte, TI59__Chips._IKeyState>(_pat_let185_0, _16_dt__update_hkeyCodeHw_h1 => TI59__Chips.KeyState.create((_14_dt__update__tmp_h3).dtor_persKeys, (_14_dt__update__tmp_h3).dtor_keyCode, (_14_dt__update__tmp_h3).dtor_keyCount, _16_dt__update_hkeyCodeHw_h1, _15_dt__update_hkeyCountHw_h2, (_14_dt__update__tmp_h3).dtor_pressCycle, (_14_dt__update__tmp_h3).dtor_unpressCycle, (_14_dt__update__tmp_h3).dtor_pressMask, (_14_dt__update__tmp_h3).dtor_unpressMask, (_14_dt__update__tmp_h3).dtor_script, (_14_dt__update__tmp_h3).dtor_scriptPos)))))))))) : (_pat_let_tv17))))))))))))))) : (k));
        byte _17_keyLine1 = ((((((((ushort)(((bus).dtor_ext) & (TI59__Nibbles.__default.EXT__HOLD))) == ((ushort)(0))) && (((ushort)(((bus).dtor_irg) & ((ushort)(65280)))) == ((ushort)(2048)))) && (((ushort)(((bus).dtor_irg) & ((ushort)(8)))) == ((ushort)(0)))) && ((new BigInteger((byte)(((_0_k1).dtor_keyCode) & ((byte)(15))))) == ((bus).dtor_dstate))) && (((_0_k1).dtor_keyCount).Sign == 1)) ? ((byte)(((bus).dtor_keyLine) | (TI59__Nibbles.__default.Bit8(new BigInteger((byte)(((byte)(((_0_k1).dtor_keyCode) >> ((int)((byte)(4))))) & ((byte)(7)))))))) : ((bus).dtor_keyLine));
        byte _18_keyLine2 = ((((new BigInteger((byte)(((_0_k1).dtor_keyCodeHw) & ((byte)(15))))) == ((bus).dtor_dstate)) && (((_0_k1).dtor_keyCountHw).Sign == 1)) ? ((byte)((_17_keyLine1) | (TI59__Nibbles.__default.Bit8(new BigInteger((byte)(((byte)(((_0_k1).dtor_keyCodeHw) >> ((int)((byte)(4))))) & ((byte)(7)))))))) : (_17_keyLine1));
        byte _19_keyLine3 = (byte)((_18_keyLine2) | (((_0_k1).dtor_persKeys).Select((bus).dtor_dstate)));
        return _System.Tuple2<TI59__Chips._IKeyState, TI59__Bus._IBus>.create(_0_k1, Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(bus, _pat_let186_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let186_0, _20_dt__update__tmp_h4 => Dafny.Helpers.Let<byte, TI59__Bus._IBus>(_19_keyLine3, _pat_let187_0 => Dafny.Helpers.Let<byte, TI59__Bus._IBus>(_pat_let187_0, _21_dt__update_hkeyLine_h0 => TI59__Bus.Bus.create((_20_dt__update__tmp_h4).dtor_ext, (_20_dt__update__tmp_h4).dtor_irg, (_20_dt__update__tmp_h4).dtor_io, (_20_dt__update__tmp_h4).dtor_displayDigit, (_20_dt__update__tmp_h4).dtor_displayDpt, (_20_dt__update__tmp_h4).dtor_displaySegH, _21_dt__update_hkeyLine_h0, (_20_dt__update__tmp_h4).dtor_dstate, (_20_dt__update__tmp_h4).dtor_idle, (_20_dt__update__tmp_h4).dtor_addr, (_20_dt__update__tmp_h4).dtor_cycleCount))))));
      }
    }
    public static TI59__Chips._IDisp DispInit() {
      return TI59__Chips.Disp.create(((System.Func<Dafny.ISequence<Dafny.Rune>>) (() => {
  BigInteger dim5 = new BigInteger(30);
  var arr5 = new Dafny.Rune[Dafny.Helpers.ToIntChecked(dim5, "array size exceeds memory limit")];
  for (int i5 = 0; i5 < dim5; i5++) {
    var _0___v2 = (BigInteger) i5;
    arr5[(int)(_0___v2)] = TI59__Nibbles.__default.Nul();
  }
  return Dafny.Sequence<Dafny.Rune>.FromArray(arr5);
}))(), ((System.Func<Dafny.ISequence<Dafny.Rune>>) (() => {
  BigInteger dim6 = new BigInteger(30);
  var arr6 = new Dafny.Rune[Dafny.Helpers.ToIntChecked(dim6, "array size exceeds memory limit")];
  for (int i6 = 0; i6 < dim6; i6++) {
    var _1___v3 = (BigInteger) i6;
    arr6[(int)(_1___v3)] = TI59__Nibbles.__default.Nul();
  }
  return Dafny.Sequence<Dafny.Rune>.FromArray(arr6);
}))(), BigInteger.Zero);
    }
    public static Dafny.ISequence<Dafny.Rune> SetChar(Dafny.ISequence<Dafny.Rune> s, BigInteger i, Dafny.Rune c)
    {
      if ((i) < (new BigInteger(30))) {
        return Dafny.Sequence<Dafny.Rune>.Update(s, i, c);
      } else {
        return s;
      }
    }
    public static _System._ITuple2<TI59__Chips._IDisp, TI59__Bus._IBus> DispStep(TI59__Chips._IDisp d, TI59__Bus._IBus bus, BigInteger sstate, bool write)
    {
      if (((sstate).Sign != 0) || (write)) {
        return _System.Tuple2<TI59__Chips._IDisp, TI59__Bus._IBus>.create(d, bus);
      } else {
        Dafny.ISequence<Dafny.Rune> _0_out1 = ((((new BigInteger(2)) <= ((bus).dtor_dstate)) && (((bus).dtor_dstate) <= (new BigInteger(13)))) ? (((((bus).dtor_dstate) != (new BigInteger(13))) ? (TI59__Chips.__default.SetChar((d).dtor_out, (d).dtor_pos, (bus).dtor_displayDigit)) : (((((bus).dtor_displayDigit) == (new Dafny.Rune('-'))) ? (TI59__Chips.__default.SetChar((d).dtor_out, (d).dtor_pos, (((bus).dtor_displaySegH) ? (new Dafny.Rune('E')) : (new Dafny.Rune('-'))))) : (TI59__Chips.__default.SetChar((d).dtor_out, (d).dtor_pos, (((bus).dtor_displaySegH) ? (new Dafny.Rune('C')) : (new Dafny.Rune(' '))))))))) : ((d).dtor_out));
        BigInteger _1_pos1 = ((((new BigInteger(2)) <= ((bus).dtor_dstate)) && (((bus).dtor_dstate) <= (new BigInteger(13)))) ? (((d).dtor_pos) + (BigInteger.One)) : ((d).dtor_pos));
        Dafny.ISequence<Dafny.Rune> _2_out2 = (((((new BigInteger(2)) <= ((bus).dtor_dstate)) && (((bus).dtor_dstate) <= (new BigInteger(13)))) && ((bus).dtor_displayDpt)) ? (TI59__Chips.__default.SetChar(_0_out1, _1_pos1, new Dafny.Rune('.'))) : (_0_out1));
        BigInteger _3_pos2 = (((((new BigInteger(2)) <= ((bus).dtor_dstate)) && (((bus).dtor_dstate) <= (new BigInteger(13)))) && ((bus).dtor_displayDpt)) ? ((_1_pos1) + (BigInteger.One)) : (_1_pos1));
        if (((bus).dtor_dstate).Sign == 0) {
          Dafny.ISequence<Dafny.Rune> _4_outF = TI59__Chips.__default.SetChar(_2_out2, (_3_pos2) + (new BigInteger(2)), (((bus).dtor_idle) ? (new Dafny.Rune(' ')) : (new Dafny.Rune('B'))));
          return _System.Tuple2<TI59__Chips._IDisp, TI59__Bus._IBus>.create(TI59__Chips.Disp.create(((System.Func<Dafny.ISequence<Dafny.Rune>>) (() => {
  BigInteger dim7 = new BigInteger(30);
  var arr7 = new Dafny.Rune[Dafny.Helpers.ToIntChecked(dim7, "array size exceeds memory limit")];
  for (int i7 = 0; i7 < dim7; i7++) {
    var _5_i = (BigInteger) i7;
    arr7[(int)(_5_i)] = (((_5_i) < (new BigInteger(29))) ? (new Dafny.Rune(' ')) : ((_4_outF).Select(new BigInteger(29))));
  }
  return Dafny.Sequence<Dafny.Rune>.FromArray(arr7);
}))(), _4_outF, BigInteger.Zero), bus);
        } else {
          return _System.Tuple2<TI59__Chips._IDisp, TI59__Bus._IBus>.create(TI59__Chips.Disp.create(_2_out2, (d).dtor_out1, _3_pos2), bus);
        }
      }
    }
    public static TI59__Chips._ILibFlags LIB__NO__FLAGS { get {
      return TI59__Chips.LibFlags.create(false, false, false);
    } }
  }

  public interface _IBrom {
    bool is_Brom { get; }
    BigInteger dtor_pc { get; }
    ushort dtor_lastIrg { get; }
    ushort dtor_lastExt { get; }
    Dafny.ISequence<ushort> dtor_data { get; }
    BigInteger dtor_start { get; }
    BigInteger dtor_end { get; }
    _IBrom DowncastClone();
  }
  public class Brom : _IBrom {
    public readonly BigInteger _pc;
    public readonly ushort _lastIrg;
    public readonly ushort _lastExt;
    public readonly Dafny.ISequence<ushort> _data;
    public readonly BigInteger _start;
    public readonly BigInteger _end;
    public Brom(BigInteger pc, ushort lastIrg, ushort lastExt, Dafny.ISequence<ushort> data, BigInteger start, BigInteger end) {
      this._pc = pc;
      this._lastIrg = lastIrg;
      this._lastExt = lastExt;
      this._data = data;
      this._start = start;
      this._end = end;
    }
    public _IBrom DowncastClone() {
      if (this is _IBrom dt) { return dt; }
      return new Brom(_pc, _lastIrg, _lastExt, _data, _start, _end);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Chips.Brom;
      return oth != null && this._pc == oth._pc && this._lastIrg == oth._lastIrg && this._lastExt == oth._lastExt && object.Equals(this._data, oth._data) && this._start == oth._start && this._end == oth._end;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._pc));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._lastIrg));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._lastExt));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._data));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._start));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._end));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Chips.Brom.Brom";
      s += "(";
      s += Dafny.Helpers.ToString(this._pc);
      s += ", ";
      s += Dafny.Helpers.ToString(this._lastIrg);
      s += ", ";
      s += Dafny.Helpers.ToString(this._lastExt);
      s += ", ";
      s += Dafny.Helpers.ToString(this._data);
      s += ", ";
      s += Dafny.Helpers.ToString(this._start);
      s += ", ";
      s += Dafny.Helpers.ToString(this._end);
      s += ")";
      return s;
    }
    private static readonly TI59__Chips._IBrom theDefault = create(BigInteger.Zero, 0, 0, Dafny.Sequence<ushort>.Empty, BigInteger.Zero, BigInteger.Zero);
    public static TI59__Chips._IBrom Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Chips._IBrom> _TYPE = new Dafny.TypeDescriptor<TI59__Chips._IBrom>(TI59__Chips.Brom.Default());
    public static Dafny.TypeDescriptor<TI59__Chips._IBrom> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IBrom create(BigInteger pc, ushort lastIrg, ushort lastExt, Dafny.ISequence<ushort> data, BigInteger start, BigInteger end) {
      return new Brom(pc, lastIrg, lastExt, data, start, end);
    }
    public static _IBrom create_Brom(BigInteger pc, ushort lastIrg, ushort lastExt, Dafny.ISequence<ushort> data, BigInteger start, BigInteger end) {
      return create(pc, lastIrg, lastExt, data, start, end);
    }
    public bool is_Brom { get { return true; } }
    public BigInteger dtor_pc {
      get {
        return this._pc;
      }
    }
    public ushort dtor_lastIrg {
      get {
        return this._lastIrg;
      }
    }
    public ushort dtor_lastExt {
      get {
        return this._lastExt;
      }
    }
    public Dafny.ISequence<ushort> dtor_data {
      get {
        return this._data;
      }
    }
    public BigInteger dtor_start {
      get {
        return this._start;
      }
    }
    public BigInteger dtor_end {
      get {
        return this._end;
      }
    }
  }

  public interface _IScom {
    bool is_Scom { get; }
    Dafny.ISequence<Dafny.ISequence<byte>> dtor_CONST { get; }
    Dafny.ISequence<Dafny.ISequence<byte>> dtor_SCOM { get; }
    uint dtor_fifoConst { get; }
    uint dtor_fifoReg { get; }
    BigInteger dtor_startConst { get; }
    BigInteger dtor_endConst { get; }
    BigInteger dtor_startReg { get; }
    BigInteger dtor_endReg { get; }
    _IScom DowncastClone();
  }
  public class Scom : _IScom {
    public readonly Dafny.ISequence<Dafny.ISequence<byte>> _CONST;
    public readonly Dafny.ISequence<Dafny.ISequence<byte>> _SCOM;
    public readonly uint _fifoConst;
    public readonly uint _fifoReg;
    public readonly BigInteger _startConst;
    public readonly BigInteger _endConst;
    public readonly BigInteger _startReg;
    public readonly BigInteger _endReg;
    public Scom(Dafny.ISequence<Dafny.ISequence<byte>> CONST, Dafny.ISequence<Dafny.ISequence<byte>> SCOM, uint fifoConst, uint fifoReg, BigInteger startConst, BigInteger endConst, BigInteger startReg, BigInteger endReg) {
      this._CONST = CONST;
      this._SCOM = SCOM;
      this._fifoConst = fifoConst;
      this._fifoReg = fifoReg;
      this._startConst = startConst;
      this._endConst = endConst;
      this._startReg = startReg;
      this._endReg = endReg;
    }
    public _IScom DowncastClone() {
      if (this is _IScom dt) { return dt; }
      return new Scom(_CONST, _SCOM, _fifoConst, _fifoReg, _startConst, _endConst, _startReg, _endReg);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Chips.Scom;
      return oth != null && object.Equals(this._CONST, oth._CONST) && object.Equals(this._SCOM, oth._SCOM) && this._fifoConst == oth._fifoConst && this._fifoReg == oth._fifoReg && this._startConst == oth._startConst && this._endConst == oth._endConst && this._startReg == oth._startReg && this._endReg == oth._endReg;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._CONST));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._SCOM));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._fifoConst));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._fifoReg));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._startConst));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._endConst));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._startReg));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._endReg));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Chips.Scom.Scom";
      s += "(";
      s += Dafny.Helpers.ToString(this._CONST);
      s += ", ";
      s += Dafny.Helpers.ToString(this._SCOM);
      s += ", ";
      s += Dafny.Helpers.ToString(this._fifoConst);
      s += ", ";
      s += Dafny.Helpers.ToString(this._fifoReg);
      s += ", ";
      s += Dafny.Helpers.ToString(this._startConst);
      s += ", ";
      s += Dafny.Helpers.ToString(this._endConst);
      s += ", ";
      s += Dafny.Helpers.ToString(this._startReg);
      s += ", ";
      s += Dafny.Helpers.ToString(this._endReg);
      s += ")";
      return s;
    }
    private static readonly TI59__Chips._IScom theDefault = create(Dafny.Sequence<Dafny.ISequence<byte>>.Empty, Dafny.Sequence<Dafny.ISequence<byte>>.Empty, 0, 0, BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, BigInteger.Zero);
    public static TI59__Chips._IScom Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Chips._IScom> _TYPE = new Dafny.TypeDescriptor<TI59__Chips._IScom>(TI59__Chips.Scom.Default());
    public static Dafny.TypeDescriptor<TI59__Chips._IScom> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IScom create(Dafny.ISequence<Dafny.ISequence<byte>> CONST, Dafny.ISequence<Dafny.ISequence<byte>> SCOM, uint fifoConst, uint fifoReg, BigInteger startConst, BigInteger endConst, BigInteger startReg, BigInteger endReg) {
      return new Scom(CONST, SCOM, fifoConst, fifoReg, startConst, endConst, startReg, endReg);
    }
    public static _IScom create_Scom(Dafny.ISequence<Dafny.ISequence<byte>> CONST, Dafny.ISequence<Dafny.ISequence<byte>> SCOM, uint fifoConst, uint fifoReg, BigInteger startConst, BigInteger endConst, BigInteger startReg, BigInteger endReg) {
      return create(CONST, SCOM, fifoConst, fifoReg, startConst, endConst, startReg, endReg);
    }
    public bool is_Scom { get { return true; } }
    public Dafny.ISequence<Dafny.ISequence<byte>> dtor_CONST {
      get {
        return this._CONST;
      }
    }
    public Dafny.ISequence<Dafny.ISequence<byte>> dtor_SCOM {
      get {
        return this._SCOM;
      }
    }
    public uint dtor_fifoConst {
      get {
        return this._fifoConst;
      }
    }
    public uint dtor_fifoReg {
      get {
        return this._fifoReg;
      }
    }
    public BigInteger dtor_startConst {
      get {
        return this._startConst;
      }
    }
    public BigInteger dtor_endConst {
      get {
        return this._endConst;
      }
    }
    public BigInteger dtor_startReg {
      get {
        return this._startReg;
      }
    }
    public BigInteger dtor_endReg {
      get {
        return this._endReg;
      }
    }
  }

  public interface _IRam {
    bool is_Ram { get; }
    BigInteger dtor_start { get; }
    BigInteger dtor_end { get; }
    Dafny.ISequence<Dafny.ISequence<byte>> dtor_data { get; }
    bool dtor_waitCmd { get; }
    bool dtor_wait2Cmd { get; }
    bool dtor_execCmd { get; }
    BigInteger dtor_cmd { get; }
    BigInteger dtor_addr { get; }
    _IRam DowncastClone();
  }
  public class Ram : _IRam {
    public readonly BigInteger _start;
    public readonly BigInteger _end;
    public readonly Dafny.ISequence<Dafny.ISequence<byte>> _data;
    public readonly bool _waitCmd;
    public readonly bool _wait2Cmd;
    public readonly bool _execCmd;
    public readonly BigInteger _cmd;
    public readonly BigInteger _addr;
    public Ram(BigInteger start, BigInteger end, Dafny.ISequence<Dafny.ISequence<byte>> data, bool waitCmd, bool wait2Cmd, bool execCmd, BigInteger cmd, BigInteger addr) {
      this._start = start;
      this._end = end;
      this._data = data;
      this._waitCmd = waitCmd;
      this._wait2Cmd = wait2Cmd;
      this._execCmd = execCmd;
      this._cmd = cmd;
      this._addr = addr;
    }
    public _IRam DowncastClone() {
      if (this is _IRam dt) { return dt; }
      return new Ram(_start, _end, _data, _waitCmd, _wait2Cmd, _execCmd, _cmd, _addr);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Chips.Ram;
      return oth != null && this._start == oth._start && this._end == oth._end && object.Equals(this._data, oth._data) && this._waitCmd == oth._waitCmd && this._wait2Cmd == oth._wait2Cmd && this._execCmd == oth._execCmd && this._cmd == oth._cmd && this._addr == oth._addr;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._start));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._end));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._data));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._waitCmd));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._wait2Cmd));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._execCmd));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cmd));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._addr));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Chips.Ram.Ram";
      s += "(";
      s += Dafny.Helpers.ToString(this._start);
      s += ", ";
      s += Dafny.Helpers.ToString(this._end);
      s += ", ";
      s += Dafny.Helpers.ToString(this._data);
      s += ", ";
      s += Dafny.Helpers.ToString(this._waitCmd);
      s += ", ";
      s += Dafny.Helpers.ToString(this._wait2Cmd);
      s += ", ";
      s += Dafny.Helpers.ToString(this._execCmd);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cmd);
      s += ", ";
      s += Dafny.Helpers.ToString(this._addr);
      s += ")";
      return s;
    }
    private static readonly TI59__Chips._IRam theDefault = create(BigInteger.Zero, BigInteger.Zero, Dafny.Sequence<Dafny.ISequence<byte>>.Empty, false, false, false, BigInteger.Zero, BigInteger.Zero);
    public static TI59__Chips._IRam Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Chips._IRam> _TYPE = new Dafny.TypeDescriptor<TI59__Chips._IRam>(TI59__Chips.Ram.Default());
    public static Dafny.TypeDescriptor<TI59__Chips._IRam> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IRam create(BigInteger start, BigInteger end, Dafny.ISequence<Dafny.ISequence<byte>> data, bool waitCmd, bool wait2Cmd, bool execCmd, BigInteger cmd, BigInteger addr) {
      return new Ram(start, end, data, waitCmd, wait2Cmd, execCmd, cmd, addr);
    }
    public static _IRam create_Ram(BigInteger start, BigInteger end, Dafny.ISequence<Dafny.ISequence<byte>> data, bool waitCmd, bool wait2Cmd, bool execCmd, BigInteger cmd, BigInteger addr) {
      return create(start, end, data, waitCmd, wait2Cmd, execCmd, cmd, addr);
    }
    public bool is_Ram { get { return true; } }
    public BigInteger dtor_start {
      get {
        return this._start;
      }
    }
    public BigInteger dtor_end {
      get {
        return this._end;
      }
    }
    public Dafny.ISequence<Dafny.ISequence<byte>> dtor_data {
      get {
        return this._data;
      }
    }
    public bool dtor_waitCmd {
      get {
        return this._waitCmd;
      }
    }
    public bool dtor_wait2Cmd {
      get {
        return this._wait2Cmd;
      }
    }
    public bool dtor_execCmd {
      get {
        return this._execCmd;
      }
    }
    public BigInteger dtor_cmd {
      get {
        return this._cmd;
      }
    }
    public BigInteger dtor_addr {
      get {
        return this._addr;
      }
    }
  }

  public interface _ILibFlags {
    bool is_LibFlags { get; }
    bool dtor_inData { get; }
    bool dtor_inPc { get; }
    bool dtor_inDataHigh { get; }
    _ILibFlags DowncastClone();
  }
  public class LibFlags : _ILibFlags {
    public readonly bool _inData;
    public readonly bool _inPc;
    public readonly bool _inDataHigh;
    public LibFlags(bool inData, bool inPc, bool inDataHigh) {
      this._inData = inData;
      this._inPc = inPc;
      this._inDataHigh = inDataHigh;
    }
    public _ILibFlags DowncastClone() {
      if (this is _ILibFlags dt) { return dt; }
      return new LibFlags(_inData, _inPc, _inDataHigh);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Chips.LibFlags;
      return oth != null && this._inData == oth._inData && this._inPc == oth._inPc && this._inDataHigh == oth._inDataHigh;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._inData));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._inPc));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._inDataHigh));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Chips.LibFlags.LibFlags";
      s += "(";
      s += Dafny.Helpers.ToString(this._inData);
      s += ", ";
      s += Dafny.Helpers.ToString(this._inPc);
      s += ", ";
      s += Dafny.Helpers.ToString(this._inDataHigh);
      s += ")";
      return s;
    }
    private static readonly TI59__Chips._ILibFlags theDefault = create(false, false, false);
    public static TI59__Chips._ILibFlags Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Chips._ILibFlags> _TYPE = new Dafny.TypeDescriptor<TI59__Chips._ILibFlags>(TI59__Chips.LibFlags.Default());
    public static Dafny.TypeDescriptor<TI59__Chips._ILibFlags> _TypeDescriptor() {
      return _TYPE;
    }
    public static _ILibFlags create(bool inData, bool inPc, bool inDataHigh) {
      return new LibFlags(inData, inPc, inDataHigh);
    }
    public static _ILibFlags create_LibFlags(bool inData, bool inPc, bool inDataHigh) {
      return create(inData, inPc, inDataHigh);
    }
    public bool is_LibFlags { get { return true; } }
    public bool dtor_inData {
      get {
        return this._inData;
      }
    }
    public bool dtor_inPc {
      get {
        return this._inPc;
      }
    }
    public bool dtor_inDataHigh {
      get {
        return this._inDataHigh;
      }
    }
  }

  public interface _ILib {
    bool is_Lib { get; }
    BigInteger dtor_pc { get; }
    Dafny.ISequence<byte> dtor_data { get; }
    TI59__Chips._ILibFlags dtor_flags { get; }
    TI59__Chips._ILibFlags dtor_flagsDelay { get; }
    _ILib DowncastClone();
  }
  public class Lib : _ILib {
    public readonly BigInteger _pc;
    public readonly Dafny.ISequence<byte> _data;
    public readonly TI59__Chips._ILibFlags _flags;
    public readonly TI59__Chips._ILibFlags _flagsDelay;
    public Lib(BigInteger pc, Dafny.ISequence<byte> data, TI59__Chips._ILibFlags flags, TI59__Chips._ILibFlags flagsDelay) {
      this._pc = pc;
      this._data = data;
      this._flags = flags;
      this._flagsDelay = flagsDelay;
    }
    public _ILib DowncastClone() {
      if (this is _ILib dt) { return dt; }
      return new Lib(_pc, _data, _flags, _flagsDelay);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Chips.Lib;
      return oth != null && this._pc == oth._pc && object.Equals(this._data, oth._data) && object.Equals(this._flags, oth._flags) && object.Equals(this._flagsDelay, oth._flagsDelay);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._pc));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._data));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._flags));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._flagsDelay));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Chips.Lib.Lib";
      s += "(";
      s += Dafny.Helpers.ToString(this._pc);
      s += ", ";
      s += Dafny.Helpers.ToString(this._data);
      s += ", ";
      s += Dafny.Helpers.ToString(this._flags);
      s += ", ";
      s += Dafny.Helpers.ToString(this._flagsDelay);
      s += ")";
      return s;
    }
    private static readonly TI59__Chips._ILib theDefault = create(BigInteger.Zero, Dafny.Sequence<byte>.Empty, TI59__Chips.LibFlags.Default(), TI59__Chips.LibFlags.Default());
    public static TI59__Chips._ILib Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Chips._ILib> _TYPE = new Dafny.TypeDescriptor<TI59__Chips._ILib>(TI59__Chips.Lib.Default());
    public static Dafny.TypeDescriptor<TI59__Chips._ILib> _TypeDescriptor() {
      return _TYPE;
    }
    public static _ILib create(BigInteger pc, Dafny.ISequence<byte> data, TI59__Chips._ILibFlags flags, TI59__Chips._ILibFlags flagsDelay) {
      return new Lib(pc, data, flags, flagsDelay);
    }
    public static _ILib create_Lib(BigInteger pc, Dafny.ISequence<byte> data, TI59__Chips._ILibFlags flags, TI59__Chips._ILibFlags flagsDelay) {
      return create(pc, data, flags, flagsDelay);
    }
    public bool is_Lib { get { return true; } }
    public BigInteger dtor_pc {
      get {
        return this._pc;
      }
    }
    public Dafny.ISequence<byte> dtor_data {
      get {
        return this._data;
      }
    }
    public TI59__Chips._ILibFlags dtor_flags {
      get {
        return this._flags;
      }
    }
    public TI59__Chips._ILibFlags dtor_flagsDelay {
      get {
        return this._flagsDelay;
      }
    }
  }

  public interface _IKeyEvent {
    bool is_KeyEvent { get; }
    byte dtor_code { get; }
    BigInteger dtor_at { get; }
    BigInteger dtor_hold { get; }
    bool dtor_toggle { get; }
    _IKeyEvent DowncastClone();
  }
  public class KeyEvent : _IKeyEvent {
    public readonly byte _code;
    public readonly BigInteger _at;
    public readonly BigInteger _hold;
    public readonly bool _toggle;
    public KeyEvent(byte code, BigInteger at, BigInteger hold, bool toggle) {
      this._code = code;
      this._at = at;
      this._hold = hold;
      this._toggle = toggle;
    }
    public _IKeyEvent DowncastClone() {
      if (this is _IKeyEvent dt) { return dt; }
      return new KeyEvent(_code, _at, _hold, _toggle);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Chips.KeyEvent;
      return oth != null && this._code == oth._code && this._at == oth._at && this._hold == oth._hold && this._toggle == oth._toggle;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._code));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._at));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._hold));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._toggle));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Chips.KeyEvent.KeyEvent";
      s += "(";
      s += Dafny.Helpers.ToString(this._code);
      s += ", ";
      s += Dafny.Helpers.ToString(this._at);
      s += ", ";
      s += Dafny.Helpers.ToString(this._hold);
      s += ", ";
      s += Dafny.Helpers.ToString(this._toggle);
      s += ")";
      return s;
    }
    private static readonly TI59__Chips._IKeyEvent theDefault = create(0, BigInteger.Zero, BigInteger.Zero, false);
    public static TI59__Chips._IKeyEvent Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Chips._IKeyEvent> _TYPE = new Dafny.TypeDescriptor<TI59__Chips._IKeyEvent>(TI59__Chips.KeyEvent.Default());
    public static Dafny.TypeDescriptor<TI59__Chips._IKeyEvent> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IKeyEvent create(byte code, BigInteger at, BigInteger hold, bool toggle) {
      return new KeyEvent(code, at, hold, toggle);
    }
    public static _IKeyEvent create_KeyEvent(byte code, BigInteger at, BigInteger hold, bool toggle) {
      return create(code, at, hold, toggle);
    }
    public bool is_KeyEvent { get { return true; } }
    public byte dtor_code {
      get {
        return this._code;
      }
    }
    public BigInteger dtor_at {
      get {
        return this._at;
      }
    }
    public BigInteger dtor_hold {
      get {
        return this._hold;
      }
    }
    public bool dtor_toggle {
      get {
        return this._toggle;
      }
    }
  }

  public interface _IKeyState {
    bool is_KeyState { get; }
    Dafny.ISequence<byte> dtor_persKeys { get; }
    byte dtor_keyCode { get; }
    BigInteger dtor_keyCount { get; }
    byte dtor_keyCodeHw { get; }
    BigInteger dtor_keyCountHw { get; }
    BigInteger dtor_pressCycle { get; }
    BigInteger dtor_unpressCycle { get; }
    byte dtor_pressMask { get; }
    byte dtor_unpressMask { get; }
    Dafny.ISequence<TI59__Chips._IKeyEvent> dtor_script { get; }
    BigInteger dtor_scriptPos { get; }
    _IKeyState DowncastClone();
  }
  public class KeyState : _IKeyState {
    public readonly Dafny.ISequence<byte> _persKeys;
    public readonly byte _keyCode;
    public readonly BigInteger _keyCount;
    public readonly byte _keyCodeHw;
    public readonly BigInteger _keyCountHw;
    public readonly BigInteger _pressCycle;
    public readonly BigInteger _unpressCycle;
    public readonly byte _pressMask;
    public readonly byte _unpressMask;
    public readonly Dafny.ISequence<TI59__Chips._IKeyEvent> _script;
    public readonly BigInteger _scriptPos;
    public KeyState(Dafny.ISequence<byte> persKeys, byte keyCode, BigInteger keyCount, byte keyCodeHw, BigInteger keyCountHw, BigInteger pressCycle, BigInteger unpressCycle, byte pressMask, byte unpressMask, Dafny.ISequence<TI59__Chips._IKeyEvent> script, BigInteger scriptPos) {
      this._persKeys = persKeys;
      this._keyCode = keyCode;
      this._keyCount = keyCount;
      this._keyCodeHw = keyCodeHw;
      this._keyCountHw = keyCountHw;
      this._pressCycle = pressCycle;
      this._unpressCycle = unpressCycle;
      this._pressMask = pressMask;
      this._unpressMask = unpressMask;
      this._script = script;
      this._scriptPos = scriptPos;
    }
    public _IKeyState DowncastClone() {
      if (this is _IKeyState dt) { return dt; }
      return new KeyState(_persKeys, _keyCode, _keyCount, _keyCodeHw, _keyCountHw, _pressCycle, _unpressCycle, _pressMask, _unpressMask, _script, _scriptPos);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Chips.KeyState;
      return oth != null && object.Equals(this._persKeys, oth._persKeys) && this._keyCode == oth._keyCode && this._keyCount == oth._keyCount && this._keyCodeHw == oth._keyCodeHw && this._keyCountHw == oth._keyCountHw && this._pressCycle == oth._pressCycle && this._unpressCycle == oth._unpressCycle && this._pressMask == oth._pressMask && this._unpressMask == oth._unpressMask && object.Equals(this._script, oth._script) && this._scriptPos == oth._scriptPos;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._persKeys));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._keyCode));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._keyCount));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._keyCodeHw));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._keyCountHw));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._pressCycle));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._unpressCycle));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._pressMask));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._unpressMask));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._script));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._scriptPos));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Chips.KeyState.KeyState";
      s += "(";
      s += Dafny.Helpers.ToString(this._persKeys);
      s += ", ";
      s += Dafny.Helpers.ToString(this._keyCode);
      s += ", ";
      s += Dafny.Helpers.ToString(this._keyCount);
      s += ", ";
      s += Dafny.Helpers.ToString(this._keyCodeHw);
      s += ", ";
      s += Dafny.Helpers.ToString(this._keyCountHw);
      s += ", ";
      s += Dafny.Helpers.ToString(this._pressCycle);
      s += ", ";
      s += Dafny.Helpers.ToString(this._unpressCycle);
      s += ", ";
      s += Dafny.Helpers.ToString(this._pressMask);
      s += ", ";
      s += Dafny.Helpers.ToString(this._unpressMask);
      s += ", ";
      s += Dafny.Helpers.ToString(this._script);
      s += ", ";
      s += Dafny.Helpers.ToString(this._scriptPos);
      s += ")";
      return s;
    }
    private static readonly TI59__Chips._IKeyState theDefault = create(Dafny.Sequence<byte>.Empty, 0, BigInteger.Zero, 0, BigInteger.Zero, BigInteger.Zero, BigInteger.Zero, 0, 0, Dafny.Sequence<TI59__Chips._IKeyEvent>.Empty, BigInteger.Zero);
    public static TI59__Chips._IKeyState Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Chips._IKeyState> _TYPE = new Dafny.TypeDescriptor<TI59__Chips._IKeyState>(TI59__Chips.KeyState.Default());
    public static Dafny.TypeDescriptor<TI59__Chips._IKeyState> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IKeyState create(Dafny.ISequence<byte> persKeys, byte keyCode, BigInteger keyCount, byte keyCodeHw, BigInteger keyCountHw, BigInteger pressCycle, BigInteger unpressCycle, byte pressMask, byte unpressMask, Dafny.ISequence<TI59__Chips._IKeyEvent> script, BigInteger scriptPos) {
      return new KeyState(persKeys, keyCode, keyCount, keyCodeHw, keyCountHw, pressCycle, unpressCycle, pressMask, unpressMask, script, scriptPos);
    }
    public static _IKeyState create_KeyState(Dafny.ISequence<byte> persKeys, byte keyCode, BigInteger keyCount, byte keyCodeHw, BigInteger keyCountHw, BigInteger pressCycle, BigInteger unpressCycle, byte pressMask, byte unpressMask, Dafny.ISequence<TI59__Chips._IKeyEvent> script, BigInteger scriptPos) {
      return create(persKeys, keyCode, keyCount, keyCodeHw, keyCountHw, pressCycle, unpressCycle, pressMask, unpressMask, script, scriptPos);
    }
    public bool is_KeyState { get { return true; } }
    public Dafny.ISequence<byte> dtor_persKeys {
      get {
        return this._persKeys;
      }
    }
    public byte dtor_keyCode {
      get {
        return this._keyCode;
      }
    }
    public BigInteger dtor_keyCount {
      get {
        return this._keyCount;
      }
    }
    public byte dtor_keyCodeHw {
      get {
        return this._keyCodeHw;
      }
    }
    public BigInteger dtor_keyCountHw {
      get {
        return this._keyCountHw;
      }
    }
    public BigInteger dtor_pressCycle {
      get {
        return this._pressCycle;
      }
    }
    public BigInteger dtor_unpressCycle {
      get {
        return this._unpressCycle;
      }
    }
    public byte dtor_pressMask {
      get {
        return this._pressMask;
      }
    }
    public byte dtor_unpressMask {
      get {
        return this._unpressMask;
      }
    }
    public Dafny.ISequence<TI59__Chips._IKeyEvent> dtor_script {
      get {
        return this._script;
      }
    }
    public BigInteger dtor_scriptPos {
      get {
        return this._scriptPos;
      }
    }
  }

  public interface _IDisp {
    bool is_Disp { get; }
    Dafny.ISequence<Dafny.Rune> dtor_out { get; }
    Dafny.ISequence<Dafny.Rune> dtor_out1 { get; }
    BigInteger dtor_pos { get; }
    _IDisp DowncastClone();
  }
  public class Disp : _IDisp {
    public readonly Dafny.ISequence<Dafny.Rune> _out;
    public readonly Dafny.ISequence<Dafny.Rune> _out1;
    public readonly BigInteger _pos;
    public Disp(Dafny.ISequence<Dafny.Rune> @out, Dafny.ISequence<Dafny.Rune> out1, BigInteger pos) {
      this._out = @out;
      this._out1 = out1;
      this._pos = pos;
    }
    public _IDisp DowncastClone() {
      if (this is _IDisp dt) { return dt; }
      return new Disp(_out, _out1, _pos);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Chips.Disp;
      return oth != null && object.Equals(this._out, oth._out) && object.Equals(this._out1, oth._out1) && this._pos == oth._pos;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._out));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._out1));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._pos));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Chips.Disp.Disp";
      s += "(";
      s += this._out.ToVerbatimString(true);
      s += ", ";
      s += this._out1.ToVerbatimString(true);
      s += ", ";
      s += Dafny.Helpers.ToString(this._pos);
      s += ")";
      return s;
    }
    private static readonly TI59__Chips._IDisp theDefault = create(Dafny.Sequence<Dafny.Rune>.Empty, Dafny.Sequence<Dafny.Rune>.Empty, BigInteger.Zero);
    public static TI59__Chips._IDisp Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Chips._IDisp> _TYPE = new Dafny.TypeDescriptor<TI59__Chips._IDisp>(TI59__Chips.Disp.Default());
    public static Dafny.TypeDescriptor<TI59__Chips._IDisp> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IDisp create(Dafny.ISequence<Dafny.Rune> @out, Dafny.ISequence<Dafny.Rune> out1, BigInteger pos) {
      return new Disp(@out, out1, pos);
    }
    public static _IDisp create_Disp(Dafny.ISequence<Dafny.Rune> @out, Dafny.ISequence<Dafny.Rune> out1, BigInteger pos) {
      return create(@out, out1, pos);
    }
    public bool is_Disp { get { return true; } }
    public Dafny.ISequence<Dafny.Rune> dtor_out {
      get {
        return this._out;
      }
    }
    public Dafny.ISequence<Dafny.Rune> dtor_out1 {
      get {
        return this._out1;
      }
    }
    public BigInteger dtor_pos {
      get {
        return this._pos;
      }
    }
  }
} // end of namespace TI59__Chips
namespace TI59__Machine {

  public partial class __default {
    public static bool MachineWF(TI59__Machine._IMachine m) {
      return (((((((((((new BigInteger(((m).dtor_broms).Count)) == (new BigInteger(3))) && ((new BigInteger(((m).dtor_scoms).Count)) == (new BigInteger(2)))) && ((new BigInteger(((m).dtor_rams).Count)) == (new BigInteger(4)))) && (Dafny.Helpers.Id<Func<TI59__Machine._IMachine, bool>>((_0_m) => Dafny.Helpers.Quantifier<BigInteger>(Dafny.Helpers.IntegerRange(BigInteger.Zero, new BigInteger(3)), true, (((_forall_var_0) => {
        BigInteger _1_i = (BigInteger)_forall_var_0;
        return !(((_1_i).Sign != -1) && ((_1_i) < (new BigInteger(3)))) || (Dafny.Helpers.Let<TI59__Chips._IBrom, bool>(((_0_m).dtor_broms).Select(_1_i), _pat_let188_0 => Dafny.Helpers.Let<TI59__Chips._IBrom, bool>(_pat_let188_0, _2_b => (((_2_b).dtor_start).Sign != -1) && ((new BigInteger(((_2_b).dtor_data).Count)) == (((_2_b).dtor_end) - ((_2_b).dtor_start))))));
      }))))(m))) && (Dafny.Helpers.Id<Func<TI59__Machine._IMachine, bool>>((_3_m) => Dafny.Helpers.Quantifier<BigInteger>(Dafny.Helpers.IntegerRange(BigInteger.Zero, new BigInteger(2)), true, (((_forall_var_1) => {
        BigInteger _4_i = (BigInteger)_forall_var_1;
        return !(((_4_i).Sign != -1) && ((_4_i) < (new BigInteger(2)))) || (Dafny.Helpers.Let<TI59__Chips._IScom, bool>(((_3_m).dtor_scoms).Select(_4_i), _pat_let189_0 => Dafny.Helpers.Let<TI59__Chips._IScom, bool>(_pat_let189_0, _5_s => ((((((new BigInteger(((_5_s).dtor_CONST).Count)) == (new BigInteger(32))) && ((new BigInteger(((_5_s).dtor_SCOM).Count)) == (new BigInteger(8)))) && (((_5_s).dtor_startConst).Sign != -1)) && (((_5_s).dtor_endConst) == (((_5_s).dtor_startConst) + (new BigInteger(32))))) && (((_5_s).dtor_startReg).Sign != -1)) && (((_5_s).dtor_endReg) == (((_5_s).dtor_startReg) + (new BigInteger(8)))))));
      }))))(m))) && (Dafny.Helpers.Id<Func<TI59__Machine._IMachine, bool>>((_6_m) => Dafny.Helpers.Quantifier<BigInteger>(Dafny.Helpers.IntegerRange(BigInteger.Zero, new BigInteger(4)), true, (((_forall_var_2) => {
        BigInteger _7_i = (BigInteger)_forall_var_2;
        return !(((_7_i).Sign != -1) && ((_7_i) < (new BigInteger(4)))) || ((new BigInteger(((((_6_m).dtor_rams).Select(_7_i)).dtor_data).Count)) == (new BigInteger(30)));
      }))))(m))) && ((new BigInteger((((m).dtor_lib).dtor_data).Count)) == (new BigInteger(5000)))) && ((new BigInteger((((m).dtor_keys).dtor_persKeys).Count)) == (new BigInteger(16)))) && ((new BigInteger((((m).dtor_disp).dtor_out).Count)) == (new BigInteger(30)))) && ((new BigInteger((((m).dtor_disp).dtor_out1).Count)) == (new BigInteger(30)))) && ((((m).dtor_bus).dtor_dstate) <= (new BigInteger(15)));
    }
    public static TI59__Machine._IMachine AluChip(TI59__Machine._IMachine m, BigInteger sstate, bool write)
    {
      TI59__Cpu._ICpu _0_c = (m).dtor_cpu;
      TI59__Bus._IBus _1_bus = (m).dtor_bus;
      TI59__Cpu._ICpu _2_c1 = Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_0_c, _pat_let190_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let190_0, _3_dt__update__tmp_h0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_0_c).dtor_flags) & ((ushort)(64511))), _pat_let191_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let191_0, _4_dt__update_hflags_h0 => TI59__Cpu.Cpu.create((_3_dt__update__tmp_h0).dtor_A, (_3_dt__update__tmp_h0).dtor_B, (_3_dt__update__tmp_h0).dtor_C, (_3_dt__update__tmp_h0).dtor_D, (_3_dt__update__tmp_h0).dtor_E, (_3_dt__update__tmp_h0).dtor_KR, (_3_dt__update__tmp_h0).dtor_SR, (_3_dt__update__tmp_h0).dtor_fA, (_3_dt__update__tmp_h0).dtor_fB, (_3_dt__update__tmp_h0).dtor_R5, (_3_dt__update__tmp_h0).dtor_Sout, (_3_dt__update__tmp_h0).dtor_Sin, (_3_dt__update__tmp_h0).dtor_opcode, _4_dt__update_hflags_h0, (_3_dt__update__tmp_h0).dtor_addr, (_3_dt__update__tmp_h0).dtor_reset, (_3_dt__update__tmp_h0).dtor_zeroSuppr)))));
      if (((_2_c1).dtor_reset).Sign != 0) {
        BigInteger _5_reset1 = ((((sstate).Sign == 0) && (write)) ? (((_2_c1).dtor_reset) - (BigInteger.One)) : ((_2_c1).dtor_reset));
        TI59__Bus._IBus _6_bus1 = (((((sstate).Sign == 0) && (write)) && (((_2_c1).dtor_reset) > (BigInteger.One))) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let192_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let192_0, _7_dt__update__tmp_h1 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>((ushort)(1), _pat_let193_0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(_pat_let193_0, _8_dt__update_hext_h0 => TI59__Bus.Bus.create(_8_dt__update_hext_h0, (_7_dt__update__tmp_h1).dtor_irg, (_7_dt__update__tmp_h1).dtor_io, (_7_dt__update__tmp_h1).dtor_displayDigit, (_7_dt__update__tmp_h1).dtor_displayDpt, (_7_dt__update__tmp_h1).dtor_displaySegH, (_7_dt__update__tmp_h1).dtor_keyLine, (_7_dt__update__tmp_h1).dtor_dstate, (_7_dt__update__tmp_h1).dtor_idle, (_7_dt__update__tmp_h1).dtor_addr, (_7_dt__update__tmp_h1).dtor_cycleCount)))))) : (_1_bus));
        bool _9_captured = (((sstate) == (new BigInteger(15))) && (!(write))) && ((_5_reset1) == (BigInteger.One));
        TI59__Cpu._ICpu _10_c2 = ((_9_captured) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c1, _pat_let194_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let194_0, _11_dt__update__tmp_h2 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>((_6_bus1).dtor_addr, _pat_let195_0 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(_pat_let195_0, _12_dt__update_haddr_h0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((_6_bus1).dtor_irg, _pat_let196_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let196_0, _13_dt__update_hopcode_h0 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(_5_reset1, _pat_let197_0 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(_pat_let197_0, _14_dt__update_hreset_h0 => TI59__Cpu.Cpu.create((_11_dt__update__tmp_h2).dtor_A, (_11_dt__update__tmp_h2).dtor_B, (_11_dt__update__tmp_h2).dtor_C, (_11_dt__update__tmp_h2).dtor_D, (_11_dt__update__tmp_h2).dtor_E, (_11_dt__update__tmp_h2).dtor_KR, (_11_dt__update__tmp_h2).dtor_SR, (_11_dt__update__tmp_h2).dtor_fA, (_11_dt__update__tmp_h2).dtor_fB, (_11_dt__update__tmp_h2).dtor_R5, (_11_dt__update__tmp_h2).dtor_Sout, (_11_dt__update__tmp_h2).dtor_Sin, _13_dt__update_hopcode_h0, (_11_dt__update__tmp_h2).dtor_flags, _12_dt__update_haddr_h0, _14_dt__update_hreset_h0, (_11_dt__update__tmp_h2).dtor_zeroSuppr)))))))))) : (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c1, _pat_let198_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let198_0, _15_dt__update__tmp_h3 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(_5_reset1, _pat_let199_0 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(_pat_let199_0, _16_dt__update_hreset_h1 => TI59__Cpu.Cpu.create((_15_dt__update__tmp_h3).dtor_A, (_15_dt__update__tmp_h3).dtor_B, (_15_dt__update__tmp_h3).dtor_C, (_15_dt__update__tmp_h3).dtor_D, (_15_dt__update__tmp_h3).dtor_E, (_15_dt__update__tmp_h3).dtor_KR, (_15_dt__update__tmp_h3).dtor_SR, (_15_dt__update__tmp_h3).dtor_fA, (_15_dt__update__tmp_h3).dtor_fB, (_15_dt__update__tmp_h3).dtor_R5, (_15_dt__update__tmp_h3).dtor_Sout, (_15_dt__update__tmp_h3).dtor_Sin, (_15_dt__update__tmp_h3).dtor_opcode, (_15_dt__update__tmp_h3).dtor_flags, (_15_dt__update__tmp_h3).dtor_addr, _16_dt__update_hreset_h1, (_15_dt__update__tmp_h3).dtor_zeroSuppr)))))));
        TI59__Machine._IMachine _17_dt__update__tmp_h4 = m;
        bool _18_dt__update_hhalted_h0 = ((m).dtor_halted) || ((_9_captured) && (((_6_bus1).dtor_addr) == (new BigInteger(-1))));
        TI59__Bus._IBus _19_dt__update_hbus_h0 = _6_bus1;
        TI59__Cpu._ICpu _20_dt__update_hcpu_h0 = _10_c2;
        return TI59__Machine.Machine.create(_20_dt__update_hcpu_h0, (_17_dt__update__tmp_h4).dtor_broms, (_17_dt__update__tmp_h4).dtor_scoms, (_17_dt__update__tmp_h4).dtor_rams, (_17_dt__update__tmp_h4).dtor_lib, (_17_dt__update__tmp_h4).dtor_keys, (_17_dt__update__tmp_h4).dtor_disp, _19_dt__update_hbus_h0, _18_dt__update_hhalted_h0);
      } else if (((sstate).Sign == 0) && (write)) {
        TI59__Cpu._ICpu _21_c2 = Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c1, _pat_let200_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let200_0, _22_dt__update__tmp_h5 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_2_c1).dtor_flags) & ((ushort)(65533))), _pat_let201_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let201_0, _23_dt__update_hflags_h1 => TI59__Cpu.Cpu.create((_22_dt__update__tmp_h5).dtor_A, (_22_dt__update__tmp_h5).dtor_B, (_22_dt__update__tmp_h5).dtor_C, (_22_dt__update__tmp_h5).dtor_D, (_22_dt__update__tmp_h5).dtor_E, (_22_dt__update__tmp_h5).dtor_KR, (_22_dt__update__tmp_h5).dtor_SR, (_22_dt__update__tmp_h5).dtor_fA, (_22_dt__update__tmp_h5).dtor_fB, (_22_dt__update__tmp_h5).dtor_R5, (_22_dt__update__tmp_h5).dtor_Sout, (_22_dt__update__tmp_h5).dtor_Sin, (_22_dt__update__tmp_h5).dtor_opcode, _23_dt__update_hflags_h1, (_22_dt__update__tmp_h5).dtor_addr, (_22_dt__update__tmp_h5).dtor_reset, (_22_dt__update__tmp_h5).dtor_zeroSuppr)))));
        TI59__Cpu._ICpu _24_c3 = Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_21_c2, _pat_let202_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let202_0, _25_dt__update__tmp_h6 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(TI59__Nibbles.__default.ZeroReg(), _pat_let203_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let203_0, _26_dt__update_hSout_h0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(TI59__Nibbles.__default.ZeroReg(), _pat_let204_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let204_0, _27_dt__update_hSin_h0 => TI59__Cpu.Cpu.create((_25_dt__update__tmp_h6).dtor_A, (_25_dt__update__tmp_h6).dtor_B, (_25_dt__update__tmp_h6).dtor_C, (_25_dt__update__tmp_h6).dtor_D, (_25_dt__update__tmp_h6).dtor_E, (_25_dt__update__tmp_h6).dtor_KR, (_25_dt__update__tmp_h6).dtor_SR, (_25_dt__update__tmp_h6).dtor_fA, (_25_dt__update__tmp_h6).dtor_fB, (_25_dt__update__tmp_h6).dtor_R5, _26_dt__update_hSout_h0, _27_dt__update_hSin_h0, (_25_dt__update__tmp_h6).dtor_opcode, (_25_dt__update__tmp_h6).dtor_flags, (_25_dt__update__tmp_h6).dtor_addr, (_25_dt__update__tmp_h6).dtor_reset, (_25_dt__update__tmp_h6).dtor_zeroSuppr)))))));
        TI59__Cpu._ICpu _28_c4 = ((TI59__Cpu.__default.Flg((_24_c3).dtor_flags, TI59__Nibbles.__default.FLG__COND)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_24_c3, _pat_let205_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let205_0, _29_dt__update__tmp_h7 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_24_c3).dtor_flags) | (TI59__Nibbles.__default.FLG__COND__LAST)), _pat_let206_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let206_0, _30_dt__update_hflags_h2 => TI59__Cpu.Cpu.create((_29_dt__update__tmp_h7).dtor_A, (_29_dt__update__tmp_h7).dtor_B, (_29_dt__update__tmp_h7).dtor_C, (_29_dt__update__tmp_h7).dtor_D, (_29_dt__update__tmp_h7).dtor_E, (_29_dt__update__tmp_h7).dtor_KR, (_29_dt__update__tmp_h7).dtor_SR, (_29_dt__update__tmp_h7).dtor_fA, (_29_dt__update__tmp_h7).dtor_fB, (_29_dt__update__tmp_h7).dtor_R5, (_29_dt__update__tmp_h7).dtor_Sout, (_29_dt__update__tmp_h7).dtor_Sin, (_29_dt__update__tmp_h7).dtor_opcode, _30_dt__update_hflags_h2, (_29_dt__update__tmp_h7).dtor_addr, (_29_dt__update__tmp_h7).dtor_reset, (_29_dt__update__tmp_h7).dtor_zeroSuppr)))))) : (_24_c3));
        _System._ITuple4<Dafny.Rune, bool, bool, bool> _31_gd = TI59__Cpu.__default.GenDigitIdle(_28_c4, (_1_bus).dtor_dstate);
        TI59__Cpu._ICpu _32_c5 = ((TI59__Cpu.__default.Flg((_28_c4).dtor_flags, TI59__Nibbles.__default.FLG__IDLE)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_28_c4, _pat_let207_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let207_0, _33_dt__update__tmp_h8 => Dafny.Helpers.Let<bool, TI59__Cpu._ICpu>((_31_gd).dtor__3, _pat_let208_0 => Dafny.Helpers.Let<bool, TI59__Cpu._ICpu>(_pat_let208_0, _34_dt__update_hzeroSuppr_h0 => TI59__Cpu.Cpu.create((_33_dt__update__tmp_h8).dtor_A, (_33_dt__update__tmp_h8).dtor_B, (_33_dt__update__tmp_h8).dtor_C, (_33_dt__update__tmp_h8).dtor_D, (_33_dt__update__tmp_h8).dtor_E, (_33_dt__update__tmp_h8).dtor_KR, (_33_dt__update__tmp_h8).dtor_SR, (_33_dt__update__tmp_h8).dtor_fA, (_33_dt__update__tmp_h8).dtor_fB, (_33_dt__update__tmp_h8).dtor_R5, (_33_dt__update__tmp_h8).dtor_Sout, (_33_dt__update__tmp_h8).dtor_Sin, (_33_dt__update__tmp_h8).dtor_opcode, (_33_dt__update__tmp_h8).dtor_flags, (_33_dt__update__tmp_h8).dtor_addr, (_33_dt__update__tmp_h8).dtor_reset, _34_dt__update_hzeroSuppr_h0)))))) : (_28_c4));
        TI59__Bus._IBus _35_bus1 = ((TI59__Cpu.__default.Flg((_28_c4).dtor_flags, TI59__Nibbles.__default.FLG__IDLE)) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let209_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let209_0, _36_dt__update__tmp_h9 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>((_31_gd).dtor__2, _pat_let210_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let210_0, _37_dt__update_hdisplaySegH_h0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>((_31_gd).dtor__1, _pat_let211_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let211_0, _38_dt__update_hdisplayDpt_h0 => Dafny.Helpers.Let<Dafny.Rune, TI59__Bus._IBus>((_31_gd).dtor__0, _pat_let212_0 => Dafny.Helpers.Let<Dafny.Rune, TI59__Bus._IBus>(_pat_let212_0, _39_dt__update_hdisplayDigit_h0 => TI59__Bus.Bus.create((_36_dt__update__tmp_h9).dtor_ext, (_36_dt__update__tmp_h9).dtor_irg, (_36_dt__update__tmp_h9).dtor_io, _39_dt__update_hdisplayDigit_h0, _38_dt__update_hdisplayDpt_h0, _37_dt__update_hdisplaySegH_h0, (_36_dt__update__tmp_h9).dtor_keyLine, (_36_dt__update__tmp_h9).dtor_dstate, (_36_dt__update__tmp_h9).dtor_idle, (_36_dt__update__tmp_h9).dtor_addr, (_36_dt__update__tmp_h9).dtor_cycleCount)))))))))) : (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let213_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let213_0, _40_dt__update__tmp_h10 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(true, _pat_let214_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let214_0, _41_dt__update_hdisplaySegH_h1 => TI59__Bus.Bus.create((_40_dt__update__tmp_h10).dtor_ext, (_40_dt__update__tmp_h10).dtor_irg, (_40_dt__update__tmp_h10).dtor_io, (_40_dt__update__tmp_h10).dtor_displayDigit, (_40_dt__update__tmp_h10).dtor_displayDpt, _41_dt__update_hdisplaySegH_h1, (_40_dt__update__tmp_h10).dtor_keyLine, (_40_dt__update__tmp_h10).dtor_dstate, (_40_dt__update__tmp_h10).dtor_idle, (_40_dt__update__tmp_h10).dtor_addr, (_40_dt__update__tmp_h10).dtor_cycleCount)))))));
        _System._ITuple2<bool, bool> _42_re = TI59__Cpu.__default.RunEarly((_32_c5).dtor_opcode);
        TI59__Cpu._ICpu _43_c6 = (((_42_re).dtor__1) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_32_c5, _pat_let215_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let215_0, _44_dt__update__tmp_h11 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_32_c5).dtor_flags) | (TI59__Nibbles.__default.FLG__IO__VALID)), _pat_let216_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let216_0, _45_dt__update_hflags_h3 => TI59__Cpu.Cpu.create((_44_dt__update__tmp_h11).dtor_A, (_44_dt__update__tmp_h11).dtor_B, (_44_dt__update__tmp_h11).dtor_C, (_44_dt__update__tmp_h11).dtor_D, (_44_dt__update__tmp_h11).dtor_E, (_44_dt__update__tmp_h11).dtor_KR, (_44_dt__update__tmp_h11).dtor_SR, (_44_dt__update__tmp_h11).dtor_fA, (_44_dt__update__tmp_h11).dtor_fB, (_44_dt__update__tmp_h11).dtor_R5, (_44_dt__update__tmp_h11).dtor_Sout, (_44_dt__update__tmp_h11).dtor_Sin, (_44_dt__update__tmp_h11).dtor_opcode, _45_dt__update_hflags_h3, (_44_dt__update__tmp_h11).dtor_addr, (_44_dt__update__tmp_h11).dtor_reset, (_44_dt__update__tmp_h11).dtor_zeroSuppr)))))) : (_32_c5));
        TI59__Cpu._ICpu _46_c7 = (((_42_re).dtor__0) ? (TI59__Cpu.__default.Execute(_43_c6, (_43_c6).dtor_opcode, (_1_bus).dtor_dstate, (_35_bus1).dtor_ext, (_35_bus1).dtor_keyLine)) : (_43_c6));
        TI59__Bus._IBus _47_bus2 = ((((_42_re).dtor__0) && (TI59__Cpu.__default.Flg((_46_c7).dtor_flags, TI59__Nibbles.__default.FLG__IO__VALID))) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_35_bus1, _pat_let217_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let217_0, _48_dt__update__tmp_h12 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Bus._IBus>((_46_c7).dtor_Sout, _pat_let218_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Bus._IBus>(_pat_let218_0, _49_dt__update_hio_h0 => TI59__Bus.Bus.create((_48_dt__update__tmp_h12).dtor_ext, (_48_dt__update__tmp_h12).dtor_irg, _49_dt__update_hio_h0, (_48_dt__update__tmp_h12).dtor_displayDigit, (_48_dt__update__tmp_h12).dtor_displayDpt, (_48_dt__update__tmp_h12).dtor_displaySegH, (_48_dt__update__tmp_h12).dtor_keyLine, (_48_dt__update__tmp_h12).dtor_dstate, (_48_dt__update__tmp_h12).dtor_idle, (_48_dt__update__tmp_h12).dtor_addr, (_48_dt__update__tmp_h12).dtor_cycleCount)))))) : (_35_bus1));
        TI59__Bus._IBus _50_bus3 = ((((_46_c7).dtor_opcode) != ((ushort)(2572))) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_47_bus2, _pat_let219_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let219_0, _51_dt__update__tmp_h13 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>((ushort)(((ushort)(((ushort)(((_46_c7).dtor_KR) >> ((int)((byte)(1))))) | (unchecked((ushort)(((ushort)(((_46_c7).dtor_KR) << ((int)((byte)(15)))))))))) & ((ushort)(65529))), _pat_let220_0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(_pat_let220_0, _52_dt__update_hext_h1 => TI59__Bus.Bus.create(_52_dt__update_hext_h1, (_51_dt__update__tmp_h13).dtor_irg, (_51_dt__update__tmp_h13).dtor_io, (_51_dt__update__tmp_h13).dtor_displayDigit, (_51_dt__update__tmp_h13).dtor_displayDpt, (_51_dt__update__tmp_h13).dtor_displaySegH, (_51_dt__update__tmp_h13).dtor_keyLine, (_51_dt__update__tmp_h13).dtor_dstate, (_51_dt__update__tmp_h13).dtor_idle, (_51_dt__update__tmp_h13).dtor_addr, (_51_dt__update__tmp_h13).dtor_cycleCount)))))) : (_47_bus2));
        TI59__Machine._IMachine _53_dt__update__tmp_h14 = m;
        TI59__Bus._IBus _54_dt__update_hbus_h1 = Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_50_bus3, _pat_let221_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let221_0, _55_dt__update__tmp_h15 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(TI59__Cpu.__default.Flg((_46_c7).dtor_flags, TI59__Nibbles.__default.FLG__IDLE), _pat_let222_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let222_0, _56_dt__update_hidle_h0 => TI59__Bus.Bus.create((_55_dt__update__tmp_h15).dtor_ext, (_55_dt__update__tmp_h15).dtor_irg, (_55_dt__update__tmp_h15).dtor_io, (_55_dt__update__tmp_h15).dtor_displayDigit, (_55_dt__update__tmp_h15).dtor_displayDpt, (_55_dt__update__tmp_h15).dtor_displaySegH, (_55_dt__update__tmp_h15).dtor_keyLine, (_55_dt__update__tmp_h15).dtor_dstate, _56_dt__update_hidle_h0, (_55_dt__update__tmp_h15).dtor_addr, (_55_dt__update__tmp_h15).dtor_cycleCount)))));
        TI59__Cpu._ICpu _57_dt__update_hcpu_h1 = _46_c7;
        return TI59__Machine.Machine.create(_57_dt__update_hcpu_h1, (_53_dt__update__tmp_h14).dtor_broms, (_53_dt__update__tmp_h14).dtor_scoms, (_53_dt__update__tmp_h14).dtor_rams, (_53_dt__update__tmp_h14).dtor_lib, (_53_dt__update__tmp_h14).dtor_keys, (_53_dt__update__tmp_h14).dtor_disp, _54_dt__update_hbus_h1, (_53_dt__update__tmp_h14).dtor_halted);
      } else if (((sstate) == (BigInteger.One)) && (write)) {
        TI59__Bus._IBus _58_bus1 = ((TI59__Cpu.__default.Flg((_2_c1).dtor_flags, TI59__Nibbles.__default.FLG__COND__LAST)) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let223_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let223_0, _59_dt__update__tmp_h16 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>((ushort)(((_1_bus).dtor_ext) | (TI59__Nibbles.__default.EXT__COND)), _pat_let224_0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(_pat_let224_0, _60_dt__update_hext_h2 => TI59__Bus.Bus.create(_60_dt__update_hext_h2, (_59_dt__update__tmp_h16).dtor_irg, (_59_dt__update__tmp_h16).dtor_io, (_59_dt__update__tmp_h16).dtor_displayDigit, (_59_dt__update__tmp_h16).dtor_displayDpt, (_59_dt__update__tmp_h16).dtor_displaySegH, (_59_dt__update__tmp_h16).dtor_keyLine, (_59_dt__update__tmp_h16).dtor_dstate, (_59_dt__update__tmp_h16).dtor_idle, (_59_dt__update__tmp_h16).dtor_addr, (_59_dt__update__tmp_h16).dtor_cycleCount)))))) : (_1_bus));
        TI59__Cpu._ICpu _61_c2 = ((TI59__Cpu.__default.Flg((_2_c1).dtor_flags, TI59__Nibbles.__default.FLG__COND__LAST)) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c1, _pat_let225_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let225_0, _62_dt__update__tmp_h17 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_2_c1).dtor_flags) & ((ushort)(61439))), _pat_let226_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let226_0, _63_dt__update_hflags_h4 => TI59__Cpu.Cpu.create((_62_dt__update__tmp_h17).dtor_A, (_62_dt__update__tmp_h17).dtor_B, (_62_dt__update__tmp_h17).dtor_C, (_62_dt__update__tmp_h17).dtor_D, (_62_dt__update__tmp_h17).dtor_E, (_62_dt__update__tmp_h17).dtor_KR, (_62_dt__update__tmp_h17).dtor_SR, (_62_dt__update__tmp_h17).dtor_fA, (_62_dt__update__tmp_h17).dtor_fB, (_62_dt__update__tmp_h17).dtor_R5, (_62_dt__update__tmp_h17).dtor_Sout, (_62_dt__update__tmp_h17).dtor_Sin, (_62_dt__update__tmp_h17).dtor_opcode, _63_dt__update_hflags_h4, (_62_dt__update__tmp_h17).dtor_addr, (_62_dt__update__tmp_h17).dtor_reset, (_62_dt__update__tmp_h17).dtor_zeroSuppr)))))) : (_2_c1));
        TI59__Machine._IMachine _64_dt__update__tmp_h18 = m;
        TI59__Bus._IBus _65_dt__update_hbus_h2 = Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_58_bus1, _pat_let227_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let227_0, _66_dt__update__tmp_h19 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(TI59__Cpu.__default.Flg((_61_c2).dtor_flags, TI59__Nibbles.__default.FLG__IDLE), _pat_let228_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let228_0, _67_dt__update_hidle_h1 => TI59__Bus.Bus.create((_66_dt__update__tmp_h19).dtor_ext, (_66_dt__update__tmp_h19).dtor_irg, (_66_dt__update__tmp_h19).dtor_io, (_66_dt__update__tmp_h19).dtor_displayDigit, (_66_dt__update__tmp_h19).dtor_displayDpt, (_66_dt__update__tmp_h19).dtor_displaySegH, (_66_dt__update__tmp_h19).dtor_keyLine, (_66_dt__update__tmp_h19).dtor_dstate, _67_dt__update_hidle_h1, (_66_dt__update__tmp_h19).dtor_addr, (_66_dt__update__tmp_h19).dtor_cycleCount)))));
        TI59__Cpu._ICpu _68_dt__update_hcpu_h2 = _61_c2;
        return TI59__Machine.Machine.create(_68_dt__update_hcpu_h2, (_64_dt__update__tmp_h18).dtor_broms, (_64_dt__update__tmp_h18).dtor_scoms, (_64_dt__update__tmp_h18).dtor_rams, (_64_dt__update__tmp_h18).dtor_lib, (_64_dt__update__tmp_h18).dtor_keys, (_64_dt__update__tmp_h18).dtor_disp, _65_dt__update_hbus_h2, (_64_dt__update__tmp_h18).dtor_halted);
      } else if (((sstate) == (new BigInteger(2))) && (write)) {
        TI59__Bus._IBus _69_bus1 = ((TI59__Cpu.__default.Flg((_2_c1).dtor_flags, TI59__Nibbles.__default.FLG__HOLD)) ? (Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let229_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let229_0, _70_dt__update__tmp_h20 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>((ushort)(((_1_bus).dtor_ext) | (TI59__Nibbles.__default.EXT__HOLD)), _pat_let230_0 => Dafny.Helpers.Let<ushort, TI59__Bus._IBus>(_pat_let230_0, _71_dt__update_hext_h3 => TI59__Bus.Bus.create(_71_dt__update_hext_h3, (_70_dt__update__tmp_h20).dtor_irg, (_70_dt__update__tmp_h20).dtor_io, (_70_dt__update__tmp_h20).dtor_displayDigit, (_70_dt__update__tmp_h20).dtor_displayDpt, (_70_dt__update__tmp_h20).dtor_displaySegH, (_70_dt__update__tmp_h20).dtor_keyLine, (_70_dt__update__tmp_h20).dtor_dstate, (_70_dt__update__tmp_h20).dtor_idle, (_70_dt__update__tmp_h20).dtor_addr, (_70_dt__update__tmp_h20).dtor_cycleCount)))))) : (_1_bus));
        TI59__Machine._IMachine _72_dt__update__tmp_h21 = m;
        TI59__Bus._IBus _73_dt__update_hbus_h3 = Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_69_bus1, _pat_let231_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let231_0, _74_dt__update__tmp_h22 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(TI59__Cpu.__default.Flg((_2_c1).dtor_flags, TI59__Nibbles.__default.FLG__IDLE), _pat_let232_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let232_0, _75_dt__update_hidle_h2 => TI59__Bus.Bus.create((_74_dt__update__tmp_h22).dtor_ext, (_74_dt__update__tmp_h22).dtor_irg, (_74_dt__update__tmp_h22).dtor_io, (_74_dt__update__tmp_h22).dtor_displayDigit, (_74_dt__update__tmp_h22).dtor_displayDpt, (_74_dt__update__tmp_h22).dtor_displaySegH, (_74_dt__update__tmp_h22).dtor_keyLine, (_74_dt__update__tmp_h22).dtor_dstate, _75_dt__update_hidle_h2, (_74_dt__update__tmp_h22).dtor_addr, (_74_dt__update__tmp_h22).dtor_cycleCount)))));
        TI59__Cpu._ICpu _76_dt__update_hcpu_h3 = _2_c1;
        return TI59__Machine.Machine.create(_76_dt__update_hcpu_h3, (_72_dt__update__tmp_h21).dtor_broms, (_72_dt__update__tmp_h21).dtor_scoms, (_72_dt__update__tmp_h21).dtor_rams, (_72_dt__update__tmp_h21).dtor_lib, (_72_dt__update__tmp_h21).dtor_keys, (_72_dt__update__tmp_h21).dtor_disp, _73_dt__update_hbus_h3, (_72_dt__update__tmp_h21).dtor_halted);
      } else if (((sstate) == (new BigInteger(2))) && (!(write))) {
        TI59__Cpu._ICpu _77_c2 = ((((ushort)(((_1_bus).dtor_ext) & (TI59__Nibbles.__default.EXT__HOLD))) == ((ushort)(0))) ? (Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c1, _pat_let233_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let233_0, _78_dt__update__tmp_h23 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((ushort)(((_2_c1).dtor_KR) & ((ushort)(65533))), _pat_let234_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let234_0, _79_dt__update_hKR_h0 => TI59__Cpu.Cpu.create((_78_dt__update__tmp_h23).dtor_A, (_78_dt__update__tmp_h23).dtor_B, (_78_dt__update__tmp_h23).dtor_C, (_78_dt__update__tmp_h23).dtor_D, (_78_dt__update__tmp_h23).dtor_E, _79_dt__update_hKR_h0, (_78_dt__update__tmp_h23).dtor_SR, (_78_dt__update__tmp_h23).dtor_fA, (_78_dt__update__tmp_h23).dtor_fB, (_78_dt__update__tmp_h23).dtor_R5, (_78_dt__update__tmp_h23).dtor_Sout, (_78_dt__update__tmp_h23).dtor_Sin, (_78_dt__update__tmp_h23).dtor_opcode, (_78_dt__update__tmp_h23).dtor_flags, (_78_dt__update__tmp_h23).dtor_addr, (_78_dt__update__tmp_h23).dtor_reset, (_78_dt__update__tmp_h23).dtor_zeroSuppr)))))) : (_2_c1));
        TI59__Machine._IMachine _80_dt__update__tmp_h24 = m;
        TI59__Bus._IBus _81_dt__update_hbus_h4 = Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let235_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let235_0, _82_dt__update__tmp_h25 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(TI59__Cpu.__default.Flg((_77_c2).dtor_flags, TI59__Nibbles.__default.FLG__IDLE), _pat_let236_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let236_0, _83_dt__update_hidle_h3 => TI59__Bus.Bus.create((_82_dt__update__tmp_h25).dtor_ext, (_82_dt__update__tmp_h25).dtor_irg, (_82_dt__update__tmp_h25).dtor_io, (_82_dt__update__tmp_h25).dtor_displayDigit, (_82_dt__update__tmp_h25).dtor_displayDpt, (_82_dt__update__tmp_h25).dtor_displaySegH, (_82_dt__update__tmp_h25).dtor_keyLine, (_82_dt__update__tmp_h25).dtor_dstate, _83_dt__update_hidle_h3, (_82_dt__update__tmp_h25).dtor_addr, (_82_dt__update__tmp_h25).dtor_cycleCount)))));
        TI59__Cpu._ICpu _84_dt__update_hcpu_h4 = _77_c2;
        return TI59__Machine.Machine.create(_84_dt__update_hcpu_h4, (_80_dt__update__tmp_h24).dtor_broms, (_80_dt__update__tmp_h24).dtor_scoms, (_80_dt__update__tmp_h24).dtor_rams, (_80_dt__update__tmp_h24).dtor_lib, (_80_dt__update__tmp_h24).dtor_keys, (_80_dt__update__tmp_h24).dtor_disp, _81_dt__update_hbus_h4, (_80_dt__update__tmp_h24).dtor_halted);
      } else if (((sstate) == (new BigInteger(14))) && (write)) {
        TI59__Machine._IMachine _85_dt__update__tmp_h26 = m;
        TI59__Bus._IBus _86_dt__update_hbus_h5 = Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let237_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let237_0, _87_dt__update__tmp_h27 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(TI59__Cpu.__default.Flg((_2_c1).dtor_flags, TI59__Nibbles.__default.FLG__IDLE), _pat_let238_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let238_0, _88_dt__update_hidle_h4 => Dafny.Helpers.Let<Dafny.Rune, TI59__Bus._IBus>(new Dafny.Rune(' '), _pat_let239_0 => Dafny.Helpers.Let<Dafny.Rune, TI59__Bus._IBus>(_pat_let239_0, _89_dt__update_hdisplayDigit_h1 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(false, _pat_let240_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let240_0, _90_dt__update_hdisplaySegH_h2 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(false, _pat_let241_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let241_0, _91_dt__update_hdisplayDpt_h1 => TI59__Bus.Bus.create((_87_dt__update__tmp_h27).dtor_ext, (_87_dt__update__tmp_h27).dtor_irg, (_87_dt__update__tmp_h27).dtor_io, _89_dt__update_hdisplayDigit_h1, _91_dt__update_hdisplayDpt_h1, _90_dt__update_hdisplaySegH_h2, (_87_dt__update__tmp_h27).dtor_keyLine, (_87_dt__update__tmp_h27).dtor_dstate, _88_dt__update_hidle_h4, (_87_dt__update__tmp_h27).dtor_addr, (_87_dt__update__tmp_h27).dtor_cycleCount)))))))))));
        TI59__Cpu._ICpu _92_dt__update_hcpu_h5 = _2_c1;
        return TI59__Machine.Machine.create(_92_dt__update_hcpu_h5, (_85_dt__update__tmp_h26).dtor_broms, (_85_dt__update__tmp_h26).dtor_scoms, (_85_dt__update__tmp_h26).dtor_rams, (_85_dt__update__tmp_h26).dtor_lib, (_85_dt__update__tmp_h26).dtor_keys, (_85_dt__update__tmp_h26).dtor_disp, _86_dt__update_hbus_h5, (_85_dt__update__tmp_h26).dtor_halted);
      } else if (((sstate) == (new BigInteger(15))) && (!(write))) {
        _System._ITuple2<bool, bool> _93_re = TI59__Cpu.__default.RunEarly((_2_c1).dtor_opcode);
        TI59__Cpu._ICpu _94_c2 = ((!((_93_re).dtor__0)) ? (TI59__Cpu.__default.Execute(Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_2_c1, _pat_let242_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let242_0, _95_dt__update__tmp_h28 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>((_1_bus).dtor_io, _pat_let243_0 => Dafny.Helpers.Let<Dafny.ISequence<byte>, TI59__Cpu._ICpu>(_pat_let243_0, _96_dt__update_hSin_h1 => TI59__Cpu.Cpu.create((_95_dt__update__tmp_h28).dtor_A, (_95_dt__update__tmp_h28).dtor_B, (_95_dt__update__tmp_h28).dtor_C, (_95_dt__update__tmp_h28).dtor_D, (_95_dt__update__tmp_h28).dtor_E, (_95_dt__update__tmp_h28).dtor_KR, (_95_dt__update__tmp_h28).dtor_SR, (_95_dt__update__tmp_h28).dtor_fA, (_95_dt__update__tmp_h28).dtor_fB, (_95_dt__update__tmp_h28).dtor_R5, (_95_dt__update__tmp_h28).dtor_Sout, _96_dt__update_hSin_h1, (_95_dt__update__tmp_h28).dtor_opcode, (_95_dt__update__tmp_h28).dtor_flags, (_95_dt__update__tmp_h28).dtor_addr, (_95_dt__update__tmp_h28).dtor_reset, (_95_dt__update__tmp_h28).dtor_zeroSuppr))))), (_2_c1).dtor_opcode, (_1_bus).dtor_dstate, (_1_bus).dtor_ext, (_1_bus).dtor_keyLine)) : (_2_c1));
        TI59__Cpu._ICpu _97_c3 = Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_94_c2, _pat_let244_0 => Dafny.Helpers.Let<TI59__Cpu._ICpu, TI59__Cpu._ICpu>(_pat_let244_0, _98_dt__update__tmp_h29 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>((_1_bus).dtor_addr, _pat_let245_0 => Dafny.Helpers.Let<BigInteger, TI59__Cpu._ICpu>(_pat_let245_0, _99_dt__update_haddr_h1 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>((_1_bus).dtor_irg, _pat_let246_0 => Dafny.Helpers.Let<ushort, TI59__Cpu._ICpu>(_pat_let246_0, _100_dt__update_hopcode_h1 => TI59__Cpu.Cpu.create((_98_dt__update__tmp_h29).dtor_A, (_98_dt__update__tmp_h29).dtor_B, (_98_dt__update__tmp_h29).dtor_C, (_98_dt__update__tmp_h29).dtor_D, (_98_dt__update__tmp_h29).dtor_E, (_98_dt__update__tmp_h29).dtor_KR, (_98_dt__update__tmp_h29).dtor_SR, (_98_dt__update__tmp_h29).dtor_fA, (_98_dt__update__tmp_h29).dtor_fB, (_98_dt__update__tmp_h29).dtor_R5, (_98_dt__update__tmp_h29).dtor_Sout, (_98_dt__update__tmp_h29).dtor_Sin, _100_dt__update_hopcode_h1, (_98_dt__update__tmp_h29).dtor_flags, _99_dt__update_haddr_h1, (_98_dt__update__tmp_h29).dtor_reset, (_98_dt__update__tmp_h29).dtor_zeroSuppr)))))));
        TI59__Machine._IMachine _101_dt__update__tmp_h30 = m;
        bool _102_dt__update_hhalted_h1 = ((m).dtor_halted) || (((_1_bus).dtor_addr) == (new BigInteger(-1)));
        TI59__Bus._IBus _103_dt__update_hbus_h6 = Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let247_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let247_0, _104_dt__update__tmp_h31 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(TI59__Cpu.__default.Flg((_97_c3).dtor_flags, TI59__Nibbles.__default.FLG__IDLE), _pat_let248_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let248_0, _105_dt__update_hidle_h5 => TI59__Bus.Bus.create((_104_dt__update__tmp_h31).dtor_ext, (_104_dt__update__tmp_h31).dtor_irg, (_104_dt__update__tmp_h31).dtor_io, (_104_dt__update__tmp_h31).dtor_displayDigit, (_104_dt__update__tmp_h31).dtor_displayDpt, (_104_dt__update__tmp_h31).dtor_displaySegH, (_104_dt__update__tmp_h31).dtor_keyLine, (_104_dt__update__tmp_h31).dtor_dstate, _105_dt__update_hidle_h5, (_104_dt__update__tmp_h31).dtor_addr, (_104_dt__update__tmp_h31).dtor_cycleCount)))));
        TI59__Cpu._ICpu _106_dt__update_hcpu_h6 = _97_c3;
        return TI59__Machine.Machine.create(_106_dt__update_hcpu_h6, (_101_dt__update__tmp_h30).dtor_broms, (_101_dt__update__tmp_h30).dtor_scoms, (_101_dt__update__tmp_h30).dtor_rams, (_101_dt__update__tmp_h30).dtor_lib, (_101_dt__update__tmp_h30).dtor_keys, (_101_dt__update__tmp_h30).dtor_disp, _103_dt__update_hbus_h6, _102_dt__update_hhalted_h1);
      } else {
        TI59__Machine._IMachine _107_dt__update__tmp_h32 = m;
        TI59__Bus._IBus _108_dt__update_hbus_h7 = Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_1_bus, _pat_let249_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Bus._IBus>(_pat_let249_0, _109_dt__update__tmp_h33 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(TI59__Cpu.__default.Flg((_2_c1).dtor_flags, TI59__Nibbles.__default.FLG__IDLE), _pat_let250_0 => Dafny.Helpers.Let<bool, TI59__Bus._IBus>(_pat_let250_0, _110_dt__update_hidle_h6 => TI59__Bus.Bus.create((_109_dt__update__tmp_h33).dtor_ext, (_109_dt__update__tmp_h33).dtor_irg, (_109_dt__update__tmp_h33).dtor_io, (_109_dt__update__tmp_h33).dtor_displayDigit, (_109_dt__update__tmp_h33).dtor_displayDpt, (_109_dt__update__tmp_h33).dtor_displaySegH, (_109_dt__update__tmp_h33).dtor_keyLine, (_109_dt__update__tmp_h33).dtor_dstate, _110_dt__update_hidle_h6, (_109_dt__update__tmp_h33).dtor_addr, (_109_dt__update__tmp_h33).dtor_cycleCount)))));
        TI59__Cpu._ICpu _111_dt__update_hcpu_h7 = _2_c1;
        return TI59__Machine.Machine.create(_111_dt__update_hcpu_h7, (_107_dt__update__tmp_h32).dtor_broms, (_107_dt__update__tmp_h32).dtor_scoms, (_107_dt__update__tmp_h32).dtor_rams, (_107_dt__update__tmp_h32).dtor_lib, (_107_dt__update__tmp_h32).dtor_keys, (_107_dt__update__tmp_h32).dtor_disp, _108_dt__update_hbus_h7, (_107_dt__update__tmp_h32).dtor_halted);
      }
    }
    public static TI59__Machine._IMachine BromsFold(TI59__Machine._IMachine m, BigInteger i, BigInteger sstate, bool write)
    {
    TAIL_CALL_START: ;
      var _pat_let_tv0 = m;
      var _pat_let_tv1 = i;
      if ((i) == (new BigInteger(3))) {
        return m;
      } else {
        _System._ITuple2<TI59__Chips._IBrom, TI59__Bus._IBus> _let_tmp_rhs0 = ((write) ? (TI59__Chips.__default.BromStep(((m).dtor_broms).Select(i), (m).dtor_bus, sstate, true)) : (TI59__Chips.__default.BromStepRead(((m).dtor_broms).Select(i), (m).dtor_bus, sstate, false)));
        TI59__Chips._IBrom _0_b = _let_tmp_rhs0.dtor__0;
        TI59__Bus._IBus _1_bus1 = _let_tmp_rhs0.dtor__1;
        TI59__Machine._IMachine _in0 = Dafny.Helpers.Let<TI59__Machine._IMachine, TI59__Machine._IMachine>(m, _pat_let251_0 => Dafny.Helpers.Let<TI59__Machine._IMachine, TI59__Machine._IMachine>(_pat_let251_0, _2_dt__update__tmp_h0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Machine._IMachine>(_1_bus1, _pat_let252_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Machine._IMachine>(_pat_let252_0, _3_dt__update_hbus_h0 => Dafny.Helpers.Let<Dafny.ISequence<TI59__Chips._IBrom>, TI59__Machine._IMachine>(Dafny.Sequence<TI59__Chips._IBrom>.Update((_pat_let_tv0).dtor_broms, _pat_let_tv1, _0_b), _pat_let253_0 => Dafny.Helpers.Let<Dafny.ISequence<TI59__Chips._IBrom>, TI59__Machine._IMachine>(_pat_let253_0, _4_dt__update_hbroms_h0 => TI59__Machine.Machine.create((_2_dt__update__tmp_h0).dtor_cpu, _4_dt__update_hbroms_h0, (_2_dt__update__tmp_h0).dtor_scoms, (_2_dt__update__tmp_h0).dtor_rams, (_2_dt__update__tmp_h0).dtor_lib, (_2_dt__update__tmp_h0).dtor_keys, (_2_dt__update__tmp_h0).dtor_disp, _3_dt__update_hbus_h0, (_2_dt__update__tmp_h0).dtor_halted)))))));
        BigInteger _in1 = (i) + (BigInteger.One);
        BigInteger _in2 = sstate;
        bool _in3 = write;
        m = _in0;
        i = _in1;
        sstate = _in2;
        write = _in3;
        goto TAIL_CALL_START;
      }
    }
    public static TI59__Machine._IMachine ScomsFold(TI59__Machine._IMachine m, BigInteger i, BigInteger sstate, bool write)
    {
    TAIL_CALL_START: ;
      var _pat_let_tv0 = sstate;
      var _pat_let_tv1 = sstate;
      var _pat_let_tv2 = m;
      var _pat_let_tv3 = i;
      if ((i) == (new BigInteger(2))) {
        return m;
      } else {
        TI59__Chips._IScom _0_s = ((m).dtor_scoms).Select(i);
        _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus> _let_tmp_rhs0 = ((write) ? (Dafny.Helpers.Let<_System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(TI59__Chips.__default.ScomConstStepW(_0_s, (m).dtor_bus, sstate, true), _pat_let254_0 => Dafny.Helpers.Let<TI59__Chips._IScom, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(_pat_let254_0.dtor__0, _1_sa => Dafny.Helpers.Let<TI59__Bus._IBus, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(_pat_let254_0.dtor__1, _2_busA => Dafny.Helpers.Let<_System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(TI59__Chips.__default.ScomRegStepW(_1_sa, _2_busA, _pat_let_tv0, true), _pat_let255_0 => Dafny.Helpers.Let<TI59__Chips._IScom, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(_pat_let255_0.dtor__0, _3_sb => Dafny.Helpers.Let<TI59__Bus._IBus, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(_pat_let255_0.dtor__1, _4_busB => _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(_3_sb, _4_busB)))))))) : (Dafny.Helpers.Let<_System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(TI59__Chips.__default.ScomConstStepR(_0_s, (m).dtor_bus, sstate, false), _pat_let256_0 => Dafny.Helpers.Let<TI59__Chips._IScom, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(_pat_let256_0.dtor__0, _5_sa => Dafny.Helpers.Let<TI59__Bus._IBus, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(_pat_let256_0.dtor__1, _6_busA => Dafny.Helpers.Let<_System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(TI59__Chips.__default.ScomRegStepR(_5_sa, _6_busA, _pat_let_tv1, false), _pat_let257_0 => Dafny.Helpers.Let<TI59__Chips._IScom, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(_pat_let257_0.dtor__0, _7_sb => Dafny.Helpers.Let<TI59__Bus._IBus, _System._ITuple2<TI59__Chips._IScom, TI59__Bus._IBus>>(_pat_let257_0.dtor__1, _8_busB => _System.Tuple2<TI59__Chips._IScom, TI59__Bus._IBus>.create(_7_sb, _8_busB)))))))));
        TI59__Chips._IScom _9_s1 = _let_tmp_rhs0.dtor__0;
        TI59__Bus._IBus _10_bus1 = _let_tmp_rhs0.dtor__1;
        TI59__Machine._IMachine _in0 = Dafny.Helpers.Let<TI59__Machine._IMachine, TI59__Machine._IMachine>(m, _pat_let258_0 => Dafny.Helpers.Let<TI59__Machine._IMachine, TI59__Machine._IMachine>(_pat_let258_0, _11_dt__update__tmp_h0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Machine._IMachine>(_10_bus1, _pat_let259_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Machine._IMachine>(_pat_let259_0, _12_dt__update_hbus_h0 => Dafny.Helpers.Let<Dafny.ISequence<TI59__Chips._IScom>, TI59__Machine._IMachine>(Dafny.Sequence<TI59__Chips._IScom>.Update((_pat_let_tv2).dtor_scoms, _pat_let_tv3, _9_s1), _pat_let260_0 => Dafny.Helpers.Let<Dafny.ISequence<TI59__Chips._IScom>, TI59__Machine._IMachine>(_pat_let260_0, _13_dt__update_hscoms_h0 => TI59__Machine.Machine.create((_11_dt__update__tmp_h0).dtor_cpu, (_11_dt__update__tmp_h0).dtor_broms, _13_dt__update_hscoms_h0, (_11_dt__update__tmp_h0).dtor_rams, (_11_dt__update__tmp_h0).dtor_lib, (_11_dt__update__tmp_h0).dtor_keys, (_11_dt__update__tmp_h0).dtor_disp, _12_dt__update_hbus_h0, (_11_dt__update__tmp_h0).dtor_halted)))))));
        BigInteger _in1 = (i) + (BigInteger.One);
        BigInteger _in2 = sstate;
        bool _in3 = write;
        m = _in0;
        i = _in1;
        sstate = _in2;
        write = _in3;
        goto TAIL_CALL_START;
      }
    }
    public static TI59__Machine._IMachine RamsFold(TI59__Machine._IMachine m, BigInteger i, BigInteger sstate, bool write)
    {
    TAIL_CALL_START: ;
      var _pat_let_tv0 = m;
      var _pat_let_tv1 = i;
      if ((i) == (new BigInteger(4))) {
        return m;
      } else {
        _System._ITuple2<TI59__Chips._IRam, TI59__Bus._IBus> _let_tmp_rhs0 = ((write) ? (TI59__Chips.__default.RamStepW(((m).dtor_rams).Select(i), (m).dtor_bus, sstate, true)) : (TI59__Chips.__default.RamStepR(((m).dtor_rams).Select(i), (m).dtor_bus, sstate, false)));
        TI59__Chips._IRam _0_r = _let_tmp_rhs0.dtor__0;
        TI59__Bus._IBus _1_bus1 = _let_tmp_rhs0.dtor__1;
        TI59__Machine._IMachine _in0 = Dafny.Helpers.Let<TI59__Machine._IMachine, TI59__Machine._IMachine>(m, _pat_let261_0 => Dafny.Helpers.Let<TI59__Machine._IMachine, TI59__Machine._IMachine>(_pat_let261_0, _2_dt__update__tmp_h0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Machine._IMachine>(_1_bus1, _pat_let262_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Machine._IMachine>(_pat_let262_0, _3_dt__update_hbus_h0 => Dafny.Helpers.Let<Dafny.ISequence<TI59__Chips._IRam>, TI59__Machine._IMachine>(Dafny.Sequence<TI59__Chips._IRam>.Update((_pat_let_tv0).dtor_rams, _pat_let_tv1, _0_r), _pat_let263_0 => Dafny.Helpers.Let<Dafny.ISequence<TI59__Chips._IRam>, TI59__Machine._IMachine>(_pat_let263_0, _4_dt__update_hrams_h0 => TI59__Machine.Machine.create((_2_dt__update__tmp_h0).dtor_cpu, (_2_dt__update__tmp_h0).dtor_broms, (_2_dt__update__tmp_h0).dtor_scoms, _4_dt__update_hrams_h0, (_2_dt__update__tmp_h0).dtor_lib, (_2_dt__update__tmp_h0).dtor_keys, (_2_dt__update__tmp_h0).dtor_disp, _3_dt__update_hbus_h0, (_2_dt__update__tmp_h0).dtor_halted)))))));
        BigInteger _in1 = (i) + (BigInteger.One);
        BigInteger _in2 = sstate;
        bool _in3 = write;
        m = _in0;
        i = _in1;
        sstate = _in2;
        write = _in3;
        goto TAIL_CALL_START;
      }
    }
    public static TI59__Machine._IMachine LibChip(TI59__Machine._IMachine m, BigInteger sstate, bool write)
    {
      _System._ITuple2<TI59__Chips._ILib, TI59__Bus._IBus> _let_tmp_rhs0 = ((write) ? (TI59__Chips.__default.LibStepW((m).dtor_lib, (m).dtor_bus, sstate, true)) : (TI59__Chips.__default.LibStepR((m).dtor_lib, (m).dtor_bus, sstate, false)));
      TI59__Chips._ILib _0_l = _let_tmp_rhs0.dtor__0;
      TI59__Bus._IBus _1_bus1 = _let_tmp_rhs0.dtor__1;
      TI59__Machine._IMachine _2_dt__update__tmp_h0 = m;
      TI59__Bus._IBus _3_dt__update_hbus_h0 = _1_bus1;
      TI59__Chips._ILib _4_dt__update_hlib_h0 = _0_l;
      return TI59__Machine.Machine.create((_2_dt__update__tmp_h0).dtor_cpu, (_2_dt__update__tmp_h0).dtor_broms, (_2_dt__update__tmp_h0).dtor_scoms, (_2_dt__update__tmp_h0).dtor_rams, _4_dt__update_hlib_h0, (_2_dt__update__tmp_h0).dtor_keys, (_2_dt__update__tmp_h0).dtor_disp, _3_dt__update_hbus_h0, (_2_dt__update__tmp_h0).dtor_halted);
    }
    public static TI59__Machine._IMachine DispChip(TI59__Machine._IMachine m, BigInteger sstate, bool write)
    {
      _System._ITuple2<TI59__Chips._IDisp, TI59__Bus._IBus> _let_tmp_rhs0 = TI59__Chips.__default.DispStep((m).dtor_disp, (m).dtor_bus, sstate, write);
      TI59__Chips._IDisp _0_d = _let_tmp_rhs0.dtor__0;
      TI59__Bus._IBus _1_bus1 = _let_tmp_rhs0.dtor__1;
      TI59__Machine._IMachine _2_dt__update__tmp_h0 = m;
      TI59__Bus._IBus _3_dt__update_hbus_h0 = _1_bus1;
      TI59__Chips._IDisp _4_dt__update_hdisp_h0 = _0_d;
      return TI59__Machine.Machine.create((_2_dt__update__tmp_h0).dtor_cpu, (_2_dt__update__tmp_h0).dtor_broms, (_2_dt__update__tmp_h0).dtor_scoms, (_2_dt__update__tmp_h0).dtor_rams, (_2_dt__update__tmp_h0).dtor_lib, (_2_dt__update__tmp_h0).dtor_keys, _4_dt__update_hdisp_h0, _3_dt__update_hbus_h0, (_2_dt__update__tmp_h0).dtor_halted);
    }
    public static TI59__Machine._IMachine KeyChip(TI59__Machine._IMachine m, BigInteger sstate, bool write)
    {
      _System._ITuple2<TI59__Chips._IKeyState, TI59__Bus._IBus> _let_tmp_rhs0 = TI59__Chips.__default.KeyStep((m).dtor_keys, (m).dtor_bus, sstate, write);
      TI59__Chips._IKeyState _0_k = _let_tmp_rhs0.dtor__0;
      TI59__Bus._IBus _1_bus1 = _let_tmp_rhs0.dtor__1;
      TI59__Machine._IMachine _2_dt__update__tmp_h0 = m;
      TI59__Bus._IBus _3_dt__update_hbus_h0 = _1_bus1;
      TI59__Chips._IKeyState _4_dt__update_hkeys_h0 = _0_k;
      return TI59__Machine.Machine.create((_2_dt__update__tmp_h0).dtor_cpu, (_2_dt__update__tmp_h0).dtor_broms, (_2_dt__update__tmp_h0).dtor_scoms, (_2_dt__update__tmp_h0).dtor_rams, (_2_dt__update__tmp_h0).dtor_lib, _4_dt__update_hkeys_h0, (_2_dt__update__tmp_h0).dtor_disp, _3_dt__update_hbus_h0, (_2_dt__update__tmp_h0).dtor_halted);
    }
    public static TI59__Machine._IMachine PhaseFold(TI59__Machine._IMachine m, BigInteger sstate, bool write)
    {
      TI59__Machine._IMachine _0_m1 = TI59__Machine.__default.AluChip(m, sstate, write);
      TI59__Machine._IMachine _1_m2 = TI59__Machine.__default.BromsFold(_0_m1, BigInteger.Zero, sstate, write);
      TI59__Machine._IMachine _2_m3 = TI59__Machine.__default.ScomsFold(_1_m2, BigInteger.Zero, sstate, write);
      TI59__Machine._IMachine _3_m4 = TI59__Machine.__default.RamsFold(_2_m3, BigInteger.Zero, sstate, write);
      TI59__Machine._IMachine _4_m5 = TI59__Machine.__default.LibChip(_3_m4, sstate, write);
      TI59__Machine._IMachine _5_m6 = TI59__Machine.__default.DispChip(_4_m5, sstate, write);
      return TI59__Machine.__default.KeyChip(_5_m6, sstate, write);
    }
    public static TI59__Machine._IMachine SStateFold(TI59__Machine._IMachine m, BigInteger s)
    {
      TI59__Machine._IMachine _0_m1 = TI59__Machine.__default.PhaseFold(m, s, true);
      TI59__Machine._IMachine _1_m2 = TI59__Machine.__default.PhaseFold(_0_m1, s, false);
      if ((s) == (new BigInteger(14))) {
        TI59__Machine._IMachine _2_dt__update__tmp_h0 = _1_m2;
        TI59__Bus._IBus _3_dt__update_hbus_h0 = TI59__Bus.__default.BusAfterS14((_1_m2).dtor_bus);
        return TI59__Machine.Machine.create((_2_dt__update__tmp_h0).dtor_cpu, (_2_dt__update__tmp_h0).dtor_broms, (_2_dt__update__tmp_h0).dtor_scoms, (_2_dt__update__tmp_h0).dtor_rams, (_2_dt__update__tmp_h0).dtor_lib, (_2_dt__update__tmp_h0).dtor_keys, (_2_dt__update__tmp_h0).dtor_disp, _3_dt__update_hbus_h0, (_2_dt__update__tmp_h0).dtor_halted);
      } else {
        return _1_m2;
      }
    }
    public static TI59__Machine._IMachine CycleFold(TI59__Machine._IMachine m, BigInteger s)
    {
    TAIL_CALL_START: ;
      if ((s) == (new BigInteger(16))) {
        return m;
      } else {
        TI59__Machine._IMachine _in0 = TI59__Machine.__default.SStateFold(m, s);
        BigInteger _in1 = (s) + (BigInteger.One);
        m = _in0;
        s = _in1;
        goto TAIL_CALL_START;
      }
    }
    public static TI59__Machine._ITraceRec MkTrace(TI59__Machine._IMachine m) {
      return TI59__Machine.TraceRec.create(((m).dtor_cpu).dtor_addr, ((m).dtor_cpu).dtor_opcode, ((m).dtor_cpu).dtor_A, ((m).dtor_cpu).dtor_B, ((m).dtor_cpu).dtor_C, ((m).dtor_cpu).dtor_D, ((m).dtor_cpu).dtor_E, ((m).dtor_cpu).dtor_fA, ((m).dtor_cpu).dtor_fB, ((m).dtor_cpu).dtor_KR, ((m).dtor_cpu).dtor_SR, ((m).dtor_cpu).dtor_R5, (ushort)(0), TI59__Cpu.__default.Flg(((m).dtor_cpu).dtor_flags, TI59__Nibbles.__default.FLG__COND), TI59__Cpu.__default.Flg(((m).dtor_cpu).dtor_flags, TI59__Nibbles.__default.FLG__IDLE), TI59__Cpu.__default.Flg(((m).dtor_cpu).dtor_flags, TI59__Nibbles.__default.FLG__HOLD), ((m).dtor_cpu).dtor_Sin, ((m).dtor_cpu).dtor_Sout, ((m).dtor_bus).dtor_dstate);
    }
    public static _System._ITuple2<TI59__Machine._IMachine, TI59__Machine._IOptRec> StepInstruction(TI59__Machine._IMachine m) {
      var _pat_let_tv0 = m;
      if ((m).dtor_halted) {
        return _System.Tuple2<TI59__Machine._IMachine, TI59__Machine._IOptRec>.create(m, TI59__Machine.OptRec.create_NoRec());
      } else {
        TI59__Machine._IOptRec _0_rec = (((((m).dtor_cpu).dtor_reset).Sign == 0) ? (TI59__Machine.OptRec.create_YesRec(TI59__Machine.__default.MkTrace(m))) : (TI59__Machine.OptRec.create_NoRec()));
        TI59__Machine._IMachine _1_m1 = Dafny.Helpers.Let<TI59__Machine._IMachine, TI59__Machine._IMachine>(m, _pat_let264_0 => Dafny.Helpers.Let<TI59__Machine._IMachine, TI59__Machine._IMachine>(_pat_let264_0, _2_dt__update__tmp_h0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Machine._IMachine>(TI59__Bus.__default.BusCycleStart((_pat_let_tv0).dtor_bus), _pat_let265_0 => Dafny.Helpers.Let<TI59__Bus._IBus, TI59__Machine._IMachine>(_pat_let265_0, _3_dt__update_hbus_h0 => TI59__Machine.Machine.create((_2_dt__update__tmp_h0).dtor_cpu, (_2_dt__update__tmp_h0).dtor_broms, (_2_dt__update__tmp_h0).dtor_scoms, (_2_dt__update__tmp_h0).dtor_rams, (_2_dt__update__tmp_h0).dtor_lib, (_2_dt__update__tmp_h0).dtor_keys, (_2_dt__update__tmp_h0).dtor_disp, _3_dt__update_hbus_h0, (_2_dt__update__tmp_h0).dtor_halted)))));
        TI59__Machine._IMachine _4_m2 = TI59__Machine.__default.CycleFold(_1_m1, BigInteger.Zero);
        return _System.Tuple2<TI59__Machine._IMachine, TI59__Machine._IOptRec>.create(_4_m2, _0_rec);
      }
    }
    public static TI59__Machine._IMachine Run(TI59__Machine._IMachine m, BigInteger budget)
    {
    TAIL_CALL_START: ;
      if (((budget).Sign == 0) || ((m).dtor_halted)) {
        return m;
      } else {
        TI59__Machine._IMachine _in0 = (TI59__Machine.__default.StepInstruction(m)).dtor__0;
        BigInteger _in1 = (budget) - (BigInteger.One);
        m = _in0;
        budget = _in1;
        goto TAIL_CALL_START;
      }
    }
  }

  public interface _IOptRec {
    bool is_NoRec { get; }
    bool is_YesRec { get; }
    TI59__Machine._ITraceRec dtor_rec { get; }
    _IOptRec DowncastClone();
  }
  public abstract class OptRec : _IOptRec {
    public OptRec() {
    }
    private static readonly TI59__Machine._IOptRec theDefault = create_NoRec();
    public static TI59__Machine._IOptRec Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Machine._IOptRec> _TYPE = new Dafny.TypeDescriptor<TI59__Machine._IOptRec>(TI59__Machine.OptRec.Default());
    public static Dafny.TypeDescriptor<TI59__Machine._IOptRec> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IOptRec create_NoRec() {
      return new OptRec_NoRec();
    }
    public static _IOptRec create_YesRec(TI59__Machine._ITraceRec rec) {
      return new OptRec_YesRec(rec);
    }
    public bool is_NoRec { get { return this is OptRec_NoRec; } }
    public bool is_YesRec { get { return this is OptRec_YesRec; } }
    public TI59__Machine._ITraceRec dtor_rec {
      get {
        var d = this;
        return ((OptRec_YesRec)d)._rec;
      }
    }
    public abstract _IOptRec DowncastClone();
  }
  public class OptRec_NoRec : OptRec {
    public OptRec_NoRec() : base() {
    }
    public override _IOptRec DowncastClone() {
      if (this is _IOptRec dt) { return dt; }
      return new OptRec_NoRec();
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Machine.OptRec_NoRec;
      return oth != null;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Machine.OptRec.NoRec";
      return s;
    }
  }
  public class OptRec_YesRec : OptRec {
    public readonly TI59__Machine._ITraceRec _rec;
    public OptRec_YesRec(TI59__Machine._ITraceRec rec) : base() {
      this._rec = rec;
    }
    public override _IOptRec DowncastClone() {
      if (this is _IOptRec dt) { return dt; }
      return new OptRec_YesRec(_rec);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Machine.OptRec_YesRec;
      return oth != null && object.Equals(this._rec, oth._rec);
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 1;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._rec));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Machine.OptRec.YesRec";
      s += "(";
      s += Dafny.Helpers.ToString(this._rec);
      s += ")";
      return s;
    }
  }

  public interface _IMachine {
    bool is_Machine { get; }
    TI59__Cpu._ICpu dtor_cpu { get; }
    Dafny.ISequence<TI59__Chips._IBrom> dtor_broms { get; }
    Dafny.ISequence<TI59__Chips._IScom> dtor_scoms { get; }
    Dafny.ISequence<TI59__Chips._IRam> dtor_rams { get; }
    TI59__Chips._ILib dtor_lib { get; }
    TI59__Chips._IKeyState dtor_keys { get; }
    TI59__Chips._IDisp dtor_disp { get; }
    TI59__Bus._IBus dtor_bus { get; }
    bool dtor_halted { get; }
    _IMachine DowncastClone();
  }
  public class Machine : _IMachine {
    public readonly TI59__Cpu._ICpu _cpu;
    public readonly Dafny.ISequence<TI59__Chips._IBrom> _broms;
    public readonly Dafny.ISequence<TI59__Chips._IScom> _scoms;
    public readonly Dafny.ISequence<TI59__Chips._IRam> _rams;
    public readonly TI59__Chips._ILib _lib;
    public readonly TI59__Chips._IKeyState _keys;
    public readonly TI59__Chips._IDisp _disp;
    public readonly TI59__Bus._IBus _bus;
    public readonly bool _halted;
    public Machine(TI59__Cpu._ICpu cpu, Dafny.ISequence<TI59__Chips._IBrom> broms, Dafny.ISequence<TI59__Chips._IScom> scoms, Dafny.ISequence<TI59__Chips._IRam> rams, TI59__Chips._ILib lib, TI59__Chips._IKeyState keys, TI59__Chips._IDisp disp, TI59__Bus._IBus bus, bool halted) {
      this._cpu = cpu;
      this._broms = broms;
      this._scoms = scoms;
      this._rams = rams;
      this._lib = lib;
      this._keys = keys;
      this._disp = disp;
      this._bus = bus;
      this._halted = halted;
    }
    public _IMachine DowncastClone() {
      if (this is _IMachine dt) { return dt; }
      return new Machine(_cpu, _broms, _scoms, _rams, _lib, _keys, _disp, _bus, _halted);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Machine.Machine;
      return oth != null && object.Equals(this._cpu, oth._cpu) && object.Equals(this._broms, oth._broms) && object.Equals(this._scoms, oth._scoms) && object.Equals(this._rams, oth._rams) && object.Equals(this._lib, oth._lib) && object.Equals(this._keys, oth._keys) && object.Equals(this._disp, oth._disp) && object.Equals(this._bus, oth._bus) && this._halted == oth._halted;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cpu));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._broms));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._scoms));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._rams));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._lib));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._keys));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._disp));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._bus));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._halted));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Machine.Machine.Machine";
      s += "(";
      s += Dafny.Helpers.ToString(this._cpu);
      s += ", ";
      s += Dafny.Helpers.ToString(this._broms);
      s += ", ";
      s += Dafny.Helpers.ToString(this._scoms);
      s += ", ";
      s += Dafny.Helpers.ToString(this._rams);
      s += ", ";
      s += Dafny.Helpers.ToString(this._lib);
      s += ", ";
      s += Dafny.Helpers.ToString(this._keys);
      s += ", ";
      s += Dafny.Helpers.ToString(this._disp);
      s += ", ";
      s += Dafny.Helpers.ToString(this._bus);
      s += ", ";
      s += Dafny.Helpers.ToString(this._halted);
      s += ")";
      return s;
    }
    private static readonly TI59__Machine._IMachine theDefault = create(TI59__Cpu.Cpu.Default(), Dafny.Sequence<TI59__Chips._IBrom>.Empty, Dafny.Sequence<TI59__Chips._IScom>.Empty, Dafny.Sequence<TI59__Chips._IRam>.Empty, TI59__Chips.Lib.Default(), TI59__Chips.KeyState.Default(), TI59__Chips.Disp.Default(), TI59__Bus.Bus.Default(), false);
    public static TI59__Machine._IMachine Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Machine._IMachine> _TYPE = new Dafny.TypeDescriptor<TI59__Machine._IMachine>(TI59__Machine.Machine.Default());
    public static Dafny.TypeDescriptor<TI59__Machine._IMachine> _TypeDescriptor() {
      return _TYPE;
    }
    public static _IMachine create(TI59__Cpu._ICpu cpu, Dafny.ISequence<TI59__Chips._IBrom> broms, Dafny.ISequence<TI59__Chips._IScom> scoms, Dafny.ISequence<TI59__Chips._IRam> rams, TI59__Chips._ILib lib, TI59__Chips._IKeyState keys, TI59__Chips._IDisp disp, TI59__Bus._IBus bus, bool halted) {
      return new Machine(cpu, broms, scoms, rams, lib, keys, disp, bus, halted);
    }
    public static _IMachine create_Machine(TI59__Cpu._ICpu cpu, Dafny.ISequence<TI59__Chips._IBrom> broms, Dafny.ISequence<TI59__Chips._IScom> scoms, Dafny.ISequence<TI59__Chips._IRam> rams, TI59__Chips._ILib lib, TI59__Chips._IKeyState keys, TI59__Chips._IDisp disp, TI59__Bus._IBus bus, bool halted) {
      return create(cpu, broms, scoms, rams, lib, keys, disp, bus, halted);
    }
    public bool is_Machine { get { return true; } }
    public TI59__Cpu._ICpu dtor_cpu {
      get {
        return this._cpu;
      }
    }
    public Dafny.ISequence<TI59__Chips._IBrom> dtor_broms {
      get {
        return this._broms;
      }
    }
    public Dafny.ISequence<TI59__Chips._IScom> dtor_scoms {
      get {
        return this._scoms;
      }
    }
    public Dafny.ISequence<TI59__Chips._IRam> dtor_rams {
      get {
        return this._rams;
      }
    }
    public TI59__Chips._ILib dtor_lib {
      get {
        return this._lib;
      }
    }
    public TI59__Chips._IKeyState dtor_keys {
      get {
        return this._keys;
      }
    }
    public TI59__Chips._IDisp dtor_disp {
      get {
        return this._disp;
      }
    }
    public TI59__Bus._IBus dtor_bus {
      get {
        return this._bus;
      }
    }
    public bool dtor_halted {
      get {
        return this._halted;
      }
    }
  }

  public interface _ITraceRec {
    bool is_TraceRec { get; }
    BigInteger dtor_addr { get; }
    ushort dtor_opcode { get; }
    Dafny.ISequence<byte> dtor_A { get; }
    Dafny.ISequence<byte> dtor_B { get; }
    Dafny.ISequence<byte> dtor_C { get; }
    Dafny.ISequence<byte> dtor_D { get; }
    Dafny.ISequence<byte> dtor_E { get; }
    ushort dtor_fA { get; }
    ushort dtor_fB { get; }
    ushort dtor_KR { get; }
    ushort dtor_SR { get; }
    byte dtor_R5 { get; }
    ushort dtor_ext { get; }
    bool dtor_cond { get; }
    bool dtor_idle { get; }
    bool dtor_hold { get; }
    Dafny.ISequence<byte> dtor_Sin { get; }
    Dafny.ISequence<byte> dtor_Sout { get; }
    BigInteger dtor_dstate { get; }
    _ITraceRec DowncastClone();
  }
  public class TraceRec : _ITraceRec {
    public readonly BigInteger _addr;
    public readonly ushort _opcode;
    public readonly Dafny.ISequence<byte> _A;
    public readonly Dafny.ISequence<byte> _B;
    public readonly Dafny.ISequence<byte> _C;
    public readonly Dafny.ISequence<byte> _D;
    public readonly Dafny.ISequence<byte> _E;
    public readonly ushort _fA;
    public readonly ushort _fB;
    public readonly ushort _KR;
    public readonly ushort _SR;
    public readonly byte _R5;
    public readonly ushort _ext;
    public readonly bool _cond;
    public readonly bool _idle;
    public readonly bool _hold;
    public readonly Dafny.ISequence<byte> _Sin;
    public readonly Dafny.ISequence<byte> _Sout;
    public readonly BigInteger _dstate;
    public TraceRec(BigInteger addr, ushort opcode, Dafny.ISequence<byte> A, Dafny.ISequence<byte> B, Dafny.ISequence<byte> C, Dafny.ISequence<byte> D, Dafny.ISequence<byte> E, ushort fA, ushort fB, ushort KR, ushort SR, byte R5, ushort ext, bool cond, bool idle, bool hold, Dafny.ISequence<byte> Sin, Dafny.ISequence<byte> Sout, BigInteger dstate) {
      this._addr = addr;
      this._opcode = opcode;
      this._A = A;
      this._B = B;
      this._C = C;
      this._D = D;
      this._E = E;
      this._fA = fA;
      this._fB = fB;
      this._KR = KR;
      this._SR = SR;
      this._R5 = R5;
      this._ext = ext;
      this._cond = cond;
      this._idle = idle;
      this._hold = hold;
      this._Sin = Sin;
      this._Sout = Sout;
      this._dstate = dstate;
    }
    public _ITraceRec DowncastClone() {
      if (this is _ITraceRec dt) { return dt; }
      return new TraceRec(_addr, _opcode, _A, _B, _C, _D, _E, _fA, _fB, _KR, _SR, _R5, _ext, _cond, _idle, _hold, _Sin, _Sout, _dstate);
    }
    public override bool Equals(object other) {
      var oth = other as TI59__Machine.TraceRec;
      return oth != null && this._addr == oth._addr && this._opcode == oth._opcode && object.Equals(this._A, oth._A) && object.Equals(this._B, oth._B) && object.Equals(this._C, oth._C) && object.Equals(this._D, oth._D) && object.Equals(this._E, oth._E) && this._fA == oth._fA && this._fB == oth._fB && this._KR == oth._KR && this._SR == oth._SR && this._R5 == oth._R5 && this._ext == oth._ext && this._cond == oth._cond && this._idle == oth._idle && this._hold == oth._hold && object.Equals(this._Sin, oth._Sin) && object.Equals(this._Sout, oth._Sout) && this._dstate == oth._dstate;
    }
    public override int GetHashCode() {
      ulong hash = 5381;
      hash = ((hash << 5) + hash) + 0;
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._addr));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._opcode));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._A));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._B));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._C));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._D));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._E));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._fA));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._fB));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._KR));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._SR));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._R5));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._ext));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._cond));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._idle));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._hold));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._Sin));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._Sout));
      hash = ((hash << 5) + hash) + ((ulong)Dafny.Helpers.GetHashCode(this._dstate));
      return (int) hash;
    }
    public override string ToString() {
      string s = "TI59_Machine.TraceRec.TraceRec";
      s += "(";
      s += Dafny.Helpers.ToString(this._addr);
      s += ", ";
      s += Dafny.Helpers.ToString(this._opcode);
      s += ", ";
      s += Dafny.Helpers.ToString(this._A);
      s += ", ";
      s += Dafny.Helpers.ToString(this._B);
      s += ", ";
      s += Dafny.Helpers.ToString(this._C);
      s += ", ";
      s += Dafny.Helpers.ToString(this._D);
      s += ", ";
      s += Dafny.Helpers.ToString(this._E);
      s += ", ";
      s += Dafny.Helpers.ToString(this._fA);
      s += ", ";
      s += Dafny.Helpers.ToString(this._fB);
      s += ", ";
      s += Dafny.Helpers.ToString(this._KR);
      s += ", ";
      s += Dafny.Helpers.ToString(this._SR);
      s += ", ";
      s += Dafny.Helpers.ToString(this._R5);
      s += ", ";
      s += Dafny.Helpers.ToString(this._ext);
      s += ", ";
      s += Dafny.Helpers.ToString(this._cond);
      s += ", ";
      s += Dafny.Helpers.ToString(this._idle);
      s += ", ";
      s += Dafny.Helpers.ToString(this._hold);
      s += ", ";
      s += Dafny.Helpers.ToString(this._Sin);
      s += ", ";
      s += Dafny.Helpers.ToString(this._Sout);
      s += ", ";
      s += Dafny.Helpers.ToString(this._dstate);
      s += ")";
      return s;
    }
    private static readonly TI59__Machine._ITraceRec theDefault = create(BigInteger.Zero, 0, TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), 0, 0, 0, 0, 0, 0, false, false, false, TI59__Nibbles.Reg.Default(), TI59__Nibbles.Reg.Default(), BigInteger.Zero);
    public static TI59__Machine._ITraceRec Default() {
      return theDefault;
    }
    private static readonly Dafny.TypeDescriptor<TI59__Machine._ITraceRec> _TYPE = new Dafny.TypeDescriptor<TI59__Machine._ITraceRec>(TI59__Machine.TraceRec.Default());
    public static Dafny.TypeDescriptor<TI59__Machine._ITraceRec> _TypeDescriptor() {
      return _TYPE;
    }
    public static _ITraceRec create(BigInteger addr, ushort opcode, Dafny.ISequence<byte> A, Dafny.ISequence<byte> B, Dafny.ISequence<byte> C, Dafny.ISequence<byte> D, Dafny.ISequence<byte> E, ushort fA, ushort fB, ushort KR, ushort SR, byte R5, ushort ext, bool cond, bool idle, bool hold, Dafny.ISequence<byte> Sin, Dafny.ISequence<byte> Sout, BigInteger dstate) {
      return new TraceRec(addr, opcode, A, B, C, D, E, fA, fB, KR, SR, R5, ext, cond, idle, hold, Sin, Sout, dstate);
    }
    public static _ITraceRec create_TraceRec(BigInteger addr, ushort opcode, Dafny.ISequence<byte> A, Dafny.ISequence<byte> B, Dafny.ISequence<byte> C, Dafny.ISequence<byte> D, Dafny.ISequence<byte> E, ushort fA, ushort fB, ushort KR, ushort SR, byte R5, ushort ext, bool cond, bool idle, bool hold, Dafny.ISequence<byte> Sin, Dafny.ISequence<byte> Sout, BigInteger dstate) {
      return create(addr, opcode, A, B, C, D, E, fA, fB, KR, SR, R5, ext, cond, idle, hold, Sin, Sout, dstate);
    }
    public bool is_TraceRec { get { return true; } }
    public BigInteger dtor_addr {
      get {
        return this._addr;
      }
    }
    public ushort dtor_opcode {
      get {
        return this._opcode;
      }
    }
    public Dafny.ISequence<byte> dtor_A {
      get {
        return this._A;
      }
    }
    public Dafny.ISequence<byte> dtor_B {
      get {
        return this._B;
      }
    }
    public Dafny.ISequence<byte> dtor_C {
      get {
        return this._C;
      }
    }
    public Dafny.ISequence<byte> dtor_D {
      get {
        return this._D;
      }
    }
    public Dafny.ISequence<byte> dtor_E {
      get {
        return this._E;
      }
    }
    public ushort dtor_fA {
      get {
        return this._fA;
      }
    }
    public ushort dtor_fB {
      get {
        return this._fB;
      }
    }
    public ushort dtor_KR {
      get {
        return this._KR;
      }
    }
    public ushort dtor_SR {
      get {
        return this._SR;
      }
    }
    public byte dtor_R5 {
      get {
        return this._R5;
      }
    }
    public ushort dtor_ext {
      get {
        return this._ext;
      }
    }
    public bool dtor_cond {
      get {
        return this._cond;
      }
    }
    public bool dtor_idle {
      get {
        return this._idle;
      }
    }
    public bool dtor_hold {
      get {
        return this._hold;
      }
    }
    public Dafny.ISequence<byte> dtor_Sin {
      get {
        return this._Sin;
      }
    }
    public Dafny.ISequence<byte> dtor_Sout {
      get {
        return this._Sout;
      }
    }
    public BigInteger dtor_dstate {
      get {
        return this._dstate;
      }
    }
  }
} // end of namespace TI59__Machine
namespace _module {

} // end of namespace _module
