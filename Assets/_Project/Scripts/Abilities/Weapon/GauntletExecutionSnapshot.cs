using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Accepted action parameters. Mutable runtime counters never live in this object.</summary>
public sealed class GauntletExecutionSnapshot
{
    private readonly Dictionary<WeaponBoon, int> _ranks = new Dictionary<WeaponBoon, int>();
    private readonly List<AreaHitStep> _steps;
    private readonly CombatActionProfile _profile;
    public GauntletActionSlot Slot { get; }
    public int DirectStepCount { get; }
    public int LastDirectProducer => DirectStepCount - 1;
    public float ManaCost { get; }
    public float ActiveTime { get; }
    public float AdvanceDistance { get; }
    public float AdvanceDuration { get; }
    public bool Asura { get; }
    public bool Shock { get; }
    public Color EffectColor { get; }
    public GauntletMutation? Mutation { get; }
    public EclipseCombatSettings Eclipse { get; }
    public bool ViolentFall { get; }
    public bool BasicFinisher { get; }
    public string BasicMutationId { get; }
    public bool EchoingFist { get; }
    public bool TemporalEcho { get; }
    public bool PerfectRhythm { get; }
    public bool TitanFall { get; }
    public float HungrySecondsPerRank { get; } = .3f;
    public float PressurePulseInterval { get; } = .5f;
    public float CadencedWaitDuration { get; } = .8f;
    public float CadencedImpactTime { get; } = .12f;
    public float CadencedImpactFraction { get; } = .3f;
    public float CadencedRecoveryStart { get; } = .22f;

