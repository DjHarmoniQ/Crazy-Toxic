using UnityEngine;

/// <summary>
/// Paladin — Tank class.
/// <para><b>Passive — Holy Aura:</b> Regenerates 2 HP per second.</para>
/// <para><b>Ultimate — Divine Shield:</b> Absorbs up to 200 damage before breaking.</para>
/// </summary>
public class CharPaladin : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Holy Aura")]
    [Tooltip("Hit points regenerated per second.")]
    [SerializeField] private float _regenPerSecond = 2f;

    [Header("Ultimate — Divine Shield")]
    [Tooltip("Total damage the Divine Shield absorbs before it breaks.")]
    [SerializeField] private float _shieldCapacity = 200f;

    [Tooltip("Mana required to activate Divine Shield.")]
    [SerializeField] private float _ultimateCost = 55f;

    [Tooltip("Cooldown in seconds after the shield breaks or expires.")]
    [SerializeField] private float _ultimateCooldown = 25f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Health _health;
    private float _shieldRemaining;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Remaining shield absorption capacity. 0 when shield is inactive.</summary>
    public float ShieldRemaining => _shieldRemaining;

    /// <summary><c>true</c> while the Divine Shield has remaining capacity.</summary>
    public bool ShieldActive => _shieldRemaining > 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>Caches the <see cref="Health"/> component in addition to base references.</summary>
    protected override void Awake()
    {
        base.Awake();
        _health = GetComponent<Health>();
    }

    /// <summary>
    /// Holy Aura passive: restores <see cref="_regenPerSecond"/> HP every second.
    /// </summary>
    public override void ApplyPassive()
    {
        if (_health == null) return;

        float regen = _regenPerSecond * Time.deltaTime;
        HealPlayer(regen);
    }

    /// <summary>
    /// Divine Shield: sets the absorption buffer to <see cref="_shieldCapacity"/>.
    /// Combat code should call <see cref="AbsorbDamage"/> before forwarding hits to <see cref="Health"/>.
    /// </summary>
    public override void ActivateUltimate()
    {
        _shieldRemaining = _shieldCapacity;
        Debug.Log($"[CharPaladin] Divine Shield activated ({_shieldCapacity} absorption).");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Absorbs up to <paramref name="damage"/> points with the Divine Shield.
    /// Returns the leftover damage that should still be applied to <see cref="Health"/>.
    /// </summary>
    /// <param name="damage">Incoming damage amount.</param>
    /// <returns>Remaining damage after shield absorption.</returns>
    public int AbsorbDamage(int damage)
    {
        if (_shieldRemaining <= 0f) return damage;

        float absorbed = Mathf.Min(_shieldRemaining, damage);
        _shieldRemaining -= absorbed;

        if (_shieldRemaining <= 0f)
            Debug.Log("[CharPaladin] Divine Shield broke.");

        return Mathf.Max(0, damage - (int)absorbed);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Restores <paramref name="amount"/> HP via <see cref="Health.Heal"/>.</summary>
    private void HealPlayer(float amount)
    {
        _health?.Heal(amount);
    }
}
