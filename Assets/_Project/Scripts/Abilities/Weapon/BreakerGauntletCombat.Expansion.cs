using UnityEngine;

public sealed partial class BreakerGauntletCombat
{
    private void ScheduleExpansionEchoes(GauntletImpactContext parent, GauntletExecutionSnapshot snapshot, AreaHitStep source)
    {
        if (!_player || _player.IsDead || !_contexts.IsValid(parent)) return;
        if (snapshot.EchoingFist) ScheduleExpansionEcho(parent, snapshot, source, GauntletExpansionCatalog.EchoingFistId, .25f, .35f, .75f);
        if (snapshot.TemporalEcho) ScheduleExpansionEcho(parent, snapshot, source, GauntletExpansionCatalog.TemporalEchoId, .45f, .3f, 1f);
    }
    private void ScheduleExpansionEcho(GauntletImpactContext parent, GauntletExecutionSnapshot snapshot, AreaHitStep source,
        string producer, float delay, float fraction, float width)
    {
        var step = source.Copy(); step.damageMultiplier *= fraction; step.stanceDamage *= fraction;
        step.boxSize.x *= width; step.sphereRadius *= width;
        step.reactionType = HitReactionType.None; step.breakEffect = StanceBreakEffect.None;
        step.pushDistance = 0f; step.knockbackDistance = 0f; step.knockUpHeight = 0f; step.stunDuration = 0f;
        var child = new PendingChild { Context = _contexts.BeginChild(parent, GauntletImpactOrigin.Echo, producer) };
        _children.Add(child);
        child.Routine = StartCoroutine(ExecuteChild(child, step, snapshot.DirectStepCount + 64, delay,
            transform.localToWorldMatrix, transform.rotation, _weapon, snapshot));
    }
}
