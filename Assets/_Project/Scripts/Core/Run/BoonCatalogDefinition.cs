using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Tech Guy/Run/Boon Capability Catalog")]
public sealed class BoonCatalogDefinition : ScriptableObject
{
    [SerializeField] private List<BoonDefinition> _entries = new List<BoonDefinition>();
    public IReadOnlyList<BoonDefinition> Entries => _entries?.AsReadOnly();
    public bool Configure(IEnumerable<BoonDefinition> entries, out string diagnostic)
    {
        if (entries == null) { diagnostic = "Missing boon definitions."; return false; }
        var authored = entries.ToList();
        if (!BoonCatalogValidator.TryCreateSnapshot(authored, out _, out diagnostic)) return false;
        _entries = authored.Select(entry => entry.Copy()).ToList(); return true;
    }
    public bool TryCreateSnapshot(out BoonCatalogSnapshot snapshot, out string diagnostic)
        => BoonCatalogValidator.TryCreateSnapshot(_entries, out snapshot, out diagnostic);
}

/// <summary>Validated, detached catalog. It contains no executable effects and activates no draft content.</summary>
public sealed class BoonCatalogSnapshot
{
    private readonly Dictionary<string, BoonDefinitionSnapshot> _byId;
    public IReadOnlyList<BoonDefinitionSnapshot> Entries { get; }
    internal BoonCatalogSnapshot(IEnumerable<BoonDefinition> definitions)
    {
        var entries = definitions.OrderBy(d => d.Id, StringComparer.Ordinal).Select(d => d.Snapshot()).ToList();
        Entries = entries.AsReadOnly(); _byId = entries.ToDictionary(d => d.Id, StringComparer.Ordinal);
    }
    public bool TryGet(string id, out BoonDefinitionSnapshot definition)
    {
        definition = null;
        return id != null && _byId.TryGetValue(id, out definition);
    }
}

