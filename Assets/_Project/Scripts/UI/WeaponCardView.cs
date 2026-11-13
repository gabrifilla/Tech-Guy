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

    private GUIStyle _title, _family, _ability, _detail, _state, _bond, _prompt;
    private Texture2D _background;

    // Same view/content/state contract; only isolated scenes opt in. No extra equipment UI.
    public bool DrawCompact(WeaponCardContent.CardContent content, Vector2 player, bool details, string detailKey) =>
        DrawCompact(content, player, details, detailKey, null, false);

    /// <summary>Compact proximity card (the Nexus uses this one), framed by the bond tier and showing the rapport.</summary>
    public bool DrawCompact(WeaponCardContent.CardContent content, Vector2 player, bool details, string detailKey, BondInfo bond, bool locked)
    {
        if (details) Draw(content, player, bond, locked);
        EnsureStyles();
        Matrix4x4 old = GUI.matrix;
        float scale = Scale;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
        if (!details)
        {
            float time = Time.unscaledTime;
            float compactHeight = 126f + (bond != null ? 78f + (bond.GiftsInHand > 0 ? BondLineHeight : 0f) : 0f);
            LastRect = PlaceCard(new Vector2(Screen.width / scale, Screen.height / scale), compactHeight, player / scale);
            int tier = bond != null ? bond.Level : 1;
            BondFrameArt.Frame(LastRect, tier, time, locked);
            Color accent = locked ? BondFrameArt.Steel : BondFrameArt.TierColor(tier);
            _title.normal.textColor = locked ? BondFrameArt.Muted : BondFrameArt.Ink;
            GUI.Label(new Rect(LastRect.x + 16, LastRect.y + 10, 308, 28), content.FamilyLabel, _title);
            _state.normal.textColor = locked ? BondFrameArt.Muted : accent;
            GUI.Label(new Rect(LastRect.x + 16, LastRect.y + 41, 308, 25), content.StateLabel, _state);
            if (bond != null)
            {
                float y = LastRect.y + 70f;
                BondFrameArt.Fill(new Rect(LastRect.x + 16, y - 3f, 308, 1f), new Color(accent.r, accent.g, accent.b, .35f));
                _bond.normal.textColor = accent;
                GUI.Label(new Rect(LastRect.x + 16, y, 308, BondHeaderHeight),
                    bond.CompanionName + "  ·  Vínculo " + bond.Level + "/" + bond.MaxLevel + "  ·  " + BondFrameArt.TierName(bond.Level), _bond);
                y += BondHeaderHeight;
                BondFrameArt.Pips(new Rect(LastRect.x + 16, y, 308, PipsHeight), bond.Level, bond.MaxLevel, bond.Progress, time);
                y += PipsHeight + 2f;
                _detail.normal.textColor = BondFrameArt.Muted;
                GUI.Label(new Rect(LastRect.x + 16, y, 308, BondLineHeight),
                    string.IsNullOrEmpty(bond.NextReward) ? "Vínculo completo." : "Próximo: " + bond.NextReward, _detail);
                y += BondLineHeight;
                if (bond.GiftsInHand > 0)
                {
                    float glow = .65f + .35f * Mathf.Sin(time * 4f);
                    BondFrameArt.Diamond(new Vector2(LastRect.x + 22, y + BondLineHeight * .5f), 5f, new Color(1f, .82f, .32f, glow));
                    _detail.normal.textColor = BondFrameArt.Gold;
                    GUI.Label(new Rect(LastRect.x + 34, y, 290, BondLineHeight),
                        bond.GiftsInHand + " " + (bond.GiftsInHand == 1 ? Rapport.GiftName : Rapport.GiftNamePlural) + " para entregar", _detail);
                }
            }
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
    /// What the card shows about the weapon's companion (Fase 1c). Null for weapons without a bond yet.
    /// </summary>
    public sealed class BondInfo
    {
        public string CompanionName = "";
        public int Level = 1, MaxLevel = 5;
        public float Progress;
        public int GiftsInHand;
        public string NextReward = "";
        public string ArtifactName = "";
        public string Prompt = "";
    }

    private const float BondHeaderHeight = 22f, PipsHeight = 24f, BondLineHeight = 18f, FooterHeight = 32f;

    /// <summary>Draws the proximity card in a margin clear of the avatar (no companion section).</summary>
    public void Draw(WeaponCardContent.CardContent content, Vector2 playerScreenPosition) => Draw(content, playerScreenPosition, null, false);

    /// <summary>
    /// Draws the proximity card: tiered frame (by bond level), header, the companion's rapport when
    /// <paramref name="bond"/> is given, the kit and a state footer. <paramref name="locked"/> greys the frame.
    /// </summary>
    /// <param name="playerScreenPosition">Avatar screen position with Y flipped to GUI space, before reference scaling.</param>
    public void Draw(WeaponCardContent.CardContent content, Vector2 playerScreenPosition, BondInfo bond, bool locked)
    {
        EnsureStyles();

        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Scale;
        GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);

        // Work in the scaled reference space so metrics and the screen bounds share one coordinate system.
        float screenWidth = Screen.width / scale;
        float screenHeight = Screen.height / scale;
        float time = Time.unscaledTime;

        float cardHeight = MeasureHeight(content, bond);
        // Proximity still selects the physical pedestal; only its information moves to a safe margin.
        var card = PlaceCard(new Vector2(screenWidth, screenHeight), cardHeight, playerScreenPosition / scale);
        LastRect = card;
        int tier = bond != null ? bond.Level : 1;
        BondFrameArt.Frame(card, tier, time, locked);
        Color accent = locked ? BondFrameArt.Steel : BondFrameArt.TierColor(tier);

        float cursorY = card.y + Padding;
        float contentX = card.x + Padding;
        float contentWidth = CardWidth - Padding * 2f;

        _title.normal.textColor = locked ? BondFrameArt.Muted : BondFrameArt.Ink;
        GUI.Label(new Rect(contentX, cursorY, contentWidth, TitleHeight), content.WeaponName, _title);
        _family.normal.textColor = accent;
        GUI.Label(new Rect(contentX, cursorY + 4f, contentWidth, FamilyHeight), content.FamilyLabel.ToUpperInvariant(), _family);
        cursorY += TitleHeight + 2f;
        BondFrameArt.Fill(new Rect(contentX, cursorY, contentWidth, 1f), new Color(accent.r, accent.g, accent.b, .45f));
        cursorY += SectionGap;

        if (bond != null)
        {
            _bond.normal.textColor = accent;
            GUI.Label(new Rect(contentX, cursorY, contentWidth, BondHeaderHeight),
                bond.CompanionName + "  ·  Vínculo " + bond.Level + "/" + bond.MaxLevel + "  ·  " + BondFrameArt.TierName(bond.Level), _bond);
            cursorY += BondHeaderHeight;
            BondFrameArt.Pips(new Rect(contentX, cursorY, contentWidth, PipsHeight), bond.Level, bond.MaxLevel, bond.Progress, time);
            cursorY += PipsHeight + 2f;
            if (!string.IsNullOrEmpty(bond.NextReward))
            {
                _detail.normal.textColor = BondFrameArt.Muted;
                GUI.Label(new Rect(contentX, cursorY, contentWidth, BondLineHeight), "Próximo: " + bond.NextReward, _detail);
                cursorY += BondLineHeight;
            }
            _detail.normal.textColor = BondFrameArt.Ink;
            GUI.Label(new Rect(contentX, cursorY, contentWidth, BondLineHeight),
                "Artefato: " + (string.IsNullOrEmpty(bond.ArtifactName) ? "nenhum equipado" : bond.ArtifactName), _detail);
            cursorY += BondLineHeight;
            if (bond.GiftsInHand > 0)
            {
                float glow = .65f + .35f * Mathf.Sin(time * 4f);
                BondFrameArt.Diamond(new Vector2(contentX + 6f, cursorY + BondLineHeight * .5f), 5f, new Color(1f, .82f, .32f, glow));
                _detail.normal.textColor = BondFrameArt.Gold;
                GUI.Label(new Rect(contentX + 18f, cursorY, contentWidth - 18f, BondLineHeight),
                    bond.GiftsInHand + " " + (bond.GiftsInHand == 1 ? Rapport.GiftName : Rapport.GiftNamePlural) + " para entregar", _detail);
                cursorY += BondLineHeight;
            }
            cursorY += SectionGap * .5f;
            BondFrameArt.Fill(new Rect(contentX, cursorY, contentWidth, 1f), new Color(accent.r, accent.g, accent.b, .25f));
            cursorY += SectionGap;
        }

        if (content.Abilities != null)
            for (int i = 0; i < content.Abilities.Count; i++)
            {
                WeaponCardContent.AbilityLine line = content.Abilities[i];
                BondFrameArt.Diamond(new Vector2(contentX + 4f, cursorY + AbilityNameHeight * .5f), 3.5f, new Color(accent.r, accent.g, accent.b, .9f));
                _ability.normal.textColor = locked ? BondFrameArt.Muted : BondFrameArt.Ink;
                GUI.Label(new Rect(contentX + 14f, cursorY, contentWidth - 14f, AbilityNameHeight),
                    line.DisplayName + "   ·   recarga " + line.CooldownTime.ToString("0.#") + " s", _ability);
                cursorY += AbilityNameHeight;
                if (!string.IsNullOrEmpty(line.Description))
                {
                    _detail.normal.textColor = BondFrameArt.Muted;
                    GUI.Label(new Rect(contentX + 14f, cursorY, contentWidth - 14f, AbilityDescHeight), line.Description, _detail);
                    cursorY += AbilityDescHeight;
                }
                cursorY += AbilityGap;
            }

        // Footer band: state on the left, the bond prompt (when there is one) on the right.
        var footer = new Rect(card.x + 2f, card.yMax - FooterHeight - 2f, card.width - 4f, FooterHeight);
        BondFrameArt.Fill(footer, new Color(accent.r, accent.g, accent.b, locked ? .06f : .14f));
        _state.normal.textColor = locked ? BondFrameArt.Muted : accent;
        GUI.Label(new Rect(contentX, footer.y, contentWidth, FooterHeight), content.StateLabel, _state);
        if (bond != null && !string.IsNullOrEmpty(bond.Prompt))
        {
            _prompt.normal.textColor = BondFrameArt.White;
            GUI.Label(new Rect(contentX, footer.y, contentWidth, FooterHeight), bond.Prompt, _prompt);
        }

        GUI.matrix = previousMatrix;
    }

    /// <summary>Total card height (scaled space) for the given content, so the box hugs its contents.</summary>
    private static float MeasureHeight(WeaponCardContent.CardContent content, BondInfo bond)
    {
        float height = Padding + TitleHeight + 2f + SectionGap;
        if (bond != null)
        {
            height += BondHeaderHeight + PipsHeight + 2f + BondLineHeight + SectionGap * 1.5f;
            if (!string.IsNullOrEmpty(bond.NextReward)) height += BondLineHeight;
            if (bond.GiftsInHand > 0) height += BondLineHeight;
        }
        if (content.Abilities != null)
            for (int i = 0; i < content.Abilities.Count; i++)
            {
                height += AbilityNameHeight;
                if (!string.IsNullOrEmpty(content.Abilities[i].Description)) height += AbilityDescHeight;
                height += AbilityGap;
            }
        height += SectionGap + FooterHeight + 4f;
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

        _state = new GUIStyle(GUI.skin.label) { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
        _state.normal.textColor = new Color(0.75f, 0.85f, 0.6f);
        _family.alignment = TextAnchor.UpperRight; _family.fontStyle = FontStyle.Bold; _family.fontSize = 12;
        _bond = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        _prompt = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight };

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
