using UnityEngine;

/// <summary>
/// A passive training target for the Nexus lobby. It reuses the existing <see cref="Actor"/>
/// combat pipeline (damage, health bar and <see cref="EnemyCombatFeedback"/>) so the player can
/// try the equipped weapon's abilities, but it is intentionally indestructible and rewardless:
///
/// - <see cref="Death"/> is overridden to restore health instead of destroying the object, so the
///   dummy becomes a valid target again the moment it "falls" (Requisito 4.3).
/// - The <see cref="CoinDrop"/> that <see cref="Actor.Awake"/> auto-attaches to every non-player
///   Actor is removed here, so hitting or "defeating" the dummy grants no coins (Requisito 4.4).
/// - No <see cref="EnemyAI"/> is added, so the dummy never chases or attacks the player
///   (Requisito 4.5). The <see cref="EnemyCombatFeedback"/> added by <see cref="Actor.Awake"/> is
///   kept for damage numbers and the health bar (Requisito 4.2).
///
/// Because damage is resolved against any <see cref="Actor"/> (areas, projectiles, hitboxes), the
/// dummy only needs a collider on an appropriate layer and a high health pool to stay usable
/// regardless of the currently equipped weapon (Requisito 4.6).
/// </summary>
[DisallowMultipleComponent]
public sealed class TrainingDummy : Actor
{
    [Tooltip("Health the dummy is initialized with when none is authored. Kept high so it stays usable for continuous practice.")]
    [SerializeField, Min(1f)] private float _defaultHealth = 1000f;

    public override void Awake()
    {
        // Ensure a high health pool for continuous use even when the field was left unset.
        if (health <= 0f) health = _defaultHealth;

        base.Awake();

        // Actor.Awake auto-attaches CoinDrop to every non-player Actor. A training dummy must never
        // reward the player, so strip it right after base setup (Requisito 4.4).
        if (TryGetComponent<CoinDrop>(out var coinDrop))
            Destroy(coinDrop);
    }

    /// <summary>
    /// Instead of destroying the dummy when health reaches zero, restore it to full so it stays a
    /// usable target (Requisito 4.3). Reuses <see cref="Actor.RestoreHealthToMax"/>, which also
    /// clears the dead flag, so no GameObject is ever left destroyed behind (Error Handling: the
    /// dummy never leaves a dangling reference).
    /// </summary>
    protected override void Death()
    {
        RestoreHealthToMax();
    }
}
