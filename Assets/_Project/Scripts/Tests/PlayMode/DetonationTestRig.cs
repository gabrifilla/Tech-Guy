using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>
    /// Shared scaffolding for the R3 (Detonation reacts to elements) property tests of
    /// modifier-synergies-theme17 (<see cref="RunSynergyEffects.Resolve"/> detonation branch).
    ///
    /// Builds a real <see cref="PlayerOnHitEffects"/> host (which lazily owns the
    /// <see cref="RunSynergyEffects"/> under test) plus box-collider "enemy" <see cref="Actor"/>s
    /// laid out in world space, and drives the death-explosion cascade through the effect's
    /// public/internal API only. <see cref="BurnStatus"/>/<see cref="ChillStatus"/> are
    /// <see cref="MonoBehaviour"/> components on an Actor, <c>Resolve</c> keys the explosion on
    /// <see cref="Actor.IsDead"/>, and the overlap/raycast reachability query is real physics, so
    /// these tests must run in PlayMode.
    ///
    /// A detonating enemy must be dead but still present (its components alive) when <c>Resolve</c>
    /// runs. <see cref="KillInPlace"/> drives the enemy's health to zero, which marks
    /// <c>IsDead</c> true and schedules a normal (deferred) <c>Destroy</c>; the synchronous
    /// <c>Resolve</c> that follows still sees the dead actor and its burn/chill components in the same
    /// frame. No <c>FindObjectOfType</c>/<c>GameObject.Find</c>/magic strings are used; every
    /// reference is created and held explicitly here. Carries no NUnit dependency so it can live in
    /// Assembly-CSharp alongside the other rigs.
    /// </summary>
    public sealed class DetonationTestRig
    {
        // BurnStatus.Tick is protected; invoked via reflection so a lone burn tick can be simulated
        // without running the StatusEffect Update lifecycle (Property 12 / R3.6).
        private static readonly MethodInfo BurnTickMethod =
            typeof(BurnStatus).GetMethod("Tick", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _spawned = new List<GameObject>();

        // RunBoons.Hooks has a private setter; assigned by reflection when a test opts in to a live
        // HookBus so RunSynergyEffects.Resolve raises real OnExplosion notifications (R8.8/R8.11).
        private static readonly System.Reflection.PropertyInfo HooksProperty =
            typeof(RunBoons).GetProperty("Hooks", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

        public GameObject Host { get; private set; }
        public PlayerOnHitEffects Effects { get; private set; }
        public RunSynergyEffects Synergies { get; private set; }
        public HookBus Hooks { get; private set; }

        /// <summary>The initial (seed) detonation target, exposed so a hook subscriber can re-enter the cascade with it.</summary>
        public Actor Seed { get; private set; }

        /// <summary>Records the seed actor a test detonates from (used by re-entrant hook subscribers).</summary>
        public void SetSeed(Actor seed) => Seed = seed;

        /// <summary>Builds the player-side host that owns the on-hit registry and cascade engine.</summary>
        public RunSynergyEffects Build()
        {
            Host = new GameObject("TestDetonationHost");
            _spawned.Add(Host);
            Effects = Host.AddComponent<PlayerOnHitEffects>();
            Synergies = Effects.Synergies; // lazily adds RunSynergyEffects to the same GameObject
            return Synergies;
        }

        /// <summary>
        /// Attaches a live <see cref="HookBus"/> that the host's cascade engine will use, so
        /// <see cref="RunSynergyEffects.Resolve"/> raises real OnExplosion notifications to
        /// subscribers (R8.8/R8.11). The bus is owned by a <see cref="RunBoons"/> placed on the same
        /// host — the same RunBoons-ownership path the production code resolves through
        /// (<c>TryGetComponent&lt;RunBoons&gt;().Hooks</c>) — with its private-setter <c>Hooks</c>
        /// property assigned by reflection.
        /// <para>
        /// <c>RunBoons</c> <see cref="RequireComponent"/>s <see cref="PlayerActor"/>/<c>AbilityHolder</c>,
        /// whose <c>Awake</c> boots heavy scene wiring. To keep the rig lightweight and deterministic
        /// the host is deactivated before these components are added and left inactive: no lifecycle
        /// callback runs, yet <see cref="RunSynergyEffects.Resolve"/> — a direct method call — still
        /// executes on the (inactive) component and resolves the bus through RunBoons ownership. The
        /// enemies built by the rig are independent, active GameObjects, so the physics overlap/raycast
        /// reachability the cascade relies on is unaffected by the host's active state.
        /// </para>
        /// </summary>
        public HookBus AttachHookBus()
        {
            if (HooksProperty == null) throw new System.InvalidOperationException("RunBoons.Hooks not found");
            Host.SetActive(false); // suppress PlayerActor/AbilityHolder Awake pulled in by RequireComponent
            RunBoons run = Host.GetComponent<RunBoons>();
            if (!run) run = Host.AddComponent<RunBoons>();
            Hooks = new HookBus();
            HooksProperty.SetValue(run, Hooks);
            return Hooks;
        }

        /// <summary>Grants <paramref name="ranks"/> Detonation ranks so dying enemies explode.</summary>
        public void AddDetonation(int ranks)
        {
            for (int i = 0; i < ranks; i++) Synergies.Add(RunSynergy.Detonation);
        }

        /// <summary>Grants <paramref name="ranks"/> Reactor ranks so burn scaling is active (R3.5).</summary>
        public void AddReactor(int ranks)
        {
            for (int i = 0; i < ranks; i++) Synergies.Add(RunSynergy.Reactor);
        }

        /// <summary>Grants <paramref name="ranks"/> Conductor ranks (used to prove death-only detonation, R3.6).</summary>
        public void AddConductor(int ranks)
        {
            for (int i = 0; i < ranks; i++) Synergies.Add(RunSynergy.Conductor);
        }

        /// <summary>Enables burn on hit so cascade-applied elements are observable.</summary>
        public void EnableBurn(float dps = 4f, float duration = 5f) => Effects.EnableBurn(dps, duration);

        /// <summary>The base Detonation explosion radius for the current Detonation rank (3 + 0.5*rank).</summary>
        public float BaseRadius => 3f + 0.5f * Synergies.Rank(RunSynergy.Detonation);

        /// <summary>The combustion radius (base * 1.3) for a burning-only death (R3.1).</summary>
        public float CombustionRadius => BaseRadius * 1.3f;

        /// <summary>Creates a live enemy Actor with a unit box collider at the given position.</summary>
        public Actor BuildEnemy(Vector3 position, float health = 1_000_000f)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            var actor = go.AddComponent<Actor>();
            actor.health = health;
            go.SetActive(true); // Awake -> maxHealth = health
            return actor;
        }

        /// <summary>Applies a burn directly to an enemy so a later death reads it as burning.</summary>
        public BurnStatus GiveBurn(Actor enemy, float dps = 4f, float duration = 5f)
        {
            BurnStatus.Apply(enemy, dps, duration);
            return enemy.GetComponent<BurnStatus>();
        }

        /// <summary>Applies a chill directly to an enemy so a later death reads it as frozen.</summary>
        public ChillStatus GiveChill(Actor enemy, float slowFraction = 0.5f, float duration = 5f)
        {
            ChillStatus.Apply(enemy, slowFraction, duration);
            return enemy.GetComponent<ChillStatus>();
        }

        public bool HasBurn(Actor enemy) => enemy.GetComponent<BurnStatus>();
        public bool HasChill(Actor enemy) => enemy.GetComponent<ChillStatus>();
        public int BurnCount(Actor enemy) => enemy.GetComponents<BurnStatus>().Length;

        /// <summary>
        /// Drives an enemy's health to zero so it becomes dead-but-present for the same-frame
        /// <c>Resolve</c>. The enemy keeps its burn/chill components until the deferred Destroy runs
        /// at end of frame, which is after the synchronous cascade under test.
        /// </summary>
        public void KillInPlace(Actor enemy)
        {
            if (!enemy || enemy.IsDead) return;
            enemy.TakeDamage(enemy.health);
        }

        /// <summary>Resolves the bounded cascade from a (typically dead) initial target.</summary>
        public void Resolve(Actor initial, float damage) => Synergies.Resolve(initial, damage, Effects);

        /// <summary>Simulates a single isolated burn tick (Property 12): calls BurnStatus.Tick directly.</summary>
        public void TickBurnOnce(BurnStatus burn, float deltaTime = 0.5f)
        {
            BurnTickMethod.Invoke(burn, new object[] { deltaTime });
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
