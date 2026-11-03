using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

/// <summary>Imports actual Blender-authored FBX meshes into copied native enemy hierarchies.</summary>
public static class RunExp05EnemyArtAuthoring
{
    public const string Root="Assets/_Project/Art/RunExp05/Enemies";
    public const string Prefabs="Assets/_Project/Prefabs/FirstSector/RunExp05";
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        Directory.CreateDirectory(Root+"/Meshes");Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
        var color=Texture("Character_BaseColor",true,false);var packed=Texture("Character_MetallicSmoothness",false,false);var normal=Texture("Character_Normal",false,true);
        Material armor=Material("Ceramica grafite cobre",color,packed,normal,false),core=Material("Nucleo contido",color,packed,normal,true);
        foreach(string name in new[]{"Fragmento","Emissor"})
        {
            string fbxPath=Root+"/"+name+"_MeshLibrary.fbx";AssetDatabase.ImportAsset(fbxPath);
            var importer=(ModelImporter)AssetImporter.GetAtPath(fbxPath);importer.isReadable=true;importer.importAnimation=false;
            importer.materialImportMode=ModelImporterMaterialImportMode.None;importer.importNormals=ModelImporterNormals.Import;importer.importTangents=ModelImporterTangents.CalculateMikk;
            importer.meshCompression=ModelImporterMeshCompression.Off;importer.SaveAndReimport();
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            var library=model.GetComponentsInChildren<MeshFilter>(true).ToDictionary(f=>f.name,f=>f.sharedMesh,StringComparer.Ordinal);
            string path=Prefabs+"/"+name+".prefab";
            if(!AssetDatabase.LoadAssetAtPath<GameObject>(path)&&!AssetDatabase.CopyAsset("Assets/_Project/Prefabs/FirstSector/RunExp04/"+name+".prefab",path))throw new Exception("Cannot copy native04 candidate");
            var go=PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach(var filter in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    // Preserve hierarchy, transforms and every native gameplay component.
                    string key=filter.sharedMesh.name.Replace("_DCC05","");
                    if(!library.TryGetValue(key,out var imported))throw new Exception("Missing imported mesh: "+key);
                    if(imported.subMeshCount!=1)throw new Exception("One material slot required: "+key);
                    string meshPath=Root+"/Meshes/"+key+".asset";var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(!mesh){mesh=Object.Instantiate(imported);mesh.name=key+"_DCC05";AssetDatabase.CreateAsset(mesh,meshPath);}
                    filter.sharedMesh=mesh;
                    filter.GetComponent<MeshRenderer>().sharedMaterial=(key=="E_Nucleo"||key=="E_Camara_interna")?core:armor;
                }
                var pose=new SerializedObject(go.GetComponent<FirstSectorEnemyPose>());pose.FindProperty("_laminatedPresentation").boolValue=true;pose.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(go,path);
            }
            finally{PrefabUtility.UnloadPrefabContents(go);}
            // Runtime references the baked meshes. Keep source FBX free of a CPU-readable duplicate.
            importer.isReadable=false;importer.SaveAndReimport();
        }
        AssetDatabase.SaveAssets();
    }
    private static Texture2D Texture(string name,bool srgb,bool normal)
    {
        string path=Root+"/"+name+".png";AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType=normal?TextureImporterType.NormalMap:TextureImporterType.Default;importer.sRGBTexture=srgb;
        importer.mipmapEnabled=true;importer.isReadable=false;importer.alphaIsTransparency=false;importer.maxTextureSize=1024;
        importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.filterMode=FilterMode.Trilinear;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    private static Material Material(string name,Texture2D color,Texture2D packed,Texture2D normal,bool core)
    {
        string path=Root+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(!material){material=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name};AssetDatabase.CreateAsset(material,path);}
        material.SetColor("_BaseColor",Color.white);material.SetTexture("_BaseMap",color);material.SetTexture("_MetallicGlossMap",packed);material.SetFloat("_Smoothness",1);
        material.EnableKeyword("_METALLICSPECGLOSSMAP");material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",.35f);material.EnableKeyword("_NORMALMAP");
        if(core){material.SetTexture("_EmissionMap",color);material.SetColor("_EmissionColor",new Color(.08f,.25f,.22f));material.EnableKeyword("_EMISSION");}
        EditorUtility.SetDirty(material);return material;
    }
    public static void Integrate()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>(RunExp03Authoring.Content);var data=new SerializedObject(content);var entries=data.FindProperty("_additionalEnemies");
        int i=0;foreach(string name in new[]{"Fragmento","Emissor"}){var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/"+name+".prefab");if(!prefab)throw new Exception("Build/validate art first");entries.GetArrayElementAtIndex(i++).objectReferenceValue=prefab;}
        data.ApplyModifiedPropertiesWithoutUndo();AssetDatabase.SaveAssets();
    }
}
