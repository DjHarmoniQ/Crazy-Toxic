using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the mana bar HUD element.
///
/// Finds the player's <see cref="ManaSystem"/> via the "Player" tag, subscribes to
/// <see cref="ManaSystem.OnManaChanged"/>, and smoothly lerps the <see cref="Slider"/>
/// fill so the bar animates instead of snapping.
///
/// Attach to: The mana-bar UI GameObject in the HUD Canvas.
/// </summary>
public class ManaBarUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("UI References")]
    [Tooltip("Slider component that represents the mana fill.")]
    [SerializeField] private Slider manaSlider;

    [Tooltip("TextMeshPro label that shows e.g. \"85 / 100\".")]
    [SerializeField] private TextMeshProUGUI manaText;

    [Header("Animation")]
    [Tooltip("Speed of the slider lerp (higher = snappier).")]
    [SerializeField] private float lerpSpeed = 5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Cached reference to the player's <see cref="ManaSystem"/>.</summary>
    private ManaSystem _manaSystem;

    /// <summary>Target normalised value (0–1) the slider is lerping toward.</summary>
    private float _targetValue;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[ManaBarUI] No GameObject with tag 'Player' found.");
            return;
        }

        _manaSystem = player.GetComponent<ManaSystem>();
        if (_manaSystem == null)
        {
            Debug.LogWarning("[ManaBarUI] ManaSystem component not found on Player.");
            return;
        }

        _manaSystem.OnManaChanged += HandleManaChanged;

        // Initialise slider immediately (no lerp on first frame)
        if (manaSlider != null)
        {
            _targetValue = _manaSystem.MaxMana > 0f
                ? _manaSystem.CurrentMana / _manaSystem.MaxMana
                : 0f;
            manaSlider.value = _targetValue;
        }

        UpdateText(_manaSystem.CurrentMana, _manaSystem.MaxMana);
    }

    private void OnDestroy()
    {
        if (_manaSystem != null)
            _manaSystem.OnManaChanged -= HandleManaChanged;
    }

    private void Update()
    {
        if (manaSlider == null) return;

        // Smoothly animate the slider toward the target value
        manaSlider.value = Mathf.Lerp(manaSlider.value, _targetValue, Time.deltaTime * lerpSpeed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Receives mana-change events and refreshes target values + text label.</summary>
    private void HandleManaChanged(float current, float max)
    {
        _targetValue = max > 0f ? current / max : 0f;
        UpdateText(current, max);
    }

    /// <summary>Updates the text label to show "<c>current / max</c>" with no decimal places.</summary>
    private void UpdateText(float current, float max)
    {
        if (manaText != null)
            manaText.text = $"{Mathf.FloorToInt(current)} / {Mathf.FloorToInt(max)}";
    }
}
