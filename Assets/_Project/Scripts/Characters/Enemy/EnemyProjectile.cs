using UnityEngine;

/// <summary>
/// Reusable enemy projectile shared by every ranged archetype (Shooter / Spread_Shooter / Sniper via
/// the <see cref="PathKind.Straight"/> path, Bomber via the <see cref="PathKind.Arced"/> path). It
/// generalizes the older FrostBolt travel-over-frames pattern from <c>EnemyCombatActions.Bolt</c>:
/// it moves across one or more frames at its configured speed (never resolving on the firing frame),
/// clips its travel against static geometry, applies its damage to the target at most once, and then
/// tears itself down within the same frame — releasing the runtime material and any effect it created.
///
/// The MonoBehaviour stays thin. Every travel / hit / cleanup <em>decision</em> lives in the scene-free
/// <see cref="ProjectileMotion"/> state machine (property-tested in task 4.2); this component only
/// performs the Unity work each frame — moving the transform, sphere-casting for geometry, testing the
/// target overlap, drawing the arced ground telegraph, and destroying itself with its material released.
///
/// Owner cascade: the projectile subscribes to its owner's <see cref="Actor.Died"/> and, if the owner
/// dies or is disabled while the projectile is in flight, marks itself for destruction and applies no
/// further damage (R19.6).
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 4.1. Requirements: 19.1, 19.2, 19.3, 19.4, 19.5, 19.6.</remarks>
[DisallowMultipleComponent]
public sealed class EnemyProjectile : MonoBehaviour
{
    /// <summary>How the projectile travels to its resolution.</summary>
    public enum PathKind
    {
        /// <summary>Flies forward in a straight line and hits the first target its volume overlaps (Shooter / Spread_Shooter / Sniper).</summary>
        Straight,

        /// <summary>Follows a parabola to a fixed ground point and does a single area check on landing (Bomber).</summary>
        Arced
    }

    private const string UnlitShader = "Universal Render Pipeline/Unlit";
    private const float GeometryProbeRadius = 0.2f;

    private ProjectileMotion _motion;
    private Actor _owner;
    private Actor _target;
    private float _damage;
    private float _speed;
    private float _radius;
    private float _groundImpactRadius;
    private PathKind _path;

    private Vector3 _direction;      // straight path travel direction (flattened)
    private Vector3 _launchPoint;    // arced path start
    private Vector3 _groundPoint;    // arced path landing point
    private float _arcHeight;        // arced path apex height
    private float _flightDuration;   // arced path total flight time
    private float _flightElapsed;    // arced path elapsed flight time

    private Renderer _renderer;
    private Material _runtimeMaterial;
    private CombatGroundRing _impactTelegraph;
    private Color _color = Color.white;

    private bool _configured;
    private bool _ownerSubscribed;

    /// <summary>The projectile's resolution state machine. Null until <see cref="Configure"/> runs.</summary>
    public ProjectileMotion Motion => _motion;

    /// <summary>True once the projectile has applied its single point of damage.</summary>
    public bool DamageApplied => _motion != null && _motion.DamageApplied;

    /// <summary>
    /// Configures the projectile before it starts travelling. Must be called immediately after the
    /// component is created; the projectile does nothing until it is configured.
    /// </summary>
    /// <param name="owner">The enemy that fired the projectile; its death / disable cascades to this projectile (R19.6).</param>
    /// <param name="target">The actor the projectile is aimed at (typically the player).</param>
    /// <param name="damage">Damage applied to the target on hit, exactly once (R19.2).</param>
    /// <param name="speed">Travel speed in metres per second (straight path).</param>
    /// <param name="range">Cumulative travel range before the projectile expires (R19.3).</param>
    /// <param name="radius">Overlap radius used to detect the target along the straight travel segment.</param>
    /// <param name="path">Straight (line) or Arced (lob to a ground point).</param>
    /// <param name="color">Tint for the runtime material and, for the arced path, the impact telegraph.</param>
    /// <param name="groundImpactRadius">Impact-area radius for the arced path; ignored on the straight path.</param>
    public void Configure(Actor owner, Actor target, float damage, float speed, float range,
        float radius, PathKind path, Color color, float groundImpactRadius = 1f)
    {
        _owner = owner;
        _target = target;
        _damage = Mathf.Max(0f, damage);
        _speed = Mathf.Max(0.01f, speed);
        _radius = Mathf.Max(0.05f, radius);
        _groundImpactRadius = Mathf.Max(0.1f, groundImpactRadius);
        _path = path;
        _color = color;
        _motion = new ProjectileMotion(range);

        BuildVisual();
        SubscribeOwner();

        if (path == PathKind.Straight) SetupStraight();
        else SetupArced();

        _configured = true;
    }

