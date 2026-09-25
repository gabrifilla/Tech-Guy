using UnityEngine;

/// <summary>Persists only an allow-listed resource identifier, never a scene object.</summary>
public static class WeaponLoadout
{
    public const string PreferenceKey = "TechGuy.Loadout.Weapon";
    public static readonly string[] ResourcePaths =
    {
        "Weapons/Melee/Gauntlet/Gauntlet", "Weapons/Ranged/Bow_arrow/Bow", "Weapons/Melee/Spear/Spear"
    };

    public static WeaponScript LoadSelected()
    {
        int index = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, ResourcePaths.Length - 1);
        return Resources.Load<WeaponScript>(ResourcePaths[index]);
    }

    public static bool Select(PlayerActor actor, int index)
    {
        if (!actor || actor.IsDead || index < 0 || index >= ResourcePaths.Length) return false;
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
