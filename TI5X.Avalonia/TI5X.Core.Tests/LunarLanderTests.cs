using TUnit.Core;
using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

/// <summary>
/// Lunar Lander scenarios from the original test suite. Each test owns its engine
/// and awaits the engine's run completion task, so the suite is parallel-safe.
/// </summary>
public class LunarLanderTests
{
    [Test]
    public async Task Initialization_Lbl_A()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.Memories[0]).IsEqualTo(600.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-500.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(2500.0).Within(1e-4);
        await Assert.That(engine.X).IsEqualTo(-500.25).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("-500.2500");
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task Free_fall_trajectory_B0()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        // Turn 1: Burn 0
        await TestHelpers.ExecuteTurnAsync(engine, 0);
        await Assert.That(engine.Memories[0]).IsEqualTo(600.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-505.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(1997.5).Within(1e-4);

        // Turn 2: Burn 0
        await TestHelpers.ExecuteTurnAsync(engine, 0);
        await Assert.That(engine.Memories[0]).IsEqualTo(600.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-510.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(1490.0).Within(1e-4);
    }

    [Test]
    public async Task Equilibrium_hover_B5()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        // Turn 1: Burn 5 (net a = 0)
        await TestHelpers.ExecuteTurnAsync(engine, 5);
        await Assert.That(engine.Memories[0]).IsEqualTo(595.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-500.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(2000.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("-500.2000");
    }

    [Test]
    public async Task Walkthrough_descent_turns_1_to_6()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        // t=1: B=100 -> R00=500, R02=2047.5, R01=-405, HUD=-405.2048
        await TestHelpers.ExecuteTurnAsync(engine, 100);
        await Assert.That(engine.Memories[0]).IsEqualTo(500.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(2047.5).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-405.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("-405.2048");

        // t=2: B=100 -> R00=400, R02=1690.0, R01=-310, HUD=-310.1690
        await TestHelpers.ExecuteTurnAsync(engine, 100);
        await Assert.That(engine.Memories[0]).IsEqualTo(400.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(1690.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-310.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("-310.1690");

        // t=3: B=100 -> R00=300, R02=1427.5, R01=-215, HUD=-215.1428
        await TestHelpers.ExecuteTurnAsync(engine, 100);
        await Assert.That(engine.Memories[0]).IsEqualTo(300.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(1427.5).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-215.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("-215.1428");

        // t=4: B=100 -> R00=200, R02=1260.0, R01=-120, HUD=-120.1260
        await TestHelpers.ExecuteTurnAsync(engine, 100);
        await Assert.That(engine.Memories[0]).IsEqualTo(200.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(1260.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-120.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("-120.1260");

        // t=5: B=100 -> R00=100, R02=1187.5, R01=-25, HUD=-25.1188
        await TestHelpers.ExecuteTurnAsync(engine, 100);
        await Assert.That(engine.Memories[0]).IsEqualTo(100.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(1187.5).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-25.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("-25.1188");

        // t=6: B=25 -> R00=75, R02=1172.5, R01=-5, HUD=-5.1173
        await TestHelpers.ExecuteTurnAsync(engine, 25);
        await Assert.That(engine.Memories[0]).IsEqualTo(75.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(1172.5).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-5.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("-5.1173");
    }

    [Test]
    public async Task Fuel_clamping_B_greater_than_fuel()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        // Pilot requests B=700 when only R00=600 is available
        await TestHelpers.ExecuteTurnAsync(engine, 700);
        await Assert.That(engine.Memories[0]).IsEqualTo(0.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(95.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(2297.5).Within(1e-4);

        // Next turn with 0 fuel requesting B=100
        await TestHelpers.ExecuteTurnAsync(engine, 100);
        await Assert.That(engine.Memories[0]).IsEqualTo(0.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(90.0).Within(1e-4);
        await Assert.That(engine.Memories[2]).IsEqualTo(2390.0).Within(1e-4);
    }

    [Test]
    public async Task Safe_touchdown_boundary_v_ge_minus_5()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        // Preset state right above surface
        engine.Memories[0] = 50.0;   // Fuel
        engine.Memories[1] = -10.0;  // Velocity
        engine.Memories[2] = 8.0;    // Altitude
        engine.ExecuteFix(4);

        // Burn B = 11 -> a = 6, x_new = 8 + (-10) + 3 = 1.0 (v = -4.0, still flying)
        // Burn B = 9  -> a = 4, x_new = 1 + (-4) + 2.0 = -1.0 (touchdown!)
        // v_touchdown = -4 + 4 = 0.0 -> Safe landing!
        engine.SetX(11);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_B);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.Memories[2]).IsEqualTo(1.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(-4.0).Within(1e-4);

        await TestHelpers.ExecuteTurnAsync(engine, 9);
        await Assert.That(engine.Memories[2]).IsEqualTo(0.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(0.0).Within(1e-4);
        await Assert.That(engine.X).IsEqualTo(0.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("0.00");
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task Rebound_landing_v_positive()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        // Spec test: x=50, v=-200, B=255 -> step ends at x=-25, v=+50 (positive rebound >= -5.0)
        engine.Memories[0] = 300.0;  // Fuel
        engine.Memories[1] = -200.0; // Velocity
        engine.Memories[2] = 50.0;   // Altitude
        engine.ExecuteFix(4);

        engine.SetX(255);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_B);
        await TestHelpers.RunUntilHaltAsync(engine);

        await Assert.That(engine.Memories[2]).IsEqualTo(0.0).Within(1e-4);
        await Assert.That(engine.Memories[1]).IsEqualTo(50.0).Within(1e-4);
        await Assert.That(engine.DisplayString).IsEqualTo("50.00");
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task Fatal_crash_boundary_v_less_than_minus_5()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        // Free fall from 2500 ft until crash
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        // Burn 0 continuously
        for (int i = 0; i < 50; ++i)
        {
            if (engine.Memories[2] <= 0 || engine.State == CalculatorEngine.EngineState.Error) break;
            await TestHelpers.ExecuteTurnAsync(engine, 0);
        }

        await Assert.That(engine.Memories[2]).IsEqualTo(0.0).Within(1e-4);
        await Assert.That(engine.Memories[1] < -5.0).IsTrue();
        await Assert.That(engine.State == CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task Telemetry_keys_Lbl_C_D_E()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        // Check Lbl C (Fuel)
        engine.ExecuteSymbolicJump(KeyCodes.KEY_C);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(600.0).Within(1e-4);

        // Check Lbl D (Altitude)
        engine.ExecuteSymbolicJump(KeyCodes.KEY_D);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(2500.0).Within(1e-4);

        // Check Lbl E (Velocity)
        engine.ExecuteSymbolicJump(KeyCodes.KEY_E);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(-500.0).Within(1e-4);
    }

    [Test]
    public async Task Complete_winning_game_64_0_0_0_0_466()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);

        double[] burns = { 64, 0, 0, 0, 0 };
        double[] alts = { 2029.5, 1586.0, 1137.5, 684.0, 225.5 };
        double[] vels = { -441, -446, -451, -456, -461 };
        for (int i = 0; i < burns.Length; ++i)
        {
            await TestHelpers.ExecuteTurnAsync(engine, burns[i]);
            await Assert.That(engine.Memories[2]).IsEqualTo(alts[i]).Within(1e-6);
            await Assert.That(engine.Memories[1]).IsEqualTo(vels[i]).Within(1e-6);
        }
        await Assert.That(engine.Memories[0]).IsEqualTo(536.0).Within(1e-6);

        await TestHelpers.ExecuteTurnAsync(engine, 466);
        await Assert.That(engine.Memories[0]).IsEqualTo(70.0).Within(1e-6);
        await Assert.That(engine.Memories[1]).IsEqualTo(0.0).Within(1e-6);
        await Assert.That(engine.Memories[2]).IsEqualTo(0.0).Within(1e-6);
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();
        await Assert.That(engine.DisplayString).IsEqualTo("0.00");
    }

    [Test]
    public async Task Resume_with_B_after_query()
    {
        var engine = TestHelpers.SetupEngineWithProgram(TestHelpers.LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        await TestHelpers.RunUntilHaltAsync(engine);
        await TestHelpers.ExecuteTurnAsync(engine, 100);

        engine.ExecuteSymbolicJump(KeyCodes.KEY_C);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.X).IsEqualTo(500.0).Within(1e-6);

        engine.SetX(100);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_B);
        await TestHelpers.RunUntilHaltAsync(engine);
        await Assert.That(engine.Memories[0]).IsEqualTo(400.0).Within(1e-6);
        await Assert.That(engine.Memories[2]).IsEqualTo(1690.0).Within(1e-6);
        await Assert.That(engine.DisplayString).IsEqualTo("-310.1690");
    }

    [Test]
    public async Task Asset_played_with_physical_keys_GUI_path()
    {
        var engine = new CalculatorEngine();
        PersistentState.LoadProgram(engine, TestHelpers.AssetPath("lunar_lander.ti5p"));
        var card = engine.Banks[engine.CurBank]?.Card;
        await Assert.That(card?.A ?? "").IsEqualTo("INIT");
        await Assert.That(card?.B ?? "").IsEqualTo("BURN");
        await Assert.That(card?.EPrime ?? "").IsEqualTo("CRASH");

        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_A);
        await Assert.That(engine.DisplayString).IsEqualTo("-500.2500");

        int[] burns = { 64, 0, 0, 0, 0 };
        string[] huds = { "-441.2030", "-446.1586", "-451.1138", "-456.0684", "-461.0226" };
        for (int i = 0; i < burns.Length; ++i)
        {
            await TestHelpers.PressKeysAsync(engine, TestHelpers.NumberKeys(burns[i]));
            await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_R_S);
            await Assert.That(engine.DisplayString).IsEqualTo(huds[i]);
        }

        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_C);
        await Assert.That(engine.X).IsEqualTo(536.0).Within(1e-6);

        await TestHelpers.PressKeysAsync(engine, TestHelpers.NumberKeys(466));
        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_B);
        await Assert.That(engine.DisplayString).IsEqualTo("0.00");
        await Assert.That(engine.State != CalculatorEngine.EngineState.Error).IsTrue();

        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_RCL, KeyCodes.KEY_0, KeyCodes.KEY_0);
        await Assert.That(engine.X).IsEqualTo(70.0).Within(1e-6);
    }

    [Test]
    public async Task Crash_then_manual_RCL_GUI_path()
    {
        var engine = new CalculatorEngine();
        PersistentState.LoadProgram(engine, TestHelpers.AssetPath("lunar_lander.ti5p"));
        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_A);
        for (int i = 0; i < 20 && engine.State != CalculatorEngine.EngineState.Error; ++i)
        {
            await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_0, KeyCodes.KEY_R_S);
        }
        await Assert.That(engine.State == CalculatorEngine.EngineState.Error).IsTrue();
        double impact = engine.Memories[1];

        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_CLR, KeyCodes.KEY_E);
        await Assert.That(engine.X).IsEqualTo(impact).Within(1e-6);

        await TestHelpers.PressKeysAsync(engine, KeyCodes.KEY_CLR, KeyCodes.KEY_RCL, KeyCodes.KEY_0, KeyCodes.KEY_1);
        await Assert.That(engine.X).IsEqualTo(impact).Within(1e-6);
    }
}
