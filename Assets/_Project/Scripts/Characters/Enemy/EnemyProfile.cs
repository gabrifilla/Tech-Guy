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

    public EnemyRarity Rarity => _rarity;
    public float HealthMultiplier => Mathf.Max(0.01f, _healthMultiplier);
    public float DamageMultiplier => Mathf.Max(0f, _damageMultiplier);
    public float MovementMultiplier => Mathf.Max(0.01f, _movementMultiplier);
    public float AttackSpeedMultiplier => Mathf.Max(0.01f, _attackSpeedMultiplier);
    public System.Collections.Generic.IReadOnlyList<GameObject> AffixPrefabs => _affixPrefabs;
}
