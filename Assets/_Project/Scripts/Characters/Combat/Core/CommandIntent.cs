using UnityEngine;

/// <summary>
/// The kind of command a player input maps to (R5.2). A command is either the basic attack, the
/// dash, or one of the four weapon skills (Q/W/E/R → <see cref="Skill1"/>..<see cref="Skill4"/>).
/// Together with <see cref="CommandIntent"/> this lets a single-slot Buffer_de_Input remember any
/// one of the six commands the player can issue without re-deriving which action it was.
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 6.1. Requirements: R5.2, R5.7.</remarks>
public enum CommandKind
{
    /// <summary>The basic attack command.</summary>
    Basic,

    /// <summary>The dash command (defensive cancel tool; see R6).</summary>
    Dash,

    /// <summary>The first weapon skill (Q).</summary>
    Skill1,

    /// <summary>The second weapon skill (W).</summary>
    Skill2,

    /// <summary>The third weapon skill (E).</summary>
    Skill3,

    /// <summary>The fourth weapon skill (R).</summary>
    Skill4
}

/// <summary>
/// The Intencao_de_Comando associated with a single player input (R5.2). It carries the command
/// kind together with the target or direction <em>explicitly derived from the cursor/input at the
/// moment of emission</em> — the aim is captured once, at emission time, and the intent is never
/// re-aimed afterwards. This is what lets a buffered intent fire later without ever re-selecting an
/// arbitrary enemy by proximity (R5.2).
///
/// <para>
/// Being a pure, scene-free (non-<c>MonoBehaviour</c>) <c>readonly struct</c>, it is deterministic
/// and property-testable in isolation (R8.7), matching the other Núcleo_Compartilhado Core types.
/// <see cref="Sequence"/> is a monotonically assigned tie-break so two intents issued at the same
/// <see cref="IssuedAt"/> timestamp still have a well-defined, deterministic ordering for the
/// single-slot buffer's "most recent wins" rule (R5.7).
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 6.1. Requirements: R5.2, R5.7.</remarks>
public readonly struct CommandIntent
{
    /// <summary>The kind of command this intent represents.</summary>
    public CommandKind Kind { get; }

    /// <summary>
    /// The explicit target captured at emission time for an Ataque_Alvo, or <c>null</c> for
    /// commands that carry no target (e.g. an Ataque_Direcional or a dash). Captured once and
    /// never re-aimed, so a buffered intent fires against the original target rather than
    /// re-selecting an enemy by proximity (R5.2).
    /// </summary>
    public Actor Target { get; }

    /// <summary>
    /// The explicit cursor direction captured at emission time for a directional attack or dash.
    /// Like <see cref="Target"/>, it is captured once at emission and never re-derived (R5.2).
    /// </summary>
    public Vector3 Direction { get; }

    /// <summary>The timestamp (seconds) at which this intent was emitted.</summary>
    public float IssuedAt { get; }

    /// <summary>
    /// A deterministic tie-break assigned at emission (R5.7). When two intents share the same
    /// <see cref="IssuedAt"/>, the one with the greater <see cref="Sequence"/> is considered the
    /// more recent, giving the single-slot buffer a stable, deterministic "most recent wins" rule.
    /// </summary>
    public long Sequence { get; }

    /// <summary>
    /// Builds a command intent, capturing the aim (target/direction) at emission time (R5.2).
    /// </summary>
    /// <param name="kind">The kind of command.</param>
    /// <param name="target">The explicit target for an Ataque_Alvo, or <c>null</c> when the
    /// command carries no target.</param>
    /// <param name="direction">The explicit cursor direction captured at emission.</param>
    /// <param name="issuedAt">The emission timestamp, in seconds.</param>
    /// <param name="sequence">The deterministic tie-break for same-timestamp intents (R5.7).</param>
    public CommandIntent(CommandKind kind, Actor target, Vector3 direction, float issuedAt, long sequence)
    {
        Kind = kind;
        Target = target;
        Direction = direction;
        IssuedAt = issuedAt;
        Sequence = sequence;
    }
}
