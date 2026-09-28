using System;
using UnityEngine;

/// <summary>Instance-owned capture state, shared presentation for title settings and pause.</summary>
public sealed class ControlRemappingPanel
{
    private int _capturing = -1, _captureFrame;
    private string _message;
    private GUIStyle _hint;
    private readonly KeyCode[] _keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    private readonly string[] _labels = { "Selecionar inimigo / mover", "Mover / interagir", "Esquiva",
        "Habilidade 1", "Habilidade 2", "Habilidade 3 / interagir no Nexus", "Habilidade 4" };
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
            int conflict = Array.IndexOf(bindings, key);
            if (conflict >= 0 && conflict != _capturing)
            {
                _message = "Já usado por " + _labels[conflict] + ". Escolha outra tecla.";
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
        for (int i = 0; i < _labels.Length; i++)
        {
            float y = area.y + i * 34;
            GUI.Label(new Rect(area.x, y + 5, area.width * .53f, 28), _labels[i]);
            if (GUI.Button(new Rect(area.x + area.width * .54f, y, area.width * .46f, 29),
                _capturing == i ? "Pressione uma tecla..." : GamePreferences.KeyLabel(bindings[i])))
            { _capturing = i; _captureFrame = Time.frameCount; _message = "Esc cancela. Teclado e botões do mouse aceitos."; }
        }
        GUI.Label(new Rect(area.x, area.y + 240, area.width, 36), _message ?? "Clique em um controle para alterar. Esc permanece reservado para pausa.", _hint);
    }
}
