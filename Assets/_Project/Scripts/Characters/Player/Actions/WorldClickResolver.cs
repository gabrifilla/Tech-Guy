using System;
using UnityEngine;

/// <summary>Resolves a world click without selecting the player's body or equipment.</summary>
public static class WorldClickResolver
{
    public static bool TryResolve(Ray ray, Transform player, int movementMask,
        out RaycastHit result, out bool crossedPlayer)
    {
        result = default;
        crossedPlayer = false;
        var hits = Physics.RaycastAll(ray, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            Transform hitTransform = hit.collider.transform;
            if (hitTransform == player || hitTransform.IsChildOf(player))
            { crossedPlayer = true; continue; }

            var interactable = hit.collider.GetComponentInParent<Interactable>();
            if (hit.collider.isTrigger && !interactable) continue;
            // A solid obstacle still occludes targets behind it, even on a non-walkable layer.
            if (!interactable && (movementMask & (1 << hit.collider.gameObject.layer)) == 0) return false;
            result = hit;
            return true;
        }
        return false;
    }

    public static bool IsNearPlayer(Vector3 destination, Vector3 playerPosition, float deadZone)
    {
        Vector3 offset = destination - playerPosition;
        offset.y = 0;
        return offset.sqrMagnitude <= deadZone * deadZone;
    }
}
