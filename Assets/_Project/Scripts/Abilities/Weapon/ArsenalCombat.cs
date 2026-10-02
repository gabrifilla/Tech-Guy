using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerActor))]
public sealed class ArsenalCombat : MonoBehaviour
{
    private PlayerActor _player;
    private NavMeshAgent _agent;
    private Coroutine _cast;
    private SkillAnimationPlayer _animation;
    private bool _locked, _wasStopped, _wasRotating;
    private float _agentBaseSpeed;         // R10.1: the full move speed restored when a reduced-fire cast ends
    private readonly Collider[] _overlap = new Collider[64]; // R7.5 ChainThrust / R7.4 sweep enemy search buffer

    // R10.1: the Arco fires on the move. Instead of the full agent stop the Manoplas/Lança use, a Bow
    // cast reduces the player's move speed to a fraction of full (strictly > 0 and < full) via the pure
    // BowFireMovement, reusing the existing NavMeshAgent.speed the player already navigates with. The
    // fraction is data (editable in the Inspector); the pure decision is injected so the MonoBehaviour
    // stays thin (AGENTS.md) and Property 33 can test the reduced speed scene-free.
    [Header("Arco — Movimento reduzido ao disparar (Requisito 10.1)")]
    [SerializeField] private BowFireMovementConfig _bowFireMovementConfig = BowFireMovementConfig.Default;
    private BowFireMovement _bowFireMovement;

    // R10.5: base stance damage a Bow W (Flecha Pesada) hit chips, scaled by the slot's BowLoopSteps
    // identity so a charged shot can crack a Heavy enemy's stance. Kept as data so numbers stay tunable.
    [SerializeField, Min(0f)] private float _bowStanceDamage = 45f;

    // True while a Bow cast is active. The player keeps moving (at reduced speed) during a Bow cast,
    // so CharControlScript consults this to process movement instead of pinning the player (R10.1).
    public bool AllowsMovementWhileFiring { get; private set; }

    // R9.1/9.2: the Lança's Sweet Spot rewards a direct tip thrust with a stronger stance hit. The
    // config is data (editable in the Inspector); the pure SweetSpot evaluator is injected here so the
    // MonoBehaviour stays thin (AGENTS.md). Its bonus applies only to thrust hits (Requisito 9.2).
    [Header("Lança — Sweet Spot (Requisito 9.1/9.2)")]
    [SerializeField] private SweetSpotConfig _sweetSpotConfig = SweetSpotConfig.Default;
    private SweetSpot _sweetSpot;

    // R7.4: base stance damage a Lança hit chips. Scaled per slot by SpearLoopSteps.Reaction.StanceScale
    // and by the Sweet Spot bonus on a tip thrust. Kept as data so numbers stay tunable.
    [SerializeField, Min(0f)] private float _spearStanceDamage = 40f;

    public bool IsExecuting { get; private set; }
    public float ExecutionDuration { get; private set; }

    // The ArsenalAbility currently being cast (null when idle), tracked only so the unified
    // (ActionPhase, localProgress) report below can read its authored CombatActionProfile. Arco/Lança
    // abilities are NOT authored with a CombatActionProfile in this phase — the full Arco adapter that
    // translates the legacy executor state into the Núcleo_Compartilhado's phase model is task 18.1 —
    // so TryGetActionProgress returns false ("no profile") for every shipped Arco/Lança ability today.
    // Keeping the field + report here (rather than in AbilityHolder) centralizes the "read the profile
    // to build the phase report" decision in the executor, matching BreakerGauntletCombat, so when 18.1
    // authors Arco/Lança profiles the core consumes them through the exact same single path.
    private ArsenalAbility _activeAbility;

    // Seconds elapsed in the current cast, mirrored from Execute's local loop clock so the unified
    // report has a progress source without changing the coroutine's own timing. Only read by
    // TryGetActionProgress (which no shipped ability reaches, since none author a profile yet).
    private float _elapsedForReport;

