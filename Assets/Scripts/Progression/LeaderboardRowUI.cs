using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Populates a single row in the leaderboard table.
///
/// Attach to: The <c>LeaderboardRow</c> prefab.
///
/// Wire-up in the Inspector:
/// <list type="bullet">
///   <item><see cref="rankText"/> — "#1", "#2", … rank label.</item>
///   <item><see cref="playerNameText"/> — player's display name.</item>
///   <item><see cref="characterNameText"/> — character played.</item>
///   <item><see cref="waveText"/> — highest wave reached.</item>
///   <item><see cref="scoreText"/> — composite score.</item>
///   <item><see cref="runTimeText"/> — run duration (mm:ss).</item>
///   <item><see cref="rowBackground"/> — Image tinted gold for the current run's entry.</item>
/// </list>
/// </summary>
public class LeaderboardRowUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Text Labels")]
    [Tooltip("Displays the rank number (e.g. '#1').")]
    [SerializeField] private TextMeshProUGUI rankText;

    [Tooltip("Player's display name.")]
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Tooltip("Character used during the run.")]
    [SerializeField] private TextMeshProUGUI characterNameText;

    [Tooltip("Highest wave reached in the run.")]
    [SerializeField] private TextMeshProUGUI waveText;

    [Tooltip("Composite score for the run.")]
    [SerializeField] private TextMeshProUGUI scoreText;

    [Tooltip("Total run time formatted as mm:ss.")]
    [SerializeField] private TextMeshProUGUI runTimeText;

    [Header("Highlight")]
    [Tooltip("Background image tinted gold when this row represents the current run.")]
    [SerializeField] private Image rowBackground;

    [Tooltip("Colour applied to the background when this row is highlighted.")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f, 1f); // gold

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Populates the row with <paramref name="entry"/> data.
    /// </summary>
    /// <param name="rank">1-based rank position.</param>
    /// <param name="entry">The leaderboard entry to display.</param>
    /// <param name="highlight">When <c>true</c> the row background is tinted gold.</param>
    public void Populate(int rank, LeaderboardEntry entry, bool highlight = false)
    {
        if (entry == null) return;

        if (rankText          != null) rankText.text          = $"#{rank}";
        if (playerNameText    != null) playerNameText.text    = entry.playerName;
        if (characterNameText != null) characterNameText.text = entry.characterName;
        if (waveText          != null) waveText.text          = entry.wave.ToString();
        if (scoreText         != null) scoreText.text         = entry.score.ToString("N0");

        if (runTimeText != null)
        {
            int totalSecs = Mathf.RoundToInt(entry.runTime);
            int mins      = totalSecs / 60;
            int secs      = totalSecs % 60;
            runTimeText.text = $"{mins:D2}:{secs:D2}";
        }

        if (rowBackground != null)
            rowBackground.color = highlight ? highlightColor : Color.white;
    }
}
