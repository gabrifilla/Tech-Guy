using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R4 Property 13 of modifier-synergies-theme17
    /// (Homing steering constraints).
    ///
    /// Property 13: for any homing arrow, on the launch frame it applies zero correction (heading
    /// equals its fan heading); at each re-seek it selects only a target within the 7-unit radius and
    /// 50-degree forward half-angle; when no such target exists it keeps its current heading; and per
    /// frame it rotates by at most 180-degrees·Δt toward its target.
    ///
    /// These run in PlayMode because <see cref="ArsenalProjectile"/> seeks and steers inside its
    /// MonoBehaviour <c>Update</c> against live colliders via <c>Physics.OverlapSphere</c>, and the
    /// per-frame turn cap depends on real <c>Time.deltaTime</c> stepping — neither is available in
    /// EditMode.
    ///
    /// Validates: Requirements 4.1, 4.2, 4.3
    /// </summary>
    public sealed class HomingSteeringConstraintsTests
    {
        private const float SeekRadius = 7f;
        private const float SeekConeDeg = 50f;
        private const float TurnRateDeg = 180f;

        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 13
        // R4.1: on the launch frame the arrow applies zero homing correction, so its initial forward
        // exactly equals the assigned fan heading — even when a valid target sits inside the cone.
        // Validates: Requirements 4.1
        [UnityTest]
        public IEnumerator Property13_LaunchFrameAppliesZeroCorrection()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 13);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                // A random launch heading in the horizontal plane (the fan heading).
                float launchAngle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
                Vector3 heading = new Vector3(Mathf.Sin(launchAngle), 0f, Mathf.Cos(launchAngle));

                // Place a valid, in-cone target so that IF the launch frame steered, the heading would
                // measurably change toward it.
                _rig.BuildEnemy(Vector3.zero + heading * 3f);

                ArsenalProjectile arrow = _rig.FireHomingArrow(Vector3.zero, heading, 40f);
                Assert.IsNotNull(arrow, $"case {c}: homing arrow failed to spawn");

                Vector3 spawnForward = arrow.transform.forward;

                // Advance exactly one Update. _nextSeek was scheduled one interval out, so the launch
                // frame must not seek or steer: the heading stays the fan heading.
                yield return null;
                if (!arrow) { Assert.Fail($"case {c}: arrow destroyed on launch frame unexpectedly"); }

                float drift = Vector3.Angle(spawnForward, arrow.transform.forward);
                Assert.LessOrEqual(drift, 0.01f,
                    $"case {c}: launch frame applied homing correction ({drift:F4}° drift); heading must equal fan heading");
            }
        }

        // Feature: modifier-synergies-theme17, Property 13
        // R4.2: a re-seek only selects a target inside the 7-unit radius AND the 50-degree forward
        // half-angle. Enemies outside either gate must never be selected.
        // Validates: Requirements 4.2
        [UnityTest]
        public IEnumerator Property13_SeekGateRespectsRadiusAndCone()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 130);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                Vector3 heading = Vector3.forward;
                ArsenalProjectile arrow = _rig.FireHomingArrow(Vector3.zero, heading, 40f);
                Assert.IsNotNull(arrow, $"case {c}: homing arrow failed to spawn");

                // Deliberately place one enemy outside the gate: either beyond the radius or outside
                // the forward cone. It must never be selected by the per-arrow seek.
                bool violateRadius = rng.Next(0, 2) == 0;
                Vector3 badPos;
                if (violateRadius)
                {
                    // In-cone (straight ahead-ish) but beyond 7 units.
                    float dist = SeekRadius + 1f + (float)rng.NextDouble() * 8f; // 8..16
                    float coneAngle = (float)(rng.NextDouble() * (SeekConeDeg - 5f) * System.Math.PI / 180.0);
                    badPos = Quaternion.AngleAxis(Mathf.Rad2Deg * coneAngle, Vector3.up) * heading * dist;
                }
                else
                {
                    // Within radius but outside the 50-degree cone.
                    float dist = 1.5f + (float)rng.NextDouble() * (SeekRadius - 2f); // 1.5..6.5
                    float angle = SeekConeDeg + 5f + (float)rng.NextDouble() * 100f; // 55..155
                    if (rng.Next(0, 2) == 0) angle = -angle;
                    badPos = Quaternion.AngleAxis(angle, Vector3.up) * heading * dist;
                }
                Actor bad = _rig.BuildEnemy(badPos);

                Actor selected = _rig.FindNextTarget(arrow, arrow.transform.position, SeekRadius, true);
                Assert.AreNotSame(bad, selected,
                    $"case {c}: seek selected an out-of-gate enemy (violateRadius={violateRadius})");
                Assert.IsNull(selected,
                    $"case {c}: no in-gate enemy exists, so seek must return null");
                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 13
        // R4.3: with no valid target at a re-seek, the arrow keeps its heading (no correction applied).
        // Validates: Requirements 4.3
        [UnityTest]
        public IEnumerator Property13_KeepsHeadingWhenNoTarget()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 131);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                float launchAngle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
                Vector3 heading = new Vector3(Mathf.Sin(launchAngle), 0f, Mathf.Cos(launchAngle));

                // No enemies at all — every seek returns null.
                ArsenalProjectile arrow = _rig.FireHomingArrow(Vector3.zero, heading, 60f);
                Assert.IsNotNull(arrow, $"case {c}: homing arrow failed to spawn");

                Vector3 spawnForward = arrow.transform.forward;

                // Force the next seek to be due immediately, then step several frames. The seek finds
                // nothing, so the steering block is skipped and the heading is preserved.
                _rig.SetNextSeek(arrow, -1f);
                int frames = 3 + rng.Next(0, 5);
                for (int f = 0; f < frames && arrow; f++)
                    yield return null;

                if (!arrow) continue; // reached max range and expired; heading was never corrected

                float drift = Vector3.Angle(spawnForward, arrow.transform.forward);
                Assert.LessOrEqual(drift, 0.01f,
                    $"case {c}: heading drifted ({drift:F4}°) despite no valid target");
            }
        }

        // Feature: modifier-synergies-theme17, Property 13
        // R4.2: per frame the arrow rotates by at most 180-degrees·Δt toward its target. Placing a
        // target directly behind the arrow forces the maximum turn, which must still be capped.
        // Validates: Requirements 4.2
        [UnityTest]
        public IEnumerator Property13_TurnRateIsCappedPerFrame()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 132);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                Vector3 heading = Vector3.forward;
                // A target within radius and inside the forward cone, offset enough to demand a real turn.
                float side = (rng.Next(0, 2) == 0 ? 1f : -1f);
                float lateral = 1f + (float)rng.NextDouble() * 3f;
                float ahead = 1.5f + (float)rng.NextDouble() * 3f;
                _rig.BuildEnemy(new Vector3(side * lateral, 0f, ahead));

                ArsenalProjectile arrow = _rig.FireHomingArrow(Vector3.zero, heading, 60f);
                Assert.IsNotNull(arrow, $"case {c}: homing arrow failed to spawn");

                _rig.SetNextSeek(arrow, -1f); // seek is due now

                // Measure per-frame heading delta across several steered frames; each must be capped.
                for (int f = 0; f < 6 && arrow; f++)
                {
                    Vector3 beforeForward = arrow.transform.forward;
                    float dtBefore = Time.time;
                    yield return null;
                    if (!arrow) break;

                    float dt = Mathf.Max(Time.deltaTime, 1e-4f);
                    float delta = Vector3.Angle(beforeForward, arrow.transform.forward);
                    float cap = TurnRateDeg * dt + 0.5f; // small tolerance for float/step jitter
                    Assert.LessOrEqual(delta, cap,
                        $"case {c}, frame {f}: turned {delta:F3}° > cap {cap:F3}° (dt={dt:F5})");
                }
            }
        }
    }
}
