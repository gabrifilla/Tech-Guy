using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The hit-stop class of an ImpactEvent's grouped feedback (R7.4, R7.5). It tags <em>what kind</em>
/// of impact a <see cref="HitStopProfile"/> represents so authored profiles can distinguish the
/// routine mid-combo pause from the heavier finisher pause and from a secondary/incidental impact.
/// It carries no gameplay weight by itself — it is a cosmetic classification feeding the hit-stop
/// decision in <see cref="HitStop"/> / <see cref="HitStopGrouping"/>.
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 12.1. Requirements: R7.4, R7.5.</remarks>
public enum HitStopClass
{
    /// <summary>Routine mid-combo impact (e.g. the individual punches of a Manopla combo).</summary>
    Intermediate,

    /// <summary>Combo/sequence finisher impact, authored for a heavier pause.</summary>
    Finisher,

    /// <summary>Secondary or incidental impact (e.g. a splash/pulse that is not the primary hit).</summary>
    Secondary
}

/// <summary>
/// A serializable, Inspector-authorable hit-stop profile referenced by index from an
/// <see cref="ImpactEvent"/> (R7.2, R7.4, R7.5). It is <em>authored data only</em>: the pure
/// decision of whether a hit-stop applies and how simultaneous impacts combine stays in
/// <see cref="HitStop"/> / <see cref="HitStopGrouping"/>, which this profile feeds.
///
/// <para>
/// The authored <see cref="Duration"/> is kept as-is (never silently mutated); callers clamp it to
/// the <c>[0, 1]</c> second range through <see cref="ClampedDuration"/> (which delegates to
/// <see cref="HitStop.ClampDuration"/>) when they need a runtime-safe value. A duration of <c>0</c>
/// disables the hit-stop without touching the game's time scale (R7.2).
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 12.1. Requirements: R7.2, R7.4, R7.5.</remarks>
[Serializable]
public struct HitStopProfile
{
    [Tooltip("Hit-stop duration in seconds. Authored range is [0, 1]; 0 disables the hit-stop without changing time scale (R7.2).")]
    [SerializeField]
    private float duration;

    [Tooltip("Cosmetic classification of this impact's hit-stop (Intermediate, Finisher, Secondary) (R7.4, R7.5).")]
    [SerializeField]
    private HitStopClass hitStopClass;

    /// <summary>The authored hit-stop duration, in seconds, exactly as serialized (not clamped).</summary>
    public float Duration => duration;

    /// <summary>The cosmetic classification of this impact's hit-stop (R7.4, R7.5).</summary>
    public HitStopClass HitStopClass => hitStopClass;

    /// <summary>
    /// The authored <see cref="Duration"/> constrained to the runtime-safe <c>[0, 1]</c> second
    /// range via <see cref="HitStop.ClampDuration"/> (R7.2). This does not mutate the authored
    /// field; it returns a safe copy for the combat loop.
    /// </summary>
    public float ClampedDuration => HitStop.ClampDuration(duration);
}

/// <summary>
/// A serializable, Inspector-authorable description of a single discrete impact (a punch, a shot,
/// an explosion, a pulse) or a continuous ImpactWindow (a sweep) of an Ação_Ofensiva (R2.5). It
/// <em>references</em> the data it needs — most importantly the authored damage/stance/hitbox in
/// the ability's existing <see cref="AreaHitStep"/> list — <strong>by index</strong>, so no
/// damage, stance, or hitbox fields are duplicated here (R2.5).
///
/// <para>
/// This is the <em>authoring</em> twin of the pure runtime <c>ImpactEvent</c> described in the
/// design: it uses plain serialized fields with public getters (Unity cannot serialize a
/// <c>readonly struct</c> with get-only auto-properties from the Inspector), while the runtime
/// consumer builds its own representation and deduplicates each emission by
/// <c>(ExecutionId, Index)</c> through <see cref="ExecutionImpactLedger"/>.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 12.1. Requirements: R2.5, R7.2.</remarks>
[Serializable]
public struct ImpactEvent
{
    [Tooltip("Ordinal of this impact within the execution; combined with the ExecutionId it dedupes a single emission (R8.9).")]
    [SerializeField]
    private int index;

