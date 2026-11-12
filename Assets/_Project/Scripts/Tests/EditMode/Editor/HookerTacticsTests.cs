using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>Etapa 2 (Hooker refeito): cadence of the hook and the "drag into danger" positioning.</summary>
    public sealed class HookerTacticsTests
    {
        [Test]
        public void HookWaitsForCooldownPlusRandomHoldAndNeverFiresOnTheRechargeFrame()
        {
            var rng = new System.Random(7);
            float minHold = float.MaxValue, maxHold = float.MinValue;
            for (int i = 0; i < 500; i++)
            {
                var cadence = new HookerCadence();
                cadence.OnHookThrown(10f, rng);
                float hold = cadence.ReadyAt - 10f;
                Assert.That(hold, Is.InRange(HookerCadence.HookCooldown, HookerCadence.HookCooldown + HookerCadence.HookRandomHoldMax));
                Assert.IsFalse(cadence.HookReady(10f + HookerCadence.HookCooldown - .01f));
                Assert.IsTrue(cadence.HookReady(10f + HookerCadence.HookCooldown + HookerCadence.HookRandomHoldMax));
                minHold = Mathf.Min(minHold, hold); maxHold = Mathf.Max(maxHold, hold);
            }
            Assert.Greater(maxHold - minHold, 1.5f, "The hold must actually vary between throws (unpredictable).");
        }

        [Test]
        public void FirstHookComesAfterAShortRandomDelayAndInitializesOnce()
        {
            var cadence = new HookerCadence();
            Assert.IsFalse(cadence.HookReady(0f), "Not ready before the encounter clock starts.");
            cadence.EnsureInitialized(3f, new System.Random(1));
            float first = cadence.ReadyAt;
            Assert.That(first - 3f, Is.InRange(HookerCadence.FirstHookDelayMin, HookerCadence.FirstHookDelayMax));
            cadence.EnsureInitialized(50f, new System.Random(2));
            Assert.AreEqual(first, cadence.ReadyAt, "Re-initialization must not reschedule.");
        }

        [Test]
        public void ChoiceUsesHookOnlyWhenReadyPossibleAndInBandOtherwiseChainStrikes()
        {
            Assert.AreEqual(EnemyAttackKind.HookLine, HookerCadence.Choose(true, true, 9f));
            Assert.AreEqual(EnemyAttackKind.HookLine, HookerCadence.Choose(true, true, HookerCadence.HookRange));
            Assert.IsNull(HookerCadence.Choose(true, true, HookerCadence.HookRange + .1f));
            // Recharging or impossible (escape window, blocked line): chain strikes cover mid range.
            Assert.AreEqual(EnemyAttackKind.ChainThrust, HookerCadence.Choose(false, true, 6f));
            Assert.AreEqual(EnemyAttackKind.ChainThrust, HookerCadence.Choose(true, false, 6f));
            Assert.IsNull(HookerCadence.Choose(true, false, 9f));
            // Close: sweep, and never a wasted hook.
            Assert.AreEqual(EnemyAttackKind.ChainSweep, HookerCadence.Choose(true, true, 3f));
            Assert.IsNull(HookerCadence.Choose(true, true, float.NaN));
        }

        [Test]
        public void PositionBehindAlliesKeepsTheHookInReachAndLandsThePlayerAmongThem()
        {
            Vector3 player = Vector3.zero, allies = new Vector3(0, 0, 7), self = new Vector3(3, 1, 5);
            Vector3 spot = HookerPositioning.BehindAnchor(player, allies, self, HookerPositioning.BehindAllies);
            Assert.AreEqual(1f, spot.y, 1e-4f, "Keeps the enemy's own height.");
            Assert.AreEqual(7f + HookerPositioning.BehindAllies, spot.z, 1e-3f, "Stands on the far side of the group.");
            // A pulled player stops 1 m in front of the Hooker: inside the ally group's 1.5 m neighbourhood.
            Vector3 landing = spot - (spot - player).normalized * HookPullModel.StopDistance; landing.y = 0;
            Assert.Less(Vector3.Distance(landing, allies), 1f);

            Vector3 far = HookerPositioning.BehindAnchor(player, new Vector3(0, 0, 30), self, 1.5f);
            Assert.LessOrEqual(new Vector2(far.x, far.z).magnitude, HookerPositioning.MaxDistanceFromPlayer + 1e-3f);
            Vector3 near = HookerPositioning.BehindAnchor(player, new Vector3(0, 0, .5f), self, 0f);
            Assert.GreaterOrEqual(new Vector2(near.x, near.z).magnitude, HookerPositioning.MinDistanceFromPlayer - 1e-3f);
        }
    }
}
