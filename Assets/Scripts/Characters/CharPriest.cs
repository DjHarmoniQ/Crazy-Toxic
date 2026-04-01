using UnityEngine;

/// <summary>
/// Priest — Support class.
/// <para><b>Passive — Prayer:</b> Party (currently the player) regenerates 5 HP per second.</para>
/// <para><b>Ultimate — Miracle:</b> Fully heals the player and grants 10 seconds of invincibility.</para>
/// </summary>
public class CharPriest : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Prayer")]
    [Tooltip("HP regenerated per second by the Prayer passive.")]
    [SerializeField] private float _regenPerSecond = 5f;

    [Header("Ultimate — Miracle")]
    [Tooltip("Duration in seconds of the invincibility granted by Miracle.")]
    [SerializeField] private float _invincibleDuration = 10f;

    [Tooltip("Mana required to activate Miracle.")]
    [SerializeField] private float _ultimateCost = 80f;

    [Tooltip("Cooldown in seconds after using Miracle.")]
    [SerializeField] private float _ultimateCooldown = 35f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Health _health;
    private float _invincibleTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary><c>true</c> while the Miracle invincibility window is active.</summary>
    public bool IsInvincible => _invincibleTimer > 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>Caches the <see cref="Health"/> component.</summary>
    protected override void Awake()
    {
        base.Awake();
        _health = GetComponent<Health>();
    }

    /// <summary>
    /// Prayer passive: ticks HP regen and counts down the Miracle invincibility timer.
    /// </summary>
    public override void ApplyPassive()
    {
        // Tick invincibility
        if (_invincibleTimer > 0f)
            _invincibleTimer -= Time.deltaTime;

        // Regen: restore _regenPerSecond HP per second
        _health?.Heal(_regenPerSecond * Time.deltaTime);
    }

    /// <summary>
    /// Miracle: fully restores player HP and starts the invincibility timer.
    /// </summary>
    public override void ActivateUltimate()
    {
        if (_health != null)
            _health.Heal(_health.MaxHealth); // Heal by max ensures full restore

        _invincibleTimer = _invincibleDuration;
        Debug.Log($"[CharPriest] Miracle! Full heal + {_invincibleDuration}s invincibility.");
    }
}