    [Tooltip("Configured instant (fraction of the phase/timeline) at which this impact is emitted or its window opens (R2.5).")]
    [SerializeField]
    private float at;

    [Tooltip("True opens a continuous ImpactWindow (sweep) until WindowEnd; false emits a single discrete impact at At (R2.5).")]
    [SerializeField]
    private bool opensWindow;

    [Tooltip("Fraction at which an opened ImpactWindow closes (within Active). Ignored when OpensWindow is false (R2.5).")]
    [SerializeField]
    private float windowEnd;

    [Tooltip("Index into the profile's hitStopProfiles array selecting this impact's hit-stop profile; negative means none.")]
    [SerializeField]
    private int hitStopProfileIndex;

    [Tooltip("Handle (index) into the ability's authored HitSteps / AreaHitStep list for damage/stance/hitbox. Never duplicated here (R2.5).")]
    [SerializeField]
    private int areaHitStepIndex;

    /// <summary>Ordinal of this impact within the execution, used with the ExecutionId for dedup (R8.9).</summary>
    public int Index => index;

    /// <summary>Configured instant (fraction of the phase/timeline) at which this impact fires (R2.5).</summary>
    public float At => at;

    /// <summary>True when this entry opens a continuous ImpactWindow (sweep); false for a discrete impact.</summary>
    public bool OpensWindow => opensWindow;

    /// <summary>Fraction at which an opened ImpactWindow closes (within Active). Meaningful only when <see cref="OpensWindow"/>.</summary>
    public float WindowEnd => windowEnd;

    /// <summary>
    /// Index into the owning profile's <c>hitStopProfiles</c> selecting this impact's
    /// <see cref="HitStopProfile"/>. A negative value means this impact has no hit-stop profile.
    /// </summary>
    public int HitStopProfileIndex => hitStopProfileIndex;

    /// <summary>
    /// Handle into the ability's authored <see cref="AreaHitStep"/> list (its HitSteps) providing
    /// this impact's damage, stance, and hitbox data. The data lives there and is never duplicated
    /// into this profile (R2.5).
    /// </summary>
    public int AreaHitStepIndex => areaHitStepIndex;
}

/// <summary>
/// A serializable, Inspector-authorable cancel rule twin of the pure runtime <see cref="CancelRule"/>
/// (R4.1, R4.2). Unity cannot serialize a <c>readonly struct</c> with get-only auto-properties from
/// the Inspector, so authored cancel windows are expressed with plain serialized fields here and
/// converted to the pure <see cref="CancelRule"/> via <see cref="ToRule"/> when the combat loop
/// needs the runtime representation. The pure <see cref="CancelRule"/>/<c>CancelResolver</c> are
/// left unchanged.
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 12.1. Requirements: R4.1, R4.2.</remarks>
[Serializable]
public struct CancelRuleData
{
    [Tooltip("Destination this rule authorizes a cancel into (Dash, Basic, Skill) (R4.1).")]
    [SerializeField]
    private CancelTarget target;

    [Tooltip("Start of the open cancel window, as a fraction in [0, 1] (R4.1).")]
    [SerializeField]
    private float start;

    [Tooltip("End of the open cancel window, as a fraction in [0, 1]; must be >= Start (R4.1).")]
    [SerializeField]
    private float end;

    /// <summary>The destination this authored rule targets (R4.1).</summary>
    public CancelTarget Target => target;

    /// <summary>The authored start of the open window, exactly as serialized (not clamped).</summary>
    public float Start => start;

    /// <summary>The authored end of the open window, exactly as serialized (not clamped).</summary>
    public float End => end;

