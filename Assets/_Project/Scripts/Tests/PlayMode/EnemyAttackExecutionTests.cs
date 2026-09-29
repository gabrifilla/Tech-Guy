using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class EnemyAttackExecutionTests
    {
        private ArsenalProjectileTestRig _rig;
        private EnemyAttackExecution _execution;
        private Actor _target;

        [SetUp]
        public void SetUp()
        {
            _rig = new ArsenalProjectileTestRig();
            _target = _rig.BuildEnemy(Vector3.zero, 100);
            _execution = new EnemyAttackExecution();
        }

        [TearDown]
        public void TearDown() { Time.timeScale = 1; _execution.Cancel(); _rig.TearDown(); }

        private EnemyAttackArea Circle => new EnemyAttackArea(EnemyAttackShape.Circle, Vector3.zero, Vector3.forward, 3);

        [UnityTest]
        public IEnumerator OverlappingAreasDealDamageOnlyOnce()
        {
            yield return _execution.Execute(new[] { Circle, Circle }, _target, .25f, 10, Color.red, () => true);
            Assert.That(_target.health, Is.EqualTo(90));
            Assert.That(_rig.DamageEvents.Count, Is.EqualTo(1));
            Assert.That(_execution.IsWindingUp, Is.False);
            Assert.That(_execution.Completed, Is.True);
        }

        [UnityTest]
        public IEnumerator LeavingLockedWarningAvoidsDamage()
        {
            var routine = _execution.Execute(new[] { Circle }, _target, .25f, 10, Color.red, () => true);
            Assert.That(routine.MoveNext(), Is.True);
            _target.transform.position = Vector3.right*6;
            yield return routine;
            Assert.That(_target.health, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator InterruptCancelsDamageAndWarning()
        {
            var routine = _execution.Execute(new[] { Circle }, _target, .25f, 10, Color.red, () => true);
            Assert.That(routine.MoveNext(), Is.True);
            Assert.That(_execution.IsWindingUp, Is.True);
            _execution.Cancel();
            yield return routine;
            Assert.That(_target.health, Is.EqualTo(100));
            Assert.That(_execution.IsWindingUp, Is.False);
            Assert.That(_execution.Completed, Is.False);
        }

        [UnityTest]
        public IEnumerator LosingPermissionDuringWindupPreventsDamage()
        {
            bool canAttack = true;
            var routine = _execution.Execute(new[] { Circle }, _target, .25f, 10, Color.red, () => canAttack);
            Assert.That(routine.MoveNext(), Is.True);
            canAttack = false;
            yield return routine;
            Assert.That(_target.health, Is.EqualTo(100));
            Assert.That(_execution.Completed, Is.False);
        }

        [UnityTest]
        public IEnumerator PauseFreezesWindup()
        {
            var routine = _execution.Execute(new[] { Circle }, _target, .25f, 10, Color.red, () => true);
            Assert.That(routine.MoveNext(), Is.True);
            Time.timeScale = 0;
            yield return null;
            for (int i = 0; i < 5; i++) { Assert.That(routine.MoveNext(), Is.True); yield return null; }
            Assert.That(_target.health, Is.EqualTo(100));
            Time.timeScale = 1;
            yield return routine;
            Assert.That(_target.health, Is.EqualTo(90));
        }
    }
}
