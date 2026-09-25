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

    public override bool CanActivate(GameObject parent) => parent &&
        parent.TryGetComponent(out BreakerGauntletCombat combat) && combat.CanUse(this);

    public override void Activate(GameObject parent)
    {
        if (parent && parent.TryGetComponent(out BreakerGauntletCombat combat)) combat.Use(this);
    }
}
