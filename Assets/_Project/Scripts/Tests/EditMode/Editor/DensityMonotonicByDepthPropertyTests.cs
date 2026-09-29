using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the depth-monotonic density scaling produced by the pure
    /// <see cref="RoomCompositionPlanner"/> — task 3.4 of procedural-stage-room-generation.
    ///
    /// The depth scaling (R5.5) is pure C# with no scene dependency, so it can be property-checked
    /// without a live Unity scene (Property 13). FsCheck/CsCheck cannot be resolved on this machine,
    /// so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic
    /// generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class DensityMonotonicByDepthPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 13: Densidade monótona por profundidade
        // Para toda seed e quaisquer duas profundidades d1 <= d2 (com os mesmos parâmetros e maxDepth),
        // ComputeTargetDensity(d1) <= ComputeTargetDensity(d2): o orçamento de densidade é não-decrescente
        // na profundidade e nunca ultrapassa 30. Cobre parâmetros padrão e configs com orçamentos
        // estreitos (min==max) e amplos via reflexão, além de profundidades no/além do maxDepth e do
        // caso maxDepth<=0 (Stage de profundidade única).
        // Validates: Requirements 5.5
        [Test]
        public void ComputeTargetDensityIsMonotonicNonDecreasingInDepth()
        {
            var planner = new RoomCompositionPlanner();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                int budgetMax = parameters.DensityBudgetMax;

                int maxDepth = rng.Next(0, 25); // includes 0 -> single-depth Stage

                // Two depths with d1 <= d2, both spanning the range including at/beyond maxDepth.
                int a = rng.Next(0, maxDepth + 6);
                int b = rng.Next(0, maxDepth + 6);
                int d1 = Math.Min(a, b);
                int d2 = Math.Max(a, b);

                int density1 = planner.ComputeTargetDensity(parameters, d1, maxDepth);
                int density2 = planner.ComputeTargetDensity(parameters, d2, maxDepth);

                string ctx =
                    $"budget=[{parameters.DensityBudgetMin},{budgetMax}] maxDepth={maxDepth} " +
                    $"d1={d1} d2={d2} density1={density1} density2={density2}";

                // Monotonic non-decreasing: deeper (or equal) room never has a smaller density.
                PropertyCheck.That(density1 <= density2,
                    $"{ctx}: density decreased with depth (density(d1) > density(d2)).");

                // Never exceeds the spec ceiling of 30 (nor the configured budget max).
                PropertyCheck.That(density2 <= budgetMax && density1 <= budgetMax,
                    $"{ctx}: density exceeded the configured budget maximum.");
                PropertyCheck.That(
                    density2 <= StageGenerationParams.DensityBudgetCeiling,
                    $"{ctx}: density exceeded the spec ceiling of 30.");

                // A negative depth is treated as 0, so it must never exceed the depth-0 value.
                int densityNegative = planner.ComputeTargetDensity(parameters, -rng.Next(1, 100), maxDepth);
                int densityZero = planner.ComputeTargetDensity(parameters, 0, maxDepth);
                PropertyCheck.That(densityNegative == densityZero,
                    $"{ctx}: negative depth ({densityNegative}) did not clamp to the depth-0 value ({densityZero}).");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Builds a params instance spanning the input space: the default (budget [4,30]), a narrow
        /// budget (min==max), and a wide budget with random ordered bounds. Private serialized fields
        /// are written via reflection then validated (clamped) exactly as the Editor would.
        /// </summary>
        private static StageGenerationParams RandomParams(Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0:
                    return new StageGenerationParams();
                case 1:
                {
                    int point = rng.Next(
                        StageGenerationParams.DensityBudgetFloor,
                        StageGenerationParams.DensityBudgetCeiling + 1);
                    return BuildParams(point, point);
                }
                default:
                {
                    int a = rng.Next(
                        StageGenerationParams.DensityBudgetFloor,
                        StageGenerationParams.DensityBudgetCeiling + 1);
                    int b = rng.Next(
                        StageGenerationParams.DensityBudgetFloor,
                        StageGenerationParams.DensityBudgetCeiling + 1);
                    return BuildParams(Math.Min(a, b), Math.Max(a, b));
                }
            }
        }

        private static StageGenerationParams BuildParams(int budgetMin, int budgetMax)
        {
            var parameters = new StageGenerationParams();
            SetPrivateField(parameters, "_densityBudgetMin", budgetMin);
            SetPrivateField(parameters, "_densityBudgetMax", budgetMax);
            parameters.Validate();
            return parameters;
        }

        private static void SetPrivateField(object target, string fieldName, int value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on StageGenerationParams.");
            field.SetValue(target, value);
        }
    }
}
