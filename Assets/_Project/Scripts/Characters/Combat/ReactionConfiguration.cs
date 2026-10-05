using System;
using System.Collections.Generic;
using UnityEngine;

public enum ReactionCategory { Unspecified = 0, SmallMob = 1, MediumMob = 2, Elite = 3, Boss = 4 }
public enum ReactionReapplication { Ignore = 0, Replace = 1, ExtendLimited = 2 }

/// <summary>Validation operates on raw authored values; runtime fallback never edits the asset.</summary>
public static class ReactionNumeric
{
    public static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    public static bool InRange(float value, float min, float max) => IsFinite(value) && value >= min && value <= max;
    public static float Safe(float value, float min, float max, float fallback) => InRange(value, min, max) ? value : fallback;
    public static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    public static void Validate(List<string> errors, string field, float value, float min, float max)
    {
        if (!InRange(value, min, max)) errors.Add($"{field}: expected finite value in [{min}, {max}], got {value}.");
    }
}

/// <summary>Optional overrides are explicit so old assets and intentional zero values are distinct.</summary>
[Serializable]
public sealed class ReactionAuthoring
{
    [SerializeField] private int _version;
    [SerializeField] private bool _overrideDefaultBreakEffect;
    [SerializeField] private StanceBreakEffect _defaultBreakEffect;
    [SerializeField] private bool _overrideKnockdownResistance;
    [SerializeField] private float _knockdownResistance;
    [SerializeField] private bool _overrideReapplication;
    [SerializeField] private ReactionReapplication _reapplication;
    [SerializeField] private float _knockdownFallSeconds = 0.2f;
    [SerializeField] private float _knockdownGroundSeconds = 0.8f;
    [SerializeField] private float _knockdownRecoverSeconds = 0.3f;
    [SerializeField] private float _defaultStunSeconds = 1f;
    [SerializeField] private float _defaultLaunchHeight = 2f;
    [SerializeField] private float _defaultLaunchSeconds = 1f;
    [SerializeField] private float _continuousControlLimitSeconds = 5f;
    [SerializeField] private float _interruptDecaySeconds = 2f;
    [SerializeField] private float _resumeWatchdogSeconds = 10f;
    [SerializeField] private bool _overrideVulnerabilitySeconds;
    [SerializeField] private float _vulnerabilitySeconds = 4f;
    [SerializeField] private bool _overrideBreakImmunitySeconds;
    [SerializeField] private float _breakImmunitySeconds = 1.5f;
    [SerializeField] private bool _allowBossLaunch;
    [SerializeField] private bool _allowBossKnockdown;
    [SerializeField] private bool _allowBossKnockback;

    public int Version => _version;
    public float FallSeconds => ReactionNumeric.Safe(_knockdownFallSeconds, 0.01f, 10f, 0.2f);
    public float GroundSeconds => ReactionNumeric.Safe(_knockdownGroundSeconds, 0.01f, 10f, 0.8f);
    public float RecoverSeconds => ReactionNumeric.Safe(_knockdownRecoverSeconds, 0.01f, 10f, 0.3f);
    public float StunSeconds => ReactionNumeric.Safe(_defaultStunSeconds, 0.01f, 10f, 1f);
    public float LaunchHeight => ReactionNumeric.Safe(_defaultLaunchHeight, 0.01f, DisplacementTier.KnockUpMax, 2f);
    public float LaunchSeconds => ReactionNumeric.Safe(_defaultLaunchSeconds, 0.01f, 10f, 1f);
    public float ContinuousLimit => ReactionNumeric.Safe(_continuousControlLimitSeconds, 0.01f, 10f, 5f);
    public float DecaySeconds => ReactionNumeric.Safe(_interruptDecaySeconds, 0.5f, 5f, 2f);
    public float WatchdogSeconds => ReactionNumeric.Safe(_resumeWatchdogSeconds, 0.01f, 10f, 10f);
    public bool AllowBossLaunch => _allowBossLaunch;
    public bool AllowBossKnockdown => _allowBossKnockdown;
    public bool AllowBossKnockback => _allowBossKnockback;

