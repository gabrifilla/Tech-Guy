using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.19 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 18). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class GenerationIsolatedFromGlobalRngPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 18: Aleatoriedade isolada da RNG global
        // Para todo Run_Seed e StageGenerationParams, alterar a semente da RNG global do Unity
        // (UnityEngine.Random.InitState) ENTRE duas chamadas de Generate com a MESMA Run_Seed NÃO
        // pode alterar o RoomGraph resultante: o núcleo deriva toda a aleatoriedade de uma SeededRng
        // isolada, semeada só pela Run_Seed, e nunca consome UnityEngine.Random (R2.5). Cobre params
        // padrão e configs variadas via reflexão + Validate(), com seeds arbitrárias de 64 bits e
        // duas sementes globais distintas.
        // Validates: Requirements 2.5
        [Test]
        public void ChangingGlobalRandomSeedDoesNotChangeTheGraph()
        {
            var generator = new StageGenerator();
            UnityEngine.Random.State savedState = UnityEngine.Random.state;

            try
            {
                PropertyCheck.ForAll((rng, i) =>
                {
                    StageGenerationParams parameters = RandomParams(rng);
                    ulong seed = RandomSeed(rng);

                    // Two DIFFERENT global RNG seeds surrounding the two generations.
                    int globalA = rng.Next(int.MinValue, int.MaxValue);
                    int globalB;
                    do
                    {
                        globalB = rng.Next(int.MinValue, int.MaxValue);
                    }
                    while (globalB == globalA);

                    UnityEngine.Random.InitState(globalA);
                    // Draw from the global stream so its state genuinely differs run-to-run;
                    // an isolated core must ignore this entirely.
                    _ = UnityEngine.Random.value;
                    StageGenerationResult g1 = generator.Generate(parameters, seed);

                    UnityEngine.Random.InitState(globalB);
                    _ = UnityEngine.Random.value;
                    StageGenerationResult g2 = generator.Generate(parameters, seed);

                    string ctx = $"seed={seed} globalA={globalA} globalB={globalB} " +
                                 $"minCombat={parameters.MinCombatRooms} maxCombat={parameters.MaxCombatRooms}";

                    PropertyCheck.That(g1.Success && g2.Success,
                        $"{ctx}: a generation failed unexpectedly " +
                        $"(g1={g1.FailureReason}, g2={g2.FailureReason}).");
                    PropertyCheck.That(g1.Graph != null && g2.Graph != null,
                        $"{ctx}: a successful result carried a null graph.");

                    // Same Run_Seed => identical graph regardless of the global RNG seed (R2.5).
                    PropertyCheck.That(g1.Graph.StructurallyEquals(g2.Graph),
                        $"{ctx}: the global RNG seed leaked into generation — graphs diverged " +
                        $"(rooms1={g1.Graph.Rooms.Count}, rooms2={g2.Graph.Rooms.Count}, " +
                        $"conns1={g1.Graph.Connections.Count}, conns2={g2.Graph.Connections.Count}).");
                });
            }
            finally
            {
                // Restore the global RNG state so the test leaves no side effect.
                UnityEngine.Random.state = savedState;
            }
        }

        // ---- generators ---------------------------------------------------------------------

        private static StageGenerationParams RandomParams(Random rng)
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

        private static float RandomProbability(Random rng)
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

        private static ulong RandomSeed(Random rng)
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
