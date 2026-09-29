using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class EnemyPhysicalAttackTests
    {
        private ArsenalProjectileTestRig _rig;
        private EnemyCombatActions _actions;
        private NavMeshData _navigation;
        private NavMeshDataInstance _instance;
        [SetUp]
        public void SetUp() { _rig=new ArsenalProjectileTestRig(); }
        [TearDown]
        public void TearDown()
        {
            if(_actions) _actions.Cancel();
            _rig.TearDown();
            if(_instance.valid) _instance.Remove();
            if(_navigation) Object.DestroyImmediate(_navigation);
        }
        private void BuildAttacker(bool navigation=false)
        {
            if(navigation)
            {
                var source=new NavMeshBuildSource
                { shape=NavMeshBuildSourceShape.Box,size=new Vector3(30,.2f,30),
                    transform=Matrix4x4.TRS(Vector3.down*.1f,Quaternion.identity,Vector3.one),area=0 };
                _navigation=NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByID(0),new List<NavMeshBuildSource>{source},
                    new Bounds(Vector3.zero,new Vector3(30,5,30)),Vector3.zero,Quaternion.identity);
                Assert.That(_navigation,Is.Not.Null);
                _instance=NavMesh.AddNavMeshData(_navigation);
            }
            var owner=_rig.BuildEnemy(Vector3.zero,1000);
            if(navigation) owner.gameObject.AddComponent<NavMeshAgent>();
            _actions=owner.gameObject.AddComponent<EnemyCombatActions>();
        }

        [UnityTest]
        public IEnumerator PunchHasNoDamageBeforeContactAndCannotHitBehind()
        {
            BuildAttacker();
            var target=_rig.BuildEnemy(Vector3.forward,100);
            var routine=_actions.StartCoroutine(_actions.Perform(EnemyAttackKind.Punch,target,10,()=>true));
            yield return new WaitForSeconds(.25f);
            Assert.That(target.health,Is.EqualTo(100));
            target.transform.position=Vector3.back;
            yield return routine;
            Assert.That(target.health,Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator ChargeActuallyMovesAndHitsOnlyOnce()
        {
            BuildAttacker(true);
            var target=_rig.BuildEnemy(Vector3.forward*4,100);
            yield return _actions.Perform(EnemyAttackKind.Charge,target,10,()=>true);
            Assert.That(_actions.transform.position.z,Is.GreaterThan(3));
            Assert.That(target.health,Is.EqualTo(90));
        }

        [UnityTest]
        public IEnumerator FrostProjectileCannotHitThroughWall()
        {
            BuildAttacker();
            var target=_rig.BuildEnemy(Vector3.forward*5,100);
            _rig.BuildWall(new Vector3(0,1,2),new Vector3(4,3,.3f));
            Physics.SyncTransforms();
            yield return _actions.Perform(EnemyAttackKind.FrostBolt,target,10,()=>true);
            Assert.That(target.health,Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator FrostProjectileTravelsToTargetAndHitsOnce()
        {
            BuildAttacker();
            var target=_rig.BuildEnemy(Vector3.forward*5,100);
            var routine=_actions.StartCoroutine(_actions.Perform(EnemyAttackKind.FrostBolt,target,10,()=>true));
            yield return new WaitForSeconds(1.05f);
            Assert.That(target.health,Is.EqualTo(100),"Projectile must travel after the casting animation.");
            yield return routine;
            Assert.That(target.health,Is.EqualTo(90));
        }

        [UnityTest]
        public IEnumerator DisablingAttackerCancelsWindup()
        {
            BuildAttacker();
            var target=_rig.BuildEnemy(Vector3.forward,100);
            _actions.StartCoroutine(_actions.Perform(EnemyAttackKind.HeavySlam,target,10,()=>true));
            yield return new WaitForSeconds(.2f);
            _actions.gameObject.SetActive(false);
            yield return new WaitForSeconds(1);
            Assert.That(target.health,Is.EqualTo(100));
            Assert.That(_actions.IsWindingUp,Is.False);
        }
    }
}
