using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Editor-only review session. Reuses FirstSector and its normal lifecycle without editing it.</summary>
[InitializeOnLoad]
public static class NexusBlockoutPlaySession
{
    private const string Active = "TechGuy.NexusBlockout.Session";
    private const string Backup = "TechGuy.NexusBlockout.Preferences";
    [Serializable] private sealed class Entry { public string key; public bool exists; public int number; public string text; }
    [Serializable] private sealed class Snapshot { public Entry[] entries; }
    static NexusBlockoutPlaySession()
    {
        EditorApplication.playModeStateChanged += OnState;
        SceneManager.sceneLoaded += OnScene;
    }

    [MenuItem("Tools/Tech Guy/Nexus Blockout/Protect Next Review Session Save")]
    private static void ProtectNextReviewSession()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SessionState.SetBool("TechGuy.NexusBlockout.ReviewRequested", true);
    }

    private static void OnState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode &&
            EditorSceneManager.GetActiveScene().path == NexusBlockoutBuilder.ScenePath &&
            SessionState.GetBool("TechGuy.NexusBlockout.ReviewRequested", false) &&
            !SessionState.GetBool("VS034.Automated", false))
        {
            SessionState.SetBool("TechGuy.NexusBlockout.ReviewRequested", false);
            if (SessionState.GetBool(Active, false)) Restore();
            var keys = new[] { WeaponLoadout.PreferenceKey, CurrencyWallet.PreferenceKey,
                "TechGuy.Loadout.Unlocked.1", "TechGuy.Loadout.Unlocked.2", RunSummaryStore.PreferenceKey };
            SessionState.SetString(Backup, JsonUtility.ToJson(new Snapshot { entries = keys.Select(key => new Entry {
                key = key, exists = PlayerPrefs.HasKey(key), number = PlayerPrefs.GetInt(key), text = PlayerPrefs.GetString(key)
            }).ToArray() }));
            SessionState.SetBool(Active, true);
            Debug.Log("Nexus blockout review: existing save used without preparation. Changes to loadout, unlocks, coins and last result are restored on Stop. FirstSector returns to the isolated scene in this Editor session.");
        }
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Active, false)) Restore();
    }

    private static void OnScene(Scene scene, LoadSceneMode mode)
    {
        if (!SessionState.GetBool(Active, false) || scene.name != "FirstSector") return;
        foreach (var director in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ProgressionDirector>(true)))
        {
            var data = new SerializedObject(director);
            data.FindProperty("_returnScene").stringValue = "NexusBlockout";
            data.ApplyModifiedPropertiesWithoutUndo(); // Runtime instance only; never SaveScene.
        }
    }

    [MenuItem("Tools/Tech Guy/Nexus Blockout/Restore Interrupted Review Save")]
    public static void Restore()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before restoring review data.");
        string json = SessionState.GetString(Backup, "");
        if (string.IsNullOrEmpty(json)) return;
        foreach (var entry in JsonUtility.FromJson<Snapshot>(json).entries)
        {
            if (!entry.exists) PlayerPrefs.DeleteKey(entry.key);
            else if (entry.key == RunSummaryStore.PreferenceKey) PlayerPrefs.SetString(entry.key, entry.text);
            else PlayerPrefs.SetInt(entry.key, entry.number);
        }
        PlayerPrefs.Save(); SessionState.SetBool(Active, false); SessionState.EraseString(Backup);
        Debug.Log("Nexus blockout review: original save restored.");
    }
}
