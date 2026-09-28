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
    private readonly Collider[] _overlap = new Collider[64]; // R7.5 ChainThrust enemy search buffer
    public bool IsExecuting { get; private set; }
    public float ExecutionDuration { get; private set; }

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _agent = GetComponent<NavMeshAgent>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
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
        _locked = _agent && _agent.enabled && _agent.isOnNavMesh;
        if (_locked)
        {
            _wasStopped = _agent.isStopped;
            _wasRotating = _agent.updateRotation;
            _agent.ResetPath();
            _agent.isStopped = true;
            _agent.updateRotation = false;
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
                    // Q sweeps stagger and chip stance; breaking a weak mob's stance briefly stuns it.
                    var reaction = new HitReactionRequest(_player, center, direction, HitReactionType.Stagger,
                        HitStrength.Medium, 40f, StanceBreakEffect.Stun, 0.5f, 1f);
                    int primaryHits = 0;
                    for (int branch = 0; branch < plan.Directions; branch++)
                    {
                        Vector3 aim = Quaternion.AngleAxis((branch - (plan.Directions - 1) * .5f) * 25f, Vector3.up) * direction;
                        primaryHits += _player.TryApplyAreaDamage(center, aim, radial ? .01f : plan.Range,
                            new Vector3(plan.Width, 2f, plan.Range), radial ? AreaHitShape.Sphere : AreaHitShape.Box,
                            plan.Width, Physics.DefaultRaycastLayers, weapon.attackDamage, plan.Damage, 0f, reaction, true, ability.AccentColor);

                    }
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
            _agent.isStopped = _wasStopped;
            _agent.updateRotation = _wasRotating;
        }
        _locked = false;
        IsExecuting = false;
    }
    private void OnDeath(Actor actor) => Cancel();
    private void OnDisable() => Cancel();
    private void OnDestroy() { if (_player) _player.Died -= OnDeath; }
}
