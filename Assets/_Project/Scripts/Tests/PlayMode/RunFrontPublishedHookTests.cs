using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class Gpr015ProductionAuditTests
    {
        [UnityTest, Category("RUN00Published")]
        public IEnumerator RUN00PublishedHookerReleasesControlAndSourceBreakCancelsPull()
        {
            yield return LoadProduction();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var session = director.ManualSession;
            Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(1).Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, 1)); yield return null;
            PreventCoinPersistence(director);
            foreach (var enemy in director.GetComponentsInChildren<Actor>()) enemy.gameObject.SetActive(false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Hooker.prefab");
            Assert.IsNotNull(prefab);
            var source = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 5, Quaternion.identity);
            try
            {
                source.GetComponent<EnemyAI>().enabled = false;
                foreach (var coin in source.GetComponents<CoinDrop>()) Object.DestroyImmediate(coin);
                Assert.IsTrue(source.GetComponent<NavMeshAgent>().Warp(player.transform.position + Vector3.forward * 5));
                yield return null;
                var actions = source.GetComponent<EnemyCombatActions>();
                var control = player.GetComponent<CharControlScript>();
                string directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Docs/validation/artifacts/run-exp-01/stage0/hooker-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")));
                Directory.CreateDirectory(directory);
                var rows = new List<string> { "seconds,pulled,controllerEnabled,windup,impacts,x,z" };
                float start = Time.time;
                actions.StartCoroutine(RunPublishedHooks(actions, player));
                bool sawPull = false, sawRelease = false;
                while (Time.time - start < 4.5f)
                {
                    rows.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F4},{1},{2},{3},{4},{5:F3},{6:F3}", Time.time-start, player.IsBeingPulled, control.enabled, actions.IsWindingUp, actions.ResolvedImpacts, player.transform.position.x, player.transform.position.z));
                    if (!sawPull && player.IsBeingPulled)
                    { sawPull = true; ScreenCapture.CaptureScreenshot(Path.Combine(directory, "hook-pull.png")); }
                    if (sawPull && !sawRelease && !player.IsBeingPulled)
                    { sawRelease = true; Assert.IsTrue(control.enabled); ScreenCapture.CaptureScreenshot(Path.Combine(directory, "hook-control-restored.png")); }
                    yield return null;
                }
                Assert.IsTrue(sawPull); Assert.IsTrue(sawRelease);
                File.WriteAllLines(Path.Combine(directory,"timeline.csv"),rows);
                actions.StopAllCoroutines(); actions.Cancel();
                float deadline = Time.time + 3;
                while(player.IsBeingPulled && Time.time < deadline) yield return null;
                // Etapa 2: hooks connect sooner and more often, so wait out the (intended) 2.5 s escape window.
                deadline = Time.time + PlayerActor.ExternalPullRecovery + .5f;
                while(!player.AcceptsExternalPull && Time.time < deadline) yield return null;
                Assert.IsTrue(player.AcceptsExternalPull, "The escape window must expire.");
                // Legacy Hooker has no authored controller; explicitly attach the normal reaction
                // authority for this isolated interruption scenario, after the real-prefab capture.
                var reaction = source.GetComponent<CombatReactionController>() ?? source.AddComponent<CombatReactionController>();
                reaction.ConfigureStance(EnemyRank.Normal, 1, 1, 0, 0, 0, 0, 0, 0);
                reaction.ConfigureReaction(new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob));
                player.BeginExternalPull(source.transform, 1.5f); Assert.IsTrue(player.IsBeingPulled);
                reaction.ApplyReaction(new HitReactionRequest(player,source.transform.position,Vector3.forward,HitReactionType.None,HitStrength.Light,10,StanceBreakEffect.Stun,0,.3f));
                yield return null; yield return null;
                Assert.IsTrue(reaction.IsControlLocked);
                Assert.IsFalse(player.IsBeingPulled); Assert.IsTrue(control.enabled);
                File.WriteAllText(Path.Combine(directory,"context.txt"),"FirstSector; normal gameplay camera; production Hooker prefab and HookLine executor; controlled attack loop, AI and other actors suspended in fixture; no human acceptance. Source posture break cancels pull.");
            }
            finally { Object.DestroyImmediate(source); }
        }
        /// <summary>Etapa 2: the real Hooker AI throws the hook, then fights with the chain while it recharges.</summary>
        [UnityTest, Category("RUN00Published")]
        public IEnumerator RUN00PublishedHookerAlternatesHookAndChainStrikesWithARandomRecharge()
        {
            yield return LoadProduction();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var session = director.ManualSession;
            Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(1).Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, 1)); yield return null;
            PreventCoinPersistence(director);
            foreach (var enemy in director.GetComponentsInChildren<Actor>()) enemy.gameObject.SetActive(false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Hooker.prefab");
            var source = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 9, Quaternion.identity);
            try
            {
                foreach (var coin in source.GetComponents<CoinDrop>()) Object.DestroyImmediate(coin);
                Assert.IsTrue(source.GetComponent<NavMeshAgent>().Warp(player.transform.position + Vector3.forward * 9));
                // As in a real room: the encounter makes every enemy in it see the player.
                source.GetComponent<EnemyAI>().ConfigureEncounterBounds(session.Route.Node(1).Center, new Vector2(30f, 30f));
                var actions = source.GetComponent<EnemyCombatActions>();
                var hookStarts = new List<float>();
                var kinds = new HashSet<EnemyAttackKind>();
                bool wasWinding = false, sawPull = false;
                float start = Time.time;
                while (Time.time - start < 14f && !player.IsDead)
                {
                    sawPull |= player.IsBeingPulled;
                    bool winding = actions.IsWindingUp;
                    if (winding && !wasWinding)
                    {
                        kinds.Add(actions.CurrentKind);
                        if (actions.CurrentKind == EnemyAttackKind.HookLine) hookStarts.Add(Time.time);
                    }
                    wasWinding = winding;
                    yield return null;
                }
                var ai = source.GetComponent<EnemyAI>();
                var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public;
                string state = "ai=" + ai.enabled + " player=" + (ai.player ? ai.player.name : "null") +
                    " sight=" + typeof(EnemyAI).GetField("playerInSightRange", flags)?.GetValue(ai) +
                    " inRange=" + typeof(EnemyAI).GetField("playerInAttackRange", flags)?.GetValue(ai) +
                    " sightRange=" + typeof(EnemyAI).GetField("sightRange", flags)?.GetValue(ai) +
                    " archetype=" + source.GetComponent<EnemyVariant>().ArchetypeId +
                    " onMesh=" + source.GetComponent<NavMeshAgent>().isOnNavMesh +
                    " dist=" + Vector3.Distance(source.transform.position, player.transform.position);
                Assert.IsTrue(kinds.Contains(EnemyAttackKind.HookLine), "The AI throws the hook. Seen: " + string.Join(",", kinds) + " | " + state);
                Assert.IsTrue(sawPull, "A standing player on the throw line is pulled.");
                Assert.IsTrue(kinds.Contains(EnemyAttackKind.ChainSweep) || kinds.Contains(EnemyAttackKind.ChainThrust),
                    "Chain strikes fill the recharge. Seen: " + string.Join(",", kinds));
                for (int i = 1; i < hookStarts.Count; i++)
                    Assert.GreaterOrEqual(hookStarts[i] - hookStarts[i - 1], HookerCadence.HookCooldown,
                        "The hook never comes back before its recharge.");
            }
            finally { Object.DestroyImmediate(source); }
        }

        /// <summary>Etapa 3: the Grunt's short lunge closes a 3.5 m gap along a locked lane and lands its hit.</summary>
        [UnityTest, Category("RUN00Published")]
        public IEnumerator ETAPA3PublishedGruntLungeClosesTheGapAndHits()
        {
            yield return LoadProduction();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var session = director.ManualSession;
            Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(1).Center));
            Assert.IsTrue(director.TryEnterNode(session.Id, 1)); yield return null;
            PreventCoinPersistence(director);
            foreach (var enemy in director.GetComponentsInChildren<Actor>()) enemy.gameObject.SetActive(false);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Grunt.prefab");
            var grunt = Object.Instantiate(prefab, player.transform.position + Vector3.forward * 3.5f, Quaternion.identity);
            try
            {
                grunt.GetComponent<EnemyAI>().enabled = false;
                foreach (var coin in grunt.GetComponents<CoinDrop>()) Object.DestroyImmediate(coin);
                Assert.IsTrue(grunt.GetComponent<NavMeshAgent>().Warp(player.transform.position + Vector3.forward * 3.5f));
                yield return null;
                var actions = grunt.GetComponent<EnemyCombatActions>();
                Vector3 start = grunt.transform.position;
                float hp = player.health;
                yield return actions.Perform(EnemyAttackKind.Lunge, player, 20f, () => true);
                Vector3 moved = grunt.transform.position - start; moved.y = 0;
                Assert.Greater(moved.magnitude, 2f, "The lunge travels toward the player.");
                Assert.Less(player.health, hp, "A player standing in the lunge lane is hit.");
            }
            finally { Object.DestroyImmediate(grunt); }
        }

        private static IEnumerator RunPublishedHooks(EnemyCombatActions actions, PlayerActor player)
        { for(int i=0;i<5;i++) yield return actions.Perform(EnemyAttackKind.HookLine,player,0,()=>true); }
    }
}
