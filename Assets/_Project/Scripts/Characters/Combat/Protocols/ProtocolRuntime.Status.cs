using System.Collections.Generic;
using UnityEngine;

public sealed partial class ProtocolRuntime
{
    private readonly List<ProtocolElementApplication> _applications = new List<ProtocolElementApplication>();
    private long _nextApplication;
    private int _burnCount;
    private int _chillCount;
    private int _tickCursor;
    private System.Func<float> _chillRoll;
    public int BurnApplications => _burnCount;
    public int ChillApplications => _chillCount;
    internal void SetChillRollForTests(System.Func<float> roll) => _chillRoll = roll;
    private ProtocolElementApplication FindApplication(Actor target)
    {
        foreach (var value in _applications) if (value.Target == target && value.Handle.Generation == target.CombatGeneration) return value;
        return null;
    }
    private ProtocolElementApplication CreateApplication(Actor target)
    {
        if (_applications.Count >= _inventory.Catalog.Limits.ApplicationsPerType * 2) { Budget.DropApplication(); return null; }
        var view = target.GetComponent<ProtocolElementStatus>() ?? target.gameObject.AddComponent<ProtocolElementStatus>();
        var value = new ProtocolElementApplication { Owner = this, Target = target, View = view,
            Handle = new ProtocolTargetHandle(EntityId.ToULong(target.GetEntityId()), target.CombatGeneration),
            SessionId = _inventory.SessionId, EncounterId = EncounterId, ApplicationId = checked(++_nextApplication) };
        value.Registered = true; value.LastUpdatedFrame = Time.frameCount; _applications.Add(value); view.Add(value); return value;
    }
    private void ApplyElements(ProtocolRootContext root, Actor target, float damage, out bool fire, out bool frost)
    {
        fire = false; frost = false;
        if (!Matches(root) || !target || target.IsDead || !ReactionNumeric.IsFinite(damage) || damage <= 0) return;
        long generation = target.CombatGeneration;
        var limits = _inventory.Catalog.Limits;
        var value = FindApplication(target);
        int ignite = root.Rank(ProtocolKind.Ignite);
        if (ignite > 0 && (value != null && value.BurnRemaining > 0 || _burnCount < limits.ApplicationsPerType))
        {
            value = value ?? CreateApplication(target);
            if (value != null)
            {
                if (value.BurnRemaining <= 0) { _burnCount++; value.TickRemaining = _inventory.Catalog.Get(ProtocolKind.Ignite).TickInterval; }
                float dps = _inventory.Catalog.Get(ProtocolKind.Ignite).DamagePerRank * ignite +
                    _inventory.Catalog.Get(ProtocolKind.Reactor).DamagePerRank * root.Rank(ProtocolKind.Reactor) * damage;
                if (!ReactionNumeric.IsFinite(dps)) dps = _inventory.Catalog.Get(ProtocolKind.Ignite).DamagePerRank * ignite;
                value.AnalyticsBurnSource = value.BurnRemaining > 0
                    ? RunDamageSource.MergeBurn(value.AnalyticsBurnSource, root.AnalyticsSource) : root.AnalyticsSource;
                value.BurnDps = Mathf.Max(value.BurnDps, dps); value.BurnRemaining = Mathf.Max(value.BurnRemaining, _inventory.Catalog.Get(ProtocolKind.Ignite).Duration);
                value.BurnRanks = new ProtocolRanks(root); value.Revision++; fire = true; _boons?.Hooks?.RaiseBurn(target);
            }
        }
        else if (ignite > 0) Budget.DropApplication();
        // Element hooks may kill, disable/recycle the target, or end/rebind this runtime.
        if (!Matches(root) || !target || target.IsDead || target.CombatGeneration != generation || value != null && !value.Registered) return;
        int frostRank = root.Rank(ProtocolKind.Frost);
        var definition = _inventory.Catalog.Get(ProtocolKind.Frost);
        float roll = frostRank > 0 ? _chillRoll?.Invoke() ?? Random.value : 0;
        float chance = Mathf.Min(1f, definition.ChancePerRank * frostRank);
        if (frostRank > 0 && ReactionNumeric.InRange(roll, 0, 1) && (chance >= 1 || roll < chance) &&
            (value != null && value.ChillRemaining > 0 || _chillCount < limits.ApplicationsPerType))
        {
            value = value ?? CreateApplication(target);
            if (value != null)
            {
                if (value.ChillRemaining <= 0) _chillCount++;
                float slow = definition.SlowFraction;
                if (target.TryGetComponent(out CombatReactionController reaction))
                {
                    if (reaction.Rank == EnemyRank.Boss || reaction.Category == ReactionCategory.Boss) slow = Mathf.Min(slow, limits.BossMaxSlow);
                    else if (reaction.Rank != EnemyRank.Normal || reaction.Category == ReactionCategory.Elite) slow = Mathf.Min(slow, limits.EliteMaxSlow);
                    else if (reaction.Category == ReactionCategory.MediumMob) slow = Mathf.Min(slow, limits.HeavyMaxSlow);
                }
                value.SlowFraction = Mathf.Max(value.SlowFraction, slow);
                value.ChillRemaining = Mathf.Max(value.ChillRemaining, definition.Duration); value.Revision++; frost = true;
                value.View.RefreshMovement(); if (value.View.IsFrozen) _boons?.Hooks?.RaiseFreeze(target);
            }
        }
        else if (frostRank > 0 && _chillCount >= limits.ApplicationsPerType) Budget.DropApplication();
    }
    internal void RemoveApplication(ProtocolElementApplication value)
    {
        if (!_applications.Remove(value)) return;
        value.Registered = false;
        if (value.BurnRemaining > 0) _burnCount--;
        if (value.ChillRemaining > 0) _chillCount--;
        value.BurnRemaining = 0; value.ChillRemaining = 0;
        if (value.View) value.View.Remove(value);
    }
    partial void ClearApplications()
    {
        while (_applications.Count > 0) RemoveApplication(_applications[_applications.Count - 1]);
        _burnCount = 0; _chillCount = 0; _tickCursor = 0;
    }
    partial void UpdateApplications()
    {
        if (!IsOpen || Budget == null || !Budget.CanAdvance || _applications.Count == 0 ||
            _boons && _boons.ProtocolSession?.IsEncounterClosing == true ||
            _player && _player.IsCombatHitStopped || _holder && _holder.IsGamePaused) return;
        int remaining = _applications.Count; int index = _tickCursor % _applications.Count;
        ProtocolElementApplication lastServiced = null;
        while (remaining-- > 0 && _applications.Count > 0)
        {
            index %= _applications.Count; var value = _applications[index];
            if (value.LastUpdatedFrame == Time.frameCount) { index++; continue; }
            value.LastUpdatedFrame = Time.frameCount;
            if (!value.Valid) { RemoveApplication(value); continue; }
            float delta = Time.deltaTime;
            if (value.ChillRemaining > 0)
            {
                value.ChillRemaining = Mathf.Max(0, value.ChillRemaining - delta);
                if (value.ChillRemaining == 0) { _chillCount--; value.SlowFraction = 0; value.View.RefreshMovement(); }
            }
            if (value.BurnRemaining > 0)
            {
                bool tickInsideLifetime = value.TickRemaining <= value.BurnRemaining;
                value.TickRemaining -= delta;
                if (value.TickRemaining <= 0 && tickInsideLifetime)
                {
                    value.TickRemaining = _inventory.Catalog.Get(ProtocolKind.Ignite).TickInterval; value.TickOrdinal++;
                    if (Budget.ReserveTick()) { lastServiced = value; ResolveBurnTick(value); }
                    if (!value.Registered) continue;
                }
                value.BurnRemaining = Mathf.Max(0, value.BurnRemaining - delta);
                if (value.BurnRemaining == 0) { _burnCount--; value.BurnDps = 0; }
            }
            if (value.Registered && value.BurnRemaining <= 0 && value.ChillRemaining <= 0) { RemoveApplication(value); continue; }
            if (value.Registered) index++;
        }
        int servicedIndex = lastServiced == null ? -1 : _applications.IndexOf(lastServiced);
        _tickCursor = _applications.Count == 0 ? 0 :
            (servicedIndex >= 0 ? servicedIndex + 1 : _tickCursor) % _applications.Count;
    }
    partial void ResolveBurnTick(ProtocolElementApplication value);
    public void CloseEncounter()
    {
        LifecycleRevision++;
        Budget?.CloseAll(); _nativeRoots.Clear(); ClearApplications(); _feedback?.Clear();
    }
}
