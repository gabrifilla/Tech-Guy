using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class CharControlScript : MonoBehaviour
{
    const string IDLE = "Idle";
    const string WALK = "Walk";
    const string PICKUP = "Pickup";

    string[] attackAnimations = { "Attack", "Attack2", "Attack3" };

    string currentAnimation;

    public bool isDashing = false;

    CustomActions input;

    NavMeshAgent agent;
    public Animator animator;

    [Header("Movement")]
    [SerializeField] ParticleSystem clickEffect;
    [SerializeField] LayerMask clickableLayers;

    float lookRotationSpeed = 8f;

    [Header("Attack")]
    [SerializeField] public ParticleSystem hitEffect;

    int currentComboCount = 0;

    bool playerBusy = false;
    Interactable target;

    // Adicione uma variável pública para o DashScript
    public DashScript dashScript;

    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    private CharacterController _controller;

    public PlayerActor playerActor;

    void Start()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        input = new CustomActions();
        AssignInputs();
    }

    void AssignInputs()
    {
        input.Main.Move.performed += ctx => ClickToMove();
    }

    void ClickToMove()
    {
        if (!isDashing)
        {
            RaycastHit hit;
            if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit, 100, clickableLayers))
            {
                // Desativa a barra de vida do alvo anterior
                if (target != null)
                {
                    target.GetComponent<Actor>().healthBar.gameObject.SetActive(false);
                }

                if (hit.transform.CompareTag("Interactable"))
                {
                    Interactable interactable = hit.transform.GetComponent<Interactable>();
                    if (interactable.interactionType == InteractableType.Enemy)
                    {
                        target = interactable;
                        // Ativa a barra de vida do novo alvo
                        if (target.GetComponent<Actor>().healthBar != null)
                        {
                            target.GetComponent<Actor>().healthBar.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        target = hit.transform.GetComponent<Interactable>();
                    }

                    if (clickEffect != null)
                    { Instantiate(clickEffect, hit.transform.position + new Vector3(0, 0.1f, 0), clickEffect.transform.rotation); }
                }
                else
                {
                    target = null;

                    agent.destination = hit.point;
                    if (clickEffect != null)
                    { Instantiate(clickEffect, hit.point + new Vector3(0, 0.1f, 0), clickEffect.transform.rotation); }
                }
            }
        }
    }

    void OnEnable()
    { input.Enable(); }

    void OnDisable()
    { input.Disable(); }

    void Update()
    {
        if (!dashScript.isDashing)
        { 
            FollowTarget();
            FaceTarget();
            SetAnimations();
        }
        
    }

    void FollowTarget()
    {
        if (target == null) return;

        if (Vector3.Distance(target.transform.position, transform.position) <= playerActor.weapon.attackDistance)
        { ReachDistance(); }
        else
        { agent.SetDestination(target.transform.position); }
    }

    void FaceTarget()
    {
        if (agent.destination == transform.position) return;

        Vector3 facing = Vector3.zero;
        if (target != null)
        { facing = target.transform.position; }
        else
        { facing = agent.destination; }

        Vector3 direction = (facing - transform.position).normalized;

        // Verifique se a direção não é zero
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * lookRotationSpeed);
        }
    }


    void ReachDistance()
    {
        agent.SetDestination(transform.position);

        if (playerBusy) return;

        playerBusy = true;

        switch (target.interactionType)
        {
            case InteractableType.Enemy:

                animator.Play(attackAnimations[currentComboCount]);

                Invoke(nameof(SendAttack), playerActor.weapon.attackDelay);
                Invoke(nameof(ResetBusyState), playerActor.weapon.attackSpeed);

                if (currentComboCount >= playerActor.weapon.maxComboCount - 1)
                {
                    currentComboCount = 0;
                    playerBusy = true;

                    Invoke(nameof(ResetBusyState), playerActor.weapon.attackSpeed);
                }
                else
                {
                    currentComboCount++;
                    Invoke(nameof(SendAttack), playerActor.weapon.attackDelay);
                    Invoke(nameof(ResetBusyState), playerActor.weapon.attackSpeed);

                }
                break;
            case InteractableType.Item:

                target.InteractWithItem();
                target = null;

                Invoke(nameof(ResetBusyState), 0.5f);
                break;
        }
    }

    void SendAttack()
    {
        if (playerActor.weapon != null)
        {
            playerActor.weapon.Attack(playerActor.handTransform, playerActor.weapon.attackDamage, hitEffect);
        }
    }


    void ResetBusyState()
    {
        playerBusy = false;
        SetAnimations();
    }

    void SetAnimations()
    {
        if (playerBusy) return;

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