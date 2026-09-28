using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R4 Property 14 of modifier-synergies-theme17
    /// (Independent per-arrow seeking).
    ///
    /// Property 14: for any volley of two or more homing arrows (including TwinShot multiplication),
    /// each arrow computes its target from its own position and forward cone, so that when two or more
    /// distinct qualifying enemies exist the arrows may lock onto different enemies.
    ///
    /// <see cref="ArsenalProjectile"/> seeks per-arrow inside its MonoBehaviour <c>Update</c> by calling
    /// the private <c>FindNextTarget(transform.position, 7, forwardOnly:true)</c> — evaluated from each
    /// arrow's own transform. This test drives that exact seam with real colliders in PlayMode: multiple
    /// arrows are given distinct positions/headings (as a fan spreads them) and each independently
    /// resolves its nearest in-cone enemy. R4.6 (TwinShot copies each seek independently) is covered by
    /// the same mechanism — every spawned arrow runs its own Update/seek from its own transform, which
    /// this test exercises directly per arrow.
    ///
    /// Validates: Requirements 4.4, 4.6
    /// </summary>
    public sealed class HomingIndependentSeekTests
    {
        private const float SeekRadius = 7f;

        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 14
        // Two homing arrows offset in position/heading (as a fan spreads them), each with its own
        // nearest in-cone enemy, resolve DIFFERENT targets — proving the seek is per-arrow, not shared.
        // Validates: Requirements 4.4
        [UnityTest]
        public IEnumerator Property14_ArrowsLockDifferentEnemiesFromOwnCone()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 14);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                // Left arrow aims left-forward; right arrow aims right-forward (a fan). Each gets a
                // distinct enemy planted straight down its own heading, inside radius and cone.
                float spread = 20f + (float)rng.NextDouble() * 20f; // 20..40 degrees off center
                Vector3 leftHeading = Quaternion.AngleAxis(-spread, Vector3.up) * Vector3.forward;
                Vector3 rightHeading = Quaternion.AngleAxis(spread, Vector3.up) * Vector3.forward;

                Vector3 leftOrigin = new Vector3(-1.5f, 0f, 0f);
                Vector3 rightOrigin = new Vector3(1.5f, 0f, 0f);

                float dL = 2f + (float)rng.NextDouble() * 3f;
                float dR = 2f + (float)rng.NextDouble() * 3f;
                Actor leftEnemy = _rig.BuildEnemy(leftOrigin + leftHeading * dL);
                Actor rightEnemy = _rig.BuildEnemy(rightOrigin + rightHeading * dR);

                ArsenalProjectile leftArrow = _rig.FireHomingArrow(leftOrigin, leftHeading, 40f);
                ArsenalProjectile rightArrow = _rig.FireHomingArrow(rightOrigin, rightHeading, 40f);
                Assert.IsNotNull(leftArrow, $"case {c}: left arrow failed to spawn");
                Assert.IsNotNull(rightArrow, $"case {c}: right arrow failed to spawn");

                Actor leftTarget = _rig.FindNextTarget(leftArrow, leftArrow.transform.position, SeekRadius, true);
                Actor rightTarget = _rig.FindNextTarget(rightArrow, rightArrow.transform.position, SeekRadius, true);

                Assert.AreSame(leftEnemy, leftTarget, $"case {c}: left arrow did not lock its own in-cone enemy");
                Assert.AreSame(rightEnemy, rightTarget, $"case {c}: right arrow did not lock its own in-cone enemy");
                Assert.AreNotSame(leftTarget, rightTarget,
                    $"case {c}: arrows locked the same enemy — seek is not independent per-arrow");

                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 14
        // The seek is evaluated from each arrow's OWN position: moving one arrow to a position where a
        // different enemy is nearest changes only that arrow's lock, not the others'.
        // Validates: Requirements 4.4, 4.6
        [UnityTest]
        public IEnumerator Property14_SeekUsesEachArrowsOwnPosition()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 141);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                // Two enemies straight ahead of the shared launch axis at different depths.
                float nearDepth = 2f + (float)rng.NextDouble() * 1.5f;
                float farDepth = nearDepth + 2f + (float)rng.NextDouble() * 2f;
                Actor nearEnemy = _rig.BuildEnemy(new Vector3(0f, 0f, nearDepth));
                Actor farEnemy = _rig.BuildEnemy(new Vector3(0f, 0f, farDepth));

                // Two homing arrows on the same heading but at different positions along the axis:
                // arrow A behind both, arrow B between near and far.
                Vector3 heading = Vector3.forward;
                ArsenalProjectile arrowA = _rig.FireHomingArrow(Vector3.zero, heading, 40f);
                ArsenalProjectile arrowB = _rig.FireHomingArrow(new Vector3(0f, 0f, nearDepth + 0.75f), heading, 40f);
                Assert.IsNotNull(arrowA, $"case {c}: arrow A failed to spawn");
                Assert.IsNotNull(arrowB, $"case {c}: arrow B failed to spawn");

                // A sees the near enemy first (nearest, in cone). B has already passed the near enemy,
                // so from B's own position only the far enemy is ahead within the cone.
                Actor targetA = _rig.FindNextTarget(arrowA, arrowA.transform.position, SeekRadius, true);
                Actor targetB = _rig.FindNextTarget(arrowB, arrowB.transform.position, SeekRadius, true);

                Assert.AreSame(nearEnemy, targetA, $"case {c}: arrow A should lock the nearer enemy from its position");
                Assert.AreSame(farEnemy, targetB, $"case {c}: arrow B should lock the far enemy from its OWN (advanced) position");
                Assert.AreNotSame(targetA, targetB,
                    $"case {c}: arrows at different positions should resolve different targets");

                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 14 (R4.6)
        // A volley of many independently-spawned homing arrows, each fanned toward its own enemy,
        // collectively locks a set of distinct enemies — no single shared target is forced.
        // Validates: Requirements 4.6
        [UnityTest]
        public IEnumerator Property14_VolleyDistributesAcrossDistinctEnemies()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 142);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                int n = 3 + rng.Next(0, 3); // 3..5 arrows in the fan
                var arrows = new List<ArsenalProjectile>();
                var expected = new List<Actor>();

                for (int i = 0; i < n; i++)
                {
                    // Fan each arrow across a shared arc; give each its own enemy straight down its heading.
                    float angle = -30f + 60f * (i / (float)(n - 1));
                    Vector3 heading = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.forward;
                    Vector3 origin = Quaternion.AngleAxis(angle, Vector3.up) * Vector3.right * 0.3f; // slight lateral offset
                    float d = 2.5f + (float)rng.NextDouble() * 2f;
                    expected.Add(_rig.BuildEnemy(origin + heading * d));
                    arrows.Add(_rig.FireHomingArrow(origin, heading, 40f));
                }

                var locked = new HashSet<Actor>();
                for (int i = 0; i < n; i++)
                {
                    Assert.IsNotNull(arrows[i], $"case {c}: arrow {i} failed to spawn");
                    Actor t = _rig.FindNextTarget(arrows[i], arrows[i].transform.position, SeekRadius, true);
                    Assert.AreSame(expected[i], t, $"case {c}: arrow {i} did not lock its own in-cone enemy");
                    if (t) locked.Add(t);
                }

                // Independent seeking allows the volley to cover multiple distinct enemies rather than
                // collapsing onto one shared target.
                Assert.GreaterOrEqual(locked.Count, 2,
                    $"case {c}: independent seeking should let arrows lock multiple distinct enemies (locked {locked.Count})");

                yield return null;
            }
        }
    }
}
