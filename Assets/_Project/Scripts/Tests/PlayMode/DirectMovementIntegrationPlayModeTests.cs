using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private sealed class DirectInputScope : IDisposable
        {
            private readonly Keyboard _oldKeyboard = Keyboard.current;
            private readonly Mouse _oldMouse = Mouse.current;
            private readonly Camera[] _sceneCameras = Camera.allCameras;
            internal readonly Keyboard Keyboard;
            internal readonly Mouse Mouse;
            private readonly bool _restoreKeyboard, _restoreMouse;
            private readonly InputSettings.BackgroundBehavior _background = InputSystem.settings.backgroundBehavior;
            private readonly InputSettings.EditorInputBehaviorInPlayMode _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            internal DirectInputScope()
            {
                // Real-scene tests can leave a live MainCamera. The fixture's projected pointer
                // and executor Camera.main must resolve through the SAME test camera.
                foreach (var camera in _sceneCameras) if (camera) camera.enabled = false;
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                _restoreKeyboard = _oldKeyboard != null && _oldKeyboard.enabled;
                _restoreMouse = _oldMouse != null && _oldMouse.enabled;
                if (_restoreKeyboard) InputSystem.DisableDevice(_oldKeyboard);
                if (_restoreMouse) InputSystem.DisableDevice(_oldMouse);
                // AddDevice applies the current background policy immediately. Configure it
                // first so a background Editor cannot create disabled virtual devices.
                Keyboard = InputSystem.AddDevice<Keyboard>("GPR-C01.2b virtual keyboard");
                Mouse = InputSystem.AddDevice<Mouse>("GPR-C01.2b virtual mouse");
                Keyboard.MakeCurrent(); Mouse.MakeCurrent();
            }
            internal void Keys(params Key[] keys) => InputSystem.QueueStateEvent(Keyboard, new KeyboardState(keys));
            internal void ConfiguredControl(GameControl action, bool held, params Key[] keys)
            {
                KeyCode code = GamePreferences.Binding(action);
                ushort buttons = 0;
                if (held && code >= KeyCode.Mouse0 && code <= KeyCode.Mouse4)
                    buttons = (ushort)(1 << ((int)code - (int)KeyCode.Mouse0));
                else if (held)
                {
                    object[] args = { code, Key.None };
                    var conversion = typeof(GamePreferences).GetMethod("TryKeyboardKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                    Assert.IsTrue((bool)conversion.Invoke(null, args), "Configured action must be injectable without rewriting player preferences.");
                    var expanded = new Key[keys.Length + 1]; Array.Copy(keys, expanded, keys.Length); expanded[keys.Length] = (Key)args[1]; keys = expanded;
                }
                InputSystem.QueueStateEvent(Mouse, new MouseState { buttons = buttons }); Keys(keys);
            }
            internal void Aim(Camera camera, Vector3 point) => InputSystem.QueueStateEvent(Mouse,
                new MouseState { position = (Vector2)camera.WorldToScreenPoint(point) });
            public void Dispose()
            {
                foreach (var camera in _sceneCameras) if (camera) camera.enabled = true;
                InputSystem.RemoveDevice(Keyboard); InputSystem.RemoveDevice(Mouse);
                if (_restoreKeyboard && _oldKeyboard != null) InputSystem.EnableDevice(_oldKeyboard);
                if (_restoreMouse && _oldMouse != null) InputSystem.EnableDevice(_oldMouse);
                _oldKeyboard?.MakeCurrent(); _oldMouse?.MakeCurrent();
                InputSystem.settings.backgroundBehavior = _background;
                InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            }
        }

        private Camera BuildDirectRig(bool authoredBasic = false)
        {
            if (authoredBasic) BuildAuthoredBasicPlayer(); else BuildFluidMovementRig();
            var camera = new GameObject("GPR-C01.2b Test Camera").AddComponent<Camera>();
            _spawned.Add(camera.gameObject);
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 18, -12);
            camera.transform.LookAt(Vector3.zero);
            SetPrivate(_controls, "mainCamera", camera);
            _controls.SetDirectMovementEnabled(true);
            return camera;
        }

        [Test]
        public void GPRDirectBindingsMigrateWithoutLosingCustomSpecialOrChangingLegacy()
        {
            var legacy = GamePreferences.DefaultBindings();
            legacy[(int)GameControl.Skill4] = KeyCode.F11;
            var copy = (KeyCode[])legacy.Clone();
            var migrated = GamePreferences.MigrateLegacyBindings(legacy);
            Assert.That(migrated[(int)GameControl.Skill1], Is.EqualTo(KeyCode.Q));
            Assert.That(migrated[(int)GameControl.Skill2], Is.EqualTo(KeyCode.E));
            Assert.That(migrated[(int)GameControl.Skill3], Is.EqualTo(KeyCode.R));
            Assert.That(migrated[(int)GameControl.Skill4], Is.EqualTo(KeyCode.F11));
            CollectionAssert.DoesNotContain(migrated, KeyCode.X);
            Assert.That(legacy, Is.EqualTo(copy));
            legacy[(int)GameControl.Skill1] = KeyCode.A;
            foreach (var key in System.Linq.Enumerable.Take(GamePreferences.MigrateLegacyBindings(legacy), 8)) Assert.That(GamePreferences.IsMovementKey(key), Is.False);
        }

        [UnityTest]
        public IEnumerator GPRDirectVirtualMovementNormalizesDiagonalsAndDoesNotCastW()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig();
                int casts = 0; _player.GetComponent<AbilityHolder>().AbilityUsed += _ => casts++;
                input.Aim(camera, Vector3.right * 10); input.Keys(); yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true); // Explicit virtual focus in a background Editor.
                input.Keys(); yield return null; yield return null;
                Assert.That(_controls.DirectInputHasFocus, Is.True);
                Vector3 start = _player.transform.position;
                float time = Time.time;
                input.Keys(Key.W); yield return new WaitForSeconds(.25f);
                float elapsed = Time.time - time;
                float cardinal = Vector3.ProjectOnPlane(_player.transform.position - start, Vector3.up).magnitude / elapsed;
                Assert.That(cardinal, Is.EqualTo(_agent.speed).Within(.6));
                Assert.That(_player.transform.forward.x, Is.GreaterThan(.8));
                Assert.That(_controls.ActualMovementVelocity.magnitude, Is.GreaterThan(1));
                Assert.That(_controls.CurrentMoveDirection.z, Is.GreaterThan(.9));
                input.Keys(); yield return null; yield return null;
                _agent.Warp(Vector3.zero); yield return null;
                start = _player.transform.position; time = Time.time;
                input.Keys(Key.W, Key.D); yield return new WaitForSeconds(.25f);
                elapsed = Time.time - time;
                float diagonal = Vector3.ProjectOnPlane(_player.transform.position - start, Vector3.up).magnitude / elapsed;
                Assert.That(diagonal / cardinal, Is.EqualTo(1).Within(.08));
                Assert.That(casts, Is.EqualTo(0), "W is movement, never the legacy W skill.");
                input.Keys(); yield return null; yield return null;
                start = _player.transform.position; yield return new WaitForSeconds(.08f);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01));
                Assert.That(_agent.hasPath, Is.False);
                string path = Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/" + SessionState.GetString("TechGuy.Tests.GprPhase", "c01-2b") + "/integration-movement.json");
                File.WriteAllText(path, JsonUtility.ToJson(new DirectMeasurement { cardinal = cardinal, diagonal = diagonal, fixtureSpeed = _agent.speed, casts = casts }));
            }
        }
        [Serializable] private sealed class DirectMeasurement { public float cardinal, diagonal, fixtureSpeed; public int casts; }

        [UnityTest]
        public IEnumerator GPRDirectRootedBasicKeepsPoseContactAndDamage()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig(true);
                var victim = BasicVictim(Vector3.forward * 1.2f);
                float initialHealth = victim.health;
                int hits = 0; victim.DamageReceived += (_, __) => hits++;
                input.Keys(); input.Aim(camera, Vector3.right * 8); yield return null; yield return null;
                Assert.That(_controls.TryDirectionalBasicAttack(victim.transform.position), Is.True);
                Vector3 start = _player.transform.position;
                input.Keys(Key.W);
                float guard = Time.realtimeSinceStartup + 2;
                while (_controls.HasScheduledBasic && _controls.BasicImpactProgress < _controls.BasicImpactFraction && Time.realtimeSinceStartup < guard)
                {
                    Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01));
                    Assert.That(Vector3.Dot(_player.transform.forward, Vector3.forward), Is.GreaterThan(.99));
                    yield return null;
                }
                input.Keys(); yield return new WaitForSeconds(.3f);
                Assert.That(hits, Is.EqualTo(1));
                Assert.That(victim.health, Is.LessThan(initialHealth));
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectMotorPullAndDashRespectTheSameUnbakedWall()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig(); input.Keys(); yield return null; yield return null;
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); _spawned.Add(wall);
                wall.transform.position = new Vector3(2, 1, 0); wall.transform.localScale = new Vector3(.1f, 3, 20);
                Physics.SyncTransforms();
                _controls.MoveThroughDirectMotor(Vector3.right * 10, true); yield return null;
                Assert.That(_player.transform.position.x, Is.LessThan(1.7));
                _agent.Warp(Vector3.zero); yield return null;
                var source = new GameObject("Pull source"); _spawned.Add(source); source.transform.position = Vector3.right * 10;
                _player.BeginExternalPull(source.transform, .25f);
                yield return new WaitForSeconds(.4f);
                Assert.That(_controls.enabled, Is.True);
                Assert.That(_player.transform.position.x, Is.LessThan(1.7));
                _agent.Warp(Vector3.zero); yield return null;
                input.Aim(camera, Vector3.right * 10); yield return null;
                _controls.SendMessage("OnApplicationFocus", true);
                var dash = Object.Instantiate(AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset"));
                try
                {
                    Assert.That(_player.GetComponent<AbilityHolder>().TryUseDash(dash), Is.True);
                    yield return new WaitForSeconds(.4f);
                    Assert.That(_player.transform.position.x, Is.GreaterThan(.5), "The real dash must actually have moved before hitting the wall.");
                    Assert.That(_player.transform.position.x, Is.LessThan(1.7));
                    Assert.That(dash.IsDashing(_player.gameObject), Is.False);
                }
                finally { Object.DestroyImmediate(dash); }
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectFocusAndPauseRequireKeyboardRelease()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); input.Keys(); yield return null; yield return null;
                input.Keys(Key.W); yield return new WaitForSeconds(.1f);
                _controls.SendMessage("OnApplicationFocus", false);
                Vector3 start = _player.transform.position;
                yield return new WaitForSeconds(.1f);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01));
                _controls.SendMessage("OnApplicationFocus", true);
                yield return new WaitForSeconds(.08f);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01));
                input.Keys(); yield return null; yield return null;
                input.Keys(Key.W); yield return new WaitForSeconds(.1f);
                Assert.That(_player.transform.position.z - start.z, Is.GreaterThan(.3));
                Time.timeScale = 0; yield return null;
                start = _player.transform.position;
                Time.timeScale = 1; yield return new WaitForSeconds(.08f);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01));
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectGauntletAdvanceRetainsExclusiveCastOwnership()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig(); input.Keys(); input.Aim(camera, Vector3.forward * 15);
                yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true);
                var holder = _player.GetComponent<AbilityHolder>();
                float mana = _player.mana;
                Assert.That(holder.TryUseAbility(0), Is.True);
                input.Keys(Key.A);
                float guard = Time.realtimeSinceStartup + 3;
                while (holder.IsCasting && Time.realtimeSinceStartup < guard)
                {
                    Assert.That(Mathf.Abs(_player.transform.position.x), Is.LessThan(.01));
                    yield return null;
                }
                input.Keys();
                Assert.That(holder.IsCasting, Is.False);
                Assert.That(_player.transform.position.z, Is.GreaterThan(.3));
                Assert.That(_player.mana, Is.LessThan(mana));
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectSpearCastDoesNotAcquireKeyboardOwnership()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig();
                _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Spear/Spear"));
                var holder = _player.GetComponent<AbilityHolder>(); holder.RefreshLoadout();
                input.Keys(); input.Aim(camera, Vector3.forward * 12); yield return null; yield return null;
                Assert.That(holder.TryUseAbility(0), Is.True);
                input.Keys(Key.A); yield return new WaitForSeconds(.08f);
                Assert.That(Mathf.Abs(_player.transform.position.x), Is.LessThan(.01));
                input.Keys(); yield return WaitForMovementCastToFinish(holder);
                Assert.That(_agent.hasPath, Is.False);
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectDashHitstopSuspendsDisplacementWithoutMintingExtraDash()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig(); input.Keys(); input.Aim(camera, Vector3.forward * 12);
                yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true);
                var dash = Object.Instantiate(AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset"));
                try
                {
                    var holder = _player.GetComponent<AbilityHolder>();
                    Vector3 start = _player.transform.position;
                    Assert.That(holder.TryUseDash(dash), Is.True);
                    yield return null;
                    using (var feedback = _player.BeginImpactFeedback(.15f)) feedback.Scope.ConfirmDamage(true);
                    Vector3 stopped = _player.transform.position;
                    yield return new WaitForSecondsRealtime(.07f);
                    Assert.That(Vector3.Distance(stopped, _player.transform.position), Is.LessThan(.01));
                    Assert.That(dash.IsDashing(_player.gameObject), Is.True);
                    yield return new WaitForSeconds(.5f);
                    Assert.That(dash.IsDashing(_player.gameObject), Is.False);
                    Assert.That(Vector3.Distance(start, _player.transform.position), Is.EqualTo(dash.dashVelocity * dash.dashTime).Within(.8));
                }
                finally { Object.DestroyImmediate(dash); }
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectBowKitingStepUsesLastActualLocomotionAtAttackAcceptance()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig();
                _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow"));
                _player.GetComponent<AbilityHolder>().RefreshLoadout();
                var enemy = BuildEnemy(Vector3.left * 3).actor;
                _player.gameObject.AddComponent<KitingStepCoordinator>().Configure(_player, _controls, _agent, 3);
                input.Keys(); input.Aim(camera, enemy.transform.position); yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true);
                input.Keys(); yield return null; yield return null;
                input.Keys(Key.D); yield return new WaitForSeconds(.1f);
                Assert.That(_controls.CurrentMoveDirection.x, Is.GreaterThan(.9));
                Vector3 before = _player.transform.position;
                int basics = 0; _controls.BasicAttackPerformed += () => basics++;
                input.Keys(Key.D, Key.LeftShift);
                InputSystem.QueueStateEvent(input.Mouse, new MouseState { position = (Vector2)camera.WorldToScreenPoint(enemy.transform.position) }.WithButton(MouseButton.Left));
                yield return null; yield return null;
                Assert.That(basics, Is.EqualTo(1));
                Assert.That(_player.transform.position.x - before.x, Is.GreaterThan(KitingImpulse.Distance(3) * .75f));
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectPullTakesOwnershipFromAnActiveDash()
        {
            using (var input = new DirectInputScope())
            {
                Camera camera = BuildDirectRig(); input.Keys(); input.Aim(camera, Vector3.forward * 12);
                yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true);
                var dash = Object.Instantiate(AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset"));
                try
                {
                    _controls.dashScript = dash;
                    Assert.That(_player.GetComponent<AbilityHolder>().TryUseDash(dash), Is.True);
                    yield return null;
                    var source = new GameObject("GPR pull priority source"); _spawned.Add(source);
                    source.transform.position = Vector3.left * 4;
                    _player.BeginExternalPull(source.transform, .2f);
                    yield return null; yield return null;
                    Assert.That(dash.IsDashing(_player.gameObject), Is.False);
                    yield return new WaitForSeconds(.3f);
                    Assert.That(_controls.enabled, Is.True);
                    Assert.That(_player.transform.position.x, Is.LessThan(-1));
                    Assert.That(Mathf.Abs(_player.transform.position.z), Is.LessThan(.2));
                }
                finally { Object.DestroyImmediate(dash); }
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectTraversalContactPoseIsImmediateAndCannotCrossSolid()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); input.Keys(); yield return null; yield return null;
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); _spawned.Add(wall);
                wall.transform.position = new Vector3(2, 1, 0); wall.transform.localScale = new Vector3(.1f, 3, 10);
                Physics.SyncTransforms();
                _controls.MoveThroughDirectMotor(Vector3.right * 8, true, PlayerMovementSource.Action, true);
                Assert.That(_player.transform.position.x, Is.GreaterThan(.5));
                Assert.That(_player.transform.position.x, Is.LessThan(1.7));
                Assert.That(Vector3.Distance(_player.transform.position, _agent.nextPosition), Is.LessThan(.001));
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectSettingsRoundTripPreservesLegacyBindings()
        {
            using (var input = new DirectInputScope())
            {
                const string prefix = "TechGuy.Settings.DirectBinding.v2.";
                int count = GamePreferences.DefaultDirectBindings().Length;
                var existed = new bool[count]; var values = new int[count]; var legacy = new int[count];
                for (int i = 0; i < count; i++)
                {
                    string key = prefix + (GameControl)i;
                    existed[i] = PlayerPrefs.HasKey(key); values[i] = PlayerPrefs.GetInt(key);
                    legacy[i] = PlayerPrefs.GetInt("TechGuy.Settings.Binding." + (GameControl)i, -999);
                }
                try
                {
                    BuildDirectRig(); input.Keys(); yield return null;
                    var bindings = GamePreferences.ReadBindings();
                    KeyCode custom = KeyCode.F8;
                    while (Array.IndexOf(bindings, custom) >= 0) custom++;
                    bindings[(int)GameControl.Skill1] = custom;
                    GamePreferences.SaveBindings(bindings);
                    Assert.That(GamePreferences.Binding(GameControl.Skill1), Is.EqualTo(custom));
                    for (int i = 0; i < count; i++) Assert.That(PlayerPrefs.GetInt("TechGuy.Settings.Binding." + (GameControl)i, -999), Is.EqualTo(legacy[i]));
                    bindings[(int)GameControl.Skill1] = KeyCode.W;
                    Assert.Throws<ArgumentException>(() => GamePreferences.SaveBindings(bindings));
                    Assert.That(GamePreferences.Binding(GameControl.Skill1), Is.EqualTo(custom), "Rejected Apply cannot partially write bindings.");
                }
                finally
                {
                    for (int i = 0; i < count; i++)
                    {
                        string key = prefix + (GameControl)i;
                        if (existed[i]) PlayerPrefs.SetInt(key, values[i]); else PlayerPrefs.DeleteKey(key);
                    }
                    PlayerPrefs.Save();
                    if (_controls) GamePreferences.UseDirectControls(_controls);
                }
            }
        }

        [UnityTest]
        public IEnumerator GPRDirectBowCastMovesWithItsAuthoredSpeedAndRestoresIt()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig();
                _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow"));
                var holder = _player.GetComponent<AbilityHolder>(); holder.RefreshLoadout();
                input.Keys(); yield return null; yield return null;
                float speed = _agent.speed;
                Assert.That(holder.TryUseAbility(0), Is.True);
                Assert.That(holder.MovementAllowedWhileCasting, Is.True);
                Vector3 start = _player.transform.position;
                input.Keys(Key.W); yield return new WaitForSeconds(.1f);
                Assert.That(_player.transform.position.z - start.z, Is.GreaterThan(.05));
                input.Keys(); yield return WaitForMovementCastToFinish(holder);
                Assert.That(_agent.speed, Is.EqualTo(speed).Within(.01));
            }
        }

        [UnityTest]
        public IEnumerator GPRSafetySlidingPreservesWallCornerAndDistanceBudget()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); input.Keys(); yield return null; yield return null;
                var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); _spawned.Add(wall);
                wall.transform.position = new Vector3(2, 1, 0); wall.transform.localScale = new Vector3(.1f, 3, 20);
                Physics.SyncTransforms();
                var motor = new PlayerNavigationMotor(_agent);
                motor.Move(new Vector3(3, 0, 3), false, true, false); yield return null;
                Vector3 blocked = _player.transform.position;
                _agent.Warp(Vector3.zero); yield return null;
                motor.Move(new Vector3(3, 0, 3), false, true, true); yield return null;
                Vector3 sliding = _player.transform.position;
                Assert.That(sliding.x, Is.LessThan(1.7));
                Assert.That(sliding.z, Is.GreaterThan(blocked.z + .5f));
                Assert.That(sliding.magnitude, Is.LessThanOrEqualTo(new Vector3(3, 0, 3).magnitude + .01f));
                _agent.Warp(Vector3.zero); yield return null;
                var corner = GameObject.CreatePrimitive(PrimitiveType.Cube); _spawned.Add(corner);
                corner.transform.position = new Vector3(0, 1, 2); corner.transform.localScale = new Vector3(20, 3, .1f);
                Physics.SyncTransforms();
                for (int i = 0; i < 40; i++) { motor.Move(new Vector3(.1f, 0, .1f), false, true, true); yield return null; }
                Assert.That(_player.transform.position.x, Is.LessThan(1.7));
                Assert.That(_player.transform.position.z, Is.LessThan(1.7));
                Vector3 stopped = _player.transform.position;
                for (int i = 0; i < 10; i++) { motor.Move(new Vector3(.1f, 0, .1f), false, true, true); yield return null; }
                Assert.That(Vector3.Distance(stopped, _player.transform.position), Is.LessThan(.01f));
                System.IO.File.WriteAllText(System.IO.Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/" + SessionState.GetString("TechGuy.Tests.GprPhase", "c01-2c") + "/wall-comparison.json"),
                    JsonUtility.ToJson(new SafetyMeasurement { blocked = blocked, sliding = sliding, corner = stopped, speed = _agent.speed }));
            }
        }
        [System.Serializable] private sealed class SafetyMeasurement { public Vector3 blocked, sliding, corner; public float speed; }

        [UnityTest]
        public IEnumerator GPRSafetyNavEdgeSlidesButNeverLeavesTheMesh()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); input.Keys(); yield return null; yield return null;
                Assert.That(_agent.Raycast(Vector3.forward * 100, out var edge), Is.True);
                _agent.Warp(edge.position - Vector3.forward * .15f); yield return null;
                Vector3 start = _player.transform.position;
                var motor = new PlayerNavigationMotor(_agent);
                for (int i = 0; i < 30; i++) { motor.Move(new Vector3(.05f, 0, .05f), false, true, true); yield return null; }
                Assert.That(_player.transform.position.x - start.x, Is.GreaterThan(.8f));
                Assert.That(_player.transform.position.z, Is.LessThanOrEqualTo(edge.position.z + .01f));
                Assert.That(_agent.isOnNavMesh, Is.True);
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(_player.transform.position, out var point, .04f, _agent.areaMask), Is.True);
                Assert.That(Vector3.Distance(point.position, _player.transform.position), Is.LessThan(.04f));
            }
        }

        [UnityTest]
        public IEnumerator GPRSafetyPauseFocusDisableRequireAllGameplayKeysReleased()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); var holder = _player.GetComponent<AbilityHolder>();
                input.Keys(); yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true); input.Keys(); yield return null; yield return null;
                var pause = _player.GetComponent<PauseMenuUI>();
                for (int context = 0; context < 3; context++)
                {
                    input.Keys(Key.W); yield return new WaitForSeconds(.06f);
                    if (context == 0) pause.Pause();
                    else if (context == 1) _controls.SendMessage("OnApplicationFocus", false);
                    else _controls.enabled = false;
                    input.Keys(Key.W, Key.Q); yield return null;
                    float mana = _player.mana; Vector3 start = _player.transform.position;
                    Assert.That(holder.TryUseAbility(0), Is.False);
                    if (context == 0) pause.Resume();
                    else if (context == 1) _controls.SendMessage("OnApplicationFocus", true);
                    else _controls.enabled = true;
                    yield return null; yield return null;
                    Assert.That(holder.TryUseAbility(0), Is.False, "Held Q cannot cross a context boundary.");
                    Assert.That(_player.mana, Is.EqualTo(mana));
                    Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01f));
                    input.Keys(); yield return null; yield return null;
                    Assert.That(_controls.GameplayInputReady, Is.True);
                    input.Keys(Key.W); yield return new WaitForSeconds(.06f);
                    Assert.That(_player.transform.position.z - start.z, Is.GreaterThan(.1f));
                    input.Keys(); yield return null; yield return null;
                }
                Assert.That(holder.TryUseAbility(0), Is.True, "A fresh command still reaches the real executor.");
            }
        }

        [UnityTest]
        public IEnumerator GPRSafetyModalSameFrameOpenCloseDropsUncommittedInput()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); input.Keys(); yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true); yield return null; yield return null;
                var pause = _player.GetComponent<PauseMenuUI>();
                pause.Pause(); pause.Resume(); // No intervening gameplay Update.
                Assert.That(_controls.GameplayInputReady, Is.False);
                input.Keys(Key.Q, Key.W); yield return null; yield return null;
                Assert.That(_player.GetComponent<AbilityHolder>().TryUseAbility(0), Is.False);
                input.Keys(); yield return null; yield return null;
                Assert.That(_controls.GameplayInputReady, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator GPRSafetyRewardCloseRequiresReleaseBeforeMovementOrSkill()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); var run = _player.gameObject.AddComponent<RunBoons>();
                input.Keys(); yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true); yield return null; yield return null;
                run.OfferReward(1); Assert.That(run.IsChoosing, Is.True);
                input.Keys(Key.W, Key.Q); yield return null;
                var holder = _player.GetComponent<AbilityHolder>();
                Assert.That(holder.TryUseAbility(0), Is.False);
                Assert.That(run.Choose(0), Is.True);
                Vector3 start = _player.transform.position;
                yield return null; yield return null;
                Assert.That(holder.TryUseAbility(0), Is.False);
                Assert.That(Vector3.Distance(start, _player.transform.position), Is.LessThan(.01f));
                input.Keys(); yield return null; yield return null;
                Assert.That(_controls.GameplayInputReady, Is.True);
            }
        }

        [UnityTest]
        public IEnumerator GPRSafetyRecoveryIntentExpiresDuringMeasuredHalfSecondHitch()
        {
            var holder = BuildFluidMovementRig(); yield return null;
            Assert.That(holder.TryUseAbility(0), Is.True); yield return WaitForMovementRecovery(holder);
            Vector3 latest = Vector3.left * 8;
            Assert.That(_controls.TryRequestGroundMovement(latest), Is.True);
            float clock = (float)typeof(CharControlScript).GetField("_movementInputClock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_controls);
            System.Threading.Thread.Sleep(500); yield return null;
            yield return WaitForMovementCastToFinish(holder); yield return null;
            float after = (float)typeof(CharControlScript).GetField("_movementInputClock", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(_controls);
            Assert.That(after - clock, Is.GreaterThan(.35f));
            Assert.That(_controls.HasPendingGroundMovement, Is.False);
            Assert.That(Vector3.Distance(latest, _agent.destination), Is.GreaterThan(1f), "An expired intention must never steer the player.");
        }

        [UnityTest]
        public IEnumerator GPRSafetyIntegratedMotorFollowsBakedSlopeWithoutLeavingItsSurface()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); input.Keys(); yield return null; yield return null;
                var source = new UnityEngine.AI.NavMeshBuildSource { shape = UnityEngine.AI.NavMeshBuildSourceShape.Box,
                    size = new Vector3(6, .2f, 12), transform = Matrix4x4.TRS(Vector3.right * 100, Quaternion.Euler(-15, 0, 0), Vector3.one), area = 0 };
                var data = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(UnityEngine.AI.NavMesh.GetSettingsByID(_agent.agentTypeID),
                    new System.Collections.Generic.List<UnityEngine.AI.NavMeshBuildSource> { source }, new Bounds(Vector3.right * 100, new Vector3(20, 20, 20)), Vector3.zero, Quaternion.identity);
                var instance = UnityEngine.AI.NavMesh.AddNavMeshData(data);
                try
                {
                    Assert.That(UnityEngine.AI.NavMesh.SamplePosition(Vector3.right * 100, out var start, 1, _agent.areaMask), Is.True);
                    Assert.That(_agent.Warp(start.position), Is.True); yield return null;
                    var motor = new PlayerNavigationMotor(_agent);
                    for (int n = 0; n < 20; n++) { motor.Move(Vector3.forward * .1f, false, true, true); yield return null; }
                    Vector3 end = _player.transform.position;
                    Assert.That(end.y - start.position.y, Is.GreaterThan(.3f));
                    Assert.That(UnityEngine.AI.NavMesh.SamplePosition(end, out var onSurface, .06f, _agent.areaMask), Is.True);
                    Assert.That(Vector3.Distance(end, onSurface.position), Is.LessThan(.06f));
                }
                finally { instance.Remove(); Object.DestroyImmediate(data); }
            }
        }

        [UnityTest]
        public IEnumerator GPRSafetyContextualRHasOneOwnerAndCannotFallThroughOnClose()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(); input.ConfiguredControl(GamePreferences.InteractionAction, false); yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true); yield return null; yield return null;
                var station = new GameObject("GPR-C01.2c informational station"); _spawned.Add(station);
                var owner = new GameObject("GPR-C01.2c lobby context"); _spawned.Add(owner); owner.SetActive(false);
                var lobby = owner.AddComponent<LobbyInteraction>();
                lobby.Configure(_player.transform, new[] { new LobbyInteraction.Station { anchor = station.transform, title = "Test", description = "Test" } });
                var holder = _player.GetComponent<AbilityHolder>(); SetPrivate(holder, "_lobbyInteraction", lobby);
                owner.SetActive(true); yield return null;
                int uses = 0; holder.AbilityUsed += _ => uses++;
                Assert.That(lobby.BlocksAbilityInput(GamePreferences.Binding(GameControl.Skill3)), Is.False);
                input.ConfiguredControl(GamePreferences.InteractionAction, true); yield return null; yield return null;
                Assert.That(lobby.IsPanelOpen, Is.True); Assert.That(uses, Is.EqualTo(0));
                input.ConfiguredControl(GamePreferences.InteractionAction, false); yield return null; yield return null;
                input.ConfiguredControl(GamePreferences.InteractionAction, true, Key.W); yield return null; yield return null;
                Assert.That(lobby.IsPanelOpen, Is.False); Assert.That(uses, Is.EqualTo(0));
                Assert.That(_controls.GameplayInputReady, Is.False);
                input.ConfiguredControl(GamePreferences.InteractionAction, false); yield return null; yield return null;
                Assert.That(_controls.GameplayInputReady, Is.True);
            }
        }

        [Test]
        public void GPRSafetyDiscardingUncommittedBasicInputPreservesCadence()
        {
            var driver = new BasicAttackDriver(); driver.QueueTap();
            Assert.That(driver.TryTakeAttack(1, 1), Is.True);
            driver.QueueTap(); driver.SetHold(true); driver.DiscardPendingInput();
            Assert.That(driver.HasAttackIntent, Is.False);
            driver.SetHold(true);
            Assert.That(driver.TryTakeAttack(1.1f, 1), Is.False);
            Assert.That(driver.TryTakeAttack(2, 1), Is.True);
        }

        [UnityTest]
        public IEnumerator GPRSafetyPausingKeepsAcceptedBasicImpactButDropsFutureInput()
        {
            using (var input = new DirectInputScope())
            {
                BuildDirectRig(true); var victim = BasicVictim(Vector3.forward * 1.2f);
                int hits = 0; victim.DamageReceived += (_, __) => hits++;
                input.Keys(); yield return null; yield return null;
                _controls.SendMessage("OnApplicationFocus", true); yield return null; yield return null;
                Assert.That(_controls.TryDirectionalBasicAttack(victim.transform.position), Is.True);
                var pause = _player.GetComponent<PauseMenuUI>(); pause.Pause();
                Assert.That(_controls.HasScheduledBasic, Is.True);
                yield return new WaitForSecondsRealtime(.08f);
                Assert.That(hits, Is.EqualTo(0)); Assert.That(_controls.HasScheduledBasic, Is.True);
                pause.Resume(); input.Keys(); yield return new WaitForSeconds(.6f);
                Assert.That(hits, Is.EqualTo(1)); Assert.That(_controls.HasScheduledBasic, Is.False);
            }
        }
    }
}
