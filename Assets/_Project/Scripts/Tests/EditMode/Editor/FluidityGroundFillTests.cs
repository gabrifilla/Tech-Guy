using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    /// <summary>New regressions for a fill reused across frames, shapes and clear/restart boundaries.</summary>
    public sealed class FluidityGroundFillTests
    {
        private const float GroundLift = .05f;
        private const float BoundaryTolerance = .02f;

        [Test]
        public void ReusedFill_HandlesShapeChangesAndClearWithoutStaleGeometry()
        {
            var fill = CombatGroundFill.Create(null, "Fluidity reusable fill", Color.red);
            try
            {
                var circle = new EnemyAttackArea(EnemyAttackShape.Circle, Vector3.zero, Vector3.forward, 4f);
                fill.SetArea(circle, .25f);
                AssertBoundsReach(ReadMesh(fill), circle.Center, 1f);
                fill.SetArea(circle, 1f);
                AssertAllVerticesInside(ReadMesh(fill), circle);
                AssertBoundsReach(ReadMesh(fill), circle.Center, 4f);

                var ring = new EnemyAttackArea(EnemyAttackShape.Ring, new Vector3(3, 0, 2), Vector3.forward, 6f, innerRadius: 2f);
                fill.SetArea(ring, 1f);
                AssertAllVerticesInside(ReadMesh(fill), ring);
                AssertBoundsReach(ReadMesh(fill), ring.Center, 6f);

                var lane = new EnemyAttackArea(EnemyAttackShape.Lane, new Vector3(-2, 0, 3), Vector3.right, 5f, width: 2f);
                fill.SetArea(lane, 1f);
                Mesh mesh = ReadMesh(fill);
                Assert.AreEqual(4, mesh.vertexCount);
                Assert.AreEqual(6, mesh.triangles.Length);
                foreach (var vertex in mesh.vertices) Assert.IsTrue(lane.Contains(vertex));

                fill.Clear();
                Assert.AreEqual(0, ReadMesh(fill).vertexCount);
                fill.SetArea(lane, .5f);
                Assert.AreEqual(4, ReadMesh(fill).vertexCount);
                Assert.AreEqual(6, ReadMesh(fill).triangles.Length);
                Assert.AreEqual(2.5f, ReadMesh(fill).bounds.size.x, .001f);

                var cone = new EnemyAttackArea(EnemyAttackShape.Cone, Vector3.zero, Vector3.forward, 3f, angle: 70f);
                fill.SetArea(cone, 1f);
                AssertAllVerticesInside(ReadMesh(fill), cone);
                fill.SetArea(cone, float.NaN);
                Assert.AreEqual(0, ReadMesh(fill).vertexCount, "Invalid progress cannot leave a stale warning.");
                fill.SetArea(circle, 1f);
                AssertBoundsReach(ReadMesh(fill), circle.Center, 4f);
            }
            finally { Cleanup(fill); }
        }

        private static Mesh ReadMesh(CombatGroundFill fill)
        {
            var filter = fill.GetComponent<MeshFilter>();
            Assert.IsNotNull(filter);
            Assert.IsNotNull(filter.sharedMesh);
            return filter.sharedMesh;
        }

        private static void AssertAllVerticesInside(Mesh mesh, in EnemyAttackArea area)
        {
            Assert.Greater(mesh.vertexCount, 0);
            Assert.Greater(mesh.triangles.Length, 0);
            foreach (Vector3 vertex in mesh.vertices)
            {
                Vector3 point = vertex - Vector3.up * GroundLift;
                Vector3 delta = point - area.Center; delta.y = 0f;
                float distance = delta.magnitude;
                bool inside = area.Contains(point);
                if (!inside)
                {
                    // Equivalent boundary tolerance from the existing area checks: only absorb
                    // quaternion/normalization rounding on the geometric edge, not an escaped fill.
                    inside = distance <= area.Reach + BoundaryTolerance;
                    if (area.Shape == EnemyAttackShape.Ring)
                        inside &= distance >= area.InnerRadius - BoundaryTolerance;
                    else if (area.Shape == EnemyAttackShape.Cone && distance > .0001f)
                        inside &= Vector3.Dot(delta / distance, area.Forward) >=
                            Mathf.Cos(area.Angle * .5f * Mathf.Deg2Rad) - .0001f;
                }
                Assert.IsTrue(inside, $"Fill vertex {vertex} escaped {area.Shape} after reuse.");
            }
        }

        private static void AssertBoundsReach(Mesh mesh, Vector3 center, float reach)
        {
            Bounds bounds = mesh.bounds;
            float x = Mathf.Max(Mathf.Abs(bounds.max.x - center.x), Mathf.Abs(bounds.min.x - center.x));
            float z = Mathf.Max(Mathf.Abs(bounds.max.z - center.z), Mathf.Abs(bounds.min.z - center.z));
            float extent = Mathf.Max(x, z);
            Assert.LessOrEqual(extent, reach + .001f);
            Assert.GreaterOrEqual(extent, reach - .1f);
        }

        private static void Cleanup(CombatGroundFill fill)
        {
            if (!fill) return;
            // These are runtime-created resources owned solely by this helper. Release them in
            // EditMode before its runtime OnDestroy cleanup sees any live resource.
            var filter = fill.GetComponent<MeshFilter>();
            var renderer = fill.GetComponent<MeshRenderer>();
            if (filter && filter.sharedMesh) Object.DestroyImmediate(filter.sharedMesh);
            if (renderer && renderer.sharedMaterial) Object.DestroyImmediate(renderer.sharedMaterial);
            Object.DestroyImmediate(fill.gameObject);
        }
    }
}
