using System;
using System.Collections.Generic;
using UnityEngine;

public enum GauntletActionSlot { Basic = -1, Q = 0, W = 1, E = 2, R = 3 }
public enum GauntletMutation { DemolishingAdvance = 0, PressureField = 1, Epicenter = 2, CadencedAsura = 3 }
public enum GauntletContentType { Technique = 0, Mutation = 1 }

/// <summary>Raw Inspector data. Validation precedes the immutable runtime copy.</summary>
[Serializable]
public sealed class GauntletEnergyAuthoring
{
    [SerializeField] private int _capacity = 100;
    [SerializeField] private int _basic = 3;
    [SerializeField] private int _skill = 10;
    [SerializeField] private int _alternation = 15;
    [SerializeField] private int _stanceBreak = 20;
    [SerializeField] private int _breaksPerRoot = 2;
    [SerializeField] private int _momentumPerRank = 10;
    [SerializeField] private int _asuraFistPerRank = 2;
    [SerializeField] private int _reservePerRank = 20;

    public bool TrySnapshot(out GauntletEnergyRates rates, out string error)
    {
        rates = default;
        error = null;
        if (_capacity != AsuraMomentum.Maximum) error = "Capacity must preserve the 100-point contract.";
        else if (!Rate(_basic) || !Rate(_skill) || !Rate(_alternation) || !Rate(_stanceBreak)
            || !Rate(_momentumPerRank) || !Rate(_asuraFistPerRank)) error = "Energy rates must be in [0, 100].";
        else if (_breaksPerRoot < 0 || _breaksPerRoot > 8) error = "Break limit must be in [0, 8].";
        else if (_reservePerRank < 0 || _reservePerRank > 20) error = "Reserve cannot exceed 60 at rank 3.";
        if (error != null) return false;
        rates = new GauntletEnergyRates(_basic, _skill, _alternation, _stanceBreak, _breaksPerRoot,
            _momentumPerRank, _asuraFistPerRank, _reservePerRank);
        return true;
    }

    private static bool Rate(int value) => value >= 0 && value <= AsuraMomentum.Maximum;
}

public readonly struct GauntletEnergyRates
{
    public int Basic { get; }
    public int Skill { get; }
    public int Alternation { get; }
    public int StanceBreak { get; }
    public int BreaksPerRoot { get; }
    public int MomentumPerRank { get; }
    public int AsuraFistPerRank { get; }
    public int ReservePerRank { get; }
    internal GauntletEnergyRates(int basic, int skill, int alternation, int stanceBreak,
        int breaksPerRoot, int momentum, int fist, int reserve)
    {
        Basic = basic; Skill = skill; Alternation = alternation; StanceBreak = stanceBreak;
        BreaksPerRoot = breaksPerRoot; MomentumPerRank = momentum;
        AsuraFistPerRank = fist; ReservePerRank = reserve;
    }
}

/// <summary>Presence flags distinguish a missing override from intentional zero.</summary>
[Serializable]
public sealed class GauntletContentAuthoring
{
    [SerializeField] private GauntletContentType _type;
    [SerializeField] private WeaponBoon _technique;
    [SerializeField] private GauntletMutation _mutation;
    [SerializeField] private GauntletActionSlot _slot;
    [SerializeField] private int _maxRank = 3;
    [SerializeField] private bool _overrideDuration;
    [SerializeField] private float _duration;
    [SerializeField] private bool _overrideInterval;
    [SerializeField] private float _interval;
    [SerializeField] private bool _overrideProfile;
    [SerializeField] private Ability _profileSource;

    public GauntletContentType Type => _type;
    public WeaponBoon Technique => _technique;
    public GauntletMutation Mutation => _mutation;
    public GauntletActionSlot Slot => _slot;
    public int MaxRank => _maxRank;
    public bool HasDuration => _overrideDuration;
    public float Duration => _duration;
    public bool HasInterval => _overrideInterval;
    public float Interval => _interval;
    public Ability ProfileSource => _overrideProfile ? _profileSource : null;
    public CombatActionProfile Profile => ProfileSource ? ProfileSource.CombatProfile : null;
    public string Id => _type == GauntletContentType.Technique
        ? WeaponRunModifiers.CatalogId(_technique) : "gauntlet_mutation_" + _mutation;

    public static GauntletContentAuthoring ForTechnique(WeaponBoon kind) =>
        new GauntletContentAuthoring { _type = GauntletContentType.Technique, _technique = kind,
            _slot = GauntletActionSlot.Basic, _maxRank = 3 };

