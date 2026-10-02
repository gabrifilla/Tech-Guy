using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AbilityHolder : MonoBehaviour
{
    private static readonly KeyCode[] DefaultAbilityKeys =
    {
        KeyCode.Q,
        KeyCode.W,
        KeyCode.E,
        KeyCode.R
    };

    [Header("Weapon Abilities")]
    public KeyCode[] keys;

    [Header("Run Passives")]
    [SerializeField] private Ability[] passiveAbilities;

    [Obsolete("Active abilities are loaded from the equipped WeaponScript. Use passiveAbilities for run passives.")]
    public Ability[] abilities;

    private readonly List<Ability> acquiredPassiveAbilities = new List<Ability>();
    private Ability[] activeAbilities = Array.Empty<Ability>();
    private float[] cooldownTimers = Array.Empty<float>();
    private float[] activeTimers = Array.Empty<float>();
    private AbilityState[] states = Array.Empty<AbilityState>();
    private PlayerActor playerActor;
    private WeaponScript currentWeapon;
    private BreakerGauntletCombat _breakerCombat;
    private ArsenalCombat _arsenalCombat;
    private RunBoons _runBoons;
    private PauseMenuUI _pause;
    public void BindPause(PauseMenuUI pause) => _pause = pause;
    public void BindRun(RunBoons run) => _runBoons = run;
    public void RefreshLoadout() => RefreshWeaponAbilities(true);
    private CharControlScript _characterControl;
    [SerializeField] private LobbyInteraction _lobbyInteraction;
    // Single-slot Buffer_de_Input (R5) owned by the holder. In this subtask it carries the dash
    // Command_Intent when the only impediment to a dash-cancel is temporal — the Dash CancelRule is
    // still closed but the dash itself is available (R6.4). The dash is then executed on the first
    // frame the Dash rule opens (or when the active action ends), reusing the pure InputBuffer's
    // "fire once when the window opens" contract. Skill/basic buffering flows through the same slot
    // in later subtasks; here only the dash command path stores into it.
    private readonly InputBuffer _inputBuffer = new InputBuffer();
    // The dash referenced by a buffered Dash intent, kept so the per-frame retry can re-attempt the
    // exact dash that was requested without re-deriving it. Cleared whenever the buffer empties.
    private DashScript _bufferedDash;
    // Monotonic tie-break for same-timestamp Command_Intents stored this frame (R5.7).
    private long _intentSequence;
    public event Action<int> AbilityUsed;
    public event Action<int, AbilityUseFailure> AbilityRejected;
    // Resolved-hit notification (impactful-weapon-boons R4.3): raised right after the passive
    // AttackPassiveAbility dispatch in NotifyAttackHits, carrying the same owner + damaged-actor list.
    // Lets run-scoped cosmetic components (e.g. PerfectSpacingFeedback) subscribe to the resolved-hit
    // channel without a scene lookup or touching any source asset. Cosmetic subscribers only.
    public event Action<PlayerActor, IReadOnlyList<Actor>> AttackHitsResolved;

    public bool IsCasting => (_breakerCombat && _breakerCombat.IsExecuting) ||
        (_arsenalCombat && _arsenalCombat.IsExecuting);

    /// <summary>
    /// True while the active cast permits the player to keep moving (R10.1): the Arco fires on the
    /// move at a reduced speed instead of pinning the player. Melee (Manoplas/Lança) casts leave this
    /// false so they keep their full stop. Consumers still treat this as casting for ability/attack
    /// gating — it only unblocks locomotion.
    /// </summary>
    public bool MovementAllowedWhileCasting => _arsenalCombat && _arsenalCombat.IsExecuting &&
        _arsenalCombat.AllowsMovementWhileFiring;
    public bool BlocksWorldInput => Time.timeScale <= 0f || (_pause && _pause.BlocksInput) || (_lobbyInteraction && _lobbyInteraction.IsPanelOpen) ||
        (_runBoons && _runBoons.IsChoosing);

    private enum AbilityState
    {
        Ready,
        Active,
        Cooldown
    }

    private void Awake()
    {
        playerActor = GetComponent<PlayerActor>();
        _characterControl = GetComponent<CharControlScript>();
    }

    private void Start()
    {
        if (passiveAbilities?.Length > 0)
        {
            foreach (Ability passiveAbility in passiveAbilities)
            {
                AddPassiveAbility(passiveAbility);
            }
        }

        RefreshWeaponAbilities(force: true);
    }

    private void Update()
    {
        RefreshWeaponAbilities(force: false);

        for (int i = 0; i < activeAbilities.Length; i++)
        {
            TickAbility(i);
            if (IsAbilityKeyPressed(i)) TryUseAbility(i);
        }

        // R6.4: service any dash intent buffered on a temporal miss, firing it on the first frame the
        // Dash CancelRule opens (or when the active action ends). Ticked after the skills so a dash
        // buffered this frame is retried starting next frame.
        TickDashBuffer();
    }

    public IReadOnlyList<Ability> ActiveAbilities => activeAbilities;
    public IReadOnlyList<Ability> PassiveAbilities => acquiredPassiveAbilities;

    public void AddPassiveAbility(Ability passiveAbility)
    {
        if (passiveAbility && !acquiredPassiveAbilities.Contains(passiveAbility))
        {
            acquiredPassiveAbilities.Add(passiveAbility);

            if (passiveAbility is PassiveAbility passive)
            {
                passive.OnAcquired(playerActor);
            }

            if (playerActor)
            {
                playerActor.RefreshResourceStats();
            }
        }
    }

    public void RemovePassiveAbility(Ability passiveAbility)
    {
        if (!passiveAbility || !acquiredPassiveAbilities.Remove(passiveAbility)) return;

        if (passiveAbility is PassiveAbility passive)
        {
            passive.OnRemoved(playerActor);
        }

        if (playerActor)
        {
            playerActor.RefreshResourceStats();
        }
    }

    public void NotifyAttackHits(PlayerActor owner, IReadOnlyList<Actor> damagedActors)
    {
        if (!owner || damagedActors is null || damagedActors.Count == 0) return;

        foreach (Ability passiveAbility in acquiredPassiveAbilities)
        {
            if (passiveAbility is AttackPassiveAbility attackPassive)
            {
                attackPassive.OnAfterAttackHits(owner, damagedActors);
            }
        }

        AttackHitsResolved?.Invoke(owner, damagedActors);
    }

    private void RefreshWeaponAbilities(bool force)
    {
        WeaponScript equippedWeapon = playerActor ? playerActor.CurrentWeapon : null;
        if (!force && equippedWeapon == currentWeapon) return;

        currentWeapon = equippedWeapon;
        if (_arsenalCombat) _arsenalCombat.Cancel();
        activeAbilities = currentWeapon && currentWeapon.abilities is not null
            ? currentWeapon.abilities
            : Array.Empty<Ability>();

        cooldownTimers = new float[activeAbilities.Length];
        activeTimers = new float[activeAbilities.Length];
        states = new AbilityState[activeAbilities.Length];

        bool usesBreaker = Array.Exists(activeAbilities, ability => ability is BreakerGauntletAbility);
        if (Array.Exists(activeAbilities, ability => ability is ArsenalAbility) && !_arsenalCombat)
            _arsenalCombat = gameObject.AddComponent<ArsenalCombat>();
        if (usesBreaker && !_breakerCombat)
            _breakerCombat = gameObject.AddComponent<BreakerGauntletCombat>();
        if (_breakerCombat) _breakerCombat.Configure(usesBreaker ? currentWeapon : null);

        if (keys is null || keys.Length < activeAbilities.Length)
        {
            Debug.LogWarning("AbilityHolder: keys array has fewer entries than weapon abilities. Default Q/W/E/R bindings will be used where needed.", this);
        }
    }

    private void TickAbility(int index)
    {
        Ability ability = activeAbilities[index];
        if (!ability) return;

        switch (states[index])
        {
            case AbilityState.Ready:
                break;
            case AbilityState.Active:
                TickActive(index, ability);
                break;
            case AbilityState.Cooldown:
                TickCooldown(index);
                break;
        }
    }

    public bool TryUseAbility(int index)
    {
        RefreshWeaponAbilities(false);
        if (index < 0 || index >= activeAbilities.Length || !activeAbilities[index]) return false;
        Ability ability = activeAbilities[index];
        // R13.3/R13.4 — a slot that is not Ready is on cooldown/active and cannot be re-activated.
        // Kept as the first gate (and mirrored by AbilityActivationDecision) so the contract reads
        // identically to the pure model that Properties 41–43 exercise.
        if (states[index] != AbilityState.Ready) return Reject(index, AbilityUseFailure.Cooldown);
        if (!CheckUse(index, ability, ResolveKey(index))) return false;

        // R13.2 — ability.TryActivate deducts exactly ManaCost via PlayerActor.TrySpendMana; if the
        // ability's own requirement fails the reserve is left untouched (R13.4).
        if (!ability.TryActivate(gameObject)) return Reject(index, AbilityUseFailure.Requirement);
        states[index] = AbilityState.Active;
        activeTimers[index] = Mathf.Max(0f, ability.activeTime);
        if (ability is ArsenalAbility && _arsenalCombat)
            activeTimers[index] = Mathf.Max(activeTimers[index], _arsenalCombat.ExecutionDuration);
        if (ability is BreakerGauntletAbility && _breakerCombat)
            activeTimers[index] = Mathf.Max(activeTimers[index], _breakerCombat.ExecutionDuration);

        // R13.2 — leaving Ready starts the recharge: slots with an active window run it down first
        // (Active), the rest go straight to Cooldown. This transition matches
        // AbilityActivationDecision.Evaluate's resulting state.
        if (activeTimers[index] <= 0f)
        {
            states[index] = AbilityState.Cooldown;
            cooldownTimers[index] = GetCooldownDuration(ability);
        }
        AbilityUsed?.Invoke(index);
        return true;
    }

    private bool CheckUse(int index, Ability ability, KeyCode key)
    {
        if (BlocksWorldInput) return Reject(index, AbilityUseFailure.Interaction);
        if (!isActiveAndEnabled || !playerActor || playerActor.IsDead) return Reject(index, AbilityUseFailure.Unavailable);
        if (_lobbyInteraction && _lobbyInteraction.BlocksAbilityInput(key)) return Reject(index, AbilityUseFailure.Interaction);
        // R4.4/R4.6/R4.7/R5.2/R5.8 — the old total block (`if (IsCasting || isDashing) Reject(Busy)`)
        // is replaced by consulting the shared CancelResolver against the active action's authored
        // CancelRuleSet. Only the cancel DECISION changes here; IsCasting/MovementAllowedWhileCasting
        // keep returning the same observable values (contract-audit O-9). A dash in progress has no
        // authored skill-cancel contract in this subtask, so it stays a terminal Busy as before.
        if (_characterControl && _characterControl.isDashing) return Reject(index, AbilityUseFailure.Busy);
        // Dash (index 4) is NOT routed through the skill resolver here — TryUseDash already resolved
        // its Dash-target cancel through the shared CancelResolver BEFORE calling CheckUse, so by the
        // time a dash reaches this method IsCasting is false (the source was either authorized-cancelled
        // or the press was buffered/rejected). Only skill slots (0..3) consult the resolver with
        // CancelTarget.Skill here; the trailing Busy for a non-skill slot is a defensive fallback.
        bool isSkillSlot = index >= 0 && index < 4;
        if (IsCasting && (!isSkillSlot || !TryCancelActiveForSkill(index)))
            return isSkillSlot ? false : Reject(index, AbilityUseFailure.Busy);
        if (!ability.CanActivate(gameObject)) return Reject(index, AbilityUseFailure.Requirement);
        if (!playerActor.HasMana(ability.ManaCost)) return Reject(index, AbilityUseFailure.NotEnoughMana);
        if (_characterControl) _characterControl.CancelCombo();
        return true;
    }

    /// <summary>
    /// Routes a skill request issued while an action is in progress through the shared
    /// <see cref="CancelResolver"/> (CancelTarget.Skill) instead of blocking unconditionally
    /// (R4.4/R4.6/R4.7). Returns <c>true</c> when the active action was authorized to cancel (and was
    /// canceled) so <see cref="CheckUse"/> may continue; returns <c>false</c> after emitting exactly one
    /// terminal <see cref="AbilityUseFailure.Busy"/> rejection (contract-audit O-6) when the cancel is
    /// denied for any reason. Availability/mana/cooldown of the incoming skill are still checked by the
    /// rest of <see cref="CheckUse"/>/<see cref="TryUseAbility"/>, so <paramref name="index"/>'s own
    /// Requirement/NotEnoughMana rejections remain unchanged.
    /// </summary>
    private bool TryCancelActiveForSkill(int index)
    {
        // Shared resolution against the active action's authored CancelRuleSet (see ResolveCancel):
        // the incoming skill's own availability (mana/cooldown/requirement) is validated later in
        // CheckUse/TryUseAbility, so the resolver's availability step is a pass-through here.
        CancelDecision decision = ResolveCancel(CancelTarget.Skill, _ => true);

        if (decision == CancelDecision.Authorized)
        {
            // Authorized: cancel the active action, then let CheckUse continue (its CanActivate/mana
            // checks may still Reject for their own reasons, which is correct).
            CancelActiveAction();
            return true;
        }

        // Every denial (DeniedWindowClosed / DeniedCommandInvalid / DeniedUnavailable /
        // DeniedInfeasible) keeps the active action unchanged (no phase change) and surfaces as a
        // single terminal Busy — the same observable failure the HUD expects, emitted at most once
        // (O-6). Temporal buffering ("retry when the window opens") is wired with the InputBuffer in a
        // later subtask; here we neither loop nor re-reject.
        // TODO(13.4→buffer): a temporal miss (DeniedWindowClosed/DeniedInfeasible) could be buffered via
        // the AbilityHolder-owned InputBuffer and retried when the Skill rule opens (tasks 5/6). Deferred
        // to keep this subtask tight; current behavior preserves the single terminal Busy rejection.
        return Reject(index, AbilityUseFailure.Busy);
    }

    /// <summary>
    /// Shared cancel resolution against the active action for a given <paramref name="target"/>
    /// (R4.3). Resolves the active action's authored <see cref="CombatActionProfile"/> and the
    /// normalized progress, builds its <see cref="CancelRuleSet"/>, and delegates the safe ordering
    /// to the single shared <see cref="CancelResolver"/>. Both offensive executors feed this one path
    /// through their <c>TryGetCancelContext</c>: the Manopla (<see cref="BreakerGauntletCombat"/>) and,
    /// since task 18.1's Adaptador_de_Compatibilidade (R8.4/R8.6), the Arco/Lança
    /// (<see cref="ArsenalCombat"/>). The shipped Arco/Lança abilities author no profile, so the Arco
    /// adapter yields a <c>null</c> profile while executing, which the resolver reads as
    /// <see cref="CancelDecision.DeniedWindowClosed"/> — the legacy total block, backward compatible by
    /// construction. When no action is executing the set is empty, so the resolver again denies (window
    /// closed), and the caller treats "not casting" separately.
    /// </summary>
    /// <param name="target">The destination the player is trying to cancel into (Skill/Dash).</param>
    /// <param name="availability">Coordinator-supplied availability of <paramref name="target"/> (R4.3 step 3).</param>
    /// <returns>The <see cref="CancelDecision"/> from the shared resolver.</returns>
    private CancelDecision ResolveCancel(CancelTarget target, DestinationAvailability availability)
    {
        // Task 15.2: the profile + global progress come from the active executor's single computation
        // (TryGetCancelContext) instead of being recomputed here. The CancelRules are authored over
        // global [0, 1] progress, so this is the global fraction the resolver needs; each executor also
        // reports its (ActionPhase, localProgress) via TryGetActionProgress for the Channel/Hold work in
        // task 15.3, which this cancel path does not need.
        //
        // Task 18.1 (Adaptador_de_Compatibilidade, R8.4/R8.6): the Arco (ArsenalCombat) is now routed
        // through the SAME single path via its own TryGetCancelContext/TryGetCommitment, instead of the
        // ad-hoc "null-profile" assumption this method used to carry as a deferral. The shipped Arco/Lança
        // abilities author no profile, so the adapter hands back a null profile while executing, which the
        // CancelResolver reads as DeniedWindowClosed ⇒ the legacy total block (Busy) — exactly today's
        // behavior, now made EXPLICIT and backward compatible by construction instead of incidental. Only
        // one offensive executor is ever active at a time (weapon-exclusive), so consulting the Manopla
        // first and the Arco otherwise cannot double-count.
        CombatActionProfile profile = null;
        float progress = 0f;
        if (_breakerCombat && _breakerCombat.TryGetCancelContext(out CombatActionProfile breakerActive, out float breakerProgress))
        {
            profile = breakerActive;
            progress = breakerProgress;
            // Resolve the active action's CommitmentCategory through the single CommitmentRules.Resolve
            // path so the core has the commitment profile available (R3.1/R3.2). The per-phase movement
            // fraction is intentionally not applied to locomotion here — doing so would risk changing the
            // observable pin/move behavior the Manopla has today (contract-audit O-9); wiring the actual
            // locomotion fraction is left as a documented follow-up. Resolving it has no side effect.
            if (_breakerCombat.TryGetCommitment(out _)) { /* commitment available to the core; no behavior change (O-9) */ }
        }
        else if (_arsenalCombat && _arsenalCombat.TryGetCancelContext(out CombatActionProfile arsenalActive, out float arsenalProgress))
        {
            profile = arsenalActive; // null for the shipped Arco/Lança ⇒ legacy total block preserved
            progress = arsenalProgress;
            // Same read-only commitment resolution as the Manopla branch, through the single adapter so
            // the core sees Arco/Lança commitment via one path (R3.10 default when unauthored). No
            // locomotion/pin change: IsCasting / MovementAllowedWhileCasting keep their values (O-9).
            if (_arsenalCombat.TryGetCommitment(out _)) { /* commitment available to the core; no behavior change (O-9) */ }
        }

        CancelRuleSet rules = profile != null ? profile.BuildCancelRuleSet() : null;

        return CancelResolver.Resolve(
            rules,
            target,
            progress,
            commandStillValid: true,
            availability: availability,
            transitionFeasible: true);
    }

    /// <summary>
    /// Cancels whichever offensive action is currently executing, reusing the existing termination
    /// points (contract-audit O-8): the Manopla's <see cref="BreakerGauntletCombat.FinishForDodge"/>
    /// (which cashes out its finisher once and ends the cast) or the Arco's
    /// <see cref="ArsenalCombat.Cancel"/>. A no-op when nothing is executing.
    /// </summary>
    private void CancelActiveAction()
    {
        if (_breakerCombat && _breakerCombat.IsExecuting) _breakerCombat.FinishForDodge();
        else if (_arsenalCombat && _arsenalCombat.IsExecuting) _arsenalCombat.Cancel();
    }

    public bool TryUseDash(DashScript dash)
    {
        if (!dash) return false;

        // R6.6 / contract-audit O-5: the dash's own cooldown/availability is checked FIRST, before
        // any source action is canceled. A dash on cooldown rejects once with the remaining cooldown
        // left completely untouched — the active action keeps running and never loses its phase.
        if (dash.GetRemainingCooldown(gameObject) > 0f) return Reject(4, AbilityUseFailure.Cooldown);

        if (IsCasting)
        {
            // An action is in progress: the dash-cancel is NOT an unconditional escape anymore. It is
            // routed through the same shared CancelResolver every other cancel uses (R6.2), testing
            // the active action's authored Dash CancelRule at the current progress together with the
            // dash's availability. This replaces the old ad-hoc FinishForDodge gate that cancelled an
            // Asura cast immediately, ignoring the cancel window (R6.2/R6.4 fix).
            CancelDecision decision = ResolveCancel(CancelTarget.Dash, _ => IsDashAvailable(dash));

            switch (decision)
            {
                case CancelDecision.Authorized:
                    // R6.3: Dash rule open AND dash available — cancel the active action, then start
                    // the dash from this eligible input. Falls through to activation below.
                    CancelActiveAction();
                    break;

                case CancelDecision.DeniedWindowClosed:
                case CancelDecision.DeniedInfeasible:
                    // R6.4: the Dash rule is still closed but the dash itself is available — the only
                    // impediment is temporal, so buffer the Dash intent and execute it on the first
                    // frame the Dash rule opens (handled by TickDashBuffer). No rejection is emitted
                    // for a temporal miss (O-6). If the dash is NOT available this is not purely
                    // temporal, so fall through to a single terminal rejection instead.
                    if (IsDashAvailable(dash))
                    {
                        BufferDashIntent(dash);
                        return false;
                    }
                    return Reject(4, AbilityUseFailure.Busy);

                default:
                    // R6.5/R6.6: DeniedUnavailable (dash unavailable while a window was open) — keep
                    // the active action unchanged (no phase change) and reject exactly once.
                    return Reject(4, AbilityUseFailure.Busy);
            }
        }

        // Not casting (or the active action was just authorized-cancelled above): run the dash through
        // the standard availability gates and activate it. CheckUse(4, …) handles BlocksWorldInput,
        // dead/inactive, lobby and mana; IsCasting is now false so it will not re-reject with Busy.
        if (!CheckUse(4, dash, GamePreferences.Binding(GameControl.Dash))) return false;
        return ActivateDash(dash);
    }

    /// <summary>
    /// Starts <paramref name="dash"/> and, on success, raises the dash events in the contract order
    /// (contract-audit O-5): <see cref="AbilityUsed"/>(4) first, then the run's <c>OnDash</c> hook —
    /// each exactly once. On a failed <see cref="DashScript.TryActivate"/> it rejects once with
    /// <see cref="AbilityUseFailure.Requirement"/> and raises no dash events.
    /// </summary>
    private bool ActivateDash(DashScript dash)
    {
        if (!dash.TryActivate(gameObject)) return Reject(4, AbilityUseFailure.Requirement);
        AbilityUsed?.Invoke(4);
        // R6.9: a dash succeeded — raise OnDash exactly once, AFTER AbilityUsed(4) (O-5). The bus is
        // reached through the RunBoons reference bound to this holder (no scene lookup) and is null
        // outside a run.
        if (_runBoons) _runBoons.Hooks?.RaiseDash();
        return true;
    }

    /// <summary>
    /// Whether the dash can be serviced right now independent of the cancel window (R6.3): not
    /// world-blocked, the holder active, the player alive with enough mana, a camera present (the
    /// dash reads the cursor), off cooldown, and not already dashing. Used as the resolver's
    /// <see cref="DestinationAvailability"/> for <see cref="CancelTarget.Dash"/> so cooldown/resource
    /// gating stays a single source of truth. Cooldown is also pre-checked in <see cref="TryUseDash"/>
    /// so a dash on cooldown rejects before any cancel (O-5).
    /// </summary>
    private bool IsDashAvailable(DashScript dash) =>
        dash && !BlocksWorldInput && isActiveAndEnabled && playerActor && !playerActor.IsDead &&
        playerActor.HasMana(dash.ManaCost) && Camera.main && dash.GetRemainingCooldown(gameObject) <= 0f &&
        !dash.IsDashing(gameObject);

    /// <summary>
    /// Stores a Dash <see cref="CommandIntent"/> in the single-slot <see cref="InputBuffer"/> when the
    /// only impediment to a dash-cancel is temporal (R6.4). The buffered dash is retried every frame
    /// by <see cref="TickDashBuffer"/> and fires on the first frame the Dash CancelRule opens. The aim
    /// (cursor) is re-derived by the dash itself at activation, so the intent carries no stale target.
    /// </summary>
    private void BufferDashIntent(DashScript dash)
    {
        _bufferedDash = dash;
        _inputBuffer.Store(new CommandIntent(
            CommandKind.Dash, null, Vector3.zero, Time.unscaledTime, _intentSequence++));
    }

    /// <summary>
    /// Per-frame tick for the buffered dash (R6.4). Expires the slot against the unscaled clock, then,
    /// while the Dash intent is still pending, attempts to consume it the moment the Dash CancelRule
    /// opens (resolver Authorized) or the action ends. On a successful consume it cancels the active
    /// action (if any) and activates the dash exactly as a live press would, preserving the
    /// AbilityUsed(4)→OnDash order (O-5). A dash that becomes unavailable or whose window never opens
    /// before expiry is dropped silently (O-6).
    /// </summary>
    private void TickDashBuffer()
    {
        _inputBuffer.Expire(Time.unscaledTime);
        if (!_inputBuffer.HasPending || !_bufferedDash) return;

        DashScript dash = _bufferedDash;

        // The applicable window is "the Dash cancel rule is open now" while an action runs, or "no
        // action is in progress" once the active action has ended (R5.3/R5.4). Compute whether the
        // dash may fire this frame, reusing the shared resolver so there is no second window rule.
        bool windowOpen;
        bool cancelActive = false;
        if (IsCasting)
        {
            CancelDecision decision = ResolveCancel(CancelTarget.Dash, _ => IsDashAvailable(dash));
            windowOpen = decision == CancelDecision.Authorized;
            cancelActive = windowOpen;
        }
        else
        {
            // Action ended: the dash can fire as long as it is available (cooldown/mana/etc.).
            windowOpen = IsDashAvailable(dash);
        }

        if (!_inputBuffer.TryConsume(Time.unscaledTime, windowOpen, out _))
        {
            // Not fired this frame: either expired (already cleared by Expire/TryConsume) or the
            // window is still closed (kept, no rejection — O-6). Clear our dash ref if the slot
            // emptied so a later unrelated store doesn't resurrect it.
            if (!_inputBuffer.HasPending) _bufferedDash = null;
            return;
        }

        _bufferedDash = null;
        if (cancelActive) CancelActiveAction();
        ActivateDash(dash);
    }

    private bool Reject(int index, AbilityUseFailure reason)
    {
        AbilityRejected?.Invoke(index, reason);
        return false;
    }

    public float GetRemainingCooldown(int index) =>
        index >= 0 && index < cooldownTimers.Length ? Mathf.Max(0f, cooldownTimers[index]) : 0f;

    /// <summary>
    /// Reduces the remaining cooldown of every slot currently in Cooldown by <paramref name="seconds"/>,
    /// clamped at zero. Acts only on the live run-scoped cooldownTimers; never reads or writes the source
    /// ability's authored cooldownTime (R6.2). A non-positive value is a no-op (R6.1), and slots that are
    /// Ready or Active are left untouched (R6.3).
    /// </summary>
    public void ReduceCooldowns(float seconds)
    {
        if (seconds <= 0f) return;                                      // R6.1
        for (int i = 0; i < cooldownTimers.Length; i++)
        {
            if (states[i] != AbilityState.Cooldown) continue;           // R6.3
            cooldownTimers[i] = Mathf.Max(0f, cooldownTimers[i] - seconds); // R6.2
        }
    }

    public KeyCode GetAbilityKey(int index) => ResolveKey(index);
    public float GetCooldownRatio(int index) => index >= 0 && index < activeAbilities.Length
        ? Mathf.Clamp01(GetRemainingCooldown(index) / Mathf.Max(0.001f, GetCooldownDuration(activeAbilities[index]))) : 0f;

    public bool IsAbilityActive(int index) =>
        index >= 0 && index < states.Length && states[index] == AbilityState.Active;

    private void TickActive(int index, Ability ability)
    {
        activeTimers[index] -= Time.deltaTime;
        if (activeTimers[index] > 0f) return;

        states[index] = AbilityState.Cooldown;
        cooldownTimers[index] = GetCooldownDuration(ability);
    }

    private void TickCooldown(int index)
    {
        cooldownTimers[index] -= Time.deltaTime;
        if (cooldownTimers[index] <= 0f)
        {
            states[index] = AbilityState.Ready;
        }
    }

    private bool IsAbilityKeyPressed(int index) => GamePreferences.WasPressed(ResolveKey(index));

    private KeyCode ResolveKey(int index) => index >= 0 && index < 4
        ? GamePreferences.Binding((GameControl)((int)GameControl.Skill1 + index)) : KeyCode.None;

    private float GetCooldownDuration(Ability ability)
    {
        float cooldownTime = ability ? ability.cooldownTime : 0f;
        float cooldownMultiplier = playerActor
            ? playerActor.Stats.CooldownMultiplier
            : 1f;
        return Mathf.Max(0f, cooldownTime * cooldownMultiplier);
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetKey(KeyCode keyCode, out Key key)
    {
        if (keyCode == KeyCode.None)
        {
            key = Key.None;
            return false;
        }

        if (keyCode == KeyCode.Return)
        {
            key = Key.Enter;
            return true;
        }

        if (keyCode == KeyCode.BackQuote)
        {
            key = Key.Backquote;
            return true;
        }

        if (keyCode == KeyCode.LeftControl)
        {
            key = Key.LeftCtrl;
            return true;
        }

        if (keyCode == KeyCode.RightControl)
        {
            key = Key.RightCtrl;
            return true;
        }

        if (keyCode == KeyCode.LeftCommand || keyCode == KeyCode.LeftApple)
        {
            key = Key.LeftMeta;
            return true;
        }

        if (keyCode == KeyCode.RightCommand || keyCode == KeyCode.RightApple)
        {
            key = Key.RightMeta;
            return true;
        }

        string name = keyCode.ToString();
        if (name.StartsWith("Alpha", StringComparison.Ordinal))
        {
            name = "Digit" + name.Substring("Alpha".Length);
        }
        else if (name.StartsWith("Keypad", StringComparison.Ordinal))
        {
            name = "Numpad" + name.Substring("Keypad".Length);
        }

        return Enum.TryParse(name, out key);
    }
#endif
}
