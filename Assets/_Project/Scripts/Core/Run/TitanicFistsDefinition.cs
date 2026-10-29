using UnityEngine;

/// <summary>Experimental A only. These values never tune the native punch.</summary>
public static class TitanicFistsDefinition
{
    public const string Id = "mutation_gauntlet_titan_fists";
    public const float GeometryMultiplier = 1.5f;
    public const float TravelDistance = 5.5f;
    public const float TravelSpeed = 3.5f;
    public const float Lifetime = 2f;
    public const float Radius = .65f;
    public const float Height = 1.8f;
    public const float SpawnOffset = .75f;
    public const float DamageCoefficient = .60f;
    public static readonly Color Color = new Color(.35f, .92f, 1f, .9f);
    public static BoonDefinition Create() => new BoonDefinition(Id, BoonFamily.Mutation,
        BoonWeaponMask.Gauntlet, BoonSlotMask.Basic, BoonCapability.MeleeHit, BoonCapability.AoE | BoonCapability.Projectile,
        enabledForOffers: true, title: "Punhos Titânicos",
        description: "Ataque básico — LMB padrão: executar o último golpe do combo lança um tornado de vento, mesmo sem acertar o soco. " +
            "Percorre até 5,5m a 3,5m/s; dano 60% do pacote não crítico capturado no terminal. Uma vez por alvo/geração, sem repetir vítimas nativas. " +
            "Paredes bloqueiam; sem postura, recursos ou Protocols adicionais. Mantém cliques, ordem e cadência; alinhe inimigos na trajetória.",
        isTransformative: true, presentation: BoonPresentationCriteria.ChangesGeometryOrOrigin | BoonPresentationCriteria.AddsConditionalInteraction,
        impactRules: new BoonImpactRules(producedImpact: BoonProducedImpact.Derived, maxInstancesPerCast: 1,
            maxDerivedImpactsPerCast: 32, maxDepth: 1));
}
