using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the stance restore-to-max and deferred-recovery behaviour on a
    /// Stance Break, enforced by the pure, scene-free helpers on
    /// <see cref="StanceBreakBounds"/> (<see cref="StanceBreakBounds.RestoredStanceOnBreak"/>,
    /// <see cref="StanceBreakBounds.NextRecoveryTimeOnBreak"/> and
    /// <see cref="StanceBreakBounds.CanRecoverAt"/>) — task 3.8 of weapon-gameplay-swarm-rework.
    ///
    /// The owning <c>CombatReactionController</c> performs exactly this bookkeeping on break
    /// (<c>currentStance = maxStance; nextStanceRecoveryTime = Time.time + ClampRecoveryDelaySeconds(delay)</c>),
    /// and its recovery gate blocks recovery while <c>Time.time &lt; nextStanceRecoveryTime</c>. The
    /// logic is mirrored here as pure helpers so the invariant can be property-checked without a live
    /// Unity scene. This project cannot resolve FsCheck/CsCheck packages on this machine, so the
    /// agreed seeded harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases
    /// per property (generating max stance, recovery delay and break time across and beyond the
    /// [0.0; 10.0] s window) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class StanceBreakRestoreRecoveryPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 6: Quebra restaura postura ao máximo e adia a recuperação.
        // Para toda quebra de postura, a reserva é restaurada ao máximo do inimigo no instante da
        // quebra e a recuperação só recomeça após o atraso de recuperação configurado.
        // Validates: Requirements 2.7
        [Test]
        public void StanceBreakRestoresToMaxAndDefersRecoveryUntilDelayElapsed()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // The enemy's maximum stance reserve at the moment of the break. The controller floors
                // the max at 1, so we generate around and below that floor as well.
                float maxStance = NextMaxStance(rng);

                // The configured recovery delay, generated across [0.0; 10.0] s and beyond so the clamp
                // is exercised: the deferred recovery time must always use the clamped delay.
                float rawRecoveryDelay = NextRecoveryDelay(rng);
                float clampedDelay = StanceBreakBounds.ClampRecoveryDelaySeconds(rawRecoveryDelay);

                // The time at which the break resolved (analogous to Time.time), non-negative.
                float breakTime = NextBreakTime(rng);

                float restored = StanceBreakBounds.RestoredStanceOnBreak(maxStance);
                float nextRecoveryTime = StanceBreakBounds.NextRecoveryTimeOnBreak(breakTime, rawRecoveryDelay);

                string state =
                    $"[maxStance={maxStance} rawRecoveryDelay={rawRecoveryDelay} clampedDelay={clampedDelay} " +
                    $"breakTime={breakTime} => restored={restored} nextRecoveryTime={nextRecoveryTime}]";

                // R2.7: the reserve is restored to the enemy's (floored) maximum at the instant of the
                // break — never above it, and equal to the max the controller would restore.
                float expectedMax = Mathf.Max(1f, maxStance);
                PropertyCheck.That(
                    Mathf.Abs(restored - expectedMax) <= 1e-3f * (1f + Mathf.Abs(expectedMax)),
                    $"stance must be restored to the enemy's maximum on break for {state}");

                // R2.7: the deferred recovery time uses the delay clamped to [0.0; 10.0] s.
                PropertyCheck.That(
                    clampedDelay >= StanceBreakBounds.MinRecoveryDelaySeconds &&
                    clampedDelay <= StanceBreakBounds.MaxRecoveryDelaySeconds,
                    $"clamped recovery delay must stay in [{StanceBreakBounds.MinRecoveryDelaySeconds}; " +
                    $"{StanceBreakBounds.MaxRecoveryDelaySeconds}] for {state}");

                // R2.7: recovery is deferred by exactly the clamped delay from the break instant.
                float expectedNext = breakTime + clampedDelay;
                PropertyCheck.That(
                    Mathf.Abs(nextRecoveryTime - expectedNext) <= 1e-3f * (1f + Mathf.Abs(expectedNext)),
                    $"recovery must resume exactly the clamped delay after the break for {state}");

                // R2.7: recovery does NOT resume before the delay has fully elapsed. Sample the window
                // strictly before the recovery time; recovery must stay paused there (unless the delay
                // is 0, in which case there is no "before").
                if (clampedDelay > 0f)
                {
                    float justBefore = breakTime + clampedDelay * (float)rng.NextDouble() * 0.999f;
                    PropertyCheck.That(
                        !StanceBreakBounds.CanRecoverAt(justBefore, nextRecoveryTime),
                        $"recovery must stay paused before the delay elapses (now={justBefore}) for {state}");
                }

                // R2.7: recovery DOES resume once the configured delay has elapsed (at and after the
                // deferred recovery time).
                PropertyCheck.That(
                    StanceBreakBounds.CanRecoverAt(nextRecoveryTime, nextRecoveryTime),
                    $"recovery must resume once the delay has elapsed (at nextRecoveryTime) for {state}");

                float afterDelay = nextRecoveryTime + NextFloat(rng, 0f, 5f);
                PropertyCheck.That(
                    StanceBreakBounds.CanRecoverAt(afterDelay, nextRecoveryTime),
                    $"recovery must remain resumed after the delay has elapsed (now={afterDelay}) for {state}");
            });
        }

        /// <summary>
        /// A stance maximum spanning the range a caller might supply: 0 and small values below the
        /// controller's Max(1, …) floor, typical reserves and large pools.
        /// </summary>
        private static float NextMaxStance(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;                          // below the floor
                case 1: return NextFloat(rng, 0f, 1f);      // around the floor
                case 2: return NextFloat(rng, 1f, 100f);    // typical reserve
                case 3: return NextFloat(rng, 100f, 10000f); // large pool
                default: return NextFloat(rng, 1f, 900f);   // common range up to Boss defaults
            }
        }

        /// <summary>
        /// A recovery delay spanning the valid [0.0; 10.0] s window and out-of-range values (negative
        /// and far above the cap) so the clamp inside the helpers is tested against inputs outside the
        /// allowed interval.
        /// </summary>
        private static float NextRecoveryDelay(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return StanceBreakBounds.MinRecoveryDelaySeconds; // 0
                case 1: return StanceBreakBounds.MaxRecoveryDelaySeconds; // 10
                case 2: return NextFloat(rng, -1000f, 0f);  // below range
                case 3: return NextFloat(rng, 10f, 1000f);  // above range
                default: return NextFloat(                    // inside [0; 10]
                    rng,
                    StanceBreakBounds.MinRecoveryDelaySeconds,
                    StanceBreakBounds.MaxRecoveryDelaySeconds);
            }
        }

        /// <summary>
        /// A non-negative break time (analogous to <c>Time.time</c>) spanning early and late run times.
        /// </summary>
        private static float NextBreakTime(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;
                case 1: return NextFloat(rng, 0f, 10f);
                case 2: return NextFloat(rng, 10f, 1000f);
                default: return NextFloat(rng, 0f, 100f);
            }
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
