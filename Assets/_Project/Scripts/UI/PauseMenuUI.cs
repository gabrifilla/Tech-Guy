using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Player-owned pause and inspection pages. Every exit restores the captured clock/audio state.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class PauseMenuUI : MonoBehaviour
{
    private RunResultsPresenter _results;
    public void BindResults(RunResultsPresenter results) => _results = results;
    private enum PausePage { Home, Upgrades, Map, Controls }

    [SerializeField] private string _mainMenuScene = "MainMenu";
    private readonly ControlRemappingPanel _remapping = new ControlRemappingPanel();
    private KeyCode[] _bindings;
    private PausePage _page;
    private PlayerActor _player;
    private TutorialDirector _tutorial;
    private RunRewardUI _rewards;
    private ManualRunUI _map;
    private float _previousTimeScale;
    private bool _previousAudioPause, _loading, _runAbortRequested;
    private int _resumeFrame = -1, _confirmation;
    [NonSerialized] private GUIStyle _title, _body, _button, _eyebrow;

    public bool IsPaused { get; private set; }
    public bool BlocksInput => IsPaused || _loading || Time.frameCount == _resumeFrame;
    public bool IsMapVisible => IsPaused && _page == PausePage.Map;
    public bool IsUpgradesVisible => IsPaused && _page == PausePage.Upgrades;

    public void BindTutorial(TutorialDirector tutorial) => _tutorial = tutorial;
    public void BindMap(ManualRunUI map) => _map = map;
    public void BindRewards(RunRewardUI rewards)
    {
        _rewards = rewards;
        // RunBoons can add its required AbilityHolder after PlayerActor has created pause.
        // Resolve the local holder again when that runtime owner binds its reward presentation.
        if (TryGetComponent(out AbilityHolder holder)) holder.BindPause(this);
    }

    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        _rewards = GetComponent<RunRewardUI>();
        if (TryGetComponent(out AbilityHolder holder)) holder.BindPause(this);
    }

    private void OnEnable()
    {
        _title = _body = _button = _eyebrow = null;
    }

    private void Update()
    {
        if (_loading || !_player || _player.IsDead || (_results && _results.BlocksInput)) return;
        if (_runAbortRequested)
        {
            if (BlocksInput) return;
            if (!_map || !_map.CanAbortRun) { _runAbortRequested = false; return; }
            // Wait for transient combat/cleanup gates, then make one director-validated attempt.
            if (!_map.CanConfirmRunAbort) return;
            _runAbortRequested = false;
            _map.TryAbortRun();
            return;
        }
        var gamepad = Gamepad.current;
        if (gamepad != null && gamepad.startButton.wasPressedThisFrame)
        {
            if (IsPaused) Resume();
            else Pause();
            return;
        }
        if (IsPaused && gamepad != null && gamepad.buttonEast.wasPressedThisFrame)
        {
            NavigateBack();
            return;
        }
        if (_page == PausePage.Controls && _remapping.Update(_bindings)) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (IsPaused) NavigateBack();
        else Pause();
    }

    private void NavigateBack()
    {
        if (_confirmation != 0) { _confirmation = 0; return; }
        if (_page != PausePage.Home) { ReturnToPauseHome(); return; }
        Resume();
    }

    public void Pause()
    {
        if (IsPaused || _loading || !_player || _player.IsDead || (_results && _results.BlocksInput)) return;
        _previousTimeScale = Time.timeScale;
        _previousAudioPause = AudioListener.pause;
        if (TryGetComponent(out CharControlScript controls)) controls.RequireAttackRelease();
        IsPaused = true;
        Time.timeScale = 0;
        AudioListener.pause = true;
        _page = PausePage.Home;
        _confirmation = 0;
    }

    public bool TryShowMap()
    {
        if (!IsPaused || !_map || !_map.HasPauseMap) return false;
        _page = PausePage.Map;
        _confirmation = 0;
        _remapping.Cancel();
        return true;
    }

    public bool TryShowUpgrades()
    {
        if (!IsPaused || !_rewards) return false;
        _page = PausePage.Upgrades;
        _confirmation = 0;
        _remapping.Cancel();
        return true;
    }

    public void ReturnToPauseHome()
    {
        _page = PausePage.Home;
        _confirmation = 0;
        _remapping.Cancel();
    }

    public void Resume()
    {
        if (!IsPaused) return;
        ReturnToPauseHome();
        IsPaused = false;
        _resumeFrame = Time.frameCount;
        Time.timeScale = _previousTimeScale;
        AudioListener.pause = _previousAudioPause;
        if (TryGetComponent(out CharControlScript controls)) controls.RequireAttackRelease();
    }

    /// <summary>Queues the explicitly confirmed exit without mutating the paused run.</summary>
    public bool TryRequestRunAbort()
    {
        if (!IsPaused || _loading || _runAbortRequested || !_map || !_map.CanAbortRun) return false;
        Resume();
        _runAbortRequested = true;
        return true;
    }

    public void ReturnToMainMenu()
    {
        if (_loading || !Application.CanStreamedLevelBeLoaded(_mainMenuScene)) return;
        Resume();
        _loading = true;
        SceneManager.LoadSceneAsync(_mainMenuScene);
    }

    private void Quit()
    {
        Resume();
        _loading = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisable()
    {
        _runAbortRequested = false;
        Resume();
    }

    private void Styles()
    {
        if (_title != null && _body != null && _button != null && _eyebrow != null) return;
        _title = Label(36, FontStyle.Bold);
        _body = Label(17, FontStyle.Normal);
        _eyebrow = Label(12, FontStyle.Bold);
        _eyebrow.normal.textColor = new Color(.3f, .85f, .98f);
        _button = new GUIStyle(GUI.skin.button)
        {
            fontSize = 19, alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(24, 16, 0, 0)
        };
    }

    private static GUIStyle Label(int size, FontStyle weight)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size, fontStyle = weight, wordWrap = true,
            padding = new RectOffset(0, 0, 0, 0)
        };
        style.normal.textColor = new Color(.92f, .94f, .97f);
        return style;
    }

    private void OnGUI()
    {
        if (!IsPaused) return;
        Styles();
        Matrix4x4 matrix = GUI.matrix;
        Color color = GUI.color, background = GUI.backgroundColor, content = GUI.contentColor;
        int depth = GUI.depth;
        bool enabled = GUI.enabled;
        float scale = Mathf.Max(.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
        try
        {
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2,
                (Screen.height - 720 * scale) / 2, 0), Quaternion.identity, Vector3.one * scale);
            GUI.depth = -100;
            GUI.color = GUI.contentColor = Color.white;
            Fill(new Rect(-Screen.width / scale, -Screen.height / scale,
                Screen.width / scale * 3, Screen.height / scale * 3), new Color(.012f, .023f, .04f, .95f));
            if (_page == PausePage.Upgrades || _page == PausePage.Map) DrawInspectionPage();
            else DrawMenuPage();
        }
        finally
        {
            GUI.matrix = matrix;
            GUI.color = color;
            GUI.backgroundColor = background;
            GUI.contentColor = content;
            GUI.depth = depth;
            GUI.enabled = enabled;
        }
    }

    private void DrawInspectionPage()
    {
        Fill(new Rect(32, 38, 1216, 644), new Color(.035f, .06f, .085f));
        Fill(new Rect(32, 38, 1216, 3), new Color(.24f, .86f, 1));
        GUI.Label(new Rect(58, 58, 620, 20), "INCURSÃO  /  CONSULTA", _eyebrow);
        GUI.Label(new Rect(58, 84, 920, 54), _page == PausePage.Map ? "Mapa da incursão" : "Upgrades adquiridos", _title);
        if (GUI.Button(new Rect(1032, 84, 190, 44), "Voltar  ·  Esc", _button)) ReturnToPauseHome();
        GUI.Label(new Rect(58, 142, 950, 18), "Esc / B: voltar  ·  Start: continuar", _eyebrow);
        var area = new Rect(58, 165, 1164, 485);
        if (_page == PausePage.Map && _map && _map.HasPauseMap) _map.DrawPauseMap(area);
        else if (_page == PausePage.Upgrades && _rewards) _rewards.DrawPauseBuild(area);
        else GUI.Label(area, "Nenhuma informação disponível nesta área.", _body);
    }

    private void DrawMenuPage()
    {
        Fill(new Rect(354, 42, 572, 636), new Color(.035f, .06f, .085f));
        Fill(new Rect(354, 42, 572, 3), new Color(.24f, .86f, 1));
        GUI.Label(new Rect(390, 66, 500, 22), "TECH GUY  /  PAUSA", _eyebrow);
        GUI.Label(new Rect(390, 103, 500, 55), _page == PausePage.Controls ? "Controles" :
            _confirmation == 1 ? "Voltar ao menu?" : _confirmation == 2 ? "Sair do jogo?" :
            _confirmation == 3 ? "Abandonar incursão?" : "Jogo pausado", _title);
        if (_page == PausePage.Controls) { DrawControls(); return; }
        if (_confirmation != 0) { DrawConfirmation(); return; }
        GUI.Label(new Rect(390, 163, 500, 42), "Consulte sua build e o mapa antes de continuar.", _body);
        if (GUI.Button(new Rect(390, 220, 500, 48), "Continuar  ·  Esc / Start", _button)) Resume();
        bool enabled = GUI.enabled;
        GUI.enabled = enabled && _rewards;
        if (GUI.Button(new Rect(390, 280, 500, 48), "Upgrades adquiridos", _button)) TryShowUpgrades();
        GUI.enabled = enabled && _map && _map.HasPauseMap;
        if (GUI.Button(new Rect(390, 340, 500, 48), "Visualizar mapa", _button)) TryShowMap();
        GUI.enabled = enabled;
        if (GUI.Button(new Rect(390, 400, 500, 48), "Controles", _button))
        {
            _page = PausePage.Controls;
            _bindings = GamePreferences.ReadBindings();
        }
        float nextY = 460f;
        if (_map && _map.CanAbortRun)
        {
            if (GUI.Button(new Rect(390, nextY, 500, 48), "Abandonar incursão", _button)) _confirmation = 3;
            nextY += 60f;
        }
        if (GUI.Button(new Rect(390, nextY, 500, 48), "Menu inicial", _button)) _confirmation = 1;
        if (GUI.Button(new Rect(390, nextY + 60f, 500, 48), "Sair do jogo", _button)) _confirmation = 2;
        if (_tutorial && GUI.Button(new Rect(390, nextY + 120f, 500, 48), "Pular tutorial e ir ao Nexus", _button))
        {
            Resume();
            _tutorial.Skip();
        }
    }

    private void DrawControls()
    {
        _remapping.Draw(new Rect(390, 200, 500, 292), _bindings);
        if (GUI.Button(new Rect(390, 540, 145, 44), "Voltar", _button)) ReturnToPauseHome();
        if (GUI.Button(new Rect(548, 540, 150, 44), "Padrões", _button))
        {
            _remapping.Cancel();
            _bindings = GamePreferences.UsesDirectControls ? GamePreferences.DefaultDirectBindings() : GamePreferences.DefaultBindings();
        }
        if (GUI.Button(new Rect(711, 540, 179, 44), "Aplicar", _button))
        {
            GamePreferences.SaveBindings(_bindings);
            ReturnToPauseHome();
        }
    }

    private void DrawConfirmation()
    {
        GUI.Label(new Rect(390, 180, 500, 112), _confirmation == 3 ?
            "Você retornará ao Nexus sem concluir esta incursão. Os upgrades e o progresso desta incursão serão perdidos. Moedas, equipamentos e configurações salvos serão mantidos." :
            "O progresso da incursão atual será encerrado. Moedas, equipamentos e configurações salvos serão mantidos.", _body);
        if (GUI.Button(new Rect(390, 330, 500, 52), "Cancelar", _button)) _confirmation = 0;
        if (GUI.Button(new Rect(390, 406, 500, 52), _confirmation == 1 ? "Confirmar e voltar ao menu" :
            _confirmation == 3 ? "Confirmar e voltar ao Nexus" : "Confirmar e sair", _button))
        {
            if (_confirmation == 1) ReturnToMainMenu();
            else if (_confirmation == 3) TryRequestRunAbort();
            else Quit();
        }
    }

    private static void Fill(Rect rect, Color color)
    {
        Color previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }
}
