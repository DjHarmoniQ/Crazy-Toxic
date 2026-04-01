using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Represents a single mode card in the mode-select grid.
///
/// Shows the mode name, description, difficulty rating (1–5 stars), and best score.
/// Raises <see cref="OnSlotClicked"/> when the player taps/clicks the card.
/// </summary>
public class ModeSlotUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Text Labels")]
    [Tooltip("TextMeshPro label displaying the mode name.")]
    [SerializeField] private TextMeshProUGUI _nameLabel;

    [Tooltip("TextMeshPro label displaying the mode description.")]
    [SerializeField] private TextMeshProUGUI _descriptionLabel;

    [Tooltip("TextMeshPro label displaying the player's best score for this mode.")]
    [SerializeField] private TextMeshProUGUI _bestScoreLabel;

    [Header("Difficulty Stars")]
    [Tooltip("Array of Image components representing difficulty stars (1–5). " +
             "Stars up to the difficulty level will use the active colour.")]
    [SerializeField] private Image[] _difficultyStars;

    [Tooltip("Color used for filled (active) difficulty stars.")]
    [SerializeField] private Color _starActiveColor = Color.yellow;

    [Tooltip("Color used for empty (inactive) difficulty stars.")]
    [SerializeField] private Color _starInactiveColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    [Header("Selection Highlight")]
    [Tooltip("Background image tinted when this slot is selected.")]
    [SerializeField] private Image _backgroundImage;

    [Tooltip("Color applied to the background when the slot is selected.")]
    [SerializeField] private Color _selectedColor = new Color(0.2f, 0.6f, 1f, 0.5f);

    [Tooltip("Default (unselected) background color.")]
    [SerializeField] private Color _defaultColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    [Header("Interaction")]
    [Tooltip("Button component on this slot's root.")]
    [SerializeField] private Button _button;

    // ─────────────────────────────────────────────────────────────────────────
    //  Data
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Mode Data")]
    [Tooltip("Display name shown on the card.")]
    [SerializeField] private string _modeName;

    [Tooltip("Short description shown beneath the name.")]
    [SerializeField] private string _modeDescription;

    [Tooltip("Difficulty rating from 1 (easiest) to 5 (hardest).")]
    [SerializeField, Range(1, 5)] private int _difficultyRating = 1;

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Raised when the player clicks or taps this mode slot.</summary>
    public event System.Action OnSlotClicked;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        RefreshDisplay();

        if (_button != null)
            _button.onClick.AddListener(() => OnSlotClicked?.Invoke());
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveAllListeners();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Highlights or un-highlights this slot's background to indicate selection state.
    /// </summary>
    /// <param name="selected"><c>true</c> to highlight, <c>false</c> to restore default.</param>
    public void SetSelected(bool selected)
    {
        if (_backgroundImage != null)
            _backgroundImage.color = selected ? _selectedColor : _defaultColor;
    }

    /// <summary>
    /// Updates the best-score label. Call after loading the player's save data.
    /// </summary>
    /// <param name="score">Best score to display.</param>
    public void SetBestScore(int score)
    {
        if (_bestScoreLabel != null)
            _bestScoreLabel.text = $"Best: {score:N0}";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Populates all UI elements from the serialized data fields.</summary>
    private void RefreshDisplay()
    {
        if (_nameLabel != null)
            _nameLabel.text = _modeName;

        if (_descriptionLabel != null)
            _descriptionLabel.text = _modeDescription;

        // Difficulty stars.
        for (int i = 0; i < _difficultyStars.Length; i++)
        {
            if (_difficultyStars[i] != null)
                _difficultyStars[i].color = i < _difficultyRating ? _starActiveColor : _starInactiveColor;
        }

        // Default best score.
        if (_bestScoreLabel != null)
            _bestScoreLabel.text = "Best: 0";

        // Start unselected.
        SetSelected(false);
    }
}
