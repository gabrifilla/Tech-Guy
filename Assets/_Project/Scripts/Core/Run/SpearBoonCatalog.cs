using System.Collections.Generic;

/// <summary>Executable geometric spear slice. Stable identities are independent from presentation.</summary>
public static class SpearBoonCatalog
{
    public const string HorizonId = "mutation_spear_horizon_cut";
    public const string SiegeId = "mutation_spear_stake_siege";
    public const string OrbitId = "mutation_spear_dragon_orbit";
    public const string ReturnMarkId = "augment_spear_return_mark";
    public const string MoonEchoId = "augment_spear_moon_echo";
    public const string ReturnLineId = "duo_spear_return_line";
    public const string HorizonDescription = "Q: estocada de até 8m causa 60% do dano/postura nominal e deixa uma linha por 5s. E nativo cruzando a linha detona os 40% restantes uma vez. Sem avanço. Mana/recarga de Q; uma linha ativa. Ecos repetem só a estocada ofensiva; nunca recriam a linha.";
    public const string SiegeDescription = "E: após 0,35s, pulso em raio 3m causa 40% do dano/postura nominal e planta seis hastes por 6s, até 7m. Cada haste pulsa uma vez por 10% em raio 1,5m por contato inimigo ou trajeto nativo de Q/R. Não bloqueia movimento ou NavMesh. Mana/recarga de E; um conjunto ativo.";
    public const string OrbitDescription = "R: projeção até 8m percorre três voltas de 1s, raio 2,5m, e retorna à posição escolhida ao iniciar a volta em até 1s. Cada passagem e retorno causa 25% do dano/postura nominal por alvo. Finalizar sequência antecipa o retorno e perde passagens restantes. Paredes interrompem a rota; finaliza no último ponto livre. Libera ações após 0,4s+0,15s; mana/recarga de R, uma projeção ativa.";
    public static IEnumerable<BoonDefinition> Definitions()
    {
        const BoonCapability status = BoonCapability.ApplyStatus;
        yield return Mutation(HorizonId, "Corte do Horizonte", BoonSlotMask.Q,
            BoonCapability.MeleeHit | BoonCapability.PiercingLine | BoonCapability.PersistentZone |
            BoonCapability.Mark | BoonCapability.DetonateMark | status, HorizonDescription);
        yield return Mutation(SiegeId, "Cerco de Hastes", BoonSlotMask.E,
            BoonCapability.PersistentZone | BoonCapability.AoE | BoonCapability.Mark | BoonCapability.DetonateMark | status, SiegeDescription, 6);
        yield return Mutation(OrbitId, "Órbita do Dragão", BoonSlotMask.R,
            BoonCapability.ReturningWeapon | BoonCapability.ReturnPath | BoonCapability.AoE | BoonCapability.Finisher | status, OrbitDescription);
        yield return new BoonDefinition(ReturnMarkId, BoonFamily.Augment, BoonWeaponMask.Spear, BoonSlotMask.R,
            requiredCapabilities: BoonCapability.ReturnPath, providedCapabilities: BoonCapability.Mark | BoonCapability.DetonateMark,
            requiredBoonIds: new[] { OrbitId }, enabledForOffers: true, title: "Marca de Retorno",
            description: "Órbita: confirmar HP na ida e na volta contra a mesma geração detona uma vez por alvo por R: 20% do dano/postura nominal, derivado. Duplicar collider ou dar mais voltas não renova a marca.",
            isTransformative: true, presentation: BoonPresentationCriteria.AddsConditionalInteraction | BoonPresentationCriteria.ChangesTimingOrRisk,
            impactRules: Derived());
        yield return new BoonDefinition(MoonEchoId, BoonFamily.Augment, BoonWeaponMask.Spear, BoonSlotMask.R,
            requiredCapabilities: BoonCapability.Finisher, providedCapabilities: BoonCapability.Echo,
            requiredBoonIds: new[] { OrbitId }, enabledForOffers: true, title: "Eco da Lua",
            description: "O final do retorno da Órbita repete um único corte em raio 1,5m por 12,5% do dano/postura nominal. É eco derivado; não cria órbita, input, mana ou energia.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction,
            impactRules: Derived(BoonProducedImpact.Echo));
        yield return new BoonDefinition(ReturnLineId, BoonFamily.Resonance, BoonWeaponMask.Spear, BoonSlotMask.R,
            requiredCapabilities: BoonCapability.ReturnPath | BoonCapability.DetonateMark,
            providedCapabilities: BoonCapability.ConsumeStatus,
            requiredBoonIds: new[] { OrbitId, ReturnMarkId }, enabledForOffers: true, title: "Linha de Retorno",
            description: "Órbita + Marca de Retorno: a primeira confirmação válida de ida e volta por R consome a linha e troca sua detonação de 20% por 50% do dano/postura nominal. Outros alvos conservam a Marca normal. Um consumo por R; sempre no orçamento derivado original.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesSkillOrder | BoonPresentationCriteria.AddsConditionalInteraction,
            impactRules: Derived());
    }
    private static BoonDefinition Mutation(string id, string title, BoonSlotMask slot, BoonCapability capabilities, string description, int instances = 1) =>
        new BoonDefinition(id, BoonFamily.Mutation, BoonWeaponMask.Spear, slot, providedCapabilities: capabilities,
            removedCapabilities: (BoonCapability)int.MaxValue & ~capabilities, enabledForOffers: true,
            title: title, description: description, isTransformative: true,
            presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction |
                BoonPresentationCriteria.ChangesTimingOrRisk,
            enemyPolicies: new BoonEnemyPolicies(heavy: BoonEnemyPolicy.PostureFallback, elite: BoonEnemyPolicy.PostureFallback, boss: BoonEnemyPolicy.PostureFallback),
            impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Native, maxInstancesPerCast: instances,
                maxDerivedImpactsPerCast: 32, maxDepth: 4, canCreateCausalRoot: true));
    private static BoonImpactRules Derived(BoonProducedImpact produced = BoonProducedImpact.Derived) =>
        new BoonImpactRules(producedImpact: produced, maxDerivedImpactsPerCast: 32, maxDepth: 4);
}
