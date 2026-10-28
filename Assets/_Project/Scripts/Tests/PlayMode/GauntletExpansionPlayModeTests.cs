using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private void AcquireGauntletExpansion(string id)
        {
            Assert.IsTrue(BoonExpansionCatalog.TryAcquire(id, _eclipseRun.WeaponModifiers, _eclipseRun.RunWeapon,
                _eclipseRun.Protocols, out string diagnostic), diagnostic);
            _eclipseHolder.RefreshLoadout();
            _player.Stats.criticalChance = 0f;
        }
        private IEnumerator ExpansionIdle()
        {
            float end = Time.realtimeSinceStartup + 6f;
            while ((_eclipseCombat.IsExecuting || _eclipseCombat.PendingChildCount > 0) && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsFalse(_eclipseCombat.IsExecuting); Assert.AreEqual(0, _eclipseCombat.PendingChildCount);
        }
        [UnityTest]
        public IEnumerator ExpansionEchoingFistConfirmsOneFinalHitWithoutASecondCostOrRoot()
        {
            yield return EclipseRig(false); AcquireGauntletExpansion(GauntletExpansionCatalog.EchoingFistId);
            var target = EclipseTarget(Vector3.forward * 1.2f); int echoes = 0, uses = 0;
            float final = 0, echo = 0; long root = 0;
            target.DamageReceived += (_, amount) =>
            {
                var context = _player.CurrentGauntletImpactContext; if (context == null) return;
                if (context.Origin == GauntletImpactOrigin.Direct && context.ImpactIndex == context.Snapshot.LastDirectProducer)
                { final = amount; root = context.RootExecution; }
                if (context.ProducerId == GauntletExpansionCatalog.EchoingFistId)
                {
                    echoes++; echo += amount; Assert.AreEqual(root, context.RootExecution);
                    Assert.IsFalse(context.EnergyEligible); Assert.AreEqual(GauntletImpactOrigin.Echo, context.Origin);
                }
            };
            _eclipseHolder.AbilityUsed += slot => { if (slot == 1) uses++; };
            var runtime = _player.GetComponent<ProtocolRuntime>(); long roots = runtime.Budget.RootsCreated;
            float mana = _player.mana, cost = _eclipseHolder.ActiveAbilities[1].ManaCost;
            Assert.IsTrue(_eclipseHolder.TryUseAbility(1)); yield return ExpansionIdle();
            Assert.AreEqual(1, echoes); Assert.Greater(final, 0); Assert.AreEqual(final * .35f, echo, .02f);
            Assert.AreEqual(1, uses); Assert.AreEqual(mana - cost, _player.mana, .001f);
            Assert.AreEqual(roots + 1, runtime.Budget.RootsCreated);
            Assert.AreEqual(0, _eclipseCombat.EnergyPolicy.RetainedRootCount);
        }
        [UnityTest]
        public IEnumerator ExpansionMissAndEncounterTeardownNeverProduceTemporalEcho()
        {
            yield return EclipseRig(false); AcquireGauntletExpansion(GauntletExpansionCatalog.TemporalEchoId);
            Assert.IsTrue(_eclipseHolder.TryUseAbility(0)); yield return ExpansionIdle();
            Assert.AreEqual(0, _eclipseCombat.PendingChildCount); Assert.AreEqual(0, _eclipseCombat.Energy);
            // A fresh accepted contact does reserve a future, but encounter close must remove it.
            _eclipseHolder.RefreshLoadout(); var target = EclipseTarget(Vector3.forward * 1.2f); int echoes = 0;
            target.DamageReceived += (_, __) => { if (_player.CurrentGauntletImpactContext?.ProducerId == GauntletExpansionCatalog.TemporalEchoId) echoes++; };
            Assert.IsTrue(_eclipseHolder.TryUseAbility(0));
            float end = Time.realtimeSinceStartup + 3;
            while (_eclipseCombat.PendingChildCount == 0 && Time.realtimeSinceStartup < end) yield return null;
            Assert.Greater(_eclipseCombat.PendingChildCount, 0); Assert.AreEqual(0, echoes);
            Assert.IsTrue(_eclipseHolder.TryStopEncounterOffense()); _eclipseRun.CloseProtocolEncounter();
            yield return new WaitForSeconds(.6f);
            Assert.AreEqual(0, echoes); Assert.AreEqual(0, _eclipseCombat.PendingChildCount);
            Assert.AreEqual(0, _eclipseCombat.EnergyPolicy.RetainedRootCount);
        }
        [UnityTest]
        public IEnumerator ExpansionTemporalEchoBoundsHpAndPostureTogetherAtEightThirtyTwoAndSixtyFourTargets()
        {
            foreach (int count in new[] { 8, 32, 64 })
            {
                yield return EclipseRig(false); AcquireGauntletExpansion(GauntletExpansionCatalog.TemporalEchoId);
                var targets = new List<Actor>(); int echoes = 0; float echoHp = 0, nativePostureDamage = 0;
                for (int i = 0; i < count; i++)
                {
                    var target = EclipseTarget(new Vector3((i % 8 - 3.5f) * .1f, 0, 1.2f + (i / 8) * .04f));
                    targets.Add(target);
                    float initialStance = target.GetComponent<CombatReactionController>().CurrentStance;
                    target.DamageReceived += (_, amount) =>
                    {
                        var context = _player.CurrentGauntletImpactContext;
                        if (context?.Origin == GauntletImpactOrigin.Direct)
                            nativePostureDamage += initialStance - target.GetComponent<CombatReactionController>().CurrentStance;
                        if (context?.ProducerId == GauntletExpansionCatalog.TemporalEchoId) { echoes++; echoHp += amount; }
                    };
                }
                float posture = targets.Sum(t => t.GetComponent<CombatReactionController>().CurrentStance);
                var runtime = _player.GetComponent<ProtocolRuntime>(); long roots = runtime.Budget.RootsCreated;
                Assert.IsTrue(_eclipseHolder.TryUseAbility(0));
                float end = Time.realtimeSinceStartup + 3;
                while (_eclipseCombat.IsExecuting && Time.realtimeSinceStartup < end) yield return null;
                yield return ExpansionIdle();
                int expected = Mathf.Min(count, 32);
                Assert.AreEqual(expected, echoes, "Derived HP must stop when the shared causal cap is exhausted.");
                Assert.Greater(echoHp, 0); Assert.AreEqual(roots + 1, runtime.Budget.RootsCreated);
                float nativePerTarget = nativePostureDamage / count;
                float echoPosture = posture - targets.Sum(t => t.GetComponent<CombatReactionController>().CurrentStance) - nativePostureDamage;
                Assert.AreEqual(nativePerTarget * .3f * expected, echoPosture, 1f,
                    "A rejected derived HP admission cannot sneak posture through the independent immunity channel.");
                Assert.AreEqual(0, _eclipseCombat.EnergyPolicy.RetainedRootCount);
                foreach (var target in targets) if (target) Object.DestroyImmediate(target.gameObject);
                if (_player) Object.DestroyImmediate(_player.gameObject);
                yield return null;
            }
        }
        private IEnumerator ExpansionRhythm(bool early, bool late)
        {
            float capture = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60f;
            try
            {
                yield return EarlyAsuraRig(); AcquireGauntletExpansion(GauntletExpansionCatalog.PerfectRhythmId);
                var front = EclipseTarget(Vector3.forward * 1.2f); var behind = EclipseTarget(Vector3.back * 2.7f);
                float hp = behind.health, mana = _player.mana; int uses = 0;
                _eclipseHolder.AbilityUsed += slot => { if (slot == 3) uses++; };
                _eclipseCombat.AddAsuraEnergy(100); Assert.IsTrue(_eclipseHolder.TryUseAbility(3)); yield return EarlyAsuraWait();
                yield return new WaitForSeconds(late ? .45f : .15f);
                if (early) Assert.IsTrue(_eclipseHolder.TryFinishSequence());
                else
                {
                    Assert.IsTrue(_eclipseHolder.TryUseAbility(3)); yield return EarlyAsuraWait();
                    yield return new WaitForSeconds(.15f); Assert.IsTrue(_eclipseHolder.TryUseAbility(3));
                }
                yield return ExpansionIdle();
                if (early || late) Assert.AreEqual(hp, behind.health, .001f, "Only two ideal normal continuations may turn the final punch into a circular wave.");
                else Assert.Less(behind.health, hp, "The perfect finisher must actually reach the rear arc that native punches miss.");
                Assert.AreEqual(1, uses); Assert.AreEqual(mana - _eclipseHolder.ActiveAbilities[3].ManaCost, _player.mana, .001f);
                Assert.Less(front.health, 10000f);
            }
            finally { Time.captureDeltaTime = capture; }
        }
        [UnityTest] public IEnumerator ExpansionPerfectRhythmChangesFinalizerGeometry() { yield return ExpansionRhythm(false, false); }
        [UnityTest] public IEnumerator ExpansionLateRhythmKeepsTheNormalGeometry() { yield return ExpansionRhythm(false, true); }
        [UnityTest] public IEnumerator ExpansionEarlyFinishDoesNotManufacturePerfectRhythm() { yield return ExpansionRhythm(true, false); }
        [UnityTest]
        public IEnumerator ExpansionTitanFallSplitsOneReservedFallAndFreezesBothStagesOnPause()
        {
            yield return EclipseRig(); AcquireGauntletExpansion(GauntletExpansionCatalog.TitanFallId);
            var target = EclipseTarget(Vector3.forward * 1.2f, false, EnemyRank.Boss);
            yield return EclipseCastQ(); Assert.AreEqual(1, _eclipseCombat.EclipseMarkCount);
            var runtime = _player.GetComponent<ProtocolRuntime>(); long roots = runtime.Budget.RootsCreated;
            var children = new List<float>();
            target.DamageReceived += (_, amount) => { if (runtime.ScopedProducer == ProtocolProducer.Technique) children.Add(amount); };
            EclipseBasic(); float hp = target.health;
            Time.timeScale = 0f;
            try { yield return new WaitForSecondsRealtime(.25f); Assert.AreEqual(hp, target.health); Assert.AreEqual(0, children.Count); }
            finally { Time.timeScale = 1f; }
            yield return EclipseWaitForFalls(1);
            Assert.AreEqual(2, children.Count); Assert.Greater(children[0], 0); Assert.AreEqual(children[0], children[1], .001f);
            Assert.AreEqual(roots + 1, runtime.Budget.RootsCreated); Assert.AreEqual(0, _eclipseCombat.EclipseMarkCount);
            Assert.IsFalse(target.GetComponent<CombatReactionController>().IsAirborne);
            Assert.AreEqual(0f, target.transform.position.y, .001f);
        }
    }
}