    private GauntletExecutionSnapshot(WeaponRunModifiers modifiers, GauntletActionSlot slot,
        BreakerGauntletAbility ability, GauntletMutationSelection selections = null, GauntletAuthoring authoring = null, bool basicFinisher = false)
    {
        Slot = slot;
        BasicMutationId = slot == GauntletActionSlot.Basic ? modifiers?.Expansion.MutationId(BoonSlot.Basic) : null;
        BasicFinisher = basicFinisher; ViolentFall = modifiers?.HasViolentFall == true;
        EchoingFist = modifiers?.Expansion.Has(GauntletExpansionCatalog.EchoingFistId, slot) == true;
        TemporalEcho = modifiers?.Expansion.Has(GauntletExpansionCatalog.TemporalEchoId, slot) == true;
        PerfectRhythm = modifiers?.Expansion.Has(GauntletExpansionCatalog.PerfectRhythmId, slot) == true;
        TitanFall = modifiers?.Expansion.Has(GauntletExpansionCatalog.TitanFallId) == true;
        var eclipseTuning = authoring ? authoring.Eclipse : new EclipseCombatTuning();
        if (!eclipseTuning.TrySnapshot(out var eclipse, out var diagnostic)) throw new ArgumentException(diagnostic);
        Eclipse = eclipse;
        // Catalog order is stable; no acquisition dictionary is retained or exposed.
        var frozen = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
        foreach (var definition in WeaponRunModifiers.Catalog)
        {
            if (definition.Family != RunWeaponFamily.Gauntlet) continue;
            int rank = modifiers?.Rank(definition.Kind) ?? 0;
            _ranks.Add(definition.Kind, rank);
            for (int i = 0; i < rank; i++) frozen.Add(definition);
        }
        _steps = new List<AreaHitStep>();
        var hungry = authoring ? authoring.FindContent(WeaponRunModifiers.CatalogId(WeaponBoon.HungryCombo)) : null;
        if (hungry != null && hungry.HasDuration) HungrySecondsPerRank = hungry.Duration;
        if (!ability) return;
        ManaCost = ability.ManaCost;
        ActiveTime = ability.activeTime;
        AdvanceDuration = ability.AdvanceDuration;
        AdvanceDistance = slot == GauntletActionSlot.Q
            ? Mathf.Min(8f, ability.AdvanceDistance * (1f + .7f * Rank(WeaponBoon.RocketAdvance)))
            : ability.AdvanceDistance;
        Asura = ability.AsuraBurst; Shock = ability.ShockSkill; EffectColor = ability.EffectColor;
        _profile = Clone(ability.CombatProfile);
        var direct = new List<AreaHitStep>();
        foreach (var step in ability.HitSteps) direct.Add(Clone(step));
        if (selections != null && selections.TryGet(slot, out var selected)) Mutation = selected;
        // ShockRing is a legacy selection of Epicenter, with its existing rank radius preserved.
        if (slot == GauntletActionSlot.E && Rank(WeaponBoon.ShockRing) > 0) Mutation = GauntletMutation.Epicenter;
        ApplyMutation(direct);
        if (Mutation == GauntletMutation.EclipseUppercut)
        {
            ActiveTime = Eclipse.Duration; AdvanceDistance = 0f; AdvanceDuration = 0f;
            _profile = CombatActionProfile.EclipseUppercut();
        }
        if (Mutation == GauntletMutation.CadencedAsura)
        {
            ActiveTime = .4f; AdvanceDistance = 0f; AdvanceDuration = 0f;
            _profile = CombatActionProfile.CadencedStrike(0);
        }
        if (Mutation == GauntletMutation.DemolishingAdvance && _profile != null)
            _profile = _profile.WithOnlyFinalImpact(ability.HitSteps.Count - 1);
        var mutationData = authoring && Mutation.HasValue ? authoring.FindContent(GauntletContentAuthoring.ForMutation(Mutation.Value).Id) : null;
        if (mutationData != null)
        {
            if (mutationData.Profile != null) _profile = Clone(mutationData.Profile);
            if (Mutation == GauntletMutation.PressureField)
                PressurePulseInterval = mutationData.HasInterval ? mutationData.Interval
                    : mutationData.HasDuration ? mutationData.Duration / 4f : .5f;
            if (Mutation == GauntletMutation.CadencedAsura)
            {
                if (mutationData.HasDuration) ActiveTime = mutationData.Duration;
                if (mutationData.HasInterval) CadencedWaitDuration = mutationData.Interval;
                if (mutationData.HasDuration || mutationData.Profile != null)
                {
                    CadencedImpactFraction = _profile.ImpactEvents.Count == 1 ? _profile.ImpactEvents[0].At : .3f;
                    CadencedImpactTime = ActiveTime * CadencedImpactFraction;
                    CadencedRecoveryStart = ActiveTime * _profile.ActiveEnd;
                    foreach (var step in direct) step.delay = CadencedImpactTime;
                }
            }
        }
        DirectStepCount = direct.Count;
        _steps = frozen.ComposeGauntletSteps(direct, (int)slot, applyLegacyGeometry: false);
        if (ViolentFall)
        {
            // Preserve retained echo HP/posture while preventing its break from replacing an armed Launch.
            int echoes = slot == GauntletActionSlot.W ? 2 * Rank(WeaponBoon.FlurryEcho)
                : slot == GauntletActionSlot.R ? Rank(WeaponBoon.AsuraEcho) : 0;
            for (int i = DirectStepCount; i < Math.Min(_steps.Count, DirectStepCount + echoes); i++)
                _steps[i].breakEffect = StanceBreakEffect.Stun;
        }
        var echoKind = slot == GauntletActionSlot.W ? WeaponBoon.FlurryEcho : WeaponBoon.AsuraEcho;
        var echoData = authoring && (slot == GauntletActionSlot.W || slot == GauntletActionSlot.R)
            ? authoring.FindContent(WeaponRunModifiers.CatalogId(echoKind)) : null;
        if (echoData != null && (echoData.HasDuration || echoData.HasInterval))
        {
            float interval = echoData.HasInterval ? echoData.Interval : .18f;
            float firstDelay = echoData.HasDuration ? echoData.Duration : interval;
            int count = (slot == GauntletActionSlot.W ? 2 : 1) * Rank(echoKind);
            for (int i = 0; i < count; i++) _steps[DirectStepCount + i].delay = direct[direct.Count - 1].delay + firstDelay + interval * i;
        }
        GauntletLoopSteps.Configure(_steps, (int)slot);
        foreach (var step in _steps) step.pushDistance = 0f;
    }

    public int Rank(WeaponBoon kind) => _ranks.TryGetValue(kind, out int value) ? value : 0;
    public CombatActionProfile CopyProfile() => Clone(_profile);
    public List<AreaHitStep> CopySteps()
    {
        var result = new List<AreaHitStep>(_steps.Count);
        foreach (var step in _steps) result.Add(Clone(step));
        return result;
    }
    public bool CanProduceLocalChild(GauntletImpactOrigin origin, int stepIndex)
        => origin == GauntletImpactOrigin.Direct && stepIndex == LastDirectProducer && stepIndex >= 0;

    public static GauntletExecutionSnapshot CaptureBasic(WeaponRunModifiers modifiers, GauntletAuthoring authoring = null)
        => CaptureBasic(modifiers, authoring, false);
    internal static GauntletExecutionSnapshot CaptureBasic(WeaponRunModifiers modifiers, GauntletAuthoring authoring, bool finisher)
        => new GauntletExecutionSnapshot(modifiers, GauntletActionSlot.Basic, null, authoring: authoring, basicFinisher: finisher);

