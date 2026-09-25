using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Lost Ark-style Stance/Stagger controller.
///
/// Every hit applies stance damage and an immediate reaction (Push or Stagger). Push is a small
/// physical nudge that keeps the enemy near the player; Stagger briefly interrupts its action.
/// Hard crowd-control (Stun / KnockUp / Knockback) only happens on a Stance Break — when the stance
/// pool is depleted — and only if the hit requests that effect and the enemy is not resistant/immune.
///
/// Control is locked (the NavMeshAgent is stopped, which EnemyAI already treats as "cannot act")
/// during Stun and while airborne from a KnockUp, and restored when the effect ends.
/// </summary>
public class CombatReactionController : MonoBehaviour
{
    [Header("Rank")]
    [SerializeField] private EnemyRank rank = EnemyRank.Normal;

    [Header("Stance")]
    [SerializeField, Min(1f)] private float maxStance = 100f;
    [SerializeField, Min(0f)] private float stanceDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float stanceRecoveryPerSecond = 25f;
    [SerializeField, Min(0f)] private float stanceRecoveryDelay = 2.5f;
    [Tooltip("Cooldown after a Stance Break before the enemy can be broken again, so it isn't chain-locked forever.")]
    [SerializeField, Min(0f)] private float breakImmunityDuration = 1.5f;

    [Header("Crowd-control resistance (0 = full effect, 1 = immune)")]
    [SerializeField, Range(0f, 1f)] private float staggerResistance;
    [SerializeField, Range(0f, 1f)] private float stunResistance;
    [SerializeField, Range(0f, 1f)] private float knockUpResistance;
    [SerializeField, Range(0f, 1f)] private float knockbackResistance;

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

    private float currentStance;
    private float nextStanceRecoveryTime;
    private float breakImmuneUntil;
    private bool controlLocked;
    private Coroutine controlLockCoroutine;
    private Coroutine pushCoroutine;
    private Coroutine airJuggleVisualCoroutine;
    private Quaternion originalVisualLocalRotation;

    public EnemyRank Rank => rank;
    public float CurrentStance => currentStance;
    public float MaxStance => maxStance;
    public float StanceRatio => maxStance > 0f ? Mathf.Clamp01(currentStance / maxStance) : 0f;
    /// <summary>True while the enemy is stunned or airborne and cannot act.</summary>
    public bool IsControlLocked => controlLocked;
    /// <summary>True while a stun/knock-up lock is active (used to draw the dizzy stars overlay).</summary>
    public bool IsStunned => controlLocked;
    /// <summary>Only tougher enemies show a stance bar; trash mobs break too fast to bother.</summary>
    public bool ShouldShowStanceBar => rank != EnemyRank.Normal;
    /// <summary>Raised the moment stance is broken, for feedback (popup, flash). Passes the world hit point.</summary>
    public event System.Action<Vector3> StanceBroken;

    /// <summary>Legacy entry point. Rank-only config derives sensible stance defaults per rank.</summary>
    public void ConfigureRank(EnemyRank newRank, bool bossAirJuggle = false, bool bossRagdoll = false)
    {
        rank = newRank;
        ApplyRankDefaults(newRank);
        currentStance = maxStance;
    }

    /// <summary>Full stance configuration, typically sourced from an <see cref="EnemyProfile"/>.</summary>
    public void ConfigureStance(EnemyRank newRank, float newMaxStance, float newStanceDamageMultiplier,
        float newRecoveryPerSecond, float newRecoveryDelay,
        float newStaggerResistance, float newStunResistance, float newKnockUpResistance, float newKnockbackResistance)
    {
        rank = newRank;
        maxStance = Mathf.Max(1f, newMaxStance);
        stanceDamageMultiplier = Mathf.Max(0f, newStanceDamageMultiplier);
        stanceRecoveryPerSecond = Mathf.Max(0f, newRecoveryPerSecond);
        stanceRecoveryDelay = Mathf.Max(0f, newRecoveryDelay);
        staggerResistance = Mathf.Clamp01(newStaggerResistance);
        stunResistance = Mathf.Clamp01(newStunResistance);
        knockUpResistance = Mathf.Clamp01(newKnockUpResistance);
        knockbackResistance = Mathf.Clamp01(newKnockbackResistance);
        currentStance = maxStance;
    }

