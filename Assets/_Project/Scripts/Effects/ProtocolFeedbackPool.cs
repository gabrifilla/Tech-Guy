using System;
using UnityEngine;
using UnityEngine.Rendering;

public enum ProtocolFeedbackKind { Conductor, Detonation, Combustion, Shatter, ThermalShock, Overflow, EclipseMark, EclipseFall }

/// <summary>Owner-local cosmetic pool. It never retains roots, actors or gameplay callbacks.</summary>
public sealed class ProtocolFeedbackPool : IDisposable
{
    private sealed class Effect
    {
        public GameObject Object;
        public Transform Transform;
        public LineRenderer[] Lines;
        public int ActiveLines;
        public float Remaining;
        public float Radius;
        public Color Color;
        public bool Expands;
        public Vector3 Position;
    }
    private readonly Transform _owner;
    private readonly ProtocolLimits _limits;
    private readonly Effect[] _effects;
    private readonly Vector3[] _points = new Vector3[33];
    private readonly Material _material;
    private readonly MaterialPropertyBlock _tint = new MaterialPropertyBlock();
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private int _frame = -1, _created;
    public int ActiveCount { get; private set; }
    public int CreatedThisFrame => _frame == Time.frameCount ? _created : 0;
    public int Dropped { get; private set; }
    public bool Enabled { get; set; } = true;
    public ProtocolFeedbackPool(Transform owner, ProtocolLimits limits)
    {
        _owner = owner; _limits = limits; _effects = new Effect[limits.ActiveVfx];
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) { Enabled = false; return; }
        _material = new Material(shader) { name = "Protocol feedback (owned)", renderQueue = 3000 };
        _material.SetColor("_BaseColor", Color.white);
        _material.SetFloat("_Surface", 1); _material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        _material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        _material.SetFloat("_ZWrite", 0); _material.SetFloat("_Cull", (float)CullMode.Off);
        _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
    }
    public bool Emit(ProtocolFeedbackKind kind, Vector3 origin, Vector3 destination, float radius, int trails = 0)
    {
        if (!Enabled || !_material || !_owner || !ReactionNumeric.IsFinite(origin) || !ReactionNumeric.IsFinite(destination)) return false;
        if (_frame != Time.frameCount) { _frame = Time.frameCount; _created = 0; }
        if (_created >= _limits.NewVfxPerFrame || ActiveCount >= _limits.ActiveVfx)
        { if (Dropped < int.MaxValue) Dropped++; return false; }
        int slot = 0;
        while (slot < _effects.Length && _effects[slot] != null && _effects[slot].Remaining > 0) slot++;
        var effect = _effects[slot] ?? (_effects[slot] = Create());
        effect.Object.SetActive(true); effect.Transform.position = origin + Vector3.up * .22f;
        effect.Position = effect.Transform.position; effect.Transform.rotation = Quaternion.identity;
        effect.Transform.localScale = Vector3.one; effect.ActiveLines = 1;
        effect.Remaining = _limits.VfxLifetime; effect.Radius = Mathf.Min(radius, 6);
        effect.Expands = kind != ProtocolFeedbackKind.Conductor;
        effect.Color = kind == ProtocolFeedbackKind.EclipseMark ? new Color(.65f,.4f,1f,.55f) :
            kind == ProtocolFeedbackKind.EclipseFall ? new Color(.85f,.65f,1f,.6f) :
            kind == ProtocolFeedbackKind.Conductor ? new Color(.15f, .85f, 1, .65f) :
            kind == ProtocolFeedbackKind.Shatter ? new Color(.65f, .9f, 1, .5f) :
            kind == ProtocolFeedbackKind.ThermalShock ? new Color(.95f, .65f, 1, .55f) :
            kind == ProtocolFeedbackKind.Overflow ? new Color(.6f, 1, .3f, .55f) :
            kind == ProtocolFeedbackKind.Combustion ? new Color(1, .3f, .06f, .45f) : new Color(1, .75f, .2f, .45f);
        for (int i = 0; i < effect.Lines.Length; i++) effect.Lines[i].enabled = false;
        var line = effect.Lines[0]; line.enabled = true;
        if (kind == ProtocolFeedbackKind.Conductor)
        {
            line.positionCount = 4;
            Vector3 end = destination - origin;
            line.SetPosition(0, Vector3.up * .6f); line.SetPosition(1, end * .33f + Vector3.up * .9f);
            line.SetPosition(2, end * .66f + Vector3.up * .35f); line.SetPosition(3, end + Vector3.up * .6f);
        }
        else
        {
            int count = kind == ProtocolFeedbackKind.Overflow || kind == ProtocolFeedbackKind.EclipseMark ? 5 : 33;
            for (int i = 0; i < count; i++)
            {
                float angle = i * Mathf.PI * 2 / (count - 1);
                _points[i] = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * effect.Radius;
            }
            line.positionCount = count;
            for (int i = 0; i < count; i++) line.SetPosition(i, _points[i]);
            if (kind == ProtocolFeedbackKind.Combustion)
            {
                var inner = effect.Lines[1]; inner.enabled = true; inner.positionCount = count;
                for (int i = 0; i < count; i++) inner.SetPosition(i, _points[i] * .6f);
                effect.ActiveLines = 2;
            }
            if (kind == ProtocolFeedbackKind.ThermalShock)
            {
                for (int i = 0; i < 2; i++)
                {
                    var axis = effect.Lines[i + 1]; axis.enabled = true; axis.positionCount = 2;
                    Vector3 direction = (i == 0 ? Vector3.right : Vector3.forward) * effect.Radius * .7f;
                    axis.SetPosition(0, -direction); axis.SetPosition(1, direction);
                }
                effect.ActiveLines = 3;
            }
            if (kind == ProtocolFeedbackKind.Shatter)
                for (int i = 0; i < Mathf.Clamp(trails, _limits.ShatterMinTrails, Mathf.Min(6, _limits.ShatterMaxTrails)); i++)
                {
                    float angle = i * Mathf.PI * 2 / Mathf.Max(3, trails);
                    var shard = effect.Lines[i + 1]; shard.enabled = true; shard.positionCount = 2;
                    Vector3 direction = new Vector3(Mathf.Cos(angle), .1f, Mathf.Sin(angle));
                    shard.SetPosition(0, direction * effect.Radius * .35f); shard.SetPosition(1, direction * effect.Radius);
                    effect.ActiveLines++;
                }
        }
        Tint(effect, 1); _created++; ActiveCount++; return true;
    }
    private Effect Create()
    {
        var effect = new Effect { Object = new GameObject("Protocol accent"), Lines = new LineRenderer[7] };
        effect.Transform = effect.Object.transform; effect.Transform.SetParent(_owner, true);
        for (int i = 0; i < effect.Lines.Length; i++)
        {
            var child = new GameObject("Stroke"); child.transform.SetParent(effect.Object.transform, false);
            var line = child.AddComponent<LineRenderer>(); line.sharedMaterial = _material;
            line.useWorldSpace = false; line.startWidth = .035f; line.endWidth = .018f;
            line.shadowCastingMode = ShadowCastingMode.Off; line.receiveShadows = false;
            effect.Lines[i] = line;
        }
        return effect;
    }
    private void Tint(Effect effect, float fraction)
    {
        var color = effect.Color; color.a *= fraction;
        // URP Unlit does not consume LineRenderer vertex colors; its material uniform does.
        _tint.SetColor(BaseColorId, color);
        for (int i = 0; i < effect.ActiveLines; i++) effect.Lines[i].SetPropertyBlock(_tint);
    }
    public void Tick(float scaledDelta)
    {
        if (!ReactionNumeric.IsFinite(scaledDelta) || scaledDelta <= 0) return;
        for (int i = 0; i < _effects.Length; i++)
        {
            var effect = _effects[i]; if (effect == null || effect.Remaining <= 0) continue;
            effect.Transform.position = effect.Position; effect.Transform.rotation = Quaternion.identity;
            effect.Remaining = Mathf.Max(0, effect.Remaining - scaledDelta);
            if (effect.Remaining == 0) { effect.Object.SetActive(false); ActiveCount--; continue; }
            float fraction = effect.Remaining / _limits.VfxLifetime;
            Tint(effect, fraction);
            if (effect.Expands) effect.Transform.localScale = Vector3.one * (.35f + .65f * (1 - fraction));
        }
    }
    public void Clear()
    {
        for (int i = 0; i < _effects.Length; i++)
            if (_effects[i] != null) { _effects[i].Remaining = 0; if (_effects[i].Object) _effects[i].Object.SetActive(false); }
        ActiveCount = 0;
    }
    public void Dispose()
    {
        Clear();
        for (int i = 0; i < _effects.Length; i++) if (_effects[i] != null && _effects[i].Object) UnityEngine.Object.Destroy(_effects[i].Object);
        if (_material) UnityEngine.Object.Destroy(_material);
    }
}
