/// <summary>
/// Pure, scene-free decision gate for the conditional-interruption rule (Requisitos 7.6, 7.7 /
/// Property 23).
///
/// An interruption ability only cancels an enemy attack while that attack is inside its
/// <b>interruptible window</b> — the telegraph windup, before the damage beat resolves
/// (<see cref="EnemyAI.IsWindingUp"/> / <see cref="EnemyAttackExecution.IsWindingUp"/>). The rule is:
/// <list type="bullet">
///   <item>WHILE the attack is in the interruptible window: cancel the attack and suppress its pending
///   damage beat in the same simulation frame (Requisito 7.6);</item>
///   <item>OTHERWISE (no attack, or the beat has already resolved / recovery): preserve the attack,
///   do NOT suppress the beat, and signal an "absence of interruption" reaction to the player
///   (Requisito 7.7).</item>
/// </list>
///
/// This mirrors the pure interrupt predicate proven for the archetype telegraph pipeline
/// (<c>beatApplies == windupComplete &amp;&amp; !interrupted</c>, see <c>TelegraphBeat</c> /
/// InterruptCancellationPropertyTests): an interrupt that lands strictly before the beat frame
/// cancels the whole attack same-frame, whereas an interrupt after the beat has nothing to cancel.
/// Extracted as a static, scene-free predicate so Property 23 can exercise it without a live scene
/// (mirrors <see cref="ControlLockGate"/> / <see cref="BreakEffectResistance"/>). The owning
/// <see cref="EnemyAI"/> keeps the state (its <c>IsWindingUp</c> flag) and simply asks this gate what
/// an incoming interruption should do this frame.
/// </summary>
public static class InterruptWindowGate
{
    /// <summary>
    /// The outcome of applying an interruption to an enemy this frame (Requisitos 7.6, 7.7). All three
    /// flags are derived deterministically from whether the attack was in its interruptible window, so
    /// they are mutually consistent: an in-window hit cancels and suppresses without signalling
    /// no-interruption, and an out-of-window hit does the exact opposite.
    /// </summary>
    public readonly struct Decision
    {
        /// <summary>True when the in-progress attack must be cancelled this frame (Requisito 7.6).</summary>
        public bool CancelsAttack { get; }

        /// <summary>True when the attack's pending damage beat must be suppressed same-frame (Requisito 7.6).</summary>
        public bool SuppressesBeat { get; }

        /// <summary>
        /// True when no interruption happened and the game should show the "no-interruption" feedback,
        /// preserving the attack and its beat (Requisito 7.7).
        /// </summary>
        public bool SignalsNoInterruption { get; }

        public Decision(bool cancelsAttack, bool suppressesBeat, bool signalsNoInterruption)
        {
            CancelsAttack = cancelsAttack;
            SuppressesBeat = suppressesBeat;
            SignalsNoInterruption = signalsNoInterruption;
        }
    }

    /// <summary>An in-window interruption: cancel the attack and suppress its beat (Requisito 7.6).</summary>
    public static readonly Decision Interrupted = new Decision(cancelsAttack: true, suppressesBeat: true, signalsNoInterruption: false);

    /// <summary>An out-of-window interruption: preserve the attack/beat and signal no-interruption (Requisito 7.7).</summary>
    public static readonly Decision NotInterrupted = new Decision(cancelsAttack: false, suppressesBeat: false, signalsNoInterruption: true);

    /// <summary>
    /// Resolves what an incoming interruption does given whether the target's attack is currently in its
    /// interruptible window (Requisitos 7.6, 7.7 / Property 23).
    ///
    /// In the window the attack is cancelled and its pending beat suppressed in the same frame; outside
    /// it the attack is preserved, the beat is not suppressed, and a no-interruption reaction is
    /// signalled. The two outcomes are exclusive and exhaustive.
    /// </summary>
    /// <param name="attackInInterruptibleWindow">
    /// True while the target enemy is winding up an attack whose damage beat has not yet resolved.
    /// </param>
    public static Decision Resolve(bool attackInInterruptibleWindow)
        => attackInInterruptibleWindow ? Interrupted : NotInterrupted;
}
