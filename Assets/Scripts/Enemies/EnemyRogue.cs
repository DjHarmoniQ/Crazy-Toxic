using System.Collections;
using UnityEngine;

/// <summary>
/// Rogue enemy — approaches from stealth (invisible/semi-transparent) and deals
/// a backstab multiplier of 2× when attacking from behind the player.
/// </summary>
public class EnemyRogue : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Rogue — Stealth")]
    [Tooltip("Alpha value of the sprite while in stealth mode (0 = fully invisible).")]
    [SerializeField] private float stealthAlpha = 0.2f;

    [Tooltip("Distance at which the rogue breaks stealth and attacks.")]
    [SerializeField] private float stealthBreakRange = 1.5f;

    [Header("Rogue — Backstab")]
    [Tooltip("Damage multiplier applied when attacking the player from behind.")]
    [SerializeField] private float backstabMultiplier = 2f;

    [Tooltip(
        "Dot-product threshold for 'behind'. " +
        "The player is considered facing away when dot(playerForward, rogueToPlayer) > threshold.")]
    [SerializeField] private float backstabDotThreshold = 0.5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _inStealth = true;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        moveSpeed = 4.5f;
        base.Start();
        ApplyStealth(true);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Melee strike — applies backstab multiplier when attacking from behind the player.
    /// </summary>
    protected override void OnAttack()
    {
        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist > attackRange) return;

        // Break stealth on attack
        if (_inStealth)
        {
            ApplyStealth(false);
            _inStealth = false;
        }

        float damage = _scaledDamage;
        if (IsBackstab())
        {
            damage *= backstabMultiplier;
            Debug.Log($"[EnemyRogue] BACKSTAB! {damage} damage.");
        }

        IDamageable target = _player.GetComponent<IDamageable>();
        target?.TakeDamage((int)damage);
    }

    /// <summary>
    /// Extended state machine: maintains stealth while approaching, breaks on contact.
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
            _currentState = EnemyState.Idle;
            StopMovement();
            return;
        }

        _currentState = EnemyState.Chase;

        // Try to position behind the player
        MoveTowardPlayerBack();

        if (dist <= stealthBreakRange && _attackCooldownTimer <= 0f)
        {
            _attackCooldownTimer = attackCooldown;
            _currentState = EnemyState.Attack;
            OnAttack();
            _currentState = EnemyState.Chase;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Sets the sprite alpha to indicate stealth or visible state.</summary>
    private void ApplyStealth(bool stealth)
    {
        if (_spriteRenderer == null) return;
        Color c = _spriteRenderer.color;
        c.a = stealth ? stealthAlpha : 1f;
        _spriteRenderer.color = c;
    }

    /// <summary>Returns true when the rogue is positioned behind the player.</summary>
    private bool IsBackstab()
    {
        if (_player == null) return false;

        // Player's facing direction (assumed to be based on localScale.x sign)
        float playerFacingX = _player.localScale.x >= 0 ? 1f : -1f;
        Vector2 playerForward = new Vector2(playerFacingX, 0f);

        // Direction from player to rogue
        Vector2 playerToRogue = ((Vector2)transform.position - (Vector2)_player.position).normalized;

        // Backstab if the rogue is in the same direction the player is facing
        // (i.e. the player has their back to the rogue)
        return Vector2.Dot(playerForward, playerToRogue) > backstabDotThreshold;
    }

    /// <summary>Moves the rogue toward a position slightly behind the player.</summary>
    private void MoveTowardPlayerBack()
    {
        if (_player == null || _rb == null) return;

        // Target position: behind the player relative to their facing direction
        float playerFacingX = _player.localScale.x >= 0 ? 1f : -1f;
        Vector2 behindPlayer = (Vector2)_player.position + new Vector2(playerFacingX * 1f, 0f);

        Vector2 dir = (behindPlayer - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * moveSpeed;
    }
}
