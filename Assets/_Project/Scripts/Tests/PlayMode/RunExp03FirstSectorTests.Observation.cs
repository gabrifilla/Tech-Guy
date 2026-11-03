using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        private readonly HashSet<string> _poseCaptures=new HashSet<string>();
        private bool _losVerified;
        private IEnumerator ObserveNativePoses(ProgressionDirector director,ManualNodeDefinition node)
        {
            var poses=director.GetComponentsInChildren<FirstSectorEnemyPose>();
            var rows=new List<string>{"time,node,enemy,phase,x,z,foot_drift"};float until=Time.time+4.5f;
            while(Time.time<until)
            {
                foreach(var pose in poses.Where(p=>p))
                {
                    string name=pose.Ranged?"Emissor":"Fragmento";
                    rows.Add(FormattableString.Invariant($"{Time.time},{node.Id},{name},{pose.Phase},{pose.transform.position.x},{pose.transform.position.z},{pose.MaximumPlantedFootDrift}"));
                    string key=name+"-"+pose.Phase;
                    if(_poseCaptures.Add(key))yield return Capture("natural-"+key+"-node-"+node.Id);
                    Assert.IsFalse(float.IsNaN(pose.MaximumPlantedFootDrift));
                    Assert.Less(pose.MaximumPlantedFootDrift,.04f,"Planted feet must remain within 4cm of their world anchors.");
                }
                yield return null;
            }
            File.WriteAllLines(Path.Combine(_dir,"poses-node-"+node.Id+".csv"),rows);
        }
        private IEnumerator VerifyEmitterCover(ProgressionDirector director,ManualNodeDefinition node,PlayerActor player)
        {
            var emitter=director.GetComponentsInChildren<FirstSectorEnemyPose>().FirstOrDefault(p=>p.Ranged);if(!emitter)yield break;
            _losVerified=true;var agent=emitter.GetComponent<NavMeshAgent>();var actions=emitter.GetComponent<EnemyCombatActions>();var ai=emitter.GetComponent<EnemyAI>();
            Vector3 oldPlayer=player.transform.position,oldEnemy=emitter.transform.position;
            ai.InterruptAttack();Assert.IsTrue(agent.Warp(node.Center+new Vector3(-3,0,-4.7f)));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(-3,0,1.8f)));
            Physics.SyncTransforms();int impacts=actions.ResolvedImpacts;
            // Positional LOS fixture within a legitimately entered room. AI and other enemies stay active.
            yield return new WaitForSeconds(1.5f);Assert.AreEqual(impacts,actions.ResolvedImpacts,"Cover must prevent the committed ranged attack.");
            yield return Capture("emissor-blocked-by-cover-node-"+node.Id);
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(-7,0,-4.7f)));
            float end=Time.time+10;
            while(actions.ResolvedImpacts==impacts){Assert.Less(Time.time,end,"Emissor must reacquire after line of sight returns.");yield return null;}
            yield return Capture("emissor-reacquired-los-node-"+node.Id);
            Assert.IsTrue(agent.Warp(oldEnemy));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(oldPlayer));
            File.WriteAllText(Path.Combine(_dir,"los-node-"+node.Id+".txt"),"Fixture posicional explícita; Emissor real de roster natural. AI ativa. Contador bloqueado="+impacts+"; após LOS="+actions.ResolvedImpacts+"; jogador imune apenas na fixture de rota.");
        }
    }
}
