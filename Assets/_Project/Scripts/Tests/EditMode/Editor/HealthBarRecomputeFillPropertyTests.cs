using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the event-driven health bar of <see cref="Actor"/> — task 8.4 of
    /// project-cleanup-optimization (Requisito 4.2).
    ///
    /// The live bar is scene/MonoBehaviour driven (an <see cref="UnityEngine.UI.Image"/>'s
    /// <c>fillAmount</c> reassigned from <see cref="Actor.HealthChanged"/> instead of per-frame). The
    /// two universal facts that survive without a scene are pure:
    ///   (1) the displayed fill is <c>clamp01(health / max)</c> via the static
    ///       <see cref="Actor.HealthBarFill(float,float)"/>, with the same non-positive-max guard the
    ///       inline code used; and
    ///   (2) the "recompute only when it changed" decision, which the HUD/bar expresses through the
    ///       pure <see cref="UiValueCache{T}"/> dirty-tracker — it (re)emits iff the source value
    ///       differs from the previous one.
    /// Pairing <see cref="Actor.HealthBarFill(float,float)"/> with a <see cref="UiValueCache{T}"/> over
    /// the fill reproduces the on-screen bar's reassign-iff-changed behavior with zero scene.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (128 by
    /// default) and reports the exact failing mutation sequence as a counterexample — matching the
    /// convention of the sibling pure-logic property tests (e.g. ProjectileMotionPropertyTests,
    /// SimpleObjectPoolPropertyTests).
    ///
    /// Validates: Requirements 4.2
    /// </summary>
    public sealed class HealthBarRecomputeFillPropertyTests
    {
        // Feature: project-cleanup-optimization, Property 5: para qualquer sequência de mutações de
        // vida, a barra recomputa sse o valor mudou, e o valor é clamp01(health/max).
        //
        // For any sequence of (health, maxHealth) mutations fed to the bar, we drive a UiValueCache
        // over the computed fill (the single value the bar reassigns) and assert, after every
        // mutation, that:
        //   (a) the fill the bar would show equals clamp01(health / max(max, 0.0001)) — the exact
        //       static formula Actor.HealthBarFill uses, so the on-screen value is unchanged (R4.2);
        //   (b) the fill is always within [0, 1] (clamp holds for any health/max, including negative,
        //       zero, overshoot and the non-positive-max guard);
        //   (c) the bar recomputes (the cache reports a change) if and only if the fill differs from
        //       the previously displayed fill — the first mutation always recomputes, an identical
        //       repeat never does, and a different value always does; and
        //   (d) when no recompute happens the cached (displayed) fill is unchanged, and when it does
        //       happen the cache now holds exactly the new fill.
        // Validates: Requirements 4.2
        [Test]
        public void BarRecomputesIffFillChangedAndFillIsClampedRatio()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // The pure dirty-tracker standing in for the bar's reassign-iff-changed decision: the
                // bar only reassigns Image.fillAmount when this reports the fill changed.
                var fillCache = new UiValueCache<float>();

                // Model of what the bar is currently displaying: whether anything has been shown yet
                // and, if so, the last fill value reassigned.
                bool hasDisplayed = false;
                float displayedFill = 0f;

                int mutations = rng.Next(20, 60);
                for (int m = 0; m < mutations; m++)
                {
                    // Decide this mutation's (health, max). Bias toward repeating the *previous*
                    // health/max so the "unchanged -> no recompute" branch is exercised often, while
                    // still visiting negative/zero/overshoot and the non-positive-max guard.
                    float health;
                    float maxHealth;
                    bool forceRepeat = hasDisplayed && rng.Next(0, 100) < 35;
                    if (forceRepeat)
                    {
                        // Re-feed values that reproduce the exact same fill. Recomputing the same
                        // health/max is the simplest guaranteed-identical fill, so we reuse the inputs
                        // that produced the current displayed fill by regenerating from the same draw.
                        health = _lastHealth;
                        maxHealth = _lastMaxHealth;
                    }
                    else
                    {
                        health = RandomHealth(rng);
                        maxHealth = RandomMaxHealth(rng);
                    }
                    _lastHealth = health;
                    _lastMaxHealth = maxHealth;

                    string ctx = $"m#{m} health={health} max={maxHealth}";

                    // (a) The fill the bar would compute is exactly the static formula.
                    float fill = Actor.HealthBarFill(health, maxHealth);
                    float expected = Mathf.Clamp01(health / Mathf.Max(maxHealth, 0.0001f));
                    PropertyCheck.That(Mathf.Approximately(fill, expected),
                        $"{ctx}: HealthBarFill={fill} differed from clamp01(health/max(max,0.0001))={expected}");

                    // (b) The fill is always a valid [0,1] fillAmount regardless of inputs.
                    PropertyCheck.That(fill >= 0f && fill <= 1f,
                        $"{ctx}: fill {fill} outside [0,1]");

                    // Snapshot the displayed state before deciding whether to recompute. The cache
                    // decides with EqualityComparer<float>.Default (exact equality), so the model
                    // mirrors it with exact equality rather than Mathf.Approximately — otherwise two
                    // bit-distinct-but-approximately-equal fills would disagree with the cache.
                    bool hadDisplayedBefore = hasDisplayed;
                    float displayedBefore = displayedFill;
                    bool fillDiffersFromDisplayed =
                        !hadDisplayedBefore || displayedBefore != fill;

                    // (c) The cache recomputes iff the fill differs from what is displayed.
                    bool recomputed = fillCache.HasChanged(fill);
                    PropertyCheck.That(recomputed == fillDiffersFromDisplayed,
                        $"{ctx}: recompute decision {recomputed} disagreed with fill-changed {fillDiffersFromDisplayed} " +
                        $"(displayedBefore={(hadDisplayedBefore ? displayedBefore.ToString() : "none")} fill={fill})");

                    // First mutation always recomputes; it must never be skipped.
                    if (!hadDisplayedBefore)
                    {
                        PropertyCheck.That(recomputed,
                            $"{ctx}: first mutation did not recompute the bar");
                    }

                    // Apply the bar's reaction: reassign the displayed fill only on recompute.
                    if (recomputed)
                    {
                        hasDisplayed = true;
                        displayedFill = fill;
                    }

                    // (d) Reconcile the model with the cache after the decision.
                    if (recomputed)
                    {
                        PropertyCheck.That(Mathf.Approximately(fillCache.Current, fill),
                            $"{ctx}: after recompute the cache held {fillCache.Current} instead of {fill}");
                        PropertyCheck.That(Mathf.Approximately(displayedFill, fill),
                            $"{ctx}: after recompute the displayed fill is {displayedFill} instead of {fill}");
                    }
                    else
                    {
                        // No recompute: the displayed fill is untouched and already equals this fill.
                        PropertyCheck.That(Mathf.Approximately(displayedFill, displayedBefore),
                            $"{ctx}: skipped recompute but displayed fill changed {displayedBefore} -> {displayedFill}");
                        PropertyCheck.That(Mathf.Approximately(displayedFill, fill),
                            $"{ctx}: skipped recompute yet displayed fill {displayedFill} != current fill {fill}");
                    }

                    // The cache always mirrors the displayed fill once anything has been shown.
                    PropertyCheck.That(fillCache.HasValue == hasDisplayed,
                        $"{ctx}: cache.HasValue={fillCache.HasValue} disagreed with displayed state {hasDisplayed}");
                    if (hasDisplayed)
                    {
                        PropertyCheck.That(Mathf.Approximately(fillCache.Current, displayedFill),
                            $"{ctx}: cache.Current={fillCache.Current} drifted from displayed fill {displayedFill}");
                    }
                }
            });
        }

        // Feature: project-cleanup-optimization, Property 5 (focused idempotence/change facet):
        // feeding the SAME fill twice recomputes exactly once (the repeat is skipped), and feeding a
        // genuinely different fill always recomputes — an isolated demonstration that the bar's
        // reassign is driven purely by whether the clamped ratio changed, not by the raw health/max.
        // Validates: Requirements 4.2
        [Test]
        public void IdenticalFillSkipsRecomputeWhileDifferentFillRecomputes()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var fillCache = new UiValueCache<float>();

                float health = RandomHealth(rng);
                float maxHealth = RandomMaxHealth(rng);
                float fill = Actor.HealthBarFill(health, maxHealth);

                // First feed always recomputes.
                PropertyCheck.That(fillCache.HasChanged(fill),
                    $"health={health} max={maxHealth}: first feed did not recompute");

                // Re-feeding the identical fill (same inputs) is always skipped — idempotent display.
                int repeats = rng.Next(1, 6);
                for (int r = 0; r < repeats; r++)
                {
                    PropertyCheck.That(!fillCache.HasChanged(fill),
                        $"health={health} max={maxHealth}: identical fill #{r} recomputed instead of being skipped");
                    PropertyCheck.That(Mathf.Approximately(fillCache.Current, fill),
                        $"health={health} max={maxHealth}: identical repeat drifted the cached fill to {fillCache.Current}");
                }

                // Now construct a genuinely different fill. Picking a health that yields a different
                // clamped ratio must recompute; if we cannot produce a different fill (e.g. already
                // saturated at the same clamp endpoint for every attempt) we simply assert the
                // skip-on-equal invariant still holds, so the property is never vacuously passed.
                float otherHealth = DifferentFillHealth(rng, maxHealth, fill, out bool producedDifferent);
                float otherFill = Actor.HealthBarFill(otherHealth, maxHealth);
                // The cache decides with exact float equality, so branch on exact equality too: any
                // bit-distinct fill must recompute, a bit-identical one must be skipped.
                if (producedDifferent && otherFill != fill)
                {
                    PropertyCheck.That(fillCache.HasChanged(otherFill),
                        $"health={otherHealth} max={maxHealth}: a different fill {otherFill} (was {fill}) did not recompute");
                    PropertyCheck.That(Mathf.Approximately(fillCache.Current, otherFill),
                        $"health={otherHealth} max={maxHealth}: after a changed fill the cache held {fillCache.Current} instead of {otherFill}");
                }
                else
                {
                    // Could not produce a distinct fill: the equal value must still be skipped.
                    PropertyCheck.That(!fillCache.HasChanged(otherFill),
                        $"health={otherHealth} max={maxHealth}: an equal fill {otherFill} recomputed unexpectedly");
                }
            });
        }

        // ---- per-iteration scratch (reused inside a single ForAll body) ---------------------

        private float _lastHealth;
        private float _lastMaxHealth;

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Health values spanning negative (clamps to 0), zero, mid-range, exactly-full and overshoot
        /// (clamps to 1), so the clamp endpoints and the interior are all visited.
        /// </summary>
        private static float RandomHealth(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return -50f + (float)rng.NextDouble() * 50f;   // negative .. 0 (clamps low)
                case 1: return 0f;                                     // exactly empty
                case 2: return (float)rng.NextDouble() * 100f;         // interior
                case 3: return 100f;                                   // a common full value
                case 4: return 100f + (float)rng.NextDouble() * 100f;  // overshoot (clamps high)
                default: return (float)rng.NextDouble() * 1000f;       // wide interior
            }
        }

        /// <summary>
        /// Max-health values spanning the non-positive guard (negative / zero -> treated as 0.0001),
        /// tiny, and typical positive maxima.
        /// </summary>
        private static float RandomMaxHealth(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -10f + (float)rng.NextDouble() * 10f;   // negative .. 0 (guarded)
                case 1: return 0f;                                     // exactly zero (guarded)
                case 2: return (float)rng.NextDouble() * 1f;           // tiny positive
                case 3: return 100f;                                   // a common max
                default: return 1f + (float)rng.NextDouble() * 500f;   // typical positive
            }
        }

        /// <summary>
        /// Tries to produce a health whose clamped fill against <paramref name="maxHealth"/> differs
        /// from <paramref name="currentFill"/>. Reports via <paramref name="producedDifferent"/>
        /// whether a plausible distinct value was generated (the caller still verifies the resulting
        /// fill truly differs, so a false positive degrades to the equal-value branch rather than a
        /// vacuous pass).
        /// </summary>
        private static float DifferentFillHealth(System.Random rng, float maxHealth, float currentFill, out bool producedDifferent)
        {
            float resolvedMax = Mathf.Max(maxHealth, 0.0001f);
            // Aim for a target fill clearly separated from the current one, then invert the formula.
            float targetFill = currentFill < 0.5f
                ? Mathf.Clamp01(currentFill + 0.25f + (float)rng.NextDouble() * 0.25f)
                : Mathf.Clamp01(currentFill - 0.25f - (float)rng.NextDouble() * 0.25f);

            producedDifferent = !Mathf.Approximately(targetFill, currentFill);
            // health = fill * resolvedMax reproduces targetFill (for interior fills); endpoints clamp.
            return targetFill * resolvedMax;
        }
    }
}
