using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-owned orchestrator that drives a procedurally generated Run. It generalizes the
/// authored, linear <c>FirstSectorDirector</c> into a graph/seed-driven director: it
/// acquires the Run_Seed, delegates generation to the pure <see cref="StageGenerator"/>,
/// materializes the resulting <see cref="RoomGraph"/> in the scene, activates rooms,
/// seals/opens gates, drops reward trophies, gates on the boss, and handles Stage
/// transitions and the return to the Nexus. All dependencies are supplied through
/// serialized references (no <c>GameObject.Find</c> / <c>FindObjectOfType</c> / magic
/// strings in gameplay logic, per AGENTS.md and R9.4).
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 1.8, 9.1, 9.2, 9.3, 9.4, 9.5.
/// <para>
/// This is the SKELETON for the director (task 8.1): serialized dependencies, dependency
/// validation, and read-only run state. Seed acquisition (8.2), graph materialization
/// (8.3), combat/seal/clear/reward/boss/death flows (8.4-8.9) are later tasks and are
/// marked with <c>TODO(8.x)</c> stubs below.
/// </para>
/// <para>
/// Dependency validation (R9.1-R9.3): during <c>Awake</c> and <c>OnValidate</c> the
/// director builds a list of the missing required dependencies and logs EXACTLY ONE error
/// naming each one. In <c>Awake</c>, if any required dependency is missing it disables
/// itself (<c>enabled = false</c>) and starts no generation, leaving run state unchanged.
/// The <see cref="_seedSource"/> is intentionally NOT a required dependency: an absent or
/// invalid seed source is handled by the fallback seed policy (R2.2, task 8.2), so it must
/// not block <c>Awake</c>.
/// </para>
/// </remarks>
public sealed class ProgressionDirector : MonoBehaviour
{
    [Header("Run dependencies")]
    [SerializeField] private PlayerActor _player;
    [SerializeField] private StageGenerationParams _generationParams;
    [SerializeField] private RunSeedSource _seedSource;
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private EnemyArchetype[] _archetypeCatalog;
    [SerializeField] private EnemyVariant[] _archetypePrefabs;
    [SerializeField] private string _returnScene = "NexusLobby";
    [SerializeField] private TMP_Text _objective;
    [SerializeField] private ProceduralStageEnvironment _environment;
    [Header("Boss access")]
    [SerializeField, Min(1)] private int _requiredBossAccessFragments = 3;
    public RoomGraph CurrentGraph => _graph;
    public int BossAccessFragments => _bossAccessFragments;
    public int RequiredBossAccessFragments => Mathf.Max(1, _requiredBossAccessFragments);
    public bool HasBossAccess => _bossAccessFragments >= RequiredBossAccessFragments;

    /// <summary>
    /// Total number of Stages this Run is configured to play before it concludes and returns to
    /// the Nexus (R7.6). There is no authored per-Run Stage list in the pure model, so this
    /// serialized count defines what "no next Stage" means: when
    /// <see cref="CurrentStageIndex"/> + 1 reaches <see cref="_stageCount"/>, defeating the boss
    /// concludes the Run instead of generating another Stage. Defaults to 1 (single Stage per Run).
    /// </summary>
    [SerializeField, Min(1)] private int _stageCount = 1;

    /// <summary>Pure generation core; the director feeds it params + seed and materializes
    /// the returned graph (task 8.3). Held as a field so it is reused across Stages.</summary>
    private readonly StageGenerator _generator = new StageGenerator();

    /// <summary>Scene/NavMesh spawn bridge used to activate each Combat_Room's composition
    /// (task 8.4). Held as a field so it is reused across rooms.</summary>
    private readonly RoomCompositionResolver _compositionResolver = new RoomCompositionResolver();

    /// <summary>The 64-bit Run_Seed actually used for this Run's generation, persisted so it can
    /// be reused to reproduce the layout (R2.6). Set by <see cref="AcquireRunSeed"/> (task 8.2).</summary>
    private ulong _activeRunSeed;

    /// <summary>The materialized layout for the current Stage. Null until a successful
    /// generation, and left null on generation failure so no partial Stage is built
    /// (R1.7/R7.2). Set by <see cref="GenerateAndMaterializeStage"/> (task 8.3).</summary>
    private RoomGraph _graph;

    /// <summary>True once a Stage has been generated and materialized successfully. Stays false
    /// on generation failure so downstream tasks (8.4-8.9) never run against a partial or absent
    /// Stage (R1.7/R7.2).</summary>
    private bool _stageBuilt;

    /// <summary>Materialized Room_Gates keyed by the <see cref="RoomConnection"/> they realize —
    /// exactly one <see cref="EncounterGates"/> instance (one door pair edge) per connection
    /// (R1.5). Consumed by later tasks to open/seal a specific connection.</summary>
    private readonly Dictionary<RoomConnection, EncounterGates> _gatesByConnection = new();

    /// <summary>Per-room index of the doors that belong to each room: for every room id, the list
    /// of (door direction, gates instance) that this room owns. Lets tasks 8.4 (seal) and 8.6
    /// (open) operate per room and per direction without re-deriving edges.</summary>
    private readonly Dictionary<int, List<(Direction direction, EncounterGates gates)>> _gatesByRoom = new();

    /// <summary>Rooms of the current graph indexed by <see cref="Room.Id"/> for O(1) lookups by
    /// the materialization and later run-loop tasks.</summary>
    private readonly Dictionary<int, Room> _roomsById = new();

    /// <summary>Distance below which the player is considered to have entered a room's combat, matching
    /// the FirstSectorDirector proximity threshold.</summary>
    private const float RoomEntryDistance = 7f;

    /// <summary>Reward_Trophy positioning attempts before falling back to the nearest reachable point (R6.2/R6.3).</summary>
    private const int TrophyPlacementAttempts = 5;

    /// <summary>Player-owned boon system reused for the per-room reward selection (R6.4/R6.7). Resolved in
    /// <see cref="Awake"/> from the player, mirroring the FirstSectorDirector.</summary>
    private RunBoons _boons;

    /// <summary>Count of enemies still alive per active Combat_Room, keyed by room id. A room reaching zero
    /// triggers the clear/reward flow (R6.1). Rooms are added on activation (8.4) and removed on clear (8.5).</summary>
    private readonly Dictionary<int, int> _aliveByRoom = new();

    /// <summary>Maps each spawned enemy to the room id it belongs to, so its <c>Died</c> event decrements the
    /// right room's alive count regardless of how many rooms are active concurrently.</summary>
    private readonly Dictionary<Actor, int> _roomByActor = new();
    private readonly Dictionary<SpawnerBehavior, Action<Actor>> _spawnHandlers = new();

    /// <summary>Runtime spawn points created per activated room, kept so they can be destroyed with the Stage.</summary>
    private readonly List<EnemyRespawnPoint> _spawnPoints = new();

