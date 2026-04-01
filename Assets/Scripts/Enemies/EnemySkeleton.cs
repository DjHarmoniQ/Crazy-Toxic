using System.Collections;
using UnityEngine;

/// <summary>
/// Skeleton enemy — ranged attacker that throws bones at the player.
/// Retreats (moves away) when the player comes too close.
/// </summary>
public class EnemySkeleton : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Skeleton — Ranged Attack")]
    [Tooltip("Prefab for the bone projectile. If null a placeholder Debug.Log is used.")]
    [SerializeField] private GameObject boneProjectilePrefab;

    [Tooltip("Speed of the thrown bone projectile.")]
    [SerializeField] private float projectileSpeed = 8f;

    [Header("Skeleton — Retreat")]
    [Tooltip("Distance at which the skeleton starts retreating from the player.")]
    [SerializeField] private float retreatRange = 3f;

    [Tooltip("Movement speed while retreating.")]
    [SerializeField] private float retreatSpeed = 3.5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        attackRange = 6f;  // Skeletons attack at range
        base.Start();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Throws a bone projectile toward the player.</summary>
    protected override void OnAttack()
    {
        if (_player == null) return;

        if (boneProjectilePrefab != null)
        {
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            GameObject bone = Instantiate(boneProjectilePrefab, transform.position, Quaternion.identity);
            Rigidbody2D boneRb = bone.GetComponent<Rigidbody2D>();
            if (boneRb != null)
                boneRb.linearVelocity = dir * projectileSpeed;

            // Pass damage info to the projectile if it has an EnemyProjectile component
            EnemyProjectile proj = bone.GetComponent<EnemyProjectile>();
            if (proj != null)
                proj.Init((int)_scaledDamage);
        }
        else
        {
            Debug.Log($"[EnemySkeleton] Throws a bone at player for {_scaledDamage} damage.");
        }
    }

    /// <summary>
    /// Extended state machine: adds retreat logic when the player is too close.
    /// </summary>
    protected override void UpdateState()
    {
        if (_currentState == EnemyState.Hurt || _currentState == EnemyState.Dead)
            return;

        float dist = _player != null
            ? Vector2.Distance(transform.position, _player.position)
            : float.MaxValue;

        // Retreat if player is closer than retreat range
        if (dist < retreatRange && _currentState != EnemyState.Idle)
        {
            Retreat();
            return;
        }

        base.UpdateState();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Moves away from the player at <see cref="retreatSpeed"/>.</summary>
    private void Retreat()
    {
        if (_player == null || _rb == null) return;
        Vector2 awayDir = ((Vector2)transform.position - (Vector2)_player.position).normalized;
        _rb.linearVelocity = awayDir * retreatSpeed;
    }
}
