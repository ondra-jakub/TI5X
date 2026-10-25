using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// Clear Entry (CE) semantics, verified against the TMC0500 microcode oracle
/// (ti59-dafny tests/traces + Ti59Runner):
/// - 2 + 3 = CE + 1 = → 6 (CE keeps a finished result),
/// - 12 CE 5 = → 5 (CE clears a number being typed),
/// - 0 1/x → flashing; CE only stops the flashing.
/// The "( ce × ..." idiom of the original Master Library programs (ML-24, ML-25)
/// relies on the first behavior.
/// </summary>
public class CeAndLibraryEntryTests
{
    [Test]
    public async Task Ce_keeps_finished_result()
    {
        var engine = new CalculatorEngine();
        engine.ClearAll();
        engine.ExecuteKeyInteractive(KeyCodes.KEY_2);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_ADD);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_3);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_EQUALS);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_CE);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_ADD);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_1);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_EQUALS);
        // oracle: 6. (CE neprosazoval 0)
        await Assert.That(engine.X).IsEqualTo(6.0).Within(1e-9);
    }

    [Test]
    public async Task Ce_clears_running_entry()
    {
        var engine = new CalculatorEngine();
        engine.ClearAll();
        engine.ExecuteKeyInteractive(KeyCodes.KEY_1);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_2);

        await Assert.That(engine.State == CalculatorEngine.EngineState.Entry).IsTrue();
        engine.ExecuteKeyInteractive(KeyCodes.KEY_CE);
        await Assert.That(engine.DisplayString.Replace(" ", "")).IsEqualTo("0.");
        engine.ExecuteKeyInteractive(KeyCodes.KEY_5);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_EQUALS);
        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-9);
    }

    [Test]
    public async Task Ce_only_stops_flashing_after_error()
    {
        var engine = new CalculatorEngine();
        engine.ClearAll();
        engine.ExecuteKeyInteractive(KeyCodes.KEY_0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_RECIPROCAL);
        await Assert.That(engine.State == CalculatorEngine.EngineState.Error).IsTrue();
        string errorDisplay = engine.DisplayString;
        engine.ExecuteKeyInteractive(KeyCodes.KEY_CE);
        await Assert.That(engine.State == CalculatorEngine.EngineState.Result).IsTrue();
        // the display is not wiped; CE only stops the flashing
        await Assert.That(engine.DisplayString).IsEqualTo(errorDisplay);
    }

    private static void LoadMasterLibrary(CalculatorEngine engine)
    {
        var module = LibraryModule.LoadFromFile(TestHelpers.AssetPath("ml.ti5l"));
        foreach (var kv in module.Banks)
        {
            engine.Banks[kv.Key] = kv.Value;
        }
    }

    [Test]
    public async Task Library_input_survives_ce_in_ml24()
    {
        // ML-24 starts "( ce × ...": with the no-op CE the operand 6 survives
        // 6 [in->cm] → 15.24 (the real manual's example; oracle 15.24)
        var engine = new CalculatorEngine();
        engine.ClearAll();
        LoadMasterLibrary(engine);

        engine.ExecuteKeyInteractive(KeyCodes.KEY_2ND);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_PGM);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_2);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_4);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_6);

        engine.ExecuteKeyInteractive(KeyCodes.KEY_A);
        if (engine.IsRunning && engine.RunCompletion != null) engine.RunCompletion.Wait();
        await Assert.That(engine.X).IsEqualTo(15.24).Within(1e-6);
    }

    [Test]
    public async Task Library_input_survives_ce_in_ml25()
    {
        // ML-25 starts "( ( ce − 32 ) ÷ 1.8 )": 41 °F → 5 °C
        var engine = new CalculatorEngine();
        engine.ClearAll();
        LoadMasterLibrary(engine);

        engine.ExecuteKeyInteractive(KeyCodes.KEY_2ND);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_PGM);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_2);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_5);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_4);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_1);

        engine.ExecuteKeyInteractive(KeyCodes.KEY_A);
        if (engine.IsRunning && engine.RunCompletion != null) engine.RunCompletion.Wait();
        await Assert.That(engine.X).IsEqualTo(5.0).Within(1e-6);
    }
}