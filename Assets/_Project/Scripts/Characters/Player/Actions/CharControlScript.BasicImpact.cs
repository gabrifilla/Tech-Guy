using UnityEngine;

public partial class CharControlScript
{
    private ActionImpactScheduler _basicScheduler;
    private CombatActionProfile _basicProfile;
    private GauntletImpactContext _basicContext;
    private GauntletEnergyPolicy _basicEnergy;
    private WeaponScript _basicWeapon;
    private SkillAnimationPlayer _basicAnimation;
    private float _basicStartedAt, _basicDuration, _basicRange, _basicScale, _basicDamage, _basicProgress;
    private Vector3 _basicBox;
    private int _basicAttackIndex, _basicContactFrame = -1;
    private bool _basicResolving;

    // Read-only observation of the existing action, consumed by GameplayObservation as well as tests.
    public bool HasScheduledBasic => _basicScheduler != null;
    public float BasicImpactProgress => _basicProgress;
    public float BasicImpactDuration => _basicDuration;
    public float BasicImpactFraction => _basicProfile != null && _basicProfile.ImpactEvents.Count > 0
        ? _basicProfile.ImpactEvents[0].At : 0f;

    private bool BeginGauntletBasicImpact(int attackIndex, float duration, float range, float scale, Vector3 box)
    {
        var profile = weapon.BasicProfile;
        // Unauthored legacy fixtures retain their previous path. The shipped Gauntlet is authored.
        if (profile == null || profile.ImpactEvents == null || profile.ImpactEvents.Count == 0) return false;
        EndGauntletBasicImpact();
        _basicProfile = profile.Copy();
        _basicWeapon = weapon; _basicAttackIndex = attackIndex; _basicDuration = duration;
        _basicRange = range; _basicScale = scale; _basicBox = box; _basicDamage = weapon.attackDamage;
        _basicStartedAt = Time.time; _basicProgress = 0; _basicContactFrame = -1;
        var combat = playerActor.GetComponent<BreakerGauntletCombat>() ?? playerActor.gameObject.AddComponent<BreakerGauntletCombat>();
        combat.Configure(weapon);
        bool finisher = attackAnimations != null && attackAnimations.Length > 0 && attackIndex == attackAnimations.Length - 1;
        _basicContext = combat.BeginBasicExecutionWithFinisher(finisher); _basicEnergy = combat.EnergyPolicy;
        _basicAnimation = GetComponent<SkillAnimationPlayer>() ?? gameObject.AddComponent<SkillAnimationPlayer>();
        _basicScheduler = new ActionImpactScheduler(ExecutionId.Next(), new ExecutionImpactLedger(),
            _basicProfile.BuildTimeline(), _basicProfile.ImpactEvents, EmitGauntletBasicImpact, EmitGauntletBasicImpact);
        SampleGauntletBasicPose(); // Immediate acceptance/anticipation; no damage here.
        return true;
    }

    private bool BasicOwnerIsLive() => playerActor && !playerActor.IsDead && playerActor.isActiveAndEnabled &&
        playerActor.CurrentWeapon == _basicWeapon && !isDashing && !(dashScript && dashScript.IsDashing(gameObject));

    private void FixedUpdate()
    {
        if (_basicScheduler == null) return;
        if (!BasicOwnerIsLive()) { CancelCombo(); return; }
        if (Time.timeScale <= 0 || playerActor.IsCombatHitStopped) return;
        float progress = Mathf.Clamp01((Time.time - _basicStartedAt) / _basicDuration);
        // A coarse fixed tick can cross contact AND recovery. Observe crossed eligible contacts
        // in order before giving the shared scheduler its final progress; ledger owns deduplication.
        var scheduler = _basicScheduler;
        foreach (var impact in _basicProfile.ImpactEvents)
        {
            if (impact.At > _basicProgress && impact.At <= progress)
            { _basicProgress = impact.At; scheduler.Advance(impact.At); }
            if (_basicScheduler != scheduler) return; // Reentrant legitimate cancellation during confirmation.
        }
        _basicProgress = progress;
        scheduler.Advance(progress);
    }

    private void EmitGauntletBasicImpact(ImpactEvent impact)
    {
        if (!BasicOwnerIsLive() || _basicContext == null || !_basicEnergy.IsLive(_basicContext)) return;
        var context = _basicContext; var energy = _basicEnergy;
        _basicContactFrame = Time.frameCount;
        _basicAnimation.Contact(BasicMotion);
        _basicResolving = true;
        try
        {
            float? stop = null;
            var stops = _basicProfile.HitStopProfiles;
            if (impact.HitStopProfileIndex >= 0 && impact.HitStopProfileIndex < stops.Count)
                stop = stops[impact.HitStopProfileIndex].ClampedDuration;
            using (playerActor.RunStatistics?.BeginAction("Basic"))
            using (playerActor.BeginImpactFeedback(stop, context.Execution, impact.Index))
            {
                playerActor.TryApplyGauntletAreaDamage(transform.position, transform.forward,
                    _basicRange * _basicScale, _basicBox * _basicScale, AreaHitShape.Box, 0,
                    attackLayers, _basicDamage, 1, 0, BuildBasicAttackReaction(_basicAttackIndex, _basicRange),
                    true, null, context, energy, impact.Index);
                // Preserve the existing every-third-emission proc, including a committed whiff.
                _runBasicCount++; TryProcComboNova();
            }
        }
        finally
        {
            _basicResolving = false;
            if (_basicContext != context) energy.Close(context); // Cancel during notification cannot revoke committed HP.
        }
    }

    private SkillMotion BasicMotion => _basicAttackIndex % 2 == 1 ? SkillMotion.PunchLeft : SkillMotion.PunchRight;
    private void SampleGauntletBasicPose()
    {
        if (!_basicAnimation || _basicProfile == null || Time.timeScale <= 0) return;
        if (_basicContactFrame == Time.frameCount || _basicContactFrame >= 0 && playerActor.IsCombatHitStopped)
        { _basicAnimation.Contact(BasicMotion); return; }
        if (playerActor.IsCombatHitStopped) return;
        float elapsed = Time.time - _basicStartedAt;
        float contact = _basicProfile.ImpactEvents[0].At * _basicDuration;
        if (_basicContactFrame < 0) elapsed = Mathf.Min(elapsed, contact - .00001f);
        _basicAnimation.Strike(BasicMotion, elapsed, 0, contact, _basicDuration);
    }
    private void LateUpdate()
    {
        SampleDirectMovement();
        if (_basicScheduler == null) return;
        if (!BasicOwnerIsLive()) { CancelCombo(); return; }
        SampleGauntletBasicPose(); _basicAnimation.ReapplyPose();
    }
    private void EndGauntletBasicImpact()
    {
        if (_basicScheduler == null) return;
        _basicScheduler.StopAndCloseWindows(); _basicScheduler = null;
        var context = _basicContext; _basicContext = null;
        if (!_basicResolving && context != null) _basicEnergy.Close(context);
        if (_basicAnimation) _basicAnimation.Release();
    }
}
