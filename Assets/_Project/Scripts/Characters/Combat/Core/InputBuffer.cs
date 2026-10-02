using System;

/// <summary>
/// The single-slot Buffer_de_Input of the Núcleo_Compartilhado (R5). It temporarily remembers one
/// <see cref="CommandIntent"/> whose <em>only</em> impediment is temporal, so the coordinator can
/// fire it the moment the applicable window opens or the current Ação_Ofensiva ends — accepting the
/// player's intention without demanding perfect timing. Only commands blocked purely by timing are
/// ever stored here; a command refused by resource/cooldown is never kept waiting (R5.9), that
/// decision belongs to the coordinator before it calls <see cref="Store"/>.
///
/// <para>
/// The buffer holds <em>at most one</em> intent (single slot): a newer <see cref="Store"/> replaces
/// whatever was pending, with "most recent wins" decided by <see cref="CommandIntent.IssuedAt"/> and
/// the deterministic <see cref="CommandIntent.Sequence"/> tie-break for equal timestamps (R5.7).
/// Because the stored aim is captured once at emission (see <see cref="CommandIntent"/>), a buffered
/// intent fires against its original target and never re-aims an arbitrary enemy (R5.2).
/// </para>
///
/// <para>
/// Clock contract (R5.10): the <c>now</c> passed to <see cref="TryConsume"/> and
/// <see cref="Expire"/> is an <strong>unscaled</strong> clock supplied by the coordinator. It does
/// <strong>not</strong> advance during a menu pause and does <strong>not</strong> consume buffer
/// validity during a Hit_Stop, so legitimate input is never discarded by the feedback pause. The
/// buffer also never emits a rejection on each internal re-evaluation (R5.12): discarding an expired
/// intent or declining because the window is still closed is silent — at most one terminal rejection
/// is the coordinator's responsibility, not this slot's.
/// </para>
///
/// <para>
/// Being a pure, scene-free (non-<c>MonoBehaviour</c>) class in the global namespace, it is
/// deterministic and property-testable in isolation, matching the other Núcleo_Compartilhado Core
/// types.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 6.2. Requirements: R5.1, R5.2, R5.3, R5.4, R5.5, R5.6, R5.7, R5.9, R5.10, R5.12.</remarks>
public sealed class InputBuffer
{
    private const float DefaultBufferDuration = 0.120f;

    private CommandIntent _pending;
    private bool _hasPending;

    /// <summary>
    /// How long (seconds) a buffered intent stays valid, measured against the unscaled clock
    /// (R5.1). The default is 120 ms; the 100–180 ms range is only a suggestion, not a hard limit.
    /// A value of <c>0</c> disables the buffer entirely, so nothing is ever retained. Negative or
    /// invalid (NaN) values are rejected by the constructor rather than silently clamped.
    /// </summary>
    public float BufferDuration { get; }

    /// <summary>
    /// Whether a non-expired intent is currently waiting in the slot. Note this reflects only the
    /// presence of a stored intent, not expiration against the current clock — call
    /// <see cref="Expire"/> or <see cref="TryConsume"/> to prune an expired one.
    /// </summary>
    public bool HasPending => _hasPending;

    /// <summary>
    /// Builds a buffer with the default <c>120 ms</c> <see cref="BufferDuration"/> (R5.1).
    /// </summary>
    public InputBuffer()
        : this(DefaultBufferDuration)
    {
    }

