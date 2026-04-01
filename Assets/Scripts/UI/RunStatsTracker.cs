using UnityEngine;

/// <summary>
/// Singleton that tracks real-time run statistics.  Reset at the start of every
/// new run via <see cref="ResetStats"/>.
///
/// Consumed by <see cref="GameOverUI"/>, <see cref="PauseMenuUI"/>, and the leaderboard.
///
/// Attach to: A persistent manager GameObject (call <see cref="ResetStats"/> when the
/// game scene loads).
/// </summary>
public class RunStatsTracker : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Global access point to the single <see cref="RunStatsTracker"/> instance.</summary>
    public static RunStatsTracker Instance { get; private set; }

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

    // ─────────────────────────────────────────────────────────────────────────
    //  Public Stats
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Number of enemies killed this run.</summary>
    public int EnemiesKilled { get; set; }

    /// <summary>Total raw damage dealt to all enemies this run.</summary>
    public int TotalDamageDealt { get; set; }

    /// <summary>Total raw damage received by the player this run.</summary>
    public int TotalDamageTaken { get; set; }

    /// <summary>Highest consecutive hit combo reached this run.</summary>
    public int HighestCombo { get; set; }

    /// <summary>Number of ability cards collected this run.</summary>
    public int CardsCollected { get; set; }

    /// <summary><see cref="Time.time"/> at which the current run started.</summary>
    public float RunStartTime { get; private set; }

    /// <summary>Elapsed run time in seconds (live while the run is active).</summary>
    public float RunDuration => Time.time - RunStartTime;

    /// <summary>Total gold earned this run.</summary>
    public int GoldEarned { get; set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resets all stats and records the current time as the run start.
    /// Call this whenever a new run begins.
    /// </summary>
    public void ResetStats()
    {
        EnemiesKilled    = 0;
        TotalDamageDealt = 0;
        TotalDamageTaken = 0;
        HighestCombo     = 0;
        CardsCollected   = 0;
        GoldEarned       = 0;
        RunStartTime     = Time.time;
        Debug.Log("[RunStatsTracker] Stats reset for new run.");
    }
}
