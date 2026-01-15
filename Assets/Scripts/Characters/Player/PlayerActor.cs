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
    [SerializeField] public Transform handTransform; // Transform da mão do personagem
    [SerializeField] private GameObject currentWeaponInstance; // Instância da arma atualmente equipada
    [SerializeField] GameObject hitbox; // Nome do prefab do efeito de ataque


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

        if (hitbox.activeSelf)
        {
            hitbox.transform.position = handTransform.position;
            hitbox.transform.rotation = handTransform.rotation;
        }

        if (currentWeaponInstance.activeSelf)
        {
            currentWeaponInstance.transform.position = handTransform.position;
            currentWeaponInstance.transform.rotation = handTransform.rotation;
        }
    }

    void EquipWeapon(WeaponScript newWeapon)
    {
        if (newWeapon == null) return;

        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance); // Destrua a arma atual se existir
        }

        weapon = newWeapon;

        // Crie uma nova instância da arma e a anexe à mão do personagem
        currentWeaponInstance = Instantiate(newWeapon.weaponPrefab, handTransform.position, handTransform.rotation, handTransform);
        hitbox = Instantiate(newWeapon.hitbox, handTransform.position, handTransform.rotation, handTransform);
        hitbox.SetActive(false);
        ConfigureHitbox(hitbox, newWeapon);
    }

    private void ConfigureHitbox(GameObject hitboxInstance, WeaponScript newWeapon)
    {
        if (hitboxInstance == null || newWeapon == null) return;

        HitboxDamage hbDamage = hitboxInstance.GetComponent<HitboxDamage>();
        if (hbDamage == null) hbDamage = hitboxInstance.AddComponent<HitboxDamage>();
        hbDamage.owner = this;
        hbDamage.damage = newWeapon.attackDamage;
        hbDamage.hitEffectResourcePath = string.IsNullOrEmpty(newWeapon.hitEffectName) ? null : $"Weapons/Melee/Gauntlet/{newWeapon.hitEffectName}";

        Collider[] colliders = hitboxInstance.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            Debug.LogWarning("Hitbox has no collider; damage will not trigger.");
        }
        else
        {
            foreach (Collider col in colliders)
            {
                col.isTrigger = true;
            }
        }

        Rigidbody rb = hitboxInstance.GetComponent<Rigidbody>();
        if (rb == null) rb = hitboxInstance.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
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

    public void ActivateHitbox()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(true);
        }
    }

    public void DeactivateHitbox()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(false);
        }
    }

    public bool TryApplyDamage(Actor targetActor)
    {
        if (hitbox == null || targetActor == null) return false;

        HitboxDamage hbDamage = hitbox.GetComponent<HitboxDamage>();
        if (hbDamage == null) return false;

        return hbDamage.TryDamageActor(targetActor);
    }
}
