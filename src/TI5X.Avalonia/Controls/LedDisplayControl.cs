using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using TI5X.Avalonia.Core;

namespace TI5X.Avalonia.Controls;

public class LedDisplayControl : Control
{
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<LedDisplayControl, string>(nameof(Text), "0.");

    public static readonly StyledProperty<bool> IsErrorProperty =
        AvaloniaProperty.Register<LedDisplayControl, bool>(nameof(IsError), false);

    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<LedDisplayControl, bool>(nameof(IsRunning), false);

    private readonly DispatcherTimer _flashTimer;
    private bool _flashState = true;

    private static readonly Color BackgroundColor = Color.FromRgb(20, 0, 0);
    private static readonly Color LedOnColor = Color.FromRgb(255, 45, 45);
    private static readonly Color LedDimColor = Color.FromRgb(150, 20, 20);
    private static readonly Color LedOffColor = Color.FromRgb(45, 5, 5);

    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsError
    {
        get => GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    static LedDisplayControl()
    {
        AffectsRender<LedDisplayControl>(TextProperty, IsErrorProperty, IsRunningProperty);
    }

    public LedDisplayControl()
    {
        ClipToBounds = true;
        _flashTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _flashTimer.Tick += (s, e) =>
        {
            if (IsError || IsRunning)
            {
                _flashState = !_flashState;
                InvalidateVisual();
            }
            else if (!_flashState)
            {
                _flashState = true;
                InvalidateVisual();
            }
        };
        _flashTimer.Start();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        context.FillRectangle(new SolidColorBrush(BackgroundColor), bounds);

        int[] segments = DisplayCodes.StringToSegments(Text);

        double totalWidth = bounds.Width;
        double totalHeight = bounds.Height;

        double digitWidth = totalWidth / DisplayCodes.NrDigits;
        double digitHeight = totalHeight * 0.75;
        double yOrigin = (totalHeight - digitHeight) / 2.0;

        bool showDigits = !IsError || _flashState;
        Color activeColor = IsRunning && !_flashState ? LedDimColor : LedOnColor;

        // Uniform italic shear applied to the whole glyph row: every segment is drawn as a
        // plain rectangle in unsheared coordinates and the transform slants all of them at
        // once, so the top half no longer appears offset as a block from the bottom half.
        double slant = digitHeight * 0.08;
        double shearRate = slant / digitHeight;
        var shear = new Matrix(1.0, 0.0, -shearRate, 1.0, slant + shearRate * yOrigin, 0.0);

        using (context.PushTransform(shear))
        {
            for (int i = 0; i < DisplayCodes.NrDigits; ++i)
            {
                double xOrigin = i * digitWidth;
                int code = showDigits ? segments[i] : 0;
                RenderDigit(context, code, xOrigin, yOrigin, digitWidth, digitHeight, activeColor);
            }
        }
    }

    private void RenderDigit(DrawingContext context, int segMask, double x, double y, double w, double h, Color activeColor)
    {
        const double StrokeRatio = 0.075; // segment thickness as a fraction of digit height
        const double GapRatio = 0.45;     // junction clearance as a fraction of the stroke

        double marginX = w * 0.15;
        double actualW = w * 0.7;
        double actualH = h;
        double s = actualH * StrokeRatio;
        double g = s * GapRatio;

        double xLeft = x + marginX;
        double xRight = x + marginX + actualW;
        double mid = y + actualH / 2.0;

        // Horizontal segments are shortened at both ends so the corners stay open;
        // vertical segments stop `gap` short of every horizontal band, like the
        // individual LED bars of the real display (no overlaps at the junctions).
        double hX = xLeft + g;
        double hW = Math.Max(0, actualW - 2 * g);

        FillSeg(context, hX, y, hW, s, (segMask & DisplayCodes.SegA) != 0, activeColor);
        FillSeg(context, hX, mid - s / 2.0, hW, s, (segMask & DisplayCodes.SegG) != 0, activeColor);
        FillSeg(context, hX, y + actualH - s, hW, s, (segMask & DisplayCodes.SegD) != 0, activeColor);

        double vTop = y + s + g;
        double vUpper = mid - s / 2.0 - g;
        double vLower = mid + s / 2.0 + g;
        double vBot = y + actualH - s - g;

        FillSeg(context, xRight - s, vTop, s, Math.Max(0, vUpper - vTop), (segMask & DisplayCodes.SegB) != 0, activeColor);
        FillSeg(context, xRight - s, vLower, s, Math.Max(0, vBot - vLower), (segMask & DisplayCodes.SegC) != 0, activeColor);
        FillSeg(context, xLeft, vTop, s, Math.Max(0, vUpper - vTop), (segMask & DisplayCodes.SegF) != 0, activeColor);
        FillSeg(context, xLeft, vLower, s, Math.Max(0, vBot - vLower), (segMask & DisplayCodes.SegE) != 0, activeColor);

        // Decimal point
        FillSeg(context, xRight + s * 0.35, y + actualH - s * 1.15, s * 1.15, s * 1.15, (segMask & DisplayCodes.SegDp) != 0, activeColor);
    }

    private static void FillSeg(DrawingContext context, double x, double y, double w, double h, bool isOn, Color activeColor)
    {
        if (w <= 0 || h <= 0) return;
        var brush = new SolidColorBrush(isOn ? activeColor : LedOffColor);
        context.FillRectangle(brush, new Rect(x, y, w, h));
    }
}
