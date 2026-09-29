using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Swept arrows cannot tunnel through targets or solid scenery.</summary>
public sealed class ArsenalProjectile : MonoBehaviour
{
    private PlayerActor _owner;
    private float _damage, _multiplier, _remaining;
    private bool _piercing;
    private readonly HashSet<Actor> _hit = new HashSet<Actor>();
    private Material _material;
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

    public static void Fire(PlayerActor owner, Vector3 origin, Vector3 direction, float damage,
        float multiplier, float range, bool piercing, Color color, bool returning = false)
    {
        if (!owner || owner.IsDead || direction.sqrMagnitude < .001f) return;
        WeaponRunModifiers mods = owner.RunModifiers;
        int twin = mods?.Rank(WeaponBoon.TwinShot) ?? 0;
        int count = 1 + 2 * twin;
        for (int i = 0; i < count; i++)
            FireSingle(owner, origin, Quaternion.AngleAxis((i - (count - 1) * .5f) * 5, Vector3.up) * direction,
                damage, multiplier / (1 + .5f * twin), range, piercing, color, mods, returning);
    }

    private static void FireSingle(PlayerActor owner, Vector3 origin, Vector3 direction, float damage,
        float multiplier, float range, bool piercing, Color color, WeaponRunModifiers mods, bool returning = false)
    {
        var go = new GameObject("Energy arrow");
        go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
        var arrow = go.AddComponent<ArsenalProjectile>();
        arrow._owner = owner;
        arrow._damage = damage;
        arrow._multiplier = multiplier;
        arrow._remaining = range;
        arrow._piercing = piercing || (mods?.Rank(WeaponBoon.Piercing) ?? 0) > 0;
        arrow._bounces = 2 * (mods?.Rank(WeaponBoon.Ricochet) ?? 0);
        arrow._homing = (mods?.Rank(WeaponBoon.Homing) ?? 0) > 0;
        // R4.1: schedule the first seek one interval out so the launch frame applies no homing
        // correction and the initial heading equals the assigned fan heading.
        arrow._nextSeek = Time.time + SeekInterval;
        arrow._returning = returning; // R7.4: ReturnWave flips this arrow back toward the player at max range
        arrow._weapon = owner.CurrentWeapon;
        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.positionCount = 4;
        line.SetPositions(new[] { new Vector3(0,0,-.65f), Vector3.zero, new Vector3(-.12f,0,-.2f), Vector3.zero });
        line.startWidth = line.endWidth = .065f;
        arrow._material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        arrow._material.color = color;
        line.sharedMaterial = arrow._material;
    }

    private void Update()
    {
        if (!_owner || _owner.IsDead || _owner.CurrentWeapon != _weapon || _remaining <= 0f) { Destroy(gameObject); return; }
        if (_homing)
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
        float distance = Mathf.Min(_remaining, 24f * Time.deltaTime);
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, .12f, transform.forward,
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

                if (_owner.TryApplyDamage(actor, reflectedDamage, _multiplier, 0f) &&
                    _owner.TryGetComponent(out AbilityHolder holder))
                    holder.NotifyAttackHits(_owner, new[] { actor });

                // R1.1/R1.4: pierce every un-hit enemy on the current straight-line heading first.
                if (_piercing && HasInlineTarget()) continue; // travels straight, no bounce consumed

                // R1.2/R1.4: only ricochet once nothing remains in-line.
                if (_bounces > 0)
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
            Destroy(gameObject);
            return;
        }
        transform.position += transform.forward * distance;
        _remaining -= distance;
        if (_remaining <= 0f)
        {
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
            Destroy(gameObject);
        }
    }
    // R1: is there an un-hit live enemy on the current straight-line heading?
    private bool HasInlineTarget()
    {
        RaycastHit[] ahead = Physics.SphereCastAll(transform.position, .12f, transform.forward,
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
