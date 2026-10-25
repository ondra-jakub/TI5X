using TI5X.Avalonia.Core;

namespace TI5X.Maui.Controls;

/// <summary>
/// Renders a library program card (magnetic card face) in the slip window: title row,
/// then the A′–E′ and A–E label cells decoded from the light card markup
/// (<see cref="CardLabelMarkup"/>) — real TI library cards list which keys start the
/// program, so those button labels must be visible.
/// </summary>
public class CardStripView : GraphicsView, IDrawable
{
    public static readonly BindableProperty CardProperty =
        BindableProperty.Create(nameof(Card), typeof(CardDef), typeof(CardStripView), null,
            propertyChanged: (bindable, _, _) => ((CardStripView)bindable).Invalidate());

    private static readonly Color CardBackground = Color.FromRgb(235, 230, 215);
    private static readonly Color BorderColor = Color.FromRgb(120, 115, 100);
    private static readonly Color DividerColor = Color.FromRgb(180, 175, 160);
    private static readonly Color TextColor = Color.FromRgb(20, 20, 20);
    private static readonly Color PrimeColor = Color.FromRgb(140, 40, 20);
    private static readonly Color EmptyTextColor = Color.FromRgb(160, 155, 140);

    public CardDef? Card
    {
        get => (CardDef?)GetValue(CardProperty);
        set => SetValue(CardProperty, value);
    }

    public CardStripView()
    {
        Drawable = this;
    }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = CardBackground;
        canvas.FillRectangle(dirtyRect);
        canvas.StrokeColor = BorderColor;
        canvas.StrokeSize = 1;
        canvas.DrawRectangle(dirtyRect.X, dirtyRect.Y, dirtyRect.Width, dirtyRect.Height);

        var card = Card;
        if (card == null)
        {
            DrawCentered(canvas, "USER PROGRAM / CARD SLOT", dirtyRect, EmptyTextColor, 11, false);
            return;
        }

        float headerH = dirtyRect.Height * 0.35f;
        float colW = dirtyRect.Width / 5f;

        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = 10;
        canvas.FontColor = TextColor;
        if (!string.IsNullOrEmpty(card.TitleLeft))
        {
            canvas.DrawString(card.TitleLeft, dirtyRect.X + 6, dirtyRect.Y + 2, dirtyRect.Width - 12, headerH - 2,
                Microsoft.Maui.Graphics.HorizontalAlignment.Left, Microsoft.Maui.Graphics.VerticalAlignment.Top);
        }
        if (!string.IsNullOrEmpty(card.TitleRight))
        {
            canvas.DrawString(card.TitleRight, dirtyRect.X + 6, dirtyRect.Y + 2, dirtyRect.Width - 12, headerH - 2,
                Microsoft.Maui.Graphics.HorizontalAlignment.Right, Microsoft.Maui.Graphics.VerticalAlignment.Top);
        }

        canvas.StrokeColor = DividerColor;
        canvas.DrawLine(dirtyRect.X, dirtyRect.Y + headerH, dirtyRect.Right, dirtyRect.Y + headerH);

        canvas.FontColor = PrimeColor;
        canvas.Font = Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = 9;
        DrawRow(canvas, dirtyRect.Width, headerH, headerH + 2, headerH + (dirtyRect.Height - headerH) / 2f,
            colW, PrimeColor, bold: false, size: 9,
            cells: new[] { card.APrime, card.BPrime, card.CPrime, card.DPrime, card.EPrime },
            fullLeft: null, fullCenter: card.PrimeCenter, fullRight: null,
            verticalCenter: false);

