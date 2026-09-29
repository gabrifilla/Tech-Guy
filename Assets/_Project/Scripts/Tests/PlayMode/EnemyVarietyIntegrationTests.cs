using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class EnemyVarietyIntegrationTests
    {
        private ArsenalProjectileTestRig _rig;
        private readonly List<GameObject> _instances = new List<GameObject>();
        private SectorBoss _boss;

        [SetUp]
        public void SetUp() { _rig = new ArsenalProjectileTestRig(); }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1;
            if (_boss) _boss.enabled = false;
            foreach (var instance in _instances) if (instance) Object.DestroyImmediate(instance);
            _instances.Clear();
            _rig.TearDown();
        }

        [UnityTest]
        public IEnumerator BossUsesPhysicalMovesAndUnlocksSecondPhaseFollowups()
        {
            var player = _rig.BuildOwner();
            player.SetMaxHealth(10000);
            player.RestoreHealthToMax();
            var actor = _rig.BuildEnemy(Vector3.forward*2, 1800);
            _boss = actor.gameObject.AddComponent<SectorBoss>();
            _boss.Configure(player);
            Time.timeScale = 4;
            var moves = new HashSet<EnemyAttackKind>();
            float deadline = Time.time + 55;
            while (_boss.ResolvedAttacks < 5 && Time.time < deadline)
            {
                if (_boss.IsTelegraphing) moves.Add(_boss.CurrentAttack);
                yield return null;
            }
            Assert.That(_boss.ResolvedAttacks, Is.GreaterThanOrEqualTo(5));
            Assert.That(moves, Does.Contain(EnemyAttackKind.HeavySlam));
            Assert.That(moves, Does.Contain(EnemyAttackKind.DoublePunch));
            Assert.That(moves, Does.Contain(EnemyAttackKind.Shockwave));
            actor.TakeDamage(1000);
            Assert.That(_boss.IsEnraged, Is.True);
            deadline = Time.time + 60;
            while (Time.time < deadline && _boss.SecondPhaseFollowups < 2) yield return null;
            Assert.That(_boss.SecondPhaseFollowups, Is.GreaterThanOrEqualTo(2));
            _boss.enabled = false;
            int attacks = _boss.ResolvedAttacks;
            yield return new WaitForSeconds(2);
            Assert.That(_boss.IsTelegraphing, Is.False);
            Assert.That(_boss.ResolvedAttacks, Is.EqualTo(attacks));
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator ExistingVariantPrefabsReceiveTheirAffixMoves()
        {
            var staging = new GameObject("Inactive variant staging");
            staging.SetActive(false);
            _instances.Add(staging);
            var expected = new Dictionary<string, EnemyAttackTraits>
            {
                { "Normal", EnemyAttackTraits.None },
                { "Magic_Haste", EnemyAttackTraits.Haste },
                { "Rare_Frost", EnemyAttackTraits.Frost },
                { "Rare_Haste_Guard", EnemyAttackTraits.Haste | EnemyAttackTraits.Guard },
                { "Rare_Frost_Haste_Guard", EnemyAttackTraits.Frost | EnemyAttackTraits.Haste | EnemyAttackTraits.Guard }
            };
            foreach (var entry in expected)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Prefabs/EnemyVariants/" + entry.Key + ".prefab");
                Assert.That(prefab, Is.Not.Null);
                staging.SetActive(false);
                var instance = Object.Instantiate(prefab, staging.transform);
                _instances.Add(instance);
                var ai = instance.GetComponent<EnemyAI>();
                ai.enabled = false;
                instance.GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = false;
                staging.SetActive(true);
                yield return null;
                Assert.That(ai.AttackTraits, Is.EqualTo(entry.Value), entry.Key);
            }
        }
#endif

        [UnityTest]
        public IEnumerator BossStanceBreakCancelsPendingImpact()
        {
            var player = _rig.BuildOwner();
            player.SetMaxHealth(1000);
            player.RestoreHealthToMax();
            var actor = _rig.BuildEnemy(Vector3.forward, 1800);
            _boss = actor.gameObject.AddComponent<SectorBoss>();
            _boss.Configure(player);
            float deadline = Time.time + 3;
            while (!_boss.IsTelegraphing && Time.time < deadline) yield return null;
            Assert.That(_boss.IsTelegraphing, Is.True);
            actor.GetComponent<CombatReactionController>().ApplyReaction(new HitReactionRequest(
                player, actor.transform.position, Vector3.forward, HitReactionType.None,
                HitStrength.Heavy, 10000, StanceBreakEffect.None, pushDistance: 0));
            Assert.That(_boss.IsTelegraphing, Is.False);
            yield return new WaitForSeconds(.5f);
            Assert.That(_boss.ResolvedAttacks, Is.EqualTo(0));
            Assert.That(player.health, Is.EqualTo(1000));
        }
    }
}
