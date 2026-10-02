using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode regression tests for task 18.2 of combat-foundation-rework — "Garantir não duplicação
    /// de custos e projétil pós-cancel" (R8.5/R8.6/R8.9). They close the three guarantees the task
    /// verifies rather than re-implements, each exercising a real production path:
    ///
    /// <list type="number">
    /// <item><b>(a) No double-charge of mana across an authorized cancel (R8.6).</b> An action A that
    /// has already paid is cancelled into action B (a dash with its own mana cost) through the real
    /// <see cref="AbilityHolder.TryUseDash"/> → shared <c>CancelResolver</c> →
    /// <c>CancelActiveAction</c>(<see cref="BreakerGauntletCombat.FinishForDodge"/>) path. The player's
    /// mana drops by exactly B's cost once, with no refund of A and no second charge of B, and the dash
    /// events fire exactly once each.</item>
    ///
    /// <item><b>(b) The finisher cash-out on cancel is ledger-deduped (R8.9).</b> Modelling
    /// <see cref="BreakerGauntletCombat.FinishForDodge"/>'s <c>_scheduler.Signal</c> cash-out: when the
    /// FixedUpdate clock already emitted the finisher ImpactEvent, the dodge-cancel's signal of that
    /// same index is a no-op through the shared <see cref="ExecutionImpactLedger"/>, so the finisher is
    /// never double-counted (no double damage / double AttackHitsResolved).</item>
    ///
    /// <item><b>(c) An already-launched projectile persists past a cancel (R8.5).</b> An arrow fired
    /// before the cancel is a standalone pooled GameObject with its own lifetime; calling
    /// <see cref="ArsenalCombat.Cancel"/> must not destroy, deactivate, or retract it, and it keeps
    /// travelling and can still resolve a hit afterward.</item>
    /// </list>
    ///
    /// Runs in PlayMode because the player, holder, dash coroutine, projectile <c>Update</c> and
    /// <see cref="ArsenalCombat"/> lifecycle are real MonoBehaviour behaviour.
    /// </summary>
    public sealed class CancelNoDuplicationPlayModeTests
    {
        // ---------------------------------------------------------------------
        // (a) No double-charge of mana / cooldown across an authorized cancel (R8.6)
        // ---------------------------------------------------------------------

        private DashCancelResolverTestRig _cancelRig;

        [SetUp]
        public void SetUp()
        {
            _cancelRig = new DashCancelResolverTestRig();
        }

        [TearDown]
        public void TearDown()
        {
            _cancelRig?.TearDown();
            _cancelRig = null;
        }

        // R8.6 / contract-audit O-7: when action A (an executing Asura cast that has already paid its
        // own cost) is authorized-cancelled into action B (a dash that costs mana), B charges its mana
        // EXACTLY ONCE and A is NOT refunded or re-charged across the cancel boundary. The player's mana
        // after the cancel equals (mana before) - (dash cost), never more (no refund) and never
        // 2 * cost (no double-charge), and AbilityUsed(4)/OnDash each fire once.
        [UnityTest]
        public IEnumerator AuthorizedCancel_ChargesIncomingDashOnce_NoRefundNoDoubleCharge()
        {
            const float startMana = 500f;
            const float dashCost = 60f;

            _cancelRig.Build(mana: startMana);
            _cancelRig.AttachHookBus();

            // Action A: an Asura burst whose Dash CancelRule is open NOW (covers [0,1]); injected into
            // an executing state so IsCasting is genuinely true and the resolver authorizes the cancel.
            // A does not touch the player's mana here (it was "already paid"); the test asserts that
            // cancelling A neither refunds nor re-charges it.
            BreakerGauntletAbility castingA =
                _cancelRig.CreateCastingAbility(dashStart: 0f, dashEnd: 1f, asuraBurst: true);
            _cancelRig.InjectCasting(castingA, elapsed: 0.5f, duration: 1f);

            Assert.IsTrue(_cancelRig.Holder.IsCasting, "action A must report IsCasting (precondition)");

            float manaBefore = _cancelRig.Player.mana;
            Assert.AreEqual(startMana, manaBefore, 0.01f, "mana starts full at the Build value (precondition)");

            // Action B: a dash with a real mana cost, off cooldown. The authorized dash-cancel ends A
            // (FinishForDodge → CancelCast) and activates B, which spends dashCost exactly once.
            DashScript dashB = _cancelRig.CreateDash(iframeWindow: 0.05f, dashTime: 0.1f, manaCost: dashCost);

            bool result = _cancelRig.Holder.TryUseDash(dashB);

            Assert.IsTrue(result, "the dash-cancel of A into B must succeed when the Dash rule is open");
            Assert.IsFalse(_cancelRig.Holder.IsCasting, "action A must have ended after the authorized cancel");

            float manaAfter = _cancelRig.Player.mana;
            Assert.AreEqual(manaBefore - dashCost, manaAfter, 0.01f,
                "B charges its mana exactly once; A is neither refunded nor re-charged (R8.6)");
            Assert.AreNotEqual(manaBefore - 2f * dashCost, manaAfter,
                "mana must not drop by twice the dash cost (no double-charge across the cancel)");
            Assert.Greater(manaAfter, manaBefore - 2f * dashCost + 0.01f,
                "no second charge of B occurred");

            Assert.AreEqual(1, CountUsed(_cancelRig, 4), "AbilityUsed(4) fires exactly once for the incoming dash");
            Assert.AreEqual(1, _cancelRig.OnDashCount, "OnDash fires exactly once for the incoming dash");
            Assert.AreEqual(0, _cancelRig.AbilityRejectedLog.Count,
                "an authorized cancel into an available dash must not reject");

            yield return WaitFor(0.2f); // let the dash coroutine finish so TearDown is clean
        }

        // R8.6: a dash that is on cooldown is rejected BEFORE action A is cancelled, so A keeps running
        // (no cancel) and the player's mana is untouched — the incoming action never charges when its
        // own availability gate fails, and the cancelled action is never refunded because no cancel
        // happened. This pins the "cost-charge-once" contract on the rejection branch of the cancel path.
        [UnityTest]
        public IEnumerator CancelRejectedByCooldown_NoManaSpentNoRefund_ActionAPreserved()
        {
            const float startMana = 500f;
            const float dashCost = 60f;

            _cancelRig.Build(mana: startMana);
            _cancelRig.AttachHookBus();

            BreakerGauntletAbility castingA =
                _cancelRig.CreateCastingAbility(dashStart: 0f, dashEnd: 1f, asuraBurst: true);
            _cancelRig.InjectCasting(castingA, elapsed: 0.5f, duration: 1f);

            float manaBefore = _cancelRig.Player.mana;

            // Dash on cooldown: TryUseDash rejects on the cooldown-first gate, before any cancel.
            DashScript dashB = _cancelRig.CreateDash(dashTime: 0.1f, manaCost: dashCost, cooldownTime: 5f);
            _cancelRig.ForceDashCooldown(dashB, remainingSeconds: 2f);

            bool result = _cancelRig.Holder.TryUseDash(dashB);

            Assert.IsFalse(result, "a dash on cooldown must not activate");
            Assert.IsTrue(_cancelRig.Holder.IsCasting, "action A must keep running — no cancel on a cooldown reject");
            Assert.AreEqual(manaBefore, _cancelRig.Player.mana, 0.01f,
                "no mana is spent and nothing is refunded when the incoming dash is rejected (R8.6)");
            Assert.AreEqual(0, _cancelRig.OnDashCount, "no OnDash on a rejected cancel");
            Assert.AreEqual(1, _cancelRig.AbilityRejectedLog.Count, "exactly one terminal rejection");
            Assert.AreEqual(AbilityUseFailure.Cooldown, _cancelRig.AbilityRejectedLog[0].reason,
                "the dash rejects with Cooldown");

            yield return null;
        }

        // ---------------------------------------------------------------------
        // (b) Finisher cash-out on cancel is ledger-deduped (R8.9)
        // ---------------------------------------------------------------------

        // R8.9: BreakerGauntletCombat.FinishForDodge cashes the finisher through _scheduler.Signal,
        // which is gated by the shared ExecutionImpactLedger. If the FixedUpdate clock already emitted
        // the finisher ImpactEvent (the Active phase reached it before the dodge-cancel), the cancel's
        // Signal of that same index is a NO-OP, so the finisher impact — and thus its damage and
        // AttackHitsResolved — is counted exactly once. This models that exact collision deterministically.
        [UnityTest]
        public IEnumerator FinisherCashOutAfterClockEmitted_IsNoOp_CountedOnce()
        {
            var emitted = new List<int>();

            // Timeline: Startup [0,0.3), Active [0.3,0.9), Recovery [0.9,1]. The finisher is the last
            // ImpactEvent (index 1), authored late in Active at 0.85 — the clock reaches it right before
            // the dodge-cancel would cash it out.
            var ledger = new ExecutionImpactLedger();
            ActionImpactScheduler scheduler = new ActionImpactScheduler(
                ExecutionId.Next(),
                ledger,
                new ActionTimeline(0.3f, 0.9f),
                new[] { Discrete(0, 0.4f), Discrete(1, 0.85f) }, // index 1 = finisher step
                impact => emitted.Add(impact.Index));

            // The single advance source (FixedUpdate clock) runs through Active and emits the finisher.
            scheduler.Advance(0.40f); // first impact
            scheduler.Advance(0.86f); // finisher (index 1) emitted by the clock
            Assert.AreEqual(2, emitted.Count, "the clock emits both impacts in Active (precondition)");
            Assert.AreEqual(1, emitted[1], "the finisher (index 1) was emitted by the clock");

            // Now the dodge-cancel cashes the finisher through the SAME scheduler/ledger path, exactly
            // as FinishForDodge → TryEmitFinisherThroughScheduler → _scheduler.Signal(finisherIndex) does.
            bool cashedOut = scheduler.Signal(1);

            Assert.IsFalse(cashedOut,
                "signalling the finisher after the clock already emitted it must be a no-op (R8.9)");
            Assert.AreEqual(2, emitted.Count,
                "the finisher is counted exactly once across the clock and the dodge-cancel cash-out (R8.9)");
            Assert.AreEqual(1, CountOf(emitted, 1), "the finisher index appears exactly once in the emit log");

            yield return null;
        }

        // R8.9 (opposite ordering): when a dodge-cancel cashes the finisher BEFORE the clock reaches it
        // (an early escape), the finisher emits once via the cancel's Signal, and a later clock Advance
        // across the finisher's instant is the no-op — still exactly one count. Pins dedup from both sides.
        [UnityTest]
        public IEnumerator FinisherCashOutBeforeClock_ThenClockIsNoOp_CountedOnce()
        {
            var emitted = new List<int>();
            var ledger = new ExecutionImpactLedger();
            ActionImpactScheduler scheduler = new ActionImpactScheduler(
                ExecutionId.Next(),
                ledger,
                new ActionTimeline(0.3f, 0.9f),
                new[] { Discrete(0, 0.4f), Discrete(1, 0.85f) },
                impact => emitted.Add(impact.Index));

            scheduler.Advance(0.40f); // first impact only; finisher not yet reached
            Assert.AreEqual(1, emitted.Count, "only the first impact emitted so far (precondition)");

            // Dodge-cancel cashes the finisher early (clock has not reached 0.85 yet).
            bool cashedOut = scheduler.Signal(1);
            Assert.IsTrue(cashedOut, "the early dodge-cancel cash-out emits the finisher once");
            Assert.AreEqual(2, emitted.Count, "the finisher emitted via the cancel's Signal");

            // A later clock tick across the finisher's instant must be the no-op.
            scheduler.Advance(0.86f);
            Assert.AreEqual(2, emitted.Count,
                "the clock crossing an already-cashed finisher is a no-op — counted once (R8.9)");

            yield return null;
        }

        // ---------------------------------------------------------------------
        // (c) An already-launched projectile persists past a cancel (R8.5)
        // ---------------------------------------------------------------------

        private ArsenalProjectileTestRig _projectileRig;

        // R8.5: a projectile fired BEFORE a cast is cancelled is a standalone pooled GameObject with its
        // own lifetime — ArsenalCombat.Cancel() must not destroy, deactivate, or retract it. The arrow
        // stays active, keeps its remaining range, and continues to travel and resolve a hit afterward.
        [UnityTest]
        public IEnumerator FiredProjectile_SurvivesArsenalCancel_AndStillResolvesHit()
        {
            _projectileRig = new ArsenalProjectileTestRig();
            try
            {
                _projectileRig.BuildOwner();

                // An enemy down-range so the surviving arrow can still land a hit after the cancel.
                Actor target = _projectileRig.BuildEnemy(new Vector3(0f, 0f, 6f), size: new Vector3(2f, 2f, 2f));

                // Fire the arrow BEFORE the cancel. It is owned by the pool/Update loop, not by any cast.
                ArsenalProjectile arrow = _projectileRig.FireArrow(
                    origin: new Vector3(0f, 1f, 0f), direction: Vector3.forward,
                    range: 20f, piercing: false, ricochetBounces: 0);

                Assert.IsNotNull(arrow, "the arrow must have spawned");
                Assert.IsTrue(arrow.gameObject.activeInHierarchy, "the arrow is live right after firing");
                float remainingBefore = _projectileRig.Remaining(arrow);
                Assert.Greater(remainingBefore, 0f, "the arrow has travel budget before the cancel");

                // Add an ArsenalCombat to the owner and cancel it. Cancel() only stops the cast coroutine
                // and restores the agent — it never references live projectiles.
                ArsenalCombat combat = _projectileRig.Owner.gameObject.AddComponent<ArsenalCombat>();
                combat.Cancel();
                yield return null; // one frame of the arrow's own Update after the cancel

                Assert.IsTrue(arrow && arrow.gameObject,
                    "the fired arrow must still exist after ArsenalCombat.Cancel() (R8.5)");
                Assert.IsTrue(arrow.gameObject.activeInHierarchy,
                    "ArsenalCombat.Cancel() must not deactivate an already-launched projectile (R8.5)");

                // The arrow keeps travelling: let it reach the enemy and resolve a hit on its own lifetime.
                int before = _projectileRig.DamageEvents.Count;
                yield return WaitFor(0.6f);

                Assert.Greater(_projectileRig.DamageEvents.Count, before,
                    "a projectile launched before the cancel still resolves a valid hit afterward (R8.5)");
                Assert.Contains(target, _projectileRig.DamageEvents,
                    "the surviving arrow hit the down-range enemy after the cancel (R8.5)");
            }
            finally
            {
                _projectileRig.TearDown();
                _projectileRig = null;
            }
        }

        // --- helpers ---------------------------------------------------------

        private static int CountUsed(DashCancelResolverTestRig rig, int index)
        {
            int n = 0;
            foreach (int i in rig.AbilityUsedLog) if (i == index) n++;
            return n;
        }

        private static int CountOf(List<int> log, int value)
        {
            int n = 0;
            foreach (int i in log) if (i == value) n++;
            return n;
        }

        // ImpactEvent is a serialized struct with private fields; box one instance, write the fields
        // through reflection so successive writes land on the same box, then unbox — the same shape the
        // authored profiles feed the executor (mirrors ActionImpactSchedulerPlayModeTests).
        private static ImpactEvent Discrete(int index, float at)
        {
            object boxed = new ImpactEvent();
            SetField(boxed, "index", index);
            SetField(boxed, "at", at);
            SetField(boxed, "opensWindow", false);
            SetField(boxed, "windowEnd", 0f);
            SetField(boxed, "hitStopProfileIndex", -1);
            SetField(boxed, "areaHitStepIndex", index);
            return (ImpactEvent)boxed;
        }

        private static void SetField(object boxed, string field, object value)
        {
            FieldInfo fi = typeof(ImpactEvent).GetField(field,
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(fi, $"ImpactEvent must have a '{field}' field");
            fi.SetValue(boxed, value);
        }

        private static IEnumerator WaitFor(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }
    }
}
