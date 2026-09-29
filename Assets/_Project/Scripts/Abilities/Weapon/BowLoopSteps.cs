using UnityEngine;

/// <summary>
/// The four ability identities of the Arco (Bow), one per Q/W/E/R slot (Requisito 10.4–10.7):
/// Q — Rajada Rápida (Rapid Burst, mobile multi-shot DPS); W — Flecha Pesada / Tiro Concentrado
/// (Heavy Arrow, a chargeable shot whose stance damage can break a Heavy enemy); E — Leque Amplo
/// (Wide Fan, an emergency cone that also grants a backstep); R — Chuva de Flechas (Arrow Rain,
/// territorial control clamped to the ability's existing range by the cursor). Each slot is usable on
/// its own; the enum only tags a slot so the identity invariants can be applied to a per-cast
/// snapshot.
/// </summary>
public enum BowLoopStage
{
    /// <summary>Q — Rajada Rápida: mobile multi-shot DPS (Requisito 10.4).</summary>
    RapidBurst,
    /// <summary>W — Flecha Pesada: chargeable Heavy/Breaker shot that breaks Heavy stance (Requisito 10.5 / 11.2).</summary>
    HeavyArrow,
    /// <summary>E — Leque Amplo: emergency cone plus a backstep (Requisito 10.6).</summary>
    WideFan,
    /// <summary>R — Chuva de Flechas: cursor-clamped territorial control (Requisito 10.7).</summary>
    ArrowRain,
    /// <summary>Any other slot: no Bow-specific identity is enforced.</summary>
    Generic
}

/// <summary>
/// Pure, scene-free configuration helper that pins the Arco identity onto a per-cast Bow shot
/// (Requisito 10.4/10.5/10.6/10.7). Like the Lança, the Bow's hits are built by <c>ArsenalCombat</c>
/// from an <see cref="ArsenalCastPlan"/> snapshot; where an arrow needs to affect an enemy's stance
/// (W — Flecha Pesada) the coordinator applies a <see cref="HitReactionRequest"/> along the shot line,
/// and this helper decides that reaction. It works entirely through the existing
/// <see cref="HitReactionRequest"/> / <see cref="HitStrength"/> fields — no parallel channel
/// (Requisito 4.4) — and never touches the source ability asset.
///
/// The identity invariants it guarantees, so the Bow behaves correctly regardless of how an ability
/// asset is authored:
/// <list type="bullet">
/// <item><b>Q — Rajada Rápida (Requisito 10.4):</b> mobile DPS. It is tagged <see cref="Reaction.Mobile"/>
/// so the coordinator keeps the player moving (at reduced speed via <see cref="BowFireMovement"/>)
/// instead of pinning them for the burst.</item>
/// <item><b>W — Flecha Pesada (Requisito 10.5 / 11.2):</b> lands with at least
/// <see cref="GauntletLoopSteps.AntiHeavyMinStrength"/> (Heavy) force and extra stance damage so it can
/// crack a Heavy enemy's stance, breaking into a KnockUp. Shares the exact anti-Heavy floor the
/// Manoplas E and Lança E use (Property 27, task 13.4).</item>
/// <item><b>E — Leque Amplo (Requisito 10.6):</b> tagged with a positive <see cref="Reaction.BackstepDistance"/>
/// so the coordinator grants the emergency backstep after firing the cone.</item>
/// <item><b>R — Chuva de Flechas (Requisito 10.7):</b> a pure area with no stance CC; the cursor-clamp
/// to the ability's existing range is enforced by the coordinator/plan and preserved here.</item>
/// </list>
///
/// It is kept as a plain static class (no <see cref="MonoBehaviour"/>) so <c>ArsenalCombat</c> stays
/// thin (AGENTS.md) and the identity can be property-tested without a live scene.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 12.4.
/// Requirements: 10.4, 10.5, 10.6, 10.7, 11.2.
/// </remarks>
public static class BowLoopSteps
{
    /// <summary>
    /// Minimum strength the W (Flecha Pesada) shot lands with so it can break a Heavy enemy's stance
    /// (Requisito 10.5 / 11.2). Shares the exact anti-Heavy floor the Manoplas E and Lança E use so the
    /// three weapons express the same contract (Property 27, task 13.4).
    /// </summary>
    public const HitStrength AntiHeavyMinStrength = GauntletLoopSteps.AntiHeavyMinStrength;

