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

public static class TutorialSceneBuilder
{
    public const string ScenePath = "Assets/_Project/Scenes/PrologueTutorial.unity";
    public const string ArtPath = "Assets/_Project/Art/PrologueTutorial";
    [MenuItem("Tools/Tech Guy/Tutorial/Create Tutorial Scene")]
    public static void Build()
    {
        if (File.Exists(ScenePath)) { RegisterEntry(); Debug.Log("Tutorial exists; preserving scene edits."); return; }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(ArtPath); AssetDatabase.Refresh();
        Scene source = EditorSceneManager.OpenScene(CombatStudyBuilder.ScenePath);
        var roots = source.GetRootGameObjects();
        var player = Object.Instantiate(roots.SelectMany(r => r.GetComponentsInChildren<PlayerActor>()).Single()).gameObject;
        var camera = Object.Instantiate(roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera")));
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(player, scene); SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        SceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true);
        player.name = "Player - Calibration"; player.transform.SetPositionAndRotation(new Vector3(0,.1f,-10), Quaternion.identity);
        PlayerActor actor = player.GetComponent<PlayerActor>(); actor.healthBar = actor.manaBar = null;
        camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>().m_Target = player.transform;
        camera.transform.position = player.transform.position + new Vector3(0,10,-10); camera.transform.LookAt(player.transform);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.018f,.03f,.045f);
        var controls = new SerializedObject(player.GetComponent<CharControlScript>());
        controls.FindProperty("mainCamera").objectReferenceValue = camera;
        controls.FindProperty("debugClickLog").boolValue = false;
        controls.ApplyModifiedPropertiesWithoutUndo();

        Material deck = Material("Deck", new Color(.12f,.19f,.24f));
        Material dark = Material("Structure", new Color(.04f,.065f,.09f));
        Material steel = Material("Trim", new Color(.28f,.4f,.46f));
        Material cyan = Material("Signal", new Color(.08f,.75f,1f), true);
        Material gold = Material("Nexus gate", new Color(1f,.63f,.18f), true);
        var environment = new GameObject("Prologue - Calibration chamber").transform;
        Box(environment,"Walkable deck",new Vector3(0,-.3f,4),new Vector3(20,.6f,38),deck,true);
        for (int side = -1; side <= 1; side += 2)
        {
            Box(environment,"Side rail",new Vector3(side*10,.65f,4),new Vector3(.45f,1.3f,38),dark,true);
            for (int z=-10; z<=20; z+=6)
            {
                Box(environment,"Service column",new Vector3(side*9,1.3f,z),new Vector3(.9f,2.6f,.9f),steel,true);
                Box(environment,"Column light",new Vector3(side*8.52f,1.7f,z),new Vector3(.06f,.15f,.8f),cyan,false);
            }
        }
        Box(environment,"Arrival wall",new Vector3(0,.65f,-15),new Vector3(20,1.3f,.4f),dark,true);
        Box(environment,"Departure wall",new Vector3(0,.65f,23),new Vector3(20,1.3f,.4f),dark,true);
        for (int z=-12; z<22; z+=3)
        {
            Box(environment,"Floor joint",new Vector3(0,.01f,z),new Vector3(19,.02f,.035f),steel,false);
            Box(environment,"Route light",new Vector3(0,.035f,z),new Vector3(.3f,.03f,.7f),cyan,false);
        }
        Transform goal = new GameObject("Movement checkpoint").transform; goal.position = new Vector3(0,0,-4);
        Transform exit = new GameObject("Nexus destination").transform; exit.position = new Vector3(0,0,19);
        for (int side=-1; side<=1; side+=2)
            Box(environment,"Gate pylon",exit.position+new Vector3(side*2,2,0),new Vector3(.5f,4,.7f),steel,true);
        Box(environment,"Gate lintel",exit.position+Vector3.up*4,new Vector3(4.5f,.4f,.7f),steel,false);
        GameObject exitLight = Ring("Nexus gate online",exit.position,gold,1.5f);
        GameObject marker = Ring("Current tutorial objective",goal.position,cyan,1.3f);

