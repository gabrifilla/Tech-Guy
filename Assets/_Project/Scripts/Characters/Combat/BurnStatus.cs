using UnityEngine;

/// <summary>
/// Burn: deals damage over time in ticks. Re-applying refreshes the duration and keeps the higher
/// tick damage, so repeated hits sustain the burn without stacking into runaway damage.
/// </summary>
public sealed class BurnStatus : StatusEffect
{
    private float _damagePerTick = 2f;
    private float _tickInterval = 0.5f;
    private float _tickTimer;

    /// <summary>Adds or refreshes a burn on <paramref name="target"/>.</summary>
    public static void Apply(Actor target, float damagePerSecond, float duration)
    {
        if (!target || target.IsDead || damagePerSecond <= 0f || duration <= 0f) return;

        BurnStatus burn = target.GetComponent<BurnStatus>();
        if (!burn) burn = target.gameObject.AddComponent<BurnStatus>();
        burn.Configure(damagePerSecond, duration);
    }

    private void Configure(float damagePerSecond, float duration)
    {
        _tickInterval = 0.5f;
        // Keep the stronger burn if re-applied by a bigger hit.
        _damagePerTick = Mathf.Max(_damagePerTick, damagePerSecond * _tickInterval);
        RefreshDuration(duration);
    }

    protected override void Tick(float deltaTime)
    {
        _tickTimer -= deltaTime;
        if (_tickTimer > 0f) return;
        _tickTimer = _tickInterval;
        if (Host && !Host.IsDead) Host.TakeDamage(_damagePerTick);
    }
}
