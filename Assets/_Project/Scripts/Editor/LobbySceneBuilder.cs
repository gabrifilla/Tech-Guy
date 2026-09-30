using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Builds editable, local Unity assets. Existing lobby scenes are never overwritten.</summary>
public static class LobbySceneBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/NexusLobby.unity";
    private const string ArtPath = "Assets/_Project/Art/NexusLobby";
    private const string SourceScene = "Assets/_Project/Scenes/Playground.unity";
    private static Material _metal, _floor, _panel, _cyan, _pink, _gold, _white, _void;
    private static Transform _environment;

    [MenuItem("Tools/Tech Guy/Lobby/Create Nexus Lobby")]
    public static void Create()
    {
        if (File.Exists(ScenePath))
        {
            if (!Application.isBatchMode) ProjectSceneMenu.OpenLobby();
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EnsureFolder("Assets/_Project/Art");
        EnsureFolder(ArtPath);
        EnsureFolder(ArtPath + "/Materials");
        Scene source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Single);
        PlayerActor original = source.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<PlayerActor>(true)).FirstOrDefault();
        if (!original) throw new InvalidOperationException("Playground must contain the configured PlayerActor.");
        GameObject player = Object.Instantiate(original.gameObject);
        player.name = "Player - Nexus";
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(player, scene);
        SceneManager.SetActiveScene(scene);
        EditorSceneManager.CloseScene(source, true);
        BuildInto(scene, player);
        LobbyCompactLayout.Apply();
        Debug.Log("NEXUS_LOBBY_SUCCESS: scene saved, environment prefab saved, navigation to all stations validated.");
    }

    /// <summary>
    /// Regenerates the Nexus lobby <b>in place</b> when the scene already exists, so the new
    /// Hades-style arsenal (three pedestals + one training dummy) replaces an older structure without
    /// ever deleting <c>NexusLobby.unity</c> or <c>NexusEnvironment.prefab</c>. Because both files are
    /// overwritten (never deleted/recreated), their <c>.meta</c> GUIDs are preserved (Requisito 5.5 /
    /// Property 9): <see cref="EditorSceneManager.SaveScene(Scene,string)"/> rewrites the existing
    /// <c>.unity</c> contents and <see cref="PrefabUtility.SaveAsPrefabAsset(GameObject,string)"/>
    /// rewrites the existing prefab, both keeping the original asset identity.
    ///
    /// This is the entry point <see cref="LobbyLayoutValidation"/> uses to make the layout validation
    /// meaningful: the on-disk scene may still carry the removed <c>ARSENAL / ARMAS</c> panel, and the
    /// compact layout throws "Lobby group missing: 04 - Weapon pedestals" until the scene is rebuilt.
    /// </summary>
    [MenuItem("Tools/Tech Guy/Lobby/Rebuild Nexus Lobby (in place)")]
    public static void Rebuild()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(ScenePath))
        {
            Create();
            return;
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EnsureFolder("Assets/_Project/Art");
        EnsureFolder(ArtPath);
        EnsureFolder(ArtPath + "/Materials");

        // Clone the configured player from the existing lobby scene (kept in place), falling back to
        // the Playground source if the lobby somehow lacks one, so no external state is assumed.
        Scene existing = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        PlayerActor lobbyPlayer = existing.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<PlayerActor>(true)).FirstOrDefault();
        GameObject sourcePlayer = lobbyPlayer ? lobbyPlayer.gameObject : null;
        Scene playgroundSource = default;
        if (!sourcePlayer)
        {
            playgroundSource = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Additive);
            sourcePlayer = playgroundSource.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<PlayerActor>(true)).FirstOrDefault()?.gameObject;
            if (!sourcePlayer) throw new InvalidOperationException("No configured PlayerActor found in lobby or Playground.");
        }
        GameObject player = Object.Instantiate(sourcePlayer);
        player.name = "Player - Nexus";

        // Build the regenerated content in a scratch scene, then hand it to BuildInto which saves it
        // over the existing NexusLobby.unity path (preserving the GUID). The scratch scene is discarded.
        Scene scratch = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(player, scratch);
        SceneManager.SetActiveScene(scratch);
        if (playgroundSource.IsValid()) EditorSceneManager.CloseScene(playgroundSource, true);
        EditorSceneManager.CloseScene(existing, true);

        BuildInto(scratch, player);
        LobbyCompactLayout.Apply();
        Debug.Log("NEXUS_LOBBY_REBUILD_SUCCESS: lobby regenerated in place; scene and prefab GUIDs preserved.");
    }

    /// <summary>
    /// Shared construction body used by both <see cref="Create"/> (fresh scene) and
    /// <see cref="Rebuild"/> (in-place regeneration). It builds the full environment (platform, core,
    /// portals, archive, arsenal pedestals + training dummy, arrival, background, lighting, camera),
    /// bakes/saves the NavMesh, saves the environment prefab and the scene at <see cref="ScenePath"/>,
    /// and validates navigation to every station. Saving to the existing paths preserves GUIDs.
    /// </summary>
    private static void BuildInto(Scene scene, GameObject player)
    {
        player.transform.SetPositionAndRotation(new Vector3(0, 0.1f, -12), Quaternion.identity);
        PlayerActor actor = player.GetComponent<PlayerActor>();
        actor.healthBar = null;
        actor.manaBar = null;

        CreateMaterials();
        _environment = new GameObject("NEXUS - Modular Environment").transform;
        BuildPlatform();
        BuildCore();
        var stations = new List<LobbyInteraction.Station>();
        BuildPortal(new Vector3(0, 0, 16), _cyan, "01 / INCURSAO", "SANDBOX", stations, true);
        BuildPortal(new Vector3(-13, 0, 13), _pink, "02 / FRAGMENTO", "SEM SINAL", stations, false);
        BuildPortal(new Vector3(13, 0, 13), _gold, "03 / ORIGEM", "ACESSO NEGADO", stations, false);
        BuildArchive(stations);
        BuildArrival();
        BuildBackground();
        RefineSignage(_environment);
        SetupLighting();
        Camera camera = SetupCamera(player.transform);
        SetupPlayer(player, camera);
        // The arsenal replaces the single weapon-selection bench: three physical pedestals plus one
        // training dummy. It needs the player and lobby camera to inject into the LobbyArsenal
        // component (no scene lookups at runtime), so it is built after the camera exists.
        BuildArsenal(actor, camera);
        BuildTrainingDummy();
        new GameObject("Lobby Guide").AddComponent<LobbyInteraction>().Configure(player.transform, stations.ToArray());

        NavMeshSurface surface = _environment.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        if (!surface.navMeshData) throw new InvalidOperationException("Lobby NavMesh bake failed.");
        NavMeshData existingData = AssetDatabase.LoadAssetAtPath<NavMeshData>(ArtPath + "/NexusNavMesh.asset");
        if (existingData)
        {
            surface.RemoveData();
            EditorUtility.CopySerialized(surface.navMeshData, existingData);
            surface.navMeshData = existingData;
            surface.AddData();
            EditorUtility.SetDirty(existingData);
        }
        else AssetDatabase.CreateAsset(surface.navMeshData, ArtPath + "/NexusNavMesh.asset");
        ValidateNavigation(player.transform.position, stations);
        PrefabUtility.SaveAsPrefabAsset(_environment.gameObject, "Assets/_Project/Prefabs/NexusEnvironment.prefab");
        SharedHudBuilder.InstallInScene(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (!buildScenes.Any(entry => entry.path == ScenePath)) buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
    }

    private static void CreateMaterials()
    {
        _metal = Material("Obsidian Alloy", new Color(0.055f, 0.08f, 0.15f), 0.65f);
        _floor = Material("Midnight Deck", new Color(0.11f, 0.16f, 0.24f), 0.35f);
        _panel = Material("Blue Steel", new Color(0.2f, 0.28f, 0.37f), 0.55f);
        _cyan = Material("Signal Cyan", new Color(0.14f, 0.66f, 0.73f), 0.15f, 1.1f);
        _pink = Material("Glitch Magenta", new Color(0.6f, 0.2f, 0.38f), 0.15f, 1f);
        _gold = Material("Gauntlet Amber", new Color(0.85f, 0.55f, 0.22f), 0.4f, 1f);
        _white = Material("Interface White", new Color(0.65f, 0.89f, 1f), 0.1f, 1.2f);
        _void = Material("Distant Data", new Color(0.035f, 0.045f, 0.09f), 0.2f);
    }

    private static Material Material(string name, Color color, float metallic, float emission = 0)
    {
        string path = ArtPath + "/Materials/" + name + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool existing = mat;
        Shader shader = Shader.Find(emission > 0 ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (!shader) throw new InvalidOperationException("Required URP shader missing.");
        if (!mat) mat = new Material(shader) { name = name };
        mat.shader = shader;
        mat.SetColor("_BaseColor", color * Mathf.Max(1, emission));
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.55f);
        if (emission > 0)
        {
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            mat.DisableKeyword("_EMISSION");
        }
        if (!existing) AssetDatabase.CreateAsset(mat, path);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Transform Group(string name, Vector3 position)
    {
        var group = new GameObject(name).transform;
        group.SetParent(_environment, false);
        group.localPosition = position;
        return group;
    }

    private static GameObject Shape(Transform parent, string name, PrimitiveType shape, Vector3 position,
        Vector3 scale, Material material, bool solid = false)
    {
        GameObject part = GameObject.CreatePrimitive(shape);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        if (!solid) Object.DestroyImmediate(part.GetComponent<Collider>());
        else
        {
            part.layer = LayerMask.NameToLayer("Ground");
            // Unity's cylinder primitive has a capsule collider, unsuitable for flat decks.
            if (shape == PrimitiveType.Cylinder)
            {
                Object.DestroyImmediate(part.GetComponent<Collider>());
                part.AddComponent<MeshCollider>().sharedMesh = part.GetComponent<MeshFilter>().sharedMesh;
            }
        }
        return part;
    }

    private static GameObject Box(Transform parent, string name, Vector3 position, Vector3 scale, Material mat, bool solid = false)
        => Shape(parent, name, PrimitiveType.Cube, position, scale, mat, solid);

    private static void Ring(Transform parent, string name, Vector3 center, float radius, Material mat,
        float width = 0.08f, bool upright = false, int segments = 64)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.sharedMaterial = mat;
        line.startWidth = line.endWidth = width;
        line.numCornerVertices = 2;
        line.shadowCastingMode = ShadowCastingMode.Off;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            Vector3 point = upright ? new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) :
                new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            line.SetPosition(i, center + point * radius);
        }
    }

    private static void Label(Transform parent, string text, Vector3 position, float size, Color color, bool floor = false)
    {
        var go = new GameObject("Label - " + text);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        if (floor) go.transform.localRotation = Quaternion.Euler(90, 0, 0);
        var label = go.AddComponent<TextMesh>();
        label.text = text;
        label.fontSize = 80;
        label.characterSize = size;
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = color;
    }

    private static void BuildPlatform()
    {
        Transform deck = Group("01 - Suspended motherboard plaza", Vector3.zero);
        Shape(deck, "Foundation", PrimitiveType.Cylinder, new Vector3(0, -0.55f, 0), new Vector3(49, 0.55f, 49), _metal, true);
        Shape(deck, "Walkable deck", PrimitiveType.Cylinder, new Vector3(0, -0.08f, 0), new Vector3(47, 0.08f, 47), _metal, true);
        Ring(deck, "Outer cyan bus", new Vector3(0, 0.035f, 0), 23.2f, _cyan, 0.1f);
        Ring(deck, "Inner bus", new Vector3(0, 0.035f, 0), 8f, _cyan, 0.045f);
        Ring(deck, "Outer lower bus", new Vector3(0, -0.55f, 0), 24.55f, _pink, 0.12f);
        for (int i = 0; i < 24; i++)
        {
            float angle = i * 15f * Mathf.Deg2Rad;
            Vector3 position = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            GameObject rail = Box(deck, "Edge guard " + i, position * 23.7f + Vector3.up * 0.6f,
                new Vector3(5.9f, 1.2f, 0.4f), _metal, true);
            rail.transform.localRotation = Quaternion.Euler(0, i * 15f, 0);
            GameObject strip = Box(deck, "Edge light " + i, position * 23.7f + Vector3.up * 1.23f,
                new Vector3(4.8f, 0.055f, 0.42f), i % 3 == 0 ? _pink : _cyan);
            strip.transform.localRotation = rail.transform.localRotation;
        }
        for (int x = -20; x <= 20; x += 4)
            for (int z = -20; z <= 20; z += 4)
            {
                if (x * x + z * z > 440 || x * x + z * z < 35) continue;
                Box(deck, "Deck tile", new Vector3(x, 0.008f, z), new Vector3(3.92f, 0.012f, 3.92f),
                    _floor);
                Box(deck, "Circuit marker", new Vector3(x + 1.65f, 0.025f, z - 1.4f), new Vector3(0.12f, 0.015f, 0.6f), _panel);
            }
        foreach (float x in new[] { -2.5f, 2.5f })
        {
            Box(deck, "Entry route", new Vector3(x, 0.04f, -13), new Vector3(0.07f, 0.025f, 12), _cyan);
            Box(deck, "Expedition route", new Vector3(x, 0.04f, 12), new Vector3(0.07f, 0.025f, 8), _cyan);
        }
        Label(deck, "N E X U S", new Vector3(0, 0.07f, -8.8f), 0.32f, Color.white, true);
        Label(deck, "P O N T O   Z E R O", new Vector3(0, 0.07f, -10), 0.11f, Color.cyan, true);
    }

    private static void BuildCore()
    {
        Transform core = Group("02 - The system heart", Vector3.zero);
        Shape(core, "Reactor pedestal", PrimitiveType.Cylinder, new Vector3(0, 0.35f, 0), new Vector3(7, 0.35f, 7), _metal, true);
        Shape(core, "Reactor inset", PrimitiveType.Cylinder, new Vector3(0, 0.73f, 0), new Vector3(5.8f, 0.04f, 5.8f), _panel);
        Ring(core, "Containment ring", new Vector3(0, 0.85f, 0), 3.1f, _cyan, 0.12f);
        Ring(core, "Projection ring", new Vector3(0, 2.3f, 0), 2.2f, _cyan, 0.07f);
        Ring(core, "Projection halo", new Vector3(0, 4.5f, 0), 2.8f, _pink, 0.055f);
        GameObject crystal = Box(core, "Floating kernel", new Vector3(0, 3.1f, 0), Vector3.one * 1.8f, _cyan);
        crystal.transform.localRotation = Quaternion.Euler(35, 30, 45);
        crystal.AddComponent<LobbyCoreMotion>();
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI / 4;
            Vector3 p = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * 3;
            Box(core, "Containment fin", p + Vector3.up * 1.25f, new Vector3(0.35f, 1.3f, 0.35f), _panel);
            Box(core, "Fin beacon", p + Vector3.up * 1.94f, Vector3.one * 0.22f, _cyan);
        }
        Label(core, "KERNEL / ONLINE", new Vector3(0, 1, -3.6f), 0.13f, Color.cyan);
    }

    private static void BuildPortal(Vector3 position, Material signal, string title, string subtitle,
        List<LobbyInteraction.Station> stations, bool unlocked)
    {
        Transform portal = Group(title, position);
        Box(portal, "Gate foundation", new Vector3(0, 0.15f, 0), new Vector3(7.6f, 0.3f, 3), _panel, true);
        for (int i = 0; i < 12; i++)
        {
            float angle = i * 30f * Mathf.Deg2Rad;
            Vector3 center = new Vector3(Mathf.Sin(angle) * 2.9f, 3.1f + Mathf.Cos(angle) * 2.9f, 0);
            GameObject block = Box(portal, "Gate arch segment", center, new Vector3(1.42f, 0.55f, 0.85f), _metal, true);
            block.transform.localRotation = Quaternion.Euler(0, 0, -i * 30f);
        }
        Ring(portal, "Address ring", new Vector3(0, 3.1f, -0.48f), 2.55f, signal, 0.13f, true);
        Ring(portal, "Inner address ring", new Vector3(0, 3.1f, -0.49f), 2.3f, signal, 0.04f, true);
        for (int i = 0; i < 7; i++)
            Box(portal, "Signal scan", new Vector3(0, 1.1f + i * 0.58f, 0.06f),
                new Vector3(2.4f + (i % 3) * 0.35f, 0.025f, 0.025f), signal);
        Box(portal, "Destination screen", new Vector3(0, 6.8f, 0), new Vector3(7, 1, 0.25f), _metal);
        Label(portal, title, new Vector3(0, 6.85f, -0.17f), 0.2f, Color.white);
        Label(portal, subtitle, new Vector3(0, 0.055f, -3.3f), 0.14f, Color.white, true);
        Transform anchor = new GameObject("Interaction point").transform;
        anchor.SetParent(portal, false);
        anchor.localPosition = new Vector3(0, 0, -2.4f);
        stations.Add(new LobbyInteraction.Station { anchor = anchor, title = title,
            description = "Este endereço ainda não responde. Outros mundos serão conectados ao Nexus conforme a jornada avançar.",
            destinationScene = unlocked ? "FirstSector" : "" });
    }

    /// <summary>
    /// Builds the Hades-style arsenal: three physical weapon pedestals (0 Manopla, 1 Arco, 2 Lança),
    /// each with a decorative model/marker, a floor label and an "Interaction point" anchor, plus a
    /// <see cref="LobbyArsenal"/> component configured with the pedestals, the player and the lobby
    /// camera. Replaces the former single weapon-selection bench (Requisitos 1.1, 1.3, 1.4).
    /// </summary>
    private static void BuildArsenal(PlayerActor player, Camera camera)
    {
        Transform arsenal = Group("04 - Weapon pedestals", new Vector3(-13, 0, -3));
        var pedestals = new LobbyArsenal.Pedestal[3];
        // Weapon models are procedurally built and expressive per weapon; the emissive accent mirrors
        // the Nexus palette (cyan / amber / magenta) so the arsenal keeps the lobby's visual identity.
        pedestals[0] = BuildPedestal(arsenal, 0, new Vector3(-4.2f, 0, 0), "MANOPLA / NEURAL LINK", _gold, BuildGauntletModel);
        pedestals[1] = BuildPedestal(arsenal, 1, new Vector3(0f, 0, 0), "ARCO / DISPARO", _cyan, BuildBowModel);
        pedestals[2] = BuildPedestal(arsenal, 2, new Vector3(4.2f, 0, 0), "LANÇA / ESTOCADA", _pink, BuildSpearModel);
        Label(arsenal, "ARSENAL / ALTARES", new Vector3(0, 3.6f, 0), 0.16f, new Color(0.8f, 0.86f, 0.92f));

        var host = new GameObject("Lobby Arsenal");
        host.transform.SetParent(_environment, false);
        host.AddComponent<LobbyArsenal>().Configure(player, camera, pedestals);
    }

    /// <summary>
    /// Creates a single weapon pedestal (base, mount, spinning weapon model, floor label and an
    /// "Interaction point" anchor) and returns the serialized pedestal data for the arsenal.
    /// </summary>
    private static LobbyArsenal.Pedestal BuildPedestal(Transform parent, int weaponIndex, Vector3 position,
        string label, Material accent, Action<Transform, Material> buildModel)
    {
        Transform pedestal = new GameObject("Pedestal " + weaponIndex).transform;
        pedestal.SetParent(parent, false);
        pedestal.localPosition = position;

        Shape(pedestal, "Pedestal base", PrimitiveType.Cylinder, new Vector3(0, 0.15f, 0), new Vector3(1.9f, 0.15f, 1.9f), _metal, true);
        Shape(pedestal, "Pedestal column", PrimitiveType.Cylinder, new Vector3(0, 0.75f, 0), new Vector3(1.1f, 0.55f, 1.1f), _panel, true);
        Shape(pedestal, "Pedestal cap", PrimitiveType.Cylinder, new Vector3(0, 1.34f, 0), new Vector3(1.5f, 0.06f, 1.5f), _metal, true);
        Ring(pedestal, "Accent ring", new Vector3(0, 1.42f, 0), 0.82f, accent, 0.06f);
        Box(pedestal, "Accent beacon", new Vector3(0, 1.42f, 0), Vector3.one * 0.12f, accent);

        Transform display = new GameObject("Weapon display").transform;
        display.SetParent(pedestal, false);
        display.localPosition = new Vector3(0, 2.15f, 0);
        buildModel(display, accent);
        display.gameObject.AddComponent<LobbyCoreMotion>();

        Label(pedestal, label, new Vector3(0, 0.06f, -2.3f), 0.1f, Color.white, true);

        Transform anchor = new GameObject("Interaction point").transform;
        anchor.SetParent(pedestal, false);
        anchor.localPosition = new Vector3(0, 0, -2.4f);

        return new LobbyArsenal.Pedestal { anchor = anchor, weaponIndex = weaponIndex, proximityRange = 3.5f };
    }

    private static void BuildGauntletModel(Transform display, Material accent)
    {
        display.localRotation = Quaternion.Euler(-20, 0, -20);
        Box(display, "Armored cuff", Vector3.zero, new Vector3(0.9f, 0.6f, 1.1f), _panel);
        Box(display, "Neural core", new Vector3(0, 0.33f, 0), new Vector3(0.48f, 0.08f, 0.65f), accent);
        for (int i = 0; i < 4; i++)
            Box(display, "Finger armor", new Vector3(-0.34f + i * 0.23f, 0, 0.78f), new Vector3(0.18f, 0.42f, 0.58f), _panel);
        Box(display, "Thumb armor", new Vector3(0.57f, -0.1f, 0.35f), new Vector3(0.35f, 0.32f, 0.45f), _panel);
    }

    private static void BuildBowModel(Transform display, Material accent)
    {
        display.localRotation = Quaternion.Euler(0, 90, 0);
        // Curved limbs approximated by short segments, plus a thin bowstring, in the weapon's accent.
        Vector3 previous = new Vector3(0, -0.9f, 0);
        for (int i = 1; i <= 8; i++)
        {
            float angle = i / 8f * Mathf.PI;
            Vector3 next = new Vector3(0, -Mathf.Cos(angle) * 0.9f, Mathf.Sin(angle) * 0.42f);
            Segment(display, "Bow limb", previous, next, 0.06f, _panel);
            previous = next;
        }
        Segment(display, "Bowstring", new Vector3(0, -0.9f, 0), new Vector3(0, 0.9f, 0), 0.015f, accent);
    }

    private static void BuildSpearModel(Transform display, Material accent)
    {
        display.localRotation = Quaternion.Euler(35, 0, 0);
        Segment(display, "Spear shaft", new Vector3(0, -1.2f, 0), new Vector3(0, 1.1f, 0), 0.05f, _panel);
        GameObject blade = Box(display, "Spear blade", new Vector3(0, 1.35f, 0), new Vector3(0.16f, 0.5f, 0.05f), accent);
        blade.transform.localRotation = Quaternion.Euler(0, 0, 45);
    }

    /// <summary>Draws a capsule/cylinder segment between two local points (decorative, no collider).</summary>
    private static void Segment(Transform parent, string name, Vector3 from, Vector3 to, float radius, Material material)
    {
        var segment = Shape(parent, name, PrimitiveType.Cylinder, (from + to) * 0.5f,
            new Vector3(radius * 2f, Vector3.Distance(from, to) * 0.5f, radius * 2f), material);
        segment.transform.localRotation = Quaternion.FromToRotation(Vector3.up, to - from);
    }

    /// <summary>
    /// Creates exactly one passive <see cref="TrainingDummy"/> next to the pedestals so the player can
    /// test the equipped weapon's abilities. The dummy carries a solid capsule collider on the Default
    /// layer (player attacks resolve against any <see cref="Actor"/> via OverlapSphere), a high health
    /// pool for continuous use, and a small decorative stand (Requisitos 4.1).
    /// </summary>
    private static void BuildTrainingDummy()
    {
        Transform group = Group("08 - Training dummy", new Vector3(-13, 0, 3));
        Shape(group, "Dummy pad", PrimitiveType.Cylinder, new Vector3(0, 0.08f, 0), new Vector3(2.2f, 0.08f, 2.2f), _panel, true);
        Ring(group, "Dummy ring", new Vector3(0, 0.14f, 0), 1.05f, _cyan, 0.05f);

        var dummyObject = new GameObject("Training Dummy");
        dummyObject.transform.SetParent(group, false);
        dummyObject.transform.localPosition = new Vector3(0, 0, 0);
        // Configure serialized fields before the Actor.Awake runs at play time; a capsule body gives
        // the dummy a hittable volume without a special layer (combat uses DefaultRaycastLayers).
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Dummy body";
        body.transform.SetParent(dummyObject.transform, false);
        body.transform.localPosition = new Vector3(0, 1.1f, 0);
        body.transform.localScale = new Vector3(0.9f, 1.1f, 0.9f);
        body.GetComponent<Renderer>().sharedMaterial = _metal;
        // Keep the capsule collider (used to receive hits); it must NOT be on the Ground layer so it
        // is excluded from NavMesh geometry and never obstructs walking to the anchor.
        Box(dummyObject.transform, "Dummy head accent", new Vector3(0, 2.0f, 0), Vector3.one * 0.45f, _cyan);

        var dummy = dummyObject.AddComponent<TrainingDummy>();
        dummy.health = 1000f;

        Label(group, "TREINO / ALVO", new Vector3(0, 0.06f, -1.7f), 0.09f, Color.white, true);

        Transform anchor = new GameObject("Interaction point").transform;
        anchor.SetParent(group, false);
        anchor.localPosition = new Vector3(0, 0, -1.8f);
    }

    private static void BuildArchive(List<LobbyInteraction.Station> stations)
    {
        Transform archive = Group("05 - System archive", new Vector3(13, 0, -3));
        for (int i = 0; i < 3; i++)
        {
            float x = (i - 1) * 1.8f;
            Box(archive, "Memory rack", new Vector3(x, 1.7f, 1), new Vector3(1.4f, 3.4f, 1.5f), _metal, true);
            for (int j = 0; j < 6; j++)
            {
                Box(archive, "Memory blade", new Vector3(x, 0.4f + j * 0.48f, 0.2f), new Vector3(1.15f, 0.3f, 0.08f), _panel);
                Box(archive, "Memory status", new Vector3(x - 0.38f, 0.4f + j * 0.48f, 0.14f), new Vector3(0.12f, 0.1f, 0.04f), j % 3 == 0 ? _pink : _cyan);
            }
        }
        Box(archive, "Archive console", new Vector3(0, 0.75f, -0.8f), new Vector3(3.5f, 1.5f, 1.1f), _panel, true);
        GameObject screen = Box(archive, "Archive display", new Vector3(0, 1.55f, -0.8f), new Vector3(3.2f, 0.06f, 0.9f), _pink);
        screen.transform.localRotation = Quaternion.Euler(-15, 0, 0);
        Label(archive, "ARQUIVO / 404", new Vector3(0, 4.3f, 1), 0.23f, new Color(1, 0.2f, 0.6f));
        AddStation(archive, stations, "ARQUIVO / PERMISSÃO NEGADA",
            "Usuário: externo. Origem: Terra. Método de entrada: desconhecido.\n\nO sistema reconhece você, mas os registros foram apagados. Cada incursão pode recuperar um fragmento.\n\nComo escapar sem acesso ao código-fonte?");
    }

    private static void AddStation(Transform parent, List<LobbyInteraction.Station> stations, string title, string description)
    {
        Transform anchor = new GameObject("Interaction point").transform;
        anchor.SetParent(parent, false);
        anchor.localPosition = new Vector3(0, 0, -2.8f);
        stations.Add(new LobbyInteraction.Station { anchor = anchor, title = title, description = description });
    }

    private static void BuildArrival()
    {
        Transform arrival = Group("06 - Arrival and rest", new Vector3(0, 0, -17));
        Ring(arrival, "Reconstruction pad", new Vector3(0, 0.04f, 0), 2.4f, _cyan, 0.09f);
        Ring(arrival, "Pad inner band", new Vector3(0, 0.045f, 0), 2.1f, _panel, 0.05f);
        Label(arrival, "RECONEXAO", new Vector3(0, 0.06f, -3), 0.15f, Color.cyan, true);
        foreach (int side in new[] { -1, 1 })
        {
            Box(arrival, "Rest bench", new Vector3(side * 7, 0.55f, 1), new Vector3(4, 0.4f, 1.3f), _panel, true);
            Box(arrival, "Bench back", new Vector3(side * 7, 1.15f, 1.6f), new Vector3(4, 1, 0.2f), _metal, true);
            Box(arrival, "Bench light", new Vector3(side * 7, 0.3f, 0.4f), new Vector3(3.5f, 0.05f, 0.08f), _cyan);
        }
    }

    private static void BuildBackground()
    {
        Transform background = Group("07 - Data skyline", Vector3.zero);
        var random = new System.Random(404);
        for (int i = 0; i < 48; i++)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2;
            float radius = 33 + (float)random.NextDouble() * 35;
            float height = 4 + (float)random.NextDouble() * 19;
            Vector3 p = new Vector3(Mathf.Sin(angle) * radius, -10 + height / 2, Mathf.Cos(angle) * radius);
            Box(background, "Disconnected memory block", p, new Vector3(2.5f, height, 2.5f), _void);
            Box(background, "Data stream", p + new Vector3(-1.27f, 0, -1.27f), new Vector3(0.035f, height * 0.75f, 0.035f), i % 3 == 0 ? _pink : _cyan);
        }
        for (int i = 0; i < 35; i++)
        {
            Vector3 p = new Vector3((float)random.NextDouble() * 80 - 40, 9 + (float)random.NextDouble() * 15,
                26 + (float)random.NextDouble() * 25);
            GameObject bit = Box(background, "Floating packet", p, Vector3.one * 0.18f, i % 2 == 0 ? _cyan : _pink);
            bit.transform.localRotation = Quaternion.Euler(30, 45, 20);
        }
    }

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
        var volume = new GameObject("Nexus atmosphere").AddComponent<Volume>();
        volume.isGlobal = true;
        string profilePath = ArtPath + "/NexusAtmosphere.asset";
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
        if (!profile)
        {
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, profilePath);
        }
        profile.components.RemoveAll(component => !component);
        foreach (VolumeComponent component in profile.components.ToArray())
            Object.DestroyImmediate(component, true);
        profile.components.Clear();
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(0.35f);
        bloom.threshold.Override(1.05f);
        bloom.scatter.Override(0.6f);
        Vignette vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.2f);
        foreach (VolumeComponent component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);
        EditorUtility.SetDirty(profile);
        volume.sharedProfile = profile;
    }

    private static Camera SetupCamera(Transform player)
    {
        var camera = new GameObject("Nexus Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.015f, 0.022f, 0.055f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 180;
        camera.gameObject.AddComponent<AudioListener>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var follow = camera.gameObject.AddComponent<TechGuy.Cameras.TG_TopDown_Camera>();
        follow.m_Target = player;
        MatchPlaygroundCamera(camera, player);
        return camera;
    }

    private static void MatchPlaygroundCamera(Camera camera, Transform player)
    {
        Scene source = EditorSceneManager.OpenScene(SourceScene, OpenSceneMode.Additive);
        try
        {
            Camera reference = source.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>())
                .First(candidate => candidate.CompareTag("MainCamera"));
            var referenceFollow = reference.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
            if (!referenceFollow) throw new InvalidOperationException("Playground camera follow component missing.");
            camera.orthographic = reference.orthographic;
            camera.orthographicSize = reference.orthographicSize;
            camera.fieldOfView = reference.fieldOfView;
            camera.nearClipPlane = reference.nearClipPlane;
            camera.farClipPlane = reference.farClipPlane;
            camera.usePhysicalProperties = reference.usePhysicalProperties;
            camera.sensorSize = reference.sensorSize;
            camera.lensShift = reference.lensShift;
            camera.gateFit = reference.gateFit;
            var follow = camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
            follow.m_Target = player;
            var from = new SerializedObject(referenceFollow);
            var to = new SerializedObject(follow);
            foreach (string field in new[] { "m_Height", "m_Distance", "m_Angle" })
                to.FindProperty(field).floatValue = from.FindProperty(field).floatValue;
            to.ApplyModifiedPropertiesWithoutUndo();
            float height = from.FindProperty("m_Height").floatValue;
            float distance = from.FindProperty("m_Distance").floatValue;
            float angle = from.FindProperty("m_Angle").floatValue;
            Vector3 target = new Vector3(player.position.x, 0, player.position.z);
            camera.transform.position = target + Quaternion.AngleAxis(angle, Vector3.up) * new Vector3(0, height, -distance);
            camera.transform.LookAt(target);
            Debug.Log($"LOBBY_CAMERA_MATCH: height={height}, distance={distance}, angle={angle}, fov={camera.fieldOfView}, orthographic={camera.orthographic}");
        }
        finally { EditorSceneManager.CloseScene(source, true); }
    }

    private static void RefineSignage(Transform environment)
    {
        foreach (TextMesh label in environment.GetComponentsInChildren<TextMesh>())
        {
            label.characterSize = Mathf.Min(label.characterSize, label.text == "N E X U S" ? 0.08f : 0.04f);
            label.color = new Color(0.64f, 0.73f, 0.78f);
        }
    }

    private static void SetupPlayer(GameObject player, Camera camera)
    {
        foreach (Transform child in player.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        var controls = player.GetComponent<CharControlScript>();
        if (!controls || !player.GetComponent<NavMeshAgent>())
            throw new InvalidOperationException("Source player must have click movement and NavMeshAgent.");
        var serialized = new SerializedObject(controls);
        serialized.FindProperty("mainCamera").objectReferenceValue = camera;
        serialized.FindProperty("clickableLayers").intValue = 1 << LayerMask.NameToLayer("Ground");
        serialized.FindProperty("clickEffectPoolRoot").objectReferenceValue = null;
        serialized.FindProperty("clickEffect").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Click Effect.prefab").GetComponent<ParticleSystem>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidateNavigation(Vector3 spawn, List<LobbyInteraction.Station> stations)
    {
        if (!NavMesh.SamplePosition(spawn, out NavMeshHit start, 2, NavMesh.AllAreas))
            throw new InvalidOperationException("No NavMesh at player spawn.");
        foreach (var station in stations)
        {
            var path = new NavMeshPath();
            if (!NavMesh.SamplePosition(station.anchor.position, out NavMeshHit end, 2, NavMesh.AllAreas) ||
                !NavMesh.CalculatePath(start.position, end.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                throw new InvalidOperationException("Unreachable lobby station: " + station.title);
        }
    }

    public static void CapturePreview()
    {
        EditorSceneManager.OpenScene(ScenePath);
        CapturePreview(Camera.main);
    }

    public static void PolishAndPreview()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath);
        CreateMaterials();
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        _environment = scene.GetRootGameObjects().First(root => root.name == "NEXUS - Modular Environment").transform;
        foreach (Renderer renderer in _environment.GetComponentsInChildren<Renderer>())
        {
            if (renderer.name == "Deck tile") renderer.sharedMaterial = _floor;
            if (renderer.name == "Walkable deck") renderer.sharedMaterial = _metal;
        }
        RefineSignage(_environment);
        Camera camera = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Camera>())
            .First(candidate => candidate.CompareTag("MainCamera"));
        MatchPlaygroundCamera(camera, camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>().m_Target);
        PrefabUtility.SaveAsPrefabAsset(_environment.gameObject, "Assets/_Project/Prefabs/NexusEnvironment.prefab");
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        CapturePreview(camera);
        CapturePreview(camera, false);
        Debug.Log("NEXUS_POLISH_SUCCESS");
    }

    public static void FinalizeAndValidate()
    {
        PolishAndPreview();
        LobbySceneValidation.Run();
    }

    private static void CapturePreview(Camera camera, bool overview = true)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
        Vector3 position = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        float size = camera.orthographicSize;
        bool orthographic = camera.orthographic;
        var texture = new RenderTexture(1600, 1000, 24);
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            if (overview)
            {
                camera.orthographic = true;
                camera.transform.position = new Vector3(32, 43, -46);
                camera.transform.LookAt(new Vector3(0, 0, 1));
                camera.orthographicSize = 30;
            }
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Docs");
            File.WriteAllBytes(overview ? "Docs/NexusLobby-preview.png" : "Docs/NexusLobby-gameplay.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographicSize = size;
            camera.orthographic = orthographic;
            ShaderUtil.allowAsyncCompilation = asyncCompilation;
            Object.DestroyImmediate(image);
            texture.Release();
            Object.DestroyImmediate(texture);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }
}