    private void ApplyRankDefaults(EnemyRank newRank)
    {
        // Higher ranks are naturally harder to stagger and progressively immune to hard CC.
        switch (newRank)
        {
            case EnemyRank.Normal:
                maxStance = 100f; stanceDamageMultiplier = 1f;
                staggerResistance = 0f; stunResistance = 0f; knockUpResistance = 0f; knockbackResistance = 0f;
                break;
            case EnemyRank.Elite:
                maxStance = 220f; stanceDamageMultiplier = 0.8f;
                staggerResistance = 0.3f; stunResistance = 0.25f; knockUpResistance = 0.4f; knockbackResistance = 0.3f;
                break;
            case EnemyRank.Legendary:
                maxStance = 400f; stanceDamageMultiplier = 0.6f;
                staggerResistance = 0.6f; stunResistance = 0.5f; knockUpResistance = 0.75f; knockbackResistance = 0.6f;
                break;
            case EnemyRank.Boss:
                maxStance = 900f; stanceDamageMultiplier = 0.5f;
                staggerResistance = 0.85f; stunResistance = 0.7f; knockUpResistance = 1f; knockbackResistance = 1f;
                break;
        }
    }

    private void Awake()
    {
        currentStance = maxStance;
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!body) body = GetComponent<Rigidbody>();
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!visualRoot && animator && animator.transform != transform) visualRoot = animator.transform;
        if (visualRoot) originalVisualLocalRotation = visualRoot.localRotation;
    }

    private void Update()
    {
        RecoverStance();
    }

    public void ApplyReaction(HitReactionRequest request)
    {
        // Immediate reaction (never hard CC): Push nudges, Stagger briefly interrupts.
        switch (request.ReactionType)
        {
            case HitReactionType.Push:
                if (!controlLocked) ApplyPush(request.HitDirection, request.PushDistance);
                PlayAnimation(flinchAnimation);
                break;
            case HitReactionType.Stagger:
                if (staggerResistance < 1f)
                {
                    if (!controlLocked) ApplyPush(request.HitDirection, request.PushDistance);
                    PlayAnimation(flinchAnimation);
                    InterruptCurrentAction();
                }
                break;
        }

        // Stance damage + possible Stance Break.
        if (request.StanceDamage > 0f) ApplyStanceDamage(request);
    }

    private void ApplyStanceDamage(HitReactionRequest request)
    {
        if (Time.time < breakImmuneUntil) return; // still recovering from the previous break

        currentStance = Mathf.Max(0f, currentStance - request.StanceDamage * stanceDamageMultiplier);
        nextStanceRecoveryTime = Time.time + stanceRecoveryDelay;

        if (currentStance > 0f) return;

        // Stance Break feedback (popup + flash + event). Tougher enemies get the announced break.
        Vector3 breakPoint = transform.position + Vector3.up * 2f;
        StanceBroken?.Invoke(breakPoint);
        if (ShouldShowStanceBar) StanceBreakPopup.Show(breakPoint);
        PlayBreakFlash();

        // Stance Break: apply the attack's chosen hard CC, gated by this enemy's resistances.
        TriggerStanceBreak(request);
        currentStance = maxStance;
        breakImmuneUntil = Time.time + breakImmunityDuration;
    }

    private Coroutine breakFlashCoroutine;

    /// <summary>A quick punchy scale-pop of the visual to sell the stance break.</summary>
    private void PlayBreakFlash()
    {
        Transform target = visualRoot ? visualRoot : transform;
        if (breakFlashCoroutine != null) StopCoroutine(breakFlashCoroutine);
        breakFlashCoroutine = StartCoroutine(BreakFlash(target));
    }

    private IEnumerator BreakFlash(Transform target)
    {
        Vector3 baseScale = target.localScale;
        float elapsed = 0f;
        const float duration = 0.25f;
        while (elapsed < duration && target)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // Pop out then settle back.
            float pop = 1f + 0.25f * Mathf.Sin(t * Mathf.PI);
            target.localScale = baseScale * pop;
            yield return null;
        }
        if (target) target.localScale = baseScale;
        breakFlashCoroutine = null;
    }

    private void TriggerStanceBreak(HitReactionRequest request)
    {
        switch (request.BreakEffect)
        {
            case StanceBreakEffect.Stun:
                if (stunResistance >= 1f) { InterruptCurrentAction(); return; }
                StartControlLock(ScaleDuration(request.StunDuration, stunResistance), airborne: false);
                break;

            case StanceBreakEffect.KnockUp:
                if (knockUpResistance >= 1f)
                {
                    // Immune to being launched: fall back to a stun of the same window if not stun-immune.
                    if (stunResistance < 1f) StartControlLock(ScaleDuration(request.StunDuration, stunResistance), airborne: false);
                    else InterruptCurrentAction();
                    return;
                }
                StartKnockUp(request);
                break;

            case StanceBreakEffect.Knockback:
                if (knockbackResistance >= 1f) { InterruptCurrentAction(); return; }
                float distance = request.KnockbackDistance * (1f - knockbackResistance);
                ApplyPush(request.HitDirection, distance, 0.28f);
                PlayAnimation(flinchAnimation);
                break;

            default:
                InterruptCurrentAction();
                break;
        }
    }

    private static float ScaleDuration(float duration, float resistance) => duration * (1f - resistance);

    // --- Immediate reactions -------------------------------------------------

    private void InterruptCurrentAction()
    {
        // A momentary agent stop makes EnemyAI drop its wind-up/attack without a full lock.
        if (controlLocked) return;
        PlayAnimation(flinchAnimation);
        if (agent && agent.enabled && agent.isOnNavMesh) agent.ResetPath();
    }

    private void ApplyPush(Vector3 direction, float distance, float duration = 0.14f)
    {
        if (distance <= 0f) return;
        Vector3 flat = direction; flat.y = 0f;
        if (flat.sqrMagnitude <= Mathf.Epsilon) return;

        if (body && !body.isKinematic)
        {
            body.AddForce(flat.normalized * distance, ForceMode.Impulse);
            return;
        }
        if (pushCoroutine != null) StopCoroutine(pushCoroutine);
        pushCoroutine = StartCoroutine(PushOverTime(flat.normalized * distance, duration));
    }

    private IEnumerator PushOverTime(Vector3 offset, float duration)
    {
        float distance = offset.magnitude;
        Vector3 dir = offset / distance;
        float elapsed = 0f, travelled = 0f;
        bool pausedAgent = agent && agent.enabled && agent.isOnNavMesh;
        if (pausedAgent) agent.isStopped = true;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float target = distance * (1f - (1f - t) * (1f - t));
            MoveStep(dir * (target - travelled));
            travelled = target;
            yield return null;
        }
        if (pausedAgent && agent && !controlLocked) agent.isStopped = false;
        pushCoroutine = null;
    }

    // --- Hard CC: stun / knock-up -------------------------------------------

    private void StartControlLock(float duration, bool airborne)
    {
        if (controlLockCoroutine != null) StopCoroutine(controlLockCoroutine);
        controlLockCoroutine = StartCoroutine(ControlLockFor(Mathf.Max(0.1f, duration), airborne));
    }

    private IEnumerator ControlLockFor(float duration, bool airborne)
    {
        controlLocked = true;
        bool hadAgent = agent && agent.enabled;
        if (hadAgent)
        {
            if (agent.isOnNavMesh) agent.ResetPath();
            agent.isStopped = true;
        }
        PlayAnimation(string.IsNullOrWhiteSpace(stunAnimation) ? flinchAnimation : stunAnimation);
        if (airborne) StartAirJuggleVisual(duration);

        yield return new WaitForSeconds(duration);

        if (hadAgent && agent) agent.isStopped = false;
        controlLocked = false;
        controlLockCoroutine = null;
    }

    private void StartKnockUp(HitReactionRequest request)
    {
        float airTime = ScaleDuration(request.StunDuration > 0f ? request.StunDuration : 1f, knockUpResistance);
        float height = request.KnockUpHeight * (1f - knockUpResistance);
        StartControlLock(airTime, airborne: true);
        if (height > 0f) StartCoroutine(HopArc(height, airTime));
    }

    private IEnumerator HopArc(float height, float duration)
    {
        // A simple parabolic hop of the visual so the enemy visibly leaves the ground and lands.
        Transform hop = visualRoot ? visualRoot : transform;
        Vector3 baseLocal = hop.localPosition;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float y = 4f * height * t * (1f - t); // peaks at mid-flight, returns to 0 on land
            hop.localPosition = baseLocal + Vector3.up * y;
            yield return null;
        }
        hop.localPosition = baseLocal;
    }

    private void StartAirJuggleVisual(float duration)
    {
        if (!forceHorizontalVisualOnLaunch || !visualRoot || visualRoot == transform) return;
        if (airJuggleVisualCoroutine != null) StopCoroutine(airJuggleVisualCoroutine);
        airJuggleVisualCoroutine = StartCoroutine(ForceVisualRotationFor(duration));
    }

    private IEnumerator ForceVisualRotationFor(float duration)
    {
        Quaternion targetRotation = originalVisualLocalRotation * Quaternion.Euler(horizontalVisualEuler);
        float endTime = Time.time + Mathf.Max(0.05f, duration);
        while (Time.time < endTime)
        {
            if (visualRoot) visualRoot.localRotation = targetRotation;
            yield return null;
        }
        if (visualRoot) visualRoot.localRotation = originalVisualLocalRotation;
        airJuggleVisualCoroutine = null;
    }

    // --- Stance recovery & movement -----------------------------------------

    private void RecoverStance()
    {
        if (currentStance >= maxStance || Time.time < nextStanceRecoveryTime) return;
        currentStance = Mathf.Min(maxStance, currentStance + stanceRecoveryPerSecond * Time.deltaTime);
    }

    private void MoveStep(Vector3 delta)
    {
        if (agent && agent.enabled && agent.isOnNavMesh) { agent.Move(delta); return; }
        if (characterController && characterController.enabled) { characterController.Move(delta); return; }
        transform.position += delta;
    }

    private void PlayAnimation(string stateName)
    {
        if (animator && !string.IsNullOrWhiteSpace(stateName)) animator.Play(stateName);
    }

    // --- Dizzy stars overlay while stunned ----------------------------------

    private Camera _uiCamera;
    private GUIStyle _starStyle;

    private void OnGUI()
    {
        if (!controlLocked) return;
        if (!_uiCamera) _uiCamera = Camera.main;
        if (!_uiCamera) return;

        // Anchor the stars just above the enemy's head, following its scale.
        float headHeight = 2.9f * Mathf.Max(0.2f, transform.lossyScale.y);
        Vector3 world = transform.position + Vector3.up * headHeight;
        Vector3 screen = _uiCamera.WorldToScreenPoint(world);
        if (screen.z <= 0f) return;

        if (_starStyle == null)
        {
            _starStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }
        float scale = Mathf.Clamp(Screen.height / 900f, 0.8f, 1.5f);
        _starStyle.fontSize = Mathf.RoundToInt(20 * scale);
        _starStyle.normal.textColor = new Color(1f, 0.9f, 0.35f);

        Vector2 center = new Vector2(screen.x, Screen.height - screen.y);
        // Three stars orbiting the head, spinning over time.
        const int stars = 3;
        float radius = 18f * scale;
        float spin = Time.time * 320f;
        for (int i = 0; i < stars; i++)
        {
            float ang = (spin + i * (360f / stars)) * Mathf.Deg2Rad;
            var pos = new Vector2(center.x + Mathf.Cos(ang) * radius, center.y + Mathf.Sin(ang) * radius * 0.45f);
            GUI.Label(new Rect(pos.x - 12 * scale, pos.y - 12 * scale, 24 * scale, 24 * scale), "\u2605", _starStyle);
        }
    }
}
