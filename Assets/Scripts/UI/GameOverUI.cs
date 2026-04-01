using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays the Game Over / Victory screen at the end of a run.
///
/// Triggered by subscribing to <see cref="GameManager.OnGameOver"/> (or called directly).
/// Shows:
/// <list type="bullet">
///   <item>"GAME OVER" or "VICTORY!" title depending on whether wave 100 was reached.</item>
///   <item>Full run summary from <see cref="RunStatsTracker"/>.</item>
///   <item>XP earned this run (animated counting-up effect).</item>
///   <item>Any achievements unlocked this run.</item>
///   <item>Buttons: Retry, Change Character, Main Menu.</item>
/// </list>
///
/// Attach to: The GameOver panel inside the gameplay Canvas (starts inactive).
/// </summary>
public class GameOverUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Title")]
    [Tooltip("Main title label — shows 'GAME OVER' or 'VICTORY!'.")]
    [SerializeField] private TextMeshProUGUI titleText;

    [Header("Run Summary Labels")]
    [Tooltip("Displays the wave reached at run end.")]
    [SerializeField] private TextMeshProUGUI waveReachedText;

    [Tooltip("Displays the total enemies killed.")]
    [SerializeField] private TextMeshProUGUI enemiesKilledText;

    [Tooltip("Displays the highest combo reached.")]
    [SerializeField] private TextMeshProUGUI highestComboText;

    [Tooltip("Displays the number of cards collected.")]
    [SerializeField] private TextMeshProUGUI cardsCollectedText;

    [Tooltip("Displays the total run time in mm:ss format.")]
    [SerializeField] private TextMeshProUGUI runTimeText;

    [Header("XP Counter")]
    [Tooltip("Label that animates from 0 to the XP gained this run.")]
    [SerializeField] private TextMeshProUGUI xpGainedText;

    [Tooltip("Duration in seconds of the XP counter animation.")]
    [SerializeField] private float xpCountDuration = 2f;

    [Header("Achievements")]
    [Tooltip("Parent container for the achievement unlock list items.")]
    [SerializeField] private Transform achievementsContainer;

    [Tooltip("Prefab for a single achievement row in the unlocked list.")]
    [SerializeField] private GameObject achievementRowPrefab;

    [Header("Buttons")]
    [Tooltip("Restarts the run with the same character and mode.")]
    [SerializeField] private Button retryButton;

    [Tooltip("Returns to the character-select screen.")]
    [SerializeField] private Button changeCharacterButton;

    [Tooltip("Returns to the main menu.")]
    [SerializeField] private Button mainMenuButton;

    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    private const int VictoryWave = 100;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        retryButton?.onClick.AddListener(OnRetryClicked);
        changeCharacterButton?.onClick.AddListener(OnChangeCharacterClicked);
        mainMenuButton?.onClick.AddListener(OnMainMenuClicked);
        gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Opens the Game Over screen and populates it with data from <see cref="RunStatsTracker"/>
    /// and <see cref="XPSystem"/>.
    /// </summary>
    /// <param name="xpGained">XP awarded for this run (animated counter).</param>
    /// <param name="unlockedAchievements">Achievement names unlocked during the run.</param>
    public void Show(int xpGained, string[] unlockedAchievements = null)
    {
        gameObject.SetActive(true);
        Time.timeScale = 0f;

        int wave = GameManager.Instance != null ? GameManager.Instance.CurrentWave : 0;
        bool isVictory = wave >= VictoryWave;

        if (titleText != null)
            titleText.text = isVictory ? "VICTORY!" : "GAME OVER";

        PopulateSummary(wave);
        PopulateAchievements(unlockedAchievements);

        StopAllCoroutines();
        StartCoroutine(AnimateXPCounter(xpGained));

        // Auto-submit score to leaderboard
        if (LocalLeaderboard.Instance != null && RunStatsTracker.Instance != null)
        {
            string charName = GameManager.Instance?.SelectedCharacter?.characterName ?? "Unknown";
            var entry = new LeaderboardEntry
            {
                playerName    = charName,
                characterName = charName,
                wave          = wave,
                score         = LeaderboardEntry.ComputeScore(wave, RunStatsTracker.Instance.EnemiesKilled, RunStatsTracker.Instance.GoldEarned),
                runTime       = RunStatsTracker.Instance.RunDuration,
                dateTicks     = System.DateTime.UtcNow.Ticks.ToString()
            };
            LocalLeaderboard.Instance.SubmitScore(entry);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Data Population
    // ─────────────────────────────────────────────────────────────────────────

    private void PopulateSummary(int wave)
    {
        if (waveReachedText    != null) waveReachedText.text    = $"Wave Reached: {wave}";

        if (RunStatsTracker.Instance == null) return;
        if (enemiesKilledText != null) enemiesKilledText.text = $"Enemies Killed: {RunStatsTracker.Instance.EnemiesKilled}";
        if (highestComboText  != null) highestComboText.text  = $"Highest Combo: x{RunStatsTracker.Instance.HighestCombo}";
        if (cardsCollectedText != null) cardsCollectedText.text = $"Cards Collected: {RunStatsTracker.Instance.CardsCollected}";

        float elapsed = RunStatsTracker.Instance.RunDuration;
        int m = (int)(elapsed / 60f);
        int s = (int)(elapsed % 60f);
        if (runTimeText != null) runTimeText.text = $"Run Time: {m:00}:{s:00}";
    }

    private void PopulateAchievements(string[] names)
    {
        if (achievementsContainer == null || achievementRowPrefab == null) return;

        // Clear old rows
        foreach (Transform child in achievementsContainer)
            Destroy(child.gameObject);

        if (names == null || names.Length == 0) return;

        foreach (string name in names)
        {
            GameObject row = Instantiate(achievementRowPrefab, achievementsContainer);
            TextMeshProUGUI label = row.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null) label.text = name;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — XP Animation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Counts the XP label up from 0 to <paramref name="target"/> over <see cref="xpCountDuration"/> seconds.</summary>
    private IEnumerator AnimateXPCounter(int target)
    {
        if (xpGainedText == null) yield break;

        float elapsed = 0f;
        while (elapsed < xpCountDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            int displayed = Mathf.RoundToInt(Mathf.Lerp(0f, target, elapsed / xpCountDuration));
            xpGainedText.text = $"+{displayed} XP";
            yield return null;
        }
        xpGainedText.text = $"+{target} XP";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Button Callbacks
    // ─────────────────────────────────────────────────────────────────────────

    private void OnRetryClicked()
    {
        Time.timeScale = 1f;
        GameManager.Instance?.RestartGame();
    }

    private void OnChangeCharacterClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("CharacterSelect");
    }

    private void OnMainMenuClicked()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}
