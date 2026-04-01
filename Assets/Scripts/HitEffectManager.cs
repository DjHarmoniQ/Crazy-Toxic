using UnityEngine;

/// <summary>
/// Singleton manager that spawns pooled particle effects for hits, blood,
/// explosions, freeze, and poison.  Each effect type maintains its own pool
/// (default 20 instances) to avoid per-frame GC allocations.
///
/// Access via <see cref="Instance"/> from any script.
/// Attach to: A dedicated "Effects" GameObject in the scene.
/// </summary>
public class HitEffectManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The scene-global singleton instance.</summary>
    public static HitEffectManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Effect Prefabs")]
    [Tooltip("Particle prefab used for generic bullet-hit sparks.")]
    [SerializeField] private ParticleSystem hitEffectPrefab;

    [Tooltip("Particle prefab used for blood splat effects.")]
    [SerializeField] private ParticleSystem bloodEffectPrefab;

    [Tooltip("Particle prefab used for explosion blasts.")]
    [SerializeField] private ParticleSystem explosionEffectPrefab;

    [Tooltip("Particle prefab used for freeze / ice effects.")]
    [SerializeField] private ParticleSystem freezeEffectPrefab;

    [Tooltip("Particle prefab used for poison / acid effects.")]
    [SerializeField] private ParticleSystem poisonEffectPrefab;

    [Header("Object Pool")]
    [Tooltip("Number of pre-warmed instances per effect type.")]
    [SerializeField] private int poolSize = 20;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private – Object Pools
    // ─────────────────────────────────────────────────────────────────────────

    private ParticleSystem[] _hitPool;
    private ParticleSystem[] _bloodPool;
    private ParticleSystem[] _explosionPool;
    private ParticleSystem[] _freezePool;
    private ParticleSystem[] _poisonPool;

    private int _hitIndex;
    private int _bloodIndex;
    private int _explosionIndex;
    private int _freezeIndex;
    private int _poisonIndex;

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
        // NOTE: intentionally NOT calling DontDestroyOnLoad so each scene
        // manages its own pooled particles; avoids null-ref errors from
        // destroyed pool objects after a scene transition.

        // Pre-warm all pools
        _hitPool        = BuildPool(hitEffectPrefab,       poolSize);
        _bloodPool      = BuildPool(bloodEffectPrefab,     poolSize);
        _explosionPool  = BuildPool(explosionEffectPrefab, poolSize);
        _freezePool     = BuildPool(freezeEffectPrefab,    poolSize);
        _poisonPool     = BuildPool(poisonEffectPrefab,    poolSize);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Plays a generic hit-spark effect at the given world position.
    /// </summary>
    /// <param name="position">World-space spawn position.</param>
    /// <param name="color">Tint colour applied to the particle start colour.</param>
    /// <param name="size">Uniform scale multiplier for the particle system.</param>
    public void PlayHitEffect(Vector2 position, Color color, float size = 1f)
    {
        PlayEffect(_hitPool, ref _hitIndex, position, color, size);
    }

    /// <summary>
    /// Plays a blood-splat effect scaled by <paramref name="damage"/> amount.
    /// </summary>
    /// <param name="position">World-space spawn position.</param>
    /// <param name="damage">Damage amount; drives the emission count of the particles.</param>
    public void PlayBloodEffect(Vector2 position, int damage)
    {
        // Clamp emission multiplier: 1 particle per 10 damage, max 50
        float scale = Mathf.Clamp(damage / 10f, 0.5f, 5f);
        PlayEffect(_bloodPool, ref _bloodIndex, position, Color.red, scale);
    }

    /// <summary>
    /// Plays an explosion particle effect at the given world position.
    /// </summary>
    /// <param name="position">World-space spawn position.</param>
    /// <param name="radius">Explosion radius in world units; used to scale the effect.</param>
    public void PlayExplosionEffect(Vector2 position, float radius)
    {
        float scale = Mathf.Max(radius, 0.1f);
        PlayEffect(_explosionPool, ref _explosionIndex, position, new Color(1f, 0.5f, 0f), scale);
    }

    /// <summary>
    /// Plays a freeze / ice particle effect at the given world position.
    /// </summary>
    /// <param name="position">World-space spawn position.</param>
    public void PlayFreezeEffect(Vector2 position)
    {
        PlayEffect(_freezePool, ref _freezeIndex, position, new Color(0.5f, 0.9f, 1f), 1f);
    }

    /// <summary>
    /// Plays a poison / acid particle effect at the given world position.
    /// </summary>
    /// <param name="position">World-space spawn position.</param>
    public void PlayPoisonEffect(Vector2 position)
    {
        PlayEffect(_poisonPool, ref _poisonIndex, position, new Color(0.3f, 1f, 0.2f), 1f);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Allocates and pre-warms a round-robin pool of <paramref name="prefab"/> instances.
    /// Returns an empty array if <paramref name="prefab"/> is null.
    /// </summary>
    private ParticleSystem[] BuildPool(ParticleSystem prefab, int size)
    {
        if (prefab == null)
            return new ParticleSystem[0];

        ParticleSystem[] pool = new ParticleSystem[size];
        for (int i = 0; i < size; i++)
        {
            ParticleSystem ps = Instantiate(prefab, transform);
            ps.gameObject.SetActive(false);
            pool[i] = ps;
        }
        return pool;
    }

    /// <summary>
    /// Retrieves the next pooled instance in round-robin order, repositions it,
    /// tints it, scales it, and plays it.
    /// </summary>
    private void PlayEffect(ParticleSystem[] pool, ref int index, Vector2 position, Color color, float scale)
    {
        if (pool == null || pool.Length == 0)
        {
            Debug.LogWarning("[HitEffectManager] Effect pool is empty or prefab not assigned.");
            return;
        }

        ParticleSystem ps = pool[index];
        index = (index + 1) % pool.Length;

        if (ps == null) return;

        // Stop any currently running instance before reuse
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(true);

        // Position & scale
        ps.transform.position = position;
        ps.transform.localScale = Vector3.one * scale;

        // Tint the start colour
        var main = ps.main;
        main.startColor = color;

        ps.Play();
    }
}
