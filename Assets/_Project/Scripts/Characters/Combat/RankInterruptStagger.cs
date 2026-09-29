using UnityEngine;

/// <summary>
/// Pure, scene-free accumulator for the rank-resisted interruption stagger rule (Requisito 7.8 /
/// Property 24).
///
/// WHERE an enemy declares stance resistance by rarity, an interruption ability does not stagger or
/// break it outright: instead each interruption's stagger value is <b>accumulated</b>, and stagger or
/// a stance break is applied only once the running total reaches or exceeds the rank's threshold.
/// Below the threshold the enemy's state is preserved (no stagger, no break) — the total simply grows.
/// The moment the threshold is met the accumulator fires and the consumed amount is subtracted, so a
/// carried-over remainder starts the next cycle (repeated interruptions keep breaking on schedule).
///
/// The per-rank threshold is derived from data, not per-weapon numbers: a base amount scaled up for
/// tougher rarities (Normal &lt; Elite &lt; Legendary &lt; Boss) and further by the enemy's stance
/// resistance in [0, 1] (a fully resistant, 1.0, enemy needs the full rank threshold; a 0-resistance
/// enemy needs the minimum). This mirrors how the rest of the reaction system reads rank/resistance
/// values (see <c>RankReactionDefaults</c> / <c>BreakEffectResistance</c>) and is kept as a plain C#
/// class — no <see cref="MonoBehaviour"/> — so Property 24 can drive it without a live scene. The
/// owning <see cref="CombatReactionController"/> holds one instance per enemy and feeds it the
/// interruption stagger values it receives.
/// </summary>
public sealed class RankInterruptStagger
{
    /// <summary>Baseline interruption stagger a Normal, zero-resistance enemy needs to stagger/break.</summary>
    public const float BaseThreshold = 1f;

    /// <summary>Lowest fraction of the rank threshold ever required, so a 0-resistance enemy still needs a real amount.</summary>
    public const float MinResistanceFactor = 0.5f;

    private EnemyRank rank;
    private float resistance;
    private float accumulated;

    /// <summary>The interruption stagger accumulated so far but not yet spent on a stagger/break.</summary>
    public float Accumulated => accumulated;

    /// <summary>The enemy's rank, which scales the accumulation threshold.</summary>
    public EnemyRank Rank => rank;

    /// <summary>The enemy's stance resistance in [0, 1], which scales the threshold within its rank.</summary>
    public float Resistance => resistance;

    /// <summary>The amount of accumulated interruption stagger that triggers a stagger/break (Requisito 7.8).</summary>
    public float Threshold => ThresholdFor(rank, resistance);

    /// <summary>The classification of a single <see cref="Accumulate"/> call.</summary>
    public enum Outcome
    {
        /// <summary>Below threshold: state preserved, nothing applied (Requisito 7.8).</summary>
        Preserved,
        /// <summary>Threshold reached or exceeded: stagger/break applied this call (Requisito 7.8).</summary>
        StaggerOrBreak
    }

    /// <summary>Creates an accumulator for a given rank and stance resistance (clamped to [0, 1]).</summary>
    public RankInterruptStagger(EnemyRank rank = EnemyRank.Normal, float resistance = 0f)
    {
        this.rank = rank;
        this.resistance = Mathf.Clamp01(resistance);
        accumulated = 0f;
    }

    /// <summary>
    /// Updates the rank and stance resistance used to derive the threshold. Any already-accumulated
    /// stagger is preserved so a re-configuration mid-fight does not reset progress toward a break.
    /// </summary>
    public void Configure(EnemyRank newRank, float newResistance)
    {
        rank = newRank;
        resistance = Mathf.Clamp01(newResistance);
    }

    /// <summary>
    /// Accumulates one interruption's <paramref name="staggerValue"/> and decides whether a stagger or
    /// stance break fires this call (Requisito 7.8 / Property 24).
    ///
    /// A non-positive value contributes nothing and preserves state. Otherwise the value is added to
    /// the running total; if the total reaches or exceeds the rank threshold the accumulator fires
    /// (<see cref="Outcome.StaggerOrBreak"/>) and the threshold amount is consumed from the total,
    /// carrying any remainder into the next cycle. Below the threshold the enemy's state is preserved
    /// (<see cref="Outcome.Preserved"/>) and only the running total grows.
    /// </summary>
    /// <param name="staggerValue">The interruption's stagger contribution (non-positive is ignored).</param>
    /// <returns>Whether this call triggered a stagger/break or preserved the enemy's state.</returns>
    public Outcome Accumulate(float staggerValue)
    {
        if (staggerValue > 0f) accumulated += staggerValue;

        float threshold = Threshold;
        if (threshold <= 0f) return Outcome.Preserved; // guard against a non-positive threshold.
        if (accumulated + 1e-6f < threshold) return Outcome.Preserved;

        // Threshold met: consume as many whole thresholds as the running total covers so a single
        // large interruption never leaves a full, un-fired threshold in the carried remainder
        // (repeated interruptions keep breaking on schedule). The remainder always ends < threshold.
        bool fired = false;
        while (accumulated + 1e-6f >= threshold)
        {
            accumulated -= threshold;
            fired = true;
        }
        if (accumulated < 0f) accumulated = 0f;
        return fired ? Outcome.StaggerOrBreak : Outcome.Preserved;
    }

    /// <summary>Clears the accumulated stagger (e.g. when the enemy fully recovers between engagements).</summary>
    public void Reset() => accumulated = 0f;

    /// <summary>
    /// The interruption-stagger threshold for a rank at a given stance resistance (Requisito 7.8): a
    /// per-rank base scaled by the resistance from <see cref="MinResistanceFactor"/> (at resistance 0)
    /// up to the full rank base (at resistance 1). Tougher rarities have a strictly higher base, so a
    /// Boss always needs more accumulated interruption stagger than a Normal at the same resistance.
    /// </summary>
    public static float ThresholdFor(EnemyRank rank, float resistance)
    {
        float clampedResistance = Mathf.Clamp01(resistance);
        float rankBase = BaseThreshold * RankScale(rank);
        float resistanceFactor = Mathf.Lerp(MinResistanceFactor, 1f, clampedResistance);
        return rankBase * resistanceFactor;
    }

    /// <summary>
    /// The strictly-increasing per-rank multiplier applied to <see cref="BaseThreshold"/>: Normal 1,
    /// Elite 2, Legendary 3, Boss 4. Higher rarities accumulate a proportionally larger threshold.
    /// </summary>
    public static float RankScale(EnemyRank rank)
    {
        switch (rank)
        {
            case EnemyRank.Boss: return 4f;
            case EnemyRank.Legendary: return 3f;
            case EnemyRank.Elite: return 2f;
            case EnemyRank.Normal:
            default: return 1f;
        }
    }
}
