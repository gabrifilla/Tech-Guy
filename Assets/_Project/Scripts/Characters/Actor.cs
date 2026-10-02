using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Actor : MonoBehaviour
{
    public float health;
    public float maxHealth { get; protected set; }

    public Image healthBar;

    public event Action<Actor> HealthChanged;
    public event Action<Actor, float> DamageReceived;
    public event Action<Actor> DamageAbsorbed;
    public event Action<Actor> Died;
    public bool IsDead { get; private set; }
    private readonly Dictionary<UnityEngine.Object, float> _damageTakenModifiers = new Dictionary<UnityEngine.Object, float>();
    // Full damage-immunity sources, keyed by source like _damageTakenModifiers (R6.9). This is the
    // SAME immunity channel the damage-taken multiplier uses (source-keyed, applied in TakeDamage),
    // not a parallel system — but it grants TRUE immunity, which SetDamageTakenMultiplier cannot
    // express because it clamps its multiplier to a 0.1 floor (90% reduction, never 0). The dash's
    // i-frame window (DashScript) adds itself here on start and removes itself on every exit path
    // (window elapsed / cancel / death / room change / end of displacement) so no residual immunity
    // can linger (R6.8). A HashSet keeps add/remove idempotent and order-independent.
    private readonly HashSet<UnityEngine.Object> _damageImmunitySources = new HashSet<UnityEngine.Object>();

    /// <summary>
    /// True while at least one source has granted full damage immunity via
    /// <see cref="AddDamageImmunity"/> (R6.9). While true, <see cref="TakeDamage"/> ignores incoming
    /// damage entirely (not merely reduced). Exposed read-only for callers that need to observe the
    /// i-frame state without mutating it.
    /// </summary>
    public bool IsDamageImmune => _damageImmunitySources.Count > 0;

    public void SetDamageTakenMultiplier(UnityEngine.Object source, float multiplier)
    {
        if (source) _damageTakenModifiers[source] = Mathf.Clamp(multiplier, 0.1f, 10f);
    }

    public void RemoveDamageTakenMultiplier(UnityEngine.Object source)
    {
        if (source) _damageTakenModifiers.Remove(source);
    }

    /// <summary>
    /// Grants full damage immunity from <paramref name="source"/> (R6.9). Multiple sources are
    /// tracked independently; immunity lasts until every source has called
    /// <see cref="RemoveDamageImmunity"/>. Idempotent per source.
    /// </summary>
    public void AddDamageImmunity(UnityEngine.Object source)
    {
        if (source) _damageImmunitySources.Add(source);
    }

    /// <summary>
    /// Removes the immunity previously granted by <paramref name="source"/> (R6.8). Once no source
    /// remains, <see cref="IsDamageImmune"/> returns to false with no residual immunity. Safe to
    /// call even if the source never added immunity.
    /// </summary>
    public void RemoveDamageImmunity(UnityEngine.Object source)
    {
        if (source) _damageImmunitySources.Remove(source);
    }

    public virtual void Awake()
    { 
        maxHealth = health;
        IsDead = false;
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }
        if (!(this is PlayerActor) && !TryGetComponent<EnemyCombatFeedback>(out _))
            gameObject.AddComponent<EnemyCombatFeedback>();
        if (!(this is PlayerActor) && !TryGetComponent<CoinDrop>(out _))
            gameObject.AddComponent<CoinDrop>();

        // Drive the health bar by event instead of per-frame (R4.2/R4.4). The actor already fires
        // HealthChanged at every health-mutation point (TakeDamage/SetMaxHealth/Heal/
        // RestoreHealthToMax), so subscribing here keeps the bar identical without the per-frame
        // UpdateHealthBar cost that previously ran for every actor each frame.
        HealthChanged += OnHealthChangedUpdateBar;
    }

    private void OnHealthChangedUpdateBar(Actor _)
    {
        UpdateHealthBar();
    }

    protected virtual void OnDestroy()
    {
        // Unsubscribe to avoid leaking the handler across destroyed actors.
        HealthChanged -= OnHealthChangedUpdateBar;
    }

    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        // Full i-frame immunity (R6.9): a damage-immune actor ignores the hit entirely — no health
        // loss, no DamageReceived, no absorber consumption. This precedes the damage-taken
        // multipliers because immunity is absolute, not a scaling. The dash arms/disarms this through
        // Add/RemoveDamageImmunity around its i-frame window.
        if (IsDamageImmune) return;
        foreach (var modifier in _damageTakenModifiers)
            if (modifier.Key) amount *= modifier.Value;

        // Offer incoming damage to an attached damage absorber (e.g. Shield) before it
        // reaches health. The absorber consumes what it can and returns the leftover, which
        // is the only portion that reduces health. When no absorber is present this is a
        // no-op and existing behavior is preserved.
        bool fullyAbsorbed = false;
        if (amount > 0f && TryGetComponent<IDamageAbsorber>(out var absorber))
        {
            float absorberInput = amount;
            amount = Mathf.Max(0f, absorber.Absorb(amount));
            fullyAbsorbed = WasFullyAbsorbed(absorberInput, amount);
        }

        float previousHealth = health;
        health = Mathf.Max(0f, health - amount);
        float actualDamage = previousHealth - health;
        if (actualDamage > 0f) DamageReceived?.Invoke(this, actualDamage);
        // A hit was fully absorbed when the absorber received a positive amount and left
        // nothing to reach health. This is distinct from a reduction via
        // _damageTakenModifiers (which scales amount before the absorber) and from a plain
        // zero-damage hit with no absorber, so ProtectionIndicator only reacts to real
        // absorption (R6.3, R6.8).
        if (fullyAbsorbed) DamageAbsorbed?.Invoke(this);
        UpdateHealthBar();
        HealthChanged?.Invoke(this);

        EnemyTargetUI ui = EnemyTargetUI.Instance;
        if (ui != null)
        {
            ui.NotifyHealthChanged(this);
        }

        if (health <= 0)
        { Death(); }
    }

    /// <summary>
    /// Pure decision for whether an incoming hit was fully absorbed: the absorber received a
    /// positive <paramref name="absorberInput"/> and returned a non-positive
    /// <paramref name="leftover"/> (nothing reached health). Kept static and side-effect free so it
    /// can be property-tested without a scene.
    /// </summary>
    public static bool WasFullyAbsorbed(float absorberInput, float leftover)
    {
        return absorberInput > 0f && leftover <= 0f;
    }

    public void SetMaxHealth(float value)
    {
        float ratio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 1f;
        maxHealth = Mathf.Max(1f, value);
        health = IsDead ? 0f : maxHealth * ratio;
        UpdateHealthBar();
        HealthChanged?.Invoke(this);
    }

    /// <summary>
    /// Restores a fixed amount of health, clamped so it never exceeds <see cref="maxHealth"/>.
    /// No-op when the actor is dead or when <paramref name="amount"/> is not positive.
    /// </summary>
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        float previousHealth = health;
        health = HealMath.Clamp(health, maxHealth, amount);
        if (health == previousHealth) return;
        UpdateHealthBar();
        HealthChanged?.Invoke(this);

        EnemyTargetUI ui = EnemyTargetUI.Instance;
        if (ui != null)
        {
            ui.NotifyHealthChanged(this);
        }
    }

    public void RestoreHealthToMax()
    {
        IsDead = false;
        health = maxHealth;
        UpdateHealthBar();
        HealthChanged?.Invoke(this);
    }

    protected void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = HealthBarFill(health, maxHealth);
        }
    }

    /// <summary>
    /// Pure fill computation for the health bar: <c>clamp01(health / max)</c>, with the same guard
    /// against a non-positive <paramref name="maxHealth"/> the per-instance code used. Kept static and
    /// side-effect free so it can be property-tested without a scene (Property 5), while the on-screen
    /// result stays identical to the previous inline formula.
    /// </summary>
    public static float HealthBarFill(float health, float maxHealth)
    {
        float resolvedMaxHealth = Mathf.Max(maxHealth, 0.0001f);
        return Mathf.Clamp01(health / resolvedMaxHealth);
    }

    protected virtual void Death()
    {
        if (IsDead) return;

        IsDead = true;
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        Died?.Invoke(this);
        Destroy(gameObject);
    }
}
