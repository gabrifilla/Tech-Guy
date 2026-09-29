using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The stages of the Manoplas (Fists) combat loop, one identity per Q/W/E/R slot
/// (Requisito 8.1): ENTRAR (Q, Avanço Relâmpago) → INTERROMPER / PRESSIONAR (W, Flurry) →
/// QUEBRAR POSTURA / JUGGLE (E, Impacto de Choque) → ASURA (R). Each stage is usable on its own,
/// without requiring the next one — the enum only tags a slot so the loop invariants can be applied.
/// </summary>
public enum GauntletLoopStage
{
    /// <summary>Q — Lightning Advance: closes the gap into the group, clamped to the NavMesh.</summary>
    Advance,
    /// <summary>W — Flurry: repeated strikes on the locked target for the whole declared duration.</summary>
    Flurry,
    /// <summary>E — Shock Impact / Stance Breaker: Heavy/Breaker stance damage that opens a juggle.</summary>
    StanceBreaker,
    /// <summary>R — Asura burst: the earned power mode.</summary>
    Asura,
    /// <summary>Any other slot: no loop-specific invariant is enforced.</summary>
    Generic
}

/// <summary>
/// Pure, scene-free configuration helper that pins the Manoplas loop identity onto a per-cast
/// <see cref="AreaHitStep"/> snapshot (Requisito 8.1/8.5/8.8/8.9). It expresses the loop entirely
/// through the existing <see cref="AreaHitStep"/> reaction/stance fields — it never introduces a
/// parallel channel and never touches the source ability asset (the caller already works on a
/// cloned snapshot via <c>WeaponRunModifiers.GauntletSteps</c>).
///
/// The invariants it guarantees, so the loop behaves correctly regardless of how conservatively an
/// ability asset is authored:
/// <list type="bullet">
/// <item><b>E — Stance Breaker (Requisito 8.5 / Property 27):</b> every step lands with at least
/// <see cref="HitStrength.Heavy"/> so it can break a Heavy enemy's stance, and its breaking step
/// declares a <see cref="StanceBreakEffect.KnockUp"/> so QUEBRAR flows into JUGGLE.</item>
/// <item><b>Juggle declaration (Requisito 8.8/8.9 / Property 29):</b> a step that intends to break
/// stance into a launch gets a KnockUp with a positive rise height and a positive airborne duration,
/// so an enemy with <c>KnockUpResistance</c> &lt; 1 stays control-locked while airborne and has its
/// control restored when the air duration ends. The actual lock/restore lives in
/// <c>CombatReactionController</c>; this only makes the gauntlet declare the KnockUp.</item>
/// </list>
///
/// It is kept as a plain static class (no <see cref="MonoBehaviour"/>) so <c>BreakerGauntletCombat</c>
/// stays thin and Properties 26/27/29 can drive it without a live scene.
/// </summary>
public static class GauntletLoopSteps
{
    /// <summary>Minimum strength E (Stance Breaker) hits land with (Requisito 8.5).</summary>
    public const HitStrength AntiHeavyMinStrength = HitStrength.Heavy;

    /// <summary>Default rise height (metres) for a loop KnockUp when the step declares none. In the shared Launch range (0.5–4.0, see <see cref="DisplacementTier.KnockUpMin"/>).</summary>
    public const float DefaultJuggleHeight = 2f;

    /// <summary>Default airborne/lock duration (seconds) for a loop KnockUp when the step declares none.</summary>
    public const float DefaultJuggleAirtime = 1f;

    /// <summary>Maps a weapon ability slot (0=Q, 1=W, 2=E, 3=R) to its loop stage.</summary>
    public static GauntletLoopStage StageForSlot(int slot)
    {
        switch (slot)
        {
            case 0: return GauntletLoopStage.Advance;
            case 1: return GauntletLoopStage.Flurry;
            case 2: return GauntletLoopStage.StanceBreaker;
            case 3: return GauntletLoopStage.Asura;
            default: return GauntletLoopStage.Generic;
        }
    }

