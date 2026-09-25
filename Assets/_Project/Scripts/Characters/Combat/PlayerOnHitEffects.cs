using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-run registry of on-hit status effects the player applies to every enemy they damage.
/// RunBoons toggles/upgrades these (e.g. "basic attacks now Burn" or "attacks may Freeze"). Both
/// the basic-swing path (HitboxDamage) and the ability/area path (PlayerActor) call
/// <see cref="ApplyTo"/> for each damaged enemy, so a single enabled effect covers all hits.
/// </summary>
[DisallowMultipleComponent]
public sealed class PlayerOnHitEffects : MonoBehaviour
{
    private bool _burnEnabled;
    private float _burnDps;
    private float _burnDuration;

    private bool _chillEnabled;
    private float _chillSlow;
    private float _chillDuration;
    private float _chillChance = 1f;

    public bool HasAnyEffect => _burnEnabled || _chillEnabled;

    /// <summary>Enables/strengthens burn on hit. Repeated calls add damage and extend duration.</summary>
    public void EnableBurn(float damagePerSecond, float duration)
    {
        _burnEnabled = true;
        _burnDps += Mathf.Max(0f, damagePerSecond);
        _burnDuration = Mathf.Max(_burnDuration, duration);
    }

    /// <summary>Enables/strengthens chill on hit. chance 0..1 for the freeze/slow to trigger per hit.</summary>
    public void EnableChill(float slowFraction, float duration, float chance)
    {
        _chillEnabled = true;
        _chillSlow = Mathf.Max(_chillSlow, Mathf.Clamp01(slowFraction));
        _chillDuration = Mathf.Max(_chillDuration, duration);
        _chillChance = Mathf.Clamp01(Mathf.Max(_chillChance, chance));
    }

    /// <summary>Applies every enabled effect to a single damaged enemy.</summary>
    public void ApplyTo(Actor enemy)
    {
        if (!enemy || enemy.IsDead || enemy is PlayerActor) return;

        if (_burnEnabled && _burnDps > 0f)
            BurnStatus.Apply(enemy, _burnDps, _burnDuration);

        if (_chillEnabled && _chillSlow > 0f && Random.value <= _chillChance)
            ChillStatus.Apply(enemy, _chillSlow, _chillDuration);
    }

    /// <summary>Applies every enabled effect to a batch of damaged enemies.</summary>
    public void ApplyTo(IReadOnlyList<Actor> enemies)
    {
        if (enemies == null) return;
        for (int i = 0; i < enemies.Count; i++) ApplyTo(enemies[i]);
    }

    /// <summary>Clears all on-hit effects. Called at the end of a run so modifiers don't leak into the next.</summary>
    public void Clear()
    {
        _burnEnabled = false; _burnDps = 0f; _burnDuration = 0f;
        _chillEnabled = false; _chillSlow = 0f; _chillDuration = 0f; _chillChance = 1f;
    }
}
