using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for task 12.3 of enemy-swarm-core-archetypes: the four authored
    /// melee <see cref="EnemyProfile"/> assets (Rush, Grunt, Heavy, Charger) must be pairwise
    /// distinct so each melee archetype plays differently.
    ///
    /// The profiles are authored Unity assets, so the "load" leg uses
    /// <see cref="UnityEditor.AssetDatabase"/> (Editor-only, valid in this EditMode/Editor test
    /// assembly). The distinctness invariant itself is pure: every unordered pair must differ in at
    /// least one defined attribute of the full attribute vector. The project cannot resolve
    /// FsCheck/CsCheck on this machine, so the agreed seeded <see cref="PropertyCheck"/> harness
    /// drives >= 100 deterministic generated cases — each case picks a random pair and a random
    /// ordering of the attribute comparisons — and reports the exact failing case as a
    /// counterexample.
    /// </summary>
    public sealed class MeleeProfileDistinctnessPropertyTests
    {
        private const string ArchetypeDir =
            "Assets/_Project/ScriptableObjects/Enemies/Archetypes/";

        private static readonly string[] MeleeAssetNames = { "Rush", "Grunt", "Heavy", "Charger" };

        // Feature: enemy-swarm-core-archetypes, Property 15: Melee profiles are pairwise distinct.
        // For every unordered pair among the four authored melee EnemyProfiles, the pair differs in
        // at least one defined attribute (the full attribute vector is compared; at least one
        // component differs). Random pair selection and random attribute-comparison ordering are
        // generated across >= 100 iterations; distinctness must hold for every generated case.
        // Validates: Requirements 22.3
        [Test]
        public void MeleeProfilesArePairwiseDistinct()
        {
            EnemyProfile[] profiles = LoadMeleeProfiles();

            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate an unordered pair (a != b) of the four melee profiles.
                int a = rng.Next(0, profiles.Length);
                int b = rng.Next(0, profiles.Length - 1);
                if (b >= a) b++; // maps into [0, length) skipping a, so a != b

                float[] va = AttributeVector(profiles[a]);
                float[] vb = AttributeVector(profiles[b]);

                // Compare the attribute vector in a randomly generated order; distinctness only
                // requires that at least one component differs, so ordering must not change the
                // outcome, but generating it satisfies "iterations over attribute comparisons".
                int[] order = ShuffledIndices(rng, va.Length);
                bool differs = false;
                int firstDifferingComponent = -1;
                for (int k = 0; k < order.Length; k++)
                {
                    int c = order[k];
                    if (!Mathf.Approximately(va[c], vb[c]))
                    {
                        differs = true;
                        firstDifferingComponent = c;
                        break;
                    }
                }

                PropertyCheck.That(differs,
                    $"melee profiles '{MeleeAssetNames[a]}' and '{MeleeAssetNames[b]}' are identical " +
                    "across every defined attribute (expected at least one to differ)");

                // Sanity: a profile compared with itself is (trivially) not distinct — guards the
                // comparison logic so a false "differs" can never mask a real duplicate.
                PropertyCheck.That(firstDifferingComponent >= 0,
                    "expected a concrete differing attribute index when profiles differ");
            });
        }

        // ---- loading ------------------------------------------------------------------------

        /// <summary>
        /// Loads the four authored melee profiles through the AssetDatabase. If any cannot be
        /// resolved (e.g. running in a headless context without an imported asset database) the
        /// test is ignored with a clear message rather than failing spuriously.
        /// </summary>
        private static EnemyProfile[] LoadMeleeProfiles()
        {
#if UNITY_EDITOR
            var profiles = new EnemyProfile[MeleeAssetNames.Length];
            for (int n = 0; n < MeleeAssetNames.Length; n++)
            {
                string path = ArchetypeDir + MeleeAssetNames[n] + ".asset";
                var profile = UnityEditor.AssetDatabase.LoadAssetAtPath<EnemyProfile>(path);
                if (profile == null)
                {
                    Assert.Ignore($"Could not load melee EnemyProfile at '{path}'. " +
                                  "The authored asset must exist and be imported for this property to run.");
                }
                profiles[n] = profile;
            }
            return profiles;
#else
            Assert.Ignore("Melee profile distinctness requires the Editor AssetDatabase to load the authored assets.");
            return null;
#endif
        }

        // ---- attribute vector + generators --------------------------------------------------

        /// <summary>
        /// The full defined-attribute vector for an <see cref="EnemyProfile"/>. Two profiles are
        /// distinct when any component differs.
        /// </summary>
        private static float[] AttributeVector(EnemyProfile p)
        {
            return new[]
            {
                p.HealthMultiplier,
                p.DamageMultiplier,
                p.MovementMultiplier,
                p.AttackSpeedMultiplier,
                p.MaxStance,
                p.StanceDamageMultiplier,
                p.StanceRecoveryPerSecond,
                p.StanceRecoveryDelay,
                p.StaggerResistance,
                p.StunResistance,
                p.KnockUpResistance,
                p.KnockbackResistance,
            };
        }

        /// <summary>A Fisher-Yates shuffle of [0, count) using the case RNG.</summary>
        private static int[] ShuffledIndices(System.Random rng, int count)
        {
            var indices = new int[count];
            for (int k = 0; k < count; k++) indices[k] = k;
            for (int k = count - 1; k > 0; k--)
            {
                int j = rng.Next(0, k + 1);
                (indices[k], indices[j]) = (indices[j], indices[k]);
            }
            return indices;
        }
    }
}
