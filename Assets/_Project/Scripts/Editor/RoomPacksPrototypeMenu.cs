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
