using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property + example tests for Property 19 of gauntlet-boon-playstyle-overhaul (the
    /// registry part) — the <c>EdgeStrike</c> / "Ponto cego" boon's vulnerability window.
    ///
    /// Property 19 (registry part, design): an edge hit (a hit in the outer 20% of the thrust range)
    /// opens a window on the enemy whose <see cref="EdgeVulnerabilityRegistry.AmplifierFor"/> amplifies
    /// that enemy's subsequent direct hits while the window is open and returns to exactly <c>1</c> when
    /// it expires; an inner hit opens no window and never amplifies. (R13.1/R13.2/R13.3.) The pure
    /// edge predicate and the stance multiplier (<c>1 + 0.4R</c>) are covered separately by
    /// <c>EdgeBandPropertyTests</c> (task 6.8); here the real run-scoped
    /// <see cref="EdgeVulnerabilityRegistry"/> is driven end-to-end.
    ///
    /// Approach: the window open → amplify → expire → restore round-trip lives entirely in the real
    /// component, so this test drives the <b>real</b> registry. The only Unity coupling in the tested
    /// path is time and the reaction/transform read; both are behind injectable seams
    /// (<c>ConfigureForTests</c> for the clock + thrust range, and <c>RegisterEdgeHit</c> which takes an
    /// explicit hit distance), so the amplifier/expiry decision can be exercised deterministically
    /// without a scene clock or positioned transforms. Real <see cref="Actor"/> GameObjects are used as
    /// window keys (created per case and destroyed with <c>DestroyImmediate</c>), matching how the
    /// registry keys its windows. The registry is configured with a null player so the reaction call is a
    /// guarded no-op and the window logic is isolated.
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    // Feature: gauntlet-boon-playstyle-overhaul, Property 19
    public sealed class EdgeStrikePropertyTests
    {
        private const float Tolerance = 1e-4f;
        private const float WindowDuration = EdgeVulnerabilityRegistry.WindowDuration; // window lifetime, seconds
        private const float ExpectedAmp = 1f + EdgeVulnerabilityRegistry.AmplifierBonus; // amplifier while open

        // Builds a fresh registry on a throwaway GameObject with a controllable clock and an explicit
        // thrust range, configured with a null player (the reaction call becomes a guarded no-op so the
        // window round-trip is isolated). The caller advances clockHolder[0] to move time.
        private static EdgeVulnerabilityRegistry MakeRegistry(int rank, float thrustRange,
            float[] clockHolder, out GameObject owner)
        {
            owner = new GameObject("EdgeVulnerabilityRegistry");
            var registry = owner.AddComponent<EdgeVulnerabilityRegistry>();
            registry.Configure(null, null, rank);
            registry.ConfigureForTests(() => clockHolder[0], thrustRange);
            return registry;
        }

        private static Actor MakeEnemy(string name)
        {
            var go = new GameObject(name);
            return go.AddComponent<Actor>();
        }

        // Property 19 (registry part): an edge hit opens a window whose AmplifierFor is 1 + bonus while
        // open and returns to exactly 1 when it expires; an inner hit opens no window (amplifier stays 1).
        // Validates: Requirements 13.1, 13.2, 13.3
        [Test]
        public void EdgeHitOpensAmplifyingWindow_ExpiresToOne_InnerHitOpensNone()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);                              // [1,3] — catalog MaxRank is 3
                float thrustRange = 1f + (float)rng.NextDouble() * 9f;  // 1 .. 10 m reach
                float edge = thrustRange * 0.8f;

                var clock = new float[] { (float)rng.NextDouble() * 100f }; // arbitrary start time
                EdgeVulnerabilityRegistry registry = MakeRegistry(rank, thrustRange, clock, out GameObject owner);
                Actor edgeEnemy = MakeEnemy($"edge_{i}");
                Actor innerEnemy = MakeEnemy($"inner_{i}");

                string state = $"[case #{i}] rank={rank} range={thrustRange} edge={edge} start={clock[0]}";
                try
                {
                    // Before any hit: amplifier is exactly 1 and the enemy is not vulnerable (R13.3).
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(edgeEnemy) - 1f) <= Tolerance,
                        $"{state}: un-hit enemy amplifier must be 1 (got {registry.AmplifierFor(edgeEnemy)})");
                    PropertyCheck.That(!registry.IsVulnerable(edgeEnemy), $"{state}: enemy must start non-vulnerable");

                    // An inner hit (clearly inside the inner 80%) opens no window and never amplifies (R13.3).
                    float innerDistance = (float)rng.NextDouble() * (edge - 1e-2f);
                    bool innerOpened = registry.RegisterEdgeHit(innerEnemy, innerDistance, thrustRange);
                    PropertyCheck.That(!innerOpened, $"{state}: inner hit at {innerDistance} must not open a window");
                    PropertyCheck.That(!registry.IsVulnerable(innerEnemy), $"{state}: inner-hit enemy must not be vulnerable");
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(innerEnemy) - 1f) <= Tolerance,
                        $"{state}: inner-hit enemy amplifier must stay 1 (got {registry.AmplifierFor(innerEnemy)})");

                    // An edge hit (clearly past the 0.8*range threshold) opens the window and amplifies (R13.1/R13.2).
                    float edgeDistance = edge + 1e-2f + (float)rng.NextDouble() * (thrustRange - edge);
                    bool edgeOpened = registry.RegisterEdgeHit(edgeEnemy, edgeDistance, thrustRange);
                    PropertyCheck.That(edgeOpened, $"{state}: edge hit at {edgeDistance} must open a window");
                    PropertyCheck.That(registry.IsVulnerable(edgeEnemy), $"{state}: edge-hit enemy must be vulnerable");
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(edgeEnemy) - ExpectedAmp) <= Tolerance,
                        $"{state}: open-window amplifier must be {ExpectedAmp} (got {registry.AmplifierFor(edgeEnemy)})");

                    // Any time strictly inside the window: still vulnerable, still amplifying subsequent hits.
                    clock[0] += (float)rng.NextDouble() * WindowDuration * 0.999f;
                    registry.ExpireElapsed(); // mirrors Update(): expires nothing yet
                    PropertyCheck.That(registry.IsVulnerable(edgeEnemy),
                        $"{state}: enemy must stay vulnerable at now={clock[0]} (< close time)");
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(edgeEnemy) - ExpectedAmp) <= Tolerance,
                        $"{state}: amplifier must stay {ExpectedAmp} inside the window (got {registry.AmplifierFor(edgeEnemy)})");

                    // Advance to/past the close time: the window closes, amplifier returns to exactly 1 (R13.2).
                    clock[0] += WindowDuration + (float)rng.NextDouble() * 5f;
                    registry.ExpireElapsed();
                    PropertyCheck.That(!registry.IsVulnerable(edgeEnemy),
                        $"{state}: enemy must stop being vulnerable after expiry (now={clock[0]})");
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(edgeEnemy) - 1f) <= Tolerance,
                        $"{state}: expired amplifier must return to 1 (got {registry.AmplifierFor(edgeEnemy)})");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(edgeEnemy.gameObject);
                    UnityEngine.Object.DestroyImmediate(innerEnemy.gameObject);
                    UnityEngine.Object.DestroyImmediate(owner);
                }
            });
        }

        // Property 19 (multi-enemy facet): only enemies hit on the edge amplify; each edge enemy's window
        // closes independently once its time elapses, returning its amplifier to 1.
        // Validates: Requirements 13.2, 13.3
        [Test]
        public void OnlyEdgeHitEnemiesAmplify_AndEachWindowExpiresIndependently()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);
                float thrustRange = 2f + (float)rng.NextDouble() * 6f; // 2 .. 8 m reach
                float edge = thrustRange * 0.8f;

                var clock = new float[] { (float)rng.NextDouble() * 50f };
                EdgeVulnerabilityRegistry registry = MakeRegistry(rank, thrustRange, clock, out GameObject owner);

                int count = rng.Next(1, 5);
                var enemies = new Actor[count];
                var onEdge = new bool[count];
                for (int e = 0; e < count; e++) enemies[e] = MakeEnemy($"enemy_{i}_{e}");

                string state = $"[case #{i}] rank={rank} range={thrustRange} count={count}";
                try
                {
                    // Hit a random subset on the edge and the rest well inside the inner band.
                    for (int e = 0; e < count; e++)
                    {
                        onEdge[e] = rng.Next(0, 2) == 0;
                        float distance = onEdge[e]
                            ? edge + 1e-2f + (float)rng.NextDouble() * (thrustRange - edge)
                            : (float)rng.NextDouble() * (edge - 1e-2f);
                        registry.RegisterEdgeHit(enemies[e], distance, thrustRange);
                    }

                    for (int e = 0; e < count; e++)
                    {
                        float amp = registry.AmplifierFor(enemies[e]);
                        float want = onEdge[e] ? ExpectedAmp : 1f;
                        PropertyCheck.That(Math.Abs(amp - want) <= Tolerance,
                            $"{state}: enemy#{e} onEdge={onEdge[e]} amplifier {amp} != {want}");
                    }

                    // Expire everything: every amplifier returns to exactly 1.
                    clock[0] += WindowDuration + 1f;
                    registry.ExpireElapsed();
                    for (int e = 0; e < count; e++)
                        PropertyCheck.That(Math.Abs(registry.AmplifierFor(enemies[e]) - 1f) <= Tolerance,
                            $"{state}: enemy#{e} amplifier must return to 1 after expiry");
                }
                finally
                {
                    for (int e = 0; e < count; e++)
                        if (enemies[e]) UnityEngine.Object.DestroyImmediate(enemies[e].gameObject);
                    UnityEngine.Object.DestroyImmediate(owner);
                }
            });
        }

        // Example: an edge hit opens a window that amplifies by exactly the configured bonus, then
        // returns to 1 once the window elapses.
        [Test]
        public void EdgeHit_AmplifiesByBonus_ThenRestoresToOneOnExpiry()
        {
            var clock = new float[] { 0f };
            EdgeVulnerabilityRegistry registry = MakeRegistry(2, 5f, clock, out GameObject owner);
            Actor enemy = MakeEnemy("enemy");

            try
            {
                // 5m range -> edge at 4m. A hit at 4.5m is on the edge.
                bool opened = registry.RegisterEdgeHit(enemy, 4.5f, 5f);
                Assert.That(opened, Is.True, "A hit past the 0.8*range edge must open a window.");
                Assert.That(registry.AmplifierFor(enemy), Is.EqualTo(ExpectedAmp).Within(Tolerance),
                    "An open window amplifies subsequent direct hits by the configured bonus.");

                clock[0] += WindowDuration + 0.5f;
                registry.ExpireElapsed();
                Assert.That(registry.AmplifierFor(enemy), Is.EqualTo(1f).Within(Tolerance),
                    "Amplifier returns to 1 once the vulnerability window expires.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        // Example: a hit inside the inner 80% opens no window and never amplifies.
        [Test]
        public void InnerHit_OpensNoWindow_AmplifierStaysOne()
        {
            var clock = new float[] { 0f };
            EdgeVulnerabilityRegistry registry = MakeRegistry(3, 5f, clock, out GameObject owner);
            Actor enemy = MakeEnemy("enemy");

            try
            {
                // 5m range -> edge at 4m. A hit at 2m is well inside the inner band.
                bool opened = registry.RegisterEdgeHit(enemy, 2f, 5f);
                Assert.That(opened, Is.False, "An inner hit must not open a window.");
                Assert.That(registry.IsVulnerable(enemy), Is.False, "An inner-hit enemy must not be vulnerable.");
                Assert.That(registry.AmplifierFor(enemy), Is.EqualTo(1f).Within(Tolerance),
                    "An inner-hit enemy must never be amplified.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
    }
}
