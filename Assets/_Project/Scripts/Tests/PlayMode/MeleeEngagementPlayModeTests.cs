using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    // Shipped enemy, scale, profile, colliders, root motion and agent. Synthetic flat NavMesh
    // and receiver isolate approach without changing enemy stats or teleporting it into reach.
    public sealed class MeleeEngagementPlayModeTests
    {
        private readonly List<GameObject> _objects = new List<GameObject>();
        private NavMeshData _data;
        private NavMeshDataInstance _mesh;
        private Actor _target;
        private readonly List<string> _trace = new List<string>();
        private static readonly Vector3 Center = new Vector3(4500, 0, 4500);
        private float _timeScale, _captureDelta;
        [SetUp] public void Setup() { _timeScale=Time.timeScale; _captureDelta=Time.captureDeltaTime; Time.timeScale=1; Time.captureDeltaTime=1f/60; }
        [TearDown] public void Cleanup()
        {
            Time.timeScale=_timeScale; Time.captureDeltaTime=_captureDelta;
            foreach(var go in _objects) if(go) Object.DestroyImmediate(go);
            if(_mesh.valid) _mesh.Remove(); if(_data) Object.DestroyImmediate(_data);
            _objects.Clear(); _trace.Clear();
        }
        private void BuildNavigation(bool disconnected=false, bool obstacle=false)
        {
            var sources=new List<NavMeshBuildSource>();
            if(disconnected)
            {
                sources.Add(Box(Center+Vector3.left*6,new Vector3(8,.2f,20)));
                sources.Add(Box(Center+Vector3.right*6,new Vector3(8,.2f,20)));
            }
            else sources.Add(Box(Center,new Vector3(40,.2f,40)));
            if(obstacle)
            {
                var block=GameObject.CreatePrimitive(PrimitiveType.Cube);_objects.Add(block);
                block.transform.position=Center+new Vector3(0,1,0);block.transform.localScale=new Vector3(2,2,4);
                var modifier=Box(Center+new Vector3(0,1,0),new Vector3(2,2,4));modifier.area=1;sources.Add(modifier);
            }
            _data=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),sources,
                new Bounds(Center,new Vector3(44,8,44)),Vector3.zero,Quaternion.identity);
            Assert.IsNotNull(_data);_mesh=NavMesh.AddNavMeshData(_data);
            var receiver=new GameObject("GI17 synthetic stationary receiver");_objects.Add(receiver);
            receiver.transform.position=Center+(disconnected?Vector3.right*6:obstacle?Vector3.right*3:Vector3.zero);
            receiver.layer=8;
            var collider=receiver.AddComponent<CapsuleCollider>();collider.radius=.5f;collider.height=4f;collider.center=Vector3.up*.9f;
            var targetAgent=receiver.AddComponent<NavMeshAgent>();targetAgent.radius=.5f;targetAgent.speed=5;targetAgent.Warp(receiver.transform.position);
            _target=receiver.AddComponent<Actor>();_target.health=1000;_target.SetMaxHealth(1000);
        }
        private static NavMeshBuildSource Box(Vector3 center,Vector3 size) => new NavMeshBuildSource {
            shape=NavMeshBuildSourceShape.Box,size=size,transform=Matrix4x4.TRS(center+Vector3.down*.1f,Quaternion.identity,Vector3.one),area=0 };
        private EnemyAI Spawn(string type="Grunt",Vector3? position=null)
        {
#if UNITY_EDITOR
            var parent=new GameObject("GI17 real enemy");_objects.Add(parent);parent.SetActive(false);
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/"+type+".prefab");
            Assert.IsNotNull(prefab);
            var enemy=Object.Instantiate(prefab,position??Center+new Vector3(-2.279f,0,-.01915f),Quaternion.identity,parent.transform);
            var reaction=enemy.GetComponent<CombatReactionController>() ?? enemy.AddComponent<CombatReactionController>();
            reaction.ConfigureRank(EnemyRank.Normal); // Same materialization step as ProgressionDirector.Manual.
            var ai=enemy.GetComponent<EnemyAI>();ai.player=_target.transform;ai.whatIsPlayer=1<<8;
            enemy.GetComponent<Animator>().cullingMode=AnimatorCullingMode.AlwaysAnimate;
            parent.SetActive(true);Assert.IsTrue(ai.agent.Warp(enemy.transform.position));
            Set(ai,"_rng",new System.Random(1701+2*7919));Physics.SyncTransforms();return ai;
#else
            Assert.Ignore("Requires shipped prefab via Editor.");return null;
#endif
        }
        private static void Set(object obj,string key,object value) => obj.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).SetValue(obj,value);
        private static T Get<T>(object obj,string key) => (T)obj.GetType().GetField(key,BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).GetValue(obj);
        private static float Range(EnemyAI ai) => Mathf.Min(ai.attackRange,1.45f*Mathf.Clamp(ai.transform.lossyScale.y,.5f,2)+.35f);
        private static float[] V(Vector3 v) => new[]{v.x,v.y,v.z};
        private void Sample(EnemyAI ai,float time)
        {
            var layer=ai.GetComponent<PreferredDistanceLayer>();Vector3 suggestion=Vector3.zero;
            bool suggested=layer && layer.TryGetPositioningTarget(out suggestion);
            var actions=ai.GetComponent<EnemyCombatActions>();
            _trace.Add(JsonUtility.ToJson(new Trace {t=time,distance=Vector3.Distance(ai.transform.position,_target.transform.position),range=Range(ai),eligible=ai.playerInAttackRange,
                position=V(ai.transform.position),target=V(_target.transform.position),preferred=V(suggestion),suggested=suggested,
                velocity=V(ai.agent.velocity),desired=V(ai.agent.desiredVelocity),destination=V(ai.agent.destination),
                remaining=ai.agent.remainingDistance,status=ai.agent.pathStatus.ToString(),winding=ai.IsWindingUp,
                completed=actions.Completed,impacts=actions.ResolvedImpacts,busy=Get<Coroutine>(ai,"_attackRoutine")!=null,
                facing=EnemyFacing.IsFacing(ai.transform.forward,_target.transform.position-ai.transform.position)}));
        }
        [Serializable] private sealed class Trace { public float t,distance,range,remaining; public bool eligible,suggested,winding,completed,busy,facing; public int impacts; public string status; public float[] position,target,preferred,velocity,desired,destination; }
        private void Save(string label)
        {
            Directory.CreateDirectory("Library/GameplayObservation/gi17");
            File.WriteAllText("Library/GameplayObservation/gi17/"+label+"-"+Guid.NewGuid().ToString("N")+".json","["+string.Join(",",_trace)+"]");
        }
        [UnityTest] public IEnumerator ProductionGruntApproachesHitsRecoversAndAttacksAgain()
        {
            BuildNavigation();var ai=Spawn();yield return null;
            Assert.AreEqual(.5f,ai.transform.lossyScale.y,.001f);
            int hits=0,windups=0,finished=0;bool wasWinding=false,wasBusy=false,entered=false;float damage=0;
            _target.DamageReceived+=(actor,amount)=>{hits++;damage+=amount;};
            float start=Time.time;
            while(Time.time-start<12)
            {
                yield return null;Sample(ai,Time.time-start);
                bool winding=ai.IsWindingUp,busy=Get<Coroutine>(ai,"_attackRoutine")!=null;
                if(winding&&!wasWinding){windups++;Assert.LessOrEqual(Vector3.Distance(ai.transform.position,_target.transform.position),Range(ai)+.05f);}
                if(!busy&&wasBusy)finished++;
                entered|=ai.playerInAttackRange;wasWinding=winding;wasBusy=busy;
                if(finished>=2)break;
            }
            Save("stationary");
            Assert.IsTrue(entered,"Production Grunt never entered its actual attack range from the production node-1 separation.");
            Assert.GreaterOrEqual(windups,2,"Must start another legal windup after recovery.");
            Assert.GreaterOrEqual(finished,2);Assert.AreEqual(windups,hits,"One HP transaction per connected authored punch.");
            Assert.AreEqual(ai.attackDamage*hits,damage,.001f);Assert.IsNull(Get<Coroutine>(ai,"_attackRoutine"));
            Assert.IsFalse(ai.GetComponent<Actor>().IsDead);Assert.AreEqual(60,ai.GetComponent<Actor>().health);
        }
        [UnityTest] public IEnumerator PreferredPositioningAblationRecordsTheSameGrunt()
        {
            BuildNavigation();
            // Diagnostic assistance only: independent fresh enemies, removing suggestions then wander.
            // Never moves the enemy or changes its damage/range/collider/avoidance/root motion.
            for(int stage=0;stage<3;stage++)
            {
                var ai=Spawn();yield return null;
                if(stage>=1) ai.GetComponent<PreferredDistanceLayer>().enabled=false;
                if(stage==2){var config=Get<EnemyAI.NaturalBehaviorConfig>(ai,"_natural");config.ApproachOffset=0;Set(ai,"_natural",config);}
                float start=Time.time;_trace.Clear();
                while(Time.time-start<6){yield return null;Sample(ai,Time.time-start);}
                Save("ablation-"+stage);Object.DestroyImmediate(ai.transform.parent.gameObject);yield return null;
            }
        }
        [UnityTest] public IEnumerator MovingTargetCanEvadeCommittedPunchAndBePursuedAgain()
        {
            BuildNavigation();var ai=Spawn();yield return null;
            int hits=0;_target.DamageReceived+=(actor,amount)=>hits++;
            float deadline=Time.time+6;
            while(!ai.IsWindingUp && Time.time<deadline)yield return null;
            Assert.IsTrue(ai.IsWindingUp);int first=ai.GetComponent<EnemyCombatActions>().ResolvedImpacts;
            Assert.IsTrue(_target.GetComponent<NavMeshAgent>().SetDestination(Center+Vector3.right*4));
            while(ai.GetComponent<EnemyCombatActions>().ResolvedImpacts==first && Time.time<deadline)yield return null;
            Assert.AreEqual(first+1,ai.GetComponent<EnemyCombatActions>().ResolvedImpacts);
            Assert.AreEqual(0,hits,"Moving outside the committed cone must still evade the authored punch.");
            deadline=Time.time+10;
            while(hits<1 && Time.time<deadline){yield return null;Sample(ai,Time.time);}
            Save("moving");Assert.GreaterOrEqual(hits,1,"Pursuit must resume after the player stops.");
        }
        [UnityTest] public IEnumerator SeveralMeleeEnemiesDoNotAllRemainOutsideReach()
        {
            BuildNavigation();var enemies=new[]{Spawn(position:Center+new Vector3(-3,0,-2)),Spawn(position:Center+Vector3.left*4),Spawn(position:Center+new Vector3(-3,0,2))};
            yield return null;int hits=0;_target.DamageReceived+=(actor,amount)=>hits++;
            float end=Time.time+10;var active=new HashSet<EnemyAI>();
            while(Time.time<end)
            {
                yield return null;foreach(var ai in enemies)if(ai.GetComponent<EnemyCombatActions>().ResolvedImpacts>0)active.Add(ai);
                if(active.Count>=2&&hits>=2)break;
            }
            foreach(var ai in enemies)Sample(ai,Time.time);Save("multiple");
            Assert.GreaterOrEqual(active.Count,2);Assert.GreaterOrEqual(hits,2);
        }
        [UnityTest] public IEnumerator MixedMeleeAndRangedKeepTheirExistingAttackOwners()
        {
            BuildNavigation();var grunt=Spawn();var shooter=Spawn("Shooter",Center+Vector3.forward*6);
            yield return null;float end=Time.time+10;
            while(Time.time<end && (grunt.GetComponent<EnemyCombatActions>().ResolvedImpacts<2 || shooter.GetComponent<EnemyCombatActions>().ResolvedImpacts<2))yield return null;
            Sample(grunt,Time.time);Sample(shooter,Time.time);Save("mixed");
            Assert.GreaterOrEqual(grunt.GetComponent<EnemyCombatActions>().ResolvedImpacts,2);
            Assert.GreaterOrEqual(shooter.GetComponent<EnemyCombatActions>().ResolvedImpacts,2);
            Assert.IsTrue(float.IsPositiveInfinity(shooter.PositioningAttackRange));
        }
        [UnityTest] public IEnumerator ReachableObstacleRequiresTheExistingCompleteNavMeshRoute()
        {
            BuildNavigation(obstacle:true);var ai=Spawn(position:Center+Vector3.left*3);yield return null;
            var path=new NavMeshPath();Assert.IsTrue(ai.agent.CalculatePath(_target.transform.position,path));
            Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);Assert.Greater(path.corners.Length,2);
            bool wentAround=false;float end=Time.time+14;int hits=0;_target.DamageReceived+=(actor,amount)=>hits++;
            while(Time.time<end && hits<1){yield return null;wentAround|=Mathf.Abs(ai.transform.position.z-Center.z)>2;Sample(ai,Time.time);}
            Save("obstacle");Assert.IsTrue(wentAround);Assert.GreaterOrEqual(hits,1);
        }
        [UnityTest] public IEnumerator DisconnectedTargetDoesNotGrantRangeOrBypassNavigation()
        {
            BuildNavigation(disconnected:true);var ai=Spawn(position:Center+Vector3.left*6);yield return null;
            float end=Time.time+6;while(Time.time<end){yield return null;Sample(ai,Time.time);}
            Save("unreachable");Assert.AreEqual(0,ai.GetComponent<EnemyCombatActions>().ResolvedImpacts);
            Assert.AreEqual(1000,_target.health);Assert.Less(ai.transform.position.x,Center.x);
        }
        [UnityTest] public IEnumerator OnlyNormalSmallGruntReceivesTheApproachConstraint()
        {
            BuildNavigation();var ai=Spawn();yield return null;
            var reaction=ai.GetComponent<CombatReactionController>();Assert.IsFalse(float.IsInfinity(ai.PositioningAttackRange));
            reaction.ConfigureRank(EnemyRank.Elite);Assert.IsTrue(float.IsInfinity(ai.PositioningAttackRange));
            reaction.ConfigureRank(EnemyRank.Legendary);Assert.IsTrue(float.IsInfinity(ai.PositioningAttackRange));
            reaction.ConfigureRank(EnemyRank.Boss);Assert.IsTrue(float.IsInfinity(ai.PositioningAttackRange));
            var heavy=Spawn("Heavy",Center+Vector3.back*6);yield return null;Assert.IsTrue(float.IsInfinity(heavy.PositioningAttackRange));
        }
        [UnityTest] public IEnumerator TargetLossDuringWindupCancelsWithoutDamageAndReleasesBusy()
        {
            BuildNavigation();var ai=Spawn();yield return null;float end=Time.time+6;
            while(!ai.IsWindingUp && Time.time<end)yield return null;
            Assert.IsTrue(ai.IsWindingUp);ai.player.gameObject.SetActive(false);
            yield return new WaitForSeconds(1);
            Assert.AreEqual(1000,_target.health);Assert.AreEqual(0,ai.GetComponent<EnemyCombatActions>().ResolvedImpacts);
            Assert.IsNull(Get<Coroutine>(ai,"_attackRoutine"));Assert.IsFalse(ai.IsWindingUp);
        }
        [UnityTest] public IEnumerator RepeatedNonlethalGetHitStillAllowsFutureAttacks()
        {
            BuildNavigation();var ai=Spawn();yield return null;
            var enemy=ai.GetComponent<Actor>();var reaction=ai.GetComponent<CombatReactionController>();
            for(int i=0;i<2;i++)
            {
                enemy.TakeDamage(10);
                reaction.ApplyReaction(new HitReactionRequest(_target,ai.transform.position,Vector3.right,
                    HitReactionType.Stagger,HitStrength.Light,12,StanceBreakEffect.None).WithInterruptContribution(.35f));
                yield return new WaitForSeconds(.1f);
            }
            Assert.IsFalse(enemy.IsDead);Assert.AreEqual(40,enemy.health);Assert.IsFalse(reaction.IsControlLocked);
            float end=Time.time+10;while(Time.time<end && ai.GetComponent<EnemyCombatActions>().ResolvedImpacts<2)yield return null;
            Assert.GreaterOrEqual(ai.GetComponent<EnemyCombatActions>().ResolvedImpacts,2);
        }
        [UnityTest] public IEnumerator TargetDeathDuringWindupPreventsPendingEmission()
        {
            BuildNavigation();var ai=Spawn();yield return null;float end=Time.time+6;
            while(!ai.IsWindingUp && Time.time<end)yield return null;
            Assert.IsTrue(ai.IsWindingUp);_target.TakeDamage(1000);
            yield return new WaitForSeconds(1);
            Assert.AreEqual(0,ai.GetComponent<EnemyCombatActions>().ResolvedImpacts);
            Assert.IsNull(Get<Coroutine>(ai,"_attackRoutine"));Assert.IsFalse(ai.IsWindingUp);
        }
    }
}
