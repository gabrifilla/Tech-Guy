using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.TestTools;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;

namespace TechGuy.Tests
{
    public sealed class RunStatisticsPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private ArsenalProjectileTestRig _rig;
        private PlayerActor _player;
        private RunStatistics _stats;
        private Guid _session;
        private bool _hadSummary;
        private string _oldSummary;
        [SetUp] public void Setup()
        {
            _hadSummary = PlayerPrefs.HasKey(RunSummaryStore.PreferenceKey);
            _oldSummary = PlayerPrefs.GetString(RunSummaryStore.PreferenceKey);
            _rig = new ArsenalProjectileTestRig(); _player = _rig.BuildOwner();
            _session = Guid.NewGuid();
            _stats = NewStatistics(_session);
        }
        private RunStatistics NewStatistics(Guid id) => new RunStatistics(_player, id, 42,
            Time.realtimeSinceStartupAsDouble, Time.timeAsDouble);
        [TearDown] public void Cleanup()
        {
            _stats?.Dispose(); _rig.TearDown();
            if (_hadSummary) PlayerPrefs.SetString(RunSummaryStore.PreferenceKey, _oldSummary);
            else PlayerPrefs.DeleteKey(RunSummaryStore.PreferenceKey);
        }
        private Actor Enemy(float hp = 100)
        {
            var actor = _rig.BuildEnemy(Vector3.forward * 2, hp); _stats.Watch(actor);
            var coins = actor.GetComponent<CoinDrop>(); if (coins) UnityEngine.Object.DestroyImmediate(coins);
            return actor;
        }
        private RunDamageSource Source(string ability = "Q", string mutation = "") =>
            new RunDamageSource(_session.ToString(), "Gauntlet", ability, "cast:1", mutation);
        private static void Damage(Actor target, float amount, RunDamageSource source, Action<Action> publish = null)
        { using (target.CaptureNextDamage(_ => { }, publish, source)) target.TakeDamage(amount); }

