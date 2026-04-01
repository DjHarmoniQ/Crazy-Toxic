using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the wave-end ability card picker panel.
///
/// When a wave clears (<see cref="WaveManager.OnWaveChanged"/> fires) the panel
/// becomes visible, <see cref="Time.timeScale"/> is set to 0 to pause the game,
/// and three (or four, if the player owns a draw-buff) random cards are offered.
/// The player selects one, or may reroll (costs 50 gold or is free if the player
/// holds <see cref="CardEffect.RerollOnWaveStart"/>) or skip (only when the player
/// holds <see cref="CardEffect.WaveSkip"/>).
///
/// After a card is selected (or the panel is skipped) <see cref="Time.timeScale"/>
/// is restored to 1 and the game continues.
///
/// Attach to: A persistent UI manager GameObject in the game scene.
/// </summary>
public class CardPickerUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Gold cost for a manual reroll (waived when holding <see cref="CardEffect.RerollOnWaveStart"/>).</summary>
    public const int RerollGoldCost = 50;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed References
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Panel")]
    [Tooltip("Root GameObject of the card picker panel. " +
             "Toggle active/inactive to show or hide the UI.")]
    [SerializeField] private GameObject _cardPickerPanel;

    [Header("Card Slots")]
    [Tooltip("Array of CardSlotUI components — 3 slots normally, 4 when the player " +
             "has a card-draw buff (CardEffect.CardDrawOnKill).")]
    [SerializeField] private CardSlotUI[] _cardSlots;

    [Header("Buttons")]
    [Tooltip("Button that rerolls the offered cards. Costs 50 gold unless the " +
             "player holds CardEffect.RerollOnWaveStart.")]
    [SerializeField] private Button _rerollButton;

    [Tooltip("TextMeshPro label on the Reroll button showing cost or 'FREE'.")]
    [SerializeField] private TextMeshProUGUI _rerollCostText;

    [Tooltip("Button that skips the card pick entirely. " +
             "Only shown when the player holds CardEffect.WaveSkip.")]
    [SerializeField] private Button _skipButton;

    [Header("Dependencies")]
    [Tooltip("Reference to the CardDatabase ScriptableObject asset. " +
             "Drag the CardDatabase asset from the Project window here.")]
    [SerializeField] private CardDatabase _cardDatabase;

    [Tooltip("Reference to the PlayerCardCollection on the player. " +
             "Resolved at runtime via FindObjectOfType if left empty.")]
    [SerializeField] private PlayerCardCollection _playerCollection;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Tracks whether the picker is currently showing (prevents double-opens).</summary>
    private bool _isOpen;

    /// <summary>Wave number that triggered the current card offer (used to ignore re-fires).</summary>
    private int _lastOfferedWave = -1;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Auto-resolve PlayerCardCollection if not set in the Inspector
        if (_playerCollection == null)
            _playerCollection = FindObjectOfType<PlayerCardCollection>();

        if (_playerCollection == null)
            Debug.LogWarning("[CardPickerUI] No PlayerCardCollection found in the scene.");

        // Use the singleton CardDatabase if not assigned
        if (_cardDatabase == null)
            _cardDatabase = CardDatabase.Instance;

        if (_cardDatabase == null)
            Debug.LogWarning("[CardPickerUI] No CardDatabase found. Assign the asset in the Inspector.");

        // Hide the panel at game start
        if (_cardPickerPanel != null)
            _cardPickerPanel.SetActive(false);

        // Subscribe to wave changes — the picker opens AFTER a wave clears,
        // which happens when WaveManager fires OnWaveChanged with the NEW wave number.
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged += OnWaveChanged;
        else
            Debug.LogWarning("[CardPickerUI] WaveManager.Instance is null at Start — " +
                             "subscribing deferred is not supported. Ensure WaveManager " +
                             "initialises before CardPickerUI.");
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged -= OnWaveChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Event Handler
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Subscribed to <see cref="WaveManager.OnWaveChanged"/>.
    /// Opens the card picker whenever a new wave begins (i.e. the previous one cleared),
    /// skipping wave 1 (no reward before the first wave has even been fought).
    /// </summary>
    /// <param name="newWave">The wave number that just started.</param>
    private void OnWaveChanged(int newWave)
    {
        // Do not show the picker for the very first wave or if already open
        if (newWave <= 1 || _isOpen || newWave == _lastOfferedWave) return;
        _lastOfferedWave = newWave;
        OpenPicker();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by a <see cref="CardSlotUI"/> when the player clicks a card.
    /// Adds the card to the player's collection and closes the panel.
    /// </summary>
    /// <param name="card">The selected <see cref="AbilityCard"/>.</param>
    public void SelectCard(AbilityCard card)
    {
        if (card == null) return;

        if (_playerCollection != null)
            _playerCollection.AddCard(card);

        Debug.Log($"[CardPickerUI] Player selected '{card.cardName}'.");
        ClosePicker();
    }

    /// <summary>
    /// Rerolls the current card offer.  Costs <see cref="RerollGoldCost"/> gold
    /// unless the player holds <see cref="CardEffect.RerollOnWaveStart"/>.
    /// Wired to the Reroll button via the Inspector.
    /// </summary>
    public void OnRerollClicked()
    {
        if (_playerCollection == null) return;

        bool isFree = _playerCollection.HasEffect(CardEffect.RerollOnWaveStart);

        if (!isFree)
        {
            bool spent = _playerCollection.TrySpendGold(RerollGoldCost);
            if (!spent)
            {
                Debug.Log("[CardPickerUI] Not enough gold to reroll.");
                return;
            }
        }

        Debug.Log($"[CardPickerUI] Reroll! (free={isFree})");
        PopulateSlots();
        RefreshRerollButtonLabel();
    }

    /// <summary>
    /// Skips the card offer entirely without picking a card.
    /// Only available when the player holds <see cref="CardEffect.WaveSkip"/>.
    /// Wired to the Skip button via the Inspector.
    /// </summary>
    public void OnSkipClicked()
    {
        Debug.Log("[CardPickerUI] Player skipped the card pick.");
        ClosePicker();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Pauses the game and shows the card picker panel.</summary>
    private void OpenPicker()
    {
        _isOpen = true;
        Time.timeScale = 0f;

        if (_cardPickerPanel != null)
            _cardPickerPanel.SetActive(true);

        PopulateSlots();
        RefreshRerollButtonLabel();
        RefreshSkipButton();

        Debug.Log("[CardPickerUI] Card picker opened.");
    }

    /// <summary>Hides the card picker panel and resumes the game.</summary>
    private void ClosePicker()
    {
        _isOpen = false;
        Time.timeScale = 1f;

        if (_cardPickerPanel != null)
            _cardPickerPanel.SetActive(false);

        Debug.Log("[CardPickerUI] Card picker closed.");
    }

    /// <summary>
    /// Determines how many cards to offer (3 normally, 4 with
    /// <see cref="CardEffect.CardDrawOnKill"/>), fetches them from the database,
    /// and populates the slot UIs.
    /// </summary>
    private void PopulateSlots()
    {
        if (_cardSlots == null || _cardSlots.Length == 0)
        {
            Debug.LogWarning("[CardPickerUI] No card slots assigned.");
            return;
        }

        // 4 cards if player holds a draw-buff, otherwise 3
        bool extraDraw = _playerCollection != null &&
                         _playerCollection.HasEffect(CardEffect.CardDrawOnKill);
        int offerCount = extraDraw ? 4 : 3;
        offerCount = Mathf.Min(offerCount, _cardSlots.Length);

        // Fetch random cards
        AbilityCard[] offered = _cardDatabase != null
            ? _cardDatabase.GetRandomCards(offerCount)
            : System.Array.Empty<AbilityCard>();

        // Populate active slots
        for (int i = 0; i < _cardSlots.Length; i++)
        {
            if (_cardSlots[i] == null) continue;

            if (i < offered.Length)
                _cardSlots[i].Populate(offered[i], this);
            else
                _cardSlots[i].gameObject.SetActive(false);
        }
    }

    /// <summary>Updates the Reroll button label to show the gold cost or "FREE".</summary>
    private void RefreshRerollButtonLabel()
    {
        if (_rerollCostText == null) return;

        bool isFree = _playerCollection != null &&
                      _playerCollection.HasEffect(CardEffect.RerollOnWaveStart);

        _rerollCostText.text = isFree
            ? "Reroll (FREE)"
            : $"Reroll ({RerollGoldCost}g)";
    }

    /// <summary>Shows or hides the Skip button based on whether the player has <see cref="CardEffect.WaveSkip"/>.</summary>
    private void RefreshSkipButton()
    {
        if (_skipButton == null) return;

        bool hasSkip = _playerCollection != null &&
                       _playerCollection.HasEffect(CardEffect.WaveSkip);

        _skipButton.gameObject.SetActive(hasSkip);
    }
}
