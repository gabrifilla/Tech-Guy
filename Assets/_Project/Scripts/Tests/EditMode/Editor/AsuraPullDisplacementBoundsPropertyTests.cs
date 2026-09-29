using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure Asura pull-displacement math in
    /// <see cref="AsuraPullDisplacement"/> — task 13.3 of weapon-gameplay-swarm-rework.
    ///
    /// <para>
    /// The Asura pull (Manoplas' R) is otherwise scene/agent driven — the thin caller applies the
    /// returned delta through the enemy's existing locomotion, exactly as <c>SoftGroupingService</c>
    /// does — but the R7.3 caps live in <see cref="AsuraPullDisplacement"/> as clamped math so they
    /// can be property-checked without a live Unity scene (Property 20). This project cannot resolve
    /// FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property
    /// (min 128) and reports the exact failing case as a counterexample.
    /// </para>
    /// </summary>
    public sealed class AsuraPullDisplacementBoundsPropertyTests
    {
        private const float Epsilon = 1e-4f;

        // Feature: weapon-gameplay-swarm-rework, Property 20: Atração de Asura limitada
        // Para todo inimigo dentro do raio de 4,0 m ao ativar Asura, a atração total não excede 1,5 m
        // por inimigo, a uma velocidade de no máximo 2,0 m/s; inimigos fora do raio não são atraídos.
        //
        // R7.3: WHEN a Manopla ativa Asura (R), THE Weapon_System SHALL aplicar uma atração dos
        // inimigos dentro de um raio de 4,0 m em direção ao ponto central, a uma velocidade de no
        // máximo 2,0 m/s, sem exceder um deslocamento total de 1,5 m por inimigo.
        //
        // Per-step half: a single application never exceeds MaxSpeed*dt nor the remaining budget, is
        // never negative, points toward the centre (no side channel), never overshoots past the
        // centre, and is exactly zero for enemies outside the 4.0 m radius (never attracted).
        // Validates: Requirements 7.3
        [Test]
        public void SinglePullStepRespectsRadiusSpeedAndRemainingBudgetTowardCentre()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                Vector3 centerPoint = RandomCentre(rng);
                Vector3 enemyPosition = RandomEnemyPosition(rng, centerPoint);
                float alreadyMoved = RandomAlreadyMoved(rng);
                float deltaTime = RandomDeltaTime(rng);

                Vector3 toCenter = centerPoint - enemyPosition;
                float distance = toCenter.magnitude;
                bool withinRadius = AsuraPullDisplacement.IsWithinRadius(centerPoint, enemyPosition);

                Vector3 displacement = AsuraPullDisplacement.ComputeDisplacement(
                    centerPoint, enemyPosition, alreadyMoved, deltaTime);
                float magnitude = displacement.magnitude;

                float remaining = AsuraPullDisplacement.MaxTotalDisplacement - Mathf.Max(0f, alreadyMoved);

                string ctx =
                    $"centre={centerPoint} enemy={enemyPosition} distance={distance} " +
                    $"withinRadius={withinRadius} alreadyMoved={alreadyMoved} dt={deltaTime} " +
                    $"remaining={remaining} => disp={displacement} |disp|={magnitude}";

                // IsWithinRadius agrees with the calculator's own gate: inside is (0, Radius].
                bool expectedWithin = distance > 0f && distance <= AsuraPullDisplacement.Radius;
                PropertyCheck.That(withinRadius == expectedWithin,
                    $"{ctx}: IsWithinRadius={withinRadius} but distance-based expectation was {expectedWithin}.");

                // R7.3: enemies outside the 4.0 m radius (or sitting on the centre) are never attracted.
                if (!withinRadius)
                {
                    PropertyCheck.That(magnitude <= Epsilon,
                        $"{ctx}: enemy outside the 4.0 m radius (or at the centre) but was displaced.");
                    return;
                }

                // No-op cases even inside the radius: non-positive dt or a spent budget produce exactly
                // zero displacement (R7.3 — no impulse when there is nothing to apply).
                if (deltaTime <= 0f || remaining <= 0f)
                {
                    PropertyCheck.That(magnitude <= Epsilon,
                        $"{ctx}: no-op case (dt<=0 or spent budget) but was displaced.");
                    return;
                }

                float speedCap = AsuraPullDisplacement.MaxSpeed * deltaTime;

                // R7.3: the step never exceeds the speed cap (MaxSpeed * dt), so the pull never moves
                // faster than 2.0 m/s.
                PropertyCheck.That(magnitude <= speedCap + Epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds MaxSpeed*dt={speedCap}.");

                // R7.3: no instantaneous impulse larger than the remaining total budget — a single step
                // can never carry the enemy past the 1.5 m total.
                PropertyCheck.That(magnitude <= remaining + Epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds the remaining total budget {remaining} (instantaneous impulse over the cap).");

                // R7.3: the total ceiling itself is never exceeded by a single step regardless of budget.
                PropertyCheck.That(magnitude <= AsuraPullDisplacement.MaxTotalDisplacement + Epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds the total displacement cap {AsuraPullDisplacement.MaxTotalDisplacement}.");

                // No overshoot: a single step never moves the enemy further than the distance to the
                // centre.
                PropertyCheck.That(magnitude <= distance + Epsilon,
                    $"{ctx}: |disp|={magnitude} overshoots past the centre (distance {distance}).");

                // The step is the smallest of the three caps (speed cap, remaining budget, distance).
                float expected = Mathf.Min(speedCap, Mathf.Min(remaining, distance));
                PropertyCheck.That(Mathf.Abs(magnitude - expected) <= 1e-3f,
                    $"{ctx}: |disp|={magnitude} is not min(speedCap={speedCap}, remaining={remaining}, distance={distance})={expected}.");

                // Displacement is directed toward the centre (never a side/opposite channel).
                Vector3 unit = toCenter / distance;
                float projection = Vector3.Dot(displacement, unit);
                PropertyCheck.That(projection > 0f,
                    $"{ctx}: displacement does not point toward the centre (projection {projection}).");
                PropertyCheck.That(Mathf.Abs(projection - magnitude) <= 1e-3f,
                    $"{ctx}: displacement is not colinear with the direction to the centre (projection {projection} != |disp| {magnitude}).");
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 20: Atração de Asura limitada
        // Integrated half: across a whole pull (many applications, arbitrary frame deltas), the total
        // displacement accumulated on an enemy within the 4.0 m radius never exceeds 1,5 m and never
        // overshoots the centre, and each step still honours the 2,0 m/s speed cap — mirroring how the
        // caller feeds alreadyMoved back in per application. Enemies outside the radius accumulate zero.
        // Validates: Requirements 7.3
        [Test]
        public void IntegratedPullNeverExceedsTotalDisplacementNorOvershootsCentre()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                Vector3 centerPoint = RandomCentre(rng);
                bool startInside = rng.Next(0, 4) != 0; // mostly inside, sometimes outside
                Vector3 enemyPosition = startInside
                    ? RandomInsideRadius(rng, centerPoint)
                    : RandomOutsideRadius(rng, centerPoint);

                float startDistance = (centerPoint - enemyPosition).magnitude;
                Vector3 position = enemyPosition;
                float alreadyMoved = 0f;
                int guard = 0;

                // Integrate many applications over the pull, varying the frame delta each step to cover
                // tiny/typical/large steps. Cap the loop so a pathological tiny-dt stream still terminates.
                while (guard++ < 100000)
                {
                    float deltaTime = RandomPositiveDeltaTime(rng);

                    Vector3 step = AsuraPullDisplacement.ComputeDisplacement(
                        centerPoint, position, alreadyMoved, deltaTime);
                    float stepMagnitude = step.magnitude;

                    float distanceToCentre = (centerPoint - position).magnitude;

                    string ctx =
                        $"centre={centerPoint} startInside={startInside} step#{guard} dt={deltaTime} " +
                        $"alreadyMoved={alreadyMoved} step={step} |step|={stepMagnitude} position={position} " +
                        $"distanceToCentre={distanceToCentre}";

                    // Each step honours the speed cap (R7.3: at most 2.0 m/s).
                    PropertyCheck.That(stepMagnitude <= AsuraPullDisplacement.MaxSpeed * deltaTime + Epsilon,
                        $"{ctx}: step {stepMagnitude} exceeds MaxSpeed*dt={AsuraPullDisplacement.MaxSpeed * deltaTime}.");

                    // No step ever overshoots the centre.
                    PropertyCheck.That(stepMagnitude <= distanceToCentre + Epsilon,
                        $"{ctx}: step {stepMagnitude} overshoots past the centre (distance {distanceToCentre}).");

                    if (stepMagnitude <= Epsilon)
                    {
                        // Budget spent, enemy reached the centre, or an outside-radius no-op: pull done.
                        break;
                    }

                    // The step never carries the running total past the 1.5 m cap.
                    PropertyCheck.That(alreadyMoved + stepMagnitude <= AsuraPullDisplacement.MaxTotalDisplacement + Epsilon,
                        $"{ctx}: running total {alreadyMoved + stepMagnitude} exceeds the total cap {AsuraPullDisplacement.MaxTotalDisplacement}.");

                    position += step;
                    alreadyMoved += stepMagnitude;
                }

                // R7.3: an enemy that started outside the 4.0 m radius is never attracted — it stays put.
                if (!startInside)
                {
                    float moved = (position - enemyPosition).magnitude;
                    PropertyCheck.That(moved <= Epsilon,
                        $"centre={centerPoint} enemy={enemyPosition} startDistance={startDistance}: " +
                        $"enemy outside the 4.0 m radius was pulled {moved} m.");
                    return;
                }

                // Total displacement from the start never exceeds the R7.3 total ceiling.
                float total = (position - enemyPosition).magnitude;
                PropertyCheck.That(total <= AsuraPullDisplacement.MaxTotalDisplacement + Epsilon,
                    $"centre={centerPoint} enemy={enemyPosition}: integrated total {total} exceeded the 1.5 m cap.");

                // The accumulated pull never carries the enemy past the centre (no overshoot overall).
                float endDistance = (centerPoint - position).magnitude;
                PropertyCheck.That(endDistance >= -Epsilon,
                    $"centre={centerPoint} enemy={enemyPosition}: ended on the far side of the centre (distance {endDistance}).");
                PropertyCheck.That(endDistance <= startDistance + Epsilon,
                    $"centre={centerPoint} enemy={enemyPosition}: enemy ended further from the centre ({endDistance}) than it started ({startDistance}).");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>An arbitrary world-space centre for the pull (including the origin).</summary>
        private static Vector3 RandomCentre(System.Random rng)
        {
            if (rng.Next(0, 4) == 0) return Vector3.zero;
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 40f,
                ((float)rng.NextDouble() - 0.5f) * 40f,
                ((float)rng.NextDouble() - 0.5f) * 40f);
        }

        /// <summary>
        /// An enemy position spanning the whole input space relative to the centre: exactly on the
        /// centre (degenerate), inside the radius, right at the radius boundary, and outside it.
        /// </summary>
        private static Vector3 RandomEnemyPosition(System.Random rng, Vector3 centre)
        {
            switch (rng.Next(0, 4))
            {
                case 0:
                    return centre; // on the centre -> no-op
                case 1:
                    return RandomInsideRadius(rng, centre);
                case 2:
                    // Exactly at the radius boundary (still inside per the (0, Radius] gate).
                    return centre + RandomUnitDirection(rng) * AsuraPullDisplacement.Radius;
                default:
                    return RandomOutsideRadius(rng, centre);
            }
        }

        /// <summary>A position strictly inside the 4.0 m radius (and not on the centre).</summary>
        private static Vector3 RandomInsideRadius(System.Random rng, Vector3 centre)
        {
            // Distance in (0.05, Radius) so it is clearly inside and not degenerate.
            float distance = 0.05f + (float)rng.NextDouble() * (AsuraPullDisplacement.Radius - 0.05f);
            return centre + RandomUnitDirection(rng) * distance;
        }

        /// <summary>A position strictly outside the 4.0 m radius (never attracted).</summary>
        private static Vector3 RandomOutsideRadius(System.Random rng, Vector3 centre)
        {
            // Distance in (Radius + small, Radius + 20) so it is clearly outside the gate.
            float distance = AsuraPullDisplacement.Radius + 0.01f + (float)rng.NextDouble() * 20f;
            return centre + RandomUnitDirection(rng) * distance;
        }

        /// <summary>A guaranteed non-degenerate unit direction in world space.</summary>
        private static Vector3 RandomUnitDirection(System.Random rng)
        {
            var dir = new Vector3(
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f);
            return dir.sqrMagnitude < 1e-6f ? Vector3.forward : dir.normalized;
        }

        /// <summary>
        /// Metres already applied to the enemy earlier in the pull, spanning: none, well inside the
        /// budget, right at the 1.5 m cap, beyond the cap (spent), and negative (clamped to zero).
        /// </summary>
        private static float RandomAlreadyMoved(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;                                                              // none applied yet
                case 1: return (float)rng.NextDouble() * AsuraPullDisplacement.MaxTotalDisplacement; // inside budget
                case 2: return AsuraPullDisplacement.MaxTotalDisplacement;                      // exactly at the cap
                case 3: return AsuraPullDisplacement.MaxTotalDisplacement + (float)rng.NextDouble() * 5f; // over the cap
                default: return -(float)rng.NextDouble() * 2f;                                  // negative -> clamped to 0
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

        /// <summary>Strictly positive frame deltas for the integrated pull (tiny/typical/large).</summary>
        private static float RandomPositiveDeltaTime(System.Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return 0.0005f + (float)rng.NextDouble() * 0.002f; // tiny step
                case 1: return 0.008f + (float)rng.NextDouble() * 0.03f;   // typical frame time
                default: return 0.05f + (float)rng.NextDouble() * 0.5f;    // large step
            }
        }
    }
}
