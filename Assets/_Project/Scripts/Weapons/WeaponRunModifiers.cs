using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunWeaponFamily { Gauntlet, Bow, Spear }
public enum WeaponBoon
{
    TwinShot, Piercing, Ricochet, Homing, HeavyBolt, RapidBurst, WideVolley, LongRain, GuidedRain, Sniper,
    LongReach, EchoThrust, TripleMoon, Trident, DragonWave, Affliction, SpearTip, Execution, Orbit, Siphon,
    LongFists, RocketAdvance, FlurryEcho, ShockRing, AsuraEcho, Momentum, AsuraReserve, StanceCrusher, ComboNova, Berserker
}

/// <summary>Run-owned ranks and cast snapshots; never writes to weapon/ability assets.</summary>
public sealed class WeaponRunModifiers
{
    public sealed class Definition
    {
        public readonly WeaponBoon Kind;
        public readonly RunWeaponFamily Family;
        public readonly string Title, Description;
        public readonly int MaxRank;
        public string Id => "weapon_" + Kind;
        public Definition(WeaponBoon kind, RunWeaponFamily family, string title, string description, int maxRank = 3)
        { Kind = kind; Family = family; Title = title; Description = description; MaxRank = maxRank; }
    }
    public static readonly IReadOnlyList<Definition> Catalog = Array.AsReadOnly(new[]
    {
        new Definition(WeaponBoon.TwinShot, RunWeaponFamily.Bow, "Corda tripla", "Todas as flechas: +2 projéteis por nível, com dano individual dividido por 1 + 0,5 × nível."),
        new Definition(WeaponBoon.Piercing, RunWeaponFamily.Bow, "Agulhas espectrais", "Flechas básicas e de habilidades atravessam inimigos. Cenário continua bloqueando.", 1),
        new Definition(WeaponBoon.Ricochet, RunWeaponFamily.Bow, "Flecha saltadora", "Cada flecha busca +2 inimigos após acertar, por nível. Cada salto conserva 75% do dano."),
        new Definition(WeaponBoon.Homing, RunWeaponFamily.Bow, "Olho caçador", "Flechas curvam em direção a inimigos próximos à trajetória, sem atravessar paredes.", 1),
        new Definition(WeaponBoon.HeavyBolt, RunWeaponFamily.Bow, "Balista portátil", "W: +60% de dano e +25% de preparação por nível."),
        new Definition(WeaponBoon.RapidBurst, RunWeaponFamily.Bow, "Tambor de disparos", "Q: +2 disparos e +25% de cadência entre disparos por nível."),
        new Definition(WeaponBoon.WideVolley, RunWeaponFamily.Bow, "Pavão de aço", "E: +4 flechas por nível, comprimidas num leque de até 100 graus."),
        new Definition(WeaponBoon.LongRain, RunWeaponFamily.Bow, "Monção", "R: +3 pulsos e +20% de raio por nível."),
        new Definition(WeaponBoon.GuidedRain, RunWeaponFamily.Bow, "Nuvem obediente", "R acompanha o cursor a cada pulso, respeitando o alcance.", 1),
        new Definition(WeaponBoon.Sniper, RunWeaponFamily.Bow, "Horizonte mortal", "Acertos diretos: até +60% de dano por nível aos 12m de distância do jogador."),
        new Definition(WeaponBoon.LongReach, RunWeaponFamily.Spear, "Haste impossível", "Básicos e habilidades: +30% de alcance e raio por nível."),
        new Definition(WeaponBoon.EchoThrust, RunWeaponFamily.Spear, "Estocada ecoante", "Habilidades de estocada: +1 repetição por nível; dano de cada golpe dividido por 1 + 0,2 × nível."),
        new Definition(WeaponBoon.TripleMoon, RunWeaponFamily.Spear, "Órbita das luas", "W: +2 varreduras por nível."),
        new Definition(WeaponBoon.Trident, RunWeaponFamily.Spear, "Tridente espiral", "E dispara estocadas em três direções. Cada direção causa 60% do dano.", 1),
        new Definition(WeaponBoon.DragonWave, RunWeaponFamily.Spear, "Dragão liberto", "R também lança uma onda perfurante de alcance duplo e 60% do dano por nível."),
        new Definition(WeaponBoon.Affliction, RunWeaponFamily.Spear, "Ponta contaminada", "Acertos diretos: +25% de dano por fogo/gelo presente no inimigo, por nível."),
        new Definition(WeaponBoon.SpearTip, RunWeaponFamily.Spear, "Distância perfeita", "Acertos diretos a 3m ou mais: +45% de dano por nível."),
        new Definition(WeaponBoon.Execution, RunWeaponFamily.Spear, "Carrasco", "Acertos diretos contra inimigos abaixo de 30% de vida: +60% de dano por nível."),
        new Definition(WeaponBoon.Orbit, RunWeaponFamily.Spear, "Lua viajante", "W: +25% de raio por nível; as varreduras avançam 1,5m por pulso."),
        new Definition(WeaponBoon.Siphon, RunWeaponFamily.Spear, "Condutor vital", "Cada acerto direto que causa dano recupera 2 de mana por nível. Descargas secundárias não recuperam."),
        new Definition(WeaponBoon.LongFists, RunWeaponFamily.Gauntlet, "Punhos titânicos", "Básicos e habilidades: +25% de alcance, largura e raio por nível."),
        new Definition(WeaponBoon.RocketAdvance, RunWeaponFamily.Gauntlet, "Propulsor de combate", "Q: +70% de avanço por nível; respeita os limites navegáveis."),
        new Definition(WeaponBoon.FlurryEcho, RunWeaponFamily.Gauntlet, "Mil punhos", "W: repete o último golpe +2 vezes por nível, a 55% do dano e postura."),
        new Definition(WeaponBoon.ShockRing, RunWeaponFamily.Gauntlet, "Epicentro", "E transforma os impactos em círculos ao redor do jogador. +20% de raio por nível."),
        new Definition(WeaponBoon.AsuraEcho, RunWeaponFamily.Gauntlet, "Asura reverberante", "R: +1 réplica do golpe final por nível, a 65% do dano e postura."),
        new Definition(WeaponBoon.Momentum, RunWeaponFamily.Gauntlet, "Dínamo de combate", "Q/W/E originais geram +10 de energia Asura por uso e por nível."),
        new Definition(WeaponBoon.AsuraReserve, RunWeaponFamily.Gauntlet, "Reserva divina", "Após consumir Asura, conserva 20 de energia por nível."),
        new Definition(WeaponBoon.StanceCrusher, RunWeaponFamily.Gauntlet, "Demolidor", "Habilidades originais: +75% de dano de postura e +30% de empurrão por nível."),
        new Definition(WeaponBoon.ComboNova, RunWeaponFamily.Gauntlet, "Terceiro impacto", "Cada terceiro básico dispara uma nova de 2,5m com 60% do dano da arma por nível."),
        new Definition(WeaponBoon.Berserker, RunWeaponFamily.Gauntlet, "Motor em pane", "Abaixo de 40% da vida: acertos diretos causam +50% de dano por nível.")
    });
    private readonly Dictionary<WeaponBoon, int> _ranks = new Dictionary<WeaponBoon, int>();
    public RunWeaponFamily Family { get; }
    public WeaponRunModifiers(RunWeaponFamily family) => Family = family;
    public int Rank(WeaponBoon kind) => _ranks.TryGetValue(kind, out int rank) ? rank : 0;
    public bool Add(Definition definition)
    {
        if (definition == null || definition.Family != Family || Rank(definition.Kind) >= definition.MaxRank) return false;
        _ranks[definition.Kind] = Rank(definition.Kind) + 1; return true;
    }
    public static RunWeaponFamily Identify(WeaponScript weapon)
    {
        if (weapon.FiresArrows) return RunWeaponFamily.Bow;
        foreach (Ability skill in weapon.abilities) if (skill is BreakerGauntletAbility) return RunWeaponFamily.Gauntlet;
        return RunWeaponFamily.Spear;
    }
    public float MeleeScale => 1f + .3f * Rank(WeaponBoon.LongReach) + .25f * Rank(WeaponBoon.LongFists);
    public float DirectDamageMultiplier(float distance, float targetHealthRatio, float playerHealthRatio, int elements)
    {
        float result = 1f + .6f * Rank(WeaponBoon.Sniper) * Mathf.Clamp01(distance / 12f);
        result *= 1f + .25f * Rank(WeaponBoon.Affliction) * elements;
        if (distance >= 3f) result *= 1f + .45f * Rank(WeaponBoon.SpearTip);
        if (targetHealthRatio < .3f) result *= 1f + .6f * Rank(WeaponBoon.Execution);
        if (playerHealthRatio < .4f) result *= 1f + .5f * Rank(WeaponBoon.Berserker);
        return result;
    }
    public ArsenalCastPlan Plan(ArsenalAbility ability, int slot)
    {
        var plan = new ArsenalCastPlan(ability);
        plan.Range *= MeleeScale; plan.Width *= MeleeScale;
        if (Family == RunWeaponFamily.Bow)
        {
            if (slot == 0) { plan.Hits += 2 * Rank(WeaponBoon.RapidBurst); plan.Interval /= 1 + .25f * Rank(WeaponBoon.RapidBurst); }
            if (slot == 1) { plan.Damage *= 1 + .6f * Rank(WeaponBoon.HeavyBolt); plan.Windup *= 1 + .25f * Rank(WeaponBoon.HeavyBolt); }
            if (slot == 2) plan.Arrows += 4 * Rank(WeaponBoon.WideVolley);
            if (slot == 3) { plan.Hits += 3 * Rank(WeaponBoon.LongRain); plan.Width *= 1 + .2f * Rank(WeaponBoon.LongRain); plan.TrackCursor = Rank(WeaponBoon.GuidedRain) > 0; }
        }
        if (Family == RunWeaponFamily.Spear)
        {
            if (ability.Kind == ArsenalSkillKind.Thrust) { plan.Hits += Rank(WeaponBoon.EchoThrust); plan.Damage /= 1 + .2f * Rank(WeaponBoon.EchoThrust); }
            if (slot == 1) { plan.Hits += 2 * Rank(WeaponBoon.TripleMoon); plan.Width *= 1 + .25f * Rank(WeaponBoon.Orbit); plan.Travel = Rank(WeaponBoon.Orbit) > 0; }
            if (slot == 2 && Rank(WeaponBoon.Trident) > 0) { plan.Directions = 3; plan.Damage *= .6f; }
            if (slot == 3) plan.WaveMultiplier = .6f * Rank(WeaponBoon.DragonWave);
        }
        return plan;
    }
    public List<AreaHitStep> GauntletSteps(BreakerGauntletAbility ability, int slot)
    {
        var steps = new List<AreaHitStep>();
        foreach (AreaHitStep source in ability.HitSteps)
        {
            if (source == null) continue;
            AreaHitStep step = JsonUtility.FromJson<AreaHitStep>(JsonUtility.ToJson(source));
            step.rangeOverride *= MeleeScale; step.boxSize *= MeleeScale; step.sphereRadius *= MeleeScale;
            step.stanceDamage *= 1 + .75f * Rank(WeaponBoon.StanceCrusher);
            step.pushDistance *= 1 + .3f * Rank(WeaponBoon.StanceCrusher);
            if (slot == 2 && Rank(WeaponBoon.ShockRing) > 0)
            {
                step.hitShape = AreaHitShape.Sphere;
                step.sphereRadius = Mathf.Max(step.sphereRadius, step.boxSize.x * .5f) * (1 + .2f * Rank(WeaponBoon.ShockRing));
                step.rangeOverride = .01f; step.localOffset = new Vector3(0, 1 - step.sphereRadius, 0);
            }
            steps.Add(step);
        }
        int echoes = slot == 1 ? 2 * Rank(WeaponBoon.FlurryEcho) : slot == 3 ? Rank(WeaponBoon.AsuraEcho) : 0;
        if (steps.Count > 0)
        {
            AreaHitStep last = steps[steps.Count - 1];
            for (int i = 0; i < echoes; i++)
            {
                AreaHitStep echo = JsonUtility.FromJson<AreaHitStep>(JsonUtility.ToJson(last));
                echo.delay += .18f * (i + 1); echo.damageMultiplier *= slot == 1 ? .55f : .65f;
                echo.stanceDamage *= slot == 1 ? .55f : .65f; steps.Add(echo);
            }
        }
        return steps;
    }
}

public sealed class ArsenalCastPlan
{
    public float Windup, Interval, Range, Width, Damage, WaveMultiplier;
    public int Hits, Arrows, Directions = 1;
    public bool TrackCursor, Travel;
    public ArsenalCastPlan(ArsenalAbility ability)
    {
        Windup = ability.Windup; Interval = ability.Interval; Range = ability.Range;
        Width = ability.Width; Damage = ability.DamageMultiplier; Hits = ability.Hits;
        Arrows = ability.Kind == ArsenalSkillKind.Volley ? 5 : 1;
    }
}
