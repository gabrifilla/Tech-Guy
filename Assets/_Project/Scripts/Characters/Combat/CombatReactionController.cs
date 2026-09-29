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
/// during Stun and while airborne from a KnockUp, and restored when the effect ends. While control
/// is locked, additional immediate displacement (Micro_Displacement / Push) from subsequent hits is
/// suppressed via the pure <see cref="ControlLockGate"/> and re-allowed once the lock ends
/// (Requisito 4.5).
/// </summary>
public class CombatReactionController : MonoBehaviour
{
    [Header("Rank")]
    [SerializeField] private EnemyRank rank = EnemyRank.Normal;
    [Tooltip("How long the post-break vulnerability window stays open, treating the enemy as one rarity lower (Elite->Normal etc.). Must be greater than 0 (Requisito 3.3).")]
    [SerializeField, Min(VulnerabilityWindowBounds.MinSeconds)] private float vulnerabilityWindowSeconds = 4f;

    [Header("Stance")]
    [SerializeField, Min(1f)] private float maxStance = 100f;
    [Tooltip("Multiplier applied to incoming stance damage. Clamped to [0, 5] (Requisito 2.4).")]
    [SerializeField, Range(StanceBreakBounds.MinStanceDamageMultiplier, StanceBreakBounds.MaxStanceDamageMultiplier)]
    private float stanceDamageMultiplier = 1f;
    [SerializeField, Min(0f)] private float stanceRecoveryPerSecond = 25f;
    [Tooltip("Delay before stance starts recovering. Clamped to [0, 10] s (Requisito 2.7).")]
    [SerializeField, Range(StanceBreakBounds.MinRecoveryDelaySeconds, StanceBreakBounds.MaxRecoveryDelaySeconds)]
    private float stanceRecoveryDelay = 2.5f;
    [Tooltip("Cooldown after a Stance Break before the enemy can be broken again, so it isn't chain-locked forever. Clamped to [0.1, 10] s (Requisito 2.6).")]
    [SerializeField, Range(StanceBreakBounds.MinBreakImmunitySeconds, StanceBreakBounds.MaxBreakImmunitySeconds)]
    private float breakImmunityDuration = 1.5f;

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
    // Post-break vulnerability window (Requisito 3.3-3.5): all the timing/downgrade logic lives in the
    // pure VulnerabilityWindow so this MonoBehaviour stays thin. It opens on a stance break and closes
    // by time; while open the enemy is treated as one rarity lower via EffectiveRank.
    private readonly VulnerabilityWindow vulnerabilityWindow = new VulnerabilityWindow();
    // Rank-resisted interruption-stagger accumulator (Requisito 7.8 / Property 24): interruption
    // abilities do not stagger/break outright — their stagger value is accumulated here and a stagger
    // or stance break only fires once the running total reaches the rank threshold (derived from the
    // enemy's rank and stance resistance). Below the threshold the enemy's state is preserved. All the
    // accumulation/threshold logic lives in the pure RankInterruptStagger so it stays scene-free.
    private readonly RankInterruptStagger interruptStagger = new RankInterruptStagger();
    private EnemyAI _enemyAI;
    private SectorBoss _sectorBoss;
    private Coroutine controlLockCoroutine;
    private Coroutine pushCoroutine;
    private Coroutine airJuggleVisualCoroutine;
    private Quaternion originalVisualLocalRotation;

