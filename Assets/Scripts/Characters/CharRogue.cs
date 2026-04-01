using UnityEngine;

/// <summary>
/// Rogue — Rogue class.
/// <para><b>Passive — Shadow Strike:</b> Attacks deal +50 % damage while in stealth.</para>
/// <para><b>Ultimate — Smoke Bomb:</b> Makes the player invincible for 2 seconds.</para>
/// </summary>
public class CharRogue : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Shadow Strike")]
    [Tooltip("Damage multiplier applied while the Rogue is in stealth.")]
    [SerializeField] private float _stealthDamageMultiplier = 1.50f;

    [Header("Ultimate — Smoke Bomb")]
    [Tooltip("Duration in seconds of the invincibility window.")]
    [SerializeField] private float _invincibleDuration = 2f;

    [Tooltip("Mana required to activate Smoke Bomb.")]
    [SerializeField] private float _ultimateCost = 35f;

    [Tooltip("Cooldown in seconds after using Smoke Bomb.")]
    [SerializeField] private float _ultimateCooldown = 15f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _isInStealth;
    private float _invincibleTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary><c>true</c> while the Rogue is actively in stealth.</summary>
    public bool IsInStealth => _isInStealth;

    /// <summary><c>true</c> while the Smoke Bomb invincibility window is active.</summary>
    public bool IsInvincible => _invincibleTimer > 0f;

    /// <summary>
    /// Current damage multiplier: <see cref="_stealthDamageMultiplier"/> while stealthy,
    /// 1.0 otherwise.
    /// </summary>
    public float DamageMultiplier => _isInStealth ? _stealthDamageMultiplier : 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>
    /// Shadow Strike passive: the Rogue is considered in stealth whenever
    /// they are not moving. The per-frame logic here checks velocity to
    /// determine stealth state. Damage multiplier is exposed via <see cref="DamageMultiplier"/>.
    /// </summary>
    public override void ApplyPassive()
    {
        // Tick invincible timer
        if (_invincibleTimer > 0f)
            _invincibleTimer -= Time.deltaTime;

        // Stealth: active when player velocity is effectively zero
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        _isInStealth = rb != null && rb.linearVelocity.sqrMagnitude < 0.01f;
    }

    /// <summary>
    /// Smoke Bomb: grants <see cref="_invincibleDuration"/> seconds of invincibility.
    /// The <see cref="Health"/> component should query <see cref="IsInvincible"/>
    /// before applying damage.
    /// </summary>
    public override void ActivateUltimate()
    {
        _invincibleTimer = _invincibleDuration;
        Debug.Log($"[CharRogue] Smoke Bomb active for {_invincibleDuration}s.");
    }
}