public static class BoonCatalogValidator
{
    // Maximum declarations accepted in this stage; runtime still reserves the shared ProtocolBudget.
    public const int MaxDeclaredDerivedImpacts = 32;
    public const int MaxDeclaredDepth = 4;
    public static bool TryCreateSnapshot(IEnumerable<BoonDefinition> definitions, out BoonCatalogSnapshot snapshot, out string diagnostic)
    {
        snapshot = null; diagnostic = null;
        if (definitions == null) { diagnostic = "Missing boon definitions."; return false; }
        var entries = definitions.ToList();
        var byId = new Dictionary<string, BoonDefinition>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (!Validate(entry, out diagnostic)) return false;
            if (byId.ContainsKey(entry.Id)) { diagnostic = "Duplicate boon ID: " + entry.Id; return false; }
            byId.Add(entry.Id, entry);
        }
        foreach (var entry in entries)
        {
            foreach (var id in entry.RequiredBoonIds.Concat(entry.BlockedBoonIds))
                if (!byId.ContainsKey(id)) { diagnostic = entry.Id + ": unknown dependency/block ID " + id; return false; }
            if (entry.RequiredBoonIds.Intersect(entry.BlockedBoonIds, StringComparer.Ordinal).Any())
            { diagnostic = entry.Id + ": a required boon is also blocked."; return false; }
            foreach (var id in entry.RequiredBoonIds)
                if (byId[id].BlockedBoonIds.Contains(entry.Id))
                { diagnostic = entry.Id + ": required boon blocks this definition."; return false; }
        }
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in entries)
            if (HasDependencyCycle(entry, byId, visiting, visited))
            { diagnostic = "Cyclic boon prerequisite: " + entry.Id; return false; }
        snapshot = new BoonCatalogSnapshot(entries); return true;
    }

    private static bool Validate(BoonDefinition entry, out string diagnostic)
    {
        diagnostic = null;
        if (entry == null) { diagnostic = "Null boon definition."; return false; }
        string error = null;
        if (!ValidId(entry.Id)) error = "ID must be explicit, nonempty, and contain no whitespace.";
        else if (string.IsNullOrWhiteSpace(entry.Title)) error = "Missing presentation title.";
        else if (!Enum.IsDefined(typeof(BoonFamily), entry.Family)) error = "Unknown boon family.";
        else if (entry.Weapons == BoonWeaponMask.None || (entry.Weapons & ~BoonWeaponMask.Universal) != 0) error = "Invalid weapon mask.";
        else if (entry.Slots == BoonSlotMask.None || (entry.Slots & ~BoonSlotMask.AllTargets) != 0) error = "Invalid slot mask.";
        else if ((entry.Slots & BoonSlotMask.Basic) != 0 && entry.Weapons != BoonWeaponMask.Gauntlet) error = "Basic targeting is limited to Gauntlet in this stage.";
        else if (!ValidCapabilities(entry.RequiredCapabilities | entry.AnyRequiredCapabilities | entry.ProvidedCapabilities | entry.RemovedCapabilities)) error = "Unknown capability flag.";
        else if (entry.RequiredBoonIds == null || entry.BlockedBoonIds == null) error = "Missing prerequisite/block list.";
        else if (!ValidateIds(entry.RequiredBoonIds, entry.Id) || !ValidateIds(entry.BlockedBoonIds, entry.Id)) error = "Invalid, duplicate, or self-referencing prerequisite/block ID.";
        else if (entry.MaxRank < 1) error = "Rank limit must be positive.";
        else if (!entry.IsRetainedSupport && (entry.Family == BoonFamily.Mutation || entry.Family == BoonFamily.Resonance || entry.Family == BoonFamily.Mythic) && entry.MaxRank != 1) error = "Replacement, Resonance, and Mythic ranks are limited to one.";
        else if (!entry.IsRetainedSupport && entry.Family == BoonFamily.Mutation && !SingleSlot(entry.Slots)) error = "A replacement Mutation must name exactly one slot.";
        else if (entry.RemovedCapabilities != BoonCapability.None && (entry.Family != BoonFamily.Mutation || entry.IsRetainedSupport)) error = "Only a replacement Mutation can remove base capabilities.";
        else if ((entry.ProvidedCapabilities & entry.RemovedCapabilities) != 0) error = "A capability cannot be both provided and removed.";
        else if (entry.EnemyPolicies == null || !ValidPolicy(entry.EnemyPolicies.Common) || !ValidPolicy(entry.EnemyPolicies.Heavy) || !ValidPolicy(entry.EnemyPolicies.Elite) || !ValidPolicy(entry.EnemyPolicies.Boss)) error = "Missing or unknown enemy control policy.";
        else if ((entry.Presentation & ~BoonPresentationCriteria.Any) != 0) error = "Unknown presentation criterion.";
        else if ((entry.IsTransformative || entry.Family == BoonFamily.Mutation && !entry.IsRetainedSupport) && CountBits((int)entry.Presentation) < 2) error = "A transformative choice must change at least two gameplay criteria.";
        else if (!ValidImpactRules(entry.ImpactRules, out error)) { }
        else if (entry.Cost == null || !FiniteNonnegative(entry.Cost.ManaCost) || !FiniteNonnegative(entry.Cost.Cooldown)) error = "Mana cost and cooldown authoring must be finite and nonnegative.";
        else if ((entry.Cost.OverrideManaCost || entry.Cost.OverrideCooldown) && (entry.Family != BoonFamily.Mutation || entry.IsRetainedSupport)) error = "Only a replacement Mutation can override slot costs.";
        if (error == null) return true;
        diagnostic = (entry.Id ?? "<missing ID>") + ": " + error; return false;
    }

    private static bool ValidImpactRules(BoonImpactRules rules, out string error)
    {
        error = null;
        if (rules == null) error = "Missing origin/impact limits.";
        else if (rules.TriggerOrigins == BoonOriginMask.None || (rules.TriggerOrigins & ~BoonOriginMask.Any) != 0 ||
            rules.TriggerImpacts == BoonImpactMask.None || (rules.TriggerImpacts & ~BoonImpactMask.Any) != 0 ||
            !Enum.IsDefined(typeof(BoonProducedImpact), rules.ProducedImpact)) error = "Invalid origin/impact policy.";
        else if (rules.MaxInstancesPerCast < 1 || rules.MaxDerivedImpactsPerCast < 0 || rules.MaxDerivedImpactsPerCast > MaxDeclaredDerivedImpacts ||
            rules.MaxDepth < 0 || rules.MaxDepth > MaxDeclaredDepth) error = "Invalid per-cast instance, impact, or depth limit.";
        else if (rules.ProducedImpact != BoonProducedImpact.Native &&
            (rules.CanGenerateEnergy || rules.CanCreateCausalRoot || rules.CanSpawnSkillEntities)) error = "Derived, Echo, and StatusTick effects cannot generate energy, create causal roots, or spawn whole skill entities.";
        else if ((rules.TriggerImpacts & ~BoonImpactMask.Native) != 0 &&
            (rules.ProducedImpact == BoonProducedImpact.Native || rules.CanGenerateEnergy || rules.CanCreateCausalRoot || rules.CanSpawnSkillEntities)) error = "A derived trigger cannot be promoted into a fresh native producer.";
        else if (rules.ProducedImpact != BoonProducedImpact.Native && (rules.MaxDerivedImpactsPerCast == 0 || rules.MaxDepth == 0)) error = "A derived effect requires an explicit positive impact and depth budget.";
        return error == null;
    }

    private static bool HasDependencyCycle(BoonDefinition entry, Dictionary<string, BoonDefinition> byId, HashSet<string> visiting, HashSet<string> visited)
    {
        if (visited.Contains(entry.Id)) return false;
        if (!visiting.Add(entry.Id)) return true;
        foreach (var id in entry.RequiredBoonIds)
            if (HasDependencyCycle(byId[id], byId, visiting, visited)) return true;
        visiting.Remove(entry.Id); visited.Add(entry.Id); return false;
    }
    private static bool ValidateIds(IReadOnlyList<string> ids, string owner)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var id in ids) if (!ValidId(id) || id == owner || !seen.Add(id)) return false;
        return true;
    }
    private static bool ValidId(string id) => !string.IsNullOrWhiteSpace(id) && !id.Any(char.IsWhiteSpace);
    private static bool ValidPolicy(BoonEnemyPolicy policy) => Enum.IsDefined(typeof(BoonEnemyPolicy), policy);
    internal static bool ValidCapabilities(BoonCapability capabilities) => (int)capabilities >= 0;
    internal static bool SingleSlot(BoonSlotMask slots) => slots != BoonSlotMask.None && ((int)slots & ((int)slots - 1)) == 0;
    internal static bool FiniteNonnegative(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
    private static int CountBits(int value) { int count = 0; while (value != 0) { value &= value - 1; count++; } return count; }
}
