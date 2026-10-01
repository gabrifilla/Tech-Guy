using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random; // PropertyCheck.ForAll supplies a System.Random; disambiguate from UnityEngine.Random.

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for gauntlet-boon-playstyle-overhaul task 19.1 — impactful family variety.
    ///
    /// Property 9 (design): for any run state with a Run_Family equipped and at least one Impactful_Boon
    /// of that family below its <c>MaxRank</c>, every reward set <see cref="RunBoons.OfferReward"/> composes
    /// SHALL include at least one Impactful_Boon offer for that family. This is the "variety" guarantee that
    /// keeps the family slot meaningful after retirement thinned the pool: the family offer slot always
    /// surfaces a still-eligible impactful boon.
    ///
    /// The Impactful_Boons of a family are its NON-RETIRED catalog entries: the ten new playstyle boons PLUS
    /// the preserved family boons that survived retirement (the weak/"sem graça" ones were retired, R2.2; what
    /// remains changes how the weapon plays, matching the Impactful_Boon glossary and the design balance
    /// table). Retired family boons (LongFists/StanceCrusher/.../Affliction) are never offered, so they can
    /// never satisfy the variety guarantee; only the non-retired impactful set can. The family offer slot in
    /// OfferReward always surfaces one such entry while any is eligible, which is exactly R14.1.
    ///
    /// Test seam: mirrors <see cref="RetirementOfferPropertyTests"/> / <see cref="PreservedBoonOfferExampleTests"/>.
    /// <see cref="RunBoons"/> is a MonoBehaviour, so the object graph is built on an INACTIVE GameObject (no
    /// Awake/Start lifecycle) and the private fields <c>OfferReward</c> reads (<c>_player</c>, <c>_weapon</c>,
    /// <c>WeaponModifiers</c>) are injected by reflection. This exercises the real <c>OfferReward</c>
    /// composition path, not a re-implementation. The Rewrite roll is forced above its 0.2 gate so the
    /// deterministic family/retire path is what the property observes.
    ///
    /// FsCheck/CsCheck cannot be resolved on this machine, so the seeded <see cref="PropertyCheck"/> harness
    /// drives &gt;= 100 deterministic generated cases and reports the exact failing case as a counterexample.
    ///
    /// Validates: Requirements 14.1
    /// </summary>
    public sealed class ImpactfulVarietyPropertyTests
    {
        // Retired family boons (mirrors RunBoons.RetiredFamilyBoons) — these are never offered, so a family's
        // Impactful_Boon set for R14.1 is its catalog entries MINUS these.
        private static readonly HashSet<WeaponBoon> Retired = new HashSet<WeaponBoon>
        {
            WeaponBoon.LongFists, WeaponBoon.StanceCrusher, WeaponBoon.Berserker,   // Gauntlet
            WeaponBoon.HeavyBolt, WeaponBoon.Sniper,        WeaponBoon.LongRain,    // Bow
            WeaponBoon.LongReach, WeaponBoon.TripleMoon,    WeaponBoon.Affliction   // Spear
        };

        // A family's Impactful_Boons = its non-retired catalog entries (the ten new boons plus the preserved
        // family boons that survived retirement). Computed from the live Catalog so it stays in lockstep with
        // registration rather than hard-coding the list.
        private static WeaponBoon[] ImpactfulFor(RunWeaponFamily family)
        {
            var list = new List<WeaponBoon>();
            foreach (WeaponRunModifiers.Definition def in WeaponRunModifiers.Catalog)
                if (def.Family == family && !Retired.Contains(def.Kind)) list.Add(def.Kind);
            return list.ToArray();
        }

        private readonly List<WeaponScript> _createdWeapons = new List<WeaponScript>();
        private readonly List<Ability> _createdAbilities = new List<Ability>();
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            _host = null;
            foreach (WeaponScript weapon in _createdWeapons) if (weapon) UnityEngine.Object.DestroyImmediate(weapon);
            _createdWeapons.Clear();
            foreach (Ability ability in _createdAbilities) if (ability) UnityEngine.Object.DestroyImmediate(ability);
            _createdAbilities.Clear();
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 9
        // For any family equipped and at least one of its impactful boons below MaxRank, every reward set
        // includes at least one impactful offer for that family. We seed a random mix of ranks across the
        // family's impactful boons, deliberately keeping at least one below MaxRank, and require the offered
        // set to carry a still-eligible impactful id for that family.
        // Validates: Requirements 14.1
        [Test]
        public void EveryRewardSet_IncludesAnImpactfulFamilyOffer_WhenOneIsEligible()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily family = RandomFamily(rng);
                RunBoons run = BuildRun(family, out WeaponRunModifiers modifiers);

                WeaponBoon[] impactful = ImpactfulFor(family);
                var impactfulIds = new HashSet<string>();
                foreach (WeaponBoon kind in impactful) impactfulIds.Add(WeaponRunModifiers.CatalogId(kind));

                // Seed a random rank mix across the family's impactful boons while guaranteeing that at
                // least one stays strictly below MaxRank — the precondition of Property 9.
                WeaponBoon mustStayEligible = impactful[rng.Next(impactful.Length)];
                var eligible = new List<WeaponBoon>();
                foreach (WeaponBoon kind in impactful)
                {
                    WeaponRunModifiers.Definition def = FindDefinition(kind);
                    // Leave the chosen boon below MaxRank; others get a random rank up to MaxRank.
                    int cap = kind == mustStayEligible ? def.MaxRank - 1 : def.MaxRank;
                    int picks = rng.Next(0, cap + 1);
                    for (int r = 0; r < picks; r++) modifiers.Add(def);
                    if (modifiers.Rank(kind) < def.MaxRank) eligible.Add(kind);
                }

                // The precondition holds by construction (mustStayEligible is below MaxRank).
                PropertyCheck.That(eligible.Count > 0,
                    $"Test setup error: no impactful {family} boon left below MaxRank.");

                DriveOfferReward(run, rng);

                bool hasImpactful = false;
                foreach (RunBoons.Offer offer in run.Choices)
                    if (impactfulIds.Contains(offer.Id)) { hasImpactful = true; break; }

                string offered = string.Join(", ", OfferedIds(run));
                PropertyCheck.That(hasImpactful,
                    $"Reward set for {family} (eligible impactful: {string.Join("/", eligible)}) " +
                    $"included no impactful family offer. Offered: [{offered}].");

                DestroyHost();
            });
        }

        // ---- Helpers ----------------------------------------------------------------------------------

        private static List<string> OfferedIds(RunBoons run)
        {
            var ids = new List<string>();
            foreach (RunBoons.Offer offer in run.Choices) ids.Add(offer.Id);
            return ids;
        }

        private static RunWeaponFamily RandomFamily(Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return RunWeaponFamily.Gauntlet;
                case 1: return RunWeaponFamily.Bow;
                default: return RunWeaponFamily.Spear;
            }
        }

        // Builds a weapon runtime copy whose family matches `family` (via WeaponRunModifiers.Identify) with
        // one live slot-1 ability so OfferReward's HasRewriteTarget() precondition can be satisfied.
        private WeaponScript NewWeapon(RunWeaponFamily family)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.name = "ImpactfulVarietyWeapon_" + family;
            SetPrivate(weapon, "_firesArrows", family == RunWeaponFamily.Bow);

            Ability slotQ;
            if (family == RunWeaponFamily.Gauntlet)
            {
                var gauntlet = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
                gauntlet.name = "TestGauntletQ";
                slotQ = gauntlet;
            }
            else
            {
                var arsenal = ScriptableObject.CreateInstance<ArsenalAbility>();
                arsenal.name = "TestQ";
                slotQ = arsenal;
            }
            _createdAbilities.Add(slotQ);
            weapon.abilities = new Ability[] { slotQ };
            _createdWeapons.Add(weapon);
            return weapon;
        }

        // Builds a RunBoons ready for OfferReward without running Awake/Start by injecting the fields it
        // reads. The GameObject stays inactive so no MonoBehaviour lifecycle runs.
        private RunBoons BuildRun(RunWeaponFamily family, out WeaponRunModifiers modifiers)
        {
            _host = new GameObject("ImpactfulVarietyHost");
            _host.SetActive(false);
            PlayerActor player = _host.AddComponent<PlayerActor>(); // fresh -> IsDead == false
            RunBoons run = _host.AddComponent<RunBoons>();

            WeaponScript weapon = NewWeapon(family);
            modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            Assert.AreEqual(family, modifiers.Family, "Test weapon did not resolve to the requested family.");

            SetPrivate(run, "_player", player);
            SetPrivate(run, "_weapon", weapon);
            SetProperty(run, "WeaponModifiers", modifiers);
            return run;
        }

        // Drives the real OfferReward once, forcing the Rewrite roll above 0.2 so the deterministic
        // family/retirement path is what the property observes. Resets IsChoosing first so it can be driven
        // repeatedly within one run.
        private static void DriveOfferReward(RunBoons run, Random rng)
        {
            SetRewriteRoll(run, () => 1.0); // > 0.2 -> no Rewrite slot forced in
            SetProperty(run, "IsChoosing", false);
            run.OfferReward(rng.Next(0, 8));
        }

        private static WeaponRunModifiers.Definition FindDefinition(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition def in WeaponRunModifiers.Catalog)
                if (def.Kind == kind) return def;
            return null;
        }

        private void DestroyHost()
        {
            if (_host != null) UnityEngine.Object.DestroyImmediate(_host);
            _host = null;
        }

        private static void SetRewriteRoll(RunBoons run, Func<double> roll)
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
