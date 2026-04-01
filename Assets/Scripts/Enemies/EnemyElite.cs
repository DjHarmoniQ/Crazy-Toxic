using System.Collections;
using UnityEngine;

/// <summary>
/// Elite enemy — a randomly-constructed hybrid of two other enemy types.
/// Only spawns on wave 20 and beyond.
/// Displays a crown sprite to indicate elite status.
/// </summary>
public class EnemyElite : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Elite — Crown Indicator")]
    [Tooltip("SpriteRenderer for the crown indicator shown above the elite. " +
             "Assign a child GameObject with a crown sprite here.")]
    [SerializeField] private SpriteRenderer crownSprite;

    [Header("Elite — Hybrid Stats")]
    [Tooltip("Flat health bonus added on top of the base+scaled health.")]
    [SerializeField] private float eliteHealthBonus = 100f;

    [Tooltip("Flat damage bonus added on top of the base+scaled damage.")]
    [SerializeField] private float eliteDamageBonus = 10f;

    [Header("Elite — Hybrid Behaviours")]
    [Tooltip("When enabled the elite gains the Rogue's backstab mechanic.")]
    [SerializeField] private bool hasBackstab = false;

    [Tooltip("Backstab damage multiplier (active when hasBackstab is true).")]
    [SerializeField] private float backstabMultiplier = 2f;

    [Tooltip("When enabled the elite can perform a ground-pound AoE (Golem behaviour).")]
    [SerializeField] private bool hasGroundPound = false;

    [Tooltip("Radius of the elite's ground-pound AoE.")]
    [SerializeField] private float groundPoundRadius = 3f;

    [Tooltip("Damage of the elite's ground-pound AoE.")]
    [SerializeField] private int groundPoundDamage = 25;

    [Tooltip("Layer mask for the ground-pound AoE.")]
    [SerializeField] private LayerMask groundPoundLayers;

    [Tooltip("When enabled the elite launches a ranged projectile (Archer/Skeleton behaviour).")]
    [SerializeField] private bool hasRangedAttack = false;

    [Tooltip("Projectile prefab for the elite's ranged attack.")]
    [SerializeField] private GameObject rangedProjectilePrefab;

    [Tooltip("Speed of the elite's ranged projectile.")]
    [SerializeField] private float rangedProjectileSpeed = 9f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private bool _isGroundPounding;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        // Randomly assign two hybrid behaviours if none were set in the Inspector
        RandomiseHybrid();

        baseHealth += eliteHealthBonus;
        base.Start();

        // Apply elite damage bonus after base scaling
        _scaledDamage += eliteDamageBonus;

        // Show crown
        if (crownSprite != null)
            crownSprite.enabled = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Hybrid attack — selects from available attack types based on range and active behaviours.
    /// </summary>
    protected override void OnAttack()
    {
        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);

        // Ground-pound if player is within melee range
        if (hasGroundPound && !_isGroundPounding && dist <= attackRange * 1.5f)
        {
            StartCoroutine(GroundPound());
            return;
        }

        // Ranged attack if player is at range
        if (hasRangedAttack && dist > attackRange)
        {
            FireProjectile();
            return;
        }

        // Default: melee hit with optional backstab
        MeleeHit();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Randomly selects two behaviour flags to create a unique hybrid on each spawn.
    /// Only runs when no behaviours have been pre-configured via the Inspector.
    /// </summary>
    private void RandomiseHybrid()
    {
        if (hasBackstab || hasGroundPound || hasRangedAttack) return; // Already configured

        // Choose 2 unique behaviours from the available set
        int[] all = { 0, 1, 2 };
        // Fisher-Yates shuffle for 3 elements
        int tmp;
        int j = Random.Range(0, 3); tmp = all[0]; all[0] = all[j]; all[j] = tmp;
        j = Random.Range(1, 3);     tmp = all[1]; all[1] = all[j]; all[j] = tmp;

        // Enable the first two
        for (int i = 0; i < 2; i++)
        {
            switch (all[i])
            {
                case 0: hasBackstab = true;      break;
                case 1: hasGroundPound = true;   break;
                case 2: hasRangedAttack = true;  break;
            }
        }

        Debug.Log($"[EnemyElite] Hybrid traits: backstab={hasBackstab} groundPound={hasGroundPound} ranged={hasRangedAttack}");
    }

    /// <summary>Standard melee hit — optionally applies backstab multiplier.</summary>
    private void MeleeHit()
    {
        float dist = _player != null
            ? Vector2.Distance(transform.position, _player.position)
            : float.MaxValue;

        if (dist > attackRange) return;

        float dmg = _scaledDamage;
        if (hasBackstab && IsBackstab())
        {
            dmg *= backstabMultiplier;
            Debug.Log($"[EnemyElite] BACKSTAB! {dmg} damage.");
        }

        IDamageable target = _player.GetComponent<IDamageable>();
        target?.TakeDamage((int)dmg);
    }

    /// <summary>Fires a projectile toward the player.</summary>
    private void FireProjectile()
    {
        if (_player == null) return;

        if (rangedProjectilePrefab != null)
        {
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            GameObject proj = Instantiate(rangedProjectilePrefab, transform.position, Quaternion.identity);
            Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = dir * rangedProjectileSpeed;

            EnemyProjectile ep = proj.GetComponent<EnemyProjectile>();
            if (ep != null) ep.Init((int)_scaledDamage);
        }
        else
        {
            Debug.Log($"[EnemyElite] Fires ranged projectile for {_scaledDamage} damage.");
        }
    }

    /// <summary>Performs a ground-pound AoE attack.</summary>
    private IEnumerator GroundPound()
    {
        _isGroundPounding = true;
        StopMovement();
        yield return new WaitForSeconds(0.5f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, groundPoundRadius, groundPoundLayers);
        foreach (Collider2D hit in hits)
        {
            IDamageable dmg = hit.GetComponent<IDamageable>();
            if (dmg != null && hit.gameObject != gameObject)
                dmg.TakeDamage(groundPoundDamage);
        }

        _isGroundPounding = false;
    }

    /// <summary>Returns true when positioned behind the player (for backstab check).</summary>
    private bool IsBackstab()
    {
        if (_player == null) return false;
        float facingX = _player.localScale.x >= 0 ? 1f : -1f;
        Vector2 forward = new Vector2(facingX, 0f);
        Vector2 toSelf = ((Vector2)transform.position - (Vector2)_player.position).normalized;
        return Vector2.Dot(forward, toSelf) > 0.5f;
    }
}
