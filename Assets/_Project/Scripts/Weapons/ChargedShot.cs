/// <summary>
/// Pure decision helper for the Charged Shot bow boon (R3). Contains no Unity types so it can be
/// unit/property tested scene-free. All timing/damage rules live here; <c>CharControlScript</c>
/// only tracks the held duration and consumes these decisions when a bow basic resolves into an arrow.
/// </summary>
public static class ChargedShot
{
    /// <summary>Seconds the basic-attack input must be held before the shot becomes charged (R3.1).</summary>
    public const float ChargeTime = .6f;

    /// <summary>
    /// True once the input has been held for at least <see cref="ChargeTime"/> (R3.1/R3.3).
    /// A release before this threshold fires the normal arrow.
    /// </summary>
    public static bool IsCharged(float heldSeconds) => heldSeconds >= ChargeTime;

    /// <summary>
    /// Damage multiplier applied to a charged arrow at the given boon rank (R3.2): 1 + 0.75 * rank.
    /// Rank 0 (boon not owned) yields 1, i.e. no bonus.
    /// </summary>
    public static float DamageMultiplier(int rank) => 1f + .75f * rank;
}
