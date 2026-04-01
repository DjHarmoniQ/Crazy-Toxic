using System.Collections;
using UnityEngine;

/// <summary>
/// Listens to <see cref="WaveManager.OnWaveChanged"/> and spawns the correct number
/// of enemies at the start of each wave, with a configurable delay between each spawn.
///
/// On wave 20+ there is a 20% chance that each spawn slot produces an
/// <see cref="EnemyElite"/> instead of a random normal enemy.
///
/// Attach to: A manager GameObject in the game scene alongside WaveManager and WaveDifficultyScaler.
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Enemy Prefabs")]
    [Tooltip("Array of the 12 normal enemy prefabs. Spawner picks from these randomly each wave.")]
    [SerializeField] private EnemyBase[] enemyPrefabs;

    [Tooltip("Elite enemy prefab. Spawned instead of a normal enemy on wave 20+ (20 % chance).")]
    [SerializeField] private EnemyBase elitePrefab;

    [Header("Spawn Points")]
    [Tooltip("Array of possible spawn-point Transforms. A random one is picked for each spawn.")]
    [SerializeField] private Transform[] spawnPoints;

    [Header("Spawn Settings")]
    [Tooltip("Wave number from which elite enemies can appear.")]
    [SerializeField] private int eliteStartWave = 20;

    [Tooltip("Probability (0–1) that a given spawn slot is filled with an elite instead of a normal enemy.")]
    [Range(0f, 1f)]
    [SerializeField] private float eliteSpawnChance = 0.2f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void OnEnable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged += OnWaveChanged;
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged -= OnWaveChanged;
    }

    private void Start()
    {
        // Subscribe in case WaveManager fired before this component was enabled
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged += OnWaveChanged;
    }

    private void OnDestroy()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged -= OnWaveChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called when a new wave starts. Begins the spawn coroutine for that wave.
    /// </summary>
    /// <param name="wave">The new wave number.</param>
    private void OnWaveChanged(int wave)
    {
        StartCoroutine(SpawnWave(wave));
    }

    /// <summary>
    /// Spawns the required number of enemies for <paramref name="wave"/> one by one,
    /// each separated by a delay derived from the spawn-rate multiplier.
    /// </summary>
    private IEnumerator SpawnWave(int wave)
    {
        if (WaveDifficultyScaler.Instance == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            yield break;

        int enemyCount = WaveDifficultyScaler.Instance.GetEnemiesPerWave(wave);
        float spawnRate = WaveDifficultyScaler.Instance.GetSpawnRateMultiplier(wave);
        float spawnDelay = 1f / Mathf.Max(0.01f, spawnRate);

        Debug.Log($"[EnemySpawner] Wave {wave}: spawning {enemyCount} enemies (delay {spawnDelay:F2}s each).");

        for (int i = 0; i < enemyCount; i++)
        {
            SpawnEnemy(wave);
            yield return new WaitForSeconds(spawnDelay);
        }
    }

    /// <summary>
    /// Selects and instantiates a single enemy at a random spawn point.
    /// </summary>
    /// <param name="wave">Current wave number (determines elite eligibility).</param>
    private void SpawnEnemy(int wave)
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[EnemySpawner] No spawn points assigned!");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        EnemyBase prefab = SelectPrefab(wave);

        if (prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] Selected enemy prefab is null — check Inspector assignments.");
            return;
        }

        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }

    /// <summary>
    /// Returns the <see cref="EnemyBase"/> prefab to spawn, choosing between the
    /// elite prefab and a random normal enemy based on wave and chance.
    /// </summary>
    /// <param name="wave">Current wave number.</param>
    private EnemyBase SelectPrefab(int wave)
    {
        bool canSpawnElite = wave >= eliteStartWave && elitePrefab != null;

        if (canSpawnElite && Random.value < eliteSpawnChance)
            return elitePrefab;

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
            return null;

        return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
    }
}
