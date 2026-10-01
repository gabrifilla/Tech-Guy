using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Rain Mark boon (gauntlet-boon-playstyle-overhaul R10, "Chuva marcadora"). A run-scoped registry
/// added to the player by <c>RunBoons</c> when the Bow ultimate boon is chosen.
///
/// The Bow R (<c>ArsenalSkillKind.Rain</c>) is the pulsing arrow storm. Instead of adding more
/// pulses/radius (the retired <c>LongRain</c>), this boon turns the ultimate into a zoning + damage-
/// prep tool: every resolved pulse <see cref="Mark"/>s the enemy it hits for a limited duration
/// (R10.1), the mark <b>slows</b> that enemy's movement while it lasts and restores normal movement
/// when it expires (R10.3), and a marked enemy takes amplified direct damage — <see cref="AmplifierFor"/>
/// returns <c>1 + amplify</c> (<c>1 + 0.2 × rank</c>) while the mark is active and exactly <c>1</c>
/// otherwise (R10.2).
///
/// Isolation (R10.4): the mark behavior is driven entirely by the per-cast <c>ArsenalCastPlan</c> flags
/// (<c>MarkOnPulse</c>/<c>MarkAmplify</c>/<c>MarkSlow</c>) plus this run-scoped state; the source rain
/// ability asset is never written. The slow reuses the enemy's existing NavMeshAgent locomotion (the
/// same channel <c>ChillStatus</c> drives) rather than a parallel movement system, and the restore is
/// owned by the mark's expiry so a marked enemy always returns to full speed when the mark ends or the
/// run tears down.
///
/// Thin MonoBehaviour: the amplifier/expiry decision is pure (a dictionary keyed by <see cref="Actor"/>
/// against an injectable clock), and the only Unity coupling is the per-enemy slow, which goes through
/// an injectable <see cref="ISlowHandle"/> factory so the amplifier/expiry round-trip can be exercised
/// scene-free (Property 16). In production the factory drives the enemy's <see cref="NavMeshAgent"/>.
///
/// Lifecycle: <see cref="OnDestroy"/> clears every mark and restores any pending slow, so a previous
/// run's mark never slows or amplifies in a later run.
/// </summary>
public sealed class RainMarkRegistry : MonoBehaviour
{
    /// <summary>Mark duration in seconds (adjustable starting point, R10.1).</summary>
    public const float Duration = 4f;

    /// <summary>
    /// A per-enemy slow handle. <see cref="Apply"/> reduces the enemy's movement; <see cref="Restore"/>
    /// returns it to normal. Kept behind an interface so the mark/expiry round-trip can be tested without
    /// a NavMeshAgent (R10.3), while production drives the real agent.
    /// </summary>
    public interface ISlowHandle
    {
        void Apply(float slowFraction);
        void Restore();
    }

    // Per-mark bookkeeping: the expiry time (on the injected clock) and the slow handle to restore when
    // the mark ends. One entry per currently-marked enemy.
    private sealed class Mark_
    {
        public float ExpireAt;
        public ISlowHandle Slow;
    }

    private readonly Dictionary<Actor, Mark_> _marks = new Dictionary<Actor, Mark_>();
    private readonly List<Actor> _expired = new List<Actor>(); // scratch list to avoid mutating while iterating
    private float _amplify;
    private float _slow;

    // Injectable clock (defaults to Time.time) and slow-handle factory (defaults to a NavMeshAgent-backed
    // handle). Tests assign deterministic replacements so the pure amplifier/expiry and the slow round-trip
    // can be verified without a scene.
    private Func<float> _clock = () => Time.time;
    private Func<Actor, ISlowHandle> _slowFactory = enemy => new AgentSlowHandle(enemy);

    /// <summary>
    /// Binds the registry to the run. <paramref name="amplify"/> is the extra direct-damage fraction while
    /// marked (<c>0.2 × rank</c>, R10.2) and <paramref name="slow"/> is the movement slow fraction applied
    /// while marked (R10.3). Both come from the per-cast <c>ArsenalCastPlan</c> flags. Safe to call again
    /// when the boon rank increases; it only updates the configured amounts (existing marks keep their
    /// expiry, and their amplifier/slow follow the new values).
    /// </summary>
    public void Configure(float amplify, float slow)
    {
        _amplify = Mathf.Max(0f, amplify);
        _slow = Mathf.Clamp01(slow);
    }

