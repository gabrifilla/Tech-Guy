using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class ProceduralRunGeometryPlanTests
    {
        private ManualRunDefinition _definition;
        private ManualRouteSnapshot _source;

        [SetUp]
        public void SetUp()
        {
            _definition = FluidityRouteTestFactory.Create();
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition,
                Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out _source, out string error), error);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_definition);

        [Test]
        public void CorridorsRemainOrthogonalAndNeverPassThroughAnUnrelatedArena()
        {
            for (ulong seed = 0; seed < 64; seed++)
            {
                var route = ManualRunRandomizer.Generate(_source, seed, null);
                var paths = ProceduralRunGeometryPlan.Create(route);
                foreach (var edge in route.Connections)
                {
                    Vector3[] path = paths[edge.Id];
                    Assert.GreaterOrEqual(path.Length, 2);
                    for (int i = 0; i < path.Length - 1; i++)
                    {
                        Assert.Greater(Vector3.Distance(path[i], path[i + 1]), 0f);
                        Assert.IsTrue(Mathf.Abs(path[i].x - path[i + 1].x) < .01f || Mathf.Abs(path[i].z - path[i + 1].z) < .01f);
                        var floor = Floor(path[i], path[i + 1]);
                        foreach (var other in route.Nodes.Where(node => node.Id != edge.From && node.Id != edge.To))
                            Assert.IsFalse(Overlaps(floor, Arena(other)), "Corridor crosses arena: seed=" + seed + " edge=" + edge.Id + " room=" + other.Id);
                    }
                }
            }
        }

        [Test]
        public void DistinctConnectionsNeverShareWalkableFloorOutsideTheirArenas()
        {
            for (ulong seed = 0; seed < 64; seed++)
            {
                var route = ManualRunRandomizer.Generate(_source, seed, null);
                var paths = ProceduralRunGeometryPlan.Create(route);
                for (int a = 0; a < route.Connections.Count; a++)
                    for (int b = a + 1; b < route.Connections.Count; b++)
                    {
                        var first = route.Connections[a]; var second = route.Connections[b];
                        Vector3[] p = paths[first.Id], q = paths[second.Id];
                        for (int i = 0; i < p.Length - 1; i++)
                            for (int j = 0; j < q.Length - 1; j++)
                                Assert.IsFalse(Overlaps(Floor(p[i], p[i + 1]), Floor(q[j], q[j + 1])),
                                    "Connections share floor and can bypass branch gates: seed=" + seed + " edges=" + first.Id + "/" + second.Id);
                    }
            }
        }

        [Test]
        public void EveryArenaOpeningIsUniqueAndItsGateSpansTheOnlyConnectingFloor()
        {
            const float gateOffset = 1f;
            const float gateThickness = .6f;
            for (ulong seed = 0; seed < 64; seed++)
            {
                var route = ManualRunRandomizer.Generate(_source, seed, null);
                var paths = ProceduralRunGeometryPlan.Create(route);
                foreach (var node in route.Nodes)
                {
                    var ports = new List<Vector3>();
                    foreach (var edge in route.Connections)
                    {
                        var p = paths[edge.Id];
                        if (edge.From == node.Id) ports.Add(p[0]);
                        if (edge.To == node.Id) ports.Add(p[p.Length - 1]);
                    }
                    for (int i = 0; i < ports.Count; i++)
                    {
                        Vector3 local = ports[i] - node.Center;
                        bool xEdge = Mathf.Abs(Mathf.Abs(local.x) - node.ArenaSize.x * .5f) < .01f;
                        bool zEdge = Mathf.Abs(Mathf.Abs(local.z) - node.ArenaSize.y * .5f) < .01f;
                        Assert.IsTrue(xEdge != zEdge, "A port must sit on exactly one arena wall.");
                        Assert.LessOrEqual(Mathf.Abs(xEdge ? local.z : local.x),
                            (xEdge ? node.ArenaSize.y : node.ArenaSize.x) * .5f - ProceduralRunGeometryPlan.CorridorWidth * .5f);
                        for (int j = i + 1; j < ports.Count; j++)
                            Assert.Greater(Vector3.Distance(ports[i], ports[j]), ProceduralRunGeometryPlan.CorridorWidth + .5f,
                                "Two wall openings share a gate passage in room " + node.Id);
                    }
                }
                foreach (var edge in route.Connections)
                {
                    var path = paths[edge.Id];
                    Vector3 direction = (path[1] - path[0]).normalized;
                    Vector3 gate = path[0] + direction * gateOffset;
                    bool horizontal = Mathf.Abs(direction.x) > .5f;
                    float half = ProceduralRunGeometryPlan.CorridorWidth * .5f;
                    var barrier = new Rect(gate.x - (horizontal ? gateThickness * .5f : half),
                        gate.z - (horizontal ? half : gateThickness * .5f),
                        horizontal ? gateThickness : half * 2f, horizontal ? half * 2f : gateThickness);
                    foreach (var node in route.Nodes)
                        Assert.IsFalse(Overlaps(barrier, Arena(node)), "Gate must close the corridor outside arena walls.");
                    foreach (var other in route.Connections.Where(candidate => candidate.Id != edge.Id))
                    {
                        var q = paths[other.Id];
                        for (int i = 0; i < q.Length - 1; i++)
                            Assert.IsFalse(Overlaps(barrier, Floor(q[i], q[i + 1])), "A sealed gate intersects another connection.");
                    }
                }
            }
        }

        // Match materializer floor extents, including its half-width end caps at bends.
        private static Rect Floor(Vector3 start, Vector3 end)
        {
            float half = ProceduralRunGeometryPlan.CorridorWidth * .5f;
            return new Rect(Mathf.Min(start.x, end.x) - half, Mathf.Min(start.z, end.z) - half,
                Mathf.Abs(end.x - start.x) + half * 2f, Mathf.Abs(end.z - start.z) + half * 2f);
        }
        private static Rect Arena(ManualNodeDefinition node) => new Rect(node.Center.x - node.ArenaSize.x * .5f,
            node.Center.z - node.ArenaSize.y * .5f, node.ArenaSize.x, node.ArenaSize.y);
        private static bool Overlaps(Rect first, Rect second) => first.xMin < second.xMax - .01f &&
            first.xMax > second.xMin + .01f && first.yMin < second.yMax - .01f && first.yMax > second.yMin + .01f;
    }
}
