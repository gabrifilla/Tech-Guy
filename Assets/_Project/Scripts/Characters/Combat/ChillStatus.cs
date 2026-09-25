using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Chill / Freeze: slows the enemy's NavMeshAgent. A slow fraction below the freeze threshold just
/// reduces movement speed; at or above it, the agent is fully stopped (a freeze), which EnemyAI
/// already treats as "cannot act". Restores the original speed when the effect ends.
/// </summary>
public sealed class ChillStatus : StatusEffect
{
    private const float FreezeThreshold = 0.9f;

    private NavMeshAgent _agent;
    private float _baseSpeed;
    private float _slowFraction;
    private bool _frozen;
    private bool _captured;

    /// <summary>Adds or refreshes a chill on <paramref name="target"/>. slowFraction 0..1 (1 = frozen).</summary>
    public static void Apply(Actor target, float slowFraction, float duration)
    {
        if (!target || target.IsDead || slowFraction <= 0f || duration <= 0f) return;

        ChillStatus chill = target.GetComponent<ChillStatus>();
        if (!chill) chill = target.gameObject.AddComponent<ChillStatus>();
        chill.Configure(slowFraction, duration);
    }

    private void Configure(float slowFraction, float duration)
    {
        EnsureAgent();
        _slowFraction = Mathf.Max(_slowFraction, Mathf.Clamp01(slowFraction));
        RefreshDuration(duration);
        ApplySlow();
    }

    protected override void Awake()
    {
        base.Awake();
        EnsureAgent();
    }

    private void EnsureAgent()
    {
        if (_captured || !Host) return;
        _agent = Host.GetComponent<NavMeshAgent>() ?? Host.GetComponentInChildren<NavMeshAgent>();
        if (_agent)
        {
            _baseSpeed = _agent.speed;
            _captured = true;
        }
    }

    private void ApplySlow()
    {
        if (!_agent) return;

        if (_slowFraction >= FreezeThreshold)
        {
            _frozen = true;
            if (_agent.isOnNavMesh) { _agent.ResetPath(); _agent.isStopped = true; }
        }
        else
        {
            _frozen = false;
            _agent.speed = _baseSpeed * (1f - _slowFraction);
        }
    }

    protected override void Tick(float deltaTime)
    {
        // Keep the freeze pinned in case something re-enables the agent mid-effect.
        if (_frozen && _agent && _agent.isOnNavMesh && !_agent.isStopped) _agent.isStopped = true;
    }

    protected override void OnEffectEnded()
    {
        if (!_agent) return;
        _agent.speed = _baseSpeed;
        if (_frozen && _agent.isOnNavMesh) _agent.isStopped = false;
    }
}
