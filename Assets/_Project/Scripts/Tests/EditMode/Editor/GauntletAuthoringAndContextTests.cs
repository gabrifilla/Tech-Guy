using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class GauntletAuthoringAndContextTests
    {
        private GameObject _ownerObject;
        private Actor _owner;
        [SetUp] public void SetUp() { _ownerObject = new GameObject("GauntletContextTest"); _owner = _ownerObject.AddComponent<Actor>(); }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(_ownerObject); }

        // gauntlet-2-rework P9/P10/P16: migration retains originals and never offers an alias twice.
        [Test]
        public void LegacyRanksRoundTripAndImportIdempotentlyWithoutDeletingUnknownRecords()
        {
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            var record = new WeaponRunModifiers.LegacyRankRecord("weapon_ShockRing", 3);
            var restored = JsonUtility.FromJson<WeaponRunModifiers.LegacyRankRecord>(JsonUtility.ToJson(record));
            Assert.AreEqual(record.Id, restored.Id); Assert.AreEqual(record.Rank, restored.Rank);
            Assert.IsTrue(mods.ImportLegacyGauntletRank(restored));
            Assert.IsTrue(mods.ImportLegacyGauntletRank(restored));
            Assert.IsTrue(mods.ImportLegacyGauntletRank(new WeaponRunModifiers.LegacyRankRecord(record.Id, 1)));
            Assert.AreEqual(3, mods.Rank(WeaponBoon.ShockRing));
            Assert.AreEqual(2, mods.LegacySource.Count, "distinct original records survive; identical reimports do not duplicate");
            Assert.IsTrue(mods.GauntletMutations.TryGet(GauntletActionSlot.E, out var mutation));
            Assert.AreEqual(GauntletMutation.Epicenter, mutation);
            Assert.IsFalse(mods.GauntletMutations.Select(GauntletMutation.Epicenter));
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                "Gauntlet legacy import: preserved unsupported record 'future_unknown' rank 9 without applying it.");
            Assert.IsFalse(mods.ImportLegacyGauntletRank(new WeaponRunModifiers.LegacyRankRecord("future_unknown", 9)));
            Assert.AreEqual("future_unknown", mods.LegacySource[2].Id);
            Assert.AreEqual(9, mods.LegacySource[2].Rank);
            mods.ClearRunInventory();
            Assert.AreEqual(0, mods.Rank(WeaponBoon.ShockRing)); Assert.AreEqual(0, mods.LegacySource.Count);
            Assert.IsFalse(mods.GauntletMutations.TryGet(GauntletActionSlot.E, out _));
        }

        [Test]
        public void RewardMetadataDistinguishesTenTechniquesAndFourUniqueMutations()
        {
            int techniques = 0;
            foreach (var definition in WeaponRunModifiers.Catalog)
            {
                if (!GauntletContentAuthoring.IsSelectedTechnique(definition.Kind)) continue;
                techniques++;
                var view = RunModifierPresentation.For(new RunBoons.Offer(definition.Id, definition.Title, definition.Description));
                Assert.AreEqual("TECHNIQUE", view.Category); Assert.AreEqual(3, view.MaxRank);
            }
            Assert.AreEqual(10, techniques);
            foreach (GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation)))
            {
                string id = GauntletContentAuthoring.ForMutation(mutation).Id;
                Assert.IsTrue(GauntletContentAuthoring.TryMutation(id, out var restored)); Assert.AreEqual(mutation, restored);
                var offer = new RunBoons.Offer(id, GauntletContentAuthoring.MutationTitle(mutation), GauntletContentAuthoring.MutationDescription(mutation));
                var view = RunModifierPresentation.For(offer);
                Assert.AreEqual("MUTATION", view.Category); Assert.AreEqual(1, view.MaxRank);
                Assert.AreEqual(GauntletMutationSelection.SlotOf(mutation).ToString(), view.Scope);
                StringAssert.Contains("→", view.Description);
                Assert.IsFalse(view.IsRewrite, "existing Rewrite rarity contract is independent");
            }
            Assert.IsFalse(GauntletContentAuthoring.TryMutation("gauntlet_mutation_999", out _));
        }

        // Feature: gauntlet-2-rework, Properties 2/12. Validates: Requirements 2.5, 7.7.
        [Test]
        public void LocalProducersRejectDescendantReproductionWithoutBlockingOtherConsumers()
        {
            var registry = new GauntletContextRegistry(_owner);
            var root = registry.Begin(GauntletActionSlot.W);
            var echo = registry.BeginChild(root, GauntletImpactOrigin.Echo, WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho));
            foreach (var producer in new[] { WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho),
                WeaponRunModifiers.CatalogId(WeaponBoon.AsuraEcho),
                GauntletContentAuthoring.ForMutation(GauntletMutation.PressureField).Id })
                Assert.Throws<InvalidOperationException>(() => registry.BeginChild(echo, GauntletImpactOrigin.Persistent, producer));
            var other = registry.BeginChild(echo, GauntletImpactOrigin.External, "legacy-consumer");
            Assert.IsTrue(registry.IsValid(other));
            registry.Close(root); registry.Close(echo); registry.Close(other); Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void ClosingRootKeepsOnlyAuthorizedChildrenAndRejectsReplayAfterRelease()
        {
            var registry = new GauntletContextRegistry(_owner);
            var root = registry.Begin(GauntletActionSlot.W);
            var child = registry.BeginChild(root, GauntletImpactOrigin.Echo, "testEcho");
            Assert.IsTrue(registry.Close(root));
            Assert.IsFalse(registry.IsValid(root.ForImpact(0, 1, 0)));
            Assert.IsTrue(registry.IsValid(child.ForImpact(0, 1, 1)));
            Assert.Throws<InvalidOperationException>(() => registry.BeginChild(root, GauntletImpactOrigin.Echo, "replay"));
            Assert.IsTrue(registry.Close(child));
            Assert.AreEqual(0, registry.Count);
            Assert.IsFalse(registry.Close(child));
            var next = registry.Begin(GauntletActionSlot.Q);
            Assert.Greater(next.Execution, child.Execution);
            Assert.IsFalse(registry.IsValid(child));
        }

        [Test]
        public void EpochOwnerFamilyAndPoolingIdentityRemainCaptured()
        {
            var registry = new GauntletContextRegistry(_owner);
            var original = registry.Begin(GauntletActionSlot.E, true);
            var impact = original.ForImpact(2, 7, 3);
            var reused = original.ForImpact(2, 7, 4);
            Assert.AreEqual(_owner, impact.Owner);
            Assert.AreNotEqual(impact.TargetGeneration, reused.TargetGeneration);
            registry.Invalidate();
            Assert.IsFalse(registry.IsValid(impact));
            var next = registry.Begin(GauntletActionSlot.Q, family: RunWeaponFamily.Bow);
            Assert.IsFalse(next.EnergyEligible);
            Assert.Greater(next.Epoch, impact.Epoch);
            Assert.Greater(next.Execution, impact.Execution);
            Assert.AreEqual(1, registry.Count);
            Assert.Throws<ArgumentNullException>(() => new GauntletContextRegistry(null));
        }

        [Test]
        public void OnlyDirectBasicGuardBreakerChildRetainsBreakEligibility()
        {
            var registry = new GauntletContextRegistry(_owner);
            var basic = registry.Begin(GauntletActionSlot.Basic);
            var guard = registry.BeginChild(basic, GauntletImpactOrigin.Technique, WeaponRunModifiers.CatalogId(WeaponBoon.GuardBreaker));
            var echo = registry.BeginChild(basic, GauntletImpactOrigin.Echo, "testEcho");
            var grandchild = registry.BeginChild(echo, GauntletImpactOrigin.Technique, WeaponRunModifiers.CatalogId(WeaponBoon.GuardBreaker));
            Assert.IsTrue(guard.EnergyEligible);
            Assert.IsFalse(echo.EnergyEligible);
            Assert.IsFalse(grandchild.EnergyEligible);
            Assert.AreEqual(basic.RootExecution, grandchild.RootExecution);
            Assert.IsFalse(registry.Begin(GauntletActionSlot.R).EnergyEligible);
        }

        // Feature: gauntlet-2-rework, Property 1. Validates: Requirements 2.3, 2.6.
        [Test]
        public void HpAndBreakReceiptsAreIndependentAndRejectInvalidInput()
        {
            var context = new GauntletContextRegistry(_owner).Begin(GauntletActionSlot.E).ForImpact(0, 1, 0);
            var hp = new ConfirmedHpReceipt(context, 9f);
            var broken = new ConfirmedBreakReceipt(context, 1);
            Assert.AreEqual(9f, hp.Damage);
            Assert.AreEqual(1, broken.BreakId);
            foreach (float invalid in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
                Assert.Throws<ArgumentOutOfRangeException>(() => new ConfirmedHpReceipt(context, invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ConfirmedBreakReceipt(context, 0));
            Assert.Throws<ArgumentException>(() => new ConfirmedHpReceipt(null, 1f));
        }

        // Feature: gauntlet-2-rework, Property 6. Validates: Requirements 2.7, 7.5.
        [Test]
        public void RawEnergyValidationDoesNotClampInvalidAuthoring()
        {
            var energy = new GauntletEnergyAuthoring();
            Assert.IsTrue(energy.TrySnapshot(out var rates, out _));
            Assert.AreEqual(3, rates.Basic); Assert.AreEqual(2, rates.BreaksPerRoot);
            foreach (string json in new[] { "{\"_basic\":-1}", "{\"_capacity\":101}", "{\"_reservePerRank\":21}", "{\"_breaksPerRoot\":9}" })
            {
                var raw = new GauntletEnergyAuthoring();
                JsonUtility.FromJsonOverwrite(json, raw);
                Assert.IsFalse(raw.TrySnapshot(out _, out var error));
                Assert.IsNotEmpty(error);
            }
        }

        [Test]
        public void CatalogValidatesSlotsRanksDuplicatesAndOptionalZero()
        {
            var authoring = ScriptableObject.CreateInstance<GauntletAuthoring>();
            try
            {
                Assert.IsTrue(authoring.Validate(out _)); Assert.AreEqual(14, authoring.Content.Count);
                var entry = GauntletContentAuthoring.ForMutation(GauntletMutation.PressureField);
                JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":0}", entry);
                Assert.IsTrue(entry.HasDuration); Assert.AreEqual(0, entry.Duration); Assert.IsTrue(entry.Validate(out _));
                JsonUtility.FromJsonOverwrite("{\"_slot\":2}", entry); Assert.IsFalse(entry.Validate(out _));
                entry = GauntletContentAuthoring.ForTechnique(WeaponBoon.Momentum);
                JsonUtility.FromJsonOverwrite("{\"_maxRank\":4}", entry); Assert.IsFalse(entry.Validate(out _));
                JsonUtility.FromJsonOverwrite("{\"_content\":[{\"_type\":0,\"_technique\":25,\"_slot\":-1,\"_maxRank\":3},{\"_type\":0,\"_technique\":25,\"_slot\":-1,\"_maxRank\":3}]}", authoring);
                Assert.IsFalse(authoring.Validate(out var error)); StringAssert.Contains("Duplicate", error);
            }
            finally { UnityEngine.Object.DestroyImmediate(authoring); }
        }

        [Test]
        public void NonFiniteTimingAndMissingEnabledProfileAreRejected()
        {
            var entry = GauntletContentAuthoring.ForMutation(GauntletMutation.PressureField);
            var duration = typeof(GauntletContentAuthoring).GetField("_duration", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            foreach (float value in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity, -1f })
            {
                duration.SetValue(entry, value);
                Assert.IsFalse(entry.Validate(out _));
            }
            duration.SetValue(entry, 0f);
            JsonUtility.FromJsonOverwrite("{\"_overrideProfile\":true}", entry);
            Assert.IsFalse(entry.Validate(out _));
        }

        [Test]
        public void EnabledProfileRejectsInvalidRawPhasesBeforeNormalization()
        {
            var source = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            var entry = GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura);
            try
            {
                typeof(GauntletContentAuthoring).GetField("_profileSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(entry, source);
                JsonUtility.FromJsonOverwrite("{\"_overrideProfile\":true}", entry);
                JsonUtility.FromJsonOverwrite("{\"_combatProfile\":{\"startupEnd\":0.8,\"activeEnd\":0.2}}", source);
                Assert.IsFalse(entry.Validate(out _));
                JsonUtility.FromJsonOverwrite("{\"_combatProfile\":{\"startupEnd\":0.2,\"activeEnd\":0.8}}", source);
                Assert.IsTrue(entry.Validate(out _));
            }
            finally { UnityEngine.Object.DestroyImmediate(source); }
        }

        [Test]
        public void GeneratedNestedClosuresDoNotLeakOrResurrectExecutions()
        {
            PropertyCheck.ForAll((rng, iteration) =>
            {
                var registry = new GauntletContextRegistry(_owner);
                var root = registry.Begin(GauntletActionSlot.W);
                var child = registry.BeginChild(root, GauntletImpactOrigin.Persistent, "zone");
                var nested = registry.BeginChild(child, GauntletImpactOrigin.Echo, "echo");
                if (rng.Next(2) == 0) { registry.Close(root); registry.Close(child); }
                else { registry.Close(child); registry.Close(root); }
                Assert.IsTrue(registry.IsValid(nested));
                registry.Close(nested);
                Assert.AreEqual(0, registry.Count);
                Assert.IsFalse(registry.IsValid(root)); Assert.IsFalse(registry.IsValid(nested));
            });
        }

        // Feature: gauntlet-2-rework, Property 10. Validates: Requirements 7.8.
        [Test]
        public void AuthoredValuesAndProfileReferenceSurviveAssetSaveAndReload()
        {
            string path = "Assets/GauntletAuthoringTest_" + Guid.NewGuid().ToString("N") + ".asset";
            string profilePath = "Assets/GauntletProfileTest_" + Guid.NewGuid().ToString("N") + ".asset";
            var authoring = ScriptableObject.CreateInstance<GauntletAuthoring>();
            var profile = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            JsonUtility.FromJsonOverwrite("{\"_combatProfile\":{\"startupEnd\":0.2,\"activeEnd\":0.6}}", profile);
            try
            {
                AssetDatabase.CreateAsset(profile, profilePath);
                AssetDatabase.CreateAsset(authoring, path);
                var serialized = new SerializedObject(authoring);
                serialized.FindProperty("_energy._basic").intValue = 7;
                var entry = serialized.FindProperty("_content").GetArrayElementAtIndex(13);
                entry.FindPropertyRelative("_overrideDuration").boolValue = true;
                entry.FindPropertyRelative("_duration").floatValue = 0;
                entry.FindPropertyRelative("_overrideProfile").boolValue = true;
                entry.FindPropertyRelative("_profileSource").objectReferenceValue = profile;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(authoring); AssetDatabase.SaveAssets();
                Resources.UnloadAsset(authoring); authoring = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                var loaded = AssetDatabase.LoadAssetAtPath<GauntletAuthoring>(path);
                Assert.IsTrue(loaded.Validate(out var error), error);
                Assert.IsTrue(loaded.Energy.TrySnapshot(out var rates, out _)); Assert.AreEqual(7, rates.Basic);
                Assert.IsTrue(loaded.Content[13].HasDuration); Assert.AreEqual(0, loaded.Content[13].Duration);
                Assert.AreEqual(profilePath, AssetDatabase.GetAssetPath(loaded.Content[13].ProfileSource));
            }
            finally
            {
                AssetDatabase.DeleteAsset(path); AssetDatabase.DeleteAsset(profilePath);
                if (authoring) UnityEngine.Object.DestroyImmediate(authoring);
                if (profile && !AssetDatabase.Contains(profile)) UnityEngine.Object.DestroyImmediate(profile);
            }
        }
    }
}
