using System.Collections.Generic;
using UnityEngine;

public sealed partial class RewardSelectionView
{
    private readonly Dictionary<string,Texture2D> _art05=new Dictionary<string,Texture2D>();
    private float _selectedAt05=-100,_detailsAt05=-100,_rejectedUntil05=-100,_confirmedAt05=-100;
    private string _confirmedName05;
    private PauseMenuUI _pause05;
    private string _status05="";
    private void RefreshStatus05()
    {
        var ticket=_run.ManualTicket;
        string options=_models.Length+(_models.Length==1?" opção":" opções");
        _status05=options+" · "+_run.Acquired.Count+" aquisições nesta incursão";
        if(ticket==null||_run.ProtocolSession==null)return;
        var node=_run.ProtocolSession.Route.Node(ticket.Request.NodeId);
        string kind=ticket.AnnouncedKind==RunRewardKind.Mutation?"MUTAÇÃO":ticket.AnnouncedKind==RunRewardKind.Protocol?"PROTOCOLO":"TÉCNICA";
        _status05=kind+" · escolha "+(ticket.Request.Ordinal+1)+"/"+node.TotalAcquisitions+" · "+options+(ticket.IsAlternative?" · categoria esgotada: alternativa":"");
    }
    public bool ReduceMotion {get;set;}
    private static readonly Color Copper05=new Color(.72f,.46f,.27f),Panel05=new Color(.063f,.089f,.104f),Teal05=new Color(.40f,.83f,.80f);
    private void LoadArt05()
    {if(_art05.Count==0)foreach(string key in RewardIllustrationCatalog.Keys)_art05.Add(key,Resources.Load<Texture2D>("UI/RunExp05/"+key));_pause05=_run.GetComponent<PauseMenuUI>();}
    private bool Reject05(){if(Visible&&Concept==Layout.Synthesis05)_rejectedUntil05=Time.unscaledTime+.45f;return false;}
    private void Art05(Rect rect,RewardChoicePresentation model,float alpha=1)
    {
        string key=RewardIllustrationCatalog.KeyFor(model.Id);
        if(key!=null&&_art05.TryGetValue(key,out var texture)&&texture)
        {var old=GUI.color;GUI.color=new Color(1,1,1,alpha);GUI.DrawTexture(rect,texture,ScaleMode.ScaleToFit,true);GUI.color=old;}
        else Glyph(rect,model.Glyph,Paper);
    }
    private static void Frame05(Rect r,Color edge)
    {
        // Keep the frame inside GUI's clipped/scaled group. Rotating GUI.matrix for vertical
        // strokes displaced IMGUI clip edges at wide aspects; rect strokes need no matrix edits.
        Fill(r,Panel05);Fill(new Rect(r.x+12,r.y,r.width-12,1),edge);
        Fill(new Rect(r.xMax-1,r.y,1,r.height-12),edge);Fill(new Rect(r.x,r.yMax-1,r.width-12,1),edge);
        Fill(new Rect(r.x,r.y+12,1,r.height-12),edge);
        for(int i=0;i<12;i++){Fill(new Rect(r.x+11-i,r.y+i,1,1),edge);Fill(new Rect(r.xMax-12+i,r.yMax-1-i,1,1),edge);}
    }
    private void DrawSynthesis05()
    {
        if(!ShowReviewControls)
        {
            Label(new Rect(44,18,920,28),_status05,_small,Muted);
            if(_run.ManualTicket!=null&&Button(new Rect(1000,10,234,30),_details?"Voltar à escolha":"Fechar · manter pendente",Teal05))Cancel();
        }
        // A fitted reference canvas keeps the same safe margins at all supported aspects.
        Label(new Rect(46,58,1000,24),"S Í N T E S E     /     MEMÓRIA "+_run.RewardRoom.ToString("00"),_small,Copper05);
        Label(new Rect(44,92,1160,60),"Dê outra forma ao seu poder.",_large);
        Line(new Vector2(45,159),new Vector2(1234,159),1,new Color(.20f,.27f,.28f));
        if(_models.Length==0)
        {Label(new Rect(80,300,1100,100),"Nenhuma escolha disponível agora. A recompensa permanece pendente.",_title);return;}
        for(int i=0;i<_models.Length;i++)
        {
            var m=_models[i];bool selected=i==_selected;float x=44+i*397;var row=new Rect(x,185,381,119);
            bool hover=GUI.enabled&&!_details&&row.Contains(Event.current.mousePosition);
            Frame05(row,selected?Copper05:hover?Teal05:new Color(.19f,.26f,.27f));
            if(selected){Fill(new Rect(x+1,186,379,117),new Color(.15f,.19f,.20f));Fill(new Rect(x+1,186,4,117),Paper);}
            Art05(new Rect(x+8,193,102,102),m,selected?1:.72f);
            Label(new Rect(x+119,200,248,23),m.Family,_small,selected?Teal05:Muted);
            Title05(new Rect(x+119,230,245,69),m.Name,_center);
            if(hover&&Event.current.mousePosition!=_lastPointer)Select(i);
            if(!_details&&GUI.Button(row,GUIContent.none,GUIStyle.none))Select(i);
        }
        if(Event.current.type==EventType.Repaint)_lastPointer=Event.current.mousePosition;
        var model=_models[_selected];float settle=ReduceMotion?1:Mathf.SmoothStep(0,1,Mathf.Clamp01((Time.unscaledTime-_selectedAt05)/.13f));
        // Three quiet structural planes echo the laminated enemy/environment surfaces.
        var stage=new Rect(44,324,382,280);Frame05(stage,new Color(.25f,.31f,.31f));
        Line(new Vector2(65,585),new Vector2(180,585),2,Copper05);
        Line(new Vector2(310,343),new Vector2(406,343),2,Copper05);
        Art05(new Rect(52+4*(1-settle),309,366,294),model,.85f+.15f*settle);
        Title05(new Rect(468,329,750,55),model.Name,_title);
        Label(new Rect(468,399,746,89),model.Effect,_title,Paper);
        if(model.Context.Length>0)
        {Fill(new Rect(468,507,3,52),Teal05);Label(new Rect(485,510,720,49),model.Context,_body,Teal05);}
        Label(new Rect(468,573,740,48),model.Restriction,_small,Paper);
        Label(new Rect(44,633,690,27),model.Level+"   /   "+model.Scope,_small,Muted);
        bool enabled=GUI.enabled;GUI.enabled=enabled&&_allowCommit&&!_details;
        if(Action05(new Rect(944,626,292,47),model.Id==RunBoonOfferCatalog.ExhaustedId?"Continuar": "Incorporar"))Confirm();
        GUI.enabled=enabled;
        if(Button(new Rect(756,636,165,30),_details?"Fechar detalhes":"Detalhes",new Color(.55f,.67f,.68f)))ToggleDetails();
        Label(new Rect(44,684,1170,25),Time.unscaledTime<_rejectedUntil05?"Escolha indisponível. Feche os detalhes e confira a oferta atual.":"← → / direcional: escolher · Enter / sul: incorporar · Tab / oeste: detalhes · Backspace / leste: voltar · Esc: pausa",_small,Muted);
        if(_details)DrawDetails05(model);
    }
    private static void Title05(Rect rect,string text,GUIStyle style)
    {
        int size=style.fontSize;while(style.fontSize>17&&style.CalcHeight(new GUIContent(text),rect.width)>rect.height)style.fontSize--;
        Label(rect,text,style,Paper);style.fontSize=size;
    }
    private bool Action05(Rect rect,string text)
    {
        bool hover=GUI.enabled&&rect.Contains(Event.current.mousePosition);
        Fill(rect,GUI.enabled?(hover?new Color(.68f,.88f,.82f):Paper):new Color(.18f,.23f,.25f));
        Fill(new Rect(rect.x,rect.y,rect.width,3),Time.unscaledTime<_rejectedUntil05?new Color(.9f,.56f,.34f):Copper05);
        Label(new Rect(rect.x+18,rect.y+13,rect.width-36,27),text,_centerSmall,GUI.enabled?Ink:Muted);
        return GUI.Button(rect,GUIContent.none,GUIStyle.none);
    }
    private void DrawDetails05(RewardChoicePresentation model)
    {
        // Modal intercepts the surface; keyboard focus still selects alternatives intentionally.
        Fill(new Rect(37,314,1205,310),new Color(.018f,.032f,.045f,.99f));
        float enter=ReduceMotion?0:4*(1-Mathf.Clamp01((Time.unscaledTime-_detailsAt05)/.1f));
        var panel=new Rect(430,317+enter,804,301);Frame05(panel,Copper05);
        Art05(new Rect(93,324,270,270),model);
        Label(new Rect(454,335+enter,750,39),model.Name+" · detalhes",_title);
        string details=model.Name+"\n\n"+model.Details;
        float height=Mathf.Max(182,_body.CalcHeight(new GUIContent(details),710)+18);
        _scroll=GUI.BeginScrollView(new Rect(454,390+enter,750,187),_scroll,new Rect(0,0,716,height));
        Label(new Rect(0,0,710,height),details,_body);GUI.EndScrollView();
        Label(new Rect(454,587+enter,734,23),"Tab / botão oeste: voltar à escolha",_small,Muted);
    }
    private void DrawCommitted05()
    {
        if(Concept!=Layout.Synthesis05||string.IsNullOrEmpty(_confirmedName05)||Time.unscaledTime-_confirmedAt05>.8f||!_run||_run.IsChoosing||Time.timeScale<=0||(_pause05&&_pause05.BlocksInput))return;
        Styles();var matrix=GUI.matrix;GUI.depth=-100;
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);float width=Screen.width/scale;GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        Frame05(new Rect((width-600)/2,63,600,53),Copper05);Label(new Rect((width-562)/2,77,562,29),"Incorporado · "+_confirmedName05,_body,Paper);
        GUI.matrix=matrix;
    }
}
