using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for re-entry idempotence — task 8.6 of
    /// procedural-stage-room-generation (Property 16, Requirements 3.3).
    ///
    /// <para>
    /// Design note on scope. The full re-entry flow lives in <c>ProgressionDirector</c>, a
    /// MonoBehaviour that drives coroutines, NavMesh sampling and scene spawns; exercising it
    /// deterministically in EditMode would require a live scene and would be flaky. Its entry
    /// guard is also private (<c>TryEnterNearbyCombatRoom</c>/<c>EnterCombatRoom</c> skip any
    /// room where <c>Visited || Cleared || _aliveByRoom.ContainsKey(id)</c>).
    /// </para>
    /// <para>
    /// The idempotence invariant of R3.3 is a property of the <see cref="Room"/> model, so it is
    /// tested there where it is deterministic and dependency-free: once a Combat_Room is marked
    /// <see cref="Room.Cleared"/>, (a) its structural/composition data (<see cref="Room.Id"/>,
    /// <see cref="Room.Type"/>, <see cref="Room.Composition"/> reference) is unchanged — runtime
    /// flags are separate from structural data — and (b) the re-entry eligibility predicate the
    /// director uses (<c>Type == Combat &amp;&amp; !Visited &amp;&amp; !Cleared</c>) evaluates to
    /// false, so the composition would never be rebuilt on re-entry. We also confirm re-generating
    /// with the same seed after toggling runtime flags yields a structurally equal graph, i.e.
    /// runtime flags do not feed back into generation.
    /// </para>
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene. The agreed seeded harness <see cref="PropertyCheck"/> drives
    /// &gt;= 100 deterministic generated cases and reports the exact failing case as a
    /// counterexample.
    /// </summary>
    public sealed class ReentryIdempotencePropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 16: Idempotência de reentrada
        // Para toda Combat_Room já marcada como limpa, reentrar nela mantém Cleared verdadeiro e
        // não reinstancia sua Room_Composition. Modelado no nível do modelo Room: alternar os flags
        // de runtime (Visited/Cleared) nunca altera dados estruturais/composição (Id/Type/Composition),
        // e o predicado de elegibilidade de reentrada do diretor
        // (Type == Combat && !Visited && !Cleared) torna-se false após limpar — a composição não é
        // reconstruída na reentrada.
        // Validates: Requirements 3.3
        [Test]
        public void ClearedCombatRoomIsNeverReactivatedAndKeepsCompositionOnReentry()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult result = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} params(minCombat={parameters.MinCombatRooms}, " +
                             $"maxCombat={parameters.MaxCombatRooms}, treasureP={parameters.TreasureProbability}, " +
                             $"secretP={parameters.SecretProbability})";

                PropertyCheck.That(result.Success,
                    $"{ctx}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;
                PropertyCheck.That(graph != null, $"{ctx}: successful result carried a null graph.");

                int combatRooms = 0;
                foreach (Room room in graph.Rooms)
                {
                    if (room.Type != RoomType.Combat)
                    {
                        continue;
                    }

                    combatRooms++;

                    // Capture structural/composition identity BEFORE toggling runtime flags.
                    int idBefore = room.Id;
                    RoomType typeBefore = room.Type;
                    RoomComposition compositionBefore = room.Composition;

                    // An unvisited, uncleared Combat_Room is eligible for entry/activation
                    // (the same predicate EnterCombatRoom uses before building the composition).
                    PropertyCheck.That(IsReentryEligible(room),
                        $"{ctx}: room {room.Id} was not eligible for its FIRST activation " +
                        $"(Visited={room.Visited}, Cleared={room.Cleared}).");

                    // Simulate resolving the room: the player entered and cleared it.
                    room.Visited = true;
                    room.Cleared = true;

                    // R3.3: re-entering a cleared room must keep it cleared and must NOT rebuild the
                    // composition. The director's eligibility predicate must now be false so no
                    // activation happens on re-entry.
                    PropertyCheck.That(room.Cleared,
                        $"{ctx}: room {room.Id} lost its Cleared flag after being marked cleared.");
                    PropertyCheck.That(!IsReentryEligible(room),
                        $"{ctx}: cleared room {room.Id} is still eligible for re-activation " +
                        $"(Visited={room.Visited}, Cleared={room.Cleared}) — composition would be rebuilt.");

                    // Runtime flags are separate from structural/composition data: toggling them
                    // does not touch Id/Type/Composition (idempotent state).
                    PropertyCheck.That(room.Id == idBefore,
                        $"{ctx}: room Id changed after toggling runtime flags ({idBefore} -> {room.Id}).");
                    PropertyCheck.That(room.Type == typeBefore,
                        $"{ctx}: room {room.Id} Type changed after toggling runtime flags " +
                        $"({typeBefore} -> {room.Type}).");
                    PropertyCheck.That(ReferenceEquals(room.Composition, compositionBefore),
                        $"{ctx}: room {room.Id} Composition reference changed after toggling runtime flags " +
                        "— composition was re-instantiated, violating idempotence.");

                    // A second toggle (repeated re-entry) is still a no-op on structure and stays cleared.
                    room.Visited = true;
                    room.Cleared = true;
                    PropertyCheck.That(room.Cleared && !IsReentryEligible(room)
                                       && ReferenceEquals(room.Composition, compositionBefore),
                        $"{ctx}: repeated re-entry of room {room.Id} was not idempotent.");
                }

                // At least one Combat_Room must exist to have exercised the property (R1.1).
                PropertyCheck.That(combatRooms >= 1,
                    $"{ctx}: expected at least one Combat_Room, found {combatRooms}.");

                // Runtime flags must not feed back into generation: regenerating with the same seed
                // after toggling flags on the first graph still yields a structurally equal graph.
                StageGenerationResult regen = generator.Generate(parameters, seed);
                PropertyCheck.That(regen.Success && regen.Graph != null,
                    $"{ctx}: regeneration with the same seed failed ({regen.FailureReason}).");
                PropertyCheck.That(regen.Graph.StructurallyEquals(graph),
                    $"{ctx}: regeneration diverged after runtime flags were toggled — " +
                    "runtime state leaked into generation.");
            });
        }

        /// <summary>
        /// The re-entry eligibility predicate the ProgressionDirector applies before activating a
        /// Combat_Room: only an unvisited, uncleared Combat_Room is activated (composition built).
        /// </summary>
        private static bool IsReentryEligible(Room room)
        {
            return room.Type == RoomType.Combat && !room.Visited && !room.Cleared;
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Builds a params instance spanning the input space: the default, plus configs with varied
        /// combat-room bounds and special probabilities. Private serialized fields are written via
        /// reflection then clamped by Validate(), exactly as the Editor would.
        /// </summary>
        private static StageGenerationParams RandomParams(Random rng)
        {
            if (rng.Next(0, 3) == 0)
            {
                return new StageGenerationParams();
            }

            var parameters = new StageGenerationParams();
            int minCombat = rng.Next(1, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int maxCombat = rng.Next(minCombat, StageGenerationParams.MaxCombatRoomCeiling + 1);
            float treasureP = RandomProbability(rng);
            float secretP = RandomProbability(rng);

            SetPrivateInt(parameters, "_minCombatRooms", minCombat);
            SetPrivateInt(parameters, "_maxCombatRooms", maxCombat);
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
