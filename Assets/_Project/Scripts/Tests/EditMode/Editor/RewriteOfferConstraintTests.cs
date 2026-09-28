using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for R9 Property 23 of modifier-synergies-theme17
    /// (Rewrite offer constraint, <see cref="RunBoons.OfferReward"/>).
    ///
    /// Property 23 has two parts:
    ///   1. At most one Rewrite (<c>transform</c>) modifier appears per offer set.
    ///   2. The empirical appearance rate of a Rewrite across many offer sets is &lt;= 20%
    ///      (within statistical tolerance).
    ///
    /// Test seam: <see cref="RunBoons"/> is a MonoBehaviour whose Rewrite gate reads
    /// <c>(_rewriteRoll ?? _random.NextDouble)() &lt;= 0.2</c>. Production leaves <c>_rewriteRoll</c>
    /// null (falling back to the shared <c>System.Random</c>); tests assign a deterministic
    /// <c>Func&lt;double&gt;</c> to the private <c>_rewriteRoll</c> field by reflection so the roll is
    /// reproducible (the seam field was added as part of task 13.3 — see the subagent report). The
    /// reflection approach matches the existing OrbitCombinationHintTests private-field seam pattern
    /// and keeps the production RunBoons API surface unchanged. The RunBoons object graph is built
    /// on an INACTIVE GameObject so no MonoBehaviour lifecycle (Awake/Start) runs; the fields that
    /// <c>OfferReward</c> reads (<c>_weapon</c>, <c>_player</c>, <c>WeaponModifiers</c>) are set by
    /// reflection, exactly the private-field seam pattern used by OrbitCombinationHintTests. This
    /// exercises the real <c>OfferReward</c> offer-construction path (not a re-implementation).
    /// </summary>
    public sealed class RewriteOfferConstraintTests
    {
        private const string TransformId = "transform"; // the only Rewrite-classified id today
        // The design pins the appearance probability at <= 20% per offer set (R9.3).
        private const double RewriteProbability = 0.2;

        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            _host = null;
            foreach (WeaponScript weapon in _createdWeapons) if (weapon) Object.DestroyImmediate(weapon);
            _createdWeapons.Clear();
            foreach (ArsenalAbility ability in _createdAbilities) if (ability) Object.DestroyImmediate(ability);
            _createdAbilities.Clear();
        }

        private readonly System.Collections.Generic.List<WeaponScript> _createdWeapons =
            new System.Collections.Generic.List<WeaponScript>();
        private readonly System.Collections.Generic.List<ArsenalAbility> _createdAbilities =
            new System.Collections.Generic.List<ArsenalAbility>();

        // A weapon runtime copy with one live slot-1 (Q) ability so HasRewriteTarget() is true; the
        // Rewrite would otherwise be excluded regardless of the roll (R9.5). firesArrows drives which
        // transform title/description OfferReward builds, so both values are exercised across cases.
        private WeaponScript NewWeapon(bool firesArrows)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.name = "TestRunWeapon";
            SetPrivate(weapon, "_firesArrows", firesArrows);
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "TestQ";
            weapon.abilities = new Ability[] { ability };
            _createdWeapons.Add(weapon);
            _createdAbilities.Add(ability);
            return weapon;
        }

        // Builds a RunBoons ready for OfferReward without running Awake/Start: the fields OfferReward
        // reads are injected directly. The GameObject is inactive so no lifecycle runs.
        private RunBoons BuildRun(WeaponScript weapon)
        {
            _host = new GameObject("RunBoonsRewriteHost");
            _host.SetActive(false);
            PlayerActor player = _host.AddComponent<PlayerActor>(); // fresh -> IsDead == false
            RunBoons run = _host.AddComponent<RunBoons>();

            SetPrivate(run, "_player", player);
            SetPrivate(run, "_weapon", weapon);
            SetProperty(run, "WeaponModifiers", new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon)));
            return run;
        }

        // Resets IsChoosing so OfferReward can be driven repeatedly on the same RunBoons.
        private static void ResetChoosing(RunBoons run) => SetProperty(run, "IsChoosing", false);

        private static bool OfferContainsRewrite(RunBoons run)
        {
            int count = 0;
            foreach (RunBoons.Offer offer in run.Choices)
                if (RunModifierPresentation.IsRewriteId(offer.Id)) count++;
            Assert.LessOrEqual(count, 1, "An offer set must never contain more than one Rewrite modifier (R9.3).");
            return count == 1;
        }

        // Feature: modifier-synergies-theme17, Property 23 (at-most-one half)
        // For any weapon/roll, an offer set contains at most one Rewrite modifier.
        // Validates: Requirements 9.3
        [Test]
        public void RewriteOffer_AtMostOnePerSet()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                bool firesArrows = rng.Next(0, 2) == 0;
                WeaponScript weapon = NewWeapon(firesArrows);
                RunBoons run = BuildRun(weapon);

                // Drive the roll across the whole [0,1) range, including values that force the
                // Rewrite in (roll <= 0.2), so the "at most one" bound is checked when a Rewrite is present.
                double roll = rng.NextDouble();
                SetRewriteRoll(run, () => roll);

                ResetChoosing(run);
                run.OfferReward(0);

                OfferContainsRewrite(run); // asserts count <= 1 internally

                Object.DestroyImmediate(_host);
                _host = null;
            });
        }

        // Feature: modifier-synergies-theme17, Property 23 (empirical-rate half)
        // Across many offer sets the empirical Rewrite appearance rate is <= 20% within tolerance.
        // The roll is uniform on [0,1); the gate admits a Rewrite iff roll <= 0.2, so the expected
        // rate is exactly 0.2. We assert the measured rate does not exceed 0.2 + a sampling tolerance.
        // Validates: Requirements 9.3
        [Test]
        public void RewriteOffer_EmpiricalRateWithinCap()
        {
            const int sets = 4000;
            var seedRng = new System.Random(unchecked((int)0x9E3779B1)); // fixed seed -> reproducible

            WeaponScript weapon = NewWeapon(firesArrows: true);
            RunBoons run = BuildRun(weapon);

            int appeared = 0;
            for (int s = 0; s < sets; s++)
            {
                double roll = seedRng.NextDouble(); // uniform [0,1)
                SetRewriteRoll(run, () => roll);
                ResetChoosing(run);
                run.OfferReward(0);
                if (OfferContainsRewrite(run)) appeared++;
            }

            double rate = (double)appeared / sets;
            // Binomial sampling tolerance: ~4 standard deviations of a p=0.2, n=4000 process (~0.025).
            const double tolerance = 0.03;
            Assert.LessOrEqual(rate, RewriteProbability + tolerance,
                $"Empirical Rewrite appearance rate {rate:F3} exceeded the {RewriteProbability:F2} cap (+{tolerance:F2} tol).");
            // Sanity floor: the gate must actually admit Rewrites at roughly the intended rate,
            // otherwise the cap would be trivially satisfied by never offering one.
            Assert.GreaterOrEqual(rate, RewriteProbability - tolerance,
                $"Empirical Rewrite appearance rate {rate:F3} fell far below the intended {RewriteProbability:F2} rate.");
        }

        // Assigns the private _rewriteRoll seam field on RunBoons so the Rewrite appearance gate is
        // driven deterministically (reflection matches the established private-field seam pattern).
        private static void SetRewriteRoll(RunBoons run, System.Func<double> roll)
        {
            FieldInfo field = typeof(RunBoons).GetField("_rewriteRoll", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected private field _rewriteRoll on RunBoons (test seam).");
            field.SetValue(run, roll);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected private field " + fieldName + " on " + target.GetType().Name + " (test seam).");
            field.SetValue(target, value);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(property, "Expected property " + propertyName + " on " + target.GetType().Name + " (test seam).");
            property.SetValue(target, value);
        }
    }
}
