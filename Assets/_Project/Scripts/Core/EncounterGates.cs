using UnityEngine;
using UnityEngine.AI;

/// <summary>Temporary room doors; carving updates the existing baked navigation.</summary>
public sealed class EncounterGates : MonoBehaviour
{
    private GameObject _entrance, _exit;
    private Material _material;
    public bool ExitLocked => _exit && _exit.activeSelf;

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
    private void OnDestroy() { if (_material) Destroy(_material); }
}
