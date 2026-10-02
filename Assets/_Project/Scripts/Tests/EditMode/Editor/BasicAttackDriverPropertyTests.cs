using System;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free <see cref="BasicAttackDriver"/> of
    /// combat-foundation-rework (task 7.2), covering the no-auto-combat guarantee of a single
    /// Toque_de_Ataque (R1.1, R1.5, R1.6, R1.11, R1.12).
    ///
    /// <para>
    /// The property is checked against the real production <see cref="BasicAttackDriver"/> type
    /// (not a re-stated model), driven by the project's seeded <see cref="PropertyCheck"/> harness
    /// over <see cref="PropertyCheck.DefaultCases"/> (&gt;= 100) deterministic generated cases, since
    /// no FsCheck/CsCheck package can be resolved on this machine:
    /// </para>
    /// <list type="number">
    /// <item>P1 — a fresh driver with <see cref="BasicAttackDriver.SetHold(bool)"/><c>(false)</c> and
    /// a single <see cref="BasicAttackDriver.QueueTap"/> authorizes <em>exactly one</em> attack across
    /// any sequence of increasing <c>now</c> values with any <c>attackInterval</c>: the first
    /// <see cref="BasicAttackDriver.TryTakeAttack"/> consumes the tap (returns <c>true</c>) and every
    /// subsequent call returns <c>false</c> because the control is not held and no new tap exists
    /// (R1.1/R1.5/R1.6). The companion case proves that with no tap and no hold, target existence
    /// alone never authorizes an attack — <c>TryTakeAttack</c> never returns <c>true</c>
    /// (R1.6/R1.11/R1.12).</item>
    /// </list>
    /// </summary>
    // Feature: combat-foundation-rework, task 7.2. Requirements: R1.1, R1.5, R1.6, R1.11, R1.12.
    public sealed class BasicAttackDriverPropertyTests
    {
        // Draws an attackInterval spanning the interesting input space: sometimes <= 0 (no gating),
        // sometimes small, sometimes large relative to the generated time steps, so the property is
        // exercised regardless of whether the interval would gate a hypothetical hold cadence.
        private static float GenAttackInterval(Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;
                case 1: return -(float)(rng.NextDouble() * 2.0);          // negative: no gating
                case 2: return (float)(rng.NextDouble() * 0.05);          // very small interval
                default: return (float)(rng.NextDouble() * 5.0 + 0.01);   // ordinary positive interval
            }
        }

        // Builds a strictly non-decreasing sequence of unscaled 'now' timestamps (the coordinator
        // only ever feeds a clock that does not run backwards), starting from a random base and
        // advancing by random non-negative deltas (sometimes zero, i.e. same frame).
        private static float[] GenIncreasingNow(Random rng)
        {
            int count = rng.Next(2, 12);
            var now = new float[count];
            float t = (float)(rng.NextDouble() * 10.0);
            for (int k = 0; k < count; k++)
            {
                now[k] = t;
                t += (float)(rng.NextDouble() * 1.5); // delta >= 0 (zero allowed: same-frame repeat)
            }

            return now;
        }

        // Feature: combat-foundation-rework, Property 1: a single QueueTap with SetHold(false) and
        // valid preconditions authorizes exactly one attack across the whole sequence — the first
        // TryTakeAttack consumes the tap and every later call returns false; and with no QueueTap and
        // SetHold(false), TryTakeAttack never authorizes an attack (target existence alone does not).
        // Validates: Requirements 1.1, 1.5, 1.6, 1.11, 1.12
        [Test]
        public void SingleTapNotHeldAuthorizesExactlyOneAttackAndNoTapNeverAuthorizes()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float[] now = GenIncreasingNow(rng);
                float attackInterval = GenAttackInterval(rng);

                // --- Case A: single tap, not held => exactly one authorized attack. ---
                var tapped = new BasicAttackDriver();
                tapped.SetHold(false);
                tapped.QueueTap();

                int authorizedCount = 0;
                int firstTrueIndex = -1;
                for (int k = 0; k < now.Length; k++)
                {
                    if (tapped.TryTakeAttack(now[k], attackInterval))
                    {
                        authorizedCount++;
                        if (firstTrueIndex < 0)
                        {
                            firstTrueIndex = k;
                        }
                    }
                }

                PropertyCheck.That(authorizedCount == 1,
                    $"attackInterval={attackInterval}, steps={now.Length}: a single QueueTap with "
                    + $"SetHold(false) authorized {authorizedCount} attacks across the sequence, expected exactly 1.");
                PropertyCheck.That(firstTrueIndex == 0,
                    $"attackInterval={attackInterval}, steps={now.Length}: the single tap was consumed at "
                    + $"TryTakeAttack call #{firstTrueIndex}, expected the first call (#0) to consume it.");

                // --- Case B: no tap, not held => never authorized (target existence alone, R1.6/R1.12). ---
                var idle = new BasicAttackDriver();
                idle.SetHold(false);

                for (int k = 0; k < now.Length; k++)
                {
                    bool authorized = idle.TryTakeAttack(now[k], attackInterval);
                    PropertyCheck.That(!authorized,
                        $"attackInterval={attackInterval}, now={now[k]} (call #{k}): with no QueueTap and "
                        + "SetHold(false), TryTakeAttack returned true, but target existence alone must never authorize an attack.");
                }
            });
        }
    }
}
