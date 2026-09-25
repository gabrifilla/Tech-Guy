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

    public float ManaCost => Mathf.Max(0f, _manaCost);
    public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
    public SkillGlyphKind Glyph => _glyph;
    public Color AccentColor => _accentColor;

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
}
