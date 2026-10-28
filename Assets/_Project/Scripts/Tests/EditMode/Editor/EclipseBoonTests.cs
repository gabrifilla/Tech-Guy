using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class EclipseBoonTests
    {
        [Test]
        public void UppercutHasStableIdentityWithoutExpandingTheLegacyAuthoringCatalog()
        {
            Assert.That((int)GauntletMutation.CadencedAsura, Is.EqualTo(3));
            Assert.That((int)GauntletMutation.EclipseUppercut, Is.EqualTo(4));
            var entry = GauntletContentAuthoring.ForMutation(GauntletMutation.EclipseUppercut);
            Assert.That(entry.Id, Is.EqualTo(BoonReworkCatalog.UppercutId));
            Assert.That(entry.Slot, Is.EqualTo(GauntletActionSlot.Q));
            Assert.That(entry.Validate(out var error), Is.True, error);
            var authoring = ScriptableObject.CreateInstance<GauntletAuthoring>();
            try { Assert.That(authoring.Content.Count, Is.EqualTo(14)); Assert.That(authoring.Validate(out error), Is.True, error); }
            finally { UnityEngine.Object.DestroyImmediate(authoring); }
        }

        [Test]
        public void UppercutRejectsLegacyProfileOverridesBeforeStartingItsFixedContactScheduler()
        {
            var weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            var q = (BreakerGauntletAbility)weapon.abilities[0];
            var source = UnityEngine.Object.Instantiate(q);
            var authoring = ScriptableObject.CreateInstance<GauntletAuthoring>();
            try
            {
                var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                modifiers.GauntletMutations.Select(GauntletMutation.EclipseUppercut);
                Assert.That(GauntletExecutionSnapshot.TryBuild(q, GauntletActionSlot.Q, modifiers, out _, modifiers.GauntletMutations, authoring), Is.True);
                JsonUtility.FromJsonOverwrite("{\"startupEnd\":0.1,\"activeEnd\":0.9,\"impactEvents\":[{\"index\":0,\"at\":0.75,\"areaHitStepIndex\":0,\"hitStopProfileIndex\":-1}]}", source.CombatProfile);
                var entry = GauntletContentAuthoring.ForMutation(GauntletMutation.EclipseUppercut);
                typeof(GauntletContentAuthoring).GetField("_overrideProfile", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entry, true);
                typeof(GauntletContentAuthoring).GetField("_profileSource", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(entry, source);
                var contents = authoring.Content.Select(item => item.Id == GauntletContentAuthoring.ForMutation(GauntletMutation.DemolishingAdvance).Id ? entry : item).ToList();
                typeof(GauntletAuthoring).GetField("_content", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(authoring, contents);
                Assert.That(authoring.Validate(out var error), Is.True, error);
                Assert.That(GauntletExecutionSnapshot.TryBuild(q, GauntletActionSlot.Q, modifiers, out _, modifiers.GauntletMutations, authoring), Is.False,
                    "Uppercut timing uses the dedicated tuning; an incompatible legacy contact cannot be accepted.");
            }
            finally { UnityEngine.Object.DestroyImmediate(source); UnityEngine.Object.DestroyImmediate(authoring); }
        }

        [Test]
        public void UppercutMergesNativeDamageRemovesAdvanceAndDoesNotMutateAssets()
        {
            var weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            var q = (BreakerGauntletAbility)weapon.abilities[0];
            string before = JsonUtility.ToJson(q);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            Assert.That(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.RocketAdvance), out var advance), Is.True);
            for (int i = 0; i < 3; i++) modifiers.Add(advance);
            modifiers.GauntletMutations.Select(GauntletMutation.EclipseUppercut);
            Assert.That(GauntletExecutionSnapshot.TryBuild(q, GauntletActionSlot.Q, modifiers, out var snapshot, modifiers.GauntletMutations), Is.True);
            var hit = snapshot.CopySteps().Single();
            Assert.That(snapshot.AdvanceDistance, Is.Zero); Assert.That(snapshot.AdvanceDuration, Is.Zero);
            Assert.That(hit.damageMultiplier, Is.EqualTo(q.HitSteps.Sum(s => s.damageMultiplier)).Within(.001f));
            Assert.That(hit.stanceDamage, Is.EqualTo(q.HitSteps.Sum(s => s.stanceDamage)).Within(.001f));
            Assert.That(hit.breakEffect, Is.EqualTo(StanceBreakEffect.KnockUp));
            Assert.That(snapshot.CopyProfile().ImpactEvents.Count, Is.EqualTo(1));
            Assert.That(snapshot.ManaCost, Is.EqualTo(q.ManaCost));
            Assert.That(JsonUtility.ToJson(q), Is.EqualTo(before));
        }

        [Test]
        public void FallRequiresUppercutAndIsAcquiredOnlyOnceWithExplicitTarget()
        {
            var weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon).Any(o => o.Id == BoonReworkCatalog.ViolentFallId), Is.False);
            Assert.That(modifiers.TryAcquireViolentFall(), Is.False);
            modifiers.GauntletMutations.Select(GauntletMutation.EclipseUppercut);
            Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon).Count(o => o.Id == BoonReworkCatalog.ViolentFallId), Is.EqualTo(1));
            Assert.That(modifiers.TryAcquireViolentFall(), Is.True); Assert.That(modifiers.TryAcquireViolentFall(), Is.False);
            Assert.That(BoonOfferCompatibility.TryBuild(modifiers, weapon, null, out var graph, out var error), Is.True, error);
            Assert.That(graph.Rank(BoonReworkCatalog.UppercutId), Is.EqualTo(1));
            Assert.That(graph.Rank(BoonReworkCatalog.ViolentFallId), Is.EqualTo(1));
            Assert.That(graph.Capabilities(BoonSlot.Q).HasFlag(BoonCapability.GapClose), Is.False);
            Assert.That(graph.TryRemove(BoonReworkCatalog.UppercutId, out _), Is.False);
            Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Technique, modifiers, weapon).Any(o => o.Id == BoonReworkCatalog.ViolentFallId), Is.False);
        }

        [Test]
        public void MarkHasAnInclusiveSingleConsumptionAndNeverArmsFromItselfOrADerivedHit()
        {
            var mark = new EclipseMarkState(3);
            mark.Advance(0); Assert.That(mark.Remaining, Is.EqualTo(3));
            Assert.That(mark.TryArm(false, false), Is.False);
            Assert.That(mark.TryArm(true, true), Is.False);
            Assert.That(mark.TryArm(true, false), Is.True);
            Assert.That(mark.TryArm(true, false), Is.False);
            mark.Advance(10); Assert.That(mark.IsArmed, Is.True, "An armed wave waits for a valid landing, not a mark timeout.");
            var expired = new EclipseMarkState(1); expired.Advance(1);
            Assert.That(expired.TryArm(true, false), Is.False);
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)] [TestCase(-1f)]
        public void RawTuningRejectsInvalidNumbersBeforeSnapshotConstruction(float value)
        {
            foreach (string field in new[] { "_duration", "_reach", "_width", "_launchHeight", "_launchSeconds", "_markSeconds", "_fallRadius", "_fallDamageFraction", "_fallPostureFraction" })
            {
                var tuning = new EclipseCombatTuning();
                typeof(EclipseCombatTuning).GetField(field, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(tuning, value);
                Assert.That(tuning.TrySnapshot(out _, out _), Is.False, field);
            }
        }

        [Test]
        public void FallPreservesEchoDamageAndPostureWithoutReplacingTheArmedLaunch()
        {
            var weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            var modifiers = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            modifiers.GauntletMutations.Select(GauntletMutation.CadencedAsura);
            modifiers.GauntletMutations.Select(GauntletMutation.EclipseUppercut);
            WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.AsuraEcho), out var echo);
            modifiers.Add(echo);
            Assert.That(GauntletExecutionSnapshot.TryBuild((BreakerGauntletAbility)weapon.abilities[3], GauntletActionSlot.R,
                modifiers, out var before, modifiers.GauntletMutations), Is.True);
            Assert.That(modifiers.TryAcquireViolentFall(), Is.True);
            Assert.That(GauntletExecutionSnapshot.TryBuild((BreakerGauntletAbility)weapon.abilities[3], GauntletActionSlot.R,
                modifiers, out var after, modifiers.GauntletMutations), Is.True);
            var oldEcho = before.CopySteps().Last(); var compatibleEcho = after.CopySteps().Last();
            Assert.That(compatibleEcho.damageMultiplier, Is.EqualTo(oldEcho.damageMultiplier));
            Assert.That(compatibleEcho.stanceDamage, Is.EqualTo(oldEcho.stanceDamage));
            Assert.That(compatibleEcho.breakEffect, Is.EqualTo(StanceBreakEffect.Stun));
            Assert.That(oldEcho.breakEffect, Is.EqualTo(StanceBreakEffect.None), "Other builds retain their existing reaction contract.");
            Assert.That(EclipseBoonCatalog.ViolentFall().ImpactRules.MaxInstancesPerCast, Is.EqualTo(32));
        }

        [Test]
        public void TuningAndDefinitionPersistAfterSaveUnloadAndReload()
        {
            string path = "Assets/_Project/ScriptableObjects/Run/EclipseTuningTest_" + Guid.NewGuid().ToString("N") + ".asset";
            var authored = ScriptableObject.CreateInstance<GauntletAuthoring>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"_duration\":0.8,\"_markSeconds\":4.0}", authored.Eclipse);
                Assert.That(authored.Validate(out var error), Is.True, error);
                AssetDatabase.CreateAsset(authored, path); AssetDatabase.SaveAssetIfDirty(authored);
                string guid = AssetDatabase.AssetPathToGUID(path);
                Resources.UnloadAsset(authored); AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                authored = AssetDatabase.LoadAssetAtPath<GauntletAuthoring>(path);
                Assert.That(authored.Eclipse.TrySnapshot(out var settings, out error), Is.True, error);
                Assert.That(settings.Duration, Is.EqualTo(.8f)); Assert.That(settings.MarkSeconds, Is.EqualTo(4));
                Assert.That(AssetDatabase.AssetPathToGUID(path), Is.EqualTo(guid));
                JsonUtility.FromJsonOverwrite("{\"_duration\":1.0}", authored.Eclipse);
                Assert.That(settings.Duration, Is.EqualTo(.8f), "Existing snapshots must remain detached from authoring.");
            }
            finally { AssetDatabase.DeleteAsset(path); }
        }
    }
}
