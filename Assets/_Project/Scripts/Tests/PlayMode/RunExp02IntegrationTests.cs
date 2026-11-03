using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    [Category("RUNEXP02")]
    public sealed class RunExp02IntegrationTests : RunSummaryIsolatedPlayModeFixture
    {
        private RunExp02Review _review;
        private string _directory;
        private string DirectoryPath => _directory ?? (_directory=CreateDirectory());
        private static string CreateDirectory()
        {
            string dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-02/integration/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);return dir;
        }
        [SetUp] public void Begin() { _directory=null;Time.timeScale=1; }
        [TearDown] public void End() { Time.timeScale=1;if(_review)_review.ClearEncounter(); }
        private IEnumerator Load(bool cover=false)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Prototypes/RunExp02/"+(cover?"ArenaB":"ArenaA")+".unity",new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;yield return null;yield return null;
            _review=Object.FindFirstObjectByType<RunExp02Review>();Assert.IsNotNull(_review);Assert.IsTrue(_review.Player.gameObject.activeInHierarchy);
            Assert.AreEqual(3,_review.Remaining);Assert.IsTrue(_review.Player.GetComponent<CharControlScript>().DirectMovementEnabled);
            Assert.IsNull(Object.FindFirstObjectByType<ProgressionDirector>());Assert.IsNull(Object.FindFirstObjectByType<CoinDrop>());
        }
        private IEnumerator Capture(string name)
        {
            string path=Path.Combine(DirectoryPath,name+".png");Assert.IsFalse(File.Exists(path));
            ScreenCapture.CaptureScreenshot(path);float deadline=Time.realtimeSinceStartup+5;
            while(!File.Exists(path)){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
            yield return null;
        }
        [UnityTest] public IEnumerator BothNativeExecutorsNavigateDamagePauseAndComplete()
        {
            yield return Load();var player=_review.Player;float hp=player.health;
            var starts=_review.Enemies.Select(e=>e.transform.position).ToArray();
            float end=Time.time+9;
            while(Time.time<end && _review.Enemies.Any(e=>e.GetComponent<EnemyCombatActions>().ResolvedImpacts==0))yield return null;
            Assert.Less(player.health,hp,"Native attacks must reach the real PlayerActor.");
            for(int i=0;i<_review.Enemies.Count;i++)
            {
                var enemy=_review.Enemies[i];Assert.Greater(enemy.GetComponent<EnemyCombatActions>().ResolvedImpacts,0,enemy.name);
                Assert.IsTrue(enemy.GetComponent<NavMeshAgent>().isOnNavMesh);
                if(i<2)Assert.Greater(Vector3.Distance(starts[i],enemy.transform.position),2);
            }
            Time.timeScale=0;var positions=_review.Enemies.Select(e=>e.transform.position).ToArray();float pausedHp=player.health;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.AreEqual(pausedHp,player.health);for(int i=0;i<positions.Length;i++)Assert.Less(Vector3.Distance(positions[i],_review.Enemies[i].transform.position),.001f);
            Time.timeScale=1;
            yield return Capture("native-combat-open");
            foreach(var enemy in _review.Enemies.ToArray())enemy.TakeDamage(100000);
            yield return null;yield return null;
            Assert.IsTrue(_review.Completed);Assert.AreEqual(3,_review.Deaths);
            Assert.AreEqual(0,Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length);
            _review.Spawn(3);yield return null;Assert.AreEqual(3,_review.Remaining);Assert.AreEqual(0,_review.Deaths);
            Assert.IsTrue(_review.Enemies.All(e=>e && !e.IsDead && e.CombatGeneration>0));
        }
        [UnityTest] public IEnumerator BothArenasRoutesCoverDashAndWeapons()
        {
            foreach(bool cover in new[]{false,true})
            {
                yield return Load(cover);var player=_review.Player;player.AddDamageImmunity(_review);
                foreach(var enemy in _review.Enemies)
                {var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(enemy.transform.position,player.transform.position,NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);}
                // Left and right circulation lanes remain navigable around the staggered barriers.
                foreach(float x in new[]{-7f,7f})
                {var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(new Vector3(x,0,-6),new Vector3(x,0,7),NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);}
                bool blocked=Physics.Raycast(new Vector3(-2.3f,.7f,-3),Vector3.forward,4,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore);
                Assert.AreEqual(cover,blocked,"Only B places solid cover on this sight line.");
                _review.HoldCombat(true);
                var controls=player.GetComponent<CharControlScript>();var holder=player.GetComponent<AbilityHolder>();
                float southFace=cover?SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BoxCollider>()).Single(c=>c.name=="Cobertura esquerda").bounds.min.z:0;
                player.GetComponent<NavMeshAgent>().Warp(cover?new Vector3(-2.3f,0,southFace-.7f):new Vector3(0,0,-8.7f));
                player.transform.forward=cover?Vector3.forward:Vector3.back;
                Assert.IsTrue(holder.TryUseDash(controls.dashScript));yield return new WaitForSeconds(.5f);
                if(cover)Assert.Less(player.transform.position.z,southFace-.2f,"Dash cannot cross the south face of the cover.");
                else Assert.Greater(player.transform.position.z,-9.8f,"Dash cannot leave the arena through its south boundary.");
                foreach(string path in new[]{"Weapons/Melee/Gauntlet/Gauntlet","Weapons/Melee/Spear/Spear","Weapons/Ranged/Bow_arrow/Bow"})
                {
                    player.EquipWeapon(Resources.Load<WeaponScript>(path));yield return null;
                    controls.CancelCombo();float deadline=Time.time+4;
                    while(!controls.TryDirectionalBasicAttack(player.transform.position+Vector3.right*5)){Assert.Less(Time.time,deadline);yield return null;}
                    yield return new WaitForSeconds(1.2f);Assert.IsFalse(controls.HasScheduledBasic);
                }
                _review.HoldCombat(false);yield return new WaitForSeconds(1);yield return Capture(cover?"combat-cover":"combat-open");
            }
        }
        [UnityTest] public IEnumerator BasicContactReactsAndCoverStopsNativeProjectile()
        {
            yield return Load();_review.HoldCombat(true);var player=_review.Player;player.AddDamageImmunity(_review);
            var enemy=_review.Enemies[0];var agent=enemy.GetComponent<NavMeshAgent>();
            agent.Warp(player.transform.position+Vector3.forward*.85f);enemy.transform.forward=Vector3.back;
            float hp=enemy.health;int receipts=0;enemy.DamageReceived+=(a,d)=>{receipts++;Assert.IsTrue(a.PublishedDamageTransaction.HasValue);Assert.AreEqual("Basic",a.PublishedDamageTransaction.Value.Source.Ability);};
            var controls=player.GetComponent<CharControlScript>();Assert.IsTrue(controls.TryDirectionalBasicAttack(enemy.transform.position));
            yield return new WaitForSeconds(.65f);
            Assert.Less(enemy.health,hp);Assert.Greater(receipts,0);Assert.IsNotNull(enemy.GetComponent<CombatReactionController>());
            yield return Load(true);_review.HoldCombat(true);player=_review.Player;
            var bounds=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BoxCollider>()).Single(c=>c.name=="Cobertura esquerda").bounds;
            player.GetComponent<NavMeshAgent>().Warp(new Vector3(-2.3f,0,bounds.max.z+.7f));
            var emitter=_review.Enemies[2];emitter.GetComponent<NavMeshAgent>().Warp(new Vector3(-2.3f,0,bounds.min.z-.7f));
            float before=player.health;
            EnemyProjectile.Spawn(emitter.transform.position+new Vector3(0,.8f,.2f),Vector3.forward,emitter,player,50,10,10,.12f,EnemyProjectile.PathKind.Straight,Color.red,lockDirection:true);
            yield return new WaitForSeconds(.7f);Assert.AreEqual(before,player.health);Assert.AreEqual(0,Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length);
        }
        [UnityTest] public IEnumerator GameViewPoseSequenceAndRewardsAreReadOnly()
        {
            yield return Load();_review.ShowToolbar=false;_review.Player.AddDamageImmunity(_review);
            var captured=new HashSet<string>();float end=Time.time+10;
            yield return Capture("pair-start-normal-camera");
            while(Time.time<end)
            {
                foreach(var enemy in _review.Enemies.Where(e=>e))
                {
                    var pose=enemy.GetComponent<RunExp02EnemyPose>();string key=(pose.Ranged?"emissor-":"fragmento-")+pose.Phase;
                    if(captured.Add(key))yield return Capture(key);
                }
                if(Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length>0 && captured.Add("projectile"))yield return Capture("emissor-projectile");
                yield return null;
            }
            foreach(string key in new[]{"fragmento-move","fragmento-windup","fragmento-attack","emissor-windup","emissor-attack","projectile"})Assert.IsTrue(captured.Contains(key),key);
            _review.HoldCombat(true);
            var rewards=_review.GetComponent<RunExp02RewardReview>();
            foreach(var weapon in new[]{new[]{"Weapons/Melee/Gauntlet/Gauntlet","Manopla"},new[]{"Weapons/Ranged/Bow_arrow/Bow","Arco"},new[]{"Weapons/Melee/Spear/Spear","Lança"}})
            {
                yield return rewards.Prepare(weapon[0],weapon[1]);Assert.Greater(rewards.Run.Choices.Count,0);Assert.AreEqual(0,rewards.Run.Acquired.Count);
                yield return Capture("rewards-proposed-"+weapon[1]);
                var ids=rewards.Run.Choices.Select(o=>o.Id).ToArray();
                Assert.AreEqual(0,RunModifierPresentation.Count(rewards.Run,"conductor"));
                Assert.IsFalse(RunExp02RewardReview.Interaction(rewards.Run,"conductor").Contains("AO ESCOLHER · o elemento"));
                yield return null;CollectionAssert.AreEqual(ids,rewards.Run.Choices.Select(o=>o.Id).ToArray());
                // Original UI captured only as a fixture with no input; production implementation is unchanged.
                rewards.enabled=false;var native=_review.Player.GetComponent<RunRewardUI>();native.enabled=true;Time.timeScale=1;
                yield return Capture("rewards-existing-"+weapon[1]);native.enabled=false;rewards.enabled=true;Time.timeScale=0;
                Assert.AreEqual(0,rewards.Run.Acquired.Count);
            }
            rewards.Close();yield return null;yield return null;
            Assert.IsNull(_review.Player.GetComponent<RunBoons>());
        }
        [UnityTest] public IEnumerator CandidateControlLockIsBoundedAndReenableStartsNewGeneration()
        {
            yield return Load();_review.Player.AddDamageImmunity(_review);
            foreach(var enemy in _review.Enemies.ToArray())
            {
                var reaction=enemy.GetComponent<CombatReactionController>();
                reaction.ApplyReaction(new HitReactionRequest(_review.Player,enemy.transform.position,Vector3.forward,HitReactionType.Stagger,HitStrength.Heavy,10000,StanceBreakEffect.Stun,0,.25f));
                yield return null;Assert.IsTrue(reaction.IsControlLocked,enemy.name);
                Time.timeScale=0;yield return new WaitForSecondsRealtime(.15f);Assert.IsTrue(reaction.IsControlLocked);Time.timeScale=1;
                yield return new WaitForSeconds(1);Assert.IsFalse(reaction.IsControlLocked);
                long generation=enemy.CombatGeneration;enemy.gameObject.SetActive(false);yield return null;enemy.gameObject.SetActive(true);yield return null;
                Assert.Greater(enemy.CombatGeneration,generation);Assert.IsFalse(enemy.IsDead);Assert.IsFalse(reaction.IsControlLocked);
            }
        }
        [UnityTest] public IEnumerator ReviewCoverKeepsGauntletAndSpearBasicsOutsideReachAcrossSolid()
        {
            yield return Load(true);_review.HoldCombat(true);
            var cover=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<BoxCollider>()).Single(c=>c.name=="Cobertura esquerda").bounds;
            var player=_review.Player;var enemy=_review.Enemies[0];enemy.SetMaxHealth(1000);enemy.RestoreHealthToMax();
            player.GetComponent<NavMeshAgent>().Warp(new Vector3(cover.center.x,0,cover.min.z-.7f));
            enemy.GetComponent<NavMeshAgent>().Warp(new Vector3(cover.center.x,0,cover.max.z+.7f));
            Physics.SyncTransforms();
            foreach(string path in new[]{"Weapons/Melee/Gauntlet/Gauntlet","Weapons/Melee/Spear/Spear"})
            {
                player.EquipWeapon(Resources.Load<WeaponScript>(path));yield return null;
                var controls=player.GetComponent<CharControlScript>();controls.CancelCombo();float before=enemy.health;
                float deadline=Time.time+4;while(!controls.TryDirectionalBasicAttack(enemy.transform.position)){Assert.Less(Time.time,deadline);yield return null;}
                yield return new WaitForSeconds(.8f);Assert.AreEqual(before,enemy.health,path+": opaque review cover must separate the basic hit volumes.");
            }
        }
        [UnityTest] public IEnumerator ActiveCost8() => ActiveCost(8);
        [UnityTest] public IEnumerator ActiveCost32() => ActiveCost(32);
        [UnityTest] public IEnumerator ActiveCost64() => ActiveCost(64);
        [Serializable] private sealed class CostSample
        { public int frame,active,moving,windups,projectiles;public float dtMs;public long[] markers; }
        [Serializable] private sealed class CostReport
        { public int count,renderers,triangles,skinned;public string fixture;public string[] markerNames;public CostSample[] samples; }
        private IEnumerator ActiveCost(int count)
        {
            yield return Load();_review.ShowToolbar=false;_review.Player.AddDamageImmunity(_review);_review.Spawn(count);
            yield return new WaitForSeconds(1);
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            var wanted=new[]{"BehaviourUpdate","Main Thread","NavMeshManager","Components.NavMeshAgent.State","MeshSkinning.Update","MeshSkinning.Skin","RUNEXP02.CustomPose","GC Allocated In Frame"};
            var names=handles.Select(ProfilerRecorderHandle.GetDescription).Select(d=>d.Name).Where(wanted.Contains).Distinct().ToArray();
            File.WriteAllLines(Path.Combine(DirectoryPath,"available-markers-"+count+".txt"),names);
            var recorders=new Dictionary<string,ProfilerRecorder>();
            foreach(var handle in handles)
            {var d=ProfilerRecorderHandle.GetDescription(handle);if(names.Contains(d.Name)&&!recorders.ContainsKey(d.Name))recorders.Add(d.Name,ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame));}
            var rows=new List<CostSample>();float end=Time.time+5;
            while(Time.time<end)
            {
                yield return null;
                rows.Add(new CostSample{frame=Time.frameCount,dtMs=Time.unscaledDeltaTime*1000,active=_review.Enemies.Count(e=>e&&e.GetComponent<EnemyAI>().enabled&&e.GetComponent<NavMeshAgent>().isOnNavMesh),moving=_review.Enemies.Count(e=>e.GetComponent<NavMeshAgent>().velocity.sqrMagnitude>.01f),windups=_review.Enemies.Count(e=>e.GetComponent<EnemyCombatActions>().IsWindingUp),projectiles=Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length,markers=recorders.Select(p=>p.Value.LastValue).ToArray()});
            }
            Assert.AreEqual(count,_review.Remaining);Assert.IsTrue(_review.Enemies.All(e=>e.GetComponent<EnemyAI>().enabled&&e.GetComponent<NavMeshAgent>().isOnNavMesh));
            File.WriteAllText(Path.Combine(DirectoryPath,"active-"+count+".json"),JsonUtility.ToJson(new CostReport{count=count,fixture="AI/nav/poses/executores reais; imunidade do jogador apenas nesta medição. Editor com instrumentação LINQ/alocações, não é aprovação de orçamento.",renderers=_review.GetComponentsInChildren<MeshRenderer>().Length,triangles=_review.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3),skinned=_review.GetComponentsInChildren<SkinnedMeshRenderer>().Length,markerNames=recorders.Keys.ToArray(),samples=rows.ToArray()},true));
            foreach(var recorder in recorders.Values)recorder.Dispose();
            yield return Capture("active-"+count);_review.ClearEncounter();yield return null;yield return null;
            Assert.AreEqual(0,_review.Remaining);Assert.AreEqual(0,_review.GetComponentsInChildren<Actor>().Length);
        }
    }
}
