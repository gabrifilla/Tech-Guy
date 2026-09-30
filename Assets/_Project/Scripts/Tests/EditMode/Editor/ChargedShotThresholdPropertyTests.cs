using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="ChargedShot"/> helper (R3).
    ///
    /// Feature: impactful-weapon-boons, Property 7 (Charged Shot threshold): for any held duration h
    /// and rank r >= 1, the shot is charged (damage x (1 + 0.75r), piercing) if and only if h >= 0.6s;
    /// otherwise it is a normal arrow (multiplier 1, non-piercing).
    /// Validates: Requirements 3.1, 3.2, 3.3
    ///
    /// The helper carries no Unity types, so this stays a fast, deterministic, physics-free EditMode
    /// test driven by the project's seeded PropertyCheck harness (>= 100 generated cases by default).
    /// </summary>
    public sealed class ChargedShotThresholdPropertyTests
    {
        private const float ChargeTime = .6f; // grounded in ChargedShot.ChargeTime (R3.1)

        // Feature: impactful-weapon-boons, Property 7
        // Validates: Requirements 3.1, 3.2, 3.3
        [Test]
        public void ChargedShot_ThresholdAndMultiplier_HoldTheBiconditional()
        {
            // The const must match the spec threshold (R3.1) exactly.
            PropertyCheck.That(Mathf.Abs(ChargedShot.ChargeTime - ChargeTime) <= float.Epsilon,
                $"ChargedShot.ChargeTime must be {ChargeTime}s but was {ChargedShot.ChargeTime}s");

            PropertyCheck.ForAll((rng, i) =>
            {
                // Held duration spans clearly-below, near-threshold, and clearly-above the 0.6s gate.
                float held = (float)rng.NextDouble() * 1.5f; // 0 .. 1.5s
                int rank = rng.Next(1, 4);                    // active boon ranks 1..3

                bool charged = ChargedShot.IsCharged(held);
                bool expectedCharged = held >= ChargeTime;

                // R3.1/R3.3: charged iff held >= 0.6s (the biconditional).
                PropertyCheck.That(charged == expectedCharged,
                    $"IsCharged({held}) returned {charged} but held >= {ChargeTime} is {expectedCharged}");

                // R3.2: charged arrow damage multiplier is exactly 1 + 0.75 * rank.
                float multiplier = ChargedShot.DamageMultiplier(rank);
                float expectedMultiplier = 1f + .75f * rank;
                PropertyCheck.That(Mathf.Abs(multiplier - expectedMultiplier) <= 1e-5f * Mathf.Max(1f, expectedMultiplier),
                    $"DamageMultiplier({rank})={multiplier} but expected {expectedMultiplier}");

                // R3.2: a charged shot always overpowers a normal one (multiplier > 1 for rank >= 1).
                PropertyCheck.That(multiplier > 1f,
                    $"charged multiplier must exceed 1 for rank {rank} but was {multiplier}");

                // R3.3: the effective multiplier the shot actually fires with. A release before the
                // threshold falls back to the normal arrow (multiplier 1, non-piercing); at/after the
                // threshold it fires the charged arrow (piercing) with the rank multiplier.
                float effective = charged ? multiplier : 1f;
                bool piercing = charged; // charged shots are marked piercing (R3.2), normals are not.

                if (expectedCharged)
                {
                    PropertyCheck.That(Mathf.Abs(effective - expectedMultiplier) <= 1e-5f * Mathf.Max(1f, expectedMultiplier),
                        $"held={held} (>= {ChargeTime}) must fire charged multiplier {expectedMultiplier} but was {effective}");
                    PropertyCheck.That(piercing,
                        $"held={held} (>= {ChargeTime}) must fire a piercing arrow");
                }
                else
                {
                    PropertyCheck.That(Mathf.Abs(effective - 1f) <= float.Epsilon,
                        $"held={held} (< {ChargeTime}) must fire a normal arrow (multiplier 1) but was {effective}");
                    PropertyCheck.That(!piercing,
                        $"held={held} (< {ChargeTime}) must fire a non-piercing arrow");
                }
            });
        }

        // Feature: impactful-weapon-boons, Property 7 (normal-shot semantics)
        // Validates: Requirements 3.3
        // Rank 0 (boon not owned) yields multiplier 1, i.e. a normal shot regardless of hold time.
        [Test]
        public void ChargedShot_RankZero_YieldsNormalShotMultiplier()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float held = (float)rng.NextDouble() * 1.5f; // 0 .. 1.5s
                float multiplier = ChargedShot.DamageMultiplier(0);
                PropertyCheck.That(Mathf.Abs(multiplier - 1f) <= float.Epsilon,
                    $"rank 0 must yield multiplier 1 (normal shot) but was {multiplier} (held={held})");
            });
        }
    }
}
