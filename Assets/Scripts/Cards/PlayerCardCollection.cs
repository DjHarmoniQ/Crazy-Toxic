using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tracks every <see cref="AbilityCard"/> the player has collected during a run.
/// Passive cards are applied immediately upon pickup via <see cref="OnCardAdded"/>.
/// Also manages the player's gold balance used for rerolls.
///
/// Attach to: The Player GameObject.
/// </summary>
public class PlayerCardCollection : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Maximum number of cards the player can hold in a single run.</summary>
    public const int MaxCards = 20;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Starting Resources")]
    [Tooltip("Gold amount the player begins with.")]
    [SerializeField] private int _startingGold = 0;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Read-only list of all ability cards currently active for this player.</summary>
    public List<AbilityCard> ActiveCards { get; private set; } = new List<AbilityCard>();

    /// <summary>Current gold balance. Used by <see cref="CardPickerUI"/> for rerolls.</summary>
    public int Gold { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever a card is successfully added to the collection.
    /// Parameter: the newly added card.
    /// </summary>
    public event System.Action<AbilityCard> OnCardAdded;

    /// <summary>
    /// Fired whenever the gold balance changes.
    /// Parameter: the new gold amount.
    /// </summary>
    public event System.Action<int> OnGoldChanged;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        Gold = _startingGold;
        OnGoldChanged?.Invoke(Gold);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Cards
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds <paramref name="card"/> to the player's active card list.
    /// If the card is passive, <see cref="OnCardAdded"/> signals listeners (e.g.
    /// <see cref="CardEffectApplier"/>) to re-apply stat modifiers immediately.
    /// Shows a warning if the deck is at capacity.
    /// </summary>
    /// <param name="card">The <see cref="AbilityCard"/> to add.</param>
    public void AddCard(AbilityCard card)
    {
        if (card == null)
        {
            Debug.LogWarning("[PlayerCardCollection] Attempted to add a null card.");
            return;
        }

        if (ActiveCards.Count >= MaxCards)
        {
            Debug.LogWarning($"[PlayerCardCollection] Deck full ({MaxCards} cards). " +
                             $"Cannot add '{card.cardName}'.");
            return;
        }

        ActiveCards.Add(card);
        Debug.Log($"[PlayerCardCollection] Added card '{card.cardName}' " +
                  $"(passive={card.isPassive}). Total: {ActiveCards.Count}/{MaxCards}");

        OnCardAdded?.Invoke(card);
    }

    /// <summary>
    /// Returns <c>true</c> if any card in <see cref="ActiveCards"/> has the
    /// specified <paramref name="effect"/>.
    /// </summary>
    /// <param name="effect">The <see cref="CardEffect"/> to search for.</param>
    public bool HasEffect(CardEffect effect)
    {
        foreach (AbilityCard card in ActiveCards)
        {
            if (card != null && card.effect == effect)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Returns the sum of <see cref="AbilityCard.effectValue"/> across all cards
    /// that match <paramref name="effect"/>.  Supports stacking multiple copies.
    /// </summary>
    /// <param name="effect">The <see cref="CardEffect"/> to accumulate.</param>
    /// <returns>Total stacked value, or 0 if no matching card is found.</returns>
    public float GetEffectValue(CardEffect effect)
    {
        float total = 0f;
        foreach (AbilityCard card in ActiveCards)
        {
            if (card != null && card.effect == effect)
                total += card.effectValue;
        }
        return total;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Gold
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Adds <paramref name="amount"/> gold to the player's balance and fires
    /// <see cref="OnGoldChanged"/>.
    /// </summary>
    /// <param name="amount">Amount of gold to add (must be positive).</param>
    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        Gold += amount;
        OnGoldChanged?.Invoke(Gold);
        Debug.Log($"[PlayerCardCollection] +{amount} gold. Total: {Gold}");
    }

    /// <summary>
    /// Attempts to spend <paramref name="amount"/> gold.
    /// </summary>
    /// <param name="amount">Gold cost (must be positive).</param>
    /// <returns>
    /// <c>true</c> if the player had enough gold and the amount was deducted;
    /// <c>false</c> otherwise.
    /// </returns>
    public bool TrySpendGold(int amount)
    {
        if (amount <= 0) return true;
        if (Gold < amount)
        {
            Debug.Log($"[PlayerCardCollection] Not enough gold. Have {Gold}, need {amount}.");
            return false;
        }

        Gold -= amount;
        OnGoldChanged?.Invoke(Gold);
        Debug.Log($"[PlayerCardCollection] Spent {amount} gold. Remaining: {Gold}");
        return true;
    }
}
