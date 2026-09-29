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
    public void ConfigureDirectional(Vector3 center, Vector2 size)
    {
        _directionalCenter = center;
        _directionalSize = size;
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
    }

    /// <summary>Opens (unseals) the door on <paramref name="direction"/>, if present.</summary>
    public void OpenDoor(Direction direction)
    {
        if (_directionalDoors.TryGetValue(direction, out var door) && door)
            door.SetActive(false);
    }

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
        => direction == Direction.North || direction == Direction.South ? _directionalSize.x : _directionalSize.y;

    private static string DoorTitle(Direction direction) => "Porta selada " + direction;

    private void OnDestroy() { if (_material) Destroy(_material); }
}
