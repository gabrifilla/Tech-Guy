using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode tests for R1 Property 1 (pierce-first ordering) of modifier-synergies-theme17.
    /// These run in PlayMode because <see cref="ArsenalProjectile"/> resolves hits with a real
    /// <c>Physics.SphereCastAll</c> sweep across live colliders, which cannot be exercised in EditMode.
    /// </summary>
    public sealed class ArsenalPierceFirstOrderingTests
    {
        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 1: WHILE an arrow has piercing and >=1 remaining
        // bounce, it damages and continues straight through every in-line un-hit enemy WITHOUT consuming
        // a bounce, and redirects (consuming exactly one bounce) only when none remain in-line.
        // Validates: Requirements 1.1, 1.2, 1.4
        [UnityTest]
        public IEnumerator Property1_PiercesAllInlineBeforeConsumingAnyBounce()
        {
            // A seeded loop of generated collinear layouts. Physics + frame stepping makes each case a
            // short coroutine, so the >=100 generated cases are driven here rather than via PropertyCheck.
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                int inlineCount = 2 + rng.Next(0, 4);       // 2..5 enemies on the straight heading
                int startingBounces = 1 + rng.Next(0, 3);   // 1..3 ricochet bounces available
                float spacing = 1.5f + (float)rng.NextDouble();

                var direction = Vector3.forward;
                var inline = new List<Actor>();
                for (int i = 0; i < inlineCount; i++)
                    inline.Add(_rig.BuildEnemy(new Vector3(0f, 0f, 2f + i * spacing)));

                // One off-axis enemy the arrow can only reach by ricocheting after the in-line run ends.
                Actor offAxis = _rig.BuildEnemy(new Vector3(4f, 0f, 2f + inlineCount * spacing + 1f));

                ArsenalProjectile arrow = _rig.FireArrow(Vector3.zero, direction, 60f, piercing: true, ricochetBounces: startingBounces);
                Assert.IsNotNull(arrow, $"case {c}: arrow failed to spawn");

                int bouncesWhileInlineRemained = startingBounces;
                int framesInlineExhausted = -1;

                // Step frames until the arrow is gone or its range is spent.
                for (int frame = 0; frame < 400 && arrow; frame++)
                {
                    yield return null; // one Update tick (physics sweep + hit resolution)

                    if (!arrow) break;

                    bool allInlineHit = AllHit(_rig.HitSet(arrow), inline);
                    if (allInlineHit && framesInlineExhausted < 0)
                    {
                        framesInlineExhausted = frame;
                        bouncesWhileInlineRemained = _rig.Bounces(arrow);
                    }
                }

                // R1.1/R1.4: no bounce was consumed while un-hit in-line enemies still remained.
                Assert.AreEqual(startingBounces, bouncesWhileInlineRemained,
                    $"case {c}: a ricochet bounce was consumed before every in-line enemy was pierced");

                // Every in-line enemy took damage (pierced through).
                foreach (Actor e in inline)
                    Assert.IsTrue(_rig.DamageEvents.Contains(e),
                        $"case {c}: an in-line enemy was not pierced");

                // R1.2/R1.4: the off-axis enemy is only reachable after the in-line run, via ricochet.
                // If it was hit at all, that only happened once the in-line phase had finished.
                if (_rig.DamageEvents.Contains(offAxis))
                    Assert.GreaterOrEqual(framesInlineExhausted, 0,
                        $"case {c}: off-axis enemy hit without finishing the in-line pierce phase");
            }
        }

        private static bool AllHit(HashSet<Actor> hitSet, List<Actor> targets)
        {
            foreach (Actor t in targets)
                if (!hitSet.Contains(t)) return false;
            return true;
        }
    }
}
