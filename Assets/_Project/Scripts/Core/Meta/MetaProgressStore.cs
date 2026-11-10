using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Persistent meta-progression profile, schema v1. Fase 1a keeps it invisible (no UI); Fase 1b builds on it.
/// Field names are the JSON contract — rename only with a schema migration.
/// </summary>
[Serializable]
public sealed class MetaProgressProfile
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;
    public int runsStarted;
    public int runsFinished;
    public int victories;
    public int deaths;
    public int abandons;
    public List<string> universesCleared = new List<string>();
    public string lastUpdatedUtc = "";
}

/// <summary>
/// JSON file store for <see cref="MetaProgressProfile"/>. It lives in its own file under
/// <c>Application.persistentDataPath</c>, not in PlayerPrefs, so the fixed PlayerPrefs key lists that editor tools
/// and tests back up and restore never touch it. The file path is injected; tests always use a temporary folder.
/// A missing, unreadable or corrupted file yields a fresh profile and a log warning — it never breaks the game.
/// A corrupted file is copied aside (<c>.corrupt</c>) before the next save overwrites it.
/// </summary>
public sealed class MetaProgressStore
{
    public const string FileName = "MetaProgress.v1.json";
    public const string Universe1 = "universe-1";

    /// <summary>Test-only redirection of <see cref="CreateDefault"/>; null means the real persistent profile.</summary>
    internal static string DirectoryOverride;

    public string FilePath { get; }

    public MetaProgressStore(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) throw new ArgumentException("A meta-progress file path is required.", nameof(filePath));
        FilePath = filePath;
    }

    public static string DefaultDirectory =>
        string.IsNullOrEmpty(DirectoryOverride) ? Application.persistentDataPath : DirectoryOverride;

    public static MetaProgressStore CreateDefault() => new MetaProgressStore(Path.Combine(DefaultDirectory, FileName));

    public MetaProgressProfile Load()
    {
        if (!File.Exists(FilePath))
        {
            Debug.LogWarning("[MetaProgress] No profile at '" + FilePath + "'; starting a new one.");
            return new MetaProgressProfile();
        }
        string reason;
        try
        {
            var profile = JsonUtility.FromJson<MetaProgressProfile>(File.ReadAllText(FilePath));
            if (TryValidate(profile, out reason)) return profile;
        }
        catch (Exception exception) { reason = exception.GetType().Name + ": " + exception.Message; }
        Debug.LogWarning("[MetaProgress] Unreadable profile at '" + FilePath + "' (" + reason + "); starting a new one. A copy is kept as '" + FileName + ".corrupt'.");
        PreserveCorruptCopy();
        return new MetaProgressProfile();
    }

    public bool Save(MetaProgressProfile profile)
    {
        if (profile == null) return false;
        string temporary = FilePath + ".tmp";
        try
        {
            string directory = Path.GetDirectoryName(FilePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            profile.schemaVersion = MetaProgressProfile.CurrentSchemaVersion;
            profile.universesCleared ??= new List<string>();
            profile.lastUpdatedUtc = DateTime.UtcNow.ToString("o");
            File.WriteAllText(temporary, JsonUtility.ToJson(profile, true));
            // Write-then-swap so an interrupted save never leaves a half-written profile.
            if (File.Exists(FilePath)) File.Replace(temporary, FilePath, null);
            else File.Move(temporary, FilePath);
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[MetaProgress] Could not save '" + FilePath + "': " + exception.Message);
            try { if (File.Exists(temporary)) File.Delete(temporary); } catch (Exception) { }
            return false;
        }
    }

    public MetaProgressProfile RecordRunStarted()
    {
        var profile = Load();
        profile.runsStarted++;
        Save(profile);
        return profile;
    }

    /// <summary>The universe boss died: this is the victory moment. Counts once per call; the caller owns per-run idempotency.</summary>
    public MetaProgressProfile RecordBossVictory(string universeId)
    {
        var profile = Load();
        profile.victories++;
        AddUniverse(profile, universeId);
        Save(profile);
        return profile;
    }

    /// <summary>
    /// Closes a run. When <paramref name="bossDefeated"/> the victory was already recorded by <see cref="RecordBossVictory"/>,
    /// so whatever happens afterwards (portal, abandon, a late death) only finishes the run. Otherwise Dead and Aborted
    /// count as such; an Extracted outcome without a recorded boss victory still counts as one (defensive fallback).
    /// </summary>
    public MetaProgressProfile RecordRunFinished(RunOutcome outcome, string universeId, bool bossDefeated = false)
    {
        if (outcome == RunOutcome.None) return null;
        var profile = Load();
        profile.runsFinished++;
        if (!bossDefeated)
        {
            if (outcome == RunOutcome.Extracted) { profile.victories++; AddUniverse(profile, universeId); }
            else if (outcome == RunOutcome.Dead) profile.deaths++;
            else profile.abandons++;
        }
        Save(profile);
        return profile;
    }

    private static void AddUniverse(MetaProgressProfile profile, string universeId)
    {
        if (!string.IsNullOrEmpty(universeId) && !profile.universesCleared.Contains(universeId)) profile.universesCleared.Add(universeId);
    }

    private static bool TryValidate(MetaProgressProfile profile, out string reason)
    {
        reason = null;
        if (profile == null) reason = "empty document";
        else if (profile.schemaVersion != MetaProgressProfile.CurrentSchemaVersion) reason = "schemaVersion " + profile.schemaVersion;
        else if (profile.runsStarted < 0 || profile.runsFinished < 0 || profile.victories < 0 || profile.deaths < 0 || profile.abandons < 0)
            reason = "negative counter";
        if (reason != null) return false;
        profile.universesCleared ??= new List<string>();
        return true;
    }

    private void PreserveCorruptCopy()
    {
        try { File.Copy(FilePath, FilePath + ".corrupt", true); }
        catch (Exception exception) { Debug.LogWarning("[MetaProgress] Could not keep a copy of the unreadable profile: " + exception.Message); }
    }
}
