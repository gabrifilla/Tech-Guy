using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>One run in the meta-progress history (newest last, capped).</summary>
[Serializable]
public sealed class MetaRunRecord
{
    public string sessionId = "";
    public string outcome = "";
    public int fragmentsEarned;
    public string allyRescued = "";
    public string startedUtc = "";
    public string finishedUtc = "";
}

/// <summary>
/// Persistent meta-progression profile, schema v2 (v1 + Fragmentos, upgrades, rescued allies and run history).
/// Field names are the JSON contract — rename only with a schema migration.
/// </summary>
[Serializable]
public sealed class MetaProgressProfile
{
    public const int CurrentSchemaVersion = 2;

    public int schemaVersion = CurrentSchemaVersion;
    public int runsStarted;
    public int runsFinished;
    public int victories;
    public int deaths;
    public int abandons;
    public List<string> universesCleared = new List<string>();
    public string lastUpdatedUtc = "";
    // v2
    public int fragments;
    public int fragmentsEarnedTotal;
    public List<string> upgrades = new List<string>();
    public List<string> rescuedAllies = new List<string>();
    public List<MetaRunRecord> runHistory = new List<MetaRunRecord>();
    // v2, added 10/11/2026 (additive: a missing field reads as empty) — dialogue keys already shown in full.
    public List<string> seenDialogues = new List<string>();

    public bool HasUpgrade(string id) => upgrades != null && upgrades.Contains(id);
    public bool HasAlly(string id) => rescuedAllies != null && rescuedAllies.Contains(id);
    public bool HasSeenDialogue(string key) => seenDialogues != null && seenDialogues.Contains(key);
    public MetaRunRecord FindRun(string sessionId)
    {
        if (string.IsNullOrEmpty(sessionId) || runHistory == null) return null;
        for (int i = runHistory.Count - 1; i >= 0; i--) if (runHistory[i] != null && runHistory[i].sessionId == sessionId) return runHistory[i];
        return null;
    }
}

/// <summary>
/// JSON file store for <see cref="MetaProgressProfile"/> in its own file under <c>Application.persistentDataPath</c>
/// (not PlayerPrefs, so the fixed PlayerPrefs key lists that tools and tests back up never touch it). The path is
/// injected; tests always use a temporary folder. A missing, unreadable or corrupted file yields a fresh profile and a
/// log warning and never breaks the game; a corrupted file is copied aside (<c>.corrupt</c>).
/// Schema v1 (<c>MetaProgress.v1.json</c>, Fase 1a) is read automatically and migrated: counters are kept and the v2
/// fields start empty — the legacy PlayerPrefs coin balance is intentionally NOT migrated. The v1 file is left as is.
/// </summary>
public sealed class MetaProgressStore
{
    public const string FileName = "MetaProgress.json";
    public const string LegacyV1FileName = "MetaProgress.v1.json";
    public const string Universe1 = "universe-1";
    public const int HistoryLimit = 20;

    /// <summary>Test-only redirection of <see cref="CreateDefault"/>; null means the real persistent profile.</summary>
    internal static string DirectoryOverride;

    public string FilePath { get; }
    public string LegacyV1Path => Path.Combine(Path.GetDirectoryName(FilePath) ?? "", LegacyV1FileName);

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
        string source = File.Exists(FilePath) ? FilePath : File.Exists(LegacyV1Path) ? LegacyV1Path : null;
        if (source == null)
        {
            Debug.LogWarning("[MetaProgress] No profile at '" + FilePath + "'; starting a new one.");
            return new MetaProgressProfile();
        }
        string reason;
        try
        {
            var profile = JsonUtility.FromJson<MetaProgressProfile>(File.ReadAllText(source));
            if (TryValidateAndMigrate(profile, out reason)) return profile;
        }
        catch (Exception exception) { reason = exception.GetType().Name + ": " + exception.Message; }
        Debug.LogWarning("[MetaProgress] Unreadable profile at '" + source + "' (" + reason + "); starting a new one. A copy is kept as '" + Path.GetFileName(source) + ".corrupt'.");
        PreserveCorruptCopy(source);
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
            Normalize(profile);
            profile.schemaVersion = MetaProgressProfile.CurrentSchemaVersion;
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

    public MetaProgressProfile RecordRunStarted(string sessionId = null)
    {
        var profile = Load();
        profile.runsStarted++;
        if (!string.IsNullOrEmpty(sessionId) && profile.FindRun(sessionId) == null)
        {
            profile.runHistory.Add(new MetaRunRecord { sessionId = sessionId, startedUtc = DateTime.UtcNow.ToString("o") });
            while (profile.runHistory.Count > HistoryLimit) profile.runHistory.RemoveAt(0);
        }
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
    public MetaProgressProfile RecordRunFinished(RunOutcome outcome, string universeId, bool bossDefeated = false, string sessionId = null)
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
        var record = profile.FindRun(sessionId);
        if (record != null) { record.outcome = bossDefeated ? "Victory" : outcome == RunOutcome.Extracted ? "Victory" : outcome == RunOutcome.Dead ? "Death" : "Aborted"; record.finishedUtc = DateTime.UtcNow.ToString("o"); }
        Save(profile);
        return profile;
    }

