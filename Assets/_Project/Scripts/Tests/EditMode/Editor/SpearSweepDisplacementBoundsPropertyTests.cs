using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure Lança sweep-displacement math in
    /// <see cref="SpearSweepDisplacement"/> — task 11.6 of weapon-gameplay-swarm-rework.
    ///
    /// <para>
    /// The sweep push (Lança's orbital W) is otherwise scene/agent driven — the thin
    /// <c>ArsenalCombat</c> caller applies the returned delta through the enemy's existing
    /// locomotion, exactly as <c>SoftGroupingService</c> does — but the R7.4 caps live in
    /// <see cref="SpearSweepDisplacement"/> as clamped math so they can be property-checked without a
    /// live Unity scene (Property 21). This project cannot resolve FsCheck/CsCheck packages on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases per property (min 128) and reports the exact failing case as a
    /// counterexample.
    /// </para>
    /// </summary>
    public sealed class SpearSweepDisplacementBoundsPropertyTests
    {
        private const float Epsilon = 1e-4f;

        // Feature: weapon-gameplay-swarm-rework, Property 21: Deslocamento da varredura da Lança limitado
        // Para todo inimigo atingido pela varredura, o deslocamento total na direção da varredura não
        // excede 0,75 m, a uma velocidade de no máximo 1,5 m/s, sem impulso instantâneo acima desse
        // limite.
        //
        // R7.4: WHEN a Lança executa a varredura (Sweep, W) e atinge um inimigo, THE Weapon_System
        // SHALL deslocar o inimigo atingido na direção da varredura a uma velocidade de no máximo
        // 1,5 m/s, limitado a um deslocamento total de 0,75 m, sem aplicar impulso instantâneo maior
        // que esse limite.
        //
        // Per-step half: a single application never exceeds MaxSpeed*dt nor the remaining budget, is
        // never negative, and points along the sweep direction (no side channel).
        // Validates: Requirements 7.4
        [Test]
        public void SingleSweepStepRespectsSpeedAndRemainingBudgetAlongSweepDirection()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                Vector3 sweepDirection = RandomDirectionVector(rng);
                float alreadyMoved = RandomAlreadyMoved(rng);
                float deltaTime = RandomDeltaTime(rng);

                Vector3 displacement = SpearSweepDisplacement.ComputeDisplacement(
                    sweepDirection, alreadyMoved, deltaTime);
                float magnitude = displacement.magnitude;

                float dirLength = sweepDirection.magnitude;
                float remaining = SpearSweepDisplacement.MaxTotalDisplacement - Mathf.Max(0f, alreadyMoved);

                string ctx =
                    $"sweepDir={sweepDirection} (|dir|={dirLength}) alreadyMoved={alreadyMoved} " +
                    $"dt={deltaTime} remaining={remaining} => disp={displacement} |disp|={magnitude}";

                // No-op cases: non-positive dt, degenerate direction, or budget already spent produce
                // exactly zero displacement (R7.4 — no impulse when there is nothing to apply).
                if (deltaTime <= 0f || dirLength <= Mathf.Epsilon || remaining <= 0f)
                {
                    PropertyCheck.That(magnitude <= Epsilon,
                        $"{ctx}: no-op case (dt<=0, degenerate direction, or spent budget) but was displaced.");
                    return;
                }

                float speedCap = SpearSweepDisplacement.MaxSpeed * deltaTime;

                // R7.4: the step never exceeds the speed cap (MaxSpeed * dt), so the push never moves
                // faster than 1.5 m/s.
                PropertyCheck.That(magnitude <= speedCap + Epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds MaxSpeed*dt={speedCap}.");

                // R7.4: no instantaneous impulse larger than the remaining total budget — a single step
                // can never carry the enemy past the 0.75 m total.
                PropertyCheck.That(magnitude <= remaining + Epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds the remaining total budget {remaining} (instantaneous impulse over the cap).");

                // R7.4: the total ceiling itself is never exceeded by a single step regardless of budget.
                PropertyCheck.That(magnitude <= SpearSweepDisplacement.MaxTotalDisplacement + Epsilon,
                    $"{ctx}: |disp|={magnitude} exceeds the total displacement cap {SpearSweepDisplacement.MaxTotalDisplacement}.");

                // The step is the smaller of the two caps (speed cap vs remaining budget), exactly.
                float expected = Mathf.Min(speedCap, remaining);
                PropertyCheck.That(Mathf.Abs(magnitude - expected) <= 1e-3f,
                    $"{ctx}: |disp|={magnitude} is not min(speedCap={speedCap}, remaining={remaining})={expected}.");

                // Displacement is directed along the sweep direction (never a side/opposite channel).
                Vector3 unit = sweepDirection / dirLength;
                float projection = Vector3.Dot(displacement, unit);
                PropertyCheck.That(projection > 0f,
                    $"{ctx}: displacement does not point along the sweep direction (projection {projection}).");
                PropertyCheck.That(Mathf.Abs(projection - magnitude) <= 1e-3f,
                    $"{ctx}: displacement is not colinear with the sweep direction (projection {projection} != |disp| {magnitude}).");
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 21: Deslocamento da varredura da Lança limitado
        // Integrated half: across a whole sweep (many applications, arbitrary frame deltas), the total
        // displacement accumulated on the enemy along the sweep direction never exceeds 0,75 m, and each
        // step still honours the 1,5 m/s speed cap — mirroring how ArsenalCombat feeds alreadyMoved back
        // in per application.
        // Validates: Requirements 7.4
        [Test]
        public void IntegratedSweepNeverExceedsTotalDisplacementAlongSweepDirection()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // A single sweep pushes along one fixed direction; the caller tracks alreadyMoved.
                Vector3 sweepDirection = RandomNonDegenerateDirection(rng);
                Vector3 unit = sweepDirection.normalized;

                Vector3 position = Vector3.zero;
                float alreadyMoved = 0f;
                int guard = 0;

                // Integrate many applications over the sweep, varying the frame delta each step to cover
                // tiny/typical/large steps. Cap the loop so a pathological tiny-dt stream still terminates.
                while (guard++ < 100000)
                {
                    float deltaTime = RandomPositiveDeltaTime(rng);

                    Vector3 step = SpearSweepDisplacement.ComputeDisplacement(
                        sweepDirection, alreadyMoved, deltaTime);
                    float stepMagnitude = step.magnitude;

                    string ctx =
                        $"sweepDir={sweepDirection} step#{guard} dt={deltaTime} alreadyMoved={alreadyMoved} " +
                        $"step={step} |step|={stepMagnitude} position={position}";

                    // Each step honours the speed cap (R7.4: at most 1.5 m/s).
                    PropertyCheck.That(stepMagnitude <= SpearSweepDisplacement.MaxSpeed * deltaTime + Epsilon,
                        $"{ctx}: step {stepMagnitude} exceeds MaxSpeed*dt={SpearSweepDisplacement.MaxSpeed * deltaTime}.");

                    if (stepMagnitude <= Epsilon)
                    {
                        // Budget spent (or a no-op step): the sweep push is done.
                        break;
                    }

                    // The step never carries the running total past the 0.75 m cap.
                    PropertyCheck.That(alreadyMoved + stepMagnitude <= SpearSweepDisplacement.MaxTotalDisplacement + Epsilon,
                        $"{ctx}: running total {alreadyMoved + stepMagnitude} exceeds the total cap {SpearSweepDisplacement.MaxTotalDisplacement}.");

                    position += step;
                    alreadyMoved += stepMagnitude;
                }

                // Total displacement from the start never exceeds the R7.4 total ceiling.
                float total = position.magnitude;
                PropertyCheck.That(total <= SpearSweepDisplacement.MaxTotalDisplacement + Epsilon,
                    $"sweepDir={sweepDirection}: integrated total {total} exceeded the 0.75 m cap.");

                // The accumulated push lies along the sweep direction (never backwards).
                float alongDirection = Vector3.Dot(position, unit);
                PropertyCheck.That(alongDirection >= -Epsilon,
                    $"sweepDir={sweepDirection}: sweep moved the enemy backwards along the sweep direction (projection {alongDirection}).");
                PropertyCheck.That(alongDirection <= SpearSweepDisplacement.MaxTotalDisplacement + Epsilon,
                    $"sweepDir={sweepDirection}: forward displacement {alongDirection} exceeded the 0.75 m cap.");
                // The whole path is colinear with the sweep direction (pure along-direction nudge).
                PropertyCheck.That(Mathf.Abs(alongDirection - total) <= 1e-3f,
                    $"sweepDir={sweepDirection}: accumulated displacement is not colinear with the sweep direction " +
                    $"(projection {alongDirection} != total {total}).");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// A sweep direction spanning the whole input space: unit-length, arbitrary non-unit lengths
        /// (the calculator normalises), and occasionally a degenerate near-zero vector so the no-op
        /// guard is exercised.
        /// </summary>
        private static Vector3 RandomDirectionVector(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0:
                    return Vector3.zero; // degenerate -> no-op
                case 1:
                    return RandomNonDegenerateDirection(rng); // unit-ish
                case 2:
                    // Arbitrary non-unit length (calculator must normalise): magnitude far from 1.
                    return RandomNonDegenerateDirection(rng) * (0.01f + (float)rng.NextDouble() * 50f);
                default:
                    // Near-degenerate small vector around the epsilon boundary.
                    return RandomNonDegenerateDirection(rng) * ((float)rng.NextDouble() * 1e-3f);
            }
        }

        /// <summary>A guaranteed non-degenerate world-space direction (unit length).</summary>
        private static Vector3 RandomNonDegenerateDirection(System.Random rng)
        {
            var dir = new Vector3(
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f);
            return dir.sqrMagnitude < 1e-6f ? Vector3.forward : dir.normalized;
        }

        /// <summary>
        /// Metres already applied to the enemy earlier in the sweep, spanning: none, well inside the
        /// budget, right at the 0.75 m cap, beyond the cap (spent), and negative (clamped to zero).
        /// </summary>
        private static float RandomAlreadyMoved(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;                                                    // none applied yet
                case 1: return (float)rng.NextDouble() * SpearSweepDisplacement.MaxTotalDisplacement; // inside budget
                case 2: return SpearSweepDisplacement.MaxTotalDisplacement;           // exactly at the cap
                case 3: return SpearSweepDisplacement.MaxTotalDisplacement + (float)rng.NextDouble() * 5f; // over the cap
                default: return -(float)rng.NextDouble() * 2f;                        // negative -> clamped to 0
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

        /// <summary>Strictly positive frame deltas for the integrated sweep (tiny/typical/large).</summary>
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
