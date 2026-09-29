using UnityEngine;

/// <summary>
/// The four ability identities of the Lança (Spear), one per Q/W/E/R slot (Requisito 9.1/9.3–9.6):
/// Q — Estocada/Avanço (line thrust with advance, an effective counter to a charging enemy);
/// W — Varredura orbital (orbital sweep, the anti-swarm area tool); E — Perfuração pesada (heavy
/// pierce with Heavy/Breaker force able to crack a Heavy enemy's stance); R — Onda do Dragão
/// (a piercing space-control wave). Each slot is usable on its own; the enum only tags a slot so the
/// identity invariants can be applied to a per-cast snapshot.
/// </summary>
public enum SpearLoopStage
{
    /// <summary>Q — line thrust that advances and counters a charge (Requisito 9.3 / 11.3).</summary>
    Thrust,
    /// <summary>W — orbital sweep, the anti-swarm area cleaner with a limited push (Requisito 9.4 / 7.4).</summary>
    Sweep,
    /// <summary>E — heavy pierce with Heavy/Breaker force that breaks Heavy stance (Requisito 9.5 / 11.2).</summary>
    Pierce,
    /// <summary>R — Dragon Wave: a piercing wave of space control (Requisito 9.6).</summary>
    DragonWave,
    /// <summary>Any other slot: no Lança-specific identity is enforced.</summary>
    Generic
}

/// <summary>
/// Pure, scene-free configuration helper that pins the Lança identity onto a per-cast hit
/// (Requisito 9.1/9.3/9.4/9.5/9.6). The Lança's hits are built by <c>ArsenalCombat</c> from an
/// <see cref="ArsenalCastPlan"/> snapshot rather than from an <see cref="AreaHitStep"/> list (that is
/// the Manoplas path, handled by <c>GauntletLoopSteps</c>), so this helper works at the level of the
/// single <see cref="HitReactionRequest"/> the spear applies per hit: it decides, per slot, the
/// immediate reaction, the <see cref="HitStrength"/>, the stance break effect and the stance-damage
/// scale — expressing everything through the existing <see cref="HitReactionRequest"/> fields and
/// never introducing a parallel channel (Requisito 4.4).
///
/// It is kept as a plain static class (no <see cref="MonoBehaviour"/>) so <c>ArsenalCombat</c> stays
/// thin (AGENTS.md) and the identity can be property-tested without a live scene.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 11.5.
/// Requirements: 9.1, 9.3, 9.4, 9.5, 9.6, 7.4.
/// </remarks>
public static class SpearLoopSteps
{
    /// <summary>
    /// Minimum strength the E (Perfuração pesada) hits land with so they can break a Heavy enemy's
    /// stance (Requisito 9.5 / 11.2). Shares the exact anti-Heavy floor the Manoplas E uses so the
    /// three weapons express the same contract (Property 27, task 13.4).
    /// </summary>
    public const HitStrength AntiHeavyMinStrength = GauntletLoopSteps.AntiHeavyMinStrength;

    /// <summary>Rise height (metres) of the E pierce's KnockUp break, inside the shared Launch range (0.5–4.0).</summary>
    public const float PierceKnockUpHeight = 2f;

    /// <summary>Airborne/lock seconds of the E pierce's KnockUp break.</summary>
    public const float PierceKnockUpDuration = 1f;

    /// <summary>
    /// The per-hit reaction identity of a Lança slot: the values <c>ArsenalCombat</c> feeds into the
    /// <see cref="HitReactionRequest"/> it applies. Pure data; carries no scene references.
    /// </summary>
    public readonly struct Reaction
    {
        /// <summary>The tagged stage this reaction belongs to.</summary>
        public SpearLoopStage Stage { get; }

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

        /// <summary>True when the slot is a thrust (Q/E/R identities that thrust); false for the sweep.</summary>
        public bool IsThrust => Stage != SpearLoopStage.Sweep;

        /// <summary>True when this reaction lands with Heavy or Breaker force (Requisito 9.5).</summary>
        public bool IsHeavyOrBreaker => GauntletLoopSteps.IsHeavyOrBreaker(Strength);

        public Reaction(SpearLoopStage stage, HitReactionType reactionType, HitStrength strength,
            StanceBreakEffect breakEffect, float pushDistance, float stunDuration, float knockUpHeight, float stanceScale)
        {
            Stage = stage;
            ReactionType = reactionType;
            Strength = strength;
            BreakEffect = breakEffect;
            PushDistance = Mathf.Max(0f, pushDistance);
            StunDuration = Mathf.Max(0f, stunDuration);
            KnockUpHeight = Mathf.Max(0f, knockUpHeight);
            StanceScale = Mathf.Max(0f, stanceScale);
        }
    }

