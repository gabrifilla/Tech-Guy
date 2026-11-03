using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shared reward presentation, installed by the native RunRewardUI owner. No lab dependency.</summary>
public sealed partial class RewardSelectionView : MonoBehaviour
{
    public enum Layout { Artifacts, Reconfiguration, Synthesis, Synthesis05 }
    private RunBoons _run;private RunRewardUI _native;private bool _allowCommit;
    private RunRewardTicket _ticket;private int _rewardRoom=-1;
    private RewardChoicePresentation[] _models=Array.Empty<RewardChoicePresentation>();
    private string[] _ids=Array.Empty<string>();private int _revision=-1,_owned=-1,_lastConfirmFrame=-1;
    private int _selected;private bool _details;private Vector2 _scroll;
    private bool _wasVisible;
    private bool _hasFocus=true,_waitForUiRelease;
    private int _uiReleaseFrame=-1;
    private Texture2D[] _glyphs;
    private Texture2D _frame;
    private GUIStyle _title,_body,_small,_large,_center,_centerSmall;
    private Vector2 _lastPointer;
    public Layout Concept {get;set;}
    public int Selected => _selected;
    public bool DetailsVisible => _details;
    public string SelectedScope => _selected>=0&&_selected<_models.Length?_models[_selected].Scope:"";
    public bool ShowReviewControls {get;set;}
    public bool Visible => _run && _native && _native.IsRewardSelectionVisible;
    public void Bind(RunBoons run,Layout layout,bool allowCommit)
    {
        _run=run;Concept=layout;_allowCommit=allowCommit;var native=run.GetComponent<RunRewardUI>();
        if(_native!=native){if(_native)_native.ReleaseSelectionView(this);_native=native;}
        _native.RegisterSelectionView(this);
        _glyphs=new[]{"Mutation","Protocol","Technique"}.Select(n=>Resources.Load<Texture2D>("UI/RunExp03/Glyph"+n)).ToArray();
        _frame=Resources.Load<Texture2D>("UI/RunExp03/GlyphFrame");
        LoadArt05();
        Refresh();
    }
    private void OnDestroy(){if(_native)_native.ReleaseSelectionView(this);}
    public void Select(int index){if(index>=0&&index<_models.Length){if(_selected!=index)_selectedAt05=Time.unscaledTime;_selected=index;_scroll=Vector2.zero;}}
    public void ToggleDetails(){_details=!_details;_scroll=Vector2.zero;_detailsAt05=Time.unscaledTime;_rejectedUntil05=-100;}
    public bool Confirm()
    {
        // Modal/duplicate/read-only input is ignored; it does not make a legal offer unavailable.
        if(!Visible||!_allowCommit||_lastConfirmFrame==Time.frameCount||(Concept==Layout.Synthesis05&&_details))return false;
        if(_selected<0||_selected>=_ids.Length)return Reject05();
        if(_ticket!=_run.ManualTicket||_rewardRoom!=_run.RewardRoom||(_run.ManualTicket?.Revision??0)!=_revision||_run.Choices.Count!=_ids.Length||_run.Choices[_selected].Id!=_ids[_selected])return Reject05();
        _lastConfirmFrame=Time.frameCount;string chosen=_models[_selected].Name;bool committed=_run.Choose(_selected);
        if(committed){_confirmedAt05=Time.unscaledTime;_confirmedName05=chosen;}else Reject05();return committed;
    }
    public void Refresh()
    {
        if(!_run)return;
        if(_ticket!=_run.ManualTicket||_rewardRoom!=_run.RewardRoom){_details=false;_scroll=Vector2.zero;_selected=0;_selectedAt05=Time.unscaledTime;_rejectedUntil05=-100;}
        _ticket=_run.ManualTicket;_rewardRoom=_run.RewardRoom;
        _ids=_run.Choices.Select(o=>o.Id).ToArray();_models=_run.Choices.Select(o=>RewardChoicePresentation.For(_run,o)).ToArray();
        _revision=_run.ManualTicket?.Revision??0;_owned=_run.Acquired.Count;_selected=Mathf.Clamp(_selected,0,Mathf.Max(0,_models.Length-1));
        RefreshStatus05();
    }
    private void Update()
    {
        if(!Visible){_wasVisible=false;return;}
        if(!_hasFocus)return;
        if(_waitForUiRelease)
        {
            var keyboard=Keyboard.current;var mouse=Mouse.current;var gamepad=Gamepad.current;
            bool held=keyboard!=null&&(keyboard.enterKey.isPressed||keyboard.tabKey.isPressed||keyboard.backspaceKey.isPressed||keyboard.digit1Key.isPressed||keyboard.digit2Key.isPressed||keyboard.digit3Key.isPressed||keyboard.leftArrowKey.isPressed||keyboard.rightArrowKey.isPressed||keyboard.upArrowKey.isPressed||keyboard.downArrowKey.isPressed)||mouse!=null&&mouse.leftButton.isPressed||gamepad!=null&&(gamepad.buttonSouth.isPressed||gamepad.buttonWest.isPressed||gamepad.buttonEast.isPressed||gamepad.dpad.ReadValue().sqrMagnitude>.1f);
            if(held)return;_waitForUiRelease=false;_uiReleaseFrame=Time.frameCount;return;
        }
        if(_uiReleaseFrame==Time.frameCount)return;
        // Settings lives under pause; rebuild labels once on return so rebound controls are current.
        if(!_wasVisible){Refresh();_wasVisible=true;}
        if(_ticket!=_run.ManualTicket||_rewardRoom!=_run.RewardRoom||_owned!=_run.Acquired.Count||_revision!=(_run.ManualTicket?.Revision??0)||_ids.Length!=_run.Choices.Count||!_ids.SequenceEqual(_run.Choices.Select(o=>o.Id)))Refresh();
        var k=Keyboard.current;var pad=Gamepad.current;
        if(k!=null)
        {
            if(k.leftArrowKey.wasPressedThisFrame||k.upArrowKey.wasPressedThisFrame)Select((_selected+_models.Length-1)%Mathf.Max(1,_models.Length));
            if(k.rightArrowKey.wasPressedThisFrame||k.downArrowKey.wasPressedThisFrame)Select((_selected+1)%Mathf.Max(1,_models.Length));
            if(k.digit1Key.wasPressedThisFrame)Select(0);if(k.digit2Key.wasPressedThisFrame)Select(1);if(k.digit3Key.wasPressedThisFrame)Select(2);
            if(k.tabKey.wasPressedThisFrame)ToggleDetails();if(k.enterKey.wasPressedThisFrame)Confirm();
            if(k.backspaceKey.wasPressedThisFrame)Cancel();
        }
        if(pad!=null)
        {if(pad.dpad.left.wasPressedThisFrame)Select((_selected+_models.Length-1)%Mathf.Max(1,_models.Length));if(pad.dpad.right.wasPressedThisFrame)Select((_selected+1)%Mathf.Max(1,_models.Length));if(pad.buttonSouth.wasPressedThisFrame)Confirm();if(pad.buttonWest.wasPressedThisFrame)ToggleDetails();if(pad.buttonEast.wasPressedThisFrame)Cancel();}
    }
    private void OnApplicationFocus(bool focused){_hasFocus=focused;_waitForUiRelease=true;}
    public bool Cancel()
    {
        if(!Visible)return false;
        if(_details){ToggleDetails();return true;}
        if(_run.ManualTicket==null)return false;
        _run.CloseManualReward();return true;
    }
    private static readonly Color Ink=new Color(.025f,.044f,.065f),Paper=new Color(.94f,.9f,.8f),Muted=new Color(.55f,.66f,.69f);
    private static Color Accent(int glyph)=>glyph==0?new Color(1,.7f,.35f):glyph==1?new Color(.32f,.85f,.85f):new Color(.68f,.71f,1);
    private static GUIStyle Style(int size,FontStyle weight=FontStyle.Normal,TextAnchor align=TextAnchor.UpperLeft)
    {var s=new GUIStyle(GUI.skin.label){fontSize=size,fontStyle=weight,wordWrap=true,richText=false,alignment=align,padding=new RectOffset()};s.normal.textColor=Paper;return s;}
    private void Styles()
    {if(_title!=null)return;_title=Style(28,FontStyle.Bold);_body=Style(19);_small=Style(13);_large=Style(44,FontStyle.Bold);_center=Style(25,FontStyle.Bold,TextAnchor.UpperCenter);_centerSmall=Style(17,FontStyle.Normal,TextAnchor.UpperCenter);}
    private static void Fill(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
    private static void Label(Rect r,string text,GUIStyle style,Color? color=null){var old=style.normal.textColor;style.normal.textColor=color??Paper;GUI.Label(r,text,style);style.normal.textColor=old;}
    private static void Line(Vector2 a,Vector2 b,float width,Color color)
    {
        var matrix=GUI.matrix;var delta=b-a;GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y,delta.x)*Mathf.Rad2Deg,a);Fill(new Rect(a.x,a.y,delta.magnitude,width),color);GUI.matrix=matrix;
    }
    private void Glyph(Rect r,int index,Color color)
    {
        if(index<3&&_glyphs!=null&&_glyphs[index]){var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,_glyphs[index],ScaleMode.ScaleToFit,true);GUI.color=old;return;}
        // Family marks intentionally describe a category, never an unowned mechanic.
        float s=Mathf.Min(r.width,r.height)*.46f,x=r.center.x-s*.5f,y=r.center.y-s*.5f,t=Mathf.Max(2,s*.07f);
        if(index==3){Fill(new Rect(x,y,s,t),color);Fill(new Rect(x,y+s-t,s,t),color);Fill(new Rect(x,y,t,s),color);Fill(new Rect(x+s-t,y,t,s),color);Fill(new Rect(x+s*.23f,y+s*.46f,s*.54f,t),color);Fill(new Rect(x+s*.46f,y+s*.23f,t,s*.54f),color);}
        else if(index==4){foreach(float p in new[]{0f,.52f}){Fill(new Rect(x+s*p,y+s*p,s*.48f,t),color);Fill(new Rect(x+s*p,y+s*p,t,s*.48f),color);Fill(new Rect(x+s*p,y+s*(p+.48f)-t,s*.48f,t),color);Fill(new Rect(x+s*(p+.48f)-t,y+s*p,t,s*.48f),color);}}
        else{Fill(new Rect(x,y+s*.48f,s,t),color);Fill(new Rect(x+s-t,y+s*.2f,t,s*.6f),color);}
    }
    private bool Button(Rect r,string text,Color color)
    {
        bool hover=r.Contains(Event.current.mousePosition);Fill(r,hover?color:color*.7f);Label(r,text,_centerSmall,Ink);
        return GUI.Button(r,GUIContent.none,GUIStyle.none);
    }
    private void OnGUI()
    {
        if(!Visible){DrawCommitted05();return;}Styles();
        // GUI.depth sorts this entire MonoBehaviour, not a scoped group of drawing commands.
        // Keep its assigned depth so detached damage popups cannot cover the reward modal.
        var oldMatrix=GUI.matrix;bool oldEnabled=GUI.enabled;GUI.depth=-100;GUI.enabled=oldEnabled&&_hasFocus&&!_waitForUiRelease;
        float scale=Mathf.Max(.1f,Mathf.Min(Screen.width/1280f,Screen.height/720f));
        GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);float width=Screen.width/scale;
        Fill(new Rect(0,0,width,Screen.height/scale),new Color(.018f,.032f,.052f,Concept==Layout.Synthesis||Concept==Layout.Synthesis05?1:.985f));
        GUI.BeginGroup(new Rect((width-1280)/2,0,1280,720));
        try
        {
            if(ShowReviewControls)
            {
                Label(new Rect(30,14,300,22),"PRÉVIA · RECOMPENSAS",_small,Muted);
                if(GUI.Button(new Rect(794,10,92,28),"A"))Concept=Layout.Artifacts;
                if(GUI.Button(new Rect(892,10,92,28),"B"))Concept=Layout.Reconfiguration;
                if(GUI.Button(new Rect(990,10,112,28),"C / 04"))Concept=Layout.Synthesis;
                if(GUI.Button(new Rect(1108,10,140,28),"C / 05"))Concept=Layout.Synthesis05;
            }
            if(Concept==Layout.Synthesis05){DrawSynthesis05();return;}
            Label(new Rect(44,58,1050,28),"MEMÓRIA RECUPERADA   /   "+(_run.RewardRoom).ToString("00"),_small,new Color(.7f,.64f,.49f));
            Label(new Rect(44,91,1180,60),Concept==Layout.Artifacts?"O que você vai se tornar?":Concept==Layout.Synthesis?"Escolha sua próxima transformação.":"Reescreva sua próxima ação.",_large);
            if(_models.Length==0){Label(new Rect(44,240,1100,100),"Não há uma escolha elegível neste contexto. A recompensa continua pendente; use o fluxo atual para retornar.",_body);return;}
            if(Concept==Layout.Artifacts)DrawArtifacts();else if(Concept==Layout.Synthesis)DrawSynthesis();else DrawReconfiguration();
            DrawFooter();
        }
        finally{GUI.EndGroup();GUI.matrix=oldMatrix;GUI.enabled=oldEnabled;}
    }
    private void DrawArtifacts()
    {
        Line(new Vector2(70,350),new Vector2(1210,350),1,new Color(.12f,.25f,.29f));
        for(int i=0;i<_models.Length;i++)
        {
            var model=_models[i];float x=55+i*395;bool selected=i==_selected;var accent=Accent(model.Glyph);
            var hit=new Rect(x,182,380,391);
            if(!_details&&hit.Contains(Event.current.mousePosition)&&Event.current.mousePosition!=_lastPointer)Select(i);
            float y=selected?206:220;float pulse=selected?Mathf.Sin(Time.unscaledTime*2)*3:0;
            Vector2 c=new Vector2(x+185,y+92+pulse);float radius=selected?113:100;
            if(_frame){var old=GUI.color;GUI.color=new Color(accent.r,accent.g,accent.b,selected?.75f:.25f);GUI.DrawTexture(new Rect(c.x-radius,c.y-radius,radius*2,radius*2),_frame);GUI.color=old;}
            Glyph(new Rect(x+88,y-3,194,194),model.Glyph,selected?Paper:accent);
            Label(new Rect(x+20,181,330,23),(i+1).ToString("00")+"    "+model.Family,_centerSmall,accent);
            Label(new Rect(x+15,420,340,65),model.Name,_center);
            Label(new Rect(x+25,488,320,60),model.Effect,_centerSmall);
            if(selected)Fill(new Rect(x+150,564,70,3),accent);
            if(!_details&&GUI.Button(hit,GUIContent.none,GUIStyle.none))Select(i);
        }
        if(Event.current.type==EventType.Repaint)_lastPointer=Event.current.mousePosition;
    }
    private void DrawReconfiguration()
    {
        for(int i=0;i<_models.Length;i++)
        {
            var m=_models[i];float y=190+i*124;bool selected=i==_selected;var r=new Rect(44,y,440,108);var accent=Accent(m.Glyph);
            Fill(r,selected?new Color(.105f,.17f,.2f):new Color(.035f,.065f,.09f));if(selected)Fill(new Rect(r.x,y,4,108),accent);
            Glyph(new Rect(58,y+15,75,75),m.Glyph,accent);Label(new Rect(150,y+13,300,22),m.Family+" · "+m.Scope,_small,Muted);Label(new Rect(150,y+42,300,57),m.Name,_title);
            if(!_details&&r.Contains(Event.current.mousePosition)&&Event.current.mousePosition!=_lastPointer)Select(i);
            if(!_details&&GUI.Button(r,GUIContent.none,GUIStyle.none))Select(i);
        }
        if(Event.current.type==EventType.Repaint)_lastPointer=Event.current.mousePosition;
        var model=_models[_selected];var color=Concept==Layout.Synthesis?Paper:Accent(model.Glyph);
        string family=_run.WeaponModifiers==null?"KIT ATUAL":_run.WeaponModifiers.Family==RunWeaponFamily.Gauntlet?"MANOPLA":_run.WeaponModifiers.Family==RunWeaponFamily.Bow?"ARCO":"LANÇA";
        Label(new Rect(535,187,680,30),"SUA ARMA   /   "+family,_small,Muted);
        Line(new Vector2(610,302),new Vector2(1130,302),2,new Color(.16f,.3f,.33f));
        Glyph(new Rect(717,205,220,220),model.Glyph,Paper);
        Label(new Rect(535,425,680,72),model.Effect,_title);
        Label(new Rect(535,506,680,48),model.Context.Length>0?model.Context:"A mudança começa nesta escolha.",_body,color);
    }
    private void DrawFooter()
    {
        var model=_models[_selected];var color=Concept==Layout.Synthesis?Paper:Accent(model.Glyph);
        string context=Concept==Layout.Artifacts?model.Context:"";
        if(model.Restriction.Length>0)context=(context.Length>0?context+"  ·  ":"")+model.Restriction;
        Label(new Rect(44,588,790,46),context,_small,color);
        Label(new Rect(44,643,710,24),model.Level+"  ·  "+model.Scope,_small,Muted);
        bool oldEnabled=GUI.enabled;GUI.enabled=oldEnabled&&_allowCommit;
        if(Button(new Rect(944,625,292,48),"Reescrever · confirmar",color))Confirm();GUI.enabled=oldEnabled;
        if(GUI.Button(new Rect(756,635,165,30),_details?"Fechar detalhes":"Detalhes · Tab"))ToggleDetails();
        Label(new Rect(44,680,1150,25),"← → / direcional: selecionar     ·     Enter / botão sul: confirmar     ·     Tab / botão oeste: detalhes",_small,Muted);
        if(_details)
        {
            Fill(new Rect(420,160,816,448),new Color(.055f,.085f,.11f,.995f));Fill(new Rect(420,160,816,3),color);
            Label(new Rect(448,184,700,45),model.Name+" · detalhes",_title);
            _scroll=GUI.BeginScrollView(new Rect(448,240,752,300),_scroll,new Rect(0,0,725,Mathf.Max(300,_body.CalcHeight(new GUIContent(model.Details),720)+55)));
            Label(new Rect(0,0,720,600),model.Details,_body);GUI.EndScrollView();
            if(Button(new Rect(1008,557,190,32),"Voltar",color))ToggleDetails();
        }
    }
}
