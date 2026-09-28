using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum GameControl { Primary, Move, Dash, Skill1, Skill2, Skill3, Skill4 }

/// <summary>Presentation and control preferences; never clears equipment, currency or run progress.</summary>
public static class GamePreferences
{
    public const string TutorialSeenKey = "TechGuy.Progress.TutorialSeen";
    private const string Prefix = "TechGuy.Settings.";
    public static bool TutorialSeen => PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;

    public sealed class Options
    {
        public float Volume;
        public int Quality, Width, Height;
        public bool Fullscreen, VSync;
        public KeyCode[] Bindings = DefaultBindings();
        public Options Copy()
        {
            var copy = (Options)MemberwiseClone();
            copy.Bindings = (KeyCode[])Bindings.Clone();
            return copy;
        }
    }

    public static Options Defaults() => new Options
    {
        Volume = 1f, Quality = Mathf.Max(0, QualitySettings.names.Length - 1),
        Width = Screen.currentResolution.width > 0 ? Screen.currentResolution.width : 1280,
        Height = Screen.currentResolution.height > 0 ? Screen.currentResolution.height : 720,
        Fullscreen = true, VSync = true
    };

    public static Options Read()
    {
        var defaults = Defaults();
        return new Options
        {
            Bindings = ReadBindings(),
            Volume = Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "Volume", defaults.Volume)),
            Quality = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Quality", defaults.Quality), 0, Mathf.Max(0, QualitySettings.names.Length - 1)),
            Width = Mathf.Max(640, PlayerPrefs.GetInt(Prefix + "Width", defaults.Width)),
            Height = Mathf.Max(480, PlayerPrefs.GetInt(Prefix + "Height", defaults.Height)),
            Fullscreen = PlayerPrefs.GetInt(Prefix + "Fullscreen", 1) == 1,
            VSync = PlayerPrefs.GetInt(Prefix + "VSync", 1) == 1
        };
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() => Apply(Read());

    public static void Apply(Options options)
    {
        AudioListener.volume = Mathf.Clamp01(options.Volume);
        QualitySettings.SetQualityLevel(Mathf.Clamp(options.Quality, 0, QualitySettings.names.Length - 1), true);
        QualitySettings.vSyncCount = options.VSync ? 1 : 0;
        // Editor Game view size is controlled by the Editor, not the player's monitor settings.
        if (!Application.isEditor)
            Screen.SetResolution(options.Width, options.Height, options.Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public static void Save(Options options)
    {
        Apply(options);
        PlayerPrefs.SetFloat(Prefix + "Volume", Mathf.Clamp01(options.Volume));
        PlayerPrefs.SetInt(Prefix + "Quality", options.Quality);
        PlayerPrefs.SetInt(Prefix + "Width", options.Width);
        PlayerPrefs.SetInt(Prefix + "Height", options.Height);
        PlayerPrefs.SetInt(Prefix + "Fullscreen", options.Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "VSync", options.VSync ? 1 : 0);
        SaveBindings(options.Bindings);
        PlayerPrefs.Save();
    }

    public static KeyCode[] DefaultBindings() => new[] { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Space,
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R };

    public static KeyCode Binding(GameControl control)
    {
        KeyCode fallback;
        switch (control)
        {
            case GameControl.Primary: fallback = KeyCode.Mouse0; break;
            case GameControl.Move: fallback = KeyCode.Mouse1; break;
            case GameControl.Dash: fallback = KeyCode.Space; break;
            case GameControl.Skill1: fallback = KeyCode.Q; break;
            case GameControl.Skill2: fallback = KeyCode.W; break;
            case GameControl.Skill3: fallback = KeyCode.E; break;
            default: fallback = KeyCode.R; break;
        }
        KeyCode key = (KeyCode)PlayerPrefs.GetInt(Prefix + "Binding." + control, (int)fallback);
        return IsBindable(key) ? key : fallback;
    }

    public static KeyCode[] ReadBindings()
    {
        var result = DefaultBindings();
        for (int i = 0; i < result.Length; i++) result[i] = Binding((GameControl)i);
        return result;
    }

    public static void SaveBindings(KeyCode[] bindings)
    {
        for (int i = 0; i < bindings.Length; i++)
            PlayerPrefs.SetInt(Prefix + "Binding." + (GameControl)i, (int)bindings[i]);
        PlayerPrefs.Save();
    }

    public static string BindingLabel(GameControl control) => KeyLabel(Binding(control));
    public static string KeyLabel(KeyCode key) => key == KeyCode.Mouse0 ? "Mouse esquerdo" :
        key == KeyCode.Mouse1 ? "Mouse direito" : key == KeyCode.Mouse2 ? "Mouse central" :
        key == KeyCode.Space ? "Espaço" : key.ToString();

    public static bool IsBindable(KeyCode key) => key != KeyCode.None && key != KeyCode.Escape &&
        (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse4 || TryKeyboardKey(key, out _));

    private static bool TryKeyboardKey(KeyCode code, out Key key)
    {
        string name = code.ToString();
        if (name.StartsWith("Alpha")) name = "Digit" + name.Substring(5);
        if (name.StartsWith("Keypad")) name = "Numpad" + name.Substring(6);
        if (code == KeyCode.Return) name = "Enter";
        if (code == KeyCode.LeftControl) name = "LeftCtrl";
        if (code == KeyCode.RightControl) name = "RightCtrl";
        return System.Enum.TryParse(name, true, out key) && key != Key.None && System.Enum.IsDefined(typeof(Key), key);
    }

    private static ButtonControl Button(KeyCode key)
    {
        if (key >= KeyCode.Mouse0 && key <= KeyCode.Mouse4)
        {
            var mouse = Mouse.current;
            if (mouse == null) return null;
            switch (key)
            {
                case KeyCode.Mouse0: return mouse.leftButton;
                case KeyCode.Mouse1: return mouse.rightButton;
                case KeyCode.Mouse2: return mouse.middleButton;
                case KeyCode.Mouse3: return mouse.backButton;
                default: return mouse.forwardButton;
            }
        }
        return Keyboard.current != null && TryKeyboardKey(key, out Key inputKey) ? Keyboard.current[inputKey] : null;
    }

    public static bool WasPressed(GameControl control) => WasPressed(Binding(control));
    public static bool WasPressed(KeyCode key) => Button(key)?.wasPressedThisFrame ?? false;
    public static bool IsHeld(GameControl control) => Button(Binding(control))?.isPressed ?? false;

    public static void MarkTutorialSeen()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();
    }
}
