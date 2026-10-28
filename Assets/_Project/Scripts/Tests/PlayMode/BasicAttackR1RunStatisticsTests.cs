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
        [UnityTest] public IEnumerator AnalyticsNativeQMatchesRealHp() => AnalyticsSkill(0, false, false);
        [UnityTest] public IEnumerator AnalyticsNativeWMultihitMatchesRealHp() => AnalyticsSkill(1, false, false);
        [UnityTest] public IEnumerator AnalyticsNativeEMatchesRealHp() => AnalyticsSkill(2, false, false);
        [UnityTest] public IEnumerator AnalyticsNativeRMatchesRealHp() => AnalyticsSkill(3, false, false);
        [UnityTest] public IEnumerator AnalyticsEclipseKeepsTransformedQIdentity() => AnalyticsSkill(0, true, false);
        [UnityTest] public IEnumerator AnalyticsCancelBeforeContactDoesNotFabricateDamage() => AnalyticsSkill(0, false, true);
        private IEnumerator AnalyticsSkill(int slot, bool mutation, bool cancel)
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); holder.RefreshLoadout();
            var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            // Fixture resource setup only; integrated observation does not change production resources.
            SetPrivate(_player, "baseMaxMana", 1000f); _player.RefreshResourceStats(); _player.mana = 1000;
            if (mutation) Assert.IsTrue(run.WeaponModifiers.GauntletMutations.Select(GauntletMutation.EclipseUppercut));
            var combat = _player.GetComponent<BreakerGauntletCombat>(); if (slot == 3) combat.AddAsuraEnergy(100);
            float distance = slot == 0 && !mutation ? ((BreakerGauntletAbility)holder.ActiveAbilities[0]).AdvanceDistance + 1.5f : 1.5f;
            var enemy = BuildEnemy(Vector3.forward * distance).actor;
            float receipts = 0; int count = 0;
            enemy.DamageReceived += (_, amount) => { receipts += amount; count++; };
            using (var stats = new RunStatistics(_player, Guid.NewGuid(), 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                stats.Watch(enemy); Physics.SyncTransforms(); Assert.IsTrue(holder.TryUseAbility(slot));
                if (cancel) combat.FinishForDodge();
                float deadline = Time.realtimeSinceStartup + 8;
                while (combat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
                yield return null;
                var s = stats.Snapshot(); Assert.That(s.EffectiveDamage, Is.EqualTo(receipts).Within(.001));
                Assert.That(s.EffectiveDamage, Is.EqualTo(1000-enemy.health).Within(.001));
                Assert.That(s.ObserverErrors, Is.Zero);
                Assert.That(s.Abilities.Single(x => x.Ability == "QWER"[slot].ToString()).Uses, Is.EqualTo(1));
                if (cancel) Assert.That(s.EffectiveDamage, Is.Zero);
                else
                {
                    Assert.That(s.EffectiveDamage, Is.GreaterThan(0));
                    Assert.That(s.Damage.All(x => x.Ability == "QWER"[slot].ToString() && x.PlayerAttributed), Is.True);
                    Assert.That(s.ConfirmedHpTransactions, Is.EqualTo(count));
                    if (slot == 1) Assert.That(count, Is.GreaterThan(1));
                    if (mutation) Assert.That(s.Damage.All(x => x.MutationId == GauntletContentAuthoring.ForMutation(GauntletMutation.EclipseUppercut).Id), Is.True);
                }
            }
        }
        [UnityTest] public IEnumerator AnalyticsBasicUsesAndHitsComeFromProductionCallbacks()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
            var enemy = BuildEnemy(Vector3.forward * 1.5f).actor;
            using (var stats = new RunStatistics(_player, Guid.NewGuid(), 1, Time.realtimeSinceStartupAsDouble, Time.timeAsDouble))
            {
                stats.Watch(enemy); Physics.SyncTransforms();
                Assert.IsTrue(_controls.TryDirectionalBasicAttack(enemy.transform.position));
                yield return new WaitForSeconds(.4f);
                Assert.IsTrue(_controls.TryBasicAttack(enemy.transform.position));
                var s = stats.Snapshot(); Assert.That(s.EffectiveDamage, Is.EqualTo(1000-enemy.health).Within(.001));
                var basic = s.Abilities.Single(x => x.Ability == "Basic");
                Assert.That(basic.Uses, Is.EqualTo(2)); Assert.That(basic.NativeHpTransactions, Is.EqualTo(2));
                Assert.That(s.Damage.All(x => x.PlayerAttributed), Is.True);
            }
        }
    }
}
