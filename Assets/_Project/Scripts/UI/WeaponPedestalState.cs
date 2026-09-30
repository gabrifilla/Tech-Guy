/// <summary>Action a weapon pedestal resolves to when the player interacts with it.</summary>
public enum PedestalAction
{
    /// <summary>Weapon is unlocked; interacting equips it.</summary>
    Equip,

    /// <summary>Weapon is locked but the player can afford it; interacting unlocks (then equips) it.</summary>
    Unlock,

    /// <summary>Weapon is locked and the player cannot afford it; interacting is denied.</summary>
    Denied
}

/// <summary>
/// Pure, Unity-free state of a single weapon pedestal, derived from <see cref="WeaponLoadout"/>.
///
/// The values (<see cref="IsUnlocked"/>, <see cref="UnlockCost"/>) are injected by the caller from
/// <see cref="WeaponLoadout"/> so this class has no Unity dependencies and stays testable in
/// EditMode without a scene. It concentrates the "locked/unlocked/equipped -> action" rule.
/// </summary>
public sealed class WeaponPedestalState
{
    /// <summary>Weapon index this pedestal represents (0 Gauntlet, 1 Bow, 2 Spear).</summary>
    public int WeaponIndex { get; }

    /// <summary>True when this pedestal's weapon is unlocked (from <see cref="WeaponLoadout.IsUnlocked"/>).</summary>
    public bool IsUnlocked { get; }

    /// <summary>Coin cost to unlock this pedestal's weapon (from <see cref="WeaponLoadout.GetCost"/>).</summary>
    public int UnlockCost { get; }

    /// <summary>Builds a pedestal state with explicit, caller-supplied values (keeps the class pure).</summary>
    public WeaponPedestalState(int weaponIndex, bool isUnlocked, int unlockCost)
    {
        WeaponIndex = weaponIndex;
        IsUnlocked = isUnlocked;
        UnlockCost = unlockCost;
    }

    /// <summary>Builds a pedestal state for <paramref name="weaponIndex"/> by reading <see cref="WeaponLoadout"/>.</summary>
    public static WeaponPedestalState FromLoadout(int weaponIndex) =>
        new WeaponPedestalState(weaponIndex, WeaponLoadout.IsUnlocked(weaponIndex), WeaponLoadout.GetCost(weaponIndex));

    /// <summary>True when the currently equipped weapon index matches this pedestal.</summary>
    public bool IsEquipped(int equippedIndex) => equippedIndex == WeaponIndex;

    /// <summary>
    /// Decides the action when the player interacts with this pedestal:
    /// <see cref="PedestalAction.Equip"/> when unlocked;
    /// <see cref="PedestalAction.Unlock"/> when locked and <paramref name="canAfford"/>;
    /// <see cref="PedestalAction.Denied"/> when locked and cannot afford.
    /// Never returns <see cref="PedestalAction.Unlock"/> without enough coins.
    /// </summary>
    public PedestalAction Resolve(bool canAfford)
    {
        if (IsUnlocked) return PedestalAction.Equip;
        return canAfford ? PedestalAction.Unlock : PedestalAction.Denied;
    }
}
