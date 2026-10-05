using System;
using UnityEngine;

/// <summary>Counts confirmed basic executions. Contextual posture resolves before public hit notifications.</summary>
public sealed class ImpactGuardTracker : MonoBehaviour
{
    private const float BaseStanceOnThird = 24f;
    private readonly ConsecutiveHitCounter _counter = new ConsecutiveHitCounter(3);
    private PlayerActor _player;
    private HookBus _hooks;
    private AbilityHolder _holder;
    private BreakerGauntletCombat _combat;
    private int _rank;
    public int Streak => _counter.Count;
    public event Action<int> StreakChanged;

    public void Configure(PlayerActor player, HookBus hooks, AbilityHolder holder, int rank)
    {
        bool preserve = _player == player && _hooks == hooks && _rank > 0 && rank > 0;
        Unsubscribe();
        _player = player; _hooks = hooks; _holder = holder; _rank = Mathf.Clamp(rank, 0, 3);
        _combat = player ? player.GetComponent<BreakerGauntletCombat>() : null;
        if (enabled) Subscribe(); // Explicit pre-Awake configuration also serves the legacy bus facade.
        if (!preserve) ResetStreak();
    }

    internal void ConfirmBasic(GauntletImpactContext context, Actor victim, GauntletImpactTransaction transaction)
    {
        int rank = context?.Snapshot?.Rank(WeaponBoon.GuardBreaker) ?? 0;
        if (rank <= 0 || !_combat || !_combat.EnergyPolicy.ClaimBasicTechnique(context, WeaponBoon.GuardBreaker)) return;
        bool breaks = _counter.RegisterBasicHit();
        int streak = _counter.Count;
        transaction.Notify(() => StreakChanged?.Invoke(streak));
        if (!breaks || !victim) return;
        float distance = Mathf.Min(DisplacementTier.KnockbackMax,
            4f * (1f + .3f * context.Snapshot.Rank(WeaponBoon.SeismicFist)));
        _player.ApplyGuardImpact(victim, context, transaction,
            BaseStanceOnThird * ConsecutiveHitCounter.StanceMultiplier(rank), distance);
    }

    // Preserve the legacy bus for callers without a contextual execution; contextual hits are handled above.
    private void OnBasicHit(Actor victim, float damage)
    {
        if (_rank <= 0 || !_player || !enabled || (_combat?.EnergyPolicy != null && !isActiveAndEnabled) ||
            _player.IsResolvingGauntletImpact || !(damage > 0f) || float.IsInfinity(damage)) return;
        bool breaks = _counter.RegisterBasicHit();
        StreakChanged?.Invoke(_counter.Count);
        if (breaks && victim)
            _player.ApplyHitReactionTo(victim, HitReactionType.Stagger, HitStrength.Heavy,
                BaseStanceOnThird * ConsecutiveHitCounter.StanceMultiplier(_rank), StanceBreakEffect.Knockback, 0f);
    }

    private void OnAbilityUsed(int slot) { if (slot >= 0 && slot <= 2) ResetStreak(); }
    private void ResetStreak() { _counter.Reset(); StreakChanged?.Invoke(0); }
    private void Subscribe()
    {
        if (_hooks != null) _hooks.OnBasicHit += OnBasicHit;
        if (_holder) _holder.AbilityUsed += OnAbilityUsed;
    }
    private void Unsubscribe()
    {
        if (_hooks != null) _hooks.OnBasicHit -= OnBasicHit;
        if (_holder) _holder.AbilityUsed -= OnAbilityUsed;
    }
    private void OnEnable() { Unsubscribe(); Subscribe(); }
    private void OnDisable() { Unsubscribe(); ResetStreak(); }
    private void OnDestroy() => Unsubscribe();
}
