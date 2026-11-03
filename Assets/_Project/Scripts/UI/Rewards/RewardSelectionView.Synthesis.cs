using UnityEngine;

public sealed partial class RewardSelectionView
{
    private void DrawSynthesis()
    {
        // Compact alternatives stay comparable; one transformation gets the visual stage.
        for(int i=0;i<_models.Length;i++)
        {
            var model=_models[i];bool selected=i==_selected;float x=44+i*397;var row=new Rect(x,184,381,121);var accent=Accent(model.Glyph);
            Fill(row,selected?Paper:new Color(.065f,.105f,.13f));if(selected)Fill(new Rect(x,184,381,4),accent);
            Glyph(new Rect(x+12,211,67,67),model.Glyph,selected?Ink:accent);
            Label(new Rect(x+95,199,273,23),model.Family,_small,selected?Ink:Muted);
            Label(new Rect(x+95,232,270,65),model.Name,_title,selected?Ink:Paper);
            if(!_details&&row.Contains(Event.current.mousePosition)&&Event.current.mousePosition!=_lastPointer)Select(i);
            if(!_details&&GUI.Button(row,GUIContent.none,GUIStyle.none))Select(i);
        }
        if(Event.current.type==EventType.Repaint)_lastPointer=Event.current.mousePosition;
        var selectedModel=_models[_selected];var color=Accent(selectedModel.Glyph);
        Glyph(new Rect(60,331,220,220),selectedModel.Glyph,Paper);
        Label(new Rect(335,345,875,60),selectedModel.Name,_title,color);
        Label(new Rect(335,417,875,105),selectedModel.Effect,_title);
        if(selectedModel.Context.Length>0)Label(new Rect(335,535,875,48),selectedModel.Context,_body,color);
    }
}
