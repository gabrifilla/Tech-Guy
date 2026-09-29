using System.Collections.Generic;

/// <summary>
/// Pure generation core that builds the <see cref="RoomComposition"/> of a single
/// Combat_Room. It has two responsibilities: computing the depth-scaled
/// <see cref="RoomComposition.TargetDensity"/> within the Density_Budget range, and
/// selecting the distinct archetypes plus distributing the target enemy count across
/// them. It holds no scene references and derives every random decision exclusively
/// from a caller-supplied <see cref="SeededRng"/>, so planning is fully deterministic
/// for a given seed and never touches <c>UnityEngine.Random</c>.
/// </summary>
/// <remarks>
/// Feature: procedural-stage-room-generation. Requirements: 5.1, 5.2, 5.3, 5.5.
/// </remarks>
public sealed class RoomCompositionPlanner
{
    /// <summary>
    /// Computes the target enemy count for a Combat_Room at the given
    /// <paramref name="depth"/>, scaled monotonically across the configured
    /// Density_Budget range. The result is always within
    /// [<see cref="StageGenerationParams.DensityBudgetMin"/>,
    /// <see cref="StageGenerationParams.DensityBudgetMax"/>], which itself lies within
    /// [4, 30]. The mapping is a deterministic integer interpolation of depth over
    /// <paramref name="maxDepth"/>, so for any two rooms A and B where
    /// depth(A) &gt; depth(B) the result for A is greater than or equal to the result
    /// for B, and it never exceeds the budget maximum (and therefore never exceeds 30).
    /// No randomness is consumed: the scaling is purely a function of depth.
    /// </summary>
    /// <param name="parameters">The generation parameters providing the budget bounds.</param>
    /// <param name="depth">
    /// The Combat_Room depth (number of connections from the Start_Room). Negative
    /// values are treated as 0.
    /// </param>
    /// <param name="maxDepth">
    /// The deepest depth in the current Stage, used as the interpolation ceiling.
    /// Values less than or equal to 0 collapse the range to the budget maximum for a
    /// single-depth Stage. Depths at or beyond <paramref name="maxDepth"/> map to the
    /// budget maximum.
    /// </param>
    /// <returns>The depth-scaled target density, clamped into the budget range.</returns>
    public int ComputeTargetDensity(StageGenerationParams parameters, int depth, int maxDepth)
    {
        int min = ClampToBudget(parameters == null ? StageGenerationParams.DensityBudgetFloor : parameters.DensityBudgetMin);
        int max = ClampToBudget(parameters == null ? StageGenerationParams.DensityBudgetCeiling : parameters.DensityBudgetMax);
        if (max < min)
        {
            max = min;
        }

        if (depth < 0)
        {
            depth = 0;
        }

        // No spread to scale across, or a single-depth Stage: use the deepest budget.
        if (max == min || maxDepth <= 0)
        {
            return max;
        }

        if (depth >= maxDepth)
        {
            return max;
        }

        // Integer floor interpolation: strictly non-decreasing in depth, bounded by max.
        int span = max - min;
        int scaled = min + (span * depth) / maxDepth;
        if (scaled < min)
        {
            scaled = min;
        }
        else if (scaled > max)
        {
            scaled = max;
        }

        return scaled;
    }

    /// <summary>
    /// Builds the full <see cref="RoomComposition"/> for a Combat_Room: computes the
    /// depth-scaled target density, selects the distinct archetypes to satisfy the
    /// Variety_Target (subject to availability) and distributes the target enemy count
    /// across the chosen archetypes so that each chosen archetype receives at least one
    /// enemy. All decisions are drawn from <paramref name="rng"/>.
    /// </summary>
    /// <param name="parameters">The generation parameters (budget + variety target).</param>
    /// <param name="availableArchetypes">
    /// The archetypes available for this room. Tests may pass fewer than the
    /// Variety_Target to exercise the availability cap. Duplicates are collapsed. A null
    /// or empty set yields an empty composition (no enemies).
    /// </param>
    /// <param name="depth">The Combat_Room depth from the Start_Room.</param>
    /// <param name="maxDepth">The deepest depth in the current Stage.</param>
    /// <param name="rng">The isolated, seeded RNG driving every random decision.</param>
    /// <returns>
    /// A composition whose <see cref="RoomComposition.TargetDensity"/> is the depth-scaled
    /// budget value and whose slots realize the selected variety and distribution.
    /// </returns>
    public RoomComposition Plan(
        StageGenerationParams parameters,
        IReadOnlyList<ArchetypeId> availableArchetypes,
        int depth,
        int maxDepth,
        SeededRng rng)
    {
        int targetDensity = ComputeTargetDensity(parameters, depth, maxDepth);

        var distinctPool = BuildDistinctPool(availableArchetypes);
        if (distinctPool.Count == 0 || rng == null)
        {
            return new RoomComposition(null, targetDensity);
        }

        int varietyTarget = parameters == null ? StageGenerationParams.VarietyTargetFloor : parameters.VarietyTarget;

        // Distinct archetypes must reach the Variety_Target, capped by availability
        // (R5.3) and by the target density (cannot give each of N archetypes >= 1
        // enemy when N exceeds the total enemy count).
        int distinctCount = varietyTarget;
        if (distinctCount > distinctPool.Count)
        {
            distinctCount = distinctPool.Count;
        }

        if (targetDensity > 0 && distinctCount > targetDensity)
        {
            distinctCount = targetDensity;
        }

        if (distinctCount < 1)
        {
            distinctCount = 1;
        }

        var chosen = SelectDistinct(distinctPool, distinctCount, rng);
        var counts = DistributeDensity(chosen.Count, targetDensity, rng);

        var slots = new List<ArchetypeSlot>(chosen.Count);
        for (int i = 0; i < chosen.Count; i++)
        {
            slots.Add(new ArchetypeSlot(chosen[i], counts[i]));
        }

        return new RoomComposition(slots, targetDensity);
    }

