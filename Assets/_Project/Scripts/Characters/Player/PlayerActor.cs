using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerActor : Actor, IExternalPullTarget
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
    private PlayerOnHitEffects onHitEffects;
    private RunBoons _runBoons;
    // R8.6: controllers whose StanceBroken signal this player is watching, mapped to the exact
    // handler delegate subscribed for each, so every subscription is added once and removed on
    // teardown (no cross-run leak). StanceBroken is Action<Vector3> (break point only), so the
    // per-controller handler is a closure that captures its controller to resolve the owning Actor.
    private readonly Dictionary<CombatReactionController, System.Action<Vector3>> _watchedStanceControllers =
        new Dictionary<CombatReactionController, System.Action<Vector3>>();
    public WeaponRunModifiers RunModifiers
    {
        get
        {
            if (!_runBoons) TryGetComponent(out _runBoons);
            return _runBoons && _runBoons.isActiveAndEnabled && CurrentWeapon == _runBoons.RunWeapon
                ? _runBoons.WeaponModifiers : null;
        }
    }

    /// <summary>
    /// Per-run combat-event bus (R8), owned by <see cref="RunBoons"/>. Resolved through the same
    /// run-scoped reference used for <see cref="RunModifiers"/> (RunBoons ownership, no scene
    /// lookups). Null outside an active run so raise-sites must null-guard before use.
    /// </summary>
    public HookBus Hooks
    {
        get
        {
            if (!_runBoons) TryGetComponent(out _runBoons);
            return _runBoons && _runBoons.isActiveAndEnabled ? _runBoons.Hooks : null;
        }
    }
    private float baseMaxHealth;
    private float baseMaxMana;

    /// <summary>Per-run on-hit status effects (burn/chill) applied to every enemy this player damages.</summary>
    public PlayerOnHitEffects OnHitEffects =>
        onHitEffects ? onHitEffects : (onHitEffects = GetComponent<PlayerOnHitEffects>() ?? gameObject.AddComponent<PlayerOnHitEffects>());

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
        if (!TryGetComponent<PauseMenuUI>(out _)) gameObject.AddComponent<PauseMenuUI>();

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
        // Skill damage is scheduled explicitly; legacy clip events belong only to basic attacks.
        if (abilityHolder && abilityHolder.IsCasting) return;
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
        if (!targetActor || targetActor == this || targetActor.IsDead) return false;

        if (damageOverride <= 0f && hitbox && hitbox.TryGetComponent(out HitboxDamage hbDamage))
        {
            return hbDamage.TryDamageActor(targetActor);
        }

        float baseDamage = damageOverride > 0f ? damageOverride : weapon ? weapon.attackDamage : 0f;
        AttackDamageRoll roll = RollAttackDamage(baseDamage);
        if (roll.Amount <= 0f) return false;

        DealResolvedAttackDamage(targetActor, roll.Amount, roll.IsCritical);
        return true;
    }

    public bool TryApplyDamage(Actor targetActor, float weaponDamage, float skillMultiplier, float addedDamage)
    {
        if (!targetActor || targetActor == this || targetActor.IsDead) return false;

        AttackDamageRoll roll = RollAttackDamage(weaponDamage, skillMultiplier, addedDamage);
        if (roll.Amount <= 0f) return false;

        DealResolvedAttackDamage(targetActor, roll.Amount, roll.IsCritical);
        return true;
    }

    public void DealResolvedAttackDamage(Actor enemy, float damage)
    {
        DealResolvedAttackDamage(enemy, damage, false);
    }

    public void DealResolvedAttackDamage(Actor enemy, float damage, bool isCritical)
    {
        if (!enemy || enemy.IsDead || enemy is PlayerActor || damage <= 0f) return;
        WeaponRunModifiers modifiers = RunModifiers;
        if (modifiers != null)
            damage *= modifiers.DirectDamageMultiplier(Vector3.Distance(transform.position, enemy.transform.position),
                enemy.health / Mathf.Max(1f, enemy.maxHealth), health / Mathf.Max(1f, maxHealth),
                (enemy.GetComponent<BurnStatus>() ? 1 : 0) + (enemy.GetComponent<ChillStatus>() ? 1 : 0));
        float before = enemy.health;
        enemy.TakeDamage(damage);
        float dealt = before - enemy.health;
        if (dealt > 0f && modifiers != null) RestoreMana(2f * modifiers.Rank(WeaponBoon.Siphon));
        if (dealt > 0f && onHitEffects && onHitEffects.HasAnyEffect)
            onHitEffects.ApplyTo(enemy, dealt);

        // R8.2/R8.3: surface crit and kill through the per-run HookBus owned by RunBoons. The bus
        // may be absent outside a run (bus null), so every raise is null-guarded; OnKill is deduped
        // inside the bus so repeat notifications for the same enemy fire the hook only once.
        if (dealt > 0f)
        {
            HookBus hooks = Hooks;
            if (hooks != null)
            {
                if (isCritical) hooks.RaiseCrit(enemy, dealt);
                if (enemy.IsDead) hooks.RaiseKill(enemy);
            }
        }
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

    public int TryApplyAreaDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize, AreaHitShape hitShape, float sphereRadius, LayerMask targetLayers, float weaponDamage, float skillMultiplier, float addedDamage, HitReactionRequest? reactionRequest, bool showEffect = true, Color? effectColor = null)
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

            if (showEffect && showAttackAreaSwoosh)
                AttackAreaSwoosh.SpawnArea(center, normalizedForward, Vector3.one * resolvedRadius * 2,
                    AreaHitShape.Sphere, resolvedRadius, transform.position.y,
                    effectColor ?? attackAreaSwooshColor, Mathf.Max(.25f, attackAreaSwooshDuration));

            hits = Physics.OverlapSphere(center, resolvedRadius, mask, QueryTriggerInteraction.Collide);
        }
        else
        {
            Vector3 resolvedBoxSize = ResolveAttackBoxSize(range, boxSize);
            center = origin + normalizedForward * (range * 0.5f);
            center.y += resolvedBoxSize.y * 0.5f;

            if (showEffect && showAttackAreaSwoosh)
                AttackAreaSwoosh.SpawnArea(center, normalizedForward, resolvedBoxSize, AreaHitShape.Box, 0,
                    transform.position.y, effectColor ?? attackAreaSwooshColor, Mathf.Max(.25f, attackAreaSwooshDuration));
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

    /// <summary>
    /// Public entry point for damage sources outside the area-attack pipeline (e.g. the animated
    /// basic-swing hitbox) to apply a crowd-control reaction to a victim. The hit direction, when
    /// unset, defaults to pushing the target away from this attacker.
    /// </summary>
    public void ApplyHitReactionTo(Actor targetActor, HitReactionType reactionType, HitStrength strength,
        float stanceDamage, StanceBreakEffect breakEffect, float pushDistance)
    {
        if (!targetActor || targetActor == this) return;

        Vector3 away = targetActor.transform.position - transform.position;
        away.y = 0f;
        Vector3 direction = away.sqrMagnitude > Mathf.Epsilon ? away.normalized : transform.forward;

        var request = new HitReactionRequest(this, targetActor.transform.position, direction,
            reactionType, strength, stanceDamage, breakEffect, pushDistance);
        ApplyHitReaction(targetActor, request, targetActor.transform.position, direction);
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

        // R8.6: this reaction is player-caused, so watch the controller's StanceBroken signal and
        // surface a stance break through the per-run HookBus. Subscribed once per controller (the
        // handler resolves the owning Actor from the controller's GameObject) and unsubscribed on
        // teardown so a break never fires into a stale handler after the run ends.
        WatchStanceBreaks(reactionController);

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
            request.StanceDamage,
            request.BreakEffect,
            request.PushDistance,
            request.StunDuration,
            request.KnockUpHeight,
            request.KnockbackDistance));
    }

    /// <summary>
    /// Subscribes once to a controller's <see cref="CombatReactionController.StanceBroken"/> so a
    /// player-caused break is surfaced through the per-run <see cref="HookBus"/> (R8.6). Idempotent
    /// per controller; the matching unsubscribe happens in <see cref="OnDestroy"/>.
    /// </summary>
    private void WatchStanceBreaks(CombatReactionController controller)
    {
        if (!controller || _watchedStanceControllers.ContainsKey(controller)) return;
        System.Action<Vector3> handler = _ => RaiseStanceBreakFor(controller);
        _watchedStanceControllers.Add(controller, handler);
        controller.StanceBroken += handler;
    }

    /// <summary>
    /// Resolves the owning <see cref="Actor"/> for a broken stance and raises OnStanceBreak(Actor)
    /// on the per-run bus. The controller's <c>StanceBroken</c> payload is the break point (unused
    /// here); we look the Actor up from the controller's GameObject. Null-guarded — the bus is
    /// absent outside a run.
    /// </summary>
    private void RaiseStanceBreakFor(CombatReactionController controller)
    {
        if (!controller) return;
        Actor broken = controller.GetComponentInParent<Actor>();
        if (!broken) broken = controller.GetComponentInChildren<Actor>();
        if (!broken) return;
        Hooks?.RaiseStanceBreak(broken);
    }

    // --- Hooker external pull (Requirement 12) ---------------------------------------------------
    // The player is NavMeshAgent-driven (CharControlScript point-and-click). A hook connect displaces
    // the player by suspending that controller's steering and moving the agent toward the Hooker over
    // a bounded <=1.5s, then always restoring control. The bound/tolerance decisions live in the pure
    // HookPullModel so task 9.2 can property-test them without a scene; this MonoBehaviour only maps
    // those decisions onto Unity (suspend controller, agent.Warp, restore).

    private NavMeshAgent _agent;
    private CharControlScript _movementControl;
    private Coroutine _externalPullCoroutine;

    private NavMeshAgent Agent => _agent ? _agent : (_agent = GetComponent<NavMeshAgent>());
    private CharControlScript MovementControl =>
        _movementControl ? _movementControl : (_movementControl = GetComponent<CharControlScript>());

    /// <summary>True while an external pull (a Hooker hook) is displacing this player.</summary>
    public bool IsBeingPulled => _externalPullCoroutine != null;

    /// <summary>
    /// Begins a bounded pull of the player toward <paramref name="source"/> (the Hooker), suspending
    /// <see cref="CharControlScript"/> agent steering, moving the player's <see cref="NavMeshAgent"/>
    /// toward the source over <c>min(maxDuration, 1.5s)</c> using <c>agent.Warp</c> (never a raw
    /// transform write, R20.1), then restoring control. Control is always restored via cleanup even if
    /// the source dies mid-pull (R12.6), and the pull ends early if the source becomes control-locked
    /// (R12.7). A pull already in progress is replaced so a fresh hook connect never stacks pulls.
    /// </summary>
    /// <param name="source">The Hooker the player is pulled toward.</param>
    /// <param name="maxDuration">Upper bound on the pull duration in seconds (≤ 1.5s per R12.3/R12.6).</param>
    public void BeginExternalPull(Transform source, float maxDuration)
    {
        if (IsDead || source == null || !isActiveAndEnabled) return;

        // Replace any running pull so a second connect restarts cleanly (restores control first).
        if (_externalPullCoroutine != null)
        {
            StopCoroutine(_externalPullCoroutine);
            _externalPullCoroutine = null;
            RestoreMovementControl();
        }

        _externalPullCoroutine = StartCoroutine(ExternalPullRoutine(source, maxDuration));
    }

    private IEnumerator ExternalPullRoutine(Transform source, float maxDuration)
    {
        var model = new HookPullModel(maxDuration);
        // Resolve the source's control-lock state once; a Hooker owns a CombatReactionController.
        CombatReactionController sourceReaction = source ? source.GetComponentInParent<CombatReactionController>() : null;

        NavMeshAgent agent = Agent;
        Vector3 start = agent && agent.enabled && agent.isOnNavMesh ? agent.nextPosition : transform.position;

        SuspendMovementControl();
        try
        {
            float elapsed = 0f;
            while (true)
            {
                bool sourceAlive = source != null;
                bool sourceLocked = sourceAlive && sourceReaction && sourceReaction.IsControlLocked;
                PullStep step = model.Evaluate(elapsed, sourceAlive, sourceLocked);
                if (step.Ended) break;

                // Recompute the pull toward the source's live position; it may have moved.
                float fraction = model.Fraction(elapsed);
                Vector3 target = HookPullModel.PositionAt(start, source.position, fraction);
                MoveAgentTo(target);

                yield return null;
                elapsed += Time.deltaTime;
            }
        }
        finally
        {
            // ALWAYS restore control, even if the source died mid-pull or the coroutine was stopped
            // (R12.6/R12.7). Reached on normal completion, early interrupt, and StopCoroutine.
            _externalPullCoroutine = null;
            RestoreMovementControl();
        }
    }

    /// <summary>
    /// Moves the player's agent to <paramref name="target"/> keeping it on the NavMesh. Uses
    /// <c>agent.Warp</c> so displacement flows through the NavMeshAgent rather than a raw
    /// <c>transform.position</c> write (R20.1); falls back to sampling a nearby NavMesh point so the
    /// pull cannot warp the player off the mesh.
    /// </summary>
    private void MoveAgentTo(Vector3 target)
    {
        NavMeshAgent agent = Agent;
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            Vector3 destination = target;
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, HookPullModel.StopDistance + 1f, agent.areaMask))
                destination = hit.position;
            agent.Warp(destination);
        }
    }

    /// <summary>
    /// Suspends point-and-click steering for the duration of the pull. Disabling
    /// <see cref="CharControlScript"/> stops its Update-driven <c>SetDestination</c>/follow and, via
    /// its <c>OnDisable</c>, cancels any in-flight combo and clears the current target so the player
    /// is not simultaneously steered by two sources. The agent's own path is reset so no queued
    /// destination fights the warp.
    /// </summary>
    private void SuspendMovementControl()
    {
        CharControlScript control = MovementControl;
        if (control) control.enabled = false;

        NavMeshAgent agent = Agent;
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Returns movement control to the player after a pull ends (R12.6/R12.7). Re-enables
    /// <see cref="CharControlScript"/> so click-to-move resumes and clears any residual agent velocity
    /// so the player does not drift once control is handed back. Idempotent — safe to call from the
    /// cleanup path even when control was never suspended.
    /// </summary>
    private void RestoreMovementControl()
    {
        NavMeshAgent agent = Agent;
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }

        CharControlScript control = MovementControl;
        if (control && !control.enabled && isActiveAndEnabled && !IsDead) control.enabled = true;
    }

    /// <summary>
    /// Removes every StanceBroken subscription this player added (R8.6) so a break can never fire
    /// into a stale handler after the player object is torn down at run end. Also ends any in-flight
    /// external pull so its cleanup restores control before teardown.
    /// </summary>
    private void OnDestroy()
    {
        if (_externalPullCoroutine != null)
        {
            StopCoroutine(_externalPullCoroutine);
            _externalPullCoroutine = null;
        }

        foreach (var pair in _watchedStanceControllers)
            if (pair.Key) pair.Key.StanceBroken -= pair.Value;
        _watchedStanceControllers.Clear();
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
        if (Time.timeScale <= 0f) return;
        float resolvedAmount = Stats.ReduceIncomingDamage(amount);
        base.TakeDamage(resolvedAmount);
        if (animator && !(abilityHolder && abilityHolder.IsCasting))
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
