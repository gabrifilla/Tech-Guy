using System.Collections.Generic;
using UnityEngine;

public abstract class AttackPassiveAbility : PassiveAbility
{
    public virtual void OnAfterAttackHits(PlayerActor owner, IReadOnlyList<Actor> damagedActors)
    {
    }
}
