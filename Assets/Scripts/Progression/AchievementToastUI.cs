using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Displays a slide-in achievement toast notification whenever an achievement
/// is unlocked. Toasts are queued so they never overlap.
///
/// Attach to: A persistent Canvas UI GameObject.
///
/// Wire-up in the Inspector:
/// <list type="bullet">
///   <item><see cref="toastPanel"/> — the RectTransform of the toast background panel.</item>
///   <item><see cref="achievementNameText"/> — TextMeshPro label for the achievement name.</item>
///   <item><see cref="achievementDescText"/> — TextMeshPro label for the description.</item>
///   <item><see cref="achievementIcon"/> — optional icon Image.</item>
/// </list>
/// </summary>
public class AchievementToastUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Toast Panel")]
    [Tooltip("RectTransform of the toast background panel.")]
    [SerializeField] private RectTransform toastPanel;

    [Header("Text Labels")]
    [Tooltip("TextMeshPro label that displays the achievement name.")]
    [SerializeField] private TextMeshProUGUI achievementNameText;

    [Tooltip("TextMeshPro label that displays the achievement description.")]
    [SerializeField] private TextMeshProUGUI achievementDescText;

    [Header("Icon")]
    [Tooltip("Optional Image component for the achievement icon.")]
    [SerializeField] private Image achievementIcon;

    [Header("Animation Settings")]
    [Tooltip("Duration in seconds for the slide-in / slide-out tween.")]
    [SerializeField] private float slideDuration = 0.4f;

    [Tooltip("How long (seconds) the toast stays fully visible before sliding out.")]
    [SerializeField] private float displayDuration = 3f;

    [Tooltip("Horizontal distance (in canvas pixels) from which the toast slides in.")]
    [SerializeField] private float offscreenOffsetX = 500f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private readonly Queue<Achievement> _queue = new Queue<Achievement>();
    private bool _isShowing;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Start hidden
        if (toastPanel != null)
        {
            Vector2 pos = toastPanel.anchoredPosition;
            pos.x = offscreenOffsetX;
            toastPanel.anchoredPosition = pos;
        }
    }

    private void OnEnable()
    {
        if (AchievementSystem.Instance != null)
            AchievementSystem.Instance.OnAchievementUnlocked += EnqueueToast;
    }

    private void OnDisable()
    {
        if (AchievementSystem.Instance != null)
            AchievementSystem.Instance.OnAchievementUnlocked -= EnqueueToast;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds <paramref name="achievement"/> to the toast queue.
    /// If no toast is currently visible, shows it immediately.
    /// </summary>
    /// <param name="achievement">The newly unlocked achievement to display.</param>
    public void EnqueueToast(Achievement achievement)
    {
        _queue.Enqueue(achievement);
        if (!_isShowing)
            StartCoroutine(ShowNextToast());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Processes the toast queue one entry at a time:
    /// slide in → hold for <see cref="displayDuration"/> → slide out → repeat.
    /// </summary>
    private IEnumerator ShowNextToast()
    {
        while (_queue.Count > 0)
        {
            _isShowing = true;
            Achievement ach = _queue.Dequeue();

            // Populate text fields
            if (achievementNameText != null) achievementNameText.text = ach.name;
            if (achievementDescText  != null) achievementDescText.text  = ach.description;

            // Slide in from the right
            yield return StartCoroutine(SlidePanel(offscreenOffsetX, 0f));

            // Hold
            yield return new WaitForSecondsRealtime(displayDuration);

            // Slide out to the right
            yield return StartCoroutine(SlidePanel(0f, offscreenOffsetX));
        }

        _isShowing = false;
    }

    /// <summary>
    /// Lerps <see cref="toastPanel"/>.anchoredPosition.x from
    /// <paramref name="fromX"/> to <paramref name="toX"/> over <see cref="slideDuration"/> seconds.
    /// Uses unscaled time so it works while the game is paused.
    /// </summary>
    private IEnumerator SlidePanel(float fromX, float toX)
    {
        if (toastPanel == null) yield break;

        float elapsed = 0f;
        Vector2 startPos = toastPanel.anchoredPosition;
        startPos.x = fromX;

        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            Vector2 pos = toastPanel.anchoredPosition;
            pos.x = Mathf.Lerp(fromX, toX, t);
            toastPanel.anchoredPosition = pos;
            yield return null;
        }

        // Snap to final position
        Vector2 finalPos = toastPanel.anchoredPosition;
        finalPos.x = toX;
        toastPanel.anchoredPosition = finalPos;
    }
}
