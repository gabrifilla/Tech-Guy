using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>
    /// Shared scaffolding for the R1 (Piercing + Ricochet) tests of modifier-synergies-theme17.
    ///
    /// Builds a minimal but real combat setup — a <see cref="PlayerActor"/> owner with deterministic
    /// stats, box-collider "enemy" <see cref="Actor"/>s, and access to the private state of the
    /// <see cref="ArsenalProjectile"/> under test via reflection (the projectile keeps its hit set,
    /// bounce count, and damage multiplier private, and the spec forbids widening gameplay APIs just
    /// for tests). No <c>FindObjectOfType</c>/<c>GameObject.Find</c>/magic strings are used; every
    /// reference is created and held explicitly here.
    ///
    /// This helper carries no NUnit dependency so it can live in Assembly-CSharp and be reused by
    /// both the EditMode and PlayMode test assemblies.
    /// </summary>
    public sealed class ArsenalProjectileTestRig
    {
        private static readonly FieldInfo HitField =
            typeof(ArsenalProjectile).GetField("_hit", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo BouncesField =
            typeof(ArsenalProjectile).GetField("_bounces", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo MultiplierField =
            typeof(ArsenalProjectile).GetField("_multiplier", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PiercingField =
            typeof(ArsenalProjectile).GetField("_piercing", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo RemainingField =
            typeof(ArsenalProjectile).GetField("_remaining", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo HomingField =
            typeof(ArsenalProjectile).GetField("_homing", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo NextSeekField =
            typeof(ArsenalProjectile).GetField("_nextSeek", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo FindNextTargetMethod =
            typeof(ArsenalProjectile).GetMethod("FindNextTarget", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _spawned = new List<GameObject>();

        public PlayerActor Owner { get; private set; }
        public WeaponScript Weapon { get; private set; }

        /// <summary>Records every damage instance applied to any enemy in this rig.</summary>
        public readonly List<Actor> DamageEvents = new List<Actor>();

        /// <summary>Builds the owner player with deterministic, crit-free stats and an equipped bow-like weapon.</summary>
        public PlayerActor BuildOwner(WeaponRunModifiers modifiers = null)
        {
            // Deterministic weapon: fixed damage, no crit variance downstream.
            Weapon = ScriptableObject.CreateInstance<WeaponScript>();
            Weapon.weaponName = "TestBow";
            Weapon.attackDamage = 10f;

            var ownerGo = new GameObject("TestPlayer");
            ownerGo.SetActive(false); // configure serialized fields before Awake runs
            _spawned.Add(ownerGo);

            var hand = new GameObject("Hand");
            hand.transform.SetParent(ownerGo.transform);

            Owner = ownerGo.AddComponent<PlayerActor>();
            SetPrivate(Owner, "handTransform", hand.transform);
            SetPrivate(Owner, "startingWeapon", Weapon);
            SetPrivate(Owner, "weapon", Weapon);

            // Crit-free, flat damage so every hit deals a known amount.
            var stats = new PlayerArpgStats
            {
                baseDamage = 0f,
                criticalChance = 0f,
                damageMultiplier = 1f,
                increasedDamagePercent = 0f,
                flatDamageBonus = 0f,
            };
            SetPrivate(Owner, "stats", stats);

            ownerGo.transform.position = Vector3.zero;
            ownerGo.SetActive(true); // Awake -> EquipWeapon(Weapon)

            // Force the equipped weapon to the deterministic test weapon regardless of saved loadout.
            Owner.EquipWeapon(Weapon);
            return Owner;
        }

        /// <summary>Creates a live enemy Actor with a box collider at the given position.</summary>
        public Actor BuildEnemy(Vector3 position, float health = 1000f, Vector3? size = null)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider>();
            col.size = size ?? Vector3.one;

            var actor = go.AddComponent<Actor>();
            actor.health = health;
            go.SetActive(true); // Awake -> maxHealth = health

            actor.DamageReceived += (a, _) => DamageEvents.Add(a);
            return actor;
        }

        /// <summary>Creates a solid (non-Actor) wall collider used to test scenery blocking.</summary>
        public GameObject BuildWall(Vector3 position, Vector3 size)
        {
            var go = new GameObject("TestWall");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider>();
            col.size = size;
            go.SetActive(true);
            return go;
        }

        /// <summary>Spawns an arrow and returns the single created <see cref="ArsenalProjectile"/>.</summary>
        public ArsenalProjectile FireArrow(Vector3 origin, Vector3 direction, float range, bool piercing, int ricochetBounces, float multiplier = 1f)
        {
            // Fire the real projectile, then locate the newly spawned "Energy arrow" (the static Fire
            // does not return a handle) and tune the R1 knobs (bounce count / multiplier) that come
            // from run modifiers in production. The rig fires without TwinShot, so exactly one spawns.
            var before = new HashSet<ArsenalProjectile>(Object.FindObjectsByType<ArsenalProjectile>());
            ArsenalProjectile.Fire(Owner, origin, direction, Weapon.attackDamage, multiplier, range, piercing, Color.white);
            ArsenalProjectile arrow = null;
            foreach (ArsenalProjectile candidate in Object.FindObjectsByType<ArsenalProjectile>())
                if (!before.Contains(candidate)) { arrow = candidate; break; }
            if (arrow)
            {
                BouncesField.SetValue(arrow, ricochetBounces);
                MultiplierField.SetValue(arrow, multiplier);
                PiercingField.SetValue(arrow, piercing);
                _spawned.Add(arrow.gameObject);
            }
            return arrow;
        }

        /// <summary>
        /// Spawns a single homing arrow along <paramref name="direction"/> for the R4 fan-then-home tests.
        ///
        /// The production <see cref="ArsenalProjectile.Fire"/> only sets <c>_homing</c> when the owner
        /// carries a Homing run modifier and only fans arrows when TwinShot is present; the rig instead
        /// fires one arrow through the same static <c>Fire</c> path (so the launch heading and
        /// <c>_nextSeek</c> scheduling come from the real <c>FireSingle</c>) and then flips <c>_homing</c>
        /// on via reflection. This mirrors how <see cref="FireArrow"/> tunes the private R1 knobs, and
        /// keeps the launch-frame heading exactly equal to the assigned fan heading.
        /// </summary>
        public ArsenalProjectile FireHomingArrow(Vector3 origin, Vector3 direction, float range)
        {
            var before = new HashSet<ArsenalProjectile>(Object.FindObjectsByType<ArsenalProjectile>());
            ArsenalProjectile.Fire(Owner, origin, direction, Weapon.attackDamage, 1f, range, false, Color.white);
            ArsenalProjectile arrow = null;
            foreach (ArsenalProjectile candidate in Object.FindObjectsByType<ArsenalProjectile>())
                if (!before.Contains(candidate)) { arrow = candidate; break; }
            if (arrow)
            {
                HomingField.SetValue(arrow, true);
                _spawned.Add(arrow.gameObject);
            }
            return arrow;
        }

        /// <summary>Invokes the projectile's private per-arrow seek from its own position/forward,
        /// exactly as the homing <c>Update</c> does (radius 7, forward cone on).</summary>
        public Actor FindNextTarget(ArsenalProjectile arrow, Vector3 origin, float radius, bool forwardOnly) =>
            (Actor)FindNextTargetMethod.Invoke(arrow, new object[] { origin, radius, forwardOnly });

        // --- private-state accessors for the projectile under test ---
        public int Bounces(ArsenalProjectile arrow) => (int)BouncesField.GetValue(arrow);
        public void SetNextSeek(ArsenalProjectile arrow, float t) => NextSeekField.SetValue(arrow, t);
        public float Multiplier(ArsenalProjectile arrow) => (float)MultiplierField.GetValue(arrow);
        public bool Piercing(ArsenalProjectile arrow) => (bool)PiercingField.GetValue(arrow);
        public float Remaining(ArsenalProjectile arrow) => (float)RemainingField.GetValue(arrow);

        public HashSet<Actor> HitSet(ArsenalProjectile arrow) => (HashSet<Actor>)HitField.GetValue(arrow);

        public void SetRemaining(ArsenalProjectile arrow, float remaining) => RemainingField.SetValue(arrow, remaining);
        public void SetMultiplier(ArsenalProjectile arrow, float multiplier) => MultiplierField.SetValue(arrow, multiplier);
        public void SetBounces(ArsenalProjectile arrow, int bounces) => BouncesField.SetValue(arrow, bounces);
        public void SetPiercing(ArsenalProjectile arrow, bool piercing) => PiercingField.SetValue(arrow, piercing);

        /// <summary>Destroys everything this rig created.</summary>
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go) Object.DestroyImmediate(go);
            _spawned.Clear();
            if (Weapon) Object.DestroyImmediate(Weapon);
            DamageEvents.Clear();
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null) fi.SetValue(target, value);
        }
    }
}
