using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TI5X.Avalonia.Core;

namespace TI5X.Avalonia.Tests;

public static class Program
{
    public static readonly byte[] LunarLanderProgram = new byte[]
    {
        // 000: Lbl A - Initialize Game
        76, 11,
        // 002: R00 = 600 (Fuel)
        06, 00, 00, 42, 00,
        // 007: R01 = -500 (Velocity)
        05, 00, 00, 94, 42, 01,
        // 013: R02 = 2500 (Altitude)
        02, 05, 00, 00, 42, 02,
        // 019: GTO C' (Jump to HUD)
        61, 18,

        // 021: Lbl B - Turn entry (Burn B in X)
        76, 12,
        // 023: R03 = requested burn B
        42, 03,
        // 025: T = requested burn B
        32,
        // 026: X = available fuel R00
        43, 00,
        // 028: If Fuel >= Burn goto Lbl A'
        77, 16,
        // 030: Otherwise Burn = remaining fuel
        43, 00, 42, 03,

        // 034: Lbl A' - Physics update
        76, 16,
        // 036: R00 = R00 - Burn
        43, 03, 22, 44, 00,
        // 041: a = Burn - 5 (R04 = a)
        43, 03, 75, 05, 95, 42, 04,
        // 048: R02 = R02 + v + a/2
        53, 43, 01, 85, 53, 43, 04, 55, 02, 54, 54, 44, 02,
        // 061: R01 = R01 + a
        43, 04, 44, 01,
        // 065: Check altitude x <= 0
        00, 32, 43, 02,
        // 069: If x < 0 goto Lbl B'
        22, 77, 17,
        // 072: If x == 0 goto Lbl B'
        67, 17,

        // 074: Lbl C' - HUD Formatter & Wait
        76, 18,
        // 076: Display = v - x / 10000
        53, 43, 01, 75, 53, 43, 02, 55, 01, 00, 00, 00, 00, 54, 54, 95,
        // 092: Fix 4
        58, 04,
        // 094: R/S (Wait for input)
        91,
        // 095: GTO B (Continue loop)
        61, 12,

        // 097: Lbl B' - Ground Touchdown
        76, 17,
        // 099: Reset altitude R02 = 0
        00, 42, 02,
        // 102: T = -5.0 (Safe landing limit)
        05, 94, 32,
        // 105: X = Touchdown velocity R01
        43, 01,
        // 107: If v >= -5 goto Lbl D' (Safe landing)
        77, 19,

        // 109: Lbl E' - CRASH State
        76, 10,
        // 111: Recall fatal velocity
        43, 01,
        // 113: Fix 2
        58, 02,
        // 115: Force Error 0 / 0
        00, 55, 00, 95,
        // 119: Halt in crash
        91,

        // 120: Lbl D' - SAFE LANDING
        76, 19,
        // 122: Recall touchdown velocity
        43, 01,
        // 124: Fix 2
        58, 02,
        // 126: Halt in victory
        91,

        // 127: Lbl C - Query Fuel
        76, 13,
        // 129: Show R00 & Halt
        43, 00, 91,

        // 132: Lbl D - Query Altitude
        76, 14,
        // 134: Show R02 & Halt
        43, 02, 91,

        // 137: Lbl E - Query Velocity
        76, 15,
        // 139: Show R01 & Halt
        43, 01, 91
    };

