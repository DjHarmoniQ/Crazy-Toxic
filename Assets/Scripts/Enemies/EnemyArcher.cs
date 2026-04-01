using System.Collections;
using UnityEngine;

/// <summary>
/// Archer enemy — long-range attacker that kites the player (maintains distance)
/// and performs a dodge roll every few seconds.
/// </summary>
public class EnemyArcher : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Archer — Ranged Attack")]
    [Tooltip("Prefab for the arrow projectile. Falls back to a Debug.Log if null.")]
    [SerializeField] private GameObject arrowPrefab;

    [Tooltip("Speed of the fired arrow in units per second.")]
    [SerializeField] private float arrowSpeed = 10f;

    [Header("Archer — Kiting")]
    [Tooltip("Desired distance the archer tries to maintain from the player.")]
    [SerializeField] private float preferredDistance = 6f;

    [Tooltip("Tolerance band: the archer won't move if within this many units of the preferred distance.")]
    [SerializeField] private float distanceTolerance = 1f;

    [Header("Archer — Roll")]
    [Tooltip("Interval in seconds between automatic dodge rolls.")]
    [SerializeField] private float rollInterval = 3f;

    [Tooltip("Force applied during the dodge roll.")]
    [SerializeField] private float rollForce = 7f;

    [Tooltip("Duration of the dodge roll impulse in seconds.")]
    [SerializeField] private float rollDuration = 0.25f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _rollTimer;
    private bool _isRolling;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        attackRange = preferredDistance + 1f; // Can shoot at range
        base.Start();
        _rollTimer = rollInterval;
    }

    /// <inheritdoc/>
    protected override void Update()
    {
        _rollTimer -= Time.deltaTime;
        base.Update();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Fires an arrow toward the player.</summary>
    protected override void OnAttack()
    {
        if (_player == null) return;

        if (arrowPrefab != null)
        {
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            GameObject arrow = Instantiate(arrowPrefab, transform.position, Quaternion.identity);
            Rigidbody2D arrowRb = arrow.GetComponent<Rigidbody2D>();
            if (arrowRb != null)
                arrowRb.linearVelocity = dir * arrowSpeed;

            EnemyProjectile proj = arrow.GetComponent<EnemyProjectile>();
            if (proj != null)
                proj.Init((int)_scaledDamage);
        }
        else
        {
            Debug.Log($"[EnemyArcher] Shoots an arrow at player for {_scaledDamage} damage.");
        }
    }

    /// <summary>
    /// Extended state machine: kites the player and periodically rolls sideways.
    /// </summary>
    protected override void UpdateState()
    {
        if (_currentState == EnemyState.Hurt || _currentState == EnemyState.Dead || _isRolling)
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

        // Periodic roll
        if (_rollTimer <= 0f)
        {
            _rollTimer = rollInterval;
            StartCoroutine(DodgeRoll());
            return;
        }

        // Kiting: maintain preferred distance
        Kite(dist);

        // Shoot when within attack range and off cooldown
        if (dist <= attackRange && _attackCooldownTimer <= 0f)
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

    /// <summary>
    /// Moves toward or away from the player to maintain <see cref="preferredDistance"/>.
    /// </summary>
    private void Kite(float currentDist)
    {
        if (_player == null || _rb == null) return;

        float delta = currentDist - preferredDistance;
        if (Mathf.Abs(delta) < distanceTolerance)
        {
            StopMovement();
            return;
        }

        Vector2 dir = delta < 0
            ? ((Vector2)transform.position - (Vector2)_player.position).normalized  // too close — retreat
            : ((Vector2)_player.position - (Vector2)transform.position).normalized; // too far — approach

        _rb.linearVelocity = dir * moveSpeed;
    }

    /// <summary>Performs a lateral dodge roll perpendicular to the player direction.</summary>
    private IEnumerator DodgeRoll()
    {
        _isRolling = true;

        Vector2 playerDir = _player != null
            ? ((Vector2)_player.position - (Vector2)transform.position).normalized
            : Vector2.right;

        // Roll perpendicular (90-degree rotation)
        Vector2 rollDir = new Vector2(-playerDir.y, playerDir.x);
        // Randomly pick left or right perpendicular
        if (Random.value < 0.5f) rollDir = -rollDir;

        float elapsed = 0f;
        while (elapsed < rollDuration)
        {
            if (_rb != null)
                _rb.linearVelocity = rollDir * rollForce;
            elapsed += Time.deltaTime;
            yield return null;
        }

        StopMovement();
        _isRolling = false;
    }
}
