using System.Collections.Generic;
using UnityEngine;

public sealed partial class BreakerGauntletCombat
{
    private sealed class EclipseMark
    {
        public Actor Target;
        public long Generation, SourceEpoch, SourceRoot, LaunchSequence, ProtocolRevision;
        public CombatReactionController Reaction;
        public EclipseMarkState State;
        public EclipseCombatSettings Settings;
        public ProtocolExecution Reservation;
        public float Damage, Posture, CueRemaining = .4f;
        public int ArmedFrame, LandingObservedFrame = -1;
        public bool LandingConfirmed, TitanPending;
        public float TitanDelay;
    }
    private readonly Dictionary<Actor, EclipseMark> _eclipseMarks = new Dictionary<Actor, EclipseMark>();
    private readonly List<Actor> _eclipseRemovals = new List<Actor>(64);
    private readonly List<EclipseMark> _titanReady = new List<EclipseMark>(64);
    internal int EclipseMarkCount => _eclipseMarks.Count;
    internal int EclipseFallsEmitted { get; private set; }

    /// <summary>Called after native HP/posture commits, before public notifications. Never inferred from proximity.</summary>
    internal void ConfirmEclipseImpact(GauntletImpactContext context, Actor target, float rolledDamage, float posture, long launchBefore)
    {
        if (context == null || context.Origin != GauntletImpactOrigin.Direct || context.Snapshot == null ||
            !target || target.IsDead || target.Affiliation != CombatAffiliation.Enemy || !target.isActiveAndEnabled ||
            target.CombatGeneration != context.TargetGeneration || !_contexts.IsValid(context)) return;
        if (!_player.TryGetComponent(out ProtocolRuntime protocols) || !protocols.IsOpen) return;
        var snapshot = context.Snapshot;
        if (snapshot.Mutation == GauntletMutation.EclipseUppercut)
        {
            if (_eclipseMarks.TryGetValue(target, out var previous))
            {
                // An armed wave is single-use; another Q cannot renew or multiply its reservation.
                if (previous.Generation == target.CombatGeneration && previous.State.IsArmed) return;
                previous.Reservation?.Dispose(); _eclipseMarks.Remove(target);
            }
            if (_eclipseMarks.Count >= snapshot.Eclipse.MaxMarks) return;
            target.TryGetComponent(out CombatReactionController reaction);
            _eclipseMarks.Add(target, new EclipseMark { Target = target, Generation = target.CombatGeneration,
                SourceEpoch = context.Epoch, SourceRoot = context.RootExecution, Reaction = reaction,
                LaunchSequence = reaction && reaction.IsAirborne ? reaction.LaunchSequence : 0,
                ProtocolRevision = protocols.LifecycleRevision, Settings = snapshot.Eclipse,
                State = new EclipseMarkState(snapshot.Eclipse.MarkSeconds) });
            EclipseMarkVfx.Show(protocols, target.transform.position);
            return;
        }
        bool finisher = context.Slot == GauntletActionSlot.Basic ? snapshot.BasicFinisher :
            context.ImpactIndex == snapshot.LastDirectProducer;
        if (!snapshot.ViolentFall || !finisher || !_eclipseMarks.TryGetValue(target, out var mark) ||
            mark.Generation != target.CombatGeneration || mark.State.IsArmed || mark.State.Remaining <= 0 ||
            mark.ProtocolRevision != protocols.LifecycleRevision ||
            mark.SourceEpoch == context.Epoch && mark.SourceRoot == context.RootExecution) return;
        if (mark.LaunchSequence != 0 && (!mark.Reaction || !mark.Reaction.IsAirborne ||
            mark.Reaction.LaunchSequence != mark.LaunchSequence && launchBefore != mark.LaunchSequence)) return;
        var root = protocols.FindRoot(context);
        if (root == null) return;
        ProtocolExecution reservation;
        using (protocols.OpenCallbackScope(root, 0)) reservation = protocols.ReserveExecution(ProtocolProducer.Technique);
        if (!reservation.IsValid) { reservation.Dispose(); return; }
        if (!mark.State.TryArm(true, false)) { reservation.Dispose(); return; }
        // A native breaking finisher may replace its own live Launch; wait for that actual token, never a virtual fall.
        if (mark.LaunchSequence != 0) mark.LaunchSequence = mark.Reaction.LaunchSequence;
        mark.Reservation = reservation; mark.ArmedFrame = Time.frameCount;
        mark.Damage = rolledDamage * mark.Settings.FallDamageFraction;
        mark.Posture = posture * mark.Settings.FallPostureFraction;
        mark.TitanPending = snapshot.TitanFall; mark.TitanDelay = .12f;
        if (mark.TitanPending) mark.Damage *= .5f;
    }

