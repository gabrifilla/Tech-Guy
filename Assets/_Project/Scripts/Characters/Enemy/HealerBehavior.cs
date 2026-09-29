using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The Healer support role (R13). A fragile, low-priority-to-ignore enemy that keeps its allies
/// alive and runs from the player, teaching the player to hunt it first. It does three things,
/// all gated on <see cref="ArchetypeBehavior.CanAct"/> so a dead, agent-less, or control-locked
/// Healer issues nothing (R20.2, R20.4, R20.5):
///
/// <list type="bullet">
///   <item><description>
///     <b>Heal (R13.3):</b> once every fixed <see cref="_healInterval"/> (authored in the 1.0–3.0s
///     band) it restores a fixed amount to the <i>most-wounded</i> allied <see cref="Actor"/> whose
///     current health is below its maximum and that lies within the fixed <see cref="_healRange"/>
///     (4–8m). The restore goes through <see cref="Actor.Heal"/>, which already clamps to the ally's
///     maximum, so no overheal can occur.
///   </description></item>
///   <item><description>
///     <b>Beam (R13.4/R13.5):</b> while a heal is channeling it shows a visible tether
///     (<see cref="LineRenderer"/>) from itself to the ally being healed, and it shows the shared
///     priority role-action cue. When no wounded ally is in range it channels nothing and the beam
///     stays hidden.
///   </description></item>
///   <item><description>
///     <b>Flee (R13.6/R20.3):</b> when the player enters the fixed <see cref="_fleeRange"/> (2–5m,
///     strictly less than the heal range) it moves <i>away</i> from the player to a NavMesh-reachable
///     point via the shared <see cref="NavMeshAgent"/> (never a raw transform write) until the player
///     is beyond the flee range.
///   </description></item>
/// </list>
///
/// A heal channel is stopped by control-lock or a stance break and applies no further restoration
/// from that channel (R13.7): the behavior both polls <see cref="CombatReactionController.IsControlLocked"/>
/// through the <see cref="ArchetypeBehavior.CanAct"/> gate and subscribes to
/// <see cref="CombatReactionController.StanceBroken"/> to cancel the in-flight channel the moment it
/// fires. Ally discovery uses serialized group references when provided, otherwise a filtered overlap
/// query for enemy <see cref="Actor"/>s (excluding this Healer and the player) — never
/// <c>FindObjectOfType</c> / <c>GameObject.Find</c> (AGENTS.md).
///
/// The MonoBehaviour stays thin: the range-ordering rule and most-wounded / flee-direction selection
/// are factored into the scene-free <see cref="HealerDecision"/> so they can be reasoned about (and
/// tested) without a live scene. The persistent priority marker itself is added by
/// <c>EnemyVariant</c> (task 11.1); this behavior only toggles that marker's role-action cue.
/// </summary>
/// <remarks>
/// Feature: enemy-swarm-core-archetypes, task 8.4.
/// Requirements: 13.1, 13.2, 13.3, 13.4, 13.5, 13.6, 13.7, 20.3.
/// </remarks>
[DisallowMultipleComponent]
public sealed class HealerBehavior : ArchetypeBehavior
{
    [Header("Heal")]
    [Tooltip("Fixed interval between heal channels, in seconds. Authored in the 1.0-3.0s band (R13.3).")]
    [SerializeField] private float _healInterval = 2f;

    [Tooltip("Fixed radius within which a wounded ally can be healed, in meters. Authored in the 4-8m band (R13.3).")]
    [SerializeField] private float _healRange = 6f;

    [Tooltip("Fixed amount of health restored to the most-wounded ally per channel; clamped to that ally's maximum by Actor.Heal (R13.3).")]
    [SerializeField] private float _healAmount = 15f;

    [Tooltip("How long the heal beam is shown while a channel resolves, in seconds. Purely visual (R13.4).")]
    [SerializeField] private float _channelDuration = 0.5f;

    [Header("Flee")]
    [Tooltip("Fixed radius at which the player triggers a flee; MUST be strictly less than the heal range (R13.6).")]
    [SerializeField] private float _fleeRange = 3.5f;

    [Tooltip("How far along the away-from-player direction the Healer tries to retreat each flee decision, in meters.")]
    [SerializeField] private float _fleeStep = 4f;

    [Tooltip("NavMesh.SamplePosition search radius used when snapping a candidate flee point onto the mesh, in meters.")]
    [SerializeField] private float _fleeSampleRadius = 2f;

    [Header("Ally discovery")]
    [Tooltip("Optional explicit allied Actors to heal. When any are assigned they are used directly; otherwise a filtered overlap query finds nearby enemy Actors (never FindObjectOfType).")]
    [SerializeField] private Actor[] _allyGroup = System.Array.Empty<Actor>();

