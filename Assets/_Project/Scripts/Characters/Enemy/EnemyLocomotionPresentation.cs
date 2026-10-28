/// <summary>Small speed hysteresis for existing Idle/Walk clips, without delaying mechanical movement.</summary>
public static class EnemyLocomotionPresentation
{
    public const float EnterWalkSpeed = .08f;
    public const float ExitWalkSpeed = .03f;
    public const float TransitionSeconds = .08f;
    public static bool WantsWalk(float speed, bool wasWalking)
    {
        if (float.IsNaN(speed) || float.IsInfinity(speed) || speed < 0f) return false;
        return speed > (wasWalking ? ExitWalkSpeed : EnterWalkSpeed);
    }
}
