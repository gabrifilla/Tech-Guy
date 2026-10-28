using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    // Isolated visual boundary: shipped enemy/avatar/library, autonomous AI/navigation disabled.
    // Synthetic target is not a production gameplay observation or a change to enemy balance.
    public sealed class EnemyInteractionPresentationTests
    {
        private GameObject _root;
        private EnemyCombatActions _actions;
        private Animator _animator;
        private SkillAnimationPlayer _motion;
        private Actor _target;
        private float _scale;

        [SetUp] public void Setup(){_scale=Time.timeScale;Time.timeScale=1;}
        [TearDown] public void Teardown()
        {
            if(_actions)_actions.Cancel();
            if(_root)Object.DestroyImmediate(_root);
            Time.timeScale=_scale;
        }

        private void Build(string archetype)
        {
#if UNITY_EDITOR
            _root=new GameObject("GI16 isolated presentation"); _root.SetActive(false);
            var target=new GameObject("Synthetic damage receiver");target.transform.SetParent(_root.transform);
            target.transform.localPosition=Vector3.forward*1.2f;
            _target=target.AddComponent<Actor>();_target.health=1000f;
            var collider=target.AddComponent<BoxCollider>();collider.size=new Vector3(.6f,1.8f,.6f);collider.center=Vector3.up*.9f;
            var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/"+archetype+".prefab");
            Assert.IsTrue(prefab);
            var enemy=Object.Instantiate(prefab,_root.transform);enemy.transform.localPosition=Vector3.zero;
            enemy.GetComponent<EnemyAI>().enabled=false;enemy.GetComponent<EnemyVariant>().enabled=false;
            enemy.GetComponent<NavMeshAgent>().enabled=false;
            _animator=enemy.GetComponent<Animator>();_animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
            _actions=enemy.GetComponent<EnemyCombatActions>();
            _root.SetActive(true);_animator.Rebind();_animator.Update(0);
            _motion=enemy.GetComponent<SkillAnimationPlayer>();
            Assert.IsTrue(_motion);
#else
            Assert.Ignore("Uses shipped enemy prefabs in the Unity Editor.");
#endif
        }

        private float PoseErrorAfterRender()
        {
            var arm=_animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            var forearm=_animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            Quaternion a=arm.localRotation,b=forearm.localRotation;
            _motion.ReapplyPose(); // Reference is the actual authored sample, not Animator state metadata.
            return Mathf.Max(Quaternion.Angle(a,arm.localRotation),Quaternion.Angle(b,forearm.localRotation));
        }

        private CombatReactionController ConfigureProductionReaction()
        {
            // EnemyVariant normally adds/configures this in Start. Start is disabled in this
            // isolated visual rig, so install the same component and authored profile explicitly.
            var reaction=_actions.GetComponent<CombatReactionController>() ?? _actions.gameObject.AddComponent<CombatReactionController>();
            reaction.ConfigureProfile(_actions.GetComponent<EnemyVariant>().Archetype.Profile);
            return reaction;
        }

        [UnityTest]
        public IEnumerator GruntPunchKeepsAuthoredPoseThroughWindupContactAndRecoveryRender() =>
            CheckAttackRender("Grunt",EnemyAttackKind.Punch);

        [UnityTest]
        public IEnumerator ShooterKeepsAuthoredPoseAndEmitsOneProjectileContact() =>
            CheckAttackRender("Shooter",EnemyAttackKind.AimedShot);

        private IEnumerator CheckAttackRender(string archetype,EnemyAttackKind kind)
        {
            Build(archetype);yield return null;
            int hits=0;_target.DamageReceived+=(actor,damage)=>hits++;
            _actions.StartCoroutine(_actions.Perform(kind,_target,10,()=>true,meleeWindupOverride:.4f));
            bool startup=false,recovery=false;float deadline=Time.realtimeSinceStartup+3;
            while(!_actions.Completed)
            {
                Assert.Less(Time.realtimeSinceStartup,deadline);yield return new WaitForEndOfFrame();
                if(_motion.IsPlaying && _motion.NormalizedTime>.03f)
                {
                    startup|=_actions.IsWindingUp;recovery|=!_actions.IsWindingUp;
                    float error=PoseErrorAfterRender();
                    Assert.Less(error,.1f,"Enemy pose diverged from its authored sample at render; winding="+_actions.IsWindingUp+", clip_t="+_motion.NormalizedTime+", angle="+error);
                }
            }
            Assert.IsTrue(startup);Assert.IsTrue(recovery);Assert.AreEqual(1,hits);
            Assert.AreEqual(990f,_target.health);Assert.IsFalse(_motion.IsPlaying);
        }

        [UnityTest]
        public IEnumerator GruntCancelReleasesPresentationWithoutPendingDamageOrRootMotion()
        {
            Build("Grunt");yield return null;bool rootMotion=_animator.applyRootMotion;
            Vector3 position=_actions.transform.position;
            _actions.StartCoroutine(_actions.Perform(EnemyAttackKind.Punch,_target,10,()=>true,meleeWindupOverride:.4f));
            yield return new WaitForSeconds(.15f);Assert.IsTrue(_motion.IsPlaying);
            _actions.Cancel();yield return new WaitForSeconds(.5f);
            Assert.AreEqual(1000f,_target.health);Assert.IsFalse(_motion.IsPlaying);
            Assert.IsFalse(_actions.IsWindingUp);Assert.AreEqual(rootMotion,_animator.applyRootMotion);
            Assert.AreEqual(position,_actions.transform.position);
        }

        [UnityTest]
        public IEnumerator PausedEnemyPreservesItsOwnedPoseWithoutAdvancingContact()
        {
            Build("Grunt");yield return null;
            _actions.StartCoroutine(_actions.Perform(EnemyAttackKind.Punch,_target,10,()=>true,meleeWindupOverride:.4f));
            yield return new WaitForSeconds(.15f);Time.timeScale=0;
            yield return new WaitForEndOfFrame();float phase=_motion.NormalizedTime;
            for(int i=0;i<3;i++)
            {
                yield return new WaitForEndOfFrame();
                Assert.AreEqual(phase,_motion.NormalizedTime);Assert.Less(PoseErrorAfterRender(),.1f);
                Assert.AreEqual(1000f,_target.health);
            }
            Time.timeScale=1;yield return new WaitForSeconds(.7f);
            Assert.AreEqual(990f,_target.health);Assert.IsFalse(_motion.IsPlaying);
        }

        [UnityTest]
        public IEnumerator RealGruntInterruptReleasesAttackPoseAndAllowsANewAttack()
        {
            Build("Grunt");yield return null;
            var reaction=ConfigureProductionReaction();
            _actions.StartCoroutine(_actions.Perform(EnemyAttackKind.Punch,_target,10,()=>!reaction.IsControlLocked,meleeWindupOverride:.4f));
            yield return new WaitForSeconds(.1f);Assert.IsTrue(_actions.IsWindingUp);
            // Explicit reaction fixture, same contribution/stance as the first Gauntlet basic.
            reaction.ApplyReaction(new HitReactionRequest(_target,_actions.transform.position,Vector3.forward,
                HitReactionType.Stagger,HitStrength.Light,12,StanceBreakEffect.None).WithInterruptContribution(.35f));
            yield return new WaitForEndOfFrame();
            Assert.IsFalse(_actions.IsWindingUp);Assert.IsFalse(_motion.IsPlaying);
            Assert.IsTrue(reaction.OwnsFlinchPresentation);
            Assert.AreEqual(88f,reaction.CurrentStance);Assert.IsFalse(reaction.IsControlLocked);
            yield return new WaitForSeconds(.5f);Assert.AreEqual(1000f,_target.health);
            yield return _actions.Perform(EnemyAttackKind.Punch,_target,10,()=>!reaction.IsControlLocked,meleeWindupOverride:.4f);
            Assert.AreEqual(990f,_target.health);Assert.AreEqual(1,_actions.ResolvedImpacts);
            Assert.IsTrue(_actions.Completed);Assert.IsFalse(_motion.IsPlaying);
        }

        [UnityTest]
        public IEnumerator RealGruntRecoveryPreservesCommittedContactAndItsPresentationPriority()
        {
            Build("Grunt");yield return null;
            var reaction=ConfigureProductionReaction();
            _actions.StartCoroutine(_actions.Perform(EnemyAttackKind.Punch,_target,10,()=>!reaction.IsControlLocked,meleeWindupOverride:.4f));
            float deadline=Time.realtimeSinceStartup+3;
            while(_actions.ResolvedImpacts==0){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
            Assert.AreEqual(990f,_target.health);Assert.IsFalse(_actions.IsWindingUp);
            reaction.ApplyReaction(new HitReactionRequest(_target,_actions.transform.position,Vector3.forward,
                HitReactionType.Stagger,HitStrength.Light,12,StanceBreakEffect.None).WithInterruptContribution(.35f));
            Assert.IsTrue(_actions.HasAttackPresentation);Assert.IsFalse(reaction.OwnsFlinchPresentation);
            yield return new WaitForEndOfFrame();Assert.Less(PoseErrorAfterRender(),.1f);
            while(!_actions.Completed){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
            Assert.AreEqual(1,_actions.ResolvedImpacts);Assert.AreEqual(990f,_target.health);
        }

        [UnityTest]
        public IEnumerator SniperMovingTargetDiagnosticRecordsWarningAndFlight()
        {
            Build("Sniper");yield return null;
            var diagnostic=new SniperAimDiagnostic();
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var ownerField=typeof(EnemyProjectile).GetField("_owner",flags);
            _target.DamageReceived+=(actor,damage)=>diagnostic.contacts++;
            _actions.StartCoroutine(_actions.Perform(EnemyAttackKind.SniperShot,_target,10,()=>true));
            yield return new WaitForSeconds(.3f);Assert.IsTrue(_actions.IsWindingUp);
            var warning=(EnemyAttackExecution)typeof(EnemyCombatActions).GetField("_warning",flags).GetValue(_actions);
            var outlines=(System.Collections.Generic.List<CombatGroundRing>)typeof(EnemyAttackExecution).GetField("_outlines",flags).GetValue(warning);
            Assert.AreEqual(1,outlines.Count);var line=outlines[0].GetComponent<LineRenderer>();Assert.IsTrue(line);
            float longest=0;
            for(int i=1;i<line.positionCount;i++)
            {
                Vector3 segment=line.GetPosition(i)-line.GetPosition(i-1);
                if(segment.sqrMagnitude>longest){longest=segment.sqrMagnitude;diagnostic.warning=segment.normalized;}
            }
            if(Vector3.Dot(diagnostic.warning,_actions.transform.forward)<0)diagnostic.warning=-diagnostic.warning;
            // Move only the synthetic receiver, as a player changing direction during the tell.
            _target.transform.position=Vector3.right*1.2f;Physics.SyncTransforms();
            float deadline=Time.realtimeSinceStartup+3;
            while(_actions.ResolvedImpacts==0){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
            foreach(var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                if(ReferenceEquals(ownerField.GetValue(projectile),_actions.GetComponent<Actor>()))
                {diagnostic.flight=(Vector3)typeof(EnemyProjectile).GetField("_direction",flags).GetValue(projectile);diagnostic.observed_flight=true;}
            while(!_actions.Completed){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
            Assert.IsTrue(diagnostic.observed_flight);Assert.AreEqual(1,_actions.ResolvedImpacts);
            diagnostic.angle=Vector3.Angle(diagnostic.warning,diagnostic.flight);
            // GI1.8 approves commitment at the existing visible warning. Keep old GI1.6 artifacts;
            // this continuing diagnostic now requires a coherent flight and a legitimate dodge.
            System.IO.Directory.CreateDirectory("Library/GameplayObservation/gi18");
            System.IO.File.WriteAllText("Library/GameplayObservation/gi18/continued-sniper-warning-"+System.Guid.NewGuid().ToString("N")+".json",JsonUtility.ToJson(diagnostic,true));
            Assert.Less(diagnostic.angle,.1f);Assert.AreEqual(0,diagnostic.contacts);Assert.AreEqual(1000f,_target.health);
        }

        [System.Serializable] private sealed class SniperAimDiagnostic
        {public Vector3 warning,flight;public float angle;public int contacts;public bool observed_flight;}
    }
}
