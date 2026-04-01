using UnityEngine;

/// <summary>
/// Tracks consecutive hit combos and provides a damage multiplier that grows
/// with the combo counter. Combos expire when no hits are registered within
/// <see cref="comboWindow"/> seconds.
///
/// Attach to: The Player GameObject.
/// </summary>
public class ComboSystem : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Constants
    // ─────────────────────────────────────────────────────────────────────────

    private const float DamageMultiplierStep = 0.05f;
    private const float MaxDamageMultiplier = 3.0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Combo Settings")]
    [Tooltip("Seconds of inactivity before the combo resets to zero.")]
    [SerializeField] private float comboWindow = 1.5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _comboTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Current number of consecutive hits in this combo chain.</summary>
    public int CurrentCombo { get; private set; }

    /// <summary>Highest combo count reached during the current run.</summary>
    public int MaxCombo { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Raised every time the combo count changes. Parameter: new combo count.</summary>
    public event System.Action<int> OnComboChanged;

    /// <summary>
    /// Raised when the combo expires (timer runs out). Parameter: final combo count
    /// at the moment of the break.
    /// </summary>
    public event System.Action<int> OnComboBreak;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (CurrentCombo <= 0) return;

        _comboTimer -= Time.deltaTime;
        if (_comboTimer <= 0f)
        {
            BreakCombo();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call this each time a hit is successfully landed. Increments the combo,
    /// resets the expiry timer, and fires milestone log messages.
    /// </summary>
    public void RegisterHit()
    {
        CurrentCombo++;
        _comboTimer = comboWindow;

        if (CurrentCombo > MaxCombo)
            MaxCombo = CurrentCombo;

        OnComboChanged?.Invoke(CurrentCombo);
        LogMilestone(CurrentCombo);
    }

    /// <summary>
    /// Returns the damage multiplier for the current combo.
    /// Formula: <c>1.0 + (combo × 0.05)</c>, capped at <c>3.0×</c>.
    /// </summary>
    /// <returns>A value between 1.0 and 3.0 inclusive.</returns>
    public float GetDamageMultiplier()
    {
        float multiplier = 1f + CurrentCombo * DamageMultiplierStep;
        return Mathf.Min(multiplier, MaxDamageMultiplier);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Resets the combo counter and fires <see cref="OnComboBreak"/>.</summary>
    private void BreakCombo()
    {
        int finalCombo = CurrentCombo;
        CurrentCombo = 0;
        _comboTimer = 0f;
        OnComboBreak?.Invoke(finalCombo);
        OnComboChanged?.Invoke(0);
    }

    /// <summary>Logs a human-readable milestone message at notable combo thresholds.</summary>
    private static void LogMilestone(int combo)
    {
        switch (combo)
        {
            case 5:  Debug.Log("[ComboSystem] COMBO!");   break;
            case 10: Debug.Log("[ComboSystem] GREAT!");   break;
            case 20: Debug.Log("[ComboSystem] INSANE!");  break;
            case 50: Debug.Log("[ComboSystem] GODLIKE!"); break;
        }
    }
}
