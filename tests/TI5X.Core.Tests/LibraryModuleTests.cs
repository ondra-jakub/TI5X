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

    [Test]
    public async Task Master_library_cards_carry_key_button_labels()
    {
        // the authentic mlsrc card templates list the entry points (A-E keys and
        // the prime row) of every program; without them the card strip shows no
        // buttons at all
        var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath("ml.ti5l"));

        // ML-01 diagnostic: full-width rows with boxed key names
        var diagnostic = module.Banks[1].Card;
        await Assert.That(diagnostic?.PrimeCenter).IsEqualTo("DIAGNOSTIC: <button>SBR</button> <button>=</button>");
        await Assert.That(diagnostic?.BaseLeft).IsEqualTo("L.R. INIT: <button>SBR</button> <button>CLR</button>");
        await Assert.That(diagnostic?.BaseRight).IsEqualTo("PRINT: mm <button>STO</button> 00");

        // ML-03 matrix arithmetic: A-E cells with subscript markup
        var ml03 = module.Banks[3].Card;
        await Assert.That(ml03?.A).IsEqualTo("m,n");
        await Assert.That(ml03?.B).IsEqualTo("j;a<sub>ij</sub>");
        await Assert.That(ml03?.BPrime).IsEqualTo("i;x<sub>i</sub>");
        await Assert.That(ml03?.CPrime).IsEqualTo("<rarrow/>Ax");

        // ML-24 unit conversions: conversion directions on all five keys
        var ml24 = module.Banks[24].Card;
        await Assert.That(ml24?.A).IsEqualTo("in <rarrow/> cm");
        await Assert.That(ml24?.EPrime).IsEqualTo("km <rarrow/> n mi");
    }

    [Test]
    public async Task Card_markup_decode_produces_styled_runs()
    {
        var runs = CardLabelMarkup.Decode("DIAGNOSTIC: <button>SBR</button> <button>=</button>");
        await Assert.That(runs.Count).IsEqualTo(4);
        await Assert.That(runs[0].Text).IsEqualTo("DIAGNOSTIC: ");
        await Assert.That(runs[1].Style).IsEqualTo(CardLabelRunStyle.Button);
        await Assert.That(runs[1].Text).IsEqualTo("SBR");
        await Assert.That(runs[3].Style).IsEqualTo(CardLabelRunStyle.Button);

        var arrow = CardLabelMarkup.Decode("in <rarrow/> cm");
        await Assert.That(CardLabelMarkup.ToPlainText("in <rarrow/> cm")).IsEqualTo("in → cm");

        var sub = CardLabelMarkup.Decode("j;a<sub>ij</sub>");
        await Assert.That(sub[0].Style).IsEqualTo(CardLabelRunStyle.Normal);
        await Assert.That(sub[^1].Style).IsEqualTo(CardLabelRunStyle.Subscript);
        await Assert.That(sub[^1].Text).IsEqualTo("ij");

        // plain labels without markup decode to a single normal run
        var plain = CardLabelMarkup.Decode("LUNAR LANDER");
        await Assert.That(plain.Count).IsEqualTo(1);
        await Assert.That(plain[0].Style).IsEqualTo(CardLabelRunStyle.Normal);
        await Assert.That(plain[0].Text).IsEqualTo("LUNAR LANDER");
    }
}