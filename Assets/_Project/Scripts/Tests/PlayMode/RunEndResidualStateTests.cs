using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R11 Property 26 of modifier-synergies-theme17
    /// (run-end leaves no residual state).
    ///
    /// After the atomic run-end clear (<c>RunBoons.ClearRunState</c>, the method
    /// <c>RunBoons.OnDestroy</c> calls), no run-scoped state survives into a later run:
    /// <list type="bullet">
    /// <item>the element registry is emptied — <see cref="PlayerOnHitEffects.HasAnyEffect"/> is false;</item>
    /// <item>the cascade ranks are zeroed — <see cref="RunSynergyEffects.HasModifiers"/> is false and
    /// every <see cref="RunSynergyEffects.Rank"/> is 0;</item>
    /// <item>the <see cref="HookBus"/> subscriptions are removed — a subscriber registered before the
    /// clear does not fire when the corresponding hook is raised after the clear;</item>
    /// <item>the escalation tier is reset — <see cref="SystemBreakState.Tier"/> is 0.</item>
    /// </list>
    ///
    /// The seam used to reach the production clear is documented on <see cref="RunEndClearTestRig"/>:
    /// the real <c>ClearRunState</c> is invoked (by reflection) on an inactive <c>RunBoons</c> whose
    /// private run-scoped members are wired to a configured player host, rather than re-implementing
    /// the three constituent clears in the test. Runs in PlayMode because the element registry and
    /// cascade engine are real MonoBehaviour components with status/component lookups.
    /// </summary>
    public sealed class RunEndResidualStateTests
    {
        private RunEndClearTestRig _rig;
        private GameObject _enemyGo;

        [TearDown]
        public void TearDown()
        {
            _rig?.TearDown();
            _rig = null;
            if (_enemyGo) Object.DestroyImmediate(_enemyGo);
            _enemyGo = null;
        }

        private Actor BuildEnemy()
        {
            _enemyGo = new GameObject("RunEndClearEnemy");
            _enemyGo.SetActive(false);
            _enemyGo.AddComponent<BoxCollider>().size = Vector3.one;
            Actor actor = _enemyGo.AddComponent<Actor>();
            actor.health = 1000f;
            _enemyGo.SetActive(true);
            return actor;
        }

        // Feature: modifier-synergies-theme17, Property 26: the atomic run-end clear empties the
        // element registry, zeroes cascade ranks, removes HookBus subscriptions (a prior-run
        // subscriber never fires afterward), and resets the escalation tier.
        // Validates: Requirements 8.9, 10.5, 11.4
        [UnityTest]
        public IEnumerator Property26_RunEndLeavesNoResidualState()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 26);

            for (int c = 0; c < cases; c++)
            {
                _rig?.TearDown();
                if (_enemyGo) Object.DestroyImmediate(_enemyGo);
                _enemyGo = null;

                _rig = new RunEndClearTestRig();
                _rig.Build();

                // Arm a rich run: burn + chill, and randomized cascade ranks (at least one so
                // HasModifiers is true before the clear).
                int detonation = rng.Next(0, 4);
                int conductor = rng.Next(0, 4);
                int reactor = rng.Next(0, 4);
                int resonance = rng.Next(0, 4);
                if (detonation + conductor + reactor + resonance == 0) detonation = 1;
                _rig.ArmRunState(detonation, conductor, reactor, resonance);

                // Register subscribers on several hooks; each flips its own fired flag.
                bool burnFired = false, killFired = false, critFired = false, explosionFired = false;
                _rig.Hooks.OnBurn += _ => burnFired = true;
                _rig.Hooks.OnKill += _ => killFired = true;
                _rig.Hooks.OnCrit += (_, __) => critFired = true;
                _rig.Hooks.OnExplosion += _ => explosionFired = true;

                // Raise the escalation tier to a non-zero value (thresholds 3/6/9).
                int interacting = 3 + rng.Next(0, 8); // 3..10 -> tier 1..3
                _rig.RaiseEscalationTier(interacting);
                Assert.Greater(_rig.SystemBreak.Tier, 0, $"case {c}: tier should be raised before the clear");

                Actor enemy = BuildEnemy();

                // Pre-clear sanity: state exists and subscribers fire.
                Assert.IsTrue(_rig.Effects.HasAnyEffect, $"case {c}: element registry should have effects before clear");
                Assert.IsTrue(_rig.Synergies.HasModifiers, $"case {c}: cascade ranks should be set before clear");
                _rig.Hooks.RaiseBurn(enemy);
                Assert.IsTrue(burnFired, $"case {c}: OnBurn subscriber should fire before the clear");
                burnFired = false;

                // Atomic run-end clear (the production method).
                _rig.InvokeRunEndClear();

                // Element registry emptied.
                Assert.IsFalse(_rig.Effects.HasAnyEffect,
                    $"case {c}: element registry not emptied by run-end clear");

                // Cascade ranks zeroed.
                Assert.IsFalse(_rig.Synergies.HasModifiers,
                    $"case {c}: cascade still reports modifiers after run-end clear");
                Assert.AreEqual(0, _rig.Synergies.Rank(RunSynergy.Detonation), $"case {c}: Detonation rank not zeroed");
                Assert.AreEqual(0, _rig.Synergies.Rank(RunSynergy.Conductor), $"case {c}: Conductor rank not zeroed");
                Assert.AreEqual(0, _rig.Synergies.Rank(RunSynergy.Reactor), $"case {c}: Reactor rank not zeroed");
                Assert.AreEqual(0, _rig.Synergies.Rank(RunSynergy.Resonance), $"case {c}: Resonance rank not zeroed");

                // HookBus subscriptions removed: no prior-run subscriber fires after the clear.
                _rig.Hooks.RaiseBurn(enemy);
                _rig.Hooks.RaiseKill(enemy);
                _rig.Hooks.RaiseCrit(enemy, 5f);
                _rig.Hooks.RaiseExplosion(enemy.transform.position);
                Assert.IsFalse(burnFired, $"case {c}: prior-run OnBurn subscriber fired after clear");
                Assert.IsFalse(killFired, $"case {c}: prior-run OnKill subscriber fired after clear");
                Assert.IsFalse(critFired, $"case {c}: prior-run OnCrit subscriber fired after clear");
                Assert.IsFalse(explosionFired, $"case {c}: prior-run OnExplosion subscriber fired after clear");

                // Escalation tier reset.
                Assert.AreEqual(0, _rig.SystemBreak.Tier, $"case {c}: escalation tier not reset by run-end clear");

                yield return null;
            }
        }
    }
}
