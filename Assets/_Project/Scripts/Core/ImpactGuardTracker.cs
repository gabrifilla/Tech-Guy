using System;
using UnityEngine;

/// <summary>
/// Guard Breaker boon (gauntlet-boon-playstyle-overhaul R5, "Guarda partida"). A run-scoped
/// coordinator added to the player by <c>RunBoons</c> when the Gauntlet boon is chosen.
///
/// It drives a pure <see cref="ConsecutiveHitCounter"/> from the Basic_Hit_Channel: every
/// <see cref="HookBus.OnBasicHit"/> advances the streak (R5.3) and every third consecutive basic
/// hit breaks the enemy's guard (R5.1). On that breaking hit it requests a <c>Stagger</c>/<c>Heavy</c>
/// reaction through <see cref="PlayerActor.ApplyHitReactionTo"/> with bonus stance damage scaled by
/// <c>1 + 0.5R</c> and the deliberate <see cref="StanceBreakEffect.Knockback"/> break effect
/// (R5.1/R5.2) — the knockback is resolved by the enemy's own locomotion channel, never a teleport.
///
/// Any interruption resets the streak (R5.3): the <see cref="HookBus.OnBasicHit"/> channel only
/// carries basic hits (a skill hit never increments), and a skill cast on
/// <see cref="AbilityHolder.AbilityUsed"/> clears the running count. <see cref="StreakChanged"/>
/// surfaces the building pressure to the HUD and fires <c>0</c> on every reset (R5.4).
///
/// Mirrors the <see cref="HungryComboTracker"/>/<see cref="MomentumStacks"/> shape: the MonoBehaviour
/// stays thin (run-scoped references plus a rank; all counting lives in the pure
/// <see cref="ConsecutiveHitCounter"/>), has no per-frame <c>Update</c>, and reconfigures
/// idempotently so a higher-rank pick re-attaches a single pair of subscriptions.
///
/// Lifecycle (R5.5): both subscriptions — <see cref="HookBus.OnBasicHit"/> and
/// <see cref="AbilityHolder.AbilityUsed"/> — are dropped by <see cref="OnDestroy"/> when the component
/// is torn down (so EditMode <c>DestroyImmediate</c> unwires reliably) and the basic channel is also
/// dropped by <c>HookBus.Clear()</c> at run end, so a previous run's streak never carries over.
/// No stance value is ever written back to a source weapon/ability asset.
/// </summary>
public sealed class ImpactGuardTracker : MonoBehaviour
{
    /// <summary>Consecutive basic hits that break the guard (R5.1).</summary>
    private const int HitsToBreak = 3;

    /// <summary>Base stance damage dealt by the breaking (third) hit before rank scaling (design starting point).</summary>
    private const float BaseStanceOnThird = 24f;

    private readonly ConsecutiveHitCounter _counter = new ConsecutiveHitCounter(HitsToBreak);
    private PlayerActor _player;
    private HookBus _hooks;
    private AbilityHolder _holder;
    private int _rank;

    /// <summary>Current consecutive basic-hit streak, exposed for HUD/tests (R5.4).</summary>
    public int Streak => _counter.Count;

    /// <summary>Raised whenever the streak changes (advances on a basic hit, or resets to 0). Cosmetic (R5.4).</summary>
    public event Action<int> StreakChanged;

    /// <summary>
    /// Binds this tracker to the run. Safe to call again when the boon rank increases: the previous
    /// subscriptions are dropped first so no handler is registered twice, then re-added at the new rank.
    /// The streak restarts clean on each (re)configure.
    /// </summary>
    public void Configure(PlayerActor player, HookBus hooks, AbilityHolder holder, int rank)
    {
        Unsubscribe();
        _player = player;
        _hooks = hooks;
        _holder = holder;
        _rank = Mathf.Max(0, rank);
        Subscribe();
        _counter.Reset();
        StreakChanged?.Invoke(0);
    }

    private void Subscribe()
    {
        if (_hooks != null) _hooks.OnBasicHit += OnBasicHit;   // R5.3: only the basic channel increments
        if (_holder) _holder.AbilityUsed += OnAbilityUsed;      // R5.3: a skill use resets the streak
    }

    private void Unsubscribe()
    {
        if (_hooks != null) _hooks.OnBasicHit -= OnBasicHit;
        if (_holder) _holder.AbilityUsed -= OnAbilityUsed;
    }

    // R5.1/R5.2: on the third consecutive basic hit, apply bonus stance = 24 * (1 + 0.5R) and request a
    // deliberate Knockback on the break. The victim comes straight off the basic channel, so the reaction
    // targets the enemy that was actually struck. pushDistance is 0f: the strong displacement comes only
    // from the StanceBreakEffect.Knockback resolved by the enemy's locomotion channel.
    private void OnBasicHit(Actor victim, float damage)
    {
        if (_rank <= 0 || !_player) return;
        bool breaks = _counter.RegisterBasicHit();
        StreakChanged?.Invoke(_counter.Count);
        if (!breaks || !victim) return;
        float stance = BaseStanceOnThird * ConsecutiveHitCounter.StanceMultiplier(_rank);
        _player.ApplyHitReactionTo(victim, HitReactionType.Stagger, HitStrength.Heavy,
            stance, StanceBreakEffect.Knockback, 0f);
    }

    // R5.3: any skill cast interrupts the basic run and clears the streak.
    private void OnAbilityUsed(int slot)
    {
        _counter.Reset();
        StreakChanged?.Invoke(0);
    }

    private void OnDestroy() => Unsubscribe();   // + HookBus.Clear() drops the basic subscription at run end (R5.5)
}
