using UnityEngine;

public class Ability : ScriptableObject
{
    public new string name;
    public float cooldownTime;
    public float activeTime;
    [SerializeField, Min(0f)] private float _manaCost;
    [SerializeField] private string _displayName;
    [SerializeField] private SkillGlyphKind _glyph;
    [SerializeField] private Color _accentColor = new Color(0.85f, 0.64f, 0.33f);

    [Header("Combat")]
    [SerializeField] private CombatActionProfile _combatProfile;

    public virtual float ManaCost => Mathf.Max(0f, _manaCost);
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public SkillGlyphKind Glyph => _glyph;
    public Color AccentColor => _accentColor;

    /// <summary>
    /// The authored <see cref="CombatActionProfile"/> for this Ação_Ofensiva (R2.3). May be
    /// <c>null</c> on older assets authored before the profile existed; callers must null-guard.
    /// </summary>
    public CombatActionProfile CombatProfile => _combatProfile;
    public void ConfigureRunPresentation(string title, float manaCost, SkillGlyphKind glyph)
    {
        _displayName = title; _manaCost = manaCost; _glyph = glyph;
    }

    public bool TryActivate(GameObject parent)
    {
        if (!CanActivate(parent) || !parent.TryGetComponent(out PlayerActor actor) ||
            !actor.TrySpendMana(ManaCost)) return false;
        Activate(parent);
        return true;
    }

    public virtual bool CanActivate(GameObject parent) => parent != null;

    public virtual void Activate(GameObject parent)
    {
    }

    /// <summary>
    /// Validates the authored <see cref="CombatProfile"/> in the Inspector without ever mutating it
    /// (R2.3, R3.10, R4.8, R8.7). Invalid phase boundaries or cancel windows are flagged via
    /// <see cref="Debug.LogWarning(object)"/> and the last valid authored values are kept (no silent
    /// mutation). An absent commitment category resolves to <c>Committed</c> with a warning through
    /// the shared <see cref="CombatActionProfile.ResolveCommitment"/> path. A <c>null</c> profile on
    /// older assets is skipped gracefully.
    /// </summary>
    protected virtual void OnValidate()
    {
        CombatProfileValidation.Validate(_combatProfile, name);
    }
}
