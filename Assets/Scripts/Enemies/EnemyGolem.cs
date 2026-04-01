using System.Collections;
using UnityEngine;

/// <summary>
/// Golem enemy — heavy, slow-moving tank.
/// Immune to knockback and performs a ground-pound AoE attack that damages
/// all IDamageable targets within a configurable radius.
/// </summary>
[RequireComponent(typeof(KnockbackSystem))]
public class EnemyGolem : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Golem — Ground Pound AoE")]
    [Tooltip("Radius of the ground-pound shockwave AoE.")]
    [SerializeField] private float groundPoundRadius = 3f;

    [Tooltip("Damage dealt to every target within the ground-pound radius.")]
    [SerializeField] private int groundPoundDamage = 30;

    [Tooltip("Layer mask used for the AoE overlap check.")]
    [SerializeField] private LayerMask damageLayers;

    [Tooltip("Optional VFX prefab for the ground-pound shockwave.")]
    [SerializeField] private GameObject groundPoundVfx;

    [Tooltip("Wind-up duration before the ground-pound lands (seconds).")]
    [SerializeField] private float windUpDuration = 0.5f;

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
        moveSpeed = 1.8f;   // Golems are slow
        baseHealth = 200f;  // Golems are very tanky
        attackRange = 2.5f;
        base.Start();

        // Golems are immune to knockback
        KnockbackSystem ks = GetComponent<KnockbackSystem>();
        if (ks != null)
            ks.IsKnockbackImmune = true;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Triggers the ground-pound coroutine as the golem's attack.
    /// </summary>
    protected override void OnAttack()
    {
        if (!_isGroundPounding)
            StartCoroutine(GroundPound());
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wind-up then AoE ground-pound dealing <see cref="groundPoundDamage"/>
    /// to every <see cref="IDamageable"/> within <see cref="groundPoundRadius"/>.
    /// </summary>
    private IEnumerator GroundPound()
    {
        _isGroundPounding = true;
        StopMovement();

        // Wind-up pause
        yield return new WaitForSeconds(windUpDuration);

        // Shockwave VFX
        if (groundPoundVfx != null)
            Instantiate(groundPoundVfx, transform.position, Quaternion.identity);

        // Damage everything in range
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, groundPoundRadius, damageLayers);
        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && hit.gameObject != gameObject)
            {
                damageable.TakeDamage(groundPoundDamage);
                Debug.Log($"[EnemyGolem] Ground pound hit {hit.gameObject.name}");
            }
        }

        _isGroundPounding = false;
    }
}
