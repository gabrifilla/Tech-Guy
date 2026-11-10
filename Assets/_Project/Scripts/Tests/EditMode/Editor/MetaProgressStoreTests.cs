using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>Fase 1a: MetaProgress.v1 persistence. Every case uses its own temporary folder.</summary>
    public sealed class MetaProgressStoreTests
    {
        private string _directory;
        private MetaProgressStore _store;

        [SetUp]
        public void CreateTemporaryProfile()
        {
            _directory = Path.Combine(Path.GetTempPath(), "TechGuyTests", "MetaProgressEdit-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
            _store = new MetaProgressStore(Path.Combine(_directory, MetaProgressStore.FileName));
        }

        [TearDown]
        public void DeleteTemporaryProfile()
        {
            try { Directory.Delete(_directory, true); } catch (Exception) { }
        }

        private MetaProgressProfile Reload() => new MetaProgressStore(_store.FilePath).Load();

        [Test]
        public void VictoryPersistsCountersAndClearsUniverseOnce()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            _store.RecordRunStarted();
            _store.RecordRunFinished(RunOutcome.Extracted, MetaProgressStore.Universe1);
            _store.RecordRunStarted();
            _store.RecordRunFinished(RunOutcome.Extracted, MetaProgressStore.Universe1);

            var profile = Reload();
            Assert.AreEqual(MetaProgressProfile.CurrentSchemaVersion, profile.schemaVersion);
            Assert.AreEqual(2, profile.runsStarted); Assert.AreEqual(2, profile.runsFinished);
            Assert.AreEqual(2, profile.victories); Assert.AreEqual(0, profile.deaths); Assert.AreEqual(0, profile.abandons);
            CollectionAssert.AreEqual(new[] { MetaProgressStore.Universe1 }, profile.universesCleared, "A universe is listed once.");
            Assert.IsTrue(DateTime.TryParse(profile.lastUpdatedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out _));
            StringAssert.Contains("\"schemaVersion\": 1", File.ReadAllText(_store.FilePath), "JSON field names are the contract.");
            Assert.IsFalse(File.Exists(_store.FilePath + ".tmp"), "The write-then-swap leaves no temporary file.");
        }

        [Test]
        public void DeathAndAbandonCountWithoutClearingTheUniverse()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            _store.RecordRunStarted(); _store.RecordRunFinished(RunOutcome.Dead, MetaProgressStore.Universe1);
            _store.RecordRunStarted(); _store.RecordRunFinished(RunOutcome.Aborted, MetaProgressStore.Universe1);

            var profile = Reload();
            Assert.AreEqual(2, profile.runsStarted); Assert.AreEqual(2, profile.runsFinished);
            Assert.AreEqual(0, profile.victories); Assert.AreEqual(1, profile.deaths); Assert.AreEqual(1, profile.abandons);
            CollectionAssert.IsEmpty(profile.universesCleared);
        }

        [Test]
        public void BossVictoryCountsAtBossDeathAndTheRunEndDoesNotCountItAgain()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            _store.RecordRunStarted();
            _store.RecordBossVictory(MetaProgressStore.Universe1);
            var atBossDeath = Reload();
            Assert.AreEqual(1, atBossDeath.victories); Assert.AreEqual(0, atBossDeath.runsFinished, "The run is still open at the boss death.");
            CollectionAssert.AreEqual(new[] { MetaProgressStore.Universe1 }, atBossDeath.universesCleared);
            _store.RecordRunFinished(RunOutcome.Extracted, MetaProgressStore.Universe1, bossDefeated: true);
            var closed = Reload();
            Assert.AreEqual(1, closed.victories); Assert.AreEqual(1, closed.runsFinished); Assert.AreEqual(1, closed.universesCleared.Count);
        }

        [Test]
        public void LateDeathOrAbandonAfterTheBossKeepTheVictory()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            _store.RecordRunStarted(); _store.RecordBossVictory(MetaProgressStore.Universe1);
            _store.RecordRunFinished(RunOutcome.Dead, MetaProgressStore.Universe1, bossDefeated: true);
            _store.RecordRunStarted(); _store.RecordBossVictory(MetaProgressStore.Universe1);
            _store.RecordRunFinished(RunOutcome.Aborted, MetaProgressStore.Universe1, bossDefeated: true);
            var profile = Reload();
            Assert.AreEqual(2, profile.victories); Assert.AreEqual(0, profile.deaths); Assert.AreEqual(0, profile.abandons);
            Assert.AreEqual(2, profile.runsFinished);
        }

        [Test]
        public void NoneOutcomeIsIgnored()
        {
            Assert.IsNull(_store.RecordRunFinished(RunOutcome.None, MetaProgressStore.Universe1));
            Assert.IsFalse(File.Exists(_store.FilePath));
        }

        [Test]
        public void MissingFileYieldsFreshProfileWithWarningAndWritesNothing()
        {
            LogAssert.Expect(LogType.Warning, new Regex("No profile at"));
            var profile = _store.Load();
            Assert.AreEqual(0, profile.runsStarted); Assert.AreEqual(0, profile.victories);
            Assert.IsNotNull(profile.universesCleared);
            Assert.IsFalse(File.Exists(_store.FilePath), "Reading never creates the profile.");
        }

        [Test]
        public void CorruptedFileYieldsFreshProfileKeepsACopyAndRecovers()
        {
            const string garbage = "{ \"runsStarted\": 4, ";
            File.WriteAllText(_store.FilePath, garbage);
            LogAssert.Expect(LogType.Warning, new Regex("Unreadable profile"));
            var profile = _store.Load();
            Assert.AreEqual(0, profile.runsStarted);
            Assert.AreEqual(garbage, File.ReadAllText(_store.FilePath + ".corrupt"), "The unreadable bytes are kept aside.");

            LogAssert.Expect(LogType.Warning, new Regex("Unreadable profile"));
            _store.RecordRunStarted();
            Assert.AreEqual(1, Reload().runsStarted, "The next save replaces the corrupted file with a valid profile.");
        }

        [Test]
        public void UnknownSchemaOrNegativeCountersAreRejected()
        {
            File.WriteAllText(_store.FilePath, "{\"schemaVersion\":99,\"runsStarted\":3}");
            LogAssert.Expect(LogType.Warning, new Regex("schemaVersion 99"));
            Assert.AreEqual(0, _store.Load().runsStarted);

            File.WriteAllText(_store.FilePath, "{\"schemaVersion\":1,\"runsStarted\":-2}");
            LogAssert.Expect(LogType.Warning, new Regex("negative counter"));
            Assert.AreEqual(0, _store.Load().runsStarted);
        }

        [Test]
        public void DefaultLocationIsItsOwnFileUnderTheDefaultDirectory()
        {
            Assert.Throws<ArgumentException>(() => new MetaProgressStore(" "));
            var store = MetaProgressStore.CreateDefault();
            Assert.AreEqual(Path.Combine(MetaProgressStore.DefaultDirectory, MetaProgressStore.FileName), store.FilePath);
            Assert.IsFalse(store.FilePath.Contains("Docs"), "The profile never lives in the repository.");
        }
    }
}
