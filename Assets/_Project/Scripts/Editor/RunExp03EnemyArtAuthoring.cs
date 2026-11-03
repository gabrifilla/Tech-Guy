using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Intermediate authored faceted geometry, kept separate from the accepted RUNEXP02 assets.</summary>
public static class RunExp03EnemyArtAuthoring
{
    private const string Root=RunExp03Authoring.Root+"/Enemies";
    private static Material _dark,_armor,_core;
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        Directory.CreateDirectory(Root);AssetDatabase.Refresh();
        _dark=Material("Estrutura",new Color(.16f,.23f,.28f),.4f);
        _armor=Material("Placas",new Color(.4f,.5f,.57f),.45f);
        _core=Material("Ceramica de memoria",new Color(.93f,.59f,.24f),.2f);
        Refine(false);Refine(true);AssetDatabase.SaveAssets();
    }
    private static Material Material(string name,Color color,float metal)
    {
        string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);if(mat)return mat;
        mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};mat.SetColor("_BaseColor",color);mat.SetFloat("_Metallic",metal);mat.SetFloat("_Smoothness",.3f);AssetDatabase.CreateAsset(mat,path);return mat;
    }
    private static GameObject Child(string name,Transform parent,Vector3 position)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go;}
    private static void Set(Object target,string field,Object value)
    {var data=new SerializedObject(target);data.FindProperty(field).objectReferenceValue=value;data.ApplyModifiedPropertiesWithoutUndo();}
    private static Mesh SaveMesh(string name,List<Vector3> vertices,List<int> triangles)
    {
        var uv=vertices.Select(v=>new Vector2(v.x*.5f+.5f,v.z*.5f+.5f)).ToList();
        double volume=0;for(int i=0;i<triangles.Count;i+=3)volume+=Vector3.Dot(vertices[triangles[i]],Vector3.Cross(vertices[triangles[i+1]],vertices[triangles[i+2]]))/6.0;
        if(volume<0)for(int i=0;i<triangles.Count;i+=3){int swap=triangles[i+1];triangles[i+1]=triangles[i+2];triangles[i+2]=swap;}
        var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);mesh.RecalculateNormals();mesh.RecalculateBounds();
        string path=Root+"/"+name+".asset";var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(mesh,path);return mesh;
    }
    private static Mesh Loft(string name,Vector3[] centres,Vector2[] radii,int sides=8)
    {
        var v=new List<Vector3>();var t=new List<int>();
        void Tri(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(c);v.Add(b);t.Add(n);t.Add(n+1);t.Add(n+2);}
        var rings=new Vector3[centres.Length,sides];
        for(int j=0;j<centres.Length;j++)for(int i=0;i<sides;i++){float a=(i+.5f)*Mathf.PI*2/sides;rings[j,i]=centres[j]+new Vector3(Mathf.Cos(a)*radii[j].x,Mathf.Sin(a)*radii[j].y,0);}
        for(int j=0;j<centres.Length-1;j++)for(int i=0;i<sides;i++){int k=(i+1)%sides;Tri(rings[j,i],rings[j+1,i],rings[j+1,k]);Tri(rings[j,i],rings[j+1,k],rings[j,k]);}
        for(int i=0;i<sides;i++){int k=(i+1)%sides;Tri(centres[0],rings[0,i],rings[0,k]);int j=centres.Length-1;Tri(centres[j],rings[j,k],rings[j,i]);}
        return SaveMesh(name,v,t);
    }
    private static Mesh Tube()
    {
        var profile=new[]{new Vector2(.18f,-.25f),new Vector2(.13f,.16f),new Vector2(.2f,.325f),new Vector2(.085f,.325f),new Vector2(.085f,-.25f)};
        var v=new List<Vector3>();var t=new List<int>();
        Vector3 Point(int station,int side){float a=side*Mathf.PI/4;return new Vector3(Mathf.Cos(a)*profile[station].x,Mathf.Sin(a)*profile[station].x,profile[station].y);}
        void Tri(Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(c);v.Add(b);t.Add(n);t.Add(n+1);t.Add(n+2);}
        for(int j=0;j<profile.Length;j++)for(int i=0;i<8;i++){int k=(i+1)%8,next=(j+1)%profile.Length;Tri(Point(j,i),Point(next,i),Point(next,k));Tri(Point(j,i),Point(next,k),Point(j,k));}
        return SaveMesh("E_Canal_oco",v,t);
    }
    private static Transform Part(string name,Transform parent,Vector3 position,Mesh mesh,Material material)
    {var go=Child(name,parent,position);go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=material;return go.transform;}
    private static void Refine(bool ranged)
    {
        string path=RunExp03Authoring.Prefabs+"/"+(ranged?"Emissor":"Fragmento")+".prefab";
        var go=PrefabUtility.LoadPrefabContents(path);
        try
        {
            if(go.GetComponent<FirstSectorEnemyPose>())return;
            var old=go.GetComponent<RunExp02EnemyPose>();var oldData=new SerializedObject(old);
            var body=(Transform)oldData.FindProperty("_body").objectReferenceValue;
            var limbs=oldData.FindProperty("_limbs");var pivots=Enumerable.Range(0,limbs.arraySize).Select(i=>(Transform)limbs.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
            foreach(var renderer in body.GetComponentsInChildren<MeshRenderer>())renderer.sharedMaterial=renderer.sharedMaterial.name=="Nucleo"?_core:_dark;
            Object.DestroyImmediate(old);var pose=go.AddComponent<FirstSectorEnemyPose>();Set(pose,"_body",body);
            var data=new SerializedObject(pose);data.FindProperty("_ranged").boolValue=ranged;data.ApplyModifiedPropertiesWithoutUndo();
            if(!ranged)
            {
                var upperMesh=Loft("F_Braco_articulado",new[]{Vector3.zero,new Vector3(0,0,.17f),Vector3.forward},new[]{new Vector2(.115f,.1f),new Vector2(.09f,.075f),new Vector2(.045f,.045f)},6);
                var lowerMesh=Loft("F_Perna_articulada",new[]{Vector3.zero,new Vector3(0,0,.22f),Vector3.forward},new[]{new Vector2(.065f,.06f),new Vector2(.07f,.06f),new Vector2(.028f,.028f)},6);
                var footMesh=Loft("F_Apoio",new[]{new Vector3(0,0,-.09f),Vector3.zero,new Vector3(0,0,.17f)},new[]{new Vector2(.05f,.035f),new Vector2(.08f,.035f),new Vector2(.012f,.012f)},6);
                data=new SerializedObject(pose);var legs=data.FindProperty("_legs");legs.arraySize=4;
                for(int i=0;i<4;i++)
                {
                    foreach(Transform child in pivots[i].Cast<Transform>().ToArray())Object.DestroyImmediate(child.gameObject);
                    var upper=Part("Coxa",body,Vector3.zero,upperMesh,_armor);var lower=Part("Canela",body,Vector3.zero,lowerMesh,_dark);var foot=Part("Apoio",body,Vector3.zero,footMesh,_armor);
                    float sign=(i&1)==0?-1:1;Vector3 rest=new Vector3(sign*.76f,-.045f,pivots[i].localPosition.z+.1f);
                    var leg=legs.GetArrayElementAtIndex(i);leg.FindPropertyRelative("hip").objectReferenceValue=pivots[i];leg.FindPropertyRelative("upper").objectReferenceValue=upper;leg.FindPropertyRelative("lower").objectReferenceValue=lower;leg.FindPropertyRelative("foot").objectReferenceValue=foot;leg.FindPropertyRelative("restFoot").vector3Value=rest;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
                var head=body.Find("F_Cabeca");Set(pose,"_head",head);
                Part("F_Testa",head,new Vector3(0,.13f,-.04f),Loft("F_Testa",new[]{new Vector3(0,0,-.12f),Vector3.zero,new Vector3(0,0,.21f)},new[]{new Vector2(.08f,.035f),new Vector2(.23f,.05f),new Vector2(.12f,.025f)},6),_armor);
                var plateMesh=Loft("F_Placa_dorsal",new[]{new Vector3(0,0,-.16f),Vector3.zero,new Vector3(0,0,.16f)},new[]{new Vector2(.18f,.025f),new Vector2(.29f,.055f),new Vector2(.18f,.02f)},6);
                // Combine the three fixed armor plates so segmentation does not add three draw calls.
                var combine=new List<CombineInstance>();
                for(int i=0;i<3;i++)combine.Add(new CombineInstance{mesh=plateMesh,transform=Matrix4x4.TRS(new Vector3(0,i==0?.77f:i==1?.75f:.67f,-.48f+i*.3f),Quaternion.identity,Vector3.one)});
                var combined=new Mesh{name="F_Dorso_segmentado"};combined.CombineMeshes(combine.ToArray(),true,true);string meshPath=Root+"/F_Dorso_segmentado.asset";
                var saved=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);if(saved){EditorUtility.CopySerialized(combined,saved);Object.DestroyImmediate(combined);combined=saved;EditorUtility.SetDirty(saved);}else AssetDatabase.CreateAsset(combined,meshPath);
                Part("Dorso segmentado",body,Vector3.zero,combined,_armor);
                data=new SerializedObject(pose);var jaws=data.FindProperty("_jaws");jaws.arraySize=2;
                for(int i=0;i<2;i++)
                {
                    var jaw=body.Find("F_Mandibula_"+i);var hinge=Child("Mandibula articulada "+i,body,jaw.localPosition).transform;jaw.SetParent(hinge,false);jaw.localPosition=Vector3.zero;jaws.GetArrayElementAtIndex(i).objectReferenceValue=hinge;
                }
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                data=new SerializedObject(pose);var shells=data.FindProperty("_shells");shells.arraySize=2;
                for(int i=0;i<2;i++)
                {
                    float s=i==0?-1:1;var shell=pivots[i].GetComponentsInChildren<MeshFilter>().First(m=>m.name=="E_Arco_"+i);
                    shell.sharedMesh=Loft("E_Carcaca_chanfrada_"+i,new[]{new Vector3(s*.04f,-.48f,-.48f),new Vector3(s*.2f,-.1f,-.3f),new Vector3(s*.2f,.37f,-.08f),new Vector3(s*.05f,.5f,.16f),new Vector3(-s*.05f,.18f,.45f)},new[]{new Vector2(.08f,.07f),new Vector2(.18f,.19f),new Vector2(.2f,.22f),new Vector2(.14f,.14f),new Vector2(.035f,.05f)},8);
                    shell.GetComponent<MeshRenderer>().sharedMaterial=_armor;shells.GetArrayElementAtIndex(i).objectReferenceValue=pivots[i];
                }
                data.ApplyModifiedPropertiesWithoutUndo();Set(pose,"_core",body.Find("E_Nucleo"));
                var barrel=body.Find("E_Canal_emissao");barrel.GetComponent<MeshFilter>().sharedMesh=Tube();barrel.GetComponent<MeshRenderer>().sharedMaterial=_armor;Set(pose,"_barrel",barrel);
                Object.DestroyImmediate(body.Find("E_Boca").gameObject);
                Part("Camara de emissao",barrel,new Vector3(0,0,.14f),Loft("E_Camara_interna",new[]{new Vector3(0,0,-.09f),new Vector3(0,0,.06f)},new[]{new Vector2(.08f,.08f),new Vector2(.07f,.07f)},8),_core);
            }
            PrefabUtility.SaveAsPrefabAsset(go,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(go);}
    }
}
