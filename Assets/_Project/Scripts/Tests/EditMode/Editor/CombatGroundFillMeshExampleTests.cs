using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example tests for <see cref="CombatGroundFill"/> — task 6.2 of
    /// ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// These validate Property 12 (filled telegraph is confined to the attack area) by EXAMPLE
    /// rather than by generated cases: for the Circle, Ring and Cone shapes we build the fill mesh
    /// via the real <see cref="CombatGroundFill.Create"/>/<see cref="CombatGroundFill.SetArea"/>
    /// MonoBehaviour API, read the generated geometry back off the <see cref="MeshFilter"/>, and
    /// assert the mesh is non-degenerate and that every generated point lands inside the matching
    /// <see cref="EnemyAttackArea"/> (the same shape the damage test consults), with none outside.
    ///
    /// The fill lifts every vertex by a small ground offset on Y so it renders just beneath the ring
    /// outline; <see cref="EnemyAttackArea.Contains"/> tolerates that lift, but these tests also strip
    /// it before sampling so the containment check exercises the pure XZ shape, not the lift tolerance.
    ///
    /// A live scene is not required: <see cref="CombatGroundFill.Create"/> only spins up a GameObject
    /// with a <see cref="MeshFilter"/>/<see cref="MeshRenderer"/>, which EditMode supports. Each test
    /// destroys the helper it creates so no scene objects leak between cases.
    /// </summary>
    public sealed class CombatGroundFillMeshExampleTests
    {
        private const float GroundLift = 0.05f; // mirrors CombatGroundFill.GroundLift
        private const float Tolerance = 1e-3f;

        // Interior samples are nudged inward from the shape edge so a point generated right on the
        // boundary (which Contains may treat as inside or outside depending on float rounding) does
        // not create a brittle test.
        private const float InteriorEpsilon = 0.02f;

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 12
        // Filled telegraph is confined to the attack area (Circle).
        // At full progress the circle fan fills the whole disc: a center apex plus a rim of segment
        // points at the exact reach. Every mesh vertex and every sampled interior point must satisfy
        // EnemyAttackArea.Contains, and sampled points beyond the reach must not.
        // Validates: Requirements 6.1, 6.3, 6.4, 6.5, 7.1
        [Test]
        public void CircleFill_AtFullProgress_IsConfinedToAttackArea()
        {
            var area = new EnemyAttackArea(EnemyAttackShape.Circle, Vector3.zero, Vector3.forward, reach: 5f);
            var fill = CombatGroundFill.Create(null, "CircleFill", Color.red);
            try
            {
                fill.SetArea(area, 1f);
                Mesh mesh = ReadMesh(fill);

                // A circle fan is a center apex + a rim of (Segments + 1) points, triangulated as a fan.
                Assert.That(mesh.vertexCount, Is.GreaterThan(3), "Circle fill should produce a real fan mesh.");
                Assert.That(mesh.triangles.Length % 3, Is.EqualTo(0), "Triangle index count must be a multiple of 3.");
                Assert.That(mesh.triangles.Length, Is.GreaterThan(0), "Circle fill must have at least one triangle.");

                // Every generated vertex lies inside the area (the rim sits on the reach boundary).
                AssertAllVerticesInside(mesh, area);

                // The rim reaches the full radius: bounds extent (XZ) matches the reach at progress 1 (R6.4).
                AssertBoundsReach(mesh, area.Center, area.Reach);

                // Interior samples (center and mid-radius in several directions) are inside.
                Assert.That(area.Contains(area.Center), Is.True, "Circle center must be inside the filled disc.");
                foreach (var dir in CardinalDirections())
                {
                    Vector3 interior = area.Center + dir * (area.Reach * 0.5f);
                    Assert.That(area.Contains(interior), Is.True,
                        $"Mid-radius point {interior} should be inside the circle.");
                }

                // Nothing beyond the reach is inside.
                foreach (var dir in CardinalDirections())
                {
                    Vector3 outside = area.Center + dir * (area.Reach + 1f);
                    Assert.That(area.Contains(outside), Is.False,
                        $"Point {outside} beyond the reach must be outside the circle.");
                }
            }
            finally { Cleanup(fill); }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 12
        // Filled telegraph is confined to the attack area (Ring).
        // The ring mesh is a band between the fixed inner radius and the outer edge; at full progress
        // the outer edge reaches the full reach. The hollow center must stay OUTSIDE the area, every
        // band vertex must be inside, and points past the outer edge must be outside.
        // Validates: Requirements 6.1, 6.3, 6.4, 6.5, 7.1
        [Test]
        public void RingFill_AtFullProgress_IsConfinedToAnnulus()
        {
            var area = new EnemyAttackArea(EnemyAttackShape.Ring, Vector3.zero, Vector3.forward, reach: 11f, innerRadius: 4f);
            var fill = CombatGroundFill.Create(null, "RingFill", Color.red);
            try
            {
                fill.SetArea(area, 1f);
                Mesh mesh = ReadMesh(fill);

                Assert.That(mesh.vertexCount, Is.GreaterThan(3), "Ring fill should produce a band mesh.");
                Assert.That(mesh.triangles.Length % 3, Is.EqualTo(0), "Triangle index count must be a multiple of 3.");
                Assert.That(mesh.triangles.Length, Is.GreaterThan(0), "Ring fill must have at least one triangle.");

                // Every generated vertex lies on or between the inner and outer radii (all inside the annulus).
                AssertAllVerticesInside(mesh, area);

                // The outer edge reaches the full reach at progress 1 (R6.4).
                AssertBoundsReach(mesh, area.Center, area.Reach);

                // The hollow center is OUTSIDE the ring (confinement to the annulus, R6.3).
                Assert.That(area.Contains(area.Center), Is.False, "Ring center must be hollow (outside the area).");

                // A point inside the inner radius is outside; a mid-band point is inside; past the reach is outside.
                foreach (var dir in CardinalDirections())
                {
                    Vector3 insideHole = area.Center + dir * (area.InnerRadius - InteriorEpsilon - 0.5f);
                    Assert.That(area.Contains(insideHole), Is.False,
                        $"Point {insideHole} inside the inner radius must be outside the ring.");

                    float mid = (area.InnerRadius + area.Reach) * 0.5f;
                    Vector3 band = area.Center + dir * mid;
                    Assert.That(area.Contains(band), Is.True,
                        $"Mid-band point {band} should be inside the ring.");

                    Vector3 outside = area.Center + dir * (area.Reach + 1f);
                    Assert.That(area.Contains(outside), Is.False,
                        $"Point {outside} beyond the outer edge must be outside the ring.");
                }
            }
            finally { Cleanup(fill); }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 12
        // Filled telegraph is confined to the attack area (Cone).
        // The cone fan sweeps the authored angle in front of the origin. Every mesh vertex must be
        // inside the cone, mid-arc interior samples inside, and flank/back points outside (R6.3).
        // Validates: Requirements 6.1, 6.3, 6.4, 6.5, 7.1
        [Test]
        public void ConeFill_AtFullProgress_IsConfinedToWedge()
        {
            var area = new EnemyAttackArea(EnemyAttackShape.Cone, Vector3.zero, Vector3.forward, reach: 4f, angle: 110f);
            var fill = CombatGroundFill.Create(null, "ConeFill", Color.red);
            try
            {
                fill.SetArea(area, 1f);
                Mesh mesh = ReadMesh(fill);

                Assert.That(mesh.vertexCount, Is.GreaterThan(3), "Cone fill should produce a fan mesh.");
                Assert.That(mesh.triangles.Length % 3, Is.EqualTo(0), "Triangle index count must be a multiple of 3.");
                Assert.That(mesh.triangles.Length, Is.GreaterThan(0), "Cone fill must have at least one triangle.");

                // Every generated vertex lies inside the cone (apex at origin, arc on the reach).
                AssertAllVerticesInside(mesh, area);

                // Points straight ahead inside the reach are inside; flanks and back are outside (R6.3).
                Assert.That(area.Contains(area.Forward * (area.Reach * 0.5f)), Is.True,
                    "A point along the cone axis within reach should be inside.");
                Assert.That(area.Contains(Vector3.back), Is.False, "Behind the cone must be outside.");
                Assert.That(area.Contains(Vector3.right * 2f), Is.False, "A flank point must be outside the 110-degree cone.");
                Assert.That(area.Contains(area.Forward * (area.Reach + 1f)), Is.False,
                    "A point beyond the reach along the axis must be outside.");
            }
            finally { Cleanup(fill); }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 12
        // Zero-windup single-frame fill: a telegraph with no windup paints its full area in one frame
        // at Fill_Progress == 1 (R6.5). The resulting mesh is identical in extent to the full-progress
        // circle built incrementally, so the single-frame fill is still confined to the area.
        // Validates: Requirements 6.1, 6.3, 6.4, 6.5, 7.1
        [Test]
        public void ZeroWindup_SingleFrameFillAtProgressOne_FillsFullArea()
        {
            var area = new EnemyAttackArea(EnemyAttackShape.Circle, new Vector3(3f, 1f, -2f), Vector3.forward, reach: 6f);
            var fill = CombatGroundFill.Create(null, "ZeroWindupFill", Color.red);
            try
            {
                // A zero-windup attack jumps straight to full progress in a single frame.
                fill.SetArea(area, 1f);
                Mesh mesh = ReadMesh(fill);

                Assert.That(mesh.vertexCount, Is.GreaterThan(3),
                    "A single-frame full fill must still generate the complete mesh.");
                AssertAllVerticesInside(mesh, area);
                AssertBoundsReach(mesh, area.Center, area.Reach);
            }
            finally { Cleanup(fill); }
        }

        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 12
        // Companion guard: zero progress paints nothing (an empty mesh), so a cleared/not-yet-started
        // telegraph never leaks geometry outside — or inside — the area.
        // Validates: Requirements 6.3, 6.5
        [Test]
        public void ZeroProgress_ProducesEmptyMesh()
        {
            var area = new EnemyAttackArea(EnemyAttackShape.Circle, Vector3.zero, Vector3.forward, reach: 5f);
            var fill = CombatGroundFill.Create(null, "EmptyFill", Color.red);
            try
            {
                fill.SetArea(area, 0f);
                Mesh mesh = ReadMesh(fill);
                Assert.That(mesh.vertexCount, Is.EqualTo(0), "Zero progress should produce no geometry.");
            }
            finally { Cleanup(fill); }
        }

        // --- Helpers ---------------------------------------------------------------------------

        private static Mesh ReadMesh(CombatGroundFill fill)
        {
            var filter = fill.GetComponent<MeshFilter>();
            Assert.That(filter, Is.Not.Null, "CombatGroundFill must expose a MeshFilter.");
            Assert.That(filter.sharedMesh, Is.Not.Null, "CombatGroundFill must build a mesh.");
            return filter.sharedMesh;
        }

        // Asserts every mesh vertex, with the render-only ground lift stripped off its Y, is confined
        // to the attack area (the same shape the damage test uses). None may fall outside.
        //
        // The fill's rim/arc vertices are generated exactly on the shape boundary (reach radius,
        // inner radius, cone edge angle). Contains uses >= / <= boundary tests, so a boundary point
        // counts as inside; but the mesh and Contains compute that boundary through different float
        // paths (Quaternion.AngleAxis vs delta.normalized), which can disagree by a sub-millimeter
        // amount. We assert a point generated by the fill is never meaningfully OUTSIDE the area by
        // allowing a small boundary tolerance, so the "confined to the area" guarantee stays honest
        // while absorbing float rounding on the exact edge.
        private static void AssertAllVerticesInside(Mesh mesh, in EnemyAttackArea area)
        {
            Vector3[] verts = mesh.vertices;
            Assert.That(verts.Length, Is.GreaterThan(0), "Mesh must contain vertices.");
            foreach (var v in verts)
            {
                Vector3 grounded = v;
                grounded.y -= GroundLift; // undo the render-only lift so we test the pure XZ shape
                Assert.That(IsWithinArea(area, grounded), Is.True,
                    $"Fill vertex {v} (grounded {grounded}) escaped the attack area.");
            }
        }

        // Boundary-tolerant containment: a point is accepted if Contains says so, or if it sits on an
        // edge within InteriorEpsilon of the shape boundary (radius / inner radius / cone edge angle).
        // This never accepts a point that is genuinely outside the shape by more than the epsilon.
        private static bool IsWithinArea(in EnemyAttackArea area, Vector3 point)
        {
            if (area.Contains(point)) return true;

            Vector3 delta = point - area.Center;
            delta.y = 0f;
            float dist = delta.magnitude;

            switch (area.Shape)
            {
                case EnemyAttackShape.Circle:
                    return dist <= area.Reach + InteriorEpsilon;
                case EnemyAttackShape.Ring:
                    return dist <= area.Reach + InteriorEpsilon &&
                           dist >= area.InnerRadius - InteriorEpsilon;
                case EnemyAttackShape.Cone:
                    if (dist > area.Reach + InteriorEpsilon) return false;
                    if (dist < 1e-4f) return true; // apex
                    float cosHalf = Mathf.Cos((area.Angle * 0.5f) * Mathf.Deg2Rad);
                    float dot = Vector3.Dot(delta / dist, area.Forward);
                    return dot >= cosHalf - 1e-4f;
                default:
                    return false;
            }
        }

        // Asserts the mesh's XZ bounding box reaches (but does not exceed) the shape's full reach,
        // confirming that at progress 1 the fill matches the area's exact extent (R6.4).
        private static void AssertBoundsReach(Mesh mesh, Vector3 center, float reach)
        {
            Bounds b = mesh.bounds; // local == world here (identity transform, world-space verts)
            float maxX = Mathf.Max(Mathf.Abs(b.max.x - center.x), Mathf.Abs(b.min.x - center.x));
            float maxZ = Mathf.Max(Mathf.Abs(b.max.z - center.z), Mathf.Abs(b.min.z - center.z));
            float extent = Mathf.Max(maxX, maxZ);
            Assert.That(extent, Is.LessThanOrEqualTo(reach + Tolerance),
                "Fill must not extend beyond the shape's reach.");
            Assert.That(extent, Is.GreaterThanOrEqualTo(reach - 0.1f),
                "At full progress the fill should reach the shape's full extent.");
        }

        private static Vector3[] CardinalDirections() => new[]
        {
            Vector3.forward, Vector3.back, Vector3.left, Vector3.right,
            (Vector3.forward + Vector3.right).normalized,
            (Vector3.forward + Vector3.left).normalized,
        };

        private static void Cleanup(CombatGroundFill fill)
        {
            if (fill) Object.DestroyImmediate(fill.gameObject);
        }
    }
}
