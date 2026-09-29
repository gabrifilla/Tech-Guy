/// <summary>
/// Stable identifier for each of the 16 core enemy archetypes. Resolved by
/// <c>EnemyVariant</c> at configure time without <c>GameObject.Find</c> or
/// <c>FindObjectOfType</c>, and used for magic-string-free logging, safe-default
/// fallback, and switch-based behavior wiring.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 1.5, 22.2.</remarks>
public enum ArchetypeId
{
    // Melee
    Rush,
    Grunt,
    Heavy,
    Charger,

    // Ranged
    Shooter,
    SpreadShooter,
    Sniper,
    Bomber,

    // Control
    HazardCaster,
    Hooker,

    // Support
    Healer,
    ShieldSupport,

    // Swarm
    Swarm,
    Spawner,

    // Special
    Fragile,
    Mirror
}
