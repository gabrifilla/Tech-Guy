using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode smoke tests for the ranged-kiting-and-attack-telegraph-overhaul feature — task 12.2.
    ///
    /// Unlike the EditMode example tests (where <c>Time.deltaTime</c> is 0 so the windup loop cannot be
    /// drained to its natural impact), these run under the live player loop so a telegraphed attack can
    /// actually advance to impact. Two behaviors are smoke-tested against live Unity objects:
    ///
    ///  - R6.6: a telegraphed <see cref="EnemyAttackExecution"/> creates BOTH a ring outline and a
    ///    filled surface during windup, then clears both in the same frame on impact (and on cancel).
    ///  - R1.2 / R1.4: the kiting speed swap/restore the <c>EnemyAI</c> kiting layer applies to a live
    ///    <see cref="NavMeshAgent"/> lowers <c>agent.speed</c> to the retreat speed while kiting and
    ///    restores the full chase speed within one frame on exit. The decision is owned by the pure
    ///    <see cref="KiteController"/> / <see cref="RetreatCadence"/> the layer consults; this drives
    ///    those against a real agent's <c>speed</c> field so the swap/restore is exercised end to end.
    /// </summary>
    public sealed class KitingStandoffPlayModeTests
    {
        private ArsenalProjectileTestRig _rig;
        private EnemyAttackExecution _execution;
        private Actor _target;
        private GameObject _agentHost;

        [SetUp]
        public void SetUp()
        {
            _rig = new ArsenalProjectileTestRig();
            _target = _rig.BuildEnemy(Vector3.zero, 1000);
            _execution = new EnemyAttackExecution();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1;
            _execution?.Cancel();
            if (_agentHost) Object.DestroyImmediate(_agentHost);
            // Destroy any telegraph helpers that may still linger between cases.
            foreach (var fill in Object.FindObjectsByType<CombatGroundFill>(FindObjectsSortMode.None))
                if (fill) Object.DestroyImmediate(fill.gameObject);
            foreach (var ring in Object.FindObjectsByType<CombatGroundRing>(FindObjectsSortMode.None))
                if (ring) Object.DestroyImmediate(ring.gameObject);
            _rig.TearDown();
        }

        private static EnemyAttackArea Circle =>
            new EnemyAttackArea(EnemyAttackShape.Circle, Vector3.zero, Vector3.forward, 3);

        private static CombatGroundFill[] Fills() =>
            Object.FindObjectsByType<CombatGroundFill>(FindObjectsSortMode.None);

        private static CombatGroundRing[] Rings() =>
            Object.FindObjectsByType<CombatGroundRing>(FindObjectsSortMode.None);

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 12.2 PlayMode smoke
        // A telegraphed attack creates both a ring and a fill during windup, then clears BOTH on impact.
        // Driven under the live player loop so the windup actually advances to its impact + same-frame
        // clear (which EditMode cannot reach because Time.deltaTime is 0 there).
        // Validates: Requirements 6.6
        [UnityTest]
        public IEnumerator TelegraphedAttack_CreatesThenClearsRingAndFill()
        {
            var routine = _execution.Execute(new[] { Circle }, _target, windup: .2f, damage: 10f,
                color: Color.red, canAttack: () => true, impactHold: 0f);

            // Pump the first frame: the windup is live and both the ring and the fill are rendered.
            Assert.That(routine.MoveNext(), Is.True, "the windup must yield at least one frame");
            Assert.That(_execution.IsWindingUp, Is.True, "the attack must be winding up");
            Assert.That(Rings().Length, Is.GreaterThanOrEqualTo(1), "a ring must render during windup (R6.6)");
            Assert.That(Fills().Length, Is.GreaterThanOrEqualTo(1), "a fill must render during windup (R6.6)");

            // Let the windup run to impact under the live player loop.
            yield return routine;

            Assert.That(_execution.Completed, Is.True, "the attack must reach impact");
            Assert.That(_execution.IsWindingUp, Is.False, "winding up must end at impact");
            // On impact ClearVisuals tears down ring + fill together; FindObjects excludes the destroyed/
            // deactivated helpers, so both collections are empty (R6.6).
            Assert.That(Rings(), Is.Empty, "the ring must be cleared on impact (R6.6)");
            Assert.That(Fills(), Is.Empty, "the fill must be cleared on impact in the same frame (R6.6)");
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 12.2 PlayMode smoke
        // Cancelling mid-windup clears both the ring and the fill in the same frame on a live object.
        // Validates: Requirements 6.6
        [UnityTest]
        public IEnumerator CancelMidWindup_ClearsRingAndFillSameFrame()
        {
            var routine = _execution.Execute(new[] { Circle }, _target, windup: 1f, damage: 10f,
                color: Color.red, canAttack: () => true, impactHold: 0f);

            Assert.That(routine.MoveNext(), Is.True);
            Assert.That(Rings().Length, Is.GreaterThanOrEqualTo(1), "ring renders mid-windup");
            Assert.That(Fills().Length, Is.GreaterThanOrEqualTo(1), "fill renders mid-windup");

            _execution.Cancel();
            yield return null;

            Assert.That(_execution.IsWindingUp, Is.False, "cancel stops the windup");
            Assert.That(Rings(), Is.Empty, "cancel clears the ring in the same frame (R6.6)");
            Assert.That(Fills(), Is.Empty, "cancel clears the fill in the same frame (R6.6)");
            Assert.That(_target.health, Is.EqualTo(1000), "a cancelled attack deals no damage");
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, task 12.2 PlayMode smoke
        // The kiting layer lowers a live NavMeshAgent's speed to the retreat speed while KiteController
        // permits kiting, and restores the full chase speed within one frame once it stops. This drives
        // the exact pure calls EnemyAI.TryMaintainStandoff makes against a real agent's speed field.
        // Validates: Requirements 1.2, 1.4
        [UnityTest]
        public IEnumerator KitingLayer_SetsRetreatSpeedThenRestoresChaseSpeedOnLiveAgent()
        {
            _agentHost = new GameObject("KitingAgent");
            _agentHost.SetActive(false);
            var agent = _agentHost.AddComponent<NavMeshAgent>();
            _agentHost.SetActive(true);

            const float chaseSpeed = 6f;        // a representative resolved EnemyVariant.ChaseSpeed
            const float retreatMultiplier = 0.5f; // KitingConfig default
            const float retreatWindow = 3f;
            const float retreatCooldown = 2f;
            const float dt = 0.1f;

            agent.speed = chaseSpeed;

            var kite = new KiteController();

            // Frame 1: player inside standoff => wantsToKite. Entering Kiting lowers the agent speed to
            // the retreat speed, exactly as TryMaintainStandoff does on the permit frame (R1.2).
            bool kiting = kite.Tick(wantsToKite: true, dt, retreatWindow, retreatCooldown);
            Assert.That(kiting, Is.True, "the first wants-to-kite frame permits kiting");
            if (kiting) agent.speed = RetreatCadence.RetreatSpeed(chaseSpeed, retreatMultiplier);
            yield return null;

            float expectedRetreat = chaseSpeed * RetreatCadence.ClampRetreatMultiplier(retreatMultiplier);
            Assert.That(agent.speed, Is.EqualTo(expectedRetreat).Within(1e-4f),
                "the live agent speed must drop to the retreat speed while kiting (R1.2)");
            Assert.That(agent.speed, Is.LessThan(chaseSpeed), "retreat speed is strictly below chase speed");

            // Drive continuous kiting until the retreat window elapses; the exit frame denies kiting,
            // which is where TryMaintainStandoff restores the chase speed.
            bool permitted = true;
            int maxFrames = Mathf.CeilToInt(retreatWindow / dt) + 5;
            for (int frame = 0; frame < maxFrames && permitted; frame++)
            {
                permitted = kite.Tick(wantsToKite: true, dt, retreatWindow, retreatCooldown);
                if (permitted) agent.speed = RetreatCadence.RetreatSpeed(chaseSpeed, retreatMultiplier);
                else agent.speed = chaseSpeed; // restore within one frame on exit (R1.4)
                yield return null;
            }

            Assert.That(permitted, Is.False, "continuous kiting must exit within the bounded window");
            Assert.That(agent.speed, Is.EqualTo(chaseSpeed).Within(1e-4f),
                "the live agent speed must be restored to the full chase speed within one frame on exit (R1.4)");
        }
    }
}
