using System.Collections.Generic;
using UnityEngine;

public sealed partial class ProtocolRuntime
{
    /// <summary>Queues a resolved descendant amount. True means admission, never synchronous HP confirmation.</summary>
    internal bool EmitBoonTarget(ProtocolExecution execution, Actor target, long generation, float damage, float posture = 0f)
    {
        TouchFrame();
        if (!CanEmitBoon(execution, damage, posture) || !target || target.IsDead || !target.isActiveAndEnabled ||
            target.Affiliation != CombatAffiliation.Enemy || target.CombatGeneration != generation) return false;
        var root = execution.Root;
        if (_boons && _boons.ProtocolSession != null && (!target.TryGetComponent(out ProtocolEncounterMembership member) ||
            !member.Matches(root.SessionId, root.EncounterId))) return false;
        if (!root.BeginBatch()) return false;
        bool ownsDrain = !_draining;
        if (ownsDrain) { _draining = true; _processingBatch = true; }
        bool queued = false;
        try
        {
            using (OpenExecutionScope(execution))
            {
                if (root.ReserveQueueItem())
                {
                    _damageQueue.Enqueue(new ProtocolDamageWork { Root = root, Target = target,
                        Handle = new ProtocolTargetHandle(EntityId.ToULong(target.GetEntityId()), generation),
                        Producer = execution.Producer, Depth = execution.Depth, Damage = damage, Posture = posture });
                    queued = true;
                }
                if (ownsDrain) DrainDamage();
            }
        }
        finally
        {
            root.EndBatch();
            if (ownsDrain)
            {
                while (_damageQueue.Count > 0) _damageQueue.Dequeue().Root.ReleaseQueueItem();
                _draining = false; _processingBatch = false;
            }
        }
        return queued;
    }

    /// <summary>Bounded LOS-aware descendant area. Returns queue admissions; damage and posture share the root's cap.</summary>
    internal int EmitBoonArea(ProtocolExecution execution, Vector3 position, float radius, float damage, float posture = 0f)
    {
        TouchFrame();
        if (!CanEmitBoon(execution, damage, posture) || !ReactionNumeric.IsFinite(position) ||
            !ReactionNumeric.IsFinite(radius) || radius <= 0f || !Budget.ReserveQuery()) return 0;
        var root = execution.Root;
        var targets = new List<Actor>(_inventory.Catalog.Limits.CollidersPerQuery);
        int count = Physics.OverlapSphereNonAlloc(position + Vector3.up * .6f, radius, _queryColliders,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            var hit = _queryColliders[i]; _queryColliders[i] = null;
            var target = hit ? hit.GetComponentInParent<Actor>() : null;
            if (!target || target == _player || target.IsDead || !target.isActiveAndEnabled ||
                target.Affiliation != CombatAffiliation.Enemy || targets.Contains(target)) continue;
            if (_boons && _boons.ProtocolSession != null && (!target.TryGetComponent(out ProtocolEncounterMembership member) ||
                !member.Matches(root.SessionId, root.EncounterId))) continue;
            targets.Add(target);
        }
        targets.Sort((a, b) => EntityId.ToULong(a.GetEntityId()).CompareTo(EntityId.ToULong(b.GetEntityId())));
        int admitted = 0, sightTests = 0;
        if (!root.BeginBatch()) return 0;
        bool ownsDrain = !_draining;
        if (ownsDrain) { _draining = true; _processingBatch = true; }
        try
        {
            using (OpenExecutionScope(execution))
            {
                foreach (var target in targets)
                {
                    if (!Matches(root) || sightTests++ >= _inventory.Catalog.Limits.SightTestsPerQuery) break;
                    Vector3 start = position + Vector3.up * .6f;
                    Vector3 delta = target.transform.position + Vector3.up * .6f - start;
                    var obstacles = Physics.RaycastAll(start, delta.normalized, delta.magnitude,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                    bool blocked = false;
                    foreach (var obstacle in obstacles)
                        if (!obstacle.collider.GetComponentInParent<Actor>()) { blocked = true; break; }
                    if (blocked || !root.ReserveQueueItem()) continue;
                    _damageQueue.Enqueue(new ProtocolDamageWork { Root = root, Target = target,
                        Handle = new ProtocolTargetHandle(EntityId.ToULong(target.GetEntityId()), target.CombatGeneration),
                        Producer = execution.Producer, Depth = execution.Depth, Damage = damage, Posture = posture });
                    admitted++;
                }
                if (ownsDrain) DrainDamage();
            }
        }
        finally
        {
            root.EndBatch();
            if (ownsDrain)
            {
                while (_damageQueue.Count > 0) _damageQueue.Dequeue().Root.ReleaseQueueItem();
                _draining = false; _processingBatch = false;
            }
        }
        return admitted;
    }

    internal bool IsBoonEnemy(Actor target) => target && target != _player && !target.IsDead &&
        target.isActiveAndEnabled && target.Affiliation == CombatAffiliation.Enemy &&
        (!(_boons && _boons.ProtocolSession != null) || target.TryGetComponent(out ProtocolEncounterMembership member) &&
            member.Matches(_inventory.SessionId, EncounterId));

    private bool CanEmitBoon(ProtocolExecution execution, float damage, float posture) => IsOpen && execution != null &&
        execution.Owner == this && execution.IsValid && execution.IsLifecycleCurrent && execution.Depth > 0 &&
        (execution.Producer == ProtocolProducer.Technique || execution.Producer == ProtocolProducer.Echo) &&
        !(_boons && _boons.ProtocolSession?.IsEncounterClosing == true) &&
        ReactionNumeric.IsFinite(damage) && damage >= 0f && ReactionNumeric.IsFinite(posture) && posture >= 0f;
}
