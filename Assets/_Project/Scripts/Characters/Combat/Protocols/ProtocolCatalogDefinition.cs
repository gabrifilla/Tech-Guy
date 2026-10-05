using System;
using System.Collections.Generic;
using UnityEngine;

public enum ProtocolKind { Ignite, Frost, Conductor, Detonation, Reactor, Resonance, Overflow }
public enum ProtocolClock { ScaledGameplay }

/// <summary>Inspector data. Runtime consumers receive deep-copied, validated snapshots.</summary>
[Serializable]
public sealed class ProtocolDefinition
{
    [SerializeField] private ProtocolKind _kind;
    [SerializeField] private string _id;
    [SerializeField] private int _maxRank = 3;
    [SerializeField] private float _damagePerRank;
    [SerializeField] private float _damageFactorBase;
    [SerializeField] private float _damageFactorPerAdditionalRank;
    [SerializeField] private float _radiusBase;
    [SerializeField] private float _radiusPerRank;
    [SerializeField] private float _duration;
    [SerializeField] private float _tickInterval;
    [SerializeField] private float _chancePerRank;
    [SerializeField] private float _slowFraction;
    [SerializeField] private int _maxVictims;
    [SerializeField] private bool _requiresIgnite;
    [SerializeField] private bool _requiresElement;
    [SerializeField] private bool _requiresPropagation;
    public ProtocolKind Kind => _kind;
    public string Id => _id;
    public int MaxRank => _maxRank;
    public float DamagePerRank => _damagePerRank;
    public float DamageFactorBase => _damageFactorBase;
    public float DamageFactorPerAdditionalRank => _damageFactorPerAdditionalRank;
    public float RadiusBase => _radiusBase;
    public float RadiusPerRank => _radiusPerRank;
    public float Duration => _duration;
    public float TickInterval => _tickInterval;
    public float ChancePerRank => _chancePerRank;
    public float SlowFraction => _slowFraction;
    public int MaxVictims => _maxVictims;
    public bool RequiresIgnite => _requiresIgnite;
    public bool RequiresElement => _requiresElement;
    public bool RequiresPropagation => _requiresPropagation;
    internal ProtocolDefinition Copy() => (ProtocolDefinition)MemberwiseClone();

    internal static ProtocolDefinition Default(ProtocolKind kind)
    {
        var value = new ProtocolDefinition { _kind = kind, _id = ProtocolCatalogValidator.IdFor(kind) };
        switch (kind)
        {
            case ProtocolKind.Ignite: value._damagePerRank = 6f; value._duration = 4f; value._tickInterval = .5f; break;
            case ProtocolKind.Frost: value._chancePerRank = .35f; value._slowFraction = .9f; value._duration = 2.5f; break;
            case ProtocolKind.Conductor: value._damageFactorBase = .45f; value._radiusBase = 3f; value._radiusPerRank = .5f; break;
            case ProtocolKind.Detonation: value._damageFactorBase = .75f; value._damageFactorPerAdditionalRank = .25f;
                value._radiusBase = 3f; value._radiusPerRank = .5f; value._maxVictims = 6; break;
            case ProtocolKind.Reactor: value._damagePerRank = .12f; value._requiresIgnite = true; break;
            case ProtocolKind.Resonance: value._damagePerRank = .4f; value._requiresElement = true; value._requiresPropagation = true; break;
            case ProtocolKind.Overflow: value._damagePerRank = .5f; value._requiresElement = true; break;
        }
        return value;
    }
}

