using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class ProtocolRuntime
{
    private sealed class TitanicWork
    {
        public ProtocolExecution Child;
        public WeaponScript Weapon;
        public Vector3 Position, Forward;
        public Vector3 SpawnOrigin;
        public bool SpawnValidated;
        public float Travelled, Age, Damage;
        public int Mask;
        public TitanicFistsVisual Visual;
        public TitanicFistsVisual.TornadoView View;
        public long ViewGeneration;
        public long Lifecycle;
        public Guid Session;
        public int Node, Destination;
    }
    private struct TitanicCandidate
    { public Actor Actor; public float Distance; }
    private readonly List<TitanicWork> _titanicWork = new List<TitanicWork>();
    private readonly List<TitanicCandidate> _titanicCandidates = new List<TitanicCandidate>();
    private RaycastHit[] _titanicSweep;
    internal int PendingTitanicWaves => _titanicWork.Count;
    public int TitanicWavesResolved { get; private set; } // Compatibility diagnostic: now counts emitted tornadoes.
    internal Vector3? FirstTitanicPosition => _titanicWork.Count > 0 ? _titanicWork[0].Position : (Vector3?)null;
    internal Vector3 LastTitanicEndPosition { get; private set; }
    internal float LastTitanicTravelled { get; private set; }
    private long _lastCosmeticTerminal, _lastCosmeticEpoch;
    internal int CosmeticTitanicEmissions { get; private set; }

    private bool NoncombatTitanicAllowed => !_ended && _inventory != null && !_inventory.IsEnded && !_inventory.IsFaulted &&
        _boons && _boons.ProtocolSession != null && !_boons.ProtocolSession.IsEncounterClosing &&
        _boons.ProtocolSession.Phase == RunPhase.Traversal &&
        _boons.ProtocolSession.NodeState(EncounterId) == RunNodeState.Completed;

    // Called only by the authored scheduler after its real contact batch. No root, query
    // budget, damage queue or combat callback is created for this presentation lease.
    internal bool QueueNoncombatTitanicContact(GauntletImpactContext parent, int impactIndex,
        Vector3 origin, Vector3 forward, WeaponScript weapon)
    {
        if (!NoncombatTitanicAllowed || !_player || !_player.isActiveAndEnabled || _player.IsDead ||
            _player.IsResolvingGauntletImpact || _player.HasOpenImpactFeedback || !weapon || _player.CurrentWeapon != weapon ||
            parent == null || parent.Owner != _player || parent.Execution == _lastCosmeticTerminal && parent.Epoch == _lastCosmeticEpoch || impactIndex < 0 ||
            parent.Origin != GauntletImpactOrigin.Direct || parent.Slot != GauntletActionSlot.Basic ||
            parent.Snapshot?.BasicMutationId != TitanicFistsDefinition.Id || !parent.Snapshot.BasicFinisher ||
            !_player.TryGetComponent(out BreakerGauntletCombat combat) || !combat.EnergyPolicy.IsLive(parent) ||
            !ReactionNumeric.IsFinite(origin) || !ReactionNumeric.IsFinite(forward) ||
            !_player.TryGetComponent(out TitanicFistsVisual visual)) return false;
        forward.y = 0; forward.Normalize(); if (forward.sqrMagnitude < .99f) return false;
        var view = visual.AcquireTornado(_inventory.Catalog.Limits.LiveRoots, parent.Snapshot.TitanicEffects); if (view == null) return false;
        var work = new TitanicWork { Weapon = weapon, Position = origin + forward * TitanicFistsDefinition.SpawnOffset,
            SpawnOrigin = origin, Forward = forward, Visual = visual, View = view, ViewGeneration = view.Generation,
            Lifecycle = LifecycleRevision, Session = _inventory.SessionId, Node = EncounterId,
            Destination = _boons.ProtocolSession.PendingDestination };
        _lastCosmeticTerminal = parent.Execution; _lastCosmeticEpoch = parent.Epoch;
        visual.UpdateTornado(view, work.ViewGeneration, work.Position, forward, 0);
        _titanicWork.Add(work); CosmeticTitanicEmissions++; return true;
    }

    internal bool QueueTitanicFists(GauntletImpactContext parent, Vector3 origin, Vector3 forward,
        float range, Vector3 box, LayerMask layers, WeaponScript weapon)
    {
        if (!IsOpen || parent?.Snapshot?.BasicMutationId != TitanicFistsDefinition.Id ||
            !parent.Snapshot.BasicFinisher || !weapon || _player.CurrentWeapon != weapon ||
            !ReactionNumeric.IsFinite(origin) || !ReactionNumeric.IsFinite(forward)) return false;
        forward.y = 0; forward = forward.normalized;
        if (forward.sqrMagnitude < .99f) return false;
        var child = ReserveTitanicTerminalEmission(parent);
        if (child == null) return false;
        var work = new TitanicWork { Child = child, Weapon = weapon,
            Position = origin + forward * TitanicFistsDefinition.SpawnOffset, SpawnOrigin = origin, Forward = forward,
            Damage = child.Root.TitanicTerminalDamage * TitanicFistsDefinition.DamageCoefficient,
            Mask = layers.value != 0 ? layers.value : Physics.DefaultRaycastLayers };
        if (_player.TryGetComponent(out TitanicFistsVisual visual))
        {
            work.Visual = visual; work.View = visual.AcquireTornado(_inventory.Catalog.Limits.LiveRoots, child.BasicLineage.Effects);
            if (work.View == null) { child.Dispose(); return false; }
            work.ViewGeneration = work.View.Generation;
            visual.UpdateTornado(work.View, work.ViewGeneration, work.Position, work.Forward, 0);
        }
        _titanicWork.Add(work); TitanicWavesResolved++;
        return true;
    }
    private void ReleaseTitanic(TitanicWork work)
    {
        LastTitanicEndPosition = work.Position; LastTitanicTravelled = work.Travelled;
        work.Child?.Dispose();
        if (work.Visual) work.Visual.ReleaseTornado(work.View, work.ViewGeneration);
    }
    private void ClearTitanicFists()
    {
        foreach (var work in _titanicWork) ReleaseTitanic(work);
        _titanicWork.Clear();
        if (_player && _player.TryGetComponent(out TitanicFistsVisual visual)) visual.Clear();
    }
    private bool TitanicLive(TitanicWork work) => (work.Child != null ? work.Child.IsValid && work.Child.IsLifecycleCurrent :
        NoncombatTitanicAllowed && work.Lifecycle == LifecycleRevision && work.Session == _inventory.SessionId &&
        work.Node == EncounterId && work.Destination == _boons.ProtocolSession.PendingDestination) && _player &&
        !_player.IsDead && _player.isActiveAndEnabled && _player.CurrentWeapon == work.Weapon &&
        _player.RunModifiers?.Expansion.MutationId(BoonSlot.Basic) == TitanicFistsDefinition.Id;
    private void TickTitanicFists()
    {
        for (int i = _titanicWork.Count - 1; i >= 0; i--)
        {
            if (i >= _titanicWork.Count) continue;
            var work = _titanicWork[i];
            bool ended = !TitanicLive(work);
            bool canAdvance = work.Child != null ? CanResolveBasicBranch : NoncombatTitanicAllowed &&
                !_player.IsCombatHitStopped && Time.timeScale > 0 && (!_holder || !_holder.IsGamePaused);
            if (!ended && canAdvance && ScaledDelta > 0 && !_draining)
            {
                float delta = Mathf.Min(ScaledDelta, TitanicFistsDefinition.Lifetime - work.Age);
                work.Age += delta;
                float distance = Mathf.Min(TitanicFistsDefinition.TravelSpeed * delta,
                    TitanicFistsDefinition.TravelDistance - work.Travelled);
                ended = !AdvanceTitanic(work, distance);
                if (!ended) ended = work.Travelled >= TitanicFistsDefinition.TravelDistance - .0001f;
                if (!ended) ended = work.Age >= TitanicFistsDefinition.Lifetime;
                if (!ended && work.Visual) work.Visual.UpdateTornado(work.View, work.ViewGeneration, work.Position, work.Forward, work.Age);
            }
            if (ended)
            {
                ReleaseTitanic(work);
                if (i < _titanicWork.Count && _titanicWork[i] == work) _titanicWork.RemoveAt(i);
            }
        }
    }
    private static void TitanicCapsule(Vector3 foot, out Vector3 a, out Vector3 b)
    {
        float center = TitanicFistsDefinition.Height * .5f + .05f;
        float half = TitanicFistsDefinition.Height * .5f - TitanicFistsDefinition.Radius;
        a = foot + Vector3.up * (center - half); b = foot + Vector3.up * (center + half);
    }
    private bool AdvanceTitanic(TitanicWork work, float distance)
    {
        if (!TitanicLive(work) || !ReactionNumeric.IsFinite(distance) || distance <= 0) return false;
        if (work.Child == null)
        {
            // Reuse the wind view and travel constants, but never enter the combat sweep.
            Vector3 next = work.Position + work.Forward * distance;
            if (!TitanicSight((work.SpawnValidated ? work.Position : work.SpawnOrigin) + Vector3.up * .8f,
                next + Vector3.up * .8f)) return false;
            work.SpawnValidated = true; work.Position = next; work.Travelled += distance; return true;
        }
        if (!Budget.ReserveQuery() || !Budget.ReserveQuery()) return false; // overlap start + continuous sweep
        bool spawnCheck = !work.SpawnValidated;
        if (spawnCheck && !TitanicSight(work.SpawnOrigin + Vector3.up * .8f, work.Position + Vector3.up * .8f)) return false;
        work.SpawnValidated = true;
        if (_titanicSweep == null || _titanicSweep.Length != _queryColliders.Length) _titanicSweep = new RaycastHit[_queryColliders.Length];
        TitanicCapsule(work.Position, out var a, out var b);
        int mask = Physics.DefaultRaycastLayers | work.Mask;
        int overlap = Physics.OverlapCapsuleNonAlloc(a, b, TitanicFistsDefinition.Radius, _queryColliders, mask, QueryTriggerInteraction.Collide);
        int swept = Physics.CapsuleCastNonAlloc(a, b, TitanicFistsDefinition.Radius, work.Forward, _titanicSweep,
            distance, mask, QueryTriggerInteraction.Collide);
        if (overlap >= _queryColliders.Length || swept >= _titanicSweep.Length) return false; // saturated geometry fails closed
        _titanicCandidates.Clear(); float stop = distance; bool blocked = false;
        for (int i = 0; i < overlap; i++) { CollectTitanic(_queryColliders[i], 0, work, ref stop, ref blocked); _queryColliders[i] = null; }
        for (int i = 0; i < swept; i++) CollectTitanic(_titanicSweep[i].collider, _titanicSweep[i].distance, work, ref stop, ref blocked);
        if (blocked && stop <= .0001f) return false;
        _titanicCandidates.Sort((x, y) => { int order = x.Distance.CompareTo(y.Distance); return order != 0 ? order :
            EntityId.ToULong(x.Actor.GetEntityId()).CompareTo(EntityId.ToULong(y.Actor.GetEntityId())); });
        var root = work.Child.Root;
        if (!root.BeginBatch()) return false;
        _draining = true; _processingBatch = true;
        try
        {
            using (OpenExecutionScope(work.Child))
            {
                int sights = spawnCheck ? 1 : 0;
                foreach (var candidate in _titanicCandidates)
                {
                    if (candidate.Distance > stop || ++sights > _inventory.Catalog.Limits.SightTestsPerQuery) break;
                    var actor = candidate.Actor;
                    Vector3 from = work.Position + Vector3.up * .8f, to = actor.transform.position + Vector3.up * .8f;
                    if (TitanicSight(from, to)) QueueDamage(root, actor, work.Damage, ProtocolProducer.Technique, 1, work.Child.BasicLineage, work.Child);
                }
                DrainDamage();
            }
        }
        finally
        {
            while (_damageQueue.Count > 0) _damageQueue.Dequeue().Root.ReleaseQueueItem();
            root.EndBatch(); _draining = false; _processingBatch = false;
        }
        if (!TitanicLive(work)) return false;
        float movement = Mathf.Max(0, stop - (blocked ? .005f : 0));
        work.Position += work.Forward * movement; work.Travelled += movement;
        return !blocked;
    }
    private void CollectTitanic(Collider collider, float distance, TitanicWork work, ref float stop, ref bool blocked)
    {
        if (!collider) return;
        var actor = collider.GetComponentInParent<Actor>();
        if (!actor)
        {
            if (!collider.isTrigger && (Physics.DefaultRaycastLayers & (1 << collider.gameObject.layer)) != 0)
            { stop = Mathf.Min(stop, distance); blocked = true; }
            return;
        }
        if (actor == _player || actor.IsDead || !actor.isActiveAndEnabled || actor.Affiliation != CombatAffiliation.Enemy ||
            (work.Mask & (1 << collider.gameObject.layer)) == 0) return;
        if (_boons && _boons.ProtocolSession != null && (!actor.TryGetComponent(out ProtocolEncounterMembership membership) ||
            !membership.Matches(work.Child.Root.SessionId, work.Child.Root.EncounterId))) return;
        for (int i = 0; i < _titanicCandidates.Count; i++)
            if (_titanicCandidates[i].Actor == actor)
            { if (distance < _titanicCandidates[i].Distance) _titanicCandidates[i] = new TitanicCandidate { Actor = actor, Distance = distance }; return; }
        _titanicCandidates.Add(new TitanicCandidate { Actor = actor, Distance = distance });
    }
    private static bool TitanicSight(Vector3 from, Vector3 to)
    {
        var ray = to - from;
        foreach (var hit in Physics.RaycastAll(from, ray.normalized, ray.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (!hit.collider.GetComponentInParent<Actor>()) return false;
        return true;
    }
}
