using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AbilityHolder : MonoBehaviour
{
    private static readonly KeyCode[] DefaultAbilityKeys =
    {
        KeyCode.Q,
        KeyCode.W,
        KeyCode.E,
        KeyCode.R
    };

    [Header("Weapon Abilities")]
    public KeyCode[] keys;

    [Header("Run Passives")]
    [SerializeField] private Ability[] passiveAbilities;

    [Obsolete("Active abilities are loaded from the equipped WeaponScript. Use passiveAbilities for run passives.")]
    public Ability[] abilities;

    private readonly List<Ability> acquiredPassiveAbilities = new List<Ability>();
    private Ability[] activeAbilities = Array.Empty<Ability>();
    private float[] cooldownTimers = Array.Empty<float>();
    private float[] activeTimers = Array.Empty<float>();
    private AbilityState[] states = Array.Empty<AbilityState>();
    private PlayerActor playerActor;
    private WeaponScript currentWeapon;
    private BreakerGauntletCombat _breakerCombat;
    private CharControlScript _characterControl;
    [SerializeField] private LobbyInteraction _lobbyInteraction;
    public event Action<int> AbilityUsed;
    public event Action<int, AbilityUseFailure> AbilityRejected;

    public bool IsCasting => _breakerCombat && _breakerCombat.IsExecuting;

    private enum AbilityState
    {
        Ready,
        Active,
        Cooldown
    }

    private void Awake()
    {
        playerActor = GetComponent<PlayerActor>();
        _characterControl = GetComponent<CharControlScript>();
    }

    private void Start()
    {
        if (passiveAbilities?.Length > 0)
        {
            foreach (Ability passiveAbility in passiveAbilities)
            {
                AddPassiveAbility(passiveAbility);
            }
        }

        RefreshWeaponAbilities(force: true);
    }

    private void Update()
    {
        RefreshWeaponAbilities(force: false);

        for (int i = 0; i < activeAbilities.Length; i++)
        {
            TickAbility(i);
            if (IsAbilityKeyPressed(i)) TryUseAbility(i);
        }
    }

    public IReadOnlyList<Ability> ActiveAbilities => activeAbilities;
    public IReadOnlyList<Ability> PassiveAbilities => acquiredPassiveAbilities;

    public void AddPassiveAbility(Ability passiveAbility)
    {
        if (passiveAbility && !acquiredPassiveAbilities.Contains(passiveAbility))
        {
            acquiredPassiveAbilities.Add(passiveAbility);

            if (passiveAbility is PassiveAbility passive)
            {
                passive.OnAcquired(playerActor);
            }

            if (playerActor)
            {
                playerActor.RefreshResourceStats();
            }
        }
    }

    public void RemovePassiveAbility(Ability passiveAbility)
    {
        if (!passiveAbility || !acquiredPassiveAbilities.Remove(passiveAbility)) return;

        if (passiveAbility is PassiveAbility passive)
        {
            passive.OnRemoved(playerActor);
        }

        if (playerActor)
        {
            playerActor.RefreshResourceStats();
        }
    }

    public void NotifyAttackHits(PlayerActor owner, IReadOnlyList<Actor> damagedActors)
    {
        if (!owner || damagedActors is null || damagedActors.Count == 0) return;

        foreach (Ability passiveAbility in acquiredPassiveAbilities)
        {
            if (passiveAbility is AttackPassiveAbility attackPassive)
            {
                attackPassive.OnAfterAttackHits(owner, damagedActors);
            }
        }
    }

    private void RefreshWeaponAbilities(bool force)
    {
        WeaponScript equippedWeapon = playerActor ? playerActor.CurrentWeapon : null;
        if (!force && equippedWeapon == currentWeapon) return;

        currentWeapon = equippedWeapon;
        activeAbilities = currentWeapon && currentWeapon.abilities is not null
            ? currentWeapon.abilities
            : Array.Empty<Ability>();

        cooldownTimers = new float[activeAbilities.Length];
        activeTimers = new float[activeAbilities.Length];
        states = new AbilityState[activeAbilities.Length];

        bool usesBreaker = Array.Exists(activeAbilities, ability => ability is BreakerGauntletAbility);
        if (usesBreaker && !_breakerCombat)
            _breakerCombat = gameObject.AddComponent<BreakerGauntletCombat>();
        if (_breakerCombat) _breakerCombat.Configure(usesBreaker ? currentWeapon : null);

        if (keys is null || keys.Length < activeAbilities.Length)
        {
            Debug.LogWarning("AbilityHolder: keys array has fewer entries than weapon abilities. Default Q/W/E/R bindings will be used where needed.", this);
        }
    }

    private void TickAbility(int index)
    {
        Ability ability = activeAbilities[index];
        if (!ability) return;

        switch (states[index])
        {
            case AbilityState.Ready:
                break;
            case AbilityState.Active:
                TickActive(index, ability);
                break;
            case AbilityState.Cooldown:
                TickCooldown(index);
                break;
        }
    }

    public bool TryUseAbility(int index)
    {
        RefreshWeaponAbilities(false);
        if (index < 0 || index >= activeAbilities.Length || !activeAbilities[index]) return false;
        Ability ability = activeAbilities[index];
        if (states[index] != AbilityState.Ready) return Reject(index, AbilityUseFailure.Cooldown);
        if (!CheckUse(index, ability, ResolveKey(index))) return false;

        if (!ability.TryActivate(gameObject)) return Reject(index, AbilityUseFailure.Requirement);
        states[index] = AbilityState.Active;
        activeTimers[index] = Mathf.Max(0f, ability.activeTime);

        if (activeTimers[index] <= 0f)
        {
            states[index] = AbilityState.Cooldown;
            cooldownTimers[index] = GetCooldownDuration(ability);
        }
        AbilityUsed?.Invoke(index);
        return true;
    }

    private bool CheckUse(int index, Ability ability, KeyCode key)
    {
        if (!isActiveAndEnabled || !playerActor || playerActor.IsDead) return Reject(index, AbilityUseFailure.Unavailable);
        if (_lobbyInteraction && _lobbyInteraction.BlocksAbilityInput(key)) return Reject(index, AbilityUseFailure.Interaction);
        if (IsCasting || (_characterControl && _characterControl.isDashing)) return Reject(index, AbilityUseFailure.Busy);
        if (!ability.CanActivate(gameObject)) return Reject(index, AbilityUseFailure.Requirement);
        if (!playerActor.HasMana(ability.ManaCost)) return Reject(index, AbilityUseFailure.NotEnoughMana);
        if (_characterControl) _characterControl.CancelCombo();
        return true;
    }

    public bool TryUseDash(DashScript dash)
    {
        if (!dash) return false;
        if (dash.GetRemainingCooldown(gameObject) > 0f) return Reject(4, AbilityUseFailure.Cooldown);
        if (!CheckUse(4, dash, KeyCode.Space)) return false;
        if (!dash.TryActivate(gameObject)) return Reject(4, AbilityUseFailure.Requirement);
        AbilityUsed?.Invoke(4);
        return true;
    }

    private bool Reject(int index, AbilityUseFailure reason)
    {
        AbilityRejected?.Invoke(index, reason);
        return false;
    }

    public float GetRemainingCooldown(int index) =>
        index >= 0 && index < cooldownTimers.Length ? Mathf.Max(0f, cooldownTimers[index]) : 0f;

    public KeyCode GetAbilityKey(int index) => ResolveKey(index);
    public float GetCooldownRatio(int index) => index >= 0 && index < activeAbilities.Length
        ? Mathf.Clamp01(GetRemainingCooldown(index) / Mathf.Max(0.001f, GetCooldownDuration(activeAbilities[index]))) : 0f;

    public bool IsAbilityActive(int index) =>
        index >= 0 && index < states.Length && states[index] == AbilityState.Active;

    private void TickActive(int index, Ability ability)
    {
        activeTimers[index] -= Time.deltaTime;
        if (activeTimers[index] > 0f) return;

        states[index] = AbilityState.Cooldown;
        cooldownTimers[index] = GetCooldownDuration(ability);
    }

    private void TickCooldown(int index)
    {
        cooldownTimers[index] -= Time.deltaTime;
        if (cooldownTimers[index] <= 0f)
        {
            states[index] = AbilityState.Ready;
        }
    }

    private bool IsAbilityKeyPressed(int index)
    {
        KeyCode keyCode = ResolveKey(index);

#if ENABLE_INPUT_SYSTEM
        if (TryGetKey(keyCode, out Key key))
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard is not null)
            {
                return keyboard[key].wasPressedThisFrame;
            }
        }
