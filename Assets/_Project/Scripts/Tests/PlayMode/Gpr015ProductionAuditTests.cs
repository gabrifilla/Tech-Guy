using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class Gpr015ProductionAuditTests : RunSummaryIsolatedPlayModeFixture
    {
        private bool _hadWeapon, _assistance;
        private int _weapon;
        private string _directory;
        private string DirectoryFor(string phase)
        {
            if (_directory == null)
            {
                _directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/gpr-01-5/" + phase + "/" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")));
                System.IO.Directory.CreateDirectory(_directory);
            }
            return _directory;
        }
        [SetUp] public void PreserveProductionSettings()
        {
            _directory = null; _hadWeapon = PlayerPrefs.HasKey(WeaponLoadout.PreferenceKey); _weapon = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            _assistance = TitanicConductorPlaytestRewards.Enabled;
            SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, false);
        }
        [TearDown] public void RestoreProductionSettings()
        {
            if (_hadWeapon) PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, _weapon); else PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
            SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, _assistance); PlayerPrefs.Save(); Time.timeScale = 1;
        }
        private static IEnumerator LoadProduction()
        {
            yield return SceneManager.LoadSceneAsync("FirstSector"); yield return null; yield return null; yield return null;
            Assert.IsFalse(TitanicConductorPlaytestRewards.Enabled);
        }
        private static void PreventCoinPersistence(ProgressionDirector director)
        {
            foreach (var coin in director.GetComponentsInChildren<CoinDrop>()) Object.DestroyImmediate(coin);
        }

        [UnityTest] public IEnumerator GPR015PublishedRoute0UsesNaturalTickets() => PublishedRoute(0);
        [UnityTest] public IEnumerator GPR015PublishedRoute1UsesNaturalTickets() => PublishedRoute(1);
        [UnityTest] public IEnumerator GPR015PublishedRoute2UsesNaturalTickets() => PublishedRoute(2);
        [UnityTest] public IEnumerator GPR015PublishedRoute3UsesNaturalTickets() => PublishedRoute(3);
        private IEnumerator PublishedRoute(int pathIndex)
        {
            yield return LoadProduction();
            var director = Object.FindFirstObjectByType<ProgressionDirector>(); var player = Object.FindFirstObjectByType<PlayerActor>();
            var run = player.GetComponent<RunBoons>(); var session = director.ManualSession;
            Assert.IsTrue(director.ManualEnvironment.HasGeneratedLayout); Assert.IsFalse(session.Route.TitanicConductorPlaytest);
            player.AddDamageImmunity(director);
            var paths = session.Route.Paths.OrderBy(p => string.Join("/", p.Select(x => x.ToString("D3"))), StringComparer.Ordinal).ToArray();
            var rows = new List<string> { "Automated room completion, published FirstSector, assistance OFF, real tickets and Choose; no boon grants. Seed=" + session.Route.RewardSeed + "; path=" + pathIndex };
            int protocolOpportunities = 0, mutationOpportunities = 0, firstTripleNode = -1;
            foreach (int node in paths[pathIndex].Skip(1))
            {
                Assert.IsTrue(director.TryCommitDestination(session.Id, node));
                Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(node).Center));
                Assert.IsTrue(director.TryEnterNode(session.Id, node));
                if (session.Phase == RunPhase.RepairPending)
                { Assert.IsTrue(director.TryResolveRepair(session.Id, node, false)); yield return null; continue; }
                PreventCoinPersistence(director);
                foreach (var actor in director.GetComponentsInChildren<Actor>()) actor.TakeDamage(1000000);
                float deadline = Time.realtimeSinceStartup + 5;
                while (session.Phase == RunPhase.Encounter || session.Phase == RunPhase.FinalResolution)
                { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                while (session.Phase == RunPhase.RewardPending)
                {
                    yield return null; yield return null;
                    int ordinal = session.RewardOrdinal; var kind = session.Route.Node(node).RewardAt(ordinal);
                    if (kind == RunRewardKind.Protocol) protocolOpportunities++;
                    if (kind == RunRewardKind.Mutation) mutationOpportunities++;
                    if (!run.IsChoosing) Assert.IsTrue(director.TryOpenManualReward(session.Id, node, ordinal));
                    string[] targets = pathIndex % 2 == 0 ? new[] { TitanicFistsDefinition.Id, "conductor", "detonation" } : new[] { TitanicFistsDefinition.Id, "detonation", "conductor" };
                    int index = 0;
                    foreach (string id in targets)
                    {
                        bool owned = id == TitanicFistsDefinition.Id ? run.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic) == id : ProtocolInventory.TryKind(id, out var protocol) && run.Protocols.Rank(protocol) > 0;
                        int wanted = run.Choices.ToList().FindIndex(x => x.Id == id);
                        if (!owned && wanted >= 0) { index = wanted; break; }
                    }
                    rows.Add(node + ":" + ordinal + " " + kind + " [" + string.Join(",", run.Choices.Select(x => x.Id)) + "] => " + run.Choices[index].Id);
                    Assert.IsTrue(run.Choose(index)); Assert.IsFalse(run.Choose(index));
                    if (firstTripleNode < 0 && run.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic) == TitanicFistsDefinition.Id && run.Protocols.Rank(ProtocolKind.Conductor) > 0 && run.Protocols.Rank(ProtocolKind.Detonation) > 0) firstTripleNode = node;
                }
                yield return null;
            }
            Assert.AreEqual(RunPhase.ExtractionAvailable, session.Phase);
            rows.Add("Mutation opportunities=" + mutationOpportunities + "; Protocol opportunities=" + protocolOpportunities + "; firstABC=" + firstTripleNode);
            rows.Add("A=" + (run.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic) == TitanicFistsDefinition.Id) + "; B=" + run.Protocols.Rank(ProtocolKind.Conductor) + "; C=" + run.Protocols.Rank(ProtocolKind.Detonation));
            Assert.AreEqual(1, mutationOpportunities); Assert.GreaterOrEqual(protocolOpportunities, 2);
            File.WriteAllLines(Path.Combine(DirectoryFor("b/production"), "path-" + pathIndex + "-tickets.txt"), rows);
            File.WriteAllText(Path.Combine(DirectoryFor("b/production"), "path-" + pathIndex + "-summary.json"), JsonUtility.ToJson(director.CurrentRunSummary, true));
            var portal = director.ManualExtractionPortal; Assert.IsNotNull(portal);
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(portal.transform.position)); Assert.IsTrue(portal.TryEnter());
            float end = Time.realtimeSinceStartup + 8;
            while (SceneManager.GetActiveScene().name != "NexusBlockout") { Assert.Less(Time.realtimeSinceStartup, end); yield return null; }
            yield return null; Assert.AreEqual(RunPhase.Ended, session.Phase);
        }

        [UnityTest] public IEnumerator GPR015GameViewWindInPublishedCombat() => VisualCombat(0);
        [UnityTest] public IEnumerator GPR015GameViewElectricWindInPublishedCombat() => VisualCombat(1);
        [UnityTest] public IEnumerator GPR015GameViewFireWindInPublishedCombat() => VisualCombat(2);
        [UnityTest] public IEnumerator GPR015GameViewTripleInPublishedCombat() => VisualCombat(3);
        private IEnumerator Capture(string label)
        {
            string path = Path.Combine(DirectoryFor("c"), label + ".png"); Assert.IsFalse(File.Exists(path));
            ScreenCapture.CaptureScreenshot(path); float deadline = Time.realtimeSinceStartup + 3;
            while (!File.Exists(path)) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null;
        }
        private static IEnumerator Attack(CharControlScript controls, PlayerActor player)
        {
            float deadline = Time.realtimeSinceStartup + 4;
            while (!controls.TryDirectionalBasicAttack(player.transform.position + Vector3.forward * 10))
            { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            do { yield return null; } while (controls.HasScheduledBasic && Time.realtimeSinceStartup < deadline);
            Assert.IsFalse(controls.HasScheduledBasic);
        }
        private static void GrantForVisualFixture(RunBoons run, string id)
        {
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(run, new object[] { new RunBoons.Offer(id, "fixture", "Assisted visual composition; not natural reward evidence"), false }));
        }
        private IEnumerator VisualCombat(int build)
        {
            yield return LoadProduction();
            var director = Object.FindFirstObjectByType<ProgressionDirector>(); var player = Object.FindFirstObjectByType<PlayerActor>();
            var session = director.ManualSession; var run = player.GetComponent<RunBoons>(); var runtime = player.GetComponent<ProtocolRuntime>();
            var controls = player.GetComponent<CharControlScript>(); player.AddDamageImmunity(director);
            GrantForVisualFixture(run, TitanicFistsDefinition.Id);
            if ((build & 1) != 0) GrantForVisualFixture(run, "conductor");
            if ((build & 2) != 0) GrantForVisualFixture(run, "detonation");
            Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(1).Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, 1)); yield return null;
            PreventCoinPersistence(director);
            var enemies = director.GetComponentsInChildren<Actor>().OrderBy(x => EntityId.ToULong(x.GetEntityId())).ToArray(); Assert.GreaterOrEqual(enemies.Length, 4);
            Vector3 origin = player.transform.position;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i].TryGetComponent<EnemyAI>(out var ai)) ai.enabled = false;
                if (enemies[i].TryGetComponent<EnemyCombatActions>(out var action)) action.Cancel();
                Assert.IsTrue(enemies[i].GetComponent<NavMeshAgent>().Warp(origin + new Vector3(7 + i, 0, 7)));
                enemies[i].SetMaxHealth(1000); enemies[i].RestoreHealthToMax();
            }
            var wind = enemies[0]; var child = enemies[1]; var blast = enemies[2]; var threat = enemies[3];
            Assert.IsTrue(wind.GetComponent<NavMeshAgent>().Warp(origin + Vector3.forward * 1.2f));
            yield return new WaitForSeconds(.25f); yield return Attack(controls, player); yield return Attack(controls, player);
            yield return Capture("build-" + build + "-native-basic");
            Assert.IsTrue(wind.GetComponent<NavMeshAgent>().Warp(origin + Vector3.forward * 5.8f));
            Assert.IsTrue(child.GetComponent<NavMeshAgent>().Warp(origin + new Vector3(2, 0, 5.8f)));
            Assert.IsTrue(blast.GetComponent<NavMeshAgent>().Warp(origin + new Vector3(4, 0, 5.8f)));
            Assert.IsTrue(threat.GetComponent<NavMeshAgent>().Warp(origin + new Vector3(-2, 0, 3)));
            wind.SetMaxHealth(build == 2 ? .5f : 1000); wind.RestoreHealthToMax();
            child.SetMaxHealth(build == 3 ? .5f : 1000); child.RestoreHealthToMax(); blast.SetMaxHealth(1000); blast.RestoreHealthToMax();
            Physics.SyncTransforms();
            using (var receipts = new RunStatistics(player, session.Id, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                foreach (var enemy in enemies) receipts.Watch(enemy);
                yield return Attack(controls, player);
                float deadline = Time.realtimeSinceStartup + 4;
                while (runtime.FirstTitanicPosition.HasValue && runtime.FirstTitanicPosition.Value.z < origin.z + 2.3f)
                { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                Assert.Greater(runtime.PendingTitanicWaves, 0);
                var attack = threat.GetComponent<EnemyCombatActions>();
                var telegraph = attack.StartCoroutine(attack.Perform(EnemyAttackKind.HeavySlam, player, 10, () => true));
                yield return null; Assert.IsTrue(attack.IsWindingUp);
                yield return Capture("build-" + build + "-travel-warning-1");
                yield return new WaitForSeconds(.1f); yield return Capture("build-" + build + "-travel-warning-2");
                while (!receipts.Snapshot().RecentDamage.Any(x => x.ProducerId == TitanicFistsDefinition.Id))
                { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                var data = receipts.Snapshot();
                if (build == 1 || build == 3) Assert.IsTrue(data.RecentDamage.Any(x => x.Producer == "Conductor"));
                if (build == 2 || build == 3) Assert.IsTrue(data.RecentDamage.Any(x => x.Producer == "Detonation"));
                if (build == 3) Assert.IsTrue(data.RecentDamage.Any(x => x.Producer == "Conductor" && x.After == 0));
                Assert.IsTrue(data.RecentDamage.All(x => x.Secondary && x.Ability == "Basic" && x.Parents.Length > 0));
                yield return Capture("build-" + build + "-confirmed-event");
                File.WriteAllText(Path.Combine(DirectoryFor("c"), "build-" + build + "-hp-lineage.json"), JsonUtility.ToJson(data, true));
                File.WriteAllText(Path.Combine(DirectoryFor("c"), "build-" + build + "-context.txt"), "Published FirstSector normal gameplay camera; assisted boon ownership and actor positioning; live EnemyCombatActions.HeavySlam telegraph; seed=" + session.Route.RewardSeed + "; camera=" + Camera.main.name + "; enemies=" + enemies.Length + "; warningAtCapture=" + attack.IsWindingUp);
                attack.Cancel(); attack.StopCoroutine(telegraph); runtime.CloseEncounter(); yield return null;
                Assert.AreEqual(0, runtime.Budget.LiveRoots); Assert.IsFalse(player.GetComponent<TitanicFistsVisual>().WaveActive);
                yield return Capture("build-" + build + "-cleanup");
            }
        }
    }
}
