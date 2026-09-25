using UnityEngine;

public enum ArsenalSkillKind { Arrow, Volley, Rain, Thrust, Sweep }

[CreateAssetMenu(menuName = "Abilities/Weapon/Arsenal Skill")]
public sealed class ArsenalAbility : Ability
{
    [SerializeField] private ArsenalSkillKind _kind;
    [SerializeField, Min(0f)] private float _windup = 0.2f;
    [SerializeField, Min(1)] private int _hits = 1;
    [SerializeField, Min(0.05f)] private float _interval = 0.15f;
    [SerializeField, Min(0.1f)] private float _range = 12f;
    [SerializeField, Min(0.1f)] private float _width = 1f;
    [SerializeField, Min(0.1f)] private float _damageMultiplier = 1f;
    [SerializeField] private bool _piercing;
    [SerializeField, TextArea] private string _description;

    public ArsenalSkillKind Kind => _kind;
    public float Windup => _windup;
    public int Hits => _hits;
    public float Interval => _interval;
    public float Range => _range;
    public float Width => _width;
    public float DamageMultiplier => _damageMultiplier;
    public bool Piercing => _piercing;
    public string Description => _description;

    // Called only on an owned, runtime-created skill; shared assets are never mutated.
    public void ConfigureRunTransformation(bool bow)
    {
        name = bow ? "Disparo prismático" : "Nova de impacto";
        _kind = bow ? ArsenalSkillKind.Volley : ArsenalSkillKind.Sweep;
        _description = bow ? "Q dispara cinco flechas perfurantes em leque." : "Q explode ao redor do personagem, em vez de avançar ou estocar.";
        _windup = .2f; _hits = 1; _interval = .2f;
        _range = bow ? 16 : 4; _width = 4;
        _damageMultiplier = bow ? 1.3f : 2.5f;
        _piercing = true;
        cooldownTime = 5; activeTime = .4f;
        ConfigureRunPresentation(name, 80, bow ? SkillGlyphKind.Arrow : SkillGlyphKind.Shock);
    }

    public override bool CanActivate(GameObject parent) => parent &&
        parent.TryGetComponent(out ArsenalCombat combat) && combat.CanUse(this);

    public override void Activate(GameObject parent) => parent.GetComponent<ArsenalCombat>().Use(this);
}
