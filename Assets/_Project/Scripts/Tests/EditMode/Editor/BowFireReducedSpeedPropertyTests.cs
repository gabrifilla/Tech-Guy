using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure reduced-movement-while-firing decision in
    /// <see cref="BowFireMovement"/> / <see cref="BowFireMovementConfig"/> — task 12.5 of
    /// weapon-gameplay-swarm-rework.
    ///
    /// The Arco lets the player keep moving while shooting, but never at full speed and never at a
    /// dead stop. That "strictly between 0 and full speed" invariant (Property 33, Requisito 10.1)
    /// lives in the plain <see cref="BowFireMovement"/> collaborator so it can be property-checked
    /// without a live Unity scene. This project cannot resolve FsCheck/CsCheck packages on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases per property (min 128) and reports the exact failing case as a
    /// counterexample.
    /// </summary>
    public sealed class BowFireReducedSpeedPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 33: Velocidade reduzida ao disparar o Arco
        // Para toda velocidade de movimento plena, a velocidade permitida enquanto o jogador dispara
        // com o Arco é estritamente maior que 0 e estritamente menor que a velocidade plena. Com
        // qualquer fração autorada (mesmo em ou além dos limites 0/1, negativa, acima de 1 ou NaN), o
        // clamp pina a fração estritamente dentro de (0, 1); logo, para todo fullSpeed > 0 o resultado
        // é estritamente > 0 e estritamente < fullSpeed. Uma velocidade plena não-positiva não tem
        // movimento a reduzir, então o resultado é exatamente 0.
        // Validates: Requirements 10.1
        [Test]
        public void FiringSpeedIsStrictlyBetweenZeroAndFullSpeedForAnyPositiveFullSpeed()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- an arbitrary authored fraction: in-range, at/beyond a bound, negative, > 1 ----
                float authoredFraction = RandomAuthoredFraction(rng);
                var config = new BowFireMovementConfig { MoveFraction = authoredFraction };
                var movement = new BowFireMovement(config);

                // The clamp always pins the fraction STRICTLY inside (0, 1) — never a dead stop, never
                // full speed — regardless of what was authored.
                float clamped = movement.ClampedFraction;
                PropertyCheck.That(clamped > 0f && clamped < 1f,
                    $"[case #{i}] clamped fraction {clamped} not strictly inside (0,1) " +
                    $"for authored {authoredFraction}");

                // ---- Property 33: for any full speed > 0, firing speed is in (0, fullSpeed) --------
                float fullSpeed = RandomPositiveFullSpeed(rng);
                float reduced = movement.ReducedSpeed(fullSpeed);

                PropertyCheck.That(reduced > 0f,
                    $"[case #{i}] reduced speed {reduced} was not strictly greater than 0 " +
                    $"(fullSpeed={fullSpeed}, authoredFraction={authoredFraction})");
                PropertyCheck.That(reduced < fullSpeed,
                    $"[case #{i}] reduced speed {reduced} was not strictly less than full speed " +
                    $"{fullSpeed} (authoredFraction={authoredFraction})");

                // ---- a non-positive full speed has no movement to reduce -> exactly 0 --------------
                float nonPositiveFullSpeed = RandomNonPositiveFullSpeed(rng);
                float reducedFromStandstill = movement.ReducedSpeed(nonPositiveFullSpeed);

                PropertyCheck.That(reducedFromStandstill == 0f,
                    $"[case #{i}] non-positive full speed {nonPositiveFullSpeed} yielded " +
                    $"{reducedFromStandstill} instead of 0 (authoredFraction={authoredFraction})");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// An authored fraction spanning every relevant case: comfortably inside (0,1), exactly at a
        /// bound (0 or 1), negative, above 1, and NaN. Each must still clamp to a value strictly
        /// inside (0,1), so <see cref="BowFireMovement"/> can never produce a full stop or full speed.
        /// </summary>
        private static float RandomAuthoredFraction(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return (float)rng.NextDouble();                       // 0 .. 1 (in-range, incl. bounds)
                case 1: return 0f;                                            // exactly the lower bound
                case 2: return 1f;                                            // exactly the upper bound
                case 3: return -(float)rng.NextDouble() * 5f;                 // negative (below 0)
                case 4: return 1f + (float)rng.NextDouble() * 5f;             // above 1
                default: return float.NaN;                                    // NaN fallback
            }
        }

        /// <summary>A strictly positive full move speed in m/s, across a wide range.</summary>
        private static float RandomPositiveFullSpeed(System.Random rng)
        {
            // 0.001 .. ~100 m/s so tiny and large speeds are both exercised, always > 0.
            return 0.001f + (float)rng.NextDouble() * 100f;
        }

        /// <summary>A non-positive full move speed: exactly 0 or negative.</summary>
        private static float RandomNonPositiveFullSpeed(System.Random rng)
        {
            return rng.Next(0, 2) == 0 ? 0f : -(float)rng.NextDouble() * 100f;
        }
    }
}
