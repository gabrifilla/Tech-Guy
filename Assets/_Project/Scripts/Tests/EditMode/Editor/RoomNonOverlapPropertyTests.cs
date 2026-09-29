using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.15 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 9). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class RoomNonOverlapPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 9: Não-sobreposição de salas
        // Para todo par de Rooms do mesmo RoomGraph, seus limites (AABB no plano XZ, center +/-
        // size/2) têm separação >= 0: as bordas podem se tocar (folga exatamente 0), mas nunca se
        // interpenetram. Formalmente, NÃO vale simultaneamente |cx1-cx2| < (sx1+sx2)/2 E
        // |cz1-cz2| < (sz1+sz2)/2 (desigualdade estrita — o toque, ==, é permitido). Cobre params
        // padrão e configs variadas via reflexão + Validate(), com seeds arbitrárias de 64 bits.
        // Validates: Requirements 1.4
        [Test]
        public void NoTwoRoomsInterpenetrateOnTheXZPlane()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult result = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} minCombat={parameters.MinCombatRooms} " +
                             $"maxCombat={parameters.MaxCombatRooms}";

                PropertyCheck.That(result.Success,
                    $"{ctx}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;
                PropertyCheck.That(graph != null, $"{ctx}: successful result carried a null graph.");

                var rooms = graph.Rooms;
                for (int a = 0; a < rooms.Count; a++)
                {
                    for (int b = a + 1; b < rooms.Count; b++)
                    {
                        Room ra = rooms[a];
                        Room rb = rooms[b];

                        // Distinct rooms must occupy distinct positions.
                        PropertyCheck.That(ra.Center != rb.Center,
                            $"{ctx}: rooms {ra.Id} and {rb.Id} share the same center {ra.Center}.");

                        float dx = Mathf.Abs(ra.Center.x - rb.Center.x);
                        float dz = Mathf.Abs(ra.Center.y - rb.Center.y);
                        float halfSumX = (ra.Size.x + rb.Size.x) * 0.5f;
                        float halfSumZ = (ra.Size.y + rb.Size.y) * 0.5f;

                        // Interpenetration means overlap on BOTH axes with a STRICT gap < 0.
                        // Touching (dx == halfSumX or dz == halfSumZ) has separation exactly 0
                        // and is allowed. Use a tiny epsilon so exact grid touches are not
                        // mistaken for overlap under floating-point.
                        const float epsilon = 1e-4f;
                        bool overlapX = dx < halfSumX - epsilon;
                        bool overlapZ = dz < halfSumZ - epsilon;

                        PropertyCheck.That(!(overlapX && overlapZ),
                            $"{ctx}: rooms {ra.Id} (c={ra.Center}, s={ra.Size}) and {rb.Id} " +
                            $"(c={rb.Center}, s={rb.Size}) interpenetrate " +
                            $"(dx={dx}, halfSumX={halfSumX}, dz={dz}, halfSumZ={halfSumZ}).");
                    }
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Builds a params instance spanning the input space (default plus varied combat-room
        /// bounds and special probabilities) so the generated graphs range from a single Combat
        /// room up to the 20-room ceiling. Private serialized fields are written via reflection
        /// then clamped by Validate().
        /// </summary>
        private static StageGenerationParams RandomParams(System.Random rng)
        {
            if (rng.Next(0, 4) == 0)
            {
                return new StageGenerationParams();
            }

            var parameters = new StageGenerationParams();
            int minCombat = rng.Next(1, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int maxCombat = rng.Next(minCombat, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int budgetMin = rng.Next(
                StageGenerationParams.DensityBudgetFloor, StageGenerationParams.DensityBudgetCeiling + 1);
            int budgetMax = rng.Next(budgetMin, StageGenerationParams.DensityBudgetCeiling + 1);
            int variety = rng.Next(
                StageGenerationParams.VarietyTargetFloor, StageGenerationParams.VarietyTargetCeiling + 1);
            float treasureP = RandomProbability(rng);
            float secretP = RandomProbability(rng);

            SetPrivateInt(parameters, "_minCombatRooms", minCombat);
            SetPrivateInt(parameters, "_maxCombatRooms", maxCombat);
            SetPrivateInt(parameters, "_densityBudgetMin", budgetMin);
            SetPrivateInt(parameters, "_densityBudgetMax", budgetMax);
            SetPrivateInt(parameters, "_varietyTarget", variety);
            SetPrivateFloat(parameters, "_treasureProbability", treasureP);
            SetPrivateFloat(parameters, "_secretProbability", secretP);
            parameters.Validate();
            return parameters;
        }

        private static float RandomProbability(System.Random rng)
        {
            return StageGenerationParams.SpecialProbabilityFloor +
                   (float)rng.NextDouble() *
                   (StageGenerationParams.SpecialProbabilityCeiling - StageGenerationParams.SpecialProbabilityFloor);
        }

        private static void SetPrivateInt(object target, string fieldName, int value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on StageGenerationParams.");
            field.SetValue(target, value);
        }

        private static void SetPrivateFloat(object target, string fieldName, float value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on StageGenerationParams.");
            field.SetValue(target, value);
        }

        private static ulong RandomSeed(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0:
                    return 0UL;
                case 1:
                    return ulong.MaxValue;
                default:
                    uint high = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
                    uint low = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
                    return ((ulong)high << 32) | low;
            }
        }
    }
}
