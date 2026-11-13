using System;
using UnityEngine;

/// <summary>Pure rules of one fishing cast (Fase 1c), testable without a scene.</summary>
public static class FishingRules
{
    public const float InteractionRadius = 3.2f;
    public const float WaitMin = 1.2f, WaitMax = 3.2f;
    /// <summary>How long the bite stays "on the hook" for the player to pull.</summary>
    public const float BiteWindow = .75f;
    public const float ResultSeconds = 1.6f;

    public enum Pull { TooEarly, Caught, TooLate }

    /// <summary>Outcome of pressing the key <paramref name="elapsed"/> seconds after the cast, for a bite at <paramref name="biteAt"/>.</summary>
    public static Pull Resolve(float elapsed, float biteAt) =>
        elapsed < biteAt ? Pull.TooEarly : elapsed <= biteAt + BiteWindow ? Pull.Caught : Pull.TooLate;

    public static float BiteTime(double roll) => Mathf.Lerp(WaitMin, WaitMax, Mathf.Clamp01((float)roll));
}

/// <summary>
/// The pond of the serene room (Fase 1c): a calm spot with a simple timing minigame — cast, wait for the bite,
/// pull in time. Up to <see cref="GiftEconomy.CastsPerVisit"/> casts. What a catch gives is decided by the owner
/// (the run director): usually Fragmentos, rarely a rapport gift. Visuals are primitives; no new asset.
/// </summary>
public sealed class FishingSpot : MonoBehaviour
{
    private enum State { Idle, Waiting, Bite, Result }
    private static readonly Color Water = new Color(.16f, .55f, .78f), Calm = new Color(.38f, .86f, .78f), Alert = new Color(1f, .85f, .25f);

    private Transform _player;
    private Func<bool> _canAct;
    private Func<string> _onCatch;
    private System.Random _rng;
    private State _state;
    private float _elapsed, _biteAt, _resultUntil;
    private string _result = "";
    private Transform _bobber;
    private LineRenderer _line;
    private Material _material, _waterMaterial;
    private GUIStyle _prompt, _big;

    public int CastsLeft { get; private set; }
    public bool IsWaitingForBite => _state == State.Waiting;
    public bool HasBite => _state == State.Bite;
    public string LastResult => _result;

    public static FishingSpot Spawn(Transform parent, Vector3 position, Transform player, int casts, int seed,
        Func<bool> canAct, Func<string> onCatch)
    {
        var go = new GameObject("Sala serena — lago de pesca");
        if (parent) go.transform.SetParent(parent, true);
        go.transform.position = position;
        var spot = go.AddComponent<FishingSpot>();
        spot._player = player; spot._canAct = canAct; spot._onCatch = onCatch;
        spot.CastsLeft = Mathf.Max(0, casts); spot._rng = new System.Random(seed);
        spot.Build();
        return spot;
    }

    private bool PlayerInRange => _player &&
        Vector3.ProjectOnPlane(_player.position - transform.position, Vector3.up).sqrMagnitude <= FishingRules.InteractionRadius * FishingRules.InteractionRadius;

    private bool CanAct => !GameplayModal.IsOpen && (_canAct == null || _canAct());

    private void Update()
    {
        if (_state == State.Result && Time.time >= _resultUntil) _state = State.Idle;
        if (_state == State.Waiting || _state == State.Bite)
        {
            if (!PlayerInRange) { EndCast("Você se afastou; a linha voltou.", consume: false); return; }
            _elapsed += Time.deltaTime;
            if (_state == State.Waiting && _elapsed >= _biteAt) _state = State.Bite;
            else if (_state == State.Bite && _elapsed > _biteAt + FishingRules.BiteWindow) { EndCast("Escapou…", consume: true); return; }
            AnimateBobber();
        }
        if (!CanAct || !PlayerInRange) return;
        if (GamePreferences.WasPressed(GamePreferences.InteractionAction)) Press();
    }

    /// <summary>The interaction key: casts when idle, pulls while the line is out (public for tests and tools).</summary>
    public void Press()
    {
        if (_state == State.Idle)
        {
            if (CastsLeft <= 0) return;
            _state = State.Waiting; _elapsed = 0f; _biteAt = FishingRules.BiteTime(_rng.NextDouble());
            SetLineVisible(true); AnimateBobber();
            return;
        }
        if (_state != State.Waiting && _state != State.Bite) return;
        switch (FishingRules.Resolve(_elapsed, _biteAt))
        {
            case FishingRules.Pull.TooEarly: EndCast("Cedo demais! O peixe fugiu.", consume: true); break;
            case FishingRules.Pull.Caught: EndCast(_onCatch != null ? _onCatch() : "Fisgou!", consume: true); break;
            default: EndCast("Escapou…", consume: true); break;
        }
    }

