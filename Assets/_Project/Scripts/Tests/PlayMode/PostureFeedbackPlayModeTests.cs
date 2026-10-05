using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class PostureFeedbackPlayModeTests
    {
        private ArsenalProjectileTestRig _rig;
        private float _timeScale;
        [SetUp] public void SetUp() { _timeScale = Time.timeScale; Time.timeScale = 1f; _rig = new ArsenalProjectileTestRig(); }
        [TearDown] public void TearDown() { _rig.TearDown(); Time.timeScale = _timeScale; }

        // Feature: posture-enemy-interaction, Property 10: confirmed lethal contribution and grouped presentation.
        // Validates: Requirements 5.8, 5.9, 6.1
        [UnityTest] public IEnumerator LethalAndReentrantHitsApplyRunnerOnceAtOuterClose()
        {
            var player = _rig.BuildOwner(); var a = _rig.BuildEnemy(Vector3.forward, 5f);
            var b = _rig.BuildEnemy(Vector3.right, 100f);
            var runner = player.gameObject.AddComponent<HitStopRunner>();
            using (var group = player.BeginImpactFeedback(0.07f, 11, 1))
            {
                player.DealResolvedAttackDamage(a, 10f);
                Assert.IsTrue(a.IsDead); Assert.AreEqual(0, runner.AppliedCount);
                using (player.BeginImpactFeedback(0.12f, 11, 2)) player.DealResolvedAttackDamage(b, 10f);
                Assert.AreEqual(0, runner.AppliedCount);
            }
            Assert.AreEqual(1, runner.AppliedCount); Assert.IsTrue(runner.IsActive);
            yield return new WaitForSecondsRealtime(0.17f); Assert.IsFalse(runner.IsActive); Assert.AreEqual(1f, Time.timeScale);
        }
        [UnityTest] public IEnumerator ImmuneContactAndDisabledProfilesDoNotPause()
        {
            var player = _rig.BuildOwner(); var target = _rig.BuildEnemy(Vector3.forward, 100f);
            var runner = player.gameObject.AddComponent<HitStopRunner>();
            target.AddDamageImmunity(player);
            Assert.IsFalse(player.TryApplyDamage(target, 10f));
            Assert.AreEqual(0, runner.AppliedCount);
            target.RemoveDamageImmunity(player);
            using (player.BeginImpactFeedback(0f)) Assert.IsTrue(player.TryApplyDamage(target, 10f));
            Assert.AreEqual(0, runner.AppliedCount); Assert.AreEqual(1f, Time.timeScale);
            yield return null;
        }

        [UnityTest] public IEnumerator AreaMultipleBreaksAndResolvedCallbackCloseOneGroup()
        {
            var player = _rig.BuildOwner();
            var holder = player.gameObject.AddComponent<AbilityHolder>();
            typeof(PlayerActor).GetField("abilityHolder", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .SetValue(player, holder);
            var runner = player.gameObject.AddComponent<HitStopRunner>();
            var a = _rig.BuildEnemy(Vector3.forward, 100f);
            var b = _rig.BuildEnemy(Vector3.forward * 2f, 100f);
            var child = _rig.BuildEnemy(Vector3.one * 30f, 100f);
            int breaks = 0, callbacks = 0;
            foreach (var target in new[] { a, b })
            {
                var reaction = target.gameObject.AddComponent<CombatReactionController>();
                reaction.ConfigureStance(EnemyRank.Boss, 10f, 1f, 0f, 0f, 1f, 1f, 1f, 1f);
                reaction.StanceBroken += _ => breaks++;
            }
            holder.AttackHitsResolved += (_, targets) =>
            {
                callbacks++; Assert.AreEqual(2, targets.Count); Assert.AreEqual(0, runner.AppliedCount);
                player.DealResolvedAttackDamage(child, 10f);
            };
            Physics.SyncTransforms();
            var request = new HitReactionRequest(player, Vector3.up, Vector3.forward,
                HitReactionType.None, HitStrength.Heavy, 10f, StanceBreakEffect.None);
            Assert.AreEqual(2, player.TryApplyAreaDamage(Vector3.zero, Vector3.forward, 4f,
                new Vector3(3f, 2f, 4f), ~0, 10f, 1f, 0f, request));
            Assert.AreEqual(2, breaks); Assert.AreEqual(1, callbacks); Assert.AreEqual(90f, child.health);
            Assert.AreEqual(1, runner.AppliedCount);
            yield return new WaitForSecondsRealtime(0.14f); Assert.IsFalse(runner.IsActive);
        }
        [UnityTest] public IEnumerator HitStopOverlapPauseAndTeardownRestoreModifiers()
        {
            var player = _rig.BuildOwner(); var runner = player.gameObject.AddComponent<HitStopRunner>();
            Time.timeScale = 0.5f; runner.Apply(0.1f);
            yield return new WaitForSecondsRealtime(0.03f); runner.Apply(0.04f);
            yield return new WaitForSecondsRealtime(0.08f); Assert.IsFalse(runner.IsActive); Assert.AreEqual(0.5f, Time.timeScale);
            runner.Apply(0.1f); Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(0.12f); Assert.AreEqual(0f, Time.timeScale);
            runner.Apply(0.1f); Assert.AreEqual(0f, Time.timeScale); Assert.IsFalse(runner.IsActive);
            Time.timeScale = 0.3f; runner.Apply(0.5f); runner.enabled = false;
            Assert.AreEqual(0.3f, Time.timeScale); Assert.IsFalse(runner.IsActive);
        }

        [UnityTest] public IEnumerator ConfirmedDamageSurvivesReentrantHealingAndThrowingCallback()
        {
            var player = _rig.BuildOwner(); var a = _rig.BuildEnemy(Vector3.forward, 100f);
            var b = _rig.BuildEnemy(Vector3.right, 100f);
            var runner = player.gameObject.AddComponent<HitStopRunner>();
            a.DamageReceived += (actor, amount) =>
            {
                actor.Heal(amount);
                player.DealResolvedAttackDamage(b, 10f);
            };
            using (player.BeginImpactFeedback(0.035f)) Assert.IsTrue(player.TryApplyDamage(a, 10f));
            Assert.AreEqual(100f, a.health); Assert.AreEqual(90f, b.health);
            Assert.AreEqual(1, runner.AppliedCount);
            yield return new WaitForSecondsRealtime(0.08f);
            b.DamageReceived += (_, __) => throw new System.InvalidOperationException("receipt callback");
            using (player.BeginImpactFeedback(0.035f))
                Assert.Throws<System.InvalidOperationException>(() => player.DealResolvedAttackDamage(b, 10f));
            Assert.AreEqual(80f, b.health); Assert.AreEqual(2, runner.AppliedCount);
            yield return new WaitForSecondsRealtime(0.08f);
            Assert.IsFalse(runner.IsActive);
        }

        [UnityTest] public IEnumerator AuthoredGauntletLethalHitPausesButOrdinaryBowDamageKeepsItsTiming()
        {
            var player = _rig.BuildOwner(); var target = _rig.BuildEnemy(Vector3.forward, 5f);
            player.DealResolvedAttackDamage(target, 10f);
            var runner = player.GetComponent<HitStopRunner>();
            Assert.IsTrue(target.IsDead); Assert.AreEqual(0, runner.AppliedCount); Assert.AreEqual(1f, Time.timeScale);
            var gauntlet = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"); player.EquipWeapon(gauntlet);
            var other = _rig.BuildEnemy(Vector3.right, 5f); player.DealResolvedAttackDamage(other, 10f);
            Assert.IsTrue(other.IsDead); Assert.AreEqual(1, runner.AppliedCount); Assert.IsTrue(runner.IsActive);
            yield return new WaitForSecondsRealtime(0.11f); Assert.IsFalse(runner.IsActive);
        }

        [UnityTest] public IEnumerator ConfirmedBreakSurvivesThrowingPresentationSubscriber()
        {
            var player = _rig.BuildOwner(); var target = _rig.BuildEnemy(Vector3.forward);
            var reaction = target.gameObject.AddComponent<CombatReactionController>();
            reaction.ConfigureStance(EnemyRank.Boss, 10f, 1f, 0f, 0f, 1f, 1f, 1f, 1f);
            reaction.StanceBroken += _ => throw new System.InvalidOperationException("break subscriber");
            using (player.BeginImpactFeedback())
            {
                player.DealResolvedAttackDamage(target, 10f);
                Assert.Throws<System.InvalidOperationException>(() => player.ApplyHitReactionTo(target,
                    HitReactionType.None, HitStrength.Heavy, 10f, StanceBreakEffect.None, 0f));
            }
            var runner = player.GetComponent<HitStopRunner>();
            Assert.AreEqual(1, runner.AppliedCount); Assert.IsTrue(reaction.IsVulnerable);
            Assert.IsTrue(reaction.IsControlLocked); Assert.IsFalse(reaction.IsStunned);
            yield return new WaitForSecondsRealtime(0.055f);
            Assert.IsTrue(runner.IsActive, "confirmed break keeps its 0.09s emphasis beyond common 0.035s");
            yield return new WaitForSecondsRealtime(0.06f); Assert.IsFalse(runner.IsActive);
        }
    }
}
