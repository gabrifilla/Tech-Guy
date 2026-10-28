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

        [UnityTest] public IEnumerator GPRVirtualDeviceDeliverySurvivesPlayerLoop()
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                Vector2 expected = view.WorldToScreenPoint(Vector3.right * 10);
                var trace = new System.Text.StringBuilder();
                trace.AppendLine("background=" + Application.runInBackground + " focused=" + Application.isFocused + " settings=" + InputSystem.settings.backgroundBehavior);
                System.Action sample = () => trace.AppendLine("frame=" + Time.frameCount + " update=" +
                    UnityEngine.InputSystem.LowLevel.InputState.currentUpdateType + " position=" + input.Mouse.position.ReadValue() +
                    " left=" + input.Mouse.leftButton.isPressed + " w=" + input.Keyboard.wKey.isPressed +
                    " mouseCurrent=" + (Mouse.current == input.Mouse) + " keyboardCurrent=" + (Keyboard.current == input.Keyboard));
                InputSystem.onAfterUpdate += sample;
                try
                {
                    Assert.That(input.Keyboard.enabled, Is.True, "Virtual keyboard must be enabled before injection.");
                    Assert.That(input.Mouse.enabled, Is.True, "Virtual mouse must be enabled before injection.");
                    input.Keys(Key.W); BasicMouse(input, view, Vector3.right * 10, true);
                    yield return null; yield return null;
                    sample();
                    Assert.That(input.Keyboard.wKey.isPressed, Is.True, trace.ToString());
                    Assert.That(input.Mouse.leftButton.isPressed, Is.True, trace.ToString());
                    Assert.That(Vector2.Distance(input.Mouse.position.ReadValue(), expected), Is.LessThan(.01f), trace.ToString());
                }
                finally { InputSystem.onAfterUpdate -= sample; }
            }
        }
        internal sealed class ConfigPreferenceScope : IDisposable
        {
            private readonly Dictionary<string, int?> _values = new Dictionary<string, int?>();
            internal ConfigPreferenceScope()
            {
                _values["TechGuy.Settings.DashDirection"] = PlayerPrefs.HasKey("TechGuy.Settings.DashDirection") ? (int?)PlayerPrefs.GetInt("TechGuy.Settings.DashDirection") : null;
                foreach (GameControl action in Enum.GetValues(typeof(GameControl)))
                {
                    string key = "TechGuy.Settings.DirectBinding.v2." + action;
                    _values[key] = PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null;
                }
            }
            public void Dispose()
            {
                foreach (var pair in _values) { if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key); }
                PlayerPrefs.Save();
            }
        }

        [Test] public void GPRConfigMigrationKeepsPersonalizedSecondaryAndHasThreeSkills()
        {
            var legacy = GamePreferences.DefaultBindings(); legacy[1] = KeyCode.Mouse3; var copy = (KeyCode[])legacy.Clone();
            var migrated = GamePreferences.MigrateLegacyBindings(legacy);
            CollectionAssert.AreEqual(copy, legacy);
            Assert.That(migrated[1], Is.EqualTo(KeyCode.Mouse3));
            Assert.That(migrated[(int)GameControl.MoveUp], Is.EqualTo(KeyCode.W));
            Assert.That(GamePreferences.ValidateDirectBindings(migrated, out _), Is.True);
            CollectionAssert.DoesNotContain(GamePreferences.DirectActions, GameControl.Skill4);
            CollectionAssert.DoesNotContain(migrated, KeyCode.X);
        }

        [UnityTest] public IEnumerator GPRConfigRoundTripConflictCaptureAndDefaults()
        {
            using (var prefs = new ConfigPreferenceScope()) using (var input = new DirectInputScope())
            {
                BuildDirectRig(); yield return null;
                var bindings = GamePreferences.DefaultDirectBindings();
                bindings[(int)GameControl.Skill1] = KeyCode.F8;
                bindings[(int)GameControl.Move] = KeyCode.Mouse3;
                bindings[(int)GameControl.MoveUp] = KeyCode.T;
                GamePreferences.SaveBindings(bindings);
                CollectionAssert.AreEqual(bindings, GamePreferences.ReadBindings());
                var invalid = (KeyCode[])bindings.Clone(); invalid[0] = invalid[1];
                Assert.Throws<ArgumentException>(() => GamePreferences.SaveBindings(invalid));
                CollectionAssert.AreEqual(bindings, GamePreferences.ReadBindings());
                var panel = new ControlRemappingPanel(); panel.BeginCapture(GameControl.Skill2);
                Assert.That(panel.TryAssign(bindings, bindings[0]), Is.False);
                Assert.That(panel.IsCapturing, Is.True);
                Assert.That(panel.TryAssign(bindings, KeyCode.Escape), Is.False);
                Assert.That(panel.TryAssign(bindings, KeyCode.Mouse4), Is.True);
                Assert.That(panel.IsCapturing, Is.False);
                Assert.That(panel.CanApply, Is.False, "Capture click cannot also apply Settings in the same frame.");
                yield return null; Assert.That(panel.CanApply, Is.True);
                Assert.That(GamePreferences.Binding(GameControl.Skill2), Is.Not.EqualTo(KeyCode.Mouse4), "Draft must not save itself.");
                panel.BeginCapture(GameControl.Skill3); panel.Cancel(); Assert.That(panel.IsCapturing, Is.False);
                GamePreferences.SaveBindings(GamePreferences.DefaultDirectBindings());
                CollectionAssert.AreEqual(GamePreferences.DefaultDirectBindings(), GamePreferences.ReadBindings());
            }
            if (_controls) GamePreferences.UseDirectControls(_controls);
        }

        [UnityTest] public IEnumerator GPRConfigReboundMovementAndMousePrimaryUseExistingOwners()
        {
            using (var prefs = new ConfigPreferenceScope()) using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true);
                var bindings = GamePreferences.DefaultDirectBindings(); bindings[0] = KeyCode.Mouse3; bindings[(int)GameControl.MoveUp] = KeyCode.T;
                GamePreferences.SaveBindings(bindings); yield return ReadyBasicInput(input, view);
                Vector3 start = _player.transform.position;
                input.Keys(Key.W); yield return new WaitForSeconds(.1f);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01f));
                input.Keys(Key.T); yield return new WaitForSeconds(.1f);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.GreaterThan(.1f));
                input.Keys(); yield return null; yield return null;
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                BasicMouse(input, view, Vector3.right * 10, true); yield return new WaitForSeconds(.1f);
                Assert.That(attacks, Is.Zero, "Old primary must not remain as an alias.");
                InputSystem.QueueStateEvent(input.Mouse, new MouseState { position = (Vector2)view.WorldToScreenPoint(Vector3.right * 10) }.WithButton(MouseButton.Back));
                yield return new WaitForSeconds(.1f); Assert.That(attacks, Is.EqualTo(1));
            }
            if (_controls) GamePreferences.UseDirectControls(_controls);
        }

        [UnityTest] public IEnumerator GPRDashModesUseNormalizedMovementAndFacingFallback()
        {
            using (var prefs = new ConfigPreferenceScope()) using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); GamePreferences.SaveBindings(GamePreferences.DefaultDirectBindings());
                yield return ReadyBasicInput(input, view);
                var resolve = typeof(DashScript).GetMethod("ResolveDashDirection", BindingFlags.Static | BindingFlags.NonPublic);
                BasicMouse(input, view, Vector3.right * 10, false); input.Keys(Key.W, Key.D); yield return null;
                GamePreferences.SaveDashDirection(DashDirectionMode.Mouse);
                Vector3 cursor = (Vector3)resolve.Invoke(null, new object[] { _player.transform, view, Vector3.forward });
                Assert.That(cursor.x, Is.GreaterThan(.9));
                GamePreferences.SaveDashDirection(DashDirectionMode.Movement);
                Assert.That(GamePreferences.Read().DashDirection, Is.EqualTo(DashDirectionMode.Movement));
                Vector3 movement = (Vector3)resolve.Invoke(null, new object[] { _player.transform, view, Vector3.forward });
                Assert.That(movement.magnitude, Is.EqualTo(1).Within(.001));
                Assert.That(Vector3.Dot(movement, _controls.ConfiguredMovementDirection.normalized), Is.GreaterThan(.999));
                Assert.That(Vector3.Distance(movement, cursor), Is.GreaterThan(.1));
                input.Keys(); yield return null;
                _player.transform.rotation = Quaternion.LookRotation(Vector3.back);
                Vector3 fallback = (Vector3)resolve.Invoke(null, new object[] { _player.transform, view, _player.transform.forward });
                Assert.That(Vector3.Dot(fallback, Vector3.back), Is.GreaterThan(.99));
                Assert.That(Vector3.Dot(fallback, cursor), Is.LessThan(.1));
                Assert.Throws<ArgumentException>(() => GamePreferences.SaveDashDirection((DashDirectionMode)99));
            }
            if (_controls) GamePreferences.UseDirectControls(_controls);
        }

        [UnityTest] public IEnumerator GPRInteractRebindCollectsOnlyNearbyItemWithoutBasicOrMovement()
        {
            using (var prefs = new ConfigPreferenceScope()) using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); var bindings = GamePreferences.DefaultDirectBindings(); bindings[(int)GameControl.Interact] = KeyCode.H;
                GamePreferences.SaveBindings(bindings); yield return ReadyBasicInput(input, view);
                var item = GameObject.CreatePrimitive(PrimitiveType.Cube); _spawned.Add(item);
                item.transform.position = new Vector3(0, .5f, 1.5f);
                item.AddComponent<Interactable>().interactionType = InteractableType.Item;
                Physics.SyncTransforms(); BasicMouse(input, view, item.transform.position, false);
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                Vector3 start = _player.transform.position;
                input.Keys(Key.G); yield return null; yield return null;
                Assert.That(item, Is.Not.Null); Assert.That(item == null, Is.False);
                input.Keys(); yield return null; yield return null;
                input.Keys(Key.H); yield return null; yield return null;
                Assert.That(item == null, Is.True);
                Assert.That(attacks, Is.Zero); Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01f));
            }
            if (_controls) GamePreferences.UseDirectControls(_controls);
        }

        [UnityTest] public IEnumerator GPRSpecialRemapsExistingThreeKitsWithoutXOrF11Alias()
        {
            using (var prefs = new ConfigPreferenceScope()) using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); var bindings = GamePreferences.DefaultDirectBindings(); bindings[1] = KeyCode.Mouse4;
                GamePreferences.SaveBindings(bindings); yield return ReadyBasicInput(input, view);
                SetPrivate(_player, "baseMaxMana", 1500f); SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f); _player.RefreshResourceStats(true);
                var holder = _player.GetComponent<AbilityHolder>(); int uses = 0, basics = 0;
                holder.AbilityUsed += slot => { if (slot == 3) uses++; }; _controls.BasicAttackPerformed += () => basics++;
                for (int weapon = 0; weapon < 3; weapon++)
                {
                    _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[weapon])); holder.RefreshLoadout();
                    _player.mana = 1500; input.Keys(); BasicMouse(input, view, Vector3.forward * 5, false);
                    yield return null; yield return null;
                    var original = holder.ActiveAbilities[3]; float before = _player.mana;
                    if (weapon == 0) _player.GetComponent<BreakerGauntletCombat>().AddAsuraEnergy(100);
                    Assert.That(holder.GetAbilityKey(3), Is.EqualTo(KeyCode.Mouse4));
                    input.Keys(Key.F11, Key.X); yield return new WaitForSeconds(.1f);
                    Assert.That(uses, Is.EqualTo(weapon)); Assert.That(_player.mana, Is.EqualTo(before));
                    input.Keys(); yield return null;
                    InputSystem.QueueStateEvent(input.Mouse, new MouseState { position = (Vector2)view.WorldToScreenPoint(Vector3.forward * 5) }.WithButton(MouseButton.Forward));
                    yield return new WaitForSeconds(.1f);
                    Assert.That(uses, Is.EqualTo(weapon + 1)); Assert.That(basics, Is.Zero);
                    Assert.That(holder.ActiveAbilities[3], Is.SameAs(original));
                    Assert.That(_player.mana, Is.EqualTo(before - original.ManaCost).Within(.001));
                    BasicMouse(input, view, Vector3.forward * 5, false); yield return null;
                }
            }
            if (_controls) GamePreferences.UseDirectControls(_controls);
        }

        [UnityTest] public IEnumerator GPRThreeSkillsRemapAndHudKeepsSeparateSpecialWithoutX()
        {
            using (var prefs = new ConfigPreferenceScope()) using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(); var bindings = GamePreferences.DefaultDirectBindings();
                bindings[3] = KeyCode.F6; bindings[4] = KeyCode.Mouse3; bindings[5] = KeyCode.F7;
                GamePreferences.SaveBindings(bindings); yield return ReadyBasicInput(input, view);
                var holder = _player.GetComponent<AbilityHolder>(); int used = -1, uses = 0;
                holder.AbilityUsed += slot => { used = slot; uses++; };
                for (int slot = 0; slot < 3; slot++)
                {
                    var ability = holder.ActiveAbilities[slot]; float mana = _player.mana;
                    BasicMouse(input, view, Vector3.forward * 3, false);
                    if (slot == 1) InputSystem.QueueStateEvent(input.Mouse, new MouseState { position = (Vector2)view.WorldToScreenPoint(Vector3.forward * 3) }.WithButton(MouseButton.Back));
                    else input.Keys(slot == 0 ? Key.F6 : Key.F7);
                    yield return new WaitForSeconds(.1f);
                    Assert.That(used, Is.EqualTo(slot)); Assert.That(uses, Is.EqualTo(slot + 1));
                    Assert.That(holder.ActiveAbilities[slot], Is.SameAs(ability)); Assert.That(_player.mana, Is.EqualTo(mana - ability.ManaCost).Within(.001));
                    input.Keys(); BasicMouse(input, view, Vector3.forward * 3, false);
                    float end = Time.realtimeSinceStartup + 4;
                    while (holder.IsCasting) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(end)); yield return null; }
                }
                var hud = BuildSharedHud(); yield return null; yield return new WaitForSeconds(.08f);
                Assert.That(hud.Slots.Length, Is.EqualTo(5), "Three skills, one special, one dash; serialized references stay intact.");
                for (int slot = 0; slot < 4; slot++)
                {
                    var text = (TMPro.TMP_Text)BasicField(hud.Slots[slot], "_key");
                    Assert.That(text.text, Is.EqualTo(GamePreferences.KeyLabel(holder.GetAbilityKey(slot))));
                    Assert.That(text.text, Is.Not.EqualTo("X"));
                }
                Assert.That(((TMPro.TMP_Text)BasicField(hud.Slots[3], "_name")).text, Does.StartWith("ESPECIAL"));
                var special = holder.ActiveAbilities[3]; bindings[1] = KeyCode.T; GamePreferences.SaveBindings(bindings);
                yield return new WaitForSeconds(.08f);
                Assert.That(((TMPro.TMP_Text)BasicField(hud.Slots[3], "_key")).text, Is.EqualTo("T"));
                Assert.That(holder.ActiveAbilities[3], Is.SameAs(special));
            }
            if (_controls) GamePreferences.UseDirectControls(_controls);
        }

        [UnityTest] public IEnumerator GPRCaptureDeviceChangeCancelsWithoutSavingDraft()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); yield return null;
                var draft = GamePreferences.ReadBindings(); var original = (KeyCode[])draft.Clone();
                var panel = new ControlRemappingPanel(); panel.BeginCapture(GameControl.Skill1);
                var replacement = InputSystem.AddDevice<Keyboard>();
                try
                {
                    replacement.MakeCurrent(); Assert.That(panel.Update(draft), Is.True);
                    Assert.That(panel.IsCapturing, Is.False); CollectionAssert.AreEqual(original, draft);
                    Assert.That(panel.CanApply, Is.False);
                }
                finally { InputSystem.RemoveDevice(replacement); input.Keyboard.MakeCurrent(); }
            }
        }

        [UnityTest] public IEnumerator GPRPauseRebindPreservesEscapeSafetyWithoutWorldInput()
        {
            using (var prefs = new ConfigPreferenceScope()) using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); var bindings = GamePreferences.DefaultDirectBindings(); bindings[(int)GameControl.Pause] = KeyCode.F12;
                GamePreferences.SaveBindings(bindings); yield return ReadyBasicInput(input, view);
                var pause = _player.GetComponent<PauseMenuUI>(); int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                input.Keys(Key.F12); yield return null; yield return null;
                Assert.That(pause.IsPaused, Is.True); Assert.That(Time.timeScale, Is.Zero);
                input.Keys(); yield return null;
                input.Keys(Key.Escape); yield return null; yield return null;
                Assert.That(pause.IsPaused, Is.False); Assert.That(attacks, Is.Zero);
                input.Keys(); yield return null; yield return null;
            }
            if (_controls) GamePreferences.UseDirectControls(_controls);
        }

        [UnityTest] public IEnumerator GPRInteractionRecreatesLostGuiTextureWithoutChangingContext()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); yield return null; yield return null;
                var anchor = new GameObject("C01.4 texture station"); _spawned.Add(anchor);
                var owner = new GameObject("C01.4 lobby texture"); _spawned.Add(owner); owner.SetActive(false);
                var lobby = owner.AddComponent<LobbyInteraction>();
                lobby.Configure(_player.transform, new[] { new LobbyInteraction.Station { anchor = anchor.transform, title = "Test" } });
                owner.SetActive(true); yield return null; yield return null;
                var texture = (Texture2D)BasicField(lobby, "_panel"); Assert.That(texture, Is.Not.Null);
                UnityEngine.Object.DestroyImmediate(texture); yield return null; yield return null;
                var recreated = (Texture2D)BasicField(lobby, "_panel");
                Assert.That(recreated == null, Is.False, "A surviving GUIStyle cannot suppress lost texture reconstruction.");
                Assert.That(lobby.IsPanelOpen, Is.False);
            }
        }

        private IEnumerator ClickBasic(DirectInputScope input, Camera view, Vector3 point)
        {
            BasicMouse(input, view, point, true); yield return new WaitForSecondsRealtime(.025f);
            BasicMouse(input, view, point, false); yield return new WaitForSecondsRealtime(.025f);
        }

        [UnityTest] public IEnumerator GPRReleasedContextAcceptsFirstFreshClickOnFollowingFrame() => RunReleasedContextInput(true);
        [UnityTest] public IEnumerator GPRReleasedContextAcceptsFirstFreshMovementOnFollowingFrame() => RunReleasedContextInput(false);

        private IEnumerator RunReleasedContextInput(bool attack)
        {
            using (var input = new DirectInputScope())
            {
                Camera view = BuildDirectRig(true); yield return ReadyBasicInput(input, view);
                int attacks = 0; _controls.BasicAttackPerformed += () => attacks++;
                Vector3 start = _player.transform.position;
                _controls.RequireAttackRelease();
                // The holder can observe all keys released before the controller's Update.
                // This frame stays blocked; a new physical press arrives in the next input update.
                Assert.That(_controls.GameplayInputReady, Is.False);
                if (attack) BasicMouse(input, view, Vector3.forward * 10, true);
                else input.Keys(Key.W);
                yield return null; yield return null;
                Assert.That(_controls.GameplayInputReady, Is.True);
                if (attack) Assert.That(attacks, Is.EqualTo(1));
                else Assert.That(Vector3.Distance(start, _player.transform.position), Is.GreaterThan(.01f));
            }
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
                System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/" + UnityEditor.SessionState.GetString("TechGuy.Tests.GprPhase", "c01-3a") + "/combo-observation.json"),
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
                    Assert.That(_controls.GameplayInputReady, Is.True, "Released context=" + context);
                    Assert.That(input.Mouse.enabled, Is.True, "Mouse context=" + context);
                    Assert.That(_controls.CanStartDirectionalBasic, Is.True, "Basic readiness context=" + context);
                    yield return ClickBasic(input, view, Vector3.forward * 10);
                    Assert.That(attacks, Is.EqualTo(context + 1), "Fresh click context=" + context + " release=" + BasicField(_controls, "_waitForAttackRelease") + " pointer=" + input.Mouse.position.ReadValue()); yield return ReadyNextBasic();
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
                input.Keys(Key.F11);
                yield return new WaitForSeconds(.1f);
                Assert.That(uses, Is.EqualTo(0), "Legacy Skill4 must not activate direct Asura.");
                Assert.That(_player.mana, Is.EqualTo(before).Within(.001f));
                Assert.That(combat.Energy, Is.EqualTo(100));
                input.Keys(); yield return null;
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
