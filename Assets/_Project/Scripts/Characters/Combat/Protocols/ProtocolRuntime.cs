using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Local adapter at the player. Identity and limits survive nested damage notifications.</summary>
[DisallowMultipleComponent]
public sealed partial class ProtocolRuntime : MonoBehaviour
{
    private ProtocolInventory _inventory;
    private PlayerActor _player;
    private RunBoons _boons;
    private AbilityHolder _holder;
    private readonly Dictionary<(long, long), ProtocolRootContext> _nativeRoots = new Dictionary<(long, long), ProtocolRootContext>();
    private readonly List<(long, long)> _closedNativeRoots = new List<(long, long)>(64);
    private bool _ended;
    internal long LifecycleRevision { get; private set; }
    private ProtocolFeedbackPool _feedback;
    public ProtocolFeedbackPool Feedback => _feedback;
    public ProtocolBudget Budget { get; private set; }
    public ProtocolInventory Inventory => _inventory;
    private bool _processingBatch;
    public bool HasConfirmedBatch => _processingBatch || Budget != null && Budget.HasConfirmedBatch;
    public float ScaledDelta { get { TouchFrame(); return Budget != null && Budget.CanAdvance ? Time.deltaTime : 0f; } }
    public int EncounterId => _boons && _boons.ProtocolSession != null ? _boons.ProtocolSession.CurrentNode : 0;
    public bool IsOpen => !_ended && _inventory != null && !_inventory.IsEnded &&
        (!_boons || _boons.ProtocolSession == null || _boons.ProtocolSession.Phase == RunPhase.Encounter);
    public void Configure(PlayerActor player, ProtocolInventory inventory, RunBoons boons = null)
    {
        if (!player || inventory == null) throw new ArgumentException("Protocol runtime requires player and inventory.");
        if (_inventory != null) EndOwnedState();
        _feedback?.Dispose();
        LifecycleRevision++;
        _player = player; _holder = player.GetComponent<AbilityHolder>(); _boons = boons; _inventory = inventory;
        Budget = new ProtocolBudget(inventory.Catalog.Limits); _ended = false; enabled = true; TouchFrame();
        InitializeDamageBuffers();
        _feedback = new ProtocolFeedbackPool(transform, inventory.Catalog.Limits);
    }
    private void TouchFrame()
    {
        if (Budget == null) return;
        float delta = _player && (_player.IsCombatHitStopped || _holder && _holder.IsGamePaused) ? 0f : Time.deltaTime;
        Budget.AdvanceFrame(Time.frameCount, delta);
        _closedNativeRoots.Clear();
        foreach (var pair in _nativeRoots) if (pair.Value.IsClosed) _closedNativeRoots.Add(pair.Key);
        foreach (var key in _closedNativeRoots) _nativeRoots.Remove(key);
        _closedNativeRoots.Clear();
    }
    public void BeginProducer(GauntletImpactContext context)
    {
        using (Profile())
        {
        if (!IsOpen || context == null || context.Owner != _player || _boons && _boons.ProtocolSession?.IsEncounterClosing == true) return;
        TouchFrame(); var key = (context.Epoch, context.RootExecution);
        if (context.Execution == context.RootExecution)
        {
            if (!_nativeRoots.ContainsKey(key))
            {
                var root = Budget.BeginRoot(_inventory.SessionId, EncounterId, _inventory, ProtocolProducer.Direct, context.RootExecution);
                if (root != null) _nativeRoots.Add(key, root);
            }
        }
        else if (_nativeRoots.TryGetValue(key, out var parent)) parent.ReserveChild(context.Execution);
        }
    }
    public void CloseProducer(GauntletImpactContext context)
    {
        using (Profile())
        {
        if (context == null || !_nativeRoots.TryGetValue((context.Epoch, context.RootExecution), out var root)) return;
        if (context.Execution == context.RootExecution) root.Seal(); else root.ReleaseChild(context.Execution);
        if (root.IsClosed) _nativeRoots.Remove((context.Epoch, context.RootExecution));
        }
    }
    public ProtocolRootContext FindRoot(GauntletImpactContext context)
    {
        using (Profile())
        {
        TouchFrame();
        if (!IsOpen || context == null || !_nativeRoots.TryGetValue((context.Epoch, context.RootExecution), out var root)) return null;
        if (context.Execution != context.RootExecution && !root.HasChild(context.Execution)) return null;
        return root.IsValid ? root : null;
        }
    }
    public ProtocolRootContext BeginStandalone(ProtocolProducer producer = ProtocolProducer.Direct)
    {
        using (Profile())
        {
        if (!IsOpen || _boons && _boons.ProtocolSession?.IsEncounterClosing == true) return null;
        TouchFrame(); return Budget.BeginRoot(_inventory.SessionId, EncounterId, _inventory, producer);
        }
    }
    public static ProtocolProducer ProducerFor(GauntletImpactContext context)
    {
        if (context == null || context.Origin == GauntletImpactOrigin.Direct) return ProtocolProducer.Direct;
        switch (context.Origin)
        {
            case GauntletImpactOrigin.Technique: return ProtocolProducer.Technique;
            case GauntletImpactOrigin.Echo: return ProtocolProducer.Echo;
            case GauntletImpactOrigin.Persistent: return ProtocolProducer.Persistent;
            default: return ProtocolProducer.Environment;
        }
    }
    public void EndOwnedState()
    { if (!_ended) LifecycleRevision++; _ended = true; Budget?.CloseAll(); _nativeRoots.Clear(); ClearApplications(); _feedback?.Clear(); }
    internal void CancelNativeProducers()
    {
        foreach (var root in _nativeRoots.Values) root.CancelFuture();
        _nativeRoots.Clear();
    }
    private void OnDisable() => EndOwnedState();
    private void OnDestroy() { EndOwnedState(); _feedback?.Dispose(); _feedback = null; }
    private void Update()
    {
        using (Profile())
        {
        if (!_ended && _inventory != null && _inventory.IsEnded) EndOwnedState();
        TouchFrame(); UpdateApplications();
        if (!_ended) _feedback?.Tick(ScaledDelta);
        }
    }
    partial void ClearApplications();
    partial void UpdateApplications();
    partial void InitializeDamageBuffers();
}
