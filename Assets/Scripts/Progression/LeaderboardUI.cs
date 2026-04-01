using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Populates and manages the leaderboard screen UI.
///
/// Attach to: The root GameObject of the Leaderboard canvas or panel.
///
/// Wire-up in the Inspector:
/// <list type="bullet">
///   <item><see cref="rowPrefab"/> — prefab that has a <see cref="LeaderboardRowUI"/> component.</item>
///   <item><see cref="rowContainer"/> — parent Transform where rows are spawned.</item>
///   <item><see cref="clearButton"/> — "Clear Scores" button.</item>
///   <item><see cref="confirmationPanel"/> — panel shown to confirm clearing scores.</item>
///   <item><see cref="confirmClearButton"/> — button inside the confirmation panel.</item>
///   <item><see cref="cancelClearButton"/> — cancels the clear action.</item>
/// </list>
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Row Prefab & Container")]
    [Tooltip("Prefab with a LeaderboardRowUI component, instantiated once per entry.")]
    [SerializeField] private LeaderboardRowUI rowPrefab;

    [Tooltip("Parent Transform that rows are added to (e.g. a Vertical Layout Group).")]
    [SerializeField] private Transform rowContainer;

    [Header("Buttons")]
    [Tooltip("Button that opens the clear-scores confirmation popup.")]
    [SerializeField] private Button clearButton;

    [Header("Confirmation Popup")]
    [Tooltip("Panel shown to ask the player to confirm clearing all scores.")]
    [SerializeField] private GameObject confirmationPanel;

    [Tooltip("Confirm button inside the confirmation popup.")]
    [SerializeField] private Button confirmClearButton;

    [Tooltip("Cancel button inside the confirmation popup.")]
    [SerializeField] private Button cancelClearButton;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Score of the run that was most recently submitted (0 = none).</summary>
    private int _currentRunScore;
    private readonly List<LeaderboardRowUI> _rows = new List<LeaderboardRowUI>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Wire up buttons
        if (clearButton        != null) clearButton.onClick.AddListener(OnClearClicked);
        if (confirmClearButton != null) confirmClearButton.onClick.AddListener(OnConfirmClear);
        if (cancelClearButton  != null) cancelClearButton.onClick.AddListener(OnCancelClear);

        // Hide confirmation panel initially
        if (confirmationPanel != null) confirmationPanel.SetActive(false);

        RefreshRows();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the score of the most recently played run so its row can be highlighted.
    /// Call this before the leaderboard panel is shown.
    /// </summary>
    /// <param name="score">Score from the current run.</param>
    public void SetCurrentRunScore(int score)
    {
        _currentRunScore = score;
        RefreshRows();
    }

    /// <summary>
    /// Destroys all existing rows and re-instantiates them from
    /// <see cref="LocalLeaderboard.GetTopScores"/>.
    /// </summary>
    public void RefreshRows()
    {
        if (rowPrefab == null || rowContainer == null) return;

        // Destroy existing rows
        foreach (LeaderboardRowUI row in _rows)
            if (row != null) Destroy(row.gameObject);
        _rows.Clear();

        LeaderboardEntry[] entries = LocalLeaderboard.Instance != null
            ? LocalLeaderboard.Instance.GetTopScores()
            : System.Array.Empty<LeaderboardEntry>();

        for (int i = 0; i < entries.Length; i++)
        {
            bool isCurrentRun = _currentRunScore > 0 && entries[i].score == _currentRunScore;
            LeaderboardRowUI row = Instantiate(rowPrefab, rowContainer);
            row.Populate(i + 1, entries[i], isCurrentRun);
            _rows.Add(row);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Button Handlers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Shows the confirmation popup when the "Clear Scores" button is pressed.</summary>
    private void OnClearClicked()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(true);
    }

    /// <summary>Confirms clearing all leaderboard entries.</summary>
    private void OnConfirmClear()
    {
        if (LocalLeaderboard.Instance != null)
            LocalLeaderboard.Instance.ClearScores();

        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        RefreshRows();
    }

    /// <summary>Cancels the clear operation and hides the confirmation popup.</summary>
    private void OnCancelClear()
    {
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }
}
