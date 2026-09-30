using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class TutorialValidation
{
    private const string Pending = "TechGuy.TutorialValidation";
    private static int _phase, _errors, _checks, _savedWeapon;
    private static readonly string[] PreferenceKeys = { GamePreferences.TutorialSeenKey, "TechGuy.Settings.Volume", "TechGuy.Settings.Quality", "TechGuy.Settings.Width", "TechGuy.Settings.Height", "TechGuy.Settings.Fullscreen", "TechGuy.Settings.VSync" };
    private static double _next, _deadline;
    private static TutorialDirector _tutorial;
    private static PlayerActor _player;
    private static TutorialTrainingTarget _target;
    private static CharControlScript _controls;
    private static AbilityHolder _abilities;
    static TutorialValidation() { if (SessionState.GetBool(Pending,false)) Subscribe(); }
    public static void BuildAndRun() { TutorialSceneBuilder.Build(); MainMenuSceneBuilder.Build(); Run(); }
    public static void Run()
    {
        _next = EditorApplication.timeSinceStartup+15;
        EditorApplication.update += Wait;
    }
    private static void Wait()
    {
        if (EditorApplication.timeSinceStartup < _next || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
        EditorApplication.update -= Wait;
        EditorSceneManager.OpenScene(TutorialSceneBuilder.ScenePath);
        SessionState.SetBool(Pending,true); Subscribe(); EditorApplication.EnterPlaymode();
    }
    private static void Subscribe()
    { EditorApplication.playModeStateChanged -= State; EditorApplication.playModeStateChanged += State; }
    private static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _phase = _errors = _checks = 0; _savedWeapon = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0);
        foreach (string key in PreferenceKeys)
        {
            SessionState.SetBool(key+".Had",PlayerPrefs.HasKey(key));
            if (key.EndsWith("Volume")) SessionState.SetFloat(key+".Before",PlayerPrefs.GetFloat(key));
            else SessionState.SetInt(key+".Before",PlayerPrefs.GetInt(key));
        }
        Application.logMessageReceived += Log; _next = EditorApplication.timeSinceStartup+2;
        _deadline = EditorApplication.timeSinceStartup+90; EditorApplication.update += Tick;
    }
    private static void Log(string text,string stack,LogType type)
    { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) _errors++; }
    private static T[] Components<T>() where T : Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
    private static void Require(bool condition,string message) { _checks++; if (!condition) throw new Exception(message); }
    private static T Field<T>(string name) => (T)typeof(TutorialDirector).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(_tutorial);
    private static void Resolve()
    {
        _tutorial = Components<TutorialDirector>().Single(); _player = Components<PlayerActor>().Single();
        _target = Components<TutorialTrainingTarget>().Single(); _controls = _player.GetComponent<CharControlScript>();
        _abilities = _player.GetComponent<AbilityHolder>();
    }
    private static void AimAtTarget()
    {
        var follow = Camera.main.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
        if (follow) typeof(TechGuy.Cameras.TG_TopDown_Camera)
            .GetMethod("HandleCamera",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(follow,null);
        if (Mouse.current == null) InputSystem.AddDevice<Mouse>();
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(_target.transform.position);
        InputState.Change(Mouse.current,new MouseState { position = screenPoint });
    }
    private static void Next(double delay=.4) { _phase++; _next = EditorApplication.timeSinceStartup+delay; }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        try
        {
            Require(EditorApplication.timeSinceStartup < _deadline,"Tutorial validation timeout");
            switch (_phase)
            {
                case 0:
                    Resolve();
                    TutorialDirector.Beat attackLesson = Field<TutorialDirector.Beat[]>("_beats")[(int)TutorialStage.BasicAttack];
                    Require(EditorBuildSettings.scenes.First(s=>s.enabled).path == MainMenuSceneBuilder.ScenePath,"Menu is first enabled scene");
                    Require(_tutorial.Stage == TutorialStage.Movement && !_target.gameObject.activeSelf,"Starts with movement, target gated");
                    Require(Components<EnemyAI>().All(e=>!e.gameObject.activeSelf),"Encounter cannot start early");
                    Require(_player.CurrentWeapon.abilities[0] is BreakerGauntletAbility,"Predictable tutorial loadout");
                    Require(attackLesson.control.Contains("SHIFT") && attackLesson.instruction.Contains("esquerdo") && attackLesson.instruction.Contains("direito"),"Tutorial teaches directional basic attack on both mouse buttons");
                    Require(_player.GetComponent<NavMeshAgent>().Warp(Field<Transform>("_movementGoal").position),"Checkpoint navigable"); Next(); break;
                case 1:
                    Require(_tutorial.Stage == TutorialStage.Dodge,"Movement progresses on proximity");
                    Require(_abilities.TryUseDash(_controls.dashScript),"Real dash accepted"); Next(1); break;
                case 2:
                    Require(_tutorial.Stage == TutorialStage.BasicAttack,"Dash progresses lesson");
                    Require(_target.gameObject.activeSelf,"Target activated");
                    _player.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.1f,1));
                    Require(_controls.TryDirectionalBasicAttack(_target.transform.position),"Directional basic one accepted without selecting a target"); Next(1); break;
                case 3: Require(_controls.TryDirectionalBasicAttack(_target.transform.position),"Directional basic two accepted"); Next(1); break;
                case 4: Require(_controls.TryDirectionalBasicAttack(_target.transform.position),"Directional basic three accepted"); Next(1); break;
                case 5:
                    Require(_tutorial.Stage == TutorialStage.Skill && _tutorial.BasicHits == 3,"Three actual hits advance");
                    Require(!_target.IsDead,"Training target survives");
                    Next(); break;
                case 6:
                    AimAtTarget();
                    Require(_abilities.TryUseAbility(0),"Real Q accepted"); Next(1.5); break;
                case 7:
                    Require(_tutorial.Stage == TutorialStage.Encounter,"Skill must actually hit target");
                    Require(Components<EnemyAI>().All(e=>e.gameObject.activeSelf),"Encounter activated");
                    foreach (var enemy in Components<EnemyAI>()) _player.TryApplyDamage(enemy.GetComponent<Actor>(),10000);
                    Next(1); break;
                case 8:
                    Require(_tutorial.Stage == TutorialStage.Reward,"Fight grants reward");
                    var run = _player.GetComponent<RunBoons>();
                    Require(run && run.IsChoosing && run.Choices.Count == 3,"Real modifier choices available");
                    Require(run.Choose(0),"Reward selection succeeds"); Next(); break;
                case 9:
                    Require(_tutorial.Stage == TutorialStage.Departure && _target.gameObject.activeSelf,"Practice and exit unlocked");
                    Require(_player.GetComponent<NavMeshAgent>().Warp(Field<Transform>("_exit").position),"Exit navigable"); Next(3); break;
                case 10:
                    Require(SceneManager.GetActiveScene().name == "NexusLobby","Completion reaches lobby");
                    Require(!Components<RunBoons>().Any(),"Training modifier does not leak to lobby");
                    SceneManager.LoadScene("PrologueTutorial"); Next(2); break;
                case 11:
                    Resolve(); _player.TakeDamage(100000); Next(2); break;
                case 12:
                    Resolve(); Require(!_player.IsDead && _tutorial.Stage == TutorialStage.Movement,"Death restarts tutorial");
                    _tutorial.Skip(); Next(2); break;
                case 13:
                    Require(SceneManager.GetActiveScene().name == "NexusLobby","Skip reaches lobby");
                    Require(PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0) == _savedWeapon,"Saved weapon preserved");
                    Require(GamePreferences.TutorialSeen,"Completion/skip unlocks continue");
                    SceneManager.LoadScene("MainMenu"); Next(2); break;
                case 14:
                    var menu = Components<MainMenuUI>().Single();
                    Require(menu.CurrentPage == MainMenuUI.Page.Home,"Menu starts at home");
                    menu.Open(MainMenuUI.Page.Settings);
                    var draft = (GamePreferences.Options)typeof(MainMenuUI).GetField("_draft",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(menu);
                    draft.Volume=.37f; draft.VSync=false;
                    menu.ApplySettings();
                    Require(Mathf.Approximately(GamePreferences.Read().Volume,.37f) && Mathf.Approximately(AudioListener.volume,.37f),"Volume applied and persisted");
                    Require(QualitySettings.vSyncCount==0 && !GamePreferences.Read().VSync,"VSync applied and persisted");
                    menu.Open(MainMenuUI.Page.Home); menu.Open(MainMenuUI.Page.Settings);
                    draft = (GamePreferences.Options)typeof(MainMenuUI).GetField("_draft",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(menu);
                    draft.Volume=.9f; menu.Open(MainMenuUI.Page.Home);
                    Require(Mathf.Approximately(GamePreferences.Read().Volume,.37f),"Unsaved changes discarded");
                    menu.Open(MainMenuUI.Page.Controls); Require(menu.CurrentPage == MainMenuUI.Page.Controls,"Controls accessible");
                    menu.Open(MainMenuUI.Page.Home); menu.StartJourney(); Next(2); break;
                case 15:
                    Require(SceneManager.GetActiveScene().name == "NexusLobby","Continue routes directly to lobby");
                    Components<LobbyInteraction>().Single().ReturnToMainMenu(); Next(2); break;
                case 16: Components<MainMenuUI>().Single().StartJourney(true); Next(2); break;
                case 17:
                    Resolve(); Require(_tutorial.Stage == TutorialStage.Movement,"Replay starts tutorial");
                    PlayerPrefs.DeleteKey(GamePreferences.TutorialSeenKey);
                    SceneManager.LoadScene("MainMenu"); Next(2); break;
                case 18: Components<MainMenuUI>().Single().StartJourney(); Next(2); break;
                case 19:
                    Resolve(); Require(_tutorial.Stage == TutorialStage.Movement,"First journey routes to tutorial");
                    Require(_errors == 0,"No runtime errors: "+_errors);
                    Debug.Log("TUTORIAL_VALIDATION_SUCCESS: "+_checks+" checks, real combat, reward, death, skip, menu routing and saved settings."); Finish(0); break;
            }
        }
        catch (Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static void Finish(int code)
    {
        foreach (string key in PreferenceKeys)
        {
            if (!SessionState.GetBool(key+".Had",false)) PlayerPrefs.DeleteKey(key);
            else if (key.EndsWith("Volume")) PlayerPrefs.SetFloat(key,SessionState.GetFloat(key+".Before",1));
            else PlayerPrefs.SetInt(key,SessionState.GetInt(key+".Before",0));
        }
        PlayerPrefs.Save(); GamePreferences.Apply(GamePreferences.Read());
        SessionState.SetBool(Pending,false); EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= State; Application.logMessageReceived -= Log; EditorApplication.Exit(code);
    }
}
