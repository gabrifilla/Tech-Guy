using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RunExp09Authoring
{
    public const string ReviewScene="Assets/_Project/Scenes/Review/RunExp09SpatialReview.unity";
    public static void PrepareReview()
    {
        if(!AssetDatabase.LoadAssetAtPath<SceneAsset>(ReviewScene)&&!AssetDatabase.CopyAsset("Assets/_Project/Scenes/FirstSector.unity",ReviewScene))throw new Exception("Cannot copy review baseline");
        SetEnabled(ReviewScene,true);
    }
    public static void EnableProduction() => SetEnabled("Assets/_Project/Scenes/FirstSector.unity",true);
    private static void SetEnabled(string path,bool enabled)
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var current=SceneManager.GetActiveScene();if(current.isDirty)throw new InvalidOperationException("Unsaved active scene");
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
        try
        {
            var owner=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ManualStageEnvironment>(true)).Single();
            var serialized=new SerializedObject(owner);serialized.FindProperty("_useSpatialLayouts").boolValue=enabled;serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene);
        }
        finally{EditorSceneManager.CloseScene(scene,true);SceneManager.SetActiveScene(current);}
    }
    [MenuItem("Tools/Tech Guy/RUN-EXP-09/Open spatial comparison")]
    public static void OpenReview()=>EditorSceneManager.OpenScene(ReviewScene);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09/Next run/Courtyard")]
    public static void Courtyard()=>Next(FirstSectorRoomFamily.AsymmetricCourtyard);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09/Next run/L Chamber")]
    public static void L()=>Next(FirstSectorRoomFamily.LChamber);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09/Next run/Gallery")]
    public static void Gallery()=>Next(FirstSectorRoomFamily.ElongatedGallery);
    [MenuItem("Tools/Tech Guy/RUN-EXP-09/Next run/Ring")]
    public static void Ring()=>Next(FirstSectorRoomFamily.FragmentedRing);
    private static void Next(FirstSectorRoomFamily family)
    {
        ulong seed=0;while(FirstSectorRoomLayout.SelectFamily(seed,1)!=family&&seed<1000)seed++;
        SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());Debug.Log("RUN-EXP-09 next run seed="+seed+" family="+family+"; one-shot SessionState, no save change.");
    }
}
