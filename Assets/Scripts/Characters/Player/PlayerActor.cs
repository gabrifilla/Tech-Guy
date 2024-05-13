using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class PlayerActor : Actor
{
    public float mana;
    public float maxMana { get; private set; }

    public Image manaBar;

    private Animator animator; // Adicione uma referência ao componente Animator

    public override void Awake()
    {
        base.Awake();
        healthBar.gameObject.SetActive(true); // A barra de vida do jogador está sempre visível
        manaBar.gameObject.SetActive(true); // A barra de mana do jogador está sempre visível
        animator = GetComponent<Animator>(); // Inicialize a referência ao componente Animator
    }

    void Update()
    {
        if (manaBar != null)
        {
            manaBar.fillAmount = Mathf.Clamp(mana / maxMana, 0, 1);
        }
    }

    public void UseMana(float amount)
    {
        mana -= amount;
        if (mana <= 0)
        { Debug.Log("Cabo mana, faz alguma coisa!"); }
    }

    // Sobrescreva a função TakeDamage
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount); // Chame a função TakeDamage da classe base
        animator.SetTrigger("GetHit"); // Ative a animação de receber dano
    }
}
