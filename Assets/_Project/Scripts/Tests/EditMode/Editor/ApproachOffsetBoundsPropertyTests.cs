using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure chase-approach displacement logic in
    /// <see cref="ApproachOffset"/> — task 1.8 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// The chase approach is otherwise scene/NavMesh driven; the bounded-displacement invariants
    /// were extracted into plain, seeded C# so they can be property-checked without a live Unity
    /// scene. FsCheck/CsCheck are not available on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property and
    /// reports the exact failing case as a counterexample. The NavMesh off-mesh fallback to the
    /// player position is covered by the <c>ChasePlayer</c> example test in task 8.3; here we only
    /// verify the pure offset vector the resolver produces.
    /// </summary>
    public sealed class ApproachOffsetBoundsPropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 4
        // Approach offset is bounded, holds between refreshes, suppressed at zero, and NavMesh-sampled.
        // For any magnitude in [0,4], refresh in [0.1,10], dt > 0, and seed, ApproachOffset.Tick returns
        // a ground-plane vector whose magnitude <= configured magnitude, stays fixed between refresh
        // intervals, equals Vector3.zero when magnitude == 0, and is deterministic per seed.
        // Validates: Requirements 13.2, 13.3, 13.6, 13.8, 16.2
        [Test]
        public void ApproachOffsetIsBoundedHeldSuppressedAndDeterministic()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float magnitude = RandomMagnitude(rng);
                float refresh = RandomRefresh(rng);
                int seed = rng.Next();

                float clampedMagnitude = Mathf.Clamp(magnitude, ApproachOffset.MinMagnitude, ApproachOffset.MaxMagnitude);
                float clampedRefresh = Mathf.Clamp(refresh, ApproachOffset.MinRefresh, ApproachOffset.MaxRefresh);

                var offset = new ApproachOffset();
                var rngA = new System.Random(seed);

                // Walk a sequence of ticks that straddles several refresh windows.
                int steps = rng.Next(6, 20);
                float accumulatedSinceRefresh = 0f;
                Vector3 lastRefreshedSample = Vector3.zero;
                bool haveSample = false;

                for (int s = 0; s < steps; s++)
                {
                    float dt = RandomDt(rng, clampedRefresh);
                    Vector3 result = offset.Tick(dt, magnitude, refresh, rngA);

                    // R13.8: a configured magnitude of 0 means a dead-straight beeline — always zero.
                    if (clampedMagnitude <= 0f)
                    {
                        PropertyCheck.That(result == Vector3.zero,
                            $"magnitude={magnitude}: expected Vector3.zero at step {s}, got {result}");
                        continue;
                    }

                    // R13.2: the offset magnitude never exceeds the clamped configured magnitude.
                    PropertyCheck.That(result.magnitude <= clampedMagnitude + 1e-4f,
                        $"magnitude={magnitude} (clamped {clampedMagnitude}): offset {result} has magnitude {result.magnitude} exceeding bound at step {s}");

                    // R13.2 (ground plane): the offset lies on the ground plane (y == 0).
                    PropertyCheck.That(Mathf.Abs(result.y) <= 1e-6f,
                        $"magnitude={magnitude}: offset {result} left the ground plane (y={result.y}) at step {s}");

                    // R13.3: the sample is held fixed between refreshes. The first tick always draws
                    // a sample; afterwards a new sample appears only once the accumulated positive dt
                    // reaches the clamped refresh interval.
                    if (dt > 0f) accumulatedSinceRefresh += dt;

                    bool shouldRefresh = !haveSample || accumulatedSinceRefresh >= clampedRefresh;
                    if (shouldRefresh)
                    {
                        lastRefreshedSample = result;
                        accumulatedSinceRefresh = 0f;
                        haveSample = true;
                    }
                    else
                    {
                        PropertyCheck.That(result == lastRefreshedSample,
                            $"magnitude={magnitude}, refresh={clampedRefresh}: offset changed between refreshes ({lastRefreshedSample} -> {result}) at step {s} with accumulated dt {accumulatedSinceRefresh}");
                    }
                }

                // R16.2: deterministic per seed — replaying the identical (dt, magnitude, refresh)
                // sequence against an identically seeded RNG reproduces the identical offsets.
                var offsetA = new ApproachOffset();
                var offsetB = new ApproachOffset();
                var replayRngA = new System.Random(seed);
                var replayRngB = new System.Random(seed);
                var replayLocalRng = new System.Random(seed ^ 0x5F3759DF);

                int replaySteps = replayLocalRng.Next(6, 20);
                for (int s = 0; s < replaySteps; s++)
                {
                    float dt = RandomDt(replayLocalRng, clampedRefresh);
                    Vector3 a = offsetA.Tick(dt, magnitude, refresh, replayRngA);
                    Vector3 b = offsetB.Tick(dt, magnitude, refresh, replayRngB);
                    PropertyCheck.That(a == b,
                        $"magnitude={magnitude}, seed={seed}: non-deterministic offset at step {s} ({a} != {b})");
                }

                // R13.8: Reset re-arms, and a zero magnitude after reset still yields zero.
                offset.Reset();
                Vector3 afterReset = offset.Tick(RandomDt(rng, clampedRefresh), 0f, refresh, rngA);
                PropertyCheck.That(afterReset == Vector3.zero,
                    $"magnitude=0 after Reset expected Vector3.zero, got {afterReset}");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Magnitudes spanning below range, zero, in range, and above range.</summary>
        private static float RandomMagnitude(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;                                   // suppressed beeline
                case 1: return -1f + (float)rng.NextDouble();        // negative (clamps to 0)
                case 2: return (float)rng.NextDouble() * 4f;         // inside [0,4]
                case 3: return 4f;                                   // the ceiling
                default: return 4f + (float)rng.NextDouble() * 3f;   // above range (clamps to 4)
            }
        }

        /// <summary>Refresh intervals spanning below, inside, and above the clamp range.</summary>
        private static float RandomRefresh(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return (float)rng.NextDouble() * 0.1f;       // below floor (clamps to 0.1)
                case 1: return 0.1f + (float)rng.NextDouble() * 9.9f; // inside [0.1,10]
                case 2: return 10f + (float)rng.NextDouble() * 5f;   // above ceiling (clamps to 10)
                default: return 1f;                                  // the default
            }
        }

        /// <summary>
        /// Positive dt steps small relative to the refresh interval so a run spans several refresh
        /// windows (occasionally a larger jump that crosses an interval in one step).
        /// </summary>
        private static float RandomDt(System.Random rng, float clampedRefresh)
        {
            if (rng.Next(0, 5) == 0)
            {
                // Occasionally a big step that crosses the whole interval at once.
                return clampedRefresh * (0.5f + (float)rng.NextDouble() * 2f);
            }
            return clampedRefresh * (0.1f + (float)rng.NextDouble() * 0.35f);
        }
    }
}