    /// <summary>True for a Heavy or Breaker hit — the anti-Heavy force floor (Requisito 8.5, Property 27).</summary>
    public static bool IsHeavyOrBreaker(HitStrength strength) => strength >= AntiHeavyMinStrength;

    /// <summary>
    /// Applies the loop identity invariants for <paramref name="slot"/> in place on the already-cloned
    /// runtime <paramref name="steps"/>. Safe to call with a null/empty list (no-op). Only the E
    /// (Stance Breaker) stage changes anything today; every other stage is left exactly as authored so
    /// the data stays the single source of truth for damage/timing/range (Requisito 8.1).
    /// </summary>
    public static void Configure(IReadOnlyList<AreaHitStep> steps, int slot)
    {
        Configure(steps, StageForSlot(slot));
    }

    /// <summary>Stage-typed overload of <see cref="Configure(IReadOnlyList{AreaHitStep}, int)"/>.</summary>
    public static void Configure(IReadOnlyList<AreaHitStep> steps, GauntletLoopStage stage)
    {
        if (steps == null || steps.Count == 0) return;
        if (stage != GauntletLoopStage.StanceBreaker) return;

        // E — Impacto de Choque / Stance Breaker (Requisito 8.5, Property 27): guarantee every hit is
        // Heavy/Breaker so it can crack a Heavy enemy's stance, and make the finishing hit declare a
        // KnockUp so QUEBRAR POSTURA opens into JUGGLE (Requisito 8.8/8.9, Property 29).
        int breakStepIndex = LastBreakingStepIndex(steps);
        for (int i = 0; i < steps.Count; i++)
        {
            AreaHitStep step = steps[i];
            if (step == null) continue;
            EnsureHeavyOrBreaker(step);
            if (i == breakStepIndex) EnsureJuggleDeclaration(step);
        }
    }

    /// <summary>
    /// Raises a step's <see cref="AreaHitStep.hitStrength"/> to at least Heavy without ever weakening a
    /// stronger authored value (Heavy stays Heavy, Breaker stays Breaker) — Requisito 8.5 / Property 27.
    /// </summary>
    public static void EnsureHeavyOrBreaker(AreaHitStep step)
    {
        if (step == null) return;
        if (!IsHeavyOrBreaker(step.hitStrength)) step.hitStrength = AntiHeavyMinStrength;
    }

    /// <summary>
    /// Ensures a stance-breaking step declares a juggle: a <see cref="StanceBreakEffect.KnockUp"/> with a
    /// positive rise height and a positive airborne duration, so an enemy with KnockUpResistance &lt; 1
    /// is juggled (control-locked while airborne) and freed when the air duration ends (Requisito
    /// 8.8/8.9). An authored Knockback/Stun break is respected and left untouched; only a step that
    /// breaks stance without declaring any hard-CC launch (BreakEffect None) is promoted to KnockUp so
    /// the loop's QUEBRAR→JUGGLE stage always exists. Never downgrades an explicit designer choice.
    /// </summary>
    public static void EnsureJuggleDeclaration(AreaHitStep step)
    {
        if (step == null) return;
        if (step.breakEffect != StanceBreakEffect.None) return; // respect an authored launch/stun.

        step.breakEffect = StanceBreakEffect.KnockUp;
        if (step.knockUpHeight <= 0f) step.knockUpHeight = DefaultJuggleHeight;
        if (step.stunDuration <= 0f) step.stunDuration = DefaultJuggleAirtime;
    }

    /// <summary>
    /// Finds the index of the loop's breaking step: the last step that actually deals stance damage
    /// (the finisher of the sequence), or -1 when no step deals stance damage.
    /// </summary>
    private static int LastBreakingStepIndex(IReadOnlyList<AreaHitStep> steps)
    {
        for (int i = steps.Count - 1; i >= 0; i--)
        {
            AreaHitStep step = steps[i];
            if (step != null && step.stanceDamage > 0f) return i;
        }
        return -1;
    }
}
