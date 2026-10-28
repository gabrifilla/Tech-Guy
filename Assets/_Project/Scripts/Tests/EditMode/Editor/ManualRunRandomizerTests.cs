using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class ManualRunRandomizerTests
    {
        private ManualRunDefinition _definition;
        private ManualRouteSnapshot _source;
        private GameObject[] _pool;

        [SetUp]
        public void SetUp()
        {
            _definition = FluidityRouteTestFactory.Create();
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition,
                Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out _source, out string error), error);
            _pool = new[] { "Grunt", "Heavy", "Shooter", "Healer", "ShieldSupport", "Spawner" }
                .Select(name => AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/_Project/Prefabs/EnemyArchetypes/" + name + ".prefab")).ToArray();
            Assert.IsTrue(_pool.All(prefab => prefab), "Published enemy prefabs are required by this fixture.");
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_definition);

        [TestCase(0UL)]
        [TestCase(1701UL)]
        [TestCase(ulong.MaxValue)]
        public void SameSeedReproducesLayoutEnemiesAndRewardSequence(ulong seed)
        {
            Assert.AreEqual(Signature(ManualRunRandomizer.Generate(_source, seed, _pool)),
                Signature(ManualRunRandomizer.Generate(_source, seed, _pool)));
        }

        [Test]
        public void DifferentSeedsVaryLayoutEncounterAndCompletionRewards()
        {
            var runs = Enumerable.Range(0, 16).Select(seed => ManualRunRandomizer.Generate(_source, (ulong)seed, _pool)).ToArray();
            Assert.Greater(runs.Select(route => string.Join(";", route.Nodes.Select(node => node.Center + "/" + node.ArenaSize))).Distinct().Count(), 1);
            Assert.Greater(runs.Select(route => string.Join(";", route.Nodes.SelectMany(node => node.Spawns).Select(spawn => spawn.Prefab.name + "/" + spawn.Offset))).Distinct().Count(), 1);
            Assert.Greater(runs.Select(route => string.Join(";", route.Nodes.Select(node => string.Join(",", Enumerable.Range(0, node.TotalAcquisitions).Select(node.RewardAt))))).Distinct().Count(), 1);
        }

        [Test]
        public void GenerationDoesNotAlterAuthoringSnapshotOrGlobalRandom()
        {
            string before = Signature(_source);
            var state = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(325);
                float expected = UnityEngine.Random.value;
                UnityEngine.Random.InitState(325);
                var generated = ManualRunRandomizer.Generate(_source, 55, _pool);
                Assert.AreEqual(expected, UnityEngine.Random.value);
                Assert.AreEqual(before, Signature(_source));
                string generatedBefore = Signature(generated);
                _source.Node(1).ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Protocol, 2) });
                Assert.AreEqual(generatedBefore, Signature(generated), "The generated snapshot must own its reward lists.");
            }
            finally { UnityEngine.Random.state = state; }
        }

        [Test]
        public void ManySeedsKeepRoomsSeparatedAndSpawnPointsInsideTheirArena()
        {
            for (ulong seed = 0; seed < 64; seed++)
            {
                var nodes = ManualRunRandomizer.Generate(_source, seed, _pool).Nodes.ToArray();
                for (int i = 0; i < nodes.Length; i++)
                {
                    var node = nodes[i];
                    Assert.That(node.ArenaSize.x, Is.InRange(22f, 30f));
                    Assert.That(node.ArenaSize.y, Is.InRange(18f, 24f));
                    foreach (var spawn in node.Spawns)
                    {
                        Assert.LessOrEqual(Mathf.Abs(spawn.Offset.x), node.ArenaSize.x * .5f - 3f);
                        Assert.LessOrEqual(Mathf.Abs(spawn.Offset.z), node.ArenaSize.y * .5f - 3f);
                    }
                    for (int j = i + 1; j < nodes.Length; j++)
                    {
                        var other = nodes[j];
                        Assert.IsTrue(Mathf.Abs(node.Center.x - other.Center.x) >= (node.ArenaSize.x + other.ArenaSize.x) * .5f ||
                            Mathf.Abs(node.Center.z - other.Center.z) >= (node.ArenaSize.y + other.ArenaSize.y) * .5f,
                            "Overlapping rooms at seed " + seed + ": " + node.Id + "/" + other.Id);
                    }
                    if (node.Kind == ManualNodeKind.Boss) continue;
                    for (int a = 0; a < node.Spawns.Count; a++)
                        for (int b = a + 1; b < node.Spawns.Count; b++)
                            Assert.Greater(Vector3.Distance(node.Spawns[a].Offset, node.Spawns[b].Offset), 2.5f);
                }
            }
        }

        [Test]
        public void PathsBranchExclusionsAndMutationBeforeBossSurviveGeneration()
        {
            for (ulong seed = 0; seed < 32; seed++)
            {
                var route = ManualRunRandomizer.Generate(_source, seed, _pool);
                CollectionAssert.AreEqual(_source.Connections.Select(edge => edge.Id + ":" + edge.From + ":" + edge.To),
                    route.Connections.Select(edge => edge.Id + ":" + edge.From + ":" + edge.To));
                CollectionAssert.AreEqual(_source.Paths.Select(path => string.Join(",", path)), route.Paths.Select(path => string.Join(",", path)));
                foreach (var path in route.Paths)
                {
                    Assert.That(path.Count(id => route.Node(id).IsEncounter), Is.InRange(6, 8));
                    var rewards = path.Where(id => id != route.BossNode).SelectMany(id =>
                        Enumerable.Range(0, route.Node(id).TotalAcquisitions).Select(route.Node(id).RewardAt)).ToArray();
                    Assert.AreEqual(1, rewards.Count(kind => kind == RunRewardKind.Mutation));
                    CollectionAssert.Contains(rewards, RunRewardKind.Technique);
                    CollectionAssert.Contains(rewards, RunRewardKind.Protocol);
                }
                foreach (var node in route.Nodes)
                {
                    Assert.AreEqual(_source.Node(node.Id).Kind, node.Kind);
                    Assert.AreEqual(_source.Node(node.Id).RepairFraction, node.RepairFraction);
                    Assert.AreEqual(node.IsEncounter && node.Kind != ManualNodeKind.Boss, node.TotalAcquisitions > 0);
                }
            }
        }

        [Test]
        public void EncountersKeepRealBossAndEliteAndCapSupportsWithActiveAttackers()
        {
            for (ulong seed = 0; seed < 32; seed++)
                foreach (var node in ManualRunRandomizer.Generate(_source, seed, _pool).Nodes.Where(node => node.IsEncounter))
                {
                    if (node.Kind == ManualNodeKind.Boss)
                    {
                        var authored = _source.Node(node.Id);
                        CollectionAssert.AreEqual(authored.Spawns.Select(spawn => spawn.Prefab), node.Spawns.Select(spawn => spawn.Prefab));
                        CollectionAssert.AreEqual(authored.Spawns.Select(spawn => spawn.Rank), node.Spawns.Select(spawn => spawn.Rank));
                        Assert.AreEqual(0, node.TotalAcquisitions);
                        continue;
                    }
                    Assert.That(node.Spawns.Count, Is.InRange(4, 10));
                    int supports = node.Spawns.Count(spawn => IsSupport(spawn.Prefab));
                    Assert.LessOrEqual(supports, 2);
                    Assert.GreaterOrEqual(node.Spawns.Count - supports, 2);
                    Assert.AreEqual(node.Kind == ManualNodeKind.Elite ? 1 : 0, node.Spawns.Count(spawn => spawn.Rank == EnemyRank.Elite));
                    Assert.IsFalse(node.Spawns.Any(spawn => spawn.Rank == EnemyRank.Boss));
                    if (node.Kind == ManualNodeKind.Elite)
                    {
                        Assert.AreSame(_source.Node(node.Id).Spawns.First(spawn => spawn.Rank == EnemyRank.Elite).Prefab,
                            node.Spawns.First(spawn => spawn.Rank == EnemyRank.Elite).Prefab);
                        Assert.That(node.TotalAcquisitions, Is.InRange(2, 3));
                    }
                }
        }

        [Test]
        public void MissingOrInvalidCatalogUsesExplicitAuthoredOrdinaryEnemies()
        {
            var invalid = new GameObject("Not an enemy");
            try
            {
                foreach (var candidates in new IReadOnlyList<GameObject>[] { null, new GameObject[0], new[] { invalid, null } })
                {
                    var generated = ManualRunRandomizer.Generate(_source, 1701, candidates);
                    Assert.IsTrue(generated.Nodes.Where(node => node.IsEncounter).All(node => node.Spawns.Count > 0));
                    Assert.IsFalse(generated.Nodes.SelectMany(node => node.Spawns).Any(spawn => spawn.Prefab == invalid));
                }
            }
            finally { Object.DestroyImmediate(invalid); }
            Assert.Throws<ArgumentNullException>(() => ManualRunRandomizer.Generate(null, 0, _pool));
        }

        private static bool IsSupport(GameObject prefab)
        {
            var variant = prefab.GetComponent<EnemyVariant>();
            return variant && (variant.ArchetypeId == ArchetypeId.Healer || variant.ArchetypeId == ArchetypeId.ShieldSupport);
        }

        private static string Signature(ManualRouteSnapshot route) => string.Join(";", route.Nodes.OrderBy(node => node.Id).Select(node =>
            node.Id + ":" + node.Center.ToString("F4") + ":" + node.ArenaSize.ToString("F4") + ":" + node.Kind + ":" +
            string.Join(",", node.Spawns.Select(spawn => spawn.Prefab.name + "/" + spawn.Offset.ToString("F4") + "/" + spawn.Rank)) + ":" +
            string.Join(",", Enumerable.Range(0, node.TotalAcquisitions).Select(node.RewardAt))));
    }
}
