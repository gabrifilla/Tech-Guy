using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;
    public LayerMask whatIsGround, whatIsPlayer;

    const string IDLE = "Idle";
    const string WALK = "Walk";
    const string ATTACK = "Attack";

    string currentAnimation;

    Animator animator;

    // Patrol
    public Vector3 walkPoint;
    bool walkPointSet;
    public float walkPointRange;

    // Attacking
    public float attackDamage;
    public float timeBetweenAttacks;
    bool alreadyAttacked;

    // States
    public float sightRange, attackRange;
    public bool playerInSightRange, playerInAttackRange;

    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    private CharacterController _controller;
    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }
    void Awake()
    {
        player = GameObject.Find("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        SetAnimations();

        // Check if player in sight and attack range
        playerInSightRange = CheckPlayerInRange(sightRange);
        playerInAttackRange = CheckPlayerInRange(attackRange);

        if (!playerInSightRange && !playerInAttackRange) Patroling();
        if (playerInSightRange && !playerInAttackRange) ChasePlayer();
        if (playerInSightRange && playerInAttackRange) AttackPlayer();
    }

    // CheckPlayerInRange usa Physics.OverlapSphere para obter todos os colisores dentro de um determinado range e verifica se algum deles tem a tag “Player”
    bool CheckPlayerInRange(float range)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, range);
        foreach (Collider collider in colliders)
        {
            if (collider.CompareTag("Player"))
            {
                return true;
            }
        }
        return false;
    }
    void Patroling()
    {
        if (!walkPointSet) SearchWalkpoint();
        if (walkPointSet) agent.SetDestination(walkPoint);

        Vector3 distancetoWalkPoint = transform.position - walkPoint;

        // Walkpoint Reached
        if (distancetoWalkPoint.magnitude < 1f) walkPointSet = false;
    }
    void SearchWalkpoint()
    {
        float randomZ = Random.Range(-walkPointRange, walkPointRange);
        float randomX = Random.Range(-walkPointRange, walkPointRange);

        walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (Physics.Raycast(walkPoint, -transform.up, 2f, whatIsGround)) walkPointSet = true;
    }
    void ChasePlayer()
    {
        agent.SetDestination(player.position);
    }

    void AttackPlayer()
    {
        agent.SetDestination(transform.position);

        transform.LookAt(player);

        if (!alreadyAttacked)
        {
            // Ataque o jogador
            player.GetComponent<Actor>().TakeDamage(attackDamage);
            animator.Play(ATTACK);
            alreadyAttacked = true;

            Invoke(nameof(ResetBusyState), timeBetweenAttacks);

        }
    }
    void ResetBusyState()
    {
        alreadyAttacked = false;
        SetAnimations();
    }

    void SetAnimations()
    {
        if (alreadyAttacked) return;

        if (agent.velocity == Vector3.zero)
        { animator.Play(IDLE); }
        else
        { animator.Play(WALK); }
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
    }

}
