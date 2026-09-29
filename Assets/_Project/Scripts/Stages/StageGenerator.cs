using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure generation core that turns a <see cref="StageGenerationParams"/> plus a 64-bit
/// Run_Seed into a fully specified <see cref="RoomGraph"/>. It touches no scene state
/// (no <see cref="GameObject"/>, no <see cref="MonoBehaviour"/>): it only produces the
/// data model that the scene orchestration layer later materializes.
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 1.1, 1.2, 1.3, 1.4, 1.6, 1.7,
/// 2.3, 2.5, 4.1, 4.2, 4.3, 4.4, 4.5, 4.6, 4.7, 4.8, 5.1, 5.2, 5.3, 5.5, 7.1, 7.2.
/// <para>
/// Determinism (R1.3/R2.3): the same Run_Seed and parameters always yield structurally
/// identical graphs because every random decision is drawn from an isolated
/// <see cref="SeededRng"/> seeded exclusively by the Run_Seed (R2.5) — the core never
/// consumes <c>UnityEngine.Random</c> or any other shared/global randomness source.
/// </para>
/// <para>
/// The generation algorithm follows design.md "Algoritmo de geração":
/// draw counts and specials (R1.1/R4.3-R4.5), grow a connected non-overlapping graph on
/// a uniform grid from the Start_Room and attach the Boss_Room at the end of a deep
/// branch (R1.2/R1.4), compute each room's shortest-path Depth (R5.5), assign room types
/// and place the drawn Treasure/Secret rooms (R4.1/R4.2/R4.6-R4.8), attach a
/// <see cref="RoomComposition"/> to every Combat_Room and to the Boss_Room (R1.6/R5.*),
/// and mark the boss as eligible (R7.1). The whole attempt is retried up to the
/// configured limit; on failure the core never returns a partial graph — it signals
/// <see cref="StageGenerationResult.Success"/> = false with a
/// <see cref="StageGenerationResult.FailureReason"/> for the caller to log (R1.7/R7.2).
/// </para>
/// </remarks>
public sealed class StageGenerator
{
    /// <summary>Uniform room footprint on the XZ plane. A single size keeps grid placement
    /// math exact so adjacent AABBs touch with a gap of exactly 0 and never interpenetrate
    /// (R1.4).</summary>
    private static readonly Vector2 RoomSize = new Vector2(10f, 10f);

    /// <summary>Stable id of the Start_Room; it is always created first (R4.2).</summary>
    private const int StartRoomId = 0;

    /// <summary>
    /// Plans the enemy composition of each Combat_Room. Held as a field so later tasks
    /// can attach compositions without re-instantiating per generation attempt.
    /// </summary>
    private readonly RoomCompositionPlanner _compositionPlanner = new RoomCompositionPlanner();

    /// <summary>The 16 archetypes available to every room's composition (R5.1).</summary>
    private static readonly ArchetypeId[] AllArchetypes = (ArchetypeId[])Enum.GetValues(typeof(ArchetypeId));

