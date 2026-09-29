using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 38 of weapon-gameplay-swarm-rework
    /// (monotonicity of run modifiers by rank, <see cref="WeaponRunModifiers.Plan"/> /
    /// <see cref="WeaponRunModifiers.GauntletSteps"/>).
    ///
    /// Property 38 (design): for every catalogued modifier and every Cast_Plan quantity it affects,
    /// the resulting value is monotonically non-decreasing as the rank grows from 1 to the catalogued
    /// maximum — additive counts (Hits, Arrows, sweeps, echoes) and size/damage/wave multipliers strengthen,
    /// while a cadence quantity (Interval) is monotonically NON-INCREASING (smaller interval = faster fire).
    ///
    /// Approach (documented per task 15.4): this is pure per-cast snapshot generation — no scene, no physics.
    /// For each catalogued monotone boon the test compares the snapshot produced at rank r against the snapshot
    /// at rank r+1 (for r in 1..MaxRank-1), holding every other input fixed (same fresh source asset, same slot,
    /// only the boon's own rank incremented via the data-authored <see cref="WeaponRunModifiers.Add"/> entry
    /// point). It then asserts the affected named quantity moved in the strengthening direction. Rank is the
    /// only variable, so any difference is attributable to the rank increment, which is exactly the monotonicity
    /// contract documented on Plan/GauntletSteps. The named quantities exercised are those the design lists:
    /// Hits (RapidBurst/LongRain/EchoThrust/TripleMoon), Arrows (WideVolley), Width (LongRain/Orbit + MeleeScale
    /// LongReach/LongFists), Damage (HeavyBolt), Interval faster (RapidBurst), WaveMultiplier (DragonWave),
    /// ShardCount (MoonShard), stanceDamage/pushDistance (StanceCrusher), sphereRadius (ShockRing) and the
    /// gauntlet echo count (FlurryEcho/AsuraEcho).
    /// </summary>
    public sealed class ModifierRankMonotonicityPropertyTests
    {
        private const float Tolerance = 1e-4f;

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        // Fresh source ArsenalAbility with default serialized values; the _kind serialized field is
        // overwritten through a JSON overlay so the Kind-gated spear branches (Thrust/Sweep) are reachable
        // without ever mutating the asset through a reward path (same technique as AssetIsolationTests).
        private static ArsenalAbility NewArsenalAbility(ArsenalSkillKind kind)
        {
            ArsenalAbility ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "MonotonicitySource_" + kind;
            var overlay = new ArsenalKindOverlay { _kind = (int)kind };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        [Serializable]
        private struct ArsenalKindOverlay { public int _kind; }

        // Fresh source BreakerGauntletAbility carrying a single authored AreaHitStep so GauntletSteps has a
        // step to clone/scale and a "last" step to append echoes onto. _hitSteps is private serialized, set
        // via JSON overlay so the source asset is only ever built here, never mutated by the reward path.
        private static BreakerGauntletAbility NewGauntletAbility()
        {
            BreakerGauntletAbility ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "MonotonicitySourceGauntlet";
            var overlay = new GauntletStepsOverlay
            {
                _hitSteps = new[]
                {
                    new AreaHitStep
                    {
                        stanceDamage = 12f,
                        pushDistance = 0.35f,
                        boxSize = new Vector3(3f, 2f, 0f),
                        sphereRadius = 1.5f,
                        damageMultiplier = 1f,
                    },
                },
            };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        [Serializable]
        private struct GauntletStepsOverlay { public AreaHitStep[] _hitSteps; }

        // Build a WeaponRunModifiers with exactly `rank` copies of `kind` added through the data entry point.
        private static WeaponRunModifiers WithRank(RunWeaponFamily family, WeaponBoon kind, int rank)
        {
            var mods = new WeaponRunModifiers(family);
            WeaponRunModifiers.Definition def = Def(kind);
            for (int r = 0; r < rank; r++)
                PropertyCheck.That(mods.Add(def), $"{kind} rank {r + 1} should be addable");
            PropertyCheck.That(mods.Rank(kind) == rank, $"{kind} expected rank {rank}, got {mods.Rank(kind)}");
            return mods;
        }

        // Reads a single float quantity from the plan produced for a fresh source ability at the given rank.
        private static float PlanFloat(RunWeaponFamily family, WeaponBoon kind, int rank,
            ArsenalSkillKind abilityKind, int slot, Func<ArsenalCastPlan, float> quantity)
        {
            WeaponRunModifiers mods = WithRank(family, kind, rank);
            ArsenalAbility ability = NewArsenalAbility(abilityKind);
            try { return quantity(mods.Plan(ability, slot)); }
            finally { UnityEngine.Object.DestroyImmediate(ability); }
        }

        // Asserts a plan quantity is non-decreasing from rank r to r+1 across the whole catalogued range.
        private static void AssertPlanNonDecreasing(WeaponBoon kind, RunWeaponFamily family,
            ArsenalSkillKind abilityKind, int slot, Func<ArsenalCastPlan, float> quantity, string label)
        {
            int maxRank = Def(kind).MaxRank;
            for (int r = 1; r < maxRank; r++)
            {
                float lo = PlanFloat(family, kind, r, abilityKind, slot, quantity);
                float hi = PlanFloat(family, kind, r + 1, abilityKind, slot, quantity);
                PropertyCheck.That(hi >= lo - Tolerance,
                    $"{kind} {label}: rank {r} -> {r + 1} decreased ({lo} -> {hi})");
            }
        }

        // Asserts a plan quantity is non-increasing (faster) from rank r to r+1 across the catalogued range.
        private static void AssertPlanNonIncreasing(WeaponBoon kind, RunWeaponFamily family,
            ArsenalSkillKind abilityKind, int slot, Func<ArsenalCastPlan, float> quantity, string label)
        {
            int maxRank = Def(kind).MaxRank;
            for (int r = 1; r < maxRank; r++)
            {
                float lo = PlanFloat(family, kind, r, abilityKind, slot, quantity);
                float hi = PlanFloat(family, kind, r + 1, abilityKind, slot, quantity);
                PropertyCheck.That(hi <= lo + Tolerance,
                    $"{kind} {label}: rank {r} -> {r + 1} increased ({lo} -> {hi})");
            }
        }

        // Total gauntlet stance damage summed across every produced step (originals + echoes). Both the
        // per-step *(1 + 0.75*rank) StanceCrusher scale and the FlurryEcho/AsuraEcho echo count feed this,
        // so it is a single monotone aggregate for those named quantities.
        private static float GauntletStanceTotal(RunWeaponFamily family, WeaponBoon kind, int rank, int slot)
        {
            WeaponRunModifiers mods = WithRank(family, kind, rank);
            BreakerGauntletAbility ability = NewGauntletAbility();
            try
            {
                List<AreaHitStep> steps = mods.GauntletSteps(ability, slot);
                float total = 0f;
                foreach (AreaHitStep step in steps) total += step.stanceDamage;
                return total;
            }
            finally { UnityEngine.Object.DestroyImmediate(ability); }
        }

        private static float GauntletFirstPush(RunWeaponFamily family, WeaponBoon kind, int rank, int slot)
        {
            WeaponRunModifiers mods = WithRank(family, kind, rank);
            BreakerGauntletAbility ability = NewGauntletAbility();
            try { return mods.GauntletSteps(ability, slot)[0].pushDistance; }
            finally { UnityEngine.Object.DestroyImmediate(ability); }
        }

        private static float GauntletFirstSphereRadius(RunWeaponFamily family, WeaponBoon kind, int rank, int slot)
        {
            WeaponRunModifiers mods = WithRank(family, kind, rank);
            BreakerGauntletAbility ability = NewGauntletAbility();
            try { return mods.GauntletSteps(ability, slot)[0].sphereRadius; }
            finally { UnityEngine.Object.DestroyImmediate(ability); }
        }

        private static int GauntletStepCount(RunWeaponFamily family, WeaponBoon kind, int rank, int slot)
        {
            WeaponRunModifiers mods = WithRank(family, kind, rank);
            BreakerGauntletAbility ability = NewGauntletAbility();
            try { return mods.GauntletSteps(ability, slot).Count; }
            finally { UnityEngine.Object.DestroyImmediate(ability); }
        }

        // Feature: weapon-gameplay-swarm-rework, Property 38: Monotonicidade dos modificadores por rank
        // For every catalogued modifier, each Cast_Plan / GauntletSteps quantity it affects is monotone in the
        // strengthening direction as the rank grows from 1 to its catalogued maximum: additive counts and
        // size/damage/wave multipliers are non-decreasing, and Interval (cadence) is non-increasing (faster).
        // Validates: Requirements 12.2, 12.3, 12.4
        [Test]
        public void CatalogedModifiers_StrengthenMonotonicallyWithRank()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Bow slot 0 — Rapid Burst: Hits += 2*rank (up) and Interval /= (1 + 0.25*rank) (faster/down).
                AssertPlanNonDecreasing(WeaponBoon.RapidBurst, RunWeaponFamily.Bow, ArsenalSkillKind.Arrow, 0, p => p.Hits, "Hits");
                AssertPlanNonIncreasing(WeaponBoon.RapidBurst, RunWeaponFamily.Bow, ArsenalSkillKind.Arrow, 0, p => p.Interval, "Interval (faster)");

                // Bow slot 1 — Heavy Bolt: Damage *= (1 + 0.6*rank).
                AssertPlanNonDecreasing(WeaponBoon.HeavyBolt, RunWeaponFamily.Bow, ArsenalSkillKind.Arrow, 1, p => p.Damage, "Damage");

                // Bow slot 2 — Wide Volley: Arrows += 4*rank.
                AssertPlanNonDecreasing(WeaponBoon.WideVolley, RunWeaponFamily.Bow, ArsenalSkillKind.Volley, 2, p => p.Arrows, "Arrows");

                // Bow slot 3 — Long Rain: Hits += 3*rank and Width *= (1 + 0.2*rank).
                AssertPlanNonDecreasing(WeaponBoon.LongRain, RunWeaponFamily.Bow, ArsenalSkillKind.Rain, 3, p => p.Hits, "Hits");
                AssertPlanNonDecreasing(WeaponBoon.LongRain, RunWeaponFamily.Bow, ArsenalSkillKind.Rain, 3, p => p.Width, "Width");

                // Spear Thrust — Echo Thrust: named monotone quantity is the echo count Hits += rank.
                AssertPlanNonDecreasing(WeaponBoon.EchoThrust, RunWeaponFamily.Spear, ArsenalSkillKind.Thrust, 0, p => p.Hits, "Hits (echoes)");

                // Spear slot 1 — Triple Moon: Hits += 2*rank; Orbit: Width *= (1 + 0.25*rank).
                AssertPlanNonDecreasing(WeaponBoon.TripleMoon, RunWeaponFamily.Spear, ArsenalSkillKind.Sweep, 1, p => p.Hits, "Hits (sweeps)");
                AssertPlanNonDecreasing(WeaponBoon.Orbit, RunWeaponFamily.Spear, ArsenalSkillKind.Sweep, 1, p => p.Width, "Width");

                // Spear slot 3 — Dragon Wave: WaveMultiplier = 0.6*rank.
                AssertPlanNonDecreasing(WeaponBoon.DragonWave, RunWeaponFamily.Spear, ArsenalSkillKind.Thrust, 3, p => p.WaveMultiplier, "WaveMultiplier");

                // Spear Sweep — Moon Shard: ShardCount = Clamp(2*rank, 0, 6): non-decreasing then flat.
                AssertPlanNonDecreasing(WeaponBoon.MoonShard, RunWeaponFamily.Spear, ArsenalSkillKind.Sweep, 1, p => p.ShardCount, "ShardCount");

                // Spear MeleeScale — Long Reach: Range and Width *= (1 + 0.3*rank).
                AssertPlanNonDecreasing(WeaponBoon.LongReach, RunWeaponFamily.Spear, ArsenalSkillKind.Thrust, 0, p => p.Range, "Range (MeleeScale)");
                AssertPlanNonDecreasing(WeaponBoon.LongReach, RunWeaponFamily.Spear, ArsenalSkillKind.Thrust, 0, p => p.Width, "Width (MeleeScale)");

                // Gauntlet — Stance Crusher: stanceDamage *= (1 + 0.75*rank) and pushDistance *= (1 + 0.3*rank).
                for (int r = 1; r < Def(WeaponBoon.StanceCrusher).MaxRank; r++)
                {
                    PropertyCheck.That(
                        GauntletStanceTotal(RunWeaponFamily.Gauntlet, WeaponBoon.StanceCrusher, r + 1, 0)
                            >= GauntletStanceTotal(RunWeaponFamily.Gauntlet, WeaponBoon.StanceCrusher, r, 0) - Tolerance,
                        $"StanceCrusher stanceDamage total decreased at rank {r} -> {r + 1}");
                    PropertyCheck.That(
                        GauntletFirstPush(RunWeaponFamily.Gauntlet, WeaponBoon.StanceCrusher, r + 1, 0)
                            >= GauntletFirstPush(RunWeaponFamily.Gauntlet, WeaponBoon.StanceCrusher, r, 0) - Tolerance,
                        $"StanceCrusher pushDistance decreased at rank {r} -> {r + 1}");
                }

                // Gauntlet slot 2 — Shock Ring: sphereRadius *= (1 + 0.2*rank).
                for (int r = 1; r < Def(WeaponBoon.ShockRing).MaxRank; r++)
                    PropertyCheck.That(
                        GauntletFirstSphereRadius(RunWeaponFamily.Gauntlet, WeaponBoon.ShockRing, r + 1, 2)
                            >= GauntletFirstSphereRadius(RunWeaponFamily.Gauntlet, WeaponBoon.ShockRing, r, 2) - Tolerance,
                        $"ShockRing sphereRadius decreased at rank {r} -> {r + 1}");

                // Gauntlet slot 1 — Flurry Echo: echo count 2*rank raises the produced step count.
                for (int r = 1; r < Def(WeaponBoon.FlurryEcho).MaxRank; r++)
                    PropertyCheck.That(
                        GauntletStepCount(RunWeaponFamily.Gauntlet, WeaponBoon.FlurryEcho, r + 1, 1)
                            >= GauntletStepCount(RunWeaponFamily.Gauntlet, WeaponBoon.FlurryEcho, r, 1),
                        $"FlurryEcho step count decreased at rank {r} -> {r + 1}");

                // Gauntlet slot 3 — Asura Echo: echo count rank raises the produced step count.
                for (int r = 1; r < Def(WeaponBoon.AsuraEcho).MaxRank; r++)
                    PropertyCheck.That(
                        GauntletStepCount(RunWeaponFamily.Gauntlet, WeaponBoon.AsuraEcho, r + 1, 3)
                            >= GauntletStepCount(RunWeaponFamily.Gauntlet, WeaponBoon.AsuraEcho, r, 3),
                        $"AsuraEcho step count decreased at rank {r} -> {r + 1}");

                // Gauntlet MeleeScale — Long Fists: sphere radius (via boxSize/MeleeScale) is non-decreasing.
                for (int r = 1; r < Def(WeaponBoon.LongFists).MaxRank; r++)
                    PropertyCheck.That(
                        GauntletFirstSphereRadius(RunWeaponFamily.Gauntlet, WeaponBoon.LongFists, r + 1, 0)
                            >= GauntletFirstSphereRadius(RunWeaponFamily.Gauntlet, WeaponBoon.LongFists, r, 0) - Tolerance,
                        $"LongFists sphereRadius (MeleeScale) decreased at rank {r} -> {r + 1}");
            });
        }
    }
}
