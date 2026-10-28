using System;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

/// <summary>Creates a NEW scene only. Never writes production scenes, prefabs or navigation assets.</summary>
public static class NexusBlockoutBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/NexusBlockout.unity";
    private const string NavPath = "Assets/_Project/Art/NexusLobby/NexusBlockoutNavMesh.asset";
    private static Transform _root;
    private static Material Mat(string name) => AssetDatabase.LoadAssetAtPath<Material>(
        "Assets/_Project/Art/NexusLobby/Materials/" + name + ".mat");

    [MenuItem("Tools/Tech Guy/Nexus Blockout/Create Isolated Scene")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) || AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath))
            throw new InvalidOperationException("Blockout already exists; edit the isolated scene directly.");
        var active = EditorSceneManager.GetActiveScene();
        if (active.isDirty) throw new InvalidOperationException("Save current scene before creating the blockout.");
        if (!AssetDatabase.CopyAsset("Assets/_Project/Scenes/NexusLobby.unity", ScenePath))
            throw new InvalidOperationException("Could not copy source scene.");
        BuildCopiedScene();
    }

    // Recovery entry for an interrupted first build. Requires the isolated source copy and no baked asset.
    public static void BuildCopiedScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.LoadAssetAtPath<NavMeshData>(NavPath))
            throw new InvalidOperationException("Build only an unbaked isolated copy in Edit Mode.");
        var scene = EditorSceneManager.OpenScene(ScenePath);
        Transform old = scene.GetRootGameObjects().Single(o => o.name == "NEXUS - Modular Environment").transform;
        // The copied scene may contain a prefab instance. Unpack this copy, never the source asset.
        if (PrefabUtility.IsPartOfPrefabInstance(old.gameObject))
            PrefabUtility.UnpackPrefabInstance(old.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        _root = new GameObject("NEXUS BLOCKOUT - Reconnection shelter").transform;
        var surface = _root.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        var sourceSurface = old.GetComponent<NavMeshSurface>();
        surface.agentTypeID = sourceSurface.agentTypeID;
        sourceSurface.RemoveData();

        // Shared construction language: service deck, low cutaway walls and open structural thresholds.
        Floor("01 Reconexao - receiving deck", 0, -4, 10, 10, "Midnight Deck");
        Floor("02 Oficina - service bay", -11, -4, 12, 10, "Blue Steel");
        Floor("03 Treino - test lane", -11, 8, 12, 14, "Midnight Deck");
        Floor("04 Travessia - uplink", 1, 7.5f, 12, 13, "Blue Steel");
        Wall("South cutaway", -6, -9, 22, .5f, true);
        Wall("West service spine", -17, 3, 24, .85f, false);
        Wall("Training backstop", -11, 15, 12, 2.1f, true);
        Wall("Uplink back", 1, 14, 12, 1.2f, true);
        Wall("East cutaway", 7, 7.5f, 13, .65f, false);
        Wall("Arrival east", 5, -4, 10, .65f, false);
        // Arrival <-> arsenal opening is four units; no gate or mandatory stop.
        Wall("Workshop partition south", -5, -7, 4, .8f, false);
        Wall("Workshop partition north", -5, 0, 2, .8f, false);
        Threshold("Oficina", new Vector3(-5, 0, -3), false, 4, "Gauntlet Amber");
        // Arrival <-> traverse opening, five units, directly ahead of spawn.
        Wall("Arrival lintel left", -3.5f, 1, 3, .85f, true);
        Wall("Arrival lintel right", 4.5f, 1, 1, .85f, true);
        Threshold("Travessia", new Vector3(1.5f, 0, 1), true, 5, "Signal Cyan");
        Wall("Training partition west", -15, 1, 4, .8f, true);
        Wall("Training partition east", -6.5f, 1, 3, .8f, true);
        Threshold("Treino", new Vector3(-10.5f, 0, 1), true, 5, "Glitch Magenta");
        Wall("Uplink partition south", -5, 3.5f, 5, .8f, false);
        Wall("Uplink partition north", -5, 12.5f, 5, .8f, false);
        Threshold("Travessia lateral", new Vector3(-5, 0, 8), false, 4, "Signal Cyan");
        // Lines connect real activities, rather than dressing empty floor with unrelated machines.
        Box("Direct uplink bus", new Vector3(2.9f, .018f, 0), new Vector3(.07f, .02f, 15), "Signal Cyan", false);
        Box("Workshop service bus", new Vector3(-7, .018f, -5), new Vector3(15, .02f, .06f), "Gauntlet Amber", false);
        for (int i = 0; i < 6; i++)
            Box("Training range mark " + i, new Vector3(-11, .02f, 3 + i * 1.6f), new Vector3(7, .02f, .04f), "Blue Steel", false);
        Label("RECONEXAO", new Vector3(.2f, .04f, -7.7f), .065f, true);
        Label("ORIGEM: TERRA  /  OBJETIVO: VOLTAR", new Vector3(.2f, .04f, -8.3f), .032f, true);
        Label("OFICINA", new Vector3(-11, .04f, -7.8f), .07f, true);
        Label("PREPARAR", new Vector3(-11, .04f, -8.4f), .035f, true);
        Label("TREINO", new Vector3(-9.2f, 1.9f, 12.5f), .07f, false);
        Label("EXPERIMENTAR O KIT", new Vector3(-9.2f, 1.35f, 12.5f), .032f, false);
        Label("TRAVESSIA", new Vector3(1.5f, .04f, 3.5f), .065f, true);
        Label("BUSCAR UM CAMINHO PARA CASA", new Vector3(1.5f, .04f, 2.8f), .03f, true);

        var arsenal = old.GetComponentInChildren<LobbyArsenal>();
        arsenal.transform.SetParent(_root, true);
        var ad = new SerializedObject(arsenal);
        ad.FindProperty("_compactPresentation").boolValue = true;
        var pedestals = ad.FindProperty("_pedestals");
        float[] zs = { -7f, -4f, -1f };
        for (int i = 0; i < pedestals.arraySize; i++)
        {
            var data = pedestals.GetArrayElementAtIndex(i);
            var anchor = (Transform)data.FindPropertyRelative("anchor").objectReferenceValue;
            var pedestal = anchor.parent;
            pedestal.SetParent(_root, false);
            pedestal.localScale = Vector3.one * .62f;
            pedestal.SetPositionAndRotation(new Vector3(-15f, 0, zs[i]), Quaternion.Euler(0, -90, 0));
            anchor.position = new Vector3(-13.3f, .06f, zs[i]);
            data.FindPropertyRelative("proximityRange").floatValue = 1.35f;
            var display = pedestal.Find("Weapon display");
            foreach (var motion in display.GetComponentsInChildren<LobbyCoreMotion>()) Object.DestroyImmediate(motion);
            display.rotation = Quaternion.Euler(0, i == 1 ? 90 : 0, 0); // Bow curve is authored in the YZ plane.
            // Display-only normalization. Hand prefabs, attack reach and hitboxes are never modified.
            Bounds bounds = BoundsOf(display);
            float target = new[] { .55f, 1.3f, 1.85f }[i];
            float longest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            display.localScale *= target / longest;
            bounds = BoundsOf(display);
            display.position += new Vector3(0, .97f - bounds.min.y, 0);
            foreach (var text in pedestal.GetComponentsInChildren<TextMesh>(true))
            { text.transform.rotation = Quaternion.identity; text.characterSize = text.text == "EQUIPADA" ? .03f : .045f; }
            var name = pedestal.GetComponentsInChildren<TextMesh>(true).First(t => t.text != "EQUIPADA");
            name.transform.position = new Vector3(-15f, .8f, zs[i] - .65f);
            var marker = (GameObject)data.FindPropertyRelative("equippedIndicator").objectReferenceValue;
            marker.transform.position = new Vector3(-15f, .48f, zs[i] - .65f);
        }
        ad.ApplyModifiedPropertiesWithoutUndo();
        var dummy = old.Find("08 - Training dummy");
        dummy.SetParent(_root, true); dummy.position = new Vector3(-11, 0, 10);
        foreach (var label in dummy.GetComponentsInChildren<TextMesh>()) label.gameObject.SetActive(false);
        var portal = old.Cast<Transform>().Single(t => t.name == "01 / INCURSAO");
        portal.SetParent(_root, true); portal.position = new Vector3(1.5f, 0, 10.5f);
        var archive = old.Find("05 - System archive");
        archive.SetParent(_root, true); archive.SetPositionAndRotation(new Vector3(-2.7f, 0, -7), Quaternion.Euler(0, 180, 0));
        archive.localScale = Vector3.one * .55f;
        foreach (var label in archive.GetComponentsInChildren<TextMesh>())
        { label.text = "ORIGEM / TERRA"; label.transform.rotation = Quaternion.identity; }
        var guide = Object.FindFirstObjectByType<LobbyInteraction>();
        guide.Configure(Object.FindFirstObjectByType<PlayerActor>().transform, new[] {
            new LobbyInteraction.Station { anchor = portal.Find("Interaction point"), title = "TRAVESSIA · First Sector",
                description = "Iniciar outra incursão.", destinationScene = "FirstSector" },
            new LobbyInteraction.Station { anchor = archive.Find("Interaction point"), title = "RECONEXAO · Origem: Terra",
                description = "Você está no universo digital. Seu objetivo é voltar à Terra.\nConsulte Último resultado para rever a tentativa; prepare sua arma e siga pela Travessia." }
        });
        var gd = new SerializedObject(guide);
        gd.FindProperty("_interactionRange").floatValue = 1.8f;
        gd.FindProperty("_blockoutGuide").boolValue = true; gd.ApplyModifiedPropertiesWithoutUndo();
        var hud = new SerializedObject(Object.FindFirstObjectByType<PlayerHUD>());
        hud.FindProperty("_lobbyTrainingAnchor").objectReferenceValue = dummy;
        hud.FindProperty("_lobbyTrainingRange").floatValue = 6.5f; hud.ApplyModifiedPropertiesWithoutUndo();
        Object.DestroyImmediate(old.gameObject);
        var player = Object.FindFirstObjectByType<PlayerActor>();
        player.transform.SetPositionAndRotation(new Vector3(1.5f, .1f, -6.3f), Quaternion.identity);
        var cam = Object.FindFirstObjectByType<Camera>();
        cam.fieldOfView = 40;
        cam.transform.position = new Vector3(1.5f, 10, -16.3f); cam.transform.LookAt(new Vector3(1.5f, 0, -6.3f));
        surface.BuildNavMesh();
        if (!surface.navMeshData) throw new InvalidOperationException("Blockout bake failed.");
        AssetDatabase.CreateAsset(surface.navMeshData, NavPath);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        if (!EditorBuildSettings.scenes.Any(s => s.path == ScenePath))
            EditorBuildSettings.scenes = EditorBuildSettings.scenes.Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) }).ToArray();
        Debug.Log("NEXUS_BLOCKOUT_CREATED: isolated scene + independent NavMesh; production assets preserved.");
    }

    private static Bounds BoundsOf(Transform t)
    {
        var renderers = t.GetComponentsInChildren<Renderer>();
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }
    private static void Floor(string name, float x, float z, float width, float depth, string material) =>
        Box(name, new Vector3(x, -.17f, z), new Vector3(width, .34f, depth), material, true);
    private static void Wall(string name, float x, float z, float length, float height, bool horizontal) =>
        Box(name, new Vector3(x, height / 2, z), new Vector3(horizontal ? length : .35f, height, horizontal ? .35f : length), "Obsidian Alloy", true);
    private static void Threshold(string name, Vector3 center, bool horizontal, float width, string material)
    {
        Vector3 axis = horizontal ? Vector3.right : Vector3.forward;
        foreach (int sign in new[] { -1, 1 })
        {
            Vector3 p = center + axis * (width / 2 * sign);
            Box(name + " door jamb", p + Vector3.up * .95f, new Vector3(.38f, 1.9f, .38f), "Blue Steel", true);
            Box(name + " threshold beacon", p + Vector3.up * 1.95f, new Vector3(.25f, .12f, .25f), material, false);
        }
        Box(name + " threshold", center + Vector3.up * .02f,
            new Vector3(horizontal ? width : .15f, .02f, horizontal ? .15f : width), material, false);
    }
    private static void Box(string name, Vector3 position, Vector3 size, string material, bool collider)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = name; go.transform.SetParent(_root);
        go.transform.position = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = Mat(material);
        if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
        else { int ground = LayerMask.NameToLayer("Ground"); if (ground >= 0) go.layer = ground; }
    }
    private static void Label(string text, Vector3 position, float size, bool floor)
    {
        var go = new GameObject("Sign - " + text); go.transform.SetParent(_root); go.transform.position = position;
        go.transform.rotation = floor ? Quaternion.Euler(90, 0, 0) : Quaternion.identity;
        var label = go.AddComponent<TextMesh>(); label.text = text; label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 80; label.characterSize = size; label.anchor = TextAnchor.MiddleCenter;
        label.color = new Color(.7f, .84f, .87f); go.GetComponent<Renderer>().sharedMaterial = label.font.material;
    }
}
