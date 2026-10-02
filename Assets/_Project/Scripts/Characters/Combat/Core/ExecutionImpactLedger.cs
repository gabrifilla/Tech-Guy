using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Identity of a single concrete execution of an Ação_Ofensiva. Each ImpactEvent produced by that
/// execution is identified by the pair <c>(ExecutionId, eventIndex)</c> for deduplication, so the
/// logical clock (the single advance source in <c>FixedUpdate</c>) and an Animation Event can both
/// signal the same impact without it ever being counted twice (R8.9).
///
/// <para>
/// Being a pure, scene-free (non-<c>MonoBehaviour</c>) <c>readonly struct</c> with value equality,
/// it is deterministic and property-testable in isolation (R8.7), matching the other
/// Núcleo_Compartilhado Core types. <see cref="Value"/> is handed out by <see cref="Next"/> from a
/// thread-safe, monotonically increasing per-process counter, so distinct executions never collide
/// on their id. Value equality (over <see cref="Value"/>) is what lets the pair key correctly in a
/// <see cref="HashSet{T}"/> inside <see cref="ExecutionImpactLedger"/>.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 8.1. Requirements: R8.9.</remarks>
public readonly struct ExecutionId : IEquatable<ExecutionId>
{
    private static int _counter;

    /// <summary>The monotonically increasing per-process identifier of this execution.</summary>
    public int Value { get; }

    private ExecutionId(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Returns a fresh <see cref="ExecutionId"/> whose <see cref="Value"/> is strictly greater than
    /// every id previously handed out in this process. Thread-safe: the underlying counter is
    /// advanced with <see cref="Interlocked.Increment(ref int)"/> so concurrent callers never share
    /// an id (R8.9).
    /// </summary>
    public static ExecutionId Next()
    {
        return new ExecutionId(Interlocked.Increment(ref _counter));
    }

    /// <summary>Value equality over <see cref="Value"/>.</summary>
    public bool Equals(ExecutionId other)
    {
        return Value == other.Value;
    }

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is ExecutionId other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return Value;
    }

    /// <summary>Value equality operator over <see cref="Value"/>.</summary>
    public static bool operator ==(ExecutionId left, ExecutionId right)
    {
        return left.Equals(right);
    }

    /// <summary>Value inequality operator over <see cref="Value"/>.</summary>
    public static bool operator !=(ExecutionId left, ExecutionId right)
    {
        return !left.Equals(right);
    }
}

/// <summary>
/// Deduplication source of truth for the ImpactEvents of the active execution (R8.9). It records
/// each <c>(ExecutionId, eventIndex)</c> pair the first time it is emitted so that an impact is
/// counted exactly once even when the logical clock and an Animation Event coincide on the same
/// <c>eventIndex</c>: whichever fires first wins and the other is a no-op.
///
/// <para>
/// Being a pure, scene-free (non-<c>MonoBehaviour</c>) class, it is deterministic and
/// property-testable in isolation (R8.7). It is backed by a <see cref="HashSet{T}"/> of the
/// <c>(ExecutionId, int)</c> key, relying on <see cref="ExecutionId"/>'s value equality so repeated
/// pairs collapse to a single set entry.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 8.1. Requirements: R8.9.</remarks>
public sealed class ExecutionImpactLedger
{
    private readonly HashSet<(ExecutionId Execution, int EventIndex)> _emitted =
        new HashSet<(ExecutionId Execution, int EventIndex)>();

    /// <summary>
    /// Attempts to emit the given <paramref name="eventIndex"/> for the given
    /// <paramref name="executionId"/>. Returns <c>true</c> only the <em>first</em> time this pair is
    /// seen (recording it as emitted) and <c>false</c> on every subsequent call with the same pair.
    /// This is the single "already emitted" check that keeps the logical clock and Animation Events
    /// from double-counting the same impact (R8.9).
    /// </summary>
    /// <param name="executionId">The identity of the concrete execution.</param>
    /// <param name="eventIndex">The index of the ImpactEvent within that execution.</param>
    /// <returns><c>true</c> on the first emission of the pair; <c>false</c> on any repeat.</returns>
    public bool TryEmit(ExecutionId executionId, int eventIndex)
    {
        return _emitted.Add((executionId, eventIndex));
    }
}
