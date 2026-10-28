using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

/// <summary>Replaces the rejected prototype geometry, retaining only its existing gameplay stations.</summary>
public static class NexusBlockoutSpatialRevision
{
    private const string ScenePath = "Assets/_Project/Scenes/NexusBlockout.unity";
    private const string Folder = "Assets/_Project/Art/NexusLobby/";
    private static Transform _geometry;
    private static Material _floor, _edge;

    [MenuItem("Tools/Tech Guy/Nexus Blockout/Rebuild Asymmetric Footprint")]
    public static void Rebuild()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath || scene.isDirty)
            throw new InvalidOperationException("Open the saved isolated NexusBlockout in Edit Mode first.");
        var roots = scene.GetRootGameObjects();
        var arsenal = roots.SelectMany(r => r.GetComponentsInChildren<LobbyArsenal>()).Single();
        var guide = roots.SelectMany(r => r.GetComponentsInChildren<LobbyInteraction>()).Single();
        var hud = roots.SelectMany(r => r.GetComponentsInChildren<PlayerHUD>()).Single();
        var ad = new SerializedObject(arsenal);
        var gd = new SerializedObject(guide);
        var hd = new SerializedObject(hud);
        var pedestals = ad.FindProperty("_pedestals");
        var supports = Enumerable.Range(0, pedestals.arraySize).Select(i =>
            ((Transform)pedestals.GetArrayElementAtIndex(i).FindPropertyRelative("anchor").objectReferenceValue).parent).ToArray();
        Transform dummy = (Transform)hd.FindProperty("_lobbyTrainingAnchor").objectReferenceValue;
        var stations = gd.FindProperty("_stations");
        Transform portal = ((Transform)stations.GetArrayElementAtIndex(0).FindPropertyRelative("anchor").objectReferenceValue).parent;
        Transform archive = ((Transform)stations.GetArrayElementAtIndex(1).FindPropertyRelative("anchor").objectReferenceValue).parent;
        var oldSurface = arsenal.GetComponentInParent<NavMeshSurface>();
        if (!oldSurface) throw new InvalidOperationException("Isolated environment surface required.");
        var oldRoot = oldSurface.transform;
        int agentType = oldSurface.agentTypeID;
        var savedNav = AssetDatabase.LoadAssetAtPath<NavMeshData>(Folder + "NexusBlockoutNavMesh.asset");
        if (!savedNav) throw new InvalidOperationException("Isolated navigation asset required.");
        // Detach functionality before removing ALL previous floors, walls, labels and colliders.
        var keep = supports.Concat(new[] { arsenal.transform, dummy, portal, archive }).ToArray();
        foreach (var t in keep) t.SetParent(null, true);
        oldSurface.RemoveData(); Object.DestroyImmediate(oldRoot.gameObject);
        var root = new GameObject("NEXUS BLOCKOUT - Asymmetric connected volumes").transform;
        _geometry = new GameObject("Geometry - Revised footprint").transform; _geometry.SetParent(root);
        var functions = new GameObject("Stations - Existing gameplay").transform; functions.SetParent(root);
        foreach (var t in keep) t.SetParent(functions, true);
        _floor = Material("NexusBlockoutGray", new Color(.28f, .30f, .32f));
        _edge = Material("NexusBlockoutEdge", new Color(.13f, .15f, .17f));

        // Coordinates follow the approved reference: preparation west, practice NW,
        // home SE, departure NE. Void between volumes is real, including physics/navigation.
        Polygon("Reconexao - clipped receiving deck", new[] { P(1,-8.5f), P(2.5f,-10), P(10.5f,-10),
            P(12,-8.5f), P(12,-5), P(10.5f,-3.5f), P(1,-3.5f) });
        Polygon("Oficina - stepped service bay", new[] { P(-15,-5.5f), P(-6,-5.5f), P(-6,-3.5f),
            P(-4,-3.5f), P(-4,3.5f), P(-15,3.5f) });
        Polygon("Treino - open chamfered range", new[] { P(-17.5f,6.5f), P(-5.5f,6.5f), P(-3.5f,8.5f),
            P(-3.5f,16.5f), P(-5.5f,18.5f), P(-15.5f,18.5f), P(-17.5f,16.5f) });
        Polygon("Travessia - terminal apse", new[] { P(4,7.5f), P(6,5.5f), P(13,5.5f), P(15,7.5f),
            P(15,14), P(12,17), P(7,17), P(4,14) });
        // Short open connectors and two landings, rather than a bounding floor beneath the rooms.
        Deck("Arrival neck", 2.5f, -2.9f, 4, 3.3f);
        Polygon("Central preparation landing", new[] { P(-4.3f,-1.5f),P(3.7f,-1.5f),P(4.5f,0),
            P(3,3.5f),P(-.5f,3.5f),P(-4.3f,1.8f) });
        Deck("North open connector", 1.25f, 5.5f, 3.5f, 5);
        Deck("Departure landing", 2.25f, 8.75f, 6, 3.5f);
        Deck("Workshop practice neck", -9.5f, 5, 4, 3.5f);
        Deck("Practice departure link", -1.75f, 9.5f, 4.5f, 3);
        // Low rear boundaries leave entrances and the avatar visible from the actual follow camera.
        Box("Receiving back", P(6.5f,-9.8f) + Vector3.up*.3f, new Vector3(7,.6f,.25f), true);
        Box("Workshop service wall", P(-14.8f,-1) + Vector3.up*.45f, new Vector3(.25f,.9f,8.5f), true);
        Box("Practice backstop", P(-10.5f,18.2f) + Vector3.up*.7f, new Vector3(9,1.4f,.35f), true);
        Box("Terminal rear", P(9.5f,16.7f) + Vector3.up*.5f, new Vector3(5,1,.3f), true);
        // A shared work surface gives the existing three selections a functional workshop scale.
        Box("Equipment workbench", new Vector3(-13.5f,.42f,-.8f), new Vector3(1.3f,.84f,8.6f), true);
        for (int i = 0; i < supports.Length; i++)
        {
            Transform support = supports[i]; float z = -3.8f + 3*i;
            support.SetPositionAndRotation(new Vector3(-13.5f,0,z), Quaternion.Euler(0,-90,0));
            foreach (string name in new[] { "Pedestal base", "Pedestal column", "Pedestal cap", "Accent ring", "Accent beacon" })
            { var item = support.Find(name); if (item) Object.DestroyImmediate(item.gameObject); }
            Box("Weapon support " + i, new Vector3(-13.5f,.9f,z), new Vector3(1.1f,.12f,1.2f), false);
            var entry = pedestals.GetArrayElementAtIndex(i);
            ((Transform)entry.FindPropertyRelative("anchor").objectReferenceValue).position = new Vector3(-11.8f,.06f,z);
            entry.FindPropertyRelative("proximityRange").floatValue = 1.35f;
            foreach (var label in support.GetComponentsInChildren<TextMesh>(true))
            {
                bool equipped = label.text == "EQUIPADA";
                label.transform.SetPositionAndRotation(new Vector3(-13.5f,equipped?.56f:.79f,z-.7f), Quaternion.identity);
            }
        }
        ad.ApplyModifiedPropertiesWithoutUndo();
        dummy.position = new Vector3(-11,0,15);
        hd.FindProperty("_lobbyTrainingRange").floatValue = 7; hd.ApplyModifiedPropertiesWithoutUndo();
        portal.position = new Vector3(9.5f,0,13.5f); portal.localScale = Vector3.one * .65f;
        archive.SetPositionAndRotation(new Vector3(3,0,-8.5f), Quaternion.Euler(0,180,0));
        foreach (var label in archive.GetComponentsInChildren<TextMesh>()) label.transform.rotation = Quaternion.identity;
        for(int i=0;i<5;i++) Box("Range distance line " + i, new Vector3(-11,.012f,9+i*1.5f),new Vector3(8,.016f,.04f),false);
        Sign("RECONEXAO / ORIGEM TERRA", new Vector3(6.5f,.035f,-8.8f),.045f,true);
        Sign("OFICINA",new Vector3(-8,.035f,-4.8f),.055f,true);
        Sign("TREINO",new Vector3(-8.8f,1.7f,17.3f),.06f,false);
        Sign("TRAVESSIA / FIRST SECTOR",new Vector3(9.5f,.035f,7.5f),.05f,true);
        var player = roots.Where(r=>r).SelectMany(r=>r.GetComponentsInChildren<PlayerActor>()).Single();
        player.transform.SetPositionAndRotation(new Vector3(6.5f,.1f,-6.8f),Quaternion.identity);
        var cam = roots.Where(r=>r).SelectMany(r=>r.GetComponentsInChildren<Camera>()).Single(c=>c.CompareTag("MainCamera"));
        cam.fieldOfView=40; cam.transform.position=new Vector3(6.5f,10,-16.8f);cam.transform.LookAt(new Vector3(6.5f,0,-6.8f));
        foreach(var light in roots.Where(r=>r).SelectMany(r=>r.GetComponentsInChildren<Light>())) light.color=new Color(.92f,.95f,1);
        var surface=root.gameObject.AddComponent<NavMeshSurface>(); surface.agentTypeID=agentType;
        surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();var baked=surface.navMeshData;
        if(!baked)throw new InvalidOperationException("Spatial revision bake failed.");
        surface.RemoveData();EditorUtility.CopySerialized(baked,savedNav);surface.navMeshData=savedNav;
        Object.DestroyImmediate(baked);surface.AddData();EditorUtility.SetDirty(savedNav);
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        Debug.Log("NEXUS_SPATIAL_REVISION: previous geometry removed; four offset volumes and short connections saved; existing scene and NavMesh GUIDs preserved.");
    }

    private static Vector3 P(float x,float z)=>new Vector3(x,0,z);
    private static Material Material(string name,Color color)
    {
        string path=Folder+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(AssetDatabase.LoadAssetAtPath<Material>(Folder+"Materials/Blue Steel.mat"));AssetDatabase.CreateAsset(material,path);}
        if(material.HasProperty("_BaseColor"))material.SetColor("_BaseColor",color);
        if(material.HasProperty("_Color"))material.SetColor("_Color",color);
        if(material.HasProperty("_Metallic"))material.SetFloat("_Metallic",0);
        if(material.HasProperty("_Smoothness"))material.SetFloat("_Smoothness",.2f);
        material.DisableKeyword("_EMISSION");EditorUtility.SetDirty(material);return material;
    }
    private static void Deck(string name,float x,float z,float width,float depth)=>
        Box(name,new Vector3(x,-.18f,z),new Vector3(width,.36f,depth),true,true);
    private static void Box(string name,Vector3 position,Vector3 size,bool collision,bool floor=false)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(_geometry);
        go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=floor?_floor:_edge;
        if(!collision)Object.DestroyImmediate(go.GetComponent<Collider>());else go.layer=LayerMask.NameToLayer("Ground");
    }
    private static void Polygon(string name,Vector3[] outline)
    {
        string path=Folder+"NexusBlockoutFloorplan.asset";
        var mesh=AssetDatabase.LoadAllAssetsAtPath(path).OfType<Mesh>().FirstOrDefault(m=>m.name==name);
        if(!mesh)
        {
            mesh=new Mesh{name=name};
            if(AssetDatabase.LoadMainAssetAtPath(path))AssetDatabase.AddObjectToAsset(mesh,path);else AssetDatabase.CreateAsset(mesh,path);
        }
        var vertices=new List<Vector3>();var triangles=new List<int>();
        Vector3 center=outline.Aggregate(Vector3.zero,(a,b)=>a+b)/outline.Length;
        vertices.Add(center);vertices.AddRange(outline);
        for(int i=0;i<outline.Length;i++){triangles.Add(0);triangles.Add((i+1)%outline.Length+1);triangles.Add(i+1);}
        for(int i=0;i<outline.Length;i++)
        {
            int j=(i+1)%outline.Length;int start=vertices.Count;
            vertices.Add(outline[i]);vertices.Add(outline[j]);vertices.Add(outline[i]-Vector3.up*.36f);vertices.Add(outline[j]-Vector3.up*.36f);
            triangles.AddRange(new[]{start,start+2,start+1,start+1,start+2,start+3});
        }
        mesh.Clear();mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();EditorUtility.SetDirty(mesh);
        var go=new GameObject(name);go.transform.SetParent(_geometry);go.layer=LayerMask.NameToLayer("Ground");
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=_floor;
        go.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    private static void Sign(string text,Vector3 position,float size,bool floor)
    {
        var go=new GameObject("Sign - "+text);go.transform.SetParent(_geometry);go.transform.position=position;
        go.transform.rotation=floor?Quaternion.Euler(90,0,0):Quaternion.identity;
        var label=go.AddComponent<TextMesh>();label.text=text;label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize=80;label.characterSize=size;label.anchor=TextAnchor.MiddleCenter;label.color=new Color(.8f,.84f,.87f);
        go.GetComponent<Renderer>().sharedMaterial=label.font.material;
    }
}
