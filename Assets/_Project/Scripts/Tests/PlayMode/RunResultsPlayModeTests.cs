using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class RunResultsPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private Keyboard _keyboard, _oldKeyboard;
        private Mouse _mouse, _oldMouse;
        private bool _keyboardEnabled, _mouseEnabled;
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInput;
        private float _scale;
        private bool _audio, _cursor;
        private CursorLockMode _lock;
        private RunResultsPresenter _presenter;
        [UnitySetUp] public IEnumerator SetupScene()
        {
            _scale=Time.timeScale;_audio=AudioListener.pause;_cursor=Cursor.visible;_lock=Cursor.lockState;
            _background = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _oldKeyboard=Keyboard.current;_oldMouse=Mouse.current;
            _keyboardEnabled=_oldKeyboard!=null&&_oldKeyboard.enabled;_mouseEnabled=_oldMouse!=null&&_oldMouse.enabled;
            if(_keyboardEnabled)InputSystem.DisableDevice(_oldKeyboard);if(_mouseEnabled)InputSystem.DisableDevice(_oldMouse);
            _keyboard=InputSystem.AddDevice<Keyboard>();_mouse=InputSystem.AddDevice<Mouse>();
            Time.timeScale=1;AudioListener.pause=false;
            yield return SceneManager.LoadSceneAsync("NexusBlockout");yield return null;yield return null;
            _presenter=UnityEngine.Object.FindFirstObjectByType<RunResultsPresenter>();Assert.IsNotNull(_presenter);
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if(_presenter){_presenter.View.CloseDetail();_presenter.Close();}
            InputSystem.RemoveDevice(_keyboard);InputSystem.RemoveDevice(_mouse);
            if(_keyboardEnabled&&_oldKeyboard!=null)InputSystem.EnableDevice(_oldKeyboard);
            if(_mouseEnabled&&_oldMouse!=null)InputSystem.EnableDevice(_oldMouse);
            _oldKeyboard?.MakeCurrent();_oldMouse?.MakeCurrent();
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            Time.timeScale=_scale;AudioListener.pause=_audio;Cursor.visible=_cursor;Cursor.lockState=_lock;
            yield return null;
        }
        [UnityTest] public IEnumerator GPRResultsReboundSideMouseButtonRequiresRelease()
        {
            var controls = UnityEngine.Object.FindFirstObjectByType<PlayerActor>().GetComponent<CharControlScript>();
            try
            {
                using (var prefs = new BasicAttackR1PlayModeTests.ConfigPreferenceScope())
                {
                    var bindings = GamePreferences.DefaultDirectBindings(); bindings[(int)GameControl.Move] = KeyCode.Mouse4;
                    GamePreferences.SaveBindings(bindings);
                    Store(Data()); _presenter.OpenManual(); yield return null;
                    InputSystem.QueueStateEvent(_mouse, new MouseState().WithButton(MouseButton.Forward));
                    yield return null; yield return null;
                    _presenter.Close(); yield return null; yield return null;
                    Assert.That(_presenter.BlocksInput, Is.True, "Rebound side mouse button must retain modal ownership until release.");
                    InputSystem.QueueStateEvent(_mouse, new MouseState()); yield return null; yield return null;
                    Assert.That(_presenter.BlocksInput, Is.False);
                    yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null; yield return null;
                    Assert.That(GamePreferences.WeaponSpecialBinding, Is.EqualTo(KeyCode.Mouse4), "Binding survives a scene reload.");
                }
            }
            finally { var activePlayer = UnityEngine.Object.FindFirstObjectByType<PlayerActor>(); if (activePlayer) GamePreferences.UseDirectControls(activePlayer.GetComponent<CharControlScript>()); }
        }

        private static RunSummary Data(string outcome="Victory",double total=100)
        {
            var s=new RunSummary {SessionId=Guid.NewGuid().ToString(),Seed="42",InitialWeapon="Gauntlet",StartedUtc=DateTime.UtcNow.ToString("o"),
                Outcome=outcome,Finalized=true,WallSeconds=64,ScaledSeconds=60,EffectiveDamage=total,ObservedEnemyHpLoss=total,LargestImpact=total};
            if(total>0)s.Damage.Add(new RunDamageBucket {Weapon="Gauntlet",Ability="Q",Producer="Direct",PlayerAttributed=true,Effective=total});
            return s;
        }
        private static void Store(RunSummary s) {PlayerPrefs.SetString(RunSummaryStore.PreferenceKey,JsonUtility.ToJson(s));PlayerPrefs.Save();}
        [UnityTest] public IEnumerator AllThreeOutcomesUseStoreAndReopenWithoutDuplicatingEventSystems()
        {
            foreach(var outcome in new[]{"Victory","Death","Aborted"})
            {
                Store(Data(outcome));_presenter.OpenManual();Assert.AreEqual(outcome,_presenter.Model.Outcome);
                Assert.IsTrue(_presenter.View.VisibleText.Contains("100"));Assert.AreEqual(100,_presenter.Model.Abilities.Sum(x=>x.Total));
                _presenter.Close();yield return null;yield return null;Assert.IsFalse(_presenter.BlocksInput);
            }
            Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
        }
        [UnityTest] public IEnumerator HandoffForAllOutcomesOpensOnceAfterActualNexusLoad()
        {
            foreach(var outcome in new[]{"Victory","Death","Aborted"})
            {
                var s=Data(outcome);Store(s);RunResultsHandoff.Expect(s.SessionId);
                yield return SceneManager.LoadSceneAsync("NexusBlockout");yield return null;yield return null;
                _presenter=UnityEngine.Object.FindFirstObjectByType<RunResultsPresenter>();
                Assert.IsTrue(_presenter.IsOpen);Assert.AreEqual(outcome,_presenter.Model.Outcome);Assert.AreEqual(s.SessionId,_presenter.Model.SessionId);
                Assert.IsNull(RunResultsHandoff.Consume());_presenter.Close();yield return null;yield return null;
            }
        }
        [UnityTest] public IEnumerator KeyboardCanOpenLastResultFromFreshNexusWithoutMouse()
        {
            Store(Data());Assert.AreEqual(_presenter.View.ManualButton.gameObject,EventSystem.current.currentSelectedGameObject);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Enter));yield return null;
            Assert.IsTrue(_presenter.IsOpen,"Enter must open without submitting the newly focused close button in the same frame.");Assert.IsTrue(UnityEngine.Object.FindFirstObjectByType<PlayerActor>().GetComponent<AbilityHolder>().BlocksWorldInput);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());
        }
        [UnityTest] public IEnumerator MouseOpensManualEntryAndDetailTrapsNavigation()
        {
            Store(Data());Canvas.ForceUpdateCanvases();
            var rect=(RectTransform)_presenter.View.ManualButton.transform;
            var center=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
            InputSystem.QueueStateEvent(_mouse,new MouseState {position=center,buttons=1});yield return null;
            Assert.IsTrue(_presenter.IsOpen);Assert.IsTrue(UnityEngine.Object.FindFirstObjectByType<LobbyInteraction>().IsPanelOpen);
            InputSystem.QueueStateEvent(_mouse,new MouseState {position=center});yield return null;
            _presenter.View.OpenDetail("fixture",_presenter.View.CloseButton.gameObject);
            Assert.IsFalse(_presenter.View.CloseButton.IsInteractable());Assert.IsTrue(_presenter.View.Tabs.All(b=>!b.IsInteractable()));
            _presenter.View.CloseDetail();Assert.IsTrue(_presenter.View.CloseButton.IsInteractable());
            Assert.AreEqual(_presenter.View.CloseButton.gameObject,EventSystem.current.currentSelectedGameObject);
            Assert.IsTrue(_presenter.View.CloseButton.GetComponent<UnityEngine.UI.Outline>().enabled,"keyboard focus must be visible");
        }
        [UnityTest] public IEnumerator EmptyInvalidAndValidZeroStayDistinct()
        {
            PlayerPrefs.DeleteKey(RunSummaryStore.PreferenceKey);_presenter.OpenManual();Assert.IsFalse(_presenter.Model.Available);Assert.AreEqual("—",_presenter.Model.Damage);
            _presenter.Close();yield return null;yield return null;
            PlayerPrefs.SetString(RunSummaryStore.PreferenceKey,"bad json");_presenter.OpenManual();Assert.IsFalse(_presenter.Model.Available);
            _presenter.Close();yield return null;yield return null;
            Store(Data("Aborted",0));_presenter.OpenManual();Assert.IsTrue(_presenter.Model.Available);Assert.AreEqual("0",_presenter.Model.Damage);Assert.IsEmpty(_presenter.Model.Abilities);
        }
        [UnityTest] public IEnumerator SnapshotAStaysOpenWhenBIsSavedAndReopensAsB()
        {
            var a=Data("Victory",100);Store(a);_presenter.OpenManual();var model=_presenter.Model;
            var b=Data("Death",250);Store(b);a.EffectiveDamage=999;yield return null;
            Assert.AreSame(model,_presenter.Model);Assert.AreEqual("100",model.Damage);Assert.AreEqual(a.SessionId,model.SessionId);
            _presenter.Close();yield return null;yield return null;_presenter.OpenManual();Assert.AreEqual(b.SessionId,_presenter.Model.SessionId);Assert.AreEqual("250",_presenter.Model.Damage);
        }
        [UnityTest] public IEnumerator ExpectedSessionMismatchDoesNotPresentOldSummary()
        {
            Store(Data());_presenter.OpenExpected(Guid.NewGuid().ToString());Assert.IsFalse(_presenter.Model.Available);Assert.IsTrue(_presenter.Model.Warning.Contains("esperada"));yield return null;
        }
        [Test] public void MixedSourcesReconcileAndKeepUnownedHpOutside()
        {
            var s=Data();s.Damage[0].Effective=60;
            s.Damage.Add(new RunDamageBucket {Weapon="Gauntlet",Ability="Q",Producer="Conductor",PlayerAttributed=true,Secondary=true,Effective=25});
            s.Damage.Add(new RunDamageBucket {Weapon="Unknown",Ability="Unattributed",Producer="BurnTick",PlayerAttributed=true,Secondary=true,Effective=15});
            s.Damage.Add(new RunDamageBucket {Weapon="Unknown",Ability="Unattributed",PlayerAttributed=false,Effective=12});s.UnattributedEnemyHpLoss=12;s.ObservedEnemyHpLoss=112;
            var m=new RunResultsModel(s);Assert.AreEqual(100,m.Abilities.Sum(x=>x.Total));Assert.AreEqual(85,m.Abilities.First().Total);Assert.AreEqual(3,m.Sources.Count);Assert.IsTrue(m.Warning.Contains("fora"));
            Assert.AreEqual(60,s.Damage[0].Effective,"model must not modify DTO");
        }
        [UnityTest] public IEnumerator ExtensiveBuildScrollsAndKeyboardFocusMoves()
        {
            var s=Data();for(int i=0;i<35;i++)s.Build.Add(new RunBoonSummary {Id="fixture-long-build-"+i,Family="Protocol",Acquisitions=1});Store(s);
            _presenter.OpenManual();_presenter.View.SelectTab(2);yield return null;
            Assert.Greater(_presenter.View.List.content.rect.height,_presenter.View.List.viewport.rect.height);
            _presenter.View.Scroll(-1);Assert.Less(_presenter.View.List.verticalNormalizedPosition,1);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Tab));yield return null;
            Assert.IsNotNull(EventSystem.current.currentSelectedGameObject);Assert.IsFalse(_presenter.GetComponent<RunResultsView>().DetailOpen);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());
        }
        [UnityTest] public IEnumerator EscapeClosesDetailThenPanelAndHeldInputBlocksWorldUntilReleased()
        {
            Store(Data());_presenter.OpenManual();yield return null;
            var pause=UnityEngine.Object.FindFirstObjectByType<PlayerActor>().GetComponent<PauseMenuUI>();
            var lobby=UnityEngine.Object.FindFirstObjectByType<LobbyInteraction>();
            _presenter.View.OpenDetail("fixture",_presenter.View.CloseButton.gameObject);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Escape));yield return null;
            Assert.IsFalse(_presenter.View.DetailOpen);Assert.IsTrue(_presenter.IsOpen);Assert.IsFalse(pause.IsPaused);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Escape));yield return null;
            Assert.IsFalse(_presenter.IsOpen);Assert.IsTrue(_presenter.BlocksInput);Assert.IsTrue(lobby.IsPanelOpen);Assert.IsFalse(pause.IsPaused);
            yield return null;Assert.IsTrue(_presenter.BlocksInput);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());yield return null;yield return null;
            Assert.IsFalse(_presenter.BlocksInput);Assert.IsFalse(lobby.IsPanelOpen);Assert.AreEqual(1,Time.timeScale);
        }
        [UnityTest] public IEnumerator DisableWithDetailRestoresCursorMovementAndCanReopen()
        {
            Store(Data());var player=UnityEngine.Object.FindFirstObjectByType<PlayerActor>();var agent=player.GetComponent<UnityEngine.AI.NavMeshAgent>();
            bool hasAgent=agent&&agent.enabled&&agent.isOnNavMesh;bool stopped=hasAgent&&agent.isStopped;
            bool visible=Cursor.visible;var locked=Cursor.lockState;
            _presenter.OpenManual();Assert.IsTrue(player.GetComponent<AbilityHolder>().BlocksWorldInput);
            if(hasAgent)Assert.IsTrue(agent.isStopped);
            _presenter.View.OpenDetail("fixture",_presenter.View.CloseButton.gameObject);
            _presenter.gameObject.SetActive(false);yield return null;
            Assert.AreEqual(visible,Cursor.visible);Assert.AreEqual(locked,Cursor.lockState);if(hasAgent)Assert.AreEqual(stopped,agent.isStopped);
            _presenter.gameObject.SetActive(true);yield return null;_presenter.OpenManual();Assert.IsTrue(_presenter.View.CloseButton.interactable);
            _presenter.Close();yield return null;yield return null;Assert.IsFalse(player.GetComponent<AbilityHolder>().BlocksWorldInput);
        }
        [UnityTest] public IEnumerator PersistedSummarySurvivesSceneReloadWithoutAutomaticOldResult()
        {
            var s=Data();Store(s);yield return SceneManager.LoadSceneAsync("NexusBlockout");yield return null;yield return null;
            _presenter=UnityEngine.Object.FindFirstObjectByType<RunResultsPresenter>();Assert.IsFalse(_presenter.IsOpen);
            _presenter.OpenManual();Assert.AreEqual(s.SessionId,_presenter.Model.SessionId);
            Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
        }
        [UnityTest] public IEnumerator ThreeRealSessionAbortsReturnAndAutomaticallyOpenMatchingSnapshot()
        {
            for(int i=0;i<3;i++)
            {
                if(_presenter&&_presenter.IsOpen){_presenter.Close();yield return null;yield return null;}
                yield return SceneManager.LoadSceneAsync("FirstSector");yield return null;yield return null;yield return null;
                var director=UnityEngine.Object.FindFirstObjectByType<ProgressionDirector>();var session=director.ManualSession;
                Assert.IsTrue(director.TryAbortManualRun(session.Id));
                float deadline=Time.realtimeSinceStartup+12;
                while(SceneManager.GetActiveScene().name!="NexusBlockout"){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                yield return null;yield return null;
                _presenter=UnityEngine.Object.FindFirstObjectByType<RunResultsPresenter>();Assert.IsTrue(_presenter.IsOpen);
                Assert.AreEqual(session.Id.ToString(),_presenter.Model.SessionId);Assert.AreEqual("Aborted",_presenter.Model.Outcome);
                Assert.AreEqual(1,UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None).Length);
            }
        }

        [UnityTest] public IEnumerator GPRSafetyResultsHeldWQCannotReplayAfterClosing()
        {
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            var control = player.GetComponent<CharControlScript>();
            Assert.IsTrue(control.DirectMovementEnabled);
            control.SendMessage("OnApplicationFocus", true);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null; yield return null;
            _presenter.OpenManual(); Assert.IsTrue(_presenter.IsOpen);
            int uses = 0; player.GetComponent<AbilityHolder>().AbilityUsed += _ => uses++;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W, Key.Q)); yield return null;
            Vector3 start = player.transform.position;
            _presenter.Close(); yield return null; yield return null;
            Assert.That(Vector3.Distance(start, player.transform.position), Is.LessThan(.01f)); Assert.AreEqual(0, uses);
            Assert.IsFalse(control.GameplayInputReady);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null; yield return null; yield return null;
            Assert.IsTrue(control.GameplayInputReady);
        }

        [UnityTest] public IEnumerator GPRSafetySceneReloadRequiresHeldGameplayKeysReleased()
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.W, Key.Q)); yield return null;
            yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null;
            _presenter = UnityEngine.Object.FindFirstObjectByType<RunResultsPresenter>();
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            var control = player.GetComponent<CharControlScript>(); control.SendMessage("OnApplicationFocus", true);
            Vector3 start = player.transform.position;
            int uses = 0; player.GetComponent<AbilityHolder>().AbilityUsed += _ => uses++;
            yield return null; yield return null;
            Assert.That(Vector3.Distance(start, player.transform.position), Is.LessThan(.01f)); Assert.AreEqual(0, uses);
            Assert.IsFalse(control.GameplayInputReady);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null; yield return null;
            Assert.IsTrue(control.GameplayInputReady);
        }
    }
}
