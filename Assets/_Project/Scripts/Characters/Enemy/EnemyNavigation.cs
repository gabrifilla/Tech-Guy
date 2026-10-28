using UnityEngine;
using UnityEngine.AI;

/// <summary>Validates an enemy's complete route using its agent type, allowed areas and encounter bounds.</summary>
public static class EnemyNavigation
{
    public static bool TryResolveDestination(NavMeshAgent agent, Vector3 desired, float sampleRadius,
        NavMeshPath path, bool hasEncounterBounds, Bounds encounterBounds, out Vector3 destination)
    {
        destination = default;
        if (agent == null || !agent.enabled || !agent.isOnNavMesh || path == null || !IsFinite(desired)) return false;
        if (hasEncounterBounds && !ContainsPlanar(encounterBounds, desired)) return false;
        if (float.IsNaN(sampleRadius) || float.IsInfinity(sampleRadius)) sampleRadius = .5f;
        var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
        if (!NavMesh.SamplePosition(desired, out NavMeshHit hit, Mathf.Max(.05f, sampleRadius), filter)) return false;
        if (hasEncounterBounds && !ContainsPlanar(encounterBounds, hit.position)) return false;
        if (!agent.CalculatePath(hit.position, path) || path.status != NavMeshPathStatus.PathComplete) return false;
        if (hasEncounterBounds)
        {
            foreach (Vector3 corner in path.corners)
                if (!ContainsPlanar(encounterBounds, corner)) return false;
        }
        destination = hit.position;
        return true;
    }

    public static bool ContainsPlanar(Bounds bounds, Vector3 point) => IsFinite(point) &&
        point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z && point.z <= bounds.max.z;
    public static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
