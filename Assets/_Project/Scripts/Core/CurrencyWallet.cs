using System;
using UnityEngine;

/// <summary>
/// Persistent, cross-run coin balance. Coins dropped by enemies are banked here and
/// spent in the lobby to unlock weapons. Backed by PlayerPrefs, mirroring WeaponLoadout.
/// </summary>
public static class CurrencyWallet
{
    public const string PreferenceKey = "TechGuy.Currency.Coins";

    /// <summary>Raised whenever the balance changes, passing the new total.</summary>
    public static event Action<int> BalanceChanged;

    /// <summary>Current banked coin total, never negative.</summary>
    public static int Balance => Mathf.Max(0, PlayerPrefs.GetInt(PreferenceKey, 0));

    /// <summary>Adds coins to the balance. Non-positive amounts are ignored.</summary>
    public static void Add(int amount)
    {
        if (amount <= 0) return;
        SetBalance(Balance + amount);
    }

    /// <summary>Returns true when the balance can cover <paramref name="cost"/>.</summary>
    public static bool CanAfford(int cost) => cost >= 0 && Balance >= cost;

    /// <summary>Spends coins if affordable, returning true on success.</summary>
    public static bool TrySpend(int cost)
    {
        if (cost < 0 || !CanAfford(cost)) return false;
        SetBalance(Balance - cost);
        return true;
    }

    private static void SetBalance(int value)
    {
        value = Mathf.Max(0, value);
        PlayerPrefs.SetInt(PreferenceKey, value);
        PlayerPrefs.Save();
        BalanceChanged?.Invoke(value);
    }
}