    /// <summary>
    /// Generates a Stage's <see cref="RoomGraph"/> from the supplied
    /// <paramref name="parameters"/> and <paramref name="runSeed"/>. Deterministic: the
    /// same inputs always produce a structurally identical graph (R1.3/R2.3). Never
    /// touches the scene and never consumes global randomness (R2.5).
    /// </summary>
    /// <param name="parameters">
    /// The generation configuration (room-count bounds, density budget, variety target,
    /// special-room probabilities, retry limit). Must not be null.
    /// </param>
    /// <param name="runSeed">The 64-bit Run_Seed that seeds all random decisions (R2.5).</param>
    /// <returns>
    /// A successful result carrying the generated <see cref="RoomGraph"/>, or a failed
    /// result with a <see cref="StageGenerationResult.FailureReason"/> and no partial
    /// graph (R1.7/R7.2).
    /// </returns>
    public StageGenerationResult Generate(StageGenerationParams parameters, ulong runSeed)
    {
        if (parameters == null)
        {
            return StageGenerationResult.Fail("StageGenerationParams was null.");
        }

        // Isolated RNG seeded exclusively by the Run_Seed (R2.5): all subsequent random
        // decisions in the algorithm draw from this stream so generation is deterministic
        // and independent of any global randomness state.
        var rng = new SeededRng(runSeed);

        // Retry whole-graph generation up to the configured limit (R1.7). Every attempt
        // consumes the same RNG stream, so the sequence of attempts is itself
        // deterministic for a given seed. A failed attempt never leaks a partial graph.
        int retryLimit = Mathf.Max(1, parameters.GenerationRetryLimit);
        string lastReason = "StageGenerator produced no connected Start -> Boss graph.";
        for (int attempt = 0; attempt < retryLimit; attempt++)
        {
            StageGenerationResult result = TrySingleGeneration(parameters, rng);
            if (result.Success)
            {
                return result;
            }

            lastReason = result.FailureReason;
        }

        // No connected Start -> Boss graph after every attempt: abort with no partial
        // graph (R1.7/R7.2). The ProgressionDirector logs this and instantiates nothing.
        return StageGenerationResult.Fail(
            $"StageGenerator failed to produce a connected Start -> Boss graph after {retryLimit} attempts. Last reason: {lastReason}");
    }

