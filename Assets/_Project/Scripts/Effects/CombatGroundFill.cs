using UnityEngine;

/// <summary>
/// Filled-surface companion to <see cref="CombatGroundRing"/>. Renders a procedural mesh that fills
/// an <see cref="EnemyAttackArea"/> beneath the ring outline during an attack windup, growing from
/// the shape's origin to its full extent as the windup progresses. The fill is built from the same
/// shape parameters the damage test (<see cref="EnemyAttackArea.Contains"/>) uses, so at
/// <c>progress == 1</c> the filled surface matches the area exactly.
/// </summary>
/// <remarks>Feature: ranged-kiting-and-attack-telegraph-overhaul. Requirements: 6.1, 6.3, 6.4, 7.1.</remarks>
public sealed class CombatGroundFill : MonoBehaviour
{
    private const int Segments = 64;
    private const float GroundLift = 0.05f; // sits just beneath the ring (ring lifts by 0.07f)

    private MeshFilter _filter;
    private MeshRenderer _renderer;
    private Mesh _mesh;
    private Material _material;
    private Vector3[] _vertices;
    private int[] _triangles;
    private int _topology = -1;
    private bool _topologyApplied;

    /// <summary>
    /// Creates a fill helper under <paramref name="parent"/>, mirroring
    /// <see cref="CombatGroundRing.Create"/>: a new GameObject with a procedural mesh and a
    /// URP/Unlit transparent material tinted to <paramref name="color"/>.
    /// </summary>
    public static CombatGroundFill Create(Transform parent, string label, Color color)
    {
        var go = new GameObject(label);
        if (parent) go.transform.SetParent(parent, false);

        var fill = go.AddComponent<CombatGroundFill>();
        fill._mesh = new Mesh { name = label + "_FillMesh" };
        fill._mesh.MarkDynamic();

        fill._filter = go.AddComponent<MeshFilter>();
        fill._filter.sharedMesh = fill._mesh;

        fill._renderer = go.AddComponent<MeshRenderer>();
        fill._renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fill._renderer.receiveShadows = false;
        fill._material = CreateTransparentMaterial();
        fill._renderer.sharedMaterial = fill._material;

        fill.SetColor(color);
        return fill;
    }

    private static Material CreateTransparentMaterial()
    {
        var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        // Configure the URP/Unlit shader for alpha-blended transparency.
        material.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
        material.SetFloat("_Blend", 0f);   // 0 = Alpha
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        return material;
    }

    /// <summary>
    /// Rebuilds the filled mesh for <paramref name="area"/> grown to <paramref name="progress"/>
    /// (0..1). The fill expands from the shape origin to its full extent: Circle/Cone scale their
    /// reach, Ring scales its outer edge toward the (fixed) inner edge, and Lane scales its reach.
    /// At <c>progress == 1</c> the extent matches the area's exact shape parameters (R6.4).
    /// </summary>
    public void SetArea(in EnemyAttackArea area, float progress)
    {
        progress = Mathf.Clamp01(progress);
        if (progress <= 0f || float.IsNaN(progress)) { Clear(); return; }

        switch (area.Shape)
        {
            case EnemyAttackShape.Lane:
                BuildLane(area, progress);
                break;
            case EnemyAttackShape.Cone:
                BuildCone(area, progress);
                break;
            case EnemyAttackShape.Ring:
                BuildRing(area, progress);
                break;
            default: // Circle
                BuildCircle(area, progress);
                break;
        }
    }

    /// <summary>Tints the fill. Alpha is preserved from <paramref name="color"/>.</summary>
    public void SetColor(Color color) => _material.SetColor("_BaseColor", color);

    /// <summary>Removes the filled geometry within the same frame (R6.6).</summary>
    public void Clear()
    {
        if (_mesh) _mesh.Clear();
        _topologyApplied = false;
    }

    // --- Mesh builders. Coordinates are world-space (matching CombatGroundRing.useWorldSpace). ---

