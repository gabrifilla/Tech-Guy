using System;
using UnityEngine;

/// <summary>
/// Data configuration for the Lança's optional <b>Flow</b> resource (Requisito 9.7). It describes how
/// much Flow good execution grants and the ceiling the resource may reach.
///
/// Flow is accumulated by <see cref="SpearFlow"/> on two events:
/// <list type="bullet">
/// <item><description><see cref="SweetSpotFlow"/> — granted when a thrust connects on the Sweet Spot.</description></item>
/// <item><description><see cref="AlternationFlow"/> — granted when the player alternates between the two
/// spear categories (thrust ↔ sweep).</description></item>
/// </list>
///
/// The amounts are clamped to be non-negative and the cap to be strictly positive, so a config can never
/// produce a negative resource or a zero ceiling. This is a plain, serializable value type with no scene
/// dependency, so it can be embedded on an ability/loadout asset and exercised in isolation. It is
/// consumed by <see cref="SpearFlow"/>.
/// </summary>
[Serializable]
public struct SpearFlowConfig
{
    /// <summary>Smallest allowed value for the Flow cap; a resource always has a positive ceiling.</summary>
    public const float MinMaxFlow = 0.0001f;

    [SerializeField, Min(0f), Tooltip("Flow granted by a thrust hit that lands on the Sweet Spot.")]
    private float _sweetSpotFlow;

    [SerializeField, Min(0f), Tooltip("Flow granted when alternating between thrust and sweep.")]
    private float _alternationFlow;

    [SerializeField, Min(MinMaxFlow), Tooltip("Maximum accumulated Flow. Must be strictly greater than 0.")]
    private float _maxFlow;

    /// <summary>
    /// Builds a Flow configuration.
    /// </summary>
    /// <param name="sweetSpotFlow">Flow granted per Sweet Spot hit; clamped to be non-negative.</param>
    /// <param name="alternationFlow">Flow granted per thrust↔sweep alternation; clamped to be non-negative.</param>
    /// <param name="maxFlow">Cap on accumulated Flow; clamped to be strictly positive.</param>
    public SpearFlowConfig(float sweetSpotFlow, float alternationFlow, float maxFlow)
    {
        _sweetSpotFlow = Mathf.Max(0f, sweetSpotFlow);
        _alternationFlow = Mathf.Max(0f, alternationFlow);
        _maxFlow = Mathf.Max(MinMaxFlow, maxFlow);
    }

    /// <summary>Flow granted by a thrust hit on the Sweet Spot; never negative.</summary>
    public float SweetSpotFlow => Mathf.Max(0f, _sweetSpotFlow);

    /// <summary>Flow granted when alternating between thrust and sweep; never negative.</summary>
    public float AlternationFlow => Mathf.Max(0f, _alternationFlow);

    /// <summary>Maximum accumulated Flow; always strictly positive.</summary>
    public float MaxFlow => Mathf.Max(MinMaxFlow, _maxFlow);

    /// <summary>
    /// A sensible default: a Sweet Spot hit grants more Flow than a plain alternation, and the cap is the
    /// conventional 100. Useful for tests and inspector defaults.
    /// </summary>
    public static SpearFlowConfig Default => new SpearFlowConfig(15f, 10f, SpearFlow.DefaultMaxFlow);
}
