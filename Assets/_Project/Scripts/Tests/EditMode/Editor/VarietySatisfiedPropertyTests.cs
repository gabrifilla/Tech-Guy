using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the archetype-variety guarantee produced by the pure
    /// <see cref="RoomCompositionPlanner"/> — task 3.5 of procedural-stage-room-generation.
    ///
    /// The variety selection (R5.3) is pure C# with no scene dependency, so it can be
    /// property-checked without a live Unity scene (Property 14). FsCheck/CsCheck cannot be resolved
    /// on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class VarietySatisfiedPropertyTests
    {
        private static readonly ArchetypeId[] AllArchetypes =
            (ArchetypeId[])Enum.GetValues(typeof(ArchetypeId));

        // Feature: procedural-stage-room-generation, Property 14: Variedade satisfeita
        // Para toda seed, DistinctArchetypes da composição do Plan é >= min(VarietyTarget,
        // disponibilidade) — limitado pela densidade quando esta é menor que a variedade alvo. Quando a
        // disponibilidade é menor que o VarietyTarget, distinct == disponibilidade (capado pela
        // disponibilidade). Alimenta contagens de arquétipos disponíveis variadas, incluindo menos que
        // o VarietyTarget e zero, e configs de variedade variadas (via reflexão sobre os campos
        // serializados privados).
        // Validates: Requirements 5.3
        [Test]
        public void DistinctArchetypesSatisfiesVarietyTargetCappedByAvailability()
        {
            var planner = new RoomCompositionPlanner();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                int varietyTarget = parameters.VarietyTarget;

                // Availability spans below the variety target, exactly at it, above it, and zero.
                int availableCount = rng.Next(0, AllArchetypes.Length + 1);
                IReadOnlyList<ArchetypeId> available = TakeArchetypes(availableCount);

                int maxDepth = rng.Next(0, 25);
                int depth = rng.Next(0, maxDepth + 6);

                var planRng = new SeededRng(RandomSeed(rng));
                RoomComposition composition = planner.Plan(parameters, available, depth, maxDepth, planRng);

                int distinct = composition.DistinctArchetypes;
                int targetDensity = composition.TargetDensity;

                string ctx =
                    $"varietyTarget={varietyTarget} available={availableCount} depth={depth} " +
                    $"maxDepth={maxDepth} targetDensity={targetDensity} distinct={distinct} " +
                    $"slots={composition.Slots.Count}";

                // DistinctArchetypes counts unique archetypes; the slot set must have no duplicates.
                PropertyCheck.That(distinct == composition.Slots.Count,
                    $"{ctx}: duplicate archetypes across slots (DistinctArchetypes != slot count).");

                if (availableCount == 0)
                {
                    // No archetypes available -> empty composition, zero distinct.
                    PropertyCheck.That(distinct == 0,
                        $"{ctx}: expected zero distinct archetypes when none are available.");
                    return;
                }

                // Distinct never exceeds availability (cannot invent archetypes) and never exceeds
                // the target density (each chosen archetype must receive at least one enemy).
                PropertyCheck.That(distinct <= availableCount,
                    $"{ctx}: distinct archetypes exceeds availability.");
                PropertyCheck.That(distinct <= targetDensity,
                    $"{ctx}: distinct archetypes exceeds the target density.");

                // The variety guarantee (R5.3): distinct reaches the Variety_Target, capped by
                // availability, and further capped by the target density when density < variety.
                int expected = Math.Min(varietyTarget, availableCount);
                expected = Math.Min(expected, targetDensity);
                if (expected < 1)
                {
                    expected = 1;
                }

                PropertyCheck.That(distinct == expected,
                    $"{ctx}: distinct={distinct} but expected min(varietyTarget, availability, targetDensity)={expected}.");

                // When availability is below the variety target, distinct is capped exactly at
                // availability (as long as the density allows placing one of each).
                if (availableCount < varietyTarget && targetDensity >= availableCount)
                {
                    PropertyCheck.That(distinct == availableCount,
                        $"{ctx}: availability below variety target but distinct != availability.");
                }
                else if (targetDensity >= varietyTarget)
                {
                    // Enough availability and density -> the full variety target is met.
                    PropertyCheck.That(distinct >= Math.Min(varietyTarget, availableCount),
                        $"{ctx}: distinct did not reach min(varietyTarget, availability).");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Builds a params instance spanning the variety-target space: the default (variety 3) and
        /// instances with a random variety target in [2, 6] paired with a random budget. Private
        /// serialized fields are written via reflection then validated (clamped) as the Editor would.
        /// </summary>
        private static StageGenerationParams RandomParams(Random rng)
        {
            if (rng.Next(0, 3) == 0)
            {
                return new StageGenerationParams();
            }

            int variety = rng.Next(
                StageGenerationParams.VarietyTargetFloor,
                StageGenerationParams.VarietyTargetCeiling + 1);

            int a = rng.Next(
                StageGenerationParams.DensityBudgetFloor,
                StageGenerationParams.DensityBudgetCeiling + 1);
            int b = rng.Next(
                StageGenerationParams.DensityBudgetFloor,
                StageGenerationParams.DensityBudgetCeiling + 1);

            return BuildParams(Math.Min(a, b), Math.Max(a, b), variety);
        }

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
