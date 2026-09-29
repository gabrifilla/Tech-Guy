using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.13 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 7). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class SpecialProbabilityBoundsPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 7: Probabilidade de especiais dentro de
        // [5%, 95%] e reprodutível
        // (a) Reprodutibilidade: a mesma Run_Seed + params produz sempre a mesma presença/ausência de
        //     Treasure e Secret (o sorteio ponderado é derivado só da seed).
        // (b) Faixa configurada: as probabilidades ficam sempre em [0.05, 0.95] (params fazem clamp).
        // Ambas verificadas por caso, sobre params e seeds variados.
        // Validates: Requirements 4.3
        [Test]
        public void SpecialAppearanceIsReproducibleAndProbabilitiesStayWithinBounds()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                // (b) The configured probabilities always fall within [0.05, 0.95] after clamping.
                PropertyCheck.That(
                    parameters.TreasureProbability >= StageGenerationParams.SpecialProbabilityFloor &&
                    parameters.TreasureProbability <= StageGenerationParams.SpecialProbabilityCeiling,
                    $"treasureP={parameters.TreasureProbability} outside [0.05, 0.95].");
                PropertyCheck.That(
                    parameters.SecretProbability >= StageGenerationParams.SpecialProbabilityFloor &&
                    parameters.SecretProbability <= StageGenerationParams.SpecialProbabilityCeiling,
                    $"secretP={parameters.SecretProbability} outside [0.05, 0.95].");

                // (a) Reproducibility: same seed + params => same presence/absence of each special.
                StageGenerationResult a = generator.Generate(parameters, seed);
                StageGenerationResult b = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} params(treasureP={parameters.TreasureProbability}, " +
                             $"secretP={parameters.SecretProbability})";

                PropertyCheck.That(a.Success && b.Success,
                    $"{ctx}: a generation failed (a={a.Success}/{a.FailureReason}, b={b.Success}/{b.FailureReason}).");

                bool treasureA = HasType(a.Graph, RoomType.Treasure);
                bool treasureB = HasType(b.Graph, RoomType.Treasure);
                bool secretA = HasType(a.Graph, RoomType.Secret);
                bool secretB = HasType(b.Graph, RoomType.Secret);

                PropertyCheck.That(treasureA == treasureB,
                    $"{ctx}: Treasure presence not reproducible (first={treasureA}, second={treasureB}).");
                PropertyCheck.That(secretA == secretB,
                    $"{ctx}: Secret presence not reproducible (first={secretA}, second={secretB}).");
            });
        }

        // Feature: procedural-stage-room-generation, Property 7: Probabilidade de especiais dentro de
        // [5%, 95%] e reprodutível
        // Verificação de distribuição: com uma probabilidade média (p=0.5) e uma amostra grande de
        // seeds distintas, cada especial aparece em ALGUMAS runs mas não em TODAS — a frequência
        // empírica fica estritamente entre 0 e o total (nem sempre-presente, nem nunca-presente).
        // Limites afrouxados propositalmente (apenas >0 e <total) para não introduzir flakiness.
        // Validates: Requirements 4.3
        [Test]
        public void MidProbabilitySpecialsAppearInSomeButNotAllRunsAcrossManySeeds()
        {
            var generator = new StageGenerator();
            StageGenerationParams parameters = BuildParams(0.5f, 0.5f);

            const int total = 256; // >= 200 distinct seeds
            int treasureCount = 0;
            int secretCount = 0;

            var rng = new Random(0x5EED); // fixed so the distribution check itself is reproducible
            for (int s = 0; s < total; s++)
            {
                uint high = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
                uint low = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
                ulong seed = ((ulong)high << 32) | low;

                StageGenerationResult result = generator.Generate(parameters, seed);
                Assert.IsTrue(result.Success, $"generation failed for seed={seed}: {result.FailureReason}");

                if (HasType(result.Graph, RoomType.Treasure)) treasureCount++;
                if (HasType(result.Graph, RoomType.Secret)) secretCount++;
            }

            // Empirical appearance frequency strictly between 0 and total for each special (p=0.5).
            Assert.Greater(treasureCount, 0,
                $"Treasure never appeared across {total} seeds at p=0.5 — the roll may be broken.");
            Assert.Less(treasureCount, total,
                $"Treasure appeared in every one of {total} seeds at p=0.5 — the roll may be broken.");
            Assert.Greater(secretCount, 0,
                $"Secret never appeared across {total} seeds at p=0.5 — the roll may be broken.");
            Assert.Less(secretCount, total,
                $"Secret appeared in every one of {total} seeds at p=0.5 — the roll may be broken.");
        }

        // ---- helpers ------------------------------------------------------------------------

        private static bool HasType(RoomGraph graph, RoomType type)
        {
            foreach (Room room in graph.Rooms)
            {
                if (room.Type == type)
                {
                    return true;
                }
            }

            return false;
        }

        private static StageGenerationParams RandomParams(Random rng)
        {
            if (rng.Next(0, 3) == 0)
            {
                return new StageGenerationParams();
            }

            // Include deliberately out-of-range authored probabilities so the clamp guarantee (b) is
            // actually exercised, not just the already-valid default range.
            float treasureP = (float)(rng.NextDouble() * 1.4 - 0.2); // spans [-0.2, 1.2]
            float secretP = (float)(rng.NextDouble() * 1.4 - 0.2);
            int minCombat = rng.Next(1, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int maxCombat = rng.Next(minCombat, StageGenerationParams.MaxCombatRoomCeiling + 1);

            var parameters = new StageGenerationParams();
            SetPrivateInt(parameters, "_minCombatRooms", minCombat);
            SetPrivateInt(parameters, "_maxCombatRooms", maxCombat);
            SetPrivateFloat(parameters, "_treasureProbability", treasureP);
            SetPrivateFloat(parameters, "_secretProbability", secretP);
            parameters.Validate();
            return parameters;
        }

        private static StageGenerationParams BuildParams(float treasureP, float secretP)
        {
            var parameters = new StageGenerationParams();
            SetPrivateFloat(parameters, "_treasureProbability", treasureP);
            SetPrivateFloat(parameters, "_secretProbability", secretP);
            parameters.Validate();
            return parameters;
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
