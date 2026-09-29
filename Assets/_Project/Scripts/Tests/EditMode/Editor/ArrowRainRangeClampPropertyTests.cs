using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the Arco's <b>Chuva de Flechas</b> (R) range clamp — task 12.6 of
    /// weapon-gameplay-swarm-rework (Property 35, Requirement 10.7).
    ///
    /// <para><b>Scope / scene-bound caveat.</b> The full R (Chuva de Flechas) flow in
    /// <c>ArsenalCombat.Execute</c> reads the cursor from the running scene:</para>
    /// <list type="bullet">
    /// <item><description>
    /// it raycasts the mouse against a ground plane (<c>Camera.main.ScreenPointToRay</c> +
    /// <c>new Plane(Vector3.up, origin).Raycast</c>) to obtain the cursor's ground point — this half
    /// needs a live <c>Camera.main</c>, an active <c>Mouse</c> device and a running scene, so it is
    /// <b>NOT</b> validated by this test; and
    /// </description></item>
    /// <item><description>
    /// it then clamps that ground point to the ability's existing range with
    /// <c>rainCenter = origin + Vector3.ClampMagnitude(cursorPoint - origin, plan.Range)</c> — this half
    /// is a pure, scene-independent decision (pure <see cref="Vector3.ClampMagnitude"/>) and is exactly
    /// what this test property-checks.
    /// </description></item>
    /// </list>
    ///
    /// <para>
    /// The clamp is inline in the <c>ArsenalCombat</c> MonoBehaviour coroutine (there is no pure
    /// <c>ArrowRainClamp</c> helper to import, and source files must not be edited for this task), so —
    /// like the sibling range/clamp property tests — this test mirrors the exact clamp expression from
    /// that coroutine to property-check the invariant that IS pure: for any cursor ground point and any
    /// authored <c>plan.Range</c>, the clamped rain center stays within <c>plan.Range</c> of the origin
    /// and lies along the origin → cursor direction.
    /// </para>
    ///
    /// <para>
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property (min 128)
    /// and reports the exact failing case as a counterexample.
    /// </para>
    /// </summary>
    public sealed class ArrowRainRangeClampPropertyTests
    {
        private const float Epsilon = 1e-3f;

        // Feature: weapon-gameplay-swarm-rework, Property 35: Chuva de Flechas respeita o alcance
        // Para toda posição de cursor, o centro da Chuva de Flechas (R do Arco) é clampado dentro do
        // alcance existente da habilidade.
        //
        // Pure, scene-independent decision validated here: for any origin, any cursor ground point and
        // any authored plan.Range (>= 0), the rain center computed exactly as ArsenalCombat does —
        // rainCenter = origin + Vector3.ClampMagnitude(cursorPoint - origin, plan.Range) — is at most
        // plan.Range away from the origin and lies along the origin -> cursor direction (never opposite,
        // never off-axis).
        //
        // Scene-bound half NOT validated here (needs a live Camera.main, a Mouse device and a running
        // scene): the mouse -> ground-plane raycast that produces the cursor point fed into the clamp.
        // Validates: Requirements 10.7
        [Test]
        public void RainCenterIsClampedWithinRangeAlongCursorDirection()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                Vector3 origin = RandomPoint(rng);
                Vector3 cursorPoint = RandomCursorPoint(rng, origin);
                float range = RandomRange(rng);

                // Exact clamp expression from ArsenalCombat.Execute (Requisito 10.7).
                Vector3 rainCenter = origin + Vector3.ClampMagnitude(cursorPoint - origin, range);

                Vector3 fromOrigin = rainCenter - origin;
                float distance = fromOrigin.magnitude;

                string ctx =
                    $"origin={origin} cursorPoint={cursorPoint} range={range} rainCenter={rainCenter} distance={distance}";

                // 1) The rain center never sits farther than plan.Range from the origin.
                PropertyCheck.That(distance <= range + Epsilon,
                    $"{ctx}: rain center is {distance} from origin, beyond the ability range {range}.");

                Vector3 toCursor = cursorPoint - origin;
                float cursorDistance = toCursor.magnitude;

                if (cursorDistance <= range + Epsilon)
                {
                    // Inside (or on) the range: ClampMagnitude leaves the point untouched, so the rain
                    // center coincides with the cursor point.
                    PropertyCheck.That((rainCenter - cursorPoint).magnitude <= Epsilon,
                        $"{ctx}: cursor within range but rain center was moved off the cursor point.");
                }
                else
                {
                    // Outside the range: the point is pulled onto the range boundary, so the rain center
                    // is exactly plan.Range from the origin.
                    PropertyCheck.That(Mathf.Abs(distance - range) <= Mathf.Max(Epsilon, range * Epsilon),
                        $"{ctx}: cursor beyond range but rain center is not on the range boundary (distance {distance} vs range {range}).");

                    // And it stays along the origin -> cursor direction (same heading, never opposite,
                    // never off-axis): the clamped vector is a non-negative scalar multiple of the
                    // origin -> cursor vector.
                    Vector3 cursorDir = toCursor / cursorDistance;
                    float projection = Vector3.Dot(fromOrigin, cursorDir);
                    PropertyCheck.That(projection >= -Epsilon,
                        $"{ctx}: clamped rain center points away from the cursor direction (projection {projection}).");

                    Vector3 offAxis = fromOrigin - cursorDir * projection;
                    PropertyCheck.That(offAxis.magnitude <= Mathf.Max(Epsilon, range * Epsilon),
                        $"{ctx}: clamped rain center drifted off the origin -> cursor axis (off-axis {offAxis.magnitude}).");
                }
            });
        }

        // Focused sub-property: the clamp is idempotent and monotone in range. Re-clamping an already
        // clamped rain center to the same range does not move it, and a larger range never pulls the
        // rain center closer to the origin than a smaller range would (Requirement 10.7 upper bound is
        // the authored range, and clamping only ever shortens an out-of-range cursor vector).
        // Feature: weapon-gameplay-swarm-rework, Property 35: Chuva de Flechas respeita o alcance
        // Validates: Requirements 10.7
        [Test]
        public void RainCenterClampIsIdempotentAndMonotoneInRange()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                Vector3 origin = RandomPoint(rng);
                Vector3 cursorPoint = RandomCursorPoint(rng, origin);
                float smallRange = RandomRange(rng);
                float largeRange = smallRange + RandomRange(rng); // >= smallRange

                Vector3 clampedSmall = origin + Vector3.ClampMagnitude(cursorPoint - origin, smallRange);
                Vector3 clampedLarge = origin + Vector3.ClampMagnitude(cursorPoint - origin, largeRange);

                string ctx =
                    $"origin={origin} cursorPoint={cursorPoint} smallRange={smallRange} largeRange={largeRange} " +
                    $"clampedSmall={clampedSmall} clampedLarge={clampedLarge}";

                // Idempotence: clamping the already clamped small-range center to the same range is a no-op.
                Vector3 reclamped = origin + Vector3.ClampMagnitude(clampedSmall - origin, smallRange);
                PropertyCheck.That((reclamped - clampedSmall).magnitude <= Mathf.Max(Epsilon, smallRange * Epsilon),
                    $"{ctx}: re-clamping to the same range moved the rain center ({reclamped}).");

                // Monotonicity: the wider range never places the rain center closer to the origin than the
                // narrower range does (a larger allowance can only keep or extend the reach to the cursor).
                float distSmall = (clampedSmall - origin).magnitude;
                float distLarge = (clampedLarge - origin).magnitude;
                PropertyCheck.That(distLarge >= distSmall - Mathf.Max(Epsilon, largeRange * Epsilon),
                    $"{ctx}: larger range produced a nearer rain center (distLarge={distLarge} < distSmall={distSmall}).");

                // Both still respect their own range ceiling.
                PropertyCheck.That(distSmall <= smallRange + Epsilon,
                    $"{ctx}: small-range rain center exceeds its range.");
                PropertyCheck.That(distLarge <= largeRange + Epsilon,
                    $"{ctx}: large-range rain center exceeds its range.");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Ability ranges spanning zero, tiny, typical, and large authored values (ArsenalAbility clamps >= 0).</summary>
        private static float RandomRange(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;                                   // exactly zero (no reach)
                case 1: return (float)rng.NextDouble() * 0.5f;       // tiny range
                case 2: return 0.5f + (float)rng.NextDouble() * 8f;  // typical range
                default: return 8f + (float)rng.NextDouble() * 24f;  // large range
            }
        }

        /// <summary>
        /// A cursor ground point relative to the origin: sometimes inside the plausible range, sometimes
        /// far outside it, occasionally coincident with the origin, so both clamp branches are exercised.
        /// </summary>
        private static Vector3 RandomCursorPoint(System.Random rng, Vector3 origin)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return origin; // coincident with origin -> zero-length vector
                case 1: return origin + RandomDirection(rng) * ((float)rng.NextDouble() * 6f);   // near
                case 2: return origin + RandomDirection(rng) * (6f + (float)rng.NextDouble() * 40f); // far
                default:
                    // Fully arbitrary offset in a wide box (may be any distance/heading).
                    return origin + new Vector3(
                        ((float)rng.NextDouble() - 0.5f) * 100f,
                        ((float)rng.NextDouble() - 0.5f) * 100f,
                        ((float)rng.NextDouble() - 0.5f) * 100f);
            }
        }

        private static Vector3 RandomPoint(System.Random rng)
        {
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 60f,
                ((float)rng.NextDouble() - 0.5f) * 20f,
                ((float)rng.NextDouble() - 0.5f) * 60f);
        }

        private static Vector3 RandomDirection(System.Random rng)
        {
            var dir = new Vector3(
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f,
                (float)rng.NextDouble() - 0.5f);
            return dir.sqrMagnitude < 1e-6f ? Vector3.forward : dir.normalized;
        }
    }
}
