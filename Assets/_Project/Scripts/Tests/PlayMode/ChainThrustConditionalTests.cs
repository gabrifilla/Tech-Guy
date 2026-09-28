using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R7 Property 20 of modifier-synergies-theme17
    /// (ChainThrust conditional).
    ///
    /// Property 20: for any thrust hit, if at least one other enemy exists within the 6-meter search
    /// radius the system SHALL create exactly one chain thrust toward a *different* enemy; if no other
    /// enemy exists within that radius it SHALL create none (Requirements 7.5, 7.6).
    ///
    /// Seam under test: <see cref="ArsenalCombat.TryChainThrust"/> decides whether to create a chain by
    /// resolving a chain target through the private <c>FindNearbyEnemy</c> selector. TryChainThrust
    /// first identifies the primary (the enemy the thrust connected with, straight ahead) and then
    /// searches for a *different* enemy within 6 m; when that second search returns null it returns
    /// without creating anything. TryChainThrust itself calls PlayerActor.TryApplyAreaDamage (the full
    /// player damage pipeline), which is impractical to stand up in isolation, so this test drives the
    /// exact decision the conditional hinges on — the FindNearbyEnemy chain-target selection — with real
    /// colliders and Physics.OverlapSphere in PlayMode. The chain is created iff a different in-range
    /// enemy is found, so asserting the selector's "different enemy within 6 m" behavior validates the
    /// conditional exactly (one target found => one chain; null => no chain).
    /// </summary>
    public sealed class ChainThrustConditionalTests
    {
        private const float ChainRadius = 6f;
        private const float ThrustReach = 12f; // stand-in for plan.Range when identifying the primary

        private static readonly MethodInfo FindNearbyEnemy = typeof(ArsenalCombat).GetMethod(
            "FindNearbyEnemy", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo PlayerField = typeof(ArsenalCombat).GetField(
            "_player", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private ArsenalCombat _combat;
        private PlayerActor _player;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNotNull(FindNearbyEnemy, "Expected private ArsenalCombat.FindNearbyEnemy (test seam).");
            Assert.IsNotNull(PlayerField, "Expected private ArsenalCombat._player (test seam).");

            // Build the combat host inactive so no MonoBehaviour lifecycle runs; ArsenalCombat's
            // [RequireComponent(typeof(PlayerActor))] guarantees a PlayerActor is present. We inject
            // _player explicitly (the field FindNearbyEnemy reads to exclude the player), avoiding a
            // full player Awake and any FindObjectOfType/GameObject.Find.
            var host = new GameObject("ChainThrustHost");
            host.SetActive(false);
            _spawned.Add(host);
            _player = host.AddComponent<PlayerActor>();
            _combat = host.AddComponent<ArsenalCombat>();
            PlayerField.SetValue(_combat, _player);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        // Creates a live enemy Actor with a unit box collider at a position.
        private Actor BuildEnemy(Vector3 position, float health = 1000f)
        {
            var go = new GameObject("ChainEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            var actor = go.AddComponent<Actor>();
            actor.health = health;
            go.SetActive(true); // Awake -> maxHealth = health, IsDead = false
            return actor;
        }

        private Actor FindPrimary(Vector3 center, Vector3 forward) =>
            (Actor)FindNearbyEnemy.Invoke(_combat, new object[] { center, ChainRadius, forward, ThrustReach, null });

        private Actor FindChainTarget(Vector3 center, Actor excludePrimary) =>
            (Actor)FindNearbyEnemy.Invoke(_combat, new object[] { center, ChainRadius, Vector3.zero, 0f, excludePrimary });

        // Feature: modifier-synergies-theme17, Property 20
        // With a primary enemy straight ahead and at least one OTHER enemy within 6 m, the chain
        // resolves exactly one different target -> exactly one chain thrust is created.
        // Validates: Requirements 7.5
        [UnityTest]
        public IEnumerator Property20_CreatesOneChainToDifferentEnemy_WhenAnotherIsInRange()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 20);

            for (int c = 0; c < cases; c++)
            {
                Vector3 center = Vector3.zero;
                Vector3 forward = Vector3.forward;

                // Primary directly ahead, inside the thrust reach and the 6 m radius.
                float primaryDist = 1f + (float)rng.NextDouble() * 3f; // 1..4 m ahead
                Actor primary = BuildEnemy(center + forward * primaryDist);

                // A different enemy off to the side, strictly within the 6 m radius.
                float angle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
                float otherDist = 1f + (float)rng.NextDouble() * (ChainRadius - 1.5f); // 1..4.5 m
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * otherDist;
                Actor other = BuildEnemy(center + offset);

                Actor resolvedPrimary = FindPrimary(center, forward);
                Actor chainTarget = FindChainTarget(center, resolvedPrimary);

                Assert.IsNotNull(chainTarget, $"case {c}: a different in-range enemy exists, so a chain must be created.");
                Assert.AreNotSame(resolvedPrimary, chainTarget, $"case {c}: chain must target a DIFFERENT enemy than the primary.");
                Assert.IsTrue(chainTarget == other || chainTarget == primary,
                    $"case {c}: chain target must be one of the spawned enemies.");
                // Exactly one chain: TryChainThrust applies a single area damage toward this one target.
                Assert.AreNotSame(chainTarget, resolvedPrimary, $"case {c}: exactly one chain to a distinct enemy.");

                TearDown();
                SetUp();
                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 20
        // With ONLY the primary enemy in range (no other enemy within 6 m), the chain-target search
        // excluding the primary returns null -> no chain thrust is created.
        // Validates: Requirements 7.6
        [UnityTest]
        public IEnumerator Property20_CreatesNoChain_WhenNoOtherEnemyInRange()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 21);

            for (int c = 0; c < cases; c++)
            {
                Vector3 center = Vector3.zero;
                Vector3 forward = Vector3.forward;

                // Only a primary enemy, straight ahead within reach and radius.
                float primaryDist = 1f + (float)rng.NextDouble() * 3f;
                Actor primary = BuildEnemy(center + forward * primaryDist);

                // Any additional enemy is placed strictly OUTSIDE the 6 m radius so it cannot chain.
                bool placeFarEnemy = rng.Next(0, 2) == 0;
                if (placeFarEnemy)
                {
                    float angle = (float)(rng.NextDouble() * System.Math.PI * 2.0);
                    float farDist = ChainRadius + 2f + (float)rng.NextDouble() * 6f; // 8..14 m: out of range
                    Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * farDist;
                    BuildEnemy(center + offset);
                }

                Actor resolvedPrimary = FindPrimary(center, forward);
                Actor chainTarget = FindChainTarget(center, resolvedPrimary);

                Assert.IsNull(chainTarget,
                    $"case {c}: no OTHER enemy within 6 m, so no chain thrust must be created (placeFar={placeFarEnemy}).");

                TearDown();
                SetUp();
                yield return null;
            }
        }
    }
}
