using System.Collections;
using UnityEngine;

/// <summary>
/// A persistent ground area placed by a Hazard_Caster that reshapes the player's safe space over
/// time. It comes in three variants selectable per instance through serialized configuration
/// (R11.2):
///   * <see cref="ZoneKind.Fire"/> / <see cref="ZoneKind.Electric"/> — while the player occupies the
///     zone, damage is applied once per fixed interval in the 0.25–1.0s band (R11.5); occupancy stops
///     the moment the player leaves (R11.6).
///   * <see cref="ZoneKind.Slow"/> — while the player occupies the zone, movement speed is reduced by
///     a bounded 20%–60% factor (R11.7); on leaving, the exact pre-slow speed is restored (R11.8).
/// Every variant is removed once its configured 3–15s lifetime elapses (R11.9).
///
/// The MonoBehaviour stays thin. It owns only the Unity concerns — a trigger volume for occupancy
/// (<see cref="OnTriggerEnter"/> / <see cref="OnTriggerExit"/>), an interval-driven coroutine for the
/// periodic damage tick (never heavy per-frame Update logic), the ground telegraph ring, and the
/// source-keyed <c>PlayerStatModifier</c> that carries the slow. Every timing / accumulation and the
/// slow round-trip math live in the scene-free <see cref="HazardZoneModel"/> so they can be
/// property-tested without a live scene (task 8.2). The slow is carried as a modifier keyed by this
/// component (mirroring <c>EnemyFrostAura</c>), so removing it returns the player to the exact
/// composed pre-slow speed rather than a value this zone has to remember.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 8.1. Requirements: 11.5, 11.6, 11.7, 11.8, 11.9.</remarks>
[DisallowMultipleComponent]
[RequireComponent(typeof(SphereCollider))]
public sealed class HazardZone : MonoBehaviour
{
    /// <summary>The kind of effect a hazard zone applies while occupied (R11.2).</summary>
    public enum ZoneKind
    {
        /// <summary>Applies periodic damage once per interval while occupied (R11.5).</summary>
        Fire,

        /// <summary>Applies periodic damage once per interval while occupied (R11.5); a distinct flavor of the same tick behavior.</summary>
        Electric,

        /// <summary>Reduces the player's movement speed while occupied and restores it on exit (R11.7, R11.8).</summary>
        Slow
    }

    [Header("Variant")]
    [Tooltip("Fire / Electric apply periodic damage; Slow reduces player movement speed (R11.2).")]
    [SerializeField] private ZoneKind _kind = ZoneKind.Fire;

    [Header("Shape")]
    [Tooltip("Radius of the zone's trigger volume and ground telegraph, in metres.")]
    [SerializeField, Min(0.1f)] private float _radius = 2.5f;

    [Header("Periodic damage (Fire / Electric)")]
    [Tooltip("Damage applied to the player each tick while occupied (R11.5).")]
    [SerializeField, Min(0f)] private float _damagePerTick = 6f;
    [Tooltip("Seconds between damage ticks. Clamped to the 0.25–1.0s band (R11.5).")]
    [SerializeField, Range(HazardZoneModel.MinDamageInterval, HazardZoneModel.MaxDamageInterval)]
    private float _damageInterval = 0.5f;

    [Header("Slow")]
    [Tooltip("Fraction of the player's movement speed removed while occupied. Clamped to the 20%–60% band (R11.7).")]
    [SerializeField, Range(HazardZoneModel.MinSlowFraction, HazardZoneModel.MaxSlowFraction)]
    private float _slowFraction = 0.35f;

    [Header("Lifetime")]
    [Tooltip("Seconds the zone stays active before it removes itself. Clamped to the 3–15s band (R11.9).")]
    [SerializeField, Range(HazardZoneModel.MinLifetime, HazardZoneModel.MaxLifetime)]
    private float _lifetime = 6f;

    [Header("Presentation")]
    [SerializeField] private LayerMask _playerLayers = ~0;
    [SerializeField] private Color _fireColor = new Color(1f, 0.45f, 0.15f);
    [SerializeField] private Color _electricColor = new Color(0.55f, 0.8f, 1f);
    [SerializeField] private Color _slowColor = new Color(0.35f, 0.6f, 1f);

    private SphereCollider _trigger;
    private CombatGroundRing _telegraph;
    private HazardTickTimer _tickTimer;
    private HazardSlow _slow;

    private PlayerActor _occupant;         // the player currently inside the zone, if any
    private bool _slowApplied;             // whether this zone's slow modifier is currently on the occupant
    private Coroutine _damageRoutine;
    private float _removeAt;               // Time.time at which the lifetime expires
    private bool _active;

    /// <summary>The configured hazard variant (R11.2).</summary>
    public ZoneKind Kind => _kind;

    /// <summary>The zone radius, in metres.</summary>
    public float Radius => Mathf.Max(0.1f, _radius);

    /// <summary>True once the zone has activated and not yet expired.</summary>
    public bool IsActive => _active;

    private void Awake()
    {
        _trigger = GetComponent<SphereCollider>();
        _trigger.isTrigger = true;
    }

