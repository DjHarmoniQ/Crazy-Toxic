using System.Collections;
using UnityEngine;

/// <summary>
/// Bat enemy — flies (ignores ground collisions) and swoops at the player.
/// The bat circles above the player then dives in a swoop attack.
/// </summary>
public class EnemyBat : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Bat — Flight")]
    [Tooltip("Height above the player the bat hovers at while circling.")]
    [SerializeField] private float hoverHeight = 3f;

    [Tooltip("Radius of the circular hover pattern around the player.")]
    [SerializeField] private float circleRadius = 2f;

    [Tooltip("Angular speed of the circular hover (degrees per second).")]
    [SerializeField] private float circleSpeed = 90f;

    [Header("Bat — Swoop Attack")]
    [Tooltip("Speed of the swoop dive toward the player.")]
    [SerializeField] private float swoopSpeed = 12f;

    [Tooltip("Duration of a single swoop in seconds.")]
    [SerializeField] private float swoopDuration = 0.4f;

    [Tooltip("Distance from the player that triggers the swoop.")]
    [SerializeField] private float swoopTriggerDistance = 4f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _circleAngle;
    private bool _isSwooping;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        moveSpeed = 4f;
        attackRange = 1f;
        base.Start();

        // Disable gravity so the bat floats freely
        if (_rb != null)
            _rb.gravityScale = 0f;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Deals contact damage to the player during a swoop.</summary>
    protected override void OnAttack()
    {
        if (_player == null) return;

        IDamageable target = _player.GetComponent<IDamageable>();
        target?.TakeDamage((int)_scaledDamage);
    }

    /// <summary>
    /// Extended state machine: bats circle above the player then swoop in to attack.
    /// </summary>
    protected override void UpdateState()
    {
        if (_currentState == EnemyState.Hurt || _currentState == EnemyState.Dead || _isSwooping)
            return;

        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);

        if (dist > detectionRange)
        {
            _currentState = EnemyState.Idle;
            StopMovement();
            return;
        }

        _currentState = EnemyState.Chase;

        if (dist <= swoopTriggerDistance && _attackCooldownTimer <= 0f)
        {
            _attackCooldownTimer = attackCooldown;
            StartCoroutine(SwoopAttack());
        }
        else
        {
            CircleAroundPlayer();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Orbits the player at <see cref="hoverHeight"/>.</summary>
    private void CircleAroundPlayer()
    {
        if (_player == null || _rb == null) return;

        _circleAngle += circleSpeed * Time.deltaTime;
        float rad = _circleAngle * Mathf.Deg2Rad;
        Vector2 target = (Vector2)_player.position
                         + new Vector2(Mathf.Cos(rad) * circleRadius, hoverHeight);

        Vector2 dir = (target - (Vector2)transform.position).normalized;
        _rb.linearVelocity = dir * moveSpeed;
    }

    /// <summary>Dives toward the player then returns to the circling pattern.</summary>
    private IEnumerator SwoopAttack()
    {
        _isSwooping = true;
        _currentState = EnemyState.Attack;

        Vector2 swoopDir = _player != null
            ? ((Vector2)_player.position - (Vector2)transform.position).normalized
            : Vector2.down;

        float elapsed = 0f;
        while (elapsed < swoopDuration)
        {
            if (_rb != null)
                _rb.linearVelocity = swoopDir * swoopSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Deal damage at the end of the swoop
        if (_player != null && Vector2.Distance(transform.position, _player.position) <= attackRange * 2f)
            OnAttack();

        StopMovement();
        _isSwooping = false;
        _currentState = EnemyState.Chase;
    }
}
