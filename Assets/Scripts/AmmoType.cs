using UnityEngine;

/// <summary>
/// Special on-hit effects that can be applied by an ammo type.
/// </summary>
public enum AmmoEffect
{
    /// <summary>No special effect.</summary>
    None,
    /// <summary>Deals area-of-effect damage on impact.</summary>
    Explosive,
    /// <summary>Applies a damage-over-time poison status.</summary>
    Poison,
    /// <summary>Slows or freezes the target on hit.</summary>
    Freeze,
    /// <summary>Bullet passes through multiple targets.</summary>
    Pierce,
    /// <summary>Bullet bounces off surfaces.</summary>
    Bounce,
    /// <summary>Chains damage to nearby enemies on hit.</summary>
    Chain
}

/// <summary>
/// ScriptableObject that defines the properties of a single ammo type
/// (damage, speed, range, fire rate, special effects, etc.).
/// Create instances via <c>Assets → Create → Crazy-Toxic → AmmoType</c>.
/// </summary>
[CreateAssetMenu(fileName = "NewAmmoType", menuName = "Crazy-Toxic/AmmoType")]
public class AmmoType : ScriptableObject
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Identity
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Identity")]
    [Tooltip("Display name shown in the HUD ammo indicator.")]
    public string ammoName;

    [Tooltip("Icon displayed in the HUD for this ammo type.")]
    public Sprite icon;

    // ─────────────────────────────────────────────────────────────────────────
    //  Combat Stats
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Combat Stats")]
    [Tooltip("Base hit-point damage dealt per bullet.")]
    public int damage = 10;

    [Tooltip("Travel speed of the bullet in units per second.")]
    public float bulletSpeed = 20f;

    [Tooltip("Maximum distance the bullet travels before being destroyed.")]
    public float bulletRange = 15f;

    [Tooltip("Minimum seconds between consecutive shots (lower = faster).")]
    public float fireRate = 0.25f;

    [Tooltip("Maximum number of rounds in a full magazine.")]
    public int maxAmmo = 30;

    // ─────────────────────────────────────────────────────────────────────────
    //  Visual
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Visual")]
    [Tooltip("Tint colour applied to the bullet sprite at runtime.")]
    public Color bulletColor = Color.white;

    // ─────────────────────────────────────────────────────────────────────────
    //  Special Effect
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Special Effect")]
    [Tooltip("On-hit special effect for this ammo type.")]
    public AmmoEffect effect = AmmoEffect.None;

    [Tooltip(
        "Scalar value whose meaning depends on the effect type:\n" +
        "• Explosive – explosion radius (units)\n" +
        "• Poison     – poison duration (seconds)\n" +
        "• Freeze     – freeze duration (seconds)\n" +
        "• Bounce     – maximum number of bounces\n" +
        "• Chain      – maximum number of chain targets\n" +
        "• Pierce     – maximum targets pierced\n" +
        "• None       – unused")]
    public float effectValue = 1f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Knockback
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Knockback")]
    [Tooltip("Force magnitude applied to the hit target's Rigidbody2D.")]
    public float knockbackForce = 5f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Infinite Ammo Flag
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Infinite Ammo")]
    [Tooltip("When true the ammo count is never decremented (e.g. melee or default attack).")]
    public bool isInfinite = false;
}
