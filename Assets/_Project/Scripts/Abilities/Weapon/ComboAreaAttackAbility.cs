using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Weapon/Combo Area Attack")]
public class ComboAreaAttackAbility : SequencedAreaAttackAbility
{
    [SerializeField] private AreaHitStep[] hitSteps =
    {
        new AreaHitStep { delay = 0.15f, damageMultiplier = 1f, reactionType = HitReactionType.Flinch, hitStrength = HitStrength.Light, poiseDamage = 10f },
        new AreaHitStep { delay = 0.45f, damageMultiplier = 1.1f, reactionType = HitReactionType.Knockback, hitStrength = HitStrength.Medium, poiseDamage = 20f, knockbackForce = 5f },
        new AreaHitStep { delay = 0.8f, damageMultiplier = 1.4f, reactionType = HitReactionType.Launch, hitStrength = HitStrength.Heavy, poiseDamage = 35f, knockbackForce = 4f, launchForce = 5f }
    };

    public override void Activate(GameObject parent)
    {
        ActivateSequence(parent, hitSteps);
    }
}
