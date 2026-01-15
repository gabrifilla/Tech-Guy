using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class AbilityHolder : MonoBehaviour
{
    public Ability[] abilities;
    private float[] cooldownTimers;
    private float[] activeTimers;
    private AbilityState[] states;
    public KeyCode[] keys;

    enum AbilityState
    {
        ready,
        active,
        cooldown
    }

    void Start()
    {
        if (abilities == null)
        {
            abilities = System.Array.Empty<Ability>();
        }

        if (keys == null || keys.Length != abilities.Length)
        {
            Debug.LogWarning("AbilityHolder: keys array should match abilities length.");
        }

        cooldownTimers = new float[abilities.Length];
        activeTimers = new float[abilities.Length];
        states = new AbilityState[abilities.Length];
        for (int i = 0; i < abilities.Length; i++)
        {
            states[i] = AbilityState.ready;
        }
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < abilities.Length; i++)
        {
            switch (states[i])
            {
                case AbilityState.ready:
                    if (abilities[i] == null)
                    {
                        break;
                    }

                    if (IsAbilityKeyPressed(i))
                    {
                        abilities[i].Activate(gameObject);
                        states[i] = AbilityState.active;
                        activeTimers[i] = abilities[i].activeTime;
                    }
                    break;
                case AbilityState.active:
                    if (activeTimers[i] > 0)
                    {
                        activeTimers[i] -= Time.deltaTime;
                    }
                    else
                    {
                        states[i] = AbilityState.cooldown;
                        cooldownTimers[i] = abilities[i].cooldownTime;
                    }
                    break;
                case AbilityState.cooldown:
                    if (cooldownTimers[i] > 0)
                    {
                        cooldownTimers[i] -= Time.deltaTime;
                    }
                    else
                    {
                        states[i] = AbilityState.ready;
                    }
                    break;
            }
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
