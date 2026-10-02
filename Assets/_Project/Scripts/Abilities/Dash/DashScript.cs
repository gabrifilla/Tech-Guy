using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[CreateAssetMenu(menuName = "Abilities/Dash")]
public class DashScript : Ability
{
    public float dashVelocity = 8f;
    public float dashTime = 0.2f;
    public string dashAnimation = "Dash";

    // Janela_de_i-frames (R6.7): the window during which a dashing player is immune to damage, a
    // parameter INDEPENDENT of the displacement duration (dashTime). It may be shorter than, equal
    // to, or longer than dashTime — dash without i-frames (0), with partial i-frames, or with
    // i-frames covering the whole displacement are all valid. The initial 0.2 s is NOT a gameplay
    // contract (R6.7), only a sensible default; designers tune it per asset. Serialized so Unity
    // persists the authored value; existing dashVelocity/dashTime fields are untouched so the asset
    // GUID and prior serialized data are preserved.
    [Header("I-frames (independent of displacement — R6.7)")]
    [SerializeField, Min(0f)] private float _iframeWindow = 0.2f;

    /// <summary>
    /// The configured Janela_de_i-frames in seconds (R6.7), independent of <see cref="dashTime"/>.
    /// A value of <c>0</c> means the dash grants no immunity.
    /// </summary>
    public float IframeWindow => Mathf.Max(0f, _iframeWindow);

    private readonly HashSet<GameObject> activeDashOwners = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, float> nextReadyTimes = new Dictionary<GameObject, float>();

    private void OnEnable()
    {
        activeDashOwners.Clear();
        nextReadyTimes.Clear();
    }

    public override bool CanActivate(GameObject parent) => parent && IsReady(parent) &&
        !IsDashing(parent) && Camera.main && parent.GetComponent<AbilityHolder>() &&
        !parent.GetComponent<AbilityHolder>().IsCasting;

    public float GetRemainingCooldown(GameObject parent) => parent && nextReadyTimes.TryGetValue(parent, out float ready)
        ? Mathf.Max(0f, ready - Time.time) : 0f;

    public override void Activate(GameObject parent)
    {
        if (parent == null || IsDashing(parent) || !IsReady(parent)) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 dashDirection = ResolveDashDirection(parent.transform, mainCamera, parent.transform.forward);
        if (dashDirection.sqrMagnitude <= Mathf.Epsilon) return;

        parent.transform.rotation = Quaternion.LookRotation(dashDirection);

        MonoBehaviour runner = parent.GetComponent<AbilityHolder>();
        if (runner == null)
        {
            runner = parent.GetComponent<MonoBehaviour>();
        }

        if (runner != null)
        {
            nextReadyTimes[parent] = Time.time + GetCooldownDuration(parent);
            runner.StartCoroutine(Dash(parent, dashDirection));
        }
    }

    public bool IsDashing(GameObject parent)
    {
        return parent != null && activeDashOwners.Contains(parent);
    }

    private bool IsReady(GameObject parent)
    {
        if (parent == null) return false;

        return !nextReadyTimes.TryGetValue(parent, out float nextReadyTime) || Time.time >= nextReadyTime;
    }

    private float GetCooldownDuration(GameObject parent)
    {
        float cooldownMultiplier = 1f;
        if (parent.TryGetComponent(out PlayerActor playerActor))
        {
            cooldownMultiplier = playerActor.Stats.CooldownMultiplier;
        }

        return Mathf.Max(0f, cooldownTime * cooldownMultiplier);
    }

    private static Vector3 ResolveDashDirection(Transform parentTransform, Camera mainCamera, Vector3 fallbackDirection)
    {
        Ray ray = mainCamera.ScreenPointToRay(GetMousePosition());
        Plane groundPlane = new Plane(Vector3.up, parentTransform.position);
        if (!groundPlane.Raycast(ray, out float distance))
        {
            return Flatten(fallbackDirection).normalized;
        }

        Vector3 targetPoint = ray.GetPoint(distance);
        Vector3 dashDirection = targetPoint - parentTransform.position;
        dashDirection.y = 0f;

        if (dashDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            dashDirection = Flatten(fallbackDirection);
        }

        return dashDirection.normalized;
    }

    private static Vector3 Flatten(Vector3 direction)
    {
        direction.y = 0f;
        return direction;
    }

    private static Vector3 GetMousePosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
#endif
        return Input.mousePosition;
    }

    private IEnumerator Dash(GameObject parent, Vector3 dashDirection)
    {
        activeDashOwners.Add(parent);

        // Arm the i-frame window up front, keyed to this DashScript as the immunity source (R6.9).
        // Its lifetime is tracked by iframeEndTime and is INDEPENDENT of the displacement end
        // (endTime below): the window may elapse before, with, or after the movement (R6.7). The
        // immunity is removed the moment the window elapses, and the finally block guarantees it is
        // also removed on cancel/death/room-change/end-of-displacement so no residue can linger
        // (R6.8). A zero IframeWindow grants no immunity at all.
        Actor dashingActor = parent.GetComponent<Actor>();
        bool immunityArmed = false;
        float iframeEndTime = Time.time + IframeWindow;
        if (dashingActor != null && IframeWindow > 0f)
        {
            dashingActor.AddDamageImmunity(this);
            immunityArmed = true;
        }

        try
        {
            Animator animator = parent.GetComponent<Animator>();
            if (animator != null && !string.IsNullOrWhiteSpace(dashAnimation))
            {
                animator.Play(dashAnimation);
            }

            NavMeshAgent agent = parent.GetComponent<NavMeshAgent>();
            bool hadAgent = agent != null && agent.enabled;
            if (hadAgent)
            {
                agent.ResetPath();
            }

            float endTime = Time.time + Mathf.Max(0f, dashTime);
            while (Time.time < endTime)
            {
                if (parent == null) break;

                // End the i-frame window exactly when it elapses, independent of the displacement
                // (R6.7/R6.8). If IframeWindow < dashTime the player loses immunity mid-dash; if it is
                // longer the window continues past the movement and is released by the finally block
                // when the loop ends.
                if (immunityArmed && Time.time >= iframeEndTime)
                {
                    dashingActor.RemoveDamageImmunity(this);
                    immunityArmed = false;
                }

                Vector3 step = dashDirection * dashVelocity * Time.deltaTime;
                if (hadAgent && agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    MoveAgentOnNavMesh(agent, step);
                }
                else
                {
                    parent.transform.position += step;
                }

                yield return null;
            }

            if (hadAgent)
            {
                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.ResetPath();
                }
            }
        }
        finally
        {
            // Single cleanup for every exit path — normal end-of-displacement, StopCoroutine on a
            // cancel, player death, or room change (which destroys/disables the dash runner and stops
            // this coroutine). Removing immunity here guarantees no residual i-frames outlive the dash
            // (R6.8). dashingActor may be destroyed on death/room-change; the null guard keeps the
            // cleanup safe.
            if (immunityArmed && dashingActor != null) dashingActor.RemoveDamageImmunity(this);
            activeDashOwners.Remove(parent);
        }
    }

    private static void MoveAgentOnNavMesh(NavMeshAgent agent, Vector3 step)
    {
        Vector3 targetPosition = agent.transform.position + step;
        // Sampling the far side of a carved room door can jump across it.
        if (agent.Raycast(targetPosition, out NavMeshHit edge)) targetPosition = edge.position;
        agent.Move(targetPosition - agent.transform.position);
    }
}
