using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum DashDirectionMode { Mouse = 0, Movement = 1 }

public enum GameControl { Primary = 0, Move = 1, Dash = 2, Skill1 = 3, Skill2 = 4, Skill3 = 5, Skill4 = 6, FinishSequence = 7, Interact = 8, MoveUp = 9, MoveDown = 10, MoveLeft = 11, MoveRight = 12, Pause = 13 }

/// <summary>Presentation and control preferences; never clears equipment, currency or run progress.</summary>
public static class GamePreferences
{
    public const string TutorialSeenKey = "TechGuy.Progress.TutorialSeen";
    private const string Prefix = "TechGuy.Settings.";
    private const string DirectPrefix = Prefix + "DirectBinding.v2.";
    private static Object _directInputOwner;
    private static int _directBindingFrame = -1;
    private static KeyCode[] _directBindings;
    public static bool UsesDirectControls => _directInputOwner != null;
    // C01 compatibility adapter: reuse the configured secondary channel; no preference/schema rewrite.
    public static GameControl InteractionAction => UsesDirectControls ? GameControl.Interact : GameControl.Skill3;
    public static KeyCode WeaponSpecialBinding => Binding(GameControl.Move);
    public static bool WasWeaponSpecialPressed() => WasPressed(WeaponSpecialBinding);
    public static readonly GameControl[] DirectActions = { GameControl.MoveUp, GameControl.MoveDown, GameControl.MoveLeft, GameControl.MoveRight,
        GameControl.Primary, GameControl.Move, GameControl.Skill1, GameControl.Skill2, GameControl.Skill3,
        GameControl.Dash, GameControl.Interact, GameControl.FinishSequence, GameControl.Pause };
    public static string MovementLabel => UsesDirectControls ? BindingLabel(GameControl.MoveUp) + "/" + BindingLabel(GameControl.MoveLeft) + "/" + BindingLabel(GameControl.MoveDown) + "/" + BindingLabel(GameControl.MoveRight) : BindingLabel(GameControl.Move);
    public static Vector2 MovementInput => Vector2.ClampMagnitude(new Vector2((IsHeld(GameControl.MoveRight) ? 1 : 0) - (IsHeld(GameControl.MoveLeft) ? 1 : 0),
        (IsHeld(GameControl.MoveUp) ? 1 : 0) - (IsHeld(GameControl.MoveDown) ? 1 : 0)), 1);
    public static string ActionLabel(GameControl action) => action == GameControl.Move ? "Especial da arma" : action == GameControl.Primary ? "Ataque básico" :
        action == GameControl.MoveUp ? "Mover para frente" : action == GameControl.MoveDown ? "Mover para trás" : action == GameControl.MoveLeft ? "Mover à esquerda" :
        action == GameControl.MoveRight ? "Mover à direita" : action == GameControl.Dash ? "Esquiva" : action == GameControl.Interact ? "Interagir" :
        action == GameControl.Pause ? "Pausa (Esc sempre disponível)" : action == GameControl.FinishSequence ? "Finalizar / retornar" : action == GameControl.Skill1 ? "Habilidade 1" : action == GameControl.Skill2 ? "Habilidade 2" : "Habilidade 3";
    public static bool AnyGameplayHeld
    {
        get { foreach (var action in DirectActions) if (IsHeld(action)) return true; return false; }
    }
    internal static void UseDirectControls(Object owner) { _directInputOwner = owner; _directBindingFrame = -1; }
    internal static void ReleaseDirectControls(Object owner)
    { if (_directInputOwner == owner) { _directInputOwner = null; _directBindingFrame = -1; } }
    public static DashDirectionMode DashDirection => PlayerPrefs.GetInt(Prefix + "DashDirection", 0) == 1 ? DashDirectionMode.Movement : DashDirectionMode.Mouse;
    public static void SaveDashDirection(DashDirectionMode mode)
    {
        if (mode != DashDirectionMode.Mouse && mode != DashDirectionMode.Movement) throw new System.ArgumentException("Direção de esquiva inválida.");
        PlayerPrefs.SetInt(Prefix + "DashDirection", (int)mode); PlayerPrefs.Save();
    }
    public static bool TutorialSeen => PlayerPrefs.GetInt(TutorialSeenKey, 0) == 1;

