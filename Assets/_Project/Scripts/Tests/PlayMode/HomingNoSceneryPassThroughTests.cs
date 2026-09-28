using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R4 Property 15 of modifier-synergies-theme17
    /// (No scenery pass-through).
    ///
    /// Property 15: for any homing arrow whose swept path (sphere cast, radius 0.12) intersects
    /// non-Enemy solid scenery before reaching an enemy, the arrow is destroyed at the impact point and
    /// does not damage an enemy beyond the scenery ("sem atravessar paredes").
    ///
    /// This must run in PlayMode: <see cref="ArsenalProjectile"/> resolves scenery blocking with a real
    /// <c>Physics.SphereCastAll</c> against live colliders each Update, and destruction happens through
    /// the MonoBehaviour lifecycle. The rig builds a solid (non-Actor) wall and an enemy behind it, then
    /// the arrow is stepped frame-by-frame until it resolves.
    ///
    /// Validates: Requirements 4.5
    /// </summary>
    public sealed class HomingNoSceneryPassThroughTests
    {
        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 15
        // A wall stands between the arrow and an enemy behind it. The homing arrow is destroyed at the
        // wall and the enemy beyond is never damaged, for any wall distance/thickness and enemy gap.
        // Validates: Requirements 4.5
        [UnityTest]
        public IEnumerator Property15_DestroyedAtSceneryNeverHitsEnemyBeyond()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 15);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                Vector3 heading = Vector3.forward;

                // Solid wall spanning the heading at a random distance and thickness.
                float wallDist = 3f + (float)rng.NextDouble() * 4f;      // 3..7 m ahead
                float wallThickness = 0.5f + (float)rng.NextDouble();    // 0.5..1.5 m deep
                _rig.BuildWall(new Vector3(0f, 0f, wallDist), new Vector3(6f, 4f, wallThickness));

                // Enemy strictly beyond the wall — the arrow must never reach it.
                float gap = 1f + (float)rng.NextDouble() * 3f;
                Actor beyond = _rig.BuildEnemy(new Vector3(0f, 0f, wallDist + wallThickness + gap));

                // Fire a homing arrow straight at the wall (no enemy in front, so any seek keeps heading).
                ArsenalProjectile arrow = _rig.FireHomingArrow(Vector3.zero, heading, 60f);
                Assert.IsNotNull(arrow, $"case {c}: homing arrow failed to spawn");

                // Force seeks to be due each frame so homing is fully active while approaching the wall;
                // the seek finds nothing in front (the only enemy is behind solid scenery), so the arrow
                // travels straight into the wall.
                _rig.SetNextSeek(arrow, -1f);

                bool destroyed = false;
                for (int frame = 0; frame < 400; frame++)
                {
                    yield return null;
                    if (!arrow) { destroyed = true; break; }
                    _rig.SetNextSeek(arrow, -1f); // keep re-seeking every frame
                }

                Assert.IsTrue(destroyed, $"case {c}: arrow was not destroyed at the wall (passed through or lingered)");
                Assert.IsFalse(_rig.DamageEvents.Contains(beyond),
                    $"case {c}: an enemy behind solid scenery was damaged — arrow tunneled through the wall");
            }
        }

        // Feature: modifier-synergies-theme17, Property 15
        // Control: with NO wall, the same arrow does reach and damage the enemy — confirming the block
        // in the previous test is caused by the scenery, not by the arrow failing to travel.
        // Validates: Requirements 4.5
        [UnityTest]
        public IEnumerator Property15_ControlNoWallReachesEnemy()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 151);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                Vector3 heading = Vector3.forward;
                float dist = 3f + (float)rng.NextDouble() * 5f; // 3..8 m ahead, in-line
                Actor target = _rig.BuildEnemy(new Vector3(0f, 0f, dist));

                ArsenalProjectile arrow = _rig.FireHomingArrow(Vector3.zero, heading, 60f);
                Assert.IsNotNull(arrow, $"case {c}: homing arrow failed to spawn");

                for (int frame = 0; frame < 400 && arrow; frame++)
                {
                    yield return null;
                    if (_rig.DamageEvents.Contains(target)) break;
                }

                Assert.IsTrue(_rig.DamageEvents.Contains(target),
                    $"case {c}: with no wall, the arrow should reach and damage the in-line enemy");
            }
        }
    }
}