    /// <summary>
    /// Activates the zone at its current position with the serialized configuration, band-clamping
    /// the interval, slow fraction, and lifetime (R11.5, R11.7, R11.9). The pre-activation placement
    /// telegraph is owned by the caster (task 8.3); this is the "windup complete → zone active" step
    /// (R11.4). Called by <c>HazardCasterBehavior</c> after its placement telegraph completes; it may
    /// also be invoked directly for a self-contained zone.
    /// </summary>
    public void Activate()
    {
        if (_active) return;

        _tickTimer = new HazardTickTimer(_damageInterval);
        _slow = new HazardSlow(_slowFraction);

        if (!_trigger) _trigger = GetComponent<SphereCollider>();
        _trigger.isTrigger = true;
        _trigger.radius = Radius;

        _telegraph = CombatGroundRing.Create(transform, "Hazard zone", ResolveColor());
        _telegraph.Draw(transform.position, Radius, 0.12f);

        _removeAt = Time.time + HazardZoneModel.ClampLifetime(_lifetime);
        _active = true;

        // A fire/electric zone runs its ticks on an interval coroutine, not per-frame Update logic.
        if (IsDamageKind) _damageRoutine = StartCoroutine(DamageTickLoop());
    }

    private bool IsDamageKind => _kind == ZoneKind.Fire || _kind == ZoneKind.Electric;

    private Color ResolveColor()
    {
        switch (_kind)
        {
            case ZoneKind.Electric: return _electricColor;
            case ZoneKind.Slow: return _slowColor;
            default: return _fireColor;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_active) return;
        PlayerActor player = ResolvePlayer(other);
        if (!player || player == _occupant) return;

        _occupant = player;
        _tickTimer?.Enter();
        if (_kind == ZoneKind.Slow) ApplySlow(player);
    }

    private void OnTriggerExit(Collider other)
    {
        if (ResolvePlayer(other) is PlayerActor player && player == _occupant)
            ClearOccupant();
    }

    /// <summary>
    /// Interval-driven periodic damage for fire/electric zones. Sleeps in <see cref="Time.deltaTime"/>
    /// steps and asks <see cref="HazardTickTimer"/> how many whole ticks came due, applying one point
    /// of damage per tick while the player occupies the zone (R11.5). Occupancy is cleared on exit so
    /// no further ticks are applied once the player leaves (R11.6). Runs only while the zone is active.
    /// </summary>
    private IEnumerator DamageTickLoop()
    {
        while (_active)
        {
            yield return null;
            if (!_active) yield break;

            int ticks = _tickTimer.Advance(Time.deltaTime);
            if (ticks <= 0) continue;

            PlayerActor occupant = _occupant;
            if (!occupant || occupant.IsDead || _damagePerTick <= 0f) continue;

            float total = _damagePerTick * ticks;
            occupant.TakeDamage(total);
        }
    }

    private void ApplySlow(PlayerActor player)
    {
        if (_slowApplied || !player) return;
        // MoreMultiplier keyed by this component: removing it returns the player to the exact composed
        // pre-slow speed (R11.8), mirroring EnemyFrostAura. HazardSlow.SpeedMultiplier is 1 - fraction.
        player.Stats.AddModifier(new PlayerStatModifier(
            PlayerStatType.MovementSpeedMultiplier, PlayerStatModifierMode.MoreMultiplier, _slow.SpeedMultiplier), this);
        _slowApplied = true;
    }

    private void RemoveSlow(PlayerActor player)
    {
        if (!_slowApplied || !player) return;
        player.Stats.RemoveModifiersFrom(this);   // restores the exact pre-slow value (R11.8)
        _slowApplied = false;
    }

    private void ClearOccupant()
    {
        if (_occupant)
        {
            _tickTimer?.Exit();                 // stop owing ticks once outside (R11.6)
            if (_kind == ZoneKind.Slow) RemoveSlow(_occupant);   // restore speed on exit (R11.8)
        }
        _occupant = null;
    }

    private void Update()
    {
        // A single lightweight lifetime check; all damage timing lives in the interval coroutine.
        if (_active && Time.time >= _removeAt) Remove();
    }

    private void Remove()
    {
        _active = false;
        if (_damageRoutine != null) { StopCoroutine(_damageRoutine); _damageRoutine = null; }
        ClearOccupant();
        if (_telegraph)
        {
            _telegraph.gameObject.SetActive(false);
            Destroy(_telegraph.gameObject);
            _telegraph = null;
        }
        Destroy(gameObject);   // remove the zone once its lifetime elapses (R11.9)
    }

    private void OnDisable()
    {
        // If the zone is torn down for any reason, never leave the player's speed permanently slowed:
        // the slow is keyed by this component, so restoring it is a single removal (R11.8).
        ClearOccupant();
    }

    private PlayerActor ResolvePlayer(Collider other)
    {
        if (!other) return null;
        if (((1 << other.gameObject.layer) & _playerLayers.value) == 0 && _playerLayers.value != ~0) return null;
        PlayerActor player = other.GetComponentInParent<PlayerActor>();
        if (player) return player;
        return other.GetComponentInChildren<PlayerActor>();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = ResolveColor();
        Gizmos.DrawWireSphere(transform.position, Radius);
    }
}
