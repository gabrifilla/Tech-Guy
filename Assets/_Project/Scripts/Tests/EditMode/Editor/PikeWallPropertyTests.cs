using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 18 of gauntlet-boon-playstyle-overhaul — the Spear's
    /// "Muralha de hastes" (<c>PikeWall</c>, R12) control zone.
    ///
    /// Property 18 (design): for any PikeWall rank R &gt;= 1, the per-cast <c>ArsenalCastPlan</c> SHALL
    /// declare the control zone (<c>ControlZone == true</c>, <c>ZonePush == 0.3R</c>) and each caught
    /// enemy SHALL be displaced OUTWARD from the zone centre by a magnitude scaled by <c>1 + 0.3R</c>
    /// (snapshot-only). At rank 0 (no boon) the zone is off: <c>ControlZone == false</c>,
    /// <c>ZonePush == 0</c>, and the outward budget collapses to the un-scaled baseline.
    ///
    /// This is the snapshot-only half of R12: it covers the pure Cast_Plan declaration
    /// (<see cref="WeaponRunModifiers.Plan"/>, Spear slot W / Sweep) and the pure outward-push
    /// magnitude/direction contract the scene-side glide in <c>ArsenalCombat.ApplyZonePush</c> is
    /// built on — the total budget is <see cref="SpearSweepDisplacement.MaxTotalDisplacement"/> scaled
    /// by <c>1 + ZonePush</c>, the per-step cap is <see cref="SpearSweepDisplacement.MaxSpeed"/> scaled
    /// the same way, and the heading is the normalised centre-&gt;enemy vector (strictly outward). The
    /// locomotion-routed, scenery-respecting application (R12.3) is exercised in PlayMode
    /// (<c>PikeWallPlayModeTests</c>).
    ///
    /// FsCheck/CsCheck cannot be resolved on this machine, so the seeded <see cref="PropertyCheck"/>
    /// harness drives &gt;= 100 deterministic cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class PikeWallPropertyTests
    {
        private const int MaxRank = 3;             // R12.5: PikeWall is a Spear boon, MaxRank = 3
        private const float PushPerRank = 0.3f;    // R12.2: plan.ZonePush = 0.3 * rank
        private const float Tolerance = 1e-4f;
        private const int Slot1 = 1;               // spear W (Sweep) ability lives in slot 1

        // The spear W (Sweep) PikeWall branch in WeaponRunModifiers.Plan is gated on ability.Kind ==
        // Sweep, which is a private serialized field. We force it through the same reflection seam
        // MoonShardFragmentBoundTests uses, so no source asset is touched through a reward path.
        private static readonly FieldInfo KindField =
            typeof(ArsenalAbility).GetField("_kind", BindingFlags.Instance | BindingFlags.NonPublic);

        private static WeaponRunModifiers.Definition Def(WeaponBoon kind)
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Kind == kind) return definition;
            throw new InvalidOperationException("Missing catalog definition for " + kind);
        }

        // A fresh Sweep ArsenalAbility (the W). Kind is forced through the serialized _kind seam.
        // Never mutated by Plan (snapshot-only).
        private static ArsenalAbility NewSweepAbility()
        {
            Assert.IsNotNull(KindField, "Expected serialized field _kind on ArsenalAbility (test seam).");
            var ability = ScriptableObject.CreateInstance<ArsenalAbility>();
            ability.name = "TestSpearSweep";
            KindField.SetValue(ability, ArsenalSkillKind.Sweep);
            return ability;
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 18
        // For any PikeWall rank R, the slot-1 Sweep Cast_Plan declares the zone exactly when R >= 1
        // (ControlZone == R > 0) and ZonePush == 0.3R, non-decreasing in R, with the source asset
        // left unchanged (snapshot-only).
        // Validates: Requirements 12.1, 12.2
        [Test]
        public void SweepPlan_DeclaresControlZone_AndZonePushMatchesRank()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(0, MaxRank + 1); // 0 .. MaxRank

                var mods = new WeaponRunModifiers(RunWeaponFamily.Spear);
                for (int r = 0; r < rank; r++)
                    PropertyCheck.That(mods.Add(Def(WeaponBoon.PikeWall)),
                        "PikeWall rank " + (r + 1) + " should be addable");
                PropertyCheck.That(mods.Rank(WeaponBoon.PikeWall) == rank, "PikeWall rank mismatch");

                ArsenalAbility ability = NewSweepAbility();
                // Snapshot the getters Plan reads so we can prove the source asset is never mutated.
                ArsenalSkillKind kind0 = ability.Kind;
                float width0 = ability.Width, range0 = ability.Range, damage0 = ability.DamageMultiplier;

                ArsenalCastPlan plan = mods.Plan(ability, Slot1);

                // R12.1: the zone is declared exactly when the boon is acquired (rank >= 1).
                PropertyCheck.That(plan.ControlZone == (rank > 0),
                    $"rank={rank}: ControlZone={plan.ControlZone}, expected {rank > 0}");

                // R12.2: ZonePush is exactly 0.3 * rank (0 when off).
                float expectedPush = PushPerRank * rank;
                PropertyCheck.That(Mathf.Abs(plan.ZonePush - expectedPush) <= Tolerance,
                    $"rank={rank}: ZonePush={plan.ZonePush}, expected {expectedPush}");

                // Non-decreasing in rank: a higher rank never pushes less than a lower rank.
                for (int lower = 0; lower < rank; lower++)
                {
                    var lowerMods = new WeaponRunModifiers(RunWeaponFamily.Spear);
                    for (int r = 0; r < lower; r++) lowerMods.Add(Def(WeaponBoon.PikeWall));
                    ArsenalAbility lowerAbility = NewSweepAbility();
                    float lowerPush = lowerMods.Plan(lowerAbility, Slot1).ZonePush;
                    PropertyCheck.That(plan.ZonePush >= lowerPush - Tolerance,
                        $"ZonePush at rank {rank} ({plan.ZonePush}) < rank {lower} ({lowerPush}) — not monotonic");
                    UnityEngine.Object.DestroyImmediate(lowerAbility);
                }

                // Snapshot-only (R12.4): the source ability is never written back to.
                PropertyCheck.That(ability.Kind == kind0, "Kind mutated on source ability");
                PropertyCheck.That(ability.Width == width0, "Width mutated on source ability");
                PropertyCheck.That(ability.Range == range0, "Range mutated on source ability");
                PropertyCheck.That(ability.DamageMultiplier == damage0, "DamageMultiplier mutated on source ability");

                UnityEngine.Object.DestroyImmediate(ability);
            });
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 18
        // For a caught enemy at any offset from the zone centre, the outward push the plan drives points
        // OUTWARD (along the normalised centre->enemy heading) and its total budget is scaled by 1 + 0.3R:
        // budget = MaxTotalDisplacement * (1 + ZonePush), per-step cap = MaxSpeed * (1 + ZonePush) * dt.
        // This is the exact magnitude/direction contract ArsenalCombat.GlidePushedEnemy applies.
        // Validates: Requirements 12.1, 12.2
        [Test]
        public void OutwardPush_PointsAwayFromCentre_AndScalesByOnePlusPointThreeRank()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = 1 + rng.Next(0, MaxRank); // 1 .. MaxRank (Property 18 premise: R >= 1)
                float zonePush = PushPerRank * rank;
                float scale = 1f + zonePush;

                Vector3 center = SampleVector(rng, 15f);
                Vector3 enemy = center + SampleOutwardOffset(rng); // strictly off-centre
                float dt = 0.005f + (float)rng.NextDouble() * 0.045f; // 5..50 ms frame

                // Expected outward heading: normalised centre->enemy, flattened to the ground plane.
                Vector3 toEnemy = enemy - center; toEnemy.y = 0f;
                PropertyCheck.That(toEnemy.sqrMagnitude > 1e-6f, "degenerate offset generated");
                Vector3 outward = toEnemy.normalized;

                // Total outward budget scales by 1 + 0.3R (R12.2).
                float budget = SpearSweepDisplacement.MaxTotalDisplacement * scale;
                float baseline = SpearSweepDisplacement.MaxTotalDisplacement; // rank-0 would-be budget
                PropertyCheck.That(budget >= baseline - Tolerance,
                    $"rank={rank}: scaled budget {budget} < baseline {baseline}");
                PropertyCheck.That(Mathf.Abs(budget - SpearSweepDisplacement.MaxTotalDisplacement * (1f + PushPerRank * rank)) <= Tolerance,
                    $"rank={rank}: budget {budget} does not equal MaxTotal*(1+0.3R)");

                // First-step delta mirrors GlidePushedEnemy: min(speed cap * scale * dt, remaining budget).
                float step = Mathf.Min(SpearSweepDisplacement.MaxSpeed * scale * dt, budget);
                PropertyCheck.That(step > 0f, $"rank={rank}: first step must be positive");
                Vector3 delta = outward * step;

                // Direction: the delta points strictly outward (positive projection on the outward heading),
                // never inward toward the centre.
                float alongOutward = Vector3.Dot(delta, outward);
                PropertyCheck.That(alongOutward > 0f,
                    $"rank={rank}: push projection {alongOutward} is not outward");

                // The pushed point is farther from the centre than the enemy started (moved outward).
                float startDist = toEnemy.magnitude;
                Vector3 endPlanar = enemy + delta - center; endPlanar.y = 0f;
                PropertyCheck.That(endPlanar.magnitude >= startDist - Tolerance,
                    $"rank={rank}: enemy moved inward (start {startDist} -> end {endPlanar.magnitude})");

                // Monotonic in rank: a higher rank's scaled budget is never smaller than a lower rank's.
                for (int lower = 1; lower < rank; lower++)
                {
                    float lowerBudget = SpearSweepDisplacement.MaxTotalDisplacement * (1f + PushPerRank * lower);
                    PropertyCheck.That(budget >= lowerBudget - Tolerance,
                        $"budget at rank {rank} ({budget}) < rank {lower} ({lowerBudget}) — not monotonic");
                }
            });
        }

        private static Vector3 SampleVector(System.Random rng, float extent)
        {
            float x = ((float)rng.NextDouble() * 2f - 1f) * extent;
            float z = ((float)rng.NextDouble() * 2f - 1f) * extent;
            float y = ((float)rng.NextDouble() * 2f - 1f) * extent;
            return new Vector3(x, y, z);
        }

        // A non-degenerate planar offset from the centre: an angle plus a radius clear of zero, with a
        // random vertical component (which the planar push ignores).
        private static Vector3 SampleOutwardOffset(System.Random rng)
        {
            float angle = (float)rng.NextDouble() * Mathf.PI * 2f;
            float radius = 0.3f + (float)rng.NextDouble() * 4f; // 0.3 .. 4.3 m from the centre
            float y = ((float)rng.NextDouble() * 2f - 1f) * 2f;
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }
    }
}
