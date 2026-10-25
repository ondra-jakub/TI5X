using System.Text;

namespace TI5X.Avalonia.Core;

/// <summary>Drawing style of one segment of a card label.</summary>
public enum CardLabelRunStyle
{
    /// <summary>Regular text.</summary>
    Normal,

    /// <summary>Lowered small text (&lt;sub&gt;).</summary>
    Subscript,

    /// <summary>Raised small text (&lt;sup&gt;).</summary>
    Superscript,

    /// <summary>Text with a bar drawn over it (&lt;bar&gt;/&lt;Bar&gt;, e.g. x̄).</summary>
    Overline,

    /// <summary>Text drawn inside a small key-button box (&lt;button&gt;, e.g. SBR, CLR).</summary>
    Button,
}

/// <summary>One styled text segment of a card label.</summary>
public sealed record CardLabelRun(string Text, CardLabelRunStyle Style);

/// <summary>
/// Decodes the light HTML-like markup used by library program card labels
/// (original TI-5X card templates) into styled text runs:
/// &lt;sub&gt;/&lt;sup&gt; subscripts/superscripts, &lt;bar&gt;/&lt;Bar&gt; overbars,
/// &lt;button&gt; key boxes and the inline symbol entities
/// &lt;angle/&gt; &lt;darrow/&gt; &lt;Delta/&gt; &lt;div/&gt; &lt;Gamma/&gt;
/// &lt;integral/&gt; &lt;lambda/&gt; &lt;mul/&gt; &lt;noverr/&gt; &lt;rarrow/&gt;
/// &lt;sigma/&gt; &lt;theta/&gt;. Text outside these tags is kept as-is, so labels
/// without markup round-trip unchanged.
/// </summary>
public static class CardLabelMarkup
{
    private static readonly Dictionary<string, string> Symbols = new(StringComparer.Ordinal)
    {
        ["angle"] = "∠",
        ["darrow"] = "↔",
        ["Delta"] = "Δ",
        ["div"] = "÷",
        ["epsilon"] = "ε",
        ["Gamma"] = "Γ",
        ["integral"] = "∫",
        ["lambda"] = "λ",
        ["mul"] = "×",
        ["noverr"] = "n/r",
        ["rarrow"] = "→",
        ["sigma"] = "Σ",
        ["theta"] = "θ",
    };

    /// <summary>Decodes markup into a sequence of styled runs (never null).</summary>
    public static IReadOnlyList<CardLabelRun> Decode(string? markup)
    {
        if (string.IsNullOrEmpty(markup))
        {
            return Array.Empty<CardLabelRun>();
        }

        var runs = new List<CardLabelRun>();
        DecodeInto(markup, 0, markup.Length, CardLabelRunStyle.Normal, runs);
        return MergeAdjacent(runs);
    }

    /// <summary>Converts markup to plain display text (Unicode approximations).</summary>
    public static string ToPlainText(string? markup)
    {
        var sb = new StringBuilder();
        foreach (var run in Decode(markup))
        {
            sb.Append(run.Text);
        }

        return sb.ToString();
    }

    private static void DecodeInto(string markup, int start, int end, CardLabelRunStyle containerStyle, List<CardLabelRun> runs)
    {
        int i = start;
        while (i < end)
        {
            int lt = markup.IndexOf('<', i);
            if (lt < 0 || lt >= end)
            {
                if (i < end) Append(runs, markup[i..end], containerStyle);
                break;
            }

            if (lt > i)
            {
                Append(runs, markup[i..lt], containerStyle);
            }

            int gt = markup.IndexOf('>', lt);
            if (gt < 0 || gt >= end)
            {
                // malformed: keep the rest as plain text
                Append(runs, markup[lt..end], containerStyle);
                break;
            }

            string rawTag = markup[(lt + 1)..gt];
            bool selfClosing = rawTag.TrimEnd().EndsWith('/');
            string tag = rawTag.Trim().TrimEnd('/').Trim();
            if (!TrySymbol(tag, selfClosing, out string? symbol))
            {
                // unknown markup or a closing tag without a matching open tag:
                // skip the token so it cannot be drawn literally
                i = gt + 1;
                continue;
            }

            if (symbol != null)
            {
                Append(runs, symbol, containerStyle);
                i = gt + 1;
                continue;
            }

            // container tag: find the matching close tag (no nesting of the same tag)
            string closeTag = $"</{tag}>";
            int inner = gt + 1;
            int close = markup.IndexOf(closeTag, inner, StringComparison.OrdinalIgnoreCase);
            if (close < 0 || close > end - closeTag.Length)
            {
                close = end; // unclosed tag: treat the rest as its content
            }

            var style = tag switch
            {
                "sub" => CardLabelRunStyle.Subscript,
                "sup" => CardLabelRunStyle.Superscript,
                "bar" => CardLabelRunStyle.Overline,
                "Bar" => CardLabelRunStyle.Overline,
                "button" => CardLabelRunStyle.Button,
                _ => containerStyle,
            };

            DecodeInto(markup, inner, close, style, runs);
            i = close < end ? close + closeTag.Length : end;
        }
    }

    private static bool TrySymbol(string tag, bool selfClosing, out string? symbol)
    {
        symbol = null;
        if (!selfClosing)
        {
            return tag is "sub" or "sup" or "bar" or "Bar" or "button";
        }

        return Symbols.TryGetValue(tag, out symbol);
    }

    private static void Append(List<CardLabelRun> runs, string text, CardLabelRunStyle style)
    {
        if (text.Length == 0) return;
        if (runs.Count > 0 && runs[^1].Style == style)
        {
            runs[^1] = runs[^1] with { Text = runs[^1].Text + text };
            return;
        }

        runs.Add(new CardLabelRun(text, style));
    }

    private static List<CardLabelRun> MergeAdjacent(List<CardLabelRun> runs)
    {
        if (runs.Count <= 1) return runs;
        var merged = new List<CardLabelRun>(runs.Count);
        foreach (var run in runs)
        {
            if (merged.Count > 0 && merged[^1].Style == run.Style)
            {
                merged[^1] = merged[^1] with { Text = merged[^1].Text + run.Text };
            }
            else
            {
                merged.Add(run);
            }
        }

        return merged;
    }
}