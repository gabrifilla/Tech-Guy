using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure soft-grouping displacement math in
    /// <see cref="SoftGroupingCalculator"/> — task 8.2 of weapon-gameplay-swarm-rework.
    ///
    /// The soft-grouping behavior is otherwise scene/agent driven (the thin
    /// <c>SoftGroupingService</c> MonoBehaviour applies the delta through NavMeshAgent /
    /// CharacterController), but the R7.1 caps live in <see cref="SoftGroupingCalculator"/> as
    /// clamped math so they can be property-checked without a live Unity scene (Property 18). This
    /// project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property (min
    /// 128) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class SoftGroupingBoundsPropertyTests
    {
        // Non-Bow families: soft grouping applies to every family except the Bow (R7.5). Property 18
        // is about the displacement bounds, so it is driven with the families that actually move.
        private static readonly RunWeaponFamily[] GroupingFamilies =
        {
            RunWeaponFamily.Gauntlet,
            RunWeaponFamily.Spear
        };

        // Feature: weapon-gameplay-swarm-rework, Property 18: Limites do agrupamento suave
        // Para todo conjunto de posições de inimigos, o agrupamento suave só desloca inimigos dentro
        // do raio de 3,0 m do ponto de agrupamento, a uma velocidade de no máximo 1,0 m/s, sem
        // exceder 0,5 m de deslocamento por aplicação. Inimigos fora do raio recebem deslocamento
        // zero; a magnitude respeita MaxSpeed*dt e MaxDisplacementPerApplication; nunca há overshoot
        // (o passo nunca ultrapassa o ponto de agrupamento). Cobre configs nos tetos e configs
        // rebaixadas, com posições/dt/raios arbitrários.
        // Validates: Requirements 7.1
        [Test]
        public void SoftGroupingDisplacementRespectsRadiusSpeedAndPerApplicationCaps()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily equipped = GroupingFamilies[rng.Next(GroupingFamilies.Length)];
                SoftGroupingConfig config = RandomConfig(rng);
                float deltaTime = RandomDeltaTime(rng);

                Vector3 groupPoint = RandomPoint(rng);
                Vector3 enemyPosition = RandomEnemyPoint(rng, groupPoint, config.Radius);

                Vector3 displacement = SoftGroupingCalculator.ComputeDisplacement(
                    equipped, enemyPosition, groupPoint, deltaTime, config);

                Vector3 toGroup = groupPoint - enemyPosition;
                float distance = toGroup.magnitude;
                float magnitude = displacement.magnitude;

                string ctx =
                    $"equipped={equipped} config(speed={config.MaxSpeed}, radius={config.Radius}, " +
                    $"perApp={config.MaxDisplacementPerApplication}) dt={deltaTime} " +
                    $"enemy={enemyPosition} group={groupPoint} distance={distance} disp={displacement}";

                // A non-positive time step (or an invalid config) is a no-op: no application can move the
                // enemy, so the caps below (which scale by dt) do not apply. Assert the zero and stop.
                if (deltaTime <= 0f || !config.IsValid)
                {
                    PropertyCheck.That(magnitude <= 1e-4f,
                        $"{ctx}: no-op case (dt<=0 or invalid config) but was displaced (|disp|={magnitude}).");
                    return;
                }

                // R7.1: enemies outside the grouping radius (or already at the centre) are never moved.
                if (distance <= 0f || distance > config.Radius)
                {
                    PropertyCheck.That(magnitude <= 1e-4f,
                        $"{ctx}: enemy outside radius (or at centre) but was displaced (|disp|={magnitude}).");
                    return;
                }

                // R7.1 caps: never exceed MaxSpeed*dt, never exceed MaxDisplacementPerApplication, and
                // never exceed the R7.1 hard ceilings (1.0 m/s, 0.5 m) regardless of config.
                float speedCap = config.MaxSpeed * deltaTime;
                float perAppCap = config.MaxDisplacementPerApplication;
                const float epsilon = 1e-4f;

                PropertyCheck.That(magnitude <= speedCap + epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds MaxSpeed*dt={speedCap}.");
                PropertyCheck.That(magnitude <= perAppCap + epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds MaxDisplacementPerApplication={perAppCap}.");
                PropertyCheck.That(
                    magnitude <= SoftGroupingConfig.MaxSpeedCap * deltaTime + epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds the R7.1 speed ceiling * dt.");
                PropertyCheck.That(
                    magnitude <= SoftGroupingConfig.MaxDisplacementPerApplicationCap + epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds the R7.1 per-application ceiling " +
                    $"{SoftGroupingConfig.MaxDisplacementPerApplicationCap}.");

                // No overshoot: the step never carries the enemy past the group point. The displacement
                // magnitude is at most the remaining distance, and it points toward the group centre.
                PropertyCheck.That(magnitude <= distance + epsilon,
                    $"{ctx}: overshoot — |disp|={magnitude} is greater than the distance to the group point.");

                if (magnitude > epsilon)
                {
                    Vector3 dir = toGroup / distance;
                    float projection = Vector3.Dot(displacement, dir);
                    // The displacement lies along the enemy->group direction (never away from it).
                    PropertyCheck.That(projection > 0f,
                        $"{ctx}: displacement does not point toward the group point (projection {projection}).");
                    // On that ray, the projected length equals the magnitude (pure toward-centre nudge).
                    PropertyCheck.That(Mathf.Abs(projection - magnitude) <= 1e-3f,
                        $"{ctx}: displacement is not colinear with the toward-group direction " +
                        $"(projection {projection} != |disp| {magnitude}).");
                    // And the post-step position still does not overshoot past the centre.
                    Vector3 next = enemyPosition + displacement;
                    float remaining = (groupPoint - next).magnitude;
                    PropertyCheck.That(remaining <= distance + epsilon,
                        $"{ctx}: post-step distance {remaining} is greater than the pre-step distance {distance}.");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Builds a config spanning the R7.1 ceilings and lowered values, plus intentionally
        /// out-of-range authored values so the calculator's clamp/no-op guarantees are exercised.
        /// </summary>
        private static SoftGroupingConfig RandomConfig(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0:
                    // Default: pinned to the R7.1 caps (1.0 m/s, 3.0 m, 0.5 m).
                    return new SoftGroupingConfig();
                case 1:
                    // Healthy lowered values inside the caps.
                    return new SoftGroupingConfig(
                        0.05f + (float)rng.NextDouble() * SoftGroupingConfig.MaxSpeedCap,
                        0.1f + (float)rng.NextDouble() * SoftGroupingConfig.RadiusCap,
                        0.01f + (float)rng.NextDouble() * SoftGroupingConfig.MaxDisplacementPerApplicationCap);
                case 2:
                    // Deliberately over the ceilings -> must be clamped down to the R7.1 caps.
                    return new SoftGroupingConfig(
                        SoftGroupingConfig.MaxSpeedCap + (float)rng.NextDouble() * 10f,
                        SoftGroupingConfig.RadiusCap + (float)rng.NextDouble() * 10f,
                        SoftGroupingConfig.MaxDisplacementPerApplicationCap + (float)rng.NextDouble() * 10f);
                default:
                    // Possibly non-positive values -> may be an invalid (no-op) config.
                    return new SoftGroupingConfig(
                        ((float)rng.NextDouble() - 0.4f) * SoftGroupingConfig.MaxSpeedCap,
                        ((float)rng.NextDouble() - 0.4f) * SoftGroupingConfig.RadiusCap,
                        ((float)rng.NextDouble() - 0.4f) * SoftGroupingConfig.MaxDisplacementPerApplicationCap);
            }
        }

        /// <summary>Time steps spanning non-positive (no-op), tiny, typical frame, and large values.</summary>
        private static float RandomDeltaTime(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return ((float)rng.NextDouble() - 0.7f) * 0.1f; // may be <= 0 -> no-op
                case 1: return (float)rng.NextDouble() * 0.001f;        // tiny step
                case 2: return 0.01f + (float)rng.NextDouble() * 0.05f; // typical frame time
                default: return 0.1f + (float)rng.NextDouble() * 2f;    // large step
            }
        }

        private static Vector3 RandomPoint(System.Random rng)
        {
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 60f,
                ((float)rng.NextDouble() - 0.5f) * 20f,
                ((float)rng.NextDouble() - 0.5f) * 60f);
        }

        /// <summary>
        /// An enemy position relative to the group point that spans the whole input space: sometimes
        /// exactly on the centre (zero-length ray), sometimes inside the radius, sometimes on/near the
        /// radius boundary, and sometimes well outside it (must receive zero displacement).
        /// </summary>
        private static Vector3 RandomEnemyPoint(System.Random rng, Vector3 groupPoint, float radius)
        {
            switch (rng.Next(0, 4))
            {
                case 0:
                    // On the centre: nothing to move toward (zero-length ray).
                    return groupPoint;
                case 1:
                    // Strictly inside the radius (or inside the R7.1 ceiling when the config is invalid).
                    return groupPoint + RandomDirection(rng) *
                        ((float)rng.NextDouble() * Mathf.Max(radius, 0.001f));
                case 2:
                    // Near the radius boundary (both just inside and just outside).
                    return groupPoint + RandomDirection(rng) *
                        (Mathf.Max(radius, SoftGroupingConfig.RadiusCap) +
                         ((float)rng.NextDouble() - 0.5f) * 0.2f);
                default:
                    // Well outside any radius -> must receive zero displacement.
                    return groupPoint + RandomDirection(rng) *
                        (SoftGroupingConfig.RadiusCap + 1f + (float)rng.NextDouble() * 20f);
            }
        }

        private static Vector3 RandomDirection(System.Random rng)
        {
            var dir = new Vector3(
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f);
            return dir.sqrMagnitude < 1e-6f ? Vector3.right : dir.normalized;
        }
    }
}
