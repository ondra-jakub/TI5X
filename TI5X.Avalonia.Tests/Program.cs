using TI5X.Avalonia.Core;

int failures = 0;
void Check(string name, Action test)
{
    try
    {
        test();
        Console.WriteLine($"PASS: {name}");
    }
    catch (Exception ex)
    {
        failures++;
        Console.Error.WriteLine($"FAIL: {name}: {ex.Message}");
    }
}

void Assert(bool condition)
{
    if (!condition) throw new InvalidOperationException("Assertion failed");
}

Check("Trailing decimal occupies no extra digit", () =>
{
    var segments = DisplayCodes.StringToSegments("0.");
    Assert(segments[^1] == (DisplayCodes.SegmentCode('0') | DisplayCodes.SegDp));
    Assert(segments.Take(11).All(s => s == 0));
});
Check("Decimal attaches to preceding digit", () =>
{
    var segments = DisplayCodes.StringToSegments("12.34");
    Assert(segments[^3] == (DisplayCodes.SegmentCode('2') | DisplayCodes.SegDp));
    Assert(segments[^2] == DisplayCodes.SegmentCode('3'));
    Assert(segments[^1] == DisplayCodes.SegmentCode('4'));
});
Check("Full width decimal display", () =>
{
    var segments = DisplayCodes.StringToSegments("123456789012.");
    Assert(segments[0] == DisplayCodes.SegmentCode('1'));
    Assert(segments[^1] == (DisplayCodes.SegmentCode('2') | DisplayCodes.SegDp));
});
Check("Arithmetic precedence", () =>
{
    var engine = new CalculatorEngine();
    foreach (int key in new[] { 83, 85, 84, 65, 72, 95 }) engine.ExecuteKeyInteractive(key);
    Assert(engine.X == 14);
});
Check("Degree conversion", () => Assert(Math.Abs(Arith.ToRadians(180, Arith.ANG_DEG) - Math.PI) < 1e-12));
Check("DMS roundtrip", () => Assert(Math.Abs(Arith.FromDms(Arith.ToDms(-12.5)) + 12.5) < 1e-10));
Check("Division by zero signals error", () =>
{
    var engine = new CalculatorEngine();
    foreach (int key in new[] { 82, 55, 92, 95 }) engine.ExecuteKeyInteractive(key);
    Assert(engine.State == CalculatorEngine.EngineState.Error);
});
return failures == 0 ? 0 : 1;