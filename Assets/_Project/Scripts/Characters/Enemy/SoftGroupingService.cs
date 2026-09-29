using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Thin MonoBehaviour wrapper for the soft-grouping behavior (feature weapon-gameplay-swarm-rework,
/// Requirement 7). It owns no rules: it delegates the bounded displacement math to the pure
/// <see cref="SoftGroupingCalculator"/> and then applies the returned delta through the enemy's
/// existing locomotion means (NavMeshAgent / CharacterController), exactly as
/// <c>CombatReactionController.MoveStep</c> does.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 8.1.
/// Requirements: 7.1, 7.5.
/// Keeping this MonoBehaviour thin (per AGENTS.md) is what lets the R7.1 caps and the Bow no-op
/// (R7.5) be verified in EditMode without a live scene. This wrapper never runs per-frame logic in
/// <c>Update</c>; the swarm coordinator (task 13.1) drives it via <see cref="ApplyGrouping"/>.
/// </remarks>
[DisallowMultipleComponent]
public sealed class SoftGroupingService : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("R7.1 caps: max speed ≤ 1.0 m/s, radius 3.0 m, ≤ 0.5 m per application.")]
    [SerializeField] private SoftGroupingConfig config = new SoftGroupingConfig();

    [Header("Locomotion (existing means; auto-resolved when missing)")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private CharacterController characterController;

    /// <summary>The grouping caps this service applies. Never null.</summary>
    public SoftGroupingConfig Config => config ??= new SoftGroupingConfig();

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!characterController) characterController = GetComponent<CharacterController>();
    }

    private void OnValidate()
    {
        config?.OnValidate();
    }

    /// <summary>
    /// Applies a single soft-grouping step toward <paramref name="groupPoint"/> for the currently
    /// equipped weapon, using <see cref="Time.deltaTime"/>. No-op when the Bow is equipped (R7.5).
    /// </summary>
    /// <param name="equipped">Family of the currently equipped weapon.</param>
    /// <param name="groupPoint">World-space centre of the group to nudge this enemy toward.</param>
    /// <returns>The displacement actually applied this step (zero when suppressed).</returns>
    public Vector3 ApplyGrouping(RunWeaponFamily equipped, Vector3 groupPoint)
        => ApplyGrouping(equipped, groupPoint, Time.deltaTime);

    /// <summary>
    /// Applies a single soft-grouping step toward <paramref name="groupPoint"/> for the currently
    /// equipped weapon over <paramref name="deltaTime"/>. The bounded delta comes from
    /// <see cref="SoftGroupingCalculator.ComputeDisplacement"/> and is moved through the existing
    /// locomotion means. No-op when the Bow is equipped (R7.5).
    /// </summary>
    /// <param name="equipped">Family of the currently equipped weapon.</param>
    /// <param name="groupPoint">World-space centre of the group to nudge this enemy toward.</param>
    /// <param name="deltaTime">Elapsed time for this application, in seconds.</param>
    /// <returns>The displacement actually applied this step (zero when suppressed).</returns>
    public Vector3 ApplyGrouping(RunWeaponFamily equipped, Vector3 groupPoint, float deltaTime)
    {
        Vector3 delta = SoftGroupingCalculator.ComputeDisplacement(
            equipped, transform.position, groupPoint, deltaTime, Config);
        if (delta == Vector3.zero) return Vector3.zero;

        MoveStep(delta);
        return delta;
    }

    /// <summary>
    /// Applies an externally-computed, already-bounded displacement through the enemy's existing
    /// locomotion means (the same NavMeshAgent / CharacterController path soft-grouping uses). This
    /// lets other bounded-displacement tools — the Lança's limited sweep push (R7.4) — reuse the one
    /// locomotion channel instead of opening a parallel one. The caller owns the caps; this method
    /// only routes the delta and returns what it moved (zero for a zero delta).
    /// </summary>
    /// <param name="delta">World-space displacement, already clamped to its own caps by the caller.</param>
    /// <returns>The displacement actually applied (zero when the delta is zero).</returns>
    public Vector3 ApplyExternalDisplacement(Vector3 delta)
    {
        if (delta == Vector3.zero) return Vector3.zero;
        MoveStep(delta);
        return delta;
    }

    // Moves through the enemy's existing locomotion, mirroring CombatReactionController.MoveStep so
    // the swarm systems never duplicate locomotion (design: "movimento pelos meios de locomoção
    // existentes NavMeshAgent/CharacterController").
    private void MoveStep(Vector3 delta)
    {
        if (agent && agent.enabled && agent.isOnNavMesh) { agent.Move(delta); return; }
        if (characterController && characterController.enabled) { characterController.Move(delta); return; }
        transform.position += delta;
    }
}
