using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private static object ResponsivenessPreview(string method, params object[] args) =>
            Type.GetType("CombatPolishPreview, TechGuy.Editor", true)
                .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, args);

        private string BeginResponsivenessTrace(string label)
        {
            string path=Path.Combine("Library/GameplayObservation/gi15",label+"-"+Guid.NewGuid().ToString("N"));
            ResponsivenessPreview("BeginObservationTelemetry",_player,null,path,Time.frameCount);
            return path;
        }

        private void BuildResponsivenessPlayer()
        {
            BuildAuthoredBasicPlayer();
            // Synthetic resource reserve, as in the existing movement fixture. Raising current
            // mana alone is clamped back to this minimal actor's small authored maximum.
            SetPrivate(_player,"baseMaxMana",500f);
            SetPrivate(_player,"_manaRegenerationPercentPerSecond",0f);
            _player.RefreshResourceStats(); _player.mana=500f;
        }

        [UnityTest]
        public IEnumerator GI15RecoveryRejectsEarlySkillOnceWithoutReplayingItLater()
        {
            BuildResponsivenessPlayer(); yield return null;
            var holder=_player.GetComponent<AbilityHolder>(); var combat=_player.GetComponent<BreakerGauntletCombat>();
            int used=0,rejected=0; holder.AbilityUsed+=slot=>{if(slot==2)used++;};
            holder.AbilityRejected+=(slot,reason)=>{if(slot==2){Assert.AreEqual(AbilityUseFailure.Busy,reason);rejected++;}};
            int first=Time.frameCount;
            string trace=BeginResponsivenessTrace("early-recovery");
            try
            {
                Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder);
                Assert.IsTrue(combat.TryGetCancelContext(out var profile,out float progress));
                float start=profile.CancelRules.Single(rule=>rule.Target==CancelTarget.Skill).Start;
                Assert.Less(progress,start,"Fixture must attempt inside recovery before the authored skill window.");
                ResponsivenessPreview("RecordObservationEvent","FixtureSkillAttempt",_player,
                    "API E during early Q recovery; global_progress="+progress.ToString(System.Globalization.CultureInfo.InvariantCulture));
                float mana=_player.mana;
                Assert.IsFalse(holder.TryUseAbility(2)); Assert.AreEqual(1,rejected); Assert.AreEqual(0,used);
                Assert.IsTrue(holder.IsCasting); Assert.IsFalse(holder.HasBufferedOffensiveIntent);
                float deadline=Time.realtimeSinceStartup+3f; int last=-1;
                while(holder.IsCasting)
                {
                    Assert.Less(Time.realtimeSinceStartup,deadline); yield return null;
                    int frame=Time.frameCount-first;
                    if(frame>last){ResponsivenessPreview("SampleObservationTelemetry",frame);last=frame;}
                }
                yield return new WaitForSeconds(.1f);
                Assert.AreEqual(0,used,"A rejected ordinary skill must not become a delayed automatic cast.");
                Assert.AreEqual(1,rejected); Assert.AreEqual(mana,_player.mana,.001f);
                ResponsivenessPreview("RecordObservationEvent","FixtureSkillAttempt",_player,"API E after Q ended; new command");
                Assert.IsTrue(holder.TryUseAbility(2)); Assert.AreEqual(1,used);
                yield return WaitForMovementCastToFinish(holder);
            }
            finally { ResponsivenessPreview("EndObservationTelemetry"); }
            string evidence=File.ReadAllText(Path.Combine(trace,"events.jsonl"));
            Assert.IsTrue(evidence.Contains("AbilityRejected")); Assert.IsTrue(evidence.Contains("reason=Busy"));
        }

        [UnityTest]
        public IEnumerator GI15LateRecoverySkillAcceptsOnceAndStartsItsPresentation()
        {
            BuildResponsivenessPlayer(); yield return null;
            var holder=_player.GetComponent<AbilityHolder>(); int used=0,rejected=0;
            holder.AbilityUsed+=slot=>{if(slot==2)used++;}; holder.AbilityRejected+=(slot,reason)=>rejected++;
            BeginResponsivenessTrace("late-recovery");
            try
            {
                Assert.IsTrue(holder.TryUseAbility(0)); yield return WaitForMovementRecovery(holder,.80f);
                float mana=_player.mana;
                ResponsivenessPreview("RecordObservationEvent","FixtureSkillAttempt",_player,"API E in authored Q Skill window");
                Assert.IsTrue(holder.TryUseAbility(2)); Assert.AreEqual(1,used); Assert.AreEqual(0,rejected);
                Assert.AreEqual(mana-holder.ActiveAbilities[2].ManaCost,_player.mana,.001f);
                Assert.AreSame(holder.ActiveAbilities[2],_player.GetComponent<BreakerGauntletCombat>().ActiveAbility);
                Assert.IsFalse(holder.HasBufferedOffensiveIntent);
                int acceptedFrame=Time.frameCount;
                // GI1.3 commits native presentation in LateUpdate. Inspect its first rendered
                // frame, not the coroutine boundary immediately before the owner's LateUpdate.
                yield return new WaitForEndOfFrame();
                Assert.AreEqual(acceptedFrame,Time.frameCount);
                Assert.IsTrue(_player.GetComponent<SkillAnimationPlayer>().IsPlaying);
                yield return WaitForMovementCastToFinish(holder); Assert.AreEqual(1,used);
            }
            finally { ResponsivenessPreview("EndObservationTelemetry"); }
        }

        [UnityTest]
        public IEnumerator GI15SkillAcceptedDuringBasicHitstopKeepsOneUseAndResumesItsPresentation()
        {
            BuildResponsivenessPlayer(); var victim=BasicVictim(Vector3.forward*1.2f); yield return null;
            var holder=_player.GetComponent<AbilityHolder>(); var combat=_player.GetComponent<BreakerGauntletCombat>();
            int uses=0; holder.AbilityUsed+=slot=>{if(slot==0)uses++;};
            BeginResponsivenessTrace("skill-during-hitstop");
            try
            {
                Assert.IsTrue(_controls.TryDirectionalBasicAttack(victim.transform.position));
                float deadline=Time.realtimeSinceStartup+2f;
                while(!_player.IsCombatHitStopped){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                float mana=_player.mana;
                ResponsivenessPreview("RecordObservationEvent","FixtureSkillAttempt",_player,"API Q while confirmed basic hitstop is active");
                Assert.IsTrue(holder.TryUseAbility(0)); Assert.AreEqual(1,uses);
                Assert.AreEqual(mana-holder.ActiveAbilities[0].ManaCost,_player.mana,.001f);
                Assert.AreSame(holder.ActiveAbilities[0],combat.ActiveAbility);
                Assert.IsFalse(holder.HasBufferedOffensiveIntent);
                while(_player.IsCombatHitStopped){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                yield return new WaitForEndOfFrame();
                Assert.IsTrue(_player.GetComponent<SkillAnimationPlayer>().IsPlaying);
                yield return WaitForMovementCastToFinish(holder); Assert.AreEqual(1,uses);
            }
            finally { ResponsivenessPreview("EndObservationTelemetry"); }
        }

        [UnityTest]
        public IEnumerator GI15InputObservationRespectsRebindingAndDoesNotConsumeOrReplayInput()
        {
            const string key="TechGuy.Settings.Binding.Skill1";
            bool existed=PlayerPrefs.HasKey(key); int previous=PlayerPrefs.GetInt(key);
            string trace=null;
            try
            {
                PlayerPrefs.SetInt(key,(int)KeyCode.F9); BuildFluidMovementRig(); yield return null;
                var holder=_player.GetComponent<AbilityHolder>(); int uses=0;
                holder.AbilityUsed+=slot=>{if(slot==0)uses++;};
                using(new CursorInputFocusScope()) using(var input=new FluidityInputDevices())
                {
                    trace=BeginResponsivenessTrace("virtual-input"); int first=Time.frameCount;
                    input.Queue(GameControl.Skill1,true,Vector2.zero); yield return null; yield return new WaitForEndOfFrame();
                    bool held=GamePreferences.IsHeld(GameControl.Skill1), pressed=GamePreferences.WasPressed(GameControl.Skill1);
                    Assert.IsTrue(held);
                    ResponsivenessPreview("SampleObservationTelemetry",Time.frameCount-first);
                    Assert.AreEqual(held,GamePreferences.IsHeld(GameControl.Skill1));
                    Assert.AreEqual(pressed,GamePreferences.WasPressed(GameControl.Skill1));
                    yield return null; yield return new WaitForEndOfFrame();
                    ResponsivenessPreview("SampleObservationTelemetry",Time.frameCount-first);
                    input.Queue(GameControl.Skill1,false,Vector2.zero); yield return null; yield return new WaitForEndOfFrame();
                    ResponsivenessPreview("SampleObservationTelemetry",Time.frameCount-first);
                    Assert.AreEqual(1,uses); ResponsivenessPreview("EndObservationTelemetry");
                }
            }
            finally
            {
                ResponsivenessPreview("EndObservationTelemetry");
                if(existed)PlayerPrefs.SetInt(key,previous);else PlayerPrefs.DeleteKey(key);
            }
            var records=File.ReadLines(Path.Combine(trace,"events.jsonl"))
                .Select(line=>JsonUtility.FromJson<ResponsivenessRecord>(line)).ToArray();
            var inputs=records.Where(row=>row.kind=="InputEdgeObserved")
                .Select(row=>JsonUtility.FromJson<ResponsivenessInput>(row.detail)).Where(row=>row.control=="Skill1").ToArray();
            Assert.AreEqual(1,inputs.Count(row=>row.pressed)); Assert.AreEqual(1,inputs.Count(row=>!row.held));
            Assert.IsTrue(inputs.All(row=>row.binding=="F9"));
            Assert.AreEqual(1,records.Count(row=>row.kind=="AbilityUsed"));
        }

        [Serializable] private sealed class ResponsivenessRecord { public string kind,detail; }
        [Serializable] private sealed class ResponsivenessInput { public string control,binding; public bool pressed,held; }
    }
}
