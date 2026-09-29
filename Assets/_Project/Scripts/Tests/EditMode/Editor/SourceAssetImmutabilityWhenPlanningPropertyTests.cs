using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 37 of weapon-gameplay-swarm-rework
    /// (task 15.2, Requisito 12.1/12.5): "Para todo conjunto de Run_Modifier aplicado a um
    /// ArsenalCastPlan/GauntletSteps, os campos dos assets de origem
    /// (ArsenalAbility/BreakerGauntletAbility/WeaponScript) permanecem inalterados após a
    /// aplicação, enquanto o snapshot reflete as mudanças."
    ///
    /// The sibling suites cover neighbouring facets: <see cref="AssetIsolationTests"/> asserts
    /// isolation with <em>empty</em> abilities, and <see cref="GauntletStepsCloneIsolationTests"/>
    /// pins the clone-distinctness of the gauntlet step loop. Neither drives Property 37's full
    /// contract — arbitrary rank sets built through <see cref="WeaponRunModifiers.Add"/>, a
    /// byte-identical source snapshot (<see cref="JsonUtility.ToJson(object)"/>) before/after, AND
    /// the complementary half that the per-cast snapshot actually <em>reflects</em> those ranks.
    /// This test adds that property-based version without duplicating the example-shaped guards.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property
    /// (min 128) and reports the exact failing case as a counterexample. Pure ScriptableObject plan
    /// generation, so it runs in EditMode (no scene, no physics).
    /// </summary>
    public sealed class SourceAssetImmutabilityWhenPlanningPropertyTests
    {
        // Overlay to seed the private serialized fields of the source assets via a JSON round-trip,
        // mirroring the technique used by AssetIsolationTests / GauntletStepsCloneIsolationTests.
        [Serializable]
        private struct ArsenalOverlay
        {
            public int _kind;
            public float _windup, _interval, _range, _width, _damageMultiplier;
            public int _hits;
            public bool _piercing;
        }

        [Serializable]
        private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        private static readonly ArsenalSkillKind[] ArsenalKinds =
        {
            ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust, ArsenalSkillKind.Sweep,
        };

        // A source arsenal ability seeded with non-default, positive values so that any rank-driven
        // scaling in Plan produces a snapshot visibly different from the rank-0 baseline. The source
        // is the asset whose byte-for-byte immutability the property asserts.
        private static ArsenalAbility NewArsenalAbility(System.Random rng, ArsenalSkillKind kind, int index)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "SourceArsenal_" + kind + "_" + index;
            var overlay = new ArsenalOverlay
            {
                _kind = (int)kind,
                _windup = 0.2f + (float)rng.NextDouble(),
                _interval = 0.15f + (float)rng.NextDouble(),
                _range = 2f + (float)rng.NextDouble() * 10f,
                _width = 1f + (float)rng.NextDouble() * 3f,
                _damageMultiplier = 1f + (float)rng.NextDouble() * 3f,
                _hits = 1 + rng.Next(0, 4),
                _piercing = rng.Next(0, 2) == 0,
            };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        private static AreaHitStep NewAuthoredStep(System.Random rng, int index)
        {
            return new AreaHitStep
            {
                delay = 0.1f * index,
                rangeOverride = 1.5f + index,
                hitShape = AreaHitShape.Box,
                boxSize = new Vector3(3f + index, 2f, 0.5f + (float)rng.NextDouble()),
                sphereRadius = 1.5f + (float)rng.NextDouble(),
                damageMultiplier = 1f + 0.25f * index,
                bonusDamage = index,
                localOffset = new Vector3(0f, index, 0f),
                reactionType = HitReactionType.Stagger,
                hitStrength = HitStrength.Light,
                pushDistance = 0.35f + (float)rng.NextDouble(),
                stanceDamage = 12f + index,
                breakEffect = StanceBreakEffect.None,
                stunDuration = 1f,
                knockUpHeight = 2f,
                knockbackDistance = 4f,
            };
        }

        private static BreakerGauntletAbility NewGauntletAbility(System.Random rng, int index, int stepCount)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "SourceGauntlet_" + index;
            var authored = new AreaHitStep[stepCount];
            for (int i = 0; i < stepCount; i++) authored[i] = NewAuthoredStep(rng, i);
            var overlay = new HitStepsOverlay { _hitSteps = authored };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // Adds a randomized-but-valid set of ranks for the given family, exclusively through the
        // public Add gate (so ranks stay inside [1; MaxRank] and the run owns them). Returns whether
        // at least one rank was actually acquired, so the "snapshot reflects changes" half can be
        // asserted only when a rank-driven modifier is present.
        private static bool AddRandomRankSet(WeaponRunModifiers mods, RunWeaponFamily family, System.Random rng)
        {
            bool anyAcquired = false;
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
            {
                if (definition.Family != family) continue;
                int wanted = rng.Next(0, definition.MaxRank + 1);
                for (int r = 0; r < wanted; r++)
                {
                    PropertyCheck.That(mods.Add(definition), "in-family rank should be addable through Add");
                    anyAcquired = true;
                }
            }
            return anyAcquired;
        }

        [Serializable]
        private struct AreaHitStepList { public List<AreaHitStep> items; }

        private static string SnapshotJson(List<AreaHitStep> steps) =>
            JsonUtility.ToJson(new AreaHitStepList { items = steps });

        // Feature: weapon-gameplay-swarm-rework, Property 37: Imutabilidade dos assets de origem ao planejar
        // For any rank set applied to a Bow/Spear ability, WeaponRunModifiers.Plan leaves the source
        // ArsenalAbility asset byte-for-byte unchanged (JsonUtility.ToJson identical before/after),
        // while the per-cast ArsenalCastPlan snapshot reflects the applied ranks (differs from the
        // rank-0 baseline whenever a rank-driven modifier is present for the driven slot).
        // Validates: Requirements 12.1, 12.5
        [Test]
        public void PlanLeavesArsenalSourceUnchangedWhileSnapshotReflectsRanks()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily family = rng.Next(0, 2) == 0 ? RunWeaponFamily.Bow : RunWeaponFamily.Spear;

                var mods = new WeaponRunModifiers(family);
                bool anyRank = AddRandomRankSet(mods, family, rng);

                // A baseline with no run modifiers, to show the snapshot reflects the applied ranks.
                var baseline = new WeaponRunModifiers(family);

                var sources = new List<UnityEngine.Object>();
                try
                {
                    for (int slot = 0; slot < 4; slot++)
                    {
                        ArsenalSkillKind kind = ArsenalKinds[rng.Next(ArsenalKinds.Length)];
                        ArsenalAbility ability = NewArsenalAbility(rng, kind, slot);
                        sources.Add(ability);

                        string before = JsonUtility.ToJson(ability);

                        ArsenalCastPlan plan = mods.Plan(ability, slot);
                        ArsenalCastPlan basePlan = baseline.Plan(ability, slot);

                        // Immutability: the source asset is byte-for-byte identical after planning.
                        PropertyCheck.That(JsonUtility.ToJson(ability) == before,
                            $"[{family} slot {slot}] source ArsenalAbility '{ability.name}' was mutated by Plan");

                        // Snapshot reflects the ranks: the modified plan differs from the rank-0
                        // baseline whenever this family/slot carries a rank-driven modifier. When no
                        // rank is present, the modified plan must equal the baseline plan (no phantom
                        // change), which also confirms the snapshot mirrors exactly the applied ranks.
                        string planJson = JsonUtility.ToJson(plan);
                        string basePlanJson = JsonUtility.ToJson(basePlan);
                        if (!anyRank)
                            PropertyCheck.That(planJson == basePlanJson,
                                $"[{family} slot {slot}] plan changed with no ranks applied");
                    }

                    // With at least one rank acquired, some slot's snapshot must differ from the
                    // rank-0 baseline: the applied Run_Modifier ranks are reflected in the snapshot.
                    if (anyRank)
                    {
                        bool anySnapshotChanged = false;
                        foreach (UnityEngine.Object o in sources)
                        {
                            var ability = (ArsenalAbility)o;
                            for (int slot = 0; slot < 4; slot++)
                            {
                                string planJson = JsonUtility.ToJson(mods.Plan(ability, slot));
                                string basePlanJson = JsonUtility.ToJson(baseline.Plan(ability, slot));
                                if (planJson != basePlanJson) { anySnapshotChanged = true; break; }
                            }
                            if (anySnapshotChanged) break;
                        }
                        PropertyCheck.That(anySnapshotChanged,
                            $"[{family}] applied ranks were not reflected in any per-cast plan snapshot");
                    }
                }
                finally
                {
                    foreach (UnityEngine.Object o in sources) if (o) UnityEngine.Object.DestroyImmediate(o);
                }
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 37: Imutabilidade dos assets de origem ao planejar
        // For any Gauntlet rank set, WeaponRunModifiers.GauntletSteps leaves the source
        // BreakerGauntletAbility asset byte-for-byte unchanged (JsonUtility.ToJson identical
        // before/after), while the returned AreaHitStep snapshot reflects the applied ranks
        // (differs from the rank-0 baseline whenever a rank-driven modifier is present).
        // Validates: Requirements 12.1, 12.5
        [Test]
        public void GauntletStepsLeaveSourceUnchangedWhileSnapshotReflectsRanks()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                bool anyRank = AddRandomRankSet(mods, RunWeaponFamily.Gauntlet, rng);
                var baseline = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);

                int stepCount = 1 + rng.Next(0, 3);
                var sources = new List<UnityEngine.Object>();
                try
                {
                    bool anySnapshotChanged = false;
                    for (int slot = 0; slot < 4; slot++)
                    {
                        BreakerGauntletAbility ability = NewGauntletAbility(rng, slot, stepCount);
                        sources.Add(ability);

                        string before = JsonUtility.ToJson(ability);

                        List<AreaHitStep> steps = mods.GauntletSteps(ability, slot);
                        List<AreaHitStep> baseSteps = baseline.GauntletSteps(ability, slot);

                        // Immutability: the source asset is byte-for-byte identical after planning.
                        PropertyCheck.That(JsonUtility.ToJson(ability) == before,
                            $"[Gauntlet slot {slot}] source BreakerGauntletAbility '{ability.name}' was mutated by GauntletSteps");

                        string snapJson = SnapshotJson(steps);
                        string baseSnapJson = SnapshotJson(baseSteps);
                        if (snapJson != baseSnapJson) anySnapshotChanged = true;
                        if (!anyRank)
                            PropertyCheck.That(snapJson == baseSnapJson,
                                $"[Gauntlet slot {slot}] steps changed with no ranks applied");
                    }

                    // With at least one Gauntlet rank acquired, some slot's step snapshot must differ
                    // from the rank-0 baseline: the applied ranks are reflected in the snapshot.
                    if (anyRank)
                        PropertyCheck.That(anySnapshotChanged,
                            "[Gauntlet] applied ranks were not reflected in any per-cast step snapshot");
                }
                finally
                {
                    foreach (UnityEngine.Object o in sources) if (o) UnityEngine.Object.DestroyImmediate(o);
                }
            });
        }
    }
}
