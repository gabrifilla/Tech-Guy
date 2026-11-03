using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>Authors isolated review assets. Never invokes production scene/prefab builders.</summary>
public static class RunExp02Builder
{
    public const string Root = "Assets/_Project/Prototypes/RunExp02";
    public const string ArenaA = Root + "/ArenaA.unity", ArenaB = Root + "/ArenaB.unity";
    private static Material _shell, _edge, _floor, _cover;

    [MenuItem("Tools/Tech Guy/RUN-EXP-02/1 - Abrir arena aberta")]
    public static void OpenA() { Open(ArenaA); }
    [MenuItem("Tools/Tech Guy/RUN-EXP-02/2 - Abrir arena com cobertura")]
    public static void OpenB() { Open(ArenaB); }
    [MenuItem("Tools/Tech Guy/RUN-EXP-02/3 - Retornar ao Nexus")]
    public static void Return() { Open("Assets/_Project/Scenes/NexusBlockout.unity"); }
    private static void Open(string path)
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Saia de Play antes de trocar a cena de revisão."); return; }
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
    }
    public static void BuildEnemies()
    {
        Directory.CreateDirectory(Root + "/Meshes"); AssetDatabase.Refresh();
        _shell = Material("Carcaca", new Color(.35f,.43f,.5f), .55f);
        _edge = Material("Nucleo", new Color(.95f,.64f,.27f), .3f);
        BuildEnemy(false); BuildEnemy(true); ConfigureReviewDistance(); AssetDatabase.SaveAssets();
    }
    public static void ConfigureReviewDistance()
    {
        string path=Root+"/DistanciaEmissor.asset";
        var profile=AssetDatabase.LoadAssetAtPath<PreferredDistanceProfile>(path);
        if(!profile){profile=ScriptableObject.CreateInstance<PreferredDistanceProfile>();AssetDatabase.CreateAsset(profile,path);}
        var data=new SerializedObject(profile);var entries=data.FindProperty("_entries");entries.arraySize=1;
        var entry=entries.GetArrayElementAtIndex(0);entry.FindPropertyRelative("archetype").enumValueIndex=(int)ArchetypeId.Sniper;
        entry.FindPropertyRelative("preferredDistance").floatValue=6;entry.FindPropertyRelative("separationRadius").floatValue=.8f;data.ApplyModifiedPropertiesWithoutUndo();
        var prefab=PrefabUtility.LoadPrefabContents(Root+"/Emissor.prefab");
        Set(prefab.GetComponent<PreferredDistanceLayer>(),"_profile",profile);
        PrefabUtility.SaveAsPrefabAsset(prefab,Root+"/Emissor.prefab");PrefabUtility.UnloadPrefabContents(prefab);AssetDatabase.SaveAssets();
    }
    private static Material Material(string name, Color color, float metallic = 0)
    {
        string path = Root + "/" + name + ".mat";
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path); if (existing) return existing;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.name = name;
        mat.SetColor("_BaseColor", color); mat.SetFloat("_Metallic", metallic); mat.SetFloat("_Smoothness", .3f);
        AssetDatabase.CreateAsset(mat,path); return mat;
    }
    private static void Set(Object obj, string field, Object value)
    { var so = new SerializedObject(obj); so.FindProperty(field).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
    private static GameObject Child(string name, Transform parent, Vector3 pos)
    { var go = new GameObject(name); go.transform.SetParent(parent,false); go.transform.localPosition = pos; return go; }

    // A swept polygonal cross-section with separately authored station widths/heights/centres.
    // Triangles are split for hard facets; UVs are a station/section unwrap, not a texture atlas.
    private static Mesh Loft(string name, Vector3[] centres, Vector2[] radii, int sides = 6)
    {
        string path = Root + "/Meshes/" + name + ".asset";
        var old = AssetDatabase.LoadAssetAtPath<Mesh>(path); if (old) return old;
        var points = new List<Vector3>(); var uv = new List<Vector2>(); var triangles = new List<int>();
        Action<Vector3,Vector3,Vector3> tri = (a,b,c) => {
            int n=points.Count; points.Add(a);points.Add(b);points.Add(c);
            uv.Add(new Vector2(a.x,a.z));uv.Add(new Vector2(b.x,b.z));uv.Add(new Vector2(c.x,c.z));
            triangles.Add(n);triangles.Add(n+2);triangles.Add(n+1);
        };
        var rings = new Vector3[centres.Length,sides];
        for (int j=0;j<centres.Length;j++) for(int i=0;i<sides;i++)
        { float a=(i+.5f)*Mathf.PI*2/sides; rings[j,i]=centres[j]+new Vector3(Mathf.Cos(a)*radii[j].x,Mathf.Sin(a)*radii[j].y,0); }
        for (int j=0;j<centres.Length-1;j++) for(int i=0;i<sides;i++)
        { int k=(i+1)%sides;tri(rings[j,i],rings[j+1,i],rings[j+1,k]);tri(rings[j,i],rings[j+1,k],rings[j,k]); }
        for(int i=0;i<sides;i++)
        {int k=(i+1)%sides;tri(centres[0],rings[0,i],rings[0,k]);int j=centres.Length-1;tri(centres[j],rings[j,k],rings[j,i]);}
        var mesh=new Mesh {name=name}; mesh.SetVertices(points);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    public static void RepairOutwardNormals()
    {
        foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{Root+"/Meshes"}))
        {
            var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
            var vertices=mesh.vertices;var triangles=mesh.triangles;double volume=0;
            for(int i=0;i<triangles.Length;i+=3)volume+=Vector3.Dot(vertices[triangles[i]],Vector3.Cross(vertices[triangles[i+1]],vertices[triangles[i+2]]))/6.0;
            if(volume>=0)continue;
            for(int i=0;i<triangles.Length;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
            mesh.triangles=triangles;mesh.RecalculateNormals();EditorUtility.SetDirty(mesh);
        }
        AssetDatabase.SaveAssets();
    }
    private static Transform Part(string name, Transform parent, Vector3 pos, Vector3[] centres, Vector2[] radii, Material mat, int sides=6)
    {
        var go=Child(name,parent,pos);go.AddComponent<MeshFilter>().sharedMesh=Loft(name,centres,radii,sides);
        go.AddComponent<MeshRenderer>().sharedMaterial=mat; return go.transform;
    }
    private static void BuildEnemy(bool ranged)
    {
        string name=ranged?"Emissor":"Fragmento", path=Root+"/"+name+".prefab";
        if(File.Exists(path)) return;
        var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/"+(ranged?"Sniper":"Rush")+".prefab");
        var go=Object.Instantiate(source);go.SetActive(false);go.name=name;
        foreach(Transform t in go.transform.Cast<Transform>().ToArray()) Object.DestroyImmediate(t.gameObject);
        Object.DestroyImmediate(go.GetComponent<Animator>());
        // Native Rush moves into its own melee reach rather than using humanoid preferred spacing.
        if(!ranged) Object.DestroyImmediate(go.GetComponent<PreferredDistanceLayer>());
        var ai=go.GetComponent<EnemyAI>();ai.sightRange=24;ai.attackRange=ranged?12:2;
        Set(ai,"animator",null);ai.player=null;
        var visual=Child("ReactionRoot",go.transform,Vector3.zero).transform;visual.localScale=Vector3.one*2;
        var body=Child("PoseRoot",visual,Vector3.zero).transform;
        var limbs=new List<Transform>();
        if(!ranged)
        {
            Part("F_Carapaca",body,new Vector3(0,.46f,-.1f),
                new[]{new Vector3(0,0,-.7f),new Vector3(0,.1f,-.35f),new Vector3(0,.05f,.22f),new Vector3(0,-.05f,.47f)},
                new[]{new Vector2(.12f,.08f),new Vector2(.46f,.25f),new Vector2(.34f,.22f),new Vector2(.16f,.1f)},_shell);
            Part("F_Cabeca",body,new Vector3(0,.42f,.57f),new[]{new Vector3(0,0,-.22f),Vector3.zero,new Vector3(0,-.08f,.34f)},
                new[]{new Vector2(.2f,.15f),new Vector2(.29f,.18f),new Vector2(.09f,.07f)},_edge);
            for(int i=0;i<4;i++)
            {
                float sign=(i&1)==0?-1:1;float z=i<2?.32f:-.4f;
                var pivot=Child("Pata_"+i,body,new Vector3(sign*.28f,.4f,z)).transform;limbs.Add(pivot);
                Part("F_Membro_"+i,pivot,Vector3.zero,
                    new[]{Vector3.zero,new Vector3(sign*.35f,-.03f,-.1f),new Vector3(sign*.5f,-.35f,.12f),new Vector3(sign*.45f,-.39f,.24f)},
                    new[]{new Vector2(.1f,.1f),new Vector2(.12f,.095f),new Vector2(.04f,.04f),new Vector2(.012f,.012f)},_shell,5);
            }
            for(int i=0;i<2;i++)
            {float s=i==0?-1:1;Part("F_Mandibula_"+i,body,new Vector3(s*.17f,.3f,.67f),
                new[]{Vector3.zero,new Vector3(s*.13f,-.04f,.25f),new Vector3(s*.02f,.04f,.44f)},
                new[]{new Vector2(.07f,.06f),new Vector2(.055f,.055f),new Vector2(.006f,.006f)},_shell,5);}
        }
        else
        {
            for(int i=0;i<2;i++)
            {
                float s=i==0?-1:1;var pivot=Child("Carcaca_abre_"+i,body,new Vector3(s*.25f,.9f,0)).transform;limbs.Add(pivot);
                Part("E_Arco_"+i,pivot,Vector3.zero,
                    new[]{new Vector3(s*.04f,-.48f,-.48f),new Vector3(s*.2f,-.1f,-.3f),new Vector3(s*.2f,.37f,-.08f),new Vector3(s*.05f,.5f,.16f),new Vector3(-s*.05f,.18f,.45f)},
                    new[]{new Vector2(.08f,.07f),new Vector2(.18f,.19f),new Vector2(.2f,.22f),new Vector2(.14f,.14f),new Vector2(.035f,.05f)},_shell,5);
                Part("E_Quilha_"+i,pivot,new Vector3(s*.2f,-.1f,-.12f),
                    new[]{new Vector3(0,0,-.25f),new Vector3(s*.32f,-.3f,-.4f),new Vector3(s*.37f,-.64f,-.52f)},
                    new[]{new Vector2(.1f,.13f),new Vector2(.1f,.1f),new Vector2(.01f,.01f)},_shell,5);
            }
            Part("E_Nucleo",body,new Vector3(0,.8f,0),new[]{new Vector3(0,0,-.35f),Vector3.zero,new Vector3(0,-.12f,.25f)},
                new[]{new Vector2(.1f,.1f),new Vector2(.22f,.25f),new Vector2(.07f,.07f)},_edge,8);
            // The barrel terminates at the native muzzle (0, .55, .325 world at root scale .5).
            Part("E_Canal_emissao",body,new Vector3(0,.55f,0),new[]{new Vector3(0,0,-.25f),new Vector3(0,0,.2f),new Vector3(0,0,.325f)},
                new[]{new Vector2(.16f,.13f),new Vector2(.12f,.09f),new Vector2(.19f,.15f)},_shell,8);
            Part("E_Boca",body,new Vector3(0,.55f,.327f),new[]{Vector3.zero,new Vector3(0,0,.015f)},
                new[]{new Vector2(.12f,.08f),new Vector2(.1f,.06f)},_edge,8);
        }
        var reaction=go.GetComponent<CombatReactionController>()??go.AddComponent<CombatReactionController>();
        reaction.ConfigureRank(EnemyRank.Normal);Set(reaction,"visualRoot",visual);
        reaction.ConfigureProfile(go.GetComponent<EnemyVariant>().Profile);
        var capsule=go.GetComponent<CapsuleCollider>();capsule.radius=ranged?.8f:.85f;capsule.height=ranged?2.6f:1.4f;capsule.center=new Vector3(0,ranged?1.3f:.7f,0);
        var pose=go.AddComponent<RunExp02EnemyPose>();Set(pose,"_body",body);
        var data=new SerializedObject(pose);data.FindProperty("_ranged").boolValue=ranged;
        var array=data.FindProperty("_limbs");array.arraySize=limbs.Count;for(int i=0;i<limbs.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=limbs[i];data.ApplyModifiedPropertiesWithoutUndo();
        go.SetActive(true);PrefabUtility.SaveAsPrefabAsset(go,path);Object.DestroyImmediate(go);
    }

    public static void BuildArenas()
    {
        _floor=Material("Piso",new Color(.105f,.14f,.18f));_cover=Material("Cobertura",new Color(.24f,.29f,.34f));
        BuildArena(false);BuildArena(true);AssetDatabase.SaveAssets();
    }
    private static void Box(string name, Transform parent, Vector3 position, Vector3 scale, Material mat, bool ground=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);go.transform.position=position;go.transform.localScale=scale;
        go.GetComponent<Renderer>().sharedMaterial=mat;go.layer=ground?LayerMask.NameToLayer("Ground"):0;go.isStatic=true;
        if(!ground){var modifier=go.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;}
    }
    private static void BuildArena(bool cover)
    {
        string path=cover?ArenaB:ArenaA;if(File.Exists(path))return;
        var original=EditorSceneManager.GetSceneManagerSetup();
        Scene source=EditorSceneManager.OpenScene(FirstSectorBuilder.ScenePath,OpenSceneMode.Additive);
        var roots=source.GetRootGameObjects();
        var player=Object.Instantiate(roots.SelectMany(r=>r.GetComponentsInChildren<PlayerActor>(true)).Single()).gameObject;
        var camera=Object.Instantiate(roots.SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.CompareTag("MainCamera"))).gameObject;
        Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(player,scene);SceneManager.MoveGameObjectToScene(camera,scene);SceneManager.SetActiveScene(scene);
        EditorSceneManager.CloseScene(source,true);
        player.name="Player RUN-EXP-02";player.transform.position=new Vector3(0,0,-6);player.SetActive(true);
        player.GetComponent<PlayerActor>().healthBar=player.GetComponent<PlayerActor>().manaBar=null;
        camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>().m_Target=player.transform;
        Set(player.GetComponent<CharControlScript>(),"mainCamera",camera.GetComponent<Camera>());
        var world=new GameObject("ReviewGeometry");
        Box("Piso",world.transform,new Vector3(0,-.25f,0),new Vector3(24,.5f,20),_floor,true);
        Box("Limite norte",world.transform,new Vector3(0,1.2f,10.3f),new Vector3(25,2.4f,.6f),_cover);
        Box("Limite sul",world.transform,new Vector3(0,1.2f,-10.3f),new Vector3(25,2.4f,.6f),_cover);
        Box("Limite leste",world.transform,new Vector3(12.3f,1.2f,0),new Vector3(.6f,2.4f,20),_cover);
        Box("Limite oeste",world.transform,new Vector3(-12.3f,1.2f,0),new Vector3(.6f,2.4f,20),_cover);
        if(cover)
        {
            Box("Cobertura esquerda",world.transform,new Vector3(-2.3f,1.1f,-.8f),new Vector3(4.2f,2.2f,4.2f),_cover);
            Box("Cobertura direita",world.transform,new Vector3(2.3f,1.1f,2.4f),new Vector3(4.2f,2.2f,4.2f),_cover);
        }
        var surface=world.AddComponent<NavMeshSurface>();surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.BuildNavMesh();
        AssetDatabase.CreateAsset(surface.navMeshData,Root+(cover?"/NavB.asset":"/NavA.asset"));
        var light=new GameObject("Luz de revisão");var sun=light.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.35f;light.transform.rotation=Quaternion.Euler(50,-35,0);
        RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(.45f,.49f,.56f);
        var director=new GameObject("Revisão isolada").AddComponent<RunExp02Review>();
        director.gameObject.AddComponent<SwarmAttackCoordinator>();
        Set(director,"_player",player.GetComponent<PlayerActor>());
        Set(director,"_fragmento",AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Fragmento.prefab"));
        Set(director,"_emissor",AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Emissor.prefab"));
        var settings=new SerializedObject(director);settings.FindProperty("_coverArena").boolValue=cover;settings.ApplyModifiedPropertiesWithoutUndo();
        SharedHudBuilder.InstallInScene(scene);
        EditorSceneManager.SaveScene(scene,path);EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(original);
    }
    public static void ThickenReviewCover()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        var original=EditorSceneManager.GetSceneManagerSetup();
        var scene=EditorSceneManager.OpenScene(ArenaB,OpenSceneMode.Additive);
        foreach(var box in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<BoxCollider>()).Where(c=>c.name.StartsWith("Cobertura ",StringComparison.Ordinal)))
        {var scale=box.transform.localScale;scale.z=4.2f;box.transform.localScale=scale;
            var modifier=box.GetComponent<NavMeshModifier>()??box.gameObject.AddComponent<NavMeshModifier>();modifier.overrideArea=true;modifier.area=1;}
        var surface=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<NavMeshSurface>()).Single();
        var originalData=surface.navMeshData;surface.BuildNavMesh();var generated=surface.navMeshData;
        surface.RemoveData();EditorUtility.CopySerialized(generated,originalData);EditorUtility.SetDirty(originalData);
        surface.navMeshData=originalData;surface.AddData();Object.DestroyImmediate(generated);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);EditorSceneManager.CloseScene(scene,true);EditorSceneManager.RestoreSceneManagerSetup(original);
    }
}
