using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for R5 Property 16 of modifier-synergies-theme17
    /// (TripleMoon + Orbit orbital core, <see cref="WeaponRunModifiers.Plan"/> spear slot 1).
    ///
    /// This is pure plan-generation logic: no physics, no scene. For any TripleMoon rank
    /// tm in [0,3] and Orbit rank o in [0,3] the slot-1 cast plan must satisfy
    ///   Hits  = base + 2*tm
    ///   Width = base * (1 + 0.25*o)
    ///   Travel = (o > 0)
    /// and generating the plan any number of times must never mutate the source
    /// <see cref="ArsenalAbility"/> (the ScriptableObject the plan snapshots from).
    ///
    /// The source-asset isolation is checked by snapshotting the ability's getters
    /// before and after repeated plan generation (R5.5).
    /// </summary>
    public sealed class SpearPlanCorrectnessAndIsolationTests
    {
        private const int Slot1 = 1; // spear W ability lives in slot 1 (see ArsenalCombat.Use)

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        // A fresh ArsenalAbility with default serialized values (Width = 1, Hits = 1). The R5
        // slot-1 branch in WeaponRunModifiers.Plan is gated on the slot index, not on ability.Kind,
        // so the default Kind is used deliberately: it isolates the TripleMoon/Orbit branch and
        // keeps the Sweep-only MoonShard/ShardCount branch out of the assertion surface. Never
        // mutated by Plan.
        private static ArsenalAbility NewSpearSlotAbility()
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "TestSpearSlot1";
            return ability;
        }

        // Feature: modifier-synergies-theme17, Property 16
        // Hits = base + 2*tm, Width = base*(1 + 0.25*o), Travel = (o > 0), for tm,o in [0,3].
        // Validates: Requirements 5.1, 5.2, 5.3
        [Test]
        public void SpearSlot1Plan_MatchesTripleMoonAndOrbitFormula()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int tm = rng.Next(0, 4); // TripleMoon rank 0..3
                int o = rng.Next(0, 4);  // Orbit rank 0..3

                var mods = new WeaponRunModifiers(RunWeaponFamily.Spear);
                for (int r = 0; r < tm; r++) PropertyCheck.That(mods.Add(Def(WeaponBoon.TripleMoon)), "TripleMoon rank " + (r + 1) + " should be addable");
                for (int r = 0; r < o; r++) PropertyCheck.That(mods.Add(Def(WeaponBoon.Orbit)), "Orbit rank " + (r + 1) + " should be addable");

                PropertyCheck.That(mods.Rank(WeaponBoon.TripleMoon) == tm, "TripleMoon rank mismatch");
                PropertyCheck.That(mods.Rank(WeaponBoon.Orbit) == o, "Orbit rank mismatch");

                ArsenalAbility ability = NewSpearSlotAbility();
                int baseHits = ability.Hits;
                float baseWidth = ability.Width;

                ArsenalCastPlan plan = mods.Plan(ability, Slot1);

                int expectedHits = baseHits + 2 * tm;
                float expectedWidth = baseWidth * (1f + 0.25f * o);
                bool expectedTravel = o > 0;

                PropertyCheck.That(plan.Hits == expectedHits,
                    $"tm={tm}, o={o}: Hits={plan.Hits}, expected={expectedHits}");
                PropertyCheck.That(Mathf.Abs(plan.Width - expectedWidth) <= 1e-4f * Mathf.Max(1f, expectedWidth),
                    $"tm={tm}, o={o}: Width={plan.Width}, expected={expectedWidth}");
                PropertyCheck.That(plan.Travel == expectedTravel,
                    $"tm={tm}, o={o}: Travel={plan.Travel}, expected={expectedTravel}");

                UnityEngine.Object.DestroyImmediate(ability);
            });
        }

        // Feature: modifier-synergies-theme17, Property 16 (isolation half)
        // Repeated plan generation leaves the source ArsenalAbility getters unchanged (R5.5):
        // the plan is a fresh per-cast snapshot, never assigned back to the source asset.
        // Validates: Requirements 5.5
        [Test]
        public void SpearSlot1Plan_LeavesSourceAbilityUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int tm = rng.Next(0, 4);
                int o = rng.Next(0, 4);
                int repeats = rng.Next(1, 8); // generate the plan multiple times

                var mods = new WeaponRunModifiers(RunWeaponFamily.Spear);
                for (int r = 0; r < tm; r++) mods.Add(Def(WeaponBoon.TripleMoon));
                for (int r = 0; r < o; r++) mods.Add(Def(WeaponBoon.Orbit));

                ArsenalAbility ability = NewSpearSlotAbility();

                // Snapshot every getter the plan reads before any generation.
                ArsenalSkillKind kind0 = ability.Kind;
                float windup0 = ability.Windup, interval0 = ability.Interval, range0 = ability.Range;
                float width0 = ability.Width, damage0 = ability.DamageMultiplier;
                int hits0 = ability.Hits;
                bool piercing0 = ability.Piercing;

                for (int r = 0; r < repeats; r++) mods.Plan(ability, Slot1);

                PropertyCheck.That(ability.Kind == kind0, "Kind mutated on source ability");
                PropertyCheck.That(ability.Windup == windup0, "Windup mutated on source ability");
                PropertyCheck.That(ability.Interval == interval0, "Interval mutated on source ability");
                PropertyCheck.That(ability.Range == range0, "Range mutated on source ability");
                PropertyCheck.That(ability.Width == width0, $"Width mutated on source ability: {width0} -> {ability.Width}");
                PropertyCheck.That(ability.DamageMultiplier == damage0, "DamageMultiplier mutated on source ability");
                PropertyCheck.That(ability.Hits == hits0, $"Hits mutated on source ability: {hits0} -> {ability.Hits}");
                PropertyCheck.That(ability.Piercing == piercing0, "Piercing mutated on source ability");

                UnityEngine.Object.DestroyImmediate(ability);
            });
        }
    }
}
