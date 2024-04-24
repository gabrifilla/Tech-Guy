using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;


namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class CharControlScript : MonoBehaviour
    {
        const string ATTACK = "Attack";
        const string PICKUP = "Pickup";

        [Tooltip("Camera")]
        public Camera cam;
        [Tooltip("Player")]
        public NavMeshAgent player;
        [Tooltip("Player Animator")]
        public Animator playerAnimator;
        [Tooltip("Target Destination")]
        public GameObject targetDest;

        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;


        [Header("Movement")]
        [SerializeField] ParticleSystem clickEffect;
        [SerializeField] LayerMask clickableLayer;

        [Header("Attack")]
        [SerializeField] float attackSpeed = 1.5f;
        [SerializeField] float attackDelay = .3f;
        [SerializeField] float attackDistance = 1.5f;
        [SerializeField] int attackDamage = 10;
        [SerializeField] ParticleSystem hitEffect;

        bool playerBusy = false;
        Interactable target;

        private CharacterController _controller;

        void Start()
        {
            _controller = GetComponent<CharacterController>();
        }

        // Update is called once per frame
        void Update()
        {
            FollowTarget();

            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitPoint;

                if (Physics.Raycast(ray, out hitPoint))
                {
                    if (hitPoint.transform.CompareTag("Interactable"))
                    {
                        target = hitPoint.transform.GetComponent<Interactable>();
                        if (!clickEffect)
                        {
                            Instantiate(clickEffect, hitPoint.point += new Vector3(0, 0.1f, 0), clickEffect.transform.rotation);
                        }
                    }
                    else
                    {
                        target = null;

                        targetDest.transform.position = hitPoint.point;
                        player.SetDestination(hitPoint.point);
                        if (!clickEffect)
                        {
                            Instantiate(clickEffect, hitPoint.point += new Vector3(0, 0.1f, 0), clickEffect.transform.rotation);
                        }
                    }

                    
                }
            }

            if (player.velocity != Vector3.zero)
            {
                playerAnimator.SetBool("isWalking", true);
            }
            else if (player.velocity == Vector3.zero)
            {
                playerAnimator.SetBool("isWalking", false);
            }
        }

        void FollowTarget()
        {
            if (target == null) return;

            if (Vector3.Distance(target.transform.position, transform.position) <= attackDistance) { ReachDistance(); }
            else { player.SetDestination(target.transform.position); }
        }
        void ReachDistance()
        {
            player.SetDestination(transform.position);

            if (playerBusy) return;
            playerBusy = true;

            switch (target.interactionType)
            {
                case InteractableType.Enemy:
                    //animator.Play(ATTACK);
                    Invoke(nameof(SendAttack), attackDelay);
                    Invoke(nameof(ResetBusyState), attackSpeed);
                    break;
                case InteractableType.Item:
                    //animator.Play(PICKUP);
                    target.InteractWithItem();
                    target = null;

                    Invoke(nameof(ResetBusyState), 0.5f);
                    break;
            }
        }
        void SendAttack()
        {
            if (target == null) return;

            if(target.myActor.health <= 0) { target = null; return; }

            Instantiate(hitEffect, target.transform.position + new Vector3(0, 1, 0), Quaternion.identity);
            target.GetComponent<EnemyAI>().TakeDamage(attackDamage);

        }
        void ResetBusyState()
        {
            playerBusy = false;
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
}
