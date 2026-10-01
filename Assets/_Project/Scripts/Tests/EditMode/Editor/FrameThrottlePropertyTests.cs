using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free <see cref="FrameThrottle"/> — task 8.5 of
    /// project-cleanup-optimization (Requisito 4.1).
    ///
    /// <see cref="FrameThrottle"/> is 100% framework-agnostic (no <see cref="UnityEngine.MonoBehaviour"/>,
    /// no <c>Time</c>, no scene): the caller advances it one frame at a time with <c>Tick()</c>, which
    /// fires on the very first tick and then at most once every <c>Interval</c> ticks. It is the reusable
    /// core behind rate-limited per-frame work (e.g. the <c>OutlineScript</c> raycast), so its firing
    /// contract can be property-checked without a live Unity scene — exactly the pure logic floor the
    /// spec says is the only CLI-automatable target.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (128 by
    /// default) and reports the exact failing tick sequence as a counterexample — matching the
    /// convention of the sibling pure-logic property tests (e.g. SimpleObjectPoolPropertyTests,
    /// ProjectileMotionPropertyTests).
    ///
    /// Validates: Requirements 4.1
    /// </summary>
    public sealed class FrameThrottlePropertyTests
    {
        // Feature: project-cleanup-optimization, Property 6: FrameThrottle dispara no máximo uma vez por
        // intervalo e nunca perde o primeiro tick.
        //
        // For any configured interval (including sub-1 values that must clamp to "fire every tick") and
        // any number of ticks, we drive the throttle one Tick() at a time and assert, after every tick:
        //   (a) the configured interval is always clamped to >= 1, so a misconfigured value can never
        //       stall the throttle forever (R4.1);
        //   (b) the FIRST ever tick always fires, regardless of interval — the first tick is never
        //       missed;
        //   (c) between two consecutive fires at least Interval ticks elapse — the throttle never
        //       fires twice within an interval (no fire closer than Interval ticks to the previous);
        //   (d) a non-first tick fires exactly when Interval ticks have elapsed since the last fire
        //       (so it is also never late / never skips a due fire);
        //   (e) Reset returns the throttle to its initial state so the next tick fires again as a
        //       fresh first tick.
        // Validates: Requirements 4.1
        [Test]
        public void TickFiresFirstThenAtMostOncePerIntervalAndNeverMissesFirst()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int requestedInterval = RandomInterval(rng);
                var throttle = new FrameThrottle(requestedInterval);

                int expectedInterval = requestedInterval < 1 ? 1 : requestedInterval;
                string ctx = $"[requested={requestedInterval}]";

                // (a) The interval is always clamped to >= 1 (never a permanent stall).
                PropertyCheck.That(throttle.Interval == expectedInterval,
                    $"{ctx}: Interval={throttle.Interval}, expected clamped {expectedInterval}");
                PropertyCheck.That(throttle.Interval >= 1,
                    $"{ctx}: Interval={throttle.Interval} was not clamped to at least 1");

                // Before any tick nothing has fired yet.
                PropertyCheck.That(throttle.TicksSinceLastFire == 0,
                    $"{ctx}: fresh throttle reported TicksSinceLastFire={throttle.TicksSinceLastFire}");

                int ticks = rng.Next(1, 400);

                // Index of the previous fire (in 1-based tick numbering); 0 means "no fire yet".
                int lastFireTick = 0;
                int fireCount = 0;

                for (int t = 1; t <= ticks; t++)
                {
                    bool fired = throttle.Tick();

                    if (fired)
                    {
                        fireCount++;

                        if (lastFireTick == 0)
                        {
                            // (b) The very first tick must be the one that fires: the first tick is
                            // never missed, whatever the interval.
                            PropertyCheck.That(t == 1,
                                $"{ctx}: first fire happened on tick {t}, expected the very first tick (1)");
                        }
                        else
                        {
                            int gap = t - lastFireTick;

                            // (c) No two fires happen closer than Interval ticks apart.
                            PropertyCheck.That(gap >= expectedInterval,
                                $"{ctx}: fired on tick {t}, only {gap} ticks after previous fire (interval {expectedInterval})");

                            // (d) ... and it fires exactly when the interval elapses (never late /
                            // never skips a due fire).
                            PropertyCheck.That(gap == expectedInterval,
                                $"{ctx}: fired on tick {t}, {gap} ticks after previous fire, expected exactly {expectedInterval}");
                        }

                        // Immediately after a fire the since-counter resets to 0.
                        PropertyCheck.That(throttle.TicksSinceLastFire == 0,
                            $"{ctx}: tick {t} fired but TicksSinceLastFire={throttle.TicksSinceLastFire}, expected 0");

                        lastFireTick = t;
                    }
                    else
                    {
                        // A non-firing tick is only legal after the first fire has already happened
                        // (the first tick can never be a non-fire).
                        PropertyCheck.That(lastFireTick != 0,
                            $"{ctx}: tick {t} did not fire, but no fire has happened yet (first tick missed)");

                        int sinceLast = t - lastFireTick;

                        // A non-fire means the interval has not yet elapsed since the last fire.
                        PropertyCheck.That(sinceLast < expectedInterval,
                            $"{ctx}: tick {t} did not fire although {sinceLast} ticks elapsed since last fire (interval {expectedInterval})");

                        // The reported since-counter mirrors the real gap on non-firing ticks.
                        PropertyCheck.That(throttle.TicksSinceLastFire == sinceLast,
                            $"{ctx}: tick {t} non-fire TicksSinceLastFire={throttle.TicksSinceLastFire}, expected {sinceLast}");
                    }
                }

                // At least the first tick fired (we always drive >= 1 tick).
                PropertyCheck.That(fireCount >= 1,
                    $"{ctx}: {ticks} ticks produced no fire at all (first tick must fire)");

                // (e) Reset makes the next tick fire again as a fresh first tick.
                throttle.Reset();
                PropertyCheck.That(throttle.TicksSinceLastFire == 0,
                    $"{ctx}: after Reset TicksSinceLastFire={throttle.TicksSinceLastFire}, expected 0");
                PropertyCheck.That(throttle.Tick(),
                    $"{ctx}: first Tick after Reset did not fire");
            });
        }

        // Feature: project-cleanup-optimization, Property 6 (focused cadence facet): for a fixed
        // interval, the set of firing tick indices over a long run is exactly {1, 1+interval,
        // 1+2*interval, ...} — a direct, isolated demonstration that the first tick always fires and
        // fires then land on an exact, non-drifting cadence of one-per-interval (never twice within an
        // interval, never a missed due fire).
        // Validates: Requirements 4.1
        [Test]
        public void FireCadenceIsExactlyOnePerIntervalStartingAtFirstTick()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int requestedInterval = RandomInterval(rng);
                int expectedInterval = requestedInterval < 1 ? 1 : requestedInterval;
                var throttle = new FrameThrottle(requestedInterval);

                string ctx = $"[requested={requestedInterval}, interval={expectedInterval}]";

                int ticks = rng.Next(expectedInterval + 1, expectedInterval * 5 + 20);

                for (int t = 1; t <= ticks; t++)
                {
                    bool fired = throttle.Tick();

                    // The expected cadence: fire iff (t - 1) is a whole multiple of the interval, i.e.
                    // on ticks 1, 1+interval, 1+2*interval, ... The first tick (t==1) is always a fire.
                    bool shouldFire = (t - 1) % expectedInterval == 0;

                    PropertyCheck.That(fired == shouldFire,
                        $"{ctx}: tick {t} fired={fired}, expected {shouldFire} (cadence 1, 1+interval, ...)");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Intervals spanning the clamp boundary (negative and zero -> clamped to 1), the "fire every
        /// tick" case (1), small intervals, and larger ones.
        /// </summary>
        private static int RandomInterval(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return -rng.Next(0, 50);        // negative .. 0 (both clamp to 1)
                case 1: return 0;                       // exactly zero (clamps to 1)
                case 2: return 1;                       // "fire every tick"
                case 3: return rng.Next(2, 6);          // small interval
                default: return rng.Next(2, 60);        // typical .. large interval
            }
        }
    }
}
