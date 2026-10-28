using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    /// <summary>Real published encounter/frontier and next-room cleanup, with explicit inventory injection.
    /// This exercises execution/lifecycle, not the probability of acquiring the selected Mutation.</summary>
    public sealed class BoonPublishedEncounterPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private readonly string[] _keys = { WeaponLoadout.PreferenceKey, "TechGuy.Loadout.Unlocked.1", "TechGuy.Loadout.Unlocked.2" };
        private readonly bool[] _had = new bool[3];
        private readonly int[] _values = new int[3];
        private float _scale, _capture;

        [SetUp]
        public void Setup()
        {
            _scale = Time.timeScale; _capture = Time.captureDeltaTime;
            Time.timeScale = 1; Time.captureDeltaTime = 1f / 60f;
            for (int i = 0; i < _keys.Length; i++)
            { _had[i] = PlayerPrefs.HasKey(_keys[i]); _values[i] = PlayerPrefs.GetInt(_keys[i]); }
            PlayerPrefs.SetInt(_keys[1], 1); PlayerPrefs.SetInt(_keys[2], 1);
        }

        [TearDown]
        public void Teardown()
        {
            for (int i = 0; i < _keys.Length; i++)
                if (_had[i]) PlayerPrefs.SetInt(_keys[i], _values[i]); else PlayerPrefs.DeleteKey(_keys[i]);
            Time.timeScale = _scale; Time.captureDeltaTime = _capture;
        }

        [UnityTest]
        public IEnumerator PublishedBowLastMineOpensOneRewardAndLeavesNoWorkInTheNextRoom() => LastPersistentHit(1);

        [UnityTest]
        public IEnumerator PublishedSpearLastOrbitHitOpensOneRewardAndLeavesNoWorkInTheNextRoom() => LastPersistentHit(2);

        [UnityTest]
        public IEnumerator PublishedGauntletUsesBaseQThenUppercutFallAndEarlyAsuraOnRealActors()
        {
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            yield return SceneManager.LoadSceneAsync("FirstSector");
            yield return null; yield return null; yield return null;
            var director = Object.FindAnyObjectByType<ProgressionDirector>();
            var player = Object.FindAnyObjectByType<PlayerActor>();
            var run = player.GetComponent<RunBoons>(); var holder = player.GetComponent<AbilityHolder>();
            var combat = player.GetComponent<BreakerGauntletCombat>(); var runtime = player.GetComponent<ProtocolRuntime>();
            var session = director.ManualSession;
            player.AddDamageImmunity(director);
            Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(1).Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, 1)); yield return null;
            var enemies = director.GetComponentsInChildren<Actor>().Where(a => a.Affiliation == CombatAffiliation.Enemy && !a.IsDead).ToArray();
            Assert.Greater(enemies.Length, 0); FreezeEnemies(enemies);
            var target = enemies[0]; foreach (var enemy in enemies.Skip(1)) enemy.TakeDamage(1000000);
            if (target.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;
            target.health = 10000;
            typeof(PlayerActor).GetField("baseMaxMana", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 1000f);
            typeof(PlayerActor).GetField("_manaRegenerationPercentPerSecond", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 0f);
            player.RefreshResourceStats(true); player.Stats.criticalChance = 0;
            // Aim is deterministic here; actual rebind/hold input has separate PlayerLoop coverage.
            // The tagged camera remains enabled/rendering, and its tag is restored in finally.
            var camera = Camera.main; string cameraTag = camera ? camera.tag : null;
            try
            {
                if (camera) camera.tag = "Untagged";
                player.transform.rotation = Quaternion.identity;
                // Base Q contacts after its forward advance; Uppercut below is stationary.
                float baseReach = ((BreakerGauntletAbility)holder.ActiveAbilities[0]).AdvanceDistance + 1.2f;
                target.transform.position = player.transform.position + Vector3.forward * baseReach; Physics.SyncTransforms();
                float baseBefore = target.health;
                Assert.IsTrue(holder.TryUseAbility(0));
                yield return WaitFor(() => !holder.IsCasting && !player.IsCombatHitStopped, "The boon-free published Q must complete.");
                Assert.Less(target.health, baseBefore, "The boon-free kit must still confirm damage.");
                float baseDamage = baseBefore - target.health;
                Assert.IsTrue(run.WeaponModifiers.GauntletMutations.Select(GauntletMutation.EclipseUppercut));
                Assert.IsTrue(run.WeaponModifiers.TryAcquireViolentFall());
                Assert.IsTrue(run.WeaponModifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura));
                holder.RefreshLoadout(); player.RefreshResourceStats(true);
                player.transform.rotation = Quaternion.identity;
                target.transform.position = player.transform.position + Vector3.forward * 1.2f; Physics.SyncTransforms();
                Assert.IsTrue(holder.TryUseAbility(0));
                yield return WaitFor(() => !holder.IsCasting && !player.IsCombatHitStopped, "The published Uppercut must complete.");
                Assert.AreEqual(1, combat.EclipseMarkCount);
                int hits = player.TryApplyGauntletBasicDamage(player.transform.position, Vector3.forward, 3, new Vector3(4, 4, 3), ~0, 10,
                    new HitReactionRequest(player, Vector3.forward, Vector3.forward, HitReactionType.None, HitStrength.Light, 12, StanceBreakEffect.None, 0, 0, 0, 0), true);
                Assert.Greater(hits, 0);
                yield return WaitFor(() => combat.EclipseFallsEmitted == 1 && !player.IsCombatHitStopped, "The confirmed basic finisher must trigger one Fall.");
                var indices = new System.Collections.Generic.List<int>();
                target.DamageReceived += (_, __) =>
                {
                    var context = player.CurrentGauntletImpactContext;
                    if (context?.Slot == GauntletActionSlot.R && context.Origin == GauntletImpactOrigin.Direct) indices.Add(context.ImpactIndex);
                };
                float mana = player.mana, cost = holder.ActiveAbilities[3].ManaCost;
                combat.AddAsuraEnergy(100); Assert.IsTrue(holder.TryUseAbility(3));
                yield return WaitFor(() => holder.CanFinishCadencedAsura, "The real published Asura must expose its free wait.");
                Assert.IsTrue(holder.TryFinishCadencedAsura());
                yield return WaitFor(() => !holder.IsCasting && combat.PendingChildCount == 0, "The early finalizer must release its work.");
                CollectionAssert.AreEqual(new[] { 0, 2 }, indices);
                Assert.AreEqual(mana - cost, player.mana, .001f); Assert.AreEqual(0, runtime.Budget.LiveRoots);
                string evidence = "weapon=Gauntlet; scene=FirstSector; baseQDamage=" + baseDamage +
                    "; uppercutConfirmed=true; falls=" + combat.EclipseFallsEmitted + "; asuraIndices=0,2; oneRCost=true; liveRoots=0; inventoryInjection=true";
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/validation/artifacts/boons/V1"));
                Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, "Gauntlet-published-combat.txt"), evidence);
                TestContext.Progress.WriteLine(evidence);
            }
            finally { if (camera) camera.tag = cameraTag; }
        }

        private static object Field(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

        private static void FreezeEnemies(Actor[] enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy.TryGetComponent(out EnemyAI ai)) ai.enabled = false;
                if (enemy.TryGetComponent(out EnemyCombatActions actions)) actions.enabled = false;
                if (enemy.TryGetComponent(out NavMeshAgent agent) && agent.enabled && agent.isOnNavMesh) agent.isStopped = true;
            }
        }

        private static IEnumerator WaitFor(Func<bool> condition, string reason)
        {
            float deadline = Time.realtimeSinceStartup + 12;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(condition(), reason);
        }

        private IEnumerator LastPersistentHit(int weapon)
        {
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, weapon);
            yield return SceneManager.LoadSceneAsync("FirstSector");
            yield return null; yield return null; yield return null;
            var director = Object.FindAnyObjectByType<ProgressionDirector>();
            var player = Object.FindAnyObjectByType<PlayerActor>();
            Assert.IsNotNull(director); Assert.IsNotNull(player);
            var run = player.GetComponent<RunBoons>(); var holder = player.GetComponent<AbilityHolder>();
            var runtime = player.GetComponent<ProtocolRuntime>(); var session = director.ManualSession;
            Assert.AreEqual((RunWeaponFamily)weapon, run.WeaponModifiers.Family);
            player.AddDamageImmunity(director);
            Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(1).Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, 1)); yield return null;
            var enemies = director.GetComponentsInChildren<Actor>().Where(a => a.Affiliation == CombatAffiliation.Enemy && !a.IsDead).ToArray();
            Assert.Greater(enemies.Length, 1); FreezeEnemies(enemies);
            var last = enemies[0];
            foreach (var enemy in enemies.Skip(1)) enemy.TakeDamage(1000000);
            Assert.AreEqual(RunPhase.Encounter, session.Phase);
            if (last.TryGetComponent(out NavMeshAgent lastAgent)) lastAgent.enabled = false;
            last.transform.position = player.transform.position + Vector3.back * 8;
            last.health = 1; Physics.SyncTransforms();

            string id = weapon == 1 ? BowBoonCatalog.MineGardenId : SpearBoonCatalog.OrbitId;
            Assert.IsTrue(BoonExpansionCatalog.TryAcquire(id, run.WeaponModifiers, run.RunWeapon, run.Protocols, out string error), error);
            holder.RefreshLoadout();
            typeof(PlayerActor).GetField("_manaRegenerationPercentPerSecond", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(player, 0f);
            player.RefreshResourceStats(true);
            var bow = player.GetComponent<BowBoonCombat>(); var spear = player.GetComponent<SpearBoonCombat>();
            int deaths = 0;
            last.Died += _ => { deaths++; Assert.AreEqual(RunPhase.Encounter, session.Phase, "Death notification precedes encounter frontier closure."); };
            float mana = player.mana; long roots = runtime.Budget.RootsCreated;
            int slot = weapon == 1 ? 2 : 3;
            Assert.IsTrue(holder.TryUseAbility(slot));
            yield return WaitFor(() => weapon == 1 ? bow.ActiveMines > 0 : spear.HasOrbit, "The real published skill must create its persistent producer.");
            Vector3 contact;
            if (weapon == 1)
            {
                var mines = (IList)Field(bow, "_mines");
                contact = (Vector3)mines[0].GetType().GetField("Position").GetValue(mines[0]);
            }
            else
            {
                var orbit = Field(spear, "_orbit");
                contact = (Vector3)orbit.GetType().GetField("Point").GetValue(orbit);
            }
            last.transform.position = contact; Physics.SyncTransforms();
            yield return WaitFor(() => session.Phase != RunPhase.Encounter, "The last persistent hit must resolve the actual encounter.");
            Assert.AreEqual(1, deaths); Assert.AreEqual(RunPhase.RewardPending, session.Phase);
            Assert.AreEqual(0, session.RewardOrdinal); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            Assert.AreEqual(roots + 1, runtime.Budget.RootsCreated, "One accepted skill owns its entire persistent execution.");
            Assert.AreEqual(mana - holder.ActiveAbilities[slot].ManaCost, player.mana, .001f);
            if (bow) { Assert.AreEqual(0, bow.ActiveMines); Assert.IsFalse(bow.IsExecuting); Assert.Greater(bow.MinesDetonated, 0); }
            if (spear) { Assert.IsFalse(spear.HasOrbit); Assert.AreEqual(0, spear.ActiveStakes); Assert.IsFalse(spear.HasHorizon); }
            yield return null; Assert.AreEqual(0, session.RewardOrdinal, "The death cannot resolve the ticket twice.");

            float rewardDeadline = Time.realtimeSinceStartup + 8;
            while (session.Phase == RunPhase.RewardPending)
            {
                Assert.Less(Time.realtimeSinceStartup, rewardDeadline);
                yield return null;
                if (!run.IsChoosing) Assert.IsTrue(director.TryOpenManualReward(session.Id, 1, session.RewardOrdinal));
                if (run.Choices.Count > 0) run.Choose(0); // Real selection gate may require one more frame.
            }
            int next = session.Route.Connections.Where(c => c.From == 1).Select(c => c.To).First();
            Assert.IsTrue(director.TryCommitDestination(session.Id, next));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(next).Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, next)); yield return null;
            var nextEnemies = director.GetComponentsInChildren<Actor>().Where(a => a.Affiliation == CombatAffiliation.Enemy && !a.IsDead).ToArray();
            Assert.Greater(nextEnemies.Length, 0); FreezeEnemies(nextEnemies);
            var health = nextEnemies.Select(a => a.health).ToArray();
            yield return new WaitForSeconds(.3f);
            CollectionAssert.AreEqual(health, nextEnemies.Select(a => a.health).ToArray(), "Old projections must not damage the new encounter.");
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
            if (bow) Assert.AreEqual(0, bow.ActiveMines);
            if (spear) Assert.IsFalse(spear.HasOrbit);
            string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/validation/artifacts/boons/V1"));
            Directory.CreateDirectory(directory);
            string evidence = "weapon=" + run.WeaponModifiers.Family + "; producer=" + id + "; deaths=" + deaths +
                "; originalEncounter=1; nextEncounter=" + next + "; phase=" + session.Phase + "; liveRoots=" + runtime.Budget.LiveRoots +
                "; nextEnemies=" + nextEnemies.Length + "; residualDamage=0; inventoryInjection=true";
            File.WriteAllText(Path.Combine(directory, run.WeaponModifiers.Family + "-published-frontier.txt"), evidence);
            TestContext.Progress.WriteLine(evidence);
        }
    }
}
