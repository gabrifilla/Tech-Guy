using UnityEngine;
using UnityEngine.UI;

public enum SkillGlyphKind { Strike, Flurry, Shock, Asura, Dash, Sword }

/// <summary>Resolution-independent, project-owned symbols for the action bar.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SkillGlyphGraphic : MaskableGraphic
{
    [SerializeField] private SkillGlyphKind _kind;
    public void SetKind(SkillGlyphKind kind) { if (_kind == kind) return; _kind = kind; SetVerticesDirty(); }

    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        if (_kind == SkillGlyphKind.Dash)
        {
            for (int i = 0; i < 3; i++)
            {
                float x = 0.13f + i * 0.25f;
                Line(mesh, new Vector2(x, 0.2f), new Vector2(x + 0.22f, 0.5f), 0.075f);
                Line(mesh, new Vector2(x + 0.22f, 0.5f), new Vector2(x, 0.8f), 0.075f);
            }
            return;
        }
        if (_kind == SkillGlyphKind.Shock)
        {
            Polygon(mesh, new[] { new Vector2(.6f,.96f), new Vector2(.18f,.45f), new Vector2(.44f,.45f),
                new Vector2(.33f,.04f), new Vector2(.86f,.61f), new Vector2(.57f,.61f) });
            return;
        }
        if (_kind == SkillGlyphKind.Sword)
        {
            Polygon(mesh, new[] { new Vector2(.23f,.14f),new Vector2(.34f,.1f),new Vector2(.86f,.89f),new Vector2(.67f,.8f) });
            Line(mesh, new Vector2(.14f,.36f), new Vector2(.5f,.22f), .065f);
            return;
        }
        Polygon(mesh, new[] { new Vector2(.31f,.18f),new Vector2(.65f,.18f),new Vector2(.79f,.53f),
            new Vector2(.77f,.77f),new Vector2(.28f,.77f),new Vector2(.23f,.52f) });
        for (int i = 0; i < 3; i++)
            Line(mesh, new Vector2(.35f+i*.13f,.79f), new Vector2(.35f+i*.13f,.93f), .09f);
        Line(mesh, new Vector2(.33f,.1f), new Vector2(.64f,.1f), .065f);
        if (_kind == SkillGlyphKind.Flurry || _kind == SkillGlyphKind.Asura)
        {
            for (int i = 0; i < 3; i++)
                Line(mesh, new Vector2(.02f,.28f+i*.19f), new Vector2(.2f,.35f+i*.19f), .035f);
        }
        if (_kind == SkillGlyphKind.Asura)
        {
            Line(mesh, new Vector2(.82f,.23f), new Vector2(.97f,.09f), .04f);
            Line(mesh, new Vector2(.86f,.53f), new Vector2(.99f,.53f), .04f);
            Line(mesh, new Vector2(.81f,.82f), new Vector2(.94f,.96f), .04f);
        }
    }

    private void Line(VertexHelper mesh, Vector2 from, Vector2 to, float width)
    {
        Vector2 normal = new Vector2(-(to-from).y, (to-from).x).normalized * width * .5f;
        Polygon(mesh, new[] { from-normal, from+normal, to+normal, to-normal });
    }

    private void Polygon(VertexHelper mesh, Vector2[] points)
    {
        Rect rect = GetPixelAdjustedRect();
        int start = mesh.currentVertCount;
        foreach (Vector2 point in points)
            mesh.AddVert(new Vector3(rect.x + point.x * rect.width, rect.y + point.y * rect.height), color, Vector2.zero);
        for (int i = 1; i < points.Length - 1; i++) mesh.AddTriangle(start, start+i, start+i+1);
    }
}
