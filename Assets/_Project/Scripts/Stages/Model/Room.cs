using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A node of the Room_Graph: a single room with a type, a center position and
/// bounds on the XZ plane, a depth relative to the Start_Room, its bidirectional
/// connections and, when applicable, an enemy composition.
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 1.3, 4.8.
/// The runtime flags (<see cref="Visited"/>, <see cref="Cleared"/>,
/// <see cref="Revealed"/>) are mutable state and are intentionally NOT part of the
/// structural graph equality used for seed reproducibility (Requirement 2.3).
/// </remarks>
public sealed class Room
{
    /// <summary>Stable room identifier, deterministic per seed (R1.3/R2.3).</summary>
    public int Id { get; }

    /// <summary>The classification of this room.</summary>
    public RoomType Type { get; }

    /// <summary>Center position on the XZ plane.</summary>
    public Vector2 Center { get; }

    /// <summary>Room_Size bounds (width/depth on the XZ plane).</summary>
    public Vector2 Size { get; }

    /// <summary>
    /// Distance (number of connections) of the shortest path back to the
    /// Start_Room. Used to scale the Density_Budget (R5.5).
    /// </summary>
    public int Depth { get; }

    /// <summary>The connections that link this room to its neighbors.</summary>
    public List<RoomConnection> Connections { get; }

    /// <summary>
    /// Enemy composition for a Combat_Room. Null for Start/Boss/Treasure/Secret
    /// rooms that carry no mandatory combat.
    /// </summary>
    public RoomComposition Composition { get; }

    /// <summary>Runtime flag: player has entered this room at least once.</summary>
    public bool Visited { get; set; }

    /// <summary>Runtime flag: this room's combat has been resolved (R3.3 idempotency).</summary>
    public bool Cleared { get; set; }

    /// <summary>Runtime flag: a Secret_Room has been revealed (R4.8/R4.9).</summary>
    public bool Revealed { get; set; }

    public Room(
        int id,
        RoomType type,
        Vector2 center,
        Vector2 size,
        int depth,
        List<RoomConnection> connections = null,
        RoomComposition composition = null)
    {
        Id = id;
        Type = type;
        Center = center;
        Size = size;
        Depth = depth;
        Connections = connections ?? new List<RoomConnection>();
        Composition = composition;
    }
}
