using UnityEngine;

/// <summary>A stationary, nonlethal practice target. It cannot disappear during a lesson.</summary>
public sealed class TutorialTrainingTarget : Actor
{
    protected override void Death() => RestoreHealthToMax();
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(Mathf.Min(Mathf.Max(0, amount), Mathf.Max(0, health - 1)));
    }
    private void LateUpdate() { if (!IsDead && health < maxHealth) RestoreHealthToMax(); }
}
