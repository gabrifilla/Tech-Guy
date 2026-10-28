using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Five local line proxies, one reused material; no collider, light or targetable actor.</summary>
internal sealed class BowBoonPresentation : IDisposable
{
    private readonly GameObject _root;
    private readonly Material _material;
    private readonly LineRenderer[] _lines = new LineRenderer[5];
    private readonly Vector3[] _ring = new Vector3[25];
    public BowBoonPresentation(Transform parent)
    {
        _root = new GameObject("Bow boon indicators"); _root.transform.SetParent(parent, false);
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
        if (shader) _material = new Material(shader) { color = new Color(1f, .72f, .2f, .8f) };
        for (int i = 0; i < _lines.Length; i++)
        {
            var go = new GameObject("Bow indicator"); go.transform.SetParent(_root.transform, false);
            var line = go.AddComponent<LineRenderer>(); line.useWorldSpace = true;
            line.sharedMaterial = _material; line.startWidth = line.endWidth = .035f; line.enabled = false;
            _lines[i] = line;
        }
    }
    public void Draw(bool charging, float ratio, Vector3 origin, Vector3 direction, Actor priority, IReadOnlyList<Vector3> mines)
    {
        _lines[0].enabled = charging;
        if (charging)
        {
            _lines[0].positionCount = 2;
            _lines[0].SetPosition(0, origin + Vector3.up * .1f);
            _lines[0].SetPosition(1, origin + Vector3.up * .1f + direction * (2f + 6f * ratio));
            _lines[0].startWidth = _lines[0].endWidth = ratio >= 1 ? .1f : .035f;
        }
        _lines[1].enabled = priority;
        if (priority) Ring(_lines[1], priority.transform.position, .65f);
        for (int i = 0; i < 3; i++)
        { _lines[i + 2].enabled = mines != null && i < mines.Count; if (_lines[i + 2].enabled) Ring(_lines[i + 2], mines[i], .7f); }
    }
    private void Ring(LineRenderer line, Vector3 point, float radius)
    {
        for (int i = 0; i < _ring.Length; i++)
        { float t = i * Mathf.PI * 2 / (_ring.Length - 1); _ring[i] = point + new Vector3(Mathf.Cos(t) * radius, .06f, Mathf.Sin(t) * radius); }
        line.positionCount = _ring.Length; line.SetPositions(_ring);
    }
    public void Hide() { foreach (var line in _lines) if (line) line.enabled = false; }
    public void Dispose() { if (_root) UnityEngine.Object.Destroy(_root); if (_material) UnityEngine.Object.Destroy(_material); }
}
