using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode integration tests for the R4/R6 dash-cancel path of combat-foundation-rework
    /// (task 13.7). They drive the real <see cref="AbilityHolder.TryUseDash"/> pipeline — the shared
    /// <see cref="CancelResolver"/> routing, the cooldown-first reject, the temporal buffer serviced by
    /// <c>TickDashBuffer</c>, the <see cref="DashScript"/> i-frame window, and the dash event ordering —
    /// through a minimal but real player built by <see cref="DashCancelResolverTestRig"/>.
    ///
    /// <para>
    /// The "while casting" scenarios inject a real <see cref="BreakerGauntletCombat"/> into an executing
    /// state (so <see cref="AbilityHolder.IsCasting"/> is genuinely true and the production
    /// <c>ResolveCancel</c>/<see cref="CancelResolver"/> read its authored Dash
    /// <see cref="CombatActionProfile"/> at the real progress), rather than standing up the full cast
    /// coroutine — which needs a weapon prefab, hitbox, Animator, 210 mana and a charged Asura meter and
    /// would make the exact progress timing non-deterministic (the task permits this reduction).
    /// </para>
    ///
    /// Each test asserts exactly one concern (R6.9/O-5 ordering, R6.6 cooldown, R6.2–R6.5 buffer/outside
    /// window, R6.7/R6.8 i-frames, R4.7/O-9 IsCasting semantics) and steps frames with
    /// <c>yield return null</c>. Runs in PlayMode because the player, holder, dash coroutine and immunity
    /// channel are real MonoBehaviour lifecycle.
    /// </summary>
    public sealed class DashCancelResolverPlayModeTests
    {
        private DashCancelResolverTestRig _rig;

        [SetUp]
        public void SetUp()
        {
            _rig = new DashCancelResolverTestRig();
            _rig.Build();
            _rig.AttachHookBus();
        }

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // R6.9 / contract-audit O-5: a successful dash with no action in progress and off cooldown fires
        // AbilityUsed(4) exactly once AND the run's OnDash hook exactly once, with AbilityUsed(4) BEFORE
        // OnDash. Proves the dash goes through the holder and raises the existing dash events once each.
        [UnityTest]
        public IEnumerator SuccessPath_FiresAbilityUsedThenOnDash_EachOnce()
        {
            DashScript dash = _rig.CreateDash(iframeWindow: 0.05f, dashTime: 0.1f);

            bool result = _rig.Holder.TryUseDash(dash);

            Assert.IsTrue(result, "a dash off cooldown with no action in progress should succeed");
            Assert.AreEqual(1, CountUsed(4), "AbilityUsed(4) must fire exactly once");
            Assert.AreEqual(1, _rig.OnDashCount, "OnDash must fire exactly once");
            Assert.AreEqual(0, _rig.AbilityRejectedLog.Count, "a successful dash must not reject");
            CollectionAssert.AreEqual(new[] { "used:4", "dash" }, _rig.OrderTrace,
                "AbilityUsed(4) must precede OnDash (contract-audit O-5)");

            // Let the dash coroutine finish so TearDown is clean.
            yield return WaitFor(0.2f);
        }

        // R6.6 / O-5: a dash on cooldown rejects exactly once with AbilityUseFailure.Cooldown, leaves the
        // remaining cooldown untouched, raises no OnDash, and never fires AbilityUsed. The reject happens
        // BEFORE any source cancel, so an active action (none here) would keep its phase.
        [UnityTest]
        public IEnumerator OnCooldown_RejectsOnceWithCooldown_UnchangedAndNoOnDash()
        {
            DashScript dash = _rig.CreateDash(dashTime: 0.1f, cooldownTime: 5f);
            _rig.ForceDashCooldown(dash, remainingSeconds: 2f);
            float before = _rig.GetDashRemainingCooldown(dash);

            bool result = _rig.Holder.TryUseDash(dash);

            Assert.IsFalse(result, "a dash on cooldown must not activate");
            Assert.AreEqual(1, _rig.AbilityRejectedLog.Count, "exactly one rejection expected");
            Assert.AreEqual(4, _rig.AbilityRejectedLog[0].index, "rejection must be for the dash slot (4)");
            Assert.AreEqual(AbilityUseFailure.Cooldown, _rig.AbilityRejectedLog[0].reason,
                "a dash on cooldown must reject with Cooldown");
            Assert.AreEqual(0, CountUsed(4), "AbilityUsed(4) must not fire on a cooldown reject");
            Assert.AreEqual(0, _rig.OnDashCount, "no OnDash on a cooldown reject");

            float after = _rig.GetDashRemainingCooldown(dash);
            Assert.AreEqual(before, after, 0.02f, "the remaining cooldown must be left unchanged (R6.6)");

            yield return null;
        }

        // R6.2/R6.3/R6.4: a dash requested while an action is casting and the Dash CancelRule is NOT open
        // yet is BUFFERED (no immediate dash, no rejection), the active action keeps its phase, and then
        // the buffered dash fires on the first frame the Dash rule opens — OnDash exactly once.
        [UnityTest]
        public IEnumerator OutsideWindowWhileCasting_BuffersThenFiresWhenRuleOpens()
        {
            // Cast whose Dash rule opens only in the second half (progress >= 0.5). Duration 1 s so one
            // second of normalized progress maps to one second of _elapsed. Authored AsuraBurst so the
            // authorized cancel actually ends the cast (FinishForDodge → CancelCast) once the rule opens,
            // exactly as the shipped Asura burst does — otherwise IsCasting would still block the dash.
            BreakerGauntletAbility ability = _rig.CreateCastingAbility(dashStart: 0.5f, dashEnd: 1f, asuraBurst: true);
            BreakerGauntletCombat combat = _rig.InjectCasting(ability, elapsed: 0.1f, duration: 1f);

            DashScript dash = _rig.CreateDash(iframeWindow: 0.05f, dashTime: 0.1f);

            Assert.IsTrue(_rig.Holder.IsCasting, "the injected cast must report IsCasting (precondition)");

            // Request the dash while the rule is closed (progress 0.1 < 0.5): buffered, not fired, not rejected.
            bool immediate = _rig.Holder.TryUseDash(dash);
            Assert.IsFalse(immediate, "a dash requested before the Dash rule opens is buffered, not fired");
            Assert.AreEqual(0, _rig.OnDashCount, "no dash should fire while the rule is closed");
            Assert.AreEqual(0, _rig.AbilityRejectedLog.Count,
                "a purely temporal miss must not emit a rejection (contract-audit O-6)");
            Assert.IsTrue(_rig.Holder.IsCasting, "the active action keeps its phase while the dash is buffered");

            // Advance a frame with the rule still closed — the buffered dash must stay pending.
            yield return null;
            Assert.AreEqual(0, _rig.OnDashCount, "buffered dash must not fire while the rule stays closed");

            // Open the Dash rule (progress 0.6 >= 0.5): the next AbilityHolder.Update tick services the
            // buffer and fires the dash exactly once.
            _rig.SetCastElapsed(combat, 0.6f);
            yield return null; // Update -> TickDashBuffer consumes the buffered intent
            yield return null; // settle

            Assert.AreEqual(1, _rig.OnDashCount, "the buffered dash fires once when the Dash rule opens (R6.4)");
            Assert.AreEqual(1, CountUsed(4), "AbilityUsed(4) fires once for the buffered dash");

            yield return WaitFor(0.2f);
        }

        // R6.5 / contract-audit O-6: a dash requested while casting whose Dash window never opens before
        // the buffer expires is dropped WITHOUT ever firing OnDash and WITHOUT spamming AbilityRejected.
        // Here the active action has NO Dash rule, so the window can never open; the holder buffers the
        // intent (its only impediment reads as temporal) and the buffer lapses silently after
        // BufferDuration. The active action keeps its phase throughout.
        [UnityTest]
        public IEnumerator DashWindowNeverOpens_BufferDropsSilently_NoOnDashNoReject()
        {
            BreakerGauntletAbility ability = _rig.CreateCastingAbility(dashStart: 1f, dashEnd: 0f); // no dash rule
            _rig.InjectCasting(ability, elapsed: 0.3f, duration: 2f);

            DashScript dash = _rig.CreateDash(dashTime: 0.1f);

            bool result = _rig.Holder.TryUseDash(dash);

            Assert.IsFalse(result, "a dash into a cast with no open Dash window must not fire immediately");
            Assert.AreEqual(0, _rig.OnDashCount, "no OnDash while the window is closed");
            Assert.IsTrue(_rig.Holder.IsCasting, "the active action keeps running with its phase unchanged (R6.5)");

            // Advance well past the single-slot buffer's BufferDuration (default 120 ms) with the rule
            // still closed: the intent must be discarded without executing and without any rejection spam.
            yield return WaitFor(0.3f);

            Assert.AreEqual(0, _rig.OnDashCount, "a dash whose window never opens must never fire (R6.5)");
            Assert.AreEqual(0, _rig.AbilityRejectedLog.Count,
                "a buffered-then-expired dash must not emit AbilityRejected on every re-evaluation (O-6)");
            Assert.IsTrue(_rig.Holder.IsCasting, "the active action still runs with its phase unchanged");
        }

        // R6.7/R6.8: the Janela_de_i-frames is independent of the Duracao_de_Deslocamento. With the i-frame
        // window shorter than dashTime, the actor is immune at dash start, loses immunity after the window
        // elapses WHILE the displacement is still running, and has NO residual immunity after the dash ends.
        [UnityTest]
        public IEnumerator Iframes_IndependentOfDisplacement_AndNoResidue()
        {
            // i-frames 0.08 s, displacement 0.4 s: immunity must end well before the dash movement does.
            DashScript dash = _rig.CreateDash(iframeWindow: 0.08f, dashTime: 0.4f, dashVelocity: 4f);

            Assert.IsFalse(_rig.Player.IsDamageImmune, "no immunity before the dash starts");

            bool result = _rig.Holder.TryUseDash(dash);
            Assert.IsTrue(result, "the dash should start");

            // Immediately after start (same frame) the i-frame window is armed.
            Assert.IsTrue(_rig.Player.IsDamageImmune, "the dash must grant immunity at its start (R6.9)");
            Assert.IsTrue(dash.IsDashing(_rig.Player.gameObject), "the displacement should still be running");

            // Advance past the i-frame window but not past the displacement: immunity must have ended while
            // the dash is still moving (independence of the two durations, R6.7).
            yield return WaitFor(0.2f);
            Assert.IsTrue(dash.IsDashing(_rig.Player.gameObject),
                "the displacement (0.4 s) must still be running after 0.2 s");
            Assert.IsFalse(_rig.Player.IsDamageImmune,
                "immunity must end when the 0.08 s i-frame window elapses, independent of the 0.4 s dash (R6.7)");

            // Let the whole dash finish: no residual immunity remains (R6.8).
            yield return WaitFor(0.4f);
            Assert.IsFalse(dash.IsDashing(_rig.Player.gameObject), "the dash should have ended");
            Assert.IsFalse(_rig.Player.IsDamageImmune, "no residual immunity after the dash fully ends (R6.8)");
        }

        // R6.8 (end-of-displacement cleanup with a long window): even when the i-frame window is authored
        // LONGER than the displacement, the dash's single cleanup exit path releases immunity when the
        // displacement ends, leaving NO residual immunity. This pins the "no residue" guarantee from the
        // opposite side of the first i-frames test (there the window was shorter than the displacement).
        [UnityTest]
        public IEnumerator LongIframeWindow_ReleasesImmunityWhenDisplacementEnds_NoResidue()
        {
            // i-frames (0.5 s) longer than the displacement (0.1 s): immunity would outlive the move unless
            // the end-of-displacement cleanup releases it.
            DashScript dash = _rig.CreateDash(iframeWindow: 0.5f, dashTime: 0.1f);

            Assert.IsTrue(_rig.Holder.TryUseDash(dash), "the dash should start");
            Assert.IsTrue(_rig.Player.IsDamageImmune, "immunity armed at dash start (R6.9)");

            // Let the dash fully end (0.1 s displacement) but stay within the 0.5 s i-frame window.
            yield return WaitFor(0.25f);

            Assert.IsFalse(dash.IsDashing(_rig.Player.gameObject), "the displacement should have ended");
            Assert.IsFalse(_rig.Player.IsDamageImmune,
                "immunity must be released at end of displacement, leaving no residue even with a longer window (R6.8)");
            Assert.AreEqual(0, _rig.ActiveDashOwnerCount(dash),
                "the dash must track no residual owner after it ends (R6.8)");
        }

        // R6.8 (death/room-change interruption): when the dash runner is torn down mid-dash (the
        // death/room-change path), the dash leaves no residual dashing state tracked in the shared
        // DashScript — the owner is gone and nothing lingers.
        [UnityTest]
        public IEnumerator PlayerDestroyedMidDash_LeavesNoResidualDashState()
        {
            DashScript dash = _rig.CreateDash(iframeWindow: 0.5f, dashTime: 0.4f);

            Assert.IsTrue(_rig.Holder.TryUseDash(dash), "the dash should start");
            Assert.IsTrue(dash.IsDashing(_rig.Player.gameObject), "the dash should be running");

            // Destroy the player mid-displacement (the death/room-change interruption): the actor and its
            // immunity set are gone, and the shared DashScript must not keep treating a dead owner as dashing.
            _rig.DestroyPlayer();
            yield return null;

            Assert.AreEqual(0, _rig.ActiveDashOwnerCount(dash),
                "a destroyed dash owner must not remain tracked as dashing (R6.8)");
        }

        // R4.7 / contract-audit O-9: replacing the old total input block with the CancelResolver preserves
        // the observable semantics of IsCasting and MovementAllowedWhileCasting. A melee (Breaker) cast
        // reports IsCasting == true and MovementAllowedWhileCasting == false, and a dash-cancel attempt
        // whose window is not open (here buffered, not authorized) leaves both getters unchanged — the new
        // routing never alters the active action's phase just because a dash was requested.
        [UnityTest]
        public IEnumerator CastingSemantics_PreservedAcrossDashCancelAttempt()
        {
            BreakerGauntletAbility ability = _rig.CreateCastingAbility(dashStart: 1f, dashEnd: 0f); // no dash rule
            _rig.InjectCasting(ability, elapsed: 0.3f, duration: 2f);

            Assert.IsTrue(_rig.Holder.IsCasting, "a melee Breaker cast must report IsCasting");
            Assert.IsFalse(_rig.Holder.MovementAllowedWhileCasting,
                "a melee cast pins the player: MovementAllowedWhileCasting must be false (O-9)");

            DashScript dash = _rig.CreateDash(dashTime: 0.1f);
            bool result = _rig.Holder.TryUseDash(dash); // window not open -> no immediate dash, no phase change

            Assert.IsFalse(result, "the dash-cancel does not fire while the Dash window is closed");
            Assert.IsTrue(_rig.Holder.IsCasting, "IsCasting must be unchanged by the dash-cancel attempt (O-9)");
            Assert.IsFalse(_rig.Holder.MovementAllowedWhileCasting,
                "MovementAllowedWhileCasting must stay false across the dash-cancel attempt (O-9)");

            yield return null;
        }

        private int CountUsed(int index)
        {
            int n = 0;
            foreach (int i in _rig.AbilityUsedLog) if (i == index) n++;
            return n;
        }

        // Steps frames until the given unscaled time has elapsed, so the dash coroutine (which uses
        // Time.time) can advance deterministically.
        private static IEnumerator WaitFor(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end) yield return null;
        }
    }
}