        GameObject dummy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dummy.name = "Calibration target"; dummy.layer = LayerMask.NameToLayer("Character");
        dummy.transform.position = new Vector3(0,1,3); dummy.transform.localScale = new Vector3(1,1,1);
        dummy.GetComponent<Renderer>().sharedMaterial = gold;
        var target = dummy.AddComponent<TutorialTrainingTarget>(); target.health = 10000;
        Actor[] enemies = new Actor[2];
        var enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyVariants/Normal.prefab");
        for (int i=0; i<enemies.Length; i++)
        {
            var enemy = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab, scene);
            enemy.name = "Training drone " + (i+1); enemy.transform.position = new Vector3(i == 0 ? -3 : 3,.1f,10);
            enemies[i] = enemy.GetComponent<Actor>(); enemies[i].health = 60;
            var ai = enemy.GetComponent<EnemyAI>(); ai.player = player.transform; ai.attackDamage = 5;
            ai.sightRange = 25; ai.timeBetweenAttacks = 2.2f; ai.walkPointRange = 0;
            var drop = enemy.GetComponent<CoinDrop>() ?? enemy.AddComponent<CoinDrop>();
            var dropData = new SerializedObject(drop); dropData.FindProperty("_dropChance").floatValue = 0; dropData.ApplyModifiedPropertiesWithoutUndo();
            enemy.SetActive(false);
        }
        // Bake only the environment: actors and objective indicators never become static obstacles.
        var surface = environment.gameObject.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
        if (!surface.navMeshData) throw new InvalidOperationException("Tutorial navigation bake failed.");
        AssetDatabase.CreateAsset(surface.navMeshData,ArtPath+"/Navigation.asset");
        FirstSectorBuilder.ValidatePaths(player.transform.position,new[] { goal.position, exit.position, new Vector3(0,0,1), new Vector3(0,0,10) });
        var light = new GameObject("Tutorial key light").AddComponent<Light>(); light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(55,-25,0); light.intensity = 1.6f; light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.4f,.46f,.55f);
        var flow = new GameObject("Tutorial flow");
        var director = flow.AddComponent<TutorialDirector>();
        director.Configure(actor,Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]),target,enemies,goal,exit,marker,exitLight);
        flow.AddComponent<TutorialHUD>().Configure(director);
        if (!SharedHudBuilder.InstallInScene(scene)) throw new InvalidOperationException("Shared HUD is missing.");
        EditorSceneManager.SaveScene(scene,ScenePath); RegisterEntry(); AssetDatabase.SaveAssets();
        Debug.Log("TUTORIAL_BUILD_SUCCESS: seven steps, navigation, HUD and tutorial-first build order.");
    }

    [MenuItem("Tools/Tech Guy/Tutorial/Update Tutorial Content")]
    public static void UpdateContent()
    {
        if (!File.Exists(ScenePath)) { Build(); return; }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        TutorialDirector director = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<TutorialDirector>(true)).Single();
        director.RefreshBasicAttackLesson();
        EditorUtility.SetDirty(director);
        EditorSceneManager.SaveScene(scene);
        RegisterEntry();
        AssetDatabase.SaveAssets();
        Debug.Log("TUTORIAL_CONTENT_UPDATE_SUCCESS: Shift plus either mouse button teaches directional basic attacks.");
    }
    private static void RegisterEntry()
    {
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath,true) }
            .Concat(EditorBuildSettings.scenes.Where(entry => entry.path != ScenePath)).ToArray();
        if (File.Exists(MainMenuSceneBuilder.ScenePath)) MainMenuSceneBuilder.Build();
    }
    private static Material Material(string name, Color color, bool unlit = false)
    {
        var material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor",color); AssetDatabase.CreateAsset(material,ArtPath+"/"+name+".mat"); return material;
    }
    private static void Box(Transform parent,string name,Vector3 point,Vector3 scale,Material material,bool collider)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(parent,true);
        obj.transform.position = point; obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material;
        if (collider) obj.layer = LayerMask.NameToLayer("Ground"); else Object.DestroyImmediate(obj.GetComponent<Collider>());
    }
    private static GameObject Ring(string name,Vector3 point,Material material,float radius)
    {
        var root = new GameObject(name); root.transform.position = point + Vector3.up*.05f;
        for (int i=0;i<32;i++)
        {
            float angle = i*Mathf.PI*2/32;
            Box(root.transform,"Signal segment",root.transform.position+new Vector3(Mathf.Cos(angle)*radius,0,Mathf.Sin(angle)*radius),new Vector3(.2f,.06f,.2f),material,false);
        }
        return root;
    }
}
