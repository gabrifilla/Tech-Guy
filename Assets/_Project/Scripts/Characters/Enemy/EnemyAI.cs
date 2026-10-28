using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    /// <summary>
    /// Serialized kiting / standoff tuning (R1.3, R2.1, R2.2, R3.1, R3.2, R3.5). Grouped so designers
    /// tune the retreat speed, standoff band, and retreat window/cooldown per prefab without touching the
    /// attack path. Each field is clamped in <see cref="OnValidate"/> and re-clamped inside the pure
    /// resolvers that consume it (R16.6).
    /// </summary>
    [System.Serializable]
    public struct KitingConfig
    {
        [Range(0.1f, 0.9f)] public float RetreatSpeedMultiplier; // default 0.5 (R1.3)
        [Range(0f, 1f)]     public float StandoffFraction;       // default 0.45 (R2.1)
        [Range(1f, 2f)]     public float StandoffMargin;         // default 1.15 (R2.2)
        [Range(0.5f, 30f)]  public float RetreatWindow;          // default 1   (fluidity revision 2026-10-11)
        [Range(0.1f, 30f)]  public float RetreatCooldown;        // default 2   (R3.2/R3.5)
    }

    /// <summary>
    /// Serialized natural-behavior tuning (R10.2, R11.2, R11.5, R12.2, R13.2, R13.3, R14.2). Drives the
    /// facing turn rate, reaction/sight-loss timing, cadence jitter, approach wander, and patrol pause.
    /// Clamped in <see cref="OnValidate"/> (with <c>PatrolPauseMax &gt;= PatrolPauseMin</c>) and re-clamped
    /// inside the pure resolvers (R16.6).
    /// </summary>
    [System.Serializable]
    public struct NaturalBehaviorConfig
    {
        [Range(90f, 1440f)] public float AngularSpeed;      // default 540 (R10.2)
        [Range(0f, 2f)]     public float ReactionDelay;     // default 0.3 (R11.2)
        [Range(0f, 10f)]    public float SightLossReset;    // default 1.0 (R11.5)
        [Range(0f, 0.5f)]   public float CadenceJitter;     // default 0.15 (R12.2)
        [Range(0f, 4f)]     public float ApproachOffset;    // default 1.5 (R13.2)
        [Range(0.1f, 10f)]  public float ApproachRefresh;   // default 1.0 (R13.3)
        [Min(0f)]           public float PatrolPauseMin;    // default 0.5 (R14.2)
        [Min(0f)]           public float PatrolPauseMax;    // default 1.5 (R14.2, clamped >= min)
    }

    private const string IdleAnimation = "Idle";
    private const string WalkAnimation = "Walk";
    private const string AttackAnimation = "Attack";

    // Locomotion blend parameter for speed-based animation (R15). When the animator exposes a float
    // parameter with this name, SetMovementAnimation drives it with MovementBlend.Normalize so idle→walk
    // reads as a smooth blend instead of a hard Idle/Walk swap; when it is absent the method falls back
    // to the existing binary Idle/Walk Play (R15.4). The presence check is cached once at Awake and the
    // missing case is logged once per enemy (mirroring PreferredDistanceLayer's missing-config log).
    private const string MovementBlendParameter = "Speed";
    private bool _hasMovementBlendParameter;
    private bool _walkingAnimation;

    [Header("References")]
    public NavMeshAgent agent;
    public Transform player;
    [SerializeField] private Animator animator;
    [SerializeField] private GameObject hitbox;

    [Header("Detection")]
    public LayerMask whatIsGround;
    public LayerMask whatIsPlayer;
    public float sightRange;
    public float attackRange;
    public bool playerInSightRange;
    public bool playerInAttackRange;

    [Header("Patrol")]
    public Vector3 walkPoint;
    public float walkPointRange;

    [Header("Attack")]
    public float attackDamage;
    public float timeBetweenAttacks;
    [SerializeField, Min(0.2f)] private float _attackWindup = 0.7f;
    [SerializeField, Min(0.1f)] private float _attackRecovery = 0.35f;
    /// <summary>Windup seconds before this enemy's attack lands (authored per prefab).</summary>
    public float AttackWindup => _attackWindup;

    private Coroutine _attackRoutine;
    private EnemyCombatActions _combatActions;
    private EnemyAttackTraits _attackTraits;
    private int _attackSequence;
    private CombatReactionController _reaction;
    private Actor _owner;
    private EnemyVariant _variant;
    private PreferredDistanceLayer _preferredDistance;
    private NavMeshPath _positioningPath;
    private Bounds _encounterBounds;
    private bool _hasEncounterBounds;
    private bool _retreating;
    private bool _rotationBeforeRetreat;
    private float _speedBeforeRetreat;
    private Vector3 _retreatOrigin;
    private Vector3 _retreatDestination;
    [SerializeField, Min(.1f)] private float _retreatMaxDistance = 3f;
    public bool IsRetreating => _retreating;
    public KiteController.Phase RetreatPhase => _kite.Current;

    // Per-enemy runtime state for the kiting / natural-behavior layers (design: "Per-enemy runtime
    // state"). These are plain, scene-free C# state objects driven each frame by the integration tasks
    // (8.x/9.x); task 7.2 only allocates them and seeds the shared RNG. All randomness (cadence jitter,
    // approach offset, patrol pause) is driven by _rng so each enemy is deterministic and repeatable
    // from a stable per-instance seed (R16.2). Note: _reactionGate is the perception reaction gate and
    // is intentionally distinct from the CombatReactionController in _reaction above.
    private readonly KiteController _kite = new KiteController();
    private readonly ReactionGate _reactionGate = new ReactionGate();
    private readonly ApproachOffset _approach = new ApproachOffset();
    private readonly PatrolPause _patrolPause = new PatrolPause();
    private System.Random _rng;

    // Log any missing required reference once per enemy so the console isn't spammed every frame,
    // mirroring PreferredDistanceLayer._loggedMissingConfig (R16.2).
    private bool _loggedMissingConfig;

    // Swarm attack-slot coordinator (R5). Optional: when a coordinator is present (assigned in the
    // Inspector or found on a parent room/encounter object), this enemy asks it for a token before
    // starting a melee telegraph and returns the token on interrupt/stun/stance-break via CancelAttack.
    // When absent, behavior is unchanged — the enemy attacks freely as before. Ranged archetypes never
    // consume a melee slot (only melee attackers gate on the pool).
    [SerializeField] private SwarmAttackCoordinator _attackCoordinator;
    private bool _holdsAttackSlot;
    public bool IsWindingUp => _combatActions && _combatActions.IsWindingUp;
    internal bool HasAttackPresentation => _attackRoutine != null || (alreadyAttacked && Time.time < nextAttackTime);
    public EnemyAttackTraits AttackTraits => _attackTraits;

    // GI1.7 is limited to the baseline Grunt. Keep every other archetype/rank's positioning
    // policy intact, and derive the approach limit from the unchanged attack eligibility.
    internal float PositioningAttackRange => _variant && _variant.ArchetypeId == ArchetypeId.Grunt &&
        _reaction && _reaction.Rank == EnemyRank.Normal && _reaction.Category == ReactionCategory.SmallMob
            ? Mathf.Min(attackRange, 1.45f * Mathf.Clamp(transform.lossyScale.y, .5f, 2f) + .35f)
            : float.PositiveInfinity;

    // Ranged standoff (R7.3): the Shooter kites to keep the player outside a minimum standoff distance.
    // The distance is derived from the archetype's engagement band (a fraction of it) so no extra
    // per-profile field is needed and existing melee enemies are untouched. Only archetypes that report a
    // standoff (currently the Shooter) reposition; everything else falls through to the existing chase.
    [Header("Kiting / Standoff")]
    [SerializeField] private KitingConfig _kiting = new KitingConfig
    {
        RetreatSpeedMultiplier = 0.5f,
        StandoffFraction = 0.45f,
        StandoffMargin = 1.15f,
        RetreatWindow = 1f,
        RetreatCooldown = 2f,
    };

    [Header("Natural Behavior")]
    [SerializeField] private NaturalBehaviorConfig _natural = new NaturalBehaviorConfig
    {
        AngularSpeed = 540f,
        ReactionDelay = 0.3f,
        SightLossReset = 1.0f,
        CadenceJitter = 0.15f,
        ApproachOffset = 1.5f,
        ApproachRefresh = 1.0f,
        PatrolPauseMin = 0.5f,
        PatrolPauseMax = 1.5f,
    };

    // Standoff/retreat math now lives in the pure RetreatCadence resolver (standoff fraction + margin)
    // and reads straight from the serialized KitingConfig, so designers tune it without code changes to
    // the attack path (R2.6). These replace the former `private const StandoffFraction/StandoffMargin`
    // and the EnemyAI-local getters that duplicated them.

    [Header("Audio")]
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    private bool walkPointSet;
    private bool alreadyAttacked;
    private float nextAttackTime;
    private CharacterController controller;

    private void Awake()
    {
        _positioningPath = new NavMeshPath();
        ClampKitingConfiguration();
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        // Cache whether the animator exposes the locomotion blend float once (R15.4): SetMovementAnimation
        // runs every frame, so probing the parameter list each call would be wasteful. When the parameter
        // is absent we log once per enemy and fall back to the binary Idle/Walk Play, mirroring the
        // once-per-enemy missing-config log used elsewhere (R16.2).
        _hasMovementBlendParameter = AnimatorHasFloatParameter(animator, MovementBlendParameter);
        if (animator != null && !_hasMovementBlendParameter)
        {
            Debug.Log($"EnemyAI on '{name}': animator has no '{MovementBlendParameter}' float parameter; using binary Idle/Walk locomotion.", this);
        }

        controller = GetComponent<CharacterController>();
        _owner = GetComponent<Actor>();
        _reaction = GetComponent<CombatReactionController>();
        _variant = GetComponent<EnemyVariant>();
        _preferredDistance = GetComponent<PreferredDistanceLayer>();
        _combatActions = GetComponent<EnemyCombatActions>() ?? gameObject.AddComponent<EnemyCombatActions>();
        if (_attackCoordinator == null)
        {
            // A coordinator lives on the room/encounter root, so search parents (never children) so
            // each enemy shares the one pool for its encounter. Left null when there is none.
            _attackCoordinator = GetComponentInParent<SwarmAttackCoordinator>();
        }
        ResolvePlayerReference();
        ConfigureHitbox();

        // Seed the per-enemy RNG from a stable per-instance identifier so cadence jitter, approach
        // offset, and patrol pause are deterministic and repeatable for this enemy across runs (R16.2).
        _rng = new System.Random(GetEntityId().GetHashCode());

        ValidateRequiredReferences();
    }

    /// <summary>
    /// Verifies the references the AI loop depends on are present, logging any that are missing once
    /// per enemy (cached via <see cref="_loggedMissingConfig"/>, with <c>this</c> context) so a
    /// misconfigured prefab is visible without spamming the console every frame — mirroring
    /// <c>PreferredDistanceLayer._loggedMissingConfig</c> (R16.2). This only reports; the existing
    /// <see cref="Update"/> prologue already guards against null <see cref="agent"/>/<see cref="player"/>.
    /// </summary>
    private void ValidateRequiredReferences()
    {
        if (_loggedMissingConfig) return;

        if (agent == null)
        {
            _loggedMissingConfig = true;
            Debug.LogWarning($"EnemyAI on '{name}': no NavMeshAgent found; movement is disabled until one is assigned.", this);
        }
        else if (player == null)
        {
            _loggedMissingConfig = true;
            Debug.LogWarning($"EnemyAI on '{name}': no player reference resolved; chase/attack will not run until a Player is present.", this);
        }
    }

    private void Start()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(false);
        }
    }

    /// <summary>
    /// Clamps the serialized kiting and natural-behavior config to the documented bounds (R1.3, R2.1,
    /// R2.2, R3.1, R3.2, R3.5, R10.2, R11.2, R11.5, R12.2, R13.2, R13.3, R14.2) and enforces
    /// <c>PatrolPauseMax &gt;= PatrolPauseMin</c> (R14.2). The pure resolvers that consume these values
    /// defensively re-clamp (R16.6), so out-of-range serialized data can never produce out-of-range
    /// behavior; this keeps the Inspector honest as well.
    /// </summary>
    private void ClampKitingConfiguration()
    {
        _kiting.RetreatSpeedMultiplier = ClampFinite(_kiting.RetreatSpeedMultiplier, 0.1f, 0.9f, 0.5f);
        _kiting.StandoffFraction = ClampFinite(_kiting.StandoffFraction, 0f, 1f, 0.45f);
        _kiting.StandoffMargin = ClampFinite(_kiting.StandoffMargin, 1f, 2f, 1.15f);
        _kiting.RetreatWindow = ClampFinite(_kiting.RetreatWindow, 0.5f, 30f, 1f);
        _kiting.RetreatCooldown = ClampFinite(_kiting.RetreatCooldown, 0.1f, 30f, 2f);

        _retreatMaxDistance = float.IsNaN(_retreatMaxDistance) || float.IsInfinity(_retreatMaxDistance)
            ? 3f : Mathf.Max(.1f, _retreatMaxDistance);

    }

    private void OnValidate()
    {
        ClampKitingConfiguration();

        _natural.AngularSpeed = Mathf.Clamp(_natural.AngularSpeed, 90f, 1440f);
        _natural.ReactionDelay = Mathf.Clamp(_natural.ReactionDelay, 0f, 2f);
        _natural.SightLossReset = Mathf.Clamp(_natural.SightLossReset, 0f, 10f);
        _natural.CadenceJitter = Mathf.Clamp(_natural.CadenceJitter, 0f, 0.5f);
        _natural.ApproachOffset = Mathf.Clamp(_natural.ApproachOffset, 0f, 4f);
        _natural.ApproachRefresh = Mathf.Clamp(_natural.ApproachRefresh, 0.1f, 10f);
        _natural.PatrolPauseMin = Mathf.Max(0f, _natural.PatrolPauseMin);
        _natural.PatrolPauseMax = Mathf.Max(_natural.PatrolPauseMin, _natural.PatrolPauseMax);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        // Cooldown uses scaled gameplay time even during an attack/recovery or a control lock.
        // TryMaintainStandoff supplies zero delta while cooling down so the clock ticks once per frame.
        if (_kite.Current == KiteController.Phase.Cooldown)
            _kite.Tick(false, Time.deltaTime, _kiting.RetreatWindow, _kiting.RetreatCooldown);
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || player == null || (_owner && _owner.IsDead))
        {
            if (_reaction) _reaction.EvaluateResumeEligibility(false);
            EndRetreat();
            CancelAttack();
            SetMovementAnimation();
            return;
        }

        if (agent.isStopped && _reaction && !_reaction.IsControlLocked) _reaction.EvaluateResumeEligibility(false);
        if (agent.isStopped || (_reaction && _reaction.IsControlLocked) || ProtocolElementQuery.IsFrozen(_owner)) { EndRetreat(); CancelAttack(); return; }
        if (_attackRoutine != null) return;

        UpdatePerception();
        if (_reaction) _reaction.EvaluateResumeEligibility(CanAttack() && playerInAttackRange && ResolvePlayerActor() && !ResolvePlayerActor().IsDead);

        // Perception reaction delay (R11): tick the gate right after UpdatePerception() with the
        // existing in-sight signal, so the delay is an *additional* gate on beginning chase/attack and
        // every existing perception gate above still computes unchanged (R11.1/R11.7). The gate opens
        // after the player has been continuously in sight for the configured ReactionDelay and re-arms
        // after a continuous SightLossReset out of sight (R11.3/R11.4/R11.5). ReactionDelay == 0 opens
        // on the first in-sight frame, matching pre-change timing (R11.6).
        bool reacted = _reactionGate.Tick(playerInSightRange, Time.deltaTime, _natural.ReactionDelay, _natural.SightLossReset);

        if (playerInSightRange && reacted)
        {
            // Player entered sight and the reaction gate opened: leave patrol for chase/attack. Clear any
            // active patrol pause so it never lingers into the chase/attack path (R14.5).
            _patrolPause.Clear();

            if (playerInAttackRange)
            {
                AttackPlayer();
            }
            else
            {
                EndRetreat();
                ChasePlayer();
            }
        }
        else
        {
            // Not yet reacted (still inside the reaction delay) or player not in sight: hold the
            // pre-detection behavior — patrol/idle — until the gate opens (R11.1/R11.3).
            EndRetreat();
            Patrol();
        }

        SetMovementAnimation();
    }

    public void ActivateHitbox()
    {
        // Animation events must not cause a second hit: impact is resolved once after the telegraph.
    }

    public void DeactivateHitbox()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(false);
        }
    }

    /// <summary>
    /// The archetype id of this enemy, resolved live from its <see cref="EnemyVariant"/> so the value
    /// reflects the post-configure state (including the Grunt-equivalent safe-default fallback, which
    /// clears the archetype). Returns <c>null</c> when there is no variant or no assigned archetype, in
    /// which case the trait-only <c>EnemyAttackPatterns</c> overloads run and behavior is unchanged.
    /// </summary>
    private ArchetypeId? ResolveArchetypeId() => _variant ? _variant.ArchetypeId : (ArchetypeId?)null;

    private void ResolvePlayerReference()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    public void ConfigureAttackTraits(EnemyAttackTraits traits)
    {
        CancelAttack();
        _attackTraits = traits;
        _attackSequence = 0;
    }

    public void ConfigureAttack(float damage, float interval)
    {
        attackDamage = Mathf.Max(0f, damage);
        timeBetweenAttacks = Mathf.Max(0.1f, interval);
        ConfigureHitbox();
    }

    private void ConfigureHitbox()
    {
        if (hitbox == null) return;

        HitboxDamage hitboxDamage = hitbox.GetComponent<HitboxDamage>();
        if (hitboxDamage == null)
        {
            hitboxDamage = hitbox.AddComponent<HitboxDamage>();
        }

        Actor owner = GetComponent<Actor>();
        if (owner == null)
        {
            owner = GetComponentInParent<Actor>();
        }

        hitboxDamage.Configure(owner, attackDamage, null);
    }

    private void UpdatePerception()
    {
        playerInSightRange = CheckPlayerInRange(sightRange);
        float meleeRange=Mathf.Min(attackRange,1.45f*Mathf.Clamp(transform.lossyScale.y,.5f,2f)+.35f);
        float distance=Vector3.Distance(transform.position,player.position);
        bool hasRangedMove=(_attackTraits & (EnemyAttackTraits.Haste|EnemyAttackTraits.Frost))!=0;
        ArchetypeId? archetype = ResolveArchetypeId();
        float engagement = EnemyAttackPatterns.EngagementRange(_attackTraits, _attackSequence, meleeRange, archetype);
        // Ranged archetypes (Shooter, Spread_Shooter, Sniper, Bomber, Hazard_Caster, Hooker) open the
        // attack anywhere inside their engagement band — they do NOT require melee proximity. Melee
        // archetypes (and every non-archetype enemy) keep the existing melee/ranged-move gate untouched.
        bool ranged = IsRangedArchetype(archetype);
        playerInAttackRange = CheckPlayerInRange(engagement) &&
            (ranged || distance<=meleeRange || (hasRangedMove && distance>3.2f));
    }

    /// <summary>
    /// True for the attack-shaped archetypes that engage from a distance band rather than at melee reach.
    /// Used only to relax the melee-proximity gate in perception; returns false for melee archetypes and
    /// for a null id (non-archetype enemies), preserving their existing behavior.
    /// </summary>
    private static bool IsRangedArchetype(ArchetypeId? archetype)
    {
        if (archetype is ArchetypeId id)
        {
            switch (id)
            {
                case ArchetypeId.Shooter:
                case ArchetypeId.SpreadShooter:
                case ArchetypeId.Sniper:
                case ArchetypeId.Bomber:
                case ArchetypeId.HazardCaster:
                case ArchetypeId.Hooker:
                    return true;
            }
        }
        return false;
    }

    /// <summary>
    /// True for the ranged archetype(s) that kite to maintain a standoff (R7.3). Only the Shooter
    /// currently repositions; every other archetype (and every non-archetype enemy) reports false and
    /// falls straight through to the existing attack path with identical behavior (R4.4). The actual
    /// standoff distance is resolved by the pure <see cref="RetreatCadence.StandoffDistance"/> from the
    /// engagement band inside <see cref="TryMaintainStandoff"/>.
    /// </summary>
    private static bool IsStandoffKiter(ArchetypeId? archetype)
    {
        return archetype is ArchetypeId id && id == ArchetypeId.Shooter;
    }

    private bool CheckPlayerInRange(float range)
    {
        if (range <= 0f) return false;
        if (player != null)
        {
            Vector3 distanceToPlayer = player.position - transform.position;
            distanceToPlayer.y = 0f;
            if (distanceToPlayer.sqrMagnitude <= range * range)
            {
                return true;
            }
        }

        int mask = whatIsPlayer.value != 0 ? whatIsPlayer.value : Physics.DefaultRaycastLayers;
        Collider[] colliders = Physics.OverlapSphere(transform.position, range, mask, QueryTriggerInteraction.Ignore);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Player") || collider.transform == player || collider.transform.IsChildOf(player))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Patrols between randomized walk points with a short idle pause inserted on arrival (R14). On
    /// reaching a walk point the enemy enters a <see cref="PatrolPause"/> sampled from the serialized
    /// <see cref="NaturalBehaviorConfig.PatrolPauseMin"/>/<see cref="NaturalBehaviorConfig.PatrolPauseMax"/>
    /// range (R14.1/R14.2); while <see cref="PatrolPause.Tick"/> reports the pause active the enemy holds
    /// position and does not select or move toward a new walk point (R14.3). When the pause elapses it
    /// resumes patrol through the existing random walk-point selection and ground check unchanged
    /// (R14.4). A configured <see cref="NaturalBehaviorConfig.PatrolPauseMax"/> of 0 never pauses, so
    /// patrol is continuous exactly as before (R14.6, handled inside <see cref="PatrolPause.Begin"/>).
    /// The pause is cleared from <see cref="Update"/>'s dispatch via <see cref="PatrolPause.Clear"/> when
    /// the player enters sight so it never lingers into chase/attack (R14.5).
    /// </summary>
    private void Patrol()
    {
        // Holding position for an active arrival pause: don't select or move toward a new walk point
        // until the pause elapses (R14.3). On elapse this frame, fall through to resume patrol (R14.4).
        if (_patrolPause.Tick(Time.deltaTime))
        {
            return;
        }

        if (!walkPointSet)
        {
            SearchWalkPoint();
        }

        if (walkPointSet)
        {
            agent.SetDestination(walkPoint);
        }

        Vector3 distanceToWalkPoint = transform.position - walkPoint;
        if (distanceToWalkPoint.magnitude < 1f)
        {
            // Reached the walk point: begin a randomized idle pause before picking the next one
            // (R14.1). PatrolPauseMax == 0 makes Begin a no-op pause, preserving continuous motion
            // (R14.6). walkPointSet is cleared so the next (post-pause) frame selects a new walk point
            // through the unchanged random/ground-check path (R14.4).
            walkPointSet = false;
            _patrolPause.Begin(_natural.PatrolPauseMin, _natural.PatrolPauseMax, _rng);
        }
    }

    private void SearchWalkPoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround))
        {
            walkPointSet = true;
        }
    }

    /// <summary>
    /// Chases the player with a bounded approach offset so a group closing in reads as a crowd rather
    /// than a dead-straight formation (R13.1). The lateral/angled displacement is owned by the pure
    /// <see cref="ApproachOffset"/> resolver (seeded by the per-enemy <see cref="_rng"/>): it is bounded
    /// by <see cref="NaturalBehaviorConfig.ApproachOffset"/>, held fixed between
    /// <see cref="NaturalBehaviorConfig.ApproachRefresh"/> intervals, and collapses to zero when the
    /// magnitude is 0 so the enemy beelines exactly as before (R13.3/R13.8). Movement is issued
    /// exclusively through the NavMesh agent destination — the Transform is never written (R13.5). The
    /// offset destination is validated with <see cref="NavMesh.SamplePosition"/>; if it would land off
    /// the mesh the chase falls back to the raw player position for that refresh so the enemy still
    /// closes to attack range (R13.4/R13.6). The offset is naturally suppressed while a ranged enemy is
    /// kiting/repositioning: that path lives in <see cref="TryMaintainStandoff"/> inside
    /// <see cref="AttackPlayer"/>, so <see cref="ChasePlayer"/> is only reached when standoff did not
    /// reposition this frame (R13.7).
    /// </summary>
    private void ChasePlayer()
    {
        if (TryPreferredPositioning(allowRetreat: !IsStandoffKiter(ResolveArchetypeId()))) return;
        Vector3 offset = _approach.Tick(Time.deltaTime, _natural.ApproachOffset, _natural.ApproachRefresh, _rng);
        Vector3 desired = player.position + offset;

        if (offset != Vector3.zero &&
            NavMesh.SamplePosition(desired, out NavMeshHit hit, _natural.ApproachOffset, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        else
        {
            // Zero offset (beeline, R13.8) or an off-mesh sample (R13.6): chase the raw player position.
            agent.SetDestination(player.position);
        }
    }

    private void AttackPlayer()
    {
        // Shooter standoff (R7.3): when the player is inside the minimum standoff distance, kite to a
        // NavMesh-reachable point that restores the standoff instead of firing this frame. Only the
        // Shooter reports a standoff distance; melee and other ranged archetypes fall straight through
        // to the attack path, so their behavior is unchanged.
        if (TryMaintainStandoff()) return;

        FacePlayer();
        if (Time.time < nextAttackTime)
        {
            if (!TryPreferredPositioning(allowRetreat: !IsStandoffKiter(ResolveArchetypeId()))) agent.ResetPath();
            return;
        }
        agent.ResetPath();
        // Attack facing and the offensive cadence remain unchanged; positioning waiting is now
        // authorized before stopping, so a held approach is not reset and reissued every frame.
        if (!IsFacingPlayer()) return;

        // Swarm attack-slot gate (R5.2): a melee attacker must hold a token before it may attack. When
        // the encounter is at its limit the request is denied and the enemy simply holds position here
        // (already facing the player, path reset) instead of attacking, until a slot frees up. Ranged
        // archetypes bypass the melee slot pool entirely.
        if (!TryAcquireAttackSlot())
        {
            TryPreferredPositioning(allowRetreat: !IsStandoffKiter(ResolveArchetypeId()));
            return;
        }

        alreadyAttacked = true;
        _attackRoutine = StartCoroutine(TelegraphedAttack());
    }

    /// <summary>Supplies the arena boundary without introducing a scene search or a global owner.</summary>
    public void ConfigureEncounterBounds(Vector3 center, Vector2 size)
    {
        _hasEncounterBounds = EnemyNavigation.IsFinite(center) && !float.IsNaN(size.x) && !float.IsInfinity(size.x) &&
            !float.IsNaN(size.y) && !float.IsInfinity(size.y) && size.x > 0f && size.y > 0f;
        if (!_hasEncounterBounds) return;
        float inset = agent ? Mathf.Max(.1f, agent.radius) : .5f;
        _encounterBounds = new Bounds(center, new Vector3(Mathf.Max(.1f, size.x - 2f * inset),
            1000f, Mathf.Max(.1f, size.y - 2f * inset)));
    }

    /// <summary>The distance layer suggests; this AI owns the final movement decision.</summary>
    private bool TryPreferredPositioning(bool allowRetreat)
    {
        if (!_preferredDistance || !_preferredDistance.TryGetPositioningTarget(out Vector3 target)) return false;
        if (!allowRetreat)
        {
            Vector3 currentOffset = transform.position - player.position; currentOffset.y = 0f;
            Vector3 targetOffset = target - player.position; targetOffset.y = 0f;
            // Preferred firing distance is an approach goal, never another escape from melee pressure.
            if (targetOffset.sqrMagnitude > currentOffset.sqrMagnitude + .01f) return false;
        }
        if (!EnemyNavigation.TryResolveDestination(agent, target, Mathf.Max(.5f, agent.radius), _positioningPath,
            _hasEncounterBounds, _encounterBounds, out Vector3 destination))
        {
            agent.ResetPath();
            return false;
        }
        if (agent.hasPath && (agent.destination - destination).sqrMagnitude <= .01f) return true;
        // Apply exactly the complete route already checked for areaMask and encounter bounds.
        // Re-requesting it with SetDestination creates a frame where hasPath can still be false.
        return agent.SetPath(_positioningPath);
    }

    /// <summary>One short radial retreat, held to a fixed target, then an unavoidable punishable pause.</summary>
    private bool TryMaintainStandoff()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || player == null) return false;
        ArchetypeId? archetype = ResolveArchetypeId();
        if (!IsStandoffKiter(archetype)) return false;
        float meleeRange = Mathf.Min(attackRange, 1.45f * Mathf.Clamp(transform.lossyScale.y, .5f, 2f) + .35f);
        float engagementBand = EnemyAttackPatterns.EngagementRange(_attackTraits, _attackSequence, meleeRange, archetype);
        float standoff = RetreatCadence.StandoffDistance(engagementBand, ClampFinite(_kiting.StandoffFraction, 0f, 1f, .45f));
        float repositionTarget = RetreatCadence.RepositionTarget(standoff, ClampFinite(_kiting.StandoffMargin, 1f, 2f, 1.15f));
        Vector3 away = transform.position - player.position; away.y = 0f;
        float distance = away.magnitude;
        // Entry threshold is lower than the exit distance: crossing 5.4m does not alternate chase/retreat.
        bool wantsToKite = distance < (_kite.Current == KiteController.Phase.Kiting ? repositionTarget : standoff);
        float delta = _kite.Current == KiteController.Phase.Cooldown ? 0f : Time.deltaTime;
        if (!_kite.Tick(wantsToKite, delta, _kiting.RetreatWindow, _kiting.RetreatCooldown))
        {
            EndRetreat();
            return false;
        }

        if (_retreating)
        {
            Vector3 travelled = transform.position - _retreatOrigin; travelled.y = 0f;
            float budget = float.IsNaN(_retreatMaxDistance) || float.IsInfinity(_retreatMaxDistance)
                ? 3f : Mathf.Max(.1f, _retreatMaxDistance);
            bool reached = !agent.pathPending && (!agent.hasPath || agent.remainingDistance <= Mathf.Max(.1f, agent.stoppingDistance));
            bool blocked = !agent.pathPending && agent.hasPath && agent.pathStatus != NavMeshPathStatus.PathComplete;
            if (reached || blocked || travelled.sqrMagnitude >= budget * budget)
            {
                EndRetreat();
                return false;
            }
        }
        else
        {
            Vector3 direction = distance > Mathf.Epsilon ? away / distance : -transform.forward;
            Vector3 desired = player.position + direction * repositionTarget;
            desired.y = transform.position.y;
            if (!EnemyNavigation.TryResolveDestination(agent, desired, Mathf.Max(.5f, agent.radius), _positioningPath,
                _hasEncounterBounds, _encounterBounds, out _retreatDestination) ||
                Mathf.Abs(PlanarDistance(_retreatDestination, player.position) - repositionTarget) > .5f)
            {
                EndRetreat();
                // No stale retreat path survives failure. This frame holds; the cooldown can then fire.
                agent.ResetPath();
                return true;
            }
            _retreatOrigin = transform.position;
            _speedBeforeRetreat = agent.speed;
            _rotationBeforeRetreat = agent.updateRotation;
            _retreating = true;
            agent.updateRotation = false;
            if (!agent.SetPath(_positioningPath))
            {
                EndRetreat();
                return true;
            }
        }
        float chaseSpeed = _variant && _variant.ChaseSpeed > 0f ? _variant.ChaseSpeed : _speedBeforeRetreat;
        agent.speed = RetreatCadence.RetreatSpeed(chaseSpeed, ClampFinite(_kiting.RetreatSpeedMultiplier, .1f, .9f, .5f));
        FacePlayer();
        return true;
    }

    private static float ClampFinite(float value, float min, float max, float fallback) =>
        float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        Vector3 offset = a - b; offset.y = 0f;
        return offset.magnitude;
    }

    private void EndRetreat()
    {
        _kite.EndRetreat();
        if (!_retreating) return;
        _retreating = false;
        if (agent == null) return;
        float chaseSpeed = _variant && _variant.ChaseSpeed > 0f ? _variant.ChaseSpeed : _speedBeforeRetreat;
        if (chaseSpeed > 0f) agent.speed = chaseSpeed;
        agent.updateRotation = _rotationBeforeRetreat;
        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
    }

    private bool CanAttack() => isActiveAndEnabled && _owner && !_owner.IsDead &&
        player && player.gameObject.activeInHierarchy && agent && agent.enabled && agent.isOnNavMesh &&
        !agent.isStopped && (!_reaction || !_reaction.IsControlLocked) && !ProtocolElementQuery.IsFrozen(_owner);

    private IEnumerator TelegraphedAttack()
    {
        long reactionExecution = ++_reactionExecution;
        if (_reaction) _reaction.BeginOffensiveAction(reactionExecution);
        Actor target = ResolvePlayerActor();
        float distance = Vector3.Distance(transform.position, player.position);
        ArchetypeId? archetype = ResolveArchetypeId();
        EnemyAttackKind kind = EnemyAttackPatterns.Select(_attackTraits, _attackSequence++, distance, archetype);
        // Per-archetype melee telegraph: Rush winds up faster than Grunt (R3.4/R4.3). The override only
        // affects the Punch/DoublePunch beat; Heavy's slam/ground-pound and Charger's charge own their
        // own longer telegraphs and ignore it. A negative sentinel means "keep the attack's default",
        // so ranged archetypes and legacy (non-archetype) enemies are unaffected.
        float meleeWindup = EnemyAttackPatterns.MeleeWindup(archetype);
        yield return _combatActions.Perform(kind, target, attackDamage, CanAttack, boss: false, meleeWindupOverride: meleeWindup);
        // Cadence jitter (R12): add bounded, seeded random variation to the next-attack delay so the
        // enemy doesn't fire on a metronome, without introducing a new timing channel. The same
        // Max(_attackRecovery, timeBetweenAttacks) value is passed as BOTH the base interval and the
        // floor, so the existing recovery guarantee is preserved exactly (R12.4) and jitter == 0 keeps
        // the old deterministic cadence (R12.5). Driven by the per-enemy _rng for determinism (R16.2).
        float cadenceBase = Mathf.Max(_attackRecovery, timeBetweenAttacks);
        nextAttackTime = Time.time + CadenceJitter.Effective(cadenceBase, _natural.CadenceJitter, cadenceBase, _rng);
        yield return new WaitForSeconds(Mathf.Max(.1f, _attackRecovery));
        if (_reaction) _reaction.EndOffensiveAction(reactionExecution);
        _attackRoutine = null;

        // Release the swarm slot when the attack completes so the pool can rotate to a waiting enemy
        // (R5.2/R5.3). This enemy re-requests a token on its next AttackPlayer tick; if the pool is full
        // it holds position instead. Interrupted/stunned/broken attacks release earlier via CancelAttack.
        ReleaseAttackSlot();
    }

    public void InterruptAttack()
    {
        EndRetreat();
        CancelAttack();
    }

    /// <summary>
    /// Applies a conditional interruption ability to this enemy (Requisitos 7.6, 7.7 / Property 23),
    /// reusing the same <see cref="CancelAttack"/> choke point every hard interrupt already flows
    /// through. The decision is owned by the pure <see cref="InterruptWindowGate"/>: while this enemy
    /// is inside its interruptible window — winding up a telegraph whose damage beat has not yet
    /// resolved (<see cref="IsWindingUp"/>) — the attack is cancelled and its pending beat suppressed
    /// in the same frame; otherwise the attack in progress (if any) is preserved, the beat is not
    /// suppressed, and the returned decision signals a no-interruption reaction for the caller to show.
    ///
    /// This does NOT replace the unconditional <see cref="InterruptAttack()"/> used by
    /// <see cref="CombatReactionController"/> for stun/knock-up/stance-break (those always cancel);
    /// it is the entry point for interruption <em>abilities</em> that only land inside the window.
    /// </summary>
    /// <returns>What the interruption did this frame, for feedback (Requisito 7.6/7.7).</returns>
    public InterruptWindowGate.Decision TryInterruptAttack()
    {
        InterruptWindowGate.Decision decision = InterruptWindowGate.Resolve(IsWindingUp);
        // In-window: cancel the telegraph before its beat resolves. Because the live attack pipeline
        // (EnemyAttackExecution) re-checks its canAttack predicate on the beat frame and CancelAttack
        // bumps the coroutine version, the pending beat is suppressed same-frame (Requisito 7.6).
        if (decision.CancelsAttack) CancelAttack();
        // Out-of-window: leave the attack/beat untouched; the caller shows the no-interruption
        // feedback carried by decision.SignalsNoInterruption (Requisito 7.7).
        return decision;
    }

    private void CancelAttack()
    {
        if (_attackRoutine != null && _reaction) _reaction.CancelOffensiveActionExternally();
        if (_attackRoutine != null)
        {
            StopCoroutine(_attackRoutine);
            nextAttackTime = Time.time + Mathf.Max(.35f, _attackRecovery);
        }
        _attackRoutine = null;
        if (_combatActions) _combatActions.Cancel();
        alreadyAttacked = false;
        DeactivateHitbox();

        // Return the swarm attack slot on interrupt / stun / stance-break (R5.3). CancelAttack is the
        // single choke point for all three: CombatReactionController already routes stun, knock-up and
        // stance-break through InterruptAttack() -> CancelAttack(). Releasing here frees the slot for
        // the next eligible waiting enemy.
        ReleaseAttackSlot();
    }

    /// <summary>
    /// Asks the swarm coordinator (when present) for a melee attack token (R5.2). Ranged archetypes and
    /// enemies with no coordinator never gate on the pool and are always allowed to attack. Returns true
    /// when this enemy may attack this frame; false when it must hold its waiting position.
    /// </summary>
    private bool TryAcquireAttackSlot()
    {
        if (_attackCoordinator == null) return true;
        if (IsRangedArchetype(ResolveArchetypeId())) return true;

        if (_holdsAttackSlot) return true;

        _holdsAttackSlot = _attackCoordinator.TryAcquireSlot(this);
        return _holdsAttackSlot;
    }

    /// <summary>
    /// Returns this enemy's melee attack token to the coordinator (R5.3) if it holds one, so the slot is
    /// freed for another eligible enemy. Safe and idempotent when no token is held or no coordinator exists.
    /// </summary>
    private void ReleaseAttackSlot()
    {
        if (_attackCoordinator == null || !_holdsAttackSlot) return;

        _holdsAttackSlot = false;
        _attackCoordinator.ReleaseSlot(this);
    }

    private void OnDisable()
    {
        EndRetreat();
        _kite.Reset();
        CancelAttack();
    }
    private long _reactionExecution;

    private Actor ResolvePlayerActor()
    {
        if (player == null) return null;

        Actor targetActor = player.GetComponentInParent<Actor>();
        if (targetActor != null) return targetActor;

        return player.GetComponentInChildren<Actor>();
    }

    /// <summary>
    /// Rotates this enemy toward the player gradually instead of snapping instantly (R10.1/R10.3).
    /// The per-frame turn step is owned by the pure <see cref="EnemyFacing"/> resolver, limited to the
    /// configured <see cref="NaturalBehaviorConfig.AngularSpeed"/> (clamped internally), and never
    /// overshoots the target (R10.5/R10.6). When the horizontal player direction is degenerate the
    /// rotation is left unchanged for the frame (R10.5). Facing-gated actions consult
    /// <see cref="IsFacingPlayer"/> so they proceed only once the forward is within the facing epsilon,
    /// exactly as the former instant snap allowed (R10.4).
    /// </summary>
    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > Mathf.Epsilon)
        {
            Vector3 stepped = EnemyFacing.StepTowards(transform.forward, direction, _natural.AngularSpeed, Time.deltaTime);
            if (stepped.sqrMagnitude > Mathf.Epsilon)
            {
                transform.rotation = Quaternion.LookRotation(stepped);
            }
        }
    }

    /// <summary>
    /// True once this enemy's forward is within <see cref="EnemyFacing.FacingEpsilonDegrees"/> of the
    /// horizontal direction to the player, meaning facing-gated actions (attack initiation, standoff
    /// repositioning) may proceed exactly as under the former instant-snap behavior (R10.4). A degenerate
    /// direction is treated as already facing, matching <see cref="EnemyFacing.IsFacing"/> (R10.5).
    /// </summary>
    private bool IsFacingPlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        return EnemyFacing.IsFacing(transform.forward, direction);
    }

    private void SetMovementAnimation()
    {
        // Null animator/agent: skip the update entirely (R15.5).
        if (animator == null || agent == null) return;
        if ((_reaction && _reaction.IsControlLocked) || ProtocolElementQuery.IsFrozen(_owner)) return;

        // Preserve the attack-animation guard: while a telegraph is running, or during the brief
        // post-attack recovery window, hold the current (attack) animation and do not drive locomotion
        // (R15.3). This is unchanged from the prior binary behavior.
        if (HasAttackPresentation)
        {
            return;
        }

        alreadyAttacked = false;

        if (_hasMovementBlendParameter)
        {
            // Speed-based blend (R15.1/R15.2): normalize current speed into [0,1] via the pure resolver
            // and drive the animator's blend float, so idle→walk reads as a smooth blend. The resolver
            // clamps to [0,1] and handles a degenerate agent.speed, so the parameter stays in range.
            // Keep the gait reference at chase speed during backpedal: a x0.5 retreat blends to Walk,
            // rather than looking like full-speed Run because the agent maximum was temporarily halved.
            float referenceSpeed = _retreating
                ? (_variant && _variant.ChaseSpeed > 0f ? _variant.ChaseSpeed : _speedBeforeRetreat)
                : agent.speed;
            animator.SetFloat(MovementBlendParameter, MovementBlend.Normalize(agent.velocity.magnitude, referenceSpeed),
                EnemyLocomotionPresentation.TransitionSeconds, Time.deltaTime);
        }
        else
        {
            // Existing clips get a short blend with speed hysteresis. Never restart the same state.
            if (_reaction && _reaction.OwnsFlinchPresentation) return;
            _walkingAnimation = EnemyLocomotionPresentation.WantsWalk(agent.velocity.magnitude, _walkingAnimation);
            int state = Animator.StringToHash(_walkingAnimation ? WalkAnimation : IdleAnimation);
            AnimatorStateInfo current = animator.GetCurrentAnimatorStateInfo(0);
            bool alreadyMovingToState = animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).shortNameHash == state;
            if (current.shortNameHash != state && !alreadyMovingToState)
                animator.CrossFadeInFixedTime(state, EnemyLocomotionPresentation.TransitionSeconds, 0);
        }
    }

    /// <summary>
    /// Returns true when <paramref name="animator"/> exposes a <see cref="AnimatorControllerParameterType.Float"/>
    /// parameter named <paramref name="parameterName"/>. Used once at <see cref="Awake"/> to cache whether the
    /// locomotion blend parameter is present (R15.4); returns false for a null animator or when the controller
    /// has not yet initialized its parameters.
    /// </summary>
    private static bool AnimatorHasFloatParameter(Animator animator, string parameterName)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight <= 0.5f) return;
        if (FootstepAudioClips == null || FootstepAudioClips.Length == 0) return;

        int index = Random.Range(0, FootstepAudioClips.Length);
        Vector3 position = controller != null ? transform.TransformPoint(controller.center) : transform.position;
        AudioSource.PlayClipAtPoint(FootstepAudioClips[index], position, FootstepAudioVolume);
    }
}
