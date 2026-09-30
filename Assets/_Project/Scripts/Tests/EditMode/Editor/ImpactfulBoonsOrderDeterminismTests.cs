using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 3 of impactful-weapon-boons (order independence).
    ///
    /// Property 3 (design): <em>for any</em> two acquisition orderings that yield the same rank
    /// multiset, the Cast_Plan (and <see cref="WeaponRunModifiers.GauntletSteps"/> output) SHALL be
    /// byte-identical. <b>Validates: Requirements 1.3.</b>
    ///
    /// This is the impactful-weapon-boons counterpart to
    /// <see cref="ModifierApplicationOrderDeterminismPropertyTests"/> (which covers the swarm-rework
    /// boons). It builds TWO independent <see cref="WeaponRunModifiers"/> for the same family, feeds them
    /// the SAME multiset of the NEW family boons in two DIFFERENT shuffled acquisition orders, confirms
    /// the per-boon ranks match, and asserts <c>Plan</c>/<c>GauntletSteps</c> yield byte-identical
    /// snapshots for every ability slot.
    ///
    /// The Bow/Spear <see cref="PlanSnapshot"/> mirror below <b>includes the new
    /// <see cref="ArsenalCastPlan.ImpaleLine"/>/<see cref="ArsenalCastPlan.ImpalePull"/> fields</b>
    /// (the swarm-rework snapshot predates them), so Impaling Line's plan contribution is part of the
    /// byte-identity comparison. The Gauntlet path serialises the whole produced step list, so the
    /// appended Shockwave step is compared in full.
    ///
    /// Pure ScriptableObject plan generation — no scene, no physics — so it runs in EditMode. Source
    /// abilities are created and destroyed after use and are never mutated (that isolation is Property 1).
    /// </summary>
    public sealed class ImpactfulBoonsOrderDeterminismTests
    {
        private static readonly ArsenalSkillKind[] ArsenalKinds =
        {
            ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust, ArsenalSkillKind.Sweep,
        };

        // The new family boons per family (the only boons whose order this property permutes).
        private static readonly WeaponBoon[] BowNewBoons = { WeaponBoon.SplitArrow, WeaponBoon.ChargedShot };
        private static readonly WeaponBoon[] SpearNewBoons = { WeaponBoon.PerfectSpacing, WeaponBoon.ImpalingLine };
        private static readonly WeaponBoon[] GauntletNewBoons = { WeaponBoon.MomentumStrike, WeaponBoon.Shockwave };

        [Serializable] private struct ArsenalKindOverlay { public int _kind; }
        [Serializable] private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        // Serializable snapshot mirror of the cast plan so JsonUtility can compare the WHOLE snapshot.
        // Extends the swarm-rework mirror with the impactful-weapon-boons R5 fields (ImpaleLine/ImpalePull).
        [Serializable]
        private struct PlanSnapshot
        {
            public float Windup, Interval, Range, Width, Damage, WaveMultiplier;
            public int Hits, Arrows, Directions;
            public bool TrackCursor, Travel, ReturnWave, ChainThrust;
            public float PhantomDelay;
            public int ShardCount;
            public bool ImpaleLine;   // impactful-weapon-boons R5
            public float ImpalePull;  // impactful-weapon-boons R5

            public static PlanSnapshot From(ArsenalCastPlan p) => new PlanSnapshot
            {
                Windup = p.Windup, Interval = p.Interval, Range = p.Range, Width = p.Width,
                Damage = p.Damage, WaveMultiplier = p.WaveMultiplier, Hits = p.Hits, Arrows = p.Arrows,
                Directions = p.Directions, TrackCursor = p.TrackCursor, Travel = p.Travel,
                ReturnWave = p.ReturnWave, ChainThrust = p.ChainThrust, PhantomDelay = p.PhantomDelay,
                ShardCount = p.ShardCount, ImpaleLine = p.ImpaleLine, ImpalePull = p.ImpalePull,
            };
        }

        [Serializable] private struct StepListSnapshot { public List<AreaHitStep> steps; }

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        private static ArsenalAbility NewArsenalAbility(ArsenalSkillKind kind, int slot)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "ImpactfulOrderArsenal_" + kind + "_" + slot;
            var overlay = new ArsenalKindOverlay { _kind = (int)kind };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        private static AreaHitStep NewAuthoredStep(int index) => new AreaHitStep
        {
            delay = .1f * index, rangeOverride = 1.5f + index, hitShape = AreaHitShape.Box,
            boxSize = new Vector3(3f + index, 2f, 0f), sphereRadius = 1.5f,
            damageMultiplier = 1f + .25f * index, bonusDamage = index,
            localOffset = new Vector3(0f, index, 0f), reactionType = HitReactionType.Stagger,
            hitStrength = HitStrength.Light, pushDistance = .35f, stanceDamage = 12f + index,
            breakEffect = StanceBreakEffect.None, stunDuration = 1f, knockUpHeight = 2f, knockbackDistance = 4f,
        };

        private static BreakerGauntletAbility NewGauntletAbility(int slot, int stepCount)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "ImpactfulOrderGauntlet_" + slot;
            var authored = new AreaHitStep[stepCount];
            for (int i = 0; i < stepCount; i++) authored[i] = NewAuthoredStep(i);
            var overlay = new HitStepsOverlay { _hitSteps = authored };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        private static WeaponBoon[] NewBoonsFor(RunWeaponFamily family) => family == RunWeaponFamily.Bow
            ? BowNewBoons : family == RunWeaponFamily.Spear ? SpearNewBoons : GauntletNewBoons;

        // Expand a per-boon wanted-rank vector into a flat acquisition multiset (a boon at rank k appears
        // k times); this is the multiset whose ORDER the property permutes.
        private static List<WeaponRunModifiers.Definition> BuildAcquisitionMultiset(WeaponBoon[] boons, int[] wanted)
        {
            var acquisitions = new List<WeaponRunModifiers.Definition>();
            for (int b = 0; b < boons.Length; b++)
                for (int r = 0; r < wanted[b]; r++) acquisitions.Add(Def(boons[b]));
            return acquisitions;
        }

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
            foreach (WeaponRunModifiers.Definition def in order)
                PropertyCheck.That(mods.Add(def), "in-family, in-range acquisition should be addable: " + def.Id);
            return mods;
        }

        // Feature: impactful-weapon-boons, Property 3: Order independence.
        // For any family and any multiset of the new boons+ranks, applying the acquisitions in two
        // different permutations yields identical per-boon ranks and byte-identical cast snapshots
        // (Plan for Bow/Spear including the new ImpaleLine/ImpalePull fields, GauntletSteps for Gauntlet
        // including the appended Shockwave step) across every ability slot.
        // Validates: Requirements 1.3
        [Test]
        public void NewBoons_PlanAndGauntletSteps_AreIndependentOfAcquisitionOrder()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int familyRoll = rng.Next(3);
                RunWeaponFamily family = familyRoll == 0 ? RunWeaponFamily.Bow
                    : familyRoll == 1 ? RunWeaponFamily.Spear : RunWeaponFamily.Gauntlet;
                WeaponBoon[] boons = NewBoonsFor(family);

                int[] wanted = new int[boons.Length];
                for (int b = 0; b < boons.Length; b++) wanted[b] = rng.Next(0, Def(boons[b]).MaxRank + 1);

                var orderA = BuildAcquisitionMultiset(boons, wanted);
                var orderB = new List<WeaponRunModifiers.Definition>(orderA);
                Shuffle(orderA, rng);
                Shuffle(orderB, rng);

                WeaponRunModifiers modsA = Apply(family, orderA);
                WeaponRunModifiers modsB = Apply(family, orderB);

                for (int b = 0; b < boons.Length; b++)
                {
                    PropertyCheck.That(modsA.Rank(boons[b]) == wanted[b],
                        $"family={family}, boon={boons[b]}: rank A={modsA.Rank(boons[b])}, wanted={wanted[b]}");
                    PropertyCheck.That(modsB.Rank(boons[b]) == modsA.Rank(boons[b]),
                        $"family={family}, boon={boons[b]}: rank A={modsA.Rank(boons[b])}, B={modsB.Rank(boons[b])}");
                }

                var sources = new List<UnityEngine.Object>();
                try
                {
                    for (int slot = 0; slot < 4; slot++)
                    {
                        if (family == RunWeaponFamily.Gauntlet)
                        {
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
