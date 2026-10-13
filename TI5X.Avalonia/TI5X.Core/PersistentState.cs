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

        // 3. card00 if present. Built with XElement so all values are escaped
        // automatically - a card loaded from an untrusted file cannot inject
        // arbitrary XML into a saved file (the old string builder did).
        if (card != null)
        {
            var cardEntry = zip.CreateEntry("card00", CompressionLevel.Optimal);
            using var writer = new StreamWriter(cardEntry.Open(), Encoding.UTF8);
            writer.Write(BuildCardXml(card));
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
                help = LibraryModule.ReadTextEntry(helpEntry);
            }

            engine.Banks[0] = new ProgBank(engine.UserProgram, card, help);
            engine.CurBank = 0;
            engine.PC = 0;
        }
    }

    private static string BuildCardXml(CardDef card)
    {
        var root = new XElement("carddef", new XAttribute("style", card.Style ?? ""));
        AddCardLabel(root, "titleleft", card.TitleLeft);
        AddCardLabel(root, "titleright", card.TitleRight);
        AddCardLabel(root, "subtitleleft", card.SubtitleLeft);
        AddCardLabel(root, "subtitleright", card.SubtitleRight);
        AddCardLabel(root, "a", card.A);
        AddCardLabel(root, "b", card.B);
        AddCardLabel(root, "c", card.C);
        AddCardLabel(root, "d", card.D);
        AddCardLabel(root, "e", card.E);
        AddCardLabel(root, "aprime", card.APrime);
        AddCardLabel(root, "bprime", card.BPrime);
        AddCardLabel(root, "cprime", card.CPrime);
        AddCardLabel(root, "dprime", card.DPrime);
        AddCardLabel(root, "eprime", card.EPrime);
        AddCardLabel(root, "primecenter", card.PrimeCenter);
        AddCardLabel(root, "baseleft", card.BaseLeft);
        AddCardLabel(root, "baseright", card.BaseRight);
        return root.ToString();
    }

    private static void AddCardLabel(XElement root, string slot, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            root.Add(new XElement("label", new XAttribute("slot", slot), value));
        }
    }
}
