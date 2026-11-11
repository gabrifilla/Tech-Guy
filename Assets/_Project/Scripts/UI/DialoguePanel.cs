using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Reusable short dialogue box (speaker name + one line at a time), built for NPCs now and talking weapons later.
/// No portrait, voice or typewriter effect. Owns a <see cref="GameplayModal"/> while open, so world input and the
/// pause menu are blocked. Enter / Space / left click / interaction key advance; Esc closes; the last line closes.
/// </summary>
public sealed class DialoguePanel : MonoBehaviour
{
    private static readonly Color Gold = new Color(.83f, .71f, .46f, 1), Ink = new Color(.945f, .914f, .843f, 1),
        Muted = new Color(.60f, .66f, .70f, 1), Panel = new Color(.05f, .07f, .09f, .94f), Edge = new Color(.30f, .92f, 1f, .9f);

    private static DialoguePanel _current;
    public static bool IsOpen => _current;
    public static DialoguePanel Current => _current;

    private IReadOnlyList<string> _lines;
    private int _index, _openedFrame;
    private TMP_Text _text, _progress;
    private Action _onClosed;
    private CharControlScript _inputOwner;
    private bool _closed;

    public string Speaker { get; private set; }
    public string CurrentLine => _lines != null && _index < _lines.Count ? _lines[_index] : null;

    public static DialoguePanel Show(string speaker, IReadOnlyList<string> lines, Action onClosed, CharControlScript inputOwner)
    {
        if (_current) _current.Close();
        var root = new GameObject("Dialogue panel", typeof(RectTransform));
        var panel = root.AddComponent<DialoguePanel>();
        panel.Build(speaker, lines ?? Array.Empty<string>(), onClosed, inputOwner);
        return panel;
    }

    private void Build(string speaker, IReadOnlyList<string> lines, Action onClosed, CharControlScript inputOwner)
    {
        _current = this; Speaker = speaker; _lines = lines; _onClosed = onClosed; _inputOwner = inputOwner;
        _openedFrame = Time.frameCount;
        GameplayModal.Enter();
        var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 260;
        var scaler = gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;

        var box = Rect("Box", transform, new Vector2(.18f, .05f), new Vector2(.82f, .30f));
        box.gameObject.AddComponent<Image>().color = Panel;
        var edge = Rect("Edge", box, new Vector2(0, 1), new Vector2(1, 1)); edge.sizeDelta = new Vector2(0, 3); edge.anchoredPosition = new Vector2(0, -1.5f);
        edge.gameObject.AddComponent<Image>().color = Edge;
        Text(box, speaker ?? "", new Vector2(.03f, .70f), new Vector2(.97f, .94f), 34, Gold, FontStyles.Bold);
        _text = Text(box, "", new Vector2(.03f, .20f), new Vector2(.97f, .70f), 30, Ink, FontStyles.Normal);
        _progress = Text(box, "", new Vector2(.03f, .04f), new Vector2(.97f, .18f), 18, Muted, FontStyles.Normal);
        _progress.alignment = TextAlignmentOptions.BottomRight;
        Refresh();
        if (_lines.Count == 0) Close();
    }

    private void Update()
    {
        if (_closed || Time.frameCount <= _openedFrame) return;
        var keyboard = Keyboard.current; var mouse = Mouse.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
        bool advance = keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
            || mouse != null && mouse.leftButton.wasPressedThisFrame || GamePreferences.WasPressed(GamePreferences.InteractionAction);
        if (advance) Advance();
    }

    public void Advance()
    {
        if (_closed) return;
        _index++;
        if (_index >= _lines.Count) Close(); else Refresh();
    }

    private void Refresh()
    {
        _text.text = CurrentLine ?? "";
        string key = GamePreferences.BindingLabel(GamePreferences.InteractionAction);
        _progress.text = (_index + 1) + "/" + _lines.Count + "   ·   Enter / Espaço / clique / " + key + ": " + (_index + 1 < _lines.Count ? "continuar" : "fechar") + "   ·   Esc: fechar";
    }

    public void Close()
    {
        if (_closed) return;
        _closed = true;
        if (_current == this) _current = null;
        GameplayModal.Exit(_inputOwner);
        Destroy(gameObject);
        _onClosed?.Invoke();
    }

    private void OnDestroy()
    {
        // Scene unload while open: never strand the frozen clock.
        if (!_closed) { _closed = true; if (_current == this) _current = null; GameplayModal.Exit(_inputOwner); }
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var go = new GameObject(name, typeof(RectTransform)); go.transform.SetParent(parent, false);
        var rect = (RectTransform)go.transform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }

    private static TMP_Text Text(Transform parent, string value, Vector2 min, Vector2 max, float size, Color color, FontStyles style)
    {
        var rect = Rect("Text", parent, min, max);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset) text.font = TMP_Settings.defaultFontAsset;
        text.text = value; text.fontSize = size; text.color = color; text.fontStyle = style; text.raycastTarget = false; text.richText = false;
        text.alignment = TextAlignmentOptions.TopLeft; text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }
}
