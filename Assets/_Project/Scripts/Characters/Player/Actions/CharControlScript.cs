using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class CharControlScript : MonoBehaviour
{
    private const string IDLE = "Idle";
    private const string WALK = "Walk";

    // ComboNova proc gate (R6). The nova fires on exactly every third basic attack and no other:
    // the gate is purely _runBasicCount % 3 == 0 with Rank(ComboNova) > 0, so the attack-speed
    // multiplier (Haste) only changes proc frequency over time, never which basic procs.
    private const int ComboNovaBasicInterval = 3;   // every third basic attack
    private const float ComboNovaRadius = 2.5f;     // R6.1 area radius in meters
    private const float ComboNovaDamagePerRank = .6f; // R6.1 damage = 0.6 x rank x weaponDamage

    [Header("Movement")]
    [SerializeField] private ParticleSystem clickEffect;
    [SerializeField] private LayerMask clickableLayers;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float lookRotationSpeed = 8f;
    [SerializeField] private int clickEffectPoolSize = 8;
    [SerializeField] private Transform clickEffectPoolRoot;
    [SerializeField] private bool debugClickLog = false;
    [SerializeField, Min(0f)] private float _movementClickDeadZone = .55f;

    [Header("Attack")]
    [SerializeField] private string[] attackAnimations = { "Attack1", "Attack2", "Attack3" };
    [SerializeField] private AudioClip[] footstepAudioClips;
    [Range(0, 1)] [SerializeField] private float footstepAudioVolume = 0.5f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackInterval = 0.7f;
    [SerializeField] private float attackBusyDuration = 0.45f;
    [SerializeField] private float attackHitboxDuration = 0.2f;
    [SerializeField] private float comboResetTime = 1.0f;
    [SerializeField] private Vector3 attackBoxSize = new Vector3(2f, 2f, 0f);
    [SerializeField] private LayerMask attackLayers;

    [Header("Attack Reaction")]
    // Basic attacks only Push/Stagger and chip stance. They never stun or knock up directly;
    // hard CC only comes from a Stance Break, which basic swings leave to the finisher/skills.
    [SerializeField] private HitReactionType defaultAttackReaction = HitReactionType.Stagger;
    [SerializeField] private HitStrength defaultAttackStrength = HitStrength.Light;
    [SerializeField, Min(0f)] private float defaultAttackStanceDamage = 12f;
    [SerializeField, Min(0f)] private float defaultAttackPushDistance = 0.35f;
    [Tooltip("Hard CC applied only if a basic hit actually breaks stance. Keep None so combos, not single swings, decide CC.")]
    [SerializeField] private StanceBreakEffect defaultAttackBreakEffect = StanceBreakEffect.None;
    [SerializeField] private HitReactionType[] comboReactions;
    [SerializeField] private HitStrength[] comboStrengths;
    [SerializeField] private float[] comboStanceDamage;

    public bool isDashing = false;

    private NavMeshAgent agent;
    private Animator animator;
    private Interactable target;
    private WeaponScript weapon;
    private AbilityHolder _abilityHolder;
    [SerializeField] private PlayerHUD _playerHUD;

    /// <summary>
    /// The scene HUD explicitly bound to this player (may be null in headless/test scenes).
    /// Exposed so run-scoped feedback holders (e.g. Momentum Strike) can push cosmetic cues
    /// through the same serialized reference instead of a scene lookup.
    /// </summary>
    public PlayerHUD HUD => _playerHUD;

    private float baseAttackRange;
    private float baseAttackInterval;
    private float baseAttackBusyDuration;
    private float baseAttackHitboxDuration;
    private Vector3 baseAttackBoxSize;
    private float baseMoveSpeed;
    private float cachedAttackSpeedMultiplier = -1f;
    private float cachedMovementSpeedMultiplier = -1f;

    private int currentComboCount = 0;
    private bool playerBusy = false;
    private float lastAttackTime;
    private float nextAttackTime;
    private float defaultStoppingDistance;
    private Coroutine attackBusyCoroutine;
    private Coroutine hitboxCoroutine;

    public DashScript dashScript;
    public PlayerActor playerActor;

    private readonly List<ParticleSystem> clickEffectPool = new List<ParticleSystem>();
    private int lastMoveRequestFrame = -1;
    private bool _waitForAttackRelease;
    private int _runBasicCount;
    // Charged Shot (R3): how long the primary attack input has been continuously held while the boon
    // is active and the weapon fires arrows. Consumed when a bow basic resolves into an arrow to pick
    // between a charged (piercing, boosted) and a normal shot, and pushed to the HUD as a charge ratio.
    private float _primaryHeldSeconds;
    private bool _primaryHeldLast;
    public event System.Action BasicAttackPerformed;
    public void RequireAttackRelease() => _waitForAttackRelease = true;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        _abilityHolder = GetComponent<AbilityHolder>();
        animator = GetComponent<Animator>();
        if (playerActor == null)
        {
            playerActor = GetComponent<PlayerActor>();
        }

        defaultStoppingDistance = agent != null ? agent.stoppingDistance : 0f;
        baseMoveSpeed = agent != null ? agent.speed : 0f;
        baseAttackRange = attackRange;
        baseAttackInterval = attackInterval;
        baseAttackBusyDuration = attackBusyDuration;
        baseAttackHitboxDuration = attackHitboxDuration;
        baseAttackBoxSize = attackBoxSize;
    }

    private void Start()
    {
        ValidateComponents();
        InitializeClickEffectPool();
        if (debugClickLog) Debug.Log($"CharControlScript active on {gameObject.name}");
        RefreshWeaponStats();
        RefreshMovementStats();
    }

    void OnDisable() { CancelCombo(); ClearTarget(); }

    void Update()
    {
        if (!playerActor || playerActor.IsDead) return;
        RefreshWeaponStats();
        RefreshMovementStats();
        TrackChargedShot();

        if (_abilityHolder && _abilityHolder.BlocksWorldInput) return;

        isDashing = dashScript != null && dashScript.IsDashing(gameObject);
        HandleDashInput();
        // R10.1: a normal cast pins the player, but a Bow cast fires on the move — let movement keep
        // running at the reduced speed the agent was set to. Basic attacks and new ability casts stay
        // gated by their own IsCasting checks, so only locomotion is unblocked here.
        if (_abilityHolder && _abilityHolder.IsCasting && !_abilityHolder.MovementAllowedWhileCasting) return;

        if (!isDashing)
        {
            HandlePointerInput();
            HandleMovement();
            SetAnimations();
            HandleComboReset();
        }
    }

    private void HandlePointerInput()
    {
        bool primary = GamePreferences.IsHeld(GameControl.Primary);
        if (!primary) _waitForAttackRelease = false;
        if (_waitForAttackRelease) return;
        if (IsDirectionalAttackGesturePressed())
        {
            TryDirectionalBasicAttackAtPointer();
            return;
        }
        if (GamePreferences.WasPressed(GameControl.Primary) || GamePreferences.WasPressed(GameControl.Move))
            RequestMove();
    }

    public static bool IsDirectionalAttackGesturePressed()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null) return false;
        bool shiftHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        return ShouldTriggerDirectionalBasicAttack(shiftHeld,
            mouse.leftButton.wasPressedThisFrame, mouse.rightButton.wasPressedThisFrame);
    }

    public static bool ShouldTriggerDirectionalBasicAttack(bool shiftHeld, bool leftPressed, bool rightPressed) =>
        shiftHeld && (leftPressed || rightPressed);

    private float EffectiveAttackRange => attackRange * (weapon && weapon.FiresArrows ? 1f : playerActor?.RunModifiers?.MeleeScale ?? 1f);

    // Charged Shot (R3): the boon only applies while a bow is equipped and the run modifier is owned.
    // Rank 0 means the boon is absent, so charging is inert and basic arrows behave normally (R3.3).
    private int ChargedShotRank => weapon && weapon.FiresArrows
        ? playerActor?.RunModifiers?.Rank(WeaponBoon.ChargedShot) ?? 0 : 0;

    private bool CanReachTarget(Actor actor)
    {
        if (!actor || actor.IsDead || !actor.isActiveAndEnabled) return false;
        Vector3 delta = actor.transform.position - transform.position;
        if (Mathf.Abs(delta.y) > Mathf.Max(2f, attackBoxSize.y)) return false;
        delta.y = 0;
        if (delta.sqrMagnitude > EffectiveAttackRange * EffectiveAttackRange) return false;
        Vector3 start = transform.position + Vector3.up;
        Vector3 end = actor.transform.position + Vector3.up;
        foreach (RaycastHit hit in Physics.RaycastAll(start, end - start, Vector3.Distance(start, end),
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Actor blocker = hit.collider.GetComponentInParent<Actor>();
            if (blocker == playerActor || blocker == actor) continue;
            if (!blocker) return false;
        }
        return true;
    }

    public bool TryBasicAttack(Vector3 aimPoint)
    {
        RefreshWeaponStats();
        Actor actor = target ? target.myActor ? target.myActor : target.GetComponentInParent<Actor>() : null;
        if (!actor)
        {
            foreach (Collider candidate in Physics.OverlapSphere(aimPoint, .6f,
                attackLayers.value != 0 ? attackLayers.value : Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                Actor found = candidate.GetComponentInParent<Actor>();
                if (found && found != playerActor && CanReachTarget(found)) { actor = found; break; }
            }
        }
        if (!CanStartBasicAttack() || !CanReachTarget(actor)) return false;
        if (agent && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        FacePosition(actor.transform.position);
        PerformAttack(actor);
        return true;
    }

    public bool TryDirectionalBasicAttack(Vector3 aimPoint)
    {
        Vector3 direction = aimPoint - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude <= Mathf.Epsilon) return false;

        ClearTarget();
        if (agent && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
        RefreshWeaponStats();
        if (!CanStartBasicAttack()) return false;

        FaceDirection(direction);
        PerformAttack(null);
        return true;
    }

    private bool CanStartBasicAttack() => isActiveAndEnabled && playerActor && !playerActor.IsDead && weapon &&
        !playerBusy && Time.time >= nextAttackTime && !isDashing &&
        (!_abilityHolder || (!_abilityHolder.BlocksWorldInput && !_abilityHolder.IsCasting));

    // Método para validar componentes essenciais
    private void ValidateComponents()
    {
        if (agent == null) Debug.LogError("NavMeshAgent não encontrado!");
        if (animator == null) Debug.LogError("Animator não encontrado!");
        if (clickEffect == null) Debug.LogWarning("Efeito de clique não configurado!");
        if (dashScript == null) Debug.LogWarning("DashScript nao configurado!");
        if (mainCamera == null && Camera.main == null) Debug.LogWarning("Main camera nao configurada!");
    }

    // Lógica principal para movimentação
    private void HandleMovement()
    {
        FollowTarget();
        FaceTarget();
    }

    // Gerencia o reset de combo
    private void HandleComboReset()
    {
        if (Time.time - lastAttackTime > comboResetTime)
        {
            currentComboCount = 0;
        }
    }

    private void RefreshWeaponStats()
    {
        WeaponScript currentWeapon = playerActor != null ? playerActor.CurrentWeapon : null;
        float currentAttackSpeedMultiplier = playerActor != null && playerActor.Stats != null
            ? playerActor.Stats.AttackSpeedMultiplier
            : 1f;
        if (currentWeapon == weapon && Mathf.Approximately(currentAttackSpeedMultiplier, cachedAttackSpeedMultiplier)) return;

        weapon = currentWeapon;
        cachedAttackSpeedMultiplier = currentAttackSpeedMultiplier;
        ApplyWeaponStats(weapon);
    }

    private void RefreshMovementStats()
    {
        if (agent == null) return;

        float currentMovementSpeedMultiplier = playerActor != null && playerActor.Stats != null
            ? playerActor.Stats.MovementSpeedMultiplier
            : 1f;
        if (Mathf.Approximately(currentMovementSpeedMultiplier, cachedMovementSpeedMultiplier)) return;

        cachedMovementSpeedMultiplier = currentMovementSpeedMultiplier;
        agent.speed = baseMoveSpeed * currentMovementSpeedMultiplier;
    }

    private void ApplyWeaponStats(WeaponScript currentWeapon)
    {
        if (currentWeapon == null)
        {
            attackRange = baseAttackRange;
            attackInterval = baseAttackInterval;
            attackBusyDuration = baseAttackBusyDuration;
            attackHitboxDuration = baseAttackHitboxDuration;
            attackBoxSize = baseAttackBoxSize;
        }
        else
        {
            attackRange = currentWeapon.attackDistance > 0f ? currentWeapon.attackDistance : baseAttackRange;
            attackInterval = currentWeapon.attackSpeed > 0f ? currentWeapon.attackSpeed : baseAttackInterval;
            if (playerActor != null)
            {
                attackInterval = playerActor.GetAttackInterval(attackInterval);
            }
            attackBoxSize = currentWeapon.attackBoxSize != Vector3.zero ? currentWeapon.attackBoxSize : baseAttackBoxSize;
            attackBusyDuration = baseAttackBusyDuration;
            if (attackBusyDuration > 0f && attackInterval > 0f && attackBusyDuration > attackInterval)
            {
                attackBusyDuration = attackInterval;
            }
            attackHitboxDuration = baseAttackHitboxDuration;
        }

        if (attackInterval > 0f && Time.time + attackInterval < nextAttackTime)
        {
            nextAttackTime = Time.time + attackInterval;
        }

        if (target != null && agent != null && target.interactionType == InteractableType.Enemy)
        {
            agent.stoppingDistance = Mathf.Max(0f, EffectiveAttackRange - .1f);
        }
    }

    private void HandleDashInput()
    {
        if (dashScript == null || isDashing) return;

        if (IsDashPressed())
        {
            if (_abilityHolder && _abilityHolder.TryUseDash(dashScript)) isDashing = true;
        }
    }

    private void RequestMove()
    {
        // R10.1: allow move orders during a Bow cast (fires on the move); block them for melee casts.
        if (_abilityHolder && (_abilityHolder.BlocksWorldInput ||
            (_abilityHolder.IsCasting && !_abilityHolder.MovementAllowedWhileCasting))) return;
        if (lastMoveRequestFrame == Time.frameCount)
        {
            return;
        }

        lastMoveRequestFrame = Time.frameCount;
        ClickToMove();
    }

    private bool IsDashPressed() => GamePreferences.WasPressed(GameControl.Dash);

    private Vector3 GetPointerPosition()
    {
        Vector3 position = Input.mousePosition;
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 inputSystemPos = mouse.position.ReadValue();
            if (inputSystemPos != Vector2.zero || position == Vector3.zero)
            {
                position = inputSystemPos;
            }
        }
#endif
        return position;
    }

    private void TryDirectionalBasicAttackAtPointer()
    {
        Vector3 pointerPosition = GetPointerPosition();
        if (_playerHUD && _playerHUD.BlocksPointer(pointerPosition)) return;

        Camera cameraToUse = mainCamera != null ? mainCamera : Camera.main;
        if (cameraToUse == null) return;

        Ray pointerRay = cameraToUse.ScreenPointToRay(pointerPosition);
        var aimPlane = new Plane(Vector3.up, transform.position);
        if (!aimPlane.Raycast(pointerRay, out float distance) || distance < 0f) return;
        TryDirectionalBasicAttack(pointerRay.GetPoint(distance));
    }

    void ClickToMove()
    {
        if (_abilityHolder && _abilityHolder.BlocksWorldInput) return;
        if (_playerHUD && _playerHUD.BlocksPointer(GetPointerPosition())) return;
        // R10.1: a Bow cast fires on the move, so it does not block a click-to-move; a melee cast does.
        if (_abilityHolder && _abilityHolder.IsCasting && !_abilityHolder.MovementAllowedWhileCasting) return;
        if (isDashing)
        {
            if (debugClickLog) Debug.Log("ClickToMove: ignored because isDashing");
            return;
        }

        Camera cameraToUse = mainCamera != null ? mainCamera : Camera.main;
        Vector3 pointerPosition = GetPointerPosition();
        if (cameraToUse == null)
        {
            if (debugClickLog) Debug.LogWarning("ClickToMove: no camera found");
            return;
        }

        int mask = clickableLayers.value != 0 ? clickableLayers.value : Physics.DefaultRaycastLayers;
        bool hitSomething = WorldClickResolver.TryResolve(cameraToUse.ScreenPointToRay(pointerPosition),
            transform, mask, out RaycastHit hit, out bool crossedPlayer);

        if (debugClickLog)
        {
            string camName = cameraToUse != null ? cameraToUse.name : "null";
            if (hitSomething && hit.collider != null)
            {
                Debug.Log($"ClickToMove: hit {hit.collider.name} at {hit.point} cam={camName} mask={mask}");
            }
            else
            {
                Debug.Log($"ClickToMove: no hit pointer={pointerPosition} cam={camName} mask={mask}");
            }
        }

        if (hitSomething && hit.collider != null)
        {
            Interactable interactable = hit.transform.GetComponentInParent<Interactable>();
            if (interactable != null)
            {
                HandleClickEffect(hit);
                HandleInteractable(hit);
                return;
            }

            // Clicking the avatar is not a new move order and must not cancel combat/selection.
            if (crossedPlayer || WorldClickResolver.IsNearPlayer(hit.point, transform.position, _movementClickDeadZone)) return;
            if (!agent || !agent.enabled || !agent.isOnNavMesh) return;
            if (!NavMesh.SamplePosition(hit.point, out NavMeshHit destination, .75f, agent.areaMask)) return;
            if (WorldClickResolver.IsNearPlayer(destination.position, transform.position, _movementClickDeadZone)) return;
            HandleClickEffect(hit);
            MoveToPosition(destination.position);
        }
    }

    private void HandleClickEffect(RaycastHit hit)
    {
        if (clickEffect != null)
        {
            ParticleSystem effect = GetClickEffectInstance();
            effect.transform.position = hit.point + Vector3.up * 0.1f;
            effect.transform.rotation = clickEffect.transform.rotation;
            effect.gameObject.SetActive(true);
            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(true);
        }
    }

    private void InitializeClickEffectPool()
    {
        if (clickEffect == null || clickEffectPoolSize <= 0) return;

        clickEffectPool.Clear();
        EnsureClickEffectPoolRoot();

        if (clickEffect.gameObject.scene.IsValid())
        {
            clickEffect.transform.SetParent(clickEffectPoolRoot, false);
            clickEffect.gameObject.SetActive(false);
            ConfigureClickEffectInstance(clickEffect);
            clickEffectPool.Add(clickEffect);
        }

        int toCreate = clickEffectPoolSize - clickEffectPool.Count;
        for (int i = 0; i < toCreate; i++)
        {
            clickEffectPool.Add(CreateClickEffectInstance());
        }
    }

    private void EnsureClickEffectPoolRoot()
    {
        if (clickEffectPoolRoot != null) return;

        GameObject root = new GameObject("ClickEffectPool");
        clickEffectPoolRoot = root.transform;
    }

    private ParticleSystem GetClickEffectInstance()
    {
        foreach (ParticleSystem ps in clickEffectPool)
        {
            if (!ps.gameObject.activeSelf)
            {
                return ps;
            }
        }

        ParticleSystem extra = CreateClickEffectInstance();
        clickEffectPool.Add(extra);
        return extra;
    }

    private ParticleSystem CreateClickEffectInstance()
    {
        ParticleSystem ps = Instantiate(clickEffect, clickEffectPoolRoot);
        ps.gameObject.SetActive(false);
        ConfigureClickEffectInstance(ps);
        return ps;
    }

    private void ConfigureClickEffectInstance(ParticleSystem ps)
    {
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Callback;

        if (ps.GetComponent<AutoDisableOnStop>() == null)
        {
            ps.gameObject.AddComponent<AutoDisableOnStop>();
        }
    }

    private void ClearTarget()
    {
        target = null;
        if (agent != null)
        {
            agent.stoppingDistance = defaultStoppingDistance;
        }

        EnemyTargetUI ui = EnemyTargetUI.Instance;
        if (ui != null)
        {
            ui.ClearTarget();
        }
    }

    private void SetTarget(Interactable newTarget)
    {
        if (newTarget == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        target = newTarget;
        agent.stoppingDistance = Mathf.Max(0f, EffectiveAttackRange - .1f);
        agent.SetDestination(target.transform.position);

        Actor targetActor = target.myActor;
        if (targetActor == null)
        {
            targetActor = target.GetComponentInParent<Actor>();
        }

        EnemyTargetUI ui = EnemyTargetUI.Ensure();
        if (ui != null)
        {
            ui.SetTarget(targetActor);
        }
    }

    void HandleInteractable(RaycastHit hit)
    {
        Interactable interactable = hit.transform.GetComponentInParent<Interactable>();
        if (interactable != null)
        {
            if (interactable.interactionType == InteractableType.Enemy)
            {
                SetTarget(interactable);
                interactable.Interact(gameObject); // Exibe barra de vida/feedback
            }
            else
            {
                MoveToPosition(hit.point);
                interactable.Interact(gameObject);
            }
        }
        else
        {
            Debug.LogWarning("Nenhum componente Interactable encontrado no objeto clicado.");
        }
    }

    private void MoveToPosition(Vector3 destination)
    {
        CancelCombo();
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.stoppingDistance = defaultStoppingDistance;
            agent.SetDestination(destination);
        }
        ClearTarget();
    }

    private void FollowTarget()
    {
        if (target == null || agent == null || !agent.enabled || !agent.isOnNavMesh) return;

        if (target.interactionType != InteractableType.Enemy)
        {
            ClearTarget();
            return;
        }

        Actor targetActor = target.myActor;
        if (targetActor == null)
        {
            targetActor = target.GetComponentInParent<Actor>();
        }
        if (targetActor == null || targetActor.IsDead || !targetActor.isActiveAndEnabled)
        {
            agent.ResetPath();
            ClearTarget();
            return;
        }

        if (playerBusy) return;
        agent.stoppingDistance = Mathf.Max(0f, EffectiveAttackRange - .1f);
        bool inRange = CanReachTarget(targetActor);
        if (!inRange)
        {
            agent.SetDestination(target.transform.position);
            return;
        }
        agent.ResetPath();
        TryAttackTarget();
    }

    private void TryAttackTarget()
    {
        if (target == null) return;
        if (Time.time < nextAttackTime) return;

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0;
        FaceDirection(direction);

        TryBasicAttack(target.transform.position);
    }

    private void FacePosition(Vector3 targetPoint)
    {
        Vector3 direction = targetPoint - transform.position;
        direction.y = 0f;
        FaceDirection(direction);
    }

    private void FaceDirection(Vector3 direction)
    {
        if (direction.sqrMagnitude > Mathf.Epsilon)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void FaceTarget()
    {
        if (agent.velocity.sqrMagnitude > Mathf.Epsilon)
        {
            Vector3 direction = agent.steeringTarget - transform.position;
            direction.y = 0;
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, lookRotationSpeed * Time.deltaTime);
        }
    }

    
    void SetAnimations()
    {
        if (playerBusy) return;

        animator.Play(agent.velocity == Vector3.zero ? IDLE : WALK);
    }

    private bool TryPlayAttackAnimation(string stateName)
    {
        if (animator == null || animator.layerCount == 0) return false;
        if (string.IsNullOrWhiteSpace(stateName)) return false;

        int layer = 0;
        int stateHash = Animator.StringToHash(stateName);
        if (animator.HasState(layer, stateHash))
        {
            animator.Play(stateHash, layer, 0f);
            return true;
        }

        if (stateName == "Attack1")
        {
            int fallbackHash = Animator.StringToHash("Attack");
            if (animator.HasState(layer, fallbackHash))
            {
                animator.Play(fallbackHash, layer, 0f);
                return true;
            }
        }

        string triggerName = stateName == "Attack1" ? "Attack" : stateName;
        if (HasParameter(triggerName, AnimatorControllerParameterType.Trigger))
        {
            animator.ResetTrigger(triggerName);
            animator.SetTrigger(triggerName);
            return true;
        }

        return false;
    }

    private bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null) return false;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.type == type && param.name == paramName)
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerator ClearBusyAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        playerBusy = false;
        attackBusyCoroutine = null;
    }

    private IEnumerator DeactivateHitboxAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerActor != null)
        {
            playerActor.DeactivateHitbox();
        }
        hitboxCoroutine = null;
    }

    public void CancelCombo()
    {
        currentComboCount = 0;
        playerBusy = false;
        lastAttackTime = 0f;

        if (attackBusyCoroutine != null)
        {
            StopCoroutine(attackBusyCoroutine);
            attackBusyCoroutine = null;
        }

        if (hitboxCoroutine != null)
        {
            StopCoroutine(hitboxCoroutine);
            hitboxCoroutine = null;
        }

        if (playerActor != null)
        {
            playerActor.DeactivateHitbox();
        }
    }

    public void Attack()
    {
        if (target) TryBasicAttack(target.transform.position);
    }

    private void PerformAttack(Actor victim)
    {
        if (!CanStartBasicAttack() || (victim && !CanReachTarget(victim))) return;
        nextAttackTime = Time.time + Mathf.Max(.05f, attackInterval);

        string attackName = null;
        if (attackAnimations != null && attackAnimations.Length > 0)
        {
            if (currentComboCount < 0 || currentComboCount >= attackAnimations.Length)
            {
                currentComboCount = 0;
            }

            attackName = attackAnimations[currentComboCount];
        }

        bool played = TryPlayAttackAnimation(attackName);
        if (!played && debugClickLog)
        {
            Debug.LogWarning($"Attack animation not found: {attackName}");
        }

        int attackIndex = currentComboCount;
        // Basic attacks resolve once through the area/projectile path, never a second trigger hitbox.

        if (playerActor != null && weapon && weapon.FiresArrows)
        {
            // Charged Shot (R3): when the boon is owned, the held duration decides the shot. A hold of at
            // least ChargeTime fires a piercing arrow with (1 + 0.75 * rank) damage (R3.1/R3.2); a shorter
            // hold (or no boon) fires the normal arrow with multiplier 1 and no piercing (R3.3).
            int chargedRank = ChargedShotRank;
            bool charged = chargedRank > 0 && ChargedShot.IsCharged(_primaryHeldSeconds);
            float multiplier = charged ? ChargedShot.DamageMultiplier(chargedRank) : 1f;
            Color arrowColor = charged ? new Color(1f, .8f, .3f) : new Color(.25f, .85f, 1f);
            ArsenalProjectile.Fire(playerActor, transform.position + Vector3.up, transform.forward,
                weapon.attackDamage, multiplier, attackRange, charged, arrowColor);
            // Consume the charge so a follow-up basic in the same hold starts fresh rather than
            // firing a second charged arrow off the same accumulated time.
            _primaryHeldSeconds = 0f;
        }
        else if (playerActor != null)
        {
            float authoredRange = attackRange > 0f ? attackRange : defaultStoppingDistance;
            // R2.5: the unified basic-swing volume comes from the central config when present, so the
            // box/reach match one coherent definition (R2.1); missing config falls back to the volume
            // authored on the weapon asset (attackBoxSize) and its attackDistance-derived range.
            CombatBalanceConfig balance = CombatBalance.Current;
            float range = balance != null ? balance.GauntletBasicReach : authoredRange;
            Vector3 basicBoxSize = balance != null ? balance.GauntletBasicBoxSize : attackBoxSize;
            float scale = playerActor.RunModifiers?.MeleeScale ?? 1f;
            playerActor.TryApplyAreaDamage(
                transform.position,
                transform.forward,
                range * scale,
                basicBoxSize * scale,
                attackLayers,
                weapon ? weapon.attackDamage : -1f,
                1f,
                0f,
                BuildBasicAttackReaction(attackIndex, range));
            _runBasicCount++;
            TryProcComboNova();
        }

        if (attackAnimations != null && attackAnimations.Length > 0)
        {
            currentComboCount = (currentComboCount + 1) % attackAnimations.Length;
        }

        lastAttackTime = Time.time;
        playerBusy = true;
        float busyDuration = attackBusyDuration > 0 ? attackBusyDuration : attackInterval;
        if (busyDuration <= 0f) busyDuration = 0.1f;

        if (attackBusyCoroutine != null)
        {
            StopCoroutine(attackBusyCoroutine);
        }

        attackBusyCoroutine = StartCoroutine(ClearBusyAfter(busyDuration));
        BasicAttackPerformed?.Invoke();
    }

    // ComboNova (R6): applies area damage on every third basic attack while ComboNova is owned.
    // The proc is gated solely on the every-third-basic rule (_runBasicCount % 3 == 0) and a rank
    // above 0. Rank 0 (not owned) fires no nova and leaves the basic outcome otherwise unchanged.
    // Haste feeds this only indirectly: a shorter basic interval (base / AttackSpeedMultiplier, via
    // PlayerActor.GetAttackInterval) means more basics per window, so more third-hits per window,
    // without ever changing which basic attack triggers the nova.
    private void TryProcComboNova()
    {
        int nova = playerActor.RunModifiers?.Rank(WeaponBoon.ComboNova) ?? 0;
        if (nova <= 0 || _runBasicCount % ComboNovaBasicInterval != 0) return;

        // Nova is centered on the player: pass a negligible reach so the sphere resolves around the
        // caster, and let TryApplyAreaDamage resolve the ComboNovaRadius sphere.
        playerActor.TryApplyAreaDamage(
            transform.position + Vector3.up * (1f - ComboNovaRadius),
            transform.forward,
            .01f,
            Vector3.one,
            AreaHitShape.Sphere,
            ComboNovaRadius,
            attackLayers,
            weapon.attackDamage,
            ComboNovaDamagePerRank * nova,
            0f,
            null);
    }

    // Charged Shot (R3): accumulate how long the primary attack input has been held while the boon is
    // active and a bow is equipped, and surface the charge ratio to the HUD. The held time only decides
    // whether the *next* resolved bow arrow is charged; it never fires an arrow itself (PerformAttack
    // owns the cadence). Releasing the input, switching off the boon, or unequipping the bow resets it.
    private void TrackChargedShot()
    {
        bool primary = ChargedShotRank > 0 && GamePreferences.IsHeld(GameControl.Primary);
        if (primary)
        {
            // Reset the accumulator on a fresh press so each hold is measured from zero (R3.1/R3.3).
            if (!_primaryHeldLast) _primaryHeldSeconds = 0f;
            _primaryHeldSeconds += Time.deltaTime;
            // R3.4: the indicator reaches full exactly at ChargeTime.
            float ratio = Mathf.Clamp01(_primaryHeldSeconds / ChargedShot.ChargeTime);
            if (_playerHUD) _playerHUD.SetChargeIndicator(ratio);
        }
        else
        {
            _primaryHeldSeconds = 0f;
            if (_primaryHeldLast && _playerHUD) _playerHUD.SetChargeIndicator(0f);
        }
        _primaryHeldLast = primary;
    }

    private HitReactionRequest BuildBasicAttackReaction(int attackIndex, float range)
    {
        HitReactionType reactionType = GetComboValue(comboReactions, attackIndex, defaultAttackReaction);
        HitStrength strength = GetComboValue(comboStrengths, attackIndex, defaultAttackStrength);
        float stanceDamage = GetComboValue(comboStanceDamage, attackIndex, defaultAttackStanceDamage);

        return new HitReactionRequest(
            playerActor,
            transform.position + transform.forward * Mathf.Max(0f, range),
            transform.forward,
            reactionType,
            strength,
            stanceDamage,
            defaultAttackBreakEffect,
            defaultAttackPushDistance);
    }

    private static T GetComboValue<T>(IReadOnlyList<T> values, int index, T fallback)
    {
        return values != null && index >= 0 && index < values.Count ? values[index] : fallback;
    }

    void OnFootstep()
    {
        if (!playerBusy && footstepAudioClips != null && footstepAudioClips.Length > 0)
        {
            int index = Random.Range(0, footstepAudioClips.Length);
            AudioClip clip = footstepAudioClips[index];
            AudioSource.PlayClipAtPoint(clip, transform.position, footstepAudioVolume);
        }
    }
}