    /// <summary>
    /// Builds the pure runtime <see cref="CancelRule"/> from the authored fields. The runtime
    /// constructor clamps the bounds to <c>0 &lt;= Start &lt;= End &lt;= 1</c> as a defense (R4.8);
    /// the authored fields here are never mutated.
    /// </summary>
    /// <returns>The runtime <see cref="CancelRule"/> for this authored window.</returns>
    public CancelRule ToRule()
    {
        return new CancelRule(target, start, end);
    }
}

/// <summary>
/// The serializable, Inspector-authorable data block describing an Ação_Ofensiva's combat profile
/// (R2.5, R3.1, R4.1, R4.2, R7.2). It is <strong>not</strong> a ScriptableObject or MonoBehaviour
/// and defines no new asset with its own GUID — it is meant to be embedded as a
/// <c>[SerializeField]</c> member in existing assets (<c>Ability</c>, <c>WeaponScript</c>), wired
/// by a later task so no GUIDs/<c>.meta</c> files change here.
///
/// <para>
/// It holds the authored phase boundaries, commitment profile, per-phase movement fractions, and
/// the authored arrays of cancel rules, impact events, and hit-stop profiles. It owns no combat
/// logic: the pure Núcleo_Compartilhado types (<see cref="ActionTimeline"/>,
/// <see cref="CancelRuleSet"/>, <see cref="CancelRule"/>, <see cref="CommitmentRules"/>,
/// <see cref="HitStop"/>) stay the single source of truth, and the <c>Build…</c> helpers below
/// convert this authored data into those runtime types without duplicating any authored
/// damage/stance/hitbox data (that lives in the ability's <see cref="AreaHitStep"/> list and is
/// referenced by index from <see cref="ImpactEvent"/>) (R2.5).
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 12.1. Requirements: R2.5, R3.1, R4.1, R4.2, R7.2, R7.4, R7.5.</remarks>
[Serializable]
public class CombatActionProfile
{
    [Header("Phase boundaries (normalized [0, 1])")]
    [Tooltip("End of Startup / start of Active, as a fraction in [0, 1] (R2.2).")]
    [SerializeField]
    private float startupEnd = 0.3f;

    [Tooltip("End of Active / start of Recovery, as a fraction in [0, 1]; must be >= startupEnd (R2.2).")]
    [SerializeField]
    private float activeEnd = 0.6f;

    [Header("Commitment")]
    [Tooltip("Declared Categoria_de_Compromisso (profile of defaults). Absence resolves to Committed + warning (R3.1, R3.10).")]
    [SerializeField]
    private CommitmentCategory commitment = CommitmentCategory.Committed;

    [Tooltip("How a Channel action ends its Active phase (Hold, Timed, Condition). Only meaningful for Channel (R3.5).")]
    [SerializeField]
    private ChannelTerminationMode channelTermination = ChannelTerminationMode.Hold;

    [Header("Per-phase movement fractions (normalized [0, 1])")]
    [Tooltip("Movement speed fraction allowed during Startup, in [0, 1] (R3.2).")]
    [SerializeField]
    private float startupMoveFraction;

    [Tooltip("Movement speed fraction allowed during Active, in [0, 1] (R3.2).")]
    [SerializeField]
    private float activeMoveFraction;

    [Tooltip("Movement speed fraction allowed during Recovery, in [0, 1] (R3.2).")]
    [SerializeField]
    private float recoveryMoveFraction = 1f;

    [Header("Authored rules and events")]
    [Tooltip("Per-destination cancel windows. Absence of a rule for a destination forbids cancel into it (R4.1, R4.2).")]
    [SerializeField]
    private CancelRuleData[] cancelRules = Array.Empty<CancelRuleData>();

    [Tooltip("Discrete impacts / continuous windows emitted during Active, referencing AreaHitStep data by index (R2.5).")]
    [SerializeField]
    private ImpactEvent[] impactEvents = Array.Empty<ImpactEvent>();

    [Tooltip("Hit-stop profiles referenced by index from impactEvents (R7.2, R7.4, R7.5).")]
    [SerializeField]
    private HitStopProfile[] hitStopProfiles = Array.Empty<HitStopProfile>();

