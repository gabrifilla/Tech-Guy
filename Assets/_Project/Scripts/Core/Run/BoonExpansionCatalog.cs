using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Only implemented extensions belong here. Drafts remain in the authoring manifest.</summary>
public static class BoonExpansionCatalog
{
    private static readonly IReadOnlyList<BoonDefinition> Executable = BuildDefinitions().ToList().AsReadOnly();
    public static IEnumerable<BoonDefinition> Definitions() => Executable;
    private static IEnumerable<BoonDefinition> BuildDefinitions()
    {
        yield return TitanicFistsDefinition.Create();
        foreach (var definition in GauntletExpansionCatalog.Definitions()) yield return definition;
        foreach (var definition in BowBoonCatalog.Definitions()) yield return definition;
        foreach (var definition in SpearBoonCatalog.Definitions()) yield return definition;
    }
    private static readonly Dictionary<string, BoonDefinition> ById = Executable.ToDictionary(d => d.Id, StringComparer.Ordinal);
    public static bool TryDefinition(string id, out BoonDefinition definition)
    { definition = null; return id != null && ById.TryGetValue(id, out definition); }
    public static bool HasTarget(BoonDefinition definition, WeaponScript weapon)
    {
        if (definition == null || !weapon || weapon.abilities == null || weapon.abilities.Length != 4 ||
            (definition.Weapons & Mask(WeaponRunModifiers.Identify(weapon))) == 0) return false;
        if ((definition.Slots & BoonSlotMask.Basic) != 0 && WeaponRunModifiers.Identify(weapon) == RunWeaponFamily.Gauntlet && weapon.BasicProfile != null) return true;
        for (int i = 0; i < 4; i++)
        {
            if ((definition.Slots & (BoonSlotMask)(1 << i)) == 0) continue;
            if (WeaponRunModifiers.Identify(weapon) == RunWeaponFamily.Gauntlet)
            { if (weapon.abilities[i] is BreakerGauntletAbility) return true; }
            else if (weapon.abilities[i] is ArsenalAbility) return true;
        }
        return false;
    }
    public static BoonSlotMask Targets(BoonCapabilityGraph graph, BoonDefinition definition)
    {
        if (graph == null || definition == null) return BoonSlotMask.None;
        // Resolve each support to exactly one live skill, never borrowing a capability from another slot.
        foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
            if ((definition.Slots & BoonCapabilityGraph.Mask(slot)) != 0 && graph.CanAcquire(definition.Id, slot, out _))
                return BoonCapabilityGraph.Mask(slot);
        return BoonSlotMask.None;
    }
    public static bool TryAcquire(string id, WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory protocols, out string diagnostic)
    {
        diagnostic = "Unknown or incompatible expansion.";
        if (modifiers == null || !TryDefinition(id, out var definition) || !HasTarget(definition, weapon) ||
            !BoonOfferCompatibility.TryBuild(modifiers, weapon, protocols, out var graph, out diagnostic)) return false;
        var targets = Targets(graph, definition);
        if (!graph.TryAcquire(id, targets, out diagnostic)) return false;
        return modifiers.Expansion.Record(id, targets);
    }
    public static IEnumerable<RunBoons.Offer> Offers(RunRewardKind kind, WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory protocols)
    {
        if (!BoonOfferCompatibility.TryBuild(modifiers, weapon, protocols, out var graph, out _)) yield break;
        foreach (var definition in Definitions().OrderBy(d => d.Id, StringComparer.Ordinal))
        {
            if (!definition.EnabledForOffers || !HasTarget(definition, weapon) ||
                (kind == RunRewardKind.Mutation ? definition.Family != BoonFamily.Mutation :
                 kind != RunRewardKind.Technique || definition.Family != BoonFamily.Augment && definition.Family != BoonFamily.Resonance)) continue;
            var targets = Targets(graph, definition); if (targets == BoonSlotMask.None) continue;
            int complement = definition.RequiredBoonIds.Count > 0 || graph.Acquisitions.Any(a =>
                (a.Slots & targets) != 0 && (GauntletContentAuthoring.TryMutation(a.Id, out _) ||
                 TryDefinition(a.Id, out var owned) && owned.Family == BoonFamily.Mutation)) ? 2 : 1;
            yield return new RunBoons.Offer(definition.Id, definition.Title,
                definition.Description + "\nAlvo: " + targets + ".", complement);
        }
    }
    public static IReadOnlyList<string> ComboHints(WeaponRunModifiers modifiers)
    {
        if (modifiers == null) return Array.Empty<string>();
        var result = new List<string>();
        foreach (var definition in Definitions().Where(d => d.Family == BoonFamily.Resonance && (d.Weapons & Mask(modifiers.Family)) != 0))
        {
            if (modifiers.Expansion.Has(definition.Id)) continue;
            var missing = definition.RequiredBoonIds.Where(id => !Owns(modifiers, id)).Select(Title).ToList();
            result.Add(definition.Title + (missing.Count == 0 ? " · pré-requisitos presentes; oferta depende do sorteio e limite de Duos." : " · falta: " + string.Join(" + ", missing)));
        }
        return result.AsReadOnly();
    }
    private static bool Owns(WeaponRunModifiers modifiers, string id) => modifiers.Expansion.Has(id) ||
        id == BoonReworkCatalog.ViolentFallId && modifiers.HasViolentFall ||
        GauntletContentAuthoring.TryMutation(id, out var mutation) && modifiers.GauntletMutations.TryGet(GauntletMutationSelection.SlotOf(mutation), out var selected) && selected == mutation;
    private static string Title(string id) => TryDefinition(id, out var definition) ? definition.Title :
        id == BoonReworkCatalog.ViolentFallId ? "Queda Violenta" : id == BoonReworkCatalog.UppercutId ? "Uppercut do Eclipse" : id;
    private static BoonWeaponMask Mask(RunWeaponFamily family) => family == RunWeaponFamily.Bow ? BoonWeaponMask.Bow : family == RunWeaponFamily.Spear ? BoonWeaponMask.Spear : BoonWeaponMask.Gauntlet;
}