    /// <summary>Banks Fragmentos immediately (kept on death and abandon). Non-positive amounts are ignored.</summary>
    public MetaProgressProfile AddFragments(int amount, string sessionId = null)
    {
        if (amount <= 0) return null;
        var profile = Load();
        profile.fragments = checked(profile.fragments + amount);
        profile.fragmentsEarnedTotal = checked(profile.fragmentsEarnedTotal + amount);
        var record = profile.FindRun(sessionId);
        if (record != null) record.fragmentsEarned += amount;
        Save(profile);
        return profile;
    }

    /// <summary>Buys an upgrade once. False when already owned, unknown cost, or the balance is short.</summary>
    public bool TryPurchase(string upgradeId, int cost, out MetaProgressProfile profile)
    {
        profile = Load();
        if (string.IsNullOrEmpty(upgradeId) || cost < 0 || profile.HasUpgrade(upgradeId) || profile.fragments < cost) return false;
        profile.fragments -= cost;
        profile.upgrades.Add(upgradeId);
        return Save(profile);
    }

    /// <summary>Saves a rescue at once (survives a later death). Idempotent per ally.</summary>
    public MetaProgressProfile RecordAllyRescued(string allyId, string sessionId = null)
    {
        var profile = Load();
        if (!string.IsNullOrEmpty(allyId) && !profile.HasAlly(allyId)) profile.rescuedAllies.Add(allyId);
        var record = profile.FindRun(sessionId);
        if (record != null) record.allyRescued = allyId ?? "";
        Save(profile);
        return profile;
    }

    /// <summary>Marks a dialogue as seen (saved at once). Idempotent.</summary>
    public MetaProgressProfile RecordDialogueSeen(string dialogueKey)
    {
        var profile = Load();
        if (string.IsNullOrEmpty(dialogueKey) || profile.HasSeenDialogue(dialogueKey)) return profile;
        profile.seenDialogues.Add(dialogueKey);
        Save(profile);
        return profile;
    }

    private static void AddUniverse(MetaProgressProfile profile, string universeId)
    {
        if (!string.IsNullOrEmpty(universeId) && !profile.universesCleared.Contains(universeId)) profile.universesCleared.Add(universeId);
    }

    private static void Normalize(MetaProgressProfile profile)
    {
        profile.universesCleared ??= new List<string>();
        profile.upgrades ??= new List<string>();
        profile.rescuedAllies ??= new List<string>();
        profile.runHistory ??= new List<MetaRunRecord>();
        profile.runHistory.RemoveAll(record => record == null);
        profile.seenDialogues ??= new List<string>();
        // Retired upgrades are refunded and removed (idempotent: the file only changes on the next save).
        foreach (var retired in MetaUpgrades.RetiredRefunds)
            if (profile.upgrades.Remove(retired.Key)) profile.fragments += retired.Value;
    }

    private static bool TryValidateAndMigrate(MetaProgressProfile profile, out string reason)
    {
        reason = null;
        if (profile == null) reason = "empty document";
        else if (profile.schemaVersion != 1 && profile.schemaVersion != MetaProgressProfile.CurrentSchemaVersion) reason = "schemaVersion " + profile.schemaVersion;
        else if (profile.runsStarted < 0 || profile.runsFinished < 0 || profile.victories < 0 || profile.deaths < 0 || profile.abandons < 0 ||
                 profile.fragments < 0 || profile.fragmentsEarnedTotal < 0)
            reason = "negative counter";
        if (reason != null) return false;
        Normalize(profile);
        if (profile.schemaVersion == 1)
        {
            // v1 → v2: keep run counters; Fragmentos, upgrades, allies and history start empty.
            profile.fragments = 0; profile.fragmentsEarnedTotal = 0;
            profile.upgrades.Clear(); profile.rescuedAllies.Clear(); profile.runHistory.Clear();
            profile.schemaVersion = MetaProgressProfile.CurrentSchemaVersion;
        }
        return true;
    }

    private static void PreserveCorruptCopy(string source)
    {
        try { File.Copy(source, source + ".corrupt", true); }
        catch (Exception exception) { Debug.LogWarning("[MetaProgress] Could not keep a copy of the unreadable profile: " + exception.Message); }
    }
}
