using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace TI5X.Avalonia.Core;

public class LibraryModule
{
    public const string CalcMimeType = "application/vnd.nz.gen.geek_central.ti5x";

    /// <summary>
    /// Sanity cap for a single ZIP entry from an untrusted .ti5p/.ti5l file.
    /// A tiny crafted archive can claim a huge decompressed entry, so the
    /// declared length is checked before the content is read into memory.
    /// </summary>
    public const long MaxEntryBytes = 4 * 1024 * 1024;

    public string? ModuleHelp { get; set; }
    public Dictionary<int, ProgBank> Banks { get; } = new();

    private static void AssertEntrySize(ZipArchiveEntry entry)
    {
        if (entry.Length > MaxEntryBytes)
        {
            throw new InvalidDataException(
                $"ZIP entry '{entry.Name}' is too large ({entry.Length} bytes, limit {MaxEntryBytes}).");
        }
    }

    /// <summary>Reads a UTF-8 text entry from an untrusted archive with a size cap.</summary>
    public static string ReadTextEntry(ZipArchiveEntry entry)
    {
        AssertEntrySize(entry);
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }

    public static LibraryModule LoadFromStream(Stream stream)
    {
        var mod = new LibraryModule();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

        // Check for general help
        var helpEntry = archive.GetEntry("help");
        if (helpEntry != null)
        {
            mod.ModuleHelp = ReadTextEntry(helpEntry);
        }

        // Scan entries
        foreach (var entry in archive.Entries)
        {
            if (entry.Name.StartsWith("prog") && entry.Name.Length == 6 && int.TryParse(entry.Name.Substring(4), out int bankNr))
            {
                byte[] progBytes = ReadProgramEntry(entry);
                CardDef? card = null;
                string? help = null;

                // Look for cardnn
                var cardEntry = archive.GetEntry($"card{bankNr:D2}");
                if (cardEntry != null)
                {
                    card = ReadCardEntry(cardEntry);
                }

                // Look for helpnn
                var hEntry = archive.GetEntry($"help{bankNr:D2}");
                if (hEntry != null)
                {
                    help = ReadTextEntry(hEntry);
                }

                mod.Banks[bankNr] = new ProgBank(progBytes, card, help);
            }
        }

        return mod;
    }

    public static LibraryModule LoadFromFile(string filePath)
    {
        using var fs = File.OpenRead(filePath);
        return LoadFromStream(fs);
    }

    public static byte[] ReadProgramEntry(ZipArchiveEntry entry)
    {
        AssertEntrySize(entry);
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        string xml = reader.ReadToEnd();
        var doc = XDocument.Parse(xml);
        var progElement = doc.Root?.Element("calc")?.Element("prog");
        if (progElement != null)
        {
            string content = progElement.Value;
            var tokens = content.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var bytes = new byte[tokens.Length];
            for (int i = 0; i < tokens.Length; ++i)
            {
                if (byte.TryParse(tokens[i], out byte val))
                {
                    bytes[i] = val;
                }
            }
            return bytes;
        }
        return Array.Empty<byte>();
    }

    public static CardDef? ReadCardEntry(ZipArchiveEntry entry)
    {
        try
        {
            AssertEntrySize(entry);
            using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
            string xml = reader.ReadToEnd();
            var doc = XDocument.Parse(xml);
            var root = doc.Root;
            if (root == null) return null;

            var card = new CardDef
            {
                Style = root.Attribute("style")?.Value ?? "system"
            };

            foreach (var el in root.Elements("label"))
            {
                string slot = el.Attribute("slot")?.Value ?? "";
                string val = el.Value; // simple text extract

                switch (slot.ToLowerInvariant())
                {
                    case "titleleft": card.TitleLeft = val; break;
                    case "titleright": card.TitleRight = val; break;
                    case "subtitleleft": card.SubtitleLeft = val; break;
                    case "subtitleright": card.SubtitleRight = val; break;
                    case "a": card.A = val; break;
                    case "b": card.B = val; break;
                    case "c": card.C = val; break;
                    case "d": card.D = val; break;
                    case "e": card.E = val; break;
                    case "aprime": card.APrime = val; break;
                    case "bprime": card.BPrime = val; break;
                    case "cprime": card.CPrime = val; break;
                    case "dprime": card.DPrime = val; break;
                    case "eprime": card.EPrime = val; break;
                    case "primecenter": card.PrimeCenter = val; break;
                    case "baseleft": card.BaseLeft = val; break;
                    case "baseright": card.BaseRight = val; break;
                }
            }

            return card;
        }
        catch
        {
            return null;
        }
    }
}
