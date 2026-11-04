using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunExp09BAuthoring
{
    public static void EnableProduction()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var current=SceneManager.GetActiveScene();if(current.isDirty)throw new InvalidOperationException("Unsaved scene");
        var scene=EditorSceneManager.OpenScene("Assets/_Project/Scenes/FirstSector.unity",OpenSceneMode.Additive);
        try
        {
            var environment=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ManualStageEnvironment>(true)).Single();
            var so=new SerializedObject(environment);so.FindProperty("_useSpatialOrientations").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();EditorSceneManager.SaveScene(scene);
            var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
            EditorUtility.SetDirty(content);AssetDatabase.SaveAssetIfDirty(content);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(current);}
    }
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/Next run/Seed 5")]
    public static void Seed5()=>Next(5);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/Next run/Seed 6")]
    public static void Seed6()=>Next(6);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/Clear next seed")]
    public static void Clear()=>SessionState.EraseString(RunSeedSource.NextEditorSeedKey);
    public static void Next(ulong seed){SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());Debug.Log("09B next run seed="+seed+"; consumed once. Normal Nexus portal; no save write.");}
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/L/0") ] public static void L0()=>NextVariant(FirstSectorRoomFamily.LChamber,0);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/L/90") ] public static void L1()=>NextVariant(FirstSectorRoomFamily.LChamber,1);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/L/180") ] public static void L2()=>NextVariant(FirstSectorRoomFamily.LChamber,2);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/L/270") ] public static void L3()=>NextVariant(FirstSectorRoomFamily.LChamber,3);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/Courtyard/0") ] public static void C0()=>NextVariant(FirstSectorRoomFamily.AsymmetricCourtyard,0);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/Courtyard/90") ] public static void C1()=>NextVariant(FirstSectorRoomFamily.AsymmetricCourtyard,1);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/Courtyard/180") ] public static void C2()=>NextVariant(FirstSectorRoomFamily.AsymmetricCourtyard,2);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09B/Courtyard/270") ] public static void C3()=>NextVariant(FirstSectorRoomFamily.AsymmetricCourtyard,3);
    public static ulong VariantSeed(FirstSectorRoomFamily family,int turns)
    {
        var c=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
        var d=AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");var n=d.Nodes.First(x=>x.Id==1);
        for(ulong seed=0;seed<10000;seed++)if(FirstSectorRoomLayout.SelectFamily(seed,1)==family&&FirstSectorRoomDistribution.Orientation(seed,1,family,new Vector2(22,18))==turns&&c.HasCover(seed,n))return seed;
        throw new InvalidOperationException("No review seed in bounded search");
    }
    private static void NextVariant(FirstSectorRoomFamily family,int turns)=>Next(VariantSeed(family,turns));
}
