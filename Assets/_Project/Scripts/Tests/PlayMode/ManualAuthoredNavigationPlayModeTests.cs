using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TechGuy.Tests
{
    public sealed class ManualAuthoredNavigationPlayModeTests
    {
#if UNITY_EDITOR
        // Exercises saved geometry/gates/NavMesh, with pure-policy commits standing in for enemy
        // combat and rewards. This is physical route validation, not a full run or gameplay acceptance.
        [UnityTest] public IEnumerator FourAuthoredPathsAreNavigableAndRejectedBranchesRemainPhysicallySealed()
        {
            const string definitionPath = "Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset";
            const string prefabPath = "Assets/_Project/Prefabs/Run/FirstSectorManualEnvironment.prefab";
            var definition = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>(definitionPath);
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(definition, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out var route, out string error), error);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            var instance = UnityEngine.Object.Instantiate(prefab);
            var environment = instance.GetComponent<ManualStageEnvironment>();
            try
            {
                environment.SetActive(true);
                foreach (var path in route.Paths)
                {
                    var policy = new ManualRoutePolicy(route); var ready = new RunActionContext(true);
                    environment.ApplyGateState(policy.Session);
                    for (int i = 1; i < path.Length; i++)
                    {
                        int from = path[i - 1], next = path[i];
                        Assert.IsTrue(policy.TryCommitDestination(policy.Session.Id, next, ready)); environment.ApplyGateState(policy.Session);
                        yield return new WaitForSeconds(.25f); Physics.SyncTransforms();
                        Assert.IsTrue(NavMesh.SamplePosition(route.Node(from).Center, out var a, 2, NavMesh.AllAreas));
                        Assert.IsTrue(NavMesh.SamplePosition(route.Node(next).Center, out var b, 2, NavMesh.AllAreas));
                        var navPath = new NavMeshPath();
                        Assert.IsTrue(NavMesh.CalculatePath(a.position, b.position, NavMesh.AllAreas, navPath));
                        Assert.AreEqual(NavMeshPathStatus.PathComplete, navPath.status, "Connection " + from + " → " + next);
                        Assert.IsTrue(policy.TryEnterNode(policy.Session.Id, next, ready)); environment.ApplyGateState(policy.Session);
                        yield return new WaitForSeconds(.25f); Physics.SyncTransforms();
                        foreach (var binding in environment.Connections)
                        {
                            Assert.IsTrue(binding.Barrier.activeInHierarchy, "All passages sealed during encounter/Repair.");
                            var collider = binding.Barrier.GetComponent<Collider>();
                            Vector3 normal = collider.bounds.size.x < collider.bounds.size.z ? Vector3.right : Vector3.forward;
                            Assert.IsTrue(collider.Raycast(new Ray(collider.bounds.center - normal * 3f, normal), out _, 6f), "Gate has no physical collider.");
                        }
                        if (route.Node(next).Kind == ManualNodeKind.Repair) Assert.IsTrue(policy.TryCompleteRepair(policy.Session.Id, next, ready));
                        else
                        {
                            Assert.IsTrue(policy.TryResolveEncounter(policy.Session.Id, next, true, ready));
                            for (int ordinal = 0; ordinal < route.Node(next).Acquisitions; ordinal++)
                                Assert.IsTrue(policy.TryCompleteReward(policy.Session.Id, next, ordinal, route.Node(next).Reward == RunRewardKind.Mutation, ready));
                        }
                        environment.ApplyGateState(policy.Session);
                        if (route.Node(next).Kind != ManualNodeKind.Boss)
                        {
                            yield return new WaitForSeconds(.25f);
                            var returnPath = new NavMeshPath();
                            Assert.IsTrue(NavMesh.CalculatePath(b.position, a.position, NavMesh.AllAreas, returnPath));
                            Assert.AreEqual(NavMeshPathStatus.PathComplete, returnPath.status, "Completed passage must permit physical backtracking.");
                        }
                        foreach (var edge in route.Connections)
                            if (policy.Session.NodeState(edge.To) == RunNodeState.Excluded)
                                Assert.IsTrue(environment.Connections.Single(c => c.ConnectionId == edge.Id).Barrier.activeInHierarchy);
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        [UnityTest] public IEnumerator AuthoredClosedGateStopsRealDashAndGauntletAdvance()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Run/FirstSectorManualEnvironment.prefab");
            var instance = UnityEngine.Object.Instantiate(prefab);
            var environment = instance.GetComponent<ManualStageEnvironment>();
            var rig = new ProceduralStageTestRig();
            var dash = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset"));
            try
            {
                environment.SetActive(true); yield return new WaitForSeconds(.3f);
                Assert.IsNull(Camera.main, "This movement fixture needs deterministic forward aim without a scene camera.");
                var binding = environment.Connections.Single(c => c.ConnectionId == 2);
                Vector3 direction = Vector3.left;
                Vector3 center = binding.Barrier.transform.position; center.y = 0;
                const float approachDistance = 1.8f; // Authored Q advances 2.8; both Q and Dash would cross from here.
                Assert.IsTrue(NavMesh.SamplePosition(center - direction * approachDistance, out var hit, .5f, NavMesh.AllAreas));
                var player = rig.BuildPlayer(hit.position);
                // The shared navigation rig starts with no mana; this scenario must actually execute Q.
                typeof(PlayerActor).GetField("baseMaxMana", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(player, 100f);
                player.RefreshResourceStats(true);
                player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]));
                var agent = player.GetComponent<NavMeshAgent>(); Assert.IsTrue(agent.Warp(hit.position));
                var holder = player.gameObject.AddComponent<AbilityHolder>(); holder.RefreshLoadout();
                yield return null;
                Assert.Greater(dash.dashVelocity * dash.dashTime, approachDistance, "Requested displacement must cross the gate without collision clamping.");
                // Invoke the actual movement coroutine with explicit direction; UI/input is covered later.
                var routine = (IEnumerator)typeof(DashScript).GetMethod("Dash", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(dash, new object[]{player.gameObject,direction});
                yield return player.StartCoroutine(routine);
                Assert.Less(Vector3.Dot(player.transform.position-center,direction), -.1f, "Dash crossed a sealed gate.");
                Assert.IsTrue(agent.Warp(hit.position)); player.transform.rotation=Quaternion.LookRotation(direction);
                var q = (BreakerGauntletAbility)player.CurrentWeapon.abilities[0]; Assert.Greater(q.AdvanceDistance,approachDistance);
                AbilityUseFailure? rejection = null;
                holder.AbilityRejected += (slot, reason) => rejection = reason;
                Assert.IsTrue(holder.TryUseAbility(0), "Authored Q was rejected: " + rejection + "; mana=" + player.mana);
                var combat = player.GetComponent<BreakerGauntletCombat>();
                float deadline=Time.realtimeSinceStartup+3f;
                while(combat.IsExecuting && Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsFalse(combat.IsExecuting);
                Assert.Less(Vector3.Dot(player.transform.position-center,direction),-.1f,"Gauntlet Q crossed a sealed gate.");
            }
            finally { rig.TearDown(); UnityEngine.Object.DestroyImmediate(dash); UnityEngine.Object.DestroyImmediate(instance); }
        }
#endif
    }
}
