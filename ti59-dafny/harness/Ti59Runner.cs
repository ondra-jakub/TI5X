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
      BigInteger at = parts.Length > 1 ? BigInteger.Parse(parts[1]) : BigInteger.Zero;
      BigInteger hold = parts.Length > 2 ? BigInteger.Parse(parts[2]) : BigInteger.Zero;
      bool toggle = parts.Length > 3 && parts[3] == "toggle";
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
    if (args.Length != 9) {
      Console.Error.WriteLine("usage: Ti59Runner rom0582 const0582 rom0583 const0583 rom0571B libdump|- script maxCycles traceOut");
      return 2;
    }
    var (rom1, base1) = LoadDumpRom(args[0]);
    var const1 = LoadDumpK(args[1]);
    var (rom2, base2) = LoadDumpRom(args[2]);
    var const2 = LoadDumpK(args[3]);
    var (rom3, base3) = LoadDumpRom(args[4]);
    byte[] libData = args[5] == "-" ? new byte[5000] : LoadDump8(args[5], 5000);
    if (libData.Length < 5000) { Console.Error.WriteLine("lib dump must be 5000 bytes"); return 2; }
    var script = LoadKeyScript(args[6]);
    long maxCycles = long.Parse(args[7]);
    var outPath = args[8];

    // postav stroj (poradi chipu jako bin/ti59.sh)
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
    var ramData = Dafny.Sequence<Dafny.ISequence<byte>>.FromArray(new Dafny.ISequence<byte>[30]);
    var ramsList = new TI59__Chips._IRam[4];
    for (int i = 0; i < 4; i++)
      ramsList[i] = TI59__Chips.__default.RamInit(EmptyRegs(), new BigInteger(30 * i));
    var rams = Dafny.Sequence<TI59__Chips._IRam>.FromArray(ramsList);
    var lib = TI59__Chips.__default.LibInit(SeqB(libData));
    var keys = TI59__Chips.__default.KeyInit(
      Dafny.Sequence<TI59__Chips._IKeyEvent>.FromArray(script.ToArray()));
    var disp = TI59__Chips.__default.DispInit();
    var bus = TI59__Bus.__default.BusInit();
    TI59__Machine._IMachine m = new TI59__Machine.Machine(cpu, broms, scoms, rams, lib, keys, disp, bus, false);

    if (!TI59__Machine.__default.MachineWF(m)) {
      Console.Error.WriteLine("MachineWF failed at init!");
      return 3;
    }

    using (var trace = new StreamWriter(outPath, false, new UTF8Encoding(false))) {
      long cycles = 0;
      long records = 0;
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
        if (((TI59__Machine.Machine)r.dtor__0).dtor_halted) break;
      }
      sw.Stop();
      trace.WriteLine("# cycles=" + cycles + " records=" + records + " halted=" + m.dtor_halted);
      trace.WriteLine("# display='" + RuneStr(m.dtor_disp.dtor_out1) + "'");
      trace.WriteLine("# secs=" + sw.Elapsed.TotalSeconds.ToString("0.00"));
    }
    return 0;
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