using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Data-only mapping from <see cref="ArchetypeId"/> to the combat distance each archetype tries to
/// hold and the collision-separation radius it keeps from its neighbors. Consumed by
/// <see cref="PreferredDistanceLayer"/> (through <see cref="PreferredDistanceResolver"/>) so preferred
/// distances stay authorable data rather than code (R6.1). Archetypes with no entry fall back to the
/// <see cref="CombatRole"/> default (R6.4); no per-archetype branching lives in gameplay code.
/// </summary>
/// <remarks>Feature: weapon-gameplay-swarm-rework, task 7.1. Requirements: 6.1, 6.2, 6.4.</remarks>
[CreateAssetMenu(menuName = "Tech Guy/Enemies/Preferred Distance Profile")]
public sealed class PreferredDistanceProfile : ScriptableObject
{
    /// <summary>
    /// One archetype's declared preferred distance and separation radius. <see cref="PreferredDistance"/>
    /// is clamped to be strictly &gt; 0 (R6.1); <see cref="SeparationRadius"/> is clamped non-negative.
    /// </summary>
    [System.Serializable]
    public struct Entry
    {
        [Tooltip("Which archetype this entry configures.")]
        public ArchetypeId archetype;

        [Tooltip("Combat distance (meters) this archetype tries to hold from the player. Must be > 0.")]
        [Min(PreferredDistanceResolver.MinPreferredDistance)]
        public float preferredDistance;

        [Tooltip("Radius (meters) kept from other enemies so collision volumes don't overlap.")]
        [Min(0f)]
        public float separationRadius;
    }

    [Tooltip("Per-archetype preferred distances. Archetypes not listed fall back to their CombatRole default.")]
    [SerializeField] private Entry[] _entries = System.Array.Empty<Entry>();

    /// <summary>
    /// Attempts to read the declared preferred distance and separation radius for
    /// <paramref name="archetype"/>. Returns false when no entry exists so the caller applies the
    /// <see cref="CombatRole"/> fallback and logs the missing configuration (R6.4). On success the
    /// returned <paramref name="preferredDistance"/> is guaranteed &gt; 0 (R6.1) and
    /// <paramref name="separationRadius"/> is non-negative, regardless of authored values.
    /// </summary>
    public bool TryGetPreferredDistance(ArchetypeId archetype, out float preferredDistance, out float separationRadius)
    {
        if (_entries != null)
        {
            for (int i = 0; i < _entries.Length; i++)
            {
                if (_entries[i].archetype == archetype)
                {
                    preferredDistance = Mathf.Max(PreferredDistanceResolver.MinPreferredDistance, _entries[i].preferredDistance);
                    separationRadius = Mathf.Max(0f, _entries[i].separationRadius);
                    return true;
                }
            }
        }

        preferredDistance = 0f;
        separationRadius = 0f;
        return false;
    }

    /// <summary>Read-only view of the authored entries, for inspection and tests.</summary>
    public IReadOnlyList<Entry> Entries => _entries;
}
