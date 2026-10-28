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
    [SerializeField] private LobbyInteraction _lobby;
    [SerializeField] private Transform _lobbyTrainingAnchor;
    [SerializeField, Min(1f)] private float _lobbyTrainingRange = 4.5f;
    private Canvas _canvas;
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
    private BowBoonCombat _bowBoons;
    private SpearBoonCombat _spearBoons;
    private float _feedbackUntil;
    private float _nextRefresh;
    private bool _bound;
    private bool _initialized;
    private GauntletEnergyPolicy _observedEnergyPolicy;
    private bool _gauntletFeedback;
    private string _gauntletFeedbackMessage;
    private int _gainFrame = -1, _gainEffective, _gainSources;
    private long _gainRoot, _gainEpoch;

    // Dirty-tracking caches (R2.1): rebuild/reassign a TMP only when its source value changes, so an
    // unchanged frame allocates no interpolated string. Formats are preserved via HudFormatter (R2.6).
    private readonly UiValueCache<string> _healthCache = new UiValueCache<string>();
    private readonly UiValueCache<string> _manaCache = new UiValueCache<string>();
    private readonly UiValueCache<string> _asuraCache = new UiValueCache<string>();
    private readonly UiValueCache<string> _tooltipCache = new UiValueCache<string>();
    private readonly UiValueCache<string> _feedbackCache = new UiValueCache<string>();

    public PlayerActor Player => _player;
    public Image HealthFill => _healthFill;
    public Image ManaFill => _manaFill;
    public string FeedbackText => _feedback.text;
    public SkillSlotView[] Slots => _slots;
    public string AsuraText => _asuraLabel ? _asuraLabel.text : "";
    public bool IsAsuraVisible => _asuraRoot && _asuraRoot.activeSelf;
    public void Configure(PlayerActor player)
    {
        if (_player == player) return;
        UnbindEvents(); _player = player; _holder = null; _controls = null; _breaker = null; _bowBoons = null; _spearBoons = null;
        if (_initialized && _player && _player.TryGetComponent(out _holder))
        {
            _controls = _player.GetComponent<CharControlScript>();
            if (isActiveAndEnabled) { BindEvents(); RefreshResources(); }
        }
    }

    private void Start()
    {
        _canvas = GetComponent<Canvas>();
        if (!_player || !_player.TryGetComponent(out _holder) || !_dock || _slots == null || _slots.Length != 5
            || !_healthFill || !_manaFill || !_healthValue || !_manaValue || !_feedback || !_tooltip
            || !_asuraLabel || !_asuraFill || !_asuraRoot)
        {
            Debug.LogError("PlayerHUD requires a player, AbilityHolder and five bound slots.", this);
            enabled = false;
            return;
        }
        _controls = _player.GetComponent<CharControlScript>();
        _player.healthBar = _healthFill;
        _player.manaBar = _manaFill;
        _initialized = true;
        BindEvents();
        RefreshResources();
    }

    private void OnEnable()
    {
        if (_initialized && _player && _holder) { BindEvents(); RefreshResources(); }
    }

    private void OnDisable()
    {
        UnbindEvents();
        if (_asuraRoot) _asuraRoot.SetActive(false);
        if (_feedback) SetFeedbackText("");
        _feedbackUntil = 0f; _gauntletFeedback = false;
        _gauntletFeedbackMessage = null; _gainFrame = -1;
    }

    private void BindEvents()
    {
        if (_bound) return;
        _holder.AbilityUsed += OnUsed;
        _holder.AbilityRejected += OnRejected;
        _player.HealthChanged += OnHealthChanged;
        _player.ManaChanged += OnManaChanged;
        _bound = true;
        if (!_breaker) _breaker = _player.GetComponent<BreakerGauntletCombat>();
        if (_breaker && _breaker.isActiveAndEnabled && _breaker.IsEquipped) ObserveEnergyPolicy(_breaker.EnergyPolicy);
    }

    private void OnDestroy()
        => UnbindEvents();

    private void UnbindEvents()
    {
        ObserveEnergyPolicy(null);
        if (!_bound) return;
        if (_holder) { _holder.AbilityUsed -= OnUsed; _holder.AbilityRejected -= OnRejected; }
        if (_player) { _player.HealthChanged -= OnHealthChanged; _player.ManaChanged -= OnManaChanged; }
        _bound = false;
    }

    private void ObserveEnergyPolicy(GauntletEnergyPolicy policy)
    {
        if (_observedEnergyPolicy == policy) return;
        if (_observedEnergyPolicy != null) _observedEnergyPolicy.Committed -= OnGauntletGain;
        _observedEnergyPolicy = policy;
        if (_observedEnergyPolicy != null) _observedEnergyPolicy.Committed += OnGauntletGain;
    }

    private void OnGauntletGain(GauntletEnergyGain gain)
    {
        if (!_bound || !_breaker || !_breaker.isActiveAndEnabled || !_breaker.IsEquipped || _player.IsDead) return;
        if (gain.Reserve > 0)
            ShowFeedback($"RESERVA · {gain.After} ASURA", new Color(.94f, .78f, .45f));
        else if (gain.Effective > 0)
        {
            long root = gain.Context?.RootExecution ?? 0, epoch = gain.Context?.Epoch ?? 0;
            if (_gainFrame != Time.frameCount || _gainRoot != root || _gainEpoch != epoch)
            {
                _gainFrame = Time.frameCount; _gainRoot = root; _gainEpoch = epoch;
                _gainEffective = 0; _gainSources = 0;
            }
            _gainEffective += gain.Effective;
            if (gain.Basic > 0) _gainSources |= 1;
            if (gain.Skill > 0) _gainSources |= 2;
            if (gain.Alternation > 0) _gainSources |= 4;
            if (gain.StanceBreak > 0) _gainSources |= 8;
            if (gain.Technique > 0) _gainSources |= 16;
            if (gain.Skill > 0 && gain.Context != null && gain.Context.MomentumRank > 0) _gainSources |= 32;
            if (gain.StanceBreak > 0 && gain.Context?.ProducerId == WeaponRunModifiers.CatalogId(WeaponBoon.GuardBreaker)) _gainSources |= 64;
            var sources = new System.Collections.Generic.List<string>();
            if ((_gainSources & 1) != 0) sources.Add("BÁSICO");
            if ((_gainSources & 2) != 0) sources.Add("HABILIDADE");
            if ((_gainSources & 4) != 0) sources.Add("ALTERNÂNCIA");
            if ((_gainSources & 8) != 0) sources.Add("QUEBRA");
            if ((_gainSources & 16) != 0) sources.Add("PUNHO DE ASURA");
            if ((_gainSources & 32) != 0) sources.Add("DÍNAMO");
            if ((_gainSources & 64) != 0) sources.Add("GUARDA PARTIDA");
            if (sources.Count == 0) sources.Add("BÔNUS");
            ShowFeedback($"+{_gainEffective} ASURA · " + string.Join(" + ", sources), new Color(.94f, .78f, .45f));
        }
        else return;
        _gauntletFeedback = true;
        _gauntletFeedbackMessage = _feedback.text;
    }

    public bool BlocksPointer(Vector2 position) => isActiveAndEnabled && (!_canvas || _canvas.enabled) && _dock &&
        RectTransformUtility.RectangleContainsScreenPoint(_dock, position, null);

    private void OnHealthChanged(Actor actor) => RefreshResources();
    private void OnManaChanged(PlayerActor actor) => RefreshResources();
    private void RefreshResources()
    {
        if (!_player) return;
        _healthFill.fillAmount = Mathf.Clamp01(_player.health / Mathf.Max(1f,_player.maxHealth));
        _manaFill.fillAmount = Mathf.Clamp01(_player.mana / Mathf.Max(1f,_player.maxMana));
        string health = HudFormatter.Resource(_player.health, _player.maxHealth);
        if (_healthCache.HasChanged(health)) _healthValue.text = health;
        string mana = HudFormatter.Resource(_player.mana, _player.maxMana);
        if (_manaCache.HasChanged(mana)) _manaValue.text = mana;
    }

    private void Update()
    {
        if (!_bound || !_player || !_holder) return;
        // Only Nexus opts in through serialized references. The base kit stays visible at its training target.
        if (_lobby && _canvas)
            _canvas.enabled = !_lobby.IsPanelOpen && _lobbyTrainingAnchor &&
                (_player.transform.position - _lobbyTrainingAnchor.position).sqrMagnitude <= _lobbyTrainingRange * _lobbyTrainingRange;
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + .033f;
        if (!_breaker) _breaker = _player.GetComponent<BreakerGauntletCombat>();
        if (!_bowBoons) _bowBoons = _player.GetComponent<BowBoonCombat>();
        if (!_spearBoons) _spearBoons = _player.GetComponent<SpearBoonCombat>();
        bool hasAsura = _breaker && _breaker.isActiveAndEnabled && _breaker.IsEquipped && !_player.IsDead;
        ObserveEnergyPolicy(hasAsura ? _breaker.EnergyPolicy : null);
        if (!hasAsura && _gauntletFeedback) { SetFeedbackText(""); _gauntletFeedback = false; }
        _asuraRoot.SetActive(hasAsura);
        AbilityUseFailure? rUnavailable = hasAsura && _breaker.IsReady ? _holder.GetGauntletRUnavailability() : AbilityUseFailure.Requirement;
        if (hasAsura)
        {
            _asuraFill.fillAmount = _breaker.Energy/100f;
            string waitingHint = HudFormatter.CadencedWait(GamePreferences.KeyLabel(_holder.GetAbilityKey(3)),
                GamePreferences.BindingLabel(GameControl.FinishSequence));
            string asura = HudFormatter.Asura(_breaker.Energy, rUnavailable, _breaker.IsAsuraActive, _breaker.IsCadencedWaiting, waitingHint);
            if (_asuraCache.HasChanged(asura)) _asuraLabel.text = asura;
        }
        Vector2 pointer = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null) pointer = Mouse.current.position.ReadValue();
