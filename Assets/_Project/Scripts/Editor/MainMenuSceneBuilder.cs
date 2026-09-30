using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the entry Main Menu scene: a live, blurred Nexus diorama (Portal 2 style) behind the
/// existing IMGUI <see cref="MainMenuUI"/>. The scene file is never recreated once it exists, so its
/// GUID is preserved; the shared lobby atmosphere profile is reused (cloned, never mutated).
/// </summary>
public static class MainMenuSceneBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string EnvironmentPrefabPath = "Assets/_Project/Prefabs/NexusEnvironment.prefab";
    private const string LobbyAtmospherePath = "Assets/_Project/Art/NexusLobby/NexusAtmosphere.asset";
    private const string MenuArtPath = "Assets/_Project/Art/MainMenu";
    private const string MenuAtmospherePath = MenuArtPath + "/MainMenuAtmosphere.asset";

    [MenuItem("Tools/Tech Guy/Scenes/Create Main Menu")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        // Never recreate an existing scene file: opening and rewriting its contents preserves the
        // scene GUID and its .meta, per the project's asset rules.
        Scene scene = File.Exists(ScenePath)
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RemoveGeneratedBackdrop(scene);

        BuildDiorama();
        SetupLighting();
        Camera camera = SetupBackgroundCamera();
        SetupAtmosphere(camera);
        EnsureMenuUI(scene);

        EditorSceneManager.SaveScene(scene, ScenePath);

        // Preserve MainMenu.unity as the first (entry) scene in the build settings without touching
        // any other entries.
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) }
            .Concat(EditorBuildSettings.scenes.Where(entry => entry.path != ScenePath)).ToArray();
        AssetDatabase.SaveAssets();
        Debug.Log("MAIN_MENU_BUILD_SUCCESS: title screen is the entry scene with a live Nexus backdrop.");
    }

    /// <summary>
    /// Clears the generated backdrop (camera, lights, atmosphere volume, prior diorama) so a rebuild
    /// stays deterministic. The <see cref="MainMenuUI"/> host is left untouched and reused.
    /// </summary>
    private static void RemoveGeneratedBackdrop(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.GetComponent<MainMenuUI>()) continue;
            Object.DestroyImmediate(root);
        }
    }

    /// <summary>Ensures a single MainMenuUI host exists, reusing the existing one to keep its settings.</summary>
    private static void EnsureMenuUI(Scene scene)
    {
        bool hasMenu = scene.GetRootGameObjects().Any(root => root.GetComponent<MainMenuUI>());
        if (!hasMenu) new GameObject("Main menu").AddComponent<MainMenuUI>();
    }

    /// <summary>Instantiates the shared Nexus environment prefab as a static, non-interactive diorama.</summary>
    private static void BuildDiorama()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPrefabPath);
        if (!prefab)
        {
            Debug.LogWarning("MAIN_MENU_BUILD: NexusEnvironment prefab missing; menu will render over the camera background only.");
            return;
        }

        // Link an instance of the prefab (keeps the prefab GUID reference intact) and strip the
        // navigation/interaction plumbing the lobby needs but the menu diorama does not.
        var diorama = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        diorama.name = "Nexus Diorama";
        diorama.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        foreach (var surface in diorama.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true))
            Object.DestroyImmediate(surface);
    }

    /// <summary>Mirrors the cool key + magenta rim rig used by the lobby so the diorama reads the same.</summary>
    private static void SetupLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.32f, 0.39f, 0.55f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.015f, 0.022f, 0.055f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.008f;

        var key = new GameObject("Cool key light").AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(0.7f, 0.84f, 1f);
        key.intensity = 1.5f;
        key.shadows = LightShadows.Soft;
        key.transform.rotation = Quaternion.Euler(48, -30, 0);

        var fill = new GameObject("Magenta rim light").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.color = new Color(0.8f, 0.22f, 0.5f);
        fill.intensity = 0.6f;
        fill.transform.rotation = Quaternion.Euler(25, 130, 0);
    }

    /// <summary>Creates the dedicated background camera framed for a cinematic view of the Nexus.</summary>
    private static Camera SetupBackgroundCamera()
    {
        var camera = new GameObject("Menu camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.022f, 0.055f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 200f;
        camera.fieldOfView = 42f;
        camera.gameObject.AddComponent<AudioListener>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        // Low, angled cinematic framing looking across the deck toward the core (Portal 2 style).
        camera.transform.SetPositionAndRotation(new Vector3(9f, 5.5f, -20f), Quaternion.identity);
        camera.transform.LookAt(new Vector3(-2f, 2.5f, 6f));
        return camera;
    }

    /// <summary>
    /// Applies a global URP Volume (Bloom + Vignette + slight Gaussian blur) to the background camera,
    /// reusing the lobby atmosphere profile as a base. The lobby asset is cloned into a dedicated menu
    /// profile so the shared source is never mutated.
    /// </summary>
    private static void SetupAtmosphere(Camera camera)
    {
        EnsureFolder("Assets/_Project/Art");
        EnsureFolder(MenuArtPath);

        VolumeProfile profile = BuildMenuProfile();

        var volume = new GameObject("Nexus atmosphere").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
        volume.transform.SetParent(camera.transform, false);
    }

    private static VolumeProfile BuildMenuProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(MenuAtmospherePath);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, MenuAtmospherePath);
        }

        // Reset the profile so re-running the builder produces a deterministic effect stack.
        profile.components.RemoveAll(component => !component);
        foreach (VolumeComponent component in profile.components.ToArray())
            Object.DestroyImmediate(component, true);
        profile.components.Clear();

        // Reuse the lobby atmosphere values (Bloom + Vignette) as the base, then add the menu blur.
        var lobbyProfile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(LobbyAtmospherePath);
        var lobbyBloom = lobbyProfile ? lobbyProfile.components.OfType<Bloom>().FirstOrDefault() : null;
        var lobbyVignette = lobbyProfile ? lobbyProfile.components.OfType<Vignette>().FirstOrDefault() : null;

        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(lobbyBloom ? lobbyBloom.intensity.value : 0.35f);
        bloom.threshold.Override(lobbyBloom ? lobbyBloom.threshold.value : 1.05f);
        bloom.scatter.Override(lobbyBloom ? lobbyBloom.scatter.value : 0.6f);

        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(lobbyVignette ? lobbyVignette.intensity.value : 0.28f);

        // Gaussian depth of field gives the Portal 2 style soft, blurred backdrop behind the UI without
        // needing an accurate focus target.
        DepthOfField blur = profile.Add<DepthOfField>(true);
        blur.mode.Override(DepthOfFieldMode.Gaussian);
        blur.gaussianStart.Override(2f);
        blur.gaussianEnd.Override(30f);
        blur.gaussianMaxRadius.Override(1.2f);
        blur.highQualitySampling.Override(true);

        foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