    /// <summary>Authored end of Startup / start of Active, as a fraction (not clamped) (R2.2).</summary>
    public float StartupEnd => startupEnd;

    /// <summary>Authored end of Active / start of Recovery, as a fraction (not clamped) (R2.2).</summary>
    public float ActiveEnd => activeEnd;

    /// <summary>The declared commitment category (profile of defaults) (R3.1).</summary>
    public CommitmentCategory Commitment => commitment;

    /// <summary>How a Channel action ends its Active phase (R3.5).</summary>
    public ChannelTerminationMode ChannelTermination => channelTermination;

    /// <summary>Authored movement fraction during Startup (not clamped) (R3.2).</summary>
    public float StartupMoveFraction => startupMoveFraction;

    /// <summary>Authored movement fraction during Active (not clamped) (R3.2).</summary>
    public float ActiveMoveFraction => activeMoveFraction;

    /// <summary>Authored movement fraction during Recovery (not clamped) (R3.2).</summary>
    public float RecoveryMoveFraction => recoveryMoveFraction;

    /// <summary>The authored cancel-rule data entries (R4.1, R4.2).</summary>
    public IReadOnlyList<CancelRuleData> CancelRules => cancelRules;

    /// <summary>The authored impact events / windows for the Active phase (R2.5).</summary>
    public IReadOnlyList<ImpactEvent> ImpactEvents => impactEvents;

    /// <summary>The authored hit-stop profiles referenced by index from <see cref="ImpactEvents"/> (R7.2).</summary>
    public IReadOnlyList<HitStopProfile> HitStopProfiles => hitStopProfiles;

    /// <summary>
    /// Builds the pure runtime <see cref="ActionTimeline"/> from the authored
    /// <see cref="StartupEnd"/>/<see cref="ActiveEnd"/> boundaries. The timeline constructor clamps
    /// the boundaries to <c>0 &lt;= startupEnd &lt;= activeEnd &lt;= 1</c> as a safe fallback (R2.3);
    /// the authored fields are never mutated here.
    /// </summary>
    /// <returns>The runtime <see cref="ActionTimeline"/> for this profile.</returns>
    public ActionTimeline BuildTimeline()
    {
        return new ActionTimeline(startupEnd, activeEnd);
    }

    /// <summary>
    /// Builds the pure runtime <see cref="CancelRuleSet"/> from the authored
    /// <see cref="CancelRules"/>, converting each <see cref="CancelRuleData"/> via
    /// <see cref="CancelRuleData.ToRule"/>. When multiple entries target the same destination the
    /// last one wins (per <see cref="CancelRuleSet"/>); a <c>null</c> array yields an empty set,
    /// meaning no destination is cancelable (R4.1, R4.2).
    /// </summary>
    /// <returns>The runtime <see cref="CancelRuleSet"/> built from the authored rules.</returns>
    public CancelRuleSet BuildCancelRuleSet()
    {
        var set = new CancelRuleSet();
        if (cancelRules == null)
        {
            return set;
        }

        for (int i = 0; i < cancelRules.Length; i++)
        {
            set.Add(cancelRules[i].ToRule());
        }

        return set;
    }

    /// <summary>
    /// Resolves the effective <see cref="CommitmentCategory"/> for this profile through
    /// <see cref="CommitmentRules.Resolve"/>. Since <see cref="commitment"/> always serializes to a
    /// concrete value, this echoes it and reports <paramref name="emitWarning"/> as <c>false</c>;
    /// the method exists so callers share the single resolution path and so a future nullable
    /// authoring form keeps the <c>Committed</c> + warning safeguard (R3.1, R3.10).
    /// </summary>
    /// <param name="emitWarning">Receives whether the default was applied for an absent category.</param>
    /// <returns>The resolved commitment category.</returns>
    public CommitmentCategory ResolveCommitment(out bool emitWarning)
    {
        return CommitmentRules.Resolve(commitment, out emitWarning);
    }
}
