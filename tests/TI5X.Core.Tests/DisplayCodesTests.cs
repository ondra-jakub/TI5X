using TI5X.Avalonia.Core;

namespace TI5X.Core.Tests;

public class DisplayCodesTests
{
    [Test]
    public async Task Trailing_decimal_occupies_no_extra_digit()
    {
        var segments = DisplayCodes.StringToSegments("0.");
        await Assert.That(segments[^1]).IsEqualTo(DisplayCodes.SegmentCode('0') | DisplayCodes.SegDp);
        await Assert.That(segments.Take(11).All(s => s == 0)).IsTrue();
    }

    [Test]
    public async Task Decimal_attaches_to_preceding_digit()
    {
        var segments = DisplayCodes.StringToSegments("12.34");
        await Assert.That(segments[^3]).IsEqualTo(DisplayCodes.SegmentCode('2') | DisplayCodes.SegDp);
        await Assert.That(segments[^2]).IsEqualTo(DisplayCodes.SegmentCode('3'));
        await Assert.That(segments[^1]).IsEqualTo(DisplayCodes.SegmentCode('4'));
    }

    [Test]
    public async Task Full_width_decimal_display()
    {
        var segments = DisplayCodes.StringToSegments("123456789012.");
        await Assert.That(segments[0]).IsEqualTo(DisplayCodes.SegmentCode('1'));
        await Assert.That(segments[^1]).IsEqualTo(DisplayCodes.SegmentCode('2') | DisplayCodes.SegDp);
    }
}
