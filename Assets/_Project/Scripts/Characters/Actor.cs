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

    public event Action<Actor> Died;
    public bool IsDead { get; private set; }

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
        if (healthBar != null)
        {
            healthBar.fillAmount = Mathf.Clamp(health / maxHealth, 0, 1);
        }
    }

    public virtual void TakeDamage(float amount)
    {
        if (IsDead) return;

        health -= amount;
        UpdateHealthBar();

        EnemyTargetUI ui = EnemyTargetUI.Instance;
        if (ui != null)
        {
            ui.NotifyHealthChanged(this);
        }

        if (health <= 0)
        { Death(); }
    }

    public void RestoreHealthToMax()
    {
        IsDead = false;
        health = maxHealth;
        UpdateHealthBar();
    }

    protected void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = Mathf.Clamp(health / maxHealth, 0, 1);
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
