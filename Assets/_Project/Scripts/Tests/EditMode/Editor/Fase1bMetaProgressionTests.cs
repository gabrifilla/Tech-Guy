using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests.Fluidity.EditMode
{
    /// <summary>Fase 1b: Fragmentos, MetaProgress v2 migration, terminal upgrades and their run hooks. Temporary folders only.</summary>
    public sealed class Fase1bMetaProgressionTests
    {
        private string _directory;
        private MetaProgressStore _store;

        [SetUp]
        public void CreateTemporaryProfile()
        {
            _directory = Path.Combine(Path.GetTempPath(), "TechGuyTests", "Fase1b-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _store = new MetaProgressStore(Path.Combine(_directory, MetaProgressStore.FileName));
        }

        [TearDown]
        public void DeleteTemporaryProfile()
        {
            WeaponLoadout.LegacyUnlockFlagsForTests = false;
            try { Directory.Delete(_directory, true); } catch (Exception) { }
        }

        private MetaProgressProfile Reload() => new MetaProgressStore(_store.FilePath).Load();

        [Test]
        public void V1ProfileMigratesToV2KeepingCountersWithFragmentsAtZero()
        {
            const string v1 = "{\"schemaVersion\":1,\"runsStarted\":3,\"runsFinished\":3,\"victories\":1,\"deaths\":2,\"abandons\":0," +
                              "\"universesCleared\":[\"universe-1\"],\"lastUpdatedUtc\":\"2026-11-09T22:00:00Z\"}";
            File.WriteAllText(_store.LegacyV1Path, v1);
            var migrated = _store.Load();
            Assert.AreEqual(MetaProgressProfile.CurrentSchemaVersion, migrated.schemaVersion);
            Assert.AreEqual(3, migrated.runsStarted); Assert.AreEqual(1, migrated.victories); Assert.AreEqual(2, migrated.deaths);
            CollectionAssert.AreEqual(new[] { MetaProgressStore.Universe1 }, migrated.universesCleared);
            Assert.AreEqual(0, migrated.fragments, "The legacy coin balance is never migrated.");
            CollectionAssert.IsEmpty(migrated.upgrades); CollectionAssert.IsEmpty(migrated.rescuedAllies); CollectionAssert.IsEmpty(migrated.runHistory);

            _store.AddFragments(4);
            Assert.IsTrue(File.Exists(_store.FilePath), "The first save writes the v2 file.");
            StringAssert.Contains("\"schemaVersion\": 2", File.ReadAllText(_store.FilePath));
            Assert.AreEqual(v1, File.ReadAllText(_store.LegacyV1Path), "The v1 file is left untouched.");
            var reloaded = Reload();
            Assert.AreEqual(3, reloaded.runsStarted); Assert.AreEqual(4, reloaded.fragments);
        }

        [Test]
        public void FragmentsAreBankedPerRunAndKeptRegardlessOfOutcome()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            _store.RecordRunStarted("run-a");
            _store.AddFragments(9, "run-a");
            _store.AddFragments(2, "run-a");
            _store.RecordRunFinished(RunOutcome.Dead, MetaProgressStore.Universe1, false, "run-a");
            _store.RecordRunStarted("run-b");
            _store.AddFragments(5, "run-b");
            _store.RecordRunFinished(RunOutcome.Aborted, MetaProgressStore.Universe1, false, "run-b");
            Assert.IsNull(_store.AddFragments(0, "run-b"), "Non-positive gains are ignored.");

            var profile = Reload();
            Assert.AreEqual(16, profile.fragments); Assert.AreEqual(16, profile.fragmentsEarnedTotal);
            Assert.AreEqual(11, profile.FindRun("run-a").fragmentsEarned); Assert.AreEqual("Death", profile.FindRun("run-a").outcome);
            Assert.AreEqual(5, profile.FindRun("run-b").fragmentsEarned); Assert.AreEqual("Aborted", profile.FindRun("run-b").outcome);
        }

        [Test]
        public void UpgradesAreBoughtOnceAndNeedEnoughFragments()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            _store.AddFragments(25);
            MetaUpgrades.TryGet(MetaUpgrades.Vitality, out var vitality);
            MetaUpgrades.TryGet(MetaUpgrades.CleanRoom, out var cleanRoom);
            Assert.IsTrue(_store.TryPurchase(vitality.Id, vitality.Cost, out var afterBuy));
            Assert.AreEqual(25 - vitality.Cost, afterBuy.fragments);
            Assert.IsFalse(_store.TryPurchase(vitality.Id, vitality.Cost, out _), "Bought once.");
            Assert.IsFalse(_store.TryPurchase(cleanRoom.Id, cleanRoom.Cost, out var shortProfile), "Not enough Fragmentos.");
            Assert.AreEqual(25 - vitality.Cost, shortProfile.fragments);
            var profile = Reload();
            Assert.IsTrue(profile.HasUpgrade(vitality.Id)); Assert.IsFalse(profile.HasUpgrade(cleanRoom.Id));
        }

        [Test]
        public void AllyRescueIsSavedAtOnceAndMarkedOnItsRun()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            _store.RecordRunStarted("run-a");
            _store.RecordAllyRescued(MetaAllies.Unknown, "run-a");
            _store.RecordAllyRescued(MetaAllies.Unknown, "run-a");
            var profile = Reload();
            CollectionAssert.AreEqual(new[] { MetaAllies.Unknown }, profile.rescuedAllies);
            Assert.AreEqual(MetaAllies.Unknown, profile.FindRun("run-a").allyRescued);
            Assert.AreEqual(0, profile.runsFinished, "Saved before the run ends.");
        }

        [Test]
        public void RunHistoryIsCapped()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            for (int i = 0; i < MetaProgressStore.HistoryLimit + 5; i++) _store.RecordRunStarted("run-" + i);
            var profile = Reload();
            Assert.AreEqual(MetaProgressStore.HistoryLimit, profile.runHistory.Count);
            Assert.IsNull(profile.FindRun("run-0")); Assert.IsNotNull(profile.FindRun("run-" + (MetaProgressStore.HistoryLimit + 4)));
        }

        [Test]
        public void TerminalCatalogMatchesTheApprovedEconomy()
        {
            // Revised after the 10/11/2026 playtest: slower economy, no free boon.
            CollectionAssert.AreEqual(new[] { 20, 40, 50, 60, 75, 90 }, MetaUpgrades.All.Select(u => u.Cost));
            CollectionAssert.AreEqual(new[] { MetaUpgrades.Vitality, MetaUpgrades.Reroll, MetaUpgrades.CleanRoom, MetaUpgrades.Repair,
                MetaUpgrades.ExtraReroll, MetaUpgrades.FourthOption }, MetaUpgrades.All.Select(u => u.Id));
            Assert.AreEqual(335, MetaUpgrades.All.Sum(u => u.Cost));
            CollectionAssert.DoesNotContain(MetaUpgrades.All.Select(u => u.Id), MetaUpgrades.RetiredInitialOffer, "A free extra boon is vetoed.");
            Assert.AreEqual(1, FragmentEconomy.CombatRoomCleared); Assert.AreEqual(2, FragmentEconomy.EliteKilled);
            Assert.AreEqual(10, FragmentEconomy.BossVictory); Assert.AreEqual(2, MetaUpgrades.CleanRoomFragments);
        }

        [Test]
        public void RerollUpgradesStackOnePerUpgrade()
        {
            var profile = new MetaProgressProfile();
            Assert.AreEqual(0, MetaUpgrades.RerollsFor(profile));
            profile.upgrades.Add(MetaUpgrades.ExtraReroll); Assert.AreEqual(1, MetaUpgrades.RerollsFor(profile), "Each works alone.");
            profile.upgrades.Add(MetaUpgrades.Reroll); Assert.AreEqual(2, MetaUpgrades.RerollsFor(profile));
        }

        [Test]
        public void RetiredInitialOfferIsRefundedOnLoad()
        {
            var old = new MetaProgressProfile { fragments = 3 };
            old.upgrades.Add(MetaUpgrades.Vitality); old.upgrades.Add(MetaUpgrades.RetiredInitialOffer);
            File.WriteAllText(_store.FilePath, JsonUtility.ToJson(old));
            var loaded = Reload();
            Assert.AreEqual(3 + MetaUpgrades.RetiredRefunds[MetaUpgrades.RetiredInitialOffer], loaded.fragments);
            CollectionAssert.AreEqual(new[] { MetaUpgrades.Vitality }, loaded.upgrades);
            Assert.IsTrue(_store.Save(loaded));
            Assert.AreEqual(loaded.fragments, Reload().fragments, "Refunded once: the saved file no longer owns it.");
        }

        [Test]
        public void SeenDialogueIsSavedAndOldFilesReadAsUnseen()
        {
            var legacy = new MetaProgressProfile();
            string json = JsonUtility.ToJson(legacy).Replace(",\"seenDialogues\":[]", "");
            StringAssert.DoesNotContain("seenDialogues", json);
            File.WriteAllText(_store.FilePath, json);
            Assert.IsFalse(Reload().HasSeenDialogue(MetaAllies.UnknownNexusDialogueKey));
            _store.RecordDialogueSeen(MetaAllies.UnknownNexusDialogueKey);
            _store.RecordDialogueSeen(MetaAllies.UnknownNexusDialogueKey);
            CollectionAssert.AreEqual(new[] { MetaAllies.UnknownNexusDialogueKey }, Reload().seenDialogues);
        }

        [Test]
        public void ResultsProgressShowsGainBalanceNextGoalAndAlly()
        {
            var profile = new MetaProgressProfile { fragments = 9 };
            profile.runHistory.Add(new MetaRunRecord { sessionId = "s", fragmentsEarned = 9, allyRescued = MetaAllies.Unknown });
            string text = MetaProgressSummary.Describe(profile, "s");
            StringAssert.Contains("Fragmentos ganhos nesta run: 9", text);
            StringAssert.Contains("Saldo: 9", text);
            StringAssert.Contains("Vida reforçada — faltam 11 Fragmentos", text);
            StringAssert.Contains("Novo aliado resgatado", text);

            profile.upgrades.Add(MetaUpgrades.Vitality); profile.fragments = 45;
            StringAssert.Contains("Reroll (40) — já dá para comprar", MetaProgressSummary.Describe(profile, "s"));
            profile.upgrades.AddRange(MetaUpgrades.All.Select(u => u.Id));
            StringAssert.Contains("Todas as melhorias", MetaProgressSummary.Describe(profile, "other"));
            Assert.AreEqual(MetaUpgrades.Vitality, MetaUpgrades.NextGoal(new MetaProgressProfile()).Id);
        }

        [Test]
        public void FourthOptionDrawsFourCardsAndRerollIsDeterministicAndLimited()
        {
            var definition = FluidityRouteTestFactory.Create();
            try
            {
                Assert.IsTrue(ManualRouteValidator.TrySnapshot(definition, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out var source, out string error), error);
                (RunRewardPolicy policy, RunRewardTicket ticket, RunRewardRequest request) Open(int count, int rerolls)
                {
                    var route = new ManualRoutePolicy(ManualRunRandomizer.Generate(source, 1701UL, Array.Empty<GameObject>()));
                    var context = new RunActionContext(true);
                    Assert.IsTrue(route.TryCommitDestination(route.Session.Id, 1, context));
                    Assert.IsTrue(route.TryEnterNode(route.Session.Id, 1, context));
                    Assert.IsTrue(route.TryResolveEncounter(route.Session.Id, 1, true, context));
                    var policy = new RunRewardPolicy(route) { OfferCount = count, RerollsRemaining = rerolls };
                    var request = new RunRewardRequest(route.Session.Id, 1, 0);
                    var offers = Enumerable.Range(0, 12).Select(i => new RunBoons.Offer("fase1b_" + i, "Teste " + i, "Fixture")).ToArray();
                    return (policy, policy.Open(request, context, "inventory", offers), request);
                }
                var three = Open(3, 0); var four = Open(4, 1); var fourAgain = Open(4, 1);
                Assert.AreEqual(3, three.ticket.Choices.Count, "Default stays three cards.");
                Assert.AreEqual(4, four.ticket.Choices.Count);
                Assert.IsFalse(three.policy.TryReroll(three.request, new RunActionContext(true)), "No reroll without the upgrade.");

                var before = four.ticket.Choices.Select(o => o.Id).ToArray(); int revision = four.ticket.Revision;
                Assert.IsTrue(four.policy.TryReroll(four.request, new RunActionContext(true)));
                Assert.AreEqual(revision + 1, four.ticket.Revision, "Stale clicks on the old cards are rejected.");
                Assert.AreEqual(4, four.ticket.Choices.Count); Assert.AreEqual(0, four.policy.RerollsRemaining);
                CollectionAssert.AreNotEqual(before, four.ticket.Choices.Select(o => o.Id).ToArray(), "This seed redraws different cards.");
                Assert.IsFalse(four.policy.TryReroll(four.request, new RunActionContext(true)), "One reroll per run.");

                Assert.IsTrue(fourAgain.policy.TryReroll(fourAgain.request, new RunActionContext(true)));
                CollectionAssert.AreEqual(four.ticket.Choices.Select(o => o.Id), fourAgain.ticket.Choices.Select(o => o.Id), "Same seed, same reroll.");
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }

        [Test]
        public void ReinforcedRepairAddsAQuarterCappedAtFullHealth()
        {
            var definition = FluidityRouteTestFactory.Create();
            try
            {
                Assert.IsTrue(ManualRouteValidator.TrySnapshot(definition, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out var source, out string error), error);
                var repair = new RunRepairPolicy(new ManualRoutePolicy(source));
                var node = new ManualNodeDefinition(3, "Reparo", ManualNodeKind.Repair, RunRewardKind.None, 0, Vector3.zero, new Vector2(10, 10), .35f, null);
                Assert.AreEqual(.35f, repair.EffectiveFraction(node), 1e-5f);
                repair.FractionBonus = MetaUpgrades.RepairFractionBonus;
                Assert.AreEqual(.60f, repair.EffectiveFraction(node), 1e-5f);
                repair.FractionBonus = 5f;
                Assert.AreEqual(1f, repair.EffectiveFraction(node), 1e-5f);
                var combat = new ManualNodeDefinition(1, "Combate", ManualNodeKind.Combat, RunRewardKind.Technique, 1, Vector3.zero, new Vector2(10, 10), 0, null);
                Assert.AreEqual(0f, repair.EffectiveFraction(combat), "Rooms without repair never heal.");
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); }
        }

        [Test]
        public void BowAndSpearAreLockedByProgressionAndNeverBought()
        {
            WeaponLoadout.LegacyUnlockFlagsForTests = false;
            Assert.IsTrue(WeaponLoadout.IsUnlocked(0), "The gauntlet is always available.");
            Assert.IsFalse(WeaponLoadout.IsUnlocked(1)); Assert.IsFalse(WeaponLoadout.IsUnlocked(2));
            Assert.IsFalse(WeaponLoadout.TryUnlock(1), "No coin purchase.");
            Assert.AreEqual("Liberada ao avançar nos universos",
                WeaponCardContent.BuildStateLabel(WeaponCardContent.StateKind.ProgressionLocked, 0, "G"));
        }
    }
}
