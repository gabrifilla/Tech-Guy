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
            Debug.Log("NexusLobby already exists. Open " + ScenePath + " to edit it.");
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
        BuildWorkshop(stations);
        BuildArchive(stations);
        BuildArrival();
        BuildBackground();
        SetupLighting();
        Camera camera = SetupCamera(player.transform);
        SetupPlayer(player, camera);
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
        EditorSceneManager.SaveScene(scene, ScenePath);
        var buildScenes = EditorBuildSettings.scenes.ToList();
        if (!buildScenes.Any(entry => entry.path == ScenePath)) buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();
        AssetDatabase.SaveAssets();
        CapturePreview(camera);
        Debug.Log("NEXUS_LOBBY_SUCCESS: scene saved, environment prefab saved, navigation to all stations validated.");
    }

    private static void CreateMaterials()
    {
        _metal = Material("Obsidian Alloy", new Color(0.055f, 0.08f, 0.15f), 0.65f);
        _floor = Material("Midnight Deck", new Color(0.11f, 0.16f, 0.24f), 0.35f);
        _panel = Material("Blue Steel", new Color(0.2f, 0.28f, 0.37f), 0.55f);
        _cyan = Material("Signal Cyan", new Color(0.03f, 0.84f, 1f), 0.15f, 2.5f);
        _pink = Material("Glitch Magenta", new Color(1f, 0.06f, 0.42f), 0.15f, 2f);
        _gold = Material("Gauntlet Amber", new Color(1f, 0.48f, 0.035f), 0.4f, 1.7f);
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
            destinationScene = unlocked ? "Playground" : "" });
    }

    private static void BuildWorkshop(List<LobbyInteraction.Station> stations)
    {
        Transform workshop = Group("04 - Neural gauntlet workbench", new Vector3(-13, 0, -3));
        Box(workshop, "Workbench", new Vector3(0, 0.7f, 0), new Vector3(6, 1.4f, 2.5f), _metal, true);
        Box(workshop, "Amber rim", new Vector3(0, 1.44f, 0), new Vector3(6.1f, 0.08f, 2.6f), _gold);
        Box(workshop, "Work surface", new Vector3(0, 1.51f, 0), new Vector3(5.7f, 0.08f, 2.3f), _panel);
        Shape(workshop, "Gauntlet mount", PrimitiveType.Cylinder, new Vector3(0, 1.75f, 0), new Vector3(1.8f, 0.2f, 1.8f), _metal);
        Transform gauntlet = new GameObject("Stylized neural gauntlet display").transform;
        gauntlet.SetParent(workshop, false);
        gauntlet.localPosition = new Vector3(0, 2.6f, 0);
        gauntlet.localRotation = Quaternion.Euler(-20, 0, -20);
        Box(gauntlet, "Armored cuff", Vector3.zero, new Vector3(0.9f, 0.6f, 1.1f), _panel);
        Box(gauntlet, "Neural core", new Vector3(0, 0.33f, 0), new Vector3(0.48f, 0.08f, 0.65f), _gold);
        for (int i = 0; i < 4; i++)
            Box(gauntlet, "Finger armor", new Vector3(-0.34f + i * 0.23f, 0, 0.78f), new Vector3(0.18f, 0.42f, 0.58f), _panel);
        Box(gauntlet, "Thumb armor", new Vector3(0.57f, -0.1f, 0.35f), new Vector3(0.35f, 0.32f, 0.45f), _panel);
        gauntlet.gameObject.AddComponent<LobbyCoreMotion>();
        Box(workshop, "Terminal screen", new Vector3(-2, 2.1f, 0.65f), new Vector3(1.1f, 0.7f, 0.12f), _cyan);
        Label(workshop, "NEURAL / LINK", new Vector3(0, 3.8f, 0), 0.23f, new Color(1, 0.7f, 0.2f));
        Label(workshop, "MANOPLA  //  PRIMEIRO CONTATO", new Vector3(0, 0.06f, -2.5f), 0.1f, Color.white, true);
        AddStation(workshop, stations, "MANOPLA / NEURAL LINK",
            "Uma arma, uma conexão... e uma voz. A manopla traduz impulsos nervosos em combate.\n\n\"Você programa. Eu cuido da parte em que a gente não morre.\"\n\nEstação de preparação — melhorias serão conectadas aqui.");
        for (int i = 0; i < 3; i++)
        {
            Vector3 p = new Vector3(-3.6f, 0.5f + i * 0.8f, 1.5f);
            Box(workshop, "Equipment case", p, new Vector3(1.2f, 0.75f, 1.1f), _panel, true);
            Box(workshop, "Case latch", p + new Vector3(0, 0, -0.56f), new Vector3(0.3f, 0.12f, 0.05f), _gold);
        }
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
        camera.orthographic = true;
        camera.orthographicSize = 12;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 180;
        camera.gameObject.AddComponent<AudioListener>();
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        var follow = camera.gameObject.AddComponent<TechGuy.Cameras.TG_TopDown_Camera>();
        follow.m_Target = player;
        var serialized = new SerializedObject(follow);
        serialized.FindProperty("m_Height").floatValue = 22;
        serialized.FindProperty("m_Distance").floatValue = 18;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        camera.transform.position = new Vector3(0, 22, -30);
        camera.transform.LookAt(new Vector3(0, 0, -12));
        return camera;
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
        PrefabUtility.SaveAsPrefabAsset(_environment.gameObject, "Assets/_Project/Prefabs/NexusEnvironment.prefab");
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        CapturePreview(Camera.main);
        Debug.Log("NEXUS_POLISH_SUCCESS");
    }

    public static void FinalizeAndValidate()
    {
        PolishAndPreview();
        LobbySceneValidation.Run();
    }

    private static void CapturePreview(Camera camera)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return;
        Vector3 position = camera.transform.position;
        Quaternion rotation = camera.transform.rotation;
        float size = camera.orthographicSize;
        var texture = new RenderTexture(1600, 1000, 24);
        var image = new Texture2D(1600, 1000, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        bool asyncCompilation = ShaderUtil.allowAsyncCompilation;
        try
        {
            ShaderUtil.allowAsyncCompilation = false;
            camera.transform.position = new Vector3(32, 43, -46);
            camera.transform.LookAt(new Vector3(0, 0, 1));
            camera.orthographicSize = 30;
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            image.ReadPixels(new Rect(0, 0, 1600, 1000), 0, 0);
            image.Apply();
            Directory.CreateDirectory("Docs");
            File.WriteAllBytes("Docs/NexusLobby-preview.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = null;
            RenderTexture.active = previous;
            camera.transform.SetPositionAndRotation(position, rotation);
            camera.orthographicSize = size;
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
