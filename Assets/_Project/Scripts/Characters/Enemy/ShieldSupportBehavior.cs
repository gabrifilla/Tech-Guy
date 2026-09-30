using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The Shield Support role behavior (R14). On a channel cadence, and only while it may act
/// (<see cref="ArchetypeBehavior.CanAct"/>), the Shield Support runs a channel that:
///
/// <list type="bullet">
///   <item><description>
///     shows a visible channel <see cref="EnemyAttackExecution"/> telegraph for a minimum windup of
///     0.25 seconds and stays <b>stationary</b> for the whole channel (R14.4); and
///   </description></item>
///   <item><description>
///     on <b>completion</b> grants a <see cref="Shield"/> to every allied <see cref="Actor"/> within a
///     fixed support radius (4–10m), each configured with a fixed absorption capacity and a duration in
///     [3,10]s (R14.2). An ally that already carries a shield has it refreshed rather than stacked.
///   </description></item>
/// </list>
///
/// If the channel is <b>interrupted</b> by a stance break or control lock before it completes, the
/// channel grants no shield at all (R14.5): the telegraph helper's own per-frame <c>canAttack</c> gate
/// cancels the windup, and this behavior only distributes shields when the helper reports
/// <see cref="EnemyAttackExecution.Completed"/>.
///
/// The role-action cue on the <see cref="PriorityTargetMarker"/> (added by <c>EnemyVariant</c> on this
/// priority archetype, task 11.1) is shown for the channel and hidden the moment it ends — completed or
/// interrupted — so the priority read stays honest (R14 legibility, R21.3).
///
/// The MonoBehaviour stays thin: it owns only the cadence coroutine, the telegraph, the stationary hold,
/// and the ally overlap query. Ally discovery uses serialized group references first and falls back to a
/// filtered <see cref="Physics.OverlapSphere"/> over enemy <see cref="Actor"/>s — never
/// <c>FindObjectOfType</c> or <c>GameObject.Find</c> (AGENTS.md).
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 8.5. Requirements: 14.1, 14.2, 14.4, 14.5.</remarks>
[DisallowMultipleComponent]
public sealed class ShieldSupportBehavior : ArchetypeBehavior
{
    [Header("Channel")]
    [Tooltip("Seconds between the end of one channel and the start of the next attempt.")]
    [SerializeField] private float _channelInterval = 4f;

    [Tooltip("Channel windup in seconds. The telegraph enforces a 0.25s floor regardless (R14.4).")]
    [SerializeField] private float _channelWindup = 1.2f;

    [Tooltip("Color of the channel telegraph ring drawn under the Shield Support while channeling.")]
    [SerializeField] private Color _channelColor = new Color(0.2f, 0.9f, 1f, 1f);

    [Header("Support")]
    [Tooltip("Fixed support radius in meters within which allies receive a shield on completion (R14.2). Bounded 4–10m.")]
    [SerializeField] private float _supportRange = 6f;

    [Tooltip("Absorption capacity granted to each shielded ally (R14.3). A fixed amount.")]
    [SerializeField] private float _shieldCapacity = 40f;

    [Tooltip("Lifetime of each granted shield in seconds (R14.3). Bounded 3–10s.")]
    [SerializeField] private float _shieldDurationSeconds = 6f;

    [Header("Ally discovery")]
    [Tooltip("Optional explicit allied Actors to shield. When any are assigned they are used directly; otherwise a filtered overlap query finds nearby enemy Actors (never FindObjectOfType).")]
    [SerializeField] private Actor[] _allyGroup = System.Array.Empty<Actor>();

    [Tooltip("Layers searched by the fallback overlap query when no ally group is assigned. Leave as everything to search the default layers.")]
    [SerializeField] private LayerMask _allyOverlapMask = ~0;

    // Support-range bounds from R14.2 ("a fixed radius in the range 4 to 10 meters").
    private const float MinSupportRange = 4f;
    private const float MaxSupportRange = 10f;
    // Shield duration bounds from R14.3 ("in the range 3 to 10 seconds").
    private const float MinShieldDuration = 3f;
    private const float MaxShieldDuration = 10f;

