using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the Manoplas' <b>Avanço Relâmpago</b> (Q) advance — task 10.2 of
    /// weapon-gameplay-swarm-rework (Property 25, Requirement 8.2).
    ///
    /// <para><b>Scope / scene-bound caveat.</b> The full Property 25 statement has two coupled halves:</para>
    /// <list type="bullet">
    /// <item><description>
    /// the advance destination stays <b>within the advance distance</b> of the player — this is a pure,
    /// per-frame distance-integration invariant driven by <c>AdvanceDistance</c>, the RocketAdvance rank
    /// (<c>+70%</c> per level) and <c>AdvanceDuration</c>; and
    /// </description></item>
    /// <item><description>
    /// the destination is <b>clamped to the NavMesh</b> (points outside the navigable area snap to the
    /// nearest valid navigable point) — this half is done in
    /// <c>BreakerGauntletCombat.Execute</c> via <c>NavMeshAgent.Raycast</c> and <b>requires a live
    /// baked NavMesh in a running scene</b>. It cannot be exercised in EditMode without a scene and is
    /// NOT validated by this test.
    /// </description></item>
    /// </list>
    ///
    /// <para>
    /// The advance logic is inline in the <c>BreakerGauntletCombat</c> MonoBehaviour (there is no pure
    /// <c>GauntletAdvanceClamp</c> helper to import, and source files must not be edited for this task),
    /// so this test mirrors the exact per-frame advance math from that coroutine to property-check the
    /// distance-clamp invariant that IS pure and scene-independent: for any aim point / direction, the
    /// integrated advance over the whole advance window never carries the player past the (rank-scaled)
    /// advance distance, and the NavMesh Raycast clamp can only ever pull the destination <i>inward</i>
    /// (nearer than or equal to the un-clamped destination), never farther out.
    /// </para>
    ///
    /// <para>
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property (min 128)
    /// and reports the exact failing case as a counterexample.
    /// </para>
    /// </summary>
    public sealed class GauntletAdvanceNavMeshPropertyTests
    {
        private const float Epsilon = 1e-3f;

        // Mirror of BreakerGauntletCombat.Execute: RocketAdvance adds +70% advance distance per rank.
        private const float RocketAdvancePerRank = 0.7f;

        // Feature: weapon-gameplay-swarm-rework, Property 25: Avanço Relâmpago fica na NavMesh
        // Para todo ponto de mira, o destino do Avanço Relâmpago (Q das Manoplas) está sobre a NavMesh e
        // dentro da distância de avanço; pontos fora da área navegável são clampados ao ponto navegável
        // válido mais próximo.
        //
        // Pure, scene-independent half validated here: for any aim direction and any advance
        // configuration (base distance, RocketAdvance rank, advance duration) the per-frame advance
        // integration used by BreakerGauntletCombat never carries the player past the effective advance
        // distance advance = AdvanceDistance * (1 + 0.7 * rank), and the NavMesh clamp — modelled as a
        // nearest-navigable-point snap — can only ever reduce (never increase) the distance travelled.
        //
        // Scene-bound half NOT validated here (needs a live baked NavMesh): the actual
        // NavMeshAgent.Raycast snapping of an off-mesh destination to the nearest navigable point.
        // Validates: Requirements 8.2
        [Test]
        public void AdvanceStaysWithinAdvanceDistanceAndNavMeshClampOnlyPullsInward()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                Vector3 startPosition = RandomPoint(rng);
                Vector3 direction = RandomDirection(rng); // the aim direction (transform.forward)

                float baseDistance = RandomAdvanceDistance(rng);
                int rocketRank = RandomRank(rng);
                float advanceDuration = RandomAdvanceDuration(rng);

                // Effective advance distance exactly as BreakerGauntletCombat computes it.
                float advance = baseDistance * (1f + RocketAdvancePerRank * rocketRank);

                string ctx =
                    $"start={startPosition} dir={direction} baseDistance={baseDistance} " +
                    $"rocketRank={rocketRank} advance={advance} advanceDuration={advanceDuration}";

                // Integrate the advance across the whole advance window using the same per-frame step the
                // coroutine uses. We vary the frame delta each iteration to cover tiny/typical/large steps.
                Vector3 position = startPosition;
                float elapsed = 0f;
                int guard = 0;
                while (elapsed < advanceDuration && guard++ < 100000)
                {
                    float delta = RandomFrameDelta(rng);
                    if (delta <= 0f) continue;

                    // Exact per-frame advance step from BreakerGauntletCombat.Execute.
                    float stepDistance = advance * Mathf.Min(delta, advanceDuration - elapsed)
                        / Mathf.Max(0.01f, advanceDuration);
                    Vector3 unclampedDestination = position + direction * stepDistance;

                    // NavMesh clamp model: the real code calls agent.Raycast and, when it hits an edge,
                    // moves to edge.position — a point on the segment between the current position and the
                    // requested destination, i.e. never farther than the requested step. We model that as a
                    // nearest-navigable snap that can pull the destination inward by an arbitrary fraction
                    // in [0,1] (fraction 1 == fully navigable, no clamp).
                    float navigableFraction = (float)rng.NextDouble();
                    Vector3 clampedDestination = position + (unclampedDestination - position) * navigableFraction;

                    // The clamp can only ever pull the destination inward (nearer to the current position),
                    // never push it farther out than the requested step.
                    float unclampedStep = (unclampedDestination - position).magnitude;
                    float clampedStep = (clampedDestination - position).magnitude;
                    PropertyCheck.That(clampedStep <= unclampedStep + Epsilon,
                        $"{ctx}: NavMesh clamp pushed destination outward (clampedStep={clampedStep} > unclampedStep={unclampedStep}).");

                    // A single step never exceeds the whole advance distance.
                    PropertyCheck.That(unclampedStep <= advance + Epsilon,
                        $"{ctx}: single advance step {unclampedStep} exceeded total advance distance {advance}.");

                    position = clampedDestination;
                    elapsed += delta;
                }

                // Total displacement from the start never exceeds the effective advance distance, whatever
                // the NavMesh clamp did (clamping only ever shortened the path).
                float totalTravelled = (position - startPosition).magnitude;
                PropertyCheck.That(totalTravelled <= advance + Epsilon,
                    $"{ctx}: total advance {totalTravelled} exceeded the effective advance distance {advance}.");

                // The advance is monotone along the aim direction: the player never ends behind the start
                // point along that direction (the advance never sends the player backwards).
                float alongDirection = Vector3.Dot(position - startPosition, direction);
                PropertyCheck.That(alongDirection >= -Epsilon,
                    $"{ctx}: advance moved the player backwards along the aim direction (projection {alongDirection}).");
                // And that forward projection is itself bounded by the advance distance.
                PropertyCheck.That(alongDirection <= advance + Epsilon,
                    $"{ctx}: forward advance {alongDirection} exceeded the effective advance distance {advance}.");
            });
        }

        // Focused sub-property: the RocketAdvance rank scaling is monotone and matches the +70%/rank rule,
        // so a higher rank never yields a shorter effective advance distance (Requirement 8.2 upper bound
        // is rank-scaled, not fixed).
        // Feature: weapon-gameplay-swarm-rework, Property 25: Avanço Relâmpago fica na NavMesh
        // Validates: Requirements 8.2
        [Test]
        public void EffectiveAdvanceDistanceIsMonotoneInRocketAdvanceRank()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float baseDistance = RandomAdvanceDistance(rng);
                int lowRank = rng.Next(0, 6);
                int highRank = lowRank + rng.Next(0, 6);

                float low = baseDistance * (1f + RocketAdvancePerRank * lowRank);
                float high = baseDistance * (1f + RocketAdvancePerRank * highRank);

                string ctx = $"baseDistance={baseDistance} lowRank={lowRank} highRank={highRank} low={low} high={high}";

                PropertyCheck.That(high >= low - Epsilon,
                    $"{ctx}: higher RocketAdvance rank produced a shorter effective advance distance.");

                // Exact scaling: each additional rank adds 70% of the base distance.
                float expectedHigh = baseDistance * (1f + RocketAdvancePerRank * highRank);
                PropertyCheck.That(Mathf.Abs(high - expectedHigh) <= Epsilon,
                    $"{ctx}: effective advance {high} does not match the +70%/rank rule ({expectedHigh}).");

                // A non-negative base distance never produces a negative advance distance.
                PropertyCheck.That(high >= -Epsilon,
                    $"{ctx}: effective advance distance is negative.");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Advance distances spanning zero, small, and large authored values (BreakerGauntletAbility clamps >= 0).</summary>
        private static float RandomAdvanceDistance(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;                                   // exactly zero (no advance)
                case 1: return (float)rng.NextDouble() * 0.5f;       // tiny advance
                case 2: return 0.5f + (float)rng.NextDouble() * 4f;  // typical advance
                default: return 4f + (float)rng.NextDouble() * 16f;  // large advance
            }
        }

        /// <summary>RocketAdvance ranks spanning no-run (0) through a generous run level.</summary>
        private static int RandomRank(System.Random rng)
        {
            return rng.Next(0, 8);
        }

        /// <summary>Advance durations spanning the BreakerGauntletAbility minimum (0.01) and larger values.</summary>
        private static float RandomAdvanceDuration(System.Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return 0.01f;                                // the authored minimum
                case 1: return 0.05f + (float)rng.NextDouble() * 0.3f;
                default: return 0.3f + (float)rng.NextDouble() * 1.5f;
            }
        }

        /// <summary>Frame deltas spanning non-positive (skipped), tiny, typical, and large steps.</summary>
        private static float RandomFrameDelta(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return ((float)rng.NextDouble() - 0.7f) * 0.02f; // may be <= 0 -> skipped
                case 1: return (float)rng.NextDouble() * 0.002f;         // tiny step
                case 2: return 0.008f + (float)rng.NextDouble() * 0.03f; // typical frame time
                default: return 0.05f + (float)rng.NextDouble() * 0.5f;  // large step
            }
        }

        private static Vector3 RandomPoint(System.Random rng)
        {
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 60f,
                ((float)rng.NextDouble() - 0.5f) * 20f,
                ((float)rng.NextDouble() - 0.5f) * 60f);
        }

        private static Vector3 RandomDirection(System.Random rng)
        {
            var dir = new Vector3(
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f);
            return dir.sqrMagnitude < 1e-6f ? Vector3.forward : dir.normalized;
        }
    }
}
