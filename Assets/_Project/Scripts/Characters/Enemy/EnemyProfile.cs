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

    public EnemyRarity Rarity => _rarity;
    public float HealthMultiplier => Mathf.Max(0.01f, _healthMultiplier);
    public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);
    public float MovementMultiplier => Mathf.Max(0.01f, _movementMultiplier);
    public float AttackSpeedMultiplier => Mathf.Max(0.01f, _attackSpeedMultiplier);
    public System.Collections.Generic.IReadOnlyList<GameObject> AffixPrefabs => _affixPrefabs;

    public float MaxStance => Mathf.Max(1f, _maxStance);
    public float StanceDamageMultiplier => Mathf.Max(0f, _stanceDamageMultiplier);
    public float StanceRecoveryPerSecond => Mathf.Max(0f, _stanceRecoveryPerSecond);
    public float StanceRecoveryDelay => Mathf.Max(0f, _stanceRecoveryDelay);
    public float StaggerResistance => Mathf.Clamp01(_staggerResistance);
    public float StunResistance => Mathf.Clamp01(_stunResistance);
    public float KnockUpResistance => Mathf.Clamp01(_knockUpResistance);
    public float KnockbackResistance => Mathf.Clamp01(_knockbackResistance);
}
