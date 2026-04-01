using UnityEngine;

/// <summary>
/// Shielder enemy — holds a shield that absorbs all damage from the front.
/// Attacks from behind bypass the shield and deal full damage.
/// </summary>
public class EnemyShielder : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Shielder — Shield Block")]
    [Tooltip("Dot-product threshold used to determine 'frontal' direction. " +
             "Attacks whose direction dot-product with the shielder's forward is greater than this " +
             "are considered frontal and blocked.")]
    [SerializeField] private float frontBlockThreshold = 0.3f;

    [Tooltip("Fraction of incoming frontal damage that is blocked by the shield. " +
             "1 = full block (no damage passes through), 0 = no block (full damage passes through).")]
    [SerializeField] private float shieldDamageReduction = 1f;

    [Tooltip("Optional sprite to display when the shield is active (child object). Can be null.")]
    [SerializeField] private SpriteRenderer shieldSprite;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _shieldActive = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        base.Start();
        UpdateShieldVisual();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  IDamageable Override
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Intercepts damage: blocks frontal damage with the shield; rear attacks deal full damage.
    /// </summary>
    /// <param name="amount">Incoming damage amount.</param>
    public override void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    /// <summary>
    /// Intercepts damage with source-position awareness: blocks frontal attacks.
    /// </summary>
    /// <param name="amount">Incoming damage amount.</param>
    /// <param name="sourcePosition">World-space position of the attacker/projectile.</param>
    public override void TakeDamage(int amount, Vector2 sourcePosition)
    {
        if (_shieldActive && sourcePosition != Vector2.zero && IsFrontalAttack(sourcePosition))
        {
            int reduced = Mathf.RoundToInt(amount * (1f - shieldDamageReduction));
            Debug.Log($"[EnemyShielder] Shield blocked! Damage reduced from {amount} to {reduced}.");
            amount = reduced;
        }

        base.TakeDamage(amount, sourcePosition);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Melee shield-bash — deals scaled damage to the player.</summary>
    protected override void OnAttack()
    {
        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= attackRange)
        {
            IDamageable target = _player.GetComponent<IDamageable>();
            target?.TakeDamage((int)_scaledDamage);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when <paramref name="sourcePosition"/> is in front of the shielder
    /// (i.e. the attack comes from the same side as the shielder is facing).
    /// </summary>
    private bool IsFrontalAttack(Vector2 sourcePosition)
    {
        // The shielder's forward direction (based on facing scale)
        float facingX = transform.localScale.x >= 0 ? 1f : -1f;
        Vector2 forward = new Vector2(facingX, 0f);

        // Direction from shielder to source
        Vector2 toSource = (sourcePosition - (Vector2)transform.position).normalized;

        return Vector2.Dot(forward, toSource) > frontBlockThreshold;
    }

    /// <summary>Shows or hides the shield sprite based on <see cref="_shieldActive"/>.</summary>
    private void UpdateShieldVisual()
    {
        if (shieldSprite != null)
            shieldSprite.enabled = _shieldActive;
    }
}
