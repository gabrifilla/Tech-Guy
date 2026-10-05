using System;
using System.Collections.Generic;

public enum KnockdownPhase { None, Falling, Grounded, Recovering }

/// <summary>Deterministic posture/effect lifecycle. All times use the same local gameplay clock.</summary>
public sealed class PostureReactionState
{
    private sealed class Lease
    {
        public int Token;
        public int Generation;
        public StanceBreakEffect Effect;
        public float Started;
        public float Ends;
    }

    private readonly List<Lease> _leases = new List<Lease>();
    private readonly VulnerabilityWindow _window = new VulnerabilityWindow();
    private int _nextToken;
    private int _generation;
    private float _firstControlStarted = float.NaN;
    private float _immuneUntil;
    private float _recoverAt;
    private bool _protectResume;
    private bool _waitingForResume;
    private float _resumeStarted;
    private long _resumeExecution;
    private float _multiplier;
    private float _recoveryRate;
    private float _recoveryDelay;

    public ReactionConfiguration Configuration { get; private set; }
    public float Now { get; private set; }
    public float CurrentStance { get; private set; }
    public float MaxStance { get; private set; }
    public int Generation => _generation;
    public bool IsVulnerable => _window.IsOpen;
    public EnemyRank EffectiveRank => _window.EffectiveRank;
    public bool IsStunned => _leases.Count > 0;
    public bool IsControlLocked => IsStunned || (Configuration.RestrictiveOpening && IsVulnerable);
    public bool IsResumeProtected => _protectResume;
    public bool IsBreakImmune => Now < _immuneUntil || _protectResume;
    public bool ResumeWatchdogExpired { get; private set; }
    public StanceBreakEffect ActiveMotion
    {
        get
        {
            foreach (var lease in _leases) if (lease.Effect != StanceBreakEffect.Stun) return lease.Effect;
            return StanceBreakEffect.None;
        }
    }

    public PostureReactionState(ReactionConfiguration config, float maxStance, float multiplier, float rate, float delay)
        => Configure(config, maxStance, multiplier, rate, delay);

    public void Configure(ReactionConfiguration config, float maxStance, float multiplier, float rate, float delay)
    {
        Configuration = config ?? new ReactionConfiguration(EnemyRank.Normal);
        MaxStance = ReactionNumeric.Safe(maxStance, 1f, float.MaxValue, RankReactionDefaults.For(Configuration.Rank).MaxStance);
        _multiplier = ReactionNumeric.Safe(multiplier, 0f, 5f, 1f);
        _recoveryRate = ReactionNumeric.Safe(rate, 0f, float.MaxValue, 25f);
        _recoveryDelay = ReactionNumeric.Safe(delay, 0f, 10f, 2.5f);
        Reset();
    }

    public void Reset()
    {
        _generation++;
        _leases.Clear();
        _window.Close();
        _window.SetBaseRank(Configuration.Rank);
        Now = 0f;
        CurrentStance = MaxStance;
        _immuneUntil = _recoverAt = 0f;
        _firstControlStarted = float.NaN;
        _protectResume = _waitingForResume = false;
        _resumeExecution = 0;
        ResumeWatchdogExpired = false;
    }

    /// <returns>True exactly when this contribution detects a legitimate break.</returns>
    public bool ApplyStanceDamage(float damage)
    {
        if (IsBreakImmune || !ReactionNumeric.IsFinite(damage) || damage <= 0f || _multiplier <= 0f) return false;
        CurrentStance = StanceBreakBounds.SubtractStance(CurrentStance, damage, _multiplier);
        _recoverAt = Now + _recoveryDelay;
        if (CurrentStance > 0f) return false;
        // Commit before any coordinator notifies callbacks (reentrant hits already see immunity).
        CurrentStance = StanceBreakBounds.RestoredStanceOnBreak(MaxStance);
        _immuneUntil = Now + Configuration.BreakImmunitySeconds;
        _window.Open(Now, Configuration.VulnerabilitySeconds);
        _protectResume = Configuration.RestrictiveOpening;
        _waitingForResume = false;
        _resumeExecution = 0;
        return true;
    }