    /// <summary>The room id whose reward is currently being awaited/selected, or -1 when none. While set, that
    /// room's exit doors stay sealed until a boon is chosen (R6.5/R6.6).</summary>
    private int _rewardRoomId = -1;

    /// <summary>The live Reward_Trophy awaiting a claim, or null. Removed on claim (R6.4).</summary>
    private RewardTrophy _trophy;

    /// <summary>Delay before the boss-defeat Stage transition fires, keeping the release within the
    /// R7.4 "≤1s after the defeat is recorded" budget while letting death effects settle.</summary>
    private const float BossTransitionDelay = 0.75f;

    /// <summary>Delay before the death return begins, keeping the Nexus load start within the R8.2
    /// "≤1s after the Run ends" budget.</summary>
    private const float DeathReturnDelay = 0.5f;

    /// <summary>Enemies still alive in the Boss_Room, or -1 while no Boss_Room is active. The Boss_Room
    /// reaching zero is treated as the boss defeat that releases the Stage transition (R7.3/R7.4).</summary>
    private int _bossRoomId = -1;
    private int _bossAccessFragments;

    /// <summary>True once the boss has been defeated and the Stage transition released (R7.4). Guards the
    /// transition so it is never released while the boss is alive (R7.3) and never fires twice.</summary>
    private bool _bossDefeated;

    /// <summary>Guards the one-way Stage transition / Nexus return so a boss defeat or player death can
    /// only start a single transition, mirroring the FirstSectorDirector <c>_leaving</c> flag.</summary>
    private bool _leaving;

    /// <summary>Zero-based index of the Stage currently being played (R9.5).</summary>
    public int CurrentStageIndex { get; private set; }

    /// <summary>Number of Combat_Rooms cleared so far in the current Run (R9.5).</summary>
    public int ClearedRooms { get; private set; }

    /// <summary>True once the Run has concluded (boss cleared with no next Stage, or player
    /// death) (R9.5).</summary>
    public bool IsRunComplete { get; private set; }

    /// <summary>The 64-bit Run_Seed used to generate the current Stage, exposed for reproducibility
    /// and diagnostics (R2.6). Valid once <see cref="Awake"/> has acquired a seed.</summary>
    public ulong ActiveRunSeed => _activeRunSeed;

    /// <summary>True once the current Stage has been generated and materialized in the scene.
    /// Remains false when generation aborted so no partial Stage is present (R1.7/R7.2).</summary>
    public bool IsStageBuilt => _stageBuilt;

    /// <summary>
    /// Validates required dependencies (R9.1). If any is missing, disables the component so
    /// no progression runs and leaves run state unchanged (R9.2/R9.3), then defers the
    /// actual run start to later tasks.
    /// </summary>
    private void Awake()
    {
        if (!ValidateDependencies())
        {
            // A required dependency is missing: log has already named each one. Disable and
            // start nothing, leaving CurrentStageIndex/ClearedRooms/IsRunComplete untouched
            // (R9.2/R9.3).
            enabled = false;
            return;
        }

        // Acquire the seed (8.2), then generate + materialize the Stage (8.3). On generation
        // failure GenerateAndMaterializeStage aborts without building any partial rooms/gates
        // (R1.7/R7.2) and leaves _stageBuilt false, so the later run-loop tasks stay dormant.
        GenerateAndMaterializeStage();

        if (_stageBuilt)
        {
            // The combat run-loop (8.4/8.5) is proximity-driven, not Update-driven (AGENTS.md):
            // a light coroutine polls the player's position and reacts to room entry and clears.
            // Started only after a successful materialization so it never runs against a partial
            // or absent Stage (R1.7/R7.2).
            _boons = _player.GetComponent<RunBoons>() ?? _player.gameObject.AddComponent<RunBoons>();
            _boons.RewardChosen += OnRewardChosen;

            // End the Run and return to the Nexus when the player dies (task 8.9, R8.1-R8.5). Subscribed
            // only after a successful materialization so the death handler never runs against a partial
            // Stage. The Treasure_Room reward (8.7), Secret_Room reveal (8.7) and boss-driven Stage
            // transition (8.8) are all driven from the combat loop / room-clear path below.
            _player.Died += OnPlayerDied;

            StartCoroutine(RunCombatLoop());
        }
    }

    /// <summary>
    /// Acquires the 64-bit Run_Seed for this Run (R2.1). When the configured
    /// <see cref="_seedSource"/> is absent or yields a value that is not a valid 64-bit seed, it
    /// logs an error naming the acquisition failure and derives a fresh 64-bit fallback seed
    /// (R2.2). The chosen seed is persisted in <see cref="_activeRunSeed"/> and logged so the
    /// same Run can be reproduced by re-authoring it in the seed source (R2.6).
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 2.1, 2.2, 2.6.</remarks>
    /// <returns>The 64-bit Run_Seed to feed the <see cref="StageGenerator"/>.</returns>
    private ulong AcquireRunSeed()
    {
        ulong seed;
        if (_seedSource != null && _seedSource.TryGetSeed(out ulong authoredSeed))
        {
            seed = authoredSeed;
        }
        else
        {
            Debug.LogError(
                "ProgressionDirector could not acquire a valid 64-bit Run_Seed from the configured " +
                "RunSeedSource (missing or invalid); generating a fallback seed.",
                this);
            seed = GenerateFallbackSeed();
        }

        // Persist and log the seed actually used so the Run can be reproduced (R2.6).
        _activeRunSeed = seed;
        Debug.Log($"ProgressionDirector Run_Seed = {seed} (reuse this value in RunSeedSource to reproduce this Run).", this);
        return seed;
    }

    /// <summary>
    /// Builds a full-width 64-bit fallback Run_Seed from two independent entropy sources — the
    /// current UTC tick count and a fresh <see cref="Guid"/> hash — so both halves of the seed
    /// are populated and successive fallbacks differ (R2.2).
    /// </summary>
    private static ulong GenerateFallbackSeed()
    {
        ulong high = (uint)Guid.NewGuid().GetHashCode();
        ulong low = (ulong)DateTime.UtcNow.Ticks;
        return (high << 32) ^ low;
    }

