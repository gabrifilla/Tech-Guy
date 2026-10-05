using System.Collections.Generic;
using UnityEngine;

public sealed partial class ProtocolRuntime
{
    private readonly Queue<ProtocolDamageWork> _damageQueue = new Queue<ProtocolDamageWork>();
    private Collider[] _queryColliders = new Collider[128];
    private readonly List<Actor> _queryTargets = new List<Actor>(128);
    private bool _draining;
    partial void InitializeDamageBuffers() { _queryColliders = new Collider[_inventory.Catalog.Limits.CollidersPerQuery]; }
    private struct ProtocolDamageWork
    {
        public ProtocolRootContext Root;
        public Actor Target;
        public ProtocolTargetHandle Handle;
        public ProtocolProducer Producer;
        public int Depth;
        public float Damage;
        public bool Emission;
        public Vector3 Position;
        public float Radius;
        public int Maximum;
        public ProtocolFeedbackKind Feedback;
        public bool DeadSource;
    }
    private bool Matches(ProtocolRootContext root) => root != null && root.IsValid &&
        root.SessionId == _inventory.SessionId && root.EncounterId == EncounterId && !_inventory.IsEnded;

    internal void SubmitConfirmedImpact(ProtocolRootContext root, Actor target, ProtocolHitSnapshot before,
        float damage, ProtocolProducer producer, int depth = 0)
    {
        using (Profile())
        {
        if (!Matches(root) || before.Affiliation != CombatAffiliation.Enemy ||
            (!before.IsLethal(damage) && (!target || target.CombatGeneration != before.Handle.Generation)) ||
            !ReactionNumeric.IsFinite(damage) || !ReactionNumeric.IsFinite(before.Position) || damage <= 0 || !before.TryClaim() || !root.BeginBatch()) return;
        bool ownsDrain = !_draining;
        if (ownsDrain) { _draining = true; _processingBatch = true; }
        try
        {
            using (OpenCallbackScope(root, depth))
            ProcessConfirmed(root, target, before, damage, producer, depth);
            if (ownsDrain) DrainDamage();
        }
        finally
        {
            if (ownsDrain)
                while (_damageQueue.Count > 0) _damageQueue.Dequeue().Root.ReleaseQueueItem();
            root.EndBatch();
            if (ownsDrain) { _draining = false; _processingBatch = false; }
        }
        }
    }

