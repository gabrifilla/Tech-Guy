using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Weapon/Front Area Attack")]
public class FrontAreaAttackAbility : SequencedAreaAttackAbility
{
    [Header("Area")]
    [SerializeField] private float rangeOverride = 0f;
    [SerializeField] private AreaHitShape hitShape = AreaHitShape.Box;
    [SerializeField] private Vector3 boxSize = new Vector3(3f, 2f, 0f);
    [SerializeField] private float sphereRadius = 1.5f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Damage")]
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float bonusDamage = 0f;

    [Header("Reaction")]
    [SerializeField] private HitReactionType reactionType = HitReactionType.Knockback;
    [SerializeField] private HitStrength hitStrength = HitStrength.Medium;
    [SerializeField] private float poiseDamage = 30f;
    [SerializeField] private float stunDuration = 0.35f;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float launchForce = 0f;
    [SerializeField] private bool canAirJuggle = true;
    [SerializeField] private bool canRagdoll = false;

    public override void Activate(GameObject parent)
    {
        AreaHitStep hitStep = new AreaHitStep
        {
            delay = 0f,
            rangeOverride = rangeOverride,
            hitShape = hitShape,
            boxSize = boxSize,
            sphereRadius = sphereRadius,
            targetLayers = targetLayers,
            damageMultiplier = damageMultiplier,
            bonusDamage = bonusDamage,
            reactionType = reactionType,
            hitStrength = hitStrength,
            poiseDamage = poiseDamage,
            stunDuration = stunDuration,
            knockbackForce = knockbackForce,
            launchForce = launchForce,
            canAirJuggle = canAirJuggle,
            canRagdoll = canRagdoll
        };

        ActivateSequence(parent, new[] { hitStep });
    }
}
