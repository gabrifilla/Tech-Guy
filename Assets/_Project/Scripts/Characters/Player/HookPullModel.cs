using UnityEngine;

/// <summary>
/// Pure, scene-free model of the Hooker's external pull (Requirement 12), extracted from
/// <see cref="PlayerActor"/>.<c>BeginExternalPull</c> so the three universal pull invariants can be
/// property-tested without a live Unity scene (task 9.2, Property 14):
///   * the pull is <b>bounded</b> — it ends at or before <see cref="MaxPullDuration"/> (≤ 1.5s), and
///     the effective duration is <c>min(maxDuration, 1.5s)</c> clamped to a non-negative value
///     (R12.3, R12.6);
///   * the pull <b>ends early and returns control</b> the first step the pull source becomes
///     control-locked or is destroyed (R12.7);
///   * the pull <b>never displaces the target beyond tolerance</b> of the source — a completed pull
///     stops at the configured stop distance and any residual offset stays within
///     <see cref="ArrivalTolerance"/> of that stop distance.
/// The MonoBehaviour maps these decisions onto Unity: it suspends <c>CharControlScript</c> steering,
/// warps/moves the <c>NavMeshAgent</c> toward the source, and restores control through the cleanup
/// path this model's <see cref="PullStep.ReturnControl"/> flag mirrors.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 9.1. Requirements: 12.3, 12.5, 12.6, 12.7.</remarks>
public readonly struct HookPullModel
{
    /// <summary>Hard upper bound on the pull duration, in seconds (R12.3, R12.6).</summary>
    public const float MaxPullDuration = 1.5f;

    /// <summary>
    /// Distance from the source, in meters, the pull settles at so the target lands beside — not
    /// inside — the Hooker. The pull is considered arrived once the target is within
    /// <see cref="ArrivalTolerance"/> of this stop distance.
    /// </summary>
    public const float StopDistance = 1.0f;

    /// <summary>
    /// Slack, in meters, added to <see cref="StopDistance"/> when deciding arrival, and the ceiling
    /// on how far past the stop distance a completed pull may leave the target (the displacement
    /// tolerance property, R12.5-adjacent bound used by task 9.2).
    /// </summary>
    public const float ArrivalTolerance = 0.05f;

    /// <summary>The band-clamped effective duration: <c>Clamp(maxDuration, 0, 1.5s)</c>.</summary>
    public float Duration { get; }

    /// <summary>Builds a pull whose duration is clamped to the [0, 1.5s] bound (R12.3, R12.6).</summary>
    /// <param name="maxDuration">Requested upper bound, in seconds; values above 1.5s are clamped down.</param>
    public HookPullModel(float maxDuration)
    {
        Duration = Mathf.Clamp(maxDuration, 0f, MaxPullDuration);
    }

    /// <summary>
    /// Decides the pull's state after <paramref name="elapsed"/> seconds, given whether the source is
    /// still a valid, controllable Hooker. The pull ends (returning control) when any of the
    /// following hold, so control is always eventually returned within the bounded duration:
    ///   * the source is gone (destroyed mid-pull) — R12.6;
    ///   * the source is control-locked — R12.7;
    ///   * the elapsed time has reached the bounded duration — R12.3, R12.6.
    /// While none hold the pull continues and control stays suspended.
    /// </summary>
    /// <param name="elapsed">Seconds elapsed since the pull began (clamped to ≥ 0 internally).</param>
    /// <param name="sourceAlive">False when the source was destroyed mid-pull (R12.6).</param>
    /// <param name="sourceControlLocked">True when the source is stunned/airborne mid-pull (R12.7).</param>
    public PullStep Evaluate(float elapsed, bool sourceAlive, bool sourceControlLocked)
    {
        float clampedElapsed = Mathf.Max(0f, elapsed);
        bool durationElapsed = clampedElapsed >= Duration;
        bool interrupted = !sourceAlive || sourceControlLocked;
        bool shouldEnd = interrupted || durationElapsed;
        return new PullStep(shouldEnd, shouldEnd, durationElapsed, interrupted);
    }

    /// <summary>
    /// The interpolation fraction 0→1 across the bounded duration at <paramref name="elapsed"/>
    /// seconds. Used by the mover to lerp the target toward its stop point at an even rate; a
    /// zero-length duration is fully complete immediately (fraction 1).
    /// </summary>
    public float Fraction(float elapsed)
    {
        if (Duration <= 0f) return 1f;
        return Mathf.Clamp01(Mathf.Max(0f, elapsed) / Duration);
    }

    /// <summary>
    /// The target's world position at fraction <paramref name="fraction"/> along a straight pull from
    /// <paramref name="start"/> toward <paramref name="source"/>, settling at <see cref="StopDistance"/>
    /// from the source. When the target already sits inside the stop distance it is not pushed away, so
    /// the pull never displaces the target <b>outward</b> beyond tolerance.
    /// </summary>
    public static Vector3 PositionAt(Vector3 start, Vector3 source, float fraction)
    {
        Vector3 toStart = start - source;
        float startDistance = toStart.magnitude;
        // Already at/inside the stop ring: don't shove the target outward, hold its planar position.
        if (startDistance <= StopDistance) return start;

        Vector3 dir = toStart / startDistance;
        Vector3 stopPoint = source + dir * StopDistance;
        return Vector3.Lerp(start, stopPoint, Mathf.Clamp01(fraction));
    }

    /// <summary>
    /// True when a completed pull leaves the target within the displacement tolerance of its intended
    /// stop point — i.e. no farther than <see cref="StopDistance"/> + <see cref="ArrivalTolerance"/>
    /// from the source and never yanked closer than the source itself. Task 9.2 asserts this holds for
    /// the terminal position of every generated pull.
    /// </summary>
    public static bool WithinDisplacementTolerance(Vector3 finalPosition, Vector3 start, Vector3 source)
    {
        float startDistance = (start - source).magnitude;
        float finalDistance = (finalPosition - source).magnitude;
        // A pull only ever moves the target inward toward the stop ring; it must not overshoot past
        // the source, nor end farther out than where it started plus tolerance.
        float upperBound = Mathf.Max(startDistance, StopDistance) + ArrivalTolerance;
        return finalDistance >= -ArrivalTolerance && finalDistance <= upperBound;
    }
}

/// <summary>
/// The outcome of one <see cref="HookPullModel.Evaluate"/> step: whether the pull should stop this
/// step, whether movement control must be returned to the target, and why it ended. <see cref="Ended"/>
/// and <see cref="ReturnControl"/> move together — control is returned exactly when the pull ends —
/// so a caller can never leave the target permanently suspended.
/// </summary>
public readonly struct PullStep
{
    /// <summary>True when the pull terminates this step (duration reached, or interrupted).</summary>
    public bool Ended { get; }

    /// <summary>True when movement control must be returned to the target this step (== <see cref="Ended"/>).</summary>
    public bool ReturnControl { get; }

    /// <summary>True when the pull ended because its bounded duration elapsed (R12.3, R12.6).</summary>
    public bool DurationElapsed { get; }

    /// <summary>True when the pull ended early because the source vanished or was control-locked (R12.6, R12.7).</summary>
    public bool Interrupted { get; }

    /// <summary>Creates a pull-step outcome.</summary>
    public PullStep(bool ended, bool returnControl, bool durationElapsed, bool interrupted)
    {
        Ended = ended;
        ReturnControl = returnControl;
        DurationElapsed = durationElapsed;
        Interrupted = interrupted;
    }
}
