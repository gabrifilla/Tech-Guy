using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Adapts retained execution data to the new declarative graph. It never modifies source assets,
/// grants an effect, or enables a candidate whose executor has not been implemented.
/// </summary>
public static class BoonOfferCompatibility
{
    public static IReadOnlyList<RunBoons.Offer> Filter(IEnumerable<RunBoons.Offer> offers,
        WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory protocols = null)
    {
        if (offers == null || !TryBuild(modifiers, weapon, protocols, out var graph, out _))
            return Array.Empty<RunBoons.Offer>();
        return offers.Where(offer => offer != null && (!BoonExpansionCatalog.TryDefinition(offer.Id, out var extension) ||
                BoonExpansionCatalog.HasTarget(extension, weapon)) && (!WeaponRunModifiers.TryDefinition(offer.Id, out var execution) ||
                RunBoonOfferCatalog.HasTarget(execution.Kind, weapon)) && CanAcquire(graph, offer.Id))
            .GroupBy(offer => offer.Id, StringComparer.Ordinal).Select(group => group.First())
            .OrderBy(offer => offer.Id, StringComparer.Ordinal).ToList().AsReadOnly();
    }

    public static bool TryBuild(WeaponRunModifiers modifiers, WeaponScript weapon, ProtocolInventory protocols,
        out BoonCapabilityGraph graph, out string diagnostic)
    {
        graph = null;
        diagnostic = "Missing or incompatible four-slot loadout.";
        if (modifiers == null || !weapon || weapon.abilities == null || weapon.abilities.Length != 4 ||
            modifiers.Family != WeaponRunModifiers.Identify(weapon) || weapon.abilities.Any(ability => !ability)) return false;
        var abilities = new List<BoonAbilityConfiguration>();
        for (int i = 0; i < 4; i++)
        {
            var ability = weapon.abilities[i];
            abilities.Add(new BoonAbilityConfiguration((BoonSlot)i, NativeCapabilities(ability), ability.ManaCost, ability.cooldownTime));
        }
        if (!BoonCatalogValidator.TryCreateSnapshot(ExecutableDefinitions(protocols), out var catalog, out diagnostic)) return false;
        try { graph = new BoonCapabilityGraph(catalog, WeaponMask(modifiers.Family), abilities); }
        catch (ArgumentException exception) { diagnostic = exception.Message; return false; }
        var records = new List<BoonAcquisition>();
        foreach (var definition in WeaponRunModifiers.Catalog)
        {
            if (definition.Family != modifiers.Family || !RunBoonOfferCatalog.IncludesOffer(definition.Id)) continue;
            int rank = modifiers.Rank(definition.Kind);
            if (rank > 0) records.Add(new BoonAcquisition(definition.Id, rank, Targets(graph, definition.Id)));
        }
        foreach (var slot in new[] { GauntletActionSlot.Q, GauntletActionSlot.W, GauntletActionSlot.E, GauntletActionSlot.R })
            if (modifiers.GauntletMutations.TryGet(slot, out var mutation) && RunBoonOfferCatalog.IsMutation(mutation))
                records.Add(new BoonAcquisition(GauntletContentAuthoring.ForMutation(mutation).Id, 1, (BoonSlotMask)(1 << (int)slot)));
        foreach (var acquisition in modifiers.Expansion.Acquisitions) records.Add(acquisition);
        if (modifiers.HasViolentFall) records.Add(new BoonAcquisition(BoonReworkCatalog.ViolentFallId, 1, BoonSlot.Q));
        if (protocols != null)
            foreach (var kind in RetainedProtocols)
                if (protocols.Rank(kind) > 0) records.Add(new BoonAcquisition(ProtocolCatalogValidator.IdFor(kind), protocols.Rank(kind), BoonSlotMask.Any));
        if (!graph.TryRestore(records, out diagnostic)) { graph = null; return false; }
        return true;
    }

    public static bool CanAcquire(BoonCapabilityGraph graph, string id)
    {
        if (graph == null || !RunBoonOfferCatalog.IncludesOffer(id)) return false;
        var targets = Targets(graph, id);
        return targets != BoonSlotMask.None && graph.CanAcquire(id, targets, out _);
    }

    private static readonly ProtocolKind[] RetainedProtocols =
        { ProtocolKind.Ignite, ProtocolKind.Frost, ProtocolKind.Conductor, ProtocolKind.Detonation };

    public static IEnumerable<BoonDefinition> ExecutableDefinitions(ProtocolInventory protocols = null)
    {
        foreach (var definition in RetainedDefinitions(protocols)) yield return definition;
        foreach (var definition in EclipseBoonCatalog.Definitions()) yield return definition;
        foreach (var definition in BoonExpansionCatalog.Definitions()) yield return definition;
    }

