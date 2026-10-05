/// <summary>One pure decision path for category gates and every physical fallback.</summary>
public static class ReactionPolicy
{
    public static bool CanContribute(ReactionConfiguration config, bool locked, bool resumeProtected, float staggerResistance)
        => !config.IsBoss && !locked && !resumeProtected && ReactionNumeric.InRange(staggerResistance, 0f, 1f)
        && staggerResistance < 1f;

    public static InterruptWindowGate.Decision ResolveInterrupt(ReactionConfiguration config, bool locked,
        bool resumeProtected, bool inWindow, bool thresholdReached, float staggerResistance)
    {
        bool allowed = CanContribute(config, locked, resumeProtected, staggerResistance)
            && (config.Category == ReactionCategory.SmallMob || thresholdReached);
        return InterruptWindowGate.Resolve(allowed && inWindow);
    }

    public static StanceBreakEffect ResolveBreak(ReactionConfiguration config, StanceBreakEffect requested,
        float stunResistance, float launchResistance, float knockbackResistance)
    {
        var candidate = requested == StanceBreakEffect.None ? config.DefaultBreakEffect : requested;
        return ResolvePhysical(config, candidate, stunResistance, launchResistance, knockbackResistance);
    }

    // None here is a final result: it must never resolve the category default again.
    public static StanceBreakEffect ResolvePhysical(ReactionConfiguration config, StanceBreakEffect candidate,
        float stunResistance, float launchResistance, float knockbackResistance)
    {
        if (config.IsBoss)
        {
            if (!config.AllowBossLaunch) launchResistance = 1f;
            if (!config.AllowBossKnockback) knockbackResistance = 1f;
        }
        float downResistance = config.IsBoss && !config.AllowBossKnockdown ? 1f : config.KnockdownResistance;
        return BreakEffectResistance.Resolve(candidate, stunResistance, launchResistance, knockbackResistance, downResistance);
    }

    public static float AllowedDuration(float authored, float resistance) =>
        !ReactionNumeric.InRange(resistance, 0f, 1f) || BreakEffectResistance.IsFullyImmune(resistance)
        ? 0f : ReactionNumeric.Safe(authored, 0.01f, 10f, 1f);
}
