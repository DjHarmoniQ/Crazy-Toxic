using UnityEngine;

/// <summary>
/// Falling Rock hazard — World 3: Stone Fortress (waves 35-54).
/// Spawns a rock prefab at a random X position within the spawn range and
/// lets it fall under gravity.  The rock deals <see cref="EnvironmentalHazardBase._damage"/>
/// to any <see cref="IDamageable"/> it hits, then destroys itself on reaching
/// the floor (<see cref="_destroyYThreshold"/>).
/// </summary>
public class FallingRockHazard : EnvironmentalHazardBase
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Falling Rock")]
    [Tooltip("Prefab spawned as the falling rock. Must have a Rigidbody2D and Collider2D.")]
    [SerializeField] private GameObject _rockPrefab;

    [Tooltip("Y world position from which rocks are spawned (top of the screen/ceiling).")]
    [SerializeField] private float _spawnY = 10f;

    [Tooltip("Minimum X world position of the spawn range.")]
    [SerializeField] private float _spawnXMin = -8f;

    [Tooltip("Maximum X world position of the spawn range.")]
    [SerializeField] private float _spawnXMax = 8f;

    [Tooltip("Y world position below which a falling rock is automatically destroyed " +
             "(floor level).")]
    [SerializeField] private float _destroyYThreshold = -6f;

    [Tooltip("Damage dealt to the player on contact with the rock.")]
    [SerializeField] private int _rockDamage = 20;

    // ─────────────────────────────────────────────────────────────────────────
    //  Hazard Activation
    // ─────────────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    protected override void ActivateHazard()
    {
        if (_rockPrefab == null)
        {
            Debug.LogWarning("[FallingRockHazard] Rock prefab not assigned.");
            return;
        }

        float spawnX = Random.Range(_spawnXMin, _spawnXMax);
        Vector3 spawnPos = new Vector3(spawnX, _spawnY, 0f);
        GameObject rock = Instantiate(_rockPrefab, spawnPos, Quaternion.identity);

        // Attach the runtime component that handles damage and self-destruction
        FallingRockInstance instance = rock.AddComponent<FallingRockInstance>();
        instance.Init(_rockDamage, _destroyYThreshold);

        Debug.Log($"[FallingRockHazard] Rock spawned at X={spawnX:F1}.");
    }

    /// <inheritdoc/>
    protected override void DeactivateHazard()
    {
        // Rocks are self-managing — nothing to do here
    }
}

/// <summary>
/// Runtime component attached to each spawned rock.
/// Handles player collision damage and floor destruction.
/// </summary>
[RequireComponent(typeof(Collider2D))]
internal class FallingRockInstance : MonoBehaviour
{
    private int _damage;
    private float _destroyY;

    /// <summary>Initialises damage and floor Y threshold.</summary>
    public void Init(int damage, float destroyY)
    {
        _damage = damage;
        _destroyY = destroyY;

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (transform.position.y < _destroyY)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(_damage);
            Debug.Log($"[FallingRock] Hit {other.gameObject.name} for {_damage} damage.");
            Destroy(gameObject);
        }
        // Non-damageable objects (terrain, walls) are ignored so the rock continues to fall
    }
}
