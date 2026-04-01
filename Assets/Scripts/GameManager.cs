using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager is a singleton that persists across scenes and manages the
/// overall state of the game (paused, playing, game over, etc.).
///
/// Usage:
///   Access it from any script via: GameManager.Instance
///   Example:  GameManager.Instance.PauseGame();
///
/// This class is designed to be expanded as the game grows — add score tracking,
/// health, loot, enemy counts, and more here.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton Pattern
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single GameManager instance.</summary>
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        // Enforce singleton: if another instance already exists, destroy this one
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Keep the GameManager alive when loading new scenes
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Game State
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Possible states the game can be in.</summary>
    public enum GameState
    {
        Playing,
        Paused,
        GameOver,
        Win
    }

    /// <summary>Current state of the game. Read-only from outside this class.</summary>
    public GameState CurrentState { get; private set; } = GameState.Playing;

    // ─────────────────────────────────────────────────────────────────────────
    //  Inspector-Exposed Settings
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Scene Names")]
    [Tooltip("Name of the main game scene (used by RestartGame).")]
    [SerializeField] private string mainSceneName = "MainScene";

    // ─────────────────────────────────────────────────────────────────────────
    //  Unity Lifecycle
    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Ensure the game starts unpaused
        Time.timeScale = 1f;
        Debug.Log("[GameManager] Game started.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API – Game Flow
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Pauses the game by setting Time.timeScale to 0.
    /// Physics, animations, and Update loops that use Time.deltaTime will stop.
    /// </summary>
    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f;
            Debug.Log("[GameManager] Game paused.");
        }
    }

    /// <summary>Resumes the game from a paused state.</summary>
    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f;
            Debug.Log("[GameManager] Game resumed.");
        }
    }

    /// <summary>
    /// Toggles between paused and playing states.
    /// Useful to bind to an Escape / Start button.
    /// </summary>
    public void TogglePause()
    {
        if (CurrentState == GameState.Playing)
            PauseGame();
        else if (CurrentState == GameState.Paused)
            ResumeGame();
    }

    /// <summary>Triggers the Game Over state.</summary>
    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        Debug.Log("[GameManager] Game Over.");
        // TODO: Show game-over screen / UI
    }

    /// <summary>Reloads the main game scene to restart the run.</summary>
    public void RestartGame()
    {
        CurrentState = GameState.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainSceneName);
        Debug.Log("[GameManager] Game restarted.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Update – Global Input Handling
    // ─────────────────────────────────────────────────────────────────────────

    private void Update()
    {
        // Press Escape to toggle pause at any time
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }
}
