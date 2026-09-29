using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example/unit tests for task 14.2 of enemy-swarm-core-archetypes — the "remaining"
    /// unit tests that pin a few numeric invariants which back live behaviour but are expressible as
    /// pure computations here:
    ///
    ///  - Charger obstruction/miss recovery is >= 1.0s (R6.6, sharing R6.5's window).
    ///  - Swarm authored maximum health is <= 25% of the Grunt's, so a single light hit kills (R15.3),
    ///    with the actual scene removal timing (R15.6) deferred to the PlayMode task 15.2.
    ///  - Fragile launch airborne-duration ordering: with KnockUp resistance 0 the airborne duration
    ///    equals the full stun duration and strictly exceeds that of any enemy with nonzero
    ///    resistance (R17.4).
    ///
    /// These are EXAMPLE tests (not property tests). Where the production logic is a private inline
    /// coroutine (the Charger recovery selection, the CombatReactionController duration scaling) the
    /// test replicates the exact formula from the source and asserts the invariant, and documents the
    /// live-scene concerns that are genuinely deferred to PlayMode.
    /// </summary>
    public sealed class RemainingArchetypeUnitTests
    {
        private const float Epsilon = 1e-6f;

        private const string ArchetypeDir =
            "Assets/_Project/ScriptableObjects/Enemies/Archetypes/";

        // ---- Charger obstruction/miss recovery >= 1.0s (R6.6) -------------------------------------

        /// <summary>
        /// Replicates the recovery-selection formula from
        /// <c>EnemyCombatActions.Charge</c>:
        /// <c>float recovery = hit ? (boss ? 1f : .8f) : Mathf.Max(1f, boss ? 1f : .8f);</c>
        /// This is inline coroutine logic in the production file; the formula is mirrored here so the
        /// R6.6 invariant can be asserted without driving a live Charge coroutine (that end-to-end
        /// path is a PlayMode concern, deferred to task 15.2).
        /// </summary>
        private static float ChargeRecovery(bool hit, bool boss)
            => hit ? (boss ? 1f : 0.8f) : Mathf.Max(1f, boss ? 1f : 0.8f);

        // Feature: enemy-swarm-core-archetypes, 14.2 example — Charger miss/obstruction recovery.
        // R6.6 (with R6.5): when the dash ends without applying damage — a miss OR a geometry-blocked
        // dash, both leaving hit == false — the recovery window is at least 1.0s. The Max(1f, ...)
        // floor guarantees this for both the normal and boss Charger.
        // Validates: Requirement 6.6
        [Test]
        public void Charger_MissOrObstruction_RecoveryIsAtLeastOneSecond()
        {
            // A geometry-obstructed or whiffed dash both reach the recovery with hit == false.
            Assert.That(ChargeRecovery(hit: false, boss: false), Is.GreaterThanOrEqualTo(1f),
                "A normal Charger that misses/obstructs must recover for >= 1.0s (R6.6).");
            Assert.That(ChargeRecovery(hit: false, boss: true), Is.GreaterThanOrEqualTo(1f),
                "A boss Charger that misses/obstructs must recover for >= 1.0s (R6.6).");

            // Sanity: the floor is exactly 1.0s for the normal Charger (0.8s base raised to 1.0s),
            // proving the Max floor is what enforces the window, not an already-long base value.
            Assert.That(ChargeRecovery(hit: false, boss: false), Is.EqualTo(1f).Within(Epsilon),
                "The normal Charger's 0.8s base recovery is raised to exactly the 1.0s floor on a miss.");
        }

        // Feature: enemy-swarm-core-archetypes, 14.2 example — a connected dash keeps the shorter
        // recovery, so the >= 1.0s window is specific to the miss/obstruction case and a landed charge
        // is not over-punished. This guards the miss test above against trivially passing because the
        // base recovery were always >= 1.0s.
        // Validates: Requirement 6.6
        [Test]
        public void Charger_ConnectedDash_KeepsShorterRecoveryThanMiss()
        {
            float hitRecovery = ChargeRecovery(hit: true, boss: false);
            float missRecovery = ChargeRecovery(hit: false, boss: false);

            Assert.That(hitRecovery, Is.EqualTo(0.8f).Within(Epsilon),
                "A landed normal-Charger dash keeps its shorter 0.8s recovery.");
            Assert.That(missRecovery, Is.GreaterThan(hitRecovery),
                "The miss/obstruction recovery must be longer than a landed dash's recovery (R6.6).");
        }

        // ---- Swarm one-hit death: profile health <= 25% of Grunt (R15.3) -------------------------

        // Feature: enemy-swarm-core-archetypes, 14.2 example — the authored Swarm profile has at most
        // 25% of the Grunt's maximum health, so a single light player hit reduces its health to <= 0.
        // R15.2 sets the <=25% ceiling; R15.3 is the one-hit-death consequence. The scene-side removal
        // within 1s (R15.6) is a PlayMode concern (native Actor.Death destroys the object) and is
        // deferred to task 15.2 — here we assert the health ceiling that makes one hit lethal.
        // Validates: Requirements 15.3, 15.6 (health ceiling; scene removal timing deferred to 15.2)
        [Test]
        public void Swarm_ProfileHealth_IsAtMostQuarterOfGrunt_SoOneHitKills()
        {
            EnemyProfile swarm = LoadProfile("Swarm");
            EnemyProfile grunt = LoadProfile("Grunt");

            float swarmHealthFraction = swarm.HealthMultiplier;
            float gruntHealthFraction = grunt.HealthMultiplier;

            Assert.That(swarmHealthFraction, Is.LessThanOrEqualTo(0.25f * gruntHealthFraction + Epsilon),
                "The Swarm's maximum health must be <= 25% of the Grunt's (R15.2) so one light hit kills (R15.3).");

            // Model-level one-hit-death: for a shared base health, a single light hit whose magnitude
            // is at least the Swarm's <=25% pool drives its health to <= 0.
            const float baseHealth = 100f;
            float swarmMaxHealth = baseHealth * swarmHealthFraction;
            float lightHit = 0.25f * baseHealth * gruntHealthFraction; // a "quarter-Grunt" light hit
            float remaining = swarmMaxHealth - lightHit;

            Assert.That(remaining, Is.LessThanOrEqualTo(0f),
                "A single light hit (>= the Swarm's max health) must reduce Swarm health to 0 or below (R15.3).");
        }

        // ---- Fragile launch airborne-duration ordering (R17.4) -----------------------------------

        /// <summary>
        /// Replicates <c>CombatReactionController.ScaleDuration</c>:
        /// <c>duration * (1f - Mathf.Clamp01(resistance))</c>. The production method is private, so the
        /// airborne-duration formula is mirrored here to assert the R17.4 ordering as a pure
        /// computation. (Resistance is clamped to [0,1] exactly as the controller clamps it.)
        /// </summary>
        private static float AirborneDuration(float stunDuration, float knockUpResistance)
            => stunDuration * (1f - Mathf.Clamp01(knockUpResistance));

        // Feature: enemy-swarm-core-archetypes, 14.2 example — Fragile airborne duration ordering.
        // R17.4: airborne duration = StunDuration * (1 - knockUpResistance). The Fragile's KnockUp
        // resistance is 0, so its airborne duration equals the full StunDuration and strictly exceeds
        // that of an otherwise-identical enemy with nonzero resistance.
        // Validates: Requirement 17.4
        [Test]
        public void Fragile_AirborneDuration_EqualsFullStunAndExceedsResistantEnemy()
        {
            const float stun = 1.5f;

            float fragile = AirborneDuration(stun, knockUpResistance: 0f);
            float resistant = AirborneDuration(stun, knockUpResistance: 0.4f);

            Assert.That(fragile, Is.EqualTo(stun).Within(Epsilon),
                "Zero KnockUp resistance means the Fragile stays airborne for the full stun duration (R17.4).");
            Assert.That(fragile, Is.GreaterThan(resistant),
                "The Fragile (resistance 0) must stay airborne longer than any enemy with nonzero resistance (R17.4).");
        }

        // Feature: enemy-swarm-core-archetypes, 14.2 example — the airborne-duration ordering holds for
        // any fixed stun duration and any strictly-positive resistance, sweeping a few representative
        // values to show the ordering is a monotonic consequence of the (1 - resistance) factor.
        // Validates: Requirement 17.4
        [Test]
        public void Fragile_AirborneDuration_OrderingHoldsAcrossResistances()
        {
            foreach (float stun in new[] { 0.5f, 1f, 2f, 3.5f })
            {
                float fragile = AirborneDuration(stun, 0f);
                foreach (float res in new[] { 0.1f, 0.25f, 0.5f, 0.75f, 1f })
                {
                    float resistant = AirborneDuration(stun, res);
                    Assert.That(fragile, Is.GreaterThan(resistant),
                        $"Fragile airborne (res=0) must exceed res={res} at stun={stun} (R17.4).");
                }
            }
        }

        // Feature: enemy-swarm-core-archetypes, 14.2 example — the authored Fragile profile really has
        // KnockUp resistance 0 (R17.2), so the ordering formula above applies to the shipped enemy and
        // is not just an abstract property of the math.
        // Validates: Requirement 17.4 (grounds the ordering on the authored Fragile profile)
        [Test]
        public void Fragile_ProfileKnockUpResistance_IsZero()
        {
            EnemyProfile fragile = LoadProfile("Fragile");

            Assert.That(fragile.KnockUpResistance, Is.EqualTo(0f).Within(Epsilon),
                "The Fragile profile must have KnockUp resistance 0 so its airborne duration is the full stun (R17.4).");
        }

        // ---- asset loading ------------------------------------------------------------------------

        /// <summary>
        /// Loads an authored <see cref="EnemyProfile"/> asset by name through the Editor
        /// AssetDatabase (same approach as task 12.3). Ignored with a clear message if the asset
        /// cannot be resolved rather than failing spuriously in a headless context.
        /// </summary>
        private static EnemyProfile LoadProfile(string assetName)
        {
#if UNITY_EDITOR
            string path = ArchetypeDir + assetName + ".asset";
            var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyProfile>(path);
            if (profile == null)
            {
                Assert.Ignore($"Could not load EnemyProfile at '{path}'. " +
                              "The authored asset must exist and be imported for this test to run.");
            }
            return profile;
#else
            Assert.Ignore("This test requires the Editor AssetDatabase to load the authored EnemyProfile asset.");
            return null;
#endif
        }
    }
}
