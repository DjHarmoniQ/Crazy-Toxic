using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Serializable container for all persistent player data.
/// Stored as JSON in <see cref="Application.persistentDataPath"/>.
/// </summary>
[Serializable]
public class SaveData
{
    // ── XP / Level ────────────────────────────────────────────────────────────
    /// <summary>Total XP accumulated across all runs.</summary>
    public int totalXP;

    /// <summary>Current player level (1–50).</summary>
    public int playerLevel;

    // ── Run Statistics ────────────────────────────────────────────────────────
    /// <summary>Cumulative enemies killed across all runs.</summary>
    public int totalKills;

    /// <summary>Number of full runs completed.</summary>
    public int totalRunsCompleted;

    /// <summary>Highest wave reached in any single run.</summary>
    public int highestWave;

    // ── Unlocks ───────────────────────────────────────────────────────────────
    /// <summary>Names of characters the player has unlocked.</summary>
    public string[] unlockedCharacters = Array.Empty<string>();

    /// <summary>Names of ability cards the player has unlocked.</summary>
    public string[] unlockedCards = Array.Empty<string>();

    // ── Leaderboard ───────────────────────────────────────────────────────────
    /// <summary>Top-10 run scores sorted highest-first.</summary>
    public int[] highScores = Array.Empty<int>();

    /// <summary>Serialised <see cref="LeaderboardEntry"/> list (JSON array).</summary>
    public string leaderboardJson = "[]";

    // ── Achievements ──────────────────────────────────────────────────────────
    /// <summary>
    /// One boolean per <see cref="AchievementID"/> value.
    /// Index matches the integer value of the enum.
    /// </summary>
    public bool[] achievementsUnlocked = new bool[30];

    // ── Misc ──────────────────────────────────────────────────────────────────
    /// <summary>Cumulative seconds of play time across all sessions.</summary>
    public float totalPlayTime;

    /// <summary>Total gold earned across all runs.</summary>
    public int totalGold;
}

/// <summary>
/// Static helper that serialises/deserialises <see cref="SaveData"/> as JSON
/// and writes it to <see cref="Application.persistentDataPath"/>.
///
/// Usage:
/// <code>
///   SaveData data = SaveSystem.Load();
///   data.totalXP += 50;
///   SaveSystem.Save(data);
/// </code>
/// </summary>
public static class SaveSystem
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    private const string SaveFileName = "savegame.json";

    /// <summary>Full path to the save file on disk.</summary>
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Serialises <paramref name="data"/> to JSON and writes it to disk.
    /// Existing save data is overwritten.
    /// </summary>
    /// <param name="data">The <see cref="SaveData"/> instance to persist.</param>
    public static void Save(SaveData data)
    {
        if (data == null) throw new ArgumentNullException(nameof(data));

        string json = JsonUtility.ToJson(data, prettyPrint: true);
        File.WriteAllText(SaveFilePath, json);
        Debug.Log($"[SaveSystem] Saved to: {SaveFilePath}");
    }

    /// <summary>
    /// Loads and deserialises save data from disk.
    /// Returns a fresh <see cref="SaveData"/> if no file is found.
    /// </summary>
    /// <returns>The loaded (or newly created) <see cref="SaveData"/>.</returns>
    public static SaveData Load()
    {
        if (!File.Exists(SaveFilePath))
        {
            Debug.Log("[SaveSystem] No save file found — returning default SaveData.");
            return new SaveData();
        }

        string json = File.ReadAllText(SaveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // Guard against null arrays after deserialisation
        data.unlockedCharacters ??= Array.Empty<string>();
        data.unlockedCards      ??= Array.Empty<string>();
        data.highScores         ??= Array.Empty<int>();

        if (data.achievementsUnlocked == null || data.achievementsUnlocked.Length < 30)
        {
            bool[] extended = new bool[30];
            if (data.achievementsUnlocked != null)
                Array.Copy(data.achievementsUnlocked, extended,
                    Math.Min(data.achievementsUnlocked.Length, 30));
            data.achievementsUnlocked = extended;
        }

        Debug.Log($"[SaveSystem] Loaded from: {SaveFilePath}");
        return data;
    }

    /// <summary>Returns <c>true</c> when a save file exists on disk.</summary>
    public static bool HasSaveData() => File.Exists(SaveFilePath);

    /// <summary>
    /// Permanently deletes the save file from disk.
    /// Typically called from a "Reset Save" option in the settings menu.
    /// </summary>
    public static void DeleteSave()
    {
        if (File.Exists(SaveFilePath))
        {
            File.Delete(SaveFilePath);
            Debug.Log("[SaveSystem] Save file deleted.");
        }
        else
        {
            Debug.Log("[SaveSystem] No save file to delete.");
        }
    }
}
