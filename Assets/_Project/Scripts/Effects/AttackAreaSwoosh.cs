using System.Collections;
using UnityEngine;

public class AttackAreaSwoosh : MonoBehaviour
{
    private const int SegmentCount = 14;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    private MeshRenderer meshRenderer;
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
        meshFilter.sharedMesh = BuildMesh(range, boxSize);
        material = CreateMaterial(color);
        meshRenderer.sharedMaterial = material;

        StartCoroutine(FadeAndDestroy(Mathf.Max(0.01f, duration)));
    }

    private static Mesh BuildMesh(float range, Vector3 boxSize)
    {
        float width = Mathf.Max(0.1f, boxSize.x);
        float depth = Mathf.Max(0.1f, boxSize.z > 0f ? boxSize.z : range);
        float halfWidth = width * 0.5f;

        Vector3[] vertices = new Vector3[SegmentCount + 3];
        int[] triangles = new int[(SegmentCount + 1) * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i <= SegmentCount + 1; i++)
        {
            float t = i / (float)(SegmentCount + 1);
            float x = Mathf.Lerp(-halfWidth, halfWidth, t);
            float arc = Mathf.Sin(t * Mathf.PI) * width * 0.22f;
            vertices[i + 1] = new Vector3(x, 0f, depth - arc);
        }

        for (int i = 0; i <= SegmentCount; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i + 2;
        }

        Mesh mesh = new Mesh
        {
            name = "AttackAreaSwooshMesh",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Unlit/Transparent");
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
        if (material)
        {
            Destroy(material);
        }
    }
}
