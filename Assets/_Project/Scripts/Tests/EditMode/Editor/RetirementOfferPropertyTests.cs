using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random; // PropertyCheck.ForAll supplies a System.Random; disambiguate from UnityEngine.Random.

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for gauntlet-boon-playstyle-overhaul task 4.1: the retirement filter and
    /// offer composition in <see cref="RunBoons.OfferReward"/>.
    ///
    /// Three properties, each driven over at least 100 generated cases through the project's
    /// <see cref="PropertyCheck.ForAll"/> harness (FsCheck/CsCheck are not available on this machine):
    ///
    ///   Property 6 — Aposentados nunca ofertados: no offer in any reward set carries an id in the retired
    ///     inline numeric set (power/haste/recharge/crit/brutal/bulwark/swift) nor the weapon_ id of any
    ///     boon in <c>RunBoons.RetiredFamilyBoons</c> (LongFists/StanceCrusher/Berserker/HeavyBolt/Sniper/
    ///     LongRain/LongReach/TripleMoon/Affliction).
    ///   Property 7 — Composição de ofertas robusta: OfferReward yields the same number of choices (3)
    ///     without throwing in any run state, including when every non-retired family boon of the equipped
    ///     family is already at its MaxRank.
    ///   Property 8 — Compatibilidade com aposentado herdado: a pre-acquired retired-boon rank (as from an
    ///     older run/save) lets the run keep composing offers without error and never re-offers it.
    ///
    /// Test seam: <see cref="RunBoons"/> is a MonoBehaviour, so the object graph is built on an INACTIVE
    /// GameObject (no Awake/Start lifecycle runs) and the private fields OfferReward reads (_player,
    /// _weapon, WeaponModifiers) are injected by reflection — the same private-field seam the sibling
    /// RewriteOfferConstraintTests uses. This exercises the real OfferReward composition path, not a
    /// re-implementation. The Rewrite roll is forced above its 0.2 gate so the deterministic family/retire
    /// path is what each property observes.
    ///
    /// Validates: Requirements 2.1, 2.2, 2.3, 2.5, 14.2, 14.3, 14.5
    /// </summary>
    public sealed class RetirementOfferPropertyTests
    {
        // R2.3/R14.3: every reward set presents exactly this many choices (the inline pool guarantees it).
        private const int ExpectedChoiceCount = 3;

        // R2.1: the inline numeric boons removed from the offer pool.
        private static readonly HashSet<string> RetiredInlineIds = new HashSet<string>
        {
            "power", "haste", "recharge", "crit", "brutal", "bulwark", "swift"
        };

        // R2.2: the family boons retired from the offer composition (mirrors RunBoons.RetiredFamilyBoons).
        private static readonly WeaponBoon[] RetiredFamilyBoons =
        {
            WeaponBoon.LongFists, WeaponBoon.StanceCrusher, WeaponBoon.Berserker,   // Gauntlet
            WeaponBoon.HeavyBolt, WeaponBoon.Sniper,        WeaponBoon.LongRain,    // Bow
            WeaponBoon.LongReach, WeaponBoon.TripleMoon,    WeaponBoon.Affliction   // Spear
        };

        // weapon_ ids of the retired family boons, for fast offer-id membership checks.
        private static readonly HashSet<string> RetiredFamilyIds = BuildRetiredFamilyIds();

        private static HashSet<string> BuildRetiredFamilyIds()
        {
            var set = new HashSet<string>();
            foreach (WeaponBoon kind in RetiredFamilyBoons) set.Add(WeaponRunModifiers.CatalogId(kind));
            return set;
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

        // ---- Property 6 -------------------------------------------------------------------------------

        // Feature: gauntlet-boon-playstyle-overhaul, Property 6
        // No reward set, for any family and any mix of acquired ranks, ever offers a retired inline numeric
        // boon or a retired family boon.
        // Validates: Requirements 2.1, 2.2
        [Test]
        public void RetiredBoons_AreNeverOffered()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily family = RandomFamily(rng);
                RunBoons run = BuildRun(family, out WeaponRunModifiers modifiers);

                // Randomly pre-acquire a mix of ranks (including retired boons) so the pool is exercised in
                // many states; retirement must hold regardless of what was already taken.
                SeedRandomRanks(modifiers, family, rng);

                DriveOfferReward(run, rng);

                foreach (RunBoons.Offer offer in run.Choices)
                {
                    PropertyCheck.That(!RetiredInlineIds.Contains(offer.Id),
                        $"Retired inline numeric boon '{offer.Id}' was offered (family {family}).");
                    PropertyCheck.That(!RetiredFamilyIds.Contains(offer.Id),
                        $"Retired family boon '{offer.Id}' was offered (family {family}).");
                }

                DestroyHost();
            });
        }

        // ---- Property 7 -------------------------------------------------------------------------------

        // Feature: gauntlet-boon-playstyle-overhaul, Property 7
        // OfferReward composes a reward set of the same choice count (3) without throwing in any run state,
        // including when every non-retired family boon of the equipped family is maxed. The inline gameplay
        // pool is always large enough to backfill the three slots once the family pool shrinks.
        // Validates: Requirements 2.3, 14.3, 14.5
        [Test]
        public void OfferReward_YieldsConstantChoiceCount_WithoutThrowing()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily family = RandomFamily(rng);
                RunBoons run = BuildRun(family, out WeaponRunModifiers modifiers);

                // In ~half the cases, drive every non-retired family boon to MaxRank so the family pool is
                // empty — the worst case for the backfill loop (R14.5). Otherwise a random partial state.
                if (rng.Next(0, 2) == 0) MaxOutNonRetiredFamily(modifiers, family);
                else SeedRandomRanks(modifiers, family, rng);

                // OfferReward must not throw; the harness already converts a throw into a counterexample,
                // but assert explicitly for a clearer message tied to the choice-count contract.
                DriveOfferReward(run, rng);

                PropertyCheck.That(run.Choices.Count == ExpectedChoiceCount,
                    $"Expected {ExpectedChoiceCount} choices, got {run.Choices.Count} (family {family}).");

                // Robustness corollary: offered ids are unique within the set (no slot filled twice).
                var seen = new HashSet<string>();
                foreach (RunBoons.Offer offer in run.Choices)
                    PropertyCheck.That(seen.Add(offer.Id),
                        $"Offer id '{offer.Id}' appeared twice in one reward set (family {family}).");

                DestroyHost();
            });
        }

        // ---- Property 8 -------------------------------------------------------------------------------

        // Feature: gauntlet-boon-playstyle-overhaul, Property 8
        // A run that already acquired a retired family boon (as an older run/save would have) keeps composing
        // offers without error and never re-offers that retired boon — at any pre-acquired rank, including max.
        // Validates: Requirements 2.5, 14.2
        [Test]
        public void InheritedRetiredBoon_DoesNotBreakRun_AndIsNeverReoffered()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily family = RandomFamily(rng);
                RunBoons run = BuildRun(family, out WeaponRunModifiers modifiers);

                // Pick a retired family boon that belongs to the equipped family and pre-acquire it to a
                // random rank in [1, MaxRank] directly through the catalog gate — exactly how an inherited
                // run/save would carry it (R2.5/R2.6: retire != delete, so Add still accepts it).
                WeaponBoon inherited = RandomRetiredBoonForFamily(family, rng);
                WeaponRunModifiers.Definition def = FindDefinition(inherited);
                int targetRank = rng.Next(1, def.MaxRank + 1);
                for (int r = 0; r < targetRank; r++)
                    PropertyCheck.That(modifiers.Add(def),
                        $"Inherited retired boon {inherited} should still be addable through the catalog gate (rank {r + 1}).");
                PropertyCheck.That(modifiers.Rank(inherited) == targetRank,
                    $"Inherited retired boon {inherited} should sit at rank {targetRank}.");

                // The run continues: OfferReward composes a full set without error...
                DriveOfferReward(run, rng);
                PropertyCheck.That(run.Choices.Count == ExpectedChoiceCount,
                    $"Inherited {inherited}: expected {ExpectedChoiceCount} choices, got {run.Choices.Count}.");

                // ...and the inherited retired boon is never offered again.
                string inheritedId = WeaponRunModifiers.CatalogId(inherited);
                foreach (RunBoons.Offer offer in run.Choices)
                    PropertyCheck.That(offer.Id != inheritedId,
                        $"Inherited retired boon '{inheritedId}' was re-offered (family {family}, rank {targetRank}).");

                DestroyHost();
            });
        }

        // ---- Helpers ----------------------------------------------------------------------------------

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
            weapon.name = "TestRunWeapon_" + family;
            // Bow is identified by FiresArrows; Gauntlet by a BreakerGauntletAbility in abilities; Spear is
            // the fallback. Keep slot 1 populated regardless so the Rewrite target precondition holds.
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
            _host = new GameObject("RetirementOfferHost");
            _host.SetActive(false);
            PlayerActor player = _host.AddComponent<PlayerActor>(); // fresh -> IsDead == false
            RunBoons run = _host.AddComponent<RunBoons>();

            WeaponScript weapon = NewWeapon(family);
            modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            // Guard the test's family assumption: Identify must resolve to the requested family.
            Assert.AreEqual(family, modifiers.Family, "Test weapon did not resolve to the requested family.");

            SetPrivate(run, "_player", player);
            SetPrivate(run, "_weapon", weapon);
            SetProperty(run, "WeaponModifiers", modifiers);
            return run;
        }

        // Drives the real OfferReward once, forcing the Rewrite roll above 0.2 so the deterministic
        // retirement/family path is what the properties observe (the Rewrite rate itself is covered by
        // RewriteOfferConstraintTests). Resets IsChoosing first so it can be driven repeatedly.
        private static void DriveOfferReward(RunBoons run, Random rng)
        {
            SetRewriteRoll(run, () => 1.0); // > 0.2 -> no Rewrite slot forced in
            SetProperty(run, "IsChoosing", false);
            run.OfferReward(rng.Next(0, 8));
        }

        // Pre-acquires a random mix of ranks across the equipped family's boons (including retired ones) so
        // the pool is exercised in varied states. Uses the real catalog gate (Add), matching production.
        private static void SeedRandomRanks(WeaponRunModifiers modifiers, RunWeaponFamily family, Random rng)
        {
            foreach (WeaponRunModifiers.Definition def in WeaponRunModifiers.Catalog)
            {
                if (def.Family != family) continue;
                int picks = rng.Next(0, def.MaxRank + 1);
                for (int r = 0; r < picks; r++) modifiers.Add(def);
            }
        }

        // Drives every NON-retired family boon of the equipped family to its MaxRank, emptying the family
        // pool so OfferReward must backfill all three slots from the inline gameplay pool (R14.5 worst case).
        private static void MaxOutNonRetiredFamily(WeaponRunModifiers modifiers, RunWeaponFamily family)
        {
            var retired = new HashSet<WeaponBoon>(RetiredFamilyBoons);
            foreach (WeaponRunModifiers.Definition def in WeaponRunModifiers.Catalog)
            {
                if (def.Family != family || retired.Contains(def.Kind)) continue;
                for (int r = modifiers.Rank(def.Kind); r < def.MaxRank; r++) modifiers.Add(def);
            }
        }

        private static WeaponBoon RandomRetiredBoonForFamily(RunWeaponFamily family, Random rng)
        {
            var inFamily = new List<WeaponBoon>();
            foreach (WeaponBoon kind in RetiredFamilyBoons)
                if (FindDefinition(kind).Family == family) inFamily.Add(kind);
            Assert.Greater(inFamily.Count, 0, $"No retired family boon catalogued for {family}.");
            return inFamily[rng.Next(inFamily.Count)];
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
