using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class GPR01FoundationTests : RunSummaryIsolatedPlayModeFixture
    {
        [Test] public void BasicMutationAcceptsExplicitTargetWithoutExpandingLegacyAny()
        {
            var basic = (BoonSlot)4;
            var definition = new BoonDefinition("fixture_basic_form", BoonFamily.Mutation,
                BoonWeaponMask.Gauntlet, (BoonSlotMask)16, BoonCapability.MeleeHit,
                BoonCapability.AoE, enabledForOffers: true, isTransformative: true,
                presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction);
            Assert.IsTrue(BoonCatalogValidator.TryCreateSnapshot(new[] { definition }, out var catalog, out var error), error);
            var graph = new BoonCapabilityGraph(catalog, BoonWeaponMask.Gauntlet,
                Enumerable.Range(0, 4).Select(i => new BoonAbilityConfiguration((BoonSlot)i, BoonCapability.MeleeHit, 10, 3)).ToArray());
            Assert.IsTrue(graph.TryAcquire(definition.Id, basic, out error), error);
            Assert.AreEqual(15, (int)BoonSlotMask.Any);
            Assert.IsTrue(graph.TryRestore(graph.Acquisitions, out error), error);
            Assert.IsFalse(graph.TryAcquire(definition.Id, BoonSlot.Q, out _));
            Assert.AreEqual(1, graph.Acquisitions.Count);
        }

        [Test] public void LegacyConfigurationAndBasicAcquisitionRoundTripWithAtomicRejection()
        {
            Assert.AreEqual(0, (int)BoonSlot.Q); Assert.AreEqual(1, (int)BoonSlot.W);
            Assert.AreEqual(2, (int)BoonSlot.E); Assert.AreEqual(3, (int)BoonSlot.R);
            Assert.AreEqual(-1, (int)GauntletActionSlot.Basic); Assert.AreEqual(4, (int)BoonSlot.Basic);
            var source = ScriptableObject.CreateInstance<BoonCatalogDefinition>();
            var restored = ScriptableObject.CreateInstance<BoonCatalogDefinition>();
            try
            {
                var d = new BoonDefinition("fixture_basic", BoonFamily.Mutation, BoonWeaponMask.Gauntlet,
                    BoonSlotMask.Basic, BoonCapability.MeleeHit, BoonCapability.AoE, enabledForOffers: true,
                    presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction);
                Assert.IsTrue(source.Configure(new[] { d }, out var error), error);
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), restored);
                Assert.IsTrue(restored.TryCreateSnapshot(out var catalog, out error), error);
                var old = Enumerable.Range(0, 4).Select(i => JsonUtility.FromJson<BoonAbilityConfiguration>(
                    "{\"_slot\":" + i + ",\"_capabilities\":1,\"_manaCost\":10,\"_cooldown\":3}")).ToArray();
                var graph = new BoonCapabilityGraph(catalog, BoonWeaponMask.Gauntlet, old);
                Assert.IsTrue(graph.TryAcquire(d.Id, BoonSlot.Basic, out error), error);
                var record = JsonUtility.FromJson<BoonAcquisition>(JsonUtility.ToJson(graph.Acquisitions.Single()));
                Assert.IsTrue(graph.TryRestore(new[] { record }, out error), error);
                Assert.IsFalse(graph.TryRestore(new[] { new BoonAcquisition(d.Id, 1, BoonSlot.Q) }, out _));
                Assert.AreEqual(BoonSlotMask.Basic, graph.Acquisitions.Single().Slots);
                Assert.IsFalse(graph.TryRestore(new[] { record, record }, out _));
                Assert.AreEqual(1, graph.Rank(d.Id)); Assert.AreEqual(10, graph.Ability(BoonSlot.R).ManaCost);
                var bow = new BoonCapabilityGraph(catalog, BoonWeaponMask.Bow, old);
                Assert.IsFalse(bow.CanAcquire(d.Id, BoonSlot.Basic, out _));
                Assert.IsTrue(BoonExpansionCatalog.TryDefinition(TitanicFistsDefinition.Id, out var production));
                Assert.AreEqual(BoonSlotMask.Basic, production.Slots);
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(restored); }
        }

        [Test] public void LegacyUniversalDoesNotAcquireBasicOrBecomeAnotherCastSlot()
        {
            var d = new BoonDefinition("fixture_universal", BoonFamily.Protocol, BoonWeaponMask.Universal,
                BoonSlotMask.Any, enabledForOffers: true);
            Assert.IsTrue(BoonCatalogValidator.TryCreateSnapshot(new[] { d }, out var catalog, out var error), error);
            var graph = new BoonCapabilityGraph(catalog, BoonWeaponMask.Gauntlet,
                Enumerable.Range(0, 4).Select(i => new BoonAbilityConfiguration((BoonSlot)i, BoonCapability.MeleeHit, 10, 3)).ToArray());
            Assert.IsFalse(graph.CanAcquire(d.Id, BoonSlot.Basic, out _));
            Assert.IsTrue(graph.TryAcquire(d.Id, BoonSlotMask.Any, out error), error);
            Assert.AreEqual(BoonSlotMask.Any, graph.Acquisitions.Single().Slots);
        }

        [Test] public void BasicAuthoringSurvivesUnityAssetSaveUnloadAndReload()
        {
            const string path = "Assets/__GPR01FoundationRoundTrip.asset";
            Assert.IsNull(UnityEditor.AssetDatabase.LoadAssetAtPath<BoonCatalogDefinition>(path));
            var source = ScriptableObject.CreateInstance<BoonCatalogDefinition>(); bool created = false;
            try
            {
                var d = new BoonDefinition("fixture_basic_roundtrip", BoonFamily.Mutation, BoonWeaponMask.Gauntlet,
                    BoonSlotMask.Basic, BoonCapability.MeleeHit, BoonCapability.AoE, enabledForOffers: true,
                    presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction);
                Assert.IsTrue(source.Configure(new[] { d }, out var error), error);
                UnityEditor.AssetDatabase.CreateAsset(source, path); created = true;
                UnityEditor.AssetDatabase.SaveAssetIfDirty(source); Resources.UnloadAsset(source);
                var loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<BoonCatalogDefinition>(path);
                Assert.AreEqual(16, new UnityEditor.SerializedObject(loaded).FindProperty("_entries").GetArrayElementAtIndex(0).FindPropertyRelative("_slots").intValue);
                Assert.IsTrue(loaded.TryCreateSnapshot(out var snapshot, out error), error);
                Assert.IsTrue(snapshot.TryGet(d.Id, out var entry)); Assert.AreEqual(BoonSlotMask.Basic, entry.Slots);
            }
            finally
            {
                if (created) Assert.IsTrue(UnityEditor.AssetDatabase.DeleteAsset(path));
                else if (source) UnityEngine.Object.DestroyImmediate(source);
            }
        }

        [UnityTest] public IEnumerator ProtocolAcquisitionKeepsAllUnchangedCooldownsIncludingSpecial()
        {
            yield return SceneManager.LoadSceneAsync("FirstSector"); yield return null; yield return null;
            var player = UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            var holder = player.GetComponent<AbilityHolder>(); var run = player.GetComponent<RunBoons>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var timers = (float[])typeof(AbilityHolder).GetField("cooldownTimers", flags).GetValue(holder);
            var states = (Array)typeof(AbilityHolder).GetField("states", flags).GetValue(holder);
            Assert.AreEqual(4, timers.Length);
            for (int i = 0; i < timers.Length; i++) { timers[i] = 2 + i; states.SetValue(Enum.Parse(states.GetType().GetElementType(), "Cooldown"), i); }
            var before = timers.ToArray();
            Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer", flags).Invoke(run,
                new object[] { new RunBoons.Offer("conductor", "fixture", "Contract injection, not reward economy proof"), false }));
            Assert.IsFalse((bool)typeof(RunBoons).GetMethod("ApplyOffer", flags).Invoke(run,
                new object[] { new RunBoons.Offer("fixture_unknown", "reject", "must not commit"), false }));
            CollectionAssert.AreEqual(before, Enumerable.Range(0, 4).Select(holder.GetRemainingCooldown).ToArray());
        }
    }
}
