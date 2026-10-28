using System.Collections.Generic;

/// <summary>
/// Phase A choices as authorable data. Draft definitions describe future behavior and remain disabled.
/// This factory supplies no executors, damage values, timings, or overrides of the current slot costs.
/// </summary>
public static class BoonReworkCatalog
{
    public const string UppercutId = "mutation_gauntlet_eclipse_uppercut";
    public const string ForestId = "mutation_bow_thorn_forest";
    public const string MinesId = "mutation_bow_mine_garden";
    public const string ViolentFallId = "augment_gauntlet_violent_fall";
    public const string InvertedHarvestId = "augment_bow_inverted_harvest";
    public const string PredatorSealId = "augment_bow_predator_seal";

    private const BoonCapability AllCapabilities = (BoonCapability)int.MaxValue;
    private const BoonPresentationCriteria GeometryAndRisk =
        BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.ChangesTimingOrRisk;
    private const BoonPresentationCriteria InputAndRisk =
        BoonPresentationCriteria.ChangesInput | BoonPresentationCriteria.ChangesTimingOrRisk;
    private const BoonPresentationCriteria GeometryAndInteraction =
        BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction;
    private const BoonCapability StatusHit = BoonCapability.ApplyStatus;

    public static IEnumerable<BoonDefinition> CreateDefinitions()
    {
        var actual = new Dictionary<string, BoonDefinition>(System.StringComparer.Ordinal);
        foreach (var definition in DraftDefinitions()) actual[definition.Id] = definition;
        foreach (var definition in BoonExpansionCatalog.Definitions()) actual[definition.Id] = definition;
        var ids = new List<string>(actual.Keys); ids.Sort(System.StringComparer.Ordinal);
        foreach (var id in ids) yield return actual[id];
    }

