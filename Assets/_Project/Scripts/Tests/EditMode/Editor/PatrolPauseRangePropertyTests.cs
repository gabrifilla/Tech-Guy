using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure idle-pause timer in <see cref="PatrolPause"/> —
    /// task 1.10 of ranged-kiting-and-attack-telegraph-overhaul (design Property 5).
    ///
    /// The patrol pause is otherwise scene/coroutine driven by <c>EnemyAI.Patrol()</c>; its
    /// invariants were extracted into plain C# so they can be property-checked without a live
    /// Unity scene. The project cannot resolve FsCheck/CsCheck on this machine, so the agreed
    /// seeded harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class PatrolPauseRangePropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 5
        // Patrol pause duration lies within the configured range and exits on demand:
        // For any min >= 0, max >= min, and seed, a begun PatrolPause lasts a duration within
        // [min, max], reports IsPaused until that duration elapses, never pauses when max == 0,
        // and Clear() ends the pause immediately.
        // Validates: Requirements 14.1, 14.2, 14.3, 14.5, 14.6, 16.2
        [Test]
        public void PauseDurationWithinRangeAndExitsOnDemand()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float min = RandomNonNegative(rng);
                float max = min + (float)rng.NextDouble() * 5f; // max >= min (R14.2)

                // --- R14.6: max == 0 never pauses ---------------------------------------------
                {
                    var neverPause = new PatrolPause();
                    neverPause.Begin(minSeconds: 0f, maxSeconds: 0f, rng: new System.Random(unchecked(i * 31 + 1)));
                    PropertyCheck.That(!neverPause.IsPaused,
                        "max==0 must never pause: IsPaused should stay false right after Begin");
                    PropertyCheck.That(!neverPause.Tick(0.016f),
                        "max==0 must never pause: Tick should return false");
                    PropertyCheck.That(!neverPause.IsPaused,
                        "max==0 must never pause: IsPaused should remain false after Tick");
                }

                // --- Begin a real pause and drive it to completion in small steps -------------
                var pause = new PatrolPause();
                pause.Begin(min, max, new System.Random(unchecked(i * 31 + 7)));

                // When max > 0 the sampled duration lies within [min, max] (R14.1/R14.2).
                // We accumulate the elapsed time required for the pause to end and verify it falls
                // in the configured band. A zero-length pause (min==max==0) ends immediately.
                if (max <= 0f)
                {
                    PropertyCheck.That(!pause.IsPaused,
                        $"min={min} max={max}: a zero-length range must not pause");
                }
                else if (!pause.IsPaused)
                {
                    // Only legitimate when the sampled duration was 0, which requires min==0.
                    PropertyCheck.That(min <= 1e-6f,
                        $"min={min} max={max}: Begin reported no pause but min>0, so a positive duration was expected");
                }
                else
                {
                    // R14.3: IsPaused stays true while ticking until the duration elapses.
                    float dt = 0.016f + (float)rng.NextDouble() * 0.05f;
                    float elapsed = 0f;
                    int guard = 0;
                    int maxSteps = Mathf.CeilToInt((max + dt) / dt) + 16;

                    while (pause.Tick(dt))
                    {
                        // While Tick returns true the pause must still be marked active.
                        PropertyCheck.That(pause.IsPaused,
                            $"min={min} max={max}: Tick returned true but IsPaused is false");
                        elapsed += dt;

                        guard++;
                        PropertyCheck.That(guard <= maxSteps,
                            $"min={min} max={max}: pause did not elapse within the expected number of steps (elapsed={elapsed})");
                    }

                    // Once Tick returns false the pause is over and IsPaused is false (R14.3).
                    PropertyCheck.That(!pause.IsPaused,
                        $"min={min} max={max}: pause ended but IsPaused is still true");

                    // R14.1/R14.2: the total elapsed time needed to exhaust the pause must lie
                    // within [min, max]. The final (overshooting) tick that drove _remaining to <=0
                    // is not counted in `elapsed`, and the previous full tick had not yet elapsed,
                    // so the true duration D satisfies: elapsed <= D <= elapsed + dt. We bound the
                    // sampled duration against the configured band allowing one dt of slack.
                    PropertyCheck.That(elapsed <= max + 1e-4f,
                        $"min={min} max={max} dt={dt}: duration lower bound {elapsed} exceeded configured max {max}");
                    PropertyCheck.That(elapsed + dt >= min - 1e-4f,
                        $"min={min} max={max} dt={dt}: duration upper bound {elapsed + dt} below configured min {min}");
                }

                // --- R14.5: Clear() ends an active pause immediately --------------------------
                var clearable = new PatrolPause();
                // Force a non-trivial, definitely-positive duration so there is something to clear.
                clearable.Begin(1f, 3f, new System.Random(unchecked(i * 31 + 13)));
                PropertyCheck.That(clearable.IsPaused,
                    "a [1,3] pause should be active immediately after Begin");

                clearable.Clear();
                PropertyCheck.That(!clearable.IsPaused,
                    "Clear() must end the pause immediately (IsPaused false)");
                PropertyCheck.That(!clearable.Tick(0.016f),
                    "Clear() must end the pause immediately (Tick false afterward)");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Non-negative minimums spanning exactly 0 and well above 0.</summary>
        private static float RandomNonNegative(System.Random rng)
        {
            // Bias one in four cases to exactly 0 so the min==0 / zero-length edge is exercised.
            if (rng.Next(0, 4) == 0) return 0f;
            return (float)rng.NextDouble() * 2.5f;
        }
    }
}