    /// <summary>
    /// Attempts a single whole-graph generation pass using the isolated
    /// <paramref name="rng"/>. Draws counts and specials, grows the connected
    /// non-overlapping grid graph, assigns types, places specials, attaches compositions
    /// and marks the boss. Returns a failed result (never a partial graph) when a
    /// mandatory step cannot be satisfied so the caller can retry (R1.7/R7.2).
    /// </summary>
    /// <param name="parameters">The generation configuration.</param>
    /// <param name="rng">The isolated, seeded RNG driving every random decision (R2.5).</param>
    /// <returns>The result of this single generation attempt.</returns>
    private StageGenerationResult TrySingleGeneration(StageGenerationParams parameters, SeededRng rng)
    {
        // ---- TASK 4.2: draw counts and special-room presence ----------------------------
        int maxCombat = Mathf.Min(parameters.MaxCombatRooms, StageGenerationParams.MaxCombatRoomCeiling);
        int minCombat = Mathf.Clamp(parameters.MinCombatRooms, 1, maxCombat);
        int combatCount = rng.NextIntInclusive(minCombat, maxCombat); // R1.1

        // Weighted rolls for each Special_Room in [5%, 95%] (params already clamp the
        // probabilities to that range). At most one Treasure and one Secret (R4.5).
        bool wantTreasure = rng.Roll(parameters.TreasureProbability); // R4.3/R4.4
        bool wantSecret = rng.Roll(parameters.SecretProbability);     // R4.3/R4.4

        // ---- TASK 4.3: grow the connected, non-overlapping grid graph -------------------
        // The layout lives on an integer grid: a room at grid cell (gx, gz) has its center
        // at (gx * width, gz * depth). Distinct cells therefore produce AABBs whose edges
        // touch with a gap of exactly 0 and never interpenetrate (R1.4). Non-overlap is
        // guaranteed by never placing two rooms in the same cell.
        var builder = new GraphBuilder(RoomSize);

        // Start_Room at the grid origin (R4.2). It is depth 0 by construction.
        builder.AddStart(StartRoomId);

        // Grow the combat body one room at a time by picking an existing room and a free
        // cardinal direction whose target cell is unoccupied. Each placement creates
        // exactly one RoomConnection for the adjacent pair (R1.5 materializes one gate pair
        // per connection at runtime).
        int nextId = StartRoomId + 1;
        for (int i = 0; i < combatCount; i++)
        {
            int placedId = builder.GrowRoom(nextId, RoomType.Combat, rng, requireLeafParent: false);
            if (placedId < 0)
            {
                // No free non-overlapping slot for a mandatory Combat_Room: fail this
                // attempt so Generate retries with the advanced RNG stream.
                return StageGenerationResult.Fail("Could not place a mandatory Combat_Room without overlap.");
            }

            nextId++;
        }

        // ---- TASK 4.3 (cont): attach the Boss_Room at the end of a deep branch ----------
        // Attaching to the deepest available room guarantees a Start -> Boss path exists
        // (R1.2) and keeps the boss far from the entrance.
        int bossId = builder.GrowFromDeepest(nextId, RoomType.Boss, rng);
        if (bossId < 0)
        {
            return StageGenerationResult.Fail("Could not attach the Boss_Room to a deep branch without overlap.");
        }

        nextId++;

        // ---- TASK 4.4: place the drawn Treasure/Secret rooms on suitable leaves ---------
        // Treasure attaches to a leaf via a normal (accessible, openable) connection. If it
        // cannot be attached accessibly it is discarded WITHOUT failing the whole
        // generation (R4.6/R4.7) — recorded in the diagnostics below.
        string treasureDiagnostic = null;
        if (wantTreasure)
        {
            int treasureId = builder.GrowFromLeaf(nextId, RoomType.Treasure, rng, hidden: false);
            if (treasureId < 0)
            {
                // Preserve the rest of the graph; just note the discard (R4.7).
                treasureDiagnostic = "Treasure_Room discarded: no accessible leaf connection could be placed.";
            }
            else
            {
                nextId++;
            }
        }

        // Secret attaches to a leaf via a hidden, closed connection (R4.8). A Secret that
        // cannot be placed is simply omitted (it is optional and never mandatory).
        if (wantSecret)
        {
            int secretId = builder.GrowFromLeaf(nextId, RoomType.Secret, rng, hidden: true);
            if (secretId >= 0)
            {
                nextId++;
            }
        }

        // ---- Depth via shortest-path BFS from the Start_Room (R5.5) ----------------------
        builder.ComputeDepths(StartRoomId);

        // Sanity: a connected Start -> Boss path must exist (R1.2). GrowFromDeepest always
        // attaches to a reachable room, but this guards against any regression.
        if (!builder.IsReachable(StartRoomId, bossId))
        {
            return StageGenerationResult.Fail("Generated graph has no Start -> Boss path.");
        }

        // ---- TASK 4.4 (cont): verify Treasure accessibility, else discard (R4.6/R4.7) ---
        if (builder.TryGetSpecialId(RoomType.Treasure, out int placedTreasureId))
        {
            if (!builder.IsReachableViaOpenable(StartRoomId, placedTreasureId))
            {
                builder.DiscardRoom(placedTreasureId);
                treasureDiagnostic = "Treasure_Room discarded: placed room was not accessible from the Start_Room.";
                builder.ComputeDepths(StartRoomId);
            }
        }

        int maxDepth = builder.MaxDepth();

        // ---- TASK 4.5: attach compositions and spawn points to every Combat_Room --------
        // Each Combat_Room receives a non-null RoomComposition from the planner. In the
        // pure model a Combat_Room "has a spawn point" (Property 11 / R1.6) exactly when it
        // carries a non-null Composition; the actual EnemyRespawnPoint placement happens at
        // runtime activation (task 7.x). Start/Treasure/Secret keep a null composition.
        builder.AttachCombatCompositions(_compositionPlanner, parameters, AllArchetypes, maxDepth, rng);

        // ---- TASK 4.6: mark the boss as eligible (R7.1/R7.2) ----------------------------
        // Enemies are instantiated at runtime, so boss eligibility is modeled at the data
        // level: the Boss_Room must carry a composition with at least one archetype slot
        // (the primary enemy the SectorBoss will mark). Building that composition below
        // guarantees eligibility for any non-degenerate archetype set; if it somehow yields
        // no primary enemy, abort with no partial graph (R7.2).
        RoomComposition bossComposition = _compositionPlanner.Plan(
            parameters, AllArchetypes, builder.GetDepth(bossId), Mathf.Max(1, maxDepth), rng);
        if (bossComposition == null || bossComposition.Slots == null || bossComposition.Slots.Count == 0)
        {
            return StageGenerationResult.Fail("Boss_Room has no eligible primary enemy to mark as SectorBoss.");
        }

        builder.SetComposition(bossId, bossComposition);

        // ---- Materialize the immutable RoomGraph ----------------------------------------
        RoomGraph graph = builder.Build(StartRoomId, bossId);

        // Treasure discard is a diagnostic, not a failure: the generation still succeeds
        // with the rest of the rooms intact (R4.7). The reason travels on a successful
        // result for the caller to surface if desired.
        return treasureDiagnostic != null
            ? StageGenerationResult.Ok(graph, treasureDiagnostic)
            : StageGenerationResult.Ok(graph);
    }

