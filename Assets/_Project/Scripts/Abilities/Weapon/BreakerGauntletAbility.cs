using UnityEngine;

[CreateAssetMenu(menuName = "Abilities/Weapon/Breaker Gauntlet")]
public sealed class BreakerGauntletAbility : Ability
{
    [SerializeField] private bool _shockSkill;
    [SerializeField] private bool _asuraBurst;
    [SerializeField, Min(0f)] private float _advanceDistance;
    [SerializeField, Min(0.01f)] private float _advanceDuration = 0.2f;
    [SerializeField, Min(0.1f)] private float _animationSpeed = 1.5f;
    [SerializeField] private Color _effectColor = new Color(1f, 0.55f, 0.12f, 0.8f);
    [SerializeField] private AreaHitStep[] _hitSteps;

    public bool ShockSkill => _shockSkill;
    public bool AsuraBurst => _asuraBurst;
    public float AdvanceDistance => _advanceDistance;
    public float AdvanceDuration => _advanceDuration;
    public float AnimationSpeed => _animationSpeed;
    public Color EffectColor => _effectColor;
    public System.Collections.Generic.IReadOnlyList<AreaHitStep> HitSteps => _hitSteps;

    /// <summary>
    /// Effective mana cost of a gauntlet (Manopla) skill: the authored <see cref="Ability.ManaCost"/>
    /// scaled by the global <c>CombatBalanceConfig.SkillManaCostMultiplier</c> (R3.1, R3.5). The
    /// multiplier is scoped to the gauntlet family here — the Arco/Lança families keep their authored
    /// cost so they never regress (R3.6, R7.2). When the config is absent the multiplier falls back to
    /// <c>1f</c>, leaving the authored cost untouched (R5.3).
    ///
    /// This override is the single source of truth for the cost: <see cref="Ability.TryActivate"/>
    /// (the debit via <c>PlayerActor.TrySpendMana</c>), <c>AbilityHolder.CheckUse</c> (the affordability
    /// gate via <c>HasMana</c>) and the HUD all read <c>ManaCost</c>, so check, debit and display can
    /// never desync. The base clamp (negatives collapse to 0) is preserved by scaling
    /// <c>base.ManaCost</c> and re-clamping.
    /// </summary>
    /// <remarks>Feature: combat-balance-tuning, task 4.1. Requirements: 3.1, 3.4, 3.5.</remarks>
    public override float ManaCost
    {
        get
        {
            float multiplier = CombatBalance.Current?.SkillManaCostMultiplier ?? 1f;
            return Mathf.Max(0f, base.ManaCost * multiplier);
        }
    }

    public override bool CanActivate(GameObject parent) => parent &&
        parent.TryGetComponent(out BreakerGauntletCombat combat) && combat.CanUse(this);

    public override void Activate(GameObject parent)
    {
        if (parent && parent.TryGetComponent(out BreakerGauntletCombat combat)) combat.Use(this);
    }
}
