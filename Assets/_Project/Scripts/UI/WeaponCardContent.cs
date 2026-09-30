using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Pure, scene-free assembly of the Hades-style weapon-card text from a <see cref="WeaponScript"/>.
///
/// This deliberately keeps the text assembly SEPARATE from the IMGUI drawing (<c>WeaponCardView</c>)
/// so it can be exercised in EditMode without a scene: given a weapon (and the pedestal state), it
/// produces the weapon name, the family/style label (<see cref="WeaponRunModifiers.Identify"/>), one
/// <see cref="AbilityLine"/> per <see cref="Ability"/> on the weapon, and a single state footer line
/// (equipped / "[key] equip" / "unlock · N coins" / "insufficient coins").
///
/// It references only data-holding Unity types (<see cref="WeaponScript"/>/<see cref="Ability"/>,
/// which are <c>ScriptableObject</c> assets) and never touches the scene, MonoBehaviours, or drawing,
/// mirroring the pure-logic pattern of <see cref="PedestalProximity"/> / <see cref="WeaponPedestalState"/>.
/// </summary>
public static class WeaponCardContent
{
    /// <summary>One ability's card line: display name, mana cost, cooldown, and optional description.</summary>
    public readonly struct AbilityLine
    {
        /// <summary>Ability display name (<see cref="Ability.DisplayName"/>).</summary>
        public readonly string DisplayName;

        /// <summary>Mana cost (<see cref="Ability.ManaCost"/>).</summary>
        public readonly float ManaCost;

        /// <summary>Cooldown time in seconds (<see cref="Ability.cooldownTime"/>).</summary>
        public readonly float CooldownTime;

        /// <summary>Ability description when the ability is an <see cref="ArsenalAbility"/>; otherwise null.</summary>
        public readonly string Description;

        public AbilityLine(string displayName, float manaCost, float cooldownTime, string description)
        {
            DisplayName = displayName;
            ManaCost = manaCost;
            CooldownTime = cooldownTime;
            Description = description;
        }
    }

    /// <summary>The assembled, drawing-free content of a weapon card.</summary>
    public readonly struct CardContent
    {
        /// <summary>Weapon name (<see cref="WeaponScript.weaponName"/>).</summary>
        public readonly string WeaponName;

        /// <summary>Family/style of the weapon (<see cref="WeaponRunModifiers.Identify"/>).</summary>
        public readonly RunWeaponFamily Family;

        /// <summary>Human-readable family label used on the card.</summary>
        public readonly string FamilyLabel;

        /// <summary>One line per <see cref="Ability"/> on the weapon, in weapon order.</summary>
        public readonly IReadOnlyList<AbilityLine> Abilities;

        /// <summary>State footer line (equipped / "[key] equip" / "unlock · N coins" / "insufficient coins").</summary>
        public readonly string StateLabel;

        public CardContent(string weaponName, RunWeaponFamily family, string familyLabel,
            IReadOnlyList<AbilityLine> abilities, string stateLabel)
        {
            WeaponName = weaponName;
            Family = family;
            FamilyLabel = familyLabel;
            Abilities = abilities;
            StateLabel = stateLabel;
        }
    }

    /// <summary>Card state footer variants, resolved by the pedestal's lock/equip status.</summary>
    public enum StateKind
    {
        /// <summary>Weapon is currently equipped.</summary>
        Equipped,

        /// <summary>Weapon is unlocked and can be equipped with the interaction key.</summary>
        Equippable,

        /// <summary>Weapon is locked and the player can afford it.</summary>
        Unlockable,

        /// <summary>Weapon is locked and the player cannot afford it.</summary>
        Insufficient
    }

    /// <summary>
    /// Assembles the full card content for <paramref name="weapon"/>. Produces exactly one
    /// <see cref="AbilityLine"/> per non-null ability on the weapon (preserving order), the weapon
    /// name and its family, and the state footer resolved from <paramref name="state"/>.
    /// </summary>
    /// <param name="weapon">The weapon asset to describe. When null, an empty card is returned.</param>
    /// <param name="state">Which state footer to show.</param>
    /// <param name="unlockCost">Coin cost, used for the unlock footer variants.</param>
    /// <param name="interactionKeyLabel">Label of the interaction key, used for the "equip" footer.</param>
    public static CardContent Build(WeaponScript weapon, StateKind state, int unlockCost, string interactionKeyLabel)
    {
        if (weapon == null)
        {
            return new CardContent(string.Empty, RunWeaponFamily.Gauntlet, FamilyLabel(RunWeaponFamily.Gauntlet),
                new List<AbilityLine>(), BuildStateLabel(state, unlockCost, interactionKeyLabel));
        }

        RunWeaponFamily family = IdentifyFamily(weapon);
        var lines = new List<AbilityLine>();
        if (weapon.abilities != null)
        {
            foreach (Ability ability in weapon.abilities)
            {
                if (ability == null) continue;
                string description = ability is ArsenalAbility arsenal ? arsenal.Description : null;
                lines.Add(new AbilityLine(ability.DisplayName, ability.ManaCost, ability.cooldownTime, description));
            }
        }

        return new CardContent(weapon.weaponName, family, FamilyLabel(family), lines,
            BuildStateLabel(state, unlockCost, interactionKeyLabel));
    }

    /// <summary>
    /// Resolves the weapon family without ever throwing on a partially-authored weapon. Mirrors
    /// <see cref="WeaponRunModifiers.Identify"/>, but tolerates a null <see cref="WeaponScript.abilities"/>
    /// array (an unfinished asset) so the card stays drawable instead of throwing.
    /// </summary>
    private static RunWeaponFamily IdentifyFamily(WeaponScript weapon)
    {
        if (weapon.FiresArrows) return RunWeaponFamily.Bow;
        if (weapon.abilities != null)
        {
            foreach (Ability skill in weapon.abilities)
                if (skill is BreakerGauntletAbility) return RunWeaponFamily.Gauntlet;
        }
        return RunWeaponFamily.Spear;
    }

    /// <summary>Portuguese family/style label shown on the card, matching the lobby wording.</summary>
    public static string FamilyLabel(RunWeaponFamily family)
    {
        switch (family)
        {
            case RunWeaponFamily.Bow: return "Arco";
            case RunWeaponFamily.Spear: return "Lança";
            default: return "Manopla";
        }
    }

    /// <summary>Builds the state footer text for the given state (design wording, no magic strings elsewhere).</summary>
    public static string BuildStateLabel(StateKind state, int unlockCost, string interactionKeyLabel)
    {
        switch (state)
        {
            case StateKind.Equipped:
                return "EQUIPADA";
            case StateKind.Unlockable:
                return "LIBERAR · " + unlockCost.ToString(CultureInfo.InvariantCulture) + " moedas";
            case StateKind.Insufficient:
                return "Moedas insuficientes";
            default:
                string key = string.IsNullOrEmpty(interactionKeyLabel) ? "?" : interactionKeyLabel;
                return "[" + key + "] Equipar";
        }
    }
}
