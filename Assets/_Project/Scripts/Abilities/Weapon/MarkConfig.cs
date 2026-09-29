using System;

/// <summary>
/// Data for the Bow's exclusive <b>Mark</b> mechanic (R10.2). Holds the mark's validity
/// (how long a designation stays live) and the priority-reinforcement flags that decide which
/// behaviors chase the marked target when one is present (R10.3): homing, ricochet and
/// Heavy Bolt priority.
///
/// This is a pure, scene-free data holder so <see cref="WeaponMark"/> can be property-tested
/// without a live Unity scene (design: "WeaponMark / MarkConfig"). Tunable values (validity,
/// reinforcement toggles) live here as data rather than as magic numbers in the coordinator
/// MonoBehaviour, following AGENTS.md.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 12.1.
/// Requirements: 10.2, 10.3, 10.8.
/// </remarks>
[Serializable]
public struct MarkConfig
{
    /// <summary>
    /// How long, in seconds, a mark stays valid after being applied. Must be &gt; 0 for a mark
    /// to be usable; a non-positive validity means marks never take hold (they expire the
    /// instant they are set), which <see cref="WeaponMark"/> treats as "no mark present".
    /// </summary>
    public float ValiditySeconds;

    /// <summary>When true, homing is reinforced toward the marked target while a mark is live (R10.3).</summary>
    public bool ReinforceHoming;

    /// <summary>When true, ricochet is reinforced toward the marked target while a mark is live (R10.3).</summary>
    public bool ReinforceRicochet;

    /// <summary>When true, Heavy Bolt priority is reinforced toward the marked target while a mark is live (R10.3).</summary>
    public bool ReinforceHeavyBolt;

    /// <summary>
    /// A conventional default: a mark that lasts a few seconds and reinforces all three priority
    /// behaviors. Concrete tuning is expected to come from serialized data on the Bow coordinator.
    /// </summary>
    public static MarkConfig Default => new MarkConfig
    {
        ValiditySeconds = 6f,
        ReinforceHoming = true,
        ReinforceRicochet = true,
        ReinforceHeavyBolt = true,
    };
}
