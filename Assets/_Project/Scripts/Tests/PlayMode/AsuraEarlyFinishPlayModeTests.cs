using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private IEnumerator EarlyAsuraRig(bool fall = false, bool echo = false, bool reserve = false)
        {
            yield return EclipseRig(fall);
            Assert.IsTrue(_eclipseRun.WeaponModifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura));
            if (echo)
            {
                Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.AsuraEcho), out var definition));
                _eclipseRun.WeaponModifiers.Add(definition);
            }
            if (reserve)
            {
                Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.AsuraReserve), out var definition));
                _eclipseRun.WeaponModifiers.Add(definition);
                _eclipseRun.WeaponModifiers.Add(definition);
            }
            _player.Stats.criticalChance = 0f;
        }

        private IEnumerator EarlyAsuraWait(Func<bool> observed = null)
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while ((!_eclipseCombat.IsCadencedWaiting || observed != null && !observed()) &&
                _eclipseCombat.IsExecuting && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(_eclipseCombat.IsCadencedWaiting, "The real runner must return control between strikes.");
            if (observed != null) Assert.IsTrue(observed(), "The requested native impacts must have happened before the wait.");
        }

        private IEnumerator EarlyAsuraIdle()
        {
            float deadline = Time.realtimeSinceStartup + 4f;
            while ((_eclipseCombat.IsExecuting || _eclipseCombat.PendingChildCount > 0) &&
                Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsFalse(_eclipseCombat.IsExecuting);
            Assert.AreEqual(0, _eclipseCombat.PendingChildCount);
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishAfterFirstStrikeUsesOneCostAndThreeQuarterNativePower()
        {
            yield return EarlyAsuraRig(reserve: true);
            var target = EclipseTarget(Vector3.forward * 1.2f);
            var reaction = target.GetComponent<CombatReactionController>();
            var indices = new List<int>(); var damage = new List<float>(); int uses = 0;
            target.DamageReceived += (_, amount) =>
            {
                var context = _player.CurrentGauntletImpactContext;
                if (context?.Slot == GauntletActionSlot.R && context.Origin == GauntletImpactOrigin.Direct)
                { indices.Add(context.ImpactIndex); damage.Add(amount); }
            };
            _eclipseHolder.AbilityUsed += slot => { if (slot == 3) uses++; };
            var runtime = _player.GetComponent<ProtocolRuntime>();
            long roots = runtime.Budget.RootsCreated;
            float mana = _player.mana, cost = _eclipseHolder.ActiveAbilities[3].ManaCost;
            float initialStance = reaction.CurrentStance;
            _eclipseCombat.AddAsuraEnergy(100);
            Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
            yield return EarlyAsuraWait();
            CollectionAssert.AreEqual(new[] { 0 }, indices);
            float firstPosture = initialStance - reaction.CurrentStance;
            Assert.Greater(firstPosture, 0f);
            float healthBeforeFinal = target.health, postureBeforeFinal = reaction.CurrentStance;
            long execution = _eclipseCombat.CadencedExecution;
            Assert.IsTrue(_eclipseHolder.CanFinishCadencedAsura);
            Assert.IsTrue(_eclipseHolder.TryFinishCadencedAsura());
            Assert.AreEqual(execution, _eclipseCombat.CadencedExecution, "Finishing continues the accepted R root.");
            Assert.IsFalse(_eclipseCombat.IsCadencedWaiting);
            Assert.IsFalse(_eclipseHolder.CanFinishCadencedAsura);
            Assert.AreEqual(healthBeforeFinal, target.health, "F starts a committed strike; it cannot apply an instant hit.");
            Assert.AreEqual(postureBeforeFinal, reaction.CurrentStance);
            Assert.AreEqual(0f, _eclipseCombat.Elapsed);
            Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura(), "A duplicate press cannot fabricate another finalizer.");
            Assert.IsFalse(_eclipseCombat.TryFinishCadenced(execution));
            Assert.IsFalse(_eclipseHolder.TryUseAbility(3), "The finalizer still owns startup.");
            yield return EarlyAsuraIdle();
            CollectionAssert.AreEqual(new[] { 0, 2 }, indices);
            Assert.AreEqual(damage[0] * 2f, damage[1], .01f);
            Assert.AreEqual(damage[0] * 3f, damage.Sum(), .01f, "The skipped 25 percent strike is lost, leaving 75 percent.");
            Assert.AreEqual(firstPosture * 3f, initialStance - reaction.CurrentStance, .1f);
            Assert.AreEqual(mana - cost, _player.mana, .001f);
            Assert.AreEqual(1, uses);
            Assert.AreEqual(40, _eclipseCombat.Energy, "The original reserve is applied once and R cannot generate energy.");
            Assert.AreEqual(roots + 1, runtime.Budget.RootsCreated);
            Assert.AreEqual(0, _eclipseCombat.EnergyPolicy.RetainedRootCount);
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishAfterSecondStrikePreservesTheFullThreeStrikePower()
        {
            yield return EarlyAsuraRig();
            var target = EclipseTarget(Vector3.forward * 1.2f);
            var reaction = target.GetComponent<CombatReactionController>();
            var indices = new List<int>(); var damage = new List<float>();
            target.DamageReceived += (_, amount) =>
            {
                var context = _player.CurrentGauntletImpactContext;
                if (context?.Slot == GauntletActionSlot.R && context.Origin == GauntletImpactOrigin.Direct)
                { indices.Add(context.ImpactIndex); damage.Add(amount); }
            };
            float mana = _player.mana, cost = _eclipseHolder.ActiveAbilities[3].ManaCost;
            float stance = reaction.CurrentStance;
            _eclipseCombat.AddAsuraEnergy(100); Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
            yield return EarlyAsuraWait(); float firstPosture = stance - reaction.CurrentStance;
            Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
            Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura(), "F must not skip a committed second strike.");
            yield return EarlyAsuraWait(() => indices.Count == 2);
            float hp = target.health;
            Assert.IsTrue(_eclipseHolder.TryFinishCadencedAsura()); Assert.AreEqual(hp, target.health);
            yield return EarlyAsuraIdle();
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indices);
            Assert.AreEqual(damage[0], damage[1], .01f);
            Assert.AreEqual(damage[0] * 2f, damage[2], .01f);
            Assert.AreEqual(damage[0] * 4f, damage.Sum(), .01f);
            Assert.AreEqual(firstPosture * 4f, stance - reaction.CurrentStance, .1f);
            Assert.AreEqual(mana - cost, _player.mana, .001f);
            Assert.AreEqual(0, _eclipseCombat.Energy);
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishRejectedDuringCommittedPhasesPreservesTheBufferedNormalContinuation()
        {
            float capture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60f;
            try
            {
                yield return EarlyAsuraRig();
                var target = EclipseTarget(Vector3.forward * 1.2f); var indices = new List<int>();
                target.DamageReceived += (_, __) =>
                {
                    var context = _player.CurrentGauntletImpactContext;
                    if (context?.Slot == GauntletActionSlot.R && context.Origin == GauntletImpactOrigin.Direct) indices.Add(context.ImpactIndex);
                };
                _eclipseCombat.AddAsuraEnergy(100); Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
                Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura()); Assert.IsFalse(_eclipseHolder.HasBufferedOffensiveIntent);
                float deadline = Time.realtimeSinceStartup + 4f;
                while (_eclipseCombat.Elapsed < _eclipseCombat.ExecutionSnapshot.CadencedImpactTime &&
                    Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura(), "The active contact is committed.");
                yield return WaitForMovementRecovery(_eclipseHolder, .85f);
                Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura(), "Recovery is not the free wait state.");
                Assert.IsTrue(_eclipseHolder.TryUseAbility(3)); Assert.IsTrue(_eclipseHolder.HasBufferedOffensiveIntent);
                Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura());
                Assert.IsTrue(_eclipseHolder.HasBufferedOffensiveIntent, "Rejected F must leave the accepted R intent intact.");
                yield return EarlyAsuraWait(() => indices.Count == 2);
                CollectionAssert.AreEqual(new[] { 0, 1 }, indices);
                Assert.IsFalse(_eclipseHolder.HasBufferedOffensiveIntent);
                Assert.IsTrue(_eclipseHolder.TryFinishCadencedAsura());
                Assert.IsFalse(_eclipseHolder.HasBufferedOffensiveIntent);
                yield return EarlyAsuraIdle(); CollectionAssert.AreEqual(new[] { 0, 1, 2 }, indices);
            }
            finally { Time.captureDeltaTime = capture; }
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishUsesTheInclusiveWaitDeadlineAndRejectsStaleExecutions()
        {
            yield return EarlyAsuraRig();
            var target = EclipseTarget(Vector3.forward * 1.2f); var indices = new List<int>();
            target.DamageReceived += (_, __) =>
            {
                var context = _player.CurrentGauntletImpactContext;
                if (context?.Slot == GauntletActionSlot.R && context.Origin == GauntletImpactOrigin.Direct) indices.Add(context.ImpactIndex);
            };
            long previous = 0;
            for (int boundary = 0; boundary < 3; boundary++)
            {
                _eclipseHolder.RefreshLoadout(); _player.RefreshResourceStats(true); _eclipseCombat.AddAsuraEnergy(100);
                int before = indices.Count; Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
                yield return EarlyAsuraWait();
                long current = _eclipseCombat.CadencedExecution;
                if (previous != 0)
                {
                    Assert.AreNotEqual(previous, current);
                    Assert.IsFalse(_eclipseCombat.TryFinishCadenced(previous));
                    Assert.AreEqual(current, _eclipseCombat.CadencedExecution); Assert.IsTrue(_eclipseCombat.IsCadencedWaiting);
                }
                SetPrivate(_eclipseCombat, "_cadencedWaitUntil", _eclipseCombat.CadencedNow + (boundary == 0 ? .01f : boundary == 1 ? 0f : -.01f));
                float hp = target.health, mana = _player.mana;
                Assert.AreEqual(boundary < 2, _eclipseHolder.TryFinishCadencedAsura());
                Assert.AreEqual(hp, target.health); Assert.AreEqual(mana, _player.mana);
                if (boundary < 2)
                {
                    yield return EarlyAsuraIdle();
                    Assert.AreEqual(before + 2, indices.Count);
                }
                else
                {
                    yield return null; yield return null;
                    Assert.IsFalse(_eclipseCombat.IsExecuting);
                    Assert.AreEqual(before + 1, indices.Count, "Timeout cannot invent the unperformed final producer.");
                }
                Assert.IsFalse(_eclipseCombat.TryFinishCadenced(current));
                Assert.AreEqual(0, _eclipseCombat.PendingChildCount);
                Assert.AreEqual(0, _eclipseCombat.EnergyPolicy.RetainedRootCount);
                previous = current;
            }
            CollectionAssert.AreEqual(new[] { 0, 2, 0, 2, 0 }, indices);
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishWaitAndContactRespectPauseMenusAndRealHitstop()
        {
            yield return EarlyAsuraRig();
            var target = EclipseTarget(Vector3.forward * 1.2f);
            _eclipseCombat.AddAsuraEnergy(100); Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
            yield return EarlyAsuraWait();
            float clock = _eclipseCombat.CadencedNow, hp = target.health;
            try
            {
                Time.timeScale = 0f;
                Assert.IsFalse(_eclipseHolder.CanFinishCadencedAsura); Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura());
                yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(clock, _eclipseCombat.CadencedNow); Assert.AreEqual(hp, target.health);
                Assert.IsTrue(_eclipseCombat.IsCadencedWaiting);
                Time.timeScale = 1f;
                _eclipseHolder.SetManualInitializationBlocked(true);
                Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura()); Assert.IsTrue(_eclipseCombat.IsCadencedWaiting);
                _eclipseHolder.SetManualInitializationBlocked(false);
                using (_player.BeginImpactFeedback(0f)) { }
                // Leave headroom for Editor scheduling while sampling the real-time pause.
                var hitStop = _player.GetComponent<HitStopRunner>(); hitStop.Apply(1f);
                Assert.IsTrue(hitStop.IsActive); clock = _eclipseCombat.CadencedNow;
                Assert.IsFalse(_eclipseHolder.CanFinishCadencedAsura); Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura());
                yield return new WaitForSecondsRealtime(.1f);
                Assert.IsTrue(hitStop.IsActive); Assert.AreEqual(clock, _eclipseCombat.CadencedNow);
                Assert.AreEqual(hp, target.health); Assert.IsTrue(_eclipseCombat.IsCadencedWaiting);
                float end = Time.realtimeSinceStartup + 3f;
                while (hitStop.IsActive && Time.realtimeSinceStartup < end) yield return null;
                Assert.IsFalse(hitStop.IsActive); Assert.IsTrue(_eclipseHolder.TryFinishCadencedAsura());
                Time.timeScale = 0f; yield return new WaitForSecondsRealtime(.15f);
                Assert.AreEqual(hp, target.health, "Accepted finalizer still waits for scaled startup/contact.");
                Time.timeScale = 1f; yield return EarlyAsuraIdle();
                Assert.Less(target.health, hp);
            }
            finally { Time.timeScale = 1f; _eclipseHolder.SetManualInitializationBlocked(false); }
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishInterruptedBeforeContactNeverCreatesFinalizerEchoOrRetainedWork()
        {
            for (int scenario = 0; scenario < 6; scenario++)
            {
                yield return EarlyAsuraRig(echo: true);
                var playerObject = _player.gameObject;
                var target = EclipseTarget(Vector3.forward * 1.2f); int direct = 0, echoes = 0;
                target.DamageReceived += (_, __) =>
                {
                    var context = _player.CurrentGauntletImpactContext;
                    if (context?.Slot != GauntletActionSlot.R) return;
                    if (context.Origin == GauntletImpactOrigin.Direct) direct++;
                    if (context.Origin == GauntletImpactOrigin.Echo) echoes++;
                };
                var otherOwner = new GameObject("Early Asura other mitigation owner"); _spawned.Add(otherOwner);
                _player.SetDamageTakenMultiplier(otherOwner, .5f);
                _eclipseCombat.AddAsuraEnergy(100); Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
                yield return EarlyAsuraWait(); Assert.AreEqual(1, direct);
                long execution = _eclipseCombat.CadencedExecution;
                Assert.IsTrue(_eclipseHolder.TryFinishCadencedAsura());
                if (scenario == 0) _eclipseCombat.FinishForDodge();
                else if (scenario == 1) _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1]));
                else if (scenario == 2) _eclipseCombat.enabled = false;
                else if (scenario == 3) _player.TakeDamage(_player.health * 100f);
                else if (scenario == 4)
                { Assert.IsTrue(_eclipseHolder.TryStopEncounterOffense()); _eclipseRun.CloseProtocolEncounter(); }
                else _eclipseHolder.enabled = false;
                yield return null; yield return null;
                Assert.IsFalse(_eclipseCombat.IsExecuting, "Interrupted scenario " + scenario);
                Assert.IsFalse(_eclipseCombat.TryFinishCadenced(execution));
                Assert.IsFalse(_eclipseCombat.EnergyPolicy.IsAsuraActive);
                Assert.AreEqual(1, direct); Assert.AreEqual(0, echoes);
                Assert.AreEqual(0, _eclipseCombat.PendingChildCount);
                Assert.AreEqual(0, _eclipseCombat.EnergyPolicy.RetainedRootCount);
                if (!_player.IsDead)
                {
                    float hp = _player.health; _player.TakeDamage(10f);
                    Assert.AreEqual(5f, hp - _player.health, .01f, "Only the cancelled execution's mitigation is released.");
                    if (_agent.enabled && _agent.isOnNavMesh) Assert.IsFalse(_agent.isStopped);
                }
                if (playerObject) UnityEngine.Object.DestroyImmediate(playerObject);
                UnityEngine.Object.DestroyImmediate(target.gameObject);
                UnityEngine.Object.DestroyImmediate(_weapon); yield return null;
            }
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishNativeFinalizerConsumesOneEclipseMarkAndProducesOneEchoOnTheSameRoot()
        {
            float capture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60f;
            try
            {
                yield return EarlyAsuraRig(fall: true, echo: true);
                var target = EclipseTarget(Vector3.forward * 1.2f, true);
                yield return EclipseCastQ(); Assert.AreEqual(1, _eclipseCombat.EclipseMarkCount);
                var indices = new List<int>(); int echoes = 0, uses = 0;
                target.DamageReceived += (_, __) =>
                {
                    var context = _player.CurrentGauntletImpactContext;
                    if (context?.Slot != GauntletActionSlot.R) return;
                    if (context.Origin == GauntletImpactOrigin.Direct) indices.Add(context.ImpactIndex);
                    if (context.Origin == GauntletImpactOrigin.Echo) echoes++;
                };
                _eclipseHolder.AbilityUsed += slot => { if (slot == 3) uses++; };
                var runtime = _player.GetComponent<ProtocolRuntime>(); long roots = runtime.Budget.RootsCreated;
                float mana = _player.mana, cost = _eclipseHolder.ActiveAbilities[3].ManaCost;
                _eclipseCombat.AddAsuraEnergy(100); Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
                yield return EarlyAsuraWait();
                Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted); Assert.AreEqual(0, echoes);
                Assert.IsTrue(_eclipseHolder.TryFinishCadencedAsura());
                Assert.AreEqual(0, _eclipseCombat.EclipseFallsEmitted); Assert.AreEqual(0, echoes);
                yield return EarlyAsuraIdle(); yield return EclipseWaitForFalls(1);
                CollectionAssert.AreEqual(new[] { 0, 2 }, indices);
                Assert.AreEqual(1, echoes); Assert.AreEqual(1, uses);
                Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount); Assert.AreEqual(0, _eclipseCombat.Energy);
                Assert.AreEqual(mana - cost, _player.mana, .001f);
                Assert.AreEqual(roots + 1, runtime.Budget.RootsCreated, "Finish, echo and fall must share the accepted R root.");
                Assert.AreEqual(0, _eclipseCombat.PendingChildCount);
                Assert.AreEqual(0, _eclipseCombat.EnergyPolicy.RetainedRootCount);
            }
            finally { Time.captureDeltaTime = capture; }
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishRejectsAReentrantImpactCallbackWithoutConsumingTheLiveWait()
        {
            yield return EarlyAsuraRig();
            var target = EclipseTarget(Vector3.forward * 1.2f);
            _eclipseCombat.AddAsuraEnergy(100); Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
            yield return EarlyAsuraWait();
            long execution = _eclipseCombat.CadencedExecution;
            bool callbackObserved = false; var rIndices = new List<int>();
            target.DamageReceived += (_, __) =>
            {
                var context = _player.CurrentGauntletImpactContext;
                if (context?.Slot == GauntletActionSlot.R && context.Origin == GauntletImpactOrigin.Direct)
                    rIndices.Add(context.ImpactIndex);
                if (context?.Slot != GauntletActionSlot.Basic) return;
                callbackObserved = true;
                Assert.IsTrue(_eclipseCombat.IsCadencedWaiting, "This callback must exercise the guard with a live wait.");
                Assert.IsTrue(_player.IsResolvingGauntletImpact);
                Assert.IsFalse(_eclipseHolder.CanFinishCadencedAsura);
                Assert.IsFalse(_eclipseHolder.TryFinishCadencedAsura());
                Assert.IsFalse(_eclipseCombat.TryFinishCadenced(execution));
            };
            // Call the native resolver directly to keep R waiting while a different mechanical batch publishes its callback.
            // The normal basic input path intentionally ends R first and would not exercise this reentrancy boundary.
            EclipseBasic(false);
            Assert.IsTrue(callbackObserved);
            Assert.IsTrue(_eclipseCombat.IsCadencedWaiting); Assert.AreEqual(execution, _eclipseCombat.CadencedExecution);
            Assert.IsFalse(_player.IsResolvingGauntletImpact);
            Assert.IsFalse(_eclipseHolder.HasBufferedOffensiveIntent, "Rejected reentrant F must not become a deferred command.");
            // The confirmed basic also starts hitstop. Finalization remains blocked until that clock resumes.
            float hitstopEnd = Time.realtimeSinceStartup + 2f;
            while (_player.IsCombatHitStopped && Time.realtimeSinceStartup < hitstopEnd) yield return null;
            Assert.IsFalse(_player.IsCombatHitStopped);
            Assert.IsTrue(_eclipseCombat.IsCadencedWaiting);
            Assert.AreEqual(execution, _eclipseCombat.CadencedExecution);
            Assert.IsTrue(_eclipseHolder.TryFinishCadencedAsura());
            yield return EarlyAsuraIdle();
            CollectionAssert.AreEqual(new[] { 2 }, rIndices);
        }

        [UnityTest]
        public IEnumerator AsuraEarlyFinishRealReboundInputWinsOverSimultaneousRAndHoldingDoesNotRepeat()
        {
            const string preference = "TechGuy.Settings.Binding.FinishSequence";
            bool hadPreference = PlayerPrefs.HasKey(preference);
            int original = PlayerPrefs.GetInt(preference);
            float capture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60f;
            var settings = InputSystem.settings;
            var oldBackground = settings.backgroundBehavior;
            var oldEditorInput = settings.editorInputBehaviorInPlayMode;
            try
            {
                // Exercise actual rebindable input even when MCP runs without Game View focus.
                settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                Assert.AreEqual(7, (int)GameControl.FinishSequence, "Existing binding identities must stay stable.");
                Assert.AreEqual(KeyCode.F, GamePreferences.DefaultBindings()[7]);
                var existing = Enumerable.Range(0, 7).Select(i => GamePreferences.Binding((GameControl)i)).ToArray();
                KeyCode rebound = new[] { KeyCode.F12, KeyCode.F11, KeyCode.F10, KeyCode.F9, KeyCode.F8, KeyCode.F7, KeyCode.F6, KeyCode.F5 }
                    .First(key => !existing.Contains(key));
                PlayerPrefs.SetInt(preference, (int)rebound);
                Assert.AreEqual(rebound, GamePreferences.Binding(GameControl.FinishSequence));
                yield return EarlyAsuraRig();
                var camera = BuildFluidInputCamera();
                var target = EclipseTarget(Vector3.forward * 1.2f); var indices = new List<int>(); int uses = 0;
                target.DamageReceived += (_, __) =>
                {
                    var context = _player.CurrentGauntletImpactContext;
                    if (context?.Slot == GauntletActionSlot.R && context.Origin == GauntletImpactOrigin.Direct) indices.Add(context.ImpactIndex);
                };
                _eclipseHolder.AbilityUsed += slot => { if (slot == 3) uses++; };
                using (var input = new FluidityInputDevices())
                {
                    yield return new WaitForEndOfFrame();
                    Vector3 pointer = camera.WorldToScreenPoint(Vector3.forward * 5f);
                    _eclipseCombat.AddAsuraEnergy(100);
                    input.Queue(GameControl.Skill4, true, pointer); yield return null;
                    Assert.IsTrue(_eclipseCombat.IsCadencedAsura);
                    input.Queue(GameControl.Skill4, false, pointer); yield return null;
                    yield return EarlyAsuraWait();
                    var buttonMethod = typeof(GamePreferences).GetMethod("Button", BindingFlags.Static | BindingFlags.NonPublic);
                    var rButton = (ButtonControl)buttonMethod.Invoke(null, new object[] { GamePreferences.Binding(GameControl.Skill4) });
                    var fButton = (KeyControl)buttonMethod.Invoke(null, new object[] { rebound });
                    var keyboard = new List<Key> { fButton.keyCode };
                    var mouse = new MouseState { position = pointer };
                    if (rButton is KeyControl rKey) keyboard.Add(rKey.keyCode);
                    else
                    {
                        MouseButton which = rButton == Mouse.current.leftButton ? MouseButton.Left : rButton == Mouse.current.rightButton ? MouseButton.Right :
                            rButton == Mouse.current.middleButton ? MouseButton.Middle : rButton == Mouse.current.backButton ? MouseButton.Back : MouseButton.Forward;
                        mouse = mouse.WithButton(which);
                    }
                    InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keyboard.ToArray()));
                    InputSystem.QueueStateEvent(Mouse.current, mouse);
                    yield return null;
                    Assert.IsFalse(_eclipseCombat.IsCadencedWaiting);
                    CollectionAssert.AreEqual(new[] { 0 }, indices, "Real input starts startup instead of applying immediate contact.");
                    // Keep both controls held through the finalizer: neither held key is a fresh command.
                    yield return EarlyAsuraIdle();
                    CollectionAssert.AreEqual(new[] { 0, 2 }, indices, "F has first-accepted priority over the simultaneous normal continuation.");
                    Assert.AreEqual(1, uses);
                    input.Queue(GameControl.FinishSequence, false, pointer); yield return null;
                    Assert.AreEqual(1, uses); Assert.IsFalse(_eclipseCombat.IsExecuting);
                }
            }
            finally
            {
                Time.captureDeltaTime = capture;
                if (hadPreference) PlayerPrefs.SetInt(preference, original); else PlayerPrefs.DeleteKey(preference);
                PlayerPrefs.Save();
                settings.backgroundBehavior = oldBackground;
                settings.editorInputBehaviorInPlayMode = oldEditorInput;
            }
        }
    }
}
