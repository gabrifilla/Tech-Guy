using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode test for R1 Property 3 (no double hit per projectile) of modifier-synergies-theme17.
    /// For any pierce/ricochet combination and any generated enemy layout, a single projectile must
    /// never damage the same enemy twice (the projectile's <c>_hit</c> set is the single source of
    /// truth). Runs in PlayMode because hit resolution is a real physics sweep across frames.
    /// </summary>
    public sealed class ArsenalNoDoubleHitTests
    {
        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 3: a single projectile damages each enemy at most
        // once, for any pierce/ricochet combination.
        // Validates: Requirements 1.6
        [UnityTest]
        public IEnumerator Property3_EachEnemyDamagedAtMostOnce()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 3);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();

                bool piercing = rng.Next(0, 2) == 0;
                int bounces = rng.Next(0, 4); // 0..3

                // Mixed layout: some collinear (pierce candidates) plus a nearby cluster (ricochet
                // candidates). Enemies are given large health so none dies mid-flight.
                int inlineCount = rng.Next(0, 4);
                for (int i = 0; i < inlineCount; i++)
                    _rig.BuildEnemy(new Vector3(0f, 0f, 2f + i * 1.6f));

                int clusterCount = rng.Next(0, 5);
                for (int i = 0; i < clusterCount; i++)
                {
                    float ang = (float)(rng.NextDouble() * System.Math.PI * 2.0);
                    float rad = 1.5f + (float)rng.NextDouble() * 4f;
                    var p = new Vector3(Mathf.Cos(ang) * rad, 0f, 3f + Mathf.Sin(ang) * rad + i * 0.3f);
                    _rig.BuildEnemy(p);
                }

                if (inlineCount + clusterCount == 0)
                    _rig.BuildEnemy(new Vector3(0f, 0f, 2f)); // guarantee at least one target

                ArsenalProjectile arrow = _rig.FireArrow(Vector3.zero, Vector3.forward, 70f, piercing, bounces);
                Assert.IsNotNull(arrow, $"case {c}: arrow failed to spawn");

                for (int frame = 0; frame < 400 && arrow; frame++)
                    yield return null;

                // No enemy appears more than once across all recorded damage events.
                var seen = new HashSet<Actor>();
                foreach (Actor damaged in _rig.DamageEvents)
                    Assert.IsTrue(seen.Add(damaged),
                        $"case {c}: enemy damaged more than once (piercing={piercing}, bounces={bounces})");
            }
        }
    }
}
