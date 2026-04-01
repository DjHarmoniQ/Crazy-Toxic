using UnityEngine;

/// <summary>
/// Reads the player's <see cref="PlayerCardCollection"/> and applies stacked card
/// bonuses to the player's base stats.
///
/// Call the relevant <c>Apply*</c> method whenever a stat is queried so that all
/// currently active card effects are factored in.  Because effects stack additively
/// via <see cref="PlayerCardCollection.GetEffectValue"/>, adding more copies of the
/// same card simply increases the total bonus.
///
/// Attach to: The Player GameObject (alongside <see cref="PlayerCardCollection"/>).
/// </summary>
public class CardEffectApplier : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Dependencies
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Dependencies")]
    [Tooltip("Reference to the PlayerCardCollection on the same GameObject. " +
             "Auto-resolved from the same GameObject if left empty.")]
    [SerializeField] private PlayerCardCollection _collection;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (_collection == null)
            _collection = GetComponent<PlayerCardCollection>();

        if (_collection == null)
            Debug.LogError("[CardEffectApplier] No PlayerCardCollection found. " +
                           "Attach one to the same GameObject.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API — Stat Queries
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns <paramref name="baseDamage"/> after all active offensive card modifiers
    /// have been applied.
    /// Handles: <see cref="CardEffect.IncreaseDamage"/>,
    ///          <see cref="CardEffect.CritChanceUp"/>,
    ///          <see cref="CardEffect.CritDamageUp"/>.
    /// </summary>
    /// <param name="baseDamage">The unmodified base damage value.</param>
    /// <returns>The modified damage value.</returns>
    public float ApplyOffensiveCards(float baseDamage)
    {
        if (_collection == null) return baseDamage;

        float damage = baseDamage;

        // Flat damage bonus (% increase stored as a decimal, e.g. 0.2 = +20 %)
        float damageBonus = _collection.GetEffectValue(CardEffect.IncreaseDamage);
        damage *= 1f + damageBonus;

        return damage;
    }

    /// <summary>
    /// Returns the critical-hit chance bonus from all active cards (0–1 range).
    /// Handles: <see cref="CardEffect.CritChanceUp"/>.
    /// </summary>
    /// <returns>Total stacked crit-chance bonus.</returns>
    public float GetCritChanceBonus()
    {
        if (_collection == null) return 0f;
        return _collection.GetEffectValue(CardEffect.CritChanceUp);
    }

    /// <summary>
    /// Returns the critical-hit damage multiplier bonus from all active cards.
    /// Handles: <see cref="CardEffect.CritDamageUp"/>.
    /// </summary>
    /// <returns>Total stacked crit-damage multiplier bonus.</returns>
    public float GetCritDamageBonus()
    {
        if (_collection == null) return 0f;
        return _collection.GetEffectValue(CardEffect.CritDamageUp);
    }

    /// <summary>
    /// Returns <paramref name="baseHealth"/> after all active defensive card modifiers
    /// have been applied.
    /// Handles: <see cref="CardEffect.MaxHealthUp"/>,
    ///          <see cref="CardEffect.DamageReduction"/>,
    ///          <see cref="CardEffect.ArmorUp"/>.
    /// </summary>
    /// <param name="baseHealth">The unmodified base max-health value.</param>
    /// <returns>The modified max-health value.</returns>
    public float ApplyDefenseCards(float baseHealth)
    {
        if (_collection == null) return baseHealth;

        float health = baseHealth;

        // Flat HP bonus (stored as additional HP points)
        float hpBonus = _collection.GetEffectValue(CardEffect.MaxHealthUp);
        health += hpBonus;

        return health;
    }

    /// <summary>
    /// Returns the incoming damage after applying any damage-reduction cards.
    /// Handles: <see cref="CardEffect.DamageReduction"/>,
    ///          <see cref="CardEffect.ArmorUp"/>.
    /// </summary>
    /// <param name="incomingDamage">The raw damage amount before mitigation.</param>
    /// <returns>Damage after card-based mitigation (clamped to ≥ 0).</returns>
    public float ApplyDamageReduction(float incomingDamage)
    {
        if (_collection == null) return incomingDamage;

        // Reduction stored as a decimal (0.1 = 10 % less damage)
        float reduction = _collection.GetEffectValue(CardEffect.DamageReduction)
                        + _collection.GetEffectValue(CardEffect.ArmorUp);
        reduction = Mathf.Clamp01(reduction);

        return Mathf.Max(0f, incomingDamage * (1f - reduction));
    }

    /// <summary>
    /// Returns <paramref name="baseSpeed"/> after all active mobility card modifiers
    /// have been applied.
    /// Handles: <see cref="CardEffect.MovementSpeedUp"/>.
    /// </summary>
    /// <param name="baseSpeed">The unmodified base movement speed.</param>
    /// <returns>The modified movement speed.</returns>
    public float ApplyMobilityCards(float baseSpeed)
    {
        if (_collection == null) return baseSpeed;

        float speed = baseSpeed;

        // Speed bonus stored as a decimal (0.1 = +10 %)
        float speedBonus = _collection.GetEffectValue(CardEffect.MovementSpeedUp);
        speed *= 1f + speedBonus;

        return speed;
    }

    /// <summary>
    /// Returns the number of extra dash charges granted by mobility cards.
    /// Handles: <see cref="CardEffect.ExtraDash"/>.
    /// </summary>
    /// <returns>Total number of extra dash charges.</returns>
    public int GetExtraDashCharges()
    {
        if (_collection == null) return 0;
        return Mathf.RoundToInt(_collection.GetEffectValue(CardEffect.ExtraDash));
    }

    /// <summary>
    /// Returns <paramref name="baseMana"/> after all active mana card modifiers
    /// have been applied.
    /// Handles: <see cref="CardEffect.MaxManaUp"/>.
    /// </summary>
    /// <param name="baseMana">The unmodified base max-mana value.</param>
    /// <returns>The modified max-mana value.</returns>
    public float ApplyManaCards(float baseMana)
    {
        if (_collection == null) return baseMana;

        float mana = baseMana;

        // Flat mana bonus (stored as additional mana points)
        float manaBonus = _collection.GetEffectValue(CardEffect.MaxManaUp);
        mana += manaBonus;

        return mana;
    }

    /// <summary>
    /// Returns the mana regeneration rate bonus from active cards.
    /// Handles: <see cref="CardEffect.ManaRegenUp"/>.
    /// </summary>
    /// <returns>Additional mana-regen points per second from cards.</returns>
    public float GetManaRegenBonus()
    {
        if (_collection == null) return 0f;
        return _collection.GetEffectValue(CardEffect.ManaRegenUp);
    }

    /// <summary>
    /// Returns the mana cost multiplier after all active cost-reduction cards.
    /// Handles: <see cref="CardEffect.ManaCostDown"/>.
    /// A return value of 0.8 means all costs are reduced by 20 %.
    /// </summary>
    /// <returns>Cost multiplier (1 = no reduction, clamped to [0.1, 1]).</returns>
    public float GetManaCostMultiplier()
    {
        if (_collection == null) return 1f;
        float reduction = Mathf.Clamp01(_collection.GetEffectValue(CardEffect.ManaCostDown));
        return Mathf.Max(0.1f, 1f - reduction);
    }

    /// <summary>
    /// Returns the cooldown multiplier after all active cooldown-reduction cards.
    /// Handles: <see cref="CardEffect.CooldownReduction"/>.
    /// A return value of 0.8 means all cooldowns are 20 % shorter.
    /// </summary>
    /// <returns>Cooldown multiplier (1 = no reduction, clamped to [0.1, 1]).</returns>
    public float GetCooldownMultiplier()
    {
        if (_collection == null) return 1f;
        float reduction = Mathf.Clamp01(_collection.GetEffectValue(CardEffect.CooldownReduction));
        return Mathf.Max(0.1f, 1f - reduction);
    }
}
