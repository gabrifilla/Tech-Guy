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

    [Header("Weapon")]
    public Transform handTransform; // Transform da m�o do personagem
    private GameObject currentWeaponInstance; // Inst�ncia da arma atualmente equipada

    public WeaponScript weapon;

    private Animator animator; // Adicione uma refer�ncia ao componente Animator


    public override void Awake()
    {
        base.Awake();
        maxMana = mana;

        healthBar.gameObject.SetActive(true); // A barra de vida do jogador est� sempre vis�vel
        manaBar.gameObject.SetActive(true); // A barra de mana do jogador est� sempre vis�vel
        animator = GetComponent<Animator>(); // Inicialize a refer�ncia ao componente Animator

        EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); // Equipa uma manopla no in�cio + Alterar para + currentWeapon, para que seja possivel n�o iniciar SEMPRE com a manopla.
    }

    void Update()
    {
        if (manaBar != null)
        {
            manaBar.fillAmount = Mathf.Clamp(mana / maxMana, 0, 1);
        }
    }

    void EquipWeapon(WeaponScript newWeapon)
    {
        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance); // Destrua a arma atual se existir
        }

        // Crie uma nova inst�ncia da arma e a anexe � m�o do personagem
        currentWeaponInstance = Instantiate(newWeapon.weaponPrefab, handTransform.position, handTransform.rotation, handTransform);
    }

    public void UseMana(float amount)
    {
        mana -= amount;
        UpdateManaBar();

        if (mana <= 0)
        { Debug.Log("Cabo mana, faz alguma coisa!"); }
    }

    void UpdateManaBar()
    {
        if (manaBar != null)
        {
            manaBar.fillAmount = Mathf.Clamp(mana / maxMana, 0, 1);
        }
    }

    // Sobrescreva a fun��o TakeDamage
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount); // Chame a fun��o TakeDamage da classe base
        animator.Play("GetHit"); // Ative a anima��o de receber dano
    }
}
