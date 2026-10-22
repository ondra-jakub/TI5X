using TI5X.Avalonia.Core;

namespace TI5X.Maui.Controls;

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

        string?[] primeLabels = { card.APrime, card.BPrime, card.CPrime, card.DPrime, card.EPrime };
        string?[] baseLabels = { card.A, card.B, card.C, card.D, card.E };

        for (int i = 0; i < 5; ++i)
        {
            float colX = dirtyRect.X + i * colW;
            if (i > 0)
            {
                canvas.DrawLine(colX, dirtyRect.Y + headerH, colX, dirtyRect.Bottom);
            }

            if (!string.IsNullOrEmpty(primeLabels[i]))
            {
                canvas.Font = Microsoft.Maui.Graphics.Font.Default;
                canvas.FontSize = 9;
                canvas.FontColor = PrimeColor;
                canvas.DrawString(primeLabels[i]!, colX, dirtyRect.Y + headerH + 2, colW, (dirtyRect.Height - headerH) / 2f - 2,
                    Microsoft.Maui.Graphics.HorizontalAlignment.Center, Microsoft.Maui.Graphics.VerticalAlignment.Top);
            }

            if (!string.IsNullOrEmpty(baseLabels[i]))
            {
                canvas.Font = Microsoft.Maui.Graphics.Font.DefaultBold;
                canvas.FontSize = 10;
                canvas.FontColor = TextColor;
                canvas.DrawString(baseLabels[i]!, colX, dirtyRect.Y + headerH + (dirtyRect.Height - headerH) / 2f, colW,
                    (dirtyRect.Height - headerH) / 2f,
                    Microsoft.Maui.Graphics.HorizontalAlignment.Center, Microsoft.Maui.Graphics.VerticalAlignment.Center);
            }
        }
    }

    private static void DrawCentered(ICanvas canvas, string text, RectF bounds, Color color, float fontSize, bool bold)
    {
        canvas.Font = bold ? Microsoft.Maui.Graphics.Font.DefaultBold : Microsoft.Maui.Graphics.Font.Default;
        canvas.FontSize = fontSize;
        canvas.FontColor = color;
        canvas.DrawString(text, bounds.X, bounds.Y, bounds.Width, bounds.Height,
            Microsoft.Maui.Graphics.HorizontalAlignment.Center, Microsoft.Maui.Graphics.VerticalAlignment.Center);
    }
}
