using UnityEngine;

/// <summary>
/// Base class for timed debuffs applied to an enemy Actor. Effects are attached as components and
/// refresh (rather than stack uncontrollably) when re-applied. They automatically clean up when
/// the effect expires or the host dies.
/// </summary>
[DisallowMultipleComponent]
public abstract class StatusEffect : MonoBehaviour
{
    protected Actor Host { get; private set; }
    protected float RemainingTime { get; private set; }

    protected virtual void Awake()
    {
        Host = GetComponent<Actor>() ?? GetComponentInParent<Actor>();
        if (Host) Host.Died += OnHostDied;
    }

    protected virtual void OnDestroy()
    {
        if (Host) Host.Died -= OnHostDied;
    }

    /// <summary>Extends the effect's remaining duration to at least <paramref name="duration"/>.</summary>
    protected void RefreshDuration(float duration)
    {
        RemainingTime = Mathf.Max(RemainingTime, Mathf.Max(0f, duration));
    }

    protected virtual void Update()
    {
        if (!Host || Host.IsDead) { EndEffect(); return; }
        RemainingTime -= Time.deltaTime;
        Tick(Time.deltaTime);
        if (RemainingTime <= 0f) EndEffect();
    }

    /// <summary>Per-frame behaviour while the effect is active.</summary>
    protected abstract void Tick(float deltaTime);

    /// <summary>Removes the effect (restores any modified state) and destroys the component.</summary>
    protected void EndEffect()
    {
        OnEffectEnded();
        Destroy(this);
    }

    /// <summary>Hook to undo any state changes (e.g. restore movement speed).</summary>
    protected virtual void OnEffectEnded() { }

    private void OnHostDied(Actor actor) => EndEffect();
}
