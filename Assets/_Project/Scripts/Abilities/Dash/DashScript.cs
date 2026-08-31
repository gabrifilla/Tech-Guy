using System.Collections;
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
    public bool isDashing = false;

    private void OnEnable()
    {
        isDashing = false;
    }

    public override void Activate(GameObject parent)
    {
        if (isDashing || parent == null) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        Vector3 dashDirection = ResolveDashDirection(parent.transform, mainCamera);
        if (dashDirection.sqrMagnitude <= Mathf.Epsilon) return;

        parent.transform.rotation = Quaternion.LookRotation(dashDirection);

        MonoBehaviour runner = parent.GetComponent<AbilityHolder>();
        if (runner == null)
        {
            runner = parent.GetComponent<MonoBehaviour>();
        }

        if (runner != null)
        {
            runner.StartCoroutine(Dash(parent, dashDirection));
        }
    }

    private static Vector3 ResolveDashDirection(Transform parentTransform, Camera mainCamera)
    {
        Vector3 mousePosition = GetMousePosition();
        Vector3 characterScreenPosition = mainCamera.WorldToScreenPoint(parentTransform.position);

        Vector3 dashDirection = mousePosition - characterScreenPosition;
        dashDirection.z = dashDirection.y;
        dashDirection.y = 0f;
        return dashDirection.normalized;
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
        isDashing = true;

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
            agent.enabled = false;
        }

        float endTime = Time.time + Mathf.Max(0f, dashTime);
        while (Time.time < endTime)
        {
            parent.transform.position += dashDirection * dashVelocity * Time.deltaTime;
            yield return null;
        }

        if (hadAgent)
        {
            agent.enabled = true;
            agent.Warp(parent.transform.position);
            agent.ResetPath();
        }

        isDashing = false;
    }
}
