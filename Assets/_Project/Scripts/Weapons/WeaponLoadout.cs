using UnityEngine;

/// <summary>Persists only an allow-listed resource identifier, never a scene object.</summary>
public static class WeaponLoadout
{
    public const string PreferenceKey = "TechGuy.Loadout.Weapon";
    private const string UnlockKeyPrefix = "TechGuy.Loadout.Unlocked.";
    public static readonly string[] ResourcePaths =
    {
        "Weapons/Melee/Gauntlet/Gauntlet", "Weapons/Ranged/Bow_arrow/Bow", "Weapons/Melee/Spear/Spear"
    };

    /// <summary>
    /// Coin price for each weapon index. Index 0 (the gauntlet) is free and starts unlocked.
    /// Kept low on purpose: coins are rare (mobs drop occasionally, bosses give 2-3), so a
    /// few runs should be enough to afford the next weapon.
    /// </summary>
    public static readonly int[] UnlockCosts = { 0, 15, 30 };

    public static WeaponScript LoadSelected()
    {
        int index = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, ResourcePaths.Length - 1);
        if (!IsUnlocked(index)) index = 0;
        return Resources.Load<WeaponScript>(ResourcePaths[index]);
    }

    /// <summary>True when the weapon at <paramref name="index"/> has been unlocked. Index 0 is always unlocked.</summary>
    public static bool IsUnlocked(int index)
    {
        if (index < 0 || index >= ResourcePaths.Length) return false;
        if (index == 0) return true;
        return PlayerPrefs.GetInt(UnlockKeyPrefix + index, 0) == 1;
    }

    /// <summary>Coin cost to unlock the weapon at <paramref name="index"/>.</summary>
    public static int GetCost(int index) =>
        index >= 0 && index < UnlockCosts.Length ? Mathf.Max(0, UnlockCosts[index]) : 0;

    /// <summary>Spends coins from the wallet to unlock a weapon. Returns true on success.</summary>
    public static bool TryUnlock(int index)
    {
        if (index < 0 || index >= ResourcePaths.Length) return false;
        if (IsUnlocked(index)) return true;
        if (!CurrencyWallet.TrySpend(GetCost(index))) return false;
        PlayerPrefs.SetInt(UnlockKeyPrefix + index, 1);
        PlayerPrefs.Save();
        return true;
    }

    public static bool Select(PlayerActor actor, int index)
    {
        if (!actor || actor.IsDead || index < 0 || index >= ResourcePaths.Length) return false;
        if (!IsUnlocked(index)) return false;
        var weapon = Resources.Load<WeaponScript>(ResourcePaths[index]);
        if (!weapon || !actor.HandTransform) return false;
        if (actor.TryGetComponent(out AbilityHolder holder) && holder.IsCasting) return false;
        if (actor.TryGetComponent(out CharControlScript control)) control.CancelCombo();
        actor.EquipWeapon(weapon);
        PlayerPrefs.SetInt(PreferenceKey, index);
        PlayerPrefs.Save();
        return true;
    }
}
