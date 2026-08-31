using UnityEngine;
using UnityEngine.AI;

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
        if (agent == null || player == null)
        {
            SetMovementAnimation();
            return;
        }

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

    private void ResolvePlayerReference()
    {
        if (player != null) return;

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
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

        Actor targetActor = player.GetComponentInParent<Actor>();
        if (targetActor != null)
        {
            targetActor.TakeDamage(attackDamage);
        }

        if (animator != null)
        {
            animator.Play(AttackAnimation);
        }

        alreadyAttacked = true;
        nextAttackTime = Time.time + Mathf.Max(0.1f, timeBetweenAttacks);
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
