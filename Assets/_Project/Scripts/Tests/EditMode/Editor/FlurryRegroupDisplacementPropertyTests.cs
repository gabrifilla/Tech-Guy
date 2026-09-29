using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the Manoplas' <b>Punhos Relâmpago</b> (W / Flurry) soft-grouping
    /// reapplication — task 13.2 of weapon-gameplay-swarm-rework (Property 19, Requirement 7.2).
    ///
    /// <para>
    /// The bounded per-application math is pure and scene-free, living in
    /// <see cref="FlurryRegroupDisplacement.ComputeDisplacement"/>: while the Flurry runs it keeps the
    /// struck target within <see cref="FlurryRegroupDisplacement.MaxRadiusFromChainPoint"/> (2.0 m) of
    /// the chaining point, only nudging a target that has drifted beyond that radius, never overshooting
    /// past it, and always respecting the shared soft-grouping caps
    /// (<c>SoftGroupingConfig.MaxDisplacementPerApplication</c> and <c>MaxSpeed × dt</c>). This test
    /// property-checks that helper directly; the scene-side wiring in <c>BreakerGauntletCombat</c>
    /// (target acquisition, applying the delta through <c>SoftGroupingService.ApplyExternalDisplacement</c>)
    /// requires a live scene and is NOT exercised here.
    /// </para>
    ///
    /// <para>
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property and reports
    /// the exact failing case as a counterexample.
    /// </para>
    /// </summary>
    public sealed class FlurryRegroupDisplacementPropertyTests
    {
        private const float Epsilon = 1e-3f;

        // Feature: weapon-gameplay-swarm-rework, Property 19: Reaplicação do agrupamento pelo Flurry respeita o teto e o raio
        // Para toda reaplicação de agrupamento durante o Flurry, o alvo é mantido dentro de 2,0 m do
        // ponto de encadeamento e cada aplicação respeita o teto de deslocamento por aplicação.
        //
        // A target still within the 2.0 m chain radius receives zero displacement; a target beyond the
        // radius is pulled toward the chaining point without overshooting the 2.0 m radius, and the step
        // magnitude never exceeds either config.MaxDisplacementPerApplication or config.MaxSpeed * dt.
        // Validates: Requirements 7.2
        [Test]
        public void FlurryRegroupKeepsTargetWithinRadiusAndRespectsPerApplicationCeiling()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                SoftGroupingConfig config = RandomConfig(rng);
                float deltaTime = RandomDeltaTime(rng);
                Vector3 chainPoint = RandomPoint(rng);
                Vector3 targetPosition = RandomTargetPosition(rng, chainPoint);

                float radius = FlurryRegroupDisplacement.MaxRadiusFromChainPoint; // 2.0 m
                float startDistance = (chainPoint - targetPosition).magnitude;

                Vector3 delta = FlurryRegroupDisplacement.ComputeDisplacement(
                    targetPosition, chainPoint, deltaTime, config);

                string ctx =
                    $"target={targetPosition} chain={chainPoint} dt={deltaTime} " +
                    $"maxDisp={config.MaxDisplacementPerApplication} maxSpeed={config.MaxSpeed} " +
                    $"startDistance={startDistance} delta={delta}";

                // A target already inside the 2.0 m chain radius (or exactly on the chaining point) is
                // never nudged — the regroup only corrects drift beyond the radius.
                if (startDistance <= radius + Epsilon)
                {
                    PropertyCheck.That(delta == Vector3.zero,
                        $"{ctx}: target within the {radius} m chain radius was displaced (expected zero).");
                    return;
                }

                float step = delta.magnitude;

                // A non-positive time step (or an invalid config) is a no-op: the helper returns zero and the
                // dt-scaled caps below do not apply. Assert the zero and stop.
                if (deltaTime <= 0f || !config.IsValid)
                {
                    PropertyCheck.That(step <= Epsilon,
                        $"{ctx}: no-op case (dt<=0 or invalid config) but was displaced (step={step}).");
                    return;
                }

                
// Every application respects the shared per-application ceiling and the max-speed × dt cap.
                PropertyCheck.That(step <= config.MaxDisplacementPerApplication + Epsilon,
                    $"{ctx}: step {step} exceeded per-application ceiling {config.MaxDisplacementPerApplication}.");
                PropertyCheck.That(step <= config.MaxSpeed * deltaTime + Epsilon,
                    $"{ctx}: step {step} exceeded max-speed*dt {config.MaxSpeed * deltaTime}.");

                if (step <= Epsilon)
                {
                    // No movement this application (caps allowed nothing). Still valid: the target stayed put.
                    return;
                }

                // The nudge is aimed toward the chaining point (never outward).
                Vector3 toChain = chainPoint - targetPosition;
                float alongToChain = Vector3.Dot(delta.normalized, toChain.normalized);
                PropertyCheck.That(alongToChain >= 1f - Epsilon,
                    $"{ctx}: displacement was not aimed toward the chaining point (dot={alongToChain}).");

                // Applying the delta pulls the target strictly inward but never past the 2.0 m radius
                // (no overshoot toward the chaining point).
                Vector3 newPosition = targetPosition + delta;
                float newDistance = (chainPoint - newPosition).magnitude;

                PropertyCheck.That(newDistance <= startDistance + Epsilon,
                    $"{ctx}: displacement pushed the target farther from the chaining point (newDistance={newDistance}).");
                PropertyCheck.That(newDistance >= radius - Epsilon,
                    $"{ctx}: displacement overshot past the {radius} m chain radius (newDistance={newDistance}).");
            });
        }

        // Focused sub-property: the step never closes more than the excess distance beyond the 2.0 m
        // radius, so the target can reach the radius boundary exactly but is never pulled inside it.
        // Feature: weapon-gameplay-swarm-rework, Property 19: Reaplicação do agrupamento pelo Flurry respeita o teto e o raio
        // Validates: Requirements 7.2
        [Test]
        public void FlurryRegroupNeverClosesMoreThanTheExcessBeyondTheRadius()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                SoftGroupingConfig config = RandomConfig(rng);
                float deltaTime = RandomDeltaTime(rng);
                Vector3 chainPoint = RandomPoint(rng);
                // Force a target beyond the radius so there is always an excess to reason about.
                Vector3 targetPosition = RandomTargetBeyondRadius(rng, chainPoint);

                float radius = FlurryRegroupDisplacement.MaxRadiusFromChainPoint;
                float startDistance = (chainPoint - targetPosition).magnitude;
                float excess = startDistance - radius;

                Vector3 delta = FlurryRegroupDisplacement.ComputeDisplacement(
                    targetPosition, chainPoint, deltaTime, config);
                float step = delta.magnitude;

                string ctx =
                    $"target={targetPosition} chain={chainPoint} dt={deltaTime} " +
                    $"maxDisp={config.MaxDisplacementPerApplication} maxSpeed={config.MaxSpeed} " +
                    $"startDistance={startDistance} excess={excess} step={step}";

                // A non-positive time step (or an invalid config) is a no-op: the helper returns zero and the
                // dt-scaled caps below do not apply. Assert the zero and stop.
                if (deltaTime <= 0f || !config.IsValid)
                {
                    PropertyCheck.That(step <= Epsilon,
                        $"{ctx}: no-op case (dt<=0 or invalid config) but was displaced (step={step}).");
                    return;
                }

                
