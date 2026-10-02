using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode integration tests for the single advance source of combat-foundation-rework
    /// (task 15.4, R2.4–R2.7, R8.9). They drive the production <see cref="ActionImpactScheduler"/>
    /// directly — a fresh <see cref="ExecutionId"/>, a shared <see cref="ExecutionImpactLedger"/>, an
    /// <see cref="ActionTimeline"/> and a list of authored-shaped <see cref="ImpactEvent"/>s with a
    /// counting emit/window callback — so the Startup→Active→Recovery emission contract is asserted
    /// deterministically, exactly as it runs each Quadro_de_Simulacao in
    /// <see cref="BreakerGauntletCombat"/>'s <c>FixedUpdate</c>.
    ///
    /// <para>
    /// The scheduler is pure and scene-free, so these assertions do not need a live cast. They are
    /// placed in the PlayMode assembly (which references the gameplay assembly where
    /// <see cref="ActionImpactScheduler"/> lives) so the whole 15.4 suite — the scheduler contract,
    /// the Manopla kit emission counts, and the Channel preservation — lives together; each test still
    /// asserts exactly one concern. Progress is advanced in discrete ticks that model consecutive
    /// FixedUpdate frames, so "within one Advance of the configured instant" is verified by the frame
    /// granularity itself.
    /// </para>
    ///
    /// <para>
    /// <see cref="ImpactEvent"/> is a serialized struct with private fields (Unity authoring form), so
    /// these tests build each event by boxing one instance and writing its private fields through
    /// reflection — the same reflection style the other PlayMode rigs use — then unboxing. This keeps
    /// the test input identical in shape to the authored profiles the executor consumes.
    /// </para>
    /// </summary>
    public sealed class ActionImpactSchedulerPlayModeTests
    {
        // A deterministic timeline shared by the scheduler-contract tests: Startup [0, 0.3),
        // Active [0.3, 0.7), Recovery [0.7, 1].
        private const float StartupEnd = 0.3f;
        private const float ActiveEnd = 0.7f;

        private List<int> _emitted;
        private List<int> _windowOpened;
        private List<int> _windowClosed;

        [SetUp]
        public void SetUp()
        {
            _emitted = new List<int>();
            _windowOpened = new List<int>();
            _windowClosed = new List<int>();
        }

        // R2.4: during Startup the scheduler emits nothing and opens no window, no matter how many
        // ticks advance below startupEnd. The single advance source stays silent until Active begins.
        [UnityTest]
        public IEnumerator Startup_EmitsNothing_BelowStartupEnd()
        {
            // One discrete impact authored in Active (at 0.5), well past Startup.
            ActionImpactScheduler scheduler = BuildScheduler(new[] { Discrete(0, 0.5f) });

            // Sweep the whole Startup range in several ticks; none should cross the impact's instant.
            for (float p = 0f; p < StartupEnd; p += 0.05f)
            {
                scheduler.Advance(p);
            }

            Assert.AreEqual(0, _emitted.Count, "no impact may emit during Startup (R2.4)");
            Assert.AreEqual(0, _windowOpened.Count, "no window may open during Startup (R2.4)");
            Assert.AreEqual(ActionPhase.Startup, scheduler.Phase, "the scheduler must still be in Startup");
            yield return null;
        }

        // R2.5: crossing startupEnd into Active emits an authored discrete ImpactEvent exactly once,
        // on the first Advance tick that crosses its At — i.e. within one Advance of the configured
        // instant — and never re-emits on later ticks.
        [UnityTest]
        public IEnumerator CrossingStartupEnd_EmitsActiveImpactOnce_WithinOneAdvance()
        {
            // Impact authored just inside Active at 0.35 (startupEnd is 0.3).
            ActionImpactScheduler scheduler = BuildScheduler(new[] { Discrete(0, 0.35f) });

            // Last tick before the instant: still nothing.
            scheduler.Advance(0.30f);
            Assert.AreEqual(0, _emitted.Count, "no emit before the configured instant is crossed");

            // The tick that crosses 0.35 must emit exactly once — within this single Advance (R2.5).
            scheduler.Advance(0.40f);
            Assert.AreEqual(1, _emitted.Count, "the Active impact emits once when its instant is crossed (R2.5)");
            Assert.AreEqual(0, _emitted[0], "the emitted impact is the authored index 0");

            // Further Active ticks must not re-emit the already-emitted index.
            scheduler.Advance(0.50f);
            scheduler.Advance(0.60f);
            Assert.AreEqual(1, _emitted.Count, "a crossed impact never re-emits on later ticks (R2.5)");
            yield return null;
        }

        // R2.6: an ImpactEvent that opens a window opens it when its At is crossed and closes it when
        // its WindowEnd is reached, both within Active. The open and close each happen exactly once.
        [UnityTest]
        public IEnumerator Window_OpensAtAt_AndClosesAtWindowEnd()
        {
            // A window spanning [0.4, 0.6] inside Active.
            ActionImpactScheduler scheduler = BuildScheduler(new[] { Window(0, 0.4f, 0.6f) });

            scheduler.Advance(0.35f);
            Assert.AreEqual(0, _windowOpened.Count, "the window must not open before its At");

            scheduler.Advance(0.45f);
            Assert.AreEqual(1, _windowOpened.Count, "the window opens when its At is crossed (R2.6)");
            Assert.IsTrue(scheduler.HasOpenWindow, "the scheduler reports an open window");
            Assert.AreEqual(0, _windowClosed.Count, "the window stays open before WindowEnd");

            scheduler.Advance(0.65f);
            Assert.AreEqual(1, _windowClosed.Count, "the window closes when WindowEnd is reached (R2.6)");
            Assert.IsFalse(scheduler.HasOpenWindow, "no window remains open after it closes");
            yield return null;
        }

        // R2.6: crossing activeEnd into Recovery closes every still-open window and emits NOTHING
        // further, even for ImpactEvents authored (erroneously) past activeEnd. Entering Recovery is
        // the hard stop for this execution's emissions.
        [UnityTest]
        public IEnumerator CrossingActiveEnd_ClosesWindows_AndNoFurtherEmit()
        {
            // A window still open when Recovery begins, plus a discrete impact authored PAST activeEnd
            // (0.8, inside Recovery) that must never fire.
            ActionImpactScheduler scheduler = BuildScheduler(new[]
            {
                Window(0, 0.4f, 0.9f),   // window would close at 0.9, but Recovery starts first at 0.7
                Discrete(1, 0.8f),       // authored in Recovery: must never emit
            });

            scheduler.Advance(0.45f); // open the window in Active
            Assert.AreEqual(1, _windowOpened.Count, "window opened in Active (precondition)");
            Assert.IsTrue(scheduler.HasOpenWindow);

            // Cross activeEnd (0.7) into Recovery (0.75): the open window closes, nothing new emits.
            scheduler.Advance(0.75f);
            Assert.AreEqual(ActionPhase.Recovery, scheduler.Phase, "the scheduler entered Recovery");
            Assert.IsTrue(scheduler.RecoveryEntered, "Recovery entry is latched");
            Assert.AreEqual(1, _windowClosed.Count, "the open window closes on entry into Recovery (R2.6)");
            Assert.IsFalse(scheduler.HasOpenWindow, "no window outlives Active (R2.6)");
            Assert.AreEqual(0, _emitted.Count, "the Recovery-authored impact must never emit (R2.6)");

            // Even advancing to/over the Recovery-authored instant leaves it silent.
            scheduler.Advance(0.85f);
            scheduler.Advance(1.0f);
            Assert.AreEqual(0, _emitted.Count, "no further ImpactEvent emits once Recovery is entered (R2.6)");
            yield return null;
        }

        // R8.9: the logical Advance path and an Animation-Event Signal for the SAME index collapse to
        // exactly one emit through the shared ledger — whichever fires first wins and the other is a
        // no-op. Asserted from both orderings; and repeated Advance never re-emits an emitted index.
        [UnityTest]
        public IEnumerator Dedup_LogicalAndSignal_CollapseToOneEmit()
        {
            // --- Signal first, then the clock crosses the same instant ---
            ActionImpactScheduler signalFirst = BuildScheduler(new[] { Discrete(0, 0.5f) });

            bool signalEmitted = signalFirst.Signal(0);
            Assert.IsTrue(signalEmitted, "the first signal of an index emits");
            Assert.AreEqual(1, _emitted.Count, "signalling emits exactly once");

            signalFirst.Advance(0.55f); // the clock crosses 0.5 AFTER the signal already emitted
            Assert.AreEqual(1, _emitted.Count,
                "the clock crossing an already-signalled index is a no-op (R8.9)");

            // Repeated Advance over the same instant never re-emits.
            signalFirst.Advance(0.60f);
            signalFirst.Advance(0.65f);
            Assert.AreEqual(1, _emitted.Count, "re-advancing never re-emits an emitted index (R8.9)");

            // --- Clock first, then an Animation Event signals the same index ---
            _emitted.Clear();
            ActionImpactScheduler clockFirst = BuildScheduler(new[] { Discrete(0, 0.5f) });

            clockFirst.Advance(0.55f); // the clock emits first
            Assert.AreEqual(1, _emitted.Count, "the clock emits the impact once");

            bool lateSignal = clockFirst.Signal(0);
            Assert.IsFalse(lateSignal, "a signal after the clock already emitted the index is a no-op (R8.9)");
            Assert.AreEqual(1, _emitted.Count, "the index is still counted exactly once (R8.9)");
            yield return null;
        }

        // --- helpers ---------------------------------------------------------

        private ActionImpactScheduler BuildScheduler(IReadOnlyList<ImpactEvent> events)
        {
            return new ActionImpactScheduler(
                ExecutionId.Next(),
                new ExecutionImpactLedger(),
                new ActionTimeline(StartupEnd, ActiveEnd),
                events,
                impact => _emitted.Add(impact.Index),
                impact => _windowOpened.Add(impact.Index),
                impact => _windowClosed.Add(impact.Index));
        }

        private static ImpactEvent Discrete(int index, float at)
        {
            return MakeImpact(index, at, false, 0f);
        }

        private static ImpactEvent Window(int index, float at, float windowEnd)
        {
            return MakeImpact(index, at, true, windowEnd);
        }

        // ImpactEvent is a serialized struct with private fields; box one instance, write the fields
        // through reflection so successive writes land on the same box, then unbox.
        private static ImpactEvent MakeImpact(int index, float at, bool opensWindow, float windowEnd)
        {
            object boxed = new ImpactEvent();
            SetField(boxed, "index", index);
            SetField(boxed, "at", at);
            SetField(boxed, "opensWindow", opensWindow);
            SetField(boxed, "windowEnd", windowEnd);
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
    }
}
