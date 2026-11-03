using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        private IEnumerator VerifyMixedHookRecovery(ProgressionDirector director,ManualNodeDefinition node,PlayerActor player,int count)
        {
            var hookers=director.GetComponentsInChildren<EnemyVariant>().Where(v=>v.ArchetypeId==ArchetypeId.Hooker).ToArray();Assert.AreEqual(count,hookers.Length);
            // Explicit positional fixture in the entered room: use the open right lane for actual connections.
            for(int i=0;i<hookers.Length;i++){hookers[i].GetComponent<EnemyAI>().InterruptAttack();Assert.IsTrue(hookers[i].GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(7-i*.3f,0,-1+i*3.5f)));}
            var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;bool keyboardEnabled=oldKeyboard!=null&&oldKeyboard.enabled,mouseEnabled=oldMouse!=null&&oldMouse.enabled;
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;Keyboard keyboard=null;Mouse mouse=null;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                if(keyboardEnabled)InputSystem.DisableDevice(oldKeyboard);if(mouseEnabled)InputSystem.DisableDevice(oldMouse);keyboard=InputSystem.AddDevice<Keyboard>("RUNEXP04 mixed hook fixture");mouse=InputSystem.AddDevice<Mouse>("RUNEXP04 mixed hook fixture");keyboard.MakeCurrent();mouse.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                yield return null;yield return null;
                float deadline=Time.time+12;
                while(!player.IsBeingPulled||hookers.Any(h=>h.GetComponent<EnemyCombatActions>().ResolvedImpacts==0)){Assert.Less(Time.time,deadline,"Actual HookLine connections/emissions must occur in the mixed room.");yield return null;}
                float lockedAt=Time.time;deadline=Time.time+2;
                while(player.IsBeingPulled){Assert.Less(Time.time,deadline,"Repeated native hooks must release player control.");yield return null;}
                float release=Time.time-lockedAt;var controls=player.GetComponent<CharControlScript>();InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;controls.SendMessage("OnApplicationFocus",true);
                Key right;Assert.IsTrue(Enum.TryParse(GamePreferences.Binding(GameControl.MoveRight).ToString(),true,out right),"Current movement binding must be delivered by the keyboard fixture.");
                Vector3 start=player.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(right));yield return new WaitForSeconds(.15f);
                Assert.IsFalse(player.IsBeingPulled,"Recovery must contain an actionable movement window.");float moved=Vector3.Distance(start,player.transform.position);Assert.Greater(moved,.15f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState{position=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.back*8)});yield return null;yield return null;
                var dash=controls.dashScript;Assert.IsNotNull(dash);Assert.IsTrue(player.GetComponent<AbilityHolder>().TryUseDash(dash));yield return new WaitForSeconds(.08f);Assert.IsTrue(dash.IsDashing(player.gameObject));
                yield return Capture("04-mixed-hooks-"+count+"-seed-"+director.ManualSession.Route.RewardSeed+"-action-window");
                deadline=Time.time+2;while(dash.IsDashing(player.gameObject)){Assert.Less(Time.time,deadline);yield return null;}
                File.WriteAllText(Path.Combine(_dir,"04-hook-recovery-"+director.ManualSession.Route.RewardSeed+"-"+count+".txt"),"One/two real Hookers mixed with production candidates; initial right-lane positioning explicit; AI/damage active, no immunity. Native pull observed; bounded release="+release+"s; real rebound movement="+moved+"m; native dash active observed. Not physical game-feel acceptance.");
            }
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboardEnabled)InputSystem.EnableDevice(oldKeyboard);if(mouseEnabled)InputSystem.EnableDevice(oldMouse);oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;
            }
        }
    }
}
