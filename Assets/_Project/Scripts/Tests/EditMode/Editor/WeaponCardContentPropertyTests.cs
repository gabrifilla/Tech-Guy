using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 4 of nexus-lobby-menu-restructure — complete weapon-card
    /// content assembly.
    ///
    /// Property 4 (design): for a weapon with N abilities, the card produces one line per ability
    /// containing <see cref="Ability.DisplayName"/>, <see cref="Ability.ManaCost"/> and
    /// <see cref="Ability.cooldownTime"/> (and <see cref="ArsenalAbility.Description"/> when the ability
    /// is an <see cref="ArsenalAbility"/>), plus the weapon name and its family
    /// (<see cref="WeaponRunModifiers.Identify"/>). <b>Validates: Requirements 2.2.</b>
    ///
    /// The pure assembler <see cref="WeaponCardContent.Build"/> keeps the text assembly separate from the
    /// IMGUI drawing, so this exercises it directly on freshly built <see cref="WeaponScript"/> assets
    /// without a scene. The weapon/ability assets are <c>ScriptableObject</c> instances created in memory
    /// (private serialized fields are set through <see cref="JsonUtility.FromJsonOverwrite"/> overlays,
    /// the same pattern used elsewhere in this suite) and destroyed after each case.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the seeded
    /// <see cref="PropertyCheck"/> harness drives &gt;= 100 deterministic cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    public sealed class WeaponCardContentPropertyTests
    {
        private const int MaxAbilities = 6;

        [Serializable] private struct AbilityOverlay { public string _displayName; public float _manaCost; }
        [Serializable] private struct ArsenalOverlay { public string _displayName; public float _manaCost; public int _kind; public string _description; }
        [Serializable] private struct WeaponFamilyOverlay { public bool _firesArrows; }

        private static readonly ArsenalSkillKind[] ArsenalKinds =
        {
            ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust, ArsenalSkillKind.Sweep,
        };

        // Feature: nexus-lobby-menu-restructure, Property 4: conteúdo do cartão completo.
        // For a weapon with N abilities, Build produces exactly one line per (non-null) ability with the
        // ability's DisplayName/ManaCost/cooldownTime (and Description when it is an ArsenalAbility), plus
        // the weapon name and family. Assembly preserves ability order.
        // Validates: Requirements 2.2
        [Test]
        public void Build_ProducesOneLinePerAbility_WithNameAndFamily()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Choose a family. Gauntlet requires a BreakerGauntletAbility to be present, so it is only
                // used when abilityCount > 0; otherwise fall back to Bow/Spear which Identify resolves
                // without needing a specific ability type.
                int abilityCount = rng.Next(0, MaxAbilities + 1);
                RunWeaponFamily wantedFamily = PickFamily(rng, abilityCount);

                var created = new List<UnityEngine.Object>();
                try
                {
                    // Build the expected ability payloads first, so the test data and the assertions share
                    // the same source of truth.
                    var expected = new List<(string display, float mana, float cd, string desc, bool arsenal)>(abilityCount);
                    var abilities = new Ability[abilityCount];

                    // Ensure Gauntlet weapons carry at least one BreakerGauntletAbility so Identify -> Gauntlet.
                    int gauntletSlot = wantedFamily == RunWeaponFamily.Gauntlet && abilityCount > 0
                        ? rng.Next(0, abilityCount)
                        : -1;

                    for (int a = 0; a < abilityCount; a++)
                    {
                        string display = "Skill_" + i + "_" + a;
                        float mana = (float)(rng.NextDouble() * 120.0);
                        float cd = (float)(rng.NextDouble() * 10.0);

                        if (a == gauntletSlot)
                        {
                            var gauntlet = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
                            gauntlet.name = display;
                            gauntlet.cooldownTime = cd;
                            var overlay = new AbilityOverlay { _displayName = display, _manaCost = mana };
                            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), gauntlet);
                            abilities[a] = gauntlet;
                            created.Add(gauntlet);
                            // BreakerGauntletAbility is not an ArsenalAbility, so no Description line.
                            // Its ManaCost is scaled by CombatBalanceConfig.SkillManaCostMultiplier
                            // (combat-balance-tuning task 4.1), so the card reports the effective cost;
                            // read it from the ability itself, not the raw authored value.
                            expected.Add((display, gauntlet.ManaCost, cd, null, false));
                        }
                        else if (rng.Next(0, 2) == 0)
                        {
                            // ArsenalAbility: carries a Description that must appear on the card line.
                            var arsenal = ScriptableObject.CreateInstance<ArsenalAbility>();
                            arsenal.name = display;
                            arsenal.cooldownTime = cd;
                            string desc = "Desc_" + i + "_" + a;
                            var overlay = new ArsenalOverlay
                            {
                                _displayName = display,
                                _manaCost = mana,
                                _kind = (int)ArsenalKinds[rng.Next(ArsenalKinds.Length)],
                                _description = desc,
                            };
                            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), arsenal);
                            abilities[a] = arsenal;
                            created.Add(arsenal);
                            expected.Add((display, Mathf.Max(0f, mana), cd, desc, true));
                        }
                        else
                        {
                            // Plain Ability: no Description (only ArsenalAbility carries one).
                            var ability = ScriptableObject.CreateInstance<Ability>();
                            ability.name = display;
                            ability.cooldownTime = cd;
                            var overlay = new AbilityOverlay { _displayName = display, _manaCost = mana };
                            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
                            abilities[a] = ability;
                            created.Add(ability);
                            expected.Add((display, Mathf.Max(0f, mana), cd, null, false));
                        }
                    }

                    var weapon = ScriptableObject.CreateInstance<WeaponScript>();
                    weapon.name = "CardWeapon_" + i;
                    weapon.weaponName = "Arma " + i;
                    weapon.abilities = abilities;
                    created.Add(weapon);

                    // Set FiresArrows for the Bow family so Identify resolves to Bow; Spear/Gauntlet keep it false.
                    if (wantedFamily == RunWeaponFamily.Bow)
                    {
                        var famOverlay = new WeaponFamilyOverlay { _firesArrows = true };
                        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(famOverlay), weapon);
                    }

                    RunWeaponFamily expectedFamily = WeaponRunModifiers.Identify(weapon);

                    WeaponCardContent.CardContent card = WeaponCardContent.Build(
                        weapon, WeaponCardContent.StateKind.Equippable, 0, "E");

                    // Name and family are carried through.
                    PropertyCheck.That(card.WeaponName == weapon.weaponName,
                        $"[case #{i}] weapon name mismatch: '{card.WeaponName}' != '{weapon.weaponName}'");
                    PropertyCheck.That(card.Family == expectedFamily,
                        $"[case #{i}] family mismatch: {card.Family} != {expectedFamily}");
                    PropertyCheck.That(!string.IsNullOrEmpty(card.FamilyLabel),
                        $"[case #{i}] family label must not be empty");

                    // Exactly one line per ability, preserving order.
                    PropertyCheck.That(card.Abilities != null && card.Abilities.Count == expected.Count,
                        $"[case #{i}] expected {expected.Count} ability lines, got {(card.Abilities?.Count ?? -1)}");

                    for (int a = 0; a < expected.Count; a++)
                    {
                        WeaponCardContent.AbilityLine line = card.Abilities[a];
                        var e = expected[a];

                        PropertyCheck.That(line.DisplayName == e.display,
                            $"[case #{i}] line {a} DisplayName '{line.DisplayName}' != '{e.display}'");
                        PropertyCheck.That(Mathf.Approximately(line.ManaCost, e.mana),
                            $"[case #{i}] line {a} ManaCost {line.ManaCost} != {e.mana}");
                        PropertyCheck.That(Mathf.Approximately(line.CooldownTime, e.cd),
                            $"[case #{i}] line {a} CooldownTime {line.CooldownTime} != {e.cd}");

                        if (e.arsenal)
                        {
                            PropertyCheck.That(line.Description == e.desc,
                                $"[case #{i}] line {a} Description '{line.Description}' != '{e.desc}' (ArsenalAbility)");
                        }
                        else
                        {
                            PropertyCheck.That(line.Description == null,
                                $"[case #{i}] line {a} non-ArsenalAbility must have null Description, got '{line.Description}'");
                        }
                    }
                }
                finally
                {
                    foreach (UnityEngine.Object o in created) if (o) UnityEngine.Object.DestroyImmediate(o);
                }
            });
        }

        // Feature: nexus-lobby-menu-restructure, Property 4: conteúdo do cartão completo (edge cases).
        // A weapon with null abilities (or interspersed null entries) yields only the real ability lines
        // and never throws, so a partially-authored weapon asset still produces a legible card.
        // Validates: Requirements 2.2
        [Test]
        public void Build_SkipsNullAbilities_AndHandlesNullArray()
        {
            // Null abilities array -> zero ability lines, still carries name and family.
            var bare = ScriptableObject.CreateInstance<WeaponScript>();
            bare.name = "BareWeapon";
            bare.weaponName = "Vazia";
            bare.abilities = null;
            try
            {
                WeaponCardContent.CardContent card = WeaponCardContent.Build(
                    bare, WeaponCardContent.StateKind.Equippable, 0, "E");
                Assert.IsNotNull(card.Abilities);
                Assert.AreEqual(0, card.Abilities.Count);
                Assert.AreEqual("Vazia", card.WeaponName);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(bare);
            }

            // Array with a null entry among real abilities -> only the real ability produces a line.
            var real = ScriptableObject.CreateInstance<ArsenalAbility>();
            real.name = "Real";
            var overlay = new ArsenalOverlay { _displayName = "Real", _manaCost = 10f, _kind = (int)ArsenalSkillKind.Arrow, _description = "d" };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), real);

            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.name = "GappyWeapon";
            weapon.weaponName = "Furada";
            weapon.abilities = new Ability[] { null, real, null };
            try
            {
                WeaponCardContent.CardContent card = WeaponCardContent.Build(
                    weapon, WeaponCardContent.StateKind.Equippable, 0, "E");
                Assert.AreEqual(1, card.Abilities.Count);
                Assert.AreEqual("Real", card.Abilities[0].DisplayName);
                Assert.AreEqual("d", card.Abilities[0].Description);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(real);
                UnityEngine.Object.DestroyImmediate(weapon);
            }
        }

        // Feature: nexus-lobby-menu-restructure, Property 4: conteúdo do cartão completo (null weapon).
        // A null weapon returns an empty, non-throwing card so the drawing layer stays safe.
        // Validates: Requirements 2.2
        [Test]
        public void Build_NullWeapon_ReturnsEmptyCard()
        {
            WeaponCardContent.CardContent card = WeaponCardContent.Build(
                null, WeaponCardContent.StateKind.Equippable, 0, "E");
            Assert.IsNotNull(card.Abilities);
            Assert.AreEqual(0, card.Abilities.Count);
        }

        private static RunWeaponFamily PickFamily(System.Random rng, int abilityCount)
        {
            // Only allow Gauntlet when there is a slot to host the required BreakerGauntletAbility.
            int roll = rng.Next(abilityCount > 0 ? 3 : 2);
            switch (roll)
            {
                case 0: return RunWeaponFamily.Bow;
                case 1: return RunWeaponFamily.Spear;
                default: return RunWeaponFamily.Gauntlet;
            }
        }
    }
}
