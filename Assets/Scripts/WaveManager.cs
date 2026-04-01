using System.Collections;
using UnityEngine;

/// <summary>
/// Singleton that tracks the current wave number and the number of living enemies.
/// When all enemies in a wave are killed a 3-second delay fires before the next wave begins.
///
/// Usage (from enemy scripts):
///   <code>WaveManager.Instance.RegisterEnemy();</code>  // call in enemy Start/Awake
///   <code>WaveManager.Instance.EnemyKilled();</code>    // call when an enemy dies
///
/// Attach to: A persistent manager GameObject in the game scene.
/// </summary>
public class WaveManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single WaveManager instance.</summary>
    public static WaveManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Wave Settings")]
    [Tooltip("Delay in seconds between the last enemy dying and the next wave starting.")]
    [SerializeField] private float waveTransitionDelay = 3f;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The wave the player is currently on (starts at 1).</summary>
    public int CurrentWave { get; private set; } = 1;

    /// <summary>Number of enemies currently alive in the active wave.</summary>
    public int EnemiesAlive { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Events
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Fired whenever the wave number changes.
    /// Parameter: the new wave number.
    /// Bind <see cref="WaveCounterUI"/> (and any other listener) to this event.
    /// </summary>
    public event System.Action<int> OnWaveChanged;

    // ─────────────────────────────────────────────────────────────────────────
    //  Private State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Prevents multiple calls to <see cref="EnemyKilled"/> from starting more than
    /// one wave-transition coroutine simultaneously (race-condition guard).
    /// </summary>
    private bool _isTransitioning;

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        // Enforce singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // Broadcast wave 1 so the UI initialises to "WAVE 1"
        OnWaveChanged?.Invoke(CurrentWave);
    }

    private void OnDestroy()
    {
        // Clear the static reference when the object is destroyed so it is not
        // accidentally used after a scene unload.
        if (Instance == this)
            Instance = null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Call from each enemy's <c>Awake</c> or <c>Start</c> to register it with the wave.
    /// </summary>
    public void RegisterEnemy()
    {
        EnemiesAlive++;
        Debug.Log($"[WaveManager] Enemy registered. Alive: {EnemiesAlive}");
    }

    /// <summary>
    /// Call when an enemy is destroyed/killed.
    /// Automatically starts the next-wave countdown when the last enemy dies.
    /// </summary>
    public void EnemyKilled()
    {
        EnemiesAlive = Mathf.Max(0, EnemiesAlive - 1);
        Debug.Log($"[WaveManager] Enemy killed. Alive: {EnemiesAlive}");

        // Guard against multiple simultaneous calls triggering more than one transition
        if (EnemiesAlive == 0 && !_isTransitioning)
        {
            _isTransitioning = true;
            StartCoroutine(WaveTransitionCoroutine());
        }
    }

    /// <summary>
    /// Immediately advances to the next wave and fires <see cref="OnWaveChanged"/>.
    /// Can be called directly to skip the transition delay (e.g. in testing).
    /// </summary>
    public void StartNextWave()
    {
        _isTransitioning = false;
        CurrentWave++;
        Debug.Log($"[WaveManager] Wave {CurrentWave} started.");
        OnWaveChanged?.Invoke(CurrentWave);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Waits <see cref="waveTransitionDelay"/> seconds then starts the next wave.</summary>
    private IEnumerator WaveTransitionCoroutine()
    {
        Debug.Log($"[WaveManager] All enemies defeated. Next wave in {waveTransitionDelay}s…");
        yield return new WaitForSeconds(waveTransitionDelay);
        StartNextWave();
    }
}
