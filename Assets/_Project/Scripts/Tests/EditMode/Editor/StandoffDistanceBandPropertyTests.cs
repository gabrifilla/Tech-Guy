using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure standoff math in <see cref="RetreatCadence"/> —
    /// task 3.5 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// The standoff distance and reposition target are consumed by the EnemyAI movement layer, but
    /// the invariants "the standoff distance stays inside the engagement band" and "the reposition
    /// target scales by a bounded margin" are pure arithmetic and were extracted into plain,
    /// scene-free C# so they can be property-checked without a live Unity scene. FsCheck/CsCheck are
    /// not available on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives
    /// &gt;= 100 deterministic generated cases per property and reports the exact failing case as a
    /// counterexample.
    /// </summary>
    public sealed class StandoffDistanceBandPropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 9
        // Standoff distance stays inside the engagement band and the reposition target scales by margin.
        // For any engagement band > 0, fraction in [0,1], and margin in [1,2],
        // RetreatCadence.StandoffDistance == band * clamp01(fraction) <= band, and
        // RepositionTarget == standoff * clamp(margin, 1, 2).
        // Validates: Requirements 2.1, 2.2, 2.3
        [Test]
        public void StandoffStaysInBandAndRepositionScalesByMargin()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float band = RandomBand(rng);
                float fraction = RandomFraction(rng);
                float margin = RandomMargin(rng);

                float clampedBand = Mathf.Max(0f, band);
                float clampedFraction = Mathf.Clamp01(fraction);
                float clampedMargin = Mathf.Clamp(margin, 1f, 2f);

                float standoff = RetreatCadence.StandoffDistance(band, fraction);
                float reposition = RetreatCadence.RepositionTarget(standoff, margin);

                // R2.1: standoff distance equals band * clamp01(fraction).
                float expectedStandoff = clampedBand * clampedFraction;
                PropertyCheck.That(Mathf.Abs(standoff - expectedStandoff) <= 1e-4f * Mathf.Max(1f, clampedBand),
                    $"band={band}, fraction={fraction}: standoff {standoff} != expected {expectedStandoff}");

                // R2.1: because the fraction is clamped to <= 1, the standoff never leaves the band,
                // so a Ranged_Enemy stays reachable by melee.
                PropertyCheck.That(standoff <= clampedBand + 1e-4f * Mathf.Max(1f, clampedBand),
                    $"band={band}, fraction={fraction}: standoff {standoff} escaped the engagement band {clampedBand}");

                // R2.1: standoff is never negative.
                PropertyCheck.That(standoff >= -1e-6f,
                    $"band={band}, fraction={fraction}: standoff {standoff} is negative");

                // R2.2/R2.3: the reposition target equals standoff * clamp(margin, 1, 2).
                float expectedReposition = standoff * clampedMargin;
                PropertyCheck.That(Mathf.Abs(reposition - expectedReposition) <= 1e-4f * Mathf.Max(1f, expectedReposition),
                    $"standoff={standoff}, margin={margin}: reposition {reposition} != expected {expectedReposition}");

                // R2.2/R2.3: because the margin is clamped to [1,2], the reposition target always sits
                // at or beyond the standoff distance (margin >= 1) and never beyond twice it (margin <= 2),
                // so the enemy backs off but stays reachable.
                PropertyCheck.That(reposition >= standoff - 1e-4f * Mathf.Max(1f, standoff),
                    $"standoff={standoff}, margin={margin}: reposition {reposition} fell short of the standoff distance");
                PropertyCheck.That(reposition <= standoff * 2f + 1e-4f * Mathf.Max(1f, standoff),
                    $"standoff={standoff}, margin={margin}: reposition {reposition} exceeded twice the standoff distance");

                // R2.1/R2.2: determinism — identical inputs yield identical outputs.
                float standoffAgain = RetreatCadence.StandoffDistance(band, fraction);
                float repositionAgain = RetreatCadence.RepositionTarget(standoff, margin);
                PropertyCheck.That(standoff == standoffAgain && reposition == repositionAgain,
                    $"band={band}, fraction={fraction}, margin={margin}: non-deterministic output");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Engagement bands spanning zero, small, typical, and large values.</summary>
        private static float RandomBand(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;                                      // degenerate band
                case 1: return (float)rng.NextDouble();                 // small (0,1)
                case 2: return 1f + (float)rng.NextDouble() * 14f;      // typical [1,15]
                case 3: return 15f + (float)rng.NextDouble() * 85f;     // large [15,100]
                default: return -1f * (float)rng.NextDouble();          // negative (clamps to 0)
            }
        }

        /// <summary>Standoff fractions spanning below, inside, and above the [0,1] clamp range.</summary>
        private static float RandomFraction(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return 0f;                                      // floor
                case 1: return 1f;                                      // ceiling (standoff == band)
                case 2: return 0.45f;                                  // the default
                case 3: return (float)rng.NextDouble();                // inside [0,1]
                case 4: return 1f + (float)rng.NextDouble();           // above range (clamps to 1)
                default: return -1f * (float)rng.NextDouble();         // below range (clamps to 0)
            }
        }

        /// <summary>Standoff margins spanning below, inside, and above the [1,2] clamp range.</summary>
        private static float RandomMargin(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return 1f;                                      // floor (no overshoot)
                case 1: return 2f;                                      // ceiling (double)
                case 2: return 1.15f;                                  // the default
                case 3: return 1f + (float)rng.NextDouble();           // inside [1,2]
                case 4: return 2f + (float)rng.NextDouble() * 3f;      // above range (clamps to 2)
                default: return (float)rng.NextDouble();               // below range (clamps to 1)
            }
        }
    }
}
