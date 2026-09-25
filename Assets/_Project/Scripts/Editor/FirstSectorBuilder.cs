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
        if (File.Exists(ScenePath))
        {
            if (!overwrite) { Debug.Log("First sector already exists; preserving scene edits. Use 'Rebuild First Sector (overwrite)' to regenerate."); return; }
            // Delete only the .unity file, keeping its .meta so the scene keeps its GUID and stays wired
            // in Build Settings. Intentionally NOT calling AssetDatabase.Refresh here: that would let Unity
            // clean up the now-orphaned .meta. SaveScene below rewrites the .unity at the same path and
            // Unity re-associates it with the preserved .meta.
            File.Delete(ScenePath);
        }
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Art); AssetDatabase.Refresh();
        Scene source = EditorSceneManager.OpenScene(CombatStudyBuilder.ScenePath);
        var roots = source.GetRootGameObjects();
        var player = Object.Instantiate(roots.SelectMany(r => r.GetComponentsInChildren<PlayerActor>()).Single()).gameObject;
        var camera = Object.Instantiate(roots.SelectMany(r => r.GetComponentsInChildren<Camera>()).First(c => c.CompareTag("MainCamera")));
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(player, scene); SceneManager.MoveGameObjectToScene(camera.gameObject, scene);
        SceneManager.SetActiveScene(scene); EditorSceneManager.CloseScene(source, true);
        player.name = "Player"; player.transform.position = new Vector3(0,.1f,-14);
        var actor = player.GetComponent<PlayerActor>(); actor.healthBar = actor.manaBar = null;
        var follow = camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>(); follow.m_Target = player.transform;
        var controls = new SerializedObject(player.GetComponent<CharControlScript>());
        controls.FindProperty("mainCamera").objectReferenceValue = camera;
        controls.ApplyModifiedPropertiesWithoutUndo();
        camera.transform.position = player.transform.position + new Vector3(0,14,-12); camera.transform.LookAt(player.transform);
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.015f,.024f,.043f);

        Material floor = Mat("Basalt alloy",new Color(.095f,.135f,.17f));
        Material panels = Mat("Floor panels",new Color(.18f,.23f,.27f));
        Material dark = Mat("Graphite",new Color(.035f,.048f,.065f));
        Material trim = Mat("Steel",new Color(.29f,.35f,.4f));
        Material cyan = Mat("Guide cyan",new Color(.05f,.7f,.9f),true);
        Material amber = Mat("Extraction gold",new Color(1,.52f,.09f),true);
        Material corruption = Mat("Corrupted signal",new Color(.7f,.08f,.22f),true);
        var environment = new GameObject("Sector 01 - Broken memory bridge").transform;
        // Six rooms marching down +Z: five escalating fights then the guardian. Rooms alternate in X
        // for variety; corridors between them are generated so the whole sector is one navmesh island.
        Vector3[] centers =
        {
            new Vector3(0,0,0), new Vector3(6,0,26), new Vector3(-4,0,52),
            new Vector3(5,0,78), new Vector3(-6,0,104), new Vector3(0,0,130)
        };
        Vector2[] sizes =
        {
            new Vector2(22,18), new Vector2(24,20), new Vector2(24,20),
            new Vector2(26,22), new Vector2(26,22), new Vector2(30,26)
        };
        int roomCount = centers.Length;
        int bossRoom = roomCount - 1;

        // Entrance apron plus a connecting corridor floor between each pair of rooms.
        Floor(environment,new Vector3(0,0,-12),new Vector2(10,12),floor);
        for(int room=0;room<roomCount-1;room++)
        {
            Vector3 a=centers[room], b=centers[room+1];
            Vector3 mid=(a+b)*0.5f;
            Floor(environment,mid,new Vector2(8, Vector3.Distance(a,b)),floor);
        }

        for(int room=0;room<roomCount;room++)
        {
            Vector3 center = centers[room]; Vector2 size=sizes[room];
            bool corrupted = room >= roomCount-2; // last two rooms read as corrupted
            Floor(environment,center,size,floor);
            for(float x=-size.x/2+2;x<size.x/2;x+=3)
                for(float z=-size.y/2+2;z<size.y/2;z+=3)
                    Box(environment,"Deck seam",center+new Vector3(x,.012f,z),new Vector3(2.94f,.02f,2.94f),panels,false);
            foreach(int side in new[]{-1,1})
            {
                Box(environment,"Outer guard rail",center+new Vector3(side*size.x/2,.55f,0),new Vector3(.4f,1.1f,size.y),dark,true);
                for(int j=-1;j<=1;j++)
                {
                    Vector3 point=center+new Vector3(side*(size.x/2-1.6f),0,j*6);
                    Box(environment,"Memory bank",point+Vector3.up*1.65f,new Vector3(1.5f,3.3f,2),dark,true);
                    Box(environment,"Bank cap",point+Vector3.up*3.35f,new Vector3(1.7f,.16f,2.2f),trim,false);
                    for(int strip=0;strip<4;strip++)
                        Box(environment,"Status light",point+new Vector3(-side*.78f,.8f+strip*.48f,0),new Vector3(.055f,.12f,1.6f),corrupted?corruption:cyan,false);
                }
            }
            Label(environment,$"{room+1:00}",center+new Vector3(0,.04f,-size.y/2+2),5,cyan.color);
        }
        // Route markers follow the room chain from the entrance to just before the boss room.
        float routeEndZ = centers[bossRoom].z - sizes[bossRoom].y/2;
        for(float z=-15;z<routeEndZ;z+=3)
        {
            // Interpolate the guide x across the nearest room centers so markers hug the path.
            float x = centers[0].x;
            for(int room=0;room<roomCount-1;room++)
                if(z>=centers[room].z){ float t=Mathf.InverseLerp(centers[room].z,centers[room+1].z,z); x=Mathf.Lerp(centers[room].x,centers[room+1].x,Mathf.Clamp01(t)); }
            Box(environment,"Route marker",new Vector3(x,.04f,z),new Vector3(.45f,.04f,.8f),cyan,false);
        }

        var encounters = new FirstSectorDirector.Encounter[roomCount];
        // Difficulty curve: more enemies and tougher archetypes deeper in; final room is the guardian.
        string[][] types =
        {
            new[]{"Normal","Normal"},
            new[]{"Normal","Normal","Normal"},
            new[]{"Normal","Magic_Haste","Normal","Normal"},
            new[]{"Magic_Haste","Normal","Normal","Magic_Haste"},
            new[]{"Rare_Haste_Guard","Magic_Haste","Normal","Normal","Normal"},
            new[]{"Rare_Haste_Guard","Magic_Haste","Magic_Haste"}
        };
        string[] titles = { "Limpe o acesso", "Recupere o rele", "Purgue a memoria", "Contenha a corrupcao", "Rompa a guarda", "Derrote o guardiao" };
        var encounterRoot = new GameObject("Finite encounters").transform;
        for(int room=0;room<roomCount;room++)
        {
            bool isBoss = room==bossRoom;
            // Escalating stats: damage and health grow with room index; boss room is the toughest.
            float roomDamage = 6f + room*1.5f;
            float baseHealth = 45f + room*12f;
            var center = new GameObject(titles[room]).transform; center.SetParent(encounterRoot); center.position=centers[room];
            var enemies = new Actor[types[room].Length];
            for(int i=0;i<enemies.Length;i++)
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyVariants/"+types[room][i]+".prefab");
                var enemy=(GameObject)PrefabUtility.InstantiatePrefab(prefab,center);
                enemy.transform.position=centers[room]+new Vector3((i-(enemies.Length-1)*.5f)*2.5f,.1f,2+(i%2)*2);
                var ai=enemy.GetComponent<EnemyAI>(); ai.player=player.transform; ai.sightRange=30;
                ai.attackDamage=isBoss?12:roomDamage; ai.timeBetweenAttacks=1.8f; ai.walkPointRange=0;
                enemies[i]=enemy.GetComponent<Actor>();
                enemies[i].health = isBoss && i==0 ? 140f : baseHealth;
                enemy.SetActive(false);
            }
            encounters[room]=new FirstSectorDirector.Encounter {title=titles[room],center=center,enemies=enemies};
        }
        float exitZ = centers[bossRoom].z + sizes[bossRoom].y/2 + 5f;
        var exit=new GameObject("Extraction portal").transform; exit.position=new Vector3(centers[bossRoom].x,0,exitZ);
        // Short apron so the extraction portal sits on walkable navmesh past the boss room.
        Floor(environment,new Vector3(centers[bossRoom].x,0,(centers[bossRoom].z+sizes[bossRoom].y/2+exitZ)*0.5f),new Vector2(10,exitZ-(centers[bossRoom].z+sizes[bossRoom].y/2)+4f),floor);
        for(int side=-1;side<=1;side+=2)
            Box(environment,"Extraction pylon",exit.position+new Vector3(side*2,1.6f,0),new Vector3(.6f,3.2f,.8f),trim,true);
        var glow=new GameObject("Extraction active"); glow.transform.SetParent(exit,false);
        for(int i=0;i<32;i++)
        {
            float a=i*Mathf.PI*2/32;
            Box(glow.transform,"Portal ring",exit.position+new Vector3(Mathf.Cos(a)*1.45f,.09f,Mathf.Sin(a)*1.45f),new Vector3(.25f,.09f,.25f),amber,false);
        }
        glow.SetActive(false);
        var surface=environment.gameObject.AddComponent<NavMeshSurface>(); surface.collectObjects=CollectObjects.Children;
        surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders; surface.BuildNavMesh();
        AssetDatabase.CreateAsset(surface.navMeshData,Art+"/Navigation.asset");
        var light=new GameObject("Cool overhead light").AddComponent<Light>(); light.type=LightType.Directional;
        light.transform.rotation=Quaternion.Euler(52,-32,0);light.intensity=1.6f;light.shadows=LightShadows.Soft;
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight=new Color(.32f,.39f,.48f);
        SharedHudBuilder.InstallInScene(scene);
        TMP_Text objective=CreateObjective();
        new GameObject("First sector flow").AddComponent<FirstSectorDirector>().Configure(actor,encounters,exit,glow,objective);
        EditorSceneManager.SaveScene(scene,ScenePath);
        var paths=EditorBuildSettings.scenes.ToList(); if(paths.All(s=>s.path!=ScenePath))paths.Add(new EditorBuildSettingsScene(ScenePath,true));
        EditorBuildSettings.scenes=paths.ToArray();
        ValidatePaths(player.transform.position,centers.Concat(new[]{exit.position}).Concat(encounters.SelectMany(e=>e.enemies).Select(e=>e.transform.position)).ToArray());
        CaptureOverview(camera);
        ConnectLobby(); AssetDatabase.SaveAssets();
        Debug.Log($"FIRST_SECTOR_BUILD_SUCCESS: {encounters.Length} finite encounters ({encounters.Sum(e=>e.enemies.Length)} enemies), guardian, extraction and lobby portal.");
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
            station.FindPropertyRelative("description").stringValue="Setor 01: Memoria Corrompida. Avance pelas salas, derrote o guardiao e extraia pelo portal dourado.";
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
        text.text="SETOR 01 / MEMORIA CORROMPIDA";return text;
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
    private static void CaptureOverview(Camera camera)
    {
        ShaderUtil.allowAsyncCompilation=false;
        camera.orthographic=true;camera.orthographicSize=47;camera.transform.position=new Vector3(38,78,-30);camera.transform.LookAt(new Vector3(0,0,23));
        var target=new RenderTexture(1400,1400,24);camera.targetTexture=target;camera.Render();
        var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1400,1400,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1400,1400),0,0);image.Apply();Directory.CreateDirectory("Docs");File.WriteAllBytes("Docs/FirstSector-overview.png",image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=old;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);
    }
}
