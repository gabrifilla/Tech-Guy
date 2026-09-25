using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class SkillSlotView : MonoBehaviour
{
    [SerializeField] private SkillGlyphGraphic _glyph;
    [SerializeField] private Image _cooldown;
    [SerializeField] private Image _flash;
    [SerializeField] private TMP_Text _key;
    [SerializeField] private TMP_Text _name;
    [SerializeField] private TMP_Text _cost;
    [SerializeField] private TMP_Text _timer;
    private float _flashUntil;
    private Color _flashColor;
    public RectTransform Rect => (RectTransform)transform;
    public string Status { get; private set; }

    public void Present(Ability ability, string key, float remaining, float ratio, bool active, bool hasMana, bool requirement)
    {
        _key.text = key;
        if (!ability)
        {
            _name.text = "VAZIO"; _cost.text = ""; _timer.text = "";
            _glyph.enabled = false; _cooldown.fillAmount = 0f; Status = "Vazio";
            _flash.color = Color.clear;
            return;
        }
        _glyph.enabled = true;
        _glyph.SetKind(ability.Glyph);
        _name.text = ability.DisplayName.ToUpperInvariant();
        _cost.text = ability.ManaCost > 0 ? $"{ability.ManaCost:0} MANA" : "GRATIS";
        _cost.color = hasMana ? new Color(.52f,.73f,.92f) : new Color(1f,.38f,.3f);
        Color accent = ability.AccentColor;
        _glyph.color = remaining > 0 || !hasMana || !requirement ? new Color(accent.r*.35f,accent.g*.35f,accent.b*.35f) : accent;
        _cooldown.fillAmount = ratio;
        _timer.text = remaining > 0 ? remaining.ToString("0.0") : active ? "EM USO" : !hasMana ? "SEM MANA" : !requirement ? "CARREGAR" : "";
        _timer.fontSize = remaining > 0 ? 28 : 11;
        Status = _timer.text;
        if (active && Time.unscaledTime >= _flashUntil)
            _flash.color = new Color(accent.r, accent.g, accent.b, .1f + .07f * Mathf.Sin(Time.unscaledTime*9f));
        else
        {
            float alpha = Mathf.Clamp01((_flashUntil-Time.unscaledTime)/.45f);
            _flash.color = new Color(_flashColor.r,_flashColor.g,_flashColor.b,alpha*.55f);
        }
    }

    public void Flash(bool success)
    {
        _flashUntil = Time.unscaledTime + .45f;
        _flashColor = success ? new Color(1f,.81f,.42f) : new Color(1f,.17f,.12f);
    }
}
