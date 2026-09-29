using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Orchestration bridge between a pure <see cref="RoomComposition"/> (data) and the
/// existing <c>EnemyRespawnPoint</c> spawn path (scene). For each enemy of a
/// Combat_Room's composition it samples a reachable NavMesh position inside the
/// room bounds (up to 20 attempts, mirroring the FirstSectorDirector
/// <c>FindReachableSpot</c> pattern), spawns the enemy through an
/// <c>EnemyRespawnPoint</c> and applies the assigned <see cref="EnemyArchetype"/>
/// via <c>EnemyVariant.Configure</c>.
/// <para>
/// This is the scene/NavMesh-touching orchestration layer (not the pure generation
/// core), so it is a plain C# service the <c>ProgressionDirector</c> drives. It
/// resolves everything through the references passed in — no
/// <c>GameObject.Find</c> / <c>FindObjectOfType</c> / magic strings (AGENTS.md).
/// </para>
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 5.4, 5.6, 5.7, 5.8, 5.9, 7.1.
/// Task 7.1 implemented the happy path (NavMesh sampling, spawning, archetype
/// application). Task 7.2 adds the discard/short-count/empty warnings (R5.7, R5.8,
/// R5.9) and Boss_Room marking (R7.1): the warnings are both logged via
/// <c>Debug.LogWarning</c> (naming the affected room) and recorded on
/// <see cref="RoomActivationResult.Warnings"/> so tests can observe either channel.
/// </remarks>
public sealed class RoomCompositionResolver
{
    /// <summary>Maximum NavMesh sampling attempts per enemy (R5.6).</summary>
    private const int NavMeshSampleAttempts = 20;

    /// <summary>NavMesh sample radius around each candidate point, in meters.</summary>
    private const float NavMeshSampleRadius = 2f;

    /// <summary>
    /// Minimum enemies expected by the Density_Budget (R5.2/R5.8). When fewer enemies
    /// than this actually land in a Combat_Room, a short-count warning is raised.
    /// </summary>
    private const int DensityBudgetMinimum = 4;

    /// <summary>
    /// Bounds inset (meters) applied to each side of the room so sampled points do
    /// not land inside a wall or past a sealed door, matching the FirstSectorDirector
    /// margin.
    /// </summary>
    private const float BoundsInset = 1.5f;

