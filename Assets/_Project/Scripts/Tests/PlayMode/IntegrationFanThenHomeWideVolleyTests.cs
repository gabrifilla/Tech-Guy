using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode INTEGRATION test for modifier-synergies-theme17 (task 16.1, scenario 2):
    /// a full fan-then-home WideVolley.
    ///
    /// This exercises the WideVolley + Homing interaction end-to-end: a fan of arrows leaves along
    /// their assigned spread headings (zero launch-frame correction, R4.1) and then each arrow homes
    /// independently onto its own in-cone enemy (R4.6), curving toward — and reaching — distinct
    /// targets. The focused R4 property tests assert each rule in isolation; this integration test
    /// asserts they compose across a whole fan flown across real frames.
    ///
    /// Integration level reached (documented per task 16.1): staging a real WideVolley through
    /// <c>ArsenalCombat</c> requires a full weapon-cast setup that is impractical in a test rig, so
    /// this test integrates at the highest practical level — it reproduces the exact fan-spread
    /// heading formula used by <c>ArsenalCombat</c> (<c>AngleAxis((i - (n-1)*.5) * step, up)</c>) and
    /// fires those fanned arrows through the same static <c>ArsenalProjectile.Fire</c> path with
    /// Homing enabled, then drives their <c>Update</c>. Every arrow's launch heading, per-frame seek,
    /// steering, and physics sweep are the real production code paths.
    ///
    /// Runs in PlayMode: homing seeks/steers inside <see cref="ArsenalProjectile"/>'s MonoBehaviour
    /// <c>Update</c> against live colliders and depends on real <c>Time.deltaTime</c> stepping.
    /// Reuses <see cref="ArsenalProjectileTestRig"/>; no <c>FindObjectOfType</c>/<c>GameObject.Find</c>/
    /// magic strings.
    ///
    /// Validates: Requirements 4.1, 4.6.
    /// </summary>
    public sealed class IntegrationFanThenHomeWideVolleyTests
    {
        private const float SeekRadius = 7f;

        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // The fan-spread step ArsenalCombat applies per arrow: min(9, 100/(n-1)) degrees.
        private static float SpreadStep(int arrows) => Mathf.Min(9f, 100f / Mathf.Max(1, arrows - 1));

        // The heading ArsenalCombat assigns to arrow i of n around a base direction.
        private static Vector3 FanHeading(int i, int arrows, Vector3 direction) =>
            Quaternion.AngleAxis((i - (arrows - 1) * 0.5f) * SpreadStep(arrows), Vector3.up) * direction;

        // Feature: modifier-synergies-theme17 (integration): a WideVolley leaves as a fan (each arrow's
        // launch heading equals its assigned fan heading, zero launch-frame homing correction) and then
        // every arrow homes onto its OWN in-cone enemy, locking a set of distinct targets.
        // Validates: Requirements 4.1, 4.6
        [UnityTest]
        public IEnumerator FanThenHome_ArrowsLaunchOnFanHeadingThenSeekIndependently()
        {
            _rig.BuildOwner();

            const int arrows = 5; // WideVolley-sized fan
            Vector3 baseDir = Vector3.forward;
            Vector3 origin = Vector3.zero;

            var headings = new List<Vector3>();
            var launched = new List<ArsenalProjectile>();
            var expectedTargets = new List<Actor>();

            for (int i = 0; i < arrows; i++)
            {
                Vector3 heading = FanHeading(i, arrows, baseDir);
                headings.Add(heading);

                // Plant one enemy straight down each arrow's fan heading, inside radius and cone,
                // so an independent seek can lock a distinct target per arrow.
                float depth = 3f + i * 0.2f;
                expectedTargets.Add(_rig.BuildEnemy(origin + heading * depth));

                ArsenalProjectile arrow = _rig.FireHomingArrow(origin, heading, 40f);
                Assert.IsNotNull(arrow, $"arrow {i} failed to spawn");
                launched.Add(arrow);
            }

            // --- R4.1: launch-frame heading equals fan heading (no homing correction yet) ---
            for (int i = 0; i < arrows; i++)
            {
                float drift = Vector3.Angle(headings[i], launched[i].transform.forward);
                Assert.LessOrEqual(drift, 0.01f,
                    $"arrow {i}: launch heading drifted {drift:F4}° from its fan heading before any seek");
            }

            // Advance one frame: _nextSeek was scheduled one interval out, so the launch frame still
            // applies zero correction — the fan is preserved.
            yield return null;
            for (int i = 0; i < arrows; i++)
            {
                if (!launched[i]) continue;
                float drift = Vector3.Angle(headings[i], launched[i].transform.forward);
                Assert.LessOrEqual(drift, 0.01f,
                    $"arrow {i}: applied homing correction on the launch frame ({drift:F4}°)");
            }

            // --- R4.6: each arrow seeks independently from its own position/cone ---
            var locked = new HashSet<Actor>();
            for (int i = 0; i < arrows; i++)
            {
                Assert.IsNotNull(launched[i], $"arrow {i} was destroyed before seeking");
                Actor target = _rig.FindNextTarget(launched[i], launched[i].transform.position, SeekRadius, true);
                Assert.AreSame(expectedTargets[i], target,
                    $"arrow {i} did not lock its own fan-heading enemy from its own cone");
                if (target) locked.Add(target);
            }

            Assert.GreaterOrEqual(locked.Count, 2,
                $"fan-then-home should let the volley cover distinct enemies (locked {locked.Count})");
            Assert.AreEqual(arrows, locked.Count,
                "each fanned arrow should independently lock its own distinct enemy");
        }

        // Feature: modifier-synergies-theme17 (integration): flown across real frames, a fanned homing
        // arrow actually curves toward and reaches its own in-cone enemy (end-to-end seek + steer +
        // physics sweep), while the fan as a whole damages multiple distinct enemies.
        // Validates: Requirements 4.1, 4.6
        [UnityTest]
        public IEnumerator FanThenHome_FlownVolleyReachesDistinctEnemies()
        {
            _rig.BuildOwner();

            const int arrows = 3;
            Vector3 baseDir = Vector3.forward;
            Vector3 origin = Vector3.zero;

            for (int i = 0; i < arrows; i++)
            {
                Vector3 heading = FanHeading(i, arrows, baseDir);
                // Enemy offset slightly off the exact heading so a real homing curve is required to hit it.
                Vector3 lateral = Quaternion.AngleAxis(90f, Vector3.up) * heading;
                Vector3 pos = origin + heading * 4f + lateral * ((i - 1) * 0.6f);
                _rig.BuildEnemy(pos);
                Assert.IsNotNull(_rig.FireHomingArrow(origin, heading, 40f), $"arrow {i} failed to spawn");
            }

            // Fly the volley: homing arrows seek every 0.1s, steer at <=180deg/s, and sweep for hits.
            for (int frame = 0; frame < 400; frame++)
            {
                yield return null;
                if (_rig.DamageEvents.Count >= arrows) break;
            }

            // The fanned homing volley should have curved onto and damaged multiple distinct enemies.
            var distinct = new HashSet<Actor>(_rig.DamageEvents);
            Assert.GreaterOrEqual(distinct.Count, 2,
                $"a flown fan-then-home volley should reach multiple distinct enemies, hit {distinct.Count}");
        }
    }
}