    [Tooltip("Layers searched by the fallback overlap query when no ally group is assigned. Leave empty to search the default layers.")]
    [SerializeField] private LayerMask _allyOverlapMask = ~0;

    [Header("Beam")]
    [Tooltip("Color of the heal tether drawn from the Healer to the ally being healed (R13.4).")]
    [SerializeField] private Color _beamColor = new Color(0.35f, 1f, 0.55f, 1f);

    // Runtime state.
    private PriorityTargetMarker _marker;
    private LineRenderer _beam;
    private Material _beamMaterial;
    private Transform _playerTransform;
    private float _nextHealTime;
    private float _channelEndTime;
    private Actor _channelTarget;
    private bool _channeling;
    private bool _stanceBrokenSubscribed;

    // Reused buffer for the fallback overlap query so heal decisions allocate nothing per tick.
    private readonly Collider[] _overlapBuffer = new Collider[32];
    private readonly List<Actor> _woundedInRange = new List<Actor>(16);

    protected override void Awake()
    {
        base.Awake();
        _marker = GetComponent<PriorityTargetMarker>();
        ResolvePlayerTransform();
        EnforceRangeOrdering();

        if (Reaction != null)
        {
            Reaction.StanceBroken += HandleStanceBroken;
            _stanceBrokenSubscribed = true;
        }

        // Stagger the first heal so a freshly spawned Healer does not fire on frame 0.
        _nextHealTime = Time.time + _healInterval;
    }

    /// <summary>
    /// Validates the flee-range &lt; heal-range ordering in the editor as soon as either field is
    /// edited, keeping the invariant (R13.6) visible before play. The at-runtime clamp lives in
    /// <see cref="EnforceRangeOrdering"/>.
    /// </summary>
    private void OnValidate()
    {
        _healInterval = Mathf.Max(0.01f, _healInterval);
        _healRange = Mathf.Max(0.01f, _healRange);
        _fleeRange = Mathf.Max(0.01f, _fleeRange);
        if (!HealerDecision.IsRangeOrderingValid(_fleeRange, _healRange))
        {
            Debug.LogWarning(
                $"{nameof(HealerBehavior)} on '{name}': flee range ({_fleeRange}m) must be strictly less " +
                $"than heal range ({_healRange}m) (R13.6). Clamping flee range.", this);
            _fleeRange = Mathf.Max(0.01f, _healRange * 0.5f);
        }
    }

    protected override void LogMissingDependencies()
    {
        // Base already resolved Actor / EnemyAI / NavMeshAgent / CombatReactionController and logged
        // any that were missing. The Healer adds no hard-required extra reference: the priority marker
        // is optional (EnemyVariant may not have added it in a bare test scene) and ally discovery
        // falls back to an overlap query, so there is nothing further to fail here.
    }

    /// <summary>
    /// Clamps the flee range below the heal range at runtime so the R13.6 ordering holds even if a
    /// prefab was authored with an inverted pair. Mirrors the <see cref="OnValidate"/> editor guard.
    /// </summary>
    private void EnforceRangeOrdering()
    {
        if (!HealerDecision.IsRangeOrderingValid(_fleeRange, _healRange))
        {
            Debug.LogError(
                $"{nameof(HealerBehavior)} on '{name}': flee range ({_fleeRange}m) is not strictly less " +
                $"than heal range ({_healRange}m) (R13.6). Clamping flee range to half the heal range.", this);
            _fleeRange = Mathf.Max(0.01f, _healRange * 0.5f);
        }
    }

    private void Update()
    {
        // A control-lock / dead / agent-invalid Healer issues nothing. If it was mid-channel, the
        // channel is abandoned and applies no restoration (R13.7, R20.2). CanAct already covers the
        // IsControlLocked case; StanceBroken is handled reactively by HandleStanceBroken.
        if (!CanAct)
        {
            if (_channeling) StopChannel();
            return;
        }

        // Fleeing takes precedence over healing: if the player is inside the flee band, retreat and
        // do not start a new heal channel this frame (R13.6). An in-flight channel is left to resolve
        // or be cut by control-lock; the beam simply follows.
        if (TryFlee())
        {
            UpdateBeam();
            ResolveChannel();
            return;
        }

        if (_channeling)
        {
            UpdateBeam();
            ResolveChannel();
            return;
        }

        // Not fleeing and not channeling: start a heal when the interval elapses and a wounded ally
        // is in range (R13.3). No wounded ally in range means no channel at all (R13.5).
        if (Time.time >= _nextHealTime)
        {
            _nextHealTime = Time.time + _healInterval;
            Actor target = FindMostWoundedAllyInRange();
            if (target != null)
                BeginChannel(target);
        }
    }

