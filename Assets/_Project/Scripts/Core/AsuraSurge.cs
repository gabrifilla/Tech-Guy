using UnityEngine;

/// <summary>
/// Asura Fist boon (gauntlet-boon-playstyle-overhaul R4, "Punho de Asura"). A run-scoped coordinator
/// added to the player by <c>RunBoons</c> when the Gauntlet boon is chosen.
///
/// Each Basic_Attack direct hit charges the Manopla's Asura meter: on every
/// <see cref="HookBus.OnBasicHit"/> it feeds <c>2 * rank</c> energy through
/// <see cref="BreakerGauntletCombat.AddAsuraEnergy(int)"/> (R4.1). That seam is a thin delegate over
/// <see cref="AsuraMomentum.AddEnergy(int)"/>, which already clamps to <c>[0, Maximum]</c>, so the
/// meter never overflows its authored cap (R4.2) and no asset is ever written.
///
/// Only the basic channel feeds the meter: the coordinator subscribes to <see cref="HookBus.OnBasicHit"/>
/// and never to the generic <c>OnHit</c>, so a skill (non-basic) hit grants nothing from this boon
/// (R4.3). The meter cap and basic-vs-skill routing are both exercised by Property 10.
///
/// Mirrors the <see cref="MomentumStacks"/>/<see cref="HungryComboTracker"/> shape: the MonoBehaviour
/// stays thin (run-scoped references plus a rank), subscribes only to the basic-hit channel, has no
/// per-frame <c>Update</c>, and reconfigures idempotently so a higher-rank pick re-attaches a single
/// subscription.
///
/// Lifecycle (R4.4): the <see cref="HookBus.OnBasicHit"/> subscription is dropped both by
/// <see cref="OnDestroy"/> when the component is torn down and by <c>HookBus.Clear()</c> at run end, so
/// a previous run's boon never charges Asura in a later run.
/// </summary>
public sealed class AsuraSurge : MonoBehaviour
{
    /// <summary>Asura energy added per rank, per basic hit (R4.1).</summary>
    private const int EnergyPerRankPerHit = 2;

    private PlayerActor _player;
    private HookBus _hooks;
    private BreakerGauntletCombat _combat;
    private int _rank;

    /// <summary>
    /// Binds this coordinator to the run. Safe to call again when the boon rank increases: the previous
    /// subscription is dropped first so no handler is registered twice, then re-added at the new rank.
    /// </summary>
    public void Configure(PlayerActor player, HookBus hooks, BreakerGauntletCombat combat, int rank)
    {
        Unsubscribe();
        _player = player;
        _hooks = hooks;
        _combat = combat;
        _rank = Mathf.Max(0, rank);
        Subscribe();
    }

    private void Subscribe() { if (_hooks != null) _hooks.OnBasicHit += OnBasicHit; }   // R4.3: only the basic channel

    private void Unsubscribe() { if (_hooks != null) _hooks.OnBasicHit -= OnBasicHit; }

    // R4.1/R4.2: a basic hit adds (2 * rank) Asura; AddAsuraEnergy delegates to AddEnergy, which clamps
    // to [0, Maximum], so an overflowing hit lands at the cap and a full meter stays there.
    private void OnBasicHit(Actor victim, float damage)
    {
        if (_rank <= 0 || !_combat) return;
        _combat.AddAsuraEnergy(EnergyPerRankPerHit * _rank);
    }

    private void OnDestroy() => Unsubscribe();   // + HookBus.Clear() drops the subscription at run end (R4.4)
}
