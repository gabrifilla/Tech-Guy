using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for the telegraph-rendering integration in
    /// <see cref="EnemyAttackExecution.Execute"/> — task 11.2 of
    /// ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// These drive the REAL <see cref="EnemyAttackExecution.Execute"/> coroutine by manually pumping
    /// its <see cref="IEnumerator"/> (<c>MoveNext</c>), with a stub <see cref="Actor"/> target and a
    /// <c>canAttack</c> delegate, exactly as the gameplay flow does — only the frame loop is driven by
    /// the test instead of Unity's player loop. No scene or NavMesh is required: <see cref="Execute"/>
    /// only spins up <see cref="CombatGroundRing"/>/<see cref="CombatGroundFill"/> GameObjects, which
    /// EditMode supports.
    ///
    /// Three behaviors are validated:
    ///  - Forced coverage (R7.2): a previously-suppressed attack (the straight shot historically routed
    ///    with <c>showWarning:false</c>) now renders a red danger zone — a fill plus an outline — even
    ///    when <c>showWarning:false</c> is passed, because rendering is forced on for every routed attack
    ///    that resolves a valid <see cref="EnemyAttackArea"/>.
    ///  - Same-frame clear (R6.6 / R8.3): the ring and the fill are torn down together in a single frame
    ///    through one <c>ClearVisuals</c> pass, leaving no residual rendered telegraph. The production
    ///    code reaches that identical pass from BOTH the impact branch and
    ///    <see cref="EnemyAttackExecution.Cancel"/>; this suite exercises the cancel branch concretely
    ///    (see the EditMode note below on why impact cannot be driven here).
    ///  - No-area pass-through (R7.4): an attack whose area union is null or empty (the straight shot's
    ///    actual <c>Array.Empty</c> route) renders nothing — forcing only applies when a valid area
    ///    resolves.
    ///
    /// The red base is <see cref="TelegraphFill.RedBase"/>, shared by every attack type.
    ///
    /// EDITMODE DRIVING NOTES — what is and isn't exercised here, stated plainly:
    ///  - The windup loop advances by <c>Time.deltaTime</c>, which is 0 in the EditMode test runner, so
    ///    the loop cannot be pumped to its natural end: the impact branch (and therefore its same-frame
    ///    clear) is NOT reachable by draining the coroutine in EditMode. That exact teardown is instead
    ///    verified through <see cref="EnemyAttackExecution.Cancel"/>, which routes through the SAME
    ///    private <c>ClearVisuals</c> the impact branch uses — one pass that deactivates every ring and
    ///    fill and clears the tracking lists in a single frame. A PlayMode smoke test (task 12.2) is the
    ///    place that drives a real impact with a non-zero frame delta.
    ///  - <c>ClearVisuals</c> tears its helpers down with the production <c>Object.Destroy</c>, which in
    ///    EditMode logs "Destroy may not be called from edit mode" (the object is still deactivated and
    ///    the lists cleared — the behavior under test). That editor-only diagnostic is tolerated via
    ///    <see cref="LogAssert.ignoreFailingMessages"/> rather than asserted on.
    /// </summary>
    // Validates: Requirements 6.6, 7.2, 7.4, 8.3
    public sealed class TelegraphExecutionIntegrationExampleTests
    {
        private static readonly Color StubColor = new Color(1f, .35f, .2f, 1f); // the historical shot color

        // ClearVisuals tears its helpers down with the production Object.Destroy, which the EditMode
        // runner reports as an error ("Destroy may not be called from edit mode"). The object is still
        // deactivated and the tracking lists cleared — the behavior under test — so each such destroy is
        // an EXPECTED editor-only diagnostic rather than a failure. ExpectEditModeDestroy registers one
        // expected error per helper that ClearVisuals will tear down, so the live clear path is still
        // driven for real while the edit-mode artifact does not fail the test.
        private static readonly Regex EditModeDestroyError =
            new Regex("Destroy may not be called from edit mode");

        private static void ExpectEditModeDestroy(int helperCount)
        {
            for (int i = 0; i < helperCount; i++)
                LogAssert.Expect(LogType.Error, EditModeDestroyError);
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 11.2 example
        // Forced coverage: a straight shot historically passed showWarning:false and rendered nothing.
        // With the overhaul, as soon as a valid EnemyAttackArea resolves, Execute forces the danger-zone
        // telegraph on regardless of showWarning — so pumping one frame produces an active red fill (and
        // its outline). The fill carries the shared red base (TelegraphFill.RedBase).
        // Validates: Requirements 7.2
        [Test]
        public void PreviouslyUnwarnedStraightShot_NowRendersRedFill()
        {
            var target = CreateTarget("ForcedCoverageTarget", health: 1000f);
            try
            {
                AssertNoResidualTelegraph("before Execute");

                var exec = new EnemyAttackExecution();
                var area = new EnemyAttackArea(EnemyAttackShape.Circle, Vector3.zero, Vector3.forward, reach: 5f);

                // showWarning:false is the historical straight-shot routing; the overhaul renders anyway.
                IEnumerator run = exec.Execute(
                    new[] { area }, target, windup: 0.25f, damage: 0f, color: StubColor,
                    canAttack: () => true, showWarning: false, impactHold: 0f);

                bool advanced = run.MoveNext(); // runs the pre-loop setup and the first windup frame

                Assert.IsTrue(advanced, "the windup coroutine must yield at least one frame");
                Assert.IsTrue(exec.IsWindingUp, "Execute must be winding up after the first frame");

                CombatGroundFill[] fills = ActiveFills();
                CombatGroundRing[] outlines = ActiveOutlines();

                Assert.That(fills.Length, Is.GreaterThanOrEqualTo(1),
                    "a previously-unwarned straight shot must now render a fill (forced coverage, R7.2)");
                Assert.That(outlines.Length, Is.GreaterThanOrEqualTo(1),
                    "the forced telegraph must also render its danger-zone outline (R7.2)");

                // The fill mesh exists this frame (the danger zone is actually drawn, not just allocated).
                var meshFilter = fills[0].GetComponent<MeshFilter>();
                Assert.That(meshFilter, Is.Not.Null, "the fill must own a MeshFilter");
                Assert.That(meshFilter.sharedMesh, Is.Not.Null, "the fill must build a mesh");

                // The shared red base is a strongly red color (used identically for every attack type).
                Assert.That(TelegraphFill.RedBase.r, Is.GreaterThan(TelegraphFill.RedBase.g),
                    "the shared telegraph base must read as red");
                Assert.That(TelegraphFill.RedBase.r, Is.GreaterThan(TelegraphFill.RedBase.b),
                    "the shared telegraph base must read as red");

                // Tear down the one ring + one fill (Circle creates a single outline) before leaving.
                ExpectEditModeDestroy(outlines.Length + fills.Length);
                exec.Cancel();
            }
            finally { Cleanup(target); }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 11.2 example
        // Same-frame clear: an in-flight windup that is cancelled tears the ring and the fill down
        // together in a single frame — the exact ClearVisuals pass the impact branch also runs (see the
        // class remarks on why impact itself can't be driven in EditMode). After one Cancel there is no
        // residual rendered telegraph, and pumping the now-cancelled coroutine never resurrects one.
        // Validates: Requirements 6.6, 8.3
        [Test]
        public void SameFrameClear_RingAndFillTornDownTogetherOnCancel()
        {
            var target = CreateTarget("CancelClearTarget", health: 1000f);
            try
            {
                var exec = new EnemyAttackExecution();
                var area = new EnemyAttackArea(EnemyAttackShape.Cone, Vector3.zero, Vector3.forward, reach: 4f, angle: 90f);

                IEnumerator run = exec.Execute(
                    new[] { area }, target, windup: 1f, damage: 0f, color: StubColor,
                    canAttack: () => true, showWarning: false, impactHold: 0f);

                run.MoveNext(); // mid-windup: both the ring and the fill are rendered this frame

                Assert.That(ActiveFills().Length, Is.GreaterThanOrEqualTo(1), "the fill must render mid-windup");
                Assert.That(ActiveOutlines().Length, Is.GreaterThanOrEqualTo(1), "the ring must render mid-windup");

                // A single Cancel runs ClearVisuals once: ring + fill are gone in the same frame (R6.6/R8.3).
                ExpectEditModeDestroy(ActiveOutlines().Length + ActiveFills().Length);
                exec.Cancel();

                Assert.IsFalse(exec.IsWindingUp, "cancel must stop the windup");
                Assert.That(ActiveFills(), Is.Empty, "cancel must clear the fill in the same frame (R6.6)");
                Assert.That(ActiveOutlines(), Is.Empty, "cancel must clear the ring in the same frame (R8.3)");

                // The now-cancelled coroutine (its version was bumped by Cancel) must not continue nor
                // resurrect any telegraph when pumped again.
                bool stillRunning = run.MoveNext();
                Assert.IsFalse(stillRunning, "a cancelled windup must not continue");
                Assert.That(ActiveFills(), Is.Empty, "a cancelled windup must leave no fill");
                Assert.That(ActiveOutlines(), Is.Empty, "a cancelled windup must leave no ring");
            }
            finally { Cleanup(target); }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 11.2 example
        // No-area pass-through: an attack whose area union is empty or null (the straight shot's actual
        // Array.Empty route) renders no telegraph at all. This is the complement to the forced-coverage
        // case: forcing is applied only when a valid area resolves, so with no area nothing is created
        // and the attack proceeds unrendered (R7.4). Cancelling the no-area windup likewise leaves
        // nothing behind, confirming the pass-through never leaks a helper.
        // Validates: Requirements 7.4
        [Test]
        public void NoAreaAttack_ProceedsWithoutRenderingAnyTelegraph()
        {
            var target = CreateTarget("NoAreaTarget", health: 1000f);
            try
            {
                AssertNoResidualTelegraph("before Execute");

                // Empty union: the straight-shot / spread-shot route passes Array.Empty here.
                var execEmpty = new EnemyAttackExecution();
                IEnumerator runEmpty = execEmpty.Execute(
                    System.Array.Empty<EnemyAttackArea>(), target, windup: 0.25f, damage: 0f, color: StubColor,
                    canAttack: () => true, showWarning: false, impactHold: 0f);

                bool advancedEmpty = runEmpty.MoveNext();

                Assert.IsTrue(advancedEmpty, "a no-area windup still runs its windup loop");
                Assert.IsTrue(execEmpty.IsWindingUp, "a no-area attack still enters the windup");
                Assert.That(ActiveFills(), Is.Empty, "an empty area union must render no fill (R7.4)");
                Assert.That(ActiveOutlines(), Is.Empty, "an empty area union must render no outline (R7.4)");

                execEmpty.Cancel();
                AssertNoResidualTelegraph("after cancelling the empty-area attack");

                // A null areas array is treated identically: no rendering, no leak.
                var execNull = new EnemyAttackExecution();
                IEnumerator runNull = execNull.Execute(
                    null, target, windup: 0.25f, damage: 0f, color: StubColor,
                    canAttack: () => true, showWarning: false, impactHold: 0f);

                runNull.MoveNext();

                Assert.That(ActiveFills(), Is.Empty, "a null area union must render no fill (R7.4)");
                Assert.That(ActiveOutlines(), Is.Empty, "a null area union must render no outline (R7.4)");

                execNull.Cancel();
                AssertNoResidualTelegraph("after cancelling the null-area attack");
            }
            finally { Cleanup(target); }
        }

        // --- Helpers ---------------------------------------------------------------------------

        // Builds a lightweight, valid Actor target on an INACTIVE host so Actor.Awake never runs (it
        // would otherwise attach EnemyCombatFeedback/CoinDrop). An inactive host still yields a valid
        // (non-null) Actor reference and a non-dead IsDead, which is all Execute inspects. Mirrors the
        // inactive-host pattern used by the other EditMode tests that need an Actor without a scene.
        private static Actor CreateTarget(string name, float health)
        {
            var host = new GameObject(name);
            host.SetActive(false);
            Actor actor = host.AddComponent<Actor>();
            actor.health = health;
            return actor;
        }

        private static CombatGroundFill[] ActiveFills() => Object.FindObjectsOfType<CombatGroundFill>();
        private static CombatGroundRing[] ActiveOutlines() => Object.FindObjectsOfType<CombatGroundRing>();

        private static void AssertNoResidualTelegraph(string when)
        {
            // FindObjectsOfType excludes inactive objects, so a helper deactivated during ClearVisuals is
            // already excluded — exactly the "no residual rendered telegraph" semantics we want.
            Assert.That(ActiveFills(), Is.Empty, $"no fill should exist {when}");
            Assert.That(ActiveOutlines(), Is.Empty, $"no outline should exist {when}");
        }

        private static void Cleanup(Actor target)
        {
            // Destroy any telegraph helpers that may still linger (active or inactive) between cases so
            // the per-test counts stay isolated, then destroy the target host.
            foreach (var fill in Resources.FindObjectsOfTypeAll<CombatGroundFill>())
                if (fill) Object.DestroyImmediate(fill.gameObject);
            foreach (var ring in Resources.FindObjectsOfTypeAll<CombatGroundRing>())
                if (ring) Object.DestroyImmediate(ring.gameObject);
            if (target) Object.DestroyImmediate(target.gameObject);
        }
    }
}
