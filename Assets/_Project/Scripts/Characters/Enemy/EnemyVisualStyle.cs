using UnityEngine;

public static class EnemyVisualStyle
{
    public static Color NameColor(EnemyRarity rarity, bool boss = false) => boss ? new Color(1f,.42f,.16f) :
        rarity == EnemyRarity.Rare ? new Color(1f,.85f,.2f) :
        rarity == EnemyRarity.Magic ? new Color(.3f,.65f,1f) : Color.white;
    public static float SizeMultiplier(EnemyRarity rarity, bool boss = false) => boss ? 2.1f : rarity == EnemyRarity.Rare ? 1.4f : 1f;
}
