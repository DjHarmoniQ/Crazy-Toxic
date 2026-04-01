using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the in-game Pause Menu.
///
/// Shown / hidden via <see cref="GameManager.TogglePause"/> (Escape key).
/// Features:
/// <list type="bullet">
///   <item>Dark overlay to dim the background (<c>Time.timeScale = 0</c> freezes the world).</item>
///   <item>Live run stats panel: wave, kills, cards, time elapsed.</item>
///   <item>"Are you sure?" confirmation popup for Quit and Restart.</item>
///   <item>Buttons: Resume, Restart Run, Settings, Quit to Main Menu.</item>
/// </list>
///
/// Attach to: The PauseMenu panel inside the gameplay Canvas.
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Buttons")]
    [Tooltip("Resumes the game and closes the pause menu.")]
    [SerializeField] private Button resumeButton;

    [Tooltip("Opens the restart confirmation popup.")]
    [SerializeField] private Button restartButton;

    [Tooltip("Opens the settings menu panel.")]
    [SerializeField] private Button settingsButton;

    [Tooltip("Opens the quit-to-main-menu confirmation popup.")]
    [SerializeField] private Button quitButton;

    [Header("Run Stats Labels")]
    [Tooltip("Displays the current wave number.")]
    [SerializeField] private TextMeshProUGUI waveLabel;

    [Tooltip("Displays the number of enemies killed this run.")]
    [SerializeField] private TextMeshProUGUI killsLabel;

    [Tooltip("Displays the number of cards collected this run.")]
    [SerializeField] private TextMeshProUGUI cardsLabel;

    [Tooltip("Displays the elapsed run time in mm:ss format.")]
    [SerializeField] private TextMeshProUGUI timeLabel;

    [Header("Confirmation Popup")]
    [Tooltip("The 'Are you sure?' confirmation panel.")]
    [SerializeField] private GameObject confirmPopup;

    [Tooltip("Text on the confirmation popup describing the pending action.")]
    [SerializeField] private TextMeshProUGUI confirmMessageText;

    [Tooltip("Yes / Confirm button on the popup.")]
    [SerializeField] private Button confirmYesButton;

    [Tooltip("No / Cancel button on the popup.")]
    [SerializeField] private Button confirmNoButton;

    [Header("Panels")]
    [Tooltip("The Settings panel to open when the Settings button is pressed.")]
    [SerializeField] private GameObject settingsPanel;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private System.Action _pendingConfirmAction;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        resumeButton?.onClick.AddListener(OnResumeClicked);
        restartButton?.onClick.AddListener(OnRestartClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
        quitButton?.onClick.AddListener(OnQuitClicked);
        confirmYesButton?.onClick.AddListener(OnConfirmYes);
        confirmNoButton?.onClick.AddListener(OnConfirmNo);
    }

    private void OnEnable()
    {
        // Hide the confirmation popup whenever the pause screen is shown
        confirmPopup?.SetActive(false);
        RefreshStats();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Stats
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Reads live run data and updates the stat labels.</summary>
    private void RefreshStats()
    {
        int wave = GameManager.Instance != null ? GameManager.Instance.CurrentWave : 0;
        if (waveLabel != null) waveLabel.text = $"Wave: {wave}";

        if (RunStatsTracker.Instance != null)
        {
            if (killsLabel != null) killsLabel.text = $"Kills: {RunStatsTracker.Instance.EnemiesKilled}";
            if (cardsLabel != null) cardsLabel.text = $"Cards: {RunStatsTracker.Instance.CardsCollected}";
            float elapsed = RunStatsTracker.Instance.RunDuration;
            int m = (int)(elapsed / 60f);
            int s = (int)(elapsed % 60f);
            if (timeLabel != null) timeLabel.text = $"Time: {m:00}:{s:00}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Confirmation Popup
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Shows the confirmation popup with a custom message and registers the action to take on "Yes".</summary>
    private void ShowConfirm(string message, System.Action onConfirm)
    {
        _pendingConfirmAction = onConfirm;
        if (confirmMessageText != null) confirmMessageText.text = message;
        confirmPopup?.SetActive(true);
    }

    private void OnConfirmYes()
    {
        confirmPopup?.SetActive(false);
        _pendingConfirmAction?.Invoke();
        _pendingConfirmAction = null;
    }

    private void OnConfirmNo()
    {
        confirmPopup?.SetActive(false);
        _pendingConfirmAction = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Button Callbacks
    // ─────────────────────────────────────────────────────────────────────────

    private void OnResumeClicked()
    {
        GameManager.Instance?.ResumeGame();
        gameObject.SetActive(false);
    }

    private void OnRestartClicked()
    {
        ShowConfirm("Restart this run?\nAll progress will be lost.", () =>
        {
            Time.timeScale = 1f;
            GameManager.Instance?.RestartGame();
        });
    }

    private void OnSettingsClicked()
    {
        settingsPanel?.SetActive(true);
    }

    private void OnQuitClicked()
    {
        ShowConfirm("Quit to Main Menu?\nAll progress will be lost.", () =>
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        });
    }
}
