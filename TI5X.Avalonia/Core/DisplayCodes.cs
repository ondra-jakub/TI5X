namespace TI5X.Avalonia.Core;

/// <summary>
/// Segment bitmasks and character codes for the 12-digit 7-segment LED display.
///
/// Segment masks are:
///      --- 0x001 (a)
/// | 0x002 (f)  | 0x004 (b)
///      --- 0x008 (g)
/// | 0x010 (e)  | 0x020 (c)
///      --- 0x040 (d)  . 0x100 (dp)
/// </summary>
public static class DisplayCodes
{
    public const int SegA = 0x001;
    public const int SegF = 0x002;
    public const int SegB = 0x004;
    public const int SegG = 0x008; // Middle bar / minus
    public const int SegE = 0x010;
    public const int SegC = 0x020;
    public const int SegD = 0x040;
    public const int SegDp = 0x100; // Decimal point

    public const int NrDigits = 12;

    public static int SegmentCode(char c)
    {
        return c switch
        {
            '0' => SegA | SegB | SegC | SegD | SegE | SegF,         // 0x077
            '1' => SegB | SegC,                                     // 0x024
            '2' => SegA | SegB | SegG | SegE | SegD,                // 0x05D
            '3' => SegA | SegB | SegG | SegC | SegD,                // 0x06D
            '4' => SegF | SegB | SegG | SegC,                       // 0x02E
            '5' => SegA | SegF | SegG | SegC | SegD,                // 0x06B
            '6' => SegA | SegF | SegG | SegE | SegC | SegD,         // 0x07B
            '7' => SegA | SegB | SegC,                              // 0x025
            '8' => SegA | SegB | SegC | SegD | SegE | SegF | SegG,  // 0x07F
            '9' => SegA | SegB | SegC | SegD | SegF | SegG,         // 0x06F
            '-' => SegG,                                            // 0x008
            '.' => SegDp,                                           // 0x100
            'C' => SegA | SegF | SegE | SegD,                       // 0x053
            'E' => SegA | SegF | SegG | SegE | SegD,                // 0x05B
            'F' => SegA | SegF | SegG | SegE,                       // 0x01B
            'H' => SegF | SegB | SegG | SegE | SegC,                // 0x03E
            'L' => SegF | SegE | SegD,                              // 0x052
            'P' => SegA | SegF | SegB | SegG | SegE,                // 0x01F
            'r' => SegG | SegE,                                     // 0x018
            'o' => SegG | SegE | SegC | SegD,                       // 0x078
            ' ' => 0,
            _ => 0
        };
    }

    /// <summary>
    /// Converts a formatted display string into an array of 12 segment masks.
    /// Handles embedded decimal points properly by attaching them to the preceding digit.
    /// </summary>
    public static int[] StringToSegments(string? text)
    {
        var result = new int[NrDigits];
        if (string.IsNullOrEmpty(text))
            return result;

        int j = NrDigits;
        bool decimalPending = false;
        for (int i = text.Length - 1; i >= 0 && j > 0; --i)
        {
            char ch = text[i];
            if (ch == '.')
            {
                decimalPending = true;
            }
            else
            {
                --j;
                result[j] = SegmentCode(ch) | (decimalPending ? SegDp : 0);
                decimalPending = false;
            }
        }

        return result;
    }
}