    /// <summary>
    /// Runs the full seed → generate → materialize pipeline for the current Stage (tasks 8.2/8.3).
    /// Acquires the Run_Seed, delegates to the pure <see cref="StageGenerator"/> and, on success,
    /// materializes one Room_Gates pair per <see cref="RoomConnection"/> (R1.5), positions the
    /// player in the Start_Room and opens the Start_Room's outgoing doors. On generation failure
    /// it logs the reason and aborts WITHOUT instantiating any partial rooms or gates, leaving
    /// <see cref="_stageBuilt"/> false (R1.7/R7.2).
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 1.5, 1.7, 7.2.</remarks>
    private void GenerateAndMaterializeStage()
    {
        ulong seed = AcquireRunSeed();
        StageGenerationResult result = _generator.Generate(_generationParams, seed);

        if (!result.Success || result.Graph == null)
        {
            // Abort with no partial Stage: instantiate nothing (R1.7/R7.2).
            string reason = string.IsNullOrEmpty(result.FailureReason) ? "unknown reason" : result.FailureReason;
            Debug.LogError(
                $"ProgressionDirector aborted Stage generation (seed {seed}): {reason}. No rooms were instantiated.",
                this);
            _stageBuilt = false;
            return;
        }

        // A successful result may still carry a non-fatal diagnostic (e.g. a discarded Treasure_Room, R4.7).
        if (!string.IsNullOrEmpty(result.FailureReason))
        {
            Debug.LogWarning($"ProgressionDirector Stage generated with a diagnostic (seed {seed}): {result.FailureReason}.", this);
        }

        _graph = result.Graph;
        IndexRooms(_graph);
        if (_environment) _environment.Build(_graph);
        MaterializeGates(_graph);
        PositionPlayerAtStart(_graph);
        OpenStartRoomDoors(_graph);
        _stageBuilt = true;
        UpdateExplorationObjective();
    }

    /// <summary>Rebuilds the <see cref="_roomsById"/> lookup for the given graph.</summary>
    private void IndexRooms(RoomGraph graph)
    {
        _roomsById.Clear();
        foreach (Room room in graph.Rooms)
        {
            _roomsById[room.Id] = room;
        }
    }

    /// <summary>
    /// Materializes exactly one <see cref="EncounterGates"/> instance per
    /// <see cref="RoomConnection"/> (R1.5). Each gate GameObject is parented to this director,
    /// configured around room A's center/size and given a single directional door on the
    /// connection's <see cref="RoomConnection.SideFromA"/> (which starts sealed). The gate is
    /// registered both by connection and by each endpoint room + door direction so later tasks
    /// can seal (8.4) and open (8.6) per room and per direction. Hidden Secret_Room connections
    /// stay sealed and are handled by the reveal flow (task 8.7).
    /// </summary>
    private void MaterializeGates(RoomGraph graph)
    {
        _gatesByConnection.Clear();
        _gatesByRoom.Clear();

        foreach (RoomConnection connection in graph.Connections)
        {
            if (!_roomsById.TryGetValue(connection.RoomAId, out Room roomA))
            {
                continue;
            }

            var gateObject = new GameObject($"Gate {connection.RoomAId}->{connection.RoomBId} ({connection.SideFromA})");
            gateObject.transform.SetParent(transform, false);
            var gates = gateObject.AddComponent<EncounterGates>();
            gates.ConfigureDirectional(ToWorld(roomA.Center), roomA.Size,
                _environment ? ProceduralStageEnvironment.DoorWidth : 0f);

            Direction sideFromA = connection.SideFromA;
            gates.AddDoor(sideFromA); // Starts sealed per EncounterGates' directional contract.
            if (IsBossConnection(connection))
            {
                gates.ConfigureBossSeal(sideFromA, RequiredBossAccessFragments);
                gates.SetBossSealProgress(_bossAccessFragments);
            }

            _gatesByConnection[connection] = gates;
            RegisterRoomGate(connection.RoomAId, sideFromA, gates);
            // Register the mirrored side against room B so it can also seal/open this shared door.
            RegisterRoomGate(connection.RoomBId, sideFromA, gates);
        }
    }

    /// <summary>Adds a (direction, gates) entry to a room's owned-door list.</summary>
    private void RegisterRoomGate(int roomId, Direction direction, EncounterGates gates)
    {
        if (!_gatesByRoom.TryGetValue(roomId, out List<(Direction, EncounterGates)> doors))
        {
            doors = new List<(Direction, EncounterGates)>();
            _gatesByRoom[roomId] = doors;
        }

        doors.Add((direction, gates));
    }

