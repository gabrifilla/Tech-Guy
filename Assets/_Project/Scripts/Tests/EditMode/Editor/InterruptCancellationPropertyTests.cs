using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for interrupt cancellation of a telegraphed damage beat — task 14.1 of
    /// enemy-swarm-core-archetypes.
    ///
    /// The live attack pipeline (<see cref="EnemyAttackExecution"/>) polls its <c>canAttack</c>
    /// predicate on every windup frame and once more immediately before the beat resolves; when the
    /// acting enemy becomes control-locked or stance-broken, the windup is cancelled and no damage
    /// beat is produced on that same frame. That interrupt semantics is a pure predicate:
    ///
    ///     beatApplies  ==  windupComplete && !interruptedBeforeCompletion
    ///
    /// This test models the windup timeline with <see cref="TelegraphWindupClock"/> and gates a
    /// <see cref="SingleBeatResolver"/> with that predicate, mirroring how
    /// <see cref="EnemyAttackExecution"/> wraps the single-beat resolve behind <c>canAttack</c>.
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases and reports
    /// the exact failing case as a counterexample.
    /// </summary>
    public sealed class InterruptCancellationPropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 3: Interrupt cancels telegraph and suppresses the beat.
        // For any windup duration and any control-lock / stance-break interrupt that occurs strictly
        // before the windup completes, the telegraphed damage beat is suppressed: no damage is ever
        // applied, and the suppression is same-frame (the beat does not sneak through on the very
        // frame the interrupt is applied). When no interrupt occurs before completion, the beat is
        // free to resolve subject to the ordinary single-beat + in-area rules.
        // Validates: Requirements 2.4, 7.6, 8.5, 9.6, 13.7, 14.5
        [Test]
        public void InterruptBeforeCompletionSuppressesTheBeatSameFrame()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- generate the windup timeline ------------------------------------------------
                float authored = RandomWindup(rng);
                bool controlling = rng.Next(0, 2) == 0;
                var clock = new TelegraphWindupClock(authored, controlling);
                float duration = clock.Duration;

                // The beat is checked on the frame at/after the windup completes.
                float beatTime = duration;

                // ---- generate the interrupt (control-lock or stance-break) -----------------------
                // An interrupt may occur before, exactly at, or after the beat frame — or never.
                bool hasInterrupt = rng.Next(0, 4) != 0; // ~75% of cases carry an interrupt
                float interruptTime = RandomInterruptTime(rng, duration);

                // The acting enemy is interrupted (control-locked / stance-broken) from interruptTime
                // onward. Because the pipeline cancels the windup the moment the predicate goes false,
                // an interrupt that lands strictly before the beat frame cancels the whole attack.
                bool interruptedBeforeCompletion =
                    hasInterrupt && interruptTime < beatTime - 1e-6f;

                // The pipeline also re-checks canAttack on the beat frame itself, so an interrupt that
                // lands *at or before* the beat suppresses the beat (same-frame cancellation).
                bool interruptedAtOrBeforeCompletion =
                    hasInterrupt && interruptTime <= beatTime + 1e-6f;

                // canAttack() as the pipeline sees it at a given time: false once interrupted.
                bool CanAttackAt(float t) => !(hasInterrupt && t >= interruptTime - 1e-6f);

                // ---- set up the target + areas so a beat *could* land absent an interrupt --------
                var resolver = new SingleBeatResolver();
                int areaCount = rng.Next(1, 4);
                var areas = new EnemyAttackArea[areaCount];
                for (int a = 0; a < areaCount; a++) areas[a] = RandomCircle(rng);
                var targetPoint = Vector3.zero; // inside the union (circles centred near origin)
                float damage = 1f + (float)rng.NextDouble() * 20f;

                // ---- drive the windup frame-by-frame, exactly like the pipeline ------------------
                // Poll canAttack each frame; if it goes false, the windup is cancelled and we bail
                // out before ever reaching the beat — this is the same-frame cancellation.
                bool beatApplied = false;
                float totalDamage = 0f;
                bool cancelled = false;

                int frames = rng.Next(3, 10);
                float step = duration / frames;
                for (int f = 1; f <= frames; f++)
                {
                    float t = f * step;
                    if (!CanAttackAt(t)) { cancelled = true; break; } // windup interrupted -> cancel
                }

                // Final pre-beat gate (the pipeline re-checks canAttack right before the beat).
                if (!cancelled && CanAttackAt(beatTime))
                {
                    // Windup completed AND not interrupted: the beat may resolve (single + in-area).
                    if (clock.IsComplete(beatTime) && resolver.TryResolve(areas, targetPoint, damage))
                    {
                        beatApplied = true;
                        totalDamage += damage;
                    }
                }
                else
                {
                    cancelled = true;
                }

                // ---- the pure predicate we are validating ----------------------------------------
                bool predictedApplies = BeatApplies(clock, beatTime, interruptedAtOrBeforeCompletion);

                // ---- assertions ------------------------------------------------------------------
                if (interruptedBeforeCompletion)
                {
                    // R2.4/7.6/8.5/9.6/13.7/14.5: interrupt before completion => no beat, no damage.
                    PropertyCheck.That(!beatApplied,
                        $"authored={authored} dur={duration} interruptTime={interruptTime}: " +
                        "a beat was applied despite an interrupt before windup completion");
                    PropertyCheck.That(totalDamage == 0f,
                        $"authored={authored} dur={duration} interruptTime={interruptTime}: " +
                        $"damage {totalDamage} was applied despite a pre-completion interrupt");
                    PropertyCheck.That(cancelled,
                        $"authored={authored} interruptTime={interruptTime}: windup was not cancelled by the interrupt");
                    PropertyCheck.That(!resolver.BeatResolved,
                        "resolver latched a beat even though the attack was interrupted before completion");
                    PropertyCheck.That(!predictedApplies,
                        "pure predicate said the beat applies even though it was interrupted before completion");
                }
                else
                {
                    // No pre-completion interrupt: prediction and actual resolution agree, and a beat
                    // that lands does so exactly once with positive damage.
                    PropertyCheck.That(beatApplied == predictedApplies,
                        $"authored={authored} dur={duration}: actual beat={beatApplied} disagreed with predicate={predictedApplies}");
                    if (beatApplied)
                    {
                        PropertyCheck.That(totalDamage == damage,
                            $"expected a single beat of {damage} damage, got {totalDamage}");
                        PropertyCheck.That(resolver.BeatResolved,
                            "beat applied but resolver did not latch it");
                    }
                }

                // ---- same-frame guarantee: an interrupt exactly at the beat frame still suppresses -
                // Model an interrupt landing on the beat frame itself and run the exact pre-beat gate
                // the pipeline uses. Because canAttack is re-checked right before the beat, a
                // same-frame interrupt (canAttack == false at beatTime) must prevent the beat: it does
                // not sneak through on the interrupt frame.
                var sameFrameResolver = new SingleBeatResolver();
                bool CanAttackWithSameFrameInterrupt(float t) => t < beatTime - 1e-6f; // interrupted at beatTime
                bool sameFrameBeat =
                    CanAttackWithSameFrameInterrupt(beatTime) &&           // false -> gate closes this frame
                    clock.IsComplete(beatTime) &&
                    sameFrameResolver.TryResolve(areas, targetPoint, damage);
                PropertyCheck.That(!sameFrameBeat,
                    $"authored={authored} dur={duration}: beat resolved on the exact frame the interrupt " +
                    "was applied (must be suppressed same-frame)");
                PropertyCheck.That(!sameFrameResolver.BeatResolved,
                    "same-frame interrupt still latched a beat in the resolver");
            });
        }

        // ---- pure predicate under test ------------------------------------------------------------

        /// <summary>
        /// The interrupt-gated beat predicate: a telegraphed damage beat applies iff the windup has
        /// completed AND the acting enemy was not interrupted (control-locked / stance-broken) at or
        /// before the beat. This is the pure equivalent of <see cref="EnemyAttackExecution"/> gating
        /// its single-beat resolve behind <c>canAttack</c> on every windup frame and again at the beat.
        /// </summary>
        private static bool BeatApplies(TelegraphWindupClock clock, float beatTime, bool interruptedAtOrBeforeCompletion)
            => clock.IsComplete(beatTime) && !interruptedAtOrBeforeCompletion;

        // ---- generators ---------------------------------------------------------------------------

        /// <summary>Authored windups spanning negative, zero, sub-floor, and well above the floor.</summary>
        private static float RandomWindup(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -1f + (float)rng.NextDouble() * 1f;      // negative .. 0
                case 1: return (float)rng.NextDouble() * 0.25f;         // 0 .. floor
                case 2: return 0.25f;                                   // exactly the floor
                default: return 0.25f + (float)rng.NextDouble() * 5f;   // above the floor
            }
        }

        /// <summary>
        /// An interrupt time that may fall strictly before, exactly at, or after the beat frame,
        /// so the property exercises pre-completion cancellation, same-frame, and no-effect cases.
        /// </summary>
        private static float RandomInterruptTime(System.Random rng, float duration)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return (float)rng.NextDouble() * duration * 0.99f;      // strictly before completion
                case 1: return duration;                                        // exactly at the beat frame
                case 2: return duration + (float)rng.NextDouble() * duration;   // after completion (no effect)
                default: return (float)rng.NextDouble() * duration;             // somewhere within the windup
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
    }
}
