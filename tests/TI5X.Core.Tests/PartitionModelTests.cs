using System.Numerics;
using TI5X.Avalonia.Core;
using CorePartition = TI5X.Avalonia.Core.Partition;

namespace TI5X.Core.Tests;

/// <summary>
/// Differential proof: the C# partition arithmetic must agree with the
/// Dafny-verified model (src/TI5X.Verified/partition.dfy) over a swept input
/// space, and must satisfy the verified bounds: never negative, never above
/// the physical bank length, and within the logical partition.
/// </summary>
public class PartitionModelTests
{
    [Test]
    public async Task Partition_matches_Dafny_verified_model()
    {
        int checks = 0;
        for (int bank = 0; bank <= 1; ++bank)
        {
            for (int len = 0; len <= 1100; len += 7)
            {
                for (int ro = 0; ro <= 1010; ro += 13)
                {
                    int verified = (int)global::Partition.__default.EffectiveMaxProg(
                        new BigInteger(ro), new BigInteger(len), new BigInteger(bank));
                    int actual = CorePartition.EffectiveMaxProg(ro, len, bank);
                    if (actual != verified)
                    {
                        await Assert.That(actual).IsEqualTo(verified);
                    }
                    // Bounds proven by EffectiveMaxProgBounds in the model.
                    await Assert.That(actual >= 0 && actual <= len).IsTrue();
                    ++checks;
                }
            }
        }
        await Assert.That(checks).IsEqualTo(2 * 158 * 78);
    }

    [Test]
    public async Task Engine_GetMaxProg_uses_the_partition_bound()
    {
        var engine = new CalculatorEngine();
        engine.RegOffset = 1000; // Op 17 with 0 memories: 2000 logical steps
        await Assert.That(engine.GetMaxProg()).IsEqualTo(960); // capped by bank length

        var shortBank = new CalculatorEngine();
        shortBank.Banks[1] = new ProgBank(new byte[127]);
        shortBank.CurBank = 1;
        await Assert.That(shortBank.GetMaxProg()).IsEqualTo(127); // capped by file length

        var normal = new CalculatorEngine();
        await Assert.That(normal.GetMaxProg()).IsEqualTo(960); // default 479.59 partition
    }
}
