using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AbilityHolder : MonoBehaviour
{
    public Ability[] abilities;
    public KeyCode[] keys;

    private float[] cooldownTimers;
    private float[] activeTimers;
    private AbilityState[] states;

    private enum AbilityState
    {
        Ready,
        Active,
        Cooldown
    }

    private void Start()
    {
        abilities ??= Array.Empty<Ability>();

        if (keys == null || keys.Length != abilities.Length)
        {
            Debug.LogWarning("AbilityHolder: keys array should match abilities length.", this);
        }

        cooldownTimers = new float[abilities.Length];
        activeTimers = new float[abilities.Length];
        states = new AbilityState[abilities.Length];
    }

    private void Update()
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            TickAbility(i);
        }
    }

    private void TickAbility(int index)
    {
        Ability ability = abilities[index];
        if (ability == null) return;

        switch (states[index])
        {
            case AbilityState.Ready:
                TryActivate(index, ability);
                break;
            case AbilityState.Active:
                TickActive(index, ability);
                break;
            case AbilityState.Cooldown:
                TickCooldown(index);
                break;
        }
    }

    private void TryActivate(int index, Ability ability)
    {
        if (!IsAbilityKeyPressed(index)) return;

        ability.Activate(gameObject);
        states[index] = AbilityState.Active;
        activeTimers[index] = ability.activeTime;
    }

    private void TickActive(int index, Ability ability)
    {
        activeTimers[index] -= Time.deltaTime;
        if (activeTimers[index] > 0f) return;

        states[index] = AbilityState.Cooldown;
        cooldownTimers[index] = ability.cooldownTime;
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
        if (keys == null || index >= keys.Length)
        {
            return false;
        }

        KeyCode keyCode = keys[index];

#if ENABLE_INPUT_SYSTEM
        if (TryGetKey(keyCode, out Key key))
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                return keyboard[key].wasPressedThisFrame;
            }
        }
#endif

        return Input.GetKeyDown(keyCode);
    }

#if ENABLE_INPUT_SYSTEM
    private static bool TryGetKey(KeyCode keyCode, out Key key)
    {
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
