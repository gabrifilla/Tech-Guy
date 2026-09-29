using UnityEngine;

/// <summary>
/// A temporary, depletable damage-absorbing buffer granted to an <see cref="Actor"/> by
/// <c>ShieldSupportBehavior</c>. It implements <see cref="IDamageAbsorber"/> so that
/// <see cref="Actor.TakeDamage"/> offers incoming damage to it first: the shield consumes what its
/// remaining capacity allows and returns the leftover, which is the only portion that reduces the
/// ally's health (R14.3). The shield self-removes (destroys this component) as soon as its capacity
/// reaches 0 or its duration expires, after which incoming damage reduces health again.
/// </summary>
/// <remarks>
/// The MonoBehaviour stays thin: the absorb math lives in the scene-free <see cref="ShieldBuffer"/>
/// struct so it can be property-tested without a live scene (task 2.3). This component only owns the
/// Unity concerns — expiry timing via <see cref="Update"/> and self-removal via
/// <see cref="Destroy(Object)"/>.
/// Feature: enemy-swarm-core-archetypes, task 2.2. Requirements: 14.3.
/// </remarks>
[DisallowMultipleComponent]
public sealed class Shield : MonoBehaviour, IDamageAbsorber
{
    private ShieldBuffer _buffer;
    private float _remainingSeconds;
    private bool _initialized;

    /// <summary>The absorption capacity still available before the shield is depleted.</summary>
    public float RemainingCapacity => _buffer.Remaining;

    /// <summary>The time in seconds until the shield expires.</summary>
    public float RemainingSeconds => _remainingSeconds;

    /// <summary>
    /// Configures the shield with a finite absorption capacity and a lifetime, then begins its
    /// expiry countdown. A non-positive capacity or duration leaves the shield with nothing to give
    /// and it removes itself immediately.
    /// </summary>
    /// <param name="capacity">Total damage the shield can absorb before depleting (R14.3).</param>
    /// <param name="durationSeconds">Lifetime in seconds; the design bounds this to 3–10s (R14.3).</param>
    public void Configure(float capacity, float durationSeconds)
    {
        _buffer = new ShieldBuffer(capacity);
        _remainingSeconds = durationSeconds;
        _initialized = true;

        // Nothing to protect with: an empty or already-expired shield removes itself at once.
        if (_buffer.IsDepleted || _remainingSeconds <= 0f)
            SelfRemove();
    }

    /// <summary>
    /// Absorbs as much of <paramref name="amount"/> as the remaining capacity allows and returns the
    /// leftover damage that should still reduce health. The returned value is always in
    /// <c>[0, amount]</c> and never increases the incoming damage. Absorbing down to 0 capacity
    /// triggers self-removal (R14.3).
    /// </summary>
    public float Absorb(float amount)
    {
        // Not yet configured, or a non-positive hit: pass the damage through untouched.
        if (!_initialized || amount <= 0f)
            return Mathf.Max(0f, amount);

        float leftover = _buffer.Absorb(amount);
        if (_buffer.IsDepleted)
            SelfRemove();
        return leftover;
    }

    private void Update()
    {
        if (!_initialized) return;

        _remainingSeconds -= Time.deltaTime;
        if (_remainingSeconds <= 0f)
            SelfRemove();
    }

    private void SelfRemove()
    {
        // Guard so a depletion during OnDestroy / double-trigger cannot re-destroy.
        _initialized = false;
        Destroy(this);
    }
}
