using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure "no Mark present" branch of
    /// <see cref="WeaponMark.ResolvePriorityTarget"/> — task 12.3 of weapon-gameplay-swarm-rework.
    ///
    /// The Bow's priority resolution is otherwise driven by a scene-side coordinator, but the
    /// no-op rule "activating a priority ability without a live Mark uses the default target and
    /// leaves the Mark state untouched" (R10.8) lives entirely in the pure <see cref="WeaponMark"/>
    /// state model, so it can be property-checked without a live Unity scene. The project cannot
    /// resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property
    /// (exercising every <see cref="PriorityBehavior"/> across the three ways a mark can be absent:
    /// never set, expired, or the target no longer alive) and reports the exact failing case as a
    /// counterexample.
    /// </summary>
    public sealed class MarkAbsentDefaultTargetPropertyTests
    {
        /// <summary>A minimal <see cref="IMarkTarget"/> whose liveness can be toggled after marking.</summary>
        private sealed class FakeTarget : IMarkTarget
        {
            public bool Alive = true;
            public bool IsAlive => Alive;
        }

        private static readonly PriorityBehavior[] AllBehaviors =
        {
            PriorityBehavior.Homing,
            PriorityBehavior.Ricochet,
            PriorityBehavior.HeavyBolt,
        };

        // How a mark comes to be absent at the moment of resolution.
        private enum AbsenceKind
        {
            NeverMarked, // Mark was never applied.
            Expired,     // A mark was applied but the designation has elapsed.
            Dead,        // A mark was applied but the target is no longer alive.
        }

        // Feature: weapon-gameplay-swarm-rework, Property 36: Habilidade de prioridade sem Marca usa alvo padrão.
        // For every priority-ability activation without a marked target present — whether the mark was
        // never set, has expired, or its target is no longer alive — ResolvePriorityTarget resolves to
        // the ability's default target for EVERY PriorityBehavior (no homing/ricochet/Heavy Bolt
        // reinforcement), and the Mark state (Target, ExpiresAt, IsMarkPresent) is exactly the same
        // after the call as it was before it (R10.8: the no-op branch cannot corrupt or clear a
        // non-existent designation).
        // Validates: Requirements 10.8
        [Test]
        public void PriorityWithoutMarkUsesDefaultTargetAndLeavesMarkStateUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- generate an arbitrary config: reinforcement flags and validity must not matter
                // for the "no mark present" branch, so sample the whole space including 0 validity. ----
                var config = new MarkConfig
                {
                    // Include non-positive validity (marks never take hold) and positive validity.
                    ValiditySeconds = rng.Next(0, 2) == 0 ? 0f : 0.5f + (float)rng.NextDouble() * 8f,
                    ReinforceHoming = rng.Next(0, 2) == 0,
                    ReinforceRicochet = rng.Next(0, 2) == 0,
                    ReinforceHeavyBolt = rng.Next(0, 2) == 0,
                };

                var mark = new WeaponMark(config);

                // A clock value used both to (optionally) mark and to resolve.
                float now = (float)(rng.NextDouble() * 1000.0 - 500.0); // -500..500

                // The distinct default target the ability would use without any mark.
                var defaultTarget = new FakeTarget { Alive = rng.Next(0, 2) == 0 };

                // ---- drive the mark into an "absent" state via one of the three routes ----
                AbsenceKind kind = (AbsenceKind)rng.Next(0, 3);
                switch (kind)
                {
                    case AbsenceKind.NeverMarked:
                        // Leave the mark untouched: never set.
                        break;

                    case AbsenceKind.Expired:
                    {
                        // Mark a live target with a POSITIVE validity so it actually takes hold, then
                        // resolve at a time strictly after it expires. If the sampled config had 0
                        // validity the mark never holds, which is also "absent" — still valid for R10.8.
                        var t = new FakeTarget { Alive = true };
                        mark.Mark(t, now);
                        if (mark.IsMarkPresent(now))
                        {
                            // Advance the clock strictly past ExpiresAt.
                            now = mark.ExpiresAt + 0.001f + (float)rng.NextDouble() * 5f;
                        }
                        break;
                    }

                    case AbsenceKind.Dead:
                    {
                        // Mark a live target (with positive validity so it can hold), then kill it before
                        // resolution so IsMarkPresent is false at `now` even though ExpiresAt is future.
                        var t = new FakeTarget { Alive = true };
                        // Ensure a positive validity for this route so the mark could hold; otherwise fall
                        // back to a fresh positive-validity mark object matching this config's flags.
                        if (config.ValiditySeconds <= 0f)
                        {
                            var holdingConfig = config;
                            holdingConfig.ValiditySeconds = 1f + (float)rng.NextDouble() * 8f;
                            mark = new WeaponMark(holdingConfig);
                        }
                        mark.Mark(t, now);
                        t.Alive = false; // target dies; expiry is still in the future.
                        break;
                    }
                }

                // Precondition for this property: the mark must be absent at resolution time.
                PropertyCheck.That(!mark.IsMarkPresent(now),
                    $"test setup expected no mark present for kind={kind}, but IsMarkPresent(now={now}) was true");

                // ---- snapshot the Mark state BEFORE the call ----
                IMarkTarget targetBefore = mark.Target;
                float expiresAtBefore = mark.ExpiresAt;
                bool presentBefore = mark.IsMarkPresent(now);

                // ---- exercise the property across EVERY priority behavior ----
                foreach (PriorityBehavior behavior in AllBehaviors)
                {
                    IMarkTarget resolved = mark.ResolvePriorityTarget(behavior, defaultTarget, now);

                    // R10.8: with no mark present, resolution is a no-op that returns the default target
                    // (no homing/ricochet/Heavy Bolt reinforcement) regardless of the config flags.
                    PropertyCheck.That(ReferenceEquals(resolved, defaultTarget),
                        $"priority without a mark must resolve to the default target for behavior={behavior} " +
                        $"(kind={kind}, reinforceH={config.ReinforceHoming} reinforceR={config.ReinforceRicochet} " +
                        $"reinforceHB={config.ReinforceHeavyBolt}), but it resolved to a different target");

                    // R10.8: the Mark state must be left untouched by the no-op branch.
                    PropertyCheck.That(ReferenceEquals(mark.Target, targetBefore),
                        $"resolving priority without a mark must not change Mark.Target (behavior={behavior}, kind={kind})");
                    PropertyCheck.That(mark.ExpiresAt == expiresAtBefore,
                        $"resolving priority without a mark must not change Mark.ExpiresAt (behavior={behavior}, kind={kind}): " +
                        $"before={expiresAtBefore}, after={mark.ExpiresAt}");
                    PropertyCheck.That(mark.IsMarkPresent(now) == presentBefore,
                        $"resolving priority without a mark must not change Mark presence (behavior={behavior}, kind={kind})");
                }

                // A no-mark state must remain a no-mark state after all three resolutions.
                PropertyCheck.That(!mark.IsMarkPresent(now),
                    $"the Mark must still be absent after resolving without a mark (kind={kind})");
            });
        }
    }
}
