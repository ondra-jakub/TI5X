module TI59_AluOps {
  import opened TI59_Nibbles

  // ============================================================
  // ALU operace - verny port Alu() [alu.c:109-168] a Xch() [alu.c:173-181].
  // Rekurzivni zapis smycky over 16 cislic; akumulatory (carry, shl, dst,
  // Sout, R5, condClear) se nasiji pres parametry - zkompilovany kod je
  // ekvivalentni C smycce.
  //
  // Semantika C unsigned char je presne bv8 (wraparound).
  // ============================================================

  // [alu.c:106] enum {ALU_ADD, ALU_SHL, ALU_SUB, ALU_SHR};
  datatype AluOp = OP_ADD | OP_SHL | OP_SUB | OP_SHR

  const MASK_ALL: Mask := Mask(0, 15, 0, 0) // [alu.c:84] mask_info[1] "ALL"

  datatype AluRes = AluRes(dst: Reg, Sout: Reg, R5: u8, condClear: bool, carry: u8)

  function IsSubLike(op: AluOp): bool { op == OP_SUB || op == OP_SHR }
  function IsShift(op: AluOp): bool { op == OP_SHL || op == OP_SHR }

  // Carry + korekce cislice [alu.c:136-146]. Cislice 0 ma hex carry
  // (>= 16, bez BCD korekce pod 16) - vlastnost C modelu zamerne pouzivana
  // IO sbernici (viz ram.c komentar o hex adresach > 99).
  function CarryCorrect(i: nat, sum5: u8, op: AluOp): (u8, u8)
    requires i <= 15
  {
    if i == 0 then
      (if sum5 >= 0x10 then 1 else 0,
       if sum5 >= 0x10 then sum5 & 0x0F else sum5)
    else
      (if sum5 >= 10 then 1 else 0,
       if sum5 >= 10 then (if !IsSubLike(op) then sum5 - 10 else sum5 + 10) else sum5)
  }

  // Zapisy dst v rozsahu masky [alu.c:148-163].
  // POZOR: pro ALU skupinu opcode plati m.start >= 0, takze zapis
  // dst[i-1] pri SHR je vzdy v rozsahu (i > start >= 0 => i >= 1).
  // Explicitni i >= 1 zachranuje totalitu i pro masky se start = -1
  // (ty se do ALU skupiny nedostanou; C by zapsala dst[-1] - UB).
  function DstWrite(i: nat, dst: Reg, shlIn: u8, shr2: u8, sum6: u8,
                    hasDst: bool, op: AluOp, m: Mask): (Reg, u8)
    requires i <= 15
  {
    if !(m.start <= i && i <= m.end) then (dst, shlIn)
    else if !hasDst then (dst, shlIn)
    else if op == OP_SHL then (RegUpd(dst, i, shlIn), sum6)
    else if op == OP_SHR then
      if i == m.end then
        (RegUpd(if i > m.start && i >= 1 then RegUpd(dst, i - 1, shr2) else dst, i, 0), sum6)
      else if i > m.start && i >= 1 then (RegUpd(dst, i - 1, shr2), sum6)
      else (dst, sum6)
    else (RegUpd(dst, i, sum6), sum6)
  }

  // Akumulator po zpracoveni jedne cislice [alu.c:109-167].
  datatype AluAcc = AluAcc(carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8, cc: bool)

  // Jeden krok smycky Alu() - vsechny lety tela AluDigits, izolovane do
  // male funkce (levne unfold pro z3 i pro codegen).
  function AluStep(i: nat, carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8,
                   condClear: bool, hasDst: bool, hasX: bool, x: Reg,
                   hasY: bool, y: Reg, m: Mask, op: AluOp, sin: Reg,
                   ioValid: bool): AluAcc
    requires i <= 15
  {
    // [alu.c:119-120] reset akumulatoru na zacatku masky
    var carryIn: u8 := if i == m.start then 0 else carry;
    var shlIn: u8 := if i == m.start then 0 else shl;
    // [alu.c:121-126] sum = srcY[i] | Sin[i] | cval
    var sum0: u8 := if hasY then y[i] else 0;
    var sum1: u8 := if ioValid then sum0 else sum0 | sin[i];
    var sum2: u8 := if i == m.cpos then sum1 | m.cval else sum1;
    var shr: u8 := sum2;
    // [alu.c:127-134] sum += carry; pripadna negace; sum += srcX[i]
    var sum3: u8 := sum2 + carryIn;
    var sum4: u8 := if IsSubLike(op) then 0 - sum3 else sum3;
    var sum5: u8 := if hasX then sum4 + x[i] else sum4;
    var shr2: u8 := if hasX then shr | x[i] else shr;
    // [alu.c:135] Sout se plni vzdy (nezavisle na masce/dst)
    var Sout1 := RegUpd(Sout, i, sum5 & 0x0F);
    // [alu.c:136-146] carry + BCD korekce
    var corr := CarryCorrect(i, sum5, op);
    var carryOut: u8 := corr.0;
    var sum6: u8 := corr.1;
    // [alu.c:148-163] zapisy dst v rozsahu masky
    var dw := DstWrite(i, dst, shlIn, shr2, sum6, hasDst, op, m);
    var dst1: Reg := dw.0;
    var shl1: u8 := dw.1;
    var inRange := m.start <= i && i <= m.end;
    // [alu.c:149-150] R5 = sum na zacatku masky (i mimo if(dst)!)
    var R51: u8 := if inRange && i == m.start then sum6 else R5;
    // [alu.c:164-165] COND se maze jen pro ADD/SUB s carry na konci masky
    var cc1 := condClear || (inRange && i == m.end && !IsShift(op) && carryOut == 1);
    AluAcc(carryOut, shl1, dst1, Sout1, R51, cc1)
  }

  // Port smycky Alu() [alu.c:109-168]: rekurze over 16 cislicemi.
  // Pozice zapisu dst jsou vzdy v rozsahu 0..15, protoze pro ALU skupinu
  // opcode (bity 8-11 opcode nejsou v {0,8,A}) plati mask.start >= 0.
  function AluDigits(i: nat, carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8,
                     condClear: bool, hasDst: bool, hasX: bool, x: Reg,
                     hasY: bool, y: Reg, m: Mask, op: AluOp, sin: Reg,
                     ioValid: bool): AluRes
    requires i <= 16
    decreases 16 - i
  {
    if i == 16 then
      AluRes(dst, Sout, R5, condClear, carry)
    else
      var s := AluStep(i, carry, shl, dst, Sout, R5, condClear, hasDst,
                       hasX, x, hasY, y, m, op, sin, ioValid);
      AluDigits(i + 1, s.carry, s.shl, s.dst, s.Sout, s.R5, s.cc,
                hasDst, hasX, x, hasY, y, m, op, sin, ioValid)
  }

  // Vstupni bod - ekvivalent volani Alu() z execute().
  function AluExec(hasDst: bool, dst: Reg, hasX: bool, x: Reg, hasY: bool,
                   y: Reg, m: Mask, op: AluOp, sin: Reg, ioValid: bool,
                   R5: u8, Sout: Reg): AluRes
  {
    AluDigits(0, 0, 0, dst, Sout, R5, false, hasDst, hasX, x, hasY, y, m, op,
              sin, ioValid)
  }

  // ============================================================
  // Xch() [alu.c:173-181]: vymena s1/s2 v rozsahu masky.
  // Pro ALU skupinu opcode plati m.start >= 0, takze zadne zapisy
  // mimo pole (C by pri start=-1 zapisovala dst[-1]).
  // ============================================================
  function XchRange(i: int, s1: Reg, s2: Reg, m: Mask): (Reg, Reg)
    requires 0 <= i <= 16
    requires m.start >= 0 && m.end <= 15
    decreases m.end + 1 - i
  {
    if i > m.end then (s1, s2)
    else XchRange(i + 1, s1[i := s2[i]], s2[i := s1[i]], m)
  }

  function XchExec(s1: Reg, s2: Reg, m: Mask): (Reg, Reg)
    requires m.start >= 0 && m.start <= 16 && m.end <= 15
  {
    XchRange(m.start, s1, s2, m)
  }

  // ============================================================
  // Specifikace: "ucitelska" BCD aritmetika - matematicky cisty popis.
  // Dukazy dole spojuji AluDigits se specifikaci.
  // ============================================================

  // ADD: digit 0 ma verny hex-carry C modelu (carry pri >=16, jinak hodnota
  // beze korekce); cislice 1-15 standardni BCD.
  // Akumulatorovy zapis (stejny tvar rekurze jako AluDigits; per-cislicni
  // matematika je porad cista "ucebnicova" BCD pravidla).
  function BcdAddSpec(i: nat, cin: u8, acc: Reg, x: Reg, y: Reg): (Reg, u8)
    requires i <= 16
    requires (cin as int) <= 1
    requires forall j | i <= j < 16 :: (x[j] as int) < 10 && (y[j] as int) < 10
    decreases 16 - i
  {
    if i == 16 then (acc, cin)
    else
      var s := (x[i] as int) + (y[i] as int) + (cin as int);
      var digit := if i == 0 then (if s >= 16 then s - 16 else s)
                   else (if s >= 10 then s - 10 else s);
      var cout := if i == 0 then (if s >= 16 then 1 else 0)
                  else (if s >= 10 then 1 else 0);
      BcdAddSpec(i + 1, cout as u8, acc[i := digit as u8], x, y)
  }

  // SUB: odecte cislice s borrow; digit 0 ma hex-borrow (vysledek d+16),
  // cislice 1-15 BCD borrow (d+10).
  function BcdSubSpec(i: nat, bin: u8, acc: Reg, x: Reg, y: Reg): (Reg, u8)
    requires i <= 16
    requires (bin as int) <= 1
    requires forall j | i <= j < 16 :: (x[j] as int) < 10 && (y[j] as int) < 10
    decreases 16 - i
  {
    if i == 16 then (acc, bin)
    else
      var d := (x[i] as int) - ((y[i] as int) + (bin as int));
      var digit := if d < 0 then (if i == 0 then d + 16 else d + 10) else d;
      var bout := if d < 0 then 1 else 0;
      BcdSubSpec(i + 1, bout as u8, acc[i := digit as u8], x, y)
  }

  // ============================================================
  // Dukazy: AluDigits(OP_ADD/OP_SUB) == Bcd spec pro BCD-validni vstupy
  // (cislice < 10), Sin = 0, maska ALL, hasDst. Male pomocne lemmata
  // drzi bv/int "mosty" oddelene (z3 je prokaze rychle v malem kontextu).
  // ============================================================

  lemma L_BvOrZeroB(a: u8, b: u8)
    requires b == (0 as u8)
    ensures (a | b) == a
  {
  }

  // Obecny fakt: bv8 scitani je int soucet mod 256. (Tvar pres % je pro
  // z3 dobre resitelny; konkretni pripady bez preteceni pak odvozeny.)
  lemma L_BvAddMod256(a: u8, b: u8)
    ensures ((a + b) as int) == ((a as int) + (b as int)) % 256
  {
  }

  lemma L_BvAdd2(a: u8, b: u8)
    requires (a as int) + (b as int) < 256
    ensures (a + b) as int == (a as int) + (b as int)
  {
    L_BvAddMod256(a, b);
  }

  lemma L_BvNegInt(v: u8)
    ensures (0 - v) as int == (256 - (v as int)) % 256
  {
  }

  lemma L_BvNegAdd(v: u8, x: u8)
    requires (v as int) <= 10 && (x as int) <= 9
    ensures (((0 - v) + x) as int) == ((x as int) - (v as int)) % 256
  {
    L_BvNegInt(v);
    L_BvAddMod256((0 - v), x);
    if (x as int) < (v as int) {
      assert ((x as int) - (v as int)) % 256 == 256 + (x as int) - (v as int);
    }
  }

  lemma L_AndF(x: u8)
    ensures (x & (0x0F)) as int == (x as int) % 16
  {
  }

  // ADD korekce: CarryCorrect == cista int aritmetika (hex carry na 0)
  lemma L_CcAdd(i: nat, sum: u8, s: int)
    requires i <= 15
    requires (sum as int) == s
    requires 0 <= s <= 19
    ensures (CarryCorrect(i, sum, OP_ADD).0 as int) ==
      (if i == 0 then (if s >= 16 then 1 else 0) else (if s >= 10 then 1 else 0))
    ensures (CarryCorrect(i, sum, OP_ADD).1 as int) ==
      (if i == 0 then (if s >= 16 then s - 16 else s) else (if s >= 10 then s - 10 else s))
  {
    if i == 0 {
      if s >= 16 {
        L_AndF(sum);
        assert 16 <= s <= 19;
        assert s % 16 == s - 16;
      }
    } else {
      if s >= 10 {
        assert (sum - (10 as u8)) as int == s - 10;
      }
    }
  }

  // SUB korekce: negace v bv8 + hex/BCD borrow
  lemma L_CcSub(i: nat, sum: u8, d: int)
    requires i <= 15
    requires (sum as int) == (if d < 0 then 256 + d else d)
    requires -10 <= d <= 9
    ensures (CarryCorrect(i, sum, OP_SUB).0 as int) == (if d < 0 then 1 else 0)
    ensures (CarryCorrect(i, sum, OP_SUB).1 as int) ==
      (if d < 0 then (if i == 0 then d + 16 else d + 10) else d)
  {
    if d < 0 {
      assert 246 <= 256 + d <= 255;
      if i == 0 {
        L_AndF(sum);
        assert (256 + d) % 16 == d + 16;
      } else {
        assert (sum + (10 as u8)) as int == d + 10;
      }
    }
  }

  lemma L_AlusAddBcd(i: nat, carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8,
                     cc: bool, hasDst: bool, x: Reg, y: Reg, sin: Reg)
    requires i <= 16
    requires hasDst
    requires (carry as int) <= 1
    requires i != 0 || carry == (0 as u8) // carry se na start=0 masce resetuje
    requires forall j | i <= j < 16 ::
      (x[j] as int) < 10 && (y[j] as int) < 10 && (sin[j] as int) == 0
    ensures var a := AluDigits(i, carry, shl, dst, Sout, R5, cc, hasDst,
                               true, x, true, y, MASK_ALL, OP_ADD, sin, false);
            var b := BcdAddSpec(i, carry, dst, x, y);
            a.dst == b.0 && a.carry == b.1
    decreases 16 - i
  {
    if i < 16 {
      L_BvOrZeroB(y[i], sin[i]);
      var carryIn: u8 := if i == 0 then 0 else carry;
      var sum1: u8 := y[i] | sin[i];
      // MASK_ALL.cval == 0: sum2 = sum1 | cval == sum1 (i == cpos == 0)
      L_BvOrZeroB(sum1, MASK_ALL.cval);
      var sum3: u8 := sum1 + carryIn;
      var sum5: u8 := sum3 + x[i];
      var s := (y[i] as int) + ((carryIn as int)) + (x[i] as int);
      L_BvAdd2(sum1, carryIn);
      L_BvAdd2(sum3, x[i]);
      assert s <= 19;
      L_CcAdd(i, sum5, s);
      var corr := CarryCorrect(i, sum5, OP_ADD);
      var sum6: u8 := corr.1;
      var cout: u8 := corr.0;
      // spec hodnoty a zpetne obejiti pretypovani int->u8
      var digitS := if i == 0 then (if s >= 16 then s - 16 else s)
                    else (if s >= 10 then s - 10 else s);
      var coutS := if i == 0 then (if s >= 16 then 1 else 0)
                   else (if s >= 10 then 1 else 0);
      assert 0 <= digitS <= 15 && 0 <= coutS <= 1;
      assert (digitS as u8) == sum6;
      assert (coutS as u8) == cout;
      var dst1: Reg := if hasDst then RegUpd(dst, i, sum6) else dst;
      var Sout1 := RegUpd(Sout, i, sum5 & 0x0F);
      var R51: u8 := if i == 0 then sum6 else R5;
      var cc1 := cc || (i == MASK_ALL.end && cout == (1 as u8));
      // krok AluStep == nase vypoctene hodnoty (per-pole, male dotazy)
      var bsum2: u8 := if i == MASK_ALL.cpos then (y[i] | sin[i]) | MASK_ALL.cval else (y[i] | sin[i]);
      var bcarryIn: u8 := if i == MASK_ALL.start then 0 else carry;
      assert bsum2 == sum1;
      assert bcarryIn == carryIn;
      assert (bsum2 + bcarryIn) == sum3;
      assert ((bsum2 + bcarryIn) + x[i]) == sum5;
      assert MASK_ALL.start >= 0 && i <= 15;
      var st := AluStep(i, carry, shl, dst, Sout, R5, cc, true,
                        true, x, true, y, MASK_ALL, OP_ADD, sin, false);
      assert st.carry == cout;
      assert st.shl == sum6;
      assert st.dst == dst1;
      assert st.Sout == Sout1;
      assert st.R5 == R51;
      assert st.cc == cc1;
      L_AlusAddBcd(i + 1, cout, sum6, dst1, Sout1, R51, cc1, hasDst,
                   x, y, sin);
    }
  }

  lemma L_AlusSubBcd(i: nat, carry: u8, shl: u8, dst: Reg, Sout: Reg, R5: u8,
                     cc: bool, hasDst: bool, x: Reg, y: Reg, sin: Reg)
    requires i <= 16
    requires hasDst
    requires (carry as int) <= 1
    requires i != 0 || carry == (0 as u8)
    requires forall j | i <= j < 16 ::
      (x[j] as int) < 10 && (y[j] as int) < 10 && (sin[j] as int) == 0
    ensures var a := AluDigits(i, carry, shl, dst, Sout, R5, cc, hasDst,
                               true, x, true, y, MASK_ALL, OP_SUB, sin, false);
            var b := BcdSubSpec(i, carry, dst, x, y);
            a.dst == b.0 && a.carry == b.1
    decreases 16 - i
  {
    if i < 16 {
      L_BvOrZeroB(y[i], sin[i]);
      var carryIn: u8 := if i == 0 then 0 else carry;
      var sum1: u8 := y[i] | sin[i];
      L_BvOrZeroB(sum1, MASK_ALL.cval);
      var sum3: u8 := sum1 + carryIn;
      var sum4: u8 := 0 - sum3;
      var sum5: u8 := sum4 + x[i];
      var d := (x[i] as int) - ((y[i] as int) + (carryIn as int));
      assert -10 <= d <= 9;
      assert sum3 as int == (y[i] as int) + (carryIn as int);
      L_BvNegAdd(sum3, x[i]);
      assert sum5 as int == (if d < 0 then 256 + d else d);
      L_CcSub(i, sum5, d);
      var corr := CarryCorrect(i, sum5, OP_SUB);
      var sum6: u8 := corr.1;
      var cout: u8 := corr.0;
      var digitS := if d < 0 then (if i == 0 then d + 16 else d + 10) else d;
      var coutS := if d < 0 then 1 else 0;
      assert 0 <= digitS <= 15 && 0 <= coutS <= 1;
      assert (digitS as u8) == sum6;
      assert (coutS as u8) == cout;
      var dst1: Reg := if hasDst then RegUpd(dst, i, sum6) else dst;
      var Sout1 := RegUpd(Sout, i, sum5 & 0x0F);
      var R51: u8 := if i == 0 then sum6 else R5;
      var cc1 := cc || (i == MASK_ALL.end && cout == (1 as u8));
      var bsum2: u8 := if i == MASK_ALL.cpos then (y[i] | sin[i]) | MASK_ALL.cval else (y[i] | sin[i]);
      var bcarryIn: u8 := if i == MASK_ALL.start then 0 else carry;
      assert bsum2 == sum1;
      assert bcarryIn == carryIn;
      assert (bsum2 + bcarryIn) == sum3;
      assert ((0 - (bsum2 + bcarryIn)) + x[i]) == sum5;
      assert MASK_ALL.start >= 0 && i <= 15;
      var st := AluStep(i, carry, shl, dst, Sout, R5, cc, true,
                        true, x, true, y, MASK_ALL, OP_SUB, sin, false);
      assert st.carry == cout;
      assert st.shl == sum6;
      assert st.dst == dst1;
      assert st.Sout == Sout1;
      assert st.R5 == R51;
      assert st.cc == cc1;
      L_AlusSubBcd(i + 1, cout, sum6, dst1, Sout1, R51, cc1, hasDst,
                   x, y, sin);
    }
  }

  // ============================================================
  // Celociselna specifikace pres ValueOf (mod 10^16) je cilovany
  // doplnek: digit-level spec vyse dokazuje "Alu == ucebnicove BCD po
  // cislicich"; bridging na (X+Y) mod 10^16 vyzaduje nonlinear lemmata
  // o 10^k - dokumentovany dukazovy TODO (hranice dukazu je na urovni
  // cistic, hodnota pres ValueOf je ghost funkce v Nibbles).
  // ============================================================
}
