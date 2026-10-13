using System.IO.Compression;
using System.Text;
using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

public class Ti5pRoundtripTests
{
    [Test]
    public async Task Save_load_and_run_roundtrip()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), $"ti5x_roundtrip_{Guid.NewGuid():N}.ti5p");
        try
        {
            var saveEngine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
            string help = "TI-58/59 Lunar Lander Simulation.\nPress A to start, enter burn and press R/S each turn.";

            PersistentState.SaveProgram(saveEngine, tmpPath, TestHelpers.LanderCard(), help);
            await Assert.That(File.Exists(tmpPath)).IsTrue();

            var loadEngine = new CalculatorEngine();
            PersistentState.LoadProgram(loadEngine, tmpPath);

            await Assert.That(TestHelpers.LunarLanderProgram.Length).IsEqualTo(142);
            for (int i = 0; i < TestHelpers.LunarLanderProgram.Length; ++i)
            {
                if (loadEngine.UserProgram[i] != TestHelpers.LunarLanderProgram[i])
                {
                    await Assert.That(loadEngine.UserProgram[i]).IsEqualTo(TestHelpers.LunarLanderProgram[i]);
                }
            }

            await Assert.That(loadEngine.Banks[0]?.Card?.TitleLeft ?? "").IsEqualTo("LUNAR LANDER");
            await Assert.That(loadEngine.Banks[0]?.Card?.A ?? "").IsEqualTo("INIT");

            // Execute initialized program from loaded file
            loadEngine.ExecuteSymbolicJump(KeyCodes.KEY_A);
            await TestHelpers.RunUntilHaltAsync(loadEngine);
            await Assert.That(loadEngine.Memories[0]).IsEqualTo(600.0).Within(1e-4);
            await Assert.That(loadEngine.Memories[1]).IsEqualTo(-500.0).Within(1e-4);
            await Assert.That(loadEngine.Memories[2]).IsEqualTo(2500.0).Within(1e-4);
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    /// <summary>
    /// Replaces the old GenerateLanderAsset test: the committed asset must stay
    /// byte-identical (after decompression) to what the current code produces.
    /// The test never writes into the repository.
    /// </summary>
    [Test]
    public async Task Committed_asset_matches_generator_output()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), $"ti5x_asset_{Guid.NewGuid():N}.ti5p");
        try
        {
            var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
            PersistentState.SaveProgram(engine, tmpPath, TestHelpers.LanderCard(), TestHelpers.LanderHelp);

            await AssertEntriesMatch(tmpPath, TestHelpers.AssetPath("lunar_lander.ti5p"));
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    [Test]
    public async Task Card_with_special_characters_survives_roundtrip()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), $"ti5x_card_{Guid.NewGuid():N}.ti5p");
        try
        {
            var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
            var card = new CardDef
            {
                Style = "system",
                TitleLeft = "A & B <C> \"D\" 'E'",
                A = "x&y",
                EPrime = "</label>"
            };
            PersistentState.SaveProgram(engine, tmpPath, card, "help & <text>");

            var loaded = new CalculatorEngine();
            PersistentState.LoadProgram(loaded, tmpPath);
            var c = loaded.Banks[0]?.Card;
            await Assert.That(c?.TitleLeft).IsEqualTo("A & B <C> \"D\" 'E'");
            await Assert.That(c?.A).IsEqualTo("x&y");
            await Assert.That(c?.EPrime).IsEqualTo("</label>");
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    [Test]
    public async Task Oversized_zip_entry_is_rejected_without_reading()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), $"ti5x_big_{Guid.NewGuid():N}.ti5p");
        try
        {
            var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
            PersistentState.SaveProgram(engine, tmpPath); // prog00 so loading proceeds to help00

            // append an oversized help entry to the saved file
            using (var fs = File.Open(tmpPath, FileMode.Open))
            {
                fs.Seek(0, SeekOrigin.End);
                using var zip = new ZipArchive(fs, ZipArchiveMode.Update);
                var entry = zip.CreateEntry("help00");
                using var writer = new StreamWriter(entry.Open());
                writer.Write(new string('x', (int)(LibraryModule.MaxEntryBytes + 1)));
            }

            var loaded = new CalculatorEngine();
            InvalidDataException? caught = null;
            try
            {
                PersistentState.LoadProgram(loaded, tmpPath);
            }
            catch (InvalidDataException ex)
            {
                caught = ex;
            }
            await Assert.That(caught).IsNotNull();
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }

    private static async Task AssertEntriesMatch(string generatedPath, string committedPath)
    {
        await Assert.That(File.Exists(committedPath)).IsTrue();

        using var generated = ZipFile.OpenRead(generatedPath);
        using var committed = ZipFile.OpenRead(committedPath);

        var genNames = generated.Entries.Select(e => e.Name).Order().ToArray();
        var comNames = committed.Entries.Select(e => e.Name).Order().ToArray();
        await Assert.That(genNames).IsEquivalentTo(comNames);

        foreach (var genEntry in generated.Entries)
        {
            var comEntry = committed.GetEntry(genEntry.Name);
            await Assert.That(comEntry).IsNotNull();
            string genText = ReadEntry(genEntry);
            string comText = ReadEntry(comEntry!);
            if (genText != comText)
            {
                await Assert.That(comText).IsEqualTo(genText);
            }
        }
    }

    private static string ReadEntry(ZipArchiveEntry entry)
    {
        using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
