using UnityEngine;

/// <summary>
/// The Mirror archetype's frontal reflective shield (R18). It holds a fixed protected arc (90–180°)
/// centred on the Mirror's forward direction and cooperates with the player's damage / projectile
/// path to reduce the damage a head-on projectile deals:
/// <list type="bullet">
///   <item>A <b>player projectile</b> that strikes from within the frontal arc has at most 25% of its
///   damage applied (R18.3).</item>
///   <item>A hit from <b>outside</b> the arc applies full damage (R18.4).</item>
///   <item>An <b>area attack</b> applies full damage regardless of the arc (R18.5).</item>
/// </list>
/// The arc is disabled while the Mirror is control-locked from a stance break and re-enabled the
/// moment it regains control (R18.6/R18.7), and a persistent ground visual communicates the protected
/// arc without the player having to target the Mirror (R18.8).
///
/// The MonoBehaviour stays thin: the arc / reflection <em>math</em> lives in the scene-free
/// <see cref="FrontalArcReflection"/> class so it can be property-tested without a live scene
/// (task 8.9). This component only owns the Unity concerns — resolving the owner and reaction
/// controller, tracking the shield-active state off the control lock, drawing the arc, and exposing
/// the cooperative <see cref="ResolveIncomingDamage"/> hook the damage source calls.
///
/// Cooperation model: <see cref="Actor.TakeDamage"/> carries no attacker direction or attack shape,
/// so rather than widen that shared signature the reflector exposes
/// <see cref="ResolveIncomingDamage"/>. A player damage source (e.g. a projectile) that knows its own
/// world position and whether it is a projectile / area attack calls this to obtain the already-reduced
/// damage it should pass to <see cref="Actor.TakeDamage"/>. When the target has no reflector the source
/// applies full damage as before, so every non-Mirror enemy is unaffected.
///
/// Protection icon (combat-balance-tuning task 6.5): the Mirror reuses the shared
/// <see cref="ProtectionIndicator"/> in icon-only mode so the same shield icon appears above its head
/// while <see cref="ShieldActive"/>, without duplicating a ground aura on top of its frontal arc
/// (R6.4). The reflection math and the arc visual below are unchanged.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 8.8; combat-balance-tuning, task 6.5. Requirements: 18.2, 18.3, 18.4, 18.5, 18.6, 18.7, 18.8, 6.4.</remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(Actor))]
public sealed class FrontalReflector : MonoBehaviour
{
    [Header("Protected arc")]
    [Tooltip("Frontal arc protected by the shield, in degrees. Clamped to the design range 90–180 (R18.2).")]
    [SerializeField, Range(FrontalArcReflection.MinArcDegrees, FrontalArcReflection.MaxArcDegrees)]
    private float arcDegrees = 120f;

    [Header("Arc visual")]
    [Tooltip("Colour of the persistent arc visual (R18.8).")]
    [SerializeField] private Color arcColor = new Color(0.4f, 0.8f, 1f, 1f);

    [Tooltip("Radius of the drawn arc fan, in metres. Purely cosmetic; does not affect the reflection math.")]
    [SerializeField, Min(0.5f)] private float visualRadius = 1.5f;

    private Actor _owner;
    private CombatReactionController _reaction;
    private CombatGroundRing _arcVisual;
    private readonly Vector3[] _arcPoints = new Vector3[24];

    private bool _shieldActive = true;
    private bool _visualShown;

    /// <summary>The protected arc width in degrees, clamped to the design range 90–180 (R18.2).</summary>
    public float ArcDegrees => FrontalArcReflection.ClampArc(arcDegrees);

    /// <summary>
    /// True while the frontal shield is protecting. False while the Mirror is control-locked from a
    /// stance break (R18.6) and restored when it regains control (R18.7).
    /// </summary>
    public bool ShieldActive => _shieldActive;

    private ProtectionIndicator _protectionIndicator;

    private void Awake()
    {
        _owner = GetComponent<Actor>();
        _reaction = GetComponent<CombatReactionController>();
        if (!_owner)
            Debug.LogError($"{nameof(FrontalReflector)} on '{name}' requires an {nameof(Actor)}; the frontal shield is disabled.", this);

        EnsureProtectionIndicator();
    }

