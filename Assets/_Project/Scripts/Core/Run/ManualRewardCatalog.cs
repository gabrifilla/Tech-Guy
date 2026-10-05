using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>Eligibility uses the actual run inventory and authored execution targets; source assets are read only.</summary>
public static class ManualRewardCatalog
{
    public static IReadOnlyList<RunBoons.Offer> Eligible(RunRewardKind kind, WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory protocols = null, int opportunity = 0)
    {
        var offers = new List<RunBoons.Offer>();
        if (modifiers == null || modifiers.Family != RunWeaponFamily.Gauntlet || !weapon ||
            WeaponRunModifiers.Identify(weapon) != RunWeaponFamily.Gauntlet) return offers.AsReadOnly();
        if (kind == RunRewardKind.Technique)
        {
            foreach (var definition in WeaponRunModifiers.Catalog)
                if (definition.Family == RunWeaponFamily.Gauntlet && GauntletContentAuthoring.IsSelectedTechnique(definition.Kind) &&
                    modifiers.Rank(definition.Kind) < definition.MaxRank)
                    offers.Add(new RunBoons.Offer(definition.Id, definition.Title, definition.Description));
        }
        else if (kind == RunRewardKind.Protocol)
        {
            if (protocols != null && opportunity > 0)
            {
                foreach (var selected in ProtocolRewardSelection.Select(protocols, opportunity))
                {
                    offers.Add(ProtocolOfferPresentation.Create(protocols, selected));
                }
                return offers.AsReadOnly();
            }
            offers.Add(new RunBoons.Offer("ignite", "Lâmina incandescente", "Protocol — Queimadura por 4s; cada aquisição aumenta seu dano."));
            offers.Add(new RunBoons.Offer("frost", "Toque glacial", "Protocol — Lentidão e chance de congelar; cópias aumentam a chance."));
            offers.Add(new RunBoons.Offer("conductor", "Bobina encadeada", "Protocol — Acertos saltam com 45% do dano; cópias aumentam alvos e alcance."));
            offers.Add(new RunBoons.Offer("detonation", "Reator de sucata", "Protocol — Mortes por impacto explodem; cópias aumentam dano e alcance."));
        }
        else if (kind == RunRewardKind.Mutation)
        {
            foreach (GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation)))
            {
                var slot = GauntletMutationSelection.SlotOf(mutation);
                if (modifiers.GauntletMutations.TryGet(slot, out _) ||
                    mutation == GauntletMutation.Epicenter && modifiers.Rank(WeaponBoon.ShockRing) > 0 ||
                    weapon.abilities == null || (int)slot >= weapon.abilities.Length ||
                    !(weapon.abilities[(int)slot] is BreakerGauntletAbility ability)) continue;
                var selection = new GauntletMutationSelection(); selection.Select(mutation);
                if (!GauntletExecutionSnapshot.TryBuild(ability, slot, modifiers, out _, selection)) continue;
                offers.Add(new RunBoons.Offer(GauntletContentAuthoring.ForMutation(mutation).Id,
                    GauntletContentAuthoring.MutationTitle(mutation), GauntletContentAuthoring.MutationDescription(mutation)));
            }
        }
        if (protocols != null && kind == RunRewardKind.Protocol)
            offers.RemoveAll(o => !ProtocolInventory.TryKind(o.Id, out var protocol) || !protocols.CanAcquire(protocol));
        return offers.OrderBy(o => o.Id, StringComparer.Ordinal).ToList().AsReadOnly();
    }

    public static string InventoryKey(WeaponRunModifiers modifiers, WeaponScript weapon, int revision)
    {
        if (modifiers == null || !weapon) return "invalid:" + revision;
        var parts = new List<string> { revision.ToString(), modifiers.Family.ToString(), weapon.GetEntityId().ToString() };
        foreach (var definition in WeaponRunModifiers.Catalog.OrderBy(d => d.Id, StringComparer.Ordinal))
            parts.Add(definition.Id + "=" + modifiers.Rank(definition.Kind));
        foreach (GauntletActionSlot slot in Enum.GetValues(typeof(GauntletActionSlot)))
            parts.Add(slot + "=" + (modifiers.GauntletMutations.TryGet(slot, out var mutation) ? mutation.ToString() : "none"));
        foreach (RunRewardKind kind in new[] { RunRewardKind.Technique, RunRewardKind.Mutation, RunRewardKind.Protocol })
            parts.Add(string.Join(",", Eligible(kind, modifiers, weapon).Select(o => o.Id)));
        return string.Join("|", parts);
    }
}
