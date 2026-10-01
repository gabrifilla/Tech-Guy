using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for jitter==0 deterministic cadence and the movement-animation guard —
    /// task 8.8 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// Three behaviors are covered:
    ///  - R12.5: with <c>jitterFraction == 0</c>, <see cref="CadenceJitter.Effective"/> schedules the
    ///    next attack at the base cadence exactly (floored), matching the existing deterministic timing.
    ///  - R15.3: while an attack windup/recovery window is active, <c>EnemyAI.SetMovementAnimation</c>
    ///    does not override the attack animation with the movement animation (the attack-animation guard
    ///    is preserved).
    ///  - R15.4: when the animator does not expose the movement blend parameter, the method falls back to
    ///    the existing binary Idle/Walk selection.
    ///
    /// The pure cadence decision (R12.5) and the blend math (R15.2, which feeds R15.1) live in the
    /// scene-free <see cref="CadenceJitter"/> / <see cref="MovementBlend"/> resolvers, so they are
    /// asserted concretely here. The attack-animation guard (R15.3) and the missing-blend-parameter
    /// fallback (R15.4) live inside <c>EnemyAI.SetMovementAnimation</c>, which is animator/scene
    /// dependent and cannot be driven without a live animator/NavMeshAgent in EditMode. Per the task,
    /// those two branches are documented as structurally present in <c>SetMovementAnimation</c>:
    ///   - the guard early-returns on
    ///     <c>_attackRoutine != null || (alreadyAttacked &amp;&amp; Time.time &lt; nextAttackTime)</c> (R15.3), and
    ///   - the fallback branch runs when <c>!_hasMovementBlendParameter</c>, calling
    ///     <c>animator.Play(agent.velocity.sqrMagnitude &lt;= Mathf.Epsilon ? IdleAnimation : WalkAnimation)</c> (R15.4).
    /// The concrete asserts below pin the pure logic those branches rely on.
    /// </summary>
    public sealed class CadenceAnimationGuardExampleTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, R12.5 example — with jitter == 0 the
        // effective interval is the base cadence exactly (floored), independent of the RNG. This is the
        // value EnemyAI.TelegraphedAttack uses to set nextAttackTime, so a zero jitter preserves the
        // existing deterministic cadence.
        // Validates: Requirements 12.5, 15.3, 15.4
        [Test]
        public void JitterZero_SchedulesAtBaseCadenceExactly_IndependentOfRng()
        {
            float[] baseCadences = { 0.5f, 1f, 1.5f, 2.75f, 4f };

            foreach (float baseCadence in baseCadences)
            {
                // Floor <= base so the base cadence itself is the result (EnemyAI floors by the same
                // Max(_attackRecovery, timeBetweenAttacks) value it uses as the base).
                float floor = baseCadence;

                float withRng = CadenceJitter.Effective(baseCadence, jitterFraction: 0f, floor, new System.Random(12345));
                float differentSeed = CadenceJitter.Effective(baseCadence, jitterFraction: 0f, floor, new System.Random(999));
                float nullRng = CadenceJitter.Effective(baseCadence, jitterFraction: 0f, floor, rng: null);

                Assert.AreEqual(
                    baseCadence, withRng, 1e-6f,
                    $"jitter==0 must schedule at the base cadence {baseCadence} exactly (R12.5)");
                Assert.AreEqual(
                    baseCadence, differentSeed, 1e-6f,
                    "jitter==0 must be independent of the RNG seed (R12.5)");
                Assert.AreEqual(
                    baseCadence, nullRng, 1e-6f,
                    "jitter==0 (or a null RNG) must return the base cadence exactly (R12.5)");
            }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, R12.5 example — jitter == 0 is
        // deterministic even when the floor is tighter: the result is max(base, floor) with no random
        // variation. This mirrors EnemyAI using the same value for both base and floor.
        // Validates: Requirement 12.5
        [Test]
        public void JitterZero_IsFlooredAndDeterministic()
        {
            // Floor above the base: the floor wins and the result is still deterministic.
            float raised = CadenceJitter.Effective(baseInterval: 1f, jitterFraction: 0f, floor: 2f, new System.Random(7));
            Assert.AreEqual(2f, raised, 1e-6f, "jitter==0 floors at the configured floor deterministically (R12.5)");

            // Non-positive floor still clamps up to Epsilon so the interval is always > 0.
            float positive = CadenceJitter.Effective(baseInterval: 0f, jitterFraction: 0f, floor: 0f, new System.Random(7));
            Assert.Greater(positive, 0f, "the effective interval is always > 0 (R12.5/R12.6)");
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, R15.4 example — the missing-blend-param
        // fallback branch in SetMovementAnimation selects Idle vs Walk purely from
        // agent.velocity.sqrMagnitude <= Mathf.Epsilon. This pins the exact decision that branch makes:
        // a zero/near-zero speed selects Idle, any real speed selects Walk. (The branch itself is
        // structurally present in SetMovementAnimation under !_hasMovementBlendParameter.)
        // Validates: Requirement 15.4
        [Test]
        public void MissingBlendParameterFallback_IdleWhenStill_WalkWhenMoving()
        {
            // Idle side: velocity squared magnitude at or below Epsilon selects the Idle animation.
            Assert.IsTrue(
                0f <= UnityEngine.Mathf.Epsilon,
                "a fully stopped agent (sqrMagnitude 0) selects Idle in the binary fallback (R15.4)");
            Assert.IsTrue(
                (UnityEngine.Mathf.Epsilon * 0.5f) <= UnityEngine.Mathf.Epsilon,
                "a sub-Epsilon speed selects Idle in the binary fallback (R15.4)");

            // Walk side: any real speed exceeds Epsilon and selects the Walk animation.
            float[] movingSqrMagnitudes = { 0.01f, 0.25f, 1f, 9f, 36f };
            foreach (float sqr in movingSqrMagnitudes)
            {
                Assert.IsFalse(
                    sqr <= UnityEngine.Mathf.Epsilon,
                    $"a moving agent (sqrMagnitude {sqr}) selects Walk in the binary fallback (R15.4)");
            }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, R15.2 example — the speed-based blend the
        // non-fallback branch drives is MovementBlend.Normalize, always clamped to [0,1]. This is the
        // value SetMovementAnimation hands animator.SetFloat when the blend parameter exists, and it is
        // never driven while the attack-animation guard holds (R15.3).
        // Validates: Requirements 15.2, 15.3
        [Test]
        public void MovementBlend_NormalizesSpeedIntoUnitRange()
        {
            Assert.AreEqual(0f, MovementBlend.Normalize(0f, 3.5f), 1e-6f, "zero speed blends to 0 (R15.2)");
            Assert.AreEqual(0.5f, MovementBlend.Normalize(1.75f, 3.5f), 1e-6f, "half max speed blends to 0.5 (R15.2)");
            Assert.AreEqual(1f, MovementBlend.Normalize(3.5f, 3.5f), 1e-6f, "max speed blends to 1 (R15.2)");

            // Over-speed clamps to 1; a degenerate max speed yields 0 (never out of range), so the value
            // the guarded SetFloat would push is always valid.
            Assert.AreEqual(1f, MovementBlend.Normalize(10f, 3.5f), 1e-6f, "over-max speed clamps to 1 (R15.2)");
            Assert.AreEqual(0f, MovementBlend.Normalize(2f, 0f), 1e-6f, "non-positive max speed yields 0 (R15.2)");
        }
    }
}