    /// <summary>
    /// Moves the Healer to a NavMesh-reachable point directly away from the player when the player is
    /// inside the flee range (R13.6). Returns true while a flee is in effect this frame. Uses only the
    /// shared agent and <see cref="NavMesh.SamplePosition"/>; if no reachable point is found the agent
    /// is left where it is with no invalid command (R20.3, R20.4).
    /// </summary>
    private bool TryFlee()
    {
        if (_playerTransform == null) ResolvePlayerTransform();
        if (_playerTransform == null) return false;

        Vector3 self = transform.position;
        Vector3 playerPos = _playerTransform.position;
        if (!HealerDecision.IsPlayerWithinFleeRange(self, playerPos, _fleeRange))
            return false;

        Vector3 candidate = HealerDecision.ComputeFleeDestination(self, playerPos, _fleeStep);
        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _fleeSampleRadius, NavMesh.AllAreas))
        {
            Agent.isStopped = false;
            Agent.SetDestination(hit.position);
        }
        // Even if sampling failed we still count this as "fleeing" so no heal starts while the player
        // is on top of us; we simply issued no new destination this frame (R20.4).
        return true;
    }

    /// <summary>Begins a heal channel toward <paramref name="target"/>, showing the beam and role cue.</summary>
    private void BeginChannel(Actor target)
    {
        _channeling = true;
        _channelTarget = target;
        _channelEndTime = Time.time + _channelDuration;
        // Stand still while channeling so the tether reads clearly.
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh) Agent.isStopped = true;
        if (_marker != null) _marker.ShowRoleActionCue();
        UpdateBeam();
    }

    /// <summary>
    /// Completes the current channel when its duration elapses, applying the fixed heal to the target
    /// via <see cref="Actor.Heal"/> (which clamps to the ally's maximum). If the target died or left
    /// range mid-channel the heal is skipped, but the channel still ends cleanly.
    /// </summary>
    private void ResolveChannel()
    {
        if (!_channeling) return;
        if (Time.time < _channelEndTime) return;

        Actor target = _channelTarget;
        if (target != null && !target.IsDead &&
            HealerDecision.IsAllyWithinHealRange(transform.position, target.transform.position, _healRange) &&
            target.health < target.maxHealth)
        {
            target.Heal(_healAmount);
        }

        StopChannel();
    }

    /// <summary>
    /// Ends the current channel with no (further) restoration and hides the beam / role cue. Called on
    /// normal completion and on interruption by control-lock or stance break (R13.7).
    /// </summary>
    private void StopChannel()
    {
        _channeling = false;
        _channelTarget = null;
        if (_marker != null) _marker.HideRoleActionCue();
        HideBeam();
        if (Agent != null && Agent.enabled && Agent.isOnNavMesh) Agent.isStopped = false;
    }

    /// <summary>
    /// Cuts an in-flight heal channel the instant the Healer's stance breaks, applying no restoration
    /// from that channel (R13.7). Subscribed to <see cref="CombatReactionController.StanceBroken"/>.
    /// </summary>
    private void HandleStanceBroken(Vector3 _)
    {
        if (_channeling) StopChannel();
    }

    /// <summary>
    /// Returns the most-wounded allied <see cref="Actor"/> whose current health is below its maximum
    /// and that lies within the heal range, or null when none qualifies (R13.3, R13.5). Ally discovery
    /// prefers the serialized group; when it is empty it falls back to a filtered overlap query for
    /// nearby enemy Actors, excluding this Healer and the player.
    /// </summary>
    private Actor FindMostWoundedAllyInRange()
    {
        _woundedInRange.Clear();
        Vector3 self = transform.position;

        if (_allyGroup != null && _allyGroup.Length > 0)
        {
            foreach (Actor ally in _allyGroup)
            {
                if (!IsHealableAlly(ally, self)) continue;
                _woundedInRange.Add(ally);
            }
        }
        else
        {
            int count = Physics.OverlapSphereNonAlloc(self, _healRange, _overlapBuffer, _allyOverlapMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider col = _overlapBuffer[i];
                if (col == null) continue;
                Actor ally = col.GetComponentInParent<Actor>();
                if (!IsHealableAlly(ally, self)) continue;
                if (!_woundedInRange.Contains(ally)) _woundedInRange.Add(ally);
            }
        }

        return HealerDecision.SelectMostWounded(_woundedInRange);
    }

    /// <summary>
    /// True when <paramref name="ally"/> is a valid heal target from <paramref name="self"/>: a live,
    /// non-player, non-self enemy <see cref="Actor"/> that is wounded and within heal range.
    /// </summary>
    private bool IsHealableAlly(Actor ally, Vector3 self)
    {
        if (ally == null) return false;
        if (ally == Owner) return false;                 // never self-heal via the ally path
        if (ally is PlayerActor) return false;           // the player is never an ally (R13.3)
        if (ally.IsDead) return false;
        if (ally.health >= ally.maxHealth) return false; // only wounded allies (R13.5)
        return HealerDecision.IsAllyWithinHealRange(self, ally.transform.position, _healRange);
    }

    // ---- Beam (R13.4) --------------------------------------------------------------------------

    private void UpdateBeam()
    {
        if (!_channeling || _channelTarget == null || _channelTarget.IsDead)
        {
            HideBeam();
            return;
        }

        EnsureBeam();
        _beam.enabled = true;
        _beam.SetPosition(0, transform.position + Vector3.up * 1.0f);
        _beam.SetPosition(1, _channelTarget.transform.position + Vector3.up * 1.0f);
    }

    private void EnsureBeam()
    {
        if (_beam != null) return;
        var go = new GameObject("HealBeam");
        go.transform.SetParent(transform, false);
        _beam = go.AddComponent<LineRenderer>();
        _beam.useWorldSpace = true;
        _beam.positionCount = 2;
        _beam.widthMultiplier = 0.1f;
        _beam.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _beamMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        _beamMaterial.SetColor("_BaseColor", _beamColor);
        _beam.sharedMaterial = _beamMaterial;
        _beam.enabled = false;
    }

    private void HideBeam()
    {
        if (_beam != null) _beam.enabled = false;
    }

    private void ResolvePlayerTransform()
    {
        // Prefer the reference EnemyAI already resolved (serialized or tag-resolved once at spawn),
        // avoiding a second FindGameObjectWithTag scan here (AGENTS.md: no FindObjectOfType/Find).
        if (Ai != null && Ai.player != null)
        {
            _playerTransform = Ai.player;
        }
    }

    private void OnDisable()
    {
        // Abandon any in-flight channel so a disabled Healer applies no restoration (R13.7 / R20.2).
        if (_channeling) StopChannel();
    }

    private void OnDestroy()
    {
        if (_stanceBrokenSubscribed && Reaction != null)
        {
            Reaction.StanceBroken -= HandleStanceBroken;
            _stanceBrokenSubscribed = false;
        }
        if (_beamMaterial != null) Destroy(_beamMaterial);
    }
}

