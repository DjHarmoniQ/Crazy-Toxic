using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Dedicated boss HP bar UI that subscribes to <see cref="BossBase.OnBossHealthChanged"/>
/// and <see cref="BossBase.OnPhaseChanged"/> events.
///
/// The bar:
/// • Slides in from the bottom of the screen when a boss becomes active.
/// • Smoothly lerps the HP slider value to avoid jarring jumps.
/// • Flashes red when the boss transitions to a new phase.
/// • Hides when no boss is present.
///
/// Setup: Add as a component to your Canvas boss-HP-bar panel.
/// Assign the Slider, boss-name Text, and phase Text fields in the Inspector.
/// Call <see cref="SetBoss"/> from wherever you spawn the boss.
/// </summary>
public class BossHPBarUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("The UI Slider that represents the boss's remaining HP.")]
    [SerializeField] private Slider hpSlider;

    [Tooltip("TextMeshPro label displaying the boss's name.")]
    [SerializeField] private TextMeshProUGUI bossNameText;

    [Tooltip("TextMeshPro label displaying the current phase (e.g. 'PHASE 3 / 5').")]
    [SerializeField] private TextMeshProUGUI phaseText;

    [Header("Lerp Settings")]
    [Tooltip("Speed at which the slider value lerps toward the target HP percentage.")]
    [SerializeField] private float lerpSpeed = 5f;

    [Header("Phase Transition Flash")]
    [Tooltip("Color the HP slider briefly flashes when the boss enters a new phase.")]
    [SerializeField] private Color phaseFlashColor = Color.red;

    [Tooltip("Duration of the flash effect on phase transition.")]
    [SerializeField] private float flashDuration = 0.4f;

    [Header("Slide Animation")]
    [Tooltip("How far below the screen (in local Y units) the panel starts when sliding in.")]
    [SerializeField] private float slideOffscreenY = -200f;

    [Tooltip("Target local Y position when the panel is fully on screen.")]
    [SerializeField] private float slideOnscreenY = 0f;

    [Tooltip("Speed at which the panel slides in/out.")]
    [SerializeField] private float slideSpeed = 600f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private BossBase _currentBoss;
    private float _targetHpFraction = 1f;
    private Image _sliderFillImage;
    private Color _originalFillColor;
    private RectTransform _rectTransform;
    private bool _isVisible;
    private int _totalPhases = 5;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (hpSlider != null)
        {
            _sliderFillImage = hpSlider.fillRect?.GetComponent<Image>();
            if (hpSlider.fillRect != null && _sliderFillImage == null)
                Debug.LogWarning("[BossHPBarUI] hpSlider.fillRect has no Image component — phase flash will be skipped.");
            if (_sliderFillImage != null)
                _originalFillColor = _sliderFillImage.color;
        }

        // Start hidden below the screen
        HideImmediate();
    }

    private void Update()
    {
        if (!_isVisible) return;

        // Smoothly lerp the slider toward the target fraction
        if (hpSlider != null)
            hpSlider.value = Mathf.Lerp(hpSlider.value, _targetHpFraction, Time.deltaTime * lerpSpeed);
    }

    private void OnDestroy()
    {
        UnsubscribeFromBoss();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a boss with this UI, subscribes to its health and phase events,
    /// and slides the HP bar onto the screen.
    /// </summary>
    /// <param name="boss">The boss that just spawned.</param>
    /// <param name="bossName">Display name shown in the boss name label.</param>
    /// <param name="totalPhases">Total number of phases for the phase counter.</param>
    public void SetBoss(BossBase boss, string bossName, int totalPhases = 5)
    {
        UnsubscribeFromBoss();

        _currentBoss = boss;
        _totalPhases = totalPhases;

        if (_currentBoss != null)
        {
            _currentBoss.OnBossHealthChanged += OnBossHealthChanged;
            _currentBoss.OnPhaseChanged      += OnPhaseChanged;
        }

        if (bossNameText != null)
            bossNameText.text = bossName;

        UpdatePhaseText(1);

        if (hpSlider != null)
            hpSlider.value = 1f;

        _targetHpFraction = 1f;

        Show();
    }

    /// <summary>Hides the HP bar (called when no boss is active).</summary>
    public void Hide()
    {
        StartCoroutine(SlideCoroutine(slideOffscreenY));
        _isVisible = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Event Handlers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Updates the target HP fraction whenever the boss's health changes.</summary>
    private void OnBossHealthChanged(float current, float max)
    {
        _targetHpFraction = max > 0f ? current / max : 0f;

        if (current <= 0f)
        {
            // Boss is dead — hide bar after a short delay
            StartCoroutine(HideAfterDelay(2f));
        }
    }

    /// <summary>Updates the phase label and flashes the bar when the boss enters a new phase.</summary>
    private void OnPhaseChanged(int newPhase)
    {
        UpdatePhaseText(newPhase);
        StartCoroutine(FlashFillColor());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Slides the HP bar panel onto the screen.</summary>
    private void Show()
    {
        _isVisible = true;
        gameObject.SetActive(true);
        StartCoroutine(SlideCoroutine(slideOnscreenY));
    }

    /// <summary>Moves the panel off-screen immediately (no animation).</summary>
    private void HideImmediate()
    {
        _isVisible = false;
        if (_rectTransform != null)
        {
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.y = slideOffscreenY;
            _rectTransform.anchoredPosition = pos;
        }
        gameObject.SetActive(false);
    }

    /// <summary>Waits <paramref name="delay"/> seconds then hides the bar.</summary>
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide();
        UnsubscribeFromBoss();
    }

    /// <summary>Animates the panel sliding to <paramref name="targetY"/>.</summary>
    private IEnumerator SlideCoroutine(float targetY)
    {
        if (_rectTransform == null) yield break;

        while (!Mathf.Approximately(_rectTransform.anchoredPosition.y, targetY))
        {
            Vector2 pos = _rectTransform.anchoredPosition;
            pos.y = Mathf.MoveTowards(pos.y, targetY, slideSpeed * Time.deltaTime);
            _rectTransform.anchoredPosition = pos;
            yield return null;
        }

        if (Mathf.Approximately(targetY, slideOffscreenY))
            gameObject.SetActive(false);
    }

    /// <summary>Briefly flashes the HP bar fill to <see cref="phaseFlashColor"/> then restores it.</summary>
    private IEnumerator FlashFillColor()
    {
        if (_sliderFillImage == null) yield break;

        _sliderFillImage.color = phaseFlashColor;
        yield return new WaitForSeconds(flashDuration);
        _sliderFillImage.color = _originalFillColor;
    }

    /// <summary>Updates the phase text label to "PHASE X / Y".</summary>
    private void UpdatePhaseText(int currentPhase)
    {
        if (phaseText != null)
            phaseText.text = $"PHASE {currentPhase} / {_totalPhases}";
    }

    /// <summary>Safely unsubscribes from the registered boss's events.</summary>
    private void UnsubscribeFromBoss()
    {
        if (_currentBoss != null)
        {
            _currentBoss.OnBossHealthChanged -= OnBossHealthChanged;
            _currentBoss.OnPhaseChanged      -= OnPhaseChanged;
            _currentBoss = null;
        }
    }
}
