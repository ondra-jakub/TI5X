using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

public class BasicMathTests
{
    [Test]
    public async Task Basic_arithmetic_and_priorities()
    {
        var engine = new CalculatorEngine();
        engine.ClearAll();

        // 2 + 3 * 4 = 14 (AOS hierarchy)
        engine.ExecuteKeyInteractive(KeyCodes.KEY_2);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_ADD);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_3);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_MUL);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_4);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_EQUALS);
        await Assert.That(engine.X).IsEqualTo(14.0).Within(1e-6);

        // 1 / 0 = Error
        engine.ClearAll();
        engine.ExecuteKeyInteractive(KeyCodes.KEY_1);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_DIV);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_EQUALS);
        await Assert.That(engine.State == CalculatorEngine.EngineState.Error).IsTrue();
    }

    [Test]
    public async Task Angle_modes_and_conversions()
    {
        var engine = new CalculatorEngine();
        engine.ClearAll();

        // sin(30 deg) = 0.5
        engine.ExecuteKeyInteractive(KeyCodes.KEY_DEG);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_3);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_0);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_2ND);
        engine.ExecuteKeyInteractive(KeyCodes.KEY_X_SQUARED); // SIN in 2nd
        await Assert.That(engine.X).IsEqualTo(0.5).Within(1e-6);
    }

    [Test]
    public async Task Arithmetic_precedence_with_raw_key_codes()
    {
        var engine = new CalculatorEngine();
        foreach (int key in new[] { 83, 85, 84, 65, 72, 95 })
        {
            engine.ExecuteKeyInteractive(key);
        }
        await Assert.That(engine.X).IsEqualTo(14.0).Within(1e-9);
    }

    [Test]
    public async Task Degree_conversion()
    {
        await Assert.That(Arith.ToRadians(180, Arith.ANG_DEG)).IsEqualTo(Math.PI).Within(1e-12);
    }

    [Test]
    public async Task Dms_roundtrip()
    {
        await Assert.That(Arith.FromDms(Arith.ToDms(-12.5))).IsEqualTo(-12.5).Within(1e-10);
    }

    [Test]
    public async Task Division_by_zero_signals_error()
    {
        var engine = new CalculatorEngine();
        foreach (int key in new[] { 82, 55, 92, 95 })
        {
            engine.ExecuteKeyInteractive(key);
        }
        await Assert.That(engine.State == CalculatorEngine.EngineState.Error).IsTrue();
    }
}
