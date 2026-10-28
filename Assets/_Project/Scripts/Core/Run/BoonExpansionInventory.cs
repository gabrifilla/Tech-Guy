using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Run-owned acquisition records. Every addition is preflighted against the live capability graph.</summary>
public sealed class BoonExpansionInventory
{
    private readonly Dictionary<string, BoonAcquisition> _records = new Dictionary<string, BoonAcquisition>(StringComparer.Ordinal);
    public IReadOnlyList<BoonAcquisition> Acquisitions => _records.Values.OrderBy(a => a.Id, StringComparer.Ordinal).Select(a => a.Copy()).ToList().AsReadOnly();
    public bool Has(string id) => id != null && _records.ContainsKey(id);
    public bool Has(string id, GauntletActionSlot slot) => _records.TryGetValue(id, out var record) &&
        slot >= GauntletActionSlot.Q && slot <= GauntletActionSlot.R && (record.Slots & (BoonSlotMask)(1 << (int)slot)) != 0;
    public string MutationId(BoonSlot slot)
    {
        var mask = BoonCapabilityGraph.Mask(slot);
        foreach (var record in _records.Values)
            if ((record.Slots & mask) != 0 && BoonExpansionCatalog.TryDefinition(record.Id, out var definition) && definition.Family == BoonFamily.Mutation) return record.Id;
        return null;
    }
    internal bool Record(string id, BoonSlotMask slots)
    {
        if (string.IsNullOrEmpty(id) || slots == BoonSlotMask.None || _records.ContainsKey(id)) return false;
        _records.Add(id, new BoonAcquisition(id, 1, slots)); return true;
    }
    internal void Clear() => _records.Clear();
}
