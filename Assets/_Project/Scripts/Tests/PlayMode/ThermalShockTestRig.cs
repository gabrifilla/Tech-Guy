using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>
    /// Shared scaffolding for the R2 (Ignite + Frost -> Thermal Shock) property tests of
    /// modifier-synergies-theme17.
    ///
    /// Builds a real <see cref="PlayerOnHitEffects"/> host and box-collider "enemy"
    /// <see cref="Actor"/>s, and drives the private Thermal Shock coordination in
    /// <c>ApplyElements</c>/<c>TriggerThermalShock</c> through the effect's public/internal API only.
    /// BurnStatus and ChillStatus are <see cref="MonoBehaviour"/> components applied to an Actor and
    /// Thermal Shock calls <c>Actor.TakeDamage</c>, so these tests must run in PlayMode where the
    /// component lifecycle and <c>GetComponent</c> lookups are real.
    ///
    /// The chill-chance roll is made deterministic through the internal
    /// <see cref="PlayerOnHitEffects.SetChillRollForTests"/> seam (a minimal, non-behavior-changing
    /// injectable RNG added for RNG-dependent properties, task 1.1 / R11.5). No
    /// <c>FindObjectOfType</c>/<c>GameObject.Find</c>/magic strings are used; every reference is
    /// created and held explicitly here. Carries no NUnit dependency so it can live in
    /// Assembly-CSharp alongside the projectile rig.
    /// </summary>
    public sealed class ThermalShockTestRig
    {
        // StatusEffect.RemainingTime is protected; read it via reflection to assert durations are
        // left unchanged by a Thermal Shock trigger (R2.5 / Property 7) without widening the API.
        private static readonly PropertyInfo RemainingTimeProp =
            typeof(StatusEffect).GetProperty("RemainingTime",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        private static readonly FieldInfo ThermalShockMultiplierField =
            typeof(PlayerOnHitEffects).GetField("_thermalShockMultiplier",
                BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public GameObject Host { get; private set; }
        public PlayerOnHitEffects Effects { get; private set; }

        /// <summary>Builds the player-side host that carries the on-hit element registry.</summary>
        public PlayerOnHitEffects BuildEffects()
        {
            Host = new GameObject("TestOnHitEffects");
            _spawned.Add(Host);
            Effects = Host.AddComponent<PlayerOnHitEffects>();
            return Effects;
        }

        /// <summary>Enables burn on every hit with a deterministic tick damage and duration.</summary>
        public void EnableBurn(float dps = 4f, float duration = 5f) => Effects.EnableBurn(dps, duration);

        /// <summary>
        /// Enables chill and forces the chance roll so frost always (chance 1) or never (chance 0)
        /// becomes active. slowFraction defaults below the freeze threshold so no NavMeshAgent is
        /// required for the status to apply.
        /// </summary>
        public void EnableChill(float chance, float slowFraction = 0.5f, float duration = 5f)
        {
            Effects.EnableChill(slowFraction, duration, Mathf.Clamp01(chance));
            // Deterministic roll: return 0 so "roll <= chance" is true iff chance > 0. With chance 0
            // the effect's own gate (_chillChance > 0 required by roll <= 0 only at exactly 0) plus
            // this 0-roll cleanly separates "always apply" (chance 1) from "never apply" (chance 0).
            Effects.SetChillRollForTests(() => 0f);
        }

        /// <summary>Routes the chill roll through a seeded source so a failed roll = no frost applied.</summary>
        public void SetChillRoll(Func<float> roll) => Effects.SetChillRollForTests(roll);

        /// <summary>Sets the configured Thermal Shock damage multiple.</summary>
        public void ConfigureThermalShock(float multiplier) => Effects.ConfigureThermalShock(multiplier);

        public float ThermalShockMultiplier => (float)ThermalShockMultiplierField.GetValue(Effects);

        /// <summary>Grants Resonance ranks on the host's RunSynergyEffects (Property 8).</summary>
        public RunSynergyEffects AddResonance(int ranks)
        {
            RunSynergyEffects synergies = Effects.Synergies;
            for (int i = 0; i < ranks; i++) synergies.Add(RunSynergy.Resonance);
            return synergies;
        }

        /// <summary>Creates a live enemy Actor with a box collider and generous health.</summary>
        public Actor BuildEnemy(float health = 1_000_000f, Vector3? position = null)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position ?? Vector3.zero;
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            var actor = go.AddComponent<Actor>();
            actor.health = health;
            go.SetActive(true); // Awake -> maxHealth = health
            return actor;
        }

        /// <summary>Applies a pre-existing burn directly (bypasses the on-hit registry) so a later frost meets it.</summary>
        public BurnStatus GiveBurn(Actor enemy, float dps = 4f, float duration = 5f)
        {
            BurnStatus.Apply(enemy, dps, duration);
            return enemy.GetComponent<BurnStatus>();
        }

        /// <summary>Applies a pre-existing chill directly so a later fire meets it.</summary>
        public ChillStatus GiveChill(Actor enemy, float slowFraction = 0.5f, float duration = 5f)
        {
            ChillStatus.Apply(enemy, slowFraction, duration);
            return enemy.GetComponent<ChillStatus>();
        }

        public bool HasBurn(Actor enemy) => enemy.GetComponent<BurnStatus>();
        public bool HasChill(Actor enemy) => enemy.GetComponent<ChillStatus>();

        /// <summary>Reads a status effect's remaining duration (R2.5 duration-unchanged checks).</summary>
        public float Remaining(StatusEffect status) => (float)RemainingTimeProp.GetValue(status);

        /// <summary>Destroys everything this rig created.</summary>
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go) UnityEngine.Object.DestroyImmediate(go);
            _spawned.Clear();
        }
    }
}
