using UnityEngine;

/// <summary>Prefab data read by EnemyVariant; never writes to the shared profile.</summary>
public sealed class EnemyStatAffix : MonoBehaviour
{
    [SerializeField] private string _displayName = "Acelerado";
    [SerializeField, Min(0.1f)] private float _movementMultiplier = 1.35f;
    [SerializeField, Min(0.1f)] private float _attackSpeedMultiplier = 1.2f;
    [SerializeField, Range(0.1f, 1f)] private float _damageTakenMultiplier = 1f;
    public string DisplayName => _displayName;
    public float MovementMultiplier => Mathf.Max(0.1f, _movementMultiplier);
    public float AttackSpeedMultiplier => Mathf.Max(0.1f, _attackSpeedMultiplier);
    public float DamageTakenMultiplier => Mathf.Clamp(_damageTakenMultiplier, 0.1f, 1f);
}
