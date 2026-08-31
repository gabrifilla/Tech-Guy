using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Actor : MonoBehaviour
{
    public float health;
    public float maxHealth { get; protected set; }

    public Image healthBar;

    public virtual void Awake()
    { 
        maxHealth = health;
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

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.fillAmount = Mathf.Clamp(health / maxHealth, 0, 1);
        }
    }

    void Death()
    {
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }
        Destroy(gameObject);
    }
}
