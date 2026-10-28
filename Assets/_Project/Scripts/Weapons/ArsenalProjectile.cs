using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Swept arrows cannot tunnel through targets or solid scenery.</summary>
public sealed class ArsenalProjectile : MonoBehaviour
{
    private PlayerActor _owner;
    private ProtocolRuntime _protocolRuntime;
    private ProtocolExecution _protocolExecution;
    private BowBoonShot _boonShot;
    private ProtocolProducer _shotProducer;
    private readonly List<Vector3> _boonRoute = new List<Vector3>(128);
    private bool _boonReturning;
    private int _boonReturnIndex;
    private float _damage, _multiplier, _remaining;
    private bool _piercing;
    private readonly HashSet<Actor> _hit = new HashSet<Actor>();
    private Material _material;
    private LineRenderer _line;
    private int _bounces;
    private bool _homing;
    private bool _returning;      // R7.4: flip heading back toward the player once, at max range
    private bool _returned;
    private WeaponScript _weapon;
    private float _nextSeek;
    private Actor _seekTarget;
    private readonly Collider[] _candidates = new Collider[64];

    // R4: homing seek tuning — the launch frame applies zero correction (first seek is one interval out).
    private const float SeekInterval = .1f;   // R4.2 re-seek cadence
    private const float SeekRadius = 7f;       // R4.2 search radius
    private const float SeekConeDeg = 50f;     // R4.2 forward half-angle (via FindNextTarget forwardOnly)
    private const float TurnRateDeg = 180f;    // R4.2 max turn rate

    // R3.1/R3.5/R3.8: arrows are pooled behind the static Fire/FireSingle API so no call-site
    // (ArsenalCombat, SplitArrowCoordinator, ArsenalValidation) changes. The factory builds the
    // GameObject + ArsenalProjectile + LineRenderer + ONE Material once per instance; the Material and
    // LineRenderer are reused across firings instead of being created/destroyed per shot.
    private static ComponentPool<ArsenalProjectile> _pool;

    // R3.8: cache the Shader.Find lookup in a static so the per-instance factory never repeats it.
    private static Shader _unlitShader;

    static ArsenalProjectile()
    {
        // task 2.3: drop the pool at the start of each play so the static state cannot leak pooled
        // instances (referencing destroyed GameObjects) between Editor plays.
        PoolResetRegistry.Register(() => _pool = null);
    }

    private static ComponentPool<ArsenalProjectile> Pool =>
        _pool ??= new ComponentPool<ArsenalProjectile>(CreateArrow, rootName: "Arrow");

    private static ArsenalProjectile CreateArrow()
    {
        var go = new GameObject("Energy arrow");
        var arrow = go.AddComponent<ArsenalProjectile>();

        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 4;
        line.SetPositions(new[] { new Vector3(0, 0, -.65f), Vector3.zero, new Vector3(-.12f, 0, -.2f), Vector3.zero });
        line.startWidth = line.endWidth = .065f;

        if (_unlitShader == null) _unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
        var material = new Material(_unlitShader);
        line.sharedMaterial = material;

        arrow._line = line;
        arrow._material = material;
        return arrow;
    }

    public static void Fire(PlayerActor owner, Vector3 origin, Vector3 direction, float damage,
        float multiplier, float range, bool piercing, Color color, bool returning = false,
        ProtocolProducer producer = ProtocolProducer.Direct, BowBoonShot boonShot = null)
    {
        if (!owner || owner.IsDead || direction.sqrMagnitude < .001f) return;
        owner.TryGetComponent(out ProtocolRuntime protocols);
        using (protocols ? protocols.BeginAreaScope(null) : null)
        {
        boonShot ??= BowBoonShot.Native(owner);
        if (boonShot == null || !boonShot.IgnorePriority) direction = BowBoonCombat.ResolvePriority(owner, origin, direction, range);
        WeaponRunModifiers mods = owner.RunModifiers;
        int twin = mods?.Rank(WeaponBoon.TwinShot) ?? 0;
        int count = 1 + 2 * twin;
        bool concentrated = boonShot != null && boonShot.Concentrated;
        if (concentrated) { multiplier *= count; count = 1; }
        for (int i = 0; i < count; i++)
            FireSingle(owner, origin, Quaternion.AngleAxis((i - (count - 1) * .5f) * 5, Vector3.up) * direction,
                damage, multiplier / (1 + .5f * twin), range, piercing, color, mods, returning,
                producer == ProtocolProducer.Direct && i != count / 2 ? ProtocolProducer.Technique : producer, boonShot);
        }
    }

