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
    [SerializeField] private HitReactionType reactionType = HitReactionType.Stagger;
    [SerializeField] private HitStrength hitStrength = HitStrength.Heavy;
    [SerializeField] private float pushDistance = 0.5f;
    [Tooltip("A heavy frontal blow: it breaks stance into a real Knockback, throwing the enemy away.")]
    [SerializeField] private StanceBreakEffect breakEffect = StanceBreakEffect.Knockback;
    [SerializeField] private float stanceDamage = 60f;
    [SerializeField] private float stunDuration = 1f;
    [SerializeField] private float knockUpHeight = 2f;
    [SerializeField] private float knockbackDistance = 5f;

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
            pushDistance = pushDistance,
            stanceDamage = stanceDamage,
            breakEffect = breakEffect,
            stunDuration = stunDuration,
            knockUpHeight = knockUpHeight,
            knockbackDistance = knockbackDistance
        };

        ActivateSequence(parent, new[] { hitStep });
    }
}