    private void SetupStraight()
    {
        Vector3 forward = _target ? _target.transform.position - transform.position : transform.forward;
        forward.y = 0f;
        _direction = forward.sqrMagnitude > 0.0001f ? forward.normalized : transform.forward;
    }

    private void SetupArced()
    {
        _launchPoint = transform.position;
        // Land on the target's current ground position; fall back to straight ahead when no target.
        _groundPoint = _target ? _target.transform.position : transform.position + transform.forward * _motion.Range;
        _groundPoint.y = _launchPoint.y;

        float distance = Vector3.Distance(_launchPoint, _groundPoint);
        _flightDuration = Mathf.Max(0.15f, distance / _speed);
        _arcHeight = Mathf.Clamp(distance * 0.5f, 1.5f, 8f);
        _flightElapsed = 0f;

        // Ground-area impact telegraph, shown for the full flight and intensifying on approach (R10.4-adjacent).
        _impactTelegraph = CombatGroundRing.Create(null, "Bomber impact", _color);
        _impactTelegraph.Draw(_groundPoint, _groundImpactRadius, 0.12f);
    }

    private void BuildVisual()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer == null)
        {
            var mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = mesh.GetComponent<Collider>();
            if (col) Destroy(col); // collision handled analytically; no physics trigger needed
            mesh.transform.SetParent(transform, false);
            mesh.transform.localScale = Vector3.one * (_radius * 2f);
            _renderer = mesh.GetComponent<Renderer>();
        }

        _runtimeMaterial = new Material(Shader.Find(UnlitShader));
        _runtimeMaterial.SetColor("_BaseColor", _color);
        if (_renderer) _renderer.sharedMaterial = _runtimeMaterial;
    }

    private void SubscribeOwner()
    {
        if (_owner && !_ownerSubscribed)
        {
            _owner.Died += OnOwnerDied;
            _ownerSubscribed = true;
        }
    }

    private void UnsubscribeOwner()
    {
        if (_owner && _ownerSubscribed) _owner.Died -= OnOwnerDied;
        _ownerSubscribed = false;
    }

    private void OnOwnerDied(Actor owner)
    {
        // Owner death cascades to this in-flight projectile: stop, no further damage (R19.6).
        _motion?.MarkOwnerGone();
        Destroy(gameObject);
    }

    private void Update()
    {
        if (!_configured || _motion == null) return;

        // The owner disappeared without firing Died (e.g. object destroyed): cascade the same way.
        if (_ownerSubscribed && !_owner)
        {
            _motion.MarkOwnerGone();
            Destroy(gameObject);
            return;
        }

        if (_path == PathKind.Straight) StepStraight();
        else StepArced();
    }

    private void StepStraight()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Vector3 start = transform.position;
        float intended = _speed * dt;

        // Clip the intended step against static geometry, mirroring EnemyCombatActions.ClipTravel.
        float allowed = ClipTravel(start, _direction, intended, GeometryProbeRadius);
        bool blocked = allowed + 0.001f < intended;

        Vector3 finish = start + _direction * allowed;

        // Detect the target along the segment actually travelled (matches SegmentContains single-hit).
        bool overlaps = _target && !_target.IsDead &&
            EnemyCombatActions.SegmentContains(_target.transform.position, start, finish, _radius);

        ProjectileStepResult result = _motion.Step(intended, new ProjectileHit(allowed, overlaps, blocked));

        transform.position = finish;

        if (result.AppliedDamage && _target && !_target.IsDead)
            _target.TakeDamage(_damage);

        if (result.ShouldDestroy)
            Destroy(gameObject);
    }

    private void StepArced()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        _flightElapsed += dt;
        float t = Mathf.Clamp01(_flightElapsed / _flightDuration);

        // Parabolic position: linear ground interpolation plus a sine-shaped vertical arc.
        Vector3 ground = Vector3.Lerp(_launchPoint, _groundPoint, t);
        float height = _arcHeight * Mathf.Sin(t * Mathf.PI);
        transform.position = ground + Vector3.up * height;

        // Intensify the ground telegraph toward its impact appearance as the projectile approaches (R10.4).
        if (_impactTelegraph) _impactTelegraph.SetColor(Color.Lerp(_color, Color.white, 0.65f * t));

        if (t < 1f) return;

        // Landed: single area check over the impact radius, damage at most once (R19.2, arced path).
        bool inArea = _target && !_target.IsDead &&
            (_target.transform.position - _groundPoint).sqrMagnitude <= _groundImpactRadius * _groundImpactRadius;

        bool applied = _motion.ResolveImpact(inArea);
        if (applied && _target && !_target.IsDead)
            _target.TakeDamage(_damage);

        Destroy(gameObject); // projectile + its impact telegraph removed on land (R10.5)
    }

    /// <summary>
    /// Returns how far the projectile may move from <paramref name="start"/> along
    /// <paramref name="direction"/> before static geometry blocks it, ignoring the owner and any
    /// actors. Mirrors the private clip used by the FrostBolt / Charge travel loops.
    /// </summary>
    private float ClipTravel(Vector3 start, Vector3 direction, float distance, float probeRadius)
    {
        float allowed = distance;
        foreach (var hit in Physics.SphereCastAll(start, probeRadius, direction, distance,
                     Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (_owner && hit.collider.transform.IsChildOf(_owner.transform)) continue;
            if (hit.collider.transform.IsChildOf(transform)) continue;
            if (hit.collider.GetComponentInParent<Actor>()) continue; // actors resolve via overlap, not geometry
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - 0.02f));
        }
        return allowed;
    }

    private void OnDisable()
    {
        // A disabled projectile stops applying damage; the owner cascade uses the same path (R19.6).
        _motion?.MarkOwnerGone();
    }

    private void OnDestroy()
    {
        // Release everything created at runtime within the same frame the projectile is destroyed,
        // leaving no orphaned material or effect instance (R19.5).
        UnsubscribeOwner();
        if (_impactTelegraph)
        {
            _impactTelegraph.gameObject.SetActive(false);
            Destroy(_impactTelegraph.gameObject);
            _impactTelegraph = null;
        }
        if (_runtimeMaterial)
        {
            Destroy(_runtimeMaterial);
            _runtimeMaterial = null;
        }
    }

    /// <summary>
    /// Convenience factory: creates a projectile GameObject at <paramref name="origin"/> facing
    /// <paramref name="direction"/> and configures it in one call. Callers (EnemyCombatActions,
    /// HazardCasterBehavior) use this so no projectile is authored per shot.
    /// </summary>
    public static EnemyProjectile Spawn(Vector3 origin, Vector3 direction, Actor owner, Actor target,
        float damage, float speed, float range, float radius, PathKind path, Color color,
        float groundImpactRadius = 1f)
    {
        var go = new GameObject("Enemy projectile");
        go.transform.position = origin;
        if (direction.sqrMagnitude > 0.0001f)
            go.transform.rotation = Quaternion.LookRotation(direction.normalized);

        var projectile = go.AddComponent<EnemyProjectile>();
        projectile.Configure(owner, target, damage, speed, range, radius, path, color, groundImpactRadius);
        return projectile;
    }
}
