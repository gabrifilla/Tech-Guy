using System;
using System.Collections.Generic;
using UnityEngine;

public enum BoonFamily { Mutation, Augment, Protocol, Resonance, Mythic }
[Flags] public enum BoonWeaponMask { None = 0, Gauntlet = 1, Bow = 2, Spear = 4, Universal = 7 }
public enum BoonSlot { Q = 0, W = 1, E = 2, R = 3 }
[Flags] public enum BoonSlotMask { None = 0, Q = 1, W = 2, E = 4, R = 8, Any = 15 }

// Keep serialized capability flags within Unity's 32-bit enum representation.
[Flags]
public enum BoonCapability
{
    None = 0,
    MeleeHit = 1 << 0, Projectile = 1 << 1, PiercingLine = 1 << 2, Cone = 1 << 3,
    AoE = 1 << 4, Chain = 1 << 5, Summon = 1 << 6, Channel = 1 << 7,
    MultiPress = 1 << 8, PersistentZone = 1 << 9,
    GapClose = 1 << 10, Dash = 1 << 11, PullEnemy = 1 << 12, SelfReposition = 1 << 13, ReturnPath = 1 << 14,
    ApplyStatus = 1 << 15, ConsumeStatus = 1 << 16, Mark = 1 << 17, DetonateMark = 1 << 18,
    Stagger = 1 << 19, Launch = 1 << 20, AirFollowup = 1 << 21, GuardCounter = 1 << 22,
    BreakPosture = 1 << 23, OnKill = 1 << 24, OnDeath = 1 << 25, OnPerfectTiming = 1 << 26,
    AscensionMark = 1 << 27, Finisher = 1 << 28, ReturningWeapon = 1 << 29, Echo = 1 << 30
}
[Flags] public enum BoonOriginMask { None = 0, Player = 1, Projectile = 2, Proxy = 4, Echo = 8, Zone = 16, Trap = 32, ReturningWeapon = 64, Any = 127 }
[Flags] public enum BoonImpactMask { None = 0, Native = 1, Derived = 2, Echo = 4, StatusTick = 8, Any = 15 }
public enum BoonProducedImpact { Native, Derived, Echo, StatusTick }
public enum BoonEnemyPolicy { RespectControlProfile, PressureFallback, PostureFallback, MarkFallback, AlternativeEffect }
[Flags] public enum BoonPresentationCriteria { None = 0, ChangesInput = 1, ChangesTimingOrRisk = 2, ChangesGeometryOrOrigin = 4, ChangesSkillOrder = 8, AddsConditionalInteraction = 16, Any = 31 }

[Serializable]
public sealed class BoonEnemyPolicies
{
    [SerializeField] private BoonEnemyPolicy _common;
    [SerializeField] private BoonEnemyPolicy _heavy;
    [SerializeField] private BoonEnemyPolicy _elite;
    [SerializeField] private BoonEnemyPolicy _boss;
    public BoonEnemyPolicy Common => _common;
    public BoonEnemyPolicy Heavy => _heavy;
    public BoonEnemyPolicy Elite => _elite;
    public BoonEnemyPolicy Boss => _boss;
    public BoonEnemyPolicies(BoonEnemyPolicy common = BoonEnemyPolicy.RespectControlProfile,
        BoonEnemyPolicy heavy = BoonEnemyPolicy.RespectControlProfile,
        BoonEnemyPolicy elite = BoonEnemyPolicy.RespectControlProfile,
        BoonEnemyPolicy boss = BoonEnemyPolicy.RespectControlProfile)
    { _common = common; _heavy = heavy; _elite = elite; _boss = boss; }
    internal BoonEnemyPolicies Copy() => (BoonEnemyPolicies)MemberwiseClone();
}

