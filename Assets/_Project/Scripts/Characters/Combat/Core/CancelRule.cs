using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The three destinations a cancel can target (R4.1). An Ação_Ofensiva may authorize a cancel
/// into a dash, into a basic attack, or into a skill — each governed by its own independent
/// <see cref="CancelRule"/>. There is deliberately no implicit ordering between destinations:
/// what is cancelable, and when, comes entirely from the authored rules, never from category.
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 4.1. Requirements: R4.1, R4.2.</remarks>
public enum CancelTarget
{
    /// <summary>Cancel into a dash (also covers Channel-into-dash via a Dash rule over Active).</summary>
    Dash,

    /// <summary>Cancel into a basic attack.</summary>
    Basic,

    /// <summary>Cancel into a skill.</summary>
    Skill
}

/// <summary>
/// An independent, per-destination cancel window on an Ação_Ofensiva's normalized <c>[0, 1]</c>
/// timeline (R4.1). It replaces the old single <c>CancelWindow</c> with three booleans: each
/// destination carries its own <see cref="CancelRule"/>, and the <em>absence</em> of a rule for a
/// destination means a cancel into that destination is forbidden (R4.2, enforced by
/// <see cref="CancelRuleSet"/>).
///
/// <para>
/// Being a pure, scene-free (non-<c>MonoBehaviour</c>) <c>readonly struct</c>, it is deterministic
/// and property-testable in isolation (R8.7). The runtime constructor clamps its inputs to
/// <c>0 &lt;= Start &lt;= End &lt;= 1</c> as a defense so a bad asset can never leave the combat
/// loop in an inconsistent state; editor-time rejection of invalid design data (without silent
/// mutation) is handled separately by <see cref="Validate"/> (R4.8), mirroring
/// <see cref="ActionTimeline"/>.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 4.1. Requirements: R4.1, R4.2, R4.8.</remarks>
public readonly struct CancelRule
{
    /// <summary>The destination this rule authorizes a cancel into.</summary>
    public CancelTarget Target { get; }

    /// <summary>
    /// Start of the open window, as a fraction of the timeline. Always in <c>[0, 1]</c> and never
    /// greater than <see cref="End"/>.
    /// </summary>
    public float Start { get; }

    /// <summary>
    /// End of the open window, as a fraction of the timeline. Always in <c>[0, 1]</c> and never
    /// less than <see cref="Start"/>.
    /// </summary>
    public float End { get; }

    /// <summary>
    /// Builds a cancel rule, clamping the bounds to satisfy <c>0 &lt;= Start &lt;= End &lt;= 1</c>
    /// as a runtime defense (R4.8). Each bound is first clamped to <c>[0, 1]</c>; <see cref="End"/>
    /// is then raised to at least <see cref="Start"/> so the window never has negative length.
    /// </summary>
    /// <param name="target">The destination this rule authorizes a cancel into.</param>
    /// <param name="start">Desired start of the open window.</param>
    /// <param name="end">Desired end of the open window.</param>
    public CancelRule(CancelTarget target, float start, float end)
    {
        // Mathf.Clamp01 leaves NaN untouched (NaN compares false against both bounds), so sanitize
        // NaN to 0 first; otherwise a NaN bound from a corrupt asset would survive the clamp and
        // break the 0 <= Start <= End <= 1 runtime invariant (R4.8 safe fallback), consistent with
        // ActionTimeline.
        float clampedStart = Mathf.Clamp01(float.IsNaN(start) ? 0f : start);
        float clampedEnd = Mathf.Clamp01(float.IsNaN(end) ? 0f : end);

        if (clampedEnd < clampedStart)
        {
            clampedEnd = clampedStart;
        }

        Target = target;
        Start = clampedStart;
        End = clampedEnd;
    }

    /// <summary>
    /// Returns <c>true</c> when the cancel window is open at <paramref name="progress"/>, i.e. when
    /// <c>Start &lt;= progress &lt;= End</c> (inclusive on both bounds) (R4.1, R4.3). Positions
    /// outside <c>[Start, End]</c> return <c>false</c>.
    /// </summary>
    /// <param name="progress">Normalized position along the timeline.</param>
    /// <returns><c>true</c> when the window is open at <paramref name="progress"/>.</returns>
    public bool IsOpenAt(float progress)
    {
        return progress >= Start && progress <= End;
    }

    /// <summary>
    /// Non-throwing editor validation of raw design bounds against the invariant
    /// <c>0 &lt;= start &lt;= end &lt;= 1</c> (R4.8). Returns <c>true</c> with an empty
    /// <paramref name="error"/> when valid; otherwise returns <c>false</c> and sets
    /// <paramref name="error"/> to a clear message naming the offending bound, without mutating the
    /// design data. Callers (e.g. <c>OnValidate</c>) keep the last valid values on failure.
    /// </summary>
    /// <param name="start">The authored start of the window to validate.</param>
    /// <param name="end">The authored end of the window to validate.</param>
    /// <param name="error">Receives a human-readable description of the invalid bound, or an empty
    /// string when the values are valid.</param>
    /// <returns><c>true</c> when the bounds satisfy the invariant; otherwise <c>false</c>.</returns>
    public static bool Validate(float start, float end, out string error)
    {
        // NaN and ±Infinity are never in [0, 1]: every comparison against NaN is false, so the
        // range guards below would let a NaN slip through and wrongly report "valid". Reject any
        // non-finite bound up front so Validate agrees with the invariant 0 <= start <= end <= 1
        // (R4.8). The runtime constructor's own NaN→0 / clamp defense is unaffected.
        if (!IsFinite(start))
        {
            error = $"start ({start}) must be a finite number within [0, 1].";
            return false;
        }

        if (!IsFinite(end))
        {
            error = $"end ({end}) must be a finite number within [0, 1].";
            return false;
        }

        if (start < 0f || start > 1f)
        {
            error = $"start ({start}) must be within [0, 1].";
            return false;
        }

        if (end < 0f || end > 1f)
        {
            error = $"end ({end}) must be within [0, 1].";
            return false;
        }

        if (start > end)
        {
            error = $"start ({start}) must be less than or equal to end ({end}).";
            return false;
        }

        error = string.Empty;
        return true;
    }

    /// <summary>
    /// True when <paramref name="value"/> is a real, finite number (not NaN and not ±Infinity).
    /// Used instead of <c>float.IsFinite</c> so the check works across every Unity scripting
    /// runtime/profile.
    /// </summary>
    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

/// <summary>
/// A per-action collection of <see cref="CancelRule"/>s holding <em>at most one</em> rule per
/// <see cref="CancelTarget"/> (R4.1). The absence of a rule for a destination means a cancel into
/// that destination is <em>forbidden</em> (R4.2) — this is the single source of truth consumed by
/// the <c>CancelResolver</c>, so there are no hidden exceptions in the coordinator.
///
/// <para>
/// Being a pure, scene-free (non-<c>MonoBehaviour</c>) type, it is deterministic and
/// property-testable in isolation (R8.7). When fed multiple rules for the same destination (e.g.
/// from a mis-authored <c>cancelRules</c> array), the last one wins so a single, well-defined rule
/// is retained per destination.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 4.1. Requirements: R4.1, R4.2, R4.8.</remarks>
public sealed class CancelRuleSet
{
    private readonly Dictionary<CancelTarget, CancelRule> _rules = new Dictionary<CancelTarget, CancelRule>();

    /// <summary>
    /// Creates an empty rule set — no destination is cancelable until a rule is added (R4.2).
    /// </summary>
    public CancelRuleSet()
    {
    }

    /// <summary>
    /// Creates a rule set from a collection of rules, keeping at most one rule per destination
    /// (last one wins on duplicates). A <c>null</c> collection yields an empty set. Mirrors the
    /// authored <c>cancelRules</c> array in <c>CombatActionProfile</c> (R4.1).
    /// </summary>
    /// <param name="rules">The rules to seed the set with, or <c>null</c> for an empty set.</param>
    public CancelRuleSet(IEnumerable<CancelRule> rules)
    {
        if (rules == null)
        {
            return;
        }

        foreach (CancelRule rule in rules)
        {
            Add(rule);
        }
    }

    /// <summary>
    /// Adds or replaces the rule for <paramref name="rule"/>'s <see cref="CancelRule.Target"/>,
    /// guaranteeing at most one rule per destination (R4.1).
    /// </summary>
    /// <param name="rule">The rule to store, keyed by its target.</param>
    public void Add(CancelRule rule)
    {
        _rules[rule.Target] = rule;
    }

    /// <summary>
    /// Returns <c>true</c> when a rule exists for <paramref name="target"/>; otherwise <c>false</c>
    /// (meaning cancel into that destination is forbidden) (R4.2).
    /// </summary>
    /// <param name="target">The destination to query.</param>
    /// <returns><c>true</c> when a rule exists for <paramref name="target"/>.</returns>
    public bool HasRule(CancelTarget target)
    {
        return _rules.ContainsKey(target);
    }

    /// <summary>
    /// Retrieves the rule for <paramref name="target"/>. Returns <c>true</c> and sets
    /// <paramref name="rule"/> when a rule exists; otherwise returns <c>false</c> (cancel forbidden)
    /// with <paramref name="rule"/> set to its <c>default</c> value (R4.2).
    /// </summary>
    /// <param name="target">The destination to query.</param>
    /// <param name="rule">Receives the stored rule when present; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> when a rule exists for <paramref name="target"/>.</returns>
    public bool TryGet(CancelTarget target, out CancelRule rule)
    {
        return _rules.TryGetValue(target, out rule);
    }
}
