/// <summary>
/// Deterministic run-to-run seed derivation. Given the current 64-bit Run_Seed,
/// derives the next Run_Seed via a single splitmix64 mixing step, so the same
/// current seed always yields the same next seed.
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 7.5.
/// <para>
/// This helper is the PUBLIC, pure counterpart of the private splitmix64 step used
/// only for internal state initialization inside <see cref="SeededRng"/>. It exists
/// as a dedicated type so next-seed derivation (used when transitioning to the next
/// Stage) is testable in isolation and fully dependency-free: it never touches
/// <c>UnityEngine.Random</c> or any shared/global randomness source.
/// </para>
/// </remarks>
public static class SeedDerivation
{
    /// <summary>
    /// One step of the splitmix64 mixing function applied to <paramref name="currentSeed"/>,
    /// producing a well-distributed next seed. Deterministic and pure: the same
    /// <paramref name="currentSeed"/> always returns the same value.
    /// </summary>
    /// <param name="currentSeed">The current 64-bit Run_Seed.</param>
    /// <returns>The deterministically derived next 64-bit Run_Seed.</returns>
    public static ulong SplitMix64(ulong currentSeed)
    {
        unchecked
        {
            ulong z = currentSeed + 0x9E3779B97F4A7C15UL;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
