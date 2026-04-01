using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays a countdown or count-up timer for <see cref="ModeSurvival"/> and
/// <see cref="ModeRushdown"/>.
///
/// Place this component on the timer panel in the game HUD.
/// It auto-detects the active mode on <c>Start</c> and configures itself accordingly:
/// <list type="bullet">
///   <item>Survival — countdown from 20:00, turns red when under 30 seconds.</item>
///   <item>Rushdown — count-up stopwatch, no colour change.</item>
/// </list>
/// </summary>
public class TimerUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Display")]
    [Tooltip("TextMeshPro label that shows the formatted time string.")]
    [SerializeField] private TextMeshProUGUI _timerLabel;

    [Tooltip("Normal text color used when time is not critical.")]
    [SerializeField] private Color _normalColor = Color.white;

    [Tooltip("Color applied when the Survival countdown drops below the warning threshold.")]
    [SerializeField] private Color _warningColor = Color.red;

    [Tooltip("Seconds remaining at which the Survival timer turns red.")]
    [SerializeField] private float _warningThreshold = 30f;

    [Header("Panel")]
    [Tooltip("Root GameObject of the timer panel. Hidden when the active mode does not use a timer.")]
    [SerializeField] private GameObject _timerPanel;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private enum TimerMode { None, Countdown, Countup }

    private TimerMode _timerMode = TimerMode.None;
    private ModeSurvival _survivalMode;
    private ModeRushdown _rushdownMode;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        DetectMode();
    }

    private void Update()
    {
        if (_timerMode == TimerMode.None) return;

        UpdateDisplay();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Checks the active game mode and configures the timer display accordingly.
    /// </summary>
    private void DetectMode()
    {
        if (GameModeManager.Instance == null)
        {
            SetVisible(false);
            return;
        }

        GameModeBase current = GameModeManager.Instance.CurrentMode;

        if (current is ModeSurvival survival)
        {
            _survivalMode = survival;
            _timerMode = TimerMode.Countdown;
            SetVisible(true);
        }
        else if (current is ModeRushdown rushdown)
        {
            _rushdownMode = rushdown;
            _timerMode = TimerMode.Countup;
            SetVisible(true);
        }
        else
        {
            SetVisible(false);
        }
    }

    /// <summary>Reads the current time value and updates the label text and colour.</summary>
    private void UpdateDisplay()
    {
        float seconds;
        bool isWarning = false;

        if (_timerMode == TimerMode.Countdown && _survivalMode != null)
        {
            seconds = _survivalMode.TimeRemaining;
            isWarning = seconds <= _warningThreshold;
        }
        else if (_timerMode == TimerMode.Countup && _rushdownMode != null)
        {
            seconds = _rushdownMode.ElapsedTime;
        }
        else
        {
            return;
        }

        if (_timerLabel != null)
        {
            _timerLabel.text = FormatTime(seconds);
            _timerLabel.color = isWarning ? _warningColor : _normalColor;
        }
    }

    /// <summary>Formats <paramref name="totalSeconds"/> as <c>MM:SS</c>.</summary>
    private static string FormatTime(float totalSeconds)
    {
        int mins = Mathf.FloorToInt(totalSeconds / 60f);
        int secs = Mathf.FloorToInt(totalSeconds % 60f);
        return $"{mins:00}:{secs:00}";
    }

    /// <summary>Shows or hides the timer panel.</summary>
    private void SetVisible(bool visible)
    {
        if (_timerPanel != null)
            _timerPanel.SetActive(visible);
    }
}