    private static IEnumerable<BoonDefinition> DraftDefinitions()
    {
        // Stable existing identities and their current offer gates are owned by the retained adapter.
        foreach (var definition in BoonOfferCompatibility.RetainedDefinitions()) yield return definition;

        yield return Mutation("mutation_universal_wandering_singularity", "Singularidade Errante",
            BoonWeaponMask.Universal, BoonSlotMask.W,
            BoonCapability.Projectile | BoonCapability.PersistentZone | BoonCapability.PullEnemy | BoonCapability.AoE | StatusHit,
            GeometryAndRisk, "Lança uma zona móvel; libera as outras ações e atrai somente inimigos autorizados pelo perfil.",
            bossPolicy: BoonEnemyPolicy.PressureFallback);
        yield return Mutation("mutation_universal_rupture_orb", "Orbe de Ruptura",
            BoonWeaponMask.Universal, BoonSlotMask.W,
            BoonCapability.PersistentZone | BoonCapability.Mark | BoonCapability.DetonateMark | BoonCapability.AoE | StatusHit,
            GeometryAndInteraction, "Instala orbe estático; skills confirmadas constroem marcas para a detonação.");
        yield return Mutation("mutation_universal_repulsion_ring", "Anel de Repulsão",
            BoonWeaponMask.Universal, BoonSlotMask.W,
            BoonCapability.AoE | BoonCapability.GuardCounter | BoonCapability.BreakPosture | StatusHit,
            GeometryAndInteraction, "Abre espaço com repulsão autorizada e uma janela de contra-ataque.",
            bossPolicy: BoonEnemyPolicy.PostureFallback);

        yield return Mutation("mutation_gauntlet_rupture_hook", "Gancho de Ruptura",
            BoonWeaponMask.Gauntlet, BoonSlotMask.Q,
            BoonCapability.MeleeHit | BoonCapability.GapClose | BoonCapability.Mark | BoonCapability.DetonateMark | StatusHit,
            GeometryAndInteraction, "Avança até alvo acessível e deixa marca que um próximo golpe pode ativar.");
        yield return EclipseBoonCatalog.Uppercut();
        yield return Mutation("mutation_gauntlet_pugilist_step", "Passo do Pugilista",
            BoonWeaponMask.Gauntlet, BoonSlotMask.Q,
            BoonCapability.Dash | BoonCapability.SelfReposition | BoonCapability.GuardCounter | BoonCapability.OnPerfectTiming | BoonCapability.MeleeHit | StatusHit,
            InputAndRisk, "Avanço curto com esquiva direcional precisa; confirmação real habilita o contra-gancho.");
        yield return Mutation("mutation_gauntlet_mass_press", "Prensa de Massa",
            BoonWeaponMask.Gauntlet, BoonSlotMask.E,
            BoonCapability.MeleeHit | BoonCapability.PiercingLine | BoonCapability.AoE | BoonCapability.BreakPosture | StatusHit,
            GeometryAndRisk, "Dois socos originam ondas convergentes; mira define a região de fechamento.");
        yield return Mutation("mutation_gauntlet_two_beat_quake", "Sismo de Dois Tempos",
            BoonWeaponMask.Gauntlet, BoonSlotMask.E,
            BoonCapability.MeleeHit | BoonCapability.PiercingLine | BoonCapability.MultiPress | BoonCapability.OnPerfectTiming | BoonCapability.BreakPosture | StatusHit,
            InputAndRisk, "Um segundo pressionamento no ritmo correto detona a linha perpendicular; hold não continua.");
        yield return Mutation("mutation_gauntlet_counter_impact", "Contra-Impacto",
            BoonWeaponMask.Gauntlet, BoonSlotMask.E,
            BoonCapability.MeleeHit | BoonCapability.GuardCounter | BoonCapability.AoE | BoonCapability.BreakPosture | StatusHit,
            InputAndRisk, "Receber golpe bloqueável durante a postura habilita resposta; golpes não bloqueáveis mantêm seu contrato.");
        yield return Mutation("mutation_gauntlet_asura_avatar", "Avatar de Asura",
            BoonWeaponMask.Gauntlet, BoonSlotMask.R,
            BoonCapability.Summon | BoonCapability.MeleeHit | BoonCapability.Finisher | StatusHit,
            GeometryAndRisk, "Projeta execução fixa remota; composição futura com input cadenciado exige adaptação explícita de suporte.");
        // CadencedAsura already has a stable retained identity and occupies R; do not duplicate it here.
        yield return Mutation("mutation_gauntlet_mirrored_asura", "Asura Espelhado",
            BoonWeaponMask.Gauntlet, BoonSlotMask.R,
            BoonCapability.Summon | BoonCapability.Echo | BoonCapability.MeleeHit | BoonCapability.Finisher,
            GeometryAndInteraction, "Avatar repete somente hits confirmados elegíveis; não replica custo, energia ou outro avatar.");

        yield return Mutation("mutation_bow_phantom_passage", "Passagem Fantasma",
            BoonWeaponMask.Bow, BoonSlotMask.Q,
            BoonCapability.Dash | BoonCapability.SelfReposition | BoonCapability.Projectile | BoonCapability.Summon | StatusHit,
            GeometryAndRisk, "Reposiciona o arqueiro e deixa uma origem ilusória de salva curta, sem aggro ou clones recursivos.");
        yield return Mutation("mutation_bow_hunting_bond", "Vínculo da Caça",
            BoonWeaponMask.Bow, BoonSlotMask.Q,
            BoonCapability.Projectile | BoonCapability.Mark | StatusHit,
            GeometryAndInteraction, "Sela alvo prioritário; próximos tiros só o buscam dentro de cone, alcance e linha de visão.");
        yield return Mutation("mutation_bow_prismatic_arrow", "Flecha Prismática",
            BoonWeaponMask.Bow, BoonSlotMask.Q,
            BoonCapability.Projectile | BoonCapability.Cone | StatusHit,
            GeometryAndInteraction, "Impacto em inimigo ou superfície origina fragmentação lateral limitada.");
        yield return Mutation(MinesId, "Jardim de Minas",
            BoonWeaponMask.Bow, BoonSlotMask.E,
            BoonCapability.PersistentZone | BoonCapability.AoE | StatusHit,
            GeometryAndRisk, "Prepara minas como armadilha territorial; cada mina é consumida uma vez.", maxInstancesPerCast: 3);
        yield return Mutation(ForestId, "Floresta de Espinhos",
            BoonWeaponMask.Bow, BoonSlotMask.E,
            BoonCapability.Projectile | BoonCapability.PersistentZone | BoonCapability.PiercingLine | StatusHit,
            GeometryAndInteraction, "Estacas delimitam corredor; flechas elegíveis que o cruzam produzem fragmentos laterais.");
        yield return Mutation("mutation_bow_predator_mark", "Marca do Predador",
            BoonWeaponMask.Bow, BoonSlotMask.E,
            BoonCapability.Mark | BoonCapability.Projectile | BoonCapability.AoE | StatusHit,
            GeometryAndInteraction, "Distribui marcas; próximo impacto elegível divide seu orçamento entre alvos marcados.");
        yield return Mutation("mutation_bow_hunting_constellation", "Constelação de Caça",
            BoonWeaponMask.Bow, BoonSlotMask.R,
            BoonCapability.Projectile | BoonCapability.Summon | StatusHit,
            GeometryAndRisk, "Origens de arco etéreas alternam salvas e podem convergir em alvo marcado.", maxInstancesPerCast: 3);
        yield return Mutation("mutation_bow_judgment_arrow", "Flecha do Juízo",
            BoonWeaponMask.Bow, BoonSlotMask.R,
            BoonCapability.Projectile | BoonCapability.PiercingLine | BoonCapability.Channel | BoonCapability.ConsumeStatus | StatusHit,
            InputAndRisk, "Soltura curta e carga completa mudam corredor e risco; carga pode consumir marcas elegíveis.");
        yield return Mutation("mutation_bow_hunting_storm", "Tempestade de Caça",
            BoonWeaponMask.Bow, BoonSlotMask.R,
            BoonCapability.Projectile | BoonCapability.PiercingLine | BoonCapability.AoE | StatusHit,
            BoonPresentationCriteria.ChangesInput | BoonPresentationCriteria.ChangesGeometryOrOrigin,
            "Arrastar e soltar a mira desenha a linha das salvas, em vez de um círculo fixo.");

        yield return Mutation("mutation_spear_dragon_anchor", "Âncora do Dragão",
            BoonWeaponMask.Spear, BoonSlotMask.Q,
            BoonCapability.Projectile | BoonCapability.ReturningWeapon | BoonCapability.MultiPress | BoonCapability.GapClose | StatusHit,
            InputAndRisk, "Arremessa âncora e reserva segundo pressionamento para avanço; recolhimento é suporte futuro.");
        yield return Mutation("mutation_spear_horizon_cut", "Corte do Horizonte",
            BoonWeaponMask.Spear, BoonSlotMask.Q,
            BoonCapability.MeleeHit | BoonCapability.PiercingLine | BoonCapability.PersistentZone | BoonCapability.Mark | BoonCapability.DetonateMark | StatusHit,
            GeometryAndInteraction, "Estocada deixa trilha que próximo E elegível pode cruzar e detonar.");
        yield return Mutation("mutation_spear_moon_step", "Passo da Lua",
            BoonWeaponMask.Spear, BoonSlotMask.Q,
            BoonCapability.MeleeHit | BoonCapability.SelfReposition | BoonCapability.MultiPress | BoonCapability.PiercingLine | StatusHit,
            InputAndRisk, "Recua e reserva próximo Q no prazo para estocada de retorno.");
        yield return Mutation("mutation_spear_stake_siege", "Cerco de Hastes",
            BoonWeaponMask.Spear, BoonSlotMask.E,
            BoonCapability.PersistentZone | BoonCapability.AoE | BoonCapability.Mark | BoonCapability.DetonateMark | StatusHit,
            GeometryAndInteraction, "Hastes sem colisão física pulsam quando Q ou R elegível cruza sua região.");
        yield return Mutation("mutation_spear_double_crescent", "Crescente Duplo",
            BoonWeaponMask.Spear, BoonSlotMask.E,
            BoonCapability.MeleeHit | BoonCapability.MultiPress | BoonCapability.BreakPosture | StatusHit,
            InputAndRisk, "Alterna cortes horizontal e vertical; confirmar o primeiro habilita ruptura no segundo.");
        yield return Mutation("mutation_spear_spiral_return", "Retorno Espiral",
            BoonWeaponMask.Spear, BoonSlotMask.E,
            BoonCapability.Projectile | BoonCapability.ReturningWeapon | BoonCapability.ReturnPath | StatusHit,
            GeometryAndRisk, "Projeta crescente de ida e volta; mover o jogador altera a rota do retorno.");
        yield return Mutation("mutation_spear_dragon_orbit", "Órbita do Dragão",
            BoonWeaponMask.Spear, BoonSlotMask.R,
            BoonCapability.ReturningWeapon | BoonCapability.ReturnPath | BoonCapability.AoE | BoonCapability.Finisher | StatusHit,
            GeometryAndRisk, "Percorre passagens orbitais e retorna por trajeto legível, respeitando sólidos.");
        yield return Mutation("mutation_spear_celestial_spear", "Lança Celestial",
            BoonWeaponMask.Spear, BoonSlotMask.R,
            BoonCapability.Summon | BoonCapability.Mark | BoonCapability.AoE | BoonCapability.Finisher | StatusHit,
            GeometryAndInteraction, "Q e E confirmados em alvos marcados guiam a queda final telegrafada.");
        yield return Mutation("mutation_spear_two_moons", "Duas Luas",
            BoonWeaponMask.Spear, BoonSlotMask.R,
            BoonCapability.MultiPress | BoonCapability.MeleeHit | BoonCapability.PiercingLine | BoonCapability.AoE | StatusHit,
            InputAndRisk, "Alterna postura de alcance e varredura, alterando a decisão de entrada sem haste permanente.");

        yield return EclipseBoonCatalog.ViolentFall();
        yield return new BoonDefinition(InvertedHarvestId, BoonFamily.Augment, BoonWeaponMask.Bow, BoonSlotMask.E,
            requiredCapabilities: BoonCapability.PersistentZone, providedCapabilities: BoonCapability.MultiPress | BoonCapability.ReturnPath,
            requiredBoonIds: new[] { ForestId }, title: "Colheita Invertida",
            description: "Segundo input distinto recolhe as estacas válidas, sem conceder a Mutation Chuva Invertida.",
            enabledForOffers: false, isTransformative: true, presentation: InputAndRisk);
        yield return new BoonDefinition(PredatorSealId, BoonFamily.Augment, BoonWeaponMask.Bow, BoonSlotMask.E,
            requiredCapabilities: BoonCapability.PersistentZone, providedCapabilities: BoonCapability.Mark,
            requiredBoonIds: new[] { MinesId }, title: "Selo do Predador",
            description: "Acerto nativo elegível marca alvo para interação com minas; explosão derivada não produz outra marca.",
            enabledForOffers: false, isTransformative: true, presentation: BoonPresentationCriteria.ChangesSkillOrder | BoonPresentationCriteria.AddsConditionalInteraction);
        yield return Resonance("duo_gauntlet_titan_fall", "Queda dos Titãs", BoonWeaponMask.Gauntlet, BoonSlotMask.Q,
            new[] { UppercutId, ViolentFallId }, BoonCapability.AoE | BoonCapability.Finisher,
            "Finisher elegível transforma queda confirmada em golpe descendente; resistentes consomem marca para alternativa limitada.",
            requiredCapabilities: BoonCapability.AscensionMark | BoonCapability.ConsumeStatus);
        yield return Resonance("duo_bow_arrow_graveyard", "Cemitério de Flechas", BoonWeaponMask.Bow, BoonSlotMask.E,
            new[] { ForestId, InvertedHarvestId }, BoonCapability.AoE,
            "Converte recolhimento de estacas em círculo de estilhaços; cada estaca é consumida uma vez.",
            requiredCapabilities: BoonCapability.PersistentZone | BoonCapability.MultiPress | BoonCapability.ReturnPath);
        yield return Resonance("duo_bow_hunting_detonator", "Detonador da Caça", BoonWeaponMask.Bow, BoonSlotMask.E,
            new[] { MinesId, PredatorSealId }, BoonCapability.DetonateMark,
            "Consome marca para detonar uma mina próxima válida; o impacto derivado não repete este Duo.",
            requiredCapabilities: BoonCapability.PersistentZone | BoonCapability.Mark);
    }

