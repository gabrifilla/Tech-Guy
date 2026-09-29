using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure telegraph logic in <see cref="TelegraphBeat"/>
    /// (<see cref="TelegraphWindupClock"/>, <see cref="TelegraphIntensity"/>, and
    /// <see cref="SingleBeatResolver"/>) — task 3.2 of enemy-swarm-core-archetypes.
    ///
    /// The archetype telegraph is otherwise scene/coroutine driven; these invariants were extracted
    /// into plain C# so they can be property-checked without a live Unity scene. The project cannot
    /// resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class TelegraphBeatPropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 1: Telegraph windup floor before any damage beat.
        // For any authored windup, the effective windup is >= 0.25s, and a controlling
        // (crowd-control / displacement) attack's windup is at least as long as its non-controlling
        // equivalent (and never below the floor).
        // Validates: Requirements 2.1, 2.2, 3.4, 7.2, 8.2, 9.3, 11.3, 14.4
        [Test]
        public void WindupHonoursFloorAndControllingOrdering()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate authored windups across the full range including negatives / zero / large.
                float authored = RandomWindup(rng);
                bool controlling = rng.Next(0, 2) == 0;

                var clock = new TelegraphWindupClock(authored, controlling);

                // R2.1: effective windup is never below the 0.25s floor.
                PropertyCheck.That(clock.Duration >= TelegraphBeat.MinWindupSeconds - 1e-6f,
                    $"authored={authored}: effective windup {clock.Duration} fell below floor {TelegraphBeat.MinWindupSeconds}");

                // R2.1: a beat can never resolve before the floor has elapsed.
                float justBeforeFloor = TelegraphBeat.MinWindupSeconds - 1e-3f;
                PropertyCheck.That(!clock.IsComplete(justBeforeFloor),
                    $"authored={authored}: windup reported complete at {justBeforeFloor}s, before the floor");

                // R2.2: the controlling equivalent is at least as long as the non-controlling one and
                // still respects the floor.
                float nonControllingWindup = new TelegraphWindupClock(authored).Duration;
                var controllingClock = TelegraphWindupClock.Controlling(authored, nonControllingWindup);

                PropertyCheck.That(controllingClock.Duration >= nonControllingWindup - 1e-6f,
                    $"authored={authored}: controlling windup {controllingClock.Duration} shorter than non-controlling {nonControllingWindup}");
                PropertyCheck.That(controllingClock.Duration >= TelegraphBeat.MinWindupSeconds - 1e-6f,
                    $"authored={authored}: controlling windup {controllingClock.Duration} fell below floor");
                PropertyCheck.That(controllingClock.IsControlling,
                    "Controlling(...) must produce a clock flagged as controlling");
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 2: Damage beat is single per telegraphed area.
        // For any union of telegraphed areas and any sequence of resolve attempts (including a point
        // inside overlapping/crossing areas), SingleBeatResolver.TryResolve returns true at most once
        // per attack; every subsequent attempt returns false.
        // Validates: Requirements 2.5, 4.3, 6.4
        [Test]
        public void BeatResolvesAtMostOncePerAttackAcrossAreaUnion()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var resolver = new SingleBeatResolver();

                // Build a small union of possibly overlapping areas around the origin.
                int areaCount = rng.Next(1, 4);
                var areas = new EnemyAttackArea[areaCount];
                for (int a = 0; a < areaCount; a++) areas[a] = RandomCircle(rng);

                // A point that lies inside at least one area (origin, since all circles are centred
                // near origin with reach >= 1).
                var pointInside = Vector3.zero;
                float damage = 1f + (float)rng.NextDouble() * 20f;

                // Fire several attempts; count how many returned true.
                int attempts = rng.Next(2, 8);
                int trueCount = 0;
                for (int t = 0; t < attempts; t++)
                {
                    // Vary the tested point occasionally, but always keep at least the first attempt
                    // on a point in the union so a beat can resolve.
                    Vector3 point = t == 0 ? pointInside : RandomNearbyPoint(rng);
                    float dmg = t % 3 == 2 ? -damage : damage; // occasionally non-positive damage
                    if (resolver.TryResolve(areas, point, dmg)) trueCount++;
                }

                PropertyCheck.That(trueCount <= 1,
                    $"resolver produced {trueCount} beats for a single attack (expected at most 1)");

                // Once resolved, BeatResolved is latched and no further beat is produced.
                if (trueCount == 1)
                {
                    PropertyCheck.That(resolver.BeatResolved, "resolver reported a beat but BeatResolved is false");
                    PropertyCheck.That(!resolver.TryResolve(areas, pointInside, damage),
                        "resolver produced a second beat after already resolving");
                }

                // Reset re-arms the resolver for a fresh attack.
                resolver.Reset();
                PropertyCheck.That(!resolver.BeatResolved, "Reset() must clear the resolved flag");
                bool firstAfterReset = resolver.TryResolve(areas, pointInside, damage);
                PropertyCheck.That(firstAfterReset,
                    "after Reset() a valid in-union positive-damage attempt should resolve a beat");
                PropertyCheck.That(!resolver.TryResolve(areas, pointInside, damage),
                    "after Reset() the beat must still be single (second attempt should fail)");
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 4: Telegraph intensity is monotonic.
        // For any windup clock, the fraction and the derived intensity are non-decreasing as elapsed
        // time advances and stay clamped to [0, 1] (fraction) / [0, ImpactLerp] (intensity).
        // Validates: Requirements 2.3, 2.6, 9.3, 10.4
        [Test]
        public void TelegraphFractionAndIntensityAreMonotonicAndClamped()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var clock = new TelegraphWindupClock(RandomWindup(rng), rng.Next(0, 2) == 0);

                // A strictly increasing sequence of elapsed samples spanning before start to past end.
                int samples = rng.Next(4, 12);
                float prevFraction = float.NegativeInfinity;
                float prevIntensity = float.NegativeInfinity;
                float elapsed = -0.5f + (float)rng.NextDouble() * 0.25f; // may start slightly negative

                for (int s = 0; s < samples; s++)
                {
                    float fraction = clock.FractionAt(elapsed);
                    float intensity = TelegraphIntensity.At(fraction);

                    // Clamp bounds.
                    PropertyCheck.That(fraction >= 0f && fraction <= 1f,
                        $"fraction {fraction} out of [0,1] at elapsed={elapsed} (duration={clock.Duration})");
                    PropertyCheck.That(intensity >= 0f && intensity <= TelegraphIntensity.ImpactLerp + 1e-6f,
                        $"intensity {intensity} out of [0,{TelegraphIntensity.ImpactLerp}] at elapsed={elapsed}");

                    // Monotonic non-decreasing as elapsed advances.
                    PropertyCheck.That(fraction >= prevFraction - 1e-6f,
                        $"fraction decreased: {prevFraction} -> {fraction} at elapsed={elapsed}");
                    PropertyCheck.That(intensity >= prevIntensity - 1e-6f,
                        $"intensity decreased: {prevIntensity} -> {intensity} at elapsed={elapsed}");

                    prevFraction = fraction;
                    prevIntensity = intensity;

                    // Advance elapsed by a positive step so the sequence is strictly increasing in time.
                    elapsed += 0.01f + (float)rng.NextDouble() * (clock.Duration + 0.5f);
                }

                // At/after the duration the telegraph has reached full impact appearance.
                PropertyCheck.That(Mathf.Approximately(clock.FractionAt(clock.Duration), 1f),
                    $"fraction at full duration {clock.Duration} was {clock.FractionAt(clock.Duration)}, expected 1");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Authored windups spanning negative, zero, sub-floor, and well above the floor.</summary>
        private static float RandomWindup(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -1f + (float)rng.NextDouble() * 1f;   // negative .. 0
                case 1: return (float)rng.NextDouble() * 0.25f;      // 0 .. floor
                case 2: return 0.25f;                                // exactly the floor
                default: return 0.25f + (float)rng.NextDouble() * 5f; // above the floor
            }
        }

        private static EnemyAttackArea RandomCircle(System.Random rng)
        {
            float reach = 1f + (float)rng.NextDouble() * 4f;
            Vector3 center = new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 0.5f, 0f,
                ((float)rng.NextDouble() - 0.5f) * 0.5f); // near origin so the origin stays inside
            return new EnemyAttackArea(EnemyAttackShape.Circle, center, Vector3.forward, reach);
        }

        private static Vector3 RandomNearbyPoint(System.Random rng)
        {
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 20f, 0f,
                ((float)rng.NextDouble() - 0.5f) * 20f);
        }
    }
}
