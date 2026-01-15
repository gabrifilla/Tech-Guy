using System.Collections;
using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[CreateAssetMenu(menuName = "Abilities/Dash")]
public class DashScript : Ability
{
    public float dashVelocity;
    public float dashTime;
    public string dashAnimation = "Dash";
    public bool isDashing = false;

    private void OnEnable()
    {
        isDashing = false;
    }

    public override void Activate(GameObject parent)
    {
        if (isDashing) return;

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Vector3 mousePosition = GetMousePosition();
        Vector3 characterScreenPosition = mainCamera.WorldToScreenPoint(parent.transform.position);

        Vector3 dashDirection = (mousePosition - characterScreenPosition).normalized;
        dashDirection.z = dashDirection.y;
        dashDirection.y = 0;

        Quaternion dashRotation = Quaternion.LookRotation(dashDirection);
        parent.transform.rotation = dashRotation;

        MonoBehaviour runner = parent.GetComponent<AbilityHolder>();
        if (runner == null)
        {
            runner = parent.GetComponent<MonoBehaviour>();
        }

        if (runner == null)
        {
            return;
        }

        runner.StartCoroutine(Dash(parent, dashDirection));
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
        if (animator != null)
        {
            animator.Play(dashAnimation);
        }

        NavMeshAgent agent = parent.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.enabled = false; // Desativa o NavMeshAgent temporariamente
        }

        float startTime = Time.time;
        while (Time.time < startTime + dashTime)
        {
            parent.transform.position += dashDirection * dashVelocity * Time.deltaTime;
            yield return null;
        }

        if (agent != null)
        {
            agent.enabled = true; // Reativa o NavMeshAgent
        }

        isDashing = false;
    }
}
