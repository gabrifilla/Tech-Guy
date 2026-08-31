using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Passives/Stat Modifier Passive")]
public class StatModifierPassiveAbility : PassiveAbility
{
    [SerializeField] private PlayerStatModifier[] modifiers;

    public override void OnAcquired(PlayerActor owner)
    {
        if (owner == null || owner.Stats == null) return;

        owner.Stats.AddModifiers(modifiers, this);
    }

    public override void OnRemoved(PlayerActor owner)
    {
        if (owner == null || owner.Stats == null) return;

        owner.Stats.RemoveModifiersFrom(this);
    }
}
