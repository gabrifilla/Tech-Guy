using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Temporary room doors; carving updates the existing baked navigation.</summary>
public sealed class EncounterGates : MonoBehaviour
{
    private GameObject _entrance, _exit;
    private Material _material;
    public bool ExitLocked => _exit && _exit.activeSelf;

    // Directional model (procedural-stage-room-generation). Kept separate from the legacy
    // _entrance/_exit pair so both APIs coexist without interfering with FirstSectorDirector.
    private readonly Dictionary<Direction, GameObject> _directionalDoors = new();
    private Vector3 _directionalCenter;
    private Vector2 _directionalSize;
    private bool _directionalConfigured;
    private float _openingWidth;
    private Direction? _bossSealDirection;
    private int _bossSealRequired;
    private int _bossSealProgress;
    private readonly List<Renderer> _bossSealIndicators = new();
    private Material _bossSealLockedMaterial;
    private Material _bossSealReadyMaterial;

    public void Configure(Vector3 center, Vector2 size)
    {
        _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _material.color = new Color(.9f,.18f,.12f,.8f);
        _entrance = Door("Entrada selada", center + Vector3.back * size.y * .5f, size.x);
        _exit = Door("Saída selada", center + Vector3.forward * size.y * .5f, size.x);
    }
    private GameObject Door(string title, Vector3 position, float width)
    {
        var door = GameObject.CreatePrimitive(PrimitiveType.Cube);
        door.name = title;
        door.transform.SetParent(transform, true);
        door.transform.position = position + Vector3.up * .65f;
        door.transform.localScale = new Vector3(width,.12f,.4f);
        door.GetComponent<Renderer>().sharedMaterial = _material;
        var collider = door.GetComponent<BoxCollider>();
        collider.size = new Vector3(1,30,1);
        var obstacle = door.AddComponent<NavMeshObstacle>();
        obstacle.shape = NavMeshObstacleShape.Box;
        obstacle.size = collider.size;
        obstacle.carving = true;
        obstacle.carveOnlyStationary = false;
        return door;
    }
    public void OpenEntrance() => _entrance.SetActive(false);
    public void Seal() { _entrance.SetActive(true); _exit.SetActive(true); }
    public void OpenExit() => _exit.SetActive(false);

    /// <summary>
    /// Sets up the directional door model around <paramref name="center"/> with the room
    /// <paramref name="size"/> (X width, Y depth on the Z axis). Doors are materialized lazily
    /// via <see cref="AddDoor"/> on the requested edge. Reuses the shared sealed-door material.
    /// </summary>
    public void ConfigureDirectional(Vector3 center, Vector2 size, float openingWidth = 0f)
    {
        _directionalCenter = center;
        _directionalSize = size;
        _openingWidth = openingWidth;
        _directionalConfigured = true;
        if (!_material)
        {
            _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            _material.color = new Color(.9f, .18f, .12f, .8f);
        }
    }

    /// <summary>
    /// Materializes a sealed door on the given edge if one does not already exist. Idempotent per
    /// <paramref name="direction"/>; the door starts sealed (active), blocking passage both ways.
    /// </summary>
    public void AddDoor(Direction direction)
    {
        if (!_directionalConfigured)
            ConfigureDirectional(_directionalCenter, _directionalSize);
        if (_directionalDoors.ContainsKey(direction))
            return;
        _directionalDoors[direction] = Door(DoorTitle(direction), DoorPosition(direction), DoorWidth(direction));
        if (direction == Direction.East || direction == Direction.West)
            _directionalDoors[direction].transform.rotation = Quaternion.Euler(0f, 90f, 0f);
    }

    /// <summary>Opens (unseals) the door on <paramref name="direction"/>, if present.</summary>
    public void OpenDoor(Direction direction)
    {
        if (_bossSealDirection == direction && _bossSealProgress < _bossSealRequired)
            return;
        if (_directionalDoors.TryGetValue(direction, out var door) && door)
            door.SetActive(false);
    }

