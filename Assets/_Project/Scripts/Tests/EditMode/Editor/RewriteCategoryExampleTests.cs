using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for R9 of modifier-synergies-theme17 (Rewrite reward category), task 13.4.
    ///
    /// Covers, as concrete examples that complement the Property 23 property test:
    ///   - Classification + presentation (R9.1/R9.2): the <c>transform</c> id is classified Rewrite and
    ///     presented with the "TRANSFORMAÇÃO" label/accent and <see cref="RunModifierPresentation.IsRewrite"/>
    ///     set; ordinary ids are not Rewrite.
    ///   - No-target exclusion (R9.5): with no valid slot-0 ability, the Rewrite is never offered even when
    ///     the appearance roll would otherwise admit it.
    ///   - Runtime-copy grant (R9.4/R9.2): the grant creates a NEW <see cref="ArsenalAbility"/> instance and
    ///     leaves the source ability asset unchanged (this exercises the exact grant operation the
    ///     <c>transform</c> case in <see cref="RunBoons.Choose"/> performs).
    ///   - Post-take single-use exclusion (R9.6): once <c>transform</c> is acquired, it is excluded from all
    ///     subsequent offer sets.
    ///
    /// Test seam: identical to <see cref="RewriteOfferConstraintTests"/> — RunBoons is built on an INACTIVE
    /// GameObject so no lifecycle runs, and the fields <c>OfferReward</c> reads (<c>_weapon</c>, <c>_player</c>,
    /// <c>WeaponModifiers</c>, <c>_acquired</c>) are set via reflection. The Rewrite roll is forced through the
    /// private <c>_rewriteRoll</c> seam field (set by reflection).
    /// </summary>
    public sealed class RewriteCategoryExampleTests
    {
        private const string TransformId = "transform";

        private GameObject _host;
        private readonly List<Object> _created = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            _host = null;
            foreach (Object obj in _created) if (obj) Object.DestroyImmediate(obj);
            _created.Clear();
        }

        // ---- R9.1 / R9.2: classification and presentation ----

        // Feature: modifier-synergies-theme17, R9.1/R9.2 example — transform classified + labelled Rewrite.
        // Validates: Requirements 9.1, 9.2
        [Test]
        public void Transform_IsClassifiedAndPresentedAsRewrite()
        {
            Assert.IsTrue(RunModifierPresentation.IsRewriteId(TransformId),
                "transform must be classified as a Rewrite id (R9.1).");

            var offer = new RunBoons.Offer(TransformId, "Nova de impacto", "TRANSFORMA Q: explosão circular.");
            RunModifierPresentation view = RunModifierPresentation.For(offer);

            Assert.IsTrue(view.IsRewrite, "Rewrite presentation flag must be set for transform (R9.2).");
            Assert.AreEqual("TRANSFORMAÇÃO", view.Category, "Rewrite reuses the TRANSFORMAÇÃO label (R9.2).");
            // The distinct accent visual reused for the Rewrite category (purple), grounded in For().
            Assert.AreEqual(new Color(.77f, .58f, 1f), view.Accent, "Rewrite reuses the TRANSFORMAÇÃO accent (R9.2).");
        }

        // Feature: modifier-synergies-theme17, R9.1 example — ordinary ids are not Rewrite.
        // Validates: Requirements 9.1
        [Test]
        public void NonAbilityReplacingIds_AreNotRewrite()
        {
            foreach (string id in new[] { "focus", "power", "haste", "ignite", "frost", "weapon_Orbit" })
            {
                Assert.IsFalse(RunModifierPresentation.IsRewriteId(id), id + " must not be classified as Rewrite.");
                var view = RunModifierPresentation.For(new RunBoons.Offer(id, id, id));
                Assert.IsFalse(view.IsRewrite, id + " presentation must not set the Rewrite flag.");
            }
        }

        // ---- R9.5: no-target exclusion ----

        // Feature: modifier-synergies-theme17, R9.5 example — no valid target ability => Rewrite not offered.
        // Even with the appearance roll forced to admit a Rewrite (roll = 0), the absence of a slot-0
        // ability must keep it out of the offer set.
        // Validates: Requirements 9.5
        [Test]
        public void Rewrite_NotOffered_WhenNoTargetAbility()
        {
            WeaponScript weapon = NewWeapon(firesArrows: false, withSlot0Ability: false);
            RunBoons run = BuildRun(weapon);
            SetRewriteRoll(run, () => 0.0); // roll would admit a Rewrite if a target existed

            run.OfferReward(0);

            Assert.IsFalse(OfferHasRewrite(run),
                "No Rewrite may be offered when no valid target ability exists (R9.5).");
        }

        // Feature: modifier-synergies-theme17, R9.5 sanity — with a valid target and an admitting roll,
        // the Rewrite IS offered (confirms the exclusion above is due to the missing target, not the roll).
        // Validates: Requirements 9.5
        [Test]
        public void Rewrite_Offered_WhenTargetPresentAndRollAdmits()
        {
            WeaponScript weapon = NewWeapon(firesArrows: false, withSlot0Ability: true);
            RunBoons run = BuildRun(weapon);
            SetRewriteRoll(run, () => 0.0);

            run.OfferReward(0);

            Assert.IsTrue(OfferHasRewrite(run),
                "A Rewrite must be offered when a valid target exists and the roll admits it (R9.5).");
        }

        // ---- R9.6: post-take single-use exclusion ----

        // Feature: modifier-synergies-theme17, R9.6 example — after transform is acquired it is excluded
        // from every subsequent offer set, even with the roll forced to admit a Rewrite.
        // Validates: Requirements 9.6
        [Test]
        public void Rewrite_ExcludedFromSubsequentOffers_AfterTaken()
        {
            WeaponScript weapon = NewWeapon(firesArrows: false, withSlot0Ability: true);
            RunBoons run = BuildRun(weapon);
            SetRewriteRoll(run, () => 0.0); // always admit, to prove exclusion is single-use, not luck

            // Simulate having already taken transform this run.
            AppendAcquired(run, TransformId);

            for (int i = 0; i < 5; i++)
            {
                SetProperty(run, "IsChoosing", false);
                run.OfferReward(0);
                Assert.IsFalse(OfferHasRewrite(run),
                    "A Rewrite already taken this run must be excluded from subsequent offers (R9.6).");
            }
        }

        // ---- R9.4 / R9.2: runtime-copy grant ----

        // Feature: modifier-synergies-theme17, R9.4/R9.2 example — granting a Rewrite creates a NEW
        // ArsenalAbility runtime copy and leaves the source ability asset unchanged.
        //
        // This exercises the exact grant operation RunBoons.Choose performs for the "transform" case:
        //   var skill = ScriptableObject.CreateInstance<ArsenalAbility>();
        //   skill.ConfigureRunTransformation(_weapon.FiresArrows);
        //   _weapon.abilities[0] = skill;   // slot-0 now points at the copy, source untouched
        // We assert the produced instance is distinct from the source and that the source's serialized
        // getters are unchanged, mirroring Choose without its fragile MonoBehaviour lifecycle
        // (Choose calls _holder.RefreshLoadout()/RewardChosen which need full scene/player state).
        // Validates: Requirements 9.4, 9.2
        [Test]
        public void RewriteGrant_CreatesRuntimeCopy_AndLeavesSourceUnchanged()
        {
            var source = ScriptableObject.CreateInstance<ArsenalAbility>();
            source.name = "SourceQ";
            _created.Add(source);

            // Snapshot the source's serialized getters before the grant.
            ArsenalSkillKind kind0 = source.Kind;
            float windup0 = source.Windup, interval0 = source.Interval, range0 = source.Range, width0 = source.Width;
            float damage0 = source.DamageMultiplier;
            int hits0 = source.Hits;
            bool piercing0 = source.Piercing;

            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.name = "GrantWeapon";
            weapon.abilities = new Ability[] { source };
            _created.Add(weapon);

            // The grant operation (identical to Choose's transform branch).
            var skill = ScriptableObject.CreateInstance<ArsenalAbility>();
            skill.ConfigureRunTransformation(bow: false);
            weapon.abilities[0] = skill;
            _created.Add(skill);

            Assert.AreNotSame(source, weapon.abilities[0], "Grant must install a NEW ability instance (R9.4).");
            Assert.AreSame(skill, weapon.abilities[0], "Slot 0 must point at the runtime copy after the grant.");

            // Source asset untouched (R9.2/R9.4/R11.1): no getter changed.
            Assert.AreEqual(kind0, source.Kind, "Source Kind mutated by the grant.");
            Assert.AreEqual(windup0, source.Windup, "Source Windup mutated by the grant.");
            Assert.AreEqual(interval0, source.Interval, "Source Interval mutated by the grant.");
            Assert.AreEqual(range0, source.Range, "Source Range mutated by the grant.");
            Assert.AreEqual(width0, source.Width, "Source Width mutated by the grant.");
            Assert.AreEqual(damage0, source.DamageMultiplier, "Source DamageMultiplier mutated by the grant.");
            Assert.AreEqual(hits0, source.Hits, "Source Hits mutated by the grant.");
            Assert.AreEqual(piercing0, source.Piercing, "Source Piercing mutated by the grant.");

            // The copy was actually reconfigured into the transform (Nova de impacto for a non-bow).
            Assert.AreEqual(ArsenalSkillKind.Sweep, skill.Kind, "Non-bow transform copy should become a Sweep.");
        }

        // ---- helpers (shared with the property test's seam pattern) ----

        private WeaponScript NewWeapon(bool firesArrows, bool withSlot0Ability)
        {
            var weapon = ScriptableObject.CreateInstance<WeaponScript>();
            weapon.name = "TestRunWeapon";
            SetPrivate(weapon, "_firesArrows", firesArrows);
            if (withSlot0Ability)
            {
                var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
                ability.name = "TestQ";
                weapon.abilities = new Ability[] { ability };
                _created.Add(ability);
            }
            else
            {
                weapon.abilities = new Ability[0]; // no slot-0 target -> HasRewriteTarget() == false
            }
            _created.Add(weapon);
            return weapon;
        }

        private RunBoons BuildRun(WeaponScript weapon)
        {
            _host = new GameObject("RunBoonsRewriteExampleHost");
            _host.SetActive(false);
            PlayerActor player = _host.AddComponent<PlayerActor>(); // fresh -> IsDead == false
            RunBoons run = _host.AddComponent<RunBoons>();
            SetPrivate(run, "_player", player);
            SetPrivate(run, "_weapon", weapon);
            SetProperty(run, "WeaponModifiers", new WeaponRunModifiers(WeaponRunModifiers.Identify(weapon)));
            return run;
        }

        private static bool OfferHasRewrite(RunBoons run)
        {
            foreach (RunBoons.Offer offer in run.Choices)
                if (RunModifierPresentation.IsRewriteId(offer.Id)) return true;
            return false;
        }

        private static void AppendAcquired(RunBoons run, string id)
        {
            FieldInfo field = typeof(RunBoons).GetField("_acquired", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "Expected private field _acquired on RunBoons (test seam).");
            var list = (List<RunBoons.Offer>)field.GetValue(run);
            list.Add(new RunBoons.Offer(id, id, id));
        }

        // Assigns the private _rewriteRoll seam field so the Rewrite appearance gate is deterministic.
        private static void SetRewriteRoll(RunBoons run, System.Func<double> roll)
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
