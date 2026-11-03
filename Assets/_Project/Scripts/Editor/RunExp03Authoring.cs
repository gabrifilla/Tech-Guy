using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunExp03Authoring
{
    public const string Root="Assets/_Project/Art/RunExp03";
    public const string Prefabs="Assets/_Project/Prefabs/FirstSector/RunExp03";
    public const string Content="Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset";
    [MenuItem("Tools/Tech Guy/RUN-EXP-03/Próxima incursão com seed 0")]
    public static void NextSeed0(){SessionState.SetString(RunSeedSource.NextEditorSeedKey,"0");Debug.Log("Próxima incursão: seed0, uma única vez. Outras runs mantêm sorteio normal.");}
    [MenuItem("Tools/Tech Guy/RUN-EXP-03/Próxima incursão com seed 63 (cobertura e dupla)")]
    public static void NextSeed63(){SessionState.SetString(RunSeedSource.NextEditorSeedKey,"63");Debug.Log("Próxima incursão: seed63, uma única vez; roster e salas sorteados normalmente por essa seed.");}
    public static void Integrate()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required.");
        Directory.CreateDirectory(Root);Directory.CreateDirectory(Prefabs);AssetDatabase.Refresh();
        foreach(string name in new[]{"Fragmento","Emissor"})
        {
            string path=Prefabs+"/"+name+".prefab";
            if(!File.Exists(path) && !AssetDatabase.CopyAsset(RunExp02Builder.Root+"/"+name+".prefab",path))throw new Exception("Prefab copy failed: "+name);
        }
        var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>(Content);
        if(!content){content=ScriptableObject.CreateInstance<FirstSectorContentDefinition>();AssetDatabase.CreateAsset(content,Content);}
        var data=new SerializedObject(content);data.FindProperty("_coverChance").floatValue=.45f;
        var array=data.FindProperty("_additionalEnemies");array.arraySize=2;
        array.GetArrayElementAtIndex(0).objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Fragmento.prefab");
        array.GetArrayElementAtIndex(1).objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+"/Emissor.prefab");data.ApplyModifiedPropertiesWithoutUndo();
        var source=EditorSceneManager.OpenScene(FirstSectorBuilder.ScenePath,OpenSceneMode.Additive);
        var director=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ProgressionDirector>(true)).Single();
        var so=new SerializedObject(director);so.FindProperty("_firstSectorContent").objectReferenceValue=content;so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(source);EditorSceneManager.CloseScene(source,true);AssetDatabase.SaveAssets();
    }
}
