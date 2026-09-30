using UnityEngine;

/// <summary>
/// Hades-style IMGUI drawing for a weapon card, kept SEPARATE from the pure text assembly
/// (<see cref="WeaponCardContent"/>). It consumes an already-assembled
/// <see cref="WeaponCardContent.CardContent"/> and renders it as a compact card anchored beside a
/// projected screen position (offset from the pedestal's on-screen anchor), rather than as a central
/// panel — the aesthetic that characterizes the Hades hub cards (Requisito 2.3).
///
/// The view owns only its lazily-built <see cref="GUIStyle"/>s and a background texture; it carries
/// no gameplay state. The thin <c>LobbyArsenal</c> MonoBehaviour (task 5) builds the content, projects
/// the pedestal anchor with the lobby camera, and calls <see cref="Draw"/> from its <c>OnGUI</c>.
///
/// Scaling mirrors the lobby convention exactly (<see cref="Scale"/> = <c>Mathf.Min(Screen.width/1280f,
/// Screen.height/720f)</c>) so the card stays legible across resolutions like the rest of the lobby
/// panels (Requisito 7.1). <see cref="Draw"/> sets and restores <c>GUI.matrix</c> itself, so the caller
/// passes the anchor in raw screen pixels and does not need to pre-scale.
/// </summary>
public sealed class WeaponCardView
{
    // Compact card metrics, expressed in the scaled (reference) space so they read the same at any
    // resolution. Deliberately narrow to feel like a side card, not a central panel.
    private const float CardWidth = 340f;
    private const float Padding = 16f;
    private const float TitleHeight = 30f;
    private const float FamilyHeight = 22f;
    private const float AbilityNameHeight = 22f;
    private const float AbilityDescHeight = 18f;
    private const float AbilityGap = 6f;
    private const float StateHeight = 26f;
    private const float SectionGap = 8f;

    // Offset (in scaled space) from the projected anchor to the card's top-left, placing the card
    // beside/above the pedestal so it never sits centered on the anchor.
    private const float AnchorOffsetX = 28f;
    private const float AnchorOffsetY = 18f;

    private GUIStyle _title, _family, _ability, _detail, _state;
    private Texture2D _background;

    /// <summary>
    /// Per-resolution scale shared with the lobby panels: <c>Mathf.Min(Screen.width/1280f,
    /// Screen.height/720f)</c>. Exposed so callers can size/position in the same reference space.
    /// </summary>
    public static float Scale => Mathf.Min(Screen.width / 1280f, Screen.height / 720f);

    /// <summary>
    /// Draws the card for <paramref name="content"/> anchored beside <paramref name="anchorScreenPosition"/>.
    /// </summary>
    /// <param name="content">The assembled card content from <see cref="WeaponCardContent.Build"/>.</param>
    /// <param name="anchorScreenPosition">
    /// The pedestal anchor projected to screen space, in raw screen pixels (e.g. from
    /// <c>Camera.WorldToScreenPoint</c>, with Y already flipped to GUI space by the caller). The card is
    /// offset from this point, Hades-style, and clamped to stay on screen.
    /// </param>
    public void Draw(WeaponCardContent.CardContent content, Vector2 anchorScreenPosition)
    {
        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Scale;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);

        // Work in the scaled reference space so metrics and the screen bounds share one coordinate system.
        float screenWidth = Screen.width / scale;
        float screenHeight = Screen.height / scale;
        Vector2 anchor = anchorScreenPosition / scale;

        int abilityCount = content.Abilities?.Count ?? 0;
        float cardHeight = MeasureHeight(content, abilityCount);

        // Anchor the card beside the pedestal (offset), not centered on it, then clamp on-screen so the
        // whole card stays readable regardless of where the pedestal projects.
        float x = anchor.x + AnchorOffsetX;
        float y = anchor.y - cardHeight - AnchorOffsetY;
        x = Mathf.Clamp(x, 8f, Mathf.Max(8f, screenWidth - CardWidth - 8f));
        y = Mathf.Clamp(y, 8f, Mathf.Max(8f, screenHeight - cardHeight - 8f));

        var card = new Rect(x, y, CardWidth, cardHeight);
        if (_background) GUI.DrawTexture(card, _background);

        float cursorY = card.y + Padding;
        float contentX = card.x + Padding;
        float contentWidth = CardWidth - Padding * 2f;

        GUI.Label(new Rect(contentX, cursorY, contentWidth, TitleHeight), content.WeaponName, _title);
        cursorY += TitleHeight;
        GUI.Label(new Rect(contentX, cursorY, contentWidth, FamilyHeight), content.FamilyLabel, _family);
        cursorY += FamilyHeight + SectionGap;

        if (content.Abilities != null)
            for (int i = 0; i < content.Abilities.Count; i++)
            {
                WeaponCardContent.AbilityLine line = content.Abilities[i];
                GUI.Label(new Rect(contentX, cursorY, contentWidth, AbilityNameHeight),
                    line.DisplayName + "   ·   " + line.ManaCost.ToString("0") + " mana   ·   " + line.CooldownTime.ToString("0.#") + "s",
                    _ability);
                cursorY += AbilityNameHeight;
                if (!string.IsNullOrEmpty(line.Description))
                {
                    GUI.Label(new Rect(contentX + 10f, cursorY, contentWidth - 10f, AbilityDescHeight), line.Description, _detail);
                    cursorY += AbilityDescHeight;
                }
                cursorY += AbilityGap;
            }

        cursorY += SectionGap;
        GUI.Label(new Rect(contentX, cursorY, contentWidth, StateHeight), content.StateLabel, _state);

        GUI.matrix = previousMatrix;
    }

    /// <summary>Total card height (scaled space) for the given content, so the box hugs its contents.</summary>
    private static float MeasureHeight(WeaponCardContent.CardContent content, int abilityCount)
    {
        float height = Padding + TitleHeight + FamilyHeight + SectionGap;
        if (content.Abilities != null)
            for (int i = 0; i < content.Abilities.Count; i++)
            {
                height += AbilityNameHeight;
                if (!string.IsNullOrEmpty(content.Abilities[i].Description)) height += AbilityDescHeight;
                height += AbilityGap;
            }
        height += SectionGap + StateHeight + Padding;
        return height;
    }

    /// <summary>Lazily builds the card styles + background once a GUI skin is available (OnGUI only).</summary>
    private void EnsureStyles()
    {
        if (_title != null) return;

        _title = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
        _title.normal.textColor = new Color(0.82f, 0.9f, 0.95f);

        _family = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Italic };
        _family.normal.textColor = new Color(0.45f, 0.75f, 0.85f);

        _ability = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
        _ability.normal.textColor = new Color(0.88f, 0.92f, 0.98f);

        _detail = new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true };
        _detail.normal.textColor = new Color(0.68f, 0.74f, 0.82f);

        _state = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold };
        _state.normal.textColor = new Color(0.75f, 0.85f, 0.6f);

        _background = new Texture2D(1, 1);
        _background.SetPixel(0, 0, new Color(0.025f, 0.035f, 0.08f, 0.93f));
        _background.Apply();
    }

    /// <summary>Releases the runtime-created background texture. Call from the owner's OnDestroy.</summary>
    public void Dispose()
    {
        if (_background) Object.Destroy(_background);
        _background = null;
    }
}
