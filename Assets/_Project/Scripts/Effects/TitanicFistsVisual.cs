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
        internal long Generation;
    }
    private readonly List<TornadoView> _pool = new List<TornadoView>();
    private PlayerActor _player;
    private AbilityHolder _holder;
    private GameObject _overlay;
    private Material _material;
    public bool OverlayActive => _overlay && _overlay.activeInHierarchy;
    public bool WaveActive { get { foreach (var view in _pool) if (view.Root && view.Root.activeSelf) return true; return false; } }
    internal int ActiveTornadoVisuals { get { int count = 0; foreach (var view in _pool) if (view.Root && view.Root.activeSelf) count++; return count; } }
    private void Awake() { _player = GetComponent<PlayerActor>(); _holder = GetComponent<AbilityHolder>(); }
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
    internal TornadoView AcquireTornado(int capacity)
    {
        if (!Active) return null;
        EnsureMaterial(); TornadoView view = null;
        foreach (var item in _pool) if (item.Root && !item.Root.activeSelf) { view = item; break; }
        if (view == null)
        {
            if (_pool.Count >= capacity) return null;
            view = new TornadoView { Root = new GameObject("Titanic wind tornado — pooled"), Ribbons = new LineRenderer[4] };
            for (int i = 0; i < view.Ribbons.Length; i++) view.Ribbons[i] = Ribbon(view.Root, "Swirling wind", 48, i == 3 ? .025f : .055f);
            _pool.Add(view);
        }
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
    }
    internal void ReleaseTornado(TornadoView view, long generation)
    { if (view != null && view.Root && view.Generation == generation) { view.Root.SetActive(false); view.Generation++; } }
    public void Clear()
    {
        if (_overlay) { _overlay.SetActive(false); Destroy(_overlay); _overlay = null; }
        foreach (var view in _pool) if (view.Root) { view.Root.SetActive(false); view.Generation++; }
    }
    private void OnDisable() { Clear(); }
    private void OnDestroy()
    { Clear(); foreach (var view in _pool) if (view.Root) Destroy(view.Root); _pool.Clear(); if (_material) Destroy(_material); }
}
