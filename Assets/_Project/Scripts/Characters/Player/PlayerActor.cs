using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class PlayerActor : Actor, IExternalPullTarget
{
    internal RunStatistics RunStatistics { get; set; }
    private ImpactFeedbackScope _feedbackScope;
    private HitStopRunner _reactionHitStop;
    internal bool IsCombatHitStopped => _reactionHitStop && _reactionHitStop.IsActive;
    internal bool HasOpenImpactFeedback => _feedbackScope != null;
    public sealed class FeedbackLease : System.IDisposable
    {
        private readonly PlayerActor _owner;
        private readonly ImpactFeedbackScope _previous;
        private bool _disposed;
        public ImpactFeedbackScope Scope { get; }
        public FeedbackLease(PlayerActor owner, ImpactFeedbackScope previous, ImpactFeedbackScope scope)
        { _owner = owner; _previous = previous; Scope = scope; }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _owner._feedbackScope = _previous;
            Scope.Dispose();
        }
    }
    public FeedbackLease BeginImpactFeedback(float? duration = null, long execution = 0, int impactIndex = -1)
    {
        var previous = _feedbackScope;
        if (previous == null)
        {
            if (!_reactionHitStop) _reactionHitStop = GetComponent<HitStopRunner>() ?? gameObject.AddComponent<HitStopRunner>();
            _feedbackScope = new ImpactFeedbackScope(value => { if (_reactionHitStop) _reactionHitStop.Apply(value); }, duration ?? DefaultCommonHitStop(), execution, impactIndex);
        }
        else _feedbackScope = previous.Child(duration, execution, impactIndex);
        return new FeedbackLease(this, previous, _feedbackScope);
    }
    private float DefaultCommonHitStop()
    {
        // Preserve ordinary Bow/Spear timing. Their explicit impact scopes can still author feedback,
        // and a genuine posture break contributes its own emphasis for any weapon.
        if (!WeaponUsesGauntletBasic(weapon)) return 0f;
        var profiles = weapon.BasicProfile?.HitStopProfiles;
        return profiles != null && profiles.Count > 0 ? profiles[0].ClampedDuration : 0.035f;
    }
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
    private GauntletImpactTransaction _gauntletImpact;
    public bool IsResolvingGauntletImpact => _gauntletImpact != null;
    internal GauntletImpactContext CurrentGauntletImpactContext => _gauntletImpact?.Context;
    public bool IsContextualGauntletBasic => _gauntletImpact != null
        && _gauntletImpact.Context.Slot == GauntletActionSlot.Basic
        && _gauntletImpact.Context.Origin == GauntletImpactOrigin.Direct;

    internal bool CaptureGauntletBreak(Actor target, long sequence, System.Action notification)
    {
        if (_gauntletImpact == null || !_gauntletImpact.IsCollecting || !target || _gauntletImpact.Context.TargetId != EntityId.ToULong(target.GetEntityId())
            || _gauntletImpact.Context.TargetGeneration != target.CombatGeneration) return false;
        _gauntletImpact.ConfirmBreak(sequence);
        _gauntletImpact.Notify(notification);
        return true;
    }
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

    /// <summary>
    /// Per-run Rain Mark registry (R10), owned by <see cref="RunBoons"/>. Resolved through the same
    /// run-scoped reference used for <see cref="RunModifiers"/>/<see cref="Hooks"/> (RunBoons ownership,
    /// no scene lookups). Null outside an active run or when the Bow ultimate boon was never chosen, so
    /// callers must null-guard before use.
    /// </summary>
    public RainMarkRegistry RainMarks
    {
        get
        {
            if (!_runBoons) TryGetComponent(out _runBoons);
            return _runBoons && _runBoons.isActiveAndEnabled ? _runBoons.RainMarks : null;
        }
    }

    /// <summary>
    /// Per-run Edge Strike vulnerability registry (R13), owned by <see cref="RunBoons"/>. Resolved
    /// through the same run-scoped reference used for <see cref="RunModifiers"/>/<see cref="Hooks"/>
    /// (RunBoons ownership, no scene lookups). Null outside an active run or when the Spear edge boon
    /// was never chosen, so callers must null-guard before use.
    /// </summary>
    public EdgeVulnerabilityRegistry EdgeVulnerabilities
    {
        get
        {
            if (!_runBoons) TryGetComponent(out _runBoons);
            return _runBoons && _runBoons.isActiveAndEnabled ? _runBoons.EdgeVulnerabilities : null;
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
        ApplyStartingStatOverrides();
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

    /// <summary>
    /// Applies the player's starting-stat overrides from the central <see cref="CombatBalanceConfig"/>
    /// (R4.1) over the prefab-authored <see cref="PlayerArpgStats"/>/health, running in
    /// <see cref="Awake"/> after <c>baseMaxHealth</c> is captured and before
    /// <see cref="RefreshResourceStats(bool)"/> so the refreshed <c>maxHealth</c> picks up the
    /// override. Semantics (R4.3): a config field of 0 (or omitted) means "keep the authored value",
    /// so each stat is only overridden when its config getter yields a meaningful (&gt; 0) value.
    /// When the config asset is absent, nothing changes (R5.3). The mitigation formula and the shape
    /// of the damage roll are untouched (R4.2); the config getters already clamp, keeping health
    /// strictly positive and armor non-negative (R4.4).
    /// </summary>
    private void ApplyStartingStatOverrides()
    {
        CombatBalanceConfig cfg = CombatBalance.Current;
        if (cfg == null) return;

        // baseDamage: 0/omitted = keep authored (R4.3). StartingBaseDamage returns the authored
        // config value untouched, so only override on a strictly positive value.
        if (cfg.StartingBaseDamage > 0f)
            Stats.baseDamage = cfg.StartingBaseDamage;

        // armor: authored default is 0 and 0 is also the neutral/"keep authored" value, so only
        // override when the config asks for a positive armor. StartingArmor is clamped >= 0.
        if (cfg.StartingArmor > 0f)
            Stats.armor = cfg.StartingArmor;

        // base health: StartingHealth returns 0 for "keep authored" (R4.3), else a clamped > 0 value
        // (R4.4). Feed it into baseMaxHealth so RefreshResourceStats computes maxHealth from it.
        if (cfg.StartingHealth > 0f)
            baseMaxHealth = cfg.StartingHealth;
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

        if (TryGetComponent(out RunBoons activeRun) && !activeRun.TryPrepareManualWeapon(newWeapon, out newWeapon)) return null;

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
        if (previousWeapon != newWeapon && TryGetComponent(out BreakerGauntletCombat gauntletCombat))
            gauntletCombat.Configure(WeaponRunModifiers.Identify(newWeapon) == RunWeaponFamily.Gauntlet ? newWeapon : null);
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

        return ResolveAttackDamage(targetActor, roll.Amount, roll.IsCritical) > 0f;
    }

    public bool TryApplyDamage(Actor targetActor, float weaponDamage, float skillMultiplier, float addedDamage)
    {
        if (!targetActor || targetActor == this || targetActor.IsDead) return false;

        AttackDamageRoll roll = RollAttackDamage(weaponDamage, skillMultiplier, addedDamage);
        if (roll.Amount <= 0f) return false;

        return ResolveAttackDamage(targetActor, roll.Amount, roll.IsCritical) > 0f;
    }

    public void DealResolvedAttackDamage(Actor enemy, float damage)
    {
        DealResolvedAttackDamage(enemy, damage, false);
    }

    public void DealResolvedAttackDamage(Actor enemy, float damage, bool isCritical)
    { ResolveAttackDamage(enemy, damage, isCritical); }

    internal float ResolveAttackDamage(Actor enemy, float damage, bool isCritical)
        => ResolveAttackDamage(enemy, damage, isCritical, null);

    private float ResolveAttackDamage(Actor enemy, float damage, bool isCritical, GauntletImpactTransaction transaction)
    {
        using (BeginImpactFeedback())
        {
        if (!enemy || enemy.IsDead || enemy is PlayerActor || !ReactionNumeric.IsFinite(damage) || damage <= 0f) return 0f;
        TryGetComponent(out ProtocolRuntime protocols);
        ProtocolRootContext protocolRoot = null;
        using (protocols ? protocols.BeginImpactScope(transaction?.Context, out protocolRoot) : null)
        {
        var protocolBefore = protocols ? protocols.CaptureSnapshot(enemy) : default;
        WeaponRunModifiers modifiers = RunModifiers;
        if (modifiers != null)
            damage *= modifiers.DirectDamageMultiplier(Vector3.Distance(transform.position, enemy.transform.position),
                enemy.health / Mathf.Max(1f, enemy.maxHealth), health / Mathf.Max(1f, maxHealth),
                (ProtocolElementQuery.HasBurn(enemy) ? 1 : 0) + (ProtocolElementQuery.HasChill(enemy) ? 1 : 0));
        // R10.2 (RainMark): a direct hit on a marked enemy is amplified by (1 + 0.2*R) while the mark
        // lasts. AmplifierFor returns 1 for unmarked/expired enemies, so this is a clean no-op without the
        // boon or outside the mark. Composes multiplicatively with DirectDamageMultiplier above and writes
        // no asset — the registry only reads its run-scoped mark state.
        RainMarkRegistry rainMarks = RainMarks;
        if (rainMarks != null) damage *= rainMarks.AmplifierFor(enemy);
        // R13.2 (EdgeStrike): a direct hit on an enemy whose vulnerability window is open (opened by a
        // prior edge-of-reach hit) is amplified while the window lasts. AmplifierFor returns 1 for enemies
        // with no open window, so this is a clean no-op without the boon or outside the window. Composes
        // multiplicatively with DirectDamageMultiplier and the RainMark amplifier above and writes no
        // asset — the registry only reads its run-scoped window state.
        EdgeVulnerabilityRegistry edgeVuln = EdgeVulnerabilities;
        if (edgeVuln != null) damage *= edgeVuln.AmplifierFor(enemy);
        var protocolProducer = transaction != null ? ProtocolRuntime.ProducerFor(transaction.Context) :
            protocols ? protocols.ScopedProducer : ProtocolProducer.Direct;
        int protocolDepth = transaction != null ? protocolProducer == ProtocolProducer.Direct ? 0 : 1 :
            protocols ? protocols.ScopedDepth : 0;
        if (protocols && protocolDepth > 0 && (protocolRoot == null || !protocolRoot.ReserveImpact(protocolDepth)))
        { transaction?.RejectForBudget(); return 0f; }
        float beforeProtocolAmplification = damage;
        if (protocols) damage = protocols.AmplifyChild(protocolRoot, protocolProducer, protocolBefore, damage);
        var damageSource = transaction != null ? RunStatistics?.SourceFor(transaction.Context) :
            protocolRoot?.AnalyticsSource ?? RunStatistics?.CurrentSource ?? RunStatistics?.UnknownSource();
        damageSource = damageSource?.WithProducer(protocolProducer.ToString(), transaction?.Context.ProducerId ?? "",
            transaction != null ? transaction.Context.Origin != GauntletImpactOrigin.Direct : protocols && protocols.ScopedAnalyticsSecondary);
        if (damage != beforeProtocolAmplification) damageSource = damageSource?.WithModifier("resonance");
        float dealt = 0f;
        using (enemy.CaptureNextDamage(actual =>
        {
            dealt = actual;
            transaction?.ConfirmHp(actual);
            transaction?.HoldProtocolBatch(protocolRoot);
            _feedbackScope.ConfirmDamage(true);
        }, transaction == null ? null : transaction.Notify, damageSource)) enemy.TakeDamage(damage);
        if (dealt > 0f && modifiers != null) RestoreMana(2f * modifiers.Rank(WeaponBoon.Siphon));
        if (dealt > 0f && protocols)
        {
            float confirmed = dealt;
            var producer = protocolProducer;
            System.Action notification = () => {
                if (protocols) protocols.SubmitConfirmedImpact(protocolRoot, enemy, protocolBefore, confirmed, producer, protocolDepth);
            };
            if (transaction != null) transaction.Notify(notification); else notification();
        }
        else if (dealt > 0f && onHitEffects && onHitEffects.HasAnyEffect)
        {
            float confirmed = dealt;
            if (transaction != null) transaction.Notify(() => { if (onHitEffects) onHitEffects.ApplyTo(enemy, confirmed); });
            else onHitEffects.ApplyTo(enemy, dealt);
        }

        // R8.2/R8.3: surface crit and kill through the per-run HookBus owned by RunBoons. The bus
        // may be absent outside a run (bus null), so every raise is null-guarded; OnKill is deduped
        // inside the bus so repeat notifications for the same enemy fire the hook only once.
        if (dealt > 0f && (!protocols || protocolProducer == ProtocolProducer.Direct))
        {
            HookBus hooks = Hooks;
            if (hooks != null)
            {
                // Direct_Hit: every basic/area/projectile hit that dealt damage routes through here.
                float confirmed = dealt;
                bool killed = protocols ? protocolBefore.IsLethal(dealt) : enemy.IsDead;
                System.Action notification = () =>
                {
                    using (protocols ? protocols.OpenCallbackScope(protocolRoot, protocolDepth) : null)
                    {
                    hooks.RaiseHit(enemy, confirmed);
                    if (isCritical) hooks.RaiseCrit(enemy, confirmed);
                    if (killed) hooks.RaiseKill(enemy);
                    }
                };
                if (transaction != null) transaction.Notify(notification); else notification();
            }
        }

            return dealt;
        }
        }
    }

    /// <summary>
    /// Surfaces a Basic_Attack direct hit on the per-run <see cref="HookBus"/>'s dedicated basic
    /// channel (R3.1/R3.2). Called <b>only</b> from the <c>HitboxDamage</c> path, so the basic
    /// channel never fires for skill hits (R3.3). <paramref name="dealt"/> is the damage actually
    /// applied; nativeLethal preserves the basic's causality when a later Protocol burst kills. Does
    /// NOT re-raise the generic <c>OnHit</c>/<c>OnKill</c> events (R3.4) — <see cref="DealResolvedAttackDamage(Actor,float)"/>
    /// already raised those for this hit. The bus is absent outside a run, so it is null-guarded.
    /// </summary>
    public void RaiseBasicAttackHit(Actor enemy, float dealt, bool? nativeLethal = null)
    {
        if (!enemy || dealt <= 0f) return;
        HookBus hooks = Hooks;
        if (hooks == null) return;
        hooks.RaiseBasicHit(enemy, dealt);
        if (nativeLethal ?? enemy.IsDead) hooks.RaiseBasicKill(enemy);
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
        => ResolveAreaDamage(origin, forward, range, boxSize, hitShape, sphereRadius, targetLayers,
            weaponDamage, skillMultiplier, addedDamage, reactionRequest, showEffect, effectColor, null, null, 0);

    internal int TryApplyGauntletAreaDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize,
        AreaHitShape hitShape, float sphereRadius, LayerMask targetLayers, float weaponDamage,
        float skillMultiplier, float addedDamage, HitReactionRequest? reactionRequest, bool showEffect,
        Color? effectColor, GauntletImpactContext context, GauntletEnergyPolicy policy, int impactIndex)
        => ResolveAreaDamage(origin, forward, range, boxSize, hitShape, sphereRadius, targetLayers,
            weaponDamage, skillMultiplier, addedDamage, reactionRequest, showEffect, effectColor, context, policy, impactIndex);

    internal int TryApplyGauntletBasicDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize,
        LayerMask layers, float damage, HitReactionRequest reaction, bool finisher = false)
    {
        if (IsResolvingGauntletImpact) return 0;
        var combat = GetComponent<BreakerGauntletCombat>() ?? gameObject.AddComponent<BreakerGauntletCombat>();
        combat.Configure(CurrentWeapon);
        var context = combat.BeginBasicExecutionWithFinisher(finisher);
        if (context == null) return 0;
        try { return TryApplyGauntletAreaDamage(origin, forward, range, boxSize, AreaHitShape.Box, 0f,
            layers, damage, 1f, 0f, reaction, true, null, context, combat.EnergyPolicy, 0); }
        finally { combat.EnergyPolicy.Close(context); }
    }

    private int ResolveAreaDamage(Vector3 origin, Vector3 forward, float range, Vector3 boxSize,
        AreaHitShape hitShape, float sphereRadius, LayerMask targetLayers, float weaponDamage,
        float skillMultiplier, float addedDamage, HitReactionRequest? reactionRequest, bool showEffect,
        Color? effectColor, GauntletImpactContext context, GauntletEnergyPolicy policy, int impactIndex)
    {
        TryGetComponent(out ProtocolRuntime areaProtocols);
        using (areaProtocols ? areaProtocols.BeginAreaScope(context) : null)
        using (BeginImpactFeedback())
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
        MomentumStacks momentum = null;
        if (context != null) TryGetComponent(out momentum);
        int momentumRank = context?.Snapshot?.Rank(WeaponBoon.MomentumStrike) ?? 0;
        var damageBatch = Stats.CaptureAttackDamage(
            weaponDamage > 0f ? weaponDamage : weapon ? weapon.attackDamage : 0f,
            skillMultiplier, addedDamage, momentum ? momentum.Modifier : null,
            momentum && momentum.isActiveAndEnabled ? momentum.BatchMultiplier(momentumRank) : 1f);
        int confirmedDirectHp = 0;
        long momentumVersion = momentum ? momentum.ResetVersion : 0;
        HashSet<Actor> resolvedActors = new HashSet<Actor>();
        List<Actor> damagedActors = new List<Actor>();
        if (context != null && context.Slot == GauntletActionSlot.Basic && context.Origin == GauntletImpactOrigin.Direct &&
            reactionRequest.HasValue && reactionRequest.Value.BreakEffect == StanceBreakEffect.Knockback)
        {
            var request = reactionRequest.Value;
            int seismic = context.Snapshot?.Rank(WeaponBoon.SeismicFist) ?? 0;
            if (seismic > 0)
                reactionRequest = new HitReactionRequest(request.Attacker, request.HitPoint, request.HitDirection,
                    request.ReactionType, request.Strength, request.StanceDamage, request.BreakEffect,
                    request.PushDistance, request.StunDuration, request.KnockUpHeight,
                    Mathf.Min(DisplacementTier.KnockbackMax, request.KnockbackDistance * (1f + .3f * seismic)))
                    .WithInterruptContribution(request.InterruptContribution);
        }
        foreach (Collider hit in hits)
        {
            if (context != null && !policy.IsLive(context)) break;
            Actor actor = ResolveActor(hit);
            if (!actor || actor == this || !resolvedActors.Add(actor)) continue;

            bool damaged;
            if (context != null && policy != null)
            {
                var previous = _gauntletImpact;
                var targetContext = context.ForImpact(impactIndex, EntityId.ToULong(actor.GetEntityId()), actor.CombatGeneration);
                using (var transaction = new GauntletImpactTransaction(policy, targetContext, () => _gauntletImpact = previous))
                {
                    _gauntletImpact = transaction;
                    var roll = Stats.RollAttackDamage(damageBatch);
                    float healthBefore = actor.health;
                    float dealt = ResolveAttackDamage(actor, roll.Amount, roll.IsCritical, transaction);
                    damaged = dealt > 0f;
                    if (damaged && policy.ConfirmDirectHpOnce(targetContext))
                    {
                        if (momentum && momentum.ResetVersion != momentumVersion)
                        { momentumVersion = momentum.ResetVersion; confirmedDirectHp = 0; }
                        confirmedDirectHp++;
                    }
                    if (damaged && context.Slot == GauntletActionSlot.Basic && context.Origin == GauntletImpactOrigin.Direct)
                        transaction.Notify(() => RaiseBasicAttackHit(actor, dealt, healthBefore > 0 && dealt >= healthBefore));
                    // Posture confirmation is independent from the HP-immunity channel for this weapon.
                    actor.TryGetComponent(out CombatReactionController postureReaction);
                    float postureBefore = postureReaction ? postureReaction.CurrentStance : 0f;
                    long launchBefore = postureReaction && postureReaction.IsAirborne ? postureReaction.LaunchSequence : 0;
                    if (!transaction.BudgetRejected) ApplyHitReaction(actor, reactionRequest, hit.ClosestPoint(center), normalizedForward);
                    bool postureConfirmed = transaction.HasBreak || postureReaction && postureReaction.CurrentStance < postureBefore;
                    if ((damaged || postureConfirmed) && TryGetComponent(out BreakerGauntletCombat eclipseCombat))
                        eclipseCombat.ConfirmEclipseImpact(targetContext, actor, roll.Amount, reactionRequest?.StanceDamage ?? 0f, launchBefore);
                    if (damaged && TryGetComponent(out ImpactGuardTracker guard) && guard.isActiveAndEnabled)
                        guard.ConfirmBasic(targetContext, actor, transaction);
                }
            }
            else
                damaged = weaponDamage <= 0f && Mathf.Approximately(skillMultiplier, 1f) && Mathf.Approximately(addedDamage, 0f)
                    ? TryApplyDamage(actor) : TryApplyDamage(actor, weaponDamage, skillMultiplier, addedDamage);
            if (damaged)
            {
                if (context == null) ApplyHitReaction(actor, reactionRequest, hit.ClosestPoint(center), normalizedForward);
                damageCount++;
                damagedActors.Add(actor);
            }
        }

        if (momentum && momentumRank > 0 && policy.IsLive(context)) momentum.ConfirmBatch(confirmedDirectHp, momentumVersion);
        if (!abilityHolder) TryGetComponent(out abilityHolder);
        if (damageCount > 0 && abilityHolder)
        {
            abilityHolder.NotifyAttackHits(this, damagedActors);
        }

        return damageCount;

        }
    }

    /// <summary>
    /// Public entry point for damage sources outside the area-attack pipeline (e.g. the animated
    /// basic-swing hitbox) to apply a crowd-control reaction to a victim. The hit direction, when
    /// unset, defaults to pushing the target away from this attacker.
    /// </summary>
    public void ApplyHitReactionTo(Actor targetActor, HitReactionType reactionType, HitStrength strength,
        float stanceDamage, StanceBreakEffect breakEffect, float pushDistance)
    {
        using (BeginImpactFeedback())
        {
        if (!targetActor || targetActor == this) return;

        Vector3 away = targetActor.transform.position - transform.position;
        away.y = 0f;
        Vector3 direction = away.sqrMagnitude > Mathf.Epsilon ? away.normalized : transform.forward;

        var request = new HitReactionRequest(this, targetActor.transform.position, direction,
            reactionType, strength, stanceDamage, breakEffect, pushDistance);
        ApplyHitReaction(targetActor, request, targetActor.transform.position, direction);

        }
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
            request.KnockbackDistance).WithInterruptContribution(request.InterruptContribution));
    }

    public void ApplyHitReactionTo(Actor targetActor, HitReactionRequest request)
    {
        using (BeginImpactFeedback())
        {
        if (!targetActor) return;
        ApplyHitReaction(targetActor, request, targetActor.transform.position, transform.forward);

        }
    }

    internal void ApplyGuardImpact(Actor target, GauntletImpactContext parent, GauntletImpactTransaction transaction,
        float stance, float distance)
    {
        var combat = GetComponent<BreakerGauntletCombat>();
        var child = combat.BeginGuardImpact(parent).ForImpact(parent.ImpactIndex, parent.TargetId, parent.TargetGeneration);
        var previous = _gauntletImpact;
        using (var impact = new GauntletImpactTransaction(combat.EnergyPolicy, child,
            () => _gauntletImpact = previous, transaction))
        {
            _gauntletImpact = impact;
            ApplyHitReactionTo(target, new HitReactionRequest(this, target.transform.position,
                target.transform.position - transform.position, HitReactionType.Stagger, HitStrength.Heavy,
                stance, StanceBreakEffect.Knockback, 0f, knockbackDistance: distance));
        }
    }

    /// <summary>
    /// Subscribes once to a controller's <see cref="CombatReactionController.StanceBroken"/> so a
    /// player-caused break is surfaced through the per-run <see cref="HookBus"/> (R8.6). Idempotent
    /// per controller; the matching unsubscribe happens in <see cref="OnDestroy"/>.
    /// </summary>
    private void WatchStanceBreaks(CombatReactionController controller)
    {
        if (!controller || _watchedStanceControllers.ContainsKey(controller)) return;
        System.Action<Vector3> handler = _ => { if (controller.LastBreakAttacker == this) RaiseStanceBreakFor(controller); };
        _watchedStanceControllers.Add(controller, handler);
        controller.StanceBroken += handler;
    }

    internal void ConfirmStanceBreakFeedback() => _feedbackScope?.ConfirmBreak(true, 0.09f);

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
    internal bool HasExternalPull => _externalPullCoroutine != null;

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
                if (MovementControl && MovementControl.DirectMovementEnabled && IsCombatHitStopped)
                { yield return null; continue; }

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
        if (PlayerNavigationMotor.TryMove(agent, target - transform.position, true, PlayerMovementSource.ExternalPull)) return;
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
    protected override void OnDestroy()
    {
        base.OnDestroy();

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
        => TrySpendMana(amount, null);

    internal bool TrySpendMana(float amount, System.Action<System.Action> publish)
    {
        if (!HasMana(amount)) return false;
        amount = Mathf.Max(0f, amount);
        if (amount == 0f) return true;
        mana = Mathf.Clamp(mana - amount, 0f, maxMana);
        _manaRegenerationAt = Time.time + _manaRegenerationDelay;
        UpdateManaBar();
        if (publish != null) publish(() => ManaChanged?.Invoke(this));
        else ManaChanged?.Invoke(this);
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

    public void RefreshResourceStats(bool fillToMax = false) => RefreshResourceStats(fillToMax, false);

    public void RefreshResourceStatsPreservingAmounts() => RefreshResourceStats(false, true);

    private void RefreshResourceStats(bool fillToMax, bool preserveAbsoluteAmounts)
    {
        float previousHealth = health, previousMana = mana;
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
            health = preserveAbsoluteAmounts ? Mathf.Clamp(previousHealth, 0f, maxHealth) : Mathf.Min(maxHealth, maxHealth * healthRatio);
            mana = preserveAbsoluteAmounts ? Mathf.Clamp(previousMana, 0f, maxMana) : Mathf.Min(maxMana, maxMana * manaRatio);
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

        // Gauntlet (Manopla) basic swings must not shove the enemy: when the equipped weapon is the
        // Gauntlet family (identified by carrying a BreakerGauntletAbility, so no name/ordering coupling),
        // zero the basic hitbox's push so the swing lands in place while keeping its stagger/stance
        // reaction. Other weapons keep their authored push. Mirrors the per-step push zeroing applied to
        // the Gauntlet skills in BreakerGauntletCombat.
        if (WeaponUsesGauntletBasic(newWeapon)) hbDamage.SetPushDistance(0f);

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

    /// <summary>
    /// True when <paramref name="weapon"/> is a Gauntlet-family weapon, detected by carrying at least one
    /// <see cref="BreakerGauntletAbility"/> in its skill list. Used to make the Gauntlet's basic swing land
    /// without a push nudge (its skills are handled in <c>BreakerGauntletCombat</c>). Data-driven so it does
    /// not couple to a weapon name or component initialization order.
    /// </summary>
    private static bool WeaponUsesGauntletBasic(WeaponScript weapon)
    {
        if (!weapon || weapon.abilities == null) return false;
        foreach (Ability ability in weapon.abilities)
            if (ability is BreakerGauntletAbility) return true;
        return false;
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
