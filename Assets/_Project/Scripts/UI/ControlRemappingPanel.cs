using System;
using UnityEngine;

/// <summary>Instance-owned capture state, shared presentation for title settings and pause.</summary>
public sealed class ControlRemappingPanel
{
    private int _capturing = -1, _captureFrame;
    private string _message;
    private GUIStyle _hint;
    private readonly KeyCode[] _keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    private readonly string[] _labels = { "Atacar (segure para repetir) / mover", "Mover / interagir", "Esquiva",
        "Habilidade 1", "Habilidade 2", "Habilidade 3 / interagir no Nexus", "Habilidade 4", "Finalizar / retornar projeção" };
    private string Label(int index) => !GamePreferences.UsesDirectControls ? _labels[index] :
        index == 0 ? "Básico (clique; hold só ChargedShot)" :
        index == 1 ? "Secundário / Asura (Manopla)" :
        index == 6 ? "Habilidade 4 / compatibilidade Asura" : _labels[index];
    public bool IsCapturing => _capturing >= 0;
    public void Cancel() { _capturing = -1; }

    public bool Update(KeyCode[] bindings)
    {
        if (!IsCapturing) return false;
        if (GamePreferences.WasPressed(KeyCode.Escape)) { Cancel(); return true; }
        if (Time.frameCount <= _captureFrame) return true;
        foreach (KeyCode key in _keys)
        {
            if (!GamePreferences.IsBindable(key) || !GamePreferences.WasPressed(key)) continue;
            if (GamePreferences.UsesDirectControls && GamePreferences.IsMovementKey(key))
            { _message = "W/A/S/D estão reservadas ao movimento nesta etapa."; return true; }
            int conflict = Array.IndexOf(bindings, key);
            if (conflict >= 0 && conflict != _capturing)
            {
                _message = "Já usado por " + Label(conflict) + ". Escolha outra tecla.";
                return true;
            }
            bindings[_capturing] = key;
            _message = "Alteração pronta. Clique em Aplicar para salvar.";
            Cancel();
            return true;
        }
        return true;
    }

    public void Draw(Rect area, KeyCode[] bindings)
    {
        if (_hint == null) _hint = new GUIStyle(GUI.skin.label) { wordWrap = true };
        float rowHeight = Mathf.Min(34f, (area.height - 40f) / _labels.Length);
        for (int i = 0; i < _labels.Length; i++)
        {
            float y = area.y + i * rowHeight;
            string label = Label(i);
            GUI.Label(new Rect(area.x, y + 3, area.width * .53f, rowHeight - 2), label);
            if (GUI.Button(new Rect(area.x + area.width * .54f, y, area.width * .46f, rowHeight - 5),
                _capturing == i ? "Pressione uma tecla..." : GamePreferences.KeyLabel(bindings[i])))
            { _capturing = i; _captureFrame = Time.frameCount; _message = "Esc cancela. Teclado e botões do mouse aceitos."; }
        }
        GUI.Label(new Rect(area.x, area.y + rowHeight * _labels.Length + 3, area.width, 36), _message ?? "Clique em um controle para alterar. Esc permanece reservado para pausa.", _hint);
    }
}
