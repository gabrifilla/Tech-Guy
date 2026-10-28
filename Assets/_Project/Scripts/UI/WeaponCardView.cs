using UnityEngine;

/// <summary>
/// IMGUI drawing for a proximity weapon card, separate from the pure text assembly
/// (<see cref="WeaponCardContent"/>). It consumes an already-assembled
/// <see cref="WeaponCardContent.CardContent"/> and renders it in a screen margin,
/// leaving the avatar's lane and physical signs visible.
///
/// The view owns only its lazily-built <see cref="GUIStyle"/>s and a background texture; it carries
/// no gameplay state. The thin <c>LobbyArsenal</c> MonoBehaviour (task 5) builds the content, projects
/// the pedestal anchor with the lobby camera, and calls <see cref="Draw"/> from its <c>OnGUI</c>.
///
/// Scaling mirrors the lobby convention exactly (<see cref="Scale"/> = <c>Mathf.Min(Screen.width/1280f,
/// Screen.height/720f)</c>) so the card stays legible across resolutions like the rest of the lobby
/// panels (Requisito 7.1). <see cref="Draw"/> sets and restores <c>GUI.matrix</c> itself, so the caller
/// passes the avatar position in raw screen pixels and does not need to pre-scale.
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

    public Rect LastRect { get; private set; }

    /// <summary>Keep the proximity card below the signs and outside the avatar's central lane.</summary>
    public static Rect PlaceCard(Vector2 referenceScreen, float cardHeight, Vector2 player)
    {
        float y = Mathf.Max(156f, referenceScreen.y - cardHeight - 24f);
        float right = referenceScreen.x - CardWidth - 24f;
        var avatar = new Rect(player.x - 68f, player.y - 135f, 136f, 190f);
        var card = new Rect(24f, y, CardWidth, cardHeight);
        if (card.Overlaps(avatar)) card.x = right;
        return card;
    }

    private GUIStyle _title, _family, _ability, _detail, _state;
    private Texture2D _background;

    // Same view/content/state contract; only isolated scenes opt in. No extra equipment UI.
    public bool DrawCompact(WeaponCardContent.CardContent content, Vector2 player, bool details, string detailKey)
    {
        if (details) Draw(content, player);
        EnsureStyles();
        Matrix4x4 old = GUI.matrix;
        float scale = Scale;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        if (!details)
        {
            LastRect = PlaceCard(new Vector2(Screen.width / scale, Screen.height / scale), 126, player / scale);
            GUI.DrawTexture(LastRect, _background);
            GUI.Label(new Rect(LastRect.x + 16, LastRect.y + 10, 308, 28), content.FamilyLabel, _title);
            GUI.Label(new Rect(LastRect.x + 16, LastRect.y + 41, 308, 25), content.StateLabel, _state);
        }
        Rect button = new Rect(LastRect.x + 16, LastRect.yMax - 43, 308, 30);
        // Full card's state footer sits above the extra disclosure control.
        if (details) button = new Rect(LastRect.x + 16, LastRect.y - 34, 308, 30);
        bool clicked = GUI.Button(button, "[" + detailKey + "] " + (details ? "Fechar detalhes do kit" : "Consultar detalhes do kit"));
        if (details) LastRect = new Rect(LastRect.x, button.y, LastRect.width, LastRect.yMax - button.y);
        GUI.matrix = old;
        return clicked ? !details : details;
    }

    /// <summary>
    /// Per-resolution scale shared with the lobby panels: <c>Mathf.Min(Screen.width/1280f,
    /// Screen.height/720f)</c>. Exposed so callers can size/position in the same reference space.
    /// </summary>
    public static float Scale => Mathf.Min(Screen.width / 1280f, Screen.height / 720f);

    /// <summary>
    /// Draws the proximity card in a margin clear of the avatar.
    /// </summary>
    /// <param name="content">The assembled card content from <see cref="WeaponCardContent.Build"/>.</param>
    /// <param name="playerScreenPosition">
    /// Avatar screen position with Y flipped to GUI space, before reference scaling.
    /// </param>
    public void Draw(WeaponCardContent.CardContent content, Vector2 playerScreenPosition)
    {
        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Scale;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);

        // Work in the scaled reference space so metrics and the screen bounds share one coordinate system.
        float screenWidth = Screen.width / scale;
        float screenHeight = Screen.height / scale;

        int abilityCount = content.Abilities?.Count ?? 0;
        float cardHeight = MeasureHeight(content, abilityCount);

        // Proximity still selects the physical pedestal; only its information moves to a safe margin.
        var card = PlaceCard(new Vector2(screenWidth, screenHeight), cardHeight, playerScreenPosition / scale);
        LastRect = card;
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
