using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class EnemyAI : MonoBehaviour
{
    private const string IdleAnimation = "Idle";
    private const string WalkAnimation = "Walk";
    private const string AttackAnimation = "Attack";

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
    private Coroutine _attackRoutine;
    private EnemyCombatActions _combatActions;
    private EnemyAttackTraits _attackTraits;
    private int _attackSequence;
    private CombatReactionController _reaction;
    private Actor _owner;
    private EnemyVariant _variant;

    // Swarm attack-slot coordinator (R5). Optional: when a coordinator is present (assigned in the
    // Inspector or found on a parent room/encounter object), this enemy asks it for a token before
    // starting a melee telegraph and returns the token on interrupt/stun/stance-break via CancelAttack.
    // When absent, behavior is unchanged — the enemy attacks freely as before. Ranged archetypes never
    // consume a melee slot (only melee attackers gate on the pool).
    [SerializeField] private SwarmAttackCoordinator _attackCoordinator;
    private bool _holdsAttackSlot;
    public bool IsWindingUp => _combatActions && _combatActions.IsWindingUp;
    public EnemyAttackTraits AttackTraits => _attackTraits;

    // Ranged standoff (R7.3): the Shooter kites to keep the player outside a minimum standoff distance.
    // The distance is derived from the archetype's engagement band (a fraction of it) so no extra
    // per-profile field is needed and existing melee enemies are untouched. Only archetypes that report a
    // standoff (currently the Shooter) reposition; everything else falls through to the existing chase.
    private const float StandoffFraction = 0.45f;   // min standoff as a fraction of the engagement band
    private const float StandoffMargin = 1.15f;      // over-shoot the min so the enemy settles just outside it

    [Header("Audio")]
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    private bool walkPointSet;
    private bool alreadyAttacked;
    private float nextAttackTime;
    private CharacterController controller;

    private void Awake()
    {
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        controller = GetComponent<CharacterController>();
        _owner = GetComponent<Actor>();
        _reaction = GetComponent<CombatReactionController>();
        _variant = GetComponent<EnemyVariant>();
        _combatActions = GetComponent<EnemyCombatActions>() ?? gameObject.AddComponent<EnemyCombatActions>();
        if (_attackCoordinator == null)
        {
            // A coordinator lives on the room/encounter root, so search parents (never children) so
            // each enemy shares the one pool for its encounter. Left null when there is none.
            _attackCoordinator = GetComponentInParent<SwarmAttackCoordinator>();
        }
        ResolvePlayerReference();
        ConfigureHitbox();
    }

    private void Start()
    {
        if (hitbox != null)
        {
            hitbox.SetActive(false);
        }
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || player == null || (_owner && _owner.IsDead))
        {
            CancelAttack();
            SetMovementAnimation();
            return;
        }

        if (agent.isStopped || (_reaction && _reaction.IsControlLocked)) { CancelAttack(); return; }
        if (_attackRoutine != null) return;

        UpdatePerception();

        if (playerInSightRange && playerInAttackRange)
        {
            AttackPlayer();
        }
        else if (playerInSightRange)
        {
            ChasePlayer();
        }
        else
        {
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
    /// Minimum standoff distance for a ranged archetype that kites (R7.3), or 0 when the archetype does
    /// not reposition. Derived as a fraction of the engagement band so no extra per-profile field is
    /// needed. Only the Shooter currently kites; the other ranged archetypes hold their band and shoot.
    /// </summary>
    private float StandoffDistance(ArchetypeId? archetype, float engagement)
    {
        if (archetype is ArchetypeId id && id == ArchetypeId.Shooter)
            return engagement * StandoffFraction;
        return 0f;
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

    private void Patrol()
    {
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
            walkPointSet = false;
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

    private void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    private void AttackPlayer()
    {
        // Shooter standoff (R7.3): when the player is inside the minimum standoff distance, kite to a
        // NavMesh-reachable point that restores the standoff instead of firing this frame. Only the
        // Shooter reports a standoff distance; melee and other ranged archetypes fall straight through
        // to the attack path, so their behavior is unchanged.
        if (TryMaintainStandoff()) return;

        agent.ResetPath();
        FacePlayer();

        if (Time.time < nextAttackTime) return;

        // Swarm attack-slot gate (R5.2): a melee attacker must hold a token before it may attack. When
        // the encounter is at its limit the request is denied and the enemy simply holds position here
        // (already facing the player, path reset) instead of attacking, until a slot frees up. Ranged
        // archetypes bypass the melee slot pool entirely.
        if (!TryAcquireAttackSlot()) return;

        alreadyAttacked = true;
        _attackRoutine = StartCoroutine(TelegraphedAttack());
    }

    /// <summary>
    /// If this enemy is a standoff-kiting ranged archetype (Shooter) and the player has closed inside its
    /// minimum standoff distance, sets a NavMesh destination directly away from the player that restores
    /// the standoff, and returns true so the caller skips attacking this frame. Movement is NavMesh-only
    /// (<c>NavMeshAgent.SetDestination</c> after <c>NavMesh.SamplePosition</c>) — never a raw transform
    /// move. Returns false (no reposition) for every other archetype, when already at or beyond standoff,
    /// or when the agent cannot move this frame. Requirement 7.3.
    /// </summary>
    private bool TryMaintainStandoff()
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh) return false;

        ArchetypeId? archetype = ResolveArchetypeId();
        float meleeRange = Mathf.Min(attackRange, 1.45f * Mathf.Clamp(transform.lossyScale.y, .5f, 2f) + .35f);
        float engagement = EnemyAttackPatterns.EngagementRange(_attackTraits, _attackSequence, meleeRange, archetype);
        float standoff = StandoffDistance(archetype, engagement);
        if (standoff <= 0f) return false; // Not a kiting archetype.

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        float distance = away.magnitude;
        if (distance >= standoff) return false; // Already at or beyond standoff: no reposition needed.

        FacePlayer();
        Vector3 direction = distance > Mathf.Epsilon ? away / distance : -transform.forward;
        Vector3 desired = player.position + direction * (standoff * StandoffMargin);

        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, standoff, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
        // Sampling failure leaves the agent at its current destination for the frame (design: repositioning
        // behaviors leave the archetype in place on sample failure). Either way we skip attacking so the
        // Shooter does not fire from inside its own standoff.
        return true;
    }

    private bool CanAttack() => isActiveAndEnabled && _owner && !_owner.IsDead &&
        player && player.gameObject.activeInHierarchy && agent && agent.enabled && agent.isOnNavMesh &&
        !agent.isStopped && (!_reaction || !_reaction.IsControlLocked);

    private IEnumerator TelegraphedAttack()
    {
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
        nextAttackTime = Time.time + Mathf.Max(_attackRecovery, timeBetweenAttacks);
        yield return new WaitForSeconds(Mathf.Max(.1f, _attackRecovery));
        _attackRoutine = null;

        // Release the swarm slot when the attack completes so the pool can rotate to a waiting enemy
        // (R5.2/R5.3). This enemy re-requests a token on its next AttackPlayer tick; if the pool is full
        // it holds position instead. Interrupted/stunned/broken attacks release earlier via CancelAttack.
        ReleaseAttackSlot();
    }

    public void InterruptAttack() => CancelAttack();

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

    private void OnDisable() => CancelAttack();

    private Actor ResolvePlayerActor()
    {
        if (player == null) return null;

        Actor targetActor = player.GetComponentInParent<Actor>();
        if (targetActor != null) return targetActor;

        return player.GetComponentInChildren<Actor>();
    }

    private void FacePlayer()
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > Mathf.Epsilon)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void SetMovementAnimation()
    {
        if (animator == null || agent == null) return;

        if (_attackRoutine != null || (alreadyAttacked && Time.time < nextAttackTime))
        {
            return;
        }

        alreadyAttacked = false;
        animator.Play(agent.velocity.sqrMagnitude <= Mathf.Epsilon ? IdleAnimation : WalkAnimation);
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
