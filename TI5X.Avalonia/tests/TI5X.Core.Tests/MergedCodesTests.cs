using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// TI-59 standard instruction semantics in program execution: merged IND codes,
/// RTN, two-byte branch targets, operand bytes and the number-before-R/S rule.
/// </summary>
public class MergedCodesTests
{
    [Test]
    public async Task Merged_STO_IND_stores_indirectly()
    {
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 72, 05, 91 });
        engine.Memories[5] = 7.0; // indirect through R05
        engine.Memories[7] = 0.0;
        engine.SetX(42.0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.Memories[7]).IsEqualTo(42.0).Within(1e-9);
    }

    [Test]
    public async Task Merged_RCL_IND_recalls_indirectly()
    {
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 73, 05, 91 });
        engine.Memories[5] = 8.0;
        engine.Memories[8] = 123.0;
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(123.0).Within(1e-9);
    }

    [Test]
    public async Task Merged_OP_IND_runs_operation_from_register()
    {
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 84, 05, 91 });
        engine.Memories[5] = 20.0; // Op 20 = increment R0
        engine.Memories[0] = 0.0;
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.Memories[0]).IsEqualTo(1.0).Within(1e-9);
    }

    [Test]
    public async Task RTN_from_keyboard_call_stops_the_program()
    {
        // Lbl A: RCL 00, RTN, R/S
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 76, 11, 43, 00, 92, 91 });
        engine.Memories[0] = 77.0;
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.IsRunning).IsFalse();
        await Assert.That(engine.X).IsEqualTo(77.0).Within(1e-9);
    }

    [Test]
    public async Task SBR_from_program_continues_after_RTN()
    {
        // 00: SBR B' | 02: RCL 03 | 04: R/S | 05: Lbl B' | 07: STO 03 | 09: RTN
        var engine = TestHelpers.SetupEngineWithProgram(new byte[]
        {
            71, 12, 43, 03, 91, 76, 12, 42, 03, 92
        });
        engine.SetX(5.0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.Memories[3]).IsEqualTo(5.0).Within(1e-9);
        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-9);
        await Assert.That(engine.IsRunning).IsFalse();
    }

    [Test]
    public async Task GTO_with_two_byte_address_jumps_correctly()
    {
        // 00: GTO 007 | 03: R/S | 04: digit 2, R/S | 07: digit 5, R/S
        var engine = TestHelpers.SetupEngineWithProgram(new byte[]
        {
            61, 00, 07, 91, 02, 91, 00, 05, 91
        });
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-9);
    }

    [Test]
    public async Task False_conditional_skips_two_byte_target()
    {
        // 00: x>=t 007 (false: X < T) | 03: digit 5, R/S | 07: digit 2, R/S
        var engine = TestHelpers.SetupEngineWithProgram(new byte[]
        {
            77, 00, 07, 05, 91, 00, 00, 02, 91
        });
        engine.SetX(0.0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_X_SWAP_T); // T = 0
        engine.SetX(0.0);
        engine.T = 1.0; // ensure X < T so the branch is not taken
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-9);
    }

    [Test]
    public async Task Number_entered_before_RS_is_kept()
    {
        // Program: digit 7, R/S -> display must show 7, not the previous X.
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 05, 91 });
        engine.SetX(0.0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-9);
    }

    [Test]
    public async Task HIR_is_a_defined_no_op()
    {
        // 82 (HIR) must not execute as digit 1; the program shows 5.
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 82, 05, 91 });
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-9);
    }

    [Test]
    public async Task Operand_byte_above_nine_addresses_register_directly()
    {
        // STO with operand byte 72 -> register R72
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 05, 42, 72, 91 });
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.Memories[72]).IsEqualTo(5.0).Within(1e-9);
    }

    [Test]
    public async Task Label_call_in_program_continues_after_RTN()
    {
        // 0: call A | 1: RCL 03 | 3: R/S | 4: Lbl A | 6: STO 03 | 8: RTN
        var engine = TestHelpers.SetupEngineWithProgram(new byte[]
        {
            11, 43, 03, 91, 76, 11, 42, 03, 92
        });
        engine.SetX(5.0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.Memories[3]).IsEqualTo(5.0).Within(1e-9);
        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-9); // proves the run continued past RTN
        await Assert.That(engine.IsRunning).IsFalse();
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task SST_over_label_call_stays_single_step()
    {
        var engine = TestHelpers.SetupEngineWithProgram(new byte[]
        {
            11, 43, 03, 91, 76, 11, 42, 03, 92
        });
        engine.StepProgram(); // executes the call to Lbl A (PC 0 -> 6)

        await Assert.That(engine.IsRunning).IsFalse(); // SST must not start a background run
        await Assert.That(engine.PC).IsEqualTo(6);
    }

    [Test]
    public async Task INV_Fix_in_program_does_not_steal_next_instruction()
    {
        // "22 58" = INV Fix (no operand); the following digit instruction must survive.
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 22, 58, 05, 91 });
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-9);
        await Assert.That(engine.CurDecimals).IsEqualTo(-1);
        await Assert.That(engine.DisplayString).IsEqualTo("5.");
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task Library_bank01_merged_PGM_IND_switches_bank()
    {
        var engine = new CalculatorEngine();
        var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath("ml.ti5l"));
        engine.Banks[1] = module.Banks[1];
        engine.CurBank = 1;
        engine.PC = 50; // position of "62 00" (Pgm Ind 00) in bank 01

        engine.StepProgram();

        await Assert.That(engine.CurBank).IsEqualTo(0);
        await Assert.That(engine.PC).IsEqualTo(0);
    }
}
