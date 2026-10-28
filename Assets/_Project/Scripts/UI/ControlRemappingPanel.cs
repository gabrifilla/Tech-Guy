using System;
using UnityEngine;

/// <summary>Instance-owned capture state, shared presentation for title settings and pause.</summary>
public sealed class ControlRemappingPanel
{
    private int _capturing = -1, _captureFrame, _captureFinishedFrame = -1;
    private string _message;
    private GUIStyle _hint;
    private readonly KeyCode[] _keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));
    private readonly string[] _labels = { "Atacar (segure para repetir) / mover", "Mover / interagir", "Esquiva",
        "Habilidade 1", "Habilidade 2", "Habilidade 3 / interagir no Nexus", "Habilidade 4", "Finalizar / retornar projeção" };
    private Vector2 _scroll;
    private int _keyboardId, _mouseId;
    private GameControl[] Actions => GamePreferences.UsesDirectControls ? GamePreferences.DirectActions :
        new[] { GameControl.Primary, GameControl.Move, GameControl.Dash, GameControl.Skill1, GameControl.Skill2, GameControl.Skill3, GameControl.Skill4, GameControl.FinishSequence };
    private string Label(int index) => GamePreferences.UsesDirectControls ? GamePreferences.ActionLabel((GameControl)index) : _labels[index];
    public string Message => _message;
    public void BeginCapture(GameControl action)
    {
        _capturing = (int)action; _captureFrame = Time.frameCount;
        _keyboardId = UnityEngine.InputSystem.Keyboard.current?.deviceId ?? -1;
        _mouseId = UnityEngine.InputSystem.Mouse.current?.deviceId ?? -1;
        _message = "Esc cancela. Teclado e botões do mouse aceitos.";
    }
    public bool TryAssign(KeyCode[] bindings, KeyCode key)
    {
        if (!IsCapturing || !GamePreferences.IsBindable(key)) return false;
        foreach (var action in Actions)
            if ((int)action != _capturing && bindings[(int)action] == key)
            { _message = "Já usado por " + Label((int)action) + ". Escolha outra tecla."; return false; }
        bindings[_capturing] = key; _message = "Alteração pronta. Clique em Aplicar para salvar."; Cancel(); return true;
    }
    public bool IsCapturing => _capturing >= 0;
    public bool CanApply => !IsCapturing && Time.frameCount > _captureFinishedFrame;
    public void Cancel() { if (IsCapturing) _captureFinishedFrame = Time.frameCount; _capturing = -1; }

    public bool Update(KeyCode[] bindings)
    {
        if (!IsCapturing) return false;
        if (!Application.isFocused || _keyboardId != (UnityEngine.InputSystem.Keyboard.current?.deviceId ?? -1) ||
            _mouseId != (UnityEngine.InputSystem.Mouse.current?.deviceId ?? -1)) { Cancel(); _message = "Captura cancelada: foco ou dispositivo mudou."; return true; }
        if (GamePreferences.WasPressed(KeyCode.Escape)) { Cancel(); return true; }
        if (Time.frameCount <= _captureFrame) return true;
        foreach (KeyCode key in _keys)
        {
            if (!GamePreferences.WasPressed(key)) continue;
            if (TryAssign(bindings, key)) return true;
        }
        return true;
    }

    public void DrawDashDirection(Rect area, ref DashDirectionMode mode)
    {
        GUI.Label(new Rect(area.x, area.y, area.width * .48f, 32), "Direção da esquiva");
        if (GUI.Button(new Rect(area.x + area.width * .5f, area.y, area.width * .5f, 32),
            mode == DashDirectionMode.Mouse ? "Cursor (padrão)" : "Movimento / olhar") && CanApply)
            mode = mode == DashDirectionMode.Mouse ? DashDirectionMode.Movement : DashDirectionMode.Mouse;
        GUI.Label(new Rect(area.x, area.y + 34, area.width, 22), mode == DashDirectionMode.Movement ? "Sem direção pressionada, segue a direção do personagem." : "Segue a direção do cursor.");
    }

    public void Draw(Rect area, KeyCode[] bindings)
    {
        if (_hint == null) _hint = new GUIStyle(GUI.skin.label) { wordWrap = true };
        var actions = Actions;
        const float rowHeight = 36f;
        _scroll = GUI.BeginScrollView(area, _scroll, new Rect(0, 0, area.width - 22, actions.Length * rowHeight + 46));
        for (int row = 0; row < actions.Length; row++)
        {
            int i = (int)actions[row]; float y = row * rowHeight;
            GUI.Label(new Rect(0, y + 3, area.width * .53f, rowHeight - 2), Label(i));
            if (GUI.Button(new Rect(area.width * .54f, y, area.width * .40f, rowHeight - 5),
                _capturing == i ? "Pressione uma tecla..." : GamePreferences.KeyLabel(bindings[i]))) BeginCapture(actions[row]);
        }
        GUI.Label(new Rect(0, actions.Length * rowHeight, area.width - 26, 44), _message ?? "Clique para alterar. Esc cancela a captura e permanece reservado para pausa.", _hint);
        GUI.EndScrollView();
    }
}
