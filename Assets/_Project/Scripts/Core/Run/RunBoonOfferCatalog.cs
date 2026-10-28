/// <summary>The explicitly retained offers. Retired IDs and execution assets stay readable for existing data.</summary>
public static class RunBoonOfferCatalog
{
    public const string ExhaustedId = "run_reward_exhausted";

    public static bool Includes(ProtocolKind kind) => kind == ProtocolKind.Ignite || kind == ProtocolKind.Frost ||
        kind == ProtocolKind.Conductor || kind == ProtocolKind.Detonation;

    public static bool IsTechnique(WeaponBoon kind) => kind == WeaponBoon.FlurryEcho || kind == WeaponBoon.AsuraEcho ||
        kind == WeaponBoon.Ricochet || kind == WeaponBoon.EchoThrust;

    // These existing sequence/chain transformations are reserved for Mutation, so Techniques cannot
    // exhaust a new weapon's mandatory first transformation before the guaranteed node.
    public static bool IsMutation(WeaponBoon kind) => kind == WeaponBoon.RapidBurst ||
        kind == WeaponBoon.PhantomSpear || kind == WeaponBoon.ChainThrust;

    public static bool IsMutation(GauntletMutation kind) => kind == GauntletMutation.CadencedAsura || kind == GauntletMutation.EclipseUppercut;

    public static bool IncludesOffer(string id)
    {
        if (id == BoonReworkCatalog.ViolentFallId) return true;
        if (BoonExpansionCatalog.TryDefinition(id, out var expansion)) return expansion.EnabledForOffers;
        if (ProtocolInventory.TryKind(id, out var protocol)) return Includes(protocol);
        if (GauntletContentAuthoring.TryMutation(id, out var mutation)) return IsMutation(mutation);
        return WeaponRunModifiers.TryDefinition(id, out var definition) &&
            (IsTechnique(definition.Kind) || IsMutation(definition.Kind));
    }

    public static bool HasTarget(WeaponBoon kind, WeaponScript weapon)
    {
        if (!weapon || weapon.abilities == null || weapon.abilities.Length != 4) return false;
        switch (kind)
        {
            case WeaponBoon.FlurryEcho: return weapon.abilities[1] is BreakerGauntletAbility;
            case WeaponBoon.AsuraEcho: return weapon.abilities[3] is BreakerGauntletAbility;
            case WeaponBoon.RapidBurst:
                return weapon.abilities[0] is ArsenalAbility rapid && rapid.Kind == ArsenalSkillKind.Arrow;
            case WeaponBoon.EchoThrust:
            case WeaponBoon.PhantomSpear:
            case WeaponBoon.ChainThrust:
                foreach (var ability in weapon.abilities)
                    if (ability is ArsenalAbility thrust && thrust.Kind == ArsenalSkillKind.Thrust) return true;
                return false;
            case WeaponBoon.Ricochet: return weapon.FiresArrows;
            default: return false;
        }
    }

    public static RunBoons.Offer ExhaustedOffer() => new RunBoons.Offer(ExhaustedId,
        "CONTINUAR — CATÁLOGO ESGOTADO", "Todas as melhorias compatíveis desta categoria e os Protocols preservados atingiram o limite. Continue sem adquirir uma melhoria; esta alternativa não conta como Mutation.");
}
