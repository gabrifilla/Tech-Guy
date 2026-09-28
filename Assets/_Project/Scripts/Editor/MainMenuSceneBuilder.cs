using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MainMenuSceneBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";

    [MenuItem("Tools/Tech Guy/Scenes/Create Main Menu")]
    public static void Build()
    {
        if (!File.Exists(ScenePath))
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camera = new GameObject("Menu camera").AddComponent<Camera>();
            camera.tag = "MainCamera"; camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.025f,.044f,.067f);
            camera.gameObject.AddComponent<AudioListener>();
            new GameObject("Main menu").AddComponent<MainMenuUI>();
            EditorSceneManager.SaveScene(scene,ScenePath);
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) }
            .Concat(EditorBuildSettings.scenes.Where(entry=>entry.path != ScenePath)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("MAIN_MENU_BUILD_SUCCESS: title screen is the entry scene.");
    }
}