    /// <summary>
    /// Activates a Combat_Room's composition: for every enemy in every
    /// <see cref="RoomComposition.Slots"/> slot, samples a reachable NavMesh point
    /// inside <paramref name="room"/>'s bounds, spawns the enemy through one of the
    /// supplied <paramref name="spawnPoints"/> and applies the archetype selected
    /// from <paramref name="archetypeCatalog"/> by the slot's <c>ArchetypeId</c>.
    /// <para>
    /// Task 7.2 layers the composition warnings on top of this: enemies with no
    /// reachable NavMesh point after 20 attempts are discarded and summarized in a
    /// single per-room warning (R5.7); a short-count warning is raised when fewer
    /// than the budget minimum (4) actually land (R5.8); an empty-composition warning
    /// is raised (and nothing spawns) when there is no valid <c>EnemyRespawnPoint</c>
    /// (R5.9). When <paramref name="room"/> is the Boss_Room and a
    /// <paramref name="player"/> is supplied, the first spawned actor is marked with
    /// <c>SectorBoss</c> and configured against the player (R7.1).
    /// </para>
    /// </summary>
    /// <param name="room">
    /// The Combat_Room to populate. Its <see cref="Room.Center"/>/<see cref="Room.Size"/>
    /// define the XZ bounds sampled against, and its <see cref="Room.Composition"/>
    /// drives which archetypes and how many enemies to spawn.
    /// </param>
    /// <param name="spawnPoints">
    /// The room's <c>EnemyRespawnPoint</c>s. Each enemy reuses one of these points
    /// (round-robin) as the spawn mechanism; the point is repositioned to the sampled
    /// NavMesh spot before spawning, since <c>EnemyRespawnPoint</c> spawns at its own
    /// transform.
    /// </param>
    /// <param name="archetypeCatalog">
    /// The catalog of the 16 <see cref="EnemyArchetype"/>s; the archetype whose
    /// <c>Id</c> matches a slot's <c>ArchetypeId</c> is applied to that enemy.
    /// </param>
    /// <param name="spawnParent">
    /// Optional parent for spawned enemies. Passed through to callers that want the
    /// enemies grouped under a room container; may be null.
    /// </param>
    /// <param name="player">
    /// Optional player reference. When supplied and <paramref name="room"/> is a
    /// Boss_Room, the first spawned actor is marked as the <c>SectorBoss</c> and
    /// configured against this player (R7.1); otherwise ignored.
    /// </param>
    /// <returns>
    /// A <see cref="RoomActivationResult"/> with the spawned actors, the requested/
    /// placed/discarded counts, any composition warnings raised, and whether a boss
    /// was marked.
    /// </returns>
    public RoomActivationResult Activate(
        Room room,
        IReadOnlyList<EnemyRespawnPoint> spawnPoints,
        IReadOnlyList<EnemyArchetype> archetypeCatalog,
        Transform spawnParent = null,
        PlayerActor player = null)
    {
        var spawned = new List<Actor>();
        var warnings = new List<string>();

        RoomComposition composition = room != null ? room.Composition : null;
        int requested = CountRequestedEnemies(composition);

        // R5.9: no valid EnemyRespawnPoint (null/empty list or all-null entries) means
        // the composition stays empty — spawn nothing and warn, naming the room.
        if (!HasValidSpawnPoint(spawnPoints))
        {
            string emptyWarning =
                $"[RoomCompositionResolver] {DescribeRoom(room)} não possui nenhum EnemyRespawnPoint válido; "
                + "composição deixada sem inimigos (R5.9).";
            Debug.LogWarning(emptyWarning);
            warnings.Add(emptyWarning);
            return new RoomActivationResult(spawned, requested, 0, requested, warnings, false);
        }

        if (composition == null || requested == 0)
        {
            return new RoomActivationResult(spawned, requested, 0, requested, warnings, false);
        }

        int spawnPointCursor = 0;
        int navMeshDiscards = 0;

        foreach (ArchetypeSlot slot in composition.Slots)
        {
            EnemyArchetype archetype = ResolveArchetype(archetypeCatalog, slot.ArchetypeId);

            for (int i = 0; i < slot.Count; i++)
            {
                EnemyRespawnPoint spawnPoint = NextSpawnPoint(spawnPoints, ref spawnPointCursor);
                if (spawnPoint == null)
                {
                    continue;
                }

                if (!TrySampleNavMeshPoint(room, out Vector3 position))
                {
                    // R5.7: no NavMesh point after 20 attempts — discard this enemy and
                    // accumulate the count for a single per-room summary warning below.
                    navMeshDiscards++;
                    continue;
                }

                Actor actor = SpawnAt(spawnPoint, position);
                if (actor == null)
                {
                    // Spawn failed (missing prefab / no Actor); treated as a discard.
                    continue;
                }

                ApplyArchetype(actor, archetype);
                spawned.Add(actor);
            }
        }

        int placed = spawned.Count;
        int discarded = requested - placed;

        // R5.7: one warning per room summarizing how many enemies were dropped for
        // lack of a reachable NavMesh point (not one per enemy).
        if (navMeshDiscards > 0)
        {
            string discardWarning =
                $"[RoomCompositionResolver] {DescribeRoom(room)}: {navMeshDiscards} inimigo(s) descartado(s) "
                + $"por não obter ponto válido de NavMesh após {NavMeshSampleAttempts} tentativas (R5.7).";
            Debug.LogWarning(discardWarning);
            warnings.Add(discardWarning);
        }

        // R5.8: fewer enemies placed than the Density_Budget minimum (4) — warn with
        // the target and the effectively placed count.
        if (placed < DensityBudgetMinimum)
        {
            int target = composition != null ? composition.TargetDensity : requested;
            string shortWarning =
                $"[RoomCompositionResolver] {DescribeRoom(room)}: apenas {placed} inimigo(s) posicionado(s), "
                + $"abaixo do mínimo do Density_Budget ({DensityBudgetMinimum}); alvo={target} (R5.8).";
            Debug.LogWarning(shortWarning);
            warnings.Add(shortWarning);
        }

        // R7.1: mark the primary enemy of the Boss_Room as the SectorBoss.
        bool bossMarked = TryMarkBoss(room, player, spawned);

        return new RoomActivationResult(spawned, requested, placed, discarded, warnings, bossMarked);
    }

    /// <summary>
    /// Marks the primary enemy of a Boss_Room by adding/obtaining a <c>SectorBoss</c>
    /// component and configuring it against <paramref name="player"/> (R7.1),
    /// mirroring the FirstSectorDirector guardian setup. This can also be called
    /// directly by the <c>ProgressionDirector</c> when it materializes the boss.
    /// </summary>
    /// <param name="primaryEnemy">The enemy to promote to the sector boss.</param>
    /// <param name="player">The player the boss should target; must be non-null.</param>
    /// <returns><c>true</c> when the boss was marked; otherwise <c>false</c>.</returns>
    public bool MarkBoss(Actor primaryEnemy, PlayerActor player)
    {
        if (primaryEnemy == null || player == null)
        {
            return false;
        }

        SectorBoss boss = primaryEnemy.GetComponent<SectorBoss>()
            ?? primaryEnemy.gameObject.AddComponent<SectorBoss>();
        boss.Configure(player);
        return true;
    }

