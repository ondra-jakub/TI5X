module TI59_Nibbles {
  // ============================================================
  // Zakladni typy pro verifikovany model TMS0500 (TI-59).
  //
  // Cituji radky z C simulatoru /Users/ondra/ti59_chips/TMS0500-simulator/
  // jako [alu.c:123] apod. - model je jejich vernym portem.
  //
  // C unsigned char / unsigned short = bv8 / bv16 bitvektory:
  // wraparound aritmetika je presne semantika C bez extra modula.
  // ============================================================

  type u8 = bv8
  type u16 = bv16

  // 16 ciselnych cisco registru, LSB-first (io[0] = nejnizsi cislice).
  // Cislice NEJSOU omezeny na 0-15: C unsigned char dovoluje 0-255
  // (ALU korekce muze vyprodukovat hodnoty > 15, napr. ADD 15+15; viz alu.c).
  type Reg = s: seq<u8> | |s| == 16 witness seq(16, _ => 0 as u8)

  // POZOR: const subsetoveho typu jako argument konstruktoru datatype
  // padne na interne chybe Dafny 4.11 (Boogie resolve null-ref);
  // proto se pro konstrukci pouziva tato funkce:
  function ZeroReg(): Reg { seq(16, _ => 0 as u8) }

  function RegUpd(s: Reg, i: nat, v: u8): Reg
    requires i < 16
  {
    s[i := v]
  }

  // Cislice jsou "BCD-platne", pokud jsou < 10 (podminka pro dukazy).
  predicate BcdValid(s: Reg) {
    forall i | 0 <= i < 16 :: (s[i] as int) < 10
  }

  ghost function ValueOf(s: seq<u8>, n: nat): int
    requires n <= |s|
    decreases n
  {
    if n == 0 then 0 else ValueOf(s, n - 1) + 10 * (s[n - 1] as int) * Pow10(n - 1)
  }

  ghost function Pow10(n: nat): nat
    decreases n
  {
    if n == 0 then 1 else 10 * Pow10(n - 1)
  }

  // ============================================================
  // Maskova tabulka ALU [alu.c:82-99] mask_info[16].
  // start muze byt -1 (masky 0, 8, 10) - pro ALU skupinu opcode
  // (bity 8-11 nen v {0,8,A}) se ale takove masky nikdy nevyberou
  // (dukaz: ALU skupina vylucuje presne tyto hodnoty), takze vsechny
  // pouzite masky maji start >= 0.
  // ============================================================
  datatype Mask = Mask(start: int, end: int, cpos: int, cval: u8)

  // Maska ALU skupiny opcode: bity 8-11 opcode nejsou v {0, 8, A}, takze
  // start >= 0 (C by pri start=-1 zapisovala mimo pole - nepouzitelne
  // masky 0/8/10 se do ALU skupiny nedostanou). Typ svymi invarianty
  // prenasí "bounds" do vsech volani bez nutnosti lemmat.
  type AluMask = m: Mask | m.start >= 0 && m.start <= 15 && m.start <= m.end + 1 && m.end <= 15
    witness Mask(0, 15, 0, 0)

  // mask_info[16] [alu.c:82-99] - ALU skupina pouziva indexy
  // {1..7, 9, B..F}; ostatni vraci neutralni masku (nepouzito).
  function MaskOf(idx: nat): AluMask
    requires idx < 16
  {
    if idx == 1 then Mask(0, 15, 0, 0)  // ALL
    else if idx == 2 then Mask(0, 0, 0, 0)  // DPT
    else if idx == 3 then Mask(0, 0, 0, 1)  // DPT 1
    else if idx == 4 then Mask(0, 0, 0, 12) // DPT C
    else if idx == 5 then Mask(3, 3, 3, 1)  // LLSD 1
    else if idx == 6 then Mask(1, 2, 1, 0)  // EXP
    else if idx == 7 then Mask(1, 2, 1, 1)  // EXP 1
    else if idx == 9 then Mask(3, 15, 3, 0) // MANT
    else if idx == 11 then Mask(3, 15, 3, 5) // MLSD 5
    else if idx == 12 then Mask(1, 15, 1, 0) // MAEX
    else if idx == 13 then Mask(1, 15, 3, 1) // MLSD 1
    else if idx == 14 then Mask(1, 15, 15, 1) // MMSD 1
    else if idx == 15 then Mask(1, 15, 1, 1)  // MAEX 1
    else Mask(0, 15, 0, 0) // 0, 8, 10: ALU skupina sem nedosahne
  }

  // ============================================================
  // Flagy CPU [alu.c:60-66]
  // ============================================================
  const FLG_IDLE: u16 := (0x0001) as u16
  const FLG_HOLD: u16 := (0x0002) as u16
  const FLG_JUMP: u16 := (0x0004) as u16
  const FLG_IO_VALID: u16 := (0x0400) as u16
  const FLG_COND: u16 := (0x0800) as u16
  const FLG_COND_LAST: u16 := (0x1000) as u16
  const FLG_BUSY: u16 := (0x8000) as u16

  // 1 << b pro b < 8
  function Bit8(b: nat): u8
    requires b < 8
    decreases b
  {
    if b == 0 then (1 as u8) else Bit8(b - 1) << 1
  }

  // 1 << b pro b < 16
  function Bit16(b: nat): u16
    requires b < 16
    decreases b
  {
    if b == 0 then (1 as u16) else Bit16(b - 1) << 1
  }

  // NUL znak (C '\0') - Dafny nema escape v char literalech
  function Nul(): char { 0 as char }

  // EXT signaly na sběrnici [bus.h:35-37]
  const EXT_PREG: u16 := (0x0001) as u16
  const EXT_COND: u16 := (0x0002) as u16
  const EXT_HOLD: u16 := (0x0004) as u16
}