    private static void FireSingle(PlayerActor owner, Vector3 origin, Vector3 direction, float damage,
        float multiplier, float range, bool piercing, Color color, WeaponRunModifiers mods, bool returning = false,
        ProtocolProducer producer = ProtocolProducer.Direct, BowBoonShot boonShot = null)
    {
        // R3.1: acquire a pooled instance (expands if empty) instead of `new GameObject`; the pooled
        // instance already owns its reused LineRenderer + Material created once by the factory.
        ArsenalProjectile arrow = Pool.Acquire();
        arrow.ResetForReuse(owner, origin, direction, damage, multiplier, range, piercing, color, mods, returning);
        arrow._boonShot = boonShot; arrow._shotProducer = producer;
        arrow._boonRoute.Clear(); arrow._boonRoute.Add(origin); arrow._boonReturning = false;
        arrow._protocolExecution?.Dispose();
        owner.TryGetComponent(out arrow._protocolRuntime);
        arrow._protocolExecution = arrow._protocolRuntime ? arrow._protocolRuntime.ReserveExecution(producer) : null;
    }

    /// <summary>
    /// Returns a pooled arrow to its "just fired" state (task 3.1 hook; full field coverage lives in
    /// task 3.2). Reassigns the per-shot fields that <c>FireSingle</c> used to set inline and tints the
    /// reused Material — never creating a new Material/LineRenderer per shot (R3.8).
    /// </summary>
    private void ResetForReuse(PlayerActor owner, Vector3 origin, Vector3 direction, float damage,
        float multiplier, float range, bool piercing, Color color, WeaponRunModifiers mods, bool returning)
    {
        transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
        _owner = owner;
        _damage = damage;
        _multiplier = multiplier;
        _remaining = range;
        _piercing = piercing || (mods?.Rank(WeaponBoon.Piercing) ?? 0) > 0;
        _bounces = 2 * (mods?.Rank(WeaponBoon.Ricochet) ?? 0);
        _homing = (mods?.Rank(WeaponBoon.Homing) ?? 0) > 0;
        // R4.1: schedule the first seek one interval out so the launch frame applies no homing
        // correction and the initial heading equals the assigned fan heading.
        _nextSeek = Time.time + SeekInterval;
        _returning = returning; // R7.4: ReturnWave flips this arrow back toward the player at max range
        _returned = false;
        _seekTarget = null;
        _hit.Clear();
        _weapon = owner.CurrentWeapon;
        _material.color = color;
    }

    /// <summary>
    /// R3.1/R3.8: terminal path for a spent arrow — deactivate and return the instance to the pool
    /// instead of <c>Destroy(gameObject)</c>. The reused LineRenderer/Material stay with the pooled
    /// instance (no per-shot create/destroy), so <see cref="OnDestroy"/> — which frees the Material —
    /// never runs on this normal release path, only on real play teardown.
    /// </summary>
    private void ReleaseToPool()
    {
        gameObject.SetActive(false);
        Pool.Release(this);
    }
    private void OnDisable() { _protocolExecution?.Dispose(); _protocolExecution = null; _boonShot = null; _boonRoute.Clear(); }

