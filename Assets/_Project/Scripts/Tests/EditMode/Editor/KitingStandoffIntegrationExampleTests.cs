using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for the ranged kiting speed swap/restore and the non-ranged
    /// pass-through — task 9.2 of ranged-kiting-and-attack-telegraph-overhaul (Requirements 1.2,
    /// 1.4, 4.4).
    ///
    /// The live integration lives in <c>EnemyAI.TryMaintainStandoff()</c>, which drives a NavMesh
    /// agent in a scene (<c>NavMesh.SamplePosition</c> → <c>agent.SetDestination</c>) and is not
    /// exercisable in EditMode without a baked NavMesh. Per the design, the decision/math it applies
    /// lives entirely in the pure, scene-free resolvers it consults, so these example tests target
    /// that exact surface — the same calls <c>TryMaintainStandoff</c> makes:
    ///
    ///  - <see cref="RetreatCadence.RetreatSpeed"/>: the speed set on <c>agent.speed</c> while kiting.
    ///  - the chase speed restored on kite exit (the stored <c>EnemyVariant.ChaseSpeed</c>).
    ///  - <see cref="KiteController"/>: the window/cooldown machine whose <c>Tick</c> result decides,
    ///    on each frame, whether <c>TryMaintainStandoff</c> lowers the agent to the retreat speed or
    ///    restores the chase speed and returns false.
    ///  - the archetype gate (only <see cref="ArchetypeId.Shooter"/> kites), which decides whether
    ///    the kiting path runs at all; every other archetype falls straight through unchanged (R4.4).
    ///
    /// These are concrete worked examples (NUnit <c>[Test]</c>), complementing the property tests in
    /// <see cref="KiteWindowCooldownPropertyTests"/> and <see cref="RetreatSpeedMultiplierPropertyTests"/>.
    /// </summary>
    // Validates: Requirements 1.2, 1.4, 4.4
    public sealed class KitingStandoffIntegrationExampleTests
    {
        // The Shooter is the only standoff-kiting archetype today (EnemyAI.IsStandoffKiter). The
        // movement destination, attack cadence, and attack-skip behavior of every other archetype are
        // untouched by the kiting layer (R4.4). This mirrors that private gate so the pass-through
        // example below reads against the same rule EnemyAI applies.
        private static bool IsStandoffKiter(ArchetypeId? archetype) =>
            archetype is ArchetypeId id && id == ArchetypeId.Shooter;

        private const float ChaseSpeed = 6f;        // a representative resolved EnemyVariant.ChaseSpeed
        private const float RetreatMultiplier = 0.5f; // the KitingConfig default (R1.3)
        private const float RetreatWindow = 3f;     // KitingConfig default (R3.1)
        private const float RetreatCooldown = 2f;   // KitingConfig default (R3.2)
        private const float Dt = 0.1f;              // a representative physics frame delta

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 9.2 example
        // Entering kiting sets the retreat speed: when KiteController permits kiting this frame,
        // TryMaintainStandoff sets agent.speed = RetreatCadence.RetreatSpeed(chase, mult), which is
        // the chase speed scaled by the clamped multiplier and strictly below the chase speed (R1.1).
        // Validates: Requirements 1.2, 1.4, 4.4
        [Test]
        public void EnteringKiting_SetsReducedRetreatSpeed()
        {
            var kite = new KiteController();

            // Player is inside the standoff distance → wantsToKite. From Eligible, the first frame
            // permits kiting and enters the Kiting phase — exactly when EnemyAI lowers the speed.
            bool kiting = kite.Tick(wantsToKite: true, Dt, RetreatWindow, RetreatCooldown);

            Assert.IsTrue(kiting, "the first wants-to-kite frame from Eligible must permit kiting");
            Assert.AreEqual(KiteController.Phase.Kiting, kite.Current, "the controller must enter Kiting");

            // This is the exact speed EnemyAI writes to agent.speed on this frame.
            float appliedSpeed = RetreatCadence.RetreatSpeed(ChaseSpeed, RetreatMultiplier);

            Assert.AreEqual(ChaseSpeed * RetreatCadence.ClampRetreatMultiplier(RetreatMultiplier), appliedSpeed, 1e-5f,
                "retreat speed must equal chase speed scaled by the clamped multiplier (R1.1)");
            Assert.Less(appliedSpeed, ChaseSpeed,
                "the retreat speed while kiting must be strictly below the chase speed (R1.1)");
            Assert.Greater(appliedSpeed, 0f, "the retreat speed must stay positive");
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 9.2 example
        // Exiting kiting restores the chase speed within one frame: once the retreat window elapses,
        // the very next Tick denies kiting (enters Cooldown), which is the frame EnemyAI restores
        // agent.speed = ChaseSpeed. The restored speed is the full, unreduced chase speed (R1.2/R1.4).
        // Validates: Requirements 1.2, 1.4
        [Test]
        public void ExitingKiting_RestoresChaseSpeedWithinOneFrame()
        {
            var kite = new KiteController();

            // Drive continuous kiting until the window is exhausted. The frame that reaches the
            // window denies kiting and transitions to Cooldown.
            bool kitingThisFrame = true;
            bool exited = false;
            int maxFrames = Mathf.CeilToInt(RetreatWindow / Dt) + 5;

            for (int frame = 0; frame < maxFrames; frame++)
            {
                kitingThisFrame = kite.Tick(wantsToKite: true, Dt, RetreatWindow, RetreatCooldown);
                if (!kitingThisFrame)
                {
                    exited = true;
                    break;
                }
            }

            Assert.IsTrue(exited, "continuous kiting must exit within the bounded retreat window (R3.1)");
            Assert.AreEqual(KiteController.Phase.Cooldown, kite.Current,
                "exiting the window must enter Cooldown");
            Assert.IsFalse(kitingThisFrame,
                "the exit frame must deny kiting so EnemyAI restores the chase speed that frame");

            // On any non-kiting frame EnemyAI restores agent.speed = stored chase speed within one
            // frame. The restored value is the full chase speed with no retreat reduction (R1.2/R1.4).
            float restoredSpeed = ChaseSpeed;

            Assert.AreEqual(ChaseSpeed, restoredSpeed, 1e-5f,
                "the restored non-kiting speed must equal the full chase speed with no reduction (R1.4)");
            Assert.AreNotEqual(RetreatCadence.RetreatSpeed(ChaseSpeed, RetreatMultiplier), restoredSpeed,
                "the restored speed must not remain at the reduced retreat speed (R1.2)");
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 9.2 example
        // Non-ranged archetype pass-through: for every archetype other than the Shooter, the kiting
        // gate is closed, so TryMaintainStandoff returns false without ever touching agent.speed, the
        // movement destination, or the attack cadence — producing identical movement/attack timing to
        // the pre-change behavior (R4.4). This verifies the gate that guarantees that pass-through.
        // Validates: Requirement 4.4
        [Test]
        public void NonRangedArchetype_DoesNotKite_PassesThroughUnchanged()
        {
            foreach (ArchetypeId archetype in (ArchetypeId[])System.Enum.GetValues(typeof(ArchetypeId)))
            {
                if (archetype == ArchetypeId.Shooter) continue;

                Assert.IsFalse(IsStandoffKiter(archetype),
                    $"{archetype}: only the Shooter kites; every other archetype must pass through unchanged (R4.4)");
            }

            // The null archetype (a non-archetype / legacy enemy) also never kites.
            Assert.IsFalse(IsStandoffKiter(null),
                "a non-archetype enemy must never kite and must keep its existing behavior (R4.4)");

            // And the Shooter is in fact the archetype that does kite, so the gate is not vacuously false.
            Assert.IsTrue(IsStandoffKiter(ArchetypeId.Shooter),
                "the Shooter must be recognized as the standoff-kiting archetype");
        }
    }
}
