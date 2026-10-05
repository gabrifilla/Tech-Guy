using System;
using System.Collections.Generic;
using UnityEngine;

public enum GauntletGainBlock { None, NoConfirmation, InvalidReceipt, StaleContext, IneligibleOrigin, Duplicate, AsuraActive }

/// <summary>All parcels of one confirmed impact, committed before any notification.</summary>
public readonly struct GauntletEnergyGain
{
    public int Before { get; }
    public int After { get; }
    public int Basic { get; }
    public int Skill { get; }
    public int Alternation { get; }
    public int StanceBreak { get; }
    public int Technique { get; }
    public int Reserve { get; }
    public int Requested => Basic + Skill + Alternation + StanceBreak + Technique + Reserve;
    public int Effective => After - Before;
    public GauntletGainBlock Block { get; }
    public GauntletImpactContext Context { get; }
    internal GauntletEnergyGain(GauntletImpactContext context, int before, int after, int basic = 0,
        int skill = 0, int alternation = 0, int stanceBreak = 0, int technique = 0, int reserve = 0,
        GauntletGainBlock block = GauntletGainBlock.None)
    {
        Context = context; Before = before; After = after; Basic = basic; Skill = skill;
        Alternation = alternation; StanceBreak = stanceBreak; Technique = technique; Reserve = reserve; Block = block;
    }
}

/// <summary>Pure policy over one existing energy owner and a local live-context registry.</summary>
public sealed class GauntletEnergyPolicy
{
    private sealed class RootState
    {
        public bool BasicPaid;
        public bool SkillPaid;
        public bool RConsumed;
        public int PaidBreaks;
        public readonly HashSet<(ulong Target, long Generation, long Break)> Breaks = new HashSet<(ulong, long, long)>();
    }
    private readonly AsuraMomentum _energy;
    private readonly GauntletContextRegistry _contexts;
    private readonly GauntletEnergyRates _rates;
    private readonly Dictionary<long, RootState> _roots = new Dictionary<long, RootState>();
    private readonly List<long> _closedRoots = new List<long>();
    private long _asuraExecution;
    public bool IsAsuraActive => _asuraExecution != 0;
    public int Energy => _energy.Energy;
    public bool IsReady => _energy.IsReady;
    public int RetainedRootCount { get { PruneClosedRoots(); return _roots.Count; } }
    public event Action<GauntletEnergyGain> Committed;

    public GauntletEnergyPolicy(AsuraMomentum energy, GauntletContextRegistry contexts, GauntletEnergyAuthoring authoring)
    {
        _energy = energy ?? throw new ArgumentNullException(nameof(energy));
        _contexts = contexts ?? throw new ArgumentNullException(nameof(contexts));
        if (authoring == null || !authoring.TrySnapshot(out _rates, out var error))
            throw new ArgumentException("Invalid energy authoring.", nameof(authoring));
    }

    public GauntletEnergyGain Confirm(ConfirmedHpReceipt? hp, ConfirmedBreakReceipt? stanceBreak)
    {
        var context = hp?.Context ?? stanceBreak?.Context;
        if (hp.HasValue && stanceBreak.HasValue && !SameImpact(hp.Value.Context, stanceBreak.Value.Context))
            return Block(context, GauntletGainBlock.InvalidReceipt);
        return ConfirmImpact(context, hp?.Damage ?? 0f, stanceBreak?.BreakId ?? 0);
    }

    public GauntletEnergyGain ConfirmImpact(GauntletImpactContext context, float hpDamage, long breakId = 0, Action<Action> defer = null)
    {
        PruneClosedRoots();
        if (float.IsNaN(hpDamage) || float.IsInfinity(hpDamage) || hpDamage < 0f || breakId < 0
            || context == null || context.ImpactIndex < 0 || context.TargetId == 0)
            return Block(context, GauntletGainBlock.InvalidReceipt);
        if (!_contexts.IsValid(context)) return Block(context, GauntletGainBlock.StaleContext);
        if (!context.EnergyEligible || context.Family != RunWeaponFamily.Gauntlet)
            return Block(context, GauntletGainBlock.IneligibleOrigin);
        if (hpDamage == 0f && breakId == 0) return Block(context, GauntletGainBlock.NoConfirmation);

        var state = State(context.RootExecution);
        bool direct = context.Origin == GauntletImpactOrigin.Direct;
        bool basic = direct && context.Slot == GauntletActionSlot.Basic && hpDamage > 0f && !state.BasicPaid;
        bool skill = direct && context.Slot >= GauntletActionSlot.Q && context.Slot <= GauntletActionSlot.E
            && (hpDamage > 0f || breakId > 0) && !state.SkillPaid;
        bool newBreak = breakId > 0 && state.Breaks.Add((context.TargetId, context.TargetGeneration, breakId));
        if (basic) state.BasicPaid = true;
        if (skill) state.SkillPaid = true;

        // Consume blocked confirmations too: ending R cannot replay an earlier confirmation.
        if (IsAsuraActive) return Block(context, GauntletGainBlock.AsuraActive);

        int breakGain = 0;
        if (newBreak && state.PaidBreaks < _rates.BreaksPerRoot)
        {
            state.PaidBreaks++;
            breakGain = _rates.StanceBreak;
        }
        int basicGain = basic ? _rates.Basic : 0;
        int skillGain = skill ? _rates.Skill : 0;
        int alternation = skill && _energy.IsAlternation(context.Shock) ? _rates.Alternation : 0;
        int technique = basic ? _rates.AsuraFistPerRank * context.AsuraFistRank
            : skill ? _rates.MomentumPerRank * context.MomentumRank : 0;
        if (skill) _energy.ConfirmCategory(context.Shock);
        int before = _energy.Energy;
        _energy.AddEnergy(basicGain + skillGain + alternation + breakGain + technique);
        var result = new GauntletEnergyGain(context, before, _energy.Energy, basicGain, skillGain,
            alternation, breakGain, technique, block: !basic && !skill && !newBreak ? GauntletGainBlock.Duplicate : GauntletGainBlock.None);
        if (result.Requested > 0)
        {
            if (defer != null) defer(() => Publish(result)); else Publish(result);
        }
        return result;
    }

