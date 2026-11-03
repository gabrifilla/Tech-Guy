using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Reversible intermediate mesh revision. Native gameplay and the03 sources stay intact.</summary>
public static class RunExp04EnemyArtAuthoring
{
    public const string Root="Assets/_Project/Art/RunExp04/Enemies";
    public const string Prefabs="Assets/_Project/Prefabs/FirstSector/RunExp04";
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Arte/Construir revisão intermediária")]
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        Directory.CreateDirectory(Root);Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
        foreach(string name in new[]{"Fragmento","Emissor"})
        {
            string path=Prefabs+"/"+name+".prefab";
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path))continue;
            if(!AssetDatabase.CopyAsset("Assets/_Project/Prefabs/FirstSector/RunExp03/"+name+".prefab",path))throw new Exception("Copy failed");
            var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                var poseData=new SerializedObject(go.GetComponent<FirstSectorEnemyPose>());poseData.FindProperty("_weightedLocomotion").boolValue=true;poseData.ApplyModifiedPropertiesWithoutUndo();
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if(name=="Fragmento")
                    {
                        if(filter.name=="Coxa")filter.sharedMesh=Loft("F_Coxa_com_encaixe",new[]{Vector3.zero,Vector3.forward*.08f,Vector3.forward*.22f,Vector3.forward*.8f,Vector3.forward},new[]{new Vector2(.14f,.12f),new Vector2(.18f,.16f),new Vector2(.11f,.09f),new Vector2(.065f,.06f),new Vector2(.075f,.075f)});
                        if(filter.name=="Canela")filter.sharedMesh=Loft("F_Canela_com_joelho",new[]{Vector3.zero,Vector3.forward*.12f,Vector3.forward*.3f,Vector3.forward},new[]{new Vector2(.09f,.085f),new Vector2(.1f,.075f),new Vector2(.055f,.045f),new Vector2(.025f,.03f)});
                        if(filter.name.StartsWith("F_Mandibula_",StringComparison.Ordinal))filter.sharedMesh=Loft("F_Mandibula_laminada",new[]{new Vector3(0,0,-.12f),Vector3.zero,new Vector3(0,-.025f,.18f),new Vector3(0,.025f,.34f)},new[]{new Vector2(.1f,.055f),new Vector2(.13f,.075f),new Vector2(.075f,.05f),new Vector2(.008f,.04f)});
                        if(filter.name=="Apoio")filter.sharedMesh=Loft("F_Apoio_bipartido",new[]{new Vector3(0,0,-.14f),Vector3.zero,new Vector3(0,0,.2f)},new[]{new Vector2(.035f,.04f),new Vector2(.105f,.045f),new Vector2(.03f,.025f)});
                    }
                    else if(filter.name=="E_Arco_0"||filter.name=="E_Arco_1")
                    {
                        float s=filter.name.EndsWith("0",StringComparison.Ordinal)?-1:1;
                        filter.sharedMesh=Loft("E_Carcaca_estruturada_"+(s<0?0:1),new[]{new Vector3(s*.02f,-.5f,-.45f),new Vector3(s*.24f,-.44f,-.44f),new Vector3(s*.38f,-.12f,-.27f),new Vector3(s*.38f,.27f,-.1f),new Vector3(s*.24f,.61f,.13f),new Vector3(s*.04f,.5f,.36f),new Vector3(-s*.05f,.19f,.46f)},new[]{new Vector2(.065f,.055f),new Vector2(.17f,.12f),new Vector2(.22f,.15f),new Vector2(.22f,.16f),new Vector2(.16f,.1f),new Vector2(.085f,.08f),new Vector2(.024f,.04f)});
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
        }
        AssetDatabase.SaveAssets();
    }
    private static Mesh Loft(string name,Vector3[] centers,Vector2[] radii)
    {
        string path=Root+"/"+name+".asset";var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved)return saved;
        const int sides=8;var vertices=new List<Vector3>();var triangles=new List<int>();
        Action<Vector3,Vector3,Vector3> tri=(a,b,c)=>{int start=vertices.Count;vertices.Add(a);vertices.Add(b);vertices.Add(c);triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);};
        Func<int,int,Vector3> point=(ring,index)=>centers[ring]+new Vector3(Mathf.Cos(index*Mathf.PI*2/sides)*radii[ring].x,Mathf.Sin(index*Mathf.PI*2/sides)*radii[ring].y,0);
        for(int ring=0;ring<centers.Length-1;ring++)for(int j=0;j<sides;j++)
        {var a=point(ring,j);var b=point(ring,(j+1)%sides);var c=point(ring+1,j);var d=point(ring+1,(j+1)%sides);tri(a,c,b);tri(b,c,d);}
        for(int j=0;j<sides;j++){tri(centers[0],point(0,j),point(0,(j+1)%sides));int last=centers.Length-1;tri(centers[last],point(last,(j+1)%sides),point(last,j));}
        double volume=0;for(int i=0;i<vertices.Count;i+=3)volume+=Vector3.Dot(vertices[i],Vector3.Cross(vertices[i+1],vertices[i+2]))/6.0;
        if(volume<0)for(int i=0;i<triangles.Count;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,vertices.Select(v=>new Vector2(v.x*.5f+.5f,v.z*.5f+.5f)).ToList());mesh.RecalculateNormals();mesh.RecalculateBounds();AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    [MenuItem("Tools/Tech Guy/RUN-EXP-04/Arte/Integrar cópias validadas na FirstSector")]
    public static void Integrate()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>(RunExp03Authoring.Content);var data=new SerializedObject(content);var list=data.FindProperty("_additionalEnemies");
        var prefabs=new[]{"Fragmento","Emissor"}.Select(n=>AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+n+".prefab")).ToArray();if(prefabs.Any(p=>!p))throw new Exception("Build and validate the revision first");
        for(int i=0;i<2;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=prefabs[i];data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
    }
}
