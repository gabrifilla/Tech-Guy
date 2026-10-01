using UnityEngine;

/// <summary>
/// Adaptive Cadence boon (gauntlet-boon-playstyle-overhaul R9). A run-scoped firing-cadence tracker,
/// added to the player by <c>RunBoons</c> when the Bow "Cadência adaptativa" boon is chosen.
///
/// Mirrors <see cref="PerfectSpacingFeedback"/> (it reacts to the basic-hit channel and measures the
/// player→victim distance), but the effect here is <b>functional</b> rather than cosmetic: while the
/// player keeps landing basic hits at or beyond <see cref="KitingBand"/> metres, a single run-scoped
/// <see cref="PlayerStatModifier"/> on <see cref="PlayerStatType.AttackSpeedMultiplier"/> in
/// <see cref="PlayerStatModifierMode.IncreasedPercent"/> mode is active at <c>15 × rank</c> percent
/// (R9.1); a basic hit landed closer than the band removes it (R9.2). The modifier is tagged with the
/// <c>RunBoons</c> instance as its source, so <c>RunBoons.OnDestroy</c>'s
/// <c>Stats.RemoveModifiersFrom(this)</c> tears it down at run end (R9.4).
///
/// The MonoBehaviour stays thin: it holds run-scoped references, tracks whether the band is currently
/// held, and keeps the single stat modifier in sync. All decision logic is a trivial distance test;
/// there is no per-frame <c>Update</c>.
///
/// Lifecycle (R9.4): the <c>HookBus.OnBasicHit</c> subscription is dropped by <c>HookBus.Clear()</c> at
/// run end and in <see cref="OnDestroy"/>; the stat modifier is removed both by this component's own
/// teardown and by <c>RunBoons.OnDestroy</c>'s <c>RemoveModifiersFrom(RunBoons)</c>.
/// </summary>
public sealed class AdaptiveCadenceTracker : MonoBehaviour
{
    /// <summary>Minimum player→victim distance (metres) that keeps the cadence bonus active (R9.1/R9.2).</summary>
    public const float KitingBand = 6f;

    /// <summary>Attack-speed increase per rank, in percentage points (R9.1: +15% per rank).</summary>
    private const float PercentPerRank = 15f;

    private PlayerActor _player;
    private HookBus _hooks;
    private Object _statSource;      // the RunBoons instance the modifier is tagged with (R9.4).
    private int _rank;
    private bool _active;
    private PlayerStatModifier _mod; // the single run-scoped cadence modifier we toggle on/off.

    /// <summary>Whether the cadence bonus is currently active. Exposed for the HUD/tests.</summary>
    public bool IsActive => _active;

    /// <summary>
    /// Binds this tracker to the run. <paramref name="statSource"/> is the <c>RunBoons</c> instance the
    /// cadence stat modifier is tagged with, so run-end teardown removes it via
    /// <c>Stats.RemoveModifiersFrom(statSource)</c>. Safe to call again when the boon rank increases:
    /// the previous subscription is dropped first so no handler is registered twice, and the modifier is
    /// rescaled to the new rank immediately when the band is already held.
    /// </summary>
    public void Configure(PlayerActor player, HookBus hooks, int rank, Object statSource)
    {
        Unsubscribe();
        _player = player;
        _hooks = hooks;
        _rank = Mathf.Max(0, rank);
        _statSource = statSource;
        Subscribe();
        // Reflect the new rank against the current band state without disturbing it.
        if (_active) ApplyModifier();
    }

    private void Subscribe()
    {
        if (_hooks != null) _hooks.OnBasicHit += OnBasicHit;
    }

    private void Unsubscribe()
    {
        if (_hooks != null) _hooks.OnBasicHit -= OnBasicHit;
        RemoveMod();
        _active = false;
    }

    // R9.1/R9.2: a basic hit at or beyond the band activates the cadence bonus; a closer basic hit removes it.
    private void OnBasicHit(Actor victim, float damage)
    {
        if (_rank <= 0 || !_player || !victim) return;
        float distance = Vector3.Distance(_player.transform.position, victim.transform.position);
        SetActive(distance >= KitingBand);
    }

    // Toggles the cadence modifier. R9.1: on → add +15%*rank AttackSpeedMultiplier (IncreasedPercent);
    // R9.2: off → remove it. The modifier is tagged with the RunBoons source so run-end teardown clears it.
    private void SetActive(bool on)
    {
        if (on == _active) return;
        _active = on;
        if (_active) ApplyModifier();
        else RemoveMod();
    }

    private void ApplyModifier()
    {
        if (!_player) return;
        RemoveMod();
        _mod = _player.Stats.AddModifier(new PlayerStatModifier(PlayerStatType.AttackSpeedMultiplier,
            PlayerStatModifierMode.IncreasedPercent, PercentPerRank * _rank, _statSource), _statSource);
    }

    private void RemoveMod()
    {
        if (_mod != null && _player) _player.Stats.RemoveModifier(_mod);
        _mod = null;
    }

    private void OnDestroy() => Unsubscribe();
}
