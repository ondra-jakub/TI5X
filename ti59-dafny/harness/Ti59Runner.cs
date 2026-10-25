// Runner pro verifikovany Dafny model TMS0500 (TI-59).
// Parse .txt dumpy (stejne jako utils.c load_dump/load_dumpK/load_dump8)
// a klavesovy skript (-K format), postavi Machine a bezi StepInstruction.
// Vystup: HRAST-kompatibilni zaznamy (bez disasm textu) + finalni displej.
// Pouziti:
//   Ti59Runner <rom0582> <const0582> <rom0583> <const0583> <rom0571B>
//              <libdump|-> <script> <maxCycles> <traceOut>
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using Dafny;

public static class Ti59Runner {
  // ---------------- dump parsing (utils.c) ----------------

  // load_dump: radky "%X: word word ...", HEX adresa
  static (ushort[] data, int baseAddr) LoadDumpRom(string file) {
    var words = new List<ushort>();
    int baseAddr = -1;
    int expect = 0;
    foreach (var raw in File.ReadAllLines(file)) {
      var line = raw.TrimEnd();
      if (line.Length < 5 || line[4] != ':') continue;
      bool ok = true;
      for (int i = 0; i < 4; i++) if (!Uri.IsHexDigit(line[i])) { ok = false; break; }
      if (!ok) continue;
      int addr = Convert.ToInt32(line.Substring(0, 4), 16);
      if (baseAddr == -1) baseAddr = addr;
      if (addr < baseAddr) continue;
      addr -= baseAddr;
      foreach (var tok in line.Substring(5).Split((char[])null, StringSplitOptions.RemoveEmptyEntries)) {
        ushort v;
        if (!ushort.TryParse(tok, System.Globalization.NumberStyles.HexNumber, null, out v)) break;
        while (words.Count < addr) words.Add(0);
        if (addr == words.Count) words.Add(v);
        addr++;
      }
    }
    return (words.ToArray(), baseAddr);
  }

  // load_dumpK: radky "%d: 16 hex ciselnich" (DECIMALNI adresa, LSB last);
  // konci na "ADDR: CONSTANT ROM (KEY CODE)".
  static byte[][] LoadDumpK(string file) {
    var rows = new List<byte[]>();
    int baseAddr = -1;
    foreach (var raw in File.ReadAllLines(file)) {
      var line = raw.TrimEnd();
      if (line.StartsWith("ADDR: CONSTANT ROM (KEY CODE)")) break;
      if (line.Length < 4 || line[3] != ':') continue;
      bool ok = line.Length >= 3 && char.IsDigit(line[0]) && char.IsDigit(line[1]) && char.IsDigit(line[2]);
      if (!ok) continue;
      int addr = int.Parse(line.Substring(0, 3));
      if (baseAddr == -1) baseAddr = addr;
      if (addr < baseAddr) continue;
      addr -= baseAddr;
      foreach (var tok in line.Substring(4).Split((char[])null, StringSplitOptions.RemoveEmptyEntries)) {
        ulong v;
        if (!ulong.TryParse(tok, System.Globalization.NumberStyles.HexNumber, null, out v)) break;
        var row = new byte[16];
        for (int j = 0; j < 16; j++) { row[j] = (byte)(v & 0xF); v >>= 4; }
        while (rows.Count < addr) rows.Add(new byte[16]);
        if (addr == rows.Count) rows.Add(row);
        addr++;
      }
    }
    return rows.ToArray();
  }

  // load_dump8: radky "%d: hex byte ..." (DECIMALNI adresa)
  static byte[] LoadDump8(string file, int size) {
    var data = new byte[size];
    foreach (var raw in File.ReadAllLines(file)) {
      var line = raw.TrimEnd();
      if (line.Length < 5 || line[4] != ':') continue;
      if (!(char.IsDigit(line[0]) && char.IsDigit(line[1]) && char.IsDigit(line[2]) && char.IsDigit(line[3]))) continue;
      int addr = int.Parse(line.Substring(0, 4));
      foreach (var tok in line.Substring(5).Split((char[])null, StringSplitOptions.RemoveEmptyEntries)) {
        byte v;
        if (!byte.TryParse(tok, System.Globalization.NumberStyles.HexNumber, null, out v)) break;
        if (addr < size) { data[addr] = v; addr++; }
      }
    }
    return data;
  }

