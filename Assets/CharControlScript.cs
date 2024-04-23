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


        private CharacterController _controller;

        private void Start()
        {
            _controller = GetComponent<CharacterController>();
        }

        // Update is called once per frame
        private void Update()
        {
            if (Input.GetMouseButtonDown(1))
            {
                Ray ray = cam.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitPoint;

                if (Physics.Raycast(ray, out hitPoint))
                {
                    targetDest.transform.position = hitPoint.point;
                    player.SetDestination(hitPoint.point);
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
