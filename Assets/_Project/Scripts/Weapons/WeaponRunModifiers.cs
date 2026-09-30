using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunWeaponFamily { Gauntlet, Bow, Spear }
public enum WeaponBoon
{
    TwinShot, Piercing, Ricochet, Homing, HeavyBolt, RapidBurst, WideVolley, LongRain, GuidedRain, Sniper,
    LongReach, EchoThrust, TripleMoon, Trident, DragonWave, Affliction, SpearTip, Execution, Orbit, Siphon,
    LongFists, RocketAdvance, FlurryEcho, ShockRing, AsuraEcho, Momentum, AsuraReserve, StanceCrusher, ComboNova, Berserker,
    PhantomSpear, MoonShard, ReturnWave, ChainThrust,
    // impactful-weapon-boons: new playstyle-changing boons. SplitArrow/ChargedShot (Bow),
    // PerfectSpacing/ImpalingLine (Spear), MomentumStrike/Shockwave (Gauntlet). MomentumStrike is
    // named to avoid colliding with the existing Momentum (Asura energy) boon above.
    SplitArrow, ChargedShot, PerfectSpacing, ImpalingLine, MomentumStrike, Shockwave
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
        public string Id => CatalogId(Kind);
        public Definition(WeaponBoon kind, RunWeaponFamily family, string title, string description, int maxRank = 3)
        { Kind = kind; Family = family; Title = title; Description = description; MaxRank = maxRank; }
    }
    /// <summary>Stable catalog id for a boon (the same id used by offers/presentation), avoiding magic strings.</summary>
    public static string CatalogId(WeaponBoon kind) => "weapon_" + kind;
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
        // impactful-weapon-boons: Bow playstyle boons (R2/R3).
        new Definition(WeaponBoon.SplitArrow, RunWeaponFamily.Bow, "Flecha estilhaçante", "Ao matar com uma flecha, dispara +1 flecha por nível a partir do alvo, em direções distintas, com 50% do dano."),
        new Definition(WeaponBoon.ChargedShot, RunWeaponFamily.Bow, "Tiro carregado", "Segure o ataque por 0,6s para disparar uma flecha perfurante com +75% de dano por nível."),
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
        // R7 (Priority 2) transformative Spear behaviors. Applied to per-cast runtime snapshots only (see WeaponRunModifiers.Plan).
        new Definition(WeaponBoon.PhantomSpear, RunWeaponFamily.Spear, "Lança fantasma", "Estocadas repetem com uma cópia espectral após um instante.", 1),
        new Definition(WeaponBoon.MoonShard, RunWeaponFamily.Spear, "Lua partida", "Varreduras lançam projéteis a partir das extremidades."),
        new Definition(WeaponBoon.ReturnWave, RunWeaponFamily.Spear, "Retorno de pacote", "Ondas retornam ao jogador ao atingir o alcance.", 1),
        new Definition(WeaponBoon.ChainThrust, RunWeaponFamily.Spear, "Encadeamento", "Estocadas encadeiam uma estocada curta a um inimigo próximo.", 1),
        // impactful-weapon-boons: Spear playstyle boons (R4/R5).
        new Definition(WeaponBoon.PerfectSpacing, RunWeaponFamily.Spear, "Espaçamento perfeito", "Acertos diretos entre 3,5m e 6,5m do jogador: +35% de dano por nível."),
        new Definition(WeaponBoon.ImpalingLine, RunWeaponFamily.Spear, "Linha empalada", "A estocada atinge todos os inimigos na linha e os puxa para o jogador. Alcance da puxada cresce por nível."),
        new Definition(WeaponBoon.LongFists, RunWeaponFamily.Gauntlet, "Punhos titânicos", "Básicos e habilidades: +25% de alcance, largura e raio por nível."),
        new Definition(WeaponBoon.RocketAdvance, RunWeaponFamily.Gauntlet, "Propulsor de combate", "Q: +70% de avanço por nível; respeita os limites navegáveis."),
        new Definition(WeaponBoon.FlurryEcho, RunWeaponFamily.Gauntlet, "Mil punhos", "W: repete o último golpe +2 vezes por nível, a 55% do dano e postura."),
        new Definition(WeaponBoon.ShockRing, RunWeaponFamily.Gauntlet, "Epicentro", "E transforma os impactos em círculos ao redor do jogador. +20% de raio por nível."),
        new Definition(WeaponBoon.AsuraEcho, RunWeaponFamily.Gauntlet, "Asura reverberante", "R: +1 réplica do golpe final por nível, a 65% do dano e postura."),
        new Definition(WeaponBoon.Momentum, RunWeaponFamily.Gauntlet, "Dínamo de combate", "Q/W/E originais geram +10 de energia Asura por uso e por nível."),
        new Definition(WeaponBoon.AsuraReserve, RunWeaponFamily.Gauntlet, "Reserva divina", "Após consumir Asura, conserva 20 de energia por nível."),
        new Definition(WeaponBoon.StanceCrusher, RunWeaponFamily.Gauntlet, "Demolidor", "Habilidades originais: +75% de dano de postura e +30% de empurrão por nível."),
        new Definition(WeaponBoon.ComboNova, RunWeaponFamily.Gauntlet, "Terceiro impacto", "Cada terceiro básico dispara uma nova de 2,5m com 60% do dano da arma por nível."),
        new Definition(WeaponBoon.Berserker, RunWeaponFamily.Gauntlet, "Motor em pane", "Abaixo de 40% da vida: acertos diretos causam +50% de dano por nível."),
        // impactful-weapon-boons: Gauntlet playstyle boons (R6/R7). MomentumStrike is the aggression-stack
        // boon (distinct from the existing Momentum/Asura boon); Shockwave is a combo-finisher burst.
        new Definition(WeaponBoon.MomentumStrike, RunWeaponFamily.Gauntlet, "Golpe de ímpeto", "Cada acerto direto acumula uma pilha (até 10) que aumenta seu dano em 2% por nível por pilha. Sofrer dano zera as pilhas."),
        new Definition(WeaponBoon.Shockwave, RunWeaponFamily.Gauntlet, "Onda de choque", "O golpe final do combo libera uma onda esférica ao redor do jogador. Raio base de 3m, +20% por nível.")
    });
    // Order-determinism contract (Requisito 12.7 / Property 39): ranks are stored per WeaponBoon in this
    // dictionary, so Add only ever increments the rank of the acquired boon and never touches another.
    // The final rank of each boon depends solely on how many times it was acquired, NOT on the order boons
    // were acquired relative to one another. Because Plan/GauntletSteps read ranks exclusively through
    // Rank(kind) while iterating the fixed Catalog + slot (see below), any acquisition permutation that
    // yields the same rank multiset produces byte-identical snapshots. Keep ranks keyed by boon here; do
    // not introduce order-sensitive state (insertion-ordered lists, running accumulators across boons).
    private readonly Dictionary<WeaponBoon, int> _ranks = new Dictionary<WeaponBoon, int>();
    public RunWeaponFamily Family { get; }
    public WeaponRunModifiers(RunWeaponFamily family) => Family = family;
    public int Rank(WeaponBoon kind) => _ranks.TryGetValue(kind, out int rank) ? rank : 0;

    // Invalid-modifier gate (Requisito 12.8 / task 15.6 / Property 40): Add is the single entry point that
    // records a run rank, so it is also the single point that must refuse — and *log* the refusal of — a
    // modifier that is not catalogued for this family or whose resulting rank would fall outside
    // [1; MaxRank]. A refused Add is a no-op: it never touches _ranks, so Plan/GauntletSteps read the same
    // ranks as before and neither the Cast_Plan nor the source assets change because of the refused
    // modifier. The refusal log uses the stable identifier (definition.Id when known, else the WeaponBoon
    // enum name) plus the offending rank, per AGENTS.md (no magic strings). LogWarning (not LogError) keeps
    // the fallback-and-continue contract: a bad modifier is ignored, the run keeps going, and it does not
    // trip error-counting validators. Kept as a single gate to preserve the 15.1 immutability and 15.3
    // monotonic/order contracts documented on Plan/GauntletSteps below.
    public bool Add(Definition definition)
    {
        if (definition == null) { Debug.LogWarning("WeaponRunModifiers: ignored null Run_Modifier (no catalog definition; rank n/a)."); return false; }
        // Uncatalogued: a Definition whose (Kind, Family, MaxRank) does not match the shared Catalog is not a
        // real Run_Modifier for this run — ignore it and log the refused modifier + its attempted rank.
        bool cataloged = false;
        foreach (Definition entry in Catalog)
            if (entry.Kind == definition.Kind && entry.Family == definition.Family && entry.MaxRank == definition.MaxRank) { cataloged = true; break; }
        if (!cataloged)
        {
            Debug.LogWarning($"WeaponRunModifiers: ignored uncataloged Run_Modifier '{definition.Id}' (family {definition.Family}, maxRank {definition.MaxRank}); not present in Catalog.");
            return false;
        }
        // Family mismatch: a boon from another weapon family is refused and logged (rank it would have taken).
        if (definition.Family != Family)
        {
            Debug.LogWarning($"WeaponRunModifiers: ignored Run_Modifier '{definition.Id}' at rank {Rank(definition.Kind) + 1}; belongs to family {definition.Family}, run family is {Family}.");
            return false;
        }
        // Rank ceiling: the next rank would leave the catalogued [1; MaxRank] range. Refuse and log.
        int nextRank = Rank(definition.Kind) + 1;
        if (nextRank < 1 || nextRank > definition.MaxRank)
        {
            Debug.LogWarning($"WeaponRunModifiers: ignored Run_Modifier '{definition.Id}' at rank {nextRank}; outside catalogued range [1; {definition.MaxRank}].");
            return false;
        }
        _ranks[definition.Kind] = nextRank; return true;
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
        // impactful-weapon-boons R4 (Perfect Spacing): reward a mid-range band rather than "farther is
        // better". Pure and rank-keyed, so it multiplies cleanly with Sniper/SpearTip and never writes
        // back to any source asset. Non-decreasing with rank; exactly 1x outside the [3.5, 6.5]m band.
        if (distance >= 3.5f && distance <= 6.5f) result *= 1f + .35f * Rank(WeaponBoon.PerfectSpacing);
        if (targetHealthRatio < .3f) result *= 1f + .6f * Rank(WeaponBoon.Execution);
        if (playerHealthRatio < .4f) result *= 1f + .5f * Rank(WeaponBoon.Berserker);
        return result;
    }
    // Immutability contract (Requisito 12.1/12.5/12.6): Plan builds a fresh ArsenalCastPlan snapshot
    // per conjuration and mutates only that snapshot. The ArsenalCastPlan constructor copies value-type
    // fields out of the ability and keeps NO reference to it, so no branch below can write back into the
    // source ArsenalAbility/WeaponScript asset. The new reaction/displacement/grouping rules (SpearFlow,
    // Mark, GauntletLoopSteps, SoftGroupingService) all consume this same snapshot / the same Catalog
    // ranks — there is no parallel modifier catalog. Enforced by AssetIsolationTests.
    //
    // Monotonicity + order contract (Requisito 12.2/12.3/12.4/12.7; Properties 38/39): every branch below
    // reads ranks through Rank(kind) and applies each cataloged modifier as a term that STRENGTHENS the
    // affected quantity as the rank grows from 1 to MaxRank — additive counts scale with +k*Rank (Hits,
    // Arrows, sweeps, pulses, echoes), size/damage/wave with *(1 + k*Rank), k > 0, and cadence via
    // Interval /= (1 + k*Rank) (smaller interval = faster fire). EchoThrust divides per-hit Damage but the
    // named monotone quantity is the echo count (Hits += Rank), which never decreases; ShardCount is
    // Clamp(2*Rank, 0, 6) — non-decreasing then flat. Application order is fixed by the Catalog iteration
    // and the slot/Kind checks, so the produced snapshot depends only on the final rank multiset, never on
    // acquisition order. When adding a branch: make it a single expression per field keyed off Rank(kind),
    // avoid subtracting from a strengthened quantity, and do not let one field's result feed another's.
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
            if (ability.Kind == ArsenalSkillKind.Thrust)
            {
                plan.Hits += Rank(WeaponBoon.EchoThrust); plan.Damage /= 1 + .2f * Rank(WeaponBoon.EchoThrust);
                plan.PhantomDelay = Rank(WeaponBoon.PhantomSpear) > 0 ? .35f : 0f; // R7.2: within 0.2-0.5s
                plan.ChainThrust = Rank(WeaponBoon.ChainThrust) > 0;               // R7.5/R7.6
                // impactful-weapon-boons R5 (Impaling Line): pierce the whole line and pull connected
                // enemies toward the player. Non-decreasing with rank; per-cast snapshot only (R5.4).
                int impale = Rank(WeaponBoon.ImpalingLine);
                plan.ImpaleLine = impale > 0;        // R5.1
                plan.ImpalePull = .75f * impale;     // R5.2
            }
            if (ability.Kind == ArsenalSkillKind.Sweep)
                plan.ShardCount = Mathf.Clamp(2 * Rank(WeaponBoon.MoonShard), 0, 6); // R7.3: between 2 and 6
            if (slot == 1)
            {
                // TripleMoon + Orbit orbital core (R5). Applied to the fresh per-cast snapshot only (R5.5);
                // every branch reads from ranks and mutates the local plan, never the source ability/weapon asset.
                plan.Hits += 2 * Rank(WeaponBoon.TripleMoon);        // R5.1/R5.2: base sweeps + 2 per TripleMoon rank
                plan.Width *= 1 + .25f * Rank(WeaponBoon.Orbit);     // R5.1/R5.3: width x (1 + 0.25 per Orbit rank); unchanged when Orbit rank 0
                plan.Travel = Rank(WeaponBoon.Orbit) > 0;            // R5.1/R5.3: outward travel enabled only by Orbit; false when Orbit rank 0
            }
            if (slot == 2 && Rank(WeaponBoon.Trident) > 0) { plan.Directions = 3; plan.Damage *= .6f; }
            if (slot == 3) { plan.WaveMultiplier = .6f * Rank(WeaponBoon.DragonWave); plan.ReturnWave = Rank(WeaponBoon.ReturnWave) > 0; } // R7.4
        }
        return plan;
    }
    // Immutability contract (Requisito 12.1/12.5/12.6): every returned AreaHitStep is a fresh deep copy
    // of the asset's authored step (JsonUtility round-trip). AreaHitStep holds only value-type fields, so
    // the round-trip is a genuine deep clone with no shared reference back into ability.HitSteps; the
    // returned list never contains a reference into the source asset. Downstream loop rules
    // (GauntletLoopSteps.Configure) and the Flurry/Asura soft-grouping therefore mutate clones only and
    // move enemies through their own SoftGroupingService locomotion — no parallel movement channel and no
    // parallel catalog. Enforced by AssetIsolationTests + GauntletStepsCloneIsolationTests.
    //
    // Monotonicity + order contract (Requisito 12.2/12.7; Properties 38/39): stanceDamage, pushDistance and
    // the ShockRing sphereRadius all scale by *(1 + k*Rank) with k > 0, and the echo count is +k*Rank, so
    // every affected quantity is non-decreasing as the rank grows from 1 to MaxRank. Steps are cloned in
    // the ability's authored HitSteps order and echoes appended deterministically, so the result depends
    // only on the final rank multiset, not on the order boons were acquired.
    public List<AreaHitStep> GauntletSteps(BreakerGauntletAbility ability, int slot)
    {
        var steps = new List<AreaHitStep>();
        // An ability with no authored steps (a fresh/placeholder asset) has a null HitSteps; treat it as
        // an empty sequence so planning never throws (echoes below also no-op on an empty list).
        if (ability == null || ability.HitSteps == null) return steps;
        
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
        // impactful-weapon-boons R7 (Shockwave Finisher): after the combo's final resolved step (echoes
        // included), release one spherical burst centered on the player. Built as a JsonUtility deep clone
        // of the final step, so the source ability's authored HitSteps are never mutated (R7.4) and the
        // clone carries the finisher's inherited stanceDamage/pushDistance (already StanceCrusher-scaled in
        // the loop above) so the wave applies stance and outward push through the same AreaHitStep channel
        // (R7.3) — StanceCrusher/ShockRing interact with it without any special casing. Radius grows with
        // rank (R7.2, monotonic). Appended last so it lands after the final step (R7.1).
        int shock = Rank(WeaponBoon.Shockwave);
        if (shock > 0 && steps.Count > 0)
        {
            AreaHitStep finisher = steps[steps.Count - 1];
            AreaHitStep wave = JsonUtility.FromJson<AreaHitStep>(JsonUtility.ToJson(finisher));
            wave.hitShape = AreaHitShape.Sphere;
            wave.sphereRadius = 3f * (1 + .2f * shock);                   // R7.2: base 3m, +20% per rank
            wave.localOffset = new Vector3(0, 1f - wave.sphereRadius, 0); // center on the player (ShockRing pattern)
            wave.rangeOverride = .01f;
            wave.damageMultiplier *= .6f;                                 // R7.2: a fraction of the finisher's damage
            wave.delay += .08f;                                           // resolve just after the final step
            steps.Add(wave);
        }
        return steps;
    }
}

