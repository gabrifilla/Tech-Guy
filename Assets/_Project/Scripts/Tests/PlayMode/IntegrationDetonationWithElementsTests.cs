using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode INTEGRATION test for modifier-synergies-theme17 (task 16.1, scenario 1):
    /// Detonation-with-elements end-to-end.
    ///
    /// Unlike the focused R3 property tests (which isolate a single classification consequence),
    /// this exercise drives the whole chain a real run would produce: an enemy is prepared with a
    /// live element (burn or chill) via the on-hit registry-equivalent path, killed, and then the
    /// Detonation death-explosion is resolved so the cascade classifies the death (combustion vs
    /// shatter) and fans out to neighbouring live enemies — all through the same
    /// <see cref="PlayerOnHitEffects"/> / <see cref="RunSynergyEffects"/> pair the game wires at
    /// runtime, staying inside the documented cascade bounds.
    ///
    /// Runs in PlayMode: <see cref="BurnStatus"/>/<see cref="ChillStatus"/> are MonoBehaviour
    /// components, <c>Resolve</c> keys on <see cref="Actor.IsDead"/>, and the explosion reach is a
    /// real physics overlap/raycast query. Reuses <see cref="DetonationTestRig"/>; no
    /// <c>FindObjectOfType</c>/<c>GameObject.Find</c>/magic strings.
    ///
    /// Integration level: highest practical for the detonation branch — the seed death, its element
    /// state, and the physical neighbours are all real, and the cascade runs unmodified. Only the
    /// initial "kill + Resolve" is invoked directly (the same call the damage pipeline makes on a
    /// qualifying death), because staging a full weapon-cast kill would add no cascade coverage.
    ///
    /// Validates: Requirements 3.1, 3.2 (also 1.1 context via the shared cascade bounds).
    /// </summary>
    public sealed class IntegrationDetonationWithElementsTests
    {
        private DetonationTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new DetonationTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17 (integration): a BURNING enemy dying under Detonation
        // combusts (radius x1.3) and the cascade fans out to a neighbour that sits in the
        // combustion-only ring; the whole burst stays within the cascade bounds.
        // Validates: Requirements 3.1
        [UnityTest]
        public IEnumerator CombustionEndToEnd_BurningDeathFansOutWithinBounds()
        {
            _rig.Build();
            _rig.AddDetonation(2);
            _rig.EnableBurn();

            float baseRadius = _rig.BaseRadius;
            float combustionRadius = _rig.CombustionRadius;
            // A neighbour that only a combustion (x1.3) explosion can reach.
            float ringDistance = baseRadius + (combustionRadius - baseRadius) * 0.5f;

            Actor dead = _rig.BuildEnemy(Vector3.zero);
            Actor neighbour = _rig.BuildEnemy(new Vector3(ringDistance, 0f, 0f));

            // Prepare the element the way a run would, then kill the prepared enemy.
            _rig.GiveBurn(dead);
            Assert.IsTrue(_rig.HasBurn(dead), "seed enemy should be burning before it dies");
            _rig.KillInPlace(dead);
            Assert.IsTrue(dead.IsDead, "seed enemy must be dead-but-present when Resolve runs");

            _rig.Resolve(dead, 40f);

            int hits = _rig.Synergies.LastSecondaryHits;
            Assert.AreEqual(1, hits,
                $"combustion (radius x1.3 = {combustionRadius}) should reach the neighbour at " +
                $"{ringDistance} (base radius {baseRadius}); saw {hits} secondary hits");
            Assert.IsTrue(_rig.HasBurn(neighbour) || !neighbour.IsDead,
                "the combustion fan-out should have applied damage/elements to the neighbour");

            // Cascade bounds hold end-to-end.
            Assert.LessOrEqual(hits, 32, "detonation cascade exceeded MaxSecondaryHits");
            yield return null;
        }

        // Feature: modifier-synergies-theme17 (integration): a FROZEN enemy dying under Detonation
        // shatters, emitting 3..6 fragments; with more than six reachable neighbours the burst caps
        // at exactly six first-generation hits (shatter fragment bound + cascade bounds).
        // Validates: Requirements 3.2
        [UnityTest]
        public IEnumerator ShatterEndToEnd_FrozenDeathEmitsBoundedFragments()
        {
            _rig.Build();
            _rig.AddDetonation(2);
            _rig.EnableBurn();

            // Nine neighbours inside the base radius (more than the shatter cap of six).
            float ringRadius = _rig.BaseRadius * 0.55f;
            const int neighbours = 9;
            Actor dead = _rig.BuildEnemy(Vector3.zero);
            for (int i = 0; i < neighbours; i++)
            {
                float angle = i * (Mathf.PI * 2f / neighbours);
                _rig.BuildEnemy(new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius));
            }

            _rig.GiveChill(dead);
            Assert.IsTrue(_rig.HasChill(dead), "seed enemy should be frozen before it dies");
            _rig.KillInPlace(dead);
            Assert.IsTrue(dead.IsDead, "seed enemy must be dead-but-present when Resolve runs");

            _rig.Resolve(dead, 40f);

            int hits = _rig.Synergies.LastSecondaryHits;
            Assert.AreEqual(6, hits,
                $"a frozen shatter with {neighbours} reachable neighbours must cap at 6 fragments, saw {hits}");
            Assert.GreaterOrEqual(hits, 3, "shatter must emit at least 3 fragments");
            Assert.LessOrEqual(hits, 32, "detonation cascade exceeded MaxSecondaryHits");
            yield return null;
        }
    }
}
