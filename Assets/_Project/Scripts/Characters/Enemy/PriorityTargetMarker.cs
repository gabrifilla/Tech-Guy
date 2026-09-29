using UnityEngine;

/// <summary>
/// World-space, tooltip-free legibility indicator for a priority enemy (Healer, Shield Support,
/// Spawner). It shows two distinct things:
///
/// <list type="bullet">
///   <item><description>
///     A <b>persistent marker</b> — present the whole time the enemy is a priority target and alive,
///     visible without the player selecting, targeting, or hovering the enemy (R21.1, R21.2). It is
///     rendered with a reused <see cref="CombatGroundRing"/> so priority enemies share the project's
///     existing world-space visual language rather than inventing a new one.
///   </description></item>
///   <item><description>
///     A <b>role-action cue</b> — a second, visually distinct ring shown only <i>while</i> the enemy
///     performs its role action (healing / shielding / spawning), toggled by the role behaviors via
///     <see cref="ShowRoleActionCue"/> / <see cref="HideRoleActionCue"/> (R21.3).
///   </description></item>
/// </list>
///
/// Both are removed within the same frame the enemy dies: the marker subscribes to
/// <see cref="Actor.Died"/> and destroys itself immediately, and <see cref="OnDestroy"/> tears down
/// the cue in the same pass (R21.4, R13.8, R14.6, R16.7).
///
/// The MonoBehaviour stays thin: the "is a marker owed at all?" rule lives in the scene-free
/// <see cref="PriorityMarkerPresence"/> so it can be property-tested without a live scene (task 8.11).
/// This component owns only the Unity concerns — component resolution, the two rings, and the
/// Died-driven teardown.
/// </summary>
/// <remarks>
/// Feature: enemy-swarm-core-archetypes, task 8.10.
/// Requirements: 21.1, 21.2, 21.3, 21.4, 13.8, 14.6, 16.7.
/// </remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Actor))]
public sealed class PriorityTargetMarker : MonoBehaviour
{
    [Tooltip("Radius of the persistent priority ring drawn on the ground under the enemy, in meters.")]
    [SerializeField] private float _markerRadius = 0.85f;

    [Tooltip("Radius of the transient role-action cue ring; kept larger than the marker so the two reads apart.")]
    [SerializeField] private float _roleActionCueRadius = 1.15f;

    [Tooltip("Color of the persistent priority marker. Reserved for priority archetypes only (R21.2).")]
    [SerializeField] private Color _markerColor = new Color(1f, 0.85f, 0.15f, 1f);

    [Tooltip("Color of the role-action cue, chosen to read as distinct from the persistent marker (R21.3).")]
    [SerializeField] private Color _roleActionCueColor = new Color(0.2f, 0.9f, 1f, 1f);

    private Actor _owner;
    private CombatGroundRing _markerRing;
    private CombatGroundRing _roleActionCueRing;
    private bool _roleActionActive;
    private bool _subscribed;

    /// <summary>True while the transient role-action cue is being shown.</summary>
    public bool IsRoleActionCueVisible => _roleActionActive;

    /// <summary>True while the persistent marker ring is present.</summary>
    public bool IsMarkerVisible => _markerRing != null;

    private void Awake()
    {
        _owner = GetComponent<Actor>();
        if (_owner == null)
        {
            Debug.LogError($"{nameof(PriorityTargetMarker)} on '{name}' requires an {nameof(Actor)}; disabling.", this);
            enabled = false;
            return;
        }

        // Persistent marker exists for as long as this component lives; it is only added by
        // EnemyVariant on priority archetypes, so its mere presence already satisfies R21.2.
        _markerRing = CombatGroundRing.Create(transform, "PriorityMarkerRing", _markerColor);

        _owner.Died += HandleOwnerDied;
        _subscribed = true;
    }

    private void LateUpdate()
    {
        // Keep the world-space rings pinned under the (moving) enemy each frame.
        if (_markerRing != null)
            _markerRing.Draw(transform.position, _markerRadius);
        if (_roleActionCueRing != null)
            _roleActionCueRing.Draw(transform.position, _roleActionCueRadius);
    }

    /// <summary>
    /// Shows the transient role-action cue that accompanies a heal / shield / spawn action. Called by
    /// the role behaviors (Healer, ShieldSupport, Spawner) when their action begins. Idempotent: a
    /// second call while already visible does nothing. No-op once the owner is dead (R21.3, R21.4).
    /// </summary>
    public void ShowRoleActionCue()
    {
        if (_owner == null || _owner.IsDead) return;
        if (_roleActionActive) return;

        _roleActionActive = true;
        if (_roleActionCueRing == null)
            _roleActionCueRing = CombatGroundRing.Create(transform, "PriorityRoleActionCue", _roleActionCueColor);
    }

    /// <summary>
    /// Hides the transient role-action cue when the heal / shield / spawn action ends. Called by the
    /// role behaviors. Idempotent: safe to call when no cue is showing (R21.3).
    /// </summary>
    public void HideRoleActionCue()
    {
        _roleActionActive = false;
        if (_roleActionCueRing != null)
        {
            Destroy(_roleActionCueRing.gameObject);
            _roleActionCueRing = null;
        }
    }

    /// <summary>
    /// Removes the persistent marker and any active role-action cue within the same frame the enemy
    /// dies. Destroying this component's <see cref="GameObject"/> parents (the rings) cascades their
    /// removal, and <see cref="OnDestroy"/> guarantees teardown even on scene unload (R21.4).
    /// </summary>
    private void HandleOwnerDied(Actor actor)
    {
        HideRoleActionCue();
        if (_markerRing != null)
        {
            Destroy(_markerRing.gameObject);
            _markerRing = null;
        }
        // The component's job is over once the marker is gone; remove it this frame.
        Destroy(this);
    }

    private void OnDestroy()
    {
        if (_subscribed && _owner != null)
        {
            _owner.Died -= HandleOwnerDied;
            _subscribed = false;
        }

        // Same-frame cleanup guarantee even if we are torn down for a reason other than death
        // (scene unload, prefab teardown): drop both rings so no orphan visual survives.
        if (_roleActionCueRing != null)
        {
            Destroy(_roleActionCueRing.gameObject);
            _roleActionCueRing = null;
        }
        if (_markerRing != null)
        {
            Destroy(_markerRing.gameObject);
            _markerRing = null;
        }
    }
}
