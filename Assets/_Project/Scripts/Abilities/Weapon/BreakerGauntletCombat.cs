using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Owns the equipped gauntlet's cast and energy; assets contain configuration only.</summary>
[RequireComponent(typeof(PlayerActor), typeof(AbilityHolder))]
public sealed class BreakerGauntletCombat : MonoBehaviour
{
    private readonly AsuraMomentum _momentum = new AsuraMomentum();
    private PlayerActor _player;
    private AbilityHolder _holder;
    private SkillAnimationPlayer _animation;
    private NavMeshAgent _agent;
    private WeaponScript _weapon;
    private Coroutine _cast;
    private BreakerGauntletAbility _activeAbility;
    private System.Collections.Generic.IReadOnlyList<AreaHitStep> _activeSteps;
    private float _elapsed;
    private bool _finisherApplied;
    private bool _savedStopped;
    private bool _savedRotation;
    private bool _agentLocked;

    public bool IsExecuting { get; private set; }
    public bool CanDodgeCancel => IsExecuting && _activeAbility && _activeAbility.AsuraBurst && _elapsed >= .25f;
    public bool IsAsuraActive => IsExecuting && _activeAbility && _activeAbility.AsuraBurst;
    public float ExecutionDuration { get; private set; }
    public int Energy => _momentum.Energy;
    public bool IsReady => _momentum.IsReady;
    public bool IsEquipped => _weapon && _player && !_player.IsDead && _player.CurrentWeapon == _weapon;

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _holder = GetComponent<AbilityHolder>();
        _animation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
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
        _activeAbility = ability; _elapsed = 0; _finisherApplied = false;
        if (ability.AsuraBurst) _player.SetDamageTakenMultiplier(this,.15f);
        if (TryGetComponent(out CharControlScript control)) control.CancelCombo();
        SequencedAreaAttackAbility.FaceMousePosition(transform);
        Vector3 direction = transform.forward;
        WeaponRunModifiers mods = _player.RunModifiers;
        int slot = System.Array.IndexOf(_weapon.abilities, ability);
        var steps = mods != null ? mods.GauntletSteps(ability, slot) : ability.HitSteps;
        _activeSteps = steps;
        float advance = ability.AdvanceDistance * (1 + .7f * (mods?.Rank(WeaponBoon.RocketAdvance) ?? 0));
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
            int animatedStep = 0;
            while (elapsed < duration)
            {
                if (!_player || _player.IsDead || _player.CurrentWeapon != _weapon || !_holder.isActiveAndEnabled)
                    break;
                float delta = Time.deltaTime;
                if (delta <= 0f) { yield return null; continue; }
                if (ability.AsuraBurst)
                {
                    // Let the earned burst follow the fight instead of firing into empty space.
                    Quaternion before = transform.rotation;
                    SequencedAreaAttackAbility.FaceMousePosition(transform);
                    transform.rotation = Quaternion.RotateTowards(before,transform.rotation,180f*delta);
                    direction = transform.forward;
                }
                if (_agentLocked && _agent.enabled && _agent.isOnNavMesh && elapsed < ability.AdvanceDuration)
                {
                    float distance = advance * Mathf.Min(delta, ability.AdvanceDuration - elapsed)
                        / Mathf.Max(0.01f, ability.AdvanceDuration);
                    Vector3 destination = transform.position + direction * distance;
                    if (_agent.Raycast(destination, out NavMeshHit edge)) destination = edge.position;
                    _agent.Move(destination - transform.position);
                }
                elapsed += delta;
                _elapsed = elapsed;
                while (animatedStep+1 < steps.Count && elapsed > steps[animatedStep].delay + Mathf.Min(.04f,(steps[animatedStep+1].delay-steps[animatedStep].delay)*.25f)) animatedStep++;
                float start = animatedStep == 0 ? 0 : steps[animatedStep-1].delay + Mathf.Min(.04f,(steps[animatedStep].delay-steps[animatedStep-1].delay)*.25f);
                float end = animatedStep+1 < steps.Count ? steps[animatedStep].delay + Mathf.Min(.04f,(steps[animatedStep+1].delay-steps[animatedStep].delay)*.25f) : duration;
                _animation.Strike(Motion(ability,animatedStep),elapsed,start,steps[animatedStep].delay,end);
                for (int i = 0; i < applied.Length; i++)
                {
                    AreaHitStep step = steps[i];
                    if (applied[i] || step == null || elapsed < step.delay) continue;
                    applied[i] = true;
                    Impact(ability,step,i);
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

    private static SkillMotion Motion(BreakerGauntletAbility ability,int index)
    {
        if (ability.ShockSkill && index >= ability.HitSteps.Count-1) return SkillMotion.Slam;
        return index%2==0 ? SkillMotion.PunchLeft : SkillMotion.PunchRight;
    }
    private void Impact(BreakerGauntletAbility ability,AreaHitStep step,int index)
    {
        bool finisher=index>=ability.HitSteps.Count-1;
        if (finisher) _finisherApplied=true;
        _animation.Contact(Motion(ability,index));
        SequencedAreaAttackAbility.ApplyHitStep(transform,_player,_weapon,step,true,ability.EffectColor);
        bool radial=step.hitShape==AreaHitShape.Sphere;
        Vector3 origin=transform.TransformPoint(step.localOffset)+(radial ? Vector3.up*(step.sphereRadius-1f) : Vector3.zero);
        GauntletImpactVfx.Spawn(origin,transform.forward,
            step.hitShape==AreaHitShape.Sphere ? step.sphereRadius*2 : step.rangeOverride,
            radial ? step.sphereRadius*2 : step.boxSize.x,ability.EffectColor,ability.ShockSkill && finisher,ability.AsuraBurst && finisher,index%2==0 ? -1 : 1,radial);
    }
    public void FinishForDodge()
    {
        if (!CanDodgeCancel) return;
        // Cash out the main finisher once; optional echoes are traded for an immediate escape.
        if (!_finisherApplied && _activeSteps!=null)
        {
            int index=_activeAbility.HitSteps.Count-1;
            Impact(_activeAbility,_activeSteps[index],index);
        }
        CancelCast();
    }

    private void RestoreCastState()
    {
        if (!IsExecuting) return;
        if (_animation) _animation.Release();
        if (_player) _player.RemoveDamageTakenMultiplier(this);
        if (_agentLocked && _agent)
        {
            _agent.updateRotation = _savedRotation;
            if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = _savedStopped;
        }
        _agentLocked = false;
        IsExecuting = false;
        _activeAbility = null; _activeSteps = null;
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