    /// <summary>Marks a directional door as the boss seal and adds one visible socket per required fragment.</summary>
    public void ConfigureBossSeal(Direction direction, int requiredFragments)
    {
        AddDoor(direction);
        _bossSealDirection = direction;
        _bossSealRequired = Mathf.Max(1, requiredFragments);
        _bossSealProgress = 0;
        _bossSealLockedMaterial = CreateMaterial(new Color(.45f, .04f, .12f, 1f));
        _bossSealReadyMaterial = CreateMaterial(new Color(1f, .58f, .08f, 1f));
        Vector3 position = DoorPosition(direction) + Vector3.up * 1.3f;
        Vector3 tangent = direction == Direction.North || direction == Direction.South
            ? Vector3.right : Vector3.forward;
        for (int i = 0; i < _bossSealRequired; i++)
        {
            var socket = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            socket.name = $"Fragmento de acesso {i + 1}";
            socket.transform.SetParent(transform, true);
            socket.transform.position = position + tangent * ((i - (_bossSealRequired - 1) * .5f) * .75f);
            socket.transform.localScale = Vector3.one * .42f;
            DestroySafely(socket.GetComponent<Collider>());
            Renderer renderer = socket.GetComponent<Renderer>();
            renderer.sharedMaterial = _bossSealLockedMaterial;
            _bossSealIndicators.Add(renderer);
        }
    }

    /// <summary>Updates the boss-seal sockets. The door can only open after every socket is charged.</summary>
    public void SetBossSealProgress(int fragments)
    {
        _bossSealProgress = Mathf.Clamp(fragments, 0, _bossSealRequired);
        for (int i = 0; i < _bossSealIndicators.Count; i++)
            if (_bossSealIndicators[i])
                _bossSealIndicators[i].sharedMaterial = i < _bossSealProgress
                    ? _bossSealReadyMaterial : _bossSealLockedMaterial;
    }

    public bool IsBossSealReady => _bossSealDirection.HasValue && _bossSealProgress >= _bossSealRequired;

    /// <summary>Seals the door on <paramref name="direction"/>, creating it first if needed.</summary>
    public void SealDoor(Direction direction)
    {
        AddDoor(direction);
        if (_directionalDoors.TryGetValue(direction, out var door) && door)
            door.SetActive(true);
    }

    /// <summary>True when a door exists on <paramref name="direction"/> and is currently sealed.</summary>
    public bool IsDoorSealed(Direction direction)
        => _directionalDoors.TryGetValue(direction, out var door) && door && door.activeSelf;

    /// <summary>Seals every directional door that has been added.</summary>
    public void SealAll()
    {
        foreach (var door in _directionalDoors.Values)
            if (door) door.SetActive(true);
    }

    private Vector3 DoorPosition(Direction direction) => direction switch
    {
        Direction.North => _directionalCenter + Vector3.forward * _directionalSize.y * .5f,
        Direction.South => _directionalCenter + Vector3.back * _directionalSize.y * .5f,
        Direction.East => _directionalCenter + Vector3.right * _directionalSize.x * .5f,
        Direction.West => _directionalCenter + Vector3.left * _directionalSize.x * .5f,
        _ => _directionalCenter,
    };

    // North/South span the room width (X); East/West span the room depth (Z).
    private float DoorWidth(Direction direction)
        => _openingWidth > 0f ? _openingWidth :
            (direction == Direction.North || direction == Direction.South ? _directionalSize.x : _directionalSize.y);

    private static string DoorTitle(Direction direction) => "Porta selada " + direction;

    private static Material CreateMaterial(Color color)
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.color = color;
        return material;
    }

    private static void DestroySafely(Object value)
    {
        if (!value) return;
        if (Application.isPlaying) Destroy(value);
        else DestroyImmediate(value);
    }

    private void OnDestroy()
    {
        DestroySafely(_material);
        DestroySafely(_bossSealLockedMaterial);
        DestroySafely(_bossSealReadyMaterial);
    }
}
