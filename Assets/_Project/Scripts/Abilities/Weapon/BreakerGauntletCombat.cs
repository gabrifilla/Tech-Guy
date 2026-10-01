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
    private NavMeshAgent _agent;
    private WeaponScript _weapon;
    private Coroutine _cast;
    private BreakerGauntletAbility _activeAbility;
    private System.Collections.Generic.IReadOnlyList<AreaHitStep> _activeSteps;
    private float _elapsed;
    private bool _finisherApplied;
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
    public bool CanDodgeCancel => IsExecuting && _activeAbility && _activeAbility.AsuraBurst && _elapsed >= .25f;
    public bool IsAsuraActive => IsExecuting && _activeAbility && _activeAbility.AsuraBurst;
    public float ExecutionDuration { get; private set; }
    public int Energy => _momentum.Energy;
    public bool IsReady => _momentum.IsReady;
    public bool IsEquipped => _weapon && _player && !_player.IsDead && _player.CurrentWeapon == _weapon;

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _holder = GetComponent<AbilityHolder>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
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

        try
        {
            float elapsed = 0f;
            float duration = Mathf.Max(ability.activeTime, ability.AdvanceDuration);
            foreach (AreaHitStep step in steps)
                if (step != null) duration = Mathf.Max(duration, step.delay + 0.18f);
            ExecutionDuration = duration;
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
                while (animatedStep+1 < steps.Count && elapsed > steps[animatedStep].delay + Mathf.Min(.04f,(steps[animatedStep+1].delay-steps[animatedStep].delay)*.25f)) animatedStep++;
                float start = animatedStep == 0 ? 0 : steps[animatedStep-1].delay + Mathf.Min(.04f,(steps[animatedStep].delay-steps[animatedStep-1].delay)*.25f);
                float end = animatedStep+1 < steps.Count ? steps[animatedStep].delay + Mathf.Min(.04f,(steps[animatedStep+1].delay-steps[animatedStep].delay)*.25f) : duration;
                _animation.Strike(Motion(ability,animatedStep),elapsed,start,steps[animatedStep].delay,end);
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

    private static SkillMotion Motion(BreakerGauntletAbility ability,int index)
    {
        if (ability.ShockSkill && index >= ability.HitSteps.Count-1) return SkillMotion.Slam;
        return index%2==0 ? SkillMotion.PunchLeft : SkillMotion.PunchRight;
    }
    private void Impact(BreakerGauntletAbility ability,AreaHitStep step,int index)
    {
        bool finisher=index>=ability.HitSteps.Count-1;
        if (finisher) _finisherApplied=true;
        _animation.Contact(Motion(ability,index));
        SequencedAreaAttackAbility.ApplyHitStep(transform,_player,_weapon,step,true,ability.EffectColor);
        bool radial=step.hitShape==AreaHitShape.Sphere;
        Vector3 origin=transform.TransformPoint(step.localOffset)+(radial ? Vector3.up*(step.sphereRadius-1f) : Vector3.zero);
        GauntletImpactVfx.Spawn(origin,transform.forward,
            step.hitShape==AreaHitShape.Sphere ? step.sphereRadius*2 : step.rangeOverride,
            radial ? step.sphereRadius*2 : step.boxSize.x,ability.EffectColor,ability.ShockSkill && finisher,ability.AsuraBurst && finisher,index%2==0 ? -1 : 1,radial);
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

    public void FinishForDodge()
    {
        if (!CanDodgeCancel) return;
        // Cash out the main finisher once; optional echoes are traded for an immediate escape.
        if (!_finisherApplied && _activeSteps!=null)
        {
            int index=_activeAbility.HitSteps.Count-1;
            Impact(_activeAbility,_activeSteps[index],index);
        }
        CancelCast();
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
