using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using TI5X.Avalonia.Core;

namespace TI5X.Avalonia.Controls;

/// <summary>
/// Renders a library program card (magnetic card face) in the slip window: title row,
/// then the A′–E′ and A–E label cells decoded from the light card markup
/// (<see cref="CardLabelMarkup"/>) — real TI library cards list which keys start the
/// program, so those button labels must be visible.
/// </summary>
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

        // Upper prime row: five cells (A′..E′), or one full-width row
        DrawRow(context, dividerPen, headerH, headerH + 2, headerH + (bounds.Height - headerH) / 2.0,
            colW, primeBrush, bold: false, size: 9,
            cells: new[] { card.APrime, card.BPrime, card.CPrime, card.DPrime, card.EPrime },
            fullLeft: null, fullCenter: card.PrimeCenter, fullRight: null,
            verticalCenter: false);

        // Lower base row: five cells (A..E), or full-width left/center/right labels
        DrawRow(context, dividerPen, headerH, headerH + (bounds.Height - headerH) / 2.0, bounds.Height,
            colW, textBrush, bold: true, size: 10,
            cells: new[] { card.A, card.B, card.C, card.D, card.E },
            fullLeft: card.BaseLeft, fullCenter: card.BaseCenter, fullRight: card.BaseRight,
            verticalCenter: true);
    }

    private void DrawRow(
        DrawingContext context,
        IPen dividerPen,
        double top,
        double cellTop,
        double bottom,
        double colW,
        IBrush brush,
        bool bold,
        double size,
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
                double colX = i * colW;
                if (i > 0)
                {
                    context.DrawLine(dividerPen, new Point(colX, top), new Point(colX, bottom));
                }

                DrawRuns(context, cells[i], colX + colW / 2.0, cellTop, bottom, size, brush, bold, verticalCenter);
            }
        }
        else
        {
            double left = 6, width = Bounds.Width - 12;
            if (!string.IsNullOrEmpty(fullCenter))
            {
                DrawRuns(context, fullCenter, Bounds.Width / 2.0, cellTop, bottom, size, brush, bold, verticalCenter);
            }
            else
            {
                if (!string.IsNullOrEmpty(fullLeft))
                {
                    DrawRuns(context, fullLeft, left, cellTop, bottom, size, brush, bold, verticalCenter, centerHorizontally: false);
                }
                if (!string.IsNullOrEmpty(fullRight))
                {
                    DrawRuns(context, fullRight, left + width, cellTop, bottom, size, brush, bold, verticalCenter, centerHorizontally: false, alignRight: true);
                }
            }
        }
    }

    private void DrawRuns(
        DrawingContext context,
        string? markup,
        double centerX,
        double top,
        double bottom,
        double size,
        IBrush brush,
        bool bold,
        bool verticalCenter,
        bool centerHorizontally = true,
        bool alignRight = false)
    {
        if (string.IsNullOrEmpty(markup)) return;
        var runs = CardLabelMarkup.Decode(markup);

        var typeface = new Typeface(Typeface.Default.FontFamily, FontStyle.Normal, bold ? FontWeight.Bold : FontWeight.Normal);
        var penBrush = new Pen(brush, 0.8);
        double boxPad = 1.5;

        // Pass 1: measure to find total width and per-run metrics
        var metrics = new (double Width, double Height, CardLabelRun Run)[runs.Count];
        double total = 0;
        for (int i = 0; i < runs.Count; ++i)
        {
            double runSize = RunFontSize(size, runs[i].Style);
            var tf = new FormattedText(runs[i].Text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, runSize, brush);
            metrics[i] = (tf.Width + (runs[i].Style == CardLabelRunStyle.Button ? 2 * boxPad : 0), tf.Height, runs[i]);
            total += tf.Width + (runs[i].Style == CardLabelRunStyle.Button ? 2 * boxPad : 0);
        }

        double x = centerHorizontally
            ? centerX - total / 2.0
            : alignRight ? centerX - total : centerX;
        double lineH = size;
        double baseY = verticalCenter ? top + Math.Max(0, (bottom - top) - lineH) / 2.0 : top;

        // Pass 2: draw
        for (int i = 0; i < runs.Count; ++i)
        {
            var (width, height, run) = metrics[i];
            double runSize = RunFontSize(size, run.Style);
            var tf = new FormattedText(run.Text, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, runSize, brush);
            double drawX = run.Style == CardLabelRunStyle.Button ? x + boxPad : x;
            double drawY = run.Style switch
            {
                CardLabelRunStyle.Subscript => baseY + height / 2.0 + 0.28 * lineH,
                CardLabelRunStyle.Superscript => baseY + height / 2.0 - 0.55 * lineH,
                _ => baseY + height / 2.0 - 0.45 * lineH,
            };

            if (run.Style == CardLabelRunStyle.Button)
            {
                context.DrawRectangle(null, penBrush, new Rect(x, baseY + height / 2.0 - 0.42 * lineH, width, lineH * 0.78), 2, 2);
                context.DrawText(tf, new Point(drawX, baseY + height / 2.0 - 0.42 * lineH));
            }
            else
            {
                context.DrawText(tf, new Point(drawX, drawY));
                if (run.Style == CardLabelRunStyle.Overline)
                {
                    context.DrawLine(penBrush, new Point(drawX, drawY - 1), new Point(drawX + tf.Width, drawY - 1));
                }
            }

            x += width;
        }
    }

    private static double RunFontSize(double size, CardLabelRunStyle style) => style switch
    {
        CardLabelRunStyle.Subscript => size * 0.8,
        CardLabelRunStyle.Superscript => size * 0.8,
        CardLabelRunStyle.Button => size * 0.85,
        _ => size,
    };
}