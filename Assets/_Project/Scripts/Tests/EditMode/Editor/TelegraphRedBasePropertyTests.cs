using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the shared telegraph red base color, the min-windup floor, and the
    /// single-beat preservation in the pure telegraph logic (<see cref="TelegraphFill"/>,
    /// <see cref="TelegraphIntensity"/>, <see cref="TelegraphWindupClock"/>, and
    /// <see cref="SingleBeatResolver"/>) — task 4.3 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// Every attack type shares the single predefined <see cref="TelegraphFill.RedBase"/>; its drawn
    /// color must lerp red->white by the same 0.65 * clamp01(fraction) factor that the archetype
    /// telegraph used, the windup must honour the 0.25s floor, and the single-beat rule must still
    /// resolve at most one damage beat per attack. The project cannot resolve FsCheck/CsCheck packages
    /// on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives >= 100
    /// deterministic generated cases per property and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class TelegraphRedBasePropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 11
        // Telegraph red base is applied for every attack type, with the single-beat rule and
        // min-windup floor preserved.
        // For any attack type, any authored windup, and any windup fraction:
        //   * TelegraphIntensity.ColorAt(TelegraphFill.RedBase, fraction) lerps the shared red base
        //     toward white by exactly 0.65 * clamp01(fraction);
        //   * TelegraphWindupClock(authored).Duration == max(0.25, authored); and
        //   * SingleBeatResolver resolves at most one beat per attack regardless of attempt count.
        // Validates: Requirements 5.1, 5.2, 5.3, 8.1, 8.2, 8.6
        [Test]
        public void RedBaseLerpsToWhiteWindupFlooredAndBeatSingleForEveryAttackType()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // The red base is the single predefined danger-zone color, shared by every attack
                // type (R5.1, R5.2). It must stay fixed and fully opaque regardless of attack type.
                Color redBase = TelegraphFill.RedBase;
                PropertyCheck.That(redBase == new Color(0.9f, 0.1f, 0.1f, 1f),
                    $"TelegraphFill.RedBase changed to {redBase}; it must be the single predefined red base");

                // Pick an arbitrary attack type / shape; the red base and lerp are type-independent (R5.3).
                EnemyAttackShape shape = RandomShape(rng);

                // --- Red base lerps red->white by 0.65 * clamp01(fraction) (R5.1, R5.2, R5.3) ------
                float fraction = RandomFraction(rng);
                float clamped = Mathf.Clamp01(fraction);
                float t = TelegraphIntensity.ImpactLerp * clamped; // 0.65 * clamp01(fraction)

                Color actual = TelegraphIntensity.ColorAt(TelegraphFill.RedBase, fraction);
                Color expected = Color.Lerp(redBase, Color.white, t);

                PropertyCheck.That(ApproximatelyEqual(actual, expected),
                    $"shape={shape}, fraction={fraction}: ColorAt {actual} != red->white lerp {expected} (t={t})");

                // The lerp factor is exactly 0.65 * clamp01(fraction) regardless of attack type.
                PropertyCheck.That(Mathf.Approximately(TelegraphIntensity.At(fraction), t),
                    $"fraction={fraction}: intensity {TelegraphIntensity.At(fraction)} != 0.65*clamp01={t}");

                // At fraction 0 the telegraph shows the pure red base; at/after 1 it reaches the
                // maximum red->white blend (0.65) — never fully white, matching the existing appearance.
                PropertyCheck.That(ApproximatelyEqual(TelegraphIntensity.ColorAt(TelegraphFill.RedBase, 0f), redBase),
                    "at fraction 0 the telegraph must be the pure red base");
                PropertyCheck.That(ApproximatelyEqual(
                        TelegraphIntensity.ColorAt(TelegraphFill.RedBase, 1f),
                        Color.Lerp(redBase, Color.white, TelegraphIntensity.ImpactLerp)),
                    "at fraction 1 the telegraph must reach the 0.65 red->white blend");

                // --- Min-windup floor preserved: Duration == max(0.25, authored) (R8.1, R8.2) ------
                float authored = RandomWindup(rng);
                var clock = new TelegraphWindupClock(authored);
                float expectedDuration = Mathf.Max(TelegraphBeat.MinWindupSeconds, authored);

                PropertyCheck.That(Mathf.Approximately(clock.Duration, expectedDuration),
                    $"authored={authored}: Duration {clock.Duration} != max(0.25, authored)={expectedDuration}");
                PropertyCheck.That(clock.Duration >= TelegraphBeat.MinWindupSeconds - 1e-6f,
                    $"authored={authored}: Duration {clock.Duration} fell below the 0.25s floor");

                // --- Single-beat rule preserved regardless of attack type (R8.6) --------------------
                var resolver = new SingleBeatResolver();
                var areas = new[] { RandomCircle(rng) };
                float damage = 1f + (float)rng.NextDouble() * 20f;

                int attempts = rng.Next(2, 8);
                int trueCount = 0;
                for (int a = 0; a < attempts; a++)
                    if (resolver.TryResolve(areas, Vector3.zero, damage)) trueCount++;

                PropertyCheck.That(trueCount <= 1,
                    $"shape={shape}: resolver produced {trueCount} beats for one attack (expected at most 1)");
                PropertyCheck.That(resolver.BeatResolved,
                    $"shape={shape}: an in-union positive-damage attack should have resolved exactly one beat");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>An arbitrary attack shape; the red base/lerp are shape (attack-type) independent.</summary>
        private static EnemyAttackShape RandomShape(System.Random rng)
        {
            var values = (EnemyAttackShape[])System.Enum.GetValues(typeof(EnemyAttackShape));
            return values[rng.Next(0, values.Length)];
        }

        /// <summary>Windup fractions spanning below 0, through [0,1], and past 1 to exercise clamping.</summary>
        private static float RandomFraction(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -1f - (float)rng.NextDouble() * 2f;  // below 0
                case 1: return 0f;                                  // exactly start
                case 2: return 1f;                                  // exactly impact
                case 3: return 1f + (float)rng.NextDouble() * 2f;   // past impact
                default: return (float)rng.NextDouble();            // within [0,1]
            }
        }

        /// <summary>Authored windups spanning negative, zero, sub-floor, exactly-floor, and above.</summary>
        private static float RandomWindup(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -1f + (float)rng.NextDouble() * 1f;    // negative .. 0
                case 1: return (float)rng.NextDouble() * 0.25f;       // 0 .. floor
                case 2: return 0.25f;                                 // exactly the floor
                default: return 0.25f + (float)rng.NextDouble() * 5f; // above the floor
            }
        }

        private static EnemyAttackArea RandomCircle(System.Random rng)
        {
            float reach = 1f + (float)rng.NextDouble() * 4f;
            Vector3 center = new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 0.5f, 0f,
                ((float)rng.NextDouble() - 0.5f) * 0.5f); // near origin so the origin stays inside
            return new EnemyAttackArea(EnemyAttackShape.Circle, center, Vector3.forward, reach);
        }

        private static bool ApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) <= 1e-5f
                && Mathf.Abs(a.g - b.g) <= 1e-5f
                && Mathf.Abs(a.b - b.b) <= 1e-5f
                && Mathf.Abs(a.a - b.a) <= 1e-5f;
        }
    }
}