    /// <summary>
    /// Builds a buffer with the given <paramref name="bufferDuration"/> (R5.1). A value of
    /// <c>0</c> disables the buffer; negative or <c>NaN</c> values are rejected.
    /// </summary>
    /// <param name="bufferDuration">How long (seconds) a buffered intent stays valid. Must be a
    /// finite, non-negative number; <c>0</c> disables the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="bufferDuration"/>
    /// is negative or <c>NaN</c> (R5.1 "valores negativos ou inválidos SHALL ser rejeitados").</exception>
    public InputBuffer(float bufferDuration)
    {
        if (float.IsNaN(bufferDuration) || bufferDuration < 0f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bufferDuration),
                bufferDuration,
                "BufferDuration must be a finite, non-negative number; 0 disables the buffer (R5.1).");
        }

        BufferDuration = bufferDuration;
    }

    /// <summary>
    /// Stores <paramref name="intent"/> in the single slot, replacing any previous intent with the
    /// most recent one (R5.2, R5.7). Recency is decided by <see cref="CommandIntent.IssuedAt"/>;
    /// for equal timestamps the greater <see cref="CommandIntent.Sequence"/> wins. When
    /// <see cref="BufferDuration"/> is <c>0</c> the buffer is disabled and this is a no-op, so
    /// nothing is ever retained.
    /// </summary>
    /// <param name="intent">The command intent whose only impediment is temporal.</param>
    public void Store(in CommandIntent intent)
    {
        // 0 disables the buffer: never retain anything (R5.1).
        if (BufferDuration == 0f)
        {
            return;
        }

        // Single slot, most recent wins; keep the current one only if it is strictly more recent
        // than the incoming intent (R5.7). Equal (IssuedAt, Sequence) replaces, which is harmless
        // because the stored content is identical.
        if (_hasPending && IsMoreRecent(_pending, intent))
        {
            return;
        }

        _pending = intent;
        _hasPending = true;
    }

    /// <summary>
    /// Fires the pending intent at most once (R5.6). When an intent is pending, has not expired
    /// (<c>now - IssuedAt &lt;= BufferDuration</c>), and <paramref name="windowOpen"/> is
    /// <c>true</c>, it is returned and the slot is cleared so a later call cannot fire it again.
    /// An expired intent is discarded without firing (R5.5). When the intent is still valid but
    /// <paramref name="windowOpen"/> is <c>false</c>, it is kept for a later evaluation and no
    /// rejection is emitted (R5.12).
    /// </summary>
    /// <param name="now">The current unscaled time, in seconds (R5.10).</param>
    /// <param name="windowOpen">Whether the window applicable to the buffered command is open now,
    /// i.e. the current action ended or the destination window opened (R5.3, R5.4).</param>
    /// <param name="intent">The fired intent when this returns <c>true</c>; otherwise
    /// <c>default</c>.</param>
    /// <returns><c>true</c> when a valid intent fired (and was removed); otherwise <c>false</c>.</returns>
    public bool TryConsume(float now, bool windowOpen, out CommandIntent intent)
    {
        intent = default;

        if (!_hasPending)
        {
            return false;
        }

        // Expired: discard silently without firing (R5.5). No rejection on re-evaluation (R5.12).
        if (IsExpired(now))
        {
            Clear();
            return false;
        }

        // Valid but the applicable window is still closed: keep it and stay silent (R5.12).
        if (!windowOpen)
        {
            return false;
        }

        // Valid and the window is open: fire exactly once and clear the slot (R5.6).
        intent = _pending;
        Clear();
        return true;
    }

    /// <summary>
    /// Drops the pending intent if it has expired against <paramref name="now"/> (R5.5). Used by
    /// the per-frame tick for cleanup; a non-expired intent is left untouched and never fires here.
    /// Discarding is silent (R5.12).
    /// </summary>
    /// <param name="now">The current unscaled time, in seconds (R5.10).</param>
    public void Expire(float now)
    {
        if (_hasPending && IsExpired(now))
        {
            Clear();
        }
    }

    /// <summary>
    /// Whether the stored intent is expired at <paramref name="now"/>, i.e.
    /// <c>now - IssuedAt &gt; BufferDuration</c> (R5.5).
    /// </summary>
    private bool IsExpired(float now)
    {
        return now - _pending.IssuedAt > BufferDuration;
    }

    /// <summary>
    /// Whether <paramref name="current"/> is strictly more recent than <paramref name="incoming"/>
    /// under the deterministic ordering: later <see cref="CommandIntent.IssuedAt"/> first, then
    /// greater <see cref="CommandIntent.Sequence"/> as the tie-break for equal timestamps (R5.7).
    /// </summary>
    private static bool IsMoreRecent(in CommandIntent current, in CommandIntent incoming)
    {
        if (current.IssuedAt > incoming.IssuedAt)
        {
            return true;
        }

        if (current.IssuedAt < incoming.IssuedAt)
        {
            return false;
        }

        return current.Sequence > incoming.Sequence;
    }

    private void Clear()
    {
        _pending = default;
        _hasPending = false;
    }
}
