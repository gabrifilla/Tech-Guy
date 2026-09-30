using UnityEngine;

/// <summary>
/// Pure, scene-independent bounds for the combat-balance knobs of the combat-balance-tuning feature
/// (Requisito 5.4).
///
/// The <c>CombatBalanceConfig</c> ScriptableObject exposes tuning values that can be edited freely in
/// the Inspector or left empty; each of its getters reads through this class so a degenerate value
/// (negative radius, zero-sized hitbox, a mana multiplier of 0, non-positive health, negative armor)
/// can never reach the combat systems. The ranges are fixed and data-driven, mirroring the design's
/// clamp table:
/// <list type="bullet">
///   <item>projectile sweep radius lives in [0.05; 1.0] m (Requisito 1);</item>
///   <item>basic-hitbox dimensions (box size components and reach) must stay strictly &gt; 0 (Requisito 2);</item>
///   <item>the skill mana-cost multiplier lives in [0.1; 2.0] (Requisito 3);</item>
///   <item>starting base health must stay strictly &gt; 0 (Requisito 4);</item>
///   <item>starting armor must stay &gt;= 0 (Requisito 4).</item>
/// </list>
///
/// This is a static class with no <see cref="MonoBehaviour"/> dependency so the clamps can be
/// exercised without a scene, per the project rules and the testing strategy in the design
/// (property tests in the style of <c>StanceBreakBounds</c>/<c>DisplacementTierBoundsPropertyTests</c>).
/// </summary>
public static class CombatBalanceBounds
{
    /// <summary>Smallest projectile sweep radius, in metres (Requisito 1).</summary>
    public const float MinProjectileSweepRadius = 0.05f;

    /// <summary>Largest projectile sweep radius, in metres, to keep arrows from tunnelling through walls (Requisito 1.5).</summary>
    public const float MaxProjectileSweepRadius = 1f;

    /// <summary>Smallest positive value any hitbox dimension may take (Requisito 2). Dimensions must stay strictly &gt; 0.</summary>
    public const float MinHitboxDimension = 0.0001f;

    /// <summary>Smallest skill mana-cost multiplier (Requisito 3).</summary>
    public const float MinSkillManaCostMultiplier = 0.1f;

    /// <summary>Largest skill mana-cost multiplier (Requisito 3).</summary>
    public const float MaxSkillManaCostMultiplier = 2f;

    /// <summary>Smallest positive base health the player may start with (Requisito 4). Health must stay strictly &gt; 0.</summary>
    public const float MinBaseHealth = 0.0001f;

    /// <summary>Smallest armor value; armor never goes negative (Requisito 4).</summary>
    public const float MinArmor = 0f;

    /// <summary>
    /// Clamps a projectile sweep radius to [0.05; 1.0] m (Requisito 1). Values outside the range are
    /// pulled to the nearest bound so the <c>SphereCastAll</c> radius can never collapse to a point
    /// nor grow large enough to punch through solid scenery.
    /// </summary>
    public static float ClampProjectileSweepRadius(float radius)
        => Mathf.Clamp(radius, MinProjectileSweepRadius, MaxProjectileSweepRadius);

    /// <summary>
    /// Clamps a single hitbox dimension to be strictly positive (Requisito 2). Non-positive and
    /// non-finite inputs are pulled up to <see cref="MinHitboxDimension"/> so the basic-attack volume
    /// always has a real extent.
    /// </summary>
    public static float ClampHitboxDimension(float dimension)
    {
        if (float.IsNaN(dimension) || dimension < MinHitboxDimension)
        {
            return MinHitboxDimension;
        }

        return dimension;
    }

    /// <summary>
    /// Clamps each component of a hitbox box size to be strictly positive (Requisito 2), reusing
    /// <see cref="ClampHitboxDimension"/> per axis so no axis can be zero or negative.
    /// </summary>
    public static Vector3 ClampHitboxSize(Vector3 size) => new Vector3(
        ClampHitboxDimension(size.x),
        ClampHitboxDimension(size.y),
        ClampHitboxDimension(size.z));

    /// <summary>
    /// Clamps a skill mana-cost multiplier to [0.1; 2.0] (Requisito 3). Keeps the global override from
    /// zeroing skill costs or making them punitive.
    /// </summary>
    public static float ClampSkillManaCostMultiplier(float multiplier)
        => Mathf.Clamp(multiplier, MinSkillManaCostMultiplier, MaxSkillManaCostMultiplier);

    /// <summary>
    /// Clamps a starting base-health override to be strictly positive (Requisito 4.4). Non-positive
    /// and non-finite inputs are pulled up to <see cref="MinBaseHealth"/> so the player never spawns
    /// with a degenerate (&lt;= 0) health pool.
    /// </summary>
    public static float ClampBaseHealth(float health)
    {
        if (float.IsNaN(health) || health < MinBaseHealth)
        {
            return MinBaseHealth;
        }

        return health;
    }

    /// <summary>
    /// Clamps a starting armor override to be non-negative (Requisito 4), keeping the mitigation
    /// formula <c>amount*100/(100+armor)</c> well defined and never amplifying incoming damage.
    /// </summary>
    public static float ClampArmor(float armor)
    {
        if (float.IsNaN(armor) || armor < MinArmor)
        {
            return MinArmor;
        }

        return armor;
    }
}
