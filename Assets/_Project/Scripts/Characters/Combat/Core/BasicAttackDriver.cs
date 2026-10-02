/// <summary>
/// Decides, as pure (scene-free, non-<c>MonoBehaviour</c>) logic, how many basic attacks a
/// sequence of player input authorizes — <strong>without any auto-combat</strong>. The mere
/// existence of an Alvo_Interno never authorizes an attack (R1.7, R1.12); only an explicit
/// Toque_de_Ataque (<see cref="QueueTap"/>) or an active Segurar_Ataque (<see cref="SetHold"/>)
/// does.
///
/// <para>
/// A pending tap consumes to <em>exactly one</em> attack and is honored even without hold
/// (R1.1/R1.5/R1.6). While held, <see cref="TryTakeAttack"/> authorizes at most one attack per
/// <c>attackInterval</c> based on an unscaled <c>now</c> clock: the first hold attack is allowed
/// immediately, then subsequent ones are gated by the interval (R1.3). <see cref="ReleaseHold"/>
/// removes future hold repeats <em>without</em> cutting a hit that was already authorized/started
/// (R1.4/R5.11), and <see cref="Clear"/> wipes the pending tap, hold state and timing when the
/// target is lost or an Ordem_de_Movimento overrides the intent (R1.12).
/// </para>
///
/// <para>
/// Being engine-free keeps the combat core deterministic and property-testable in isolation
/// (R8.7): callers pass an unscaled <c>now</c> that does not advance during menu pause nor consume
/// validity during hit-stop, and this type never reads the Unity clock itself.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 7.1. Requirements: R1.1, R1.3, R1.4, R1.5, R1.6, R1.12.</remarks>
public sealed class BasicAttackDriver
{
    /// <summary>
    /// A one-shot pending Toque_de_Ataque. Set by <see cref="QueueTap"/>, consumed to exactly one
    /// attack by the next <see cref="TryTakeAttack"/>, then cleared (R1.1/R1.5/R1.6).
    /// </summary>
    private bool _pendingTap;

    /// <summary>Whether Segurar_Ataque is currently active and authorizing repeats (R1.3).</summary>
    private bool _held;

    /// <summary>
    /// The unscaled timestamp of the most recently authorized attack, used to gate hold repeats by
    /// <c>attackInterval</c>. Only meaningful when <see cref="_hasLastAttack"/> is <c>true</c>.
    /// </summary>
    private float _lastAttackTime;

    /// <summary>
    /// <c>true</c> once at least one attack has been authorized since the last <see cref="Clear"/>.
    /// While <c>false</c>, the first hold attack is allowed immediately (R1.3).
    /// </summary>
    private bool _hasLastAttack;

    /// <summary>
    /// Queues a one-shot pending Toque_de_Ataque that the next <see cref="TryTakeAttack"/> consumes
    /// to exactly one attack (R1.1/R1.6). Releasing the control after the tap preserves that single
    /// authorized execution (R1.5): the tap is independent of <see cref="_held"/>, so it fires even
    /// without hold. Queuing again before consumption is idempotent — it stays a single pending tap,
    /// never a backlog of attacks.
    /// </summary>
    public void QueueTap()
    {
        _pendingTap = true;
    }

    /// <summary>
    /// Sets whether Segurar_Ataque is active. While held, <see cref="TryTakeAttack"/> authorizes one
    /// attack per <c>attackInterval</c> as long as the intent/target stays valid (R1.3). Passing
    /// <c>false</c> stops future hold repeats; it does <em>not</em> clear a pending tap and does not
    /// cut an already-started hit (that is <see cref="ReleaseHold"/>'s contract, R1.4).
    /// </summary>
    /// <param name="held"><c>true</c> to enable hold repeats; <c>false</c> to stop them.</param>
    public void SetHold(bool held)
    {
        _held = held;
    }

    /// <summary>
    /// Decides whether an attack is authorized right now, consuming at most one authorization per
    /// call. A pending Toque_de_Ataque takes priority and is honored even without hold, consuming to
    /// exactly one attack (returns <c>true</c> once, then <c>false</c> until another tap/hold
    /// authorizes) (R1.1/R1.5/R1.6). Otherwise, while held, returns <c>true</c> at most once per
    /// <paramref name="attackInterval"/>: the first hold attack is allowed immediately and later
    /// ones are gated by <paramref name="now"/> against the last-attack time (R1.3). With no pending
    /// tap and no hold it returns <c>false</c> — target existence alone never authorizes an attack
    /// (R1.6/R1.7/R1.12).
    /// </summary>
    /// <param name="now">
    /// An unscaled clock supplied by the coordinator. It must not advance during menu pause nor
    /// consume validity during hit-stop (R5.10); this type never reads the Unity clock itself.
    /// </param>
    /// <param name="attackInterval">
    /// The configured Intervalo_de_Ataque between consecutive hold attacks, in the same unit as
    /// <paramref name="now"/>. Values &lt;= 0 impose no gating (every held call authorizes).
    /// </param>
    /// <returns><c>true</c> when exactly one attack is authorized by this call; otherwise <c>false</c>.</returns>
    public bool TryTakeAttack(float now, float attackInterval)
    {
        // A pending tap wins and consumes to exactly one attack, even without hold (R1.1/R1.6).
        if (_pendingTap)
        {
            _pendingTap = false;
            MarkAttack(now);
            return true;
        }

        // Without a pending tap and not held, nothing authorizes an attack (R1.6/R1.12).
        if (!_held)
        {
            return false;
        }

        // While held: allow the first hold attack immediately, then gate by attackInterval (R1.3).
        if (!_hasLastAttack || attackInterval <= 0f || now - _lastAttackTime >= attackInterval)
        {
            MarkAttack(now);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Releases Segurar_Ataque: removes future hold repeats <em>without</em> cutting a hit that was
    /// already authorized/started by a tap or a prior hold cycle (R1.4/R5.11). Does not touch the
    /// last-attack timing, so an already-authorized execution is preserved.
    /// </summary>
    public void ReleaseHold()
    {
        _held = false;
    }

    /// <summary>
    /// Resets the pending tap, hold state and timing when the Intencao_de_Comando is invalidated —
    /// the target is lost (death, out of range, loss of line of sight) or an Ordem_de_Movimento
    /// overrides it (R1.12). After this, no attack is authorized until a new tap or hold arrives,
    /// and the next hold attack is again allowed immediately.
    /// </summary>
    public void Clear()
    {
        _pendingTap = false;
        _held = false;
        _hasLastAttack = false;
        _lastAttackTime = 0f;
    }

    /// <summary>
    /// Records that an attack was authorized at <paramref name="now"/>, so hold cadence resumes from
    /// this instant (a tap also reseeds the hold timer, keeping tap→hold transitions smooth).
    /// </summary>
    private void MarkAttack(float now)
    {
        _lastAttackTime = now;
        _hasLastAttack = true;
    }
}
