using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// World-space visual that signals a protected enemy (Requisito 6). While the owning
/// <see cref="Actor"/> carries a <see cref="Shield"/> with capacity left, this shows a ground aura
/// (reusing the <see cref="CombatGroundRing"/> pattern the Mirror already uses, R6.6) plus a
/// billboarded shield icon above the enemy's head (R6.1, R6.7). When the shield self-destroys the
/// visuals disappear (R6.2), and a fully-absorbed hit briefly pulses the icon/aura so the player can
/// tell a "blocked" hit apart from a normal one (R6.3, via <see cref="Actor.DamageAbsorbed"/>).
///
/// The component is thin: it owns only the Unity visual concerns and reuses the scene systems above.
/// It resolves its <see cref="Actor"/>/<see cref="Shield"/> via <c>GetComponent</c> so it is
/// self-sufficient when added at runtime (attachment is task 6.4; Mirror integration is task 6.5).
///
/// Sprite loading: the icon sprite is a serialized field so a prefab/inspector can assign it. Because
/// this component is added at runtime through <c>AddComponent</c>, the field is normally unassigned,
/// so it falls back to loading the authored sprite by its known project path in the Editor
/// (<see cref="EditorSpritePath"/>). If no sprite is available at all (e.g. a player build with the
/// field left unassigned), it draws a design-sanctioned raised <see cref="CombatGroundRing"/> outline
/// as the icon so the protection state still reads.
///
/// External driver (task 6.5): a component that already owns its own ground visual can drive this
/// indicator in "icon-only" mode via <see cref="ConfigureExternalSource"/>. That supplies the
/// protected state from a predicate instead of a <see cref="Shield"/> and suppresses the ground aura,
/// so the Mirror (<see cref="FrontalReflector"/>) reuses the same shield icon while <c>ShieldActive</c>
/// without a second ground ring on top of its frontal arc (R6.4). When no external source is set the
/// component keeps its default Shield-driven aura+icon behaviour unchanged (tasks 6.3/6.4).
/// </summary>
/// <remarks>Feature: combat-balance-tuning, tasks 6.3/6.5. Requirements: 6.1, 6.2, 6.3, 6.4, 6.5, 6.6, 6.7.</remarks>
[DisallowMultipleComponent]
public sealed class ProtectionIndicator : MonoBehaviour
{
    /// <summary>Project path of the authored shield sprite, used as an Editor runtime fallback when the field is unassigned.</summary>
    private const string EditorSpritePath = "Assets/_Project/Art/UI/ProtectionShieldIcon.png";

    /// <summary>Local default aura/icon color when no <see cref="CombatBalanceConfig"/> asset is present (R5.3).</summary>
    private static readonly Color DefaultAuraColor = new Color(0.4f, 0.9f, 1f, 1f);

    [Header("Icon")]
    [Tooltip("Shield sprite shown above the enemy's head. Left unassigned at runtime it is loaded by path in the Editor, else a ground-ring outline is drawn instead.")]
    [SerializeField] private Sprite shieldIcon;

    [Tooltip("World-space height of the icon above the enemy's bounds top, keeping it clear of the health bar (R6.7).")]
    [SerializeField, Min(0f)] private float iconHeadroom = 0.6f;

    [Tooltip("World-space size (metres) of the shield icon.")]
    [SerializeField, Min(0.05f)] private float iconSize = 0.6f;

    [Header("Absorb pulse")]
    [Tooltip("Duration in seconds of the pulse played when a hit is fully absorbed (R6.3).")]
    [SerializeField, Min(0f)] private float pulseDuration = 0.15f;

    [Tooltip("Extra scale added to the icon at the peak of the absorb pulse.")]
    [SerializeField, Min(0f)] private float pulseScaleBoost = 0.6f;

    private Actor _owner;
    private Shield _shield;

    private CombatGroundRing _aura;
    private GameObject _icon;
    private SpriteRenderer _iconRenderer;
    private CombatGroundRing _iconOutline; // fallback when no sprite is available
    private readonly Vector3[] _iconOutlinePoints = new Vector3[6];

    // External driver (task 6.5): when set, the protected state comes from this predicate instead of
    // the Shield component, and _suppressAura hides the ground ring so a driver that owns its own
    // ground visual (the Mirror's frontal arc) doesn't get a duplicate aura (R6.4).
    private Func<bool> _externalIsProtected;
    private bool _suppressAura;

    private Color _auraColor = DefaultAuraColor;
    private bool _iconEnabled = true;
    private bool _visible;
    private float _auraRadius = 0.75f;
    private float _iconHeight = 2f;
    private float _pulseTimer;
    private Transform _cameraTransform;

