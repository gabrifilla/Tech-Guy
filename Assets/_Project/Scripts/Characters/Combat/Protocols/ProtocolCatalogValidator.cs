using System;
using System.Collections.Generic;

/// <summary>Validates raw authoring data. No clamping and no gameplay or asset mutations.</summary>
public static class ProtocolCatalogValidator
{
    public static string IdFor(ProtocolKind kind)
    {
        switch (kind)
        {
            case ProtocolKind.Ignite: return "ignite";
            case ProtocolKind.Frost: return "frost";
            case ProtocolKind.Conductor: return "conductor";
            case ProtocolKind.Detonation: return "detonation";
            case ProtocolKind.Reactor: return "reactor";
            case ProtocolKind.Resonance: return "resonance";
            case ProtocolKind.Overflow: return "overflow";
            default: throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public static bool TryCreate(ProtocolCatalogDefinition source, out ProtocolCatalogSnapshot snapshot, out string diagnostic)
    {
        snapshot = null;
        diagnostic = Validate(source);
        if (diagnostic != null) return false;
        snapshot = new ProtocolCatalogSnapshot(source);
        return true;
    }

    private static string Validate(ProtocolCatalogDefinition source)
    {
        if (!source) return "Protocol catalog: missing definition.";
        if (source.Revision <= 0) return "Protocol catalog: Revision must be positive.";
        if (source.Entries == null || source.Entries.Count != 7) return "Protocol catalog: exactly seven entries required.";
        var kinds = new HashSet<ProtocolKind>();
        foreach (var entry in source.Entries)
        {
            if (entry == null) return "Protocol catalog: null entry.";
            if (!Enum.IsDefined(typeof(ProtocolKind), entry.Kind)) return "Protocol catalog: unknown Kind.";
            if (entry.Id != IdFor(entry.Kind)) return entry.Kind + ": Id does not match stable identity.";
            if (!kinds.Add(entry.Kind)) return entry.Id + ": duplicate Kind.";
            if (entry.MaxRank != 3) return entry.Id + ": MaxRank must be 3 for this catalog.";
            float[] values = { entry.DamagePerRank, entry.DamageFactorBase, entry.DamageFactorPerAdditionalRank,
                entry.RadiusBase, entry.RadiusPerRank, entry.Duration, entry.TickInterval, entry.ChancePerRank, entry.SlowFraction };
            string[] fields = { "DamagePerRank", "DamageFactorBase", "DamageFactorPerAdditionalRank", "RadiusBase",
                "RadiusPerRank", "Duration", "TickInterval", "ChancePerRank", "SlowFraction" };
            for (int i = 0; i < values.Length; i++)
                if (!FiniteNonNegative(values[i])) return entry.Id + ": invalid " + fields[i] + ".";
            if (entry.ChancePerRank > 1f || entry.SlowFraction > 1f) return entry.Id + ": chance/slow must be in [0,1].";
            if (entry.MaxVictims < 0) return entry.Id + ": invalid MaxVictims.";
            if ((entry.Kind == ProtocolKind.Ignite || entry.Kind == ProtocolKind.Frost) && entry.Duration <= 0f)
                return entry.Id + ": Duration must be positive.";
            if (entry.Kind == ProtocolKind.Ignite && (entry.TickInterval <= 0f || entry.TickInterval > entry.Duration || entry.DamagePerRank <= 0f))
                return entry.Id + ": invalid burn intensity/interval.";
            if ((entry.Kind == ProtocolKind.Conductor || entry.Kind == ProtocolKind.Detonation) && entry.RadiusBase <= 0f)
                return entry.Id + ": RadiusBase must be positive.";
            if (entry.Kind == ProtocolKind.Detonation && entry.MaxVictims <= 0) return entry.Id + ": MaxVictims must be positive.";
            bool ignite = entry.Kind == ProtocolKind.Reactor;
            bool element = entry.Kind == ProtocolKind.Resonance || entry.Kind == ProtocolKind.Overflow;
            bool propagation = entry.Kind == ProtocolKind.Resonance;
            if (entry.RequiresIgnite != ignite || entry.RequiresElement != element || entry.RequiresPropagation != propagation)
                return entry.Id + ": prerequisites differ from D1.";
        }
        var limits = source.Limits;
        if (limits == null) return "Protocol catalog: missing Limits.";
        if (limits.Clock != ProtocolClock.ScaledGameplay) return "Limits: unsupported Clock.";
        int[] caps = { limits.ImpactsPerRoot, limits.MaxDepth, limits.QueuePerRoot, limits.ReservationsPerRoot,
            limits.LiveRoots, limits.ImpactsPerFrame, limits.QueriesPerFrame, limits.CollidersPerQuery,
            limits.SightTestsPerQuery, limits.ApplicationsPerType, limits.BurnTicksPerFrame, limits.ActiveVfx, limits.NewVfxPerFrame };
        string[] capFields = { "ImpactsPerRoot", "MaxDepth", "QueuePerRoot", "ReservationsPerRoot", "LiveRoots",
            "ImpactsPerFrame", "QueriesPerFrame", "CollidersPerQuery", "SightTestsPerQuery", "ApplicationsPerType",
            "BurnTicksPerFrame", "ActiveVfx", "NewVfxPerFrame" };
        for (int i = 0; i < caps.Length; i++) if (caps[i] <= 0) return "Limits: invalid " + capFields[i] + ".";
        int[] maximumCaps = { 32, 4, 64, 32, 64, 256, 64, 128, 128, 256, 64, 24, 16 };
        for (int i = 0; i < caps.Length; i++) if (caps[i] > maximumCaps[i]) return "Limits: " + capFields[i] + " exceeds D6 caps.";
        if (limits.CollidersPerQuery > 128 || limits.ApplicationsPerType > int.MaxValue / 2 ||
            limits.ImpactsPerRoot > int.MaxValue - limits.QueuePerRoot) return "Limits: query buffer exceeds D6 or combined counters overflow.";
        if (limits.QueuePerRoot < limits.ImpactsPerRoot || limits.ImpactsPerFrame < limits.ImpactsPerRoot ||
            limits.SightTestsPerQuery > limits.CollidersPerQuery || limits.BurnTicksPerFrame > limits.ApplicationsPerType ||
            limits.NewVfxPerFrame > limits.ActiveVfx) return "Limits: incoherent caps.";
        float[] timings = { limits.VfxLifetime, limits.RootLifetime, limits.ThermalShockMultiplier, limits.CombustionRadiusMultiplier };
        foreach (float value in timings) if (!FiniteNonNegative(value) || value <= 0f) return "Limits: invalid timing/multiplier.";
        if (limits.VfxLifetime > .6f || limits.RootLifetime > 10f) return "Limits: lifetime exceeds D6.";
        float[] slows = { limits.HeavyMaxSlow, limits.EliteMaxSlow, limits.BossMaxSlow };
        foreach (float value in slows) if (!FiniteNonNegative(value) || value >= .9f) return "Limits: invalid resistant Slow.";
        if (limits.HeavyMaxSlow < limits.EliteMaxSlow || limits.EliteMaxSlow < limits.BossMaxSlow)
            return "Limits: resistance order is incoherent.";
        if (limits.ShatterMinTrails < 3 || limits.ShatterMaxTrails > 6 || limits.ShatterMaxTrails < limits.ShatterMinTrails)
            return "Limits: invalid shatter trail range.";
        return null;
    }
    private static bool FiniteNonNegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0f;
}
