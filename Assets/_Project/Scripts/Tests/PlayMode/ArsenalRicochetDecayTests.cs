using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode test for R1 Property 2 (ricochet damage decay) of modifier-synergies-theme17.
    /// Runs in PlayMode so real frames drive the projectile's physics sweep and its
    /// destroy-at-range lifecycle. Enemies are placed off the straight heading so every hit
    /// triggers a redirect (not a pierce), and the arrow's multiplier is sampled after each redirect.
    /// </summary>
    public sealed class ArsenalRicochetDecayTests
    {
        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 2: after k ricochet redirects the arrow's damage
        // multiplier equals its initial multiplier * 0.75^k.
        // Validates: Requirements 1.3
        [UnityTest]
        public IEnumerator Property2_MultiplierEqualsInitialTimesPointSevenFivePowK()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 2);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                float initialMultiplier = 0.5f + (float)rng.NextDouble() * 2.5f; // 0.5..3.0
                int budget = 1 + rng.Next(0, 4);                                 // 1..4 bounces available

                // Chain of enemies, each offset sideways from the previous so the arrow must redirect
                // (never pierce straight) to reach the next, all inside the 6-unit ricochet search.
                Vector3 pos = new Vector3(0f, 0f, 2f);
                for (int k = 0; k < budget + 1; k++)
                {
                    _rig.BuildEnemy(pos);
                    pos += new Vector3((k % 2 == 0) ? 3f : -3f, 0f, 2.4f);
                }

                ArsenalProjectile arrow = _rig.FireArrow(Vector3.zero, Vector3.forward, 80f,
                    piercing: false, ricochetBounces: budget, multiplier: initialMultiplier);
                Assert.IsNotNull(arrow, $"case {c}: arrow failed to spawn");

                int startBounces = _rig.Bounces(arrow);
                float lastMultiplier = _rig.Multiplier(arrow);
                int observedRedirects = 0;

                for (int frame = 0; frame < 400 && arrow; frame++)
                {
                    yield return null;
                    if (!arrow) break;

                    int consumed = startBounces - _rig.Bounces(arrow);
                    if (consumed > observedRedirects)
                    {
                        observedRedirects = consumed;
                        lastMultiplier = _rig.Multiplier(arrow);

                        // Invariant checked at every redirect: multiplier == initial * 0.75^consumed.
                        float expected = initialMultiplier * Mathf.Pow(0.75f, consumed);
                        Assert.LessOrEqual(Mathf.Abs(lastMultiplier - expected),
                            1e-3f * Mathf.Max(1f, Mathf.Abs(expected)),
                            $"case {c}: after {consumed} redirects multiplier={lastMultiplier}, expected {expected}");
                    }
                }

                Assert.Greater(observedRedirects, 0, $"case {c}: expected at least one ricochet redirect");
            }
        }
    }
}
