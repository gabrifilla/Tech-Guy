using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class RagdollController : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Rigidbody rootRigidbody;
    [SerializeField] private float defaultRecoverDelay = 1.5f;

    private Rigidbody[] ragdollBodies;
    private Collider[] ragdollColliders;
    private Coroutine recoverCoroutine;

    private void Awake()
    {
        if (!animator)
        {
            animator = GetComponent<Animator>();
        }

        if (!agent)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        rootRigidbody = rootRigidbody ? rootRigidbody : GetComponent<Rigidbody>();
        ragdollBodies = GetComponentsInChildren<Rigidbody>(includeInactive: true);
        ragdollColliders = GetComponentsInChildren<Collider>(includeInactive: true);
        SetRagdollActive(false);
    }

    public void EnableRagdoll(Vector3 force, Vector3 hitPoint, float recoverDelay = -1f)
    {
        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
        }

        if (agent)
        {
            agent.enabled = false;
        }

        if (animator)
        {
            animator.enabled = false;
        }

        SetRagdollActive(true);
        ApplyForce(force, hitPoint);

        float resolvedDelay = recoverDelay >= 0f ? recoverDelay : defaultRecoverDelay;
        if (resolvedDelay > 0f)
        {
            recoverCoroutine = StartCoroutine(RecoverAfter(resolvedDelay));
        }
    }

    public void DisableRagdoll()
    {
        if (recoverCoroutine != null)
        {
            StopCoroutine(recoverCoroutine);
            recoverCoroutine = null;
        }

        SetRagdollActive(false);

        if (animator)
        {
            animator.enabled = true;
        }

        if (agent)
        {
            agent.enabled = true;
            agent.Warp(transform.position);
            agent.ResetPath();
        }
    }

    private IEnumerator RecoverAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        DisableRagdoll();
    }

    private void SetRagdollActive(bool active)
    {
        HashSet<Transform> ragdollTransforms = new HashSet<Transform>();
        foreach (Rigidbody body in ragdollBodies)
        {
            if (!body || body == rootRigidbody) continue;

            ragdollTransforms.Add(body.transform);
            body.isKinematic = !active;
            body.useGravity = active;
        }

        foreach (Collider col in ragdollColliders)
        {
            if (!col) continue;

            if (ragdollTransforms.Contains(col.transform))
            {
                col.enabled = active;
            }
        }
    }

    private void ApplyForce(Vector3 force, Vector3 hitPoint)
    {
        if (force.sqrMagnitude <= Mathf.Epsilon) return;

        Rigidbody closestBody = null;
        float closestDistance = float.PositiveInfinity;
        foreach (Rigidbody body in ragdollBodies)
        {
            if (!body || body == rootRigidbody || body.isKinematic) continue;

            float distance = (body.worldCenterOfMass - hitPoint).sqrMagnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestBody = body;
            }
        }

        if (closestBody)
        {
            closestBody.AddForceAtPosition(force, hitPoint, ForceMode.Impulse);
        }
    }
}
