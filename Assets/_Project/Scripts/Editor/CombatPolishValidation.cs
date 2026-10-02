using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

[InitializeOnLoad]
public static class CombatPolishValidation
{
    private const string Pending="TechGuy.CombatPolishValidation";
    private static int _phase,_checks,_errors,_hits,_beforeHits;
    private static double _next;
    private static float _clock,_health,_mana;
    private static Vector3 _position,_hand;
    private static PlayerActor _player;
    private static AbilityHolder _holder;
    private static PauseMenuUI _pause;
    private static BreakerGauntletCombat _breaker;
    private static SkillAnimationPlayer _animation;
    private static TutorialTrainingTarget _target;
    private static RunBoons _run;
    static CombatPolishValidation() { if (SessionState.GetBool(Pending,false)) Subscribe(); }
    public static void Run()
    {
        EditorSceneManager.OpenScene(TutorialSceneBuilder.ScenePath);
        foreach (var director in Components<TutorialDirector>()) director.enabled=false;
        foreach (var hud in Components<TutorialHUD>()) hud.enabled=false;
        SessionState.SetBool(Pending,true); Subscribe(); EditorApplication.EnterPlaymode();
    }
    private static T[] Components<T>() where T:Component => SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<T>(true)).ToArray();
    private static void Subscribe() { EditorApplication.playModeStateChanged-=State; EditorApplication.playModeStateChanged+=State; }
    private static void State(PlayModeStateChange state)
    {
        if (state!=PlayModeStateChange.EnteredPlayMode) return;
        _phase=_checks=_errors=_hits=0; _next=EditorApplication.timeSinceStartup+2;
        Application.logMessageReceived+=Log; EditorApplication.update+=Tick;
    }
    private static void Log(string message,string stack,LogType type) { if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert) _errors++; }
    private static void Require(bool value,string message) { _checks++; if(!value) throw new Exception(message); }
    private static void Near(float actual,float expected,string message) => Require(Mathf.Abs(actual-expected)<.015f,message+" ("+actual+" vs "+expected+")");
    private static void Next(double wait=.3) { _phase++; _next=EditorApplication.timeSinceStartup+wait; }
    private static void Charge()
    {
        var momentum=(AsuraMomentum)typeof(BreakerGauntletCombat).GetField("_momentum",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(_breaker);
        momentum.AddEnergy(100); _holder.RefreshLoadout();
        Aim();
    }
    private static void Aim()
    {
        // Warp-based fixtures must settle the follow camera before projecting a screen cursor.
        var follow=Camera.main.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
        if (follow) typeof(TechGuy.Cameras.TG_TopDown_Camera).GetMethod("HandleCamera",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(follow,null);
        if (Mouse.current==null) InputSystem.AddDevice<Mouse>();
        InputState.Change(Mouse.current,new MouseState { position=Camera.main.WorldToScreenPoint(new Vector3(0,.1f,3)) });
    }
    private static void OnHit(Actor actor,float damage) { if(damage>0) _hits++; }
    private static void Tick()
    {
        if (EditorApplication.timeSinceStartup<_next) return;
        try
        {
            switch(_phase)
            {
                case 0:
                    _player=Components<PlayerActor>().Single(); _holder=_player.GetComponent<AbilityHolder>();
                    _pause=_player.GetComponent<PauseMenuUI>();
                    _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); _holder.RefreshLoadout();
                    _breaker=_player.GetComponent<BreakerGauntletCombat>(); _animation=_player.GetComponent<SkillAnimationPlayer>();
                    _target=Components<TutorialTrainingTarget>().Single(); _target.gameObject.SetActive(true); _target.DamageReceived+=OnHit;
                    _player.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.1f,-1));
                    var library=Resources.Load<SkillAnimationLibrary>(SkillAnimationLibrary.ResourcePath);
                    foreach (SkillMotion motion in Enum.GetValues(typeof(SkillMotion)))
                    {
                        var entry=library.Get(motion);
                        Require(entry!=null && entry.clip && entry.clip.humanMotion,"Humanoid clip for "+motion);
                        Require(AnimationUtility.GetAnimationEvents(entry.clip).Length==0,"No duplicate damage events: "+motion);
                        Vector3 before=_player.transform.position;
                        _animation.Contact(motion);
                        Require(_animation.IsPlaying && _animation.CurrentMotion==motion,"Real playable: "+motion);
                        Near(_animation.NormalizedTime,entry.contact,"Contact pose: "+motion);
                        Require(Vector3.Distance(before,_player.transform.position)<.01f,"No root-motion teleport: "+motion);
                    }
                    _animation.Release(); Require(!_animation.IsPlaying,"Playable releases correctly");
                    Aim(); Require(_holder.TryUseAbility(0),"Q starts"); Next(.08); break;
                case 1:
                    Require(_holder.IsCasting && _animation.IsPlaying,"Skill owns animation during cast");
                    _pause.Pause(); _clock=Time.time; _mana=_player.mana; _beforeHits=_hits;
                    _position=_player.transform.position; _hand=_player.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand).position;
                    Require(_pause.IsPaused && Time.timeScale==0 && AudioListener.pause,"Pause freezes clock and audio");
                    Require(!_holder.TryUseAbility(1),"Paused skill rejected");
                    Require(!_holder.TryUseDash(_player.GetComponent<CharControlScript>().dashScript),"Paused dash rejected");
                    Require(!_player.GetComponent<CharControlScript>().TryBasicAttack(_target.transform.position),"Paused basic rejected");
                    Next(.6); break;
                case 2:
                    Near(Time.time,_clock,"Clock stays frozen"); Near(_player.mana,_mana,"No mana consumed during pause");
                    Require(_hits==_beforeHits,"No hits land during pause");
                    Require(Vector3.Distance(_position,_player.transform.position)<.01f,"Actor stays still");
                    Require(Vector3.Distance(_hand,_player.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.RightHand).position)<.01f,"Pose stays frozen");
                    _pause.Resume(); Require(Time.timeScale==1 && !AudioListener.pause,"Resume restores clock and audio"); Next(1); break;
                case 3:
                    Require(!_holder.IsCasting && !_animation.IsPlaying,"Q recovers cleanly"); Require(_hits==2,"Exactly two Q hits without legacy events");
                    // Asura is paid with earned energy, even with an empty mana pool.
                    Charge(); _player.mana=0; _beforeHits=_hits;
                    Require(_holder.TryUseAbility(3),"Charged Asura starts at zero mana");
                    // Task 15.3 (R3.8): the Asura dash-cancel window is now governed by its authored Dash
                    // CancelRule (start 0.05) resolved through the shared CancelResolver, not the former
                    // hidden _elapsed >= 0.25 s exception. At the very first tick (progress < 0.05) the
                    // Dash rule is still closed, so CanDodgeCancel — which now reflects the authored rule,
                    // not a 0.25 s gate — reports false. (Avoid calling TryUseDash here: a temporal miss
                    // buffers the intent, which would fire into the next phase.)
                    Require(_breaker.IsAsuraActive && !_breaker.CanDodgeCancel,"Startup cannot be dash-cancelled before the authored Dash window opens");
                    Next(.35); break;
                case 4:
                    Require(_breaker.IsAsuraActive && _breaker.CanDodgeCancel,"Asura dodge window opens (authored Dash rule governs it)");
                    _player.RestoreHealthToMax(); _health=_player.health;
                    float expected=_player.Stats.ReduceIncomingDamage(50)*.15f;
                    _player.TakeDamage(50); Near(_health-_player.health,expected,"Asura reduces damage by 85 percent");
                    int beforeFinisher=_hits;
                    Require(_holder.TryUseDash(_player.GetComponent<CharControlScript>().dashScript),"Dash cashes out Asura");
                    Require(!_breaker.IsExecuting && !_animation.IsPlaying,"Cancel releases pose and casting lock");
                    Require(_hits==beforeFinisher+1,"Early finisher lands exactly once"); _beforeHits=_hits;
                    _player.RestoreHealthToMax(); _health=_player.health;
                    _player.TakeDamage(20); Near(_health-_player.health,_player.Stats.ReduceIncomingDamage(20),"Protection removed after cancel"); Next(2); break;
                case 5:
                    Require(_hits==_beforeHits,"Cancelled barrage cannot continue hitting");
                    _player.GetComponent<NavMeshAgent>().Warp(new Vector3(0,.1f,1));
                    Charge(); _beforeHits=_hits;
                    Require(_holder.TryUseAbility(3),"Second full Asura starts");
                    Next(1.8); break;
                case 6:
                    Require(!_breaker.IsExecuting && !_animation.IsPlaying,"Full Asura releases within 1.8 seconds");
                    Require(_hits-_beforeHits==11,"Full burst has ten punches and one finisher; observed "+(_hits-_beforeHits));
                    Require(!_player.GetComponent<NavMeshAgent>().isStopped,"Navigation restored");
                    _player.RestoreMana(_player.maxMana); Charge(); Require(_holder.TryUseAbility(3),"Third burst starts");
                    _breaker.enabled=false;
                    Require(!_breaker.IsExecuting && !_animation.IsPlaying,"Disabling cast cleans up graph");
                    _player.RestoreHealthToMax(); _health=_player.health; _player.TakeDamage(20);
                    Near(_health-_player.health,_player.Stats.ReduceIncomingDamage(20),"Disabling cleans up protection");
                    _breaker.enabled=true;
                    _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1])); _holder.RefreshLoadout();
                    Require(_holder.TryUseAbility(0),"Bow Q starts"); Next(.08); break;
                case 7:
                    Require(_animation.IsPlaying && _animation.CurrentMotion==SkillMotion.BowShot,"Bow draws instead of punching"); Next(2); break;
                case 8:
                    _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[2])); _holder.RefreshLoadout();
                    Require(_holder.TryUseAbility(1),"Spear sweep starts"); Next(.08); break;
                case 9:
                    Require(_animation.CurrentMotion==SkillMotion.SpearSweep,"Sweep uses horizontal weapon motion"); Next(2); break;
                case 10:
                    _run=_player.gameObject.AddComponent<RunBoons>(); Next(); break;
                case 11:
                    _run.OfferReward(1); _pause.Pause();
                    Require(_run.IsChoosing && !_run.Choose(0),"Pause prevents reward selection underneath");
                    _pause.Resume(); Next(); break;
                case 12:
                    Require(_run.IsChoosing && _run.Choose(0),"Reward remains selectable after resume");
                    _pause.Pause(); _pause.ReturnToMainMenu(); Next(2); break;
                case 13:
                    Require(SceneManager.GetActiveScene().name=="MainMenu","Paused exit reaches main menu");
                    Require(Time.timeScale==1 && !AudioListener.pause,"Scene change does not leave game paused");
                    Require(_errors==0,"No runtime errors: "+_errors);
                    Debug.Log("COMBAT_POLISH_VALIDATION_SUCCESS: "+_checks+" checks."); Finish(0); break;
            }
        }
        catch(Exception e) { Debug.LogException(e); Finish(1); }
    }
    private static void Finish(int code)
    {
        if (_pause) _pause.Resume();
        SessionState.SetBool(Pending,false); Application.logMessageReceived-=Log;
        EditorApplication.update-=Tick; EditorApplication.Exit(code);
    }
}
