using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Actor : MonoBehaviour
{
    public float health;
    public float maxHealth { get; private set; }

    public Image healthBar;

    public virtual void Awake()
    { 
        maxHealth = health;
        healthBar.gameObject.SetActive(false);
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
        healthBar.gameObject.SetActive(false);
        Destroy(gameObject);
    }
}