using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Owns the equipped gauntlet's cast and energy; assets contain configuration only.</summary>
[RequireComponent(typeof(PlayerActor), typeof(AbilityHolder))]
public sealed class BreakerGauntletCombat : MonoBehaviour
{
    private readonly AsuraMomentum _momentum = new AsuraMomentum();
    private PlayerActor _player;
    private AbilityHolder _holder;
    private SkillAnimationPlayer _animation;
    private HitStopRunner _hitStop;
    private NavMeshAgent _agent;
    private WeaponScript _weapon;
    private Coroutine _cast;
    private BreakerGauntletAbility _activeAbility;
    private System.Collections.Generic.IReadOnlyList<AreaHitStep> _activeSteps;
    private float _elapsed;
    private bool _finisherApplied;

    // Single advance source for the active execution's ImpactEvents (task 15.1). The ledger dedups
    // every emission by (ExecutionId, ImpactEvent.Index) so the logical clock advanced in FixedUpdate
    // and any Animation Event signal collapse to one impact (R8.9); the scheduler decides, from the
    // ability's authored CombatProfile.ImpactEvents, when each impact fires / each window opens/closes
    // across Startup→Active→Recovery (R2.4–R2.6). It is null for abilities with no authored profile,
    // in which case the coroutine keeps its legacy per-step impact loop so Arco/Lança and any
    // unauthored asset never regress.
    private readonly ExecutionImpactLedger _ledger = new ExecutionImpactLedger();
    private ActionImpactScheduler _scheduler;
    private bool _savedStopped;
    private bool _savedRotation;
    private bool _agentLocked;

    // Weapon-applied soft grouping (Requisito 7.2/7.3). Both reuse SoftGroupingService's existing
    // locomotion channel (ApplyExternalDisplacement) instead of opening a parallel one; the bounded
    // per-application math lives in the pure FlurryRegroupDisplacement / AsuraPullDisplacement helpers
    // so it can be property-tested without a scene (Properties 19/20).
    private readonly Collider[] _grouping = new Collider[64];
    // The Flurry regroup reuses the shared soft-grouping caps (≤1.0 m/s, ≤0.5 m per application) while
    // its own 2.0 m chain radius lives in FlurryRegroupDisplacement.
    private readonly SoftGroupingConfig _regroupConfig = new SoftGroupingConfig();
    // Flurry chaining point: the struck target's position captured when the Flurry first locks onto it.
    private SoftGroupingService _flurryTarget;
    private Vector3 _flurryChainPoint;
    // Per-enemy budget already spent by the Asura pull this cast (≤1.5 m per enemy — R7.3).
    private readonly System.Collections.Generic.Dictionary<SoftGroupingService, float> _asuraPulled
        = new System.Collections.Generic.Dictionary<SoftGroupingService, float>();

    public bool IsExecuting { get; private set; }
    public bool IsAsuraActive => IsExecuting && _activeAbility && _activeAbility.AsuraBurst;
    public float ExecutionDuration { get; private set; }