    private readonly EnemyAttackExecution _channel = new EnemyAttackExecution();
    private readonly Collider[] _overlapBuffer = new Collider[32];
    private readonly List<Actor> _allyScratch = new List<Actor>();

    private PriorityTargetMarker _marker;
    private Coroutine _loop;

    protected override void Awake()
    {
        base.Awake();
        _marker = GetComponent<PriorityTargetMarker>();
        ClampSerializedFields();
    }

    private void OnValidate() => ClampSerializedFields();

    /// <summary>
    /// Names any required dependency that failed to resolve so a misconfigured prefab is diagnosed at
    /// spawn instead of silently doing nothing (R1.6). The base resolves Owner/Ai/Agent; the NavMesh
    /// agent is required here because the stationary-hold rule (R14.4) drives locomotion through it.
    /// </summary>
    protected override void LogMissingDependencies()
    {
        var missing = new List<string>();
        if (!Owner) missing.Add(nameof(Actor));
        if (!Ai) missing.Add(nameof(EnemyAI));
        if (!Agent) missing.Add("NavMeshAgent");
        if (missing.Count > 0)
            Debug.LogError(
                $"{nameof(ShieldSupportBehavior)} on '{name}' is missing required component(s): {string.Join(", ", missing)}.",
                this);
    }

    /// <summary>
    /// Keeps the authored fields inside their requirement bounds so a misconfigured prefab still behaves
    /// within R14: support range in [4,10]m, shield duration in [3,10]s, and non-negative timing/capacity.
    /// </summary>
    private void ClampSerializedFields()
    {
        _supportRange = Mathf.Clamp(_supportRange, MinSupportRange, MaxSupportRange);
        _shieldDurationSeconds = Mathf.Clamp(_shieldDurationSeconds, MinShieldDuration, MaxShieldDuration);
        _shieldCapacity = Mathf.Max(0f, _shieldCapacity);
        _channelInterval = Mathf.Max(0f, _channelInterval);
        _channelWindup = Mathf.Max(0f, _channelWindup);
    }

    private void OnEnable() => _loop = StartCoroutine(ChannelCadence());

    private void OnDisable()
    {
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
        // Any in-flight channel is abandoned without granting a shield; drop its telegraph + cue this frame.
        _channel.Cancel();
        HoldPosition(false);
        if (_marker) _marker.HideRoleActionCue();
    }

    /// <summary>
    /// The channel cadence: wait a beat, then — gated on <see cref="ArchetypeBehavior.CanAct"/> — run one
    /// channel and grant shields on completion. Timer-driven rather than heavy <c>Update</c> work.
    /// </summary>
    private IEnumerator ChannelCadence()
    {
        while (true)
        {
            yield return new WaitForSeconds(_channelInterval);
            if (CanAct)
                yield return RunChannel();
        }
    }

    /// <summary>
    /// Runs a single channel: hold position, show the telegraph + role-action cue, and on completion
    /// distribute shields. On interruption (control lock / stance break) the telegraph self-cancels and no
    /// shield is granted (R14.5). The cue and the stationary hold are always released when the channel ends.
    /// </summary>
    private IEnumerator RunChannel()
    {
        // Nobody to protect: skip the channel rather than telegraph an empty commitment.
        if (CollectAllies(_allyScratch) == 0)
            yield break;

        // The channel is a self-cast support action, not a strike, so it does not require a player target.
        // The shared telegraph helper only needs a live, non-dead Actor to gate on: the caster itself.
        // Passing Owner keeps shielding independent of whether the player is in range.
        Actor target = Owner;
        if (!target) yield break;

        HoldPosition(true);
        if (_marker) _marker.ShowRoleActionCue();

        // The channel telegraph: a circle under the caster sized to the support range so the player reads
        // exactly who is being protected. damage=0 — this beat never harms anyone; it only marks completion.
        var area = new EnemyAttackArea(EnemyAttackShape.Circle, transform.position, transform.forward, _supportRange);

        // Per-frame gate: the channel proceeds only while this behavior may still act. A control lock or
        // stance break flips CanAct false, EnemyAttackExecution cancels the windup, and Completed stays
        // false — so the completion branch below never runs and no shield is granted (R14.5).
        yield return _channel.Execute(
            new[] { area }, target, _channelWindup, 0f, _channelColor,
            canAttack: () => CanAct,
            showWarning: true, impactHold: 0f);

        bool completed = _channel.Completed;

        // Release the stationary hold and the cue whether the channel completed or was interrupted.
        HoldPosition(false);
        if (_marker) _marker.HideRoleActionCue();

        if (!completed) yield break; // Interrupted: grant nothing (R14.5).

        GrantShieldsToAllies();
    }

