using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode guard tests for the run-modifier immutability contract of
    /// weapon-gameplay-swarm-rework (Requisito 12.1/12.5/12.6, task 15.1).
    ///
    /// The suite-wide <see cref="AssetIsolationTests"/> already asserts that driving
    /// <see cref="WeaponRunModifiers.Plan"/> / <see cref="WeaponRunModifiers.GauntletSteps"/> leaves
    /// every source ability asset byte-for-byte unchanged, but it does so with <em>empty</em> source
    /// abilities — <see cref="BreakerGauntletAbility.HitSteps"/> has no authored step, so the clone
    /// loop inside <c>GauntletSteps</c> is never actually exercised. These tests populate the source
    /// with real <see cref="AreaHitStep"/> data and pin down the two structural invariants the
    /// contract relies on:
    /// <list type="bullet">
    ///   <item>Every <see cref="AreaHitStep"/> returned by <c>GauntletSteps</c> is a fresh instance,
    ///   reference-distinct from the source's authored steps, so a later mutation cannot alias the
    ///   asset (Requisito 12.1/12.5).</item>
    ///   <item>Running the new loop / reaction / displacement rule that mutates those steps
    ///   (<see cref="GauntletLoopSteps.Configure(IReadOnlyList{AreaHitStep}, int)"/>) leaves the
    ///   source ability byte-for-byte unchanged — the rules operate only on the per-cast snapshot,
    ///   never on a parallel catalog or the source asset (Requisito 12.6).</item>
    /// </list>
    ///
    /// <see cref="BreakerGauntletAbility.HitSteps"/> is a private serialized field, so the source
    /// steps are injected via a <see cref="JsonUtility.FromJsonOverwrite(string, object)"/> overlay,
    /// mirroring the technique already used by <see cref="AssetIsolationTests"/> for the arsenal
    /// <c>_kind</c> field. Pure ScriptableObject plan generation, so this runs in EditMode.
    /// </summary>
    public sealed class GauntletStepsCloneIsolationTests
    {
        [Serializable]
        private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        // Slot 2 (E — Stance Breaker) is deliberately included because GauntletLoopSteps.Configure only
        // mutates that stage; driving it proves the loop rule stays on the clone even when it writes.
        private static readonly int[] Slots = { 0, 1, 2, 3 };

        private static AreaHitStep NewAuthoredStep(int index)
        {
            return new AreaHitStep
            {
                delay = 0.1f * index,
                rangeOverride = 1.5f + index,
                hitShape = AreaHitShape.Box,
                boxSize = new Vector3(3f + index, 2f, 0f),
                sphereRadius = 1.5f,
                damageMultiplier = 1f + 0.25f * index,
                bonusDamage = index,
                localOffset = new Vector3(0f, index, 0f),
                reactionType = HitReactionType.Stagger,
                hitStrength = HitStrength.Light,   // deliberately weak so slot 2 forces a change on the clone
                pushDistance = 0.35f,
                stanceDamage = 12f + index,
                breakEffect = StanceBreakEffect.None,
                stunDuration = 1f,
                knockUpHeight = 2f,
                knockbackDistance = 4f,
            };
        }

        private static BreakerGauntletAbility NewGauntletWithSteps(int stepCount)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "SourceGauntletWithSteps";
            var authored = new AreaHitStep[stepCount];
            for (int i = 0; i < stepCount; i++) authored[i] = NewAuthoredStep(i);
            // _hitSteps is the private serialized backing field of the HitSteps property.
            var overlay = new HitStepsOverlay { _hitSteps = authored };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        private static WeaponRunModifiers MaxRankGauntlet()
        {
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            foreach (var definition in WeaponRunModifiers.Catalog)
            {
                if (definition.Family != RunWeaponFamily.Gauntlet) continue;
                for (int r = 0; r < definition.MaxRank; r++) mods.Add(definition);
            }
            return mods;
        }

        // Feature: weapon-gameplay-swarm-rework, task 15.1
        // GauntletSteps returns AreaHitStep instances that are reference-distinct from the source
        // ability's authored steps, across every slot and at max ranks, so no returned step aliases
        // the asset. Validates: Requirements 12.1, 12.5
        [Test]
        public void GauntletSteps_ReturnsClonesDistinctFromSource()
        {
            // Sanity: the overlay actually populated the private HitSteps (guards the whole premise).
            using (var probe = new DisposableAbility(NewGauntletWithSteps(3)))
                Assert.That(probe.Ability.HitSteps.Count, Is.EqualTo(3),
                    "Test setup failed to inject authored HitSteps into the source ability");

            WeaponRunModifiers mods = MaxRankGauntlet();
            foreach (int slot in Slots)
            {
                using var source = new DisposableAbility(NewGauntletWithSteps(3));
                IReadOnlyList<AreaHitStep> authored = source.Ability.HitSteps;

                List<AreaHitStep> steps = mods.GauntletSteps(source.Ability, slot);

                foreach (AreaHitStep clone in steps)
                    foreach (AreaHitStep original in authored)
                        Assert.That(ReferenceEquals(clone, original), Is.False,
                            "GauntletSteps returned a reference into the source ability (slot " + slot + ")");
            }
        }

        // Feature: weapon-gameplay-swarm-rework, task 15.1
        // Driving GauntletSteps AND the new loop rule (GauntletLoopSteps.Configure, which mutates the
        // E-stage steps) leaves the source ability byte-for-byte unchanged, for every slot and at max
        // ranks: the reaction/displacement/grouping rules operate only on the per-cast snapshot.
        // Validates: Requirements 12.1, 12.5, 12.6
        [Test]
        public void GauntletStepsPlusLoopRule_LeavesSourceAssetUnchanged()
        {
            WeaponRunModifiers mods = MaxRankGauntlet();
            foreach (int slot in Slots)
            {
                using var source = new DisposableAbility(NewGauntletWithSteps(3));
                string before = JsonUtility.ToJson(source.Ability);

                // Build the per-cast snapshot and then run the loop rule that intentionally writes to it
                // (slot 2 raises hitStrength to Heavy and declares a KnockUp on the finisher).
                List<AreaHitStep> steps = mods.GauntletSteps(source.Ability, slot);
                GauntletLoopSteps.Configure(steps, slot);

                // The mutation must have landed on the clone, not the asset: the source stays identical.
                Assert.That(JsonUtility.ToJson(source.Ability), Is.EqualTo(before),
                    "Source gauntlet ability was mutated when planning slot " + slot);
            }
        }

        // Feature: weapon-gameplay-swarm-rework, task 15.1
        // Confirms the loop rule actually mutates the snapshot on slot 2, so the immutability assertion
        // above is meaningful (otherwise a no-op Configure would pass trivially).
        // Validates: Requirements 12.6
        [Test]
        public void LoopRule_MutatesSnapshotOnStanceBreakerSlot()
        {
            WeaponRunModifiers mods = MaxRankGauntlet();
            using var source = new DisposableAbility(NewGauntletWithSteps(3));

            List<AreaHitStep> steps = mods.GauntletSteps(source.Ability, 2);
            string snapshotBefore = JsonUtility.ToJson(new AreaHitStepList { items = steps });
            GauntletLoopSteps.Configure(steps, 2);
            string snapshotAfter = JsonUtility.ToJson(new AreaHitStepList { items = steps });

            Assert.That(snapshotAfter, Is.Not.EqualTo(snapshotBefore),
                "GauntletLoopSteps.Configure should transform the E-stage snapshot (weak Light hits -> Heavy)");
        }

        [Serializable]
        private struct AreaHitStepList { public List<AreaHitStep> items; }

        private readonly struct DisposableAbility : IDisposable
        {
            public BreakerGauntletAbility Ability { get; }
            public DisposableAbility(BreakerGauntletAbility ability) => Ability = ability;
            public void Dispose() { if (Ability) UnityEngine.Object.DestroyImmediate(Ability); }
        }
    }
}
