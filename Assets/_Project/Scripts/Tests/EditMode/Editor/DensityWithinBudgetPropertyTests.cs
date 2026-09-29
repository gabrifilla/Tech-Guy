using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the depth-scaled density budget produced by the pure
    /// <see cref="RoomCompositionPlanner"/> — task 3.3 of procedural-stage-room-generation.
    ///
    /// The Density_Budget logic (R5.2) is pure C# with no scene dependency, so it can be
    /// property-checked without a live Unity scene (Property 12). FsCheck/CsCheck cannot be
    /// resolved on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives
    /// &gt;= 100 deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class DensityWithinBudgetPropertyTests
    {
        // All 16 archetypes, used to feed the planner with real availability.
        private static readonly ArchetypeId[] AllArchetypes =
            (ArchetypeId[])Enum.GetValues(typeof(ArchetypeId));

        // Feature: procedural-stage-room-generation, Property 12: Densidade dentro do orçamento
        // Para toda seed, profundidade e maxDepth, o TargetDensity calculado pelo
        // RoomCompositionPlanner fica dentro de [DensityBudgetMin, DensityBudgetMax], que por sua vez
        // está contido em [4, 30]. O TargetDensity resultante do Plan coincide com ComputeTargetDensity,
        // e a soma das quantidades dos slots distribui exatamente TargetDensity inimigos entre os
        // arquétipos escolhidos. Cobre parâmetros padrão e configs com orçamentos estreitos (min==max)
        // e alvos de variedade variados via reflexão sobre os campos serializados.
        // Validates: Requirements 5.2
        [Test]
        public void ComputedAndPlannedDensityStayWithinBudgetAndDistributeExactly()
        {
            var planner = new RoomCompositionPlanner();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                int budgetMin = parameters.DensityBudgetMin;
                int budgetMax = parameters.DensityBudgetMax;

                int maxDepth = rng.Next(0, 25);          // includes 0 (single-depth Stage)
                int depth = rng.Next(0, maxDepth + 6);   // includes depths at/beyond maxDepth
                int availableCount = rng.Next(0, AllArchetypes.Length + 1);
                IReadOnlyList<ArchetypeId> available = TakeArchetypes(availableCount);

                int computed = planner.ComputeTargetDensity(parameters, depth, maxDepth);

                string ctx =
                    $"budget=[{budgetMin},{budgetMax}] depth={depth} maxDepth={maxDepth} " +
                    $"available={availableCount} computed={computed}";

                // The spec floor/ceiling: the budget bounds themselves are within [4, 30].
                PropertyCheck.That(
                    budgetMin >= StageGenerationParams.DensityBudgetFloor &&
                    budgetMax <= StageGenerationParams.DensityBudgetCeiling,
                    $"{ctx}: budget bounds fall outside the spec range [4, 30].");

                // ComputeTargetDensity is within [budgetMin, budgetMax] (and hence [4, 30]).
                PropertyCheck.That(computed >= budgetMin && computed <= budgetMax,
                    $"{ctx}: computed density outside the budget range.");
                PropertyCheck.That(
                    computed >= StageGenerationParams.DensityBudgetFloor &&
                    computed <= StageGenerationParams.DensityBudgetCeiling,
                    $"{ctx}: computed density outside the spec ceiling [4, 30].");

                var rngForPlan = new SeededRng(RandomSeed(rng));
                RoomComposition composition = planner.Plan(parameters, available, depth, maxDepth, rngForPlan);

                // Plan's TargetDensity matches ComputeTargetDensity and stays in range.
                PropertyCheck.That(composition.TargetDensity == computed,
                    $"{ctx}: Plan TargetDensity={composition.TargetDensity} != ComputeTargetDensity={computed}.");
                PropertyCheck.That(
                    composition.TargetDensity >= budgetMin && composition.TargetDensity <= budgetMax,
                    $"{ctx}: Plan TargetDensity outside the budget range.");

                // The sum of slot counts equals TargetDensity whenever there is at least one
                // archetype to place; with no archetypes the composition is empty (no enemies).
                int distributed = 0;
                for (int s = 0; s < composition.Slots.Count; s++)
                {
                    PropertyCheck.That(composition.Slots[s].Count >= 1,
                        $"{ctx}: a chosen archetype slot has a non-positive count.");
                    distributed += composition.Slots[s].Count;
                }

                if (availableCount == 0)
                {
                    PropertyCheck.That(composition.Slots.Count == 0 && distributed == 0,
                        $"{ctx}: expected an empty composition when no archetypes are available.");
                }
                else
                {
                    PropertyCheck.That(distributed == composition.TargetDensity,
                        $"{ctx}: distributed enemies ({distributed}) != TargetDensity ({composition.TargetDensity}).");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Builds a params instance spanning the input space: the default (budget [4,30], variety 3),
        /// a narrow budget (min==max) at a random point in [4,30], and a wide budget with a random
        /// variety target in [2,6]. Since the serialized fields are private with no setters, the
        /// non-default variants are configured via reflection and then validated (clamped) exactly as
        /// the Editor would.
        /// </summary>
        private static StageGenerationParams RandomParams(Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0:
                    return new StageGenerationParams();
                case 1:
                {
                    // Narrow budget: min == max at a random point in [4, 30].
                    int point = rng.Next(
                        StageGenerationParams.DensityBudgetFloor,
                        StageGenerationParams.DensityBudgetCeiling + 1);
                    return BuildParams(point, point, rng.Next(
                        StageGenerationParams.VarietyTargetFloor,
                        StageGenerationParams.VarietyTargetCeiling + 1));
                }
                default:
                {
                    // Wide-ish budget with random ordered bounds and a random variety target.
                    int a = rng.Next(
                        StageGenerationParams.DensityBudgetFloor,
                        StageGenerationParams.DensityBudgetCeiling + 1);
                    int b = rng.Next(
                        StageGenerationParams.DensityBudgetFloor,
                        StageGenerationParams.DensityBudgetCeiling + 1);
                    int lo = Math.Min(a, b);
                    int hi = Math.Max(a, b);
                    return BuildParams(lo, hi, rng.Next(
                        StageGenerationParams.VarietyTargetFloor,
                        StageGenerationParams.VarietyTargetCeiling + 1));
                }
            }
        }

        /// <summary>
        /// Constructs a <see cref="StageGenerationParams"/> with the given budget bounds and variety
        /// target by writing its private serialized fields via reflection, then calls
        /// <see cref="StageGenerationParams.Validate"/> so the values are clamped just like in-Editor.
        /// </summary>
        private static StageGenerationParams BuildParams(int budgetMin, int budgetMax, int varietyTarget)
        {
            var parameters = new StageGenerationParams();
            SetPrivateField(parameters, "_densityBudgetMin", budgetMin);
            SetPrivateField(parameters, "_densityBudgetMax", budgetMax);
            SetPrivateField(parameters, "_varietyTarget", varietyTarget);
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

        private static IReadOnlyList<ArchetypeId> TakeArchetypes(int count)
        {
            var list = new List<ArchetypeId>(count);
            for (int i = 0; i < count; i++)
            {
                list.Add(AllArchetypes[i]);
            }

            return list;
        }

        private static ulong RandomSeed(Random rng)
        {
            uint high = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
            uint low = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
            return ((ulong)high << 32) | low;
        }
    }
}