    /// <summary>
    /// Drives this indicator from an external protection source instead of the owning <see cref="Shield"/>
    /// (task 6.5). Used by the Mirror (<see cref="FrontalReflector"/>) to show only the shield icon while
    /// its frontal shield is active, reusing this component's icon/billboard/pulse/cleanup rather than
    /// drawing a second ground ring on top of its arc (R6.4).
    /// </summary>
    /// <param name="isProtected">
    /// Predicate polled each frame for the protected state. Pass <c>null</c> to restore the default
    /// Shield-driven behaviour (tasks 6.3/6.4).
    /// </param>
    /// <param name="suppressAura">
    /// When <c>true</c> the ground aura is not drawn — the caller already owns a ground visual, so the
    /// indicator shows the icon only.
    /// </param>
    public void ConfigureExternalSource(Func<bool> isProtected, bool suppressAura)
    {
        _externalIsProtected = isProtected;
        _suppressAura = suppressAura;

        // If we were already showing visuals, rebuild them so a newly-suppressed aura is torn down (or a
        // re-enabled one recreated) on the next frame rather than lingering with the old configuration.
        if (_visible)
            HideVisuals();
    }

    private void Awake()
    {
        _owner = GetComponent<Actor>();
        _shield = GetComponent<Shield>();

        var config = CombatBalance.Current;
        _auraColor = config != null ? config.ProtectionAuraColor : DefaultAuraColor;
        _iconEnabled = config == null || config.ProtectionIconEnabled;

        ResolveBounds();
        ResolveSprite();
    }

    private void OnEnable()
    {
        if (_owner != null)
        {
            _owner.DamageAbsorbed += OnDamageAbsorbed;
            _owner.Died += OnOwnerDied;
        }
    }

    private void OnDisable()
    {
        if (_owner != null)
        {
            _owner.DamageAbsorbed -= OnDamageAbsorbed;
            _owner.Died -= OnOwnerDied;
        }
        HideVisuals();
    }

    private void Update()
    {
        if (_pulseTimer > 0f)
            _pulseTimer = Mathf.Max(0f, _pulseTimer - Time.deltaTime);

        bool protectedNow;
        if (_externalIsProtected != null)
        {
            // External driver mode (task 6.5): the protected state is owned by another component (e.g.
            // the Mirror's ShieldActive), not by a Shield on this object.
            protectedNow = _externalIsProtected();
        }
        else
        {
            // Default mode: protection is active while a Shield component with remaining capacity is
            // present. The Shield self-destroys when depleted/expired, so re-resolve it (cheap
            // GetComponent) rather than caching a stale reference (R6.2).
            if (_shield == null)
                _shield = GetComponent<Shield>();
            protectedNow = _shield != null && _shield.RemainingCapacity > 0f;
        }

        if (protectedNow)
            ShowVisuals();
        else
            HideVisuals();
    }

    private void LateUpdate()
    {
        if (!_visible) return;
        UpdateAura();
        UpdateIcon();
    }

    private void OnDamageAbsorbed(Actor _)
    {
        // Start (or restart) the confirm-absorb pulse. Distinct from a normal damaging hit because it
        // only fires when the shield consumed the whole hit (R6.3).
        _pulseTimer = pulseDuration;
    }

    private void OnOwnerDied(Actor _) => HideVisuals();

    private void ShowVisuals()
    {
        if (_visible) return;
        _visible = true;

        // Skip the ground aura when an external driver owns its own ground visual (Mirror's arc, R6.4).
        if (!_suppressAura)
            _aura = CombatGroundRing.Create(transform, "Protection aura", _auraColor);

        if (_iconEnabled)
            CreateIcon();
    }

    // Full teardown mirroring FrontalReflector.HideArcVisual: destroy the ring and icon and null the
    // refs so no orphan visuals survive an OnDisable or death (R6.5).
    private void HideVisuals()
    {
        if (_aura)
        {
            _aura.gameObject.SetActive(false);
            Destroy(_aura.gameObject);
            _aura = null;
        }
        if (_icon)
        {
            _icon.SetActive(false);
            Destroy(_icon);
            _icon = null;
        }
        _iconRenderer = null;
        _iconOutline = null;
        _visible = false;
    }

