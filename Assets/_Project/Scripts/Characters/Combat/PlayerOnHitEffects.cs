using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-run registry of on-hit status effects the player applies to every enemy they damage.
/// RunBoons toggles/upgrades these. PlayerActor reports each resolved attack exactly once,
/// including hitboxes, areas and projectiles. Secondary cascades apply elements without recursion.
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
    private float _chillChance;
    private RunSynergyEffects _synergies;
    public float ChillChance => _chillChance;
    public RunSynergyEffects Synergies => _synergies ? _synergies :
        (_synergies = GetComponent<RunSynergyEffects>() ?? gameObject.AddComponent<RunSynergyEffects>());

    public bool HasAnyEffect => _burnEnabled || _chillEnabled || (_synergies && _synergies.HasModifiers);

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
        _chillChance = Mathf.Clamp01(_chillChance + Mathf.Clamp01(chance));
    }

    /// <summary>Applies every enabled effect to a single damaged enemy.</summary>
    public void ApplyTo(Actor enemy, float damageDealt = 0f)
    {
        if (!enemy || enemy is PlayerActor) return;
        ApplyElements(enemy, damageDealt);
        if (_synergies && damageDealt > 0f) _synergies.Resolve(enemy, damageDealt, this);
    }

    public void ApplyElements(Actor enemy, float damageDealt)
    {
        if (!enemy || enemy.IsDead || enemy is PlayerActor) return;

        if (_burnEnabled && _burnDps > 0f)
            BurnStatus.Apply(enemy, _burnDps + damageDealt * (_synergies ? _synergies.BurnScaling : 0f), _burnDuration);

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
        _chillEnabled = false; _chillSlow = 0f; _chillDuration = 0f; _chillChance = 0f;
        if (_synergies) _synergies.Clear();
    }
}
