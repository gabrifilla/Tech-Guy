using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Owns the equipped gauntlet's cast and energy; assets contain configuration only.</summary>
[RequireComponent(typeof(PlayerActor), typeof(AbilityHolder))]
public sealed class BreakerGauntletCombat : MonoBehaviour
{
    private static readonly int[] PunchStates =
    {
        Animator.StringToHash("Attack"), Animator.StringToHash("Attack2"), Animator.StringToHash("Attack3")
    };
    private readonly AsuraMomentum _momentum = new AsuraMomentum();
    private PlayerActor _player;
    private AbilityHolder _holder;
    private Animator _animator;
    private NavMeshAgent _agent;
    private WeaponScript _weapon;
    private Coroutine _cast;
    private float _savedAnimationSpeed;
    private bool _savedStopped;
    private bool _savedRotation;
    private bool _agentLocked;

    public bool IsExecuting { get; private set; }
    public float ExecutionDuration { get; private set; }
    public int Energy => _momentum.Energy;
    public bool IsReady => _momentum.IsReady;
    public bool IsEquipped => _weapon && _player && !_player.IsDead && _player.CurrentWeapon == _weapon;

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _holder = GetComponent<AbilityHolder>();
        _animator = GetComponent<Animator>();
        _agent = GetComponent<NavMeshAgent>();
        _player.Died += OnDeath;
    }

    public void Configure(WeaponScript weapon)
    {
        if (_weapon == weapon) return;
        CancelCast();
        _momentum.Reset();
        _weapon = weapon;
    }

    public bool CanUse(BreakerGauntletAbility ability)
    {
        if (!isActiveAndEnabled || !_holder.isActiveAndEnabled || !_player || _player.IsDead ||
            !_weapon || _player.CurrentWeapon != _weapon || IsExecuting || !ability ||
            ability.HitSteps == null || ability.HitSteps.Count == 0) return false;
        if (System.Array.IndexOf(_weapon.abilities, ability) < 0) return false;
        return !ability.AsuraBurst || _momentum.IsReady;
    }

    public void Use(BreakerGauntletAbility ability)
    {
        if (!CanUse(ability)) return;
        WeaponRunModifiers mods = _player.RunModifiers;
        if (ability.AsuraBurst)
        {
            _momentum.TryConsume();
            _momentum.AddEnergy(20 * (mods?.Rank(WeaponBoon.AsuraReserve) ?? 0));
        }
        else
        {
            _momentum.RegisterSkill(ability.ShockSkill);
            _momentum.AddEnergy(10 * (mods?.Rank(WeaponBoon.Momentum) ?? 0));
        }
        _cast = StartCoroutine(Execute(ability));
    }

    private IEnumerator Execute(BreakerGauntletAbility ability)
    {
        IsExecuting = true;
        if (TryGetComponent(out CharControlScript control)) control.CancelCombo();
        SequencedAreaAttackAbility.FaceMousePosition(transform);
        Vector3 direction = transform.forward;
        WeaponRunModifiers mods = _player.RunModifiers;
        int slot = System.Array.IndexOf(_weapon.abilities, ability);
        var steps = mods != null ? mods.GauntletSteps(ability, slot) : ability.HitSteps;
        float advance = ability.AdvanceDistance * (1 + .7f * (mods?.Rank(WeaponBoon.RocketAdvance) ?? 0));
        if (_animator)
        {
            _savedAnimationSpeed = _animator.speed;
            _animator.speed = ability.AnimationSpeed;
        }
        _agentLocked = _agent && _agent.enabled && _agent.isOnNavMesh;
        if (_agentLocked)
        {
            _savedStopped = _agent.isStopped;
            _savedRotation = _agent.updateRotation;
            _agent.ResetPath();
            _agent.isStopped = true;
            _agent.updateRotation = false;
        }

        try
        {
            float elapsed = 0f;
            float duration = Mathf.Max(ability.activeTime, ability.AdvanceDuration);
            foreach (AreaHitStep step in steps)
                if (step != null) duration = Mathf.Max(duration, step.delay + 0.18f);
            ExecutionDuration = duration;
            var applied = new bool[steps.Count];
            PlayPunch(0);
            while (elapsed < duration)
            {
                if (!_player || _player.IsDead || _player.CurrentWeapon != _weapon || !_holder.isActiveAndEnabled)
                    break;
                float delta = Time.deltaTime;
                if (_agentLocked && _agent.enabled && _agent.isOnNavMesh && elapsed < ability.AdvanceDuration)
                {
                    float distance = advance * Mathf.Min(delta, ability.AdvanceDuration - elapsed)
                        / Mathf.Max(0.01f, ability.AdvanceDuration);
                    Vector3 destination = transform.position + direction * distance;
                    if (_agent.Raycast(destination, out NavMeshHit edge)) destination = edge.position;
                    _agent.Move(destination - transform.position);
                }
                elapsed += delta;
                for (int i = 0; i < applied.Length; i++)
                {
                    AreaHitStep step = steps[i];
                    if (applied[i] || step == null || elapsed < step.delay) continue;
                    applied[i] = true;
                    bool finisher = i == applied.Length - 1;
                    PlayPunch(finisher ? 2 : i % 2);
                    SequencedAreaAttackAbility.ApplyHitStep(transform, _player, _weapon, step, false);
                    GauntletImpactVfx.Spawn(transform.TransformPoint(step.localOffset), direction,
                        step.rangeOverride, step.boxSize.x, ability.EffectColor,
                        ability.ShockSkill && finisher, ability.AsuraBurst && finisher, i % 2 == 0 ? -1 : 1);
                }
                yield return null;
            }
        }
        finally
        {
            RestoreCastState();
            _cast = null;
        }
    }

    private void PlayPunch(int index)
    {
        if (_animator && _animator.layerCount > 0 && _animator.HasState(0, PunchStates[index]))
            _animator.Play(PunchStates[index], 0, 0f);
    }

    private void RestoreCastState()
    {
        if (!IsExecuting) return;
        if (_animator) _animator.speed = _savedAnimationSpeed;
        if (_agentLocked && _agent)
        {
            _agent.updateRotation = _savedRotation;
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = _savedStopped;
        }
        _agentLocked = false;
        IsExecuting = false;
    }

    private void CancelCast()
    {
        if (_cast != null) StopCoroutine(_cast);
        _cast = null;
        RestoreCastState();
    }

    private void OnDeath(Actor actor) { CancelCast(); _momentum.Reset(); }
    private void OnDisable() { CancelCast(); _momentum.Reset(); }
    private void OnDestroy() { if (_player) _player.Died -= OnDeath; }

}