    public void Advance(float delta, bool paused = false, bool hitStopped = false)
    {
        if (paused || hitStopped || !ReactionNumeric.IsFinite(delta) || delta <= 0f) return;
        Now = (float)Math.Min(float.MaxValue, (double)Now + delta);
        _window.Update(Now);
        _leases.RemoveAll(lease => lease.Ends <= Now);
        if (_leases.Count == 0) _firstControlStarted = float.NaN;
        if (CurrentStance < MaxStance && Now > _recoverAt)
        {
            float elapsed = Math.Min(delta, Now - _recoverAt);
            CurrentStance = (float)Math.Min(MaxStance, CurrentStance + (double)_recoveryRate * elapsed);
        }
        if (_protectResume && !IsControlLocked)
        {
            if (!_waitingForResume) { _waitingForResume = true; _resumeStarted = Now; }
            if (Now - _resumeStarted >= Configuration.WatchdogSeconds)
            {
                ResumeWatchdogExpired = true;
                EndResumeProtection();
            }
        }
    }

    public int StartEffect(StanceBreakEffect effect, float duration)
    {
        if (effect == StanceBreakEffect.None || !Enum.IsDefined(typeof(StanceBreakEffect), effect)
            || !ReactionNumeric.InRange(duration, 0.01f, 10f)) return 0;
        var existing = _leases.Find(lease => lease.Effect == effect);
        float end = Now + duration;
        if (existing != null)
        {
            if (Configuration.Reapplication == ReactionReapplication.Ignore) return 0;
            if (Configuration.Reapplication == ReactionReapplication.ExtendLimited) end = existing.Ends + duration;
        }
        var motion = _leases.Find(lease => lease.Effect != StanceBreakEffect.Stun);
        if (effect != StanceBreakEffect.Stun && motion != null && Priority(effect) < Priority(motion.Effect)) return 0;
        if (float.IsNaN(_firstControlStarted)) _firstControlStarted = Now;
        end = Math.Min(end, _firstControlStarted + Configuration.ContinuousLimit);
        if (end - Now < 0.01f) return 0;
        if (existing != null) _leases.Remove(existing);
        if (effect != StanceBreakEffect.Stun && motion != null) _leases.Remove(motion);
        int token = ++_nextToken;
        _leases.Add(new Lease { Token = token, Generation = _generation, Effect = effect, Started = Now, Ends = end });
        return token;
    }

    public bool Owns(int token, int generation) => generation == _generation
        && _leases.Exists(lease => lease.Token == token && lease.Generation == generation);

    public bool EndEffect(int token, int generation)
    {
        if (!Owns(token, generation)) return false;
        _leases.RemoveAll(lease => lease.Token == token);
        if (_leases.Count == 0) _firstControlStarted = float.NaN;
        return true;
    }

    public float Remaining(int token) => Math.Max(0f, (_leases.Find(lease => lease.Token == token)?.Ends ?? Now) - Now);
    public float Progress(int token)
    {
        var lease = _leases.Find(value => value.Token == token);
        return lease == null ? 1f : Math.Min(1f, Math.Max(0f, (Now - lease.Started) / (lease.Ends - lease.Started)));
    }

    public KnockdownPhase CurrentKnockdownPhase
    {
        get
        {
            var lease = _leases.Find(value => value.Effect == StanceBreakEffect.Knockdown);
            if (lease == null) return KnockdownPhase.None;
            float total = Configuration.FallSeconds + Configuration.GroundSeconds + Configuration.RecoverSeconds;
            float authoredElapsed = Progress(lease.Token) * total;
            return authoredElapsed < Configuration.FallSeconds ? KnockdownPhase.Falling
                : authoredElapsed < Configuration.FallSeconds + Configuration.GroundSeconds ? KnockdownPhase.Grounded
                : KnockdownPhase.Recovering;
        }
    }

    public void EvaluateResumeEligibility(bool eligible)
    {
        if (_protectResume && !IsControlLocked && !eligible) EndResumeProtection();
    }

    public void BeginOffensiveAction(long execution)
    {
        if (_protectResume && !IsControlLocked && _resumeExecution == 0 && execution != 0) _resumeExecution = execution;
    }

    public void EndOffensiveAction(long execution)
    {
        if (_protectResume && execution != 0 && execution == _resumeExecution) EndResumeProtection();
    }

    public void CancelOffensiveActionExternally() { if (!IsControlLocked) EndResumeProtection(); }
    private void EndResumeProtection() { _protectResume = _waitingForResume = false; _resumeExecution = 0; }
    public static int Priority(StanceBreakEffect effect) => effect == StanceBreakEffect.Knockdown ? 4
        : effect == StanceBreakEffect.KnockUp ? 3 : effect == StanceBreakEffect.Knockback ? 2 : 0;
}
