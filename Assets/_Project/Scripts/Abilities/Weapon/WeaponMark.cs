/// <summary>
/// A target a Bow ability can chase. Kept as a minimal, scene-free abstraction so
/// <see cref="WeaponMark"/> stays a pure plain C# class that is property-testable without a live
/// Unity scene. The scene-side Bow coordinator (task 12.4) wraps a concrete enemy
/// (<c>Actor</c>) in an adapter that satisfies this contract, mirroring how the priority-marker
/// legibility is driven by <see cref="PriorityTargetMarker"/> for enemies.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 12.1.
/// Requirements: 10.2, 10.3, 10.8.
/// </remarks>
public interface IMarkTarget
{
    /// <summary>True while the target is a valid thing to chase (typically: alive / not removed).</summary>
    bool IsAlive { get; }
}

/// <summary>
/// Pure, scene-free state model of the Bow's exclusive <b>Mark</b> mechanic (R10.2).
///
/// It holds the current marked target plus the time at which the designation expires, and answers
/// the single question every priority Bow ability needs: <i>where should homing / ricochet /
/// Heavy Bolt priority be directed?</i>
///
/// <list type="bullet">
///   <item><description>
///     <b>When a valid mark is present</b> it reinforces the priority behaviors toward the marked
///     target — but only the behaviors the equipped <see cref="MarkConfig"/> actually enables
///     (R10.3).
///   </description></item>
///   <item><description>
///     <b>When no mark is present</b> (never set, expired, or the target is no longer alive) every
///     priority query is a <i>no-op</i>: it resolves to the ability's default target and the Mark
///     state is left untouched, so activating a priority ability without a mark cannot corrupt or
///     clear a (non-existent) designation (R10.8).
///   </description></item>
/// </list>
///
/// The legibility of the mark — the world-space indicator over the chosen enemy — is <b>reused</b>
/// from <see cref="PriorityTargetMarker"/> by the scene-side coordinator rather than inventing a
/// new indicator (design: "Reusa <c>PriorityTargetMarker</c> para a legibilidade"). This class owns
/// only the data/rules; it never touches Unity types.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 12.1.
/// Requirements: 10.2, 10.3, 10.8; WeaponMark / MarkConfig.
/// </remarks>
public sealed class WeaponMark
{
    private readonly MarkConfig _config;
    private IMarkTarget _target;
    private float _expiresAt;

    /// <param name="config">Validity + reinforcement flags that shape how the mark behaves.</param>
    public WeaponMark(MarkConfig config)
    {
        _config = config;
    }

    /// <summary>The currently marked target, or <c>null</c> when no mark is present.</summary>
    public IMarkTarget Target => _target;

    /// <summary>The time (same clock as <c>Mark</c>/<c>IsMarkPresent</c> callers pass in) at which the mark expires.</summary>
    public float ExpiresAt => _expiresAt;

    /// <summary>
    /// Designates <paramref name="target"/> as the priority target, valid until
    /// <paramref name="now"/> + <see cref="MarkConfig.ValiditySeconds"/>. Marking a null or
    /// already-dead target, or marking under a non-positive validity, clears the mark instead of
    /// setting an unusable one. Re-marking refreshes the expiry.
    /// </summary>
    /// <param name="target">The target to mark.</param>
    /// <param name="now">Current time on the caller's clock.</param>
    public void Mark(IMarkTarget target, float now)
    {
        if (target == null || !target.IsAlive || _config.ValiditySeconds <= 0f)
        {
            Clear();
            return;
        }

        _target = target;
        _expiresAt = now + _config.ValiditySeconds;
    }

    /// <summary>Clears any current mark. Idempotent.</summary>
    public void Clear()
    {
        _target = null;
        _expiresAt = 0f;
    }

    /// <summary>
    /// True when a valid mark is present at <paramref name="now"/>: a target is set, is still
    /// alive, and the designation has not expired. This is the single predicate the priority
    /// resolution and legibility both key off, so the visual (via <see cref="PriorityTargetMarker"/>)
    /// and the gameplay reinforcement can never disagree.
    /// </summary>
    /// <param name="now">Current time on the caller's clock.</param>
    public bool IsMarkPresent(float now)
        => _target != null && _target.IsAlive && now < _expiresAt;

    /// <summary>
    /// Resolves the target a priority behavior should chase, given the ability's default target.
    /// With a valid mark present the behavior is reinforced toward the marked target (R10.3); with
    /// no mark present it is a no-op that returns the default target and leaves the Mark state
    /// untouched (R10.8).
    /// </summary>
    /// <param name="behavior">Which priority behavior is asking.</param>
    /// <param name="defaultTarget">The target the ability would use without any mark.</param>
    /// <param name="now">Current time on the caller's clock.</param>
    /// <returns>The marked target when the mark is present and reinforces <paramref name="behavior"/>; otherwise <paramref name="defaultTarget"/>.</returns>
    public IMarkTarget ResolvePriorityTarget(PriorityBehavior behavior, IMarkTarget defaultTarget, float now)
        => IsBehaviorReinforced(behavior) && IsMarkPresent(now) ? _target : defaultTarget;

    /// <summary>
    /// True when the equipped <see cref="MarkConfig"/> enables reinforcing <paramref name="behavior"/>.
    /// A mark only ever reinforces the behaviors its config turns on; behaviors left off resolve to
    /// the default target even while a mark is present.
    /// </summary>
    public bool IsBehaviorReinforced(PriorityBehavior behavior)
    {
        switch (behavior)
        {
            case PriorityBehavior.Homing: return _config.ReinforceHoming;
            case PriorityBehavior.Ricochet: return _config.ReinforceRicochet;
            case PriorityBehavior.HeavyBolt: return _config.ReinforceHeavyBolt;
            default: return false;
        }
    }
}

/// <summary>The Bow priority behaviors that a live Mark can reinforce (R10.3).</summary>
public enum PriorityBehavior
{
    /// <summary>Projectiles curve toward the marked target.</summary>
    Homing,

    /// <summary>Ricochets prefer the marked target as their next bounce.</summary>
    Ricochet,

    /// <summary>Heavy Bolt prioritizes the marked target.</summary>
    HeavyBolt,
}
