using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure Sweet Spot evaluator in <see cref="SweetSpot"/> /
    /// <see cref="SweetSpotConfig"/> — task 11.2 of weapon-gameplay-swarm-rework.
    ///
    /// The Sweet Spot bonus is otherwise wired into <c>ArsenalCombat</c>; the "a direct tip hit is
    /// always rewarded strictly more than an equivalent hit outside the tip" invariant (Property 31,
    /// Requisito 9.2) was extracted into the plain <see cref="SweetSpot"/> collaborator so it can be
    /// property-checked without a live Unity scene. This project cannot resolve FsCheck/CsCheck
    /// packages on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives
    /// &gt;= 100 deterministic generated cases per property (min 128) and reports the exact failing
    /// case as a counterexample.
    /// </summary>
    public sealed class SweetSpotStrictlyGreaterBonusPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 31: Bônus do Sweet Spot estritamente maior
        // Para todo acerto direto de estocada, o bônus de dano de postura e de recurso concedido dentro
        // da região do Sweet_Spot é estritamente maior do que o concedido por um acerto equivalente fora
        // do Sweet_Spot. Com uma config válida (bônus estritamente > 1), um acerto na ponta devolve
        // StanceMultiplier e ResourceMultiplier estritamente maiores que os de um acerto fora (que são
        // exatamente 1.0). Gera pontos de acerto dentro e fora da região da ponta com geometria de
        // estocada arbitrária.
        // Validates: Requirements 9.2
        [Test]
        public void TipHitBonusIsStrictlyGreaterThanAnEquivalentHitOutsideTheSweetSpot()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- a VALID Sweet Spot config: both bonuses strictly > 1 (Requisito 9.2) ----------
                SweetSpotConfig config = RandomValidConfig(rng);
                var sweetSpot = new SweetSpot(config);

                PropertyCheck.That(config.IsValid,
                    $"generated config was expected to be valid: tip={config.TipFraction} " +
                    $"lateral={config.LateralRadius} stance={config.StanceBonus} resource={config.ResourceBonus}");

                // ---- arbitrary thrust geometry --------------------------------------------------
                Vector3 origin = RandomPoint(rng);
                Vector3 direction = RandomDirection(rng);
                float reach = 0.5f + (float)rng.NextDouble() * 10f; // 0.5 .. 10.5 m, always positive

                Vector3 axis = direction.normalized;

                // ---- an INSIDE hit: on the tip region of the axis, within the lateral radius -----
                Vector3 inside = InsideTipHit(rng, origin, axis, reach, config);
                PropertyCheck.That(sweetSpot.IsInsideTipRegion(inside, origin, direction, reach),
                    $"[case #{i}] generated inside hit {inside} was not classified inside the tip region " +
                    $"(origin={origin} dir={direction} reach={reach} tip={config.TipFraction} lateral={config.LateralRadius})");

                SweetSpot.Result insideResult = sweetSpot.Evaluate(inside, origin, direction, reach);

                // The inside hit is a Sweet Spot hit and carries the configured bonus multipliers.
                PropertyCheck.That(insideResult.IsSweetSpot,
                    $"inside hit {inside} did not evaluate as a Sweet Spot hit");
                PropertyCheck.That(Mathf.Approximately(insideResult.StanceMultiplier, config.StanceBonus),
                    $"inside StanceMultiplier {insideResult.StanceMultiplier} != configured {config.StanceBonus}");
                PropertyCheck.That(Mathf.Approximately(insideResult.ResourceMultiplier, config.ResourceBonus),
                    $"inside ResourceMultiplier {insideResult.ResourceMultiplier} != configured {config.ResourceBonus}");

                // ---- an OUTSIDE hit: an equivalent hit off the tip region ------------------------
                Vector3 outside = OutsideTipHit(rng, origin, axis, reach, config);
                PropertyCheck.That(!sweetSpot.IsInsideTipRegion(outside, origin, direction, reach),
                    $"[case #{i}] generated outside hit {outside} was unexpectedly classified inside the tip region " +
                    $"(origin={origin} dir={direction} reach={reach} tip={config.TipFraction} lateral={config.LateralRadius})");

                SweetSpot.Result outsideResult = sweetSpot.Evaluate(outside, origin, direction, reach);

                // An outside hit gets the neutral baseline of exactly 1.0 on both channels.
                PropertyCheck.That(!outsideResult.IsSweetSpot,
                    $"outside hit {outside} unexpectedly evaluated as a Sweet Spot hit");
                PropertyCheck.That(outsideResult.StanceMultiplier == SweetSpotConfig.OutsideMultiplier,
                    $"outside StanceMultiplier {outsideResult.StanceMultiplier} != baseline {SweetSpotConfig.OutsideMultiplier}");
                PropertyCheck.That(outsideResult.ResourceMultiplier == SweetSpotConfig.OutsideMultiplier,
                    $"outside ResourceMultiplier {outsideResult.ResourceMultiplier} != baseline {SweetSpotConfig.OutsideMultiplier}");

                // ---- Property 31: the inside bonus is STRICTLY greater than the outside bonus -----
                PropertyCheck.That(insideResult.StanceMultiplier > outsideResult.StanceMultiplier,
                    $"stance bonus not strictly greater inside ({insideResult.StanceMultiplier}) than outside " +
                    $"({outsideResult.StanceMultiplier})");
                PropertyCheck.That(insideResult.ResourceMultiplier > outsideResult.ResourceMultiplier,
                    $"resource bonus not strictly greater inside ({insideResult.ResourceMultiplier}) than outside " +
                    $"({outsideResult.ResourceMultiplier})");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// A valid Sweet Spot config: a positive tip fraction (never the whole reach, so an outside
        /// region always exists) and both bonuses strictly above the 1.0 baseline.
        /// </summary>
        private static SweetSpotConfig RandomValidConfig(System.Random rng)
        {
            // Keep the tip fraction in (0,1) with a margin so a distinct outside region always remains.
            float tipFraction = 0.1f + (float)rng.NextDouble() * 0.7f;   // 0.1 .. 0.8
            float lateralRadius = (float)rng.NextDouble() * 2f;           // 0 .. 2 m
            float stanceBonus = 1f + 0.05f + (float)rng.NextDouble() * 3f; // strictly > 1
            float resourceBonus = 1f + 0.05f + (float)rng.NextDouble() * 3f;
            return new SweetSpotConfig(tipFraction, lateralRadius, stanceBonus, resourceBonus);
        }

        /// <summary>
        /// A hit that lands inside the tip region: its projection onto the axis is within the trailing
        /// tip fraction of the reach and its off-axis distance is within the lateral radius.
        /// </summary>
        private static Vector3 InsideTipHit(
            System.Random rng, Vector3 origin, Vector3 axis, float reach, SweetSpotConfig config)
        {
            float regionStart = reach * (1f - config.TipFraction);
            // Along the axis, strictly within [regionStart, reach].
            float along = Mathf.Lerp(regionStart, reach, (float)rng.NextDouble());

            // Off-axis by up to the lateral radius (leave a tiny margin below the bound).
            float perpMax = Mathf.Max(0f, config.LateralRadius - 1e-3f);
            float perp = perpMax * (float)rng.NextDouble();

            return origin + axis * along + PerpendicularTo(rng, axis) * perp;
        }

        /// <summary>
        /// A hit that lands outside the tip region. Alternates between the three ways to be outside:
        /// behind the tip region along the axis, past the tip, or too far off-axis.
        /// </summary>
        private static Vector3 OutsideTipHit(
            System.Random rng, Vector3 origin, Vector3 axis, float reach, SweetSpotConfig config)
        {
            float regionStart = reach * (1f - config.TipFraction);
            Vector3 perpDir = PerpendicularTo(rng, axis);

            switch (rng.Next(0, 3))
            {
                case 0:
                {
                    // Before the tip region along the axis (0 .. regionStart, exclusive of the boundary).
                    float along = regionStart * (float)rng.NextDouble() * 0.99f;
                    float perp = Mathf.Max(0f, config.LateralRadius) * (float)rng.NextDouble();
                    return origin + axis * along + perpDir * perp;
                }
                case 1:
                {
                    // Past the tip (beyond the reach), inside the lateral band.
                    float along = reach + 0.1f + (float)rng.NextDouble() * 5f;
                    float perp = Mathf.Max(0f, config.LateralRadius) * (float)rng.NextDouble();
                    return origin + axis * along + perpDir * perp;
                }
                default:
                {
                    // Within the tip band along the axis, but too far off-axis to count.
                    float along = Mathf.Lerp(regionStart, reach, (float)rng.NextDouble());
                    float perp = config.LateralRadius + 0.1f + (float)rng.NextDouble() * 5f;
                    return origin + axis * along + perpDir * perp;
                }
            }
        }

        private static Vector3 RandomPoint(System.Random rng)
        {
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 40f,
                ((float)rng.NextDouble() - 0.5f) * 40f,
                ((float)rng.NextDouble() - 0.5f) * 40f);
        }

        private static Vector3 RandomDirection(System.Random rng)
        {
            var dir = new Vector3(
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f);
            return dir.sqrMagnitude < 1e-6f ? Vector3.forward : dir.normalized;
        }

        /// <summary>A unit vector perpendicular to <paramref name="axis"/>.</summary>
        private static Vector3 PerpendicularTo(System.Random rng, Vector3 axis)
        {
            // Cross with a reference that is not parallel to the axis to get a perpendicular direction.
            Vector3 reference = Mathf.Abs(Vector3.Dot(axis, Vector3.up)) > 0.9f ? Vector3.right : Vector3.up;
            Vector3 perp = Vector3.Cross(axis, reference);
            if (perp.sqrMagnitude < 1e-6f) perp = Vector3.Cross(axis, Vector3.forward);
            perp = perp.normalized;

            // Rotate the perpendicular around the axis by a random angle so hits sample the full disc.
            float angle = (float)rng.NextDouble() * 360f;
            return Quaternion.AngleAxis(angle, axis) * perp;
        }
    }
}
