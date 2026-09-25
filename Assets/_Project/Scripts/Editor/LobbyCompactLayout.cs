using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Applies absolute transforms so repeating the layout never shrinks it again.</summary>
public static class LobbyCompactLayout
{
    private const string EnvironmentName = "NEXUS - Modular Environment";
    private const string PrefabPath = "Assets/_Project/Prefabs/NexusEnvironment.prefab";
    private const string NavigationPath = "Assets/_Project/Art/NexusLobby/NexusNavMesh.asset";
    private static readonly Vector3 Spawn = new Vector3(-3f, 0.1f, -8.5f);

    [MenuItem("Tools/Tech Guy/Lobby/Apply Compact Layout")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Scene scene = EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath);
        Transform environment = scene.GetRootGameObjects().Single(root => root.name == EnvironmentName).transform;
        ApplyEnvironment(environment);
        PlayerActor player = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerActor>()).Single();
        player.transform.SetPositionAndRotation(Spawn, Quaternion.identity);
        LobbyInteraction guide = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<LobbyInteraction>()).Single();
        var guideData = new SerializedObject(guide);
        guideData.FindProperty("_interactionRange").floatValue = 2.2f;
        guideData.ApplyModifiedPropertiesWithoutUndo();

        NavMeshSurface surface = environment.GetComponent<NavMeshSurface>();
        surface.BuildNavMesh();
        if (!surface.navMeshData) throw new InvalidOperationException("Compact lobby NavMesh bake failed.");
        NavMeshData saved = AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath);
        if (!saved) throw new InvalidOperationException("Existing lobby NavMesh asset is missing.");
        NavMeshData baked = surface.navMeshData;
        surface.RemoveData();
        EditorUtility.CopySerialized(baked, saved);
        surface.navMeshData = saved;
        surface.AddData();
        EditorUtility.SetDirty(saved);
        if (baked != saved) Object.DestroyImmediate(baked);
        ValidatePaths(environment, player.transform.position);

        GameObject prefab = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ApplyEnvironment(prefab.transform);
            prefab.GetComponent<NavMeshSurface>().navMeshData = saved;
            PrefabUtility.SaveAsPrefabAsset(prefab, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefab); }

        Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>())
            .Single(candidate => candidate.CompareTag("MainCamera"));
        PositionGameplayCamera(camera, player.transform.position);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Capture(camera, "Docs/NexusLobby-gameplay.png", false);
        Capture(camera, "Docs/NexusLobby-preview.png", true);
        Debug.Log("NEXUS_COMPACT_SUCCESS: scene, prefab, saved NavMesh and five station paths validated.");
    }

    private static void ApplyEnvironment(Transform root)
    {
        Place(root, "01 - Suspended motherboard plaza", Vector3.zero, new Vector3(0.72f, 0.85f, 0.68f));
        Place(root, "02 - The system heart", new Vector3(-1, 0, 5), Vector3.one * 0.62f);
        Place(root, "01 / INCURSAO", new Vector3(8.5f, 0, 7), Vector3.one * 0.76f);
        Place(root, "02 / FRAGMENTO", new Vector3(-9, 0, 7), Vector3.one * 0.62f);
        Place(root, "03 / ORIGEM", new Vector3(-3, 0, 10.5f), Vector3.one * 0.62f);
        Place(root, "04 - Neural gauntlet workbench", new Vector3(-8, 0, -1), Vector3.one * 0.78f);
        Place(root, "05 - System archive", new Vector3(8, 0, -2), Vector3.one * 0.72f);
        Place(root, "06 - Arrival and rest", new Vector3(-3, 0, -10), Vector3.one * 0.75f);
        Place(root, "07 - Data skyline", Vector3.zero, new Vector3(0.82f, 0.85f, 0.82f));

        Transform deck = root.Find("01 - Suspended motherboard plaza");
        Transform innerBus = deck.Find("Inner bus");
        innerBus.localPosition = new Vector3(-1f / 0.72f, 0, 5f / 0.68f);
        innerBus.localScale = Vector3.one * 0.45f;
        int entry = 0, exit = 0;
        foreach (Transform child in deck)
        {
            if (child.name == "Entry route")
                Route(child, -3f + (entry++ == 0 ? -1.7f : 1.7f), -5.7f, 6.6f);
            else if (child.name == "Expedition route")
                Route(child, 8.5f + (exit++ == 0 ? -1.7f : 1.7f), 3.4f, 5f);
            else if (child.name == "Label - N E X U S")
                child.localPosition = new Vector3(-3f / 0.72f, 0.07f, -5f / 0.68f);
            else if (child.name == "Label - P O N T O   Z E R O")
                child.localPosition = new Vector3(-3f / 0.72f, 0.07f, -5.8f / 0.68f);
        }
    }

    private static void Place(Transform root, string name, Vector3 position, Vector3 scale)
    {
        // Exact child names are part of the editor-generated scene structure, never runtime lookups.
        Transform child = root.Cast<Transform>().SingleOrDefault(candidate => candidate.name == name);
        if (!child) throw new InvalidOperationException("Lobby group missing: " + name);
        child.localPosition = position;
        child.localScale = scale;
    }

    private static void Route(Transform route, float x, float z, float length)
    {
        route.localPosition = new Vector3(x / 0.72f, 0.04f, z / 0.68f);
        route.localScale = new Vector3(0.05f / 0.72f, 0.025f, length / 0.68f);
    }

    private static void ValidatePaths(Transform environment, Vector3 spawn)
    {
        if (!NavMesh.SamplePosition(spawn, out NavMeshHit start, 0.5f, NavMesh.AllAreas))
            throw new InvalidOperationException("Compact lobby spawn is not on the NavMesh.");
        Transform[] anchors = environment.GetComponentsInChildren<Transform>()
            .Where(item => item.name == "Interaction point").ToArray();
        if (anchors.Length != 5) throw new InvalidOperationException("Expected five lobby stations.");
        foreach (Transform anchor in anchors)
        {
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(anchor.position, out NavMeshHit end, 0.6f, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Unreachable compact station: " + anchor.parent.name);
            Debug.Log("COMPACT_PATH_OK: " + anchor.parent.name);
        }
    }

    private static void PositionGameplayCamera(Camera camera, Vector3 playerPosition)
    {
        var data = new SerializedObject(camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>());
        float height = data.FindProperty("m_Height").floatValue;
        float distance = data.FindProperty("m_Distance").floatValue;
        float angle = data.FindProperty("m_Angle").floatValue;
        Vector3 target = new Vector3(playerPosition.x, 0, playerPosition.z);
        camera.transform.position = target + Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(0, height, -distance);
        camera.transform.LookAt(target);
    }

    private static void Capture(Camera camera, string path, bool overview)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
        Vector3 position = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        bool orthographic = camera.orthographic;
        float size = camera.orthographicSize;
        RenderTexture previous = RenderTexture.active;
        RenderTexture target = camera.targetTexture;
        bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
        var texture = new RenderTexture(1600, 1000, 24);
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            if (overview)
            {
                camera.orthographic = true;
                camera.orthographicSize = 21;
                camera.transform.position = new Vector3(23, 34, -36);
                camera.transform.LookAt(Vector3.zero);
            }
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Docs");
            File.WriteAllBytes(path, image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = target;
            RenderTexture.active = previous;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographic = orthographic;
            camera.orthographicSize = size;
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            Object.DestroyImmediate(image);
            texture.Release();
            Object.DestroyImmediate(texture);
        }
    }
}
