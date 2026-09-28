using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for R11 Property 25 of modifier-synergies-theme17
    /// (asset isolation: applying any new/modified reward leaves every original
    /// weapon/ability/status asset unchanged).
    ///
    /// This is the in-suite counterpart to the editor validator
    /// <c>WeaponModifierValidation.CheckAssetIsolation</c> (task 15.3): where that tool loads the
    /// real <c>Resources</c> weapons and drives the reward path inside a Play-mode session, this
    /// property test constructs standalone <see cref="ArsenalAbility"/> / <see cref="BreakerGauntletAbility"/>
    /// / <see cref="WeaponScript"/> <see cref="ScriptableObject"/> instances, snapshots their
    /// serialized state with <see cref="JsonUtility.ToJson(object)"/>, drives the reward-owned
    /// modifier paths (<see cref="WeaponRunModifiers.Plan"/> and
    /// <see cref="WeaponRunModifiers.GauntletSteps"/>) at randomized ranks across every slot, and
    /// then re-asserts every source instance is byte-for-byte unchanged. It also exercises the
    /// Rewrite runtime-copy grant (<see cref="ArsenalAbility.ConfigureRunTransformation"/>): the
    /// grant mutates only the runtime copy and never the source it was instantiated from (R11.1).
    ///
    /// Statuses (<see cref="BurnStatus"/>/<see cref="ChillStatus"/>) are runtime MonoBehaviour
    /// components, not assets, so — exactly as the editor validator notes — the only source assets
    /// that a reward path could mutate are the source <see cref="WeaponScript"/> and its
    /// <see cref="Ability"/> assets. This test is pure plan generation on ScriptableObject
    /// instances, so it runs in EditMode (no scene, no physics).
    /// </summary>
    public sealed class AssetIsolationTests
    {
        private static readonly RunWeaponFamily[] Families =
        {
            RunWeaponFamily.Bow, RunWeaponFamily.Spear, RunWeaponFamily.Gauntlet,
        };

        private static readonly ArsenalSkillKind[] ArsenalKinds =
        {
            ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust, ArsenalSkillKind.Sweep,
        };

        // A fresh arsenal ability whose serialized _kind is set (via a re-serialized JSON blob) so the
        // spear Thrust/Sweep branches in Plan are exercised. The instance is never mutated by Plan;
        // it is the "source asset" whose isolation the property asserts.
        private static ArsenalAbility NewArsenalAbility(ArsenalSkillKind kind, int index)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "SourceArsenal_" + kind + "_" + index;
            // _kind is private serialized; overwrite via JSON round-trip so Plan's Kind-gated
            // branches (Thrust/Sweep) are reachable without touching the source through a reward path.
            string json = JsonUtility.ToJson(ability);
            var overlay = new ArsenalKindOverlay { _kind = (int)kind };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        [Serializable]
        private struct ArsenalKindOverlay { public int _kind; }

        private static BreakerGauntletAbility NewGauntletAbility(int index)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "SourceGauntlet_" + index;
            return ability;
        }

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        // Feature: modifier-synergies-theme17, Property 25
        // Driving the reward-owned modifier plan path (Plan / GauntletSteps) at randomized ranks
        // across every slot leaves every source ability asset byte-for-byte unchanged (R11.1).
        // Validates: Requirements 11.1
        [Test]
        public void RewardPlanPath_LeavesSourceAbilityAssetsUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily family = Families[rng.Next(Families.Length)];
                var mods = new WeaponRunModifiers(family);

                // Add a randomized subset of this family's boons at randomized ranks (up to max).
                foreach (var definition in WeaponRunModifiers.Catalog)
                {
                    if (definition.Family != family) continue;
                    int wanted = rng.Next(0, definition.MaxRank + 1);
                    for (int r = 0; r < wanted; r++) PropertyCheck.That(mods.Add(definition), "in-family rank should be addable");
                }

                var sources = new List<UnityEngine.Object>();
                var before = new Dictionary<UnityEngine.Object, string>();

                try
                {
                    if (family == RunWeaponFamily.Gauntlet)
                    {
                        // Build a source gauntlet ability per slot; snapshot, then drive GauntletSteps.
                        var abilities = new BreakerGauntletAbility[4];
                        for (int slot = 0; slot < abilities.Length; slot++)
                        {
                            abilities[slot] = NewGauntletAbility(slot);
                            sources.Add(abilities[slot]);
                            before[abilities[slot]] = JsonUtility.ToJson(abilities[slot]);
                        }
                        // Drive the reward path repeatedly across every slot.
                        int repeats = rng.Next(1, 5);
                        for (int rep = 0; rep < repeats; rep++)
                            for (int slot = 0; slot < abilities.Length; slot++)
                                mods.GauntletSteps(abilities[slot], slot);
                    }
                    else
                    {
                        // Build a source arsenal ability per slot; snapshot, then drive Plan.
                        var abilities = new ArsenalAbility[4];
                        for (int slot = 0; slot < abilities.Length; slot++)
                        {
                            ArsenalSkillKind kind = ArsenalKinds[rng.Next(ArsenalKinds.Length)];
                            abilities[slot] = NewArsenalAbility(kind, slot);
                            sources.Add(abilities[slot]);
                            before[abilities[slot]] = JsonUtility.ToJson(abilities[slot]);
                        }
                        int repeats = rng.Next(1, 5);
                        for (int rep = 0; rep < repeats; rep++)
                            for (int slot = 0; slot < abilities.Length; slot++)
                                mods.Plan(abilities[slot], slot);
                    }

                    foreach (KeyValuePair<UnityEngine.Object, string> entry in before)
                        PropertyCheck.That(JsonUtility.ToJson(entry.Key) == entry.Value,
                            "Source asset mutated by reward plan path: " + entry.Key.name);
                }
                finally
                {
                    foreach (UnityEngine.Object o in sources) if (o) UnityEngine.Object.DestroyImmediate(o);
                }
            });
        }

        // Feature: modifier-synergies-theme17, Property 25
        // The Rewrite runtime-copy grant (ConfigureRunTransformation) mutates only the runtime copy
        // and never the source ArsenalAbility asset it was instantiated from (R11.1/R9.4).
        // Validates: Requirements 11.1
        [Test]
        public void RewriteGrant_MutatesOnlyRuntimeCopy_NotSourceAsset()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                bool bow = rng.Next(0, 2) == 0;
                ArsenalSkillKind kind = ArsenalKinds[rng.Next(ArsenalKinds.Length)];
                ArsenalAbility source = NewArsenalAbility(kind, i);

                try
                {
                    string sourceBefore = JsonUtility.ToJson(source);

                    // The run creates a runtime copy (RunBoons.Start uses Instantiate) and configures
                    // the transformation on the copy only.
                    ArsenalAbility runtimeCopy = UnityEngine.Object.Instantiate(source);
                    try
                    {
                        runtimeCopy.ConfigureRunTransformation(bow);

                        // The source asset must be unchanged by the grant.
                        PropertyCheck.That(JsonUtility.ToJson(source) == sourceBefore,
                            "Rewrite grant mutated the source ArsenalAbility asset");

                        // Sanity: the runtime copy actually changed (so the assertion above is meaningful).
                        PropertyCheck.That(JsonUtility.ToJson(runtimeCopy) != sourceBefore,
                            "Rewrite grant should have transformed the runtime copy");
                    }
                    finally { if (runtimeCopy) UnityEngine.Object.DestroyImmediate(runtimeCopy); }
                }
                finally { if (source) UnityEngine.Object.DestroyImmediate(source); }
            });
        }
    }
}
