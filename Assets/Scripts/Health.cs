using UnityEngine;

/// <summary>
/// Tracks a GameObject's hit points, exposes health-change events for the UI,
/// and implements <see cref="IDamageable"/> so bullets and hazards can deal damage
/// without needing to know the concrete type.
///
/// Attach to: Player, enemies, and any destructible prop.
/// </summary>
public class Health : MonoBehaviour, IDamageable
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Health")]
    [Tooltip("Starting maximum hit points. Can be overridden at runtime by CharacterStatApplier.")]
    [SerializeField] private float maxHealth = 100f;

    [Tooltip("When true, reaching zero HP calls GameManager.GameOver(). " +
             "Disable this for enemies so their death does not end the run.")]
    [SerializeField] private bool triggerGameOverOnDeath = true;

    /// <summary>
    /// When <c>false</c>, death does not call <see cref="GameManager.GameOver"/>.
    /// Set this to <c>false</c> on enemy Health components.
    /// </summary>
    public bool TriggerGameOverOnDeath
    {
        get => triggerGameOverOnDeath;
        set => triggerGameOverOnDeath = value;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The character's current hit points.</summary>
    public float CurrentHealth { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever health changes.
    /// Parameters: <c>currentHealth</c>, <c>maxHealth</c>.
    /// Bind your health-bar UI to this event.
    /// </summary>
    public event System.Action<float, float> OnHealthChanged;

    /// <summary>Fired when health reaches zero. Bind game-over logic here.</summary>
    public event System.Action OnDeath;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the maximum health value (called by <see cref="CharacterStatApplier"/>
    /// before the scene fully starts). Also resets current health to the new max.
    /// </summary>
    /// <param name="value">New maximum health.</param>
    public void SetMaxHealth(float value)
    {
        maxHealth = Mathf.Max(1f, value);
        CurrentHealth = maxHealth;
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }

    /// <summary>
    /// Applies <paramref name="amount"/> damage points, clamping health to zero,
    /// and triggers <see cref="OnDeath"/> (plus <see cref="GameManager.GameOver"/>)
    /// when health runs out.
    /// </summary>
    /// <param name="amount">Positive number of hit points to subtract.</param>
    public void TakeDamage(int amount)
    {
        if (CurrentHealth <= 0f) return; // already dead

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        Debug.Log($"[Health] {gameObject.name} took {amount} damage — {CurrentHealth}/{maxHealth} HP remaining.");

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Handles death: fires events and notifies the GameManager.</summary>
    private void Die()
    {
        Debug.Log($"[Health] {gameObject.name} has died.");
        OnDeath?.Invoke();

        if (triggerGameOverOnDeath && GameManager.Instance != null)
            GameManager.Instance.GameOver();
    }
}
