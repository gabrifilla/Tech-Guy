using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

public static class RunExp05EnvironmentAuthoring
{
    public const string Root="Assets/_Project/Art/RunExp05/Environment";
    [MenuItem("Tools/Tech Guy/RUN-EXP-05/Revisão/Ambiente04 anterior (Play)")]
    public static void Before(){if(EditorApplication.isPlaying)foreach(var view in Object.FindObjectsByType<FirstSectorEnvironmentArt>(FindObjectsSortMode.None))view.SetVisible(false);}
    [MenuItem("Tools/Tech Guy/RUN-EXP-05/Revisão/Ambiente05 arquivo (Play)")]
    public static void After(){if(EditorApplication.isPlaying)foreach(var view in Object.FindObjectsByType<FirstSectorEnvironmentArt>(FindObjectsSortMode.None))view.SetVisible(true);}
    public static void BuildAndIntegrate()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        string path=Root+"/Archive_MeshLibrary.fbx";AssetDatabase.ImportAsset(path);
        var importer=(ModelImporter)AssetImporter.GetAtPath(path);importer.isReadable=true;importer.importAnimation=false;
        importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;importer.SaveAndReimport();
        var meshes=AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<MeshFilter>().ToDictionary(m=>m.name,m=>m.sharedMesh);
        var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>(RunExp03Authoring.Content);var data=new SerializedObject(content);
        foreach(var entry in new[]{new[]{"ArchiveCover","_archiveCover"},new[]{"ArchiveServiceRack","_archiveRack"}})
        {
            string target=Root+"/"+entry[0]+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(target);
            if(!mesh){mesh=Object.Instantiate(meshes[entry[0]]);mesh.name=entry[0];AssetDatabase.CreateAsset(mesh,target);}
            data.FindProperty(entry[1]).objectReferenceValue=mesh;
        }
        data.FindProperty("_archiveMaterial").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Material>(RunExp05EnemyArtAuthoring.Root+"/Ceramica grafite cobre.mat");
        data.FindProperty("_archiveFloor").objectReferenceValue=Mat("Piso de arquivo",new Color(.115f,.14f,.15f),true);
        data.FindProperty("_archiveWall").objectReferenceValue=Mat("Estrutura de arquivo",new Color(.075f,.095f,.105f),false);
        data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();importer.isReadable=false;importer.SaveAndReimport();
    }
    private static Material Mat(string name,Color color,bool floor)
    {
        string path=Root+"/"+name+".mat";var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(mat,path);}
        mat.SetColor("_BaseColor",color);mat.SetFloat("_Smoothness",.12f);
        if(floor)
        {
            string texturePath=Root+"/ArchiveFloor.png";AssetDatabase.ImportAsset(texturePath);var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
            importer.sRGBTexture=true;importer.mipmapEnabled=true;importer.isReadable=false;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
            mat.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath));mat.SetTextureScale("_BaseMap",new Vector2(7,7));
        }
        EditorUtility.SetDirty(mat);return mat;
    }
}
