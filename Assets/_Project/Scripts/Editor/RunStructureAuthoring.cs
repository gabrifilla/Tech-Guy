using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>Explicit editor authoring; never invoked by OnValidate or by the gameplay director.</summary>
public static class RunStructureAuthoring
{
    public const string DefinitionPath = "Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset";
    public const string PrefabPath = "Assets/_Project/Prefabs/Run/FirstSectorManualEnvironment.prefab";
    public const string NavigationPath = "Assets/_Project/ScriptableObjects/Run/FirstSectorManualNavigation.asset";
    private const float CorridorWidth = 4f;
    private static readonly Vector3[] Centers = {
        P(500,0), P(500,40), P(500,80), P(460,120), P(540,120), P(500,160),
        P(460,220), P(540,200), P(540,240), P(500,280), P(500,320) };
    private static Vector3 P(float x, float z) => new Vector3(x, 0, z);

    [MenuItem("Tech Guy/Run Structure/Author isolated manual environment")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Authoring requires Edit Mode.");
        if (LayerMask.NameToLayer("Ground") < 0) throw new InvalidOperationException("Manual floors require the existing Ground interaction layer.");
        if (AssetDatabase.LoadAssetAtPath<ManualRunDefinition>(DefinitionPath) || AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) ||
            AssetDatabase.LoadAssetAtPath<NavMeshData>(NavigationPath))
            throw new InvalidOperationException("Manual assets already exist; inspect/update them explicitly while preserving GUIDs.");
        EnsureFolder("Assets/_Project/ScriptableObjects/Run"); EnsureFolder("Assets/_Project/Prefabs/Run");
        var grunt = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Grunt.prefab");
        var heavy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Heavy.prefab");
        var elite = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyVariants/Magic_Haste.prefab");
        var floor = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Toon/Materials/Basalt alloy_68a01a28_2100000.mat");
        var wall = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Toon/Materials/Blue Steel_5355bd05_2100000.mat");
        if (!grunt || !heavy || !elite || !floor || !wall) throw new InvalidOperationException("Existing authoring dependencies are missing.");
        var definition = CreateDefinition(grunt, heavy, elite);
        if (!ManualRouteValidator.TrySnapshot(definition, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out var route, out string error))
        {
            UnityEngine.Object.DestroyImmediate(definition); throw new InvalidOperationException(error);
        }
        Scene previous = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        GameObject root = null;
        try
        {
            SceneManager.SetActiveScene(scene);
            root = new GameObject("First Sector Manual Environment");
            var environment = root.AddComponent<ManualStageEnvironment>();
            var geometry = new GameObject("Authored memory deck"); geometry.transform.SetParent(root.transform, false);
            var surface = geometry.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            var corridors = Corridors();
            var arenas = new Dictionary<int, Transform>();
            var volumes = new Dictionary<int, BoxCollider>();
            var anchors = new Dictionary<int, Transform>();
            foreach (var node in route.Nodes)
            {
                var arena = new GameObject(node.Label); arena.transform.SetParent(geometry.transform, false); arena.transform.position = node.Center;
                arenas.Add(node.Id, arena.transform);
                Box(arena.transform, "Walkable arena", node.Center + Vector3.down * .25f, new Vector3(node.ArenaSize.x, .5f, node.ArenaSize.y), floor).layer = LayerMask.NameToLayer("Ground");
                var ports = new List<Vector3>();
                foreach (var edge in route.Connections)
                {
                    if (edge.From == node.Id) ports.Add(corridors[edge.Id][0]);
                    if (edge.To == node.Id) ports.Add(corridors[edge.Id][corridors[edge.Id].Length - 1]);
                }
                ArenaWalls(arena.transform, node, ports, wall);
                var entry = new GameObject("Entry volume"); entry.transform.SetParent(arena.transform, false);
                var volume = entry.AddComponent<BoxCollider>(); volume.isTrigger = true;
                volume.center = Vector3.up * 1.5f; volume.size = new Vector3(node.ArenaSize.x - 2, 5, node.ArenaSize.y - 2);
                volumes.Add(node.Id, volume);
                var anchor = new GameObject("Reward anchor"); anchor.transform.SetParent(arena.transform, false); anchors.Add(node.Id, anchor.transform);
            }
            var barriers = new Dictionary<int, GameObject>(); var from = new Dictionary<int, Transform>(); var to = new Dictionary<int, Transform>();
            foreach (var edge in route.Connections)
            {
                var connection = new GameObject("Connection " + edge.Id); connection.transform.SetParent(geometry.transform, false);
                Vector3[] points = corridors[edge.Id];
                for (int i = 0; i < points.Length - 1; i++) Corridor(connection.transform, points[i], points[i + 1], floor, wall, i);
                Vector3 direction = (points[1] - points[0]).normalized;
                Vector3 gateCenter = points[0] + direction * 1f;
                bool horizontal = Mathf.Abs(direction.x) > .5f;
                var barrier = Box(connection.transform, "Authored gate", gateCenter + Vector3.up * 3f,
                    horizontal ? new Vector3(.6f,6,CorridorWidth) : new Vector3(CorridorWidth,6,.6f), wall);
                var obstacle = barrier.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.carving = true; obstacle.carveOnlyStationary = false;
                barrier.AddComponent<EncounterGates>().ConfigureAuthoredBarrier(barrier);
                barrier.SetActive(false); // Bake traversable geometry; obstacles control runtime access.
                barriers.Add(edge.Id, barrier);
                from.Add(edge.Id, Anchor(connection.transform, "From", points[0]));
                to.Add(edge.Id, Anchor(connection.transform, "To", points[points.Length - 1]));
            }
            Physics.SyncTransforms(); surface.BuildNavMesh();
            if (!surface.navMeshData) throw new InvalidOperationException("Manual bake did not produce navigation data.");
            var data = new SerializedObject(environment);
            data.FindProperty("_geometryRoot").objectReferenceValue = geometry;
            data.FindProperty("_navigation").objectReferenceValue = surface;
            var nodeBindings = data.FindProperty("_nodes"); nodeBindings.arraySize = route.Nodes.Count;
            int index = 0;
            foreach (var node in route.Nodes.OrderBy(n => n.Id))
            {
                var binding = nodeBindings.GetArrayElementAtIndex(index++);
                binding.FindPropertyRelative("_nodeId").intValue = node.Id;
                binding.FindPropertyRelative("_arena").objectReferenceValue = arenas[node.Id];
                binding.FindPropertyRelative("_entryVolume").objectReferenceValue = volumes[node.Id];
                binding.FindPropertyRelative("_rewardAnchor").objectReferenceValue = anchors[node.Id];
            }
            var edgeBindings = data.FindProperty("_connections"); edgeBindings.arraySize = route.Connections.Count;
            index = 0;
            foreach (var edge in route.Connections)
            {
                var binding = edgeBindings.GetArrayElementAtIndex(index++);
                binding.FindPropertyRelative("_connectionId").intValue = edge.Id;
                binding.FindPropertyRelative("_barrier").objectReferenceValue = barriers[edge.Id];
                binding.FindPropertyRelative("_fromAnchor").objectReferenceValue = from[edge.Id];
                binding.FindPropertyRelative("_toAnchor").objectReferenceValue = to[edge.Id];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            if (!environment.Validate(route, out error)) throw new InvalidOperationException(error);
            AssetDatabase.CreateAsset(definition, DefinitionPath);
            AssetDatabase.CreateAsset(surface.navMeshData, NavigationPath);
            foreach (var barrier in barriers.Values) barrier.SetActive(true);
            geometry.SetActive(false); // Published selection enables exactly one environment later.
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            AssetDatabase.SaveAssetIfDirty(definition); AssetDatabase.SaveAssetIfDirty(surface.navMeshData);
        }
        finally
        {
            if (root) UnityEngine.Object.DestroyImmediate(root);
            if (definition && !EditorUtility.IsPersistent(definition)) UnityEngine.Object.DestroyImmediate(definition);
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        }
    }

