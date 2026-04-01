using UnityEngine;

/// <summary>
/// Moves a bullet prefab in a fixed direction, deals damage to the first
/// <see cref="IDamageable"/> it hits, and self-destructs after travelling
/// <c>bulletRange</c> units or on any collision.
///
/// Attach to: The bullet prefab GameObject (requires Rigidbody2D + Collider2D trigger).
/// Initialised at spawn time via <see cref="Init"/>.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BulletProjectile : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Runtime State (set by Init)
    // ─────────────────────────────────────────────────────────────────────────

    private int _damage;
    private float _speed;
    private float _range;
    private Vector3 _spawnPosition;
    private Rigidbody2D _rb;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        // Destroy the bullet once it has travelled the maximum range
        if (Vector3.Distance(transform.position, _spawnPosition) >= _range)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Try to deal damage to whatever was hit
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(_damage);
        }

        // Destroy bullet on any collision (wall, enemy, player, etc.)
        Destroy(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Initialises the bullet's direction, damage, speed, and range.
    /// Call this immediately after instantiating the bullet prefab.
    /// </summary>
    /// <param name="direction">Normalised 2-D travel direction.</param>
    /// <param name="damage">Hit points subtracted from the target.</param>
    /// <param name="speed">Travel speed in units per second.</param>
    /// <param name="range">Maximum distance before the bullet is destroyed.</param>
    public void Init(Vector2 direction, int damage, float speed, float range)
    {
        _damage = damage;
        _speed = speed;
        _range = range;
        _spawnPosition = transform.position;

        if (_rb != null)
            _rb.linearVelocity = direction.normalized * speed;
    }
}
