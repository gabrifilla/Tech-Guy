using System;
using UnityEngine;

/// <summary>
/// Fase 1b ally trapped in a data capsule, built from primitives (no new asset). Revised after the 10/11/2026
/// playtest so the rescue is felt rather than found: the capsule is already in the room when the fight starts,
/// <b>locked</b> (amber) while enemies remain, and the ally shouts short lines during the fight (arrival, combat,
/// when the player is hurt). A marker over the capsule — clamped to the screen edge when it is off-screen — keeps it
/// in view. Winning the room unlocks it (cyan); pressing the interaction key nearby frees the ally: the rescue
/// callback runs first (the director saves it and heals the player), then a short dialogue plays and the capsule
/// disappears. The capsule cannot be damaged. Colliders are stripped so it never blocks navigation.
/// </summary>
public sealed class RescueCapsule : MonoBehaviour
{
    public const float InteractionRadius = 2.4f;
    public const float BarkDuration = 3.2f;
    private const float FightingBarkInterval = 7f, HurtBarkCooldown = 9f, HurtThreshold = .40f, WaitingDistance = 10f, WaitingInterval = 8f;
    private static readonly Color Cyan = new Color(.30f, .92f, 1f, 1f), Amber = new Color(1f, .55f, .18f, 1f), Ink = new Color(.945f, .914f, .843f, 1f);

    private PlayerActor _player;
    private Func<bool> _canInteract;
    private Action _onRescued;
    private string _displayName;
    private string[] _lines;
    private RescueBarks _barks;
    private bool _rescued, _unlocked;
    private Material _field;
    private Light _glow;
    private string _bark;
    private float _barkUntil, _nextFightingBark, _nextHurtBark, _nextWaitingBark;
    private int _barkCursor;
    private GUIStyle _prompt, _barkStyle, _markerStyle;

    public bool Rescued => _rescued;
    public bool Unlocked => _unlocked;
    /// <summary>The line on screen right now, or null.</summary>
    public string CurrentBark => Time.time < _barkUntil ? _bark : null;
    public bool PlayerInRange => _player && !_player.IsDead &&
        Vector3.ProjectOnPlane(_player.transform.position - transform.position, Vector3.up).sqrMagnitude <= InteractionRadius * InteractionRadius;

    public static RescueCapsule Spawn(Transform parent, Vector3 position, PlayerActor player, string displayName, string[] lines,
        RescueBarks barks, Func<bool> canInteract, Action onRescued)
    {
        var root = new GameObject("Rescue capsule · " + displayName);
        if (parent) root.transform.SetParent(parent, true);
        root.transform.position = position;
        var capsule = root.AddComponent<RescueCapsule>();
        capsule._player = player; capsule._displayName = displayName; capsule._lines = lines; capsule._barks = barks;
        capsule._canInteract = canInteract; capsule._onRescued = onRescued;
        capsule.BuildVisual();
        capsule.Say(barks?.Arrival);
        capsule._nextFightingBark = Time.time + BarkDuration + FightingBarkInterval * .6f;
        return capsule;
    }

    /// <summary>The room was won: the lock drops and the ally can be freed.</summary>
    public void Unlock()
    {
        if (_unlocked || _rescued) return;
        _unlocked = true;
        Tint(Cyan);
        Say(_barks?.Unlocked);
        _nextWaitingBark = Time.time + BarkDuration + WaitingInterval;
    }

