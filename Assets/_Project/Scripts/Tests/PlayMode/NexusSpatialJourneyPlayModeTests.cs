using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    // Scripted task walkthrough, not a study with a new player. All movement uses pointer input.
    public sealed class NexusSpatialJourneyPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private Keyboard _keyboard, _oldKeyboard;
        private Mouse _mouse, _oldMouse;
        private bool _keyboardEnabled, _mouseEnabled, _audio;
        private float _scale;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInput;
        private readonly Dictionary<string, int?> _prefs = new Dictionary<string, int?>();
        private PlayerActor _player;
        private EditorWindow _gameView;
        private int _oldSize, _customSize;
        private string _folder;
        private float _distance;
        private readonly List<string> _steps = new List<string>();

        [UnitySetUp] public IEnumerator Setup()
        {
            _scale = Time.timeScale; _audio = AudioListener.pause;
            Time.timeScale = 1; AudioListener.pause = false;
            foreach (string key in new[] { WeaponLoadout.PreferenceKey, CurrencyWallet.PreferenceKey,
                "TechGuy.Loadout.Unlocked.1", "TechGuy.Loadout.Unlocked.2" })
                _prefs[key] = PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null;
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            PlayerPrefs.SetInt("TechGuy.Loadout.Unlocked.1", 1);
            PlayerPrefs.SetInt("TechGuy.Loadout.Unlocked.2", 1);
            _background = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _oldKeyboard = Keyboard.current; _oldMouse = Mouse.current;
            _keyboardEnabled = _oldKeyboard != null && _oldKeyboard.enabled;
            _mouseEnabled = _oldMouse != null && _oldMouse.enabled;
            if (_keyboardEnabled) InputSystem.DisableDevice(_oldKeyboard);
            if (_mouseEnabled) InputSystem.DisableDevice(_oldMouse);
            _keyboard = InputSystem.AddDevice<Keyboard>(); _mouse = InputSystem.AddDevice<Mouse>();
            var type = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            _gameView = Resources.FindObjectsOfTypeAll(type).Cast<EditorWindow>().First();
            _oldSize = (int)type.GetProperty("selectedSizeIndex", Flags).GetValue(_gameView);
            _customSize = -1; _distance = 0; _steps.Clear();
            yield return null;
        }

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static object SizeGroup()
        {
            var t = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            return t.GetMethod("GetGroup").Invoke(t.BaseType.GetProperty("instance").GetValue(null), new object[] { 0 });
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            var pause = _player ? _player.GetComponent<PauseMenuUI>() : null;
            if (pause) pause.Resume();
            InputSystem.RemoveDevice(_keyboard); InputSystem.RemoveDevice(_mouse);
            if (_keyboardEnabled && _oldKeyboard != null) InputSystem.EnableDevice(_oldKeyboard);
            if (_mouseEnabled && _oldMouse != null) InputSystem.EnableDevice(_oldMouse);
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            _gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(_gameView, _oldSize);
            if (_customSize >= 0) { var g = SizeGroup(); g.GetType().GetMethod("RemoveCustomSize").Invoke(g, new object[] { _customSize }); }
            foreach (var pair in _prefs)
                if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save(); Time.timeScale = _scale; AudioListener.pause = _audio;
            yield return null;
        }
        [UnityTest] public IEnumerator Journey720() { yield return Journey(1280, 720); }
        [UnityTest] public IEnumerator Journey1080() { yield return Journey(1920, 1080); }

        private IEnumerator Walk(float x, float z)
        {
            Vector3 target = new Vector3(x, .06f, z);
            var screen = Camera.main.WorldToScreenPoint(target);
            Assert.Greater(screen.z, 0); Assert.That(screen.x, Is.InRange(0, Screen.width)); Assert.That(screen.y, Is.InRange(0, Screen.height));
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screen, buttons = 2 });
            yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screen });
            float deadline = Time.realtimeSinceStartup + 10;
            Vector3 previous = _player.transform.position;
            while (Vector3.Distance(_player.transform.position, target) > .55f)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Pointer route blocked: " + target);
                _distance += Vector3.Distance(previous, _player.transform.position); previous = _player.transform.position;
                yield return null;
            }
            _steps.Add("pointer walk " + target);
            yield return new WaitForSecondsRealtime(.25f);
        }
        private IEnumerator KeyPress(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null; yield return null;
        }
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(_folder + "/" + name + ".png");
            yield return new WaitForEndOfFrame(); yield return null;
            if (_folder.Contains("after") && (name == "arsenal" || name == "resumed"))
            {
                var arsenal = Object.FindFirstObjectByType<LobbyArsenal>();
                Rect card = arsenal.VisibleCardRect;
                Assert.Greater(card.width, 0, "The proximity card must remain visible.");
                Vector3 screen = Camera.main.WorldToScreenPoint(_player.transform.position);
                Vector2 avatar = new Vector2(screen.x, Screen.height - screen.y) / WeaponCardView.Scale;
                Assert.IsFalse(card.Overlaps(new Rect(avatar.x - 68, avatar.y - 135, 136, 190)), "Card must clear the avatar.");
                Assert.IsFalse(card.Overlaps(new Rect(24, 24, 370, 116)), "Card must clear the guide.");
                var markers = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(t => t.name == "Equipped indicator").ToArray();
                Assert.AreEqual(3, markers.Length); Assert.AreEqual(1, markers.Count(t => t.gameObject.activeSelf));
                int expected = WeaponRunModifiers.Identify(_player.CurrentWeapon) == RunWeaponFamily.Bow ? 1 : 2;
                Assert.AreEqual("Pedestal " + expected, markers.Single(t => t.gameObject.activeSelf).parent.name);
            }
            File.WriteAllText(_folder + "/" + name + ".json", JsonUtility.ToJson(new CaptureState {
                player = _player.transform.position, camera = Camera.main.transform.position,
                rotation = Camera.main.transform.eulerAngles, weapon = _player.CurrentWeapon.name, width = Screen.width, height = Screen.height }));
        }
        [Serializable] private sealed class CaptureState { public Vector3 player, camera, rotation; public string weapon; public int width, height; }

        private IEnumerator Journey(int width, int height)
        {
            string phase = SessionState.GetString("VS032.CapturePhase", "after-validation-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff"));
            _folder = "Library/GameplayObservation/vs032/" + phase + "-" + height;
            Directory.CreateDirectory(_folder);
            var group = SizeGroup(); var asm = typeof(Editor).Assembly;
            object size = Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"), new object[] {
                Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"), 1), width, height, "VS032 capture" });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            _customSize = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            _gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(_gameView, _customSize); _gameView.Focus();
            yield return SceneManager.LoadSceneAsync("NexusLobby"); yield return null; yield return null;
            _player = Object.FindFirstObjectByType<PlayerActor>();
            Assert.AreEqual(width, Screen.width); Assert.AreEqual(height, Screen.height);
            yield return Capture("arrival");
            yield return Walk(-7, -5); yield return Walk(-8.5f, -4.8f); yield return KeyPress(Key.E);
            Assert.AreEqual(RunWeaponFamily.Bow, WeaponRunModifiers.Identify(_player.CurrentWeapon));
            yield return Capture("arsenal");
            yield return Walk(-4.9f, -4.8f); yield return KeyPress(Key.E);
            Assert.AreEqual(RunWeaponFamily.Spear, WeaponRunModifiers.Identify(_player.CurrentWeapon));
            yield return Walk(-8.5f, -4.8f); yield return Walk(-11.8f, -4.8f); yield return KeyPress(Key.E);
            Assert.AreEqual(RunWeaponFamily.Gauntlet, WeaponRunModifiers.Identify(_player.CurrentWeapon));
            yield return Walk(-11, -1); yield return Walk(-11, 2);
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerHUD>().GetComponent<Canvas>().enabled);
            yield return Capture("training");
            yield return Walk(-6, 2); yield return Walk(0, 2); yield return Walk(6, 3); yield return Walk(9.5f, 5.4f);
            yield return Capture("incursion");
            yield return KeyPress(Key.E);
            float deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "FirstSector") { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null; yield return null; _player = Object.FindFirstObjectByType<PlayerActor>();
            Assert.AreEqual(RunWeaponFamily.Gauntlet, WeaponRunModifiers.Identify(_player.CurrentWeapon));
            yield return KeyPress(Key.Escape);
            Assert.IsTrue(_player.GetComponent<PauseMenuUI>().IsPaused);
            // The test runner does not deliver native IMGUI pointer events reliably. Keep this
            // integration step explicitly API-driven; navigation and equipment remain virtual input.
            Assert.IsTrue(_player.GetComponent<PauseMenuUI>().TryRequestRunAbort());
            deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "NexusLobby") { Assert.Less(Time.realtimeSinceStartup, deadline, "Pause UI failed to return"); yield return null; }
            yield return null; yield return null; _player = Object.FindFirstObjectByType<PlayerActor>();
            var results = Object.FindFirstObjectByType<RunResultsPresenter>(); Assert.IsTrue(results.IsOpen); Assert.AreEqual("Aborted", results.Model.Outcome);
            yield return Capture("results");
            yield return KeyPress(Key.Escape); Assert.IsFalse(results.BlocksInput);
            Canvas.ForceUpdateCanvases();
            var button = (RectTransform)results.View.ManualButton.transform;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point, buttons = 1 }); yield return null; yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point }); yield return null;
            Assert.IsTrue(results.IsOpen, "Last result must remain accessible by mouse after returning.");
            yield return KeyPress(Key.Escape); Assert.IsFalse(results.BlocksInput);
            yield return Walk(-4.9f, -4.8f); yield return KeyPress(Key.E);
            Assert.AreEqual(RunWeaponFamily.Spear, WeaponRunModifiers.Identify(_player.CurrentWeapon));
            yield return Capture("resumed");
            File.WriteAllText(_folder + "/journey.txt", "Scripted tasks, virtual mouse/keyboard. Pause-confirmed abort uses TryRequestRunAbort API. No Warp or movement API.\nMeasured walking distance: " + _distance + "\n" + string.Join("\n", _steps));
        }
    }
}
