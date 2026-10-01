using System;
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

    /// <summary>
    /// Preloaded weapons keyed by index, filled once at boot so the swap path never hits
    /// <c>Resources.Load</c> at runtime (R5.1). Reset between Editor plays via
    /// <see cref="PoolResetRegistry"/> so stale static state cannot leak across play sessions.
    /// </summary>
    private static WeaponCache _cache;

    static WeaponLoadout()
    {
        // Drop the cache at the start of each play / on domain reload (same pattern as the pools and
        // CombatBalance) so a weapon preloaded in a previous play cannot leak into the next one.
        PoolResetRegistry.Register(() => _cache = null);
    }

    /// <summary>
    /// Boot hook: preload the three weapons once, before the first scene loads, so the first weapon
    /// swap in combat does not trigger a synchronous disk load (R5.1). If a path is invalid the cache
    /// records a null for that slot and the lookup falls back to on-demand <c>Resources.Load</c>
    /// (R5.5); the preload never aborts combat.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PreloadWeapons() => EnsureCache();

    /// <summary>Builds the cache on demand if the boot hook has not run (e.g. after a reset).</summary>
    private static WeaponCache EnsureCache() => _cache ??= WeaponCache.Preload(ResourcePaths, LoadFromResources);

    /// <summary>Resolves a weapon by index, preferring the preloaded cache and falling back to a fresh load.</summary>
    private static WeaponScript Resolve(int index) => EnsureCache().Get(index, LoadFromResources);

    /// <summary>
    /// The on-demand loader. Used to populate the cache at boot and as the fallback when a preload
    /// slot is missing. A failed load logs a clear warning and returns null so callers degrade
    /// gracefully instead of crashing combat (R5.5).
    /// </summary>
    private static WeaponScript LoadFromResources(int index)
    {
        if (index < 0 || index >= ResourcePaths.Length) return null;
        var weapon = Resources.Load<WeaponScript>(ResourcePaths[index]);
        if (!weapon)
            Debug.LogWarning($"[WeaponLoadout] Could not load weapon at '{ResourcePaths[index]}' (index {index}); falling back to on-demand loading.");
        return weapon;
    }

    public static WeaponScript LoadSelected()
    {
        int index = Mathf.Clamp(PlayerPrefs.GetInt(PreferenceKey, 0), 0, ResourcePaths.Length - 1);
        if (!IsUnlocked(index)) index = 0;
        return Resolve(index);
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
        var weapon = Resolve(index);
        if (!weapon || !actor.HandTransform) return false;
        if (actor.TryGetComponent(out AbilityHolder holder) && holder.IsCasting) return false;
        if (actor.TryGetComponent(out CharControlScript control)) control.CancelCombo();
        actor.EquipWeapon(weapon);
        PlayerPrefs.SetInt(PreferenceKey, index);
        PlayerPrefs.Save();
        return true;
    }

    /// <summary>
    /// Pure index → <see cref="WeaponScript"/> cache. Keeps the preload/lookup/fallback logic free of
    /// Unity so it can be property-tested (task 9.3 / design Property 7): for every valid index the
    /// <see cref="Get"/> is idempotent and does not reload after a successful preload, while an
    /// out-of-range index falls back to index 0. The loader is injected so tests can supply a counting
    /// stub instead of <c>Resources.Load</c>.
    /// </summary>
    /// <remarks>Feature: project-cleanup-optimization, task 9.1. Requirements: 5.1, 5.3, 5.4, 5.5.</remarks>
    public sealed class WeaponCache
    {
        private readonly WeaponScript[] _weapons;

        private WeaponCache(WeaponScript[] weapons) => _weapons = weapons;

        /// <summary>Number of cached slots (one per resource path).</summary>
        public int Count => _weapons.Length;

        /// <summary>
        /// Builds a cache by loading every path once through <paramref name="loader"/>. A slot whose
        /// load fails stays null and is resolved on demand by <see cref="Get"/> (R5.5).
        /// </summary>
        public static WeaponCache Preload(string[] paths, Func<int, WeaponScript> loader)
        {
            int length = paths?.Length ?? 0;
            var weapons = new WeaponScript[length];
            if (loader != null)
                for (int i = 0; i < length; i++)
                    weapons[i] = loader(i);
            return new WeaponCache(weapons);
        }

        /// <summary>
        /// Returns the cached weapon for <paramref name="index"/>. An out-of-range index falls back to
        /// index 0 (R5.5). A cached hit is returned without reloading (idempotent); a missing slot is
        /// loaded once through <paramref name="loader"/> and memoized so a later call does not reload.
        /// </summary>
        public WeaponScript Get(int index, Func<int, WeaponScript> loader)
        {
            if (_weapons.Length == 0) return null;
            if (index < 0 || index >= _weapons.Length) index = 0;
            if (!_weapons[index] && loader != null) _weapons[index] = loader(index);
            return _weapons[index];
        }
    }
}
