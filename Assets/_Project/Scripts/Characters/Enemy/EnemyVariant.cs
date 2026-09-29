using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Actor), typeof(EnemyAI))]
public sealed class EnemyVariant : MonoBehaviour
{
    [SerializeField] private EnemyProfile _profile;
    [Tooltip("Optional core-archetype identity for this enemy. When assigned, its EnemyProfile drives the existing stat/stance paths, its Combat_Role is recorded, its role behavior is ensured, and priority archetypes get a marker.")]
    [SerializeField] private EnemyArchetype _archetype;
    [Tooltip("Optional abilities for this enemy variant, independent of its rarity profile.")]
    [SerializeField] private GameObject[] _additionalAffixPrefabs;
    private Actor _actor;
    private EnemyAI _ai;
    private NavMeshAgent _agent;
    private float _baseHealth, _baseDamage, _baseSpeed, _baseInterval;
    private bool _started;
    private Vector3 _baseScale;
    public float VisualScaleMultiplier => EnemyVisualStyle.SizeMultiplier(_profile ? _profile.Rarity : EnemyRarity.Normal, GetComponent<SectorBoss>());
    private readonly List<GameObject> _affixes = new List<GameObject>();

    public EnemyProfile Profile => _profile;
    public string AffixSummary { get; private set; } = "";

    /// <summary>The assigned core archetype, or null when this enemy is not archetype-driven.</summary>
    public EnemyArchetype Archetype => _archetype;

    /// <summary>
    /// The resolved <see cref="ArchetypeId"/> of the assigned archetype, or <c>null</c> when this enemy
    /// is not archetype-driven (or fell back to the Grunt-equivalent safe default, which clears the
    /// archetype). <c>EnemyAI</c> reads this in its attack coroutine / perception to pick the
    /// archetype-scoped <c>EnemyAttackPatterns.Select</c> / <c>EngagementRange</c> overloads; a null id
    /// keeps the pre-archetype trait-only behavior so existing enemies are unchanged. This is a plain
    /// field read of the serialized reference — no <c>GameObject.Find</c> / <c>FindObjectOfType</c> (R1.5).
    /// </summary>
    public ArchetypeId? ArchetypeId => _archetype ? _archetype.Id : (ArchetypeId?)null;

    /// <summary>
    /// The primary <see cref="CombatRole"/> recorded from the assigned archetype at configure time.
    /// Defaults to <see cref="CombatRole.MeleePressure"/> (the Grunt-equivalent role) when no
    /// archetype is assigned. Task 11.2 hardens the unresolved/duplicate-id fallback.
    /// </summary>
    public CombatRole CombatRole { get; private set; } = CombatRole.MeleePressure;

    private void Start()
    {
        _actor = GetComponent<Actor>();
        _baseScale = transform.localScale;
        _ai = GetComponent<EnemyAI>();
        _agent = GetComponent<NavMeshAgent>();
        _baseHealth = _actor.maxHealth;
        _baseDamage = _ai.attackDamage;
        _baseInterval = _ai.timeBetweenAttacks;
        _baseSpeed = _agent ? _agent.speed : 0f;
        _started = true;
        ApplyProfile();
    }

    public void Configure(EnemyProfile profile)
    {
        _profile = profile;
        if (_started) ApplyProfile();
    }

    public void Configure(EnemyProfile profile, GameObject[] affixPrefabs)
    {
        _additionalAffixPrefabs = affixPrefabs;
        Configure(profile);
    }

    /// <summary>
    /// Assigns a core <see cref="EnemyArchetype"/> and (re)applies configuration. The archetype's
    /// <see cref="EnemyProfile"/> feeds the existing stat/stance paths, so callers do not pass a
    /// separate profile here.
    /// </summary>
    public void Configure(EnemyArchetype archetype)
    {
        _archetype = archetype;
        if (_started) ApplyProfile();
    }

