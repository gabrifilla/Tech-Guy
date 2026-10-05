using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class ProtocolProjectileContextTests
    {
        private ArsenalProjectileTestRig _rig;
        private float _savedTimeScale;
        [SetUp] public void SetUp() { _savedTimeScale = Time.timeScale; _rig = new ArsenalProjectileTestRig(); }
        [TearDown] public void TearDown() { Time.timeScale = _savedTimeScale; _rig?.TearDown(); }
        private ProtocolRuntime BuildRuntime(params ProtocolKind[] kinds)
        {
            var player = _rig.BuildOwner();
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.That(source.TryCreateSnapshot(out var catalog, out _), Is.True); UnityEngine.Object.DestroyImmediate(source);
            var inventory = new ProtocolInventory(Guid.NewGuid(), catalog);
            foreach (var kind in kinds) Assert.That(inventory.TryAcquire(inventory.SessionId, kind, inventory.Revision, _ => true), Is.True);
            var runtime = player.gameObject.AddComponent<ProtocolRuntime>(); runtime.Configure(player, inventory); return runtime;
        }
        [UnityTest] public IEnumerator PiercingProjectileRetainsRootAndRanksAcrossFramesAndReleasesLease()
        {
            var player = _rig.BuildOwner();
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.That(source.TryCreateSnapshot(out var catalog, out _), Is.True);
            UnityEngine.Object.DestroyImmediate(source);
            var inventory = new ProtocolInventory(Guid.NewGuid(), catalog);
            Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, inventory.Revision, _ => true), Is.True);
            Assert.That(inventory.TryAcquire(inventory.SessionId, ProtocolKind.Overflow, inventory.Revision, _ => true), Is.True);
            var runtime = player.gameObject.AddComponent<ProtocolRuntime>(); runtime.Configure(player, inventory);
            var first = _rig.BuildEnemy(new Vector3(0, 0, 2));
            var second = _rig.BuildEnemy(new Vector3(0, 0, 4));
            var arrow = _rig.FireArrow(Vector3.up, Vector3.forward, 6, true, 0);
            var execution = (ProtocolExecution)typeof(ArsenalProjectile).GetField("_protocolExecution", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(arrow);
            Assert.That(execution.IsValid, Is.True);
            var root = execution.Root;
            Assert.That(runtime.Budget.LiveRoots, Is.EqualTo(1));
            inventory.TryAcquire(inventory.SessionId, ProtocolKind.Ignite, inventory.Revision, _ => true);
            for (int frame = 0; frame < 180 && arrow.isActiveAndEnabled; frame++) yield return null;
            Assert.That(_rig.DamageEvents, Does.Contain(first));
            Assert.That(_rig.DamageEvents, Does.Contain(second));
            Assert.That(root.Rank(ProtocolKind.Ignite), Is.EqualTo(1));
            Assert.That(root.ImpactAttempts, Is.EqualTo(2), "Both Overflow impacts belong to the launched projectile's root.");
            Assert.That(execution.IsValid, Is.False);
            Assert.That(root.IsClosed, Is.True);
            Assert.That(runtime.Budget.LiveRoots, Is.Zero);
        }
        [UnityTest] public IEnumerator SiblingProjectilesAndMultitargetAreaShareOneExecution()
        {
            var runtime = BuildRuntime(ProtocolKind.Ignite, ProtocolKind.Overflow);
            var left = _rig.BuildEnemy(new Vector3(-1, 0, 2));
            var right = _rig.BuildEnemy(new Vector3(1, 0, 2));
            long previous = runtime.Budget.RootsCreated;
            using (var execution = runtime.BeginExecution())
            using (runtime.OpenExecutionScope(execution))
            {
                var first = _rig.FireArrow(Vector3.up, left.transform.position.normalized, 4, false, 0);
                var second = _rig.FireArrow(Vector3.up, right.transform.position.normalized, 4, false, 0);
                var field = typeof(ArsenalProjectile).GetField("_protocolExecution", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(((ProtocolExecution)field.GetValue(first)).Root, Is.SameAs(((ProtocolExecution)field.GetValue(second)).Root));
                Assert.That(runtime.Budget.RootsCreated - previous, Is.EqualTo(1));
            }
            for (int frame = 0; frame < 180 && runtime.Budget.LiveRoots > 0; frame++) yield return null;
            Assert.That(_rig.DamageEvents, Does.Contain(left)); Assert.That(_rig.DamageEvents, Does.Contain(right));
            previous = runtime.Budget.RootsCreated;
            int hits = _rig.Owner.TryApplyAreaDamage(Vector3.zero, Vector3.forward, 3,
                new Vector3(4, 2, 3), Physics.DefaultRaycastLayers, 10);
            Assert.That(hits, Is.EqualTo(2));
            Assert.That(runtime.Budget.RootsCreated - previous, Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator BurnRefreshKeepsStrongestReactorDpsAndTickClockAndPauseFreezesTimers()
        {
            var runtime = BuildRuntime(ProtocolKind.Ignite, ProtocolKind.Reactor);
            var target = _rig.BuildEnemy(Vector3.forward * 20);
            yield return null;
            _rig.Owner.DealResolvedAttackDamage(target, 10);
            var status = target.GetComponent<ProtocolElementStatus>();
            Assert.That(status.BurnDps, Is.EqualTo(7.2f).Within(.001));
            float started = runtime.Budget.ScaledTime;
            float timeout = Time.realtimeSinceStartup + 5;
            while (runtime.Budget.ScaledTime - started < .3f && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(target.health, Is.EqualTo(990).Within(.001), "No tick before 0.5 seconds.");
            _rig.Owner.DealResolvedAttackDamage(target, 3);
            Assert.That(status.BurnDps, Is.EqualTo(7.2f).Within(.001));
            Time.timeScale = 0;
            yield return null;
            float remaining = status.BurnRemaining;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(status.BurnRemaining, Is.EqualTo(remaining).Within(.001));
            Time.timeScale = _savedTimeScale;
            timeout = Time.realtimeSinceStartup + 5;
            while (runtime.Budget.ScaledTime - started < .52f && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(target.health, Is.EqualTo(983.4f).Within(.01), "Refresh must not restart the first tick clock.");
            timeout = Time.realtimeSinceStartup + 8;
            while (runtime.Budget.ScaledTime - started < 4.45f && Time.realtimeSinceStartup < timeout) yield return null;
            Assert.That(status.HasBurn, Is.False); Assert.That(runtime.BurnApplications, Is.Zero);
            float expiredHp = target.health;
            yield return new WaitForSeconds(.3f);
            Assert.That(target.health, Is.EqualTo(expiredHp), "Expired applications cannot deliver later ticks.");
        }
        [Test] public void FrostRespectsSmallHeavyEliteLegendaryAndBossProfiles()
        {
            var runtime = BuildRuntime(ProtocolKind.Frost); runtime.SetChillRollForTests(() => 0);
            var ranks = new[] { EnemyRank.Normal, EnemyRank.Normal, EnemyRank.Elite, EnemyRank.Legendary, EnemyRank.Boss };
            var categories = new[] { ReactionCategory.SmallMob, ReactionCategory.MediumMob, ReactionCategory.Elite, ReactionCategory.Elite, ReactionCategory.Boss };
            var speeds = new[] { 0f, 2f, 2.6f, 2.6f, 3.4f };
            for (int i = 0; i < ranks.Length; i++)
            {
                var target = _rig.BuildEnemy(Vector3.forward * (20 + i));
                var agent = target.gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>(); agent.speed = 4;
                var reaction = target.gameObject.AddComponent<CombatReactionController>();
                reaction.ConfigureReaction(new ReactionConfiguration(ranks[i], categories[i]));
                _rig.Owner.DealResolvedAttackDamage(target, 10);
                Assert.That(target.GetComponent<ProtocolElementStatus>().IsFrozen, Is.EqualTo(i == 0));
                Assert.That(agent.speed, Is.EqualTo(speeds[i]).Within(.001));
                runtime.CloseEncounter();
                Assert.That(agent.speed, Is.EqualTo(4).Within(.001), "Cleanup restores only the owned speed contribution.");
            }
        }
        [UnityTest] public IEnumerator HitstopFreezesBurnChillAndVisualTimersAndClosingInvalidatesReservedProjectile()
        {
            var runtime = BuildRuntime(ProtocolKind.Ignite, ProtocolKind.Frost, ProtocolKind.Overflow);
            runtime.SetChillRollForTests(() => 0);
            var target = _rig.BuildEnemy(Vector3.forward * 20);
            yield return null; _rig.Owner.DealResolvedAttackDamage(target, 10);
            var status = target.GetComponent<ProtocolElementStatus>();
            _rig.Owner.GetComponent<HitStopRunner>().Apply(.3f);
            float burn = status.BurnRemaining, chill = status.ChillRemaining;
            int visuals = runtime.Feedback.ActiveCount; float hp = target.health;
            yield return new WaitForSecondsRealtime(.15f);
            Assert.That(status.BurnRemaining, Is.EqualTo(burn).Within(.001));
            Assert.That(status.ChillRemaining, Is.EqualTo(chill).Within(.001));
            Assert.That(target.health, Is.EqualTo(hp)); Assert.That(runtime.Feedback.ActiveCount, Is.EqualTo(visuals));
            while (_rig.Owner.IsCombatHitStopped) yield return null;
            var arrow = _rig.FireArrow(Vector3.up, Vector3.forward, 4, false, 0);
            var execution = (ProtocolExecution)typeof(ArsenalProjectile).GetField("_protocolExecution", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(arrow);
            runtime.CloseEncounter(); Assert.That(execution.IsLifecycleCurrent, Is.False);
            yield return null; Assert.That(arrow.isActiveAndEnabled, Is.False);
            Assert.That(runtime.BurnApplications, Is.Zero); Assert.That(runtime.Feedback.ActiveCount, Is.Zero);
        }
        [Test] public void HeavyChillCleanupDoesNotOverwriteAnotherSpeedOwner()
        {
            var runtime = BuildRuntime(ProtocolKind.Frost); runtime.SetChillRollForTests(() => 0);
            var target = _rig.BuildEnemy(Vector3.forward * 20);
            var agent = target.gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>(); agent.speed = 4;
            target.gameObject.AddComponent<CombatReactionController>().ConfigureReaction(new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob));
            _rig.Owner.DealResolvedAttackDamage(target, 10); Assert.That(agent.speed, Is.EqualTo(2));
            agent.speed = 7; runtime.CloseEncounter(); Assert.That(agent.speed, Is.EqualTo(7));
        }
        [Test] public void FullRootDropsOnlyProtocolDamageAndPreservesEveryNativeHpReceipt()
        {
            var runtime = BuildRuntime(ProtocolKind.Ignite, ProtocolKind.Overflow);
            var target = _rig.BuildEnemy(Vector3.forward * 20);
            using (var execution = runtime.BeginExecution())
            using (runtime.OpenExecutionScope(execution))
            {
                for (int i = 0; i < 33; i++) _rig.Owner.DealResolvedAttackDamage(target, 10);
                Assert.That(execution.Root.ImpactAttempts, Is.EqualTo(32));
                Assert.That(target.health, Is.EqualTo(510).Within(.001));
                Assert.That(runtime.Budget.DroppedImpacts, Is.GreaterThan(0));
            }
            Assert.That(runtime.Budget.LiveRoots, Is.Zero);
        }
        [UnityTest] public IEnumerator SaturatedBurnTicksRotateWithoutCatchupOrStarvationAndDeathTicksDetonate()
        {
            var runtime = BuildRuntime(ProtocolKind.Ignite, ProtocolKind.Detonation);
            var targets = new System.Collections.Generic.List<Actor>();
            yield return null;
            using (_rig.Owner.BeginImpactFeedback(0))
            using (runtime.BeginAreaScope(null))
                for (int i = 0; i < 256; i++)
                { var target = _rig.BuildEnemy(new Vector3(100 + i * 10, 0, 0)); targets.Add(target); _rig.Owner.DealResolvedAttackDamage(target, 10); }
            float started = runtime.Budget.ScaledTime, deadline = Time.realtimeSinceStartup + 6;
            while (runtime.Budget.ScaledTime - started < .6f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(targets.FindAll(actor => actor.health < 990).Count, Is.EqualTo(64));
            Assert.That(runtime.Budget.DroppedTicks, Is.GreaterThan(0));
            while (runtime.Budget.ScaledTime - started < 2.15f && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(targets.FindAll(actor => actor.health < 990).Count, Is.EqualTo(256), "Every group gets serviced before expiry.");
            runtime.CloseEncounter();
            var source = _rig.BuildEnemy(Vector3.forward * 30); var neighbor = _rig.BuildEnemy(Vector3.forward * 30 + Vector3.right * 2);
            using (_rig.Owner.BeginImpactFeedback(0)) _rig.Owner.DealResolvedAttackDamage(source, 10);
            source.health = 2;
            deadline = Time.realtimeSinceStartup + 3;
            while (!source.IsDead && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(source.IsDead, Is.True); Assert.That(neighbor.health, Is.EqualTo(998.5f).Within(.001));
            Assert.That(runtime.Budget.LiveRoots, Is.Zero);
        }
        [UnityTest] public IEnumerator NextSessionInvalidatesOldProjectileAndClearsOwnedApplications()
        {
            var runtime = BuildRuntime(ProtocolKind.Ignite);
            var target = _rig.BuildEnemy(Vector3.forward * 20);
            yield return null; _rig.Owner.DealResolvedAttackDamage(target, 10);
            var arrow = _rig.FireArrow(Vector3.up, Vector3.forward, 4, false, 0);
            var execution = (ProtocolExecution)typeof(ArsenalProjectile).GetField("_protocolExecution", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(arrow);
            var catalog = runtime.Inventory.Catalog;
            var next = new ProtocolInventory(Guid.NewGuid(), catalog);
            runtime.EndOwnedState(); runtime.EndOwnedState(); runtime.Configure(_rig.Owner, next);
            Assert.That(execution.IsLifecycleCurrent, Is.False); Assert.That(runtime.BurnApplications, Is.Zero);
            Assert.That(ProtocolElementQuery.HasBurn(target), Is.False); Assert.That(runtime.Budget.LiveRoots, Is.Zero);
            yield return null; Assert.That(arrow.isActiveAndEnabled, Is.False);
            Assert.That(next.Revision, Is.Zero); Assert.That(target.health, Is.EqualTo(990));
        }
        [UnityTest] public IEnumerator FrostExpiresAndRestoresOwnedSpeed()
        {
            var runtime = BuildRuntime(ProtocolKind.Frost); runtime.SetChillRollForTests(() => 0);
            var target = _rig.BuildEnemy(Vector3.forward * 20);
            var agent = target.gameObject.AddComponent<UnityEngine.AI.NavMeshAgent>(); agent.speed = 4;
            target.gameObject.AddComponent<CombatReactionController>().ConfigureReaction(new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.SmallMob));
            yield return null; _rig.Owner.DealResolvedAttackDamage(target, 10);
            Assert.That(agent.speed, Is.Zero); yield return new WaitForSeconds(2.65f);
            Assert.That(ProtocolElementQuery.HasChill(target), Is.False); Assert.That(ProtocolElementQuery.IsFrozen(target), Is.False);
            Assert.That(agent.speed, Is.EqualTo(4)); Assert.That(runtime.ChillApplications, Is.Zero);
        }
    }
}
