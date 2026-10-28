using System.Collections.Generic;

/// <summary>The two executable additions in Phase C. All other candidates remain disabled.</summary>
public static class EclipseBoonCatalog
{
    public const string UppercutDescription = "Substitui Q — Avanço Relâmpago por uppercut frontal de 2,4m, sem avanço. Mantém dano/postura total, mana e recarga de Q. Launch só na quebra autorizada; caso contrário aplica marca de ascensão por 3s. Permite extensão por finalizadores nativos e Queda Violenta. Não herda o gap closer.";
    public const string FallDescription = "Augment de Q — Uppercut do Eclipse. Um finalizador nativo de outra execução consome a marca. No alvo lançado, arma uma onda na aterrissagem natural; no resistente, gera impacto/postura alternativo. Raio 2,5m, 35% do roll de HP/postura do finalizador. Um consumo por marca; echoes, ticks, pouso e timeout não ativam. Sem custo ou recarga adicional. Nesta combinação, ecos mantêm dano/postura e suas quebras usam Stun permitido pelo perfil, sem renovar Launch.";
    public static IEnumerable<BoonDefinition> Definitions()
    { yield return Uppercut(); yield return ViolentFall(); }
    public static BoonDefinition Uppercut() => new BoonDefinition(BoonReworkCatalog.UppercutId,
        BoonFamily.Mutation, BoonWeaponMask.Gauntlet, BoonSlotMask.Q,
        providedCapabilities: BoonCapability.MeleeHit | BoonCapability.Launch | BoonCapability.AscensionMark |
            BoonCapability.AirFollowup | BoonCapability.ApplyStatus,
        removedCapabilities: (BoonCapability)int.MaxValue & ~(BoonCapability.MeleeHit | BoonCapability.Launch |
            BoonCapability.AscensionMark | BoonCapability.AirFollowup | BoonCapability.ApplyStatus),
        enabledForOffers: true, title: "Uppercut do Eclipse", description: UppercutDescription,
        isTransformative: true, presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.ChangesTimingOrRisk,
        enemyPolicies: new BoonEnemyPolicies(heavy: BoonEnemyPolicy.MarkFallback, elite: BoonEnemyPolicy.MarkFallback, boss: BoonEnemyPolicy.MarkFallback),
        impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Native, maxDerivedImpactsPerCast: 32,
            maxDepth: 4, canGenerateEnergy: true, canCreateCausalRoot: true));
    public static BoonDefinition ViolentFall() => new BoonDefinition(BoonReworkCatalog.ViolentFallId,
        BoonFamily.Augment, BoonWeaponMask.Gauntlet, BoonSlotMask.Q,
        providedCapabilities: BoonCapability.AoE | BoonCapability.ConsumeStatus,
        requiredBoonIds: new[] { BoonReworkCatalog.UppercutId }, enabledForOffers: true,
        title: "Queda Violenta", description: FallDescription, isTransformative: true,
        presentation: BoonPresentationCriteria.AddsConditionalInteraction | BoonPresentationCriteria.ChangesTimingOrRisk,
        enemyPolicies: new BoonEnemyPolicies(heavy: BoonEnemyPolicy.PostureFallback, elite: BoonEnemyPolicy.PostureFallback, boss: BoonEnemyPolicy.PostureFallback),
        impactRules: new BoonImpactRules(maxInstancesPerCast: 32, maxDerivedImpactsPerCast: 32, maxDepth: 4),
        anyRequiredCapabilities: BoonCapability.Launch | BoonCapability.AscensionMark);
    public static RunBoons.Offer FallOffer() => new RunBoons.Offer(BoonReworkCatalog.ViolentFallId, "Queda Violenta", FallDescription);
}