    private void ApplyProfile()
    {
        // Resolve the assigned archetype (if any) BEFORE the existing stat pipeline runs so its
        // EnemyProfile flows through the same SetMaxHealth / ConfigureAttack / ConfigureAttackTraits /
        // ConfigureStance / agent.speed calls the variant already uses. Resolution is a plain field
        // read of the serialized reference: no GameObject.Find / FindObjectOfType (R1.5).
        ResolveArchetypeProfile();

        transform.localScale = _baseScale * VisualScaleMultiplier;
        foreach (GameObject affix in _affixes)
        {
            if (!affix) continue;
            affix.SetActive(false);
            Destroy(affix);
        }
        _affixes.Clear();
        if (TryGetComponent(out SectorBoss boss))
        {
            AffixSummary = "";
            _actor.SetDamageTakenMultiplier(this,1f);
            _actor.SetMaxHealth(boss.MaximumHealth);
            return;
        }
        var selected = new HashSet<GameObject>();
        if (_profile && _profile.AffixPrefabs != null)
            foreach (var prefab in _profile.AffixPrefabs) if (prefab) selected.Add(prefab);
        if (_additionalAffixPrefabs != null)
            foreach (var prefab in _additionalAffixPrefabs) if (prefab) selected.Add(prefab);
        float movement = 1f, attackSpeed = 1f, damageTaken = 1f;
        var names = new List<string>();
        EnemyAttackTraits traits = EnemyAttackTraits.None;
        foreach (var prefab in selected)
        {
            if (prefab.GetComponentInChildren<EnemyFrostAura>(true))
            { names.Add("Gelo"); traits |= EnemyAttackTraits.Frost; }
            foreach (var affix in prefab.GetComponentsInChildren<EnemyStatAffix>(true))
            {
                if (affix.MovementMultiplier > 1f || affix.AttackSpeedMultiplier > 1f) traits |= EnemyAttackTraits.Haste;
                if (affix.DamageTakenMultiplier < 1f) traits |= EnemyAttackTraits.Guard;
                movement *= affix.MovementMultiplier;
                attackSpeed *= affix.AttackSpeedMultiplier;
                damageTaken *= affix.DamageTakenMultiplier;
                names.Add(affix.DisplayName);
            }
        }
        AffixSummary = string.Join(" · ", names);
        _ai.ConfigureAttackTraits(traits);
        ApplyStanceProfile();
        _actor.SetDamageTakenMultiplier(this, damageTaken);
        _actor.SetMaxHealth(_baseHealth * (_profile ? _profile.HealthMultiplier : 1f));
        _ai.ConfigureAttack(_baseDamage * (_profile ? _profile.DamageMultiplier : 1f),
            _baseInterval / ((_profile ? _profile.AttackSpeedMultiplier : 1f) * attackSpeed));
        if (_agent) _agent.speed = _baseSpeed * (_profile ? _profile.MovementMultiplier : 1f) * movement;

        // Archetype identity extras: record the Combat_Role, seed archetype attack traits, ensure the
        // role-behavior component exists for non-attack roles, and add a priority marker when required.
        // Runs after the stat pipeline so it layers on top of the profile-driven configuration.
        ApplyArchetype();

        if (_actor.IsDead) return;
        var added = new HashSet<GameObject>();
        if (_profile) SpawnAffixes(_profile.AffixPrefabs, added);
        SpawnAffixes(_additionalAffixPrefabs, added);
    }

