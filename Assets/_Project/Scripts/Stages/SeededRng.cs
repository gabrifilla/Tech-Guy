using System.Collections.Generic;

/// <summary>
/// Deterministic, fully isolated pseudo-random number generator seeded exclusively
/// by a 64-bit Run_Seed. It never consumes any shared/global randomness source
/// (in particular, it never touches <c>UnityEngine.Random</c>), so two generations
/// using the same Run_Seed always yield the same stream regardless of external state.
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 2.5.
/// <para>
/// The project does not depend on <c>com.unity.mathematics</c>, so this type is a
/// self-contained algorithm rather than a wrapper over <c>Unity.Mathematics.Random</c>.
/// The internal generator is xorshift128+, whose 128-bit state is initialized from the
/// raw <see cref="ulong"/> seed via splitmix64 (a well-known way to expand a single
/// seed into a well-distributed state, avoiding weak all-zero states).
/// </para>
/// </remarks>
public sealed class SeededRng
{
    private ulong _s0;
    private ulong _s1;

    /// <summary>The Run_Seed this generator was constructed with (for logging/reproducibility).</summary>
    public ulong Seed { get; }

    /// <summary>
    /// Creates a generator seeded exclusively by <paramref name="runSeed"/>.
    /// The same seed always produces the same sequence.
    /// </summary>
    public SeededRng(ulong runSeed)
    {
        Seed = runSeed;

        // Expand the single 64-bit seed into a 128-bit xorshift128+ state via splitmix64.
        ulong z = runSeed;
        _s0 = SplitMix64Step(ref z);
        _s1 = SplitMix64Step(ref z);

        // Guard against the degenerate all-zero state (xorshift128+ would be stuck at 0).
        if ((_s0 | _s1) == 0UL)
        {
            _s1 = 0x9E3779B97F4A7C15UL;
        }
    }

    /// <summary>Returns the next raw 64-bit value in the stream.</summary>
    public ulong NextULong()
    {
        // xorshift128+ (Vigna). Deterministic and dependency-free.
        ulong s1 = _s0;
        ulong s0 = _s1;
        _s0 = s0;
        s1 ^= s1 << 23;
        _s1 = s1 ^ s0 ^ (s1 >> 18) ^ (s0 >> 5);
        return unchecked(_s1 + s0);
    }

    /// <summary>Returns a non-negative 32-bit integer in [0, int.MaxValue].</summary>
    public int NextInt()
    {
        return (int)(NextULong() >> 33); // top 31 bits -> [0, 2^31 - 1]
    }

    /// <summary>
    /// Returns a deterministic integer in the half-open interval [<paramref name="minInclusive"/>,
    /// <paramref name="maxExclusive"/>). When the range is empty, returns <paramref name="minInclusive"/>.
    /// Uses rejection sampling so the distribution is unbiased across the range.
    /// </summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive)
        {
            return minInclusive;
        }

        ulong range = (ulong)((long)maxExclusive - minInclusive);
        return minInclusive + (int)(long)NextBounded(range);
    }

    /// <summary>
    /// Returns a deterministic integer in the inclusive interval
    /// [<paramref name="minInclusive"/>, <paramref name="maxInclusive"/>].
    /// </summary>
    public int NextIntInclusive(int minInclusive, int maxInclusive)
    {
        if (maxInclusive < minInclusive)
        {
            return minInclusive;
        }

        ulong range = (ulong)((long)maxInclusive - minInclusive) + 1UL;
        return minInclusive + (int)(long)NextBounded(range);
    }

    /// <summary>Returns a deterministic double in the half-open interval [0, 1).</summary>
    public double NextDouble()
    {
        // Use the top 53 bits for full double precision in [0,1).
        return (NextULong() >> 11) * (1.0 / 9007199254740992.0);
    }

    /// <summary>Returns a deterministic float in the half-open interval [0, 1).</summary>
    public float NextFloat()
    {
        return (float)NextDouble();
    }

    /// <summary>
    /// Deterministic probability roll: returns <c>true</c> with probability
    /// <paramref name="threshold"/> (a value in [0, 1]). A threshold &lt;= 0 never
    /// succeeds and a threshold &gt;= 1 always succeeds.
    /// </summary>
    public bool Roll(double threshold)
    {
        if (threshold <= 0.0)
        {
            return false;
        }

        if (threshold >= 1.0)
        {
            return true;
        }

        return NextDouble() < threshold;
    }

    /// <summary>
    /// Deterministic weighted choice: returns the index selected proportionally to the
    /// supplied non-negative <paramref name="weights"/>. Negative weights are treated as 0.
    /// Returns -1 when the collection is empty or the total weight is 0.
    /// </summary>
    public int WeightedChoice(IReadOnlyList<double> weights)
    {
        if (weights == null || weights.Count == 0)
        {
            return -1;
        }

        double total = 0.0;
        for (int i = 0; i < weights.Count; i++)
        {
            double w = weights[i];
            if (w > 0.0)
            {
                total += w;
            }
        }

        if (total <= 0.0)
        {
            return -1;
        }

        double pick = NextDouble() * total;
        double cumulative = 0.0;
        for (int i = 0; i < weights.Count; i++)
        {
            double w = weights[i];
            if (w <= 0.0)
            {
                continue;
            }

            cumulative += w;
            if (pick < cumulative)
            {
                return i;
            }
        }

        // Floating-point guard: return the last positive-weight index.
        for (int i = weights.Count - 1; i >= 0; i--)
        {
            if (weights[i] > 0.0)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns a uniformly distributed value in [0, <paramref name="range"/>) using
    /// rejection sampling to avoid modulo bias. Assumes <paramref name="range"/> &gt; 0.
    /// </summary>
    private ulong NextBounded(ulong range)
    {
        // Largest multiple of range that fits in ulong; values above it are rejected.
        ulong limit = ulong.MaxValue - (ulong.MaxValue % range);
        ulong value;
        do
        {
            value = NextULong();
        }
        while (value >= limit);

        return value % range;
    }

    /// <summary>
    /// One step of the splitmix64 mixing function, used only to initialize state.
    /// Kept private here; the run-to-run seed derivation lives in its own type (task 2.2).
    /// </summary>
    private static ulong SplitMix64Step(ref ulong state)
    {
        unchecked
        {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
