using UnityEngine;

/// <summary>
/// Pure, scene-independent accumulator for the Lança's optional <b>Flow</b> resource (Requisito 9.7).
///
/// Flow rewards good execution: it builds up when a thrust connects on the <see cref="SweetSpot"/> and
/// when the player alternates between the two spear categories (<see cref="ArsenalSkillKind.Thrust"/> ↔
/// <see cref="ArsenalSkillKind.Sweep"/>). It is an <b>opt-in</b> feature: a fresh instance starts
/// <see cref="IsEnabled"/> = false and, while disabled, every accumulation entry point is a complete
/// no-op. This is what guarantees the core contract of Requisito 9.7 (Property 32): for a player who
/// does not accumulate Flow, enabling the feature must not change the damage, mana cost or cooldown of
/// any spear ability. This class only ever reads from ability data and tracks its own internal counter;
/// it never mutates an <see cref="ArsenalAbility"/>, an <c>ArsenalCastPlan</c>, or any other ability
/// state, so it cannot alter damage/cost/cooldown for anyone — accumulating or not.
///
/// It is injected into <c>ArsenalCombat</c> as a plain C# collaborator so the coordinating
/// <see cref="MonoBehaviour"/> stays thin (AGENTS.md). It holds no Unity references of its own and is
/// fully testable without a scene. Wiring into <c>ArsenalCombat</c> is a separate task.
/// </summary>
public sealed class SpearFlow
{
    /// <summary>Flow value of a freshly reset / never-accumulated resource.</summary>
    public const float EmptyFlow = 0f;

    /// <summary>Default cap on accumulated Flow when the config does not specify one.</summary>
    public const float DefaultMaxFlow = 100f;

    private readonly SpearFlowConfig _config;

    private bool _isEnabled;
    private float _flow;

    // The category of the previous spear ability, used to detect thrust<->sweep alternation.
    // Null until the first ability of an accumulating session is registered.
    private ArsenalSkillKind? _lastCategory;

    /// <summary>
    /// Creates a Flow accumulator bound to a configuration. The resource is <b>disabled by default</b>;
    /// call <see cref="SetEnabled"/> to opt in. While disabled it is a complete no-op (Requisito 9.7).
    /// </summary>
    /// <param name="config">Tuning for how much Flow each event grants and the cap.</param>
    public SpearFlow(SpearFlowConfig config)
    {
        _config = config;
        _isEnabled = false;
        _flow = EmptyFlow;
        _lastCategory = null;
    }

    /// <summary>Creates a Flow accumulator with the default configuration, disabled.</summary>
    public SpearFlow() : this(SpearFlowConfig.Default)
    {
    }

    /// <summary>The configuration this accumulator was built with.</summary>
    public SpearFlowConfig Config => _config;

    /// <summary>
    /// Whether the optional Flow resource is active. False by default: a player who has not opted in
    /// does not accumulate Flow, and none of the accumulation entry points do anything.
    /// </summary>
    public bool IsEnabled => _isEnabled;

    /// <summary>The current accumulated Flow, in [0, <see cref="MaxFlow"/>]. Zero while disabled.</summary>
    public float Flow => _flow;

    /// <summary>The maximum Flow this resource can hold, derived from the config (always &gt; 0).</summary>
    public float MaxFlow => _config.MaxFlow;

    /// <summary>Whether Flow has reached its cap. Always false while disabled.</summary>
    public bool IsFull => _isEnabled && _flow >= MaxFlow;

    /// <summary>
    /// Opts the player in or out of accumulating Flow. Turning the resource off clears any accumulated
    /// Flow and the alternation history, so a disabled resource is indistinguishable from a fresh one
    /// (a complete no-op). Turning it on begins from an empty resource.
    /// </summary>
    /// <param name="enabled">True to start accumulating Flow; false to opt out.</param>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (!enabled)
        {
            _flow = EmptyFlow;
            _lastCategory = null;
        }
    }

    /// <summary>
    /// Registers a spear ability activation for alternation tracking, granting Flow when the ability's
    /// category differs from the previous one (thrust ↔ sweep). No-op while disabled.
    /// </summary>
    /// <param name="kind">The category of the spear ability that was just used.</param>
    /// <returns>The Flow granted by this registration (0 while disabled or when not alternating).</returns>
    public float RegisterAbility(ArsenalSkillKind kind)
    {
        if (!_isEnabled)
        {
            return 0f;
        }

        float granted = 0f;
        if (_lastCategory.HasValue && IsAlternation(_lastCategory.Value, kind))
        {
            granted = Accumulate(_config.AlternationFlow);
        }

        _lastCategory = kind;
        return granted;
    }

    /// <summary>
    /// Registers a thrust hit that landed on the Sweet Spot, granting Flow. No-op while disabled or when
    /// the hit was not a Sweet Spot hit.
    /// </summary>
    /// <param name="isSweetSpot">Whether the hit landed in the tip Sweet Spot region.</param>
    /// <returns>The Flow granted by this hit (0 while disabled or when it was not a Sweet Spot hit).</returns>
    public float RegisterSweetSpotHit(bool isSweetSpot)
    {
        if (!_isEnabled || !isSweetSpot)
        {
            return 0f;
        }

        return Accumulate(_config.SweetSpotFlow);
    }

    /// <summary>
    /// Resets accumulated Flow and alternation history to empty without changing the enabled state.
    /// </summary>
    public void Reset()
    {
        _flow = EmptyFlow;
        _lastCategory = null;
    }

    // Only thrust and sweep participate in alternation; two consecutive different categories among the
    // spear kinds count as alternation. Other kinds (Arrow/Volley/Rain) are not spear categories, but
    // a change to/from any category is still an alternation in the strict thrust<->sweep sense only
    // when both endpoints are spear categories.
    private static bool IsAlternation(ArsenalSkillKind previous, ArsenalSkillKind current)
    {
        bool bothSpear = IsSpearCategory(previous) && IsSpearCategory(current);
        return bothSpear && previous != current;
    }

    private static bool IsSpearCategory(ArsenalSkillKind kind) =>
        kind == ArsenalSkillKind.Thrust || kind == ArsenalSkillKind.Sweep;

    // Adds the amount, clamps to [0, MaxFlow], and returns how much was actually gained (never negative).
    private float Accumulate(float amount)
    {
        if (amount <= 0f)
        {
            return 0f;
        }

        float before = _flow;
        _flow = Mathf.Clamp(_flow + amount, EmptyFlow, MaxFlow);
        return _flow - before;
    }
}
