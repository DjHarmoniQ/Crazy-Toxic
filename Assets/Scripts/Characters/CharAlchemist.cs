using UnityEngine;

/// <summary>
/// Alchemist — Support class.
/// <para><b>Passive — Experiment:</b> All potion/heal effects are boosted by 20 %.</para>
/// <para><b>Ultimate — Transmute:</b> Converts all ammo to explosive rounds for 10 seconds.</para>
/// </summary>
public class CharAlchemist : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Experiment")]
    [Tooltip("Bonus multiplier applied on top of all potion/heal effects (e.g. 0.2 = +20 %).")]
    [SerializeField] private float _potionBonusFraction = 0.20f;

    [Header("Ultimate — Transmute")]
    [Tooltip("Duration in seconds that all ammo is converted to explosive.")]
    [SerializeField] private float _transmuteDuration = 10f;

    [Tooltip("The explosive AmmoType ScriptableObject to equip during Transmute.")]
    [SerializeField] private AmmoType _explosiveAmmoType;

    [Tooltip("Mana required to activate Transmute.")]
    [SerializeField] private float _ultimateCost = 50f;

    [Tooltip("Cooldown in seconds after Transmute expires.")]
    [SerializeField] private float _ultimateCooldown = 20f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _transmuteTimer;
    private AmmoManager _ammoManager;
    private int _previousAmmoIndex;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Multiplicative bonus applied to all healing amounts (e.g. 1.2 = +20 %).
    /// </summary>
    public float PotionMultiplier => 1f + _potionBonusFraction;

    /// <summary><c>true</c> while Transmute is converting ammo to explosive.</summary>
    public bool TransmuteActive => _transmuteTimer > 0f;

    // ─────────────────────────────────────────────────────────────────────────
    //  CharacterAbilityHandler Implementation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public override float UltimateCost => _ultimateCost;

    /// <inheritdoc/>
    public override float UltimateCooldown => _ultimateCooldown;

    /// <summary>Caches <see cref="AmmoManager"/> in addition to base references.</summary>
    protected override void Awake()
    {
        base.Awake();
        _ammoManager = GetComponent<AmmoManager>();
    }

    /// <summary>
    /// Experiment passive: no per-frame tick — bonus is exposed via <see cref="PotionMultiplier"/>.
    /// Also ticks the Transmute duration timer.
    /// </summary>
    public override void ApplyPassive()
    {
        if (_transmuteTimer > 0f)
        {
            _transmuteTimer -= Time.deltaTime;
            if (_transmuteTimer <= 0f)
            {
                // Restore original ammo type when Transmute expires
                if (_ammoManager != null)
                    _ammoManager.SwitchAmmo(_previousAmmoIndex);
                Debug.Log("[CharAlchemist] Transmute expired — ammo restored.");
            }
        }
    }

    /// <summary>
    /// Transmute: converts all ammo to the <see cref="_explosiveAmmoType"/> for
    /// <see cref="_transmuteDuration"/> seconds, then restores the previous ammo.
    /// </summary>
    public override void ActivateUltimate()
    {
        if (_ammoManager != null)
        {
            _previousAmmoIndex = _ammoManager.CurrentAmmoIndex;

            if (_explosiveAmmoType != null)
                _ammoManager.SwitchToAmmo(_explosiveAmmoType);
        }

        _transmuteTimer = _transmuteDuration;
        Debug.Log($"[CharAlchemist] Transmute active — all ammo is Explosive for {_transmuteDuration}s.");
    }
}