#endif

        return Input.GetKeyDown(keyCode);
    }

    private KeyCode ResolveKey(int index)
    {
        if (keys is not null && index < keys.Length && keys[index] != KeyCode.None)
        {
            return keys[index];
        }

        if (index < DefaultAbilityKeys.Length)
        {
            return DefaultAbilityKeys[index];
        }

        return KeyCode.None;
    }

    private float GetCooldownDuration(Ability ability)
    {
        float cooldownTime = ability ? ability.cooldownTime : 0f;
        float cooldownMultiplier = playerActor
            ? playerActor.Stats.CooldownMultiplier
            : 1f;
        return Mathf.Max(0f, cooldownTime * cooldownMultiplier);
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetKey(KeyCode keyCode, out Key key)
    {
        if (keyCode == KeyCode.None)
        {
            key = Key.None;
            return false;
        }

        if (keyCode == KeyCode.Return)
        {
            key = Key.Enter;
            return true;
        }

        if (keyCode == KeyCode.BackQuote)
        {
            key = Key.Backquote;
            return true;
        }

        if (keyCode == KeyCode.LeftControl)
        {
            key = Key.LeftCtrl;
            return true;
        }

        if (keyCode == KeyCode.RightControl)
        {
            key = Key.RightCtrl;
            return true;
        }

        if (keyCode == KeyCode.LeftCommand || keyCode == KeyCode.LeftApple)
        {
            key = Key.LeftMeta;
            return true;
        }

        if (keyCode == KeyCode.RightCommand || keyCode == KeyCode.RightApple)
        {
            key = Key.RightMeta;
            return true;
        }

        string name = keyCode.ToString();
        if (name.StartsWith("Alpha", StringComparison.Ordinal))
        {
            name = "Digit" + name.Substring("Alpha".Length);
        }
        else if (name.StartsWith("Keypad", StringComparison.Ordinal))
        {
            name = "Numpad" + name.Substring("Keypad".Length);
        }

        return Enum.TryParse(name, out key);
    }
#endif
}
