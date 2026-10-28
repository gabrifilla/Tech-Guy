using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class BoonOfferCompatibilityTests
    {
        private static readonly ProtocolKind[] RetainedProtocols =
            { ProtocolKind.Ignite, ProtocolKind.Frost, ProtocolKind.Conductor, ProtocolKind.Detonation };

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void PublishedLoadoutOffersOnlyItsImplementedCompatibleBoons(int weaponIndex)
        {
            var weapon = LoadWeapon(weaponIndex);
            string before = SourceSignature(weapon);
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out string error), Is.True, error);
            var expected = ExpectedWeaponIds(weaponIndex).Concat(RetainedProtocols.Select(ProtocolCatalogValidator.IdFor))
                .OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var candidates = WeaponRunModifiers.Catalog.Select(definition => definition.Id)
                .Concat(Enum.GetValues(typeof(GauntletMutation)).Cast<GauntletMutation>()
                    .Select(mutation => GauntletContentAuthoring.ForMutation(mutation).Id))
                .Concat(RetainedProtocols.Select(ProtocolCatalogValidator.IdFor))
                .Concat(new[] { "reactor", "resonance", "overflow", "vitality", "focus", "transform" })
                .Concat(BoonReworkCatalog.CreateDefinitions().Select(definition => definition.Id))
                .Distinct().Select(id => new RunBoons.Offer(id, id, "Compatibility fixture")).ToArray();
            var filtered = BoonOfferCompatibility.Filter(candidates, modifiers, weapon);
            Assert.That(filtered.Select(offer => offer.Id), Is.EqualTo(expected));
            foreach (string id in expected) Assert.That(BoonOfferCompatibility.CanAcquire(graph, id), Is.True, id);
            foreach (string id in candidates.Select(offer => offer.Id).Except(expected))
                Assert.That(BoonOfferCompatibility.CanAcquire(graph, id), Is.False, id);
            AssertCostsMatch(weapon, graph);
            Assert.That(SourceSignature(weapon), Is.EqualTo(before));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void DraftManifestValidatesAlongsideRetainedDataAndCannotEnterLiveOffers(int weaponIndex)
        {
            var definitions = BoonReworkCatalog.CreateDefinitions().ToArray();
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(definitions, out var catalog, out string error), Is.True, error);
            var retainedIds = BoonOfferCompatibility.ExecutableDefinitions().Select(definition => definition.Id).ToArray();
            Assert.That(definitions.Where(definition => definition.EnabledForOffers).Select(definition => definition.Id),
                Is.EquivalentTo(retainedIds), "Only existing executors may enter offers in this stage.");
            var drafts = definitions.Where(definition => !retainedIds.Contains(definition.Id)).ToArray();
            Assert.That(drafts, Is.Not.Empty);
            Assert.That(drafts.All(definition => !definition.EnabledForOffers), Is.True);
            Assert.That(definitions.Single(definition => definition.Id == BoonReworkCatalog.UppercutId).EnabledForOffers, Is.True);
            Assert.That(drafts.Any(definition => definition.Family == BoonFamily.Augment), Is.True);
            Assert.That(drafts.Any(definition => definition.Family == BoonFamily.Resonance), Is.True);
            Assert.That(definitions.Any(definition => definition.Family == BoonFamily.Mythic), Is.False,
                "Mythics stay outside the initial implementation stage.");
            var weapon = LoadWeapon(weaponIndex);
            string before = SourceSignature(weapon);
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var native, out error), Is.True, error);
            var graph = new BoonCapabilityGraph(catalog, native.Weapon,
                Enum.GetValues(typeof(BoonSlot)).Cast<BoonSlot>().Select(native.Ability).ToArray());
            foreach (var draft in drafts)
                Assert.That(graph.TryAcquire(draft.Id, draft.Slots, out _), Is.False, draft.Id);
            Assert.That(graph.Acquisitions, Is.Empty, "Rejected draft acquisition cannot mutate a build.");
            var draftOffers = drafts.Select(definition => new RunBoons.Offer(definition.Id, definition.Title, definition.Description));
            Assert.That(BoonOfferCompatibility.Filter(draftOffers, modifiers, weapon), Is.Empty);
            Assert.That(SourceSignature(weapon), Is.EqualTo(before));
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void ExhaustionRestoresRetainedRanksWithoutConsumingOtherRewardOpportunities(int weaponIndex)
        {
            var weapon = LoadWeapon(weaponIndex);
            string before = SourceSignature(weapon);
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            var protocolAsset = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            try
            {
                Assert.That(protocolAsset.TryCreateSnapshot(out var catalog, out string error), Is.True, error);
                var protocols = new ProtocolInventory(Guid.NewGuid(), catalog);
                foreach (var definition in WeaponRunModifiers.Catalog.Where(definition =>
                    definition.Family == modifiers.Family && RunBoonOfferCatalog.IsTechnique(definition.Kind)))
                    Fill(modifiers, definition);
                FillCompatibleExtensions(RunRewardKind.Technique, modifiers, weapon, protocols);
                Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon, protocols), Is.Empty);
                var mutations = ManualRewardCatalog.Eligible(RunRewardKind.Mutation, modifiers, weapon, protocols);
                Assert.That(mutations, Is.Not.Empty, "Retained supports cannot consume the mandatory first transformation.");
                Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, protocols, out var graph, out error), Is.True, error);
                Assert.That(mutations.All(offer => BoonOfferCompatibility.CanAcquire(graph, offer.Id)), Is.True);
                Assert.That(ManualRewardCatalog.Alternatives(RunRewardKind.Technique, modifiers, weapon, protocols)
                    .All(offer => ProtocolInventory.TryKind(offer.Id, out _)), Is.True);
                if (modifiers.Family == RunWeaponFamily.Gauntlet)
                {
                    Assert.That(modifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura), Is.True);
                    Assert.That(modifiers.GauntletMutations.Select(GauntletMutation.EclipseUppercut), Is.True);
                    Assert.That(modifiers.TryAcquireViolentFall(), Is.True);
                }
                else
                    foreach (var definition in WeaponRunModifiers.Catalog.Where(definition =>
                        definition.Family == modifiers.Family && RunBoonOfferCatalog.IsMutation(definition.Kind)))
                        Fill(modifiers, definition);
                FillCompatibleExtensions(RunRewardKind.Mutation, modifiers, weapon, protocols);
                // Mutations unlock new Augments and Duos; exhaust that newly eligible pool too.
                FillCompatibleExtensions(RunRewardKind.Technique, modifiers, weapon, protocols);
                Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, protocols, out graph, out error), Is.True, error);
                Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Mutation, modifiers, weapon, protocols), Is.Empty);
                Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Protocol, modifiers, weapon, protocols)
                    .Select(offer => offer.Id), Is.EquivalentTo(RetainedProtocols.Select(ProtocolCatalogValidator.IdFor)));
                foreach (var kind in RetainedProtocols)
                    while (protocols.CanAcquire(kind))
                        Assert.That(protocols.TryAcquire(protocols.SessionId, kind, protocols.Revision, _ => true), Is.True);
                Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, protocols, out graph, out error), Is.True, error);
                foreach (var definition in WeaponRunModifiers.Catalog.Where(definition =>
                    definition.Family == modifiers.Family && RunBoonOfferCatalog.IncludesOffer(definition.Id)))
                {
                    Assert.That(graph.Rank(definition.Id), Is.EqualTo(definition.MaxRank), definition.Id);
                    Assert.That(BoonOfferCompatibility.CanAcquire(graph, definition.Id), Is.False, definition.Id);
                }
                foreach (var kind in RetainedProtocols)
                {
                    string id = ProtocolCatalogValidator.IdFor(kind);
                    Assert.That(graph.Rank(id), Is.EqualTo(catalog.Get(kind).MaxRank), id);
                    Assert.That(BoonOfferCompatibility.CanAcquire(graph, id), Is.False, id);
                }
                foreach (RunRewardKind kind in new[] { RunRewardKind.Technique, RunRewardKind.Mutation, RunRewardKind.Protocol })
                {
                    Assert.That(ManualRewardCatalog.Eligible(kind, modifiers, weapon, protocols), Is.Empty, kind.ToString());
                    Assert.That(ManualRewardCatalog.Alternatives(kind, modifiers, weapon, protocols)
                        .Select(offer => offer.Id), Is.EqualTo(new[] { RunBoonOfferCatalog.ExhaustedId }));
                }
                AssertCostsMatch(weapon, graph);
                Assert.That(SourceSignature(weapon), Is.EqualTo(before));
            }
            finally { Object.DestroyImmediate(protocolAsset); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SpearEchoPhantomAndChainCoexistAtTheirOriginalRanksOnEveryThrust(bool reverseOrder)
        {
            var weapon = LoadWeapon(2);
            string before = SourceSignature(weapon);
            var kinds = new[] { WeaponBoon.EchoThrust, WeaponBoon.PhantomSpear, WeaponBoon.ChainThrust };
            if (reverseOrder) Array.Reverse(kinds);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Spear);
            Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var acquired, out string error), Is.True, error);
            const BoonSlotMask thrustTargets = BoonSlotMask.Q | BoonSlotMask.E | BoonSlotMask.R;
            foreach (var kind in kinds)
            {
                Assert.That(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(kind), out var definition), Is.True);
                for (int rank = 0; rank < definition.MaxRank; rank++)
                {
                    Assert.That(acquired.TryAcquire(definition.Id, thrustTargets, out error), Is.True, error);
                    Assert.That(modifiers.Add(definition), Is.True);
                }
            }
            Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var restored, out error), Is.True, error);
            Assert.That(restored.Acquisitions.Count, Is.EqualTo(3), "Retained supports do not consume the new Augment/replacement limits.");
            Assert.That(restored.Acquisitions.Select(record => record.Id + ":" + record.Rank + ":" + record.Slots),
                Is.EqualTo(acquired.Acquisitions.Select(record => record.Id + ":" + record.Rank + ":" + record.Slots)));
            Assert.That(restored.TryRestore(restored.Acquisitions.Reverse(), out error), Is.True, error);
            foreach (var kind in kinds)
            {
                Assert.That(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(kind), out var definition), Is.True);
                Assert.That(restored.Rank(definition.Id), Is.EqualTo(definition.MaxRank));
                Assert.That(restored.Acquisitions.Single(record => record.Id == definition.Id).Slots, Is.EqualTo(thrustTargets));
            }
            foreach (int slot in new[] { 0, 2, 3 })
            {
                var ability = weapon.abilities[slot] as ArsenalAbility;
                Assert.That(ability, Is.Not.Null);
                Assert.That(ability.Kind, Is.EqualTo(ArsenalSkillKind.Thrust));
                var basePlan = new WeaponRunModifiers(RunWeaponFamily.Spear).Plan(ability, slot);
                var plan = modifiers.Plan(ability, slot);
                Assert.That(plan.Hits, Is.EqualTo(basePlan.Hits + modifiers.Rank(WeaponBoon.EchoThrust)));
                Assert.That(plan.PhantomDelay, Is.GreaterThan(0f));
                Assert.That(plan.ChainThrust, Is.True);
                Assert.That((restored.Capabilities((BoonSlot)slot) & (BoonCapability.Echo | BoonCapability.Chain)),
                    Is.EqualTo(BoonCapability.Echo | BoonCapability.Chain));
            }
            Assert.That(weapon.abilities[1] is ArsenalAbility sweep && sweep.Kind == ArsenalSkillKind.Sweep, Is.True);
            Assert.That(restored.Capabilities(BoonSlot.W) & (BoonCapability.Echo | BoonCapability.Chain), Is.EqualTo(BoonCapability.None));
            AssertCostsMatch(weapon, restored);
            Assert.That(SourceSignature(weapon), Is.EqualTo(before));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1f)]
        public void InvalidRawCooldownIsRejectedWithoutNormalizingOrMutatingSource(float cooldown)
        {
            var source = LoadWeapon(2);
            string before = SourceSignature(source);
            var weapon = Object.Instantiate(source);
            var ability = Object.Instantiate(source.abilities[2]);
            try
            {
                weapon.abilities = source.abilities.ToArray();
                weapon.abilities[2] = ability;
                ability.cooldownTime = cooldown;
                var modifiers = new WeaponRunModifiers(RunWeaponFamily.Spear);
                Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out string error), Is.False);
                Assert.That(graph, Is.Null);
                Assert.That(error, Is.Not.Null.And.Not.Empty);
                if (float.IsNaN(cooldown)) Assert.That(float.IsNaN(ability.cooldownTime), Is.True);
                else Assert.That(ability.cooldownTime, Is.EqualTo(cooldown));
                Assert.That(BoonOfferCompatibility.Filter(new[] { new RunBoons.Offer("weapon_EchoThrust", "Echo", "") }, modifiers, weapon), Is.Empty);
                Assert.That(SourceSignature(source), Is.EqualTo(before));
            }
            finally { Object.DestroyImmediate(ability); Object.DestroyImmediate(weapon); }
        }

        [Test]
        public void BowRainIsAZoneAndCannotBorrowRicochetProjectileCapabilities()
        {
            var weapon = LoadWeapon(1);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Bow);
            Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out var error), Is.True, error);
            Assert.That(((ArsenalAbility)weapon.abilities[3]).Kind, Is.EqualTo(ArsenalSkillKind.Rain));
            Assert.That(graph.Capabilities(BoonSlot.R) & BoonCapability.Projectile, Is.EqualTo(BoonCapability.None));
            Assert.That(graph.Capabilities(BoonSlot.R) & BoonCapability.PersistentZone, Is.Not.EqualTo(BoonCapability.None));
            Assert.That(graph.TryAcquire(WeaponRunModifiers.CatalogId(WeaponBoon.Ricochet), BoonSlot.R, out _), Is.False);
            Assert.That(BoonOfferCompatibility.CanAcquire(graph, WeaponRunModifiers.CatalogId(WeaponBoon.Ricochet)), Is.True);
        }

        [TestCase(0, BoonReworkCatalog.UppercutId, BoonReworkCatalog.ViolentFallId, "duo_gauntlet_titan_fall", BoonSlot.Q, true)]
        [TestCase(1, BoonReworkCatalog.ForestId, BoonReworkCatalog.InvertedHarvestId, "duo_bow_arrow_graveyard", BoonSlot.E, false)]
        [TestCase(1, BoonReworkCatalog.MinesId, BoonReworkCatalog.PredatorSealId, "duo_bow_hunting_detonator", BoonSlot.E, true)]
        public void CorrectedRecipesKeepPrerequisitesAndEnableOnlyImplementedExecutors(int weaponIndex, string form, string support, string duo, BoonSlot slot, bool implemented)
        {
            var weapon = LoadWeapon(weaponIndex);
            Assert.That(BoonOfferCompatibility.TryBuild(new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon)), weapon,
                null, out var native, out var error), Is.True, error);
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(BoonReworkCatalog.CreateDefinitions(), out var catalog, out error), Is.True, error);
            var graph = new BoonCapabilityGraph(catalog, native.Weapon,
                Enum.GetValues(typeof(BoonSlot)).Cast<BoonSlot>().Select(native.Ability).ToArray());
            Assert.That(graph.TryRestore(new[] { new BoonAcquisition(duo, 1, slot),
                new BoonAcquisition(support, 1, slot), new BoonAcquisition(form, 1, slot) }, out error), Is.True, error);
            Assert.That(graph.CanRemove(form, out _), Is.False);
            Assert.That(graph.TryRemove(duo, out _), Is.True);
            Assert.That(graph.TryAcquire(duo, slot, out _), Is.EqualTo(implemented), "Draft data and implemented Duos have distinct offer gates.");
        }

        [Test]
        public void TwoAlternativeEMutationsStillCannotShareTheirSlot()
        {
            var weapon = LoadWeapon(1);
            Assert.That(BoonOfferCompatibility.TryBuild(new WeaponRunModifiers(RunWeaponFamily.Bow), weapon,
                null, out var native, out var error), Is.True, error);
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(BoonReworkCatalog.CreateDefinitions(), out var catalog, out error), Is.True, error);
            var graph = new BoonCapabilityGraph(catalog, native.Weapon,
                Enum.GetValues(typeof(BoonSlot)).Cast<BoonSlot>().Select(native.Ability).ToArray());
            Assert.That(graph.TryRestore(new[] { new BoonAcquisition(BoonReworkCatalog.ForestId, 1, BoonSlot.E),
                new BoonAcquisition(BoonReworkCatalog.MinesId, 1, BoonSlot.E) }, out _), Is.False);
            Assert.That(graph.Acquisitions, Is.Empty);
        }

        [Test]
        public void RapidBurstNeedsItsActualArrowExecutorAfterLegacyQReplacement()
        {
            var source = LoadWeapon(1);
            string before = SourceSignature(source);
            var weapon = Object.Instantiate(source);
            var q = ScriptableObject.CreateInstance<ArsenalAbility>();
            try
            {
                q.ConfigureRunTransformation(true);
                weapon.abilities = source.abilities.ToArray(); weapon.abilities[0] = q;
                var modifiers = new WeaponRunModifiers(RunWeaponFamily.Bow);
                string id = WeaponRunModifiers.CatalogId(WeaponBoon.RapidBurst);
                Assert.That(q.Kind, Is.EqualTo(ArsenalSkillKind.Volley));
                Assert.That(BoonOfferCompatibility.Filter(new[] { new RunBoons.Offer(id, id, "") }, modifiers, weapon), Is.Empty,
                    "Projectile capability alone does not make Volley an Arrow sequence producer.");
                Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Mutation, modifiers, weapon).Any(o => o.Id == id), Is.False);
                Assert.That(SourceSignature(source), Is.EqualTo(before));
            }
            finally { Object.DestroyImmediate(q); Object.DestroyImmediate(weapon); }
        }

        [Test]
        public void RetainedImpactDeclarationsUseTheSharedCeilingAndCadencedRemainsNative()
        {
            var definitions = BoonOfferCompatibility.RetainedDefinitions().ToArray();
            foreach (var definition in definitions)
            {
                Assert.That(definition.ImpactRules.MaxDerivedImpactsPerCast, Is.EqualTo(32), definition.Id);
                Assert.That(definition.ImpactRules.MaxDepth, Is.EqualTo(4), definition.Id);
            }
            var cadence = definitions.Single(d => d.Id == GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura).Id);
            Assert.That(cadence.ImpactRules.ProducedImpact, Is.EqualTo(BoonProducedImpact.Native));
            Assert.That(cadence.ImpactRules.CanGenerateEnergy, Is.False);
            Assert.That(cadence.ImpactRules.CanCreateCausalRoot, Is.True);
        }

        private static WeaponScript LoadWeapon(int index)
        {
            var weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[index]);
            Assert.That(weapon, Is.Not.Null, WeaponLoadout.ResourcePaths[index]);
            Assert.That(weapon.abilities, Has.Length.EqualTo(4));
            Assert.That(weapon.abilities.All(ability => ability), Is.True);
            return weapon;
        }

        private static IEnumerable<string> ExpectedWeaponIds(int index)
        {
            switch (index)
            {
                case 0: return new[] { WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho), WeaponRunModifiers.CatalogId(WeaponBoon.AsuraEcho),
                    GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura).Id, BoonReworkCatalog.UppercutId,
                    GauntletExpansionCatalog.EchoingFistId, GauntletExpansionCatalog.TemporalEchoId };
                case 1: return new[] { WeaponRunModifiers.CatalogId(WeaponBoon.Ricochet), WeaponRunModifiers.CatalogId(WeaponBoon.RapidBurst),
                    BowBoonCatalog.HuntingBondId, BowBoonCatalog.JudgmentArrowId, BowBoonCatalog.MineGardenId };
                default: return new[] { WeaponRunModifiers.CatalogId(WeaponBoon.EchoThrust), WeaponRunModifiers.CatalogId(WeaponBoon.PhantomSpear),
                    WeaponRunModifiers.CatalogId(WeaponBoon.ChainThrust), SpearBoonCatalog.HorizonId, SpearBoonCatalog.SiegeId, SpearBoonCatalog.OrbitId };
            }
        }

        private static void FillCompatibleExtensions(RunRewardKind kind, WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory protocols)
        {
            for (int i = 0; i < 32; i++)
            {
                var extension = ManualRewardCatalog.Eligible(kind, modifiers, weapon, protocols)
                    .FirstOrDefault(o => BoonExpansionCatalog.TryDefinition(o.Id, out _) || o.Id == BoonReworkCatalog.ViolentFallId);
                if (extension == null) return;
                if (extension.Id == BoonReworkCatalog.ViolentFallId) Assert.IsTrue(modifiers.TryAcquireViolentFall());
                else Assert.IsTrue(BoonExpansionCatalog.TryAcquire(extension.Id, modifiers, weapon, protocols, out string error), error);
            }
            Assert.Fail("Finite rank-one extension pool failed to exhaust.");
        }

        private static void Fill(WeaponRunModifiers modifiers, WeaponRunModifiers.Definition definition)
        {
            while (modifiers.Rank(definition.Kind) < definition.MaxRank)
                Assert.That(modifiers.Add(definition), Is.True, definition.Id);
        }

        private static void AssertCostsMatch(WeaponScript weapon, BoonCapabilityGraph graph)
        {
            for (int slot = 0; slot < 4; slot++)
            {
                Assert.That(graph.Ability((BoonSlot)slot).ManaCost, Is.EqualTo(weapon.abilities[slot].ManaCost), "Mana " + slot);
                Assert.That(graph.Ability((BoonSlot)slot).Cooldown, Is.EqualTo(weapon.abilities[slot].cooldownTime), "Cooldown " + slot);
            }
        }

        private static string SourceSignature(WeaponScript weapon) => JsonUtility.ToJson(weapon) + "\n" +
            string.Join("\n", weapon.abilities.Select(ability => JsonUtility.ToJson(ability)));
    }
}
