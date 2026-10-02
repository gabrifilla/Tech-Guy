/// <summary>
/// The three contiguous, non-overlapping phases of an Ação_Ofensiva's timeline, in order
/// (R2.1). Startup is the anticipation phase (no ImpactEvent or ImpactWindow is active),
/// Active is the phase during which impacts emit and windows open, and Recovery is the
/// trailing phase after impacts where part of the cancellation is permitted.
///
/// <para>
/// This is the base, scene-free (non-<c>MonoBehaviour</c>) type shared by every subsequent
/// pure class of the Núcleo_Compartilhado — <c>ActionTimeline</c>, <c>ChannelProgress</c>,
/// the cancel resolver and the executors all describe an action's progress in terms of an
/// <see cref="ActionPhase"/> plus a local progress. Keeping it a plain enum with no engine
/// dependency lets the combat core be property-tested in isolation (R8.7).
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 1. Requirements: R2.1, R8.7.</remarks>
public enum ActionPhase
{
    /// <summary>
    /// Preparation/anticipation phase occupying <c>[0, startupEnd)</c> of a fixed-duration
    /// timeline. May play VFX, SFX and anticipation displacement, but no ImpactEvent of the
    /// action is emitted and no ImpactWindow is open here.
    /// </summary>
    Startup,

    /// <summary>
    /// The middle phase occupying <c>[startupEnd, activeEnd)</c>, during which the action may
    /// emit one or more ImpactEvents and open one or more ImpactWindows.
    /// </summary>
    Active,

    /// <summary>
    /// The trailing phase occupying <c>[activeEnd, 1]</c>, after the impacts, during which part
    /// of the cancellations are permitted and already-emitted effects may continue per their own
    /// life policy.
    /// </summary>
    Recovery
}
