using System.Collections.Generic;
using UnityEngine;

/// <summary>Run-owned wind cuff and reusable world-space vortices. Visuals never cause damage.</summary>
[DisallowMultipleComponent]
public sealed class TitanicFistsVisual : MonoBehaviour
{
    internal sealed class TornadoView
    {
        internal GameObject Root;
        internal LineRenderer[] Ribbons;
        internal LineRenderer[] Fire, Electricity;
        internal TitanicFlameSilhouette Flame, FlameCore;
        internal ProtocolBasicEffects Effects;
        internal long Generation;
    }
    private readonly List<TornadoView> _pool = new List<TornadoView>();
    private PlayerActor _player;
    private AbilityHolder _holder;
    private GameObject _overlay;
    private Material _material;
    private MaterialPropertyBlock _identityTint;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int LegacyColor = Shader.PropertyToID("_Color");
    public bool OverlayActive => _overlay && _overlay.activeInHierarchy;
    public bool WaveActive { get { foreach (var view in _pool) if (view.Root && view.Root.activeSelf) return true; return false; } }
    internal int ActiveTornadoVisuals { get { int count = 0; foreach (var view in _pool) if (view.Root && view.Root.activeSelf) count++; return count; } }
    private void Awake() { _player = GetComponent<PlayerActor>(); _holder = GetComponent<AbilityHolder>(); _identityTint = new MaterialPropertyBlock(); }
    private bool Active => _player && _player.isActiveAndEnabled && !_player.IsDead && _player.CurrentWeapon &&
        WeaponRunModifiers.Identify(_player.CurrentWeapon) == RunWeaponFamily.Gauntlet &&
        _player.RunModifiers?.Expansion.MutationId(BoonSlot.Basic) == TitanicFistsDefinition.Id;
    private void EnsureMaterial()
    {
        if (_material) return;
        _material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"))
            { name = "Titanic wind — runtime", color = Color.white };
    }
    private LineRenderer Ribbon(GameObject parent, string name, int points, float width)
    {
        var go = new GameObject(name); go.transform.SetParent(parent.transform, false);
        var line = go.AddComponent<LineRenderer>(); line.sharedMaterial = _material;
        line.useWorldSpace = false; line.positionCount = points; line.widthMultiplier = width;
        line.startColor = TitanicFistsDefinition.Color; line.endColor = new Color(.8f, 1, 1, .8f);
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; line.receiveShadows = false;
        return line;
    }
    private void TintIdentity(LineRenderer line, Color color)
    {
        // URP Unlit ignores the LineRenderer's vertex gradient. Match ProtocolFeedbackPool.
        _identityTint.Clear(); _identityTint.SetColor(BaseColor, color); _identityTint.SetColor(LegacyColor, color);
        line.SetPropertyBlock(_identityTint);
    }
    private void LateUpdate()
    {
        if (!Active) { Clear(); return; }
        if (!_overlay && _player.HandTransform)
        {
            EnsureMaterial(); _overlay = new GameObject("Punhos Titânicos — wind cuff");
            _overlay.transform.SetParent(_player.HandTransform, false);
            for (int k = 0; k < 2; k++)
            {
                var line = Ribbon(_overlay, "Wind pressure", 40, .035f);
                for (int i = 0; i < 40; i++)
                { float f = i / 39f, angle = f * Mathf.PI * 4 + k * Mathf.PI;
                    line.SetPosition(i, new Vector3(Mathf.Cos(angle) * .19f, Mathf.Sin(angle) * .19f, -.08f + f * .36f)); }
            }
        }
        if (_overlay && !_player.IsCombatHitStopped && Time.timeScale > 0 && (!_holder || !_holder.IsGamePaused))
            _overlay.transform.Rotate(Vector3.forward, Time.deltaTime * 220f, Space.Self);
    }
    internal TornadoView AcquireTornado(int capacity, ProtocolBasicEffects effects = ProtocolBasicEffects.None)
    {
        if (!Active) return null;
        EnsureMaterial(); TornadoView view = null;
        foreach (var item in _pool) if (item.Root && !item.Root.activeSelf) { view = item; break; }
        if (view == null)
        {
            if (_pool.Count >= capacity) return null;
            view = new TornadoView { Root = new GameObject("Titanic wind tornado — pooled"), Ribbons = new LineRenderer[4] };
            for (int i = 0; i < view.Ribbons.Length; i++) view.Ribbons[i] = Ribbon(view.Root, "Swirling wind", 48, i == 3 ? .025f : .055f);
            view.Fire = new LineRenderer[3]; view.Electricity = new LineRenderer[2];
            view.Flame = new TitanicFlameSilhouette(view.Root.transform, _material, false);
            view.FlameCore = new TitanicFlameSilhouette(view.Root.transform, _material, true);
            for (int i = 0; i < view.Fire.Length; i++)
            {
                var flame = view.Fire[i] = Ribbon(view.Root, "Titanic fire identity", 32, .09f);
                flame.startColor = new Color(1, .22f, .035f); flame.endColor = new Color(1, .68f, .10f);
                TintIdentity(flame, i == 1 ? new Color(1, .5f, .04f) : new Color(1, .18f, .02f));
                flame.widthCurve = AnimationCurve.Linear(0, 1.25f, 1, .12f);
            }
            for (int i = 0; i < view.Electricity.Length; i++)
            {
                var bolt = view.Electricity[i] = Ribbon(view.Root, "Titanic storm identity", 32, .035f);
                bolt.startColor = new Color(.12f, .4f, 1); bolt.endColor = new Color(.65f, .85f, 1);
                TintIdentity(bolt, new Color(.15f, .65f, 1));
            }
            _pool.Add(view);
        }
        SetIdentity(view, effects);
        view.Generation++; view.Root.SetActive(true); return view;
    }
    internal void UpdateTornado(TornadoView view, long generation, Vector3 position, Vector3 forward, float age)
    {
        if (view == null || !view.Root || view.Generation != generation || !view.Root.activeSelf) return;
        view.Root.transform.SetPositionAndRotation(position, Quaternion.LookRotation(forward));
        for (int k = 0; k < view.Ribbons.Length; k++)
            for (int i = 0; i < 48; i++)
            {
                float f = i / 47f, angle = f * Mathf.PI * (k == 3 ? 2f : 5f) + k * 2.1f - age * 12f;
                float radius = k == 3 ? TitanicFistsDefinition.Radius : Mathf.Lerp(.12f, TitanicFistsDefinition.Radius, f);
                float y = k == 3 ? .08f : .08f + f * (TitanicFistsDefinition.Height - .08f);
                view.Ribbons[k].SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius));
            }
        // Identity ribbons never query colliders or issue gameplay/feedback events.
        if ((view.Effects & ProtocolBasicEffects.Detonation) != 0)
        {
            view.Flame.Update(age); view.FlameCore.Update(age);
            for (int k = 0; k < view.Fire.Length; k++)
                for (int i = 0; i < 32; i++)
                {
                    float f = i / 31f, angle = f * Mathf.PI * 4 + k * 2.1f - age * 9;
                    float radius = Mathf.Lerp(.16f, .57f, f) + Mathf.Sin(f * 23 - age * 16 + k) * .06f;
                    view.Fire[k].SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .12f + f * 1.5f, Mathf.Sin(angle) * radius));
                }
        }
        if ((view.Effects & ProtocolBasicEffects.Conductor) != 0)
            for (int k = 0; k < view.Electricity.Length; k++)
                for (int i = 0; i < 32; i++)
                {
                    float f = i / 31f, angle = f * Mathf.PI * 3 + k * Mathf.PI - age * 6;
                    float zigzag = (i % 2 == 0 ? 1 : -1) * (.06f + .025f * Mathf.Sin(age * 18 + i));
                    float radius = Mathf.Lerp(.24f, .72f, f) + zigzag;
                    view.Electricity[k].SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, .14f + f * 1.55f, Mathf.Sin(angle) * radius));
                }
    }
    private static void SetIdentity(TornadoView view, ProtocolBasicEffects effects)
    {
        view.Effects = effects;
        view.Flame.SetActive((effects & ProtocolBasicEffects.Detonation) != 0);
        view.FlameCore.SetActive((effects & ProtocolBasicEffects.Detonation) != 0);
        // Keep wind readable as a contour around flames, without washing out the fire body.
        for (int i = 0; i < view.Ribbons.Length; i++)
            view.Ribbons[i].widthMultiplier = (i == 3 ? .025f : .055f) *
                ((effects & ProtocolBasicEffects.Detonation) != 0 ? .5f : 1f);
        foreach (var line in view.Fire) line.enabled = (effects & ProtocolBasicEffects.Detonation) != 0;
        foreach (var line in view.Electricity) line.enabled = (effects & ProtocolBasicEffects.Conductor) != 0;
    }
    internal void ReleaseTornado(TornadoView view, long generation)
    { if (view != null && view.Root && view.Generation == generation) { SetIdentity(view, ProtocolBasicEffects.None); view.Root.SetActive(false); view.Generation++; } }
    public void Clear()
    {
        if (_overlay) { _overlay.SetActive(false); Destroy(_overlay); _overlay = null; }
        foreach (var view in _pool) if (view.Root) { SetIdentity(view, ProtocolBasicEffects.None); view.Root.SetActive(false); view.Generation++; }
    }
    private void OnDisable() { Clear(); }
    private void OnDestroy()
    { Clear(); foreach (var view in _pool) { view.Flame.Dispose(); view.FlameCore.Dispose(); if (view.Root) Destroy(view.Root); } _pool.Clear(); if (_material) Destroy(_material); }
}
