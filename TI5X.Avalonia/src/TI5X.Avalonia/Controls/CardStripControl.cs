using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using TI5X.Avalonia.Core;

namespace TI5X.Avalonia.Controls;

public class CardStripControl : Control
{
    public static readonly StyledProperty<CardDef?> CardProperty =
        AvaloniaProperty.Register<CardStripControl, CardDef?>(nameof(Card), null);

    public CardDef? Card
    {
        get => GetValue(CardProperty);
        set => SetValue(CardProperty, value);
    }

    static CardStripControl()
    {
        AffectsRender<CardStripControl>(CardProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        var card = Card;

        // Draw card background
        var bgBrush = new SolidColorBrush(Color.FromRgb(235, 230, 215)); // Vintage cardstock
        var borderPen = new Pen(new SolidColorBrush(Color.FromRgb(120, 115, 100)), 1);
        var dividerPen = new Pen(new SolidColorBrush(Color.FromRgb(180, 175, 160)), 1);
        var textBrush = new SolidColorBrush(Color.FromRgb(20, 20, 20));
        var primeBrush = new SolidColorBrush(Color.FromRgb(140, 40, 20));

        context.FillRectangle(bgBrush, bounds);
        context.DrawRectangle(borderPen, bounds);

        if (card == null)
        {
            // Empty user program default label strip
            var tf = new FormattedText("USER PROGRAM / CARD SLOT", System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface("Courier New", FontStyle.Normal, FontWeight.SemiBold), 11, new SolidColorBrush(Color.FromRgb(160, 155, 140)));
            context.DrawText(tf, new Point((bounds.Width - tf.Width) / 2, (bounds.Height - tf.Height) / 2));
            return;
        }

        double headerH = bounds.Height * 0.35;
        double colW = bounds.Width / 5.0;

        // Title line
        if (!string.IsNullOrEmpty(card.TitleLeft))
        {
            var leftTf = new FormattedText(card.TitleLeft, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Typeface.Default.FontFamily, FontStyle.Normal, FontWeight.Bold), 10, textBrush);
            context.DrawText(leftTf, new Point(6, 2));
        }

        if (!string.IsNullOrEmpty(card.TitleRight))
        {
            var rightTf = new FormattedText(card.TitleRight, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Typeface.Default.FontFamily, FontStyle.Normal, FontWeight.Bold), 10, textBrush);
            context.DrawText(rightTf, new Point(bounds.Width - rightTf.Width - 6, 2));
        }

        // Horizontal divider under title
        context.DrawLine(dividerPen, new Point(0, headerH), new Point(bounds.Width, headerH));

        // 5 Columns for A, B, C, D, E
        string?[] primeLabels = { card.APrime, card.BPrime, card.CPrime, card.DPrime, card.EPrime };
        string?[] baseLabels = { card.A, card.B, card.C, card.D, card.E };

        for (int i = 0; i < 5; ++i)
        {
            double colX = i * colW;
            if (i > 0)
            {
                context.DrawLine(dividerPen, new Point(colX, headerH), new Point(colX, bounds.Height));
            }

            // Upper prime label
            string primeText = primeLabels[i] ?? "";
            if (!string.IsNullOrEmpty(primeText))
            {
                var tfPrime = new FormattedText(primeText, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Typeface.Default.FontFamily, FontStyle.Normal, FontWeight.Normal), 9, primeBrush);
                context.DrawText(tfPrime, new Point(colX + (colW - tfPrime.Width) / 2, headerH + 2));
            }

            // Lower base label
            string baseText = baseLabels[i] ?? "";
            if (!string.IsNullOrEmpty(baseText))
            {
                var tfBase = new FormattedText(baseText, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, new Typeface(Typeface.Default.FontFamily, FontStyle.Normal, FontWeight.Bold), 10, textBrush);
                context.DrawText(tfBase, new Point(colX + (colW - tfBase.Width) / 2, headerH + (bounds.Height - headerH) / 2.0));
            }
        }
    }
}