    /// <summary>
    /// Mutable scratch structure used during a single generation attempt. It tracks room
    /// records, their occupied grid cells and their connections, then bakes an immutable
    /// <see cref="RoomGraph"/> at the end. Kept private to the generator so the pure model
    /// types (<see cref="Room"/>, <see cref="RoomConnection"/>) stay free of build state.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 1.2, 1.4, 5.5.</remarks>
    private sealed class GraphBuilder
    {
        private readonly Vector2 _roomSize;
        private readonly List<RoomRecord> _rooms = new List<RoomRecord>();
        private readonly Dictionary<int, int> _idToIndex = new Dictionary<int, int>();
        private readonly Dictionary<long, int> _cellToId = new Dictionary<long, int>();
        private readonly List<RoomConnection> _connections = new List<RoomConnection>();

        public GraphBuilder(Vector2 roomSize)
        {
            _roomSize = roomSize;
        }

        /// <summary>Adds the Start_Room at the grid origin cell (0, 0).</summary>
        public void AddStart(int id)
        {
            AddRoom(id, RoomType.Start, 0, 0);
        }

        /// <summary>
        /// Places a new room adjacent to some existing room in a free cardinal direction.
        /// When <paramref name="requireLeafParent"/> is true the parent must currently have
        /// no other placed neighbor (used for hanging special rooms off leaves). Returns
        /// the new room id, or -1 when no free non-overlapping slot exists.
        /// </summary>
        public int GrowRoom(int newId, RoomType type, SeededRng rng, bool requireLeafParent)
        {
            // Shuffle the candidate parents so growth is organic yet seed-reproducible.
            var parentOrder = BuildShuffledIndexOrder(rng);
            foreach (int parentIndex in parentOrder)
            {
                RoomRecord parent = _rooms[parentIndex];
                if (requireLeafParent && parent.NeighborCount > 0)
                {
                    continue;
                }

                if (TryPlaceAdjacent(parent, newId, type, rng, hidden: false))
                {
                    return newId;
                }
            }

            return -1;
        }

        /// <summary>
        /// Attaches a new room to the deepest existing room (by current hop distance from
        /// the Start_Room), falling back to shallower rooms if the deepest has no free
        /// side. Guarantees the new room extends a Start-reachable branch (R1.2).
        /// </summary>
        public int GrowFromDeepest(int newId, RoomType type, SeededRng rng)
        {
            ComputeDepths(_rooms[0].Id);

            // Order parents deepest-first; ties broken deterministically by id.
            var ordered = new List<int>(_rooms.Count);
            for (int i = 0; i < _rooms.Count; i++)
            {
                ordered.Add(i);
            }

            ordered.Sort((a, b) =>
            {
                int depthCompare = _rooms[b].Depth.CompareTo(_rooms[a].Depth);
                return depthCompare != 0 ? depthCompare : _rooms[a].Id.CompareTo(_rooms[b].Id);
            });

            foreach (int parentIndex in ordered)
            {
                if (TryPlaceAdjacent(_rooms[parentIndex], newId, type, rng, hidden: false))
                {
                    return newId;
                }
            }

            return -1;
        }

