using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Player-owned pause. Restores the previous clock/audio state on every exit path.</summary>
[DefaultExecutionOrder(-1000)]
public sealed class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private string _mainMenuScene = "MainMenu";
    private readonly ControlRemappingPanel _remapping = new ControlRemappingPanel();
    private KeyCode[] _bindings;
    private bool _settings;
    private PlayerActor _player;
    private TutorialDirector _tutorial;
    private float _previousTimeScale;
    private bool _previousAudioPause, _loading;
    private int _resumeFrame = -1, _confirmation;
    private GUIStyle _title, _body, _button;
    public bool IsPaused { get; private set; }
    public bool BlocksInput => IsPaused || _loading || Time.frameCount == _resumeFrame;
    public void BindTutorial(TutorialDirector tutorial) => _tutorial = tutorial;
    private void Awake()
    {
        _player = GetComponent<PlayerActor>();
        if (TryGetComponent(out AbilityHolder holder)) holder.BindPause(this);
    }
    private void Update()
    {
        if (_settings && _remapping.Update(_bindings)) return;
        if (_loading || !_player || _player.IsDead || Keyboard.current == null) return;
        if (!Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (_settings) { _settings = false; _remapping.Cancel(); return; }
        if (_confirmation != 0) _confirmation = 0;
        else if (IsPaused) Resume();
        else Pause();
    }
    public void Pause()
    {
        if (IsPaused || _loading || !_player || _player.IsDead) return;
        _previousTimeScale = Time.timeScale; _previousAudioPause = AudioListener.pause;
        IsPaused = true; Time.timeScale = 0; AudioListener.pause = true;
        _confirmation = 0;
    }
    public void Resume()
    {
        if (!IsPaused) return;
        _settings = false; _remapping.Cancel();
        IsPaused = false; _confirmation = 0; _resumeFrame = Time.frameCount;
        Time.timeScale = _previousTimeScale; AudioListener.pause = _previousAudioPause;
        if (TryGetComponent(out CharControlScript controls)) controls.RequireAttackRelease();
    }
    public void ReturnToMainMenu()
    {
        if (_loading || !Application.CanStreamedLevelBeLoaded(_mainMenuScene)) return;
        Resume(); _loading = true; SceneManager.LoadSceneAsync(_mainMenuScene);
    }
    private void Quit()
    {
        Resume(); _loading = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    private void OnDisable() => Resume();
    private void OnGUI()
    {
        if (!IsPaused) return;
        if (_title == null)
        {
            _title = new GUIStyle(GUI.skin.label) { fontSize=36, fontStyle=FontStyle.Bold };
            _body = new GUIStyle(GUI.skin.label) { fontSize=17, wordWrap=true };
            _button = new GUIStyle(GUI.skin.button) { fontSize=19, alignment=TextAnchor.MiddleLeft, padding=new RectOffset(24,16,0,0) };
        }
        Matrix4x4 matrix=GUI.matrix; Color color=GUI.color; int depth=GUI.depth;
        float scale=Mathf.Max(.1f,Mathf.Min(Screen.width/1280f,Screen.height/720f));
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,Vector3.one*scale);
        GUI.depth=-100;
        Fill(new Rect(-Screen.width/scale,-Screen.height/scale,Screen.width/scale*3,Screen.height/scale*3),new Color(.012f,.023f,.04f,.93f));
        Fill(new Rect(380,105,520,510),new Color(.035f,.06f,.085f));
        Fill(new Rect(380,105,520,3),new Color(.24f,.86f,1));
        if (_settings)
        {
            GUI.Label(new Rect(415,140,455,55), "Controles", _title);
            _remapping.Draw(new Rect(415,210,450,275), _bindings);
            if (GUI.Button(new Rect(415,510,140,42), "Voltar")) { _settings=false; _remapping.Cancel(); }
            if (GUI.Button(new Rect(565,510,140,42), "Padrões")) { _remapping.Cancel(); _bindings=GamePreferences.DefaultBindings(); }
            if (GUI.Button(new Rect(715,510,150,42), "Aplicar")) { GamePreferences.SaveBindings(_bindings); _remapping.Cancel(); _settings=false; }
            GUI.matrix=matrix; GUI.color=color; GUI.depth=depth;
            return;
        }
        GUI.Label(new Rect(415,140,455,55),_confirmation==0 ? "Jogo pausado" : _confirmation==1 ? "Voltar ao menu?" : "Sair do jogo?",_title);
        GUI.Label(new Rect(415,207,455,76),_confirmation==0 ? "Respire. A ação continua de onde você parou." :
            "O progresso da incursão atual será encerrado. Moedas, equipamentos e configurações salvos serão mantidos.",_body);
        if (_confirmation==0)
        {
            if (GUI.Button(new Rect(415,298,450,52),"Continuar  ·  Esc",_button)) Resume();
            if (GUI.Button(new Rect(415,366,450,52),"Menu inicial",_button)) _confirmation=1;
            if (GUI.Button(new Rect(415,434,450,52),"Sair do jogo",_button)) _confirmation=2;
            if (GUI.Button(new Rect(415,494,450,42),"Configurações de controles",_button)) { _settings=true; _bindings=GamePreferences.ReadBindings(); }
            if (_tutorial && GUI.Button(new Rect(415,548,450,52),"Pular tutorial e ir ao Nexus",_button)) { Resume(); _tutorial.Skip(); }
        }
        else
        {
            if (GUI.Button(new Rect(415,330,450,52),"Cancelar",_button)) _confirmation=0;
            if (GUI.Button(new Rect(415,406,450,52),_confirmation==1 ? "Confirmar e voltar ao menu" : "Confirmar e sair",_button))
            { if (_confirmation==1) ReturnToMainMenu(); else Quit(); }
        }
        GUI.matrix=matrix; GUI.color=color; GUI.depth=depth;
    }
    private static void Fill(Rect rect,Color color)
    { Color previous=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=previous; }
}
