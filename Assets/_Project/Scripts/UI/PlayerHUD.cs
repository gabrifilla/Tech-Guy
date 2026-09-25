using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>One scene-owned prefab, explicitly bound to that scene's player.</summary>
public sealed class PlayerHUD : MonoBehaviour
{
    [SerializeField] private PlayerActor _player;
    [SerializeField] private RectTransform _dock;
    [SerializeField] private Image _healthFill;
    [SerializeField] private Image _manaFill;
    [SerializeField] private TMP_Text _healthValue;
    [SerializeField] private TMP_Text _manaValue;
    [SerializeField] private TMP_Text _feedback;
    [SerializeField] private TMP_Text _tooltip;
    [SerializeField] private TMP_Text _asuraLabel;
    [SerializeField] private Image _asuraFill;
    [SerializeField] private GameObject _asuraRoot;
    [SerializeField] private SkillSlotView[] _slots;
    private AbilityHolder _holder;
    private CharControlScript _controls;
    private BreakerGauntletCombat _breaker;
    private float _feedbackUntil;
    private float _nextRefresh;
    private bool _bound;

    public PlayerActor Player => _player;
    public Image HealthFill => _healthFill;
    public Image ManaFill => _manaFill;
    public string FeedbackText => _feedback.text;
    public SkillSlotView[] Slots => _slots;
    public void Configure(PlayerActor player) => _player = player;

    private void Start()
    {
        if (!_player || !_player.TryGetComponent(out _holder) || !_dock || _slots == null || _slots.Length != 5)
        {
            Debug.LogError("PlayerHUD requires a player, AbilityHolder and five bound slots.", this);
            enabled = false;
            return;
        }
        _controls = _player.GetComponent<CharControlScript>();
        _player.healthBar = _healthFill;
        _player.manaBar = _manaFill;
        _holder.AbilityUsed += OnUsed;
        _holder.AbilityRejected += OnRejected;
        _player.HealthChanged += OnHealthChanged;
        _player.ManaChanged += OnManaChanged;
        _bound = true;
        RefreshResources();
    }

    private void OnDestroy()
    {
        if (!_bound) return;
        if (_holder) { _holder.AbilityUsed -= OnUsed; _holder.AbilityRejected -= OnRejected; }
        if (_player) { _player.HealthChanged -= OnHealthChanged; _player.ManaChanged -= OnManaChanged; }
    }

    public bool BlocksPointer(Vector2 position) => isActiveAndEnabled && _dock &&
        RectTransformUtility.RectangleContainsScreenPoint(_dock, position, null);

    private void OnHealthChanged(Actor actor) => RefreshResources();
    private void OnManaChanged(PlayerActor actor) => RefreshResources();
    private void RefreshResources()
    {
        if (!_player) return;
        _healthFill.fillAmount = Mathf.Clamp01(_player.health / Mathf.Max(1f,_player.maxHealth));
        _manaFill.fillAmount = Mathf.Clamp01(_player.mana / Mathf.Max(1f,_player.maxMana));
        _healthValue.text = $"{Mathf.CeilToInt(_player.health)} / {Mathf.CeilToInt(_player.maxHealth)}";
        _manaValue.text = $"{Mathf.CeilToInt(_player.mana)} / {Mathf.CeilToInt(_player.maxMana)}";
    }

    private void Update()
    {
        if (!_bound || !_player || !_holder) return;
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + .033f;
        if (!_breaker) _breaker = _player.GetComponent<BreakerGauntletCombat>();
        bool hasAsura = _breaker && _breaker.IsEquipped;
        _asuraRoot.SetActive(hasAsura);
        if (hasAsura)
        {
            _asuraFill.fillAmount = _breaker.Energy/100f;
            _asuraLabel.text = _breaker.IsReady ? "ASURA PRONTO" : $"ASURA   {_breaker.Energy} / 100";
        }
        Vector2 pointer = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) pointer = Mouse.current.position.ReadValue();
#else
        pointer = Input.mousePosition;
#endif
        _tooltip.text = "";
        for (int i = 0; i < _slots.Length; i++)
        {
            Ability ability = i < 4 ? (i < _holder.ActiveAbilities.Count ? _holder.ActiveAbilities[i] : null) : _controls ? _controls.dashScript : null;
            float remaining = i < 4 ? _holder.GetRemainingCooldown(i) : ability is DashScript dash ? dash.GetRemainingCooldown(_player.gameObject) : 0f;
            float ratio = i < 4 ? _holder.GetCooldownRatio(i) : remaining / Mathf.Max(.001f, ability ? ability.cooldownTime * _player.Stats.CooldownMultiplier : 1f);
            bool active = i < 4 ? _holder.IsAbilityActive(i) : ability is DashScript move && move.IsDashing(_player.gameObject);
            bool requirement = !(ability is BreakerGauntletAbility breakerAbility) || !breakerAbility.AsuraBurst || (hasAsura && _breaker.IsReady) || active;
            _slots[i].Present(ability, i < 4 ? _holder.GetAbilityKey(i).ToString() : "SPACE", remaining, ratio,
                active, ability && _player.HasMana(ability.ManaCost), requirement);
            if (ability && RectTransformUtility.RectangleContainsScreenPoint(_slots[i].Rect, pointer, null))
                _tooltip.text = $"{ability.name}  ·  {ability.ManaCost:0} mana  ·  {ability.cooldownTime:0.#}s recarga";
        }
        if (Time.unscaledTime >= _feedbackUntil) _feedback.text = "";
    }

    private void OnUsed(int index)
    {
        if (index < 0 || index >= _slots.Length) return;
        _slots[index].Flash(true);
        Ability ability = index < 4 ? _holder.ActiveAbilities[index] : _controls.dashScript;
        ShowFeedback(ability.ManaCost > 0 ? $"{ability.name}  -{ability.ManaCost:0} mana" : $"{ability.name}  |  Gratis", new Color(.94f,.78f,.45f));
    }

    private void OnRejected(int index, AbilityUseFailure reason)
    {
        if (index >= 0 && index < _slots.Length) _slots[index].Flash(false);
        string message = reason == AbilityUseFailure.NotEnoughMana ? "MANA INSUFICIENTE" :
            reason == AbilityUseFailure.Cooldown ? "HABILIDADE EM RECARGA" :
            reason == AbilityUseFailure.Busy ? "AGUARDE O GOLPE ATUAL" :
            reason == AbilityUseFailure.Interaction ? "INTERAGINDO COM O TERMINAL" :
            reason == AbilityUseFailure.Requirement ? "CARREGUE A ENERGIA ASURA" : "HABILIDADE INDISPONIVEL";
        ShowFeedback(message, new Color(1f,.45f,.33f));
    }

    private void ShowFeedback(string message, Color color)
    {
        _feedback.text = message;
        _feedback.color = color;
        _feedbackUntil = Time.unscaledTime + 1.6f;
    }
}
