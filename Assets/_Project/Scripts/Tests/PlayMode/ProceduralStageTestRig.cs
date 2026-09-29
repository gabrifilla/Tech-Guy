using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.AI;

namespace TechGuy.Tests
{
    /// <summary>
    /// Shared scaffolding for the procedural-stage-room-generation PlayMode integration tests
    /// (tasks 10.1-10.4). It builds the minimal-but-real scene pieces those tests need — a baked
    /// flat NavMesh (mirroring <c>ArchetypeIntegrationTests.TryBakeNavMesh</c> /
    /// <c>EnemyPhysicalAttackTests</c>), an enemy prefab template carrying the real
    /// <see cref="Actor"/> + <see cref="EnemyAI"/> + <see cref="EnemyVariant"/> the resolver spawns
    /// and configures, an <see cref="EnemyRespawnPoint"/> driven through its additive
    /// <c>ConfigurePrefab</c> initializer, a <see cref="PlayerActor"/> host and
    /// <see cref="EnemyArchetype"/> assets assembled by reflection (the archetype fields are
    /// serialized-private, exactly as <c>ArchetypeIntegrationTests</c> authors them).
    /// <para>
    /// No <c>GameObject.Find</c> / <c>FindObjectOfType</c> / magic strings are used; every
    /// reference is created and held explicitly. The rig carries no NUnit dependency so it can be
    /// reused across the four test files. Callers own the assertions and the NavMesh-availability
    /// fallbacks documented on each test.
    /// </para>
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Tasks: 10.1, 10.2, 10.3, 10.4.</remarks>
    public sealed class ProceduralStageTestRig
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<Object> _assets = new List<Object>();
        private NavMeshData _navMesh;
        private NavMeshDataInstance _navMeshInstance;

        /// <summary>True once <see cref="TryBakeNavMesh"/> has produced a valid runtime NavMesh.</summary>
        public bool HasNavMesh { get; private set; }

