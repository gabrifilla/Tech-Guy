using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Captures real rendered scenes in an explicitly launched isolated Editor.</summary>
[InitializeOnLoad]
public static class TitleTutorialPreview
{
    private const string Pending = "TechGuy.TitlePreview";
    private static int _phase;
    private static double _next;
    static TitleTutorialPreview() { if (SessionState.GetBool(Pending,false)) Subscribe(); }
    public static void Run()
    {
        if (!Array.Exists(Environment.GetCommandLineArgs(),arg=>arg=="-titlePreview")) return;
        // Unity renders cyan placeholders while compiling new shader variants asynchronously.
        // Captures must wait for the real materials, especially after opening a new scene.
        ShaderUtil.allowAsyncCompilation = false;
        var view = EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        view.position = new Rect(40,40,1280,760); view.Show(); view.Focus();
        EditorSceneManager.OpenScene(MainMenuSceneBuilder.ScenePath);
        SessionState.SetBool(Pending,true); Subscribe(); EditorApplication.EnterPlaymode();
    }
    private static void Subscribe()
    { EditorApplication.playModeStateChanged -= State; EditorApplication.playModeStateChanged += State; }
    private static void State(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredPlayMode) return;
        _phase=0; _next=EditorApplication.timeSinceStartup+5; EditorApplication.update+=Tick;
    }
    private static MainMenuUI Menu() => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<MainMenuUI>()).Single();
    private static void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../"+name+".png"));
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup<_next || ShaderUtil.anythingCompiling) return;
        switch (_phase++)
        {
            case 0: Capture("MainMenuPreview"); break;
            case 1: Menu().Open(MainMenuUI.Page.Settings); break;
            case 2: Capture("SettingsPreview"); break;
            case 3: Menu().Open(MainMenuUI.Page.Controls); break;
            case 4: Capture("ControlsPreview"); break;
            case 5: Menu().StartJourney(true); break;
            case 6: Capture("TutorialPreview"); break;
            default:
                bool captured = new[] { "MainMenuPreview", "SettingsPreview", "ControlsPreview", "TutorialPreview" }.All(name=>File.Exists(Path.Combine(Application.dataPath,"../"+name+".png")));
                Debug.Log(captured ? "TITLE_PREVIEW_SUCCESS" : "TITLE_PREVIEW_FAILED");
                SessionState.SetBool(Pending,false); EditorApplication.update-=Tick; EditorApplication.Exit(captured ? 0 : 1); return;
        }
        _next=EditorApplication.timeSinceStartup+(_phase==6 ? 15 : 4);
    }
}
