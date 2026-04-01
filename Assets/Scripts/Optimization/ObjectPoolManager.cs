using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generic round-robin object pool for any <see cref="MonoBehaviour"/> prefab.
/// Keeps a separate pool per prefab instance so that multiple prefab types can
/// share one manager.
///
/// Pre-warmed pools are created in <see cref="Awake"/> for bullets, enemy
/// projectiles, hit particles, blood effects, and UI damage popups.
///
/// Usage:
/// <code>
///   BulletProjectile b = ObjectPoolManager.Instance.Get(bulletPrefab);
///   // … when done:
///   ObjectPoolManager.Instance.Return(b);
/// </code>
///
/// Attach to: A persistent "Managers" GameObject in the scene.
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="ObjectPoolManager"/> instance.</summary>
    public static ObjectPoolManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Pool Settings")]
    [Tooltip("Number of objects pre-instantiated per prefab when the scene loads.")]
    [SerializeField] private int defaultPoolSize = 20;

    [Header("Pre-warm Prefabs")]
    [Tooltip("Bullet projectile prefab — pool pre-warmed on Awake.")]
    [SerializeField] private BulletProjectile bulletPrefab;

    [Tooltip("Enemy projectile prefab — pool pre-warmed on Awake.")]
    [SerializeField] private EnemyProjectile enemyProjectilePrefab;

    [Tooltip("Hit-spark particle prefab — pool pre-warmed on Awake.")]
    [SerializeField] private ParticleSystem hitParticlePrefab;

    [Tooltip("Blood-splat particle prefab — pool pre-warmed on Awake.")]
    [SerializeField] private ParticleSystem bloodParticlePrefab;

    [Tooltip("Floating damage-number popup prefab — pool pre-warmed on Awake.")]
    [SerializeField] private TMPro.TextMeshProUGUI damagePopupPrefab;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Pool Storage
    // ─────────────────────────────────────────────────────────────────────────

    // Key: prefab instance ID  →  Value: queue of inactive clones
    private readonly Dictionary<int, Queue<MonoBehaviour>> _pools =
        new Dictionary<int, Queue<MonoBehaviour>>();

    // Prefab lookup by instance ID (so we can instantiate more on demand)
    private readonly Dictionary<int, MonoBehaviour> _prefabRegistry =
        new Dictionary<int, MonoBehaviour>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Pre-warm pools for all assigned prefabs
        PreWarm(bulletPrefab,          defaultPoolSize);
        PreWarm(enemyProjectilePrefab, defaultPoolSize);
        PreWarm(hitParticlePrefab,     defaultPoolSize);
        PreWarm(bloodParticlePrefab,   defaultPoolSize);
        PreWarm(damagePopupPrefab,     defaultPoolSize);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves an inactive instance of <paramref name="prefab"/> from the pool.
    /// If the pool is empty a new instance is created.
    /// </summary>
    /// <typeparam name="T">Component type (must derive from <see cref="MonoBehaviour"/>).</typeparam>
    /// <param name="prefab">The prefab to retrieve an instance of.</param>
    /// <returns>An active instance ready to use.</returns>
    public T Get<T>(T prefab) where T : MonoBehaviour
    {
        if (prefab == null)
        {
            Debug.LogError("[ObjectPoolManager] Get called with a null prefab.");
            return null;
        }

        int key = prefab.GetInstanceID();
        EnsurePoolExists(prefab, key);

        Queue<MonoBehaviour> pool = _pools[key];
        T obj;

        if (pool.Count > 0)
        {
            obj = (T)pool.Dequeue();
        }
        else
        {
            // Pool exhausted — instantiate a new one
            obj = Instantiate(prefab, transform);
        }

        obj.gameObject.SetActive(true);
        return obj;
    }

    /// <summary>
    /// Returns <paramref name="obj"/> to its pool by deactivating it.
    /// The object must have been obtained via <see cref="Get{T}"/> with the same prefab.
    /// </summary>
    /// <typeparam name="T">Component type (must derive from <see cref="MonoBehaviour"/>).</typeparam>
    /// <param name="obj">The instance to return.</param>
    /// <param name="prefab">The prefab this instance was spawned from.</param>
    public void Return<T>(T obj, T prefab) where T : MonoBehaviour
    {
        if (obj == null || prefab == null) return;

        obj.gameObject.SetActive(false);
        obj.transform.SetParent(transform);

        int key = prefab.GetInstanceID();
        EnsurePoolExists(prefab, key);
        _pools[key].Enqueue(obj);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Pre-instantiates <paramref name="count"/> inactive copies of <paramref name="prefab"/>.</summary>
    private void PreWarm<T>(T prefab, int count) where T : MonoBehaviour
    {
        if (prefab == null) return;

        int key = prefab.GetInstanceID();
        EnsurePoolExists(prefab, key);

        Queue<MonoBehaviour> pool = _pools[key];
        for (int i = 0; i < count; i++)
        {
            T instance = Instantiate(prefab, transform);
            instance.gameObject.SetActive(false);
            pool.Enqueue(instance);
        }
    }

    /// <summary>Creates the pool queue and registers the prefab if not already done.</summary>
    private void EnsurePoolExists<T>(T prefab, int key) where T : MonoBehaviour
    {
        if (!_pools.ContainsKey(key))
        {
            _pools[key]           = new Queue<MonoBehaviour>();
            _prefabRegistry[key]  = prefab;
        }
    }
}