    private void BuildVisual()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material Mat(Color color, float emission)
        {
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (emission > 0 && material.HasProperty("_EmissionColor")) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * emission); }
            return material;
        }
        Part(PrimitiveType.Cylinder, Mat(new Color(.10f, .13f, .16f), 0), new Vector3(0, .1f, 0), new Vector3(1.8f, .1f, 1.8f));
        _field = Mat(Amber, .6f);
        Part(PrimitiveType.Cylinder, _field, new Vector3(0, 1.25f, 0), new Vector3(1.3f, 1.1f, 1.3f), "Data field");
        Part(PrimitiveType.Capsule, Mat(new Color(.55f, .58f, .62f), 0), new Vector3(0, 1.1f, 0), new Vector3(.55f, .8f, .55f), "Figure");
        Part(PrimitiveType.Cylinder, Mat(new Color(.10f, .13f, .16f), 0), new Vector3(0, 2.45f, 0), new Vector3(1.6f, .08f, 1.6f));
        _glow = new GameObject("Capsule glow").AddComponent<Light>();
        _glow.transform.SetParent(transform, false); _glow.transform.localPosition = new Vector3(0, 1.6f, 0);
        _glow.type = LightType.Point; _glow.color = Amber; _glow.intensity = 3f; _glow.range = 7f;
    }

    private void Tint(Color color)
    {
        if (_field)
        {
            _field.color = color;
            if (_field.HasProperty("_BaseColor")) _field.SetColor("_BaseColor", color);
            if (_field.HasProperty("_EmissionColor")) _field.SetColor("_EmissionColor", color * .6f);
        }
        if (_glow) _glow.color = color;
    }

    private void Part(PrimitiveType type, Material material, Vector3 position, Vector3 scale, string name = null)
    {
        var part = GameObject.CreatePrimitive(type);
        if (name != null) part.name = name;
        part.transform.SetParent(transform, false); part.transform.localPosition = position; part.transform.localScale = scale;
        if (part.TryGetComponent(out Collider collider)) Destroy(collider);
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    private void Say(string[] pool)
    {
        if (pool == null || pool.Length == 0) return;
        _bark = pool[_barkCursor++ % pool.Length];
        _barkUntil = Time.time + BarkDuration;
    }

    private bool Interactive => _canInteract == null || _canInteract();

    private void Update()
    {
        if (_rescued || GameplayModal.IsOpen || !Interactive) return;
        UpdateBarks();
        if (_unlocked && PlayerInRange && GamePreferences.WasPressed(GamePreferences.InteractionAction)) Rescue();
    }

    private void UpdateBarks()
    {
        if (!_player || _player.IsDead || Time.time < _barkUntil) return;
        if (!_unlocked)
        {
            if (_player.maxHealth > 0 && _player.health / _player.maxHealth <= HurtThreshold && Time.time >= _nextHurtBark)
            { Say(_barks?.PlayerHurt); _nextHurtBark = Time.time + HurtBarkCooldown; return; }
            if (Time.time >= _nextFightingBark) { Say(_barks?.Fighting); _nextFightingBark = Time.time + BarkDuration + FightingBarkInterval; }
            return;
        }
        float distance = Vector3.ProjectOnPlane(_player.transform.position - transform.position, Vector3.up).magnitude;
        if (distance >= WaitingDistance && Time.time >= _nextWaitingBark) { Say(_barks?.Waiting); _nextWaitingBark = Time.time + BarkDuration + WaitingInterval; }
    }

    /// <summary>Frees the ally (only once unlocked): callback (save, heal) first, then the short dialogue; the capsule
    /// disappears when it closes. False while the room is still being fought.</summary>
    public bool Rescue()
    {
        if (_rescued || !_unlocked) return false;
        _rescued = true; _bark = null;
        _onRescued?.Invoke();
        var controls = _player ? _player.GetComponent<CharControlScript>() : null;
        DialoguePanel.Show(_displayName, _lines, () => { if (this) Destroy(gameObject); }, controls);
        return true;
    }

    private void OnGUI()
    {
        if (_rescued || GameplayModal.IsOpen || !Interactive) return;
        var camera = Camera.main;
        if (camera) DrawMarker(camera);
        if (!_unlocked || !PlayerInRange) return;
        _prompt ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height / 36f), alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        _prompt.normal.textColor = Cyan;
        string key = GamePreferences.BindingLabel(GamePreferences.InteractionAction);
        GUI.Label(new Rect(0, Screen.height * .70f, Screen.width, Screen.height * .06f), "[" + key + "] Libertar · " + _displayName, _prompt);
    }

    /// <summary>Name tag (and the current line) above the capsule; clamped to the screen edge when off-screen.</summary>
    private void DrawMarker(Camera camera)
    {
        Vector3 screen = camera.WorldToScreenPoint(transform.position + Vector3.up * 2.9f);
        bool behind = screen.z < 0;
        if (behind) { screen.x = Screen.width - screen.x; screen.y = Screen.height - screen.y; }
        float margin = Screen.height * .06f;
        bool offScreen = behind || screen.x < margin || screen.x > Screen.width - margin || screen.y < margin || screen.y > Screen.height - margin;
        float x = Mathf.Clamp(screen.x, margin * 2.5f, Screen.width - margin * 2.5f);
        float y = Mathf.Clamp(Screen.height - screen.y, margin, Screen.height - margin * 2f);

        _markerStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        _markerStyle.fontSize = Mathf.RoundToInt(Screen.height / 48f);
        _markerStyle.normal.textColor = _unlocked ? Cyan : Amber;
        string tag = (offScreen ? Arrow(screen) + " " : "") + _displayName + (_unlocked ? "" : "  · preso");
        GUI.Label(new Rect(x - 160, y - 14, 320, 28), tag, _markerStyle);

        string bark = CurrentBark;
        if (string.IsNullOrEmpty(bark)) return;
        _barkStyle ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, wordWrap = true, fontStyle = FontStyle.Bold };
        _barkStyle.fontSize = Mathf.RoundToInt(Screen.height / 42f);
        _barkStyle.normal.textColor = Ink;
        float width = Screen.height * .52f, height = Screen.height * .075f;
        GUI.Box(new Rect(Mathf.Clamp(x - width * .5f, 8, Screen.width - width - 8), Mathf.Max(8, y - 14 - height - 4), width, height), "“" + bark + "”", _barkStyle);
    }

    private static string Arrow(Vector3 screen)
    {
        var direction = new Vector2(screen.x - Screen.width * .5f, screen.y - Screen.height * .5f);
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y)) return direction.x < 0 ? "◀" : "▶";
        return direction.y < 0 ? "▼" : "▲";
    }
}
