using UnityEngine;

/// <summary>
/// Hungry Combo boon (gauntlet-boon-playstyle-overhaul R6, "Combo faminto"). A run-scoped coordinator
/// added to the player by <c>RunBoons</c> when the Gauntlet boon is chosen.
///
/// Each Basic_Attack direct hit shaves the remaining recharge off the player's skills: on every
/// <see cref="HookBus.OnBasicHit"/> it calls <see cref="AbilityHolder.ReduceCooldowns(float)"/> with
/// <c>0.3s * rank</c> (R6.1). Weaving basics therefore accelerates the kit without ever touching an
/// authored asset — <c>ReduceCooldowns</c> only lowers the live, run-scoped timers of slots that are
/// currently on cooldown, clamped at zero, and leaves Ready/Active slots and <c>Ability.cooldownTime</c>
/// untouched (R6.3).
///
/// Mirrors the <see cref="MomentumStacks"/>/<c>AsuraSurge</c> shape: the MonoBehaviour stays thin
/// (run-scoped references plus a rank), subscribes only to the basic-hit channel, has no per-frame
/// <c>Update</c>, and reconfigures idempotently so a higher-rank pick re-attaches a single subscription.
///
/// Lifecycle (R6.4): the <see cref="HookBus.OnBasicHit"/> subscription is dropped both by
/// <see cref="OnDestroy"/> when the component is torn down and by <c>HookBus.Clear()</c> at run end, so
/// a previous run's boon never cuts cooldowns in a later run.
/// </summary>
public sealed class HungryComboTracker : MonoBehaviour
{
    /// <summary>Seconds of remaining cooldown removed per rank, per basic hit (R6.1).</summary>
    private const float SecondsPerRank = 0.3f;

    private PlayerActor _player;
    private HookBus _hooks;
    private AbilityHolder _holder;
    private int _rank;

    /// <summary>
    /// Binds this tracker to the run. Safe to call again when the boon rank increases: the previous
    /// subscription is dropped first so no handler is registered twice, then re-added at the new rank.
    /// </summary>
    public void Configure(PlayerActor player, HookBus hooks, AbilityHolder holder, int rank)
    {
        Unsubscribe();
        _player = player;
        _hooks = hooks;
        _holder = holder;
        _rank = Mathf.Max(0, rank);
        Subscribe();
    }

    private void Subscribe() { if (_hooks != null) _hooks.OnBasicHit += OnBasicHit; }   // R6: only the basic channel

    private void Unsubscribe() { if (_hooks != null) _hooks.OnBasicHit -= OnBasicHit; }

    // R6.1: a basic hit removes (0.3 * rank)s of remaining recharge. ReduceCooldowns clamps at zero and
    // ignores slots that are not currently on cooldown, so this is a no-op when nothing is recharging (R6.3).
    private void OnBasicHit(Actor victim, float damage)
    {
        if (_rank <= 0 || !_holder) return;
        _holder.ReduceCooldowns(SecondsPerRank * _rank);
    }

    private void OnDestroy() => Unsubscribe();   // + HookBus.Clear() drops the subscription at run end (R6.4)
}
