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
    /// PlayMode integration test for the Gauntlet overhaul — task 15.1 (SpacingRecoil / "Recuo controlado", R11).
    ///
    /// R11.1/R11.2/R11.3 require a CONNECTING Lança thrust to step the player back toward the ideal
    /// spacing band, routed through the player's own <see cref="NavMeshAgent"/> so the recoil respects
    /// the navmesh and never crosses solid scenery; R11.4 requires a whiff (no connect) to apply no
    /// step-back. The design implements this in <c>ArsenalCombat</c>: after a thrust resolves its
    /// <c>primaryHits</c>, when <c>plan.SpacingRecoil &amp;&amp; primaryHits &gt; 0</c> it calls the private
    /// <c>ApplySpacingRecoil</c>, which measures the current spacing to the thrust target, asks the pure
    /// <see cref="SpacingBand"/> for the clamped backward step (scaled by the SpacingRecoil rank), and
    /// moves the player with <c>NavMeshAgent.Raycast</c> (to clamp to the mesh) + <c>NavMeshAgent.Move</c>.
    ///
    /// This must run in PlayMode: the recoil moves a live <see cref="NavMeshAgent"/> on a real baked
    /// NavMesh, and the NavMesh clamp (Raycast → mesh edge) only exists in a running scene. The test
    /// drives the exact production method the boon invokes on a connecting thrust, following the same
    /// seam-driving approach as <see cref="ImpalingLinePierceAndPullTests"/> and
    /// <see cref="ChainThrustConditionalTests"/> (invoke the private method, inject the private
    /// <c>_player</c>/<c>_agent</c> fields) — the spec forbids widening gameplay APIs purely for tests.
    ///
    /// Validates: Requirements 11.1, 11.3, 11.4
    /// </summary>
    public sealed class SpacingRecoilPlayModeTests
    {
        private static readonly MethodInfo ApplySpacingRecoilMethod = typeof(ArsenalCombat).GetMethod(
            "ApplySpacingRecoil", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo PlayerField = typeof(ArsenalCombat).GetField(
            "_player", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo AgentField = typeof(ArsenalCombat).GetField(
            "_agent", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private ArsenalProjectileTestRig _rig;
        private ArsenalCombat _combat;
        private NavMeshData _navMesh;
        private NavMeshDataInstance _navMeshInstance;

        [SetUp]
        public void SetUp()
        {
            Assert.IsNotNull(ApplySpacingRecoilMethod, "Expected private ArsenalCombat.ApplySpacingRecoil (test seam).");
            Assert.IsNotNull(PlayerField, "Expected private ArsenalCombat._player (test seam).");
            Assert.IsNotNull(AgentField, "Expected private ArsenalCombat._agent (test seam).");
            _rig = new ArsenalProjectileTestRig();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            if (_combat) Object.DestroyImmediate(_combat);
            _combat = null;
            foreach (GameObject go in _spawned)
                if (go) Object.DestroyImmediate(go);
            _spawned.Clear();
            if (_navMeshInstance.valid) _navMeshInstance.Remove();
            if (_navMesh) Object.DestroyImmediate(_navMesh);
            _rig?.TearDown();
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
        /// Bakes a NavMesh that only covers <c>z &gt;= wallZ</c>: a floor box whose near edge ends at
        /// <paramref name="wallZ"/>. Everything with <c>z &lt; wallZ</c> (where a solid wall stands behind
        /// the player) is OFF the mesh, exactly as solid scenery carves a NavMesh hole. The player, when
        /// recoiling backward (toward -Z, away from the thrust aimed at +Z), is clamped at the mesh edge
        /// and cannot cross into the walled region.
        /// </summary>
        private bool TryBakeFloorBehindWall(float wallZ)
        {
            // Floor spans z in [wallZ, 40]; the near edge sits exactly at wallZ.
            float depth = 40f - wallZ;
            float centreZ = (wallZ + 40f) * 0.5f;
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(60f, 0.2f, depth),
                transform = Matrix4x4.TRS(new Vector3(0f, -0.1f, centreZ), Quaternion.identity, Vector3.one),
                area = 0
            };
            _navMesh = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new List<NavMeshBuildSource> { source },
                new Bounds(new Vector3(0f, 0f, centreZ), new Vector3(64f, 5f, depth + 4f)),
                Vector3.zero, Quaternion.identity);
            if (_navMesh == null) return false;
            _navMeshInstance = NavMesh.AddNavMeshData(_navMesh);
            return _navMeshInstance.valid;
        }

        /// <summary>
        /// Builds the player with a live <see cref="NavMeshAgent"/> warped to <paramref name="position"/>
        /// and an <see cref="ArsenalCombat"/> whose private <c>_player</c>/<c>_agent</c> fields are injected
        /// explicitly (no FindObjectOfType/GameObject.Find). The agent carries the SpacingRecoil step.
        /// </summary>
        private (PlayerActor player, NavMeshAgent agent) BuildPlayer(Vector3 position)
        {
            PlayerActor player = _rig.BuildOwner();
            player.transform.position = position;

            var agent = player.gameObject.AddComponent<NavMeshAgent>();
            agent.radius = 0.3f;
            agent.height = 1.8f;
            agent.speed = 6f;
            agent.baseOffset = 0f;
            agent.Warp(position); // snap onto the baked mesh at the spawn point

            _combat = player.gameObject.AddComponent<ArsenalCombat>();
            // Inject the exact fields ArsenalCombat.Awake would wire, so the private recoil runs against
            // the live player + agent regardless of component-add ordering.
            PlayerField.SetValue(_combat, player);
            AgentField.SetValue(_combat, agent);
            return (player, agent);
        }

        /// <summary>Builds an <see cref="ArsenalCastPlan"/> for a SpacingRecoil-enabled thrust.</summary>
        private static ArsenalCastPlan BuildThrustPlan(float range, float width)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            var plan = new ArsenalCastPlan(ability)
            {
                Range = range,
                Width = width,
                SpacingRecoil = true, // R11.1: set in WeaponRunModifiers.Plan when SpacingRecoil rank > 0
            };
            Object.DestroyImmediate(ability); // plan copied by value; the asset is no longer needed
            return plan;
        }

        private Actor BuildEnemy(Vector3 position)
        {
            var go = new GameObject("SpacingRecoilEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            var actor = go.AddComponent<Actor>();
            actor.health = 100000f;
            go.SetActive(true);
            return actor;
        }

        private void InvokeRecoil(Vector3 center, Vector3 direction, ArsenalCastPlan plan, int rank) =>
            ApplySpacingRecoilMethod.Invoke(_combat, new object[] { center, direction, plan, rank });

        // --- tests --------------------------------------------------------------------------------

        // Feature: gauntlet-boon-playstyle-overhaul, task 15.1
        // A CONNECTING thrust steps the player back toward the ideal band, through the live NavMeshAgent.
        // The player starts close to the target (inside the band's lower edge) and, after the recoil,
        // sits farther from it along the walkable mesh, having moved backward (away from the thrust).
        // Validates: Requirements 11.1, 11.3
        [UnityTest]
        public IEnumerator ConnectingThrust_StepsPlayerBackAlongNavMesh()
        {
            if (!TryBakeFullFloor())
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            // Player at origin; target 2.5 m ahead (+Z), inside the band's lower edge (Min = 3.5 m) so a
            // step-back is warranted. The thrust direction is +Z, so the recoil pushes the player to -Z.
            (PlayerActor player, NavMeshAgent agent) = BuildPlayer(Vector3.zero);
            Assert.That(agent.isOnNavMesh, Is.True, "the player agent must be on the baked NavMesh");

            Vector3 direction = Vector3.forward;
            Actor target = BuildEnemy(new Vector3(0f, 0f, 2.5f));
            Physics.SyncTransforms();

            Vector3 start = player.transform.position;
            float startDistance = Vector3.Distance(start, target.transform.position);

            // The exact production call the thrust makes on a connect (primaryHits > 0) at rank 3.
            InvokeRecoil(player.transform.position, direction, BuildThrustPlan(range: 12f, width: 1f), rank: 3);

            // NavMeshAgent.Move applies over the agent's internal step; let a few frames settle it.
            for (int frame = 0; frame < 10; frame++) yield return null;

            Vector3 end = player.transform.position;
            float movedBack = start.z - end.z;             // positive = stepped backward (-Z)
            float endDistance = Vector3.Distance(end, target.transform.position);

            Assert.That(movedBack, Is.GreaterThan(0.05f),
                "a connecting thrust must step the player backward, away from the thrust direction (R11.1)");
            Assert.That(endDistance, Is.GreaterThan(startDistance),
                "the recoil must increase the spacing to the target, settling toward the ideal band (R11.1)");
            // The result never passes the band's upper edge (SpacingBand clamps the step to Max).
            Assert.That(endDistance, Is.LessThanOrEqualTo(SpacingBand.Max + 0.2f),
                "the step-back must never carry the player past the band's upper edge (R11.2)");
            Assert.That(agent.isOnNavMesh, Is.True,
                "the player stays on the NavMesh after the recoil — it is locomotion, not a teleport (R11.3)");
            Assert.That(Mathf.Abs(end.y - start.y), Is.LessThan(0.2f),
                "the recoil is a ground-plane locomotion step, not a vertical launch (R11.3)");
        }

        // Feature: gauntlet-boon-playstyle-overhaul, task 15.1
        // The recoil never carries the player through solid scenery: a wall stands just behind the player
        // and the NavMesh ends at the wall, so the walled region is OFF the mesh. The backward step is
        // clamped (NavMeshAgent.Raycast → mesh edge) and never crosses into the walled region.
        // Validates: Requirements 11.3
        [UnityTest]
        public IEnumerator Recoil_NeverCrossesSolidScenery()
        {
            const float wallZ = -0.6f; // NavMesh ends here; the wall + unwalkable space are at z < wallZ.

            if (!TryBakeFloorBehindWall(wallZ))
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            // A solid wall at the mesh's near edge (scenery that carved the hole for z < wallZ).
            var wall = new GameObject("SpacingRecoilWall");
            _spawned.Add(wall);
            wall.transform.position = new Vector3(0f, 1f, wallZ - 0.5f);
            var wallCol = wall.AddComponent<BoxCollider>();
            wallCol.size = new Vector3(20f, 3f, 1f);
            Physics.SyncTransforms();

            // Player right at the mesh edge; target ahead (+Z) so the recoil aims the player straight back
            // (into the wall / off-mesh region). An unclamped step would tunnel past wallZ; the agent clamps it.
            (PlayerActor player, NavMeshAgent agent) = BuildPlayer(new Vector3(0f, 0f, wallZ + 0.05f));
            Assert.That(agent.isOnNavMesh, Is.True, "the player agent must be on the baked NavMesh");

            Vector3 direction = Vector3.forward;
            BuildEnemy(new Vector3(0f, 0f, wallZ + 2.5f));
            Physics.SyncTransforms();

            // Rank 3 at a close spacing produces the largest step-back, which would overshoot the wall if
            // unclamped; NavMeshAgent.Raycast/Move clamps it to the mesh edge instead.
            InvokeRecoil(player.transform.position, direction, BuildThrustPlan(range: 12f, width: 1f), rank: 3);

            for (int frame = 0; frame < 30; frame++) yield return null;

            Vector3 end = player.transform.position;
            Assert.That(agent.isOnNavMesh, Is.True,
                "the player must remain on the NavMesh — the recoil is locomotion, not a teleport (R11.3)");
            Assert.That(end.z, Is.GreaterThanOrEqualTo(wallZ - 1e-2f),
                "the recoil must not push the player through the wall / off the NavMesh (R11.3)");
        }

        // Feature: gauntlet-boon-playstyle-overhaul, task 15.1
        // A WHIFF applies no step-back. R11.4 gates the recoil behind primaryHits > 0 at the call site;
        // its no-target guard realizes the same contract — with no enemy to connect with, the recoil
        // finds no spacing target and leaves the player exactly where it was.
        // Validates: Requirements 11.4
        [UnityTest]
        public IEnumerator Whiff_AppliesNoStepBack()
        {
            if (!TryBakeFullFloor())
                Assert.Ignore("Harness could not bake a runtime NavMesh.");

            (PlayerActor player, NavMeshAgent agent) = BuildPlayer(Vector3.zero);
            Assert.That(agent.isOnNavMesh, Is.True, "the player agent must be on the baked NavMesh");

            Vector3 start = player.transform.position;

            // No enemy in front: the thrust whiffed. The production call site only invokes the recoil when
            // primaryHits > 0, so a whiff never calls it; the no-target guard here proves that even if it
            // were called with no connect, the player does not step back (R11.4).
            InvokeRecoil(player.transform.position, Vector3.forward, BuildThrustPlan(range: 12f, width: 1f), rank: 3);

            for (int frame = 0; frame < 10; frame++) yield return null;

            Vector3 end = player.transform.position;
            Vector3 horizontal = end - start; horizontal.y = 0f;
            // The recoil's smallest meaningful step (rank 1 at a close spacing) is ~0.5 m; a live
            // NavMeshAgent settling onto the baked mesh after a Warp drifts only a few centimetres. A
            // 0.1 m bound cleanly separates "no step-back" from any genuine recoil, so a whiff that never
            // calls Move stays well under it (R11.4).
            Assert.That(horizontal.magnitude, Is.LessThan(0.1f),
                "a whiff (no connecting thrust) must apply no step-back (R11.4)");
        }
    }
}
