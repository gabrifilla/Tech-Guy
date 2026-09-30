using System.Collections.Generic;
using TMPro;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

/// <summary>Builds the memory-deck geometry and navigation from the same graph used by combat.</summary>
public sealed class ProceduralStageEnvironment : MonoBehaviour
{
    public enum ArenaShape
    {
        AccessAtrium,
        OpenDeck,
        Crossroads,
        Octagon,
        SplitLanes,
        PillarCourt,
        MemoryVault,
        HiddenArchive,
        CoreSanctum,
    }

    public const float DoorWidth = 4f;
    [SerializeField] private Material _floor;
    [SerializeField] private Material _panels;
    [SerializeField] private Material _walls;
    [SerializeField] private Material _cyan;
    [SerializeField] private Material _corruption;
    [SerializeField] private Material _gold;
    [SerializeField] private Transform _geometry;
    [SerializeField] private ProgressionDirector _director;
    private NavMeshSurface _surface;
    private NavMeshData _runtimeNavigation;
    private readonly Dictionary<int, GameObject> _roomObjects = new();

    public NavMeshSurface Build(RoomGraph graph)
    {
        if (_geometry)
        {
            _geometry.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(_geometry.gameObject);
            else DestroyImmediate(_geometry.gameObject);
        }
        if (_runtimeNavigation) Destroy(_runtimeNavigation);
        _roomObjects.Clear();
        _geometry = new GameObject("Generated memory deck").transform;
        _geometry.SetParent(transform, false);
        foreach (Room room in graph.Rooms)
        {
            var root = new GameObject($"Room {room.Id:00} - {room.Type}").transform;
            root.SetParent(_geometry, false);
            _roomObjects.Add(room.Id, root.gameObject);
            Vector3 center = new Vector3(room.Center.x, 0f, room.Center.y);
            ArenaShape shape = ResolveArenaShape(room);
            Material accent = room.Type == RoomType.Boss ? _corruption :
                room.Type == RoomType.Treasure || room.Type == RoomType.Secret ? _gold : _cyan;
            Box(root, "Walkable deck", center + Vector3.down * .3f,
                new Vector3(room.Size.x, .6f, room.Size.y), _floor, true);
            for (float x = -room.Size.x / 2 + 2; x < room.Size.x / 2 - 1; x += 3)
                for (float z = -room.Size.y / 2 + 2; z < room.Size.y / 2 - 1; z += 3)
                    Box(root, "Deck panel", center + new Vector3(x, .012f, z),
                        new Vector3(2.94f, .02f, 2.94f), _panels, false);

            foreach (Direction side in System.Enum.GetValues(typeof(Direction)))
            {
                bool connected = false;
                foreach (RoomConnection edge in graph.Connections)
                    if ((edge.RoomAId == room.Id && edge.SideFromA == side) ||
                        (edge.RoomBId == room.Id && Opposite(edge.SideFromA) == side)) connected = true;
                bool horizontal = side == Direction.North || side == Direction.South;
                float length = horizontal ? room.Size.x : room.Size.y;
                Vector3 normal = side == Direction.North ? Vector3.forward : side == Direction.South ? Vector3.back :
                    side == Direction.East ? Vector3.right : Vector3.left;
                Vector3 tangent = horizontal ? Vector3.right : Vector3.forward;
                Vector3 edgeCenter = center + normal * (horizontal ? room.Size.y : room.Size.x) * .5f;
                if (connected)
                {
                    float segment = (length - DoorWidth) * .5f;
                    foreach (int sign in new[] { -1, 1 })
                        Wall(root, edgeCenter + tangent * sign * (DoorWidth + segment) * .5f, segment, horizontal);
                    Box(root, "Door guide", edgeCenter - normal * 1.2f + Vector3.up * .05f,
                        horizontal ? new Vector3(3f, .04f, .25f) : new Vector3(.25f, .04f, 3f), accent, false);
                }
                else Wall(root, edgeCenter, length, horizontal);
            }

            BuildArenaProfile(root, center, room.Size, shape, accent);
            var label = new GameObject("Room designation").AddComponent<TextMeshPro>();
            label.transform.SetParent(root, false);
            label.transform.position = center + new Vector3(0f, .07f, -3f);
            label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            label.font = TMP_Settings.defaultFontAsset;
            label.fontSize = 5;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(15f, 3f);
            label.text = room.Type == RoomType.Start ? "SETOR 01 / ACESSO" :
                room.Type == RoomType.Boss ? "GUARDIAO DO NUCLEO" :
                room.Type == RoomType.Treasure ? "CACHE DE MEMORIA" :
                room.Type == RoomType.Secret ? "MEMORIA OCULTA" :
                room.Type == RoomType.Combat ? $"MEMORIA {room.Id:00} / {ShapeName(shape)}" : $"MEMORIA {room.Id:00}";
            label.color = room.Type == RoomType.Boss ? new Color(1f, .2f, .3f) : new Color(.2f, .8f, 1f);
        }
        Physics.SyncTransforms();
        _surface = _geometry.gameObject.AddComponent<NavMeshSurface>();
        _surface.collectObjects = CollectObjects.Children;
        _surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        _surface.BuildNavMesh();
        if (Application.isPlaying) _runtimeNavigation = _surface.navMeshData;
        // Secret floors stay in navigation, but their presentation is hidden until discovery.
        foreach (Room room in graph.Rooms)
            if (room.Type == RoomType.Secret) SetRevealed(room.Id, false);
        return _surface;
    }

    public void SetRevealed(int roomId, bool revealed)
    {
        if (!_roomObjects.TryGetValue(roomId, out GameObject roomObject)) return;
        foreach (Renderer renderer in roomObject.GetComponentsInChildren<Renderer>()) renderer.enabled = revealed;
    }

