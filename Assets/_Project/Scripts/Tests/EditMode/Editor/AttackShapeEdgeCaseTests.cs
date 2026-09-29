using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example/unit tests for the attack-shape edge cases of the new attack pipeline —
    /// task 6.5 of enemy-swarm-core-archetypes.
    ///
    /// These are deliberately EXAMPLE tests (not property tests): each pins a specific boundary or
    /// ordering that the pure logic must honour. They exercise the scene-free types that back the
    /// widened pipeline (<see cref="TelegraphWindupClock"/>, <see cref="EnemyAttackPatterns"/>,
    /// <see cref="ProjectileMotion"/>, <see cref="SingleBeatResolver"/>, <see cref="EnemyAttackArea"/>)
    /// so they run without a live Unity scene.
    ///
    /// Cases that genuinely need a live scene (a real telegraph coroutine advancing frames, a real
    /// NavMesh, per-profile authored damage/interval values from the authored assets) are deferred:
    ///  - Per-profile Sniper interval/damage ordering (R9.4) depends on the authored EnemyProfile
    ///    assets (task 12.2/12.3) and is verified there; here we assert what is expressible now
    ///    without a live scene: the engagement-band ordering (R9.2) and the Select() archetype
    ///    narrowing that routes Sniper/Shooter to their distinct kinds.
    ///  - End-to-end telegraph-then-single-beat behaviour is covered by the PlayMode tests (task 15.2).
    /// </summary>
    public sealed class AttackShapeEdgeCaseTests
    {
        private const float Epsilon = 1e-6f;

        // ---- Windup boundary exactly at 0.25s (R2.1 floor, exercised by the melee/ranged windups) ----

        // Feature: enemy-swarm-core-archetypes, 6.5 example — windup authored exactly at the 0.25s floor.
        // A clock authored at exactly the floor keeps Duration == 0.25 and its beat may resolve at
        // exactly 0.25s but not a hair before.
        // Validates: Requirement 2.1 (windup floor underpinning R4.5/R9.3/R10.4 telegraphs)
        [Test]
        public void Windup_AuthoredExactlyAtFloor_HasFloorDurationAndCompletesAtBoundary()
        {
            var clock = new TelegraphWindupClock(TelegraphBeat.MinWindupSeconds);

            Assert.That(clock.Duration, Is.EqualTo(0.25f).Within(Epsilon),
                "A windup authored at exactly 0.25s should keep a 0.25s effective duration.");
            Assert.That(clock.IsComplete(0.25f), Is.True,
                "The windup should be complete at exactly the 0.25s boundary.");
            Assert.That(clock.IsComplete(0.249f), Is.False,
                "The windup must not be complete at 0.249s, just before the boundary.");
        }

        // Feature: enemy-swarm-core-archetypes, 6.5 example — a sub-floor windup clamps up to 0.25s.
        // Any authored windup below the floor is raised to exactly 0.25s, so a beat can never resolve
        // earlier than the floor no matter how short the authored value is.
        // Validates: Requirement 2.1
        [Test]
        public void Windup_AuthoredBelowFloor_ClampsUpToFloor()
        {
            var clock = new TelegraphWindupClock(0.1f);

            Assert.That(clock.Duration, Is.EqualTo(0.25f).Within(Epsilon),
                "A windup authored below 0.25s should clamp up to the 0.25s floor.");
            Assert.That(clock.IsComplete(0.24f), Is.False,
                "Even a sub-floor authored windup must not complete before the 0.25s floor.");
            Assert.That(clock.IsComplete(0.25f), Is.True,
                "The clamped windup should complete at exactly the floor.");
        }

        // ---- Sniper vs Shooter ordering (R9.2 range; R9.4 documented via Select narrowing) ----

        // Feature: enemy-swarm-core-archetypes, 6.5 example — Sniper engages farther than the Shooter.
        // R9.2: the Sniper's firing range exceeds the Shooter's. The engagement-band constants encode
        // this: LongEngagementRange (Sniper) > MediumEngagementRange (Shooter).
        // Validates: Requirement 9.2
        [Test]
        public void EngagementBands_SniperRangeExceedsShooterRange()
        {
            Assert.That(EnemyAttackPatterns.LongEngagementRange,
                Is.GreaterThan(EnemyAttackPatterns.MediumEngagementRange),
                "Sniper (long) engagement band must exceed the Shooter (medium) band (R9.2).");

            // And both stand off well beyond melee, keeping the ranged archetypes at distance.
            Assert.That(EnemyAttackPatterns.MediumEngagementRange,
                Is.GreaterThan(EnemyAttackPatterns.MeleeEngagementRange),
                "Ranged bands must sit beyond the melee band.");
        }

        // Feature: enemy-swarm-core-archetypes, 6.5 example — Select resolves each ranged archetype's
        // engagement band from its own id, preserving the Sniper > Shooter ordering (R9.2).
        // Validates: Requirement 9.2
        [Test]
        public void EngagementRange_ResolvesPerArchetype_SniperFartherThanShooter()
        {
            // Distance/traits are irrelevant here: the archetype dictates its own band.
            float sniper = EnemyAttackPatterns.EngagementRange(
                EnemyAttackTraits.None, 0, 2.5f, ArchetypeId.Sniper);
            float shooter = EnemyAttackPatterns.EngagementRange(
                EnemyAttackTraits.None, 0, 2.5f, ArchetypeId.Shooter);

            Assert.That(sniper, Is.EqualTo(EnemyAttackPatterns.LongEngagementRange).Within(Epsilon));
            Assert.That(shooter, Is.EqualTo(EnemyAttackPatterns.MediumEngagementRange).Within(Epsilon));
            Assert.That(sniper, Is.GreaterThan(shooter),
                "Per-archetype engagement range must keep Sniper farther than Shooter (R9.2).");
        }

        // Feature: enemy-swarm-core-archetypes, 6.5 example — Select narrows Sniper and Shooter to
        // their distinct attack kinds regardless of distance/traits.
        // R9.4's per-profile interval/damage ordering (Sniper damage > Grunt, Sniper interval >
        // Shooter) is authored on the EnemyProfile assets and verified once those assets exist
        // (task 12.3). What is testable now is that the pipeline routes each archetype to a distinct
        // kind, which is the seam those authored values hang off.
        // Validates: Requirement 9.4 (repertoire narrowing; per-profile values deferred to task 12.3)
        [Test]
        public void Select_NarrowsSniperAndShooterToDistinctKinds()
        {
            // Sweep a range of distances and sequences; the archetype narrowing must dominate.
            foreach (float distance in new[] { 0f, 2.5f, 6f, 12f, 22f, 40f })
            {
                for (int seq = 0; seq < 4; seq++)
                {
                    Assert.That(EnemyAttackPatterns.Select(EnemyAttackTraits.None, seq, distance, ArchetypeId.Sniper),
                        Is.EqualTo(EnemyAttackKind.SniperShot),
                        $"Sniper must always narrow to SniperShot (distance={distance}, seq={seq}).");
                    Assert.That(EnemyAttackPatterns.Select(EnemyAttackTraits.None, seq, distance, ArchetypeId.Shooter),
                        Is.EqualTo(EnemyAttackKind.AimedShot),
                        $"Shooter must always narrow to AimedShot (distance={distance}, seq={seq}).");
                }
            }

            Assert.That(EnemyAttackKind.SniperShot, Is.Not.EqualTo(EnemyAttackKind.AimedShot),
                "Sniper and Shooter must resolve to distinct attack kinds.");
        }

        // ---- Bomber player-outside-impact-area no damage (R10.3) ----

        // Feature: enemy-swarm-core-archetypes, 6.5 example — Bomber lob lands with the player OUTSIDE
        // the impact area, so no damage is applied.
        // R10.3: area damage applies only if the player is within the telegraphed impact area at the
        // moment of impact, and no damage otherwise. ResolveImpact(false) models "player outside".
        // Validates: Requirement 10.3
        [Test]
        public void Bomber_ImpactWithPlayerOutsideArea_AppliesNoDamage()
        {
            var motion = new ProjectileMotion(range: 20f);

            bool applied = motion.ResolveImpact(targetInImpactArea: false);

            Assert.That(applied, Is.False, "Impact with the player outside the area must apply no damage (R10.3).");
            Assert.That(motion.DamageApplied, Is.False, "No single beat should have landed.");
            Assert.That(motion.IsMarkedForDestruction, Is.True,
                "The projectile and its telegraph are still removed on land, even on a miss (R10.5).");
        }

        // Feature: enemy-swarm-core-archetypes, 6.5 example — Bomber lob lands with the player INSIDE
        // the impact area applies its single beat exactly once, and never again.
        // Validates: Requirement 10.3
        [Test]
        public void Bomber_ImpactWithPlayerInsideArea_AppliesDamageExactlyOnce()
        {
            var motion = new ProjectileMotion(range: 20f);

            bool first = motion.ResolveImpact(targetInImpactArea: true);
            bool second = motion.ResolveImpact(targetInImpactArea: true);

            Assert.That(first, Is.True, "First impact with the player inside the area should apply damage once.");
            Assert.That(second, Is.False, "A landed projectile is spent; a second resolve must not re-apply damage.");
            Assert.That(motion.DamageApplied, Is.True, "Exactly one beat should have landed.");
        }

        // ---- Grunt player-leaves-melee no-damage + stance retained (R4.5) ----

        // Feature: enemy-swarm-core-archetypes, 6.5 example — the player leaves the Grunt's melee cone
        // before the beat, so the single-beat resolver produces no beat (no damage applied).
        // Validates: Requirement 4.5
        [Test]
        public void Grunt_PlayerOutsideMeleeAreaAtBeat_ResolvesNoDamage()
        {
            // A forward melee cone in front of the Grunt (origin at the Grunt, facing +Z).
            var meleeCone = new EnemyAttackArea(EnemyAttackShape.Cone, Vector3.zero, Vector3.forward, 2.5f, angle: 110);
            var areas = new[] { meleeCone };
            var resolver = new SingleBeatResolver();

            // Player has stepped out of melee range (beyond the cone reach) before the beat opens.
            var playerOutside = Vector3.forward * 3.5f;
            Assert.That(meleeCone.Contains(playerOutside), Is.False,
                "The player who left melee range should be outside the cone.");

            bool resolved = resolver.TryResolve(areas, playerOutside, damage: 10f);

            Assert.That(resolved, Is.False, "No beat should resolve when the player is outside the melee area (R4.5).");
            Assert.That(resolver.BeatResolved, Is.False, "A missed beat must not latch the resolver.");
        }

        // Feature: enemy-swarm-core-archetypes, 6.5 example — a missed melee beat implies no stance
        // change (model-level). The Grunt only pays stance when a hit lands; a beat that never resolves
        // leaves stance untouched. R4.5 requires the Grunt to retain its remaining stance.
        // Validates: Requirement 4.5
        [Test]
        public void Grunt_MissedBeat_RetainsStance()
        {
            var meleeCone = new EnemyAttackArea(EnemyAttackShape.Cone, Vector3.zero, Vector3.forward, 2.5f, angle: 110);
            var areas = new[] { meleeCone };
            var resolver = new SingleBeatResolver();

            // Model the Grunt's stance pool: it only changes if the attack lands a beat.
            const int startingStance = 5;
            int stance = startingStance;

            var playerOutside = Vector3.right * 4f; // to the flank, clearly outside the forward cone
            bool resolved = resolver.TryResolve(areas, playerOutside, damage: 10f);
            if (resolved) stance -= 1; // a landed beat would be the only reason to touch stance

            Assert.That(resolved, Is.False, "The out-of-range beat should not resolve.");
            Assert.That(stance, Is.EqualTo(startingStance), "A missed beat must leave the Grunt's stance unchanged (R4.5).");
        }

        // Feature: enemy-swarm-core-archetypes, 6.5 example — sanity companion: a player still inside
        // the melee cone at the beat DOES take the single beat (so the "no damage on miss" case above
        // is a genuine miss, not a resolver that never fires).
        // Validates: Requirement 4.5
        [Test]
        public void Grunt_PlayerInsideMeleeAreaAtBeat_ResolvesExactlyOnce()
        {
            var meleeCone = new EnemyAttackArea(EnemyAttackShape.Cone, Vector3.zero, Vector3.forward, 2.5f, angle: 110);
            var areas = new[] { meleeCone };
            var resolver = new SingleBeatResolver();

            var playerInside = Vector3.forward * 2f; // in front, within reach

            Assert.That(meleeCone.Contains(playerInside), Is.True, "The in-range player should be inside the cone.");
            Assert.That(resolver.TryResolve(areas, playerInside, damage: 10f), Is.True,
                "A player inside the melee area should take the beat.");
            Assert.That(resolver.TryResolve(areas, playerInside, damage: 10f), Is.False,
                "The melee beat is single per attack.");
        }
    }
}
