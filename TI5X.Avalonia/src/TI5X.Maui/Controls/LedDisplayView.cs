using System.Numerics;
using Microsoft.Maui.Dispatching;
using TI5X.Avalonia.Core;

namespace TI5X.Maui.Controls;

public class LedDisplayView : GraphicsView, IDrawable
{
    public static readonly BindableProperty TextProperty =
        BindableProperty.Create(nameof(Text), typeof(string), typeof(LedDisplayView), "0.",
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty IsErrorProperty =
        BindableProperty.Create(nameof(IsError), typeof(bool), typeof(LedDisplayView), false,
            propertyChanged: OnVisualChanged);

    public static readonly BindableProperty IsRunningProperty =
        BindableProperty.Create(nameof(IsRunning), typeof(bool), typeof(LedDisplayView), false,
            propertyChanged: OnVisualChanged);

    private static readonly Color LedBackgroundColor = Color.FromRgb(20, 0, 0);
    private static readonly Color LedOnColor = Color.FromRgb(255, 45, 45);
    private static readonly Color LedDimColor = Color.FromRgb(150, 20, 20);
    private static readonly Color LedOffColor = Color.FromRgb(45, 5, 5);

    private IDispatcherTimer? _flashTimer;
    private bool _flashState = true;

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public bool IsError
    {
        get => (bool)GetValue(IsErrorProperty);
        set => SetValue(IsErrorProperty, value);
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public LedDisplayView()
    {
        Drawable = this;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private static void OnVisualChanged(BindableObject bindable, object oldValue, object newValue)
    {
        ((LedDisplayView)bindable).Invalidate();
    }

    private void OnLoaded(object? sender, EventArgs e)
    {
        if (_flashTimer != null) return;

        _flashTimer = Dispatcher.CreateTimer();
        _flashTimer.Interval = TimeSpan.FromMilliseconds(300);
        _flashTimer.Tick += OnFlashTick;
        _flashTimer.Start();
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        _flashTimer?.Stop();
        _flashTimer = null;
    }

    private void OnFlashTick(object? sender, EventArgs e)
    {
        if (IsError || IsRunning)
        {
            _flashState = !_flashState;
            Invalidate();
        }
        else if (!_flashState)
        {
            _flashState = true;
            Invalidate();
        }
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = LedBackgroundColor;
        canvas.FillRectangle(dirtyRect);

        int[] segments = DisplayCodes.StringToSegments(Text);

        float totalWidth = dirtyRect.Width;
        float totalHeight = dirtyRect.Height;
        float digitWidth = totalWidth / DisplayCodes.NrDigits;
        float digitHeight = totalHeight * 0.75f;
        float yOrigin = (totalHeight - digitHeight) / 2f;

        bool showDigits = !IsError || _flashState;
        Color activeColor = IsRunning && !_flashState ? LedDimColor : LedOnColor;

        float slant = digitHeight * 0.08f;
        float shearRate = slant / digitHeight;
        var shear = new Matrix3x2(1f, 0f, -shearRate, 1f, slant + shearRate * yOrigin, 0f);

        canvas.SaveState();
        canvas.ConcatenateTransform(shear);

        for (int i = 0; i < DisplayCodes.NrDigits; ++i)
        {
            float xOrigin = i * digitWidth;
            int code = showDigits ? segments[i] : 0;
            RenderDigit(canvas, dirtyRect.X + xOrigin, dirtyRect.Y + yOrigin, digitWidth, digitHeight, code, activeColor);
        }

        canvas.RestoreState();
    }

    private static void RenderDigit(ICanvas canvas, float x, float y, float w, float h, int segMask, Color activeColor)
    {
        float marginX = w * 0.15f;
        float actualW = w * 0.7f;
        float actualH = h;
        float stroke = actualH * 0.10f;

        DrawHorizontalSeg(canvas, x + marginX, y, actualW, stroke, (segMask & DisplayCodes.SegA) != 0, activeColor);
        DrawHorizontalSeg(canvas, x + marginX, y + actualH / 2f - stroke / 2f, actualW, stroke, (segMask & DisplayCodes.SegG) != 0, activeColor);
        DrawHorizontalSeg(canvas, x + marginX, y + actualH - stroke, actualW, stroke, (segMask & DisplayCodes.SegD) != 0, activeColor);

        DrawVerticalSeg(canvas, x + marginX + actualW - stroke, y + stroke * 0.5f, stroke, actualH / 2f - stroke * 0.5f, (segMask & DisplayCodes.SegB) != 0, activeColor);
        DrawVerticalSeg(canvas, x + marginX + actualW - stroke, y + actualH / 2f, stroke, actualH / 2f - stroke * 0.5f, (segMask & DisplayCodes.SegC) != 0, activeColor);

        DrawVerticalSeg(canvas, x + marginX, y + stroke * 0.5f, stroke, actualH / 2f - stroke * 0.5f, (segMask & DisplayCodes.SegF) != 0, activeColor);
        DrawVerticalSeg(canvas, x + marginX, y + actualH / 2f, stroke, actualH / 2f - stroke * 0.5f, (segMask & DisplayCodes.SegE) != 0, activeColor);

        DrawDot(canvas, x + marginX + actualW + stroke * 0.4f, y + actualH - stroke, stroke * 1.2f, (segMask & DisplayCodes.SegDp) != 0, activeColor);
    }

    private static void DrawHorizontalSeg(ICanvas canvas, float x, float y, float w, float h, bool isOn, Color activeColor)
    {
        canvas.FillColor = isOn ? activeColor : LedOffColor;
        canvas.FillRectangle(x + h * 0.3f, y, Math.Max(0, w - h * 0.6f), h);
    }

    private static void DrawVerticalSeg(ICanvas canvas, float x, float y, float w, float h, bool isOn, Color activeColor)
    {
        canvas.FillColor = isOn ? activeColor : LedOffColor;
        canvas.FillRectangle(x, y + w * 0.3f, w, Math.Max(0, h - w * 0.6f));
    }

    private static void DrawDot(ICanvas canvas, float x, float y, float size, bool isOn, Color activeColor)
    {
        canvas.FillColor = isOn ? activeColor : LedOffColor;
        canvas.FillRectangle(x, y, size, size);
    }
}