[Serializable]
public sealed class ProtocolLimits
{
    [SerializeField] private ProtocolClock _clock = ProtocolClock.ScaledGameplay;
    [SerializeField] private int _impactsPerRoot = 32;
    [SerializeField] private int _maxDepth = 4;
    [SerializeField] private int _queuePerRoot = 64;
    [SerializeField] private int _reservationsPerRoot = 32;
    [SerializeField] private int _liveRoots = 64;
    [SerializeField] private int _impactsPerFrame = 256;
    [SerializeField] private int _queriesPerFrame = 64;
    [SerializeField] private int _collidersPerQuery = 128;
    [SerializeField] private int _sightTestsPerQuery = 128;
    [SerializeField] private int _applicationsPerType = 256;
    [SerializeField] private int _burnTicksPerFrame = 64;
    [SerializeField] private int _activeVfx = 24;
    [SerializeField] private int _newVfxPerFrame = 16;
    [SerializeField] private float _vfxLifetime = .6f;
    [SerializeField] private float _rootLifetime = 10f;
    [SerializeField] private float _thermalShockMultiplier = 1.5f;
    [SerializeField] private float _combustionRadiusMultiplier = 1.3f;
    [SerializeField] private int _shatterMinTrails = 3;
    [SerializeField] private int _shatterMaxTrails = 6;
    [SerializeField] private float _heavyMaxSlow = .5f;
    [SerializeField] private float _eliteMaxSlow = .35f;
    [SerializeField] private float _bossMaxSlow = .15f;
    public ProtocolClock Clock => _clock;
    public int ImpactsPerRoot => _impactsPerRoot;
    public int MaxDepth => _maxDepth;
    public int QueuePerRoot => _queuePerRoot;
    public int ReservationsPerRoot => _reservationsPerRoot;
    public int LiveRoots => _liveRoots;
    public int ImpactsPerFrame => _impactsPerFrame;
    public int QueriesPerFrame => _queriesPerFrame;
    public int CollidersPerQuery => _collidersPerQuery;
    public int SightTestsPerQuery => _sightTestsPerQuery;
    public int ApplicationsPerType => _applicationsPerType;
    public int BurnTicksPerFrame => _burnTicksPerFrame;
    public int ActiveVfx => _activeVfx;
    public int NewVfxPerFrame => _newVfxPerFrame;
    public float VfxLifetime => _vfxLifetime;
    public float RootLifetime => _rootLifetime;
    public float ThermalShockMultiplier => _thermalShockMultiplier;
    public float CombustionRadiusMultiplier => _combustionRadiusMultiplier;
    public int ShatterMinTrails => _shatterMinTrails;
    public int ShatterMaxTrails => _shatterMaxTrails;
    public float HeavyMaxSlow => _heavyMaxSlow;
    public float EliteMaxSlow => _eliteMaxSlow;
    public float BossMaxSlow => _bossMaxSlow;
    internal ProtocolLimits Copy() => (ProtocolLimits)MemberwiseClone();
}

[CreateAssetMenu(menuName = "Tech Guy/Combat/Protocol Catalog")]
public sealed class ProtocolCatalogDefinition : ScriptableObject
{
    [SerializeField] private int _revision = 1;
    [SerializeField] private List<ProtocolDefinition> _entries = CreateDefaults();
    [SerializeField] private ProtocolLimits _limits = new ProtocolLimits();
    public int Revision => _revision;
    public IReadOnlyList<ProtocolDefinition> Entries => _entries?.AsReadOnly();
    public ProtocolLimits Limits => _limits;
    public bool TryCreateSnapshot(out ProtocolCatalogSnapshot snapshot, out string diagnostic)
        => ProtocolCatalogValidator.TryCreate(this, out snapshot, out diagnostic);
    private static List<ProtocolDefinition> CreateDefaults()
    {
        var entries = new List<ProtocolDefinition>();
        foreach (ProtocolKind kind in Enum.GetValues(typeof(ProtocolKind))) entries.Add(ProtocolDefinition.Default(kind));
        return entries;
    }
}

public sealed class ProtocolCatalogSnapshot
{
    private readonly IReadOnlyList<ProtocolDefinition> _entries;
    public int Revision { get; }
    public ProtocolLimits Limits { get; }
    public IReadOnlyList<ProtocolDefinition> Entries => _entries;
    internal ProtocolCatalogSnapshot(ProtocolCatalogDefinition source)
    {
        Revision = source.Revision; Limits = source.Limits.Copy();
        var entries = new List<ProtocolDefinition>();
        foreach (var entry in source.Entries) entries.Add(entry.Copy());
        _entries = entries.AsReadOnly();
    }
    public ProtocolDefinition Get(ProtocolKind kind)
    {
        for (int i = 0; i < _entries.Count; i++) if (_entries[i].Kind == kind) return _entries[i];
        throw new ArgumentOutOfRangeException(nameof(kind));
    }
}
