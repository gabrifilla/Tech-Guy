using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for R12 Property 39 of weapon-gameplay-swarm-rework
    /// (deterministic modifier application order, <see cref="WeaponRunModifiers.Plan"/> /
    /// <see cref="WeaponRunModifiers.GauntletSteps"/>).
    ///
    /// Property 39 (design.md): for every multiset of Run_Modifier with their ranks, applying the
    /// modifiers in any permutation produces exactly the same resulting cast snapshot
    /// (<see cref="ArsenalCastPlan"/> for Bow/Spear, the <see cref="AreaHitStep"/> list for Gauntlet).
    ///
    /// Approach: <see cref="WeaponRunModifiers"/> keys ranks per <see cref="WeaponBoon"/> in a
    /// dictionary and <c>Add</c> only ever increments the acquired boon (see the order-determinism
    /// contract documented on the field/methods), so the final rank multiset is the only thing a
    /// snapshot can depend on. This test builds TWO independent <see cref="WeaponRunModifiers"/> for
    /// the same family, feeds them the SAME multiset of catalog boons but in two DIFFERENT randomly
    /// shuffled acquisition orders, confirms the resulting per-boon ranks match, and then asserts
    /// that <c>Plan</c>/<c>GauntletSteps</c> yield byte-identical snapshots for every ability slot.
    /// Snapshots are compared via <see cref="JsonUtility.ToJson(object)"/> of a serializable wrapper
    /// so the whole snapshot (every field / every step) is compared, not a hand-picked subset.
    ///
    /// Pure ScriptableObject plan generation — no scene, no physics — so it runs in EditMode. Source
    /// abilities are created with <see cref="ScriptableObject.CreateInstance"/> and destroyed after
    /// use; they are never mutated (that isolation is Property 25, asserted elsewhere).
    /// </summary>
    public sealed class ModifierApplicationOrderDeterminismPropertyTests
    {
        private static readonly RunWeaponFamily[] Families =
        {
            RunWeaponFamily.Bow, RunWeaponFamily.Spear, RunWeaponFamily.Gauntlet,
        };

        // The arsenal kinds Plan can branch on (Thrust/Sweep gate the spear-specific branches);
        // every slot gets one so the Kind-gated branches are actually exercised on some casts.
        private static readonly ArsenalSkillKind[] ArsenalKinds =
        {
            ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust, ArsenalSkillKind.Sweep,
        };

        // A JSON overlay lets us drive Plan's Kind-gated spear branches without touching the source
        // through a reward path (same technique as AssetIsolationTests / GauntletStepsCloneIsolationTests).
        [Serializable] private struct ArsenalKindOverlay { public int _kind; }
        [Serializable] private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        // Serializable snapshot mirrors of the cast plan / step list so JsonUtility can compare the
        // WHOLE snapshot. ArsenalCastPlan itself is not [Serializable], so its fields are copied here.
        [Serializable]
        private struct PlanSnapshot
        {
            public float Windup, Interval, Range, Width, Damage, WaveMultiplier;
            public int Hits, Arrows, Directions;
            public bool TrackCursor, Travel, ReturnWave, ChainThrust;
            public float PhantomDelay;
            public int ShardCount;

            public static PlanSnapshot From(ArsenalCastPlan p) => new PlanSnapshot
            {
                Windup = p.Windup, Interval = p.Interval, Range = p.Range, Width = p.Width,
                Damage = p.Damage, WaveMultiplier = p.WaveMultiplier, Hits = p.Hits, Arrows = p.Arrows,
                Directions = p.Directions, TrackCursor = p.TrackCursor, Travel = p.Travel,
                ReturnWave = p.ReturnWave, ChainThrust = p.ChainThrust, PhantomDelay = p.PhantomDelay,
                ShardCount = p.ShardCount,
            };
        }

        [Serializable] private struct StepListSnapshot { public List<AreaHitStep> steps; }

        private static ArsenalAbility NewArsenalAbility(ArsenalSkillKind kind, int slot)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "OrderDetArsenal_" + kind + "_" + slot;
            var overlay = new ArsenalKindOverlay { _kind = (int)kind };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // Authored gauntlet steps so the clone/echo path inside GauntletSteps is actually exercised
        // (an empty ability would make the snapshot trivially order-independent).
        private static AreaHitStep NewAuthoredStep(int index) => new AreaHitStep
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
            breakEffect = StanceBreakEffect.None,
            stunDuration = 1f,
            knockUpHeight = 2f,
            knockbackDistance = 4f,
        };

        private static BreakerGauntletAbility NewGauntletAbility(int slot, int stepCount)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "OrderDetGauntlet_" + slot;
            var authored = new AreaHitStep[stepCount];
            for (int i = 0; i < stepCount; i++) authored[i] = NewAuthoredStep(i);
            var overlay = new HitStepsOverlay { _hitSteps = authored };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // Every catalog definition for a family, in a stable order (Catalog order), so we can build a
        // per-boon "wanted rank" multiset and later replay it in shuffled acquisition orders.
        private static List<WeaponRunModifiers.Definition> FamilyBoons(RunWeaponFamily family)
        {
            var list = new List<WeaponRunModifiers.Definition>();
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Family == family) list.Add(definition);
            return list;
        }

        // Expand a per-boon wanted-rank vector into a flat acquisition multiset: one entry per rank
        // step (a boon wanted at rank k appears k times). This is the multiset whose ORDER we permute.
        private static List<WeaponRunModifiers.Definition> BuildAcquisitionMultiset(
            List<WeaponRunModifiers.Definition> boons, int[] wanted)
        {
            var acquisitions = new List<WeaponRunModifiers.Definition>();
            for (int b = 0; b < boons.Count; b++)
                for (int r = 0; r < wanted[b]; r++) acquisitions.Add(boons[b]);
            return acquisitions;
        }

        // Fisher-Yates shuffle driven by the property RNG (deterministic per seed+iteration).
        private static void Shuffle(List<WeaponRunModifiers.Definition> list, System.Random rng)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static WeaponRunModifiers Apply(RunWeaponFamily family, List<WeaponRunModifiers.Definition> order)
        {
            var mods = new WeaponRunModifiers(family);
            foreach (var definition in order)
                PropertyCheck.That(mods.Add(definition),
                    "in-family, in-range acquisition should be addable: " + definition.Id);
            return mods;
        }

        // Feature: weapon-gameplay-swarm-rework, Property 39: Ordem determinística de aplicação de modificadores
        // For any family and any multiset of catalog boons+ranks, applying the acquisitions in two
        // different permutations produces identical per-boon ranks and byte-identical cast snapshots
        // (Plan for Bow/Spear, GauntletSteps for Gauntlet) across every ability slot.
        // Validates: Requirements 12.7
        [Test]
        public void PlanAndGauntletSteps_AreIndependentOfModifierAcquisitionOrder()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                RunWeaponFamily family = Families[rng.Next(Families.Length)];
                List<WeaponRunModifiers.Definition> boons = FamilyBoons(family);
                PropertyCheck.That(boons.Count > 0, "family " + family + " has no catalog boons");

                // A random wanted rank in [0; MaxRank] per boon -> the shared acquisition multiset.
                int[] wanted = new int[boons.Count];
                for (int b = 0; b < boons.Count; b++) wanted[b] = rng.Next(0, boons[b].MaxRank + 1);

                var orderA = BuildAcquisitionMultiset(boons, wanted);
                var orderB = new List<WeaponRunModifiers.Definition>(orderA);
                // Permute both orders independently; identical multiset, (generally) different order.
                Shuffle(orderA, rng);
                Shuffle(orderB, rng);

                WeaponRunModifiers modsA = Apply(family, orderA);
                WeaponRunModifiers modsB = Apply(family, orderB);

                // Ranks must match the wanted multiset regardless of acquisition order (the contract's core).
                for (int b = 0; b < boons.Count; b++)
                {
                    WeaponBoon kind = boons[b].Kind;
                    PropertyCheck.That(modsA.Rank(kind) == wanted[b],
                        $"family={family}, boon={kind}: rank A={modsA.Rank(kind)}, wanted={wanted[b]}");
                    PropertyCheck.That(modsB.Rank(kind) == modsA.Rank(kind),
                        $"family={family}, boon={kind}: rank A={modsA.Rank(kind)}, rank B={modsB.Rank(kind)}");
                }

                var sources = new List<UnityEngine.Object>();
                try
                {
                    for (int slot = 0; slot < 4; slot++)
                    {
                        if (family == RunWeaponFamily.Gauntlet)
                        {
                            // Two independent source abilities with identical authored steps so any
                            // difference in the output can only come from acquisition order.
                            int stepCount = rng.Next(1, 4);
                            BreakerGauntletAbility abilityA = NewGauntletAbility(slot, stepCount);
                            BreakerGauntletAbility abilityB = NewGauntletAbility(slot, stepCount);
                            sources.Add(abilityA);
                            sources.Add(abilityB);

                            string snapA = JsonUtility.ToJson(new StepListSnapshot { steps = modsA.GauntletSteps(abilityA, slot) });
                            string snapB = JsonUtility.ToJson(new StepListSnapshot { steps = modsB.GauntletSteps(abilityB, slot) });
                            PropertyCheck.That(snapA == snapB,
                                $"family={family}, slot={slot}: GauntletSteps snapshot differs by acquisition order\nA={snapA}\nB={snapB}");
                        }
                        else
                        {
                            ArsenalSkillKind kind = ArsenalKinds[rng.Next(ArsenalKinds.Length)];
                            ArsenalAbility abilityA = NewArsenalAbility(kind, slot);
                            ArsenalAbility abilityB = NewArsenalAbility(kind, slot);
                            sources.Add(abilityA);
                            sources.Add(abilityB);

                            string snapA = JsonUtility.ToJson(PlanSnapshot.From(modsA.Plan(abilityA, slot)));
                            string snapB = JsonUtility.ToJson(PlanSnapshot.From(modsB.Plan(abilityB, slot)));
                            PropertyCheck.That(snapA == snapB,
                                $"family={family}, slot={slot}, kind={kind}: Plan snapshot differs by acquisition order\nA={snapA}\nB={snapB}");
                        }
                    }
                }
                finally
                {
                    foreach (UnityEngine.Object o in sources) if (o) UnityEngine.Object.DestroyImmediate(o);
                }
            });
        }
    }
}
