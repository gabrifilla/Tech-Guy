using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R3 Property 9 of modifier-synergies-theme17
    /// (Element-death detonation classification, <see cref="RunSynergyEffects.Resolve"/>).
    ///
    /// A detonating enemy reacts to the elements it carried at death:
    ///  - burning and not frozen -> combustion, whose radius is the base explosion radius x1.3
    ///    (R3.1), so it reaches enemies that lie between the base radius and the combustion radius;
    ///  - frozen -> shatter, which emits between 3 and 6 fragments (R3.2), so it fans out to at most
    ///    six nearby enemies and its reach is the base radius (no x1.3 combustion bonus);
    ///  - both present -> shatter only, applied once (R3.3), so a both-case behaves like the frozen
    ///    case: it caps at six fragments and does NOT extend to the combustion-only ring.
    ///
    /// Because <c>DetonationKind</c> is an internal local of <c>Resolve</c> (not observable state),
    /// the classification is asserted through its two behavioral consequences: explosion reach
    /// (radius x1.3 for combustion) and fragment cap (<= 6 for shatter). Secondary targets are kept
    /// alive with large health and Conductor is left at rank 0, so no secondary re-detonates or
    /// branches; the whole cascade is the single first-generation burst from the dead enemy, and
    /// <see cref="RunSynergyEffects.LastSecondaryHits"/> equals that burst's hit count exactly.
    ///
    /// Runs in PlayMode: statuses are MonoBehaviour components on real Actors, the explosion keys on
    /// <c>Actor.IsDead</c>, and reachability is a real overlap/raycast query.
    /// </summary>
    public sealed class DetonationClassificationTests
    {
        private DetonationTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new DetonationTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 9: a burning (not frozen) death combusts with
        // radius x1.3, reaching a ring enemy that sits beyond the base radius but inside the
        // combustion radius; the same death without fire (frozen or plain) does not reach that ring.
        // Validates: Requirements 3.1
        [UnityTest]
        public IEnumerator Property9_Combustion_ExtendsRadiusBy1Point3()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 9);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();

                int detonation = 1 + rng.Next(0, 3); // 1..3
                _rig.AddDetonation(detonation);
                _rig.EnableBurn();

                float baseRadius = _rig.BaseRadius;               // 3 + .5*det
                float combustionRadius = _rig.CombustionRadius;   // base * 1.3
                // A "ring" enemy strictly between the two radii: hit only by a combustion explosion.
                float ringDistance = baseRadius + (combustionRadius - baseRadius) * 0.5f;

                // --- combustion case: burning, not frozen ---
                Actor dead = _rig.BuildEnemy(Vector3.zero);
                Actor ring = _rig.BuildEnemy(new Vector3(ringDistance, 0f, 0f));
                _rig.GiveBurn(dead);
                _rig.KillInPlace(dead);
                Assert.IsTrue(dead.IsDead, $"case {c}: the detonating enemy must be dead");

                _rig.Resolve(dead, 40f);
                int combustionHits = _rig.Synergies.LastSecondaryHits;

                Assert.AreEqual(1, combustionHits,
                    $"case {c}: combustion (radius x1.3 = {combustionRadius}) should reach the ring " +
                    $"enemy at {ringDistance}, base radius was {baseRadius}");

                // --- control case: same layout, no fire (plain death) -> base radius only ---
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();
                _rig.AddDetonation(detonation);
                _rig.EnableBurn();

                Actor deadPlain = _rig.BuildEnemy(Vector3.zero);
                _rig.BuildEnemy(new Vector3(ringDistance, 0f, 0f));
                _rig.KillInPlace(deadPlain); // no burn, no chill
                _rig.Resolve(deadPlain, 40f);

                Assert.AreEqual(0, _rig.Synergies.LastSecondaryHits,
                    $"case {c}: a plain death (base radius {baseRadius}) must NOT reach the ring at " +
                    $"{ringDistance}");
                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 9: a frozen death shatters into at most six
        // fragments, so with more than six reachable enemies it produces exactly six first-generation
        // secondary hits; a burning-only death of the identical layout is not fragment-capped.
        // Validates: Requirements 3.2
        [UnityTest]
        public IEnumerator Property9_Shatter_CapsAtSixFragments()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 90);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();

                int detonation = 1 + rng.Next(0, 3);
                _rig.AddDetonation(detonation);
                _rig.EnableBurn();

                // 7..12 enemies, all comfortably inside the base radius so every one is reachable.
                int clustered = 7 + rng.Next(0, 6);
                float ringRadius = _rig.BaseRadius * 0.6f;

                // --- shatter case: frozen ---
                Actor dead = _rig.BuildEnemy(Vector3.zero);
                PlaceRing(clustered, ringRadius);
                _rig.GiveChill(dead);
                _rig.KillInPlace(dead);
                _rig.Resolve(dead, 40f);
                int shatterHits = _rig.Synergies.LastSecondaryHits;

                Assert.AreEqual(6, shatterHits,
                    $"case {c}: a frozen shatter with {clustered} reachable enemies must cap at 6 " +
                    $"fragments, saw {shatterHits}");

                // --- combustion case: burning-only, identical layout -> not fragment-capped ---
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();
                _rig.AddDetonation(detonation);
                _rig.EnableBurn();

                Actor deadBurn = _rig.BuildEnemy(Vector3.zero);
                PlaceRing(clustered, ringRadius);
                _rig.GiveBurn(deadBurn);
                _rig.KillInPlace(deadBurn);
                _rig.Resolve(deadBurn, 40f);

                Assert.AreEqual(clustered, _rig.Synergies.LastSecondaryHits,
                    $"case {c}: combustion is not fragment-capped; expected all {clustered} reachable " +
                    $"enemies hit, saw {_rig.Synergies.LastSecondaryHits}");
                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 9: a death carrying BOTH burn and chill
        // applies shatter only (shatter precedence, R3.3): it is fragment-capped at 6 like the frozen
        // case AND uses the base (non-combustion) radius, so it does NOT reach the combustion-only ring.
        // Validates: Requirements 3.3
        [UnityTest]
        public IEnumerator Property9_BothElements_ShatterOnce_NotCombustion()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 903);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();

                int detonation = 1 + rng.Next(0, 3);
                _rig.AddDetonation(detonation);
                _rig.EnableBurn();

                float baseRadius = _rig.BaseRadius;
                float combustionRadius = _rig.CombustionRadius;
                float ringDistance = baseRadius + (combustionRadius - baseRadius) * 0.5f;

                // Seven enemies inside the base radius (would exceed the shatter cap of 6) plus one
                // ring enemy in the combustion-only band. Shatter must cap at 6 and skip the ring.
                Actor dead = _rig.BuildEnemy(Vector3.zero);
                PlaceRing(7, baseRadius * 0.55f);
                _rig.BuildEnemy(new Vector3(ringDistance, 0f, 0f)); // combustion-only ring

                _rig.GiveBurn(dead);
                _rig.GiveChill(dead); // both present
                _rig.KillInPlace(dead);
                Assert.IsTrue(_rig.HasBurn(dead) && _rig.HasChill(dead),
                    $"case {c}: the both-case must carry burn AND chill at death");

                _rig.Resolve(dead, 40f);

                // Shatter-once: capped at 6 despite 8 total reachable-by-combustion enemies, and the
                // combustion-only ring (beyond base radius) is never touched.
                Assert.AreEqual(6, _rig.Synergies.LastSecondaryHits,
                    $"case {c}: both-elements death must shatter once (cap 6, base radius), saw " +
                    $"{_rig.Synergies.LastSecondaryHits} — combustion reach or double-application leaked");
                yield return null;
            }
        }

        /// <summary>Places <paramref name="count"/> enemies evenly on a ring of the given radius around the origin.</summary>
        private void PlaceRing(int count, float radius)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i * (Mathf.PI * 2f / count);
                _rig.BuildEnemy(new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
        }
    }
}
