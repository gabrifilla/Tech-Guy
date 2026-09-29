/// <summary>
/// Classification of a Room within a generated Stage's Room_Graph.
/// Each Room is assigned exactly one Room_Type.
/// </summary>
/// <remarks>Feature: procedural-stage-room-generation. Requirements: 4.1.</remarks>
public enum RoomType
{
    /// <summary>The room where the player begins the Stage; contains no enemies.</summary>
    Start,

    /// <summary>A room with an enemy composition that must be cleared to grant a reward.</summary>
    Combat,

    /// <summary>The room containing the Stage boss, marked by the Sector_Boss component.</summary>
    Boss,

    /// <summary>A special reward room with no mandatory combat.</summary>
    Treasure,

    /// <summary>A hidden room revealed only after a Discovery_Action; its presence is probabilistic.</summary>
    Secret,
}
