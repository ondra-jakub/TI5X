using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// Validates the built-in Solid State Software modules generated from the original
/// TMC0540 CROM dumps (util/mklibfromdump): bank counts, sizes within the engine's
/// 1000-step library-bank cap, and the authentic ML-01 content of the Master Library.
/// </summary>
public class LibraryModuleTests
{
    private static readonly (string File, int Programs)[] BuiltInModules =
    [
        ("ml.ti5l", 25),    // -1- Master Library
        ("mod02.ti5l", 22), // -2- Applied Statistics
        ("mod03.ti5l", 17), // -3- Real Estate/Investment
        ("mod04.ti5l", 25), // -4- Surveying
        ("mod05.ti5l", 30), // -5- Marine Navigation
        ("mod06.ti5l", 23), // -6- Aviation
        ("mod07.ti5l", 21), // -7- Leisure Library
        ("mod08.ti5l", 15), // -8- Securities Analysis
        ("mod09.ti5l", 11), // -9- Business Decisions
        ("mod10.ti5l", 21), // -10- Math/Utilities
        ("mod11.ti5l", 19), // -11- Electrical Engineering
        ("mod12.ti5l", 17), // -12- Agriculture
        ("mod13.ti5l", 64), // -13- RPN Simulator
    ];

    [Test]
    public async Task All_built_in_modules_have_expected_program_counts()
    {
        foreach (var (file, programs) in BuiltInModules)
        {
            var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath(file));

            await Assert.That(module.Banks.Count).IsEqualTo(programs);
            foreach (var bank in module.Banks.Values)
            {
                await Assert.That(bank.Program.Length).IsLessThanOrEqualTo(1000)
                    .Because($"library banks are capped at 1000 steps ({file})");
            }
        }
    }

    [Test]
    public async Task Authentic_master_library_diagnostic_program_content()
    {
        var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath("ml.ti5l"));

        // authentic ML-01 from the TMC0541 CROM dump: 189 key codes starting with
        // 2nd Lbl CE, 0, STO 09, 2nd Deg, 2nd Fix 09, 2nd Lbl CLR, 2nd CP, 6, STO 01, 0
        await Assert.That(module.Banks[1].Program.Length).IsEqualTo(189);
        await Assert
            .That(module.Banks[1].Program.Take(13).ToArray())
            .IsEquivalentTo(new byte[] { 76, 24, 00, 42, 09, 60, 58, 09, 76, 25, 29, 06, 42 });

        // "62 00" (2nd Pgm Ind 00) used by the merged-codes test sits at offset 103
        await Assert.That((int)module.Banks[1].Program[103]).IsEqualTo(62);
        await Assert.That((int)module.Banks[1].Program[104]).IsEqualTo(0);
    }

    [Test]
    public async Task Generated_cards_carry_program_titles()
    {
        var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath("ml.ti5l"));

        await Assert.That(module.Banks[24].Card?.TitleLeft).IsEqualTo("UNIT CONVERSIONS (1)");
        await Assert.That(module.Banks[24].Card?.TitleRight).IsEqualTo("ML-24");
    }
}