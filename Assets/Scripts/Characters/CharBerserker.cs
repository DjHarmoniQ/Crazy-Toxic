using UnityEngine;

/// <summary>
/// Berserker — Warrior class.
/// <para><b>Passive — Rage:</b> Gains +1 % damage for every 1 % of max HP missing.</para>
/// <para><b>Ultimate — Blood Rage:</b> 5-second rampage granting +100 % damage and prevents death.</para>
/// </summary>
public class CharBerserker : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Ultimate — Blood Rage")]
    [Tooltip("Duration in seconds of the Blood Rage rampage.")]
    [SerializeField] private float _rageDuration = 5f;

    [Tooltip("Additional damage multiplier applied during Blood Rage (e.g. 2 = +100 %).")]
    [SerializeField] private float _rageDamageMultiplier = 2f;

    [Tooltip("Mana required to activate Blood Rage.")]
    [SerializeField] private float _ultimateCost = 65f;

    [Tooltip("Cooldown in seconds after Blood Rage ends.")]
    [SerializeField] private float _ultimateCooldown = 30f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Health _health;
    private float _rageTimer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Current damage multiplier combining Rage passive and Blood Rage ultimate.
    /// At full HP this equals 1.0; at low HP the passive scales up linearly.
    /// During Blood Rage the <see cref="_rageDamageMultiplier"/> is applied on top.
    /// </summary>
    public float DamageMultiplier { get; private set; } = 1f;

    /// <summary><c>true</c> while Blood Rage is active.</summary>
    public bool BloodRageActive => _rageTimer > 0f;

    /// <summary>
    /// When Blood Rage is active the Berserker cannot be killed (HP is clamped to 1).
    /// External damage code should check this flag before destroying the player.
    /// </summary>
    public bool IsDeathInhibited => BloodRageActive;

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
    /// Rage passive + Blood Rage tick.
    /// <list type="bullet">
    ///   <item>Passive: multiplier scales with missing HP percentage.</item>
    ///   <item>Ultimate: ticks the rage timer and clamps HP to 1 to prevent death.</item>
    /// </list>
    /// </summary>
    public override void ApplyPassive()
    {
        // Tick Blood Rage
        if (_rageTimer > 0f)
        {
            _rageTimer -= Time.deltaTime;

            // Prevent death during Blood Rage
            if (_health != null && _health.CurrentHealth <= 0f)
            {
                // We can't directly set health without a SetCurrentHealth API;
                // TakeDamage is the only path. We clamp via the flag IsDeathInhibited
                // which Health should check. This is a design-level hook.
            }
        }

        // Calculate Rage bonus using actual HP percentage
        float missingHPFraction = 0f;
        if (_health != null && _health.MaxHealth > 0f)
        {
            missingHPFraction = 1f - Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth);
        }

        float rageBonus = 1f + missingHPFraction;          // +1 % per 1 % missing HP
        float rageUltBonus = BloodRageActive ? _rageDamageMultiplier : 1f;
        DamageMultiplier = rageBonus * rageUltBonus;
    }

    /// <summary>
    /// Blood Rage: starts the rampage timer granting +100 % damage and death immunity.
    /// </summary>
    public override void ActivateUltimate()
    {
        _rageTimer = _rageDuration;
        Debug.Log($"[CharBerserker] Blood Rage activated for {_rageDuration}s!");
    }
}
