using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    /// <summary>Fase 1c: rapport by gifts, artifacts, boon locks, fishing and the serene room plan. Temporary folders only.</summary>
    public sealed class Fase1cRapportTests
    {
        private string _directory;
        private MetaProgressStore _store;

        [SetUp]
        public void CreateTemporaryProfile()
        {
            _directory = Path.Combine(Path.GetTempPath(), "TechGuyTests", "Fase1c-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _store = new MetaProgressStore(Path.Combine(_directory, MetaProgressStore.FileName));
        }

        [TearDown]
        public void DeleteTemporaryProfile() { try { Directory.Delete(_directory, true); } catch (Exception) { } }

        private MetaProgressProfile Reload() => new MetaProgressStore(_store.FilePath).Load();

        [Test]
        public void IcoLevelsCostOneOneTwoTwoGiftsAndStopAtFive()
        {
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 3, 4, 4, 5, 5 }, Enumerable.Range(0, 8).Select(Rapport.IcoLevel));
            Assert.AreEqual(6, Rapport.MaxGifts(Rapport.Ico));
            Assert.IsTrue(Rapport.CanReceive(Rapport.Ico, 5)); Assert.IsFalse(Rapport.CanReceive(Rapport.Ico, 6));
            Assert.AreEqual(1, Rapport.IcoGiftsToNextLevel(0)); Assert.AreEqual(2, Rapport.IcoGiftsToNextLevel(2));
            Assert.AreEqual(1, Rapport.IcoGiftsToNextLevel(3)); Assert.AreEqual(0, Rapport.IcoGiftsToNextLevel(6));
            Assert.AreEqual(.5f, Rapport.IcoLevelProgress(3), 1e-4f, "Halfway through the two gifts of level 4.");
            Assert.AreEqual(3, Rapport.MaxGifts(MetaAllies.Unknown)); Assert.AreEqual(0, Rapport.MaxGifts("ninguem"));
        }

        [Test]
        public void TwoGauntletBoonsUnlockAtLevelsTwoAndThreeAndTheFirstOfferPoolIsNeverLocked()
        {
            CollectionAssert.AreEquivalent(new[] { GauntletExpansionCatalog.TitanFallId }, Rapport.BoonsUnlockedAt(2));
            CollectionAssert.AreEquivalent(new[] { GauntletExpansionCatalog.PerfectRhythmId }, Rapport.BoonsUnlockedAt(3));
            Assert.IsFalse(Rapport.IsBoonUnlocked(GauntletExpansionCatalog.TitanFallId, 1));
            Assert.IsTrue(Rapport.IsBoonUnlocked(GauntletExpansionCatalog.TitanFallId, 2));
            Assert.IsFalse(Rapport.IsBoonUnlocked(GauntletExpansionCatalog.PerfectRhythmId, 2));
            foreach (var free in new[] { "conductor", "detonation", "ignite", "frost", BoonReworkCatalog.ViolentFallId, "qualquer_outro",
                         GauntletExpansionCatalog.TemporalEchoId, GauntletExpansionCatalog.EchoingFistId })
                Assert.IsTrue(Rapport.IsBoonUnlocked(free, 1), free + " is never locked.");
        }

        [Test]
        public void GiftsAreBankedGivenOnceEachAndUnlockArtifactsThatCanBeEquipped()
        {
            var started = _store.RecordRunStarted("run-a");
            _store.AddGift("run-a"); _store.AddGift("run-a");
            var saved = Reload();
            Assert.AreEqual(2, saved.gifts); Assert.AreEqual(2, saved.giftsFoundTotal); Assert.AreEqual(2, saved.FindRun("run-a").giftsFound);

            Assert.IsFalse(_store.TryGiveGift("ninguem", out _), "Unknown NPC.");
            Assert.IsTrue(_store.TryGiveGift(Rapport.Ico, out var afterIco));
            Assert.AreEqual(1, afterIco.gifts); Assert.AreEqual(2, Rapport.IcoLevel(afterIco));
            Assert.IsTrue(afterIco.HasArtifact(Artifacts.SparringWrap.Id), "Level 2 hands over the first artifact.");
            Assert.IsTrue(_store.TryGiveGift(MetaAllies.Unknown, out var afterUnknown));
            Assert.IsTrue(afterUnknown.HasArtifact(Artifacts.LuckyHook.Id)); Assert.AreEqual(0, afterUnknown.gifts);
            Assert.IsFalse(_store.TryGiveGift(Rapport.Ico, out _), "No gift left.");

            Assert.IsFalse(_store.TryEquipArtifact(Artifacts.SecondRound.Id, out _), "Not owned yet.");
            Assert.IsTrue(_store.TryEquipArtifact(Artifacts.LuckyHook.Id, out var equipped));
            Assert.IsTrue(equipped.HasEquipped(Artifacts.LuckyHook.Id));
            Assert.IsTrue(_store.TryEquipArtifact("", out var none)); Assert.AreEqual("", none.equippedArtifact);
            Assert.AreEqual(2, Reload().giftsFoundTotal, "Giving does not change what was found.");
        }

        [Test]
        public void AllSixIcoGiftsReachLevelFiveWithTheThreeIcoArtifactsAndThenSheRefuses()
        {
            for (int i = 0; i < 7; i++) _store.AddGift();
            for (int i = 0; i < 6; i++) Assert.IsTrue(_store.TryGiveGift(Rapport.Ico, out _), "Gift " + i);
            Assert.IsFalse(_store.TryGiveGift(Rapport.Ico, out var profile), "Maximum reached.");
            Assert.AreEqual(5, Rapport.IcoLevel(profile)); Assert.AreEqual(1, profile.gifts);
            foreach (var artifact in new[] { Artifacts.SparringWrap, Artifacts.Mouthguard, Artifacts.SecondRound })
                Assert.IsTrue(profile.HasArtifact(artifact.Id), artifact.Name);
            Assert.IsFalse(profile.HasArtifact(Artifacts.LuckyHook.Id));
        }

        [Test]
        public void OldProfilesWithoutTheNewFieldsLoadWithEmptyRapport()
        {
            File.WriteAllText(_store.FilePath, "{\"schemaVersion\":2,\"runsStarted\":4,\"fragments\":30,\"upgrades\":[\"reroll\"]}");
            var profile = _store.Load();
            Assert.AreEqual(0, profile.gifts); Assert.AreEqual(1, Rapport.IcoLevel(profile)); Assert.AreEqual("", profile.equippedArtifact);
            CollectionAssert.IsEmpty(profile.artifacts); Assert.AreEqual(30, profile.fragments); Assert.IsTrue(profile.HasUpgrade("reroll"));
        }

        [Test]
        public void FirstGiftIsGuaranteedThenRareAndTheLuckyHookDoublesTheChance()
        {
            var fresh = new MetaProgressProfile();
            Assert.IsTrue(GiftEconomy.FishingGivesGift(fresh, .999), "The first gift of a profile is guaranteed.");
            var veteran = new MetaProgressProfile { giftsFoundTotal = 1 };
            Assert.IsTrue(GiftEconomy.FishingGivesGift(veteran, .079)); Assert.IsFalse(GiftEconomy.FishingGivesGift(veteran, .081));
            veteran.artifacts.Add(Artifacts.LuckyHook.Id); veteran.equippedArtifact = Artifacts.LuckyHook.Id;
            Assert.IsTrue(GiftEconomy.FishingGivesGift(veteran, .159)); Assert.IsFalse(GiftEconomy.FishingGivesGift(veteran, .161));
        }

        [Test]
        public void FishingPullOnlyCatchesInsideTheBiteWindow()
        {
            Assert.AreEqual(FishingRules.Pull.TooEarly, FishingRules.Resolve(1.0f, 2f));
            Assert.AreEqual(FishingRules.Pull.Caught, FishingRules.Resolve(2.0f, 2f));
            Assert.AreEqual(FishingRules.Pull.Caught, FishingRules.Resolve(2f + FishingRules.BiteWindow, 2f));
            Assert.AreEqual(FishingRules.Pull.TooLate, FishingRules.Resolve(2f + FishingRules.BiteWindow + .01f, 2f));
            Assert.That(FishingRules.BiteTime(0), Is.EqualTo(FishingRules.WaitMin)); Assert.That(FishingRules.BiteTime(1), Is.EqualTo(FishingRules.WaitMax));
        }

        [Test]
        public void SereneRoomOnlyReplacesABranchCombatRoomWithoutMutationAndNeverOppositeTheRepair()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(definition, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out var source, out string error), error);
            var pool = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/EnemyArchetypes" })
                .Select(id => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id))).OrderBy(p => p.name).ToArray();
            int withCandidate = 0;
            for (ulong seed = 0; seed < 64; seed++)
            {
                var route = ManualRunRandomizer.Generate(source, seed, pool);
                var branchIds = route.Groups.SelectMany(g => g.FirstBranch.Concat(g.SecondBranch)).ToList();
                var candidates = SereneRoomPlan.Candidates(route);
                if (candidates.Count > 0) withCandidate++;
                foreach (int id in candidates)
                {
                    var node = route.Node(id);
                    Assert.AreEqual(ManualNodeKind.Combat, node.Kind); CollectionAssert.Contains(branchIds, id);
                    for (int i = 0; i < node.TotalAcquisitions; i++) Assert.AreNotEqual(RunRewardKind.Mutation, node.RewardAt(i));
                    var group = route.Groups.First(g => g.FirstBranch.Contains(id) || g.SecondBranch.Contains(id));
                    var other = group.FirstBranch.Contains(id) ? group.SecondBranch : group.FirstBranch;
                    Assert.IsFalse(other.Any(o => route.Node(o).Kind == ManualNodeKind.Repair), "Never opposite the repair room.");
                }
            }
            Assert.Greater(withCandidate, 32, "Most seeds can host the serene room.");
        }

        [Test]
        public void ResultsSummaryMentionsGiftsFoundAndGiftsToDeliver()
        {
            _store.RecordRunStarted("run-b"); _store.AddGift("run-b");
            string text = MetaProgressSummary.Describe(Reload(), "run-b");
            StringAssert.Contains(Rapport.GiftName + " encontrado nesta run!", text);
            StringAssert.Contains("Você tem 1 " + Rapport.GiftName + " para entregar no Nexus.", text);
            StringAssert.DoesNotContain(Rapport.GiftName, MetaProgressSummary.Describe(new MetaProgressProfile(), "nada"));
        }

        [Test]
        public void EveryConversationAndArtifactExistsForEveryStep()
        {
            for (int given = 0; given <= Rapport.MaxGifts(Rapport.Ico); given++) Assert.Greater(NexusDialogues.Ico(given).Count, 0, "Ico " + given);
            for (int given = 0; given <= Rapport.UnknownMaxLayers; given++) Assert.Greater(NexusDialogues.Unknown(given).Count, 0, "Desconhecido " + given);
            CollectionAssert.AllItemsAreUnique(Artifacts.All.Select(a => a.Id));
            Assert.AreEqual(4, Artifacts.All.Count);
            foreach (var line in Enumerable.Range(0, 7).SelectMany(NexusDialogues.Ico).Concat(Enumerable.Range(0, 4).SelectMany(NexusDialogues.Unknown)))
                StringAssert.DoesNotContain("TODO", line);
        }
    }
}