    public bool TryStartAsura(GauntletImpactContext context, out GauntletEnergyGain reserve, Action<Action> defer = null)
    {
        reserve = Block(context, GauntletGainBlock.NoConfirmation);
        if (!_contexts.IsValid(context) || context.Slot != GauntletActionSlot.R
            || context.Origin != GauntletImpactOrigin.Direct || context.Family != RunWeaponFamily.Gauntlet
            || IsAsuraActive || !_energy.IsReady) return false;
        var state = State(context.RootExecution);
        if (state.RConsumed) return false;
        state.RConsumed = true;
        _energy.TryConsume();
        _asuraExecution = context.Execution;
        int amount = _rates.ReservePerRank * context.ReserveRank;
        _energy.AddEnergy(amount);
        reserve = new GauntletEnergyGain(context, 0, _energy.Energy, reserve: amount);
        if (amount > 0)
        {
            var committed = reserve;
            if (defer != null) defer(() => Publish(committed)); else Publish(committed);
        }
        return true;
    }

    public bool EndAsura(GauntletImpactContext context)
    {
        if (context == null || context.Epoch != _contexts.Epoch || context.Execution != _asuraExecution) return false;
        _asuraExecution = 0;
        return true;
    }

    public bool Close(GauntletImpactContext context)
    {
        EndAsura(context);
        bool closed = _contexts.Close(context);
        PruneClosedRoots();
        return closed;
    }

    public bool AddExternal(int amount)
    {
        if (amount < 0 || IsAsuraActive) return false;
        _energy.AddEnergy(amount);
        return true;
    }

    internal bool IsLive(GauntletImpactContext context) => _contexts.IsValid(context);
    internal bool ConfirmDirectHpOnce(GauntletImpactContext context) => _contexts.ConfirmDirectHpOnce(context);
    internal bool ClaimBasicTechnique(GauntletImpactContext context, WeaponBoon technique)
        => _contexts.ClaimBasicTechnique(context, technique);

    public void Reset()
    {
        _asuraExecution = 0; _energy.Reset(); _contexts.Invalidate(); _roots.Clear();
    }

    private RootState State(long root)
    {
        if (!_roots.TryGetValue(root, out var state)) { state = new RootState(); _roots.Add(root, state); }
        return state;
    }

    private void PruneClosedRoots()
    {
        _closedRoots.Clear();
        foreach (var pair in _roots) if (!_contexts.ContainsRoot(pair.Key)) _closedRoots.Add(pair.Key);
        foreach (long root in _closedRoots) _roots.Remove(root);
    }

    private GauntletEnergyGain Block(GauntletImpactContext context, GauntletGainBlock reason) =>
        new GauntletEnergyGain(context, _energy.Energy, _energy.Energy, block: reason);

    private static bool SameImpact(GauntletImpactContext left, GauntletImpactContext right) =>
        left != null && right != null && left.Owner == right.Owner && left.Epoch == right.Epoch
        && left.Execution == right.Execution && left.ImpactIndex == right.ImpactIndex
        && left.TargetId == right.TargetId && left.TargetGeneration == right.TargetGeneration;

    private void Publish(GauntletEnergyGain gain)
    {
        if (Committed == null) return;
        foreach (Delegate subscriber in Committed.GetInvocationList())
            try { ((Action<GauntletEnergyGain>)subscriber)(gain); }
            catch (Exception exception) { Debug.LogException(exception); }
    }
}