  // -K skript: "HEX [at] [hold] [toggle]"
  static List<TI59__Chips._IKeyEvent> LoadKeyScript(string file) {
    var events = new List<TI59__Chips._IKeyEvent>();
    foreach (var raw in File.ReadAllLines(file)) {
      var line = raw.Trim();
      if (line.Length == 0 || line[0] == '#') continue;
      var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length < 1) continue;
      byte code = Convert.ToByte(parts[0], 16);
      // format "code [at] [hold] [toggle]" - "4A 3000 toggle" = 3 tokeny:
      // hold-slot neslovo "toggle" = key_script_load (patch 0003)
      BigInteger at = BigInteger.Zero, hold = BigInteger.Zero;
      bool toggle = false;
      if (parts.Length > 1) {
        if (parts[1] == "toggle") { toggle = true; }
        else {
          at = BigInteger.Parse(parts[1]);
          if (parts.Length > 2) {
            if (parts[2] == "toggle") { toggle = true; }
            else {
              hold = BigInteger.Parse(parts[2]);
              if (parts.Length > 3 && parts[3] == "toggle") { toggle = true; }
            }
          }
        }
      }
      events.Add(new TI59__Chips.KeyEvent(code, at, hold, toggle));
    }
    return events;
  }

  static Dafny.ISequence<byte> SeqB(byte[] a) { return Dafny.Sequence<byte>.FromArray(a); }
  static Dafny.ISequence<Dafny.ISequence<byte>> SeqReg(byte[][] rows) {
    var seqs = new Dafny.ISequence<byte>[rows.Length];
    for (int i = 0; i < rows.Length; i++) seqs[i] = SeqB(rows[i]);
    return Dafny.Sequence<Dafny.ISequence<byte>>.FromArray(seqs);
  }
  static Dafny.ISequence<Dafny.Rune> SeqC(string s) {
    var runes = new Dafny.Rune[s.Length];
    for (int i = 0; i < s.Length; i++) runes[i] = new Dafny.Rune(s[i]);
    return Dafny.Sequence<Dafny.Rune>.FromArray(runes);
  }

  // ---------------- HRAST formatting ----------------
  static readonly StringBuilder sb = new StringBuilder();
  static string RegHex(Dafny.ISequence<byte> r) {
    sb.Clear();
    for (int i = 15; i >= 0; i--) sb.Append(r.Select(i).ToString("X"));
    return sb.ToString();
  }
  static string RuneStr(Dafny.ISequence<Dafny.Rune> s) {
    sb.Clear();
    foreach (var r in s) {
      if (r.Value == 0) break;  // %s konci na NUL
      sb.Append((char)r.Value);
    }
    return sb.ToString();
  }

  static int Main(string[] args) {
    // Pouziti (faze 3):
    //   Ti59Runner <rom0582> <const0582> <rom0583> <const0583> <rom0571B>
    //              <libdump|-> <card|-|file(300B)> <printer:0|1>
    //              <script> <maxCycles> <traceOut> [cardOut] [tapeOut]
    // Poradi chipu = argv orakula (vbus.c main()): alu, brom x3, scom x2,
    // ram x4, print (-p), lib (-l), crd (-c), display, key.
    if (args.Length < 11 || args.Length > 13) {
      Console.Error.WriteLine("usage: Ti59Runner rom0582 const0582 rom0583 const0583 rom0571B libdump|- card|- printer:0/1 script maxCycles traceOut [cardOut] [tapeOut]");
      return 2;
    }
    var (rom1, base1) = LoadDumpRom(args[0]);
    var const1 = LoadDumpK(args[1]);
    var (rom2, base2) = LoadDumpRom(args[2]);
    var const2 = LoadDumpK(args[3]);
    var (rom3, base3) = LoadDumpRom(args[4]);
    byte[] libData = args[5] == "-" ? new byte[5000] : LoadDump8(args[5], 5000);
    if (libData.Length < 5000) { Console.Error.WriteLine("lib dump must be 5000 bytes"); return 2; }
    bool hasPrinter = args[7] == "1";
    var script = LoadKeyScript(args[8]);
    long maxCycles = long.Parse(args[9]);
    var outPath = args[10];
    string cardOutPath = args.Length > 11 ? args[11] : null;
    string tapeOutPath = args.Length > 12 && args[12] != "-" ? args[12] : null;

    // card reader: obraz karty 300 B (crd.c: default 0x92 + fread overlay)
    byte[] cardData = null;
    if (args[6] != "-") {
      cardData = LoadCard(args[6]);
      Console.Error.WriteLine("card support");
    }

    // postav stroj (poradi chipu jako ti59d)
    var cpu = TI59__Cpu.__default.CpuInit();
    var broms = Dafny.Sequence<TI59__Chips._IBrom>.FromArray(new TI59__Chips._IBrom[] {
      TI59__Chips.__default.BromInit(Dafny.Sequence<ushort>.FromArray(rom1), new BigInteger(base1), new BigInteger(base1 + rom1.Length)),
      TI59__Chips.__default.BromInit(Dafny.Sequence<ushort>.FromArray(rom2), new BigInteger(base2), new BigInteger(base2 + rom2.Length)),
      TI59__Chips.__default.BromInit(Dafny.Sequence<ushort>.FromArray(rom3), new BigInteger(base3), new BigInteger(base3 + rom3.Length)),
    });
    var scoms = Dafny.Sequence<TI59__Chips._IScom>.FromArray(new TI59__Chips._IScom[] {
      TI59__Chips.__default.ScomInit(SeqReg(const1), new BigInteger(0)),
      TI59__Chips.__default.ScomInit(SeqReg(const2), new BigInteger(32)),
    });
    var ramsList = new TI59__Chips._IRam[4];
    for (int i = 0; i < 4; i++)
      ramsList[i] = TI59__Chips.__default.RamInit(EmptyRegs(), new BigInteger(30 * i));
    var rams = Dafny.Sequence<TI59__Chips._IRam>.FromArray(ramsList);
    var lib = TI59__Chips.__default.LibInit(SeqB(libData));
    var keys = TI59__Chips.__default.KeyInit(
      Dafny.Sequence<TI59__Chips._IKeyEvent>.FromArray(script.ToArray()), hasPrinter);
    var disp = TI59__Chips.__default.DispInit();
    var bus = TI59__Bus.__default.BusInit();
    var prnOpt = hasPrinter
      ? (TI59__Machine._IPrnOpt)new TI59__Machine.PrnOpt_Prn(TI59__PrintCrd.__default.PrintInit())
      : new TI59__Machine.PrnOpt_PrnNone();
    var crdOpt = cardData != null
      ? (TI59__Machine._ICrdOpt)new TI59__Machine.CrdOpt_Crd(TI59__PrintCrd.__default.CrdInit(SeqB(cardData)))
      : new TI59__Machine.CrdOpt_CrdNone();
    TI59__Machine._IMachine m = new TI59__Machine.Machine(cpu, broms, scoms, rams, lib, keys, disp, bus, false,
      prnOpt, crdOpt, Dafny.Sequence<TI59__PrintCrd._ITi59Event>.FromArray(new TI59__PrintCrd._ITi59Event[0]));

    if (!TI59__Machine.__default.MachineWF(m)) {
      Console.Error.WriteLine("MachineWF failed at init!");
      return 3;
    }

    using (var trace = new StreamWriter(outPath, false, new UTF8Encoding(false)))
    using (var tape = tapeOutPath != null
            ? new StreamWriter(tapeOutPath, false, new UTF8Encoding(false)) : null) {
      long cycles = 0;
      long records = 0;
      long prevEvents = 0;
      var sbEv = new StringBuilder();
      var sw = System.Diagnostics.Stopwatch.StartNew();
      // C -n semantika: count++ na zacatku cyklu; count >= max zastavi
      // PRED provedenim cyklu -> probehne max-1 cyklu.
      while (cycles + 1 < maxCycles) {
        var r = TI59__Machine.__default.StepInstruction(m);
        if (r.dtor__1.is_YesRec) {
          var rec = ((TI59__Machine.OptRec_YesRec)r.dtor__1)._rec;
          trace.Write(rec.dtor_addr.ToString("X4"));
          trace.Write(' ');
          trace.WriteLine(rec.dtor_opcode.ToString("X4"));
          trace.Write("A="); trace.Write(RegHex(rec.dtor_A));
          trace.Write(" B="); trace.Write(RegHex(rec.dtor_B));
          trace.Write(" C="); trace.Write(RegHex(rec.dtor_C));
          trace.Write(" D="); trace.Write(RegHex(rec.dtor_D));
          trace.Write(" E="); trace.WriteLine(RegHex(rec.dtor_E));
          trace.Write("FA="); trace.Write(rec.dtor_fA.ToString("X4"));
          trace.Write(" KR="); trace.Write(rec.dtor_KR.ToString("X4"));
          trace.Write(" EXT="); trace.Write(rec.dtor_ext.ToString("X2"));
          trace.Write(" COND="); trace.Write(rec.dtor_cond ? "1" : "0");
          trace.Write(" IDLE="); trace.Write(rec.dtor_idle ? "1" : "0");
          trace.Write(" IOi="); trace.Write(RegHex(rec.dtor_Sin));
          trace.Write(" IO="); trace.WriteLine(RegHex(rec.dtor_Sout));
          trace.Write("FB="); trace.Write(rec.dtor_fB.ToString("X4"));
          trace.Write(" SR="); trace.Write(rec.dtor_SR.ToString("X4"));
          trace.Write(" R5="); trace.WriteLine(rec.dtor_R5.ToString("X"));
          records++;
        }
        m = r.dtor__0;
        cycles++;
        // udalosti (LOG volani C): delta od minuleho cyklu, format C logu
        var evs = ((TI59__Machine.Machine)m).dtor_evs;
        long eventCount = evs.Count;
        sbEv.Clear();
        for (long i = prevEvents; i < eventCount; i++) {
          EventLogString(evs.Select(i), sbEv);
        }
        prevEvents = eventCount;
        if (sbEv.Length > 0) {
          trace.Write(sbEv.ToString());
          trace.WriteLine();
        }
        if (((TI59__Machine.Machine)m).dtor_halted) break;
      }
      sw.Stop();
      trace.WriteLine("# cycles=" + cycles + " records=" + records + " halted=" + m.dtor_halted);
      trace.WriteLine("# display='" + RuneStr(m.dtor_disp.dtor_out1) + "'");
      trace.WriteLine("# secs=" + sw.Elapsed.TotalSeconds.ToString("0.00"));

      // paska (tiskarna "|      %.20s" radky jako display_print v C)
      if (tape != null && m.dtor_printOpt.is_Prn) {
        var prnState = ((TI59__Machine.PrnOpt_Prn)m.dtor_printOpt)._prn;
        var tapeSeq = prnState.dtor_tape;
        for (long i = 0; i < tapeSeq.Count; i++) {
          tape.Write("|      ");
          foreach (var ch in tapeSeq.Select(i)) tape.Write((char)ch.Value);
          tape.WriteLine();
        }
      }
    }

    // obraz karty po 0x0A48 (fwrite crd->data, 300 B)
    if (cardOutPath != null && m.dtor_crdOpt.is_Crd) {
      var crdState = ((TI59__Machine.CrdOpt_Crd)m.dtor_crdOpt)._crd;
      var cardOpt = crdState.dtor_cardOut;
      if (cardOpt.is_Card) {
        var img = ((TI59__PrintCrd.OptCrd_Card)cardOpt)._img;
        var bytes = new byte[img.Count];
        for (long i = 0; i < img.Count; i++) bytes[i] = img.Select(i);
        File.WriteAllBytes(cardOutPath, bytes);
        Console.Error.WriteLine("open crd " + cardOutPath);
      }
    }
    else if (cardOutPath != null) {
      Console.Error.WriteLine("no card image (crd off never reached data)");
    }
    return 0;
  }

  // printf-vjerne formaty LOG volani C simulatoru (print.c/crd.c).
  // Vsechny LOG tvaru "PRINT_xxx[...]='...' " / "CRD.xxx[...]=.. " konci
  // mezerou; C je koncatenuje do radku, runner tiskne run cyklu na radku.
  static void EventLogString(TI59__PrintCrd._ITi59Event ev, StringBuilder sb) {
    if (ev.is_EvPrintChar) {
      var e = (TI59__PrintCrd.Ti59Event_EvPrintChar)ev;
      sb.Append("PRINT_CHAR[").Append(e._head).Append("]='").Append((char)e._ch.Value).Append("' ");
    } else if (ev.is_EvPrintFunc) {
      var e = (TI59__PrintCrd.Ti59Event_EvPrintFunc)ev;
      sb.Append("PRINT_FUNC[").Append(e._head).Append("]='").Append(RuneStr(e._s)).Append("' ");
    } else if (ev.is_EvPrintNotFound) {
      var e = (TI59__PrintCrd.Ti59Event_EvPrintNotFound)ev;
      sb.Append("PRINT[").Append(e._head).Append("]='").Append(e._code).Append(" not fund' ");
    } else if (ev.is_EvPrintClear) {
      var e = (TI59__PrintCrd.Ti59Event_EvPrintClear)ev;
      sb.Append("PRINT_CLEAR[").Append(e._head).Append("]='").Append(ParseStr(e._buf)).Append("' ");
    } else if (ev.is_EvPrint) {
      var e = (TI59__PrintCrd.Ti59Event_EvPrint)ev;
      sb.Append("PRINT[").Append(e._head).Append("]='").Append(ParseStr(e._buf)).Append("' ");
    } else if (ev.is_EvCrdRd) {
      var e = (TI59__PrintCrd.Ti59Event_EvCrdRd)ev;
      sb.Append("CRD.rd[").Append(e._pc.ToString("0000")).Append("]=").Append(e._data.ToString("x2")).Append(" ");
    } else if (ev.is_EvCrdWr) {
      var e = (TI59__PrintCrd.Ti59Event_EvCrdWr)ev;
      sb.Append("CRD.wr[").Append(e._pc.ToString("0000")).Append("]=").Append(e._data.ToString("x2")).Append(" ");
    } else if (ev.is_EvCrdOff) {
      // [crd.c:71] printf na stdout (ne do logu)
      var e = (TI59__PrintCrd.Ti59Event_EvCrdOff)ev;
      Console.Write("crd read/write " + e._pc + "\n");
    }
  }

  // %.20s - presne 20 znaku (buffer bez NUL)
  static string ParseStr(Dafny.ISequence<Dafny.Rune> s) {
    sb.Clear();
    foreach (var r in s) sb.Append((char)r.Value);
    return sb.ToString();
  }

  // [crd.c:102-108] fread obrazek karty (300 B; zbytek = 0x92 vypln)
  static byte[] LoadCard(string file) {
    var data = new byte[300];
    for (int i = 0; i < 300; i++) data[i] = 0x92;
    var bytes = File.ReadAllBytes(file);
    for (int i = 0; i < bytes.Length && i < 300; i++) data[i] = bytes[i];
    return data;
  }

  static Dafny.ISequence<Dafny.ISequence<byte>> EmptyRegs() {
    var rows = new Dafny.ISequence<byte>[30];
    for (int i = 0; i < 30; i++) {
      var row = new byte[16];
      rows[i] = Dafny.Sequence<byte>.FromArray(row);
    }
    return Dafny.Sequence<Dafny.ISequence<byte>>.FromArray(rows);
  }
}