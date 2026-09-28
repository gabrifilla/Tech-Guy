using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode example test for R1 criterion 1.5 of modifier-synergies-theme17: an arrow with zero
    /// remaining ricochet bounces and piercing disabled is destroyed after resolving a hit. Runs in
    /// PlayMode so the real destroy lifecycle (deferred to end of frame) is observed.
    /// </summary>
    public sealed class ArsenalTerminalDestroyTests
    {
        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17 — terminal destroy example.
        // Validates: Requirements 1.5
        [UnityTest]
        public IEnumerator NonPiercingZeroBounceArrow_IsDestroyedAfterResolvingHit()
        {
            _rig.BuildOwner();
            Actor enemy = _rig.BuildEnemy(new Vector3(0f, 0f, 3f));

            ArsenalProjectile arrow = _rig.FireArrow(Vector3.zero, Vector3.forward, 40f,
                piercing: false, ricochetBounces: 0);
            Assert.IsNotNull(arrow, "arrow failed to spawn");

            bool destroyed = false;
            for (int frame = 0; frame < 120; frame++)
            {
                yield return null;
                if (!arrow) { destroyed = true; break; }
            }

            Assert.IsTrue(_rig.DamageEvents.Contains(enemy), "the enemy was never hit");
            Assert.IsTrue(destroyed, "arrow was not destroyed after resolving the hit with no bounces and no piercing");
        }

        // Complementary guard: an arrow that still has bounces or piercing does NOT terminate on the
        // first hit (it continues), distinguishing the terminal condition from a live one.
        // Validates: Requirements 1.5 (negative case)
        [UnityTest]
        public IEnumerator PiercingArrow_SurvivesFirstHitWhenInlineTargetRemains()
        {
            _rig.BuildOwner();
            _rig.BuildEnemy(new Vector3(0f, 0f, 3f));
            _rig.BuildEnemy(new Vector3(0f, 0f, 5f)); // second in-line target keeps it alive

            ArsenalProjectile arrow = _rig.FireArrow(Vector3.zero, Vector3.forward, 40f,
                piercing: true, ricochetBounces: 0);
            Assert.IsNotNull(arrow, "arrow failed to spawn");

            // After the first enemy is hit the arrow should still exist (one more in-line target).
            bool survivedAfterFirstHit = false;
            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
                if (arrow && _rig.HitSet(arrow).Count == 1) { survivedAfterFirstHit = true; break; }
            }

            Assert.IsTrue(survivedAfterFirstHit, "piercing arrow did not survive its first hit while an in-line target remained");
        }
    }
}