    public static bool TryBuild(BreakerGauntletAbility ability, GauntletActionSlot slot,
        WeaponRunModifiers modifiers, out GauntletExecutionSnapshot snapshot, GauntletMutationSelection selections = null, GauntletAuthoring authoring = null)
    {
        snapshot = null;
        if (authoring && !authoring.Validate(out _)) return false;
        if (!ability || slot < GauntletActionSlot.Q || slot > GauntletActionSlot.R ||
            ability.HitSteps == null || ability.HitSteps.Count == 0 ||
            !FiniteNonnegative(ability.ManaCost) || !FiniteNonnegative(ability.activeTime) ||
            !FiniteNonnegative(ability.AdvanceDistance) || !FiniteNonnegative(ability.AdvanceDuration)) return false;
        bool legacyRFinisher = slot == GauntletActionSlot.R && ability.AsuraBurst &&
            ability.HitSteps[ability.HitSteps.Count - 1] != null &&
            (int)ability.HitSteps[ability.HitSteps.Count - 1].reactionType == 3;
        for (int i = 0; i < ability.HitSteps.Count; i++)
            if (!ValidateStep(ability.HitSteps[i], legacyRFinisher && i == ability.HitSteps.Count - 1)) return false;
        if (ability.CombatProfile != null && !GauntletContentAuthoring.ValidateProfile(ability.CombatProfile)) return false;
        var candidate = new GauntletExecutionSnapshot(modifiers, slot, ability, selections, authoring);
        var authoredMutation = authoring && candidate.Mutation.HasValue
            ? authoring.FindContent(GauntletContentAuthoring.ForMutation(candidate.Mutation.Value).Id) : null;
        // Eclipse owns a single fixed contact profile; its timing is authored through EclipseCombatTuning.
        // A legacy profile override could omit that contact and strand its scheduler waiting forever.
        if (candidate.Mutation == GauntletMutation.EclipseUppercut && authoredMutation?.Profile != null) return false;
        if (authoredMutation?.Profile != null)
        {
            if (candidate._profile.ImpactEvents == null || candidate._profile.ImpactEvents.Count == 0) return false;
            var indices = new HashSet<int>();
            foreach (var impact in candidate._profile.ImpactEvents)
                if (!indices.Add(impact.Index) || impact.AreaHitStepIndex >= candidate.DirectStepCount
                    || impact.At < candidate._profile.StartupEnd || impact.At > candidate._profile.ActiveEnd) return false;
        }
        if (!FiniteNonnegative(candidate.PressurePulseInterval) || !FiniteNonnegative(candidate.HungrySecondsPerRank)
            || !FiniteNonnegative(candidate.CadencedWaitDuration)) return false;
        if (candidate.Mutation == GauntletMutation.CadencedAsura &&
            (candidate.ActiveTime <= 0f || candidate._profile.ImpactEvents.Count != 1
             || candidate._profile.ImpactEvents[0].OpensWindow || candidate._profile.ImpactEvents[0].AreaHitStepIndex != 0
             || candidate.CadencedImpactFraction < candidate._profile.StartupEnd || candidate.CadencedImpactFraction > candidate._profile.ActiveEnd
             || candidate._profile.Commitment != CommitmentCategory.Committed
             || candidate._profile.StartupMoveFraction != 0f || candidate._profile.ActiveMoveFraction != 0f || candidate._profile.RecoveryMoveFraction != 0f)) return false;
        for (int i = 0; i < candidate._steps.Count; i++)
            if (!ValidateStep(candidate._steps[i], legacyRFinisher && i >= candidate.LastDirectProducer)) return false;
        snapshot = candidate;
        return true;
    }