        canvas.FontColor = TextColor;
        canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
        canvas.FontSize = 10;
        DrawRow(canvas, dirtyRect.Width, headerH, headerH + (dirtyRect.Height - headerH) / 2f, dirtyRect.Height,
            colW, TextColor, bold: true, size: 10,
            cells: new[] { card.A, card.B, card.C, card.D, card.E },
            fullLeft: card.BaseLeft, fullCenter: card.BaseCenter, fullRight: card.BaseRight,
            verticalCenter: true);
    }

    private void DrawRow(
        ICanvas canvas,
        float rowWidth,
        float top,
        float cellTop,
        float bottom,
        float colW,
        Color color,
        bool bold,
        float size,
        string?[] cells,
        string? fullLeft,
        string? fullCenter,
        string? fullRight,
        bool verticalCenter)
    {
        bool hasCells = cells.Any(c => !string.IsNullOrEmpty(c));
        if (hasCells)
        {
            for (int i = 0; i < 5; ++i)
            {
                float colX = i * colW;
                if (i > 0)
                {
                    canvas.StrokeColor = DividerColor;
                    canvas.DrawLine(colX, top, colX, bottom);
                }

                DrawRuns(canvas, cells[i], colX + colW / 2f, rowWidth, top, cellTop, bottom, size, color, bold, verticalCenter);
            }
        }
        else
        {
            float left = 6, width = rowWidth - 12;
            if (!string.IsNullOrEmpty(fullCenter))
            {
                DrawRuns(canvas, fullCenter, left + width / 2f, rowWidth, top, cellTop, bottom, size, color, bold, verticalCenter);
            }
            else
            {
                if (!string.IsNullOrEmpty(fullLeft))
                {
                    DrawRuns(canvas, fullLeft, left, rowWidth, top, cellTop, bottom, size, color, bold, verticalCenter, centerHorizontally: false);
                }
                if (!string.IsNullOrEmpty(fullRight))
                {
                    DrawRuns(canvas, fullRight, left + width, rowWidth, top, cellTop, bottom, size, color, bold, verticalCenter,
                        centerHorizontally: false, alignRight: true);
                }
            }
        }
    }

    private void DrawRuns(
        ICanvas canvas,
        string? markup,
        float centerX,
        float rowWidth,
        float rowTop,
        float cellTop,
        float bottom,
        float size,
        Color color,
        bool bold,
        bool verticalCenter,
        bool centerHorizontally = true,
        bool alignRight = false)
    {
        if (string.IsNullOrEmpty(markup)) return;
        var runs = CardLabelMarkup.Decode(markup);

        var font = bold ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
        canvas.Font = font;
        canvas.FontColor = color;
        float boxPad = 1.5f;

        var metrics = new (float Width, float Height, CardLabelRun Run)[runs.Count];
        float total = 0;
        for (int i = 0; i < runs.Count; ++i)
        {
            float runSize = RunFontSize(size, runs[i].Style);
            var measured = canvas.GetStringSize(runs[i].Text, font, runSize);
            float width = measured.Width + (runs[i].Style == CardLabelRunStyle.Button ? 2 * boxPad : 0);
            float height = measured.Height;
            metrics[i] = (width, height, runs[i]);
            total += width;
        }

        float x = centerHorizontally
            ? centerX - total / 2f
            : alignRight ? centerX - total : centerX;
        float lineH = size;
        float baseY = verticalCenter ? cellTop + Math.Max(0, (bottom - cellTop) - lineH) / 2f : cellTop;

        for (int i = 0; i < runs.Count; ++i)
        {
            var (width, height, run) = metrics[i];
            float runSize = RunFontSize(size, run.Style);
            float drawX = run.Style == CardLabelRunStyle.Button ? x + boxPad : x;
            float drawY = run.Style switch
            {
                CardLabelRunStyle.Subscript => baseY + height / 2f + 0.28f * lineH,
                CardLabelRunStyle.Superscript => baseY + height / 2f - 0.55f * lineH,
                _ => baseY + height / 2f - 0.45f * lineH,
            };

            canvas.FontSize = runSize;
            if (run.Style == CardLabelRunStyle.Button)
            {
                canvas.StrokeColor = color;
                canvas.StrokeSize = 1;
                canvas.DrawRectangle(x, baseY + height / 2f - 0.2f * lineH, width, lineH * 0.8f);
                canvas.DrawString(run.Text, drawX, baseY + height / 2f - 0.1f * lineH, width, height,
                    Microsoft.Maui.Graphics.HorizontalAlignment.Left, Microsoft.Maui.Graphics.VerticalAlignment.Top);
            }
            else
            {
                canvas.DrawString(run.Text, drawX, drawY, width, height + 1,
                    Microsoft.Maui.Graphics.HorizontalAlignment.Left, Microsoft.Maui.Graphics.VerticalAlignment.Top);
                if (run.Style == CardLabelRunStyle.Overline)
                {
                    canvas.StrokeColor = color;
                    canvas.StrokeSize = 1;
                    canvas.DrawLine(drawX, drawY - 1, drawX + width, drawY - 1);
                }
            }

            x += width;
        }
    }

    private static float RunFontSize(float size, CardLabelRunStyle style) => style switch
    {
        CardLabelRunStyle.Subscript => size * 0.8f,
        CardLabelRunStyle.Superscript => size * 0.8f,
        CardLabelRunStyle.Button => size * 0.85f,
        _ => size,
    };

    private static void DrawCentered(ICanvas canvas, string text, RectF bounds, Color color, float fontSize, bool bold)
    {
        canvas.Font = bold ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = fontSize;
        canvas.FontColor = color;
        canvas.DrawString(text, bounds.X, bounds.Y, bounds.Width, bounds.Height,
            Microsoft.Maui.Graphics.HorizontalAlignment.Center, Microsoft.Maui.Graphics.VerticalAlignment.Center);
    }
}