using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class EnemyCombatSetup
{
    public const string LibraryPath="Assets/_Project/ScriptableObjects/Enemies/EnemyMotions.asset";
    private const string Folder="Assets/_Project/Art/Character/Animations/Enemy";

    [MenuItem("Tools/Tech Guy/Enemies/Install Physical Attacks")]
    public static void Apply()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Exit Play Mode first.");
        for(int i=0;i<SceneManager.sceneCount;i++)
            if(SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scene edits first.");
        Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
        var right=Copy("PunchRight",AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Character/Animations/PunchRightA.anim"));
        var left=Copy("PunchLeft",AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/_Project/Art/Character/Animations/PunchLeftA.anim"));
        AnimationClip slam=CreateSlam();
        var library=AssetDatabase.LoadAssetAtPath<SkillAnimationLibrary>(LibraryPath);
        if(!library) { library=ScriptableObject.CreateInstance<SkillAnimationLibrary>(); AssetDatabase.CreateAsset(library,LibraryPath); }
        library.Configure(new[]
        {
            new SkillAnimationLibrary.Entry { motion=SkillMotion.PunchRight,clip=right,contact=.3333f },
            new SkillAnimationLibrary.Entry { motion=SkillMotion.PunchLeft,clip=left,contact=.3333f },
            new SkillAnimationLibrary.Entry { motion=SkillMotion.Slam,clip=slam,contact=.55f }
        });
        EditorUtility.SetDirty(library);
        var setup=EditorSceneManager.GetSceneManagerSetup();
        try
        {
            foreach(string guid in AssetDatabase.FindAssets("t:Prefab",new[]{"Assets/_Project/Prefabs"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var root=PrefabUtility.LoadPrefabContents(path);
                try { if(Configure(root,library)) PrefabUtility.SaveAsPrefabAsset(root,path); }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach(string guid in AssetDatabase.FindAssets("t:Scene",new[]{"Assets/_Project/Scenes"}))
            {
                var scene=EditorSceneManager.OpenScene(AssetDatabase.GUIDToAssetPath(guid));
                bool changed=false;
                foreach(var root in scene.GetRootGameObjects()) changed|=Configure(root,library);
                if(changed) EditorSceneManager.SaveScene(scene);
            }
            AssetDatabase.SaveAssets();
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(setup); }
    }
    private static bool Configure(GameObject root,SkillAnimationLibrary library)
    {
        bool changed=false;
        foreach(var ai in root.GetComponentsInChildren<EnemyAI>(true))
        {
            var actions=ai.GetComponent<EnemyCombatActions>() ?? ai.gameObject.AddComponent<EnemyCombatActions>();
            actions.ConfigureAnimations(library);
            EditorUtility.SetDirty(actions); PrefabUtility.RecordPrefabInstancePropertyModifications(actions);
            changed=true;
        }
        return changed;
    }
    private static AnimationClip Copy(string name,AnimationClip source)
    {
        if(!source) throw new InvalidOperationException("Missing enemy source motion: "+name);
        string path=Folder+"/"+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(!clip) { clip=Object.Instantiate(source); AssetDatabase.CreateAsset(clip,path); }
        clip.name=name; AnimationUtility.SetAnimationEvents(clip,Array.Empty<AnimationEvent>());
        var settings=AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime=false;
        AnimationUtility.SetAnimationClipSettings(clip,settings); EditorUtility.SetDirty(clip);
        return clip;
    }
    private static AnimationClip CreateSlam()
    {
        string path=Folder+"/GroundSlam.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip) return clip;
        var sample=PrefabUtility.LoadPrefabContents("Assets/_Project/Prefabs/EnemyVariants/Normal.prefab");
        HumanPose pose=new HumanPose();
        try
        {
            var animator=sample.GetComponent<Animator>();
            animator.runtimeAnimatorController.animationClips.First(c=>c.name=="Idle").SampleAnimation(sample,.2f);
            using(var handler=new HumanPoseHandler(animator.avatar,animator.transform)) handler.GetHumanPose(ref pose);
        }
        finally { PrefabUtility.UnloadPrefabContents(sample); }
        clip=new AnimationClip {name="GroundSlam",frameRate=60};
        for(int i=0;i<HumanTrait.MuscleCount;i++)
            clip.SetCurve("",typeof(Animator),HumanTrait.MuscleName[i],AnimationCurve.Constant(0,1,pose.muscles[i]));
        clip.SetCurve("",typeof(Animator),"RootT.y",AnimationCurve.Constant(0,1,pose.bodyPosition.y));
        clip.SetCurve("",typeof(Animator),"RootQ.w",AnimationCurve.Constant(0,1,1));
        Curve(clip,"Spine Front-Back",0,-.3f,.55f);
        Curve(clip,"Chest Front-Back",0,-.25f,.4f);
        foreach(string side in new[]{"Left","Right"})
        {
            Curve(clip,side+" Arm Down-Up",-.6f,.8f,-.5f);
            Curve(clip,side+" Arm Front-Back",.1f,.7f,.85f);
            Curve(clip,side+" Forearm Stretch",-.2f,-.4f,.75f);
        }
        AssetDatabase.CreateAsset(clip,path);
        return clip;
    }
    private static void Curve(AnimationClip clip,string property,float rest,float raised,float contact)
    {
        var curve=new AnimationCurve(new Keyframe(0,rest),new Keyframe(.3f,raised),new Keyframe(.55f,contact),new Keyframe(1,rest));
        for(int i=0;i<curve.length;i++)
        { AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto); AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto); }
        clip.SetCurve("",typeof(Animator),property,curve);
    }
}
