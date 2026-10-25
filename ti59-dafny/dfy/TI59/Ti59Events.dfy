module TI59_Events {
  import opened TI59_Nibbles

  // ============================================================
  // Udalosti tiskarny/karty - LOG() volani C simulatoru (viz -v 3 =
  // LOG_SHORT + LOG_HRAST v log.txt) emitovane z modelu; runner je tiskne
  // ve stejnem formatu, compare-trace.py porovnava podsekvence.
  // Poradi odpovida poradi chipu ve foldu = poradi C logu.
  // ============================================================
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
}