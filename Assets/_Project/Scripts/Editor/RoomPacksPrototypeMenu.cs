using UnityEditor;

/// <summary>Editor switch for room packs (Fornalha / Anzol). On by default since 11/11/2026; turn off to compare. Read at run start.</summary>
public static class RoomPacksPrototypeMenu
{
    private const string MenuPath = "Tech Guy/Protótipo/Salas-pacote (Fornalha · Anzol)";

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        bool enabled = !EditorPrefs.GetBool(ProgressionDirector.RoomPacksPrefKey, true);
        EditorPrefs.SetBool(ProgressionDirector.RoomPacksPrefKey, enabled);
        UnityEngine.Debug.Log("[Salas-pacote] " + (enabled ? "ligadas" : "DESLIGADAS (sorteio antigo)") + ". Vale a partir da próxima run.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(ProgressionDirector.RoomPacksPrefKey, true));
        return true;
    }
}

/// <summary>Editor switch for the enemy name labels (development aid). On by default.</summary>
public static class EnemyLabelsMenu
{
    private const string MenuPath = "Tech Guy/Protótipo/Nomes dos inimigos";

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        bool enabled = !EditorPrefs.GetBool(ProgressionDirector.EnemyLabelsPrefKey, true);
        EditorPrefs.SetBool(ProgressionDirector.EnemyLabelsPrefKey, enabled);
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(ProgressionDirector.EnemyLabelsPrefKey, true));
        return true;
    }
}

/// <summary>Fase 1c test aids: force the rare serene fishing room into every run, and grant a rapport gift.</summary>
public static class RapportPrototypeMenu
{
    private const string SerenePath = "Tech Guy/Protótipo/Sala serena em toda run (teste)";
    private const string GiftPath = "Tech Guy/Protótipo/Ganhar 1 Easter Egg (teste)";

    [MenuItem(SerenePath)]
    private static void ToggleSerene()
    {
        bool enabled = !EditorPrefs.GetBool(ProgressionDirector.ForceSereneRoomPrefKey, false);
        EditorPrefs.SetBool(ProgressionDirector.ForceSereneRoomPrefKey, enabled);
        UnityEngine.Debug.Log("[Sala serena] " + (enabled ? "forçada em toda run" : "rara (35% das runs)") + ". Vale a partir da próxima run.");
    }

    [MenuItem(SerenePath, true)]
    private static bool ToggleSereneValidate()
    {
        Menu.SetChecked(SerenePath, EditorPrefs.GetBool(ProgressionDirector.ForceSereneRoomPrefKey, false));
        return true;
    }

    [MenuItem(GiftPath)]
    private static void GrantGift()
    {
        var profile = MetaProgressStore.CreateDefault().AddGift();
        UnityEngine.Debug.Log("[Rapport] +1 " + Rapport.GiftName + " no perfil (total para entregar: " + profile.gifts + "). Reabra o Nexus para ver.");
    }
}
