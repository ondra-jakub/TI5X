using System.IO.Compression;
using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// Regression tests for authentic Solid State Software module (TMC0541 / Master
/// Library) execution: key-code labels, key-callable labels, library program
/// selection and the running ML-01 diagnostics.
/// </summary>
public class LibraryDiagnosticTests
{
    private static CalculatorEngine SetupWithMasterLibrary()
    {
        var engine = new CalculatorEngine();
        var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath("ml.ti5l"));
        foreach (var (nr, bank) in module.Banks)
            engine.Banks[nr] = bank;
        return engine;
    }

    [Test]
    public async Task SBR_equals_calls_the_ml01_selftest_label()
    {
        // ML-01 program 1 starts with "Lbl =" at offset 22; the user instruction
        // card asks for 2nd Pgm 01 + SBR =.
        var engine = SetupWithMasterLibrary();
        engine.ExecutePgm(1, ind: false);
        await Assert.That(engine.CurBank).IsEqualTo(1);

        engine.ExecuteSbr(95, isSymbolic: true, ind: false, fromInteractive: false);
        await Assert.That(engine.PC).IsEqualTo(24); // first instruction after "Lbl ="
        await Assert.That(engine.State).IsNotEqualTo(CalculatorEngine.EngineState.Error);
    }

    [Test]
    public async Task Keyboard_SBR_equals_is_accepted_as_label_operand()
    {
        var engine = SetupWithMasterLibrary();
        engine.ExecutePgm(1, ind: false);

        engine.ProcessKey(KeyCodes.KEY_SBR);
        engine.ProcessKey(KeyCodes.KEY_EQUALS);
        engine.StopProgram();

        await Assert.That(engine.State).IsNotEqualTo(CalculatorEngine.EngineState.Error);
        await Assert.That(engine.PC).IsEqualTo(24);
    }

    [Test]
    public async Task Symbolic_target_without_label_falls_back_to_legacy_address()
    {
        // "71 55" is a one-byte address 55 in hand-written programs when no
        // key-code label 55 exists; authentic modules need the label form.
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 71, 55, 91 });
        engine.SetX(42.0); // a recognizable marker at address 55
        engine.PC = 0;
        engine.StepProgram();
        await Assert.That(engine.PC).IsEqualTo(55);
        await Assert.That(engine.State).IsNotEqualTo(CalculatorEngine.EngineState.Error);
    }

    [Test]
    public async Task Dsz_zero_does_not_decrement_and_does_not_jump()
    {
        // The original reduces the magnitude toward zero: 0 stays 0 and the
        // branch is not taken. bytes: 00, STO 01, DSZ 1 ->004, R/S, 05, R/S
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 00, 42, 01, 97, 01, 00, 04, 91, 05, 91 });
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.Memories[1]).IsEqualTo(0.0);
        await Assert.That(engine.X).IsEqualTo(0.0).Within(1e-12);
        await Assert.That(engine.State).IsNotEqualTo(CalculatorEngine.EngineState.Error);
    }

    [Test]
    public async Task Sigma_plus_uses_real_machine_y_in_x_x_in_t()
    {
        var engine = new CalculatorEngine();
        engine.SetX(2.0); // y on display
        engine.T = 1.0;   // x in T
        engine.ProcessKey(KeyCodes.KEY_SIGMA_PLUS);

        await Assert.That(engine.Memories[1]).IsEqualTo(2.0).Within(1e-9);  // Σy
        await Assert.That(engine.Memories[4]).IsEqualTo(1.0).Within(1e-9);  // Σx
        await Assert.That(engine.Memories[6]).IsEqualTo(2.0).Within(1e-9);  // Σxy
    }

    [Test]
    public async Task P_to_R_follows_real_machine_register_convention()
    {
        // 5 (r, in T), 30 (theta, in X), P/R -> y = 2.5 in X, x = 4.3301270... in T
        var engine = new CalculatorEngine();
        engine.SetX(30.0);
        engine.T = 5.0;
        engine.ProcessKey(KeyCodes.KEY_P_TO_R);

        await Assert.That(engine.X).IsEqualTo(2.5).Within(1e-9);
        await Assert.That(engine.T).IsEqualTo(4.3301270189221932).Within(1e-8);
    }

    [Test]
    public async Task Op14_computes_correlation_like_real_machine()
    {
        var engine = new CalculatorEngine();
        // points (1,2) and (2,4) -> r = 1
        engine.Memories[1] = 6.0;
        engine.Memories[2] = 20.0;
        engine.Memories[3] = 2.0;
        engine.Memories[4] = 3.0;
        engine.Memories[5] = 5.0;
        engine.Memories[6] = 10.0;

        engine.ExecuteOp(14, ind: false);
        await Assert.That(engine.X).IsEqualTo(1.0).Within(1e-8);
    }

    [Test]
    public async Task Pgm_in_program_selects_bank_for_next_call_and_returns_to_caller()
    {
        // bank 01 offset 103: 62 00 = Pgm Ind 00 (R00=0 -> user program bank),
        // followed by 11 = call Lbl A of the selected program.
        var engine = SetupWithMasterLibrary();
        engine.UserProgram[0] = KeyCodes.KEY_LBL;
        engine.UserProgram[1] = KeyCodes.KEY_A;
        engine.UserProgram[2] = KeyCodes.OP_INV_SBR; // RTN right back
        engine.Banks[0] = new ProgBank(engine.UserProgram);
        engine.CurBank = 1;
        engine.PC = 103;

        engine.StepProgram(); // "Pgm Ind 00": selects bank, does not jump
        await Assert.That(engine.CurBank).IsEqualTo(1);
        await Assert.That(engine.PC).IsEqualTo(105);

        engine.StepProgram(); // label call A -> user bank
        await Assert.That(engine.CurBank).IsEqualTo(0);
        await Assert.That(engine.PC).IsEqualTo(2);

        engine.StepProgram(); // RTN returns to the calling bank
        await Assert.That(engine.CurBank).IsEqualTo(1);
        await Assert.That(engine.PC).IsEqualTo(106);
        await Assert.That(engine.State).IsNotEqualTo(CalculatorEngine.EngineState.Error);
    }

    [Test]
    public async Task CP_inside_library_bank_keeps_program_counter()
    {
        // ML-01 init routine runs "CP" while executing in library bank 01.
        var engine = SetupWithMasterLibrary();
        engine.CurBank = 1;
        engine.PC = 50;

        engine.ExecuteKeyInteractive(KeyCodes.KEY_CP);

        await Assert.That(engine.PC).IsEqualTo(50);
        await Assert.That(engine.UserProgram.AsSpan().ToArray().All(b => b == 0)).IsTrue();
    }

    [Test]
    public async Task Merged_STO_IND_finishes_preceding_number_entry()
    {
        // Authentic ML-01 init: 6 STO 01, 0, ST* 01 -> must store 0 (the just
        // entered number), not the stale 6.
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 06, 42, 01, 00, 72, 01, 91 });
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.Memories[1]).IsEqualTo(6.0).Within(1e-9);
        await Assert.That(engine.Memories[6]).IsEqualTo(0.0).Within(1e-9);
    }

    [Test]
    public async Task ML01_module_check_runs_through_init_lr_and_cross_bank_call()
    {
        // 2nd Pgm 01, SBR =. Progress marker: everything up to the final
        // self-test comparison runs (init, P/R + Σ+ chain, cross-bank "Pgm 15
        // SBR DMS" call, parenthesised subexpression), then the checksum branch
        // fails at the intentional "0, 1/x" error path (bank 01 offset 72/73).
        // TODO: full pass needs R8/R9 scratch-register fidelity of P/R and the
        // statistics Op routines (see ODCHYLKY_A_OMEZENI.md, "Knihovní moduly").
        var engine = SetupWithMasterLibrary();
        engine.Printer = new PrinterEngine();

        engine.ProcessKey(KeyCodes.KEY_2ND);
        engine.ProcessKey(KeyCodes.KEY_LRN);
        engine.ProcessKey(KeyCodes.KEY_0);
        engine.ProcessKey(KeyCodes.KEY_1);
        engine.ProcessKey(KeyCodes.KEY_SBR);
        engine.ProcessKey(KeyCodes.KEY_EQUALS);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.PC).IsEqualTo(74);
        await Assert.That(engine.State).IsEqualTo(CalculatorEngine.EngineState.Error);
        // the cross-bank call really ran in bank 15 and came back
        await Assert.That(engine.Memories[9]).IsEqualTo(99991.0).Within(1e-6);
    }
}