    public static int Main(string[] args)
    {
        Console.WriteLine("=== TI5X & Lunar Lander Automated Test Suite ===");
        int failures = 0;

        failures += RunTest("Basic Calculator Arithmetic & Priorities", TestBasicMath);
        failures += RunTest("Angle Modes & Conversions", TestAngleConversions);
        failures += RunTest("Lunar Lander Initialization (Lbl A)", TestLunarLanderInit);
        failures += RunTest("Lunar Lander Free Fall Trajectory (B=0)", TestLunarLanderFreeFall);
        failures += RunTest("Lunar Lander Equilibrium Hover (B=5)", TestLunarLanderHover);
        failures += RunTest("Lunar Lander Walkthrough Descent (Turns 1..6)", TestLunarLanderWalkthrough);
        failures += RunTest("Lunar Lander Fuel Clamping (B > Fuel)", TestLunarLanderFuelClamping);
        failures += RunTest("Lunar Lander Safe Touchdown Boundary (v >= -5.0)", TestLunarLanderSafeLanding);
        failures += RunTest("Lunar Lander Rebound Landing (v > 0)", TestLunarLanderReboundLanding);
        failures += RunTest("Lunar Lander Fatal Crash Boundary (v < -5.0)", TestLunarLanderCrash);
        failures += RunTest("Lunar Lander Telemetry Keys (Lbl C, D, E)", TestLunarLanderTelemetry);
        failures += RunTest("Lunar Lander Complete Winning Game (64,0,0,0,0,466)", TestLunarLanderWinningGame);
        failures += RunTest("Lunar Lander Resume With B After Query", TestLunarLanderResumeAfterQuery);
        failures += RunTest("Lunar Lander .ti5p Save, Load and Run Roundtrip", TestLunarLanderTi5pRoundtrip);
        failures += RunTest("Generate Assets/lunar_lander.ti5p", GenerateLanderAsset);

        if (failures == 0)
        {
            Console.WriteLine("\n[SUCCESS] All tests passed cleanly!");
            return 0;
        }
        else
        {
            Console.WriteLine($"\n[FAILURE] {failures} test(s) failed!");
            return 1;
        }
    }

    private static void GenerateLanderAsset()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string assetPath = Path.Combine(projectRoot, "Assets", "lunar_lander.ti5p");
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        var card = new CardDef
        {
            Style = "system",
            TitleLeft = "LUNAR LANDER",
            TitleRight = "TI-58 / TI-59",
            A = "INIT",
            B = "BURN",
            C = "FUEL",
            D = "ALT",
            E = "VEL",
            APrime = "PHYS",
            BPrime = "TOUCH",
            CPrime = "HUD",
            DPrime = "WIN",
            EPrime = "CRASH"
        };
        string help = "TI-58 / TI-59 Lunar Lander Simulation.\n\nPress A to initialize the game (f=600, v=-500, x=2500).\nEnter desired propellant burn B and press R/S (or B) each 1-second turn.\nHUD displays combined status (-V.AAAA).\nPress C for remaining fuel, D for altitude, E for velocity.\nAfter a C/D/E query, enter the burn and press B (not R/S).\nSafe landing: v >= -5 shown in Fix 2. Crash: flashing error.";

