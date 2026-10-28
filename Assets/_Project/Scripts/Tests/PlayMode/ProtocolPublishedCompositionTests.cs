using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class ProtocolPublishedCompositionTests : RunSummaryIsolatedPlayModeFixture
    {
        private int _selection;
        [SetUp] public void Setup() { _selection = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0); }
        [TearDown] public void Teardown() { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, _selection); Time.timeScale = 1; }
        [UnityTest] public IEnumerator FiveCompositionsAreAcquiredThroughPublishedTicketsAndDriveRealDamage()
        {
            string[][] sequences = {
                new[] { "ignite", "conductor", "frost", "resonance" },
                new[] { "ignite", "reactor", "detonation", "resonance" },
                new[] { "frost", "detonation", "ignite", "reactor" },
                new[] { "frost", "conductor", "overflow", "resonance" },
                new[] { "ignite", "conductor", "frost", "resonance", "ignite" }
            };
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int build = 0; build < sequences.Length; build++)
            {
                yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null;
                yield return SceneManager.LoadSceneAsync("FirstSector"); yield return null; yield return null; yield return null;
                var director = Object.FindFirstObjectByType<ProgressionDirector>();
                var player = Object.FindFirstObjectByType<PlayerActor>(); var run = player.GetComponent<RunBoons>();
                var runtime = player.GetComponent<ProtocolRuntime>(); runtime.SetChillRollForTests(() => 0);
                player.AddDamageImmunity(director); // Deterministic enemies, not a gameplay acceptance run.
                var session = director.ManualSession; Assert.That(run.Protocols.Revision, Is.Zero);
                int[] nodes = build == 4 ? new[] { 1, 2, 4, 5, 7, 8, 9, 10 } : new[] { 1, 2, 4, 5, 6, 9, 10 };
                int opportunity = 0;
                foreach (int node in nodes)
                {
                    Assert.That(director.TryCommitDestination(session.Id, node), Is.True);
                    Assert.That(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(node).Center), Is.True);
                    Assert.That(director.TryEnterNode(session.Id, node), Is.True);
                    yield return null;
                    var enemies = director.GetComponentsInChildren<Actor>();
                    for (int i = 0; i < enemies.Length; i++)
                    {
                        var ai = enemies[i].GetComponent<EnemyAI>(); if (ai) ai.enabled = false;
                        enemies[i].SetMaxHealth(1000);
                        enemies[i].GetComponent<NavMeshAgent>().Warp(player.transform.position + Vector3.forward * 2 + Vector3.right * (.65f * i));
                    }
                    Physics.SyncTransforms();
                    if (opportunity > 0)
                    {
                        player.DealResolvedAttackDamage(enemies[0], 10);
                        if (run.Protocols.Rank(ProtocolKind.Ignite) > 0) Assert.That(ProtocolElementQuery.HasBurn(enemies[0]), Is.True);
                        if (run.Protocols.Rank(ProtocolKind.Frost) > 0) Assert.That(ProtocolElementQuery.HasChill(enemies[0]), Is.True);
                        if (run.Protocols.Rank(ProtocolKind.Reactor) > 0) Assert.That(enemies[0].GetComponent<ProtocolElementStatus>().BurnDps, Is.GreaterThan(6));
                        if (run.Protocols.Rank(ProtocolKind.Conductor) > 0 && enemies.Length > 1 || run.Protocols.Rank(ProtocolKind.Overflow) > 0)
                            Assert.That(runtime.Budget.FrameImpacts, Is.GreaterThan(0), "build=" + build + " node=" + node + " delta=" + Time.deltaTime + " canAdvance=" + runtime.Budget.CanAdvance + " queries=" + runtime.Budget.FrameQueries + " hitstop=" + player.IsCombatHitStopped + " hp=" + enemies[0].health);
                    }
                    foreach (var enemy in enemies) if (enemy && !enemy.IsDead) player.DealResolvedAttackDamage(enemy, 1000000);
                    float deadline = Time.realtimeSinceStartup + 5;
                    while (session.Phase == RunPhase.Encounter || session.Phase == RunPhase.FinalResolution)
                    { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
                    while (session.Phase == RunPhase.RewardPending)
                    {
                        deadline = Time.realtimeSinceStartup + 5;
                        while (!run.IsChoosing) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
                        yield return null; yield return null;
                        int index = 0;
                        if (run.ManualTicket.AnnouncedKind == RunRewardKind.Protocol)
                        {
                            string wanted = sequences[build][opportunity++];
                            index = run.Choices.ToList().FindIndex(offer => offer.Id == wanted);
                            Assert.That(index, Is.GreaterThanOrEqualTo(0), "Unavailable published choice " + wanted);
                            seen.Add(wanted);
                        }
                        Assert.That(run.Choose(index), Is.True);
                    }
                    if (node == 5) Assert.That(session.MutationAcquisitions, Is.EqualTo(1));
                    yield return null;
                }
                Assert.That(session.Phase, Is.EqualTo(RunPhase.ExtractionAvailable));
                Assert.That(session.ProtocolAcquisitions, Is.EqualTo(build == 4 ? 5 : 4));
                Assert.That(runtime.Budget.LiveRoots, Is.Zero); Assert.That(runtime.BurnApplications, Is.Zero);
                if (build == 4) Assert.That(run.Protocols.Rank(ProtocolKind.Ignite), Is.EqualTo(2));
            }
            Assert.That(seen.Count, Is.EqualTo(7));
            yield return SceneManager.LoadSceneAsync("NexusBlockout"); yield return null;
        }
    }
}
