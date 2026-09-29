using UnityEngine;

/// <summary>
/// Pure, scene-free model of a shield's finite absorption buffer. Extracted from the
/// <see cref="Shield"/> MonoBehaviour so the absorb math can be property-tested without a live Unity
/// scene (task 2.3, Property 7).
///
/// The buffer holds a remaining capacity and, on each <see cref="Absorb(float)"/>, consumes as much
/// of the incoming damage as it can and returns the leftover that must still reduce health. It never
/// increases the incoming damage and never lets capacity go negative, so once depleted it passes all
/// further damage straight through (R14.3).
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 2.2. Requirements: 14.3.</remarks>
public struct ShieldBuffer
{
    private float _remaining;

    /// <summary>Creates a buffer with the given absorption capacity, clamped to be non-negative.</summary>
    public ShieldBuffer(float capacity)
    {
        _remaining = Mathf.Max(0f, capacity);
    }

    /// <summary>The absorption capacity still available before the buffer is depleted.</summary>
    public float Remaining => _remaining;

    /// <summary>True once the buffer can absorb no more damage.</summary>
    public bool IsDepleted => _remaining <= 0f;

    /// <summary>
    /// Consumes up to <see cref="Remaining"/> of <paramref name="amount"/> and returns the leftover
    /// damage that should still be applied to health. The result is always in <c>[0, amount]</c>:
    /// a non-positive hit is returned as 0, a hit smaller than the buffer is fully absorbed (returns
    /// 0), and a hit larger than the buffer depletes it and returns the overflow.
    /// </summary>
    public float Absorb(float amount)
    {
        if (amount <= 0f) return 0f;

        float absorbed = Mathf.Min(_remaining, amount);
        _remaining -= absorbed;
        return amount - absorbed;
    }
}
