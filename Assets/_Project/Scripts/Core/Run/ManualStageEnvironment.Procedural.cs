using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public sealed partial class ManualStageEnvironment
{
    private NavMeshData _generatedNavigation;
    public bool HasGeneratedLayout => _generatedNavigation;

    /// <summary>Build a session-only deck using the materials of the new authored rooms.</summary>
    public bool BuildGenerated(ManualRouteSnapshot route, out string error)
    {
        error = null;
        if (!Application.isPlaying || route == null || !_geometryRoot)
        { error = "Procedural rooms require Play Mode and a valid arena template."; return false; }
        Material floor = null, wall = null;
        foreach (var renderer in _geometryRoot.GetComponentsInChildren<MeshRenderer>(true))
        {
            if (!renderer.TryGetComponent<BoxCollider>(out var box)) continue;
            float height = Mathf.Abs(box.size.y * renderer.transform.lossyScale.y);
            if (height < 1f && !floor) floor = renderer.sharedMaterial;
            if (height >= 2f && !wall) wall = renderer.sharedMaterial;
        }
        if (!floor || !wall || LayerMask.NameToLayer("Ground") < 0)
        { error = "The new rooms require floor/wall materials and the Ground layer."; return false; }
        var corridors = ProceduralRunGeometryPlan.Create(route);
        GameObject previousRoot = _geometryRoot;
        previousRoot.SetActive(false);
        _geometryRoot = new GameObject("Procedural memory deck");
        _geometryRoot.transform.SetParent(transform, false);
        _nodes = new List<ManualNodeBinding>();
        _connections = new List<ManualConnectionBinding>();
        try
        {
            foreach (var node in route.Nodes)
            {
                var arena = new GameObject(node.Label).transform;
                arena.SetParent(_geometryRoot.transform, false);
                arena.position = node.Center;
                GeneratedBox(arena, "Walkable arena", node.Center + Vector3.down * .25f,
                    new Vector3(node.ArenaSize.x, .5f, node.ArenaSize.y), floor).layer = LayerMask.NameToLayer("Ground");
                var ports = new List<Vector3>();
                foreach (var edge in route.Connections)
                {
                    if (edge.From == node.Id) ports.Add(corridors[edge.Id][0]);
                    if (edge.To == node.Id) ports.Add(corridors[edge.Id][corridors[edge.Id].Length - 1]);
                }
                GeneratedArenaWalls(arena, node, ports, wall);
                var entry = new GameObject("Entry volume").AddComponent<BoxCollider>();
                entry.transform.SetParent(arena, false); entry.isTrigger = true;
                entry.center = Vector3.up * 1.5f;
                entry.size = new Vector3(node.ArenaSize.x - 2, 5, node.ArenaSize.y - 2);
                var reward = new GameObject("Reward anchor").transform;
                reward.SetParent(arena, false);
                _nodes.Add(new ManualNodeBinding(node.Id, arena, entry, reward));
            }
            foreach (var edge in route.Connections)
            {
                var root = new GameObject("Connection " + edge.Id).transform;
                root.SetParent(_geometryRoot.transform, false);
                Vector3[] points = corridors[edge.Id];
                for (int i = 0; i < points.Length - 1; i++)
                    GeneratedCorridor(root, points[i], points[i + 1], floor, wall);
                Vector3 direction = (points[1] - points[0]).normalized;
                bool horizontal = Mathf.Abs(direction.x) > .5f;
                var barrier = GeneratedBox(root, "Encounter gate", points[0] + direction + Vector3.up * 3f,
                    horizontal ? new Vector3(.6f, 6, ProceduralRunGeometryPlan.CorridorWidth) :
                    new Vector3(ProceduralRunGeometryPlan.CorridorWidth, 6, .6f), wall);
                var obstacle = barrier.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box; obstacle.carving = true; obstacle.carveOnlyStationary = false;
                barrier.AddComponent<EncounterGates>().ConfigureAuthoredBarrier(barrier);
                barrier.SetActive(false);
                _connections.Add(new ManualConnectionBinding(edge.Id, barrier,
                    GeneratedAnchor(root, points[0]), GeneratedAnchor(root, points[points.Length - 1])));
            }
            _navigation = _geometryRoot.AddComponent<NavMeshSurface>();
            _navigation.collectObjects = CollectObjects.Children;
            _navigation.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            Physics.SyncTransforms();
            _navigation.BuildNavMesh();
            _generatedNavigation = _navigation.navMeshData;
            if (!_generatedNavigation || !Validate(route, out error) || !ValidateGeneratedPaths(route, out error))
                throw new InvalidOperationException(error ?? "Generated navigation is missing.");
            foreach (var binding in _connections) binding.Barrier.SetActive(true);
            return true;
        }
        catch (Exception exception)
        {
            error = "Generated rooms refused: " + exception.Message;
            _geometryRoot.SetActive(false);
            return false;
        }
    }

    private bool ValidateGeneratedPaths(ManualRouteSnapshot route, out string error)
    {
        error = null;
        foreach (var node in route.Nodes)
        {
            if (!NavMesh.SamplePosition(node.Center, out var center, 2f, NavMesh.AllAreas))
            { error = "Arena has no walkable center: " + node.Id; return false; }
            foreach (var spawn in node.Spawns)
            {
                var path = new NavMeshPath();
                if (!NavMesh.SamplePosition(node.Center + spawn.Offset, out var target, 2f, NavMesh.AllAreas) ||
                    !NavMesh.CalculatePath(center.position, target.position, NavMesh.AllAreas, path) || path.status != NavMeshPathStatus.PathComplete)
                { error = "Unreachable enemy spawn in arena " + node.Id; return false; }
            }
        }
        foreach (var edge in route.Connections)
        {
            var path = new NavMeshPath();
            if (!NavMesh.CalculatePath(route.Node(edge.From).Center, route.Node(edge.To).Center, NavMesh.AllAreas, path) ||
                path.status != NavMeshPathStatus.PathComplete)
            { error = "Unreachable generated connection " + edge.Id; return false; }
        }
        return true;
    }

    private static void GeneratedArenaWalls(Transform root, ManualNodeDefinition node, List<Vector3> ports, Material wall)
    {
        for (int side = 0; side < 4; side++)
        {
            bool horizontal = side < 2;
            float sign = side % 2 == 0 ? -1 : 1;
            float extent = (horizontal ? node.ArenaSize.y : node.ArenaSize.x) * .5f;
            float length = horizontal ? node.ArenaSize.x : node.ArenaSize.y;
            var cuts = ports.Where(p => Mathf.Abs((horizontal ? p.z - node.Center.z : p.x - node.Center.x) - sign * extent) < .01f)
                .Select(p => horizontal ? p.x - node.Center.x : p.z - node.Center.z).OrderBy(p => p);
            float cursor = -length * .5f;
            foreach (float cut in cuts)
            {
                GeneratedWall(root, node.Center, horizontal, sign * extent, cursor, cut - ProceduralRunGeometryPlan.CorridorWidth * .5f, wall);
                cursor = cut + ProceduralRunGeometryPlan.CorridorWidth * .5f;
            }
            GeneratedWall(root, node.Center, horizontal, sign * extent, cursor, length * .5f, wall);
        }
    }

    private static void GeneratedWall(Transform root, Vector3 center, bool horizontal, float edge, float start, float end, Material wall)
    {
        if (end <= start) return;
        GeneratedBox(root, "Arena wall", center + (horizontal ? new Vector3((start + end) * .5f, 2, edge) : new Vector3(edge, 2, (start + end) * .5f)),
            horizontal ? new Vector3(end - start, 4, .5f) : new Vector3(.5f, 4, end - start), wall);
    }

    private static void GeneratedCorridor(Transform root, Vector3 start, Vector3 end, Material floor, Material wall)
    {
        float width = ProceduralRunGeometryPlan.CorridorWidth;
        bool horizontal = Mathf.Abs(end.x - start.x) > .01f;
        float length = Vector3.Distance(start, end);
        Vector3 mid = (start + end) * .5f;
        GeneratedBox(root, "Corridor floor", mid + Vector3.down * .25f,
            horizontal ? new Vector3(length + width, .5f, width) : new Vector3(width, .5f, length + width), floor).layer = LayerMask.NameToLayer("Ground");
        foreach (int sign in new[] { -1, 1 })
            GeneratedBox(root, "Corridor wall", mid + Vector3.up * 2 + (horizontal ? Vector3.forward : Vector3.right) * sign * (width * .5f + .25f),
                horizontal ? new Vector3(Mathf.Max(.01f, length - width), 4, .5f) : new Vector3(.5f, 4, Mathf.Max(.01f, length - width)), wall);
    }

    private static GameObject GeneratedBox(Transform root, string label, Vector3 position, Vector3 size, Material material)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.name = label;
        go.transform.SetParent(root, false); go.transform.position = position; go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }
    private static Transform GeneratedAnchor(Transform root, Vector3 position)
    {
        var anchor = new GameObject("Door anchor").transform;
        anchor.SetParent(root, false); anchor.position = position; return anchor;
    }
    private void OnDestroy()
    {
        if (_generatedNavigation) Destroy(_generatedNavigation);
    }
}
