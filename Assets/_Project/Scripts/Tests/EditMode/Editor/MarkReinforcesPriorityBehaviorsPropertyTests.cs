using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the priority-reinforcement rule in
    /// <see cref="WeaponMark.ResolvePriorityTarget"/> — task 12.2 of weapon-gameplay-swarm-rework
    /// (Requisito 10.3 / Property 34).
    ///
    /// The Bow's exclusive Mark mechanic (R10.2) is modelled by the pure, scene-free
    /// <see cref="WeaponMark"/> so this cross-cutting invariant can be property-checked without a
    /// live Unity scene — the scene-side coordinator wraps a concrete enemy in an
    /// <see cref="IMarkTarget"/> adapter and reuses <see cref="PriorityTargetMarker"/> for the
    /// legibility. The project cannot resolve FsCheck/CsCheck packages on this machine, so the
    /// agreed seeded harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated
    /// cases (every priority behavior × reinforcement flag combination, with a live mark present
    /// alongside a distinct default target) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class MarkReinforcesPriorityBehaviorsPropertyTests
    {
        /// <summary>
        /// Minimal reference-typed <see cref="IMarkTarget"/> stand-in. Targets are compared by
        /// reference identity by <see cref="WeaponMark"/>, so two distinct instances (a marked
        /// target and a default target) are unambiguously different.
        /// </summary>
        private sealed class FakeMarkTarget : IMarkTarget
        {
            public FakeMarkTarget(bool isAlive) => IsAlive = isAlive;

            public bool IsAlive { get; }
        }

        private static readonly PriorityBehavior[] Behaviors =
        {
            PriorityBehavior.Homing,
            PriorityBehavior.Ricochet,
            PriorityBehavior.HeavyBolt,
        };

        // Feature: weapon-gameplay-swarm-rework, Property 34: Marca reforça comportamentos de prioridade
        // Para todo alvo marcado presente entre os candidatos, os comportamentos de prioridade
        // (homing, ricochete e prioridade de Heavy Bolt) são direcionados ao alvo marcado — mas só os
        // comportamentos que o MarkConfig equipado habilita; um comportamento desabilitado resolve para
        // o alvo padrão mesmo com a Marca presente.
        // Validates: Requirements 10.3
        [Test]
        public void MarkPresentDirectsReinforcedPriorityBehaviorsToTheMarkedTarget()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- generate a config that independently toggles each reinforcement flag --------
                // Sample each flag across {true, false} so the full 2^3 space of reinforced sets is
                // exercised, and a strictly-positive validity so a mark can actually take hold.
                var config = new MarkConfig
                {
                    ValiditySeconds = 0.5f + (float)rng.NextDouble() * 10f, // > 0, so marks are usable
                    ReinforceHoming = rng.Next(0, 2) == 0,
                    ReinforceRicochet = rng.Next(0, 2) == 0,
                    ReinforceHeavyBolt = rng.Next(0, 2) == 0,
                };

                var mark = new WeaponMark(config);

                // ---- generate a live mark: a distinct marked target and default target -----------
                var markedTarget = new FakeMarkTarget(isAlive: true);
                var defaultTarget = new FakeMarkTarget(isAlive: true);

                // Mark at some time on the caller's clock, then resolve strictly before expiry so the
                // mark is genuinely present among the candidates (R10.2/R10.3).
                float now = (float)(rng.NextDouble() * 100.0);
                mark.Mark(markedTarget, now);

                // Any query time in [now, expiry) keeps the mark present.
                float queryOffset = (float)(rng.NextDouble()) * config.ValiditySeconds; // [0, validity)
                float queryTime = now + queryOffset;

                // Sanity: the generated mark is actually present at the query time; otherwise this
                // case would not exercise Property 34 (a present mark). This should always hold given
                // the strictly-positive validity and query time strictly before expiry.
                PropertyCheck.That(mark.IsMarkPresent(queryTime),
                    $"generated mark was not present at query time (now={now} validity={config.ValiditySeconds} " +
                    $"queryOffset={queryOffset}); Property 34 requires a present mark");

                // Pick one of the three priority behaviors to resolve this iteration.
                PriorityBehavior behavior = Behaviors[rng.Next(Behaviors.Length)];

                IMarkTarget resolved = mark.ResolvePriorityTarget(behavior, defaultTarget, queryTime);

                bool reinforced = mark.IsBehaviorReinforced(behavior);

                if (reinforced)
                {
                    // R10.3: a reinforced behavior with a present mark is directed to the MARKED
                    // target, never the default target.
                    PropertyCheck.That(ReferenceEquals(resolved, markedTarget),
                        $"reinforced behavior {behavior} with a present mark must resolve to the marked " +
                        $"target, but resolved to {(ReferenceEquals(resolved, defaultTarget) ? "the default target" : "an unexpected target")}");
                }
                else
                {
                    // A behavior the config does NOT reinforce resolves to the default target even
                    // while a mark is present (only enabled behaviors chase the mark, R10.3).
                    PropertyCheck.That(ReferenceEquals(resolved, defaultTarget),
                        $"non-reinforced behavior {behavior} must resolve to the default target even with " +
                        $"a present mark, but resolved to {(ReferenceEquals(resolved, markedTarget) ? "the marked target" : "an unexpected target")}");
                }
            });
        }
    }
}