    public static GauntletContentAuthoring ForMutation(GauntletMutation kind) =>
        new GauntletContentAuthoring { _type = GauntletContentType.Mutation, _mutation = kind,
            _slot = (GauntletActionSlot)(int)kind, _maxRank = 1 };

    public static bool TryMutation(string id, out GauntletMutation mutation)
    {
        foreach (GauntletMutation value in Enum.GetValues(typeof(GauntletMutation)))
            if (ForMutation(value).Id == id) { mutation = value; return true; }
        mutation = default;
        return false;
    }

    public static string MutationTitle(GauntletMutation mutation)
    {
        switch (mutation)
        {
            case GauntletMutation.DemolishingAdvance: return "Avanço Demolidor";
            case GauntletMutation.PressureField: return "Campo de Pressão";
            case GauntletMutation.Epicenter: return "Epicentro";
            case GauntletMutation.CadencedAsura: return "Asura Cadenciada";
            default: throw new ArgumentOutOfRangeException(nameof(mutation));
        }
    }

    public static string MutationDescription(GauntletMutation mutation)
    {
        switch (mutation)
        {
            case GauntletMutation.DemolishingAdvance:
                return "Q: avanço com golpes → travessia de corpos e uppercut final com o dano total. Launch somente na quebra. Mantém hurtbox, mana e cooldown; sólidos bloqueiam. Combina com Propulsor de combate; Punho sísmico não substitui Launch.";
            case GauntletMutation.PressureField:
                return "W: sequência de golpes → sequência e zona no último impacto, com 4 pulsos em 2s a 25% do HP/postura final. Mantém mana e cooldown. Mil punhos não replica a zona; pulsos não geram Asura.";
            case GauntletMutation.Epicenter:
                return "E: impactos frontais → impactos radiais centrados no jogador, com +20% de raio. Mantém HP/postura por alvo, mana e cooldown. ShockRing é o mesmo conteúdo; até 2 quebras geram Asura por execução.";
            case GauntletMutation.CadencedAsura:
                return "R: sequência automática → 3 pressionamentos, golpes com 25%/25%/50% do dano total. Até 0,8s para continuar. Custo único; espera permite movimento sem proteção. Asura reverberante replica somente o terceiro golpe; segurar R não continua.";
            default: throw new ArgumentOutOfRangeException(nameof(mutation));
        }
    }

    public bool Validate(out string error)
    {
        error = null;
        if (!Enum.IsDefined(typeof(GauntletContentType), _type)
            || !Enum.IsDefined(typeof(GauntletActionSlot), _slot)) error = "Invalid content type or slot.";
        else if (_type == GauntletContentType.Technique && !IsSelectedTechnique(_technique)) error = "Technique is not in the Phase 3 catalog.";
        else if (_type == GauntletContentType.Mutation && (!Enum.IsDefined(typeof(GauntletMutation), _mutation)
            || (int)_slot != (int)_mutation)) error = "Mutation must match its authored slot.";
        else if (_maxRank != (_type == GauntletContentType.Technique ? 3 : 1)) error = "Invalid content rank limit.";
        else if (!FiniteNonNegative(_duration) || !FiniteNonNegative(_interval)) error = "Timing must be finite and non-negative.";
        else if (_overrideProfile && (!_profileSource || _profileSource.CombatProfile == null)) error = "An enabled profile override requires an ability with a profile.";
        else if (_overrideProfile && !ValidateProfile(_profileSource.CombatProfile)) error = "The profile override contains invalid phase, movement or impact data.";
        else if (_overrideProfile && _type != GauntletContentType.Mutation) error = "Profile overrides belong to Mutations, not Techniques.";
        else if (_overrideDuration && !SupportsDuration) error = "Duration override has no defined effect for " + Id + ".";
        else if (_overrideInterval && !SupportsInterval) error = "Interval override has no defined effect for " + Id + ".";
        else if (_type == GauntletContentType.Mutation && _mutation == GauntletMutation.PressureField
            && _overrideDuration && _overrideInterval && !Mathf.Approximately(_duration, 4f * _interval))
            error = "Pressure Field duration must equal four pulse intervals.";
        return error == null;
    }

    private bool SupportsDuration => _type == GauntletContentType.Mutation
        ? _mutation == GauntletMutation.PressureField || _mutation == GauntletMutation.CadencedAsura
        : _technique == WeaponBoon.HungryCombo || _technique == WeaponBoon.FlurryEcho || _technique == WeaponBoon.AsuraEcho;
    private bool SupportsInterval => _type == GauntletContentType.Mutation
        ? _mutation == GauntletMutation.PressureField || _mutation == GauntletMutation.CadencedAsura
        : _technique == WeaponBoon.FlurryEcho || _technique == WeaponBoon.AsuraEcho;

