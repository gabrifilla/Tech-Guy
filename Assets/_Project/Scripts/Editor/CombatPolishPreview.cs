using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class CombatPolishPreview
{
    private const string Pending="TechGuy.CombatPolishPreview";
    private static int _phase;
    private static double _next;
    private static PlayerActor _player;
    private static SkillAnimationPlayer _motion;
    static CombatPolishPreview() { if(SessionState.GetBool(Pending,false)) Subscribe(); }
    public static void Run()
    {
        if (!Environment.GetCommandLineArgs().Contains("-combatPreview")) return;
        ShaderUtil.allowAsyncCompilation=false;
        var view=EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor"));
        view.position=new Rect(40,40,1280,760); view.Show(); view.Focus();
        EditorSceneManager.OpenScene(TutorialSceneBuilder.ScenePath);
        SessionState.SetBool(Pending,true); Subscribe(); EditorApplication.EnterPlaymode();
    }
    private static void Subscribe() { EditorApplication.playModeStateChanged-=State; EditorApplication.playModeStateChanged+=State; }
    private static void State(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode) return;
        _phase=0; _next=EditorApplication.timeSinceStartup+4; EditorApplication.update+=Tick;
    }
    private static T[] All<T>() where T:Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<T>(true)).ToArray();
    private static void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath,"../"+name+".png"));
    private static void Pose(SkillMotion kind,int weapon)
    {
        _motion.Release(); _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[weapon]));
        _player.GetComponent<AbilityHolder>().RefreshLoadout(); _motion.Contact(kind);
    }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup<_next || ShaderUtil.anythingCompiling) return;
        switch (_phase++)
        {
            case 0:
                _player=All<PlayerActor>().Single(); _motion=_player.GetComponent<SkillAnimationPlayer>();
                _player.GetComponent<PauseMenuUI>().Pause(); break;
            case 1: Capture("PausePreview"); break;
            case 2:
                _player.GetComponent<PauseMenuUI>().Resume();
                All<TutorialHUD>().Single().enabled=false;
                _player.GetComponent<CharControlScript>().enabled=false;
                Camera.main.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>().enabled=false;
                Camera.main.transform.position=_player.transform.position+new Vector3(3,2.8f,5);
                Camera.main.transform.LookAt(_player.transform.position+Vector3.up);
                Pose(SkillMotion.PunchLeft,0); break;
            case 3: Capture("PunchPosePreview"); break;
            case 4: Pose(SkillMotion.Slam,0); break;
            case 5: Capture("SlamPosePreview"); break;
            case 6: Pose(SkillMotion.BowShot,1); break;
            case 7: Capture("BowPosePreview"); break;
            case 8: Pose(SkillMotion.SpearThrust,2); break;
            case 9: Capture("ThrustPosePreview"); break;
            case 10: Pose(SkillMotion.SpearSweep,2); break;
            case 11: Capture("SweepPosePreview"); break;
            default:
                _motion.Release();
                Debug.Log("COMBAT_POLISH_PREVIEW_SUCCESS"); SessionState.SetBool(Pending,false);
                EditorApplication.update-=Tick; EditorApplication.Exit(0); return;
        }
        _next=EditorApplication.timeSinceStartup+3;
    }
}
