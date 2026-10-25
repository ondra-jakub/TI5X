using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// Regression tests for the interactive module-listing sequence: "2nd Pgm nn"
/// (2nd + LRN) selects a library bank and "2nd List" (2nd + "=") prints the
/// selected bank's steps on the PC-100A tape. Also guards against confusing
/// the List key with the look-alike Ins/Del keys (2nd+SST / 2nd+BST), which
/// print nothing outside LRN, and against switching to a missing bank.
/// </summary>
public class ListDiagnosticTests
{
    private static (CalculatorEngine Engine, PrinterEngine Printer) SetupWithMasterLibrary()
    {
        var engine = new CalculatorEngine();
        var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath("ml.ti5l"));
        foreach (var (nr, bank) in module.Banks)
            engine.Banks[nr] = bank;
        var printer = new PrinterEngine();
        engine.Printer = printer;
        return (engine, printer);
    }

    [Test]
    public async Task Correct_keys_2nd_Pgm_01_then_2nd_equals_List_prints_bank_01()
    {
        var (engine, printer) = SetupWithMasterLibrary();

        // 2nd Pgm 01  (LRN is the base key under "Pgm", = maps to List)
        await TestHelpers.PressKeysAsync(engine,
            KeyCodes.KEY_2ND, KeyCodes.KEY_LRN, KeyCodes.KEY_0, KeyCodes.KEY_1);

        await Assert.That(engine.CurBank).IsEqualTo(1);
        await Assert.That(engine.State).IsNotEqualTo(CalculatorEngine.EngineState.Error);
        await Assert.That(printer.TapeLines.Count).IsEqualTo(0);

        // 2nd List  (2nd + "=")
        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_2ND, KeyCodes.KEY_EQUALS);

        int expectedLines = Math.Min(engine.GetMaxProg(), PrinterEngine.MaxLines);
        await Assert.That(printer.TapeLines.Count).IsEqualTo(expectedLines);

        // First tape line must match the first byte of bank 01, format "PPP  KK MNE",
        // padded to the 20-column tape by the TMC0251 model.
        byte first = engine.Banks[1]!.Program[0];
        await Assert.That(printer.TapeLines[0])
            .IsEqualTo($"{0:D3}  {first:D2} {PrinterEngine.ListMnemonic(first)}".PadRight(20));

        // And the last printed step is the newest tape line.
        int lastIdx = expectedLines - 1;
        byte last = engine.Banks[1]!.Program[lastIdx];
        await Assert.That(printer.TapeLines[^1])
            .IsEqualTo($"{lastIdx:D3}  {last:D2} {PrinterEngine.ListMnemonic(last)}".PadRight(20));
    }

    [Test]
    public async Task Wrong_keys_2nd_SST_Ins_and_2nd_BST_Del_print_nothing()
    {
        var (engine, printer) = SetupWithMasterLibrary();

        // 2nd Pgm 01 to select bank 01 first
        await TestHelpers.PressKeysAsync(engine,
            KeyCodes.KEY_2ND, KeyCodes.KEY_LRN, KeyCodes.KEY_0, KeyCodes.KEY_1);
        await Assert.That(engine.CurBank).IsEqualTo(1);

        // 2nd + SST = Ins -> no print outside LRN
        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_2ND, KeyCodes.KEY_SST);
        await Assert.That(printer.TapeLines.Count).IsEqualTo(0);

        // 2nd + BST = Del -> no print outside LRN
        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_2ND, KeyCodes.KEY_BST);
        await Assert.That(printer.TapeLines.Count).IsEqualTo(0);

        await Assert.That(engine.State).IsNotEqualTo(CalculatorEngine.EngineState.Error);
    }

    [Test]
    public async Task Pgm_with_missing_bank_sets_error_and_does_not_switch_bank()
    {
        var (engine, printer) = SetupWithMasterLibrary();

        // The rebuilt ml.ti5l carries banks 01-25: 2nd Pgm 26 must blink Error.
        await TestHelpers.PressKeysAsync(engine,
            KeyCodes.KEY_2ND, KeyCodes.KEY_LRN, KeyCodes.KEY_2, KeyCodes.KEY_6);

        await Assert.That(engine.State).IsEqualTo(CalculatorEngine.EngineState.Error);
        await Assert.That(engine.CurBank).IsEqualTo(0);
        await Assert.That(printer.TapeLines.Count).IsEqualTo(0);
    }

    [Test]
    public async Task List_of_loaded_user_program_fits_on_the_tape()
    {
        // A loaded .ti5p program (Lunar Lander, steps 000-140) is in bank 0.
        // List starts at the current step (PC = 0) and prints through the end
        // of the default partition (960 steps); with the 1000-line tape the
        // whole listing stays visible, including the program start at 000.
        var engine = new CalculatorEngine();
        var printer = new PrinterEngine();
        engine.Printer = printer;

        PersistentState.LoadProgram(engine, TestHelpers.AssetPath("lunar_lander.ti5p"));

        await Assert.That(engine.CurBank).IsEqualTo(0);
        await Assert.That(engine.Banks[0]!.Program[0]).IsEqualTo((byte)76); // LBL
        await Assert.That(engine.Banks[0]!.Program[1]).IsEqualTo((byte)11); // A
        await Assert.That(engine.PC).IsEqualTo(0);

        // 2nd "=" = List
        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_2ND, KeyCodes.KEY_EQUALS);

        await Assert.That(printer.TapeLines.Count).IsEqualTo(engine.GetMaxProg());

        // The listing starts at step 000, so the program itself is on the tape.
        byte p0 = engine.Banks[0]!.Program[0];
        await Assert.That(printer.TapeLines[0])
            .IsEqualTo($"{0:D3}  {p0:D2} {PrinterEngine.ListMnemonic(p0)}".PadRight(20));
        await Assert.That(printer.TapeLines[0]).Contains("LBL");

        // First empty step after the program.
        int end = 141;
        byte pend = engine.Banks[0]!.Program[end];
        await Assert.That(printer.TapeLines[end])
            .IsEqualTo($"{end:D3}  {pend:D2} {PrinterEngine.ListMnemonic(pend)}".PadRight(20));
    }

    [Test]
    public async Task List_prints_from_the_current_step_like_the_original()
    {
        var engine = new CalculatorEngine();
        var printer = new PrinterEngine();
        engine.Printer = printer;

        PersistentState.LoadProgram(engine, TestHelpers.AssetPath("lunar_lander.ti5p"));

        // GTO 141 outside LRN moves the program pointer; 2nd List starts there.
        await TestHelpers.PressKeysAsync(engine,
            KeyCodes.KEY_GTO, KeyCodes.KEY_1, KeyCodes.KEY_4, KeyCodes.KEY_1,
            KeyCodes.KEY_2ND, KeyCodes.KEY_EQUALS);

        await Assert.That(engine.PC).IsEqualTo(141);

        int max = engine.GetMaxProg();
        await Assert.That(printer.TapeLines.Count).IsEqualTo(max - 141);

        byte first = engine.Banks[0]!.Program[141];
        await Assert.That(printer.TapeLines[0])
            .IsEqualTo($"{141:D3}  {first:D2} {PrinterEngine.ListMnemonic(first)}".PadRight(20));

        // Nothing from steps 000-140 was printed.
        await Assert.That(printer.TapeLines[1])
            .IsEqualTo($"{142:D3}  {engine.Banks[0]!.Program[142]:D2} {PrinterEngine.ListMnemonic(engine.Banks[0]!.Program[142])}".PadRight(20));
    }
}