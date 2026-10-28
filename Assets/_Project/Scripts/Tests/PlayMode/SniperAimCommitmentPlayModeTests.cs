using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    // Shipped Sniper, motion library, authored damage/profile and reaction materialization.
    // Autonomous AI/Nav are disabled to isolate execution; receiver/positions are assisted fixtures.
    public sealed class SniperAimCommitmentPlayModeTests
    {
        private GameObject _root;
        private Actor _target;
        private EnemyCombatActions _actions;
        private float _scale,_delta;
        private readonly List<EnemyCombatActions> _owners=new List<EnemyCombatActions>();
        private static readonly Vector3 Center=new Vector3(4800,0,4800);
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        [SetUp] public void Setup(){_scale=Time.timeScale;_delta=Time.captureDeltaTime;Time.timeScale=1;Time.captureDeltaTime=1f/60;}
        [TearDown] public void Cleanup()
        {
            foreach(var owner in _owners)if(owner){owner.Cancel();foreach(var projectile in Owned(owner))if(projectile)Object.DestroyImmediate(projectile.gameObject);}
            if(_root)Object.DestroyImmediate(_root);_owners.Clear();Time.timeScale=_scale;Time.captureDeltaTime=_delta;
        }
        private void Build()
        {
            _root=new GameObject("GI18 Sniper execution fixture");_root.transform.position=Center;_root.SetActive(false);
            var receiver=new GameObject("Synthetic receiver");receiver.transform.SetParent(_root.transform,false);receiver.transform.localPosition=Vector3.forward*8;
            _target=receiver.AddComponent<Actor>();_target.health=1000;_target.SetMaxHealth(1000);
            var capsule=receiver.AddComponent<CapsuleCollider>();capsule.height=1.8f;capsule.radius=.5f;capsule.center=Vector3.up*.9f;
            _actions=AddSniper(Vector3.zero);_root.SetActive(true);
        }
        private EnemyCombatActions AddSniper(Vector3 position)
        {
#if UNITY_EDITOR
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Sniper.prefab");Assert.IsNotNull(prefab);
            var staging=new GameObject("Sniper materialization");staging.transform.SetParent(_root.transform,false);staging.SetActive(false);
            var go=Object.Instantiate(prefab,staging.transform);go.transform.localPosition=position;
            go.GetComponent<EnemyAI>().player=_target.transform;go.GetComponent<EnemyAI>().enabled=false;go.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled=false;
            var reaction=go.GetComponent<CombatReactionController>()??go.AddComponent<CombatReactionController>();reaction.ConfigureRank(EnemyRank.Normal);
            go.GetComponent<Animator>().cullingMode=AnimatorCullingMode.AlwaysAnimate;
            var actions=go.GetComponent<EnemyCombatActions>();_owners.Add(actions);staging.SetActive(true);return actions;
#else
            Assert.Ignore("Loads the shipped prefab through the Unity Editor.");return null;
#endif
        }
        private void Fire(EnemyCombatActions actions=null)
        {
            actions=actions??_actions;
            actions.StartCoroutine(actions.Perform(EnemyAttackKind.SniperShot,_target,actions.GetComponent<EnemyAI>().attackDamage,
                ()=>_target&&!_target.IsDead&&!actions.GetComponent<CombatReactionController>().IsControlLocked));
        }
        private static EnemyProjectile[] Owned(EnemyCombatActions owner) => Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
            .Where(p=>ReferenceEquals(typeof(EnemyProjectile).GetField("_owner",Private).GetValue(p),owner.GetComponent<Actor>())).ToArray();
        private static List<CombatGroundRing> Warnings(EnemyCombatActions actions) => (List<CombatGroundRing>)typeof(EnemyAttackExecution)
            .GetField("_outlines",Private).GetValue(typeof(EnemyCombatActions).GetField("_warning",Private).GetValue(actions));
        private static Vector3 WarningDirection(EnemyCombatActions actions)
        {
            Assert.AreEqual(1,Warnings(actions).Count);var line=Warnings(actions)[0].GetComponent<LineRenderer>();float longest=0;Vector3 result=Vector3.zero;
            for(int i=1;i<line.positionCount;i++){var segment=line.GetPosition(i)-line.GetPosition(i-1);if(segment.sqrMagnitude>longest){longest=segment.sqrMagnitude;result=segment.normalized;}}
            return Vector3.Dot(result,actions.transform.forward)<0?-result:result;
        }
        private IEnumerator AwaitEmission(EnemyCombatActions actions=null)
        {
            actions=actions??_actions;float end=Time.time+2;
            while(actions.ResolvedImpacts==0&&Time.time<end)yield return null;
            Assert.AreEqual(1,actions.ResolvedImpacts);Assert.AreEqual(1,Owned(actions).Length,"One actual projectile must accompany the emission.");
        }
        [Serializable] private sealed class AimEvidence {public Vector3 warning,flight;public float angle,damage;public int emissions,contacts;}
        private static void Save(AimEvidence evidence,string label)
        {
            Directory.CreateDirectory("Library/GameplayObservation/gi18");File.WriteAllText("Library/GameplayObservation/gi18/"+label+"-"+Guid.NewGuid().ToString("N")+".json",JsonUtility.ToJson(evidence,true));
        }
        [UnityTest] public IEnumerator MovingAfterVisibleWarningCannotRedirectTheActualFlight()
        {
            Build();yield return null;int contacts=0;float damage=0;_target.DamageReceived+=(actor,amount)=>{contacts++;damage+=amount;};
            Fire();yield return new WaitForSeconds(.3f);Assert.IsTrue(_actions.IsWindingUp);
            Vector3 warning=WarningDirection(_actions);_target.transform.position=Center+Vector3.right*8;Physics.SyncTransforms();
            yield return AwaitEmission();var projectile=Owned(_actions)[0];Vector3 start=projectile.transform.position;
            yield return null;Assert.IsTrue(projectile&&projectile.isActiveAndEnabled);Vector3 flight=(projectile.transform.position-start).normalized;
            yield return new WaitForSeconds(.6f);
            var evidence=new AimEvidence {warning=warning,flight=flight,angle=Vector3.Angle(warning,flight),emissions=_actions.ResolvedImpacts,contacts=contacts,damage=damage};Save(evidence,"lateral");
            Assert.Less(evidence.angle,.1f,"The actual projectile trajectory must follow the visible warning.");
            Assert.AreEqual(0,contacts,"Leaving the announced line is a valid dodge.");Assert.AreEqual(1000,_target.health);
            Assert.IsTrue(_actions.Completed);Assert.AreEqual(0,Warnings(_actions).Count);
        }
        private IEnumerator CheckStationary(bool movedBeforeCommit)
        {
            Build();yield return null;
            if(movedBeforeCommit){_target.transform.position=Center+Vector3.right*8;Physics.SyncTransforms();}
            int hits=0;float damage=0;_target.DamageReceived+=(actor,amount)=>{hits++;damage+=amount;};
            Fire();yield return new WaitForSeconds(.2f);Vector3 warning=WarningDirection(_actions);
            Assert.Less(Vector3.Angle(warning,movedBeforeCommit?Vector3.right:Vector3.forward),.1f);
            yield return AwaitEmission();var projectile=Owned(_actions)[0];Vector3 before=projectile.transform.position;yield return null;
            Vector3 flight=(projectile.transform.position-before).normalized;yield return new WaitForSeconds(.6f);
            Save(new AimEvidence{warning=warning,flight=flight,angle=Vector3.Angle(warning,flight),emissions=_actions.ResolvedImpacts,contacts=hits,damage=damage},movedBeforeCommit?"before-commit":"stationary");
            Assert.Less(Vector3.Angle(warning,flight),.1f);Assert.AreEqual(1,hits);
            Assert.AreEqual(_actions.GetComponent<EnemyAI>().attackDamage,damage,.001f);Assert.IsTrue(_actions.Completed);
        }
        [UnityTest] public IEnumerator StationaryTargetReceivesExactlyOneAuthoredShot() => CheckStationary(false);
        [UnityTest] public IEnumerator MovementBeforeActionIsCapturedByTheFirstVisibleWarning() => CheckStationary(true);
        [UnityTest] public IEnumerator EligibleStaggerBeforeEmissionRemovesWarningAndPendingProjectile()
        {
            Build();yield return null;Fire();yield return new WaitForSeconds(.3f);
            Assert.AreEqual(1,Warnings(_actions).Count);Assert.IsTrue(_actions.IsWindingUp);
            _actions.GetComponent<CombatReactionController>().ApplyReaction(new HitReactionRequest(_target,_actions.transform.position,Vector3.forward,
                HitReactionType.Stagger,HitStrength.Light,12,StanceBreakEffect.None).WithInterruptContribution(.35f));
            yield return new WaitForSeconds(1.1f);
            Assert.AreEqual(0,Warnings(_actions).Count);Assert.AreEqual(0,_actions.ResolvedImpacts);
            Assert.AreEqual(0,Owned(_actions).Length);Assert.AreEqual(1000,_target.health);
        }
        [UnityTest] public IEnumerator CancellationAfterEmissionPreservesTheSingleExistingFlight()
        {
            Build();yield return null;int contacts=0;_target.DamageReceived+=(actor,damage)=>contacts++;
            Fire();yield return AwaitEmission();var flight=Owned(_actions)[0];_actions.Cancel();
            Assert.IsTrue(flight&&flight.isActiveAndEnabled);Assert.AreEqual(0,Warnings(_actions).Count);
            yield return new WaitForSeconds(.7f);
            Assert.AreEqual(1,contacts);Assert.AreEqual(1,_actions.ResolvedImpacts);Assert.AreEqual(0,Owned(_actions).Length);
        }
        [UnityTest] public IEnumerator PausePreservesWarningAndAimThenResumesTheCommittedShot()
        {
            Build();yield return null;Fire();yield return new WaitForSeconds(.3f);Vector3 warning=WarningDirection(_actions);
            Time.timeScale=0;_target.transform.position=Center+Vector3.right*8;Physics.SyncTransforms();
            yield return new WaitForSecondsRealtime(.06f);
            Assert.IsTrue(_actions.IsWindingUp);Assert.AreEqual(0,_actions.ResolvedImpacts);
            Assert.Less(Vector3.Angle(warning,WarningDirection(_actions)),.1f);Time.timeScale=1;
            yield return AwaitEmission();var p=Owned(_actions)[0];Vector3 start=p.transform.position;yield return null;
            Assert.Less(Vector3.Angle(warning,p.transform.position-start),.1f);
        }
        [UnityTest] public IEnumerator HitstopDoesNotRetargetOrAdvanceTheShotToEmission()
        {
            Build();yield return null;Fire();yield return new WaitForSeconds(.3f);Vector3 warning=WarningDirection(_actions);
            var runner=_root.AddComponent<HitStopRunner>();runner.Apply(.06f);Assert.Less(Time.timeScale,.001f);
            _target.transform.position=Center+Vector3.right*8;Physics.SyncTransforms();yield return null;
            Assert.AreEqual(0,_actions.ResolvedImpacts);Assert.Less(Vector3.Angle(warning,WarningDirection(_actions)),.1f);
            yield return new WaitForSecondsRealtime(.09f);Assert.AreEqual(1,Time.timeScale);
            yield return AwaitEmission();var p=Owned(_actions)[0];Vector3 start=p.transform.position;yield return null;
            Assert.Less(Vector3.Angle(warning,p.transform.position-start),.1f);
        }
        private IEnumerator InvalidTarget(bool destroyed)
        {
            Build();yield return null;Fire();yield return new WaitForSeconds(.3f);Assert.AreEqual(1,Warnings(_actions).Count);
            if(destroyed)Object.DestroyImmediate(_target.gameObject);else _target.TakeDamage(1000);
            yield return new WaitForSeconds(1.2f);
            Assert.AreEqual(0,Warnings(_actions).Count);Assert.AreEqual(0,Owned(_actions).Length);Assert.AreEqual(0,_actions.ResolvedImpacts);
            Assert.IsFalse(_actions.IsWindingUp);Assert.IsFalse(_actions.GetComponent<SkillAnimationPlayer>().IsPlaying);
        }
        [UnityTest] public IEnumerator TargetDeathCancelsWithoutEmissionOrStaleWarning() => InvalidTarget(false);
        [UnityTest] public IEnumerator TargetDestructionCancelsWithoutEmissionOrStaleWarning() => InvalidTarget(true);
        [UnityTest] public IEnumerator ConcurrentSnipersKeepIndependentWarningAndFlightSnapshots()
        {
            Build();var second=AddSniper(Vector3.right*2);yield return null;
            Fire();yield return new WaitForSeconds(.2f);Vector3 firstWarning=WarningDirection(_actions);
            _target.transform.position=Center+new Vector3(7,0,8);Physics.SyncTransforms();Fire(second);
            yield return new WaitForSeconds(.2f);Vector3 secondWarning=WarningDirection(second);
            Assert.Greater(Vector3.Angle(firstWarning,secondWarning),15);
            yield return AwaitEmission();var a=Owned(_actions)[0];Vector3 a0=a.transform.position;yield return null;
            Assert.Less(Vector3.Angle(firstWarning,a.transform.position-a0),.1f);
            yield return AwaitEmission(second);var b=Owned(second)[0];Vector3 b0=b.transform.position;yield return null;
            Assert.Less(Vector3.Angle(secondWarning,b.transform.position-b0),.1f);
            Assert.AreEqual(1,_actions.ResolvedImpacts);Assert.AreEqual(1,second.ResolvedImpacts);
        }
        [UnityTest] public IEnumerator DirectionCommitmentIsOptInAndDoesNotLeakAcrossReconfiguration()
        {
            Build();yield return null;
            var owner=_actions.GetComponent<Actor>();
            var locked=EnemyProjectile.Spawn(Center,Vector3.right,owner,_target,12,24,26,.35f,EnemyProjectile.PathKind.Straight,Color.red,lockDirection:true);
            Vector3 start=locked.transform.position;yield return null;
            Assert.Less(Vector3.Angle(Vector3.right,locked.transform.position-start),.1f);
            // Explicitly reconfigure the same instance, as Configure does on every pool acquire.
            locked.transform.position=Center;locked.transform.rotation=Quaternion.LookRotation(Vector3.right);
            locked.Configure(owner,_target,12,24,26,.35f,EnemyProjectile.PathKind.Straight,Color.red);
            var legacy=locked;start=legacy.transform.position;yield return null;
            Assert.Less(Vector3.Angle(Vector3.forward,legacy.transform.position-start),.1f,"Default callers must preserve their current target-based setup.");
        }
    }
}
