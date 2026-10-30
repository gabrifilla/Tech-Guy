using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Bounded, pooled flame tongues. Presentation only; never changes tornado collision.</summary>
internal sealed class TitanicFlameSilhouette
{
    private const int Tongues = 8, Segments = 6;
    private readonly Mesh _mesh;
    private readonly Vector3[] _vertices = new Vector3[Tongues * (Segments + 1) * 2];
    private readonly MeshRenderer _renderer;
    private readonly bool _core;

    internal TitanicFlameSilhouette(Transform parent, Material material, bool core)
    {
        _core = core;
        var root = new GameObject(core ? "Titanic flame cores" : "Titanic flame silhouette");
        root.transform.SetParent(parent, false);
        _mesh = new Mesh { name = root.name + " — pooled mesh" };
        _mesh.MarkDynamic();
        var triangles = new int[Tongues * Segments * 12];
        int cursor = 0;
        for (int k = 0; k < Tongues; k++)
            for (int i = 0; i < Segments; i++)
            {
                int a = (k * (Segments + 1) + i) * 2, b = a + 2;
                // Explicit backfaces keep tongues visible from any gameplay camera direction.
                foreach (int vertex in new[] { a, b, a + 1, a + 1, b, b + 1, a + 1, b, a, b + 1, b, a + 1 })
                    triangles[cursor++] = vertex;
            }
        _mesh.vertices = _vertices;
        _mesh.triangles = triangles;
        _mesh.bounds = new Bounds(Vector3.up, new Vector3(2, 2.4f, 2));
        root.AddComponent<MeshFilter>().sharedMesh = _mesh;
        _renderer = root.AddComponent<MeshRenderer>();
        _renderer.sharedMaterial = material;
        _renderer.shadowCastingMode = ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        var tint = new MaterialPropertyBlock();
        Color color = core ? new Color(1f, .57f, .035f) : new Color(1f, .16f, .012f);
        tint.SetColor("_BaseColor", color); tint.SetColor("_Color", color);
        _renderer.SetPropertyBlock(tint);
    }

    internal void SetActive(bool active) => _renderer.enabled = active;

    internal void Update(float age)
    {
        for (int k = 0; k < Tongues; k++)
            for (int i = 0; i <= Segments; i++)
            {
                float f = i / (float)Segments;
                float pulse = Mathf.Sin(age * 13f + k * 2.3f);
                float height = (k % 3 == 0 ? 1.85f : 1.35f) + pulse * .12f;
                float angle = k * Mathf.PI * 2 / Tongues - age * 3f + f * 1.8f +
                    Mathf.Sin(f * 8f - age * 12f + k) * f * .16f;
                float radius = Mathf.Lerp(.20f, .73f, f) + (_core ? .035f : 0f);
                float width = (1f - f * f) * (.26f + .035f * pulse) * (_core ? .58f : 1f);
                Vector3 radial = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Vector3 tangent = new Vector3(-radial.z, 0, radial.x);
                Vector3 center = radial * radius + Vector3.up * (.12f + f * height);
                int index = (k * (Segments + 1) + i) * 2;
                _vertices[index] = center - tangent * width;
                _vertices[index + 1] = center + tangent * width;
            }
        // Fixed arrays and bounds: no allocations or collider/physics work during travel.
        _mesh.vertices = _vertices;
    }

    internal void Dispose() { if (_mesh) Object.Destroy(_mesh); }
}
