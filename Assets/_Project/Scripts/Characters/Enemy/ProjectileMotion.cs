using UnityEngine;

/// <summary>
/// Pure, scene-free state machine for a single ranged projectile's travel, collision, and
/// cleanup decisions. Extracted from <see cref="EnemyProjectile"/> (which generalizes the older
/// FrostBolt travel-over-frames pattern in <c>EnemyCombatActions.Bolt</c>) so the universal
/// projectile invariants can be property-tested without a live Unity scene (task 4.2,
/// Property 5 / Property 6).
///
/// The model owns only the decisions, never the scene:
///   * It advances the projectile one <see cref="Step(float, ProjectileHit)"/> per frame, feeding
///     back how far it was allowed to move (clipped by geometry) and whether the target overlapped
///     that segment (R19.1 — outcome never resolves on the firing frame).
///   * It applies its damage <em>at most once</em>: the first frame the target overlaps, it reports
///     a hit and marks itself for destruction (R19.2). Subsequent steps never damage again.
///   * It marks itself for destruction — with no damage — once the cumulative travelled distance
///     reaches the configured range (R19.3) or when a step is blocked short by geometry (R19.4).
///   * Once <see cref="IsMarkedForDestruction"/> it is inert: no further damage, no further travel.
///   * <see cref="MarkOwnerGone"/> lets the owner's death / disable cascade mark it for destruction
///     with no further damage (R19.6).
/// The mapping of these decisions onto Unity (transform moves, SphereCast clipping, overlap tests,
/// material release, Destroy) lives entirely in <see cref="EnemyProjectile"/>.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 4.1. Requirements: 19.1, 19.2, 19.3, 19.4, 19.6.</remarks>
public sealed class ProjectileMotion
{
    private readonly float _range;
    private float _travelled;
    private bool _damageApplied;
    private bool _markedForDestruction;

    /// <summary>
    /// Creates a projectile motion with the given total travel range in metres. A non-positive
    /// range means the projectile has no distance to cover, so it is marked for destruction
    /// immediately (it still never damages on creation).
    /// </summary>
    /// <param name="range">Cumulative travel distance the projectile may cover before it expires (R19.3).</param>
    public ProjectileMotion(float range)
    {
        _range = Mathf.Max(0f, range);
        if (_range <= 0f) _markedForDestruction = true;
    }

    /// <summary>Cumulative distance travelled so far, in metres.</summary>
    public float Travelled => _travelled;

    /// <summary>The configured total travel range, in metres.</summary>
    public float Range => _range;

    /// <summary>Distance still available before the projectile exhausts its range.</summary>
    public float RemainingRange => Mathf.Max(0f, _range - _travelled);

    /// <summary>True once this projectile has applied its single point of damage (R19.2).</summary>
    public bool DamageApplied => _damageApplied;

    /// <summary>
    /// True once the projectile has been marked for destruction — on hit, on range exhaustion, on a
    /// geometry block, or by the owner cascade. Once set it is permanent and the projectile is inert.
    /// </summary>
    public bool IsMarkedForDestruction => _markedForDestruction;

    /// <summary>
    /// Advances the projectile by one frame and returns the resolution for that frame. The caller
    /// supplies the intended travel step and the collision facts observed along that step
    /// (<see cref="ProjectileHit"/>): how far it was allowed to move before geometry blocked it, and
    /// whether the target overlapped the travelled segment.
    ///
    /// Resolution rules, evaluated in order (matching the FrostBolt loop and R19):
    ///   1. If already marked for destruction, the projectile is inert: no move, no damage.
    ///   2. A target overlap on this segment applies damage exactly once and marks for destruction
    ///      (R19.2); it damages even if the same step is also geometry-blocked, because the overlap
    ///      is detected along the travelled portion.
    ///   3. Otherwise, geometry that blocked the step short marks for destruction with no damage (R19.4).
    ///   4. Otherwise, reaching the configured range marks for destruction with no damage (R19.3).
    ///   5. Otherwise the projectile keeps travelling.
    /// The projectile can never resolve on the frame it is fired unless the very first step already
    /// overlaps the target or is blocked — travel itself is inherently multi-frame (R19.1).
    /// </summary>
    /// <param name="intendedStep">The distance the projectile would move this frame absent any block. Clamped to the remaining range and to be non-negative.</param>
    /// <param name="hit">The collision facts observed for this step (allowed distance + target overlap).</param>
    public ProjectileStepResult Step(float intendedStep, ProjectileHit hit)
    {
        if (_markedForDestruction)
            return ProjectileStepResult.Inert(_travelled);

        // Never travel past the remaining range, and never a negative step.
        float requested = Mathf.Clamp(intendedStep, 0f, RemainingRange);
        float allowed = Mathf.Clamp(hit.AllowedDistance, 0f, requested);
        bool blocked = hit.BlockedByGeometry && allowed + 0.001f < requested;

        _travelled += allowed;

        // A target overlap along the travelled segment applies damage exactly once (R19.2).
        if (hit.OverlapsTarget && !_damageApplied)
        {
            _damageApplied = true;
            _markedForDestruction = true;
            return ProjectileStepResult.Hit(_travelled);
        }

        // Geometry blocked the step short of its intended travel: stop, no damage (R19.4).
        if (blocked)
        {
            _markedForDestruction = true;
            return ProjectileStepResult.Blocked(_travelled);
        }

        // Range exhausted without hitting the target: expire, no damage (R19.3).
        if (RemainingRange <= 0f)
        {
            _markedForDestruction = true;
            return ProjectileStepResult.RangeExhausted(_travelled);
        }

        return ProjectileStepResult.Travelling(allowed, _travelled);
    }

