using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class TitanicRoomClearPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        [UnityTest] public IEnumerator RoomClearTerminalKeepsPresentationWithoutReopeningCombat() => Scenario("travel");
        [UnityTest] public IEnumerator RoomClearCosmeticCleansOnDisable() => Scenario("disable");
        [UnityTest] public IEnumerator RoomClearCosmeticCleansOnWeaponSwap() => Scenario("swap");
        [UnityTest] public IEnumerator RoomClearCosmeticCleansOnRunEnd() => Scenario("end");
        [UnityTest] public IEnumerator RoomClearCosmeticCleansOnSceneChange() => Scenario("scene");
        [UnityTest] public IEnumerator RoomClearConductorCannotElectrifyCosmeticAndResumesNextEncounter() => Scenario("travel", true);
        [UnityTest] public IEnumerator RoomClearLastEnemyDiesToTornadoAndCancelsCompositionBeforeTraversal() => Scenario("travel", true, true);
        [UnityTest] public IEnumerator RoomClearGameViewRecordsCosmeticAndNextEncounter() => Scenario("travel", true, false, true);
        [UnityTest, Category("GPR014d")] public IEnumerator RoomClearElementalCosmeticSnapshotAndNextRoom() => Scenario("travel", true, false, true, true);
        [UnityTest, Category("GPR014d")] public IEnumerator RoomClearElementalDisableClearsAllLayers() => Scenario("disable", true, false, false, true);
        [UnityTest, Category("GPR014d")] public IEnumerator RoomClearElementalSwapClearsAllLayers() => Scenario("swap", true, false, false, true);
        [UnityTest, Category("GPR014d")] public IEnumerator RoomClearElementalRunEndClearsAllLayers() => Scenario("end", true, false, false, true);
        [UnityTest, Category("GPR014d")] public IEnumerator RoomClearElementalSceneChangeClearsAllLayers() => Scenario("scene", true, false, false, true);
        [UnityTest, Category("GPR014d")] public IEnumerator RoomClearElementalLastKillRevokesAllCombat() => Scenario("travel", true, true, false, true);

        private static IEnumerator CaptureRoomClear(string name)
        {
            string directory = System.IO.Path.Combine(Application.dataPath,
                "../Docs/validation/artifacts/gameplay-rework/" + UnityEditor.SessionState.GetString("TechGuy.Tests.GprPhase", "gpr-01-2c") + "/room-captures/" + System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));
            System.IO.Directory.CreateDirectory(directory);
            string path = System.IO.Path.Combine(directory, name + ".png"); ScreenCapture.CaptureScreenshot(path);
            float deadline = Time.realtimeSinceStartup + 3;
            while (!System.IO.File.Exists(path)) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null;
        }

        private IEnumerator Scenario(string cleanup, bool conductor = false, bool lastTornadoKill = false, bool capture = false, bool detonation = false)
        {
            yield return SceneManager.LoadSceneAsync("FirstSector");
            yield return null; yield return null; yield return null;
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var run = player.GetComponent<RunBoons>(); var runtime = player.GetComponent<ProtocolRuntime>();
            var controls = player.GetComponent<CharControlScript>(); var session = director.ManualSession;
            player.AddDamageImmunity(director);
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(run, new object[] { new RunBoons.Offer(TitanicFistsDefinition.Id, "fixture", "Assisted, not reward evidence"), false }));
            if (conductor) Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(run, new object[] { new RunBoons.Offer("conductor", "fixture", "Assisted lifecycle composition"), false }));
            if (detonation) Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(run, new object[] { new RunBoons.Offer("detonation", "fixture", "Assisted elemental lifecycle"), false }));
            Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(1).Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, 1)); yield return null;
            var enemies = director.GetComponentsInChildren<Actor>();
            foreach (var enemy in enemies)
            {
                if (enemy.TryGetComponent<EnemyAI>(out var ai)) ai.enabled = false;
                if (enemy.TryGetComponent<EnemyCombatActions>(out var actions)) actions.enabled = false;
                if (enemy.TryGetComponent<NavMeshAgent>(out var agent)) Assert.IsTrue(agent.Warp(player.transform.position + Vector3.right * 8));
                else enemy.transform.position = player.transform.position + Vector3.right * 8;
            }
            if (lastTornadoKill)
            {
                foreach (var enemy in enemies.Skip(1)) enemy.TakeDamage(1000000);
                enemies[0].SetMaxHealth(1); enemies[0].RestoreHealthToMax();
                if (enemies[0].TryGetComponent<NavMeshAgent>(out var agent)) Assert.IsTrue(agent.Warp(player.transform.position + Vector3.forward * 4.5f));
                else enemies[0].transform.position = player.transform.position + Vector3.forward * 4.5f;
                Physics.SyncTransforms();
            }
            int attacks = 0; controls.BasicAttackPerformed += () => attacks++;
            yield return Combo(controls, player);
            Assert.AreEqual(1, runtime.TitanicWavesResolved, "Active encounter emits.");
            if (!lastTornadoKill) foreach (var enemy in enemies) enemy.TakeDamage(1000000);
            float deadline = Time.realtimeSinceStartup + 6;
            while (session.Phase == RunPhase.Encounter || player.IsCombatHitStopped)
            { Assert.Less(Time.realtimeSinceStartup, deadline, "Clear frontier sourceHp=" + (enemies[0] ? enemies[0].health : -1) + " source=" + (enemies[0] ? enemies[0].transform.position.ToString() : "destroyed") + " windEnd=" + runtime.LastTitanicEndPosition + " distance=" + runtime.LastTitanicTravelled); yield return null; }
            if (lastTornadoKill) Assert.IsTrue(enemies[0] == null || enemies[0].IsDead);
            Assert.AreEqual(RunPhase.RewardPending, session.Phase);
            // Published map/reward UI intentionally blocks offense. Resolve the ticket and
            // commit the next destination, but remain physically inside the cleared room.
            while (session.Phase == RunPhase.RewardPending)
            {
                Assert.IsTrue(director.TryOpenManualReward(session.Id, session.CurrentNode, session.RewardOrdinal));
                yield return null; yield return null;
                Assert.IsTrue(run.Choose(0)); yield return null; yield return null;
            }
            Assert.IsTrue(director.TryCommitDestination(session.Id, director.ManualDestinations[0]));
            yield return null; yield return null;
            Assert.AreEqual(RunPhase.Traversal, session.Phase);
            Assert.IsFalse(runtime.IsOpen); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            long roots = runtime.Budget.RootsCreated; int ordinal = session.RewardOrdinal;
            var stats = player.RunStatistics?.Snapshot();
            var staleObject = new GameObject("Completed encounter target — no damage authorized");
            var stale = staleObject.AddComponent<Actor>(); stale.SetMaxHealth(1000); stale.RestoreHealthToMax();
            staleObject.AddComponent<BoxCollider>();
            staleObject.transform.position = player.transform.position + Vector3.forward * 4.5f + Vector3.up * .8f;
            staleObject.AddComponent<ProtocolEncounterMembership>().Configure(session.Id, session.CurrentNode);
            player.RunStatistics?.Watch(stale); Physics.SyncTransforms();
            yield return Combo(controls, player);
            Assert.AreEqual(6, attacks, "All six native attacks were accepted, including the post-clear terminal.");
            Assert.IsTrue(player.GetComponent<TitanicFistsVisual>().WaveActive,
                "A legitimate post-clear terminal must retain wind feedback without a Protocol root.");
            Assert.AreEqual(roots, runtime.Budget.RootsCreated); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            Assert.AreEqual(ordinal, session.RewardOrdinal); Assert.AreEqual(RunPhase.Traversal, session.Phase);
            if (stats != null) Assert.AreEqual(stats.EffectiveDamage, player.RunStatistics.Snapshot().EffectiveDamage);
            Assert.AreEqual(1, runtime.CosmeticTitanicEmissions);
            Assert.AreEqual(0, runtime.Feedback.ActiveCount);
            if (detonation)
            {
                var layers = Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None);
                Assert.AreEqual(3, layers.Count(x => x.name == "Titanic fire identity" && x.enabled));
                Assert.AreEqual(2, layers.Count(x => x.name == "Titanic storm identity" && x.enabled));
            }
            if (capture) yield return CaptureRoomClear("post-clear-cosmetic-no-root");
            var visual = player.GetComponent<TitanicFistsVisual>();
            var position = runtime.FirstTitanicPosition.Value;
            Time.timeScale = 0; yield return new WaitForSecondsRealtime(.08f);
            Assert.AreEqual(position, runtime.FirstTitanicPosition.Value); Time.timeScale = 1;
            if (cleanup == "travel")
            {
                yield return new WaitForSeconds(.9f); Assert.AreEqual(1000, stale.health);
                yield return Combo(controls, player);
                int next = session.PendingDestination;
                Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(next).Center));
                Assert.IsTrue(director.TryEnterNode(session.Id, next)); yield return null;
                Assert.AreEqual(0, runtime.PendingTitanicWaves); Assert.IsFalse(visual.WaveActive);
                controls.CancelCombo();
                int before = runtime.TitanicWavesResolved;
                yield return Combo(controls, player);
                Assert.Greater(runtime.TitanicWavesResolved, before, "Combat tornado resumes in next production encounter.");
                if (capture) yield return CaptureRoomClear("next-room-combat-resumed");
                runtime.CloseEncounter(); yield return null;
            }
            else if (cleanup == "disable") { player.gameObject.SetActive(false); yield return null; }
            else if (cleanup == "swap") { player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1])); yield return null; }
            else if (cleanup == "end") { runtime.EndOwnedState(); yield return null; }
            else { yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null; }
            Assert.AreEqual(1000, stale ? stale.health : 1000);
            if (runtime) { Assert.AreEqual(0, runtime.PendingTitanicWaves); Assert.AreEqual(0, runtime.Budget.LiveRoots); }
            Assert.IsTrue(!visual || !visual.WaveActive);
            if (staleObject) Object.Destroy(staleObject);
        }

        private static IEnumerator Combo(CharControlScript controls, PlayerActor player)
        {
            for (int i = 0; i < 3; i++)
            {
                float deadline = Time.realtimeSinceStartup + 3;
                while (!controls.TryDirectionalBasicAttack(player.transform.position + Vector3.forward * 5))
                { Assert.Less(Time.realtimeSinceStartup, deadline, "Attack acceptance i=" + i + " blocked=" + player.GetComponent<AbilityHolder>().BlocksWorldInput + " scale=" + Time.timeScale + " scheduled=" + controls.HasScheduledBasic + " weapon=" + player.CurrentWeapon.name + " phase=" + player.GetComponent<RunBoons>().ProtocolSession.Phase + " choosing=" + player.GetComponent<RunBoons>().IsChoosing); yield return null; }
                do { yield return null; } while (controls.HasScheduledBasic && Time.realtimeSinceStartup < deadline);
                Assert.IsFalse(controls.HasScheduledBasic);
            }
        }
    }
}
