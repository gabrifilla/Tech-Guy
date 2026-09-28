using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Isolated visual fixture. Captures the actual OnGUI reward screen and build strip.</summary>
[InitializeOnLoad]
public static class RunRewardPreview
{
    private const string Pending = "TechGuy.RunRewardPreview";
    private static double _next;
    private static int _phase;
    private static RunBoons _run;
    static RunRewardPreview() { if (SessionState.GetBool(Pending, false)) Subscribe(); }
    public static void Run()
    {
        if (!Application.isBatchMode && !System.Array.Exists(System.Environment.GetCommandLineArgs(), arg => arg == "-rewardPreview"))
        { Debug.LogWarning("RunRewardPreview requires an explicit isolated preview launch."); return; }
        if (!Application.isBatchMode)
        {
            var gameView = EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));
            gameView.position = new Rect(50,50,1280,760); gameView.Show(); gameView.Focus();
        }
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Pending, true); Subscribe(); EditorApplication.EnterPlaymode();
    }
    private static void Subscribe()
    { EditorApplication.playModeStateChanged -= State; EditorApplication.playModeStateChanged += State; }
    private static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        var camera = new GameObject("Preview camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.07f,.09f,.12f);
        var go = new GameObject("Preview player"); go.SetActive(false);
        go.AddComponent<AbilityHolder>();
        var player = go.AddComponent<PlayerActor>(); player.handTransform = go.transform;
        player.health = 100; player.mana = 1000; go.SetActive(true);
        _run = go.AddComponent<RunBoons>();
        _phase = 0; _next = EditorApplication.timeSinceStartup + 3;
        EditorApplication.update += Tick;
    }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup < _next) return;
        if (_phase == 0)
        {
            _run.OfferReward(2);
            var acquired = (List<RunBoons.Offer>)typeof(RunBoons).GetField("_acquired", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_run);
            acquired.Add(new RunBoons.Offer("ignite", "Lâmina incandescente", ""));
            var choices = (List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_run);
            choices.Clear();
            choices.Add(new RunBoons.Offer("transform", "Nova de impacto", "TRANSFORMA Q: troca o avanço/estocada por uma explosão circular de 4m. Não gera Asura."));
            var definition = WeaponRunModifiers.Catalog[22];
            choices.Add(new RunBoons.Offer(definition.Id, definition.Title, definition.Description));
            choices.Add(new RunBoons.Offer("reactor", "Combustível instável", "Com fogo adquirido, sua queimadura ganha por segundo +12% do dano do acerto por cópia. Combine com críticos e descargas."));
            _phase = 1; _next = EditorApplication.timeSinceStartup + 2;
        }
        else if (_phase == 1)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../RewardCards.png")); _phase = 2; _next = EditorApplication.timeSinceStartup + 5;
        }
        else if (_phase == 2)
        {
            _run.Choose(1);
            var acquired = (List<RunBoons.Offer>)typeof(RunBoons).GetField("_acquired", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(_run);
            var definition = WeaponRunModifiers.Catalog[22];
            acquired.Add(new RunBoons.Offer(definition.Id, definition.Title, definition.Description));
            acquired.Add(new RunBoons.Offer("reactor", "Combustível instável", "Sua queimadura ganha +12% do dano do acerto por cópia."));
            _phase = 3; _next = EditorApplication.timeSinceStartup + 2;
        }
        else if (_phase == 3)
        {
            ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../RewardBuild.png"));
            _phase = 4; _next = EditorApplication.timeSinceStartup + 5;
        }
        else
        {
            bool captured = File.Exists(Path.Combine(Application.dataPath,"../RewardCards.png")) && File.Exists(Path.Combine(Application.dataPath,"../RewardBuild.png"));
            Debug.Log(captured ? "REWARD_PREVIEW_SUCCESS" : "REWARD_PREVIEW_FAILED");
            SessionState.SetBool(Pending, false); EditorApplication.update -= Tick; EditorApplication.Exit(captured ? 0 : 1);
        }
    }
}
