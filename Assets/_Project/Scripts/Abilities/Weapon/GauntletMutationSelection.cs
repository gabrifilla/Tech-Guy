using System;
using System.Collections.Generic;

/// <summary>Run inventory selection: one mutation per slot, with explicit replacement.</summary>
public sealed class GauntletMutationSelection
{
    private readonly Dictionary<GauntletActionSlot, GauntletMutation> _slots = new Dictionary<GauntletActionSlot, GauntletMutation>();
    public bool TryGet(GauntletActionSlot slot, out GauntletMutation mutation) => _slots.TryGetValue(slot, out mutation);
    internal void Clear() => _slots.Clear();
    public bool Select(GauntletMutation mutation, bool replace = false)
    {
        if (!Enum.IsDefined(typeof(GauntletMutation), mutation)) return false;
        GauntletActionSlot slot = SlotOf(mutation);
        if (_slots.TryGetValue(slot, out var current) && (current == mutation || !replace)) return false;
        _slots[slot] = mutation;
        return true;
    }
    public static GauntletActionSlot SlotOf(GauntletMutation mutation)
    {
        switch (mutation)
        {
            case GauntletMutation.DemolishingAdvance: return GauntletActionSlot.Q;
            case GauntletMutation.PressureField: return GauntletActionSlot.W;
            case GauntletMutation.Epicenter: return GauntletActionSlot.E;
            case GauntletMutation.CadencedAsura: return GauntletActionSlot.R;
            default: throw new ArgumentOutOfRangeException(nameof(mutation));
        }
    }
}
