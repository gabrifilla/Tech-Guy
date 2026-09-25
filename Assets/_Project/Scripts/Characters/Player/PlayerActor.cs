using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerActor : Actor
{
    public float mana;
    public float maxMana { get; private set; }

    public Image manaBar;
    [SerializeField, Min(0f)] private float _manaRegenerationPercentPerSecond = 4f;
    [SerializeField, Min(0f)] private float _manaRegenerationDelay = 1.25f;
    private float _manaRegenerationAt;
    public event System.Action<PlayerActor> ManaChanged;

    [Header("Weapon")]
    [SerializeField] public Transform handTransform;
    [SerializeField] private GameObject currentWeaponInstance;
    [SerializeField] private GameObject hitbox;
    [SerializeField] private WeaponScript startingWeapon;
    public WeaponScript weapon;

    [Header("Attack Area Swoosh")]
    [SerializeField] private bool showAttackAreaSwoosh = true;
    [SerializeField] private Color attackAreaSwooshColor = new Color(1f, 0.85f, 0.25f, 0.36f);
    [SerializeField] private float attackAreaSwooshDuration = 0.18f;
    [SerializeField] private float attackAreaSwooshHeightOffset = 0.06f;

    [Header("ARPG Stats")]
    [SerializeField] private PlayerArpgStats stats = new PlayerArpgStats();

    public WeaponScript CurrentWeapon => weapon;
    public GameObject CurrentHitbox => hitbox;
    public Transform HandTransform => handTransform;
    public PlayerArpgStats Stats
    {
        get
        {
            stats ??= new PlayerArpgStats();
            return stats;
        }
    }

    private Animator animator;
    private AbilityHolder abilityHolder;
    private float baseMaxHealth;
    private float baseMaxMana;

    public override void Awake()
    {
        base.Awake();
        baseMaxHealth = maxHealth;
        baseMaxMana = mana;
        RefreshResourceStats(fillToMax: true);

        if (healthBar)
        {
            healthBar.gameObject.SetActive(true);
        }

        if (manaBar)
        {
            manaBar.gameObject.SetActive(true);
        }

        animator = GetComponent<Animator>();
        abilityHolder = GetComponent<AbilityHolder>();

        WeaponScript weaponToEquip = startingWeapon
            ? startingWeapon
            : weapon
                ? weapon
                : Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
        EquipWeapon(WeaponLoadout.LoadSelected() ?? weaponToEquip);
    }

    private void Update()
    {
        if (!IsDead && Time.time >= _manaRegenerationAt && mana < maxMana)
            RestoreMana(maxMana * _manaRegenerationPercentPerSecond * 0.01f * Time.deltaTime);
        UpdateManaBar();
        SyncEquippedObject(hitbox);
        SyncEquippedObject(currentWeaponInstance);
    }

    public WeaponScript EquipWeapon(WeaponScript newWeapon)
    {
        if (!newWeapon) return null;
        if (!handTransform)
        {
            Debug.LogError($"{nameof(PlayerActor)} requires a hand transform to equip weapons.", this);
            return null;
        }

        WeaponScript previousWeapon = weapon;
        if (TryGetComponent(out ArsenalCombat arsenal)) arsenal.Cancel();
        if (TryGetComponent(out CharControlScript control)) control.CancelCombo();

        if (currentWeaponInstance)
        {
            Destroy(currentWeaponInstance);
        }

        if (hitbox)
        {
            Destroy(hitbox);
        }

        weapon = newWeapon;
        currentWeaponInstance = null;
        hitbox = null;

        if (newWeapon.weaponPrefab)
        {
            currentWeaponInstance = Instantiate(newWeapon.weaponPrefab, handTransform.position, handTransform.rotation, handTransform);
        }

        if (newWeapon.hitbox)
        {
            hitbox = Instantiate(newWeapon.hitbox, handTransform.position, handTransform.rotation, handTransform);
            hitbox.SetActive(false);
            ConfigureHitbox(hitbox, newWeapon);
        }

        return previousWeapon;
    }

    public void ActivateHitbox()
    {
        if (hitbox)
        {
            hitbox.SetActive(true);
        }
    }

    public void DeactivateHitbox()
    {
        if (hitbox)
        {
            hitbox.SetActive(false);
        }
    }

    public bool TryApplyDamage(Actor targetActor)
    {
        return TryApplyDamage(targetActor, -1f);
    }

    public bool TryApplyDamage(Actor targetActor, float damageOverride)
    {
        if (!targetActor || targetActor == this) return false;

        if (damageOverride <= 0f && hitbox && hitbox.TryGetComponent(out HitboxDamage hbDamage))
        {
            return hbDamage.TryDamageActor(targetActor);
        }

        float baseDamage = damageOverride > 0f ? damageOverride : weapon ? weapon.attackDamage : 0f;
        float damage = RollAttackDamage(baseDamage).Amount;
        if (damage <= 0f) return false;

        targetActor.TakeDamage(damage);
        return true;
    }

    public bool TryApplyDamage(Actor targetActor, float weaponDamage, float skillMultiplier, float addedDamage)
    {
        if (!targetActor || targetActor == this) return false;

        float damage = RollAttackDamage(weaponDamage, skillMultiplier, addedDamage).Amount;
        if (damage <= 0f) return false;

        targetActor.TakeDamage(damage);
        return true;
    }

    public AttackDamageRoll RollAttackDamage(float weaponDamage, float skillMultiplier = 1f, float addedDamage = 0f)
    {
        return Stats.RollAttackDamage(weaponDamage, skillMultiplier, addedDamage);
    }

    public float GetAttackInterval(float baseInterval)
    {
        return baseInterval / Stats.AttackSpeedMultiplier;
    }

    public int TryApplyAreaDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize, LayerMask targetLayers)
    {
        return TryApplyAreaDamage(origin, forward, range, boxSize, targetLayers, -1f);
    }

    public int TryApplyAreaDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize, LayerMask targetLayers, float damageOverride)
    {
        return TryApplyAreaDamage(origin, forward, range, boxSize, targetLayers, damageOverride, 1f, 0f);
    }

    public int TryApplyAreaDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize, LayerMask targetLayers, float weaponDamage, float skillMultiplier, float addedDamage)
    {
        return TryApplyAreaDamage(origin, forward, range, boxSize, targetLayers, weaponDamage, skillMultiplier, addedDamage, null);
    }

    public int TryApplyAreaDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize, LayerMask targetLayers, float weaponDamage, float skillMultiplier, float addedDamage, HitReactionRequest? reactionRequest)
    {
        return TryApplyAreaDamage(origin, forward, range, boxSize, AreaHitShape.Box, 0f, targetLayers, weaponDamage, skillMultiplier, addedDamage, reactionRequest);
    }

    public int TryApplyAreaDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize, AreaHitShape hitShape, float sphereRadius, LayerMask targetLayers, float weaponDamage, float skillMultiplier, float addedDamage, HitReactionRequest? reactionRequest, bool showEffect = true)
    {
        if (range <= 0f || forward.sqrMagnitude <= Mathf.Epsilon) return 0;

        Vector3 normalizedForward = forward.normalized;
        int mask = targetLayers.value != 0 ? targetLayers.value : Physics.DefaultRaycastLayers;
        Vector3 center;
        Collider[] hits;

        if (hitShape == AreaHitShape.Sphere)
        {
            float resolvedRadius = ResolveAttackSphereRadius(range, boxSize, sphereRadius);
            center = origin + normalizedForward * range;
            center.y += resolvedRadius;

            Vector3 effectOrigin = origin + normalizedForward * Mathf.Max(0f, range - resolvedRadius);
            Vector3 effectSize = Vector3.one * (resolvedRadius * 2f);
            if (showEffect) ShowAttackAreaSwoosh(effectOrigin, normalizedForward, resolvedRadius * 2f, effectSize);

            hits = Physics.OverlapSphere(center, resolvedRadius, mask, QueryTriggerInteraction.Collide);
        }
        else
        {
            Vector3 resolvedBoxSize = ResolveAttackBoxSize(range, boxSize);
            if (showEffect) ShowAttackAreaSwoosh(origin, normalizedForward, range, resolvedBoxSize);
            center = origin + normalizedForward * (range * 0.5f);
            center.y += resolvedBoxSize.y * 0.5f;

            hits = Physics.OverlapBox(center, resolvedBoxSize * 0.5f, Quaternion.LookRotation(normalizedForward), mask, QueryTriggerInteraction.Collide);
        }

        int damageCount = 0;
        HashSet<Actor> resolvedActors = new HashSet<Actor>();
        List<Actor> damagedActors = new List<Actor>();
        foreach (Collider hit in hits)
        {
            Actor actor = ResolveActor(hit);
            if (!actor || actor == this || !resolvedActors.Add(actor)) continue;

            bool damaged = weaponDamage <= 0f && Mathf.Approximately(skillMultiplier, 1f) && Mathf.Approximately(addedDamage, 0f)
                ? TryApplyDamage(actor)
                : TryApplyDamage(actor, weaponDamage, skillMultiplier, addedDamage);
            if (damaged)
            {
                ApplyHitReaction(actor, reactionRequest, hit.ClosestPoint(center), normalizedForward);
                damageCount++;
                damagedActors.Add(actor);
            }
        }

        if (damageCount > 0 && abilityHolder)
        {
            abilityHolder.NotifyAttackHits(this, damagedActors);
        }

        return damageCount;
    }

    private void ApplyHitReaction(Actor targetActor, HitReactionRequest? reactionRequest, Vector3 hitPoint, Vector3 fallbackDirection)
    {
        if (!targetActor || !reactionRequest.HasValue) return;

        CombatReactionController reactionController = targetActor.GetComponentInParent<CombatReactionController>();
        if (!reactionController)
        {
            reactionController = targetActor.GetComponentInChildren<CombatReactionController>();
        }

        if (!reactionController) return;

        HitReactionRequest request = reactionRequest.Value;
        Vector3 hitDirection = request.HitDirection.sqrMagnitude > Mathf.Epsilon
            ? request.HitDirection
            : fallbackDirection;

        reactionController.ApplyReaction(new HitReactionRequest(
            this,
            hitPoint,
            hitDirection,
            request.ReactionType,
            request.Strength,
            request.PoiseDamage,
            request.StunDuration,
            request.KnockbackForce,
            request.LaunchForce,
            request.CanAirJuggle,
            request.CanRagdoll));
    }

    public void UseMana(float amount)
    {
        TrySpendMana(amount);
    }

    public bool HasMana(float amount) => !IsDead && !float.IsNaN(amount) &&
        !float.IsInfinity(amount) && mana >= Mathf.Max(0f, amount);

    public bool TrySpendMana(float amount)
    {
        if (!HasMana(amount)) return false;
        amount = Mathf.Max(0f, amount);
        if (amount == 0f) return true;
        mana = Mathf.Clamp(mana - amount, 0f, maxMana);
        _manaRegenerationAt = Time.time + _manaRegenerationDelay;
        UpdateManaBar();
        ManaChanged?.Invoke(this);
        return true;
    }

    public void RestoreMana(float amount)
    {
        if (IsDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
        mana = Mathf.Clamp(mana + amount, 0f, maxMana);
        UpdateManaBar();
        ManaChanged?.Invoke(this);
    }

    public override void TakeDamage(float amount)
    {
        float resolvedAmount = Stats.ReduceIncomingDamage(amount);
        base.TakeDamage(resolvedAmount);
        if (animator)
        {
            animator.Play("GetHit");
        }
    }

    public void RefreshResourceStats(bool fillToMax = false)
    {
        float healthRatio = maxHealth > 0f ? Mathf.Clamp01(health / maxHealth) : 1f;
        float manaRatio = maxMana > 0f ? Mathf.Clamp01(mana / maxMana) : 1f;

        maxHealth = baseMaxHealth + Stats.MaxHealthBonus;
        maxMana = baseMaxMana + Stats.MaxManaBonus;

        if (fillToMax)
        {
            health = maxHealth;
            mana = maxMana;
        }
        else
        {
            health = Mathf.Min(maxHealth, maxHealth * healthRatio);
            mana = Mathf.Min(maxMana, maxMana * manaRatio);
        }

        UpdateHealthBar();
        UpdateManaBar();
        ManaChanged?.Invoke(this);
    }

    private void SyncEquippedObject(GameObject equippedObject)
    {
        if (!equippedObject || !handTransform || !equippedObject.activeSelf) return;

        equippedObject.transform.position = handTransform.position;
        equippedObject.transform.rotation = handTransform.rotation;
    }

    private void ConfigureHitbox(GameObject hitboxInstance, WeaponScript newWeapon)
    {
        if (!hitboxInstance || !newWeapon) return;

        if (!hitboxInstance.TryGetComponent(out HitboxDamage hbDamage))
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
        if (!rb)
        {
            rb = hitboxInstance.AddComponent<Rigidbody>();
        }

        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void UpdateManaBar()
    {
        if (manaBar)
        {
            manaBar.fillAmount = maxMana > 0f ? Mathf.Clamp01(mana / maxMana) : 0f;
        }
    }

    private static Vector3 ResolveAttackBoxSize(float range, Vector3 configuredSize)
    {
        float width = configuredSize.x > 0f ? configuredSize.x : 2f;
        float height = configuredSize.y > 0f ? configuredSize.y : 2f;
        float depth = configuredSize.z > 0f ? configuredSize.z : range;
        return new Vector3(width, height, depth);
    }

    private static float ResolveAttackSphereRadius(float range, Vector3 configuredSize, float configuredRadius)
    {
        if (configuredRadius > 0f) return configuredRadius;

        if (configuredSize != Vector3.zero)
        {
            return Mathf.Max(0.1f, Mathf.Max(configuredSize.x, configuredSize.y, configuredSize.z) * 0.5f);
        }

        return Mathf.Max(0.1f, range * 0.5f);
    }

    private void ShowAttackAreaSwoosh(Vector3 origin, Vector3 forward, float range, Vector3 boxSize)
    {
        if (!showAttackAreaSwoosh) return;

        Vector3 spawnOrigin = origin + Vector3.up * attackAreaSwooshHeightOffset;
        AttackAreaSwoosh.Spawn(spawnOrigin, forward, range, boxSize, attackAreaSwooshColor, attackAreaSwooshDuration);
    }

    private static Actor ResolveActor(Collider collider)
    {
        if (!collider) return null;

        Actor actor = collider.GetComponentInParent<Actor>();
        if (actor) return actor;

        actor = collider.GetComponentInChildren<Actor>();
        if (actor) return actor;

        Interactable interactable = collider.GetComponentInParent<Interactable>();
        if (interactable && interactable.myActor)
        {
            return interactable.myActor;
        }

        interactable = collider.GetComponentInChildren<Interactable>();
        return interactable ? interactable.myActor : null;
    }
}
