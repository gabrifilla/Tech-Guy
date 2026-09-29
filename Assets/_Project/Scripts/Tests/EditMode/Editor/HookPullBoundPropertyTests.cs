using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure hook-pull model in <see cref="HookPullModel"/> — task 9.2
    /// of enemy-swarm-core-archetypes.
    ///
    /// The Hooker's pull is otherwise scene/agent driven (<see cref="PlayerActor"/>.<c>BeginExternalPull</c>);
    /// the bound / control-return / displacement-tolerance invariants were extracted into
    /// <see cref="HookPullModel"/> so they can be property-checked without a live Unity scene. The
    /// project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (min 128)
    /// and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class HookPullBoundPropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 14: Hook pull is bounded and returns control.
        // For any requested maxDuration (including values above 1.5s, zero, and negatives), the effective
        // pull Duration is clamped to [0, 1.5s]; evaluating at or past that Duration with the source
        // still alive and unlocked ends the pull, returns control, and reports DurationElapsed; and
        // sweeping elapsed upward the pull ends no later than Duration (<= 1.5s). Ended and ReturnControl
        // always move together so control is always returned when the pull stops.
        // Validates: Requirements 12.3, 12.6
        [Test]
        public void PullDurationIsClampedAndEndsReturningControlAtBound()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float requested = RandomDuration(rng);
                var model = new HookPullModel(requested);

                // R12.3 / R12.6: Duration is clamped to [0, 1.5s] for every requested value.
                PropertyCheck.That(
                    model.Duration >= 0f && model.Duration <= HookPullModel.MaxPullDuration + 1e-6f,
                    $"requested={requested}: Duration {model.Duration} out of [0,{HookPullModel.MaxPullDuration}]");

                // The clamp matches min(max(requested,0), 1.5).
                float expectedDuration = Mathf.Clamp(requested, 0f, HookPullModel.MaxPullDuration);
                PropertyCheck.That(Mathf.Approximately(model.Duration, expectedDuration),
                    $"requested={requested}: Duration {model.Duration} != clamped {expectedDuration}");

                // R12.6: at or past the bounded Duration, with a live unlocked source, the pull ends,
                // returns control, and reports the duration-elapsed reason.
                float elapsedAtBound = model.Duration + (float)rng.NextDouble() * 0.5f;
                PullStep atBound = model.Evaluate(elapsedAtBound, sourceAlive: true, sourceControlLocked: false);
                PropertyCheck.That(atBound.Ended && atBound.ReturnControl && atBound.DurationElapsed,
                    $"requested={requested}: at elapsed {elapsedAtBound} (Duration {model.Duration}) expected an " +
                    $"ended pull that returns control via DurationElapsed, got Ended={atBound.Ended} " +
                    $"ReturnControl={atBound.ReturnControl} DurationElapsed={atBound.DurationElapsed}");

                // Ended <=> ReturnControl at the bound (control is returned exactly when the pull ends).
                PropertyCheck.That(atBound.Ended == atBound.ReturnControl,
                    "Ended and ReturnControl must move together");

                // Sweeping elapsed upward from 0, the pull must not end before its bounded Duration when
                // the source stays alive and unlocked — control stays suspended until the bound.
                int steps = rng.Next(8, 20);
                // Sample strictly within [0, Duration) so every sample precedes the bound.
                for (int s = 0; s < steps; s++)
                {
                    float elapsed = model.Duration * s / steps; // in [0, Duration), never reaching Duration
                    PullStep pull = model.Evaluate(elapsed, sourceAlive: true, sourceControlLocked: false);

                    // Whenever the pull ends this step, control is returned this step.
                    PropertyCheck.That(pull.Ended == pull.ReturnControl,
                        $"requested={requested}: at elapsed {elapsed}, Ended={pull.Ended} != ReturnControl={pull.ReturnControl}");

                    // Before reaching Duration the pull must not have ended.
                    if (elapsed < model.Duration - 1e-4f)
                    {
                        PropertyCheck.That(!pull.Ended,
                            $"requested={requested}: pull ended early at elapsed {elapsed} before Duration {model.Duration} " +
                            "with a live, unlocked source");
                    }
                }

                // Evaluated exactly at the bounded Duration, the pull ends and returns control, so the pull
                // always ends at or before Duration — which is itself clamped to <= 1.5s.
                PullStep atDuration = model.Evaluate(model.Duration, sourceAlive: true, sourceControlLocked: false);
                PropertyCheck.That(atDuration.Ended && atDuration.ReturnControl && atDuration.DurationElapsed,
                    $"requested={requested}: pull did not end at its bounded Duration {model.Duration}");
                PropertyCheck.That(model.Duration <= HookPullModel.MaxPullDuration + 1e-6f,
                    $"requested={requested}: bounded Duration {model.Duration} exceeded the 1.5s bound");
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 14: Hook pull is bounded and returns control.
        // Focused sub-property: the pull ends early and returns control the first step the source becomes
        // control-locked (R12.7) or is destroyed mid-pull (R12.6), even when elapsed < Duration. In both
        // cases the step is flagged Interrupted (not DurationElapsed) and control is returned.
        // Validates: Requirements 12.6, 12.7
        [Test]
        public void PullEndsEarlyAndReturnsControlOnLockOrSourceLoss()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Use a healthy, non-trivial Duration so "early" (elapsed < Duration) is meaningful.
                float requested = 0.2f + (float)rng.NextDouble() * 2f; // spans below and above the 1.5s bound
                var model = new HookPullModel(requested);

                // Pick an elapsed strictly before the bound so termination must come from the interrupt.
                float earlyElapsed = (float)rng.NextDouble() * Mathf.Max(0f, model.Duration - 1e-3f);

                // R12.7: source control-locked mid-pull ends the pull early and returns control.
                PullStep locked = model.Evaluate(earlyElapsed, sourceAlive: true, sourceControlLocked: true);
                PropertyCheck.That(locked.Ended && locked.ReturnControl && locked.Interrupted,
                    $"requested={requested}, elapsed={earlyElapsed}: a control-locked source must end the pull " +
                    $"early and return control (Interrupted), got Ended={locked.Ended} " +
                    $"ReturnControl={locked.ReturnControl} Interrupted={locked.Interrupted}");
                PropertyCheck.That(!locked.DurationElapsed,
                    "an early control-lock end must not be attributed to DurationElapsed");

                // R12.6: source destroyed mid-pull ends the pull early and returns control.
                PullStep destroyed = model.Evaluate(earlyElapsed, sourceAlive: false, sourceControlLocked: false);
                PropertyCheck.That(destroyed.Ended && destroyed.ReturnControl && destroyed.Interrupted,
                    $"requested={requested}, elapsed={earlyElapsed}: a destroyed source must end the pull early and " +
                    $"return control (Interrupted), got Ended={destroyed.Ended} " +
                    $"ReturnControl={destroyed.ReturnControl} Interrupted={destroyed.Interrupted}");

                // Both interrupts together also end the pull early and return control.
                PullStep both = model.Evaluate(earlyElapsed, sourceAlive: false, sourceControlLocked: true);
                PropertyCheck.That(both.Ended && both.ReturnControl && both.Interrupted,
                    "a destroyed AND control-locked source must end the pull early and return control");
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 14: Hook pull is bounded and returns control.
        // Focused sub-property (R12.5): for random start/source positions and any fraction in [0,1], the
        // interpolated pull position never overshoots — WithinDisplacementTolerance always holds — and a
        // target already inside the stop distance is never pushed outward.
        // Validates: Requirements 12.5
        [Test]
        public void PullNeverDisplacesTargetBeyondTolerance()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                Vector3 source = RandomPoint(rng);
                Vector3 start = RandomPoint(rng);

                // Sweep the whole fraction range plus a couple of out-of-range values to confirm clamping.
                int samples = rng.Next(6, 14);
                for (int s = 0; s < samples; s++)
                {
                    float fraction = RandomFraction(rng, s, samples);
                    Vector3 pos = HookPullModel.PositionAt(start, source, fraction);

                    // R12.5: the interpolated position stays within the displacement tolerance of its
                    // intended stop point — it never overshoots the source or ends farther out than start.
                    PropertyCheck.That(HookPullModel.WithinDisplacementTolerance(pos, start, source),
                        $"start={start} source={source} fraction={fraction}: position {pos} exceeded displacement tolerance");

                    float startDist = (start - source).magnitude;
                    float posDist = (pos - source).magnitude;

                    // The pull only ever moves the target inward: it never ends farther from the source
                    // than it started (within tolerance).
                    PropertyCheck.That(posDist <= startDist + HookPullModel.ArrivalTolerance,
                        $"start={start} source={source} fraction={fraction}: pull pushed target outward " +
                        $"({posDist} > start {startDist})");

                    // A target already inside the stop distance is held in place, not shoved outward.
                    if (startDist <= HookPullModel.StopDistance)
                    {
                        PropertyCheck.That(pos == start,
                            $"start={start} source={source}: target already inside stop distance was displaced to {pos}");
                    }
                }

                // A completed pull (fraction 1) from outside the stop ring settles within tolerance of the
                // stop distance and never overshoots past the source.
                float outerDist = HookPullModel.StopDistance + 1f + (float)rng.NextDouble() * 20f;
                Vector3 farStart = source + RandomDirection(rng) * outerDist;
                Vector3 arrived = HookPullModel.PositionAt(farStart, source, 1f);
                float arrivedDist = (arrived - source).magnitude;
                PropertyCheck.That(
                    arrivedDist >= HookPullModel.StopDistance - HookPullModel.ArrivalTolerance &&
                    arrivedDist <= HookPullModel.StopDistance + HookPullModel.ArrivalTolerance,
                    $"farStart={farStart} source={source}: completed pull settled at {arrivedDist}, outside " +
                    $"[{HookPullModel.StopDistance - HookPullModel.ArrivalTolerance}, {HookPullModel.StopDistance + HookPullModel.ArrivalTolerance}]");
                PropertyCheck.That(HookPullModel.WithinDisplacementTolerance(arrived, farStart, source),
                    $"farStart={farStart} source={source}: completed pull violated displacement tolerance at {arrived}");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Requested durations spanning negative, zero, within-bound, and above the 1.5s bound.</summary>
        private static float RandomDuration(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -1f + (float)rng.NextDouble() * 1f;                       // negative .. 0
                case 1: return 0f;                                                       // exactly zero
                case 2: return (float)rng.NextDouble() * HookPullModel.MaxPullDuration;  // 0 .. 1.5
                case 3: return HookPullModel.MaxPullDuration;                            // exactly the bound
                default: return HookPullModel.MaxPullDuration + (float)rng.NextDouble() * 5f; // above the bound
            }
        }

        /// <summary>A fraction sweeping [0,1] with occasional out-of-range values to confirm clamping.</summary>
        private static float RandomFraction(System.Random rng, int index, int samples)
        {
            switch (index % 4)
            {
                case 0: return -0.25f + (float)rng.NextDouble() * 0.25f; // slightly negative .. 0
                case 1: return (float)index / Mathf.Max(1, samples - 1); // even sweep across [0,1]
                case 2: return 1f + (float)rng.NextDouble() * 0.5f;      // slightly above 1
                default: return (float)rng.NextDouble();                 // uniform in [0,1)
            }
        }

        private static Vector3 RandomPoint(System.Random rng)
        {
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 40f,
                ((float)rng.NextDouble() - 0.5f) * 40f,
                ((float)rng.NextDouble() - 0.5f) * 40f);
        }

        private static Vector3 RandomDirection(System.Random rng)
        {
            var dir = new Vector3(
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f);
            return dir.sqrMagnitude < 1e-6f ? Vector3.forward : dir.normalized;
        }
    }
}
