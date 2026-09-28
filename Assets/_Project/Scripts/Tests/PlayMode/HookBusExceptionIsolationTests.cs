using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R8 Property 22 of modifier-synergies-theme17
    /// (<see cref="HookBus"/> per-subscriber exception isolation).
    ///
    /// Property 22: for any set of subscribers on any hook event, with any subset that throws,
    /// raising the event SHALL invoke every subscriber and SHALL NOT propagate any exception to the
    /// code that raised the hook (Requirement 8.10). The bus iterates each event's invocation list
    /// and wraps every call in its own try/catch, logging a thrown subscriber via
    /// <see cref="Debug.LogException"/> instead of re-raising.
    ///
    /// The property is asserted across all seven event signatures. Because a thrown subscriber is
    /// logged (not swallowed silently), <see cref="LogAssert.ignoreFailingMessages"/> is enabled for
    /// the duration so the logged exceptions do not fail the test — the point under test is that the
    /// raiser never sees the exception and every subscriber still runs.
    ///
    /// Runs in PlayMode because most events carry an <see cref="Actor"/> (a MonoBehaviour) built
    /// through the shared rig.
    /// </summary>
    public sealed class HookBusExceptionIsolationTests
    {
        private HookBusTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new HookBusTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 22: raising any event with a random mix of
        // throwing and non-throwing subscribers invokes every subscriber and never propagates an
        // exception to the raiser. Validates: Requirements 8.10
        [UnityTest]
        public IEnumerator Property22_EverySubscriberInvoked_NoExceptionToRaiser()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 22);

            // Logged subscriber exceptions are expected; do not let them fail the test.
            LogAssert.ignoreFailingMessages = true;
            try
            {
                for (int c = 0; c < cases; c++)
                {
                    _rig.TearDown();
                    _rig = new HookBusTestRig();

                    var bus = new HookBus();

                    // 1..7 subscribers; each independently either throws or records that it ran.
                    int subscriberCount = 1 + rng.Next(0, 7);
                    var invoked = new bool[subscriberCount];
                    var throws = new bool[subscriberCount];
                    for (int s = 0; s < subscriberCount; s++)
                        throws[s] = rng.Next(0, 2) == 0;

                    // Guarantee at least one thrower so the isolation path is genuinely exercised.
                    throws[rng.Next(0, subscriberCount)] = true;

                    // Pick which of the seven events this case raises.
                    int which = rng.Next(0, 7);
                    Actor enemy = _rig.BuildEnemy();
                    float payload = (float)rng.NextDouble() * 100f;
                    var origin = new Vector3(
                        (float)rng.NextDouble() * 10f,
                        (float)rng.NextDouble() * 10f,
                        (float)rng.NextDouble() * 10f);

                    // Subscribe the mixed handlers to the chosen event, then raise it.
                    for (int s = 0; s < subscriberCount; s++)
                    {
                        int idx = s; // capture
                        Action run = () =>
                        {
                            invoked[idx] = true;
                            if (throws[idx]) throw new InvalidOperationException($"subscriber {idx} boom");
                        };
                        Subscribe(bus, which, run);
                    }

                    Assert.DoesNotThrow(() => Raise(bus, which, enemy, payload, origin),
                        $"case {c}: raising event {which} propagated an exception to the raiser");

                    for (int s = 0; s < subscriberCount; s++)
                        Assert.IsTrue(invoked[s],
                            $"case {c}: event {which} subscriber {s} was not invoked " +
                            $"(throws={throws[s]}) — a thrower must not block later subscribers");

                    yield return null;
                }
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        // Subscribes a signature-agnostic action to event index `which` (0..6), adapting the
        // action to each event's delegate signature.
        private static void Subscribe(HookBus bus, int which, Action run)
        {
            switch (which)
            {
                case 0: bus.OnCrit += (_, __) => run(); break;
                case 1: bus.OnKill += _ => run(); break;
                case 2: bus.OnFreeze += _ => run(); break;
                case 3: bus.OnBurn += _ => run(); break;
                case 4: bus.OnStanceBreak += _ => run(); break;
                case 5: bus.OnDash += () => run(); break;
                default: bus.OnExplosion += _ => run(); break;
            }
        }

        // Raises event index `which` (0..6) with the appropriate payload.
        private static void Raise(HookBus bus, int which, Actor enemy, float payload, Vector3 origin)
        {
            switch (which)
            {
                case 0: bus.RaiseCrit(enemy, payload); break;
                case 1: bus.RaiseKill(enemy); break;
                case 2: bus.RaiseFreeze(enemy); break;
                case 3: bus.RaiseBurn(enemy); break;
                case 4: bus.RaiseStanceBreak(enemy); break;
                case 5: bus.RaiseDash(); break;
                default: bus.RaiseExplosion(origin); break;
            }
        }
    }
}
