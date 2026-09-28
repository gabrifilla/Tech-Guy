using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode INTEGRATION test for modifier-synergies-theme17 (task 16.1, scenario 3):
    /// pierce-then-ricochet on a generated layout.
    ///
    /// This composes the R1 anti-synergy fix end-to-end on a single arrow that has BOTH Piercing and
    /// Ricochet: it must pierce every in-line enemy first (no bounce consumed while an un-hit enemy
    /// lies on its straight heading) and only then ricochet toward off-axis enemies — with the
    /// no-double-hit guarantee holding across the whole flight. The focused R1 property tests isolate
    /// each rule; this integration test drives the full pierce -> ricochet handoff on a generated
    /// in-line-plus-off-axis layout across real frames.
    ///
    /// Integration level: full — the real <see cref="ArsenalProjectile"/> flies through a real
    /// physics sweep across live colliders; only the pierce/ricochet knobs (which come from run
    /// modifiers in production) are set on the spawned arrow via the rig, exactly as the R1 property
    /// tests do.
    ///
    /// Runs in PlayMode: hit resolution is a real <c>Physics.SphereCastAll</c> sweep across frames.
    /// Reuses <see cref="ArsenalProjectileTestRig"/>; no <c>FindObjectOfType</c>/<c>GameObject.Find</c>/
    /// magic strings.
    ///
    /// Validates: Requirements 1.1 (also 3.1/3.2 share the cascade-bound guarantees exercised elsewhere).
    /// </summary>
    public sealed class IntegrationPierceThenRicochetTests
    {
        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17 (integration): on a generated in-line-plus-off-axis
        // layout, a Piercing+Ricochet arrow pierces every in-line enemy WITHOUT consuming a bounce,
        // then ricochets to reach an off-axis enemy — and never double-hits any enemy.
        // Validates: Requirements 1.1
        [UnityTest]
        public IEnumerator PierceThenRicochet_PiercesInlineFirstThenRedirects()
        {
            const int cases = 30; // integration sweep over generated layouts
            var rng = new System.Random(PropertyCheck.DefaultSeed + 161);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                int inlineCount = 2 + rng.Next(0, 4);     // 2..5 collinear enemies
                int startingBounces = 1 + rng.Next(0, 3);  // 1..3 ricochet bounces
                float spacing = 1.5f + (float)rng.NextDouble();

                Vector3 direction = Vector3.forward;
                var inline = new List<Actor>();
                for (int i = 0; i < inlineCount; i++)
                    inline.Add(_rig.BuildEnemy(new Vector3(0f, 0f, 2f + i * spacing)));

                // An off-axis enemy reachable only by ricocheting after the in-line run finishes,
                // placed inside the 6-unit ricochet search radius of the last in-line enemy.
                float lastZ = 2f + (inlineCount - 1) * spacing;
                Actor offAxis = _rig.BuildEnemy(new Vector3(3.5f, 0f, lastZ + 1.5f));

                ArsenalProjectile arrow = _rig.FireArrow(Vector3.zero, direction, 70f,
                    piercing: true, ricochetBounces: startingBounces);
                Assert.IsNotNull(arrow, $"case {c}: arrow failed to spawn");

                int bouncesWhileInlineRemained = startingBounces;
                int framesInlineExhausted = -1;

                for (int frame = 0; frame < 400 && arrow; frame++)
                {
                    yield return null;
                    if (!arrow) break;

                    if (framesInlineExhausted < 0 && AllHit(_rig.HitSet(arrow), inline))
                    {
                        framesInlineExhausted = frame;
                        bouncesWhileInlineRemained = _rig.Bounces(arrow);
                    }
                }

                // Pierce-first: no bounce consumed while in-line enemies still remained un-hit.
                Assert.AreEqual(startingBounces, bouncesWhileInlineRemained,
                    $"case {c}: a bounce was consumed before all in-line enemies were pierced");

                // Every in-line enemy was pierced.
                foreach (Actor e in inline)
                    Assert.IsTrue(_rig.DamageEvents.Contains(e),
                        $"case {c}: an in-line enemy was not pierced");

                // If the off-axis enemy was reached, it was only after the in-line pierce phase (ricochet).
                if (_rig.DamageEvents.Contains(offAxis))
                    Assert.GreaterOrEqual(framesInlineExhausted, 0,
                        $"case {c}: off-axis enemy hit before the in-line pierce phase finished");

                // No-double-hit holds across the entire pierce+ricochet flight.
                var seen = new HashSet<Actor>();
                foreach (Actor damaged in _rig.DamageEvents)
                    Assert.IsTrue(seen.Add(damaged),
                        $"case {c}: an enemy was damaged more than once during pierce+ricochet");
            }
        }

        // Feature: modifier-synergies-theme17 (integration): after the in-line pierce phase, entering
        // ricochet decays the arrow's damage multiplier by 0.75 per redirect — confirming the pierce
        // -> ricochet handoff yields a genuine ricochet, not a continued pierce.
        // Validates: Requirements 1.1
        [UnityTest]
        public IEnumerator PierceThenRicochet_RicochetPhaseDecaysMultiplier()
        {
            _rig.BuildOwner();

            // Two in-line enemies to pierce, then an off-axis enemy to ricochet toward.
            _rig.BuildEnemy(new Vector3(0f, 0f, 2f));
            _rig.BuildEnemy(new Vector3(0f, 0f, 4f));
            _rig.BuildEnemy(new Vector3(3f, 0f, 6f)); // off-axis ricochet target within 6 units

            const float initial = 1f;
            ArsenalProjectile arrow = _rig.FireArrow(Vector3.zero, Vector3.forward, 70f,
                piercing: true, ricochetBounces: 2, multiplier: initial);
            Assert.IsNotNull(arrow, "arrow failed to spawn");

            int startBounces = _rig.Bounces(arrow);
            bool sawRedirect = false;

            for (int frame = 0; frame < 400 && arrow; frame++)
            {
                yield return null;
                if (!arrow) break;

                int consumed = startBounces - _rig.Bounces(arrow);
                if (consumed > 0)
                {
                    sawRedirect = true;
                    float expected = initial * Mathf.Pow(0.75f, consumed);
                    Assert.LessOrEqual(Mathf.Abs(_rig.Multiplier(arrow) - expected),
                        1e-3f * Mathf.Max(1f, Mathf.Abs(expected)),
                        $"after {consumed} redirects the multiplier should be {expected}");
                }
            }

            Assert.IsTrue(sawRedirect,
                "the arrow should ricochet (consume a bounce) after finishing the in-line pierce phase");
        }

        private static bool AllHit(HashSet<Actor> hitSet, List<Actor> targets)
        {
            foreach (Actor t in targets)
                if (!hitSet.Contains(t)) return false;
            return true;
        }
    }
}
