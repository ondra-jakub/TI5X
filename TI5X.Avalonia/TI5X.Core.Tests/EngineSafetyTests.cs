using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// Safety regressions: execution lifecycle (stop/restart races), memory bounds
/// (partition vs. bank length) and single-step determinism.
/// </summary>
public class EngineSafetyTests
{
    [Test]
    public async Task Stop_then_restart_does_not_kill_the_new_run()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);

        engine.ExecuteSymbolicJump(KeyCodes.KEY_A); // start run 1
        engine.StopProgram();                       // cancel run 1 immediately
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A); // start run 2 (new generation)
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.IsRunning).IsFalse();
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
        await Assert.That(engine.DisplayString).IsEqualTo("-500.2500");
    }

    [Test]
    public async Task Rapid_stop_restart_cycle_stays_consistent()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        for (int i = 0; i < 5; ++i)
        {
            engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
            engine.StopProgram();
        }
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.IsRunning).IsFalse();
        await Assert.That(engine.DisplayString).IsEqualTo("-500.2500");
    }

    [Test]
    public async Task Zero_Op17_then_List_Op8_and_SST_stay_in_bounds()
    {
        var engine = new CalculatorEngine();
        // 0 Op 17 claims 2000 logical steps while bank 0 holds 960 bytes.
        engine.SetX(0);
        engine.ExecuteOp(17, ind: false);
        await Assert.That(engine.GetMaxProg()).IsEqualTo(960);

        engine.ExecuteList();            // walks the usable range
        engine.ExecuteOp(8, ind: false); // lists labels
        engine.StepProgram();            // one step from PC = 0
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task Short_library_bank_operations_stay_in_bounds()
    {
        var engine = new CalculatorEngine();
        var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath("ml.ti5l"));
        await Assert.That(module.Banks.ContainsKey(1)).IsTrue();
        engine.Banks[1] = module.Banks[1];
        engine.CurBank = 1;
        engine.PC = 0;

        int bankLength = module.Banks[1].Program.Length; // 127 bytes, not 1000
        await Assert.That(engine.GetMaxProg()).IsEqualTo(bankLength);

        engine.ExecuteList();            // walks the whole bank
        engine.ExecuteOp(8, ind: false);  // label listing

        // Running into the short bank must end in a defined state, never a crash.
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.IsRunning).IsFalse();
    }

    [Test]
    public async Task SST_steps_exactly_one_instruction()
    {
        // Program: Lbl 0, RCL 05, R/S
        var engine = TestHelpers.SetupEngineWithProgram(new byte[] { 76, 00, 43, 05, 91 });
        engine.Memories[5] = 42.0;

        engine.StepProgram(); // Lbl 0 -> skips label byte, PC = 2
        await Assert.That(engine.PC).IsEqualTo(2);

        engine.StepProgram(); // RCL 05 -> X = 42
        await Assert.That(engine.PC).IsEqualTo(4);
        await Assert.That(engine.X).IsEqualTo(42.0).Within(1e-9);

        engine.StepProgram(); // R/S -> stop, no crash
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
    }
}
