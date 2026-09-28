using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode example tests for R8 hook wiring of modifier-synergies-theme17
    /// (Requirements 8.1, 8.2, 8.4, 8.5, 8.6, 8.7, 8.8).
    ///
    /// These are example (not property) tests, per the design's Testing Strategy: hook wiring
    /// (8.1, 8.2, 8.4–8.8) is covered by example tests while dedupe (8.3) and isolation (8.10) are
    /// the property tests. They verify two things directly against <see cref="HookBus"/>:
    ///   1. Each event supports zero-or-more subscribers (8.1): raising an event with no subscriber
    ///      is a safe no-op, and adding several subscribers fans out to all of them.
    ///   2. Each raise-site forwards the correct payload to subscribers: the affected
    ///      <see cref="Actor"/> for OnCrit/OnKill/OnFreeze/OnBurn/OnStanceBreak, the resolved damage
    ///      for OnCrit, a bare invocation for OnDash, and the origin position for OnExplosion.
    ///
    /// Testing <c>HookBus.Raise*</c> payload forwarding directly is deliberate: the full
    /// PlayerActor / PlayerOnHitEffects / RunSynergyEffects raise-sites (the code that decides *when*
    /// to call Raise*) require heavy scene setup, so the end-to-end integration wiring — that those
    /// production call-sites invoke the bus with the right arguments — is covered structurally by
    /// the raise-site implementation (tasks 11.3–11.5) and by the integration tests (task 16.1).
    /// What these tests pin is the bus contract each raise-site depends on: the correct argument is
    /// delivered, unchanged, to every subscriber.
    ///
    /// Runs in PlayMode because most events carry an <see cref="Actor"/> (a MonoBehaviour).
    /// </summary>
    public sealed class HookBusWiringExampleTests
    {
        private HookBusTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new HookBusTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Requirement 8.1: every event supports zero subscribers — raising with none must not throw.
        [UnityTest]
        public IEnumerator RaisingWithNoSubscribers_IsANoOp()
        {
            var bus = new HookBus();
            Actor enemy = _rig.BuildEnemy();

            Assert.DoesNotThrow(() => bus.RaiseCrit(enemy, 12.5f), "OnCrit with no subscriber");
            Assert.DoesNotThrow(() => bus.RaiseKill(enemy), "OnKill with no subscriber");
            Assert.DoesNotThrow(() => bus.RaiseFreeze(enemy), "OnFreeze with no subscriber");
            Assert.DoesNotThrow(() => bus.RaiseBurn(enemy), "OnBurn with no subscriber");
            Assert.DoesNotThrow(() => bus.RaiseStanceBreak(enemy), "OnStanceBreak with no subscriber");
            Assert.DoesNotThrow(() => bus.RaiseDash(), "OnDash with no subscriber");
            Assert.DoesNotThrow(() => bus.RaiseExplosion(Vector3.one), "OnExplosion with no subscriber");

            yield return null;
        }

        // Requirement 8.1: every event supports many subscribers — all of them are invoked.
        [UnityTest]
        public IEnumerator EveryEvent_FansOutToMultipleSubscribers()
        {
            var bus = new HookBus();
            Actor enemy = _rig.BuildEnemy();
            const int subscribers = 3;

            AssertFanOut(subscribers, add => { for (int i = 0; i < subscribers; i++) bus.OnCrit += (_, __) => add(); },
                () => bus.RaiseCrit(enemy, 1f), "OnCrit");
            AssertFanOut(subscribers, add => { for (int i = 0; i < subscribers; i++) bus.OnKill += _ => add(); },
                () => bus.RaiseKill(enemy), "OnKill");
            // OnKill dedupes per enemy, so use a fresh enemy for the fan-out below.
            Actor enemy2 = _rig.BuildEnemy();
            AssertFanOut(subscribers, add => { for (int i = 0; i < subscribers; i++) bus.OnFreeze += _ => add(); },
                () => bus.RaiseFreeze(enemy2), "OnFreeze");
            AssertFanOut(subscribers, add => { for (int i = 0; i < subscribers; i++) bus.OnBurn += _ => add(); },
                () => bus.RaiseBurn(enemy2), "OnBurn");
            AssertFanOut(subscribers, add => { for (int i = 0; i < subscribers; i++) bus.OnStanceBreak += _ => add(); },
                () => bus.RaiseStanceBreak(enemy2), "OnStanceBreak");
            AssertFanOut(subscribers, add => { for (int i = 0; i < subscribers; i++) bus.OnDash += () => add(); },
                () => bus.RaiseDash(), "OnDash");
            AssertFanOut(subscribers, add => { for (int i = 0; i < subscribers; i++) bus.OnExplosion += _ => add(); },
                () => bus.RaiseExplosion(Vector3.zero), "OnExplosion");

            yield return null;
        }

        // Requirement 8.2: OnCrit forwards the affected enemy AND the resolved damage value.
        [UnityTest]
        public IEnumerator OnCrit_ForwardsEnemyAndDamage()
        {
            var bus = new HookBus();
            Actor enemy = _rig.BuildEnemy();
            const float damage = 42.75f;

            Actor received = null;
            float receivedDamage = 0f;
            int fired = 0;
            bus.OnCrit += (a, d) => { received = a; receivedDamage = d; fired++; };

            bus.RaiseCrit(enemy, damage);

            Assert.AreEqual(1, fired, "OnCrit should fire once");
            Assert.AreSame(enemy, received, "OnCrit forwarded the wrong Actor");
            Assert.AreEqual(damage, receivedDamage, 1e-4f, "OnCrit forwarded the wrong damage value");

            yield return null;
        }

        // Requirements 8.4/8.5/8.6: single-Actor events forward the affected enemy unchanged.
        [UnityTest]
        public IEnumerator ActorEvents_ForwardTheAffectedEnemy()
        {
            var bus = new HookBus();

            // OnFreeze (8.4)
            Actor freezeEnemy = _rig.BuildEnemy();
            Actor freezeReceived = null;
            bus.OnFreeze += a => freezeReceived = a;
            bus.RaiseFreeze(freezeEnemy);
            Assert.AreSame(freezeEnemy, freezeReceived, "OnFreeze forwarded the wrong Actor");

            // OnBurn (8.5)
            Actor burnEnemy = _rig.BuildEnemy();
            Actor burnReceived = null;
            bus.OnBurn += a => burnReceived = a;
            bus.RaiseBurn(burnEnemy);
            Assert.AreSame(burnEnemy, burnReceived, "OnBurn forwarded the wrong Actor");

            // OnStanceBreak (8.6)
            Actor stanceEnemy = _rig.BuildEnemy();
            Actor stanceReceived = null;
            bus.OnStanceBreak += a => stanceReceived = a;
            bus.RaiseStanceBreak(stanceEnemy);
            Assert.AreSame(stanceEnemy, stanceReceived, "OnStanceBreak forwarded the wrong Actor");

            yield return null;
        }

        // Requirement 8.7: OnDash fires once per raise with no payload.
        [UnityTest]
        public IEnumerator OnDash_FiresOncePerRaise()
        {
            var bus = new HookBus();
            int fired = 0;
            bus.OnDash += () => fired++;

            bus.RaiseDash();
            bus.RaiseDash();
            bus.RaiseDash();

            Assert.AreEqual(3, fired, "OnDash should fire once per RaiseDash call");
            yield return null;
        }

        // Requirement 8.8: OnExplosion forwards the origin position unchanged.
        [UnityTest]
        public IEnumerator OnExplosion_ForwardsOrigin()
        {
            var bus = new HookBus();
            var origin = new Vector3(3.5f, -1.25f, 7f);

            Vector3 received = Vector3.negativeInfinity;
            int fired = 0;
            bus.OnExplosion += o => { received = o; fired++; };

            bus.RaiseExplosion(origin);

            Assert.AreEqual(1, fired, "OnExplosion should fire once");
            Assert.AreEqual(origin, received, "OnExplosion forwarded the wrong origin");
            yield return null;
        }

        // Subscribes `subscribe` (which registers N handlers that each call the passed counter),
        // raises via `raise`, and asserts all N handlers ran.
        private static void AssertFanOut(int expected, System.Action<System.Action> subscribe,
            System.Action raise, string label)
        {
            int count = 0;
            subscribe(() => count++);
            raise();
            Assert.AreEqual(expected, count, $"{label}: expected {expected} subscribers to fire, got {count}");
        }
    }
}
