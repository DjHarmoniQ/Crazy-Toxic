using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Shows an extended 5-card picker at the start of <see cref="ModeDraft"/>.
/// Listens for <see cref="ModeDraft.OnDraftPickerRequested"/> and presents
/// <see cref="ModeDraft.DraftHandSize"/> card slots for the player to confirm.
/// </summary>
public class DraftPickerUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Root panel shown during the draft pick phase.")]
    [SerializeField] private GameObject _pickerPanel;

    [Header("Card Slots")]
    [Tooltip("Array of 5 UI card-slot root GameObjects shown during draft.")]
    [SerializeField] private GameObject[] _cardSlots;

    [Header("Confirm")]
    [Tooltip("Button the player presses to lock in their 5-card draft deck.")]
    [SerializeField] private Button _confirmButton;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (_pickerPanel != null)
            _pickerPanel.SetActive(false);

        if (GameModeManager.Instance?.CurrentMode is ModeDraft draft)
            draft.OnDraftPickerRequested += ShowPicker;

        if (_confirmButton != null)
            _confirmButton.onClick.AddListener(OnConfirmClicked);
    }

    private void OnDestroy()
    {
        if (GameModeManager.Instance?.CurrentMode is ModeDraft draft)
            draft.OnDraftPickerRequested -= ShowPicker;

        if (_confirmButton != null)
            _confirmButton.onClick.RemoveListener(OnConfirmClicked);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Opens the draft picker panel and activates all card slots.</summary>
    private void ShowPicker()
    {
        if (_pickerPanel != null)
            _pickerPanel.SetActive(true);

        foreach (GameObject slot in _cardSlots)
            if (slot != null) slot.SetActive(true);

        Debug.Log("[DraftPickerUI] Draft picker opened — pick your 5 cards.");
    }

    /// <summary>Closes the panel and notifies <see cref="ModeDraft"/> that the draft is confirmed.</summary>
    private void OnConfirmClicked()
    {
        if (_pickerPanel != null)
            _pickerPanel.SetActive(false);

        if (GameModeManager.Instance?.CurrentMode is ModeDraft draft)
            draft.ConfirmDraft();

        Debug.Log("[DraftPickerUI] Draft confirmed.");
    }
}
