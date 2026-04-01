using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton ScriptableObject that owns the master list of every <see cref="AbilityCard"/>
/// in the game and provides weighted-random selection helpers used by
/// <see cref="CardPickerUI"/>.
///
/// Create the asset via <c>Assets ▶ Create ▶ Crazy-Toxic ▶ CardDatabase</c>,
/// then drag every <see cref="AbilityCard"/> asset into <see cref="allCards"/>.
/// The database registers itself as <see cref="Instance"/> automatically when
/// it is loaded.
/// </summary>
[CreateAssetMenu(menuName = "Crazy-Toxic/CardDatabase", fileName = "CardDatabase")]
public class CardDatabase : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the loaded <see cref="CardDatabase"/> asset.</summary>
    public static CardDatabase Instance { get; private set; }

    private void OnEnable()
    {
        Instance = this;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Data
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Card Pool")]
    [Tooltip("Every AbilityCard asset that can appear during a run. " +
             "Populate this in the Inspector by dragging AbilityCard assets here.")]
    [SerializeField] public AbilityCard[] allCards = System.Array.Empty<AbilityCard>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <paramref name="count"/> distinct cards chosen via weighted-random rarity
    /// selection.  If <paramref name="minimumRarity"/> is set, only cards of that rarity
    /// or higher (Epic &gt; Rare &gt; Uncommon &gt; Common) are eligible.
    /// </summary>
    /// <param name="count">Number of cards to return.</param>
    /// <param name="minimumRarity">
    /// Optional minimum rarity filter.  When <c>null</c> all rarities are eligible.
    /// </param>
    /// <returns>
    /// An array of up to <paramref name="count"/> distinct <see cref="AbilityCard"/>
    /// instances.  May contain fewer cards than requested if the pool is smaller.
    /// </returns>
    public AbilityCard[] GetRandomCards(int count, CardRarity? minimumRarity = null)
    {
        if (allCards == null || allCards.Length == 0)
        {
            Debug.LogWarning("[CardDatabase] allCards is empty — returning empty array.");
            return System.Array.Empty<AbilityCard>();
        }

        // Build weighted candidate list
        List<(AbilityCard card, float weight)> pool = BuildWeightedPool(minimumRarity);
        if (pool.Count == 0)
        {
            Debug.LogWarning("[CardDatabase] No cards match the requested rarity filter.");
            return System.Array.Empty<AbilityCard>();
        }

        int actual = Mathf.Min(count, pool.Count);
        AbilityCard[] result = new AbilityCard[actual];
        List<(AbilityCard card, float weight)> remaining = new List<(AbilityCard, float)>(pool);

        for (int i = 0; i < actual; i++)
        {
            AbilityCard picked = WeightedPick(remaining);
            result[i] = picked;
            // Remove the picked card so we don't select it again
            remaining.RemoveAll(entry => entry.card == picked);
        }

        return result;
    }

    /// <summary>
    /// Returns all cards that belong to the specified <paramref name="category"/>.
    /// </summary>
    /// <param name="category">The <see cref="CardCategory"/> to filter by.</param>
    /// <returns>Array of matching <see cref="AbilityCard"/> instances (may be empty).</returns>
    public AbilityCard[] GetCardsByCategory(CardCategory category)
    {
        if (allCards == null) return System.Array.Empty<AbilityCard>();

        List<AbilityCard> filtered = new List<AbilityCard>();
        foreach (AbilityCard card in allCards)
        {
            if (card != null && card.category == category)
                filtered.Add(card);
        }
        return filtered.ToArray();
    }

    /// <summary>
    /// Returns one random card whose rarity exactly matches <paramref name="rarity"/>.
    /// Returns <c>null</c> if no card with that rarity exists in the database.
    /// </summary>
    /// <param name="rarity">The exact <see cref="CardRarity"/> to look for.</param>
    /// <returns>A randomly chosen <see cref="AbilityCard"/>, or <c>null</c>.</returns>
    public AbilityCard GetRandomByRarity(CardRarity rarity)
    {
        if (allCards == null) return null;

        List<AbilityCard> matches = new List<AbilityCard>();
        foreach (AbilityCard card in allCards)
        {
            if (card != null && card.rarity == rarity)
                matches.Add(card);
        }

        if (matches.Count == 0)
        {
            Debug.LogWarning($"[CardDatabase] No cards found for rarity '{rarity}'.");
            return null;
        }

        return matches[Random.Range(0, matches.Count)];
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a weighted list of candidate cards.
    /// When <paramref name="minimumRarity"/> is provided, only cards whose rarity
    /// integer value is ≥ the minimum are included (Legendary=4 is highest).
    /// </summary>
    private List<(AbilityCard card, float weight)> BuildWeightedPool(CardRarity? minimumRarity)
    {
        List<(AbilityCard card, float weight)> pool = new List<(AbilityCard, float)>();

        foreach (AbilityCard card in allCards)
        {
            if (card == null) continue;
            if (minimumRarity.HasValue && (int)card.rarity < (int)minimumRarity.Value) continue;

            float weight = AbilityCard.GetRarityWeight(card.rarity);
            pool.Add((card, weight));
        }

        return pool;
    }

    /// <summary>
    /// Performs a single weighted random pick from <paramref name="pool"/>.
    /// </summary>
    private static AbilityCard WeightedPick(List<(AbilityCard card, float weight)> pool)
    {
        float total = 0f;
        foreach (var entry in pool)
            total += entry.weight;

        float roll = Random.value * total;
        float cumulative = 0f;

        foreach (var entry in pool)
        {
            cumulative += entry.weight;
            if (roll <= cumulative)
                return entry.card;
        }

        // Fallback: return last entry (handles floating-point edge case)
        return pool[pool.Count - 1].card;
    }
}
