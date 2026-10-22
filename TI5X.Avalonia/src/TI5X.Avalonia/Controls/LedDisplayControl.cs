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

        for (int i = 0; i < DisplayCodes.NrDigits; ++i)
        {
            double xOrigin = i * digitWidth;
            int code = showDigits ? segments[i] : 0;
            RenderDigit(context, code, xOrigin, yOrigin, digitWidth, digitHeight, activeColor);
        }
    }

    private void RenderDigit(DrawingContext context, int segMask, double x, double y, double w, double h, Color activeColor)
    {
        double marginX = w * 0.15;
        double actualW = w * 0.7;
        double actualH = h;
        double stroke = actualH * 0.10;
        double slant = actualH * 0.08;

        // Draw 7 segments + decimal point
        // Segments: A (top), B (top-right), C (bot-right), D (bot), E (bot-left), F (top-left), G (mid), DP (dot)
        DrawHorizontalSeg(context, x + marginX + slant, y, actualW, stroke, (segMask & DisplayCodes.SegA) != 0, activeColor);
        DrawHorizontalSeg(context, x + marginX + slant * 0.5, y + actualH / 2.0 - stroke / 2.0, actualW, stroke, (segMask & DisplayCodes.SegG) != 0, activeColor);
        DrawHorizontalSeg(context, x + marginX, y + actualH - stroke, actualW, stroke, (segMask & DisplayCodes.SegD) != 0, activeColor);

        DrawVerticalSeg(context, x + marginX + actualW - stroke + slant * 0.75, y + stroke * 0.5, stroke, actualH / 2.0 - stroke * 0.5, (segMask & DisplayCodes.SegB) != 0, activeColor);
        DrawVerticalSeg(context, x + marginX + actualW - stroke + slant * 0.25, y + actualH / 2.0, stroke, actualH / 2.0 - stroke * 0.5, (segMask & DisplayCodes.SegC) != 0, activeColor);

        DrawVerticalSeg(context, x + marginX + slant * 0.75, y + stroke * 0.5, stroke, actualH / 2.0 - stroke * 0.5, (segMask & DisplayCodes.SegF) != 0, activeColor);
        DrawVerticalSeg(context, x + marginX + slant * 0.25, y + actualH / 2.0, stroke, actualH / 2.0 - stroke * 0.5, (segMask & DisplayCodes.SegE) != 0, activeColor);

        // Decimal point
        DrawDot(context, x + marginX + actualW + stroke * 0.4, y + actualH - stroke, stroke * 1.2, (segMask & DisplayCodes.SegDp) != 0, activeColor);
    }

    private void DrawHorizontalSeg(DrawingContext context, double x, double y, double w, double h, bool isOn, Color activeColor)
    {
        var brush = new SolidColorBrush(isOn ? activeColor : LedOffColor);
        var rect = new Rect(x + h * 0.3, y, Math.Max(0, w - h * 0.6), h);
        context.FillRectangle(brush, rect);
    }

    private void DrawVerticalSeg(DrawingContext context, double x, double y, double w, double h, bool isOn, Color activeColor)
    {
        var brush = new SolidColorBrush(isOn ? activeColor : LedOffColor);
        var rect = new Rect(x, y + w * 0.3, w, Math.Max(0, h - w * 0.6));
        context.FillRectangle(brush, rect);
    }

    private void DrawDot(DrawingContext context, double x, double y, double size, bool isOn, Color activeColor)
    {
        var brush = new SolidColorBrush(isOn ? activeColor : LedOffColor);
        var rect = new Rect(x, y, size, size);
        context.FillRectangle(brush, rect);
    }
}
