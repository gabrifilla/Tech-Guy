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
    private WeaponScript _weapon;
    private float _nextSeek;
    private Actor _seekTarget;
    private readonly Collider[] _candidates = new Collider[64];

    public static void Fire(PlayerActor owner, Vector3 origin, Vector3 direction, float damage,
        float multiplier, float range, bool piercing, Color color)
    {
        if (!owner || owner.IsDead || direction.sqrMagnitude < .001f) return;
        WeaponRunModifiers mods = owner.RunModifiers;
        int twin = mods?.Rank(WeaponBoon.TwinShot) ?? 0;
        int count = 1 + 2 * twin;
        for (int i = 0; i < count; i++)
            FireSingle(owner, origin, Quaternion.AngleAxis((i - (count - 1) * .5f) * 5, Vector3.up) * direction,
                damage, multiplier / (1 + .5f * twin), range, piercing, color, mods);
    }

    private static void FireSingle(PlayerActor owner, Vector3 origin, Vector3 direction, float damage,
        float multiplier, float range, bool piercing, Color color, WeaponRunModifiers mods)
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
                _nextSeek = Time.time + .1f;
                _seekTarget = FindNextTarget(transform.position, 7f, true);
            }
            if (_seekTarget && !_seekTarget.IsDead && !_hit.Contains(_seekTarget))
            {
                Vector3 aim = _seekTarget.transform.position + Vector3.up - transform.position;
                if (aim.sqrMagnitude > .01f)
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(aim), 180 * Time.deltaTime);
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
                if (_owner.TryApplyDamage(actor, _damage, _multiplier, 0f) &&
                    _owner.TryGetComponent(out AbilityHolder holder))
                    holder.NotifyAttackHits(_owner, new[] { actor });
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
                if (_piercing) continue;
            }
            else if (hit.collider.isTrigger) continue;
            Destroy(gameObject);
            return;
        }
        transform.position += transform.forward * distance;
        _remaining -= distance;
        if (_remaining <= 0f) Destroy(gameObject);
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
            if (delta.sqrMagnitude >= nearest || (forwardOnly && Vector3.Angle(transform.forward, delta) > 50)) continue;
            best = candidate; nearest = delta.sqrMagnitude;
        }
        return best;
    }
    private void OnDestroy() { if (_material) Destroy(_material); }
}