    public static bool IsSelectedTechnique(WeaponBoon kind)
    {
        switch (kind)
        {
            case WeaponBoon.Momentum: case WeaponBoon.AsuraReserve: case WeaponBoon.AsuraFist:
            case WeaponBoon.GuardBreaker: case WeaponBoon.HungryCombo: case WeaponBoon.MomentumStrike:
            case WeaponBoon.FlurryEcho: case WeaponBoon.AsuraEcho: case WeaponBoon.RocketAdvance:
            case WeaponBoon.SeismicFist: return true;
            default: return false;
        }
    }

    private static bool FiniteNonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;

    private static bool Unit(float value) => FiniteNonNegative(value) && value <= 1f;
    internal static bool ValidateProfile(CombatActionProfile profile)
    {
        if (!Unit(profile.StartupEnd) || !Unit(profile.ActiveEnd)
            || !ActionTimeline.Validate(profile.StartupEnd, profile.ActiveEnd, out _)
            || !Enum.IsDefined(typeof(CommitmentCategory), profile.Commitment)
            || !Enum.IsDefined(typeof(ChannelTerminationMode), profile.ChannelTermination)
            || !Unit(profile.StartupMoveFraction) || !Unit(profile.ActiveMoveFraction) || !Unit(profile.RecoveryMoveFraction)) return false;
        if (profile.CancelRules != null)
            foreach (var rule in profile.CancelRules)
                if (!Unit(rule.Start) || !Unit(rule.End) || !CancelRule.Validate(rule.Start, rule.End, out _)) return false;
        if (profile.ImpactEvents != null)
            foreach (var impact in profile.ImpactEvents)
                if (impact.Index < 0 || impact.AreaHitStepIndex < 0 || !Unit(impact.At)
                    || (impact.OpensWindow && (!Unit(impact.WindowEnd) || impact.WindowEnd < impact.At))) return false;
        if (profile.HitStopProfiles != null)
            foreach (var hitStop in profile.HitStopProfiles) if (!Unit(hitStop.Duration)) return false;
        return true;
    }
}

[CreateAssetMenu(menuName = "Tech-Guy/Weapons/Gauntlet Phase 3 Configuration")]
public sealed class GauntletAuthoring : ScriptableObject
{
    [SerializeField] private GauntletEnergyAuthoring _energy = new GauntletEnergyAuthoring();
    [SerializeField] private List<GauntletContentAuthoring> _content = DefaultContent();
    public GauntletEnergyAuthoring Energy => _energy;
    public IReadOnlyList<GauntletContentAuthoring> Content => _content.AsReadOnly();
    internal GauntletContentAuthoring FindContent(string id)
    {
        foreach (var entry in _content) if (entry != null && entry.Id == id) return entry;
        return null;
    }

    public bool Validate(out string error)
    {
        error = null;
        if (_energy == null || _content == null) { error = "Energy and content data are required."; return false; }
        if (!_energy.TrySnapshot(out _, out error)) return false;
        var ids = new HashSet<string>();
        int techniques = 0, mutations = 0;
        foreach (var entry in _content)
        {
            if (entry == null) { error = "Null catalog entry."; return false; }
            if (!entry.Validate(out error)) return false;
            if (!ids.Add(entry.Id)) { error = "Duplicate catalog identity: " + entry.Id; return false; }
            if (entry.Type == GauntletContentType.Technique) techniques++; else mutations++;
        }
        if (techniques != 10 || mutations != 4) error = "Phase 3 requires the selected 10 Techniques and 4 Mutations.";
        return error == null;
    }

    private void OnValidate()
    {
        if (!Validate(out var error)) Debug.LogWarning("Gauntlet authoring: " + error, this);
    }

    private static List<GauntletContentAuthoring> DefaultContent()
    {
        var result = new List<GauntletContentAuthoring>();
        foreach (var kind in new[] { WeaponBoon.Momentum, WeaponBoon.AsuraReserve, WeaponBoon.AsuraFist,
            WeaponBoon.GuardBreaker, WeaponBoon.HungryCombo, WeaponBoon.MomentumStrike,
            WeaponBoon.FlurryEcho, WeaponBoon.AsuraEcho, WeaponBoon.RocketAdvance, WeaponBoon.SeismicFist })
            result.Add(GauntletContentAuthoring.ForTechnique(kind));
        foreach (GauntletMutation kind in Enum.GetValues(typeof(GauntletMutation)))
            result.Add(GauntletContentAuthoring.ForMutation(kind));
        return result;
    }
}
