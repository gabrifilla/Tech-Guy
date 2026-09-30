using System;
using System.Collections.Generic;
using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for impactful-weapon-boons Property 11 (Shockwave append).
    ///
    /// <para>
    /// For any Shockwave rank <c>r &gt;= 1</c>, <see cref="WeaponRunModifiers.GauntletSteps"/> SHALL
    /// append exactly one spherical step after the final authored step with radius
    /// <c>3 * (1 + 0.2r)</c>, and the source ability's authored steps SHALL remain unchanged.
    /// </para>
    ///
    /// <para>
    /// The test mirrors the conventions established by <see cref="AssetIsolationTests"/> and
    /// <see cref="GauntletStepsCloneIsolationTests"/>: it constructs a standalone
    /// <see cref="BreakerGauntletAbility"/> <see cref="ScriptableObject"/> with authored
    /// <see cref="AreaHitStep"/> data injected through a <see cref="JsonUtility.FromJsonOverwrite"/>
    /// overlay (<c>_hitSteps</c> is the private serialized backing field), snapshots the source's
    /// serialized <see cref="BreakerGauntletAbility.HitSteps"/> with <see cref="JsonUtility.ToJson(object)"/>,
    /// drives <c>GauntletSteps</c>, and asserts. Pure ScriptableObject plan generation, so it runs in
    /// EditMode (no scene, no physics).
    /// </para>
    ///
    /// <para>
    /// Isolation of the Shockwave sphere: the E slot (slot 2) ShockRing branch turns every step into
    /// a sphere, which would make "exactly one extra sphere" impossible to observe cleanly. The
    /// Shockwave boon is the only Gauntlet boon added here (FlurryEcho/AsuraEcho echoes are therefore
    /// 0), and the test drives a non-E slot (0/1/3), so the authored steps stay boxes and the single
    /// appended step is the Shockwave sphere. A rank-0 baseline (no Shockwave) is compared against the
    /// rank-r run for the same slot and identical source data, so "exactly one MORE sphere step" is
    /// asserted structurally rather than assumed.
    /// </para>
    /// </summary>
    public sealed class ShockwaveAppendPropertyTests
    {
        // MaxRank for the Shockwave boon, read from the shared catalog (kept in sync automatically).
        private static readonly int ShockwaveMaxRank = ShockwaveDefinition().MaxRank;

        // Non-E slots only: slot 2 (E) forces every step to a sphere via the ShockRing branch, which
        // would mask the single appended Shockwave sphere. 0 (Q), 1 (W), 3 (R) keep authored boxes.
        private static readonly int[] NonEStanceSlots = { 0, 1, 3 };

        [Serializable]
        private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        private static WeaponRunModifiers.Definition ShockwaveDefinition()
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == WeaponBoon.Shockwave) return definition;
            throw new InvalidOperationException("Missing catalog definition for Shockwave");
        }

        // A boxed authored step (never a sphere) so the appended Shockwave sphere is the only sphere in
        // the result for a non-E slot; distinct field values per index give the source a non-trivial
        // serialized footprint for the isolation snapshot.
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
                hitStrength = HitStrength.Medium,
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
            ability.name = "SourceGauntletShockwave";
            var authored = new AreaHitStep[stepCount];
            for (int i = 0; i < stepCount; i++) authored[i] = NewAuthoredStep(i);
            var overlay = new HitStepsOverlay { _hitSteps = authored };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // A Gauntlet WeaponRunModifiers carrying ONLY Shockwave ranks (rank == 0 when count == 0), so
        // no other Gauntlet boon perturbs the step list (echoes stay 0, no ShockRing sphere-conversion).
        private static WeaponRunModifiers ShockwaveOnly(int rank)
        {
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            var definition = ShockwaveDefinition();
            for (int r = 0; r < rank; r++)
                PropertyCheck.That(mods.Add(definition), "Shockwave rank should be addable within [1, MaxRank]");
            return mods;
        }

        private static int CountSpheres(List<AreaHitStep> steps)
        {
            int count = 0;
            foreach (AreaHitStep step in steps)
                if (step.hitShape == AreaHitShape.Sphere) count++;
            return count;
        }

        [Serializable]
        private struct AuthoredStepsSnapshot { public List<AreaHitStep> items; }

        // Setup sanity: the overlay actually injected the authored HitSteps (guards the premise so the
        // isolation snapshot below is comparing real authored data, not an empty list).
        [Test]
        public void Setup_InjectsAuthoredHitSteps()
        {
            var ability = NewGauntletWithSteps(3);
            try { Assert.That(ability.HitSteps.Count, Is.EqualTo(3)); }
            finally { UnityEngine.Object.DestroyImmediate(ability); }
        }

        // Feature: impactful-weapon-boons, Property 11
        // For any Shockwave rank r >= 1, GauntletSteps appends exactly one spherical step after the
        // final authored step with radius 3*(1+0.2r) (exactly one MORE sphere than the rank-0 baseline
        // for the same slot), and the source ability's authored HitSteps are byte-identical before and
        // after the call (source unchanged).
        // Validates: Requirements 7.1, 7.2, 7.4
        [Test]
        public void GauntletSteps_AppendsExactlyOneShockwaveSphere_LeavingSourceUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = 1 + rng.Next(ShockwaveMaxRank);         // r in [1, MaxRank]
                int slot = NonEStanceSlots[rng.Next(NonEStanceSlots.Length)];
                int stepCount = 1 + rng.Next(4);                    // 1..4 authored steps

                var source = NewGauntletWithSteps(stepCount);
                try
                {
                    // Snapshot the source's authored steps before any planning (R7.4 isolation).
                    string sourceBefore = JsonUtility.ToJson(
                        new AuthoredStepsSnapshot { items = new List<AreaHitStep>(source.HitSteps) });

                    // Rank-0 baseline: no Shockwave (and no other boon), so no sphere is appended and
                    // the sphere count equals whatever the authored steps carry (0 here, all boxes).
                    List<AreaHitStep> baseline = ShockwaveOnly(0).GauntletSteps(source, slot);
                    int baselineSpheres = CountSpheres(baseline);

                    // Rank-r run for the same slot and identical source data.
                    List<AreaHitStep> planned = ShockwaveOnly(rank).GauntletSteps(source, slot);

                    // Exactly one MORE step than the baseline, and exactly one MORE sphere.
                    PropertyCheck.That(planned.Count == baseline.Count + 1,
                        "Shockwave rank " + rank + " should add exactly one step over the rank-0 baseline (slot "
                        + slot + "): baseline=" + baseline.Count + " planned=" + planned.Count);
                    PropertyCheck.That(CountSpheres(planned) == baselineSpheres + 1,
                        "Shockwave should add exactly one MORE sphere step than the baseline (slot " + slot
                        + "): baselineSpheres=" + baselineSpheres + " plannedSpheres=" + CountSpheres(planned));

                    // The appended step is the last one, is a sphere, with radius 3*(1+0.2r).
                    AreaHitStep appended = planned[planned.Count - 1];
                    PropertyCheck.That(appended.hitShape == AreaHitShape.Sphere,
                        "The appended (last) step must be a Sphere (slot " + slot + ", rank " + rank + ")");
                    float expectedRadius = 3f * (1f + 0.2f * rank);
                    PropertyCheck.That(Mathf.Approximately(appended.sphereRadius, expectedRadius),
                        "Appended sphere radius should be 3*(1+0.2*" + rank + ")=" + expectedRadius
                        + " but was " + appended.sphereRadius);

                    // R7.4: the source ability's authored steps are byte-identical after the call.
                    string sourceAfter = JsonUtility.ToJson(
                        new AuthoredStepsSnapshot { items = new List<AreaHitStep>(source.HitSteps) });
                    PropertyCheck.That(sourceAfter == sourceBefore,
                        "GauntletSteps mutated the source ability's authored HitSteps (slot " + slot
                        + ", rank " + rank + ")");
                }
                finally { if (source) UnityEngine.Object.DestroyImmediate(source); }
            });
        }
    }
}
