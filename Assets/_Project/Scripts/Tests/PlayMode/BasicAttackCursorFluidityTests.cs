using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest]
        public IEnumerator CursorFluidity_TargetedTapSurvivesAttackCadenceAndExecutesExactlyOnce()
        {
            BuildFluidMovementRig();
            using (new CursorInputFocusScope())
            using (var input = new FluidityInputDevices())
            {
                yield return null;
                var enemy = BuildEnemy(Vector3.forward * 1.2f, 10000f);
                Physics.SyncTransforms();
                SetTarget(enemy.interactable);
                NextAttackTimeField.SetValue(_controls, Time.time + 100f);
                QueueTap();
                for (int i = 0; i < 3; i++) yield return null;
                Assert.AreEqual(0, _attackCount, "The attack interval must still prevent early execution.");

                NextAttackTimeField.SetValue(_controls, Time.time - 1f);
                yield return WaitForCursorAttackCount(1);
                Assert.AreEqual(1, _attackCount, "A tap received during cadence must survive until the basic is available.");
                yield return new WaitForSeconds(.6f);
                Assert.AreEqual(1, _attackCount, "One released tap must authorize exactly one basic.");
            }
        }

        [UnityTest]
        public IEnumerator CursorFluidity_TargetedTapSurvivesBusyRecoveryAndExecutesExactlyOnce()
        {
            BuildFluidMovementRig();
            using (new CursorInputFocusScope())
            using (var input = new FluidityInputDevices())
            {
                yield return null;
                var enemy = BuildEnemy(Vector3.forward * 1.2f, 10000f);
                Physics.SyncTransforms();
                SetTarget(enemy.interactable);
                PlayerBusyField.SetValue(_controls, true);
                QueueTap();
                // Exercise the same target-attack coordinator that receives queued pointer commands.
                // This isolates recovery readiness from NavMesh follow's own early return.
                var attack = typeof(CharControlScript).GetMethod("TryAttackTarget", BindingFlags.Instance | BindingFlags.NonPublic);
                attack.Invoke(_controls, null);
                Assert.AreEqual(0, _attackCount);
                PlayerBusyField.SetValue(_controls, false);
                yield return WaitForCursorAttackCount(1);
                yield return new WaitForSeconds(.6f);
                Assert.AreEqual(1, _attackCount, "A blocked attempt must not consume the player's one pending tap.");
            }
        }

        [UnityTest]
        public IEnumerator CursorFluidity_PrimaryClickWithoutShiftHitsAtZeroManaWithoutResourceCost()
        {
            BuildFluidMovementRig();
            var camera = BuildFluidInputCamera();
            var enemy = BuildEnemy(Vector3.right * 1.2f, 10000f);
            _player.mana = 0f;
            float healthBefore = enemy.actor.health;
            using (new CursorInputFocusScope())
            using (var input = new FluidityInputDevices())
            {
                yield return new WaitForEndOfFrame();
                Physics.SyncTransforms();
                Vector2 pointer = camera.WorldToScreenPoint(enemy.actor.transform.position + Vector3.up * .2f);
                input.Queue(GameControl.Primary, true, pointer);
                yield return null;
                input.Queue(GameControl.Primary, false, pointer);
                yield return WaitForCursorAttackCount(1);
                Assert.AreEqual(1, _attackCount);
                // GI1.4: acceptance stays immediate; the authored Gauntlet impact owns HP.
                float contactDeadline = Time.realtimeSinceStartup + 1f;
                while (enemy.actor.health >= healthBefore && Time.realtimeSinceStartup < contactDeadline)
                    yield return null;
                Assert.That(enemy.actor.health, Is.LessThan(healthBefore), "The real contextual click must confirm HP damage without Shift.");
                Assert.AreEqual(0f, _player.mana, .001f, "Basic attacks must remain usable without spending or requiring mana.");
                yield return new WaitForSeconds(.6f);
                Assert.AreEqual(1, _attackCount, "Releasing the click must stop repetition.");
            }
        }

        [UnityTest]
        public IEnumerator CursorFluidity_SpearBasicKeepsItsAuthoredReachWithGauntletBalanceLoaded()
        {
            Assert.IsTrue(TryBakeFullFloor());
            BuildPlayer(Vector3.zero);
            var spear = Resources.Load<WeaponScript>("Weapons/Melee/Spear/Spear");
            Assert.IsTrue(spear, "The published spear must be exercised.");
            Assert.IsNotNull(CombatBalance.Current, "The regression requires the balance asset that formerly replaced all melee geometry.");
            Assert.That(spear.attackDistance, Is.GreaterThan(3.3f));
            _player.EquipWeapon(spear);
            using (new CursorInputFocusScope())
            using (var input = new FluidityInputDevices())
            {
                yield return null;
                var enemy = BuildEnemy(Vector3.forward * 3.3f, 10000f);
                Physics.SyncTransforms();
                SetTarget(enemy.interactable);
                float healthBefore = enemy.actor.health;
                Assert.IsTrue(_controls.TryBasicAttack(enemy.actor.transform.position));
                Assert.AreEqual(1, _attackCount);
                Assert.That(enemy.actor.health, Is.LessThan(healthBefore), "The spear must connect at its own authored reach instead of the shorter gauntlet volume.");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator CursorFluidity_TargetWithoutAttackIntentDoesNotPursue()
        {
            BuildFluidMovementRig();
            using (new CursorInputFocusScope())
            using (var input = new FluidityInputDevices())
            {
                yield return null;
                var enemy = BuildEnemy(Vector3.forward * 8f, 10000f);
                Vector3 start = _player.transform.position;
                SetTarget(enemy.interactable);
                yield return new WaitForSeconds(.35f);
                Assert.AreEqual(0, _attackCount);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.15f),
                    "A selected target with no tap or held attack must not authorize pursuit.");
                Assert.IsFalse(_agent.hasPath, "Target tracking alone must not retain a movement route.");
            }
        }

        [UnityTest]
        public IEnumerator CursorFluidity_CompletedTapDoesNotChaseTargetThatMovesAway()
        {
            BuildFluidMovementRig();
            using (new CursorInputFocusScope())
            using (var input = new FluidityInputDevices())
            {
                yield return null;
                var enemy = BuildEnemy(Vector3.forward * 1.2f, 10000f);
                Physics.SyncTransforms();
                SetTarget(enemy.interactable);
                QueueTap();
                yield return WaitForCursorAttackCount(1);
                yield return new WaitForSeconds(.4f);
                Vector3 start = _player.transform.position;
                enemy.actor.transform.position = Vector3.forward * 8f;
                Physics.SyncTransforms();
                yield return new WaitForSeconds(.4f);
                Assert.AreEqual(1, _attackCount);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.15f),
                    "The completed one-shot command must not become persistent pursuit.");
                Assert.IsFalse(_agent.hasPath);
            }
        }

        [UnityTest]
        public IEnumerator CursorFluidity_ShiftWithReboundPrimaryRepeatsUntilReleaseWithoutMouseClick()
        {
            const string preference = "TechGuy.Settings.Binding.Primary";
            bool existed = PlayerPrefs.HasKey(preference);
            int previous = PlayerPrefs.GetInt(preference);
            try
            {
                PlayerPrefs.SetInt(preference, (int)KeyCode.F10);
                BuildFluidMovementRig();
                var camera = BuildFluidInputCamera();
                using (new CursorInputFocusScope())
                using (var input = new FluidityInputDevices())
                {
                    yield return new WaitForEndOfFrame();
                    Vector2 pointer = camera.WorldToScreenPoint(Vector3.right * 3f);
                    InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pointer });
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.LeftShift, Key.F10));
                    yield return WaitForCursorAttackCount(3);
                    Assert.IsNull(CurrentTarget(), "The held directional attack stays in place without selecting an enemy.");
                    Assert.That(Vector3.Distance(Vector3.zero, _player.transform.position), Is.LessThan(.1f));
                    Assert.That(Vector3.Dot(_player.transform.forward, Vector3.right), Is.GreaterThan(.95f));
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                    yield return null;
                    int afterRelease = _attackCount;
                    yield return new WaitForSeconds(.6f);
                    Assert.AreEqual(afterRelease, _attackCount, "Releasing the rebound action must stop future directional repeats.");
                }
            }
            finally
            {
                if (existed) PlayerPrefs.SetInt(preference, previous);
                else PlayerPrefs.DeleteKey(preference);
            }
        }

        [UnityTest]
        public IEnumerator CursorFluidity_ReleaseGateRequiresBothPointerActionsBeforeShiftMoveCanResume()
        {
            const string primaryPreference = "TechGuy.Settings.Binding.Primary";
            const string movePreference = "TechGuy.Settings.Binding.Move";
            bool hadPrimary = PlayerPrefs.HasKey(primaryPreference), hadMove = PlayerPrefs.HasKey(movePreference);
            int previousPrimary = PlayerPrefs.GetInt(primaryPreference), previousMove = PlayerPrefs.GetInt(movePreference);
            try
            {
                PlayerPrefs.SetInt(primaryPreference, (int)KeyCode.F10);
                PlayerPrefs.SetInt(movePreference, (int)KeyCode.F9);
                BuildFluidMovementRig();
                var camera = BuildFluidInputCamera();
                using (new CursorInputFocusScope())
                using (var input = new FluidityInputDevices())
                {
                    yield return new WaitForEndOfFrame();
                    Vector2 pointer = camera.WorldToScreenPoint(Vector3.right * 3f);
                    _controls.RequireAttackRelease();
                    InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = pointer });
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.LeftShift, Key.F9));
                    yield return new WaitForSeconds(.5f);
                    Assert.AreEqual(0, _attackCount,
                        "Holding Shift plus Move must not bypass a modal/pause release gate just because Primary is already up.");
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                    yield return null;
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.LeftShift, Key.F9));
                    yield return WaitForCursorAttackCount(1);
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState());
                    yield return null;
                    int afterRelease = _attackCount;
                    yield return new WaitForSeconds(.4f);
                    Assert.AreEqual(afterRelease, _attackCount, "Only the fresh action after a full release may resume attacks.");
                }
            }
            finally
            {
                if (hadPrimary) PlayerPrefs.SetInt(primaryPreference, previousPrimary); else PlayerPrefs.DeleteKey(primaryPreference);
                if (hadMove) PlayerPrefs.SetInt(movePreference, previousMove); else PlayerPrefs.DeleteKey(movePreference);
            }
        }

        private IEnumerator WaitForCursorAttackCount(int minimum)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (_attackCount < minimum && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.That(_attackCount, Is.GreaterThanOrEqualTo(minimum), "The authorized basic did not execute before the deadline.");
        }

        private sealed class CursorInputFocusScope : System.IDisposable
        {
            private readonly Keyboard _previousKeyboard = Keyboard.current;
            private readonly Mouse _previousMouse = Mouse.current;
            private readonly InputSettings _settings = InputSystem.settings;
            private readonly InputSettings.BackgroundBehavior _background;
            private readonly InputSettings.EditorInputBehaviorInPlayMode _editorInput;
            public CursorInputFocusScope()
            {
                _background = _settings.backgroundBehavior;
                _editorInput = _settings.editorInputBehaviorInPlayMode;
                _settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                _settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            }
            public void Dispose()
            {
                _settings.backgroundBehavior = _background;
                _settings.editorInputBehaviorInPlayMode = _editorInput;
                if (_previousKeyboard != null) _previousKeyboard.MakeCurrent();
                if (_previousMouse != null) _previousMouse.MakeCurrent();
            }
        }
    }
}
