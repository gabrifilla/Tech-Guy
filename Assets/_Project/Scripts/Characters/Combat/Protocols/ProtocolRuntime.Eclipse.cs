using System.Collections.Generic;
using UnityEngine;

public sealed partial class ProtocolRuntime
{
    /// <summary>One bounded Technique emission using the finisher's reserved root, never a standalone cast.</summary>
    internal int EmitEclipseFall(ProtocolExecution execution, Vector3 position, float radius, float damage, float posture)
    {
        TouchFrame();
        if (!IsOpen || execution == null || execution.Owner != this || !execution.IsValid || !execution.IsLifecycleCurrent ||
            execution.Producer != ProtocolProducer.Technique || execution.Depth != 1 ||
            _boons && _boons.ProtocolSession?.IsEncounterClosing == true || !ReactionNumeric.IsFinite(position) ||
            !ReactionNumeric.IsFinite(radius) || radius <= 0 || !ReactionNumeric.IsFinite(damage) || damage < 0 ||
            !ReactionNumeric.IsFinite(posture) || posture < 0 || !Budget.ReserveQuery()) return 0;
        var root = execution.Root;
        var targets = new List<Actor>(_inventory.Catalog.Limits.CollidersPerQuery);
        int count = Physics.OverlapSphereNonAlloc(position, radius, _queryColliders, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var collider = _queryColliders[i]; _queryColliders[i] = null;
            var candidate = collider ? collider.GetComponentInParent<Actor>() : null;
            if (!candidate || candidate == _player || candidate.IsDead || !candidate.isActiveAndEnabled ||
                candidate.Affiliation != CombatAffiliation.Enemy || targets.Contains(candidate)) continue;
            if (_boons && _boons.ProtocolSession != null && (!candidate.TryGetComponent(out ProtocolEncounterMembership member) ||
                !member.Matches(root.SessionId, root.EncounterId))) continue;
            targets.Add(candidate);
        }
        targets.Sort((a,b) => EntityId.ToULong(a.GetEntityId()).CompareTo(EntityId.ToULong(b.GetEntityId())));
        int before = root.ImpactAttempts;
        if (!root.BeginBatch()) return 0;
        bool ownsDrain = !_draining;
        if (ownsDrain) { _draining = true; _processingBatch = true; }
        try
        {
            using (OpenExecutionScope(execution))
            {
                int sightTests = 0;
                foreach (var target in targets)
                {
                    if (!Matches(root) || sightTests++ >= _inventory.Catalog.Limits.SightTestsPerQuery) break;
                    Vector3 start = position + Vector3.up * .5f;
                    Vector3 delta = target.transform.position + Vector3.up * .5f - start;
                    if (delta.sqrMagnitude > .0001f && Physics.Raycast(start, delta.normalized, out var hit, delta.magnitude,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                        hit.collider.GetComponentInParent<Actor>() != target && hit.collider.GetComponentInParent<Actor>() != _player) continue;
                    if (!root.ReserveQueueItem()) break;
                    _damageQueue.Enqueue(new ProtocolDamageWork { Root = root, Target = target,
                        Handle = new ProtocolTargetHandle(EntityId.ToULong(target.GetEntityId()), target.CombatGeneration),
                        Producer = ProtocolProducer.Technique, Depth = 1, Damage = damage, Posture = posture });
                }
                if (ownsDrain) DrainDamage();
            }
        }
        finally
        {
            if (ownsDrain) while (_damageQueue.Count > 0) _damageQueue.Dequeue().Root.ReleaseQueueItem();
            root.EndBatch();
            if (ownsDrain) { _draining = false; _processingBatch = false; }
        }
        int admitted = root.ImpactAttempts - before;
        if (admitted > 0) _feedback?.Emit(ProtocolFeedbackKind.EclipseFall, position, position, radius);
        return admitted;
    }
}
