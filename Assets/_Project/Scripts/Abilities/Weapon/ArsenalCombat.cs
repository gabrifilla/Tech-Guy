using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerActor))]
public sealed class ArsenalCombat : MonoBehaviour
{
    private PlayerActor _player;
    private NavMeshAgent _agent;
    private Coroutine _cast;
    private bool _locked, _wasStopped, _wasRotating;
    public bool IsExecuting { get; private set; }

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _agent = GetComponent<NavMeshAgent>();
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
        SequencedAreaAttackAbility.FaceMousePosition(transform);
        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        Vector3 rainCenter = origin + direction * ability.Range;
        // Rain is clamped to the cursor distance, with a fixed ground target for the whole cast.
        if (Camera.main && UnityEngine.InputSystem.Mouse.current != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
            if (new Plane(Vector3.up, origin).Raycast(ray, out float distance))
                rainCenter = origin + Vector3.ClampMagnitude(ray.GetPoint(distance) - origin, ability.Range);
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
        if (TryGetComponent(out Animator animator))
        {
            int attack = Animator.StringToHash("Attack");
            if (animator.HasState(0, attack)) animator.Play(attack, 0, 0);
        }
        try
        {
            yield return new WaitForSeconds(ability.Windup);
            for (int i = 0; i < ability.Hits; i++)
            {
                if (_player.IsDead || _player.CurrentWeapon != weapon) yield break;
                if (ability.Kind == ArsenalSkillKind.Arrow || ability.Kind == ArsenalSkillKind.Volley)
                {
                    int arrows = ability.Kind == ArsenalSkillKind.Volley ? 5 : 1;
                    for (int arrow = 0; arrow < arrows; arrow++)
                    {
                        Vector3 aim = Quaternion.AngleAxis((arrow - (arrows - 1) * .5f) * 9f, Vector3.up) * direction;
                        ArsenalProjectile.Fire(_player, origin + Vector3.up, aim, weapon.attackDamage,
                            ability.DamageMultiplier, ability.Range, ability.Piercing, ability.AccentColor);
                    }
                }
                else
                {
                    bool radial = ability.Kind != ArsenalSkillKind.Thrust;
                    Vector3 center = ability.Kind == ArsenalSkillKind.Rain ? rainCenter : origin;
                    // Sphere helper raises its center by the radius; keep large sweeps at torso height.
                    center.y += radial ? 1f - ability.Width : 0f;
                    // Q sweeps stagger and chip stance; breaking a weak mob's stance briefly stuns it.
                    var reaction = new HitReactionRequest(_player, center, direction, HitReactionType.Stagger,
                        HitStrength.Medium, 40f, StanceBreakEffect.Stun, 0.5f, 1f);
                    _player.TryApplyAreaDamage(center, direction, radial ? .01f : ability.Range,
                        new Vector3(ability.Width, 2f, ability.Range), radial ? AreaHitShape.Sphere : AreaHitShape.Box,
                        ability.Width, Physics.DefaultRaycastLayers, weapon.attackDamage,
                        ability.DamageMultiplier, 0f, reaction, false);
                    AttackAreaSwoosh.Spawn((radial ? center + Vector3.up * (ability.Width - 1f) - direction * ability.Width : origin) + Vector3.up * .08f,
                        direction, radial ? ability.Width * 2 : ability.Range,
                        new Vector3(radial ? ability.Width * 2 : ability.Width, 1, radial ? ability.Width * 2 : ability.Range),
                        ability.AccentColor, .25f);
                }
                yield return new WaitForSeconds(ability.Interval);
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
