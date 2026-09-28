using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Reward cards and passive build strip. RunBoons owns input blocking.</summary>
public sealed class RunRewardUI : MonoBehaviour
{
    private RunBoons _run;
    private GUIStyle _heading, _title, _body, _small, _badge, _symbol;
    private int _selected, _knownRoom = -1, _knownCount = -1;
    private readonly List<RunBoons.Offer> _owned = new List<RunBoons.Offer>();
    private readonly Dictionary<string, RunModifierPresentation> _presentation = new Dictionary<string, RunModifierPresentation>();
    private Vector2 _lastPointer;
    private static readonly Color Ink = new Color(.035f,.045f,.065f,.98f);
    private static readonly Color Muted = new Color(.6f,.67f,.75f);
    public void Bind(RunBoons run) => _run = run;
    public float BuildTop { get; set; } = 96f;

    private void Update()
    {
        if (!_run || !_run.IsChoosing) return;
        if (_knownRoom != _run.RewardRoom) { _knownRoom = _run.RewardRoom; _selected = 0; }
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.digit1Key.wasPressedThisFrame) _run.Choose(0);
        else if (keyboard.digit2Key.wasPressedThisFrame) _run.Choose(1);
        else if (keyboard.digit3Key.wasPressedThisFrame) _run.Choose(2);
        else if (keyboard.leftArrowKey.wasPressedThisFrame) _selected = Mathf.Max(0, _selected - 1);
        else if (keyboard.rightArrowKey.wasPressedThisFrame) _selected = Mathf.Min(_run.Choices.Count - 1, _selected + 1);
        else if (keyboard.enterKey.wasPressedThisFrame) _run.Choose(_selected);
    }
    private RunModifierPresentation Present(RunBoons.Offer offer)
    {
        if (!_presentation.TryGetValue(offer.Id, out var view))
            _presentation[offer.Id] = view = RunModifierPresentation.For(offer);
        return view;
    }
    private static GUIStyle Style(int size, FontStyle weight)
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, wordWrap = true, richText = true, padding = new RectOffset(0,0,0,0) };
        style.normal.textColor = new Color(.92f,.94f,.97f); return style;
    }
    private void Styles()
    {
        if (_heading != null) return;
        _heading = Style(34, FontStyle.Bold); _title = Style(23, FontStyle.Bold);
        _body = Style(17, FontStyle.Normal); _small = Style(12, FontStyle.Normal);
        _badge = Style(12, FontStyle.Bold); _badge.alignment = TextAnchor.MiddleCenter;
        _symbol = Style(28, FontStyle.Bold); _symbol.alignment = TextAnchor.MiddleCenter;
    }
    private static void Fill(Rect rect, Color color)
    { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
    private static void Text(Rect rect, string text, GUIStyle style, Color color)
    { Color old = style.normal.textColor; style.normal.textColor = color; GUI.Label(rect,text,style); style.normal.textColor = old; }
    private void Description(Rect rect, string value)
    {
        string text = RunModifierPresentation.Emphasize(value);
        int originalSize = _body.fontSize;
        while (_body.fontSize > 14 && _body.CalcHeight(new GUIContent(text), rect.width) > rect.height) _body.fontSize--;
        GUI.Label(rect, text, _body);
        _body.fontSize = originalSize;
    }
    private void OnGUI()
    {
        if (!_run) return;
        Styles();
        Matrix4x4 previous = GUI.matrix;
        Color oldColor = GUI.color;
        float scale = Mathf.Max(.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float width = Screen.width / scale, height = Screen.height / scale;
        try
        {
            if (_run.IsChoosing) DrawChoices(width, height);
            else DrawBuild(width, height);
        }
        finally { GUI.matrix = previous; GUI.color = oldColor; }
    }
    private void DrawChoices(float width, float height)
    {
        Fill(new Rect(0,0,width,height), new Color(.012f,.018f,.03f,.97f));
        float left = (width - 1100) * .5f, top = (height - 650) * .5f;
        Text(new Rect(left,top+4,700,22), "RECOMPENSA DE SALA  /  " + _run.RewardRoom.ToString("00"), _small, new Color(.9f,.73f,.43f));
        GUI.Label(new Rect(left,top+33,1000,50), "Escolha sua próxima evolução", _heading);
        Text(new Rect(left,top+91,1000,28), "Uma escolha. Novas combinações. Os efeitos duram até o fim desta run.", _body, Muted);
        bool pointerMoved = Event.current.mousePosition != _lastPointer;
        if (Event.current.type == EventType.Repaint) _lastPointer = Event.current.mousePosition;
        for (int i = 0; i < _run.Choices.Count; i++)
        {
            var rect = new Rect(left + i * 374, top + 145, 352, 450);
            if (rect.Contains(Event.current.mousePosition) && pointerMoved) _selected = i;
            if (DrawCard(rect, _run.Choices[i], i)) { _run.Choose(i); break; }
        }
        Text(new Rect(left,top+610,1100,24), "[1] [2] [3] escolher     •     ← → navegar + Enter     •     ou clique no cartão", _body, Muted);
        Text(new Rect(left,top+646,1100,20), _run.Acquired.Count + " modificadores adquiridos nesta run", _small, Muted);
    }
    private bool DrawCard(Rect rect, RunBoons.Offer offer, int index)
    {
        var view = Present(offer);
        bool active = index == _selected;
        Fill(new Rect(rect.x+4,rect.y+7,rect.width,rect.height), new Color(0,0,0,.5f));
        Fill(rect, active ? view.Accent : new Color(.18f,.23f,.3f));
        Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4), active ? new Color(.065f,.085f,.12f) : Ink);
        Fill(new Rect(rect.x+2,rect.y+2,rect.width-4,4), view.Accent);
        float x = rect.x + 24, y = rect.y + 24, w = rect.width - 48;
        Text(new Rect(x,y,w-80,20), view.Category, _small, view.Accent);
        int rank = RunModifierPresentation.Count(_run,offer.Id);
        string level = view.MaxRank == 1 ? "ÚNICO" : "NÍVEL " + (rank + 1) + (view.MaxRank > 0 ? "/" + view.MaxRank : "");
        Text(new Rect(x+w-90,y,90,20), level, _badge, view.Accent);
        Fill(new Rect(x,y+35,60,60), new Color(view.Accent.r,view.Accent.g,view.Accent.b,.12f));
        Text(new Rect(x,y+35,60,60), view.Symbol, _symbol, view.Accent);
        Text(new Rect(x+76,y+38,w-76,20), "AFETA", _small, Muted);
        Text(new Rect(x+76,y+61,w-76,30), view.Scope, _body, view.Accent);
        GUI.Label(new Rect(x,y+113,w,60), offer.Title, _title);
        Fill(new Rect(x,y+184,w,1), new Color(.2f,.26f,.34f));
        Description(new Rect(x,y+199,w,100), view.Description);
        Text(new Rect(x,y+304,w,44), RunModifierPresentation.Hint(_run, offer.Id), _small, Muted);
        Fill(new Rect(x,rect.yMax-54,w,32), active ? view.Accent : new Color(.12f,.16f,.22f));
        Text(new Rect(x,rect.yMax-54,w,32), "[" + (index+1) + "]   " + (rank > 0 ? "MELHORAR" : "ADQUIRIR"), _badge, active ? Ink : Color.white);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }
    private void DrawBuild(float width, float height)
    {
        if (_knownCount != _run.Acquired.Count)
        {
            _owned.Clear(); var seen = new HashSet<string>();
            foreach (var offer in _run.Acquired) if (seen.Add(offer.Id)) _owned.Add(offer);
            _knownCount = _run.Acquired.Count;
        }
        if (_owned.Count == 0) return;
        float x = 24, y = BuildTop;
        Text(new Rect(x,y,480,18), "SUA BUILD  /  " + _run.Acquired.Count + " AQUISIÇÕES   ·   passe o mouse para detalhes", _small, Muted);
        RunBoons.Offer hovered = null; Rect hoveredRect = default;
        int columns = 3;
        for (int i = 0; i < _owned.Count; i++)
        {
            var offer = _owned[i]; var view = Present(offer);
            var tile = new Rect(x + i % columns * 158, y + 30 + i / columns * 56, 150, 50);
            Fill(tile, view.Accent); Fill(new Rect(tile.x+1,tile.y+1,148,48), Ink);
            Text(new Rect(tile.x,tile.y,36,50), view.Symbol, _badge, view.Accent);
            GUI.Label(new Rect(tile.x+38,tile.y+5,106,28), offer.Title, _small);
            Text(new Rect(tile.x+38,tile.y+33,106,14), "NÍVEL " + RunModifierPresentation.Count(_run,offer.Id), _small, view.Accent);
            if (tile.Contains(Event.current.mousePosition)) { hovered = offer; hoveredRect = tile; }
        }
        if (hovered == null) return;
        var data = Present(hovered);
        float tooltipX = Mathf.Min(hoveredRect.x, width - 364);
        float tooltipY = Mathf.Min(hoveredRect.yMax+12, height - 250);
        var panel = new Rect(tooltipX,tooltipY,340,230);
        Fill(panel, data.Accent); Fill(new Rect(panel.x+1,panel.y+1,338,228), Ink);
        Text(new Rect(panel.x+18,panel.y+16,304,20), data.Category + "  /  " + data.Scope, _small, data.Accent);
        GUI.Label(new Rect(panel.x+18,panel.y+46,304,55), hovered.Title, _title);
        Description(new Rect(panel.x+18,panel.y+110,304,100), data.Description);
    }
}
