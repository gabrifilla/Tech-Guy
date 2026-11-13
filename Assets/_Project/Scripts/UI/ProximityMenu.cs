using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Small choice box shown near the bottom of the screen while the player stands next to something in the Nexus
/// (Etapa 4b, 12/11/2026 — the developer asked for a mini menu instead of full-screen panels). It never freezes the
/// game or takes the camera: each option shows its own key, so there is no selection state — the interaction key
/// triggers option 0, the number keys 1..9 the others, and a mouse click works on any of them.
/// The owner draws it from OnGUI and polls <see cref="WasChosen"/> from Update.
/// </summary>
public sealed class ProximityMenu
{
    public struct Option
    {
        public string Label, Detail, State;
        public bool Enabled;
        public Option(string label, string detail = "", bool enabled = true, string state = "")
        { Label = label; Detail = detail; Enabled = enabled; State = state; }
    }

    private const float Width = 520f, RowHeight = 30f, DetailHeight = 17f, Padding = 12f, HeaderHeight = 26f;
    private GUIStyle _title, _key, _label, _detail, _state;
    private int _clicked = -1, _clickedFrame = -1;

    /// <summary>Rect of the last drawn box, in the scaled reference space (for pointer blocking).</summary>
    public Rect LastRect { get; private set; }

    public static string KeyLabel(int index) =>
        index == 0 ? GamePreferences.BindingLabel(GamePreferences.InteractionAction) : index.ToString();

    private static bool KeyPressed(int index)
    {
        if (index == 0) return GamePreferences.WasPressed(GamePreferences.InteractionAction);
        if (index < 1 || index > 9) return false;
        return GamePreferences.WasPressed(KeyCode.Alpha1 + (index - 1)) || GamePreferences.WasPressed(KeyCode.Keypad1 + (index - 1));
    }

    /// <summary>True on the frame option <paramref name="index"/> was chosen by its key or by a click on its row.</summary>
    public bool WasChosen(int index)
    {
        if (_clicked == index && Time.frameCount - _clickedFrame <= 1) { _clicked = -1; return true; }
        return KeyPressed(index);
    }

    /// <summary>Draws the box. <paramref name="tier"/> picks the frame (1 plain .. 5 signature), as on the weapon card.</summary>
    public void Draw(string title, string subtitle, IReadOnlyList<Option> options, int tier = 1)
    {
        if (options == null || options.Count == 0) return;
        EnsureStyles();
        Matrix4x4 old = GUI.matrix;
        float scale = WeaponCardView.Scale;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        float screenWidth = Screen.width / scale, screenHeight = Screen.height / scale, time = Time.unscaledTime;

        float height = Padding * 2f + HeaderHeight;
        for (int i = 0; i < options.Count; i++) height += RowHeight + (string.IsNullOrEmpty(options[i].Detail) ? 0f : DetailHeight) + 4f;
        var box = new Rect((screenWidth - Width) * .5f, screenHeight - height - 96f, Width, height);
        LastRect = box;
        BondFrameArt.Frame(box, tier, time);
        Color accent = BondFrameArt.TierColor(tier);

        _title.normal.textColor = accent;
        GUI.Label(new Rect(box.x + Padding, box.y + Padding - 2f, Width * .5f, HeaderHeight), title ?? "", _title);
        if (!string.IsNullOrEmpty(subtitle))
        {
            _state.normal.textColor = BondFrameArt.Muted;
            GUI.Label(new Rect(box.x + Width * .4f, box.y + Padding - 2f, Width * .6f - Padding, HeaderHeight), subtitle, _state);
        }
        float y = box.y + Padding + HeaderHeight;
        for (int i = 0; i < options.Count; i++)
        {
            var option = options[i];
            float rowHeight = RowHeight + (string.IsNullOrEmpty(option.Detail) ? 0f : DetailHeight);
            var row = new Rect(box.x + Padding, y, Width - Padding * 2f, rowHeight);
            bool hover = option.Enabled && row.Contains(Event.current.mousePosition);
            BondFrameArt.Fill(row, hover ? new Color(accent.r, accent.g, accent.b, .18f) : new Color(1, 1, 1, .04f));
            // Key cap.
            var cap = new Rect(row.x + 6f, row.y + 4f, 46f, RowHeight - 8f);
            Color capColor = option.Enabled ? accent : BondFrameArt.Muted;
            BondFrameArt.Border(cap, 1f, new Color(capColor.r, capColor.g, capColor.b, .9f));
            _key.normal.textColor = capColor;
            GUI.Label(cap, KeyLabel(i), _key);
            _label.normal.textColor = option.Enabled ? BondFrameArt.Ink : BondFrameArt.Muted;
            GUI.Label(new Rect(row.x + 62f, row.y, row.width - 190f, RowHeight), option.Label ?? "", _label);
            if (!string.IsNullOrEmpty(option.State))
            {
                _state.normal.textColor = option.Enabled ? BondFrameArt.Gold : BondFrameArt.Muted;
                GUI.Label(new Rect(row.xMax - 130f, row.y, 124f, RowHeight), option.State, _state);
            }
            if (!string.IsNullOrEmpty(option.Detail))
            {
                _detail.normal.textColor = BondFrameArt.Muted;
                GUI.Label(new Rect(row.x + 62f, row.y + RowHeight - 6f, row.width - 70f, DetailHeight), option.Detail, _detail);
            }
            if (option.Enabled && GUI.Button(row, GUIContent.none, GUIStyle.none)) { _clicked = i; _clickedFrame = Time.frameCount; }
            y += rowHeight + 4f;
        }
        GUI.matrix = old;
    }

    /// <summary>True when the pointer (raw screen pixels, Y up) is over the last drawn box.</summary>
    public bool ContainsPointer(Vector2 screenPosition) =>
        LastRect.Contains(new Vector2(screenPosition.x, Screen.height - screenPosition.y) / WeaponCardView.Scale);

    private void EnsureStyles()
    {
        if (_title != null) return;
        _title = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        _key = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        _label = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        _detail = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.UpperLeft };
        _state = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };
    }
}
