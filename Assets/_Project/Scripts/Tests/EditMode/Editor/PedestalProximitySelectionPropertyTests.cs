using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 3 of nexus-lobby-menu-restructure — deterministic
    /// proximity selection of the weapon pedestal whose Hades-style card should show.
    ///
    /// Property 3 (design): given the player position and the pedestal anchors, the selected
    /// pedestal is the one of least distance within its own radius; if the player is outside every
    /// radius, no pedestal is selected (the card stays hidden).
    ///
    /// The pure selector <see cref="PedestalProximity.SelectNearest"/> holds no scene dependency, so
    /// this exercises it directly. This project cannot resolve FsCheck/CsCheck on this machine, so the
    /// seeded <see cref="PropertyCheck"/> harness drives >= 100 deterministic cases and reports the
    /// exact failing case as a counterexample.
    /// </summary>
    public sealed class PedestalProximitySelectionPropertyTests
    {
        private const int MinPedestals = 1;
        private const int MaxPedestals = 6;
        private const float Span = 20f;   // world spread for anchors and player, in metres
        private const float MaxRadius = 8f;

        // Feature: nexus-lobby-menu-restructure, Property 3: deterministic proximity selection
        // For any player position and set of pedestal anchors+radii, the selected pedestal is the
        // in-radius one of least distance (ties resolve to the lowest index); when the player is
        // outside every radius, nothing is selected.
        // Validates: Requirements 2.1, 2.4
        [Test]
        public void SelectNearest_PicksClosestWithinRadius_OrNoneWhenOutsideAll()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int count = rng.Next(MinPedestals, MaxPedestals + 1);
                var anchors = new List<Vector3>(count);
                var radii = new List<float>(count);
                for (int p = 0; p < count; p++)
                {
                    anchors.Add(RandomPoint(rng));
                    // Include some zero/negative radii so "unreachable" pedestals are covered too.
                    radii.Add(RandomRadius(rng, p));
                }

                Vector3 player = SamplePlayer(rng, i, anchors);
                int selected = PedestalProximity.SelectNearest(player, anchors, radii);

                // Independently compute the expected answer: min distance among in-radius pedestals,
                // lowest index on ties.
                int expected = PedestalProximity.None;
                float expectedSqr = float.PositiveInfinity;
                int inRadiusCount = 0;
                for (int p = 0; p < count; p++)
                {
                    if (radii[p] <= 0f) continue;
                    float sqr = (anchors[p] - player).sqrMagnitude;
                    if (sqr > radii[p] * radii[p]) continue;
                    inRadiusCount++;
                    if (sqr < expectedSqr)
                    {
                        expectedSqr = sqr;
                        expected = p;
                    }
                }

                PropertyCheck.That(selected == expected,
                    $"[case #{i}] selected={selected} but expected={expected} " +
                    $"(count={count}, player={player}, inRadius={inRadiusCount})");

                if (selected == PedestalProximity.None)
                {
                    // No selection must mean the player is genuinely outside every reachable radius.
                    for (int p = 0; p < count; p++)
                    {
                        if (radii[p] <= 0f) continue;
                        float sqr = (anchors[p] - player).sqrMagnitude;
                        PropertyCheck.That(sqr > radii[p] * radii[p],
                            $"[case #{i}] returned None but pedestal {p} was within radius " +
                            $"(dist2={sqr}, r2={radii[p] * radii[p]})");
                    }
                }
                else
                {
                    // A selection must be a valid index, within its own radius, and no reachable
                    // pedestal may be strictly closer.
                    PropertyCheck.That(selected >= 0 && selected < count,
                        $"[case #{i}] selected index {selected} out of range [0,{count})");

                    float selSqr = (anchors[selected] - player).sqrMagnitude;
                    PropertyCheck.That(radii[selected] > 0f && selSqr <= radii[selected] * radii[selected],
                        $"[case #{i}] selected {selected} is not within its own radius " +
                        $"(dist2={selSqr}, r2={radii[selected] * radii[selected]})");

                    for (int p = 0; p < count; p++)
                    {
                        if (radii[p] <= 0f) continue;
                        float sqr = (anchors[p] - player).sqrMagnitude;
                        if (sqr > radii[p] * radii[p]) continue;
                        // Nobody in radius is strictly closer; equal distance only allowed for a
                        // lower-or-equal index (deterministic tie-break to the lowest index).
                        PropertyCheck.That(sqr > selSqr || (sqr == selSqr && p >= selected),
                            $"[case #{i}] pedestal {p} (dist2={sqr}) is closer than selected " +
                            $"{selected} (dist2={selSqr})");
                    }
                }
            });
        }

        // Feature: nexus-lobby-menu-restructure, Property 3: deterministic proximity selection
        // Selection is a pure function of its inputs: identical inputs always yield the same index.
        // Validates: Requirements 2.1, 2.4
        [Test]
        public void SelectNearest_IsDeterministicForIdenticalInputs()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int count = rng.Next(MinPedestals, MaxPedestals + 1);
                var anchors = new List<Vector3>(count);
                var radii = new List<float>(count);
                for (int p = 0; p < count; p++)
                {
                    anchors.Add(RandomPoint(rng));
                    radii.Add(RandomRadius(rng, p));
                }

                Vector3 player = SamplePlayer(rng, i, anchors);

                int first = PedestalProximity.SelectNearest(player, anchors, radii);
                int second = PedestalProximity.SelectNearest(player, anchors, radii);
                PropertyCheck.That(first == second,
                    $"[case #{i}] non-deterministic selection: {first} then {second}");
            });
        }

        // Feature: nexus-lobby-menu-restructure, Property 3: deterministic proximity selection
        // Null anchors/radii or a zero-radius-only set never select a pedestal (card hidden).
        // Validates: Requirements 2.4
        [Test]
        public void SelectNearest_NullOrUnreachable_SelectsNone()
        {
            Assert.AreEqual(PedestalProximity.None,
                PedestalProximity.SelectNearest(Vector3.zero, null, new List<float> { 1f }));
            Assert.AreEqual(PedestalProximity.None,
                PedestalProximity.SelectNearest(Vector3.zero, new List<Vector3> { Vector3.zero }, null));

            // A pedestal exactly on top of the player but with a non-positive radius is unreachable.
            Assert.AreEqual(PedestalProximity.None,
                PedestalProximity.SelectNearest(
                    Vector3.zero,
                    new List<Vector3> { Vector3.zero },
                    new List<float> { 0f }));
        }

        private static Vector3 RandomPoint(System.Random rng)
        {
            return new Vector3(
                (float)(rng.NextDouble() * 2 - 1) * Span,
                0f,
                (float)(rng.NextDouble() * 2 - 1) * Span);
        }

        private static float RandomRadius(System.Random rng, int index)
        {
            // Every 5th pedestal is unreachable (zero radius) to cover the guard path.
            if (index % 5 == 4) return 0f;
            return (float)(rng.NextDouble() * MaxRadius) + 0.25f;
        }

        // Sample player positions that stress the selector: exactly on an anchor (forces a tie/edge),
        // just inside/outside a radius, well outside everything, and free positions.
        private static Vector3 SamplePlayer(System.Random rng, int index, IReadOnlyList<Vector3> anchors)
        {
            switch (index % 4)
            {
                case 0:
                    // Sit exactly on an anchor so distance-0 ties can appear.
                    return anchors[rng.Next(0, anchors.Count)];
                case 1:
                    // Far outside everything so None is exercised.
                    return new Vector3(Span * 4f, 0f, Span * 4f);
                default:
                    return RandomPoint(rng);
            }
        }
    }
}
