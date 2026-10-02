/// <summary>
/// Immutable description of where a variable-duration <c>Channel</c> Ação_Ofensiva currently
/// sits in its timeline (R3.7). Unlike a fixed-duration action — whose progress is a single
/// global fraction in <c>[0, 1]</c> modelled by <c>ActionTimeline</c> — a Channel of variable
/// duration has no known total duration, so the global <c>[0, 1]</c> timeline does not apply.
///
/// <para>
/// The cancel position of such an action is instead expressed as a <see cref="Phase"/> plus a
/// <see cref="LocalProgress"/> within that phase, which is all the Núcleo_Compartilhado needs to
/// evaluate CancelRules (e.g. a Dash CancelRule covering the whole Active phase) without ever
/// depending on a total duration. Being a scene-free (non-<c>MonoBehaviour</c>) <c>readonly</c>
/// struct keeps it deterministic and property-testable in isolation (R8.7).
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 2.2. Requirements: R3.7.</remarks>
public readonly struct ChannelProgress
{
    /// <summary>
    /// The current phase of the Channel action: <see cref="ActionPhase.Startup"/>,
    /// <see cref="ActionPhase.Active"/> or <see cref="ActionPhase.Recovery"/>.
    /// </summary>
    public ActionPhase Phase { get; }

    /// <summary>
    /// Progress in the closed interval <c>[0, 1]</c> within the current <see cref="Phase"/>.
    /// This is local to the phase and carries no information about the action's total duration.
    /// </summary>
    public float LocalProgress { get; }

    /// <summary>
    /// Creates a channel progress snapshot for the given <paramref name="phase"/>, clamping
    /// <paramref name="localProgress"/> into the closed interval <c>[0, 1]</c> so the invariant
    /// <c>0 &lt;= LocalProgress &lt;= 1</c> always holds regardless of the supplied value.
    /// </summary>
    /// <param name="phase">The current phase of the Channel action.</param>
    /// <param name="localProgress">
    /// Raw progress within the phase; values below <c>0</c> clamp to <c>0</c> and values above
    /// <c>1</c> clamp to <c>1</c>. <c>NaN</c> is treated as <c>0</c>.
    /// </param>
    public ChannelProgress(ActionPhase phase, float localProgress)
    {
        Phase = phase;
        LocalProgress = Clamp01(localProgress);
    }

    /// <summary>
    /// Clamps <paramref name="value"/> to the closed interval <c>[0, 1]</c>, mapping <c>NaN</c>
    /// to <c>0</c>. Kept local so the struct carries no dependency on <c>UnityEngine.Mathf</c>
    /// and stays testable without the engine.
    /// </summary>
    private static float Clamp01(float value)
    {
        if (value < 0f || float.IsNaN(value))
        {
            return 0f;
        }

        return value > 1f ? 1f : value;
    }
}
