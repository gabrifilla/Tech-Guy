using UnityEngine;
using UnityEngine.UI;

public class PlayerActor : Actor
{
    public float mana;
    public float maxMana { get; private set; }

    public Image manaBar;

    [Header("Weapon")]
    [SerializeField] public Transform handTransform;
    [SerializeField] private GameObject currentWeaponInstance;
    [SerializeField] private GameObject hitbox;
    [SerializeField] private WeaponScript startingWeapon;
    public WeaponScript weapon;

    public WeaponScript CurrentWeapon => weapon;
    public GameObject CurrentHitbox => hitbox;
    public Transform HandTransform => handTransform;

    private Animator animator;

    public override void Awake()
    {
        base.Awake();
        maxMana = mana;

        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(true);
        }

        if (manaBar != null)
        {
            manaBar.gameObject.SetActive(true);
        }

        animator = GetComponent<Animator>();

        WeaponScript weaponToEquip = startingWeapon != null
            ? startingWeapon
            : Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
        EquipWeapon(weaponToEquip);
    }

    private void Update()
    {
        UpdateManaBar();
        SyncEquippedObject(hitbox);
        SyncEquippedObject(currentWeaponInstance);
    }

    public void EquipWeapon(WeaponScript newWeapon)
    {
        if (newWeapon == null) return;
        if (handTransform == null)
        {
            Debug.LogError($"{nameof(PlayerActor)} requires a hand transform to equip weapons.", this);
            return;
        }

        if (currentWeaponInstance != null)
        {
            Destroy(currentWeaponInstance);
        }

        if (hitbox != null)
        {
            Destroy(hitbox);
        }

        weapon = newWeapon;
        currentWeaponInstance = null;
        hitbox = null;

        if (newWeapon.weaponPrefab != null)
        {
            currentWeaponInstance = Instantiate(newWeapon.weaponPrefab, handTransform.position, handTransform.rotation, handTransform);
        }

        if (newWeapon.hitbox != null)
        {
            hitbox = Instantiate(newWeapon.hitbox, handTransform.position, handTransform.rotation, handTransform);
            hitbox.SetActive(false);
            ConfigureHitbox(hitbox, newWeapon);
        }
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

    public void UseMana(float amount)
    {
        mana -= amount;
        UpdateManaBar();

        if (mana <= 0)
        {
            Debug.Log("Mana depleted.");
        }
    }

    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);
        if (animator != null)
        {
            animator.Play("GetHit");
        }
    }

    private void SyncEquippedObject(GameObject equippedObject)
    {
        if (equippedObject == null || handTransform == null || !equippedObject.activeSelf) return;

        equippedObject.transform.position = handTransform.position;
        equippedObject.transform.rotation = handTransform.rotation;
    }

    private void ConfigureHitbox(GameObject hitboxInstance, WeaponScript newWeapon)
    {
        if (hitboxInstance == null || newWeapon == null) return;

        HitboxDamage hbDamage = hitboxInstance.GetComponent<HitboxDamage>();
        if (hbDamage == null)
        {
            hbDamage = hitboxInstance.AddComponent<HitboxDamage>();
        }

        hbDamage.Configure(this, newWeapon.attackDamage, newWeapon.HitEffectResourcePath);

        Collider[] colliders = hitboxInstance.GetComponentsInChildren<Collider>();
        if (colliders.Length == 0)
        {
            Debug.LogWarning("Hitbox has no collider; damage will not trigger.", hitboxInstance);
        }
        else
        {
            foreach (Collider col in colliders)
            {
                col.isTrigger = true;
            }
        }

        Rigidbody rb = hitboxInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = hitboxInstance.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void UpdateManaBar()
    {
        if (manaBar != null)
        {
            manaBar.fillAmount = maxMana > 0f ? Mathf.Clamp01(mana / maxMana) : 0f;
        }
    }
}
