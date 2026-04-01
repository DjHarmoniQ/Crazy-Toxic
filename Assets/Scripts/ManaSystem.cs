using UnityEngine;

/// <summary>
/// Manages the player's mana pool: passive regeneration with a post-spend delay,
/// spending, adding, and broadcasting changes via <see cref="OnManaChanged"/>.
///
/// Attach to: The Player GameObject (alongside <see cref="CharacterStatApplier"/>).
/// </summary>
public class ManaSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Mana Settings")]
    [Tooltip("Maximum mana the player can hold.")]
    [SerializeField] private float maxMana = 100f;

    [Tooltip("Mana points regenerated per second during passive regen.")]
    [SerializeField] private float manaRegenRate = 5f;

    [Tooltip("Seconds after spending mana before passive regen resumes.")]
    [SerializeField] private float manaRegenDelay = 2f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Current mana amount (0 … <see cref="MaxMana"/>).</summary>
    public float CurrentMana { get; private set; }

    /// <summary>Maximum mana capacity. Can be changed at runtime via <see cref="SetMaxMana"/>.</summary>
    public float MaxMana { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever mana changes.
    /// Parameters: <c>(float currentMana, float maxMana)</c>.
    /// </summary>
    public event System.Action<float, float> OnManaChanged;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Counts down to zero; regen is paused while this is greater than zero.</summary>
    private float _regenDelayTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        MaxMana = maxMana;
        CurrentMana = MaxMana;
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    private void Update()
    {
        // Count down the post-spend delay timer
        if (_regenDelayTimer > 0f)
        {
            _regenDelayTimer -= Time.deltaTime;
            return;
        }

        // Passive regen: tick only when below max
        if (CurrentMana < MaxMana)
        {
            CurrentMana = Mathf.Min(CurrentMana + manaRegenRate * Time.deltaTime, MaxMana);
            OnManaChanged?.Invoke(CurrentMana, MaxMana);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to spend <paramref name="amount"/> mana.
    /// </summary>
    /// <param name="amount">Mana to deduct (must be positive).</param>
    /// <returns>
    /// <c>true</c> if the mana was successfully spent; <c>false</c> if there was
    /// not enough mana.
    /// </returns>
    public bool TrySpendMana(float amount)
    {
        if (CurrentMana < amount)
        {
            Debug.Log($"[ManaSystem] Not enough mana. Have {CurrentMana:F1}, need {amount:F1}.");
            return false;
        }

        CurrentMana -= amount;
        _regenDelayTimer = manaRegenDelay;
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        Debug.Log($"[ManaSystem] Spent {amount:F1} mana. Remaining: {CurrentMana:F1}/{MaxMana:F1}");
        return true;
    }

    /// <summary>
    /// Adds <paramref name="amount"/> mana, clamping to <see cref="MaxMana"/>.
    /// </summary>
    /// <param name="amount">Mana to add (positive values only).</param>
    public void AddMana(float amount)
    {
        CurrentMana = Mathf.Min(CurrentMana + amount, MaxMana);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        Debug.Log($"[ManaSystem] Added {amount:F1} mana. Current: {CurrentMana:F1}/{MaxMana:F1}");
    }

    /// <summary>
    /// Overrides the maximum mana capacity (used by <see cref="CharacterStatApplier"/>).
    /// Current mana is clamped to the new maximum.
    /// </summary>
    /// <param name="value">New maximum mana value (must be positive).</param>
    public void SetMaxMana(float value)
    {
        MaxMana = Mathf.Max(0f, value);
        CurrentMana = Mathf.Min(CurrentMana, MaxMana);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
        Debug.Log($"[ManaSystem] MaxMana set to {MaxMana:F1}.");
    }
}
