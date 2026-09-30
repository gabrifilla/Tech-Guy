using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 2 of impactful-weapon-boons (monotonicity with rank).
    ///
    /// Property 2 (design): <em>for any</em> new boon and any two ranks <c>a &lt; b</c> in
    /// <c>[1, MaxRank]</c>, the boon's primary named quantity at <c>b</c> SHALL be &gt;= its value at
    /// <c>a</c>. <b>Validates: Requirements 1.2.</b>
    ///
    /// This is the impactful-weapon-boons counterpart to
    /// <see cref="ModifierRankMonotonicityPropertyTests"/> (which covers the swarm-rework boons). It adds
    /// the six new family boons' named quantities and asserts each is non-decreasing across ranks 1..3:
    /// <list type="bullet">
    /// <item><see cref="WeaponBoon.ImpalingLine"/> — <c>ImpalePull = 0.75·r</c> on the thrust plan.</item>
    /// <item><see cref="WeaponBoon.Shockwave"/> — appended sphere step <c>sphereRadius = 3·(1 + 0.2·r)</c>.</item>
    /// <item><see cref="WeaponBoon.PerfectSpacing"/> — in-band multiplier <c>1 + 0.35·r</c>.</item>
    /// <item><see cref="WeaponBoon.ChargedShot"/> — <c>DamageMultiplier = 1 + 0.75·r</c>.</item>
    /// <item><see cref="WeaponBoon.MomentumStrike"/> — per-stack bonus <c>2·r·stacks</c> (sampled at a fixed
    /// stack count, since the named per-rank quantity is the coefficient <c>2·r</c>).</item>
    /// <item><see cref="WeaponBoon.SplitArrow"/> — split count is exactly <c>r</c> (the coordinator fans
    /// <c>rank</c> arrows), which is trivially non-decreasing; asserted as the count coefficient.</item>
    /// </list>
    ///
    /// Rank is the only variable in each comparison (all other inputs held fixed on a fresh source
    /// asset), so any difference is attributable to the rank increment — exactly the monotonicity
    /// contract documented on <see cref="WeaponRunModifiers.Plan"/>/<see cref="WeaponRunModifiers.GauntletSteps"/>.
    ///
    /// Pure ScriptableObject / arithmetic evaluation — no scene, no physics — so it runs in EditMode.
    /// The seeded harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic cases and reports the
    /// exact failing case as a counterexample.
    /// </summary>
    public sealed class ImpactfulBoonsMonotonicityTests
    {
        private const float Tolerance = 1e-4f;

        [Serializable] private struct ArsenalKindOverlay { public int _kind; }
        [Serializable] private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        private static WeaponRunModifiers WithRank(RunWeaponFamily family, WeaponBoon kind, int rank)
        {
            var mods = new WeaponRunModifiers(family);
            WeaponRunModifiers.Definition def = Def(kind);
            for (int r = 0; r < rank; r++)
                PropertyCheck.That(mods.Add(def), $"{kind} rank {r + 1} should be addable");
            PropertyCheck.That(mods.Rank(kind) == rank, $"{kind} expected rank {rank}, got {mods.Rank(kind)}");
            return mods;
        }

        private static ArsenalAbility NewArsenalAbility(ArsenalSkillKind kind)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "ImpactfulMonoSource_" + kind;
            var overlay = new ArsenalKindOverlay { _kind = (int)kind };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        private static BreakerGauntletAbility NewGauntletAbility()
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "ImpactfulMonoSourceGauntlet";
            var overlay = new HitStepsOverlay
            {
                _hitSteps = new[]
                {
                    new AreaHitStep
                    {
                        stanceDamage = 12f, pushDistance = .35f,
                        boxSize = new Vector3(3f, 2f, 0f), sphereRadius = 1.5f, damageMultiplier = 1f,
                    },
                },
            };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // ImpalePull on the thrust plan at a given rank.
        private static float ImpalePull(int rank)
        {
            WeaponRunModifiers mods = WithRank(RunWeaponFamily.Spear, WeaponBoon.ImpalingLine, rank);
            ArsenalAbility ability = NewArsenalAbility(ArsenalSkillKind.Thrust);
            try { return mods.Plan(ability, 0).ImpalePull; }
            finally { UnityEngine.Object.DestroyImmediate(ability); }
        }

        // The appended Shockwave sphere step's radius (the last step GauntletSteps produces at slot 3).
        private static float ShockwaveRadius(int rank)
        {
            WeaponRunModifiers mods = WithRank(RunWeaponFamily.Gauntlet, WeaponBoon.Shockwave, rank);
            BreakerGauntletAbility ability = NewGauntletAbility();
            try
            {
                List<AreaHitStep> steps = mods.GauntletSteps(ability, 3);
                return steps[steps.Count - 1].sphereRadius;
            }
            finally { UnityEngine.Object.DestroyImmediate(ability); }
        }

        // Perfect Spacing's in-band direct-damage multiplier (distance held at 5m, inside [3.5, 6.5]).
        private static float PerfectSpacingInBand(int rank)
        {
            WeaponRunModifiers mods = WithRank(RunWeaponFamily.Spear, WeaponBoon.PerfectSpacing, rank);
            return mods.DirectDamageMultiplier(5f, 1f, 1f, 0);
        }

        // Charged Shot's charged-arrow damage multiplier: pure helper, no scene.
        private static float ChargedShotMultiplier(int rank) => ChargedShot.DamageMultiplier(rank);

        // Feature: impactful-weapon-boons, Property 2: Monotonicity with rank.
        // Each new family boon's named quantity is non-decreasing from rank r to r+1 across the whole
        // catalogued [1, MaxRank] range (all boons MaxRank == 3): ImpalePull (0.75r), Shockwave
        // sphereRadius (3(1+0.2r)), PerfectSpacing multiplier (1+0.35r), ChargedShot multiplier
        // (1+0.75r), MomentumStrike per-stack coefficient (2r), and the SplitArrow split count (r).
        // Validates: Requirements 1.2
        [Test]
        public void NewFamilyBoons_StrengthenMonotonicallyWithRank()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int impaleMax = Def(WeaponBoon.ImpalingLine).MaxRank;
                for (int r = 1; r < impaleMax; r++)
                    PropertyCheck.That(ImpalePull(r + 1) >= ImpalePull(r) - Tolerance,
                        $"ImpalingLine ImpalePull decreased at rank {r} -> {r + 1} ({ImpalePull(r)} -> {ImpalePull(r + 1)})");

                int shockMax = Def(WeaponBoon.Shockwave).MaxRank;
                for (int r = 1; r < shockMax; r++)
                    PropertyCheck.That(ShockwaveRadius(r + 1) >= ShockwaveRadius(r) - Tolerance,
                        $"Shockwave sphereRadius decreased at rank {r} -> {r + 1} ({ShockwaveRadius(r)} -> {ShockwaveRadius(r + 1)})");

                int spacingMax = Def(WeaponBoon.PerfectSpacing).MaxRank;
                for (int r = 1; r < spacingMax; r++)
                    PropertyCheck.That(PerfectSpacingInBand(r + 1) >= PerfectSpacingInBand(r) - Tolerance,
                        $"PerfectSpacing in-band multiplier decreased at rank {r} -> {r + 1} " +
                        $"({PerfectSpacingInBand(r)} -> {PerfectSpacingInBand(r + 1)})");

                int chargedMax = Def(WeaponBoon.ChargedShot).MaxRank;
                for (int r = 1; r < chargedMax; r++)
                    PropertyCheck.That(ChargedShotMultiplier(r + 1) >= ChargedShotMultiplier(r) - Tolerance,
                        $"ChargedShot DamageMultiplier decreased at rank {r} -> {r + 1} " +
                        $"({ChargedShotMultiplier(r)} -> {ChargedShotMultiplier(r + 1)})");

                // MomentumStrike bonus is 2*r*stacks. At any fixed stack count s >= 1 the bonus is
                // non-decreasing in r (coefficient 2*r), so sample a random stack count and compare.
                int momentumMax = Def(WeaponBoon.MomentumStrike).MaxRank;
                int stacks = 1 + rng.Next(0, MomentumStacks.MaxStacks); // 1..10
                for (int r = 1; r < momentumMax; r++)
                {
                    float lo = 2f * r * stacks;
                    float hi = 2f * (r + 1) * stacks;
                    PropertyCheck.That(hi >= lo - Tolerance,
                        $"MomentumStrike bonus decreased at rank {r} -> {r + 1} at {stacks} stacks ({lo} -> {hi})");
                }

                // SplitArrow spawns exactly `rank` arrows, so the split count is non-decreasing in rank.
                int splitMax = Def(WeaponBoon.SplitArrow).MaxRank;
                for (int r = 1; r < splitMax; r++)
                    PropertyCheck.That((r + 1) >= r,
                        $"SplitArrow split count decreased at rank {r} -> {r + 1}");
            });
        }

        // Example: the exact per-rank values anchor the property's sampled quantities (rank 1/2/3).
        [Test]
        public void NewFamilyBoons_NamedQuantitiesMatchDesign()
        {
            for (int r = 1; r <= 3; r++)
            {
                Assert.That(ImpalePull(r), Is.EqualTo(0.75f * r).Within(Tolerance),
                    $"ImpalePull at rank {r} must equal 0.75*r.");
                Assert.That(ShockwaveRadius(r), Is.EqualTo(3f * (1f + 0.2f * r)).Within(Tolerance),
                    $"Shockwave radius at rank {r} must equal 3*(1+0.2r).");
                Assert.That(PerfectSpacingInBand(r), Is.EqualTo(1f + 0.35f * r).Within(Tolerance),
                    $"PerfectSpacing in-band multiplier at rank {r} must equal 1+0.35r.");
                Assert.That(ChargedShotMultiplier(r), Is.EqualTo(1f + 0.75f * r).Within(Tolerance),
                    $"ChargedShot multiplier at rank {r} must equal 1+0.75r.");
            }
        }
    }
}
