using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Short lived punch trails and impact rings, independent of damage collision.</summary>
public sealed class GauntletImpactVfx : MonoBehaviour
{
    private readonly List<LineRenderer> _lines = new List<LineRenderer>();
    private Material _material;

    public static void Spawn(Vector3 origin, Vector3 forward, float range, float width,
        Color color, bool shock, bool burst, int hand)
    {
        var effect = new GameObject(burst ? "Asura impact" : shock ? "Shock impact" : "Punch trail");
        effect.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(forward, Vector3.up));
        effect.AddComponent<GauntletImpactVfx>().Build(range, width, color, shock, burst, hand);
    }

    private void Build(float range, float width, Color color, bool shock, bool burst, int hand)
    {
        _material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _material.SetColor("_BaseColor", color);
        _material.SetFloat("_Surface", 1);
        _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        _material.SetFloat("_DstBlend", (float)BlendMode.One);
        _material.SetFloat("_ZWrite", 0);
        _material.SetFloat("_Cull", (float)CullMode.Off);
        _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _material.renderQueue = 3000;
        float end = Mathf.Max(.6f, range * .8f);
        for (int i = 0; i < 3; i++)
        {
            float x = hand * .24f + (i - 1) * .1f;
            Line(new[] { new Vector3(x, .95f + i * .08f, .25f),
                new Vector3(x * .6f, 1.05f + i * .08f, end) }, i == 1 ? .09f : .035f);
        }
        Ring(new Vector3(0, 1.05f, end), shock ? width * .32f : .24f, false, .055f);
        if (shock)
        {
            Ring(new Vector3(0, .07f, range * .5f), width * .52f, true, .08f);
            if (burst) Ring(new Vector3(0, .09f, range * .5f), width * .72f, true, .035f);
            for (int i = 0; i < 8; i++)
            {
                Vector3 direction = Quaternion.Euler(0, i * 45, 0) * Vector3.forward;
                Vector3 center = new Vector3(0, .1f, range * .5f);
                Line(new[] { center + direction * .25f, center + direction * width * .48f }, .045f);
            }
        }
        StartCoroutine(Animate(color, shock ? .32f : .13f));
    }

    private void Ring(Vector3 center, float radius, bool ground, float thickness)
    {
        var points = new Vector3[33];
        for (int i = 0; i < points.Length; i++)
        {
            float a = i * Mathf.PI * 2 / 32;
            points[i] = center + (ground ? new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) :
                new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0)) * radius;
        }
        Line(points, thickness);
    }

    private void Line(Vector3[] points, float thickness)
    {
        var obj = new GameObject("Energy stroke");
        obj.transform.SetParent(transform, false);
        var line = obj.AddComponent<LineRenderer>();
        line.sharedMaterial = _material;
        line.useWorldSpace = false;
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.startWidth = thickness;
        line.endWidth = points.Length == 2 ? thickness * .15f : thickness;
        line.numCapVertices = 3;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        _lines.Add(line);
    }

    private IEnumerator Animate(Color color, float duration)
    {
        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.localScale = Vector3.one * Mathf.Lerp(.85f, 1.12f, t);
            color.a = 1 - t;
            _material.SetColor("_BaseColor", color);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy() { if (_material) Destroy(_material); }
}
