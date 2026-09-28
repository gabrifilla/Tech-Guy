using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

public static class CombatPolishBuilder
{
    private const string Art = "Assets/_Project/Art/Character/Animations/Skills";
    public const string LibraryPath = "Assets/_Project/Resources/Combat/SkillAnimations.asset";
    private static HumanPose _pose;

    [MenuItem("Tools/Tech Guy/Combat/Build Skill Animation Library")]
    public static void Build()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory(Art); Directory.CreateDirectory(Path.GetDirectoryName(LibraryPath)); AssetDatabase.Refresh();
        var scene=EditorSceneManager.OpenScene(CombatStudyBuilder.ScenePath);
        var source=scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<PlayerActor>()).Single();
        var sample=Object.Instantiate(source.gameObject);
        try
        {
            var animator=sample.GetComponent<Animator>();
            var idle=Clip("Assets/_Project/Art/Character/Animations/Stand--Idle.anim.fbx");
            idle.SampleAnimation(sample,.25f);
            using (var handler=new HumanPoseHandler(animator.avatar,animator.transform)) handler.GetHumanPose(ref _pose);
            var left=Copy("PunchLeft",Clip("Assets/_Project/Art/Character/Animations/PunchLeftA.anim"));
            var right=Copy("PunchRight",Clip("Assets/_Project/Art/Character/Animations/PunchRightA.anim"));
            var bow=Copy("BowShot",Clip("Assets/_ThirdParty/Blink/Art/Animations/Animations_Starter_Pack/Combat/BowShot.fbx"));
            var rain=Copy("BowRain",bow);
            // Aim up for the rain cast; preserve the source bow draw/release mechanics.
            Curve(rain,"Spine Front-Back",-.12f,-.45f,-.4f);
            Curve(rain,"Chest Front-Back",-.12f,-.35f,-.3f);
            var thrust=PoseClip("SpearThrust");
            Curve(thrust,"Chest Twist Left-Right",-.2f,-.5f,.4f);
            Curve(thrust,"Right Arm Front-Back",.1f,-.55f,.8f);
            Curve(thrust,"Right Arm Down-Up",-.6f,-.35f,-.35f);
            Curve(thrust,"Right Forearm Stretch",-.1f,-.8f,.9f);
            Curve(thrust,"Left Arm Front-Back",.4f,.25f,.8f);
            Curve(thrust,"Left Arm Down-Up",-.5f,-.4f,-.4f);
            Curve(thrust,"Left Forearm Stretch",-.3f,-.5f,.6f);
            var sweep=PoseClip("SpearSweep");
            Curve(sweep,"Chest Twist Left-Right",0,-.8f,.8f);
            Curve(sweep,"Spine Twist Left-Right",0,-.3f,.3f);
            Curve(sweep,"Right Arm Front-Back",.1f,-.8f,.9f);
            Curve(sweep,"Right Arm Down-Up",-.6f,-.1f,-.1f);
            Curve(sweep,"Right Forearm Stretch",-.2f,.65f,.9f);
            Curve(sweep,"Left Arm Front-Back",.4f,-.3f,.75f);
            Curve(sweep,"Left Arm Down-Up",-.5f,-.25f,-.25f);
            Curve(sweep,"Left Forearm Stretch",-.3f,-.2f,.5f);
            var slam=PoseClip("ShockSlam");
            Curve(slam,"Spine Front-Back",0,-.35f,.55f);
            Curve(slam,"Chest Front-Back",0,-.3f,.45f);
            foreach (string side in new[] { "Left", "Right" })
            {
                Curve(slam,side+" Arm Down-Up",-.6f,.8f,-.55f);
                Curve(slam,side+" Arm Front-Back",.1f,.75f,.85f);
                Curve(slam,side+" Forearm Stretch",-.2f,-.45f,.8f);
            }
            var library=AssetDatabase.LoadAssetAtPath<SkillAnimationLibrary>(LibraryPath);
            if (!library) { library=ScriptableObject.CreateInstance<SkillAnimationLibrary>(); AssetDatabase.CreateAsset(library,LibraryPath); }
            library.Configure(new[] { Entry(SkillMotion.PunchLeft,left,.3333f),Entry(SkillMotion.PunchRight,right,.3333f),
                Entry(SkillMotion.Slam,slam,.5f),Entry(SkillMotion.BowShot,bow,.5f),Entry(SkillMotion.BowRain,rain,.5f),
                Entry(SkillMotion.SpearThrust,thrust,.5f),Entry(SkillMotion.SpearSweep,sweep,.5f) });
            EditorUtility.SetDirty(library);
            foreach (var clip in new[] { left,right,bow,rain,thrust,sweep,slam }) EditorUtility.SetDirty(clip);
            BalanceAsura(); AssetDatabase.SaveAssets();
            Debug.Log("COMBAT_POLISH_BUILD_SUCCESS: seven skill motions, isolated events and Asura balance.");
        }
        finally { Object.DestroyImmediate(sample); }
    }
    private static SkillAnimationLibrary.Entry Entry(SkillMotion motion,AnimationClip clip,float contact) => new SkillAnimationLibrary.Entry { motion=motion,clip=clip,contact=contact };
    private static AnimationClip Clip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().First(clip=>!clip.name.StartsWith("__preview__"));
    private static AnimationClip Copy(string name,AnimationClip source)
    {
        string path=Art+"/"+name+".anim";
        var result=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (!result) { result=Object.Instantiate(source); result.name=name; AssetDatabase.CreateAsset(result,path); }
        else EditorUtility.CopySerialized(source,result);
        result.name=name; AnimationUtility.SetAnimationEvents(result,Array.Empty<AnimationEvent>());
        var settings=AnimationUtility.GetAnimationClipSettings(result); settings.loopTime=false; AnimationUtility.SetAnimationClipSettings(result,settings);
        return result;
    }
    private static AnimationClip PoseClip(string name)
    {
        string path=Art+"/"+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (!clip) { clip=new AnimationClip { name=name,frameRate=60 }; AssetDatabase.CreateAsset(clip,path); }
        clip.ClearCurves();
        for (int i=0;i<HumanTrait.MuscleCount;i++) Constant(clip,HumanTrait.MuscleName[i],_pose.muscles[i]);
        Constant(clip,"RootT.x",0); Constant(clip,"RootT.y",_pose.bodyPosition.y); Constant(clip,"RootT.z",0);
        Constant(clip,"RootQ.x",_pose.bodyRotation.x); Constant(clip,"RootQ.y",_pose.bodyRotation.y);
        Constant(clip,"RootQ.z",_pose.bodyRotation.z); Constant(clip,"RootQ.w",_pose.bodyRotation.w);
        return clip;
    }
    private static void Constant(AnimationClip clip,string property,float value) =>
        clip.SetCurve("",typeof(Animator),property,AnimationCurve.Constant(0,1,value));
    private static void Curve(AnimationClip clip,string muscle,float ready,float windup,float impact)
    {
        if (Array.IndexOf(HumanTrait.MuscleName,muscle)<0) throw new InvalidOperationException("Unknown muscle: "+muscle);
        var curve=new AnimationCurve(new Keyframe(0,ready),new Keyframe(.28f,windup),new Keyframe(.5f,impact),new Keyframe(1,ready));
        for (int i=0;i<curve.length;i++) AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
        clip.SetCurve("",typeof(Animator),muscle,curve);
    }
    private static void BalanceAsura()
    {
        var ability=AssetDatabase.LoadAssetAtPath<BreakerGauntletAbility>("Assets/_Project/ScriptableObjects/Abilities/Weapon/BreakerAsura.asset");
        ability.activeTime=1.45f; ability.cooldownTime=8;
        ability.ConfigureRunPresentation("Asura",0,SkillGlyphKind.Asura);
        var data=new SerializedObject(ability); var steps=data.FindProperty("_hitSteps"); steps.arraySize=11;
        for (int i=0;i<11;i++)
        {
            var step=steps.GetArrayElementAtIndex(i); bool final=i==10;
            step.FindPropertyRelative("delay").floatValue=final ? 1.28f : .12f+i*.105f;
            step.FindPropertyRelative("damageMultiplier").floatValue=final ? 6f : .9f;
            step.FindPropertyRelative("rangeOverride").floatValue=final ? .01f : 3.4f;
            step.FindPropertyRelative("hitShape").enumValueIndex=final ? (int)AreaHitShape.Sphere : (int)AreaHitShape.Box;
            step.FindPropertyRelative("sphereRadius").floatValue=final ? 4f : 1.5f;
            step.FindPropertyRelative("boxSize").vector3Value=new Vector3(final ? 8 : 2.4f,2,0);
            step.FindPropertyRelative("localOffset").vector3Value=final ? new Vector3(0,-3,0) : new Vector3(i%2==0 ? -.22f : .22f,0,0);
            step.FindPropertyRelative("reactionType").enumValueIndex=(int)HitReactionType.Stagger;
            step.FindPropertyRelative("hitStrength").enumValueIndex=final ? (int)HitStrength.Heavy : (int)HitStrength.Light;
            step.FindPropertyRelative("stanceDamage").floatValue=final ? 100 : 12;
            step.FindPropertyRelative("breakEffect").enumValueIndex=final ? (int)StanceBreakEffect.Stun : (int)StanceBreakEffect.None;
            step.FindPropertyRelative("stunDuration").floatValue=final ? 1.1f : .12f;
            step.FindPropertyRelative("pushDistance").floatValue=0;
        }
        data.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(ability);
    }
}
