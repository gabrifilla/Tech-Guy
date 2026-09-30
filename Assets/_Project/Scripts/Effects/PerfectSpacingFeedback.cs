using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cosmetic feedback for the Perfect Spacing spear boon (impactful-weapon-boons R4.3).
///
/// Subscribes to the player's resolved-hit notification (the same
/// <see cref="AbilityHolder.NotifyAttackHits"/>/<c>OnAfterAttackHits</c> channel passives use, surfaced
/// through <see cref="AbilityHolder.AttackHitsResolved"/>) and escalates a streak as consecutive
/// in-band direct hits land, resetting on an out-of-band hit.
///
/// This component is purely presentational: it mirrors the same distance band the damage term in
/// <see cref="WeaponRunModifiers.DirectDamageMultiplier"/> uses ([3.5, 6.5]m from the player), but it
/// NEVER gates or feeds that term — the multiplier is computed independently in the direct-hit path.
/// It reads nothing from and writes nothing to any source <c>ScriptableObject</c>. Thin by design:
/// it only tracks a streak counter and raises an event a HUD can render.
/// </summary>
[RequireComponent(typeof(PlayerActor))]
public sealed class PerfectSpacingFeedback : MonoBehaviour
{
    // Must match the reward band in WeaponRunModifiers.DirectDamageMultiplier (R4.1/R4.2).
    public const float MinBandDistance = 3.5f;
    public const float MaxBandDistance = 6.5f;

    private PlayerActor _player;
    private AbilityHolder _holder;
    private int _streak;

    /// <summary>Current consecutive in-band-hit streak; a HUD can render this. Resets to 0 out of band.</summary>
    public int Streak => _streak;

    /// <summary>Raised whenever the streak changes (escalates or resets), so a HUD can update. Cosmetic only.</summary>
    public event Action<int> StreakChanged;

    /// <summary>
    /// Bind the feedback to the run-scoped player + holder. Called by <see cref="RunBoons"/> when the
    /// Perfect Spacing boon is chosen, so there is no scene lookup here.
    /// </summary>
    public void Configure(PlayerActor player, AbilityHolder holder)
    {
        // Detach any previous subscription before re-binding so a reconfigure never double-subscribes.
        if (_holder) _holder.AttackHitsResolved -= OnAttackHitsResolved;
        _player = player;
        _holder = holder;
        if (_holder) _holder.AttackHitsResolved += OnAttackHitsResolved;
        SetStreak(0);
    }

    private void OnAttackHitsResolved(PlayerActor owner, IReadOnlyList<Actor> damagedActors)
    {
        if (owner != _player || !_player || damagedActors == null || damagedActors.Count == 0) return;

        // A resolved attack is a streak step: it counts as "in band" when the nearest damaged enemy
        // sits inside the reward band, mirroring the direct-hit damage term's own distance test.
        bool inBand = false;
        for (int i = 0; i < damagedActors.Count; i++)
        {
            Actor victim = damagedActors[i];
            if (!victim) continue;
            float distance = Vector3.Distance(_player.transform.position, victim.transform.position);
            if (distance >= MinBandDistance && distance <= MaxBandDistance) { inBand = true; break; }
        }

        SetStreak(inBand ? _streak + 1 : 0);
    }

    private void SetStreak(int value)
    {
        if (value == _streak) return;
        _streak = value;
        StreakChanged?.Invoke(_streak);
    }

    private void OnDestroy()
    {
        if (_holder) _holder.AttackHitsResolved -= OnAttackHitsResolved;
    }
}
