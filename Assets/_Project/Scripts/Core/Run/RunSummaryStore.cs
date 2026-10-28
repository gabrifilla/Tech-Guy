using System;
using System.Linq;
using UnityEngine;

/// <summary>One versioned result in the existing PlayerPrefs persistence, independent of permanent save keys.</summary>
public static class RunSummaryStore
{
    public const string PreferenceKey = "TechGuy.RunSummary.v1";
    public static bool TryRead(out RunSummary summary, out string diagnostic)
    {
        summary = null; diagnostic = "No completed run summary.";
        if (!PlayerPrefs.HasKey(PreferenceKey)) return false;
        try
        {
            var value = JsonUtility.FromJson<RunSummary>(PlayerPrefs.GetString(PreferenceKey));
            if (!Valid(value)) { diagnostic = "Invalid or unsupported run summary; permanent saves are unchanged."; return false; }
            summary = value; diagnostic = null; return true;
        }
        catch (Exception ex) { diagnostic = ex.GetType().Name; return false; }
    }
    internal static bool TryWrite(RunSummary summary, out string diagnostic)
    {
        diagnostic = null;
        if (!Valid(summary)) { diagnostic = "Refused invalid run summary."; return false; }
        try { PlayerPrefs.SetString(PreferenceKey, JsonUtility.ToJson(summary)); PlayerPrefs.Save(); return true; }
        catch (Exception ex) { diagnostic = ex.GetType().Name; return false; }
    }
    private static bool Valid(RunSummary s) => s != null && s.SchemaVersion == 1 && s.Finalized &&
        Guid.TryParse(s.SessionId, out _) && (s.Outcome == "Victory" || s.Outcome == "Death" || s.Outcome == "Aborted") &&
        Finite(s.EffectiveDamage) && Finite(s.ObservedEnemyHpLoss) && Finite(s.UnattributedEnemyHpLoss) &&
        Finite(s.LargestImpact) && Finite(s.Overkill) && s.ObserverErrors >= 0 &&
        s.EnemiesDefeated >= 0 && s.PlayerCreditedKills >= 0 && s.PlayerCreditedKills <= s.EnemiesDefeated &&
        s.Abilities != null && s.Abilities.All(x => x != null && Finite(x.EffectiveIncludingChildren) && x.Uses >= 0 && x.NativeHpTransactions >= 0 && x.SecondaryHpTransactions >= 0) &&
        Finite(s.DamageReceived) && Finite(s.WallSeconds) && Finite(s.ScaledSeconds) && s.Damage != null &&
        s.Damage.All(x => x != null && Finite(x.Effective) && Finite(x.Overkill)) &&
        Math.Abs(s.Damage.Where(x => x.PlayerAttributed).Sum(x => x.Effective) - s.EffectiveDamage) < .001 &&
        Math.Abs(s.Damage.Sum(x => x.Effective) - s.ObservedEnemyHpLoss) < .001 &&
        Math.Abs(s.EffectiveDamage + s.UnattributedEnemyHpLoss - s.ObservedEnemyHpLoss) < .001;
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
}
