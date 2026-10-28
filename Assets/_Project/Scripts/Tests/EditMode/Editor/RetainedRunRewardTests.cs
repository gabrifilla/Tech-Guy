using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class RetainedRunRewardTests
    {
        private static readonly string[] WeaponPaths =
        {
            "Assets/_Project/Resources/Weapons/Melee/Gauntlet/Gauntlet.asset",
            "Assets/_Project/Resources/Weapons/Ranged/Bow_arrow/Bow.asset",
            "Assets/_Project/Resources/Weapons/Melee/Spear/Spear.asset"
        };

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void PublishedWeaponsKeepAnAttainableMutationAfterTechniquesAreExhausted(int index)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponScript>(WeaponPaths[index]);
            Assert.That(ManualRouteValidator.ValidateLoadout(weapon, out var error), Is.True, error);
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            foreach (var definition in WeaponRunModifiers.Catalog.Where(d => d.Family == modifiers.Family && RunBoonOfferCatalog.IsTechnique(d.Kind)))
                while (modifiers.Rank(definition.Kind) < definition.MaxRank) Assert.That(modifiers.Add(definition), Is.True);
            FillCompatibleTechniques(modifiers, weapon);
            Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon), Is.Empty);
            var mutations = ManualRewardCatalog.Eligible(RunRewardKind.Mutation, modifiers, weapon);
            Assert.That(mutations, Is.Not.Empty, "The first mandatory transformation must be reserved for every weapon.");
            Assert.That(mutations.All(o => RunBoonOfferCatalog.IncludesOffer(o.Id)), Is.True);
        }

        [Test]
        public void RetainedCatalogExcludesTheOtherOldBoonsAtEveryProtocolOpportunity()
        {
            var catalog = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            try
            {
                Assert.That(catalog.TryCreateSnapshot(out var snapshot, out var error), Is.True, error);
                var inventory = new ProtocolInventory(Guid.NewGuid(), snapshot);
                foreach (var weaponPath in WeaponPaths)
                {
                    var weapon = AssetDatabase.LoadAssetAtPath<WeaponScript>(weaponPath);
                    var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
                    foreach (RunRewardKind kind in new[] { RunRewardKind.Technique, RunRewardKind.Mutation, RunRewardKind.Protocol })
                        for (int opportunity = 1; opportunity <= 20; opportunity++)
                            Assert.That(ManualRewardCatalog.Eligible(kind, modifiers, weapon, inventory, opportunity)
                                .All(o => RunBoonOfferCatalog.IncludesOffer(o.Id)), Is.True);
                }
                foreach (var id in new[] { "reactor", "resonance", "overflow", "vitality", "focus", "transform", "weapon_LongRain", "weapon_TripleMoon", "weapon_ShockRing" })
                    Assert.That(RunBoonOfferCatalog.IncludesOffer(id), Is.False, id);
            }
            finally { UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void EveryPublishedWeaponCanResolveEveryRouteWithFrozenCompatibleRewards(int index)
        {
            var definition = FluidityRouteTestFactory.Create();
            var catalog = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            try
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponScript>(WeaponPaths[index]);
                Assert.That(ManualRouteValidator.TrySnapshot(definition, weapon, out var snapshot, out var error), Is.True, error);
                Assert.That(catalog.TryCreateSnapshot(out var protocols, out error), Is.True, error);
                foreach (ulong seed in new ulong[] { 1UL, 17UL, 8675309UL })
                foreach (var path in snapshot.Paths)
                {
                    var generated = ManualRunRandomizer.Generate(snapshot, seed, Array.Empty<GameObject>());
                    var route = new ManualRoutePolicy(generated);
                    var policy = new RunRewardPolicy(route);
                    var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
                    var inventory = new ProtocolInventory(route.Session.Id, protocols);
                    var context = new RunActionContext(true);
                    foreach (int nodeId in path.Skip(1))
                    {
                        Assert.That(route.TryCommitDestination(route.Session.Id, nodeId, context), Is.True, "Commit " + nodeId);
                        Assert.That(route.TryEnterNode(route.Session.Id, nodeId, context), Is.True);
                        var node = generated.Node(nodeId);
                        if (node.Kind == ManualNodeKind.Repair)
                        { Assert.That(route.TryCompleteRepair(route.Session.Id, nodeId, context), Is.True); continue; }
                        Assert.That(route.TryResolveEncounter(route.Session.Id, nodeId, true, context), Is.True);
                        if (node.Kind == ManualNodeKind.Boss) continue;
                        for (int ordinal = 0; ordinal < node.TotalAcquisitions; ordinal++)
                        {
                            var request = new RunRewardRequest(route.Session.Id, nodeId, ordinal);
                            string key = ManualRewardCatalog.InventoryKey(modifiers, weapon, route.Session.InventoryRevision) + inventory.InventoryKey();
                            var eligible = ManualRewardCatalog.Eligible(node.RewardAt(ordinal), modifiers, weapon, inventory);
                            var alternatives = ManualRewardCatalog.Alternatives(node.RewardAt(ordinal), modifiers, weapon, inventory);
                            var ticket = policy.Open(request, context, key, eligible, alternatives);
                            Assert.That(ticket.Choices.Count, Is.InRange(1, 3));
                            Assert.That(policy.Open(request, context, key, eligible, alternatives), Is.SameAs(ticket));
                            var selected = ticket.Choices[0];
                            Assert.That(policy.TryChoose(request, ticket.Revision, selected.Id, context, () => true,
                                () => Apply(selected.Id, modifiers, inventory, weapon), null), Is.True, selected.Id);
                            Assert.That(route.Session.ProtocolAcquisitions, Is.EqualTo(inventory.Revision), "Count actual Protocol acquisitions, including alternatives.");
                            Assert.That(policy.TryChoose(request, ticket.Revision, selected.Id, context, () => true, () => false, null), Is.False);
                        }
                    }
                    Assert.That(route.Session.MutationAcquisitions, Is.GreaterThanOrEqualTo(1));
                    Assert.That(route.TryFinalizeBoss(route.Session.Id, context), Is.True);
                    Assert.That(route.TryRequestExtraction(route.Session.Id, context), Is.True);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [Test]
        public void ExhaustedCategoriesUseRetainedProtocolsThenAnExplicitContinueWithoutInventingAnUpgrade()
        {
            var definition = FluidityRouteTestFactory.Create();
            var catalog = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            try
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponScript>(WeaponPaths[1]);
                Assert.That(ManualRouteValidator.TrySnapshot(definition, weapon, out var snapshot, out var error), Is.True, error);
                Assert.That(catalog.TryCreateSnapshot(out var protocolCatalog, out error), Is.True, error);
                snapshot = ManualRunRandomizer.Generate(snapshot, 42UL, Array.Empty<GameObject>());
                var route = new ManualRoutePolicy(snapshot);
                var context = new RunActionContext(true);
                Assert.That(route.TryCommitDestination(route.Session.Id, 1, context), Is.True);
                Assert.That(route.TryEnterNode(route.Session.Id, 1, context), Is.True);
                Assert.That(route.TryResolveEncounter(route.Session.Id, 1, true, context), Is.True);
                var modifiers = new WeaponRunModifiers(RunWeaponFamily.Bow);
                var inventory = new ProtocolInventory(route.Session.Id, protocolCatalog);
                Assert.That(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.Ricochet), out var ricochet), Is.True);
                while (modifiers.Rank(ricochet.Kind) < ricochet.MaxRank) Assert.That(modifiers.Add(ricochet), Is.True);
                var eligible = ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon, inventory);
                Assert.That(eligible, Is.Empty);
                var alternatives = ManualRewardCatalog.Alternatives(RunRewardKind.Technique, modifiers, weapon, inventory);
                Assert.That(alternatives.All(o => ProtocolInventory.TryKind(o.Id, out var kind) && RunBoonOfferCatalog.Includes(kind)), Is.True);
                var request = new RunRewardRequest(route.Session.Id, 1, 0);
                var policy = new RunRewardPolicy(route);
                var ticket = policy.Open(request, context, "technique-exhausted", eligible, alternatives);
                Assert.That(ticket.IsAlternative, Is.True);
                string selected = ticket.Choices[0].Id;
                Assert.That(policy.TryChoose(request, ticket.Revision, selected, context, () => true,
                    () => Apply(selected, modifiers, inventory, weapon), null), Is.True);
                Assert.That(route.Session.ProtocolAcquisitions, Is.EqualTo(1), "A Protocol chosen as a Technique alternative is a real acquisition.");
                Assert.That(route.Session.Phase, Is.EqualTo(RunPhase.RewardPending));
                foreach (var kind in new[] { ProtocolKind.Ignite, ProtocolKind.Frost, ProtocolKind.Conductor, ProtocolKind.Detonation })
                    while (inventory.CanAcquire(kind)) Assert.That(inventory.TryAcquire(inventory.SessionId, kind, inventory.Revision, _ => true), Is.True);
                alternatives = ManualRewardCatalog.Alternatives(RunRewardKind.Protocol, modifiers, weapon, inventory);
                Assert.That(alternatives.Select(o => o.Id), Is.EqualTo(new[] { RunBoonOfferCatalog.ExhaustedId }));
                Assert.That(snapshot.Node(1).RewardAt(1), Is.EqualTo(RunRewardKind.Protocol));
                request = new RunRewardRequest(route.Session.Id, 1, 1);
                eligible = ManualRewardCatalog.Eligible(RunRewardKind.Protocol, modifiers, weapon, inventory);
                ticket = policy.Open(request, context, "protocol-exhausted", eligible, alternatives);
                Assert.That(ticket.IsAlternative, Is.True);
                Assert.That(policy.TryChoose(request, ticket.Revision, RunBoonOfferCatalog.ExhaustedId, context, () => true, () => true, null), Is.True);
                Assert.That(route.Session.Phase, Is.EqualTo(RunPhase.RouteChoice));
                Assert.That(route.Session.MutationAcquisitions, Is.Zero);
                Assert.That(route.Session.ProtocolAcquisitions, Is.EqualTo(1), "Continue must not invent another Protocol acquisition.");
            }
            finally { UnityEngine.Object.DestroyImmediate(definition); UnityEngine.Object.DestroyImmediate(catalog); }
        }

        [TestCase(1, WeaponBoon.RapidBurst)]
        [TestCase(2, WeaponBoon.PhantomSpear)]
        [TestCase(2, WeaponBoon.ChainThrust)]
        public void ReservedMutationsChangeTheExistingWeaponExecution(int index, WeaponBoon boon)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponScript>(WeaponPaths[index]);
            var modifiers = new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon));
            var ability = (ArsenalAbility)weapon.abilities[0];
            var before = modifiers.Plan(ability, 0);
            Assert.That(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(boon), out var definition), Is.True);
            Assert.That(modifiers.Add(definition), Is.True);
            var after = modifiers.Plan(ability, 0);
            if (boon == WeaponBoon.RapidBurst) Assert.That(after.Hits, Is.GreaterThan(before.Hits));
            else if (boon == WeaponBoon.PhantomSpear) Assert.That(after.PhantomDelay, Is.GreaterThan(before.PhantomDelay));
            else Assert.That(after.ChainThrust && !before.ChainThrust, Is.True);
            Assert.That(RunModifierPresentation.For(new RunBoons.Offer(definition.Id, definition.Title, definition.Description)).Category, Is.EqualTo("MUTATION"));
        }

        private static void FillCompatibleTechniques(WeaponRunModifiers modifiers, WeaponScript weapon)
        {
            for (int i = 0; i < 32; i++)
            {
                var offer = ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon).FirstOrDefault();
                if (offer == null) return;
                Assert.That(Apply(offer.Id, modifiers, null, weapon), Is.True, offer.Id);
            }
            Assert.Fail("Finite Technique pool failed to exhaust.");
        }

        private static bool Apply(string id, WeaponRunModifiers modifiers, ProtocolInventory inventory, WeaponScript weapon)
        {
            if (id == RunBoonOfferCatalog.ExhaustedId) return true;
            if (id == BoonReworkCatalog.ViolentFallId) return modifiers.TryAcquireViolentFall();
            if (BoonExpansionCatalog.TryDefinition(id, out _))
                return BoonExpansionCatalog.TryAcquire(id, modifiers, weapon, inventory, out _);
            if (ProtocolInventory.TryKind(id, out var protocol))
                return inventory.TryAcquire(inventory.SessionId, protocol, inventory.Revision, _ => true);
            if (WeaponRunModifiers.TryDefinition(id, out var definition)) return modifiers.Add(definition);
            return GauntletContentAuthoring.TryMutation(id, out var mutation) && modifiers.GauntletMutations.Select(mutation);
        }
    }
}