    /// <summary>
    /// Feeds the enemy's stance pool and crowd-control resistances into its CombatReactionController.
    /// The rank (already set by the spawner) stays authoritative for the enemy category; the profile
    /// supplies the concrete stance numbers and per-CC resistances on top of it.
    /// </summary>
    private void ApplyStanceProfile()
    {
        CombatReactionController reaction = GetComponentInChildren<CombatReactionController>();
        if (!reaction) return;

        EnemyRank resolvedRank = reaction.Rank;
        if (!_profile)
        {
            // Missing stance/resistance configuration for this enemy (R3.7): with no EnemyProfile to
            // source concrete numbers from, fall back to the data-driven per-rank defaults
            // (RankReactionDefaults, applied by ConfigureRank) and log a stable missing-config
            // identifier so the gap is traceable. This never interrupts the hit — the enemy ends in a
            // valid, fully-configured reaction state and processing continues normally.
            Debug.LogWarning(
                $"EnemyVariant on '{name}': no EnemyProfile assigned; applying per-rank reaction " +
                $"defaults for {resolvedRank} (missing config id '{RankReactionDefaults.MissingConfigId(resolvedRank)}').",
                this);
            reaction.ConfigureRank(resolvedRank);
            return;
        }

        reaction.ConfigureStance(
            resolvedRank,
            _profile.MaxStance,
            _profile.StanceDamageMultiplier,
            _profile.StanceRecoveryPerSecond,
            _profile.StanceRecoveryDelay,
            _profile.StaggerResistance,
            _profile.StunResistance,
            _profile.KnockUpResistance,
            _profile.KnockbackResistance);
    }

    private void SpawnAffixes(IReadOnlyList<GameObject> prefabs, HashSet<GameObject> added)
    {
        if (prefabs == null) return;
        foreach (GameObject prefab in prefabs)
        {
            if (prefab && added.Add(prefab)) _affixes.Add(Instantiate(prefab, transform, false));
        }
    }

    /// <summary>
    /// When an <see cref="EnemyArchetype"/> is assigned, validates it and adopts its
    /// <see cref="EnemyProfile"/> as the active profile so the existing stat/stance pipeline (health,
    /// attack, traits, stance, speed) consumes the archetype's numbers through its normal paths.
    /// <para>
    /// Safe-default fallback (R1.7): if the archetype reference is assigned but unresolvable — the
    /// reference is null, its <see cref="EnemyArchetype.Profile"/> is missing, or its
    /// <see cref="ArchetypeId"/> is not a defined enum value (a guard that also covers a future
    /// project-wide catalog resolving an id to the wrong or a duplicate definition) — this logs an
    /// error naming the id (or "unassigned" when there is no archetype/id) and falls back to a
    /// Grunt-equivalent safe default: the baseline non-archetype profile path
    /// (<c>_profile</c> left as authored / null), <see cref="CombatRole.MeleePressure"/>, and no role
    /// behavior component. The archetype reference is cleared so the rest of the pipeline
    /// (<see cref="ApplyArchetype"/>) treats this enemy as the baseline Grunt-equivalent. This never
    /// throws — the enemy always ends in a valid configuration.
    /// </para>
    /// </summary>
    private void ResolveArchetypeProfile()
    {
        // No archetype assigned at all: this is the ordinary non-archetype path (baseline profile,
        // MeleePressure default), not an error. Nothing to resolve or fall back from.
        if (!_archetype) return;

        // A single serialized reference cannot structurally "resolve to more than one definition",
        // but per design we still validate the resolved archetype's Id is a defined ArchetypeId so a
        // future catalog (or a corrupted/mismatched asset) is caught by the same guard. A missing
        // Profile is equally unresolvable for the stat pipeline. Guard both, plus a null reference.
        bool idValid = System.Enum.IsDefined(typeof(ArchetypeId), _archetype.Id);
        if (!idValid || !_archetype.Profile)
        {
            FallBackToSafeDefault(idValid ? _archetype.Id.ToString() : ((int)_archetype.Id).ToString());
            return;
        }

        _profile = _archetype.Profile;
    }

