using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class CharControlScript : MonoBehaviour
{
    private const string IDLE = "Idle";
    private const string WALK = "Walk";

    [Header("Movement")]
    [SerializeField] private ParticleSystem clickEffect;
    [SerializeField] private LayerMask clickableLayers;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float lookRotationSpeed = 8f;
    [SerializeField] private int clickEffectPoolSize = 8;
    [SerializeField] private Transform clickEffectPoolRoot;
    [SerializeField] private bool debugClickLog = false;

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
    [SerializeField] private HitReactionType defaultAttackReaction = HitReactionType.Flinch;
    [SerializeField] private HitStrength defaultAttackStrength = HitStrength.Light;
    [SerializeField] private float defaultAttackPoiseDamage = 10f;
    [SerializeField] private float defaultAttackStunDuration = 0.25f;
    [SerializeField] private float defaultAttackKnockbackForce = 3f;
    [SerializeField] private float defaultAttackLaunchForce = 0f;
    [SerializeField] private bool defaultAttackCanAirJuggle = true;
    [SerializeField] private bool defaultAttackCanRagdoll = false;
    [SerializeField] private HitReactionType[] comboReactions;
    [SerializeField] private HitStrength[] comboStrengths;
    [SerializeField] private float[] comboPoiseDamage;

    public bool isDashing = false;

    private CustomActions input;
    private NavMeshAgent agent;
    private Animator animator;
    private Interactable target;
    private WeaponScript weapon;

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

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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

        input = new CustomActions();
        input.Main.Move.performed += ctx => RequestMove();
    }

    private void Start()
    {
        ValidateComponents();
        InitializeClickEffectPool();
        if (debugClickLog) Debug.Log($"CharControlScript active on {gameObject.name}");
        if (dashScript != null && dashScript.isDashing)
        {
            dashScript.isDashing = false;
            if (debugClickLog) Debug.Log("Reset dash state on start");
        }

        RefreshWeaponStats();
        RefreshMovementStats();
    }

    void OnEnable() => input.Enable();
    void OnDisable() => input.Disable();

    void Update()
    {
        RefreshWeaponStats();
        RefreshMovementStats();


        isDashing = dashScript != null && dashScript.isDashing;
        HandleDashInput();

        if (!isDashing)
        {
            HandleMovement();
            SetAnimations();
            HandleComboReset();
        }
    }

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
            agent.stoppingDistance = attackRange;
        }
    }

    private void HandleDashInput()
    {
        if (dashScript == null || isDashing) return;

        if (IsDashPressed())
        {
            dashScript.Activate(gameObject);
        }
    }

    private void RequestMove()
    {
        if (lastMoveRequestFrame == Time.frameCount)
        {
            return;
        }

        lastMoveRequestFrame = Time.frameCount;
        ClickToMove();
    }

    private bool IsDashPressed()
    {
        bool pressed = Input.GetKeyDown(KeyCode.Space);
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            pressed |= keyboard.spaceKey.wasPressedThisFrame;
        }
#endif
        return pressed;
    }

    private bool IsAttackModifierPressed()
    {
        bool pressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            pressed |= keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        }
#endif
        return pressed;
    }

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

    void ClickToMove()
    {
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
        bool hitSomething = Physics.Raycast(cameraToUse.ScreenPointToRay(pointerPosition), out RaycastHit hit, 100, mask);
        if (!hitSomething && clickableLayers.value != 0)
        {
            hitSomething = Physics.Raycast(cameraToUse.ScreenPointToRay(pointerPosition), out hit, 100, Physics.DefaultRaycastLayers);
        }

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
            HandleClickEffect(hit);

            if (IsAttackModifierPressed())
            {
                RequestStationaryAttack(hit.point);
                return;
            }

            Interactable interactable = hit.transform.GetComponentInParent<Interactable>();
            if (interactable != null)
            {
                HandleInteractable(hit);
                return;
            }

            MoveToPosition(hit.point);
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
        if (newTarget == null || agent == null) return;

        target = newTarget;
        agent.stoppingDistance = attackRange;
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
                interactable.Interact(gameObject); // Passa o jogador como parametro para o metodo Interact
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
        if (agent != null)
        {
            agent.stoppingDistance = defaultStoppingDistance;
            agent.SetDestination(destination);
        }
        ClearTarget();
    }

    private void RequestStationaryAttack(Vector3 targetPoint)
    {
        ClearTarget();

        if (agent != null)
        {
            agent.ResetPath();
            agent.stoppingDistance = defaultStoppingDistance;
        }

        FacePosition(targetPoint);

        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackInterval;
        Attack();
    }

    private void FollowTarget()
    {
        if (target == null || agent == null || agent.pathPending) return;

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
        if (targetActor == null || !targetActor.isActiveAndEnabled)
        {
            ClearTarget();
            return;
        }

        agent.SetDestination(target.transform.position);
        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            agent.ResetPath();
            TryAttackTarget();
        }
    }

    private void TryAttackTarget()
    {
        if (target == null) return;
        if (Time.time < nextAttackTime) return;

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0;
        FaceDirection(direction);

        nextAttackTime = Time.time + attackInterval;
        Attack();
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
        if (playerBusy) return;

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
        if (playerActor != null)
        {
            playerActor.ActivateHitbox();
            if (hitboxCoroutine != null)
            {
                StopCoroutine(hitboxCoroutine);
            }
            float hitboxDuration = attackHitboxDuration > 0 ? attackHitboxDuration : 0.2f;
            hitboxCoroutine = StartCoroutine(DeactivateHitboxAfter(hitboxDuration));
        }

        if (playerActor != null)
        {
            float range = attackRange > 0f ? attackRange : defaultStoppingDistance;
            playerActor.TryApplyAreaDamage(
                transform.position,
                transform.forward,
                range,
                attackBoxSize,
                attackLayers,
                weapon ? weapon.attackDamage : -1f,
                1f,
                0f,
                BuildBasicAttackReaction(attackIndex, range));
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
    }

    private HitReactionRequest BuildBasicAttackReaction(int attackIndex, float range)
    {
        HitReactionType reactionType = GetComboValue(comboReactions, attackIndex, defaultAttackReaction);
        HitStrength strength = GetComboValue(comboStrengths, attackIndex, defaultAttackStrength);
        float poiseDamage = GetComboValue(comboPoiseDamage, attackIndex, defaultAttackPoiseDamage);

        return new HitReactionRequest(
            playerActor,
            transform.position + transform.forward * Mathf.Max(0f, range),
            transform.forward,
            reactionType,
            strength,
            poiseDamage,
            defaultAttackStunDuration,
            defaultAttackKnockbackForce,
            defaultAttackLaunchForce,
            defaultAttackCanAirJuggle,
            defaultAttackCanRagdoll);
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
