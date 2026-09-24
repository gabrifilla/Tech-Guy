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
    public event Action<Actor> Died;
    public bool IsDead { get; private set; }
    private readonly Dictionary<UnityEngine.Object, float> _damageTakenModifiers = new Dictionary<UnityEngine.Object, float>();

    public void SetDamageTakenMultiplier(UnityEngine.Object source, float multiplier)
    {
        if (source) _damageTakenModifiers[source] = Mathf.Clamp(multiplier, 0.1f, 10f);
    }

    public void RemoveDamageTakenMultiplier(UnityEngine.Object source)
    {
        if (source) _damageTakenModifiers.Remove(source);
    }

    public virtual void Awake()
    { 
        maxHealth = health;
        IsDead = false;
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        UpdateHealthBar();
    }

    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;
        foreach (var modifier in _damageTakenModifiers)
            if (modifier.Key) amount *= modifier.Value;
        health = Mathf.Max(0f, health - amount);
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

    public void SetMaxHealth(float value)
    {
        float ratio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 1f;
        maxHealth = Mathf.Max(1f, value);
        health = IsDead ? 0f : maxHealth * ratio;
        UpdateHealthBar();
        HealthChanged?.Invoke(this);
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
            float resolvedMaxHealth = Mathf.Max(maxHealth, 0.0001f);
            healthBar.fillAmount = Mathf.Clamp01(health / resolvedMaxHealth);
        }
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