public sealed class ArsenalCastPlan
{
    public float Windup, Interval, Range, Width, Damage, WaveMultiplier;
    public int Hits, Arrows, Directions = 1;
    public bool TrackCursor, Travel;
    // Immutability invariant (Requisito 12.1/12.5): every field of this per-cast snapshot is a value type.
    // The constructor below copies from the ability by value and stores NO reference to it, so mutating a
    // plan can never reach the source asset. Keep it that way — do not add a reference-type field that
    // aliases an asset (e.g. a shared list or a UnityEngine.Object handle) without cloning it here.
    // R7 transformative spear behaviors, set from ranks in WeaponRunModifiers.Plan (per-cast snapshot only).
    public float PhantomDelay;   // R7.2: 0 = off; 0.2-0.5 when PhantomSpear active
    public int ShardCount;       // R7.3: 0 = off; clamped 2-6 when MoonShard active
    public bool ReturnWave;      // R7.4
    public bool ChainThrust;     // R7.5/R7.6
    // impactful-weapon-boons R5 (Impaling Line): value types, so the immutability invariant above holds
    // (no reference aliases an asset). Set from ImpalingLine rank in WeaponRunModifiers.Plan.
    public bool ImpaleLine;      // R5.1: thrust damages every enemy along the line
    public float ImpalePull;     // R5.2: metres each connected enemy is pulled toward the player (0 = off)
    public ArsenalCastPlan(ArsenalAbility ability)
    {
        Windup = ability.Windup; Interval = ability.Interval; Range = ability.Range;
        Width = ability.Width; Damage = ability.DamageMultiplier; Hits = ability.Hits;
        Arrows = ability.Kind == ArsenalSkillKind.Volley ? 5 : 1;
    }
}
