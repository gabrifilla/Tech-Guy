using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace TechGuy.Tests.Fluidity.EditMode
{
    /// <summary>Exercises the published loadouts and live offer adapter, without creating an actor or editing assets.</summary>
    public sealed class BoonExpansionOfferTests
    {
        private const int SimulationSeeds = 300;

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EveryExecutableExtensionHasARealTargetAndRejectedAcquisitionIsAtomic(int weaponIndex)
        {
            var weapon = LoadWeapon(weaponIndex);
            string source = SourceSignature(weapon);
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            string inventory = InventorySignature(modifiers);
            Assert.IsFalse(BoonExpansionCatalog.TryAcquire("unknown_extension", modifiers, weapon, null, out _));
            Assert.AreEqual(inventory, InventorySignature(modifiers));

            foreach (var definition in BoonExpansionCatalog.Definitions())
            {
                bool actualTarget = BoonExpansionCatalog.HasTarget(definition, weapon);
                if (actualTarget) continue;
                Assert.IsFalse(BoonExpansionCatalog.TryAcquire(definition.Id, modifiers, weapon, null, out _), definition.Id);
                Assert.AreEqual(inventory, InventorySignature(modifiers), definition.Id);
            }

            var offer = ManualRewardCatalog.Eligible(weaponIndex == 0 ? RunRewardKind.Technique : RunRewardKind.Mutation, modifiers, weapon)
                .First(o => BoonExpansionCatalog.TryDefinition(o.Id, out _));
            Assert.IsTrue(BoonExpansionCatalog.TryAcquire(offer.Id, modifiers, weapon, null, out string error), error);
            string accepted = InventorySignature(modifiers);
            Assert.IsFalse(BoonExpansionCatalog.TryAcquire(offer.Id, modifiers, weapon, null, out error));
            Assert.IsNotEmpty(error);
            Assert.AreEqual(accepted, InventorySignature(modifiers), "A rejected duplicate must not alter any bound skill.");
            Assert.IsFalse(ManualRewardCatalog.Eligible(RunRewardKind.Mutation, modifiers, weapon).Any(o => o.Id == offer.Id));
            Assert.AreEqual(source, SourceSignature(weapon));
        }

        [Test]
        public void ANewAugmentBindsOneLiveSkillAndCannotBorrowOrChangeItsTarget()
        {
            var weapon = LoadWeapon(0);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out string error), error);
            Assert.IsFalse(graph.TryAcquire(GauntletExpansionCatalog.TemporalEchoId, BoonSlotMask.Q | BoonSlotMask.E, out error));
            Assert.IsNotEmpty(error);
            Assert.IsEmpty(graph.Acquisitions);
            Assert.IsTrue(BoonExpansionCatalog.TryAcquire(GauntletExpansionCatalog.TemporalEchoId, modifiers, weapon, null, out error), error);
            var record = modifiers.Expansion.Acquisitions.Single(a => a.Id == GauntletExpansionCatalog.TemporalEchoId);
            Assert.AreEqual(BoonSlotMask.Q, record.Slots, "The card's deterministic target is the first eligible live skill.");
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out graph, out error), error);
            Assert.IsFalse(graph.TryAcquire(record.Id, BoonSlot.E, out error), "A later call cannot silently move an acquired support.");
            Assert.AreEqual(BoonSlotMask.Q, graph.Acquisitions.Single(a => a.Id == record.Id).Slots);
        }

        [Test]
        public void TwoRealOrbitAugmentsBindRAndRetainedThrustSupportDoesNotSpendTheirCapacity()
        {
            var weapon = LoadWeapon(2);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Spear);
            Acquire(SpearBoonCatalog.OrbitId, modifiers, weapon);
            Acquire(SpearBoonCatalog.ReturnMarkId, modifiers, weapon);
            Acquire(SpearBoonCatalog.MoonEchoId, modifiers, weapon);
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out string error), error);
            var rAugments = graph.Acquisitions.Where(a => a.Slots == BoonSlotMask.R &&
                BoonExpansionCatalog.TryDefinition(a.Id, out var definition) && definition.Family == BoonFamily.Augment).ToArray();
            Assert.AreEqual(BoonCapabilityGraph.MaxAugmentsPerAbility, rAugments.Length);
            foreach (var entry in rAugments) Assert.AreEqual(BoonSlotMask.R, entry.Slots);

            string before = InventorySignature(modifiers);
            Assert.IsFalse(BoonExpansionCatalog.TryAcquire(SpearBoonCatalog.MoonEchoId, modifiers, weapon, null, out _));
            Assert.AreEqual(before, InventorySignature(modifiers));

            // A legacy support is allowed only when all its real bound producers survive the replacement.
            Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.EchoThrust), out var retained));
            var native = new WeaponRunModifiers(RunWeaponFamily.Spear);
            Assert.IsTrue(native.Add(retained));
            Acquire(SpearBoonCatalog.HorizonId, native, weapon);
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(native, weapon, null, out graph, out error), error);
            Assert.AreEqual(1, graph.Rank(retained.Id));
        }

        [Test]
        public void RealDuoRejectsBothMissingPrerequisitesBeforeAppearingWithAnExplicitTarget()
        {
            var weapon = LoadWeapon(1);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Bow);
            Assert.IsFalse(ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon)
                .Any(o => o.Id == BowBoonCatalog.HuntingDetonatorId));
            string empty = InventorySignature(modifiers);
            Assert.IsFalse(BoonExpansionCatalog.TryAcquire(BowBoonCatalog.HuntingDetonatorId, modifiers, weapon, null, out _));
            Assert.AreEqual(empty, InventorySignature(modifiers));
            Acquire(BowBoonCatalog.MineGardenId, modifiers, weapon);
            string garden = InventorySignature(modifiers);
            Assert.IsFalse(BoonExpansionCatalog.TryAcquire(BowBoonCatalog.HuntingDetonatorId, modifiers, weapon, null, out _));
            Assert.AreEqual(garden, InventorySignature(modifiers));
            Acquire(BowBoonCatalog.PredatorSealId, modifiers, weapon);
            var card = ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon)
                .Single(o => o.Id == BowBoonCatalog.HuntingDetonatorId);
            StringAssert.Contains("Alvo: E", card.Description);
            Assert.AreEqual(2, card.SelectionWeight, "A completed prerequisite receives weight, never a guaranteed draw.");
            Acquire(card.Id, modifiers, weapon);
            Assert.AreEqual(BoonSlotMask.E, modifiers.Expansion.Acquisitions.Single(a => a.Id == card.Id).Slots);
            Assert.IsFalse(ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon).Any(o => o.Id == card.Id));
        }

        [Test]
        public void CadencedAndAsuraEchoRemainCompatibleWithPerfectRhythm()
        {
            var weapon = LoadWeapon(0);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            Assert.IsTrue(modifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura));
            AddRetained(modifiers, WeaponBoon.AsuraEcho);
            Acquire(GauntletExpansionCatalog.PerfectRhythmId, modifiers, weapon);
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out string error), error);
            Assert.AreEqual(1, graph.Rank(WeaponRunModifiers.CatalogId(WeaponBoon.AsuraEcho)));
            Assert.IsTrue((graph.Capabilities(BoonSlot.R) & BoonCapability.MultiPress) != 0);
            Assert.IsTrue((graph.Capabilities(BoonSlot.R) & BoonCapability.Echo) != 0);
        }

        [Test]
        public void RetainedGlobalTechniquesFollowSurvivingProducersAcrossReplacements()
        {
            var weapon = LoadWeapon(1); var modifiers = new WeaponRunModifiers(RunWeaponFamily.Bow);
            AddRetained(modifiers, WeaponBoon.Ricochet); Acquire(BowBoonCatalog.MineGardenId, modifiers, weapon);
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out string error), error);
            Assert.AreEqual(BoonSlotMask.Q | BoonSlotMask.W,
                graph.Acquisitions.Single(a => a.Id == WeaponRunModifiers.CatalogId(WeaponBoon.Ricochet)).Slots);
            Assert.IsFalse((graph.Capabilities(BoonSlot.E) & BoonCapability.Chain) != 0);
            Assert.IsFalse((graph.Capabilities(BoonSlot.E) & BoonCapability.Projectile) != 0);
            Assert.IsTrue((graph.Capabilities(BoonSlot.Q) & BoonCapability.Chain) != 0);
            // The reverse order reaches the same effective shape and preserves the technique on real arrows.
            var reverse = new WeaponRunModifiers(RunWeaponFamily.Bow); Acquire(BowBoonCatalog.MineGardenId, reverse, weapon);
            AddRetained(reverse, WeaponBoon.Ricochet);
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(reverse, weapon, null, out var other, out error), error);
            CollectionAssert.AreEqual(graph.Acquisitions.Select(a => a.Id + "@" + a.Slots), other.Acquisitions.Select(a => a.Id + "@" + a.Slots));
        }

        [Test]
        public void HuntingBondPreservesRicochetAndHorizonPreservesPhantomSpear()
        {
            var bow = LoadWeapon(1);
            var bowModifiers = new WeaponRunModifiers(RunWeaponFamily.Bow);
            AddRetained(bowModifiers, WeaponBoon.Ricochet);
            Acquire(BowBoonCatalog.HuntingBondId, bowModifiers, bow);
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(bowModifiers, bow, null, out var bowGraph, out string error), error);
            Assert.AreEqual(1, bowGraph.Rank(WeaponRunModifiers.CatalogId(WeaponBoon.Ricochet)));
            Assert.IsTrue((bowGraph.Capabilities(BoonSlot.Q) & BoonCapability.Chain) != 0);

            var spear = LoadWeapon(2);
            var spearModifiers = new WeaponRunModifiers(RunWeaponFamily.Spear);
            AddRetained(spearModifiers, WeaponBoon.PhantomSpear);
            Acquire(SpearBoonCatalog.HorizonId, spearModifiers, spear);
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(spearModifiers, spear, null, out var spearGraph, out error), error);
            Assert.AreEqual(1, spearGraph.Rank(WeaponRunModifiers.CatalogId(WeaponBoon.PhantomSpear)));
            Assert.IsTrue((spearGraph.Capabilities(BoonSlot.Q) & BoonCapability.Echo) != 0);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ExpansionAcquisitionChangesTheRealRewardInventoryKeyAndKeepsSourceCosts(int weaponIndex)
        {
            var weapon = LoadWeapon(weaponIndex);
            string source = SourceSignature(weapon);
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            string before = ManualRewardCatalog.InventoryKey(modifiers, weapon, 17);
            var card = ManualRewardCatalog.Eligible(weaponIndex == 0 ? RunRewardKind.Technique : RunRewardKind.Mutation, modifiers, weapon)
                .First(o => BoonExpansionCatalog.TryDefinition(o.Id, out _));
            Acquire(card.Id, modifiers, weapon);
            string after = ManualRewardCatalog.InventoryKey(modifiers, weapon, 17);
            Assert.AreNotEqual(before, after, "The revision number is deliberately unchanged: acquisition itself invalidates the ticket key.");
            Assert.AreEqual(after, ManualRewardCatalog.InventoryKey(modifiers, weapon, 17));
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out string error), error);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(weapon.abilities[i].ManaCost, graph.Ability((BoonSlot)i).ManaCost);
                Assert.AreEqual(weapon.abilities[i].cooldownTime, graph.Ability((BoonSlot)i).Cooldown);
            }
            Assert.AreEqual(source, SourceSignature(weapon));
        }

        [Test]
        public void WeightedRealCardsDrawWithoutDuplicatesAndFreezeTheirSeededSet()
        {
            var weapon = LoadWeapon(1);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Bow);
            Acquire(BowBoonCatalog.MineGardenId, modifiers, weapon);
            Acquire(BowBoonCatalog.PredatorSealId, modifiers, weapon);
            var source = ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon).ToArray();
            Assert.IsTrue(source.Any(o => o.SelectionWeight == 2));
            var frozen = BoonOfferDraw.Draw(source, new Random(1701), 3);
            var replay = BoonOfferDraw.Draw(source.Reverse().Concat(source), new Random(1701), 3);
            CollectionAssert.AreEqual(frozen.Select(o => o.Id), replay.Select(o => o.Id));
            Assert.AreEqual(frozen.Count, frozen.Select(o => o.Id).Distinct().Count());
            Assert.LessOrEqual(frozen.Count, 3);
            string[] ids = frozen.Select(o => o.Id).ToArray();
            Acquire(BowBoonCatalog.HuntingDetonatorId, modifiers, weapon);
            CollectionAssert.AreEqual(ids, frozen.Select(o => o.Id), "Later inventory changes cannot mutate an issued set of cards.");
            Assert.IsFalse(ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon)
                .Any(o => o.Id == BowBoonCatalog.HuntingDetonatorId));
        }

        [Test]
        public void AnUnweightedRealPoolRetainsTheHistoricalCanonicalShuffle()
        {
            var weapon = LoadWeapon(1);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Bow);
            var cards = ManualRewardCatalog.Eligible(RunRewardKind.Mutation, modifiers, weapon).ToArray();
            Assert.IsTrue(cards.All(o => o.SelectionWeight == 1));
            var expected = cards.OrderBy(o => o.Id, StringComparer.Ordinal).ToList();
            var random = new Random(51);
            for (int i = expected.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                var swap = expected[i]; expected[i] = expected[j]; expected[j] = swap;
            }
            CollectionAssert.AreEqual(expected.Take(3).Select(o => o.Id),
                BoonOfferDraw.Draw(cards.Reverse(), new Random(51), 3).Select(o => o.Id));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ThreeHundredRealCatalogSeedsMeasureAssemblyWithoutPromisingABalanceRate(int weaponIndex)
        {
            var weapon = LoadWeapon(weaponIndex);
            string source = SourceSignature(weapon);
            var weighted = Simulate(weapon, true);
            var uniform = Simulate(weapon, false);
            Assert.Greater(weighted.TransformedRuns, 0, "The published mutation pool must be reachable.");
            Assert.Greater(weighted.DuoEligibleRuns, 0, "Real prerequisites must be assemblable in this explicit opportunity model.");
            Assert.Greater(weighted.DuoAcquiredRuns, 0, "A ready real Duo must be drawable.");
            Assert.LessOrEqual(weighted.DuoAcquiredRuns, weighted.DuoEligibleRuns);
            Assert.AreEqual(SimulationTrace(weapon, true, 42), SimulationTrace(weapon, true, 42));
            Assert.AreEqual(source, SourceSignature(weapon));
            TestContext.WriteLine("Assembly measurement: weapon=" + weaponIndex + "; seeds=" + SimulationSeeds +
                "; schedule=4 Mutation plus 8 Technique; choice=uniform among up to3 drawn cards; weighted eligible=" +
                weighted.DuoEligibleRuns + "/" + SimulationSeeds + ", acquired=" + weighted.DuoAcquiredRuns + "/" + SimulationSeeds +
                "; uniform eligible=" + uniform.DuoEligibleRuns + "/" + SimulationSeeds + ", acquired=" + uniform.DuoAcquiredRuns + "/" + SimulationSeeds +
                ". This measures this opportunity/choice model only; it is not elapsed run time, player preference, or a guaranteed rate.");
        }

        private sealed class SimulationTotals
        {
            public int TransformedRuns, DuoEligibleRuns, DuoAcquiredRuns;
        }

        private static SimulationTotals Simulate(WeaponScript weapon, bool weighted)
        {
            var totals = new SimulationTotals();
            for (int seed = 0; seed < SimulationSeeds; seed++)
            {
                bool transformed = false, eligible = false, acquired = false;
                SimulateRun(weapon, weighted, seed, ref transformed, ref eligible, ref acquired);
                if (transformed) totals.TransformedRuns++;
                if (eligible) totals.DuoEligibleRuns++;
                if (acquired) totals.DuoAcquiredRuns++;
            }
            return totals;
        }

        private static string SimulationTrace(WeaponScript weapon, bool weighted, int seed)
        {
            bool transformed = false, eligible = false, acquired = false;
            return SimulateRun(weapon, weighted, seed, ref transformed, ref eligible, ref acquired);
        }

        private static string SimulateRun(WeaponScript weapon, bool weighted, int seed,
            ref bool transformed, ref bool duoEligible, ref bool duoAcquired)
        {
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            var random = new Random(seed);
            var trace = new List<string>();
            for (int opportunity = 0; opportunity < 12; opportunity++)
            {
                var kind = opportunity % 3 == 0 ? RunRewardKind.Mutation : RunRewardKind.Technique;
                var pool = ManualRewardCatalog.Eligible(kind, modifiers, weapon);
                if (pool.Any(o => BoonExpansionCatalog.TryDefinition(o.Id, out var d) && d.Family == BoonFamily.Resonance))
                    duoEligible = true;
                var cards = weighted ? pool : pool.Select(o => new RunBoons.Offer(o.Id, o.Title, o.Description)).ToArray();
                var draw = BoonOfferDraw.Draw(cards, random, 3);
                Assert.AreEqual(draw.Count, draw.Select(o => o.Id).Distinct().Count(), "Duplicate reward within one draw.");
                if (draw.Count == 0) { trace.Add("-"); continue; }
                var selected = draw[random.Next(draw.Count)];
                Acquire(selected.Id, modifiers, weapon);
                trace.Add(selected.Id);
                if (kind == RunRewardKind.Mutation) transformed = true;
                if (BoonExpansionCatalog.TryDefinition(selected.Id, out var definition) && definition.Family == BoonFamily.Resonance)
                    duoAcquired = true;
                Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out _, out string error), error);
            }
            return string.Join("|", trace);
        }

        private static void Acquire(string id, WeaponRunModifiers modifiers, WeaponScript weapon)
        {
            Assert.IsTrue(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out string diagnostic), diagnostic);
            Assert.IsTrue(BoonOfferCompatibility.CanAcquire(graph, id), "Offered card must still pass the live graph: " + id);
            bool accepted;
            if (BoonExpansionCatalog.TryDefinition(id, out _))
                accepted = BoonExpansionCatalog.TryAcquire(id, modifiers, weapon, null, out diagnostic);
            else if (id == BoonReworkCatalog.ViolentFallId) accepted = modifiers.TryAcquireViolentFall();
            else if (GauntletContentAuthoring.TryMutation(id, out var mutation)) accepted = modifiers.GauntletMutations.Select(mutation);
            else
            {
                Assert.IsTrue(WeaponRunModifiers.TryDefinition(id, out var retained), id);
                accepted = modifiers.Add(retained);
            }
            Assert.IsTrue(accepted, diagnostic ?? id);
        }

        private static void AddRetained(WeaponRunModifiers modifiers, WeaponBoon kind)
        {
            Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(kind), out var definition));
            Assert.IsTrue(modifiers.Add(definition));
        }

        private static WeaponScript LoadWeapon(int index)
        {
            var weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[index]);
            Assert.IsNotNull(weapon, WeaponLoadout.ResourcePaths[index]);
            Assert.AreEqual(4, weapon.abilities.Length);
            return weapon;
        }

        private static string InventorySignature(WeaponRunModifiers modifiers) =>
            string.Join("|", modifiers.Expansion.Acquisitions.Select(a => a.Id + "@" + a.Slots + ":" + a.Rank)) + ";" +
            string.Join("|", WeaponRunModifiers.Catalog.Select(d => d.Id + ":" + modifiers.Rank(d.Kind))) + ";" +
            string.Join("|", Enum.GetValues(typeof(GauntletActionSlot)).Cast<GauntletActionSlot>()
                .Select(slot => slot + ":" + (modifiers.GauntletMutations.TryGet(slot, out var mutation) ? mutation.ToString() : "-"))) +
            ";fall:" + modifiers.HasViolentFall;

        private static string SourceSignature(WeaponScript weapon) => JsonUtility.ToJson(weapon) + "\n" +
            string.Join("\n", weapon.abilities.Select(JsonUtility.ToJson));
    }
}
