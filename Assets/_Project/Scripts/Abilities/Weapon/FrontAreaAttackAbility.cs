using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[CreateAssetMenu(menuName = "Abilities/Weapon/Front Area Attack")]
public class FrontAreaAttackAbility : Ability
{
    [Header("Area")]
    [SerializeField] private float rangeOverride = 0f;
    [SerializeField] private Vector3 boxSize = new Vector3(3f, 2f, 0f);
    [SerializeField] private LayerMask targetLayers;

    [Header("Damage")]
    [SerializeField] private float damageMultiplier = 1.5f;
    [SerializeField] private float bonusDamage = 0f;

    [Header("Presentation")]
    [SerializeField] private string animationName = "Attack1";
    [SerializeField] private bool stopMovement = true;
    [SerializeField] private bool faceMousePosition = true;

    public override void Activate(GameObject parent)
    {
        if (parent == null) return;

        PlayerActor playerActor = parent.GetComponent<PlayerActor>();
        if (playerActor == null) return;

        if (stopMovement && parent.TryGetComponent(out NavMeshAgent agent))
        {
            agent.ResetPath();
        }

        if (faceMousePosition)
        {
            FaceMousePosition(parent.transform);
        }

        if (!string.IsNullOrWhiteSpace(animationName) && parent.TryGetComponent(out Animator animator))
        {
            animator.Play(animationName, 0, 0f);
        }

        WeaponScript weapon = playerActor.CurrentWeapon;
        float range = rangeOverride > 0f
            ? rangeOverride
            : weapon != null && weapon.attackDistance > 0f
                ? weapon.attackDistance
                : 2f;

        Vector3 resolvedBoxSize = boxSize != Vector3.zero
            ? boxSize
            : weapon != null
                ? weapon.attackBoxSize
                : Vector3.zero;

        float weaponDamage = weapon != null ? weapon.attackDamage : 0f;
        playerActor.TryApplyAreaDamage(
            parent.transform.position,
            parent.transform.forward,
            range,
            resolvedBoxSize,
            targetLayers,
            weaponDamage,
            damageMultiplier,
            bonusDamage);
    }

    private static void FaceMousePosition(Transform actorTransform)
    {
        Camera camera = Camera.main;
        if (camera == null) return;

        Ray ray = camera.ScreenPointToRay(GetMousePosition());
        Plane groundPlane = new Plane(Vector3.up, actorTransform.position);
        if (!groundPlane.Raycast(ray, out float distance)) return;

        Vector3 targetPoint = ray.GetPoint(distance);
        Vector3 direction = targetPoint - actorTransform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude > Mathf.Epsilon)
        {
            actorTransform.rotation = Quaternion.LookRotation(direction);
        }
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
}
