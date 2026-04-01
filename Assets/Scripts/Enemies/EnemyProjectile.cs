using UnityEngine;

/// <summary>
/// Simple projectile component attached to enemy-fired projectile prefabs.
/// Deals a configurable amount of damage to any <see cref="IDamageable"/> it contacts,
/// then destroys itself.
///
/// Attach to: bone, arrow, and elite-projectile prefabs used by enemy AI.
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Projectile")]
    [Tooltip("Tag of GameObjects this projectile will damage on contact.")]
    [SerializeField] private string targetTag = "Player";

    [Tooltip("Maximum lifetime in seconds before the projectile is automatically destroyed.")]
    [SerializeField] private float lifetime = 5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private int _damage;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the projectile with the damage value set by the spawning enemy.
    /// Call immediately after <c>Instantiate</c>.
    /// </summary>
    /// <param name="damage">Amount of damage this projectile deals on impact.</param>
    public void Init(int damage)
    {
        _damage = damage;
        Destroy(gameObject, lifetime);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(targetTag)) return;

        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(_damage);
            Debug.Log($"[EnemyProjectile] Hit {other.gameObject.name} for {_damage} damage.");
        }

        Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag(targetTag))
        {
            IDamageable target = collision.gameObject.GetComponent<IDamageable>();
            target?.TakeDamage(_damage);
        }
        Destroy(gameObject);
    }
}