    /// <summary>
    /// Logs the unresolved/mismatched archetype id (R1.7) and drops back to the Grunt-equivalent safe
    /// default without throwing: clears the archetype reference so <see cref="ApplyArchetype"/> becomes
    /// a no-op (baseline profile behavior, no role behavior, no priority marker) and forces
    /// <see cref="CombatRole"/> to <see cref="CombatRole.MeleePressure"/>. The active profile is left as
    /// authored (typically null → all multipliers 1.0, all CC resistances 0, i.e. the Grunt baseline).
    /// </summary>
    /// <param name="id">The unresolvable id to name in the log, or "unassigned" when none is available.</param>
    private void FallBackToSafeDefault(string id)
    {
        Debug.LogError(
            $"EnemyVariant on '{name}': archetype id '{(string.IsNullOrEmpty(id) ? "unassigned" : id)}' " +
            "could not be resolved (missing profile or undefined id). Falling back to the Grunt-equivalent " +
            "safe default (baseline profile, MeleePressure, no role behavior).",
            this);

        // Drop the archetype so the rest of the configuration pipeline treats this enemy as the
        // baseline non-archetype (Grunt-equivalent): ApplyArchetype no-ops, so no role behavior and no
        // priority marker are added, and CombatRole stays MeleePressure.
        _archetype = null;
        CombatRole = CombatRole.MeleePressure;
    }

    /// <summary>
    /// Applies the non-stat parts of an assigned archetype on top of the profile pipeline: records the
    /// <see cref="CombatRole"/>, seeds the archetype's default attack traits, ensures the role-behavior
    /// component exists for non-attack roles, and adds a <see cref="PriorityTargetMarker"/> for priority
    /// archetypes (R21.1). No-op when no archetype is assigned.
    /// </summary>
    private void ApplyArchetype()
    {
        if (!_archetype) return;

        CombatRole = _archetype.PrimaryRole;

        // Seed the archetype's default attack traits so the attack selector starts from the archetype's
        // intent. Affix-derived traits were already applied above; OR them together so neither is lost.
        if (_archetype.Traits != EnemyAttackTraits.None)
            _ai.ConfigureAttackTraits(_ai.AttackTraits | _archetype.Traits);

        EnsureRoleBehavior();

        if (_archetype.IsPriorityTarget && !TryGetComponent(out PriorityTargetMarker _))
            gameObject.AddComponent<PriorityTargetMarker>();
    }

    /// <summary>
    /// Guarantees the correct role-behavior component is present for non-attack archetypes
    /// (Healer, Shield_Support, Spawner, Hazard_Caster) and the Mirror's <see cref="FrontalReflector"/>.
    /// Attack-shaped archetypes (Rush, Grunt, Heavy, Charger, Shooter, Spread_Shooter, Sniper, Bomber,
    /// Hooker) need no component and are skipped. When the archetype supplies a
    /// <see cref="EnemyArchetype.BehaviorTemplate"/> its component type is added; otherwise the type is
    /// selected from the archetype id so authored assets that leave the template empty still work.
    /// </summary>
    private void EnsureRoleBehavior()
    {
        System.Type behaviorType = ResolveRoleBehaviorType();
        if (behaviorType == null) return; // Attack-shaped archetype: no role component required.

        if (!GetComponent(behaviorType))
            gameObject.AddComponent(behaviorType);
    }

    /// <summary>
    /// Resolves the concrete role-behavior component type for the assigned archetype, preferring the
    /// archetype's serialized <see cref="EnemyArchetype.BehaviorTemplate"/> and otherwise mapping from
    /// the <see cref="ArchetypeId"/>. Returns null for attack-shaped archetypes that need no component.
    /// </summary>
    private System.Type ResolveRoleBehaviorType()
    {
        if (_archetype.BehaviorTemplate)
            return _archetype.BehaviorTemplate.GetType();

        switch (_archetype.Id)
        {
            case global::ArchetypeId.Healer: return typeof(HealerBehavior);
            case global::ArchetypeId.ShieldSupport: return typeof(ShieldSupportBehavior);
            case global::ArchetypeId.Spawner: return typeof(SpawnerBehavior);
            case global::ArchetypeId.HazardCaster: return typeof(HazardCasterBehavior);
            case global::ArchetypeId.Mirror: return typeof(FrontalReflector);
            default: return null; // Attack-shaped: Rush, Grunt, Heavy, Charger, Shooter, SpreadShooter,
                                   // Sniper, Bomber, Hooker, Swarm, Fragile — driven by the attack pipeline.
        }
    }
}
