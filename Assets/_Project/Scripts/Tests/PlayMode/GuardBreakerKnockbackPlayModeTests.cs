using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode integration test for the Gauntlet overhaul — task 9.2 (Guard Breaker / "Guarda partida").
    ///
    /// R5.2 requires the third consecutive basic hit to break the enemy's stance into a deliberate
    /// <see cref="StanceBreakEffect.Knockback"/>. The design routes that knockback through the enemy's
    /// own locomotion channel: <c>ImpactGuardTracker</c> calls
    /// <see cref="PlayerActor.ApplyHitReactionTo"/> with <see cref="StanceBreakEffect.Knockback"/>, which
    /// resolves in <c>CombatReactionController.TriggerStanceBreak</c> → <c>ApplyPush</c> →
    /// <c>MoveStep</c> → <c>NavMeshAgent.Move</c>. Because <c>NavMeshAgent.Move</c> clamps displacement to
    /// the NavMesh, the thrown enemy slides along its walkable channel and is never pushed through solid
    /// scenery (which carves a hole in the NavMesh).
    ///
    /// This must run in PlayMode: the knockback plays out over several frames through a live
    /// <see cref="NavMeshAgent"/> on a real baked NavMesh, which an EditMode test (with no player loop and
    /// <c>Time.deltaTime == 0</c>) cannot drive. The test drives the exact mechanism the Guard Breaker
    /// boon invokes on its third hit, rather than the boon's pure counter (covered by the EditMode
    /// property test 9.1).
    ///
    /// Validates: Requirements 5.2
    /// </summary>
    public sealed class GuardBreakerKnockbackPlayModeTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();
        private NavMeshData _navMesh;
        private NavMeshDataInstance _navMeshInstance;
        private WeaponScript _weapon;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (GameObject go in _spawned)
                if (go) Object.DestroyImmediate(go);
            _spawned.Clear();
            if (_weapon) Object.DestroyImmediate(_weapon);
            if (_navMeshInstance.valid) _navMeshInstance.Remove();
            if (_navMesh) Object.DestroyImmediate(_navMesh);
        }

        // --- rig helpers --------------------------------------------------------------------------

        /// <summary>
        /// Bakes a NavMesh that only covers the half-space <c>z &lt;= wallZ</c>: a floor box whose far
        /// edge ends at <paramref name="wallZ"/>. Everything beyond <paramref name="wallZ"/> (where a
        /// solid wall stands) is OFF the mesh, exactly as solid scenery carves a NavMesh hole. An agent
        /// knocked toward +Z is therefore clamped at the mesh edge and cannot cross into the walled
        /// region. Returns false if the harness cannot bake, so callers can <see cref="Assert.Ignore"/>.
        /// </summary>
        private bool TryBakeHalfFloor(float wallZ)
        {
            // Floor spans z in [-20, wallZ]; centre and size chosen so the far edge sits exactly at wallZ.
            float depth = wallZ - (-20f);
            float centreZ = (-20f + wallZ) * 0.5f;
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(40f, 0.2f, depth),
                transform = Matrix4x4.TRS(new Vector3(0f, -0.1f, centreZ), Quaternion.identity, Vector3.one),
                area = 0
            };
            _navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(new Vector3(0f, 0f, centreZ), new Vector3(44f, 5f, depth + 4f)),
                Vector3.zero, Quaternion.identity);
            if (_navMesh == null) return false;
            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            return _navMeshInstance.valid;
        }

        /// <summary>Bakes a flat 40x40 floor NavMesh (the control case, no scenery blocking).</summary>
        private bool TryBakeFullFloor()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(40f, 0.2f, 40f),
                transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one),
                area = 0
            };
            _navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(40f, 5f, 40f)), Vector3.zero, Quaternion.identity);
            if (_navMesh == null) return false;
            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            return _navMeshInstance.valid;
        }

        /// <summary>
        /// Builds a minimal but real <see cref="PlayerActor"/> at <paramref name="position"/>, mirroring
        /// <see cref="ArsenalProjectileTestRig.BuildOwner"/>: serialized fields are set while inactive so
        /// the equip pipeline in Awake has what it needs. The weapon is irrelevant to the reaction path
        /// but keeps Awake happy.
        /// </summary>
        private PlayerActor BuildPlayer(Vector3 position)
        {
            _weapon = ScriptableObject.CreateInstance<WeaponScript>();
            _weapon.weaponName = "TestFist";
            _weapon.attackDamage = 10f;

            var go = new GameObject("TestPlayer");
            go.SetActive(false);
            _spawned.Add(go);

            var hand = new GameObject("Hand");
            hand.transform.SetParent(go.transform);

            var player = go.AddComponent<PlayerActor>();
            SetPrivate(player, "handTransform", hand.transform);
            SetPrivate(player, "startingWeapon", _weapon);
            SetPrivate(player, "weapon", _weapon);
            SetPrivate(player, "stats", new PlayerArpgStats
            {
                baseDamage = 0f, criticalChance = 0f, damageMultiplier = 1f,
                increasedDamagePercent = 0f, flatDamageBonus = 0f,
            });

            go.transform.position = position;
            go.SetActive(true);
            return player;
        }

        /// <summary>
        /// Builds a live enemy with a <see cref="NavMeshAgent"/> (warped onto the mesh) and a
        /// <see cref="CombatReactionController"/>. The controller auto-wires the agent in Awake, so its
        /// knockback routes through <c>agent.Move</c>. No Rigidbody is added, so the push always takes
        /// the NavMesh locomotion path (never a physics impulse). Stance is set to break on a single hit.
        /// </summary>
        private (Actor actor, NavMeshAgent agent, CombatReactionController reaction) BuildEnemy(Vector3 position)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;

            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;

            var actor = go.AddComponent<Actor>();
            actor.health = 1000f;

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.8f;
            agent.speed = 6f;

            var reaction = go.AddComponent<CombatReactionController>();

            go.SetActive(true); // Awake: agent enables, reaction auto-wires the agent

            // A tiny stance pool so the very first requested hit breaks stance and fires the Knockback.
            reaction.ConfigureStance(EnemyRank.Normal, newMaxStance: 1f, newStanceDamageMultiplier: 1f,
                newRecoveryPerSecond: 0f, newRecoveryDelay: 10f,
                newStaggerResistance: 0f, newStunResistance: 0f, newKnockUpResistance: 0f,
                newKnockbackResistance: 0f);

            if (agent.enabled) agent.Warp(position);
            return (actor, agent, reaction);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(fi, Is.Not.Null, $"Expected field '{field}' on {target.GetType().Name}");
            fi.SetValue(target, value);
        }

        // --- tests --------------------------------------------------------------------------------

        // Feature: gauntlet-boon-playstyle-overhaul, task 9.2
        // The stance-break Knockback moves the enemy through its NavMesh locomotion channel: a heavy
        // stance hit that breaks the guard displaces the agent away from the player along the walkable
        // mesh. Driven under the live player loop so the multi-frame push coroutine actually advances.
        // Validates: Requirements 5.2
        [UnityTest]
        public IEnumerator StanceBreakKnockback_MovesEnemyThroughNavMeshChannel()
        {
            if (!TryBakeFullFloor())
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            // Player at the origin; enemy 2 m toward +Z so the away-from-player push aims at +Z.
            PlayerActor player = BuildPlayer(Vector3.zero);
            (Actor enemy, NavMeshAgent agent, _) = BuildEnemy(new Vector3(0f, 0f, 2f));
            Assert.That(agent.isOnNavMesh, Is.True, "the enemy agent must be on the baked NavMesh");

            Vector3 start = enemy.transform.position;

            // The exact call ImpactGuardTracker makes on the third basic hit (R5.2): a heavy stagger that
            // breaks stance into a deliberate Knockback, with no immediate push (pushDistance 0f).
            player.ApplyHitReactionTo(enemy, HitReactionType.Stagger, HitStrength.Heavy,
                stanceDamage: 100f, StanceBreakEffect.Knockback, pushDistance: 0f);

            // Let the push coroutine play out across frames.
            for (int frame = 0; frame < 60; frame++) yield return null;

            Vector3 end = enemy.transform.position;
            float movedAlongPush = end.z - start.z;

            Assert.That(movedAlongPush, Is.GreaterThan(0.5f),
                "the stance-break Knockback must displace the enemy away from the player (R5.2)");
            // Displacement stays on the walkable plane (NavMesh locomotion, not a launch).
            Assert.That(Mathf.Abs(end.y - start.y), Is.LessThan(0.2f),
                "the Knockback is a ground-plane locomotion displacement, not a vertical launch");
            Assert.That(agent.isOnNavMesh, Is.True, "the enemy stays on the NavMesh after the knockback");
        }

        // Feature: gauntlet-boon-playstyle-overhaul, task 9.2
        // The Knockback never pushes the enemy through solid scenery. A wall stands just behind the enemy
        // (relative to the player) and the NavMesh ends at the wall, so the walled region is OFF the mesh
        // exactly as a solid obstacle carves it. The enemy is thrown toward the wall but NavMeshAgent.Move
        // clamps it to the mesh edge: it never crosses into the walled region.
        // Validates: Requirements 5.2
        [UnityTest]
        public IEnumerator StanceBreakKnockback_NeverCrossesSolidScenery()
        {
            const float wallZ = 4f; // NavMesh ends here; the wall + unwalkable space begin here.

            if (!TryBakeHalfFloor(wallZ))
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            // A solid wall at the mesh edge (visual/physical scenery that carved the hole beyond wallZ).
            var wall = new GameObject("TestWall");
            _spawned.Add(wall);
            wall.transform.position = new Vector3(0f, 1f, wallZ + 0.5f);
            var wallCol = wall.AddComponent<BoxCollider>();
            wallCol.size = new Vector3(10f, 3f, 1f);
            Physics.SyncTransforms();

            // Player at origin, enemy near the wall edge; the away-from-player push aims straight at +Z
            // (into the wall / off-mesh region). A large knockback would overshoot the wall if unclamped.
            PlayerActor player = BuildPlayer(Vector3.zero);
            (Actor enemy, NavMeshAgent agent, _) = BuildEnemy(new Vector3(0f, 0f, wallZ - 0.6f));
            Assert.That(agent.isOnNavMesh, Is.True, "the enemy agent must be on the baked NavMesh");

            // Break stance into a Knockback (the request's default 4 m throw). The enemy starts 0.6 m
            // from the wall, so an unclamped 4 m throw would tunnel ~3.4 m past wallZ into the carved-out
            // region; NavMeshAgent.Move clamps it to the mesh edge instead, proving it never crosses.
            player.ApplyHitReactionTo(enemy, HitReactionType.Stagger, HitStrength.Heavy,
                stanceDamage: 100f, StanceBreakEffect.Knockback, pushDistance: 0f);

            for (int frame = 0; frame < 90; frame++) yield return null;

            Vector3 end = enemy.transform.position;

            // The agent stays on the mesh and never crosses the wall plane into the carved-out region.
            Assert.That(agent.isOnNavMesh, Is.True,
                "the enemy must remain on the NavMesh — the knockback is locomotion, not teleport (R5.2)");
            Assert.That(end.z, Is.LessThanOrEqualTo(wallZ + 1e-2f),
                "the Knockback must not push the enemy through the wall / off the NavMesh (R5.2)");
            // Sanity: the knockback did act on the enemy (it was pushed toward, and clamped at, the edge).
            Assert.That(end.z, Is.GreaterThanOrEqualTo(wallZ - 0.6f - 1e-2f),
                "the enemy should be nudged toward the wall edge, not away from it");
        }
    }
}
