using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Renders a single ability card in the wave-end card picker panel.
///
/// Wire up the serialised fields to the matching child UI objects in the prefab,
/// then call <see cref="Populate"/> to fill the slot with an <see cref="AbilityCard"/>
/// asset before the panel becomes visible.
///
/// Hover animation: scales the card to 1.05× using <see cref="Mathf.Lerp"/>.
/// Click: calls <see cref="CardPickerUI.SelectCard"/> on the parent UI.
///
/// Attach to: Each card-slot root GameObject inside the card picker panel prefab.
/// </summary>
public class CardSlotUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Serialised References
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Card Visuals")]
    [Tooltip("Image component that displays the card illustration.")]
    [SerializeField] private Image _cardArtwork;

    [Tooltip("Image component used as the coloured rarity border.")]
    [SerializeField] private Image _rarityBorder;

    [Tooltip("TextMeshPro label for the card's display name.")]
    [SerializeField] private TextMeshProUGUI _cardNameText;

    [Tooltip("TextMeshPro label for the card's description text.")]
    [SerializeField] private TextMeshProUGUI _descriptionText;

    [Tooltip("TextMeshPro label that shows the rarity name (e.g. 'RARE').")]
    [SerializeField] private TextMeshProUGUI _rarityText;

    [Header("Animation")]
    [Tooltip("Scale factor applied to the card when the cursor hovers over it.")]
    [SerializeField] private float _hoverScale = 1.05f;

    [Tooltip("Speed at which the hover scale lerp runs (higher = snappier).")]
    [SerializeField] private float _hoverLerpSpeed = 10f;

    [Header("Audio")]
    [Tooltip("Optional AudioSource used to play the click SFX. " +
             "If null, no sound is played on click.")]
    [SerializeField] private AudioSource _audioSource;

    [Tooltip("AudioClip played when the player clicks this card slot.")]
    [SerializeField] private AudioClip _clickSFX;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The card currently displayed in this slot.</summary>
    private AbilityCard _card;

    /// <summary>The <see cref="CardPickerUI"/> that owns this slot.</summary>
    private CardPickerUI _owner;

    /// <summary>Target scale driven by hover state.</summary>
    private float _targetScale = 1f;

    /// <summary>Current lerped scale applied to the RectTransform.</summary>
    private float _currentScale = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Smoothly lerp toward the target scale each frame
        _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.unscaledDeltaTime * _hoverLerpSpeed);
        transform.localScale = Vector3.one * _currentScale;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fills this slot with the data from <paramref name="card"/> and stores a
    /// reference to <paramref name="owner"/> so that <see cref="OnClick"/> can
    /// forward the selection.
    /// </summary>
    /// <param name="card">The ability card to display.</param>
    /// <param name="owner">The parent <see cref="CardPickerUI"/>.</param>
    public void Populate(AbilityCard card, CardPickerUI owner)
    {
        _card  = card;
        _owner = owner;

        if (card == null)
        {
            gameObject.SetActive(false);
            return;
        }

        gameObject.SetActive(true);

        // Artwork
        if (_cardArtwork != null)
        {
            _cardArtwork.sprite  = card.artwork;
            _cardArtwork.enabled = card.artwork != null;
        }

        // Rarity border colour
        Color rarityCol = AbilityCard.GetRarityColor(card.rarity);
        if (_rarityBorder != null)
            _rarityBorder.color = rarityCol;

        // Text fields
        if (_cardNameText    != null) _cardNameText.text    = card.cardName;
        if (_descriptionText != null) _descriptionText.text = card.description;
        if (_rarityText      != null)
        {
            _rarityText.text  = card.rarity.ToString().ToUpperInvariant();
            _rarityText.color = rarityCol;
        }
    }

    /// <summary>
    /// Called by a <c>Button</c> component (or <c>EventTrigger</c>) when the
    /// player clicks this card slot.
    /// </summary>
    public void OnClick()
    {
        if (_card == null || _owner == null) return;

        // Play click SFX
        if (_audioSource != null && _clickSFX != null)
            _audioSource.PlayOneShot(_clickSFX);

        _owner.SelectCard(_card);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Hover Callbacks (wire to EventTrigger PointerEnter / PointerExit)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Called when the pointer enters this card slot (PointerEnter).</summary>
    public void OnHoverEnter()
    {
        _targetScale = _hoverScale;
    }

    /// <summary>Called when the pointer leaves this card slot (PointerExit).</summary>
    public void OnHoverExit()
    {
        _targetScale = 1f;
    }
}
