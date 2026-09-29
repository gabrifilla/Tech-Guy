using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure damage-absorb and heal-clamp math used by the archetype
    /// stack — task 2.3 of enemy-swarm-core-archetypes.
    ///
    /// The <see cref="Shield"/> and <see cref="Actor"/> behaviors are scene/MonoBehaviour driven, so
    /// their invariants were extracted into plain C# (<see cref="ShieldBuffer"/> for absorb,
    /// <see cref="HealMath"/> for the heal clamp) to be property-checked without a live Unity scene.
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class ShieldHealPropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 7: Shield absorbs before health and never increases health.
        // For any sequence of incoming damage amounts against a shield buffer, each hit's leftover is
        // in [0, amount], the total absorbed never exceeds the buffer's capacity, the buffer never
        // increases (health is never raised by taking damage), and once depleted all further damage
        // passes straight through to health.
        // Validates: Requirements 14.3
        [Test]
        public void ShieldAbsorbsBeforeHealthAndNeverIncreasesHealth()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float capacity = RandomCapacity(rng);
                var buffer = new ShieldBuffer(capacity);
                float expectedRemaining = Mathf.Max(0f, capacity);

                // The buffer clamps a negative authored capacity to zero and reports depletion.
                PropertyCheck.That(buffer.Remaining >= 0f,
                    $"capacity={capacity}: initial remaining {buffer.Remaining} is negative");
                PropertyCheck.That(Mathf.Approximately(buffer.Remaining, expectedRemaining),
                    $"capacity={capacity}: initial remaining {buffer.Remaining} != expected {expectedRemaining}");

                int hits = rng.Next(1, 10);
                float totalAbsorbed = 0f;
                for (int h = 0; h < hits; h++)
                {
                    float amount = RandomDamage(rng);
                    float remainingBefore = buffer.Remaining;
                    float leftover = buffer.Absorb(amount);

                    // Leftover of a hit is always in [0, amount] (non-positive hits absorb nothing).
                    float clampedAmount = Mathf.Max(0f, amount);
                    PropertyCheck.That(leftover >= -1e-6f && leftover <= clampedAmount + 1e-6f,
                        $"capacity={capacity}, amount={amount}: leftover {leftover} outside [0,{clampedAmount}]");

                    // Health is only ever reduced: the absorbed portion never becomes negative, so a
                    // hit can never raise the leftover above the incoming amount (never heals).
                    float absorbedThisHit = clampedAmount - leftover;
                    PropertyCheck.That(absorbedThisHit >= -1e-6f,
                        $"capacity={capacity}, amount={amount}: absorbed {absorbedThisHit} is negative");

                    // The buffer only shrinks; it never grows back on a hit.
                    PropertyCheck.That(buffer.Remaining <= remainingBefore + 1e-6f,
                        $"capacity={capacity}, amount={amount}: remaining grew {remainingBefore} -> {buffer.Remaining}");
                    PropertyCheck.That(buffer.Remaining >= -1e-6f,
                        $"capacity={capacity}, amount={amount}: remaining {buffer.Remaining} went negative");

                    totalAbsorbed += absorbedThisHit;

                    // Once depleted, a positive hit passes straight through (leftover == amount).
                    if (buffer.IsDepleted && clampedAmount > 0f)
                    {
                        float passthrough = buffer.Absorb(amount);
                        PropertyCheck.That(Mathf.Approximately(passthrough, clampedAmount),
                            $"capacity={capacity}: depleted buffer absorbed {clampedAmount - passthrough} instead of passing through");
                        totalAbsorbed += clampedAmount - passthrough;
                    }
                }

                // Total absorbed across the whole attack sequence never exceeds the capacity.
                PropertyCheck.That(totalAbsorbed <= expectedRemaining + 1e-4f,
                    $"capacity={capacity}: total absorbed {totalAbsorbed} exceeded capacity {expectedRemaining}");
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 8: Heal clamps to maximum and never exceeds it.
        // For any current health and any heal amount, the result equals min(maxHealth, health + amount)
        // for a positive amount, never exceeds maxHealth, never drops below the starting health, and is
        // a no-op for a non-positive amount.
        // Validates: Requirements 13.3
        [Test]
        public void HealClampsToMaximumAndNeverExceedsIt()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float maxHealth = 1f + (float)rng.NextDouble() * 999f;
                float health = (float)rng.NextDouble() * maxHealth; // starting health in [0, maxHealth]
                float amount = RandomHealAmount(rng);

                float result = HealMath.Clamp(health, maxHealth, amount);

                // Never exceeds the maximum.
                PropertyCheck.That(result <= maxHealth + 1e-4f,
                    $"health={health}, max={maxHealth}, amount={amount}: result {result} exceeded max");

                // Never drops below the starting health (heal never harms).
                PropertyCheck.That(result >= health - 1e-4f,
                    $"health={health}, max={maxHealth}, amount={amount}: result {result} below starting health");

                if (amount <= 0f)
                {
                    // No-op semantics: a non-positive amount leaves health unchanged.
                    PropertyCheck.That(Mathf.Approximately(result, health),
                        $"health={health}, amount={amount}: non-positive heal changed health to {result}");
                }
                else
                {
                    // Positive heal equals the clamped sum exactly.
                    float expected = Mathf.Min(maxHealth, health + amount);
                    PropertyCheck.That(Mathf.Approximately(result, expected),
                        $"health={health}, max={maxHealth}, amount={amount}: result {result} != min(max, health+amount)={expected}");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Capacities spanning negative (clamped to 0), zero, and positive values.</summary>
        private static float RandomCapacity(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return -5f + (float)rng.NextDouble() * 5f; // negative .. 0
                case 1: return 0f;                                 // exactly zero
                default: return (float)rng.NextDouble() * 100f;    // positive
            }
        }

        /// <summary>Damage hits spanning negative, zero, small, and large values.</summary>
        private static float RandomDamage(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -10f + (float)rng.NextDouble() * 10f; // negative .. 0
                case 1: return 0f;                                   // exactly zero
                default: return (float)rng.NextDouble() * 60f;       // positive
            }
        }

        /// <summary>Heal amounts spanning negative, zero, small, and large values.</summary>
        private static float RandomHealAmount(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -50f + (float)rng.NextDouble() * 50f; // negative .. 0
                case 1: return 0f;                                   // exactly zero
                default: return (float)rng.NextDouble() * 2000f;     // positive, can overshoot max
            }
        }
    }
}
