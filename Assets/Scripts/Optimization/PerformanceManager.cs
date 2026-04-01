using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Applies global performance settings and runs per-frame optimisations:
/// <list type="bullet">
///   <item>Sets <see cref="Application.targetFrameRate"/> on <c>Awake</c>.</item>
///   <item>Exposes <see cref="SetQuality"/> to map to <see cref="QualitySettings.SetQualityLevel"/>.</item>
///   <item>Monitors active particle count via <see cref="HitEffectManager"/> and shrinks new particles when over budget.</item>
///   <item>Disables AI scripts on enemies that are more than <see cref="enemyCullDistance"/> units from the player.</item>
///   <item>Shifts distant renderables to a culled layer so the camera ignores them.</item>
/// </list>
///
/// Attach to: A persistent "Managers" GameObject in the game scene.
/// </summary>
public class PerformanceManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="PerformanceManager"/> instance.</summary>
    public static PerformanceManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Fields
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Frame Rate")]
    [Tooltip("Target frames per second the application should run at.")]
    [SerializeField] private int targetFrameRate = 60;

    [Tooltip("When true, sets Application.targetFrameRate on Awake.")]
    [SerializeField] private bool limitFrameRate = true;

    [Header("Particle Budget")]
    [Tooltip("Maximum number of simultaneously active particle systems before sizes are reduced.")]
    [SerializeField] private int particleBudget = 200;

    [Tooltip("Scale applied to new particle systems when the budget is exceeded.")]
    [SerializeField] private float overBudgetParticleScale = 0.5f;

    [Header("Enemy Culling")]
    [Tooltip("Distance from the player (in world units) beyond which enemy AI scripts are disabled.")]
    [SerializeField] private float enemyCullDistance = 20f;

    [Tooltip("How often (in seconds) the enemy culling pass runs.")]
    [SerializeField] private float enemyCullInterval = 0.5f;

    [Header("Camera Culling")]
    [Tooltip("Distance from the camera beyond which objects are moved to the culled layer.")]
    [SerializeField] private float cameraCullDistance = 25f;

    [Tooltip("Layer index to assign to objects outside the camera's culling distance.  " +
             "Ensure this layer is removed from the Main Camera's Culling Mask.")]
    [SerializeField] private int culledLayerIndex = 31;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    private float _cullTimer;
    private Transform _playerTransform;

    // Cached enemy AI components for culling pass
    private readonly List<EnemyBase> _trackedEnemies = new List<EnemyBase>();
    private readonly List<int>       _originalLayers  = new List<int>();

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

        if (limitFrameRate)
            Application.targetFrameRate = targetFrameRate;
    }

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) _playerTransform = player.transform;
    }

    private void Update()
    {
        _cullTimer += Time.deltaTime;
        if (_cullTimer >= enemyCullInterval)
        {
            _cullTimer = 0f;
            RunEnemyCullingPass();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies Unity quality preset <paramref name="level"/> via
    /// <see cref="QualitySettings.SetQualityLevel(int, bool)"/>.
    /// </summary>
    /// <param name="level">
    /// Index into Unity's quality-settings list (0 = lowest, higher = better).
    /// </param>
    public void SetQuality(int level)
    {
        QualitySettings.SetQualityLevel(Mathf.Clamp(level, 0, QualitySettings.names.Length - 1), true);
        Debug.Log($"[PerformanceManager] Quality set to level {level} ({QualitySettings.names[level]}).");
    }

    /// <summary>
    /// Returns the scale that should be applied to a newly spawned particle system.
    /// Returns 1 when under budget, <see cref="overBudgetParticleScale"/> when over.
    /// </summary>
    public float GetParticleScale()
    {
        int activeCount = GetActiveParticleCount();
        return activeCount > particleBudget ? overBudgetParticleScale : 1f;
    }

    /// <summary>
    /// Registers an enemy with the performance manager so it is included in the
    /// AI culling pass.  Called automatically by <see cref="EnemyBase"/> on Start.
    /// </summary>
    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy != null && !_trackedEnemies.Contains(enemy))
            _trackedEnemies.Add(enemy);
    }

    /// <summary>
    /// Unregisters an enemy from the culling pass (called on enemy death/destroy).
    /// </summary>
    public void UnregisterEnemy(EnemyBase enemy)
    {
        _trackedEnemies.Remove(enemy);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Enemy Culling Pass
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Iterates over all tracked enemies and enables/disables their AI components
    /// based on distance from the player.
    /// </summary>
    private void RunEnemyCullingPass()
    {
        if (_playerTransform == null)
        {
            // Retry finding the player in case it spawned after Start
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
            else return;
        }

        Vector3 playerPos = _playerTransform.position;

        for (int i = _trackedEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = _trackedEnemies[i];
            if (enemy == null)
            {
                _trackedEnemies.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(enemy.transform.position, playerPos);
            bool shouldEnable = dist <= enemyCullDistance;

            // Toggle the EnemyBase MonoBehaviour (pauses UpdateState / AI)
            if (enemy.enabled != shouldEnable)
                enemy.enabled = shouldEnable;

            // Camera culling: move objects beyond cameraCullDistance to culled layer
            int targetLayer = dist > cameraCullDistance ? culledLayerIndex : 0;
            if (enemy.gameObject.layer != targetLayer)
                enemy.gameObject.layer = targetLayer;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private — Particle Budget
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Returns the total number of currently active <see cref="ParticleSystem"/> components in the scene.</summary>
    private static int GetActiveParticleCount()
    {
        ParticleSystem[] all = FindObjectsOfType<ParticleSystem>();
        int count = 0;
        foreach (ParticleSystem ps in all)
            if (ps.isPlaying) count++;
        return count;
    }
}
