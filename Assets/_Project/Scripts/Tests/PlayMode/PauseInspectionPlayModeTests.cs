using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private RunBoons BuildPauseInspectionRun()
        {
            Assert.IsTrue(TryBakeFullFloor());
            BuildPlayer(Vector3.zero);
            var holder = _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]));
            holder.RefreshLoadout();
            return _player.gameObject.AddComponent<RunBoons>();
        }

        [UnityTest]
        public IEnumerator PauseUpgradesRemainHiddenDuringPlayAndAppearOnlyOnTheInspectionPage()
        {
            var run = BuildPauseInspectionRun();
            yield return null;
            run.OfferReward(1);
            Assert.IsTrue(run.Choose(0));
            var rewards = _player.GetComponent<RunRewardUI>();
            var pause = _player.GetComponent<PauseMenuUI>();
            Assert.AreEqual(1, run.Acquired.Count);
            Assert.IsFalse(rewards.IsBuildVisible);
            Assert.IsFalse(pause.TryShowUpgrades(), "A page cannot open outside player-owned pause.");
            pause.Pause();
            Assert.IsFalse(rewards.IsBuildVisible, "The pause home does not automatically fill the screen with upgrades.");
            Assert.IsTrue(pause.TryShowUpgrades());
            Assert.IsTrue(rewards.IsBuildVisible);
            Assert.IsTrue(pause.BlocksInput);
            Assert.AreEqual(0f, Time.timeScale);
            pause.ReturnToPauseHome();
            Assert.IsTrue(pause.IsPaused);
            Assert.IsFalse(rewards.IsBuildVisible);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(pause.TryShowUpgrades());
            Assert.AreEqual(1, run.Acquired.Count, "Inspecting a build cannot acquire or remove upgrades.");
            pause.Resume();
            Assert.IsFalse(rewards.IsBuildVisible);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PauseInspectionKeepsRewardPendingAndBlocksAcquisitionUntilTheResumeFrameHasPassed()
        {
            var run = BuildPauseInspectionRun();
            yield return null;
            run.OfferReward(1);
            var rewards = _player.GetComponent<RunRewardUI>();
            var pause = _player.GetComponent<PauseMenuUI>();
            Assert.IsTrue(rewards.IsRewardSelectionVisible);
            int choiceCount = run.Choices.Count;
            pause.Pause();
            Assert.IsTrue(pause.TryShowUpgrades());
            Assert.IsFalse(rewards.IsRewardSelectionVisible);
            Assert.IsFalse(run.Choose(0));
            Assert.IsTrue(run.IsChoosing);
            Assert.AreEqual(choiceCount, run.Choices.Count);
            Assert.IsEmpty(run.Acquired);
            pause.Resume();
            Assert.IsTrue(pause.BlocksInput, "The click that resumes the game must not choose a reward.");
            Assert.IsFalse(rewards.IsRewardSelectionVisible);
            Assert.IsFalse(run.Choose(0));
            yield return null;
            Assert.IsTrue(rewards.IsRewardSelectionVisible);
            Assert.IsTrue(run.Choose(0));
            Assert.AreEqual(1, run.Acquired.Count);
        }

        [UnityTest]
        public IEnumerator PauseInspectionRestoresTheCapturedClockAndAudioRatherThanForcingDefaults()
        {
            BuildPauseInspectionRun();
            yield return null;
            var pause = _player.GetComponent<PauseMenuUI>();
            float originalClock = Time.timeScale;
            bool originalAudio = AudioListener.pause;
            try
            {
                foreach (float clock in new[] { .4f, 0f })
                {
                    foreach (bool audio in new[] { false, true })
                    {
                        Time.timeScale = clock;
                        AudioListener.pause = audio;
                        pause.Pause();
                        Assert.IsTrue(pause.TryShowUpgrades());
                        Assert.AreEqual(0f, Time.timeScale);
                        Assert.IsTrue(AudioListener.pause);
                        pause.ReturnToPauseHome();
                        Assert.IsTrue(pause.IsPaused);
                        pause.Resume();
                        Assert.AreEqual(clock, Time.timeScale);
                        Assert.AreEqual(audio, AudioListener.pause);
                    }
                }
            }
            finally
            {
                pause.Resume();
                Time.timeScale = originalClock;
                AudioListener.pause = originalAudio;
            }
        }

        [UnityTest]
        public IEnumerator DisablingPauseDuringBuildInspectionRestoresClockAudioAndReleasesItsPage()
        {
            BuildPauseInspectionRun();
            yield return null;
            var pause = _player.GetComponent<PauseMenuUI>();
            var rewards = _player.GetComponent<RunRewardUI>();
            float originalClock = Time.timeScale;
            bool originalAudio = AudioListener.pause;
            try
            {
                Time.timeScale = .65f;
                AudioListener.pause = false;
                pause.Pause();
                Assert.IsTrue(pause.TryShowUpgrades());
                Assert.IsTrue(rewards.IsBuildVisible);
                pause.enabled = false;
                Assert.IsFalse(pause.IsPaused);
                Assert.IsFalse(pause.IsUpgradesVisible);
                Assert.IsFalse(rewards.IsBuildVisible);
                Assert.AreEqual(.65f, Time.timeScale);
                Assert.IsFalse(AudioListener.pause);
                Assert.IsTrue(pause.BlocksInput, "Release continues to protect the same frame.");
                yield return null;
                Assert.IsFalse(pause.BlocksInput);
            }
            finally
            {
                pause.Resume();
                Time.timeScale = originalClock;
                AudioListener.pause = originalAudio;
            }
        }

        [UnityTest]
        public IEnumerator PauseConfirmedAbortWaitsForResumeFrameAndReturnsFromUnavailableMutationWithoutVictory()
        {
            int originalLoadout = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
            float originalClock = Time.timeScale;
            bool originalAudio = AudioListener.pause;
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            Assert.IsTrue(TryBakeFullFloor());
            BuildPlayer(Vector3.zero);
            _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture = CreateManualWorldFixture();
            PauseMenuUI pause = null;
            try
            {
                if (_navMeshInstance.valid) _navMeshInstance.Remove();
                fixture.director.gameObject.SetActive(true);
                yield return null;
                yield return null;
                yield return null;
                var route = (ManualRoutePolicy)typeof(ProgressionDirector)
                    .GetField("_manualPolicy", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(fixture.director);
                foreach (int node in new[] { 1, 2, 4 })
                {
                    ReachManualReward(route, node);
                    Assert.IsTrue(route.TryCompleteReward(route.Session.Id, node, 0, false, RewardReady));
                }
                ReachManualReward(route, 5);
                var run = _player.GetComponent<RunBoons>();
                foreach (GauntletMutation mutation in System.Enum.GetValues(typeof(GauntletMutation)))
                    Assert.IsTrue(run.WeaponModifiers.GauntletMutations.Select(mutation));
                LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Manual Mutation guarantee unavailable for session .*"));
                Assert.IsTrue(fixture.director.TryOpenManualReward(route.Session.Id, 5, 0));
                Assert.IsTrue(run.ManualRecoveryRequired);
                Assert.IsEmpty(run.Choices);
                Assert.AreEqual(0, route.Session.MutationAcquisitions);
                Assert.IsFalse(fixture.director.TryCommitDestination(route.Session.Id, 10), "A recovery exit must not fabricate a Mutation or unlock the Boss.");

                pause = _player.GetComponent<PauseMenuUI>();
                var mapRoot = new GameObject("Pause abort map fixture");
                mapRoot.SetActive(false);
                _spawned.Add(mapRoot);
                var map = mapRoot.AddComponent<ManualRunUI>();
                SetPrivate(map, "_director", fixture.director);
                SetPrivate(map, "_player", _player);
                SetPrivate(map, "_pause", pause);
                pause.BindMap(map);
                Assert.IsTrue(map.CanAbortRun);
                Assert.IsFalse(pause.TryRequestRunAbort(), "The confirmed pause action cannot be requested during gameplay.");
                pause.Pause();
                Assert.IsTrue(pause.TryShowMap());
                Assert.IsFalse(map.TryAbortRun(), "Read-only pause inspection cannot end the run through the director.");
                Assert.AreEqual(RunPhase.RewardPending, route.Session.Phase);
                Assert.AreEqual(RunOutcome.None, route.Session.Outcome);
                Assert.IsTrue(run.ManualRecoveryRequired);
                Assert.AreEqual(0f, Time.timeScale);

                SetManualField(fixture.director, "_returnScene", "MissingPauseAbortTestScene");
                LogAssert.Expect(LogType.Error, "ProgressionDirector cannot return to the Nexus: scene 'MissingPauseAbortTestScene' is missing from Build Settings. Aborting the transition without loading another scene (R8.4).");
                SetManualField(fixture.director, "_manualCleanupPending", true);
                Assert.IsTrue(pause.TryRequestRunAbort());
                Assert.IsFalse(pause.IsPaused);
                Assert.IsTrue(pause.BlocksInput);
                Assert.AreEqual(RunPhase.RewardPending, route.Session.Phase, "Confirmation must wait until the resume frame ends.");
                Assert.AreEqual(RunOutcome.None, route.Session.Outcome);
                Assert.AreEqual(originalClock, Time.timeScale);
                Assert.AreEqual(originalAudio, AudioListener.pause);
                Assert.IsFalse(pause.TryRequestRunAbort(), "The same confirmed exit cannot be queued twice.");
                yield return null;
                Assert.AreEqual(RunPhase.RewardPending, route.Session.Phase, "Transient cleanup must finish before the confirmed exit is applied.");
                Assert.AreEqual(RunOutcome.None, route.Session.Outcome);
                SetManualField(fixture.director, "_manualCleanupPending", false);
                yield return null;
                Assert.AreEqual(RunOutcome.Aborted, route.Session.Outcome);
                Assert.AreEqual(RunPhase.Returning, route.Session.Phase);
                Assert.AreEqual(ManualReturnState.Failed, fixture.director.ManualReturnStatus);
                Assert.AreEqual(0, route.Session.MutationAcquisitions);
                Assert.IsFalse(map.CanAbortRun);
                Assert.IsFalse(map.TryAbortRun());
                yield return null;
                Assert.AreEqual(RunOutcome.Aborted, route.Session.Outcome, "Later frames retain the single claimed result.");
                Assert.AreEqual(0, fixture.director.ManualLoadAttempts, "The missing test scene avoids loading or replacing the current scene.");
            }
            finally
            {
                if (pause) pause.Resume();
                Time.timeScale = originalClock;
                AudioListener.pause = originalAudio;
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, originalLoadout);
                UnityEngine.Object.DestroyImmediate(fixture.definition);
            }
        }

        [UnityTest]
        public IEnumerator GamepadBackReturnsFromPauseInspectionAndStartResumesWithoutReplayingHeldWorldInput()
        {
            var run = BuildPauseInspectionRun();
            yield return null;
            var pause = _player.GetComponent<PauseMenuUI>();
            var rewards = _player.GetComponent<RunRewardUI>();
            // A valid map projection isolates pause navigation from encounter generation.
            var mapRoot = new GameObject("Pause gamepad map fixture");
            mapRoot.SetActive(false);
            _spawned.Add(mapRoot);
            var director = mapRoot.AddComponent<ProgressionDirector>();
            var route = BuildManualRewardRoute();
            SetPrivate(director, "_manualPolicy", route);
            SetPrivate(director, "_player", _player);
            var map = mapRoot.AddComponent<ManualRunUI>();
            SetPrivate(map, "_director", director);
            SetPrivate(map, "_player", _player);
            SetPrivate(map, "_pause", pause);
            pause.BindMap(map);
            var originalPad = Gamepad.current;
            var originalMouse = Mouse.current;
            bool padEnabled = originalPad != null && originalPad.enabled;
            bool mouseEnabled = originalMouse != null && originalMouse.enabled;
            float originalClock = Time.timeScale;
            bool originalAudio = AudioListener.pause;
            if (padEnabled) InputSystem.DisableDevice(originalPad);
            if (mouseEnabled) InputSystem.DisableDevice(originalMouse);
            var pad = InputSystem.AddDevice<Gamepad>();
            var mouse = InputSystem.AddDevice<Mouse>();
            var attackRelease = typeof(CharControlScript).GetField("_waitForAttackRelease", BindingFlags.Instance | BindingFlags.NonPublic);
            try
            {
                pause.Pause();
                Assert.IsTrue(pause.TryShowMap());
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
                yield return null;
                Assert.IsTrue(pause.IsPaused);
                Assert.IsFalse(pause.IsMapVisible, "B returns from the map to the pause home.");
                Assert.AreEqual(0f, Time.timeScale);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null;
                Assert.IsTrue(pause.TryShowUpgrades());
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.East));
                yield return null;
                Assert.IsTrue(pause.IsPaused);
                Assert.IsFalse(rewards.IsBuildVisible, "B also returns from acquired upgrades.");
                Assert.AreEqual(RunPhase.RouteChoice, route.Session.Phase);
                Assert.AreEqual(-1, route.Session.PendingDestination);
                InputSystem.QueueStateEvent(pad, new GamepadState());
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
                yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                Assert.IsFalse(pause.IsPaused, "Start resumes from the pause home.");
                Assert.AreEqual(originalClock, Time.timeScale);
                Assert.AreEqual(originalAudio, AudioListener.pause);
                Assert.IsTrue(pause.BlocksInput, "Resume consumes its own frame before world input resumes.");
                Assert.IsTrue((bool)attackRelease.GetValue(_controls));
                Assert.IsFalse(_controls.TryDirectionalBasicAttack(Vector3.forward * 2f));
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null;
                Assert.IsFalse(pause.BlocksInput);
                Assert.IsTrue((bool)attackRelease.GetValue(_controls), "A primary button held during inspection still requires release.");
                Assert.AreEqual(0, _attackCount);
                Assert.IsFalse(_agent.hasPath, "Inspection clicks cannot leave a world movement order.");
                Assert.IsEmpty(run.Acquired);
                InputSystem.QueueStateEvent(mouse, new MouseState());
                yield return null;
                Assert.IsFalse((bool)attackRelease.GetValue(_controls));
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                Assert.IsTrue(pause.IsPaused, "Start opens pause while playing, without keyboard input.");
                Assert.IsTrue(pause.TryShowMap());
                InputSystem.QueueStateEvent(pad, new GamepadState());
                yield return null;
                InputSystem.QueueStateEvent(pad, new GamepadState().WithButton(GamepadButton.Start));
                yield return null;
                Assert.IsFalse(pause.IsPaused, "Start can resume directly from an inspection page.");
                Assert.IsFalse(pause.IsMapVisible);
                Assert.IsTrue(pause.BlocksInput);
            }
            finally
            {
                pause.Resume();
                InputSystem.RemoveDevice(pad);
                InputSystem.RemoveDevice(mouse);
                if (padEnabled) InputSystem.EnableDevice(originalPad);
                if (mouseEnabled) InputSystem.EnableDevice(originalMouse);
                Time.timeScale = originalClock;
                AudioListener.pause = originalAudio;
            }
        }
    }
}
