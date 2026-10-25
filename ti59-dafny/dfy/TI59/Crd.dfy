module TI59_Crd {
  import opened TI59_Nibbles
  import opened TI59_Bus
  import opened TI59_Chips
  import opened TI59_Events

  // ============================================================
  // crd.c - magneticka ctecka/zapisovacka karet. Verny port; udalosti
  // CRD.* jako Ti59Event (viz TI59_Events). Zavislost: Nibbles (Bit8),
  // Bus, Chips (KeyState/KR_BIT).
  // ============================================================

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