    /// <summary>Test seam: jumps the current cast to the moment of the bite.</summary>
    internal void SkipToBite() { if (_state == State.Waiting) { _elapsed = _biteAt; _state = State.Bite; } }

    private void EndCast(string message, bool consume)
    {
        if (consume) CastsLeft = Mathf.Max(0, CastsLeft - 1);
        _result = message ?? ""; _state = State.Result; _resultUntil = Time.time + FishingRules.ResultSeconds;
        SetLineVisible(false);
    }

    private void Build()
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        _waterMaterial = new Material(shader); _waterMaterial.SetColor("_BaseColor", Water);
        _material = new Material(shader); _material.SetColor("_BaseColor", Calm);
        var pond = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pond.name = "Lago"; pond.transform.SetParent(transform, false);
        pond.transform.localPosition = new Vector3(0, .03f, 0); pond.transform.localScale = new Vector3(3.2f, .02f, 3.2f);
        if (pond.TryGetComponent(out Collider pondCollider)) Destroy(pondCollider);
        pond.GetComponent<Renderer>().sharedMaterial = _waterMaterial;
        var ring = CombatGroundRing.Create(transform, "Borda do lago", Calm);
        ring.Draw(transform.position + Vector3.up * .06f, 1.7f, .1f);
        var bobber = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        bobber.name = "Boia"; bobber.transform.SetParent(transform, false); bobber.transform.localScale = Vector3.one * .22f;
        if (bobber.TryGetComponent(out Collider bobberCollider)) Destroy(bobberCollider);
        bobber.GetComponent<Renderer>().sharedMaterial = _material;
        _bobber = bobber.transform; _bobber.gameObject.SetActive(false);
        _line = new GameObject("Linha").AddComponent<LineRenderer>();
        _line.transform.SetParent(transform, false);
        _line.sharedMaterial = _material; _line.positionCount = 2; _line.useWorldSpace = true;
        _line.startWidth = _line.endWidth = .03f; _line.enabled = false;
        var glow = new GameObject("Luz do lago").AddComponent<Light>();
        glow.transform.SetParent(transform, false); glow.transform.localPosition = new Vector3(0, 1.6f, 0);
        glow.type = LightType.Point; glow.color = Calm; glow.intensity = 2f; glow.range = 7f;
    }

    private void SetLineVisible(bool visible)
    {
        if (_bobber) _bobber.gameObject.SetActive(visible);
        if (_line) _line.enabled = visible;
    }

    private void AnimateBobber()
    {
        if (!_bobber) return;
        bool bite = _state == State.Bite;
        float dip = bite ? -.12f + Mathf.Sin(Time.time * 40f) * .05f : Mathf.Sin(Time.time * 2.2f) * .03f;
        _bobber.position = transform.position + new Vector3(.25f, .16f + dip, .15f);
        _material.SetColor("_BaseColor", bite ? Alert : Calm);
        if (_line && _player) { _line.SetPosition(0, _player.position + Vector3.up * 1.2f); _line.SetPosition(1, _bobber.position); }
    }

    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint || GameplayModal.IsOpen || !PlayerInRange) return;
        _prompt ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        _big ??= new GUIStyle(_prompt);
        _prompt.fontSize = Mathf.RoundToInt(Screen.height / 36f); _big.fontSize = Mathf.RoundToInt(Screen.height / 16f);
        string key = "[" + GamePreferences.BindingLabel(GamePreferences.InteractionAction) + "] ";
        var rect = new Rect(0, Screen.height * .70f, Screen.width, Screen.height * .06f);
        switch (_state)
        {
            case State.Idle:
                _prompt.normal.textColor = Calm;
                GUI.Label(rect, CastsLeft > 0 ? key + "Pescar  ·  " + CastsLeft + (CastsLeft == 1 ? " lançamento" : " lançamentos")
                    : "O lago se acalmou. Nada mais por hoje.", _prompt);
                break;
            case State.Waiting:
                _prompt.normal.textColor = Calm;
                GUI.Label(rect, "Espere a fisgada…", _prompt);
                break;
            case State.Bite:
                _big.normal.textColor = Alert;
                GUI.Label(new Rect(0, Screen.height * .56f, Screen.width, Screen.height * .12f), "!", _big);
                _prompt.normal.textColor = Alert;
                GUI.Label(rect, key + "Puxar agora!", _prompt);
                break;
            default:
                _prompt.normal.textColor = Color.white;
                GUI.Label(rect, _result, _prompt);
                break;
        }
    }

    private void OnDestroy()
    {
        if (_material) Destroy(_material);
        if (_waterMaterial) Destroy(_waterMaterial);
    }
}
