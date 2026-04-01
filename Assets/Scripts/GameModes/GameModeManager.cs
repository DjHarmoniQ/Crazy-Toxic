using UnityEngine;

/// <summary>
/// Singleton that owns the active <see cref="GameModeBase"/> and proxies all
/// game events (wave cleared, enemy killed, player damaged/dead) to it.
///
/// Call <see cref="SetMode"/> from the mode-select UI before loading the game
/// scene, then hook the instance up to WaveManager / Health events at runtime.
/// </summary>
public class GameModeManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="GameModeManager"/> instance.</summary>
    public static GameModeManager Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Available Modes")]
    [Tooltip("All GameModeBase components available for selection. " +
             "Order matches the index passed to SetMode.")]
    [SerializeField] private GameModeBase[] _availableModes;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Properties
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The game mode currently active for this run.</summary>
    public GameModeBase CurrentMode { get; private set; }

    /// <summary>Index of the selected mode within <see cref="_availableModes"/>.</summary>
    public int SelectedModeIndex { get; private set; }

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
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Subscribe to WaveManager events once the scene is ready.
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged += HandleWaveChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (WaveManager.Instance != null)
            WaveManager.Instance.OnWaveChanged -= HandleWaveChanged;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Selects and activates the mode at <paramref name="index"/> in
    /// <see cref="_availableModes"/>. Called by <see cref="ModeSelectUI"/> before
    /// the game scene is loaded.
    /// </summary>
    /// <param name="index">Zero-based index into <see cref="_availableModes"/>.</param>
    public void SetMode(int index)
    {
        if (_availableModes == null || index < 0 || index >= _availableModes.Length)
        {
            Debug.LogWarning($"[GameModeManager] Invalid mode index {index}.");
            return;
        }

        SelectedModeIndex = index;
        CurrentMode = _availableModes[index];
        Debug.Log($"[GameModeManager] Mode set to: {CurrentMode.ModeName}");
    }

    /// <summary>
    /// Starts the active mode. Call after the game scene finishes loading.
    /// </summary>
    public void StartCurrentMode()
    {
        if (CurrentMode == null)
        {
            Debug.LogWarning("[GameModeManager] No mode selected — defaulting to index 0.");
            SetMode(0);
        }

        CurrentMode?.OnModeStart();
    }

    /// <summary>
    /// Notifies the active mode that the run has ended.
    /// </summary>
    /// <param name="victory"><c>true</c> if the player won; <c>false</c> on defeat.</param>
    public void EndCurrentMode(bool victory)
    {
        CurrentMode?.OnModeEnd(victory);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Event Proxies — wire these to game systems
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Proxy to <see cref="GameModeBase.OnEnemyKilled"/>. Wire to the enemy's
    /// death event, or call from <see cref="WaveManager"/>.
    /// </summary>
    /// <param name="enemy">The enemy that died.</param>
    public void NotifyEnemyKilled(EnemyBase enemy)
    {
        CurrentMode?.OnEnemyKilled(enemy);
    }

    /// <summary>
    /// Proxy to <see cref="GameModeBase.OnPlayerDamaged"/>. Wire to the player's
    /// <see cref="Health.OnHealthChanged"/> event.
    /// </summary>
    /// <param name="damage">Amount of damage dealt.</param>
    public void NotifyPlayerDamaged(float damage)
    {
        CurrentMode?.OnPlayerDamaged(damage);
    }

    /// <summary>
    /// Proxy to <see cref="GameModeBase.OnPlayerDeath"/>. Wire to the player's
    /// <see cref="Health.OnDeath"/> event.
    /// </summary>
    public void NotifyPlayerDeath()
    {
        CurrentMode?.OnPlayerDeath();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Received from <see cref="WaveManager.OnWaveChanged"/>. Fires
    /// <see cref="GameModeBase.OnWaveComplete"/> for the wave that just ended
    /// (the new value minus one).
    /// </summary>
    private void HandleWaveChanged(int newWave)
    {
        if (newWave > 1)
            CurrentMode?.OnWaveComplete(newWave - 1);
    }
}