    /// <summary>Seconds elapsed in the current cast (0 when idle), used only by the phase report.</summary>
    private float ElapsedForReport => _elapsedForReport;

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _agent = GetComponent<NavMeshAgent>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
        _sweetSpot = new SweetSpot(_sweetSpotConfig);
        _bowFireMovement = new BowFireMovement(_bowFireMovementConfig);
        _player.Died += OnDeath;
    }

    public bool CanUse(ArsenalAbility ability) => isActiveAndEnabled && !IsExecuting &&
        !_player.IsDead && _player.CurrentWeapon && ability &&
        System.Array.IndexOf(_player.CurrentWeapon.abilities, ability) >= 0;

    public void Use(ArsenalAbility ability)
    {
        if (CanUse(ability)) _cast = StartCoroutine(Execute(ability));
    }

    /// <summary>
    /// Reports the active Arco/Lança action's unified <c>(ActionPhase, localProgress)</c> to the
    /// Núcleo_Compartilhado (task 15.2, R2.5/R8.8) from the active ability's authored
    /// <see cref="CombatActionProfile"/>, mirroring <see cref="BreakerGauntletCombat.TryGetActionProgress"/>
    /// so the core consumes one path. The shipped Arco/Lança abilities author <strong>no</strong>
    /// profile in this phase (the full Arco Adaptador_de_Compatibilidade is task 18.1), so this
    /// returns <c>false</c> ("no profile") for them today and the holder keeps the legacy total block
    /// for the Arco — deliberately deferred. When a profile IS present it computes the global progress
    /// as <c>Clamp01(Elapsed / ExecutionDuration)</c> and derives phase + phase-local progress through
    /// the shared <see cref="ActionTimeline"/>, Channel actions reported via <see cref="ChannelProgress"/>.
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
        if (profile == null) return false; // deferred to task 18.1 — Arco/Lança author no profile yet

        ActionTimeline timeline = profile.BuildTimeline();
        float globalProgress = Mathf.Clamp01(ElapsedForReport / Mathf.Max(0.0001f, ExecutionDuration));
        phase = timeline.PhaseOf(globalProgress);
        float phaseLocal = timeline.LocalProgressOf(globalProgress);

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

    // ---------------------------------------------------------------------------------------------
    // Adaptador_de_Compatibilidade — Arco/Lança ⇄ Núcleo_Compartilhado (task 18.1, R8.4/R8.6)
    //
    // The two methods below are a THIN, READ-ONLY translation layer that lets the shared cancel
    // pipeline in AbilityHolder treat an in-progress Arco/Lança action through the exact same single
    // path it already uses for the Manopla (BreakerGauntletCombat.TryGetCancelContext/TryGetCommitment),
    // instead of the ad-hoc "null-profile" assumption the holder carried as a deferral. They do NOT
    // change any Arco/Lança gameplay: firing-on-the-move, AttackHitsResolved emission/ordering,
    // projectile behavior, OnBasicHit, damage and timing are all untouched. IsCasting /
    // MovementAllowedWhileFiring keep their current values (contract-audit O-9) — the adapter only
    // answers the core's cancel/commitment queries by reflecting what the legacy executor really does.
    //
    // Because the shipped Arco/Lança abilities author NO CombatActionProfile, both methods report
    // "no authored profile" today: TryGetCancelContext hands the core a null profile (so the core
    // builds no CancelRuleSet and the CancelResolver returns DeniedWindowClosed ⇒ the legacy total
    // block — Busy — preserved exactly, O-9), and TryGetCommitment resolves the absent category
    // through CommitmentRules.Resolve(null, …) ⇒ Committed + warning (R3.10). The deferral is thus
    // made EXPLICIT and routed through the single core path rather than hidden in a holder null check.
    // When a future Arco/Lança ability authors a profile, the core consumes it through this same path
    // with zero further wiring — the adapter is the one place that reads the legacy executor state.
    // ---------------------------------------------------------------------------------------------

    /// <summary>
    /// Supplies the shared cancel pipeline in <see cref="AbilityHolder"/> with the active Arco/Lança
    /// action's authored <see cref="CombatActionProfile"/> and the <em>global</em> normalized progress
    /// (<c>Clamp01(Elapsed / ExecutionDuration)</c>) in ONE call, mirroring
    /// <see cref="BreakerGauntletCombat.TryGetCancelContext"/> so the core has a single cancel-context
    /// source for both executors (R8.4/R8.6). Returns <c>false</c> (profile <c>null</c>, progress
    /// <c>0</c>) when nothing is executing, so the holder treats "not casting" distinctly. Shipped
    /// Arco/Lança abilities author no profile, so this returns <c>true</c> with a <c>null</c> profile
    /// while a cast runs — which the <see cref="CancelResolver"/> reads as
    /// <see cref="CancelDecision.DeniedWindowClosed"/>, reproducing today's legacy total block for the
    /// Arco/Lança with no change in observable behavior.
    /// </summary>
    /// <param name="profile">The active ability's authored profile, or <c>null</c> when unauthored.</param>
    /// <param name="globalProgress">The global normalized progress of the active cast, in <c>[0, 1]</c>.</param>
    /// <returns><c>true</c> when an action is executing (even with a <c>null</c> profile).</returns>
    public bool TryGetCancelContext(out CombatActionProfile profile, out float globalProgress)
    {
        profile = null;
        globalProgress = 0f;
        if (!IsExecuting || !_activeAbility) return false;

        profile = _activeAbility.CombatProfile; // null for the shipped Arco/Lança (legacy total block)
        globalProgress = Mathf.Clamp01(ElapsedForReport / Mathf.Max(0.0001f, ExecutionDuration));
        return true;
    }

    /// <summary>
    /// Reports the active Arco/Lança action's resolved <see cref="CommitmentCategory"/> to the core
    /// through the single <see cref="CommitmentRules.Resolve"/> path (R3.1/R3.2/R3.10), mirroring
    /// <see cref="BreakerGauntletCombat.TryGetCommitment"/>. Reads the authored
    /// <see cref="CombatActionProfile.Commitment"/>; when the ability authors no profile (every shipped
    /// Arco/Lança today) the absent category resolves to <see cref="CommitmentRules.Default"/>
    /// (<see cref="CommitmentCategory.Committed"/>) with the backward-compatibility warning flagged.
    /// Returns <c>false</c> when nothing is executing. Pure read: it never mutates the design asset or
    /// the cast, so exposing the commitment to the core has no observable side effect (O-9).
    /// </summary>
    /// <param name="commitment">The resolved commitment category of the active action.</param>
    /// <returns><c>true</c> when an action is executing and a commitment was resolved.</returns>
    public bool TryGetCommitment(out CommitmentCategory commitment)
    {
        commitment = CommitmentRules.Default;
        if (!IsExecuting || !_activeAbility) return false;

        CombatActionProfile profile = _activeAbility.CombatProfile;
        commitment = profile != null
            ? profile.ResolveCommitment(out _)
            : CommitmentRules.Resolve(null, out _); // absent ⇒ Committed + warning (R3.10)
        return true;
    }

    private IEnumerator Execute(ArsenalAbility ability)
    {
        IsExecuting = true;
        _activeAbility = ability;
        _elapsedForReport = 0f;
        WeaponScript weapon = _player.CurrentWeapon;
        int slot = System.Array.IndexOf(weapon.abilities, ability);
        ArsenalCastPlan plan = _player.RunModifiers?.Plan(ability, slot) ?? new ArsenalCastPlan(ability);
        ExecutionDuration = plan.Windup + plan.Hits * plan.Interval;
        SequencedAreaAttackAbility.FaceMousePosition(transform);
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        Vector3 rainCenter = origin + direction * plan.Range;
        // Rain is clamped to the cursor distance, with a fixed ground target for the whole cast.
        if (Camera.main && UnityEngine.InputSystem.Mouse.current != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            if (new Plane(Vector3.up, origin).Raycast(ray, out float distance))
                rainCenter = origin + Vector3.ClampMagnitude(ray.GetPoint(distance) - origin, plan.Range);
        }
        bool bow = weapon.FiresArrows;
        _locked = _agent && _agent.enabled && _agent.isOnNavMesh;
        if (_locked)
        {
            _wasStopped = _agent.isStopped;
            _wasRotating = _agent.updateRotation;
            if (bow)
            {
                // R10.1: the Arco fires on the move. Keep the agent navigating (never a full stop) but
                // reduce its speed to the fraction of full the pure BowFireMovement decides — strictly
                // > 0 and < full. We reuse the existing NavMeshAgent.speed rather than a parallel channel;
                // the base speed is captured here and restored in Release. Rotation stays free so the
                // player can keep facing the cursor while stepping.
                _agentBaseSpeed = _agent.speed;
                _agent.speed = _bowFireMovement.ReducedSpeed(_agentBaseSpeed);
                _agent.isStopped = false;
                AllowsMovementWhileFiring = true;
            }
            else
            {
                _agent.ResetPath();
                _agent.isStopped = true;
                _agent.updateRotation = false;
            }
        }
        SkillMotion motion = weapon.FiresArrows ? (ability.Kind==ArsenalSkillKind.Rain ? SkillMotion.BowRain : SkillMotion.BowShot) :
            ability.Kind==ArsenalSkillKind.Thrust ? SkillMotion.SpearThrust :
            WeaponRunModifiers.Identify(weapon)==RunWeaponFamily.Gauntlet ? SkillMotion.Slam : SkillMotion.SpearSweep;
        try
        {
            float elapsed = 0f;
            for (int i = 0; i < plan.Hits; i++)
            {
                float impact = plan.Windup + i*plan.Interval;
                float start = i==0 ? 0 : impact-plan.Interval+Mathf.Min(.04f,plan.Interval*.25f);
                float end = i==plan.Hits-1 ? ExecutionDuration : impact+Mathf.Min(.04f,plan.Interval*.25f);
                while (elapsed < impact)
                {
                    _animation.Strike(motion,elapsed,start,impact,end);
                    yield return null;
                    elapsed += Time.deltaTime;
                    _elapsedForReport = elapsed; // mirror the loop clock for the phase report (task 15.2)
                }
                if (_player.IsDead || _player.CurrentWeapon != weapon) yield break;
                _animation.Contact(motion);
                if (ability.Kind == ArsenalSkillKind.Arrow || ability.Kind == ArsenalSkillKind.Volley)
                {
                    int arrows = plan.Arrows;
                    for (int arrow = 0; arrow < arrows; arrow++)
                    {
                        Vector3 aim = Quaternion.AngleAxis((arrow - (arrows - 1) * .5f) * Mathf.Min(9f, 100f / Mathf.Max(1, arrows - 1)), Vector3.up) * direction;
                        ArsenalProjectile.Fire(_player, origin + Vector3.up, aim, weapon.attackDamage,
                            plan.Damage, plan.Range, ability.Piercing, ability.AccentColor);
                    }

                    // Per-slot Arco identity (Requisito 10.4–10.6). The arrows carry the damage; this
                    // adds the identity a projectile alone can't express, built from data via the pure
                    // BowLoopSteps. A non-Bow weapon never reaches this block.
                    BowLoopSteps.Reaction bowId = BowLoopSteps.ReactionForSlot(slot, ability.Kind);

                    // R10.5 / 11.2: W (Flecha Pesada) applies a Heavy/Breaker stance hit along the shot
                    // line so a charged shot can crack a Heavy enemy's stance (KnockUp). Routed through
                    // the shared area-damage path (cascade-bounded) with zero extra damage — the arrows
                    // already dealt the damage; this hit only carries the stance reaction.
                    if (bowId.AppliesStanceReaction)
                    {
                        HitReactionRequest heavy = new HitReactionRequest(_player,
                            origin + direction.normalized * plan.Range, direction,
                            bowId.ReactionType, bowId.Strength, _bowStanceDamage * bowId.StanceScale,
                            bowId.BreakEffect, bowId.PushDistance, bowId.StunDuration, bowId.KnockUpHeight);
                        _player.TryApplyAreaDamage(origin + Vector3.up, direction, plan.Range,
                            new Vector3(plan.Width, 2f, plan.Range), AreaHitShape.Box, plan.Width,
                            Physics.DefaultRaycastLayers, weapon.attackDamage, 0f, 0f, heavy, false, ability.AccentColor);
                    }

                    // R10.6: E (Leque Amplo) grants an emergency backstep after firing the cone, clamped
                    // to the NavMesh so the recoil never leaves the navigable space.
                    if (bowId.BackstepDistance > 0f) ApplyBackstep(direction, bowId.BackstepDistance);
                }
                else
                {
                    bool radial = ability.Kind != ArsenalSkillKind.Thrust;
                    if (plan.TrackCursor && Camera.main && UnityEngine.InputSystem.Mouse.current != null)
                    {
                        Ray cursor = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                        if (new Plane(Vector3.up, origin).Raycast(cursor, out float cursorDistance))
                            rainCenter = origin + Vector3.ClampMagnitude(cursor.GetPoint(cursorDistance) - origin, plan.Range);
                    }
                    Vector3 center = ability.Kind == ArsenalSkillKind.Rain ? rainCenter : origin;
                    if (plan.Travel) center += direction * (i * 1.5f);
                    // Sphere helper raises its center by the radius; keep large sweeps at torso height.
                    center.y += radial ? 1f - plan.Width : 0f;
                    // Per-slot Lança identity (Requisito 9.3–9.6): Q counter thrust, W anti-swarm sweep,
                    // E Heavy/Breaker pierce that breaks Heavy (KnockUp), R Dragon Wave. Built from data
                    // via the pure SpearLoopSteps; a non-Spear weapon keeps the neutral baseline.
                    HitReactionRequest reaction = BuildSpearReaction(weapon, ability, slot, center, direction);
                    // R5.1 (Impaling Line): keep the thrust box at the full plan.Range so
                    // TryApplyAreaDamage damages every enemy along the line, not just the first. This is
                    // the existing box length, so behavior is unchanged when ImpaleLine is false.
                    float thrustReach = radial ? .01f : plan.Range;
                    int primaryHits = 0;
                    for (int branch = 0; branch < plan.Directions; branch++)
                    {
                        Vector3 aim = Quaternion.AngleAxis((branch - (plan.Directions - 1) * .5f) * 25f, Vector3.up) * direction;
                        primaryHits += _player.TryApplyAreaDamage(center, aim, thrustReach,
                            new Vector3(plan.Width, 2f, plan.Range), radial ? AreaHitShape.Sphere : AreaHitShape.Box,
                            plan.Width, Physics.DefaultRaycastLayers, weapon.attackDamage, plan.Damage, 0f, reaction, true, ability.AccentColor);

                    }

                    // R7.4/R9.4: the orbital sweep (W) nudges hit enemies along the sweep direction by a
                    // LIMITED amount (<=1.5 m/s, <=0.75 m total), routed through each enemy's existing
                    // locomotion means — never a hard knockback.
                    if (radial && ability.Kind == ArsenalSkillKind.Sweep && primaryHits > 0)
                        ApplySweepDisplacement(center, direction, plan);

                    // R12.1/R12.2/R12.3 (PikeWall): while the sweep runs and the plan declares a control
                    // zone, push each caught enemy OUTWARD from the zone centre through its own
                    // SoftGroupingService (never a teleport). Reuses the ApplySweepDisplacement pattern but
                    // inverts the direction to point outward from the centre, scaled by plan.ZonePush.
                    if (radial && ability.Kind == ArsenalSkillKind.Sweep && plan.ControlZone)
                        ApplyZonePush(center, plan);

                    // R10.1 (RainMark): each resolved rain pulse marks the enemies it caught. The per-cast
                    // plan declares MarkOnPulse from the boon rank, so this only runs for the Bow R while the
                    // boon is active. Marking goes through the run-scoped RainMarkRegistry (resolved from the
                    // player, no scene lookup); the registry applies the slow and answers the amplifier. The
                    // source ability asset is never touched (R10.4).
                    if (radial && ability.Kind == ArsenalSkillKind.Rain && plan.MarkOnPulse)
                        MarkRainPulse(center, plan);

                    if (plan.WaveMultiplier > 0)
                        // R7.4: ReturnWave flips the wave back toward the player at max range.
                        ArsenalProjectile.Fire(_player, origin + Vector3.up, direction, weapon.attackDamage,
                            plan.Damage * plan.WaveMultiplier, plan.Range * 2, true, ability.AccentColor, plan.ReturnWave);

                    // R7.3: MoonShard launches ShardCount (2-6) projectiles from the sweep extremities.
                    if (radial && ability.Kind == ArsenalSkillKind.Sweep && plan.ShardCount > 0)
                        FireMoonShards(center, direction, plan, weapon, ability);

                    // impactful-weapon-boons R5.2/R5.3 (Impaling Line): on a connecting thrust, pull each
                    // connected enemy with locomotion toward the player by up to plan.ImpalePull metres,
                    // through its own SoftGroupingService — never a hard impulse or teleport.
                    if (!radial && plan.ImpalePull > 0f && primaryHits > 0)
                        ApplyImpalePull(center, direction, plan);

                    // gauntlet-boon-playstyle-overhaul R11 (SpacingRecoil, "Recuo controlado"): on a
                    // CONNECTING thrust only (primaryHits > 0, R11.1/R11.4), step the player back toward the
                    // ideal spacing band. The backward step comes from the pure SpacingBand (scaled by rank,
                    // clamped to the band's upper edge, R11.2) and the move is routed through the player's own
                    // NavMeshAgent (Raycast + Move) so it respects the navmesh and never crosses scenery
                    // (R11.3). A whiff (primaryHits == 0) does nothing.
                    if (!radial && plan.SpacingRecoil && primaryHits > 0)
                        ApplySpacingRecoil(center, direction, plan,
                            _player.RunModifiers != null ? _player.RunModifiers.Rank(WeaponBoon.SpacingRecoil) : 0);

                    // R7.5/R7.6: on a connecting thrust, chain exactly one short thrust to a different nearby enemy.
                    if (!radial && plan.ChainThrust && primaryHits > 0)
                        TryChainThrust(center, direction, plan, weapon, ability, reaction);

                    // R7.2: PhantomSpear repeats the thrust with a spectral copy after a short delay.
                    if (!radial && plan.PhantomDelay > 0f)
                        StartCoroutine(SpectralThrust(center, direction, plan, weapon, ability, reaction, plan.PhantomDelay));
                }
                while (elapsed < end)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    _elapsedForReport = elapsed; // mirror the loop clock for the phase report (task 15.2)
                    _animation.Strike(motion,elapsed,start,impact,end);
                }
            }
        }
        finally { Release(); }
    }

    // Builds the per-hit reaction that carries the Lança's Q/W/E/R identity (Requisito 9.3–9.6). The
    // reaction values come from the pure SpearLoopSteps; stance damage is the data-driven base scaled by
    // the slot and by the Sweet Spot bonus on a direct tip thrust (Requisito 9.2). A non-Spear weapon
    // keeps the neutral baseline (Medium/Stagger/Stun) this method's Generic stage produces.
    private HitReactionRequest BuildSpearReaction(WeaponScript weapon, ArsenalAbility ability, int slot,
        Vector3 center, Vector3 direction)
    {
        bool isSpear = WeaponRunModifiers.Identify(weapon) == RunWeaponFamily.Spear;
        SpearLoopSteps.Reaction id = isSpear
            ? SpearLoopSteps.ReactionForSlot(slot, ability.Kind)
            : SpearLoopSteps.ReactionForStage(SpearLoopStage.Generic, ability.Kind);

        float stance = _spearStanceDamage * id.StanceScale;

        // R9.2: a direct thrust that lands in the tip Sweet Spot gets a strictly larger stance hit. The
        // hit point straight ahead approximates the thrust contact; the sweep is not a tip thrust.
        if (isSpear && id.IsThrust && _sweetSpot != null)
        {
            Vector3 tip = center + direction.normalized * ability.Range;
            SweetSpot.Result sweet = _sweetSpot.Evaluate(tip, center, direction, ability.Range);
            stance *= sweet.StanceMultiplier; // 1.0 outside, > 1.0 inside (Requisito 9.2)
        }

        return new HitReactionRequest(_player, center, direction, id.ReactionType, id.Strength,
            stance, id.BreakEffect, id.PushDistance, id.StunDuration, id.KnockUpHeight);
    }

    // R7.4/R9.4: apply the LIMITED sweep displacement to enemies the orbital sweep hit. Each swept enemy
    // is glided along the sweep direction by SpearSweepDisplacement (<=1.5 m/s, <=0.75 m total) through
    // its own existing locomotion (SoftGroupingService), reusing that means instead of a parallel
    // channel. Enemies without a SoftGroupingService are simply not displaced (no hard impulse).
    private void ApplySweepDisplacement(Vector3 center, Vector3 direction, ArsenalCastPlan plan)
    {
        Vector3 sweepDir = direction; sweepDir.y = 0f;
        if (sweepDir.sqrMagnitude < .001f) return;
        sweepDir.Normalize();

        float radius = Mathf.Max(.1f, plan.Width);
        int count = Physics.OverlapSphereNonAlloc(center, radius, _overlap,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Actor enemy = _overlap[i].GetComponentInParent<Actor>();
            if (!enemy || enemy == _player || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            if (!enemy.TryGetComponent(out SoftGroupingService locomotion)) continue;
            StartCoroutine(GlideSweptEnemy(locomotion, enemy, sweepDir));
        }
    }

    // Glides a single swept enemy along the sweep direction over multiple frames, spending the 0.75 m
    // total budget at no more than 1.5 m/s (R7.4) — never an instantaneous impulse past that limit. The
    // per-frame step is decided by the pure SpearSweepDisplacement and moved through the enemy's own
    // locomotion; the glide stops as soon as the total budget is spent or the enemy dies.
    private IEnumerator GlideSweptEnemy(SoftGroupingService locomotion, Actor enemy, Vector3 sweepDir)
    {
        float moved = 0f;
        while (moved < SpearSweepDisplacement.MaxTotalDisplacement)
        {
            if (!enemy || enemy.IsDead || !enemy.isActiveAndEnabled || !locomotion) yield break;
            Vector3 delta = SpearSweepDisplacement.ComputeDisplacement(sweepDir, moved, Time.deltaTime);
            if (delta == Vector3.zero) yield break;
            moved += locomotion.ApplyExternalDisplacement(delta).magnitude;
            yield return null;
        }
    }

    // R12.1/R12.2/R12.3 (PikeWall): push each enemy caught by the control-zone sweep OUTWARD from the
    // zone centre. Mirrors ApplySweepDisplacement, but each enemy is glided along the centre->enemy
    // heading (outward) rather than along the sweep direction, with the magnitude scaled by
    // plan.ZonePush (the +30%-per-rank term). The push is routed through each enemy's own
    // SoftGroupingService (R12.3) — the same bounded-displacement locomotion channel the sweep reuses —
    // so an enemy without that service is simply not pushed (no hard impulse, nothing teleports through
    // scenery). Driven entirely off the per-cast plan snapshot (R12.4); the source asset is never touched.
    private void ApplyZonePush(Vector3 center, ArsenalCastPlan plan)
    {
        float radius = Mathf.Max(.1f, plan.Width);
        int count = Physics.OverlapSphereNonAlloc(center, radius, _overlap,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Actor enemy = _overlap[i].GetComponentInParent<Actor>();
            if (!enemy || enemy == _player || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            if (!enemy.TryGetComponent(out SoftGroupingService locomotion)) continue;
            StartCoroutine(GlidePushedEnemy(locomotion, enemy, center, plan.ZonePush));
        }
    }

    // Glides a single caught enemy outward from the zone centre over multiple frames. The outward
    // heading (centre->enemy) is recomputed each frame so the push always points away from the centre,
    // and the magnitude is scaled by (1 + zonePush): the sweep's budget/speed caps grow by the
    // +30%-per-rank factor so a higher PikeWall rank pushes farther (R12.2). The step is moved through
    // the enemy's own locomotion (R12.3); the glide stops as soon as the scaled total budget is spent,
    // the enemy dies, or the heading becomes degenerate (enemy exactly on the centre).
    private IEnumerator GlidePushedEnemy(SoftGroupingService locomotion, Actor enemy, Vector3 center, float zonePush)
    {
        float scale = 1f + Mathf.Max(0f, zonePush);
        float budget = SpearSweepDisplacement.MaxTotalDisplacement * scale;
        float moved = 0f;
        while (moved < budget)
        {
            if (!enemy || enemy.IsDead || !enemy.isActiveAndEnabled || !locomotion) yield break;
            Vector3 outward = enemy.transform.position - center; outward.y = 0f;
            if (outward.sqrMagnitude < .0001f) yield break; // degenerate: enemy sits on the centre
            outward.Normalize();
            // Per-step magnitude mirrors SpearSweepDisplacement's clamp (speed cap x deltaTime, bounded by
            // the remaining budget), with the whole budget scaled by the rank factor so the outward push
            // reaches 0.75 * (1 + 0.3R) m total at <= 1.5 * (1 + 0.3R) m/s — never an instantaneous impulse.
            float step = Mathf.Min(SpearSweepDisplacement.MaxSpeed * scale * Time.deltaTime, budget - moved);
            if (step <= 0f) yield break;
            moved += locomotion.ApplyExternalDisplacement(outward * step).magnitude;
            yield return null;
        }
    }

    // R10.1 (RainMark): mark every enemy caught by a resolved rain pulse. The pulse hits a sphere at
    // `center` with radius plan.Width (the same radial sphere the pulse's TryApplyAreaDamage used), so we
    // overlap that sphere and feed each live enemy to the run-scoped RainMarkRegistry — resolved from the
    // player (RunBoons ownership, no scene lookup) and null outside an active run / without the boon. The
    // registry owns the mark duration, the slow, and the amplifier; this method only reports who was hit.
    private void MarkRainPulse(Vector3 center, ArsenalCastPlan plan)
    {
        RainMarkRegistry registry = _player ? _player.RainMarks : null;
        if (registry == null) return;
        float radius = Mathf.Max(.1f, plan.Width);
        int count = Physics.OverlapSphereNonAlloc(center, radius, _overlap,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        for (int i = 0; i < count; i++)
        {
            Actor enemy = _overlap[i].GetComponentInParent<Actor>();
            if (!enemy || enemy == _player || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            registry.Mark(enemy);
        }
    }

    // impactful-weapon-boons R5.2/R5.3 (Impaling Line): pull each enemy connected by the thrust toward
    // the player by up to plan.ImpalePull metres. Enemies are searched along the thrust line and glided
    // through their own SoftGroupingService (the same locomotion channel the sweep push reuses), so an
    // enemy without that service is simply not pulled (no hard impulse) and nothing teleports through
    // scenery. Driven entirely off the per-cast plan snapshot (R5.4); the source asset is never touched.
    private void ApplyImpalePull(Vector3 center, Vector3 direction, ArsenalCastPlan plan)
    {
        Vector3 lineDir = direction; lineDir.y = 0f;
        if (lineDir.sqrMagnitude < .001f) return;
        lineDir.Normalize();

        // Search a sphere that covers the whole thrust line, then filter to enemies inside the line box.
        float halfLine = Mathf.Max(.1f, plan.Range * .5f);
        Vector3 lineMid = center + lineDir * halfLine;
        float searchRadius = halfLine + Mathf.Max(.1f, plan.Width);
        int count = Physics.OverlapSphereNonAlloc(lineMid, searchRadius, _overlap,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        float halfWidth = Mathf.Max(.1f, plan.Width);
        for (int i = 0; i < count; i++)
        {
            Actor enemy = _overlap[i].GetComponentInParent<Actor>();
            if (!enemy || enemy == _player || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            Vector3 delta = enemy.transform.position - center; delta.y = 0f;
            float along = Vector3.Dot(delta, lineDir);
            if (along < 0f || along > plan.Range) continue;                 // outside the thrust line
            float lateral = (delta - lineDir * along).magnitude;
            if (lateral > halfWidth) continue;                              // outside the line width
            if (!enemy.TryGetComponent(out SoftGroupingService locomotion)) continue; // R5.3: locomotion only
            StartCoroutine(GlideImpaledEnemy(locomotion, enemy, plan.ImpalePull));
        }
    }

    // Glides a single impaled enemy toward the player over multiple frames, spending the plan.ImpalePull
    // budget (0.75 m per ImpalingLine rank) at no more than SpearSweepDisplacement.MaxSpeed, recomputing
    // the toward-player heading each frame so the enemy tracks the player through its own locomotion. The
    // glide stops as soon as the budget is spent, the enemy dies, or it reaches the player (R5.2/R5.3).
    private IEnumerator GlideImpaledEnemy(SoftGroupingService locomotion, Actor enemy, float pull)
    {
        float moved = 0f;
        while (moved < pull)
        {
            if (!enemy || enemy.IsDead || !enemy.isActiveAndEnabled || !locomotion || !_player) yield break;
            Vector3 toPlayer = _player.transform.position - enemy.transform.position; toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance < .001f) yield break;                              // already at the player
            // Per-step magnitude: the smaller of the speed cap, the remaining pull budget, and the
            // distance left to the player. This keeps the pull smooth (no hard impulse) while letting a
            // higher ImpalingLine rank pull farther (up to 0.75 * rank) since the budget scales with rank.
            float step = Mathf.Min(SpearSweepDisplacement.MaxSpeed * Time.deltaTime,
                Mathf.Min(pull - moved, distance));
            if (step <= 0f) yield break;
            Vector3 delta = toPlayer / distance * step;
            moved += locomotion.ApplyExternalDisplacement(delta).magnitude;
            yield return null;
        }
    }

    // R7.3: launch the shard projectiles from the sweep extremities, fanned across the sweep arc.
    // Projectiles route their hits through ArsenalProjectile -> PlayerActor.TryApplyDamage (cascade-bounded).
    private void FireMoonShards(Vector3 center, Vector3 direction, ArsenalCastPlan plan, WeaponScript weapon, ArsenalAbility ability)
    {
        int shards = Mathf.Clamp(plan.ShardCount, 2, 6);
        Vector3 flat = direction; flat.y = 0f;
        Vector3 forward = flat.sqrMagnitude > .001f ? flat.normalized : transform.forward;
        Vector3 rimBase = center; rimBase.y = (transform.position + Vector3.up).y;
        float edge = Mathf.Max(.1f, plan.Width);
        for (int i = 0; i < shards; i++)
        {
            // Spread outward headings evenly across the sweep and originate from the rim in that heading.
            float t = shards == 1 ? 0f : i / (float)(shards - 1) * 2f - 1f; // -1..1 across the arc
            Vector3 aim = Quaternion.AngleAxis(t * 90f, Vector3.up) * forward;
            Vector3 spawn = rimBase + aim * edge;
            ArsenalProjectile.Fire(_player, spawn, aim, weapon.attackDamage,
                plan.Damage, plan.Range, ability.Piercing, ability.AccentColor);
        }
    }

    // R7.5/R7.6: create exactly one short chain thrust toward a different nearby enemy within 6m; nothing otherwise.
    private void TryChainThrust(Vector3 center, Vector3 direction, ArsenalCastPlan plan, WeaponScript weapon,
        ArsenalAbility ability, HitReactionRequest reaction)
    {
        // The primary thrust connects with the enemy straight ahead; the chain must reach a *different* one.
        Actor primary = FindNearbyEnemy(center, 6f, direction, plan.Range);
        Actor target = FindNearbyEnemy(center, 6f, Vector3.zero, 0f, primary);
        if (!target) return; // R7.6: no other enemy in range -> create nothing
        Vector3 toTarget = target.transform.position - center; toTarget.y = 0f;
        if (toTarget.sqrMagnitude < .001f) return;
        Vector3 aim = toTarget.normalized;
        float reach = Mathf.Min(plan.Range, toTarget.magnitude + plan.Width);
        // Route through the shared area-damage path so the chain hit stays inside MaxDepth/MaxSecondaryHits (R7.9).
        _player.TryApplyAreaDamage(center, aim, reach,
            new Vector3(plan.Width, 2f, reach), AreaHitShape.Box, plan.Width,
            Physics.DefaultRaycastLayers, weapon.attackDamage, plan.Damage, 0f, reaction, true, ability.AccentColor);
    }

    // R7.2: repeat the thrust with a spectral copy after the plan delay; the repeat reuses the shared damage path.
    private IEnumerator SpectralThrust(Vector3 center, Vector3 direction, ArsenalCastPlan plan, WeaponScript weapon,
        ArsenalAbility ability, HitReactionRequest reaction, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!_player || _player.IsDead || _player.CurrentWeapon != weapon) yield break;
        Vector3 aim = direction; aim.y = 0f;
        if (aim.sqrMagnitude < .001f) yield break;
        aim.Normalize();
        _player.TryApplyAreaDamage(center, aim, plan.Range,
            new Vector3(plan.Width, 2f, plan.Range), AreaHitShape.Box, plan.Width,
            Physics.DefaultRaycastLayers, weapon.attackDamage, plan.Damage, 0f, reaction, true, ability.AccentColor);
    }

    // Nearest live non-player Actor within radius of origin; used by ChainThrust (R7.5).
    // When forward is non-zero, only enemies within the forward thrust box (half-width plan) count, so the
    // "primary" hit can be identified and excluded from the chain target search (R7.5 "different Enemy").
    private Actor FindNearbyEnemy(Vector3 origin, float radius, Vector3 forward = default, float forwardReach = 0f, Actor exclude = null)
    {
        bool forwardOnly = forward.sqrMagnitude > .001f && forwardReach > 0f;
        Vector3 heading = forwardOnly ? new Vector3(forward.x, 0f, forward.z).normalized : Vector3.zero;
        int count = Physics.OverlapSphereNonAlloc(origin, radius, _overlap, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Actor best = null; float nearest = radius * radius;
        for (int i = 0; i < count; i++)
        {
            Actor candidate = _overlap[i].GetComponentInParent<Actor>();
            if (!candidate || candidate == _player || candidate == exclude || candidate.IsDead || !candidate.isActiveAndEnabled) continue;
            Vector3 delta = candidate.transform.position - origin; delta.y = 0f;
            if (forwardOnly)
            {
                float along = Vector3.Dot(delta, heading);
                if (along <= 0f || along > forwardReach) continue; // behind the thrust or past its reach
            }
            float sqr = delta.sqrMagnitude;
            if (sqr >= nearest) continue;
            best = candidate; nearest = sqr;
        }
        return best;
    }

    // R10.6: recoil the player straight back from the fired direction by up to backstepDistance,
    // clamped to the NavMesh (reusing NavMesh.SamplePosition / agent.Warp) so the Leque Amplo's
    // emergency step never lands the player off the navigable surface. No-op without a live agent.
    private void ApplyBackstep(Vector3 fireDirection, float backstepDistance)
    {
        if (!_agent || !_agent.enabled || !_agent.isOnNavMesh) return;
        Vector3 back = fireDirection; back.y = 0f;
        if (back.sqrMagnitude < .001f) return;
        Vector3 desired = transform.position - back.normalized * backstepDistance;
        if (NavMesh.SamplePosition(desired, out NavMeshHit hit, backstepDistance + 1f, _agent.areaMask))
            _agent.Warp(hit.position);
    }

    // gauntlet-boon-playstyle-overhaul R11 (SpacingRecoil, "Recuo controlado"): step the player straight
    // back (away from the thrust direction) to settle into the ideal spacing band after a thrust connects.
    // The backward step is decided by the pure SpacingBand from the current distance to the thrust target
    // and the SpacingRecoil rank (resolved from WeaponRunModifiers at the call site and passed in), clamped
    // so the player never leaves the band's upper edge (R11.2). The move is routed through the player's own
    // NavMeshAgent (Raycast to clamp to the mesh, then Move) exactly like the Manopla advance, so it respects
    // the navmesh and never crosses scenery (R11.3). It is a no-op without a live agent, without a resolvable
    // target, or when the rank/step collapses to zero. The caller only invokes it on a connecting thrust
    // (primaryHits > 0), so a whiff never steps back (R11.4).
    private void ApplySpacingRecoil(Vector3 center, Vector3 direction, ArsenalCastPlan plan, int rank)
    {
        if (!_agent || !_agent.enabled || !_agent.isOnNavMesh) return;        // never teleport without a live agent
        if (rank <= 0) return;

        // The thrust connects with the enemy straight ahead; measure the current spacing to that target so
        // SpacingBand knows how far to step back. Without a resolvable target there is nothing to space from.
        Actor target = FindNearbyEnemy(center, Mathf.Max(.1f, plan.Range), direction, plan.Range);
        if (!target) return;
        Vector3 toTarget = target.transform.position - transform.position; toTarget.y = 0f;
        float currentDistance = toTarget.magnitude;
        if (currentDistance < .001f) return;

        float step = SpacingBand.StepBack(currentDistance, rank);
        if (step <= 0f) return;

        Vector3 back = direction; back.y = 0f;
        if (back.sqrMagnitude < .001f) return;
        back.Normalize();

        // Clamp the destination to the navmesh (Raycast returns the mesh edge when the straight-line step
        // would leave the navigable area) and move via the agent so the recoil never tunnels through walls.
        Vector3 destination = transform.position - back * step;
        if (_agent.Raycast(destination, out NavMeshHit edge)) destination = edge.position;
        _agent.Move(destination - transform.position);
    }

    public void Cancel()
    {
        if (_cast != null) StopCoroutine(_cast);
        _cast = null;
        Release();
    }

    private void Release()
    {
        if (_animation) _animation.Release();
        if (_locked && _agent && _agent.enabled && _agent.isOnNavMesh)
        {
            // R10.1: a Bow cast only reduced the speed (never stopped the agent), so restore the full
            // move speed captured at cast start; a Manoplas/Lança cast restores the prior stop/rotation.
            if (AllowsMovementWhileFiring && _agentBaseSpeed > 0f) _agent.speed = _agentBaseSpeed;
            _agent.isStopped = _wasStopped;
            _agent.updateRotation = _wasRotating;
        }
        AllowsMovementWhileFiring = false;
        _locked = false;
        IsExecuting = false;
        _activeAbility = null;
        _elapsedForReport = 0f;
    }
    private void OnDeath(Actor actor) => Cancel();
    private void OnDisable() => Cancel();
    private void OnDestroy() { if (_player) _player.Died -= OnDeath; }
}
