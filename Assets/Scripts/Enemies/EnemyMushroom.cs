using UnityEngine;

/// <summary>
/// Mushroom enemy — completely stationary.
/// On death it emits a poison cloud AoE that damages all IDamageable targets
/// within a configurable radius.
/// </summary>
public class EnemyMushroom : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Mushroom — Poison Cloud AoE")]
    [Tooltip("Radius of the poison cloud spawned on death.")]
    [SerializeField] private float poisonCloudRadius = 3f;

    [Tooltip("Damage dealt to every IDamageable target inside the poison cloud.")]
    [SerializeField] private int poisonCloudDamage = 20;

    [Tooltip("Layer mask used for the AoE overlap check.")]
    [SerializeField] private LayerMask damageLayers;

    [Tooltip("Optional VFX prefab for the poison cloud. Instantiated at death position.")]
    [SerializeField] private GameObject poisonCloudVfx;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        moveSpeed = 0f; // Mushrooms don't move
        base.Start();

        // Disable rigidbody movement
        if (_rb != null)
        {
            _rb.bodyType = RigidbodyType2D.Static;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Mushrooms don't perform a targeted attack; they only damage via their death cloud.
    /// This is intentionally a no-op.
    /// </summary>
    protected override void OnAttack()
    {
        // Mushrooms are passive — no direct attack
    }

    /// <summary>
    /// Overrides the default state machine: mushrooms never move or chase.
    /// </summary>
    protected override void UpdateState()
    {
        // Stationary — no state transitions needed
    }

    /// <summary>
    /// On death: emits the poison cloud AoE, then calls base death logic.
    /// </summary>
    protected override void OnDeath()
    {
        EmitPoisonCloud();
        base.OnDeath();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Overlaps a circle at the mushroom's position and deals <see cref="poisonCloudDamage"/>
    /// to every <see cref="IDamageable"/> found within the radius.
    /// </summary>
    private void EmitPoisonCloud()
    {
        if (poisonCloudVfx != null)
            Instantiate(poisonCloudVfx, transform.position, Quaternion.identity);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, poisonCloudRadius, damageLayers);
        foreach (Collider2D hit in hits)
        {
            IDamageable damageable = hit.GetComponent<IDamageable>();
            if (damageable != null && hit.gameObject != gameObject)
            {
                damageable.TakeDamage((int)(poisonCloudDamage * (_scaledDamage / Mathf.Max(1f, baseDamage))));
                Debug.Log($"[EnemyMushroom] Poison cloud hit {hit.gameObject.name}");
            }
        }
    }
}
