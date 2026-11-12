using UnityEngine;

/// <summary>
/// When a Grunt or Rush uses its short lunge (Etapa 3, 11/11/2026): the player is just out of melee reach and
/// walking away, and the lunge is off cooldown. Walking away stops being a free escape; the dash still is one.
/// </summary>
public static class LungeRules
{
    public const float MinDistance = 2.5f, MaxDistance = 5f;
    /// <summary>Planar speed (m/s) away from the enemy that counts as "walking away" (player walks at 5).</summary>
    public const float MinAwaySpeed = 1.5f;
    public const float Cooldown = 4f;

    public static bool UsesLunge(ArchetypeId? archetype) => archetype == ArchetypeId.Grunt || archetype == ArchetypeId.Rush;

    /// <summary>
    /// True when a lunge should start: distance in [<see cref="MinDistance"/>, <see cref="MaxDistance"/>], the player
    /// moving away at least <see cref="MinAwaySpeed"/>, and the cooldown elapsed.
    /// </summary>
    public static bool ShouldLunge(Vector3 enemy, Vector3 player, Vector3 playerVelocity, float now, float readyAt)
    {
        if (now < readyAt) return false;
        Vector3 away = player - enemy; away.y = 0f;
        float distance = away.magnitude;
        if (distance < MinDistance || distance > MaxDistance || distance < 1e-4f) return false;
        playerVelocity.y = 0f;
        float awaySpeed = Vector3.Dot(playerVelocity, away / distance);
        return !float.IsNaN(awaySpeed) && awaySpeed >= MinAwaySpeed;
    }
}
