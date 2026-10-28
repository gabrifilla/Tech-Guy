using UnityEngine;
using UnityEngine.AI;

/// <summary>One position writer for a direct-control player; action owners retain their own clocks.</summary>
internal enum PlayerMovementSource { Locomotion, Action, Dash, ExternalPull, Reaction, Boon }

internal sealed class PlayerNavigationMotor
{
    private readonly NavMeshAgent _agent;
    private readonly Transform _owner;
    private readonly RaycastHit[] _hits = new RaycastHit[32];
    private const float Skin = .02f;

    internal PlayerNavigationMotor(NavMeshAgent agent) { _agent = agent; _owner = agent.transform; }

    // Sliding is locomotion-only: authored traversals keep their original direction and distance budget.
    internal void Move(Vector3 offset, bool ignoreActors, bool synchronizePose, bool slide = false)
    {
        if (!_agent || !_agent.enabled || !_agent.isOnNavMesh || !GroundMovementBuffer.IsFinite(offset)) return;
        offset.y = 0;
        for (int step = 0; step < (slide ? 3 : 1); step++)
        {
            float length = offset.magnitude;
            if (length < .00001f) break;
            Vector3 feet = _agent.nextPosition, normal = Vector3.zero;
            float radius = _agent.radius, allowed = length;
            int count = Physics.CapsuleCastNonAlloc(feet + Vector3.up * (radius + Skin),
                feet + Vector3.up * Mathf.Max(radius + Skin, _agent.height - radius), radius,
                offset / length, _hits, length + Skin, Physics.AllLayers, QueryTriggerInteraction.Ignore);
            if (count == _hits.Length) break; // Fail closed if the contact buffer is saturated.
            for (int i = 0; i < count; i++)
            {
                var collider = _hits[i].collider;
                if (collider.transform.IsChildOf(_owner) || ignoreActors && collider.GetComponentInParent<Actor>()) continue;
                if (Vector3.Dot(_hits[i].normal, Vector3.up) >= .7f) continue;
                float distance = Mathf.Max(0, _hits[i].distance - Skin);
                if (distance < allowed) { allowed = distance; normal = _hits[i].normal; }
            }
            if (_agent.Raycast(feet + offset, out NavMeshHit edge))
            {
                float distance = Mathf.Max(0, Vector3.ProjectOnPlane(edge.position - feet, Vector3.up).magnitude - Skin);
                if (distance < allowed) { allowed = distance; normal = edge.normal; }
            }
            Vector3 accepted = offset * (allowed / length);
            if (accepted.sqrMagnitude > .00000001f) _agent.Move(accepted);
            if (!slide || allowed >= length) break;
            normal.y = 0;
            if (normal.sqrMagnitude < .0001f) break;
            normal.Normalize();
            // Do not normalize the residual: sliding never creates extra speed.
            offset = Vector3.ProjectOnPlane(offset - accepted, normal);
        }
        // The only pose synchronization remains inside this authoritative writer.
        if (synchronizePose && _agent.updatePosition) _owner.position = _agent.nextPosition;
    }

    // False means the caller retains its unchanged legacy/NPC movement path.
    internal static bool TryMove(NavMeshAgent agent, Vector3 offset, bool ignoreActors = true, PlayerMovementSource source = PlayerMovementSource.Action, bool synchronizePose = false)
    {
        if (!agent || !agent.TryGetComponent<CharControlScript>(out var control) || !control.DirectMovementEnabled) return false;
        control.MoveThroughDirectMotor(offset, ignoreActors, source, synchronizePose);
        return true;
    }
}
