using UnityEngine;

/// <summary>
/// Pure, scene-independent per-<see cref="EnemyRank"/> reaction defaults (Requisitos 3.6, 3.7).
///
/// This is the data-driven fallback table applied when an enemy has no <see cref="EnemyProfile"/> to
/// source concrete stance/resistance numbers from. It is the single shared point that owns the
/// per-rarity stance pool, stance-damage multiplier, recovery, the four crowd-control resistances,
/// the post-break vulnerability-window duration (> 0, R3.3) and the post-break immunity window
/// (0.1–10.0 s, R2.6). Every value is derived from data here — there are no per-weapon numbers
/// (R3.6) — and the timing values are pinned to their shared ranges through
/// <see cref="StanceBreakBounds"/> / <see cref="VulnerabilityWindowBounds"/> so the invariants always
/// hold no matter which rank is requested.
///
/// It is a static table analogous to the controller's original <c>ApplyRankDefaults</c> switch
/// (the values are kept identical, only extended with the two timing fields), and it is a plain C#
/// type with no <see cref="MonoBehaviour"/> dependency so it can be exercised without a scene, per
/// the project rules (AGENTS.md). The owning <c>CombatReactionController</c> reads from here in
/// <c>ConfigureRank</c>, and <c>EnemyVariant</c> falls back to it (logging a stable missing-config
/// identifier) whenever no profile is assigned (R3.7).
/// </summary>
public static class RankReactionDefaults
{
    /// <summary>
    /// The immutable set of reaction defaults for a single <see cref="EnemyRank"/>. All values are
    /// already inside their shared valid ranges (the constructor clamps the timing fields), so a
    /// consumer can apply them directly without re-validating.
    /// </summary>
    public readonly struct Defaults
    {
        public readonly EnemyRank Rank;
        public readonly float MaxStance;
        public readonly float StanceDamageMultiplier;
        public readonly float StanceRecoveryPerSecond;
        public readonly float StanceRecoveryDelay;
        public readonly float StaggerResistance;
        public readonly float StunResistance;
        public readonly float KnockUpResistance;
        public readonly float KnockbackResistance;
        /// <summary>Post-break vulnerability-window duration, strictly &gt; 0 (R3.3).</summary>
        public readonly float VulnerabilityWindowSeconds;
        /// <summary>Post-break immunity window, clamped to [0.1; 10.0] s (R2.6).</summary>
        public readonly float BreakImmunitySeconds;

        public Defaults(
            EnemyRank rank,
            float maxStance,
            float stanceDamageMultiplier,
            float stanceRecoveryPerSecond,
            float stanceRecoveryDelay,
            float staggerResistance,
            float stunResistance,
            float knockUpResistance,
            float knockbackResistance,
            float vulnerabilityWindowSeconds,
            float breakImmunitySeconds)
        {
            Rank = rank;
            // Stance pool is floored the same way the controller floors it (Max(1, …)).
            MaxStance = Mathf.Max(1f, maxStance);
            // Pin every configurable value to its shared range so the table can never emit an
            // out-of-range default: multiplier in [0, 5] (R2.4), recovery delay in [0, 10] s (R2.7),
            // resistances in [0, 1], vulnerability window > 0 (R3.3), immunity in [0.1, 10] s (R2.6).
            StanceDamageMultiplier = StanceBreakBounds.ClampStanceDamageMultiplier(stanceDamageMultiplier);
            StanceRecoveryPerSecond = Mathf.Max(0f, stanceRecoveryPerSecond);
            StanceRecoveryDelay = StanceBreakBounds.ClampRecoveryDelaySeconds(stanceRecoveryDelay);
            StaggerResistance = Mathf.Clamp01(staggerResistance);
            StunResistance = Mathf.Clamp01(stunResistance);
            KnockUpResistance = Mathf.Clamp01(knockUpResistance);
            KnockbackResistance = Mathf.Clamp01(knockbackResistance);
            VulnerabilityWindowSeconds = VulnerabilityWindowBounds.ClampSeconds(vulnerabilityWindowSeconds);
            BreakImmunitySeconds = StanceBreakBounds.ClampBreakImmunitySeconds(breakImmunitySeconds);
        }
    }

    /// <summary>
    /// The per-rank defaults. The stance/resistance values are kept identical to the controller's
    /// original hard-coded <c>ApplyRankDefaults</c> switch (extend, don't recreate); the two timing
    /// fields (vulnerability window &gt; 0, break immunity 0.1–10.0 s) are added on top so the whole
    /// reaction shape for a rank is data-driven from one place.
    /// </summary>
    public static Defaults For(EnemyRank rank)
    {
        switch (rank)
        {
            case EnemyRank.Elite:
                return new Defaults(
                    EnemyRank.Elite,
                    maxStance: 220f, stanceDamageMultiplier: 0.8f,
                    stanceRecoveryPerSecond: 25f, stanceRecoveryDelay: 2.5f,
                    staggerResistance: 0.3f, stunResistance: 0.25f,
                    knockUpResistance: 0.4f, knockbackResistance: 0.3f,
                    vulnerabilityWindowSeconds: 4f, breakImmunitySeconds: 2f);

            case EnemyRank.Legendary:
                return new Defaults(
                    EnemyRank.Legendary,
                    maxStance: 400f, stanceDamageMultiplier: 0.6f,
                    stanceRecoveryPerSecond: 25f, stanceRecoveryDelay: 2.5f,
                    staggerResistance: 0.6f, stunResistance: 0.5f,
                    knockUpResistance: 0.75f, knockbackResistance: 0.6f,
                    vulnerabilityWindowSeconds: 3.5f, breakImmunitySeconds: 3f);

            case EnemyRank.Boss:
                return new Defaults(
                    EnemyRank.Boss,
                    maxStance: 900f, stanceDamageMultiplier: 0.5f,
                    stanceRecoveryPerSecond: 25f, stanceRecoveryDelay: 2.5f,
                    staggerResistance: 0.85f, stunResistance: 0.7f,
                    knockUpResistance: 1f, knockbackResistance: 1f,
                    vulnerabilityWindowSeconds: 3f, breakImmunitySeconds: 4f);

            case EnemyRank.Normal:
            default:
                return new Defaults(
                    EnemyRank.Normal,
                    maxStance: 100f, stanceDamageMultiplier: 1f,
                    stanceRecoveryPerSecond: 25f, stanceRecoveryDelay: 2.5f,
                    staggerResistance: 0f, stunResistance: 0f,
                    knockUpResistance: 0f, knockbackResistance: 0f,
                    vulnerabilityWindowSeconds: 4f, breakImmunitySeconds: 1.5f);
        }
    }

    /// <summary>
    /// A stable, human-readable identifier for a missing per-rank reaction configuration, used when an
    /// enemy has no <see cref="EnemyProfile"/> so the fallback is logged consistently (R3.7). The id is
    /// deterministic for a given rank (it does not depend on scene/instance names), so the same missing
    /// configuration always produces the same identifier across runs.
    /// </summary>
    public static string MissingConfigId(EnemyRank rank) => $"RankReactionDefaults.MissingProfile.{rank}";
}