        /// <summary>
        /// Bakes a flat 60x60 NavMesh at y=0 the same way <c>ArchetypeIntegrationTests</c> does so
        /// agents placed on it report <c>isOnNavMesh</c> and <c>NavMesh.SamplePosition</c> succeeds.
        /// Returns false when the harness cannot build one, letting callers assert the documented
        /// no-NavMesh fallback rather than fail.
        /// </summary>
        public bool TryBakeNavMesh()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(60, .2f, 60),
                transform = Matrix4x4.TRS(Vector3.down * .1f, Quaternion.identity, Vector3.one),
                area = 0
            };
            _navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(60, 5, 60)), Vector3.zero, Quaternion.identity);
            if (_navMesh == null)
            {
                HasNavMesh = false;
                return false;
            }

            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            HasNavMesh = _navMeshInstance.valid;
            return HasNavMesh;
        }

        /// <summary>
        /// Builds an inactive enemy prefab template with the components the resolver expects to find and
        /// configure: an <see cref="Actor"/>, a <see cref="NavMeshAgent"/>, an <see cref="EnemyAI"/> and
        /// an <see cref="EnemyVariant"/>. Kept inactive so it acts as a template; clones are live enemies.
        /// The template's own lifecycle never runs (it stays inactive) so it does not pollute the scene.
        /// </summary>
        public GameObject BuildEnemyPrefabTemplate(float health = 30f)
        {
            var prefab = new GameObject("StageEnemyTemplate");
            prefab.SetActive(false);
            _spawned.Add(prefab);

            var col = prefab.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            var actor = prefab.AddComponent<Actor>();
            actor.health = health;

            var agent = prefab.AddComponent<NavMeshAgent>();
            agent.radius = .3f;
            agent.height = 1.8f;

            // EnemyVariant [RequireComponent(Actor, EnemyAI)]; add EnemyAI first, then the variant.
            prefab.AddComponent<EnemyAI>();
            prefab.AddComponent<EnemyVariant>();
            return prefab;
        }

        /// <summary>
        /// Creates a runtime <see cref="EnemyRespawnPoint"/> at <paramref name="position"/> injected with
        /// <paramref name="prefab"/> through the additive <c>ConfigurePrefab</c> initializer (which also
        /// suppresses death-respawns), exactly as the <c>ProgressionDirector</c> materializes spawn points.
        /// </summary>
        public EnemyRespawnPoint BuildRespawnPoint(GameObject prefab, Vector3 position)
        {
            var go = new GameObject("StageRespawnPoint");
            _spawned.Add(go);
            go.transform.position = position;
            var point = go.AddComponent<EnemyRespawnPoint>();
            point.ConfigurePrefab(prefab);
            return point;
        }

        /// <summary>
        /// Builds an <see cref="EnemyArchetype"/> asset with the given id / role wrapping a fresh
        /// <see cref="EnemyProfile"/>, wiring the serialized-private fields by reflection the same way
        /// <c>ArchetypeIntegrationTests</c> does. The profile instance is returned via
        /// <paramref name="profile"/> so a test can assert the variant adopted exactly that profile.
        /// </summary>
        public EnemyArchetype BuildArchetype(ArchetypeId id, CombatRole role, out EnemyProfile profile)
        {
            profile = ScriptableObject.CreateInstance<EnemyProfile>();
            _assets.Add(profile);

            var archetype = ScriptableObject.CreateInstance<EnemyArchetype>();
            SetPrivate(archetype, "_id", id);
            SetPrivate(archetype, "_profile", profile);
            SetPrivate(archetype, "_primaryRole", role);
            _assets.Add(archetype);
            return archetype;
        }

        /// <summary>
        /// Builds a live, minimally-configured <see cref="PlayerActor"/> at <paramref name="position"/>
        /// with the serialized dependencies its <c>Awake</c> needs (hand transform, a deterministic
        /// starting weapon, crit-free stats) so it can stand up without a full scene, mirroring
        /// <c>ArsenalProjectileTestRig.BuildOwner</c>. A <see cref="NavMeshAgent"/> is added and warped
        /// onto the mesh when one has been baked so trophy reachability checks have an agent to path with.
        /// </summary>
        public PlayerActor BuildPlayer(Vector3 position, float health = 500f)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.weaponName = "StageRigWeapon";
            weapon.attackDamage = 5f;
            // RunBoons.Start reads _weapon.abilities and copies it, so give the weapon a non-null
            // (empty) ability array to keep RunBoons alive when a test adds it to this player.
            weapon.abilities = new Ability[0];
            _assets.Add(weapon);

            var go = new GameObject("StageRigPlayer");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;

            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = .3f;
            agent.height = 1.8f;

            var player = go.AddComponent<PlayerActor>();
            SetPrivate(player, "handTransform", hand.transform);
            SetPrivate(player, "startingWeapon", weapon);
            SetPrivate(player, "weapon", weapon);
            SetPrivate(player, "stats", new PlayerArpgStats
            {
                baseDamage = 0f,
                criticalChance = 0f,
                damageMultiplier = 1f,
                increasedDamagePercent = 0f,
                flatDamageBonus = 0f,
            });

            player.health = health;
            go.SetActive(true);

            if (HasNavMesh && agent.enabled && agent.isOnNavMesh)
            {
                agent.Warp(position);
            }

            return player;
        }

        /// <summary>Registers a GameObject for teardown so tests can track anything they create.</summary>
        public T Track<T>(T go) where T : Object
        {
            if (go is GameObject gameObject)
            {
                _spawned.Add(gameObject);
            }
            else
            {
                _assets.Add(go);
            }

            return go;
        }

        /// <summary>Destroys everything this rig created and removes the baked NavMesh.</summary>
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (GameObject go in _spawned)
            {
                if (go) Object.DestroyImmediate(go);
            }

            _spawned.Clear();

            foreach (Object asset in _assets)
            {
                if (asset) Object.DestroyImmediate(asset);
            }

            _assets.Clear();

            if (_navMeshInstance.valid) _navMeshInstance.Remove();
            if (_navMesh) Object.DestroyImmediate(_navMesh);
            HasNavMesh = false;
        }

        /// <summary>Sets a private/serialized field by reflection (used to author archetype/player deps).</summary>
        public static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (fi != null) fi.SetValue(target, value);
        }
    }
}
