using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CombatReactionController : MonoBehaviour
{
    [Header("Rank")]
    [SerializeField] private EnemyRank rank = EnemyRank.Normal;
    [SerializeField] private bool allowBossAirJuggle;
    [SerializeField] private bool allowBossRagdoll;

    [Header("Poise")]
    [SerializeField] private float maxPoise = 100f;
    [SerializeField] private float armorBreakDuration = 3f;
    [SerializeField] private float poiseRecoveryDelay = 2f;
    [SerializeField] private float poiseRecoveryPerSecond = 35f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string flinchAnimation = "GetHit";
    [SerializeField] private string stunAnimation = "Stun";
    [SerializeField] private Transform visualRoot;

    [Header("Air Juggle Visual")]
    [SerializeField] private bool forceHorizontalVisualOnLaunch = true;
    [SerializeField] private Vector3 horizontalVisualEuler = new Vector3(90f, 0f, 0f);

    [Header("Movement")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rigidbody body;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private float defaultStunDuration = 0.5f;

    [Header("Ragdoll")]
    [SerializeField] private RagdollController ragdollController;

    private float currentPoise;
    private bool armorBroken;
    private float nextPoiseRecoveryTime;
    private Coroutine stunCoroutine;
    private Coroutine armorBreakCoroutine;
    private Coroutine airJuggleVisualCoroutine;
    private Quaternion originalVisualLocalRotation;

    public EnemyRank Rank => rank;
    public bool ArmorBroken => armorBroken;
    public float CurrentPoise => currentPoise;

    public void ConfigureRank(EnemyRank newRank, bool bossAirJuggle = false, bool bossRagdoll = false)
    {
        rank = newRank;
        allowBossAirJuggle = bossAirJuggle;
        allowBossRagdoll = bossRagdoll;
        armorBroken = false;
        currentPoise = Mathf.Max(1f, maxPoise);
    }

    private void Awake()
    {
        currentPoise = Mathf.Max(1f, maxPoise);

        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!body) body = GetComponent<Rigidbody>();
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!ragdollController) ragdollController = GetComponent<RagdollController>();
        if (!visualRoot && animator && animator.transform != transform) visualRoot = animator.transform;
        if (visualRoot) originalVisualLocalRotation = visualRoot.localRotation;
    }

    private void Update()
    {
        RecoverPoise();
    }

    public void ApplyReaction(HitReactionRequest request)
    {
        if (request.ReactionType == HitReactionType.None && request.PoiseDamage <= 0f) return;

        ApplyPoiseDamage(request);
        HitReactionType reactionType = ResolveReactionType(request);
        if (reactionType == HitReactionType.None) return;

        switch (reactionType)
        {
            case HitReactionType.Flinch:
                PlayAnimation(flinchAnimation);
                break;
            case HitReactionType.Stun:
                StartStun(request.StunDuration > 0f ? request.StunDuration : defaultStunDuration);
                break;
            case HitReactionType.Knockback:
                ApplyImpulse(request.HitDirection * request.KnockbackForce);
                PlayAnimation(flinchAnimation);
                break;
            case HitReactionType.Launch:
                ApplyImpulse(request.HitDirection * request.KnockbackForce + Vector3.up * request.LaunchForce);
                float launchDuration = request.StunDuration > 0f ? request.StunDuration : defaultStunDuration;
                StartAirJuggleVisual(launchDuration);
                StartStun(launchDuration);
                break;
            case HitReactionType.Ragdoll:
                if (ragdollController)
                {
                    Vector3 force = request.HitDirection * request.KnockbackForce + Vector3.up * request.LaunchForce;
                    ragdollController.EnableRagdoll(force, request.HitPoint, request.StunDuration);
                }
                break;
        }
    }

    private void ApplyPoiseDamage(HitReactionRequest request)
    {
        if (rank == EnemyRank.Normal || rank == EnemyRank.Elite) return;
        if (rank == EnemyRank.Boss && !allowBossAirJuggle && !allowBossRagdoll) return;

        currentPoise = Mathf.Max(0f, currentPoise - request.PoiseDamage);
        nextPoiseRecoveryTime = Time.time + Mathf.Max(0f, poiseRecoveryDelay);

        if (rank == EnemyRank.Legendary && !armorBroken && (currentPoise <= 0f || request.Strength == HitStrength.Breaker))
        {
            BreakArmor();
        }
    }

    private HitReactionType ResolveReactionType(HitReactionRequest request)
    {
        if (rank == EnemyRank.Boss)
        {
            if ((request.ReactionType == HitReactionType.Launch && allowBossAirJuggle && request.CanAirJuggle) ||
                (request.ReactionType == HitReactionType.Ragdoll && allowBossRagdoll && request.CanRagdoll))
            {
                return request.ReactionType;
            }

            return request.Strength == HitStrength.Breaker ? HitReactionType.Stun : HitReactionType.None;
        }

        if (rank == EnemyRank.Legendary && !armorBroken)
        {
            return currentPoise <= 0f || request.Strength == HitStrength.Breaker
                ? HitReactionType.Stun
                : HitReactionType.None;
        }

        if (request.ReactionType == HitReactionType.Ragdoll)
        {
            return request.CanRagdoll ? HitReactionType.Ragdoll : HitReactionType.Knockback;
        }

        if (request.ReactionType == HitReactionType.Launch)
        {
            if (!request.CanAirJuggle) return HitReactionType.Knockback;
            if (rank == EnemyRank.Normal) return HitReactionType.Launch;
            return IsAtLeast(request.Strength, HitStrength.Medium) ? HitReactionType.Launch : HitReactionType.Flinch;
        }

        if (request.ReactionType == HitReactionType.Knockback && request.Strength == HitStrength.Light && rank == EnemyRank.Elite)
        {
            return HitReactionType.Flinch;
        }

        return request.ReactionType;
    }

    private void BreakArmor()
    {
        armorBroken = true;
        currentPoise = 0f;

        if (armorBreakCoroutine != null)
        {
            StopCoroutine(armorBreakCoroutine);
        }

        armorBreakCoroutine = StartCoroutine(RestoreArmorAfter());
    }

    private IEnumerator RestoreArmorAfter()
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, armorBreakDuration));
        armorBroken = false;
        currentPoise = Mathf.Max(1f, maxPoise);
    }

    private void RecoverPoise()
    {
        if (armorBroken || currentPoise >= maxPoise || Time.time < nextPoiseRecoveryTime) return;

        currentPoise = Mathf.Min(maxPoise, currentPoise + poiseRecoveryPerSecond * Time.deltaTime);
    }

    private void StartStun(float duration)
    {
        if (stunCoroutine != null)
        {
            StopCoroutine(stunCoroutine);
        }

        stunCoroutine = StartCoroutine(StunFor(duration));
    }

    private void StartAirJuggleVisual(float duration)
    {
        if (!forceHorizontalVisualOnLaunch || !visualRoot || visualRoot == transform) return;

        if (airJuggleVisualCoroutine != null)
        {
            StopCoroutine(airJuggleVisualCoroutine);
        }

        airJuggleVisualCoroutine = StartCoroutine(ForceVisualRotationFor(duration));
    }

    private IEnumerator ForceVisualRotationFor(float duration)
    {
        Quaternion targetRotation = originalVisualLocalRotation * Quaternion.Euler(horizontalVisualEuler);
        float endTime = Time.time + Mathf.Max(0.05f, duration);

        while (Time.time < endTime)
        {
            if (visualRoot)
            {
                visualRoot.localRotation = targetRotation;
            }

            yield return null;
        }

        if (visualRoot)
        {
            visualRoot.localRotation = originalVisualLocalRotation;
        }

        airJuggleVisualCoroutine = null;
    }

    private IEnumerator StunFor(float duration)
    {
        bool hadAgent = agent && agent.enabled;
        if (hadAgent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        PlayAnimation(string.IsNullOrWhiteSpace(stunAnimation) ? flinchAnimation : stunAnimation);
        yield return new WaitForSeconds(Mathf.Max(0.05f, duration));

        if (hadAgent && agent)
        {
            agent.isStopped = false;
        }
    }

    private void ApplyImpulse(Vector3 impulse)
    {
        if (impulse.sqrMagnitude <= Mathf.Epsilon) return;

        if (body && !body.isKinematic)
        {
            body.AddForce(impulse, ForceMode.Impulse);
            return;
        }

        if (characterController && characterController.enabled)
        {
            characterController.Move(impulse * Time.deltaTime);
            return;
        }

        transform.position += impulse * Time.deltaTime;
    }

    private void PlayAnimation(string stateName)
    {
        if (animator && !string.IsNullOrWhiteSpace(stateName))
        {
            animator.Play(stateName);
        }
    }

    private static bool IsAtLeast(HitStrength current, HitStrength required)
    {
        return (int)current >= (int)required;
    }
}
