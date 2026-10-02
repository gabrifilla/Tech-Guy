using UnityEngine;

/// <summary>
/// Shared, non-mutating validation of an authored <see cref="CombatActionProfile"/> for use from
/// the <c>OnValidate</c> of assets that embed one (<c>Ability</c>, <c>WeaponScript</c>) (R2.3,
/// R3.10, R4.8, R8.7). It flags invalid phase boundaries and cancel windows through
/// <see cref="Debug.LogWarning(object)"/> and keeps the last valid authored values untouched — it
/// never writes back to the profile (no silent mutation). The pure Núcleo_Compartilhado validators
/// (<see cref="ActionTimeline.Validate"/>, <see cref="CancelRule.Validate"/>) remain the single
/// source of truth; this helper only routes to them and reports.
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 12.2. Requirements: R2.3, R3.10, R4.8, R8.7.</remarks>
public static class CombatProfileValidation
{
    /// <summary>
    /// Validates <paramref name="profile"/> and emits warnings for any invalid authored values,
    /// without mutating the profile. A <c>null</c> profile (older assets authored before the field
    /// existed) is skipped gracefully.
    /// </summary>
    /// <param name="profile">The authored profile to validate; may be <c>null</c>.</param>
    /// <param name="ownerName">The owning asset's name, used to prefix warning messages.</param>
    public static void Validate(CombatActionProfile profile, string ownerName)
    {
        if (profile == null)
        {
            return;
        }

        // Phase boundaries: flag invalid values but keep the authored fields (no silent mutation) (R2.3, R8.7).
        if (!ActionTimeline.Validate(profile.StartupEnd, profile.ActiveEnd, out string timelineError))
        {
            Debug.LogWarning($"[{ownerName}] CombatActionProfile: {timelineError}");
        }

        // Cancel windows: flag each invalid window by its target, keeping authored values (R4.8, R8.7).
        var cancelRules = profile.CancelRules;
        if (cancelRules != null)
        {
            for (int i = 0; i < cancelRules.Count; i++)
            {
                CancelRuleData rule = cancelRules[i];
                if (!CancelRule.Validate(rule.Start, rule.End, out string cancelError))
                {
                    Debug.LogWarning($"[{ownerName}] CombatActionProfile cancel rule ({rule.Target}): {cancelError}");
                }
            }
        }

        // Share the single commitment resolution path; warn if an absent category defaulted to Committed (R3.10).
        profile.ResolveCommitment(out bool emitWarning);
        if (emitWarning)
        {
            Debug.LogWarning($"[{ownerName}] CombatActionProfile: commitment category absent; resolved to Committed.");
        }
    }
}
