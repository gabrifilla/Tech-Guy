using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunExp05ProfileBuild
{
    public static void Build()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var active=SceneManager.GetActiveScene();if(active.isDirty)throw new InvalidOperationException("Save current scene before isolated profiling build");
        string scenePath="Assets/_Project/Scenes/Review/RunExp05Profile.unity";var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        try
        {
            var go=new GameObject("Explicit standalone profiling harness");SceneManager.MoveGameObjectToScene(go,scene);var harness=go.AddComponent<RunExp05StandaloneProfile>();var data=new SerializedObject(harness);
            foreach(var version in new[]{"04","05"})
            {
                var array=data.FindProperty(version=="04"?"_oldArt":"_newArt");array.arraySize=2;int i=0;
                foreach(string kind in new[]{"Fragmento","Emissor"})array.GetArrayElementAtIndex(i++).objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FirstSector/RunExp"+version+"/"+kind+".prefab");
            }
            data.FindProperty("_grunt").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Grunt.prefab");data.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(scene,scenePath);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(active);}
        string firstSector="Assets/_Project/Scenes/Review/RunExp05ProfileFirstSector.unity";
        if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(firstSector)&&!AssetDatabase.CopyAsset("Assets/_Project/Scenes/FirstSector.unity",firstSector))throw new Exception("Cannot create diagnostic scene copy");
        var copy=EditorSceneManager.OpenScene(firstSector,OpenSceneMode.Additive);
        try
        {
            foreach(var root in copy.GetRootGameObjects())foreach(var source in root.GetComponentsInChildren<RunSeedSource>(true))
            {var seed=new SerializedObject(source);seed.FindProperty("_randomizeEachRun").boolValue=false;seed.FindProperty("_seed").stringValue="0";seed.ApplyModifiedPropertiesWithoutUndo();}
            EditorSceneManager.SaveScene(copy);
        }
        finally{EditorSceneManager.CloseScene(copy,true);SceneManager.SetActiveScene(active);}
        string company=PlayerSettings.companyName,product=PlayerSettings.productName;bool timing=PlayerSettings.enableFrameTimingStats;
        Directory.CreateDirectory("Builds/RunExp05Profile");
        try
        {
            PlayerSettings.companyName="TechGuyReviewLocal";PlayerSettings.productName="RUNEXP05Profile";PlayerSettings.enableFrameTimingStats=true;
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{scenePath,firstSector,"Assets/_Project/Scenes/FirstSector.unity","Assets/_Project/Scenes/NexusBlockout.unity"},locationPathName="Builds/RunExp05Profile/TechGuyProfile.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            File.WriteAllText("Docs/validation/artifacts/run-exp-05/standalone-build.json",JsonUtility.ToJson(new BuildResultInfo{result=report.summary.result.ToString(),errors=report.summary.totalErrors,warnings=report.summary.totalWarnings,seconds=report.summary.totalTime.TotalSeconds,bytes=report.summary.totalSize,backend=PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone).ToString()},true));
            if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Standalone profile build failed; inspect preserved report");
        }
        finally{PlayerSettings.companyName=company;PlayerSettings.productName=product;PlayerSettings.enableFrameTimingStats=timing;AssetDatabase.SaveAssets();}
    }
    [Serializable] private sealed class BuildResultInfo{public string result,backend;public int errors,warnings;public double seconds;public ulong bytes;}
}