    /// <summary>
    /// Ensures a <see cref="ProtectionIndicator"/> on this Mirror and drives it in icon-only mode
    /// (task 6.5). The Mirror already owns a ground visual (its frontal arc), so the shared indicator
    /// shows only the shield icon while <see cref="ShieldActive"/> and its ground aura is suppressed to
    /// avoid duplicating a ring on top of the arc (R6.4). The indicator itself honours
    /// <c>ProtectionIconEnabled</c> from the config, so nothing extra shows when the icon is disabled.
    /// </summary>
    private void EnsureProtectionIndicator()
    {
        // ProtectionIndicator is DisallowMultipleComponent, so reuse an existing one (e.g. granted by a
        // ShieldSupportBehavior) rather than adding a duplicate.
        if (!TryGetComponent(out _protectionIndicator))
            _protectionIndicator = gameObject.AddComponent<ProtectionIndicator>();

        _protectionIndicator.ConfigureExternalSource(() => _shieldActive, suppressAura: true);
    }

    private void OnValidate()
    {
        // Keep the serialized arc inside the design range even if edited outside the slider (R18.2).
        arcDegrees = FrontalArcReflection.ClampArc(arcDegrees);
    }

    private void OnEnable()
    {
        RefreshShieldState();
        ShowArcVisual();
    }

    private void OnDisable()
    {
        HideArcVisual();
    }

    private void Update()
    {
        // The shield follows the control lock: disabled during a stance-break CC, re-enabled the frame
        // control returns (R18.6/R18.7). Driven off IsControlLocked, which the reaction controller sets
        // when a stance break applies a stun / knock-up and clears when the lock ends.
        RefreshShieldState();

        // The arc visual tracks the Mirror's facing and is only shown while the shield is active
        // (a broken shield stops protecting, so the read must reflect that) — but the marker itself
        // remains present while the Mirror is alive (R18.8).
        UpdateArcVisual();
    }

    private void RefreshShieldState()
    {
        bool controlLocked = _reaction && _reaction.IsControlLocked;
        _shieldActive = !controlLocked;
    }

    /// <summary>
    /// Cooperative damage hook the player's damage source calls when it strikes this Mirror. Given the
    /// world position the hit came from and the attack's shape, it returns the damage that should
    /// actually be applied after frontal-arc reflection. The reduction only ever lowers the incoming
    /// damage; a hit from outside the arc, an area attack, a non-projectile hit, or a hit while the
    /// shield is inactive returns the full <paramref name="incomingDamage"/> unchanged.
    /// </summary>
    /// <param name="incomingDamage">The damage the source would otherwise apply.</param>
    /// <param name="hitSourcePosition">World position the hit approaches from (e.g. the projectile's position at impact).</param>
    /// <param name="isPlayerProjectile">True when the hit is a player projectile (only projectiles are reflected, R18.3/R18.4).</param>
    /// <param name="isAreaAttack">True when the hit is an area attack (area always applies full damage, R18.5).</param>
    /// <returns>The (possibly reduced) damage to apply to this Mirror.</returns>
    public float ResolveIncomingDamage(float incomingDamage, Vector3 hitSourcePosition, bool isPlayerProjectile, bool isAreaAttack)
    {
        if (incomingDamage <= 0f) return Mathf.Max(0f, incomingDamage);

        Vector3 approach = hitSourcePosition - transform.position;
        float fraction = FrontalArcReflection.DamageFraction(
            _shieldActive, isPlayerProjectile, isAreaAttack, transform.forward, approach, ArcDegrees);
        return incomingDamage * fraction;
    }

    private void ShowArcVisual()
    {
        if (_visualShown) return;
        _arcVisual = CombatGroundRing.Create(transform, "Mirror frontal arc", arcColor);
        _visualShown = true;
        UpdateArcVisual();
    }

    private void HideArcVisual()
    {
        if (_arcVisual)
        {
            _arcVisual.gameObject.SetActive(false);
            Destroy(_arcVisual.gameObject);
            _arcVisual = null;
        }
        _visualShown = false;
    }

    private void UpdateArcVisual()
    {
        if (!_arcVisual) return;

        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 1e-6f) forward = Vector3.forward;
        forward.Normalize();

        float half = ArcDegrees * 0.5f;
        Vector3 origin = transform.position + Vector3.up * 0.05f;

        // Fan of points sweeping the protected arc from -half to +half around the Mirror's forward,
        // returning to the origin so the drawn shape reads as a wedge in front of the Mirror.
        int span = _arcPoints.Length - 1;
        for (int i = 0; i < span; i++)
        {
            float t = span > 1 ? (float)i / (span - 1) : 0f;
            float angle = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * forward;
            _arcPoints[i] = origin + dir * visualRadius;
        }
        _arcPoints[span] = origin; // close the wedge back to the Mirror

        // A broken shield reads as dimmed so the protection state stays legible (R18.6/R18.8).
        _arcVisual.SetColor(_shieldActive ? arcColor : arcColor * 0.35f);
        _arcVisual.DrawPath(_arcPoints);
    }
}
