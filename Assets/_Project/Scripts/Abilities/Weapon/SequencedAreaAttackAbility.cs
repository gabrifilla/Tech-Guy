using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public abstract class SequencedAreaAttackAbility : Ability
{
    [Header("Presentation")]
    [SerializeField] private string animationName = "Attack1";
    [SerializeField] private bool stopMovement = true;
    [SerializeField] private bool faceMousePosition = true;

    protected void ActivateSequence(GameObject parent, IReadOnlyList<AreaHitStep> hitSteps)
    {
        if (parent == null || hitSteps == null || hitSteps.Count == 0) return;

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

        MonoBehaviour runner = parent.GetComponent<AbilityHolder>();
        if (runner != null)
        {
            runner.StartCoroutine(ApplyHitSteps(parent.transform, playerActor, hitSteps));
            return;
        }

        ApplyDueHitSteps(parent.transform, playerActor, hitSteps, float.PositiveInfinity);
    }

    private static IEnumerator ApplyHitSteps(Transform ownerTransform, PlayerActor playerActor, IReadOnlyList<AreaHitStep> hitSteps)
    {
        float elapsed = 0f;
        bool[] appliedSteps = new bool[hitSteps.Count];
        int appliedCount = 0;

        while (appliedCount < hitSteps.Count)
        {
            elapsed += Time.deltaTime;
            appliedCount += ApplyDueHitSteps(ownerTransform, playerActor, hitSteps, elapsed, appliedSteps);
            yield return null;
        }
    }

    private static int ApplyDueHitSteps(Transform ownerTransform, PlayerActor playerActor, IReadOnlyList<AreaHitStep> hitSteps, float elapsed, bool[] appliedSteps = null)
    {
        if (!ownerTransform || !playerActor) return 0;

        int appliedCount = 0;
        WeaponScript weapon = playerActor.CurrentWeapon;
        for (int i = 0; i < hitSteps.Count; i++)
        {
            if (appliedSteps != null && appliedSteps[i]) continue;

            AreaHitStep hitStep = hitSteps[i];
            if (hitStep == null || elapsed < hitStep.delay) continue;

            if (appliedSteps != null)
            {
                appliedSteps[i] = true;
            }

            ApplyHitStep(ownerTransform, playerActor, weapon, hitStep);
            appliedCount++;
        }

        return appliedCount;
    }

    private static void ApplyHitStep(Transform ownerTransform, PlayerActor playerActor, WeaponScript weapon, AreaHitStep hitStep)
    {
        float range = hitStep.rangeOverride > 0f
            ? hitStep.rangeOverride
            : weapon != null && weapon.attackDistance > 0f
                ? weapon.attackDistance
                : 2f;

        Vector3 boxSize = hitStep.boxSize != Vector3.zero
            ? hitStep.boxSize
            : weapon != null
                ? weapon.attackBoxSize
                : Vector3.zero;

        float weaponDamage = weapon != null ? weapon.attackDamage : 0f;
        Vector3 origin = ownerTransform.TransformPoint(hitStep.localOffset);
        HitReactionRequest reactionRequest = new HitReactionRequest(
            playerActor,
            origin + ownerTransform.forward * Mathf.Max(0f, range),
            ownerTransform.forward,
            hitStep.reactionType,
            hitStep.hitStrength,
            hitStep.poiseDamage,
            hitStep.stunDuration,
            hitStep.knockbackForce,
            hitStep.launchForce,
            hitStep.canAirJuggle,
            hitStep.canRagdoll);

        playerActor.TryApplyAreaDamage(
            origin,
            ownerTransform.forward,
            range,
            boxSize,
            hitStep.hitShape,
            hitStep.sphereRadius,
            hitStep.targetLayers,
            weaponDamage,
            hitStep.damageMultiplier,
            hitStep.bonusDamage,
            reactionRequest);
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
