using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure, scene-free selection math for weapon pedestals (Hades-style proximity cards).
///
/// Given the player's position and a set of pedestal anchors, each with its own proximity
/// radius, this picks the single pedestal the proximity card should attach to: the one whose
/// anchor is closest to the player AND within that pedestal's radius. When the player is outside
/// every pedestal's radius, no pedestal is selected (the card stays hidden).
///
/// This deliberately holds no Unity scene dependency (only <see cref="Vector3"/>, a struct used as
/// plain math) so it can be exercised in EditMode without instantiating a scene, and consumed by
/// the thin <c>LobbyArsenal</c> MonoBehaviour at runtime.
/// </summary>
public static class PedestalProximity
{
    /// <summary>Sentinel returned when the player is outside all pedestal radii.</summary>
    public const int None = -1;

    /// <summary>
    /// Returns the index of the nearest pedestal whose anchor lies within its own radius, or
    /// <see cref="None"/> when the player is outside every radius. Ties (equal distances) resolve
    /// to the lowest index so selection is deterministic. Non-positive radii and null inputs never
    /// select a pedestal.
    /// </summary>
    /// <param name="playerPosition">World position of the player.</param>
    /// <param name="anchors">Pedestal anchor world positions, parallel to <paramref name="radii"/>.</param>
    /// <param name="radii">Per-pedestal proximity radii, parallel to <paramref name="anchors"/>.</param>
    public static int SelectNearest(Vector3 playerPosition, IReadOnlyList<Vector3> anchors, IReadOnlyList<float> radii)
    {
        if (anchors == null || radii == null) return None;

        int best = None;
        float bestSqrDistance = float.PositiveInfinity;
        int count = Mathf.Min(anchors.Count, radii.Count);

        for (int i = 0; i < count; i++)
        {
            float radius = radii[i];
            if (radius <= 0f) continue; // a pedestal with no reach can never be selected

            float sqrDistance = (anchors[i] - playerPosition).sqrMagnitude;
            if (sqrDistance > radius * radius) continue; // player is outside this pedestal's reach

            // Strictly-closer keeps the lowest index on ties, making the result deterministic.
            if (sqrDistance < bestSqrDistance)
            {
                bestSqrDistance = sqrDistance;
                best = i;
            }
        }

        return best;
    }
}
