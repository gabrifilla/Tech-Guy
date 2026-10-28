using System.Collections.Generic;

public static class GauntletExpansionCatalog
{
    public const string EchoingFistId = "augment_gauntlet_echoing_fist";
    public const string TemporalEchoId = "augment_gauntlet_temporal_echo";
    public const string PerfectRhythmId = "augment_gauntlet_perfect_rhythm";
    public const string TitanFallId = "duo_gauntlet_titan_fall";
    public static IEnumerable<BoonDefinition> Definitions()
    {
        yield return new BoonDefinition(EchoingFistId, BoonFamily.Augment, BoonWeaponMask.Gauntlet, BoonSlotMask.W,
            BoonCapability.MeleeHit | BoonCapability.Finisher, BoonCapability.Echo, enabledForOffers: true,
            title: "Punho Ecoante", description: "W: somente o finalizador que confirmar HP cria um soco atrasado em 0,25s no mesmo lugar, a 35% de dano/postura e 75% da largura. Sem novo W, custo, controle ou Asura. Compõe com Mil punhos dentro do mesmo teto causal.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesTimingOrRisk | BoonPresentationCriteria.ChangesGeometryOrOrigin,
            impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Echo, maxDerivedImpactsPerCast: 32, maxDepth: 4));
        yield return new BoonDefinition(TemporalEchoId, BoonFamily.Augment, BoonWeaponMask.Gauntlet, BoonSlotMask.Q | BoonSlotMask.E,
            BoonCapability.MeleeHit, BoonCapability.Echo, enabledForOffers: true,
            title: "Eco Temporal", description: "Q ou E: após o último contato nativo confirmar HP, uma silhueta repete somente esse hit em 0,45s na origem capturada, a 30% de dano/postura. Não repete avanço, Launch, marcas, geração de Asura ou a skill inteira. Vincula à habilidade indicada no cartão.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesTimingOrRisk | BoonPresentationCriteria.ChangesGeometryOrOrigin,
            impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Echo, maxDerivedImpactsPerCast: 32, maxDepth: 4));
        yield return new BoonDefinition(PerfectRhythmId, BoonFamily.Augment, BoonWeaponMask.Gauntlet, BoonSlotMask.R,
            BoonCapability.MultiPress | BoonCapability.MeleeHit | BoonCapability.Finisher,
            requiredBoonIds: new[] { GauntletContentAuthoring.ForMutation(GauntletMutation.CadencedAsura).Id }, enabledForOffers: true,
            title: "Ritmo Perfeito", description: "Cadenciada: continuar R nas duas esperas entre 0,10s e 0,35s transforma o último soco em onda circular de 3m. Mantém HP/postura, custo e proteção. Fora da janela mantém o golpe normal; finalizar cedo não concede a onda.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesInput | BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction,
            impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Native, maxDerivedImpactsPerCast: 32, maxDepth: 4));
        yield return new BoonDefinition(TitanFallId, BoonFamily.Resonance, BoonWeaponMask.Gauntlet, BoonSlotMask.Q,
            BoonCapability.AscensionMark | BoonCapability.ConsumeStatus,
            requiredBoonIds: new[] { BoonReworkCatalog.UppercutId, BoonReworkCatalog.ViolentFallId }, enabledForOffers: true,
            title: "Queda dos Titãs", description: "Uppercut + Queda Violenta: um finisher que armar a queda prepara um cruzado descendente após 0,12s. Metade do dano da Queda atinge o marcado; a outra metade e toda a postura permanecem na onda ao pousar. Heavy/Elite/Boss recebem os dois momentos sem Launch virtual. Um consumo por marca; cancelamento, morte e reciclagem limpam futuros.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesTimingOrRisk | BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction,
            enemyPolicies: new BoonEnemyPolicies(boss: BoonEnemyPolicy.MarkFallback, heavy: BoonEnemyPolicy.MarkFallback, elite: BoonEnemyPolicy.MarkFallback),
            impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Derived, maxDerivedImpactsPerCast: 32, maxDepth: 4));
    }
}
