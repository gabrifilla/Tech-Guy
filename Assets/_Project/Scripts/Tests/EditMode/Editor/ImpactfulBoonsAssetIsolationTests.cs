using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 1 of impactful-weapon-boons (asset isolation).
    ///
    /// Property 1 (design): <em>for any</em> application of any new boon at any rank, every original
    /// weapon, ability, and status <see cref="ScriptableObject"/> SHALL remain unchanged.
    /// <b>Validates: Requirements 1.1, 4.4, 5.4, 7.4.</b>
    ///
    /// This is the impactful-weapon-boons counterpart to <see cref="AssetIsolationTests"/> (which
    /// covers the modifier-synergies-theme17 boons). It extends that coverage to the six new
    /// weapon-family boons — <see cref="WeaponBoon.SplitArrow"/>, <see cref="WeaponBoon.ChargedShot"/>
    /// (Bow), <see cref="WeaponBoon.PerfectSpacing"/>, <see cref="WeaponBoon.ImpalingLine"/> (Spear),
    /// <see cref="WeaponBoon.MomentumStrike"/>, <see cref="WeaponBoon.Shockwave"/> (Gauntlet) — driven
    /// through the reward-owned snapshot builders (<see cref="WeaponRunModifiers.Plan"/> and
    /// <see cref="WeaponRunModifiers.GauntletSteps"/>).
    ///
    /// The property is asserted the same way as the existing test: standalone source
    /// <see cref="ArsenalAbility"/>/<see cref="BreakerGauntletAbility"/> instances are snapshotted with
    /// <see cref="JsonUtility.ToJson(object)"/>, the reward path is driven at randomized ranks across
    /// every slot, and every source instance is re-asserted byte-for-byte unchanged.
    ///
    /// Notes on which new boons touch assets (per the task's guidance):
    /// <list type="bullet">
    /// <item><see cref="WeaponBoon.ImpalingLine"/> writes <c>ImpaleLine</c>/<c>ImpalePull</c> onto the
    /// per-cast <see cref="ArsenalCastPlan"/> in the thrust branch — value types on the snapshot, so the
    /// source ability is never mutated (R5.4).</item>
    /// <item><see cref="WeaponBoon.Shockwave"/> appends a JsonUtility deep-cloned <see cref="AreaHitStep"/>
    /// to the <c>GauntletSteps</c> output — the source ability's authored steps are never mutated (R7.4).</item>
    /// <item><see cref="WeaponBoon.PerfectSpacing"/> only reads ranks inside
    /// <see cref="WeaponRunModifiers.DirectDamageMultiplier"/>, which touches no asset at all (R4.4);
    /// its purity is asserted separately below.</item>
    /// <item><see cref="WeaponBoon.SplitArrow"/>/<see cref="WeaponBoon.ChargedShot"/>/
    /// <see cref="WeaponBoon.MomentumStrike"/> are event/registry-driven and are not consulted by the
    /// Plan/GauntletSteps builders; adding them at any rank must still leave the plan path (and the
    /// source assets it reads) untouched, which this test confirms by adding them alongside the others.</item>
    /// </list>
    ///
    /// Statuses (<see cref="BurnStatus"/>/<see cref="ChillStatus"/>) are runtime MonoBehaviour components,
    /// not assets, so the only source assets a reward path could mutate are the source ability assets.
    /// Pure ScriptableObject plan generation — no scene, no physics — so it runs in EditMode.
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property and reports
    /// the exact failing case as a counterexample.
    /// </summary>
    public sealed class ImpactfulBoonsAssetIsolationTests
    {
        private static readonly ArsenalSkillKind[] ArsenalKinds =
        {
            ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust, ArsenalSkillKind.Sweep,
        };

        // The six new weapon-family boons, grouped by family, so the property adds only in-family boons
        // (the family gate would refuse the others). Overflow (cross-family) is not a WeaponBoon and is
        // covered by the run-end/gate tests, not the plan-path isolation property.
        private static readonly WeaponBoon[] BowNewBoons = { WeaponBoon.SplitArrow, WeaponBoon.ChargedShot };
        private static readonly WeaponBoon[] SpearNewBoons = { WeaponBoon.PerfectSpacing, WeaponBoon.ImpalingLine };
        private static readonly WeaponBoon[] GauntletNewBoons = { WeaponBoon.MomentumStrike, WeaponBoon.Shockwave };

        [Serializable] private struct ArsenalKindOverlay { public int _kind; }
        [Serializable] private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        private static ArsenalAbility NewArsenalAbility(ArsenalSkillKind kind, int index)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "ImpactfulSourceArsenal_" + kind + "_" + index;
            var overlay = new ArsenalKindOverlay { _kind = (int)kind };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // A source gauntlet ability carrying authored AreaHitSteps so GauntletSteps has a final step to
        // clone the Shockwave off of (an empty ability would make the append trivially a no-op).
        private static BreakerGauntletAbility NewGauntletAbility(int index, int stepCount)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "ImpactfulSourceGauntlet_" + index;
            var authored = new AreaHitStep[stepCount];
            for (int i = 0; i < stepCount; i++)
                authored[i] = new AreaHitStep
                {
                    delay = .1f * i,
                    rangeOverride = 1.5f + i,
                    hitShape = AreaHitShape.Box,
                    boxSize = new Vector3(3f + i, 2f, 0f),
                    sphereRadius = 1.5f,
                    damageMultiplier = 1f + .25f * i,
                    stanceDamage = 12f + i,
                    pushDistance = .35f,
                };
            var overlay = new HitStepsOverlay { _hitSteps = authored };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // Feature: impactful-weapon-boons, Property 1: Asset isolation.
        // Driving the reward-owned plan path (Plan for Bow/Spear, GauntletSteps for Gauntlet) at
        // randomized ranks of the NEW boons across every slot leaves every source ability asset
        // byte-for-byte unchanged. Covers ImpalingLine (plan fields), Shockwave (appended step clone),
        // PerfectSpacing (rank-only read), and the event-driven boons (SplitArrow/ChargedShot/
        // MomentumStrike) whose presence must not perturb the plan path.
        // Validates: Requirements 1.1, 4.4, 5.4, 7.4
        [Test]
        public void NewBoonsPlanPath_LeavesSourceAbilityAssetsUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Pick a family and its set of new boons; only in-family boons are addable through the gate.
                int familyRoll = rng.Next(3);
                RunWeaponFamily family = familyRoll == 0 ? RunWeaponFamily.Bow
                    : familyRoll == 1 ? RunWeaponFamily.Spear : RunWeaponFamily.Gauntlet;
                WeaponBoon[] newBoons = familyRoll == 0 ? BowNewBoons
                    : familyRoll == 1 ? SpearNewBoons : GauntletNewBoons;

                var mods = new WeaponRunModifiers(family);
                // Add each new boon to a randomized rank in [0, MaxRank] (0 = not picked).
                foreach (WeaponBoon boon in newBoons)
                {
                    WeaponRunModifiers.Definition def = Def(boon);
                    int wanted = rng.Next(0, def.MaxRank + 1);
                    for (int r = 0; r < wanted; r++)
                        PropertyCheck.That(mods.Add(def), $"in-family {boon} rank {r + 1} should be addable");
                }

                var sources = new List<UnityEngine.Object>();
                var before = new Dictionary<UnityEngine.Object, string>();
                try
                {
                    if (family == RunWeaponFamily.Gauntlet)
                    {
                        var abilities = new BreakerGauntletAbility[4];
                        for (int slot = 0; slot < abilities.Length; slot++)
                        {
                            abilities[slot] = NewGauntletAbility(slot, rng.Next(1, 4));
                            sources.Add(abilities[slot]);
                            before[abilities[slot]] = JsonUtility.ToJson(abilities[slot]);
                        }
                        int repeats = rng.Next(1, 5);
                        for (int rep = 0; rep < repeats; rep++)
                            for (int slot = 0; slot < abilities.Length; slot++)
                                mods.GauntletSteps(abilities[slot], slot);
                    }
                    else
                    {
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
                            "Source asset mutated by the new-boon plan path: " + entry.Key.name);
                }
                finally
                {
                    foreach (UnityEngine.Object o in sources) if (o) UnityEngine.Object.DestroyImmediate(o);
                }
            });
        }

        // Feature: impactful-weapon-boons, Property 1: Asset isolation (Perfect Spacing purity).
        // DirectDamageMultiplier reads only PerfectSpacing's rank and returns a scalar; it constructs no
        // ability/weapon reference and cannot write any asset. This case asserts the term is a pure
        // function of (distance, rank) by confirming repeated evaluation is side-effect-free and that no
        // WeaponScript/ArsenalAbility source built alongside it is disturbed.
        // Validates: Requirements 4.4, 1.1
        [Test]
        public void PerfectSpacing_DirectDamageMultiplierIsPureAndTouchesNoAsset()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(0, Def(WeaponBoon.PerfectSpacing).MaxRank + 1);
                var mods = new WeaponRunModifiers(RunWeaponFamily.Spear);
                for (int r = 0; r < rank; r++)
                    PropertyCheck.That(mods.Add(Def(WeaponBoon.PerfectSpacing)), "PerfectSpacing rank should be addable");

                // A source ability + weapon that exist while the pure term is evaluated: they must be
                // untouched, proving the multiplier never reaches back into any asset.
                ArsenalAbility ability = NewArsenalAbility(ArsenalSkillKind.Thrust, i);
                var weapon = ScriptableObject.CreateInstance<WeaponScript>();
                weapon.name = "PerfectSpacingSourceWeapon_" + i;
                try
                {
                    string abilityBefore = JsonUtility.ToJson(ability);
                    string weaponBefore = JsonUtility.ToJson(weapon);

                    float distance = (float)(rng.NextDouble() * 12.0); // 0..12m spans in- and out-of-band
                    float first = mods.DirectDamageMultiplier(distance, 1f, 1f, 0);
                    float second = mods.DirectDamageMultiplier(distance, 1f, 1f, 0);

                    // Purity: identical inputs -> identical output, with no accumulation between calls.
                    PropertyCheck.That(Mathf.Approximately(first, second),
                        $"DirectDamageMultiplier not pure: {first} != {second} (distance={distance}, rank={rank})");

                    // No asset was disturbed by evaluating the term.
                    PropertyCheck.That(JsonUtility.ToJson(ability) == abilityBefore,
                        "PerfectSpacing term mutated a source ability asset");
                    PropertyCheck.That(JsonUtility.ToJson(weapon) == weaponBefore,
                        "PerfectSpacing term mutated a source weapon asset");
                }
                finally
                {
                    if (ability) UnityEngine.Object.DestroyImmediate(ability);
                    if (weapon) UnityEngine.Object.DestroyImmediate(weapon);
                }
            });
        }
    }
}
