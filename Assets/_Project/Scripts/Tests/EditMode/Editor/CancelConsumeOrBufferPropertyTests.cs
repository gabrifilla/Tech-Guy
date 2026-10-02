using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 11 of combat-foundation-rework (task 18.3): a valid cancel
    /// command stored in the single-slot <see cref="InputBuffer"/> is <em>consumed within its window or
    /// correctly buffered, never silently dropped except by expiry</em>.
    ///
    /// <para>
    /// This is the "non-duplication / no-silent-loss" half of task 18.3, and it complements the
    /// InputBuffer P6/P7 tests (task 6.3): where P6/P7 pin "never fires after expiry" and "fires at most
    /// once", P11 pins the stronger timeline invariant — across a run where the applicable cancel window
    /// (the <c>windowOpen</c> flag the coordinator derives from the active action's
    /// <see cref="CancelRuleSet"/> / <see cref="CancelResolver"/>) opens at some point, a non-expired
    /// intent must <strong>never leave the buffer unaccounted for</strong>. On every evaluation exactly
    /// one of three things is true for a stored, non-expired intent:
    /// </para>
    /// <list type="bullet">
    /// <item>it is <b>consumed</b> (<see cref="InputBuffer.TryConsume"/> returns it) — only possible when
    /// the window is open and it has not expired; or</item>
    /// <item>it is <b>still buffered</b> (<see cref="InputBuffer.HasPending"/> stays <c>true</c>) because
    /// the window is still closed; or</item>
    /// <item>it has <b>expired</b> (<c>now - IssuedAt &gt; BufferDuration</c>), the only sanctioned way an
    /// intent leaves without firing (R5.5).</item>
    /// </list>
    /// <para>
    /// Phrased as the invariant actually asserted: a valid command is <b>never silently dropped</b> while
    /// it is both non-expired and the window is closed — it is retained; and it <b>never</b> leaves the
    /// buffer without either firing or being expired. In particular the pure buffer never re-aims, never
    /// drops a still-valid-and-waiting intent, and the <em>only</em> no-fire removal is expiry.
    /// </para>
    ///
    /// <para>
    /// The system under test is the real production <see cref="InputBuffer"/> (not a re-stated model),
    /// driven through the project's seeded <see cref="PropertyCheck"/> harness over
    /// <see cref="PropertyCheck.DefaultCases"/> (&gt;= 100) deterministic cases, since no FsCheck/CsCheck
    /// package resolves on this machine. Each case builds a window-open timeline: an intent is stored at
    /// <c>t0</c> with a positive <see cref="InputBuffer.BufferDuration"/>, then the buffer is ticked at a
    /// strictly increasing sequence of instants, each marked window-open or window-closed, and the
    /// invariant is checked at every step and at the terminal state. <see cref="Actor"/> is a
    /// <c>MonoBehaviour</c> (not instantiable in an EditMode property body), so the stored intent carries
    /// a <c>null</c> target and the directional aim is verified to round-trip on consume.
    /// </para>
    /// </summary>
    // Feature: combat-foundation-rework, task 18.3. Requirements: R8.5, R8.6, R5.1, and the audited
    // event contract from task 11 (the cancel-command buffer never silently drops a valid command).
    public sealed class CancelConsumeOrBufferPropertyTests
    {
        // Draws any of the six command kinds; the dash/skill cancels are the ones routed through the
        // buffer by the CancelResolver, but the invariant holds for every kind the buffer can hold.
        private static CommandKind GenKind(Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return CommandKind.Basic;
                case 1: return CommandKind.Dash;
                case 2: return CommandKind.Skill1;
                case 3: return CommandKind.Skill2;
                case 4: return CommandKind.Skill3;
                default: return CommandKind.Skill4;
            }
        }

        private static Vector3 GenDirection(Random rng)
        {
            return new Vector3(
                (float)(rng.NextDouble() * 20.0 - 10.0),
                (float)(rng.NextDouble() * 20.0 - 10.0),
                (float)(rng.NextDouble() * 20.0 - 10.0));
        }

        // A strictly positive, finite buffer duration (0 disables the buffer and is covered by P10).
        private static float GenPositiveDuration(Random rng)
        {
            return (float)(rng.NextDouble() * 0.4 + 0.02); // [0.02, 0.42)
        }

        // Feature: combat-foundation-rework, Property 11: every valid cancel command stored in the
        // single-slot buffer is consumed within its window or correctly buffered, and is NEVER silently
        // dropped except by expiry. Driven over a window-open timeline, at every tick a stored,
        // non-expired intent is either consumed (window open) or still pending (window closed); the only
        // no-fire removal is expiry (now - IssuedAt > BufferDuration).
        // Validates: Requirements 5.1
        [Test]
        public void ValidCancelCommand_ConsumedOrBuffered_NeverSilentlyDroppedExceptByExpiry()
        {
            PropertyCheck.ForAll((rng, caseIndex) =>
            {
                float duration = GenPositiveDuration(rng);
                float t0 = (float)(rng.NextDouble() * 100.0);

                CommandKind kind = GenKind(rng);
                Vector3 direction = GenDirection(rng);
                long sequence = rng.Next();

                var buffer = new InputBuffer(duration);
                var intent = new CommandIntent(kind, null, direction, t0, sequence);
                buffer.Store(in intent);

                // A positive buffer must retain a freshly stored intent (nothing dropped at store time).
                PropertyCheck.That(buffer.HasPending,
                    $"case #{caseIndex}: duration={duration}, t0={t0}: a valid command stored in a "
                    + "positive buffer must be retained, never dropped at store time.");

                // Build a strictly increasing timeline of evaluation instants starting at t0, each either
                // window-open or window-closed. The window "opens at some point": we pick an open-from
                // instant so some ticks are closed and later ticks are open, exercising both the
                // buffered-while-closed and consumed-when-open paths.
                int ticks = rng.Next(3, 10);
                float openFromOffset = (float)(rng.NextDouble() * duration * 2.0); // may be within or past expiry

                bool fired = false;
                bool firedWindowWasOpen = false;
                bool firedWasNonExpired = false;
                float prevNow = t0;

                for (int step = 0; step < ticks; step++)
                {
                    // Strictly increasing offset from t0 so time only moves forward across the run.
                    float offset = (float)((step + rng.NextDouble()) * (duration / 2.0));
                    float now = t0 + offset;
                    PropertyCheck.That(now >= prevNow,
                        $"case #{caseIndex}: timeline must be non-decreasing (now={now} < prev={prevNow}).");
                    prevNow = now;

                    bool windowOpen = now >= t0 + openFromOffset;
                    bool wasPendingBefore = buffer.HasPending;
                    bool expiredNow = now - t0 > duration;

                    // Snapshot whether a currently-pending intent is non-expired at this instant, so we can
                    // assert the "retained while non-expired and closed" and "fires only when non-expired"
                    // guarantees against the real pre-call state.
                    bool pendingNonExpiredBefore = wasPendingBefore && !expiredNow;

                    bool consumed = buffer.TryConsume(now, windowOpen, out CommandIntent got);

                    if (consumed)
                    {
                        // (1) CONSUMED: this is only legitimate when the window was open AND the intent had
                        // not expired. Capture the conditions so we can assert the "consumed within window,
                        // never from expiry" rule, and verify the aim round-trips unchanged (no re-aim).
                        PropertyCheck.That(!fired,
                            $"case #{caseIndex}: the intent fired twice over the timeline; a buffered "
                            + "command must be consumed at most once.");
                        fired = true;
                        firedWindowWasOpen = windowOpen;
                        firedWasNonExpired = !expiredNow;

                        PropertyCheck.That(got.Kind == kind && got.Direction == direction
                            && got.Target == null && got.Sequence == sequence,
                            $"case #{caseIndex}: the consumed intent must preserve the originally stored "
                            + "aim/kind (never re-aimed): "
                            + $"stored (kind={kind}, dir={direction}, seq={sequence}), "
                            + $"got (kind={got.Kind}, dir={got.Direction}, seq={got.Sequence}, target={(got.Target == null ? "null" : "non-null")}).");

                        PropertyCheck.That(!buffer.HasPending,
                            $"case #{caseIndex}: after a consume the slot must be empty.");
                    }
                    else
                    {
                        // (2) NOT CONSUMED: the intent must be either still buffered or gone-by-expiry.
                        // The forbidden case is a SILENT DROP: a non-expired, window-closed intent that was
                        // pending before the call must STILL be pending after it (never dropped), because
                        // the buffer never discards a valid, waiting command.
                        if (pendingNonExpiredBefore && !windowOpen)
                        {
                            PropertyCheck.That(buffer.HasPending,
                                $"case #{caseIndex}: duration={duration}, t0={t0}, now={now} "
                                + $"(elapsed={now - t0} <= duration), windowClosed: a valid, non-expired "
                                + "command was silently dropped — it must stay buffered until the window "
                                + "opens or it expires.");
                        }

                        // If an intent WAS pending but is gone after the call, the only sanctioned reason is
                        // expiry: an open window with a non-expired intent would have fired (handled above),
                        // so a disappearance here must coincide with expiry.
                        if (wasPendingBefore && !buffer.HasPending)
                        {
                            PropertyCheck.That(expiredNow,
                                $"case #{caseIndex}: duration={duration}, t0={t0}, now={now}: a pending "
                                + "command left the buffer without firing while non-expired — the only "
                                + "no-fire removal allowed is expiry (now - IssuedAt > BufferDuration).");
                        }
                    }
                }

                // Terminal invariant: whatever happened across the timeline, if the intent fired it did so
                // under an open window and while non-expired (consumed within its window); if it never
                // fired, it either is still correctly buffered (never reached an open window while valid)
                // or it expired — it was never silently dropped.
                if (fired)
                {
                    PropertyCheck.That(firedWindowWasOpen && firedWasNonExpired,
                        $"case #{caseIndex}: the command fired but not within an open, non-expired window "
                        + "(it must only be consumed within its window).");
                }
                else
                {
                    // A never-fired intent was either still correctly buffered (never reached an open
                    // window while valid) or already expired — never silently dropped. Prove the only way
                    // it can finally leave without firing is expiry: an Expire well past BufferDuration
                    // clears it, and nothing else did beforehand.
                    buffer.Expire(t0 + duration + 1000f);
                    PropertyCheck.That(!buffer.HasPending,
                        $"case #{caseIndex}: a never-fired intent must be removable only by expiry; it "
                        + "remained after an expiry well past BufferDuration.");
                }
            });
        }
    }
}