    public sealed class Options
    {
        public float Volume;
        public DashDirectionMode DashDirection;
        public int Quality, Width, Height;
        public bool Fullscreen, VSync;
        public KeyCode[] Bindings = UsesDirectControls ? DefaultDirectBindings() : DefaultBindings();
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
            DashDirection = GamePreferences.DashDirection,
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
        if (UsesDirectControls && !ValidateDirectBindings(options.Bindings, out string error)) throw new System.ArgumentException(error);
        if (!System.Enum.IsDefined(typeof(DashDirectionMode), options.DashDirection)) throw new System.ArgumentException("Direção de esquiva inválida.");
        Apply(options);
        SaveDashDirection(options.DashDirection);
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
        KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.F };

    public static KeyCode Binding(GameControl control)
    {
        if (!UsesDirectControls && (int)control < 8) return LegacyBinding(control);
        if (_directBindings == null || _directBindingFrame != Time.frameCount)
        { _directBindings = ReadDirectBindings(); _directBindingFrame = Time.frameCount; }
        int index = (int)control;
        return index >= 0 && index < _directBindings.Length ? _directBindings[index] : KeyCode.None;
    }

    private static KeyCode LegacyBinding(GameControl control)
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
            case GameControl.FinishSequence: fallback = KeyCode.F; break;
            default: fallback = KeyCode.R; break;
        }
        KeyCode key = (KeyCode)PlayerPrefs.GetInt(Prefix + "Binding." + control, (int)fallback);
        if (control == GameControl.FinishSequence)
        {
            var existing = new KeyCode[(int)GameControl.FinishSequence];
            for (int i = 0; i < existing.Length; i++) existing[i] = LegacyBinding((GameControl)i);
            return ResolveFinishSequenceBinding(key, existing);
        }
        return IsBindable(key) ? key : fallback;
    }

    private static readonly KeyCode[] FinishSequenceFallbacks =
        { KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.T, KeyCode.Y, KeyCode.U, KeyCode.J, KeyCode.K };

    internal static KeyCode ResolveFinishSequenceBinding(KeyCode preferred, IReadOnlyList<KeyCode> existing)
    {
        if (IsBindable(preferred) && !ContainsBinding(existing, preferred)) return preferred;
        foreach (var fallback in FinishSequenceFallbacks)
            if (!ContainsBinding(existing, fallback)) return fallback;
        return KeyCode.None;
    }
    private static bool ContainsBinding(IReadOnlyList<KeyCode> existing, KeyCode key)
    {
        if (existing != null) for (int i = 0; i < existing.Count; i++) if (existing[i] == key) return true;
        return false;
    }

    public static KeyCode[] ReadBindings()
    {
        var result = UsesDirectControls ? DefaultDirectBindings() : DefaultBindings();
        for (int i = 0; i < result.Length; i++) result[i] = Binding((GameControl)i);
        return result;
    }

    public static void SaveBindings(KeyCode[] bindings)
    {
        if (UsesDirectControls)
        {
            if (!ValidateDirectBindings(bindings, out string error)) throw new System.ArgumentException(error);
            // Action names remain stable: extend v2 without rewriting legacy keys or saving on read.
            foreach (var action in DirectActions) PlayerPrefs.SetInt(DirectPrefix + action, (int)bindings[(int)action]);
            _directBindingFrame = -1;
            PlayerPrefs.Save();
            return;
        }
        for (int i = 0; i < bindings.Length; i++)
            PlayerPrefs.SetInt(Prefix + "Binding." + (GameControl)i, (int)bindings[i]);
        PlayerPrefs.Save();
    }

    // Index 3 remains the legacy special, NOT a fourth modular skill or X/Asura alias.
    public static KeyCode[] DefaultDirectBindings() => new[] { KeyCode.Mouse0, KeyCode.Mouse1, KeyCode.Space,
        KeyCode.Q, KeyCode.E, KeyCode.R, KeyCode.F11, KeyCode.F, KeyCode.G, KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Escape };

    public static bool IsMovementKey(KeyCode key) => key == KeyCode.W || key == KeyCode.A || key == KeyCode.S || key == KeyCode.D;

    internal static KeyCode[] MigrateLegacyBindings(IReadOnlyList<KeyCode> legacy)
    {
        var oldDefaults = DefaultBindings();
        var proposed = DefaultDirectBindings();
        for (int i = 0; i < oldDefaults.Length && i < legacy.Count; i++)
            if (legacy[i] != oldDefaults[i] && !IsMovementKey(legacy[i])) proposed[i] = legacy[i];
        return NormalizeDirectBindings(proposed);
    }

    public static bool ValidateDirectBindings(KeyCode[] bindings, out string error)
    {
        if (bindings == null || bindings.Length != DefaultDirectBindings().Length) { error = "Configuração de controles inválida."; return false; }
        var used = new Dictionary<KeyCode, GameControl>();
        foreach (var action in DirectActions)
        {
            var key = bindings[(int)action];
            if (!IsBindable(key) && !(action == GameControl.Pause && key == KeyCode.Escape)) { error = "Tecla inválida para " + ActionLabel(action) + ". Esc permanece reservado."; return false; }
            if (used.TryGetValue(key, out var other)) { error = ActionLabel(action) + " conflita com " + ActionLabel(other) + "."; return false; }
            used.Add(key, action);
        }
        error = null; return true;
    }

    private static KeyCode[] NormalizeDirectBindings(KeyCode[] preferred)
    {
        var defaults = DefaultDirectBindings(); var result = new KeyCode[defaults.Length];
        var used = new HashSet<KeyCode>();
        // Preserve valid custom assignments first. Repair only invalid/duplicate/missing entries in memory.
        foreach (var action in DirectActions)
        {
            int i = (int)action;
            if (preferred[i] != defaults[i] && IsBindable(preferred[i]) && used.Add(preferred[i])) result[i] = preferred[i];
        }
        foreach (var action in DirectActions)
        {
            int i = (int)action; if (result[i] != KeyCode.None) continue;
            if (used.Add(defaults[i])) { result[i] = defaults[i]; continue; }
            foreach (KeyCode candidate in System.Enum.GetValues(typeof(KeyCode)))
                if (IsBindable(candidate) && used.Add(candidate)) { result[i] = candidate; break; }
        }
        // Historical execution index is retained, but never a configurable modular action.
        result[(int)GameControl.Skill4] = preferred[(int)GameControl.Skill4];
        return result;
    }

    private static KeyCode[] ReadDirectBindings()
    {
        var legacy = DefaultBindings();
        for (int i = 0; i < legacy.Length; i++) legacy[i] = LegacyBinding((GameControl)i);
        var result = MigrateLegacyBindings(legacy);
        for (int i = 0; i < result.Length; i++)
            if (PlayerPrefs.HasKey(DirectPrefix + (GameControl)i)) result[i] = (KeyCode)PlayerPrefs.GetInt(DirectPrefix + (GameControl)i);
        return NormalizeDirectBindings(result);
    }

    public static string BindingLabel(GameControl control) => KeyLabel(Binding(control));
    public static string KeyLabel(KeyCode key) => key == KeyCode.Mouse0 ? "Mouse esquerdo" :
        key == KeyCode.Mouse1 ? "Mouse direito" : key == KeyCode.Mouse2 ? "Mouse central" :
        key == KeyCode.Mouse3 ? "Mouse lateral atrás" : key == KeyCode.Mouse4 ? "Mouse lateral frente" :
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
    public static bool IsHeld(GameControl control) => IsHeld(Binding(control));
    public static bool IsHeld(KeyCode key) => Button(key)?.isPressed ?? false;

    public static void MarkTutorialSeen()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();
    }
}