    /// <summary>Clamps a raw budget value into the spec-defined [4, 30] range.</summary>
    private static int ClampToBudget(int value)
    {
        if (value < StageGenerationParams.DensityBudgetFloor)
        {
            return StageGenerationParams.DensityBudgetFloor;
        }

        if (value > StageGenerationParams.DensityBudgetCeiling)
        {
            return StageGenerationParams.DensityBudgetCeiling;
        }

        return value;
    }

    /// <summary>
    /// Collapses the available archetypes into a stable, duplicate-free list, preserving
    /// first-seen order so selection is deterministic given the RNG stream.
    /// </summary>
    private static List<ArchetypeId> BuildDistinctPool(IReadOnlyList<ArchetypeId> availableArchetypes)
    {
        var pool = new List<ArchetypeId>();
        if (availableArchetypes == null)
        {
            return pool;
        }

        var seen = new HashSet<ArchetypeId>();
        for (int i = 0; i < availableArchetypes.Count; i++)
        {
            ArchetypeId id = availableArchetypes[i];
            if (seen.Add(id))
            {
                pool.Add(id);
            }
        }

        return pool;
    }

    /// <summary>
    /// Selects <paramref name="count"/> distinct archetypes from <paramref name="pool"/>
    /// using a seeded partial Fisher-Yates shuffle so the choice is unbiased and
    /// reproducible for a given seed. The pool is copied so the caller's data is untouched.
    /// </summary>
    private static List<ArchetypeId> SelectDistinct(List<ArchetypeId> pool, int count, SeededRng rng)
    {
        var working = new List<ArchetypeId>(pool);
        if (count >= working.Count)
        {
            return working;
        }

        var chosen = new List<ArchetypeId>(count);
        int last = working.Count - 1;
        for (int i = 0; i < count; i++)
        {
            int pick = rng.NextInt(0, last + 1);
            chosen.Add(working[pick]);

            // Swap the picked entry to the tail and shrink the selectable window.
            working[pick] = working[last];
            working[last] = chosen[chosen.Count - 1];
            last--;
        }

        return chosen;
    }

    /// <summary>
    /// Distributes <paramref name="targetDensity"/> enemies across
    /// <paramref name="archetypeCount"/> chosen archetypes so that each receives at least
    /// one enemy (when density allows) and the counts sum exactly to the target. Any
    /// surplus beyond the guaranteed one-per-archetype is scattered randomly, keeping the
    /// distribution seed-reproducible.
    /// </summary>
    private static int[] DistributeDensity(int archetypeCount, int targetDensity, SeededRng rng)
    {
        var counts = new int[archetypeCount];
        if (archetypeCount == 0)
        {
            return counts;
        }

        if (targetDensity <= 0)
        {
            return counts;
        }

        // Guarantee at least one enemy per chosen archetype (density is >= distinct
        // count by construction in Plan).
        int guaranteed = archetypeCount <= targetDensity ? archetypeCount : targetDensity;
        for (int i = 0; i < guaranteed; i++)
        {
            counts[i] = 1;
        }

        int remaining = targetDensity - guaranteed;
        for (int i = 0; i < remaining; i++)
        {
            int bucket = rng.NextInt(0, archetypeCount);
            counts[bucket]++;
        }

        return counts;
    }
}
