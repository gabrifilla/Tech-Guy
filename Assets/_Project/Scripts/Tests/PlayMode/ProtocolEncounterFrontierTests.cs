using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class ProtocolEncounterFrontierTests : RunSummaryIsolatedPlayModeFixture
    {
        private int _selection;
        [SetUp] public void Setup() { _selection = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0); }
        [TearDown] public void Teardown() { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, _selection); Time.timeScale = 1; }
        [UnityTest] public IEnumerator LastOverflowDeathKeepsNativeBasicKillUncreditedAndOpensOneTicket() => Scenario("overflow", false);
        [UnityTest] public IEnumerator LastBurnTickDeathOpensRewardWithoutWaitingForStatusLifetime() => Scenario("tick", false);
        [UnityTest] public IEnumerator LastDetonationCascadeCompletesBeforeReward() => Scenario("cascade", false);
        [UnityTest] public IEnumerator ConcurrentPlayerDeathHasPriorityOverProtocolClear() => Scenario("overflow", true);

        private IEnumerator Scenario(string kind, bool playerDies)
        {
            yield return SceneManager.LoadSceneAsync("FirstSector"); yield return null; yield return null; yield return null;
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>(); var run = player.GetComponent<RunBoons>();
            var runtime = player.GetComponent<ProtocolRuntime>(); var session = director.ManualSession;
            player.AddDamageImmunity(director); player.Stats.baseDamage = 0; player.Stats.criticalChance = 0;
            // Explicit frontier fixture injection, not acquisition/reachability evidence.
            foreach (var protocol in new[] { ProtocolKind.Ignite, ProtocolKind.Detonation, ProtocolKind.Overflow })
                if (protocol != ProtocolKind.Overflow || kind == "overflow")
                    Assert.That(run.Protocols.TryAcquire(session.Id, protocol, run.Protocols.Revision, _ => true), Is.True);
            Assert.That(director.TryCommitDestination(session.Id, 1), Is.True);
            Assert.That(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(1).Center), Is.True);
            Assert.That(director.TryEnterNode(session.Id, 1), Is.True); yield return null;
            var enemies = director.GetComponentsInChildren<Actor>(); Assert.That(enemies.Length, Is.GreaterThanOrEqualTo(2));
            foreach (var enemy in enemies)
            {
                var ai = enemy.GetComponent<EnemyAI>(); if (ai) ai.enabled = false;
                var actions = enemy.GetComponent<EnemyCombatActions>(); if (actions) actions.enabled = false;
            }
            var source = enemies[0]; var neighbor = enemies[1];
            foreach (var enemy in enemies.Skip(kind == "tick" ? 1 : 2)) enemy.TakeDamage(1000000);
            source.transform.position = player.transform.position + Vector3.forward * 2;
            neighbor.transform.position = source.transform.position + Vector3.right;
            source.health = kind == "cascade" ? 10 : 12;
            if (kind != "tick") neighbor.health = 1;
            Physics.SyncTransforms();
            int basicHits = 0, basicKills = 0, protocolDeaths = 0;
            run.Hooks.OnBasicHit += (_, __) => basicHits++;
            run.Hooks.OnBasicKill += _ => basicKills++;
            run.Hooks.OnProtocolImpact += receipt =>
            {
                if (!receipt.IsDeath) return;
                protocolDeaths++;
                Assert.That(session.Phase, Is.EqualTo(RunPhase.Encounter), "Confirmed notifications precede the frontier.");
                if (playerDies && !player.IsDead) { player.RemoveDamageImmunity(director); player.TakeDamage(1000000); }
            };
            var execution = runtime.BeginExecution(); ProtocolExecution future;
            using (runtime.OpenExecutionScope(execution)) future = runtime.ReserveExecution(ProtocolProducer.Echo);
            var hitboxObject = new GameObject("Frontier basic hitbox"); var hitbox = hitboxObject.AddComponent<HitboxDamage>();
            hitbox.owner = player; hitbox.damage = 10;
            using (player.BeginImpactFeedback(0)) Assert.That(hitbox.TryDamageActor(source), Is.True);
            Object.Destroy(hitboxObject); execution.Dispose();
            Assert.That(basicHits, Is.EqualTo(playerDies ? 0 : 1), "Player death ends the run and suppresses later native notifications.");
            Assert.That(basicKills, Is.EqualTo(kind == "cascade" ? 1 : 0), "Protocol lethality must not pay native basic-kill hooks.");
            float deadline = Time.realtimeSinceStartup + 8;
            if (playerDies)
            {
                while (SceneManager.GetActiveScene().name != "NexusBlockout") { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
                Assert.That(session.Outcome, Is.EqualTo(RunOutcome.Dead)); Assert.That(session.Phase, Is.EqualTo(RunPhase.Ended));
            }
            else
            {
                while (session.Phase == RunPhase.Encounter) { Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline)); yield return null; }
                Assert.That(session.Phase, Is.EqualTo(RunPhase.RewardPending));
                Assert.That(session.RewardOrdinal, Is.Zero); Assert.That(session.ProtocolAcquisitions, Is.Zero);
                Assert.That(protocolDeaths, Is.GreaterThanOrEqualTo(1));
                Assert.That(runtime.BurnApplications, Is.Zero); Assert.That(runtime.Budget.LiveRoots, Is.Zero);
                yield return null; Assert.That(session.RewardOrdinal, Is.Zero);
            }
            Assert.That(future.IsLifecycleCurrent, Is.False); future.Dispose();
        }
    }
}
