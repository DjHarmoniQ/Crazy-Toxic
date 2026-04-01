using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A single entry in the local leaderboard.
/// </summary>
[Serializable]
public class LeaderboardEntry
{
    /// <summary>Player's display name.</summary>
    public string playerName;

    /// <summary>Name of the character used during this run.</summary>
    public string characterName;

    /// <summary>Highest wave reached in this run.</summary>
    public int wave;

    /// <summary>
    /// Composite score: <c>wave × 100 + totalKills × 10 + totalGold</c>.
    /// </summary>
    public int score;

    /// <summary>Total real-world seconds elapsed during this run.</summary>
    public float runTime;

    /// <summary>ISO-8601 string representation of the run date (<see cref="DateTime"/>).</summary>
    public string dateTicks; // serialised as string for JsonUtility compatibility

    // ── Computed property (not serialised) ───────────────────────────────────

    /// <summary>
    /// Run date parsed from <see cref="dateTicks"/>.
    /// Defaults to <see cref="DateTime.MinValue"/> when the string is empty.
    /// </summary>
    [NonSerialized]
    public DateTime date;

    /// <summary>
    /// Computes the composite score from raw run statistics.
    /// Formula: <c>wave × 100 + totalKills × 10 + totalGold</c>.
    /// </summary>
    /// <param name="wave">Highest wave reached.</param>
    /// <param name="totalKills">Enemies killed this run.</param>
    /// <param name="totalGold">Gold earned this run.</param>
    public static int ComputeScore(int wave, int totalKills, int totalGold) =>
        wave * 100 + totalKills * 10 + totalGold;
}

// ─────────────────────────────────────────────────────────────────────────────
//  Serialisation wrapper (JsonUtility cannot serialise List<T> at root level)
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Internal wrapper used only for JSON round-tripping a list of entries.</summary>
[Serializable]
internal class LeaderboardWrapper
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

/// <summary>
/// Singleton that maintains and persists a top-10 local leaderboard.
///
/// Usage:
/// <code>
///   var entry = new LeaderboardEntry { … };
///   LocalLeaderboard.Instance.SubmitScore(entry);
///   LeaderboardEntry[] top = LocalLeaderboard.Instance.GetTopScores();
/// </code>
/// </summary>
public class LocalLeaderboard : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Maximum number of entries kept on the leaderboard.</summary>
    public const int MaxEntries = 10;

    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="LocalLeaderboard"/> instance.</summary>
    public static LocalLeaderboard Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private readonly List<LeaderboardEntry> _entries = new List<LeaderboardEntry>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadFromSave();
    }

    private void OnApplicationQuit()
    {
        PersistToSave();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the current top-10 entries sorted from highest to lowest score.
    /// The returned array is a copy; modifying it does not affect the leaderboard.
    /// </summary>
    public LeaderboardEntry[] GetTopScores() => _entries.ToArray();

    /// <summary>
    /// Inserts <paramref name="entry"/> into the leaderboard, keeps the list sorted
    /// by score (descending), and trims to the top <see cref="MaxEntries"/>.
    /// The result is persisted immediately.
    /// </summary>
    /// <param name="entry">The run result to submit.</param>
    public void SubmitScore(LeaderboardEntry entry)
    {
        if (entry == null) throw new ArgumentNullException(nameof(entry));

        // Stamp date
        entry.date      = DateTime.Now;
        entry.dateTicks = entry.date.ToString("o");

        _entries.Add(entry);

        // Sort descending by score
        _entries.Sort((a, b) => b.score.CompareTo(a.score));

        // Trim to MaxEntries
        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(MaxEntries, _entries.Count - MaxEntries);

        PersistToSave();
        Debug.Log($"[LocalLeaderboard] Score submitted: {entry.score} by {entry.playerName}.");
    }

    /// <summary>
    /// Removes all leaderboard entries and persists the empty state.
    /// </summary>
    public void ClearScores()
    {
        _entries.Clear();
        PersistToSave();
        Debug.Log("[LocalLeaderboard] Scores cleared.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads the leaderboard JSON from the save file and deserialises entries.</summary>
    private void LoadFromSave()
    {
        if (!SaveSystem.HasSaveData()) return;

        SaveData data = SaveSystem.Load();

        if (string.IsNullOrWhiteSpace(data.leaderboardJson)) return;

        LeaderboardWrapper wrapper = JsonUtility.FromJson<LeaderboardWrapper>(data.leaderboardJson);
        if (wrapper?.entries == null) return;

        _entries.Clear();
        foreach (LeaderboardEntry e in wrapper.entries)
        {
            if (DateTime.TryParse(e.dateTicks, out DateTime dt))
                e.date = dt;
            _entries.Add(e);
        }

        Debug.Log($"[LocalLeaderboard] Loaded {_entries.Count} entries.");
    }

    /// <summary>Serialises the current entries and writes them to the save file.</summary>
    private void PersistToSave()
    {
        SaveData data = SaveSystem.Load();
        var wrapper = new LeaderboardWrapper { entries = _entries };
        data.leaderboardJson = JsonUtility.ToJson(wrapper);
        SaveSystem.Save(data);
    }
}
