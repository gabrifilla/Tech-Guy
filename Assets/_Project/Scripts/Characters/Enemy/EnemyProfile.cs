using UnityEngine;

public enum EnemyRarity { Normal, Magic, Rare }

[CreateAssetMenu(menuName = "Tech Guy/Enemies/Profile")]
public sealed class EnemyProfile : ScriptableObject
{
    [SerializeField] private EnemyRarity _rarity;
    [SerializeField, Min(0.01f)] private float _healthMultiplier = 1f;
    [SerializeField, Min(0f)] private float _damageMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float _movementMultiplier = 1f;
    [SerializeField, Min(0.01f)] private float _attackSpeedMultiplier = 1f;
    [Tooltip("Optional child prefabs containing reusable enemy abilities and visuals.")]
    [SerializeField] private GameObject[] _affixPrefabs;

    [Header("Stance / Stagger")]
    [Tooltip("Total stance a fresh enemy has. A hit's stance damage subtracts from this; reaching 0 is a Stance Break. Higher = harder to break.")]
    [SerializeField, Min(1f)] private float _maxStance = 100f;
    [Tooltip("Multiplier applied to incoming stance damage. <1 makes the enemy tougher to stagger, >1 easier.")]
    [SerializeField, Min(0f)] private float _stanceDamageMultiplier = 1f;
    [Tooltip("Stance regained per second after the recovery delay.")]
    [SerializeField, Min(0f)] private float _stanceRecoveryPerSecond = 25f;
    [Tooltip("Seconds after the last hit before stance starts recovering.")]
    [SerializeField, Min(0f)] private float _stanceRecoveryDelay = 2.5f;

    [Header("Crowd-control (0 = full duration, 1 = immune)")]
    [Tooltip("Resistance to the short interrupt from a Stagger. 1 makes the enemy ignore stagger.")]
    [SerializeField, Range(0f, 1f)] private float _staggerResistance;
    [Tooltip("Resistance to Stun on a break. 1 = immune to stun.")]
    [SerializeField, Range(0f, 1f)] private float _stunResistance;
    [Tooltip("Resistance to KnockUp / air-juggle on a break. 1 = immune (never leaves the ground).")]
    [SerializeField, Range(0f, 1f)] private float _knockUpResistance;
    [Tooltip("Resistance to Knockback on a break. 1 = immune (never thrown).")]
    [SerializeField, Range(0f, 1f)] private float _knockbackResistance;

    [Header("Phase 2 reaction category (independent of rarity)")]
    [SerializeField] private ReactionCategory _reactionCategory;
    [SerializeField] private ReactionAuthoring _reaction = new ReactionAuthoring();

    public ReactionCategory ReactionCategory => _reactionCategory;
    public ReactionConfiguration ResolveReaction(EnemyRank rank) => new ReactionConfiguration(rank, _reactionCategory, _reaction, StunResistance);

    public System.Collections.Generic.List<string> ValidateReactionData()
    {
        var errors = new System.Collections.Generic.List<string>();
        ReactionNumeric.Validate(errors, nameof(_maxStance), _maxStance, 1f, float.MaxValue);
        ReactionNumeric.Validate(errors, nameof(_stanceDamageMultiplier), _stanceDamageMultiplier, 0f, 5f);
        ReactionNumeric.Validate(errors, nameof(_stanceRecoveryPerSecond), _stanceRecoveryPerSecond, 0f, float.MaxValue);
        ReactionNumeric.Validate(errors, nameof(_stanceRecoveryDelay), _stanceRecoveryDelay, 0f, 10f);
        ReactionNumeric.Validate(errors, nameof(_staggerResistance), _staggerResistance, 0f, 1f);
        ReactionNumeric.Validate(errors, nameof(_stunResistance), _stunResistance, 0f, 1f);
        ReactionNumeric.Validate(errors, nameof(_knockUpResistance), _knockUpResistance, 0f, 1f);
        ReactionNumeric.Validate(errors, nameof(_knockbackResistance), _knockbackResistance, 0f, 1f);
        if (!System.Enum.IsDefined(typeof(ReactionCategory), _reactionCategory)) errors.Add("Undefined reaction category.");
        _reaction?.Validate(errors);
        return errors;
    }

    private void OnValidate()
    {
        foreach (string error in ValidateReactionData()) Debug.LogError($"EnemyProfile '{name}': {error}", this);
    }

    public EnemyRarity Rarity => _rarity;
    public float HealthMultiplier => Mathf.Max(0.01f, _healthMultiplier);
    public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);
    public float MovementMultiplier => Mathf.Max(0.01f, _movementMultiplier);
    public float AttackSpeedMultiplier => Mathf.Max(0.01f, _attackSpeedMultiplier);
    public System.Collections.Generic.IReadOnlyList<GameObject> AffixPrefabs => _affixPrefabs;

    public float MaxStance => ReactionNumeric.Safe(_maxStance, 1f, float.MaxValue, 100f);
    public float StanceDamageMultiplier => ReactionNumeric.Safe(_stanceDamageMultiplier, 0f, 5f, 1f);
    public float StanceRecoveryPerSecond => ReactionNumeric.Safe(_stanceRecoveryPerSecond, 0f, float.MaxValue, 25f);
    public float StanceRecoveryDelay => ReactionNumeric.Safe(_stanceRecoveryDelay, 0f, 10f, 2.5f);
    public float StaggerResistance => ReactionNumeric.Safe(_staggerResistance, 0f, 1f, 0f);
    public float StunResistance => ReactionNumeric.Safe(_stunResistance, 0f, 1f, 0f);
    public float KnockUpResistance => ReactionNumeric.Safe(_knockUpResistance, 0f, 1f, 0f);
    public float KnockbackResistance => ReactionNumeric.Safe(_knockbackResistance, 0f, 1f, 0f);
}
