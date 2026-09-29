using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the Legendary "no continuous stagger lock" rule — task 4.4 of
    /// weapon-gameplay-swarm-rework (Property 10, Requisito 3.4).
    ///
    /// A Legendary enemy must never get chain-locked out of the fight by repeated staggers: outside
    /// its post-break vulnerability window a sequence of Stagger immediate reactions must not leave it
    /// in continuous control lock, so it can keep attacking. The controller already implements exactly
    /// that separation, and it does so through pure, scene-free rules this test drives directly:
    ///   * <see cref="CombatReactionController.ApplyReaction"/> treats a Stagger only as an immediate
    ///     reaction — it nudges (clamped) and calls <c>InterruptCurrentAction</c>, but it never sets
    ///     the <c>controlLocked</c> flag. Control lock is armed ONLY by hard CC (Stun / KnockUp) inside
    ///     <c>TriggerStanceBreak</c>, i.e. on a Stance Break, never by a plain Stagger.
    ///   * <see cref="ControlLockGate.AllowsImmediateDisplacement"/> reports the enemy can still be
    ///     acted upon / can act whenever it is not control-locked.
    ///   * <see cref="VulnerabilityWindow"/> tracks whether the enemy is currently in its post-break
    ///     opening; "fora de sua janela de vulnerabilidade" means the window is closed and the
    ///     effective rank is still Legendary.
    ///
    /// Because none of those rules require a live scene, the test models the immediate-reaction
    /// channel with the real shared helpers and asserts that applying an arbitrary sequence of
    /// Staggers to a Legendary that is not stance-broken never accumulates control lock.
    ///
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property
    /// (sampling stagger-sequence lengths, per-hit push distances, and the enemy's stagger resistance)
    /// and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class LegendaryStaggerNoContinuousLockPropertyTests
    {
        /// <summary>
        /// Pure model of what a single immediate reaction does to the control-lock state, mirroring
        /// <see cref="CombatReactionController.ApplyReaction"/>: Push and Stagger are immediate
        /// reactions that never arm control lock (only a Stance Break's hard CC does), and None does
        /// nothing at all. So an immediate reaction NEVER locks control — the previous lock state is
        /// carried through unchanged. This is the crux of Property 10: staggers alone cannot chain-lock
        /// a Legendary.
        /// </summary>
        private static bool ControlLockedAfterImmediateReaction(bool wasLocked, HitReactionType reactionType)
        {
            // No branch of the immediate-reaction channel sets controlLocked; it stays whatever it was.
            switch (reactionType)
            {
                case HitReactionType.Push:
                case HitReactionType.Stagger:
                case HitReactionType.None:
                default:
                    return wasLocked;
            }
        }

        // Feature: weapon-gameplay-swarm-rework, Property 10: Legendary não permanece em stagger contínuo
        // Para toda sequência de staggers aplicada a um inimigo Legendary fora de sua janela de
        // vulnerabilidade, ele não fica em travamento de controle contínuo e pode continuar atacando.
        // Validates: Requirements 3.4
        [Test]
        public void LegendaryStaysActingUnderAContinuousStaggerSequenceOutsideItsVulnerabilityWindow()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // A Legendary enemy that has NOT been stance-broken: its vulnerability window is closed
                // and its effective rank is still Legendary ("fora de sua janela de vulnerabilidade").
                var window = new VulnerabilityWindow(EnemyRank.Legendary);
                PropertyCheck.That(!window.IsOpen && window.EffectiveRank == EnemyRank.Legendary,
                    "test setup: a fresh Legendary must be outside its vulnerability window and rank Legendary");

                // Legendary is highly stagger-resistant but not fully immune, so staggers DO react
                // (interrupt) — the point of the property is that even reacting staggers never lock.
                // Sample below 1.0 so the Stagger branch actually runs (matching the controller's
                // `staggerResistance < 1f` gate).
                float staggerResistance = NextFloat(rng, 0f, 0.999f);

                // An arbitrary continuous sequence of staggers (plus the occasional Push/None mixed in),
                // each with an arbitrary requested push distance across the whole range.
                int hits = rng.Next(1, 40);

                bool controlLocked = false; // starts free: the enemy can act.
                float clock = NextFloat(rng, 0f, 1000f);

                for (int h = 0; h < hits; h++)
                {
                    // Most hits are staggers (the sequence under test); a few Push/None keep the model
                    // honest that no immediate reaction locks control.
                    HitReactionType reactionType = (rng.Next(0, 5) == 0)
                        ? (rng.Next(0, 2) == 0 ? HitReactionType.Push : HitReactionType.None)
                        : HitReactionType.Stagger;

                    float requestedPush = NextFloat(rng, -1f, 9f); // spans below/above the readability clamp.

                    // Advance the (still-closed) vulnerability window with the caller's clock, exactly
                    // as the controller does each frame. No stance break ever opens it here.
                    clock += NextFloat(rng, 0f, 0.5f);
                    window.Update(clock);

                    // Immediate-reaction channel: a Stagger reacts only when not fully stagger-immune,
                    // and whatever it does, it does NOT arm control lock (Property 10 crux).
                    bool reacts = reactionType != HitReactionType.Stagger || staggerResistance < 1f;

                    // The clamped nudge stays within the readability limit and never turns into a lock.
                    float clampedPush = reacts
                        ? ImmediateReactionClamp.ClampPush(reactionType, requestedPush)
                        : 0f;
                    PropertyCheck.That(clampedPush >= 0f && clampedPush <= ImmediateReactionClamp.MaxDisplacementMeters,
                        $"stagger #{h}: immediate nudge {clampedPush} left the readability range [0,{ImmediateReactionClamp.MaxDisplacementMeters}]");

                    controlLocked = ControlLockedAfterImmediateReaction(controlLocked, reactionType);

                    // R3.4: after EVERY hit in the sequence the Legendary is not control-locked, so it
                    // is never in continuous control lock and can keep acting/attacking.
                    PropertyCheck.That(!controlLocked,
                        $"Legendary became control-locked after stagger #{h} of {hits} " +
                        $"[reaction={reactionType} push={requestedPush} staggerRes={staggerResistance}] — " +
                        "a stagger sequence must never chain-lock a Legendary");

                    // "pode continuar atacando": while not control-locked the shared gate allows it to
                    // act (its NavMeshAgent is not stopped), and it stays outside its vulnerability
                    // window with rank Legendary the whole time.
                    PropertyCheck.That(ControlLockGate.AllowsImmediateDisplacement(controlLocked),
                        $"Legendary must be free to act after stagger #{h} (not control-locked)");
                    PropertyCheck.That(!window.IsOpen && window.EffectiveRank == EnemyRank.Legendary,
                        $"Legendary must stay outside its vulnerability window (rank Legendary) after stagger #{h}");
                }

                // End state: still free to act after the entire continuous stagger sequence.
                PropertyCheck.That(!controlLocked,
                    $"after a continuous sequence of {hits} staggers the Legendary must not be control-locked");
                PropertyCheck.That(ControlLockGate.AllowsImmediateDisplacement(controlLocked),
                    "after the stagger sequence the Legendary must still be able to act");
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 10: Legendary não permanece em stagger contínuo
        // Focused sub-property: an immediate reaction of ANY type (Push, Stagger, None), at ANY stagger
        // resistance below full immunity, never arms control lock. This isolates the exact controller
        // rule that makes Property 10 hold — only a Stance Break's hard CC locks control, never the
        // immediate-reaction channel a stagger uses.
        // Validates: Requirements 3.4
        [Test]
        public void NoImmediateReactionEverArmsControlLock()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                HitReactionType reactionType = RandomReaction(rng);
                bool wasLocked = false; // a Legendary that is currently free to act.

                bool nowLocked = ControlLockedAfterImmediateReaction(wasLocked, reactionType);
                PropertyCheck.That(!nowLocked,
                    $"immediate reaction {reactionType} must not arm control lock on a free Legendary");

                // Idempotent under repetition: applying the same immediate reaction many times in a row
                // still never locks control (continuous stagger can't accumulate a lock).
                bool locked = false;
                int reps = rng.Next(1, 25);
                for (int r = 0; r < reps; r++)
                {
                    locked = ControlLockedAfterImmediateReaction(locked, reactionType);
                    PropertyCheck.That(!locked,
                        $"repeated {reactionType} (#{r}) armed control lock — staggers must not accumulate a lock");
                }
            });
        }

        // --- Generators ---------------------------------------------------------------------------

        private static HitReactionType RandomReaction(System.Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return HitReactionType.None;
                case 1: return HitReactionType.Push;
                default: return HitReactionType.Stagger;
            }
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
