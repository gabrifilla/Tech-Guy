using System;
using UnityEngine;

/// <summary>
/// Serializable, Inspector-editable configuration that drives one Stage generation:
/// combat-room count bounds, the per-room density budget range, the variety target and
/// the appearance probabilities of the optional Special_Rooms, plus the generation retry
/// limit. It is a plain <c>[Serializable]</c> data class (not a <see cref="MonoBehaviour"/>),
/// so it exposes read-only accessors and a <see cref="Validate"/> method that clamps every
/// range; the owning <see cref="MonoBehaviour"/> should call <see cref="Validate"/> from its
/// own <c>OnValidate</c>.
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 1.1, 4.3, 5.2, 5.3, 1.7.
/// </remarks>
[Serializable]
public sealed class StageGenerationParams
{
    /// <summary>Absolute upper bound on Combat_Rooms enforced by the spec (R1.1).</summary>
    public const int MaxCombatRoomCeiling = 20;

    /// <summary>Lowest allowed value for the density budget bounds (R5.2).</summary>
    public const int DensityBudgetFloor = 4;

    /// <summary>Highest allowed value for the density budget bounds (R5.2).</summary>
    public const int DensityBudgetCeiling = 30;

    /// <summary>Lowest allowed value for the variety target (R5.3).</summary>
    public const int VarietyTargetFloor = 2;

    /// <summary>Highest allowed value for the variety target (R5.3).</summary>
    public const int VarietyTargetCeiling = 6;

    /// <summary>Lowest allowed appearance probability for a Special_Room (R4.3).</summary>
    public const float SpecialProbabilityFloor = 0.05f;

    /// <summary>Highest allowed appearance probability for a Special_Room (R4.3).</summary>
    public const float SpecialProbabilityCeiling = 0.95f;

    [SerializeField, Min(1)] private int _minCombatRooms = 1;
    [SerializeField] private int _maxCombatRooms = 20;
    [SerializeField, Range(DensityBudgetFloor, DensityBudgetCeiling)] private int _densityBudgetMin = 4;
    [SerializeField, Range(DensityBudgetFloor, DensityBudgetCeiling)] private int _densityBudgetMax = 30;
    [SerializeField, Range(VarietyTargetFloor, VarietyTargetCeiling)] private int _varietyTarget = 3;
    [SerializeField, Range(SpecialProbabilityFloor, SpecialProbabilityCeiling)] private float _treasureProbability = 0.35f;
    [SerializeField, Range(SpecialProbabilityFloor, SpecialProbabilityCeiling)] private float _secretProbability = 0.25f;
    [SerializeField, Min(1)] private int _generationRetryLimit = 50;
    [SerializeField] private Vector2 _roomSize = new Vector2(10f, 10f);
    public Vector2 RoomSize => _roomSize;

    /// <summary>Minimum number of Combat_Rooms to generate (at least 1). See R1.1.</summary>
    public int MinCombatRooms => _minCombatRooms;

    /// <summary>Maximum number of Combat_Rooms to generate (clamped to 20). See R1.1.</summary>
    public int MaxCombatRooms => _maxCombatRooms;

    /// <summary>Lower bound of the per-room density budget, within [4, 30]. See R5.2.</summary>
    public int DensityBudgetMin => _densityBudgetMin;

    /// <summary>Upper bound of the per-room density budget, within [4, 30]. See R5.2.</summary>
    public int DensityBudgetMax => _densityBudgetMax;

    /// <summary>Minimum number of distinct archetypes per Combat_Room, within [2, 6]. See R5.3.</summary>
    public int VarietyTarget => _varietyTarget;

    /// <summary>Appearance probability of the Treasure_Room, within [0.05, 0.95]. See R4.3.</summary>
    public float TreasureProbability => _treasureProbability;

    /// <summary>Appearance probability of the Secret_Room, within [0.05, 0.95]. See R4.3.</summary>
    public float SecretProbability => _secretProbability;

    /// <summary>Number of whole-graph generation attempts before aborting (at least 1). See R1.7.</summary>
    public int GenerationRetryLimit => _generationRetryLimit;

    /// <summary>
    /// Clamps every field to its valid range and enforces the cross-field invariants
    /// (<c>_maxCombatRooms</c> in [<c>_minCombatRooms</c>, 20]; <c>_densityBudgetMin &lt;= _densityBudgetMax</c>
    /// within [4, 30]; variety target in [2, 6]; probabilities in [0.05, 0.95]; retry limit &gt;= 1).
    /// Call this from the owning <see cref="MonoBehaviour"/>'s <c>OnValidate</c>. Also aliased as
    /// <see cref="OnValidate"/> for callers that prefer the Unity naming convention.
    /// </summary>
    public void Validate()
    {
        _roomSize = new Vector2(Mathf.Max(10f, _roomSize.x), Mathf.Max(10f, _roomSize.y));
        // Combat-room count: min >= 1, max in [min, ceiling].
        _minCombatRooms = Mathf.Max(1, _minCombatRooms);
        if (_minCombatRooms > MaxCombatRoomCeiling)
        {
            _minCombatRooms = MaxCombatRoomCeiling;
        }

        _maxCombatRooms = Mathf.Clamp(_maxCombatRooms, _minCombatRooms, MaxCombatRoomCeiling);

        // Density budget: each bound within [4, 30], and min <= max.
        _densityBudgetMin = Mathf.Clamp(_densityBudgetMin, DensityBudgetFloor, DensityBudgetCeiling);
        _densityBudgetMax = Mathf.Clamp(_densityBudgetMax, DensityBudgetFloor, DensityBudgetCeiling);
        if (_densityBudgetMax < _densityBudgetMin)
        {
            _densityBudgetMax = _densityBudgetMin;
        }

        // Variety target within [2, 6].
        _varietyTarget = Mathf.Clamp(_varietyTarget, VarietyTargetFloor, VarietyTargetCeiling);

        // Special-room probabilities within [0.05, 0.95].
        _treasureProbability = Mathf.Clamp(_treasureProbability, SpecialProbabilityFloor, SpecialProbabilityCeiling);
        _secretProbability = Mathf.Clamp(_secretProbability, SpecialProbabilityFloor, SpecialProbabilityCeiling);

        // Retry limit: at least 1.
        _generationRetryLimit = Mathf.Max(1, _generationRetryLimit);
    }

    /// <summary>
    /// Unity-style alias for <see cref="Validate"/>. This class is not a
    /// <see cref="MonoBehaviour"/>, so Unity does not invoke this automatically; the owning
    /// component forwards its <c>OnValidate</c> here so the clamps run in the Editor.
    /// </summary>
    public void OnValidate()
    {
        Validate();
    }
}
