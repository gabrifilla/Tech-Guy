using System;
using System.Collections.Generic;

/// <summary>Per-owner frame limits. Neither a new root nor reentry renews the frame budget.</summary>
public sealed class ProtocolBudget
{
    private readonly ProtocolLimits _limits;
    private readonly HashSet<ProtocolRootContext> _roots = new HashSet<ProtocolRootContext>();
    private readonly List<ProtocolRootContext> _expiredRoots = new List<ProtocolRootContext>(64);
    private int _frame = -1;
    private long _nextRoot;
    public float ScaledTime { get; private set; }
    public bool CanAdvance { get; private set; }
    public int FrameImpacts { get; private set; }
    public int FrameQueries { get; private set; }
    public int FrameTicks { get; private set; }
    public int LiveRoots => _roots.Count;
    public long RootsCreated => _nextRoot;
    public bool HasConfirmedBatch { get { foreach (var root in _roots) if (root.HasConfirmedBatch) return true; return false; } }
    public int DroppedImpacts { get; private set; }
    public int DroppedRoots { get; private set; }
    public int DroppedQueries { get; private set; }
    public int DroppedTicks { get; private set; }
    public int DroppedQueueItems { get; private set; }
    public int DroppedReservations { get; private set; }
    public int DroppedApplications { get; private set; }
    public int MaximumQueue { get; private set; }
    internal void DropImpact() { if (DroppedImpacts < int.MaxValue) DroppedImpacts++; }
    internal void DropQueue() { if (DroppedQueueItems < int.MaxValue) DroppedQueueItems++; }
    internal void DropReservation() { if (DroppedReservations < int.MaxValue) DroppedReservations++; }
    internal void DropApplication() { if (DroppedApplications < int.MaxValue) DroppedApplications++; }
    internal void RecordQueue(int count) { MaximumQueue = Math.Max(MaximumQueue, count); }
    public ProtocolBudget(ProtocolLimits limits) { _limits = limits ?? throw new ArgumentNullException(nameof(limits)); }
    public void AdvanceFrame(int frame, float scaledDelta)
    {
        if (frame < 0 || float.IsNaN(scaledDelta) || float.IsInfinity(scaledDelta) || scaledDelta < 0)
            throw new ArgumentOutOfRangeException(nameof(scaledDelta));
        if (frame == _frame) return;
        if (frame < _frame) throw new ArgumentOutOfRangeException(nameof(frame));
        _frame = frame; CanAdvance = scaledDelta > 0;
        ScaledTime += scaledDelta;
        FrameImpacts = 0; FrameQueries = 0; FrameTicks = 0;
        _expiredRoots.Clear();
        foreach (var root in _roots)
            if (root.IsExpired && !root.HasConfirmedBatch) { root.Invalidate(); _expiredRoots.Add(root); }
        foreach (var root in _expiredRoots) _roots.Remove(root);
        _expiredRoots.Clear();
    }
    public ProtocolRootContext BeginRoot(Guid sessionId, int encounterId, ProtocolInventory inventory, ProtocolProducer producer = ProtocolProducer.Direct, long executionId = 0, ProtocolRanks capturedRanks = null)
    {
        if (sessionId == Guid.Empty || !Enum.IsDefined(typeof(ProtocolProducer), producer) || executionId < 0 || inventory == null || inventory.SessionId != sessionId ||
            inventory.IsFaulted || inventory.IsEnded) return null;
        if (_roots.Count >= _limits.LiveRoots) { if (DroppedRoots < int.MaxValue) DroppedRoots++; return null; }
        var root = new ProtocolRootContext(this, _limits, checked(++_nextRoot), sessionId, encounterId, inventory, ScaledTime, producer, executionId, capturedRanks);
        _roots.Add(root); return root;
    }
    internal bool ReserveImpact(ProtocolRootContext root)
    {
        if (!CanAdvance || !_roots.Contains(root) || FrameImpacts >= _limits.ImpactsPerFrame)
        { if (DroppedImpacts < int.MaxValue) DroppedImpacts++; return false; }
        FrameImpacts++; return true;
    }
    internal bool HasImpactCapacity(ProtocolRootContext root)
        => CanAdvance && _roots.Contains(root) && FrameImpacts < _limits.ImpactsPerFrame;
    public bool ReserveQuery()
    { if (!CanAdvance || FrameQueries >= _limits.QueriesPerFrame) { if (DroppedQueries < int.MaxValue) DroppedQueries++; return false; } FrameQueries++; return true; }
    public bool ReserveTick()
    { if (!CanAdvance || FrameTicks >= _limits.BurnTicksPerFrame) { if (DroppedTicks < int.MaxValue) DroppedTicks++; return false; } FrameTicks++; return true; }
    internal void Release(ProtocolRootContext root) => _roots.Remove(root);
    public void CloseAll()
    {
        foreach (var root in _roots) root.Invalidate();
        _roots.Clear();
    }
}

