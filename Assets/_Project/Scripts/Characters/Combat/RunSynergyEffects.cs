using System.Collections.Generic;
using UnityEngine;

public enum RunSynergy { Conductor, Detonation, Reactor, Resonance }

/// <summary>Player-owned, bounded hit cascades. Secondary hits inherit elements and resolved damage.</summary>
[DisallowMultipleComponent]
public sealed class RunSynergyEffects : MonoBehaviour
{
    private const int MaxSecondaryHits = 32;
    private const int MaxDepth = 4;
    private readonly int[] _ranks = new int[4];
    private readonly Collider[] _colliders = new Collider[128];
    private readonly HashSet<Actor> _visited = new HashSet<Actor>();
    private readonly List<Actor> _nearby = new List<Actor>();
    private readonly Queue<Impact> _pending = new Queue<Impact>();
    private bool _resolving;
    private struct Impact
    {
        public Actor Target;
        public float Damage;
        public int Depth;
    }
    public bool HasModifiers => _ranks[0] + _ranks[1] + _ranks[2] + _ranks[3] > 0;
    public float BurnScaling => .12f * Rank(RunSynergy.Reactor);
    public int LastSecondaryHits { get; private set; }
    public int Rank(RunSynergy kind) => _ranks[(int)kind];
    public void Add(RunSynergy kind) => _ranks[(int)kind]++;
    public void Clear() { System.Array.Clear(_ranks, 0, _ranks.Length); LastSecondaryHits = 0; }

    public void Resolve(Actor initial, float damage, PlayerOnHitEffects elements)
    {
        if (_resolving || !initial || damage <= 0f || !HasModifiers) return;
        _resolving = true;
        _visited.Clear(); _pending.Clear(); LastSecondaryHits = 0;
        _visited.Add(initial);
        _pending.Enqueue(new Impact { Target = initial, Damage = damage });
        try
        {
            while (_pending.Count > 0 && LastSecondaryHits < MaxSecondaryHits)
            {
                Impact impact = _pending.Dequeue();
                if (!impact.Target || impact.Depth >= MaxDepth) continue;
                int conductor = Rank(RunSynergy.Conductor), detonation = Rank(RunSynergy.Detonation);
                bool explode = impact.Target.IsDead && detonation > 0;
                if (!explode && conductor == 0) continue;
                float radius = explode ? 3f + .5f * detonation : 3f + .5f * conductor;
                Vector3 origin = impact.Target.transform.position;
                int count = Physics.OverlapSphereNonAlloc(origin, radius, _colliders,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
                _nearby.Clear();
                for (int i = 0; i < count; i++)
                {
                    Actor candidate = _colliders[i].GetComponentInParent<Actor>();
                    if (!candidate || candidate is PlayerActor || candidate.IsDead || !candidate.isActiveAndEnabled ||
                        _visited.Contains(candidate) || _nearby.Contains(candidate)) continue;
                    Vector3 start = origin + Vector3.up * .5f;
                    Vector3 delta = _colliders[i].bounds.center - start;
                    if (Physics.Raycast(start, delta.normalized, out RaycastHit obstruction, delta.magnitude,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) &&
                        !obstruction.collider.GetComponentInParent<Actor>()) continue;
                    _nearby.Add(candidate);
                }
                _nearby.Sort((a,b) => (a.transform.position-origin).sqrMagnitude.CompareTo((b.transform.position-origin).sqrMagnitude));
                int limit = explode ? _nearby.Count : Mathf.Min(conductor, _nearby.Count);
                for (int i = 0; i < limit && LastSecondaryHits < MaxSecondaryHits; i++)
                {
                    Actor victim = _nearby[i];
                    if (!victim || victim.IsDead || !_visited.Add(victim)) continue;
                    int statuses = (victim.GetComponent<BurnStatus>() ? 1 : 0) + (victim.GetComponent<ChillStatus>() ? 1 : 0);
                    float amplification = 1f + .4f * Rank(RunSynergy.Resonance) * statuses;
                    float amount = impact.Damage * (explode ? .75f + .25f * (detonation - 1) : .45f) * amplification;
                    float before = victim.health;
                    victim.TakeDamage(amount);
                    float dealt = before - victim.health;
                    if (dealt <= 0f) continue;
                    LastSecondaryHits++;
                    elements.ApplyElements(victim, dealt);
                    Vector3 direction = victim.transform.position - origin; direction.y = 0;
                    if (direction.sqrMagnitude > .01f)
                        GauntletImpactVfx.Spawn(origin, direction.normalized, direction.magnitude, explode ? radius : .4f,
                            explode ? new Color(1f,.4f,.1f) : Color.cyan, explode, false, 0);
                    _pending.Enqueue(new Impact { Target = victim, Damage = dealt, Depth = impact.Depth + 1 });
                }
            }
        }
        finally { _pending.Clear(); _visited.Clear(); _resolving = false; }
    }
}