        /// <summary>
        /// Attaches a new room to a leaf (a room with a single neighbor) or, if none has a
        /// free side, to any room. Used for Treasure/Secret rooms. The connection is marked
        /// <paramref name="hidden"/> for Secret rooms (R4.8).
        /// </summary>
        public int GrowFromLeaf(int newId, RoomType type, SeededRng rng, bool hidden)
        {
            var parentOrder = BuildShuffledIndexOrder(rng);

            // First pass: prefer genuine leaves (exactly one neighbor) so specials hang off
            // the branch tips rather than the trunk.
            foreach (int parentIndex in parentOrder)
            {
                if (_rooms[parentIndex].NeighborCount != 1)
                {
                    continue;
                }

                if (TryPlaceAdjacent(_rooms[parentIndex], newId, type, rng, hidden))
                {
                    return newId;
                }
            }

            // Second pass: any room with a free side.
            foreach (int parentIndex in parentOrder)
            {
                if (TryPlaceAdjacent(_rooms[parentIndex], newId, type, rng, hidden))
                {
                    return newId;
                }
            }

            return -1;
        }

        /// <summary>Computes each room's shortest-path hop distance from
        /// <paramref name="startId"/> using BFS over all connections (R5.5).</summary>
        public void ComputeDepths(int startId)
        {
            foreach (RoomRecord room in _rooms)
            {
                room.Depth = int.MaxValue;
            }

            if (!_idToIndex.TryGetValue(startId, out int startIndex))
            {
                return;
            }

            var adjacency = BuildAdjacency(includeHidden: true);
            var queue = new Queue<int>();
            _rooms[startIndex].Depth = 0;
            queue.Enqueue(startId);
            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                int currentDepth = _rooms[_idToIndex[currentId]].Depth;
                if (!adjacency.TryGetValue(currentId, out List<int> neighbors))
                {
                    continue;
                }

                foreach (int neighborId in neighbors)
                {
                    RoomRecord neighbor = _rooms[_idToIndex[neighborId]];
                    if (neighbor.Depth != int.MaxValue)
                    {
                        continue;
                    }

                    neighbor.Depth = currentDepth + 1;
                    queue.Enqueue(neighborId);
                }
            }

            // Any unreached room (should not happen for a connected build) gets depth 0 to
            // keep density scaling well-defined.
            foreach (RoomRecord room in _rooms)
            {
                if (room.Depth == int.MaxValue)
                {
                    room.Depth = 0;
                }
            }
        }

        /// <summary>Returns the depth previously computed for the given room id.</summary>
        public int GetDepth(int id) => _rooms[_idToIndex[id]].Depth;

        /// <summary>Returns the largest depth across all rooms.</summary>
        public int MaxDepth()
        {
            int max = 0;
            foreach (RoomRecord room in _rooms)
            {
                if (room.Depth > max)
                {
                    max = room.Depth;
                }
            }

            return max;
        }

        /// <summary>True when <paramref name="targetId"/> is reachable from
        /// <paramref name="startId"/> across any connection (hidden included).</summary>
        public bool IsReachable(int startId, int targetId)
        {
            return Reachable(startId, targetId, includeHidden: true);
        }

        /// <summary>True when <paramref name="targetId"/> is reachable from
        /// <paramref name="startId"/> using only non-hidden (openable) connections, i.e.
        /// accessible without a Discovery_Action (R4.6).</summary>
        public bool IsReachableViaOpenable(int startId, int targetId)
        {
            return Reachable(startId, targetId, includeHidden: false);
        }

        /// <summary>Finds a special room's id by type, if present.</summary>
        public bool TryGetSpecialId(RoomType type, out int id)
        {
            foreach (RoomRecord room in _rooms)
            {
                if (room.Type == type)
                {
                    id = room.Id;
                    return true;
                }
            }

            id = -1;
            return false;
        }

