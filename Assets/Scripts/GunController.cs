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
/// Phase 3 additions: integrates <see cref="AmmoManager"/>, <see cref="ComboSystem"/>,
/// <see cref="KnockbackSystem"/>, and <see cref="HitEffectManager"/>.
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
    [Tooltip("Seconds between consecutive shots (lower = faster fire rate). " +
             "Overridden by the active AmmoType.fireRate when an AmmoManager is present.")]
    [SerializeField] private float fireRate = 0.25f;

    [Tooltip("Hit points subtracted from the target per bullet. " +
             "Overridden by the active AmmoType.damage when an AmmoManager is present.")]
    [SerializeField] private int damage = 10;

    [Tooltip("Speed of the spawned bullet projectile (units/second). " +
             "Overridden by the active AmmoType.bulletSpeed when an AmmoManager is present.")]
    [SerializeField] private float bulletSpeed = 20f;

    [Tooltip("Maximum distance a bullet travels before being destroyed. " +
             "Overridden by the active AmmoType.bulletRange when an AmmoManager is present.")]
    [SerializeField] private float bulletRange = 15f;

    [Header("Bullet Prefab")]
    [Tooltip("Prefab with BulletProjectile + Rigidbody2D + Collider2D (trigger). " +
             "If left empty, a hitscan raycast is used instead.")]
    [SerializeField] private BulletProjectile bulletPrefab;

    [Header("Hitscan (fallback when no prefab assigned)")]
    [Tooltip("LayerMask for hitscan targets when no bullet prefab is used.")]
    [SerializeField] private LayerMask hitscanLayerMask = ~0; // everything by default

    [Header("Knockback")]
    [Tooltip("Duration in seconds the knockback velocity is applied to the hit target.")]
    [SerializeField] private float knockbackDuration = 0.15f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _nextFireTime;
    private SpriteRenderer _spriteRenderer;
    private AmmoManager _ammoManager;
    private ComboSystem _comboSystem;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Current base damage value; can be overridden by CharacterStatApplier.</summary>
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
        _ammoManager    = GetComponent<AmmoManager>();
        _comboSystem    = GetComponent<ComboSystem>();
    }

    private void Update()
    {
        // Do not fire while the game is paused or in a non-playing state
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        // Update the fire-point position every frame so it tracks sprite flipping
        UpdateFirePointPosition();

        // Determine the effective fire rate (AmmoType overrides inspector value)
        float effectiveFireRate = GetEffectiveFireRate();

        // Fire on left-mouse / controller "Fire1"
        if (Input.GetButtonDown("Fire1") && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + effectiveFireRate;
            TryShoot();
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
    /// Returns the fire rate in seconds per shot. Uses the active AmmoType value
    /// when an <see cref="AmmoManager"/> is present, otherwise falls back to the
    /// inspector-set <see cref="fireRate"/>.
    /// </summary>
    private float GetEffectiveFireRate()
    {
        if (_ammoManager != null && _ammoManager.CurrentAmmo != null)
            return _ammoManager.CurrentAmmo.fireRate;
        return fireRate;
    }

    /// <summary>
    /// Checks ammo availability then fires; does nothing if out of ammo.
    /// </summary>
    private void TryShoot()
    {
        // Consume one round; bail out if the magazine is empty
        if (_ammoManager != null && !_ammoManager.TryConsumeAmmo())
        {
            Debug.Log("[GunController] Out of ammo – cannot fire.");
            return;
        }

        Shoot();
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
        int   effectiveDamage = GetEffectiveDamage();
        float effectiveSpeed  = GetEffectiveBulletSpeed();
        float effectiveRange  = GetEffectiveBulletRange();

        BulletProjectile bullet = Instantiate(bulletPrefab, origin, Quaternion.identity);
        bullet.Init(direction, effectiveDamage, effectiveSpeed, effectiveRange);
    }

    /// <summary>
    /// Performs an instant hitscan raycast, applies combo-scaled damage to the
    /// first <see cref="IDamageable"/> hit, applies knockback, and spawns hit
    /// particles via <see cref="HitEffectManager"/>.
    /// </summary>
    private void FireHitscan(Vector2 origin, Vector2 direction)
    {
        int   effectiveDamage = GetEffectiveDamage();
        float effectiveRange  = GetEffectiveBulletRange();

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, effectiveRange, hitscanLayerMask);

        if (hit.collider != null)
        {
            // ── Damage ────────────────────────────────────────────────────────
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(effectiveDamage);

                // Register the hit with the combo system
                _comboSystem?.RegisterHit();
            }

            // ── Knockback ─────────────────────────────────────────────────────
            Rigidbody2D hitRb = hit.collider.GetComponent<Rigidbody2D>();
            if (hitRb != null)
            {
                float knockbackForce = GetEffectiveKnockbackForce();
                KnockbackSystem.ApplyKnockback(hitRb, origin, knockbackForce, knockbackDuration);
            }

            // ── Hit Particles ─────────────────────────────────────────────────
            if (HitEffectManager.Instance != null)
            {
                Color hitColor = GetEffectiveBulletColor();
                HitEffectManager.Instance.PlayHitEffect(hit.point, hitColor);
            }

            Debug.Log($"[GunController] Hitscan hit {hit.collider.name} for {effectiveDamage} dmg");
        }

        // Draw the ray in the Scene view for easy debugging
        Debug.DrawRay(origin, direction * effectiveRange, hit.collider != null ? Color.red : Color.yellow, 0.1f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Effective-Value Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the effective damage per shot, factoring in the active AmmoType's
    /// base damage and the <see cref="ComboSystem"/> multiplier.
    /// </summary>
    private int GetEffectiveDamage()
    {
        int baseDamage = (_ammoManager != null && _ammoManager.CurrentAmmo != null)
            ? _ammoManager.CurrentAmmo.damage
            : damage;

        float multiplier = _comboSystem != null ? _comboSystem.GetDamageMultiplier() : 1f;
        return Mathf.RoundToInt(baseDamage * multiplier);
    }

    /// <summary>Returns bullet speed from AmmoType or inspector fallback.</summary>
    private float GetEffectiveBulletSpeed()
    {
        if (_ammoManager != null && _ammoManager.CurrentAmmo != null)
            return _ammoManager.CurrentAmmo.bulletSpeed;
        return bulletSpeed;
    }

    /// <summary>Returns bullet range from AmmoType or inspector fallback.</summary>
    private float GetEffectiveBulletRange()
    {
        if (_ammoManager != null && _ammoManager.CurrentAmmo != null)
            return _ammoManager.CurrentAmmo.bulletRange;
        return bulletRange;
    }

    /// <summary>Returns knockback force from AmmoType or zero when none is set.</summary>
    private float GetEffectiveKnockbackForce()
    {
        if (_ammoManager != null && _ammoManager.CurrentAmmo != null)
            return _ammoManager.CurrentAmmo.knockbackForce;
        return 0f;
    }

    /// <summary>Returns the bullet tint colour from AmmoType or white when none is set.</summary>
    private Color GetEffectiveBulletColor()
    {
        if (_ammoManager != null && _ammoManager.CurrentAmmo != null)
            return _ammoManager.CurrentAmmo.bulletColor;
        return Color.white;
    }
}