    /// <summary>
    /// Resolves an area (arced-impact) projectile the moment it lands: applies damage exactly once
    /// if the target lies within the impact area, and marks the projectile for destruction either
    /// way (R19.2 for the arced path — a single area check on land). No-op once already destroyed.
    /// </summary>
    /// <param name="targetInImpactArea">Whether the target is inside the telegraphed impact area at the moment of landing.</param>
    /// <returns>True when this call applied the projectile's single point of damage.</returns>
    public bool ResolveImpact(bool targetInImpactArea)
    {
        if (_markedForDestruction) return false;

        _markedForDestruction = true;
        if (targetInImpactArea && !_damageApplied)
        {
            _damageApplied = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Marks the projectile for destruction because its owning enemy died or was disabled while it
    /// was in flight. After this the projectile applies no further damage (R19.6). It is a no-op if
    /// the projectile has already resolved.
    /// </summary>
    public void MarkOwnerGone()
    {
        _markedForDestruction = true;
    }
}

/// <summary>
/// The collision facts observed for a single projectile step, supplied by the caller each frame.
/// Kept separate from the scene so a test can feed mocked collision (task 4.2).
/// </summary>
public readonly struct ProjectileHit
{
    /// <summary>How far the projectile was actually allowed to move this step before geometry blocked it, in metres.</summary>
    public readonly float AllowedDistance;

    /// <summary>Whether the projectile's volume overlapped the target along the travelled segment this step.</summary>
    public readonly bool OverlapsTarget;

    /// <summary>Whether static geometry limited this step (an obstacle lay in the path).</summary>
    public readonly bool BlockedByGeometry;

    public ProjectileHit(float allowedDistance, bool overlapsTarget, bool blockedByGeometry)
    {
        AllowedDistance = allowedDistance;
        OverlapsTarget = overlapsTarget;
        BlockedByGeometry = blockedByGeometry;
    }

    /// <summary>A clear step of <paramref name="distance"/> metres with no target overlap and no block.</summary>
    public static ProjectileHit Clear(float distance) => new ProjectileHit(distance, false, false);

    /// <summary>A step that reached the target after <paramref name="distance"/> metres of travel.</summary>
    public static ProjectileHit HitTarget(float distance) => new ProjectileHit(distance, true, false);

    /// <summary>A step blocked short by geometry after <paramref name="distance"/> metres of travel.</summary>
    public static ProjectileHit HitGeometry(float distance) => new ProjectileHit(distance, false, true);
}

/// <summary>What happened to the projectile on a single <see cref="ProjectileMotion.Step"/>.</summary>
public enum ProjectileOutcome
{
    /// <summary>The projectile moved and remains in flight.</summary>
    Travelling,

    /// <summary>The projectile overlapped the target, applied its single point of damage, and is marked for destruction.</summary>
    Hit,

    /// <summary>The projectile was stopped short by geometry and is marked for destruction with no damage.</summary>
    Blocked,

    /// <summary>The projectile exhausted its configured range and is marked for destruction with no damage.</summary>
    RangeExhausted,

    /// <summary>The projectile was already marked for destruction and did nothing this step.</summary>
    Inert
}

/// <summary>The immutable result of a single projectile step: what happened and the resulting state.</summary>
public readonly struct ProjectileStepResult
{
    /// <summary>What the projectile did this step.</summary>
    public readonly ProjectileOutcome Outcome;

    /// <summary>How far the projectile actually moved this step, in metres.</summary>
    public readonly float MovedDistance;

    /// <summary>Cumulative distance travelled after this step, in metres.</summary>
    public readonly float Travelled;

    private ProjectileStepResult(ProjectileOutcome outcome, float movedDistance, float travelled)
    {
        Outcome = outcome;
        MovedDistance = movedDistance;
        Travelled = travelled;
    }

    /// <summary>Whether the projectile is marked for destruction after this step.</summary>
    public bool ShouldDestroy => Outcome != ProjectileOutcome.Travelling;

    /// <summary>Whether this step applied the projectile's single point of damage.</summary>
    public bool AppliedDamage => Outcome == ProjectileOutcome.Hit;

    internal static ProjectileStepResult Travelling(float moved, float travelled)
        => new ProjectileStepResult(ProjectileOutcome.Travelling, moved, travelled);

    internal static ProjectileStepResult Hit(float travelled)
        => new ProjectileStepResult(ProjectileOutcome.Hit, 0f, travelled);

    internal static ProjectileStepResult Blocked(float travelled)
        => new ProjectileStepResult(ProjectileOutcome.Blocked, 0f, travelled);

    internal static ProjectileStepResult RangeExhausted(float travelled)
        => new ProjectileStepResult(ProjectileOutcome.RangeExhausted, 0f, travelled);

    internal static ProjectileStepResult Inert(float travelled)
        => new ProjectileStepResult(ProjectileOutcome.Inert, 0f, travelled);
}