        /// <summary>Removes a room and all of its connections (used to discard an
        /// inaccessible Treasure while preserving the rest of the graph, R4.7).</summary>
        public void DiscardRoom(int id)
        {
            if (!_idToIndex.TryGetValue(id, out int index))
            {
                return;
            }

            RoomRecord record = _rooms[index];
            _cellToId.Remove(CellKey(record.GridX, record.GridZ));

            _connections.RemoveAll(c => c.RoomAId == id || c.RoomBId == id);

            _rooms.RemoveAt(index);
            RebuildIndex();
            RecountNeighbors();
        }

        /// <summary>Attaches a planner-built composition to every Combat_Room (R1.6/R5.*).</summary>
        public void AttachCombatCompositions(
            RoomCompositionPlanner planner,
            StageGenerationParams parameters,
            IReadOnlyList<ArchetypeId> archetypes,
            int maxDepth,
            SeededRng rng)
        {
            int scaleDepth = Mathf.Max(1, maxDepth);
            foreach (RoomRecord room in _rooms)
            {
                if (room.Type != RoomType.Combat)
                {
                    continue;
                }

                room.Composition = planner.Plan(parameters, archetypes, room.Depth, scaleDepth, rng);
            }
        }

        /// <summary>Overrides the composition of a single room (used for the Boss_Room).</summary>
        public void SetComposition(int id, RoomComposition composition)
        {
            _rooms[_idToIndex[id]].Composition = composition;
        }

        /// <summary>Bakes the immutable <see cref="RoomGraph"/> from the current build state.</summary>
        public RoomGraph Build(int startId, int bossId)
        {
            var rooms = new List<Room>(_rooms.Count);
            foreach (RoomRecord record in _rooms)
            {
                var roomConnections = new List<RoomConnection>();
                foreach (RoomConnection connection in _connections)
                {
                    if (connection.RoomAId == record.Id || connection.RoomBId == record.Id)
                    {
                        roomConnections.Add(connection);
                    }
                }

                var center = new Vector2(record.GridX * _roomSize.x, record.GridZ * _roomSize.y);
                rooms.Add(new Room(
                    record.Id,
                    record.Type,
                    center,
                    _roomSize,
                    record.Depth,
                    roomConnections,
                    record.Composition));
            }

            return new RoomGraph(rooms, _connections, startId, bossId);
        }

        /// <summary>
        /// Tries to place <paramref name="newId"/> adjacent to <paramref name="parent"/> in
        /// a free, unoccupied cardinal direction. Creates the RoomConnection on success.
        /// </summary>
        private bool TryPlaceAdjacent(RoomRecord parent, int newId, RoomType type, SeededRng rng, bool hidden)
        {
            foreach (Direction side in ShuffledDirections(rng))
            {
                if (parent.IsSideUsed(side))
                {
                    continue;
                }

                Offset(side, out int dx, out int dz);
                int targetX = parent.GridX + dx;
                int targetZ = parent.GridZ + dz;
                if (_cellToId.ContainsKey(CellKey(targetX, targetZ)))
                {
                    continue; // Cell occupied: would overlap (R1.4). Try another side.
                }

                RoomRecord placed = AddRoom(newId, type, targetX, targetZ);
                _connections.Add(new RoomConnection(parent.Id, placed.Id, side, hidden, open: false));
                parent.MarkSide(side);
                placed.MarkSide(Opposite(side));
                parent.NeighborCount++;
                placed.NeighborCount++;
                return true;
            }

            return false;
        }

        private RoomRecord AddRoom(int id, RoomType type, int gridX, int gridZ)
        {
            var record = new RoomRecord(id, type, gridX, gridZ);
            _idToIndex[id] = _rooms.Count;
            _rooms.Add(record);
            _cellToId[CellKey(gridX, gridZ)] = id;
            return record;
        }

        private List<int> BuildShuffledIndexOrder(SeededRng rng)
        {
            var order = new List<int>(_rooms.Count);
            for (int i = 0; i < _rooms.Count; i++)
            {
                order.Add(i);
            }

            // Fisher-Yates using the seeded RNG so the walk is reproducible.
            for (int i = order.Count - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (order[i], order[j]) = (order[j], order[i]);
            }

            return order;
        }

