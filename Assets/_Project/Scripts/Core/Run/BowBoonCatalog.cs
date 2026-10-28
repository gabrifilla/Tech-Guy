using System.Collections.Generic;

/// <summary>Executable Bow definitions. Registration and inventory ownership remain in RunBoons.</summary>
public static class BowBoonCatalog
{
    public const string HuntingBondId = "mutation_bow_hunting_bond";
    public const string JudgmentArrowId = "mutation_bow_judgment_arrow";
    public const string MineGardenId = BoonReworkCatalog.MinesId;
    public const string PredatorSealId = BoonReworkCatalog.PredatorSealId;
    public const string CuttingReturnId = "augment_bow_cutting_return";
    public const string EclipseArcherId = "duo_bow_eclipse_archer";
    public const string HuntingDetonatorId = "duo_bow_hunting_detonator";
    public const string BondDescription = "Q sela o alvo atingido por 6s. Próximas flechas priorizam esse alvo somente no cone de 50°, alcance do tiro e linha de visão. Mantém tiros/cadência do snapshot de Q, mana e recarga; perde a dispersão do Q. Não atravessa paredes nem concede controle sobre boss.";
    public const string JudgmentDescription = "Segure R para concentrar. Soltar antes de 0,8s: 60% do dano nominal de R em corredor estreito, sem perfuração própria. A partir de 0,8s: 100%, flecha larga perfurante e +25% ao consumir selo válido. Auto-solta em 1,6s; recuperação 0,2s. Parado ao carregar; dash ou HP recebido interrompem sem reembolso. Mantém mana/recarga do slot e técnicas compatíveis.";
    public const string GardenDescription = "E prepara até 3 minas por 6s na área apontada, respeitando paredes. Arma em 0,25s; inimigo a 1,25m detona a mina uma vez, explosão de 2,5m com 1/3 do dano nominal de E. Replantar remove o conjunto anterior. Perde leque/recuo; mantém mana/recarga. Derivados usam a raiz original e não plantam minas.";
    public static IEnumerable<BoonDefinition> Definitions()
    {
        yield return Mutation(HuntingBondId, "Vínculo da Caça", BoonSlotMask.Q,
            BoonCapability.Projectile | BoonCapability.Mark | BoonCapability.ApplyStatus,
            BondDescription, BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction);
        yield return Mutation(JudgmentArrowId, "Flecha do Juízo", BoonSlotMask.R,
            BoonCapability.Projectile | BoonCapability.PiercingLine | BoonCapability.Channel | BoonCapability.ConsumeStatus | BoonCapability.ApplyStatus,
            JudgmentDescription, BoonPresentationCriteria.ChangesInput | BoonPresentationCriteria.ChangesTimingOrRisk);
        yield return Mutation(MineGardenId, "Jardim de Minas", BoonSlotMask.E,
            BoonCapability.PersistentZone | BoonCapability.AoE | BoonCapability.ApplyStatus,
            GardenDescription, BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.ChangesTimingOrRisk, 3);
        yield return new BoonDefinition(PredatorSealId, BoonFamily.Augment, BoonWeaponMask.Bow, BoonSlotMask.E,
            requiredCapabilities: BoonCapability.PersistentZone, providedCapabilities: BoonCapability.Mark,
            requiredBoonIds: new[] { MineGardenId }, enabledForOffers: true, title: "Selo do Predador",
            description: "Flechas nativas confirmadas marcam por 3s. Marcados acionam minas a até 2,5m; consome o selo ao acionar. Flecha do Juízo completa também pode consumir esse selo. Explosões, retornos e técnicas derivadas não marcam.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesSkillOrder | BoonPresentationCriteria.AddsConditionalInteraction);
        yield return new BoonDefinition(CuttingReturnId, BoonFamily.Augment, BoonWeaponMask.Bow, BoonSlotMask.R,
            requiredCapabilities: BoonCapability.Projectile | BoonCapability.PiercingLine,
            providedCapabilities: BoonCapability.ReturnPath, requiredBoonIds: new[] { JudgmentArrowId },
            enabledForOffers: true, title: "Retorno Cortante",
            description: "Flecha do Juízo completa retorna uma vez ao fim do alcance pela rota percorrida, a 40% do dano. Sólidos encerram o tiro. Retorno é derivado da mesma raiz, não consome nem produz marcas, e não cria outro retorno.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.ChangesTimingOrRisk,
            impactRules: new BoonImpactRules(maxDerivedImpactsPerCast: 32, maxDepth: 4));
        yield return new BoonDefinition(EclipseArcherId, BoonFamily.Resonance, BoonWeaponMask.Bow, BoonSlotMask.R,
            requiredBoonIds: new[] { HuntingBondId, JudgmentArrowId }, providedCapabilities: BoonCapability.ConsumeStatus,
            enabledForOffers: true, title: "Arqueiro do Eclipse",
            description: "Com Vínculo válido, a flecha completa usa corredor estreito de foco e consumo de selo +50% em vez de +25%. Concentra as frações extras de Corda tripla em um único projétil, preservando o orçamento dessas frações. Exige cone, alcance e linha de visão; sem alvo prioritário mantém o tiro largo normal.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction);
        yield return new BoonDefinition(HuntingDetonatorId, BoonFamily.Resonance, BoonWeaponMask.Bow, BoonSlotMask.E,
            requiredCapabilities: BoonCapability.PersistentZone | BoonCapability.Mark,
            requiredBoonIds: new[] { MineGardenId, PredatorSealId }, providedCapabilities: BoonCapability.DetonateMark,
            enabledForOffers: true, title: "Detonador da Caça",
            description: "Uma nova flecha nativa confirmada em alvo com Selo consome a marca e detona no máximo uma mina armada a até 4m, com linha de visão. A explosão não marca, não detona outra mina e mantém a raiz e o limite de impactos do plantio.",
            isTransformative: true, presentation: BoonPresentationCriteria.ChangesSkillOrder | BoonPresentationCriteria.AddsConditionalInteraction);
    }
    private static BoonDefinition Mutation(string id, string title, BoonSlotMask slot, BoonCapability capabilities,
        string description, BoonPresentationCriteria criteria, int maximum = 1)
        => new BoonDefinition(id, BoonFamily.Mutation, BoonWeaponMask.Bow, slot, providedCapabilities: capabilities,
            removedCapabilities: (BoonCapability)int.MaxValue & ~capabilities, enabledForOffers: true, title: title,
            description: description, isTransformative: true, presentation: criteria,
            enemyPolicies: new BoonEnemyPolicies(heavy: BoonEnemyPolicy.MarkFallback, elite: BoonEnemyPolicy.MarkFallback, boss: BoonEnemyPolicy.MarkFallback),
            impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Native, maxInstancesPerCast: maximum,
                maxDerivedImpactsPerCast: 32, maxDepth: 4, canCreateCausalRoot: true, canSpawnSkillEntities: true));
}
