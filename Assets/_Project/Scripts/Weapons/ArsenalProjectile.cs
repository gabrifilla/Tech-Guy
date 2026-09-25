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

    public static void Fire(PlayerActor owner, Vector3 origin, Vector3 direction, float damage,
        float multiplier, float range, bool piercing, Color color)
    {
        var go = new GameObject("Energy arrow");
        go.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
        var arrow = go.AddComponent<ArsenalProjectile>();
        arrow._owner = owner;
        arrow._damage = damage;
        arrow._multiplier = multiplier;
        arrow._remaining = range;
        arrow._piercing = piercing;
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
        if (!_owner || _owner.IsDead) { Destroy(gameObject); return; }
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
    private void OnDestroy() { if (_material) Destroy(_material); }
}
