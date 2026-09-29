using System;
using System.Collections.Generic;

/// <summary>
/// Immutable enemy composition specification for a single Combat_Room: the set of
/// <see cref="ArchetypeSlot"/> entries (each pairing an <see cref="ArchetypeId"/>
/// with a spawn count), the target enemy count for the room and the number of
/// distinct archetypes present. Produced by the pure generation core and consumed
/// at runtime by the resolver; it holds no scene references.
/// </summary>
/// <remarks>Feature: procedural-stage-room-generation. Requirements: 5.1, 5.2, 5.3.</remarks>
public sealed class RoomComposition
{
    private readonly ArchetypeSlot[] _slots;

    /// <summary>
    /// Creates a composition from its archetype slots and the target density.
    /// </summary>
    /// <param name="slots">
    /// Per-archetype spawn slots. Copied defensively so the composition stays
    /// immutable. A null argument is treated as an empty slot set.
    /// </param>
    /// <param name="targetDensity">
    /// The target number of enemies for the room, within the Density_Budget range
    /// (4 to 30) scaled by room depth.
    /// </param>
    public RoomComposition(IReadOnlyList<ArchetypeSlot> slots, int targetDensity)
    {
        if (slots == null || slots.Count == 0)
        {
            _slots = Array.Empty<ArchetypeSlot>();
        }
        else
        {
            _slots = new ArchetypeSlot[slots.Count];
            for (int i = 0; i < slots.Count; i++)
            {
                _slots[i] = slots[i];
            }
        }

        TargetDensity = targetDensity;
    }

    /// <summary>
    /// The per-archetype spawn slots (Archetype_Id + count) that make up this room.
    /// </summary>
    public IReadOnlyList<ArchetypeSlot> Slots => _slots;

    /// <summary>
    /// The target enemy count for the room, within [4, 30] and scaled by depth.
    /// </summary>
    public int TargetDensity { get; }

    /// <summary>
    /// The number of distinct <see cref="ArchetypeId"/> values present across the
    /// slots (the Variety_Target satisfied by this composition, subject to
    /// availability).
    /// </summary>
    public int DistinctArchetypes
    {
        get
        {
            if (_slots.Length == 0)
            {
                return 0;
            }

            var seen = new HashSet<ArchetypeId>();
            for (int i = 0; i < _slots.Length; i++)
            {
                seen.Add(_slots[i].ArchetypeId);
            }

            return seen.Count;
        }
    }
}

/// <summary>
/// A single entry of a <see cref="RoomComposition"/>: an <see cref="ArchetypeId"/>
/// paired with the number of enemies of that archetype to spawn. Value type so
/// compositions remain cheap to copy and compare.
/// </summary>
/// <remarks>Feature: procedural-stage-room-generation. Requirements: 5.1, 5.3.</remarks>
public readonly struct ArchetypeSlot : IEquatable<ArchetypeSlot>
{
    /// <summary>
    /// Creates a slot for the given archetype and count.
    /// </summary>
    /// <param name="archetypeId">The archetype to spawn for this slot.</param>
    /// <param name="count">The number of enemies of this archetype to spawn.</param>
    public ArchetypeSlot(ArchetypeId archetypeId, int count)
    {
        ArchetypeId = archetypeId;
        Count = count;
    }

    /// <summary>The archetype this slot spawns.</summary>
    public ArchetypeId ArchetypeId { get; }

    /// <summary>The number of enemies of <see cref="ArchetypeId"/> to spawn.</summary>
    public int Count { get; }

    public bool Equals(ArchetypeSlot other) => ArchetypeId == other.ArchetypeId && Count == other.Count;

    public override bool Equals(object obj) => obj is ArchetypeSlot other && Equals(other);

    public override int GetHashCode() => ((int)ArchetypeId * 397) ^ Count;
}
