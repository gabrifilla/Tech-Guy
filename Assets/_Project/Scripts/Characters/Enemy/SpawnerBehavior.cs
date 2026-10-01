using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawner archetype role behavior (R16). On a spawn-interval timer, while alive and below its
/// configured living cap, it NavMesh-samples a point within a configured radius (the existing
/// Spawn_Point instantiation path shared with <see cref="EnemyRespawnPoint"/>) and instantiates a
/// Swarm enemy there. It tracks the number of concurrently living produced Swarms by subscribing to
/// each spawned <see cref="Actor"/>'s <see cref="Actor.Died"/> event, decrementing on death, and
/// never exceeding the cap. It stops producing on its own death and does not resume while dead. A
/// NavMesh sampling miss skips the attempt, retains the count, and retries next interval (R16.9).
///
/// The concurrency arithmetic (cap, increment, decrement, "may I spawn?") lives in the pure
/// <see cref="SpawnerLivingCount"/> model so it can be property-tested without a live scene
/// (task 8.7); this MonoBehaviour stays thin and owns only the Unity concerns (timer coroutine,
/// NavMesh sampling, instantiation, event subscriptions).
///
/// The Spawner declares Combat_Role Priority-Threat and is a priority target (R16.1); the
/// persistent marker itself is added by <see cref="EnemyVariant"/> / <see cref="PriorityTargetMarker"/>
/// in task 11.1 — this behavior only produces Swarms.
/// </summary>
/// <remarks>
/// Feature: enemy-swarm-core-archetypes, task 8.6.
/// Requirements: 16.1, 16.2, 16.3, 16.4, 16.5, 16.6, 16.8, 16.9.
/// </remarks>
[AddComponentMenu("Enemy/Archetypes/Spawner Behavior")]
public sealed class SpawnerBehavior : ArchetypeBehavior
{
    [Header("Swarm production")]
    [Tooltip("Prefab of the Swarm enemy to produce. Must carry an Actor. Injected in the Inspector; never resolved by search.")]
    [SerializeField] private GameObject _swarmPrefab;

    [Tooltip("Seconds between spawn attempts. Must be positive (R16.2).")]
    [SerializeField] private float _spawnInterval = 3f;

    [Tooltip("Maximum concurrently living produced Swarms, clamped to 1..100 (R16.3).")]
    [SerializeField] private int _livingCap = 10;

    [Tooltip("Radius around the Spawner within which a NavMesh spawn position is sampled (R16.8).")]
    [SerializeField] private float _spawnRadius = 3f;

    [Tooltip("Optional parent for spawned Swarms. Leave empty to spawn at the scene root.")]
    [SerializeField] private Transform _spawnedParent;

    private SpawnerLivingCount _count;
    private Coroutine _spawnLoop;
    public event System.Action<Actor> Produced;

    /// <summary>The Combat_Role the Spawner declares (R16.1). Priority-target status derives from this role.</summary>
    public CombatRole CombatRole => CombatRole.PriorityThreat;

    /// <summary>Metres trimmed off each arena half-extent when clamping a spawn candidate, keeping produced Swarms clear of the walls (mirrors RoomCompositionResolver's bounds inset).</summary>
    private const float ArenaBoundsInset = 1.5f;

    private Vector3 _arenaCenter;
    private Vector2 _arenaSize;
    private bool _hasArenaBounds;

    /// <summary>Whether the Spawner is a priority target (R16.1). Always true for this role.</summary>
    public bool IsPriorityTarget => true;

    /// <summary>Current count of concurrently living produced Swarms (test/inspection hook).</summary>
    public int LivingCount => _count.Living;

    /// <summary>The effective living cap after clamping (test/inspection hook).</summary>
    public int LivingCap => _count.Cap;

    protected override void Awake()
    {
        base.Awake();
        _count = new SpawnerLivingCount(_livingCap);
    }

    private void OnEnable()
    {
        // Stop producing on own death and never resume while dead (R16.6). The timer coroutine
        // itself also re-checks CanAct each interval, so death is caught within one interval; the
        // Died subscription guarantees the loop is torn down promptly (well within 1s for any
        // sane interval).
        if (Owner) Owner.Died += OnOwnerDied;
        _spawnLoop = StartCoroutine(SpawnLoop());
    }

    private void OnDisable()
    {
        if (Owner) Owner.Died -= OnOwnerDied;
        StopSpawnLoop();
    }

