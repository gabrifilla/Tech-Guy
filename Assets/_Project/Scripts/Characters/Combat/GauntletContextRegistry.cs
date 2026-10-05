using System;
using System.Collections.Generic;

/// <summary>Per-owner live contexts. Closing cannot make a forgotten callback eligible again.</summary>
public sealed class GauntletContextRegistry
{
    private sealed class Entry
    {
        public GauntletImpactContext Context;
        public bool Producing = true;
        public int LiveMembers;
        public readonly HashSet<(int, ulong, long)> ConfirmedHp = new HashSet<(int, ulong, long)>();
        public readonly HashSet<WeaponBoon> BasicTechniques = new HashSet<WeaponBoon>();
        public readonly HashSet<int> Producers = new HashSet<int>();
    }
    private readonly Actor _owner;
    private readonly Dictionary<long, Entry> _entries = new Dictionary<long, Entry>();
    private long _nextExecution;
    public long Epoch { get; private set; } = 1;
    public int Count => _entries.Count;

    public GauntletContextRegistry(Actor owner)
    {
        if (!owner) throw new ArgumentNullException(nameof(owner));
        _owner = owner;
    }

    public GauntletImpactContext Begin(GauntletActionSlot slot, bool shock = false,
        int momentumRank = 0, int asuraFistRank = 0, int reserveRank = 0,
        RunWeaponFamily family = RunWeaponFamily.Gauntlet, GauntletExecutionSnapshot snapshot = null)
    {
        if (!_owner) throw new InvalidOperationException("The context owner no longer exists.");
        if (!Enum.IsDefined(typeof(GauntletActionSlot), slot) || !Enum.IsDefined(typeof(RunWeaponFamily), family))
            throw new ArgumentOutOfRangeException(nameof(slot));
        ValidateRank(momentumRank); ValidateRank(asuraFistRank); ValidateRank(reserveRank);
        long id = checked(++_nextExecution);
        var context = new GauntletImpactContext(_owner, family, Epoch, id, id, slot, shock,
            GauntletImpactOrigin.Direct, null, family == RunWeaponFamily.Gauntlet && slot != GauntletActionSlot.R,
            momentumRank, asuraFistRank, reserveRank, snapshot: snapshot);
        _entries.Add(id, new Entry { Context = context, LiveMembers = 1 });
        if (_owner.TryGetComponent(out ProtocolRuntime protocols)) protocols.BeginProducer(context);
        return context;
    }

    public GauntletImpactContext BeginChild(GauntletImpactContext parent, GauntletImpactOrigin origin, string producerId)
    {
        if (!IsValid(parent)) throw new InvalidOperationException("A live parent is required.");
        if (!Enum.IsDefined(typeof(GauntletImpactOrigin), origin) || origin == GauntletImpactOrigin.Direct
            || string.IsNullOrWhiteSpace(producerId)) throw new ArgumentException("A descendant must declare its producer.");
        bool localProducer = producerId == WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho)
            || producerId == WeaponRunModifiers.CatalogId(WeaponBoon.AsuraEcho)
            || producerId == GauntletContentAuthoring.ForMutation(GauntletMutation.PressureField).Id;
        if (localProducer && parent.Origin != GauntletImpactOrigin.Direct)
            throw new InvalidOperationException("A descendant cannot reproduce a local echo or zone producer.");
        bool guard = origin == GauntletImpactOrigin.Technique
            && producerId == WeaponRunModifiers.CatalogId(WeaponBoon.GuardBreaker)
            && parent.Origin == GauntletImpactOrigin.Direct && parent.Slot == GauntletActionSlot.Basic;
        long id = checked(++_nextExecution);
        var child = new GauntletImpactContext(_owner, parent.Family, Epoch, id, parent.RootExecution,
            parent.Slot, parent.Shock, origin, producerId, guard && parent.EnergyEligible,
            parent.MomentumRank, parent.AsuraFistRank, parent.ReserveRank, snapshot: parent.Snapshot);
        _entries.Add(id, new Entry { Context = child });
        _entries[parent.RootExecution].LiveMembers++;
        if (_owner.TryGetComponent(out ProtocolRuntime protocols)) protocols.BeginProducer(child);
        return child;
    }

    public bool IsValid(GauntletImpactContext context)
    {
        return _owner && context != null && context.Owner == _owner && context.Epoch == Epoch
            && _entries.TryGetValue(context.Execution, out var entry) && entry.Producing
            && _entries.ContainsKey(context.RootExecution);
    }

    public bool ContainsRoot(long root) => _entries.ContainsKey(root);
    internal bool ClaimProducer(GauntletImpactContext context, int index)
        => IsValid(context) && context.Snapshot != null &&
            context.Snapshot.CanProduceLocalChild(context.Origin, index) &&
            _entries[context.RootExecution].Producers.Add(index);
    internal bool ClaimBasicTechnique(GauntletImpactContext context, WeaponBoon technique)
        => IsValid(context) && context.Origin == GauntletImpactOrigin.Direct &&
            context.Slot == GauntletActionSlot.Basic &&
            _entries[context.RootExecution].BasicTechniques.Add(technique);

    public bool ConfirmDirectHpOnce(GauntletImpactContext context)
        => IsValid(context) && context.Origin == GauntletImpactOrigin.Direct && context.ImpactIndex >= 0 &&
            context.TargetId != 0 && _entries[context.Execution].ConfirmedHp.Add(
                (context.ImpactIndex, context.TargetId, context.TargetGeneration));

    public bool Close(GauntletImpactContext context)
    {
        if (!IsValid(context)) return false;
        var entry = _entries[context.Execution];
        entry.Producing = false;
        var root = _entries[context.RootExecution];
        root.LiveMembers--;
        if (_owner.TryGetComponent(out ProtocolRuntime protocols)) protocols.CloseProducer(context);
        if (context.Execution != context.RootExecution) _entries.Remove(context.Execution);
        if (root.LiveMembers == 0) _entries.Remove(context.RootExecution);
        return true;
    }

    public void Invalidate()
    {
        Epoch = checked(Epoch + 1);
        _entries.Clear();
        if (_owner && _owner.TryGetComponent(out ProtocolRuntime protocols)) protocols.CancelNativeProducers();
    }

    private static void ValidateRank(int rank)
    {
        if (rank < 0 || rank > 3) throw new ArgumentOutOfRangeException(nameof(rank));
    }
}