        PersistentState.SaveProgram(engine, assetPath, card, help);
        AssertTrue(File.Exists(assetPath), "Assets/lunar_lander.ti5p generated");
    }

    private static int RunTest(string name, Action testAction)
    {
        try
        {
            testAction();
            Console.WriteLine($"  [PASS] {name}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  [FAIL] {name}: {ex.Message}");
            return 1;
        }
    }

    private static void AssertEqual(double expected, double actual, double tolerance = 1e-4, string message = "")
    {
        if (Math.Abs(expected - actual) > tolerance)
        {
            throw new Exception($"Expected {expected}, got {actual}. {message}");
        }
    }

    private static void AssertStringEqual(string expected, string actual, string message = "")
    {
        if (expected != actual)
        {
            throw new Exception($"Expected \"{expected}\", got \"{actual}\". {message}");
        }
    }

    private static void AssertTrue(bool condition, string message = "")
    {
        if (!condition)
        {
            throw new Exception($"Condition failed: {message}");
        }
    }

    private static CalculatorEngine SetupEngineWithProgram(byte[] prog)
    {
        var engine = new CalculatorEngine();
        Array.Clear(engine.UserProgram, 0, engine.UserProgram.Length);
        Array.Copy(prog, engine.UserProgram, prog.Length);
        engine.Banks[0] = new ProgBank(engine.UserProgram);
        engine.CurBank = 0;
        engine.PC = 0;
        return engine;
    }

    private static void RunUntilHalt(CalculatorEngine engine, int timeoutMs = 5000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (engine.IsRunning && sw.ElapsedMilliseconds < timeoutMs)
        {
            Thread.Sleep(2);
        }
        if (sw.ElapsedMilliseconds >= timeoutMs)
        {
            throw new Exception("Program execution timed out without halting on R/S.");
        }
    }

    private static void ExecuteTurn(CalculatorEngine engine, double burn)
    {
        engine.SetX(burn);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        RunUntilHalt(engine);
    }

    private static void TestBasicMath()
    {
        var engine = new CalculatorEngine();
        engine.ClearAll();

        // 2 + 3 * 4 = 14
        engine.ExecuteKeyInteractive(KeyCodes.KEY_2);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_ADD);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_3);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_MUL);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_4);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_EQUALS);
        AssertEqual(14.0, engine.X, 1e-6, "AOS hierarchy 2 + 3 * 4");

        // 1 / 0 = Error
        engine.ClearAll();
        engine.ExecuteKeyInteractive(KeyCodes.KEY_1);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_DIV);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_EQUALS);
        AssertTrue(engine.State == CalculatorEngine.EngineState.Error, "Division by zero triggers error state");
    }

    private static void TestAngleConversions()
    {
        var engine = new CalculatorEngine();
        engine.ClearAll();

        // sin(30 deg) = 0.5
        engine.ExecuteKeyInteractive(KeyCodes.KEY_DEG);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_3);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_2ND);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_X_SQUARED); // SIN in 2nd
        AssertEqual(0.5, engine.X, 1e-6, "sin(30 deg) == 0.5");
    }

    private static void TestLunarLanderInit()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);

        AssertEqual(600.0, engine.Memories[0], 1e-4, "Fuel R00 == 600");
        AssertEqual(-500.0, engine.Memories[1], 1e-4, "Velocity R01 == -500");
        AssertEqual(2500.0, engine.Memories[2], 1e-4, "Altitude R02 == 2500");
        AssertEqual(-500.25, engine.X, 1e-4, "HUD display value == -500.25");
        AssertStringEqual("-500.2500", engine.DisplayString, "Fix 4 HUD display == -500.2500");
        AssertTrue(engine.State != CalculatorEngine.EngineState.Error, "No error on init");
    }

    private static void TestLunarLanderFreeFall()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);

        // Turn 1: Burn 0
        ExecuteTurn(engine, 0);
        AssertEqual(600.0, engine.Memories[0], 1e-4, "Turn 1 Fuel");
        AssertEqual(-505.0, engine.Memories[1], 1e-4, "Turn 1 Velocity: v = -500 - 5 = -505");
        AssertEqual(1997.5, engine.Memories[2], 1e-4, "Turn 1 Altitude: x = 2500 - 500 - 2.5 = 1997.5");

        // Turn 2: Burn 0
        ExecuteTurn(engine, 0);
        AssertEqual(600.0, engine.Memories[0], 1e-4, "Turn 2 Fuel");
        AssertEqual(-510.0, engine.Memories[1], 1e-4, "Turn 2 Velocity: v = -505 - 5 = -510");
        AssertEqual(1490.0, engine.Memories[2], 1e-4, "Turn 2 Altitude: x = 1997.5 - 505 - 2.5 = 1490.0");
    }

    private static void TestLunarLanderWinningGame()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);

        double[] burns = { 64, 0, 0, 0, 0 };
        double[] alts = { 2029.5, 1586.0, 1137.5, 684.0, 225.5 };
        double[] vels = { -441, -446, -451, -456, -461 };
        for (int i = 0; i < burns.Length; ++i)
        {
            ExecuteTurn(engine, burns[i]);
            AssertEqual(alts[i], engine.Memories[2], 1e-6, $"Turn {i + 1} altitude");
            AssertEqual(vels[i], engine.Memories[1], 1e-6, $"Turn {i + 1} velocity");
            Console.WriteLine($"      t={i + 1} HUD {engine.DisplayString}");
        }
        AssertEqual(536.0, engine.Memories[0], 1e-6, "Fuel before final burn");

        ExecuteTurn(engine, 466);
        AssertEqual(70.0, engine.Memories[0], 1e-6, "Fuel left after landing");
        AssertEqual(0.0, engine.Memories[1], 1e-6, "Touchdown velocity");
        AssertEqual(0.0, engine.Memories[2], 1e-6, "Altitude clamped to 0");
        AssertTrue(engine.State != CalculatorEngine.EngineState.Error, "Safe landing has no error");
        AssertStringEqual("0.00", engine.DisplayString, "Victory display in Fix 2");
    }

    private static void TestLunarLanderResumeAfterQuery()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);
        ExecuteTurn(engine, 100);

        engine.ExecuteSymbolicJump(KeyCodes.KEY_C);
        RunUntilHalt(engine);
        AssertEqual(500.0, engine.X, 1e-6, "Fuel query");

        engine.SetX(100);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_B);
        RunUntilHalt(engine);
        AssertEqual(400.0, engine.Memories[0], 1e-6, "Turn 2 fuel after resuming with B");
        AssertEqual(1690.0, engine.Memories[2], 1e-6, "Turn 2 altitude after resuming with B");
        AssertStringEqual("-310.1690", engine.DisplayString, "HUD after resuming with B");
    }

    private static void TestLunarLanderHover()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);

        // Turn 1: Burn 5 (net a = 0)
        ExecuteTurn(engine, 5);
        AssertEqual(595.0, engine.Memories[0], 1e-4, "Hover Turn 1 Fuel == 595");
        AssertEqual(-500.0, engine.Memories[1], 1e-4, "Hover Turn 1 Velocity == -500");
        AssertEqual(2000.0, engine.Memories[2], 1e-4, "Hover Turn 1 Altitude == 2000.0");
        AssertStringEqual("-500.2000", engine.DisplayString, "Hover HUD == -500.2000");
    }

    private static void TestLunarLanderWalkthrough()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);

        // t=1: B=100 -> R00=500, R02=2047.5, R01=-405, HUD=-405.2048
        ExecuteTurn(engine, 100);
        AssertEqual(500.0, engine.Memories[0], 1e-4, "t=1 Fuel");
        AssertEqual(2047.5, engine.Memories[2], 1e-4, "t=1 Alt");
        AssertEqual(-405.0, engine.Memories[1], 1e-4, "t=1 Vel");
        AssertStringEqual("-405.2048", engine.DisplayString, "t=1 HUD");

        // t=2: B=100 -> R00=400, R02=1690.0, R01=-310, HUD=-310.1690
        ExecuteTurn(engine, 100);
        AssertEqual(400.0, engine.Memories[0], 1e-4, "t=2 Fuel");
        AssertEqual(1690.0, engine.Memories[2], 1e-4, "t=2 Alt");
        AssertEqual(-310.0, engine.Memories[1], 1e-4, "t=2 Vel");
        AssertStringEqual("-310.1690", engine.DisplayString, "t=2 HUD");

        // t=3: B=100 -> R00=300, R02=1427.5, R01=-215, HUD=-215.1428
        ExecuteTurn(engine, 100);
        AssertEqual(300.0, engine.Memories[0], 1e-4, "t=3 Fuel");
        AssertEqual(1427.5, engine.Memories[2], 1e-4, "t=3 Alt");
        AssertEqual(-215.0, engine.Memories[1], 1e-4, "t=3 Vel");
        AssertStringEqual("-215.1428", engine.DisplayString, "t=3 HUD");

        // t=4: B=100 -> R00=200, R02=1260.0, R01=-120, HUD=-120.1260
        ExecuteTurn(engine, 100);
        AssertEqual(200.0, engine.Memories[0], 1e-4, "t=4 Fuel");
        AssertEqual(1260.0, engine.Memories[2], 1e-4, "t=4 Alt");
        AssertEqual(-120.0, engine.Memories[1], 1e-4, "t=4 Vel");
        AssertStringEqual("-120.1260", engine.DisplayString, "t=4 HUD");

        // t=5: B=100 -> R00=100, R02=1187.5, R01=-25, HUD=-25.1188
        ExecuteTurn(engine, 100);
        AssertEqual(100.0, engine.Memories[0], 1e-4, "t=5 Fuel");
        AssertEqual(1187.5, engine.Memories[2], 1e-4, "t=5 Alt");
        AssertEqual(-25.0, engine.Memories[1], 1e-4, "t=5 Vel");
        AssertStringEqual("-25.1188", engine.DisplayString, "t=5 HUD");

        // t=6: B=25 -> R00=75, R02=1172.5, R01=-5, HUD=-5.1173
        ExecuteTurn(engine, 25);
        AssertEqual(75.0, engine.Memories[0], 1e-4, "t=6 Fuel");
        AssertEqual(1172.5, engine.Memories[2], 1e-4, "t=6 Alt");
        AssertEqual(-5.0, engine.Memories[1], 1e-4, "t=6 Vel");
        AssertStringEqual("-5.1173", engine.DisplayString, "t=6 HUD");
    }

    private static void TestLunarLanderFuelClamping()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);

        // Pilot requests B=700 when only R00=600 is available
        ExecuteTurn(engine, 700);
        AssertEqual(0.0, engine.Memories[0], 1e-4, "Fuel clamped to 0");
        // Effective burn was 600, a = 595, x = 2500 - 500 + 595/2 = 2297.5, v = -500 + 595 = 95
        AssertEqual(95.0, engine.Memories[1], 1e-4, "Velocity after clamped burn");
        AssertEqual(2297.5, engine.Memories[2], 1e-4, "Altitude after clamped burn");

        // Next turn with 0 fuel requesting B=100
        ExecuteTurn(engine, 100);
        AssertEqual(0.0, engine.Memories[0], 1e-4, "Fuel remains 0");
        // Effective burn = 0, a = -5, x = 2297.5 + 95 - 2.5 = 2390.0, v = 95 - 5 = 90
        AssertEqual(90.0, engine.Memories[1], 1e-4, "Velocity after unpowered turn");
        AssertEqual(2390.0, engine.Memories[2], 1e-4, "Altitude after unpowered turn");
    }

    private static void TestLunarLanderSafeLanding()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        // Preset state right above surface
        engine.Memories[0] = 50.0;  // Fuel
        engine.Memories[1] = -10.0; // Velocity
        engine.Memories[2] = 8.0;   // Altitude
        engine.ExecuteFix(4);

        // Burn B = 11 -> a = 6, x_new = 8 + (-10) + 3 = 1.0 (v = -4.0, still flying)
        // Burn B = 9  -> a = 4, x_new = 1 + (-4) + 2.0 = -1.0 (touchdown!)
        // v_touchdown = -4 + 4 = 0.0 -> Safe landing!
        engine.SetX(11);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_B);
        RunUntilHalt(engine);
        AssertEqual(1.0, engine.Memories[2], 1e-4, "Turn 1 altitude above ground");
        AssertEqual(-4.0, engine.Memories[1], 1e-4, "Turn 1 velocity == -4.0");

        ExecuteTurn(engine, 9);
        AssertEqual(0.0, engine.Memories[2], 1e-4, "Altitude clamped to 0 on touchdown");
        AssertEqual(0.0, engine.Memories[1], 1e-4, "Touchdown velocity == 0.0");
        AssertEqual(0.0, engine.X, 1e-4, "Final display velocity == 0.00");
        AssertStringEqual("0.00", engine.DisplayString, "Safe landing Fix 2 display == 0.00");
        AssertTrue(engine.State != CalculatorEngine.EngineState.Error, "Safe touchdown must NOT trigger error");
    }

    private static void TestLunarLanderReboundLanding()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        // Spec test: x=50, v=-200, B=255 -> step ends at x=-25, v=+50 (positive rebound >= -5.0)
        engine.Memories[0] = 300.0; // Fuel
        engine.Memories[1] = -200.0;// Velocity
        engine.Memories[2] = 50.0;  // Altitude
        engine.ExecuteFix(4);

        engine.SetX(255);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_B);
        RunUntilHalt(engine);

        AssertEqual(0.0, engine.Memories[2], 1e-4, "Altitude clamped to 0 on rebound contact");
        AssertEqual(50.0, engine.Memories[1], 1e-4, "Velocity after turn");
        AssertStringEqual("50.00", engine.DisplayString, "Rebound velocity display in Fix 2");
        AssertTrue(engine.State != CalculatorEngine.EngineState.Error, "Rebound with v >= -5 must land safely without error per spec");
    }

    private static void TestLunarLanderCrash()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        // Free fall from 2500 ft until crash
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);

        // Burn 0 continuously
        for (int i = 0; i < 50; ++i)
        {
            if (engine.Memories[2] <= 0 || engine.State == CalculatorEngine.EngineState.Error) break;
            ExecuteTurn(engine, 0);
        }

        AssertEqual(0.0, engine.Memories[2], 1e-4, "Altitude clamped to 0 on crash");
        AssertTrue(engine.Memories[1] < -5.0, "Impact velocity was fatal (< -5.0 ft/s)");
        AssertTrue(engine.State == CalculatorEngine.EngineState.Error, "Fatal impact must trigger error state (0 / 0)");
    }

    private static void TestLunarLanderTelemetry()
    {
        var engine = SetupEngineWithProgram(LunarLanderProgram);
        engine.ExecuteSymbolicJump(KeyCodes.KEY_A);
        RunUntilHalt(engine);

        // Check Lbl C (Fuel)
        engine.ExecuteSymbolicJump(KeyCodes.KEY_C);
        RunUntilHalt(engine);
        AssertEqual(600.0, engine.X, 1e-4, "Key C returns fuel");

        // Check Lbl D (Altitude)
        engine.ExecuteSymbolicJump(KeyCodes.KEY_D);
        RunUntilHalt(engine);
        AssertEqual(2500.0, engine.X, 1e-4, "Key D returns altitude");

        // Check Lbl E (Velocity)
        engine.ExecuteSymbolicJump(KeyCodes.KEY_E);
        RunUntilHalt(engine);
        AssertEqual(-500.0, engine.X, 1e-4, "Key E returns velocity");
    }

    private static void TestLunarLanderTi5pRoundtrip()
    {
        string tmpPath = Path.Combine(Path.GetTempPath(), "lunar_lander_test.ti5p");
        try
        {
            var saveEngine = SetupEngineWithProgram(LunarLanderProgram);
            var card = new CardDef
            {
                Style = "system",
                TitleLeft = "LUNAR LANDER",
                TitleRight = "TI-58 / TI-59",
                A = "INIT",
                B = "BURN",
                C = "FUEL",
                D = "ALT",
                E = "VEL",
                APrime = "PHYS",
                BPrime = "TOUCH",
                CPrime = "HUD",
                DPrime = "WIN",
                EPrime = "CRASH"
            };
            string help = "TI-58/59 Lunar Lander Simulation.\nPress A to start, enter burn and press R/S each turn.";

            PersistentState.SaveProgram(saveEngine, tmpPath, card, help);
            AssertTrue(File.Exists(tmpPath), ".ti5p file created");

            var loadEngine = new CalculatorEngine();
            PersistentState.LoadProgram(loadEngine, tmpPath);

            AssertEqual(LunarLanderProgram.Length, 142, 0.1, "Program size is 142 steps");
            for (int i = 0; i < LunarLanderProgram.Length; ++i)
            {
                if (loadEngine.UserProgram[i] != LunarLanderProgram[i])
                {
                    throw new Exception($"Byte mismatch at step {i:D3}: expected {LunarLanderProgram[i]:D2}, got {loadEngine.UserProgram[i]:D2}");
                }
            }

            AssertStringEqual("LUNAR LANDER", loadEngine.Banks[0].Card?.TitleLeft ?? "", "Card title loaded");
            AssertStringEqual("INIT", loadEngine.Banks[0].Card?.A ?? "", "Card A label loaded");

            // Execute initialized program from loaded file
            loadEngine.ExecuteSymbolicJump(KeyCodes.KEY_A);
            RunUntilHalt(loadEngine);
            AssertEqual(600.0, loadEngine.Memories[0], 1e-4, "Loaded prog Fuel");
            AssertEqual(-500.0, loadEngine.Memories[1], 1e-4, "Loaded prog Velocity");
            AssertEqual(2500.0, loadEngine.Memories[2], 1e-4, "Loaded prog Altitude");
        }
        finally
        {
            if (File.Exists(tmpPath)) File.Delete(tmpPath);
        }
    }
}
