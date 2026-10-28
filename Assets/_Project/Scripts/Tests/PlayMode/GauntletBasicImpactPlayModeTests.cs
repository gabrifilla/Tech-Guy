using System.Collections;
using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        // Existing isolated actor/NavMesh fixture, but real weapon profile, controller, avatar and clips.
        // These tests may manipulate fixture targets; gameplay observation uses untouched production actors.
        private void BuildAuthoredBasicPlayer()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var visual = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PlayerArmature.prefab"), _player.transform);
            var source = visual.GetComponent<Animator>();
            var animator = _player.GetComponent<Animator>();
            animator.runtimeAnimatorController = source.runtimeAnimatorController; animator.avatar = source.avatar;
            source.enabled = false;
            animator.Rebind(); animator.Update(0);
            _player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"));
            _player.GetComponent<AbilityHolder>().RefreshLoadout();
        }

        private Actor BasicVictim(Vector3 position)
        {
            var victim = BuildEnemy(position).actor;
            var coins = victim.GetComponent<CoinDrop>(); if (coins) Object.DestroyImmediate(coins);
            return victim;
        }
        private IEnumerator WaitForBasicEnd()
        {
            float deadline = Time.realtimeSinceStartup + 3;
            while ((bool)PlayerBusyField.GetValue(_controls))
            { Assert.Less(Time.realtimeSinceStartup,deadline,"Basic must leave busy/recovery."); yield return null; }
        }

        [UnityTest] public IEnumerator GIBasicPreImpactMovementCancelsWithoutGhostHitOrEnergy()
        {
            BuildAuthoredBasicPlayer(); var victim=BasicVictim(Vector3.forward*1.2f); yield return null;
            float hp=victim.health;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            MoveToPosition(Vector3.right*3);
            yield return new WaitForSeconds(.3f);
            Assert.AreEqual(hp,victim.health); Assert.IsFalse(_controls.HasScheduledBasic);
            Assert.AreEqual(0,_player.GetComponent<BreakerGauntletCombat>().Energy);
            Assert.IsFalse(_player.GetComponent<SkillAnimationPlayer>().IsPlaying);
            Assert.Greater(_player.transform.position.x,.1f,"Existing movement must resume.");
        }

        [UnityTest] public IEnumerator GIBasicCommittedAreaSurvivesReentrantCancelExactlyOnce()
        {
            BuildAuthoredBasicPlayer(); var a=BasicVictim(new Vector3(-.3f,0,1.2f));
            var b=BasicVictim(new Vector3(.3f,0,1.2f)); yield return null;
            int hits=0;
            a.DamageReceived+=(actor,damage)=>{hits++;_controls.CancelCombo();};
            b.DamageReceived+=(actor,damage)=>{hits++;_controls.CancelCombo();};
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward));
            yield return new WaitForSeconds(.35f);
            Assert.AreEqual(2,hits,"Already committed overlap resolves each eligible target once.");
            Assert.AreEqual(3,_player.GetComponent<BreakerGauntletCombat>().Energy,"Existing per-root basic energy cap.");
            Assert.AreEqual(0,_player.GetComponent<BreakerGauntletCombat>().EnergyPolicy.RetainedRootCount);
            Assert.IsFalse(_controls.HasScheduledBasic);
        }

        [UnityTest] public IEnumerator GIBasicMovingTargetWhiffsWithoutHomingOrResources()
        {
            BuildAuthoredBasicPlayer();var victim=BasicVictim(Vector3.forward*1.2f);yield return null;
            float hp=victim.health; Assert.IsTrue(_controls.TryBasicAttack(victim.transform.position));
            victim.transform.position=Vector3.right*10;Physics.SyncTransforms();
            yield return WaitForBasicEnd();
            Assert.AreEqual(hp,victim.health);Assert.AreEqual(0,_player.GetComponent<BreakerGauntletCombat>().Energy);
            Assert.Less(Vector3.Angle(Vector3.forward,_player.transform.forward),.1f,"No new homing/aim lock.");
        }

        [UnityTest] public IEnumerator GIBasicTargetChangeDoesNotRedirectAcceptedSwing()
        {
            BuildAuthoredBasicPlayer();var a=BasicVictim(Vector3.forward*1.2f);
            var b=BasicVictim(Vector3.right*2.4f);yield return null;
            float ahp=a.health,bhp=b.health;
            Assert.IsTrue(_controls.TryBasicAttack(a.transform.position));
            SetTarget(b.GetComponent<Interactable>());
            yield return WaitForBasicEnd();
            Assert.Less(a.health,ahp);Assert.AreEqual(bhp,b.health);
        }

        [UnityTest] public IEnumerator GIBasicDestroyedOrDisabledVictimIsSafeAtContact()
        {
            BuildAuthoredBasicPlayer();var victim=BasicVictim(Vector3.forward*1.2f);yield return null;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            victim.gameObject.SetActive(false);Object.DestroyImmediate(victim.gameObject);
            yield return WaitForBasicEnd();Assert.AreEqual(0,_player.GetComponent<BreakerGauntletCombat>().Energy);
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward));yield return WaitForBasicEnd();
        }

        [UnityTest] public IEnumerator GIBasicVictimDeathBeforeContactCannotReceiveAnotherTransaction()
        {
            BuildAuthoredBasicPlayer();var victim=BasicVictim(Vector3.forward*1.2f);yield return null;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            victim.TakeDamage(victim.health); // Explicit fixture death, not attack damage.
            yield return WaitForBasicEnd();Assert.AreEqual(0,_player.GetComponent<BreakerGauntletCombat>().Energy);
        }

        [UnityTest] public IEnumerator GIBasicWeaponSwitchAndDisableCancelPendingContact()
        {
            BuildAuthoredBasicPlayer();var victim=BasicVictim(Vector3.forward*1.2f);yield return null;float hp=victim.health;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[2]));
            yield return new WaitForSeconds(.3f);Assert.AreEqual(hp,victim.health);
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]));
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));_controls.enabled=false;
            yield return new WaitForSeconds(.3f);Assert.AreEqual(hp,victim.health);Assert.IsFalse(_controls.HasScheduledBasic);
        }

        [UnityTest] public IEnumerator GIBasicPausePreservesPendingContactAndPose()
        {
            BuildAuthoredBasicPlayer();var victim=BasicVictim(Vector3.forward*1.2f);yield return null;float hp=victim.health;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            float pose=_player.GetComponent<SkillAnimationPlayer>().NormalizedTime;
            Time.timeScale=0;yield return new WaitForSecondsRealtime(.12f);
            Assert.AreEqual(hp,victim.health);Assert.AreEqual(pose,_player.GetComponent<SkillAnimationPlayer>().NormalizedTime);
            Time.timeScale=1;yield return WaitForBasicEnd();Assert.Less(victim.health,hp);
        }

        [UnityTest] public IEnumerator GIBasicRapidSequenceKeepsCadenceSingleTransactionsAndRecovery()
        {
            BuildAuthoredBasicPlayer();var victim=BasicVictim(Vector3.forward*1.2f);yield return null;
            int hits=0;victim.DamageReceived+=(actor,damage)=>hits++;
            for(int i=0;i<5;i++)
            {
                float deadline=Time.realtimeSinceStartup+2;
                while(!_controls.TryDirectionalBasicAttack(victim.transform.position))
                {Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                Assert.AreEqual(i,hits,"A new command must not emit synchronously.");
                yield return WaitForBasicEnd();Assert.AreEqual(i+1,hits);
            }
            Assert.AreEqual(5,_attackCount);Assert.AreEqual(15,_player.GetComponent<BreakerGauntletCombat>().Energy);
            Assert.AreEqual(5,_player.GetComponent<HitStopRunner>().AppliedCount);
            Assert.IsFalse(_player.GetComponent<SkillAnimationPlayer>().IsPlaying);
        }

        [UnityTest] public IEnumerator GIBasicCoarseAdvanceAndAnimationSignalCannotDoubleEmit()
        {
            BuildAuthoredBasicPlayer();var victim=BasicVictim(Vector3.forward*1.2f);yield return null;
            int hits=0;victim.DamageReceived+=(actor,damage)=>hits++;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            // Explicit clock fixture: cross contact and recovery in one production scheduler advance.
            SetPrivate(_controls,"_basicStartedAt",Time.time-_controls.BasicImpactDuration*.8f);
            typeof(CharControlScript).GetMethod("FixedUpdate",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(_controls,null);
            var scheduler=(ActionImpactScheduler)typeof(CharControlScript).GetField("_basicScheduler",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(_controls);
            scheduler.Advance(.4f);Assert.IsFalse(scheduler.Signal(0));scheduler.Advance(.9f);
            Assert.AreEqual(1,hits);yield return WaitForBasicEnd();Assert.AreEqual(1,hits);
        }

        [UnityTest] public IEnumerator GIBasicCritProtocolAndEnergyKeepOneCausalRoot()
        {
            BuildAuthoredBasicPlayer();var run=_player.gameObject.AddComponent<RunBoons>();yield return null;
            var victim=BasicVictim(Vector3.forward*1.2f);_player.Stats.criticalChance=100;
            var definition=ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.IsTrue(definition.TryCreateSnapshot(out var catalog,out _));Object.DestroyImmediate(definition);
            var inventory=new ProtocolInventory(Guid.NewGuid(),catalog);
            var runtime=_player.GetComponent<ProtocolRuntime>();runtime.Configure(_player,inventory,run);
            int hits=0,basics=0,crits=0,protocols=0,gains=0;
            victim.DamageReceived+=(actor,damage)=>{hits++;Assert.IsTrue(actor.PublishedDamageTransaction.HasValue);};
            run.Hooks.OnBasicHit+=(actor,damage)=>basics++;run.Hooks.OnCrit+=(actor,damage)=>crits++;
            run.Hooks.OnProtocolImpact+=impact=>protocols++;
            _player.GetComponent<BreakerGauntletCombat>().EnergyPolicy.Committed+=gain=>gains++;
            long roots=runtime.Budget.RootsCreated;float mana=_player.mana;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            Assert.AreEqual(0,hits);Assert.AreEqual(0,basics);Assert.AreEqual(0,crits);Assert.AreEqual(0,gains);
            yield return WaitForBasicEnd();
            Assert.AreEqual(1,hits);Assert.AreEqual(1,basics);Assert.AreEqual(1,crits);Assert.AreEqual(1,protocols);Assert.AreEqual(1,gains);
            Assert.AreEqual(roots+1,runtime.Budget.RootsCreated);Assert.AreEqual(0,runtime.Budget.LiveRoots);
            Assert.AreEqual(mana,_player.mana,"Basic must not introduce a second resource cost.");
        }

        [UnityTest] public IEnumerator GIBasicSkillBeforeContactCancelsOnlyPendingBasic()
        {
            BuildAuthoredBasicPlayer(); var victim=BasicVictim(Vector3.forward*1.2f); yield return null;
            _player.mana=500f; int basicHits=0;
            victim.DamageReceived+=(actor,damage)=>
            { if(actor.PublishedDamageTransaction?.Source?.Ability=="Basic") basicHits++; };
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            Assert.IsTrue(_player.GetComponent<AbilityHolder>().TryUseAbility(0));
            Assert.IsFalse(_controls.HasScheduledBasic);
            yield return new WaitForSeconds(.5f);
            Assert.AreEqual(0,basicHits,"The accepted skill must not leave a pending basic hit.");
        }

        [UnityTest] public IEnumerator GIBasicOwnerDeathCancelsBeforeContact()
        {
            BuildAuthoredBasicPlayer(); var victim=BasicVictim(Vector3.forward*1.2f); yield return null;
            float hp=victim.health; int gains=0;
            var energy=_player.GetComponent<BreakerGauntletCombat>().EnergyPolicy;
            energy.Committed+=gain=>gains++;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            _player.TakeDamage(_player.health+1000f);
            Assert.IsTrue(_player.IsDead);
            yield return new WaitForSeconds(.3f);
            // The real death path destroys this minimal actor. Inspect the retained managed policy,
            // not GetComponent on the destroyed Unity wrapper.
            Assert.AreEqual(hp,victim.health);
            Assert.IsFalse(_controls.HasScheduledBasic);
            Assert.AreEqual(0,gains); Assert.AreEqual(0,energy.RetainedRootCount);
        }

        [UnityTest] public IEnumerator GIBasicBowStillLaunchesImmediatelyAndHitsAfterTravel()
        {
            BuildAuthoredBasicPlayer(); var victim=BasicVictim(Vector3.forward*4f+Vector3.up); yield return null;
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1]));
            float hp=victim.health; int hits=0;
            victim.DamageReceived+=(actor,damage)=>hits++;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
            Assert.AreEqual(hp,victim.health); Assert.IsFalse(_controls.HasScheduledBasic);
            Assert.AreEqual(1,Object.FindObjectsByType<ArsenalProjectile>(FindObjectsSortMode.None).Length,
                "A Bow command must still launch its projectile in the accepted frame.");
            float deadline=Time.realtimeSinceStartup+2f;
            while(hits==0) { Assert.Less(Time.realtimeSinceStartup,deadline); yield return null; }
            Assert.Less(victim.health,hp); Assert.AreEqual(1,hits);
            yield return new WaitForSeconds(.2f); Assert.AreEqual(1,hits);
        }

        [UnityTest] public IEnumerator GIBasicSpearKeepsImmediateLegacyEmission()
        {
            BuildAuthoredBasicPlayer();var victim=BasicVictim(Vector3.forward*1.2f);yield return null;
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[2]));float hp=victim.health;
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));Assert.Less(victim.health,hp);
            Assert.IsFalse(_controls.HasScheduledBasic);yield return null;
        }

        [UnityTest]
        public IEnumerator GIBasicAcceptedAttackWaitsForAuthoredContact()
        {
            BuildAuthoredBasicPlayer(); var enemy = BuildEnemy(Vector3.forward * 1.2f).actor;
            yield return null;
            float hp = enemy.health; int transactions = 0;
            enemy.DamageReceived += (actor, damage) => {
                Assert.IsTrue(actor.PublishedDamageTransaction.HasValue); transactions++;
                var motion=_player.GetComponent<SkillAnimationPlayer>();
                var library=Resources.Load<SkillAnimationLibrary>(SkillAnimationLibrary.ResourcePath);
                Assert.IsTrue(motion.IsPlaying);
                Assert.AreEqual(library.Get(SkillMotion.PunchRight).contact,motion.NormalizedTime,.0001f,
                    "Confirmed HP must see the actual authored clip contact pose.");
            };
            Assert.IsTrue(_controls.TryDirectionalBasicAttack(enemy.transform.position));
            Assert.AreEqual(1, _attackCount, "Acceptance must remain immediate.");
            Assert.AreEqual(hp, enemy.health, "Accepted input must not apply HP before authored visual contact.");
            float duration = (float)typeof(CharControlScript).GetField("attackBusyDuration",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).GetValue(_controls);
            float contact = _player.CurrentWeapon.BasicProfile.ImpactEvents[0].At * duration;
            float start = Time.time;
            while (Time.time - start < contact - Time.fixedDeltaTime)
            { Assert.AreEqual(hp,enemy.health); yield return null; }
            float deadline = Time.realtimeSinceStartup + 2;
            while (transactions == 0) { Assert.Less(Time.realtimeSinceStartup,deadline); yield return null; }
            Assert.GreaterOrEqual(Time.time-start,contact-.001f);
            Assert.AreEqual(1,transactions);
            yield return new WaitForSeconds(.35f);
            Assert.AreEqual(1,transactions,"One authored impact must stay one HP transaction.");
        }
    }
}
