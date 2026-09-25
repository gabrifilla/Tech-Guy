using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class FirstSectorValidation
{
    private const string Pending = "TechGuy.FirstSectorValidation";
    private static int _phase, _errors, _room;
    private static double _next;
    private static PlayerActor _player;
    private static FirstSectorDirector _director;
    private static DashScript _modifiedDash;
    private static Actor _target;
    private static float _healthBefore;
    static FirstSectorValidation() { if(SessionState.GetBool(Pending,false)) Subscribe(); }
    public static void Run() { _next=EditorApplication.timeSinceStartup+20;EditorApplication.update+=Wait; }
    private static void Wait()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.timeSinceStartup<_next)return;
        EditorApplication.update-=Wait;EditorSceneManager.OpenScene(LobbySceneBuilder.ScenePath);
        SessionState.SetBool(Pending,true);Subscribe();EditorApplication.EnterPlaymode();
    }
    private static void Subscribe() { EditorApplication.playModeStateChanged-=OnState;EditorApplication.playModeStateChanged+=OnState; }
    private static void OnState(PlayModeStateChange state)
    {
        if(state!=PlayModeStateChange.EnteredPlayMode)return;
        _phase=_errors=_room=0;_next=EditorApplication.timeSinceStartup+3;
        Application.logMessageReceived+=OnLog;EditorApplication.update+=Tick;
    }
    private static void OnLog(string message,string stack,LogType type)
    { if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)_errors++; }
    private static T[] Components<T>() where T:Component=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<T>()).ToArray();
    private static void Require(bool value,string message) { if(!value)throw new Exception(message); }
    private static void Advance(int phase,double delay=.6) { _phase=phase;_next=EditorApplication.timeSinceStartup+delay; }
    private static void Warp(Vector3 point) { Require(_player.GetComponent<NavMeshAgent>().Warp(point),"Could not warp to "+point); }
    private static void Tick()
    {
        if(EditorApplication.timeSinceStartup<_next)return;
        try
        {
            if(_phase==0)
            {
                _player=Components<PlayerActor>().Single();var portal=Components<ScenePortal>().Single();
                Require(portal.Destination=="FirstSector","Lobby portal has wrong destination");
                Warp(portal.transform.position);Advance(1,3);
            }
            else if(_phase==1)
            {
                Require(SceneManager.GetActiveScene().name=="FirstSector","Walking into lobby portal did not load level");
                _player=Components<PlayerActor>().Single();_director=Components<FirstSectorDirector>().Single();
                Require(Components<PlayerHUD>().Single().Player==_player,"First sector HUD is not bound");
                Require(Components<EnemyAI>().Length==0,"Encounters started before approaching");
                var data=new SerializedObject(_player);data.FindProperty("_manaRegenerationPercentPerSecond").floatValue=0;data.ApplyModifiedPropertiesWithoutUndo();
                _player.TrySpendMana(_player.mana);
                var holder=_player.GetComponent<AbilityHolder>();var dash=_player.GetComponent<CharControlScript>().dashScript;
                Require(dash.ManaCost==0&&holder.TryUseDash(dash),"Base dash failed with zero mana");
                Require(_player.mana==0,"Base dash changed mana");
                Require(!holder.TryUseDash(dash),"Repeated dash accepted during active dash");
                _modifiedDash=Object.Instantiate(dash);var config=new SerializedObject(_modifiedDash);config.FindProperty("_manaCost").floatValue=60;config.ApplyModifiedPropertiesWithoutUndo();
                Advance(2);
            }
            else if(_phase==2)
            {
                var holder=_player.GetComponent<AbilityHolder>();
                Require(!holder.TryUseDash(_modifiedDash),"Modified dash ignored insufficient mana");
                _player.RestoreMana(120);Require(holder.TryUseDash(_modifiedDash),"Funded modified dash rejected");
                Require(Mathf.Abs(_player.mana-60)<.01f,"Modified dash cost incorrect");
                Advance(3);
            }
            else if(_phase==3)
            {
                Object.Destroy(_modifiedDash);Warp(new Vector3(-4,.1f,59));Advance(4);
            }
            else if(_phase==4)
            {
                Require(SceneManager.GetActiveScene().name=="FirstSector"&&!_director.IsComplete,"Extraction unlocked before clearing level");
                Warp(new Vector3(0,.1f,-4));Advance(5,1);
            }
            else if(_phase==5)
            {
                var enemies=Components<EnemyAI>();Require(enemies.Length==3,"First encounter did not activate exactly three enemies");
                foreach(var enemy in enemies) { Require(enemy.agent.isOnNavMesh,"Enemy not on NavMesh");enemy.enabled=false;enemy.agent.ResetPath(); }
                _target=enemies[0].GetComponent<Actor>();_healthBefore=_target.health;
                _player.RestoreMana(_player.maxMana);
                Require(_player.GetComponent<AbilityHolder>().TryUseAbility(1),"Flurry failed");
                Require(Mathf.Abs(_player.mana-(_player.maxMana-150))<.01f,"Flurry cost not applied exactly once");
                Require(_target.GetComponent<NavMeshAgent>().Warp(_player.transform.position+_player.transform.forward*1.5f),"Cannot place damage target");
                Advance(6,.28);
            }
            else if(_phase==6)
            {
                Require(Components<AttackAreaSwoosh>().Length==0,"Gauntlet still creates duplicate generic effect");
                Capture("FirstSector-combat");Advance(7,1.2);
            }
            else if(_phase==7)
            {
                Require(!_target||_target.health<_healthBefore,"Flurry did not damage target");
                Require(Components<GauntletImpactVfx>().Length==0,"Temporary punch effects leaked");
                foreach(var enemy in Components<EnemyAI>())enemy.GetComponent<Actor>().TakeDamage(100000);
                Require(_director.ClearedEncounters==1,"First encounter did not finish");
                Require(_director.Boons.IsChoosing && _director.Boons.Choose(1),"First room reward missing");
                _room=1;Warp(new Vector3(6,.1f,23));Advance(8,1);
            }
            else if(_phase==8)
            {
                var enemies=Components<EnemyAI>();Require(enemies.Length==(_room==1?4:3),"Wrong finite encounter size");
                foreach(var enemy in enemies) { Require(enemy.agent.isOnNavMesh,"Enemy spawn off NavMesh");enemy.enabled=false;enemy.agent.ResetPath(); }
                if(_room==2)Capture("FirstSector-guardian");
                foreach(var enemy in enemies)enemy.GetComponent<Actor>().TakeDamage(100000);
                Require(_director.ClearedEncounters==_room+1,"Encounter did not progress");
                Require(_director.Boons.IsChoosing && _director.Boons.Choose(1),"Room reward missing");
                if(_room==1) { _room=2;Warp(new Vector3(-4,.1f,48));Advance(8,1); }
                else { Require(_director.IsComplete,"Level did not complete");Warp(new Vector3(-4,.1f,59));Advance(9,3); }
            }
            else if(_phase==9)
            {
                Require(SceneManager.GetActiveScene().name=="NexusLobby","Extraction did not return to lobby");
                _player=Components<PlayerActor>().Single();Warp(Components<ScenePortal>().Single().transform.position);Advance(10,3);
            }
            else if(_phase==10)
            {
                Require(SceneManager.GetActiveScene().name=="FirstSector","Could not replay level");
                _player=Components<PlayerActor>().Single();Require(!Components<FirstSectorDirector>().Single().IsComplete,"Encounter progress leaked across runs");
                _player.TakeDamage(100000);Advance(11,4);
            }
            else if(_phase==11)
            {
                Require(SceneManager.GetActiveScene().name=="NexusLobby","Death did not return to lobby");
                Require(_errors==0,"Runtime errors: "+_errors);
                Debug.Log("FIRST_SECTOR_PLAYMODE_SUCCESS: portal entry, HUD, free/modified dash, locked extraction, flurry damage and cleanup, 3 finite encounters, victory, replay and death return.");Finish(0);
            }
        }
        catch(Exception exception) { Debug.LogException(exception);Finish(1); }
    }
    private static void Capture(string name)
    {
        var camera=Camera.main;var canvases=Components<Canvas>().Where(c=>c.renderMode==RenderMode.ScreenSpaceOverlay).ToArray();
        var target=new RenderTexture(1600,900,24);var image=new Texture2D(1600,900,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;camera.targetTexture=target;
        foreach(var canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1; }
        Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
        image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();Directory.CreateDirectory("Docs");File.WriteAllBytes("Docs/"+name+".png",image.EncodeToPNG());
        foreach(var canvas in canvases)canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        camera.targetTexture=null;RenderTexture.active=previous;Object.DestroyImmediate(image);target.Release();Object.DestroyImmediate(target);
    }
    private static void Finish(int code)
    { SessionState.SetBool(Pending,false);EditorApplication.update-=Tick;Application.logMessageReceived-=OnLog;EditorApplication.Exit(code); }
}