        [Test] public void EffectiveHpAndOverkillPartitionExactlyOnceAcrossTargetsAndChildren()
        {
            var a = Enemy(20); var b = Enemy(50); var source = Source("Q", "eclipse_uppercut");
            Damage(a, 7, source); Damage(a, 100, source);
            Damage(b, 8, source.WithProducer("Overflow", "protocol:Overflow", true));
            var s = _stats.Snapshot();
            Assert.That(s.EffectiveDamage, Is.EqualTo(28)); Assert.That(s.Overkill, Is.EqualTo(87));
            Assert.That(s.LargestImpact, Is.EqualTo(13)); Assert.That(s.ConfirmedHpTransactions, Is.EqualTo(3));
            Assert.That(s.EnemiesDefeated, Is.EqualTo(1)); Assert.That(s.PlayerCreditedKills, Is.EqualTo(1));
            Assert.That(s.Damage.Sum(x => x.Effective), Is.EqualTo(s.EffectiveDamage));
            Assert.That(s.Abilities.Single().EffectiveIncludingChildren, Is.EqualTo(28));
            Assert.That(s.Abilities.Single().SecondaryHpTransactions, Is.EqualTo(1));
            Assert.That(s.Damage.All(x => x.MutationId == "eclipse_uppercut"), Is.True);
            Assert.That(s.ObserverErrors, Is.Zero);
        }
        [Test] public void RejectedAndUnattributedHpCannotBeCreditedToThePlayer()
        {
            var a = Enemy(); a.AddDamageImmunity(_player); Damage(a, 10, Source());
            a.RemoveDamageImmunity(_player); Damage(a, 0, Source());
            Damage(a, 3, null); Damage(a, 4, new RunDamageSource(Guid.NewGuid().ToString(), "Bow", "Q", "old"));
            var s = _stats.Snapshot(); Assert.That(s.EffectiveDamage, Is.Zero);
            Assert.That(s.ObservedEnemyHpLoss, Is.EqualTo(7)); Assert.That(s.UnattributedEnemyHpLoss, Is.EqualTo(7));
            Assert.That(s.ConfirmedHpTransactions, Is.Zero);
        }
        [Test] public void QueuedReversedAndRepeatedPublicationsRetainTheirOwnHpTransaction()
        {
            var a = Enemy(); var callbacks = new List<Action>();
            Damage(a, 3, Source("Q"), callbacks.Add); Damage(a, 5, Source("W"), callbacks.Add);
            callbacks[1](); callbacks[0](); callbacks[1]();
            var s = _stats.Snapshot(); Assert.That(s.EffectiveDamage, Is.EqualTo(8));
            Assert.That(s.Abilities.Single(x => x.Ability == "Q").EffectiveIncludingChildren, Is.EqualTo(3));
            Assert.That(s.DuplicateReceiptsIgnored, Is.EqualTo(1)); Assert.That(a.PublishedDamageTransaction, Is.Null);
        }
        [Test] public void ReentrantPublicationRestoresOuterTransactionAndDoesNotLoseEarlierDamage()
        {
            var a = _rig.BuildEnemy(Vector3.forward * 2); bool nested = false;
            a.DamageReceived += (_, __) => { if (!nested) { nested = true; Damage(a, 5, Source("W")); } };
            _stats.Watch(a); Damage(a, 3, Source("Q"));
            var s = _stats.Snapshot(); Assert.That(s.EffectiveDamage, Is.EqualTo(8));
            Assert.That(s.ConfirmedHpTransactions, Is.EqualTo(2)); Assert.That(a.PublishedDamageTransaction, Is.Null);
        }
        [Test] public void DamageReceivedUsesHpLossAndSnapshotsCannotMutateCollector()
        {
            _player.health = 30; _player.TakeDamage(4);
            var a = Enemy(); Damage(a, 6, Source()); var s = _stats.Snapshot(); s.Damage.Clear();
            Assert.That(_stats.Snapshot().DamageReceived, Is.EqualTo(4));
            Assert.That(_stats.Snapshot().Damage.Single().Effective, Is.EqualTo(6));
        }
        [Test] public void MixedBurnOriginsRemainAmbiguousInsteadOfLastWriterAttribution()
        {
            var a = Enemy(); var q = Source("Q"); var w = new RunDamageSource(_session.ToString(), "Gauntlet", "W", "cast:2");
            var mixed = RunDamageSource.MergeBurn(q, w).WithProducer("BurnTick", "ignite", true);
            Damage(a, 3, mixed); var row = _stats.Snapshot().Damage.Single();
            Assert.That(row.Ability, Is.EqualTo("Unattributed")); Assert.That(row.Attribution, Is.EqualTo("mixed_burn_origins"));
            Assert.That(_stats.Snapshot().RecentDamage.Single().Parents, Is.EquivalentTo(new[]{"cast:1", "cast:2"}));
        }
        [Test] public void RealProtocolOverflowKeepsOriginAndAddsOnlyConfirmedChildHp()
        {
            var authoring = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.That(authoring.TryCreateSnapshot(out var catalog, out _), Is.True); UnityEngine.Object.DestroyImmediate(authoring);
            var inventory = new ProtocolInventory(_session, catalog);
            Assert.That(inventory.TryAcquire(_session, ProtocolKind.Ignite, inventory.Revision, _ => true), Is.True);
            Assert.That(inventory.TryAcquire(_session, ProtocolKind.Overflow, inventory.Revision, _ => true), Is.True);
            var runtime = _player.GetComponent<ProtocolRuntime>(); if (!runtime) runtime = _player.gameObject.AddComponent<ProtocolRuntime>();
            runtime.Configure(_player, inventory); runtime.Budget.AdvanceFrame(Time.frameCount + 1, .01f);
            var target = Enemy(); var root = runtime.Budget.BeginRoot(_session, 0, inventory);
            root.AnalyticsSource = Source("W"); var before = new ProtocolHitSnapshot(target);
            Damage(target, 10, root.AnalyticsSource); runtime.SubmitConfirmedImpact(root, target, before, 10, ProtocolProducer.Direct); root.Seal();
            var s = _stats.Snapshot(); Assert.That(s.EffectiveDamage, Is.EqualTo(100-target.health));
            Assert.That(s.Damage.Any(x => x.Secondary && x.Producer == "Overflow" && x.Ability == "W" && x.Effective > 0), Is.True);
            Assert.That(s.Damage.Sum(x => x.Effective), Is.EqualTo(s.EffectiveDamage));
        }
        [Test] public void ThreeConsecutiveSessionsFinalizeAllOutcomesWithoutMixingListenersOrPersistence()
        {
            _stats.Dispose();
            var definition = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");
            Assert.That(ManualRouteValidator.TrySnapshot(definition, WeaponLoadout.LoadSelected(), out var route, out var error), Is.True, error);
            var outcomes = new[]{RunOutcome.Aborted, RunOutcome.Dead, RunOutcome.Extracted};
            for (int i = 0; i < outcomes.Length; i++)
            {
                var policy = new ManualRoutePolicy(route); _session = policy.Session.Id; _stats = NewStatistics(_session);
                var a = Enemy(); Damage(a, i+2, Source());
                if (outcomes[i] == RunOutcome.Extracted) policy.Session.Phase = RunPhase.ExtractionAvailable;
                Assert.That(policy.TryEndRun(_session, outcomes[i], new RunActionContext(outcomes[i] != RunOutcome.Dead)), Is.True);
                _stats.Finish(policy.Session); _stats.Finish(policy.Session);
                Assert.That(RunSummaryStore.TryRead(out var saved, out var diagnostic), Is.True, diagnostic);
                Assert.That(saved.SessionId, Is.EqualTo(_session.ToString())); Assert.That(saved.EffectiveDamage, Is.EqualTo(i+2));
                Assert.That(saved.Outcome, Is.EqualTo(i==0 ? "Aborted" : i==1 ? "Death" : "Victory"));
                Damage(a, 1, Source()); Assert.That(_stats.Snapshot().EffectiveDamage, Is.EqualTo(i+2));
            }
        }
        private ProtocolRuntime Runtime()
        {
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.That(source.TryCreateSnapshot(out var catalog, out _), Is.True); UnityEngine.Object.DestroyImmediate(source);
            var runtime = _player.GetComponent<ProtocolRuntime>() ?? _player.gameObject.AddComponent<ProtocolRuntime>();
            runtime.Configure(_player, new ProtocolInventory(_session, catalog)); return runtime;
        }
        [Test] public void NativePersistentProducerIsNotMisclassifiedAsAProc()
        {
            var runtime = Runtime(); var enemy = Enemy();
            using (_stats.BeginAction("W"))
            using (var execution = runtime.BeginExecution(ProtocolProducer.Persistent))
            using (runtime.OpenExecutionScope(execution)) _player.DealResolvedAttackDamage(enemy, 10);
            var row = _stats.Snapshot().Damage.Single();
            Assert.That(row.Ability, Is.EqualTo("W")); Assert.That(row.Secondary, Is.False);
            Assert.That(row.Producer, Is.EqualTo("Persistent"));
        }
        [UnityTest] public IEnumerator DelayedArrowRetainsOriginAfterTheAmbientActionChangesAndPause()
        {
            Runtime(); var enemy = _rig.BuildEnemy(Vector3.forward * 3); _stats.Watch(enemy);
            ArsenalProjectile arrow;
            using (_stats.BeginAction("Q")) arrow = _rig.FireArrow(Vector3.up, Vector3.forward, 6, false, 0);
            float oldScale = Time.timeScale;
            try
            {
                using (_stats.BeginAction("W"))
                {
                    Time.timeScale = 0;
                    yield return new WaitForSecondsRealtime(.06f);
                    Assert.That(_stats.Snapshot().EffectiveDamage, Is.Zero);
                    Time.timeScale = 1;
                    for (int frame = 0; frame < 180 && arrow && arrow.isActiveAndEnabled; frame++) yield return null;
                }
                var result = _stats.Snapshot();
                Assert.That(result.EffectiveDamage, Is.GreaterThan(0));
                Assert.That(result.Damage.All(x => x.Ability == "Q" && x.PlayerAttributed && !x.Secondary), Is.True);
                Assert.That(result.EffectiveDamage, Is.EqualTo(1000-enemy.health).Within(.001));
            }
            finally { Time.timeScale = oldScale; }
        }
        [Test] public void ReactivatedActorGenerationKeepsBothConfirmedLivesWithoutDuplicateReceipts()
        {
            var a = Enemy(); Damage(a, 3, Source());
            a.gameObject.SetActive(false); a.gameObject.SetActive(true); Damage(a, 4, Source());
            var s = _stats.Snapshot(); Assert.That(s.EffectiveDamage, Is.EqualTo(7));
            Assert.That(s.RecentDamage.Select(x => x.Generation).Distinct().Count(), Is.EqualTo(2));
            Assert.That(s.DuplicateReceiptsIgnored, Is.Zero);
        }
        [Test] public void BurnOriginsFromDifferentSessionsCannotCreditTheCurrentRun()
        {
            var previous = new RunDamageSource(Guid.NewGuid().ToString(), "Gauntlet", "Q", "action:1");
            var mixed = RunDamageSource.MergeBurn(previous, Source()); Damage(Enemy(), 4, mixed);
            Assert.That(_stats.Snapshot().EffectiveDamage, Is.Zero);
            Assert.That(_stats.Snapshot().UnattributedEnemyHpLoss, Is.EqualTo(4));
        }
        [Test] public void CorruptAndUnsupportedStoredSummaryAreRejectedWithoutTouchingOtherPreferences()
        {
            PlayerPrefs.SetString(RunSummaryStore.PreferenceKey, "{\"SchemaVersion\":99}");
            Assert.That(RunSummaryStore.TryRead(out _, out _), Is.False);
            PlayerPrefs.SetString(RunSummaryStore.PreferenceKey, "bad json");
            Assert.That(RunSummaryStore.TryRead(out _, out _), Is.False);
        }
    }
}
