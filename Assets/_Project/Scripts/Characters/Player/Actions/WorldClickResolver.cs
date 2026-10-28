using System;
using UnityEngine;

/// <summary>Resolves a world click without selecting the player's body or equipment.</summary>
public static class WorldClickResolver
{
    /// <summary>
    /// Gives an enemy a small screen-space picking margin. Direct hits always win;
    /// every sample still uses the normal scenery/occlusion rules.
    /// </summary>
    public static bool TryResolveWithEnemyAssist(Camera camera, Vector2 pointer, Transform player,
        int movementMask, out RaycastHit result, out bool crossedPlayer, float pointerRadiusPixels = 18f,
        TechGuy.Cameras.TG_TopDown_Camera occlusionCamera = null)
    {
        result = default;
        crossedPlayer = false;
        if (!camera) return false;
        bool resolved = TryResolve(camera.ScreenPointToRay(pointer), player, movementMask,
            out result, out crossedPlayer, occlusionCamera);
        // Wall faces, interactions and clicks on the avatar keep their original meaning.
        if (!resolved || crossedPlayer || result.collider.GetComponentInParent<Interactable>()
            || Vector3.Dot(result.normal, Vector3.up) < .5f) return resolved;
        if (float.IsNaN(pointerRadiusPixels) || float.IsInfinity(pointerRadiusPixels)) return resolved;
        float radius = Mathf.Clamp(pointerRadiusPixels, 0f, 40f) * camera.pixelHeight / 1080f;
        if (radius <= 0f) return resolved;
        float bestScore = float.PositiveInfinity;
        for (int ring = 1; ring <= 2; ring++)
        {
            for (int sample = 0; sample < 8; sample++)
            {
                float angle = sample * Mathf.PI / 4f;
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (radius * ring * .5f);
                if (!camera.pixelRect.Contains(pointer + offset)
                    || !TryResolve(camera.ScreenPointToRay(pointer + offset), player, movementMask,
                    out RaycastHit candidate, out _, occlusionCamera)) continue;
                var interactable = candidate.collider.GetComponentInParent<Interactable>();
                if (!interactable || interactable.interactionType != InteractableType.Enemy) continue;
                Actor actor = interactable.myActor ? interactable.myActor : interactable.GetComponentInParent<Actor>();
                if (!actor || actor.IsDead || !actor.isActiveAndEnabled
                    || actor.transform == player || actor.transform.IsChildOf(player)) continue;
                float score = offset.sqrMagnitude + candidate.distance * .0001f;
                if (score >= bestScore) continue;
                bestScore = score;
                result = candidate;
            }
        }
        return resolved;
    }

    public static bool TryResolve(Ray ray, Transform player, int movementMask,
        out RaycastHit result, out bool crossedPlayer,
        TechGuy.Cameras.TG_TopDown_Camera occlusionCamera = null)
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
            // Picking may cross camera-faded scenery. Physical collision and attack LOS still use the collider.
            if (!interactable && occlusionCamera && occlusionCamera.IsOcclusionFaded(hit.collider)) continue;
            // Other solid obstacles still occlude targets behind them, even on a non-walkable layer.
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