/// <summary>Declarations only. Executors must still use the shared ProtocolBudget and enemy control authority.</summary>
[Serializable]
public sealed class BoonImpactRules
{
    [SerializeField] private BoonOriginMask _triggerOrigins = BoonOriginMask.Player;
    [SerializeField] private BoonImpactMask _triggerImpacts = BoonImpactMask.Native;
    [SerializeField] private BoonProducedImpact _producedImpact = BoonProducedImpact.Derived;
    [SerializeField] private int _maxInstancesPerCast = 1;
    [SerializeField] private int _maxDerivedImpactsPerCast = 1;
    [SerializeField] private int _maxDepth = 1;
    [SerializeField] private bool _canGenerateEnergy;
    [SerializeField] private bool _canCreateCausalRoot;
    [SerializeField] private bool _canSpawnSkillEntities;
    public BoonOriginMask TriggerOrigins => _triggerOrigins;
    public BoonImpactMask TriggerImpacts => _triggerImpacts;
    public BoonProducedImpact ProducedImpact => _producedImpact;
    public int MaxInstancesPerCast => _maxInstancesPerCast;
    public int MaxDerivedImpactsPerCast => _maxDerivedImpactsPerCast;
    public int MaxDepth => _maxDepth;
    public bool CanGenerateEnergy => _canGenerateEnergy;
    public bool CanCreateCausalRoot => _canCreateCausalRoot;
    /// <summary>Whole-skill entities (avatars, mine sets, persistent zones), not budgeted individual hits/projectiles.</summary>
    public bool CanSpawnSkillEntities => _canSpawnSkillEntities;
    public BoonImpactRules(BoonOriginMask triggerOrigins = BoonOriginMask.Player,
        BoonImpactMask triggerImpacts = BoonImpactMask.Native, BoonProducedImpact producedImpact = BoonProducedImpact.Derived,
        int maxInstancesPerCast = 1, int maxDerivedImpactsPerCast = 1, int maxDepth = 1,
        bool canGenerateEnergy = false, bool canCreateCausalRoot = false, bool canSpawnSkillEntities = false)
    {
        _triggerOrigins = triggerOrigins; _triggerImpacts = triggerImpacts; _producedImpact = producedImpact;
        _maxInstancesPerCast = maxInstancesPerCast; _maxDerivedImpactsPerCast = maxDerivedImpactsPerCast; _maxDepth = maxDepth;
        _canGenerateEnergy = canGenerateEnergy; _canCreateCausalRoot = canCreateCausalRoot; _canSpawnSkillEntities = canSpawnSkillEntities;
    }
    internal BoonImpactRules Copy() => (BoonImpactRules)MemberwiseClone();
}

[Serializable]
public sealed class BoonCostOverride
{
    [SerializeField] private bool _overrideManaCost;
    [SerializeField] private float _manaCost;
    [SerializeField] private bool _overrideCooldown;
    [SerializeField] private float _cooldown;
    public bool OverrideManaCost => _overrideManaCost;
    public float ManaCost => _manaCost;
    public bool OverrideCooldown => _overrideCooldown;
    public float Cooldown => _cooldown;
    public BoonCostOverride(bool overrideManaCost = false, float manaCost = 0,
        bool overrideCooldown = false, float cooldown = 0)
    { _overrideManaCost = overrideManaCost; _manaCost = manaCost; _overrideCooldown = overrideCooldown; _cooldown = cooldown; }
    internal BoonCostOverride Copy() => (BoonCostOverride)MemberwiseClone();
}