    /// <summary>Maps a weapon ability slot (0=Q, 1=W, 2=E, 3=R) to its Lança identity stage.</summary>
    public static SpearLoopStage StageForSlot(int slot)
    {
        switch (slot)
        {
            case 0: return SpearLoopStage.Thrust;
            case 1: return SpearLoopStage.Sweep;
            case 2: return SpearLoopStage.Pierce;
            case 3: return SpearLoopStage.DragonWave;
            default: return SpearLoopStage.Generic;
        }
    }

    /// <summary>
    /// Builds the per-hit reaction identity for <paramref name="slot"/>. The <paramref name="kind"/>
    /// (from the ability asset) is respected so a slot re-authored to a different skill kind still
    /// gets a coherent reaction: a Sweep kind always reads as the anti-swarm sweep, everything else
    /// reads as a thrust of the slot's identity.
    /// </summary>
    public static Reaction ReactionForSlot(int slot, ArsenalSkillKind kind)
    {
        return ReactionForStage(kind == ArsenalSkillKind.Sweep ? SpearLoopStage.Sweep : StageForSlot(slot), kind);
    }

    /// <summary>Stage-typed reaction identity. Pure; the single source of the Lança's per-hit contract.</summary>
    public static Reaction ReactionForStage(SpearLoopStage stage, ArsenalSkillKind kind)
    {
        switch (stage)
        {
            // Q — Estocada/Avanço (Requisito 9.3 / 11.3): a precise line thrust that staggers and chips
            // stance. Medium force with a Stagger interrupt is what makes it an effective counter to a
            // Charger — it interrupts the dash without needing a full break. No hard launch on Q.
            case SpearLoopStage.Thrust:
                return new Reaction(stage, HitReactionType.Stagger, HitStrength.Medium,
                    StanceBreakEffect.None, pushDistance: 0.35f, stunDuration: 0.5f, knockUpHeight: 0f, stanceScale: 1f);

            // W — Varredura orbital (Requisito 9.4): the anti-swarm sweep. Light stagger so it cleans a
            // crowd without launching; its signature displacement is the LIMITED sweep push (R7.4),
            // applied separately through the enemy's existing locomotion, not as a hard knockback here.
            case SpearLoopStage.Sweep:
                return new Reaction(stage, HitReactionType.Stagger, HitStrength.Light,
                    StanceBreakEffect.None, pushDistance: 0.3f, stunDuration: 0.4f, knockUpHeight: 0f, stanceScale: 1f);

            // E — Perfuração pesada (Requisito 9.5 / 11.2): the anti-Heavy tool. Heavy/Breaker force and
            // extra stance damage so it can crack a Heavy enemy's stance, breaking into a KnockUp.
            case SpearLoopStage.Pierce:
                return new Reaction(stage, HitReactionType.Stagger, AntiHeavyMinStrength,
                    StanceBreakEffect.KnockUp, pushDistance: 0.4f, stunDuration: PierceKnockUpDuration,
                    knockUpHeight: PierceKnockUpHeight, stanceScale: 2f);

            // R — Onda do Dragão (Requisito 9.6): a piercing space-control wave. Heavy force with a
            // moderate Push (space control), no vertical launch — the wave clears a line, it doesn't juggle.
            case SpearLoopStage.DragonWave:
                return new Reaction(stage, HitReactionType.Push, HitStrength.Heavy,
                    StanceBreakEffect.None, pushDistance: 0.6f, stunDuration: 0.5f, knockUpHeight: 0f, stanceScale: 1.25f);

            default:
                // Generic fallback keeps the previous baseline (Medium/Stagger/Stun) so an unexpected
                // slot never loses its reaction entirely.
                return new Reaction(SpearLoopStage.Generic, HitReactionType.Stagger, HitStrength.Medium,
                    StanceBreakEffect.Stun, pushDistance: 0.35f, stunDuration: 1f, knockUpHeight: 0f, stanceScale: 1f);
        }
    }
}
