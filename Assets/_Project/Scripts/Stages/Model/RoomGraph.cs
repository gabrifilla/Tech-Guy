using System.Collections.Generic;

/// <summary>
/// Pure data model for a generated Stage layout: the set of <see cref="Room"/>s and the
/// <see cref="RoomConnection"/>s that link them into a connected, navigable graph, plus the
/// identifiers of the Start_Room and Boss_Room.
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 1.3, 2.3.
/// This type carries no <see cref="UnityEngine.GameObject"/> or scene state; it is produced by the
/// pure <c>StageGenerator</c> core and consumed by the scene orchestration layer. It is comparable via
/// <see cref="StructurallyEquals"/> so determinism (same Run_Seed + params =&gt; identical graph) can be
/// verified in EditMode without opening a scene.
/// </remarks>
public sealed class RoomGraph
{
    private readonly List<Room> _rooms;
    private readonly List<RoomConnection> _connections;

    /// <summary>Creates a graph from the generated rooms and connections and the special room ids.</summary>
    /// <param name="rooms">Every <see cref="Room"/> in the Stage. Must not be null.</param>
    /// <param name="connections">Every <see cref="RoomConnection"/> in the Stage. Must not be null.</param>
    /// <param name="startRoomId">The <see cref="Room.Id"/> of the Start_Room.</param>
    /// <param name="bossRoomId">The <see cref="Room.Id"/> of the Boss_Room.</param>
    public RoomGraph(IEnumerable<Room> rooms, IEnumerable<RoomConnection> connections, int startRoomId, int bossRoomId)
    {
        _rooms = rooms != null ? new List<Room>(rooms) : new List<Room>();
        _connections = connections != null ? new List<RoomConnection>(connections) : new List<RoomConnection>();
        StartRoomId = startRoomId;
        BossRoomId = bossRoomId;
    }

    /// <summary>Every room in the Stage, in generation order.</summary>
    public IReadOnlyList<Room> Rooms => _rooms;

    /// <summary>Every bidirectional connection between adjacent rooms.</summary>
    public IReadOnlyList<RoomConnection> Connections => _connections;

    /// <summary>The stable id of the single Start_Room.</summary>
    public int StartRoomId { get; }

    /// <summary>The stable id of the single Boss_Room.</summary>
    public int BossRoomId { get; }

    /// <summary>
    /// Compares this graph to <paramref name="other"/> for structural (seed-reproducible) equality.
    /// Two graphs are structurally equal when they have the same number of Rooms, the same set of Room IDs,
    /// the same set of Room_Connections, the same Room_Type per Room, and the same Room_Composition per Room.
    /// Runtime flags (Visited/Cleared/Revealed/Open) are intentionally excluded from the comparison.
    /// </summary>
    /// <remarks>Feature: procedural-stage-room-generation. Requirements: 1.3, 2.3.</remarks>
    /// <param name="other">The graph to compare against. A null graph is never structurally equal.</param>
    /// <returns><c>true</c> when the two graphs are structurally identical; otherwise <c>false</c>.</returns>
    public bool StructurallyEquals(RoomGraph other)
    {
        if (other == null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        if (StartRoomId != other.StartRoomId || BossRoomId != other.BossRoomId)
        {
            return false;
        }

        // Number of Rooms.
        if (_rooms.Count != other._rooms.Count)
        {
            return false;
        }

        // Room IDs, Room_Types and Room_Composition, matched by stable Room ID (order-independent).
        var otherRoomsById = new Dictionary<int, Room>(other._rooms.Count);
        foreach (Room room in other._rooms)
        {
            // Duplicate ids would make matching ambiguous: treat as not equal.
            if (otherRoomsById.ContainsKey(room.Id))
            {
                return false;
            }

            otherRoomsById.Add(room.Id, room);
        }

        foreach (Room room in _rooms)
        {
            if (!otherRoomsById.TryGetValue(room.Id, out Room otherRoom))
            {
                return false;
            }

            if (room.Type != otherRoom.Type)
            {
                return false;
            }

            if (!CompositionsEqual(room.Composition, otherRoom.Composition))
            {
                return false;
            }
        }

        // Room_Connections: same count and same set (order-independent).
        return ConnectionsEqual(_connections, other._connections);
    }

    private static bool ConnectionsEqual(List<RoomConnection> a, List<RoomConnection> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        // Multiset comparison: every connection in A must be matched by a distinct connection in B.
        var matched = new bool[b.Count];
        foreach (RoomConnection connection in a)
        {
            bool found = false;
            for (int i = 0; i < b.Count; i++)
            {
                if (matched[i])
                {
                    continue;
                }

                if (ConnectionEquals(connection, b[i]))
                {
                    matched[i] = true;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                return false;
            }
        }

        return true;
    }

    private static bool ConnectionEquals(RoomConnection a, RoomConnection b)
    {
        if (a == null || b == null)
        {
            return ReferenceEquals(a, b);
        }

        // Structural identity of a connection: which rooms it links, from which side, and whether it is hidden.
        // Endpoints are treated as unordered because the connection is bidirectional.
        bool sameEndpoints =
            (a.RoomAId == b.RoomAId && a.RoomBId == b.RoomBId) ||
            (a.RoomAId == b.RoomBId && a.RoomBId == b.RoomAId);

        return sameEndpoints && a.SideFromA == b.SideFromA && a.Hidden == b.Hidden;
    }

    private static bool CompositionsEqual(RoomComposition a, RoomComposition b)
    {
        if (a == null || b == null)
        {
            return ReferenceEquals(a, b);
        }

        if (a.TargetDensity != b.TargetDensity || a.DistinctArchetypes != b.DistinctArchetypes)
        {
            return false;
        }

        IReadOnlyList<ArchetypeSlot> slotsA = a.Slots;
        IReadOnlyList<ArchetypeSlot> slotsB = b.Slots;
        int countA = slotsA != null ? slotsA.Count : 0;
        int countB = slotsB != null ? slotsB.Count : 0;
        if (countA != countB)
        {
            return false;
        }

        // Slot order is deterministic for a given seed, so a positional comparison is sufficient
        // and cheaper than a multiset match.
        for (int i = 0; i < countA; i++)
        {
            if (!SlotEquals(slotsA[i], slotsB[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool SlotEquals(ArchetypeSlot a, ArchetypeSlot b)
    {
        return a.ArchetypeId == b.ArchetypeId && a.Count == b.Count;
    }
}
