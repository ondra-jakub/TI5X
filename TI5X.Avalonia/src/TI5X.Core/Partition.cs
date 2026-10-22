namespace TI5X.Avalonia.Core;

/// <summary>
/// Memory partition arithmetic for the calculator engine.
///
/// This mirrors the Dafny-verified model in <c>src/TI5X.Verified/partition.dfy</c>
/// (lemma <c>EffectiveMaxProgBounds</c> proves the result is never negative and
/// never exceeds the physical bank length). The test suite proves that this
/// implementation and the verified generated code agree over a swept input
/// space; keep both in sync.
/// </summary>
public static class Partition
{
    public const int MaxLibProgram = 1000;

    /// <summary>
    /// Usable program steps for the current partition and bank: the logical
    /// size (two steps per relocated register on bank 0) capped by the real
    /// bank array length.
    /// </summary>
    public static int EffectiveMaxProg(int regOffset, int bankLength, int curBank)
    {
        int logical = curBank == 0 ? regOffset * 2 : MaxLibProgram;
        return Math.Min(logical, bankLength);
    }
}
