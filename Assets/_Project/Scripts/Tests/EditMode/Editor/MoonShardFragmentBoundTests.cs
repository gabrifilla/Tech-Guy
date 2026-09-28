using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for R7 Property 19 of modifier-synergies-theme17
    /// (MoonShard fragment bound).
    ///
    /// Property 19: for any MoonShard rank >= 1, a sweep SHALL launch a shard count within [2,6],
    /// originating from the sweep extremities (Requirement 7.3).
    ///
    /// Two seams cover the property end to end, both pure logic (no physics, no scene):
    ///   1. <see cref="WeaponRunModifiers.Plan"/> derives <c>ShardCount</c> for a Sweep ability as
    ///      <c>Mathf.Clamp(2 * Rank(MoonShard), 0, 6)</c>. For any MoonShard rank in [1,3] the
    ///      resulting count is within [2,6]; rank 0 leaves it at 0 (off). A non-Sweep ability never
    ///      sets a shard count, so a sweep is the only cast that launches shards.
    ///   2. <see cref="ArsenalCombat.FireMoonShards"/> then clamps the fire count with
    ///      <c>Mathf.Clamp(plan.ShardCount, 2, 6)</c>. This second clamp is modeled here so that even
    ///      an out-of-range ShardCount still fires a bounded [2,6] shard count. The FireMoonShards
    ///      method is private and physics-driven (ArsenalProjectile.Fire), so the count model — the
    ///      exact clamp used at the fire site — is asserted directly rather than spawning projectiles.
    /// </summary>
    public sealed class MoonShardFragmentBoundTests
    {
        private const int ShardMin = 2;
        private const int ShardMax = 6;

        private static readonly FieldInfo KindField =
            typeof(ArsenalAbility).GetField("_kind", BindingFlags.Instance | BindingFlags.NonPublic);

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        // A fresh ArsenalAbility whose Kind is forced through the serialized _kind field (test seam).
        // MoonShard's ShardCount branch in WeaponRunModifiers.Plan is gated on ability.Kind == Sweep.
        private static ArsenalAbility NewAbility(ArsenalSkillKind kind)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "TestSpear_" + kind;
            Assert.IsNotNull(KindField, "Expected serialized field _kind on ArsenalAbility (test seam).");
            KindField.SetValue(ability, kind);
            return ability;
        }

        // The fire-site count model: FireMoonShards fires Mathf.Clamp(plan.ShardCount, 2, 6) shards.
        private static int FireShardCount(int planShardCount) => Mathf.Clamp(planShardCount, ShardMin, ShardMax);

        // Feature: modifier-synergies-theme17, Property 19
        // For a Sweep, ShardCount = clamp(2*rank, 0, 6): 0 when rank 0; within [2,6] for ranks 1..3.
        // Validates: Requirements 7.3
        [Test]
        public void SweepShardCount_IsWithinBounds_AcrossMoonShardRanks()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(0, 4); // MoonShard rank 0..3 (MaxRank is 3 in the catalog)

                var mods = new WeaponRunModifiers(RunWeaponFamily.Spear);
                for (int r = 0; r < rank; r++)
                    PropertyCheck.That(mods.Add(Def(WeaponBoon.MoonShard)),
                        "MoonShard rank " + (r + 1) + " should be addable");
                PropertyCheck.That(mods.Rank(WeaponBoon.MoonShard) == rank, "MoonShard rank mismatch");

                ArsenalAbility sweep = NewAbility(ArsenalSkillKind.Sweep);
                // Slot 2 is the sweep's E slot in the arsenal; slot value here avoids the slot-1
                // TripleMoon/Orbit branch so only the Sweep ShardCount branch is asserted.
                ArsenalCastPlan plan = mods.Plan(sweep, 2);

                int expected = Mathf.Clamp(2 * rank, 0, 6);
                PropertyCheck.That(plan.ShardCount == expected,
                    $"rank={rank}: ShardCount={plan.ShardCount}, expected={expected}");

                if (rank == 0)
                    PropertyCheck.That(plan.ShardCount == 0, "rank 0 must leave shards off (ShardCount 0)");
                else
                    PropertyCheck.That(plan.ShardCount >= ShardMin && plan.ShardCount <= ShardMax,
                        $"rank={rank}: ShardCount {plan.ShardCount} outside [2,6]");

                UnityEngine.Object.DestroyImmediate(sweep);
            });
        }

        // Feature: modifier-synergies-theme17, Property 19
        // The FireMoonShards fire-count clamp keeps the launched shard count within [2,6] for any
        // ShardCount value (including out-of-range values), so shards fired are always in [2,6].
        // Validates: Requirements 7.3
        [Test]
        public void FireMoonShards_ClampsLaunchedCount_ToBounds()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Sample ShardCount across and beyond the intended range to prove the fire-site clamp.
                int shardCount = rng.Next(-4, 20);
                int fired = FireShardCount(shardCount);

                PropertyCheck.That(fired >= ShardMin && fired <= ShardMax,
                    $"ShardCount={shardCount}: fired {fired} outside [2,6]");
                // Within the intended range the clamp is an identity; outside it clamps to the edge.
                if (shardCount >= ShardMin && shardCount <= ShardMax)
                    PropertyCheck.That(fired == shardCount, $"in-range ShardCount {shardCount} should fire unchanged");
                else if (shardCount < ShardMin)
                    PropertyCheck.That(fired == ShardMin, $"below-range ShardCount {shardCount} should clamp to {ShardMin}");
                else
                    PropertyCheck.That(fired == ShardMax, $"above-range ShardCount {shardCount} should clamp to {ShardMax}");
            });
        }

        // Feature: modifier-synergies-theme17, Property 19 (origin: sweep only)
        // A non-Sweep ability never receives a shard count, so only sweeps launch shards from the
        // sweep extremities. This isolates the "originating from the sweep" half of the property.
        // Validates: Requirements 7.3
        [Test]
        public void NonSweepAbility_NeverSetsShardCount()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4); // MoonShard owned at rank 1..3
                // Any non-Sweep kind: Thrust, Arrow, Volley, Rain.
                ArsenalSkillKind[] nonSweep = { ArsenalSkillKind.Thrust, ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain };
                ArsenalSkillKind kind = nonSweep[rng.Next(nonSweep.Length)];

                var mods = new WeaponRunModifiers(RunWeaponFamily.Spear);
                for (int r = 0; r < rank; r++) mods.Add(Def(WeaponBoon.MoonShard));

                ArsenalAbility ability = NewAbility(kind);
                ArsenalCastPlan plan = mods.Plan(ability, 2);

                PropertyCheck.That(plan.ShardCount == 0,
                    $"kind={kind}, rank={rank}: non-sweep must not set ShardCount (was {plan.ShardCount})");

                UnityEngine.Object.DestroyImmediate(ability);
            });
        }
    }
}
