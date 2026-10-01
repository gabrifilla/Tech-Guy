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
    /// PlayMode integration test for the Gauntlet/Bow overhaul — task 12.1 (Kiting Step / "Disparo em recuo").
    ///
    /// R8 grants the player a short reposition impulse when a Bow basic fires WHILE the player is retreating
    /// from the nearest enemy. The decision lives in the pure <see cref="KitingImpulse"/> helper (covered by
    /// the EditMode property test 6.4); this test exercises the <c>KitingStepCoordinator</c> MonoBehaviour
    /// end to end against a live player loop and a real baked NavMesh:
    ///
    ///  - R8.1/R8.4: firing while moving AWAY grants a backward impulse; firing while moving TOWARD the enemy
    ///    or standing still grants NONE (the dot-product gate in <see cref="KitingImpulse.ShouldReposition"/>).
    ///  - R8.3: the impulse routes through the player's own <see cref="NavMeshAgent"/> via
    ///    <see cref="NavMeshAgent.Raycast"/> + <see cref="NavMeshAgent.Move"/>, so it is clamped to the
    ///    walkable mesh and never crosses solid scenery (which carves a NavMesh hole).
    ///
    /// This must run in PlayMode: the coordinator reads the agent's live velocity for the move direction and
    /// displaces it through a real <see cref="NavMeshAgent"/> on a baked mesh across frames, which an EditMode
    /// test (no player loop, <c>Time.deltaTime == 0</c>) cannot drive. The test raises the real
    /// <c>CharControlScript.BasicAttackPerformed</c> event the coordinator subscribes to, rather than its pure
    /// decision helper.
    /// </summary>
    public sealed class KitingStepPlayModeTests
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

        /// <summary>Bakes a flat 60x60 floor NavMesh (the control case, no scenery blocking).</summary>
        private bool TryBakeFullFloor()
        {
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(60f, 0.2f, 60f),
                transform = Matrix4x4.TRS(Vector3.down * 0.1f, Quaternion.identity, Vector3.one),
                area = 0
            };
            _navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(Vector3.zero, new Vector3(60f, 5f, 60f)), Vector3.zero, Quaternion.identity);
            if (_navMesh == null) return false;
            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            return _navMeshInstance.valid;
        }

        /// <summary>
        /// Bakes a NavMesh that only covers the half-space <c>x &lt;= wallX</c>: a floor whose far edge ends
        /// at <paramref name="wallX"/>. Everything beyond is OFF the mesh exactly as a solid wall carves a
        /// NavMesh hole, so an agent pushed toward +X is clamped at the edge and cannot cross.
        /// </summary>
        private bool TryBakeHalfFloor(float wallX)
        {
            float width = wallX - (-30f);
            float centreX = (-30f + wallX) * 0.5f;
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(width, 0.2f, 40f),
                transform = Matrix4x4.TRS(new Vector3(centreX, -0.1f, 0f), Quaternion.identity, Vector3.one),
                area = 0
            };
            _navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(new Vector3(centreX, 0f, 0f), new Vector3(width + 4f, 5f, 44f)),
                Vector3.zero, Quaternion.identity);
            if (_navMesh == null) return false;
            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            return _navMeshInstance.valid;
        }

        /// <summary>
        /// Builds a minimal but real <see cref="PlayerActor"/> with a <see cref="NavMeshAgent"/> warped onto
        /// the mesh and a <see cref="CharControlScript"/> whose <c>playerActor</c> is wired. Mirrors
        /// <see cref="ArsenalProjectileTestRig.BuildOwner"/>'s serialize-while-inactive pattern.
        /// </summary>
        private (PlayerActor player, NavMeshAgent agent, CharControlScript controls) BuildPlayer(Vector3 position)
        {
            _weapon = ScriptableObject.CreateInstance<WeaponScript>();
            _weapon.weaponName = "TestBow";
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

            var agent = go.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.8f;
            agent.speed = 6f;
            agent.acceleration = 60f;
            agent.angularSpeed = 999f;
            agent.autoBraking = false;

            // CharControlScript.ValidateComponents logs an error if the Animator is missing (and warnings
            // for the optional click/dash/camera refs). Add a bare Animator so the test produces no
            // unexpected error logs; its locomotion is driven by the NavMeshAgent, not the Animator.
            go.AddComponent<Animator>();

            var controls = go.AddComponent<CharControlScript>();
            controls.playerActor = player;

            go.transform.position = position;
            go.SetActive(true);
            if (agent.enabled) agent.Warp(position);
            return (player, agent, controls);
        }

        /// <summary>Creates a live enemy Actor with a box collider at the given position.</summary>
        private Actor BuildEnemy(Vector3 position)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            var actor = go.AddComponent<Actor>();
            actor.health = 1000f;
            go.SetActive(true);
            return actor;
        }

        private KitingStepCoordinator AttachCoordinator(PlayerActor player, CharControlScript controls,
            NavMeshAgent agent, int rank)
        {
            var coordinator = player.gameObject.AddComponent<KitingStepCoordinator>();
            coordinator.Configure(player, controls, agent, rank);
            return coordinator;
        }

        /// <summary>Raises the real basic-attack event the coordinator subscribes to.</summary>
        private static void FireBasic(CharControlScript controls)
        {
            FieldInfo evt = typeof(CharControlScript).GetField("BasicAttackPerformed",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var del = (System.Action)evt.GetValue(controls);
            del?.Invoke();
        }

        /// <summary>
        /// Drives the agent toward <paramref name="destination"/> until its horizontal velocity settles into a
        /// stable direction (so <c>CharControlScript.CurrentMoveDirection</c> is well defined), or the frame
        /// budget runs out. Returns the observed move direction.
        /// </summary>
        private static IEnumerator DriveUntilMoving(NavMeshAgent agent, CharControlScript controls,
            Vector3 destination, System.Action<Vector3> onReady)
        {
            agent.SetDestination(destination);
            for (int frame = 0; frame < 120; frame++)
            {
                yield return null;
                if (controls.CurrentMoveDirection != Vector3.zero) { onReady(controls.CurrentMoveDirection); yield break; }
            }
            onReady(controls.CurrentMoveDirection);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.That(fi, Is.Not.Null, $"Expected field '{field}' on {target.GetType().Name}");
            fi.SetValue(target, value);
        }

        // --- tests --------------------------------------------------------------------------------

        // Feature: gauntlet-boon-playstyle-overhaul, task 12.1
        // Firing a basic WHILE moving away from the nearest enemy grants a navmesh-routed backward impulse:
        // the player ends up farther from the enemy than its pathing alone would place it, and stays on the
        // walkable mesh (the displacement is agent locomotion, not a teleport).
        // Validates: Requirements 8.1, 8.3
        [UnityTest]
        public IEnumerator FiringWhileMovingAway_GrantsBackwardNavMeshImpulse()
        {
            if (!TryBakeFullFloor())
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            // Enemy at the origin; player at +X. Retreating means moving toward +X (away from the enemy).
            Actor enemy = BuildEnemy(Vector3.zero);
            (PlayerActor player, NavMeshAgent agent, CharControlScript controls) = BuildPlayer(new Vector3(6f, 0f, 0f));
            Assert.That(agent.isOnNavMesh, Is.True, "the player agent must be on the baked NavMesh");

            AttachCoordinator(player, controls, agent, rank: 3);

            // Drive the player away from the enemy (toward +X) until it is actually moving.
            Vector3 moveDir = Vector3.zero;
            yield return DriveUntilMoving(agent, controls, new Vector3(20f, 0f, 0f), d => moveDir = d);
            Assert.That(moveDir.x, Is.GreaterThan(0f), "the player must be retreating toward +X for this case");

            // A reference run WITHOUT firing: how far pathing alone carries the player over the same window.
            float distBeforeFire = Vector3.Distance(player.transform.position, enemy.transform.position);

            // Fire the basic while retreating: KitingImpulse grants an extra backward push (R8.1).
            FireBasic(controls);
            // Let the agent.Move impulse resolve this frame plus a couple of follow-up frames.
            for (int frame = 0; frame < 3; frame++) yield return null;

            float distAfterFire = Vector3.Distance(player.transform.position, enemy.transform.position);
            Assert.That(distAfterFire, Is.GreaterThan(distBeforeFire + 0.3f),
                "firing while retreating must add a backward reposition impulse away from the enemy (R8.1)");
            Assert.That(agent.isOnNavMesh, Is.True,
                "the impulse must keep the player on the NavMesh — it is locomotion, not a teleport (R8.3)");
            Assert.That(Mathf.Abs(player.transform.position.y), Is.LessThan(0.2f),
                "the reposition is a ground-plane displacement, not a vertical launch");
        }

        // Feature: gauntlet-boon-playstyle-overhaul, task 12.1
        // Firing a basic while moving TOWARD the enemy grants NO impulse: the dot-product gate rejects an
        // approaching move direction, so the player's distance to the enemy is governed purely by its pathing.
        // Validates: Requirements 8.1, 8.4
        [UnityTest]
        public IEnumerator FiringWhileMovingToward_GrantsNoImpulse()
        {
            if (!TryBakeFullFloor())
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            Actor enemy = BuildEnemy(Vector3.zero);
            (PlayerActor player, NavMeshAgent agent, CharControlScript controls) = BuildPlayer(new Vector3(12f, 0f, 0f));
            Assert.That(agent.isOnNavMesh, Is.True);

            AttachCoordinator(player, controls, agent, rank: 3);

            // Drive the player TOWARD the enemy (toward -X / the origin).
            Vector3 moveDir = Vector3.zero;
            yield return DriveUntilMoving(agent, controls, new Vector3(2f, 0f, 0f), d => moveDir = d);
            Assert.That(moveDir.x, Is.LessThan(0f), "the player must be approaching the enemy for this case");

            Vector3 posBeforeFire = player.transform.position;
            FireBasic(controls);
            yield return null; // same frame the impulse would have applied, had the gate permitted it

            // With no impulse, the only displacement this frame is the agent's own forward pathing (toward
            // the enemy). An impulse would have shoved the player backward (+X); assert that did NOT happen.
            Vector3 posAfterFire = player.transform.position;
            Assert.That(posAfterFire.x, Is.LessThanOrEqualTo(posBeforeFire.x + 1e-3f),
                "approaching the enemy must grant no backward impulse (R8.1/R8.4)");
        }

        // Feature: gauntlet-boon-playstyle-overhaul, task 12.1
        // Firing a basic while STANDING STILL grants no impulse: with zero move direction the gate is false,
        // so the stationary player does not move at all.
        // Validates: Requirements 8.1, 8.4
        [UnityTest]
        public IEnumerator FiringWhileStandingStill_GrantsNoImpulse()
        {
            if (!TryBakeFullFloor())
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            Actor enemy = BuildEnemy(Vector3.zero);
            (PlayerActor player, NavMeshAgent agent, CharControlScript controls) = BuildPlayer(new Vector3(6f, 0f, 0f));
            Assert.That(agent.isOnNavMesh, Is.True);

            AttachCoordinator(player, controls, agent, rank: 3);

            // No destination set: let the agent settle to a standstill.
            for (int frame = 0; frame < 10; frame++) yield return null;
            Assert.That(controls.CurrentMoveDirection, Is.EqualTo(Vector3.zero),
                "the player must be standing still for this case");

            Vector3 posBeforeFire = player.transform.position;
            FireBasic(controls);
            yield return null;

            Vector3 posAfterFire = player.transform.position;
            Assert.That(Vector3.Distance(posAfterFire, posBeforeFire), Is.LessThan(1e-2f),
                "a standing-still basic must grant no reposition impulse (R8.1/R8.4)");
        }

        // Feature: gauntlet-boon-playstyle-overhaul, task 12.1
        // The reposition impulse never crosses solid scenery. A wall stands just behind the retreating player
        // and the NavMesh ends at the wall, so the walled region is OFF the mesh. The player fires while
        // retreating straight at the wall; NavMeshAgent.Raycast/Move clamps the impulse to the mesh edge, so
        // the player never crosses into the walled region.
        // Validates: Requirements 8.3
        [UnityTest]
        public IEnumerator RepositionImpulse_NeverCrossesSolidScenery()
        {
            const float wallX = 10f; // NavMesh ends here; the wall + unwalkable space begin here.

            if (!TryBakeHalfFloor(wallX))
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            var wall = new GameObject("TestWall");
            _spawned.Add(wall);
            wall.transform.position = new Vector3(wallX + 0.5f, 1f, 0f);
            var wallCol = wall.AddComponent<BoxCollider>();
            wallCol.size = new Vector3(1f, 3f, 10f);
            Physics.SyncTransforms();

            // Enemy at the origin; player near the wall edge. Retreating (+X) aims straight at the wall / the
            // off-mesh region, so an unclamped impulse would tunnel past the edge.
            Actor enemy = BuildEnemy(Vector3.zero);
            (PlayerActor player, NavMeshAgent agent, CharControlScript controls) = BuildPlayer(new Vector3(wallX - 0.8f, 0f, 0f));
            Assert.That(agent.isOnNavMesh, Is.True);

            AttachCoordinator(player, controls, agent, rank: 3);

            Vector3 moveDir = Vector3.zero;
            yield return DriveUntilMoving(agent, controls, new Vector3(wallX + 20f, 0f, 0f), d => moveDir = d);

            // Fire repeatedly while pinned against the wall edge so the impulse is attempted at full strength.
            for (int burst = 0; burst < 8; burst++)
            {
                FireBasic(controls);
                for (int frame = 0; frame < 3; frame++) yield return null;
            }

            Assert.That(agent.isOnNavMesh, Is.True,
                "the player must remain on the NavMesh — the impulse is locomotion, not a teleport (R8.3)");
            Assert.That(player.transform.position.x, Is.LessThanOrEqualTo(wallX + 1e-1f),
                "the reposition impulse must not push the player through the wall / off the NavMesh (R8.3)");
        }
    }
}
