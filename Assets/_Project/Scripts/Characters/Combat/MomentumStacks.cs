using UnityEngine;

/// <summary>
/// Momentum Strike boon (impactful-weapon-boons R6). A run-scoped aggression-stack holder, added to
/// the player by <c>RunBoons</c> when the Gauntlet "Golpe de ímpeto" boon is chosen.
///
/// Each Direct_Hit adds one stack (up to <see cref="MaxStacks"/>); taking damage clears every stack.
/// The current bonus is expressed as a single run-scoped <see cref="PlayerStatModifier"/> on
/// <see cref="PlayerStatType.IncreasedDamagePercent"/> in <see cref="PlayerStatModifierMode.IncreasedPercent"/>
/// mode, valued at <c>2 * rank * stacks</c> percent (R6.2). On every stack change the old modifier is
/// removed and a fresh one re-added so <c>GetStat</c> reflects the current stack count through the
/// normal additive layer.
///
/// The MonoBehaviour stays thin: it holds run-scoped references, tracks the stack count, keeps the
/// single stat modifier in sync, and pushes a cosmetic HUD indicator. All decision logic is trivial
/// arithmetic; there is no per-frame <c>Update</c>.
///
/// Sources of the two triggers:
/// - Direct_Hit -> <c>HookBus.OnHit</c> (raised on every damaging basic/area/projectile hit).
/// - Taking damage -> the player's own <c>Actor.DamageReceived</c> event, which fires only when the
///   player actually loses health (R6.3), so heals never reset stacks.
///
/// Lifecycle (R6.5): the stat modifier is tagged with the <c>RunBoons</c> instance as its source, so
/// <c>RunBoons.OnDestroy</c>'s <c>Stats.RemoveModifiersFrom(this)</c> removes it at run end. The
/// <c>HookBus.OnHit</c> subscription is dropped by <c>HookBus.Clear()</c>; the <c>DamageReceived</c>
/// subscription is dropped in <see cref="OnDestroy"/> when the player object is torn down.
/// </summary>
public sealed class MomentumStacks : MonoBehaviour
{
    /// <summary>Maximum number of Momentum stacks (R6.1).</summary>
    public const int MaxStacks = 10;

    /// <summary>Outgoing-damage increase per rank, per stack, in percentage points (R6.2).</summary>
    private const float PercentPerRankPerStack = 2f;

    private PlayerActor _player;
    private HookBus _hooks;
    private Object _statSource;      // the RunBoons instance the modifier is tagged with (R6.5).
    private int _rank;
    private int _stacks;
    private PlayerStatModifier _mod; // the single run-scoped modifier we rescale on each stack change.

    /// <summary>The current stack count. Exposed for the HUD/tests; never gates the damage bonus.</summary>
    public int Stacks => _stacks;

    /// <summary>
    /// Binds this holder to the run. <paramref name="statSource"/> is the <c>RunBoons</c> instance the
    /// momentum stat modifier is tagged with, so run-end teardown removes it via
    /// <c>Stats.RemoveModifiersFrom(statSource)</c>. Safe to call again when the boon rank increases:
    /// the previous subscriptions are dropped first so no handler is registered twice, and the stat
    /// modifier is rescaled to the new rank immediately.
    /// </summary>
    public void Configure(PlayerActor player, HookBus hooks, int rank, Object statSource)
    {
        Unsubscribe();
        _player = player;
        _hooks = hooks;
        _rank = Mathf.Max(0, rank);
        _statSource = statSource;
        Subscribe();
        // Reflect the new rank against the current stack count without disturbing the stacks themselves.
        ApplyModifier();
    }

    private void Subscribe()
    {
        if (_hooks != null) _hooks.OnHit += OnDirectHit;
        if (_player) _player.DamageReceived += OnDamageReceived;
    }

    private void Unsubscribe()
    {
        if (_hooks != null) _hooks.OnHit -= OnDirectHit;
        if (_player) _player.DamageReceived -= OnDamageReceived;
    }

    // R6.1: a direct hit adds one stack, clamped to MaxStacks, then rescales the stat modifier.
    private void OnDirectHit(Actor victim, float damage)
    {
        if (_rank <= 0 || _stacks >= MaxStacks) return;
        _stacks++;
        ApplyModifier();
        PushHud();
    }

    // R6.3: taking any damage resets stacks (and the bonus) to zero.
    private void OnDamageReceived(Actor self, float amount)
    {
        if (_stacks == 0) return;
        _stacks = 0;
        ApplyModifier();
        PushHud();
    }

    // R6.2: remove our previous modifier and add a fresh one valued at (2 * rank * stacks) percent, so
    // GetStat reflects the current stack count. Only this holder's own modifier instance is removed
    // (via RemoveModifier), leaving other RunBoons stat picks that share the same source tag intact.
    // The modifier is still tagged with the RunBoons source so run-end RemoveModifiersFrom(RunBoons)
    // clears it (R6.5). A zero value re-adds a modifier that contributes nothing to the sum.
    private void ApplyModifier()
    {
        if (!_player) return;
        PlayerArpgStats stats = _player.Stats;
        if (_mod != null) { stats.RemoveModifier(_mod); _mod = null; }

        float value = PercentPerRankPerStack * _rank * _stacks;
        _mod = stats.AddModifier(new PlayerStatModifier(PlayerStatType.IncreasedDamagePercent,
            PlayerStatModifierMode.IncreasedPercent, value, _statSource), _statSource);
    }

    // R6.4: push the current stack count to the HUD; a zero count clears the indicator. Null-guarded so
    // absence of a HUD (headless/test scenes) never affects gameplay.
    private void PushHud()
    {
        if (!_player || !_player.TryGetComponent(out CharControlScript controls)) return;
        PlayerHUD hud = controls.HUD;
        if (hud) hud.ShowMomentumStacks(_stacks);
    }

    private void OnDestroy() => Unsubscribe();
}
