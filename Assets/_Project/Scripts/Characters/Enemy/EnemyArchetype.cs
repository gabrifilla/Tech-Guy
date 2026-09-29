using UnityEngine;

/// <summary>
/// Data identity for one of the 16 core enemy archetypes: the tuple of a stable
/// <see cref="ArchetypeId"/>, an <see cref="EnemyProfile"/> stat/stance configuration,
/// default <see cref="EnemyAttackTraits"/>, exactly one primary <see cref="CombatRole"/>,
/// and an optional role-behavior template for non-attack roles. Resolved by
/// <c>EnemyVariant</c> at configure time through a serialized reference (no
/// <c>GameObject.Find</c> / <c>FindObjectOfType</c>, no global registry).
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 1.1, 1.4, 22.2.</remarks>
[CreateAssetMenu(menuName = "Tech Guy/Enemies/Archetype")]
public sealed class EnemyArchetype : ScriptableObject
{
    [Tooltip("Stable identifier for this archetype, used for logging, safe-default fallback, and behavior wiring.")]
    [SerializeField] private ArchetypeId _id;

    [Tooltip("Stat / stance / crowd-control configuration applied through the existing EnemyVariant paths.")]
    [SerializeField] private EnemyProfile _profile;

    [Tooltip("Default attack traits that seed the attack selector for this archetype.")]
    [SerializeField] private EnemyAttackTraits _traits = EnemyAttackTraits.None;

    [Tooltip("The single primary tactical role this archetype fulfils.")]
    [SerializeField] private CombatRole _primaryRole;

    // NOTE: Typed as MonoBehaviour as a forward reference until the abstract
    // ArchetypeBehavior base is introduced in task 7.1; attack-shaped archetypes
    // (Rush, Grunt, Heavy, Charger, Shooter, Spread_Shooter, Sniper, Bomber,
    // Hooker, Hazard_Caster) leave this empty. Once ArchetypeBehavior exists this
    // field can be retyped to it without breaking authored assets.
    [Tooltip("Optional role-behavior component template for non-attack roles (Healer, ShieldSupport, Spawner, HazardCaster). Attack-shaped archetypes leave this empty.")]
    [SerializeField] private MonoBehaviour _behaviorTemplate;

    /// <summary>Stable identifier resolved by <c>EnemyVariant</c>.</summary>
    public ArchetypeId Id => _id;

    /// <summary>Stat/stance profile this archetype applies.</summary>
    public EnemyProfile Profile => _profile;

    /// <summary>Default attack traits seeding the attack selector.</summary>
    public EnemyAttackTraits Traits => _traits;

    /// <summary>The single primary combat role for this archetype.</summary>
    public CombatRole PrimaryRole => _primaryRole;

    /// <summary>Optional role-behavior template; null for attack-shaped archetypes.</summary>
    public MonoBehaviour BehaviorTemplate => _behaviorTemplate;

    /// <summary>
    /// True when this archetype must be visually legible as a high-value kill,
    /// i.e. its primary role is Ally-Support or Priority-Threat (Healer,
    /// Shield Support, Spawner).
    /// </summary>
    public bool IsPriorityTarget =>
        _primaryRole == CombatRole.AllySupport || _primaryRole == CombatRole.PriorityThreat;
}