// The step never closes more than the excess distance beyond the radius.
                PropertyCheck.That(step <= excess + Epsilon,
                    $"{ctx}: step {step} closed more than the excess distance {excess} beyond the radius.");

                // The step equals the minimum of excess, per-application cap and max-speed*dt (the exact
                // bounded-displacement rule the helper documents).
                float expected = Mathf.Min(excess,
                    Mathf.Min(config.MaxDisplacementPerApplication, config.MaxSpeed * deltaTime));
                PropertyCheck.That(Mathf.Abs(step - expected) <= Epsilon,
                    $"{ctx}: step {step} does not match the bounded-displacement rule (expected {expected}).");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Configs spanning invalid (no-op) and valid caps, always within the R7.1 ceilings the
        /// SoftGroupingConfig constructor enforces (MaxSpeed &lt;= 1.0, MaxDisplacement &lt;= 0.5).
        /// </summary>
        private static SoftGroupingConfig RandomConfig(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return new SoftGroupingConfig();                              // default (capped) config
                case 1: return new SoftGroupingConfig(0f, 3f, 0.5f);                  // invalid: zero speed -> no-op
                case 2: return new SoftGroupingConfig(1f, 3f, 0f);                    // invalid: zero per-application -> no-op
                case 3: return new SoftGroupingConfig(                                // tiny valid caps
                    0.05f + (float)rng.NextDouble() * 0.2f,
                    3f,
                    0.02f + (float)rng.NextDouble() * 0.1f);
                default: return new SoftGroupingConfig(                               // arbitrary valid caps within R7.1
                    (float)rng.NextDouble() * SoftGroupingConfig.MaxSpeedCap,
                    3f,
                    (float)rng.NextDouble() * SoftGroupingConfig.MaxDisplacementPerApplicationCap);
            }
        }

        /// <summary>Frame deltas spanning non-positive (no-op), tiny, typical and large steps.</summary>
        private static float RandomDeltaTime(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return ((float)rng.NextDouble() - 0.7f) * 0.02f; // may be <= 0 -> no-op
                case 1: return (float)rng.NextDouble() * 0.002f;         // tiny step
                case 2: return 0.008f + (float)rng.NextDouble() * 0.03f; // typical frame time
                default: return 0.05f + (float)rng.NextDouble() * 0.5f;  // large step
            }
        }

        /// <summary>
        /// A target position at an arbitrary distance from the chaining point: exactly on it, inside
        /// the 2.0 m radius, at the boundary, and beyond it — so both the no-op and the regroup branches
        /// are exercised.
        /// </summary>
        private static Vector3 RandomTargetPosition(System.Random rng, Vector3 chainPoint)
        {
            float radius = FlurryRegroupDisplacement.MaxRadiusFromChainPoint;
            float distance;
            switch (rng.Next(0, 5))
            {
                case 0: distance = 0f; break;                                   // exactly on the chaining point
                case 1: distance = (float)rng.NextDouble() * radius; break;     // inside the radius
                case 2: distance = radius; break;                               // exactly at the boundary
                case 3: distance = radius + (float)rng.NextDouble() * 3f; break; // just beyond
                default: distance = radius + 3f + (float)rng.NextDouble() * 20f; break; // far beyond
            }
            return chainPoint + RandomDirection(rng) * distance;
        }

        /// <summary>A target guaranteed to sit beyond the 2.0 m chain radius.</summary>
        private static Vector3 RandomTargetBeyondRadius(System.Random rng, Vector3 chainPoint)
        {
            float radius = FlurryRegroupDisplacement.MaxRadiusFromChainPoint;
            float distance = radius + 0.1f + (float)rng.NextDouble() * 20f;
            return chainPoint + RandomDirection(rng) * distance;
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
