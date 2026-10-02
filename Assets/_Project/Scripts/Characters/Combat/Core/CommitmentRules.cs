/// <summary>
/// The Categoria_de_Compromisso of an Ação_Ofensiva — a <em>profile of defaults</em> for mobility
/// and cancelability, not a rigid ordering between categories (R3.1). Concrete mobility and
/// cancellation always derive from the per-action/per-phase configuration in
/// <c>CombatActionProfile</c>; nothing here imposes relations such as "Fluid &gt; Channel".
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 3.1. Requirements: R3.1, R3.2, R3.10.</remarks>
public enum CommitmentCategory
{
    /// <summary>Profile of fast, responsive actions (short recovery, partial movement, early cancel).</summary>
    Fluid,

    /// <summary>Profile of heavy actions with a legible startup and a limited cancel window.</summary>
    Committed,

    /// <summary>Profile of sustained actions that continue while Active, cancelable by dash when a Dash CancelRule allows it.</summary>
    Channel
}

/// <summary>
/// How a <see cref="CommitmentCategory.Channel"/> Ação_Ofensiva ends its Active phase (R3.5).
/// Not all modes are required to be implemented in this phase, and <see cref="Hold"/> is not a
/// global requirement of every Channel action.
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 3.1. Requirements: R3.5.</remarks>
public enum ChannelTerminationMode
{
    /// <summary>Ends when the control that triggered it is released, per the action's Recovery rule.</summary>
    Hold,

    /// <summary>A single activation runs a fixed-duration sequence.</summary>
    Timed,

    /// <summary>Ends by an explicit condition.</summary>
    Condition
}

/// <summary>
/// Pure, scene-free (non-<c>MonoBehaviour</c>) helpers that resolve a declared
/// <see cref="CommitmentCategory"/> and clamp per-phase movement fractions (R3.1, R3.2, R3.10).
///
/// <para>
/// Categories are only profiles of defaults: this type deliberately imposes <em>no</em> rigid
/// relations between them. <see cref="Resolve"/> echoes the declared category when present and
/// falls back to <see cref="Default"/> (<see cref="CommitmentCategory.Committed"/>) with a warning
/// when absent, as a backward-compatibility safeguard. <see cref="ClampMovementFraction"/> simply
/// constrains a configured fraction to the closed interval <c>[0, 1]</c>. Being engine-free keeps
/// the combat core deterministic and property-testable in isolation (R8.7).
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 3.1. Requirements: R3.1, R3.2, R3.10.</remarks>
public static class CommitmentRules
{
    /// <summary>
    /// The category applied when none is declared: <see cref="CommitmentCategory.Committed"/>,
    /// as a compatibility safeguard (R3.10).
    /// </summary>
    public const CommitmentCategory Default = CommitmentCategory.Committed;

    /// <summary>
    /// Resolves the effective <see cref="CommitmentCategory"/>: returns the
    /// <paramref name="declared"/> category when present, or <see cref="Default"/> when it is
    /// <c>null</c> (R3.10). <paramref name="emitWarning"/> is set <c>true</c> <em>if and only if</em>
    /// the category was absent, so callers (e.g. editor <c>OnValidate</c>) can surface the missing
    /// category without ever mutating the design data.
    /// </summary>
    /// <param name="declared">The declared category, or <c>null</c> when none was authored.</param>
    /// <param name="emitWarning">
    /// Receives <c>true</c> when <paramref name="declared"/> was <c>null</c> (the default was
    /// applied); otherwise <c>false</c>.
    /// </param>
    /// <returns>The declared category when present; otherwise <see cref="Default"/>.</returns>
    public static CommitmentCategory Resolve(CommitmentCategory? declared, out bool emitWarning)
    {
        if (declared.HasValue)
        {
            emitWarning = false;
            return declared.Value;
        }

        emitWarning = true;
        return Default;
    }

    /// <summary>
    /// Clamps a configured per-phase movement fraction to the closed interval <c>[0, 1]</c> (R3.2).
    /// Values below <c>0</c> clamp to <c>0</c>, values above <c>1</c> clamp to <c>1</c>, and
    /// <c>NaN</c> is treated as <c>0</c>. This derives from the concrete per-action/per-phase
    /// configuration and does <em>not</em> impose any ordering between categories.
    /// </summary>
    /// <param name="configuredFraction">The authored movement fraction for a phase.</param>
    /// <returns>The fraction constrained to <c>[0, 1]</c>.</returns>
    public static float ClampMovementFraction(float configuredFraction)
    {
        if (configuredFraction < 0f || float.IsNaN(configuredFraction))
        {
            return 0f;
        }

        return configuredFraction > 1f ? 1f : configuredFraction;
    }
}
