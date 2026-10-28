using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Eligibility uses the actual run inventory and authored execution targets; source assets are read only.</summary>
public static class ManualRewardCatalog
{
    public static IReadOnlyList<RunBoons.Offer> Eligible(RunRewardKind kind, WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory protocols = null, int opportunity = 0)
    {
        var offers = new List<RunBoons.Offer>();
        if (modifiers == null || !weapon || modifiers.Family != WeaponRunModifiers.Identify(weapon)) return offers.AsReadOnly();
        if (kind == RunRewardKind.Technique || kind == RunRewardKind.Mutation && modifiers.Family != RunWeaponFamily.Gauntlet)
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Family == modifiers.Family &&
                    (kind == RunRewardKind.Technique ? RunBoonOfferCatalog.IsTechnique(definition.Kind) : RunBoonOfferCatalog.IsMutation(definition.Kind)) &&
                    modifiers.Rank(definition.Kind) < definition.MaxRank && RunBoonOfferCatalog.HasTarget(definition.Kind, weapon))
                    offers.Add(new RunBoons.Offer(definition.Id, definition.Title,
                        "Nível " + (modifiers.Rank(definition.Kind) + 1) + "/" + definition.MaxRank + "\n" + definition.Description));
        }
        else if (kind == RunRewardKind.Protocol)
        {
            // The ticket owns the random draw. Returning the whole retained compatible pool avoids
            // the old opportunity script silently starving late rewards after boon retirement.
            foreach (var protocol in new[] { ProtocolKind.Ignite, ProtocolKind.Frost, ProtocolKind.Conductor, ProtocolKind.Detonation })
            {
                if (protocols != null)
                {
                    if (protocols.CanAcquire(protocol)) offers.Add(ProtocolOfferPresentation.Create(protocols, protocol));
                }
                else
                {
                    string id = ProtocolCatalogValidator.IdFor(protocol);
                    string title = protocol == ProtocolKind.Ignite ? "Lâmina incandescente" : protocol == ProtocolKind.Frost ? "Toque glacial" :
                        protocol == ProtocolKind.Conductor ? "Bobina encadeada" : "Reator de sucata";
                    offers.Add(new RunBoons.Offer(id, title, "Protocol — " + protocol));
                }
            }
        }
        else if (kind == RunRewardKind.Mutation)
        {
            foreach (GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation)))
            {
                if (!RunBoonOfferCatalog.IsMutation(mutation)) continue;
                var slot = GauntletMutationSelection.SlotOf(mutation);
                if (modifiers.GauntletMutations.TryGet(slot, out _) || weapon.abilities == null || (int)slot >= weapon.abilities.Length ||
                    !(weapon.abilities[(int)slot] is BreakerGauntletAbility ability)) continue;
                var selection = new GauntletMutationSelection(); selection.Select(mutation);
                if (!GauntletExecutionSnapshot.TryBuild(ability, slot, modifiers, out _, selection)) continue;
                offers.Add(new RunBoons.Offer(GauntletContentAuthoring.ForMutation(mutation).Id,
                    GauntletContentAuthoring.MutationTitle(mutation), GauntletContentAuthoring.MutationDescription(mutation)));
            }
        }
        if (kind == RunRewardKind.Technique && modifiers.Family == RunWeaponFamily.Gauntlet)
            offers.Add(EclipseBoonCatalog.FallOffer());
        offers.AddRange(BoonExpansionCatalog.Offers(kind, modifiers, weapon, protocols));
        return BoonOfferCompatibility.Filter(offers, modifiers, weapon, protocols);
    }

    public static IReadOnlyList<RunBoons.Offer> Alternatives(RunRewardKind kind, WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory protocols)
    {
        var offers = new List<RunBoons.Offer>();
        foreach (var protocol in Eligible(RunRewardKind.Protocol, modifiers, weapon, protocols))
            offers.Add(new RunBoons.Offer(protocol.Id, "ALTERNATIVA — " + protocol.Title,
                kind + " esgotada. Esta escolha concede um Protocol preservado e não conta como Mutation.\n" + protocol.Description));
        if (offers.Count == 0) offers.Add(RunBoonOfferCatalog.ExhaustedOffer());
        return offers.AsReadOnly();
    }

    public static string InventoryKey(WeaponRunModifiers modifiers, WeaponScript weapon, int revision)
    {
        if (modifiers == null || !weapon) return "invalid:" + revision;
        var parts = new List<string> { revision.ToString(), modifiers.Family.ToString(), weapon.GetEntityId().ToString() };
        foreach (var definition in WeaponRunModifiers.Catalog.OrderBy(d => d.Id, StringComparer.Ordinal))
            parts.Add(definition.Id + "=" + modifiers.Rank(definition.Kind));
        foreach (GauntletActionSlot slot in Enum.GetValues(typeof(GauntletActionSlot)))
            parts.Add(slot + "=" + (modifiers.GauntletMutations.TryGet(slot, out var mutation) ? mutation.ToString() : "none"));
        parts.Add(BoonReworkCatalog.ViolentFallId + "=" + (modifiers.HasViolentFall ? 1 : 0));
        foreach (var acquisition in modifiers.Expansion.Acquisitions)
            parts.Add(acquisition.Id + "=" + acquisition.Rank + "@" + acquisition.Slots);
        foreach (RunRewardKind kind in new[] { RunRewardKind.Technique, RunRewardKind.Mutation, RunRewardKind.Protocol })
            parts.Add(string.Join(",", Eligible(kind, modifiers, weapon).Select(o => o.Id)));
        return string.Join("|", parts);
    }
}
