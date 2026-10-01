using System;
using NUnit.Framework;
using UnityEngine;
// Disambiguate the bare `Random` used throughout this file to System.Random; without this it is
// ambiguous with UnityEngine.Random (CS0104). The project convention is to avoid relying on
// `using System;` for Random — this alias makes every `Random` here unambiguously System.Random.
using Random = System.Random;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test asserting that every pure gameplay helper in the
    /// ranged-kiting-and-attack-telegraph-overhaul feature is deterministic: for identical inputs
    /// (and, where applicable, an identically seeded <see cref="System.Random"/>) two independent
    /// runs produce bit-for-bit identical outputs across a full sequence of ticks.
    ///
    /// The pure classes (<see cref="EnemyFacing"/>, <see cref="ReactionGate"/>,
    /// <see cref="CadenceJitter"/>, <see cref="ApproachOffset"/>, <see cref="PatrolPause"/>,
    /// <see cref="KiteController"/>, <see cref="RetreatCadence"/>, <see cref="TelegraphFill"/>) are
    /// scene-free, so they can be property-checked without a live Unity scene. FsCheck/CsCheck cannot
    /// be resolved on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives
    /// &gt;= 100 deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class PureLogicDeterminismPropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 13
        // Determinism of all pure logic: for identical inputs and seeds, each pure helper produces
        // identical outputs. Each generated case builds one random input sequence, replays it against
        // two independent instances/calls seeded identically, and asserts the two output streams match
        // exactly. Covers EnemyFacing, ReactionGate, CadenceJitter, ApproachOffset, PatrolPause,
        // KiteController, RetreatCadence, and TelegraphFill.
        // Validates: Requirements 9.4, 16.1, 16.2
        [Test]
        public void IdenticalInputsAndSeedsProduceIdenticalOutputsForAllPureLogic()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // One shared RNG generates the input sequence for this case; the pure classes that
                // consume randomness each get two independently-seeded Random instances built from the
                // SAME per-case seed, so "identical seed => identical output" is what we verify.
                int consumerSeed = rng.Next(int.MinValue, int.MaxValue);
                int steps = rng.Next(1, 40);

                CheckEnemyFacing(rng, steps);
                CheckReactionGate(rng, steps);
                CheckCadenceJitter(rng, consumerSeed, steps);
                CheckApproachOffset(rng, consumerSeed, steps);
                CheckPatrolPause(rng, consumerSeed, steps);
                CheckKiteController(rng, steps);
                CheckRetreatCadence(rng, steps);
                CheckTelegraphFill(rng, steps);
            });
        }

        // ---- EnemyFacing (static math) ------------------------------------------------------

        private static void CheckEnemyFacing(Random rng, int steps)
        {
            Vector3 forward = RandomDirection(rng);
            float angularSpeed = RandomFloat(rng, -200f, 2000f);

            for (int s = 0; s < steps; s++)
            {
                Vector3 target = s % 5 == 0 ? Vector3.zero : RandomDirection(rng);
                float dt = RandomDt(rng);

                Vector3 a = EnemyFacing.StepTowards(forward, target, angularSpeed, dt);
                Vector3 b = EnemyFacing.StepTowards(forward, target, angularSpeed, dt);
                PropertyCheck.That(a == b,
                    $"EnemyFacing.StepTowards diverged at step {s}: a={Fmt(a)} b={Fmt(b)} " +
                    $"(forward={Fmt(forward)}, target={Fmt(target)}, speed={angularSpeed}, dt={dt}).");

                bool fa = EnemyFacing.IsFacing(forward, target);
                bool fb = EnemyFacing.IsFacing(forward, target);
                PropertyCheck.That(fa == fb,
                    $"EnemyFacing.IsFacing diverged at step {s}: {fa} vs {fb}.");

                // Advance along the deterministic result so later steps also exercise determinism.
                forward = a;
            }
        }

        // ---- ReactionGate (stateful, no RNG) ------------------------------------------------

        private static void CheckReactionGate(Random rng, int steps)
        {
            float reactionDelay = RandomFloat(rng, -1f, 3f);
            float sightLossReset = RandomFloat(rng, -1f, 12f);

            var gateA = new ReactionGate();
            var gateB = new ReactionGate();

            for (int s = 0; s < steps; s++)
            {
                bool inSight = rng.Next(0, 2) == 0;
                float dt = RandomDt(rng);

                bool ra = gateA.Tick(inSight, dt, reactionDelay, sightLossReset);
                bool rb = gateB.Tick(inSight, dt, reactionDelay, sightLossReset);

                PropertyCheck.That(ra == rb && gateA.Reacted == gateB.Reacted,
                    $"ReactionGate diverged at step {s}: a(ret={ra},reacted={gateA.Reacted}) " +
                    $"b(ret={rb},reacted={gateB.Reacted}) (inSight={inSight}, dt={dt}, " +
                    $"delay={reactionDelay}, reset={sightLossReset}).");
            }
        }

        // ---- CadenceJitter (static, explicit RNG) -------------------------------------------

        private static void CheckCadenceJitter(Random rng, int consumerSeed, int steps)
        {
            float baseInterval = RandomFloat(rng, -0.5f, 5f);
            float jitter = RandomFloat(rng, -0.2f, 0.8f);
            float floor = RandomFloat(rng, -0.5f, 2f);

            var rngA = new Random(consumerSeed);
            var rngB = new Random(consumerSeed);

            for (int s = 0; s < steps; s++)
            {
                float a = CadenceJitter.Effective(baseInterval, jitter, floor, rngA);
                float b = CadenceJitter.Effective(baseInterval, jitter, floor, rngB);
                PropertyCheck.That(a.Equals(b),
                    $"CadenceJitter.Effective diverged at step {s}: a={a} b={b} " +
                    $"(base={baseInterval}, jitter={jitter}, floor={floor}, seed={consumerSeed}).");
            }
        }

        // ---- ApproachOffset (stateful, explicit RNG) ----------------------------------------

        private static void CheckApproachOffset(Random rng, int consumerSeed, int steps)
        {
            float magnitude = RandomFloat(rng, -1f, 5f);
            float refresh = RandomFloat(rng, 0f, 11f);

            var offsetA = new ApproachOffset();
            var offsetB = new ApproachOffset();
            var rngA = new Random(consumerSeed);
            var rngB = new Random(consumerSeed);

            for (int s = 0; s < steps; s++)
            {
                float dt = RandomDt(rng);
                Vector3 a = offsetA.Tick(dt, magnitude, refresh, rngA);
                Vector3 b = offsetB.Tick(dt, magnitude, refresh, rngB);
                PropertyCheck.That(a == b,
                    $"ApproachOffset.Tick diverged at step {s}: a={Fmt(a)} b={Fmt(b)} " +
                    $"(magnitude={magnitude}, refresh={refresh}, dt={dt}, seed={consumerSeed}).");
            }
        }

        // ---- PatrolPause (stateful, explicit RNG) -------------------------------------------

        private static void CheckPatrolPause(Random rng, int consumerSeed, int steps)
        {
            float min = RandomFloat(rng, -1f, 3f);
            float max = RandomFloat(rng, -1f, 5f);

            var pauseA = new PatrolPause();
            var pauseB = new PatrolPause();
            var rngA = new Random(consumerSeed);
            var rngB = new Random(consumerSeed);

            // Begin draws from the RNG; both instances draw from identically-seeded sources.
            pauseA.Begin(min, max, rngA);
            pauseB.Begin(min, max, rngB);
            PropertyCheck.That(pauseA.IsPaused == pauseB.IsPaused,
                $"PatrolPause.Begin diverged: a={pauseA.IsPaused} b={pauseB.IsPaused} " +
                $"(min={min}, max={max}, seed={consumerSeed}).");

            for (int s = 0; s < steps; s++)
            {
                float dt = RandomDt(rng);
                bool a = pauseA.Tick(dt);
                bool b = pauseB.Tick(dt);
                PropertyCheck.That(a == b && pauseA.IsPaused == pauseB.IsPaused,
                    $"PatrolPause.Tick diverged at step {s}: a(ret={a},paused={pauseA.IsPaused}) " +
                    $"b(ret={b},paused={pauseB.IsPaused}) (min={min}, max={max}, dt={dt}).");
            }
        }

        // ---- KiteController (stateful, no RNG) ----------------------------------------------

        private static void CheckKiteController(Random rng, int steps)
        {
            float window = RandomFloat(rng, -0.5f, 4f);
            float cooldown = RandomFloat(rng, -0.5f, 4f);

            var kiteA = new KiteController();
            var kiteB = new KiteController();

            for (int s = 0; s < steps; s++)
            {
                bool wantsToKite = rng.Next(0, 2) == 0;
                float dt = RandomDt(rng);

                bool a = kiteA.Tick(wantsToKite, dt, window, cooldown);
                bool b = kiteB.Tick(wantsToKite, dt, window, cooldown);
                PropertyCheck.That(a == b && kiteA.Current == kiteB.Current,
                    $"KiteController diverged at step {s}: a(ret={a},phase={kiteA.Current}) " +
                    $"b(ret={b},phase={kiteB.Current}) (wantsToKite={wantsToKite}, dt={dt}, " +
                    $"window={window}, cooldown={cooldown}).");
            }
        }

        // ---- RetreatCadence (static math) ---------------------------------------------------

        private static void CheckRetreatCadence(Random rng, int steps)
        {
            for (int s = 0; s < steps; s++)
            {
                float chaseSpeed = RandomFloat(rng, -1f, 10f);
                float multiplier = RandomFloat(rng, -0.5f, 1.5f);
                float engagementBand = RandomFloat(rng, -1f, 20f);
                float standoffFraction = RandomFloat(rng, -0.5f, 1.5f);
                float standoffMargin = RandomFloat(rng, 0f, 3f);

                float m1 = RetreatCadence.ClampRetreatMultiplier(multiplier);
                float m2 = RetreatCadence.ClampRetreatMultiplier(multiplier);
                PropertyCheck.That(m1.Equals(m2),
                    $"RetreatCadence.ClampRetreatMultiplier diverged: {m1} vs {m2} (in={multiplier}).");

                float rs1 = RetreatCadence.RetreatSpeed(chaseSpeed, multiplier);
                float rs2 = RetreatCadence.RetreatSpeed(chaseSpeed, multiplier);
                PropertyCheck.That(rs1.Equals(rs2),
                    $"RetreatCadence.RetreatSpeed diverged: {rs1} vs {rs2} " +
                    $"(chase={chaseSpeed}, mult={multiplier}).");

                float sd1 = RetreatCadence.StandoffDistance(engagementBand, standoffFraction);
                float sd2 = RetreatCadence.StandoffDistance(engagementBand, standoffFraction);
                PropertyCheck.That(sd1.Equals(sd2),
                    $"RetreatCadence.StandoffDistance diverged: {sd1} vs {sd2} " +
                    $"(band={engagementBand}, frac={standoffFraction}).");

                float rt1 = RetreatCadence.RepositionTarget(sd1, standoffMargin);
                float rt2 = RetreatCadence.RepositionTarget(sd1, standoffMargin);
                PropertyCheck.That(rt1.Equals(rt2),
                    $"RetreatCadence.RepositionTarget diverged: {rt1} vs {rt2} " +
                    $"(standoff={sd1}, margin={standoffMargin}).");
            }
        }

        // ---- TelegraphFill (static math) ----------------------------------------------------

        private static void CheckTelegraphFill(Random rng, int steps)
        {
            for (int s = 0; s < steps; s++)
            {
                float fraction = RandomFloat(rng, -0.5f, 1.5f);

                float p1 = TelegraphFill.ProgressAt(fraction);
                float p2 = TelegraphFill.ProgressAt(fraction);
                PropertyCheck.That(p1.Equals(p2),
                    $"TelegraphFill.ProgressAt diverged: {p1} vs {p2} (fraction={fraction}).");

                Color c1 = TelegraphIntensity.ColorAt(TelegraphFill.RedBase, fraction);
                Color c2 = TelegraphIntensity.ColorAt(TelegraphFill.RedBase, fraction);
                PropertyCheck.That(c1 == c2,
                    $"TelegraphFill color diverged: {c1} vs {c2} (fraction={fraction}).");
            }
        }

        // ---- generators ---------------------------------------------------------------------

        private static float RandomFloat(Random rng, float min, float max)
            => min + (float)rng.NextDouble() * (max - min);

        private static float RandomDt(Random rng)
        {
            // Mix non-positive dt (ignored by the pure logic) with typical frame steps.
            switch (rng.Next(0, 6))
            {
                case 0: return 0f;
                case 1: return -RandomFloat(rng, 0f, 0.1f);
                default: return RandomFloat(rng, 0.001f, 0.5f);
            }
        }

        private static Vector3 RandomDirection(Random rng)
        {
            // Occasionally emit a degenerate (near-zero) vector to exercise that path deterministically.
            if (rng.Next(0, 7) == 0)
            {
                return Vector3.zero;
            }

            return new Vector3(
                RandomFloat(rng, -5f, 5f),
                RandomFloat(rng, -5f, 5f),
                RandomFloat(rng, -5f, 5f));
        }

        private static string Fmt(Vector3 v) => $"({v.x:R},{v.y:R},{v.z:R})";
    }
}
