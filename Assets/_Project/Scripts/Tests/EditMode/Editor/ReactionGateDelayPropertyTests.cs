using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure, scene-free <see cref="ReactionGate"/> — task 1.4 of
    /// ranged-kiting-and-attack-telegraph-overhaul (Property 2).
    ///
    /// The gate models the short delay between an enemy first seeing the player and the enemy
    /// beginning to chase/attack, plus the sight-loss reset that re-arms that delay. Because the
    /// logic is a plain C# class driven by explicit (inSight, dt, reactionDelay, sightLossReset)
    /// inputs, this test drives the real <see cref="ReactionGate"/> directly — it does NOT touch a
    /// live Unity scene or <c>EnemyAI</c>.
    ///
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per
    /// property (sampling reaction delays, sight-loss resets, and frame deltas across and around
    /// each threshold) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class ReactionGateDelayPropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 2
        // For any sequence of in-sight/out-of-sight frames and dt > 0 with reactionDelay in [0,2]
        // and sightLossReset in [0,10], ReactionGate reports Reacted == false until the player has
        // been continuously in sight for reactionDelay, then true while continuously in sight;
        // after the player is continuously out of sight for sightLossReset, the next detection
        // again blocks for reactionDelay. With reactionDelay == 0, Reacted is true on the first
        // in-sight frame.
        // Validates: Requirements 11.1, 11.3, 11.4, 11.5, 11.6, 16.1, 16.2
        [Test]
        public void ReactionGateBlocksUntilDelayThenOpensAndReArmsAfterSightLoss()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float reactionDelay = NextFloat(rng, ReactionGate.MinReactionDelay, ReactionGate.MaxReactionDelay);
                float sightLossReset = NextFloat(rng, ReactionGate.MinSightLossReset, ReactionGate.MaxSightLossReset);
                // A positive, bounded frame delta so the thresholds are approached in several steps.
                float dt = NextFloat(rng, 0.01f, 0.2f);

                string state = $"[reactionDelay={reactionDelay} sightLossReset={sightLossReset} dt={dt}]";

                var gate = new ReactionGate();

                // ---- Initial condition: armed, not yet reacted (R11.1) -----------------------
                PropertyCheck.That(!gate.Reacted,
                    $"gate must start not-yet-reacted for {state}");

                // ---- reactionDelay == 0 reacts on the first in-sight frame (R11.6) -----------
                {
                    var zeroGate = new ReactionGate();
                    bool reactedFirstFrame = zeroGate.Tick(true, dt, 0f, sightLossReset);
                    PropertyCheck.That(reactedFirstFrame && zeroGate.Reacted,
                        $"reactionDelay==0 must react on the first in-sight frame for {state}");
                }

                // ---- Blocks until the delay elapses while continuously in sight (R11.1/R11.3) --
                // Feed in-sight frames; before the accrued in-sight time reaches reactionDelay the
                // gate must report false, and it must open exactly once that threshold is reached.
                float accrued = 0f;
                bool everOpenedEarly = false;
                bool openedAtOrAfterDelay = false;
                // Enough steps to comfortably exceed the max delay (2s) at the min dt (0.01s).
                int inSightSteps = Mathf.CeilToInt(ReactionGate.MaxReactionDelay / 0.01f) + 2;
                for (int step = 0; step < inSightSteps; step++)
                {
                    accrued += dt;
                    bool reacted = gate.Tick(true, dt, reactionDelay, sightLossReset);

                    if (accrued < reactionDelay - 1e-4f)
                    {
                        // Still inside the delay window: the gate must remain closed.
                        if (reacted) everOpenedEarly = true;
                    }
                    else if (accrued >= reactionDelay)
                    {
                        // The accrued in-sight time has reached the delay: the gate must be open.
                        if (reacted) openedAtOrAfterDelay = true;
                        PropertyCheck.That(reacted,
                            $"gate must be open once in-sight time ({accrued}) reaches the delay for {state}");
                    }
                }

                PropertyCheck.That(!everOpenedEarly,
                    $"gate must stay closed until the reaction delay elapses for {state}");
                PropertyCheck.That(openedAtOrAfterDelay,
                    $"gate must open once the reaction delay elapses for {state}");

                // ---- Stays open while continuously in sight (R11.4) --------------------------
                for (int step = 0; step < 5; step++)
                {
                    bool reacted = gate.Tick(true, dt, reactionDelay, sightLossReset);
                    PropertyCheck.That(reacted && gate.Reacted,
                        $"gate must stay open while continuously in sight for {state}");
                }

                // ---- Sub-threshold sight loss does NOT re-arm (R11.5) ------------------------
                // Lose sight for strictly less than sightLossReset: the gate must remain open.
                if (sightLossReset > 2e-2f)
                {
                    float outAccrued = 0f;
                    // Stop just short of the reset so we never cross it.
                    while (outAccrued + dt < sightLossReset - 1e-4f)
                    {
                        outAccrued += dt;
                        bool reacted = gate.Tick(false, dt, reactionDelay, sightLossReset);
                        PropertyCheck.That(reacted && gate.Reacted,
                            $"gate must stay open while out-of-sight time ({outAccrued}) is below the reset for {state}");
                    }
                }

                // ---- Continuous out-of-sight >= sightLossReset re-arms (R11.5) ---------------
                // Feed enough out-of-sight frames to exceed the max reset (10s).
                int outSightSteps = Mathf.CeilToInt(ReactionGate.MaxSightLossReset / 0.01f) + 2;
                for (int step = 0; step < outSightSteps; step++)
                {
                    gate.Tick(false, dt, reactionDelay, sightLossReset);
                }
                PropertyCheck.That(!gate.Reacted,
                    $"gate must re-arm after continuous sight loss >= reset for {state}");

                // ---- After re-arming, the next detection incurs the delay again (R11.5) ------
                // A single in-sight frame must not immediately re-open unless the delay is ~0.
                bool reopenedImmediately = gate.Tick(true, dt, reactionDelay, sightLossReset);
                if (dt < reactionDelay - 1e-4f)
                {
                    PropertyCheck.That(!reopenedImmediately,
                        $"re-armed gate must block for the delay again on next detection for {state}");
                }
            });
        }

        /// <summary>
        /// Determinism: identical (inSight, dt, reactionDelay, sightLossReset) sequences produce an
        /// identical Reacted trace across two independent gates (R16.1/R16.2).
        /// </summary>
        [Test]
        public void ReactionGateIsDeterministicForIdenticalInputSequences()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float reactionDelay = NextFloat(rng, ReactionGate.MinReactionDelay, ReactionGate.MaxReactionDelay);
                float sightLossReset = NextFloat(rng, ReactionGate.MinSightLossReset, ReactionGate.MaxSightLossReset);

                var a = new ReactionGate();
                var b = new ReactionGate();

                int frames = 40 + rng.Next(0, 60);
                for (int f = 0; f < frames; f++)
                {
                    bool inSight = rng.Next(0, 2) == 0;
                    float dt = NextFloat(rng, 0.01f, 0.25f);

                    bool ra = a.Tick(inSight, dt, reactionDelay, sightLossReset);
                    bool rb = b.Tick(inSight, dt, reactionDelay, sightLossReset);

                    PropertyCheck.That(ra == rb && a.Reacted == b.Reacted,
                        $"identical input sequences must produce identical Reacted traces at frame {f} " +
                        $"[reactionDelay={reactionDelay} sightLossReset={sightLossReset} inSight={inSight} dt={dt}]");
                }
            });
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
