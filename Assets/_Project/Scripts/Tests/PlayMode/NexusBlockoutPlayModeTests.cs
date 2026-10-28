using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    // Scripted integration walkthrough. Virtual devices are not human acceptance.
    public sealed class NexusBlockoutPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private Keyboard _keyboard, _oldKeyboard;
        private Mouse _mouse, _oldMouse;
        private bool _keyboardEnabled, _mouseEnabled, _audio;
        private float _clock;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInput;
        private readonly Dictionary<string, int?> _prefs = new Dictionary<string, int?>();
        private PlayerActor _player;
        private EditorWindow _gameView;
        private int _oldSize, _customSize = -1;
        private string _folder;
        private readonly List<string> _steps = new List<string>();
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        [UnitySetUp] public IEnumerator Setup()
        {
            _clock = Time.timeScale; _audio = AudioListener.pause; Time.timeScale = 1; AudioListener.pause = false;
            SessionState.SetBool("VS034.Automated", true);
            foreach (string key in new[] { WeaponLoadout.PreferenceKey, CurrencyWallet.PreferenceKey,
                "TechGuy.Loadout.Unlocked.1", "TechGuy.Loadout.Unlocked.2" })
                _prefs[key] = PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null;
            _background = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            // Evidence capture uses an existing unlocked profile; never prepare a real save with grants.
            if (!WeaponLoadout.IsUnlocked(1) || !WeaponLoadout.IsUnlocked(2))
                Assert.Ignore("Evidence journey requires an already unlocked profile; no unlocks are granted here.");
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _oldKeyboard = Keyboard.current; _oldMouse = Mouse.current;
            _keyboardEnabled = _oldKeyboard != null && _oldKeyboard.enabled;
            _mouseEnabled = _oldMouse != null && _oldMouse.enabled;
            if (_keyboardEnabled) InputSystem.DisableDevice(_oldKeyboard);
            if (_mouseEnabled) InputSystem.DisableDevice(_oldMouse);
            _keyboard = InputSystem.AddDevice<Keyboard>(); _mouse = InputSystem.AddDevice<Mouse>();
            var t = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            _gameView = Resources.FindObjectsOfTypeAll(t).Cast<EditorWindow>().First();
            _oldSize = (int)t.GetProperty("selectedSizeIndex", Flags).GetValue(_gameView);
            _customSize = -1; _steps.Clear(); yield return null;
        }
        private static object SizeGroup()
        {
            var t = typeof(Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            return t.GetMethod("GetGroup").Invoke(t.BaseType.GetProperty("instance").GetValue(null), new object[] { 0 });
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (_player && _player.TryGetComponent(out PauseMenuUI pause)) pause.Resume();
            if (_keyboard != null) InputSystem.RemoveDevice(_keyboard);
            if (_mouse != null) InputSystem.RemoveDevice(_mouse);
            if (_keyboardEnabled && _oldKeyboard != null) InputSystem.EnableDevice(_oldKeyboard);
            if (_mouseEnabled && _oldMouse != null) InputSystem.EnableDevice(_oldMouse);
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            if (_gameView) _gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(_gameView, _oldSize);
            if (_customSize >= 0) { var g = SizeGroup(); g.GetType().GetMethod("RemoveCustomSize").Invoke(g, new object[] { _customSize }); }
            foreach (var pair in _prefs)
                if (pair.Value.HasValue) PlayerPrefs.SetInt(pair.Key, pair.Value.Value); else PlayerPrefs.DeleteKey(pair.Key);
            PlayerPrefs.Save(); SessionState.EraseBool("VS034.Automated");
            Time.timeScale = _clock; AudioListener.pause = _audio; yield return null;
        }
        [UnityTest] public IEnumerator Journey720() { yield return Journey(1280, 720); }
        [UnityTest] public IEnumerator Journey1080() { yield return Journey(1920, 1080); }

        private IEnumerator Walk(float x, float z)
        {
            var target = new Vector3(x, .06f, z);
            var point = Camera.main.WorldToScreenPoint(target);
            // The fixed follow camera cannot show a distant point behind the avatar. Walk toward it
            // with successive visible ground clicks, as a player does; never move via API.
            if (point.x < 24 || point.x > Screen.width - 24 || point.y < 24 || point.y > Screen.height - 24)
            {
                Vector3 step = Vector3.MoveTowards(_player.transform.position, target, 2.5f);
                yield return Walk(step.x, step.z);
                yield return Walk(x, z);
                yield break;
            }
            Assert.Greater(point.z, 0); Assert.That(point.x, Is.InRange(0, Screen.width)); Assert.That(point.y, Is.InRange(0, Screen.height));
            WorldClickResolver.TryResolve(Camera.main.ScreenPointToRay(point), _player.transform, 1 << LayerMask.NameToLayer("Ground"),
                out _, out bool crossesAvatar, Camera.main.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>());
            if (crossesAvatar)
            {
                // A click on the avatar deliberately does not move. Step sideways on visible ground
                // before clicking the destination, preserving production picking semantics.
                Vector3 side = _player.transform.position + Vector3.right * 1.1f;
                if (!NavMesh.SamplePosition(side, out _, .1f, NavMesh.AllAreas))
                    side = _player.transform.position - Vector3.right * 1.1f;
                Assert.IsTrue(NavMesh.SamplePosition(side, out _, .1f, NavMesh.AllAreas), "No lateral ground click available.");
                yield return Walk(side.x, side.z);
                yield return Walk(x, z);
                yield break;
            }
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point, buttons = 2 }); yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point });
            float deadline = Time.realtimeSinceStartup + 10;
            while (Vector3.Distance(_player.transform.position, target) > .55f)
            { Assert.Less(Time.realtimeSinceStartup, deadline, "Pointer route blocked: " + target + " from " + _player.transform.position + " stopped=" + _player.GetComponent<NavMeshAgent>().isStopped); yield return null; }
            _steps.Add("virtual pointer " + target); yield return new WaitForSecondsRealtime(.15f);
        }
        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null; yield return null;
        }
        private static Key Bound(GameControl control) => (Key)Enum.Parse(typeof(Key), GamePreferences.Binding(control).ToString(), true);
        private IEnumerator Capture(string name)
        {
            yield return new WaitForSecondsRealtime(.2f);
            ScreenCapture.CaptureScreenshot(_folder + "/" + name + ".png");
            yield return new WaitForEndOfFrame(); yield return null;
            File.WriteAllText(_folder + "/" + name + ".json", JsonUtility.ToJson(new CaptureState {
                player = _player.transform.position, camera = Camera.main.transform.position,
                weapon = _player.CurrentWeapon.name, width = Screen.width, height = Screen.height, fov = Camera.main.fieldOfView }));
        }
        [Serializable] private sealed class CaptureState { public Vector3 player, camera; public string weapon; public int width, height; public float fov; }

        private IEnumerator Journey(int width, int height)
        {
            _folder = "Library/GameplayObservation/vs034-revision/" + SessionState.GetString("VS034.Phase", "validation-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmss")) + "-" + height;
            Directory.CreateDirectory(_folder);
            var group = SizeGroup(); var asm = typeof(Editor).Assembly;
            object size = Activator.CreateInstance(asm.GetType("UnityEditor.GameViewSize"), new object[] {
                Enum.ToObject(asm.GetType("UnityEditor.GameViewSizeType"), 1), width, height, "VS034 capture" });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { size });
            _customSize = (int)group.GetType().GetMethod("GetTotalCount").Invoke(group, null) - 1;
            _gameView.GetType().GetProperty("selectedSizeIndex", Flags).SetValue(_gameView, _customSize); _gameView.Focus();
            yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null; yield return null;
            _player = Object.FindFirstObjectByType<PlayerActor>();
            Assert.AreEqual(width, Screen.width); Assert.AreEqual(height, Screen.height); Assert.AreEqual(40, Camera.main.fieldOfView);
            var agent = _player.GetComponent<NavMeshAgent>(); Assert.IsTrue(agent.isOnNavMesh);
            foreach (Vector3 voidPoint in new[] { new Vector3(8,0,1), new Vector3(-14,0,5) })
            {
                Assert.IsFalse(NavMesh.SamplePosition(voidPoint,out _,.2f,NavMesh.AllAreas),"Negative space must not retain the rejected arena's navigation.");
                Assert.IsFalse(Physics.Raycast(voidPoint+Vector3.up*4,Vector3.down,5,1<<LayerMask.NameToLayer("Ground")),"No hidden old floor/collider in the gaps.");
            }
            foreach (var target in new[] { new Vector3(-11.8f, 0, -3.8f), new Vector3(-11.8f, 0, -.8f),
                new Vector3(-11.8f, 0, 2.2f), new Vector3(-11, 0, 11.5f), new Vector3(9.5f, 0, 11f) })
            {
                var path = new NavMeshPath(); Assert.IsTrue(agent.CalculatePath(target, path));
                Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
            }
            yield return Capture("reconexao");
            // Fast route first, without interacting with arsenal/training.
            yield return Walk(3,-4.6f); yield return Walk(2.5f,-1); yield return Capture("arrival-connection");
            yield return Walk(1.25f,2); yield return Walk(1.25f,6); yield return Capture("north-connection");
            yield return Walk(2.2f,8.5f); yield return Walk(5.5f,8.5f); yield return Walk(9.5f,10.9f);
            yield return Capture("travessia");
            yield return Walk(5.5f,8.5f); yield return Walk(1.25f,8.5f); yield return Walk(1.25f,4);
            yield return Walk(1.25f,.5f); yield return Walk(-3,.5f); yield return Walk(-6,.5f);
            yield return Walk(-10,0); yield return Walk(-11.8f,-.8f); yield return Walk(-11.8f,-3.8f);
            var arsenal = Object.FindFirstObjectByType<LobbyArsenal>();
            int casts = 0; var holder = _player.GetComponent<AbilityHolder>(); holder.AbilityUsed += slot => casts++;
            for (int i = 0; i < 3; i++)
            {
                yield return Walk(-11.8f, -3.8f + 3 * i);
                Assert.AreEqual(i, arsenal.FocusedWeaponIndex); Assert.IsFalse(arsenal.DetailsRequested);
                yield return Press(Bound(GameControl.Skill3));
                Assert.AreEqual(i, PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey)); Assert.AreEqual(0, casts, "Equipment may not cast.");
                yield return Capture("arma-" + i);
                Assert.LessOrEqual(arsenal.VisibleCardRect.height, 126);
            }
            yield return Press(Bound(GameControl.Skill4)); Assert.IsTrue(arsenal.DetailsRequested); Assert.AreEqual(0, casts);
            yield return Capture("detalhes");
            Rect card = arsenal.VisibleCardRect;
            Vector2 hover = new Vector2(card.center.x * WeaponCardView.Scale, Screen.height - card.center.y * WeaponCardView.Scale);
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = hover }); yield return null;
            Assert.IsTrue(holder.BlocksWorldInput, "Expanded card must own pointer input over its rectangle.");
            Vector3 heldPosition = _player.transform.position;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = hover, buttons = 2 }); yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = hover }); yield return new WaitForSecondsRealtime(.2f);
            Assert.Less(Vector3.Distance(heldPosition, _player.transform.position), .1f, "Card click must not move the player.");
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = new Vector2(Screen.width / 2f, Screen.height / 2f) }); yield return null;
            yield return Press(Bound(GameControl.Skill4)); Assert.IsFalse(arsenal.DetailsRequested);
            var results = Object.FindFirstObjectByType<RunResultsPresenter>();
            results.OpenManual(); // Explicit API setup for ownership regression.
            yield return Press(Bound(GameControl.Skill3)); Assert.AreEqual(2, PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey));
            yield return Press(Key.Escape); Assert.IsFalse(results.BlocksInput);
            yield return Press(Key.Escape); Assert.IsTrue(_player.GetComponent<PauseMenuUI>().IsPaused);
            yield return Press(Bound(GameControl.Skill3)); Assert.AreEqual(2, PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey));
            yield return Press(Key.Escape);
            yield return Walk(-9.5f,2); yield return Walk(-9.5f,5); yield return Capture("practice-connection");
            yield return Walk(-9.5f,8); yield return Walk(-11,11.5f);
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerHUD>().GetComponent<Canvas>().enabled);
            var dummy = Object.FindFirstObjectByType<TrainingDummy>();
            float targetHealth = dummy.health;
            Vector2 aim = Camera.main.WorldToScreenPoint(dummy.transform.position);
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = aim }); yield return null;
            yield return Press(Bound(GameControl.Skill1)); yield return new WaitForSeconds(1.2f);
            Assert.Greater(casts, 0, "A real skill must execute in the training area.");
            Assert.Less(dummy.health,targetHealth,"Training skill must actually damage the existing target.");
            yield return Capture("treino");
            yield return Walk(-6,11); yield return Walk(-2,9.5f); yield return Capture("practice-departure-connection");
            yield return Walk(2.2f,9.5f); yield return Walk(5.5f,9.5f); yield return Walk(9.5f,11);
            yield return Press(Bound(GameControl.Skill3));
            float deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "FirstSector") { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null; yield return null; _player = Object.FindFirstObjectByType<PlayerActor>();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            // Mirrors the Editor review hook. Production FirstSector is unchanged on disk.
            typeof(ProgressionDirector).GetField("_returnScene", Flags).SetValue(director, "NexusBlockout");
            var session = director.ManualSession.Id;
            Assert.AreEqual(RunWeaponFamily.Spear, WeaponRunModifiers.Identify(_player.CurrentWeapon));
            yield return Press(Key.Escape); Assert.IsTrue(_player.GetComponent<PauseMenuUI>().IsPaused);
            Assert.IsTrue(_player.GetComponent<PauseMenuUI>().TryRequestRunAbort()); // API confirmation, not native mouse.
            deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "NexusBlockout") { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null; yield return null; _player = Object.FindFirstObjectByType<PlayerActor>();
            Assert.Less(Vector3.Distance(_player.transform.position, new Vector3(6.5f, .1f, -6.8f)), .5f);
            results = Object.FindFirstObjectByType<RunResultsPresenter>(); Assert.IsTrue(results.IsOpen);
            Assert.AreEqual(session.ToString(), results.Model.SessionId); Assert.AreEqual("Aborted", results.Model.Outcome);
            yield return Capture("resultado"); yield return Press(Key.Escape); Assert.IsFalse(results.BlocksInput);
            Canvas.ForceUpdateCanvases(); var button = (RectTransform)results.View.ManualButton.transform;
            Vector2 click = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center));
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = click, buttons = 1 }); yield return null; yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = click }); yield return null;
            Assert.IsTrue(results.IsOpen); yield return Press(Key.Escape);
            yield return Walk(3,-4.6f); yield return Walk(2.5f,-1); yield return Walk(-2,0);
            yield return Walk(-5.5f,0); yield return Walk(-9.5f,-.8f); yield return Walk(-11.8f,-.8f);
            yield return Press(Bound(GameControl.Skill3)); Assert.AreEqual(RunWeaponFamily.Bow, WeaponRunModifiers.Identify(_player.CurrentWeapon));
            yield return Capture("oficina");
            File.WriteAllText(_folder + "/journey.txt", "Virtual mouse/keyboard; no movement API/Warp. Ownership modal opened by API; abort confirmed by PauseMenu API; runtime return target injected by fixture. Existing unlocked save used, exact preferences restored.\n" + string.Join("\n", _steps));
        }
    }
}