    private void ProcessConfirmed(ProtocolRootContext root, Actor target, ProtocolHitSnapshot before,
        float damage, ProtocolProducer producer, int depth)
    {
        if (before.IsLethal(damage))
        {
            if (!ProtocolEligibility.Detonates(producer) || !root.ClaimDeath(before.Handle.TargetId, before.Handle.Generation)) return;
            _boons?.Hooks?.RaiseProtocolImpact(new ProtocolCombatEvent(root, before, producer, depth, damage));
            if (depth >= _inventory.Catalog.Limits.MaxDepth) return;
            QueueConductor(root, target, before, damage, producer, depth, true);
            int rank = root.Rank(ProtocolKind.Detonation);
            if (rank > 0)
            {
                var definition = _inventory.Catalog.Get(ProtocolKind.Detonation);
                float radius = definition.RadiusBase + definition.RadiusPerRank * rank;
                if (!before.Chill && before.Burn) radius *= _inventory.Catalog.Limits.CombustionRadiusMultiplier;
                QueueEmission(root, target, before.Position, radius,
                    damage * (definition.DamageFactorBase + definition.DamageFactorPerAdditionalRank * (rank - 1)),
                    ProtocolProducer.Detonation, depth + 1, before.Chill ? definition.MaxVictims : 128, before.Handle,
                    before.Chill ? ProtocolFeedbackKind.Shatter : before.Burn ? ProtocolFeedbackKind.Combustion : ProtocolFeedbackKind.Detonation, true);
            }
            return;
        }
        if (!target || target.IsDead || target.Affiliation != CombatAffiliation.Enemy || target.CombatGeneration != before.Handle.Generation) return;
        if (producer != ProtocolProducer.Environment) _boons?.Hooks?.RaiseProtocolImpact(new ProtocolCombatEvent(root, before, producer, depth, damage));
        if (!Matches(root) || !target || target.IsDead || target.CombatGeneration != before.Handle.Generation) return;
        if (depth >= _inventory.Catalog.Limits.MaxDepth) return;
        bool fire = false, frost = false;
        if (ProtocolEligibility.AppliesElements(producer)) ApplyElements(root, target, damage, out fire, out frost);
        if (!Matches(root) || !target || target.IsDead || target.CombatGeneration != before.Handle.Generation) return;
        if (ProtocolEligibility.Shocks(producer))
        {
            if (fire && before.Chill) QueueDamage(root, target, damage * _inventory.Catalog.Limits.ThermalShockMultiplier, ProtocolProducer.ThermalShock, depth + 1);
            if (frost && before.Burn) QueueDamage(root, target, damage * _inventory.Catalog.Limits.ThermalShockMultiplier, ProtocolProducer.ThermalShock, depth + 1);
        }
        int overflow = root.Rank(ProtocolKind.Overflow);
        if (overflow > 0 && ProtocolEligibility.Overflows(producer) &&
            (ProtocolElementQuery.HasBurn(target) || ProtocolElementQuery.HasChill(target)))
            QueueDamage(root, target, damage * _inventory.Catalog.Get(ProtocolKind.Overflow).DamagePerRank * overflow, ProtocolProducer.Overflow, depth + 1);
        QueueConductor(root, target, before, damage, producer, depth, false);
    }
    private void QueueConductor(ProtocolRootContext root, Actor target, ProtocolHitSnapshot before, float damage,
        ProtocolProducer producer, int depth, bool deadSource)
    {
        int conductor = root.Rank(ProtocolKind.Conductor);
        if (conductor > 0 && ProtocolEligibility.Conducts(producer))
        {
            var definition = _inventory.Catalog.Get(ProtocolKind.Conductor);
            QueueEmission(root, target, before.Position, definition.RadiusBase + definition.RadiusPerRank * conductor,
                damage * definition.DamageFactorBase, ProtocolProducer.Conductor, depth + 1, conductor, before.Handle,
                ProtocolFeedbackKind.Conductor, deadSource);
        }
    }
    private void QueueEmission(ProtocolRootContext root, Actor source, Vector3 position, float radius,
        float damage, ProtocolProducer producer, int depth, int maximum, ProtocolTargetHandle? sourceHandle = null,
        ProtocolFeedbackKind feedback = ProtocolFeedbackKind.Conductor, bool deadSource = false)
    {
        if (!ReactionNumeric.IsFinite(damage) || damage <= 0 || !root.ReserveQueueItem()) return;
        _damageQueue.Enqueue(new ProtocolDamageWork { Root = root, Target = source,
            Handle = sourceHandle ?? new ProtocolTargetHandle(EntityId.ToULong(source.GetEntityId()), source.CombatGeneration),
            Damage = damage, Producer = producer, Depth = depth, Emission = true,
            Position = position, Radius = radius, Maximum = maximum, Feedback = feedback, DeadSource = deadSource });
    }
    private void QueueDamage(ProtocolRootContext root, Actor target, float damage, ProtocolProducer producer, int depth)
    {
        if (!target || target.IsDead || !ReactionNumeric.IsFinite(damage) || damage <= 0 || !root.ReserveQueueItem()) return;
        _damageQueue.Enqueue(new ProtocolDamageWork { Root = root, Target = target,
            Handle = new ProtocolTargetHandle(EntityId.ToULong(target.GetEntityId()), target.CombatGeneration),
            Damage = damage, Producer = producer, Depth = depth });
    }
    private void DrainDamage()
    {
        while (_damageQueue.Count > 0)
        {
            var work = _damageQueue.Dequeue();
            try
            {
                if (work.Emission)
                {
                    if (Matches(work.Root) && (work.DeadSource ||
                        work.Target && !work.Target.IsDead && work.Target.CombatGeneration == work.Handle.Generation))
                        Emit(work.Root, work.Target, work.Handle, work.Position, work.Radius, work.Damage, work.Producer, work.Depth, work.Maximum, work.Feedback);
                    continue;
                }
                if (!Matches(work.Root) || !work.Target || work.Target.IsDead ||
                    work.Target.CombatGeneration != work.Handle.Generation || !work.Root.ReserveImpact(work.Depth)) continue;
                var before = new ProtocolHitSnapshot(work.Target);
                float amount = work.Damage;
                if (ProtocolEligibility.Amplifies(work.Producer)) amount *= 1f +
                    _inventory.Catalog.Get(ProtocolKind.Resonance).DamagePerRank * work.Root.Rank(ProtocolKind.Resonance) *
                    ((before.Burn ? 1 : 0) + (before.Chill ? 1 : 0));
                if (!ReactionNumeric.IsFinite(amount)) continue;
                if (work.Producer == ProtocolProducer.ThermalShock || work.Producer == ProtocolProducer.Overflow)
                    _feedback?.Emit(work.Producer == ProtocolProducer.ThermalShock ? ProtocolFeedbackKind.ThermalShock : ProtocolFeedbackKind.Overflow,
                        before.Position, before.Position, .55f);
                float actual = 0;
                using (work.Target.CaptureNextDamage(value => actual = value)) work.Target.TakeDamage(amount);
                if (actual > 0 && (before.IsLethal(actual) || work.Target && work.Target.CombatGeneration == before.Handle.Generation))
                    using (OpenCallbackScope(work.Root, work.Depth))
                    ProcessConfirmed(work.Root, work.Target, before, actual, work.Producer, work.Depth);
            }
            finally { work.Root.ReleaseQueueItem(); }
        }
    }
    private void Emit(ProtocolRootContext root, Actor source, ProtocolTargetHandle sourceHandle, Vector3 position, float radius,
        float damage, ProtocolProducer producer, int depth, int maximum, ProtocolFeedbackKind feedback)
    {
        if (!Budget.ReserveQuery()) return;
        if (producer == ProtocolProducer.Detonation)
            _feedback?.Emit(feedback, position, position, radius, _inventory.Catalog.Limits.ShatterMinTrails + root.Rank(ProtocolKind.Detonation) - 1);
        if (producer == ProtocolProducer.Detonation)
            using (OpenCallbackScope(root, depth - 1)) _boons?.Hooks?.RaiseExplosion(position);
        int count = Physics.OverlapSphereNonAlloc(position, radius, _queryColliders,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        _queryTargets.Clear();
        for (int i = 0; i < count; i++)
        {
            var candidate = _queryColliders[i].GetComponentInParent<Actor>();
            _queryColliders[i] = null;
            if (!candidate || candidate == source && candidate.CombatGeneration == sourceHandle.Generation || candidate.Affiliation != CombatAffiliation.Enemy || candidate.IsDead ||
                !candidate.isActiveAndEnabled || _queryTargets.Contains(candidate)) continue;
            if (_boons && _boons.ProtocolSession != null && (!candidate.TryGetComponent(out ProtocolEncounterMembership membership) ||
                !membership.Matches(root.SessionId, root.EncounterId))) continue;
            _queryTargets.Add(candidate);
        }
        _queryTargets.Sort((left, right) => {
            int order = (left.transform.position - position).sqrMagnitude.CompareTo((right.transform.position - position).sqrMagnitude);
            return order != 0 ? order : EntityId.ToULong(left.GetEntityId()).CompareTo(EntityId.ToULong(right.GetEntityId()));
        });
        int admitted = 0;
        int sightTests = 0;
        for (int i = 0; i < _queryTargets.Count && admitted < maximum && sightTests < _inventory.Catalog.Limits.SightTestsPerQuery; i++)
        {
            sightTests++;
            var candidate = _queryTargets[i];
            Vector3 delta = candidate.transform.position - position;
            if (Physics.Raycast(position + Vector3.up * .5f, delta.normalized, out var obstruction,
                delta.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                obstruction.collider.GetComponentInParent<Actor>() != candidate &&
                obstruction.collider.GetComponentInParent<Actor>() != source) continue;
            if (!root.ClaimPropagation(EntityId.ToULong(candidate.GetEntityId()), candidate.CombatGeneration)) continue;
            if (producer == ProtocolProducer.Conductor) _feedback?.Emit(ProtocolFeedbackKind.Conductor, position, candidate.transform.position, radius);
            QueueDamage(root, candidate, damage, producer, depth); admitted++;
        }
    }
    partial void ResolveBurnTick(ProtocolElementApplication value)
    {
        if (!value.Valid) return;
        var root = Budget.BeginRoot(value.SessionId, value.EncounterId, _inventory,
            ProtocolProducer.BurnTick, value.TickOrdinal, value.BurnRanks);
        if (root == null) return;
        try
        {
            var before = new ProtocolHitSnapshot(value.Target);
            float actual = 0;
            using (value.Target.CaptureNextDamage(amount => actual = amount))
                value.Target.TakeDamage(value.BurnDps * _inventory.Catalog.Get(ProtocolKind.Ignite).TickInterval);
            SubmitConfirmedImpact(root, value.Target, before, actual, ProtocolProducer.BurnTick);
        }
        finally { root.Seal(); }
    }
}