    private void BuildLane(in EnemyAttackArea area, float progress)
    {
        float reach = area.Reach * progress;
        Vector3 center = Lift(area.Center);
        Vector3 right = Vector3.Cross(Vector3.up, area.Forward) * (area.Width * 0.5f);
        Vector3 ahead = area.Forward * reach;

        PrepareBuffers(0, 4, 6);
        _vertices[0] = center - right;
        _vertices[1] = center + right;
        _vertices[2] = center + ahead + right;
        _vertices[3] = center + ahead - right;
        Assign();
    }

    private void BuildCircle(in EnemyAttackArea area, float progress)
    {
        float radius = area.Reach * progress;
        BuildFan(Lift(area.Center), area.Center, area.Forward, radius, -180f, 360f);
    }

    private void BuildCone(in EnemyAttackArea area, float progress)
    {
        float radius = area.Reach * progress;
        BuildFan(Lift(area.Center), area.Center, area.Forward, radius, -area.Angle * 0.5f, area.Angle);
    }

    private void BuildRing(in EnemyAttackArea area, float progress)
    {
        // Inner edge is fixed; the outer edge grows from the inner radius to the full reach.
        float inner = area.InnerRadius;
        float outer = Mathf.Lerp(inner, area.Reach, progress);
        Vector3 center = area.Center;

        PrepareBuffers(2, (Segments + 1) * 2, Segments * 6);
        for (int i = 0; i <= Segments; i++)
        {
            float degrees = 360f * i / Segments;
            Vector3 dir = Quaternion.AngleAxis(degrees, Vector3.up) * area.Forward;
            _vertices[i * 2] = Lift(center + dir * inner);
            _vertices[i * 2 + 1] = Lift(center + dir * outer);
        }
        Assign();
    }

    /// <summary>Builds a triangle fan from the center/apex sweeping <paramref name="arc"/> degrees.</summary>
    private void BuildFan(Vector3 apex, Vector3 center, Vector3 forward, float radius, float startDegrees, float arc)
    {
        PrepareBuffers(1, Segments + 2, Segments * 3);
        _vertices[0] = apex;
        for (int i = 0; i <= Segments; i++)
        {
            float degrees = startDegrees + arc * i / Segments;
            Vector3 dir = Quaternion.AngleAxis(degrees, Vector3.up) * forward;
            _vertices[i + 1] = Lift(center + dir * radius);
        }
        Assign();
    }

    private static Vector3 Lift(Vector3 point)
    {
        point.y += GroundLift;
        return point;
    }

    // Windup geometry changes every frame; the vertex/index buffers only change with topology.
    private void PrepareBuffers(int topology, int vertexCount, int indexCount)
    {
        if (_topology == topology) return;
        _topology = topology;
        _vertices = new Vector3[vertexCount];
        _triangles = new int[indexCount];
        _mesh.Clear();
        _topologyApplied = false;
        if (topology == 0)
        {
            _triangles[0] = 0; _triangles[1] = 2; _triangles[2] = 1;
            _triangles[3] = 0; _triangles[4] = 3; _triangles[5] = 2;
            return;
        }
        for (int i = 0; i < Segments; i++)
        {
            if (topology == 1)
            {
                int t = i * 3;
                _triangles[t] = 0; _triangles[t + 1] = i + 1; _triangles[t + 2] = i + 2;
            }
            else
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                int t = i * 6;
                _triangles[t] = a; _triangles[t + 1] = b; _triangles[t + 2] = d;
                _triangles[t + 3] = a; _triangles[t + 4] = d; _triangles[t + 5] = c;
            }
        }
    }

    private void Assign()
    {
        _mesh.vertices = _vertices;
        if (!_topologyApplied)
        {
            _mesh.triangles = _triangles;
            _topologyApplied = true;
        }
        _mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (_material) Destroy(_material);
        if (_mesh) Destroy(_mesh);
    }
}
