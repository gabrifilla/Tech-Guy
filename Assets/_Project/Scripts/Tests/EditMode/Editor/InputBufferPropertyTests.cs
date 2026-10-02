using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free single-slot <see cref="InputBuffer"/> of
    /// combat-foundation-rework (task 6.3), covering the temporal validity, single-fire, most-recent
    /// retention, aim preservation, and "never wait indefinitely" guarantees (R5.2, R5.5, R5.6,
    /// R5.7, R5.9).
    ///
    /// <para>
    /// Each property is checked against the real production <see cref="InputBuffer"/> (not a
    /// re-stated model) through the project's seeded <see cref="PropertyCheck"/> harness over
    /// <see cref="PropertyCheck.DefaultCases"/> (&gt;= 100) deterministic generated cases, since no
    /// FsCheck/CsCheck package can be resolved on this machine:
    /// </para>
    /// <list type="number">
    /// <item>P6 — a stored intent never fires once <c>now - t0 &gt; BufferDuration</c> (R5.5).</item>
    /// <item>P7 — a valid intent fires at most once; a second consume returns false and the slot is
    /// empty afterwards (R5.6).</item>
    /// <item>P8 — storing two intents keeps the most recent by the deterministic
    /// <c>(IssuedAt, Sequence)</c> ordering, independent of store order (R5.7).</item>
    /// <item>P9 — the fired intent preserves the originally stored Direction and Kind; the buffer
    /// never mutates or re-aims the captured intent (R5.2).</item>
    /// <item>P10 — a command refused by resource/cooldown (never stored by the coordinator) never
    /// fires, so nothing waits indefinitely; a disabled buffer (<c>BufferDuration == 0</c>) retains
    /// nothing and never fires (R5.9).</item>
    /// </list>
    ///
    /// <para>
    /// <see cref="Actor"/> is a <c>MonoBehaviour</c>, so these scene-free property tests pass
    /// <c>null</c> as the intent target (instantiating MonoBehaviours is not valid in an EditMode
    /// property body) and verify Direction/Kind/Sequence round-trip instead, which is exactly what
    /// R5.2's "preserve the aim, never re-aim" guarantee reduces to for the pure slot.
    /// </para>
    /// </summary>
    // Feature: combat-foundation-rework, task 6.3. Requirements: R5.2, R5.5, R5.6, R5.7, R5.9.
    public sealed class InputBufferPropertyTests
    {
        // Draws any of the six command kinds so the properties are exercised across kinds.
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

        // A finite, well-spread emission timestamp (seconds). Includes 0 so the clock origin is covered.
        private static float GenIssuedAt(Random rng)
        {
            return (float)(rng.NextDouble() * 1000.0);
        }

        // A finite cursor direction captured at emission; magnitude is irrelevant to the buffer.
        private static Vector3 GenDirection(Random rng)
        {
            return new Vector3(
                (float)(rng.NextDouble() * 20.0 - 10.0),
                (float)(rng.NextDouble() * 20.0 - 10.0),
                (float)(rng.NextDouble() * 20.0 - 10.0));
        }

        // A strictly positive buffer duration (0 is covered separately in P10), finite and realistic.
        private static float GenPositiveDuration(Random rng)
        {
            return (float)(rng.NextDouble() * 0.5 + 0.001); // [0.001, 0.501): always > 0
        }

        // Feature: combat-foundation-rework, Property 6: a stored intent never fires after it has
        // expired, i.e. once now - t0 > BufferDuration TryConsume returns false even with the window
        // open.
        // Validates: Requirements 5.5
        [Test]
        public void NeverFiresAfterBufferDurationElapsed()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float duration = GenPositiveDuration(rng);
                float t0 = GenIssuedAt(rng);

                var buffer = new InputBuffer(duration);
                var intent = new CommandIntent(GenKind(rng), null, GenDirection(rng), t0, rng.Next());
                buffer.Store(in intent);

                // Choose a strictly-expired instant: now - t0 > duration by a positive margin.
                float margin = (float)(rng.NextDouble() * 10.0 + 1e-3);
                float now = t0 + duration + margin;

                bool fired = buffer.TryConsume(now, windowOpen: true, out CommandIntent _);

                PropertyCheck.That(!fired,
                    $"duration={duration}, t0={t0}, now={now} (elapsed={now - t0} > duration): "
                    + "TryConsume fired an expired intent but must never fire after now - t0 > BufferDuration.");
            });
        }

        // Feature: combat-foundation-rework, Property 7: a valid, non-expired intent with the window
        // open fires on the first TryConsume; an immediate second TryConsume returns false and the
        // slot is empty (HasPending == false), so it fires at most once and empties afterwards.
        // Validates: Requirements 5.6
        [Test]
        public void FiresAtMostOnceAndEmptiesAfterwards()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float duration = GenPositiveDuration(rng);
                float t0 = GenIssuedAt(rng);

                var buffer = new InputBuffer(duration);
                var intent = new CommandIntent(GenKind(rng), null, GenDirection(rng), t0, rng.Next());
                buffer.Store(in intent);

                // A now inside the valid window: 0 <= now - t0 <= duration.
                float now = t0 + (float)(rng.NextDouble()) * duration;

                bool first = buffer.TryConsume(now, windowOpen: true, out CommandIntent _);
                PropertyCheck.That(first,
                    $"duration={duration}, t0={t0}, now={now}: a valid intent with the window open "
                    + "must fire on the first TryConsume.");

                bool second = buffer.TryConsume(now, windowOpen: true, out CommandIntent _);
                PropertyCheck.That(!second,
                    $"duration={duration}, t0={t0}, now={now}: the second TryConsume fired again but "
                    + "a buffered intent must fire at most once.");

                PropertyCheck.That(!buffer.HasPending,
                    $"duration={duration}, t0={t0}, now={now}: HasPending is still true after firing "
                    + "but the slot must be empty once the intent fired.");
            });
        }

        // Feature: combat-foundation-rework, Property 8: storing two intents in either order retains
        // the most recent one under the deterministic (IssuedAt, then Sequence) ordering; the fired
        // intent is the one with the greater key regardless of insertion order.
        // Validates: Requirements 5.7
        [Test]
        public void RetainsMostRecentWithDeterministicSequenceTieBreak()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float duration = GenPositiveDuration(rng);

                // Two intents sharing a timestamp half the time (to force the Sequence tie-break),
                // otherwise distinct timestamps. Sequences are always distinct so there is a single
                // deterministic winner.
                float tA = GenIssuedAt(rng);
                bool sameInstant = rng.Next(0, 2) == 0;
                float tB = sameInstant ? tA : GenIssuedAt(rng);

                long seqA = rng.Next();
                long seqB = seqA + 1 + rng.Next(0, 1000); // seqB != seqA, both well-defined

                var a = new CommandIntent(CommandKind.Basic, null, GenDirection(rng), tA, seqA);
                var b = new CommandIntent(CommandKind.Dash, null, GenDirection(rng), tB, seqB);

                // The deterministic expected winner: later IssuedAt first, then greater Sequence.
                CommandIntent expected;
                if (a.IssuedAt > b.IssuedAt) expected = a;
                else if (a.IssuedAt < b.IssuedAt) expected = b;
                else expected = a.Sequence > b.Sequence ? a : b;

                var buffer = new InputBuffer(duration);
                // Store in a randomized order to prove retention is order-independent.
                if (rng.Next(0, 2) == 0) { buffer.Store(in a); buffer.Store(in b); }
                else { buffer.Store(in b); buffer.Store(in a); }

                // Consume at the later timestamp so neither candidate is expired.
                float now = Math.Max(tA, tB);
                bool fired = buffer.TryConsume(now, windowOpen: true, out CommandIntent got);

                PropertyCheck.That(fired,
                    $"tA={tA}/seqA={seqA}, tB={tB}/seqB={seqB}: the retained intent should still be "
                    + "valid at now and fire.");
                PropertyCheck.That(got.IssuedAt == expected.IssuedAt && got.Sequence == expected.Sequence,
                    $"tA={tA}/seqA={seqA}, tB={tB}/seqB={seqB}: retained (IssuedAt={got.IssuedAt}, "
                    + $"Sequence={got.Sequence}) but the most recent by (IssuedAt, Sequence) is "
                    + $"(IssuedAt={expected.IssuedAt}, Sequence={expected.Sequence}).");
            });
        }

        // Feature: combat-foundation-rework, Property 9: the fired intent preserves the originally
        // stored Direction and Kind (and Sequence); the single slot never mutates or re-aims the
        // captured intent, so a buffered command fires against its original aim rather than an
        // enemy picked by proximity.
        // Validates: Requirements 5.2
        [Test]
        public void PreservesOriginalDirectionAndKind()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float duration = GenPositiveDuration(rng);
                float t0 = GenIssuedAt(rng);

                CommandKind kind = GenKind(rng);
                Vector3 direction = GenDirection(rng);
                long sequence = rng.Next();

                var buffer = new InputBuffer(duration);
                // null Target: Actor is a MonoBehaviour (not instantiable in an EditMode property
                // body); the aim-preservation guarantee is asserted via Direction/Kind round-trip.
                var intent = new CommandIntent(kind, null, direction, t0, sequence);
                buffer.Store(in intent);

                float now = t0 + (float)(rng.NextDouble()) * duration; // inside the valid window
                bool fired = buffer.TryConsume(now, windowOpen: true, out CommandIntent got);

                PropertyCheck.That(fired,
                    $"kind={kind}, t0={t0}, now={now}: a valid stored intent should fire.");
                PropertyCheck.That(got.Kind == kind,
                    $"stored kind={kind} but fired kind={got.Kind}: the buffer must not change the command kind.");
                PropertyCheck.That(got.Direction == direction,
                    $"stored direction={direction} but fired direction={got.Direction}: the buffer must "
                    + "preserve the captured aim and never re-aim.");
                PropertyCheck.That(got.Target == null,
                    $"kind={kind}: the stored null target must round-trip as null (never re-selected by proximity).");
                PropertyCheck.That(got.Sequence == sequence,
                    $"stored sequence={sequence} but fired sequence={got.Sequence}: the buffer must not change the intent.");
            });
        }

        // Feature: combat-foundation-rework, Property 10: a command refused by resource/cooldown is
        // never stored by the coordinator, so an empty buffer never fires for any now and nothing
        // waits indefinitely; likewise a disabled buffer (BufferDuration == 0) treats Store as a
        // no-op and never fires.
        // Validates: Requirements 5.9
        [Test]
        public void RefusedCommandNeverWaitsIndefinitely()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Case A: the coordinator refused the command (resource/cooldown), so nothing was
                // ever stored. An empty, enabled buffer must never fire for any now, with the window
                // open or closed — it does not keep anything waiting.
                var empty = new InputBuffer(GenPositiveDuration(rng));
                PropertyCheck.That(!empty.HasPending,
                    "a freshly built buffer with nothing stored must report HasPending == false.");

                float now = GenIssuedAt(rng);
                bool windowOpen = rng.Next(0, 2) == 0;
                bool firedEmpty = empty.TryConsume(now, windowOpen, out CommandIntent _);
                PropertyCheck.That(!firedEmpty,
                    $"now={now}, windowOpen={windowOpen}: an empty buffer fired, but a command never "
                    + "stored (refused by resource/cooldown) must never fire or wait indefinitely.");

                // Case B: a disabled buffer (BufferDuration == 0) must drop Store as a no-op and
                // never fire, so a would-be-buffered command is not held waiting either.
                var disabled = new InputBuffer(0f);
                float t0 = GenIssuedAt(rng);
                var intent = new CommandIntent(GenKind(rng), null, GenDirection(rng), t0, rng.Next());
                disabled.Store(in intent);

                PropertyCheck.That(!disabled.HasPending,
                    "a disabled buffer (BufferDuration == 0) must treat Store as a no-op and retain nothing.");

                float nowDisabled = t0 + (float)(rng.NextDouble()) * 0.1f; // within any plausible window
                bool firedDisabled = disabled.TryConsume(nowDisabled, windowOpen: true, out CommandIntent _);
                PropertyCheck.That(!firedDisabled,
                    $"t0={t0}, now={nowDisabled}: a disabled buffer fired, but with BufferDuration == 0 "
                    + "nothing is ever retained or fired.");
            });
        }
    }
}