    public StanceBreakEffect DefaultBreakEffect(StanceBreakEffect fallback) =>
        _overrideDefaultBreakEffect && Enum.IsDefined(typeof(StanceBreakEffect), _defaultBreakEffect)
        && _defaultBreakEffect != StanceBreakEffect.Knockback ? _defaultBreakEffect : fallback;
    public float KnockdownResistance(float fallback) => _overrideKnockdownResistance
        ? ReactionNumeric.Safe(_knockdownResistance, 0f, 1f, fallback) : fallback;
    public ReactionReapplication Reapplication(ReactionReapplication fallback) => _overrideReapplication
        && Enum.IsDefined(typeof(ReactionReapplication), _reapplication) ? _reapplication : fallback;
    public float VulnerabilitySeconds(float fallback) => _overrideVulnerabilitySeconds
        ? ReactionNumeric.Safe(_vulnerabilitySeconds, VulnerabilityWindowBounds.MinSeconds, 10f, fallback) : fallback;
    public float BreakImmunitySeconds(float fallback) => _overrideBreakImmunitySeconds
        ? ReactionNumeric.Safe(_breakImmunitySeconds, 0.1f, 10f, fallback) : fallback;

    public void Validate(List<string> errors)
    {
        ReactionNumeric.Validate(errors, nameof(_knockdownFallSeconds), _knockdownFallSeconds, 0.01f, 10f);
        ReactionNumeric.Validate(errors, nameof(_knockdownGroundSeconds), _knockdownGroundSeconds, 0.01f, 10f);
        ReactionNumeric.Validate(errors, nameof(_knockdownRecoverSeconds), _knockdownRecoverSeconds, 0.01f, 10f);
        float total = _knockdownFallSeconds + _knockdownGroundSeconds + _knockdownRecoverSeconds;
        ReactionNumeric.Validate(errors, "Knockdown total duration", total, 0.03f, 10f);
        ReactionNumeric.Validate(errors, nameof(_defaultStunSeconds), _defaultStunSeconds, 0.01f, 10f);
        ReactionNumeric.Validate(errors, nameof(_defaultLaunchSeconds), _defaultLaunchSeconds, 0.01f, 10f);
        ReactionNumeric.Validate(errors, nameof(_defaultLaunchHeight), _defaultLaunchHeight, 0.01f, DisplacementTier.KnockUpMax);
        ReactionNumeric.Validate(errors, nameof(_continuousControlLimitSeconds), _continuousControlLimitSeconds, 0.01f, 10f);
        ReactionNumeric.Validate(errors, nameof(_interruptDecaySeconds), _interruptDecaySeconds, 0.5f, 5f);
        ReactionNumeric.Validate(errors, nameof(_resumeWatchdogSeconds), _resumeWatchdogSeconds, 0.01f, 10f);
        if (_overrideKnockdownResistance) ReactionNumeric.Validate(errors, nameof(_knockdownResistance), _knockdownResistance, 0f, 1f);
        if (_overrideVulnerabilitySeconds) ReactionNumeric.Validate(errors, nameof(_vulnerabilitySeconds), _vulnerabilitySeconds, 0.01f, 10f);
        if (_overrideBreakImmunitySeconds) ReactionNumeric.Validate(errors, nameof(_breakImmunitySeconds), _breakImmunitySeconds, 0.1f, 10f);
        if (_overrideDefaultBreakEffect && (!Enum.IsDefined(typeof(StanceBreakEffect), _defaultBreakEffect)
            || _defaultBreakEffect == StanceBreakEffect.Knockback)) errors.Add("Default break effect: undefined effect or implicit Knockback is forbidden.");
        if (_overrideReapplication && !Enum.IsDefined(typeof(ReactionReapplication), _reapplication)) errors.Add("Undefined reapplication policy.");
        if (_version < 0 || _version > 1) errors.Add("Reaction version must be 0 (legacy) or 1.");
    }
}

