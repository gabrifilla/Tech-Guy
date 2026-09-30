using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class FirstSectorBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/FirstSector.unity";
    private const string Art = "Assets/_Project/Art/FirstSector";

    [MenuItem("Tools/Tech Guy/Scenes/Create First Sector")]
    public static void Build() => Build(false);

    [MenuItem("Tools/Tech Guy/Scenes/Rebuild First Sector (overwrite)")]
    public static void Rebuild()
    {
        // Explicit, destructive action: warn before discarding the existing generated scene.
        if (!Application.isBatchMode && File.Exists(ScenePath) &&
            !EditorUtility.DisplayDialog("Rebuild First Sector",
                "This regenerates FirstSector.unity from scratch and DISCARDS any manual edits made to that scene. " +
                "The scene's asset GUID is preserved so Build Settings stay intact.\n\nContinue?",
                "Rebuild", "Cancel"))
        {
            return;
        }
        Build(true);
    }

    private static void Build(bool overwrite)
    {
        if (File.Exists(ScenePath) && !overwrite) return;
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Art);
        AssetDatabase.Refresh();
        Scene source = EditorSceneManager.OpenScene(CombatStudyBuilder.ScenePath);
        var roots = source.GetRootGameObjects();
        var player = Object.Instantiate(roots.SelectMany(r => r.GetComponentsInChildren<PlayerActor>()).Single()).gameObject;
        var camera = Object.Instantiate(roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera")));
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(player, scene);
        SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        SceneManager.SetActiveScene(scene);
        EditorSceneManager.CloseScene(source, true);
        EnemyVariant[] prefabs = BuildArchetypePrefabs();
        player.name = "Player";
        player.transform.position = Vector3.zero;
        var actor = player.GetComponent<PlayerActor>();
        actor.healthBar = actor.manaBar = null;
        var follow = camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
        follow.m_Target = player.transform;
        // Pull the top-down camera back for combat readability (starting point; tuned in Play).
        var cameraData = new SerializedObject(follow);
        cameraData.FindProperty("m_Height").floatValue = 16f;
        cameraData.FindProperty("m_Distance").floatValue = 16f;
        cameraData.FindProperty("m_Angle").floatValue = 0f;
        cameraData.ApplyModifiedPropertiesWithoutUndo();
        var controls = new SerializedObject(player.GetComponent<CharControlScript>());
        controls.FindProperty("mainCamera").objectReferenceValue = camera;
        controls.ApplyModifiedPropertiesWithoutUndo();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.015f, .024f, .043f);

        var flow = new GameObject("First sector procedural flow");
        flow.AddComponent<SwarmAttackCoordinator>();
        var environment = flow.AddComponent<ProceduralStageEnvironment>();
        var seed = flow.AddComponent<RunSeedSource>();
        var seedData = new SerializedObject(seed);
        seedData.FindProperty("_randomizeEachRun").boolValue = true;
        seedData.FindProperty("_seed").stringValue = "1701";
        seedData.ApplyModifiedPropertiesWithoutUndo();
        var director = flow.AddComponent<ProgressionDirector>();
        var data = new SerializedObject(director);
        data.FindProperty("_player").objectReferenceValue = actor;
        data.FindProperty("_seedSource").objectReferenceValue = seed;
        data.FindProperty("_enemyPrefab").objectReferenceValue = prefabs.Single(p => p.ArchetypeId == ArchetypeId.Grunt).gameObject;
        data.FindProperty("_environment").objectReferenceValue = environment;
        data.FindProperty("_stageCount").intValue = 1;
        data.FindProperty("_requiredBossAccessFragments").intValue = 3;
        data.FindProperty("_returnScene").stringValue = "NexusLobby";
        SharedHudBuilder.InstallInScene(scene);
        data.FindProperty("_objective").objectReferenceValue = CreateObjective();
        var parameters = data.FindProperty("_generationParams");
        parameters.FindPropertyRelative("_minCombatRooms").intValue = 5;
        parameters.FindPropertyRelative("_maxCombatRooms").intValue = 5;
        parameters.FindPropertyRelative("_densityBudgetMin").intValue = 8;
        parameters.FindPropertyRelative("_densityBudgetMax").intValue = 16;
        parameters.FindPropertyRelative("_varietyTarget").intValue = 4;
        parameters.FindPropertyRelative("_roomSize").vector2Value = new Vector2(24, 24);
        parameters.FindPropertyRelative("_treasureProbability").floatValue = .5f;
        parameters.FindPropertyRelative("_secretProbability").floatValue = .3f;
        parameters.FindPropertyRelative("_generationRetryLimit").intValue = 50;
        var catalog = data.FindProperty("_archetypeCatalog");
        var prefabList = data.FindProperty("_archetypePrefabs");
        catalog.arraySize = prefabList.arraySize = prefabs.Length;
        for (int i = 0; i < prefabs.Length; i++)
        {
            catalog.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i].Archetype;
            prefabList.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        }
        data.ApplyModifiedPropertiesWithoutUndo();
        var visuals = new SerializedObject(environment);
        visuals.FindProperty("_director").objectReferenceValue = director;
        visuals.FindProperty("_floor").objectReferenceValue = Mat("Basalt alloy", new Color(.095f,.135f,.17f));
        visuals.FindProperty("_panels").objectReferenceValue = Mat("Floor panels", new Color(.18f,.23f,.27f));
        visuals.FindProperty("_walls").objectReferenceValue = Mat("Graphite", new Color(.035f,.048f,.065f));
        visuals.FindProperty("_cyan").objectReferenceValue = Mat("Guide cyan", new Color(.05f,.7f,.9f), true);
        visuals.FindProperty("_corruption").objectReferenceValue = Mat("Corrupted signal", new Color(.7f,.08f,.22f), true);
        visuals.FindProperty("_gold").objectReferenceValue = Mat("Extraction gold", new Color(1,.52f,.09f), true);
        visuals.ApplyModifiedPropertiesWithoutUndo();

        // Persist a representative layout for editing; runtime rebuilds it from the run's seed.
        var settings = (StageGenerationParams)typeof(ProgressionDirector).GetField("_generationParams",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(director);
        var graph = new StageGenerator().Generate(settings, 1701).Graph;
        var surface = environment.Build(graph);
        surface.navMeshData.name = "Navigation";
        string navigationPath = Art + "/Navigation.asset";
        var existingNavigation = AssetDatabase.LoadAssetAtPath<NavMeshData>(navigationPath);
        if (existingNavigation)
        {
            EditorUtility.CopySerialized(surface.navMeshData, existingNavigation);
            surface.RemoveData();
            Object.DestroyImmediate(surface.navMeshData);
            surface.navMeshData = existingNavigation;
            surface.AddData();
            EditorUtility.SetDirty(existingNavigation);
        }
        else AssetDatabase.CreateAsset(surface.navMeshData, navigationPath);
        var light = new GameObject("Cool overhead light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(52, -32, 0);
        light.intensity = 1.6f;
        light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.32f, .39f, .48f);
        ValidatePaths(Vector3.zero, graph.Rooms.Select(r => new Vector3(r.Center.x, 0, r.Center.y)).ToArray());
        CaptureOverview(camera, graph);
        EditorSceneManager.SaveScene(scene, ScenePath);
        var paths = EditorBuildSettings.scenes.ToList();
        if (paths.All(s => s.path != ScenePath)) paths.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = paths.ToArray();
        ConnectLobby();
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("FIRST_SECTOR_BUILD_SUCCESS: procedural stage, 5 distinct combat arenas, 3-fragment boss seal, guardian and 16 archetypes.");
    }

    private static EnemyVariant[] BuildArchetypePrefabs()
    {
        const string folder = "Assets/_Project/Prefabs/EnemyArchetypes";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var archetypes = AssetDatabase.FindAssets("t:EnemyArchetype", new[] { "Assets/_Project/ScriptableObjects/Enemies/Archetypes" })
            .Select(g => AssetDatabase.LoadAssetAtPath<EnemyArchetype>(AssetDatabase.GUIDToAssetPath(g)))
            .OrderBy(a => a.Id).ToArray();
        if (archetypes.Length != 16 || archetypes.Select(a => a.Id).Distinct().Count() != 16 || archetypes.Any(a => !a.Profile))
            throw new InvalidOperationException("Expected exactly 16 unique, valid enemy archetypes.");
        var source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyVariants/Normal.prefab");
        var hazardObject = new GameObject("Corrupted memory hazard");
        var hazardZone = hazardObject.AddComponent<HazardZone>();
        var body = hazardObject.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        const string hazardPath = "Assets/_Project/Prefabs/FirstSectorHazard.prefab";
        var hazardPrefab = PrefabUtility.SaveAsPrefabAsset(hazardObject, hazardPath).GetComponent<HazardZone>();
        Object.DestroyImmediate(hazardObject);
        var result = new System.Collections.Generic.List<EnemyVariant>();
        // Swarm is authored before Spawner so its prefab dependency is serialized before Awake.
        GameObject swarmPrefab = null;
        foreach (var archetype in archetypes.OrderBy(a => a.Id == ArchetypeId.Swarm ? 0 : a.Id == ArchetypeId.Spawner ? 2 : 1))
        {
            var instance = Object.Instantiate(source);
            instance.name = archetype.Id.ToString();
            var variant = instance.GetComponent<EnemyVariant>();
            variant.Configure(archetype);
            if (!instance.GetComponent<EnemyCombatActions>()) instance.AddComponent<EnemyCombatActions>();
            System.Type behavior = archetype.Id switch
            {
                ArchetypeId.Healer => typeof(HealerBehavior),
                ArchetypeId.ShieldSupport => typeof(ShieldSupportBehavior),
                ArchetypeId.Spawner => typeof(SpawnerBehavior),
                ArchetypeId.HazardCaster => typeof(HazardCasterBehavior),
                ArchetypeId.Mirror => typeof(FrontalReflector),
                _ => null
            };
            if (behavior != null && !instance.GetComponent(behavior)) instance.AddComponent(behavior);
            if (archetype.IsPriorityTarget && !instance.GetComponent<PriorityTargetMarker>()) instance.AddComponent<PriorityTargetMarker>();
            if (archetype.Id == ArchetypeId.Spawner)
            {
                var spawner = new SerializedObject(instance.GetComponent<SpawnerBehavior>());
                spawner.FindProperty("_swarmPrefab").objectReferenceValue = swarmPrefab;
                spawner.FindProperty("_livingCap").intValue = 4;
                spawner.FindProperty("_spawnInterval").floatValue = 5f;
                spawner.ApplyModifiedPropertiesWithoutUndo();
            }
            if (archetype.Id == ArchetypeId.HazardCaster)
            {
                var caster = new SerializedObject(instance.GetComponent<HazardCasterBehavior>());
                caster.FindProperty("_zonePrefab").objectReferenceValue = hazardPrefab;
                caster.ApplyModifiedPropertiesWithoutUndo();
            }
            var prefab = PrefabUtility.SaveAsPrefabAsset(instance, folder + "/" + archetype.Id + ".prefab");
            if (archetype.Id == ArchetypeId.Swarm) swarmPrefab = prefab;
            result.Add(prefab.GetComponent<EnemyVariant>());
            Object.DestroyImmediate(instance);
        }
        AssetDatabase.SaveAssets();
        return archetypes.Select(a => AssetDatabase.LoadAssetAtPath<GameObject>(folder + "/" + a.Id + ".prefab")
            .GetComponent<EnemyVariant>()).ToArray();
    }

    public static void ConnectLobby()
    {
        var scene=EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath);
        var roots=scene.GetRootGameObjects();
        var player=roots.SelectMany(r=>r.GetComponentsInChildren<PlayerActor>()).Single();
        var guide=roots.SelectMany(r=>r.GetComponentsInChildren<LobbyInteraction>()).Single();
        var data=new SerializedObject(guide);var stations=data.FindProperty("_stations");
        for(int i=0;i<stations.arraySize;i++)
        {
            var station=stations.GetArrayElementAtIndex(i);var destination=station.FindPropertyRelative("destinationScene");
            if(string.IsNullOrEmpty(destination.stringValue))continue;
            destination.stringValue="FirstSector";
            station.FindPropertyRelative("description").stringValue="Setor 01: Memoria Corrompida. Explore cinco salas de combate, recupere memorias e derrote o guardiao para retornar ao Nexus.";
            var anchor=(Transform)station.FindPropertyRelative("anchor").objectReferenceValue;
            var portal=anchor.parent.GetComponent<ScenePortal>()??anchor.parent.gameObject.AddComponent<ScenePortal>();
            portal.Configure(player.transform,"FirstSector");
        }
        data.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(scene);
    }
    private static TMP_Text CreateObjective()
    {
        var canvas=new GameObject("Sector objective",typeof(Canvas),typeof(CanvasScaler));
        canvas.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;canvas.GetComponent<Canvas>().sortingOrder=30;
        var scaler=canvas.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        var obj=new GameObject("Objective",typeof(RectTransform),typeof(TextMeshProUGUI));obj.transform.SetParent(canvas.transform,false);
        var rect=obj.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);
        rect.anchoredPosition=new Vector2(0,-28);rect.sizeDelta=new Vector2(950,90);
        var text=obj.GetComponent<TextMeshProUGUI>();text.font=TMP_Settings.defaultFontAsset;text.fontSize=24;
        text.alignment=TextAlignmentOptions.Center;text.color=new Color(.9f,.8f,.59f);text.raycastTarget=false;
        text.text="SETOR 01 / MEMORIA CORROMPIDA\nExplore as salas | E: procurar memoria oculta";return text;
    }
    private static void Floor(Transform parent,Vector3 center,Vector2 size,Material material)=>Box(parent,"Walkable deck",center+Vector3.down*.3f,new Vector3(size.x,.6f,size.y),material,true);
    private static void Box(Transform parent,string name,Vector3 position,Vector3 scale,Material material,bool collider)
    {
        var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.transform.SetParent(parent,true);
        obj.transform.position=position;obj.transform.localScale=scale;obj.GetComponent<Renderer>().sharedMaterial=material;
        if(collider)obj.layer=LayerMask.NameToLayer("Ground");else Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    private static Material Mat(string name,Color color,bool emissive=false)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(Art+"/"+name+".mat");
        if (existing) return existing;
        var material=new Material(Shader.Find(emissive?"Universal Render Pipeline/Unlit":"Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor",color);AssetDatabase.CreateAsset(material,Art+"/"+name+".mat");return material;
    }
    private static void Label(Transform parent,string value,Vector3 position,float size,Color color)
    {
        var obj=new GameObject("Deck number");obj.transform.SetParent(parent);obj.transform.position=position;obj.transform.rotation=Quaternion.Euler(90,0,0);
        var label=obj.AddComponent<TextMeshPro>();label.font=TMP_Settings.defaultFontAsset;label.text=value;label.fontSize=size;
        label.alignment=TextAlignmentOptions.Center;label.color=color;label.rectTransform.sizeDelta=new Vector2(5,3);
    }
    public static void ValidatePaths(Vector3 start,Vector3[] targets)
    {
        if(!NavMesh.SamplePosition(start,out var source,2,NavMesh.AllAreas))throw new Exception("First sector spawn is not walkable");
        foreach(var target in targets)
        {
            var path=new NavMeshPath();
            if(!NavMesh.SamplePosition(target,out var end,2,NavMesh.AllAreas)||!NavMesh.CalculatePath(source.position,end.position,NavMesh.AllAreas,path)||path.status!=NavMeshPathStatus.PathComplete)
                throw new Exception("First sector unreachable point: "+target);
        }
        Debug.Log("FIRST_SECTOR_NAVIGATION_SUCCESS: "+targets.Length+" paths");
    }
    private static void CaptureOverview(Camera gameplayCamera, RoomGraph graph)
    {
        ShaderUtil.allowAsyncCompilation=false;
        float minX = graph.Rooms.Min(r => r.Center.x - r.Size.x * .5f);
        float maxX = graph.Rooms.Max(r => r.Center.x + r.Size.x * .5f);
        float minZ = graph.Rooms.Min(r => r.Center.y - r.Size.y * .5f);
        float maxZ = graph.Rooms.Max(r => r.Center.y + r.Size.y * .5f);
        var camera = Object.Instantiate(gameplayCamera);
        camera.name = "First sector overview camera";
        var follow = camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
        if (follow) follow.enabled = false;
        var listener = camera.GetComponent<AudioListener>();
        if (listener) listener.enabled = false;
        camera.orthographic=true;
        camera.orthographicSize=Mathf.Max(maxX-minX,maxZ-minZ)*.58f;
        camera.transform.position=new Vector3((minX+maxX)*.5f,80,(minZ+maxZ)*.5f);
        camera.transform.rotation=Quaternion.Euler(90,0,0);
        var target=new RenderTexture(1400,1400,24);camera.targetTexture=target;camera.Render();
        var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1400,1400,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1400,1400),0,0);image.Apply();Directory.CreateDirectory("Docs");File.WriteAllBytes("Docs/FirstSector-overview.png",image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=old;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(camera.gameObject);
    }
}