    private static BoonDefinition Mutation(string id, string title, BoonWeaponMask weapon, BoonSlotMask slot,
        BoonCapability capabilities, BoonPresentationCriteria presentation, string description,
        BoonEnemyPolicy heavyPolicy = BoonEnemyPolicy.RespectControlProfile,
        BoonEnemyPolicy elitePolicy = BoonEnemyPolicy.RespectControlProfile,
        BoonEnemyPolicy bossPolicy = BoonEnemyPolicy.RespectControlProfile, int maxInstancesPerCast = 1)
        => new BoonDefinition(id, BoonFamily.Mutation, weapon, slot,
            providedCapabilities: capabilities, removedCapabilities: AllCapabilities & ~capabilities,
            title: title, description: description, enabledForOffers: false, isTransformative: true, presentation: presentation,
            enemyPolicies: new BoonEnemyPolicies(heavy: heavyPolicy, elite: elitePolicy, boss: bossPolicy),
            impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Native,
                maxInstancesPerCast: maxInstancesPerCast, maxDerivedImpactsPerCast: 32, maxDepth: 4,
                // Energy remains conservative until the native Q/W/E confirmation policy is implemented per slice.
                canGenerateEnergy: false, canCreateCausalRoot: true,
                canSpawnSkillEntities: (capabilities & (BoonCapability.Projectile | BoonCapability.PersistentZone | BoonCapability.Summon | BoonCapability.ReturningWeapon)) != 0));

    private static BoonDefinition Resonance(string id, string title, BoonWeaponMask weapon, BoonSlotMask slots,
        string[] requiredIds, BoonCapability capabilities, string description,
        BoonCapability requiredCapabilities = BoonCapability.None)
        => new BoonDefinition(id, BoonFamily.Resonance, weapon, slots,
            requiredCapabilities: requiredCapabilities, providedCapabilities: capabilities,
            requiredBoonIds: requiredIds, title: title, description: description,
            enabledForOffers: false, isTransformative: true, presentation: GeometryAndInteraction,
            enemyPolicies: new BoonEnemyPolicies(heavy: BoonEnemyPolicy.AlternativeEffect,
                elite: BoonEnemyPolicy.AlternativeEffect, boss: BoonEnemyPolicy.AlternativeEffect));
}
