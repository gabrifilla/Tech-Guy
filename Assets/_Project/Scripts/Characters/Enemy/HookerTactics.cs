using UnityEngine;

/// <summary>
/// Pure decision model for the Hooker redo (Etapa 2, 11/11/2026). The hook is a visible projectile with a
/// cooldown plus a random hold, so it never fires the instant it recharges; while it recharges the Hooker
/// fights at mid range with chain strikes (a straight thrust, and a sweep when the player is close).
/// <see cref="EnemyAI"/> owns the clock and calls into this model; nothing here touches the scene.
/// </summary>
public sealed class HookerCadence
{
    /// <summary>Maximum hook reach (meters), measured from the Hooker.</summary>
    public const float HookRange = 12f;
    /// <summary>Chain thrust reach: a straight lane in front of the Hooker. Matches its 6 m preferred distance.</summary>
    public const float ThrustRange = 6.5f;
    /// <summary>Below this distance the Hooker sweeps instead of thrusting, and never wastes a hook.</summary>
    public const float SweepRange = 4f;
    /// <summary>Hook recharge after a throw.</summary>
    public const float HookCooldown = 5f;
    /// <summary>Extra random hold after the recharge (0..this), so the hook is not on a metronome.</summary>
    public const float HookRandomHoldMax = 2.5f;
    /// <summary>The first hook of an encounter comes after a short random delay, not on the first frame.</summary>
    public const float FirstHookDelayMin = .5f, FirstHookDelayMax = 2.5f;

    private float _readyAt;
    private bool _initialized;

    /// <summary>Time (same clock as the caller) at which the hook may next be thrown.</summary>
    public float ReadyAt => _readyAt;

    /// <summary>Schedules the first hook once; later calls are no-ops.</summary>
    public void EnsureInitialized(float now, System.Random rng)
    {
        if (_initialized) return;
        _initialized = true;
        _readyAt = now + Mathf.Lerp(FirstHookDelayMin, FirstHookDelayMax, Sample(rng));
    }

    public bool HookReady(float now) => _initialized && now >= _readyAt;

    /// <summary>A hook left the hand: recharge plus a random hold in [0, <see cref="HookRandomHoldMax"/>].</summary>
    public void OnHookThrown(float now, System.Random rng)
    {
        _initialized = true;
        _readyAt = now + HookCooldown + HookRandomHoldMax * Sample(rng);
    }

    /// <summary>
    /// Picks the Hooker's next attack. The hook only goes out when ready, possible (clear line and the target
    /// can be pulled right now) and the player is between <see cref="SweepRange"/> and <see cref="HookRange"/>.
    /// Otherwise chain strikes cover the mid range. Null means "nothing in reach: keep positioning".
    /// </summary>
    public static EnemyAttackKind? Choose(bool hookReady, bool hookPossible, float distance)
    {
        if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f) return null;
        if (hookReady && hookPossible && distance > SweepRange && distance <= HookRange) return EnemyAttackKind.HookLine;
        if (distance <= SweepRange) return EnemyAttackKind.ChainSweep;
        if (distance <= ThrustRange) return EnemyAttackKind.ChainThrust;
        return null;
    }

    private static float Sample(System.Random rng) => rng != null ? (float)rng.NextDouble() : .5f;
}

/// <summary>
/// Where the Hooker wants to stand so a connected hook drags the player into danger: just behind its allies
/// (the player lands among them) or at a danger zone (the player lands inside it). Pure geometry; the
/// <see cref="PreferredDistanceLayer"/> finds the anchor and <see cref="EnemyAI"/> still owns the movement.
/// </summary>
public static class HookerPositioning
{
    /// <summary>How far behind the ally group's center the Hooker stands (player lands ~1 m in front of him).</summary>
    public const float BehindAllies = 1.5f;
    /// <summary>How far past a hazard's center the Hooker stands (the player ends inside the zone).</summary>
    public const float BehindHazard = .5f;
    /// <summary>Allies or hazards farther than this from the Hooker are not used as an anchor.</summary>
    public const float AnchorSearchRadius = 9f;
    /// <summary>Never choose a spot from which the hook could not reach the player.</summary>
    public const float MaxDistanceFromPlayer = HookerCadence.HookRange - 1.5f;
    /// <summary>Never stand closer than this to the player because of an anchor (that is sweep range).</summary>
    public const float MinDistanceFromPlayer = HookerCadence.SweepRange + .5f;

    /// <summary>
    /// The point on the far side of <paramref name="anchor"/>, as seen from the player, at
    /// <paramref name="behind"/> meters past it, kept inside the hook's reach band. Y comes from <paramref name="self"/>.
    /// </summary>
    public static Vector3 BehindAnchor(Vector3 player, Vector3 anchor, Vector3 self, float behind)
    {
        Vector3 away = anchor - player; away.y = 0f;
        float distance = away.magnitude;
        Vector3 direction;
        if (distance > .01f) direction = away / distance;
        else
        {
            direction = self - player; direction.y = 0f;
            direction = direction.sqrMagnitude > .0001f ? direction.normalized : Vector3.forward;
        }
        float wanted = Mathf.Clamp(distance + Mathf.Max(0f, behind), MinDistanceFromPlayer, MaxDistanceFromPlayer);
        Vector3 target = player + direction * wanted;
        target.y = self.y;
        return target;
    }
}
