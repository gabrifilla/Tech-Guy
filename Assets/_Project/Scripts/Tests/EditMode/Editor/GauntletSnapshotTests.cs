using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class GauntletSnapshotTests
    {
        [Serializable] private struct Overlay { public AreaHitStep[] _hitSteps; }
        private BreakerGauntletAbility _ability;
        [SetUp] public void SetUp()
        {
            _ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Overlay { _hitSteps = new[] {
                new AreaHitStep { delay = .2f, damageMultiplier = 1f },
                new AreaHitStep { delay = .4f, damageMultiplier = 2f } } }), _ability);
        }
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(_ability);

        [Test]
        public void AuthoredTimersAndProfilesAreConsumedAndFrozenInSnapshots()
        {
            var authoring = ScriptableObject.CreateInstance<GauntletAuthoring>();
            var source = ScriptableObject.CreateInstance<Ability>();
            try
            {
                var profile = CombatActionProfile.CadencedStrike(0);
                JsonUtility.FromJsonOverwrite("{\"startupEnd\":0.25,\"activeEnd\":0.6,\"impactEvents\":[{\"index\":0,\"at\":0.25,\"areaHitStepIndex\":0,\"hitStopProfileIndex\":-1}]}", profile);
                typeof(Ability).GetField("_combatProfile", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(source, profile);
                var r = authoring.FindContent(GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura).Id);
                JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":0.8,\"_overrideInterval\":true,\"_interval\":0.4,\"_overrideProfile\":true}", r);
                typeof(GauntletContentAuthoring).GetField("_profileSource", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(r, source);
                var w = authoring.FindContent(GauntletContentAuthoring.ForMutation(GauntletMutation.PressureField).Id);
                JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":1,\"_overrideInterval\":true,\"_interval\":0.25}", w);
                var echo = authoring.FindContent("weapon_FlurryEcho");
                JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":0.05,\"_overrideInterval\":true,\"_interval\":0.07}", echo);
                var hungry = authoring.FindContent("weapon_HungryCombo");
                JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":0}", hungry);
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                mods.Add(Definition(WeaponBoon.FlurryEcho)); mods.GauntletMutations.Select(GauntletMutation.PressureField);
                mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
                Assert.IsTrue(authoring.Validate(out var error), error);
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.R, mods, out var rs, mods.GauntletMutations, authoring));
                Assert.AreEqual(.8f, rs.ActiveTime); Assert.AreEqual(.4f, rs.CadencedWaitDuration);
                Assert.AreEqual(.2f, rs.CadencedImpactTime, .00001f); Assert.AreEqual(.48f, rs.CadencedRecoveryStart, .00001f);
                Assert.AreEqual(.25f, rs.CopyProfile().ImpactEvents[0].At);
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.W, mods, out var ws, mods.GauntletMutations, authoring));
                Assert.AreEqual(.25f, ws.PressurePulseInterval);
                var steps = ws.CopySteps(); float last = steps[ws.LastDirectProducer].delay;
                Assert.AreEqual(last + .05f, steps[ws.DirectStepCount].delay, .00001f);
                Assert.AreEqual(last + .12f, steps[ws.DirectStepCount + 1].delay, .00001f);
                Assert.AreEqual(0f, GauntletExecutionSnapshot.CaptureBasic(mods, authoring).HungrySecondsPerRank);
                JsonUtility.FromJsonOverwrite("{\"_duration\":0}", r);
                Assert.IsFalse(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.R, mods, out _, mods.GauntletMutations, authoring));
                Assert.AreEqual(.8f, rs.ActiveTime, "accepted snapshots ignore later authoring changes");
            }
            finally { UnityEngine.Object.DestroyImmediate(authoring); UnityEngine.Object.DestroyImmediate(source); }
        }

        [Test]
        public void AuthoringRejectsOverridesWithoutSemanticsAndContradictoryPulseTimes()
        {
            var q = GauntletContentAuthoring.ForMutation(GauntletMutation.DemolishingAdvance);
            JsonUtility.FromJsonOverwrite("{\"_overrideInterval\":true,\"_interval\":0}", q);
            Assert.IsFalse(q.Validate(out var error)); StringAssert.Contains("no defined effect", error);
            var reserve = GauntletContentAuthoring.ForTechnique(WeaponBoon.AsuraReserve);
            JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":0}", reserve);
            Assert.IsFalse(reserve.Validate(out error)); StringAssert.Contains("no defined effect", error);
            var w = GauntletContentAuthoring.ForMutation(GauntletMutation.PressureField);
            JsonUtility.FromJsonOverwrite("{\"_overrideDuration\":true,\"_duration\":2,\"_overrideInterval\":true,\"_interval\":0.1}", w);
            Assert.IsFalse(w.Validate(out error)); StringAssert.Contains("four pulse intervals", error);
        }

        [Test]
        public void LegacyEpicenterRankPreservesRadiusAndSnapshotsAcrossImportOrder()
        {
            var forward = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            var reverse = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            foreach (int rank in new[] { 1, 3 })
                Assert.IsTrue(forward.ImportLegacyGauntletRank(new WeaponRunModifiers.LegacyRankRecord("weapon_ShockRing", rank)));
            foreach (int rank in new[] { 3, 1 })
                Assert.IsTrue(reverse.ImportLegacyGauntletRank(new WeaponRunModifiers.LegacyRankRecord("weapon_ShockRing", rank)));
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.E, forward, out var a, forward.GauntletMutations));
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.E, reverse, out var b, reverse.GauntletMutations));
            for (int i = 0; i < a.CopySteps().Count; i++)
            {
                var source = _ability.HitSteps[i];
                Assert.AreEqual(Mathf.Max(source.sphereRadius, source.boxSize.x * .5f) * 1.6f, a.CopySteps()[i].sphereRadius, .0001f);
                Assert.AreEqual(a.CopySteps()[i].sphereRadius, b.CopySteps()[i].sphereRadius);
                Assert.AreEqual(source.damageMultiplier, a.CopySteps()[i].damageMultiplier);
            }
        }
        private static WeaponRunModifiers.Definition Definition(WeaponBoon kind)
        {
            foreach (var entry in WeaponRunModifiers.Catalog) if (entry.Kind == kind) return entry;
            throw new ArgumentException();
        }

        // gauntlet-2-rework P9/P10: final ranks, not acquisition order, define the action.
        [Test] public void RanksAndStepsAreFrozenAcrossAcquisitionAndReturnedCopyChanges()
        {
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            mods.Add(Definition(WeaponBoon.FlurryEcho));
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.W, mods, out var snapshot));
            mods.Add(Definition(WeaponBoon.FlurryEcho));
            var steps = snapshot.CopySteps(); steps[1].damageMultiplier = 500f; steps.Clear();
            _ability.HitSteps[1].damageMultiplier = 300f;
            Assert.AreEqual(1, snapshot.Rank(WeaponBoon.FlurryEcho));
            Assert.AreEqual(4, snapshot.CopySteps().Count);
            Assert.AreEqual(2f, snapshot.CopySteps()[1].damageMultiplier);
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.W, mods, out var next));
            Assert.AreEqual(6, next.CopySteps().Count);
            Assert.AreEqual(300f, next.CopySteps()[1].damageMultiplier);
        }

        [Test] public void AcquisitionPermutationsProduceIdenticalSnapshotsForAllRanks()
        {
            var kinds = new[] { WeaponBoon.FlurryEcho, WeaponBoon.SeismicFist, WeaponBoon.MomentumStrike };
            for (int rank = 0; rank <= 3; rank++)
            {
                var first = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                var second = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                for (int i = 0; i < kinds.Length; i++) for (int r = 0; r < rank; r++) {
                    first.Add(Definition(kinds[i])); second.Add(Definition(kinds[kinds.Length - 1 - i])); }
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.W, first, out var a));
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.W, second, out var b));
                var x = a.CopySteps(); var y = b.CopySteps(); Assert.AreEqual(x.Count, y.Count);
                for (int i = 0; i < x.Count; i++) Assert.AreEqual(JsonUtility.ToJson(x[i]), JsonUtility.ToJson(y[i]));
            }
        }

        [Test] public void LocalProducerIsLastDirectStepAndNeverADescendant()
        {
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.Add(Definition(WeaponBoon.FlurryEcho));
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.W, mods, out var snapshot));
            Assert.AreEqual(1, snapshot.LastDirectProducer);
            for (int i = 0; i < snapshot.CopySteps().Count; i++) {
                Assert.AreEqual(i == 1, snapshot.CanProduceLocalChild(GauntletImpactOrigin.Direct, i));
                Assert.IsFalse(snapshot.CanProduceLocalChild(GauntletImpactOrigin.Echo, i));
                Assert.IsFalse(snapshot.CanProduceLocalChild(GauntletImpactOrigin.Persistent, i));
            }
        }

        [Test] public void InvalidStepFailsBeforeAnActionSnapshotExists()
        {
            _ability.HitSteps[0].delay = float.NaN;
            Assert.IsFalse(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.Q, null, out var snapshot));
            Assert.IsNull(snapshot);
        }

        [Test] public void CadencedAsuraPreservesTotalsAndCostsInThreeFrozenSteps()
        {
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            var ability = (BreakerGauntletAbility)weapon.abilities[3]; string before = JsonUtility.ToJson(ability);
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(GauntletMutation.CadencedAsura);
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(ability, GauntletActionSlot.R, mods, out var snapshot, mods.GauntletMutations));
            Assert.AreEqual(3, snapshot.DirectStepCount); Assert.AreEqual(2, snapshot.LastDirectProducer);
            float damage = 0f, bonus = 0f, stance = 0f;
            foreach (var step in ability.HitSteps) { damage += step.damageMultiplier; bonus += step.bonusDamage; stance += step.stanceDamage; }
            var steps = snapshot.CopySteps();
            for (int i = 0; i < 3; i++)
            {
                float fraction = i == 2 ? .5f : .25f;
                Assert.AreEqual(damage * fraction, steps[i].damageMultiplier, .001f);
                Assert.AreEqual(bonus * fraction, steps[i].bonusDamage, .001f);
                Assert.AreEqual(stance * fraction, steps[i].stanceDamage, .001f);
            }
            Assert.AreEqual(.4f, snapshot.ActiveTime); Assert.AreEqual(ability.ManaCost, snapshot.ManaCost);
            var profile = snapshot.CopyProfile(); Assert.AreEqual(.3f, profile.StartupEnd); Assert.AreEqual(.55f, profile.ActiveEnd);
            Assert.AreEqual(CommitmentCategory.Committed, profile.Commitment);
            Assert.AreEqual(.775f, profile.CancelRules[0].Start); Assert.AreEqual(before, JsonUtility.ToJson(ability));
        }

        [Test] public void MergedMutationsPreserveHpWithDifferentMultipliersAndAuthoredBonuses()
        {
            var source = new[] {
                new AreaHitStep { damageMultiplier = .5f, bonusDamage = 10f, stanceDamage = 3f },
                new AreaHitStep { damageMultiplier = 2f, bonusDamage = 4f, stanceDamage = 7f },
                new AreaHitStep { damageMultiplier = 1f, bonusDamage = -5f, stanceDamage = 2f } };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(new Overlay { _hitSteps = source }), _ability);
            var stats = new PlayerArpgStats { baseDamage = 5f, flatDamageBonus = 7f, criticalChance = 0f,
                damageMultiplier = 2f, increasedDamagePercent = 10f };
            float hp = 0f; foreach (var step in source) hp += stats.RollAttackDamage(10f, step.damageMultiplier, step.bonusDamage).Amount;
            foreach (var mutation in new[] { GauntletMutation.DemolishingAdvance, GauntletMutation.CadencedAsura })
            {
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.GauntletMutations.Select(mutation);
                var slot = mutation == GauntletMutation.DemolishingAdvance ? GauntletActionSlot.Q : GauntletActionSlot.R;
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, slot, mods, out var snapshot, mods.GauntletMutations));
                float total = 0f; int i = 0;
                foreach (var step in snapshot.CopySteps())
                {
                    float amount = stats.RollAttackDamage(10f, step.damageMultiplier, step.bonusDamage).Amount;
                    Assert.AreEqual(hp * (slot == GauntletActionSlot.Q ? 1f : i == 2 ? .5f : .25f), amount, .001f);
                    total += amount; i++;
                }
                Assert.AreEqual(hp, total, .001f);
            }
        }

        [Test] public void EchoHpFractionsPreserveAuthoredBonusBeforeMultiplication()
        {
            _ability.HitSteps[1].bonusDamage = 11f;
            var stats = new PlayerArpgStats { baseDamage = 5f, criticalChance = 0f, damageMultiplier = 1f };
            foreach (var slot in new[] { GauntletActionSlot.W, GauntletActionSlot.R })
            {
                var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
                mods.Add(Definition(slot == GauntletActionSlot.W ? WeaponBoon.FlurryEcho : WeaponBoon.AsuraEcho));
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, slot, mods, out var snapshot));
                var steps = snapshot.CopySteps(); var direct = steps[snapshot.LastDirectProducer];
                float hp = stats.RollAttackDamage(10f, direct.damageMultiplier, direct.bonusDamage).Amount;
                for (int i = snapshot.DirectStepCount; i < steps.Count; i++)
                    Assert.AreEqual(hp * (slot == GauntletActionSlot.W ? .55f : .65f),
                        stats.RollAttackDamage(10f, steps[i].damageMultiplier, steps[i].bonusDamage).Amount, .001f);
            }
        }

        [Test] public void ProfileCopiesCannotChangeAcceptedWindowsOrHitStop()
        {
            var profile = new CombatActionProfile();
            JsonUtility.FromJsonOverwrite("{\"startupEnd\":0.2,\"activeEnd\":0.7,\"hitStopProfiles\":[{\"duration\":0.04}]}", profile);
            typeof(Ability).GetField("_combatProfile", System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(_ability, profile);
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.Q, null, out var snapshot));
            JsonUtility.FromJsonOverwrite("{\"startupEnd\":0.5}", profile);
            var copy = snapshot.CopyProfile();
            JsonUtility.FromJsonOverwrite("{\"activeEnd\":0.9,\"hitStopProfiles\":[]}", copy);
            Assert.AreEqual(.2f, snapshot.CopyProfile().StartupEnd);
            Assert.AreEqual(.7f, snapshot.CopyProfile().ActiveEnd);
            Assert.AreEqual(.04f, snapshot.CopyProfile().HitStopProfiles[0].Duration);
        }

        [Test] public void SelectionIsSlotExclusiveAndLegacyEpicenterAliasHasTheSameGeometry()
        {
            var selection = new GauntletMutationSelection();
            Assert.IsTrue(selection.Select(GauntletMutation.Epicenter));
            Assert.IsFalse(selection.Select(GauntletMutation.Epicenter));
            Assert.IsFalse(selection.Select((GauntletMutation)999));
            Assert.IsFalse(selection.TryGet(GauntletActionSlot.Q, out _));
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.E, null, out var current, selection));
            var legacy = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); legacy.Add(Definition(WeaponBoon.ShockRing));
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.E, legacy, out var alias));
            Assert.AreEqual(current.Mutation, alias.Mutation);
            var x = current.CopySteps(); var y = alias.CopySteps();
            for (int i = 0; i < x.Count; i++) Assert.AreEqual(JsonUtility.ToJson(x[i]), JsonUtility.ToJson(y[i]));
            selection.Select(GauntletMutation.PressureField);
            Assert.AreEqual(GauntletMutation.Epicenter, current.Mutation);
        }

        [Test] public void MutationChoosesDirectProducerBeforeEchoExpansion()
        {
            var selection = new GauntletMutationSelection(); selection.Select(GauntletMutation.PressureField);
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.Add(Definition(WeaponBoon.FlurryEcho));
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.W, mods, out var snapshot, selection));
            Assert.AreEqual(2, snapshot.DirectStepCount); Assert.AreEqual(4, snapshot.CopySteps().Count);
            Assert.AreEqual(1, snapshot.LastDirectProducer);
            Assert.AreEqual(1.1f, snapshot.CopySteps()[2].damageMultiplier, .0001f);
        }

        [Test] public void LimitsRunAfterCompositionAndRejectNumericOverflow()
        {
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            for (int i = 0; i < 3; i++) mods.Add(Definition(WeaponBoon.RocketAdvance));
            JsonUtility.FromJsonOverwrite("{\"_advanceDistance\":4.0}", _ability);
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.Q, mods, out var snapshot));
            Assert.AreEqual(8f, snapshot.AdvanceDistance);
            var selection = new GauntletMutationSelection(); selection.Select(GauntletMutation.DemolishingAdvance);
            _ability.HitSteps[0].stanceDamage = float.MaxValue;
            _ability.HitSteps[1].stanceDamage = float.MaxValue;
            Assert.IsFalse(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.Q, mods, out snapshot, selection));
            Assert.IsNull(snapshot);
        }

        [Test] public void ExistingAsuraFinisherRemainsUsableButOtherUnknownReactionsAreRejected()
        {
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            var r = (BreakerGauntletAbility)weapon.abilities[3];
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(r, GauntletActionSlot.R, null, out var snapshot));
            Assert.AreEqual((int)r.HitSteps[r.HitSteps.Count - 1].reactionType,
                (int)snapshot.CopySteps()[snapshot.LastDirectProducer].reactionType);
            _ability.HitSteps[0].reactionType = (HitReactionType)999;
            Assert.IsFalse(GauntletExecutionSnapshot.TryBuild(_ability, GauntletActionSlot.Q, null, out _));
        }

        [Test] public void DemolishingAdvanceKeepsPhasesAndRemapsOnlyTheFinalAuthoredImpact()
        {
            var weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
            var q = (BreakerGauntletAbility)weapon.abilities[0];
            string original = JsonUtility.ToJson(q);
            var selections = new GauntletMutationSelection(); selections.Select(GauntletMutation.DemolishingAdvance);
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet); mods.Add(Definition(WeaponBoon.SeismicFist));
            Assert.IsTrue(GauntletExecutionSnapshot.TryBuild(q, GauntletActionSlot.Q, mods, out var snapshot, selections));
            var steps = snapshot.CopySteps(); Assert.AreEqual(1, steps.Count);
            float hp = 0f, posture = 0f, bonus = 0f;
            foreach (var step in q.HitSteps) { hp += step.damageMultiplier; posture += step.stanceDamage; bonus += step.bonusDamage; }
            Assert.AreEqual(hp, steps[0].damageMultiplier); Assert.AreEqual(posture, steps[0].stanceDamage);
            Assert.AreEqual(bonus, steps[0].bonusDamage); Assert.AreEqual(StanceBreakEffect.KnockUp, steps[0].breakEffect);
            var profile = snapshot.CopyProfile(); Assert.AreEqual(1, profile.ImpactEvents.Count);
            Assert.AreEqual(0, profile.ImpactEvents[0].AreaHitStepIndex);
            Assert.AreEqual(q.CombatProfile.ImpactEvents[q.CombatProfile.ImpactEvents.Count - 1].At, profile.ImpactEvents[0].At);
            Assert.AreEqual(q.CombatProfile.StartupEnd, profile.StartupEnd); Assert.AreEqual(q.CombatProfile.ActiveEnd, profile.ActiveEnd);
            Assert.AreEqual(q.ManaCost, snapshot.ManaCost); Assert.AreEqual(q.activeTime, snapshot.ActiveTime);
            Assert.AreEqual(original, JsonUtility.ToJson(q));
        }
    }
}
