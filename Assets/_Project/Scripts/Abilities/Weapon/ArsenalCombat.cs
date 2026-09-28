using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerActor))]
public sealed class ArsenalCombat : MonoBehaviour
{
    private PlayerActor _player;
    private NavMeshAgent _agent;
    private Coroutine _cast;
    private SkillAnimationPlayer _animation;
    private bool _locked, _wasStopped, _wasRotating;
    public bool IsExecuting { get; private set; }
    public float ExecutionDuration { get; private set; }

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _agent = GetComponent<NavMeshAgent>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
        _player.Died += OnDeath;
    }

    public bool CanUse(ArsenalAbility ability) => isActiveAndEnabled && !IsExecuting &&
        !_player.IsDead && _player.CurrentWeapon && ability &&
        System.Array.IndexOf(_player.CurrentWeapon.abilities, ability) >= 0;

    public void Use(ArsenalAbility ability)
    {
        if (CanUse(ability)) _cast = StartCoroutine(Execute(ability));
    }

    private IEnumerator Execute(ArsenalAbility ability)
    {
        IsExecuting = true;
        WeaponScript weapon = _player.CurrentWeapon;
        int slot = System.Array.IndexOf(weapon.abilities, ability);
        ArsenalCastPlan plan = _player.RunModifiers?.Plan(ability, slot) ?? new ArsenalCastPlan(ability);
        ExecutionDuration = plan.Windup + plan.Hits * plan.Interval;
        SequencedAreaAttackAbility.FaceMousePosition(transform);
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        Vector3 rainCenter = origin + direction * plan.Range;
        // Rain is clamped to the cursor distance, with a fixed ground target for the whole cast.
        if (Camera.main && UnityEngine.InputSystem.Mouse.current != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            if (new Plane(Vector3.up, origin).Raycast(ray, out float distance))
                rainCenter = origin + Vector3.ClampMagnitude(ray.GetPoint(distance) - origin, plan.Range);
        }
        _locked = _agent && _agent.enabled && _agent.isOnNavMesh;
        if (_locked)
        {
            _wasStopped = _agent.isStopped;
            _wasRotating = _agent.updateRotation;
            _agent.ResetPath();
            _agent.isStopped = true;
            _agent.updateRotation = false;
        }
        SkillMotion motion = weapon.FiresArrows ? (ability.Kind==ArsenalSkillKind.Rain ? SkillMotion.BowRain : SkillMotion.BowShot) :
            ability.Kind==ArsenalSkillKind.Thrust ? SkillMotion.SpearThrust :
            WeaponRunModifiers.Identify(weapon)==RunWeaponFamily.Gauntlet ? SkillMotion.Slam : SkillMotion.SpearSweep;
        try
        {
            float elapsed = 0f;
            for (int i = 0; i < plan.Hits; i++)
            {
                float impact = plan.Windup + i*plan.Interval;
                float start = i==0 ? 0 : impact-plan.Interval+Mathf.Min(.04f,plan.Interval*.25f);
                float end = i==plan.Hits-1 ? ExecutionDuration : impact+Mathf.Min(.04f,plan.Interval*.25f);
                while (elapsed < impact)
                {
                    _animation.Strike(motion,elapsed,start,impact,end);
                    yield return null;
                    elapsed += Time.deltaTime;
                }
                if (_player.IsDead || _player.CurrentWeapon != weapon) yield break;
                _animation.Contact(motion);
                if (ability.Kind == ArsenalSkillKind.Arrow || ability.Kind == ArsenalSkillKind.Volley)
                {
                    int arrows = plan.Arrows;
                    for (int arrow = 0; arrow < arrows; arrow++)
                    {
                        Vector3 aim = Quaternion.AngleAxis((arrow - (arrows - 1) * .5f) * Mathf.Min(9f, 100f / Mathf.Max(1, arrows - 1)), Vector3.up) * direction;
                        ArsenalProjectile.Fire(_player, origin + Vector3.up, aim, weapon.attackDamage,
                            plan.Damage, plan.Range, ability.Piercing, ability.AccentColor);
                    }
                }
                else
                {
                    bool radial = ability.Kind != ArsenalSkillKind.Thrust;
                    if (plan.TrackCursor && Camera.main && UnityEngine.InputSystem.Mouse.current != null)
                    {
                        Ray cursor = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
                        if (new Plane(Vector3.up, origin).Raycast(cursor, out float cursorDistance))
                            rainCenter = origin + Vector3.ClampMagnitude(cursor.GetPoint(cursorDistance) - origin, plan.Range);
                    }
                    Vector3 center = ability.Kind == ArsenalSkillKind.Rain ? rainCenter : origin;
                    if (plan.Travel) center += direction * (i * 1.5f);
                    // Sphere helper raises its center by the radius; keep large sweeps at torso height.
                    center.y += radial ? 1f - plan.Width : 0f;
                    // Q sweeps stagger and chip stance; breaking a weak mob's stance briefly stuns it.
                    var reaction = new HitReactionRequest(_player, center, direction, HitReactionType.Stagger,
                        HitStrength.Medium, 40f, StanceBreakEffect.Stun, 0.5f, 1f);
                    for (int branch = 0; branch < plan.Directions; branch++)
                    {
                        Vector3 aim = Quaternion.AngleAxis((branch - (plan.Directions - 1) * .5f) * 25f, Vector3.up) * direction;
                        _player.TryApplyAreaDamage(center, aim, radial ? .01f : plan.Range,
                            new Vector3(plan.Width, 2f, plan.Range), radial ? AreaHitShape.Sphere : AreaHitShape.Box,
                            plan.Width, Physics.DefaultRaycastLayers, weapon.attackDamage, plan.Damage, 0f, reaction, false);
                        AttackAreaSwoosh.Spawn((radial ? center + Vector3.up * (plan.Width - 1f) - aim * plan.Width : origin) + Vector3.up * .08f,
                            aim, radial ? plan.Width * 2 : plan.Range,
                            new Vector3(radial ? plan.Width * 2 : plan.Width, 1, radial ? plan.Width * 2 : plan.Range), ability.AccentColor, .25f);
                    }
                    if (plan.WaveMultiplier > 0)
                        ArsenalProjectile.Fire(_player, origin + Vector3.up, direction, weapon.attackDamage,
                            plan.Damage * plan.WaveMultiplier, plan.Range * 2, true, ability.AccentColor);
                }
                while (elapsed < end)
                {
                    yield return null;
                    elapsed += Time.deltaTime;
                    _animation.Strike(motion,elapsed,start,impact,end);
                }
            }
        }
        finally { Release(); }
    }

    public void Cancel()
    {
        if (_cast != null) StopCoroutine(_cast);
        _cast = null;
        Release();
    }

    private void Release()
    {
        if (_animation) _animation.Release();
        if (_locked && _agent && _agent.enabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = _wasStopped;
            _agent.updateRotation = _wasRotating;
        }
        _locked = false;
        IsExecuting = false;
    }
    private void OnDeath(Actor actor) => Cancel();
    private void OnDisable() => Cancel();
    private void OnDestroy() { if (_player) _player.Died -= OnDeath; }
}
