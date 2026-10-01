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
        _mesh.Clear();
        if (progress <= 0f) return;

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
    }

    // --- Mesh builders. Coordinates are world-space (matching CombatGroundRing.useWorldSpace). ---

    private void BuildLane(in EnemyAttackArea area, float progress)
    {
        float reach = area.Reach * progress;
        Vector3 center = Lift(area.Center);
        Vector3 right = Vector3.Cross(Vector3.up, area.Forward) * (area.Width * 0.5f);
        Vector3 ahead = area.Forward * reach;

        var verts = new[]
        {
            center - right,
            center + right,
            center + ahead + right,
            center + ahead - right,
        };
        var tris = new[] { 0, 2, 1, 0, 3, 2 };
        Assign(verts, tris);
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

        var verts = new Vector3[(Segments + 1) * 2];
        var tris = new int[Segments * 6];
        for (int i = 0; i <= Segments; i++)
        {
            float degrees = 360f * i / Segments;
            Vector3 dir = Quaternion.AngleAxis(degrees, Vector3.up) * area.Forward;
            verts[i * 2] = Lift(center + dir * inner);
            verts[i * 2 + 1] = Lift(center + dir * outer);
        }
        for (int i = 0; i < Segments; i++)
        {
            int a = i * 2, b = i * 2 + 1, c = (i + 1) * 2, d = (i + 1) * 2 + 1;
            int t = i * 6;
            tris[t] = a; tris[t + 1] = b; tris[t + 2] = d;
            tris[t + 3] = a; tris[t + 4] = d; tris[t + 5] = c;
        }
        Assign(verts, tris);
    }

    /// <summary>Builds a triangle fan from the center/apex sweeping <paramref name="arc"/> degrees.</summary>
    private void BuildFan(Vector3 apex, Vector3 center, Vector3 forward, float radius, float startDegrees, float arc)
    {
        var verts = new Vector3[Segments + 2];
        var tris = new int[Segments * 3];
        verts[0] = apex;
        for (int i = 0; i <= Segments; i++)
        {
            float degrees = startDegrees + arc * i / Segments;
            Vector3 dir = Quaternion.AngleAxis(degrees, Vector3.up) * forward;
            verts[i + 1] = Lift(center + dir * radius);
        }
        for (int i = 0; i < Segments; i++)
        {
            int t = i * 3;
            tris[t] = 0; tris[t + 1] = i + 1; tris[t + 2] = i + 2;
        }
        Assign(verts, tris);
    }

    private static Vector3 Lift(Vector3 point)
    {
        point.y += GroundLift;
        return point;
    }

    private void Assign(Vector3[] verts, int[] tris)
    {
        _mesh.Clear();
        _mesh.vertices = verts;
        _mesh.triangles = tris;
        _mesh.RecalculateBounds();
    }

    private void OnDestroy()
    {
        if (_material) Destroy(_material);
        if (_mesh) Destroy(_mesh);
    }
}
