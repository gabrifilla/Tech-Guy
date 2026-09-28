using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>Scene-local title screen, settings draft and display confirmation.</summary>
public sealed class MainMenuUI : MonoBehaviour
{
    public enum Page { Home, Settings, Controls, Quit }
    [SerializeField] private string _tutorialScene = "PrologueTutorial";
    [SerializeField] private string _lobbyScene = "NexusLobby";
    private Page _page;
    private GamePreferences.Options _draft, _saved;
    private readonly List<Vector2Int> _resolutions = new List<Vector2Int>();
    private GUIStyle _logo, _heading, _body, _small, _button, _value;
    private int _selected, _tab;
    private bool _loading, _confirmDisplay;
    private float _displayDeadline;
    private string _message;
    private static readonly Color Cyan = new Color(.24f,.86f,1f);
    private static readonly Color Muted = new Color(.56f,.66f,.74f);
    private static readonly Color Ink = new Color(.025f,.044f,.067f);
    public Page CurrentPage => _page;
    public bool AwaitingDisplayConfirmation => _confirmDisplay;

    private void Awake()
    {
        Time.timeScale = 1;
        Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        _saved = GamePreferences.Read(); _draft = _saved.Copy();
        foreach (var resolution in Screen.resolutions) AddResolution(resolution.width, resolution.height);
        AddResolution(1280,720); AddResolution(1600,900); AddResolution(1920,1080);
        AddResolution(_saved.Width,_saved.Height);
        _resolutions.Sort((a,b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
    }
    private void AddResolution(int width, int height)
    {
        var size = new Vector2Int(width,height);
        if (width >= 640 && height >= 480 && !_resolutions.Contains(size)) _resolutions.Add(size);
    }
    private void Update()
    {
        if (_confirmDisplay && Time.unscaledTime >= _displayDeadline) RevertDisplay();
        if (_loading || Keyboard.current == null) return;
        var key = Keyboard.current;
        if (key.escapeKey.wasPressedThisFrame)
        {
            if (_confirmDisplay) RevertDisplay();
            else if (_page == Page.Home) Open(Page.Quit);
            else Open(Page.Home);
            return;
        }
        if (_page != Page.Home) return;
        if (key.downArrowKey.wasPressedThisFrame) _selected = (_selected + 1) % 5;
        if (key.upArrowKey.wasPressedThisFrame) _selected = (_selected + 4) % 5;
        if (key.enterKey.wasPressedThisFrame) Activate(_selected);
    }
    public void Open(Page page)
    {
        if (_confirmDisplay) RevertDisplay();
        _page = page; _message = null;
        if (page == Page.Settings) { _saved = GamePreferences.Read(); _draft = _saved.Copy(); }
    }
    public void StartJourney(bool replayTutorial = false)
    {
        if (_loading) return;
        string destination = replayTutorial || !GamePreferences.TutorialSeen ? _tutorialScene : _lobbyScene;
        if (!Application.CanStreamedLevelBeLoaded(destination))
        { _message = "Não foi possível abrir a próxima área."; Debug.LogError("Menu destination missing: " + destination, this); return; }
        _loading = true; SceneManager.LoadSceneAsync(destination);
    }
    private void Activate(int index)
    {
        switch (index)
        {
            case 0: StartJourney(); break;
            case 1: StartJourney(true); break;
            case 2: Open(Page.Settings); break;
            case 3: Open(Page.Controls); break;
            case 4: Open(Page.Quit); break;
        }
    }
    public void ApplySettings()
    {
        bool displayChanged = _draft.Width != _saved.Width || _draft.Height != _saved.Height || _draft.Fullscreen != _saved.Fullscreen;
        if (displayChanged && !Application.isEditor)
        {
            GamePreferences.Apply(_draft); _confirmDisplay = true; _displayDeadline = Time.unscaledTime + 15;
        }
        else CommitSettings();
    }
    public void CommitSettings()
    {
        GamePreferences.Save(_draft); _saved = _draft.Copy(); _confirmDisplay = false;
        _message = "Configurações salvas.";
    }
    public void RevertDisplay()
    {
        GamePreferences.Apply(_saved); _draft = _saved.Copy(); _confirmDisplay = false;
        _message = "Alterações de vídeo revertidas.";
    }
    private void OnDestroy() { if (_confirmDisplay) GamePreferences.Apply(_saved); }

    private static GUIStyle Style(int size, bool bold = false) => new GUIStyle(GUI.skin.label)
    { fontSize = size, fontStyle = bold ? FontStyle.Bold : FontStyle.Normal, wordWrap = true, normal = { textColor = Color.white } };
    private void Styles()
    {
        if (_logo != null) return;
        _logo = Style(76,true); _heading = Style(32,true); _body = Style(18); _small = Style(13);
        _button = Style(19,true); _button.alignment = TextAnchor.MiddleLeft; _button.padding = new RectOffset(22,12,0,0);
        _value = Style(17,true); _value.alignment = TextAnchor.MiddleCenter;
    }
    private static void Fill(Rect rect, Color color)
    { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color = old; }
    private static void Label(Rect rect, string text, GUIStyle style, Color color)
    { Color old = style.normal.textColor; style.normal.textColor = color; GUI.Label(rect,text,style); style.normal.textColor = old; }
    private bool Button(Rect rect, string text, bool highlighted = false)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        Fill(rect, highlighted || hover ? new Color(.1f,.25f,.32f) : new Color(.06f,.1f,.14f));
        Fill(new Rect(rect.x,rect.y,highlighted || hover ? 4 : 1,rect.height),highlighted || hover ? Cyan : new Color(.18f,.29f,.35f));
        Label(rect,text,_button,highlighted || hover ? Color.white : new Color(.73f,.81f,.85f));
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }
    private void OnGUI()
    {
        Styles(); Matrix4x4 old = GUI.matrix; Color color = GUI.color; int depth = GUI.depth;
        float scale = Mathf.Max(.1f,Mathf.Min(Screen.width/1280f,Screen.height/720f));
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-720*scale)/2,0),Quaternion.identity,Vector3.one*scale);
        GUI.depth = -10;
        try
        {
            Fill(new Rect(-Screen.width/scale,-Screen.height/scale,Screen.width/scale*3,Screen.height/scale*3),Ink);
            DrawBackdrop();
            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && !_confirmDisplay && !_loading;
            if (_page == Page.Home) DrawHome();
            else if (_page == Page.Settings) DrawSettings();
            else if (_page == Page.Controls) DrawControls();
            else DrawQuit();
            GUI.enabled = wasEnabled;
            if (_loading) { Fill(new Rect(0,0,1280,720),new Color(.015f,.03f,.05f,.96f)); Label(new Rect(80,320,1100,70),"Conectando ao Nexus…",_heading,Cyan); }
            if (_confirmDisplay) DrawDisplayConfirmation();
        }
        finally { GUI.matrix = old; GUI.color = color; GUI.depth = depth; }
    }
    private void DrawBackdrop()
    {
        for (int i=0; i<24; i++) Fill(new Rect(0,i*32,1280,1),new Color(.09f,.18f,.23f,.22f));
        for (int i=0; i<40; i++) Fill(new Rect(i*32,0,1,720),new Color(.09f,.18f,.23f,.16f));
        Fill(new Rect(64,48,38,3),Cyan);
        Label(new Rect(113,38,800,30),"NEXUS  /  PROTOCOLO DE CONEXÃO",_small,Muted);
        Label(new Rect(64,677,880,24),"TECH GUY   •   EM DESENVOLVIMENTO",_small,Muted);
        Label(new Rect(1000,677,240,24),"MOUSE + TECLADO",_small,Muted);
    }
    private void DrawHome()
    {
        Label(new Rect(64,107,600,100),"TECH GUY",_logo,Color.white);
        Label(new Rect(69,207,490,48),"Prepare seu arsenal. Descubra novas combinações.",_body,Muted);
        string[] choices = { GamePreferences.TutorialSeen ? "Continuar no Nexus" : "Iniciar jornada", "Repetir prólogo", "Configurações", "Controles", "Sair do jogo" };
        for (int i=0;i<choices.Length;i++)
        {
            var rect = new Rect(70,291+i*62,420,52);
            if (Event.current.type == EventType.MouseMove && rect.Contains(Event.current.mousePosition)) _selected = i;
            if (Button(rect,choices[i],_selected==i)) Activate(i);
        }
        Vector2 center = new Vector2(924,324);
        for (int ring=0;ring<3;ring++)
        {
            float radius = 92 + ring*36;
            for (int i=0;i<48;i++)
            {
                if (i%12>8) continue;
                float angle = i*7.5f + Time.unscaledTime*(ring==1 ? -3 : 2);
                Matrix4x4 before = GUI.matrix; GUIUtility.RotateAroundPivot(angle,center);
                Fill(new Rect(center.x+radius,center.y,ring==1 ? 12 : 4,8),new Color(Cyan.r,Cyan.g,Cyan.b,.4f+ring*.2f)); GUI.matrix = before;
            }
        }
        Label(new Rect(861,298,160,64),"N / 01",_heading,Cyan);
        Fill(new Rect(710,532,428,2),new Color(.18f,.38f,.45f));
        Label(new Rect(710,551,428,28),GamePreferences.TutorialSeen ? "CONEXÃO RESTABELECIDA" : "CALIBRAÇÃO PENDENTE",_small,Cyan);
        Label(new Rect(710,585,428,66),GamePreferences.TutorialSeen ? "Seu próximo destino é o Nexus. O prólogo pode ser revisitado a qualquer momento." : "Aprenda a se mover, lutar e escolher modificadores antes de acessar o Nexus.",_body,Muted);
        if (!string.IsNullOrEmpty(_message)) Label(new Rect(70,618,560,40),_message,_small,new Color(1,.7f,.4f));
    }
    private void Panel(string title, string subtitle)
    {
        Fill(new Rect(154,92,972,552),new Color(.035f,.06f,.085f,.98f));
        Fill(new Rect(154,92,972,3),Cyan);
        Label(new Rect(188,118,900,48),title,_heading,Color.white);
        Label(new Rect(190,170,900,32),subtitle,_small,Muted);
    }
    private void DrawSettings()
    {
        Panel("Configurações","Ajuste sua experiência. As alterações só são salvas ao aplicar.");
        if (Button(new Rect(190,214,210,43),"Vídeo",_tab==0)) _tab=0;
        if (Button(new Rect(414,214,210,43),"Áudio",_tab==1)) _tab=1;
        if (_tab == 0)
        {
            RowLabel(282,"Modo de tela");
            if (Button(new Rect(635,282,440,43),_draft.Fullscreen ? "Tela cheia sem bordas" : "Janela")) _draft.Fullscreen=!_draft.Fullscreen;
            RowLabel(338,"Resolução");
            int current = _resolutions.IndexOf(new Vector2Int(_draft.Width,_draft.Height));
            int next = Cycle(338,_draft.Width+" × "+_draft.Height,current,_resolutions.Count);
            if (next != current) { _draft.Width=_resolutions[next].x; _draft.Height=_resolutions[next].y; }
            RowLabel(394,"Qualidade gráfica");
            _draft.Quality = Cycle(394,QualitySettings.names[_draft.Quality],_draft.Quality,QualitySettings.names.Length);
            RowLabel(450,"Sincronização vertical");
            if (Button(new Rect(635,450,440,43),_draft.VSync ? "Ativada" : "Desativada")) _draft.VSync=!_draft.VSync;
            Label(new Rect(190,510,880,36),Application.isEditor ? "No Editor, resolução e modo de tela são usados apenas no jogo compilado." : "Mudanças de tela pedem confirmação e revertem após 15 segundos.",_small,Muted);
        }
        else
        {
            RowLabel(300,"Volume geral");
            _draft.Volume = GUI.HorizontalSlider(new Rect(635,315,350,24),_draft.Volume,0,1);
            Label(new Rect(990,300,85,34),Mathf.RoundToInt(_draft.Volume*100)+"%",_value,Cyan);
            Label(new Rect(190,376,830,64),"Controla o volume de todos os sons do jogo. Use 0% para silenciar.",_body,Muted);
        }
        if (Button(new Rect(190,572,200,44),"Voltar")) Open(Page.Home);
        if (Button(new Rect(408,572,244,44),"Restaurar padrões")) { _draft=GamePreferences.Defaults(); AddResolution(_draft.Width,_draft.Height); _message="Padrões carregados. Clique em Aplicar para salvar."; }
        if (Button(new Rect(859,572,216,44),"Aplicar",true)) ApplySettings();
        if (!string.IsNullOrEmpty(_message)) Label(new Rect(190,646,900,26),_message,_small,Cyan);
    }
    private void RowLabel(float y,string text) => Label(new Rect(190,y+8,420,35),text,_body,Color.white);
    private int Cycle(float y,string text,int index,int count)
    {
        if (Button(new Rect(635,y,54,43),"‹")) index=(index+count-1)%count;
        Label(new Rect(695,y,320,43),text,_value,Cyan);
        if (Button(new Rect(1021,y,54,43),"›")) index=(index+1)%count;
        return Mathf.Max(0,index);
    }
    private void DrawControls()
    {
        Panel("Controles","Aponte o cursor na direção em que deseja agir.");
        string[] keys = { "BOTÃO DIREITO", "BOTÃO ESQUERDO", "ESPAÇO", "Q / W / E", "R", "1 / 2 / 3" };
        string[] actions = { "Mover até o ponto indicado", "Atacar • segure para continuar", "Esquivar", "Usar habilidades da arma", "Usar a habilidade especial da arma", "Escolher um modificador na recompensa" };
        for (int i=0;i<keys.Length;i++)
        {
            float y=215+i*49;
            Fill(new Rect(190,y,885,42),i%2==0 ? new Color(.065f,.105f,.14f) : new Color(.045f,.08f,.11f));
            Label(new Rect(203,y+9,250,28),keys[i],_small,Cyan);
            Label(new Rect(475,y+6,590,32),actions[i],_body,Color.white);
        }
        Label(new Rect(190,522,880,40),"Mire perto dos inimigos: a assistência do cursor ajuda a selecionar o alvo.",_small,Muted);
        if (Button(new Rect(190,572,210,44),"Voltar")) Open(Page.Home);
        if (Button(new Rect(815,572,260,44),"Praticar no prólogo",true)) StartJourney(true);
    }
    private void DrawQuit()
    {
        Panel("Sair do jogo?","Seu equipamento e suas configurações salvas serão mantidos.");
        Label(new Rect(190,290,830,110),"A conexão com o Nexus estará aqui quando você voltar.",_heading,Muted);
        if (Button(new Rect(190,526,320,52),"Continuar aqui",true)) Open(Page.Home);
        if (Button(new Rect(755,526,320,52),"Sair"))
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
    private void DrawDisplayConfirmation()
    {
        // Modal consumes pointer events before the next frame can activate a background control.
        Fill(new Rect(0,0,1280,720),new Color(0,0,0,.93f));
        Fill(new Rect(330,220,620,270),Ink);
        Label(new Rect(365,252,550,60),"Manter estas configurações?",_heading,Color.white);
        Label(new Rect(365,328,550,44),"Revertendo em "+Mathf.CeilToInt(_displayDeadline-Time.unscaledTime)+" segundos.",_body,Muted);
        if (Button(new Rect(365,413,245,48),"Manter",true)) CommitSettings();
        if (Button(new Rect(636,413,280,48),"Reverter")) RevertDisplay();
    }
}
