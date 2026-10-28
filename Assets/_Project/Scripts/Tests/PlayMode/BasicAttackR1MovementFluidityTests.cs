using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Controls;

namespace TechGuy.Tests
{
    // Uses the existing real player/agent/NavMesh fixture and the shipped resource Gauntlet Q.
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest]
        public IEnumerator MovementDuringRecoveryKeepsTheCast_UsesLatestValidOrderOnce()
        {
            AbilityHolder holder = BuildFluidMovementRig();
            using (var trace = new RecoveryMovementTrace(_controls, holder, _agent))
            {
                yield return null;
                Assert.IsTrue(holder.TryUseAbility(0));
                Assert.IsFalse(holder.CanQueueRecoveryMovement, "Startup must not accept recovery movement.");
                Assert.IsFalse(_controls.TryRequestGroundMovement(Vector3.right * 9f));
                Assert.IsTrue(holder.IsCasting);
                yield return WaitForMovementRecovery(holder);
                Vector3 first = Vector3.right * 9f, latest = Vector3.left * 8f;
                float manaAfterCast = _player.mana;
                Vector3 previousDestination = _agent.destination;
                Assert.IsTrue(_controls.TryRequestGroundMovement(first));
                Assert.IsTrue(_controls.TryRequestGroundMovement(latest));
                Assert.IsFalse(_controls.TryRequestGroundMovement(new Vector3(float.NaN, 0, 0)));
                Assert.IsFalse(_controls.TryRequestGroundMovement(_player.transform.position + Vector3.right * .1f));
                Assert.IsTrue(holder.IsCasting, "A movement click must not cancel the recovering Q.");
                Assert.IsTrue(_controls.HasPendingGroundMovement);
                Assert.AreEqual(manaAfterCast, _player.mana, "A ground intention has no combat cost.");
                Assert.That(Vector3.Distance(previousDestination, _agent.destination), Is.LessThan(.01f), "The agent is not steered during commitment.");
                trace.HitchIfRequested();
                yield return WaitForMovementCastToFinish(holder);
                yield return null;
                Assert.IsFalse(_controls.HasPendingGroundMovement);
                Assert.That(Vector3.Distance(latest, _agent.destination), Is.LessThan(.1f));
                Vector3 position = _player.transform.position;
                yield return new WaitForSeconds(.2f);
                Assert.That(Vector3.Distance(position, _player.transform.position), Is.GreaterThan(.2f));
                Vector3 fresh = new Vector3(0, 0, -9);
                Assert.IsTrue(_controls.TryRequestGroundMovement(fresh));
                yield return null; yield return null;
                Assert.That(Vector3.Distance(fresh, _agent.destination), Is.LessThan(.1f), "Consumed recovery input must not overwrite a later order.");
                Assert.AreEqual(0, _attackCount);
            }
        }

        [UnityTest]
        public IEnumerator InvalidGroundDestinationPreservesTheCurrentValidPath()
        {
            BuildFluidMovementRig(); yield return null;
            Vector3 valid = Vector3.right * 9f;
            Assert.IsTrue(_controls.TryRequestGroundMovement(valid));
            Assert.IsFalse(_controls.TryRequestGroundMovement(new Vector3(float.PositiveInfinity, 0, 0)));
            Assert.IsFalse(_controls.TryRequestGroundMovement(Vector3.right * 1000f));
            Assert.IsFalse(_controls.TryRequestGroundMovement(_player.transform.position + Vector3.right * .1f));
            Assert.That(Vector3.Distance(valid, _agent.destination), Is.LessThan(.1f));
            Vector3 start = _player.transform.position;
            yield return new WaitForSeconds(.2f);
            Assert.That(Vector3.Distance(start, _player.transform.position), Is.GreaterThan(.2f));
        }

        [UnityTest]
        public IEnumerator UnreachableNavMeshIslandCannotReplaceTheValidMovementOrder()
        {
            BuildFluidMovementRig(); yield return null;
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box, size = new Vector3(8f, .2f, 8f),
                transform = Matrix4x4.TRS(new Vector3(100f, -.1f, 0f), Quaternion.identity, Vector3.one), area = 0
            };
            var island = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),
                new System.Collections.Generic.List<NavMeshBuildSource> { source },
                new Bounds(Vector3.right * 100f, new Vector3(8f, 5f, 8f)), Vector3.zero, Quaternion.identity);
            Assert.IsTrue(island);
            NavMeshDataInstance instance = NavMesh.AddNavMeshData(island);
            try
            {
                Assert.IsTrue(instance.valid);
                Assert.IsTrue(NavMesh.SamplePosition(Vector3.right * 100f, out NavMeshHit sample, .75f, _agent.areaMask));
                Vector3 valid = Vector3.right * 9f;
                Assert.IsTrue(_controls.TryRequestGroundMovement(valid));
                Assert.IsFalse(_controls.TryRequestGroundMovement(sample.position), "Sampling a point is not evidence of a complete route.");
                Assert.That(Vector3.Distance(valid, _agent.destination), Is.LessThan(.1f));
                yield return null;
            }
            finally
            {
                if (instance.valid) instance.Remove();
                UnityEngine.Object.DestroyImmediate(island);
            }
        }

        [UnityTest]
        public IEnumerator PauseDiscardsRecoveryMovementAndDoesNotReplayOnResume()
        {
            AbilityHolder holder = BuildFluidMovementRig(); yield return null;
            Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder);
            Vector3 queued = Vector3.right * 9f;
            Assert.IsTrue(_controls.TryRequestGroundMovement(queued));
            Time.timeScale = 0f;
            yield return null;
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.IsFalse(_controls.TryRequestGroundMovement(Vector3.left * 9f));
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(holder.IsCasting);
            Time.timeScale = 1f;
            yield return WaitForMovementCastToFinish(holder); yield return null;
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.That(Vector3.Distance(queued, _agent.destination), Is.GreaterThan(1f));
            Assert.IsFalse(_agent.hasPath);
        }

        [UnityTest]
        public IEnumerator HitStopPreservesRecoveryIntentWithoutUsingItsRealTimeForExpiry()
        {
            AbilityHolder holder = BuildFluidMovementRig(); yield return null;
            Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder);
            Vector3 queued = Vector3.right * 9f;
            Assert.IsTrue(_controls.TryRequestGroundMovement(queued));
            using (var feedback = _player.BeginImpactFeedback(.5f)) feedback.Scope.ConfirmDamage(true);
            Assert.IsTrue(_player.IsCombatHitStopped);
            yield return new WaitForSecondsRealtime(.4f);
            Assert.IsTrue(_controls.HasPendingGroundMovement, "The 350 ms move lifetime must exclude confirmed hit-stop.");
            yield return WaitForMovementCastToFinish(holder); yield return null;
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.That(Vector3.Distance(queued, _agent.destination), Is.LessThan(.1f));
        }

        [UnityTest]
        public IEnumerator AcceptedNewSkillClearsRecoveryMovementInsteadOfReplayingItAfterward()
        {
            AbilityHolder holder = BuildFluidMovementRig(); yield return null;
            Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder, .82f);
            Vector3 queued = Vector3.right * 9f;
            Assert.IsTrue(_controls.TryRequestGroundMovement(queued));
            Assert.IsTrue(holder.TryUseAbility(2), "The shipped Q Skill cancel window is open in late recovery.");
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.IsTrue(holder.IsCasting);
            yield return WaitForMovementCastToFinish(holder); yield return null;
            Assert.IsFalse(_agent.hasPath);
            Assert.That(Vector3.Distance(queued, _agent.destination), Is.GreaterThan(1f));
        }

        [UnityTest]
        public IEnumerator AcceptedDashClearsRecoveryMovementWithoutAnOldOrderAfterTheDash()
        {
            AbilityHolder holder = BuildFluidMovementRig(); yield return null;
            var cameraObject = new GameObject("Movement dash camera"); _spawned.Add(cameraObject);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 10f, -10f);
            cameraObject.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            cameraObject.AddComponent<Camera>();
            var authoredDash = UnityEditor.AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset");
            Assert.IsTrue(authoredDash);
            var dash = UnityEngine.Object.Instantiate(authoredDash);
            try
            {
                _controls.dashScript = dash;
                Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder, .75f);
                Vector3 queued = Vector3.right * 15f;
                Assert.IsTrue(_controls.TryRequestGroundMovement(queued));
                Assert.IsTrue(holder.TryUseDash(dash));
                Assert.IsTrue(dash.IsDashing(_player.gameObject));
                Assert.IsFalse(_controls.TryRequestGroundMovement(Vector3.left * 12f), "A dash accepted before CharControl.Update still owns movement in this same frame.");
                Assert.IsFalse(_controls.HasPendingGroundMovement);
                float deadline = Time.realtimeSinceStartup + 3f;
                while (dash.IsDashing(_player.gameObject) && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(dash.IsDashing(_player.gameObject));
                yield return null; yield return null;
                Assert.IsFalse(_controls.HasPendingGroundMovement);
                Assert.IsFalse(_agent.hasPath);
                Assert.That(Vector3.Distance(queued, _agent.destination), Is.GreaterThan(1f));
            }
            finally { _controls.dashScript = null; UnityEngine.Object.DestroyImmediate(dash); }
        }

        [UnityTest]
        public IEnumerator DisableAndExternalPullDiscardRecoveryMovement()
        {
            AbilityHolder holder = BuildFluidMovementRig(); yield return null;
            Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder);
            Vector3 queued = Vector3.right * 9f;
            Assert.IsTrue(_controls.TryRequestGroundMovement(queued));
            _controls.enabled = false;
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            _controls.enabled = true;
            Assert.IsTrue(holder.IsCasting);
            Assert.IsTrue(_controls.TryRequestGroundMovement(queued));
            var source = new GameObject("External pull source"); _spawned.Add(source);
            source.transform.position = _player.transform.position + Vector3.left;
            _player.BeginExternalPull(source.transform, .05f);
            Assert.IsFalse(_controls.enabled);
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            yield return new WaitForSeconds(.1f);
            Assert.IsTrue(_controls.enabled);
            yield return WaitForMovementCastToFinish(holder); yield return null;
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.IsFalse(_agent.hasPath);
        }

        [UnityTest]
        public IEnumerator EncounterOffenseCleanupAndDeathCannotKeepQueuedMovement()
        {
            AbilityHolder holder = BuildFluidMovementRig(); yield return null;
            Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder);
            Assert.IsTrue(_controls.TryRequestGroundMovement(Vector3.right * 9f));
            Assert.IsTrue(holder.TryStopEncounterOffense());
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.IsFalse(holder.IsCasting);
            Assert.IsFalse(_agent.hasPath);
            holder.RefreshLoadout();
            Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder);
            Assert.IsTrue(_controls.TryRequestGroundMovement(Vector3.right * 9f));
            CharControlScript beforeDeath = _controls;
            _player.TakeDamage(_player.health + 1f);
            Assert.IsTrue(_player.IsDead);
            Assert.IsFalse(_controls.TryRequestGroundMovement(Vector3.left * 9f));
            yield return null;
            Assert.IsFalse(beforeDeath.HasPendingGroundMovement, "Death/destruction must release the remembered input.");
        }

        [UnityTest]
        public IEnumerator CadencedRContinuationDiscardsRecoveryMovementWithoutPublishingAnotherAbilityUse()
        {
            AbilityHolder holder = BuildFluidMovementRig();
            var run = _player.gameObject.AddComponent<RunBoons>();
            yield return null;
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            Assert.IsTrue(modifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura));
            typeof(RunBoons).GetProperty("WeaponModifiers").SetValue(run, modifiers);
            SetPrivate(_player, "baseMaxMana", 500f);
            _player.RefreshResourceStats(); _player.mana = 500f;
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            Assert.IsTrue(combat);
            combat.AddAsuraEnergy(100);
            int uses = 0;
            holder.AbilityUsed += slot => { if (slot == 3) uses++; };
            Assert.IsTrue(holder.TryUseAbility(3));
            long execution = combat.CadencedExecution;
            float manaAfterStart = _player.mana;
            Assert.AreEqual(1, uses);
            // The shipped pulse recovers for 180 ms, while offensive input lasts 120 ms.
            // Issue this fresh continuation in the last 15% (at most 60 ms) of the real pulse,
            // so the assertion exercises priority/consumption rather than legitimate input expiry.
            yield return WaitForMovementRecovery(holder, .85f);
            Vector3 queued = Vector3.right * 9f;
            Assert.IsTrue(_controls.TryRequestGroundMovement(queued));
            Assert.IsTrue(_controls.HasPendingGroundMovement);
            Assert.IsTrue(holder.TryUseAbility(3), "A fresh R press in recovery buffers the next pulse of the same execution.");
            Assert.IsTrue(holder.HasBufferedOffensiveIntent);
            Assert.IsFalse(_controls.HasPendingGroundMovement, "Accepted offensive continuation supersedes ground input immediately.");
            Assert.IsFalse(_controls.TryRequestGroundMovement(Vector3.left * 9f), "Pending offensive input owns the next opportunity before ground movement.");
            Assert.AreEqual(1, uses, "A continuation must not publish a second AbilityUsed for the same R execution.");
            Assert.AreEqual(manaAfterStart, _player.mana);

            // Observe the real runner advancing its pulse index. No cast state/timing is assigned by this test.
            var strike = typeof(BreakerGauntletCombat).GetField("_cadencedStrike",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(strike);
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((int)strike.GetValue(combat) < 1 && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.AreEqual(1, (int)strike.GetValue(combat));
            Assert.AreEqual(execution, combat.CadencedExecution);
            Assert.IsFalse(holder.HasBufferedOffensiveIntent);
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.IsFalse(_agent.hasPath);
            Assert.AreEqual(1, uses);

            deadline = Time.realtimeSinceStartup + 3f;
            while (!combat.IsCadencedWaiting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(combat.IsCadencedWaiting);
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.IsFalse(_agent.hasPath, "Superseded ground input must not execute when the second pulse returns control.");
            Assert.IsTrue(holder.TryUseAbility(3), "At the actual wait state the third pulse continues immediately.");
            Assert.AreEqual(2, (int)strike.GetValue(combat));
            Assert.IsFalse(combat.IsCadencedWaiting);
            Assert.AreEqual(1, uses, "Direct continuation also preserves the one-use contract.");
            Assert.AreEqual(manaAfterStart, _player.mana);
            yield return WaitForMovementCastToFinish(holder);
            yield return null;
            Assert.IsFalse(_controls.HasPendingGroundMovement);
            Assert.IsFalse(_agent.hasPath);
            Assert.That(Vector3.Distance(queued, _agent.destination), Is.GreaterThan(1f));
            Assert.AreEqual(1, uses);
            Assert.AreEqual(manaAfterStart, _player.mana);
        }

        [UnityTest]
        public IEnumerator DashTapDuringHitStopReachesItsExistingGateExactlyOnce()
        {
            AbilityHolder holder = BuildFluidMovementRig();
            var camera = BuildFluidInputCamera();
            var dash = ScriptableObject.CreateInstance<DashScript>();
            dash.dashAnimation = string.Empty;
            dash.cooldownTime = 1f;
            _controls.dashScript = dash;
            using (var input = new FluidityInputDevices())
            {
                try
                {
                    // Let the new camera finish its first render/projection before deriving pointer coordinates.
                    yield return new WaitForEndOfFrame();
                    int uses = 0;
                    holder.AbilityUsed += slot => { if (slot == 4) uses++; };
                    using (var feedback = _player.BeginImpactFeedback(.5f)) feedback.Scope.ConfirmDamage(true);
                    Assert.IsTrue(_player.IsCombatHitStopped);
                    input.Queue(GameControl.Dash, true, camera.WorldToScreenPoint(Vector3.right * 5f));
                    yield return null;
                    Assert.IsTrue(_player.IsCombatHitStopped, "The input must be sampled inside the real feedback stop.");
                    Assert.AreEqual(1, uses, "A one-frame dash tap must reach AbilityHolder despite frozen locomotion.");
                    input.Queue(GameControl.Dash, false, camera.WorldToScreenPoint(Vector3.right * 5f));
                    yield return null;
                    yield return new WaitForSecondsRealtime(.6f);
                    Assert.AreEqual(1, uses, "Releasing the tap must not duplicate it after hitstop ends.");
                    Assert.IsFalse(_controls.HasPendingGroundMovement);
                    float deadline = Time.realtimeSinceStartup + 3f;
                    while (dash.IsDashing(_player.gameObject) && Time.realtimeSinceStartup < deadline) yield return null;
                    Assert.IsFalse(dash.IsDashing(_player.gameObject));
                }
                finally { _controls.dashScript = null; UnityEngine.Object.DestroyImmediate(dash); }
            }
        }

        [UnityTest]
        public IEnumerator TargetClickDuringHitStopKeepsOneAttackAfterRelease()
        {
            BuildFluidMovementRig();
            var camera = BuildFluidInputCamera();
            var enemy = BuildEnemy(Vector3.right * 1.2f, 10000f);
            Physics.SyncTransforms();
            using (var input = new FluidityInputDevices())
            {
                yield return new WaitForEndOfFrame();
                Physics.SyncTransforms();
                using (var feedback = _player.BeginImpactFeedback(.5f)) feedback.Scope.ConfirmDamage(true);
                Vector2 pointer = camera.WorldToScreenPoint(enemy.actor.transform.position + Vector3.up * .2f);
                RaycastHit hit; bool crossed;
                Assert.IsTrue(WorldClickResolver.TryResolve(camera.ScreenPointToRay(pointer), _player.transform, Physics.DefaultRaycastLayers, out hit, out crossed));
                Assert.AreEqual(enemy.interactable, hit.collider.GetComponentInParent<Interactable>(), "The fixture pointer must hit the intended enemy before input is sent.");
                input.Queue(GameControl.Primary, true, pointer);
                yield return null;
                Assert.IsTrue(_player.IsCombatHitStopped);
                Assert.AreEqual(enemy.interactable, CurrentTarget(), $"The actual pointer raycast must capture the explicit target during hitstop. enabled={_controls.enabled}, navEnabled={_agent.enabled}, onMesh={_agent.isOnNavMesh}, primary={GamePreferences.WasPressed(GameControl.Primary)}, blocked={_player.GetComponent<AbilityHolder>().BlocksWorldInput}, casting={_player.GetComponent<AbilityHolder>().IsCasting}, pointer={Mouse.current.position.ReadValue()}, expectedPointer={pointer}");
                Assert.AreEqual(0, _attackCount, "Capturing the tap must not advance locomotion/combat while stopped.");
                input.Queue(GameControl.Primary, false, pointer);
                yield return null;
                float deadline = Time.realtimeSinceStartup + 3f;
                while (_attackCount == 0 && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.AreEqual(1, _attackCount, "The released tap must survive hitstop and execute once.");
                yield return new WaitForSeconds(.8f);
                Assert.AreEqual(1, _attackCount, "No hold or second tap was authorized.");
            }
        }

        private Camera BuildFluidInputCamera()
        {
            var go = new GameObject("Fluidity input camera"); _spawned.Add(go);
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 10f, -10f);
            go.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
            var camera = go.AddComponent<Camera>();
            SetPrivate(_controls, "mainCamera", camera);
            return camera;
        }

        private sealed class FluidityInputDevices : System.IDisposable
        {
            private readonly Keyboard _oldKeyboard = Keyboard.current;
            private readonly Mouse _oldMouse = Mouse.current;
            private readonly bool _keyboardEnabled, _mouseEnabled;
            private readonly Keyboard _keyboard;
            private readonly Mouse _mouse;
            public FluidityInputDevices()
            {
                _keyboardEnabled = _oldKeyboard != null && _oldKeyboard.enabled;
                _mouseEnabled = _oldMouse != null && _oldMouse.enabled;
                if (_keyboardEnabled) InputSystem.DisableDevice(_oldKeyboard);
                if (_mouseEnabled) InputSystem.DisableDevice(_oldMouse);
                _keyboard = InputSystem.AddDevice<Keyboard>();
                _mouse = InputSystem.AddDevice<Mouse>();
            }
            public void Queue(GameControl control, bool pressed, Vector2 pointer)
            {
                // Resolve the real configured binding without writing the user's preferences.
                var method = typeof(GamePreferences).GetMethod("Button", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
                var button = (ButtonControl)method.Invoke(null, new object[] { GamePreferences.Binding(control) });
                Assert.IsNotNull(button);
                var key = button as KeyControl;
                var mouseState = new MouseState { position = pointer };
                if (key != null)
                    InputSystem.QueueStateEvent(_keyboard, pressed ? new KeyboardState(key.keyCode) : new KeyboardState());
                else if (pressed)
                {
                    MouseButton which = button == _mouse.leftButton ? MouseButton.Left : button == _mouse.rightButton ? MouseButton.Right :
                        button == _mouse.middleButton ? MouseButton.Middle : button == _mouse.backButton ? MouseButton.Back : MouseButton.Forward;
                    mouseState = mouseState.WithButton(which);
                }
                InputSystem.QueueStateEvent(_mouse, mouseState);
            }
            public void Dispose()
            {
                InputSystem.RemoveDevice(_keyboard); InputSystem.RemoveDevice(_mouse);
                if (_keyboardEnabled) InputSystem.EnableDevice(_oldKeyboard);
                if (_mouseEnabled) InputSystem.EnableDevice(_oldMouse);
            }
        }

        private AbilityHolder BuildFluidMovementRig()
        {
            Assert.IsTrue(TryBakeFullFloor(), "A real NavMesh is required for movement validation.");
            BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            holder.keys = new[] { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };
            var gauntlet = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            Assert.IsTrue(gauntlet); Assert.IsNotNull(gauntlet.abilities[0].CombatProfile);
            _player.EquipWeapon(gauntlet); holder.RefreshLoadout();
            SetPrivate(_player, "baseMaxMana", 500f);
            SetPrivate(_player, "_manaRegenerationPercentPerSecond", 0f);
            _player.RefreshResourceStats(); _player.mana = 500f;
            _player.SetMaxHealth(100f); _player.RestoreHealthToMax();
            return holder;
        }

        private IEnumerator WaitForMovementRecovery(AbilityHolder holder, float minimumGlobalProgress = 0f)
        {
            var combat = _player.GetComponent<BreakerGauntletCombat>();
            Assert.IsTrue(combat);
            float deadline = Time.realtimeSinceStartup + 3f;
            while (holder.IsCasting && Time.realtimeSinceStartup < deadline)
            {
                if (holder.CanQueueRecoveryMovement && combat.TryGetCancelContext(out _, out float progress)
                    && progress >= minimumGlobalProgress) yield break;
                yield return null;
            }
            Assert.Fail("The authored action must expose its real recovery window while it is still executing.");
        }

        private IEnumerator WaitForMovementCastToFinish(AbilityHolder holder)
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while (holder.IsCasting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(holder.IsCasting, "The real cast must finish within the test guard.");
        }

        // Test-only diagnostics: no assertions, expiry values or combat timing are changed.
        private sealed class RecoveryMovementTrace : System.IDisposable
        {
            private readonly CharControlScript _control;
            private readonly AbilityHolder _holder;
            private readonly UnityEngine.AI.NavMeshAgent _navigation;
            private readonly System.IO.StreamWriter _writer;
            private int _frame = -1;
            private static object Field(object owner, string name) => owner.GetType().GetField(name,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(owner);
            internal RecoveryMovementTrace(CharControlScript control, AbilityHolder holder, UnityEngine.AI.NavMeshAgent navigation)
            {
                _control = control; _holder = holder; _navigation = navigation;
                string label = UnityEditor.SessionState.GetString("TechGuy.C01.2c.RecoveryRun", "normal");
                string dir = System.IO.Path.Combine(Application.dataPath, "../Docs/validation/artifacts/gameplay-rework/" + UnityEditor.SessionState.GetString("TechGuy.Tests.GprPhase", "c01-2c"));
                System.IO.Directory.CreateDirectory(dir);
                _writer = new System.IO.StreamWriter(System.IO.Path.Combine(dir, "recovery-" + label + ".csv"));
                _writer.WriteLine("frame,dt,unscaledDt,clock,expiresAt,pending,casting,recovery,destinationX,focused");
                UnityEditor.EditorApplication.update += Sample;
            }
            private void Sample()
            {
                if (!_control || _frame == Time.frameCount) return;
                _frame = Time.frameCount;
                var culture = System.Globalization.CultureInfo.InvariantCulture;
                _writer.WriteLine(string.Join(",", new[] { _frame.ToString(), Time.deltaTime.ToString("R", culture),
                    Time.unscaledDeltaTime.ToString("R", culture), System.Convert.ToString(Field(_control, "_movementInputClock"), culture),
                    System.Convert.ToString(Field(Field(_control, "_groundMoveBuffer"), "_expiresAt"), culture),
                    _control.HasPendingGroundMovement.ToString(), _holder.IsCasting.ToString(), _holder.CanQueueRecoveryMovement.ToString(),
                    _navigation.destination.x.ToString("R", culture), UnityEditorInternal.InternalEditorUtility.isApplicationActive.ToString() }));
                _writer.Flush();
            }
            internal void HitchIfRequested()
            {
                Sample();
                int milliseconds = UnityEditor.SessionState.GetInt("TechGuy.C01.2c.RecoveryHitchMs", 0);
                if (milliseconds > 0) System.Threading.Thread.Sleep(milliseconds);
            }
            public void Dispose() { Sample(); UnityEditor.EditorApplication.update -= Sample; _writer.Dispose(); }
        }
    }
}