    /// <summary>
    /// Marks the first spawned actor as the sector boss when <paramref name="room"/>
    /// is a Boss_Room, a <paramref name="player"/> is supplied and at least one enemy
    /// was spawned (R7.1). No-op otherwise.
    /// </summary>
    private bool TryMarkBoss(Room room, PlayerActor player, List<Actor> spawned)
    {
        if (room == null || room.Type != RoomType.Boss || player == null || spawned.Count == 0)
        {
            return false;
        }

        return MarkBoss(spawned[0], player);
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="spawnPoints"/> contains at least one
    /// non-null <c>EnemyRespawnPoint</c> (R5.9 valid-spawn-point check).
    /// </summary>
    private static bool HasValidSpawnPoint(IReadOnlyList<EnemyRespawnPoint> spawnPoints)
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            return false;
        }

        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds a human-readable identity for a room used in warning messages, naming
    /// its <see cref="Room.Id"/> and <see cref="Room.Type"/> (R5.7/R5.8/R5.9).
    /// </summary>
    private static string DescribeRoom(Room room)
    {
        return room == null ? "Room <null>" : $"Room #{room.Id} ({room.Type})";
    }

    /// <summary>Sums the enemy counts across all slots of a composition.</summary>
    private static int CountRequestedEnemies(RoomComposition composition)
    {
        if (composition == null)
        {
            return 0;
        }

        int total = 0;
        foreach (ArchetypeSlot slot in composition.Slots)
        {
            if (slot.Count > 0)
            {
                total += slot.Count;
            }
        }

        return total;
    }

