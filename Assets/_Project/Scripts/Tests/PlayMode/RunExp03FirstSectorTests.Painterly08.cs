using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        private const string PainterlyScene = "Assets/_Project/Scenes/Review/RunExp08PainterlyRoom.unity";

        [UnityTest] public IEnumerator Painterly08GauntletCombatCoverAndDoorFlow() => PainterlyRoom08(0);
        [UnityTest] public IEnumerator Painterly08BowCombatCoverAndDoorFlow() => PainterlyRoom08(1);
        [UnityTest] public IEnumerator Painterly08SpearCombatCoverAndDoorFlow() => PainterlyRoom08(2);

        private IEnumerator PainterlyRoom08(int weapon)
        {
            using (var input08 = new PublishedInput06())
            {
            _dir = Path.GetFullPath("Docs/validation/artifacts/run-exp-08/gameplay/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "-weapon-" + weapon);
            Directory.CreateDirectory(_dir);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, weapon);
            SessionState.SetString(RunSeedSource.NextEditorSeedKey, "63");
            EditorSceneManager.LoadSceneInPlayMode(PainterlyScene, new LoadSceneParameters(LoadSceneMode.Single));
            for (int i = 0; i < 5; i++) yield return null;
            var view = Object.FindAnyObjectByType<PainterlyRoomPrototype>();
            float deadline = Time.realtimeSinceStartup + 10;
            while (!view.Ready) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            var director = Object.FindAnyObjectByType<ProgressionDirector>();
            var player = Object.FindAnyObjectByType<PlayerActor>();
            var session = director.ManualSession; var node = session.Route.Node(1);
            var nav = player.GetComponent<NavMeshAgent>();
            player.AddDamageImmunity(director);
            Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
            Assert.IsTrue(nav.Warp(node.Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, 1));
            foreach (var coin in director.GetComponentsInChildren<CoinDrop>()) Object.DestroyImmediate(coin);
            yield return new WaitForSeconds(.35f);
            var geometry = director.ManualEnvironment.GeometryRoot;
            var colliders = geometry.GetComponentsInChildren<Collider>(true);
            var bounds = colliders.Select(c => c.bounds).ToArray();
            Assert.AreEqual(1, geometry.GetComponentsInChildren<FirstSectorEnvironmentArt>().Count(p => p.transform == director.ManualEnvironment.Node(1).Arena));
            Assert.AreEqual(2, director.GetComponentsInChildren<FirstSectorEnemyPose>().Length);
            Time.timeScale = 0;
            try
            {
                view.SetPainterlyVisible(false); yield return Capture("08-baseline-room");
                view.SetPainterlyVisible(true); yield return Capture("08-painterly-room");
                Assert.AreEqual(colliders.Length, geometry.GetComponentsInChildren<Collider>(true).Length);
                for (int i = 0; i < colliders.Length; i++) Assert.AreEqual(bounds[i], colliders[i].bounds, colliders[i].name);
                var c = Camera.main;
                File.WriteAllText(Path.Combine(_dir, "camera-and-room.json"), "{\"camera\":\"" + c.transform.position.ToString("F4") + "\",\"rotation\":\"" + c.transform.eulerAngles.ToString("F4") + "\",\"fov\":" + c.fieldOfView + ",\"width\":" + Screen.width + ",\"height\":" + Screen.height + ",\"colliders\":" + colliders.Length + ",\"visuals\":" + view.VisualObjectCount + "}");
            }
            finally { Time.timeScale = 1; }
            foreach (float side in new[] { -1f, 1f })
            {
                var path = new NavMeshPath();
                Assert.IsTrue(NavMesh.CalculatePath(node.Center + new Vector3(side * 10, 0, -7), node.Center + new Vector3(side * 10, 0, 7), NavMesh.AllAreas, path));
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
            }
            yield return ObserveNativePoses(director, node);
            yield return VerifyEmitterCover(director, node, player);
            var coverCollider = director.ManualEnvironment.Node(1).Arena.GetComponentsInChildren<BoxCollider>().First(c => c.name == "Cobertura deslocada 0");
            var cameraRig = Camera.main.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
            Assert.IsTrue(nav.Warp(node.Center + new Vector3(-3,0,1.15f)));
            yield return new WaitForSeconds(.5f);
            Assert.IsTrue(cameraRig.IsOcclusionFaded(coverCollider), "Painted cover resolves native camera fade.");
            Assert.IsTrue(coverCollider.enabled); yield return Capture("08-painted-cover-occlusion");
            Assert.IsTrue(nav.Warp(node.Center + new Vector3(-8, 0, -5)));
            Assert.IsTrue(nav.SetDestination(node.Center + new Vector3(-8, 0, 5)));
            var start = player.transform.position;
            yield return new WaitForSeconds(.7f);
            Assert.Greater((player.transform.position - start).sqrMagnitude, .5f);
            yield return Capture("08-player-circulating"); nav.ResetPath();
            // Use the same resource-only fixture as05; native executors, mana costs and cooldowns remain intact.
            typeof(PlayerActor).GetField("baseMaxMana", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 1500f);
            player.RefreshResourceStats();
            var holder = player.GetComponent<AbilityHolder>(); var controls = player.GetComponent<CharControlScript>();
            deadline = Time.realtimeSinceStartup + 5;
            while (!controls.GameplayInputReady) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return Basic(controls, player.transform.position + Vector3.forward * 4);
            for (int slot = 0; slot < 4; slot++)
            {
                player.mana = 1500;
                if (weapon == 0 && slot == 3) player.GetComponent<BreakerGauntletCombat>().AddAsuraEnergy(100);
                Assert.IsTrue(holder.TryUseAbility(slot), "weapon=" + weapon + " slot=" + slot);
                yield return new WaitForSeconds(.12f); yield return Capture("08-native-skill-" + slot);
                deadline = Time.realtimeSinceStartup + 15;
                while (holder.IsCasting) { Assert.Less(Time.realtimeSinceStartup, deadline); if (holder.CanFinishSequence) holder.TryFinishSequence(); yield return null; }
                yield return new WaitForSeconds(.15f);
            }
            var dash = AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset");
            player.mana = 1500; Assert.IsTrue(holder.TryUseDash(dash));
            yield return new WaitForSeconds(.4f); Assert.IsFalse(dash.IsDashing(player.gameObject)); Assert.IsTrue(nav.isOnNavMesh);
            yield return Capture("08-dash-recovered");
            foreach (var actor in director.GetComponentsInChildren<Actor>()) actor.TakeDamage(1000000);
            deadline = Time.realtimeSinceStartup + 10;
            while (session.Phase == RunPhase.Encounter) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            var run = player.GetComponent<RunBoons>();
            while (session.Phase == RunPhase.RewardPending)
            {
                if (!run.IsChoosing) Assert.IsTrue(director.TryOpenManualReward(session.Id, 1, session.RewardOrdinal));
                yield return null; yield return Capture("08-C05-real-reward-" + session.RewardOrdinal);
                Assert.IsTrue(run.Choose(0));
            }
            int next = session.Route.Paths.First().SkipWhile(id => id != 1).Skip(1).First();
            Assert.IsTrue(director.TryCommitDestination(session.Id, next));
            var edge = session.Route.Connections.First(e => e.From == 1 && e.To == next);
            var points = ProceduralRunGeometryPlan.Create(session.Route)[edge.Id];
            var binding = director.ManualEnvironment.Connections.First(e => e.ConnectionId == edge.Id);
            Assert.IsFalse(binding.Barrier.activeSelf, "Native gate opens for committed traversal.");
            // Reposition to the mouth, then physically traverse the dressed threshold with the real agent.
            Assert.IsTrue(nav.Warp(points[0] - Vector3.forward));
            Assert.IsTrue(nav.SetDestination(points[0] + Vector3.forward * 5));
            deadline = Time.realtimeSinceStartup + 12;
            while (Vector3.Distance(player.transform.position, points[0] + Vector3.forward * 5) > 1f)
            { Assert.Less(Time.realtimeSinceStartup, deadline, "Physical transition traversal"); yield return null; }
            yield return Capture("08-open-door-transition");
            Assert.IsTrue(nav.Warp(session.Route.Node(next).Center)); Assert.IsTrue(director.TryEnterNode(session.Id, next));
            yield return null;
            Assert.IsFalse(director.ManualEnvironment.Node(next).Arena.GetComponentsInChildren<MeshRenderer>().Any(r => r.sharedMaterials.Any(m => m && m.name == "MineralPigment")), "Next room remains baseline.");
            yield return Capture("08-next-room-regular-flow");
            File.WriteAllText(Path.Combine(_dir, "validation.txt"), "Native roster/AI/nav active; player immunity, mana/Asura resource fixture, warp to scenario anchors and lethal room completion declared. Same colliders/bounds A/B; two routes, live movement, Emissor LOS, all four native skills plus basic/dash, real C05 ticket, native gate and physical threshold traversal, next room baseline. Not human acceptance or unassisted survival.");
            }
        }
    }
}
