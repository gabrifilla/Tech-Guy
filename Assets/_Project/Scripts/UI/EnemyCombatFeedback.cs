using UnityEngine;

/// <summary>Always-readable enemy health and event-driven confirmation of actual damage.</summary>
[RequireComponent(typeof(Actor))]
[DisallowMultipleComponent]
public sealed class EnemyCombatFeedback : MonoBehaviour
{
    [SerializeField] private float _barHeight = 2.5f;
    private Actor _actor;
    private EnemyVariant _variant;
    private CombatReactionController _reaction;
    private Camera _camera;
    private float _damageAt = -10f, _trailingHealth = 1;
    private GUIStyle _label;
    private SectorBoss _boss;
    private float _focusUntil;
    public void ShowFocus() => _focusUntil = Time.time + .25f;
    public Color NameColor => EnemyVisualStyle.NameColor(_variant && _variant.Profile ? _variant.Profile.Rarity : EnemyRarity.Normal, GetComponent<SectorBoss>());
    public float HealthRatio => _actor && _actor.maxHealth > 0 ? Mathf.Clamp01(_actor.health / _actor.maxHealth) : 0;

    private void Awake()
    {
        _actor = GetComponent<Actor>();
        _variant = GetComponent<EnemyVariant>();
        _reaction = GetComponent<CombatReactionController>();
        _boss = GetComponent<SectorBoss>();
        _actor.DamageReceived += OnDamage;
    }
    private void OnDamage(Actor actor, float amount)
    {
        _damageAt = Time.time;
        DamagePopup.Show(transform.position + Vector3.up * (1.5f * transform.lossyScale.y), amount, actor.health <= 0);
    }
    private void OnGUI()
    {
        if (!_actor || _actor.IsDead) return;
        if (!_camera) _camera = Camera.main;
        if (!_camera) return;
        Vector3 screen = _camera.WorldToScreenPoint(transform.position + Vector3.up * (_barHeight * transform.lossyScale.y));
        if (screen.z <= 0 || screen.x < 0 || screen.x > Screen.width || screen.y < 0 || screen.y > Screen.height) return;
        if (Event.current.type == EventType.Repaint)
            _trailingHealth = Time.time - _damageAt < .2f ? Mathf.Max(_trailingHealth, HealthRatio) :
                Mathf.MoveTowards(_trailingHealth, HealthRatio, Time.deltaTime * 1.4f);
        float scale = Mathf.Clamp(Screen.height / 900f, .8f, 1.5f);
        bool focused = Time.time < _focusUntil;
        bool special = _boss || (_variant && _variant.Profile && _variant.Profile.Rarity != EnemyRarity.Normal);
        float barWidth = _boss ? 180 : special ? 76 : 60;
        var bar = new Rect(screen.x-barWidth*.5f*scale, Screen.height-screen.y, barWidth*scale, 5*scale);
        Color old = GUI.color;
        GUI.color = focused ? new Color(1f,.86f,.48f) : special ? NameColor : new Color(.12f,.09f,.08f);
        GUI.DrawTexture(new Rect(bar.x-3,bar.y-3,bar.width+6,bar.height+6),Texture2D.whiteTexture);
        GUI.color = new Color(.015f,.02f,.03f,.95f);
        GUI.DrawTexture(new Rect(bar.x-2,bar.y-2,bar.width+4,bar.height+4),Texture2D.whiteTexture);
        GUI.color = new Color(1,.72f,.22f);
        GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*_trailingHealth,bar.height),Texture2D.whiteTexture);
        GUI.color = Time.time-_damageAt < .12f ? Color.white : new Color(.92f,.16f,.2f);
        GUI.DrawTexture(new Rect(bar.x,bar.y,bar.width*HealthRatio,bar.height),Texture2D.whiteTexture);
        GUI.color = old;

        // Stance bar for tougher enemies: a thin bar just below the health bar. Turns amber when
        // broken (empty), and flashes white while stunned so the player reads the CC window.
        if (_reaction && _reaction.ShouldShowStanceBar)
        {
            float stanceRatio = _reaction.StanceRatio;
            var stanceBar = new Rect(bar.x, bar.y + bar.height + 4f*scale, bar.width, 2f*scale);
            GUI.color = new Color(.015f,.02f,.03f,.95f);
            GUI.DrawTexture(new Rect(stanceBar.x-2,stanceBar.y-2,stanceBar.width+4,stanceBar.height+4),Texture2D.whiteTexture);
            GUI.color = _reaction.IsStunned ? Color.white : new Color(.45f,.7f,1f);
            GUI.DrawTexture(new Rect(stanceBar.x,stanceBar.y,stanceBar.width*stanceRatio,stanceBar.height),Texture2D.whiteTexture);
            GUI.color = old;
        }

        if (!_boss) _boss = GetComponent<SectorBoss>();
        if (!focused && !_boss) return;
        if (_label == null)
        {
            _label = new GUIStyle(GUI.skin.label) { alignment=TextAnchor.MiddleCenter, fontStyle=FontStyle.Bold };
            _label.normal.textColor = Color.white;
        }
        _label.fontSize = Mathf.RoundToInt(12*scale);
        string title = "INIMIGO";
        if (_variant && _variant.Profile)
        {
            title = _variant.Profile.Rarity == EnemyRarity.Rare ? "RARO" : _variant.Profile.Rarity == EnemyRarity.Magic ? "MÁGICO" : "INIMIGO";
            if (!string.IsNullOrEmpty(_variant.AffixSummary)) title += " · " + _variant.AffixSummary;
        }
        if (TryGetComponent(out SectorBoss boss)) title = boss.DisplayName + (boss.IsEnraged ? " · FÚRIA" : " · BOSS");
        _label.normal.textColor = NameColor;
        GUI.Label(new Rect(screen.x-180*scale,bar.y-24*scale,360*scale,22*scale),title,_label);
        _label.normal.textColor = Color.white;
        GUI.Label(new Rect(screen.x-90*scale,bar.y+16*scale,180*scale,21*scale),
            $"{Mathf.CeilToInt(_actor.health)} / {Mathf.CeilToInt(_actor.maxHealth)}",_label);
        if (_boss && _boss.IsTelegraphing)
        {
            _label.normal.textColor = new Color(1f, .75f, .35f);
            GUI.Label(new Rect(screen.x-220*scale,bar.y+38*scale,440*scale,22*scale),
                _boss.CurrentAttackName,_label);
        }
    }
    private void OnDestroy() { if (_actor) _actor.DamageReceived -= OnDamage; }
}
