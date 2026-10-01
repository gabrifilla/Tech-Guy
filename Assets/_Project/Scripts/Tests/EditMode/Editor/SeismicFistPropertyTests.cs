using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 13 of gauntlet-boon-playstyle-overhaul — the opt-in
    /// deliberate knockback of the Gauntlet's "Punho sísmico" (<see cref="WeaponBoon.SeismicFist"/>,
    /// R7) boon as wired into <see cref="WeaponRunModifiers.GauntletSteps"/> (task 11).
    ///
    /// Property 13 (design): when <c>Rank(SeismicFist) &gt;= 1</c> every cloned <see cref="AreaHitStep"/>
    /// returned by <c>GauntletSteps</c> SHALL declare <see cref="StanceBreakEffect.Knockback"/> as its
    /// <c>breakEffect</c>, with <c>knockbackDistance</c> scaled by <c>1 + 0.3 * rank</c>; when
    /// <c>Rank(SeismicFist) == 0</c> the baseline no-push behavior SHALL be unchanged (the clone keeps
    /// the source's authored <c>breakEffect</c> and <c>knockbackDistance</c>, and the per-hit
    /// <c>pushDistance</c> is left exactly as the baseline leaves it — SeismicFist adds no shove of its
    /// own; the hard displacement only comes from the Stance_Break knockback). The source asset is
    /// never mutated (the scaling lands on the per-cast JsonUtility clone).
    ///
    /// This mirrors <see cref="GauntletOverhaulInvariantPropertyTests"/> and
    /// <see cref="GauntletStepsCloneIsolationTests"/>: a standalone source
    /// <see cref="BreakerGauntletAbility"/> is given real authored steps via a
    /// <see cref="JsonUtility.FromJsonOverwrite(string, object)"/> overlay on the private
    /// <c>_hitSteps</c> backing field, and the seeded <see cref="PropertyCheck"/> harness drives
    /// &gt;= 100 deterministic cases (FsCheck/CsCheck are unavailable on this machine), reporting the
    /// exact failing case as a counterexample.
    ///
    /// Isolation of the SeismicFist increment: only SeismicFist is applied, and each rank-R clone is
    /// compared against the rank-0 clone produced from an identically authored source. The rank-0 clone
    /// is the baseline (it already carries any non-SeismicFist scaling GauntletSteps applies with no
    /// boons), so the only difference the assertions attribute to SeismicFist is the Knockback declaration
    /// and the <c>1 + 0.3R</c> distance scaling.
    ///
    /// Pure ScriptableObject plan generation — no scene, no physics — so it runs in EditMode.
    /// </summary>
    public sealed class SeismicFistPropertyTests
    {
        private const float Tolerance = 1e-3f;
        private const int MaxRank = 3;

        // Drive every ability slot (Q/W/E/R); SeismicFist's opt-in is per-cast on every slot's steps.
        private static readonly int[] Slots = { 0, 1, 2, 3 };

        [Serializable] private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        private static WeaponRunModifiers.Definition SeismicDef()
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == WeaponBoon.SeismicFist) return definition;
            throw new InvalidOperationException("Missing catalog definition for SeismicFist");
        }

        // The design's adjustable starting-point formula for the knockback distance scale (R7.1).
        private static float KnockbackScale(int rank) => 1f + 0.3f * rank;

        // A deterministic authored step whose breakEffect varies (including non-None) and whose
        // knockbackDistance is a known positive value, so the baseline-vs-scaled comparison is meaningful.
        private static AreaHitStep NewAuthoredStep(System.Random rng, int index)
        {
            StanceBreakEffect[] effects =
                { StanceBreakEffect.None, StanceBreakEffect.Stun, StanceBreakEffect.KnockUp, StanceBreakEffect.Knockback };
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
                hitStrength = HitStrength.Light,
                pushDistance = 0.35f,
                stanceDamage = 12f + index,
                breakEffect = effects[rng.Next(effects.Length)],
                stunDuration = 1f,
                knockUpHeight = 2f,
                knockbackDistance = 2f + (float)rng.NextDouble() * 6f, // positive baseline so scaling is observable
            };
        }

        private static BreakerGauntletAbility NewGauntletWithSteps(System.Random rng, int stepCount)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "SeismicSourceGauntlet";
            var authored = new AreaHitStep[stepCount];
            for (int i = 0; i < stepCount; i++) authored[i] = NewAuthoredStep(rng, i);
            var overlay = new HitStepsOverlay { _hitSteps = authored };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        private static WeaponRunModifiers GauntletWithSeismic(int rank)
        {
            var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            WeaponRunModifiers.Definition def = SeismicDef();
            for (int r = 0; r < rank; r++)
                PropertyCheck.That(mods.Add(def), $"SeismicFist rank {r + 1} should be addable");
            return mods;
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 13: Punho sísmico é opt-in.
        // For any authored step list, any slot and any rank R in [1, MaxRank], every cloned step
        // GauntletSteps returns declares StanceBreakEffect.Knockback and its knockbackDistance equals the
        // rank-0 baseline clone's knockbackDistance scaled by (1 + 0.3R). At rank 0 the clone keeps the
        // baseline breakEffect/knockbackDistance (no SeismicFist mutation). The source asset is never
        // mutated by the scaling.
        // Validates: Requirements 7.1, 7.3
        [Test]
        public void SeismicFist_IsOptIn_DeclaresScaledKnockbackAtRankOrAbove_BaselineAtRankZero()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int stepCount = rng.Next(1, 4);        // 1..3 authored steps
                int rank = rng.Next(0, MaxRank + 1);   // 0..MaxRank
                int slot = Slots[rng.Next(Slots.Length)];

                // Capture the authored steps once so both the baseline (rank 0) and the scaled (rank R)
                // snapshots are planned from byte-identical sources.
                BreakerGauntletAbility template = NewGauntletWithSteps(rng, stepCount);
                string authoredJson = JsonUtility.ToJson(template);
                UnityEngine.Object.DestroyImmediate(template);

                var baselineSource = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
                var scaledSource = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
                try
                {
                    JsonUtility.FromJsonOverwrite(authoredJson, baselineSource);
                    JsonUtility.FromJsonOverwrite(authoredJson, scaledSource);
                    string scaledSourceBefore = JsonUtility.ToJson(scaledSource);

                    WeaponRunModifiers baselineMods = GauntletWithSeismic(0);
                    WeaponRunModifiers scaledMods = GauntletWithSeismic(rank);

                    List<AreaHitStep> baseline = baselineMods.GauntletSteps(baselineSource, slot);
                    List<AreaHitStep> scaled = scaledMods.GauntletSteps(scaledSource, slot);

                    PropertyCheck.That(baseline.Count == scaled.Count,
                        $"[case #{i}] slot={slot} rank={rank}: step count mismatch baseline={baseline.Count} scaled={scaled.Count}");

                    for (int s = 0; s < scaled.Count; s++)
                    {
                        AreaHitStep b = baseline[s];
                        AreaHitStep c = scaled[s];

                        if (rank <= 0)
                        {
                            // Opt-in (R7.3): rank 0 ⇒ no SeismicFist mutation; the scaled clone equals the
                            // baseline clone's break declaration and distance.
                            PropertyCheck.That(c.breakEffect == b.breakEffect,
                                $"[case #{i}] slot={slot} step={s} rank=0: breakEffect changed from baseline " +
                                $"{b.breakEffect} to {c.breakEffect} without the boon");
                            PropertyCheck.That(Mathf.Approximately(c.knockbackDistance, b.knockbackDistance),
                                $"[case #{i}] slot={slot} step={s} rank=0: knockbackDistance changed from baseline " +
                                $"{b.knockbackDistance} to {c.knockbackDistance} without the boon");
                        }
                        else
                        {
                            // R7.1: rank >= 1 ⇒ the clone declares Knockback.
                            PropertyCheck.That(c.breakEffect == StanceBreakEffect.Knockback,
                                $"[case #{i}] slot={slot} step={s} rank={rank}: expected Knockback breakEffect but was {c.breakEffect}");

                            // R7.1: knockbackDistance = baseline * (1 + 0.3R).
                            float expected = b.knockbackDistance * KnockbackScale(rank);
                            PropertyCheck.That(Mathf.Abs(c.knockbackDistance - expected) <= Tolerance + Tolerance * Mathf.Abs(expected),
                                $"[case #{i}] slot={slot} step={s} rank={rank}: knockbackDistance {c.knockbackDistance} " +
                                $"!= baseline {b.knockbackDistance} * (1 + 0.3*{rank}) = {expected}");

                            // Monotonic in rank: the scaled distance is never below the baseline (R7.1 is a
                            // non-decreasing +30%/rank growth from a positive baseline).
                            PropertyCheck.That(c.knockbackDistance >= b.knockbackDistance - Tolerance,
                                $"[case #{i}] slot={slot} step={s} rank={rank}: scaled distance {c.knockbackDistance} " +
                                $"fell below baseline {b.knockbackDistance}");
                        }
                    }

                    // Asset isolation (R7.4): the scaling landed on the per-cast clone, never the source.
                    PropertyCheck.That(JsonUtility.ToJson(scaledSource) == scaledSourceBefore,
                        $"[case #{i}] slot={slot} rank={rank}: SeismicFist mutated the source gauntlet asset");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(baselineSource);
                    UnityEngine.Object.DestroyImmediate(scaledSource);
                }
            });
        }
    }
}
