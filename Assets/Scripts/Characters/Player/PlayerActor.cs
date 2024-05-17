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
    public Transform handTransform; // Transform da mão do personagem
    private GameObject currentWeaponInstance; // Instância da arma atualmente equipada

    public WeaponScript weapon;

    private Animator animator; // Adicione uma referência ao componente Animator


    public override void Awake()
    {
        base.Awake();
        maxMana = mana;

        healthBar.gameObject.SetActive(true); // A barra de vida do jogador está sempre visível
        manaBar.gameObject.SetActive(true); // A barra de mana do jogador está sempre visível
        animator = GetComponent<Animator>(); // Inicialize a referência ao componente Animator

        EquipWeapon(Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet")); // Equipa uma manopla no início + Alterar para + currentWeapon, para que seja possivel não iniciar SEMPRE com a manopla.
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

        // Crie uma nova instância da arma e a anexe à mão do personagem
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

    // Sobrescreva a função TakeDamage
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount); // Chame a função TakeDamage da classe base
        animator.Play("GetHit"); // Ative a animação de receber dano
    }
}
