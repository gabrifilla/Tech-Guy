using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectSceneMenu
{
    [MenuItem("Tools/Tech Guy/Scenes/Open First Sector")]
    public static void OpenFirstSector() => OpenScene(FirstSectorBuilder.ScenePath);

    [MenuItem("Tools/Tech Guy/Scenes/Open Nexus Lobby")]
    public static void OpenLobby() => OpenScene(LobbySceneBuilder.ScenePath);

    [MenuItem("Tools/Tech Guy/Scenes/Open Combat Study")]
    public static void OpenCombat() => OpenScene(CombatStudyBuilder.ScenePath);

    [MenuItem("Tools/Tech Guy/Scenes/Open Playground")]
    public static void OpenPlayground() => OpenScene("Assets/_Project/Scenes/Playground.unity");

    [MenuItem("Tools/Tech Guy/Scenes/Open Nexus Lobby", true)]
    [MenuItem("Tools/Tech Guy/Scenes/Open Combat Study", true)]
    [MenuItem("Tools/Tech Guy/Scenes/Open Playground", true)]
    [MenuItem("Tools/Tech Guy/Scenes/Open First Sector", true)]
    private static bool CanOpenScene() => !EditorApplication.isPlayingOrWillChangePlaymode;

    private static void OpenScene(string path)
    {
        if (!CanOpenScene()) return;
        var scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
        if (!scene)
        {
            Debug.LogError("Scene not found: " + path);
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        EditorGUIUtility.PingObject(scene);
    }
}
