using System.Collections.Generic;
using UnityEngine;

public enum RunSynergy { Conductor, Detonation, Reactor, Resonance }

/// <summary>How a detonating enemy reacts to the elements it carried at death (R3).</summary>
internal enum DetonationKind { None, Combustion, Shatter }

/// <summary>Player-owned, bounded hit cascades. Secondary hits inherit elements and resolved damage.</summary>
[DisallowMultipleComponent]
public sealed class RunSynergyEffects : MonoBehaviour
{
    private const int MaxSecondaryHits = 32;
    private const int MaxDepth = 4;
    // R3.2: a shatter fragments a frozen death into between 3 and 6 shards.
    private const int ShatterMinFragments = 3;
    private const int ShatterMaxFragments = 6;
    private static readonly Color ShatterColor = new Color(.6f, .85f, 1f);
    private readonly int[] _ranks = new int[4];
    private readonly Collider[] _colliders = new Collider[128];
    private readonly HashSet<Actor> _visited = new HashSet<Actor>();
    private readonly List<Actor> _nearby = new List<Actor>();
    private readonly Queue<Impact> _pending = new Queue<Impact>();
    private bool _resolving;

    // Per-run combat-event bus (R8): owned and cleared by RunBoons, which lives on this same
    // GameObject (PlayerOnHitEffects.Synergies adds RunSynergyEffects to the player object that
    // also carries RunBoons). Reached through RunBoons ownership via TryGetComponent — the same
    // pattern PlayerActor and PlayerOnHitEffects use — so no FindObjectOfType/GameObject.Find is
    // needed. Cached lazily; null until a run's RunBoons has created its bus, and every raise-site
    // null-guards it.
    private RunBoons _runBoons;
    private HookBus Hooks
    {
        get
        {
            if (!_runBoons) TryGetComponent(out _runBoons);
            return _runBoons ? _runBoons.Hooks : null;
        }
    }
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

    /// <summary>
    /// Routes a single bounded impact (e.g. Thermal Shock) through the same Resonance amplification
    /// math used by <see cref="Resolve"/> (<c>1 + 0.4·Resonance·statusCount</c>) without starting a new
    /// cascade. The impact is emitted as one secondary hit so it cannot exceed <see cref="MaxSecondaryHits"/>.
    /// No-op when Resonance is inactive, the target is invalid, damage is non-positive, or a cascade is
    /// already resolving.
    /// </summary>
    public void ReportImpact(Actor target, float damage, PlayerOnHitEffects elements)
    {
        if (_resolving || !target || target.IsDead || damage <= 0f || Rank(RunSynergy.Resonance) == 0) return;
        if (LastSecondaryHits >= MaxSecondaryHits) return;
        int statuses = (target.GetComponent<BurnStatus>() ? 1 : 0) + (target.GetComponent<ChillStatus>() ? 1 : 0);
        float amplification = 1f + .4f * Rank(RunSynergy.Resonance) * statuses;
        float amount = damage * amplification;
        float before = target.health;
        target.TakeDamage(amount);
        float dealt = before - target.health;
        if (dealt <= 0f) return;
        LastSecondaryHits++;
        if (elements)
            elements.ApplyElements(target, dealt);
    }

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
                // R3: a detonating enemy reacts to the elements it carried when it died.
                // Shatter (frost, including the burn+frost case) takes precedence over combustion
                // (burn only). Classification only alters explosion radius / fragment presentation;
                // the bounded loop, MaxDepth guard and one-hit-per-target rule below are untouched.
                DetonationKind detonationKind = DetonationKind.None;
                if (explode)
                {
                    bool burning = impact.Target.GetComponent<BurnStatus>();
                    bool frozen = impact.Target.GetComponent<ChillStatus>();
                    if (frozen)
                    {
                        detonationKind = DetonationKind.Shatter;
                        ShatterVfx(origin, radius);
                    }
                    else if (burning)
                    {
                        detonationKind = DetonationKind.Combustion;
                        radius *= 1.3f;
                    }
                    // R8.8: a detonation explosion occurred; surface its origin through the per-run
                    // HookBus so subscribers can react. Null-guarded (bus absent outside a run).
                    Hooks?.RaiseExplosion(origin);
                }
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
                // R3.2: a shatter emits between 3 and 6 fragments, so it fans out to at most 6
                // nearby enemies (and at least attempts 3) rather than the whole overlap set.
                if (detonationKind == DetonationKind.Shatter)
                    limit = Mathf.Min(limit, ShatterMaxFragments);
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
                        GauntletImpactVfx.Spawn(origin, direction.normalized, direction.magnitude,
                            detonationKind == DetonationKind.Shatter ? .5f : explode ? radius : .4f,
                            detonationKind == DetonationKind.Shatter ? ShatterColor :
                                explode ? new Color(1f,.4f,.1f) : Color.cyan,
                            explode, detonationKind == DetonationKind.Shatter, 0);
                    _pending.Enqueue(new Impact { Target = victim, Damage = dealt, Depth = impact.Depth + 1 });
                }
            }
        }
        finally { _pending.Clear(); _visited.Clear(); _resolving = false; }
    }

    /// <summary>
    /// Presents the distinct shatter burst for a frozen detonation (R3.2). Emits between
    /// <see cref="ShatterMinFragments"/> and <see cref="ShatterMaxFragments"/> outward fragment
    /// strokes in a cold shard color so the shatter reads differently from a combustion explosion.
    /// Purely cosmetic: fragment damage is applied through the bounded secondary-hit loop.
    /// </summary>
    private static void ShatterVfx(Vector3 origin, float radius)
    {
        int fragments = Random.Range(ShatterMinFragments, ShatterMaxFragments + 1);
        for (int i = 0; i < fragments; i++)
        {
            float angle = (i + Random.value) * (Mathf.PI * 2f / fragments);
            Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            GauntletImpactVfx.Spawn(origin, direction, radius, .5f, ShatterColor, true, true, 0);
        }
    }
}