    private void TickEclipse()
    {
        if (_eclipseMarks.Count == 0) return;
        if (!_player || _player.IsDead || !_weapon || _player.CurrentWeapon != _weapon ||
            !_player.TryGetComponent(out ProtocolRuntime protocols) || !protocols.IsOpen)
        { ClearEclipse(); return; }
        if (_holder.IsGamePaused || _player.IsCombatHitStopped ||
            _player.TryGetComponent(out RunBoons boons) && boons.IsChoosing) return;
        float delta = protocols.ScaledDelta;
        // A hitstop may end after ProtocolRuntime already froze this frame. Keep reservations until its clock resumes.
        if (delta <= 0f) return;
        _eclipseRemovals.Clear(); _titanReady.Clear();
        foreach (var pair in _eclipseMarks)
        {
            var mark = pair.Value; var target = mark.Target;
            if (!target || target.IsDead || !target.isActiveAndEnabled || target.CombatGeneration != mark.Generation ||
                mark.ProtocolRevision != protocols.LifecycleRevision ||
                mark.Reservation != null && (!mark.Reservation.IsValid || !mark.Reservation.IsLifecycleCurrent))
            { _eclipseRemovals.Add(pair.Key); continue; }
            mark.State.Advance(delta);
            mark.CueRemaining -= delta;
            if (mark.CueRemaining <= 0) { EclipseMarkVfx.Show(protocols, target.transform.position, mark.State.IsArmed); mark.CueRemaining = .4f; }
            if (!mark.State.IsArmed)
            {
                if (mark.State.Remaining <= 0 || mark.LaunchSequence != 0 &&
                    (!mark.Reaction || !mark.Reaction.IsAirborne || mark.Reaction.LaunchSequence != mark.LaunchSequence))
                    _eclipseRemovals.Add(pair.Key);
                continue;
            }
            if (Time.frameCount <= mark.ArmedFrame) continue;
            if (mark.TitanPending)
            {
                mark.TitanDelay -= delta;
                if (mark.TitanDelay > 0f) continue;
                mark.TitanPending = false; _titanReady.Add(mark);
            }
            if (mark.LaunchSequence != 0)
            {
                if (!mark.Reaction || mark.Reaction.LaunchSequence != mark.LaunchSequence)
                { _eclipseRemovals.Add(pair.Key); continue; }
                if (mark.Reaction.IsAirborne) continue;
                if (mark.Reaction.LandedLaunchSequence != mark.LaunchSequence)
                {
                    // Controller and player LateUpdate order is unspecified; allow its pose restoration to finish.
                    if (mark.LandingObservedFrame < 0) mark.LandingObservedFrame = Time.frameCount;
                    else if (Time.frameCount > mark.LandingObservedFrame) _eclipseRemovals.Add(pair.Key);
                    continue;
                }
            }
            // Confirm every ready landing before any wave can apply posture to its neighbors.
            mark.LandingConfirmed = true;
            _eclipseRemovals.Add(pair.Key);
        }
        var descending = _titanReady.ToArray(); _titanReady.Clear();
        foreach (var mark in descending)
        {
            if (mark.Target && !mark.Target.IsDead && mark.Target.CombatGeneration == mark.Generation &&
                _eclipseMarks.TryGetValue(mark.Target, out var current) && ReferenceEquals(current, mark) && mark.Reservation != null)
                // Preserve the live Launch: the descending HP hit leaves all posture to the landing wave.
                protocols.EmitBoonTarget(mark.Reservation, mark.Target, mark.Generation, mark.Damage);
        }
        var resolving = _eclipseRemovals.ToArray(); _eclipseRemovals.Clear();
        foreach (var target in resolving)
        {
            if (!_eclipseMarks.TryGetValue(target, out var mark)) continue;
            _eclipseMarks.Remove(target);
            try
            {
                if (mark.State.IsArmed && mark.LandingConfirmed && mark.Target && !mark.Target.IsDead && mark.Target.isActiveAndEnabled &&
                    mark.Target.CombatGeneration == mark.Generation && mark.Reservation != null &&
                    mark.Reservation.IsValid && mark.Reservation.IsLifecycleCurrent)
                {
                    Vector3 point = mark.Target.transform.position;
                    if (protocols.EmitEclipseFall(mark.Reservation, point, mark.Settings.FallRadius, mark.Damage, mark.Posture) > 0)
                    {
                        EclipseFallsEmitted++;
                        // ProtocolFeedbackPool owns the bounded impact cue.
                    }
                }
            }
            finally { mark.Reservation?.Dispose(); }
        }
        _eclipseRemovals.Clear();
    }

    internal void ClearEclipse()
    {
        foreach (var mark in _eclipseMarks.Values) mark.Reservation?.Dispose();
        _eclipseMarks.Clear(); _eclipseRemovals.Clear(); _titanReady.Clear();
    }
}