    public static IEnumerable<BoonDefinition> RetainedDefinitions(ProtocolInventory protocols = null)
    {
        foreach (var kind in RetainedProtocols)
            yield return new BoonDefinition(ProtocolCatalogValidator.IdFor(kind), BoonFamily.Protocol,
                BoonWeaponMask.Universal, BoonSlotMask.Any, maxRank: protocols?.Catalog.Get(kind).MaxRank ?? 3,
                enabledForOffers: true, impactRules: new BoonImpactRules(triggerOrigins: BoonOriginMask.Any,
                    triggerImpacts: kind == ProtocolKind.Detonation ? BoonImpactMask.Any :
                        BoonImpactMask.Native | BoonImpactMask.Derived | BoonImpactMask.Echo,
                    producedImpact: kind == ProtocolKind.Ignite ? BoonProducedImpact.StatusTick : BoonProducedImpact.Derived,
                    maxDerivedImpactsPerCast: 32, maxDepth: 4));
        foreach (var definition in WeaponRunModifiers.Catalog)
        {
            if (!RunBoonOfferCatalog.IsTechnique(definition.Kind) && !RunBoonOfferCatalog.IsMutation(definition.Kind)) continue;
            BoonSlotMask slots;
            BoonCapability required;
            BoonCapability provided;
            switch (definition.Kind)
            {
                case WeaponBoon.FlurryEcho:
                    slots = BoonSlotMask.W; required = BoonCapability.MeleeHit | BoonCapability.Finisher; provided = BoonCapability.Echo; break;
                case WeaponBoon.AsuraEcho:
                    slots = BoonSlotMask.R; required = BoonCapability.MeleeHit | BoonCapability.Finisher; provided = BoonCapability.Echo; break;
                case WeaponBoon.Ricochet:
                    slots = BoonSlotMask.Any; required = BoonCapability.Projectile; provided = BoonCapability.Chain; break;
                case WeaponBoon.RapidBurst:
                    slots = BoonSlotMask.Q; required = BoonCapability.Projectile; provided = BoonCapability.None; break;
                default:
                    slots = BoonSlotMask.Any; required = BoonCapability.MeleeHit | BoonCapability.PiercingLine;
                    provided = definition.Kind == WeaponBoon.ChainThrust ? BoonCapability.Chain : BoonCapability.Echo; break;
            }
            yield return new BoonDefinition(definition.Id,
                RunBoonOfferCatalog.IsMutation(definition.Kind) ? BoonFamily.Mutation : BoonFamily.Augment,
                WeaponMask(definition.Family), slots, required, provided, maxRank: definition.MaxRank,
                isRetainedSupport: true, enabledForOffers: true, title: definition.Title, description: definition.Description,
                impactRules: new BoonImpactRules(triggerOrigins: BoonOriginMask.Player | BoonOriginMask.Projectile,
                    maxDerivedImpactsPerCast: 32, maxDepth: 4));
        }
        var cadenced = GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura);
        yield return new BoonDefinition(cadenced.Id, BoonFamily.Mutation, BoonWeaponMask.Gauntlet, BoonSlotMask.R,
            BoonCapability.MeleeHit | BoonCapability.Finisher, BoonCapability.MultiPress,
            enabledForOffers: true, title: GauntletContentAuthoring.MutationTitle(GauntletMutation.CadencedAsura),
            presentation: BoonPresentationCriteria.ChangesInput | BoonPresentationCriteria.ChangesTimingOrRisk,
            impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Native, maxDerivedImpactsPerCast: 32,
                maxDepth: 4, canCreateCausalRoot: true));
    }

    private static BoonSlotMask Targets(BoonCapabilityGraph graph, string id)
    {
        if (BoonExpansionCatalog.TryDefinition(id, out var expansion)) return BoonExpansionCatalog.Targets(graph, expansion);
        if (id == BoonReworkCatalog.ViolentFallId) return BoonSlotMask.Q;
        if (ProtocolInventory.TryKind(id, out _)) return BoonSlotMask.Any;
        if (GauntletContentAuthoring.TryMutation(id, out var mutation))
            return (BoonSlotMask)(1 << (int)GauntletMutationSelection.SlotOf(mutation));
        if (!WeaponRunModifiers.TryDefinition(id, out var definition)) return BoonSlotMask.None;
        switch (definition.Kind)
        {
            case WeaponBoon.FlurryEcho: return BoonSlotMask.W;
            case WeaponBoon.AsuraEcho: return BoonSlotMask.R;
            case WeaponBoon.RapidBurst: return BoonSlotMask.Q;
        }
        BoonCapability required = definition.Kind == WeaponBoon.Ricochet ? BoonCapability.Projectile :
            BoonCapability.MeleeHit | BoonCapability.PiercingLine;
        var result = BoonSlotMask.None;
        foreach (BoonSlot slot in Enum.GetValues(typeof(BoonSlot)))
            if ((graph.Capabilities(slot) & required) == required) result |= (BoonSlotMask)(1 << (int)slot);
        return result;
    }

    private static BoonWeaponMask WeaponMask(RunWeaponFamily family)
    {
        switch (family)
        {
            case RunWeaponFamily.Gauntlet: return BoonWeaponMask.Gauntlet;
            case RunWeaponFamily.Bow: return BoonWeaponMask.Bow;
            case RunWeaponFamily.Spear: return BoonWeaponMask.Spear;
            default: return BoonWeaponMask.None;
        }
    }

    private static BoonCapability NativeCapabilities(Ability ability)
    {
        if (ability is BreakerGauntletAbility gauntlet)
            return BoonCapability.MeleeHit | BoonCapability.Finisher | BoonCapability.ApplyStatus |
                (gauntlet.AdvanceDistance > 0 ? BoonCapability.GapClose : BoonCapability.None) |
                (gauntlet.ShockSkill ? BoonCapability.AoE | BoonCapability.BreakPosture : BoonCapability.None);
        if (ability is ArsenalAbility arsenal)
        {
            switch (arsenal.Kind)
            {
                case ArsenalSkillKind.Arrow:
                case ArsenalSkillKind.Volley:
                    return BoonCapability.Projectile | BoonCapability.ApplyStatus |
                        (arsenal.Piercing ? BoonCapability.PiercingLine : BoonCapability.None);
                case ArsenalSkillKind.Rain:
                    return BoonCapability.AoE | BoonCapability.PersistentZone | BoonCapability.ApplyStatus;
                case ArsenalSkillKind.Thrust:
                    return BoonCapability.MeleeHit | BoonCapability.PiercingLine | BoonCapability.ApplyStatus;
                case ArsenalSkillKind.Sweep:
                    return BoonCapability.MeleeHit | BoonCapability.AoE | BoonCapability.ApplyStatus;
            }
        }
        return BoonCapability.None;
    }
}