/// <summary>An execution snapshot retained by its reserved children, including across frames.</summary>
public sealed class ProtocolRootContext
{
    internal RunDamageSource AnalyticsSource { get; set; }
    private readonly ProtocolBudget _budget;
    private readonly ProtocolLimits _limits;
    private readonly int[] _ranks = new int[7];
    private readonly HashSet<long> _children = new HashSet<long>();
    private readonly HashSet<long> _usedChildren = new HashSet<long>();
    private readonly HashSet<(ulong, long)> _propagationTargets = new HashSet<(ulong, long)>();
    private readonly HashSet<(ulong, long)> _deaths = new HashSet<(ulong, long)>();
    private bool _sealed;
    private bool _invalid;
    private int _queued;
    private int _openBatches;
    private int _totalReservations;
    private readonly float _createdAt;
    public long RootId { get; }
    public Guid SessionId { get; }
    public int EncounterId { get; }
    public ProtocolProducer Producer { get; }
    public long ExecutionId { get; }
    public int ImpactAttempts { get; private set; }
    public int QueueCount => _queued;
    public int Reservations => _children.Count;
    public bool HasConfirmedBatch => _openBatches > 0;
    public bool IsExpired => _budget.ScaledTime - _createdAt >= _limits.RootLifetime;
    public bool IsValid => !_invalid && !IsExpired;
    public bool IsClosed => _invalid;
    internal ProtocolRootContext(ProtocolBudget budget, ProtocolLimits limits, long id, Guid session, int encounter,
        ProtocolInventory inventory, float createdAt, ProtocolProducer producer, long executionId, ProtocolRanks capturedRanks)
    {
        _budget = budget; _limits = limits; RootId = id; SessionId = session; EncounterId = encounter; _createdAt = createdAt;
        Producer = producer; ExecutionId = executionId;
        for (int i = 0; i < _ranks.Length; i++) _ranks[i] = capturedRanks == null ? inventory.Rank((ProtocolKind)i) : capturedRanks.Rank((ProtocolKind)i);
    }
    private readonly HashSet<ProtocolTargetHandle> _basicTerminalTargets = new HashSet<ProtocolTargetHandle>();
    private bool _basicChildReserved;
    private bool _titanicTerminalCommitted;
    internal float TitanicTerminalDamage { get; private set; }
    internal bool RecordTitanicTerminalContact(GauntletImpactContext context, int impactIndex, float damage)
    {
        if (!IsValid || _titanicTerminalCommitted || context == null || impactIndex < 0 ||
            context.Execution != ExecutionId || context.RootExecution != ExecutionId ||
            context.Slot != GauntletActionSlot.Basic || context.Origin != GauntletImpactOrigin.Direct ||
            context.Snapshot?.BasicMutationId != TitanicFistsDefinition.Id || !context.Snapshot.BasicFinisher ||
            !ReactionNumeric.IsFinite(damage) || damage < 0f) return false;
        _titanicTerminalCommitted = true; TitanicTerminalDamage = damage; return true;
    }
    internal ProtocolBasicLineage ReserveTitanicTerminalEmission(long producerId)
    {
        if (!_titanicTerminalCommitted || _basicChildReserved || !ReserveChild(producerId)) return null;
        _basicChildReserved = true;
        return new ProtocolBasicLineage(ExecutionId, RootId, _basicTerminalTargets, TitanicFistsDefinition.Id, ProtocolBasicEffects.None);
    }
    internal float ConfirmedBasicSourceDamage { get; private set; }
    private string _basicProducerId = "basic-derived";
    internal void ConfirmBasicTerminal(ConfirmedHpReceipt receipt)
    {
        var c = receipt.Context;
        if (IsValid && c != null && c.RootExecution == ExecutionId && c.Execution == ExecutionId &&
            c.Slot == GauntletActionSlot.Basic && c.Origin == GauntletImpactOrigin.Direct &&
            c.Snapshot != null && c.Snapshot.BasicFinisher && receipt.Damage > 0)
        {
            _basicProducerId = c.Snapshot.BasicMutationId ?? "basic-derived";
            ConfirmedBasicSourceDamage = UnityEngine.Mathf.Max(ConfirmedBasicSourceDamage, receipt.SourceDamage);
            _basicTerminalTargets.Add(new ProtocolTargetHandle(c.TargetId, c.TargetGeneration));
        }
    }
    internal ProtocolBasicLineage ReserveBasicTerminalChild(long producerId, ProtocolBasicEffects effects)
    {
        if (_basicChildReserved || _basicTerminalTargets.Count == 0 || !ReserveChild(producerId)) return null;
        _basicChildReserved = true;
        return new ProtocolBasicLineage(ExecutionId, RootId, _basicTerminalTargets, _basicProducerId, effects);
    }
    public int Rank(ProtocolKind kind) => (int)kind >= 0 && (int)kind < _ranks.Length ? _ranks[(int)kind] : 0;
    public bool ReserveImpact(int depth)
    {
        if (!IsValid || depth < 1 || depth > _limits.MaxDepth || ImpactAttempts >= _limits.ImpactsPerRoot) { _budget.DropImpact(); return false; }
        if (!_budget.ReserveImpact(this)) return false;
        ImpactAttempts++; return true;
    }
    internal bool HasImpactCapacity(int depth)
        => IsValid && depth >= 1 && depth <= _limits.MaxDepth && ImpactAttempts < _limits.ImpactsPerRoot && _budget.HasImpactCapacity(this);
    public bool ReserveChild(long producerId)
        => IsValid && !_sealed && AddReservation(producerId);
    public bool ReserveDescendant(long parentProducerId, long producerId)
        => HasChild(parentProducerId) && AddReservation(producerId);
    private bool AddReservation(long producerId)
    {
        // Positive IDs belong to Gauntlet executions; negative IDs to legacy/local producers.
        if (producerId == 0 || _totalReservations >= _limits.ReservationsPerRoot || !_usedChildren.Add(producerId)) { _budget.DropReservation(); return false; }
        _children.Add(producerId);
        _totalReservations++; return true;
    }
    public void ReleaseChild(long producerId) { _children.Remove(producerId); TryClose(); }
    public bool HasChild(long producerId) => IsValid && _children.Contains(producerId);
    public bool ReserveQueueItem()
    { if (!IsValid || _queued >= _limits.QueuePerRoot) { _budget.DropQueue(); return false; } _queued++; _budget.RecordQueue(_queued); return true; }
    public void ReleaseQueueItem() { if (_queued > 0) _queued--; TryClose(); }
    public bool BeginBatch()
    { if (!IsValid) return false; _openBatches++; return true; }
    public void EndBatch() { if (_openBatches > 0) _openBatches--; TryClose(); }
    public bool ClaimPropagation(ulong targetId, long generation)
        => IsValid && targetId != 0 && generation >= 0 && _propagationTargets.Count < _limits.ImpactsPerRoot && _propagationTargets.Add((targetId, generation));
    public bool ClaimDeath(ulong targetId, long generation)
        => IsValid && targetId != 0 && generation >= 0 && _deaths.Count < _limits.ImpactsPerRoot + _limits.QueuePerRoot && _deaths.Add((targetId, generation));
    public void Seal() { _sealed = true; TryClose(); }
    public void CancelFuture() { _sealed = true; _children.Clear(); TryClose(); }
    internal void Invalidate() { _invalid = true; _children.Clear(); _queued = 0; }
    private void TryClose()
    {
        if (_sealed && _children.Count == 0 && _queued == 0 && _openBatches == 0)
        { _invalid = true; _budget.Release(this); }
    }
}
