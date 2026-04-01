using UnityEngine;

/// <summary>
/// Ninja — Rogue class.
/// <para><b>Passive — Wind Step:</b> Dash has no cooldown while the player is below 30 % HP.</para>
/// <para><b>Ultimate — Shuriken Storm:</b> Fires 30 shurikens in a spiral pattern.</para>
/// </summary>
public class CharNinja : CharacterAbilityHandler
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Passive — Wind Step")]
    [Tooltip("HP threshold (fraction 0–1) below which the dash cooldown is removed.")]
    [SerializeField] private float _windStepHPThreshold = 0.30f;

    [Header("Ultimate — Shuriken Storm")]
    [Tooltip("Number of shurikens fired in the spiral.")]
    [SerializeField] private int _shurikenCount = 30;

    [Tooltip("Angular step in degrees between consecutive shurikens.")]
    [SerializeField] private float _spiralStep = 12f;

    [Tooltip("Projectile prefab for each shuriken.")]
    [SerializeField] private GameObject _shurikenPrefab;

    [Tooltip("Mana required to activate Shuriken Storm.")]
    [SerializeField] private float _ultimateCost = 55f;

    [Tooltip("Cooldown in seconds after using Shuriken Storm.")]
    [SerializeField] private float _ultimateCooldown = 18f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private Health _health;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <c>true</c> when the player is below the Wind Step HP threshold.
    /// <see cref="PlayerController"/> should remove dash cooldown when this is set.
    /// </summary>
    public bool WindStepActive { get; private set; }

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
    /// Wind Step passive: sets <see cref="WindStepActive"/> based on current HP percentage.
    /// </summary>
    public override void ApplyPassive()
    {
        if (_health == null) return;
        float maxHp = _health.MaxHealth > 0f ? _health.MaxHealth : 1f;
        WindStepActive = _health.CurrentHealth / maxHp < _windStepHPThreshold;
    }

    /// <summary>
    /// Shuriken Storm: fires <see cref="_shurikenCount"/> shurikens in an
    /// expanding spiral (each shuriken offset by <see cref="_spiralStep"/> degrees).
    /// </summary>
    public override void ActivateUltimate()
    {
        if (_shurikenPrefab == null)
        {
            Debug.LogWarning("[CharNinja] Shuriken prefab not assigned — Shuriken Storm skipped.");
            return;
        }

        for (int i = 0; i < _shurikenCount; i++)
        {
            float angle = i * _spiralStep;
            Vector3 dir = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
            Instantiate(_shurikenPrefab, transform.position, Quaternion.LookRotation(Vector3.forward, dir));
        }

        Debug.Log("[CharNinja] Shuriken Storm fired.");
    }
}