    public EnemyRank Rank => rank;
    /// <summary>
    /// The rank the enemy currently reacts as: one step lower while the post-break vulnerability
    /// window is open (Elite→Normal etc.), otherwise the real <see cref="Rank"/> (Requisito 3.3).
    /// Rank-differentiated logic (Legendary R3.4, Boss R3.5) should consult this, not <see cref="Rank"/>,
    /// so a broken enemy exposes the intended opening.
    /// </summary>
    public EnemyRank EffectiveRank => vulnerabilityWindow.EffectiveRank;
    /// <summary>True while the post-break vulnerability window is open (Requisito 3.3).</summary>
    public bool IsVulnerable => vulnerabilityWindow.IsOpen;
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
        vulnerabilityWindow.SetBaseRank(newRank);
        // Keep the interruption-stagger threshold in sync with the rank + stagger resistance (R7.8).
        interruptStagger.Configure(newRank, staggerResistance);
        currentStance = maxStance;
    }

    /// <summary>Full stance configuration, typically sourced from an <see cref="EnemyProfile"/>.</summary>
    public void ConfigureStance(EnemyRank newRank, float newMaxStance, float newStanceDamageMultiplier,
        float newRecoveryPerSecond, float newRecoveryDelay,
        float newStaggerResistance, float newStunResistance, float newKnockUpResistance, float newKnockbackResistance)
    {
        rank = newRank;
        maxStance = Mathf.Max(1f, newMaxStance);
        // Stance-break channel values are pinned to their data-driven ranges regardless of the profile
        // source: multiplier in [0, 5] (Requisito 2.4), recovery delay in [0, 10] s (Requisito 2.7).
        stanceDamageMultiplier = StanceBreakBounds.ClampStanceDamageMultiplier(newStanceDamageMultiplier);
        stanceRecoveryPerSecond = Mathf.Max(0f, newRecoveryPerSecond);
        stanceRecoveryDelay = StanceBreakBounds.ClampRecoveryDelaySeconds(newRecoveryDelay);
        staggerResistance = Mathf.Clamp01(newStaggerResistance);
        stunResistance = Mathf.Clamp01(newStunResistance);
        knockUpResistance = Mathf.Clamp01(newKnockUpResistance);
        knockbackResistance = Mathf.Clamp01(newKnockbackResistance);
        vulnerabilityWindow.SetBaseRank(newRank);
        // The interruption-stagger threshold scales with the rank and the (clamped) stagger resistance (R7.8).
        interruptStagger.Configure(newRank, staggerResistance);
        currentStance = maxStance;
    }

    private void ApplyRankDefaults(EnemyRank newRank)
    {
        // Data-driven per-rank defaults (Requisito 3.6): the stance pool, stance-damage multiplier,
        // recovery and the four crowd-control resistances all come from the shared RankReactionDefaults
        // table (higher ranks are naturally harder to stagger and progressively immune to hard CC),
        // plus the post-break vulnerability window (> 0, R3.3) and immunity window (0.1–10.0 s, R2.6).
        // The table already pins every value to its shared valid range, so this applies them directly.
        RankReactionDefaults.Defaults defaults = RankReactionDefaults.For(newRank);
        maxStance = defaults.MaxStance;
        stanceDamageMultiplier = defaults.StanceDamageMultiplier;
        stanceRecoveryPerSecond = defaults.StanceRecoveryPerSecond;
        stanceRecoveryDelay = defaults.StanceRecoveryDelay;
        staggerResistance = defaults.StaggerResistance;
        stunResistance = defaults.StunResistance;
        knockUpResistance = defaults.KnockUpResistance;
        knockbackResistance = defaults.KnockbackResistance;
        vulnerabilityWindowSeconds = defaults.VulnerabilityWindowSeconds;
        breakImmunityDuration = defaults.BreakImmunitySeconds;
        // Sync the interruption-stagger threshold to the rank defaults' rank + stagger resistance (R7.8).
        interruptStagger.Configure(newRank, staggerResistance);
    }

    private void Awake()
    {
        currentStance = maxStance;
        // Sync the vulnerability window with the Inspector-configured rank so EffectiveRank is correct
        // before any stance break (e.g. enemies placed directly in a scene without ConfigureRank).
        vulnerabilityWindow.SetBaseRank(rank);
        // Seed the interruption-stagger threshold from the Inspector-configured rank + stagger
        // resistance so it is correct for enemies placed directly in a scene without ConfigureRank (R7.8).
        interruptStagger.Configure(rank, staggerResistance);
        _enemyAI = GetComponent<EnemyAI>();
        _sectorBoss = GetComponent<SectorBoss>();
        if (!animator) animator = GetComponent<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!body) body = GetComponent<Rigidbody>();
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!visualRoot && animator && animator.transform != transform) visualRoot = animator.transform;
        if (visualRoot) originalVisualLocalRotation = visualRoot.localRotation;
    }

    /// <summary>
    /// Pins the stance-break channel values to their data-driven ranges when edited in the Inspector
    /// (Requisitos 2.4, 2.6, 2.7): multiplier in [0, 5], immunity window in [0.1, 10] s, recovery
    /// delay in [0, 10] s. The Range attributes already gate the sliders; this guards values set
    /// programmatically or via serialization so the runtime invariants always hold.
    /// </summary>
    private void OnValidate()
    {
        stanceDamageMultiplier = StanceBreakBounds.ClampStanceDamageMultiplier(stanceDamageMultiplier);
        breakImmunityDuration = StanceBreakBounds.ClampBreakImmunitySeconds(breakImmunityDuration);
        stanceRecoveryDelay = StanceBreakBounds.ClampRecoveryDelaySeconds(stanceRecoveryDelay);
        // The vulnerability window must stay a real, > 0 opening (Requisito 3.3).
        vulnerabilityWindowSeconds = VulnerabilityWindowBounds.ClampSeconds(vulnerabilityWindowSeconds);
    }

    private void Update()
    {
        RecoverStance();
        // Close the vulnerability window once its configured duration has elapsed, restoring the
        // effective rank to the enemy's real rank (Requisito 3.3). Driven by the controller's own
        // clock (Time.time) so the pure VulnerabilityWindow stays scene-free.
        vulnerabilityWindow.Update(Time.time);
    }

    public void ApplyReaction(HitReactionRequest request)
    {
        // The four reaction channels are processed independently and none reads another's state
        // (Requisito 2.1 / Property 1): (1) life damage lives outside this component; (2) the
        // immediate reaction below; (3) stance damage in ApplyStanceDamage; (4) the Stance Break in
        // TriggerStanceBreak. Neutralising one channel (ReactionType=None, StanceDamage=0 or
        // BreakEffect=None) leaves the others' observable outcome unchanged.

        // Channel 2 — immediate reaction (never hard CC): Push nudges, Stagger briefly interrupts.
        // The displacement is clamped for readability (<= 0.5 m, <= 15 deg per hit, Requisito 2.2);
        // a None reaction produces no displacement/rotation/interruption (Requisito 2.3).
        //
        // WHILE the enemy is control-locked (stunned or airborne), additional Micro_Displacement and
        // Push from the immediate reaction are suppressed so it is not shoved out of the readable
        // stun/juggle; the pure ControlLockGate makes that decision and re-allows displacement once
        // the lock ends (Requisito 4.5). The flinch/interruption feedback still plays — only the
        // displacement is gated. Launch (hard CC) is a separate channel resolved on Stance Break.
        bool allowDisplacement = ControlLockGate.AllowsImmediateDisplacement(controlLocked);
        switch (request.ReactionType)
        {
            case HitReactionType.Push:
                if (allowDisplacement) ApplyImmediateReaction(request.ReactionType, request.HitDirection, request.PushDistance);
                PlayAnimation(flinchAnimation);
                break;
            case HitReactionType.Stagger:
                if (staggerResistance < 1f)
                {
                    if (allowDisplacement) ApplyImmediateReaction(request.ReactionType, request.HitDirection, request.PushDistance);
                    PlayAnimation(flinchAnimation);
                    InterruptCurrentAction();
                }
                break;
            case HitReactionType.None:
            default:
                // No displacement, rotation nor interruption for this channel (Requisito 2.3).
                break;
        }

        // Channel 3 (+ possible channel 4) — stance damage and Stance Break.
        if (request.StanceDamage > 0f) ApplyStanceDamage(request);
    }

    /// <summary>
    /// Applies an interruption ability to this enemy, combining the interruptible-window gate with the
    /// rank-resisted stagger accumulation (Requisitos 7.6, 7.7, 7.8 / Properties 23-24). Reuses the
    /// existing interruption hooks — it never introduces a parallel damage/CC channel.
    ///
    /// The window decision is owned by <see cref="EnemyAI.TryInterruptAttack"/> (pure
    /// <see cref="InterruptWindowGate"/>): if the enemy is inside its interruptible window the ability
    /// cancels the attack and suppresses its pending beat in the same frame (Requisito 7.6). If it is
    /// not (no attack in progress, or the beat already resolved) the attack/beat is preserved and the
    /// result reports a no-interruption reaction for feedback (Requisito 7.7).
    ///
    /// Independently, the interruption's <paramref name="staggerValue"/> is accumulated through the
    /// pure <see cref="RankInterruptStagger"/>: stagger or a stance break is applied only once the
    /// running total reaches the rank threshold; below it the enemy's state is preserved (Requisito
    /// 7.8). When the threshold fires this drives the same <see cref="ApplyStanceDamage"/> /
    /// <see cref="TriggerStanceBreak"/> path a normal stance hit uses, so resistances and the
    /// same-frame break rule (Requisito 2.5/2.8) still apply. A threshold-firing interruption that
    /// carries no stance-break request resolves as a plain interrupt (stagger).
    /// </summary>
    /// <param name="request">The interruption hit (its stance damage/break drive a threshold-triggered break).</param>
    /// <param name="staggerValue">The interruption's stagger contribution toward the rank threshold (R7.8).</param>
    /// <returns>What the interruptible-window gate decided this frame (Requisito 7.6/7.7).</returns>
    public InterruptWindowGate.Decision ApplyInterruptStagger(HitReactionRequest request, float staggerValue)
    {
        // (1) Interruptible-window gate (R7.6/R7.7): reuse EnemyAI's conditional interrupt so the
        // cancel + same-frame beat suppression flows through the one CancelAttack choke point.
        InterruptWindowGate.Decision decision = _enemyAI
            ? _enemyAI.TryInterruptAttack()
            : InterruptWindowGate.Resolve(false);

        // (2) Rank-resisted accumulation (R7.8): stagger/break only once the total meets the threshold;
        // below it the state is preserved. A post-break immune enemy still accumulates but the break
        // itself is gated by the same immunity window ApplyStanceDamage enforces.
        if (interruptStagger.Accumulate(staggerValue) == RankInterruptStagger.Outcome.StaggerOrBreak)
        {
            if (request.StanceDamage > 0f || request.BreakEffect != StanceBreakEffect.None)
            {
                // Route through the normal stance path so the break (if any) resolves in the same
                // frame and through the enemy's resistances (R2.5/R2.8).
                ApplyStanceDamage(request);
            }
            else if (!controlLocked)
            {
                // No stance-break payload: the threshold-reached interruption is a plain stagger.
                InterruptCurrentAction();
            }
        }

        return decision;
    }

    /// <summary>
    /// Applies the immediate reaction nudge with the shared readability clamp (Requisito 2.2): the
    /// resulting displacement is at most <see cref="ImmediateReactionClamp.MaxDisplacementMeters"/> and
    /// any facing change at most <see cref="ImmediateReactionClamp.MaxRotationDegrees"/> per hit,
    /// regardless of the requested <paramref name="requestedDistance"/>. A None reaction never reaches
    /// here, so it produces no displacement or rotation (Requisito 2.3).
    /// </summary>
    private void ApplyImmediateReaction(HitReactionType reactionType, Vector3 direction, float requestedDistance)
    {
        float clampedDistance = ImmediateReactionClamp.ClampPush(reactionType, requestedDistance);
        if (clampedDistance > 0f) ApplyPush(direction, clampedDistance);
        ApplyReactionRotation(reactionType, direction);
    }

    /// <summary>
    /// Turns the enemy a little towards the hit direction, clamped to the readability rotation limit
    /// (Requisito 2.2). The facing delta between the current forward and the hit direction is capped at
    /// <see cref="ImmediateReactionClamp.MaxRotationDegrees"/> so a single hit never spins the enemy.
    /// </summary>
    private void ApplyReactionRotation(HitReactionType reactionType, Vector3 direction)
    {
        if (!ImmediateReactionClamp.ProducesReaction(reactionType)) return;

        Vector3 flat = direction; flat.y = 0f;
        if (flat.sqrMagnitude <= Mathf.Epsilon) return;

        Transform target = visualRoot ? visualRoot : transform;
        Vector3 desiredForward = flat.normalized;
        float signedDelta = Vector3.SignedAngle(target.forward, desiredForward, Vector3.up);
        float clampedDelta = ImmediateReactionClamp.ClampRotation(reactionType, signedDelta);
        if (Mathf.Approximately(clampedDelta, 0f)) return;

        target.rotation = Quaternion.AngleAxis(clampedDelta, Vector3.up) * target.rotation;
    }

    private void ApplyStanceDamage(HitReactionRequest request)
    {
        // Post-break immunity window (Requisito 2.6): while immune, ignore ALL additional stance
        // damage so a broken enemy isn't instantly re-broken. The window length is clamped to
        // [0.1, 10] s wherever breakImmuneUntil is armed below.
        if (Time.time < breakImmuneUntil) return;

        // Stance subtraction (Requisito 2.4): max(0, current − StanceDamage × multiplier), with the
        // multiplier held in [0, 5] by the shared bounds. The reserve never drops below 0.
        currentStance = StanceBreakBounds.SubtractStance(currentStance, request.StanceDamage, stanceDamageMultiplier);
        nextStanceRecoveryTime = Time.time + StanceBreakBounds.ClampRecoveryDelaySeconds(stanceRecoveryDelay);

        if (currentStance > 0f) return;

        // Stance Break feedback (popup + flash + event). Tougher enemies get the announced break.
        Vector3 breakPoint = transform.position + Vector3.up * 2f;
        StanceBroken?.Invoke(breakPoint);
        if (ShouldShowStanceBar) StanceBreakPopup.Show(breakPoint);
        PlayBreakFlash();

        // Stance Break resolved in the SAME step the reserve hits 0 (Requisito 2.5): apply the
        // attack's chosen hard CC, gated by this enemy's resistances.
        TriggerStanceBreak(request);

        // On break, restore the reserve to max and defer recovery (Requisito 2.7). Immunity and
        // recovery-delay windows are pinned to their ranges ([0.1, 10] s and [0, 10] s) so a broken
        // enemy gets a readable, bounded opening (Requisito 2.6 / 2.7).
        currentStance = maxStance;
        breakImmuneUntil = Time.time + StanceBreakBounds.ClampBreakImmunitySeconds(breakImmunityDuration);
        nextStanceRecoveryTime = Time.time + StanceBreakBounds.ClampRecoveryDelaySeconds(stanceRecoveryDelay);
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
        if (!_sectorBoss) _sectorBoss = GetComponent<SectorBoss>();
        if (_sectorBoss) _sectorBoss.InterruptAttack();

        // Open the post-break vulnerability window (Requisito 3.3-3.5): for the configured (> 0)
        // duration the enemy reacts as one rarity lower (Elite→Normal, Legendary→Elite, Boss→
        // Legendary) via EffectiveRank, giving the player a readable opening. The window closes by
        // time in Update. It opens on the break itself, independent of how the hard-CC effect below
        // resolves against resistances — a broken enemy is exposed even if it resisted the CC.
        vulnerabilityWindow.SetBaseRank(rank);
        vulnerabilityWindow.Open(Time.time, VulnerabilityWindowBounds.ClampSeconds(vulnerabilityWindowSeconds));

        // Resolve the actually-applied effect through the deterministic degrade ladder
        // (Requisito 2.8 / Property 7): the requested effect when not fully immune to it, otherwise
        // the lowest-severity effect down the KnockUp→Stun ladder the enemy is not fully immune to,
        // or None. The pure BreakEffectResistance helper owns that decision so it can be exercised
        // scene-free; this method only carries out the resolved effect.
        StanceBreakEffect effect = BreakEffectResistance.Resolve(
            request.BreakEffect, stunResistance, knockUpResistance, knockbackResistance);

        switch (effect)
        {
            case StanceBreakEffect.Stun:
                // Reached either by a requested Stun or by a KnockUp degraded to Stun; the airborne
                // lock window uses the same duration scaled by the stun resistance.
                StartControlLock(ScaleDuration(request.StunDuration, stunResistance), airborne: false);
                break;

            case StanceBreakEffect.KnockUp:
                StartKnockUp(request);
                break;

            case StanceBreakEffect.Knockback:
                float distance = request.KnockbackDistance * (1f - knockbackResistance);
                ApplyPush(request.HitDirection, distance, 0.28f);
                PlayAnimation(flinchAnimation);
                break;

            case StanceBreakEffect.None:
            default:
                // Fully immune (or no effect requested): a plain interrupt, no hard CC.
                InterruptCurrentAction();
                break;
        }
    }

    private static float ScaleDuration(float duration, float resistance) => duration * (1f - resistance);

    // --- Immediate reactions -------------------------------------------------

    private void InterruptCurrentAction()
    {
        // Cancel the warning explicitly; ResetPath alone does not interrupt a coroutine.
        if (controlLocked) return;
        if (_enemyAI) _enemyAI.InterruptAttack();
        // Bosses ignore ordinary flinches; a stance break/control lock interrupts their patterns.
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
        if (_enemyAI) _enemyAI.InterruptAttack();
        if (!_sectorBoss) _sectorBoss = GetComponent<SectorBoss>();
        if (_sectorBoss) _sectorBoss.InterruptAttack();
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