    /// <summary>
    /// Whether the active cast's authored Dash <see cref="CancelRule"/> is open at the current
    /// progress (R3.8). This is a <em>read-only reflection</em> of the authored rule resolved through
    /// the shared <see cref="CancelResolver"/> — it is <strong>not</strong> an authorization shortcut:
    /// the actual dash-cancel decision is made by <see cref="AbilityHolder"/> via
    /// <c>ResolveCancel(CancelTarget.Dash)</c>, and <see cref="FinishForDodge"/> is only the cancel
    /// EXECUTION. Task 15.3 removed the old hidden <c>_elapsed &gt;= .25f</c> exception that gated the
    /// Asura dash-cancel independently of its authored rule; the window is now governed solely by the
    /// authored Dash rule covering Active. Returns <c>false</c> when nothing is executing or the active
    /// ability authors no Dash rule.
    /// </summary>
    public bool CanDodgeCancel
    {
        get
        {
            if (!IsExecuting || !_activeAbility) return false;
            CombatActionProfile profile = _activeAbility.CombatProfile;
            if (profile == null) return false;
            CancelRuleSet rules = profile.BuildCancelRuleSet();
            if (!rules.TryGet(CancelTarget.Dash, out CancelRule dash)) return false;
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, ExecutionDuration));
            return dash.IsOpenAt(progress);
        }
    }

    /// <summary>
    /// Seconds already elapsed in the current cast (0 when idle). Exposed read-only so the
    /// shared cancel pipeline in <see cref="AbilityHolder"/> can compute the normalized progress
    /// used by <see cref="CancelResolver"/> without reaching into the coroutine's private state
    /// (R4.6/R4.7). Mirrors the private <c>_elapsed</c> the <c>Execute</c> loop advances.
    /// </summary>
    public float Elapsed => _elapsed;

    /// <summary>
    /// The ability currently being cast, or <c>null</c> when idle. Exposed read-only so the shared
    /// cancel pipeline in <see cref="AbilityHolder"/> can read the active action's authored
    /// <see cref="Ability.CombatProfile"/> to build its <see cref="CancelRuleSet"/> (R4.4/R4.6).
    /// </summary>
    public BreakerGauntletAbility ActiveAbility => _activeAbility;

    /// <summary>
    /// Reports the active action's unified <c>(ActionPhase, localProgress)</c> to the
    /// Núcleo_Compartilhado (task 15.2, R2.5/R8.8), computed from the ability's authored
    /// <see cref="CombatActionProfile"/>. On the fixed-duration path the global progress is
    /// <c>Clamp01(Elapsed / ExecutionDuration)</c>, the phase is <see cref="ActionTimeline.PhaseOf"/>
    /// and the local progress is <see cref="ActionTimeline.LocalProgressOf"/> (progress mapped into
    /// the current phase's sub-range → <c>[0, 1]</c>). A <see cref="CommitmentCategory.Channel"/>
    /// action (e.g. the Asura R) reports the same phase + phase-local progress expressed as a
    /// <see cref="ChannelProgress"/>, which carries no dependency on a total duration and prepares the
    /// Channel-Hold termination of task 15.3. Returns <c>false</c> (phase <see cref="ActionPhase.Startup"/>,
    /// localProgress <c>0</c>) when nothing is executing or the active ability authors no profile, so
    /// the core can treat "no report" distinctly from a real Startup report.
    /// </summary>
    /// <param name="phase">The current phase of the active action.</param>
    /// <param name="localProgress">Progress within <paramref name="phase"/>, in <c>[0, 1]</c>.</param>
    /// <returns><c>true</c> when an authored action is executing and a report was produced.</returns>
    public bool TryGetActionProgress(out ActionPhase phase, out float localProgress)
    {
        phase = ActionPhase.Startup;
        localProgress = 0f;
        if (!IsExecuting || !_activeAbility) return false;

        CombatActionProfile profile = _activeAbility.CombatProfile;
        if (profile == null) return false;

        ActionTimeline timeline = profile.BuildTimeline();
        float globalProgress = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, ExecutionDuration));
        phase = timeline.PhaseOf(globalProgress);
        float phaseLocal = timeline.LocalProgressOf(globalProgress);

        // A Channel action (e.g. Asura R) is reported through ChannelProgress semantics so the report
        // never depends on a known total duration (R3.7); the phase + phase-local progress are the same
        // pair the fixed-duration path produces, which is all the Núcleo_Compartilhado's CancelRules
        // need and what task 15.3's Hold termination consumes.
        if (profile.ResolveCommitment(out _) == CommitmentCategory.Channel)
        {
            ChannelProgress channel = new ChannelProgress(phase, phaseLocal);
            phase = channel.Phase;
            localProgress = channel.LocalProgress;
            return true;
        }

        localProgress = phaseLocal;
        return true;
    }

    /// <summary>
    /// Reports the active action's resolved <see cref="CommitmentCategory"/> to the core through the
    /// single <see cref="CommitmentRules.Resolve"/> path (task 15.2, R3.1/R3.2), reading the
    /// authored <see cref="CombatActionProfile.Commitment"/>. Returns <c>false</c> when nothing is
    /// executing or the active ability authors no profile, so the core can gate movement fractions
    /// only when an authored commitment is available (the per-phase movement fraction read stays
    /// non-behavioral here, honoring contract-audit O-9).
    /// </summary>
    /// <param name="commitment">The resolved commitment category of the active action.</param>
    /// <returns><c>true</c> when an authored action is executing and a commitment was resolved.</returns>
    public bool TryGetCommitment(out CommitmentCategory commitment)
    {
        commitment = CommitmentRules.Default;
        if (!IsExecuting || !_activeAbility) return false;

        CombatActionProfile profile = _activeAbility.CombatProfile;
        if (profile == null) return false;

        commitment = profile.ResolveCommitment(out _);
        return true;
    }

    /// <summary>
    /// Supplies the shared cancel pipeline in <see cref="AbilityHolder"/> with everything it needs to
    /// evaluate a cancel against the active action in ONE call (task 15.2): the active ability's
    /// authored <see cref="CombatActionProfile"/> and the <em>global</em> normalized progress
    /// (<c>Clamp01(Elapsed / ExecutionDuration)</c>) that the authored CancelRules are expressed over.
    /// Centralizing this read in the executor means the holder no longer recomputes
    /// <c>Elapsed / ExecutionDuration</c> itself — the single progress computation lives here, next to
    /// the unified <see cref="TryGetActionProgress"/> report. Returns <c>false</c> (profile <c>null</c>,
    /// progress <c>0</c>) when nothing is executing, so the holder treats "not casting" distinctly.
    /// </summary>
    /// <param name="profile">The active ability's authored profile, or <c>null</c> when unauthored.</param>
    /// <param name="globalProgress">The global normalized progress of the active cast, in <c>[0, 1]</c>.</param>
    /// <returns><c>true</c> when an action is executing (even with a <c>null</c> profile).</returns>
    public bool TryGetCancelContext(out CombatActionProfile profile, out float globalProgress)
    {
        profile = null;
        globalProgress = 0f;
        if (!IsExecuting || !_activeAbility) return false;

        profile = _activeAbility.CombatProfile;
        globalProgress = Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, ExecutionDuration));
        return true;
    }

    public int Energy => _momentum.Energy;
    public bool IsReady => _momentum.IsReady;
    public bool IsEquipped => _weapon && _player && !_player.IsDead && _player.CurrentWeapon == _weapon;

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _holder = GetComponent<AbilityHolder>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
        // The thin hit-stop runtime lives on the player GameObject (same get-or-add pattern as the
        // SkillAnimationPlayer above). It is driven once per resolved ImpactEvent by EmitAuthoredImpact
        // (task 16.1); the pure HitStop/HitStopGrouping decision stays in the Núcleo_Compartilhado.
        _hitStop = GetComponent<HitStopRunner>() ?? gameObject.AddComponent<HitStopRunner>();
        _agent = GetComponent<NavMeshAgent>();
        _player.Died += OnDeath;
    }

    /// <summary>
    /// Adds Asura energy from an external source (e.g. a basic-hit boon), delegating to
    /// <see cref="AsuraMomentum.AddEnergy"/> which already clamps to [0, Maximum]. Thin delegate only
    /// (Requisitos 4.1/4.2).
    /// </summary>
    public void AddAsuraEnergy(int amount) => _momentum.AddEnergy(amount);

    public void Configure(WeaponScript weapon)
    {
        if (_weapon == weapon) return;
        CancelCast();
        _momentum.Reset();
        _weapon = weapon;
    }

    public bool CanUse(BreakerGauntletAbility ability)
    {
        if (!isActiveAndEnabled || !_holder.isActiveAndEnabled || !_player || _player.IsDead ||
            !_weapon || _player.CurrentWeapon != _weapon || IsExecuting || !ability ||
            ability.HitSteps == null || ability.HitSteps.Count == 0) return false;
        if (System.Array.IndexOf(_weapon.abilities, ability) < 0) return false;
        return !ability.AsuraBurst || _momentum.IsReady;
    }

    public void Use(BreakerGauntletAbility ability)
    {
        if (!CanUse(ability)) return;
        WeaponRunModifiers mods = _player.RunModifiers;
        if (ability.AsuraBurst)
        {
            // Requisitos 8.6/8.7: CanUse already gated R on _momentum.IsReady, so a full meter enters
            // Asura and TryConsume zeroes it; a sub-100 meter never reaches here (activation denied,
            // energy preserved). The post-burst reserve is a separate run-modifier grant, not a self-loop.
            _momentum.TryConsume();
            _momentum.AddEnergy(20 * (mods?.Rank(WeaponBoon.AsuraReserve) ?? 0));
        }
        else
        {
            _momentum.RegisterSkill(ability.ShockSkill);
            _momentum.AddEnergy(10 * (mods?.Rank(WeaponBoon.Momentum) ?? 0));
        }
        _cast = StartCoroutine(Execute(ability));
    }

    private IEnumerator Execute(BreakerGauntletAbility ability)
    {
        IsExecuting = true;
        _activeAbility = ability; _elapsed = 0; _finisherApplied = false;
        _flurryTarget = null; _asuraPulled.Clear();
        if (ability.AsuraBurst) _player.SetDamageTakenMultiplier(this,.15f);
        if (TryGetComponent(out CharControlScript control)) control.CancelCombo();
        SequencedAreaAttackAbility.FaceMousePosition(transform);
        Vector3 direction = transform.forward;
        WeaponRunModifiers mods = _player.RunModifiers;
        int slot = System.Array.IndexOf(_weapon.abilities, ability);
        // Per-cast step snapshot: GauntletSteps already clones (never writes the asset). On the no-run
        // fallback path we clone here too so the loop configuration below never mutates the source
        // ability (Requisito 12.1). The pure GauntletLoopSteps helper then pins the Manoplas loop
        // identity for this slot — E (slot 2) lands Heavy/Breaker and its finisher declares a KnockUp
        // so QUEBRAR flows into JUGGLE (Requisito 8.5/8.8/8.9); other slots are left as authored so the
        // loop's ENTRAR→INTERROMPER→PRESSIONAR→QUEBRAR→JUGGLE→ASURA→FINALIZAR steps stay data-driven and
        // each usable on its own (Requisito 8.1).
        var steps = mods != null ? mods.GauntletSteps(ability, slot) : CloneSteps(ability.HitSteps);
        GauntletLoopSteps.Configure(steps, slot);
        // Gauntlet (Manopla) basic/skill hits must NOT shove the enemy: zero the per-step push nudge so
        // every blow lands in place (dano, stance damage, stagger e rotação de reação são preservados; só
        // o deslocamento imediato é removido). Operates on the per-cast clone, never the source asset, so
        // other weapons (Arco/Lança) are untouched. Strong displacement stays available as a deliberate
        // Stance_Break Knockback (e.g. a future boon/skill), which these steps do not request.
        foreach (AreaHitStep gauntletStep in steps)
            if (gauntletStep != null) gauntletStep.pushDistance = 0f;
        _activeSteps = steps;
        float advance = ability.AdvanceDistance * (1 + .7f * (mods?.Rank(WeaponBoon.RocketAdvance) ?? 0));
        _agentLocked = _agent && _agent.enabled && _agent.isOnNavMesh;
        if (_agentLocked)
        {
            _savedStopped = _agent.isStopped;
            _savedRotation = _agent.updateRotation;
            _agent.ResetPath();
            _agent.isStopped = true;
            _agent.updateRotation = false;
        }

        // Task 15.3 (R3.6): generic Channel-Hold termination. When the active ability authors a
        // Channel commitment with ChannelTermination == Hold, the Active phase is SUSTAINED while the
        // triggering control is held and ends — transitioning to Recovery per the action's authored
        // Recovery rule — the moment that control is released. This is a weapon-generic mechanism: it
        // reads only the authored CombatActionProfile and the slot's bound control, so ANY future
        // Channel+Hold Manopla action is hold-terminated without a bespoke code path. It is dormant for
        // every shipped action here — the Rajada Asura authors Timed termination (its fixed-duration
        // sequence is its current control, preserved verbatim per R3.9), and the other skills are not
        // Channel — so no observable behavior of the shipped kit changes. The triggering control is the
        // skill slot's binding (GameControl.Skill1 + slot); dash (slot < 0) and any unmapped slot are
        // never hold-sustained.
        CombatActionProfile holdProfile = ability ? ability.CombatProfile : null;
        bool holdSustained = holdProfile != null &&
            holdProfile.ResolveCommitment(out _) == CommitmentCategory.Channel &&
            holdProfile.ChannelTermination == ChannelTerminationMode.Hold &&
            slot >= 0 && slot < 4;
        GameControl holdControl = holdSustained
            ? (GameControl)((int)GameControl.Skill1 + slot) : GameControl.Skill1;
        // The normalized end of Active comes from the authored timeline; releasing the control jumps the
        // cast to this instant so the remaining loop plays out Recovery exactly as a natural end would.
        float holdActiveEnd = holdSustained ? holdProfile.BuildTimeline().ActiveEnd : 1f;

        try
        {
            float elapsed = 0f;
            float duration = Mathf.Max(ability.activeTime, ability.AdvanceDuration);
            foreach (AreaHitStep step in steps)
                if (step != null) duration = Mathf.Max(duration, step.delay + 0.18f);
            ExecutionDuration = duration;
            // Task 15.1: when the ability authors a CombatProfile with ImpactEvents, the single
            // advance source (FixedUpdate → scheduler) owns impact emission. The scheduler maps each
            // authored ImpactEvent's AreaHitStepIndex back to this per-cast step clone and emits
            // through Impact(...) exactly once per (ExecutionId, Index) via the shared ledger. The
            // coroutine's own applied[] loop is suppressed in that case to avoid double-counting; it
            // stays as the fallback for abilities with no authored ImpactEvents.
            BuildImpactScheduler(ability, steps);
            bool schedulerOwnsImpacts = _scheduler != null;
            var applied = new bool[steps.Count];
            int animatedStep = 0;
            while (elapsed < duration)
            {
                if (!_player || _player.IsDead || _player.CurrentWeapon != _weapon || !_holder.isActiveAndEnabled)
                    break;
                float delta = Time.deltaTime;
                if (delta <= 0f) { yield return null; continue; }
                if (ability.AsuraBurst)
                {
                    // Let the earned burst follow the fight instead of firing into empty space.
                    Quaternion before = transform.rotation;
                    SequencedAreaAttackAbility.FaceMousePosition(transform);
                    transform.rotation = Quaternion.RotateTowards(before,transform.rotation,180f*delta);
                    direction = transform.forward;
                }
                if (_agentLocked && _agent.enabled && _agent.isOnNavMesh && elapsed < ability.AdvanceDuration)
                {
                    float distance = advance * Mathf.Min(delta, ability.AdvanceDuration - elapsed)
                        / Mathf.Max(0.01f, ability.AdvanceDuration);
                    Vector3 destination = transform.position + direction * distance;
                    if (_agent.Raycast(destination, out NavMeshHit edge)) destination = edge.position;
                    _agent.Move(destination - transform.position);
                }
                // Weapon-applied soft grouping, reapplied every frame during the cast (well within the
                // R7.2 ≤0.25 s reapplication interval). Both route through the enemy's existing
                // SoftGroupingService locomotion; no parallel movement channel is opened.
                if (ability.AsuraBurst) ApplyAsuraPull(delta);            // R7.3
                else if (GauntletLoopSteps.StageForSlot(slot) == GauntletLoopStage.Flurry)
                    ApplyFlurryRegroup(delta);                            // R7.2
                elapsed += delta;
                _elapsed = elapsed;
                // R3.6: a hold-sustained Channel ends its Active phase the moment the triggering control
                // is released. Only acts once Active has begun (progress >= holdActiveEnd's startup side
                // is irrelevant — we only cut Active short, never Startup) and only while still inside
                // Active (elapsed < Recovery start); on release we jump elapsed to the Active→Recovery
                // boundary so the remaining loop plays Recovery per the authored Recovery rule and
                // returns control, instead of running the fixed sequence to completion. Dormant unless
                // holdSustained (shipped actions never enter here — see holdSustained setup above).
                if (holdSustained && elapsed < holdActiveEnd * duration &&
                    elapsed >= holdProfile.BuildTimeline().StartupEnd * duration &&
                    !GamePreferences.IsHeld(holdControl))
                {
                    elapsed = holdActiveEnd * duration;
                    _elapsed = elapsed;
                }
                while (animatedStep+1 < steps.Count && elapsed > steps[animatedStep].delay + Mathf.Min(.04f,(steps[animatedStep+1].delay-steps[animatedStep].delay)*.25f)) animatedStep++;
                float start = animatedStep == 0 ? 0 : steps[animatedStep-1].delay + Mathf.Min(.04f,(steps[animatedStep].delay-steps[animatedStep-1].delay)*.25f);
                float end = animatedStep+1 < steps.Count ? steps[animatedStep].delay + Mathf.Min(.04f,(steps[animatedStep+1].delay-steps[animatedStep].delay)*.25f) : duration;
                _animation.Strike(Motion(ability,animatedStep),elapsed,start,steps[animatedStep].delay,end);
                // Legacy per-step impact fallback — only when no authored ImpactEvent scheduler owns
                // emission (task 15.1). When a scheduler is active, FixedUpdate advances it and it
                // emits each impact once through the ledger, so this loop stays idle to avoid duplicates.
                if (!schedulerOwnsImpacts)
                    for (int i = 0; i < applied.Length; i++)
                    {
                        AreaHitStep step = steps[i];
                        if (applied[i] || step == null || elapsed < step.delay) continue;
                        applied[i] = true;
                        Impact(ability,step,i);
                    }
                yield return null;
            }
        }
        finally
        {
            RestoreCastState();
            _cast = null;
        }
    }

    // Deep-clones the asset's authored steps so the no-run fallback path can be configured without
    // mutating the source ability (mirrors WeaponRunModifiers.GauntletSteps' JsonUtility round-trip so
    // the source ScriptableObject stays untouched — Requisito 12.1).
    private static System.Collections.Generic.List<AreaHitStep> CloneSteps(
        System.Collections.Generic.IReadOnlyList<AreaHitStep> source)
    {
        var clones = new System.Collections.Generic.List<AreaHitStep>();
        if (source == null) return clones;
        foreach (AreaHitStep step in source)
        {
            if (step == null) continue;
            clones.Add(JsonUtility.FromJson<AreaHitStep>(JsonUtility.ToJson(step)));
        }
        return clones;
    }

    /// <summary>
    /// The single advance source for the active execution's ImpactEvents (task 15.1, R2.5–R2.7).
    /// Driven once per Quadro_de_Simulacao (<c>FixedUpdate</c>), it advances the scheduler's logical
    /// clock to the current normalized progress so each authored ImpactEvent emits — and each window
    /// opens/closes — within one FixedUpdate of its configured instant, and so entry into Recovery
    /// closes windows and stops emissions. The scheduler dedups through the shared ledger, so the
    /// Animation Event path never double-counts an impact already emitted here (R8.9).
    /// </summary>
    private void FixedUpdate()
    {
        if (_scheduler == null || !IsExecuting) return;
        float duration = ExecutionDuration;
        float progress = duration > 0f ? Mathf.Clamp01(_elapsed / duration) : 1f;
        _scheduler.Advance(progress);
    }

    /// <summary>
    /// Builds the per-execution <see cref="ActionImpactScheduler"/> when <paramref name="ability"/>
    /// authors a <see cref="CombatActionProfile"/> with at least one ImpactEvent; otherwise clears it
    /// so the coroutine's legacy per-step impact loop stays in charge (keeping unauthored assets and
    /// Arco/Lança unchanged). A fresh <see cref="ExecutionId.Next"/> is assigned per execution, and the
    /// emit callback maps each ImpactEvent's <see cref="ImpactEvent.AreaHitStepIndex"/> back to this
    /// cast's step clone, routing it through the existing <see cref="Impact"/> path — the damage
    /// channel is unchanged (task 15.1 governs the advance source + dedup only, not which channel
    /// fires).
    /// </summary>
    private void BuildImpactScheduler(BreakerGauntletAbility ability,
        System.Collections.Generic.IReadOnlyList<AreaHitStep> steps)
    {
        _scheduler = null;
        CombatActionProfile profile = ability ? ability.CombatProfile : null;
        if (profile == null || profile.ImpactEvents == null || profile.ImpactEvents.Count == 0) return;

        ActionTimeline timeline = profile.BuildTimeline();
        _scheduler = new ActionImpactScheduler(
            ExecutionId.Next(),
            _ledger,
            timeline,
            profile.ImpactEvents,
            impact => EmitAuthoredImpact(ability, steps, impact),
            impact => EmitAuthoredImpact(ability, steps, impact));
    }

    /// <summary>
    /// Applies the <see cref="AreaHitStep"/> referenced by an authored <see cref="ImpactEvent"/>
    /// through the existing <see cref="Impact"/> path (task 15.1). The scheduler has already gated
    /// this call through the ledger, so it runs at most once per <c>(ExecutionId, Index)</c>; an
    /// out-of-range or missing step index is a no-op so a mis-authored profile never throws.
    /// </summary>
    private void EmitAuthoredImpact(BreakerGauntletAbility ability,
        System.Collections.Generic.IReadOnlyList<AreaHitStep> steps, ImpactEvent impact)
    {
        if (steps == null) return;
        int stepIndex = impact.AreaHitStepIndex;
        if (stepIndex < 0 || stepIndex >= steps.Count) return;
        AreaHitStep step = steps[stepIndex];
        if (step == null) return;
        int enemiesDamaged = Impact(ability, step, stepIndex);
        ApplyHitStopForImpact(ability, impact, enemiesDamaged);
    }

    /// <summary>
    /// Applies the ImpactEvent's authored hit-stop EXACTLY ONCE for this ImpactEvent, independent of how
    /// many enemies it hit (R7.1, R7.3). The ledger-gated scheduler already guarantees
    /// <see cref="EmitAuthoredImpact"/> runs at most once per <c>(ExecutionId, ImpactEvent.Index)</c>, so
    /// this is one Apply per ImpactEvent, never per enemy. The duration is the ImpactEvent's
    /// <see cref="ImpactEvent.HitStopProfileIndex"/> into the ability's authored
    /// <see cref="CombatActionProfile.HitStopProfiles"/>; the pure <see cref="HitStop.ShouldApply"/>
    /// decides from the clamped duration + enemy count whether a pause happens at all. A profile authored
    /// with duration <c>0</c> (e.g. the Asura burst pulses, HitStopClass Secondary) yields
    /// <c>ShouldApply == false</c> and never touches the time scale (R7.2); zero damage likewise never
    /// pauses (R7.8). When multiple ImpactEvents resolve on the same frame, each still produces at most
    /// one pause of its own profile — the simultaneous "max, never sum" grouping is expressed by
    /// <see cref="HitStopGrouping.Combine"/>, which <see cref="HitStopRunner.Apply"/>'s overlap policy
    /// honors by keeping the longest in-flight pause rather than stacking them.
    /// </summary>
    private void ApplyHitStopForImpact(BreakerGauntletAbility ability, ImpactEvent impact, int enemiesDamaged)
    {
        if (_hitStop == null) return;
        CombatActionProfile profile = ability ? ability.CombatProfile : null;
        if (profile == null) return;

        System.Collections.Generic.IReadOnlyList<HitStopProfile> profiles = profile.HitStopProfiles;
        int hitStopIndex = impact.HitStopProfileIndex;
        if (profiles == null || hitStopIndex < 0 || hitStopIndex >= profiles.Count) return;

        float duration = profiles[hitStopIndex].ClampedDuration;
        if (HitStop.ShouldApply(duration, enemiesDamaged))
        {
            _hitStop.Apply(duration);
        }
    }

    private static SkillMotion Motion(BreakerGauntletAbility ability,int index)
    {
        if (ability.ShockSkill && index >= ability.HitSteps.Count-1) return SkillMotion.Slam;
        return index%2==0 ? SkillMotion.PunchLeft : SkillMotion.PunchRight;
    }
    // Returns the number of enemies this impact resolved damage on, so the authored-ImpactEvent path
    // (EmitAuthoredImpact) can feed HitStop.ShouldApply and apply a hit-stop once per ImpactEvent
    // regardless of how many enemies were hit (R7.1, R7.3). The legacy per-step fallback loop ignores
    // the value — it never carried a hit-stop profile — so its behavior is unchanged.
    private int Impact(BreakerGauntletAbility ability,AreaHitStep step,int index)
    {
        bool finisher=index>=ability.HitSteps.Count-1;
        if (finisher) _finisherApplied=true;
        _animation.Contact(Motion(ability,index));
        int enemiesDamaged=SequencedAreaAttackAbility.ApplyHitStep(transform,_player,_weapon,step,true,ability.EffectColor);
        bool radial=step.hitShape==AreaHitShape.Sphere;
        Vector3 origin=transform.TransformPoint(step.localOffset)+(radial ? Vector3.up*(step.sphereRadius-1f) : Vector3.zero);
        GauntletImpactVfx.Spawn(origin,transform.forward,
            step.hitShape==AreaHitShape.Sphere ? step.sphereRadius*2 : step.rangeOverride,
            radial ? step.sphereRadius*2 : step.boxSize.x,ability.EffectColor,ability.ShockSkill && finisher,ability.AsuraBurst && finisher,index%2==0 ? -1 : 1,radial);
        return enemiesDamaged;
    }
    // R7.2: while W (Flurry) runs, reapply soft grouping to the struck target so it is kept within
    // 2.0 m of the chaining point. The chaining point is captured the first frame a target is locked;
    // the bounded per-application delta comes from the pure FlurryRegroupDisplacement (2.0 m radius +
    // the shared ≤0.5 m / ≤1.0 m/s caps) and is moved through the target's own SoftGroupingService,
    // reusing that locomotion channel instead of a parallel one. Retargets when the current target
    // dies or leaves reach; targets without a SoftGroupingService are simply not regrouped.
    private void ApplyFlurryRegroup(float deltaTime)
    {
        if (_flurryTarget && (!_flurryTarget.isActiveAndEnabled ||
            !_flurryTarget.TryGetComponent(out Actor current) || current.IsDead))
            _flurryTarget = null;

        if (!_flurryTarget)
        {
            // Acquire whatever the Flurry is actually punching in front of the player (a slightly wider
            // reach than the 2.0 m chain radius so a target at the edge of the punch can be locked).
            SoftGroupingService acquired = NearestGroupingEnemy(transform.position, transform.forward,
                FlurryRegroupDisplacement.MaxRadiusFromChainPoint + 1f);
            if (!acquired) return;
            _flurryTarget = acquired;
            // The chaining point is anchored to where the target was locked so the 2.0 m radius stays
            // stable as the target is punched, rather than drifting with the player (which would read
            // as an Asura-style vacuum instead of a subtle regroup).
            _flurryChainPoint = acquired.transform.position;
        }

        Vector3 delta = FlurryRegroupDisplacement.ComputeDisplacement(
            _flurryTarget.transform.position, _flurryChainPoint, deltaTime, _regroupConfig);
        if (delta != Vector3.zero) _flurryTarget.ApplyExternalDisplacement(delta);
    }

    // R7.3: while R (Asura) runs, gently attract every enemy within 4.0 m of the player toward that
    // centre — ≤2.0 m/s and ≤1.5 m total per enemy — never pulling enemies outside the radius. The
    // bounded per-enemy delta comes from the pure AsuraPullDisplacement; the 1.5 m total budget is
    // tracked per enemy in _asuraPulled and each enemy is moved through its own SoftGroupingService.
    private void ApplyAsuraPull(float deltaTime)
    {
        Vector3 center = transform.position;
        int count = Physics.OverlapSphereNonAlloc(center, AsuraPullDisplacement.Radius, _grouping,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Actor enemy = _grouping[i].GetComponentInParent<Actor>();
            if (!enemy || enemy == _player || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            if (!enemy.TryGetComponent(out SoftGroupingService locomotion)) continue;
            // Enemies outside the radius are never pulled (AsuraPullDisplacement returns zero); the
            // OverlapSphere already scopes to 4.0 m, but the pure guard keeps the rule scene-free.
            if (!AsuraPullDisplacement.IsWithinRadius(center, enemy.transform.position)) continue;

            _asuraPulled.TryGetValue(locomotion, out float moved);
            Vector3 delta = AsuraPullDisplacement.ComputeDisplacement(
                center, enemy.transform.position, moved, deltaTime);
            if (delta == Vector3.zero) continue;
            _asuraPulled[locomotion] = moved + locomotion.ApplyExternalDisplacement(delta).magnitude;
        }
    }

    // Finds the nearest living enemy carrying a SoftGroupingService within reach, biased to the
    // player's facing so the Flurry regroups the target it is actually punching. Mirrors ArsenalCombat's
    // enemy-search pattern (OverlapSphere + GetComponentInParent<Actor>) rather than a scene lookup.
    private SoftGroupingService NearestGroupingEnemy(Vector3 origin, Vector3 forward, float radius)
    {
        Vector3 heading = forward; heading.y = 0f;
        bool forwardOnly = heading.sqrMagnitude > .001f;
        if (forwardOnly) heading.Normalize();

        int count = Physics.OverlapSphereNonAlloc(origin, radius, _grouping,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        SoftGroupingService best = null; float nearest = radius * radius;
        for (int i = 0; i < count; i++)
        {
            Actor candidate = _grouping[i].GetComponentInParent<Actor>();
            if (!candidate || candidate == _player || candidate.IsDead || !candidate.isActiveAndEnabled) continue;
            if (!candidate.TryGetComponent(out SoftGroupingService locomotion)) continue;
            Vector3 flat = candidate.transform.position - origin; flat.y = 0f;
            if (forwardOnly && Vector3.Dot(flat, heading) < 0f) continue; // skip enemies behind the player
            float sqr = flat.sqrMagnitude;
            if (sqr < nearest) { nearest = sqr; best = locomotion; }
        }
        return best;
    }

    /// <summary>
    /// Executes a dash-cancel of the active cast: cashes the main finisher once, then ends the cast
    /// (R3.8). This is the cancel EXECUTION only — authorization is the shared
    /// <see cref="CancelResolver"/>'s job in <see cref="AbilityHolder"/>, which has already tested the
    /// authored Dash <see cref="CancelRule"/> at the current progress before calling here. Task 15.3
    /// removed the former hidden <c>CanDodgeCancel</c> (<c>_elapsed &gt;= .25f</c>) gate that lived here,
    /// so there is no longer a controller-side exception overriding the authored window; the method is a
    /// no-op only when nothing is executing (a defensive guard, not an authorization rule).
    /// </summary>
    public void FinishForDodge()
    {
        if (!IsExecuting || !_activeAbility) return;
        // Cash out the main finisher once; optional echoes are traded for an immediate escape.
        if (!_finisherApplied && _activeSteps!=null)
        {
            int index=_activeAbility.HitSteps.Count-1;
            // When the single advance source is active, cash the finisher through the scheduler's
            // ledger (task 15.1): if the FixedUpdate clock already emitted that index it is a no-op,
            // so the dodge-cancel never double-applies an impact the Active phase already fired.
            if (_scheduler != null)
            {
                if (!TryEmitFinisherThroughScheduler(index))
                    Impact(_activeAbility, _activeSteps[index], index);
            }
            else
            {
                Impact(_activeAbility,_activeSteps[index],index);
            }
        }
        CancelCast();
    }

    /// <summary>
    /// Routes the dodge-cancel finisher through the scheduler's ledger so it is counted at most once
    /// across the logical clock and this early cash-out (task 15.1, R8.9). Finds the authored
    /// ImpactEvent whose <see cref="ImpactEvent.AreaHitStepIndex"/> matches the finisher step and
    /// signals it; returns <c>true</c> when a matching authored event was handled (emitted or already
    /// emitted), <c>false</c> when the profile authors no event for that step so the caller falls back
    /// to a direct <see cref="Impact"/>.
    /// </summary>
    private bool TryEmitFinisherThroughScheduler(int finisherStepIndex)
    {
        CombatActionProfile profile = _activeAbility ? _activeAbility.CombatProfile : null;
        if (profile == null || profile.ImpactEvents == null) return false;
        for (int i = 0; i < profile.ImpactEvents.Count; i++)
        {
            if (profile.ImpactEvents[i].AreaHitStepIndex != finisherStepIndex) continue;
            _scheduler.Signal(i); // ledger-gated: emits once or no-ops if the clock beat us to it
            return true;
        }
        return false;
    }

    private void RestoreCastState()
    {
        if (!IsExecuting) return;
        if (_animation) _animation.Release();
        if (_player) _player.RemoveDamageTakenMultiplier(this);
        if (_agentLocked && _agent)
        {
            _agent.updateRotation = _savedRotation;
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = _savedStopped;
        }
        _agentLocked = false;
        IsExecuting = false;
        // Close any ImpactWindow still open and release the single advance source so a cancelled or
        // finished execution never leaves a window alive or a stale scheduler ticking (task 15.1, R2.6).
        if (_scheduler != null) { _scheduler.StopAndCloseWindows(); _scheduler = null; }
        _activeAbility = null; _activeSteps = null;
        _flurryTarget = null; _asuraPulled.Clear();
    }

    private void CancelCast()
    {
        if (_cast != null) StopCoroutine(_cast);
        _cast = null;
        RestoreCastState();
    }

    private void OnDeath(Actor actor) { CancelCast(); _momentum.Reset(); }
    private void OnDisable() { CancelCast(); _momentum.Reset(); }
    private void OnDestroy() { if (_player) _player.Died -= OnDeath; }

}
