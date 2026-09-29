using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class EnemyAttackAreaTests
    {
        [Test]
        public void ConeLeavesFlanksAndBackSafe()
        {
            var area = new EnemyAttackArea(EnemyAttackShape.Cone, Vector3.zero, Vector3.forward, 4, angle: 110);
            Assert.That(area.Contains(Vector3.forward*3), Is.True);
            Assert.That(area.Contains(Vector3.back), Is.False);
            Assert.That(area.Contains(Vector3.right*2), Is.False);
            Assert.That(area.Contains(Vector3.forward*4.1f), Is.False);
        }

        [Test]
        public void RotatedLaneUsesLengthAndHalfWidth()
        {
            var origin = new Vector3(10, 2, -7);
            var area = new EnemyAttackArea(EnemyAttackShape.Lane, origin, Vector3.right, 8, 2);
            Assert.That(area.Contains(origin+Vector3.right*7+Vector3.forward*.9f), Is.True);
            Assert.That(area.Contains(origin+Vector3.right*7+Vector3.forward*1.1f), Is.False);
            Assert.That(area.Contains(origin-Vector3.right), Is.False);
            Assert.That(area.Contains(origin+Vector3.right*8.1f), Is.False);
            Assert.That(area.Contains(origin+Vector3.up*3), Is.False);
        }

        [Test]
        public void RingHasSafeCenterAndSafeExterior()
        {
            var area = new EnemyAttackArea(EnemyAttackShape.Ring, Vector3.zero, Vector3.forward, 11, innerRadius: 4);
            Assert.That(area.Contains(Vector3.zero), Is.False);
            Assert.That(area.Contains(Vector3.right*3.9f), Is.False);
            Assert.That(area.Contains(Vector3.right*4.1f), Is.True);
            Assert.That(area.Contains(Vector3.right*10.9f), Is.True);
            Assert.That(area.Contains(Vector3.right*11.1f), Is.False);
            foreach (var point in area.Outline(true)) Assert.That(point.magnitude, Is.EqualTo(4).Within(.001f));
            foreach (var point in area.Outline()) Assert.That(point.magnitude, Is.EqualTo(11).Within(.001f));
        }

        [Test]
        public void AffixesChooseMovesAppropriateToDistance()
        {
            var traits = EnemyAttackTraits.Haste | EnemyAttackTraits.Frost | EnemyAttackTraits.Guard;
            Assert.That(EnemyAttackPatterns.Select(traits, 0, 6), Is.EqualTo(EnemyAttackKind.FrostBolt));
            Assert.That(EnemyAttackPatterns.Select(traits, 1, 6), Is.EqualTo(EnemyAttackKind.Charge));
            Assert.That(EnemyAttackPatterns.Select(traits, 1, 1.5f), Is.EqualTo(EnemyAttackKind.HeavySlam));
            Assert.That(EnemyAttackPatterns.Select(traits, 0, 1.5f), Is.EqualTo(EnemyAttackKind.DoublePunch));
            Assert.That(EnemyAttackPatterns.Select(EnemyAttackTraits.None, 4, 1.5f), Is.EqualTo(EnemyAttackKind.Punch));
        }

        [Test]
        public void SweptChargeDoesNotSkipTargetBetweenFrames()
        {
            Assert.That(EnemyCombatActions.SegmentContains(Vector3.forward*3,Vector3.zero,Vector3.forward*6,.6f), Is.True);
            Assert.That(EnemyCombatActions.SegmentContains(Vector3.forward*3+Vector3.right,Vector3.zero,Vector3.forward*6,.6f), Is.False);
        }

        [Test]
        public void ShockwaveDamagesOnlyItsTravellingFront()
        {
            Assert.That(EnemyCombatActions.WaveContains(Vector3.forward*5,Vector3.zero,4,6,.3f), Is.True);
            Assert.That(EnemyCombatActions.WaveContains(Vector3.forward*2,Vector3.zero,4,6,.3f), Is.False);
            Assert.That(EnemyCombatActions.WaveContains(Vector3.forward*8,Vector3.zero,4,6,.3f), Is.False);
        }
    }
}