    private static bool ValidateStep(AreaHitStep step, bool allowLegacyRFinisher = false)
    {
        return !(step == null || !FiniteNonnegative(step.delay) || !FiniteNonnegative(step.damageMultiplier) ||
                !FiniteNonnegative(step.stanceDamage) || !FiniteNonnegative(step.rangeOverride) ||
                !FiniteNonnegative(step.sphereRadius) || !FiniteNonnegative(step.pushDistance) ||
                !FiniteNonnegative(step.knockbackDistance) || !FiniteNonnegative(step.knockUpHeight) ||
                !FiniteNonnegative(step.stunDuration) || !FiniteNonnegative(step.boxSize.x) ||
                !FiniteNonnegative(step.boxSize.y) || !FiniteNonnegative(step.boxSize.z) ||
                !Finite(step.bonusDamage) || !Finite(step.localOffset.x) || !Finite(step.localOffset.y) ||
                !Finite(step.localOffset.z) || !Enum.IsDefined(typeof(AreaHitShape), step.hitShape) ||
                !Enum.IsDefined(typeof(HitStrength), step.hitStrength) ||
                (!Enum.IsDefined(typeof(HitReactionType), step.reactionType) &&
                    !(allowLegacyRFinisher && (int)step.reactionType == 3)) ||
                !Enum.IsDefined(typeof(StanceBreakEffect), step.breakEffect));
    }

    private void ApplyMutation(List<AreaHitStep> direct)
    {
        if (Mutation == GauntletMutation.EclipseUppercut)
        {
            var step = Clone(direct[direct.Count - 1]);
            MergeTotals(direct, out step.damageMultiplier, out step.bonusDamage, out step.stanceDamage);
            step.delay = Eclipse.Duration * .3f; step.rangeOverride = Eclipse.Reach;
            step.boxSize = new Vector3(Eclipse.Width, 3f, Eclipse.Reach);
            step.hitShape = AreaHitShape.Box; step.localOffset = Vector3.zero;
            step.reactionType = HitReactionType.Stagger; step.hitStrength = HitStrength.Heavy;
            step.breakEffect = StanceBreakEffect.KnockUp; step.knockUpHeight = Eclipse.LaunchHeight;
            step.stunDuration = Eclipse.LaunchSeconds; step.pushDistance = 0f; step.knockbackDistance = 0f;
            direct.Clear(); direct.Add(step);
        }
        else if (Mutation == GauntletMutation.DemolishingAdvance)
        {
            var final = Clone(direct[direct.Count - 1]);
            MergeTotals(direct, out final.damageMultiplier, out final.bonusDamage, out final.stanceDamage);
            final.breakEffect = StanceBreakEffect.KnockUp;
            final.reactionType = HitReactionType.Stagger;
            final.hitStrength = HitStrength.Heavy;
            direct.Clear(); direct.Add(final);
        }
        else if (Mutation == GauntletMutation.CadencedAsura)
        {
            MergeTotals(direct, out float multiplier, out float bonus, out float stance);
            var final = Clone(direct[direct.Count - 1]);
            direct.Clear();
            for (int i = 0; i < 3; i++)
            {
                var step = Clone(final); float fraction = i == 2 ? .5f : .25f;
                step.delay = .12f; step.damageMultiplier = multiplier * fraction;
                if (i < 2) { step.reactionType = HitReactionType.Stagger; step.breakEffect = StanceBreakEffect.None; }
                // addedDamage enters the base BEFORE the skill multiplier in PlayerArpgStats.
                step.bonusDamage = bonus; step.stanceDamage = stance * fraction;
                direct.Add(step);
            }
        }
        else if (Mutation == GauntletMutation.Epicenter)
        {
            int radiusRank = Mathf.Max(1, Rank(WeaponBoon.ShockRing));
            foreach (var step in direct)
            {
                step.hitShape = AreaHitShape.Sphere;
                step.sphereRadius = Mathf.Max(step.sphereRadius, step.boxSize.x * .5f) * (1f + .2f * radiusRank);
                step.rangeOverride = .01f; step.localOffset = new Vector3(0f, 1f - step.sphereRadius, 0f);
            }
        }
        // W's zone and R's manual continuation are execution plans, not extra direct producers.
        // Their schedulers are implemented in the respective Mutation stages.
    }

    private static bool FiniteNonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
    private static void MergeTotals(List<AreaHitStep> steps, out float multiplier, out float bonus, out float stance)
    {
        double total = 0d, weightedBonus = 0d, posture = 0d;
        foreach (var step in steps)
        {
            total += step.damageMultiplier; posture += step.stanceDamage;
            weightedBonus += (double)step.damageMultiplier * Math.Max(0f, step.bonusDamage);
        }
        multiplier = (float)total; stance = (float)posture;
        bonus = total > 0d ? (float)(weightedBonus / total) : 0f;
    }
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static T Clone<T>(T value) where T : class
        => value == null ? null : value is AreaHitStep step ? (T)(object)step.Copy() :
            value is CombatActionProfile profile ? (T)(object)profile.Copy() :
            JsonUtility.FromJson<T>(JsonUtility.ToJson(value));
}