    public static ArenaShape ResolveArenaShape(Room room)
    {
        if (room.Type == RoomType.Start) return ArenaShape.AccessAtrium;
        if (room.Type == RoomType.Boss) return ArenaShape.CoreSanctum;
        if (room.Type == RoomType.Treasure) return ArenaShape.MemoryVault;
        if (room.Type == RoomType.Secret) return ArenaShape.HiddenArchive;
        return (ArenaShape)(1 + Mathf.Abs(room.Id - 1) % 5);
    }

    private void BuildArenaProfile(Transform root, Vector3 center, Vector2 size, ArenaShape shape, Material accent)
    {
        switch (shape)
        {
            case ArenaShape.Crossroads:
                // Four occupied corners turn the clear space into a broad cross with four attack lanes.
                foreach (int x in new[] { -1, 1 })
                    foreach (int z in new[] { -1, 1 })
                        Block(root, "Crossroad archive", center + new Vector3(x * 7f, 1f, z * 7f),
                            new Vector3(5.5f, 2f, 5.5f), accent);
                break;
            case ArenaShape.Octagon:
                // Diagonal containment rails cut the square corners and create an octagonal fighting ring.
                foreach (int x in new[] { -1, 1 })
                    foreach (int z in new[] { -1, 1 })
                        Box(root, "Diagonal containment rail", center + new Vector3(x * 8.2f, .8f, z * 8.2f),
                            new Vector3(7f, 1.6f, .45f), _walls, true, x == z ? 45f : -45f);
                break;
            case ArenaShape.SplitLanes:
                // A broken central spine creates two long lanes with crossings at the center and doors.
                foreach (int z in new[] { -1, 1 })
                    Block(root, "Broken data spine", center + new Vector3(0f, 1f, z * 5.8f),
                        new Vector3(1.6f, 2f, 6.5f), accent);
                break;
            case ArenaShape.PillarCourt:
                // Six columns form a circular court with sightline breaks for ranged and support enemies.
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * Mathf.PI * 2f / 6f;
                    Block(root, "Signal pillar", center + new Vector3(Mathf.Cos(angle) * 6.5f, 1.25f,
                        Mathf.Sin(angle) * 6.5f), new Vector3(1.4f, 2.5f, 1.4f), accent);
                }
                break;
            case ArenaShape.MemoryVault:
                foreach (int x in new[] { -1, 1 })
                    Block(root, "Vault stack", center + new Vector3(x * 5.5f, 1.2f, 0f),
                        new Vector3(3.5f, 2.4f, 8f), accent);
                break;
            case ArenaShape.HiddenArchive:
                foreach (int z in new[] { -1, 0, 1 })
                    Block(root, "Hidden archive row", center + new Vector3(z % 2 == 0 ? 4f : -4f, 1f, z * 5f),
                        new Vector3(6f, 2f, 1.2f), accent);
                break;
            case ArenaShape.CoreSanctum:
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI * 2f / 8f;
                    Block(root, "Core pylon", center + new Vector3(Mathf.Cos(angle) * 8f, 1.5f,
                        Mathf.Sin(angle) * 8f), new Vector3(1.2f, 3f, 1.2f), accent);
                }
                Box(root, "Guardian sigil", center + Vector3.up * .04f,
                    new Vector3(7f, .035f, 7f), accent, false, 45f);
                break;
            default:
                // The access atrium and open deck retain broad sightlines, with cover only in the corners.
                foreach (int x in new[] { -1, 1 })
                    foreach (int z in new[] { -1, 1 })
                        Block(root, "Memory bank", center + new Vector3(x * (size.x * .5f - 2), 1.3f,
                            z * (size.y * .5f - 2)), new Vector3(1.2f, 2.6f, 1.2f), accent);
                break;
        }
    }

    private void Block(Transform root, string title, Vector3 position, Vector3 size, Material accent)
    {
        Box(root, title, position, size, _walls, true);
        Box(root, title + " signal", position + Vector3.up * (size.y * .5f + .08f),
            new Vector3(size.x + .05f, .08f, size.z + .05f), accent, false);
    }

    private static string ShapeName(ArenaShape shape) => shape switch
    {
        ArenaShape.OpenDeck => "PLATAFORMA",
        ArenaShape.Crossroads => "CRUZAMENTO",
        ArenaShape.Octagon => "OCTOGONO",
        ArenaShape.SplitLanes => "CORREDORES",
        ArenaShape.PillarCourt => "PILARES",
        _ => shape.ToString().ToUpperInvariant(),
    };

    private void Update()
    {
        if (_director && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            _director.TryDiscoverSecret();
    }

    private void OnDestroy()
    {
        if (_runtimeNavigation) Destroy(_runtimeNavigation);
    }

    private void Wall(Transform parent, Vector3 position, float length, bool horizontal)
    {
        Box(parent, "Containment rail", position + Vector3.up * .7f,
            horizontal ? new Vector3(length, 1.4f, .35f) : new Vector3(.35f, 1.4f, length), _walls, true);
    }

    private static void Box(Transform parent, string title, Vector3 position, Vector3 size, Material material, bool solid, float yaw = 0f)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = title;
        box.transform.SetParent(parent, false);
        box.transform.position = position;
        box.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        box.transform.localScale = size;
        box.GetComponent<Renderer>().sharedMaterial = material;
        var collider = box.GetComponent<Collider>();
        collider.enabled = solid;
        if (solid) box.layer = LayerMask.NameToLayer("Ground");
    }

    private static Direction Opposite(Direction side) => side switch
    {
        Direction.North => Direction.South, Direction.South => Direction.North,
        Direction.East => Direction.West, _ => Direction.East
    };
}
