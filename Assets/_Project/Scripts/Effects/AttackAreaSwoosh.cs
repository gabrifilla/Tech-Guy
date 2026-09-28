using System.Collections;
using UnityEngine;

public class AttackAreaSwoosh : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    private MeshRenderer meshRenderer;
    private Mesh _mesh;
    private Material material;
    private Color baseColor;

    public static void Spawn(Vector3 origin, Vector3 forward, float range, Vector3 boxSize, Color color, float duration)
    {
        if (range <= 0f || forward.sqrMagnitude <= Mathf.Epsilon) return;

        GameObject effectObject = new GameObject("AttackAreaSwoosh");
        effectObject.transform.position = origin;
        effectObject.transform.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

        AttackAreaSwoosh swoosh = effectObject.AddComponent<AttackAreaSwoosh>();
        swoosh.Initialize(range, boxSize, color, duration);
    }

    private void Initialize(float range, Vector3 boxSize, Color color, float duration)
    {
        baseColor = color;

        MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _mesh = BuildMesh(range, boxSize);
        meshFilter.sharedMesh = _mesh;
        material = CreateMaterial(color);
        meshRenderer.sharedMaterial = material;

        StartCoroutine(FadeAndDestroy(Mathf.Max(0.01f, duration)));
    }

    public static void SpawnArea(Vector3 center, Vector3 forward, Vector3 size, AreaHitShape shape,
        float radius, float groundHeight, Color color, float duration)
    {
        var effect = new GameObject("Attack area " + shape);
        center.y = groundHeight + .06f;
        effect.transform.SetPositionAndRotation(center, Quaternion.LookRotation(forward, Vector3.up));
        var visual = effect.AddComponent<AttackAreaSwoosh>();
        color.a = Mathf.Max(.8f, color.a);
        visual.baseColor = color;
        var filter = effect.AddComponent<MeshFilter>();
        visual.meshRenderer = effect.AddComponent<MeshRenderer>();
        visual._mesh = BuildBoundary(size, shape, radius);
        filter.sharedMesh = visual._mesh;
        visual.material = CreateMaterial(color);
        visual.meshRenderer.sharedMaterial = visual.material;
        visual.meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        visual.meshRenderer.receiveShadows = false;
        visual.StartCoroutine(visual.FadeAndDestroy(Mathf.Max(.12f, duration)));
    }

    private static Mesh BuildMesh(float range, Vector3 boxSize)
    {
        Vector3 size = new Vector3(boxSize.x > 0 ? boxSize.x : 2f,
            boxSize.y > 0 ? boxSize.y : 2f, boxSize.z > 0 ? boxSize.z : range);
        Mesh mesh = BuildBoundary(size, AreaHitShape.Box, 0);
        Vector3[] vertices = mesh.vertices;
        for (int i = 0; i < vertices.Length; i++) vertices[i].z += range * .5f;
        mesh.vertices = vertices;
        mesh.RecalculateBounds();
        return mesh;
    }

    // The outside edge is exactly the footprint of the physics query. Never expand the area during fade.
    private static Mesh BuildBoundary(Vector3 size, AreaHitShape shape, float radius)
    {
        int count = shape == AreaHitShape.Sphere ? 64 : 4;
        var vertices = new Vector3[count * 2];
        var triangles = new int[count * 6];
        float thickness = .07f;
        for (int i = 0; i < count; i++)
        {
            Vector3 outer, inner;
            if (shape == AreaHitShape.Sphere)
            {
                float angle = i * Mathf.PI * 2 / count;
                Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
                outer = direction * radius;
                inner = direction * Mathf.Max(0, radius - thickness);
            }
            else
            {
                float x = i == 0 || i == 3 ? -1 : 1;
                float z = i < 2 ? -1 : 1;
                outer = new Vector3(x * size.x * .5f, 0, z * size.z * .5f);
                inner = new Vector3(x * Mathf.Max(0, size.x * .5f - thickness), 0,
                    z * Mathf.Max(0, size.z * .5f - thickness));
            }
            vertices[i * 2] = outer; vertices[i * 2 + 1] = inner;
            int next = (i + 1) % count * 2, t = i * 6, v = i * 2;
            triangles[t] = v; triangles[t+1] = next; triangles[t+2] = v+1;
            triangles[t+3] = v+1; triangles[t+4] = next; triangles[t+5] = next+1;
        }
        var mesh = new Mesh { name = "Damage footprint", vertices = vertices, triangles = triangles };
        mesh.RecalculateBounds(); mesh.RecalculateNormals();
        return mesh;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (!shader)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        }

        Material mat = new Material(shader)
        {
            color = color
        };

        if (mat.HasProperty(BaseColorId))
        {
            mat.SetColor(BaseColorId, color);
        }

        if (mat.HasProperty(ColorId))
        {
            mat.SetColor(ColorId, color);
        }

        ConfigureTransparency(mat);
        mat.renderQueue = 3000;
        return mat;
    }

    private static void ConfigureTransparency(Material mat)
    {
        if (!mat) return;

        if (mat.HasProperty(SurfaceId))
        {
            mat.SetFloat(SurfaceId, 1f);
        }

        if (mat.HasProperty(SrcBlendId))
        {
            mat.SetInt(SrcBlendId, (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }

        if (mat.HasProperty(DstBlendId))
        {
            mat.SetInt(DstBlendId, (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }

        if (mat.HasProperty(ZWriteId))
        {
            mat.SetInt(ZWriteId, 0);
        }

        if (mat.HasProperty(CullId))
        {
            mat.SetInt(CullId, (int)UnityEngine.Rendering.CullMode.Off);
        }

        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }

    private IEnumerator FadeAndDestroy(float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(baseColor.a, 0f, elapsed / duration);
            SetAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void SetAlpha(float alpha)
    {
        if (!material) return;

        Color color = baseColor;
        color.a = alpha;

        material.color = color;
        if (material.HasProperty(BaseColorId))
        {
            material.SetColor(BaseColorId, color);
        }

        if (material.HasProperty(ColorId))
        {
            material.SetColor(ColorId, color);
        }
    }

    private void OnDestroy()
    {
        if (_mesh) Destroy(_mesh);
        if (material)
        {
            Destroy(material);
        }
    }
}