/// <summary>Inspector authoring. Stable IDs are explicit and never derived from titles or enum names.</summary>
[Serializable]
public sealed class BoonDefinition
{
    [SerializeField] private string _id;
    [SerializeField] private string _title;
    [SerializeField, TextArea] private string _description;
    [SerializeField] private BoonFamily _family;
    [SerializeField] private BoonWeaponMask _weapons;
    [SerializeField] private BoonSlotMask _slots;
    [SerializeField] private BoonCapability _requiredCapabilities;
    [SerializeField] private BoonCapability _anyRequiredCapabilities;
    [SerializeField] private BoonCapability _providedCapabilities;
    [SerializeField] private BoonCapability _removedCapabilities;
    [SerializeField] private List<string> _requiredBoonIds = new List<string>();
    [SerializeField] private List<string> _blockedBoonIds = new List<string>();
    [SerializeField] private int _maxRank = 1;
    [SerializeField] private bool _isRetainedSupport;
    [SerializeField] private bool _enabledForOffers;
    [SerializeField] private bool _isTransformative;
    [SerializeField] private BoonPresentationCriteria _presentation;
    [SerializeField] private BoonEnemyPolicies _enemyPolicies = new BoonEnemyPolicies();
    [SerializeField] private BoonImpactRules _impactRules = new BoonImpactRules();
    [SerializeField] private BoonCostOverride _cost = new BoonCostOverride();
    public string Id => _id;
    public string Title => _title;
    public string Description => _description;
    public BoonFamily Family => _family;
    public BoonWeaponMask Weapons => _weapons;
    public BoonSlotMask Slots => _slots;
    public BoonCapability RequiredCapabilities => _requiredCapabilities;
    public BoonCapability AnyRequiredCapabilities => _anyRequiredCapabilities;
    public BoonCapability ProvidedCapabilities => _providedCapabilities;
    public BoonCapability RemovedCapabilities => _removedCapabilities;
    public IReadOnlyList<string> RequiredBoonIds => _requiredBoonIds?.AsReadOnly();
    public IReadOnlyList<string> BlockedBoonIds => _blockedBoonIds?.AsReadOnly();
    public int MaxRank => _maxRank;
    public bool IsRetainedSupport => _isRetainedSupport;
    public bool EnabledForOffers => _enabledForOffers;
    public bool IsTransformative => _isTransformative;
    public BoonPresentationCriteria Presentation => _presentation;
    public BoonEnemyPolicies EnemyPolicies => _enemyPolicies;
    public BoonImpactRules ImpactRules => _impactRules;
    public BoonCostOverride Cost => _cost;
    public BoonDefinition(string id, BoonFamily family, BoonWeaponMask weapons, BoonSlotMask slots,
        BoonCapability requiredCapabilities = BoonCapability.None, BoonCapability providedCapabilities = BoonCapability.None,
        IEnumerable<string> requiredBoonIds = null, IEnumerable<string> blockedBoonIds = null, int maxRank = 1,
        bool isRetainedSupport = false, bool enabledForOffers = false, string title = null, string description = null,
        bool isTransformative = false, BoonPresentationCriteria presentation = BoonPresentationCriteria.None,
        BoonEnemyPolicies enemyPolicies = null, BoonImpactRules impactRules = null, BoonCostOverride cost = null,
        BoonCapability removedCapabilities = BoonCapability.None, BoonCapability anyRequiredCapabilities = BoonCapability.None)
    {
        _id = id; _title = title ?? id; _description = description ?? string.Empty; _family = family; _weapons = weapons; _slots = slots;
        _requiredCapabilities = requiredCapabilities; _anyRequiredCapabilities = anyRequiredCapabilities; _providedCapabilities = providedCapabilities; _removedCapabilities = removedCapabilities;
        _requiredBoonIds = requiredBoonIds == null ? new List<string>() : new List<string>(requiredBoonIds);
        _blockedBoonIds = blockedBoonIds == null ? new List<string>() : new List<string>(blockedBoonIds);
        _maxRank = maxRank; _isRetainedSupport = isRetainedSupport; _enabledForOffers = enabledForOffers;
        _isTransformative = isTransformative; _presentation = presentation;
        _enemyPolicies = enemyPolicies?.Copy() ?? new BoonEnemyPolicies();
        _impactRules = impactRules?.Copy() ?? new BoonImpactRules(); _cost = cost?.Copy() ?? new BoonCostOverride();
    }
    internal BoonDefinition Copy() => new BoonDefinition(_id, _family, _weapons, _slots, _requiredCapabilities, _providedCapabilities,
        _requiredBoonIds, _blockedBoonIds, _maxRank, _isRetainedSupport, _enabledForOffers, _title, _description,
        _isTransformative, _presentation, _enemyPolicies, _impactRules, _cost, _removedCapabilities, _anyRequiredCapabilities);
    internal BoonDefinitionSnapshot Snapshot() => new BoonDefinitionSnapshot(this);
}

public sealed class BoonDefinitionSnapshot
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public BoonFamily Family { get; }
    public BoonWeaponMask Weapons { get; }
    public BoonSlotMask Slots { get; }
    public BoonCapability RequiredCapabilities { get; }
    public BoonCapability AnyRequiredCapabilities { get; }
    public BoonCapability ProvidedCapabilities { get; }
    public BoonCapability RemovedCapabilities { get; }
    public IReadOnlyList<string> RequiredBoonIds { get; }
    public IReadOnlyList<string> BlockedBoonIds { get; }
    public int MaxRank { get; }
    public bool IsRetainedSupport { get; }
    public bool EnabledForOffers { get; }
    public bool IsTransformative { get; }
    public BoonPresentationCriteria Presentation { get; }
    public BoonEnemyPolicies EnemyPolicies { get; }
    public BoonImpactRules ImpactRules { get; }
    public BoonCostOverride Cost { get; }
    internal BoonDefinitionSnapshot(BoonDefinition source)
    {
        Id = source.Id; Title = source.Title; Description = source.Description; Family = source.Family;
        Weapons = source.Weapons; Slots = source.Slots; RequiredCapabilities = source.RequiredCapabilities;
        AnyRequiredCapabilities = source.AnyRequiredCapabilities; ProvidedCapabilities = source.ProvidedCapabilities; RemovedCapabilities = source.RemovedCapabilities;
        RequiredBoonIds = new List<string>(source.RequiredBoonIds).AsReadOnly();
        BlockedBoonIds = new List<string>(source.BlockedBoonIds).AsReadOnly();
        MaxRank = source.MaxRank; IsRetainedSupport = source.IsRetainedSupport; EnabledForOffers = source.EnabledForOffers;
        IsTransformative = source.IsTransformative; Presentation = source.Presentation;
        EnemyPolicies = source.EnemyPolicies.Copy(); ImpactRules = source.ImpactRules.Copy(); Cost = source.Cost.Copy();
    }
}