    /// <summary>
    /// Grants (or refreshes) a shield on every ally currently within the support radius, re-querying at the
    /// moment of completion so only allies present when the channel lands are protected (R14.2).
    /// </summary>
    private void GrantShieldsToAllies()
    {
        int count = CollectAllies(_allyScratch);
        for (int i = 0; i < count; i++)
        {
            Actor ally = _allyScratch[i];
            if (!ally) continue;

            // One shield per ally: reuse the existing component if present (refresh) instead of stacking.
            var shield = ally.GetComponent<Shield>();
            if (!shield) shield = ally.gameObject.AddComponent<Shield>();
            shield.Configure(_shieldCapacity, _shieldDurationSeconds);

            // Attach the protection indicator so the shielded ally reads as protected (R6.1, R6.2).
            // The indicator is self-sufficient (resolves Actor/Shield via GetComponent) and auto-hides
            // when the Shield self-destroys, so it needs no removal here. It is DisallowMultipleComponent,
            // so guard with TryGetComponent to reuse the existing one and avoid a duplicate warning.
            if (!ally.TryGetComponent<ProtectionIndicator>(out _))
                ally.gameObject.AddComponent<ProtectionIndicator>();
        }
    }

    /// <summary>
    /// Fills <paramref name="result"/> with the allied <see cref="Actor"/>s eligible for a shield right now
    /// and returns the count. An ally is any live, active enemy Actor (never the player, never this Shield
    /// Support) within the support radius. Prefers the serialized <see cref="_allyGroup"/> when populated;
    /// otherwise runs a filtered overlap query — never <c>FindObjectOfType</c>.
    /// </summary>
    private int CollectAllies(List<Actor> result)
    {
        result.Clear();
        float rangeSqr = _supportRange * _supportRange;
        Vector3 origin = transform.position;

        if (_allyGroup != null && _allyGroup.Length > 0)
        {
            foreach (Actor ally in _allyGroup)
            {
                if (!IsEligibleAlly(ally)) continue;
                if ((ally.transform.position - origin).sqrMagnitude > rangeSqr) continue;
                if (!result.Contains(ally)) result.Add(ally);
            }
            return result.Count;
        }

        int hits = Physics.OverlapSphereNonAlloc(origin, _supportRange, _overlapBuffer,
            _allyOverlapMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hits; i++)
        {
            Actor candidate = _overlapBuffer[i] ? _overlapBuffer[i].GetComponentInParent<Actor>() : null;
            if (!IsEligibleAlly(candidate)) continue;
            if (!result.Contains(candidate)) result.Add(candidate);
        }
        return result.Count;
    }

    /// <summary>
    /// True when <paramref name="candidate"/> is a shieldable ally: a live, active enemy Actor other than
    /// this Shield Support. The player (<see cref="PlayerActor"/>) is never an ally.
    /// </summary>
    private bool IsEligibleAlly(Actor candidate)
    {
        if (!candidate || candidate.IsDead || !candidate.isActiveAndEnabled) return false;
        if (candidate is PlayerActor) return false;
        if (Owner && candidate == Owner) return false;
        return true;
    }

    /// <summary>
    /// Enforces the "stationary for the duration of the channel" rule (R14.4) through the NavMesh agent —
    /// no direct transform writes. Stopping the agent and clearing its path holds the position; releasing
    /// resumes normal locomotion. A missing or off-mesh agent is a safe no-op.
    /// </summary>
    private void HoldPosition(bool hold)
    {
        if (!Agent || !Agent.enabled || !Agent.isOnNavMesh) return;
        if (hold)
        {
            Agent.isStopped = true;
            Agent.ResetPath();
            Agent.velocity = Vector3.zero;
        }
        else
        {
            Agent.isStopped = false;
        }
    }
}