    private static ManualRunDefinition CreateDefinition(GameObject grunt, GameObject heavy, GameObject elite)
    {
        var nodes = new List<ManualNodeDefinition>();
        string[] labels = { "S", "C1", "C2", "RP", "C3", "C4", "C5", "E1", "E2", "C6", "B" };
        for (int i = 0; i < labels.Length; i++)
        {
            var kind = i == 0 ? ManualNodeKind.Start : i == 3 ? ManualNodeKind.Repair : i == 7 ? ManualNodeKind.Elite : i == 10 ? ManualNodeKind.Boss : ManualNodeKind.Combat;
            var reward = i == 0 || i == 3 || i == 10 ? RunRewardKind.None : i == 5 ? RunRewardKind.Mutation : i == 6 || i == 8 ? RunRewardKind.Protocol : RunRewardKind.Technique;
            var spawns = new List<ManualEnemySpawn>();
            if (i == 10) spawns.Add(new ManualEnemySpawn(heavy, Vector3.zero, EnemyRank.Boss));
            else if (kind == ManualNodeKind.Combat || kind == ManualNodeKind.Elite)
            {
                if (i == 7) spawns.Add(new ManualEnemySpawn(elite, Vector3.forward * 3, EnemyRank.Elite));
                foreach (var position in new[]{new Vector3(-5,0,4),new Vector3(5,0,4),new Vector3(-5,0,-4),new Vector3(5,0,-4)})
                    spawns.Add(new ManualEnemySpawn(i == 9 ? heavy : grunt, position));
            }
            nodes.Add(new ManualNodeDefinition(i, labels[i], kind, reward, reward == RunRewardKind.None ? 0 : i == 7 ? 2 : 1,
                Centers[i], i == 5 ? new Vector2(32,20) : new Vector2(24,20), i == 3 ? .35f : 0, spawns));
        }
        int[,] pairs = {{0,1},{1,2},{2,3},{2,4},{3,5},{4,5},{5,6},{5,7},{6,9},{7,8},{8,9},{9,10}};
        var edges = new List<ManualConnectionDefinition>();
        for (int i = 0; i < 12; i++) edges.Add(new ManualConnectionDefinition(i,pairs[i,0],pairs[i,1]));
        var definition = ScriptableObject.CreateInstance<ManualRunDefinition>();
        definition.Configure("first-sector-manual-v1", 0, 10, nodes, edges, new[]{
            new ManualExclusiveGroup("A",2,new[]{3},new[]{4}), new ManualExclusiveGroup("B",5,new[]{6},new[]{7,8})});
        foreach (var node in definition.Nodes)
        {
            if (node.Label == "C1" || node.Label == "C2" || node.Label == "C6")
                node.ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Technique, 1), new ManualRewardStep(RunRewardKind.Protocol, 1) });
            else if (node.Label == "E2") node.ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Protocol, 2) });
        }
        definition.ConfigureProtocolCatalog(AssetDatabase.LoadAssetAtPath<ProtocolCatalogDefinition>(
            "Assets/_Project/ScriptableObjects/Protocols/DefaultProtocolCatalog.asset"));
        return definition;
    }

    private static Dictionary<int, Vector3[]> Corridors() => new Dictionary<int, Vector3[]>
    {
        {0,new[]{P(500,10),P(500,30)}}, {1,new[]{P(500,50),P(500,70)}},
        {2,new[]{P(488,80),P(460,80),P(460,110)}}, {3,new[]{P(512,80),P(540,80),P(540,110)}},
        {4,new[]{P(460,130),P(460,160),P(484,160)}}, {5,new[]{P(540,130),P(540,160),P(516,160)}},
        {6,new[]{P(490,170),P(490,180),P(460,180),P(460,210)}},
        {7,new[]{P(510,170),P(510,180),P(540,180),P(540,190)}},
        {8,new[]{P(460,230),P(460,280),P(488,280)}}, {9,new[]{P(540,210),P(540,230)}},
        {10,new[]{P(540,250),P(540,280),P(512,280)}}, {11,new[]{P(500,290),P(500,310)}}
    };

    private static void ArenaWalls(Transform parent, ManualNodeDefinition node, List<Vector3> ports, Material material)
    {
        for (int side = 0; side < 4; side++)
        {
            bool horizontal = side < 2; float sign = side % 2 == 0 ? -1 : 1;
            float extent = (horizontal ? node.ArenaSize.y : node.ArenaSize.x) * .5f;
            float length = horizontal ? node.ArenaSize.x : node.ArenaSize.y;
            var cuts = ports.Where(p => Mathf.Abs((horizontal ? p.z - node.Center.z : p.x - node.Center.x) - sign * extent) < .01f)
                .Select(p => horizontal ? p.x - node.Center.x : p.z - node.Center.z).OrderBy(p => p).ToArray();
            float cursor = -length * .5f;
            foreach (float cut in cuts)
            {
                WallSegment(parent, node.Center, horizontal, sign * extent, cursor, cut - CorridorWidth * .5f, material);
                cursor = cut + CorridorWidth * .5f;
            }
            WallSegment(parent, node.Center, horizontal, sign * extent, cursor, length * .5f, material);
        }
    }
    private static void WallSegment(Transform parent, Vector3 center, bool horizontal, float edge, float start, float end, Material material)
    {
        if (end <= start) return;
        Vector3 position = center + (horizontal ? new Vector3((start+end)*.5f,2,edge) : new Vector3(edge,2,(start+end)*.5f));
        Box(parent,"Arena wall",position,horizontal?new Vector3(end-start,4,.5f):new Vector3(.5f,4,end-start),material);
    }
    private static void Corridor(Transform parent, Vector3 start, Vector3 end, Material floor, Material wall, int segment)
    {
        Vector3 direction=(end-start).normalized; bool horizontal=Mathf.Abs(direction.x)>.5f;
        float length=Vector3.Distance(start,end); Vector3 mid=(start+end)*.5f;
        Box(parent,"Corridor floor " + segment,mid+Vector3.down*.25f,
            horizontal?new Vector3(length+CorridorWidth,.5f,CorridorWidth):new Vector3(CorridorWidth,.5f,length+CorridorWidth),floor).layer = LayerMask.NameToLayer("Ground");
        // Trim walls at bends so orthogonal paths turn without clipping a closed wall.
        float wallLength=Mathf.Max(0,length-CorridorWidth);
        foreach(int sign in new[]{-1,1})
            Box(parent,"Corridor wall",mid+Vector3.up*2+(horizontal?Vector3.forward:Vector3.right)*sign*(CorridorWidth*.5f+.25f),
                horizontal?new Vector3(wallLength,4,.5f):new Vector3(.5f,4,wallLength),wall);
    }
    private static GameObject Box(Transform parent,string name,Vector3 position,Vector3 size,Material material)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent,false);
        go.transform.position=position;go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=material;return go;
    }
    private static Transform Anchor(Transform parent,string name,Vector3 position)
    {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=position;return go.transform;}
    private static void EnsureFolder(string path)
    {
        if(AssetDatabase.IsValidFolder(path))return;
        int slash=path.LastIndexOf('/');string parent=path.Substring(0,slash);EnsureFolder(parent);AssetDatabase.CreateFolder(parent,path.Substring(slash+1));
    }
}