        private static Direction[] ShuffledDirections(SeededRng rng)
        {
            var dirs = new[] { Direction.North, Direction.South, Direction.East, Direction.West };
            for (int i = dirs.Length - 1; i > 0; i--)
            {
                int j = rng.NextInt(0, i + 1);
                (dirs[i], dirs[j]) = (dirs[j], dirs[i]);
            }

            return dirs;
        }

        private Dictionary<int, List<int>> BuildAdjacency(bool includeHidden)
        {
            var adjacency = new Dictionary<int, List<int>>(_rooms.Count);
            foreach (RoomRecord room in _rooms)
            {
                adjacency[room.Id] = new List<int>();
            }

            foreach (RoomConnection connection in _connections)
            {
                if (!includeHidden && connection.Hidden)
                {
                    continue;
                }

                if (adjacency.TryGetValue(connection.RoomAId, out List<int> a))
                {
                    a.Add(connection.RoomBId);
                }

                if (adjacency.TryGetValue(connection.RoomBId, out List<int> b))
                {
                    b.Add(connection.RoomAId);
                }
            }

            return adjacency;
        }

        private bool Reachable(int startId, int targetId, bool includeHidden)
        {
            if (startId == targetId)
            {
                return true;
            }

            if (!_idToIndex.ContainsKey(startId) || !_idToIndex.ContainsKey(targetId))
            {
                return false;
            }

            var adjacency = BuildAdjacency(includeHidden);
            var visited = new HashSet<int> { startId };
            var queue = new Queue<int>();
            queue.Enqueue(startId);
            while (queue.Count > 0)
            {
                int currentId = queue.Dequeue();
                if (!adjacency.TryGetValue(currentId, out List<int> neighbors))
                {
                    continue;
                }

                foreach (int neighborId in neighbors)
                {
                    if (neighborId == targetId)
                    {
                        return true;
                    }

                    if (visited.Add(neighborId))
                    {
                        queue.Enqueue(neighborId);
                    }
                }
            }

            return false;
        }

        private void RebuildIndex()
        {
            _idToIndex.Clear();
            for (int i = 0; i < _rooms.Count; i++)
            {
                _idToIndex[_rooms[i].Id] = i;
            }
        }

        private void RecountNeighbors()
        {
            foreach (RoomRecord room in _rooms)
            {
                room.NeighborCount = 0;
                room.ResetSides();
            }

            foreach (RoomConnection connection in _connections)
            {
                if (!_idToIndex.TryGetValue(connection.RoomAId, out int aIndex) ||
                    !_idToIndex.TryGetValue(connection.RoomBId, out int bIndex))
                {
                    continue;
                }

                RoomRecord a = _rooms[aIndex];
                RoomRecord b = _rooms[bIndex];
                a.MarkSide(connection.SideFromA);
                b.MarkSide(Opposite(connection.SideFromA));
                a.NeighborCount++;
                b.NeighborCount++;
            }
        }

        private static long CellKey(int gridX, int gridZ)
        {
            // Pack two 32-bit grid coordinates into one 64-bit key.
            return ((long)gridX << 32) ^ (uint)gridZ;
        }

        private static void Offset(Direction side, out int dx, out int dz)
        {
            switch (side)
            {
                case Direction.North: dx = 0; dz = 1; break;
                case Direction.South: dx = 0; dz = -1; break;
                case Direction.East: dx = 1; dz = 0; break;
                default: dx = -1; dz = 0; break; // West
            }
        }

        private static Direction Opposite(Direction side)
        {
            switch (side)
            {
                case Direction.North: return Direction.South;
                case Direction.South: return Direction.North;
                case Direction.East: return Direction.West;
                default: return Direction.East; // West
            }
        }

