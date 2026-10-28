using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Seeded, without replacement. Complementary cards get a modest 2:1 weight, never a guarantee.</summary>
public static class BoonOfferDraw
{
    public static IReadOnlyList<RunBoons.Offer> Draw(IEnumerable<RunBoons.Offer> source, Random random, int count = 3)
    {
        if (source == null || random == null || count <= 0) return Array.Empty<RunBoons.Offer>();
        var pool = source.Where(o => o != null).GroupBy(o => o.Id, StringComparer.Ordinal).Select(g => g.First())
            .OrderBy(o => o.Id, StringComparer.Ordinal).ToList();
        if (pool.All(o => o.SelectionWeight == 1))
        {
            // Preserve the historical draw exactly for unweighted pools.
            for (int i = pool.Count - 1; i > 0; i--)
            { int j = random.Next(i + 1); var swap = pool[i]; pool[i] = pool[j]; pool[j] = swap; }
            return pool.Take(count).ToList().AsReadOnly();
        }
        var result = new List<RunBoons.Offer>();
        while (result.Count < count && pool.Count > 0)
        {
            int total = pool.Sum(o => o.SelectionWeight), draw = random.Next(total), index = 0;
            for (; index < pool.Count - 1; index++)
            { draw -= pool[index].SelectionWeight; if (draw < 0) break; }
            result.Add(pool[index]); pool.RemoveAt(index);
        }
        return result.AsReadOnly();
    }
}