    /// <summary>
    /// Places the player at the Start_Room center. Prefers the player's <see cref="NavMeshAgent"/>
    /// via <c>Warp</c> so the move flows through navigation rather than a raw transform write when
    /// an agent is present and on the mesh; otherwise falls back to a direct transform position.
    /// </summary>
    private void PositionPlayerAtStart(RoomGraph graph)
    {
        if (!_roomsById.TryGetValue(graph.StartRoomId, out Room startRoom))
        {
            return;
        }

        Vector3 target = ToWorld(startRoom.Center);
        var agent = _player.GetComponent<NavMeshAgent>();
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(target);
        }
        else
        {
            _player.transform.position = target;
        }
    }

    /// <summary>
    /// Opens the Start_Room's outgoing doors so the player can leave the Start_Room immediately,
    /// while every other connection's door remains sealed until its room is entered/cleared
    /// (tasks 8.4/8.6). Hidden (Secret) connections are left sealed here (task 8.7 reveals them).
    /// </summary>
    private void OpenStartRoomDoors(RoomGraph graph)
    {
        foreach (RoomConnection connection in graph.Connections)
        {
            if (connection.Hidden)
            {
                continue;
            }

            if (!_gatesByConnection.TryGetValue(connection, out EncounterGates gates) || !gates)
            {
                continue;
            }

            if (connection.RoomAId == graph.StartRoomId)
            {
                gates.OpenDoor(connection.SideFromA);
                connection.Open = true;
            }
            else if (connection.RoomBId == graph.StartRoomId)
            {
                gates.OpenDoor(connection.SideFromA);
                connection.Open = true;
            }
        }
    }

    /// <summary>
    /// Proximity-driven combat run-loop (tasks 8.4/8.5). Polls the player position on a light cadence
    /// (0.15s, mirroring FirstSectorDirector) rather than in <c>Update</c> (AGENTS.md). On each tick,
    /// when the player nears an unvisited/uncleared Combat_Room center it enters that room: seals its
    /// doors (R3.4) and activates its composition (R3.5/R5.4). The loop pauses while a reward selection
    /// is pending so a boon menu is never interrupted (R6.5/R6.6). It exits once the Run ends or the
    /// player dies, so no combat logic runs against a dead player or a completed Run.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 3.4, 3.5, 5.4.</remarks>
    private IEnumerator RunCombatLoop()
    {
        var interval = new WaitForSeconds(.15f);
        while (!IsRunComplete && _player && !_player.IsDead)
        {
            // Do not start a new room while the player is claiming/choosing a reward (R6.5/R6.6).
            if (_rewardRoomId < 0 && !(_boons && _boons.IsChoosing))
            {
                TryEnterNearbyRoom();
            }

            yield return interval;
        }
    }

    /// <summary>
    /// Enters the first not-yet-started room whose center the player is within
    /// <see cref="RoomEntryDistance"/> of. Entry is idempotent: a room already visited/cleared or already
    /// tracked as active is skipped, so re-entering a resolved room never re-activates it (supports R3.3).
    /// Combat_Rooms and Boss_Rooms are sealed and have their composition activated (R3.4/R3.5/R5.4);
    /// Treasure_Rooms grant a no-combat reward on entry (R4.11, task 8.7); Secret_Rooms are only entered
    /// once revealed by the Discovery_Action (task 8.7). Start_Rooms are never "entered" as encounters.
    /// </summary>
    private void TryEnterNearbyRoom()
    {
        Vector3 playerPosition = _player.transform.position;
        foreach (Room room in _graph.Rooms)
        {
            if (room.Visited || room.Cleared || _aliveByRoom.ContainsKey(room.Id))
            {
                continue;
            }

            // A generated-but-unrevealed Secret_Room stays inert until the Discovery_Action reveals it
            // and opens its hidden connection (R4.8/R4.9); it is never auto-entered by proximity.
            if (room.Type == RoomType.Secret && !room.Revealed)
            {
                continue;
            }

            if (room.Type == RoomType.Start)
            {
                continue;
            }

            Vector3 offset = playerPosition - ToWorld(room.Center);
            if (Mathf.Abs(offset.x) > room.Size.x * .5f - 1.5f ||
                Mathf.Abs(offset.z) > room.Size.y * .5f - 1.5f)
            {
                continue;
            }

            if (room.Type == RoomType.Treasure || room.Type == RoomType.Secret)
            {
                EnterTreasureRoom(room);
            }
            else
            {
                // Combat_Room and Boss_Room both spawn a composition and seal until cleared.
                EnterCombatRoom(room);
            }

            return;
        }
    }

    /// <summary>
    /// Enters a Treasure_Room (task 8.7, R4.11): grants its reward through the existing
    /// Reward_Trophy / Run_Boons flow WITHOUT requiring any combat. The room is marked visited and
    /// cleared, then a single Reward_Trophy is dropped on a reachable NavMesh spot exactly as a cleared
    /// Combat_Room would (R6.2/R6.3); claiming it opens the boon selection (R6.4) and choosing a boon
    /// opens the eligible exits (R6.7). No enemies are spawned and no doors are sealed for combat, so the
    /// player can collect the reward and leave freely.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 4.11.</remarks>
    private void EnterTreasureRoom(Room room)
    {
        room.Visited = true;

        // Reuse the exact clear/reward path: mark cleared, count it, hold this room's exits until the
        // reward is claimed and a boon is chosen, and drop the trophy on a reachable NavMesh spot.
        ClearRoom(room);
    }

    /// <summary>
    /// Enters an uncleared Combat_Room: marks it visited, seals every door the room owns to contain the
    /// fight (R3.4), then activates its composition through the <see cref="RoomCompositionResolver"/>,
    /// spawning enemies at runtime-created spawn points inside the room (R3.5/R5.4). Each spawned actor is
    /// tracked so its death decrements the room's alive count (task 8.5). If activation places no enemies
    /// the room is cleared immediately so the run can proceed.
    /// </summary>
    private void EnterCombatRoom(Room room)
    {
        room.Visited = true;
        SealRoomDoors(room.Id);
        if (_objective) _objective.text = room.Type == RoomType.Boss
            ? "SETOR 01 / DERROTE O GUARDIAO DO NUCLEO"
            : $"SETOR 01 / MEMORIA {room.Id:00}\nElimine os inimigos e recupere o fragmento.";

        IReadOnlyList<EnemyRespawnPoint> spawnPoints = CreateSpawnPoints(room);
        // Boss_Rooms hand the player through so the resolver can mark the SectorBoss (R7.1); Combat_Rooms
        // pass null. The Boss_Room is tracked so its clear is recognized as the boss defeat that releases
        // the Stage transition (task 8.8, R7.3/R7.4).
        PlayerActor bossPlayer = _player;
        if (room.Type == RoomType.Boss)
        {
            _bossRoomId = room.Id;
        }

        RoomActivationResult activation = _compositionResolver.Activate(room, spawnPoints, _archetypeCatalog, transform, bossPlayer, _archetypePrefabs);

        int alive = 0;
        foreach (Actor actor in activation.SpawnedActors)
        {
            if (!actor)
            {
                continue;
            }

            _roomByActor[actor] = room.Id;
            actor.Died += OnEnemyDied;
            if (actor.TryGetComponent(out SpawnerBehavior spawner))
            {
                Action<Actor> handler = child => RegisterProducedEnemy(child, room.Id);
                _spawnHandlers[spawner] = handler;
                spawner.Produced += handler;
            }
            alive++;
        }

        _aliveByRoom[room.Id] = alive;

        // A room that spawned nothing (e.g. all enemies discarded, R5.7/R5.9) is already "clear": resolve
        // it right away so the player is not trapped behind sealed doors with no enemies to defeat.
        if (alive == 0)
        {
            ClearRoom(room);
        }
    }

    /// <summary>
    /// Creates one runtime <see cref="EnemyRespawnPoint"/> at the room center, injects the shared enemy
    /// prefab via the additive <c>ConfigurePrefab</c> initializer (which also suppresses death-respawns so
    /// this director owns the alive-count lifecycle) and returns it as the room's spawn-point list. The
    /// pure model carries no scene spawn points, so the director materializes them on entry; the resolver
    /// repositions the point per enemy while sampling the NavMesh (R5.4/R5.6).
    /// </summary>
    private IReadOnlyList<EnemyRespawnPoint> CreateSpawnPoints(Room room)
    {
        var spawnObject = new GameObject($"SpawnPoint (Room {room.Id})");
        spawnObject.transform.SetParent(transform, false);
        spawnObject.transform.position = ToWorld(room.Center);

        var spawnPoint = spawnObject.AddComponent<EnemyRespawnPoint>();
        spawnPoint.ConfigurePrefab(_enemyPrefab, transform);
        _spawnPoints.Add(spawnPoint);

        return new[] { spawnPoint };
    }

    private void RegisterProducedEnemy(Actor actor, int roomId)
    {
        if (!actor || IsRunComplete || !_aliveByRoom.ContainsKey(roomId)) return;
        _roomByActor[actor] = roomId;
        _aliveByRoom[roomId]++;
        actor.Died += OnEnemyDied;
    }

    private void UnsubscribeSpawners()
    {
        foreach (var pair in _spawnHandlers)
            if (pair.Key) pair.Key.Produced -= pair.Value;
        _spawnHandlers.Clear();
    }

    /// <summary>Seals every door the given room owns, containing combat inside it (R3.4).</summary>
    private void SealRoomDoors(int roomId)
    {
        if (!_gatesByRoom.TryGetValue(roomId, out List<(Direction direction, EncounterGates gates)> doors))
        {
            return;
        }

        foreach ((Direction direction, EncounterGates gates) in doors)
        {
            if (gates)
            {
                gates.SealDoor(direction);
            }
        }
    }

    /// <summary>
    /// Decrements the dying enemy's room alive count and, when it reaches zero, clears that room (task 8.5).
    /// Guards against a dead player / completed Run so no reward flow starts after the run has ended.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 6.1.</remarks>
    private void OnEnemyDied(Actor actor)
    {
        if (actor == null || !_roomByActor.TryGetValue(actor, out int roomId))
        {
            return;
        }

        actor.Died -= OnEnemyDied;
        _roomByActor.Remove(actor);

        if (IsRunComplete || !_player || _player.IsDead)
        {
            return;
        }

        if (!_aliveByRoom.TryGetValue(roomId, out int alive))
        {
            return;
        }

        alive = Mathf.Max(0, alive - 1);
        _aliveByRoom[roomId] = alive;

        if (alive == 0 && _roomsById.TryGetValue(roomId, out Room room) && !room.Cleared)
        {
            ClearRoom(room);
        }
    }

    /// <summary>
    /// Marks a Combat_Room cleared (R6.1), counts it toward <see cref="ClearedRooms"/> and drops a single
    /// Reward_Trophy on a reachable NavMesh spot inside the room (R6.2/R6.3). The room's exit doors stay
    /// sealed until the reward is claimed and a boon is chosen (R6.5/R6.6); the trophy claim opens the boon
    /// selection (R6.4) and choosing a boon opens the eligible exits (R6.7/R3.2). Called within the loop
    /// cadence so the room is marked cleared within ~1s of the last enemy dying.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 6.1, 6.2, 6.3.</remarks>
    private void ClearRoom(Room room)
    {
        room.Cleared = true;
        _aliveByRoom.Remove(room.Id);

        // The Boss_Room clearing is the boss defeat, not a standard rewarded Combat_Room clear: it does
        // not count toward ClearedRooms and it releases the Stage transition instead of dropping a
        // trophy (task 8.8, R7.3/R7.4).
        if (room.Type == RoomType.Boss || room.Id == _bossRoomId)
        {
            OnBossDefeated(room);
            return;
        }

        if (room.Type == RoomType.Combat) ClearedRooms++;
        if (_objective) _objective.text = "MEMORIA RECUPERADA\nRecolha o fragmento e escolha uma bencao.";

        // Hold this room's exits sealed until its reward is claimed and a boon is chosen (R6.5/R6.6).
        _rewardRoomId = room.Id;

        Vector3 spot = FindReachableSpot(room);
        _trophy = RewardTrophy.Spawn(spot, _player.transform, () => OnTrophyClaimed(room.Id));
    }

    /// <summary>
    /// Finds a NavMesh position inside the room that the player can reach by a continuous path, in up to
    /// <see cref="TrophyPlacementAttempts"/> attempts (R6.2). On failure it falls back to the reachable
    /// NavMesh point nearest the room center and logs a diagnostic, keeping the room cleared (R6.3). Mirrors
    /// the FirstSectorDirector <c>FindReachableSpot</c> pattern (bounds-clamped ring sampling + path check).
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 6.2, 6.3.</remarks>
    private Vector3 FindReachableSpot(Room room)
    {
        Vector3 center = ToWorld(room.Center);
        Vector3 playerPosition = _player.transform.position;
        float halfX = Mathf.Max(1f, room.Size.x * 0.5f - 1.5f);
        float halfZ = Mathf.Max(1f, room.Size.y * 0.5f - 1.5f);

        Vector3 nearestReachable = center;
        float nearestSqr = float.PositiveInfinity;
        bool hasReachable = false;

        for (int attempt = 0; attempt < TrophyPlacementAttempts; attempt++)
        {
            float angle = UnityEngine.Random.value * Mathf.PI * 2f;
            float radius = UnityEngine.Random.Range(1.5f, Mathf.Min(halfX, halfZ));
            Vector3 candidate = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            candidate.x = Mathf.Clamp(candidate.x, center.x - halfX, center.x + halfX);
            candidate.z = Mathf.Clamp(candidate.z, center.z - halfZ, center.z + halfZ);

            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                continue;
            }

            var path = new NavMeshPath();
            bool reachable = NavMesh.CalculatePath(playerPosition, hit.position, NavMesh.AllAreas, path)
                && path.status == NavMeshPathStatus.PathComplete;
            if (reachable)
            {
                float sqr = (hit.position - center).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearestReachable = hit.position;
                    hasReachable = true;
                }

                // A reachable spot inside the room satisfies R6.2 directly.
                return hit.position;
            }
        }

        if (hasReachable)
        {
            return nearestReachable;
        }

        // R6.3 fallback: no reachable candidate after the attempt budget. Snap toward the nearest NavMesh
        // point to the room center, log the diagnostic, and keep the room cleared.
        Debug.LogWarning(
            $"ProgressionDirector could not place a reachable Reward_Trophy in Room #{room.Id} after "
            + $"{TrophyPlacementAttempts} attempts; using the nearest NavMesh point to the room center (R6.3).",
            this);
        if (NavMesh.SamplePosition(center, out NavMeshHit centerHit, Mathf.Max(halfX, halfZ), NavMesh.AllAreas))
        {
            return centerHit.position;
        }

        return center;
    }

    /// <summary>
    /// Handles a Reward_Trophy claim (R6.4): removes the trophy reference and opens the boon selection for
    /// the cleared room via the reused <see cref="RunBoons"/>. The room's exits stay sealed while the
    /// selection is active (R6.5); they open when a boon is chosen (<see cref="OnRewardChosen"/>).
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 6.4, 6.5.</remarks>
    private void OnTrophyClaimed(int roomId)
    {
        _trophy = null;
        if (_boons)
        {
            _boons.OfferReward(roomId);
        }
    }

    /// <summary>
    /// Applies once a boon has been chosen for the pending reward room (R6.7): opens that room's exit
    /// connections that lead to not-yet-visited rooms and clears the pending-reward hold so the combat loop
    /// resumes. Connections to already-visited rooms and hidden (Secret) connections are left as-is (R3.2).
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 6.7, 3.2.</remarks>
    private void OnRewardChosen()
    {
        if (_rewardRoomId < 0)
        {
            return;
        }

        int resolvedRoomId = _rewardRoomId;
        if (_roomsById.TryGetValue(resolvedRoomId, out Room resolvedRoom) &&
            resolvedRoom.Type == RoomType.Combat && !HasBossAccess)
        {
            _bossAccessFragments++;
            UpdateBossSealProgress();
        }

        OpenEligibleExits(resolvedRoomId);
        if (HasBossAccess) TryUnlockBossConnections();
        _rewardRoomId = -1;
        UpdateExplorationObjective();
    }

    /// <summary>
    /// Opens the eligible exit connections of a cleared room: non-hidden connections whose neighbor room has
    /// not been visited yet (R6.7/R3.2). Each opened door is unsealed on both endpoints and the connection is
    /// flagged <see cref="RoomConnection.Open"/> so bidirectional passage is allowed (R3.1).
    /// </summary>
    private void OpenEligibleExits(int roomId)
    {
        foreach (RoomConnection connection in _graph.Connections)
        {
            if (connection.Hidden &&
                !(_roomsById[connection.RoomAId].Revealed || _roomsById[connection.RoomBId].Revealed))
            {
                continue;
            }

            int neighborId;
            if (connection.RoomAId == roomId)
            {
                neighborId = connection.RoomBId;
            }
            else if (connection.RoomBId == roomId)
            {
                neighborId = connection.RoomAId;
            }
            else
            {
                continue;
            }

            if (_roomsById.TryGetValue(neighborId, out Room destination) &&
                destination.Type == RoomType.Boss && !HasBossAccess)
            {
                connection.Open = false;
                continue;
            }

            // Eligible = leads to a room not yet visited (R6.7). Already-visited neighbors keep their
            // current gate state so re-entry stays idempotent (R3.3).
            if (_roomsById.TryGetValue(neighborId, out Room neighbor) && neighbor.Visited && !neighbor.Cleared && neighbor.Type != RoomType.Start)
            {
                continue;
            }

            if (_gatesByConnection.TryGetValue(connection, out EncounterGates gates) && gates)
            {
                gates.OpenDoor(connection.SideFromA);
                connection.Open = true;
            }
        }
    }

    private bool IsBossConnection(RoomConnection connection)
    {
        return _roomsById.TryGetValue(connection.RoomAId, out Room a) && a.Type == RoomType.Boss ||
               _roomsById.TryGetValue(connection.RoomBId, out Room b) && b.Type == RoomType.Boss;
    }

    private void UpdateBossSealProgress()
    {
        foreach (RoomConnection connection in _graph.Connections)
            if (IsBossConnection(connection) && _gatesByConnection.TryGetValue(connection, out EncounterGates gates) && gates)
                gates.SetBossSealProgress(_bossAccessFragments);
    }

    private void TryUnlockBossConnections()
    {
        foreach (RoomConnection connection in _graph.Connections)
        {
            if (!IsBossConnection(connection) || !_gatesByConnection.TryGetValue(connection, out EncounterGates gates) || !gates)
                continue;
            int approachId = _roomsById[connection.RoomAId].Type == RoomType.Boss
                ? connection.RoomBId : connection.RoomAId;
            if (!_roomsById.TryGetValue(approachId, out Room approach) || (!approach.Cleared && approach.Type != RoomType.Start))
                continue;
            gates.SetBossSealProgress(_bossAccessFragments);
            gates.OpenDoor(connection.SideFromA);
            connection.Open = true;
        }
    }

    private void UpdateExplorationObjective()
    {
        if (!_objective) return;
        _objective.text = HasBossAccess
            ? "SELO DO GUARDIAO ABERTO\nEncontre a arena e derrote o Guardiao do Nucleo."
            : $"FRAGMENTOS DE ACESSO {_bossAccessFragments}/{RequiredBossAccessFragments}\nExplore as salas | E: procurar memoria oculta";
    }

    /// <summary>
    /// Discovery_Action entry point (task 8.7, R4.9/R4.10). Meant to be invoked by the player's input
    /// (e.g. an interact button); input wiring lives outside this director, so this is exposed as a
    /// public method a player-controller/interaction component can call. It finds the room the player is
    /// currently in (the nearest room center) and, if that room is directly adjacent to a generated,
    /// still-hidden Secret_Room through a <see cref="RoomConnection.Hidden"/> connection, reveals the
    /// Secret_Room and opens that connection within ≤1s, surfacing a visual reveal indicator. If the
    /// player is NOT in a room adjacent to a hidden Secret connection, nothing is revealed and no
    /// connection is opened (R4.10).
    /// </summary>
    /// <remarks>
    /// Feature: procedural-stage-room-generation. Requirements: 4.9, 4.10.
    /// <para>
    /// Reveal indicator: the project ships no authored Secret-reveal VFX, so the visual indicator is the
    /// door opening itself (a previously sealed passage becomes traversable) plus a <c>Debug.Log</c>
    /// reveal message. A dedicated VFX/UI cue can be attached later at the logged reveal site without
    /// changing this flow.
    /// </para>
    /// </remarks>
    public void TryDiscoverSecret()
    {
        if (!_stageBuilt || _graph == null || IsRunComplete || !_player || _player.IsDead ||
            _aliveByRoom.Count > 0 || _rewardRoomId >= 0)
        {
            return;
        }

        Room current = FindRoomContainingPlayer();
        if (current == null)
        {
            // Not inside any room -> not adjacent to a Secret; keep everything hidden (R4.10).
            return;
        }

        foreach (RoomConnection connection in _graph.Connections)
        {
            if (!connection.Hidden)
            {
                continue;
            }

            int neighborId;
            if (connection.RoomAId == current.Id)
            {
                neighborId = connection.RoomBId;
            }
            else if (connection.RoomBId == current.Id)
            {
                neighborId = connection.RoomAId;
            }
            else
            {
                continue;
            }

            if (!_roomsById.TryGetValue(neighborId, out Room neighbor) || neighbor.Type != RoomType.Secret)
            {
                continue;
            }

            if (neighbor.Revealed)
            {
                // Already revealed on a previous Discovery_Action; nothing more to open.
                continue;
            }

            RevealSecret(neighbor, connection);
            return;
        }
    }

    /// <summary>
    /// Reveals a Secret_Room and opens its hidden connection (R4.9). Marks the room
    /// <see cref="Room.Revealed"/>, opens the door on both endpoints, flags the connection
    /// <see cref="RoomConnection.Open"/> and logs a reveal indicator. Runs synchronously so the passage
    /// is traversable well within the ≤1s budget.
    /// </summary>
    private void RevealSecret(Room secret, RoomConnection connection)
    {
        secret.Revealed = true;
        if (_environment) _environment.SetRevealed(secret.Id, true);

        if (_gatesByConnection.TryGetValue(connection, out EncounterGates gates) && gates)
        {
            gates.OpenDoor(connection.SideFromA);
        }

        connection.Open = true;

        // Reveal indicator (see method remarks): the newly opened door is the visible cue; the log marks
        // the reveal site for an optional authored VFX/UI hook.
        Debug.Log($"ProgressionDirector revealed Secret_Room #{secret.Id} and opened its hidden connection (R4.9).", this);
    }

    /// <summary>
    /// Returns the room whose center is nearest the player and within <see cref="RoomEntryDistance"/>,
    /// treating that as the room the player currently occupies for the Discovery_Action adjacency test
    /// (R4.9/R4.10). Returns null when the player is not near any room center.
    /// </summary>
    private Room FindRoomContainingPlayer()
    {
        Vector3 playerPosition = _player.transform.position;
        Room nearest = null;
        float nearestDistance = float.PositiveInfinity;

        foreach (Room room in _graph.Rooms)
        {
            float distance = HorizontalDistance(playerPosition, ToWorld(room.Center));
            Vector3 offset = playerPosition - ToWorld(room.Center);
            if (Mathf.Abs(offset.x) <= room.Size.x * .5f &&
                Mathf.Abs(offset.z) <= room.Size.y * .5f && distance <= nearestDistance)
            {
                nearestDistance = distance;
                nearest = room;
            }
        }

        return nearest;
    }

    /// <summary>
    /// Records the boss defeat and releases the Stage transition within ≤1s (task 8.8, R7.3/R7.4). While
    /// the boss lives the transition stays blocked because this is only reached when the Boss_Room's alive
    /// count hits zero. Guards against re-entry and against a Run that has already ended.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 7.3, 7.4.</remarks>
    private void OnBossDefeated(Room bossRoom)
    {
        if (_bossDefeated || IsRunComplete || _leaving)
        {
            return;
        }

        _bossDefeated = true;
        _bossRoomId = -1;
        _bossAccessFragments = 0;
        Debug.Log($"ProgressionDirector recorded the boss defeat in Room #{bossRoom.Id}; releasing the Stage transition.", this);
        StartCoroutine(ReleaseStageTransition());
    }

    /// <summary>
    /// Releases the Stage transition ≤1s after the boss defeat is recorded (R7.4), then advances: if a
    /// next Stage exists it is generated with a derived seed (task 8.8, R7.5); otherwise the Run concludes
    /// and returns to the Nexus reusing the existing return flow (R7.6). Aborts if the Run ended (e.g. the
    /// player died) during the delay.
    /// </summary>
    private IEnumerator ReleaseStageTransition()
    {
        yield return new WaitForSeconds(BossTransitionDelay);

        if (IsRunComplete || _leaving || !_player || _player.IsDead)
        {
            yield break;
        }

        AdvanceToNextStageOrConclude();
    }

    /// <summary>
    /// Advances the Run after a boss defeat (task 8.8). When a next Stage remains
    /// (<see cref="CurrentStageIndex"/> + 1 &lt; <see cref="_stageCount"/>), derives the next Run_Seed via
    /// <see cref="SeedDerivation.SplitMix64"/> (R7.5), tears down the current materialized Stage and
    /// generates + materializes the next one with that seed. When no next Stage is configured, concludes
    /// the Run and returns to the Nexus reusing the existing return flow (R7.6).
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 7.5, 7.6.</remarks>
    private void AdvanceToNextStageOrConclude()
    {
        bool hasNextStage = CurrentStageIndex + 1 < _stageCount;
        if (!hasNextStage)
        {
            // R7.6: no next Stage -> conclude the Run and return to the Nexus via the shared flow.
            Debug.Log("ProgressionDirector: boss defeated with no next Stage configured; concluding the Run.", this);
            IsRunComplete = true;
            ReturnToNexus();
            return;
        }

        // Derive the next Stage's seed deterministically from the current one (R7.5) and rebuild.
        ulong nextSeed = SeedDerivation.SplitMix64(_activeRunSeed);
        TearDownCurrentStage();
        CurrentStageIndex++;
        _bossDefeated = false;
        Debug.Log($"ProgressionDirector advancing to Stage index {CurrentStageIndex} with derived seed {nextSeed}.", this);
        MaterializeStageWithSeed(nextSeed);
    }

    /// <summary>
    /// Destroys the current Stage's materialized scene objects (gate GameObjects and runtime spawn
    /// points) and resets all per-Stage tracking so a fresh Stage can be materialized without the
    /// previous one accumulating in the scene. Enemy <c>Died</c> subscriptions are detached first so no
    /// stale callback fires against the torn-down Stage.
    /// </summary>
    private void TearDownCurrentStage()
    {
        UnsubscribeSpawners();
        foreach (Actor actor in _roomByActor.Keys)
        {
            if (actor)
            {
                actor.Died -= OnEnemyDied;
            }
        }

        _roomByActor.Clear();

        foreach (EncounterGates gates in _gatesByConnection.Values)
        {
            if (gates)
            {
                Destroy(gates.gameObject);
            }
        }

        foreach (EnemyRespawnPoint spawnPoint in _spawnPoints)
        {
            if (spawnPoint)
            {
                Destroy(spawnPoint.gameObject);
            }
        }

        _spawnPoints.Clear();
        _gatesByConnection.Clear();
        _gatesByRoom.Clear();
        _roomsById.Clear();
        _aliveByRoom.Clear();
        _rewardRoomId = -1;
        _bossRoomId = -1;
        _trophy = null;
        _graph = null;
        _stageBuilt = false;
    }

    /// <summary>
    /// Handles the player's death (task 8.9, R8.1-R8.5): marks the Run ended within ≤0.5s and blocks all
    /// subsequent spawns/transitions (the combat loop and boss transition both check
    /// <see cref="IsRunComplete"/>/<c>_player.IsDead</c>), then starts the guarded Nexus return within
    /// ≤1s. Idempotent against multiple death signals.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 8.1, 8.2, 8.3, 8.4, 8.5.</remarks>
    private void OnPlayerDied(Actor actor)
    {
        if (IsRunComplete && _leaving)
        {
            return;
        }

        // R8.1: mark the Run ended immediately (well within 0.5s). This alone blocks the combat loop
        // (it exits on IsRunComplete/_player.IsDead) and any pending boss transition.
        IsRunComplete = true;
        if (_objective)
        {
            _objective.text = "CONEXAO PERDIDA\nRetornando ao Nexus...";
        }

        StartCoroutine(ReturnToNexusAfterDeath());
    }

    /// <summary>
    /// Waits a short beat (≤1s of the Run ending, R8.2) so death feedback can play, then returns to the
    /// Nexus via the shared guarded flow. Skips if a return is already underway.
    /// </summary>
    private IEnumerator ReturnToNexusAfterDeath()
    {
        yield return new WaitForSeconds(DeathReturnDelay);
        ReturnToNexus();
    }

    /// <summary>
    /// Loads the Nexus_Scene reusing the FirstSectorDirector guard pattern (R8.2/R8.4/R8.5, R7.6). If the
    /// scene is missing from Build Settings it logs an error and stops WITHOUT loading (R8.4), keeping the
    /// Run marked ended. Otherwise it starts an async load and, on failure, logs and retries at most once
    /// (R8.5). The one-way <see cref="_leaving"/> guard prevents a second concurrent return. Transient
    /// Stage state is not preserved: a non-additive scene load replaces the current scene, so the loaded
    /// Nexus owns player positioning and input restoration (R8.3).
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 7.6, 8.2, 8.3, 8.4, 8.5.</remarks>
    private void ReturnToNexus()
    {
        if (_leaving)
        {
            return;
        }

        // R8.4: guard against a missing scene -> log and stop without loading, Run stays ended.
        if (!Application.CanStreamedLevelBeLoaded(_returnScene))
        {
            Debug.LogError(
                $"ProgressionDirector cannot return to the Nexus: scene '{_returnScene}' is missing from Build Settings. "
                + "Aborting the transition without loading another scene (R8.4).",
                this);
            return;
        }

        _leaving = true;
        StartCoroutine(LoadNexusScene());
    }

    /// <summary>
    /// Awaits the Nexus load and retries at most once on failure (R8.5). A load is considered failed when
    /// the <see cref="AsyncOperation"/> comes back null or does not reach completion; on the single retry
    /// the scene availability is re-checked before trying again.
    /// </summary>
    private IEnumerator LoadNexusScene()
    {
        const int maxAttempts = 2; // initial attempt + at most 1 retry (R8.5).
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            AsyncOperation load = SceneManager.LoadSceneAsync(_returnScene);
            if (load == null)
            {
                Debug.LogError(
                    $"ProgressionDirector failed to start loading the Nexus scene '{_returnScene}' (attempt {attempt}/{maxAttempts}).",
                    this);
            }
            else
            {
                while (!load.isDone)
                {
                    yield return null;
                }

                // A completed non-additive load replaces this scene; reaching here means success.
                yield break;
            }

            if (attempt < maxAttempts)
            {
                // Re-check availability before the single retry so a transiently missing scene is caught.
                if (!Application.CanStreamedLevelBeLoaded(_returnScene))
                {
                    Debug.LogError(
                        $"ProgressionDirector aborting Nexus load retry: scene '{_returnScene}' is unavailable (R8.5).",
                        this);
                    yield break;
                }

                yield return null;
            }
        }

        Debug.LogError(
            $"ProgressionDirector exhausted its Nexus load retry for scene '{_returnScene}'; transition interrupted (R8.5).",
            this);
    }

    /// <summary>
    /// Runs the seed → generate → materialize pipeline for a Stage using an explicit, already-derived
    /// seed (subsequent Stages, R7.5). Mirrors <see cref="GenerateAndMaterializeStage"/> but skips seed
    /// acquisition since the seed is derived from the previous Stage rather than the seed source. On
    /// generation failure it logs and concludes the Run with a Nexus return so the player is never left in
    /// an empty Stage.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 7.5, 1.7, 7.2.</remarks>
    private void MaterializeStageWithSeed(ulong seed)
    {
        _activeRunSeed = seed;
        Debug.Log($"ProgressionDirector Stage {CurrentStageIndex} Run_Seed = {seed} (derived).", this);

        StageGenerationResult result = _generator.Generate(_generationParams, seed);
        if (!result.Success || result.Graph == null)
        {
            string reason = string.IsNullOrEmpty(result.FailureReason) ? "unknown reason" : result.FailureReason;
            Debug.LogError(
                $"ProgressionDirector aborted next-Stage generation (seed {seed}): {reason}. Concluding the Run.",
                this);
            IsRunComplete = true;
            ReturnToNexus();
            return;
        }

        if (!string.IsNullOrEmpty(result.FailureReason))
        {
            Debug.LogWarning($"ProgressionDirector next Stage generated with a diagnostic (seed {seed}): {result.FailureReason}.", this);
        }

        _graph = result.Graph;
        IndexRooms(_graph);
        if (_environment) _environment.Build(_graph);
        MaterializeGates(_graph);
        PositionPlayerAtStart(_graph);
        OpenStartRoomDoors(_graph);
        _stageBuilt = true;
        UpdateExplorationObjective();
    }

    /// <summary>Horizontal (XZ) distance between two world points, ignoring height.</summary>
    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>Converts a Room center on the XZ plane to a world position at y = 0.</summary>
    private static Vector3 ToWorld(Vector2 centerXZ) => new Vector3(centerXZ.x, 0f, centerXZ.y);

    /// <summary>Returns the opposite cardinal direction, used to mirror a shared door onto room B.</summary>
    private static Direction Opposite(Direction direction) => direction switch
    {
        Direction.North => Direction.South,
        Direction.South => Direction.North,
        Direction.East => Direction.West,
        Direction.West => Direction.East,
        _ => direction,
    };

    /// <summary>
    /// Re-validates dependencies in the Editor and forwards clamping to
    /// <see cref="StageGenerationParams"/>. Logs the same single message naming every
    /// missing required dependency (R9.1). Guarded so it stays quiet while the object is
    /// still being assembled (e.g. a freshly added component with no references yet) to
    /// avoid noisy edit-mode logging.
    /// </summary>
    private void OnValidate()
    {
        _generationParams?.OnValidate();

        // Only report once the component looks configured: if EVERY required dependency is
        // still unset, treat it as an in-progress setup and stay silent. As soon as at least
        // one is wired up, surface the remaining gaps so the designer sees them (R9.1).
        if (HasAnyRequiredDependency())
        {
            ValidateDependencies();
        }
    }

    /// <summary>
    /// Checks every required dependency and, when any is missing, logs EXACTLY ONE error
    /// naming each missing one (R9.1). The <see cref="_seedSource"/> is deliberately
    /// excluded from the required set because an absent/invalid seed source is covered by
    /// the fallback seed policy (R2.2) and must not block startup.
    /// </summary>
    /// <returns><c>true</c> when all required dependencies are present; otherwise <c>false</c>.</returns>
    private bool ValidateDependencies()
    {
        var missing = new List<string>();

        if (!_player)
        {
            missing.Add(nameof(_player) + " (PlayerActor)");
        }

        if (_generationParams == null)
        {
            missing.Add(nameof(_generationParams) + " (StageGenerationParams)");
        }

        if (!_enemyPrefab)
        {
            missing.Add(nameof(_enemyPrefab) + " (enemy prefab)");
        }

        if (_archetypeCatalog == null || _archetypeCatalog.Length == 0)
        {
            missing.Add(nameof(_archetypeCatalog) + " (EnemyArchetype catalog)");
        }

        if (!_objective)
        {
            missing.Add(nameof(_objective) + " (TMP_Text objective)");
        }

        if (missing.Count == 0)
        {
            return true;
        }

        var builder = new StringBuilder();
        builder.Append("ProgressionDirector is missing required dependencies: ");
        for (int i = 0; i < missing.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(", ");
            }

            builder.Append(missing[i]);
        }

        builder.Append('.');
        Debug.LogError(builder.ToString(), this);
        return false;
    }

    /// <summary>
    /// True when at least one required dependency has been assigned. Used by
    /// <see cref="OnValidate"/> to suppress error logging for a component that has not been
    /// configured at all yet.
    /// </summary>
    private bool HasAnyRequiredDependency()
    {
        return _player
            || _generationParams != null
            || _enemyPrefab
            || (_archetypeCatalog != null && _archetypeCatalog.Length > 0)
            || _objective;
    }

    /// <summary>
    /// Detaches every event the director subscribed to (the reused <see cref="RunBoons.RewardChosen"/> and
    /// each tracked enemy's <c>Died</c>) so no callback fires against a torn-down director, mirroring the
    /// FirstSectorDirector cleanup.
    /// </summary>
    private void OnDestroy()
    {
        UnsubscribeSpawners();
        if (_boons)
        {
            _boons.RewardChosen -= OnRewardChosen;
        }

        if (_player)
        {
            _player.Died -= OnPlayerDied;
        }

        foreach (Actor actor in _roomByActor.Keys)
        {
            if (actor)
            {
                actor.Died -= OnEnemyDied;
            }
        }

        _roomByActor.Clear();
    }
}
