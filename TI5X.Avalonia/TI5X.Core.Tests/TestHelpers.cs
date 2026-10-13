using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

public static class TestHelpers
{
    /// <summary>Lunar Lander program in TI-59 instruction codes (see docs/LUNAR_LANDER_SPEC.md).</summary>
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

    public static CalculatorEngine SetupEngineWithProgram(byte[] prog)
    {
        var engine = new CalculatorEngine();
        Array.Clear(engine.UserProgram, 0, engine.UserProgram.Length);
        Array.Copy(prog, engine.UserProgram, prog.Length);
        engine.Banks[0] = new ProgBank(engine.UserProgram);
        engine.CurBank = 0;
        engine.PC = 0;
        return engine;
    }

    public static async Task RunUntilHaltAsync(CalculatorEngine engine, int timeoutMs = 5000)
    {
        try
        {
            await engine.RunCompletion.WaitAsync(TimeSpan.FromMilliseconds(timeoutMs));
        }
        catch (TimeoutException)
        {
            throw new InvalidOperationException("Program execution timed out without halting on R/S.");
        }
    }

    public static async Task ExecuteTurnAsync(CalculatorEngine engine, double burn)
    {
        engine.SetX(burn);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_R_S);
        await RunUntilHaltAsync(engine);
    }

    public static async Task PressKeysAsync(CalculatorEngine engine, params int[] keys)
    {
        foreach (int key in keys)
        {
            engine.ProcessKey(key);
            await RunUntilHaltAsync(engine);
        }
    }

    public static int[] NumberKeys(int value)
    {
        int[] map = { KeyCodes.KEY_0, KeyCodes.KEY_1, KeyCodes.KEY_2, KeyCodes.KEY_3, KeyCodes.KEY_4,
                      KeyCodes.KEY_5, KeyCodes.KEY_6, KeyCodes.KEY_7, KeyCodes.KEY_8, KeyCodes.KEY_9 };
        return Array.ConvertAll(value.ToString().ToCharArray(), c => map[c - '0']);
    }

    public static CardDef LanderCard() => new()
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

    public const string LanderHelp =
        "TI-58 / TI-59 Lunar Lander Simulation.\n\nPress A to initialize the game (f=600, v=-500, x=2500).\nEnter desired propellant burn B and press R/S (or B) each 1-second turn.\nHUD displays combined status (-V.AAAA).\nPress C for remaining fuel, D for altitude, E for velocity.\nAfter a C/D/E query, enter the burn and press B (not R/S).\nSafe landing: v >= -5 shown in Fix 2. Crash: flashing error.";

    public static string AssetPath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Assets", fileName);
}
