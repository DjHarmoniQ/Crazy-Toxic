using UnityEngine;

/// <summary>
/// Handles all shooting logic for the player's gun.
///
/// ── GRIDLINE BUG FIX ──────────────────────────────────────────────────────
/// The original bug: the shot origin was derived from the player's world-unit
/// position (e.g. transform.position + offset), which Unity snapped to the
/// physics grid, causing bullets to spawn underground or inside geometry.
///
/// The fix: assign a dedicated child Transform (<see cref="firePoint"/>) in the
/// Unity Editor at the exact pixel position of the gun barrel.  Because the
/// Transform is a scene object it is never grid-snapped by physics, so shots
/// always originate from the correct visual position.
/// ─────────────────────────────────────────────────────────────────────────
///
/// The component also mirrors the fire-point offset when the sprite is flipped
/// so the gun stays in the player's LEFT hand regardless of facing direction.
///
/// Attach to: The Player GameObject (alongside SpriteRenderer).
/// </summary>
public class GunController : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Fire Point")]
    [Tooltip(
        "Child Transform placed at the gun barrel in the Editor. " +
        "This MUST be a child GameObject, NOT a calculated world position — " +
        "that is the gridline-bug fix: child Transforms are never physics-grid-snapped.")]
    [SerializeField] private Transform firePoint;

    [Tooltip(
        "Pixel-accurate offset from the player pivot to the gun barrel. " +
        "Negative X = left side (gun in left hand). " +
        "Mirrored automatically when the sprite is flipped.")]
    [SerializeField] private Vector2 firePointOffset = new Vector2(-0.4f, 0.1f);

    [Header("Fire Settings")]
    [Tooltip("Seconds between consecutive shots (lower = faster fire rate).")]
    [SerializeField] private float fireRate = 0.25f;

    [Tooltip("Hit points subtracted from the target per bullet.")]
    [SerializeField] private int damage = 10;

    [Tooltip("Speed of the spawned bullet projectile (units/second).")]
    [SerializeField] private float bulletSpeed = 20f;

    [Tooltip("Maximum distance a bullet travels before being destroyed.")]
    [SerializeField] private float bulletRange = 15f;

    [Header("Bullet Prefab")]
    [Tooltip("Prefab with BulletProjectile + Rigidbody2D + Collider2D (trigger). " +
             "If left empty, a hitscan raycast is used instead.")]
    [SerializeField] private BulletProjectile bulletPrefab;

    [Header("Hitscan (fallback when no prefab assigned)")]
    [Tooltip("LayerMask for hitscan targets when no bullet prefab is used.")]
    [SerializeField] private LayerMask hitscanLayerMask = ~0; // everything by default

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _nextFireTime;
    private SpriteRenderer _spriteRenderer;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Current damage value; can be overridden by CharacterStatApplier.</summary>
    public int Damage
    {
        get => damage;
        set => damage = value;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        // Do not fire while the game is paused or in a non-playing state
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Update the fire-point position every frame so it tracks sprite flipping
        UpdateFirePointPosition();

        // Fire on left-mouse / controller "Fire1"
        if (Input.GetButtonDown("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Repositions the firePoint child Transform based on the sprite's flip state
    /// so the gun always appears in the LEFT hand of the character.
    /// </summary>
    private void UpdateFirePointPosition()
    {
        if (firePoint == null) return;

        // Mirror the X offset when the sprite is flipped so the gun stays on
        // the correct (left) hand side regardless of facing direction.
        bool flipped = _spriteRenderer != null && _spriteRenderer.flipX;
        float xOffset = flipped ? -firePointOffset.x : firePointOffset.x;

        // localPosition keeps the offset relative to the parent (player) pivot,
        // which avoids any world-unit grid snapping — this is the gridline fix.
        firePoint.localPosition = new Vector3(xOffset, firePointOffset.y, 0f);
    }

    /// <summary>
    /// Fires either a physical bullet (if a prefab is assigned) or a hitscan ray.
    /// </summary>
    private void Shoot()
    {
        Vector2 origin = GetFireOrigin();
        Vector2 direction = GetFireDirection();

        if (bulletPrefab != null)
        {
            FirePhysicalBullet(origin, direction);
        }
        else
        {
            FireHitscan(origin, direction);
        }
    }

    /// <summary>
    /// Returns the world-space position from which bullets spawn.
    /// Prefers the firePoint Transform; falls back to a calculated offset so the
    /// game still works before the Designer assigns the child in the Inspector.
    /// </summary>
    private Vector2 GetFireOrigin()
    {
        if (firePoint != null)
            return firePoint.position;

        // Fallback: calculate from the player pivot (gridline risk — use firePoint instead!)
        bool flipped = _spriteRenderer != null && _spriteRenderer.flipX;
        float xOffset = flipped ? -firePointOffset.x : firePointOffset.x;
        return (Vector2)transform.position + new Vector2(xOffset, firePointOffset.y);
    }

    /// <summary>Returns the normalised fire direction based on the sprite flip state.</summary>
    private Vector2 GetFireDirection()
    {
        bool flipped = _spriteRenderer != null && _spriteRenderer.flipX;
        return flipped ? Vector2.left : Vector2.right;
    }

    /// <summary>Instantiates a <see cref="BulletProjectile"/> and initialises it.</summary>
    private void FirePhysicalBullet(Vector2 origin, Vector2 direction)
    {
        BulletProjectile bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
        bullet.Init(direction, damage, bulletSpeed, bulletRange);
    }

    /// <summary>
    /// Performs an instant hitscan raycast and applies damage if the ray hits an
    /// <see cref="IDamageable"/> target.
    /// </summary>
    private void FireHitscan(Vector2 origin, Vector2 direction)
    {
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, bulletRange, hitscanLayerMask);

        if (hit.collider != null)
        {
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            target?.TakeDamage(damage);
            Debug.Log($"[GunController] Hitscan hit {hit.collider.name}");
        }

        // Draw the ray in the Scene view for easy debugging
        Debug.DrawRay(origin, direction * bulletRange, hit.collider != null ? Color.red : Color.yellow, 0.1f);
    }
}
