using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Weapon/Combo Area Attack")]
public class ComboAreaAttackAbility : SequencedAreaAttackAbility
{
    [SerializeField] private AreaHitStep[] hitSteps =
    {
        // Each hit chips stance and staggers; only the finisher breaks stance into a knock-up.
        new AreaHitStep { delay = 0.15f, damageMultiplier = 1f, reactionType = HitReactionType.Stagger, hitStrength = HitStrength.Light, pushDistance = 0.3f, stanceDamage = 25f, breakEffect = StanceBreakEffect.None },
        new AreaHitStep { delay = 0.45f, damageMultiplier = 1.1f, reactionType = HitReactionType.Stagger, hitStrength = HitStrength.Medium, pushDistance = 0.5f, stanceDamage = 35f, breakEffect = StanceBreakEffect.None },
        new AreaHitStep { delay = 0.8f, damageMultiplier = 1.4f, reactionType = HitReactionType.Stagger, hitStrength = HitStrength.Heavy, pushDistance = 0.4f, stanceDamage = 55f, breakEffect = StanceBreakEffect.KnockUp, knockUpHeight = 2f, stunDuration = 1.1f }
    };

    public override void Activate(GameObject parent)
    {
        ActivateSequence(parent, hitSteps);
    }
}
