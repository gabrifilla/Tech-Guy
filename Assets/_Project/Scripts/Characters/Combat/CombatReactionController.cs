using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Coordinates the pure posture model, bounded movement and independent presentation owners.</summary>
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


    [SerializeField] private ReactionCategory _reactionCategory;
    [SerializeField] private ReactionAuthoring _reactionAuthoring = new ReactionAuthoring();
    [SerializeField] private HitStopRunner _hitStopRunner;
    private PostureReactionState _state;
    private readonly RankInterruptStagger _interruptStagger = new RankInterruptStagger();
    private Actor _owner;
    private EnemyAI _enemyAI;
    private SectorBoss _sectorBoss;
    private bool _ownsAgentStop, _savedAgentRotation, _ownsAgentRotation;
    private int _motionToken, _motionGeneration;
    private float _motionHeight, _motionDistance, _motionTravelled;
    private bool _motionWasKnockback;
    private Vector3 _motionDirection, _lastPoseOffset;
    private Quaternion _poseBaseRotation;
    private Transform _poseTarget;
    private Vector3 _pushDirection;
    private float _pushStart, _pushDistance, _pushTravelled;
    private Coroutine _breakFlash;
    private Transform _flashTarget;
    private Vector3 _flashBaseScale;
    private string _warnedMissingConfig;
    public EnemyRank Rank => rank;
    public ReactionCategory Category => State.Configuration.Category;
    public EnemyRank EffectiveRank => State.EffectiveRank;
    public bool IsVulnerable => State.IsVulnerable;
    public float CurrentStance => State.CurrentStance;
    public float MaxStance => State.MaxStance;
    public float StanceRatio => MaxStance > 0f ? Mathf.Clamp01(CurrentStance / MaxStance) : 0f;
    public bool IsControlLocked => State.IsControlLocked;
    public bool IsStunned => State.IsStunned;
    public bool IsResumeProtected => State.IsResumeProtected;
    public bool ShouldShowStanceBar => rank != EnemyRank.Normal || Category >= ReactionCategory.MediumMob;
    public float GameplayTime => State.Now;
    public Actor LastBreakAttacker { get; private set; }
    private long _breakSequence;
    public long ConfirmedBreakSequence => _breakSequence;
    public KnockdownPhase KnockdownPhase => State.CurrentKnockdownPhase;
    public event System.Action<Vector3> StanceBroken;
    public event System.Action<bool> VulnerabilityChanged;
    private bool Alive => isActiveAndEnabled && (!_owner || !_owner.IsDead);
    private PostureReactionState State
    {
        get
        {
            if (_state == null) RebuildState(new ReactionConfiguration(rank, _reactionCategory, _reactionAuthoring, stunResistance, vulnerabilityWindowSeconds, breakImmunityDuration));
            return _state;
        }
    }
    private void Awake()
    {
        _owner = GetComponent<Actor>();
        _enemyAI = GetComponent<EnemyAI>();
        _sectorBoss = GetComponent<SectorBoss>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!body) body = GetComponent<Rigidbody>();
        if (!characterController) characterController = GetComponent<CharacterController>();
        if (!visualRoot && animator && animator.transform != transform) visualRoot = animator.transform;
        if (_owner) _owner.Died += OnDeath;
        if (_state == null) RebuildState(new ReactionConfiguration(rank, _reactionCategory, _reactionAuthoring, stunResistance, vulnerabilityWindowSeconds, breakImmunityDuration));
    }
    public void ConfigureRank(EnemyRank newRank, bool bossAirJuggle = false, bool bossRagdoll = false)
    {
        rank = newRank;
        var d = RankReactionDefaults.For(newRank);
        maxStance = d.MaxStance; stanceDamageMultiplier = d.StanceDamageMultiplier;
        stanceRecoveryPerSecond = d.StanceRecoveryPerSecond; stanceRecoveryDelay = d.StanceRecoveryDelay;
        staggerResistance = d.StaggerResistance; stunResistance = d.StunResistance;
        knockUpResistance = d.KnockUpResistance; knockbackResistance = d.KnockbackResistance;
        vulnerabilityWindowSeconds = d.VulnerabilityWindowSeconds; breakImmunityDuration = d.BreakImmunitySeconds;
        RebuildState(new ReactionConfiguration(newRank, _reactionCategory, _reactionAuthoring, stunResistance, vulnerabilityWindowSeconds, breakImmunityDuration));
    }
    public void ConfigureStance(EnemyRank newRank, float newMaxStance, float newStanceDamageMultiplier,
        float newRecoveryPerSecond, float newRecoveryDelay, float newStaggerResistance,
        float newStunResistance, float newKnockUpResistance, float newKnockbackResistance)
    {
        rank = newRank;
        maxStance = ReactionNumeric.Safe(newMaxStance, 1f, float.MaxValue, RankReactionDefaults.For(rank).MaxStance);
        stanceDamageMultiplier = ReactionNumeric.Safe(newStanceDamageMultiplier, 0f, 5f, 1f);
        stanceRecoveryPerSecond = ReactionNumeric.Safe(newRecoveryPerSecond, 0f, float.MaxValue, 25f);
        stanceRecoveryDelay = ReactionNumeric.Safe(newRecoveryDelay, 0f, 10f, 2.5f);
        staggerResistance = SafeResistance(newStaggerResistance); stunResistance = SafeResistance(newStunResistance);
        knockUpResistance = SafeResistance(newKnockUpResistance); knockbackResistance = SafeResistance(newKnockbackResistance);
        RebuildState(new ReactionConfiguration(rank, _reactionCategory, _reactionAuthoring, stunResistance, vulnerabilityWindowSeconds, breakImmunityDuration));
    }
    private static float SafeResistance(float value) => ReactionNumeric.Safe(value, 0f, 1f, 1f);
    public void ConfigureProfile(EnemyProfile profile)
    {
        if (!profile)
        {
            string id = RankReactionDefaults.MissingConfigId(rank);
            if (_warnedMissingConfig != id) { _warnedMissingConfig = id; Debug.LogWarning(id + ": using complete reaction defaults.", this); }
            ConfigureRank(rank);
            return;
        }
        _reactionCategory = profile.ReactionCategory;
        ConfigureStance(rank, profile.MaxStance, profile.StanceDamageMultiplier, profile.StanceRecoveryPerSecond,
            profile.StanceRecoveryDelay, profile.StaggerResistance, profile.StunResistance, profile.KnockUpResistance, profile.KnockbackResistance);
        RebuildState(profile.ResolveReaction(rank));
    }
    public void ConfigureReaction(ReactionConfiguration configuration)
    {
        rank = configuration.Rank;
        RebuildState(configuration);
    }
    public void BindGameplayClock(HitStopRunner runner) { if (runner) _hitStopRunner = runner; }
    private void RebuildState(ReactionConfiguration configuration)
    {
        CleanupOwnedPresentation();
        ReleaseAgentStop();
        _state = new PostureReactionState(configuration, maxStance, stanceDamageMultiplier, stanceRecoveryPerSecond, stanceRecoveryDelay);
        _interruptStagger.Configure(configuration.Rank, staggerResistance); _interruptStagger.Reset();
    }
    private void OnValidate()
    {
        var errors = new System.Collections.Generic.List<string>();
        ReactionNumeric.Validate(errors, nameof(maxStance), maxStance, 1f, float.MaxValue);
        ReactionNumeric.Validate(errors, nameof(stanceDamageMultiplier), stanceDamageMultiplier, 0f, 5f);
        ReactionNumeric.Validate(errors, nameof(stanceRecoveryPerSecond), stanceRecoveryPerSecond, 0f, float.MaxValue);
        ReactionNumeric.Validate(errors, nameof(stanceRecoveryDelay), stanceRecoveryDelay, 0f, 10f);
        ReactionNumeric.Validate(errors, nameof(breakImmunityDuration), breakImmunityDuration, 0.1f, 10f);
        ReactionNumeric.Validate(errors, nameof(vulnerabilityWindowSeconds), vulnerabilityWindowSeconds, 0.01f, 10f);
        foreach (float value in new[] { staggerResistance, stunResistance, knockUpResistance, knockbackResistance })
            ReactionNumeric.Validate(errors, "CC resistance", value, 0f, 1f);
        _reactionAuthoring?.Validate(errors);
        foreach (string error in errors) Debug.LogError("CombatReactionController '" + name + "': " + error, this);
    }
    private void Update()
    {
        if (!Alive) return;
        bool windowWasOpen = State.IsVulnerable;
        State.Advance(Time.deltaTime, Time.timeScale <= 0f, _hitStopRunner && _hitStopRunner.IsActive);
        _interruptStagger.Decay(State.Now, State.Configuration.DecaySeconds);
        if (windowWasOpen != State.IsVulnerable) VulnerabilityChanged?.Invoke(State.IsVulnerable);
        SynchronizeControl();
        TickPush();
        if (State.ResumeWatchdogExpired && !_watchdogReported)
        {
            _watchdogReported = true;
            Debug.LogWarning("Reaction resume watchdog expired on '" + name + "'. Check offensive action eligibility.", this);
        }
    }
    private bool _watchdogReported;
    private void LateUpdate() { if (Alive) TickMotion(); }
    public void EvaluateResumeEligibility(bool eligible) => State.EvaluateResumeEligibility(eligible);
    public void BeginOffensiveAction(long execution) => State.BeginOffensiveAction(execution);
    public void EndOffensiveAction(long execution) => State.EndOffensiveAction(execution);
    public void CancelOffensiveActionExternally() => State.CancelOffensiveActionExternally();
    public void ApplyReaction(HitReactionRequest request)
    {
        if (!Alive) return;
        if (request.Attacker) BindGameplayClock(request.Attacker.GetComponent<HitStopRunner>());
        bool breakWillOccur = !State.IsBreakImmune && request.StanceDamage > 0f
            && (double)request.StanceDamage * stanceDamageMultiplier >= CurrentStance;
        // One cancellation authority: a breaking hit resolves its break instead of an immediate stagger.
        if (!breakWillOccur)
        {
            if (request.ReactionType == HitReactionType.Push && ControlLockGate.AllowsImmediateDisplacement(IsControlLocked))
                StartPush(request.HitDirection, ImmediateReactionClamp.ClampPush(request.ReactionType, request.PushDistance));
            if (request.ReactionType == HitReactionType.Stagger)
                ResolveInterrupt(request.InterruptContribution);
        }
        if (request.ReactionType != HitReactionType.None) PlayAnimation(flinchAnimation);
        ResolvePosture(request);
    }
    public InterruptWindowGate.Decision ApplyInterruptStagger(HitReactionRequest request, float staggerValue)
    {
        if (!Alive) return InterruptWindowGate.NotInterrupted;
        if (request.Attacker) BindGameplayClock(request.Attacker.GetComponent<HitStopRunner>());
        bool breaking = !State.IsBreakImmune && request.StanceDamage > 0f
            && (double)request.StanceDamage * stanceDamageMultiplier >= CurrentStance;
        var decision = breaking ? InterruptWindowGate.NotInterrupted : ResolveInterrupt(staggerValue);
        ResolvePosture(request);
        return decision;
    }
    private InterruptWindowGate.Decision ResolveInterrupt(float contribution)
    {
        if (!ReactionNumeric.IsFinite(contribution) || contribution <= 0f)
            return InterruptWindowGate.NotInterrupted;
        if (!_enemyAI) TryGetComponent(out _enemyAI);
        var config = State.Configuration;
        bool threshold = false;
        if (ReactionPolicy.CanContribute(config, IsControlLocked, IsResumeProtected, staggerResistance))
            threshold = _interruptStagger.Accumulate(contribution, State.Now, config.DecaySeconds) == RankInterruptStagger.Outcome.StaggerOrBreak;
        bool inWindow = _enemyAI && _enemyAI.IsWindingUp;
        var decision = ReactionPolicy.ResolveInterrupt(config, IsControlLocked, IsResumeProtected, inWindow, threshold, staggerResistance);
        if (decision.CancelsAttack) _enemyAI.TryInterruptAttack();
        return decision;
    }
    private void ResolvePosture(HitReactionRequest request)
    {
        if (!State.ApplyStanceDamage(request.StanceDamage)) return;
        // Runtime spawners may add offensive components after this controller's Awake.
        if (!_enemyAI) TryGetComponent(out _enemyAI);
        if (!_sectorBoss) TryGetComponent(out _sectorBoss);
        _watchdogReported = false;
        LastBreakAttacker = request.Attacker;
        _breakSequence = checked(_breakSequence + 1);
        _interruptStagger.Reset();
        _pushDistance = 0f;
        if (_enemyAI) _enemyAI.InterruptAttack();
        if (_sectorBoss) _sectorBoss.InterruptAttack();
        var config = State.Configuration;
        var effect = ReactionPolicy.ResolveBreak(config, request.BreakEffect, stunResistance, knockUpResistance, knockbackResistance);
        StartPhysical(effect, request);
        SynchronizeControl();
        Vector3 point = ReactionNumeric.IsFinite(request.HitPoint) ? request.HitPoint : transform.position + Vector3.up * 2f;
        // The break is already committed. Retain its candidate even when a public subscriber throws.
        if (request.Attacker is PlayerActor player) player.ConfirmStanceBreakFeedback();
        // State, immunity, physical ownership and opening are committed before reentrant subscribers.
        System.Action notify = () => { VulnerabilityChanged?.Invoke(true); StanceBroken?.Invoke(point); };
        if (!(request.Attacker is PlayerActor attacker) || !attacker.CaptureGauntletBreak(_owner, _breakSequence, notify)) notify();
        if (!Alive) return;
        if (ShouldShowStanceBar) StanceBreakPopup.Show(point);
        PlayBreakFlash();
        var presentation = GetComponent<StanceBreakPresentation>() ?? gameObject.AddComponent<StanceBreakPresentation>();
        presentation.Show(point);
    }
    private void StartPhysical(StanceBreakEffect effect, HitReactionRequest request)
    {
        var config = State.Configuration;
        if (effect == StanceBreakEffect.KnockUp)
        {
            float height = ReactionNumeric.Safe(request.KnockUpHeight, 0.01f, DisplacementTier.KnockUpMax, config.LaunchHeight);
            if (HasObstruction(Vector3.up, height))
                effect = ReactionPolicy.ResolvePhysical(config, StanceBreakEffect.Stun, stunResistance, knockUpResistance, knockbackResistance);
        }
        if (effect == StanceBreakEffect.None) return;
        float duration = effect == StanceBreakEffect.Knockdown ? config.FallSeconds + config.GroundSeconds + config.RecoverSeconds
            : ReactionNumeric.Safe(request.StunDuration, 0.01f, 10f,
                effect == StanceBreakEffect.KnockUp ? config.LaunchSeconds : config.StunSeconds);
        if (effect == StanceBreakEffect.Knockback) duration = 0.28f;
        int token = State.StartEffect(effect, duration);
        if (token == 0) return;
        _pushDistance = 0f;
        if (effect != StanceBreakEffect.Stun)
        {
            RestorePose();
            _motionToken = token; _motionGeneration = State.Generation;
            _motionWasKnockback = effect == StanceBreakEffect.Knockback;
            _motionHeight = ReactionNumeric.Safe(request.KnockUpHeight, 0.01f, DisplacementTier.KnockUpMax, config.LaunchHeight);
            _motionDistance = ReactionNumeric.Safe(request.KnockbackDistance, 0f, DisplacementTier.KnockbackMax, 4f);
            _motionDirection = FlatDirection(request.HitDirection);
            _motionTravelled = 0f;
            _poseTarget = visualRoot ? visualRoot : transform;
            _poseBaseRotation = _poseTarget.localRotation;
        }
        PlayAnimation(stunAnimation);
    }
    private bool HasObstruction(Vector3 direction, float distance)
    {
        foreach (var hit in Physics.SphereCastAll(transform.position + Vector3.up * 0.5f, 0.25f, direction,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit.collider && !hit.collider.transform.IsChildOf(transform) && hit.distance > 0.001f) return true;
        return false;
    }
    private void TickMotion()
    {
        if (_motionToken == 0) return;
        if (!State.Owns(_motionToken, _motionGeneration))
        {
            if (_motionGeneration == State.Generation && _motionWasKnockback)
                MoveStep(_motionDirection * Mathf.Max(0f, _motionDistance - _motionTravelled));
            RestorePose(); return;
        }
        float t = State.Progress(_motionToken);
        var effect = State.ActiveMotion;
        if (effect == StanceBreakEffect.Knockback)
        {
            float target = _motionDistance * (1f - (1f - t) * (1f - t));
            MoveStep(_motionDirection * (target - _motionTravelled));
            _motionTravelled = target;
        }
        else if (_poseTarget)
        {
            Vector3 offset = effect == StanceBreakEffect.KnockUp ? Vector3.up * (4f * _motionHeight * t * (1f - t)) : Vector3.zero;
            _poseTarget.localPosition += offset - _lastPoseOffset; _lastPoseOffset = offset;
            float pose = effect == StanceBreakEffect.KnockUp ? (forceHorizontalVisualOnLaunch ? 1f : 0f)
                : KnockdownPose(t);
            _poseTarget.localRotation = _poseBaseRotation * Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(horizontalVisualEuler), pose);
        }
    }
    private float KnockdownPose(float t)
    {
        var c = State.Configuration;
        float elapsed = t * (c.FallSeconds + c.GroundSeconds + c.RecoverSeconds);
        return elapsed < c.FallSeconds ? elapsed / c.FallSeconds
            : elapsed < c.FallSeconds + c.GroundSeconds ? 1f
            : 1f - Mathf.Clamp01((elapsed - c.FallSeconds - c.GroundSeconds) / c.RecoverSeconds);
    }
    private void SynchronizeControl()
    {
        if (IsControlLocked)
        {
            if (agent && agent.enabled && agent.isOnNavMesh)
            {
                if (!_ownsAgentRotation) { _savedAgentRotation = agent.updateRotation; _ownsAgentRotation = true; agent.updateRotation = false; }
                if (!agent.isStopped) { _ownsAgentStop = true; agent.isStopped = true; }
            }
        }
        else ReleaseAgentStop();
    }
    private void ReleaseAgentStop()
    {
        if (_ownsAgentRotation && agent) agent.updateRotation = _savedAgentRotation;
        _ownsAgentRotation = false;
        if (_ownsAgentStop && Alive && agent && agent.enabled && agent.isOnNavMesh && !(_state?.IsControlLocked ?? false))
            agent.isStopped = false;
        _ownsAgentStop = false;
    }
    private static Vector3 FlatDirection(Vector3 direction)
    {
        if (!ReactionNumeric.IsFinite(direction)) return Vector3.forward;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.00001f ? direction.normalized : Vector3.forward;
    }
    private void StartPush(Vector3 direction, float distance)
    {
        if (!ReactionNumeric.IsFinite(distance) || distance <= 0f) return;
        _pushDirection = FlatDirection(direction); _pushStart = State.Now;
        _pushDistance = distance; _pushTravelled = 0f;
        Transform target = visualRoot ? visualRoot : transform;
        float angle = Vector3.SignedAngle(target.forward, _pushDirection, Vector3.up);
        target.rotation = Quaternion.AngleAxis(ImmediateReactionClamp.ClampRotation(HitReactionType.Push, angle), Vector3.up) * target.rotation;
    }
    private void TickPush()
    {
        if (IsControlLocked) { _pushDistance = 0f; return; }
        if (_pushDistance <= 0f) return;
        float t = Mathf.Clamp01((State.Now - _pushStart) / 0.14f);
        float target = _pushDistance * (1f - (1f - t) * (1f - t));
        MoveStep(_pushDirection * (target - _pushTravelled)); _pushTravelled = target;
        if (t >= 1f) _pushDistance = 0f;
    }
    private void MoveStep(Vector3 delta)
    {
        if (!Alive || !ReactionNumeric.IsFinite(delta) || delta.sqrMagnitude <= 0f) return;
        float distance = delta.magnitude;
        Vector3 direction = delta / distance;
        foreach (var hit in Physics.SphereCastAll(transform.position + Vector3.up * 0.5f, 0.25f, direction,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            if (hit.collider && !hit.collider.transform.IsChildOf(transform))
                distance = Mathf.Min(distance, Mathf.Max(0f, hit.distance - 0.01f));
        delta = direction * distance;
        if (agent && agent.enabled && agent.isOnNavMesh) agent.Move(delta);
        else if (characterController && characterController.enabled) characterController.Move(delta);
        else if (body && !body.isKinematic) body.position += delta;
        else transform.position += delta;
    }
    private void PlayAnimation(string stateName)
    {
        if (animator && animator.runtimeAnimatorController && !string.IsNullOrWhiteSpace(stateName)
            && animator.HasState(0, Animator.StringToHash(stateName))) animator.Play(stateName);
    }
    private void PlayBreakFlash()
    {
        RestoreFlash();
        _flashTarget = visualRoot ? visualRoot : transform; _flashBaseScale = _flashTarget.localScale;
        _breakFlash = StartCoroutine(BreakFlash());
    }
    private IEnumerator BreakFlash()
    {
        float elapsed = 0f;
        while (elapsed < 0.25f && _flashTarget)
        {
            elapsed += Time.unscaledDeltaTime;
            _flashTarget.localScale = _flashBaseScale * (1f + 0.25f * Mathf.Sin(Mathf.Clamp01(elapsed / 0.25f) * Mathf.PI));
            yield return null;
        }
        RestoreFlash();
    }
    private void RestoreFlash()
    {
        if (_breakFlash != null) StopCoroutine(_breakFlash);
        _breakFlash = null;
        if (_flashTarget) _flashTarget.localScale = _flashBaseScale;
        _flashTarget = null;
    }
    private void RestorePose()
    {
        if (_poseTarget) { _poseTarget.localPosition -= _lastPoseOffset; _poseTarget.localRotation = _poseBaseRotation; }
        _lastPoseOffset = Vector3.zero; _poseTarget = null; _motionToken = 0;
    }
    private void CleanupOwnedPresentation() { RestorePose(); RestoreFlash(); _pushDistance = 0f; }
    private void OnDeath(Actor actor) => ResetLifecycle();
    private void OnDisable() => ResetLifecycle();
    private void OnEnable() { if (_state != null) _state.Reset(); _watchdogReported = false; }
    private void ResetLifecycle()
    {
        bool wasOpen = _state != null && _state.IsVulnerable;
        CleanupOwnedPresentation(); _state?.Reset(); _interruptStagger.Reset(); ReleaseAgentStop();
        LastBreakAttacker = null;
        if (wasOpen) VulnerabilityChanged?.Invoke(false);
    }
    private void OnDestroy() { if (_owner) _owner.Died -= OnDeath; }
}
