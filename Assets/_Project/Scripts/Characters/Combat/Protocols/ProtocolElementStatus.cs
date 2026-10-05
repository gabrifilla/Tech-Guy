using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Owned Protocol contributions coexist with legacy BurnStatus/ChillStatus; cleanup removes only its owner.</summary>
[DisallowMultipleComponent]
public sealed class ProtocolElementStatus : MonoBehaviour
{
    private readonly List<ProtocolElementApplication> _applications = new List<ProtocolElementApplication>();
    private Actor _actor;
    private NavMeshAgent _agent;
    private float _baseSpeed;
    private float _appliedSpeed;
    private bool _ownsSpeed;
    private CombatReactionController _reaction;
    private bool _wasFrozen;
    private bool CanFreeze => !_reaction || !_reaction.IsResumeProtected;
    public bool HasBurn { get { foreach (var value in _applications) if (value.Valid && value.BurnRemaining > 0) return true; return false; } }
    public bool HasChill { get { foreach (var value in _applications) if (value.Valid && value.ChillRemaining > 0) return true; return false; } }
    public float BurnDps { get { float result = 0; foreach (var value in _applications) if (value.Valid && value.BurnRemaining > 0) result = Mathf.Max(result, value.BurnDps); return result; } }
    public float BurnRemaining { get { float result = 0; foreach (var value in _applications) if (value.Valid) result = Mathf.Max(result, value.BurnRemaining); return result; } }
    public float ChillRemaining { get { float result = 0; foreach (var value in _applications) if (value.Valid) result = Mathf.Max(result, value.ChillRemaining); return result; } }
    public bool IsFrozen
    {
        get { if (!CanFreeze) return false; foreach (var value in _applications) if (value.Valid && value.ChillRemaining > 0 && value.SlowFraction >= .9f) return true; return false; }
    }
    private void Awake()
    {
        _actor = GetComponent<Actor>(); _agent = GetComponent<NavMeshAgent>(); _reaction = GetComponent<CombatReactionController>();
        if (_actor) _actor.Died += OnDeath;
    }
    internal void Add(ProtocolElementApplication application) { _applications.Add(application); RefreshMovement(); }
    internal void Remove(ProtocolElementApplication application) { _applications.Remove(application); RefreshMovement(); }
    internal void RefreshMovement()
    {
        if (!_agent) return;
        float slow = 0;
        foreach (var value in _applications) if (value.Valid && value.ChillRemaining > 0) slow = Mathf.Max(slow, value.SlowFraction);
        if (slow <= 0)
        {
            if (_ownsSpeed && Mathf.Approximately(_agent.speed, _appliedSpeed)) _agent.speed = _baseSpeed;
            _ownsSpeed = false; return;
        }
        if (!_ownsSpeed || !Mathf.Approximately(_agent.speed, _appliedSpeed)) _baseSpeed = _agent.speed;
        _wasFrozen = slow >= .9f && CanFreeze;
        _appliedSpeed = _wasFrozen ? 0 : _baseSpeed * (1f - slow);
        _agent.speed = _appliedSpeed; _ownsSpeed = true;
        // isStopped/path/rotation are owned by AI and reaction; do not release their leases.
    }
    private void Update() { if (_ownsSpeed && _agent &&
        (_wasFrozen != IsFrozen || !Mathf.Approximately(_agent.speed, _appliedSpeed))) RefreshMovement(); }
    private void OnDeath(Actor actor)
    {
        while (_applications.Count > 0)
        {
            var value = _applications[_applications.Count - 1];
            if (value.Owner && value.Registered) value.Owner.RemoveApplication(value);
            else Remove(value);
        }
    }
    private void OnDisable() { if (_actor) OnDeath(_actor); }
    private void OnDestroy() { if (_actor) _actor.Died -= OnDeath; }
}

public sealed class ProtocolElementApplication
{
    internal bool Registered { get; set; }
    internal int LastUpdatedFrame { get; set; } = -1;
    public ProtocolRuntime Owner { get; internal set; }
    public Actor Target { get; internal set; }
    public ProtocolElementStatus View { get; internal set; }
    public ProtocolTargetHandle Handle { get; internal set; }
    public System.Guid SessionId { get; internal set; }
    public int EncounterId { get; internal set; }
    public long ApplicationId { get; internal set; }
    public int Revision { get; internal set; }
    public ProtocolRanks BurnRanks { get; internal set; }
    public float BurnRemaining { get; internal set; }
    public float BurnDps { get; internal set; }
    public float TickRemaining { get; internal set; }
    public long TickOrdinal { get; internal set; }
    public float ChillRemaining { get; internal set; }
    public float SlowFraction { get; internal set; }
    public bool Valid => Registered && Owner && Target && !Target.IsDead && Target.isActiveAndEnabled &&
        Target.CombatGeneration == Handle.Generation && Owner.Inventory != null && !Owner.Inventory.IsEnded &&
        Owner.Inventory.SessionId == SessionId && Owner.EncounterId == EncounterId;
}

public static class ProtocolElementQuery
{
    public static bool HasBurn(Actor target) => target && (target.GetComponent<BurnStatus>() ||
        target.TryGetComponent(out ProtocolElementStatus status) && status.HasBurn);
    public static bool HasChill(Actor target) => target && (target.GetComponent<ChillStatus>() ||
        target.TryGetComponent(out ProtocolElementStatus status) && status.HasChill);
    public static bool IsFrozen(Actor target) => target && target.TryGetComponent(out ProtocolElementStatus status) && status.IsFrozen;
}
