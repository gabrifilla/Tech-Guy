using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private static object BasicField(object owner, string name) => owner.GetType().GetField(name,
            BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
        private static void BasicMouse(DirectInputScope input, Camera view, Vector3 point, bool held, bool secondary = false)
        {
            var state = new MouseState { position = (Vector2)view.WorldToScreenPoint(point) };
            if (held) state = state.WithButton(MouseButton.Left);
            if (secondary) state = state.WithButton(MouseButton.Right);
            InputSystem.QueueStateEvent(input.Mouse, state);
        }
        private IEnumerator ReadyBasicInput(DirectInputScope input, Camera view)
        {
            input.Keys(); BasicMouse(input, view, Vector3.forward * 10, false);
            yield return null; yield return null;
            _controls.SendMessage("OnApplicationFocus", true);
            yield return null; yield return null;
            Assert.That(_controls.GameplayInputReady, Is.True);
        }
        private IEnumerator ReadyNextBasic()
        {
            float guard = Time.realtimeSinceStartup + 3;
            while (!_controls.CanStartDirectionalBasic)
            { Assert.That(Time.realtimeSinceStartup, Is.LessThan(guard)); yield return null; }
        }
        private IEnumerator ClickBasic(DirectInputScope input, Camera view, Vector3 point)
        {
            BasicMouse(input, view, point, true); yield return new WaitForSecondsRealtime(.025f);
            BasicMouse(input, view, point, false); yield return new WaitForSecondsRealtime(.025f);
        }

        [Serializable] private sealed class DirectComboObservation
        { public string input = "virtual Mouse"; public bool physical; public float interval; public float[] times; public int[] stages; }

        [UnityTest] public IEnumerator GPRClickSingleHeldPressWhiffsOnceWithoutTargetOrNavigation()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                Vector3 start = _player.transform.position;
                BasicMouse(input, view, Vector3.right * 10, true);
                yield return new WaitForSeconds(1.5f);
                Assert.That(attacks, Is.EqualTo(1));
                Assert.That(TargetField.GetValue(_controls), Is.Null);
                Assert.That(_agent.hasPath, Is.False);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01f));
                Assert.That(_controls.HasScheduledBasic, Is.False);
                BasicMouse(input, view, Vector3.right * 10, false); yield return new WaitForSeconds(.2f);
                Assert.That(attacks, Is.EqualTo(1));
            }
        }

        [UnityTest] public IEnumerator GPRClickSuccessivePressesAdvanceAuthoredComboAndKeepCadence()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                var stages = new List<int>(); var times = new List<float>();
                float interval = (float)AttackIntervalField.GetValue(_controls);
                _controls.BasicAttackPerformed += () => { stages.Add((int)BasicField(_controls, "_basicAttackIndex")); times.Add(Time.time); };
                for (int i = 0; i < 3; i++)
                { yield return ReadyNextBasic(); yield return ClickBasic(input, view, Vector3.forward * 10); }
                yield return ReadyNextBasic(); yield return new WaitForSeconds(.15f);
                CollectionAssert.AreEqual(new[] { 0, 1, 2 }, stages);
                for (int i = 1; i < times.Count; i++) Assert.That(times[i] - times[i - 1], Is.GreaterThanOrEqualTo(interval - .001f));
                System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/c01-3a/combo-observation.json"),
                    JsonUtility.ToJson(new DirectComboObservation { physical = false, interval = interval, times = times.ToArray(), stages = stages.ToArray() }));
            }
        }

        [UnityTest] public IEnumerator GPRClickEmptyCursorUsesAuthoredContactOnceAndKeepsCommittedFacing()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true);
                var contact = BasicVictim(Vector3.right * 1.2f); contact.SetMaxHealth(1000); contact.RestoreHealthToMax();
                var miss = BasicVictim(Vector3.left * 1.2f); miss.SetMaxHealth(1000); miss.RestoreHealthToMax();
                int hits = 0, misses = 0; contact.DamageReceived += (_, __) => hits++; miss.DamageReceived += (_, __) => misses++;
                yield return ReadyBasicInput(input, view); Physics.SyncTransforms();
                yield return ClickBasic(input, view, Vector3.right * 10);
                Assert.That(hits, Is.EqualTo(0), "Acceptance/animation must precede authored contact.");
                Assert.That(_controls.HasScheduledBasic, Is.True);
                BasicMouse(input, view, Vector3.left * 10, false);
                while (_controls.HasScheduledBasic && _controls.BasicImpactProgress < _controls.BasicImpactFraction)
                { Assert.That(_player.transform.forward.x, Is.GreaterThan(.99f)); yield return null; }
                yield return new WaitForSeconds(.6f);
                Assert.That(hits, Is.EqualTo(1)); Assert.That(misses, Is.EqualTo(0));
                Assert.That(TargetField.GetValue(_controls), Is.Null);
            }
        }

        [UnityTest] public IEnumerator GPRClickRecoveryStoresOnlyLatestDirectionForOneFollowup()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                int attacks = 0; Vector3 second = Vector3.zero;
                _controls.BasicAttackPerformed += () => { if (++attacks == 2) second = _player.transform.forward; };
                yield return ClickBasic(input, view, Vector3.forward * 10);
                float next = (float)NextAttackTimeField.GetValue(_controls);
                while (Time.time < next - .07f) yield return null;
                var holder = _player.GetComponent<AbilityHolder>();
                // Multiple same-window requests exercise the existing shared single slot, not an attack list.
                Assert.That(holder.TryRequestDirectionalBasic(Vector3.left), Is.True);
                Assert.That(holder.TryRequestDirectionalBasic(Vector3.right), Is.True);
                BasicMouse(input, view, Vector3.left * 10, false);
                yield return new WaitForSeconds(.9f);
                Assert.That(attacks, Is.EqualTo(2)); Assert.That(second.x, Is.GreaterThan(.99f));
                Assert.That(holder.HasBufferedOffensiveIntent, Is.False);
            }
        }

        [UnityTest] public IEnumerator GPRClickTooEarlyPressExpiresInsteadOfPlayingTheWholeCombo()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                yield return ClickBasic(input, view, Vector3.forward * 10);
                yield return ClickBasic(input, view, Vector3.right * 10);
                yield return new WaitForSeconds(1.1f);
                Assert.That(attacks, Is.EqualTo(1));
                Assert.That(_player.GetComponent<AbilityHolder>().HasBufferedOffensiveIntent, Is.False);
            }
        }

        [UnityTest] public IEnumerator GPRClickWASDAndAimOpposeWithoutClickToMoveOrFacingOverride()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                input.Keys(Key.W); BasicMouse(input, view, Vector3.right * 10, false); yield return new WaitForSeconds(.08f);
                Vector3 start = _player.transform.position;
                yield return ClickBasic(input, view, start + Vector3.right * 10);
                Assert.That(_player.transform.forward.x, Is.GreaterThan(.99f));
                while (_controls.HasScheduledBasic)
                { Assert.That(_player.transform.forward.x, Is.GreaterThan(.99f)); Assert.That(_agent.hasPath, Is.False); yield return null; }
                input.Keys(); yield return null;
                Assert.That(Mathf.Abs(_player.transform.position.x - start.x), Is.LessThan(.01f));
            }
        }

        [UnityTest] public IEnumerator GPRClickPauseFocusAndDisableNeverReplayHeldPrimary()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                var pause = _player.GetComponent<PauseMenuUI>();
                for (int context = 0; context < 3; context++)
                {
                    if (context == 0) pause.Pause();
                    else if (context == 1) _controls.SendMessage("OnApplicationFocus", false);
                    else _controls.enabled = false;
                    BasicMouse(input, view, Vector3.forward * 10, true); yield return null;
                    if (context == 0) pause.Resume();
                    else if (context == 1) _controls.SendMessage("OnApplicationFocus", true);
                    else _controls.enabled = true;
                    yield return new WaitForSeconds(.2f);
                    Assert.That(attacks, Is.EqualTo(context));
                    BasicMouse(input, view, Vector3.forward * 10, false); yield return null; yield return null;
                    yield return ClickBasic(input, view, Vector3.forward * 10);
                    Assert.That(attacks, Is.EqualTo(context + 1)); yield return ReadyNextBasic();
                }
            }
        }

        [UnityTest] public IEnumerator GPRClickUiGraphicOwnsPressAndNoLateAttackOccurs()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                if (!EventSystem.current) { var e = new GameObject("Test EventSystem"); _spawned.Add(e); e.AddComponent<EventSystem>(); }
                var canvas = new GameObject("Test modal graphic", typeof(Canvas), typeof(GraphicRaycaster)); _spawned.Add(canvas);
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var panel = new GameObject("UI owns pointer", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
                var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                yield return null;
                BasicMouse(input, view, Vector3.forward * 10, true); yield return null; yield return null;
                Assert.That(attacks, Is.EqualTo(0));
                canvas.SetActive(false); yield return new WaitForSeconds(.8f);
                Assert.That(attacks, Is.EqualTo(0));
                BasicMouse(input, view, Vector3.forward * 10, false); yield return null; yield return null;
                yield return ClickBasic(input, view, Vector3.forward * 10); Assert.That(attacks, Is.EqualTo(1));
            }
        }

        [UnityTest] public IEnumerator GPRClickWeaponSwapInvalidatesPendingBasic()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                yield return ClickBasic(input, view, Vector3.forward * 10);
                Assert.That(_player.GetComponent<AbilityHolder>().TryRequestDirectionalBasic(Vector3.right), Is.True);
                _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Spear/Spear"));
                yield return new WaitForSeconds(1);
                Assert.That(attacks, Is.EqualTo(1)); Assert.That(_player.GetComponent<AbilityHolder>().HasBufferedOffensiveIntent, Is.False);
            }
        }

        [UnityTest] public IEnumerator GPRClickBowTapDoesNotRepeatWithoutChargedShot()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow"));
                _player.GetComponent<AbilityHolder>().RefreshLoadout(); yield return ReadyBasicInput(input, view);
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                BasicMouse(input, view, Vector3.forward * 10, true); yield return new WaitForSeconds(1.5f);
                Assert.That(attacks, Is.EqualTo(1));
                BasicMouse(input, view, Vector3.forward * 10, false); yield return null;
                yield return ClickBasic(input, view, Vector3.forward * 10); Assert.That(attacks, Is.EqualTo(2));
            }
        }

        [UnityTest] public IEnumerator GPRClickBowChargedShotRetainsApprovedHoldExceptionAndReleaseStopsIt()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow"));
                var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
                WeaponRunModifiers.Definition charged = null;
                foreach (var definition in WeaponRunModifiers.Catalog) if (definition.Kind == WeaponBoon.ChargedShot) charged = definition;
                Assert.That(charged, Is.Not.Null); Assert.That(run.WeaponModifiers.Add(charged), Is.True);
                Assert.That(_player.RunModifiers.Rank(WeaponBoon.ChargedShot), Is.EqualTo(1));
                yield return ReadyBasicInput(input, view);
                Debug.Log("C01 charge context ready=" + _controls.GameplayInputReady + " blocked=" + _player.GetComponent<AbilityHolder>().BlocksWorldInput + " pointer=" + view.WorldToScreenPoint(Vector3.forward * 10));
                var piercing = new List<bool>();
                _controls.BasicAttackPerformed += () => {
                    foreach (var arrow in Object.FindObjectsByType<ArsenalProjectile>())
                        if ((PlayerActor)BasicField(arrow, "_owner") == _player) piercing.Add((bool)BasicField(arrow, "_piercing"));
                };
                BasicMouse(input, view, Vector3.forward * 10, true); yield return new WaitForSeconds(1.05f);
                Assert.That(piercing.Count, Is.GreaterThanOrEqualTo(2), "Basic events=" + _attackCount + "; ready=" + _controls.GameplayInputReady + "; focus=" + _controls.DirectInputHasFocus + "; blocked=" + _player.GetComponent<AbilityHolder>().BlocksWorldInput + "; pointer=" + Mouse.current.position.ReadValue() + "; mouse=" + Mouse.current.name + "; ui=" + ((System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>)BasicField(_controls, "_basicUiHits")).Count);
                Assert.That(piercing[0], Is.False); Assert.That(piercing, Does.Contain(true));
                int before = piercing.Count;
                BasicMouse(input, view, Vector3.forward * 10, false); yield return new WaitForSeconds(.8f);
                Assert.That(piercing.Count, Is.EqualTo(before), "Release must not synthesize an attack.");
            }
        }

        [UnityTest] public IEnumerator GPRClickSpearUsesItsExistingReachWithoutTargetOrAutomaticRepeat()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Spear/Spear"));
                _player.GetComponent<AbilityHolder>().RefreshLoadout();
                var victim = BasicVictim(Vector3.forward * 2); victim.SetMaxHealth(1000); victim.RestoreHealthToMax();
                int hits = 0, attacks = 0; victim.DamageReceived += (_, __) => hits++; _controls.BasicAttackPerformed += () => attacks++;
                yield return ReadyBasicInput(input, view); Physics.SyncTransforms();
                BasicMouse(input, view, Vector3.forward * 10, true); yield return new WaitForSeconds(1);
                Assert.That(attacks, Is.EqualTo(1)); Assert.That(hits, Is.EqualTo(1)); Assert.That(TargetField.GetValue(_controls), Is.Null);
                Assert.That(_agent.hasPath, Is.False);
            }
        }

        [UnityTest] public IEnumerator GPRAsuraRmbUsesExistingStateOnceAndNeverBasicEvenWithLegacyKey()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); yield return ReadyBasicInput(input, view);
                var holder = _player.GetComponent<AbilityHolder>(); var combat = _player.GetComponent<BreakerGauntletCombat>();
                Assert.That(holder.UsesDirectAsuraInput, Is.True); Assert.That(holder.GetAbilityKey(3), Is.EqualTo(KeyCode.Mouse1));
                var original = holder.ActiveAbilities[3]; float before = _player.mana, cost = original.ManaCost;
                combat.AddAsuraEnergy(100); int uses = 0, basics = 0;
                holder.AbilityUsed += slot => { if (slot == 3) uses++; }; _controls.BasicAttackPerformed += () => basics++;
                input.Keys(Key.F11); BasicMouse(input, view, Vector3.forward * 10, true, true);
                yield return new WaitForSeconds(.2f);
                Assert.That(uses, Is.EqualTo(1)); Assert.That(basics, Is.EqualTo(0));
                Assert.That(holder.ActiveAbilities[3], Is.SameAs(original));
                Assert.That(_player.mana, Is.EqualTo(before - cost).Within(.001f)); Assert.That(combat.Energy, Is.EqualTo(0));
                yield return new WaitForSeconds(.2f);
                Assert.That(uses, Is.EqualTo(1)); Assert.That(_player.mana, Is.EqualTo(before - cost).Within(.001f));
            }
        }

        [UnityTest] public IEnumerator GPRAsuraRmbRejectsWithoutEnergyAndCannotReplayAfterPause()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); yield return ReadyBasicInput(input, view);
                var holder = _player.GetComponent<AbilityHolder>(); var combat = _player.GetComponent<BreakerGauntletCombat>();
                int uses = 0, basics = 0; holder.AbilityUsed += slot => { if (slot == 3) uses++; }; _controls.BasicAttackPerformed += () => basics++;
                float mana = _player.mana;
                BasicMouse(input, view, Vector3.forward * 10, false, true); yield return new WaitForSeconds(.08f);
                Assert.That(uses, Is.EqualTo(0)); Assert.That(basics, Is.EqualTo(0)); Assert.That(_player.mana, Is.EqualTo(mana));
                BasicMouse(input, view, Vector3.forward * 10, false); yield return null; yield return null;
                combat.AddAsuraEnergy(100); var pause = _player.GetComponent<PauseMenuUI>(); pause.Pause();
                BasicMouse(input, view, Vector3.forward * 10, false, true); yield return null;
                pause.Resume(); yield return new WaitForSeconds(.2f);
                Assert.That(uses, Is.EqualTo(0)); Assert.That(combat.Energy, Is.EqualTo(100));
                BasicMouse(input, view, Vector3.forward * 10, false); yield return null; yield return null;
                BasicMouse(input, view, Vector3.forward * 10, false, true); yield return new WaitForSeconds(.08f);
                Assert.That(uses, Is.EqualTo(1)); Assert.That(basics, Is.EqualTo(0));
            }
        }

        [UnityTest] public IEnumerator GPRAsuraCadencedRmbEdgesContinueSameExecutionAndFinisherWins() => RunDirectAsuraCadence(true);
        [UnityTest] public IEnumerator GPRAsuraCadencedThreeRmbPressesKeepSingleExecutionAndCost() => RunDirectAsuraCadence(false);
        private IEnumerator RunDirectAsuraCadence(bool finish)
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); var run = _player.gameObject.AddComponent<RunBoons>(); yield return null;
                Assert.That(run.WeaponModifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura), Is.True);
                var holder = _player.GetComponent<AbilityHolder>(); holder.RefreshLoadout();
                var combat = _player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(100);
                yield return ReadyBasicInput(input, view);
                int uses = 0; holder.AbilityUsed += slot => { if (slot == 3) uses++; };
                float mana = _player.mana;
                BasicMouse(input, view, Vector3.forward * 10, false, true); yield return new WaitForSeconds(.025f);
                Assert.That(combat.IsCadencedAsura, Is.True); long execution = combat.CadencedExecution;
                float paid = _player.mana; Assert.That(paid, Is.LessThan(mana));
                float guard = Time.realtimeSinceStartup + 4;
                while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < guard) yield return null;
                Assert.That(combat.IsCadencedWaiting, Is.True);
                yield return new WaitForSeconds(.08f); Assert.That((int)BasicField(combat, "_cadencedStrike"), Is.EqualTo(0), "Holding RMB must not continue.");
                BasicMouse(input, view, Vector3.forward * 10, false); yield return null; yield return null;
                BasicMouse(input, view, Vector3.forward * 10, false, true); yield return new WaitForSeconds(.025f);
                Assert.That((int)BasicField(combat, "_cadencedStrike"), Is.EqualTo(1)); Assert.That(combat.CadencedExecution, Is.EqualTo(execution));
                BasicMouse(input, view, Vector3.forward * 10, false); yield return null; yield return null;
                guard = Time.realtimeSinceStartup + 4;
                while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < guard) yield return null;
                Assert.That(combat.IsCadencedWaiting, Is.True);
                input.Keys(finish ? new[] { Key.F } : Array.Empty<Key>()); BasicMouse(input, view, Vector3.forward * 10, false, true); yield return new WaitForSeconds(.025f);
                Assert.That((int)BasicField(combat, "_cadencedStrike"), Is.EqualTo(2)); Assert.That(combat.CadencedExecution, Is.EqualTo(execution));
                Assert.That(uses, Is.EqualTo(1)); Assert.That(_player.mana, Is.EqualTo(paid));
                Assert.That(holder.HasBufferedOffensiveIntent, Is.False);
            }
        }

        [UnityTest] public IEnumerator GPRClickHitstopRetainsOneReleasedTapUntilResume()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                using (var feedback = _player.BeginImpactFeedback(.18f)) feedback.Scope.ConfirmDamage(true);
                Assert.That(_player.IsCombatHitStopped, Is.True);
                yield return ClickBasic(input, view, Vector3.right * 10);
                Assert.That(attacks, Is.EqualTo(0)); Assert.That(_player.GetComponent<AbilityHolder>().HasBufferedOffensiveIntent, Is.True);
                float guard = Time.realtimeSinceStartup + 3;
                while (attacks == 0 && Time.realtimeSinceStartup < guard) yield return null;
                Assert.That(attacks, Is.EqualTo(1)); yield return new WaitForSeconds(.5f); Assert.That(attacks, Is.EqualTo(1));
            }
        }

        [UnityTest] public IEnumerator GPRClickAcceptedSkillDiscardsPendingBasic()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); yield return ReadyBasicInput(input, view);
                var holder = _player.GetComponent<AbilityHolder>();
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                yield return ClickBasic(input, view, Vector3.forward * 10);
                Assert.That(holder.TryRequestDirectionalBasic(Vector3.right), Is.True);
                Assert.That(holder.HasBufferedOffensiveIntent, Is.True);
                Assert.That(holder.TryUseAbility(0), Is.True);
                Assert.That(holder.HasBufferedOffensiveIntent, Is.False);
                yield return WaitForMovementCastToFinish(holder); yield return new WaitForSeconds(.2f);
                Assert.That(attacks, Is.EqualTo(1));
            }
        }

        [UnityTest] public IEnumerator GPRAsuraUiGraphicOwnsRmbWithoutSpendingOrReplay()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); yield return ReadyBasicInput(input, view);
                var holder = _player.GetComponent<AbilityHolder>(); var combat = _player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(100);
                if (!EventSystem.current) { var e = new GameObject("Test special EventSystem"); _spawned.Add(e); e.AddComponent<EventSystem>(); }
                var canvas = new GameObject("Test special UI", typeof(Canvas), typeof(GraphicRaycaster)); _spawned.Add(canvas);
                canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                var panel = new GameObject("UI owns RMB", typeof(RectTransform), typeof(Image)); panel.transform.SetParent(canvas.transform, false);
                var rect = panel.GetComponent<RectTransform>(); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                int uses = 0; holder.AbilityUsed += slot => { if (slot == 3) uses++; }; yield return null;
                BasicMouse(input, view, Vector3.forward * 10, false, true); yield return new WaitForSeconds(.05f);
                Assert.That(uses, Is.EqualTo(0)); Assert.That(combat.Energy, Is.EqualTo(100));
                canvas.SetActive(false); yield return new WaitForSeconds(.2f);
                Assert.That(uses, Is.EqualTo(0)); Assert.That(combat.Energy, Is.EqualTo(100));
                BasicMouse(input, view, Vector3.forward * 10, false); yield return null; yield return null;
                BasicMouse(input, view, Vector3.forward * 10, false, true); yield return new WaitForSeconds(.05f);
                Assert.That(uses, Is.EqualTo(1));
            }
        }
    }
}
