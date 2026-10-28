using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private ProtocolRuntime PrepareBasicFoundation(out ProtocolInventory inventory)
        {
            BuildAuthoredBasicPlayer();
            var authoring = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.IsTrue(authoring.TryCreateSnapshot(out var catalog, out _));
            UnityEngine.Object.DestroyImmediate(authoring);
            inventory = new ProtocolInventory(Guid.NewGuid(), catalog);
            var runtime = _player.gameObject.AddComponent<ProtocolRuntime>(); runtime.Configure(_player, inventory);
            return runtime;
        }
        private int ConfirmFoundationBasic(BreakerGauntletCombat combat, GauntletImpactContext parent)
        {
            Physics.SyncTransforms();
            return _player.TryApplyGauntletAreaDamage(Vector3.zero, Vector3.forward, 2, new Vector3(1, 2, 2),
                AreaHitShape.Box, 0, ~0, 10, 1, 0, null, false, null, parent, combat.EnergyPolicy, 0);
        }

        private IEnumerator WaitForFoundationEmission(ProtocolRuntime runtime)
        {
            float deadline = Time.realtimeSinceStartup + 2;
            while (_player.IsCombatHitStopped || runtime.ScaledDelta <= 0f)
            { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            Assert.IsTrue(runtime.CanResolveBasicBranch,
                "Branch readiness: open=" + runtime.IsOpen + " playerEnabled=" + _player.isActiveAndEnabled +
                " dead=" + _player.IsDead + " scale=" + Time.timeScale + " paused=" + _player.GetComponent<AbilityHolder>().IsGamePaused);
        }

        [UnityTest] public IEnumerator GPR01OnlyConfirmedTerminalReservesOneChildOnSameRoot()
        {
            var runtime = PrepareBasicFoundation(out _); BasicVictim(Vector3.forward * 1.2f); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var parent = combat.BeginBasicExecutionWithFinisher(true);
            Assert.IsNull(runtime.ReserveConfirmedBasicChild(parent), "Anticipation is not a receipt.");
            Assert.AreEqual(1, ConfirmFoundationBasic(combat, parent));
            long roots = runtime.Budget.RootsCreated; var root = runtime.FindRoot(parent);
            var child = runtime.ReserveConfirmedBasicChild(parent);
            Assert.NotNull(child); Assert.AreEqual(root.RootId, child.RootId);
            Assert.AreEqual(parent.Execution, child.ParentExecutionId);
            Assert.IsNull(runtime.ReserveConfirmedBasicChild(parent)); Assert.AreEqual(roots, runtime.Budget.RootsCreated);
            child.Dispose(); combat.EnergyPolicy.Close(parent);
            Assert.AreEqual(0, runtime.Budget.LiveRoots);
        }

        [UnityTest] public IEnumerator GPR01WhiffImmunityAndNonterminalCannotAuthorizeChild()
        {
            var runtime = PrepareBasicFoundation(out _); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            var whiff = combat.BeginBasicExecutionWithFinisher(true);
            Assert.AreEqual(0, ConfirmFoundationBasic(combat, whiff)); Assert.IsNull(runtime.ReserveConfirmedBasicChild(whiff));
            combat.EnergyPolicy.Close(whiff);
            var victim = BasicVictim(Vector3.forward * 1.2f); victim.AddDamageImmunity(_player);
            var immune = combat.BeginBasicExecutionWithFinisher(true);
            Assert.AreEqual(0, ConfirmFoundationBasic(combat, immune)); Assert.IsNull(runtime.ReserveConfirmedBasicChild(immune));
            combat.EnergyPolicy.Close(immune); victim.RemoveDamageImmunity(_player);
            var nonterminal = combat.BeginBasicExecutionWithFinisher(false);
            Assert.AreEqual(1, ConfirmFoundationBasic(combat, nonterminal)); Assert.IsNull(runtime.ReserveConfirmedBasicChild(nonterminal));
            combat.EnergyPolicy.Close(nonterminal);
        }

        [UnityTest] public IEnumerator GPR01BranchDedupesExcludesNativeVictimAndCannotMintRoot()
        {
            var runtime = PrepareBasicFoundation(out _); var native = BasicVictim(Vector3.forward * 1.2f);
            var extra = BasicVictim(Vector3.right * 5); yield return null;
            using (var stats = new RunStatistics(_player, runtime.Inventory.SessionId, 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
            stats.Watch(native); stats.Watch(extra);
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var parent = combat.BeginBasicExecutionWithFinisher(true);
            ConfirmFoundationBasic(combat, parent); yield return WaitForFoundationEmission(runtime); var child = runtime.ReserveConfirmedBasicChild(parent); Assert.NotNull(child);
            int energy = combat.Energy; float hp = native.health, extraHp = extra.health; long roots = runtime.Budget.RootsCreated;
            using (runtime.OpenExecutionScope(child))
            {
                _player.DealResolvedAttackDamage(native, 10); _player.DealResolvedAttackDamage(extra, 10); _player.DealResolvedAttackDamage(extra, 10);
                using (var denied = runtime.BeginExecution()) Assert.IsFalse(denied.IsValid);
                using (var denied = runtime.ReserveExecution(ProtocolProducer.Technique)) Assert.IsFalse(denied.IsValid);
            }
            Assert.AreEqual(hp, native.health); Assert.That(extra.health, Is.EqualTo(extraHp - 10).Within(.001));
            Assert.AreEqual(energy, combat.Energy); Assert.AreEqual(roots, runtime.Budget.RootsCreated);
            var snapshot = stats.Snapshot();
            Assert.That(snapshot.EffectiveDamage, Is.EqualTo(2000 - native.health - extra.health).Within(.001));
            Assert.AreEqual(2, snapshot.ConfirmedHpTransactions);
            var secondary = snapshot.RecentDamage.Single(x => x.Secondary);
            Assert.AreEqual("Basic", secondary.Ability); Assert.AreEqual("basic-derived", secondary.ProducerId);
            CollectionAssert.Contains(secondary.Parents, "gauntlet:" + parent.Epoch + ":" + parent.RootExecution);
            child.Dispose(); combat.EnergyPolicy.Close(parent);
            }
        }

        [UnityTest] public IEnumerator GPR01BranchBudgetDenialAndRunEndCannotDealGhostDamage()
        {
            var runtime = PrepareBasicFoundation(out _); BasicVictim(Vector3.forward * 1.2f);
            var extra = BasicVictim(Vector3.right * 5); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var parent = combat.BeginBasicExecutionWithFinisher(true);
            ConfirmFoundationBasic(combat, parent); yield return WaitForFoundationEmission(runtime); var child = runtime.ReserveConfirmedBasicChild(parent); var root = runtime.FindRoot(parent);
            while (root.ReserveImpact(1)) { }
            float hp = extra.health;
            using (runtime.OpenExecutionScope(child)) _player.DealResolvedAttackDamage(extra, 10);
            Assert.AreEqual(hp, extra.health);
            runtime.EndOwnedState(); Assert.IsFalse(child.IsValid);
            using (runtime.OpenExecutionScope(child)) _player.DealResolvedAttackDamage(extra, 10);
            Assert.AreEqual(hp, extra.health); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            child.Dispose(); combat.EnergyPolicy.Close(parent);
        }

        [UnityTest] public IEnumerator GPR01ReentrantReservationWaitsForWholeNativeBatchAndDisableCleansBranch()
        {
            var runtime = PrepareBasicFoundation(out _);
            var a = BasicVictim(new Vector3(-.2f, 0, 1.2f)); var b = BasicVictim(new Vector3(.2f, 0, 1.2f));
            yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var parent = combat.BeginBasicExecutionWithFinisher(true);
            int callbacks = 0;
            a.DamageReceived += (_, __) => { callbacks++; Assert.IsNull(runtime.ReserveConfirmedBasicChild(parent)); };
            Assert.AreEqual(2, ConfirmFoundationBasic(combat, parent)); Assert.AreEqual(1, callbacks);
            yield return WaitForFoundationEmission(runtime);
            var child = runtime.ReserveConfirmedBasicChild(parent); Assert.NotNull(child);
            float ahp = a.health, bhp = b.health;
            using (runtime.OpenExecutionScope(child)) { _player.DealResolvedAttackDamage(a, 10); _player.DealResolvedAttackDamage(b, 10); }
            Assert.AreEqual(ahp, a.health); Assert.AreEqual(bhp, b.health);
            runtime.enabled = false; Assert.IsFalse(child.IsValid); Assert.AreEqual(0, runtime.Budget.LiveRoots);
            child.Dispose(); combat.EnergyPolicy.Close(parent);
        }

        [UnityTest] public IEnumerator GPR01SceneTransitionClosesOutstandingChild()
        {
            var runtime = PrepareBasicFoundation(out _); BasicVictim(Vector3.forward * 1.2f); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var parent = combat.BeginBasicExecutionWithFinisher(true);
            ConfirmFoundationBasic(combat, parent); yield return WaitForFoundationEmission(runtime); var child = runtime.ReserveConfirmedBasicChild(parent); Assert.NotNull(child);
            var budget = runtime.Budget;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("NexusBlockout"); yield return null;
            Assert.IsFalse(child.IsValid); Assert.AreEqual(0, budget.LiveRoots); child.Dispose();
        }

        [UnityTest] public IEnumerator GPR01PauseBlocksReservedChildImmediatelyAndResumeKeepsSameRoot()
        {
            var runtime = PrepareBasicFoundation(out _); BasicVictim(Vector3.forward * 1.2f);
            var extra = BasicVictim(Vector3.right * 5); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var parent = combat.BeginBasicExecutionWithFinisher(true);
            ConfirmFoundationBasic(combat, parent); yield return WaitForFoundationEmission(runtime); var child = runtime.ReserveConfirmedBasicChild(parent); Assert.NotNull(child);
            float hp = extra.health; long roots = runtime.Budget.RootsCreated;
            Time.timeScale = 0;
            using (runtime.OpenExecutionScope(child)) _player.DealResolvedAttackDamage(extra, 10);
            Assert.AreEqual(hp, extra.health);
            yield return new WaitForSecondsRealtime(.03f);
            Assert.IsTrue(child.IsValid); Time.timeScale = 1; yield return null;
            using (runtime.OpenExecutionScope(child)) _player.DealResolvedAttackDamage(extra, 10);
            Assert.That(extra.health, Is.EqualTo(hp - 10).Within(.001)); Assert.AreEqual(roots, runtime.Budget.RootsCreated);
            child.Dispose(); combat.EnergyPolicy.Close(parent);
        }

        [UnityTest] public IEnumerator GPR01NewBranchDoesNotInheritProtocolsWithoutExplicitPermission()
        {
            var runtime = PrepareBasicFoundation(out var inventory);
            foreach (var kind in new[] { ProtocolKind.Conductor, ProtocolKind.Detonation })
                Assert.IsTrue(inventory.TryAcquire(inventory.SessionId, kind, inventory.Revision, _ => true));
            BasicVictim(Vector3.forward * 1.2f); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var parent = combat.BeginBasicExecutionWithFinisher(true);
            ConfirmFoundationBasic(combat, parent); yield return WaitForFoundationEmission(runtime);
            var a = BasicVictim(Vector3.right * 20); a.SetMaxHealth(1);
            var b = BasicVictim(Vector3.right * 22); b.SetMaxHealth(1000);
            Physics.SyncTransforms(); var child = runtime.ReserveConfirmedBasicChild(parent); Assert.NotNull(child);
            using (runtime.OpenExecutionScope(child)) _player.DealResolvedAttackDamage(a, 10);
            Assert.IsTrue(a.IsDead); Assert.AreEqual(1000, b.health);
            child.Dispose(); combat.EnergyPolicy.Close(parent);
        }

        [UnityTest] public IEnumerator GPR01PooledRelayCannotContinueOldBranchIntoNewGeneration()
        {
            var runtime = PrepareBasicFoundation(out var inventory);
            foreach (var kind in new[] { ProtocolKind.Conductor, ProtocolKind.Detonation })
                Assert.IsTrue(inventory.TryAcquire(inventory.SessionId, kind, inventory.Revision, _ => true));
            BasicVictim(Vector3.forward * 1.2f); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var parent = combat.BeginBasicExecutionWithFinisher(true);
            ConfirmFoundationBasic(combat, parent); yield return WaitForFoundationEmission(runtime);
            var a = BasicVictim(Vector3.right * 20); a.SetMaxHealth(1000);
            var relay = BasicVictim(Vector3.right * 22); relay.SetMaxHealth(1000);
            var end = BasicVictim(Vector3.right * 24); end.SetMaxHealth(1000);
            long generation = relay.CombatGeneration;
            relay.DamageReceived += (_, __) => { relay.gameObject.SetActive(false); relay.RestoreHealthToMax(); relay.gameObject.SetActive(true); };
            Physics.SyncTransforms(); var child = runtime.ReserveConfirmedBasicChild(parent, ProtocolBasicEffects.Both);
            using (runtime.OpenExecutionScope(child)) _player.DealResolvedAttackDamage(a, 10);
            Assert.Greater(relay.CombatGeneration, generation); Assert.AreEqual(1000, relay.health);
            Assert.AreEqual(1000, end.health); child.Dispose(); combat.EnergyPolicy.Close(parent);
        }

        [UnityTest] public IEnumerator GPR01BranchAllowsConductorThenKillDetonationWithoutRecursion()
        {
            var runtime = PrepareBasicFoundation(out var inventory);
            foreach (var kind in new[] { ProtocolKind.Conductor, ProtocolKind.Detonation, ProtocolKind.Ignite, ProtocolKind.Frost })
                Assert.IsTrue(inventory.TryAcquire(inventory.SessionId, kind, inventory.Revision, _ => true));
            BasicVictim(Vector3.forward * 1.2f); yield return null;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); var parent = combat.BeginBasicExecutionWithFinisher(true);
            ConfirmFoundationBasic(combat, parent); yield return WaitForFoundationEmission(runtime);
            // Isolated contract targets deliberately placed away from native B+C: not a finished wave or economy proof.
            var waveTarget = BasicVictim(Vector3.right * 20); waveTarget.SetMaxHealth(1000);
            var relay = BasicVictim(Vector3.right * 22); relay.SetMaxHealth(1);
            var final = BasicVictim(Vector3.right * 24); final.SetMaxHealth(1000);
            Physics.SyncTransforms();
            var child = runtime.ReserveConfirmedBasicChild(parent, ProtocolBasicEffects.Both); Assert.NotNull(child); long roots = runtime.Budget.RootsCreated;
            using (runtime.OpenExecutionScope(child)) _player.DealResolvedAttackDamage(waveTarget, 10);
            Assert.IsTrue(relay.IsDead); Assert.Less(final.health, 1000);
            Assert.AreEqual(990, waveTarget.health, "The explosion cannot hit an already damaged branch victim again.");
            Assert.IsFalse(ProtocolElementQuery.HasBurn(waveTarget)); Assert.IsFalse(ProtocolElementQuery.HasChill(waveTarget));
            Assert.AreEqual(roots, runtime.Budget.RootsCreated);
            int attempts = runtime.FindRoot(parent).ImpactAttempts; Assert.LessOrEqual(attempts, 3);
            child.Dispose(); combat.EnergyPolicy.Close(parent);
        }
    }
}
