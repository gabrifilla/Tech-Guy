using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        [UnityTest] public IEnumerator Distribution09BTripleNativeInRotatedL90()
        {
            using(new PublishedInput06())
            {
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09b/triple/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0); ulong seed=0; while(FirstSectorRoomLayout.SelectFamily(seed,1)!=FirstSectorRoomFamily.LChamber||FirstSectorRoomDistribution.Orientation(seed,1,FirstSectorRoomFamily.LChamber,new Vector2(22,18))!=1)seed++; SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());
            yield return SceneManager.LoadSceneAsync("FirstSector");for(int i=0;i<5;i++)yield return null;
            var director=Object.FindAnyObjectByType<ProgressionDirector>();var player=Object.FindAnyObjectByType<PlayerActor>();var session=director.ManualSession;var run=player.GetComponent<RunBoons>();var runtime=player.GetComponent<ProtocolRuntime>();
            player.AddDamageImmunity(director);
            foreach(string id in new[]{TitanicFistsDefinition.Id,"conductor","detonation"})
                Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(run,new object[]{new RunBoons.Offer(id,"fixture","Assisted visual comparison; not natural acquisition"),false}));
            var node=session.Route.Node(1);var layout=node.SpatialLayout;Assert.AreEqual(FirstSectorRoomFamily.LChamber,layout.Family);Assert.AreEqual(1,layout.QuarterTurns);Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center+layout.ToOriented(new Vector3(-3,0,-3))));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
            foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
            var enemies=director.GetComponentsInChildren<Actor>().OrderBy(a=>EntityId.ToULong(a.GetEntityId())).ToArray();Assert.GreaterOrEqual(enemies.Length,4);
            Vector3 origin=player.transform.position;
            for(int i=0;i<enemies.Length;i++)
            {
                if(enemies[i].TryGetComponent<EnemyAI>(out var ai))ai.enabled=false;
                if(enemies[i].TryGetComponent<EnemyCombatActions>(out var actions))actions.Cancel();
                Assert.IsTrue(enemies[i].GetComponent<NavMeshAgent>().Warp(node.Center+layout.ToOriented(new Vector3(-11+i*1.3f,0,-10))));
                enemies[i].SetMaxHealth(1000);enemies[i].RestoreHealthToMax();
            }
            var controls=player.GetComponent<CharControlScript>();
            Assert.IsTrue(enemies[0].GetComponent<NavMeshAgent>().Warp(origin+layout.ToOriented(Vector3.forward*1.2f)));yield return new WaitForSeconds(.25f);
            yield return NativeOrientedBasic09B(controls,player,layout);yield return NativeOrientedBasic09B(controls,player,layout);
            Assert.IsTrue(enemies[0].GetComponent<NavMeshAgent>().Warp(origin+layout.ToOriented(Vector3.forward*5.8f)));
            Assert.IsTrue(enemies[1].GetComponent<NavMeshAgent>().Warp(origin+layout.ToOriented(new Vector3(2,0,5.8f))));
            Assert.IsTrue(enemies[2].GetComponent<NavMeshAgent>().Warp(origin+layout.ToOriented(new Vector3(4,0,5.8f))));
            Assert.IsTrue(enemies[3].GetComponent<NavMeshAgent>().Warp(origin+layout.ToOriented(new Vector3(-2,0,3))));
            enemies[1].SetMaxHealth(.5f);enemies[1].RestoreHealthToMax();Physics.SyncTransforms();
            using(var receipts=new RunStatistics(player,session.Id,1,Time.realtimeSinceStartupAsDouble,Time.timeAsDouble))
            {
                foreach(var enemy in enemies)receipts.Watch(enemy);
                yield return NativeOrientedBasic09B(controls,player,layout);
                float until=Time.realtimeSinceStartup+5;
                while(runtime.FirstTitanicPosition.HasValue&&Vector3.Dot(runtime.FirstTitanicPosition.Value-origin,layout.ToOriented(Vector3.forward))<2.3f){Assert.Less(Time.realtimeSinceStartup,until);yield return null;}
                Assert.Greater(runtime.PendingTitanicWaves,0);
                var attack=enemies[3].GetComponent<EnemyCombatActions>();attack.StartCoroutine(attack.Perform(EnemyAttackKind.HeavySlam,player,10,()=>true));
                yield return null;Assert.IsTrue(attack.IsWindingUp);
                yield return Capture("09-triple-warning");yield return new WaitForSeconds(.1f);yield return Capture("09-triple-travel");
                while(!receipts.Snapshot().RecentDamage.Any(r=>r.ProducerId==TitanicFistsDefinition.Id)){Assert.Less(Time.realtimeSinceStartup,until);yield return null;}
                var data=receipts.Snapshot();Assert.IsTrue(data.RecentDamage.Any(r=>r.Producer=="Conductor"));Assert.IsTrue(data.RecentDamage.Any(r=>r.Producer=="Detonation"));
                File.WriteAllText(Path.Combine(_dir,"native-damage.json"),JsonUtility.ToJson(data,true));yield return Capture("09-triple-confirmed");
            }
            File.WriteAllText(Path.Combine(_dir,"fixture.txt"),"Assisted visual triple: real native boons temporarily applied in memory; AI disabled for controlled target positions, HP changed on target to trigger existing kill detonation, native attack/HP/proc lineage asserted, real HeavySlam telegraph. Separate three-weapon tests have natural AI. No Burn/DoT or boon tuning authored.");
            }
        }

        private static IEnumerator NativeOrientedBasic09B(CharControlScript controls,PlayerActor player,FirstSectorRoomLayout layout)
        {
            controls.SendMessage("OnApplicationFocus",true);float until=Time.realtimeSinceStartup+4;
            while(!controls.TryDirectionalBasicAttack(player.transform.position+layout.ToOriented(Vector3.forward*10))){Assert.Less(Time.realtimeSinceStartup,until);yield return null;}
            do{yield return null;}while(controls.HasScheduledBasic&&Time.realtimeSinceStartup<until);Assert.IsFalse(controls.HasScheduledBasic);
        }
    }
}
