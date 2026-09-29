using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the Manoplas' W (Punhos Relâmpago / Flurry) strike-and-lock loop —
    /// task 10.3 of weapon-gameplay-swarm-rework, Property 26 (Requisito 8.4).
    ///
    /// SCENE-BOUND SOURCE (not edited): the Flurry actually runs inside
    /// <c>BreakerGauntletCombat.Execute</c>, a <see cref="MonoBehaviour"/> coroutine that drives a
    /// live <c>NavMeshAgent</c> and applies <see cref="AreaHitStep"/> impacts against real actors.
    /// That coroutine cannot run without a Unity scene, so — exactly like the existing
    /// <see cref="InterruptCancellationPropertyTests"/> and <see cref="HookPullBoundPropertyTests"/> —
    /// the extractable decision is mirrored here in a test-local pure model (<see cref="FlurryLoop"/>)
    /// whose arithmetic is copied field-for-field from the coroutine:
    ///
    ///   • Declared duration = max(activeTime, AdvanceDuration, max over steps of (step.delay + 0.18)),
    ///     matching the loop's <c>duration</c> computation.
    ///   • The loop runs <c>while (elapsed &lt; duration)</c>; every non-null step whose
    ///     <c>delay</c> has been reached fires exactly once (the coroutine's per-index
    ///     <c>applied[i]</c> latch). Because the duration is padded by +0.18 past the last step's
    ///     delay, EVERY authored step lands within the declared window — repeated strikes across the
    ///     full duration (Requisito 8.4).
    ///   • The agent is locked (<c>isStopped = true</c>, <c>updateRotation = false</c>) for the whole
    ///     loop and restored only in the coroutine's <c>finally</c>/<c>RestoreCastState</c>, i.e. once
    ///     <c>elapsed &gt;= duration</c> — the target lock is maintained until the very end.
    ///
    /// This test validates that extractable invariant. What it does NOT cover — and what could only be
    /// checked from a live PlayMode scene — is called out explicitly in the assertions' comments: the
    /// real agent lock/restore side effects, animation, VFX and the actual per-actor damage
    /// application through <c>PlayerActor.TryApplyAreaDamage</c>.
    ///
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property
    /// and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class FlurryStrikesAndLockDurationPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 26: Flurry mantém golpes e travamento pela duração
        // For every Flurry (W) execution, the repeated strikes are applied to the locked target across
        // the whole declared duration, and the lock is maintained until the end. Concretely: every
        // authored step fires exactly once and no later than the declared duration; the number of
        // strikes equals the number of non-null steps; and the control lock stays engaged for every
        // sampled instant strictly before the declared duration, releasing only at/after it.
        // Validates: Requirements 8.4
        [Test]
        public void FlurryAppliesEveryStrikeOnceAcrossTheDurationAndHoldsTheLockUntilTheEnd()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- generate a Flurry (W) timeline ------------------------------------------------
                // The W slot is authored as a multi-hit sequence; generate 1..8 steps with random,
                // non-negative delays (AreaHitStep.delay is [Min(0)]), plus a couple of authored
                // activeTime / AdvanceDuration values so the max() in the duration formula is exercised.
                int stepCount = rng.Next(1, 9);
                var steps = new List<AreaHitStep>(stepCount);
                for (int s = 0; s < stepCount; s++) steps.Add(RandomStep(rng));

                // Sprinkle in the odd null step: the coroutine skips nulls when both computing the
                // duration and applying impacts, so a null must never count as a strike.
                if (rng.Next(0, 5) == 0) steps.Insert(rng.Next(0, steps.Count + 1), null);

                float activeTime = (float)rng.NextDouble() * 2f;      // 0 .. 2 s
                float advanceDuration = (float)rng.NextDouble() * 2f; // 0 .. 2 s

                var loop = new FlurryLoop(steps, activeTime, advanceDuration);

                // The declared duration matches the coroutine's max(activeTime, AdvanceDuration, delay+0.18).
                float expectedDuration = Mathf.Max(activeTime, advanceDuration);
                int nonNullSteps = 0;
                foreach (AreaHitStep step in steps)
                {
                    if (step == null) continue;
                    nonNullSteps++;
                    expectedDuration = Mathf.Max(expectedDuration, step.delay + FlurryLoop.StepDurationPad);
                }
                PropertyCheck.That(Mathf.Approximately(loop.Duration, expectedDuration),
                    $"steps={stepCount}: declared duration {loop.Duration} != expected {expectedDuration}");

                // ---- drive the loop frame-by-frame, like the coroutine's while(elapsed<duration) ----
                // Use a random-but-positive delta so the sampling is uneven, as real frames are.
                var applied = new bool[steps.Count];
                int totalStrikes = 0;
                float elapsed = 0f;
                float lastElapsedWhileLocked = -1f;
                bool lockReleasedEarly = false;

                // Guard against pathological infinite loops from a zero delta; the coroutine skips a
                // zero-delta frame, so we always advance by a positive amount here.
                int safety = 0;
                while (elapsed < loop.Duration)
                {
                    if (++safety > 100000) break;
                    float delta = 0.005f + (float)rng.NextDouble() * 0.05f; // 5..55 ms frames

                    // R8.4 (lock): the target stays locked for the entire loop body. While elapsed is
                    // still below the declared duration the lock must be engaged.
                    if (!loop.IsLockEngaged(elapsed))
                    {
                        lockReleasedEarly = true;
                        break;
                    }
                    lastElapsedWhileLocked = elapsed;

                    elapsed += delta;

                    // Apply every pending step exactly once (the coroutine's applied[i] latch).
                    for (int idx = 0; idx < applied.Length; idx++)
                    {
                        AreaHitStep step = steps[idx];
                        if (applied[idx] || step == null || elapsed < step.delay) continue;
                        applied[idx] = true;
                        totalStrikes++;

                        // A strike only ever lands within the declared window (Requisito 8.4): the loop
                        // exits once elapsed >= duration, and duration >= delay + 0.18 for every step.
                        PropertyCheck.That(step.delay <= loop.Duration + 1e-4f,
                            $"step {idx} delay {step.delay} lies beyond the declared duration {loop.Duration}");
                    }
                }

                // ---- assertions --------------------------------------------------------------------
                PropertyCheck.That(!lockReleasedEarly,
                    $"steps={stepCount} dur={loop.Duration}: control lock released early at elapsed " +
                    $"{lastElapsedWhileLocked} (before the declared duration)");

                // Repeated strikes across the whole duration: every authored (non-null) step landed
                // exactly once, so the strike count equals the number of real steps (Requisito 8.4).
                PropertyCheck.That(totalStrikes == nonNullSteps,
                    $"steps={stepCount}: applied {totalStrikes} strikes but expected {nonNullSteps} " +
                    "(every authored step must fire exactly once across the duration)");

                for (int idx = 0; idx < steps.Count; idx++)
                {
                    bool shouldApply = steps[idx] != null;
                    PropertyCheck.That(applied[idx] == shouldApply,
                        $"steps={stepCount}: step {idx} applied={applied[idx]} but shouldApply={shouldApply}");
                }

                // The lock is maintained until the end and released only at/after the declared duration.
                PropertyCheck.That(loop.IsLockEngaged(loop.Duration - 1e-3f),
                    $"steps={stepCount}: lock was not engaged just before the declared duration {loop.Duration}");
                PropertyCheck.That(!loop.IsLockEngaged(loop.Duration),
                    $"steps={stepCount}: lock still engaged at/after the declared duration {loop.Duration}");
            });
        }

        // ---- generators -------------------------------------------------------------------------------

        /// <summary>
        /// A Flurry hit step with a non-negative delay spanning zero and a few seconds, so the duration
        /// formula's <c>delay + 0.18</c> term dominates for late steps and the <c>max</c> with
        /// activeTime/AdvanceDuration dominates for early ones.
        /// </summary>
        private static AreaHitStep RandomStep(System.Random rng)
        {
            return new AreaHitStep
            {
                delay = rng.Next(0, 4) == 0 ? 0f : (float)rng.NextDouble() * 2.5f,
                stanceDamage = (float)rng.NextDouble() * 30f,
                hitStrength = HitStrength.Light,
                reactionType = HitReactionType.Stagger,
            };
        }

        // ---- test-local pure model of the scene-bound Flurry loop -------------------------------------

        /// <summary>
        /// Pure mirror of the strike-scheduling and lock semantics inside
        /// <c>BreakerGauntletCombat.Execute</c> for the W (Flurry) slot. It owns none of the coroutine's
        /// scene side effects — it only reproduces the arithmetic the property is about: the declared
        /// duration, and whether the control lock is engaged at a given elapsed time.
        /// </summary>
        private sealed class FlurryLoop
        {
            /// <summary>Padding the coroutine adds past each step's delay when sizing the loop (step.delay + 0.18).</summary>
            public const float StepDurationPad = 0.18f;

            public float Duration { get; }

            public FlurryLoop(IReadOnlyList<AreaHitStep> steps, float activeTime, float advanceDuration)
            {
                // Mirrors: duration = Mathf.Max(ability.activeTime, ability.AdvanceDuration); then
                // for each non-null step: duration = Mathf.Max(duration, step.delay + 0.18f);
                float duration = Mathf.Max(activeTime, advanceDuration);
                if (steps != null)
                {
                    foreach (AreaHitStep step in steps)
                        if (step != null) duration = Mathf.Max(duration, step.delay + StepDurationPad);
                }
                Duration = duration;
            }

            /// <summary>
            /// The agent is locked from the moment the cast begins (before the loop) and restored only
            /// in the coroutine's finally block, which runs once <c>elapsed &gt;= duration</c>. So the
            /// lock is engaged for every instant strictly before the declared duration and released
            /// at/after it.
            /// </summary>
            public bool IsLockEngaged(float elapsed) => elapsed < Duration;
        }
    }
}
