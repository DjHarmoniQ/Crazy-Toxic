using System.Collections;
using UnityEngine;

/// <summary>
/// Goblin enemy — fast melee attacker that dodge-rolls away from the player
/// when its health drops below 30 %.
/// </summary>
public class EnemyGoblin : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Goblin — Dodge Roll")]
    [Tooltip("Health percentage (0–1) below which the goblin starts dodge-rolling.")]
    [SerializeField] private float dodgeHealthThreshold = 0.3f;

    [Tooltip("Force applied during a dodge roll.")]
    [SerializeField] private float dodgeForce = 8f;

    [Tooltip("Duration of the dodge-roll impulse in seconds.")]
    [SerializeField] private float dodgeDuration = 0.3f;

    [Tooltip("Cooldown between consecutive dodge rolls in seconds.")]
    [SerializeField] private float dodgeCooldown = 1.5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _dodgeCooldownTimer;
    private bool _isDodging;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        moveSpeed = 5f; // Goblins are fast
        base.Start();
    }

    /// <inheritdoc/>
    protected override void Update()
    {
        _dodgeCooldownTimer -= Time.deltaTime;
        base.Update();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Melee slash — deals scaled damage when within attack range.
    /// </summary>
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

    /// <summary>
    /// Extended state machine tick: adds dodge-roll behaviour when HP is low.
    /// </summary>
    protected override void UpdateState()
    {
        if (_currentState == EnemyState.Hurt || _currentState == EnemyState.Dead || _isDodging)
            return;

        // Check whether to dodge
        if (ShouldDodge())
        {
            StartCoroutine(DodgeRoll());
            return;
        }

        base.UpdateState();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns true when health is below the dodge threshold and the dodge is off cooldown.</summary>
    private bool ShouldDodge()
    {
        if (_health == null || _dodgeCooldownTimer > 0f) return false;
        return HealthFraction() <= dodgeHealthThreshold;
    }

    /// <summary>Returns the ratio current HP / max HP.</summary>
    private float HealthFraction()
    {
        return _health.CurrentHealth / Mathf.Max(1f, _scaledMaxHealth);
    }

    /// <summary>Executes a dodge roll away from the player.</summary>
    private IEnumerator DodgeRoll()
    {
        _isDodging = true;
        _dodgeCooldownTimer = dodgeCooldown;

        // Roll away from player
        Vector2 awayDir = _player != null
            ? ((Vector2)transform.position - (Vector2)_player.position).normalized
            : Vector2.right;

        float elapsed = 0f;
        while (elapsed < dodgeDuration)
        {
            if (_rb != null)
                _rb.linearVelocity = awayDir * dodgeForce;
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopMovement();
        _isDodging = false;
    }
}
