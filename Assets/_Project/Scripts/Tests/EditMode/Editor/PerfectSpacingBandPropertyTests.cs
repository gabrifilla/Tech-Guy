using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 8 of impactful-weapon-boons — the Perfect Spacing band on
    /// <see cref="WeaponRunModifiers.DirectDamageMultiplier"/>.
    ///
    /// Property 8 (design): for any direct-hit distance d and PerfectSpacing rank r, the Perfect Spacing
    /// multiplier equals (1 + 0.35*r) when 3.5 <= d <= 6.5 and exactly 1 otherwise.
    ///
    /// Isolating the term: <see cref="WeaponRunModifiers.DirectDamageMultiplier"/> also folds in Sniper
    /// (a Bow boon), SpearTip (d >= 3), Affliction (elements), Execution (target below 30% HP) and
    /// Berserker (player below 40% HP). To measure ONLY the PerfectSpacing contribution this test builds
    /// a Spear <see cref="WeaponRunModifiers"/> with nothing but PerfectSpacing ranks added (so SpearTip,
    /// a separate Spear boon, stays at rank 0 and is inert), and always evaluates with
    /// targetHealthRatio = 1, playerHealthRatio = 1 and elements = 0 so the Affliction/Execution/Berserker
    /// terms are all exactly 1. The PerfectSpacing effect is then read as the ratio of the rank-r
    /// multiplier to a rank-0 baseline multiplier at the same distance, which cancels every other term
    /// even if the surrounding formula changes. This project cannot resolve FsCheck/CsCheck on this
    /// machine, so the seeded <see cref="PropertyCheck"/> harness drives >= 100 deterministic cases and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class PerfectSpacingBandPropertyTests
    {
        private const float BandMin = 3.5f;
        private const float BandMax = 6.5f;
        private const float PerRank = 0.35f;
        private const float Tolerance = 1e-4f;

        private static WeaponRunModifiers.Definition PerfectSpacingDef()
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == WeaponBoon.PerfectSpacing) return definition;
            throw new System.InvalidOperationException("Missing catalog definition for PerfectSpacing");
        }

        // A Spear WeaponRunModifiers carrying exactly `rank` copies of PerfectSpacing (added through the
        // data entry point) and no other boon, so only the [3.5, 6.5] term can move the multiplier.
        private static WeaponRunModifiers SpearWithPerfectSpacing(int rank)
        {
            var mods = new WeaponRunModifiers(RunWeaponFamily.Spear);
            WeaponRunModifiers.Definition def = PerfectSpacingDef();
            for (int r = 0; r < rank; r++)
                PropertyCheck.That(mods.Add(def), $"PerfectSpacing rank {r + 1} should be addable");
            PropertyCheck.That(mods.Rank(WeaponBoon.PerfectSpacing) == rank,
                $"PerfectSpacing expected rank {rank}, got {mods.Rank(WeaponBoon.PerfectSpacing)}");
            return mods;
        }

        // The direct-hit multiplier evaluated with neutral inputs (full target/player HP, no elements),
        // so every non-PerfectSpacing term collapses to 1 and the PerfectSpacing band is the only factor.
        private static float Multiplier(WeaponRunModifiers mods, float distance) =>
            mods.DirectDamageMultiplier(distance, 1f, 1f, 0);

        // Feature: impactful-weapon-boons, Property 8: Perfect Spacing band
        // For any direct-hit distance d and PerfectSpacing rank r, the multiplier (isolated as the ratio of
        // the rank-r direct-hit multiplier to the rank-0 baseline at the same distance) equals (1 + 0.35*r)
        // when 3.5 <= d <= 6.5 and exactly 1 outside the band.
        // Validates: Requirements 4.1, 4.2
        [Test]
        public void PerfectSpacingMultiplier_IsBandGatedAndScalesWithRank()
        {
            int maxRank = PerfectSpacingDef().MaxRank;

            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(0, maxRank + 1);          // 0 .. MaxRank
                float distance = SampleDistance(rng, i);      // covers inside, both edges, and outside
                bool inBand = distance >= BandMin && distance <= BandMax;

                WeaponRunModifiers baseline = SpearWithPerfectSpacing(0);
                WeaponRunModifiers ranked = SpearWithPerfectSpacing(rank);

                float baseMultiplier = Multiplier(baseline, distance);
                float rankedMultiplier = Multiplier(ranked, distance);

                // With neutral inputs and no other boons, the rank-0 baseline is exactly 1 everywhere, so
                // the ratio isolates the PerfectSpacing contribution regardless of the surrounding formula.
                PropertyCheck.That(Mathf.Approximately(baseMultiplier, 1f),
                    $"[case #{i}] baseline multiplier at d={distance} expected 1 but was {baseMultiplier}");

                float ratio = rankedMultiplier / baseMultiplier;
                float expected = inBand ? 1f + PerRank * rank : 1f;

                PropertyCheck.That(Mathf.Abs(ratio - expected) <= Tolerance * Mathf.Max(1f, expected),
                    $"[case #{i}] d={distance} (inBand={inBand}) rank={rank}: PerfectSpacing ratio {ratio} " +
                    $"!= expected {expected} (ranked={rankedMultiplier}, baseline={baseMultiplier})");

                // Outside the band the bonus must vanish exactly, even at max rank.
                if (!inBand)
                    PropertyCheck.That(ratio == 1f,
                        $"[case #{i}] d={distance} outside [{BandMin}, {BandMax}] rank={rank}: expected " +
                        $"exactly 1x but ratio was {ratio}");
            });
        }

        // Distances that stress the band: interior points, both inclusive edges exactly, just-outside
        // points on either side, and far inside/outside values.
        private static float SampleDistance(System.Random rng, int index)
        {
            switch (index % 8)
            {
                case 0: return BandMin;                                            // lower edge (inclusive)
                case 1: return BandMax;                                            // upper edge (inclusive)
                case 2: return BandMin - 1e-3f;                                    // just below the band
                case 3: return BandMax + 1e-3f;                                    // just above the band
                case 4: return BandMin + (float)rng.NextDouble() * (BandMax - BandMin); // inside the band
                case 5: return (float)rng.NextDouble() * BandMin;                  // 0 .. 3.5, below the band
                case 6: return BandMax + (float)rng.NextDouble() * 20f;            // well above the band
                default: return (float)rng.NextDouble() * 30f;                     // anywhere in 0 .. 30 m
            }
        }
    }
}