        /// <summary>Per-room mutable build record; never leaks into the immutable graph.</summary>
        private sealed class RoomRecord
        {
            private bool _north;
            private bool _south;
            private bool _east;
            private bool _west;

            public RoomRecord(int id, RoomType type, int gridX, int gridZ)
            {
                Id = id;
                Type = type;
                GridX = gridX;
                GridZ = gridZ;
                Depth = 0;
            }

            public int Id { get; }
            public RoomType Type { get; }
            public int GridX { get; }
            public int GridZ { get; }
            public int Depth { get; set; }
            public int NeighborCount { get; set; }
            public RoomComposition Composition { get; set; }

            public bool IsSideUsed(Direction side)
            {
                switch (side)
                {
                    case Direction.North: return _north;
                    case Direction.South: return _south;
                    case Direction.East: return _east;
                    default: return _west;
                }
            }

            public void MarkSide(Direction side)
            {
                switch (side)
                {
                    case Direction.North: _north = true; break;
                    case Direction.South: _south = true; break;
                    case Direction.East: _east = true; break;
                    default: _west = true; break;
                }
            }

            public void ResetSides()
            {
                _north = _south = _east = _west = false;
            }
        }
    }
}

/// <summary>
/// Outcome of a <see cref="StageGenerator.Generate"/> call. On success it carries the
/// generated <see cref="Graph"/>; on failure it carries a human-readable
/// <see cref="FailureReason"/> for the caller to log and never a partial graph
/// (R1.7/R7.2). A successful result may also carry a non-fatal
/// <see cref="FailureReason"/> diagnostic (e.g. a discarded Treasure_Room, R4.7).
/// </summary>
/// <remarks>Feature: procedural-stage-room-generation. Requirements: 1.3, 2.3, 4.7.</remarks>
public readonly struct StageGenerationResult
{
    private StageGenerationResult(bool success, RoomGraph graph, string failureReason)
    {
        Success = success;
        Graph = graph;
        FailureReason = failureReason;
    }

    /// <summary>True when generation succeeded and <see cref="Graph"/> is valid.</summary>
    public bool Success { get; }

    /// <summary>The generated graph; valid only when <see cref="Success"/> is true.</summary>
    public RoomGraph Graph { get; }

    /// <summary>
    /// The reason generation failed; set only when <see cref="Success"/> is false. May also
    /// carry a non-fatal diagnostic on a successful result (e.g. a discarded Treasure_Room,
    /// R4.7), in which case <see cref="Success"/> is still true.
    /// </summary>
    public string FailureReason { get; }

    /// <summary>
    /// Creates a successful result wrapping the generated <paramref name="graph"/>.
    /// </summary>
    /// <param name="graph">The generated <see cref="RoomGraph"/>. Must not be null.</param>
    /// <returns>A result with <see cref="Success"/> = true.</returns>
    public static StageGenerationResult Ok(RoomGraph graph)
    {
        return new StageGenerationResult(true, graph, null);
    }

    /// <summary>
    /// Creates a successful result wrapping <paramref name="graph"/> plus a non-fatal
    /// <paramref name="diagnostic"/> (e.g. a discarded Treasure_Room, R4.7).
    /// </summary>
    /// <param name="graph">The generated <see cref="RoomGraph"/>. Must not be null.</param>
    /// <param name="diagnostic">A human-readable non-fatal note about the generation.</param>
    /// <returns>A result with <see cref="Success"/> = true carrying the diagnostic.</returns>
    public static StageGenerationResult Ok(RoomGraph graph, string diagnostic)
    {
        return new StageGenerationResult(true, graph, diagnostic);
    }

    /// <summary>
    /// Creates a failed result carrying <paramref name="reason"/> and no graph.
    /// </summary>
    /// <param name="reason">A human-readable explanation for the failure.</param>
    /// <returns>A result with <see cref="Success"/> = false and no graph.</returns>
    public static StageGenerationResult Fail(string reason)
    {
        return new StageGenerationResult(false, null, reason);
    }
}
