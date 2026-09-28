using System;
using UnityEngine;

/// <summary>
/// Escalating "system breaking" feedback state (R10). Plain C# — not a <see cref="MonoBehaviour"/> —
/// owned and driven per run by <c>RunBoons</c>. It maps the count of accumulated interacting
/// modifiers onto discrete escalation tiers (thresholds 3/6/9) and presents purely cosmetic
/// feedback when a tier boundary is crossed upward.
/// <para>
/// This type never touches damage, cascade limits, or reward rules; it only asks an injected
/// <see cref="ISystemBreakFeedback"/> sink to present the tier's bundle. A missing tier bundle
/// (or a missing sink) is skipped without throwing so partially-authored content cannot break a run.
/// </para>
/// </summary>
public sealed class SystemBreakState
{
    /// <summary>Interacting-modifier counts at which each escalation tier begins (R10.1).</summary>
    private static readonly int[] Thresholds = { 3, 6, 9 };

    /// <summary>Highest tier the maximum-tier index can reach (0 = calm, 3 = fully "breaking").</summary>
    public const int MaxTier = 3;

    private readonly ISystemBreakFeedback _feedback;

    /// <summary>Current escalation tier in the range 0..<see cref="MaxTier"/>.</summary>
    public int Tier { get; private set; }

    /// <summary>
    /// The channels a tier can fire, in the fixed order they are presented when several fire
    /// together (R10.3). Lower values present first.
    /// </summary>
    public enum Channel { GlitchVisual = 0, WeaponComment = 1, FakeSystemMessage = 2 }

    /// <summary>
    /// A tier's cosmetic feedback bundle. Any channel may be left null; null channels are skipped
    /// so an incompletely authored bundle presents whatever it has without raising an error (R10.7).
    /// </summary>
    public readonly struct FeedbackBundle
    {
        public readonly string GlitchVisual;
        public readonly string WeaponComment;
        public readonly string FakeSystemMessage;

        public FeedbackBundle(string glitchVisual, string weaponComment, string fakeSystemMessage)
        {
            GlitchVisual = glitchVisual;
            WeaponComment = weaponComment;
            FakeSystemMessage = fakeSystemMessage;
        }

        /// <summary>True when the bundle carries no presentable channel (a "missing" bundle).</summary>
        public bool IsEmpty =>
            string.IsNullOrEmpty(GlitchVisual) &&
            string.IsNullOrEmpty(WeaponComment) &&
            string.IsNullOrEmpty(FakeSystemMessage);
    }

    /// <summary>
    /// Presentation sink for escalation feedback. Kept as a seam so <see cref="SystemBreakState"/>
    /// stays a plain, testable C# class and so the concrete overlay (glitch layer, weapon barks,
    /// fake OS notices) can be swapped without touching the escalation logic. Implementations are
    /// responsible for rendering on a non-blocking overlay that keeps gameplay-critical UI legible
    /// (R10.6). May be null — a null sink disables presentation without disabling tier tracking.
    /// </summary>
    public interface ISystemBreakFeedback
    {
        /// <summary>Returns the bundle for <paramref name="tier"/> (1..3), or null/empty if none exists.</summary>
        FeedbackBundle? BundleFor(int tier);

        /// <summary>
        /// Presents a single channel for a tier. <paramref name="intensity"/> and
        /// <paramref name="frequency"/> are non-decreasing with tier (R10.3); implementations use
        /// them to scale visual/audio emphasis and repeat cadence. Presentation is cosmetic only.
        /// </summary>
        void Present(int tier, Channel channel, string content, float intensity, float frequency);
    }

    /// <param name="feedback">Presentation sink; may be null to run tier tracking without visuals.</param>
    public SystemBreakState(ISystemBreakFeedback feedback = null) => _feedback = feedback;

    /// <summary>
    /// Maps a raw interacting-modifier count onto a tier (R10.1). The tier is the number of
    /// thresholds the count has reached, which makes it monotonic non-decreasing in the count.
    /// </summary>
    public static int TierFor(int interactingCount)
    {
        int tier = 0;
        for (int i = 0; i < Thresholds.Length; i++)
            if (interactingCount >= Thresholds[i]) tier++;
        return tier; // 0..3
    }

    /// <summary>
    /// Recomputes the tier from <paramref name="interactingCount"/> and, when the tier rises,
    /// presents each newly reached tier's bundle (R10.2/R10.3). Lowering the count updates the
    /// tier but presents nothing. Cosmetic only — no gameplay state is touched (R10.4).
    /// </summary>
    /// <returns>The tier after evaluation.</returns>
    public int Evaluate(int interactingCount)
    {
        int target = TierFor(Mathf.Max(0, interactingCount));
        if (target > Tier)
        {
            // Present every crossed boundary in order so skipping straight from tier 0 to 3
            // (a big single pick-up) still escalates through each tier's bundle.
            for (int crossed = Tier + 1; crossed <= target; crossed++)
                PresentTier(crossed);
        }
        Tier = target;
        return Tier;
    }

    /// <summary>Resets escalation state at run end (R10.5). Presents nothing.</summary>
    public void Reset() => Tier = 0;

    private void PresentTier(int tier)
    {
        if (_feedback == null) return;

        FeedbackBundle? bundle;
        try
        {
            bundle = _feedback.BundleFor(tier);
        }
        catch (Exception ex)
        {
            // A faulty content provider must not break the run; skip this tier's bundle (R10.7).
            Debug.LogException(ex);
            return;
        }

        if (bundle == null || bundle.Value.IsEmpty) return; // missing bundle -> skipped, no error (R10.7)

        FeedbackBundle content = bundle.Value;
        float intensity = IntensityFor(tier);
        float frequency = FrequencyFor(tier);

        // Fixed ordering when several channels fire together: glitch -> weapon comment -> fake message (R10.3).
        TryPresent(tier, Channel.GlitchVisual, content.GlitchVisual, intensity, frequency);
        TryPresent(tier, Channel.WeaponComment, content.WeaponComment, intensity, frequency);
        TryPresent(tier, Channel.FakeSystemMessage, content.FakeSystemMessage, intensity, frequency);
    }

    private void TryPresent(int tier, Channel channel, string content, float intensity, float frequency)
    {
        if (string.IsNullOrEmpty(content)) return; // null-guard a missing channel within the bundle (R10.7)
        try
        {
            _feedback.Present(tier, channel, content, intensity, frequency);
        }
        catch (Exception ex)
        {
            // Isolate a misbehaving presenter so one bad channel cannot abort the rest of the bundle.
            Debug.LogException(ex);
        }
    }

    /// <summary>Emphasis for a tier; strictly increasing so higher tiers present more intensely (R10.3).</summary>
    public static float IntensityFor(int tier) => Mathf.Clamp01(tier / (float)MaxTier);

    /// <summary>Repeat cadence for a tier; non-decreasing so higher tiers present more frequently (R10.3).</summary>
    public static float FrequencyFor(int tier) => tier <= 0 ? 0f : tier;
}
