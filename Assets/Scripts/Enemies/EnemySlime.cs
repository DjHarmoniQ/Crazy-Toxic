using UnityEngine;

/// <summary>
/// Slime enemy — slow melee attacker that splits into two smaller slimes on death.
/// The mini-slimes are spawned from a configurable prefab and inherit a fraction
/// of the original's stats.
/// </summary>
public class EnemySlime : EnemyBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Slime — Split on Death")]
    [Tooltip("Prefab for the smaller slime spawned when this slime dies. Should be a smaller " +
             "version of the slime with isMiniSlime = true to prevent infinite splitting.")]
    [SerializeField] private GameObject miniSlimePrefab;

    [Tooltip("Number of mini-slimes to spawn on death.")]
    [SerializeField] private int splitCount = 2;

    [Tooltip("When true this slime is already a mini-slime and will NOT split further on death.")]
    [SerializeField] private bool isMiniSlime = false;

    // ─────────────────────────────────────────────────────────────────────────
    //  Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void Start()
    {
        // Slimes are slow by default
        moveSpeed = isMiniSlime ? 2.5f : 1.5f;
        base.Start();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  EnemyBase Overrides
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Melee slam — deals scaled damage to the player if they are within attack range.
    /// </summary>
    protected override void OnAttack()
    {
        if (_player == null) return;

        float dist = Vector2.Distance(transform.position, _player.position);
        if (dist <= attackRange)
        {
            IDamageable target = _player.GetComponent<IDamageable>();
            target?.TakeDamage((int)_scaledDamage);
        }
    }

    /// <summary>
    /// On death: spawns <see cref="splitCount"/> mini-slimes around the death position
    /// (only when <see cref="isMiniSlime"/> is false), then calls the base death handler.
    /// </summary>
    protected override void OnDeath()
    {
        if (!isMiniSlime && miniSlimePrefab != null)
        {
            for (int i = 0; i < splitCount; i++)
            {
                // Offset each mini-slime slightly so they don't overlap
                Vector2 offset = Random.insideUnitCircle * 0.5f;
                GameObject mini = Instantiate(miniSlimePrefab,
                    (Vector2)transform.position + offset,
                    Quaternion.identity);

                // Mark it as a mini-slime so it won't split again
                EnemySlime miniComp = mini.GetComponent<EnemySlime>();
                if (miniComp != null)
                    miniComp.isMiniSlime = true;
            }
        }

        base.OnDeath();
    }
}
