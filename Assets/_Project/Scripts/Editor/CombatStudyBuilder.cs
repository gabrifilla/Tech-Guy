using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class CombatStudyBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/CombatStudy.unity";
    private const string Art = "Assets/_Project/Art/CombatStudy";
    private const string Prefabs = "Assets/_Project/Prefabs/EnemyVariants";

    [MenuItem("Tools/Tech Guy/Combat/Create Combat Study")]
    public static void Create()
    {
        if (File.Exists(ScenePath))
        {
            if (!Application.isBatchMode) ProjectSceneMenu.OpenCombat();
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Folder(Art); Folder(Prefabs);
        EnemyVariantSetup.CreateExamples();
        var haste = Affix("Haste", "Acelerado", 1.35f, 1.2f, 1);
        var guard = Affix("Guard", "Resistente", 1, 1, 0.7f);
        var frost = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyFrostAura.prefab");
        Scene source = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Playground.unity");
        var roots = source.GetRootGameObjects();
        PlayerActor originalPlayer = roots.SelectMany(root => root.GetComponentsInChildren<PlayerActor>()).First();
        EnemyAI originalEnemy = roots.SelectMany(root => root.GetComponentsInChildren<EnemyAI>()).First();
        Camera originalCamera = roots.SelectMany(root => root.GetComponentsInChildren<Camera>()).First(camera => camera.CompareTag("MainCamera"));
        GameObject player = Object.Instantiate(originalPlayer.gameObject);
        GameObject cameraObject = Object.Instantiate(originalCamera.gameObject);
        GameObject template = Object.Instantiate(originalEnemy.gameObject);
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        foreach (GameObject obj in new[] { player, cameraObject, template }) SceneManager.MoveGameObjectToScene(obj, scene);
        SceneManager.SetActiveScene(scene);
        EditorSceneManager.CloseScene(source, true);
        Clean(player); Clean(template); Clean(cameraObject);
        player.name = "Player";
        player.transform.position = new Vector3(0, 0.1f, -8);
        player.GetComponent<PlayerActor>().healthBar = null;
        player.GetComponent<PlayerActor>().manaBar = null;
        if (!player.GetComponent<CombatReadabilityUI>()) player.AddComponent<CombatReadabilityUI>();
        Camera camera = cameraObject.GetComponent<Camera>();
        var follow = cameraObject.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
        follow.m_Target = player.transform;
        cameraObject.transform.position = new Vector3(0, 10, -18);
        cameraObject.transform.LookAt(player.transform.position);
        var controls = new SerializedObject(player.GetComponent<CharControlScript>());
        controls.FindProperty("mainCamera").objectReferenceValue = camera;
        controls.FindProperty("clickableLayers").intValue = 1 << LayerMask.NameToLayer("Ground");
        controls.FindProperty("clickEffect").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Click Effect.prefab").GetComponent<ParticleSystem>();
        controls.ApplyModifiedPropertiesWithoutUndo();

        Material deck = Mat("Deck", new Color(0.16f, 0.2f, 0.24f));
        Material steel = Mat("Brushed steel", new Color(0.3f, 0.36f, 0.4f));
        Material dark = Mat("Graphite", new Color(0.055f, 0.075f, 0.09f));
        Material signal = Mat("Amber ceramic", new Color(0.7f, 0.38f, 0.12f));
        var yard = new GameObject("Reference - Maintenance deck");
        Box(yard.transform, "Walkable foundation", new Vector3(0, -0.3f, 0), new Vector3(34, 0.6f, 30), dark, true);
        for (int x = -15; x <= 15; x += 3)
            for (int z = -12; z <= 12; z += 3)
                Box(yard.transform, "Inset floor panel", new Vector3(x, 0.01f, z), new Vector3(2.94f, 0.02f, 2.94f), deck);
        foreach (int side in new[] { -1, 1 })
        {
            Box(yard.transform, "Side barrier", new Vector3(side * 17, 0.8f, 0), new Vector3(0.4f, 1.6f, 30), dark, true);
            for (int i = -12; i <= 12; i += 4)
            {
                Box(yard.transform, "Structural rib", new Vector3(side * 16.6f, 1.1f, i), new Vector3(0.7f, 2.2f, 0.35f), steel, true);
                Box(yard.transform, "Amber cap", new Vector3(side * 16.5f, 2.24f, i), new Vector3(0.8f, 0.08f, 0.45f), signal);
            }
            Box(yard.transform, "End barrier", new Vector3(0, 0.8f, side * 15), new Vector3(34, 1.6f, 0.4f), dark, true);
        }
        // Detail stays at the perimeter so attack boundaries remain readable on the floor.
        for (int i = 0; i < 4; i++)
        {
            float x = -12 + i * 8;
            Box(yard.transform, "Equipment cabinet", new Vector3(x, 1.3f, 13), new Vector3(2.2f, 2.6f, 1.4f), dark, true);
            Box(yard.transform, "Cabinet face", new Vector3(x, 1.3f, 12.25f), new Vector3(1.95f, 2.3f, 0.12f), steel);
            for (int j = 0; j < 6; j++) Box(yard.transform, "Vent slat", new Vector3(x, 0.7f + j * 0.22f, 12.16f), new Vector3(1.3f, 0.07f, 0.04f), dark);
            Box(yard.transform, "Service latch", new Vector3(x + 0.73f, 1.5f, 12.12f), new Vector3(0.1f, 0.45f, 0.06f), signal);
        }
        var normal = Profile("Normal"); var magic = Profile("Magic"); var rare = Profile("Rare");
        var enemy = template.GetComponent<EnemyAI>();
        enemy.player = null;
        enemy.sightRange = 7; enemy.attackRange = 2.4f; enemy.attackDamage = 12; enemy.timeBetweenAttacks = 1.5f; enemy.walkPointRange = 0;
        template.GetComponent<Actor>().health = 60;
        template.GetComponent<Actor>().healthBar = null;
        if (!template.GetComponent<EnemyVariant>()) template.AddComponent<EnemyVariant>();
        if (!template.GetComponent<CombatReadabilityUI>()) template.AddComponent<CombatReadabilityUI>();
        SaveEnemy(template, "Normal", normal, Array.Empty<GameObject>());
        SaveEnemy(template, "Magic_Haste", magic, new[] { haste });
        SaveEnemy(template, "Rare_Frost", rare, new[] { frost });
        SaveEnemy(template, "Rare_Haste_Guard", rare, new[] { haste, guard });
        SaveEnemy(template, "Rare_Frost_Haste_Guard", rare, new[] { frost, haste, guard });
        string[] examples = { "Normal", "Magic_Haste", "Rare_Frost_Haste_Guard" };
        for (int i = 0; i < examples.Length; i++)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "/" + examples[i] + ".prefab"));
            instance.transform.position = new Vector3((i - 1) * 9, 0.1f, 7);
            instance.GetComponent<EnemyAI>().player = player.transform;
        }
        Object.DestroyImmediate(template);
        NavMeshSurface surface = yard.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        AssetDatabase.CreateAsset(surface.navMeshData, Art + "/Navigation.asset");
        if (!NavMesh.SamplePosition(player.transform.position, out _, 2, NavMesh.AllAreas)) throw new Exception("Combat study spawn is not walkable.");
        var light = new GameObject("Soft key").AddComponent<Light>();
        light.type = LightType.Directional; light.intensity = 1.5f; light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        RenderSettings.ambientLight = new Color(0.35f, 0.4f, 0.48f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.04f, 0.065f);
        PrefabUtility.SaveAsPrefabAsset(yard, "Assets/_Project/Prefabs/CombatReferenceDeck.prefab");
        SharedHudBuilder.InstallInScene(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        RefineLobby(steel, dark, signal);
        AssetDatabase.SaveAssets();
        Debug.Log("COMBAT_STUDY_CREATED: 5 enemy combinations, arena, refined workshop, saved navigation.");
    }

    private static EnemyProfile Profile(string name) => AssetDatabase.LoadAssetAtPath<EnemyProfile>("Assets/_Project/ScriptableObjects/Enemies/" + name + ".asset");
    public static void InstallFeedbackAndValidate()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Playground.unity");
        foreach (var player in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerActor>(true)))
            if (!player.GetComponent<CombatReadabilityUI>()) player.gameObject.AddComponent<CombatReadabilityUI>();
        EditorSceneManager.SaveScene(scene);
        CombatStudyValidation.Run();
    }
    private static void SaveEnemy(GameObject template, string name, EnemyProfile profile, GameObject[] affixes)
    {
        template.name = name;
        template.GetComponent<EnemyVariant>().Configure(profile, affixes);
        PrefabUtility.SaveAsPrefabAsset(template, Prefabs + "/" + name + ".prefab");
    }

    private static GameObject Affix(string name, string display, float movement, float attack, float damageTaken)
    {
        string path = Prefabs + "/Affix_" + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing) return existing;
        var obj = new GameObject(name);
        var so = new SerializedObject(obj.AddComponent<EnemyStatAffix>());
        so.FindProperty("_displayName").stringValue = display;
        so.FindProperty("_movementMultiplier").floatValue = movement;
        so.FindProperty("_attackSpeedMultiplier").floatValue = attack;
        so.FindProperty("_damageTakenMultiplier").floatValue = damageTaken;
        so.ApplyModifiedPropertiesWithoutUndo();
        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Object.DestroyImmediate(obj);
        return prefab;
    }

    private static void RefineLobby(Material steel, Material dark, Material signal)
    {
        Scene scene = EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath);
        var environment = scene.GetRootGameObjects().First(root => root.name == "NEXUS - Modular Environment");
        Transform workshop = environment.GetComponentsInChildren<Transform>().First(t => t.name == "04 - Neural gauntlet workbench");
        if (workshop.Find("Workshop detail")) return;
        var detail = new GameObject("Workshop detail").transform;
        detail.SetParent(workshop, false);
        for (int i = -2; i <= 2; i++)
        {
            Box(detail, "Inset drawer", new Vector3(i * 1.05f, 0.85f, -1.28f), new Vector3(0.96f, 0.7f, 0.08f), steel);
            Box(detail, "Drawer grip", new Vector3(i * 1.05f, 1, -1.37f), new Vector3(0.35f, 0.08f, 0.1f), dark);
        }
        foreach (int side in new[] { -1, 1 })
        {
            Box(detail, "Workbench frame", new Vector3(side * 2.8f, 0.8f, 0), new Vector3(0.25f, 1.7f, 2.6f), dark);
            Box(detail, "Tool rail", new Vector3(side * 2.7f, 2.2f, 1), new Vector3(0.16f, 1.5f, 0.18f), steel);
        }
        Box(detail, "Rear service rail", new Vector3(0, 2.9f, 1), new Vector3(5.5f, 0.16f, 0.18f), steel);
        for (int i = 0; i < 4; i++)
            Box(detail, "Tool module", new Vector3(0.7f + i * 0.35f, 1.68f, -0.65f), new Vector3(0.2f, 0.18f, 0.55f), i % 2 == 0 ? signal : steel);
        SaveWorkshopDetail(detail);
        EditorSceneManager.SaveScene(scene);
    }

    private static void SaveWorkshopDetail(Transform detail)
    {
        const string path = "Assets/_Project/Prefabs/NexusEnvironment.prefab";
        GameObject contents = PrefabUtility.LoadPrefabContents(path);
        try
        {
            Transform workshop = contents.GetComponentsInChildren<Transform>().First(t => t.name == "04 - Neural gauntlet workbench");
            if (!workshop.Find("Workshop detail"))
            {
                var copy = Object.Instantiate(detail.gameObject, workshop, false);
                copy.name = "Workshop detail";
            }
            PrefabUtility.SaveAsPrefabAsset(contents, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(contents); }
    }

    public static void FinalizeVisualReference()
    {
        Scene scene = EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath);
        Transform detail = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>()).First(t => t.name == "Workshop detail");
        SaveWorkshopDetail(detail);
        Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera"));
        camera.transform.position = new Vector3(-8, 5, -10);
        camera.transform.LookAt(new Vector3(-13, 1.5f, -3));
        var rt = new RenderTexture(1280, 800, 24);
        var image = new Texture2D(1280, 800, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            image.ReadPixels(new Rect(0, 0, 1280, 800), 0, 0); image.Apply();
            File.WriteAllBytes("Docs/WorkshopReference.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            Object.DestroyImmediate(image); rt.Release(); Object.DestroyImmediate(rt);
        }
        Debug.Log("WORKSHOP_REFERENCE_SAVED");
    }

    private static void Clean(GameObject root)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true)) GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
    }
    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/'); AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
    private static Material Mat(string name, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Smoothness", 0.25f);
        AssetDatabase.CreateAsset(mat, Art + "/" + name + ".mat"); return mat;
    }
    private static void Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material, bool solid = false)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name;
        obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        if (solid) obj.layer = LayerMask.NameToLayer("Ground"); else Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
}
