using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Scene-owned uGUI/TMP renderer. No Store access, clock changes, or gameplay calculations.</summary>
public sealed class RunResultsView : MonoBehaviour
{
    private static readonly Color Background = new Color(.031f,.051f,.086f,1);
    private static readonly Color Panel = new Color(.075f,.11f,.165f,1);
    private static readonly Color TextColor = new Color(.945f,.914f,.843f,1);
    private static readonly Color Muted = new Color(.70f,.75f,.80f,1);
    private static readonly Color Gold = new Color(.83f,.71f,.46f,1);
    private static readonly Color Cyan = new Color(.51f,.85f,.90f,1);
    private TMP_FontAsset _font;
    private EventSystem _events;
    private RectTransform _sheet, _body, _content, _side, _detail;
    private TMP_Text _title, _subtitle, _identity, _duration, _damage, _received, _kills, _warning, _detailText, _sideText;
    private Button _manual, _close, _detailClose;
    private readonly List<Button> _tabs = new List<Button>();
    private readonly List<Button> _rows = new List<Button>();
    private GameObject _detailReturn;
    private readonly List<Button> _modalDisabled = new List<Button>();
    private RunResultsModel _model;
    private int _tab;
    public bool DetailOpen => _detail && _detail.gameObject.activeSelf;
    public ScrollRect List { get; private set; }
    public GameObject Sheet => _sheet ? _sheet.gameObject : null;
    public Button ManualButton => _manual;
    public Button CloseButton => _close;
    public IReadOnlyList<Button> Tabs => _tabs.AsReadOnly();
    public string VisibleText => string.Join("\n", GetComponentsInChildren<TMP_Text>().Where(x => x.gameObject.activeInHierarchy).Select(x => x.text));
    public void Build(TMP_FontAsset font, EventSystem events, Action open, Action close)
    {
        if (_sheet) return;
        _font=font; _events=events;
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=200;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        _manual=ButtonAt(transform,"Último resultado",1000,24,240,44,open); _manual.name="LastRunResult";
        _sheet=Box(transform,"Run result",Background);Stretch(_sheet,0,0,0,0);
        Label(_sheet,"TG  /  NEXUS  /  REGISTRO DE INCURSÃO",40,25,900,24,14,Gold);
        _title=Label(_sheet,"",40,53,940,52,38,TextColor);
        _subtitle=Label(_sheet,"",40,111,850,28,15,Muted);
        _identity=Label(_sheet,"",40,142,1170,25,13,Muted);
        _duration=Label(_sheet,"",1005,45,235,75,29,TextColor);_duration.alignment=TextAlignmentOptions.TopRight;
        BoxAt(_sheet,Panel,40,177,1200,82);
        _damage=Metric("Dano efetivo causado",40);_kills=Metric("Inimigos derrotados",440);_received=Metric("Dano recebido",840);
        string[] labels={"Visão geral","Fontes de dano","Build e percurso"};
        for(int i=0;i<3;i++){int index=i;_tabs.Add(ButtonAt(_sheet,labels[i],40+i*235,269,225,38,()=>SelectTab(index)));}
        _warning=Label(_sheet,"",40,312,1200,43,13,new Color(.94f,.77f,.53f));
        _body=Box(_sheet,"Result content",Color.clear);Place(_body,40,355,1200,278);
        var scrollRoot=Box(_body,"Scrollable facts",Color.clear);Stretch(scrollRoot,0,0,0,0);
        List=scrollRoot.gameObject.AddComponent<ScrollRect>();List.horizontal=false;List.movementType=ScrollRect.MovementType.Clamped;List.scrollSensitivity=38;
        var viewport=Box(scrollRoot,"Viewport",Color.clear);Stretch(viewport,0,0,17,0);viewport.gameObject.AddComponent<RectMask2D>();
        _content=Box(viewport,"Rows",Color.clear);_content.anchorMin=new Vector2(0,1);_content.anchorMax=Vector2.one;_content.pivot=new Vector2(.5f,1);_content.sizeDelta=Vector2.zero;
        var layout=_content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=9;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;layout.childForceExpandWidth=true;
        var fit=_content.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        List.content=_content;List.viewport=viewport;
        var barRoot=Box(scrollRoot,"Scroll",new Color(.16f,.22f,.29f));barRoot.anchorMin=new Vector2(1,0);barRoot.anchorMax=Vector2.one;barRoot.pivot=new Vector2(1,.5f);barRoot.sizeDelta=new Vector2(10,0);
        var handle=Box(barRoot,"Handle",Muted);Stretch(handle,0,0,0,0);var scrollbar=barRoot.gameObject.AddComponent<Scrollbar>();scrollbar.handleRect=handle;scrollbar.targetGraphic=handle.GetComponent<Image>();scrollbar.direction=Scrollbar.Direction.BottomToTop;List.verticalScrollbar=scrollbar;List.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        _side=Box(_body,"Overview context",Panel);_side.anchorMin=new Vector2(.67f,0);_side.anchorMax=Vector2.one;_side.offsetMin=Vector2.zero;_side.offsetMax=Vector2.zero;
        _sideText=Label(_side,"",14,12,365,245,16,TextColor);
        Label(_sheet,"Efeitos já incluídos no total.  ·  Role para ver mais.  ·  Esc: voltar",40,640,850,28,13,Muted);
        Label(_sheet,"REGISTRO EM CONSULTA  /  disponível no Nexus",40,684,850,20,12,Muted);
        _close=ButtonAt(_sheet,"Continuar no Nexus",982,657,258,46,close);
        _detail=Box(_sheet,"Ability details",new Color(.01f,.02f,.04f,.98f));Stretch(_detail,190,85,190,85);
        _detailText=Label(_detail,"",25,25,820,410,18,TextColor);
        _detailClose=ButtonAt(_detail,"Fechar detalhes",25,470,230,44,CloseDetail);
        _detail.gameObject.SetActive(false);_sheet.gameObject.SetActive(false);
        if(!_events.firstSelectedGameObject)_events.firstSelectedGameObject=_manual.gameObject;Focus(_manual.gameObject);
    }
    private TMP_Text Metric(string caption,float x)
    { Label(_sheet,caption,x+15,187,350,22,15,Muted);return Label(_sheet,"—",x+15,212,350,40,30,Gold); }
    public void Show(RunResultsModel model)
    {
        _model=model;_sheet.gameObject.SetActive(true);_manual.gameObject.SetActive(false);_detail.gameObject.SetActive(false);
        _title.text=model.Title;_title.color=model.Outcome=="Victory"?Gold:model.Outcome=="Death"?new Color(.90f,.63f,.60f):TextColor;
        _subtitle.text=model.Subtitle;_identity.text=model.Identity;_duration.text=model.Duration+"\n<size=24>Inclui pausas</size>";_duration.richText=true;
        _damage.text=model.Damage;_received.text=model.Received;_kills.text=model.Kills;_warning.text=model.Warning;
        for(int i=0;i<3;i++)_tabs[i].interactable=model.Available;
        SelectTab(0);Focus(_close.gameObject);
    }
    public void Hide()
    { if(DetailOpen)CloseDetail();_sheet.gameObject.SetActive(false);_detail.gameObject.SetActive(false);_manual.gameObject.SetActive(true);Focus(_manual.gameObject); }
    public void SetManualAvailable(bool available) { if(_manual)_manual.interactable=available; }
    public void SelectTab(int index)
    {
        _tab=index;
        for(int i=_content.childCount-1;i>=0;i--){var go=_content.GetChild(i).gameObject;go.SetActive(false);Destroy(go);}
        _rows.Clear();
        for(int i=0;i<_tabs.Count;i++)_tabs[i].GetComponent<Image>().color=i==index?new Color(.25f,.22f,.16f):Panel;
        _side.gameObject.SetActive(index==0&&_model.Available);
        var rt=(RectTransform)List.transform;rt.anchorMin=Vector2.zero;rt.anchorMax=new Vector2(index==0&&_model.Available ? .64f : 1,1);rt.offsetMin=rt.offsetMax=Vector2.zero;
        Canvas.ForceUpdateCanvases();
        if(!_model.Available)AddText("Sem estatísticas disponíveis\n\nA ausência de resumo não indica vitória, morte ou abandono. Você pode continuar no Nexus.",110);
        else if(index==0)
        {
            AddText("Dano por habilidade   /   Nativo + secundário incluído",22);
            if(_model.Total==0)AddText("Nenhum dano efetivo registrado. Sem percentuais ou destaque.",75);
            foreach(var row in _model.Abilities)
            {
                var button=RowButton(row.Label+"   ·   "+RunResultsModel.Number(row.Total)+" HP   ·   "+RunResultsModel.Number(_model.Total>0?row.Total/_model.Total*100:0)+"%",43);
                button.onClick.AddListener(()=>OpenDetail(row.Detail,button.gameObject));
                button.GetComponentInChildren<TMP_Text>().rectTransform.offsetMin=new Vector2(18,-34);
                Label(button.transform,"Nativo "+RunResultsModel.Number(row.Native)+"  +  secundário "+RunResultsModel.Number(row.Secondary),12,23,680,18,12,Muted);
                var track=Box(button.transform,"Total share",new Color(.15f,.2f,.26f));track.anchorMin=new Vector2(.018f,.08f);track.anchorMax=new Vector2(.98f,.14f);track.offsetMin=track.offsetMax=Vector2.zero;
                Fill(track,0,(float)(_model.Total>0?row.Native/_model.Total:0),Gold);Fill(track,(float)(_model.Total>0?row.Native/_model.Total:0),(float)(_model.Total>0?row.Total/_model.Total:0),Cyan);
            }
            var leader=_model.Abilities.FirstOrDefault(x=>!x.Key.EndsWith(":Unattributed")&&x.Total>0);
            _sideText.text="MAIOR DANO REGISTRADO\n"+(leader==null?"Sem destaque":leader.Label+"\n"+RunResultsModel.Number(leader.Total)+" HP, incluindo filhos")+"\n\nADQUIRIDOS NA RUN\n"+string.Join("\n",_model.Build.Take(3))+"\n\nAquisição não comprova dano causado. Consulte fontes e build.";
        }
        else if(index==1)
        {
            AddText("Outra leitura do mesmo dano. Não some ao total por habilidade.",36);
            foreach(var line in _model.Sources)AddText(line,62);
            if(_model.Sources.Count==0)AddText("Nenhum impacto registrado.",50);
            AddText("Total destas parcelas: "+_model.Damage+" HP · já incluído no total",45);
        }
        else
        {
            AddText("ADQUIRIDOS NA RUN · quantidade não é rank ou configuração ativa",36);
            foreach(var line in _model.Build)AddText(line,64);
            if(_model.Build.Count==0)AddText("Nenhum boon adquirido.",40);
            AddText("RECOMPENSAS CONFIRMADAS",36);
            foreach(var line in _model.Rewards)AddText(line,55);
            if(_model.Rewards.Count==0)AddText("Nenhuma recompensa registrada.",40);
            AddText("IDENTIDADE E PERCURSO\n"+_model.Facts,240);
        }
        Canvas.ForceUpdateCanvases();List.verticalNormalizedPosition=1;
    }
    private void Fill(RectTransform parent,float start,float end,Color color)
    {var f=Box(parent,"Confirmed damage",color);f.anchorMin=new Vector2(start,0);f.anchorMax=new Vector2(end,1);f.offsetMin=f.offsetMax=Vector2.zero;}
    private void AddText(string text,float height)
    {
        var button=RowButton(text,height);button.interactable=false;
        var label=button.GetComponentInChildren<TMP_Text>();
        float actual=Mathf.Max(height*1.5f,label.GetPreferredValues(text,Mathf.Max(250,_content.rect.width-36),Mathf.Infinity).y+24);
        button.GetComponent<LayoutElement>().preferredHeight=actual;label.rectTransform.offsetMin=new Vector2(18,-actual+8);
    }
    private Button RowButton(string text,float height)
    {
        var row=Box(_content,"Fact",Panel);row.gameObject.AddComponent<LayoutElement>().preferredHeight=height*1.5f;
        var button=row.gameObject.AddComponent<Button>();button.targetGraphic=row.GetComponent<Image>();Style(button);
        var label=Label(row,text,12,7,1100,height-10,16,TextColor);label.rectTransform.anchorMax=new Vector2(1,1);label.rectTransform.offsetMax=new Vector2(-18,-10);label.rectTransform.offsetMin=new Vector2(18,-height*1.5f+8);
        _rows.Add(button);return button;
    }
    public void OpenDetail(string text,GameObject returnTo)
    {
        _detailReturn=returnTo;_detailText.text=text;
        _modalDisabled.Clear();foreach(var button in _tabs.Concat(_rows).Concat(new[]{_close}))
            if(button.interactable){_modalDisabled.Add(button);button.interactable=false;}
        _detail.gameObject.SetActive(true);Focus(_detailClose.gameObject);
    }
    public void CloseDetail()
    {
        _detail.gameObject.SetActive(false);foreach(var button in _modalDisabled)if(button)button.interactable=true;
        _modalDisabled.Clear();Focus(_detailReturn?_detailReturn:_close.gameObject);
    }
    public void FocusNext(bool reverse)
    {
        var active=DetailOpen?new List<Button>{_detailClose}:_tabs.Concat(_rows).Concat(new[]{_close}).Where(x=>x&&x.IsActive()&&x.IsInteractable()).ToList();
        if(active.Count==0)return;int i=active.FindIndex(x=>x.gameObject==_events.currentSelectedGameObject);i=(i+(reverse?-1:1)+active.Count)%active.Count;Focus(active[i].gameObject);
    }
    public void Scroll(float direction) { List.verticalNormalizedPosition=Mathf.Clamp01(List.verticalNormalizedPosition+direction*.65f); }
    public void EnsureSelectionVisible()
    {
        var selected=_events.currentSelectedGameObject;if(!selected||!selected.transform.IsChildOf(_content))return;
        var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(List.viewport,selected.transform);var rect=List.viewport.rect;
        float delta=bounds.min.y<rect.yMin?rect.yMin-bounds.min.y:bounds.max.y>rect.yMax?rect.yMax-bounds.max.y:0;
        if(Mathf.Abs(delta)>.1f)_content.anchoredPosition+=new Vector2(0,delta);
    }
    private void Focus(GameObject target) { if(_events&&target&&target.activeInHierarchy)_events.SetSelectedGameObject(target); }
    private RectTransform Box(Transform parent,string name,Color color)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);go.GetComponent<Image>().color=color;return (RectTransform)go.transform;}
    private void BoxAt(Transform parent,Color color,float x,float y,float w,float h) => Place(Box(parent,"Panel",color),x,y,w,h);
    private TMP_Text Label(Transform parent,string text,float x,float y,float w,float h,float size,Color color)
    {var go=new GameObject("Text",typeof(RectTransform));go.transform.SetParent(parent,false);var t=go.AddComponent<TextMeshProUGUI>();t.font=_font;t.text=text;t.fontSize=size*1.5f;t.color=color;t.richText=false;t.raycastTarget=false;t.textWrappingMode=TextWrappingModes.Normal;t.overflowMode=TextOverflowModes.Overflow;Place(t.rectTransform,x,y,w,h);return t;}
    private Button ButtonAt(Transform parent,string text,float x,float y,float w,float h,Action click)
    {var rect=Box(parent,text,Panel);Place(rect,x,y,w,h);var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=rect.GetComponent<Image>();Style(b);b.onClick.AddListener(()=>click());var label=Label(rect,text,8,6,w-16,h-12,16,TextColor);label.alignment=TextAlignmentOptions.Center;return b;}
    private static void Style(Button b)
    {
        var outline=b.gameObject.AddComponent<UnityEngine.UI.Outline>();outline.effectColor=Cyan;outline.effectDistance=new Vector2(2,-2);
        b.gameObject.AddComponent<RunResultsFocus>();var colors=b.colors;colors.highlightedColor=new Color(.65f,.75f,.82f);colors.selectedColor=new Color(.8f,.88f,1);colors.pressedColor=new Color(.45f,.75f,.85f);colors.disabledColor=Color.white;b.colors=colors;}
    private static void Place(RectTransform r,float x,float y,float w,float h)
    {r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y)*1.5f;r.sizeDelta=new Vector2(w,h)*1.5f;}
    private static void Stretch(RectTransform r,float left,float top,float right,float bottom)
    {r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=new Vector2(left,bottom)*1.5f;r.offsetMax=new Vector2(-right,-top)*1.5f;}
}