    /// <summary>Rise height (metres) of the W shot's KnockUp break, inside the shared Launch range (0.5–4.0).</summary>
    public const float HeavyArrowKnockUpHeight = 2f;

    /// <summary>Airborne/lock seconds of the W shot's KnockUp break.</summary>
    public const float HeavyArrowKnockUpDuration = 1f;

    /// <summary>Backstep distance (metres) the E (Leque Amplo) grants after firing the cone (Requisito 10.6).</summary>
    public const float WideFanBackstepDistance = 3f;

    /// <summary>
    /// The per-hit reaction identity of a Bow slot, plus the two non-reaction traits the coordinator
    /// needs: whether the slot keeps the player mobile (Q) and how far it backsteps (E). Pure data;
    /// carries no scene references.
    /// </summary>
    public readonly struct Reaction
    {
        /// <summary>The tagged stage this reaction belongs to.</summary>
        public BowLoopStage Stage { get; }

        /// <summary>Immediate reaction (Push/Stagger) applied regardless of stance.</summary>
        public HitReactionType ReactionType { get; }

        /// <summary>Relative attack power that gates stance-break thresholds (Heavy/Breaker breaks Heavy).</summary>
        public HitStrength Strength { get; }

        /// <summary>Hard CC applied when the hit breaks the enemy's stance.</summary>
        public StanceBreakEffect BreakEffect { get; }

        /// <summary>Metres the enemy is nudged on a Push (kept small for readability).</summary>
        public float PushDistance { get; }

        /// <summary>Seconds of stun/airborne lock on a Stun/KnockUp break.</summary>
        public float StunDuration { get; }

        /// <summary>Rise height (metres) on a KnockUp break; 0 when the break is not a KnockUp.</summary>
        public float KnockUpHeight { get; }

        /// <summary>Multiplier applied to the base stance damage the plan carries for this hit.</summary>
        public float StanceScale { get; }

        /// <summary>
        /// True when the slot is meant to keep the player mobile while firing (Q — Rajada Rápida,
        /// Requisito 10.4). The coordinator reads this to allow reduced-speed movement for the burst.
        /// </summary>
        public bool Mobile { get; }

        /// <summary>
        /// Metres the player backsteps after firing (E — Leque Amplo, Requisito 10.6); 0 for slots
        /// that grant no backstep.
        /// </summary>
        public float BackstepDistance { get; }

        /// <summary>
        /// True when this slot lands a stance reaction worth applying to enemies (W). Q/E/R do their
        /// work through projectiles / area and carry no stance-break reaction here.
        /// </summary>
        public bool AppliesStanceReaction => BreakEffect != StanceBreakEffect.None || StanceScale > 0f;

        /// <summary>True when this reaction lands with Heavy or Breaker force (Requisito 10.5 / 11.2).</summary>
        public bool IsHeavyOrBreaker => GauntletLoopSteps.IsHeavyOrBreaker(Strength);

        public Reaction(BowLoopStage stage, HitReactionType reactionType, HitStrength strength,
            StanceBreakEffect breakEffect, float pushDistance, float stunDuration, float knockUpHeight,
            float stanceScale, bool mobile, float backstepDistance)
        {
            Stage = stage;
            ReactionType = reactionType;
            Strength = strength;
            BreakEffect = breakEffect;
            PushDistance = Mathf.Max(0f, pushDistance);
            StunDuration = Mathf.Max(0f, stunDuration);
            KnockUpHeight = Mathf.Max(0f, knockUpHeight);
            StanceScale = Mathf.Max(0f, stanceScale);
            Mobile = mobile;
            BackstepDistance = Mathf.Max(0f, backstepDistance);
        }
    }