    private void CreateIcon()
    {
        _icon = new GameObject("Protection icon");
        _icon.transform.SetParent(transform, false);
        _icon.transform.localPosition = Vector3.up * _iconHeight;

        if (shieldIcon != null)
        {
            _iconRenderer = _icon.AddComponent<SpriteRenderer>();
            _iconRenderer.sprite = shieldIcon;
            _iconRenderer.color = _auraColor;
            _iconRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        else
        {
            // Design-sanctioned fallback: a small raised ground-ring outline shaped like a shield
            // when no sprite asset is available (e.g. a build with the field unassigned).
            _iconOutline = CombatGroundRing.Create(_icon.transform, "Protection icon outline", _auraColor);
        }
    }

    private void UpdateAura()
    {
        if (!_aura) return;
        _aura.SetColor(PulseColor(_auraColor));
        _aura.Draw(transform.position, _auraRadius, PulseWidth(0.07f));
    }

    private void UpdateIcon()
    {
        if (!_icon) return;

        // Keep the icon pinned above the head as the enemy moves.
        _icon.transform.localPosition = Vector3.up * _iconHeight;

        // Billboard toward the active camera so the flat sprite/outline always faces the player.
        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;
        if (_cameraTransform != null)
            _icon.transform.rotation = Quaternion.LookRotation(_icon.transform.position - _cameraTransform.position, Vector3.up);

        float scale = iconSize * (1f + PulseAmount() * pulseScaleBoost);

        if (_iconRenderer != null)
        {
            _icon.transform.localScale = Vector3.one * scale;
            _iconRenderer.color = PulseColor(_auraColor);
        }
        else if (_iconOutline != null)
        {
            _iconOutline.SetColor(PulseColor(_auraColor));
            DrawIconOutline(scale);
        }
    }

    // Draws a simple shield-like pentagon outline (billboarded via the parent's rotation) as the
    // icon fallback. Reuses the shared points buffer to avoid per-frame allocations.
    private void DrawIconOutline(float scale)
    {
        Transform t = _icon.transform;
        float half = scale * 0.5f;
        // Local shield silhouette points (top-center, upper corners, lower sides, point).
        _iconOutlinePoints[0] = t.TransformPoint(new Vector3(0f, half, 0f));
        _iconOutlinePoints[1] = t.TransformPoint(new Vector3(half, half * 0.4f, 0f));
        _iconOutlinePoints[2] = t.TransformPoint(new Vector3(half * 0.6f, -half * 0.6f, 0f));
        _iconOutlinePoints[3] = t.TransformPoint(new Vector3(-half * 0.6f, -half * 0.6f, 0f));
        _iconOutlinePoints[4] = t.TransformPoint(new Vector3(-half, half * 0.4f, 0f));
        _iconOutlinePoints[5] = _iconOutlinePoints[0]; // close the loop
        _iconOutline.DrawPath(_iconOutlinePoints, PulseWidth(0.05f));
    }

    /// <summary>Normalised pulse strength in [0,1], peaking right after an absorb and easing back to 0.</summary>
    private float PulseAmount() => pulseDuration > 0f ? Mathf.Clamp01(_pulseTimer / pulseDuration) : 0f;

    private Color PulseColor(Color baseColor)
    {
        // Brighten toward white during the pulse so an absorbed hit reads as a flash.
        return Color.Lerp(baseColor, Color.white, PulseAmount());
    }

    private float PulseWidth(float baseWidth) => baseWidth * (1f + PulseAmount() * 0.5f);

    // Resolves the icon height and aura radius from the enemy's renderer/collider bounds so the icon
    // clears the head/health bar and the aura fits the footprint. Falls back to sensible constants
    // when no bounds are available.
    private void ResolveBounds()
    {
        Bounds bounds = default;
        bool hasBounds = false;

        if (TryGetComponent<Renderer>(out var selfRenderer))
        {
            bounds = selfRenderer.bounds;
            hasBounds = true;
        }
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            if (r is LineRenderer) continue; // ignore ground rings / trails
            if (!hasBounds) { bounds = r.bounds; hasBounds = true; }
            else bounds.Encapsulate(r.bounds);
        }
        if (!hasBounds && TryGetComponent<Collider>(out var col))
        {
            bounds = col.bounds;
            hasBounds = true;
        }

        if (hasBounds)
        {
            float topLocal = bounds.max.y - transform.position.y;
            _iconHeight = Mathf.Max(0.5f, topLocal + iconHeadroom);
            _auraRadius = Mathf.Max(0.4f, Mathf.Max(bounds.extents.x, bounds.extents.z));
        }
        else
        {
            _iconHeight = 2f;
            _auraRadius = 0.75f;
        }
    }

    private void ResolveSprite()
    {
        if (shieldIcon != null) return;
#if UNITY_EDITOR
        // Runtime-added component can't be inspector-assigned, so load the authored sprite by path in
        // the Editor. In a build without an assigned sprite we keep the ground-ring outline fallback.
        shieldIcon = AssetDatabase.LoadAssetAtPath<Sprite>(EditorSpritePath);
#endif
    }
}
