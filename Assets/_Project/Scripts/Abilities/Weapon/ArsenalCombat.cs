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

    private IEnumerator Execute(ArsenalAbility ability)
    {
        IsExecuting = true;
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
                    int primaryHits = 0;
                    for (int branch = 0; branch < plan.Directions; branch++)
                    {
                        Vector3 aim = Quaternion.AngleAxis((branch - (plan.Directions - 1) * .5f) * 25f, Vector3.up) * direction;
                        primaryHits += _player.TryApplyAreaDamage(center, aim, radial ? .01f : plan.Range,
                            new Vector3(plan.Width, 2f, plan.Range), radial ? AreaHitShape.Sphere : AreaHitShape.Box,
                            plan.Width, Physics.DefaultRaycastLayers, weapon.attackDamage, plan.Damage, 0f, reaction, true, ability.AccentColor);

                    }

                    // R7.4/R9.4: the orbital sweep (W) nudges hit enemies along the sweep direction by a
                    // LIMITED amount (<=1.5 m/s, <=0.75 m total), routed through each enemy's existing
                    // locomotion means — never a hard knockback.
                    if (radial && ability.Kind == ArsenalSkillKind.Sweep && primaryHits > 0)
                        ApplySweepDisplacement(center, direction, plan);

                    if (plan.WaveMultiplier > 0)
                        // R7.4: ReturnWave flips the wave back toward the player at max range.
                        ArsenalProjectile.Fire(_player, origin + Vector3.up, direction, weapon.attackDamage,
                            plan.Damage * plan.WaveMultiplier, plan.Range * 2, true, ability.AccentColor, plan.ReturnWave);

                    // R7.3: MoonShard launches ShardCount (2-6) projectiles from the sweep extremities.
                    if (radial && ability.Kind == ArsenalSkillKind.Sweep && plan.ShardCount > 0)
                        FireMoonShards(center, direction, plan, weapon, ability);

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
    }
    private void OnDeath(Actor actor) => Cancel();
    private void OnDisable() => Cancel();
    private void OnDestroy() { if (_player) _player.Died -= OnDeath; }
}
