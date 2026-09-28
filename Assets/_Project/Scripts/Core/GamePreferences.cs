using UnityEngine;

/// <summary>Only presentation preferences; never clears equipment, currency or run progress.</summary>
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
        public Options Copy() => (Options)MemberwiseClone();
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
        PlayerPrefs.Save();
    }

    public static void MarkTutorialSeen()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();
    }
}
