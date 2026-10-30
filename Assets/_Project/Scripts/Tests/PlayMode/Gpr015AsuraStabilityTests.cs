using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest]
        public IEnumerator GPR015AsuraMovableWaitMatchesHistoricalRecoveryFailureCondition()
        {
            var holder = BuildFluidMovementRig(); var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            Assert.IsTrue(modifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura));
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, modifiers);
            SetPrivate(_player, "baseMaxMana", 500f); _player.RefreshResourceStats(); _player.mana = 500;
            var combat = _player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(100);
            Assert.IsTrue(holder.TryUseAbility(3)); long execution = combat.CadencedExecution;
            float mana = _player.mana, deadline = Time.realtimeSinceStartup + 3;
            while (!combat.IsCadencedWaiting) { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            Assert.IsTrue(combat.TryGetCancelContext(out _, out float progress));
            bool oldHelperWouldAdmit = holder.CanQueueRecoveryMovement && progress >= .85f;
            Assert.IsTrue(oldHelperWouldAdmit, "The old helper admits the movable wait as if it required a movement buffer.");
            Assert.IsTrue(holder.MovementAllowedWhileCasting);
            Assert.IsTrue(_controls.TryRequestGroundMovement(Vector3.right * 9));
            Assert.IsFalse(_controls.HasPendingGroundMovement, "Movement is legitimately immediate in the wait; this reproduces the historical False value.");
            TestContext.WriteLine("GPR015 boundary: waiting=" + combat.IsCadencedWaiting + "; progress=" + progress + "; oldHelperAdmits=" + oldHelperWouldAdmit + "; movementAllowed=" + holder.MovementAllowedWhileCasting + "; pending=" + _controls.HasPendingGroundMovement);
            Assert.IsTrue(holder.TryUseAbility(3)); Assert.AreEqual(execution, combat.CadencedExecution); Assert.AreEqual(mana, _player.mana);
            yield return WaitForMovementCastToFinish(holder);
        }
    }
}
