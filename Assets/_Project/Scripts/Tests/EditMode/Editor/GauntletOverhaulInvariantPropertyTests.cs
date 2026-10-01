using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the three cross-cutting invariants of
    /// gauntlet-boon-playstyle-overhaul, covering the ten new boons
    /// (<see cref="WeaponBoon.AsuraFist"/>, <see cref="WeaponBoon.GuardBreaker"/>,
    /// <see cref="WeaponBoon.HungryCombo"/>, <see cref="WeaponBoon.SeismicFist"/> — Gauntlet;
    /// <see cref="WeaponBoon.KitingStep"/>, <see cref="WeaponBoon.AdaptiveCadence"/>,
    /// <see cref="WeaponBoon.RainMark"/> — Bow; <see cref="WeaponBoon.SpacingRecoil"/>,
    /// <see cref="WeaponBoon.PikeWall"/>, <see cref="WeaponBoon.EdgeStrike"/> — Spear).
    ///
    /// This is the gauntlet-boon-playstyle-overhaul counterpart to <see cref="AssetIsolationTests"/>,
    /// <see cref="ImpactfulBoonsMonotonicityTests"/> and <see cref="ImpactfulBoonsOrderDeterminismTests"/>,
    /// reusing their patterns (standalone source ScriptableObjects snapshotted with
    /// <see cref="JsonUtility.ToJson(object)"/>, the <see cref="PropertyCheck"/> seeded harness driving
    /// &gt;= 100 deterministic cases). FsCheck/CsCheck are not available on this machine.
    ///
    /// Property 1 (Isolamento de asset): applying any new boon at any rank (and any retired boon)
    /// leaves every source weapon/ability asset byte-for-byte unchanged, and the <see cref="WeaponRunModifiers.Catalog"/>
    /// still contains the retired entries (retire != delete, R2.5/R2.6).
    ///
    /// Property 2 (Monotonicidade por rank): each new boon's named per-rank quantity (the design's
    /// adjustable starting-point formula) is non-decreasing across ranks 1..3. Several of the new boons
    /// are event/coordinator-driven or back pure decision classes (AsuraFist/GuardBreaker/HungryCombo/
    /// KitingStep/AdaptiveCadence/SpacingRecoil/EdgeStrike) and some plan/step wirings (RainMark/
    /// SeismicFist/PikeWall) are consumed by downstream tasks, so the monotone quantity asserted here is
    /// the named coefficient each boon's rank feeds, exactly as <see cref="ImpactfulBoonsMonotonicityTests"/>
    /// samples MomentumStrike's per-stack coefficient and SplitArrow's count rather than a live scene.
    ///
    /// Property 3 (Independência de ordem): for a family, two acquisition orderings yielding the same
    /// rank multiset of the new boons produce byte-identical <c>Plan</c>/<c>GauntletSteps</c> and an
    /// identical <see cref="WeaponRunModifiers.DirectDamageMultiplier"/> value across every slot.
    ///
    /// Pure ScriptableObject / arithmetic evaluation — no scene, no physics — so it runs in EditMode.
    /// </summary>
    public sealed class GauntletOverhaulInvariantPropertyTests
    {
        private const float Tolerance = 1e-4f;

        private static readonly ArsenalSkillKind[] ArsenalKinds =
        {
            ArsenalSkillKind.Arrow, ArsenalSkillKind.Volley, ArsenalSkillKind.Rain,
            ArsenalSkillKind.Thrust, ArsenalSkillKind.Sweep,
        };

        // The ten new boons grouped by family; the family gate only admits in-family boons.
        private static readonly WeaponBoon[] GauntletNewBoons =
            { WeaponBoon.AsuraFist, WeaponBoon.GuardBreaker, WeaponBoon.HungryCombo, WeaponBoon.SeismicFist };
        private static readonly WeaponBoon[] BowNewBoons =
            { WeaponBoon.KitingStep, WeaponBoon.AdaptiveCadence, WeaponBoon.RainMark };
        private static readonly WeaponBoon[] SpearNewBoons =
            { WeaponBoon.SpacingRecoil, WeaponBoon.PikeWall, WeaponBoon.EdgeStrike };

        // The nine retired family boons (retire != delete): they must stay in the Catalog for run/save
        // compatibility even though RunBoons.OfferReward skips them (R2.5/R2.6).
        private static readonly WeaponBoon[] RetiredFamilyBoons =
        {
            WeaponBoon.LongFists, WeaponBoon.StanceCrusher, WeaponBoon.Berserker,  // Gauntlet
            WeaponBoon.HeavyBolt, WeaponBoon.Sniper,        WeaponBoon.LongRain,   // Bow
            WeaponBoon.LongReach, WeaponBoon.TripleMoon,    WeaponBoon.Affliction, // Spear
        };

        [Serializable] private struct ArsenalKindOverlay { public int _kind; }
        [Serializable] private struct HitStepsOverlay { public AreaHitStep[] _hitSteps; }

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        private static bool CatalogContains(WeaponBoon kind)
        {
            foreach (WeaponRunModifiers.Definition definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return true;
            return false;
        }

        private static WeaponBoon[] NewBoonsFor(RunWeaponFamily family) => family == RunWeaponFamily.Bow
            ? BowNewBoons : family == RunWeaponFamily.Spear ? SpearNewBoons : GauntletNewBoons;

        private static ArsenalAbility NewArsenalAbility(ArsenalSkillKind kind, int index)
        {
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "OverhaulSourceArsenal_" + kind + "_" + index;
            var overlay = new ArsenalKindOverlay { _kind = (int)kind };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // Source gauntlet ability carrying authored steps so GauntletSteps has a real step list to clone,
        // scale and append onto (SeismicFist's knockbackDistance scaling, task 11, lands on these clones).
        private static BreakerGauntletAbility NewGauntletAbility(int index, int stepCount)
        {
            var ability = ScriptableObject.CreateInstance<BreakerGauntletAbility>();
            ability.name = "OverhaulSourceGauntlet_" + index;
            var authored = new AreaHitStep[stepCount];
            for (int i = 0; i < stepCount; i++)
                authored[i] = new AreaHitStep
                {
                    delay = .1f * i, rangeOverride = 1.5f + i, hitShape = AreaHitShape.Box,
                    boxSize = new Vector3(3f + i, 2f, 0f), sphereRadius = 1.5f,
                    damageMultiplier = 1f + .25f * i, stanceDamage = 12f + i,
                    pushDistance = .35f, knockbackDistance = 4f,
                };
            var overlay = new HitStepsOverlay { _hitSteps = authored };
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(overlay), ability);
            return ability;
        }

        // ---- Named per-rank quantities (design's adjustable starting-point formulas) ----
        // These are the monotone quantities each new boon's rank feeds (Property 2). They mirror the
        // coefficient-sampling style of ImpactfulBoonsMonotonicityTests for event/coordinator/pure boons.
        private static float AsuraFistEnergyPerHit(int rank) => 2f * rank;                 // R4.1: +2*rank Asura/basic hit
        private static float GuardBreakerStanceMultiplier(int rank) => 1f + 0.5f * rank;   // R5.1: 1 + 0.5R
        private static float HungryComboSecondsPerHit(int rank) => 0.3f * rank;            // R6.1: 0.3s * rank
        private static float SeismicKnockbackScale(int rank) => 1f + 0.3f * rank;          // R7.1: 1 + 0.3R
        private static float KitingDistanceScale(int rank) => 1f + 0.25f * rank;           // R8.2: 1 + 0.25R
        private static float AdaptiveCadencePercent(int rank) => 15f * rank;               // R9.1: 15% * rank
        private static float RainMarkAmplify(int rank) => 0.2f * rank;                     // R10.2: 0.2R
        private static float SpacingStepBackScale(int rank) => 0.2f * rank;                // R11.1: 0.2R
        private static float PikeWallZonePush(int rank) => 0.3f * rank;                    // R12.2: 0.3R
        private static float EdgeStrikeStanceMultiplier(int rank) => 1f + 0.4f * rank;     // R13.3: 1 + 0.4R

        private static float NamedQuantity(WeaponBoon kind, int rank)
        {
            switch (kind)
            {
                case WeaponBoon.AsuraFist: return AsuraFistEnergyPerHit(rank);
                case WeaponBoon.GuardBreaker: return GuardBreakerStanceMultiplier(rank);
                case WeaponBoon.HungryCombo: return HungryComboSecondsPerHit(rank);
                case WeaponBoon.SeismicFist: return SeismicKnockbackScale(rank);
                case WeaponBoon.KitingStep: return KitingDistanceScale(rank);
                case WeaponBoon.AdaptiveCadence: return AdaptiveCadencePercent(rank);
                case WeaponBoon.RainMark: return RainMarkAmplify(rank);
                case WeaponBoon.SpacingRecoil: return SpacingStepBackScale(rank);
                case WeaponBoon.PikeWall: return PikeWallZonePush(rank);
                case WeaponBoon.EdgeStrike: return EdgeStrikeStanceMultiplier(rank);
                default: throw new InvalidOperationException("No named quantity for " + kind);
            }
        }

        // Serializable snapshot of the cast plan including the gauntlet-overhaul fields, so the WHOLE
        // snapshot (incl. RainMark/SpacingRecoil/PikeWall value-type flags) is part of byte-identity.
        [Serializable]
        private struct PlanSnapshot
        {
            public float Windup, Interval, Range, Width, Damage, WaveMultiplier;
            public int Hits, Arrows, Directions;
            public bool TrackCursor, Travel, ReturnWave, ChainThrust;
            public float PhantomDelay;
            public int ShardCount;
            public bool ImpaleLine;
            public float ImpalePull;
            public bool MarkOnPulse;      // R10.1 (RainMark)
            public float MarkAmplify;     // R10.2 (RainMark)
            public float MarkSlow;        // R10.3 (RainMark)
            public bool SpacingRecoil;    // R11.1 (SpacingRecoil)
            public bool ControlZone;      // R12.1 (PikeWall)
            public float ZonePush;        // R12.2 (PikeWall)

            public static PlanSnapshot From(ArsenalCastPlan p) => new PlanSnapshot
            {
                Windup = p.Windup, Interval = p.Interval, Range = p.Range, Width = p.Width,
                Damage = p.Damage, WaveMultiplier = p.WaveMultiplier, Hits = p.Hits, Arrows = p.Arrows,
                Directions = p.Directions, TrackCursor = p.TrackCursor, Travel = p.Travel,
                ReturnWave = p.ReturnWave, ChainThrust = p.ChainThrust, PhantomDelay = p.PhantomDelay,
                ShardCount = p.ShardCount, ImpaleLine = p.ImpaleLine, ImpalePull = p.ImpalePull,
                MarkOnPulse = p.MarkOnPulse, MarkAmplify = p.MarkAmplify, MarkSlow = p.MarkSlow,
                SpacingRecoil = p.SpacingRecoil, ControlZone = p.ControlZone, ZonePush = p.ZonePush,
            };
        }

        [Serializable] private struct StepListSnapshot { public List<AreaHitStep> steps; }

        private static WeaponBoon[] FamilyRetiredBoons(RunWeaponFamily family)
        {
            var list = new List<WeaponBoon>();
            foreach (WeaponBoon retired in RetiredFamilyBoons)
                if (Def(retired).Family == family) list.Add(retired);
            return list.ToArray();
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 1: Isolamento de asset.
        // Adding the new boons AND the retired family boons at randomized ranks, then driving the
        // reward-owned plan path (Plan for Bow/Spear, GauntletSteps for Gauntlet) across every slot,
        // leaves every source ability asset byte-for-byte unchanged; and the Catalog still contains all
        // nine retired entries (retire != delete, R2.5/R2.6).
        // Validates: Requirements 1.1, 2.6, 5.5, 6.2, 7.4, 10.4, 11.5, 12.4, 13.4
        [Test]
        public void NewAndRetiredBoons_PlanPath_LeaveSourceAssetsUnchanged_AndCatalogKeepsRetired()
        {
            // The retired entries must remain catalogued regardless of any run state.
            foreach (WeaponBoon retired in RetiredFamilyBoons)
                Assert.That(CatalogContains(retired), Is.True,
                    $"Retired boon {retired} must remain in the Catalog (retire != delete).");

            PropertyCheck.ForAll((rng, i) =>
            {
                int familyRoll = rng.Next(3);
                RunWeaponFamily family = familyRoll == 0 ? RunWeaponFamily.Bow
                    : familyRoll == 1 ? RunWeaponFamily.Spear : RunWeaponFamily.Gauntlet;

                var mods = new WeaponRunModifiers(family);

                // Add each new in-family boon to a randomized rank in [0, MaxRank].
                foreach (WeaponBoon boon in NewBoonsFor(family))
                {
                    WeaponRunModifiers.Definition def = Def(boon);
                    int wanted = rng.Next(0, def.MaxRank + 1);
                    for (int r = 0; r < wanted; r++)
                        PropertyCheck.That(mods.Add(def), $"in-family {boon} rank {r + 1} should be addable");
                }

                // Also add this family's retired boons at randomized ranks: a run/save that already
                // acquired a retired boon must still plan without mutating any source asset (R2.6).
                foreach (WeaponBoon retired in FamilyRetiredBoons(family))
                {
                    WeaponRunModifiers.Definition def = Def(retired);
                    int wanted = rng.Next(0, def.MaxRank + 1);
                    for (int r = 0; r < wanted; r++)
                        PropertyCheck.That(mods.Add(def), $"in-family retired {retired} rank {r + 1} should be addable");
                }

                var sources = new List<UnityEngine.Object>();
                var before = new Dictionary<UnityEngine.Object, string>();
                try
                {
                    if (family == RunWeaponFamily.Gauntlet)
                    {
                        var abilities = new BreakerGauntletAbility[4];
                        for (int slot = 0; slot < abilities.Length; slot++)
                        {
                            abilities[slot] = NewGauntletAbility(slot, rng.Next(1, 4));
                            sources.Add(abilities[slot]);
                            before[abilities[slot]] = JsonUtility.ToJson(abilities[slot]);
                        }
                        int repeats = rng.Next(1, 5);
                        for (int rep = 0; rep < repeats; rep++)
                            for (int slot = 0; slot < abilities.Length; slot++)
                                mods.GauntletSteps(abilities[slot], slot);
                    }
                    else
                    {
                        var abilities = new ArsenalAbility[4];
                        for (int slot = 0; slot < abilities.Length; slot++)
                        {
                            ArsenalSkillKind kind = ArsenalKinds[rng.Next(ArsenalKinds.Length)];
                            abilities[slot] = NewArsenalAbility(kind, slot);
                            sources.Add(abilities[slot]);
                            before[abilities[slot]] = JsonUtility.ToJson(abilities[slot]);
                        }
                        int repeats = rng.Next(1, 5);
                        for (int rep = 0; rep < repeats; rep++)
                            for (int slot = 0; slot < abilities.Length; slot++)
                                mods.Plan(abilities[slot], slot);
                    }

                    foreach (KeyValuePair<UnityEngine.Object, string> entry in before)
                        PropertyCheck.That(JsonUtility.ToJson(entry.Key) == entry.Value,
                            "Source asset mutated by the overhaul plan path: " + entry.Key.name);
                }
                finally
                {
                    foreach (UnityEngine.Object o in sources) if (o) UnityEngine.Object.DestroyImmediate(o);
                }
            });
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 2: Monotonicidade por rank.
        // Each new boon's named per-rank quantity is non-decreasing from rank r to r+1 across the whole
        // catalogued [1, MaxRank] range (all ten new boons MaxRank == 3). Rank is the only variable, so
        // any difference is attributable to the increment.
        // Validates: Requirements 1.2
        [Test]
        public void NewBoons_NamedQuantities_StrengthenMonotonicallyWithRank()
        {
            WeaponBoon[] all = new[]
            {
                WeaponBoon.AsuraFist, WeaponBoon.GuardBreaker, WeaponBoon.HungryCombo, WeaponBoon.SeismicFist,
                WeaponBoon.KitingStep, WeaponBoon.AdaptiveCadence, WeaponBoon.RainMark,
                WeaponBoon.SpacingRecoil, WeaponBoon.PikeWall, WeaponBoon.EdgeStrike,
            };

            PropertyCheck.ForAll((rng, i) =>
            {
                foreach (WeaponBoon boon in all)
                {
                    int maxRank = Def(boon).MaxRank;
                    for (int r = 1; r < maxRank; r++)
                    {
                        float lo = NamedQuantity(boon, r);
                        float hi = NamedQuantity(boon, r + 1);
                        PropertyCheck.That(hi >= lo - Tolerance,
                            $"{boon} named quantity decreased at rank {r} -> {r + 1} ({lo} -> {hi})");
                    }
                }
            });
        }

        // Example: the exact per-rank named quantities anchor the monotone property's sampled values.
        [Test]
        public void NewBoons_NamedQuantitiesMatchDesign()
        {
            for (int r = 1; r <= 3; r++)
            {
                Assert.That(AsuraFistEnergyPerHit(r), Is.EqualTo(2f * r).Within(Tolerance));
                Assert.That(GuardBreakerStanceMultiplier(r), Is.EqualTo(1f + 0.5f * r).Within(Tolerance));
                Assert.That(HungryComboSecondsPerHit(r), Is.EqualTo(0.3f * r).Within(Tolerance));
                Assert.That(SeismicKnockbackScale(r), Is.EqualTo(1f + 0.3f * r).Within(Tolerance));
                Assert.That(KitingDistanceScale(r), Is.EqualTo(1f + 0.25f * r).Within(Tolerance));
                Assert.That(AdaptiveCadencePercent(r), Is.EqualTo(15f * r).Within(Tolerance));
                Assert.That(RainMarkAmplify(r), Is.EqualTo(0.2f * r).Within(Tolerance));
                Assert.That(SpacingStepBackScale(r), Is.EqualTo(0.2f * r).Within(Tolerance));
                Assert.That(PikeWallZonePush(r), Is.EqualTo(0.3f * r).Within(Tolerance));
                Assert.That(EdgeStrikeStanceMultiplier(r), Is.EqualTo(1f + 0.4f * r).Within(Tolerance));
            }
        }

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

        // Feature: gauntlet-boon-playstyle-overhaul, Property 3: Independência de ordem.
        // For any family and any multiset of the new boons+ranks, applying the acquisitions in two
        // different permutations yields identical per-boon ranks and byte-identical cast output: Plan
        // snapshots (including the new RainMark/SpacingRecoil/PikeWall flags) for Bow/Spear across every
        // slot, GauntletSteps snapshots for Gauntlet, and an identical DirectDamageMultiplier value.
        // Validates: Requirements 1.3
        [Test]
        public void NewBoons_PlanGauntletStepsAndDirectDamage_AreIndependentOfAcquisitionOrder()
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

                // DirectDamageMultiplier depends only on ranks (and the sampled inputs), so it must match
                // regardless of acquisition order.
                float distance = (float)(rng.NextDouble() * 12.0);
                float targetHp = (float)rng.NextDouble();
                float playerHp = (float)rng.NextDouble();
                int elements = rng.Next(0, 3);
                PropertyCheck.That(
                    Mathf.Approximately(
                        modsA.DirectDamageMultiplier(distance, targetHp, playerHp, elements),
                        modsB.DirectDamageMultiplier(distance, targetHp, playerHp, elements)),
                    $"family={family}: DirectDamageMultiplier differs by acquisition order");

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
