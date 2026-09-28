using System.Collections.Generic;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>
    /// Shared scaffolding for the R8 (generic combat-event <see cref="HookBus"/>) tests of
    /// modifier-synergies-theme17.
    ///
    /// <see cref="HookBus"/> is a plain C# class, but several of its events carry an
    /// <see cref="Actor"/> (a <see cref="MonoBehaviour"/>). Creating an Actor runs its
    /// <c>Awake</c>, which adds companion components and touches the scene-level target UI, so the
    /// component lifecycle and <c>GetComponent</c> lookups must be real. That is why the
    /// Actor-carrying HookBus tests live in PlayMode and build enemies through this rig rather than
    /// in a pure EditMode context.
    ///
    /// Every enemy is created and held explicitly here — no <c>FindObjectOfType</c>/
    /// <c>GameObject.Find</c>/magic strings — and torn down deterministically. The rig carries no
    /// NUnit dependency so it can live in Assembly-CSharp alongside the other PlayMode rigs.
    /// </summary>
    public sealed class HookBusTestRig
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        /// <summary>
        /// Creates a live enemy <see cref="Actor"/> with a box collider and generous health so it
        /// stays alive unless a test deliberately kills it. Distinct calls yield distinct Actor
        /// references, which is what OnKill dedupe (Property 21) keys on.
        /// </summary>
        public Actor BuildEnemy(float health = 1_000_000f, Vector3? position = null)
        {
            var go = new GameObject("HookBusTestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position ?? Vector3.zero;
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            var actor = go.AddComponent<Actor>();
            actor.health = health;
            go.SetActive(true); // Awake -> maxHealth = health, IsDead = false
            return actor;
        }

        /// <summary>Destroys everything this rig created.</summary>
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go) UnityEngine.Object.DestroyImmediate(go);
            _spawned.Clear();
        }
    }
}
