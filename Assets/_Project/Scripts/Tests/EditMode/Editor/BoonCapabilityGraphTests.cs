using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class BoonCapabilityGraphTests
    {
        private const BoonPresentationCriteria Transform = BoonPresentationCriteria.ChangesInput | BoonPresentationCriteria.ChangesGeometryOrOrigin;
        private static BoonDefinition Define(string id, BoonFamily family = BoonFamily.Augment,
            BoonSlotMask slots = BoonSlotMask.Q, BoonCapability requires = BoonCapability.None,
            BoonCapability provides = BoonCapability.None, IEnumerable<string> ids = null, IEnumerable<string> blocks = null,
            bool retained = false, int maxRank = 1, bool enabled = true, BoonCapability removes = BoonCapability.None,
            BoonCapability any = BoonCapability.None, BoonCostOverride cost = null, BoonImpactRules impact = null,
            BoonWeaponMask weapons = BoonWeaponMask.Gauntlet)
            => new BoonDefinition(id, family, weapons, slots, requires, provides, ids, blocks, maxRank,
                retained, enabled, isTransformative: family == BoonFamily.Mutation && !retained,
                presentation: family == BoonFamily.Mutation && !retained ? Transform : BoonPresentationCriteria.None,
                cost: cost, impactRules: impact, removedCapabilities: removes, anyRequiredCapabilities: any);

        private static BoonCapabilityGraph Graph(params BoonDefinition[] definitions)
        {
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(definitions, out var catalog, out var error), Is.True, error);
            return new BoonCapabilityGraph(catalog, BoonWeaponMask.Gauntlet, new[]
            {
                new BoonAbilityConfiguration(BoonSlot.Q, BoonCapability.MeleeHit | BoonCapability.GapClose, 12, 2),
                new BoonAbilityConfiguration(BoonSlot.W, BoonCapability.MeleeHit | BoonCapability.Launch, 15, 3),
                new BoonAbilityConfiguration(BoonSlot.E, BoonCapability.AoE | BoonCapability.AscensionMark, 18, 4),
                new BoonAbilityConfiguration(BoonSlot.R, BoonCapability.MeleeHit | BoonCapability.Finisher, 20, 5)
            });
        }

        [Test]
        public void CapabilitiesCannotBeBorrowedFromAnotherSkillOrWeapon()
        {
            var graph = Graph(Define("air", slots: BoonSlotMask.Any, requires: BoonCapability.MeleeHit | BoonCapability.Launch),
                Define("bow", weapons: BoonWeaponMask.Bow));
            Assert.That(graph.CanAcquire("air", BoonSlot.Q, out _), Is.False, "Q has MeleeHit but Launch belongs to W.");
            Assert.That(graph.TryAcquire("air", BoonSlot.W, out var error), Is.True, error);
            Assert.That(graph.CanAcquire("bow", BoonSlot.Q, out _), Is.False);
        }

        [Test]
        public void AlternativeCapabilityPrerequisiteAcceptsLaunchOrAscensionMarkInTheSameSlot()
        {
            var graph = Graph(Define("fall", slots: BoonSlotMask.Any, any: BoonCapability.Launch | BoonCapability.AscensionMark));
            Assert.That(graph.CanAcquire("fall", BoonSlot.Q, out _), Is.False);
            Assert.That(graph.CanAcquire("fall", BoonSlot.W, out _), Is.True);
            Assert.That(graph.CanAcquire("fall", BoonSlot.E, out _), Is.True);
        }

        [Test]
        public void ReplacementChangesCapabilitiesAndCentralCostsWithoutMutatingTheBase()
        {
            var graph = Graph(Define("uppercut", BoonFamily.Mutation, requires: BoonCapability.GapClose,
                provides: BoonCapability.Launch, removes: BoonCapability.GapClose,
                cost: new BoonCostOverride(true, 0, true, 7)));
            Assert.That(graph.TryAcquire("uppercut", BoonSlot.Q, out var error), Is.True, error);
            Assert.That(graph.Capabilities(BoonSlot.Q).HasFlag(BoonCapability.Launch), Is.True);
            Assert.That(graph.Capabilities(BoonSlot.Q).HasFlag(BoonCapability.GapClose), Is.False);
            Assert.That(graph.Ability(BoonSlot.Q).ManaCost, Is.Zero);
            Assert.That(graph.Ability(BoonSlot.Q).Cooldown, Is.EqualTo(7));
            Assert.That(graph.TryRemove("uppercut", out error), Is.True, error);
            Assert.That(graph.Ability(BoonSlot.Q).ManaCost, Is.EqualTo(12));
            Assert.That(graph.Capabilities(BoonSlot.Q).HasFlag(BoonCapability.GapClose), Is.True);
        }

        [Test]
        public void ReplacementCannotInvalidateAnAlreadyAcquiredSupport()
        {
            var graph = Graph(Define("dash_support", requires: BoonCapability.GapClose),
                Define("uppercut", BoonFamily.Mutation, provides: BoonCapability.Launch, removes: BoonCapability.GapClose));
            Assert.That(graph.TryAcquire("dash_support", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.TryAcquire("uppercut", BoonSlot.Q, out _), Is.False);
            Assert.That(graph.Rank("uppercut"), Is.Zero);
            Assert.That(graph.Capabilities(BoonSlot.Q).HasFlag(BoonCapability.GapClose), Is.True);
        }

        [Test]
        public void OneReplacementAndTwoAugmentsPerSkillAreEnforcedButExistingSupportsRemainComposable()
        {
            var graph = Graph(Define("form_a", BoonFamily.Mutation), Define("form_b", BoonFamily.Mutation),
                Define("support_a"), Define("support_b"), Define("support_c"),
                Define("retained_a", retained: true), Define("retained_b", retained: true), Define("retained_c", retained: true));
            foreach (var id in new[] { "retained_a", "retained_b", "retained_c", "form_a", "support_a", "support_b" })
                Assert.That(graph.TryAcquire(id, BoonSlot.Q, out var error), Is.True, error);
            Assert.That(graph.CanAcquire("support_c", BoonSlot.Q, out _), Is.False);
            Assert.That(graph.CanAcquire("form_b", BoonSlot.Q, out _), Is.False);
            Assert.That(graph.TryRestore(new[] { new BoonAcquisition("form_a", 1, BoonSlot.Q), new BoonAcquisition("form_b", 1, BoonSlot.Q) }, out _), Is.False);
            Assert.That(graph.Rank("retained_c"), Is.EqualTo(1), "Failed restore must leave the original inventory intact.");
        }

        [Test]
        public void ResonancesRequireTheirIdsAndRemovalCannotInvalidateTheBuild()
        {
            var graph = Graph(Define("form", BoonFamily.Mutation, provides: BoonCapability.Launch),
                Define("support", requires: BoonCapability.Launch, provides: BoonCapability.AirFollowup),
                Define("duo", BoonFamily.Resonance, requires: BoonCapability.AirFollowup, ids: new[] { "form", "support" }));
            Assert.That(graph.CanAcquire("duo", BoonSlot.Q, out _), Is.False);
            Assert.That(graph.TryAcquire("form", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.TryAcquire("support", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.TryAcquire("duo", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.TryRemove("form", out _), Is.False);
            Assert.That(graph.TryRemove("support", out _), Is.False);
            Assert.That(graph.TryRemove("duo", out _), Is.True);
            Assert.That(graph.TryRemove("form", out _), Is.False, "The support still needs Launch even without an explicit ID dependency.");
        }

        [Test]
        public void RestoreResolvesProvidersIndependentlyOfRecordOrderAndCannotSelfSatisfy()
        {
            var definitions = new[]
            {
                Define("z_provider", provides: BoonCapability.Launch),
                Define("a_dependent", requires: BoonCapability.Launch),
                Define("self", requires: BoonCapability.Mark, provides: BoonCapability.Mark)
            };
            var graph = Graph(definitions);
            var records = new[] { new BoonAcquisition("a_dependent", 1, BoonSlot.Q), new BoonAcquisition("z_provider", 1, BoonSlot.Q) };
            Assert.That(graph.TryRestore(records, out var error), Is.True, error);
            var expected = graph.Capabilities(BoonSlot.Q);
            Assert.That(graph.TryRestore(records.Reverse(), out error), Is.True, error);
            Assert.That(graph.Capabilities(BoonSlot.Q), Is.EqualTo(expected));
            Assert.That(graph.TryRestore(new[] { new BoonAcquisition("self", 1, BoonSlot.Q) }, out _), Is.False);
        }

        [Test]
        public void OneSidedBlocksApplyInEitherAcquisitionOrder()
        {
            var definitions = new[] { Define("a", blocks: new[] { "b" }), Define("b") };
            var graph = Graph(definitions);
            Assert.That(graph.TryAcquire("a", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.CanAcquire("b", BoonSlot.Q, out _), Is.False);
            graph = Graph(definitions);
            Assert.That(graph.TryAcquire("b", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.CanAcquire("a", BoonSlot.Q, out _), Is.False);
        }

        [Test]
        public void DraftsMythicsRankAndResonanceLimitsCannotLeakIntoAcquisition()
        {
            var graph = Graph(Define("draft", enabled: false), Define("mythic", BoonFamily.Mythic),
                Define("ranked", retained: true, maxRank: 2), Define("duo_a", BoonFamily.Resonance),
                Define("duo_b", BoonFamily.Resonance), Define("duo_c", BoonFamily.Resonance));
            Assert.That(graph.CanAcquire("draft", BoonSlot.Q, out _), Is.False);
            Assert.That(graph.CanAcquire("mythic", BoonSlot.Q, out _), Is.False);
            Assert.That(graph.TryRestore(new[] { new BoonAcquisition("mythic", 1, BoonSlot.Q) }, out _), Is.False);
            Assert.That(graph.TryAcquire("ranked", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.TryAcquire("ranked", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.CanAcquire("ranked", BoonSlot.Q, out _), Is.False);
            Assert.That(graph.TryAcquire("duo_a", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.TryAcquire("duo_b", BoonSlot.Q, out _), Is.True);
            Assert.That(graph.CanAcquire("duo_c", BoonSlot.Q, out _), Is.False);
        }

        [Test]
        public void CadencedAsuraOccupiesRWhileItsRetainedFinisherEchoRemainsCompatible()
        {
            var definitions = BoonOfferCompatibility.RetainedDefinitions().Concat(new[]
            {
                Define("future_r", BoonFamily.Mutation, slots: BoonSlotMask.R)
            }).ToArray();
            var graph = Graph(definitions);
            string cadenced = GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura).Id;
            string echo = WeaponRunModifiers.CatalogId(WeaponBoon.AsuraEcho);
            Assert.That(graph.TryAcquire(cadenced, BoonSlot.R, out var error), Is.True, error);
            Assert.That(graph.TryAcquire(echo, BoonSlot.R, out error), Is.True, error);
            Assert.That(graph.CanAcquire("future_r", BoonSlot.R, out _), Is.False);
            Assert.That(graph.TryRestore(new[] { new BoonAcquisition(cadenced, 1, BoonSlot.R),
                new BoonAcquisition("future_r", 1, BoonSlot.R) }, out _), Is.False);
            Assert.That(graph.Rank(echo), Is.EqualTo(1));
        }

        [Test]
        public void CatalogRejectsDuplicateUnknownAndCyclicPrerequisiteIds()
        {
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(new[] { Define("a"), Define("a") }, out _, out _), Is.False);
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(new[] { Define("a", ids: new[] { "missing" }) }, out _, out _), Is.False);
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(new[] { Define("a", ids: new[] { "b" }), Define("b", ids: new[] { "a" }) }, out _, out _), Is.False);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1f)]
        public void RawCostValuesAreValidatedEvenWhenOverridesAreDisabled(float invalid)
        {
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(new[] { Define("bad_mana", cost: new BoonCostOverride(false, invalid)) }, out _, out _), Is.False);
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(new[] { Define("bad_cooldown", cost: new BoonCostOverride(false, 0, false, invalid)) }, out _, out _), Is.False);
        }

        [Test]
        public void DerivedEffectsCannotRenewRootsEnergyEntitiesOrExceedSharedDeclarations()
        {
            foreach (var impact in new[]
            {
                new BoonImpactRules(canCreateCausalRoot: true), new BoonImpactRules(canGenerateEnergy: true),
                new BoonImpactRules(canSpawnSkillEntities: true), new BoonImpactRules(maxDepth: 5),
                new BoonImpactRules(maxDerivedImpactsPerCast: 33),
                new BoonImpactRules(triggerImpacts: BoonImpactMask.Derived, producedImpact: BoonProducedImpact.Native)
            })
                Assert.That(BoonCatalogValidator.TryCreateSnapshot(new[] { Define("bad", impact: impact) }, out _, out _), Is.False);
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(new[] { Define("limit", impact: new BoonImpactRules(maxDerivedImpactsPerCast: 32, maxDepth: 4)) }, out _, out var error), Is.True, error);
        }

        [Test]
        public void TransformativePresentationRequiresTwoDecisionChanges()
        {
            var one = new BoonDefinition("one", BoonFamily.Mutation, BoonWeaponMask.Gauntlet, BoonSlotMask.Q,
                presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin);
            Assert.That(BoonCatalogValidator.TryCreateSnapshot(new[] { one }, out _, out _), Is.False);
        }

        [Test]
        public void AuthoredCatalogPersistsAfterSaveUnloadAndReload()
        {
            string path = "Assets/_Project/ScriptableObjects/BoonGraphSerializationTest_" + Guid.NewGuid().ToString("N") + ".asset";
            BoonCatalogDefinition authored = ScriptableObject.CreateInstance<BoonCatalogDefinition>();
            try
            {
                var definitions = new[]
                {
                    Define("stable_uppercut", BoonFamily.Mutation, provides: BoonCapability.Launch | BoonCapability.Echo,
                        removes: BoonCapability.GapClose, cost: new BoonCostOverride(true, 0, true, 7)),
                    Define("stable_fall", any: BoonCapability.Launch | BoonCapability.AscensionMark,
                        ids: new[] { "stable_uppercut" }, provides: BoonCapability.AirFollowup)
                };
                Assert.That(authored.Configure(definitions, out var error), Is.True, error);
                AssetDatabase.CreateAsset(authored, path); AssetDatabase.SaveAssets();
                string guid = AssetDatabase.AssetPathToGUID(path);
                Resources.UnloadAsset(authored); authored = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                authored = AssetDatabase.LoadAssetAtPath<BoonCatalogDefinition>(path);
                Assert.That(authored.TryCreateSnapshot(out var snapshot, out error), Is.True, error);
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
                Assert.That(snapshot.TryGet("stable_uppercut", out var form), Is.True);
                Assert.That(form.ProvidedCapabilities, Is.EqualTo(BoonCapability.Launch | BoonCapability.Echo));
                Assert.That(form.RemovedCapabilities, Is.EqualTo(BoonCapability.GapClose));
                Assert.That(form.Cost.OverrideManaCost && form.Cost.OverrideCooldown, Is.True);
                Assert.That(form.Cost.ManaCost, Is.Zero); Assert.That(form.Cost.Cooldown, Is.EqualTo(7));
                Assert.That(snapshot.TryGet("stable_fall", out var support), Is.True);
                Assert.That(support.AnyRequiredCapabilities, Is.EqualTo(BoonCapability.Launch | BoonCapability.AscensionMark));
                Assert.That(support.RequiredBoonIds, Is.EqualTo(new[] { "stable_uppercut" }));
                Assert.That(form.ImpactRules.MaxDepth, Is.EqualTo(1));
            }
            finally
            {
                if (AssetDatabase.LoadAssetAtPath<BoonCatalogDefinition>(path)) AssetDatabase.DeleteAsset(path);
                else if (authored) UnityEngine.Object.DestroyImmediate(authored);
            }
        }
    }
}
