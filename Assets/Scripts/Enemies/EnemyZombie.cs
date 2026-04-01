using System.Collections;
using UnityEngine;

/// <summary>
/// Zombie enemy — slow and tankier than a basic enemy.
/// On a successful melee hit the zombie infects the player with a poison
/// damage-over-time (DoT) effect.
/// </summary>
public class EnemyZombie : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Zombie — Infection (Poison DoT)")]
    [Tooltip("Damage per tick of the poison DoT applied to the player.")]
    [SerializeField] private int poisonTickDamage = 3;

    [Tooltip("Number of poison ticks.")]
    [SerializeField] private int poisonTicks = 5;

    [Tooltip("Interval in seconds between each poison damage tick.")]
    [SerializeField] private float poisonTickInterval = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        moveSpeed = 1.5f;     // Zombies are slow
        baseHealth = 120f;    // Zombies are tankier
        base.Start();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Melee bite — deals scaled damage and applies a poison DoT to the player.
    /// </summary>
    protected override void OnAttack()
    {
        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist > attackRange) return;

        IDamageable target = _player.GetComponent<IDamageable>();
        target?.TakeDamage((int)_scaledDamage);

        // Apply poison DoT
        StartCoroutine(PoisonDoT(target));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Applies <see cref="poisonTicks"/> damage ticks to the player at regular intervals.</summary>
    private IEnumerator PoisonDoT(IDamageable target)
    {
        for (int i = 0; i < poisonTicks; i++)
        {
            yield return new WaitForSeconds(poisonTickInterval);
            if (target == null) yield break;
            target.TakeDamage(poisonTickDamage);
            Debug.Log($"[EnemyZombie] Poison tick {i + 1}/{poisonTicks} — {poisonTickDamage} dmg");
        }
    }
}
