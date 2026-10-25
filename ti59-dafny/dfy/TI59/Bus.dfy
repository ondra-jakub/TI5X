module TI59_Bus {
  import opened TI59_Nibbles

  // ============================================================
  // Sbernice [bus.h] + inicializace cyklu z vbus.c run().
  // sstate/write jsou parametry foldu (v C jsou poli bus).
  // io je 16 cisl. (Reg = seq 16x u8), LSB-first.
  // ============================================================

  datatype Bus = Bus(
    ext: u16,          // [bus.h:38]
    irg: u16,          // [bus.h:42]
    io: Reg,           // [bus.h:45]
    displayDigit: char,  // [bus.h:48]
    displayDpt: bool,    // [bus.h:49]
    displaySegH: bool,   // [bus.h:50]
    keyLine: u8,       // [bus.h:60]
    dstate: nat,       // [bus.h:63] D15..D0
    idle: bool,        // [bus.h:66]
    addr: int,         // [bus.h:74] -1 = zadny vystup ROM
    cycleCount: nat)   // -K patch: citac instrukcnich cyklu

  function BusInit(): Bus
  {
    Bus((0) as u16, (0) as u16, ZeroReg(), ' ', false, false,
        (0) as u8, 15, false, 0, 0)  // [vbus.c:42-43] dstate = 15, digit = ' '
  }

  // Zacatek instrukcniho cyklu [vbus.c:46-49]
  function BusCycleStart(b: Bus): Bus
  {
    b.(ext := 0, irg := 0, io := ZeroReg(), addr := -1,
       cycleCount := b.cycleCount + 1)
  }

  // Mezi S14R/S15W [vbus.c:68-75]: key_line = 0, dstate--
  function BusAfterS14(b: Bus): Bus
  {
    b.(keyLine := 0,
       dstate := if b.dstate != 0 then b.dstate - 1 else 15)
  }
}