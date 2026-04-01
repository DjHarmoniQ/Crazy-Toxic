using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the mode-select screen.
///
/// Populates 6 <see cref="ModeSlotUI"/> slots from <see cref="GameModeManager.Instance"/>
/// and loads the game scene when the player confirms their selection.
/// </summary>
public class ModeSelectUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Slots")]
    [Tooltip("The six ModeSlotUI components displayed in the mode select grid. " +
             "Order must match GameModeManager.availableModes.")]
    [SerializeField] private ModeSlotUI[] _modeSlots;

    [Header("Navigation")]
    [Tooltip("The START button that confirms the selected mode and loads the game scene.")]
    [SerializeField] private Button _startButton;

    [Tooltip("Name of the Unity scene to load when the player presses START.")]
    [SerializeField] private string _gameSceneName = "GameScene";

    // ─────────────────────────────────────────────────────────────────────────
    //  State
    // ─────────────────────────────────────────────────────────────────────────

    private int _selectedIndex = 0;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        PopulateSlots();
        SelectSlot(0);

        if (_startButton != null)
            _startButton.onClick.AddListener(OnStartPressed);
    }

    private void OnDestroy()
    {
        if (_startButton != null)
            _startButton.onClick.RemoveListener(OnStartPressed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Reads available modes from <see cref="GameModeManager"/> and fills each slot.
    /// </summary>
    private void PopulateSlots()
    {
        if (GameModeManager.Instance == null)
        {
            Debug.LogWarning("[ModeSelectUI] GameModeManager not found in scene.");
            return;
        }

        for (int i = 0; i < _modeSlots.Length; i++)
        {
            if (_modeSlots[i] == null) continue;

            int capturedIndex = i; // Capture for lambda closure.
            _modeSlots[i].OnSlotClicked += () => SelectSlot(capturedIndex);
        }
    }

    /// <summary>
    /// Highlights the slot at <paramref name="index"/> and deselects all others.
    /// </summary>
    private void SelectSlot(int index)
    {
        _selectedIndex = index;

        for (int i = 0; i < _modeSlots.Length; i++)
        {
            if (_modeSlots[i] != null)
                _modeSlots[i].SetSelected(i == index);
        }
    }

    /// <summary>
    /// Passes the selected index to <see cref="GameModeManager"/> and loads the game scene.
    /// </summary>
    private void OnStartPressed()
    {
        if (GameModeManager.Instance != null)
            GameModeManager.Instance.SetMode(_selectedIndex);

        SceneManager.LoadScene(_gameSceneName);
    }
}
