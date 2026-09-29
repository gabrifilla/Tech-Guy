using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure next-seed derivation in <see cref="SeedDerivation"/> —
    /// task 2.3 of procedural-stage-room-generation.
    ///
    /// Next-seed derivation (used when transitioning to the next Stage) is a dependency-free pure
    /// function, so it can be property-checked without a live Unity scene (Property 17). This
    /// project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases and reports the
    /// exact failing case as a counterexample.
    /// </summary>
    public sealed class NextSeedDeterminismPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 17: Determinismo da próxima seed
        // Para todo Run_Seed atual de 64 bits, SplitMix64 é determinística: chamada duas vezes com o
        // mesmo currentSeed produz exatamente o mesmo nextSeed. Cobre o espaço completo de ulong,
        // incluindo os limites (0 e ulong.MaxValue) e valores arbitrários compostos de 64 bits.
        // Validates: Requirements 7.5
        [Test]
        public void SplitMix64IsDeterministicForTheSameCurrentSeed()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                ulong currentSeed = RandomSeed(rng);

                ulong first = SeedDerivation.SplitMix64(currentSeed);
                ulong second = SeedDerivation.SplitMix64(currentSeed);

                PropertyCheck.That(first == second,
                    $"SplitMix64 not deterministic for currentSeed={currentSeed}: " +
                    $"first call returned {first}, second call returned {second}.");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// A 64-bit current seed spanning the whole ulong input space: the boundary values (0 and
        /// ulong.MaxValue), plus arbitrary values composed from two independent 32-bit draws so the
        /// full 64-bit range (not just the low int range) is exercised.
        /// </summary>
        private static ulong RandomSeed(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0:
                    return 0UL;
                case 1:
                    return ulong.MaxValue;
                default:
                    uint high = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
                    uint low = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
                    return ((ulong)high << 32) | low;
            }
        }
    }
}