    /// <summary>Maps a weapon ability slot (0=Q, 1=W, 2=E, 3=R) to its Bow identity stage.</summary>
    public static BowLoopStage StageForSlot(int slot)
    {
        switch (slot)
        {
            case 0: return BowLoopStage.RapidBurst;
            case 1: return BowLoopStage.HeavyArrow;
            case 2: return BowLoopStage.WideFan;
            case 3: return BowLoopStage.ArrowRain;
            default: return BowLoopStage.Generic;
        }
    }

    /// <summary>
    /// Builds the per-hit reaction identity for <paramref name="slot"/>. The <paramref name="kind"/>
    /// (from the ability asset) is respected so a slot re-authored to a different skill kind still gets
    /// a coherent identity: a Rain kind always reads as the cursor-clamped rain, everything else reads
    /// as the slot's Bow identity.
    /// </summary>
    public static Reaction ReactionForSlot(int slot, ArsenalSkillKind kind)
    {
        return ReactionForStage(kind == ArsenalSkillKind.Rain ? BowLoopStage.ArrowRain : StageForSlot(slot), kind);
    }

    /// <summary>Stage-typed reaction identity. Pure; the single source of the Arco's per-slot contract.</summary>
    public static Reaction ReactionForStage(BowLoopStage stage, ArsenalSkillKind kind)
    {
        switch (stage)
        {
            // Q — Rajada Rápida (Requisito 10.4): mobile multi-shot DPS. It carries no stance CC of its
            // own — its damage flows through the fired arrows — but is tagged Mobile so the coordinator
            // keeps the player moving (at reduced speed) through the burst instead of pinning them.
            case BowLoopStage.RapidBurst:
                return new Reaction(stage, HitReactionType.None, HitStrength.Light,
                    StanceBreakEffect.None, pushDistance: 0f, stunDuration: 0f, knockUpHeight: 0f,
                    stanceScale: 0f, mobile: true, backstepDistance: 0f);

            // W — Flecha Pesada (Requisito 10.5 / 11.2): the anti-Heavy tool. Heavy/Breaker force and
            // extra stance damage so a charged shot can crack a Heavy enemy's stance, breaking into a
            // KnockUp. Not mobile (the shot is charged, telegraphed).
            case BowLoopStage.HeavyArrow:
                return new Reaction(stage, HitReactionType.Stagger, AntiHeavyMinStrength,
                    StanceBreakEffect.KnockUp, pushDistance: 0.4f, stunDuration: HeavyArrowKnockUpDuration,
                    knockUpHeight: HeavyArrowKnockUpHeight, stanceScale: 2f, mobile: false, backstepDistance: 0f);

            // E — Leque Amplo (Requisito 10.6): an emergency cone. Light stagger to buy space; its
            // signature is the backstep the coordinator grants after firing. No hard CC.
            case BowLoopStage.WideFan:
                return new Reaction(stage, HitReactionType.Stagger, HitStrength.Light,
                    StanceBreakEffect.None, pushDistance: 0.3f, stunDuration: 0.3f, knockUpHeight: 0f,
                    stanceScale: 0f, mobile: false, backstepDistance: WideFanBackstepDistance);

            // R — Chuva de Flechas (Requisito 10.7): territorial control clamped to the ability's
            // existing range by the cursor (enforced by the plan/coordinator). No stance CC of its own.
            case BowLoopStage.ArrowRain:
                return new Reaction(stage, HitReactionType.None, HitStrength.Light,
                    StanceBreakEffect.None, pushDistance: 0f, stunDuration: 0f, knockUpHeight: 0f,
                    stanceScale: 0f, mobile: false, backstepDistance: 0f);

            default:
                // Generic fallback: no Bow-specific identity, neutral values.
                return new Reaction(BowLoopStage.Generic, HitReactionType.None, HitStrength.Light,
                    StanceBreakEffect.None, pushDistance: 0f, stunDuration: 0f, knockUpHeight: 0f,
                    stanceScale: 0f, mobile: false, backstepDistance: 0f);
        }
    }
}