    /// <summary>
    /// Interval-driven production loop (a coroutine timer, not heavy per-frame Update). Each tick it
    /// waits the configured interval, then — while it may act and its living count is below the cap —
    /// attempts a single NavMesh-sampled spawn (R16.2, R16.4, R16.8, R16.9).
    /// </summary>
    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.01f, _spawnInterval));

            if (!CanAct) continue;      // dead / control-locked / off-mesh: skip, retry next interval
            if (!_count.CanSpawn) continue; // at cap: no spawn until a death frees a slot (R16.4)

            TrySpawnOne();
        }
    }

    /// <summary>
    /// Attempts one spawn: samples a NavMesh position within the radius; on success instantiates the
    /// Swarm, records the spawn, and subscribes to its death; on a sampling miss skips and retains the
    /// count so the next interval retries (R16.9).
    /// </summary>
    private void TrySpawnOne()
    {
        if (!_swarmPrefab) return;

        if (!TrySampleSpawnPosition(out Vector3 position))
        {
            // NavMesh sampling failed within the radius: skip this attempt, keep the count (R16.9).
            return;
        }

        GameObject instance = Instantiate(_swarmPrefab, position, transform.rotation,
            _spawnedParent ? _spawnedParent : transform.parent);
        if (instance.TryGetComponent(out EnemyAI childAI)) childAI.player = GetComponent<EnemyAI>().player;

        if (instance.TryGetComponent(out NavMeshAgent spawnedAgent) && spawnedAgent.enabled)
        {
            spawnedAgent.Warp(position);
        }

        Actor spawnedActor = instance.GetComponent<Actor>();
        if (!spawnedActor) spawnedActor = instance.GetComponentInChildren<Actor>();
        if (!spawnedActor)
        {
            // No Actor means we cannot track its death (R16.5); do not count an untrackable spawn.
            Debug.LogWarning($"{nameof(SpawnerBehavior)} spawned a Swarm without an Actor; it will not be tracked.", instance);
            return;
        }

        _count.RecordSpawn();
        spawnedActor.Died += OnProducedSwarmDied;
        Produced?.Invoke(spawnedActor);
    }

    /// <summary>
    /// NavMesh sampling within the configured radius, mirroring <see cref="EnemyRespawnPoint"/>'s
    /// behavior (R16.8): a random offset on the Spawner's plane, snapped onto the mesh. Returns false
    /// when no valid position is found (R16.9).
    /// </summary>
    private bool TrySampleSpawnPosition(out Vector3 position)
    {
        Vector3 candidate = transform.position + Random.insideUnitSphere * Mathf.Max(0f, _spawnRadius);
        candidate.y = transform.position.y;

        // Confine the candidate to the sealed arena before sampling so a Spawner standing near a wall or
        // doorway cannot fling produced Swarms out of the room (into a corridor / past a sealed door).
        // The arena bounds are injected by the director that owns the encounter; when they are absent
        // (e.g. a non-arena spawner) sampling is unconstrained, preserving the previous behavior.
        if (_hasArenaBounds)
        {
            float halfX = Mathf.Max(0.5f, _arenaSize.x * 0.5f - ArenaBoundsInset);
            float halfZ = Mathf.Max(0.5f, _arenaSize.y * 0.5f - ArenaBoundsInset);
            candidate.x = Mathf.Clamp(candidate.x, _arenaCenter.x - halfX, _arenaCenter.x + halfX);
            candidate.z = Mathf.Clamp(candidate.z, _arenaCenter.z - halfZ, _arenaCenter.z + halfZ);
        }

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, Mathf.Max(0.1f, _spawnRadius), NavMesh.AllAreas))
        {
            position = hit.position;
            return true;
        }

        position = candidate;
        return false;
    }

    /// <summary>
    /// Constrains produced Swarms to the sealed arena the Spawner fights in (R16.8). The owning director
    /// injects the room's world-space center and XZ size on activation; <see cref="TrySampleSpawnPosition"/>
    /// then clamps each sampled point to these (inset) bounds so a Swarm never spawns outside the arena —
    /// e.g. in a doorway or corridor — even when the Spawner stands near an edge. Mirrors the bounds-clamp
    /// in <c>RoomCompositionResolver.TrySampleNavMeshPoint</c>.
    /// </summary>
    public void ConfigureArenaBounds(Vector3 center, Vector2 size)
    {
        _arenaCenter = center;
        _arenaSize = size;
        _hasArenaBounds = size.x > 0f && size.y > 0f;
    }

    /// <summary>Decrements the living count when a produced Swarm dies, exactly once per death (R16.5).</summary>
    private void OnProducedSwarmDied(Actor swarm)
    {
        if (swarm) swarm.Died -= OnProducedSwarmDied;
        _count.RecordDeath();
    }

    /// <summary>Stops production the moment the Spawner dies and keeps it stopped while dead (R16.6).</summary>
    private void OnOwnerDied(Actor owner)
    {
        StopSpawnLoop();
    }

    private void StopSpawnLoop()
    {
        if (_spawnLoop != null)
        {
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }
    }

    /// <summary>
    /// Names any missing dependency so a misconfigured Spawner prefab is diagnosed at spawn (R1.6).
    /// The base already validates <see cref="Actor"/> / <see cref="EnemyAI"/> / agent; here we add the
    /// Swarm prefab this behavior needs to produce anything.
    /// </summary>
    protected override void LogMissingDependencies()
    {
        if (!_swarmPrefab)
        {
            Debug.LogError($"{nameof(SpawnerBehavior)} on '{name}' is missing a Swarm prefab reference; it will not spawn.", this);
        }
    }

    private void OnValidate()
    {
        // Keep the interval positive (R16.2) and the cap inside 1..100 (R16.3), matching the model's
        // clamp so the Inspector reflects the effective values.
        _spawnInterval = Mathf.Max(0.01f, _spawnInterval);
        _livingCap = Mathf.Clamp(_livingCap, SpawnerLivingCount.MinCap, SpawnerLivingCount.MaxCap);
        _spawnRadius = Mathf.Max(0f, _spawnRadius);
    }
}
