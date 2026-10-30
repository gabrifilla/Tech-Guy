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
                yield return new WaitForSeconds(.65f);
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
        private static IEnumerator RunPublishedHooks(EnemyCombatActions actions, PlayerActor player)
        { for(int i=0;i<5;i++) yield return actions.Perform(EnemyAttackKind.HookLine,player,0,()=>true); }
    }
}