/// <summary>
/// Pure, scene-free decision helpers for <see cref="HealerBehavior"/>. Extracted so the range
/// ordering (R13.6), most-wounded selection (R13.3), and flee-direction math can be reasoned about
/// and unit/property-tested without a live Unity scene, matching the design's testing strategy.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 8.4. Requirements: 13.3, 13.5, 13.6, 20.3.</remarks>
public static class HealerDecision
{
    /// <summary>
    /// True when the flee range is strictly less than the heal range, the invariant R13.6 requires so
    /// the Healer always begins retreating before the player could interrupt a heal at melee distance.
    /// </summary>
    public static bool IsRangeOrderingValid(float fleeRange, float healRange) => fleeRange < healRange;

    /// <summary>True when the player is at or within the flee range on the horizontal plane (R13.6).</summary>
    public static bool IsPlayerWithinFleeRange(Vector3 self, Vector3 player, float fleeRange)
    {
        Vector3 d = player - self;
        d.y = 0f;
        return d.sqrMagnitude <= fleeRange * fleeRange;
    }

    /// <summary>True when the ally is at or within the heal range on the horizontal plane (R13.3).</summary>
    public static bool IsAllyWithinHealRange(Vector3 self, Vector3 ally, float healRange)
    {
        Vector3 d = ally - self;
        d.y = 0f;
        return d.sqrMagnitude <= healRange * healRange;
    }

    /// <summary>
    /// Computes a flee destination <paramref name="step"/> meters directly away from the player. When
    /// the player is exactly co-located an arbitrary but stable direction is used so the result is
    /// always well-defined (never NaN).
    /// </summary>
    public static Vector3 ComputeFleeDestination(Vector3 self, Vector3 player, float step)
    {
        Vector3 away = self - player;
        away.y = 0f;
        away = away.sqrMagnitude > 1e-6f ? away.normalized : Vector3.forward;
        return self + away * Mathf.Max(0f, step);
    }

    /// <summary>
    /// Returns the most-wounded candidate — the one whose current health is lowest relative to its
    /// maximum (smallest health/maxHealth ratio) — or null when the list is empty. Callers are
    /// responsible for having already filtered to live, in-range, wounded allies. Ties resolve to the
    /// first encountered candidate, which is stable for a fixed discovery order.
    /// </summary>
    public static Actor SelectMostWounded(IReadOnlyList<Actor> candidates)
    {
        if (candidates == null || candidates.Count == 0) return null;

        Actor best = null;
        float bestRatio = float.PositiveInfinity;
        for (int i = 0; i < candidates.Count; i++)
        {
            Actor a = candidates[i];
            if (a == null) continue;
            float max = a.maxHealth > 0f ? a.maxHealth : 1f;
            float ratio = a.health / max;
            if (ratio < bestRatio)
            {
                bestRatio = ratio;
                best = a;
            }
        }
        return best;
    }
}