    /// <summary>Test seam: inject a deterministic clock and/or slow-handle factory. Null leaves the default.</summary>
    internal void ConfigureForTests(Func<float> clock, Func<Actor, ISlowHandle> slowFactory)
    {
        if (clock != null) _clock = clock;
        if (slowFactory != null) _slowFactory = slowFactory;
    }

    /// <summary>
    /// Marks <paramref name="enemy"/> for <see cref="Duration"/> seconds (R10.1), applying the configured
    /// movement slow (R10.3). Re-marking a still-marked enemy refreshes the expiry and keeps its single
    /// slow handle (no stacking). Dead/null enemies are ignored. Called by the rain pulse resolution when
    /// the per-cast plan has <c>MarkOnPulse</c>.
    /// </summary>
    public void Mark(Actor enemy)
    {
        if (!enemy || enemy.IsDead) return;
        float now = _clock();
        if (_marks.TryGetValue(enemy, out Mark_ mark))
        {
            mark.ExpireAt = now + Duration; // refresh; keep the existing slow handle
            return;
        }
        var fresh = new Mark_ { ExpireAt = now + Duration, Slow = _slow > 0f ? _slowFactory(enemy) : null };
        fresh.Slow?.Apply(_slow);
        _marks[enemy] = fresh;
    }

    /// <summary>
    /// R10.2: the direct-damage multiplier for <paramref name="enemy"/> — <c>1 + amplify</c> while the
    /// enemy's mark is active, exactly <c>1</c> otherwise (unmarked, expired, dead, or null). Consulted by
    /// the direct-damage pipeline; never writes any asset.
    /// </summary>
    public float AmplifierFor(Actor enemy)
    {
        if (!enemy) return 1f;
        if (_marks.TryGetValue(enemy, out Mark_ mark) && _clock() < mark.ExpireAt) return 1f + _amplify;
        return 1f;
    }

    /// <summary>True while <paramref name="enemy"/> currently carries an active mark. Exposed for the HUD/tests.</summary>
    public bool IsMarked(Actor enemy) =>
        enemy && _marks.TryGetValue(enemy, out Mark_ mark) && _clock() < mark.ExpireAt;

    // R10.3: expire elapsed marks each frame and restore normal movement. Also drops marks whose enemy was
    // destroyed so the registry never holds a dangling reference.
    private void Update() => ExpireElapsed();

    // Pure expiry step (invoked by Update and reusable by tests): any mark whose expiry has passed — or
    // whose enemy is gone/dead — is removed and its slow restored.
    internal void ExpireElapsed()
    {
        if (_marks.Count == 0) return;
        float now = _clock();
        _expired.Clear();
        foreach (var pair in _marks)
            if (!pair.Key || pair.Key.IsDead || now >= pair.Value.ExpireAt) _expired.Add(pair.Key);
        for (int i = 0; i < _expired.Count; i++)
            Remove(_expired[i]);
    }

    private void Remove(Actor enemy)
    {
        if (!_marks.TryGetValue(enemy, out Mark_ mark)) return;
        mark.Slow?.Restore();   // R10.3: restore normal movement when the mark expires
        _marks.Remove(enemy);
    }

    // Clears every mark and restores any pending slow so no mark leaks into a later run.
    private void OnDestroy()
    {
        foreach (var pair in _marks)
            pair.Value.Slow?.Restore();
        _marks.Clear();
    }

    /// <summary>
    /// Production slow handle: captures the enemy's NavMeshAgent base speed, reduces it while marked, and
    /// restores it on <see cref="Restore"/> — the same locomotion channel <c>ChillStatus</c> uses, so the
    /// mark never introduces a parallel movement system. An enemy without an agent is a safe no-op.
    /// </summary>
    private sealed class AgentSlowHandle : ISlowHandle
    {
        private readonly NavMeshAgent _agent;
        private float _baseSpeed;
        private bool _applied;

        public AgentSlowHandle(Actor enemy)
        {
            if (enemy)
                _agent = enemy.GetComponent<NavMeshAgent>() ?? enemy.GetComponentInChildren<NavMeshAgent>();
        }

        public void Apply(float slowFraction)
        {
            if (!_agent || _applied) return;
            _baseSpeed = _agent.speed;
            _agent.speed = _baseSpeed * (1f - Mathf.Clamp01(slowFraction));
            _applied = true;
        }

        public void Restore()
        {
            if (!_agent || !_applied) return;
            _agent.speed = _baseSpeed;
            _applied = false;
        }
    }
}