    private void Update()
    {
        if (!_owner || _owner.IsDead || _owner.CurrentWeapon != _weapon || _remaining <= 0f ||
            _protocolRuntime && (_protocolExecution == null || !_protocolExecution.IsLifecycleCurrent || !_protocolExecution.IsValid) ||
            _boonShot != null && !_boonShot.IsCurrent) { ReleaseToPool(); return; }
        if (_boonReturning)
        {
            while (_boonReturnIndex >= 0 && (transform.position - _boonRoute[_boonReturnIndex]).sqrMagnitude < .0025f) _boonReturnIndex--;
            if (_boonReturnIndex < 0) { ReleaseToPool(); return; }
            transform.rotation = Quaternion.LookRotation(_boonRoute[_boonReturnIndex] - transform.position);
        }
        if (_homing && !_boonReturning)
        {
            if (Time.time >= _nextSeek)
            {
                // R4.2/R4.4: seek per-arrow from its own position/forward within the radius + forward cone.
                _nextSeek = Time.time + SeekInterval;
                _seekTarget = FindNextTarget(transform.position, SeekRadius, true);
            }
            // R4.3: with no valid target the block is skipped, so the arrow keeps its current heading.
            if (_seekTarget && !_seekTarget.IsDead && !_hit.Contains(_seekTarget))
            {
                Vector3 aim = _seekTarget.transform.position + Vector3.up - transform.position;
                if (aim.sqrMagnitude > .01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(aim), TurnRateDeg * Time.deltaTime);
            }
        }
        float delta = _owner.IsCombatHitStopped || _owner.TryGetComponent(out AbilityHolder inputHolder) && inputHolder.BlocksWorldInput ? 0 : Time.deltaTime;
        float distance = Mathf.Min(_remaining, 24f * delta);
        if (_boonReturning) distance = Mathf.Min(distance, Vector3.Distance(transform.position, _boonRoute[_boonReturnIndex]));
        // R1.1/R1.4: sweep radius comes from the central config (default 0.35 m — larger than the old
        // 0.12 m) so arrows reliably register grazing hits; missing config falls back to that default.
        float sweepRadius = _boonShot != null && _boonShot.SweepRadius > 0 ? _boonShot.SweepRadius : CombatBalance.Current?.ProjectileSweepRadius ?? 0.35f;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, sweepRadius, transform.forward,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.transform.IsChildOf(_owner.transform)) continue;
            Actor actor = hit.collider.GetComponentInParent<Actor>();
            if (actor)
            {
                if (actor == _owner || actor.IsDead || !_hit.Add(actor)) continue;

                // Mirror frontal shield (R18): a player projectile striking from within the Mirror's
                // frontal arc has at most 25% of its damage applied; hits from outside the arc apply
                // full damage. Only projectiles are reduced here — area attacks flow through
                // TryApplyAreaDamage and always deal full damage (R18.5). The reflector scales the base
                // weapon damage this projectile carries before the normal crit / run-modifier roll, so
                // no shared TakeDamage signature has to change and non-Mirror enemies are unaffected.
                float reflectedDamage = _damage;
                if (actor.TryGetComponent(out FrontalReflector reflector))
                    reflectedDamage = reflector.ResolveIncomingDamage(_damage, transform.position,
                        isPlayerProjectile: true, isAreaAttack: false);

                long generation = actor.CombatGeneration;
                float scale = _boonShot?.DamageScale(actor, _shotProducer) ?? 1f;
                using (_protocolRuntime ? _protocolRuntime.OpenExecutionScope(_protocolExecution) : null)
                {
                    if (_boonReturning)
                    {
                        if (_protocolRuntime) _protocolRuntime.EmitBoonTarget(_protocolExecution, actor, generation,
                            _owner.RollAttackDamage(reflectedDamage, _multiplier * scale).Amount);
                    }
                    else if (_owner.TryApplyDamage(actor, reflectedDamage, _multiplier * scale, 0f))
                    {
                        _boonShot?.Confirm(actor, generation, _shotProducer);
                        if (_owner.TryGetComponent(out AbilityHolder holder)) holder.NotifyAttackHits(_owner, new[] { actor });
                    }
                }

                // R1.1/R1.4: pierce every un-hit enemy on the current straight-line heading first.
                if (_piercing && HasInlineTarget()) continue; // travels straight, no bounce consumed

                // R1.2/R1.4: only ricochet once nothing remains in-line.
                if (_bounces > 0 && !_boonReturning)
                {
                    Actor next = FindNextTarget(hit.point, 6f, false);
                    if (next)
                    {
                        _bounces--; _multiplier *= .75f;
                        Vector3 aim = (next.transform.position + Vector3.up - hit.point).normalized;
                        transform.SetPositionAndRotation(hit.point + aim * .15f, Quaternion.LookRotation(aim));
                        _remaining -= hit.distance;
                        return;
                    }
                }
                if (_piercing) continue; // R1.1 fallback: pierce even when no ricochet target is available
            }
            else if (hit.collider.isTrigger) continue;
            ReleaseToPool();
            return;
        }
        transform.position += transform.forward * distance;
        if (_boonShot != null && _boonShot.CuttingReturn && !_boonReturning && _boonRoute.Count < 128 &&
            (transform.position - _boonRoute[_boonRoute.Count - 1]).sqrMagnitude >= .0625f) _boonRoute.Add(transform.position);
        _remaining -= distance;
        if (_remaining <= 0f)
        {
            if (!_boonReturning && _boonShot != null && _boonShot.CuttingReturn && BeginBoonReturn()) return;
            // R7.4: a ReturnWave flips its heading toward the player exactly once at max range,
            // then travels home; a normal (or already-returned) wave is destroyed.
            if (_returning && !_returned && _owner && !_owner.IsDead)
            {
                _returned = true;
                Vector3 home = _owner.transform.position + Vector3.up - transform.position;
                if (home.sqrMagnitude > .01f)
                {
                    transform.rotation = Quaternion.LookRotation(home);
                    _remaining = home.magnitude + .5f; // reach the player, then expire
                    _hit.Clear();                       // allow the returning pass to hit enemies again
                    return;
                }
            }
            ReleaseToPool();
        }
    }
    private bool BeginBoonReturn()
    {
        if (!_protocolRuntime || _protocolExecution == null || !_protocolExecution.IsValid) return false;
        ProtocolExecution child;
        using (_protocolRuntime.OpenExecutionScope(_protocolExecution)) child = _protocolRuntime.ReserveExecution(ProtocolProducer.Technique);
        if (child == null || !child.IsValid) { child?.Dispose(); return false; }
        _protocolExecution.Dispose(); _protocolExecution = child; _shotProducer = ProtocolProducer.Technique;
        _boonReturning = true; _boonReturnIndex = _boonRoute.Count - 1; _multiplier *= .4f;
        _remaining = 1f;
        Vector3 point = transform.position;
        for (int i = _boonReturnIndex; i >= 0; i--) { _remaining += Vector3.Distance(point, _boonRoute[i]); point = _boonRoute[i]; }
        _hit.Clear(); return true;
    }
    // R1: is there an un-hit live enemy on the current straight-line heading?
    private bool HasInlineTarget()
    {
        // R1.3: pierce uses the same configured sweep radius as the damage sweep for consistency.
        float sweepRadius = CombatBalance.Current?.ProjectileSweepRadius ?? 0.35f;
        RaycastHit[] ahead = Physics.SphereCastAll(transform.position, sweepRadius, transform.forward,
            _remaining, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Array.Sort(ahead, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit h in ahead)
        {
            if (h.collider.transform.IsChildOf(_owner.transform)) continue;
            Actor a = h.collider.GetComponentInParent<Actor>();
            if (a && a != _owner && !a.IsDead && !_hit.Contains(a)) return true;
            if (!a && !h.collider.isTrigger) return false; // solid scenery blocks the line
        }
        return false;
    }

    private Actor FindNextTarget(Vector3 origin, float radius, bool forwardOnly)
    {
        int count = Physics.OverlapSphereNonAlloc(origin, radius, _candidates, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Actor best = null; float nearest = radius * radius;
        for (int i = 0; i < count; i++)
        {
            Actor candidate = _candidates[i].GetComponentInParent<Actor>();
            if (!candidate || candidate is PlayerActor || candidate.IsDead || !candidate.isActiveAndEnabled || _hit.Contains(candidate)) continue;
            Vector3 delta = _candidates[i].bounds.center - origin;
            if (delta.sqrMagnitude >= nearest || (forwardOnly && Vector3.Angle(transform.forward, delta) > SeekConeDeg)) continue;
            best = candidate; nearest = delta.sqrMagnitude;
        }
        return best;
    }
    private void OnDestroy() { if (_material) Destroy(_material); }
}
