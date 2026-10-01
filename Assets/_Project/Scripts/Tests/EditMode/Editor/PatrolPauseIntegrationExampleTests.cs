// Feature: ranged-kiting-and-attack-telegraph-overhaul
// Validates: Requirements 14.6
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example/edge test for task 8.6 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// <para>
    /// R14.6: when the Patrol_Pause maximum duration is configured to 0.0 seconds, the enemy
    /// patrols with no idle pause between walk points, matching the existing continuous-motion
    /// behavior. In <c>EnemyAI.Patrol()</c> the idle hold is driven purely by
    /// <see cref="PatrolPause"/>: the enemy holds position only while <see cref="PatrolPause.Tick"/>
    /// returns true and <see cref="PatrolPause.IsPaused"/> is set. The pause/no-pause decision is
    /// scene-free pure logic, so this requirement is pinned by example directly against
    /// <see cref="PatrolPause"/> without a live Unity scene — mirroring how
    /// <see cref="PatrolPauseRangePropertyTests"/> property-tests the same class.
    /// </para>
    ///
    /// Validates: Requirements 14.6
    /// </summary>
    public sealed class PatrolPauseIntegrationExampleTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, R14.6 example — a max of exactly 0
        // never begins a pause: IsPaused stays false right after Begin and Tick returns false, so
        // Patrol() keeps moving continuously (no idle hold between walk points).
        // Validates: Requirements 14.6
        [Test]
        public void PatrolPauseMaxZero_NeverPauses_ContinuousMotion()
        {
            var pause = new PatrolPause();

            // Configured max duration of 0.0 (R14.6). A non-zero min is still clamped so max >= min
            // cannot force a positive pause; the authored EnemyAI OnValidate also enforces
            // PatrolPauseMax >= PatrolPauseMin, so the realistic case is min == max == 0.
            pause.Begin(minSeconds: 0f, maxSeconds: 0f, rng: new System.Random(12345));

            Assert.IsFalse(pause.IsPaused,
                "max==0 must never begin a pause: IsPaused should be false immediately after Begin (R14.6).");

            // Driving the timer forward over several frames must never report an active hold, so
            // Patrol() advances to the next walk point every frame (continuous motion).
            for (int frame = 0; frame < 120; frame++)
            {
                Assert.IsFalse(pause.Tick(0.016f),
                    $"max==0 must never pause: Tick must return false on frame {frame} (R14.6).");
                Assert.IsFalse(pause.IsPaused,
                    $"max==0 must never pause: IsPaused must stay false on frame {frame} (R14.6).");
            }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, R14.6 example — the no-pause result
        // is independent of the RNG seed, so a max of 0 produces continuous motion deterministically
        // regardless of which per-enemy seed EnemyAI samples from.
        // Validates: Requirements 14.6
        [Test]
        public void PatrolPauseMaxZero_NeverPauses_ForAnySeed()
        {
            foreach (int seed in new[] { 0, 1, 7, 42, 1337, int.MaxValue })
            {
                var pause = new PatrolPause();
                pause.Begin(minSeconds: 0f, maxSeconds: 0f, rng: new System.Random(seed));

                Assert.IsFalse(pause.IsPaused,
                    $"seed={seed}: max==0 must never pause right after Begin (R14.6).");
                Assert.IsFalse(pause.Tick(0.5f),
                    $"seed={seed}: max==0 must never pause even after a large tick (R14.6).");
            }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, R14.6 example — a null RNG is
        // tolerated: a max of 0 still never pauses without throwing, so a misconfigured enemy that
        // reaches Patrol() before its seed is assigned degrades to continuous motion.
        // Validates: Requirements 14.6
        [Test]
        public void PatrolPauseMaxZero_NeverPauses_WithNullRng()
        {
            var pause = new PatrolPause();

            Assert.DoesNotThrow(() => pause.Begin(minSeconds: 0f, maxSeconds: 0f, rng: null),
                "max==0 with a null RNG must not throw (R14.6).");
            Assert.IsFalse(pause.IsPaused,
                "max==0 must never pause even with a null RNG (R14.6).");
            Assert.IsFalse(pause.Tick(0.016f),
                "max==0 must never pause even with a null RNG: Tick stays false (R14.6).");
        }
    }
}
