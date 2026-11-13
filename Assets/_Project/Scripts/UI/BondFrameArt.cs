using UnityEngine;

/// <summary>
/// Frames, pips and bars for the weapon card and the bond panel (Fase 1c). The frame grows with the rapport tier:
/// 1 cold steel · 2 circuit (cyan corner brackets) · 3 pulse (breathing inner line) · 4 gold (double border and
/// corner diamonds) · 5 signature (gold, travelling shimmer and halo). Drawn with IMGUI primitives only (no new
/// asset); animation uses unscaled time so it keeps moving while a modal freezes the game clock.
/// </summary>
public static class BondFrameArt
{
    public static readonly Color Ink = new Color(.945f, .914f, .843f), Muted = new Color(.60f, .66f, .70f),
        Panel = new Color(.035f, .045f, .075f, .96f), Band = new Color(.07f, .09f, .13f, .98f),
        Steel = new Color(.50f, .55f, .60f), Cyan = new Color(.30f, .92f, 1f), Violet = new Color(.72f, .52f, 1f),
        Gold = new Color(1f, .82f, .32f), White = new Color(1f, .97f, .88f);

    /// <summary>Name of each bond tier with Ico (1..5).</summary>
    public static string TierName(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 5))
        {
            case 1: return "Estranhas";
            case 2: return "Parceiras de treino";
            case 3: return "Cúmplices";
            case 4: return "De mãos dadas";
            default: return "Segundo Round";
        }
    }

    public static Color TierColor(int tier)
    {
        switch (Mathf.Clamp(tier, 1, 5))
        {
            case 1: return Steel;
            case 2: return Cyan;
            case 3: return Color.Lerp(Cyan, Violet, .5f);
            default: return Gold;
        }
    }

    public static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color; GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    public static void Border(Rect r, float thickness, Color color)
    {
        Fill(new Rect(r.x, r.y, r.width, thickness), color);
        Fill(new Rect(r.x, r.yMax - thickness, r.width, thickness), color);
        Fill(new Rect(r.x, r.y + thickness, thickness, r.height - thickness * 2f), color);
        Fill(new Rect(r.xMax - thickness, r.y + thickness, thickness, r.height - thickness * 2f), color);
    }

    public static void Diamond(Vector2 center, float radius, Color color)
    {
        Matrix4x4 old = GUI.matrix;
        GUIUtility.RotateAroundPivot(45f, center);
        Fill(new Rect(center.x - radius * .7071f, center.y - radius * .7071f, radius * 1.4142f, radius * 1.4142f), color);
        GUI.matrix = old;
    }

    /// <summary>Panel background plus the tier frame. <paramref name="dim"/> greys it out (locked weapon).</summary>
    public static void Frame(Rect r, int tier, float time, bool dim = false)
    {
        tier = Mathf.Clamp(tier, 1, 5);
        Color main = TierColor(tier);
        if (dim) main = Color.Lerp(main, new Color(.25f, .27f, .3f), .7f);
        float pulse = .5f + .5f * Mathf.Sin(time * 2.4f);

        if (tier >= 5)
        {
            // Halo: two soft expanded rectangles breathing behind the card.
            Fill(new Rect(r.x - 6, r.y - 6, r.width + 12, r.height + 12), new Color(Gold.r, Gold.g, Gold.b, .05f + .06f * pulse));
            Fill(new Rect(r.x - 3, r.y - 3, r.width + 6, r.height + 6), new Color(Gold.r, Gold.g, Gold.b, .08f + .08f * pulse));
        }
        Fill(r, Panel);
        // Subtle top-light so the panel does not read as a flat rectangle.
        Fill(new Rect(r.x, r.y, r.width, Mathf.Min(46f, r.height * .3f)), new Color(main.r, main.g, main.b, .06f));

        Border(r, tier >= 2 ? 2f : 1f, new Color(main.r, main.g, main.b, tier == 1 ? .55f : .9f));
        if (tier >= 2) Brackets(r, tier >= 4 ? 22f : 16f, 4f, main);
        if (tier == 3 || tier == 5)
        {
            var inner = new Rect(r.x + 6, r.y + 6, r.width - 12, r.height - 12);
            Color glow = tier == 5 ? White : Violet;
            Border(inner, 1f, new Color(glow.r, glow.g, glow.b, .15f + .45f * pulse));
        }
        if (tier >= 4)
        {
            Border(new Rect(r.x + 5, r.y + 5, r.width - 10, r.height - 10), 1f, new Color(main.r, main.g, main.b, .55f));
            float d = 6f;
            Diamond(new Vector2(r.x, r.y), d, main); Diamond(new Vector2(r.xMax, r.y), d, main);
            Diamond(new Vector2(r.x, r.yMax), d, main); Diamond(new Vector2(r.xMax, r.yMax), d, main);
            Diamond(new Vector2(r.center.x, r.y), d * .8f, main); Diamond(new Vector2(r.center.x, r.yMax), d * .8f, main);
        }
        if (tier >= 5) Shimmer(r, time);
    }

    /// <summary>Thick L-shaped corner brackets (the "circuit" look).</summary>
    private static void Brackets(Rect r, float length, float thickness, Color color)
    {
        Fill(new Rect(r.x - 1, r.y - 1, length, thickness), color); Fill(new Rect(r.x - 1, r.y - 1, thickness, length), color);
        Fill(new Rect(r.xMax - length + 1, r.y - 1, length, thickness), color); Fill(new Rect(r.xMax - thickness + 1, r.y - 1, thickness, length), color);
        Fill(new Rect(r.x - 1, r.yMax - thickness + 1, length, thickness), color); Fill(new Rect(r.x - 1, r.yMax - length + 1, thickness, length), color);
        Fill(new Rect(r.xMax - length + 1, r.yMax - thickness + 1, length, thickness), color); Fill(new Rect(r.xMax - thickness + 1, r.yMax - length + 1, thickness, length), color);
    }

    /// <summary>A bright segment that runs clockwise along the border (signature tier).</summary>
    private static void Shimmer(Rect r, float time)
    {
        float perimeter = 2f * (r.width + r.height), length = 70f;
        float head = Mathf.Repeat(time * 180f, perimeter);
        for (int i = 0; i < 7; i++)
        {
            float at = Mathf.Repeat(head - i * (length / 7f), perimeter);
            float alpha = 1f - i / 7f;
            Vector2 p = PointOnPerimeter(r, at);
            Fill(new Rect(p.x - 2.5f, p.y - 2.5f, 5f, 5f), new Color(White.r, White.g, White.b, alpha * .9f));
        }
    }

    private static Vector2 PointOnPerimeter(Rect r, float distance)
    {
        if (distance < r.width) return new Vector2(r.x + distance, r.y);
        distance -= r.width;
        if (distance < r.height) return new Vector2(r.xMax, r.y + distance);
        distance -= r.height;
        if (distance < r.width) return new Vector2(r.xMax - distance, r.yMax);
        distance -= r.width;
        return new Vector2(r.x, r.yMax - distance);
    }

    /// <summary>Rapport pips: filled diamonds up to <paramref name="level"/>, hollow after; the next one fills with <paramref name="progress"/>.</summary>
    public static void Pips(Rect row, int level, int max, float progress, float time)
    {
        float gap = Mathf.Min(34f, row.width / Mathf.Max(1, max));
        float radius = Mathf.Min(9f, row.height * .45f);
        Color main = TierColor(level);
        for (int i = 0; i < max; i++)
        {
            var center = new Vector2(row.x + radius + 2f + i * gap, row.center.y);
            if (i > 0) Fill(new Rect(center.x - gap + radius, center.y - 1f, gap - radius * 2f, 2f), new Color(main.r, main.g, main.b, i < level ? .8f : .25f));
            if (i < level)
            {
                float pop = i == level - 1 ? 1f + .12f * Mathf.Sin(time * 3f) : 1f;
                Diamond(center, radius * pop, main);
                Diamond(center, radius * .45f, White);
            }
            else
            {
                Diamond(center, radius, new Color(main.r, main.g, main.b, .35f));
                Diamond(center, radius * .72f, Panel);
                if (i == level && progress > 0f) Diamond(center, radius * .72f * Mathf.Clamp01(progress), new Color(Gold.r, Gold.g, Gold.b, .85f));
            }
        }
    }
}
