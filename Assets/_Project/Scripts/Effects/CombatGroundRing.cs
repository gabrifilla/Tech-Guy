using UnityEngine;

/// <summary>Reusable world-space boundary. The same radius is used by gameplay checks.</summary>
public sealed class CombatGroundRing : MonoBehaviour
{
    private LineRenderer _line;
    private Material _material;
    private readonly Vector3[] _points = new Vector3[65];

    public static CombatGroundRing Create(Transform parent, string label, Color color)
    {
        var go = new GameObject(label);
        if (parent) go.transform.SetParent(parent, false);
        var ring = go.AddComponent<CombatGroundRing>();
        ring._line = go.AddComponent<LineRenderer>();
        ring._line.useWorldSpace = true;
        ring._line.positionCount = 65;
        ring._line.widthMultiplier = 0.07f;
        ring._line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ring._material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        ring._line.sharedMaterial = ring._material;
        ring.SetColor(color);
        return ring;
    }

    public void Draw(Vector3 center, float radius, float width = 0.07f)
    {
        _line.widthMultiplier = width;
        center.y += 0.07f;
        for (int i = 0; i < _points.Length; i++)
        {
            float angle = i * Mathf.PI * 2 / 64;
            _points[i] = center + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
        }
        _line.SetPositions(_points);
    }

    public void SetColor(Color color) => _material.SetColor("_BaseColor", color);
    private void OnDestroy() { if (_material) Destroy(_material); }
}
