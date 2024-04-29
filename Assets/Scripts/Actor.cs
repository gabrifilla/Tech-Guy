using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Actor : MonoBehaviour
{
    public float health;
    public float maxHealth { get; private set; }

    public Image healthBar;

    void Awake()
    { maxHealth = health; }

    void Update()
    {
        healthBar.fillAmount = Mathf.Clamp(health / maxHealth, 0, 1);
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        if (health <= 0)
        { Death(); }
    }

    void Death()
    {
        Destroy(gameObject);
    }
}