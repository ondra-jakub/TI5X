using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace TI5X.Avalonia.Core;

public static class PersistentState
{
    public const string CalcMimeType = "application/vnd.nz.gen.geek_central.ti5x";

    public static void SaveProgram(CalculatorEngine engine, string filePath, CardDef? card = null, string? help = null)
    {
        using var fs = File.Create(filePath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);

        // 1. mimetype entry
        var mime = zip.CreateEntry("mimetype", CompressionLevel.NoCompression);
        using (var writer = new StreamWriter(mime.Open(), Encoding.ASCII))
        {
            writer.Write(CalcMimeType);
        }

        // 2. prog00
        var progEntry = zip.CreateEntry("prog00", CompressionLevel.Optimal);
        using (var writer = new StreamWriter(progEntry.Open(), Encoding.UTF8))
        {
            var sb = new StringBuilder();
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<state>");
            sb.AppendLine("  <calc>");
            sb.Append("    <prog>");
            for (int i = 0; i < engine.UserProgram.Length; ++i)
            {
                if (i > 0) sb.Append(' ');
                sb.Append($"{engine.UserProgram[i]:D2}");
            }
            sb.AppendLine("</prog>");
            sb.AppendLine("  </calc>");
            sb.AppendLine("</state>");
            writer.Write(sb.ToString());
        }

        // 3. card00 if present
        if (card != null)
        {
            var cardEntry = zip.CreateEntry("card00", CompressionLevel.Optimal);
            using var writer = new StreamWriter(cardEntry.Open(), Encoding.UTF8);
            var sb = new StringBuilder();
            sb.AppendLine($"<carddef style=\"{card.Style}\">");
            if (!string.IsNullOrEmpty(card.TitleLeft)) sb.AppendLine($"<label slot=\"titleleft\">{card.TitleLeft}</label>");
            if (!string.IsNullOrEmpty(card.TitleRight)) sb.AppendLine($"<label slot=\"titleright\">{card.TitleRight}</label>");
            if (!string.IsNullOrEmpty(card.A)) sb.AppendLine($"<label slot=\"a\">{card.A}</label>");
            if (!string.IsNullOrEmpty(card.B)) sb.AppendLine($"<label slot=\"b\">{card.B}</label>");
            if (!string.IsNullOrEmpty(card.C)) sb.AppendLine($"<label slot=\"c\">{card.C}</label>");
            if (!string.IsNullOrEmpty(card.D)) sb.AppendLine($"<label slot=\"d\">{card.D}</label>");
            if (!string.IsNullOrEmpty(card.E)) sb.AppendLine($"<label slot=\"e\">{card.E}</label>");
            if (!string.IsNullOrEmpty(card.APrime)) sb.AppendLine($"<label slot=\"aprime\">{card.APrime}</label>");
            if (!string.IsNullOrEmpty(card.BPrime)) sb.AppendLine($"<label slot=\"bprime\">{card.BPrime}</label>");
            if (!string.IsNullOrEmpty(card.CPrime)) sb.AppendLine($"<label slot=\"cprime\">{card.CPrime}</label>");
            if (!string.IsNullOrEmpty(card.DPrime)) sb.AppendLine($"<label slot=\"dprime\">{card.DPrime}</label>");
            if (!string.IsNullOrEmpty(card.EPrime)) sb.AppendLine($"<label slot=\"eprime\">{card.EPrime}</label>");
            sb.AppendLine("</carddef>");
            writer.Write(sb.ToString());
        }

        // 4. help00 if present
        if (!string.IsNullOrEmpty(help))
        {
            var helpEntry = zip.CreateEntry("help00", CompressionLevel.Optimal);
            using var writer = new StreamWriter(helpEntry.Open(), Encoding.UTF8);
            writer.Write(help);
        }
    }

    public static void LoadProgram(CalculatorEngine engine, string filePath)
    {
        using var fs = File.OpenRead(filePath);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Read);

        var progEntry = zip.GetEntry("prog00");
        if (progEntry != null)
        {
            byte[] bytes = LibraryModule.ReadProgramEntry(progEntry);
            Array.Clear(engine.UserProgram, 0, engine.UserProgram.Length);
            Array.Copy(bytes, engine.UserProgram, Math.Min(bytes.Length, engine.UserProgram.Length));

            CardDef? card = null;
            var cardEntry = zip.GetEntry("card00");
            if (cardEntry != null)
            {
                card = LibraryModule.ReadCardEntry(cardEntry);
            }

            string? help = null;
            var helpEntry = zip.GetEntry("help00");
            if (helpEntry != null)
            {
                using var reader = new StreamReader(helpEntry.Open(), Encoding.UTF8);
                help = reader.ReadToEnd();
            }

            engine.Banks[0] = new ProgBank(engine.UserProgram, card, help);
            engine.CurBank = 0;
            engine.PC = 0;
        }
    }
}
