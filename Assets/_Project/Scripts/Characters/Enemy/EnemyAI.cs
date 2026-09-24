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
    private CombatGroundRing _attackBoundary, _attackProgress;
    private Actor _owner;
    public bool IsWindingUp { get; private set; }

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
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || player == null || (_owner && _owner.IsDead))
        {
            SetMovementAnimation();
            return;
        }

        if (agent.isStopped) { CancelAttack(); return; }
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

    private void ResolvePlayerReference()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
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
        playerInAttackRange = CheckPlayerInRange(attackRange);
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
        agent.ResetPath();
        FacePlayer();

        if (Time.time < nextAttackTime) return;

        alreadyAttacked = true;
        _attackRoutine = StartCoroutine(TelegraphedAttack());
    }

    private IEnumerator TelegraphedAttack()
    {
        IsWindingUp = true;
        Vector3 origin = transform.position;
        float radius = Mathf.Max(0.1f, attackRange);
        _attackBoundary = CombatGroundRing.Create(null, "Enemy attack boundary", new Color(1, 0.35f, 0.12f));
        _attackProgress = CombatGroundRing.Create(null, "Enemy attack countdown", new Color(1, 0.8f, 0.25f));
        _attackBoundary.Draw(origin, radius, 0.1f);
        float duration = Mathf.Max(0.2f, _attackWindup);
        float elapsed = 0;
        if (animator) animator.Play(IdleAnimation);
        while (elapsed < duration)
        {
            if ((_owner && _owner.IsDead) || !agent || !agent.enabled || !agent.isOnNavMesh || agent.isStopped)
            { IsWindingUp = false; ClearAttackVisuals(); _attackRoutine = null; yield break; }
            _attackProgress.Draw(origin, radius * Mathf.Clamp01(elapsed / duration), 0.14f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        IsWindingUp = false;
        ClearAttackVisuals();
        if (animator) animator.Play(AttackAnimation);
        Actor target = ResolvePlayerActor();
        if (target && !target.IsDead)
        {
            Vector3 delta = target.transform.position - origin;
            float height = Mathf.Abs(delta.y);
            delta.y = 0;
            if (height <= 2f && delta.sqrMagnitude <= radius * radius) target.TakeDamage(attackDamage);
        }
        nextAttackTime = Time.time + Mathf.Max(_attackRecovery, timeBetweenAttacks);
        yield return new WaitForSeconds(Mathf.Max(0.1f, _attackRecovery));
        _attackRoutine = null;
    }

    private void ClearAttackVisuals()
    {
        if (_attackBoundary) { _attackBoundary.gameObject.SetActive(false); Destroy(_attackBoundary.gameObject); }
        if (_attackProgress) { _attackProgress.gameObject.SetActive(false); Destroy(_attackProgress.gameObject); }
    }

    private void CancelAttack()
    {
        if (_attackRoutine != null) StopCoroutine(_attackRoutine);
        _attackRoutine = null;
        IsWindingUp = false;
        alreadyAttacked = false;
        ClearAttackVisuals();
        DeactivateHitbox();
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

        if (alreadyAttacked && Time.time < nextAttackTime)
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
