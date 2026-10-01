using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for the two pure gates that EnemyAI 8.1 wires into its perception/facing
    /// flow — task 8.2 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// These pin the two "behave exactly as the old instant code did" boundary cases that the
    /// property tests sweep but that are worth asserting as concrete, named examples:
    ///
    ///  - <see cref="ReactionGate"/> with reactionDelay == 0 reacts on the very first in-sight Tick,
    ///    so chase/attack dispatch is not deferred at all when the designer disables the delay (R11.6).
    ///  - <see cref="EnemyFacing.IsFacing"/> returns true once the enemy forward is within
    ///    <see cref="EnemyFacing.FacingEpsilonDegrees"/> (0.5°) of the target direction, which is the
    ///    exact gate that permits the facing-dependent attack/standoff actions the instant
    ///    <c>Quaternion.LookRotation</c> snap used to permit immediately (R10.4).
    ///
    /// Both logic classes are plain, scene-free C#, so these drive the real types directly — no live
    /// Unity scene or <c>EnemyAI</c> is involved.
    /// </summary>
    public sealed class EnemyReactionFacingExampleTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 8.2 example — a gate configured
        // with reactionDelay == 0 opens on the first in-sight frame, exactly as the old
        // detect-then-act-same-frame behavior did (no deferral).
        // Validates: Requirements 10.4, 11.6
        [Test]
        public void ReactionDelayZero_ReactsOnFirstInSightFrame()
        {
            var gate = new ReactionGate();

            // Precondition: a fresh gate has not reacted yet.
            Assert.IsFalse(gate.Reacted, "a fresh gate must start not-yet-reacted");

            bool reacted = gate.Tick(inSight: true, dt: 0.016f, reactionDelay: 0f, sightLossReset: 1f);

            Assert.IsTrue(reacted, "reactionDelay==0 must react on the first in-sight Tick (R11.6)");
            Assert.IsTrue(gate.Reacted, "the gate must report Reacted after reacting on the first frame (R11.6)");
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 8.2 example — with reactionDelay
        // == 0 the gate stays open across subsequent continuously-in-sight frames (same-frame reaction
        // is sticky, not a one-frame pulse), so dispatch remains permitted.
        // Validates: Requirements 10.4, 11.6
        [Test]
        public void ReactionDelayZero_StaysReactedWhileContinuouslyInSight()
        {
            var gate = new ReactionGate();

            Assert.IsTrue(gate.Tick(inSight: true, dt: 0.016f, reactionDelay: 0f, sightLossReset: 1f),
                "reactionDelay==0 must react on the first in-sight Tick (R11.6)");

            for (int frame = 0; frame < 5; frame++)
            {
                Assert.IsTrue(gate.Tick(inSight: true, dt: 0.016f, reactionDelay: 0f, sightLossReset: 1f),
                    $"gate must stay reacted while continuously in sight (frame {frame}) with reactionDelay==0");
            }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 8.2 example — forward aligned with
        // the target direction is trivially facing, so facing-gated attack/standoff proceeds exactly as
        // the instant snap allowed.
        // Validates: Requirement 10.4
        [Test]
        public void IsFacing_WhenForwardMatchesTarget_PermitsFacingGatedActions()
        {
            Vector3 target = Vector3.forward;

            Assert.IsTrue(EnemyFacing.IsFacing(Vector3.forward, target),
                "forward exactly aligned with the target must be treated as facing (R10.4)");
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 8.2 example — a remaining angle at
        // or just inside the 0.5° epsilon permits facing-gated actions (the exact threshold the instant
        // snap effectively used), while an angle clearly outside the epsilon does not.
        // Validates: Requirement 10.4
        [Test]
        public void IsFacing_WithinHalfDegreeEpsilon_PermitsActionButBeyondItBlocks()
        {
            Vector3 target = Vector3.forward;

            // Just inside the 0.5° epsilon: facing-gated actions must be permitted. A small margin
            // below the boundary avoids Vector3.Angle's floating-point rounding at the exact threshold.
            Vector3 justInside = Quaternion.AngleAxis(EnemyFacing.FacingEpsilonDegrees - 0.05f, Vector3.up) * target;
            Assert.IsTrue(EnemyFacing.IsFacing(justInside, target),
                $"a remaining angle just inside {EnemyFacing.FacingEpsilonDegrees}° must permit facing-gated actions (R10.4)");

            // Clearly beyond the epsilon: not yet facing, so facing-gated actions are withheld.
            Vector3 beyond = Quaternion.AngleAxis(EnemyFacing.FacingEpsilonDegrees + 2f, Vector3.up) * target;
            Assert.IsFalse(EnemyFacing.IsFacing(beyond, target),
                $"a remaining angle beyond {EnemyFacing.FacingEpsilonDegrees}° must withhold facing-gated actions (R10.4)");
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 8.2 example — stepping toward the
        // target with the pure resolver converges to within the 0.5° epsilon, at which point IsFacing
        // flips true and the facing-gated action proceeds exactly as the instant snap used to. This ties
        // the StepTowards turn to the IsFacing gate the integration relies on.
        // Validates: Requirement 10.4
        [Test]
        public void StepTowards_ConvergesUntilIsFacingPermitsAction()
        {
            Vector3 target = Vector3.forward;
            // Start facing away by 170° so convergence takes several steps.
            Vector3 forward = Quaternion.AngleAxis(170f, Vector3.up) * target;

            Assert.IsFalse(EnemyFacing.IsFacing(forward, target),
                "precondition: starting 170° off must not be facing");

            const float angularSpeed = 540f;
            const float dt = 0.016f;
            bool becameFacing = false;

            // 170° at 540°/s is ~0.31s; 60 frames (~0.96s) is comfortably enough to converge.
            for (int frame = 0; frame < 60 && !becameFacing; frame++)
            {
                forward = EnemyFacing.StepTowards(forward, target, angularSpeed, dt);
                if (EnemyFacing.IsFacing(forward, target))
                    becameFacing = true;
            }

            Assert.IsTrue(becameFacing,
                "stepping toward the target must converge to within the 0.5° facing epsilon so the facing-gated action proceeds (R10.4)");
        }
    }
}