/// <summary>Resolved snapshot; no Unity object or mutable authoring data is held by the runtime model.</summary>
public sealed class ReactionConfiguration
{
    public EnemyRank Rank { get; }
    public ReactionCategory Category { get; }
    public bool IsBoss => Rank == EnemyRank.Boss || Category == ReactionCategory.Boss;
    public bool RestrictiveOpening => IsBoss || Rank != EnemyRank.Normal || Category == ReactionCategory.Elite;
    public StanceBreakEffect DefaultBreakEffect { get; }
    public ReactionReapplication Reapplication { get; }
    public float KnockdownResistance { get; }
    public float FallSeconds { get; }
    public float GroundSeconds { get; }
    public float RecoverSeconds { get; }
    public float StunSeconds { get; }
    public float LaunchHeight { get; }
    public float LaunchSeconds { get; }
    public float ContinuousLimit { get; }
    public float DecaySeconds { get; }
    public float WatchdogSeconds { get; }
    public float VulnerabilitySeconds { get; }
    public float BreakImmunitySeconds { get; }
    public bool AllowBossLaunch { get; }
    public bool AllowBossKnockdown { get; }
    public bool AllowBossKnockback { get; }

    public ReactionConfiguration(EnemyRank rank, ReactionCategory category = ReactionCategory.Unspecified,
        ReactionAuthoring authoring = null, float stunResistance = float.NaN,
        float vulnerabilitySeconds = float.NaN, float breakImmunitySeconds = float.NaN)
    {
        Rank = Enum.IsDefined(typeof(EnemyRank), rank) ? rank : EnemyRank.Normal;
        Category = ResolveCategory(Rank, category);
        var data = authoring ?? new ReactionAuthoring();
        var defaults = RankReactionDefaults.For(Rank);
        StanceBreakEffect defaultEffect = Category == ReactionCategory.SmallMob ? StanceBreakEffect.KnockUp
            : Category == ReactionCategory.MediumMob ? StanceBreakEffect.Knockdown : StanceBreakEffect.Stun;
        DefaultBreakEffect = data.DefaultBreakEffect(defaultEffect);
        Reapplication = data.Reapplication(Category == ReactionCategory.SmallMob ? ReactionReapplication.Replace : ReactionReapplication.Ignore);
        KnockdownResistance = data.KnockdownResistance(ReactionNumeric.Safe(stunResistance, 0f, 1f, defaults.StunResistance));
        bool validKnockdownTotal = data.FallSeconds + data.GroundSeconds + data.RecoverSeconds <= 10f;
        FallSeconds = validKnockdownTotal ? data.FallSeconds : 0.2f;
        GroundSeconds = validKnockdownTotal ? data.GroundSeconds : 0.8f;
        RecoverSeconds = validKnockdownTotal ? data.RecoverSeconds : 0.3f;
        StunSeconds = data.StunSeconds;
        LaunchHeight = data.LaunchHeight;
        LaunchSeconds = data.LaunchSeconds;
        ContinuousLimit = data.ContinuousLimit;
        DecaySeconds = data.DecaySeconds;
        WatchdogSeconds = data.WatchdogSeconds;
        VulnerabilitySeconds = data.VulnerabilitySeconds(ReactionNumeric.Safe(vulnerabilitySeconds, 0.01f, 10f, defaults.VulnerabilityWindowSeconds));
        BreakImmunitySeconds = data.BreakImmunitySeconds(ReactionNumeric.Safe(breakImmunitySeconds, 0.1f, 10f, defaults.BreakImmunitySeconds));
        AllowBossLaunch = data.AllowBossLaunch;
        AllowBossKnockdown = data.AllowBossKnockdown;
        AllowBossKnockback = data.AllowBossKnockback;
    }

    public static ReactionCategory ResolveCategory(EnemyRank rank, ReactionCategory category)
    {
        if (category >= ReactionCategory.SmallMob && category <= ReactionCategory.Boss) return category;
        return rank == EnemyRank.Boss ? ReactionCategory.Boss
            : rank == EnemyRank.Elite || rank == EnemyRank.Legendary ? ReactionCategory.Elite : ReactionCategory.SmallMob;
    }
}
