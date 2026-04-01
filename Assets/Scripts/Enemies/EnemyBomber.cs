using System.Collections;
using UnityEngine;

/// <summary>
/// Bomber enemy — charges straight at the player and explodes on contact or on death.
/// The explosion deals AoE damage to all IDamageable targets within the blast radius.
/// </summary>
public class EnemyBomber : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Bomber — Explosion")]
    [Tooltip("Radius of the explosion blast.")]
    [SerializeField] private float explosionRadius = 3f;

    [Tooltip("Damage dealt to every IDamageable within the explosion radius.")]
    [SerializeField] private int explosionDamage = 40;

    [Tooltip("Layer mask for the explosion AoE overlap check.")]
    [SerializeField] private LayerMask explosionLayers;

    [Tooltip("Optional VFX prefab instantiated at the explosion origin.")]
    [SerializeField] private GameObject explosionVfx;

    [Header("Bomber — Charge")]
    [Tooltip("Speed multiplier applied while the bomber is in charging mode.")]
    [SerializeField] private float chargeSpeedMultiplier = 2f;

    [Tooltip("Distance at which the bomber starts its final charge toward the player.")]
    [SerializeField] private float chargeRange = 4f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _hasExploded;
    private bool _isCharging;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        moveSpeed = 3f;
        base.Start();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Contact attack — triggers the explosion immediately.</summary>
    protected override void OnAttack()
    {
        Explode();
    }

    /// <summary>
    /// Extended state machine: switches to a faster charge when within <see cref="chargeRange"/>.
    /// </summary>
    protected override void UpdateState()
    {
        if (_currentState == EnemyState.Hurt || _currentState == EnemyState.Dead)
            return;

        float dist = _player != null
            ? Vector2.Distance(transform.position, _player.position)
            : float.MaxValue;

        if (dist > detectionRange)
        {
            _isCharging = false;
            _currentState = EnemyState.Idle;
            StopMovement();
            return;
        }

        _currentState = EnemyState.Chase;

        if (dist <= chargeRange)
            _isCharging = true;

        if (_isCharging)
        {
            // Full-speed charge toward the player
            ChargeAtPlayer();
        }
        else
        {
            MoveTowardPlayer();
        }

        // Explode on contact
        if (dist <= attackRange)
        {
            _attackCooldownTimer = attackCooldown;
            OnAttack();
        }
    }

    /// <summary>
    /// On death: triggers the explosion (if not already exploded), then calls base death.
    /// </summary>
    protected override void OnDeath()
    {
        if (!_hasExploded)
            Explode();

        base.OnDeath();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Deals AoE explosion damage and destroys the bomber.</summary>
    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        if (explosionVfx != null)
            Instantiate(explosionVfx, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, explosionLayers);
        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && hit.gameObject != gameObject)
            {
                damageable.TakeDamage(explosionDamage);
                Debug.Log($"[EnemyBomber] Explosion hit {hit.gameObject.name} for {explosionDamage}.");
            }
        }
    }

    /// <summary>Moves toward the player at charge speed (faster than normal).</summary>
    private void ChargeAtPlayer()
    {
        if (_player == null || _rb == null) return;
        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * (moveSpeed * chargeSpeedMultiplier);
    }
}