#else
        pointer = Input.mousePosition;
#endif
        string tooltip = "";
        for (int i = 0; i < _slots.Length; i++)
        {
            Ability ability = i < 4 ? (i < _holder.ActiveAbilities.Count ? _holder.ActiveAbilities[i] : null) : _controls ? _controls.dashScript : null;
            float remaining = i < 4 ? _holder.GetRemainingCooldown(i) : ability is DashScript dash ? dash.GetRemainingCooldown(_player.gameObject) : 0f;
            float ratio = i < 4 ? _holder.GetCooldownRatio(i) : remaining / Mathf.Max(.001f, ability ? ability.cooldownTime * _player.Stats.CooldownMultiplier : 1f);
            bool active = i < 4 ? _holder.IsAbilityActive(i) : ability is DashScript move && move.IsDashing(_player.gameObject);
            bool requirement = !(ability is BreakerGauntletAbility breakerAbility) || !breakerAbility.AsuraBurst || (hasAsura && _breaker.IsReady) || active;
            bool asuraSlot = ability is BreakerGauntletAbility r && r.AsuraBurst;
            if (asuraSlot && _breaker && _breaker.IsAsuraActive) { active = true; remaining = 0f; ratio = 0f; }
            if (asuraSlot && !active) requirement = !rUnavailable.HasValue;
            string actionName = ActionName(i, ability);
            if (i == 3 && _holder.UsesDirectWeaponSpecial) actionName = "Especial · " + actionName;
            string extensionStatus = i == 3 && _bowBoons && _bowBoons.IsCharging ? (_bowBoons.ChargeRatio * 100f).ToString("0") + "%" :
                i == 3 && _spearBoons && _spearBoons.HasOrbit ? GamePreferences.BindingLabel(GameControl.FinishSequence) + " RETORNO" :
                i == 2 && _bowBoons && _bowBoons.ActiveMines > 0 ? _bowBoons.ActiveMines + " MINAS" :
                i == 2 && _spearBoons && _spearBoons.ActiveStakes > 0 ? _spearBoons.ActiveStakes + " HASTES" : null;
            _slots[i].Present(ability, i < 4 ? GamePreferences.KeyLabel(_holder.GetAbilityKey(i)) : GamePreferences.BindingLabel(GameControl.Dash), remaining, ratio,
                active, ability && _player.HasMana(ability.ManaCost), requirement, actionName,
                asuraSlot && hasAsura && _breaker.IsReady ? "BLOQUEADO" : null,
                asuraSlot && _breaker && _breaker.IsCadencedWaiting
                    ? GamePreferences.KeyLabel(_holder.GetAbilityKey(3)) + " / " + GamePreferences.BindingLabel(GameControl.FinishSequence) : extensionStatus);
            if (ability && RectTransformUtility.RectangleContainsScreenPoint(_slots[i].Rect, pointer, null))
            {
                tooltip = HudFormatter.Tooltip(actionName, ability.ManaCost, ability.cooldownTime);
                if (i == 3 && _bowBoons && _bowBoons.Handles(ability as ArsenalAbility))
                    tooltip += "\nSegure " + GamePreferences.KeyLabel(_holder.GetAbilityKey(3)) + ": concentração completa em 0,8s; solte para disparar.";
                if (i == 3 && _spearBoons && _spearBoons.HasOrbit)
                    tooltip += "\n[" + GamePreferences.BindingLabel(GameControl.FinishSequence) + "] retornar cedo · passagens omitidas não são redistribuídas.";
                if (asuraSlot && _breaker && _breaker.IsCadencedAsura)
                    tooltip += "\n" + HudFormatter.CadencedWait(GamePreferences.KeyLabel(_holder.GetAbilityKey(3)),
                        GamePreferences.BindingLabel(GameControl.FinishSequence)) + " · após 1º: 75%; após 2º: 100% do dano/postura nativos de R.";
            }
        }
        if (_tooltipCache.HasChanged(tooltip)) _tooltip.text = tooltip;
        if (Time.unscaledTime >= _feedbackUntil) { SetFeedbackText(""); _gauntletFeedback = false; }
    }

    private string ActionName(int index, Ability ability)
    {
        if (!ability) return "";
        var modifiers = _player.RunModifiers;
        if (modifiers != null && index >= 0 && index < 4)
        {
            string id = modifiers.Expansion.MutationId((BoonSlot)index);
            if (BoonExpansionCatalog.TryDefinition(id, out var replacement)) return replacement.Title;
        }
        var executing = _breaker ? _breaker.ExecutionSnapshot : null;
        if (executing != null && _breaker.IsExecuting && (int)executing.Slot == index)
            return executing.Mutation.HasValue ? GauntletContentAuthoring.MutationTitle(executing.Mutation.Value) : ability.DisplayName;
        if (index >= 0 && index < 4 && ability is BreakerGauntletAbility && modifiers != null)
        {
            if (modifiers.GauntletMutations.TryGet((GauntletActionSlot)index, out var mutation))
                return GauntletContentAuthoring.MutationTitle(mutation);
            if (index == 2 && modifiers.Rank(WeaponBoon.ShockRing) > 0)
                return GauntletContentAuthoring.MutationTitle(GauntletMutation.Epicenter);
        }
        return ability.DisplayName;
    }

    private void OnUsed(int index)
    {
        if (index < 0 || index >= _slots.Length) return;
        _slots[index].Flash(true);
        Ability ability = index < 4 ? _holder.ActiveAbilities[index] : _controls.dashScript;
        if (ability is BreakerGauntletAbility)
        {
            if (!_breaker) _breaker = _player.GetComponent<BreakerGauntletCombat>();
            if (_breaker && _breaker.isActiveAndEnabled && _breaker.IsEquipped) ObserveEnergyPolicy(_breaker.EnergyPolicy);
        }
        string message = ability is BreakerGauntletAbility gauntlet && gauntlet.AsuraBurst
            ? (_breaker && _breaker.IsCadencedAsura ? "ASURA CADENCIADA · 3 pressionamentos · proteção só nos golpes" : "ASURA · 85% proteção · esquiva na janela")
                + $" · reserva {(_breaker ? _breaker.Energy : 0)}"
            : ability.ManaCost > 0 ? $"{ActionName(index, ability)}  -{ability.ManaCost:0} mana" : $"{ActionName(index, ability)}  |  Gratis";
        ShowFeedback(message, new Color(.94f,.78f,.45f));
        _gauntletFeedback = ability is BreakerGauntletAbility;
        if (_gauntletFeedback) _gauntletFeedbackMessage = message;
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
        _gauntletFeedback = false;
        SetFeedbackText(message);
        _feedback.color = color;
        _feedbackUntil = Time.unscaledTime + 1.6f;
    }

    /// <summary>
    /// Writes the feedback label through the dirty cache so every writer (Update's per-frame clear,
    /// ShowFeedback, charge indicator, momentum stacks) keeps the cache in sync. The TMP is only
    /// reassigned when the string actually changes, preventing redundant text churn (R2.1/R2.2).
    /// </summary>
    private void SetFeedbackText(string message)
    {
        if (_feedbackCache.HasChanged(message)) _feedback.text = message;
    }

    /// <summary>
    /// Charged Shot (R3.4): present the current charge as a 0..1 ratio while the player holds the
    /// basic-attack input. The cue reaches its full state exactly when <c>ratio</c> hits 1 (at
    /// <see cref="ChargedShot.ChargeTime"/>); passing 0 clears it. Purely cosmetic and never gates
    /// the shot, so it is safe to no-op when the feedback label is missing.
    /// </summary>
    public void SetChargeIndicator(float ratio)
    {
        if (!_feedback) return;
        ratio = Mathf.Clamp01(ratio);
        if (ratio <= 0f)
        {
            SetFeedbackText("");
            return;
        }
        bool full = ratio >= 1f;
        SetFeedbackText(full ? "TIRO CARREGADO" : $"CARREGANDO  {Mathf.RoundToInt(ratio * 100f)}%");
        _feedback.color = full ? new Color(1f, .8f, .3f) : new Color(.6f, .85f, 1f);
        // Keep the cue alive only as long as the caller keeps pushing updates while holding.
        _feedbackUntil = Time.unscaledTime + .1f;
    }

    /// <summary>
    /// Momentum Strike (R6.4): present the current aggression stack count. Purely cosmetic and
    /// never gates the damage bonus. A count of zero clears the indicator so the HUD shows nothing
    /// once stacks are lost. Reuses the shared feedback text channel; safe to no-op when the
    /// feedback label is missing.
    /// </summary>
    public void ShowMomentumStacks(int stacks)
    {
        if (!_feedback) return;
        if (_gauntletFeedback && Time.unscaledTime < _feedbackUntil)
        {
            SetFeedbackText(_gauntletFeedbackMessage + (stacks > 0 ? $" · ÍMPETO x{stacks}" : ""));
            return;
        }
        if (stacks <= 0)
        {
            SetFeedbackText("");
            _feedbackUntil = 0f;
            return;
        }
        SetFeedbackText($"ÍMPETO  x{stacks}");
        _feedback.color = new Color(1f, .62f, .25f);
        _feedbackUntil = Time.unscaledTime + 1.6f;
    }
}
