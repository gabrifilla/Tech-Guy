/// <summary>
/// A bidirectional link between two adjacent rooms of the Room_Graph, materialized
/// at runtime by a single pair of Room_Gates (one pair per connection, R1.5).
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 1.3, 4.8.
/// <see cref="Hidden"/> marks a Secret_Room's connection that stays closed until the
/// player performs the Discovery_Action (R4.8). <see cref="Open"/> is mutable runtime
/// passage state and is not part of the structural graph equality (R2.3).
/// </remarks>
public sealed class RoomConnection
{
    /// <summary>Id of the first room of the connection.</summary>
    public int RoomAId { get; }

    /// <summary>Id of the second room of the connection.</summary>
    public int RoomBId { get; }

    /// <summary>The edge of room A on which the door for this connection is born.</summary>
    public Direction SideFromA { get; }

    /// <summary>
    /// True when this connection is hidden (Secret_Room), kept closed until the
    /// Discovery_Action reveals it (R4.8).
    /// </summary>
    public bool Hidden { get; }

    /// <summary>Runtime state of the passage: true when the connection is open.</summary>
    public bool Open { get; set; }

    public RoomConnection(
        int roomAId,
        int roomBId,
        Direction sideFromA,
        bool hidden = false,
        bool open = false)
    {
        RoomAId = roomAId;
        RoomBId = roomBId;
        SideFromA = sideFromA;
        Hidden = hidden;
        Open = open;
    }
}