    /// <summary>
    /// Selects the <see cref="EnemyArchetype"/> whose <c>Id</c> matches
    /// <paramref name="archetypeId"/> from the catalog, or null when the catalog is
    /// empty or has no matching entry (the enemy then spawns with its prefab default).
    /// </summary>
    private static EnemyArchetype ResolveArchetype(
        IReadOnlyList<EnemyArchetype> archetypeCatalog,
        ArchetypeId archetypeId)
    {
        if (archetypeCatalog == null)
        {
            return null;
        }

        for (int i = 0; i < archetypeCatalog.Count; i++)
        {
            EnemyArchetype candidate = archetypeCatalog[i];
            if (candidate != null && candidate.Id == archetypeId)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns the next non-null spawn point in round-robin order, advancing
    /// <paramref name="cursor"/>. Returns null only when every entry is null.
    /// </summary>
    private static EnemyRespawnPoint NextSpawnPoint(
        IReadOnlyList<EnemyRespawnPoint> spawnPoints,
        ref int cursor)
    {
        for (int scanned = 0; scanned < spawnPoints.Count; scanned++)
        {
            EnemyRespawnPoint candidate = spawnPoints[cursor % spawnPoints.Count];
            cursor++;
            if (candidate != null)
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    /// Samples up to 20 candidate positions inside <paramref name="room"/>'s XZ bounds
    /// and returns the first that lands on a valid NavMesh point, preferring the
    /// sampled hit. Mirrors the FirstSectorDirector <c>FindReachableSpot</c> pattern:
    /// candidates are clamped to the (inset) room bounds so they cannot fall inside a
    /// wall, and each is snapped to the NavMesh via <c>SamplePosition</c> (R5.6).
    /// </summary>
    private static bool TrySampleNavMeshPoint(Room room, out Vector3 position)
    {
        Vector3 center = new Vector3(room.Center.x, 0f, room.Center.y);
        float halfX = Mathf.Max(0.5f, room.Size.x * 0.5f - BoundsInset);
        float halfZ = Mathf.Max(0.5f, room.Size.y * 0.5f - BoundsInset);

        for (int attempt = 0; attempt < NavMeshSampleAttempts; attempt++)
        {
            float offsetX = Random.Range(-halfX, halfX);
            float offsetZ = Random.Range(-halfZ, halfZ);
            Vector3 candidate = new Vector3(center.x + offsetX, center.y, center.z + offsetZ);

            // Keep the candidate inside the room bounds before sampling.
            candidate.x = Mathf.Clamp(candidate.x, center.x - halfX, center.x + halfX);
            candidate.z = Mathf.Clamp(candidate.z, center.z - halfZ, center.z + halfZ);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, NavMeshSampleRadius, NavMesh.AllAreas))
            {
                position = hit.position;
                return true;
            }
        }

        position = center;
        return false;
    }

    /// <summary>
    /// Repositions the spawn point to <paramref name="position"/> and spawns one enemy
    /// through it, reusing the existing <c>EnemyRespawnPoint.SpawnEnemy</c> path (which
    /// spawns at its own transform, so it is moved to the sampled NavMesh spot first).
    /// </summary>
    private static Actor SpawnAt(EnemyRespawnPoint spawnPoint, Vector3 position)
    {
        spawnPoint.transform.position = position;
        return spawnPoint.SpawnEnemy();
    }

    /// <summary>
    /// Applies <paramref name="archetype"/> to the spawned enemy through its
    /// <c>EnemyVariant</c> (added when missing), which adopts the archetype's
    /// <c>EnemyProfile</c> and role. No-op when no matching archetype was resolved.
    /// </summary>
    private static void ApplyArchetype(Actor actor, EnemyArchetype archetype)
    {
        if (actor == null || archetype == null)
        {
            return;
        }

        EnemyVariant variant = actor.GetComponent<EnemyVariant>();
        if (variant == null)
        {
            variant = actor.GetComponentInChildren<EnemyVariant>();
        }

        if (variant == null)
        {
            variant = actor.gameObject.AddComponent<EnemyVariant>();
        }

        variant.Configure(archetype);
    }
}

/// <summary>
/// Outcome of activating a Combat_Room's composition: the enemies actually spawned,
/// the requested / placed / discarded counts, any composition warnings raised
/// (R5.7, R5.8, R5.9) and whether the Boss_Room's primary enemy was marked (R7.1).
/// </summary>
/// <remarks>Feature: procedural-stage-room-generation. Requirements: 5.4, 5.6, 5.7, 5.8, 5.9, 7.1.</remarks>
public sealed class RoomActivationResult
{
    private static readonly IReadOnlyList<string> NoWarnings = new List<string>();

    private readonly List<Actor> _spawnedActors;
    private readonly IReadOnlyList<string> _warnings;

    /// <summary>
    /// Creates an activation result carrying only counts (task 7.1 shape); records no
    /// warnings and no boss marking.
    /// </summary>
    /// <param name="spawnedActors">The actors spawned into the room (may be empty).</param>
    /// <param name="requestedCount">Total enemies the composition asked for.</param>
    /// <param name="placedCount">Enemies successfully spawned and placed.</param>
    /// <param name="discardedCount">Enemies dropped (no NavMesh spot or spawn failure).</param>
    public RoomActivationResult(
        List<Actor> spawnedActors,
        int requestedCount,
        int placedCount,
        int discardedCount)
        : this(spawnedActors, requestedCount, placedCount, discardedCount, null, false)
    {
    }

    /// <summary>
    /// Creates an activation result including the composition warnings raised
    /// (R5.7/R5.8/R5.9) and whether the boss was marked (R7.1).
    /// </summary>
    /// <param name="spawnedActors">The actors spawned into the room (may be empty).</param>
    /// <param name="requestedCount">Total enemies the composition asked for.</param>
    /// <param name="placedCount">Enemies successfully spawned and placed.</param>
    /// <param name="discardedCount">Enemies dropped (no NavMesh spot or spawn failure).</param>
    /// <param name="warnings">Warnings raised during activation; null becomes empty.</param>
    /// <param name="bossMarked">Whether a <c>SectorBoss</c> was marked on a spawned actor.</param>
    public RoomActivationResult(
        List<Actor> spawnedActors,
        int requestedCount,
        int placedCount,
        int discardedCount,
        List<string> warnings,
        bool bossMarked)
    {
        _spawnedActors = spawnedActors ?? new List<Actor>();
        _warnings = warnings != null ? warnings.AsReadOnly() : NoWarnings;
        RequestedCount = requestedCount;
        PlacedCount = placedCount;
        DiscardedCount = discardedCount;
        BossMarked = bossMarked;
    }

    /// <summary>The actors spawned into the room by this activation.</summary>
    public IReadOnlyList<Actor> SpawnedActors => _spawnedActors;

    /// <summary>Total enemies the composition requested across all slots.</summary>
    public int RequestedCount { get; }

    /// <summary>Enemies successfully spawned and placed on the NavMesh.</summary>
    public int PlacedCount { get; }

    /// <summary>Enemies discarded (no reachable NavMesh point after 20 attempts, or spawn failure).</summary>
    public int DiscardedCount { get; }

    /// <summary>
    /// Warning messages raised during activation (R5.7 discards, R5.8 short-count,
    /// R5.9 empty composition). Empty when activation was fully successful.
    /// </summary>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>Whether the Boss_Room's primary enemy was marked with <c>SectorBoss</c> (R7.1).</summary>
    public bool BossMarked { get; }
}
