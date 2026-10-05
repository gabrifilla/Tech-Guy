using System.Collections.Generic;

public static class ProtocolRewardSelection
{
    public static IReadOnlyList<ProtocolKind> Select(ProtocolInventory inventory, int opportunity)
    {
        var result = new List<ProtocolKind>();
        if (inventory == null || opportunity < 1) return result.AsReadOnly();
        void Add(ProtocolKind kind)
        { if (result.Count < 3 && !result.Contains(kind) && inventory.CanAcquire(kind)) result.Add(kind); }
        if (opportunity == 1)
        { Add(ProtocolKind.Ignite); Add(ProtocolKind.Frost); return result.AsReadOnly(); }
        if (opportunity == 2)
        {
            Add(ProtocolKind.Conductor); Add(ProtocolKind.Detonation);
            Add(inventory.CanAcquire(ProtocolKind.Reactor) ? ProtocolKind.Reactor : ProtocolKind.Overflow);
        }
        else if (opportunity == 3)
        {
            if (inventory.Rank(ProtocolKind.Ignite) == 0) Add(ProtocolKind.Ignite);
            if (inventory.Rank(ProtocolKind.Frost) == 0) Add(ProtocolKind.Frost);
            Add(ProtocolKind.Overflow);
            if (inventory.CanAcquire(ProtocolKind.Resonance)) Add(ProtocolKind.Resonance);
            else if (inventory.Rank(ProtocolKind.Detonation) == 0) Add(ProtocolKind.Detonation);
            else if (inventory.Rank(ProtocolKind.Conductor) == 0) Add(ProtocolKind.Conductor);
        }
        else if (opportunity == 4)
        { Add(ProtocolKind.Resonance); Add(ProtocolKind.Reactor); Add(ProtocolKind.Detonation); }
        else
            foreach (var entry in inventory.Catalog.Entries) if (inventory.Rank(entry.Kind) > 0) Add(entry.Kind);
        foreach (var entry in inventory.Catalog.Entries) Add(entry.Kind);
        return result.AsReadOnly();
    }
}
