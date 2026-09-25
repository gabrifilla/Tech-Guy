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

        activeDashOwners.Remove(parent);
    }

    private static void MoveAgentOnNavMesh(NavMeshAgent agent, Vector3 step)
    {
        Vector3 targetPosition = agent.transform.position + step;
        // Sampling the far side of a carved room door can jump across it.
        if (agent.Raycast(targetPosition, out NavMeshHit edge)) targetPosition = edge.position;
        agent.Move(targetPosition - agent.transform.position);
    }
}
